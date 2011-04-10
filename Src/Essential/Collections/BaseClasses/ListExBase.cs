using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Runtime
{
	public abstract class ListExBase<T> : ListSourceBase<T>, IListEx<T>
	{
		//public abstract int Count { get; }
		//public abstract T TryGet(int index, ref bool fail);
		public abstract bool TrySet(int index, T value);
		public abstract void Add(T item);
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

		public bool Remove(T item)
		{
			int i = IndexOf(item);
			if (i > -1) {
				RemoveAt(i);
				return true;
			}
			return false;
		}
	}
}
