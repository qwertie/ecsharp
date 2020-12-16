using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Collections.Impl;

namespace Loyc.Syntax
{
	/// <summary>Encapsulates an algorithm that consumes trivia (comments and 
	/// newlines) from a list and adds it as trivia attributes into LNodes.</summary>
	/// <remarks>
	/// Call <see cref="Run"/> to invoke the algorithm. One must also write a
	/// derived class that knows how to interpret the trivia and associate it with
	/// a specific <see cref="LNode"/>, or use the standard derived class 
	/// <see cref="StandardTriviaInjector"/>.
	/// <para/>
	/// The algorithm is designed to postprocess output from a parser that works in
	/// a typical way. The lexer for your language needs to follow the following rules:
	/// 
	/// <ul>
	/// <li>It must not include the newline character in the range of a single-line 
	/// comment.</li>
	/// <li>Generally, newlines (including the newline after a single-line comment)
	/// should be included in the trivia list, as the algorithm relies on newline trivia
	/// to notice when a new line is starting. Notably, a comment/trivia right after a 
	/// statement (e.g. on the same line) should normally be associated with that
	/// statement; if this class is unaware of the newline it will be associated with
	/// the next statement instead.</li>
	/// </ul>
	/// 
	/// Typically one will wrap the lexer in <see cref="Lexing.TriviaSaver"/>, which 
	/// saves trivia while filtering out whitespace so that the parser doesen't see it.
	/// <para/>
	/// Your language's parser needs to follow the following rules:
	/// 
	/// <ul>
	/// <li>The parser should assign minimal boundaries to each node: the 
	/// <see cref="LNode.Range"/> should not be wider than necessary. If there is 
	/// a comment before an expression like <c>/* ! * / x + y</c>, the parser should 
	/// not include the comment as part of the range unless it wants the comment to 
	/// be associated with a child node (<c>x</c>) instead of with the entire 
	/// expression (<c>x + y</c>).</li>
	/// <li>However, if a node has normal (non-trivia) attributes attached to it, 
	/// the Range of the node must include those attributes. If your parser fails 
	/// to do this, one symptom can be that a newline after an attribute "moves up" 
	/// in the attribute list so it appears before the attribute.</li>
	/// <li>The parser should use <see cref="LNodeFactory.InParens"/> to place a
	/// node in parentheses and include the index of the opening and closing parens.</li>
	/// <li>If an expression/statement is terminated by a semicolon/comma, it's 
	/// best (but not crucial) to include the semicolon/comma in the LNode.Range, 
	/// but if a statement is terminated by a newline, the newline should not be 
	/// included in the range. If general, when the terminator is included in the 
	/// range, then any comment that appears before the terminator is attached
	/// to the final child node rather than to the node as a whole. (e.g. in 
	/// <c>x = y /*y* /;</c>, the comment is associated with <c>y</c> if the
	/// comment is within the range of the statement as a whole.)</li>
	/// </ul>
	/// This example shows how comments are associated with nodes:
	/// [NOTE: the space in "* /" is a workaround for a serious bug in Doxygen, the html doc generator]
	/// <pre>
	/// // Comment attached to block
	/// {
	///   // Comment attached to Foo() call
	///   Foo(
	///		// Comment attached to «argument1 + 1». Note that the comma is 
	///		// generally invisible to the injector since it is not part of the LNode 
	///		// tree, so the 1st and 2nd comments will both be attached to the sum-
	///		// expression as a whole.
	///		argument1 + 1 /*1st comment* /, // 2nd comment
	///		// Comment attached to «argument2 + 2»
	///		argument2 + 2); // Comment attached to Foo() call
	///	  Area = 3.14159265/*PI* /  *  /*radius* /r**2;
	///	  // Comment attached to «Area =» statement, preceded by newline trivia?
	///
	///	  // Comment attached to Bar()
	///	  @BarAttr // Comment attached to BarAttr attribute
	///	  Bar();
	///	  
	///	  // Comment attached to Bar() because there are no statements afterward
	/// }
	/// </pre>
	/// When there is a single newline between two nodes, <see cref="Run"/> will
	/// associate it with the second one. When there is a blank line between two 
	/// nodes (two newlines), the first newline (and any before) is associated with 
	/// the first node and the second newline (and any following) is associated with 
	/// the second.
	/// </remarks>
	public abstract class AbstractTriviaInjector<Trivia>
	{
		/// <summary>List of trivia to be injected by <see cref="Run"/>. Must be sorted.</summary>
		public IListSource<Trivia> SortedTrivia { get; set; }
		
