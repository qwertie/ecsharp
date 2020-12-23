/*
 * Created by SharpDevelop.
 * User: Pook
 * Date: 4/10/2011
 * Time: 9:04 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Linq;
using System.Collections.Generic;

namespace Loyc.Collections
{
	public static partial class LCExt
	{
		/// <summary>Converts any IListSource{T} object to a read-only IList{T}.</summary>
		/// <remarks>This method is named "AsList" and not "ToList" because
		/// because, in contrast to methods like ToArray(), it does not make a copy
		/// of the sequence, although it does create a new wrapper object if <c>c</c>
		/// does not implement <see cref="IList{T}"/>.</remarks>
		public static IList<T> AsList<T>(this IListSource<T> c)
		{
			if (c == null)
				return null;
			var list = c as IList<T>;
			if (list != null)
				return list;
			return new ListSourceAsList<T>(c);
		}
	}

	/// <summary>Adapter: a read-only wrapper that implements IList(T) and 
	/// IListSource(T), returned from <see cref="LCExt.AsList{T}"/>.</summary>
	[Serializable]
	public sealed class ListSourceAsList<T> : WrapperBase<IListSource<T>>, IListAndListSource<T>
	{
		public ListSourceAsList(IListSource<T> obj) : base(obj) { }

		#region IList<T> Members

		public int IndexOf(T item)
		{
			return _obj.IndexOf(item);
		}
		public void Insert(int index, T item)
		{
			throw new NotSupportedException("Collection is read-only.");
		}
		public void RemoveAt(int index)
		{
			throw new NotSupportedException("Collection is read-only.");
		}
		public T this[int index]
		{
			get { return _obj[index]; }
			set { throw new NotSupportedException("Collection is read-only."); }
		}
		public void Add(T item)
		{
			throw new NotSupportedException("Collection is read-only.");
		}
		public void Clear()
		{
			throw new NotSupportedException("Collection is read-only.");
		}

		public int Count => _obj.Count;
		
		public bool Contains(T item)
		{
			return _obj.Contains(item);
		}
		public void CopyTo(T[] array, int arrayIndex)
		{
			_obj.CopyTo(array, arrayIndex);
		}
		public bool IsReadOnly
		{
			get { return true; }
		}
		public bool Remove(T item)
		{
			throw new NotSupportedException("Collection is read-only.");
		}
		public IEnumerator<T> GetEnumerator()
		{
			return _obj.GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		public bool IsEmpty => _obj.Count == 0;
		public T TryGet(int index, out bool fail)
		{
			return _obj.TryGet(index, out fail);
		}
		IRange<T> IListSource<T>.Slice(int start, int count)
		{
			return Slice(start, count); 
		}
		public Slice_<T> Slice(int start, int count)
		{
			return new Slice_<T>(_obj, start, count); 
		}
	}
}
