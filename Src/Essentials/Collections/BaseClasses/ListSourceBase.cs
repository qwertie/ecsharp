using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Loyc.Collections
{
	/// <summary>Helps you implement sources (read-only collections) by providing
	/// default implementations for most methods of IList(T) and
	/// IListSource(T).</summary>
	/// <remarks>
	/// You only need to implement two methods yourself:
	/// <code>
	///     public abstract int Count { get; }
	///     public abstract T TryGet(int index, ref bool fail);
	/// </code>
	/// </remarks>
	[Serializable]
	public abstract class ListSourceBase<T> : SourceBase<T>, IListSource<T>, IList<T>
	{
		#region IListSource<T> Members

		public abstract T TryGet(int index, ref bool fail);

		public T this[int index]
		{ 
			get {
				bool fail = false;
				T value = TryGet(index, ref fail);
				if (fail)
					ThrowIndexOutOfRange(index);
				return value;
			}
		}
		
		public int IndexOf(T item)
		{
			return LCInterfaces.IndexOf(this, item);
		}
		protected int ThrowIndexOutOfRange(int index)
		{
			throw new IndexOutOfRangeException(string.Format(
				"Index out of range: {0}[{1} of {2}]", GetType().Name, index, Count));
		}

		#endregion

		#region IList<T> Members

		T IList<T>.this[int index]
		{
			get {
				bool fail = false;
				T value = TryGet(index, ref fail);
				if (fail)
					ThrowIndexOutOfRange(index);
				return value;
			}
			set { throw new NotSupportedException("List is read-only."); }
		}
		void IList<T>.Insert(int index, T item)
		{
			throw new NotSupportedException("List is read-only.");
		}
		void IList<T>.RemoveAt(int index)
		{
			throw new NotSupportedException("List is read-only.");
		}
		public new IEnumerator<T> GetEnumerator()
		{
			bool fail = false;
			T value;
			int count = Count;
			int i = 0;
			for (;; ++i) {
				value = TryGet(i, ref fail);
				if (count != Count)
					throw new EnumerationException();
				if (fail)
					break;
				yield return value;
			}
			Debug.Assert(i >= Count);
		}
		public override Iterator<T> GetIterator()
		{
			int i = -1;
			int count = Count;
			return delegate(ref bool ended) {
				if (count != Count)
					throw new EnumerationException();
				return TryGet(++i, ref ended);
			};
		}

		#endregion
	}
}
