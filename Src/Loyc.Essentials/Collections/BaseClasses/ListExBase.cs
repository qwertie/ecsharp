using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Collections
{
	[Serializable]
	public abstract class ListExBase<T> : ListSourceBase<T>, IListEx<T>
	{
		//public abstract int Count { get; }
		//public abstract T TryGet(int index, ref bool fail);
		public abstract bool TrySet(int index, T value);
		public abstract void Insert(int index, T item);
		public abstract void Clear();
		public abstract void RemoveAt(int index);

		public new T this[int index]
		{
			get {
				bool fail = false;
				T value = TryGet(index, ref fail);
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
			ListExt.AddRange(this, e);
		}
		public void AddRange(IListSource<T> s)
		{
			ListExt.AddRange(this, s);
		}
	}
}
