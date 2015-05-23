using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Syntax;
using S = Loyc.Syntax.CodeSymbols;
using Loyc.Collections;
using System.Diagnostics;
using Loyc.Syntax.Lexing;
using Loyc.Collections.Impl;

namespace LeMP
{
	public partial class StandardMacros
	{
		[LexicalMacro(@"replace (input($capture) => output($capture), ...) {...}",
			"Finds one or more patterns in a block of code and replaces each matching expression with another expression. "+
			"The braces are omitted from the output (and are not matchable)."+
			"This macro can be used without braces, in which case it affects all the statements/arguments that follow it in the current statement or argument list.")]
		public static LNode replace(LNode node, IMacroContext context)
		{
			var args_body = context.GetArgsAndBody(true);
			var args = args_body.A;
			var body = args_body.B;
			if (args.Count >= 1)
			{
				var patterns = new Pair<LNode, LNode>[args.Count];
				for (int i = 0; i < patterns.Length; i++)
				{
					var pair = args[i];
					if (pair.Calls(S.Lambda, 2)) {
						LNode pattern = pair[0], repl = pair[1];
						patterns[i] = Pair.Create(pattern, repl);
					} else {
						string msg = "Expected 'pattern => replacement'.";
						if (pair.Descendants().Any(n => n.Calls(S.Lambda, 2)))
							msg += " " + "(Using '=>' already? Put the pattern on the left-hand side in parentheses.)";
						return Reject(context, pair, msg);
					}
				}

				int replacementCount;
				var output = Replace(body, patterns, out replacementCount);
				if (replacementCount == 0)
					context.Write(Severity.Warning, node, "No patterns recognized; no replacements were made.");
				return output.AsLNode(S.Splice);
			}
			return null;
		}

		[ThreadStatic]
		static InternalList<Triplet<Symbol, LNode, int>> _tokenTreeRepls;

		public static RVList<LNode> Replace(RVList<LNode> stmts, Pair<LNode, LNode>[] patterns, out int replacementCount)
		{
			// This list is used to support simple token replacement in TokenTrees
			_tokenTreeRepls = InternalList<Triplet<Symbol, LNode, int>>.Empty;
			foreach (var pair in patterns) // Look for Id => Id or Id => Literal
				if (pair.A.IsId && (pair.B.IsId || pair.B.IsLiteral))
					_tokenTreeRepls.Add(new Triplet<Symbol,LNode,int>(pair.A.Name, pair.B, 0));

			// Scan the syntax tree for things to replace...
			int count = 0;
			var temp = new MMap<Symbol, LNode>();
			var output = stmts.SmartSelect(stmt => stmt.ReplaceRecursive(n => {
				LNode r = TryReplaceHere(n, patterns, temp);
				if (r != null) count++;
				return r;
			}));
			replacementCount = count;
			return output;
		}

		static LNode TryReplaceHere(LNode node, Pair<LNode, LNode>[] patterns, MMap<Symbol, LNode> temp)
		{
			for (int i = 0; i < patterns.Length; i++)
			{
				temp.Clear();
				LNode r = TryReplaceHere(node, patterns[i].A, patterns[i].B, temp, patterns);
				if (r != null) return r;
			}

			// Support simple token replacement in TokenTrees
			TokenTree tt;
			if (node.IsLiteral && (tt = node.Value as TokenTree) != null) {
				bool modified = ReplaceInTokenTree(ref tt, _tokenTreeRepls);
				if (modified)
					return node.WithValue(tt);
			}
	
			return null;
		}
		public static LNode TryReplaceHere(LNode node, LNode pattern, LNode replacement, MMap<Symbol, LNode> captures, Pair<LNode, LNode>[] allPatterns)
		{
			RVList<LNode> attrs;
			if (LNodeExt.MatchesPattern(node, pattern, ref captures, out attrs)) {
				foreach (var pair in captures) {
					var input = pair.Value.AsList(S.Splice);
					int c;
					var output = Replace(input, allPatterns, out c);
					if (output != input)
						captures[pair.Key] = output.AsLNode(S.Splice);
				}
				return ReplaceCaptures(replacement, captures).PlusAttrs(attrs);
			}

			return null;
		}
		static LNode ReplaceCaptures(LNode node, MMap<Symbol, LNode> captures)
		{
			if (captures.Count != 0)
			{
				// TODO: EXPAND SPLICES! Generally it works anyway though because 
				// the macro processor has built-in support for #splice.
				return node.ReplaceRecursive(n => {
					LNode sub, cap;
					if (n.Calls(S.Substitute, 1) && (sub = n.Args.Last).IsId && captures.TryGetValue(sub.Name, out cap))
						return cap;
					return null;
				});
			}
			return node;
		}
	}
}
