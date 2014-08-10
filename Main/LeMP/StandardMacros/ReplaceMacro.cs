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
			if (MatchesPattern(candidate, pattern, ref captures, out attrs)) {
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

		#region MatchesPattern() and helper methods

		public static bool MatchesPattern(LNode candidate, LNode pattern, ref MMap<Symbol, LNode> captures, out RVList<LNode> attrs)
		{
			// [$capture] (...)
			if (!AttributesMatch(candidate, pattern, ref captures, out attrs))
				return false;

			// $capture
			LNode sub;
			if (pattern.Calls(S.Substitute, 1) && (sub = pattern.Args.Last).IsId)
			{
				if (candidate.AttrCount > attrs.Count)
					candidate = candidate.WithAttrs(attrs);
				AddCapture(captures, sub.Name, candidate);
				attrs = RVList<LNode>.Empty;
				return true;
			}

			var kind = candidate.Kind;
			if (kind != pattern.Kind)
				return false;

			if (candidate.Name != pattern.Name)
				return false;
			if (kind == NodeKind.Literal)
				return object.Equals(candidate.Value, pattern.Value);
			else if (kind == NodeKind.Call) {
				if (!MatchesPatternNested(candidate.Target, pattern.Target, ref captures, ref attrs))
					return false;
				var cArgs = candidate.Args;
				var pArgs = pattern.Args;

				if (pArgs.Count != cArgs.Count && !pArgs.Any(IsParamsCapture))
					return false;

				// Scan from the end of the list to the beginning (RVLists is good at this),
				// matching args one-by-one. Use MatchThenParams() in case of $(params capture).
				while (!pArgs.IsEmpty) {
					LNode pArg = pArgs.Pop();
					if (IsParamsCapture(pArg))
						return MatchThenParams(cArgs, pArgs, pArg, ref captures, ref attrs);
					if (cArgs.IsEmpty)
						return false;
					if (!MatchesPatternNested(cArgs.Pop(), pArg, ref captures, ref attrs))
						return false;
				}
				return true;
			} else // kind == Id
				return true;
		}

		static void AddCapture(MMap<Symbol, LNode> captures, LNode cap, Slice_<LNode> items)
		{
			Debug.Assert(cap.Calls(S.Substitute, 1) && cap.Args.Last.IsId);
			if (items.Count == 1)
				AddCapture(captures, cap.Args.Last.Name, items[0]);
			else
				AddCapture(captures, cap.Args.Last.Name, F.Call(S.Splice, items));
		}
		static void AddCapture(MMap<Symbol, LNode> captures, Symbol capName, LNode candidate)
		{
			LNode oldCap = captures.TryGetValue(capName, null);
			captures[capName] = LNode.MergeLists(oldCap, candidate, S.Splice);
		}

		static bool MatchesPatternNested(LNode candidate, LNode pattern, ref MMap<Symbol, LNode> captures, ref RVList<LNode> attrs)
		{
			RVList<LNode> unmatchedAttrs;
			if (!MatchesPattern(candidate, pattern, ref captures, out unmatchedAttrs))
				return false;
			if (unmatchedAttrs.Any(a => !a.IsTrivia))
				return false;
			attrs.AddRange(unmatchedAttrs);
			return true;
		}

		static bool AttributesMatch(LNode candidate, LNode pattern, ref MMap<Symbol, LNode> captures, out RVList<LNode> unmatchedAttrs)
		{
			if (pattern.HasPAttrs())
				throw new NotImplementedException("TODO: attributes in patterns are not yet supported");
			unmatchedAttrs = candidate.Attrs;
			return true;
		}
		static bool IsParamsCapture(LNode p)
		{
			return p.Calls(S.Substitute, 1) && p.Args.Last.AttrNamed(S.Params) != null;
		}
		static bool MatchThenParams(RVList<LNode> cArgs, RVList<LNode> pArgs, LNode paramsCap, ref MMap<Symbol, LNode> captures, ref RVList<LNode> attrs)
		{
			// This helper function of MatchesPattern() is called when pArgs is followed 
			// by a $(params capture). cArgs is the list of candidate.Args that have not 
			// yet been matched; pArgs is the list of pattern.Args that have not yet been 
			// matched, and paramsCap is the $(params capture) node that follows pArgs.
			int c = 0, p = 0;
		restart:
			for (; p < pArgs.Count; p++, c++) {
				if (IsParamsCapture(pArgs[p])) {
					if (!CaptureGroup(ref c, ref p, cArgs, pArgs, ref captures, ref attrs))
						return false;
					goto restart;
				} else {
					if (c >= cArgs.Count)
						return false;
					if (!MatchesPatternNested(cArgs[c], pArgs[p], ref captures, ref attrs))
						return false;
				}
			}
			AddCapture(captures, paramsCap, new Slice_<LNode>(cArgs, c));
			return true;
		}

		static bool CaptureGroup(ref int c, ref int p, RVList<LNode> cArgs, RVList<LNode> pArgs, ref MMap<Symbol, LNode> captures, ref RVList<LNode> attrs)
		{
			Debug.Assert(IsParamsCapture(pArgs[p]));
			// The goal now is to find a sequence of nodes in cArgs that matches
			// the sequence pArgs[p+1 .. p+x] where x is the maximum value such
			// that none of the nodes in the sequence are $(params caps).
			int saved_p = p, saved_c = c;
			var savedCaptures = captures.AsImmutable();
			var savedAttrs = attrs;
			int captureSize = 0;
			for (;; captureSize++) {
				for (p++, c += captureSize; ; c++, p++) {
					// If we run out of pArgs, great, we're done; if we run out 
					// of cArgs, the match fails, unless all remaining pArgs are 
					// $(params caps).
					if (p >= pArgs.Count || IsParamsCapture(pArgs[p])) {
						goto done_group;
					} else {
						if (c >= cArgs.Count)
							return false;
						if (!MatchesPatternNested(cArgs[c], pArgs[p], ref captures, ref attrs))
							goto continue_group;
					}
				}
				continue_group:;
				p = saved_p;
				c = saved_c;
				attrs = savedAttrs;
				captures = savedCaptures.AsMutable();
			}
		done_group:
			AddCapture(captures, pArgs[saved_p], cArgs.Slice(saved_c, captureSize));
			return true;
		}

		#endregion
	}
}
