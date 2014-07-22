using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Loyc.Collections.Impl
{
	/// <summary>Internal node of <see cref="AList{T}"/> and <see cref="SparseAList{T}"/>.</summary>
	internal class AListInner<T> : AListInnerBase<int, T>
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

		internal override int DoSparseOperation(ref AListSparseOperation<T> op, int index, out AListNode<int, T> splitLeft, out AListNode<int, T> splitRight)
		{
			Debug.Assert(!IsFrozen);
			Debug.Assert(op.Source == null || op.SourceCount == op.Source.Count);
			AssertValid();
			Entry e = default(Entry);
			
			// Perform the insert/replace
			int change = 0, i = 0;
			do {
				if (change == 0) {
					// runs once for insert operations, once or multiple times for replace
					i = BinarySearchI((uint)(index + op.SourceIndex));
					AutoClone(ref _children[i].Node, this, op.tob);
					e = _children[i];
				}
				change += e.Node.DoSparseOperation(ref op, index - (int)e.Index, out splitLeft, out splitRight);
			} while (op.SourceIndex < op.SourceCount && splitLeft == null && index + op.SourceIndex < TotalCount);
			
			// Adjust base index of nodes that follow
			if (change != 0)
				AdjustIndexesAfter(i, change);

			// Handle child split/undersize
			if (splitLeft == null)
				return change;
			else if (splitRight != null) {
				splitLeft = HandleChildSplit(i, splitLeft, ref splitRight, op.tob);
				return change;
			} else {
				splitLeft = HandleUndersized(i, op.tob) ? this : null;
				return change;
			}
		}
		
		internal override T SparseGetNearest(ref int? index, int direction)
		{
			int i = BinarySearchI((uint)index.Value);
			if (i >= _childCount) {
				if (direction < 0)
					i = _childCount - 1;
				else {
					index = null;
					return default(T);
				}
			}
			var e = _children[i];
			int? index2 = index.Value - (int)e.Index;
			var result = e.Node.SparseGetNearest(ref index2, direction);
			if (index2 == null && direction != 0 && (uint)(i + direction) < _childCount)
			{
				// index must have pointed to blank space. In that case there are 
				// two children where the nearest might live; now check the other one.
				e = _children[i + direction];
				index2 = direction > 0 ? 0 : int.MaxValue;
				result = e.Node.SparseGetNearest(ref index2, direction);
			}
			index = index2 == null ? null : index2 + (int)e.Index;
			return result;
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

		public override uint GetRealItemCount()
		{
			uint ric = 0;
			for (int i = 0; i < _childCount; i++)
				ric += Child(i).GetRealItemCount();
			return ric;
		}
	}
}
