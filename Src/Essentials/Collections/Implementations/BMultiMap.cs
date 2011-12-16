using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections.Impl;
using System.Diagnostics;

namespace Loyc.Collections
{
	using System;

	/// <summary>
	/// An sorted dictionary that allows multiple values to be associated with a
	/// single key.
	/// </summary>
	/// <remarks>
	/// Often when people want to be able to associate multiple values with a 
	/// single key, they use a Dictionary with values of type <see cref="List{T}"/>.
	/// This approach is very inefficient (in terms of memory use) if most keys are 
	/// only associated with one or two values; this class solves the problem using
	/// a single sorted B+ tree for all keys and all values. It requires, however,
	/// that both the keys and values are totally ordered (i.e. are sortable).
	/// <para/>
	/// By default, keys and values are sorted using <see cref="Comparer{T}.Default"/>.
	/// This will work provided that the keys and values both implement the
	/// <see cref="IComparable{T}"/> interface. If they don't, you can pass custom 
	/// comparison functions to the constructor instead (one comparison function for
	/// keys, and a second one for values).
	/// <para/>
	/// Since it is derived from <see cref="BList{T}"/>, this class enjoys the space 
	/// efficiency of a B+ tree and capabilities of a <see cref="AListBase{K,V}"/>, 
	/// although it tends to be slower than <see cref="Dictionary{K,V}"/>.
	/// </remarks>
	class BMultiMap<K, V> : BList<KeyValuePair<K, V>>
	{
		#region Constructors

		const int DefaultMaxLeafNodeSize = AListLeaf<KeyValuePair<K, V>, KeyValuePair<K, V>>.DefaultMaxNodeSize;
		const int DefaultMaxInnerNodeSize = AListInnerBase<KeyValuePair<K, V>, KeyValuePair<K, V>>.DefaultMaxNodeSize;

		public BMultiMap()
			: this(DefaultMaxLeafNodeSize, DefaultMaxInnerNodeSize) { }
		public BMultiMap(int maxLeafSize)
			: this(maxLeafSize, DefaultMaxInnerNodeSize) { }
		public BMultiMap(int maxLeafSize, int maxInnerSize)
			: base(DefaultPairComparison, maxLeafSize, maxInnerSize)
		{
			_compareKeys = DefaultKComparison;
			_compareValues = DefaultVComparison;
		}
		
		public BMultiMap(Func<K, K, int> compareKeys)
			: this(compareKeys, DefaultVComparison) { }
		public BMultiMap(Func<K, K, int> compareKeys, Func<V, V, int> compareValues)
			: this(compareKeys, compareValues, DefaultMaxLeafNodeSize, DefaultMaxInnerNodeSize) { }
		public BMultiMap(Func<K, K, int> compareKeys, Func<V, V, int> compareValues, int maxLeafSize)
			: this(compareKeys, compareValues, maxLeafSize, DefaultMaxInnerNodeSize) { }
		public BMultiMap(Func<K, K, int> compareKeys, Func<V, V, int> compareValues, int maxLeafSize, int maxInnerSize)
			: base(DefaultPairComparison, maxLeafSize, maxInnerSize)
		{
			_compareKeys = compareKeys;
			_compareValues = compareValues;
			_compareItems = CompareKeyAndValue;
		}

		#endregion
		
		#region Member variables and comparison functions

		protected readonly static Func<K, K, int> DefaultKComparison = Comparer<K>.Default.Compare;
		protected readonly static Func<V, V, int> DefaultVComparison = Comparer<V>.Default.Compare;
		protected readonly static Func<KeyValuePair<K, V>, KeyValuePair<K, V>, int> DefaultPairComparison = (a, b) =>
		{
			int c = DefaultKComparison(a.Key, b.Key);
			if (c != 0)
				return c;
			return DefaultVComparison(a.Value, b.Value);
		};

		protected readonly Func<K, K, int> _compareKeys;
		protected readonly Func<V, V, int> _compareValues;

		public int CompareKeyAndValue(KeyValuePair<K, V> a, KeyValuePair<K, V> b)
		{
			int c = _compareKeys(a.Key, b.Key);
			if (c != 0)
				return c;
			return _compareValues(a.Value, b.Value);
		}
		public int CompareKeysOnly(KeyValuePair<K, V> a, KeyValuePair<K, V> b)
		{
			return _compareKeys(a.Key, b.Key);
		}
		public int UpperBoundCompare(KeyValuePair<K, V> candidate, KeyValuePair<K, V> searchKey)
		{
			// When searchKey==candidate, act like searchKey>candidate.
			return -(_compareKeys(searchKey.Key, candidate.Key) | 1);
		}
		
