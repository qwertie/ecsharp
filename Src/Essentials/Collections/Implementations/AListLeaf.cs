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
		private byte _maxNodeSize;
		private bool _isFrozen;
		private byte _userByte;
		
		protected byte UserByte { get { return _userByte; } set { _userByte = value; } }

		public AListLeaf(byte maxNodeSize)
		{
			Debug.Assert(maxNodeSize >= 3);
			_maxNodeSize = maxNodeSize;
		}
		public AListLeaf(byte maxNodeSize, ListSourceSlice<T> slice) : this(maxNodeSize)
		{
			_list = new InternalDList<T>(slice.Count + 1);
			_list.PushLast(slice);
		}

		public AListLeaf(AListLeaf<T> frozen)
		{
			Debug.Assert(frozen.IsFrozen);
			_list = frozen._list.Clone();
			_maxNodeSize = frozen._maxNodeSize;
			_isFrozen = false;
			_userByte = frozen._userByte;
		}
		
		public override AListNode<T> Insert(uint index, T item, out AListNode<T> splitRight)
		{
			if (_isFrozen)
			{
				var clone = Clone();
				return clone.Insert(index, item, out splitRight) ?? clone;
			}
			if (_list.Count < _maxNodeSize)
			{
				_list.AutoEnlarge(1, _maxNodeSize);
				_list.Insert((int)index, item);
				splitRight = null;
				return null;
			}
			else
			{
				int divAt = _list.Count >> 1;
				var dlist = new DList<T>(_list);
				var left = new AListLeaf<T>(_maxNodeSize, dlist.Slice(0, divAt));
				var right = new AListLeaf<T>(_maxNodeSize, dlist.Slice(divAt, _list.Count - divAt));
				if (index <= divAt)
					left.Insert(index, item, out splitRight);
				else
					right.Insert(index - (uint)divAt, item, out splitRight);
				Debug.Assert(splitRight == null);
				splitRight = right;
				return left;
			}
		}
		public override AListNode<T> Insert(uint index, IListSource<T> source, ref int sourceIndex, out AListNode<T> splitRight)
		{
			if (_isFrozen)
			{
				var clone = Clone();
				return clone.Insert(index, source, ref sourceIndex, out splitRight) ?? clone;
			}

			// Note: (int)index may sometimes be negative, but the adjustedIndex is not.
			int adjustedIndex = (int)index + sourceIndex;
			int leftHere = (int)_maxNodeSize - _list.Count;
			int leftIns = source.Count - sourceIndex;
			Debug.Assert(leftIns > 0);
			if (leftHere > 2) {
				int amtToIns = Math.Min(leftHere, leftIns);
				_list.AutoEnlarge(amtToIns, _maxNodeSize);
				_list.InsertRange(adjustedIndex, source.Slice(sourceIndex, amtToIns));
				sourceIndex += amtToIns;
				splitRight = null;
				return null;
			} else
				return Insert((uint)adjustedIndex, source[sourceIndex++], out splitRight);
		}

		public sealed override int LocalCount
		{
			get { return _list.Count; }
		}

		public override T this[uint index]
		{
			get { return _list[(int)index]; }
		}
		public override AListNode<T> SetAt(uint index, T item)
		{
			if (!_isFrozen) {
				_list[(int)index] = item;
				return null;
			} else {
				var clone = Clone();
				clone.SetAt(index, item);
				return clone;
			}
		}

		internal sealed override uint TakeFromRight(AListNode<T> child)
		{
			var right = (AListLeaf<T>)child;
			if (IsFullLeaf || _isFrozen || right._isFrozen)
				return 0;
			_list.PushLast(right._list.First);
			right._list.PopFirst(1);
			return 1;
		}

		internal sealed override uint TakeFromLeft(AListNode<T> child)
		{
			var left = (AListLeaf<T>)child;
			if (IsFullLeaf || _isFrozen || left._isFrozen)
				return 0;
			_list.PushFirst(left._list.Last);
			left._list.PopLast(1);
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

		public override AListNode<T> RemoveAt(uint index, uint count)
		{
			if (_isFrozen)
			{
				var clone = Clone();
				return clone.RemoveAt(index, count) ?? clone;
			}
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

		public override AListNode<T> Clone()
		{
			return new AListLeaf<T>(this);
		}
	}
}
