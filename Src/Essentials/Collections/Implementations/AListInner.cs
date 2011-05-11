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
			public uint Index;
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

		protected AListInner(AListInner<T> frozen)
		{
			Debug.Assert(frozen.IsFrozen);
			_children = InternalList.CopyToNewArray(frozen._children);
			_children[0].Index &= ~FrozenBit; // unfreeze the clone

			// Inform children that they are frozen
			for (int i = LocalCount - 1; i >= 0; i--)
				_children[i].Node.Freeze();
			AssertValid();
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

		private void InitEmpties(int at)
		{
			Debug.Assert(at == LocalCount);
			for (; at < _children.Length; at++)
				_children[at].Index = uint.MaxValue;
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
				int i = InternalList.BinarySearch(_children, LocalCount, index, Entry.Compare);
				return i >= 0 ? i : ~i - 1;
			}
			if (index < _children[2].Index)
				return (index >= _children[1].Index ? 1 : 0);
			else
				return 2 + (index >= _children[3].Index ? 1 : 0);
		}

		private int PrepareToInsert(uint index, out Entry e)
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

			// If the child is a full leaf, consider shifting an element to a sibling
			if (e.Node.IsFullLeaf)
			{
				AListNode<T> childL, childR;
				// Check the left sibling
				if (i > 0 && !(childL = _children[i - 1].Node).IsFullLeaf)
				{
					childL.TakeFromRight(e.Node);
					_children[i].Index++;
					e = GetEntry(i);
				}
				// Check the right sibling
				else if (i + 1 < _children.Length && (childR = _children[i + 1].Node) != null && !childR.IsFullLeaf)
				{
					childR.TakeFromLeft(e.Node);
					_children[i + 1].Index--;
				}
			}
			return i;
		}

		public override AListNode<T> Insert(uint index, T item, out AListNode<T> splitRight)
		{
			if (IsFrozen)
			{
				var clone = Clone();
				return clone.Insert(index, item, out splitRight) ?? clone;
			}

			Entry e;
			int i = PrepareToInsert(index, out e);

			// Perform the insert, and adjust base index of nodes that follow
			AssertValid();
			var splitLeft = e.Node.Insert(index - e.Index, item, out splitRight);
			AdjustIndexesAfter(i, 1);

			// Handle child split/clone
			if (splitLeft != null)
				splitLeft = HandleChildSplit(e, i, splitLeft, ref splitRight);
			AssertValid();
			return splitLeft;
		}
		
		private AListNode<T> HandleChildSplit(Entry e, int i, AListNode<T> splitLeft, ref AListNode<T> splitRight)
		{
			Debug.Assert(e.Node == _children[i].Node);
			
			_children[i].Node = splitLeft;
			if (splitRight == null)
			{	// Child was cloned, not split
				Debug.Assert(e.Node.IsFrozen);
				return null;
			}

			LLInsert(i + 1, splitRight, 0);
			_children[i + 1].Index = e.Index + splitLeft.TotalCount;

			// Does this node need to split too?
			if (_children.Length > MaxNodeSize)
				return SplitAt(_children.Length >> 1, out splitRight);
			else {
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
			right = new AListInner<T>(_children.AsListSource().Slice(divAt, _children.Length - divAt), _children[divAt].Index, MaxNodeSize);
			return new AListInner<T>(_children.AsListSource().Slice(0, divAt), 0, MaxNodeSize);
		}

		public override AListNode<T> Insert(uint index, IListSource<T> source, ref int sourceIndex, out AListNode<T> splitRight)
		{
			if (IsFrozen)
			{
				var clone = Clone();
				return clone.Insert(index, source, ref sourceIndex, out splitRight) ?? clone;
			}

			Entry e;
			int i = PrepareToInsert(index + (uint)sourceIndex, out e);

			// Perform the insert
			int oldSourceIndex = sourceIndex;
			AListNode<T> splitLeft;
			do
				splitLeft = e.Node.Insert(index - e.Index, source, ref sourceIndex, out splitRight);
			while (sourceIndex < source.Count && splitLeft == null);
			
			// Adjust base index of nodes that follow
			int change = sourceIndex - oldSourceIndex;
			AdjustIndexesAfter(i, change);

			// Handle child split/clone
			if (splitLeft != null)
				splitLeft = HandleChildSplit(e, i, splitLeft, ref splitRight);
			AssertValid();
			return splitLeft;
		}

		[Conditional("DEBUG")]
		protected void AssertValid()
		{
			Debug.Assert(LocalCount > 0 && LocalCount <= _children.Length);
			Debug.Assert(_children[0].Node != null);

			uint @base = 0;
			for (int i = 1; i < LocalCount; i++) {
				Debug.Assert(_children[i].Node != null);
				Debug.Assert(_children[i].Index == (@base += _children[i-1].Node.TotalCount));
			}
			for (int i = LocalCount; i < _children.Length; i++) {
				Debug.Assert(_children[i].Node == null);
				Debug.Assert(_children[i].Index == uint.MaxValue);
			}
		}

		private void LLInsert(int i, AListNode<T> child, uint indexAdjustment)
		{
			Debug.Assert(LocalCount <= MaxNodeSize);
			if (LocalCount == _children.Length)
			{
				_children = InternalList.CopyToNewArray(_children, _children.Length, Math.Min(_children.Length * 2, MaxNodeSize + 1));
				InitEmpties(LocalCount);
			}
			for (int j = _children.Length - 1; j > i; j--)
				_children[j] = _children[j - 1]; // insert room
			if (i == 0)
				_children[1].Index = 0;
			if (indexAdjustment != 0)
				AdjustIndexesAfter(i, (int)indexAdjustment);
			_children[i].Node = child;
			++_children[0].Index; // increment LocalCount
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
				var e = _children[LocalCount - 1];
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

		public override AListNode<T> SetAt(uint index, T item)
		{
			if (!IsFrozen) {
				int i = BinarySearch(index);
				var e = GetEntry(i);
				if (i != 0)
					index -= _children[i].Index;
				var clone = _children[i].Node.SetAt(index, item);
				if (clone != null)
					_children[i].Node = clone;
				return null;
			} else {
				var clone = Clone();
				clone.SetAt(index, item);
				return clone;
			}
		}

		protected Entry GetEntry(int i)
		{
			Entry e = _children[i];
			if (i == 0)
				e.Index = 0;
			return e;
		}
		public override AListNode<T> RemoveAt(uint index)
		{
			if (IsFrozen)
			{
				var clone = Clone();
				return clone.RemoveAt(index) ?? clone;
			}

			Debug.Assert((uint)index < (uint)TotalCount);
			int i = BinarySearch(index);
			var e = GetEntry(i);
			var result = e.Node.RemoveAt(index - e.Index);
			AdjustIndexesAfter(i, -1);
			if (result != null)
			{
				_children[i].Node = result;
				if (result.IsUndersized)
					result = HandleUndersized(i, result);
				else
					result = null;
			}
			AssertValid();
			return result;
		}

		private AListInner<T> HandleUndersized(int i, AListNode<T> node)
		{
			Debug.Assert(node == _children[i].Node);

			// Examine the fullness of the siblings of e.Node
			uint ui = (uint)i;
			AListNode<T> left = null, right = null;
			int leftCap = 0, rightCap = 0;
			if (ui-1u < (uint)_children.Length) {
				left = _children[ui-1u].Node;
				leftCap = left.CapacityLeft;
			}
			if (ui + 1u < (uint)LocalCount) {
				right = _children[ui + 1u].Node;
				rightCap = right.CapacityLeft;
			}

			// If the siblings have enough capacity...
			if (leftCap + rightCap >= node.LocalCount)
			{
				// Unload data from 'node' into its siblings
				int oldRightCap = rightCap;
				while (node.LocalCount > 0)
					if (leftCap >= rightCap) {
						left.TakeFromRight(node);
						leftCap--;
					} else {
						right.TakeFromLeft(node);
						rightCap--;
					}
					
				_children[i+1].Index -= (uint)(oldRightCap - rightCap);
					
				LLDelete(i, false);
				if (LocalCount < MaxNodeSize / 2)
					return this;
			}
			else
			{	// Transfer an element from the fullest sibling so that 'node'
				// is no longer not undersized.
				if (leftCap > rightCap) {
					Debug.Assert(i > 0);
					node.TakeFromLeft(left);
					_children[i].Index--;
				} else {
					node.TakeFromRight(right);
					_children[i+1].Index++;
				}
			}
			return null;
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
					AdjustIndexesAfter(i, (int)indexAdjustment);
				}
				for (int j = i; j < newLCount; j++)
					_children[j] = _children[j + 1];
			}
			_children[0].Index = special - 1; // decrement LocalCount
			_children[newLCount] = new Entry { Node = null, Index = uint.MaxValue };
		}

		internal sealed override void TakeFromRight(AListNode<T> sibling)
		{
			Debug.Assert(!IsFrozen);
			var right = (AListInner<T>)sibling;
			uint oldTotal = TotalCount;
			int oldLocal = LocalCount;
			LLInsert(oldLocal, right.Child(0), 0);
			Debug.Assert(oldLocal > 0);
			_children[oldLocal].Index = oldTotal;
			right.LLDelete(0, true);
			AssertValid();
			right.AssertValid();
		}

		internal sealed override void TakeFromLeft(AListNode<T> sibling)
		{
			Debug.Assert(!IsFrozen);
			var left = (AListInner<T>)sibling;
			var child = left.Child(left.LocalCount - 1);
			LLInsert(0, child, child.TotalCount);
			left.LLDelete(left.LocalCount - 1, false);
			AssertValid();
			left.AssertValid();
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

		protected virtual AListInner<T> Clone()
		{
			return new AListInner<T>(this);
		}
	}
}
