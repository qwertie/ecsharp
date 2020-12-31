using System;
using System.Collections.Generic;
using System.Diagnostics;
using Loyc.Math;

namespace Loyc.Collections.MutableListExtensionMethods
{
	public static partial class IListExt
	{
		public static ListSlice<T> Slice<T>(this IList<T> list, int start, int length = int.MaxValue)
		{
			return new ListSlice<T>(list, start, length);
		}
		public static ListSlice<T> Slice<T>(this IListAndListSource<T> list, int start, int length = int.MaxValue)
		{
			return new ListSlice<T>(list, start, length);
		}
	}
}

namespace Loyc.Collections
{
	/// <summary>
	/// Adapter: a wrapper of a list that provides a view of a range of elements.
	/// Objects of this type are returned from <see cref="ListExt.Slice{T}"/>
	/// </summary>
	/// <remarks>
	/// ListSlice provides both a <see cref="IList{T}"/> interface and a 
	/// <see cref="IRange{T}"/> interface, and it is important not to confuse them.
	/// The <see cref="IList{T}"/> interface allows you to insert and remove items
	/// from both the original list and the slice simultaneously. The 
	/// <see cref="IRange{T}"/> interface allows you to "Pop" items from the front
	/// and back, but this reduces the length of the slice only, not the original
	/// list.
	/// </remarks>
	public struct ListSlice<T> : IRange<T>, ICloneable<ListSlice<T>>, IListAndListSource<T>, ICollectionEx<T>, IArray<T>, IIsEmpty
	{
		public static readonly ListSlice<T> Empty = new ListSlice<T>();

		IList<T> _list;
		int _start, _count;

		/// <summary>Initializes a slice.</summary>
		/// <exception cref="ArgumentException">The start index was below zero.</exception>
		/// <remarks>The (start, count) range is allowed to be invalid, as long
		/// as 'start' and 'count' are zero or above. 
		/// <ul>
		/// <li>If 'start' is above the original Count, the Count of the new slice 
		/// is set to zero.</li>
		/// <li>if (start + count) is above the original Count, the Count of the new
		/// slice is reduced to <c>list.Count - start</c>. Note that the Count of 
		/// the slice will not increase if the list expands after the slice is 
		/// created.</li>
		/// </ul>
		/// </remarks>
		public ListSlice(IList<T> list, int start, int count = int.MaxValue)
		{
			_list = list;
			_start = start;
			_count = count;
			if (start < 0) CheckParam.ThrowBadArgument("The start index was below zero.");
			if (count < 0) CheckParam.ThrowBadArgument("The count was below zero.");
			if (count > _list.Count - start)
				_count = System.Math.Max(_list.Count - start, 0);
		}

		public ListSlice(IList<T> list)
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

		public T PopFirst(out bool fail)
		{
			if (_count != 0) {
				fail = false;
				_count--;
				return _list[_start++];
			}
			fail = true;
			return default(T);
		}
		public T PopLast(out bool fail)
		{
			if (_count != 0) {
				fail = false;
				_count--;
				return _list[_start + _count];
			}
			fail = true;
			return default(T);
		}

		IFRange<T>  ICloneable<IFRange<T>>.Clone() { return this; }
		IBRange<T>  ICloneable<IBRange<T>>.Clone() { return this; }
		IRange<T>   ICloneable<IRange<T>> .Clone() { return this; }
		ListSlice<T> ICloneable<ListSlice<T>>.Clone() { return this; }

