using Loyc.Syntax;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using S = Loyc.Syntax.CodeSymbols;
using Loyc;
using Loyc.Collections;

namespace LeMP
{
	partial class StandardMacros
	{
		[LexicalMacro(@"##map(inputList, pattern => replacement, ...); ##map(inputList, skipSpec, pattern => replacement, ...)",
			"Performs a transformation on each argument in an argument list." +
			"\n\n" +
			"This macro requires at least two arguments. First, it preprocesses all " +
			"arguments that are not of the form `a => b`. It expects the first argument " +
			"(inputList) to be a call node, against which a find-and-replace operation is " +
			"performed using the other arguments." +
			"\n\n" +
			"Example: `##map(foo(var, 5 + 5), $x => stringify($x))` produces `foo(\"var\", \"5 + 5\")`.\n" +
			"Example: `##map(Print(x, y, -3), $x => Abs($x), 1)` produces `Print(x, Abs(y), Abs(-3))`." +
			"\n\n" +
			"The optional `skipSpec` parameter specifies which arguments of `inputList` " +
			"to process; if it is an integer literal, it specifies a number of arguments to " +
			"skip at the beginning. This must be used, for example, to transform C# array " +
			"expressions. For example, `##map(new int[] { x, y }, 1, $x => Abs($x))` produces " +
			"`new int[] { Abs(x), Abs(y) }`. In the Loyc tree, the first argument of `new` " +
			"indicates the data type being constructed (in this case `int[]`) and usually " +
			"you do not want to perform any transformations on this first argument. The " +
			"`skipSpec` can also be a range such as `1 .. 3` (to process the second and " +
			"third arguments only) or `0 .. ^1` (to process all arguments except the last). " +
			"Neither of the integers is required to be in range; for example, " +
			"`##map((a, b), 2..5, $x => -$x)` is valid but has no effect (output: `(a, b)`)." +
			"The skipSpec can also be used to cause the Target and/or attributes to be " +
			"processed; the Target has index -1 and attributes have more negative indexes, so " +
			"the value `-1` would transform the Target and all arguments, while `-999_999_999..-1` " +
			"processes only the attributes. If the final argument is -1 or less, the normal " +
			"requirement that `inputList` be a call node is waived." +
			"\n\n" +
			"If an argument doesn't match any of the patterns, `##map` copies it to the output" +
			"unchanged. If you'd like to remove such arguments, use `##mapWithFilter` instead." +
			"\n\n" +
			"##map also has the notable ability to perform matching operations against multiple " +
			"consecutive arguments. For example, `##map((1, 2, 3, 4), { $a; $b; } => $a + $b)` " +
			"produces `(1 + 2, 3 + 4)`." +
			"\n\n" +
			"Keep in mind that the `=>` operator has a high precedence on the left side. For " +
			"example, `$a + $b => $b + $a` is invalid because it means `$a + ($b => $b + $a)`." +
			"Each of the other arguments to ##map should have the form `pattern => replacement`," +
			"and this example does not, so ##map will print an error.",
			"##map")]
		public static LNode map(LNode node, IMacroContext context) => mapWithFilter(node, context, true);

		[LexicalMacro(@"##mapWithFilter(inputList, pattern => replacement, ...); ##mapWithFilter(inputList, skipSpec, pattern => replacement, ...)",
			"Performs a transformation on each argument in an argument list. This macro behaves " +
			"the same way as `##map` except in the case where an input item doesn't match any of " +
			"the patterns you provided." +
			"\n\n" +
			"Unlike ##map, ##mapWithFilter filters out any items that don't match a pattern. For example, " +
			"`##map((1+2, 3, 4*5), ($a+$b) => $a+$b, ($a*$b) => $a*$b)` produces `(1+2, 4*5)`.",
			"##mapWithFilter")]
		public static LNode mapWithFilter(LNode node, IMacroContext context) => mapWithFilter(node, context, false);

