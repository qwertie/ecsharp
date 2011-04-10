using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Loyc.Essentials;

namespace Loyc.Collections
{
	/// <summary>
	/// A read-only wrapper of a list that provides a view of a range of elements.
	/// Objects of this type are returned from <see cref="LCExtensions.Slice{T}"/>
	/// </summary>
	public class ListSourceSlice<T> : WrapperBase<IListSource<T>>, IListSource<T>
	{
		public ListSourceSlice(IListSource<T> inner, int start, int length) : base(inner)
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
		public ListSourceSlice(IList<T> inner, int start, int length)
			: this((IListSource<T>)inner.AsListSource(), start, length) { }

		protected int _start, _length;
		
		public T this[int index]
		{
			get {
				if ((uint)index < (uint)_length)
					return _obj[_start + index];
				throw new IndexOutOfRangeException();
			}
		}
		public T TryGet(int index, ref bool fail)
		{
			if ((uint)index < (uint)_length)
				return _obj.TryGet(_start + index, ref fail);
			else {
				fail = true;
				return default(T);
			}
		}
		public int Count
		{
			[DebuggerStepThrough]
			get { return _length; }
		}
		public IEnumerator<T> GetEnumerator()
		{
 			for (int i = 0; i < _length; i++)
				yield return _obj[_start + i];
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
 			return GetEnumerator();
		}
		public Iterator<T> GetIterator()
		{
			return GetEnumerator().AsIterator();
		}

		public int SliceStart
		{
			[DebuggerStepThrough]
			get { return _start; }
		}
		public IListSource<T> OriginalSource
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
	}
}