		#endregion

		#region IDictionary-like Members

		/// <summary>Adds a key-value pair if there is not already a pair that 
		/// compares equal to the new one.</summary>
		/// <returns>True if the pair was added, or false if it already existed.</returns>
		public bool AddIfUnique(K key, V value)
		{
			return Do(AListOperation.AddIfNotPresent, new KeyValuePair<K, V>(key, value)) > 0;
		}

		/// <summary>Finds out whether the specified key is present.</summary>
		/// <param name="key">Key to search for</param>
		/// <returns>Returns true if the dictionary contains at least one key-
		/// value pair in which the key compares equal to the specified key.</returns>
		public bool ContainsKey(K key)
		{
			var op = new AListSingleOperation<KeyValuePair<K, V>, KeyValuePair<K, V>>();
			op.CompareToKey = op.CompareKeys = CompareKeysOnly;
			op.Key = new KeyValuePair<K,V>(key, default(V));
			OrganizedRetrieve(ref op);
			return op.Found;
		}

		/// <summary>Finds the lowest index of an item with the specified key.</summary>
		/// <param name="key">Key to search for</param>
		/// <returns>The index of the item that was found, or -1 if there is no such item.</returns>
		/// <remarks>This method is like <see cref="FindLowerBound"/> except that 
		/// it returns -1 if the key was not found.</remarks>
		public int FirstIndexOf(K key)
		{
			bool found;
			int index = FindLowerBound(key, out found);
			if (found)
				return index;
			else
				return -1;
		}

		/// <summary>Removes one pair from the collection that matches the specified key.</summary>
		/// <returns>True if a pair was removed, or false if the key was not found.</returns>
		public bool RemoveAny(K key)
		{
			var op = new AListSingleOperation<KeyValuePair<K, V>, KeyValuePair<K, V>>();
			op.Mode = AListOperation.Remove;
			op.CompareToKey = op.CompareKeys = CompareKeysOnly;
			op.Key = op.Item = new KeyValuePair<K, V>(key, default(V));
			return DoSingleOperation(ref op) < 0;
		}

		/// <summary>Removes up to a specified number of items from the collections 
		/// that have the specified key.</summary>
		/// <param name="key">The key to remove.</param>
		/// <param name="maxToRemove">Maximum number of items to remove.</param>
		/// <returns>The number of items removed.</returns>
		public int Remove(K key, int maxToRemove)
		{
			bool found;
			int lower = FindLowerBound(key, out found);
			if (!found)
				return 0;

			if (maxToRemove > 1)
			{
				int upper = FindUpperBound(key);
				int removeCount = Math.Min(maxToRemove, upper - lower);
				RemoveRange(lower, removeCount);
				return removeCount;
			}
			else if (maxToRemove == 1)
			{
				RemoveAt(lower);
				return 1;
			}
			return 0;
		}

		/// <summary>Removes all the items from the collection whose key compares 
		/// equal to the specified key.</summary>
		/// <param name="key">The key to remove.</param>
		/// <returns>The number of items removed.</returns>
		public int RemoveAll(K key)
		{
			return Remove(key, int.MaxValue);
		}

		/// <summary>Finds a value associated with the specified key.</summary>
		/// <param name="key">Key to find</param>
		/// <param name="value">Set to the value associated with that value, or default(V) if the key was not found.</param>
		/// <returns>True if the key was found, false if not.</returns>
		public bool TryGetValue(K key, out V value)
		{
			bool found;
			FindLowerBound(key, out value, out found);
			return found;
		}

		/// <summary>Gets a collection associated with the specified key.</summary>
		/// <param name="key"></param>
		/// <returns>A synthetic collection associated with the specified key.</returns>
		/// <remarks>This property always succeeds and does not actually search
		/// for the key you requested. It returns an object that represents the
		/// set of values associated with a key, but those values are not actually
		/// retrieved from the collection until you enumerate the collection.</remarks>
		/// <seealso cref="Values"/>
		public Values this[K key]
		{
			get { return new Values(this, key); }
		}

