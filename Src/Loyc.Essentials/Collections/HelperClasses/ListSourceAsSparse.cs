using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>An adapter from <see cref="IListSource{T}"/> to <see cref="ISparseListSource{T}"/>.</summary>
	/// <seealso cref="LCExt.AsSparse{T}"/>
	public class ListSourceAsSparse<T> : ListSourceBase<T>, ISparseListSource<T>
	{
		private IListSource<T> list;

		public ListSourceAsSparse(Loyc.Collections.IListSource<T> list)
		{
			// TODO: Complete member initialization
			this.list = list;
		}
		public sealed override T TryGet(int index, out bool fail)
		{
			return list.TryGet(index, out fail);
		}
		public sealed override int Count
		{
			get { return list.Count; }
		}
		public IEnumerable<KeyValuePair<int, T>> Items
		{
			get { return list.WithIndexes(); }
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
