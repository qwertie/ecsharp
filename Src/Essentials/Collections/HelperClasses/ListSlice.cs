/*
 * Created by SharpDevelop.
 * User: Pook
 * Date: 4/12/2011
 * Time: 8:53 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Loyc.Essentials;

namespace Loyc.Collections
{
	/// <summary>
	/// A read-only wrapper of a list that provides a view of a range of elements.
	/// Objects of this type are returned from <see cref="LCExtensions.Slice{T}"/>
	/// </summary>
	public class ListSlice<T> : WrapperBase<IList<T>>, IListEx<T>
	{
		/// <summary>Initializes a ListSourceSlice object which provides a view on part of another list.</summary>
		/// <param name="list">A list to wrap (must not be null).</param>
		/// <param name="start">An index into the original list. this[0] will refer to that index.</param>
		/// <param name="length">The number of elements to allow access to.</param>
		/// <exception cref="IndexOutOfRangeException">
		/// The range [start, start+length) was exceeded the range of the original list.
		/// </exception>
		public ListSlice(IList<T> inner, int start, int length) : base(inner)
		{
			int count = inner.Count;
			_start = start;
			_length = length;
			
			if (length < 0)
				throw new IndexOutOfRangeException("ListSourceSlice: length can't be negative");
			if ((uint)start > (uint)count)
				throw new IndexOutOfRangeException("ListSourceSlice: start is out of range");
			if ((uint)(start + length) > (uint)count)
				throw new IndexOutOfRangeException("ListSourceSlice: end is out of range");
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
				yield return this.TryGet(_start + i, default(T));
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
		public ListSourceSlice<T> Slice(int start, int length)
		{
			if ((uint)start > (uint)_length)
				throw new IndexOutOfRangeException("Slice(): start is out of range");
			if ((uint)(start + length) > (uint)_length)
				throw new IndexOutOfRangeException("Slice(): end is out of range");
			return new ListSourceSlice<T>(_obj, _start + start, length);
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
