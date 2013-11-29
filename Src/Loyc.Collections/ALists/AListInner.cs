using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Loyc.Collections.Impl
{
	class AListInner<T> : AListInnerBase<int, T>
	{
		#region Constructors and boilerplate

		protected AListInner(AListInner<T> frozen) : base(frozen) { }
		public AListInner(AListNode<int, T> left, AListNode<int, T> right, int maxNodeSize) : base(left, right, maxNodeSize) { }
		protected AListInner(AListInner<T> original, int localIndex, int localCount, uint baseIndex, int maxNodeSize) 
			: base(original, localIndex, localCount, baseIndex, maxNodeSize) { }
		protected AListInner(AListInner<T> original, uint index, uint count, AListBase<int, T> list) : base(original, index, count, list) { }

		public override AListNode<int, T> DetachedClone()
		{
			AssertValid();
			return new AListInner<T>(this);
		}
		public override AListNode<int, T> CopySection(uint index, uint count, AListBase<int, T> list)
		{
			Debug.Assert(count > 0 && count <= TotalCount);
			if (index == 0 && count >= TotalCount)
				return DetachedClone();

			return new AListInner<T>(this, index, count, list);
		}
		protected override AListInnerBase<int, T> SplitAt(int divAt, out AListNode<int, T> right)
		{
			right = new AListInner<T>(this, divAt, LocalCount - divAt, _children[divAt].Index, MaxNodeSize);
			return new AListInner<T>(this, 0, divAt, 0, MaxNodeSize);
		}

		#endregion

		private AListInnerBase<int, T> AutoHandleChildSplit(int i, AListNode<int, T> splitLeft, ref AListNode<int, T> splitRight, IAListTreeObserver<int, T> tob)
		{
			if (splitLeft == null)
			{
				AssertValid();
				return null;
			}
			return HandleChildSplit(i, splitLeft, ref splitRight, tob);
		}

		private int PrepareToInsertAt(uint index, out Entry e, IAListTreeObserver<int, T> tob)
		{
			Debug.Assert(index <= TotalCount);

			// Choose a child node [i] = entry {child, baseIndex} in which to insert the item(s)
			int i = BinarySearchI(index);
			if (i != 0 && _children[i].Index == index)
			{
				// Check whether one slot left is a better insertion location
				if (_children[i - 1].Node.LocalCount < _children[i].Node.LocalCount)
					--i;
			}

			PrepareToInsert(i, tob);
			e = _children[i];
			return i;
		}

		public override AListNode<int, T> Insert(uint index, T item, out AListNode<int, T> splitRight, IAListTreeObserver<int, T> tob)
		{
			Debug.Assert(!IsFrozen);
			Entry e;
			int i = PrepareToInsertAt(index, out e, tob);

			// Perform the insert, and adjust base index of nodes that follow
			AssertValid();
			var splitLeft = e.Node.Insert(index - e.Index, item, out splitRight, tob);
			AdjustIndexesAfter(i, 1);

			// Handle child split
			return splitLeft == null ? null : HandleChildSplit(i, splitLeft, ref splitRight, tob);
		}

		public override AListNode<int, T> InsertRange(uint index, IListSource<T> source, ref int sourceIndex, out AListNode<int, T> splitRight, IAListTreeObserver<int, T> tob)
		{
			Debug.Assert(!IsFrozen);
			Entry e;
			int i = PrepareToInsertAt(index + (uint)sourceIndex, out e, tob);

			// Perform the insert
			int oldSourceIndex = sourceIndex;
			AListNode<int, T> splitLeft;
			do {
				splitLeft = e.Node.InsertRange(index - e.Index, source, ref sourceIndex, out splitRight, tob);
			} while (sourceIndex < source.Count && splitLeft == null);
			
			// Adjust base index of nodes that follow
			int change = sourceIndex - oldSourceIndex;
			AdjustIndexesAfter(i, change);

			// Handle child split
			return splitLeft == null ? null : HandleChildSplit(i, splitLeft, ref splitRight, tob);
		}

		public override int DoSparseOperation(ref AListSparseOperation<T> op, out AListNode<int, T> splitLeft, out AListNode<int, T> splitRight)
		{
			Debug.Assert(!IsFrozen);
			Debug.Assert(op.Source == null || op.SourceCount == op.Source.Count);
			AssertValid();
			Entry e;
			
			//int i = PrepareToInsertAt(op.Index + (uint)op.SourceIndex, out e, op.tob);
			int i = BinarySearchI(op.Index + (uint)op.SourceIndex);
			AutoClone(ref _children[i].Node, this, op.tob);
			e = _children[i];

			// Perform the insert
			op.Index -= e.Index;
			int change = 0;
			do {
				change += e.Node.DoSparseOperation(ref op, out splitLeft, out splitRight);
			} while (op.SourceIndex < op.SourceCount && splitLeft == null);
			
			// Adjust base index of nodes that follow
			AdjustIndexesAfter(i, change);

			// Handle child split/undersize
			if (splitLeft == null)
				return change;
			else if (splitRight != null) {
				splitLeft = HandleChildSplit(i, splitLeft, ref splitRight, op.tob);
				return change;
			} else {
				if (HandleUndersized(i, op.tob))
					splitLeft = this;
				return change;
			}
		}

		/// <summary>Appends or prepends some other list to this list. The other 
		/// list must be the same height or less tall.</summary>
		/// <param name="other">A list to append/prepend</param>
		/// <param name="heightDifference">Height difference between the trees (0 or >0)</param>
		/// <param name="splitRight">Right half in case node is split</param>
		/// <param name="tob">Observer to be notified of changes</param>
		/// <param name="move">Move semantics (avoids freezing the nodes of the other tree)</param>
		/// <param name="append">Operation to perform (true => append)</param>
		/// <returns>Normally null, or left half in case node is split</returns>
		public virtual AListInnerBase<int, T> Combine(AListInnerBase<int, T> other, int heightDifference, out AListNode<int, T> splitRight, IAListTreeObserver<int, T> tob, bool move, bool append)
		{
			Debug.Assert(!IsFrozen && heightDifference >= 0);
			if (heightDifference != 0)
			{
				int i = append ? LocalCount - 1 : 0;
				AutoClone(ref _children[i].Node, this, tob);
				var splitLeft = ((AListInner<T>)Child(i)).Combine(other, heightDifference - 1, out splitRight, tob, move, append);
				if (!append) {
					Debug.Assert(LocalCount == 1 || other.TotalCount == _children[0].Node.TotalCount - _children[1].Index);
					AdjustIndexesAfter(i, (int)other.TotalCount);
				}
				return AutoHandleChildSplit(i, splitLeft, ref splitRight, tob);
			}

			Debug.Assert(other.GetType() == GetType());
			int otherLC = other.LocalCount;
			AutoEnlargeChildren(otherLC);
			for (int i = 0; i < otherLC; i++)
			{
				var child = other.Child(i);
				if (!move)
					child.Freeze(); // we're sharing this node between two trees
				if (append) {
					uint tc = TotalCount;
					LLInsert(_childCount, child, 0);
					_children[_childCount - 1].Index = tc;
				} else
					LLInsert(i, child, child.TotalCount);
			}

			return AutoSplit(out splitRight);
		}
	}
}
