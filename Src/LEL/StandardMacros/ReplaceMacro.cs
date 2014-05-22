using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Syntax;
using S = Loyc.Syntax.CodeSymbols;

namespace LeMP
{
	public partial class StandardMacros
	{
		[SimpleMacro(@"replace (input($capture) => output($capture), ...) {...}",
			"Produces variations of a block of code. The braces are omitted from the output.")]
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

				var stmts = node.AsList(S.Braces);
				var captures = new Dictionary<Symbol, LNode>();
				var results = stmts.Select(stmt => stmt.ReplaceRecursive(n => ReplaceOne(n, patterns, captures)));
			}
			return null;
		}

		private static LNode ReplaceOne(LNode node, Pair<LNode, LNode>[] patterns, Dictionary<Symbol, LNode> captures)
		{
			for (int i = 0; i < patterns.Length; i++)
			{
				captures.Clear();
				LNode r = MatchPattern(node, patterns[i].A, captures);
				if (r != null) return r;
			}
			return null;
		}

		public static LNode MatchPattern(LNode candidate, LNode pattern, Dictionary<Symbol, LNode> captures)
		{
			return null;//TODO
		}
	}
}
