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

		public static LNodeList GetTrivia(this LNode node) { return GetTrivia(node.Attrs); }
		public static LNodeList GetTrivia(this LNodeList attrs)
		{
			var trivia = LNodeList.Empty;
			foreach (var a in attrs)
				if (a.IsTrivia)
					trivia.Add(a);
			return trivia;
		}
		/// <summary>Gets all trailing trivia attached to the specified node.</summary>
		public static LNodeList GetTrailingTrivia(this LNode node) { return GetTrailingTrivia(node.Attrs); }
		/// <summary>Gets all trailing trivia attached to the specified node.</summary>
		/// <remarks>Trailing trivia is represented by a call to <c>%trailing</c> in
		/// a node's attribute list; each argument to %trailing represents one
		/// piece of trivia. If the attribute list has multiple calls to 
		/// %trailing, this method combines those lists into a single list.</remarks>
		public static LNodeList GetTrailingTrivia(this LNodeList attrs)
		{
			var trivia = LNodeList.Empty;
			foreach (var a in attrs)
				if (a.Calls(S.TriviaTrailing))
					trivia.AddRange(a.Args);
			return trivia;
		}
		/// <summary>Removes a node's trailing trivia and adds a new list of trailing trivia.</summary>
		public static LNode WithTrailingTrivia(this LNode node, LNodeList trivia)
		{
			return node.WithAttrs(WithTrailingTrivia(node.Attrs, trivia));
		}
		/// <summary>Removes all existing trailing trivia from an attribute list and adds a new list of trailing trivia.</summary>
		/// <remarks>This method has a side-effect of recreating the %trailing
		/// node, if there is one, at the end of the attribute list. If <c>trivia</c>
		/// is empty then all calls to %trailing are removed.</remarks>
		public static LNodeList WithTrailingTrivia(this LNodeList attrs, LNodeList trivia)
		{
			var attrs2 = WithoutTrailingTrivia(attrs);
			if (trivia.IsEmpty)
				return attrs2;
			return attrs2.Add(LNode.Call(S.TriviaTrailing, trivia));
		}
		/// <summary>Gets a new list with any %trailing attributes removed.</summary>
		public static LNodeList WithoutTrailingTrivia(this LNodeList attrs)
		{
			return attrs.SmartWhere(attr => !attr.Calls(S.TriviaTrailing));
		}
		/// <summary>Gets a new list with any %trailing attributes removed. Trailing 
		/// trivia inside those attributes are returned in an `out` parameter.</summary>
		public static LNodeList WithoutTrailingTrivia(this LNodeList attrs, out LNodeList trailingTrivia)
		{
			trailingTrivia = LNodeList.Empty;
			var attrs2 = attrs.SmartWhere(attr => !attr.Calls(S.TriviaTrailing));
			if (attrs2 != attrs) {
				foreach (var attr in attrs)
					if (attr.Calls(S.TriviaTrailing))
						trailingTrivia.AddRange(attr.Args);
			}
			return attrs2;
		}
		/// <summary>Adds additional trailing trivia to a node.</summary>
		public static LNode PlusTrailingTrivia(this LNode node, LNodeList trivia)
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
		/// Trailing trivia is represented by a call to <c>%trailing</c> in a node's 
		/// attribute list; each argument to %trailing represents one piece of trivia.
		/// <para/>
		/// In the current design, this method has a side-effect of recreating the %trailing
		/// node at the end of the attribute list, and if there are multiple %trailing
		/// lists, consolidating them into a single list, but only if the specified <c>trivia</c> 
		/// list is not empty.</remarks>
		public static LNodeList PlusTrailingTrivia(this LNodeList attrs, LNodeList trivia)
		{
			if (trivia.IsEmpty)
				return attrs;
			LNodeList oldTrivia;
			attrs = WithoutTrailingTrivia(attrs, out oldTrivia);
			return attrs.Add(LNode.Call(S.TriviaTrailing, oldTrivia.AddRange(trivia)));
		}
		/// <summary>Adds additional trailing trivia to an attribute list.</summary>
		public static LNodeList PlusTrailingTrivia(this LNodeList attrs, LNode trivia)
		{
			LNodeList oldTrivia;
			attrs = WithoutTrailingTrivia(attrs, out oldTrivia);
			return attrs.Add(LNode.Call(S.TriviaTrailing, oldTrivia.Add(trivia)));
		}

		#endregion

		/// <summary>Interprets a node as a list by returning <c>block.Args</c> if 
		/// <c>block.Calls(listIdentifier)</c>, otherwise returning a one-item list 
		/// of nodes with <c>block</c> as the only item.</summary>
		public static LNodeList AsList(this LNode block, Symbol listIdentifier) =>
			block.Calls(listIdentifier) ? block.Args : new LNodeList(block);


		/// <summary>Returns <c>node.Args[0]</c> if <c>node.Calls(S.Braces, 1)</c>, otherwise returns <c>node</c> itself.</summary>
		/// <remarks>This is useful for macros that want to allow their arguments 
		/// to use statement syntax, indicated by braces, which will themselves be 
		/// ignored.</remarks>
		public static LNode UnwrapBraces(this LNode node) => node.Calls(S.Braces, 1) ? node.Args[0] : node;

		/// <summary>Returns <c>node.Args[0]</c> if <c>node.Calls(wrapper, 1)</c>, otherwise returns <c>node</c> itself.</summary>
		/// <remarks>This is a more general version of <see cref="UnwrapBraces"/>.</remarks>
		public static LNode Unwrap(this LNode node, Symbol wrapper) => node.Calls(wrapper, 1) ? node.Args[0] : node;

		/// <summary>Converts an expression to a list. Similar to calling 
		/// <c>AsList(block, CodeSymbols.Splice)</c>, if the expression calls #splice
		/// then the arguments of the splice are returned, and if not then the 
		/// argument is converted to a list with one item. However, if the call to 
		/// #splice has attached trivia/attributes, those attributes are attached to
		/// the output list using <see cref="IncludingAttributes(LNodeList, LNodeList)"/>.
		/// </summary>
		/// <param name="node">A node that may or may not be a call to #splice</param>
		/// <returns>A list of nodes that <c>block</c> is equivalent to.</returns>
		/// <remarks>
		/// Attributes attached to #splice are ordinarily attached to the first item 
		/// in the output list, but any %trailing attribute is attached to the last
		/// item instead. If the #splice() call has no arguments, then (i) if it has
		/// no trivia attributes, an empty list is returned, but (ii) if it has 
		/// trivia attributes, the attributes themselves are returned as the content
		/// of the list. This assumes that printers can print 
		/// </remarks>
		public static LNodeList Unsplice(this LNode node)
		{
			var list = node.AsList(S.Splice);
			return list.IncludingAttributes(node.Attrs);
		}

		/// <summary>Finds trivia attributes attached directly to <c>otherNode</c>, and 
		/// returns a new version of <c>node</c> with these attributes added.</summary>
		public static LNode IncludingTriviaFrom(this LNode node, LNode otherNode)
		{
			var trivia = otherNode.GetTrivia();
			if (trivia.IsEmpty)
				return node; // optimize common case
			return node.PlusAttrsBefore(trivia.WithoutTrailingTrivia(out var trailing)).PlusTrailingTrivia(trailing);
		}

		/// <summary>Prepends attributes to the first item in a list, except for
		/// trailing trivia (%trailing(...)), which is appended to the last item in 
		/// the list. If the list is empty, the attributes are ignored.</summary>
		/// <returns>A modified version of the list with attributes added. If the
		/// attribute list is empty, the empty <c>list</c> is returned unchanged.
		/// </returns>
		public static LNodeList IncludingAttributes(this LNodeList list, LNodeList attributes)
		{
			if (attributes.IsEmpty || list.IsEmpty)
				return list;
			attributes = attributes.WithoutTrailingTrivia(out LNodeList trailing);
			if (trailing.Count != 0) {
				if (attributes.Count != 0)
					list[0] = list[0].PlusAttrsBefore(attributes);
				list[list.Count - 1] = list.Last.PlusTrailingTrivia(trailing);
			} else {
				list[0] = list[0].PlusAttrsBefore(attributes);
			}
			return list;
		}

		/// <summary>Converts a list of LNodes to a single LNode by using the list 
		/// as the argument list in a call to the specified identifier, or, if the 
		/// list contains a single item, by returning that single item.</summary>
		/// <param name="listIdentifier">Target of the node that is created if <c>list</c>
		/// does not contain exactly one item. Typical values include "'{}" and "#splice".</param>
		/// <remarks>This is the reverse of the operation performed by <see cref="AsList(LNode,Symbol)"/>.</remarks>
		public static LNode AsLNode(this LNodeList list, Symbol listIdentifier)
		{
			if (list.Count == 1)
				return list[0];
			else {
				var r = SourceRange.Synthetic;
				if (list.Count != 0) {
					r = list[0].Range;
					r = new SourceRange(r.Source, r.StartIndex, list.Last.Range.EndIndex - r.StartIndex);
				}
 				return LNode.Call(listIdentifier, list, r);
			}
		}

		public static LNodeList WithSpliced(this LNodeList list, int index, LNode node, Symbol listName = null)
		{
			if (node.Calls(listName ?? CodeSymbols.Splice))
				return list.InsertRange(index, node.Args);
			else
				return list.Insert(index, node);
		}
		public static LNodeList WithSpliced(this LNodeList list, LNode node, Symbol listName = null)
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
		public static LNodeList Without(this LNodeList list, LNode node)
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
		public static LNodeList WithoutNodeNamed(this LNodeList a, Symbol name)
		{
			LNode _;
			return WithoutNodeNamed(a, name, out _);
		}
		public static LNodeList WithoutNodeNamed(this LNodeList list, Symbol name, out LNode removedNode)
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
		public static int IndexWithName(this LNodeList self, Symbol name, int resultIfNotFound = -1)
		{
			int i = 0;
			foreach (LNode node in self)
				if (node.Name == name)
					return i;
				else
					i++;
			return resultIfNotFound;
		}
		public static LNode NodeNamed(this LNodeList self, Symbol name)
		{
			foreach (LNode node in self)
				if (node.Name == name)
					return node;
			return null;
		}

		public static LNodeList RecursiveReplace(this LNodeList self, Func<LNode, LNodeList?> matcher, LNode.ReplaceOpt options = LNode.ReplaceOpt.Default)
		{
			return self.SmartSelectMany(n =>
			{
				var choice = matcher(n);
				if (choice == null)
					return n.RecursiveReplace(matcher, options);
				else
					return choice;
			});
		}

		#region Parentheses management

		public static bool IsParenthesizedExpr(this LNode node)
		{
			return node.AttrNamed(CodeSymbols.TriviaInParens) != null;
		}

		/// <summary>Returns the same node with a parentheses attribute added.</summary>
		public static LNode InParens(this LNode node)
		{
			return node.PlusAttrBefore(LNode.Id(CodeSymbols.TriviaInParens));
		}
		/// <summary>Returns the same node with a parentheses attribute added.</summary>
		/// <remarks>The node's range is changed to the provided <see cref="SourceRange"/>.</remarks>
		public static LNode InParens(this LNode node, SourceRange range)
		{
			return node.WithRange(range).PlusAttrBefore(LNode.Id(CodeSymbols.TriviaInParens));
		}
		/// <summary>Returns the same node with a parentheses attribute added.</summary>
		public static LNode InParens(this LNode node, ISourceFile file, int startIndex, int endIndex)
		{
            return InParens(node, new SourceRange(file, startIndex, endIndex - startIndex));
		}
		/// <summary>Removes a single pair of parentheses, if the node has a 
		/// %inParens attribute. Returns the same node when no parens are 
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
		///   placeholder is a call to the $ operator with one parameter, which must 
		///   be either (A) a simple identifier, or (B) the ".." operator with a simple
		///   identifier as its single parameter. Otherwise, the $ operator is treated 
		///   literally as something that must exist in <c>candidate</c>). The subtree 
		///   in <c>candidate</c> corresponding to the placeholder is saved in 
		///   <c>captures</c>.</param>
		/// <param name="captures">A table that maps placeholder names from 
		///   <c>pattern</c> to subtrees in <c>candidate</c>. You can set your map to 
		///   null and a map will be created for you if necessary. If you already have
		///   a map, you should clear it before calling this method.</param>
		/// <param name="unmatchedAttrs">On return, a list of trivia attributes in 
		///   <c>candidate</c> that were not present in <c>pattern</c>.</param>
		/// <param name="gatherDuplicatesInList">This parameter controls the behavior
		///   of this method in case the pattern repeats the same $placeholder more
		///   than once. If this is true, the old behavior is used: the two or more
		///   matched subtrees are combined into a single output node (calling #splice).
		///   For example, matching `$x + $x` against `1 + 2` would succeed, assigning
		///   `#splice(1, 2)` to `x` in <c>captures</c>. If this is false, the default
		///   behavior is used: the two or more matched subtrees must be equal, and
		///   only the first matching subtree is saved in <c>captures</c>. For example,
		///   matching `$x + $x` against `1 + 2` fails.</param>
		/// <returns>true if <c>pattern</c> matches <c>candidate</c>, false otherwise.</returns>
		/// <remarks>
		/// Attributes in patterns are not yet supported.
		/// <para/>
		/// This method supports multi-part captures, which are matched to 
		/// placeholders whose identifier either (A) has a #params attribute or
		/// (B) has the unary ".." or "..." operator applied to it (for example, if 
		/// the placeholder is called p, this is written as <c>$(...p)</c> in 
		/// EC#.) A placeholder that looks like this can match multiple arguments or
		/// multiple statements in the <c>candidate</c> (or <i>no</i> arguments, or
		/// no statements), and will become a #splice(...) node in <c>captures</c>
		/// if it matches multiple items. Multi-part captures are often useful for
		/// getting lists of statements before and after some required element,
		/// e.g. <c>{ $(...before); MatchThis($something); $(...after); }</c>
		/// <para/>
		/// If matching is unsuccessful, <c>captures</c> and <c>unmatchedAttrs</c>
		/// may contain irrelevant information gathered during the attempt to match.
		/// <para/>
		/// In EC#, the quote(...) macro can be used to create the LNode object for 
		/// a pattern.
		/// </remarks>
		public static bool MatchesPattern(this LNode candidate, LNode pattern, ref MMap<Symbol, LNode> captures, out LNodeList unmatchedAttrs, bool gatherDuplicatesInList = false)
		{
			// [$capture] (...)
			if (!AttributesMatch(candidate, pattern, ref captures, out unmatchedAttrs, gatherDuplicatesInList))
				return false;

			// $capture or $(..capture)
			LNode sub = GetCaptureIdentifier(pattern);
			if (sub != null)
			{
				captures = captures ?? new MMap<Symbol, LNode>();
				if (!AddCapture(captures, sub.Name, candidate, gatherDuplicatesInList))
					return false;
				unmatchedAttrs = LNodeList.Empty; // The attrs (if any) were captured
				return true;
			}

			var kind = candidate.Kind;
			if (kind != pattern.Kind)
				return false;

			if (kind == LNodeKind.Id && candidate.Name != pattern.Name)
				return false;
			if (kind == LNodeKind.Literal)
				return object.Equals(candidate.Value, pattern.Value);
			else if (kind == LNodeKind.Call)
			{
				if (!MatchesPatternNested(candidate.Target, pattern.Target, ref captures, ref unmatchedAttrs, gatherDuplicatesInList))
					return false;
				var cArgs = candidate.Args;
				var pArgs = pattern.Args;

				return ListMatches(cArgs, pArgs, ref captures, ref unmatchedAttrs, gatherDuplicatesInList);
			}
			else // kind == Id
				return true;
		}

		/// <inheritdoc cref="MatchesPattern(LNode, LNode, ref MMap{Symbol, LNode}, out LNodeList, bool)"/>
		public static bool MatchesPattern(this LNode candidate, LNode pattern, out IDictionary<Symbol, LNode> captures, out LNodeList unmatchedAttrs, bool gatherDuplicatesInList = false)
		{
			MMap<Symbol, LNode> captures2 = null;
			var matched = MatchesPattern(candidate, pattern, ref captures2, out unmatchedAttrs, gatherDuplicatesInList);
			captures = captures2;
			return matched;
		}

		/// <inheritdoc cref="MatchesPattern(LNode, LNode, ref MMap{Symbol, LNode}, out LNodeList, bool)"/>
		public static bool MatchesPattern(this LNode candidate, LNode pattern, out IDictionary<Symbol, LNode> captures, bool gatherDuplicatesInList = false)
		{
			return MatchesPattern(candidate, pattern, out captures, out var _, gatherDuplicatesInList);
		}

		private static bool ListMatches(LNodeList candidates, LNodeList patterns, ref MMap<Symbol, LNode> captures, ref LNodeList unmatchedAttrs, bool gatherDuplicatesInList)
		{
			if (patterns.Count != candidates.Count && !patterns.Any(IsParamsCapture))
				return false;

			// Scan from the end of the list to the beginning (RVLists is good at this),
			// matching args one-by-one. Use MatchThenParams() in case of $(params capture).
			while (!patterns.IsEmpty)
			{
				LNode pArg = patterns.Pop();
				if (IsParamsCapture(pArg))
					return MatchThenParams(candidates, patterns, pArg, ref captures, ref unmatchedAttrs, gatherDuplicatesInList);
				if (candidates.IsEmpty)
					return false;
				if (!MatchesPatternNested(candidates.Pop(), pArg, ref captures, ref unmatchedAttrs, gatherDuplicatesInList))
					return false;
			}
			return true;
		}

		static bool AddCapture(MMap<Symbol, LNode> captures, LNode cap, Slice_<LNode> items, bool gatherDuplicatesInList)
		{
			LNode capId = GetCaptureIdentifier(cap);
			if (items.Count == 1)
				return AddCapture(captures, capId.Name, items[0], gatherDuplicatesInList);
			else
				return AddCapture(captures, capId.Name, F.Call(S.Splice, items), gatherDuplicatesInList);
		}

		static readonly Symbol __ = (Symbol)"_";

		static bool AddCapture(MMap<Symbol, LNode> captures, Symbol capName, LNode candidate, bool gatherDuplicatesInList)
		{
			LNode oldCap = captures.TryGetValue(capName, null);
			if (oldCap == null || gatherDuplicatesInList || capName == __) {
				captures[capName] = LNode.MergeLists(oldCap, candidate, S.Splice);
			} else {
				if (!LNode.Equals(oldCap, candidate, LNode.CompareMode.IgnoreTrivia))
					return false;
			}
			return true;
		}

		static bool MatchesPatternNested(LNode candidate, LNode pattern, ref MMap<Symbol, LNode> captures, ref LNodeList trivia, bool gatherDuplicatesInList)
		{
			LNodeList unmatchedAttrs;
			if (!MatchesPattern(candidate, pattern, ref captures, out unmatchedAttrs, gatherDuplicatesInList))
				return false;
			if (unmatchedAttrs.Any(a => !a.IsTrivia))
				return false;
			trivia.AddRange(unmatchedAttrs);
			return true;
		}

		static bool AttributesMatch(LNode candidate, LNode pattern, ref MMap<Symbol, LNode> captures, out LNodeList unmatchedAttrs, bool gatherDuplicatesInList)
		{
			if (pattern.HasPAttrs()) {
				unmatchedAttrs = LNode.List();
				return ListMatches(candidate.Attrs, pattern.Attrs, ref captures, ref unmatchedAttrs, gatherDuplicatesInList);
			} else {
				unmatchedAttrs = candidate.Attrs;
			}
			return true;
		}
		static bool IsParamsCapture(LNode pattern)
		{
			if (pattern.Calls(S.Substitute, 1)) {
				LNode arg = pattern.Args.Last;
				return (arg.Calls(S.DotDot, 1) || arg.Calls(S.DotDotDot, 1) || arg.AttrNamed(S.Params) != null)
					&& GetCaptureIdentifier(pattern) != null;
			}
			return false;
		}
		/// <summary>Checks if <c>pattern</c> matches one of the syntax trees 
		/// <c>$x</c> or <c>$(..x)</c> or <c>$(...x)</c> for some identifier <c>x</c>.
		/// These are conventionally used to represent partial syntax trees.</summary>
		/// <returns>The matched identifier (<c>x</c> in the examples above), or null 
		/// if <c>pattern</c> was not a match.</returns>
		public static LNode GetCaptureIdentifier(LNode pattern, bool identifierRequired = true)
		{
			if (pattern.Calls(S.Substitute, 1)) {
				var arg = pattern.Args.Last;
				if (arg.Calls(S.DotDot, 1) || arg.Calls(S.DotDotDot, 1))
					arg = arg.Args[0];
				if (arg.IsId || !identifierRequired)
					return arg;
			}
			return null;
		}

		static bool MatchThenParams(LNodeList cArgs, LNodeList pArgs, LNode paramsCap, ref MMap<Symbol, LNode> captures, ref LNodeList attrs, bool gatherDuplicatesInList)
		{
			// This helper function of MatchesPattern() is called when pArgs is followed 
			// by a $(...listCapture). cArgs is the list of candidate.Args that have not 
			// yet been matched; pArgs is the list of pattern.Args that have not yet been 
			// matched, and paramsCap is the $(params capture) node that follows pArgs.
			captures = captures ?? new MMap<Symbol, LNode>();
			int c = 0, p = 0;
		restart:
			for (; p < pArgs.Count; p++, c++) {
				if (IsParamsCapture(pArgs[p])) {
					if (!CaptureGroup(ref c, ref p, cArgs, pArgs, ref captures, ref attrs, gatherDuplicatesInList))
						return false;
					goto restart;
				} else {
					if (c >= cArgs.Count)
						return false;
					if (!MatchesPatternNested(cArgs[c], pArgs[p], ref captures, ref attrs, gatherDuplicatesInList))
						return false;
				}
			}
			return AddCapture(captures, paramsCap, new Slice_<LNode>(cArgs, c), gatherDuplicatesInList);
		}

		static bool CaptureGroup(ref int c, ref int p, LNodeList cArgs, LNodeList pArgs, ref MMap<Symbol, LNode> captures, ref LNodeList attrs, bool gatherDuplicatesInList)
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
						if (!MatchesPatternNested(cArgs[c], pArgs[p], ref captures, ref attrs, gatherDuplicatesInList))
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
			return AddCapture(captures, pArgs[saved_p], cArgs.Slice(saved_c, captureSize), gatherDuplicatesInList);
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

		public static NodeStyle BaseStyle(this ILNode node) { return node.Style & NodeStyle.BaseStyleMask; }

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

		public static ILNode AttrNamed(this ILNode node, Symbol name)
		{
			return node.Attrs().NodeNamed(name);
		}
		public static ILNode NodeNamed(this NegListSlice<ILNode> self, Symbol name)
		{
			foreach (var node in self)
				if (node.Name == name)
					return node;
			return null;
		}
		public static IListSource<ILNode> GetTrailingTrivia(this ILNode node)
		{
			if (node is LNode) {
				LNodeList list = GetTrailingTrivia((LNode)node);
				if (list.IsEmpty)
					return EmptyList<ILNode>.Value; // avoid boxing in the common case
				return list;
			} else {
				VList<ILNode> list = VList<ILNode>.Empty;
				foreach (ILNode a in node.Attrs()) {
					if (a.Calls(S.TriviaTrailing))
						list.AddRange(a.Args());
				}
				if (list.IsEmpty)
					return EmptyList<ILNode>.Value; // avoid boxing in the common case
				return list;
			}
		}

		/// <summary>Converts <see cref="ILNode"/> to <see cref="LNode"/> recursively.</summary>
		public static LNode ToLNode(this ILNode node)
		{
			if (node is LNode n)
				return n;

			var attrs = LNodeList.Empty;
			for (int i = node.Min; i < -1; i++)
				attrs.Add(ToLNode(node[i]));

			switch (node.Kind) {
				case LNodeKind.Id:
					return LNode.Id(attrs, node.Name, new SourceRange(node.Range), node.Style);
				case LNodeKind.Literal:
					return LNode.Literal(attrs, node.Value, new SourceRange(node.Range), node.Style);
				default:
					var args = LNodeList.Empty;
					for (int i = 0, max = node.Max; i <= max; i++)
						args.Add(ToLNode(node[i]));
					var target = ToLNode(node.Target);
					return LNode.Call(attrs, target, args, new SourceRange(node.Range), node.Style);
			}
		}

		#endregion

		#region Backward compatibility with VList<LNode>

		public static VList<LNode> ToVList(this LNodeList list) => list;
		public static FVList<LNode> ToFVList(this LNodeList list) => list.ToVList().ToFVList();
		public static WList<LNode> ToWList(this LNodeList list) => list.ToVList().ToWList();
		public static LNodeList ToLNodeList(this WList<LNode> list) => new LNodeList(list.ToVList());
		public static LNodeList ToLNodeList(this VList<LNode> list) => new LNodeList(list);

		#endregion

		// Workarounds: even though LNode includes ITryGet<int, LNode>, C# in 2020 is 
		// too stupid to figure out how to call the overload for ITryGet<K, V>.
		public static Maybe<LNode> TryGet(this LNode self, int key) => TryGetExt.TryGet((ITryGet<int, LNode>)self, key);
		public static LNode TryGet(this LNode self, int key, LNode defaultValue) => TryGetExt.TryGet((ITryGet<int, LNode>)self, key, defaultValue);
	}
}
