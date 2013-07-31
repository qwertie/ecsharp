using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Loyc.Collections
{
	/// <summary>Helps you implement sources (read-only collections) by providing
	/// default implementations for most methods of <see cref="ICollection{T}"/> and
	/// <see cref="IReadOnlyCollection{T}"/>.</summary>
	/// <remarks>
	/// You only need to implement two methods yourself:
	/// <code>
	///     public abstract int Count { get; }
	///     public abstract IEnumerator<T> GetEnumerator();
	/// </code>
	/// </remarks>
	[Serializable]
	public abstract class SourceBase<T> : IReadOnlyCollection<T>, ICollection<T>
	{
		#region ISource<T> Members

		public abstract int Count { get; }
		public abstract IEnumerator<T> GetEnumerator();
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

		#endregion

		#region ICollection<T> Members

		void ICollection<T>.Add(T item)
		{
			throw new NotSupportedException("Collection is read-only.");
		}
		void ICollection<T>.Clear()
		{
			throw new NotSupportedException("Collection is read-only.");
		}
		void ICollection<T>.CopyTo(T[] array, int arrayIndex)
		{
			ListExt.CopyTo(this, array, arrayIndex);
		}
		bool ICollection<T>.IsReadOnly
		{
			get { return true; }
		}
		bool ICollection<T>.Remove(T item)
		{
			throw new NotSupportedException("Collection is read-only.");
		}
		public bool Contains(T item)
		{
			return Enumerable.Contains(this, item);
		}

		#endregion
	}
}
