using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Collections
{
	partial class ListExt
	{
		/// <summary>Adapts a list to the <see cref="IListSink{T}"/> interface.</summary>
		public static IListSink<T> AsSink<T>(this IList<T> list) => new ListAsSink<T, IList<T>>(list);
		public static ICollectionSink<T> AsSink<T>(this ICollection<T> list) => new CollectionAsSink<T, ICollection<T>>(list);
	}

	/// <summary>Helps implement extension method <see cref="ListExt.AsSink{T}(ICollection{T})"/>.</summary>
	public class CollectionAsSink<T, List> : WrapperBase<List>, ICollectionSink<T> where List : ICollection<T>
	{
		public CollectionAsSink(List list) : base(list) { }

		public void Add(T item) => _obj.Add(item);

		public void Clear() => _obj.Clear();

		public bool Remove(T item) => _obj.Remove(item);
	}

	/// <summary>Helps implement extension method <see cref="ListExt.AsSink{T}(IList{T})"/>.</summary>
	public class ListAsSink<T, List> : CollectionAsSink<T, List>, IListSink<T> where List : IList<T>
	{
		public ListAsSink(List list) : base(list) { }

		public T this[int index] { set => _obj[index] = value; }
	}
}
