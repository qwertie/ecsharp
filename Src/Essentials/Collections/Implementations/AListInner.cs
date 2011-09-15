namespace Loyc.Collections.Impl
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Diagnostics;

	[Serializable]
	public class AListInner<T> : AListNode<T>
	{
		public const int DefaultMaxNodeSize = 8;

		protected struct Entry
		{
			// Normally this is the base index of the items in Node (the first entry 
			// uses Index differently; see documentation of _children)
			public uint Index;
			// Child node
			public AListNode<T> Node;

			public static Func<Entry, uint, int> Compare = delegate(Entry e, uint index)
			{
				return e.Index.CompareTo(index);
			};
		}

		/// <summary>List of child nodes. Empty children are null.</summary>
		/// <remarks>Binary search is optimized for Length of 4 or 8. 
		/// _children[0].Index holds special information (not an index):
		/// 1. The low byte holds the number of slots used in _children.
		/// 2. The second byte holds the maximum node size.
		/// 3. The third byte marks the node as frozen when it is nonzero
		/// 4. The fourth byte is available for the derived class to use
		/// </remarks>
		Entry[] _children;

		#region Constructors

		protected AListInner(AListInner<T> frozen)
		{
			Debug.Assert(frozen.IsFrozen);
			_children = InternalList.CopyToNewArray(frozen._children);
			_children[0].Index &= ~FrozenBit; // unfreeze the clone

			MarkChildrenFrozen();
			AssertValid();
		}
		private void MarkChildrenFrozen()
		{
			// Inform children that they are frozen
			for (int i = LocalCount - 1; i >= 0; i--)
				_children[i].Node.Freeze();
		}

		public AListInner(AListNode<T> left, AListNode<T> right, int maxNodeSize)
		{
			_children = new Entry[4];
			_children[0] = new Entry { Node = left, Index = 2 };
			_children[1] = new Entry { Node = right, Index = left.TotalCount };
			_children[2] = new Entry { Index = uint.MaxValue };
			_children[3] = new Entry { Index = uint.MaxValue };
			MaxNodeSize = maxNodeSize;
			AssertValid();
		}

		protected AListInner(ListSourceSlice<Entry> slice, uint baseIndex, int maxNodeSize)
		{
			// round up size to the nearest 4.
			_children = new Entry[(slice.Count + 3) & ~3];

			int i;
			for (i = 0; i < slice.Count; i++)
			{
				_children[i] = slice[i];
				_children[i].Index -= baseIndex;
			}
			_children[0].Index = (uint)slice.Count;
			MaxNodeSize = Math.Min(maxNodeSize, MaxMaxNodeSize);

			InitEmpties(i);
			AssertValid();
		}

		private AListInner(AListInner<T> original, uint index, uint count)
		{
			// This constructor is called by CopySection
			Debug.Assert(count > 0 && count <= TotalCount);
			int i0 = original.BinarySearch(index);
			int iN = original.BinarySearch(index + count - 1);
			Entry e0 = original.GetEntry(i0);
			Entry eN = original.GetEntry(iN);
			int localCount = iN - i0 + 1;
			// round up size to the nearest 4.
			_children = new Entry[(localCount + 3) & ~3];
			_children[0].Index = original._children[0].Index;
			SetLCount(localCount);
			InitEmpties(iN-i0+1);

			if (i0 == iN) {
				_children[0].Node = e0.Node.CopySection(index - e0.Index, count);
			} else {
				uint adjusted0 = index - e0.Index;
				uint adjustedN = index + count - eN.Index;
				Debug.Assert(adjusted0 <= index && adjustedN < count);
				AListNode<T> child0 = e0.Node.CopySection(adjusted0, e0.Node.TotalCount - adjusted0);
				AListNode<T> childN = eN.Node.CopySection(0, adjustedN);

				_children[0].Node = child0;
				_children[iN-i0].Node = childN;
				uint offset = child0.TotalCount;
				for (int i = i0+1; i < iN; i++)
				{
					AListNode<T> childI = original._children[i].Node;
					// Freeze child because it will be shared between the original 
					// list and the section being copied
					childI.Freeze();
					_children[i-i0] = new Entry { Node = childI, Index = offset };
					offset += childI.TotalCount;
				}
				_children[iN-i0].Index = offset;
		
				// Finally, if the first/last node is undersized, redistribute items.
				// Note: we can set the 'idx' parameter to null because this
				// constructor is called by CopySection, which creates an 
				// independent AList that does not have an indexer.
				while (_children[0].Node.IsUndersized)
					HandleUndersized(0, null);
				while ((localCount = LocalCount) > 1 && _children[localCount-1].Node.IsUndersized)
					HandleUndersized(localCount-1, null);
			}
			
			AssertValid();
		}

		#endregion

		private void InitEmpties(int at)
		{
			Debug.Assert(at == LocalCount);
			for (; at < _children.Length; at++) {
				Debug.Assert(_children[at].Node == null);
				_children[at].Index = uint.MaxValue;
			}
		}

		public int BinarySearch(uint index)
		{
			// optimize for Length 4 and 8
			if (_children.Length == 8) {
				if (index >= _children[4].Index) {
					if (index < _children[6].Index)
						return 4 + (index >= _children[5].Index ? 1 : 0);
					else
						return 6 + (index >= _children[7].Index ? 1 : 0);
				}
			} else if (_children.Length != 4) {
				// TODO: verify whether this works. I have a feeling the binary search will be tripped up by the special value of _children[0].Index
				int i = InternalList.BinarySearch(_children, LocalCount, index, Entry.Compare);
				return i >= 0 ? i : ~i - 1;
			}
			if (index < _children[2].Index)
				return (index >= _children[1].Index ? 1 : 0);
			else
				return 2 + (index >= _children[3].Index ? 1 : 0);
		}

		private int PrepareToInsert(uint index, out Entry e, AListIndexerBase<T> idx)
		{
			Debug.Assert(index <= TotalCount);

			// Choose a child node [i] = entry {child, baseIndex} in which to insert the item(s)
			int i = BinarySearch(index);
			e = _children[i];
			if (i == 0)
				e.Index = 0;
			else if (e.Index == index)
			{
				// Check whether one slot left is a better insertion location
				Entry eL = _children[i - 1];
				if (eL.Node.LocalCount < e.Node.LocalCount)
				{
					e = eL;
					if (--i == 0)
						e.Index = 0;
				}
			}

			if (AutoClone(ref e.Node, this, idx))
				_children[i].Node = e.Node;

			// If the child is a full leaf, consider shifting an element to a sibling
			if (e.Node.IsFullLeaf)
			{
				AListNode<T> childL, childR;
				// Check the left sibling
				if (i > 0 && (childL = _children[i - 1].Node).TakeFromRight(e.Node, idx) != 0)
				{
					_children[i].Index++;
					e = GetEntry(i);
				}
				// Check the right sibling
				else if (i + 1 < _children.Length &&
					(childR = _children[i + 1].Node) != null && childR.TakeFromLeft(e.Node, idx) != 0)
				{
					_children[i + 1].Index--;
				}
			}
			return i;
		}

		public override AListNode<T> Insert(uint index, T item, out AListNode<T> splitRight, AListIndexerBase<T> idx)
		{
			Debug.Assert(!IsFrozen);
			Entry e;
			int i = PrepareToInsert(index, out e, idx);

			// Perform the insert, and adjust base index of nodes that follow
			AssertValid();
			var splitLeft = e.Node.Insert(index - e.Index, item, out splitRight, idx);
			AdjustIndexesAfter(i, 1);

			// Handle child split
			return splitLeft == null ? null : HandleChildSplit(e, i, splitLeft, ref splitRight, idx);
		}

		private AListInner<T> AutoHandleChildSplit(int i, AListNode<T> splitLeft, ref AListNode<T> splitRight, AListIndexerBase<T> idx)
		{
			if (splitLeft == null)
			{
				AssertValid();
				return null;
			}
			return HandleChildSplit(GetEntry(i), i, splitLeft, ref splitRight, idx);
		}
		private AListInner<T> HandleChildSplit(Entry e, int i, AListNode<T> splitLeft, ref AListNode<T> splitRight, AListIndexerBase<T> idx)
		{
			Debug.Assert(splitLeft != null);
			Debug.Assert(e.Node == _children[i].Node);

			if (idx != null) idx.HandleChildReplaced(e.Node, splitLeft, splitRight, this);

			_children[i].Node = splitLeft;
			if (splitRight == null)
			{	// Child was cloned, not split
				Debug.Assert(false); // code refactored: I don't think this happens any more
				Debug.Assert(e.Node.IsFrozen);
				AssertValid();
				return null;
			}

			LLInsert(i + 1, splitRight, 0);
			_children[i + 1].Index = e.Index + splitLeft.TotalCount;
			AssertValid();

			// Does this node need to split too?
			return AutoSplit(out splitRight);
		}

		private AListInner<T> AutoSplit(out AListNode<T> splitRight)
		{
			if (_children.Length > MaxNodeSize) {
				Debug.Assert(LocalCount > MaxNodeSize);
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
		protected virtual AListInner<T> SplitAt(int divAt, out AListNode<T> right)
		{
			right = new AListInner<T>(_children.AsListSource().Slice(divAt, LocalCount - divAt), _children[divAt].Index, MaxNodeSize);
			return new AListInner<T>(_children.AsListSource().Slice(0, divAt), 0, MaxNodeSize);
		}

		public override AListNode<T> InsertRange(uint index, IListSource<T> source, ref int sourceIndex, out AListNode<T> splitRight, AListIndexerBase<T> idx)
		{
			Debug.Assert(!IsFrozen);
			Entry e;
			int i = PrepareToInsert(index + (uint)sourceIndex, out e, idx);

			// Perform the insert
			int oldSourceIndex = sourceIndex;
			AListNode<T> splitLeft;
			do {
				splitLeft = e.Node.InsertRange(index - e.Index, source, ref sourceIndex, out splitRight, idx);
			} while (sourceIndex < source.Count && splitLeft == null);
			
			// Adjust base index of nodes that follow
			int change = sourceIndex - oldSourceIndex;
			AdjustIndexesAfter(i, change);

			// Handle child split
			return splitLeft == null ? null : HandleChildSplit(e, i, splitLeft, ref splitRight, idx);
		}

		[Conditional("DEBUG")]
		protected void AssertValid()
		{
			Debug.Assert(LocalCount <= _children.Length);
			Debug.Assert((_children[0].Node != null) == (LocalCount != 0));

			uint @base = 0;
			int i;
			for (i = 1; i < LocalCount; i++) {
				Debug.Assert(_children[i].Node != null);
				Debug.Assert(_children[i].Index == (@base += _children[i-1].Node.TotalCount));
			}
			for (; i < _children.Length; i++) {
				Debug.Assert(_children[i].Node == null);
				Debug.Assert(_children[i].Index == uint.MaxValue);
			}
		}

		private void LLInsert(int i, AListNode<T> child, uint indexAdjustment)
		{
			AutoEnlarge(1);
			for (int j = LocalCount; j > i; j--)
				_children[j] = _children[j - 1]; // insert room
			if (i == 0)
				_children[1].Index = 0;
			_children[i].Node = child;
			++_children[0].Index; // increment LocalCount
			if (indexAdjustment != 0)
				AdjustIndexesAfter(i, (int)indexAdjustment);
		}

		private void AdjustIndexesAfter(int i, int indexAdjustment)
		{
			int lcount = LocalCount;
			for (i++; i < lcount; i++)
				_children[i].Index += (uint)indexAdjustment;
		}

		public AListNode<T> Child(int i)
		{
			return _children[i].Node;
		}

		public sealed override int LocalCount
		{
			get {
				Debug.Assert(_children[0].Index != 0);
				return (byte)_children[0].Index;
			}
		}
		void SetLCount(int value)
		{
			Debug.Assert((uint)value < 0xFFu);
			_children[0].Index = (_children[0].Index & ~0xFFu) + (uint)value;
		}

		protected const int MaxMaxNodeSize = 0x7F;
		protected const uint FrozenBit = 0x8000;

		protected int MaxNodeSize
		{
			get { return (int)((_children[0].Index >> 8) & (uint)MaxMaxNodeSize); }
			set { 
				Debug.Assert((uint)value <= (uint)MaxMaxNodeSize);
				_children[0].Index = (_children[0].Index & ~((uint)MaxMaxNodeSize<<8)) | ((uint)value << 8);
			}
		}

		public sealed override uint TotalCount
		{
			get {
				int lc = LocalCount;
				if (lc <= 1)
				{
					if (LocalCount == 0)
						return 0;
					return _children[0].Node.TotalCount;
				}
				Entry e = _children[lc - 1];
				return e.Index + e.Node.TotalCount;
			}
		}

		public sealed override bool IsFullLeaf
		{
			get { return false; }
		}
		public override bool IsUndersized
		{
			get { return LocalCount < MaxNodeSize / 2; }
		}

		public override T this[uint index]
		{
			get {
				int i = BinarySearch(index);
				if (i == 0)
					return _children[i].Node[index];
				return _children[i].Node[index - _children[i].Index];
			}
		}

		public override void SetAt(uint index, T item, AListIndexerBase<T> idx)
		{
			Debug.Assert(!IsFrozen);
			int i = BinarySearch(index);
			AutoClone(ref _children[i].Node, this, idx);
			var e = GetEntry(i);
			index -= e.Index;
			e.Node.SetAt(index, item, idx);
		}

		protected Entry GetEntry(int i)
		{
			Entry e = _children[i];
			if (i == 0)
				e.Index = 0;
			return e;
		}
		internal uint ChildIndexOffset(int i)
		{
			Debug.Assert(i < LocalCount);
			return i == 0 ? 0u : _children[i].Index;
		}

		public override AListNode<T> RemoveAt(uint index, uint count, AListIndexerBase<T> idx)
		{
			Debug.Assert(!IsFrozen);

			Debug.Assert(index + count <= TotalCount && (int)(count|index) >= 0);
			bool undersized = false;
			while (count != 0)
			{
				int i = BinarySearch(index);
				var e = GetEntry(i);

				AListNode<T> result;
				uint adjustedIndex = index - e.Index;
				uint adjustedCount = count;
				if (count > 1) {
					uint left = e.Node.TotalCount - adjustedIndex;
					Debug.Assert((int)left > 0);
					if (adjustedCount >= left)
					{
						adjustedCount = left;
						if (adjustedIndex == 0 && idx == null)
							e.Node = null;
					}
				}
				if (e.Node == null) {
					// The child will be empty after the remove operation, so we
					// can simply delete it without looking at it. This is not
					// required for correctness, but we do this optimization so 
					// that RemoveSection() runs in O(log N) time.
					Debug.Assert(idx == null);
					LLDelete(i, true);
					if (!undersized && IsUndersized)
						undersized = true;
				} else {
					if (AutoClone(ref e.Node, this, idx))
						_children[i].Node = e.Node;
					result = e.Node.RemoveAt(adjustedIndex, adjustedCount, idx);

					AdjustIndexesAfter(i, -(int)adjustedCount);
					if (result != null)
					{
						_children[i].Node = result;
						if (result.IsUndersized)
							undersized |= HandleUndersized(i, idx);
					}
				}
				AssertValid();
				count -= adjustedCount;
			}
			return undersized ? this : null;
		}

		internal AListInner<T> HandleChildCloned(int i, AListNode<T> childClone, AListIndexerBase<T> idx)
		{
			Debug.Assert(childClone.LocalCount == _children[i].Node.LocalCount);
			Debug.Assert(childClone.TotalCount == _children[i].Node.TotalCount);
			if (idx != null) idx.HandleChildReplaced(_children[i].Node, childClone, null, this);

			var self = this;
			if (IsFrozen)
				self = new AListInner<T>(this); // equivalent to DetachedClone()
			self._children[i].Node = childClone;
			return self != this ? self : null;
		}

		private bool HandleUndersized(int i, AListIndexerBase<T> idx)
		{
			AListNode<T> node = _children[i].Node;
			// This is called by RemoveAt(), or the constructor called by 
			// CopySection(), when child [i] drops below its normal size range.
			// We'll either distribute the child's items to its siblings, or
			// transfer ONE item from a sibling to increase the node's size.
			Debug.Assert(!node.IsFrozen);

			// Examine the fullness of the siblings of e.Node.
			uint ui = (uint)i;
			AListNode<T> left = null, right = null;
			int leftCap = 0, rightCap = 0;
			if (ui-1u < (uint)_children.Length) {
				AutoClone(ref _children[ui-1u].Node, this, idx);
				left = _children[ui-1u].Node;
				leftCap = left.CapacityLeft;
			}
			if (ui + 1u < (uint)LocalCount) {
				AutoClone(ref _children[ui+1u].Node, this, idx);
				right = _children[ui+1u].Node;
				rightCap = right.CapacityLeft;
			}

			// If the siblings have enough capacity...
			if (leftCap + rightCap >= node.LocalCount)
			{
				// Unload data from 'node' into its siblings
				int oldRightCap = rightCap;
				uint rightAdjustment = 0, a;
				while (node.LocalCount > 0)
					if (leftCap >= rightCap) {
						Verify(left.TakeFromRight(node, idx) != 0);
						leftCap--;
					} else {
						Verify((a = right.TakeFromLeft(node, idx)) != 0);
						rightAdjustment += a;
						rightCap--;
					}
					
				if (rightAdjustment != 0) // if rightAdjustment==0, _children[i+1] might not exist
					_children[i+1].Index -= rightAdjustment;
					
				LLDelete(i, false);
				// Return true if this node has become undersized.
				return LocalCount < MaxNodeSize / 2;
			}
			else if (left != null || right != null)
			{	// Transfer an element from the fullest sibling so that 'node'
				// is no longer undersized.
				if (left == null)
					leftCap = int.MaxValue;
				if (right == null)
					rightCap = int.MaxValue;
				if (leftCap < rightCap) {
					Debug.Assert(i > 0);
					uint amt = node.TakeFromLeft(left, idx);
					Debug.Assert(amt > 0);
					_children[i].Index -= amt;
				} else {
					uint amt = node.TakeFromRight(right, idx);
					Debug.Assert(amt > 0);
					_children[i+1].Index += amt;
				}
			}
			return false;
		}
		private void LLDelete(int i, bool adjustIndexesAfterI)
		{
			int newLCount = LocalCount - 1;
			// if i=0 then this special int will be overwritten temporarily
			uint special = _children[0].Index;
			if (i < newLCount) {
				if (adjustIndexesAfterI)
				{
					uint indexAdjustment = _children[i + 1].Index - (i > 0 ? _children[i].Index : 0);
					AdjustIndexesAfter(i, -(int)indexAdjustment);
				}
				for (int j = i; j < newLCount; j++)
					_children[j] = _children[j + 1];
			}
			_children[newLCount] = new Entry { Node = null, Index = uint.MaxValue };
			_children[0].Index = special - 1; // decrement LocalCount
		}

		internal sealed override uint TakeFromRight(AListNode<T> sibling, AListIndexerBase<T> idx)
		{
			var right = (AListInner<T>)sibling;
			if (IsFrozen || right.IsFrozen)
				return 0;
			uint oldTotal = TotalCount;
			int oldLocal = LocalCount;
			var child = right.Child(0);
			LLInsert(oldLocal, child, 0);
			Debug.Assert(oldLocal > 0);
			_children[oldLocal].Index = oldTotal;
			right.LLDelete(0, true);
			AssertValid();
			right.AssertValid();
			if (idx != null) idx.NodeMoved(child, right, this);
			return child.TotalCount;
		}

		internal sealed override uint TakeFromLeft(AListNode<T> sibling, AListIndexerBase<T> idx)
		{
			Debug.Assert(!IsFrozen);
			var left = (AListInner<T>)sibling;
			if (IsFrozen || left.IsFrozen)
				return 0;
			var child = left.Child(left.LocalCount - 1);
			LLInsert(0, child, child.TotalCount);
			left.LLDelete(left.LocalCount - 1, false);
			AssertValid();
			left.AssertValid();
			if (idx != null) idx.NodeMoved(child, left, this);
			return child.TotalCount;
		}

		protected byte UserByte
		{
			get { return (byte)(_children[0].Index >> 24); }
			set { _children[0].Index = (_children[0].Index & 0xFFFFFF) | ((uint)value << 24); }
		}

		public sealed override bool IsFrozen
		{
			get { return (_children[0].Index & FrozenBit) != 0; }
		}
		public override void Freeze()
		{
			_children[0].Index |= FrozenBit;
		}

		public override int CapacityLeft { get { return MaxNodeSize - LocalCount; } }

		public override AListNode<T> DetachedClone()
		{
			Freeze();
			return new AListInner<T>(this);
		}

		public override AListNode<T> CopySection(uint index, uint count)
		{
			Debug.Assert(count > 0);
			if (index == 0 && count == TotalCount)
				return DetachedClone();
			
			return new AListInner<T>(this, index, count);
		}

		public void AutoEnlarge(int more)
		{
			int LC = LocalCount;
			if (LC + more > _children.Length)
			{
				_children = InternalList.CopyToNewArray(_children, LC, (LC + more + 3) & ~3);
				InitEmpties(LC);
			}
		}

		public virtual AListInner<T> Append(AListInner<T> other, int heightDifference, out AListNode<T> splitRight, AListIndexerBase<T> idx)
		{
			Debug.Assert(!IsFrozen);
			if (heightDifference != 0)
			{
				int i = LocalCount - 1;
				AutoClone(ref _children[i].Node, this, idx);
				var splitLeft = ((AListInner<T>)Child(i)).Append(other, heightDifference - 1, out splitRight, idx);
				return AutoHandleChildSplit(i, splitLeft, ref splitRight, idx);
			}

			int otherLC = other.LocalCount, LC = LocalCount;
			AutoEnlarge(otherLC);
			for (int i = 0; i < otherLC; i++)
				LLInsert(LC++, other.Child(i), 0);
			
			return AutoSplit(out splitRight);
		}

		public virtual AListInner<T> Prepend(AListInner<T> other, int heightDifference, out AListNode<T> splitRight, AListIndexerBase<T> idx)
		{
			Debug.Assert(!IsFrozen);
			if (heightDifference != 0)
			{
				int i = LocalCount - 1;
				AutoClone(ref _children[i].Node, this, idx);
				var splitLeft = ((AListInner<T>)Child(i)).Prepend(other, heightDifference - 1, out splitRight, idx);
				return AutoHandleChildSplit(i, splitLeft, ref splitRight, idx);
			}

			int otherLC = other.LocalCount;
			AutoEnlarge(otherLC);
			for (int i = 0; i < otherLC; i++) {
				var child = other.Child(i);
				LLInsert(i, child, child.TotalCount);
			}
			
			return AutoSplit(out splitRight);
		}

		public uint BaseIndexOf(AListNode<T> child)
		{
			int i = IndexOf(child);
			if (i <= 0)
				return (uint)i;
			return _children[i].Index;
		}
		public int IndexOf(AListNode<T> child)
		{
			for (int i = 0; i < _children.Length; i++)
				if (_children[i].Node == child)
					return i;
			return -1;
		}
	}
}
