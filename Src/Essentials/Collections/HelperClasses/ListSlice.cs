/*
 * Created by SharpDevelop.
 * User: Pook
 * Date: 4/12/2011
 * Time: 8:53 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
namespace Loyc.Collections
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using Loyc.Essentials;

	/// <summary>
	/// A read-only wrapper of a list that provides a view of a range of elements.
	/// Objects of this type are returned from <see cref="LCExt.Slice{T}"/>
	/// </summary>
	[Serializable]
	public class ListSlice<T> : WrapperBase<IList<T>>, IListEx<T>
	{
		/// <summary>Initializes a ListSourceSlice object which provides a view on part of another list.</summary>
		/// <param name="list">A list to wrap (must not be null).</param>
		/// <param name="start">An index into the original list. this[0] will refer to that index.
		/// This cannot be negative, but it can be beyond the end of the list.</param>
		/// <param name="length">The number of elements to allow access to. If 
		/// start+length exceeds the list size, length is reduced immediately. 
		/// Thus, if the original list expands after the slice is created, the
		/// slice's Count never increases to match the original list.</param>
		/// <exception cref="IndexOutOfRangeException">'start' or 'length' was negative.</exception>
		public ListSlice(IList<T> list, int start, int length) : base(list)
		{
			CheckParam.IsNotNegative("length", length);
			CheckParam.IsNotNegative("start", start);

			int count = list.Count;
			if (start > count)
				length = 0;
			else if (length > count - start)
				length = count - start;
			_start = start;
			_length = length;
		}

		protected int _start, _length;
		
		public T this[int index]
		{
			get {
				if ((uint)index < (uint)_length)
					return _obj[_start + index];
				throw new IndexOutOfRangeException();
			}
			set {
				if ((uint)index < (uint)_length)
					_obj[_start + index] = value;
				throw new IndexOutOfRangeException();
			}
		}
		public T TryGet(int index, ref bool fail)
		{
			if ((uint)index < (uint)_length) {
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
			get { return _length; }
		}
		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 0; i < _length; i++)
				yield return _obj.TryGet(_start + i, default(T));
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
 			return GetEnumerator();
		}
		public Iterator<T> GetIterator()
		{
			return GetEnumerator().AsIterator(); // TODO
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
			if (start > _length)
				length = 0;
			else if (length > _length - start)
				length = _length - start;
			return new ListSlice<T>(_obj, _start + start, length);
		}
		
		public bool IsReadOnly
		{
			get { return _obj.IsReadOnly; }
		}
		
		public int IndexOf(T item)
		{
			EqualityComparer<T> comparer = EqualityComparer<T>.Default;
			int stop = Math.Min(_start + _length, _obj.Count);
			for (int i = _start; i < stop; i++)
				if (comparer.Equals(item, _obj[i]))
					return i - _start;
			return -1;
		}
		
		public void Insert(int index, T item)
		{
			_obj.Insert(_start + index, item);
			++_length;
		}
		
		public void RemoveAt(int index)
		{
			_obj.RemoveAt(_start + index);
			--_length;
		}
		
		public void Add(T item)
		{
			_obj.Insert(_start + _length, item);
			++_length;
		}
		
		public void Clear()
		{
			for (int i = Math.Min(_start+_length, _obj.Count)-1; i >= _start; i--)
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
					throw new ArgumentException(Localize.From("CopyTo: array is too small ({0} < {1})", space, count));
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
			if ((uint)index < (uint)_length) {
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
}
