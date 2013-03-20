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
	public class ObjectSet<T> : ICollection<T>
		#if DotNet4
		, ISet<T>
		#endif
	{
		InternalSet<T> _set = InternalSet<T>.Empty;
		IEqualityComparer<T> _comparer;
		int _count;
		
		public ObjectSet() { }
		public ObjectSet(IEnumerable<T> copy) { AddRange(copy); }
		public ObjectSet(IEnumerable<T> copy, IEqualityComparer<T> comparer) { _comparer = comparer; AddRange(copy); }
		public ObjectSet(IEqualityComparer<T> comparer) { _comparer = comparer; }

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
			int numAdded = 0;
			foreach (T item in items)
				if (Add(item))
					numAdded++;
			return numAdded;
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
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region ISet<T>

		public void ExceptWith(IEnumerable<T> other)
		{
			foreach (var item in other)
				Remove(item);
		}
		public void IntersectWith(IEnumerable<T> other)
		{
			var e = GetEnumerator();
			var otherSet = other as ObjectSet<T>;
			ICollection<T> coll;
			if (otherSet == null && (coll = other as ICollection<T>) != null && !(other is IList<T>)) {
				// We can't tell if 'other.Contains' is fast in advance, so
				// I'm using a heuristic: if 'other' implements ICollection<T>
				// but NOT IList<T> we'll assume it is fast, otherwise we assume
				// it is slow and take the second code path.
				foreach (var item in this)
					if (!coll.Contains(item))
						Remove(item);
			} else {
				otherSet = otherSet ?? new ObjectSet<T>(other);
				foreach (var item in this)
					if (!otherSet.Contains(item))
						Remove(item);
			}
		}

		public bool IsProperSubsetOf(IEnumerable<T> other)
		{
			throw new NotImplementedException();
		}

		public bool IsProperSupersetOf(IEnumerable<T> other)
		{
			throw new NotImplementedException();
		}

		public bool IsSubsetOf(IEnumerable<T> other)
		{
			throw new NotImplementedException();
		}

		public bool IsSupersetOf(IEnumerable<T> other)
		{
			throw new NotImplementedException();
		}

		public bool Overlaps(IEnumerable<T> other)
		{
			throw new NotImplementedException();
		}

		public bool SetEquals(IEnumerable<T> other)
		{
			throw new NotImplementedException();
		}

		public void SymmetricExceptWith(IEnumerable<T> other)
		{
			throw new NotImplementedException();
		}

		public void UnionWith(IEnumerable<T> other)
		{
			throw new NotImplementedException();
		}

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
