using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>A random-access range for a slice of an <see cref="INegListSource{T}"/>.</summary>
	/// <typeparam name="T">Item type in the list</typeparam>
	/// <remarks>Although this slices a neg-list, the slice itself is an ordinary zero-indexed 
	/// <see cref="IListSource{T}"/>. It implements <see cref="INegListSource{T}"/> for 
	/// completeness, but its <c>Min</c> is 0.</remarks>
	public struct NegListSlice<T> : IRange<T>, ICloneable<NegListSlice<T>>, IIsEmpty, INegListSource<T>
	{
		INegListSource<T> _list;
		int _start, _count;
		
		/// <summary>Initializes a slice.</summary>
		/// <exception cref="ArgumentException">The start index was below zero.</exception>
		/// <remarks>The (start, count) range is allowed to be invalid, as long
		/// as 'start' is <c>Min</c> or above and 'count' is zero or above. 
		/// <ul>
		/// <li>If 'start' is above the original Count, the Count of the new slice 
		/// is set to zero.</li>
		/// <li>if (start + count) is above the original Count, the Count of the new
		/// slice is reduced to <c>list.Count - start</c>.</li>
		/// </ul>
		/// </remarks>
		public NegListSlice(INegListSource<T> list, int start, int count)
		{
			_list = list;
			_start = start;
			_count = count;
			if (start < list.Min) throw new ArgumentException("The start index was below Min.");
			if (count < 0) throw new ArgumentException("The count was below zero.");
			if (start + count - 1 > _list.Max)
				_count = (int)System.Math.Max((long)_list.Max + 1 - start, 0); // use long to avoid overflow if start==int.MaxValue && Max<0
		}

		public int Count
		{
			get { return _count; }
		}
		public bool IsEmpty
		{
			get { return _count == 0; }
		}
		public T Front
		{
			get { return this[0]; }
		}
		public T Back
		{
			get { return this[_count - 1]; }
		}

		public T PopFront(out bool empty)
		{
			if (_count != 0) {
				empty = false;
				_count--;
				return _list[_start++];
			}
			empty = true;
			return default(T);
		}
		public T PopBack(out bool empty)
		{
			if (_count != 0) {
				empty = false;
				_count--;
				return _list[_start + _count];
			}
			empty = true;
			return default(T);
		}

		IFRange<T> ICloneable<IFRange<T>>.Clone() { return Clone(); }
		IBRange<T> ICloneable<IBRange<T>>.Clone() { return Clone(); }
		IRange<T>  ICloneable<IRange<T>> .Clone() { return Clone(); }
		public NegListSlice<T> Clone() { return this; }

		IEnumerator<T> IEnumerable<T>.GetEnumerator() { return GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		public RangeEnumerator<NegListSlice<T>,T> GetEnumerator()
		{
			return new RangeEnumerator<NegListSlice<T>, T>(this);
		}

		public T this[int index]
		{
			get { 
				if ((uint)index < (uint)_count)
					return _list[_start + index];
				throw new ArgumentOutOfRangeException("index");
			}
		}
		public T this[int index, T defaultValue]
		{
			get { 
				if ((uint)index < (uint)_count) {
					bool fail;
					var r = _list.TryGet(_start + index, out fail);
					return fail ? defaultValue : r;
				}
				return defaultValue;
			}
		}
		public T TryGet(int index, out bool fail)
		{
			if ((uint)index < (uint)_count)
				return _list.TryGet(_start + index, out fail);
			fail = true;
			return default(T);
		}

		IRange<T> IListSource<T>.Slice(int start, int count) { return Slice(start, count); }
		IRange<T> INegListSource<T>.Slice(int start, int count) { return Slice(start, count); }
		public NegListSlice<T> Slice(int start, int count)
		{
			if (start < 0) throw new ArgumentException("The start index was below zero.");
			if (count < 0) throw new ArgumentException("The count was below zero.");
			var slice = new NegListSlice<T>();
			slice._list = this._list;
			slice._start = this._start + start;
			slice._count = count;
			if (slice._count > this._count - start)
				slice._count = System.Math.Max(this._count - start, 0);
			return slice;
		}

		int INegListSource<T>.Min { get { return 0; } }
		int INegListSource<T>.Max { get { return Count - 1; } }
	}
}
