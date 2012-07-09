using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Loyc.Essentials;

namespace Loyc.Collections
{
	using System;

	/// <summary>
	/// A read-only wrapper of a list that provides a view of a range of elements.
	/// Objects of this type are returned from <see cref="LCExt.Slice{T}"/>
	/// </summary>
	#if !WindowsCE
	[DebuggerTypeProxy(typeof(ListSourceDebugView<>)), DebuggerDisplay("Count = {Count}")]
	#endif
	public class ListSourceSlice<T> : WrapperBase<IListSource<T>>, IListSource<T>
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
		public ListSourceSlice(IListSource<T> list, int start, int length) : base(list)
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
			return GetIterator().AsEnumerator();
 			//for (int i = 0; i < _length; i++)
			//	yield return _obj.TryGet(_start + i, default(T));
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
 			return GetEnumerator();
		}
		public Iterator<T> GetIterator()
		{
			int i = _start, stop = i + _length;
			IListSource<T> list = _obj;
			
			return delegate(ref bool ended)
			{
				if (i < stop)
					return list.TryGet(i++, ref ended);
				ended = true;
				return default(T);
			};
		}

		public int SliceStart
		{
			[DebuggerStepThrough]
			get { return _start; }
		}
		public IListSource<T> OriginalList
		{
			[DebuggerStepThrough]
			get { return _obj; }
		}

		/// <summary>Returns a sub-slice of this slice. This method cannot be used 
		/// to expand the range of the original slice.</summary>
		public ListSourceSlice<T> Slice(int start, int length)
		{
			CheckParam.IsNotNegative("length", length);
			if (start > _length)
				length = 0;
			else if (length > _length - start)
				length = _length - start;
			return new ListSourceSlice<T>(_obj, _start + start, length);
		}
	}
}
