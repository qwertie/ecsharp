using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Collections;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.Syntax
{
	/// <summary>Standard extension methods for <see cref="LNode"/>.</summary>
	public static class LNodeExt
	{
		/// <summary>Interprets a node as a list by returning <c>block.Args</c> if 
		/// <c>block.Calls(braces)</c>, otherwise returning a one-item list of nodes 
		/// with <c>block</c> as the only item.</summary>
		public static VList<LNode> AsList(this LNode block, Symbol listIdentifier)
		{
			return block.Calls(listIdentifier) ? block.Args : new VList<LNode>(block);
		}

		/// <summary>Converts a list of LNodes to a single LNode by using the list 
		/// as the argument list in a call to the specified identifier, or, if the 
		/// list contains a single item, by returning that single item.</summary>
		/// <param name="listIdentifier">Target of the node that is created if <c>list</c>
		/// does not contain exactly one item. Typical values include "'{}" and "#splice".</param>
		/// <remarks>This is the reverse of the operation performed by <see cref="AsList(LNode,Symbol)"/>.</remarks>
		public static LNode AsLNode(this VList<LNode> list, Symbol listIdentifier)
		{
			if (list.Count == 1)
				return list[0];
			else {
				var r = SourceRange.Nowhere;
				if (list.Count != 0) {
					r = list[0].Range;
					r = new SourceRange(r.Source, r.StartIndex, list.Last.Range.EndIndex - r.StartIndex);
				}
 				return LNode.Call(listIdentifier, list, r);
			}
		}

		public static VList<LNode> WithSpliced(this VList<LNode> list, int index, LNode node, Symbol listName = null)
		{
			if (node.Calls(listName ?? CodeSymbols.Splice))
				return list.InsertRange(index, node.Args);
			else
				return list.Insert(index, node);
		}
		public static VList<LNode> WithSpliced(this VList<LNode> list, LNode node, Symbol listName = null)
		{
			if (node.Calls(listName ?? CodeSymbols.Splice))
				return list.AddRange(node.Args);
			else
				return list.Add(node);
		}
		public static void SpliceInsert(this WList<LNode> list, int index, LNode node, Symbol listName = null)
		{
			if (node.Calls(listName ?? CodeSymbols.Splice))
				list.InsertRange(index, node.Args);
			else
				list.Insert(index, node);
		}
		public static void SpliceAdd(this WList<LNode> list, LNode node, Symbol listName = null)
		{
			if (node.Calls(listName ?? CodeSymbols.Splice))
				list.AddRange(node.Args);
			else
				list.Add(node);
		}


		public static LNode AttrNamed(this LNode self, Symbol name)
		{
			return self.Attrs.NodeNamed(name);
		}
		public static LNode WithoutAttrNamed(this LNode self, Symbol name)
		{
			LNode _;
			return WithoutAttrNamed(self, name, out _);
		}
		public static VList<LNode> Without(this VList<LNode> list, LNode node)
		{
			int i = list.Count;
			foreach (var item in list.ToFVList()) {
				i--;
				if (item == node) {
					Debug.Assert(list[i] == node);
					return list.RemoveAt(i);
				}
			}
			return list;
		}
		public static LNode WithoutAttr(this LNode self, LNode node)
		{
			return self.WithAttrs(self.Attrs.Without(node));
		}
		public static LNode WithoutAttrNamed(this LNode self, Symbol name, out LNode removedAttr)
		{
			var a = self.Attrs.WithoutNodeNamed(name, out removedAttr);
			if (removedAttr != null)
				return self.WithAttrs(a);
			else
				return self;
		}
		public static VList<LNode> WithoutNodeNamed(this VList<LNode> a, Symbol name)
		{
			LNode _;
			return WithoutNodeNamed(a, name, out _);
		}
		public static VList<LNode> WithoutNodeNamed(this VList<LNode> list, Symbol name, out LNode removedNode)
		{
			removedNode = null;
			for (int i = 0, c = list.Count; i < c; i++)
				if (list[i].Name == name) {
					removedNode = list[i];
					return list.RemoveAt(i);
				}
			return list;
		}

		public static LNode ArgNamed(this LNode self, Symbol name)
		{
			return self.Args.NodeNamed(name);
		}
		public static int IndexWithName(this VList<LNode> self, Symbol name)
		{
			int i = 0;
			foreach (LNode node in self)
				if (node.Name == name)
					return i;
				else
					i++;
			return -1;
		}
		public static LNode NodeNamed(this VList<LNode> self, Symbol name)
		{
			foreach (LNode node in self)
				if (node.Name == name)
					return node;
			return null;
		}

		#region Add/remove parentheses

		/// <summary>Returns the same node with a parentheses attribute added.</summary>
		public static LNode InParens(this LNode node)
		{
			return node.PlusAttrBefore(LNode.Id(CodeSymbols.TriviaInParens, node.Range));
		}
		/// <summary>Returns the same node with a parentheses attribute added.</summary>
		/// <remarks>The node's range is changed to the provided <see cref="SourceRange"/>
        /// and the original range of the node is assigned to the parentheses attribute.</remarks>
		public static LNode InParens(this LNode node, SourceRange range)
		{
			return node.WithRange(range).PlusAttrBefore(LNode.Id(CodeSymbols.TriviaInParens, node.Range));
		}
		/// <summary>Returns the same node with a parentheses attribute added.</summary>
		public static LNode InParens(this LNode node, ISourceFile file, int startIndex, int endIndex)
		{
            return InParens(node, new SourceRange(file, startIndex, endIndex - startIndex));
		}
		/// <summary>Removes a single pair of parentheses, if the node has a 
		/// #trivia_inParens attribute. Returns the same node when no parens are 
		/// present.</summary>
		public static LNode WithoutOuterParens(this LNode self)
		{
			LNode parens;
			self = WithoutAttrNamed(self, S.TriviaInParens, out parens);
			// Restore original node range
			if (parens != null && self.Range.Contains(parens.Range))
				return self.WithRange(parens.Range);
			return self;
		}

		#endregion

		#region MatchesPattern() and helper methods // Used by replace() macro

		static LNodeFactory F = new LNodeFactory(new EmptySourceFile("LNodeExt.cs"));

		/// <summary>Determines whether one Loyc tree "matches" another. This is 
		/// different from a simple equality test in that (1) trivia atributes do 
		/// not have to match, and (2) the pattern can contain placeholders represented
		/// by calls to $ (the substitution operator) with an identifier as a parameter.
		/// Placeholders match any subtree, and are saved to the <c>captures</c> map.
		/// </summary>
		/// <param name="candidate">A node that you want to compare with a 'pattern'.</param>
		/// <param name="pattern">A syntax tree that may contain placeholders. A 
		/// placeholder is a call to the $ operator with one parameter, which must 
		/// be either (A) a simple identifier, or (B) the ".." operator with a simple
		/// identifier as its single parameter. Otherwise, the $ operator is treated 
		/// literally as something that must exist in <c>candidate</c>). The subtree 
		/// in <c>candidate</c> corresponding to the placeholder is saved in 
		/// <c>captures</c>.</param>
		/// <param name="captures">A table that maps placeholder names from 
		/// <c>pattern</c> to subtrees in <c>candidate</c>. You can set your map to 
		/// null and a map will be created for you if necessary. If you already have
		/// a map, you should clear it before calling this method.</param>
		/// <param name="unmatchedAttrs">On return, a list of trivia attributes in 
		/// <c>candidate</c> that were not present in <c>pattern</c>.</param>
		/// <returns>true if <c>pattern</c> matches <c>candidate</c>, false otherwise.</returns>
		/// <remarks>
		/// Attributes in patterns are not yet supported.
		/// <para/>
		/// This method supports multi-part captures, which are matched to 
		/// placeholders whose identifier either (A) has a #params attribute or
		/// (B) has the unary ".." operator applied to it (for example, if 
		/// the placeholder is called p, this is written as <c>$(params p)</c> in 
		/// EC#.) A placeholder that looks like this can match multiple arguments or
		/// multiple statements in the <c>candidate</c> (or <i>no</i> arguments, or
		/// no statements), and will become a #splice(...) node in <c>captures</c>
		/// if it matches multiple items. Multi-part captures are often useful for
		/// getting lists of statements before and after some required element,
		/// e.g. <c>{ $(params before); MatchThis($something); $(params after); }</c>
		/// <para/>
		/// If the same placeholder appears twice then the two matching items are 
		/// combined into a single output node (calling #splice).
		/// <para/>
		/// If matching is unsuccessful, <c>captures</c> and <c>unmatchedAttrs</c>
		/// may contain irrelevant information gathered during the attempt to match.
		/// <para/>
		/// In EC#, the quote(...) macro can be used to create the LNode object for 
		/// a pattern.
		/// </remarks>
		public static bool MatchesPattern(this LNode candidate, LNode pattern, ref MMap<Symbol, LNode> captures, out VList<LNode> unmatchedAttrs)
		{
			// [$capture] (...)
			if (!AttributesMatch(candidate, pattern, ref captures, out unmatchedAttrs))
				return false;

			// $capture or $(..capture)
			LNode sub = GetCaptureIdentifier(pattern);
			if (sub != null)
			{
				captures = captures ?? new MMap<Symbol, LNode>();
				AddCapture(captures, sub.Name, candidate);
				unmatchedAttrs = VList<LNode>.Empty; // The attrs (if any) were captured
				return true;
			}

			var kind = candidate.Kind;
			if (kind != pattern.Kind)
				return false;

			if (candidate.Name != pattern.Name)
				return false;
			if (kind == LNodeKind.Literal)
				return object.Equals(candidate.Value, pattern.Value);
			else if (kind == LNodeKind.Call) {
				if (!MatchesPatternNested(candidate.Target, pattern.Target, ref captures, ref unmatchedAttrs))
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
						return MatchThenParams(cArgs, pArgs, pArg, ref captures, ref unmatchedAttrs);
					if (cArgs.IsEmpty)
						return false;
					if (!MatchesPatternNested(cArgs.Pop(), pArg, ref captures, ref unmatchedAttrs))
						return false;
				}
				return true;
			} else // kind == Id
				return true;
		}
		public static bool MatchesPattern(this LNode candidate, LNode pattern, out MMap<Symbol, LNode> captures)
		{
			VList<LNode> unmatchedAttrs = VList<LNode>.Empty;
			captures = null;
			return MatchesPattern(candidate, pattern, ref captures, out unmatchedAttrs);
		}

		static void AddCapture(MMap<Symbol, LNode> captures, LNode cap, Slice_<LNode> items)
		{
			LNode capId = GetCaptureIdentifier(cap);
			if (items.Count == 1)
				AddCapture(captures, capId.Name, items[0]);
			else
				AddCapture(captures, capId.Name, F.Call(S.Splice, items));
		}
		static void AddCapture(MMap<Symbol, LNode> captures, Symbol capName, LNode candidate)
		{
			LNode oldCap = captures.TryGetValue(capName, null);
			captures[capName] = LNode.MergeLists(oldCap, candidate, S.Splice);
		}

		static bool MatchesPatternNested(LNode candidate, LNode pattern, ref MMap<Symbol, LNode> captures, ref VList<LNode> trivia)
		{
			VList<LNode> unmatchedAttrs;
			if (!MatchesPattern(candidate, pattern, ref captures, out unmatchedAttrs))
				return false;
			if (unmatchedAttrs.Any(a => !a.IsTrivia))
				return false;
			trivia.AddRange(unmatchedAttrs);
			return true;
		}

		static bool AttributesMatch(LNode candidate, LNode pattern, ref MMap<Symbol, LNode> captures, out VList<LNode> unmatchedAttrs)
		{
			if (pattern.HasPAttrs())
				throw new NotImplementedException("TODO: attributes in patterns are not yet supported");
			unmatchedAttrs = candidate.Attrs;
			return true;
		}
		static bool IsParamsCapture(LNode p)
		{
			return p.Calls(S.Substitute, 1) 
				&& (p.Args.Last.AttrNamed(S.Params) != null || p.Args.Last.Calls(S.DotDot, 1))
				&& GetCaptureIdentifier(p) != null;
		}
		static LNode GetCaptureIdentifier(LNode pattern)
		{
			if (pattern.Calls(S.Substitute, 1)) {
				var arg = pattern.Args.Last;
				if (arg.Calls(S.DotDot, 1))
					arg = arg.Args[0];
				if (arg.IsId)
					return arg;
			}
			return null;
		}

		static bool MatchThenParams(VList<LNode> cArgs, VList<LNode> pArgs, LNode paramsCap, ref MMap<Symbol, LNode> captures, ref VList<LNode> attrs)
		{
			// This helper function of MatchesPattern() is called when pArgs is followed 
			// by a $(params capture). cArgs is the list of candidate.Args that have not 
			// yet been matched; pArgs is the list of pattern.Args that have not yet been 
			// matched, and paramsCap is the $(params capture) node that follows pArgs.
			captures = captures ?? new MMap<Symbol, LNode>();
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

		static bool CaptureGroup(ref int c, ref int p, VList<LNode> cArgs, VList<LNode> pArgs, ref MMap<Symbol, LNode> captures, ref VList<LNode> attrs)
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
