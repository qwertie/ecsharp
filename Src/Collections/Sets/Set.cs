using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections.Impl;
using System.Diagnostics;

namespace Loyc.Collections
{
	/// <summary>An immutable set.</summary>
	/// <remarks>
	/// This is the immutable version of <see cref="MSet{T}"/>. It does not
	/// allow changes to the set, but it provides operators (&, |, ^, -) for 
	/// intersecting, merging, and subtracting sets, and it can be converted to 
	/// a mutable <see cref="MSet{T}"/> in O(1) time. You can also add
	/// single items to the set using operators + and -.
	/// <para/>
	/// For more information, please read the documentation of <see cref="Set{T}"/> 
	/// and <see cref="InternalSet{T}"/>.
	/// </remarks>
	[Serializable]
	[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
	[DebuggerDisplay("Count = {Count}")]
	public struct Set<T> : ISetImm<T, Set<T>>, ICollection<T> //, ICount
	{
		public static readonly Set<T> Empty = new Set<T>();
		internal InternalSet<T> _set;
		IEqualityComparer<T> _comparer;
		int _count;

		public Set(IEnumerable<T> list) : this(list, InternalSet<T>.DefaultComparer) { }
		public Set(IEqualityComparer<T> comparer) : this(null, comparer) { }
		public Set(IEnumerable<T> list, IEqualityComparer<T> comparer)
		{
			_set = new InternalSet<T>();
			_comparer = comparer;
			_count = 0;
			if (list != null) {
				_count = _set.UnionWith(list, comparer, false);
				_set.CloneFreeze();
			}
			if (comparer == null && !_set.HasRoot) {
				// give it a root so that Comparer does not change _comparer
				_set = InternalSet<T>.Empty;
			}
		}
		public Set(InternalSet<T> set, IEqualityComparer<T> comparer) : this(set, comparer, set.Count()) { }
		internal Set(InternalSet<T> set, IEqualityComparer<T> comparer, int count)
		{
			Debug.Assert(count >= 0);
			_set = set;
			_comparer = comparer;
			_count = count;
			set.CloneFreeze();
		}

		public bool IsEmpty { get { return _count == 0; } }
		public InternalSet<T> InternalSet { get { return _set; } }
		public IEqualityComparer<T> Comparer
		{
			get {
				if (_comparer == null && !_set.HasRoot)
					return _comparer = InternalSet<T>.DefaultComparer;
				return _comparer;
			}
		}

		/// <inheritdoc cref="MSet{T}.Find"/>
		public bool Find(ref T item)
		{
			return _set.Find(ref item, _comparer);
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

		/// <summary>Enumerator for <see cref="MSet{T}"/>.</summary>
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

		#region ISet<T>: IsSubsetOf, IsSupersetOf, Overlaps, IsProperSubsetOf, IsProperSupersetOf, SetEquals
		// Remember to keep this code in sync with MSet<T> (the copies can be identical)

		/// <summary>Returns true if all items in this set are present in the other set.</summary>
		public bool IsSubsetOf(IEnumerable<T> other) { return _set.IsSubsetOf(other, _comparer, _count); }
		public bool IsSubsetOf(Set<T> other) { return Count <= other.Count && _set.IsSubsetOf(other.InternalSet, other.Comparer); }
		public bool IsSubsetOf(MSet<T> other) { return Count <= other.Count && _set.IsSubsetOf(other.InternalSet, other.Comparer); }
		public bool IsSubsetOf(ISet<T> other) { return _set.IsSubsetOf(other, _count); }

		/// <summary>Returns true if all items in the other set are present in this set.</summary>
		public bool IsSupersetOf(IEnumerable<T> other) { return _set.IsSupersetOf(other, _comparer, _count); }
		public bool IsSupersetOf(Set<T> other) { return Count >= other.Count && _set.IsSupersetOf(other.InternalSet, _comparer); }
		public bool IsSupersetOf(MSet<T> other) { return Count >= other.Count && _set.IsSupersetOf(other.InternalSet, _comparer); }

		/// <summary>Returns true if this set contains at least one item from 'other'.</summary>
		public bool Overlaps(IEnumerable<T> other) { return _set.Overlaps(other, _comparer); }
		public bool Overlaps(Set<T> other) { return _set.Overlaps(other.InternalSet, _comparer); }
		public bool Overlaps(MSet<T> other) { return _set.Overlaps(other.InternalSet, _comparer); }

		/// <inheritdoc cref="InternalSet{T}.IsProperSubsetOf(ISet{T}, int)"/>
		public bool IsProperSubsetOf(Set<T> other) { return Count < other.Count && IsSubsetOf(other); }
		/// <inheritdoc cref="InternalSet{T}.IsProperSubsetOf(ISet{T}, int)"/>
		public bool IsProperSubsetOf(MSet<T> other) { return Count < other.Count && IsSubsetOf(other); }
		/// <inheritdoc cref="InternalSet{T}.IsProperSubsetOf(ISet{T}, int)"/>
		public bool IsProperSubsetOf(ISet<T> other) { return _set.IsProperSubsetOf(other, _count); }
		/// <inheritdoc cref="InternalSet{T}.IsProperSubsetOf(IEnumerable{T}, IEqualityComparer{T}, int)"/>
		public bool IsProperSubsetOf(IEnumerable<T> other) { return _set.IsProperSubsetOf(other, _comparer, _count); }

		/// <inheritdoc cref="InternalSet{T}.IsProperSupersetOf(ISet{T}, IEqualityComparer{T}, int)"/>
		public bool IsProperSupersetOf(Set<T> other) { return Count > other.Count && IsSupersetOf(other); }
		/// <inheritdoc cref="InternalSet{T}.IsProperSupersetOf(ISet{T}, IEqualityComparer{T}, int)"/>
		public bool IsProperSupersetOf(MSet<T> other) { return Count > other.Count && IsSupersetOf(other); }
		/// <inheritdoc cref="InternalSet{T}.IsProperSupersetOf(ISet{T}, IEqualityComparer{T}, int)"/>
		public bool IsProperSupersetOf(ISet<T> other) { return _set.IsProperSupersetOf(other, _comparer, _count); }
		/// <inheritdoc cref="InternalSet{T}.IsProperSupersetOf(IEnumerable{T}, IEqualityComparer{T}, int)"/>
		public bool IsProperSupersetOf(IEnumerable<T> other) { return _set.IsProperSupersetOf(other, _comparer, _count); }

		/// <inheritdoc cref="InternalSet{T}.SetEquals(ISet{T}, int)"/>
		public bool SetEquals(Set<T> other) { return Count == other.Count && IsSubsetOf(other); }
		/// <inheritdoc cref="InternalSet{T}.SetEquals(ISet{T}, int)"/>
		public bool SetEquals(MSet<T> other) { return Count == other.Count && IsSubsetOf(other); }
		/// <inheritdoc cref="InternalSet{T}.SetEquals(ISet{T}, int)"/>
		public bool SetEquals(ISet<T> other) { return _set.SetEquals(other, _count); }
		/// <inheritdoc cref="InternalSet{T}.SetEquals(IEnumerable{T}, IEqualityComparer{T}, int)"/>
		public bool SetEquals(IEnumerable<T> other) { return _set.SetEquals(other, _comparer, _count); }

		#endregion

		#region Persistent set operations: With, Without, Union, Except, Intersect, Xor

		public Set<T> With(T item)
		{
			Debug.Assert(_set.IsRootFrozen);
			var set = _set;
			int count2 = _count;
			if (set.Add(ref item, Comparer, false))
				count2++;
			return new Set<T>(set, _comparer, count2);
		}
		public Set<T> Without(T item)
		{
			Debug.Assert(_set.IsRootFrozen);
			var set = _set;
			if (set.Remove(ref item, Comparer))
				return new Set<T>(set, _comparer, _count - 1);
			return this;
		}
		public Set<T> Union(Set<T> other)                                  { return Union(other._set, false); }
		public Set<T> Union(Set<T> other, bool replaceWithValuesFromOther) { return Union(other._set, replaceWithValuesFromOther); }
		public Set<T> Union(MSet<T> other, bool replaceWithValuesFromOther = false) { return Union(other._set, replaceWithValuesFromOther); }
		internal Set<T> Union(InternalSet<T> other, bool replaceWithValuesFromOther = false)
		{
			Debug.Assert(_set.IsRootFrozen);
			var set = _set;
			int count2 = _count + set.UnionWith(other, Comparer, replaceWithValuesFromOther);
			return new Set<T>(set, _comparer, count2);
		}
		public Set<T> Intersect(Set<T> other) { return Intersect(other._set, other.Comparer); }
		public Set<T> Intersect(MSet<T> other) { return Intersect(other._set, other.Comparer); }
		internal Set<T> Intersect(InternalSet<T> other, IEqualityComparer<T> otherComparer)
		{
			Debug.Assert(_set.IsRootFrozen);
			var set = _set;
			int count2 = _count - set.IntersectWith(other, otherComparer);
			return new Set<T>(set, Comparer, count2);
		}
		public Set<T> Except(Set<T> other) { return Except(other._set); }
		public Set<T> Except(MSet<T> other) { return Except(other._set); }
		internal Set<T> Except(InternalSet<T> other)
		{
			Debug.Assert(_set.IsRootFrozen);
			var set = _set;
			int count2 = _count - set.ExceptWith(other, Comparer);
			return new Set<T>(set, _comparer, count2);
		}
		public Set<T> Xor(Set<T> other) { return Xor(other._set); }
		public Set<T> Xor(MSet<T> other) { return Xor(other._set); }
		internal Set<T> Xor(InternalSet<T> other)
		{
			Debug.Assert(_set.IsRootFrozen);
			var set = _set;
			int count2 = _count + set.SymmetricExceptWith(other, Comparer);
			return new Set<T>(set, _comparer, count2);
		}

		#endregion

		#region Operators: & | - ^ +
		// Note that if the two operands use different comparers or have different
		// types, the comparer and type of the left operand propagates to the 
		// result. When mixing Set<T> and MSet<T>, it is advisable to use Set<T> 
		// as the left-hand argument because the left-argument is always 
		// freeze-cloned, which is a no-op for Set<T>.

		public static Set<T> operator &(Set<T> a, Set<T> b) { return a.Intersect(b._set, b.Comparer); }
		public static Set<T> operator &(Set<T> a, MSet<T> b) { return a.Intersect(b._set, b.Comparer); }
		public static Set<T> operator |(Set<T> a, Set<T> b) { return a.Union(b._set); }
		public static Set<T> operator |(Set<T> a, MSet<T> b) { return a.Union(b._set); }
		public static Set<T> operator -(Set<T> a, Set<T> b) { return a.Except(b._set); }
		public static Set<T> operator -(Set<T> a, MSet<T> b) { return a.Except(b._set); }
		public static Set<T> operator ^(Set<T> a, Set<T> b) { return a.Xor(b._set); }
		public static Set<T> operator ^(Set<T> a, MSet<T> b) { return a.Xor(b._set); }
		public static Set<T> operator +(T item, Set<T> a) { return a.With(item); }
		public static Set<T> operator +(Set<T> a, T item) { return a.With(item); }
		public static Set<T> operator -(Set<T> a, T item) { return a.Without(item); }

		public static explicit operator Set<T>(MSet<T> a)
		{
			return new Set<T>(a.InternalSet, a.Comparer, a.Count);
		}

		#endregion

		/// <summary>Returns a new set that contains only items that match the 
		/// specified predicate (i.e. for which the predicate returns true).</summary>
		public Set<T> Where(Predicate<T> match)
		{
			var result = new Set<T>(_comparer);
			foreach (var item in this) {
				var item2 = item;
				if (match(item))
					if (result._set.Add(ref item2, _comparer, false))
						result._count++;
			}
			return result;
		}

		/// <summary>Measures the total size of all objects allocated to this 
		/// collection, in bytes, including the size of this object itself; see
		/// <see cref="InternalSet{T}.CountMemory"/>.</summary>
		public long CountMemory(int sizeOfT)
		{
			return IntPtr.Size * 2 + _set.CountMemory(sizeOfT);
		}

		bool ISetImm<T, Set<T>>.IsInverted
		{
			get { return false; }
		}
	}
}