		/// <summary>Represents the set of values associated with a particular key 
		/// in a <see cref="BMultiMap{K,V}"/> collection.</summary>
		public struct Values : ISource<V>, ICollection<V>
		{
			readonly BMultiMap<K, V> _map;
			readonly K _key;
			
			internal Values(BMultiMap<K, V> map, K key)
			{
				_map = map;
				_key = key;
			}

			#region ISource<V> members

			public int Count
			{
				get {
					bool found;
					int lower = _map.FindLowerBound(_key, out found);
					if (!found)
						return 0;
					int upper = _map.FindUpperBound(_key);
					return upper - lower;
				}
			}

			public Iterator<V> GetIterator()
			{
				int index = _map.FirstIndexOf(_key);
				if (index <= -1)
					return EmptyIterator<V>.Value;

				var it = _map.GetIterator(index);
				var map = _map;
				var key = _key;
				return (ref bool ended) =>
				{
					var pair = it(ref ended);
					if (!ended)
					{
						if (map._compareKeys(pair.Key, key) != 0)
							ended = true;
					}
					return pair.Value;
				};
			}

			#endregion

			#region ICollection<V> Members

			/// <summary>Adds a new item associated with the key that this object 
			/// represents. Allows duplicate values.</summary>
			/// <param name="item">Value to add.</param>
			public void Add(V item)
			{
				_map.Add(new KeyValuePair<K,V>(_key, item));
			}
			public void Clear()
			{
				_map.RemoveAll(_key);
			}
			public bool Contains(V item)
			{
				return _map.Contains(new KeyValuePair<K, V>(_key, item));
			}
			public void CopyTo(V[] array, int arrayIndex)
			{
				LCInterfaces.CopyTo(this, array, arrayIndex);
			}
			public bool IsReadOnly
			{
				get { return _map.IsFrozen; }
			}
			public bool Remove(V item)
			{
				return _map.Remove(new KeyValuePair<K, V>(_key, item));
			}
			public IEnumerator<V> GetEnumerator()
			{
				return GetIterator().AsEnumerator();
			}
			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			#endregion
		}

		#endregion

		#region FindLowerBound, FindUpperBound
		
		/// <summary>Finds the lowest index of an item that is equal to or greater than the specified item.</summary>
		/// <param name="key">The key to find.</param>
		/// <param name="value">The first value associated with the specified key,
		/// if the key was found, or default(V) if not.</param>
		/// <param name="found">Set to true if the item was found, false if not.</param>
		/// <returns>The index of the item that was found, or of the next greater
		/// item, or Count if the given key is greater than the keys of all items 
		/// in the list.</returns>
		public int FindLowerBound(K key)
		{
			bool found;
			V value;
			return FindLowerBound(key, out value, out found);
		}
		/// <inheritdoc cref="FindLowerBound(K)"/>
		public int FindLowerBound(K key, out bool found)
		{
			V value;
			return FindLowerBound(key, out value, out found);
		}
		/// <inheritdoc cref="FindLowerBound(K)"/>
		public int FindLowerBound(K key, out V value, out bool found)
		{
			var op = new AListSingleOperation<KeyValuePair<K, V>, KeyValuePair<K, V>>();
			op.CompareKeys = op.CompareToKey = CompareKeysOnly;
			op.Key = new KeyValuePair<K, V>(key, default(V));
			op.LowerBound = true;
			OrganizedRetrieve(ref op);
			found = op.Found;
			value = op.Item.Value;
			return (int)op.BaseIndex;
		}

		/// <summary>Finds the index of the first item in the list that is greater 
		/// than the specified item.</summary>
		/// <param name="item">The item to find. If passed by reference, when this 
		/// method returns, item is set to the next greater item than the item you 
		/// searched for, or left unchanged if there is no greater item.</param>
		/// <param name="index">The index of the next greater item that was found,
		/// or Count if the given item is greater than all items in the list.</param>
		/// <returns></returns>
		public int FindUpperBound(K key)
		{
			var op = new AListSingleOperation<KeyValuePair<K, V>, KeyValuePair<K, V>>();
			op.CompareKeys = op.CompareToKey = UpperBoundCompare;
			op.Key = new KeyValuePair<K, V>(key, default(V));
			OrganizedRetrieve(ref op);
			return (int)op.BaseIndex;
		}
 
		#endregion
	}
}
