namespace Loyc.Collections.Impl
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Diagnostics;

	/// <summary>Internal implementation class. Shared code of non-sparse AList leaf nodes.</summary>
	[Serializable]
	public abstract class AListLeaf<K, T> : AListNode<K, T>
	{
		public const int DefaultMaxNodeSize = 64;

		protected internal InternalList<T> _list = InternalList<T>.Empty;

		public override long CountSizeInBytes(int sizeOfT, int sizeOfK) =>
			4 * IntPtr.Size + (LocalCount == 0 ? 0 : 3 * IntPtr.Size + sizeOfT * _list.Capacity);

		public AListLeaf(ushort maxNodeSize)
		{
			Debug.Assert(maxNodeSize >= 3);
			_maxNodeSize = maxNodeSize;
		}
		protected AListLeaf(ushort maxNodeSize, InternalList<T> list) : this(maxNodeSize)
		{
			_list = list;
		}

		public AListLeaf(AListLeaf<K, T> original)
		{
			_list = original._list.Clone();
			_maxNodeSize = original._maxNodeSize;
			_isFrozen = false;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public override bool IsLeaf => true;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public sealed override int LocalCount => _list.Count;
		public override uint TotalCount => (uint)_list.Count;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public sealed override bool IsFullLeaf => _list.Count >= _maxNodeSize;
		public override bool IsUndersized => _list.Count * 3 <= _maxNodeSize;
		public override int CapacityLeft => _maxNodeSize - LocalCount;

		public override T this[uint index] => _list[(int)index];

		public override void SetAt(uint index, T item, IAListTreeObserver<K, T> tob)
		{
			Debug.Assert(!_isFrozen);
			if (tob != null) {
				tob.ItemRemoved(_list[(int)index], this);
				tob.ItemAdded(item, this);
			}
			_list[(int)index] = item;
		}

		internal override uint TakeFromRight(AListNode<K, T> sibling, int localsToMove, IAListTreeObserver<K, T> tob)
		{
			Debug.Assert(localsToMove <= sibling.LocalCount && LocalCount + localsToMove <= _maxNodeSize);
			var right = (AListLeaf<K, T>)sibling;
			if (_isFrozen || right._isFrozen)
				return 0;
			
			EnsureCapacity(localsToMove);
			_list.AddRange(right._list.Slice(0, localsToMove));
			right._list.RemoveRange(0, localsToMove);
			if (tob != null)
				tob.ItemsMoved(_list, _list.Count - localsToMove, localsToMove, right, this);
			return (uint)localsToMove;
		}

		internal override uint TakeFromLeft(AListNode<K, T> sibling, int localsToMove, IAListTreeObserver<K, T> tob)
		{
			Debug.Assert(localsToMove <= sibling.LocalCount && LocalCount + localsToMove <= _maxNodeSize);
			var left = (AListLeaf<K, T>)sibling;
			if (_isFrozen || left._isFrozen)
				return 0;
			
			EnsureCapacity(localsToMove);
			int leftStart = left._list.Count - localsToMove;
			_list.InsertRange(0, left._list.Slice(leftStart, localsToMove));
			left._list.RemoveRange(leftStart, localsToMove);
			if (tob != null) 
				tob.ItemsMoved(_list, 0, localsToMove, left, this);
			return (uint)localsToMove;
		}

		public override T GetLastItem()
		{
			return _list.Last;
		}

		public override bool RemoveAt(uint index, uint count, IAListTreeObserver<K, T> tob)
		{
			Debug.Assert(!_isFrozen);

			if (tob != null) 
				tob.ItemsRemoved(_list, (int)index, (int)count, this);
			_list.RemoveRange((int)index, (int)count);
			return IsUndersized;
		}

		public override void Freeze()
		{
			_isFrozen = true;
		}

		//public Iterator<T> GetIterator(int start, int subcount)
		//{
		//    return _list.GetIterator(start, subcount);
		//}

		public void Sort(int start, int subcount, Comparison<T> comp)
		{
			Debug.Assert(!_isFrozen);
			_list.Sort(start, subcount, comp);
		}

		public int IndexOf(T item, int startIndex)
		{
			return _list.IndexOf(item, startIndex);
		}
		
		public override uint GetImmutableCount(bool _)
		{
			return IsFrozen ? (uint)LocalCount : 0;
		}

		protected void EnsureCapacity(int amountToInsert)
		{
			Debug.Assert(amountToInsert >= 0);
			int newSize = _list.Count + amountToInsert;
			if (newSize > _list.Capacity)
			{
				int maxCapacity = (newSize << 1) + 2, capacity;
				if (newSize <= _maxNodeSize) {
					for (capacity = _maxNodeSize; capacity > maxCapacity; capacity >>= 1) { }
				} else {
					capacity = newSize;
					Debug.Assert(false);
				}
				var newArray = InternalList.CopyToNewArray(_list.InternalArray, _list.Count, capacity);
				_list = new InternalList<T>(newArray, _list.Count);
			}
		}
	}

	/// <summary>
	/// Leaf node of <see cref="AList{T}"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[Serializable]
	internal class AListLeaf<T> : AListLeaf<int, T>
	{
		public AListLeaf(ushort maxNodeSize) : base(maxNodeSize) { }
		protected AListLeaf(ushort maxNodeSize, InternalList<T> list) : base(maxNodeSize, list) { }
		public AListLeaf(AListLeaf<T> frozen) : base(frozen) { }

		public override AListNode<int, T> DetachedClone()
		{
			return new AListLeaf<T>(this);
		}

		public override AListNode<int, T> CopySection(uint index, uint count, AListBase<int, T> list)
		{
			Debug.Assert((int)(count | index) > 0);
			if (index == 0 && count == _list.Count)
				return DetachedClone();

			return new AListLeaf<T>(_maxNodeSize, _list.CopySection((int)index, (int)count));
		}

		public override AListNode<int, T> Insert(uint index, T item, out AListNode<int, T> splitRight, IAListTreeObserver<int, T> tob)
		{
			Debug.Assert(!_isFrozen);

			if (_list.Count < _maxNodeSize)
			{
				EnsureCapacity(1);
				_list.Insert((int)index, item);
				splitRight = null;
				if (tob != null) tob.ItemAdded(item, this);
				return null;
			}
			else
			{
				// If user seems to be adding items at the end, split at the end so that
				// nodes _efficiently_ end up full; otherwise split down the middle.
				int divAt = (int)index == _list.Count ? _list.Count : (_list.Count >> 1);
				var left = new AListLeaf<T>(_maxNodeSize, _list.CopySection(0, divAt));
				var right = new AListLeaf<T>(_maxNodeSize, _list.CopySection(divAt, _list.Count - divAt));
				
				// Note: don't pass tob to left.Insert() or right.Insert() because 
				// parent node will send required notifications to tob instead.
				if (index < divAt)
					left.Insert(index, item, out splitRight, null);
				else
					right.Insert(index - (uint)divAt, item, out splitRight, null);
				Debug.Assert(splitRight == null);
				splitRight = right;
				return left;
			}
		}
		public override AListNode<int, T> InsertRange(uint index, IListSource<T> source, ref int sourceIndex, out AListNode<int, T> splitRight, IAListTreeObserver<int, T> tob)
		{
			Debug.Assert(!_isFrozen);

			// Note: (int)index may sometimes be negative, but the adjustedIndex is not.
			int adjustedIndex = (int)index + sourceIndex;
			int leftHere = (int)_maxNodeSize - _list.Count;
			int leftIns = source.Count - sourceIndex;
			Debug.Assert(leftIns > 0);
			if (leftHere > 1)
			{
				int amtToIns = Math.Min(leftHere, leftIns);
				var list = _list;
				EnsureCapacity(amtToIns);
				
				list.InsertRangeHelper(adjustedIndex, amtToIns);
				int sourceIndex2 = sourceIndex, i = adjustedIndex;
				while (i < adjustedIndex + amtToIns)
					list[i++] = source[sourceIndex2++];
				
				_list = list;
				splitRight = null;
				if (tob != null) tob.ItemsAdded(source.Slice(sourceIndex, amtToIns), this);
				sourceIndex = sourceIndex2;
				return null;
			}
			else
				return Insert((uint)adjustedIndex, source[sourceIndex++], out splitRight, tob);
		}
	}
}
