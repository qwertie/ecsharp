using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Loyc.Collections.Impl
{
	/// <summary>A base class for classes that wish to implement <see cref="IListEx{T}"/>.
	/// Provides default implementations for most of the methods.</summary>
	[Serializable]
	[DebuggerTypeProxy(typeof(CollectionDebugView<>)), DebuggerDisplay("Count = {Count}")]
	public abstract class ListExBase<T> : ListSourceBase<T>, IListEx<T>
	{
		//public abstract int Count { get; }
		//public abstract T TryGet(int index, out bool fail);
		public abstract bool TrySet(int index, T value);
		public abstract void Insert(int index, T item);
		public abstract void Clear();
		public abstract void RemoveAt(int index);

		public new T this[int index]
		{
			get {
				bool fail;
				T value = TryGet(index, out fail);
				if (fail)
					ThrowIndexOutOfRange(index);
				return value;
			}
			set {
				if (!TrySet(index, value))
					ThrowIndexOutOfRange(index);
			}
		}

		public void Add(T item)
		{
			Insert(Count, item);
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

		public int RemoveAll(Predicate<T> match)
		{
			return ListExt.RemoveAll(this, match);
		}
		public void AddRange(IEnumerable<T> e)
		{
			InsertRange(Count, e);
		}
		public void AddRange(IReadOnlyCollection<T> s)
		{
			InsertRange(Count, s);
		}
		public virtual void RemoveRange(int start, int count)
		{
			ListExt.RemoveRange(this, start, count);
		}
		public virtual void InsertRange(int index, IReadOnlyCollection<T> items)
		{
			ListExt.InsertRange(this, index, items);
		}
		public virtual void InsertRange(int index, IEnumerable<T> items)
		{
			var items2 = items as IReadOnlyCollection<T>;
			if (items2 != null)
				ListExt.InsertRange(this, index, items2);
			else
				ListExt.InsertRange(this, index, items.Buffered());
		}
	}
}