		/// <summary>Index of next trivia to be injected.</summary>
		protected int NextIndex { get; set; }

		/// <summary>Initializes the <see cref="SortedTrivia"/> property.</summary>
		public AbstractTriviaInjector(IListSource<Trivia> sortedTrivia) { SortedTrivia = sortedTrivia; }

		/// <summary>Derived class translates a list of trivia (tokens) into appropriate 
		/// trivia attributes. This will be called for leading trivia before trailing 
		/// trivia.</summary>
		/// <param name="node">The node.</param>
		/// <param name="trivia">Trivia to be associated with <c>node</c>.</param>
		/// <param name="loc">Location of the trivia. For a given node, the base class
		/// calls this method at most once for each value of <see cref="TriviaLocation"/>.
		/// </param>
		/// <param name="parent">(Original version of) parent of <c>node</c>.</param>
		/// <param name="indexInParent">Index of <c>node</c> within <c>parent</c>.</param>
		/// <returns>The same node with trivia attributes added. If loc indicates trailing
		/// trivia, the derived class can say "I don't want trivia to be associated with 
		/// this node" by returning null; the base class will, if possible, associate it 
		/// with the next node instead, but this doesn't work for the last child in a 
		/// sequence; in that case this method is called again with the same trivia and
		/// loc is set to TriviaLocation.TrailingExtra.</returns>
		/// <remarks>This method may STILL called for a given node when there is no trivia 
		/// associated with that node, IF the node is at the top level or its sibling 
		/// nodes in the same parent have associated trivia.</remarks>
		protected abstract LNodeList GetAttachedTrivia(LNode node, IListSource<Trivia> trivia, TriviaLocation loc, LNode parent, int indexInParent);

		private LNode AttachTriviaTo(LNode node, IListSource<Trivia> trivia, TriviaLocation loc, LNode parent, int indexInParent)
		{
			var newAttrs = GetAttachedTrivia(node, trivia, loc, parent, indexInParent);
			if (loc == TriviaLocation.Leading)
				return node.PlusAttrsBefore(newAttrs);
			else
				return node.PlusTrailingTrivia(newAttrs);
		}

		protected enum TriviaLocation {
			/// <summary>Trivia that appeared before a node</summary>
			Leading = 0,
			/// <summary>Trivia associated with a node that came before it</summary>
			Trailing = 1,
			/// <summary>Trivia attached to the last node in an argument list or last statement in a block</summary>
			TrailingExtra = 2,
			/// <summary>The trivia begins within the range of an identifier or literal. 
			/// This occurs, for example, if a list of arguments or attributes includes a
			/// comma in the range of each argument, and there is trivia before the comma,
			/// e.g. in <c>Foo(x/* trivia * /, y);</c>, this occurs if the range of <c>x</c> 
			/// includes the comma token. The trivia injector is not aware of <c>x</c> or
			/// the comma; all it knows is that the trivia is "inside" the range of <c>x</c>.</summary>
			Ambiguous = 3,
		}

		/// <summary>This method is called when a node has no newlines or comments within it
		/// (although the node may still have a leading or trailing comment). It informs the
		/// derived class that <see cref="AbstractTriviaInjector{Trivia}"/> will not traverse 
		/// into the node.</summary>
		/// <remarks>The default implementation sets the <see cref="NodeStyle.OneLiner"/> 
		/// style flag.</remarks>
		protected virtual void MarkOneLiner(ref LNode node)
		{
			node.Style |= NodeStyle.OneLiner;
		}

		/// <summary>Gets the <see cref="SourceRange"/> for an element of trivia.</summary>
		protected abstract SourceRange GetRange(Trivia trivia);

		/// <summary>Returns true if the trivia represents a newline, false otherwise.</summary>
		protected abstract bool IsNewline(Trivia trivia);

