using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections.Impl;

namespace Loyc.Collections
{
	/// <summary>An immutable set.</summary>
	/// <remarks>
	/// This is the immutable version of <see cref="ObjectSet{T}"/>. It does not
	/// allow changes to the set, but it provides operators (&, |, ^, -) for 
	/// intersecting, merging, and subtracting sets, and it can be converted to 
	/// a mutable <see cref="ObjectSet{T}"/> in O(1) time.
	/// </remarks>
	public struct ObjectSetI<T> : ICollection<T>, ICount
	{
		InternalSet<T> _set;
		IEqualityComparer<T> _comparer;
		int _count;

		public ObjectSetI(IEnumerable<T> list) : this(list, EqualityComparer<T>.Default) { }
		public ObjectSetI(IEnumerable<T> list, IEqualityComparer<T> comparer)
		{
			_set = InternalSet<T>.Empty;
			_comparer = comparer;
			_count = 0;
			_set.UnionWith(list, comparer, false);
		}
		public ObjectSetI(IEqualityComparer<T> comparer) : this()
		{
			_set = InternalSet<T>.Empty;
			_comparer = comparer;
			_count = 0;
		}
		public ObjectSetI(InternalSet<T> set, IEqualityComparer<T> comparer) : this(set, comparer, set.Count()) { }
		internal ObjectSetI(InternalSet<T> set, IEqualityComparer<T> comparer, int count)
		{
			_set = set;
			_comparer = comparer;
			_count = count;
			set.CloneFreeze();
		}

		public InternalSet<T> InternalSet { get { return _set; } }
		public IEqualityComparer<T> Comparer {
			get {
				if (_comparer == null && !_set.HasRoot)
					return _comparer = EqualityComparer<T>.Default;
				return _comparer;
			}
		}

		#region ICollection<T>

		public bool Contains(T item)
		{
			return _set.Find(ref item, _comparer);
		}
		public void CopyTo(T[] array, int arrayIndex)
		{
			if (_count > array.Length - arrayIndex)
				throw new ArgumentException(Localize.From("CopyTo: Insufficient space in supplied array"));
			_set.CopyTo(array, arrayIndex);
		}
		public int Count { get { return _count; } }
		public Enumerator GetEnumerator() { return new Enumerator(_set); }

		/// <summary>Enumerator for <see cref="ObjectSet{T}"/>.</summary>
		/// <remarks>This is a wrapper of <see cref="InternalSet{T}.Enumerator"/> 
		/// that blocks editing functionality.</remarks>
		public struct Enumerator : IEnumerator<T>
		{
			internal Enumerator(InternalSet<T> set) { _e = new InternalSet<T>.Enumerator(set); }
			InternalSet<T>.Enumerator _e;

			public T Current { get { return _e.Current; } }
			public bool MoveNext() { return _e.MoveNext(); }

			void IDisposable.Dispose() { }
			object System.Collections.IEnumerator.Current { get { return Current; } }
			void System.Collections.IEnumerator.Reset() { throw new NotSupportedException(); }
		}

		public bool IsReadOnly { get { return true; } }
		void ICollection<T>.Add(T item) { throw new ReadOnlyException(); }
		void ICollection<T>.Clear() { throw new ReadOnlyException(); }
		bool ICollection<T>.Remove(T item) { throw new ReadOnlyException(); }
		IEnumerator<T> IEnumerable<T>.GetEnumerator() { return GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

		#endregion

		/// <inheritdoc cref="ObjectSet{T}.Find"/>
		public bool Find(ref T item)
		{
			return _set.Find(ref item, _comparer);
		}

	}
}
