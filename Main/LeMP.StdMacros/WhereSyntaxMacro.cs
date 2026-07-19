using Loyc;
using Loyc.Collections;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeMP
{
	public partial class StandardMacros
	{
		[LexicalMacro(@"##whereMatches(inputList, patterns...); ##whereMatches(inputList, skipSpec, patterns...)",
			"Filters out arguments of inputList that do not match any of the patterns listed. " +
			"If no patterns are provided, this macro simply deletes all the arguments. " +
			"\n\n" +
			"This macro does not preprocess its arguments before filtering." +
			"\n\n" +
			"Example: `##whereMatches(foo(x++, y(), z++), $var++)` produces `foo(x++, z++)`.\n" +
			"\n\n" +
			"The optional `skipSpec` parameter specifies which arguments of `inputList` " +
			"to process; if it is an integer literal, it specifies a number of arguments to " +
			"skip at the beginning. See the documentation of ##map for more information.",
			"##filter", "##whereMatches")]
		public static LNode filter(LNode node, IMacroContext context) => whereSyntax(node, context, false);

		[LexicalMacro(@"##filterOut(inputList, patterns...); ##filterOut(inputList, skipSpec, patterns...)",
			"Filters out arguments of inputList that match any of the patterns listed. " +
			"If no patterns are provided, this macro returns inputList unchanged. " +
			"\n\n" +
			"This macro does not preprocess its arguments before filtering." +
			"\n\n" +
			"Example: `##filterOut(foo(x++, y(), z++), $var++)` produces `foo(y())`.\n" +
			"\n\n" +
			"The optional `skipSpec` parameter specifies which arguments of `inputList` " +
			"to process; if it is an integer literal, it specifies a number of arguments to " +
			"skip at the beginning. See the documentation of ##map for more information.",
			"##filterOut")]
		public static LNode filterOut(LNode node, IMacroContext context) => whereSyntax(node, context, true);

		public static LNode whereSyntax(LNode node, IMacroContext context, bool filterOut)
		{
			if (node.ArgCount < 2)
				return Reject(context, node, "Expected two or more arguments");

			var args = node.Args;
			LNode input = args[0];

			// Decode the skipSpec, if any
			var range = TryDecodeSkipSpec(input, node.Args[1]);
			int iFirstPattern = range.exists ? 2 : 1;

			// Expect first argument to be a call, unless skipSpec says to process attributes only
			if (!input.IsCall && range.stop > -1)
				return Reject(context, input, "Expected a call node");

			int index = input.Min - 1;
			var captures = new MMap<Symbol, LNode>();

			return input.Select(arg =>
			{
				index++;
				if (index < range.start || index >= range.stop)
					return arg;

				bool match = false;
				for (int i = iFirstPattern; i < args.Count; i++) {
					var pattern = args[i];
					captures.Clear();
					if (LNodeExt.MatchesPattern(arg, pattern, ref captures, out var unmatchedAttrs)) {
						match = true;
						break;
					}
				}

				return match == filterOut ? default(Maybe<LNode>) : arg;
			});
		}
	}
}