		IEnumerator<T> IEnumerable<T>.GetEnumerator() { return GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		public RangeEnumerator<ListSlice<T>, T> GetEnumerator()
		{
			return new RangeEnumerator<ListSlice<T>, T>(this);
		}

		public T this[int index]
		{
			get {
				if ((uint)index < (uint)_count)
					return _list[_start + index];
				throw new ArgumentOutOfRangeException("index");
			}
			set {
				if ((uint)index < (uint)_count)
					_list[_start + index] = value;
				else
					throw new ArgumentOutOfRangeException("index");
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
			if ((uint)index < (uint)_count)
			{
				fail = false;
				return _list[_start + index];
			}
			fail = true;
			return default(T);
		}

		IRange<T> IListSource<T>.Slice(int start, int count) { return Slice(start, count); }
		public ListSlice<T> Slice(int start, int count = int.MaxValue)
		{
			if (start < 0)
				CheckParam.ThrowBadArgument("The start index was below zero.");
			if (count < 0)
				count = 0;
			var slice = new ListSlice<T>();
			slice._list = this._list;
			slice._start = this._start + start;
			slice._count = count;
			if (slice._count > this._count - start)
				slice._count = System.Math.Max(this._count - start, 0);
			return slice;
		}

		/// <summary>Returns the original list.</summary>
		/// <remarks>Ideally, to protect the list there would be no way to access
		/// its contents beyond the boundaries of the slice. However, the 
		/// reality in .NET today is that many methods accept "slices" in the 
		/// form of a triple (list, start index, count). In order to call such an
		/// old-style API using a slice, one must be able to extract the internal
		/// list and start index values.</remarks>
		public IList<T> InternalList { get { return _list; } }
		public int InternalStart { get { return _start; } }
		public int InternalStop { get { return _start + _count; } }

		#region IListEx<T> methods

		public bool IsReadOnly
		{
			get { return _list.IsReadOnly; }
		}
		
		public int IndexOf(T item)
		{
			EqualityComparer<T> comparer = EqualityComparer<T>.Default;
			int stop = _start + _count;
			for (int i = _start; i < stop; i++)
				if (comparer.Equals(item, _list[i]))
					return i - _start;
			return -1;
		}
		
		public void Insert(int index, T item)
		{
			if ((uint)index > (uint)_count) throw new IndexOutOfRangeException();
			_list.Insert(_start + index, item);
			++_count;
		}
		
		public void RemoveAt(int index)
		{
			if ((uint)index >= (uint)_count) throw new IndexOutOfRangeException();
			_list.RemoveAt(_start + index);
			--_count;
		}
		
		public void Add(T item)
		{
			_list.Insert(_start + _count, item);
			++_count;
		}
		
		public void Clear()
		{
			for (int i = System.Math.Min(_start+_count, _list.Count)-1; i >= _start; i--)
				_list.RemoveAt(i);
		}
		
		public bool Contains(T item)
		{
			return IndexOf(item) > -1;
		}
		
		public void CopyTo(T[] array, int arrayIndex)
		{
			int space = array.Length - arrayIndex;
			int count = Count;
			if (space < count) {
				if ((uint)arrayIndex >= (uint)count)
					CheckParam.ThrowOutOfRange("arrayIndex");
				else
					CheckParam.ThrowBadArgument(nameof(array), "CopyTo: array is too small ({0} < {1})", space, count);
			}
			
			for (int i = 0; i < count; i++)
				array[arrayIndex + i] = this[i];
		}
		
		public bool Remove(T item)
		{
			int i = IndexOf(item);
			if (i > -1) {
				RemoveAt(i);
				return true;
			}
			return false;
		}
		
		public bool TrySet(int index, T value)
		{
			if ((uint)index < (uint)_count) {
				int index2 = index + _start;
				// Check in case Count changed since the slice was constructed
				if ((uint)index2 < (uint)_list.Count) {
					_list[index2] = value;
					return true;
				}
			}
			return false;
		}

		public void AddRange(IReadOnlyCollection<T> list)
		{
			ListExt.AddRange(this, list);
		}
		public void AddRange(IEnumerable<T> list)
		{
			ListExt.AddRange(this, list);
		}

		#endregion
	}

#if false
	[Serializable]
	public class ListSlice<T> : WrapperBase<IList<T>>, IListEx<T>
	{
		/// <summary>Initializes a ListSourceSlice object which provides a view on part of another list.</summary>
		/// <param name="list">A list to wrap (must not be null).</param>
		/// <param name="start">An index into the original list. this[0] will refer to that index.
		/// This cannot be negative, but it can be beyond the end of the list.</param>
		/// <param name="count">The number of elements to allow access to. If 
		/// start+length exceeds the list size, length is reduced immediately. 
		/// Thus, if the original list expands after the slice is created, the
		/// slice's Count never increases to match the original list.</param>
		/// <exception cref="IndexOutOfRangeException">'start' or 'length' was negative.</exception>
		public ListSlice(IList<T> list, int start, int count) : base(list)
		{
			_start = start;
			if (start < 0) throw new ArgumentException("The start index was below zero.");
			_count = count;
			if (count < 0) throw new ArgumentException("The count was below zero.");
			if (count > _obj.Count - _start)
				_count = Math.Max(_obj.Count - _start, 0);
		}

		protected int _start, _count;
		
		public T this[int index]
		{
			get {
				if ((uint)index < (uint)_count)
					return _obj[_start + index];
				throw new IndexOutOfRangeException();
			}
			set {
				if ((uint)index < (uint)_count)
					_obj[_start + index] = value;
				throw new IndexOutOfRangeException();
			}
		}
		public T TryGet(int index, ref bool fail)
		{
			if ((uint)index < (uint)_count) {
				int index2 = index + _start;
				// Check in case Count changed since the slice was constructed
				if ((uint)index2 < (uint)_obj.Count)
					return _obj[index2];
			}
			fail = true;
			return default(T);
		}
		public int Count
		{
			[DebuggerStepThrough]
			get { return _count; }
		}
		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 0; i < _count; i++)
				yield return _obj.TryGet(_start + i, default(T));
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
 			return GetEnumerator();
		}

