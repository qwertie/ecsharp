using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Syntax;
using S = Loyc.Syntax.CodeSymbols;
using Loyc.Collections;
using System.Diagnostics;

namespace LeMP
{
	public partial class StandardMacros
	{
		[SimpleMacro(@"replace (input($capture) => output($capture), ...) {...}",
			"Finds one or more patterns in a block of code and replaces each matching expression with another expression. The braces are omitted from the output (and are not matchable).")]
		public static LNode replace(LNode node, IMessageSink sink)
		{
			if (node.ArgCount >= 2)
			{
				var patterns = new Pair<LNode, LNode>[node.ArgCount-1];
				for (int i = 0; i < patterns.Length; i++)
				{
					var pair = node.Args[i];
					if (pair.Calls(S.Lambda, 2)) {
						LNode pattern = pair[0], repl = pair[1];
						patterns[i] = Pair.Create(pattern, repl);
					} else {
						string msg = "Expected 'pattern => replacement'.";
						if (pair.Descendants().Any(n => n.Calls(S.Lambda, 2)))
							msg += " " + "(Using '=>' already? Put the pattern in parentheses.)";
						return Reject(sink, pair, msg);
					}
				}

				var stmts = node.Args.Last.AsList(S.Braces);
				var output = Replace(stmts, patterns);
				return output.AsLNode(S.Splice);
			}
			return null;
		}

		static RVList<LNode> Replace(RVList<LNode> stmts, Pair<LNode, LNode>[] patterns)
		{
			var temp = new MMap<Symbol, LNode>();
			var output = stmts.SmartSelect(stmt => stmt.ReplaceRecursive(n => TryReplaceHere(n, patterns, temp)));
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
			return null;
		}
		public static LNode TryReplaceHere(LNode candidate, LNode pattern, LNode replacement, MMap<Symbol, LNode> captures, Pair<LNode, LNode>[] allPatterns)
		{
			RVList<LNode> attrs;
			if (LNodeExt.MatchesPattern(candidate, pattern, ref captures, out attrs)) {
				foreach (var pair in captures) {
					var input = pair.Value.AsList(S.Splice);
					var output = Replace(input, allPatterns);
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
