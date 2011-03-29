using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Loyc.Runtime
{
	public class ListSourceSlice<T> : IListSource<T>, IEnumerable<T>
	{
		public ListSourceSlice(IListSource<T> inner, int start, int length)
		{
			int count = inner.Count;
			if (length < 0)
				throw new IndexOutOfRangeException("ListSourceSlice: length can't be negative");
			if ((uint)start > (uint)count)
				throw new IndexOutOfRangeException("ListSourceSlice: start is out of range");
			if ((uint)(start + length) > (uint)count)
				throw new IndexOutOfRangeException("ListSourceSlice: end is out of range");

			_inner = inner;
			_start = start;
			_length = length;
		}
		public ListSourceSlice(IList<T> inner, int start, int length)
			: this((IListSource<T>)inner.ToListSource(), start, length) { }

		protected IListSource<T> _inner;
		protected int _start, _length;
		
		public T this[int index]
		{
			get {
				if ((uint)index < (uint)_length)
					return _inner[_start + index];
				throw new IndexOutOfRangeException();
			}
		}
		public T TryGet(int index, ref bool fail)
		{
			if ((uint)index < (uint)_length)
				return _inner.TryGet(_start + index, ref fail);
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
				yield return _inner[_start + i];
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
 			return GetEnumerator();
		}
		public Iterator<T> GetIterator()
		{
			return GetEnumerator().ToIterator();
		}

		public int SliceStart
		{
			[DebuggerStepThrough]
			get { return _start; }
		}
		public IListSource<T> OriginalSource
		{
			[DebuggerStepThrough]
			get { return _inner; }
		}
		public ListSourceSlice<T> Slice(int start, int length)
		{
			if ((uint)start > (uint)_length)
				throw new IndexOutOfRangeException("Slice(): start is out of range");
			if ((uint)(start + length) > (uint)_length)
				throw new IndexOutOfRangeException("Slice(): end is out of range");
			return new ListSourceSlice<T>(_inner, _start + start, length);
		}
	}
}
