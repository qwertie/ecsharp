using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Loyc.Collections.Impl
{
	/// <summary>Internal implementation class. Leaf node of <see cref="SparseAList{T}"/>.</summary>
	/// <remarks>This node consists of a certain number of virtual slots (_totalCount)
	/// and a certain number of real slots (_list.Count). Node splitting/joining
	/// behavior is based entirely on the number of real slots. There can be any 
	/// number of empty spaces anywhere in the list. If there are empty spaces at 
	/// the beginning, <c>_list[0].Offset > 0</c>; if there are empty spaces at the 
	/// end, <c>_totalCount > _list.Last.Offset + 1</c>. <c>_list.Count == 0</c> 
	/// only if the entire list consists of empty space and there is only a single 
	/// node.</remarks>
	[Serializable]
	public class SparseAListLeaf<T> : AListLeafBase<int, T>
	{
		[Serializable, DebuggerDisplay("Offset = {Offset}, Item = {Item}")]
		protected internal struct Entry
		{
			public Entry(uint offset, T item) { Offset = offset; Item = item; }
			public T Item;
			public uint Offset;
		}
		protected internal InternalList<Entry> _list;
		protected uint _totalCount;

		public override long CountSizeInBytes(int sizeOfT, int sizeOfK) =>
			// We don't know the alignment of Entry; optimistically assume 4.
			5 * IntPtr.Size + (LocalCount == 0 ? 0 : 3 * IntPtr.Size + (sizeOfT + 4) * _list.Capacity);

		public SparseAListLeaf(ushort maxNodeSize) : this(maxNodeSize, InternalList<Entry>.Empty, 0) { }
		private SparseAListLeaf(ushort maxNodeSize, InternalList<Entry> list, uint totalCount)
		{
			Debug.Assert(maxNodeSize >= 3);
			_maxNodeSize = maxNodeSize;
			_list = list;
			_totalCount = totalCount;
		}

		public SparseAListLeaf(SparseAListLeaf<T> original)
		{
			_list = original._list.Clone();
			_maxNodeSize = original._maxNodeSize;
			_totalCount = original._totalCount;
			_isFrozen = false;
		}

		public override bool IsLeaf
		{
			get { return true; }
		}

		public override T GetLastItem()
		{
			if (_list.Count > 0 && _list.Last.Offset + 1 == _totalCount)
				return _list.Last.Item;
			else
				return default(T);
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public override uint TotalCount => _totalCount;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public override int LocalCount => _list.Count;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public override bool IsFullLeaf => _list.Count >= _maxNodeSize;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public override bool IsUndersized => _list.Count * 3 <= _maxNodeSize;

		static Func<Entry, uint, int> _binarySearchComp = (e, i) => e.Offset.CompareTo(i);

		private bool BinarySearch(uint index, out int i)
		{
			// TODO: optimize by writing specialized binary search method
			i = _list.BinarySearch(index, _binarySearchComp, false);
			if (i < 0) {
				i = ~i;
				return false;
			}
			return true;
		}

		public override T this[uint index]
		{
			get {
				int i;
				if (BinarySearch(index, out i))
					return _list[i].Item;
				else
					return default(T);
			}
		}

		public override void SetAt(uint index, T item, IAListTreeObserver<int, T> tob)
		{
			throw new NotSupportedException();
			//Debug.Assert(!IsFrozen);
			//int i;
			//if (BinarySearch(index, out i))
			//    _list.InternalArray[_list.Internalize(i)].Item = item;
			//else
			//    _list.Insert(i, new Entry(index, item));
		}

		int GetSectionRange(uint index, uint count, out int i2)
		{
			Debug.Assert(index + count >= index);
			int i1;
			BinarySearch(index, out i1);
			BinarySearch(index + count, out i2);
			return i1;
		}

		public override bool RemoveAt(uint index, uint count, IAListTreeObserver<int, T> tob)
		{
			Debug.Assert(!IsFrozen);
			Debug.Assert(count <= _totalCount);
			if (count == 0)
				return false;
			int i1, i2;
			i1 = GetSectionRange(index, count, out i2);
	
			_list.RemoveRange(i1, i2 - i1);
			AdjustOffsetsStartingAt(i1, ref _list, -(int)count);
			_totalCount -= count;

			return IsUndersized;
		}

		static void AdjustOffsetsStartingAt(int i, ref InternalList<Entry> list, int change)
		{
			for (; i < list.Count; i++)
				list.InternalArray[i].Offset += (uint)change;
		}

		internal override uint TakeFromRight(AListNode<int, T> sibling, int localsToMove, IAListTreeObserver<int, T> tob)
		{
			Debug.Assert(localsToMove <= sibling.LocalCount && LocalCount + localsToMove <= _maxNodeSize);
			var right = (SparseAListLeaf<T>)sibling;
			if (_isFrozen || right._isFrozen)
				return 0;
			
			uint spaceBeingMoved;
			int localStart = _list.Count;
			// Be greedy: when taking all items, we must also take empty space after them
			spaceBeingMoved = localsToMove == right._list.Count ? right.TotalCount : right._list[localsToMove].Offset;
			_list.AddRange(right._list.Slice(0, localsToMove));
			right._list.RemoveRange(0, localsToMove);
			AdjustOffsetsStartingAt(localStart, ref _list, (int)_totalCount);
			AdjustOffsetsStartingAt(0, ref right._list, -(int)spaceBeingMoved);
			
			right._totalCount -= spaceBeingMoved;
			_totalCount += spaceBeingMoved;
			//if (tob != null) tob.ItemMoved(item, right, this);
			return spaceBeingMoved;
		}

		internal override uint TakeFromLeft(AListNode<int, T> sibling, int localsToMove, IAListTreeObserver<int, T> tob)
		{
			Debug.Assert(localsToMove <= sibling.LocalCount && LocalCount + localsToMove <= _maxNodeSize);
			var left = (SparseAListLeaf<T>)sibling;
			if (_isFrozen || left._isFrozen)
				return 0;

			uint spaceBeingMoved;
			int startIndex = left._list.Count - localsToMove;
			// Be greedy: when taking all items, we must also take empty space before them
			uint startOffset = startIndex == 0 ? 0 : left._list[startIndex].Offset;
			spaceBeingMoved = left._totalCount - startOffset;
			AdjustOffsetsStartingAt(startIndex, ref left._list, -(int)startOffset);
			AdjustOffsetsStartingAt(0, ref _list, (int)spaceBeingMoved);
			var itemsToMove = left._list.Slice(left._list.Count - localsToMove, localsToMove);
			_list.InsertRange(0, itemsToMove);
			left._list.RemoveRange(startIndex, localsToMove);

			left._totalCount -= spaceBeingMoved;
			_totalCount += spaceBeingMoved;
			//if (tob != null) tob.ItemMoved(item, left, this);
			return spaceBeingMoved;
		}

		public override void Freeze()
		{
			_isFrozen = true;
		}

		public override int CapacityLeft { get { return _maxNodeSize - LocalCount; } }

		public override AListNode<int, T> DetachedClone()
		{
			return new SparseAListLeaf<T>(this);
		}

		public override AListNode<int, T> CopySection(uint index, uint count, AListBase<int, T> list)
		{
			int i1, i2;
			i1 = GetSectionRange(index, count, out i2);

			InternalList<Entry> section = _list.CopySection(i1, i2 - i1);
			AdjustOffsetsStartingAt(0, ref section, -(int)index);
			return new SparseAListLeaf<T>(_maxNodeSize, section, count);
		}

		public override uint GetImmutableCount(bool excludeSparse)
		{
			if (!IsFrozen)
				return 0;
			return excludeSparse ? (uint)LocalCount : TotalCount;
		}

		public override AListNode<int, T> Insert(uint index, T item, out AListNode<int, T> splitRight, IAListTreeObserver<int, T> tob)
		{
			throw new NotSupportedException();
		}
		public override AListNode<int, T> InsertRange(uint index, IListSource<T> source, ref int sourceIndex, out AListNode<int, T> splitRight, IAListTreeObserver<int, T> tob)
		{
			throw new NotSupportedException();
		}

		private void InsertSpace(uint index, int count)
		{
			int i;
			BinarySearch(index, out i);
			AdjustOffsetsStartingAt(i, ref _list, count);
			_totalCount += (uint)count;
		}

		internal override int DoSparseOperation(ref AListSparseOperation<T> op, int index, out AListNode<int, T> splitLeft, out AListNode<int, T> splitRight)
		{
			Debug.Assert(!IsFrozen);
			if (op.IsInsert)
				return DoInsert(ref op, index, out splitLeft, out splitRight);
			else
				return DoReplace(ref op, index, out splitLeft, out splitRight);
		}

		internal override T SparseGetNearest(ref int? index_, int direction)
		{
			uint index = index_.Value < 0 ? 0 : (uint)index_.Value;
			int i;
			if (!BinarySearch((uint)index, out i)) {
				if (direction < 0)
					i--;
				if (direction == 0 || (uint)i >= (uint)_list.Count) {
					index_ = null;
					return default(T);
				}
			}
			index_ = (int)_list[i].Offset;
			return _list[i].Item;
		}

		private int DoReplace(ref AListSparseOperation<T> op, int index, out AListNode<int, T> splitLeft, out AListNode<int, T> splitRight)
		{
			if (op.WriteEmpty)
			{
				int i1, i2;
				uint leftToReplace = (uint)(op.SourceCount - op.SourceIndex);
				uint startAt = (uint)(index + op.SourceIndex);
				i1 = GetSectionRange(startAt, leftToReplace, out i2);
				_list.RemoveRange(i1, i2 - i1);
				
				uint amtReplaced = System.Math.Min(leftToReplace, _totalCount - startAt);
				op.SourceIndex += (int)amtReplaced;
				
				splitLeft = splitRight = null;
				if (IsUndersized)
					splitLeft = this;

				return 0;
			}

			// Currently there is only one other replacement operation exposed on 
			// SparseAList, namely changing the value of a single item. I'll 
			// include code for changing multiple items at once, but not optimize 
			// that case. And no sparse support.
			Debug.Assert(op.SparseSource == null);

			splitLeft = splitRight = null;
			if (op.Source != null) {
				for (; op.SourceIndex < op.SourceCount; op.SourceIndex++) {
					op.Item = op.Source[op.SourceIndex];
					if (!ReplaceSingleItem(ref op, (uint)(index + op.SourceIndex))) {
						SplitLeaf(out splitLeft, out splitRight, false);
						return 0;
					}
				}
			} else {
				Debug.Assert(op.SourceIndex == 0 && op.SourceCount == 1);
				if (!ReplaceSingleItem(ref op, (uint)(index + op.SourceIndex))) {
					SplitLeaf(out splitLeft, out splitRight, false);
					return 0;
				}
				op.SourceIndex++;
			}
			return 0;
		}
		private bool ReplaceSingleItem(ref AListSparseOperation<T> op, uint index)
		{
			int i;
			if (BinarySearch(index, out i))
				_list.InternalArray[i].Item = op.Item;
			else {
				if (_list.Count >= _maxNodeSize)
					return false;
				_list.Insert(i, new Entry(index, op.Item));
			}
			return true;
		}

		private int DoInsert(ref AListSparseOperation<T> op, int index0, out AListNode<int, T> splitLeft, out AListNode<int, T> splitRight)
		{
			Debug.Assert(_totalCount + op.SourceCount >= _totalCount); // caller ensures list size does not overflow
			
			if (op.WriteEmpty)
			{
				// SourceIndex will be 0 because inserting empty space always finishes on the first try.
				Debug.Assert(op.SourceIndex == 0);
				InsertSpace((uint)index0, op.SourceCount);
				op.SourceIndex = op.SourceCount;
				splitLeft = splitRight = null;
				return op.SourceCount;
			}

			uint index = (uint)(index0 + op.SourceIndex);
			int i;
			BinarySearch(index, out i);

			int leftHere = _maxNodeSize - _list.Count;
			if (leftHere == 0)
			{
				SplitLeaf(out splitLeft, out splitRight, i == _list.Count);
				return 0; // return without inserting anything
			}

			if (op.Source == null)
			{
				// Special case: insert a single item
				Debug.Assert(op.SourceCount == 1);
				_list.AutoRaiseCapacity(1, _maxNodeSize);
				
				_list.Insert(i, new Entry(index, op.Item));
				AdjustOffsetsStartingAt(i + 1, ref _list, 1);
				_totalCount++;
				op.SourceIndex++;
				splitLeft = splitRight = null;
				return 1;
			}

			splitLeft = splitRight = null;

			if (op.SparseSource != null)
			{
				var source = op.SparseSource;
				var tempList = new InternalList<Entry>(leftHere);

				int? si;
				T item;
				for (int prev = op.SourceIndex - 1; ; prev = si.Value) {
					si = prev;
					item = source.NextHigherItem(ref si);
					if (si == null)
						break;
					tempList.Add(new Entry {
						Offset = (uint)(index0 + si.Value),
						Item = item
					});
					if (tempList.Count == leftHere)
						break;
				}
				_list.InsertRangeHelper(i, tempList.Count);
				for (int j = 0; j < tempList.Count; j++)
					_list[i + j] = tempList[j];

				int newSourceIndex = si == null ? source.Count : si.Value + 1;
				int virtualItemsInserted = newSourceIndex - op.SourceIndex;
				op.SourceIndex = newSourceIndex;

				AdjustOffsetsStartingAt(i + tempList.Count, ref _list, virtualItemsInserted);
				_totalCount += (uint)virtualItemsInserted;
				return virtualItemsInserted;
			}
			else
			{
				int c = op.SourceCount;
				int leftToInsert = c - op.SourceIndex;
				Debug.Assert(leftToInsert > 0);
				int amtToIns = System.Math.Min(leftHere, leftToInsert);
				_list.AutoRaiseCapacity(amtToIns, _maxNodeSize);
				_list.InsertRangeHelper(i, amtToIns);
				for (int j = 0; j < amtToIns; j++)
					_list[i + j] = new Entry(index + (uint)j, op.Source[op.SourceIndex + j]);
				AdjustOffsetsStartingAt(i + amtToIns, ref _list, amtToIns);
				_totalCount += (uint)amtToIns;

				op.SourceIndex += amtToIns;
				//if (tob != null) tob.AddingItems(slice, this, false);
				return amtToIns;
			}
		}

		private void SplitLeaf(out AListNode<int, T> splitLeft, out AListNode<int, T> splitRight, bool insertingAtFarRight)
		{
			if (insertingAtFarRight)
			{
				splitLeft = new SparseAListLeaf<T>(_maxNodeSize, _list.CopySection(0, _list.Count), TotalCount);
				// nodes aren't allowed to be empty, but the caller is promising to put something in it
				splitRight = new SparseAListLeaf<T>(_maxNodeSize, InternalList<Entry>.Empty, 0);
			}
			else
			{
				int divAt = insertingAtFarRight ? _list.Count : _list.Count >> 1;
				uint divOffset = _list[divAt].Offset;
				splitLeft = new SparseAListLeaf<T>(_maxNodeSize, _list.CopySection(0, divAt), divOffset);
				var rightSec = _list.CopySection(divAt, _list.Count - divAt);
				AdjustOffsetsStartingAt(0, ref rightSec, -(int)divOffset);
				splitRight = new SparseAListLeaf<T>(_maxNodeSize, rightSec, _totalCount - divOffset);
			}
		}

		public override uint GetRealItemCount() { return (uint)LocalCount; }

		public override int IndexOf(T item, int startIndex)
		{
			throw new NotImplementedException();
		}

		// This skips over the spaces and so might not be the right approach, but this is rarely used
		public override IEnumerator<T> GetEnumerator() => _list.Select(e => e.Item).GetEnumerator();
	}
}
