using System.Runtime.CompilerServices;

namespace Loyc.Collections.Impl
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Diagnostics;

	/// <summary>Internal implementation class. Shared base class of internal nodes
	/// for <see cref="AList{T}"/>, <see cref="SparseAList{T}"/>, <see cref="BList{T}"/>, 
	/// <see cref="BMultiMap{K,V}"/> and <see cref="BDictionary{K,V}"/>.</summary>
	[Serializable]
	public abstract class AListInnerBase<K, T> : AListNode<K, T>
	{
		public const int DefaultMaxNodeSize = 64;

		[Serializable]
		[DebuggerDisplay("Index = {Index}, Node = {Node}")]
		protected internal struct Entry
		{
			// Normally this is the base index of the items in Node (the first entry 
			// uses Index differently; see documentation of _children)
			public uint Index;
			// Child node
			public AListNode<K, T> Node;
		}

		/// <summary>List of child nodes. Empty children are null.</summary>
		/// <remarks>
		/// *** TODO ***: don't increase _children size by 4. Increase it exponentially
		/// </remarks>
		protected internal Entry[] _children;

		public override long CountSizeInBytes(int sizeOfT, int sizeOfK)
		{	// This object header + the _children reference + array object header = 6 words.
			// Assume that Entry is padded to 16 bytes on 64-bit systems.
			long size = 6 * IntPtr.Size + _children.Length * (IntPtr.Size * 2);
			for (int i = 0; i < _children.Length; i++)
				size += _children[i].Node?.CountSizeInBytes(sizeOfT, sizeOfK) ?? 0;
			return size;
		}

		#region Constructors

		protected AListInnerBase(AListInnerBase<K, T> frozen)
		{
			_children = InternalList.CopyToNewArray(frozen._children);
			_childCount = frozen._childCount;
			_maxNodeSize = frozen._maxNodeSize;
			_isFrozen = false;

			MarkChildrenFrozen();
			AssertValid();
		}
		private void MarkChildrenFrozen()
		{
			// Inform children that they are frozen
			for (int i = _childCount - 1; i >= 0; i--)
				_children[i].Node.Freeze();
		}

		public AListInnerBase(AListNode<K, T> left, AListNode<K, T> right, int maxNodeSize)
		{
			_children = new Entry[4];
			_childCount = 2;
			_children[0] = new Entry { Node = left, Index = 0 };
			_children[1] = new Entry { Node = right, Index = left.TotalCount };
			_children[2] = new Entry { Index = uint.MaxValue };
			_children[3] = new Entry { Index = uint.MaxValue };
			MaxNodeSize = maxNodeSize;
			AssertValid();
		}

		protected AListInnerBase(AListInnerBase<K,T> original, int localIndex, int localCount, uint baseIndex, int maxNodeSize)
		{
			// round up size to the nearest 4.
			_children = new Entry[(localCount + 3) & ~3];

			int i;
			for (i = 0; i < localCount; i++)
			{
				_children[i] = original._children[localIndex + i];
				_children[i].Index -= baseIndex;
			}
			_childCount = (byte)localCount;
			Debug.Assert(maxNodeSize <= MaxMaxNodeSize);
			MaxNodeSize = maxNodeSize;

			InitEmpties(i);
			AssertValid();
		}

		protected AListInnerBase(AListInnerBase<K, T> original, uint index, uint count, AListBase<K,T> list)
		{
			// This constructor is called by CopySection
			Debug.Assert(count > 0 && count <= original.TotalCount);
			int i0 = original.BinarySearchI(index);
			int iN = original.BinarySearchI(index + count - 1);
			Entry e0 = original._children[i0];
			Entry eN = original._children[iN];
			int localCount = iN - i0 + 1;
			// round up size to the nearest 4.
			_children = new Entry[(localCount + 3) & ~3];
			_isFrozen = false;
			_maxNodeSize = original._maxNodeSize;
			//_userByte = original._userByte;
			_childCount = (byte)localCount;
			InitEmpties(iN-i0+1);

			if (i0 == iN) {
				_children[0].Node = e0.Node.CopySection(index - e0.Index, count, list);
			} else {
				uint adjusted0 = index - e0.Index;
				uint adjustedN = index + count - eN.Index;
				Debug.Assert(adjusted0 <= index && adjustedN < count);
				AListNode<K, T> child0 = e0.Node.CopySection(adjusted0, e0.Node.TotalCount - adjusted0, list);
				AListNode<K, T> childN = eN.Node.CopySection(0, adjustedN, list);

				_children[0].Node = child0;
				_children[iN-i0].Node = childN;
				uint offset = child0.TotalCount;
				for (int i = i0+1; i < iN; i++)
				{
					AListNode<K, T> childI = original._children[i].Node;
					// Freeze child because it will be shared between the original 
					// list and the section being copied
					childI.Freeze();
					_children[i-i0] = new Entry { Node = childI, Index = offset };
					offset += childI.TotalCount;
				}
				_children[iN-i0].Index = offset;
		
				// Finally, if the first/last node is undersized, redistribute items.
				// Note: we can set the 'tob' parameter to null because this
				// constructor is called by CopySection, which creates an 
				// independent AList that does not have an indexer.
				while (_childCount > 1 && _children[0].Node.IsUndersized)
					HandleUndersized(0, null);
				while (_childCount > 1 && _children[_childCount - 1].Node.IsUndersized)
					HandleUndersized(_childCount - 1, null);
			}
			
			AssertValid();
		}

		#endregion

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public override bool IsLeaf => false;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public sealed override bool IsFullLeaf => false;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public override bool IsUndersized => (_childCount << 1) < _maxNodeSize;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public sealed override int LocalCount => _childCount;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		protected int MaxNodeSize
		{
			get { return _maxNodeSize; }
			set { _maxNodeSize = (byte)value; }
		}

		public sealed override uint TotalCount
		{
			get {
				int lc = _childCount;
				if (lc == 0)
					return 0;
				Entry e = _children[lc - 1];
				return e.Index + e.Node.TotalCount;
			}
		}



		private void InitEmpties(int at)
		{
			Debug.Assert(at == LocalCount);
			for (; at < _children.Length; at++) {
				Debug.Assert(_children[at].Node == null);
				_children[at].Index = uint.MaxValue;
			}
		}

		/// <summary>Performs a binary search for an index.</summary>
		/// <remarks>Optimized. Fastest for power-of-two node sizes.
		/// If index is out of range, returns the highest valid index.</remarks>
		public int BinarySearchI(uint index)
		{
			var children = _children; // might be faster
			Debug.Assert(children.Length < 256);
			
			int i = 2;
			if (children.Length > 4)
			{
				i = 8;
				if (children.Length > 16)
				{
					i = 32;
					if (children.Length > 64)
					{
						i = 128;
						i += ((uint)i < (uint)children.Length && index >= children[i].Index ? 64 : -64);
						i += ((uint)i < (uint)children.Length && index >= children[i].Index ? 32 : -32);
					}
					i += ((uint)i < (uint)children.Length && index >= children[i].Index ? 16 : -16);
					i += ((uint)i < (uint)children.Length && index >= children[i].Index ? 8 : -8);
				}
				i += ((uint)i < (uint)children.Length && index >= children[i].Index ? 4 : -4);
				i += ((uint)i < (uint)children.Length && index >= children[i].Index ? 2 : -2);
			}
			i += ((uint)i < (uint)children.Length && index >= children[i].Index ? 1 : -1);
			if (((uint)i >= (uint)children.Length || index < children[i].Index))
				--i;
			return i;
		}
		
		#if DotNet45
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		#endif
		protected bool PrepareToInsert(int i, IAListTreeObserver<K, T> tob)
		{
			AutoClone(ref _children[i].Node, this, tob);

			if (_children[i].Node.IsFullLeaf) {
				TryToShiftItemsToSiblings(i, tob);
				return false;
			}
			return true;
		}
		protected void TryToShiftItemsToSiblings(int i, IAListTreeObserver<K, T> tob)
		{
			Debug.Assert(_children[i].Node.CapacityLeft == 0);
			// Pick a sibling to transfer items to. Don't bother unless we can move at least two.
			int leftCap = i > 0 ? _children[i - 1].Node.CapacityLeft : 0;
			int rightCap = i + 1 < LocalCount ? _children[i + 1].Node.CapacityLeft : 0;
			uint totalMoved;
			if (leftCap > rightCap && leftCap >= 2) {
				totalMoved = _children[i - 1].Node.TakeFromRight(_children[i].Node, (leftCap >> 1) + 1, tob);
				_children[i].Index += totalMoved;
			} else if (rightCap >= 2) {
				totalMoved = _children[i + 1].Node.TakeFromLeft(_children[i].Node, (rightCap >> 1) + 1, tob);
				_children[i + 1].Index -= totalMoved;
			}
		}

		/// <summary>Inserts a slot after _children[i], increasing _childCount and 
		/// replacing [i] and [i+1] with splitLeft and splitRight. Notifies 'tob' 
		/// of the replacement, and checks whether this node itself needs to split.</summary>
		/// <returns>Value of splitLeft to be returned to parent (non-null if splitting)</returns>
		protected AListInnerBase<K, T> HandleChildSplit(int i, AListNode<K, T> splitLeft, ref AListNode<K, T> splitRight, IAListTreeObserver<K, T> tob)
		{
			Debug.Assert(splitLeft != null && splitRight != null);

			if (tob != null) tob.HandleChildReplaced(_children[i].Node, splitLeft, splitRight, this);

			_children[i].Node = splitLeft;

			LLInsert(i + 1, splitRight, 0);
			_children[i + 1].Index = _children[i].Index + splitLeft.TotalCount;
			AssertValid();

			// Does this node need to split too?
			return AutoSplit(out splitRight);
		}

		protected virtual AListInnerBase<K, T> AutoSplit(out AListNode<K, T> splitRight)
		{
			if (_childCount > _maxNodeSize) {
				return SplitAt(LocalCount >> 1, out splitRight);
			} else {
				splitRight = null;
				return null;
			}
		}

		/// <summary>Splits this node into two halves</summary>
		/// <param name="divAt">Index into _children where the right half starts</param>
		/// <param name="right">An AListInner node containing the right children</param>
		/// <returns>An AListInner node containing the left children</returns>
		protected abstract AListInnerBase<K, T> SplitAt(int divAt, out AListNode<K, T> right);

		public override T GetLastItem()
		{
			return _children[LocalCount-1].Node.GetLastItem();
		}

		[Conditional("DEBUG")]
		protected void AssertValid()
		{
			Debug.Assert(_childCount <= _children.Length);
			if (_childCount != 0)
				Debug.Assert(_children[0].Node != null && _children[0].Index == 0);
			else
				Debug.Assert(_children[0].Node == null && _children[0].Index == uint.MaxValue);

			uint @base = 0;
			int i;
			for (i = 1; i < _childCount; i++) {
				Debug.Assert(_children[i].Node != null);
				Debug.Assert(_children[i].Index == (@base += _children[i-1].Node.TotalCount));
			}
			for (; i < _children.Length; i++) {
				Debug.Assert(_children[i].Node == null);
				Debug.Assert(_children[i].Index == uint.MaxValue);
			}
		}

		/// <summary>Inserts a child node into _children at index i (resizing 
		/// _children if necessary), increments _childCount, and adds 
		/// indexAdjustment to _children[j].Index for all j>i (indexAdjustment can
		/// be 0 if i==_childCount).</summary>
		/// <remarks>Derived classes can override to add their own bookkeeping.</remarks>
		protected virtual void LLInsert(int i, AListNode<K, T> child, uint indexAdjustment)
		{
			AutoEnlargeChildren(1);
			for (int j = LocalCount; j > i; j--)
				_children[j] = _children[j - 1]; // insert room
			_children[i].Node = child;
			++_childCount; // increment LocalCount
			if (indexAdjustment != 0)
				AdjustIndexesAfter(i, (int)indexAdjustment);
		}

		protected void AdjustIndexesAfter(int i, int indexAdjustment)
		{
			int lcount = LocalCount;
			for (i++; i < lcount; i++)
				_children[i].Index += (uint)indexAdjustment;
		}

		public AListNode<K, T> Child(int i)
		{
			return _children[i].Node;
		}

		protected const int MaxMaxNodeSize = 0xFF;
		protected const uint FrozenBit = 0x8000;

		public override T this[uint index]
		{
			get {
				int i = BinarySearchI(index);
				return _children[i].Node[index - _children[i].Index];
			}
		}

		public override void SetAt(uint index, T item, IAListTreeObserver<K, T> tob)
		{
			Debug.Assert(!IsFrozen);
			int i = BinarySearchI(index);
			AutoClone(ref _children[i].Node, this, tob);
			var e = _children[i];
			index -= e.Index;
			e.Node.SetAt(index, item, tob);
		}

		internal uint ChildIndexOffset(int i)
		{
			Debug.Assert(i < _childCount);
			return _children[i].Index;
		}

		public override bool RemoveAt(uint index, uint count, IAListTreeObserver<K, T> tob)
		{
			Debug.Assert(!IsFrozen);
			AssertValid();
			Debug.Assert(index + count <= TotalCount && (int)(count|index) >= 0);
			bool undersizedOrAggChg = false;
			
			while (count != 0)
			{
				int i = BinarySearchI(index + count - 1);
				var e = _children[i];

				uint adjustedCount = count;
				uint adjustedIndex = index - e.Index;
				if (index <= e.Index)
				{
					adjustedCount = count + index - e.Index;
					adjustedIndex = 0;
					if (adjustedCount == e.Node.TotalCount && tob == null)
						e.Node = null; // check below
				}

				if (e.Node == null) {
					// The child will be empty after the remove operation, so we
					// can simply delete it without looking at it. This is not
					// required for correctness, but we do this optimization so 
					// that RemoveSection() runs in O(log N) time.
					Debug.Assert(tob == null);
					undersizedOrAggChg |= LLDelete(i, true);
					if (!undersizedOrAggChg && IsUndersized)
						undersizedOrAggChg = true;
				} else {
					if (AutoClone(ref e.Node, this, tob))
						_children[i].Node = e.Node;
					bool result = e.Node.RemoveAt(adjustedIndex, adjustedCount, tob);

					AdjustIndexesAfter(i, -(int)adjustedCount);
					if (result)
						undersizedOrAggChg |= HandleUndersizedOrAggregateChanged(i, tob);
				}
				AssertValid();
				count -= adjustedCount;
			}
			return undersizedOrAggChg;
		}

		internal AListInnerBase<K, T> HandleChildCloned(int i, AListNode<K, T> childClone, IAListTreeObserver<K, T> tob)
		{
			Debug.Assert(childClone.LocalCount == _children[i].Node.LocalCount);
			Debug.Assert(childClone.TotalCount == _children[i].Node.TotalCount);
			if (tob != null) tob.HandleChildReplaced(_children[i].Node, childClone, null, this);

			var self = this;
			if (IsFrozen)
				self = (AListInnerBase<K, T>) DetachedClone();
			self._children[i].Node = childClone;
			return self != this ? self : null;
		}

		protected virtual bool HandleUndersizedOrAggregateChanged(int i, IAListTreeObserver<K, T> tob)
		{
			if (_children[i].Node.IsUndersized)
				return HandleUndersized(i, tob);
			return false;
		}

		/// <summary>
		/// This is called by RemoveAt(), DoSingleOperation() for B+ trees, or by
		/// the constructor called by CopySection(), when child [i] drops below its 
		/// normal size range. We'll either distribute the child's items to its 
		/// siblings, or transfer ONE item from a sibling to increase the node's 
		/// size.
		/// </summary>
		/// <param name="i">Index of undersized child</param>
		/// <param name="tob">Observer to notify about node movements</param>
		/// <returns>True iff this node has become undersized.</returns>
		protected virtual bool HandleUndersized(int i, IAListTreeObserver<K, T> tob)
		{
			AListNode<K, T> node = _children[i].Node;
			Debug.Assert(!node.IsFrozen);

			// Examine the fullness of the siblings of e.Node.
			uint ui = (uint)i;
			AListNode<K, T> left = null, right = null;
			int leftCap = 0, rightCap = 0;
			if (ui-1u < (uint)_children.Length) {
				AutoClone(ref _children[ui-1u].Node, this, tob);
				left = _children[ui-1u].Node;
				leftCap = left.CapacityLeft;
			}
			if (ui + 1u < (uint)LocalCount) {
				AutoClone(ref _children[ui+1u].Node, this, tob);
				right = _children[ui+1u].Node;
				rightCap = right.CapacityLeft;
			}

			if (left == null && right == null)
			{
				if (node.TotalCount == 0) {
					if (tob != null) tob.NodeRemoved(node, this);
					LLDelete(i, false);
				}
				return IsUndersized;
			}
			else if (leftCap + rightCap >= node.LocalCount)
			{	// There's enough capacity to move all items from `node` into its siblings.
				// Move items in such a way that the left and right nodes have equal size if possible.
				int dif = leftCap - rightCap;
				int itemsToMoveLeft = dif + ((node.LocalCount - dif) >> 1);

				if (left != null && itemsToMoveLeft >= 0)
					left.TakeFromRight(node, Math.Min(itemsToMoveLeft, node.LocalCount), tob);
				if (node.TotalCount > 0) {
					uint rightAdjustment = right.TakeFromLeft(node, node.LocalCount, tob);
					_children[i+1].Index -= rightAdjustment;
				}

				Debug.Assert(node.LocalCount == 0);
				// Sparse leaves can have empty space without any items, but TakeFromLeft/Right takes all space
				Debug.Assert(node.TotalCount == 0);
					
				if (tob != null) tob.NodeRemoved(node, this);
				LLDelete(i, false);
				return IsUndersized;
			}
			else
			{	// Try to transfer element(s) from sibling(s) so that 'node' is no longer undersized.
				int targetSize = ((left?.LocalCount ?? 0) + (right?.LocalCount ?? 0) + node.LocalCount + 2) / 3;
				if (left != null && targetSize < left.LocalCount)
				{
					uint spaceMoved = node.TakeFromLeft(left, left.LocalCount - targetSize, tob);
					_children[i].Index -= spaceMoved;
				}
				if (right != null && targetSize < right.LocalCount)
				{
					uint spaceMoved = node.TakeFromRight(right, right.LocalCount - targetSize, tob);
					_children[i + 1].Index += spaceMoved;
				}
				return false;
			}
		}

		/// <summary>Deletes the child _children[i], shifting all entries afterward 
		/// to the left, and decrements _childCount. If adjustIndexesAfterI is true,
		/// the values of _children[j].Index where j>i are decreased appropriately.</summary>
		/// <returns>True if the aggregate value of this node may have changed (organized lists only)</returns>
		/// <remarks>Derived classes can override to add their own bookkeeping.</remarks>
		protected virtual bool LLDelete(int i, bool adjustIndexesAfterI)
		{
			int newCCount = LocalCount - 1;
			if (i < newCCount) {
				if (adjustIndexesAfterI)
				{
					uint indexAdjustment = _children[i + 1].Index - _children[i].Index;
					AdjustIndexesAfter(i, -(int)indexAdjustment);
				}
				for (int j = i; j < newCCount; j++)
					_children[j] = _children[j + 1];
			}
			_children[newCCount] = new Entry { Node = null, Index = uint.MaxValue };
			_childCount--;
			return false;
		}

		internal override uint TakeFromRight(AListNode<K, T> sibling, int localsToMove, IAListTreeObserver<K, T> tob)
		{
			Debug.Assert(localsToMove <= sibling.LocalCount && LocalCount + localsToMove <= _maxNodeSize);

			var right = (AListInnerBase<K, T>)sibling;
			if (_isFrozen || right._isFrozen)
				return 0;

			uint totalCountMoved = 0;
			for (int i = 0; i < localsToMove; i++)
			{
				uint oldTotal = TotalCount;
				int oldLocal = LocalCount;
				var child = right.Child(0);
				LLInsert(oldLocal, child, 0);
				Debug.Assert(oldLocal > 0);
				Debug.Assert(child.TotalCount > 0);
				_children[oldLocal].Index = oldTotal;
				right.LLDelete(0, true);
				AssertValid();
				right.AssertValid();
				if (tob != null) tob.NodeMoved(child, right, this);
				totalCountMoved += child.TotalCount;
			}
			return totalCountMoved;
		}

		internal override uint TakeFromLeft(AListNode<K, T> sibling, int localsToMove, IAListTreeObserver<K, T> tob)
		{
			Debug.Assert(!IsFrozen);
			var left = (AListInnerBase<K, T>)sibling;
			if (IsFrozen || left.IsFrozen)
				return 0;

			uint totalCountMoved = 0;
			for (int i = 0; i < localsToMove; i++)
			{
				var child = left.Child(left.LocalCount - 1);
				LLInsert(0, child, child.TotalCount);
				Debug.Assert(child.TotalCount > 0);
				left.LLDelete(left.LocalCount - 1, false);
				AssertValid();
				left.AssertValid();
				if (tob != null) tob.NodeMoved(child, left, this);
				totalCountMoved += child.TotalCount;
			}
			return totalCountMoved;
		}

		public override void Freeze()
		{
			_isFrozen = true;
		}

		public override int CapacityLeft { get { return MaxNodeSize - LocalCount; } }

		public void AutoEnlargeChildren(int more)
		{
			if (_childCount + more > _children.Length)
			{
				int newCapacity = InternalList.NextLargerSize(_childCount + more - 1, _maxNodeSize);
				_children = InternalList.CopyToNewArray(_children, _childCount, newCapacity);
				InitEmpties(_childCount);
			}
		}

		public uint BaseIndexOf(AListNode<K, T> child)
		{
			int i = IndexOf(child);
			if (i <= 0)
				return (uint)i;
			return _children[i].Index;
		}
		public int IndexOf(AListNode<K, T> child)
		{
			for (int i = 0; i < _children.Length; i++)
				if (_children[i].Node == child)
					return i;
			return -1;
		}

		public override uint GetImmutableCount(bool excludeSparse)
		{
			if (IsFrozen && !excludeSparse)
				return TotalCount;
			uint ic = 0;
			for (int i = 0; i < _childCount; i++)
				ic += Child(i).GetImmutableCount(excludeSparse);
			return ic;
		}

		internal override IEnumerable<AListNode<K, T>> Leaves => Children.SelectMany(child => child.Leaves);
		internal override IEnumerable<AListNode<K, T>> Children => _children.Select(e => e.Node).WhereNotNull();
	}
}
