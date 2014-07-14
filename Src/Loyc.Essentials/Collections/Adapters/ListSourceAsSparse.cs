using Loyc.Collections.Impl;
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

	/// <summary>Adapter from <see cref="IListSource{T}"/> to <see cref="ISparseListSource{T}"/>.</summary>
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
		public T NextHigherItem(ref int? index)
		{
			if (index == null || index.Value < 0)
				index = 0;
			else if (index == int.MaxValue) {
				index = null;
				return default(T);
			} else
				index++;

			bool fail;
			var result = _list.TryGet(index.Value, out fail);
			if (fail) index = null;
			return result;
		}
		public T NextLowerItem(ref int? index)
		{
			if (index == null || index >= Count)
				index = Count - 1;
			else if (index == int.MinValue) {
				index = null;
				return default(T);
			} else
				index--;

			bool fail;
			var result = _list.TryGet(index.Value, out fail);
			if (fail) index = null;
			return result;
		}
	}
}
