using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>Helps you implement sources (read-only collections) by providing
	/// default implementations for most methods of <see cref="ICollection{T}"/> and
	/// <see cref="ISource{T}"/>.</summary>
	/// <remarks>
	/// You only need to implement two methods yourself:
	/// <code>
	///     public abstract int Count { get; }
	///     public abstract Iterator&lt;T> GetIterator();
	/// </code>
	/// </remarks>
	[Serializable]
	public abstract class SourceBase<T> : IterableBase<T>, ISource<T>, ICollection<T>
	{
		#region ISource<T> Members

		public abstract int Count { get; }

		public bool Contains(T item)
		{
			return LCInterfaces.Contains(this, item);
		}

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
			LCInterfaces.CopyTo(this, array, arrayIndex);
		}
		bool ICollection<T>.IsReadOnly
		{
			get { return true; }
		}
		bool ICollection<T>.Remove(T item)
		{
			throw new NotSupportedException("Collection is read-only.");
		}

		#endregion
	}
}
