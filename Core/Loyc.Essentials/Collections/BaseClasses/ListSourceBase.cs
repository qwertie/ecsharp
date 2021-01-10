using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Loyc.Collections.Impl
{
	/// <summary>A base class for read-only collections that wish to implement 
	/// <see cref="IList{T}"/> and <see cref="IListSource{T}"/>. Provides
	/// default implementations for most of the methods.</summary>
	/// <remarks>
	/// You only need to implement two methods yourself:
	/// <code>
	///     public abstract int Count { get; }
	///     public abstract T TryGet(int index, out bool fail);
	/// </code>
	/// </remarks>
	[Serializable]
	public abstract class ListSourceBase<T> : ReadOnlyCollectionBase<T>, IListImpl<T>
	{
		#region IListSource<T> Members

		public abstract T TryGet(int index, out bool fail);
		public abstract override int Count { get; }
		
		public bool IsEmpty { get { return Count == 0; } }

        public T this[int index]
		{ 
			get {
				bool fail;
				T value = TryGet(index, out fail);
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
			throw new IndexOutOfRangeException(Localize.Localized(
				"Index out of range: {0}[{1} of {2}]", GetType().Name, index, Count));
		}

		IRange<T> IListSource<T>.Slice(int start, int count)
		{
			return Slice(start, count); 
		}
		public Slice_<T> Slice(int start, int count)
		{
			return new Slice_<T>(this, start, count); 
		}

		public override IEnumerator<T> GetEnumerator()
		{
			bool fail;
			T value;
			int count = Count;
			int i = 0;
			for (;; ++i) {
				value = TryGet(i, out fail);
				if (count != Count)
					throw new EnumerationException();
				if (fail)
					break;
				yield return value;
			}
			Debug.Assert(i >= Count);
		}

		#endregion

		#region IList<T> / IListImpl<T> Members

		T IArraySink<T>.this[int index] { set => throw new ReadOnlyException(); }
		T IList<T>.this[int index]
		{
			get {
				bool fail;
				T value = TryGet(index, out fail);
				if (fail)
					ThrowIndexOutOfRange(index);
				return value;
			}
			set { throw new ReadOnlyException(); }
		}
		void IList<T>.Insert(int index, T item) => throw new ReadOnlyException();
		void IList<T>.RemoveAt(int index) => throw new ReadOnlyException();

		#endregion

		void ICollectionSource<T>.CopyTo(T[] array, int arrayIndex) => ListExt.CopyTo(this, array, arrayIndex);
	}
}
