namespace Loyc.Collections.Impl
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Diagnostics;
	using Loyc.Essentials;

	/// <summary>
	/// Leaf node of <see cref="AList{T}"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[Serializable]
	public class AListLeaf<T> : AListNode<T>
	{
		protected InternalDList<T> _list = InternalDList<T>.Empty;

		public AListLeaf(byte maxNodeSize)
		{
			Debug.Assert(maxNodeSize >= 3);
			_maxNodeSize = maxNodeSize;
		}
		public AListLeaf(byte maxNodeSize, InternalDList<T> list) : this(maxNodeSize)
		{
			_list = list;
		}

		public AListLeaf(AListLeaf<T> frozen)
		{
			Debug.Assert(frozen.IsFrozen);
			_list = frozen._list.Clone();
			_maxNodeSize = frozen._maxNodeSize;
			_isFrozen = false;
			_userByte = frozen._userByte;
		}

		public override AListNode<T> Insert(uint index, T item, out AListNode<T> splitRight, AListIndexerBase<T> idx)
		{
			Debug.Assert(!_isFrozen);

			if (_list.Count < _maxNodeSize)
			{
				_list.AutoRaiseCapacity(1, _maxNodeSize);
				_list.Insert((int)index, item);
				splitRight = null;
				if (idx != null) idx.ItemAdded(item, this);
				return null;
			}
			else
			{
				int divAt = _list.Count >> 1;
				var left = new AListLeaf<T>(_maxNodeSize, _list.CopySection(0, divAt));
				var right = new AListLeaf<T>(_maxNodeSize, _list.CopySection(divAt, _list.Count - divAt));
				if (index <= divAt)
					left.Insert(index, item, out splitRight, idx);
				else
					right.Insert(index - (uint)divAt, item, out splitRight, idx);
				Debug.Assert(splitRight == null);
				splitRight = right;
				return left;
			}
		}
		public override AListNode<T> InsertRange(uint index, IListSource<T> source, ref int sourceIndex, out AListNode<T> splitRight, AListIndexerBase<T> idx)
		{
			Debug.Assert(!_isFrozen);

			// Note: (int)index may sometimes be negative, but the adjustedIndex is not.
			int adjustedIndex = (int)index + sourceIndex;
			int leftHere = (int)_maxNodeSize - _list.Count;
			int leftIns = source.Count - sourceIndex;
			Debug.Assert(leftIns > 0);
			if (leftHere > 2) {
				int amtToIns = Math.Min(leftHere, leftIns);
				_list.AutoRaiseCapacity(amtToIns, _maxNodeSize);
				var slice = source.Slice(sourceIndex, amtToIns);
				_list.InsertRange(adjustedIndex, slice);
				sourceIndex += amtToIns;
				splitRight = null;
				if (idx != null) idx.AddingItems(slice, this);
				return null;
			} else
				return Insert((uint)adjustedIndex, source[sourceIndex++], out splitRight, idx);
		}

		public sealed override int LocalCount
		{
			get { return _list.Count; }
		}

		public override T this[uint index]
		{
			get { return _list[(int)index]; }
		}
		public override void SetAt(uint index, T item, AListIndexerBase<T> idx)
		{
			Debug.Assert(!_isFrozen);
			if (idx != null) {
				idx.ItemRemoved(_list[(int)index], this);
				idx.ItemAdded(item, this);
			}
			_list[(int)index] = item;
		}

		internal sealed override uint TakeFromRight(AListNode<T> child, AListIndexerBase<T> idx)
		{
			var right = (AListLeaf<T>)child;
			if (IsFullLeaf || _isFrozen || right._isFrozen)
				return 0;
			T item = right._list.First;
			_list.PushLast(item);
			right._list.PopFirst(1);
			if (idx != null) idx.ItemMoved(item, right, this);
			return 1;
		}

		internal sealed override uint TakeFromLeft(AListNode<T> child, AListIndexerBase<T> idx)
		{
			var left = (AListLeaf<T>)child;
			if (IsFullLeaf || _isFrozen || left._isFrozen)
				return 0;
			T item = left._list.Last;
			_list.PushFirst(item);
			left._list.PopLast(1);
			if (idx != null) idx.ItemMoved(item, left, this);
			return 1;
		}

		public sealed override uint TotalCount
		{
			get { return (uint)_list.Count; }
		}

		public sealed override bool IsFullLeaf
		{
			get { return _list.Count >= _maxNodeSize; }
		}
		public override bool IsUndersized
		{
			get { return _list.Count * 3 <= _maxNodeSize; }
		}

		public override AListNode<T> RemoveAt(uint index, uint count, AListIndexerBase<T> idx)
		{
			Debug.Assert(!_isFrozen);

			if (idx != null) idx.RemovingItems(_list, (int)index, (int)count, this);
			_list.RemoveRange((int)index, (int)count);
			return (_list.Count << 1) <= _maxNodeSize && IsUndersized ? this : null;
		}

		public override bool IsFrozen
		{
			get { return _isFrozen; }
		}
		public override void Freeze()
		{
			_isFrozen = true;
		}

		public override int CapacityLeft { get { return _maxNodeSize - LocalCount; } }

		public Iterator<T> GetIterator(int start, int subcount)
		{
			return _list.GetIterator(start, subcount);
		}

		public override AListNode<T> DetachedClone()
		{
			_isFrozen = true;
			return new AListLeaf<T>(this);
		}

		public override AListNode<T> CopySection(uint index, uint count)
		{
			Debug.Assert((int)(count|index) > 0);
			if (index == 0 && count == _list.Count)
				return DetachedClone();

			return new AListLeaf<T>(_maxNodeSize, _list.CopySection((int)index, (int)count));
		}

		public void Sort(int start, int subcount, Comparison<T> comp)
		{
			Debug.Assert(!_isFrozen);
			_list.Sort(start, subcount, comp);
		}

		public int IndexOf(T item, int startIndex)
		{
			return _list.IndexOf(item, startIndex);
		}
	}
}
