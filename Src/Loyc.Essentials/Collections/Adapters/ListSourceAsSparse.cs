using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	public static partial class LCExt
	{
		/// <summary>Treats a non-sparse list as a read-only sparse list with no empty
		/// spaces.</summary>
		public static ListSourceAsSparse<T> AsSparse<T>(this IListSource<T> list)
		{
			return new ListSourceAsSparse<T>(list);
		}
		/// <summary>Returns <c>list</c> itself. This overload exists to prevent you from 
		/// accidentally wrapping a sparse list in <see cref="ListSourceAsSparse{T}"/>,
		/// which would block access to knowledge of any empty spaces in the list.</summary>
		public static ISparseListSource<T> AsSparse<T>(this ISparseListSource<T> list)
		{
			return list;
		}
	}

	/// <summary>An adapter from <see cref="IListSource{T}"/> to <see cref="ISparseListSource{T}"/>.</summary>
	/// <seealso cref="LCExt.AsSparse{T}"/>
	public class ListSourceAsSparse<T> : ListSourceBase<T>, ISparseListSource<T>
	{
		private IListSource<T> _list;

		public ListSourceAsSparse(Loyc.Collections.IListSource<T> list)
		{
			_list = list;
		}
		public sealed override T TryGet(int index, out bool fail)
		{
			return _list.TryGet(index, out fail);
		}
		public sealed override int Count
		{
			get { return _list.Count; }
		}
		public IEnumerable<KeyValuePair<int, T>> Items
		{
			get { return _list.WithIndexes(); }
		}
		public bool IsSet(int index)
		{
			return (uint)index < (uint)Count;
		}
		public new System.Collections.IEnumerator GetEnumerator()
		{
			return GetEnumerator();
		}
		public int? NextHigher(int index)
		{
			if ((uint)(index + 1) < (uint)Count)
				return index + 1;
			else if (index < 0)
				return 0;
			else
				return null;
		}
		public int? NextLower(int index)
		{
			if ((uint)(index - 1) < (uint)Count)
				return index - 1;
			else if (index > Count && index > 0)
				return Count - 1;
			else
				return null;
		}
	}
}
