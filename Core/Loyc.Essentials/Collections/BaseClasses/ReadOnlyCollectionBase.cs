using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Loyc.Collections.Impl
{
	/// <summary>Helps you implement read-only collections by providing
	/// default implementations for most methods of <see cref="ICollection{T}"/> 
	/// and <see cref="IReadOnlyCollection{T}"/>.</summary>
	/// <remarks>
	/// You only need to implement two methods yourself:
	/// <code>
	///     public abstract int Count { get; }
	///     public abstract IEnumerator&lt;T> GetEnumerator();
	/// </code>
	/// </remarks>
	[Serializable]
	public abstract class ReadOnlyCollectionBase<T> : ICollectionImpl<T>
	{
		#region ISource<T> Members

		public abstract int Count { get; }
		public abstract IEnumerator<T> GetEnumerator();
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

		#endregion

		#region ICollection<T> Members

		void ICollection<T>.Add(T item) => throw new NotSupportedException("Collection is read-only.");
		void IAdd<T>.Add(T item) => throw new NotSupportedException("Collection is read-only.");
		void ICollection<T>.Clear() => throw new NotSupportedException("Collection is read-only.");
		void ICollectionSink<T>.Clear() => throw new NotSupportedException("Collection is read-only.");
		void ICollection<T>.CopyTo(T[] array, int arrayIndex) => ListExt.CopyTo(this, array, arrayIndex);
		void ICollectionSource<T>.CopyTo(T[] array, int arrayIndex) => ListExt.CopyTo(this, array, arrayIndex);
		bool ICollection<T>.IsReadOnly => true;
		bool ICollection<T>.Remove(T item) => throw new NotSupportedException("Collection is read-only.");
		bool ICollectionSink<T>.Remove(T item) => throw new NotSupportedException("Collection is read-only.");

		public bool Contains(T item)
		{
			return Enumerable.Contains(this, item);
		}

		#endregion
	}
}
