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
	/// must be included in the trivia list, as the algorithm relies on these newlines
	/// instead of asking <see cref="LNode.Range"/> for line numbers, which is
	/// slower. If newlines are omitted then a comment right after a statement (e.g.
	/// on the same line) will be associated with the next statement instead.</li>
	/// </ul>
	/// 
	/// Typically one will wrap the lexer in <see cref="TriviaSaver"/>, which saves
	/// trivia while filtering out whitespace so that the parser doesen't see it.
	/// <para/>
	/// Your language's parser needs to follow the following rules:
	/// 
	/// <ul>
	/// <li>The parser should assign minimal boundaries to each node: the 
	/// <see cref="LNode.Range"/> should not be wider than necessary. If there is 
	/// a comment before an expression like <c>/*!*/ x + y</c>, the parser should 
	/// not include the comment as part of the range unless it wants the comment to 
	/// be associated with a child node (<c>x</c>) instead of with the entire 
	/// expression (<c>x + y</c>).</li>
	/// <li>If a node has normal (non-trivia) attributes attached to it, the range of
	/// the node should include those attributes.</li>
	/// <li>The parser should use <see cref="LNodeFactory.InParens"/> to place a
	/// node in parentheses and include the index of the opening and closing parens.</li>
	/// <li>If an expression/statement is terminated by a semicolon/comma, it's 
	/// best (but not crucial) to include the semicolon/comma in the LNode.Range, 
	/// but if a statement is terminated by a newline, the newline should not be 
	/// included in the range. If general, when the terminator is included in the 
	/// range, then any comment that appears before the terminator is attached
	/// to the final child node rather than to the node as a whole. (e.g. in 
	/// <c>x = y /*y*/;</c>, the comment is associated with <c>y</c> if the
	/// comment is within the range of the statement as a whole.)</li>
	/// </ul>
	/// 
	/// This example shows how comments are associated with nodes:
	/// <pre>
	/// // Comment attached to block
	/// {
	///   // Comment attached to Foo() call
	///   Foo(
	///		// Comment attached to «argument1 + 1». Note that the comma is 
	///		// generally invisible to the injector since it is not part of the LNode 
	///		// tree, so the 1st and 2nd comments will both be attached to the sum-
	///		// expression as a whole.
	///		argument1 + 1 /*1st comment*/, // 2nd comment
	///		// Comment attached to «argument2 + 2»
	///		argument2 + 2); // Comment attached to Foo() call
	///	  Area = 3.14159265/*PI*/ * /*radius*/r**2;
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

		/// <summary>Parent calls of the current node.</summary>
		protected IListSource<LNode> ParentNodes { get { return _parentNodes; } }
		DList<LNode> _parentNodes = new DList<LNode>();

		/// <summary>Initializes the <see cref="SortedTrivia"/> property.</summary>
		public AbstractTriviaInjector(IListSource<Trivia> sortedTrivia) { SortedTrivia = sortedTrivia; }

		/// <summary>Derived class should associate the given list of trivia with the 
		/// specified node. Leading trivia will be attached to a given node before 
		/// trailing trivia.</summary>
		/// <param name="trivia">Trivia to be associated with <c>node</c>. Usually, all 
		/// leading trivia is provided in a single call to this method and all trailing trivia is 
		/// provided in a second call, but this is not guaranteed.</param>
		/// <param name="trailing">If true, this is trailing trivia. If false, this is leading trivia.</param>
		/// <returns>The same node with trivia attributes added.</returns>
		/// <remarks>This method is STILL called for a given node when there is no trivia 
		/// associated with that node.</remarks>
		protected abstract LNode AttachTriviaTo(LNode node, IListSource<Trivia> trivia, TriviaLocation loc, LNode parent, int indexInParent);

		protected enum TriviaLocation {
			/// <summary>Trivia that appeared before a node</summary>
			Leading = 0,
			/// <summary>Trivia associated with a node that came before it</summary>
			Trailing = 1,
			/// <summary>Trivia attached to the last node in an argument list or last statement in a block</summary>
			TrailingExtra = 2
		}

		/// <summary>Gets the <see cref="SourceRange"/> for an element of trivia.</summary>
		protected abstract SourceRange GetRange(Trivia trivia);

		/// <summary>Returns true if the trivia represents a newline, false otherwise.</summary>
		protected abstract bool IsNewline(Trivia trivia);

		/// <summary>A method called to create a virtual node to apply trivia to an empty source file.</summary>
		/// <remarks>Default implementation attaches all trivia to a "missing" node 
		/// (zero-length identifier). If this method returns null then the result is discarded.</remarks>
		protected virtual LNode GetEmptyResultSet()
		{
			if (SortedTrivia.Count == 0)
				return null;
			var dummy = LNode.Id(LNode.List(LNode.Id(CodeSymbols.TriviaDummyNode)), GSymbol.Empty, GetRange(SortedTrivia[0]));
			return AttachTriviaTo(dummy, SortedTrivia, TriviaLocation.TrailingExtra, null, -1);
		}

		/// <summary>Attaches trivia to the input nodes provided.</summary>
		/// <param name="nodes">List of input nodes. This method calls nodes.MoveNext()
		/// before calling nodes.Current.</param>
		/// <remarks>Trailing trivia after all nodes is attached to the final node. If 
		/// <c>nodes</c> is empty then <see cref="GetEmptyResultSet"/> is called to
		/// create a dummy node and attach all trivia to it.</remarks>
		public IEnumerator<LNode> Run(IEnumerator<LNode> nodes)
		{
			NextIndex = 0;
			var e = RunCore(nodes, null);
			if (e.MoveNext()) {
				LNode next = e.Current;
				while (e.MoveNext()) {
					yield return next;
					next = e.Current;
				}
				// Associate remaining trivia with final node
				if (NextIndex < SortedTrivia.Count)
					next = AttachTriviaTo(next, SortedTrivia.Slice(NextIndex), TriviaLocation.TrailingExtra, null, int.MaxValue);
				yield return next;
			} else {
				// No nodes exist
				var result = GetEmptyResultSet();
				if (result != null)
					yield return result;
			}
		}

		protected IEnumerator<LNode> RunCore(IEnumerator<LNode> nodes, LNode parent, int numAttrs = -1)
		{
			SourceRange triviaRange;
			Maybe<Trivia> trivia = CurrentTrivia(out triviaRange);
			int ordinal = 0;
			LNode node, prev;
			for (prev = null; trivia.HasValue && nodes.MoveNext(); prev = node, ordinal++) {
				node = nodes.Current;
				int indexInParent = ordinal - numAttrs - 1;

				// Get a list of trivia that appears before the current node. The tricky part 
				// of this is that we sometimes want to associate some comments with the
				// previous node instead of the current node. This is done when...
				// (1) a comment appears on the same line as the previous node AND the
				//        current node is on a different line, e.g. Kill(p); /* end process */
				// (2) a comment appears on the next line after the previous node AND
				//        there is a blank line between that comment and the current node
				InternalList<Trivia> triviaList = InternalList<Trivia>.Empty;
				bool sawNewlineYet = false;
				while (triviaRange.StartIndex < node.Range.StartIndex) {
					if (prev != null && IsNewline(trivia.Value)) {
						if (!triviaList.IsEmpty) {
							if (!sawNewlineYet) {
								// case (1)
								prev = AttachTriviaTo(prev, triviaList, TriviaLocation.Trailing, parent, indexInParent - 1);
								triviaList.Clear();
							} else if (IsNewline(triviaList.Last)) {
								// case (2)
								yield return AttachTriviaTo(prev, triviaList, TriviaLocation.Trailing, parent, indexInParent - 1);
								triviaList.Clear();
								prev = null;
							}
						}
						sawNewlineYet = true;
					}
					triviaList.Add(trivia.Value);
					if (!(trivia = AdvanceTrivia(out triviaRange)).HasValue)
						break;
				}

				// Attach leading trivia to current node
				node = AttachTriviaTo(node, triviaList, TriviaLocation.Leading, parent, indexInParent);
				triviaList.Clear();

				while (trivia.HasValue && triviaRange.EndIndex <= node.Range.EndIndex && node.Range.EndIndex >= 0) {
					// Current trivia's range is within node's range: apply it to children
					if (!node.IsCall) {
						// This case is rare
						triviaList.Add(trivia.Value);
						node = AttachTriviaTo(node, triviaList, TriviaLocation.Leading, parent, indexInParent);
						trivia = AdvanceTrivia(out triviaRange);
					} else {
						// Assuming the node is sorted, we can do this:
						// TODO: not always true; write alternate version of this code
						_parentNodes.Add(node);
						try {
							var output = RunCore(node.GetEnumerator(), node, node.AttrCount);

							int max = node.Max;
							for (int i = node.Min; ; i++) {
								G.Verify(output.MoveNext());
								// TODO: do this more efficiently (since node is immutable, this code 
								// is O(n^2) in the number of children if O(n) changes occur)
								node = node.WithChildChanged(i, output.Current);
								if (i == max) {
									// Attach any remaining trivia to the last child
									trivia = CurrentTrivia(out triviaRange);
									while (trivia.HasValue && triviaRange.EndIndex <= node.Range.EndIndex) {
										triviaList.Add(trivia.Value);
										trivia = AdvanceTrivia(out triviaRange);
									}
									node = node.WithChildChanged(max, AttachTriviaTo(node[max], triviaList, TriviaLocation.TrailingExtra, node, max));
									break;
								}
							}
						} finally {
							_parentNodes.RemoveAt(_parentNodes.Count - 1);
						}
					}
				}
				if (prev != null)
					yield return prev;
			}
			if (prev != null)
				yield return prev;
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
