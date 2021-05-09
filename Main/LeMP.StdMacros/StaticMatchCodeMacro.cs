using Loyc;
using Loyc.Collections;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using S = Loyc.Syntax.CodeSymbols;

namespace LeMP
{
	partial class StandardMacros
	{
		internal static void SetSyntaxVariables(IDictionary<Symbol, LNode> captures, IMacroContext context)
		{
			foreach (var captured in captures)
				if (captured.Key != __)
					context.ScopedProperties["$" + captured.Key.Name] = captured.Value;
		}

		[LexicalMacro("static matchCode (expr) { case ...: ... }; // In LES, use a => b instead of case a: b",
			"Attempts to match and deconstruct a syntax tree at compile-time, e.g. `case $a + $b:` "+
			"expects a tree that calls `+` with two parameters, placed in compile-time variables called $a and $b. "+
			"Example: \n\n"+
			"    #set varDecl = List<int> x;\n"+
			"    static matchCode($varDecl) {\n"+
			"        case $T[] $x, $T[,] $x: length = $x.Length;\n"+
			"        default:                length = $x.Count;\n"+
			"    }\n\n"+
			"If `expr` is a single statement inside braces, the braces are stripped. Then, macros are executed "+
			"on `expr` to produce a new syntax tree which is then matched."+
			"`matchCode` then scans the cases to find one that matches. Finally, the entire `static matchCode` "+
			"construct is replaced with the handler associated with the matching `case`. "+
			"If none of the cases match and there is no `default:` case, the entire `static matchCode` "+
			"construct and all its cases are eliminated from the output.\n\n"+
			"Use `case pattern1, pattern2:` to handle multiple cases with the same handler. "+
			"Unlike C# `switch`, this statement does not expect `break` at the end of each case. If `break` "+
			"is present at the end of the matching case, it is emitted literally into the output.\n\n",
			"matchCode", "#matchCode", Mode = MacroMode.Passive)]
		public static LNode static_matchCode(LNode node, IMacroContext context)
		{
			if (node.AttrNamed(S.Static) == null && !node.HasSpecialName)
				return null; // handled by normal matchCode macro

			var args_body = context.GetArgsAndBody(false);
			LNodeList args = args_body.Item1, body = args_body.Item2;
			if (args.Count != 1)
				return Reject(context, args[1], "Expected only one expression to match");

			var expression = context.PreProcess(UnwrapBraces(args[0]));

			var cases = GetCases(body, context.Sink);
			// The `default:` case is represented by an empty list of patterns.
			if (cases.WithoutLast(1).Any(pair => pair.Cases.IsEmpty))
				context.Write(Severity.Error, node, "The `default:` case must be the last one, because the cases are tested in the order they appear, so no case after `default:` can be matched.");

			MMap<Symbol, LNode> captures = new MMap<Symbol, LNode>();
			foreach (var pair in cases)
			{
				var patterns = pair.Cases.IsEmpty ? new VList<LNode>((LNode)null) : new VList<LNode>(pair.Cases);
				foreach (var pattern in patterns)
				{
					captures.Clear();
					if (pattern == null || LNodeExt.MatchesPattern(expression, pattern, ref captures, out LNodeList _)) {
						captures[_hash] = expression; // define $#
						captures.Remove(__);
						return ReplaceCaptures(pair.Handler.AsLNode(S.Splice), captures);
					}
				}
			}
			return F.Call(S.Splice); // none of the cases matched
		}

		[LexicalMacro(@"syntaxTree `staticMatches` pattern", 
			 "Returns the literal true if the form of the syntax tree on the left matches the pattern on the right. "
			+"The pattern can use `$variables` to match any subtree. `$(..lists)` can be matched too. "
			+"In addition, if the result is true then a syntax variable is created for each binding in the pattern other than `$_`; "
			+"for example, ``Foo(123) `staticMatches` Foo($arg)`` sets `$arg` to `123`; you can use `$arg` later in your code.\n\n"
			+"The syntax tree on the left is macro-preprocessed, but the argument on the right is not. "
			+"If either side is a single statement in braces (before preprocessing), the braces are ignored. ",
			"staticMatches", "'staticMatches", "staticMatchesCode", "'staticMatchesCode", "'#matchesCode", "'#matchesCode")]
		public static LNode staticMatches(LNode node, IMacroContext context)
		{
			if (node.ArgCount != 2)
				return null;

			LNode candidate = context.PreProcess(UnwrapBraces(node[0]));
			LNode pattern = UnwrapBraces(node[1]);
			MMap<Symbol, LNode> captures = new MMap<Symbol, LNode>();
			if (LNodeExt.MatchesPattern(candidate, pattern, ref captures, out LNodeList _)) {
				SetSyntaxVariables(captures, context);
				return F.True;
			}
			return F.False;
		}
	}
}