		/// <summary>A method called to create a virtual node, in order to apply trivia to a 
		/// source file that is completely empty except for trivia.</summary>
		/// <remarks>Default implementation attaches all trivia to a "missing" node 
		/// (zero-length identifier). If this method returns null, the source file will truly 
		/// be empty (containing no trivia either).</remarks>
		protected virtual LNode GetEmptyResultSet()
		{
			if (SortedTrivia.Count == 0)
				return null;
			var dummy = LNode.Id(LNode.List(LNode.Id(CodeSymbols.TriviaDummyNode)), GSymbol.Empty, GetRange(SortedTrivia[0]));
			return AttachTriviaTo(dummy, SortedTrivia, TriviaLocation.TrailingExtra, null, -1) ?? dummy;
		}

		/// <summary>This method is called after a node has been processed and any 
		/// applicable trivia was attached.</summary>
		/// <param name="node">Node (after trivia attached)</param>
		/// <param name="parent">Parent of <c>node</c> (old version, before changes to children are applied)</param>
		/// <param name="indexInParent">Index of <c>node</c> within <c>parent</c>.</param>
		/// <returns>Should return <c>node</c> or an altered version of <c>node</c>.</returns>
		/// <remarks>This method gives the derived class one final chance to 
		/// rearrange or alter the interpretation of the attached trivia. Note
		/// that this method may be called on some nodes to which trivia was
		/// not attached, when siblings of the same parent had trivia attached.
		/// </remarks>
		protected virtual LNode DoneAttaching(LNode node, LNode parent, int indexInParent) { return node; }

		/// <summary>Attaches trivia to the input nodes provided.</summary>
		/// <param name="nodes">List of input nodes. This method calls nodes.MoveNext()
		/// before calling nodes.Current.</param>
		/// <remarks>Trailing trivia after all nodes is attached to the final node. If 
		/// <c>nodes</c> is empty then <see cref="GetEmptyResultSet"/> is called to
		/// create a dummy node and attach all trivia to it.</remarks>
		public IEnumerator<LNode> Run(IEnumerator<LNode> nodes)
		{
			NextIndex = 0;
			var e = RunCore(WithIndexes(nodes), null);
			if (e.MoveNext()) {
				LNode next = e.Current.A;
				while (e.MoveNext()) {
					yield return next;
					next = e.Current.A;
				}
				// Associate remaining trivia with final node
				if (NextIndex < SortedTrivia.Count)
					next = AttachTriviaTo(next, SortedTrivia.Slice(NextIndex), TriviaLocation.TrailingExtra, null, int.MaxValue) ?? next;
				yield return next;
			} else {
				// No nodes exist
				var result = GetEmptyResultSet();
				if (result != null)
					yield return result;
			}
		}

		protected static IEnumerator<Pair<T, int>> WithIndexes<T>(IEnumerator<T> e)
		{
			int i = 0;
			while (e.MoveNext()) {
				yield return new Pair<T, int>(e.Current, i);
				i++;
			}
		}

