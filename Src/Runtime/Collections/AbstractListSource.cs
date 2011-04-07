using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Runtime
{
	/// <summary>Helps you implement sources (read-only collections) by providing
	/// default implementations for most methods of IList(T) and
	/// IListSource(T).</summary>
	/// <remarks>
	/// You only need to implement two methods yourself:
	/// <code>
	///     public abstract int Count { get; }
	///     public abstract bool TryGetValue(int index, T defaultValue);
	/// </code>
	/// </remarks>
	public abstract class AbstractListSource<T> : AbstractSource<T>, IListSource<T>, IList<T>
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
			return Collections.IndexOf(this, item);
		}
		public override Iterator<T> GetIterator()
		{
			int i = 0;
			return delegate(ref bool ended) {
				return TryGet(i++, ref ended);
			};
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
			for (int i = 0; i < count; i++)
			{
				value = TryGet(i, ref fail);
				if (fail)
					break;
				yield return value;
			}
			if (count != Count)
				throw new InvalidOperationException("The collection was modified after the enumerator was created.");
		}

		#endregion
	}
}