		public static LNode mapWithFilter(LNode node, IMacroContext context, bool keepUnmatched)
		{
			if (node.ArgCount < 2)
				return Reject(context, node, "Expected two or more arguments");

			// Preprocess some arguments
			var args = node.Args.SmartSelect(n => n.Calls(S.Lambda, 2) ? n : context.PreProcess(n));

			LNode input = node.Args[0];

			// Decode the skipSpec, if any
			LNode skipSpec = node.Args[1];
			int? asInt = GetIntValue(skipSpec);
			(int start, int stop)? range = GetIntRange(skipSpec, input.ArgCount);
			if (range == null) {
				int? num = GetIndex(skipSpec, input.ArgCount);
				if (num != null)
					range = (num.Value, int.MaxValue);
			}
			int iFirstPattern = 1; // index of first pattern
			if (range == null)
				range = (0, int.MaxValue);
			else
				iFirstPattern++;

			// Ensure range is valid
			range = (Math.Max(range.Value.start, input.Min), Math.Min(range.Value.stop, input.Max + 1));

			// Expect first argument to be a call, unless skipSpec says to process attributes only
			if (!input.IsCall && range.Value.stop > -1)
				return Reject(context, input, "Expected a call node");

			// Decode the patterns
			var cases = new List<(LNodeList Pattern, LNodeList Replacement)>();
			for (int i = iFirstPattern; i < node.ArgCount; i++) {
				var arg = node.Args[i];
				if (arg.Calls(CodeSymbols.Lambda, 2)) {
					LNode pattern = arg.Args[0], replacement = arg.Args[1];
					if (pattern.Calls(S.Braces, 0))
						return Reject(context, pattern, "An empty pattern is not valid. To match an empty pair of braces, use nested braces: {{}}");
					cases.Add((pattern.AsList(S.Braces), replacement.AsList(S.Braces)));
				} else {
					string msg = "Expected expression of the form `pattern => replacement`.".Localized();
					if (arg.Descendants().Any(n => n.Calls(S.Lambda, 2)))
						msg += " " + "Ensure that the left-hand side of `=>` is enclosed in parentheses.".Localized();
					return Reject(context, arg, msg);
				}
			}

			if (cases.Count == 0)
				return Reject(context, args[1], "Expected at least one pattern of the form `pattern => replacement`");

			if (keepUnmatched) {
				// To keep unmatched items, add an "identity" pattern
				var keep = LNode.List(F.Call(S.Substitute, F.Id("keep")));
				cases.Add((keep, keep));
			}

			if (range.Value.start < -1) {
				// Perform the mapping operation on the attributes
				int stop = Math.Min(range.Value.stop, -1);
				var (outputAttrs, _) = DoMapping(input, range.Value.start, stop, cases);
				outputAttrs.InsertRange(0, input.Attrs.Slice(0, range.Value.start - input.Min));
				outputAttrs.AddRange(input.Attrs.Slice(input.Attrs.Count - (-1 - stop)));
				input = input.WithAttrs(LNode.List(outputAttrs));
			}

			if (range.Value.stop > -1) {
				// Perform the mapping operation on the Target and/or Args
				int min = Math.Max(range.Value.start, -1);
				var (output, _) = DoMapping(input, min, range.Value.stop, cases);
				if (min > -1)
					output.InsertRange(0, input.Slice(0, min));
				output.AddRange(input.Slice(range.Value.stop));

				if (min > -1) {
					// Target was not changed
					return input.WithArgs(output);
				} else {
					// Target was changed (Args may have also been changed)
					if (output.Count == 0) {
						context.Warning(node, "Your mappings caused the Target and argument list to be deleted. The output will be the empty identifier.");
						return LNode.Id(input.Attrs, S.Missing, input);
					}
					return input.With(output[0], LNode.List(output.Slice(1)));
				}
			} else {
				return input;
			}
		}

		static (List<LNode> output, int failIndex) DoMapping(LNode input, int start, int stop, List<(LNodeList Pattern, LNodeList Replacement)> cases)
		{
			List<LNode> output = new List<LNode>();
			int failIndex = -1;
			for (int i = start; i < stop; i++)
			{
				foreach (var @case in cases)
				{
					if (i + @case.Pattern.Count > stop)
						continue;

					var unmatchedAttrs = LNode.List();
					var captures = new MMap<Symbol, LNode>();
					for (int offset = 0; offset < @case.Pattern.Count; offset++)
					{
						if (!LNodeExt.MatchesPattern(input[i + offset], @case.Pattern[offset], ref captures, out var unmatchedAttrs_))
							goto not_matched;
						unmatchedAttrs.AddRange(unmatchedAttrs_);
					}

					// Match found!
					foreach (var outputSpec in @case.Replacement)
						output.Add(ReplaceCaptures(outputSpec, captures).PlusAttrsBefore(unmatchedAttrs));
					i += @case.Pattern.Count - 1;
					goto matched;

					not_matched: continue;
				}
				failIndex = i;
				matched: continue;
			}

			return (output, failIndex);
		}

		public static int? GetIntValue(LNode integer)
		{
			bool negative;
			if (negative = integer.Calls(S.Sub, 1))
				integer = integer.Args[0];
			if (!(integer.Value is int i32)) {
				if (!integer.IsLiteral)
					return null;
				i32 = Convert.ToInt32(integer.Value);
			}
			return negative ? -i32 : i32;
		}
		public static int? GetIndex(LNode indexSpec, int listCount)
		{
			int? value = GetIntValue(indexSpec);
			if (value != null)
				return value.Value;
			if (indexSpec.Calls(S.XorBits, 1) && (value = GetIntValue(indexSpec[0])) != null)
				return listCount - value.Value;
			return null;
		}
		public static (int start, int stop)? GetIntRange(LNode rangeSpec, int listCount)
		{
			int? start = null, stop = null;
			if (rangeSpec.Calls(CodeSymbols.DotDot, 2) && G.Var(out LNode first, rangeSpec.Args[0]) && G.Var(out LNode second, rangeSpec.Args[1])
				|| rangeSpec.Calls(CodeSymbols.DotDot, 1) && G.True(second = rangeSpec.Args[0]) && G.True(first = null)
				|| rangeSpec.Calls(CodeSymbols.SufDotDot, 1) && G.True(first = rangeSpec.Args[0]) && G.True(second = null)) {
				if (first != null) {
					start = GetIndex(first, listCount);
					if (start == null)
						return null;
				}
				if (second != null) {
					stop = GetIndex(second, listCount);
					if (stop == null)
						return null;
				}
				return (start ?? 0, stop ?? listCount);
			}
			return null;
		}
	}
}
