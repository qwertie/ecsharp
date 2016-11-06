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
		#region Trivia management

		public static VList<LNode> GetTrivia(this LNode node) { return GetTrivia(node.Attrs); }
		public static VList<LNode> GetTrivia(this VList<LNode> attrs)
		{
			var trivia = VList<LNode>.Empty;
			foreach (var a in attrs)
				if (a.IsTrivia)
					trivia.Add(a);
			return trivia;
		}
		/// <summary>Gets all trailing trivia attached to the specified node.</summary>
		public static VList<LNode> GetTrailingTrivia(this LNode node) { return GetTrailingTrivia(node.Attrs); }
		/// <summary>Gets all trailing trivia attached to the specified node.</summary>
		/// <remarks>Trailing trivia is represented by a call to #trivia_trailing in
		/// a node's attribute list; each argument to #trivia_trailing represents one
		/// piece of trivia. If the attribute list has multiple calls to 
		/// #trivia_trailing, this method combines those lists into a single list.</remarks>
		public static VList<LNode> GetTrailingTrivia(this VList<LNode> attrs)
		{
			var trivia = VList<LNode>.Empty;
			foreach (var a in attrs)
				if (a.Calls(S.TriviaTrailing))
					trivia.AddRange(a.Args);
			return trivia;
		}
		/// <summary>Removes a node's trailing trivia and adds a new list of trailing trivia.</summary>
		public static LNode WithTrailingTrivia(this LNode node, VList<LNode> trivia)
		{
			return node.WithAttrs(WithTrailingTrivia(node.Attrs, trivia));
		}
		/// <summary>Removes all existing trailing trivia from an attribute list and adds a new list of trailing trivia.</summary>
		/// <remarks>This method has a side-effect of recreating the #trivia_trailing
		/// node, if there is one, at the end of the attribute list. If <c>trivia</c>
		/// is empty then all calls to #trivia_trailing are removed.</remarks>
		public static VList<LNode> WithTrailingTrivia(this VList<LNode> attrs, VList<LNode> trivia)
		{
			var attrs2 = WithoutTrailingTrivia(attrs);
			if (trivia.IsEmpty)
				return attrs2;
			return attrs2.Add(LNode.Call(S.TriviaTrailing, trivia));
		}
		/// <summary>Gets a new list with any #trivia_trailing attributes removed.</summary>
		public static VList<LNode> WithoutTrailingTrivia(this VList<LNode> attrs)
		{
			return attrs.Transform((int i, ref LNode attr) => attr.Calls(S.TriviaTrailing) ? XfAction.Drop : XfAction.Keep);
		}
		/// <summary>Gets a new list with any #trivia_trailing attributes removed. Those trivia are returned in an `out` parameter.</summary>
		public static VList<LNode> WithoutTrailingTrivia(this VList<LNode> attrs, out VList<LNode> trailingTrivia)
		{
			var trailingTrivia2 = VList<LNode>.Empty;
			attrs = attrs.Transform((int i, ref LNode attr) => {
				if (attr.Calls(S.TriviaTrailing)) {
					trailingTrivia2.AddRange(attr.Args);
					return XfAction.Drop;
				}
				return XfAction.Keep;
			});
			trailingTrivia = trailingTrivia2; // cannot use `out` parameter within lambda method
			return attrs;
		}
		/// <summary>Adds additional trailing trivia to a node.</summary>
		public static LNode PlusTrailingTrivia(this LNode node, VList<LNode> trivia)
		{
			return node.WithAttrs(PlusTrailingTrivia(node.Attrs, trivia));
		}
		/// <summary>Adds additional trailing trivia to a node.</summary>
		public static LNode PlusTrailingTrivia(this LNode node, LNode trivia)
		{
			return node.WithAttrs(PlusTrailingTrivia(node.Attrs, trivia));
		}
		/// <summary>Adds additional trailing trivia to an attribute list. Has no effect if <c>trivia</c> is empty.</summary>
		/// <remarks>
		/// Trailing trivia is represented by a call to #trivia_trailing in a node's 
		/// attribute list; each argument to #trivia_trailing represents one piece of 
		/// trivia.
		/// <para/>
		/// In the current design, this method has a side-effect of recreating the #trivia_trailing
		/// node at the end of the attribute list, and if there are multiple #trivia_trailing
		/// lists, consolidating them into a single list, but only if the specified <c>trivia</c> 
		/// list is not empty.</remarks>
		public static VList<LNode> PlusTrailingTrivia(this VList<LNode> attrs, VList<LNode> trivia)
		{
			if (trivia.IsEmpty)
				return attrs;
			VList<LNode> oldTrivia;
			attrs = WithoutTrailingTrivia(attrs, out oldTrivia);
			return attrs.Add(LNode.Call(S.TriviaTrailing, oldTrivia.AddRange(trivia)));
		}
		/// <summary>Adds additional trailing trivia to an attribute list.</summary>
		public static VList<LNode> PlusTrailingTrivia(this VList<LNode> attrs, LNode trivia)
		{
			VList<LNode> oldTrivia;
			attrs = WithoutTrailingTrivia(attrs, out oldTrivia);
			return attrs.Add(LNode.Call(S.TriviaTrailing, oldTrivia.Add(trivia)));
		}

		#endregion

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
		public static int IndexWithName(this VList<LNode> self, Symbol name, int resultIfNotFound = -1)
		{
			int i = 0;
			foreach (LNode node in self)
				if (node.Name == name)
					return i;
				else
					i++;
			return resultIfNotFound;
		}
		public static LNode NodeNamed(this VList<LNode> self, Symbol name)
		{
			foreach (LNode node in self)
				if (node.Name == name)
					return node;
			return null;
		}

		#region Parentheses management

		public static bool IsParenthesizedExpr(this LNode node)
		{
			return node.AttrNamed(CodeSymbols.TriviaInParens) != null;
		}

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
		public static bool MatchesPattern(this LNode candidate, LNode pattern, out IDictionary<Symbol, LNode> captures, out VList<LNode> unmatchedAttrs)
		{
			MMap<Symbol, LNode> captures2 = null;
			var matched = MatchesPattern(candidate, pattern, ref captures2, out unmatchedAttrs);
			captures = captures2;
			return matched;
		}
		public static bool MatchesPattern(this LNode candidate, LNode pattern, out IDictionary<Symbol, LNode> captures)
		{
			VList<LNode> unmatchedAttrs;
			return MatchesPattern(candidate, pattern, out captures, out unmatchedAttrs);
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

		#region ILNode extensions

		public static bool IsCall(this ILNode node) { return node.Kind == LNodeKind.Call; }
		public static bool IsId(this ILNode node) { return node.Kind == LNodeKind.Id; }
		public static bool IsLiteral(this ILNode node) { return node.Kind == LNodeKind.Literal; }

		public static int ArgCount(this ILNode node) { return node.Max + 1; }
		public static int AttrCount(this ILNode node) { return -node.Min - 1; }

		public static NegListSlice<ILNode> Attrs(this ILNode node)
		{
			int min = node.Min;
			return new NegListSlice<ILNode>(node, min, -1 - min);
		}
		public static NegListSlice<ILNode> Args(this ILNode node)
		{
			return new NegListSlice<ILNode>(node, 0, System.Math.Max(node.Max + 1, 0));
		}

		public static bool IsTrivia(this ILNode node)
		{
			return CodeSymbols.IsTriviaSymbol(node.Name);
		}

		public static bool IsIdNamed(this ILNode node, Symbol name) { return node.Name == name; }
		public static bool IsIdNamed(this ILNode node, string name)
		{
			var nn = node.Name;
			return nn == null ? name == null : nn.Name == name;
		}

		public static bool Calls(this ILNode node, Symbol name) { return node.CallsMin(name, 0); }

		public static bool IsParenthesizedExpr(this ILNode node)
		{
			return node.Attrs().NodeNamed(CodeSymbols.TriviaInParens) != null;
		}

		public static bool HasSpecialName(this ILNode node) { return LNode.IsSpecialName(node.Name); }

		public static bool HasAttrs(this ILNode node)
		{
			return node.Min < -1;
		}
		public static bool HasPAttrs(this ILNode node)
		{
			for (int i = node.Min; i < -1; i++)
				if (!node[i].IsTrivia())
					return true;
			return false;
		}

		public static LNode NodeNamed(this NegListSlice<ILNode> self, Symbol name)
		{
			foreach (LNode node in self)
				if (node.Name == name)
					return node;
			return null;
		}

		#endregion
	}
}
