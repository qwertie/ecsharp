// Author: David Piepgrass
// License: LGPL
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Loyc.Utilities;
using Loyc.Collections.Impl;
using NUnit.Framework;
using Loyc.Math;
using System.ComponentModel;

namespace Loyc.Collections
{
	/// <summary>A mutable set.</summary>
	/// <remarks>
	/// This class is based on <see cref="InternalSet{T}"/>; see its documentation 
	/// for technical details about the implementation.
	/// <para/>
	/// Assuming T is a reference type, this class uses less memory than <see 
	/// cref="HashSet{T}"/> and, under certain conditions, is faster. Specifically, 
	/// <ul>
	/// <li>This class is optimized for objects whose Equals() and GetHashCode() 
	/// methods are fast, or for which equality is synonymous with "reference 
	/// equality" so it is not necessary to call Equals() at all.</li>
	/// <li>This class supports fast cloning and overloads the following operators
	/// to perform set operations: & (intersection), | (union), - (subtraction, i.e.
	/// <see cref="ExceptWith"/>) and ^ (xor, i.e. <see cref="SymmetricExceptWith"/>.
	/// These operators clone the left-hand argument, so they benefit from fast-
	/// cloning functionality.</li>
	/// </ul>
	/// This class may be slower than <see cref="HashSet{T}"/> if the comparison
	/// method for T is slow; up to four comparisons are required per add/remove
	/// operation.
	/// <para/>
	/// You can convert <see cref="MSet{T}"/> to <see cref="Set{T}"/> 
	/// and back in O(1) time using a C# cast operator.
	/// </remarks>
	[Serializable]
	[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
	[DebuggerDisplay("Count = {Count}")]
	public class MSet<T> : ISetImm<T, MSet<T>>, ICollection<T>, ICloneable<MSet<T>>, IReadOnlyCollection<T>, ISinkCollection<T> // ICount
		#if DotNet4
		, ISet<T>
		#endif
	{
		internal InternalSet<T> _set;
		internal IEqualityComparer<T> _comparer;
		internal int _count;

		public MSet() { _comparer = InternalSet<T>.DefaultComparer; }
		public MSet(IEnumerable<T> copy) : this(copy, InternalSet<T>.DefaultComparer) { }
		public MSet(IEnumerable<T> copy, IEqualityComparer<T> comparer) { _comparer = comparer; AddRange(copy); }
		public MSet(IEqualityComparer<T> comparer) { _comparer = comparer; }
		public MSet(InternalSet<T> set, IEqualityComparer<T> comparer) : this(set, comparer, set.Count()) { }
		internal MSet(InternalSet<T> set, IEqualityComparer<T> comparer, int count)
		{
			Debug.Assert(count >= 0);
			_set = set;
			_comparer = comparer;
			_count = count;
			set.CloneFreeze();
		}

		public bool IsEmpty { get { return _count == 0; } }
		internal InternalSet<T> InternalSet { get { return _set; } }
		public InternalSet<T> FrozenInternalSet { get { _set.CloneFreeze(); return _set; } }
		public IEqualityComparer<T> Comparer { get { return _comparer; } }

		/// <summary>Adds the specified item to the set, or throws an exception if
		/// a matching item is already present.</summary>
		/// <exception cref="ArgumentException">The item already exists in the set.</exception>
		public void AddUnique(T item)
		{
			if (_set.Add(ref item, _comparer, false))
				_count++;
			throw new ArgumentException("The item already exists in the set.");
		}

		/// <summary>Searches for an item. If the item is found, the copy in the 
		/// set is returned in the 'item' parameter. Note: there is no reason to 
		/// call this method in a <see cref="SymbolSet"/> because the item reference
		/// will never change; call <see cref="Contains"/> instead.</summary>
		/// <returns>true if the item was found, false if not.</returns>
		public bool Find(ref T item)
		{
			return _set.Find(ref item, _comparer);
		}

		/// <summary>Adds the specified item to the set, and retrieves an existing 
		/// copy of the item if one existed. Note: there is no reason to call this
		/// method in a <see cref="SymbolSet"/> because if an item is found, it 
		/// will always be the exact same object that you searched for.</summary>
		/// <param name="item">An object to search for. If this method returns false,
		/// this parameter is changed to the existing value that was found in the
		/// collection.</param>
		/// <param name="replaceIfPresent">If true, and a matching item exists in
		/// the set, that item will be replaced with the specified new item. The
		/// old value will be returned in the 'item' parameter.</param>
		/// <returns>True if a new item was added, false if the item already 
		/// existed in the set.</returns>
		public bool AddOrFind(ref T item, bool replaceIfPresent)
		{
			if (_set.Add(ref item, _comparer, replaceIfPresent)) {
				_count++;
				return true;
			}
			return false;
		}

		/// <summary>Adds the specified item to the set.</summary>
		/// <param name="replaceIfPresent">If true, and a matching item is 
		/// already present in the set, the specified item replaces the existing 
		/// copy. If false, the existing copy is left alone. This parameter
		/// has no effect in a <see cref="SymbolSet"/>.</param>
		/// <returns>true if the item was new, false if it was already present.</returns>
		public bool Add(T item, bool replaceIfPresent)
		{
			return AddOrFind(ref item, replaceIfPresent);
		}

		/// <summary>Fast-clones the set in O(1) time.</summary>
		/// <remarks>
		/// Once the set is cloned, modifications to both sets take
		/// longer because portions of the set must be duplicated. See 
		/// <see cref="InternalSet{T}"/> for details about the fast-
		/// cloning technique.</remarks>
		public virtual MSet<T> Clone()
		{
			return new MSet<T>(_set, _comparer, _count);
		}

		#region ICollection<T>

		/// <summary>Adds the specified item to the set if it is not present.</summary>
		/// <returns>true if the item was new, false if it was not added because
		/// it was already present.</returns>
		public bool Add(T item)
		{
			return AddOrFind(ref item, false);
		}
		void ICollection<T>.Add(T item) { Add(item); }
		void IHasAdd<T>.Add(T item) { Add(item); }
		public int AddRange(IEnumerable<T> items)
		{
			int added = _set.UnionWith(items, _comparer, true);
			_count += added;
			return added;
		}
		public void Clear()
		{
			_set.Clear();
			_count = 0;
		}
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
		public int Count
		{
			get { return _count; }
		}
		public bool IsReadOnly
		{
			get { return false; }
		}
		public bool Remove(T item) { return Remove(ref item); }
		public bool Remove(ref T item)
		{
			if (_set.Remove(ref item, _comparer)) {
				_count--;
				Debug.Assert(_count >= 0);
				return true;
			}
			return false;
		}
		public InternalSet<T>.Enumerator GetEnumerator()
		{
			return _set.GetEnumerator();
		}
		IEnumerator<T> IEnumerable<T>.GetEnumerator() { return GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

		#endregion

		#region ISet<T>: UnionWith, ExceptWith, IntersectWith, SymmetricIntersectWith

		/// <summary>Adds all items in the other set to this set.</summary>
		/// <remarks>Any items that are already present are left unmodified.</remarks>
		public void UnionWith(IEnumerable<T> other) { UnionWith(other, false); }
		public void UnionWith(IEnumerable<T> other, bool replaceIfPresent) { _count += _set.UnionWith(other, _comparer, replaceIfPresent); }
		public void UnionWith(Set<T> other, bool replaceIfPresent = false) { _count += _set.UnionWith(other.InternalSet, _comparer, replaceIfPresent); }
		public void UnionWith(MSet<T> other, bool replaceIfPresent = false) { _count += _set.UnionWith(other.InternalSet, _comparer, replaceIfPresent); }

		/// <summary>Removes all items from this set that are present in 'other'.</summary>
		/// <param name="other">The set whose members should be removed from this set.</param>
		public void ExceptWith(IEnumerable<T> other) { _count -= _set.ExceptWith(other, _comparer); }
		public void ExceptWith(Set<T> other) { _count -= _set.ExceptWith(other.InternalSet, _comparer); }
		public void ExceptWith(MSet<T> other) { _count -= _set.ExceptWith(other.InternalSet, _comparer); }

		/// <inheritdoc cref="InternalSet{T}.IntersectWith(IEnumerable{T}, IEqualityComparer{T})"/>
		public void IntersectWith(IEnumerable<T> other)
		{
			var otherOSet = other as MSet<T>;
			if (otherOSet != null)
				IntersectWith(otherOSet);
			else
				_set.IntersectWith(other, _comparer); // relatively costly unless other is ISet<T>
		}
		/// <summary>Removes all items from this set that are not present in 'other'.</summary>
		public void IntersectWith(Set<T> other) { _count -= _set.IntersectWith(other.InternalSet, other.Comparer); }
		/// <summary>Removes all items from this set that are not present in 'other'.</summary>
		public void IntersectWith(MSet<T> other) { _count -= _set.IntersectWith(other.InternalSet, other.Comparer); }
		/// <summary>Removes all items from this set that are not present in 'other'.</summary>
		public void IntersectWith(ISet<T> other) { _count -= _set.IntersectWith(other); }

		/// <summary>Modifies the current set to contain only elements that were
		/// present either in this set or in the other collection, but not both.</summary>
		public void SymmetricExceptWith(IEnumerable<T> other) { SymmetricExceptWith(other, false); }
		/// <inheritdoc cref="InternalSet{T}.SymmetricExceptWith(IEnumerable{T}, IEqualityComparer{T}, bool)"/>
		public void SymmetricExceptWith(IEnumerable<T> other, bool xorDuplicates)
		                                                      { _count += _set.SymmetricExceptWith(other, _comparer, xorDuplicates); }
		public void SymmetricExceptWith(Set<T> other)  { _count += _set.SymmetricExceptWith(other.InternalSet, _comparer); }
		public void SymmetricExceptWith(MSet<T> other)   { _count += _set.SymmetricExceptWith(other.InternalSet, _comparer); }

		#endregion

		#region ISet<T>: IsSubsetOf, IsSupersetOf, Overlaps, IsProperSubsetOf, IsProperSupersetOf, SetEquals
		// Remember to keep this code in sync with Set<T> (the copies can be identical)

		/// <summary>Returns true if all items in this set are present in the other set.</summary>
		public bool IsSubsetOf(IEnumerable<T> other) { return _set.IsSubsetOf(other, _comparer, _count); }
		public bool IsSubsetOf(Set<T> other)  { return Count <= other.Count && _set.IsSubsetOf(other.InternalSet, other.Comparer); }
		public bool IsSubsetOf(MSet<T> other)   { return Count <= other.Count &&_set.IsSubsetOf(other.InternalSet, other.Comparer); }
		public bool IsSubsetOf(ISet<T> other)        { return _set.IsSubsetOf(other, _count); }

		/// <summary>Returns true if all items in the other set are present in this set.</summary>
		public bool IsSupersetOf(IEnumerable<T> other) { return _set.IsSupersetOf(other, _comparer, _count); }
		public bool IsSupersetOf(Set<T> other)  { return Count >= other.Count && _set.IsSupersetOf(other.InternalSet, _comparer); }
		public bool IsSupersetOf(MSet<T> other)   { return Count >= other.Count && _set.IsSupersetOf(other.InternalSet, _comparer); }

		/// <summary>Returns true if this set contains at least one item from 'other'.</summary>
		public bool Overlaps(IEnumerable<T> other) { return _set.Overlaps(other, _comparer); }
		public bool Overlaps(Set<T> other)  { return _set.Overlaps(other.InternalSet, _comparer); }
		public bool Overlaps(MSet<T> other)   { return _set.Overlaps(other.InternalSet, _comparer); }

		/// <inheritdoc cref="InternalSet{T}.IsProperSubsetOf(ISet{T}, int)"/>
		public bool IsProperSubsetOf(Set<T> other)  { return Count < other.Count && IsSubsetOf(other); }
		/// <inheritdoc cref="InternalSet{T}.IsProperSubsetOf(ISet{T}, int)"/>
		public bool IsProperSubsetOf(MSet<T> other)   { return Count < other.Count && IsSubsetOf(other); }
		/// <inheritdoc cref="InternalSet{T}.IsProperSubsetOf(ISet{T}, int)"/>
		public bool IsProperSubsetOf(ISet<T> other)        { return _set.IsProperSubsetOf(other, _count); }
		/// <inheritdoc cref="InternalSet{T}.IsProperSubsetOf(IEnumerable{T}, IEqualityComparer{T}, int)"/>
		public bool IsProperSubsetOf(IEnumerable<T> other) { return _set.IsProperSubsetOf(other, _comparer, _count); }

		/// <inheritdoc cref="InternalSet{T}.IsProperSupersetOf(ISet{T}, IEqualityComparer{T}, int)"/>
		public bool IsProperSupersetOf(Set<T> other)  { return Count > other.Count && IsSupersetOf(other); }
		/// <inheritdoc cref="InternalSet{T}.IsProperSupersetOf(ISet{T}, IEqualityComparer{T}, int)"/>
		public bool IsProperSupersetOf(MSet<T> other)   { return Count > other.Count && IsSupersetOf(other); }
		/// <inheritdoc cref="InternalSet{T}.IsProperSupersetOf(ISet{T}, IEqualityComparer{T}, int)"/>
		public bool IsProperSupersetOf(ISet<T> other)        { return _set.IsProperSupersetOf(other, _comparer, _count); }
		/// <inheritdoc cref="InternalSet{T}.IsProperSupersetOf(IEnumerable{T}, IEqualityComparer{T}, int)"/>
		public bool IsProperSupersetOf(IEnumerable<T> other) { return _set.IsProperSupersetOf(other, _comparer, _count); }

		/// <inheritdoc cref="InternalSet{T}.SetEquals(ISet{T}, int)"/>
		public bool SetEquals(Set<T> other) { return Count == other.Count && IsSubsetOf(other); }
		/// <inheritdoc cref="InternalSet{T}.SetEquals(ISet{T}, int)"/>
		public bool SetEquals(MSet<T> other)   { return Count == other.Count && IsSubsetOf(other); }
		/// <inheritdoc cref="InternalSet{T}.SetEquals(ISet{T}, int)"/>
		public bool SetEquals(ISet<T> other)        { return _set.SetEquals(other, _count); }
		/// <inheritdoc cref="InternalSet{T}.SetEquals(IEnumerable{T}, IEqualityComparer{T}, int)"/>
		public bool SetEquals(IEnumerable<T> other) { return _set.SetEquals(other, _comparer, _count); }

		#endregion

		#region Persistent set operations: With, Without, Union, Except, Intersect, Xor

		public MSet<T> With(T item)
		{
			var set = _set.CloneFreeze();
			if (set.Add(ref item, Comparer, false))
				return new MSet<T>(set, _comparer, _count + 1);
			return this;
		}
		public MSet<T> Without(T item)
		{
			var set = _set.CloneFreeze();
			if (set.Remove(ref item, Comparer))
				return new MSet<T>(set, _comparer, _count - 1);
			return this;
		}
		public MSet<T> Union(Set<T> other, bool replaceWithValuesFromOther = false) { return Union(other._set, replaceWithValuesFromOther); }
		public MSet<T> Union(MSet<T> other)                                  { return Union(other._set, false); }
		public MSet<T> Union(MSet<T> other, bool replaceWithValuesFromOther) { return Union(other._set, replaceWithValuesFromOther); }
		internal MSet<T> Union(InternalSet<T> other, bool replaceWithValuesFromOther = false)
		{
			var set = _set.CloneFreeze();
			int count2 = _count + set.UnionWith(other, Comparer, replaceWithValuesFromOther);
			return new MSet<T>(set, _comparer, count2);
		}
		public MSet<T> Intersect(Set<T> other) { return Intersect(other._set, other.Comparer); }
		public MSet<T> Intersect(MSet<T> other) { return Intersect(other._set, other.Comparer); }
		internal MSet<T> Intersect(InternalSet<T> other, IEqualityComparer<T> otherComparer)
		{
			var set = _set.CloneFreeze();
			int count2 = _count - set.IntersectWith(other, otherComparer);
			return new MSet<T>(set, Comparer, count2);
		}
		public MSet<T> Except(Set<T> other) { return Except(other._set); }
		public MSet<T> Except(MSet<T> other) { return Except(other._set); }
		internal MSet<T> Except(InternalSet<T> other)
		{
			var set = _set.CloneFreeze();
			int count2 = _count - set.ExceptWith(other, Comparer);
			return new MSet<T>(set, _comparer, count2);
		}
		public MSet<T> Xor(Set<T> other) { return Xor(other._set); }
		public MSet<T> Xor(MSet<T> other) { return Xor(other._set); }
		internal MSet<T> Xor(InternalSet<T> other)
		{
			var set = _set.CloneFreeze();
			int count2 = _count + set.SymmetricExceptWith(other, Comparer);
			return new MSet<T>(set, _comparer, count2);
		}

		#endregion

		#region Operators: & | - ^ +
		// Note that if the two operands use different comparers or have different
		// types, the comparer and type of the left operand propagates to the 
		// result. When mixing Set<T> and MSet<T>, it is advisable
		// to use Set<T> as the left-hand argument because the left-argument
		// is always freeze-cloned, which is a no-op for Set<T>.

		public static MSet<T> operator &(MSet<T> a, MSet<T> b) { return a.Intersect(b._set, b._comparer); }
		public static MSet<T> operator &(MSet<T> a, Set<T> b) { return a.Intersect(b._set, b.Comparer); }
		public static MSet<T> operator |(MSet<T> a, MSet<T> b) { return a.Union(b._set); }
		public static MSet<T> operator |(MSet<T> a, Set<T> b) { return a.Union(b._set); }
		public static MSet<T> operator -(MSet<T> a, MSet<T> b) { return a.Except(b._set); }
		public static MSet<T> operator -(MSet<T> a, Set<T> b) { return a.Except(b._set); }
		public static MSet<T> operator ^(MSet<T> a, MSet<T> b) { return a.Xor(b._set); }
		public static MSet<T> operator ^(MSet<T> a, Set<T> b) { return a.Xor(b._set); }
		public static MSet<T> operator +(T item, MSet<T> a) { return a.With(item); }
		public static MSet<T> operator +(MSet<T> a, T item) { return a.With(item); }
		public static MSet<T> operator -(MSet<T> a, T item) { return a.Without(item); }
		public static MSet<T> operator ^(MSet<T> a, T item) { var c = a.Clone(); c.Toggle(item); return c; }

		#endregion

		public static explicit operator Set<T>(MSet<T> a)
		{
			return new Set<T>(a.InternalSet, a.Comparer, a.Count);
		}
		public Set<T> AsImmutable()
		{
			return new Set<T>(this.InternalSet, this.Comparer, this.Count);
		}

		/// <summary>Removes all elements that match the conditions defined by the 
		/// specified predicate from this collection.</summary>
		/// <returns>The number of elements that were removed from the set.</returns>
		public int RemoveWhere(Predicate<T> match)
		{
			int removed = 0;
			var e = _set.GetEnumerator();
			while (e.MoveNext())
				if (match(e.Current))
					if (e.RemoveCurrent(ref _set))
						removed++;
			return removed;
		}

		/// <summary>Toggle's an object's presence in the set.</summary>
		/// <returns>true if the item was added, false if the item was removed.</returns>
		public bool Toggle(T item)
		{
			if (AddOrFind(ref item, false))
				return true;
			else {
				G.Verify(Remove(item));
				return false;
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public void ThawRoot() { _set.Thaw(); } // for a benchmark test

		/// <summary>Measures the total size of all objects allocated to this 
		/// collection, in bytes, including the size of this object itself; see
		/// <see cref="InternalSet{T}.CountMemory"/>.</summary>
		public long CountMemory(int sizeOfT)
		{
			return IntPtr.Size * 4 + _set.CountMemory(sizeOfT);
		}

		bool ISetImm<T, MSet<T>>.IsInverted
		{
			get { return false; }
		}
	}
}