		public int SliceStart
		{
			[DebuggerStepThrough]
			get { return _start; }
		}
		public IList<T> OriginalList
		{
			[DebuggerStepThrough]
			get { return _obj; }
		}
		
		/// <summary>Returns a sub-slice of this slice. This method cannot be used 
		/// to expand the range of the original slice.</summary>
		public ListSlice<T> Slice(int start, int length)
		{
			CheckParam.IsNotNegative("length", length);
			if (start > _count)
				length = 0;
			else if (length > _count - start)
				length = _count - start;
			return new ListSlice<T>(_obj, _start + start, length);
		}
		IRange<T> IListSource<T>.Slice(int start, int count)
		{
			return new Slice_<T>(this, start, count);
		}
		
		public bool IsReadOnly
		{
			get { return _obj.IsReadOnly; }
		}
		
		public int IndexOf(T item)
		{
			EqualityComparer<T> comparer = EqualityComparer<T>.Default;
			int stop = Math.Min(_start + _count, _obj.Count);
			for (int i = _start; i < stop; i++)
				if (comparer.Equals(item, _obj[i]))
					return i - _start;
			return -1;
		}
		
		public void Insert(int index, T item)
		{
			_obj.Insert(_start + index, item);
			++_count;
		}
		
		public void RemoveAt(int index)
		{
			_obj.RemoveAt(_start + index);
			--_count;
		}
		
		public void Add(T item)
		{
			_obj.Insert(_start + _count, item);
			++_count;
		}
		
		public void Clear()
		{
			for (int i = Math.Min(_start+_count, _obj.Count)-1; i >= _start; i--)
				_obj.RemoveAt(i);
		}
		
		public bool Contains(T item)
		{
			return IndexOf(item) > -1;
		}
		
		public void CopyTo(T[] array, int arrayIndex)
		{
			int space = array.Length - arrayIndex;
			int count = Count;
			if (space < count) {
				if ((uint)arrayIndex >= (uint)count)
					throw new ArgumentOutOfRangeException("arrayIndex");
				else
					throw new ArgumentException("CopyTo: array is too small ({0} < {1})".Localized(space, count));
			}
			
			for (int i = 0; i < count; i++)
				array[arrayIndex + i] = this[i];
		}
		
		public bool Remove(T item)
		{
			int i = IndexOf(item);
			if (i > -1) {
				RemoveAt(i);
				return true;
			}
			return false;
		}
		
		public bool TrySet(int index, T value)
		{
			if ((uint)index < (uint)_count) {
				int index2 = index + _start;
				// Check in case Count changed since the slice was constructed
				if ((uint)index2 < (uint)_obj.Count) {
					_obj[index2] = value;
					return true;
				}
			}
			return false;
		}
	}
#endif
}
