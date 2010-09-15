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

		public abstract bool TryGetValue(int index, ref T value);

		public T this[int index, T defaultValue]
		{
			get {
				TryGetValue(index, ref defaultValue);
				return defaultValue;
			}
		}
		public T this[int index]
		{ 
			get {
				T value = default(T);
				if (!TryGetValue(index, ref value))
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
			return delegate(out T current) {
				current = default(T);
				return TryGetValue(i++, ref current);
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
				T value = default(T);
				if (!TryGetValue(index, ref value))
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
		public IEnumerator<T> GetEnumerator()
		{
			T value = default(T);
			for (int i = 0; i < Count; i++)
			{
				TryGetValue(i, ref value);
				yield return value;
			}
		}

		#endregion
	}
}
