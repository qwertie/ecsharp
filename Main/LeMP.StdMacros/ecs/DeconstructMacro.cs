using Loyc;
using Loyc.Collections;
using Loyc.Ecs;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static LeMP.StandardMacros;
using S = Loyc.Syntax.CodeSymbols;

namespace LeMP.ecs
{
	partial class StandardMacros
	{
		[LexicalMacro("#deconstruct(pattern1 | pattern2 = tree);",
			 "Deconstructs the syntax tree `tree` into constituent parts which are assigned to "
			+"compile-time syntax variables marked with `$` that can be used later in the "
			+"same braced block. For example, `#deconstruct($a + $b = x + y + 123)` creates "
			+"a syntax variable called `$a` which expands to `x + y`, and another variable `$b` "
			+"that expands to `123`. These variables behave like macros in their own right that "
			+"can be used later in the same braced block (although technically `$` is a macro in "
			+"the `LeMP` namespace).\n"
			+"\n"
			+"The left-hand side of `=` can specify multiple patterns separated by `|`. If you "
			+"want `=` or `|` themselves (or other low-precedence operators, such as `&&`) to be part "
			+"of the pattern itself on the left-hand side, you should enclose the pattern in braces "
			+"(note: expressions in braces must end with `;` in EC#). If the pattern itself is "
			+"intended to match a braced block, use double braces (e.g. `{{ $stuff; }}`).\n"
			+"\n"
			+"Macros are expanded in the right-hand side (`tree`) before deconstruction occurs.\n\n"
			+"If multiple arguments are provided, e.g. `#deconstruct(e1 => p1, e2 => p2)`, it has "
			+"the same effect as simply writing multiple `#deconstruct` commands.\n\n"
			+"An error is printed when a deconstruction operation fails.",
			"deconstruct", "#deconstruct")]
		public static LNode static_deconstruct(LNode node, IMacroContext context)
		{
			if (node.AttrNamed(S.Static) == null && !node.HasSpecialName)
				return Reject(context, node, "Expected 'static' attribute");
			foreach (var arg in node.Args)
				DoDeconstruct(arg, context, printErrorOnFailure: true);
			return F.Call(S.Splice);
		}

		[LexicalMacro("#tryDeconstruct(expr => pattern);",
			 "This macro behaves the same as `#deconstruct()` except that no error is printed "
			+"(and no syntax variables are created) when a deconstruction operation fails.",
			"tryDeconstruct", "#tryDeconstruct")]
		public static LNode static_tryDeconstruct(LNode node, IMacroContext context)
		{
			if (node.AttrNamed(S.Static) == null && !node.HasSpecialName)
				return Reject(context, node, "Expected 'static' attribute");
			foreach (var arg in node.Args)
				DoDeconstruct(arg, context, printErrorOnFailure: false);
			return F.Call(S.Splice);
		}

		private static void DoDeconstruct(LNode arg, IMacroContext context, bool printErrorOnFailure)
		{
			LNode patternSpec = arg.Args[0, LNode.Missing], input = arg.Args[1, LNode.Missing];
			if (!arg.Calls(S.Assign, 2))
			{
				if (arg.Calls(S.Lambda, 2))
					G.Swap(ref patternSpec, ref input);
				else {
					context.Sink.Error(arg, "expected an assignment (`patterns = input`)");
					return;
				}
			}

			// Build list of patterns out of the binary operator series p1 | p2 | p3
			var patterns = new FVList<LNode>();
			while (patternSpec.Calls(S.OrBits, 2) && !patternSpec.IsParenthesizedExpr()) {
				patterns.Add(patternSpec[1]);
				patternSpec = patternSpec[0];
			}
			patterns.Add(patternSpec);

			// Remove outer braces, then run macros
			patterns = patterns.SmartSelect(p => p.UnwrapBraces());
			input = input.UnwrapBraces();
			input = context.PreProcess(input);

			// Perform matching & capturing
			foreach (var pattern in patterns) {
				IDictionary<Symbol, LNode> captures;
				if (LNodeExt.MatchesPattern(input, pattern, out captures)) {
					if (captures.Count == 0)
						context.Write(printErrorOnFailure ? Severity.Warning : Severity.Error, pattern,
							"This pattern has no effect, since it does not use `$` to capture any variables.");
					SetSyntaxVariables(captures, context);
					return;
				}
			}

			if (printErrorOnFailure)
				context.Sink.Error(arg, "Deconstruction failed.");
		}

		[LexicalMacro("$variableName",
			"Expands a variable (scoped property) assigned by a macro such as `static deconstruct()` or `static tryDeconstruct()`.",
			"'$", Mode = MacroMode.Passive)]
		public static LNode DollarSignVariable(LNode node, IMacroContext context)
		{
			LNode id;
			if (node.ArgCount == 1 && (id = node.Args[0]).IsId && !id.HasPAttrs()) {
				object value;
				if (context.ScopedProperties.TryGetValue("$" + id.Name.Name, out value) ||
					context.ScopedProperties.TryGetValue(id.Name, out value)) {
					if (value is LNode)
						return ((LNode)value).WithRange(id.Range);
					else
						context.Sink.Warning(id, "The specified scoped property is not a syntax tree. "+
							"Use `#getScopedProperty({0})` to insert it as a literal.", EcsNodePrinter.PrintId(id.Name));
				} else {
					context.Sink.Warning(id, "There is no macro property in scope named `{0}`", id.Name);
				}
			}
			return null;
		}
	}
}
