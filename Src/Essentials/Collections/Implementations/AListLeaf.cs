namespace Loyc.Collections.Impl
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Diagnostics;

	/// <summary>
	/// Leaf node of <see cref="AList{T}"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[Serializable]
	public class AListLeaf<T> : AListNode<T>
	{
		protected const int DefaultMaxInnerNodeSize = 8;
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
		
		public override AListInner<T> Insert(uint index, T item)
		{
			if (_list.Count < _maxNodeSize)
			{
				_list.AutoEnlarge(1, _maxNodeSize);
				_list.Insert((int)index, item);
				return null;
			}
			else
			{
				int divAt = _list.Count >> 1;
				var dlist = new DList<T>(_list);
				var left = new AListLeaf<T>(_maxNodeSize, dlist.Slice(0, divAt));
				var right = new AListLeaf<T>(_maxNodeSize, dlist.Slice(divAt, _list.Count - divAt));
				if (index <= divAt)
					left.Insert(index, item);
				else
					right.Insert(index - (uint)divAt, item);
				return new AListInner<T>(left, right, DefaultMaxInnerNodeSize);
			}
		}
		public override AListInner<T> Insert(uint index, IListSource<T> source, ref int sourceIndex)
		{
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
				return null;
			} else
				return Insert((uint)adjustedIndex, source[sourceIndex++]);
		}

		public sealed override int LocalCount
		{
			get { return _list.Count; }
		}

		public override T this[uint index]
		{
			get { return _list[(int)index]; }
			set { _list[(int)index] = value; }
		}

		internal sealed override void TakeFromRight(AListNode<T> child)
		{
			var right = (AListLeaf<T>)child;
			_list.PushLast(right._list.First);
			right._list.PopFirst(1);
		}

		internal sealed override void TakeFromLeft(AListNode<T> child)
		{
			var left = (AListLeaf<T>)child;
			_list.PushFirst(left._list.Last);
			left._list.PopLast(1);
		}

		public sealed override uint TotalCount
		{
			get { return (uint)_list.Count; }
		}

		public sealed override bool IsFullLeaf
		{
			get { return _list.Count >= _maxNodeSize; }
		}

		public override RemoveResult RemoveAt(uint index)
		{
			_list.RemoveAt((int)index);
			return _list.Count > _maxNodeSize / 3 ? RemoveResult.OK : RemoveResult.Underflow;
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

		public Iterator<T> GetIterator()
		{
			return _list.GetIterator();
		}
	}
}
