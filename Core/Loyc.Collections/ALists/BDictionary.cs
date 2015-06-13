namespace Loyc.Collections
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Collections.Specialized;
	using System.Diagnostics;
	using Loyc.Collections.Impl;
	using Loyc.Math;

	/// <summary>
	/// An sorted dictionary that is efficient for all operations and offers 
	/// indexed access to its list of key-value pairs.
	/// </summary>
	/// <remarks>
	/// An <a href="http://core.loyc.net/collections/alists-part2.html">article</a>
	/// about the BList classes is available.
	/// <para/>
	/// The keys must be comparable (ordered); if the type does not implement
	/// <c>IComparable</c> or <c>IComparable(T)</c>, you must provide a 
	/// Comparison(T) delegate to perform comparisons.
	/// <para/>
	/// This class offers the following additional features beyond what's offered 
	/// by the standard SortedDictionary{T} class: indexed access, a find-nearest-
	/// key operation called <see cref="FindLowerBound"/> (similar to lower_bound 
	/// in C++), observability, fast cloning, freezability, fast cloning of an 
	/// arbitrary range of items in a large collection, enumeration of part
	/// of the list (not just the entire list), and reverse enumeration, and a 
	/// few compound operations.
	/// <para/>
	/// Duplicate keys are not allowed in a BDictionary. If you would like to be
	/// able to associate multiple values with a single key, use 
	/// <see cref="BMultiMap{K,V}"/> instead. 
	/// <para/>
	/// If you need to store only keys, not values, use <see cref="BList{K}"/> 
	/// instead (but note that BList does allow duplicate keys).
	/// </remarks>
	[Serializable]
	public class BDictionary<K, V> : AListBase<K, KeyValuePair<K, V>>, 
		ICollectionEx<KeyValuePair<K, V>>, IAddRange<KeyValuePair<K, V>>, ICloneable<BDictionary<K,V>>, IDictionary<K,V>, IReadOnlyDictionary<K, V>
	{
		#region Constructors

		const int DefaultMaxInnerNodeSize = AListInnerBase<K, KeyValuePair<K, V>>.DefaultMaxNodeSize;
		const int DefaultMaxLeafNodeSize = AListLeaf<K, KeyValuePair<K, V>>.DefaultMaxNodeSize;
		protected readonly static Func<K, K, int> DefaultComparison = Comparer<K>.Default.Compare;

		/// <summary>Initializes an empty BList.</summary>
		/// <remarks>By default, elements of the list will be compared using
		/// <see cref="Comparer{T}.Default"/>.Compare.</remarks>
		public BDictionary() 
			: this(DefaultComparison, DefaultMaxLeafNodeSize, DefaultMaxInnerNodeSize) { }
		/// <inheritdoc cref="BDictionary(Func{K,K,int}, int, int)"/>
		public BDictionary(int maxLeafSize)
			: this(DefaultComparison, maxLeafSize, DefaultMaxInnerNodeSize) { }
		/// <inheritdoc cref="BDictionary(Func{K,K,int}, int, int)"/>
		public BDictionary(int maxLeafSize, int maxInnerSize)
			: this(DefaultComparison, maxLeafSize, maxInnerSize) { }
		/// <inheritdoc cref="BDictionary(Func{K,K,int}, int, int)"/>
		public BDictionary(Func<K, K, int> compareKeys)
			: this(compareKeys, DefaultMaxLeafNodeSize, DefaultMaxInnerNodeSize) { }
		/// <inheritdoc cref="BDictionary(Func{K,K,int}, int, int)"/>
		public BDictionary(Func<K, K, int> compareKeys, int maxLeafSize)
			: this(compareKeys, maxLeafSize, DefaultMaxInnerNodeSize) { }
		
		/// <summary>Initializes an empty BDictionary.</summary>
		/// <param name="compareKeys">A method that compares two items and returns 
		/// a negative number (typically -1) if the first item is smaller than the 
		/// second item, 0 if it is equal, and a positive number (typically 1) if 
		/// it is greater.</param>
		/// <param name="maxLeafSize">Maximum number of elements to place in a leaf node of the B+ tree.</param>
		/// <param name="maxInnerSize">Maximum number of elements to place in an inner node of the B+ tree.</param>
		/// <remarks>
		/// If present, the compareKeys parameter must be a "Func" delegate instead 
		/// of the more conventional <see cref="Comparison{T}"/> delegate for an 
		/// obscure design decision for the benefit of <see cref="BList{T}"/>.
		/// You should not notice any difference between the two, but the stupid 
		/// .NET type system  insists that the two types are not compatible. So, if 
		/// (for some reason) you already happen to have a <see cref="Comparison{K}"/> 
		/// delegate, you must explicitly convert it to a Func delegate with code 
		/// such as "new Func&lt;K,K,int>(comparisonDelegate)".
		/// <para/>
		/// If you leave out the compareKeys parameter, <see cref="Comparer{K}.Default"/>.Compare
		/// will be used by default.
		/// <para/>
		/// See the documentation of <see cref="AListBase{K,T}"/> for a discussion
		/// about node sizes.
		/// <para/>
		/// An empty BDictionary is created with no root node, so it consumes much less 
		/// memory than a BDictionary with a single element.
		/// </remarks>
		public BDictionary(Func<K,K,int> compareKeys, int maxLeafSize, int maxInnerSize)
			: base(maxLeafSize, maxInnerSize) { _compareKeys = compareKeys; }

		/// <inheritdoc cref="Clone(bool)"/>
		/// <param name="items">A list of items to be cloned.</param>
		public BDictionary(BDictionary<K,V> items, bool keepListChangingHandlers) 
			: base(items, keepListChangingHandlers) { _compareKeys = items._compareKeys; }

		protected BDictionary(BDictionary<K, V> original, AListNode<K, KeyValuePair<K,V>> section) 
			: base(original, section) { _compareKeys = original._compareKeys; }

		#endregion

		protected Func<K, K, int> _compareKeys;

		#region General supporting protected methods

		protected override AListNode<K, KeyValuePair<K, V>> NewRootLeaf()
		{
			return new BListLeaf<K, KeyValuePair<K, V>>(_maxLeafSize);
		}

		protected override AListInnerBase<K, KeyValuePair<K, V>> SplitRoot(AListNode<K, KeyValuePair<K, V>> left, AListNode<K, KeyValuePair<K, V>> right)
		{
			return new BDictionaryInner<K, V>(left, right, _maxInnerSize);
		}

		protected internal override K GetKey(KeyValuePair<K, V> item)
		{
			return item.Key;
		}

		protected int CompareToKey(KeyValuePair<K, V> item, K key)
		{
			return _compareKeys(item.Key, key);
		}

		internal new int DoSingleOperation(ref AListSingleOperation<K, KeyValuePair<K, V>> op)
		{
			op.CompareKeys = _compareKeys;
			op.CompareToKey = CompareToKey;
			op.Key = op.Item.Key;
			return base.DoSingleOperation(ref op);
		}

		internal new void OrganizedRetrieve(ref AListSingleOperation<K, KeyValuePair<K, V>> op)
		{
			op.CompareKeys = _compareKeys;
			op.CompareToKey = CompareToKey;
			base.OrganizedRetrieve(ref op);
		}

		#endregion

		#region Standard operations with KeyValuePair: Add, Contains, Remove, IndexOf

		public void Add(KeyValuePair<K, V> item)
		{
			var op = new AListSingleOperation<K, KeyValuePair<K, V>>();
			op.Mode = AListOperation.AddOrThrow;
			op.Item = item;
			DoSingleOperation(ref op);
		}
		
		public int IndexOf(KeyValuePair<K, V> item)
		{
			var op = new AListSingleOperation<K, KeyValuePair<K, V>>();
			op.Item = item;
			op.Key = item.Key;
			op.RequireExactMatch = true;
			OrganizedRetrieve(ref op);
			if (!op.Found)
				return -1;
			Debug.Assert(item.Equals(op.Item));
			return (int)op.BaseIndex;
		}

		public bool Contains(KeyValuePair<K, V> item)
		{
			return IndexOf(item) > -1;
		}

		bool ICollection<KeyValuePair<K,V>>.IsReadOnly
		{
			get { return IsFrozen; }
		}

		public bool Remove(KeyValuePair<K, V> item)
		{
			var op = new AListSingleOperation<K, KeyValuePair<K, V>>();
			op.Mode = AListOperation.Remove;
			op.Item = item;
			op.RequireExactMatch = true;
			return DoSingleOperation(ref op) < 0;
		}

		#endregion

		#region FindLowerBound, FindUpperBound, IndexOf(key)

		/// <summary>Finds the lowest index of an item with a key that is equal to 
		/// or greater than the specified key.</summary>
		/// <param name="key">The key to find. If passed by reference, when this 
		/// method returns, key is set to the key of the item that was found, or to 
		/// the next greater item if the item was not found. If the item passed in 
		/// is higher than all items in the list, it will be left unchanged when 
		/// this method returns.</param>
		/// <param name="found">Set to true if the item was found, false if not.</param>
		/// <returns>The index of the item that was found, or of the next
		/// greater item, or Count if the given item is greater than all items 
		/// in the list.</returns>
		public int FindLowerBound(K key)
		{
			bool found;
			return FindLowerBound(key, out found);
		}
		/// <inheritdoc cref="FindLowerBound(K)"/>
		public int FindLowerBound(K key, out bool found)
		{
			var op = new AListSingleOperation<K, KeyValuePair<K, V>>();
			op.Key = key;
			OrganizedRetrieve(ref op);
			found = op.Found;
			return (int)op.BaseIndex;
		}
		/// <inheritdoc cref="FindLowerBound(K)"/>
		public int FindLowerBound(ref K key)
		{
			bool found;
			return FindLowerBound(ref key, out found);
		}
		/// <inheritdoc cref="FindLowerBound(K)"/>
		public int FindLowerBound(ref K key, out bool found)
		{
			var op = new AListSingleOperation<K, KeyValuePair<K, V>>();
			op.Key = key;
			OrganizedRetrieve(ref op);
			if (found = op.Found)
				key = op.Item.Key;
			else if ((int)op.BaseIndex < Count)
				key = this[(int)op.BaseIndex].Key;
			return (int)op.BaseIndex;
		}

		public int IndexOf(K key)
		{
			bool found;
			int index = FindLowerBound(key, out found);
			if (found)
				return index;
			else
				return -1;
		}

		/// <summary>Finds the index of the first item in the list that is greater 
		/// than the specified item.</summary>
		/// <param name="key">The item to find. If passed by reference, when this 
		/// method returns, item is set to the next greater item than the item you 
		/// searched for, or left unchanged if there is no greater item.</param>
		/// <returns>The index of the next greater item that was found,
		/// or Count if the given item is greater than all items in the list.</returns>
		public int FindUpperBound(K key)
		{
			var op = new AListSingleOperation<K, KeyValuePair<K, V>>();
			// When searchKey==candidate, act like searchKey>candidate.
			Func<K, K, int> upperBoundCmp = (candidate, searchKey) => -(_compareKeys(searchKey, candidate) | 1);
			op.Key = key;
			op.CompareKeys = upperBoundCmp;
			op.CompareToKey = (pair, k) => upperBoundCmp(pair.Key, k);
			base.OrganizedRetrieve(ref op);
			return (int)op.BaseIndex;
		}
		public int FindUpperBound(ref K key)
		{
			int index = FindUpperBound(key);
			if (index < Count)
				key = this[index].Key;
			return index;
		}

		#endregion

		#region AddRange, RemoveRange

		void IAddRange<KeyValuePair<K, V>>.AddRange(IReadOnlyCollection<KeyValuePair<K, V>> s) { AddRange(s); }

		public void AddRange(IEnumerable<KeyValuePair<K, V>> e)
		{
			foreach (var pair in e)
				Add(pair);
		}
		
		public int RemoveRange(IEnumerable<KeyValuePair<K, V>> e)
		{
			int removeCount = 0;
			foreach (var pair in e)
				if (Remove(pair))
					removeCount++;
			return removeCount;
		}

		public int RemoveRange(IEnumerable<K> e)
		{
			int removeCount = 0;
			foreach (var key in e)
				if (Remove(key))
					removeCount++;
			return removeCount;
		}

		#endregion

		#region IDictionary<K,V> Members

		public void Add(K key, V value)
		{
			Add(new KeyValuePair<K, V>(key, value));
		}

		public bool ContainsKey(K key)
		{
			bool found;
			FindLowerBound(key, out found);
			return found;
		}

		public ICollection<K> Keys
		{
			get { return new KeyCollection<K, V>(this); }
		}
		public ICollection<V> Values
		{
			get { return new ValueCollection<K, V>(this); }
		}
		IEnumerable<K> IReadOnlyDictionary<K, V>.Keys { get { return Keys; } }
		IEnumerable<V> IReadOnlyDictionary<K, V>.Values { get { return Values; } }

		public bool Remove(K key)
		{
			var op = new AListSingleOperation<K, KeyValuePair<K, V>>();
			op.Mode = AListOperation.Remove;
			op.Key = key;
			op.CompareKeys = _compareKeys;
			op.CompareToKey = CompareToKey;
			return base.DoSingleOperation(ref op) < 0;
		}

		public bool TryGetValue(K key, out V value)
		{
			var op = new AListSingleOperation<K, KeyValuePair<K, V>>();
			op.Key = key;
			OrganizedRetrieve(ref op);
			value = op.Item.Value;
			return op.Found;
		}

		public V this[K key]
		{
			get {
				V value;
				if (!TryGetValue(key, out value))
					throw new KeyNotFoundException();
				return value;
			}
			set {
				var op = new AListSingleOperation<K, KeyValuePair<K, V>>();
				op.Mode = AListOperation.AddOrReplace;
				op.Item = new KeyValuePair<K,V>(key, value);
				DoSingleOperation(ref op);
			}
		}

		public V this[K key, V defaultValue]
		{
			get {
				var op = new AListSingleOperation<K, KeyValuePair<K, V>>();
				op.Key = key;
				OrganizedRetrieve(ref op);
				if (!op.Found)
					return defaultValue;
				return op.Item.Value;
			}
		}

		#endregion

		#region Bonus features delegated to AListBase: Clone, CopySection, RemoveSection

		/// <inheritdoc cref="Clone(bool)"/>
		public BDictionary<K, V> Clone()
		{
			return Clone(true);
		}
		/// <summary>Clones a BDictionary.</summary>
		/// <param name="keepListChangingHandlers">If true, ListChanging handlers
		/// will be copied from the existing list of items to the new collection.
		/// Note: if it exists, the NodeObserver is never copied. 
		/// <see cref="AListBase{K,T}.ObserverCount"/> will be 0 in the new list.</param>
		/// <remarks>
		/// Cloning is performed in O(1) time by marking the tree root as frozen 
		/// and sharing it between the two lists. However, the new dictionary 
		/// itself will not be frozen, even if the original dictionary was marked 
		/// as frozen. Instead, nodes will be copied on demand when you modify the 
		/// new dictionary.
		/// </remarks>
		public BDictionary<K, V> Clone(bool keepListChangingHandlers)
		{
			return new BDictionary<K, V>(this, keepListChangingHandlers);
		}

		public BDictionary<K, V> CopySection(int start, int subcount)
		{
			return new BDictionary<K, V>(this, CopySectionHelper(start, subcount));
		}
		public BDictionary<K, V> RemoveSection(int start, int count)
		{
			if ((uint)count > _count - (uint)start)
				throw new ArgumentOutOfRangeException(count < 0 ? "count" : "start+count");

			var newList = new BDictionary<K, V>(this, CopySectionHelper(start, count));
			RemoveRange(start, count);
			return newList;
		}

		#endregion

		#region Other operations: AddIfNotPresent, ReplaceIfPresent, SetAndGetOldValue

		/// <summary>Adds the specified pair only if the key is not already present in the dictionary.</summary>
		/// <param name="key">Key to search for or add. If this parameter is passed by reference and a matching pair exists already, this method sets it to the existing key instance.</param>
		/// <param name="value">Value to search for or add. If this parameter is passed by reference and a matching pair exists already, this method sets it to the existing value.</param>
		/// <returns>True if the new pair was added, false if not.</returns>
		/// <remarks>
		/// This method has no effect if the key is already present.
		/// </remarks>
		public bool AddIfNotPresent(K key, V value)
		{
			var op = new AListSingleOperation<K, KeyValuePair<K, V>>();
			op.Mode = AListOperation.AddIfNotPresent;
			op.Item = new KeyValuePair<K,V>(key, value);
			DoSingleOperation(ref op);
			return !op.Found;
		}

		/// <inheritdoc cref="AddIfNotPresent(K,V)"/>
		public bool AddIfNotPresent(K key, ref V value)
		{
			return AddIfNotPresent(ref key, ref value);
		}

		/// <inheritdoc cref="AddIfNotPresent(K,V)"/>
		public bool AddIfNotPresent(ref K key, ref V value)
		{
			var op = new AListSingleOperation<K, KeyValuePair<K, V>>();
			op.Mode = AListOperation.AddIfNotPresent;
			op.Item = new KeyValuePair<K, V>(key, value);
			DoSingleOperation(ref op);
			if (op.Found)
			{
				key = op.Item.Key;
				value = op.Item.Value;
			}
			return !op.Found;
		}

		/// <summary>Associates the specified value with the specified key, while getting the old value if one exists.</summary>
		/// <param name="key">Key to search for or add. If this parameter is passed by reference and a matching pair existed already, this method sets it to the old key instance.</param>
		/// <param name="value">Value to search for or add. If this parameter is passed by reference and a matching pair existed already, this method sets it to the old value.</param>
		/// <returns>True if the new pair was added, false if it was replaced.</returns>
		public bool SetAndGetOldValue(K key, ref V value)
		{
			return SetAndGetOldValue(ref key, ref value);
		}

		/// <inheritdoc cref="SetAndGetOldValue(K,ref V)"/>
		public bool SetAndGetOldValue(ref K key, ref V value)
		{
			var op = new AListSingleOperation<K, KeyValuePair<K, V>>();
			op.Mode = AListOperation.AddOrReplace;
			op.Item = new KeyValuePair<K, V>(key, value);
			DoSingleOperation(ref op);
			if (op.Found)
			{
				key = op.Item.Key;
				value = op.Item.Value;
			}
			return !op.Found;
		}

		/// <summary>Replaces the value associated with a specified key, if it already exists in the dictionary.</summary>
		/// <param name="key">Key to replace. If this parameter is passed by reference and a matching pair existed, this method sets it to the old key instance.</param>
		/// <param name="value">New value to associate with the key. If this parameter is passed by reference and a matching pair existed, this method sets it to the old value.</param>
		/// <returns>True if the key was found and the pair was replaced, false if it was not found.</returns>
		/// <remarks>
		/// This method has no effect if the key was not already present.
		/// </remarks>
		public bool ReplaceIfPresent(K key, V value)
		{
			var op = new AListSingleOperation<K, KeyValuePair<K, V>>();
			op.Mode = AListOperation.ReplaceIfPresent;
			op.Item = new KeyValuePair<K, V>(key, value);
			DoSingleOperation(ref op);
			return op.Found;
		}

		/// <inheritdoc cref="ReplaceIfPresent(K,V)"/>
		public bool ReplaceIfPresent(K key, ref V value)
		{
			return ReplaceIfPresent(ref key, ref value);
		}

		/// <inheritdoc cref="ReplaceIfPresent(K,V)"/>
		public bool ReplaceIfPresent(ref K key, ref V value)
		{
			var op = new AListSingleOperation<K, KeyValuePair<K, V>>();
			op.Mode = AListOperation.ReplaceIfPresent;
			op.Item = new KeyValuePair<K, V>(key, value);
			DoSingleOperation(ref op);
			if (op.Found)
			{
				key = op.Item.Key;
				value = op.Item.Value;
			}
			return op.Found;
		}

		#endregion


	}
}
