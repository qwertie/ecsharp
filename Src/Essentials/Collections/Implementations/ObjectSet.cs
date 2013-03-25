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

namespace Loyc.Collections
{
	
	/// <summary>A mutable set.</summary>
	/// <remarks>This class uses less memory than <see cref="HashSet{T}"/> and, 
	/// under certain conditions, is faster. Specifically, this class is optimized
	/// for objects whose Equals() and GetHashCode() methods are fast, or for which
	/// equality is synonymous with "reference equality" so it is not necessary to
	/// call Equals() at all.
	/// </remarks>
	public class ObjectSet<T> : ICollection<T>, ICount
		#if DotNet4
		, ISet<T>
		#endif
	{
		InternalSet<T> _set;
		IEqualityComparer<T> _comparer;
		int _count;
		
		public ObjectSet() { }
		public ObjectSet(IEnumerable<T> copy) { AddRange(copy); }
		public ObjectSet(IEnumerable<T> copy, IEqualityComparer<T> comparer) { _comparer = comparer; AddRange(copy); }
		public ObjectSet(IEqualityComparer<T> comparer) { _comparer = comparer; }
		public ObjectSet(InternalSet<T> set, IEqualityComparer<T> comparer) : this(set, comparer, set.Count()) { }
		internal ObjectSet(InternalSet<T> set, IEqualityComparer<T> comparer, int count)
		{
			_set = set;
			_comparer = comparer;
			_count = count;
			set.CloneFreeze();
		}

		internal InternalSet<T> InternalSet { get { return _set; } }
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
		public ObjectSet<T> Clone()
		{
			return new ObjectSet<T>(_set, _comparer, _count);
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
		public bool Remove(T item)
		{
			if (_set.Remove(item, _comparer)) {
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
		public void UnionWith(ObjectSetI<T> other, bool replaceIfPresent = false) { _count += _set.UnionWith(other.InternalSet, _comparer, replaceIfPresent); }
		public void UnionWith(ObjectSet<T> other, bool replaceIfPresent = false) { _count += _set.UnionWith(other.InternalSet, _comparer, replaceIfPresent); }

		/// <summary>Removes all items from this set that are present in 'other'.</summary>
		/// <param name="other">The set whose members should be removed from this set.</param>
		public void ExceptWith(IEnumerable<T> other) { _set.ExceptWith(other, _comparer); }
		public void ExceptWith(ObjectSetI<T> other) { _set.ExceptWith(other.InternalSet, _comparer); }
		public void ExceptWith(ObjectSet<T> other) { _set.ExceptWith(other.InternalSet, _comparer); }

		/// <inheritdoc cref="InternalSet{T}.IntersectWith(IEnumerable{T}, IEqualityComparer{T})"/>
		public void IntersectWith(IEnumerable<T> other)
		{
			var otherOSet = other as ObjectSet<T>;
			if (otherOSet != null)
				IntersectWith(otherOSet);
			else
				_set.IntersectWith(other, _comparer); // relatively costly unless other is ISet<T>
		}
		/// <summary>Removes all items from this set that are not present in 'other'.</summary>
		public void IntersectWith(ObjectSetI<T> other) { _set.IntersectWith(other.InternalSet, other.Comparer); }
		/// <summary>Removes all items from this set that are not present in 'other'.</summary>
		public void IntersectWith(ObjectSet<T> other) { _set.IntersectWith(other.InternalSet, other.Comparer); }
		/// <summary>Removes all items from this set that are not present in 'other'.</summary>
		public void IntersectWith(ISet<T> other) { _set.IntersectWith(other); }

		/// <summary>Modifies the current set to contain only elements that were
		/// present either in this set or in the other collection, but not both.</summary>
		public void SymmetricExceptWith(IEnumerable<T> other) { SymmetricExceptWith(other, false); }
		/// <inheritdoc cref="InternalSet{T}.SymmetricExceptWith(IEnumerable{T}, IEqualityComparer{T}, bool)"/>
		public void SymmetricExceptWith(IEnumerable<T> other, bool xorDuplicates) { _set.SymmetricExceptWith(other, _comparer, xorDuplicates); }
		public void SymmetricExceptWith(ObjectSetI<T> other)  { _set.SymmetricExceptWith(other.InternalSet, _comparer); }
		public void SymmetricExceptWith(ObjectSet<T> other)   { _set.SymmetricExceptWith(other.InternalSet, _comparer); }

		#endregion

		#region ISet<T>: IsSubsetOf, IsSupersetOf, Overlaps, IsProperSubsetOf, IsProperSupersetOf, SetEquals

		/// <summary>Returns true if all items in this set are present in the other set.</summary>
		public bool IsSubsetOf(IEnumerable<T> other) { return _set.IsSubsetOf(other, _comparer, _count); }
		public bool IsSubsetOf(ObjectSetI<T> other)  { return Count <= other.Count && _set.IsSubsetOf(other.InternalSet, other.Comparer); }
		public bool IsSubsetOf(ObjectSet<T> other)   { return Count <= other.Count &&_set.IsSubsetOf(other.InternalSet, other.Comparer); }
		public bool IsSubsetOf(ISet<T> other)        { return _set.IsSubsetOf(other, _count); }

		/// <summary>Returns true if all items in the other set are present in this set.</summary>
		public bool IsSupersetOf(IEnumerable<T> other) { return _set.IsSupersetOf(other, _comparer, _count); }
		public bool IsSupersetOf(ObjectSetI<T> other)  { return Count >= other.Count && _set.IsSupersetOf(other.InternalSet, _comparer); }
		public bool IsSupersetOf(ObjectSet<T> other)   { return Count >= other.Count && _set.IsSupersetOf(other.InternalSet, _comparer); }

		/// <summary>Returns true if this set contains at least one item from 'other'.</summary>
		public bool Overlaps(IEnumerable<T> other) { return _set.Overlaps(other, _comparer); }
		public bool Overlaps(ObjectSetI<T> other)  { return _set.Overlaps(other.InternalSet, _comparer); }
		public bool Overlaps(ObjectSet<T> other)   { return _set.Overlaps(other.InternalSet, _comparer); }

		/// <inheritdoc cref="InternalSet{T}.IsProperSubsetOf(ISet{T}, int)"/>
		public bool IsProperSubsetOf(ObjectSetI<T> other)  { return Count < other.Count && IsSubsetOf(other); }
		/// <inheritdoc cref="InternalSet{T}.IsProperSubsetOf(ISet{T}, int)"/>
		public bool IsProperSubsetOf(ObjectSet<T> other)   { return Count < other.Count && IsSubsetOf(other); }
		/// <inheritdoc cref="InternalSet{T}.IsProperSubsetOf(ISet{T}, int)"/>
		public bool IsProperSubsetOf(ISet<T> other)        { return _set.IsProperSubsetOf(other, _count); }
		/// <inheritdoc cref="InternalSet{T}.IsProperSubsetOf(IEnumerable{T}, IEqualityComparer{T}, int)"/>
		public bool IsProperSubsetOf(IEnumerable<T> other) { return _set.IsProperSubsetOf(other, _comparer, _count); }

		/// <inheritdoc cref="InternalSet{T}.IsProperSupersetOf(ISet{T}, IEqualityComparer{T}, int)"/>
		public bool IsProperSupersetOf(ObjectSetI<T> other)  { return Count > other.Count && IsSupersetOf(other); }
		/// <inheritdoc cref="InternalSet{T}.IsProperSupersetOf(ISet{T}, IEqualityComparer{T}, int)"/>
		public bool IsProperSupersetOf(ObjectSet<T> other)   { return Count > other.Count && IsSupersetOf(other); }
		/// <inheritdoc cref="InternalSet{T}.IsProperSupersetOf(ISet{T}, IEqualityComparer{T}, int)"/>
		public bool IsProperSupersetOf(ISet<T> other)        { return _set.IsProperSupersetOf(other, _comparer, _count); }
		/// <inheritdoc cref="InternalSet{T}.IsProperSupersetOf(IEnumerable{T}, IEqualityComparer{T}, int)"/>
		public bool IsProperSupersetOf(IEnumerable<T> other) { return _set.IsProperSupersetOf(other, _comparer, _count); }

		/// <inheritdoc cref="InternalSet{T}.SetEquals(ISet{T}, int)"/>
		public bool SetEquals(ObjectSetI<T> other) { return Count == other.Count && IsSubsetOf(other); }
		/// <inheritdoc cref="InternalSet{T}.SetEquals(ISet{T}, int)"/>
		public bool SetEquals(ObjectSet<T> other)   { return Count == other.Count && IsSubsetOf(other); }
		/// <inheritdoc cref="InternalSet{T}.SetEquals(ISet{T}, int)"/>
		public bool SetEquals(ISet<T> other)        { return _set.SetEquals(other, _count); }
		/// <inheritdoc cref="InternalSet{T}.SetEquals(IEnumerable{T}, IEqualityComparer{T}, int)"/>
		public bool SetEquals(IEnumerable<T> other) { return _set.SetEquals(other, _comparer, _count); }

		#endregion

		#region Operators: & | - ^

		//public static ObjectSet<T> operator &(ObjectSet<T> other) { new 

		#endregion
	}


	/// <summary>
	/// A mutable set of symbols.
	/// </summary><remarks>
	/// This is a very simple hashtrie optimized for symbols, based on 
	/// <see cref="InternalSet{T}"/>.
	/// <para/>
	/// Sorry, <c>null</c> is not permitted as a member of the set.
	/// </remarks>
	public class SymbolSet : ObjectSet<Symbol>
	{
	}

	[TestFixture]
	public class SymbolSetTests
	{
		[Test]
		public void Test()
		{
			// TODO
		}
	}
}
