using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Loyc.Collections.Impl;

namespace Loyc.Collections
{
	public struct SparseListSourceSlice<T, TList> : ISparseListSource<T>, IFRange<T>, ICloneable<SparseListSourceSlice<T, TList>> where TList : ISparseListSource<T>
	{
		TList _list;
		int _start, _count;

		/// <summary>Initializes a slice.</summary>
		/// <exception cref="ArgumentException">The start index was below zero.</exception>
		/// <remarks>The (start, count) range is allowed to be invalid, as long
		/// as 'start' and 'count' are zero or above. 
		/// <ul>
		/// <li>If 'start' is above the original Count, the Count of the new slice 
		/// is set to zero.</li>
		/// <li>if (start + count) is above the original Count, the Count of the new
		/// slice is reduced to <c>list.Count - start</c>.</li>
		/// </ul>
		/// </remarks>
		public SparseListSourceSlice(TList list, int start, int count = int.MaxValue)
		{
			_list = list;
			_start = start;
			_count = CheckParam.ThrowIfStartOrCountAreBelowZeroAndLimitCountIfNecessary(start, count, list.Count);
		}

		public int Count => _count;
		public bool IsEmpty => _count == 0;

		public T First => this[0];

		public T PopFirst(out bool empty)
		{
			if (_count != 0)
			{
				empty = false;
				_count--;
				return _list[_start++];
			}
			empty = true;
			return default(T);
		}

		SparseListSourceSlice<T, TList> ICloneable<SparseListSourceSlice<T, TList>>.Clone() => this;
		IFRange<T> ICloneable<IFRange<T>>.Clone() => this;

		IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
		public RangeEnumerator<SparseListSourceSlice<T, TList>, T> GetEnumerator()
		{
			return new RangeEnumerator<SparseListSourceSlice<T, TList>, T>(this);
		}

		public T this[int index]
		{
			get {
				if ((uint)index < (uint)_count)
					return _list[_start + index];
				throw new ArgumentOutOfRangeException(nameof(index));
			}
		}
		public T this[int index, T defaultValue]
		{
			get {
				if ((uint)index < (uint)_count)
					return _list[_start + index];
				return defaultValue;
			}
		}
		public T TryGet(int index, out bool fail)
		{
			int i = _start + index;
			if (!(fail = (uint)index >= (uint)_count || (uint)i >= (uint)_list.Count))
				return _list[i];
			else
				return default(T);
		}

		IListSource<T> IListSource<T>.Slice(int start, int count)
		{
			var slice = Slice(start, count);
			return new Slice_<T>(_list, slice._start, slice._count);
		}
		ISparseListSource<T> ISparseListSource<T>.Slice(int start, int count) => Slice(start, count);
		public SparseListSourceSlice<T, TList> Slice(int start, int count = int.MaxValue)
		{
			var slice = new SparseListSourceSlice<T, TList>();
			slice._list = this._list;
			slice._start = this._start + start;
			slice._count = CheckParam.ThrowIfStartOrCountAreBelowZeroAndLimitCountIfNecessary(start, count, this._count);
			return slice;
		}

		public T NextHigherItem(ref int? index)
		{
			index += _start;
			T next = _list.NextHigherItem(ref index);
			index -= _start;
			if (index >= _count)
			{
				index = null;
				return default;
			}
			return next;
		}

		public T NextLowerItem(ref int? index)
		{
			index += _start;
			T next = _list.NextLowerItem(ref index);
			index -= _start;
			if (index < 0)
			{
				index = null;
				return default;
			}
			return next;
		}

		public bool IsSet(int index) => _list.IsSet(_start + index);

		public IEnumerator<KeyValuePair<int, T>> GetItemEnumerator()
		{
			int? index = null;
			T item = NextHigherItem(ref index);
			while (index != null)
			{
				yield return new KeyValuePair<int, T>(index.Value, item);
				item = NextHigherItem(ref index);
			}
		}
	}
}
