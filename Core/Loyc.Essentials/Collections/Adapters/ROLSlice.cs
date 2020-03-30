using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Math;

namespace Loyc.Collections
{
	public static partial class ListExt
	{
		public static ROLSlice<TList, T> Slice<TList, T>(this TList list, int start, int count = int.MaxValue) where TList : IReadOnlyList<T>
			=> new ROLSlice<TList, T>(list, start, count);
	}

	/// <summary>Adapter: a random-access range for a slice of an <see cref="IReadOnlyList{T}"/>.</summary>
	/// <typeparam name="T">Item type in the list</typeparam>
	/// <typeparam name="TList">List type</typeparam>
	public struct ROLSlice<TList, T> : IListSource<T>, IRange<T>, ICloneable<ROLSlice<TList,T>> where TList : IReadOnlyList<T>
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
		public ROLSlice(TList list, int start, int count = int.MaxValue)
		{
			_list = list;
			_start = start;
			_count = count;
			if (start < 0) throw new ArgumentException("The start index was below zero.");
			if (count < 0) throw new ArgumentException("The count was below zero.");
			if (count > _list.Count - start)
				_count = System.Math.Max(_list.Count - start, 0);
		}
		public ROLSlice(TList list)
		{
			_list = list;
			_start = 0;
			_count = list.Count;
		}

		public int Count
		{
			get { return _count; }
		}
		public bool IsEmpty
		{
			get { return _count == 0; }
		}
		public T First
		{
			get { return this[0]; }
		}
		public T Last
		{
			get { return this[_count - 1]; }
		}

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
		public T PopLast(out bool empty)
		{
			if (_count != 0)
			{
				empty = false;
				_count--;
				return _list[_start + _count];
			}
			empty = true;
			return default(T);
		}

		ROLSlice<TList, T> ICloneable<ROLSlice<TList, T>>.Clone() => this;
		IRange<T> ICloneable<IRange<T>>.Clone() => this;
		IFRange<T> ICloneable<IFRange<T>>.Clone() => this;
		IBRange<T> ICloneable<IBRange<T>>.Clone() => this;

		IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
		public RangeEnumerator<ROLSlice<TList, T>, T> GetEnumerator()
		{
			return new RangeEnumerator<ROLSlice<TList, T>, T>(this);
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

		IRange<T> IListSource<T>.Slice(int start, int count) => Slice(start, count);
		public ROLSlice<TList, T> Slice(int start, int count = int.MaxValue)
		{
			if (start < 0) throw new ArgumentException("The start index was below zero.");
			if (count < 0) throw new ArgumentException("The count was below zero.");
			var slice = new ROLSlice<TList, T>();
			slice._list = this._list;
			slice._start = this._start + start;
			slice._count = count;
			if (slice._count > this._count - start)
				slice._count = System.Math.Max(this._count - start, 0);
			return slice;
		}
	}
}
