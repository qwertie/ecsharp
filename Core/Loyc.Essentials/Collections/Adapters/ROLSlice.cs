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
		// C# generally can't infer the arguments
		//public static ROLSlice<T, TList> Slice<T, TList>(this TList list, int start, int count = int.MaxValue) where TList : IReadOnlyList<T>
		//	=> new ROLSlice<T, TList>(list, start, count);
		public static ROLSlice<T, IReadOnlyList<T>> Slice<T>(this IReadOnlyList<T> list, int start, int count = int.MaxValue)
			=> new ROLSlice<T, IReadOnlyList<T>>(list, start, count);
	}

	/// <summary>Adapter: a random-access range for a slice of an <see cref="IReadOnlyList{T}"/>.</summary>
	/// <typeparam name="T">Item type in the list</typeparam>
	/// <typeparam name="TList">List type</typeparam>
	public struct ROLSlice<T, TList> : IListSource<T>, IRange<T>, ICloneable<ROLSlice<T, TList>> where TList : IReadOnlyList<T>
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
			_count = CheckParam.ThrowIfStartOrCountAreBelowZeroAndLimitCountIfNecessary(start, count, list.Count);
		}
		[Obsolete("Not being used, planning to remove")]
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

		ROLSlice<T, TList> ICloneable<ROLSlice<T, TList>>.Clone() => this;
		IRange<T> ICloneable<IRange<T>>.Clone() => this;
		IFRange<T> ICloneable<IFRange<T>>.Clone() => this;
		IBRange<T> ICloneable<IBRange<T>>.Clone() => this;

		IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
		public RangeEnumerator<ROLSlice<T, TList>, T> GetEnumerator()
		{
			return new RangeEnumerator<ROLSlice<T, TList>, T>(this);
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
		public ROLSlice<T, TList> Slice(int start, int count = int.MaxValue)
		{
			var slice = new ROLSlice<T, TList>();
			slice._list = this._list;
			slice._start = this._start + start;
			slice._count = CheckParam.ThrowIfStartOrCountAreBelowZeroAndLimitCountIfNecessary(start, count, this._count);
			return slice;
		}
	}
}
