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
		public T this[int index, T defaultValue]
		{
			get {
				if ((uint)index < (uint)_length)
					return _inner[_start + index, defaultValue];
				else
					return defaultValue;
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
		bool ISource<T>.Contains(T item)
		{
		    return Collections.Contains(this, item);
		}
		int IListSource<T>.IndexOf(T item)
		{
		    return Collections.IndexOf(this, item);
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
	}
}