		/// <summary>Core trivia associaton algorithm.</summary>
		/// <remarks>
		/// NOTE: the enumerator may DRIVE lexing and actually cause the trivia list
		/// (SortedTrivia) to increase in size. For this reason, this algorithm is careful
		/// to call nodes.MoveNext() BEFORE getting the current trivia. I'm not sure if this
		/// precaution is sufficient to preserve trivia in all "streaming" cases, but it
		/// has worked fine up to now.
		/// </remarks>
		protected IEnumerator<Pair<LNode, int>> RunCore(IEnumerator<Pair<LNode, int>> nodes, LNode parent)
		{
			SourceRange triviaRange;
			Maybe<Trivia> trivia = NoValue.Value;
			InternalList<Trivia> triviaList = InternalList<Trivia>.Empty;
			
			int prevIndexInParent = int.MinValue, indexInParent;
			LNode node, prev;
			for (prev = null; nodes.MoveNext(); prev = node, prevIndexInParent = indexInParent)
			{
				Debug.Assert(triviaList.IsEmpty);
				var current = nodes.Current;
				node = current.Item1;
				indexInParent = current.Item2;

				if ((trivia = CurrentTrivia(out triviaRange)).HasValue) {
					// Get a list of trivia that appears before the current node. The tricky part 
					// of this is that we sometimes want to associate some comments with the
					// previous node instead of the current node. This is done when...
					// (1) a comment appears on the same line as the previous node AND the
					//     current node is on a different line, e.g. Kill(p); /* end process */
					// (2) a comment appears on the next line after the previous node AND
					//     there is a blank line between that comment and the current node
					// ...however, don't do this between attributes, or between the last 
					// attribute and the target, because in that case we want to yield the 
					// trivia into the attribute list of the parent using the special logic 
					// below.
					bool canAssociateWithPrev = prev != null && !(prevIndexInParent < -1 && indexInParent <= -1);
					int firstNewlineAt = -1;
					while (triviaRange.StartIndex < node.Range.StartIndex) {
						if (canAssociateWithPrev && IsNewline(trivia.Value)) {
							if (firstNewlineAt == -1) {
								firstNewlineAt = triviaList.Count;
							} else if (!triviaList.IsEmpty && IsNewline(triviaList.Last)) {
								// case (2)
								TryAttachTriviaTo(ref prev, ref triviaList, TriviaLocation.Trailing, parent, prevIndexInParent);
								yield return YieldPrev(ref prev, parent, prevIndexInParent);
								canAssociateWithPrev = false; // because prev == null now
							}
						}
						triviaList.Add(trivia.Value);
						if (!(trivia = AdvanceTrivia(out triviaRange)).HasValue)
							break;
					}
					if (firstNewlineAt > 0 && canAssociateWithPrev) {
						// case (1)
						var triviaList2 = new InternalList<Trivia>(triviaList.Take(firstNewlineAt));
						if (TryAttachTriviaTo(ref prev, ref triviaList2, TriviaLocation.Trailing, parent, prevIndexInParent))
							triviaList.RemoveRange(0, firstNewlineAt);
						yield return YieldPrev(ref prev, parent, prevIndexInParent);
					}
				}

				if (prev != null)
					yield return YieldPrev(ref prev, parent, prevIndexInParent);

				// Attach leading trivia to current node
				var newAttrs = GetAttachedTrivia(node, triviaList, TriviaLocation.Leading, parent, indexInParent);

				if (!(prevIndexInParent < -1 && indexInParent <= -1))
					node = node.PlusAttrsBefore(newAttrs);
				else {
					// Special case: this trivia appears between attributes. 
					// Yield it to parent node as additional attribute(s)
					foreach (var attr in newAttrs) {
						yield return Pair.Create(attr, prevIndexInParent);
					}
				}
				triviaList.Clear();

				// Attach trivia within this node's range to this node's children, if applicable
				if (trivia.HasValue && triviaRange.EndIndex <= node.Range.EndIndex && node.Range.EndIndex >= 0) {
					node.Style &= ~NodeStyle.OneLiner;
					InjectTriviaInChildren(parent, out triviaRange, out trivia, indexInParent, ref node);
				} else
					MarkOneLiner(ref node);
			}
			if (prev != null)
				yield return YieldPrev(ref prev, parent, prevIndexInParent);
		}

		private bool TryAttachTriviaTo(ref LNode prev, ref InternalList<Trivia> triviaList, TriviaLocation loc, LNode parent, int prevIndexInParent)
		{
			var prev2 = AttachTriviaTo(prev, triviaList, loc, parent, prevIndexInParent);
			if (prev2 != null) {
				prev = prev2;
				triviaList.Clear();
				return true;
			}
			return false;
		}

		private Pair<LNode, int> YieldPrev(ref LNode prev, LNode parent, int prevIndexInParent)
		{
			var node = DoneAttaching(prev, parent, prevIndexInParent) ?? prev;
			prev = null;
			return new Pair<LNode, int>(node, prevIndexInParent);
		}

