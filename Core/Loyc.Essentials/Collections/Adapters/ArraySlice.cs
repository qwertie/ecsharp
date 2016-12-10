using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Math;

namespace Loyc.Collections
{
	public static partial class ListExt
	{
		public static ArraySlice<T> Slice<T>(this T[] list, int start, int length = int.MaxValue)
		{
			return new ArraySlice<T>(list, start, length);
		}
		//public static ArraySlice<T> Slice<T>(this T[] list, NumRange<int, MathI> range)
		//{
		//	return new ArraySlice<T>(list, range.Lo, range.Count);
		//}
	}

	/// <summary>Adapter: Provides access to a section of an array.</summary>
	public struct ArraySlice<T> : IMRange<T>, ICloneable<ArraySlice<T>>, IIsEmpty
	{
		T[] _list;
		int _start, _count;

		public static implicit operator ArraySlice<T>(T[] array) { return new ArraySlice<T>(array); }

		/// <summary>Initializes an array slice.</summary>
		/// <exception cref="ArgumentException">The start index was below zero.</exception>
		/// <remarks>The (start, count) range is allowed to be invalid, as long
		/// as 'start' is zero or above. 
		/// <ul>
		/// <li>If 'count' is below zero, or if 'start' is above the original Length, 
		/// the Count of the new slice is set to zero.</li>
		/// <li>if (start + count) is above the original Length, the Count of the new
		/// slice is reduced to <c>list.Length - start</c>.</li>
		/// </ul>
		/// </remarks>
		public ArraySlice(T[] list, int start, int count)
		{
			_list = list;
			_start = start;
			_count = count;
			if (start < 0) throw new ArgumentException("The start index was below zero.");
			if (count < 0) throw new ArgumentException("The count was below zero.");
			if (count > _list.Length - start)
				_count = System.Math.Max(_list.Length - start, 0);
		}
		public ArraySlice(T[] list)
		{
			_list = list;
			_start = 0;
			_count = list.Length;
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
			set { this[0] = value; }
		}
		public T Last
		{
			get { return this[_count - 1]; }
			set { this[_count - 1] = value; }
		}

		public T PopFirst(out bool empty)
		{
			if (_count != 0) {
				empty = false;
				_count--;
				return _list[_start++];
			}
			empty = true;
			return default(T);
		}
		public T PopLast(out bool empty)
		{
			if (_count != 0) {
				empty = false;
				_count--;
				return _list[_start + _count];
			}
			empty = true;
			return default(T);
		}

		IFRange<T> ICloneable<IFRange<T>>.Clone() { return this; }
		IBRange<T> ICloneable<IBRange<T>>.Clone() { return this; }
		IRange<T> ICloneable<IRange<T>>.Clone() { return this; }
		ArraySlice<T> ICloneable<ArraySlice<T>>.Clone() { return this; }

		IEnumerator<T> IEnumerable<T>.GetEnumerator() { return GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		public RangeEnumerator<ArraySlice<T>,T> GetEnumerator()
		{
			return new RangeEnumerator<ArraySlice<T>,T>(this);
		}

		public T this[int index]
		{
			get { 
				if ((uint)index < (uint)_count)
					return _list[_start + index];
				throw new IndexOutOfRangeException();
			}
			set {
				if ((uint)index < (uint)_count)
					_list[_start + index] = value;
				else
					throw new IndexOutOfRangeException();
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
			if ((uint)index < (uint)_count) {
				fail = false;
				return _list[_start + index];
			}
			fail = true;
			return default(T);
		}
		IRange<T> IListSource<T>.Slice(int start, int count) { return Slice(start, count); }
		public ArraySlice<T> Slice(int start, int count = int.MaxValue)
		{
			if (start < 0)
				throw new ArgumentException("The start index was below zero.");
			if (count < 0)
				count = 0;
			var slice = new ArraySlice<T>();
			slice._list = this._list;
			slice._start = this._start + start;
			slice._count = count;
			if (slice._count > this._count - start)
				slice._count = System.Math.Max(this._count - start, 0);
			return slice;
		}

		public T[] ToArray()
		{
			var array = new T[Count];
			for (int i = 0; i < array.Length; i++)
				array[i] = _list[_start + i];
			return array;
		}

		/// <summary>Returns the original array.</summary>
		/// <remarks>Ideally, to protect the array there would be no way to access
		/// its contents beyond the boundaries of the slice. However, the 
		/// reality in .NET today is that many methods accept "slices" in the 
		/// form of a triple (list, start index, count). In order to call such an
		/// old-style API using a slice, one must be able to extract the internal
		/// list and start index values.</remarks>
		public T[] InternalList { get { return _list; } }
		public int InternalStart { get { return _start; } }
		public int InternalStop { get { return _start + _count; } }
	}
}