		private void InjectTriviaInChildren(LNode parent, out SourceRange triviaRange, out Maybe<Trivia> trivia, int indexInParent, ref LNode node)
		{
			// Current trivia's range is within node's range: Apply it to 
			// the node's children, if any. First gather list of children
			// and sort by source-code order, if necessary:
			int min = node.Min;
			InternalList<Pair<LNode, int>> children = InternalList<Pair<LNode, int>>.Empty;
			children.Resize(node.Max - min + 1);
			bool inOrder = true;
			int start, prevStart = int.MinValue;
			for (int i = 0; i < children.Count; i++, prevStart = start) {
				children[i] = Pair.Create(node[i + min], i + min);
				start = children[i].A.Range.StartIndex;
				if (prevStart > start)
					inOrder = false;
			}
			if (!inOrder)
				children.Sort((a, b) => a.Item1.Range.StartIndex.CompareTo(b.Item1.Range.StartIndex));

			// Call ourself recursively to apply trivia to children. Usually, 
			// newChildren is the same length as children, but it may have extra
			// trivia attributes added.
			InternalList<Pair<LNode, int>> newChildren = new InternalList<Pair<LNode, int>>(children.Count);
			var output = RunCore(children.GetEnumerator(), node);
			bool changed = false;
			while (output.MoveNext()) {
				var @new = output.Current;
				newChildren.Add(@new);
				if (@new.Item1 != children[@new.Item2 - min].Item1)
					changed = true;
			}

			// At the end, gather up any remaining trivia in the node's range that wasn't 
			// associated with a child.
			//
			// A newline is treated specially here, because sometimes (e.g. in LES3 token 
			// lists) a newline can be reified into its own node but also exist in the trivia 
			// list. In this case, node will be something like `'\n` when triviaRange equals 
			// node.Range. So let's say the node stream is something like Ann, `'\n`, Bob. If 
			// we attach the newline to the `'\n` node as usual (using the 
			// TriviaLocation.Ambiguous attachment mode, since the newline is neither leading 
			// nor trailing), the output node list will be something rather complicated:
			//   Ann, @`%appendStatement` @(`%trailing`(`%newline`)) `'\n`, @`%appendStatement` Bob
			// If we ignore the newline temporarily so that it gets treated as leading trivia
			// of Bob instead, we get simpler output:
			//   Ann, @`%appendStatement` `'\n`, Bob
			InternalList<Trivia> triviaList = InternalList<Trivia>.Empty;
			trivia = CurrentTrivia(out triviaRange);
			while (trivia.HasValue && triviaRange.EndIndex <= node.Range.EndIndex && 
			       !(IsNewline(trivia.Value) && triviaRange == node.Range)) {
				triviaList.Add(trivia.Value);
				trivia = AdvanceTrivia(out triviaRange);
				changed = true;
			}

			if (changed) {
				// If this is a call, attach any remaining trivia to the last child.
				if (node.IsCall && !triviaList.IsEmpty) {
					int i = newChildren.Count - 1;
					var last = newChildren.InternalArray[i];
					newChildren.InternalArray[i].A = AttachTriviaTo(last.A, triviaList, TriviaLocation.TrailingExtra, node, last.B) ?? last.A;
				}

				// Put the children back in their "conceptual" order
				if (!inOrder)
					newChildren.StableSort((a, b) => a.Item2.CompareTo(b.Item2));

				// Update node's attributes, if any
				int numAttrs = newChildren.TakeWhile(p => p.B < -1).Count();
				if (numAttrs > 0) {
					var attrs = LNode.List(newChildren.Slice(0, numAttrs).SelectArray(p => p.A));
					node = node.WithAttrs(attrs);
				}

				if (node.IsCall) {
					Debug.Assert(newChildren[numAttrs].B == -1);
					var newArgs = newChildren.Slice(numAttrs + 1).SelectArray(p => p.A);
					node = node.With(newChildren[numAttrs].A, LNode.List(newArgs));
				} else if (!triviaList.IsEmpty) {
					// If current node is not a call, attach any remaining trivia to it.
					node = AttachTriviaTo(node, triviaList, TriviaLocation.Ambiguous, parent, indexInParent);
				}
			}
		}

		private Maybe<Trivia> CurrentTrivia(out SourceRange range)
		{
			var trivia = SortedTrivia.TryGet(NextIndex);
			if (trivia.HasValue)
				range = GetRange(trivia.Value);
			else
				range = default(SourceRange);
			return trivia;
		}

		private Maybe<Trivia> AdvanceTrivia(out SourceRange range)
		{
			++NextIndex;
			return CurrentTrivia(out range);
		}
	}
}
