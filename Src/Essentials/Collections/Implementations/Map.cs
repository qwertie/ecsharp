using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections.Impl;
using System.Diagnostics;

namespace Loyc.Collections
{
	/// <summary>
	/// An immutable dictionary.
	/// </summary>
	/// <remarks>
	/// This class is a read-only dictionary, known in comp-sci nerd speak as a 
	/// "persistent" data structure (not to be confused with the normal meaning
	/// of "persistent" as something that is saved to disk--this data structure
	/// is designed only to exist in memory). <c>Map</c> allows modification only 
	/// by creating new dictionaries. To create new dictionaries, this class 
	/// provides the following methods:
	/// <ul>
	/// <li><see cref="Union"/>, <see cref="Intersect"/>, <see cref="Except"/> 
	/// and <see cref="Xor"/> combine two dictionaries to create a new 
	/// dictionary, without modifying either of the original dictionaries.</li>
	/// <li><see cref="With"/> and <see cref="Without"/> create a new 
	/// dictionary with a single item added or removed.</li>
	/// <li>A C# cast operator is provided to convert a Map into an 
	/// <see cref="MMap{K,V}"/>.</li>
	/// </ul>
	/// See <see cref="MMap{K,V}"/> and <see cref="InternalSet{T}"/> for more
	/// information.
	/// </remarks>
	[Serializable]
	[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
	[DebuggerDisplay("Count = {Count}")]
	public class Map<K, V> : IDictionary<K, V>, ICollection<KeyValuePair<K, V>>, ICount, IEqualityComparer<KeyValuePair<K, V>>
	{
		public static readonly Map<K, V> Empty = new Map<K, V>(InternalSet<K>.DefaultComparer);
		internal InternalSet<KeyValuePair<K, V>> _set;
		// compares keys; never null (if user specifies null, ValueComparer<K>.Default is used)
		private IEqualityComparer<K> _keyComparer;
		internal IEqualityComparer<KeyValuePair<K, V>> Comparer { get { return this; } }
		private int _count;

		/// <summary>Creates an empty map. Consider using <see cref="Empty"/> instead.</summary>
		/// <remarks>This is marked <c>Obsolete</c> instead of <c>protected</c> so 
		/// that this class is compatible with the generic constraint known in C# 
		/// as <c>new()</c>.</remarks>
		[Obsolete("It is recommended to use Map<K,V>.Empty instead, to avoid an unnecessary memory allocation.")]
		public Map() : this(InternalSet<K>.DefaultComparer) { }
		/// <summary>Creates an empty map with the specified key comparer.</summary>
		public Map(IEqualityComparer<K> comparer) { _keyComparer = comparer ?? ValueComparer<K>.Default; }
		/// <summary>Creates a map with the specified elements.</summary>
		public Map(IEnumerable<KeyValuePair<K, V>> list) : this(list, InternalSet<K>.DefaultComparer) { }
		/// <summary>Creates a map with the specified elements and key comparer.</summary>
		public Map(IEnumerable<KeyValuePair<K, V>> list, IEqualityComparer<K> comparer)
		{
			_keyComparer = comparer ?? ValueComparer<K>.Default;
			_count = _set.UnionWith(list, Comparer, false);
			_set.CloneFreeze();
		}
		internal Map(InternalSet<KeyValuePair<K, V>> set, IEqualityComparer<K> keyComparer, int count)
		{
			_set = set;
			_keyComparer = keyComparer;
			_count = count;
			_set.CloneFreeze();
		}

		public InternalSet<KeyValuePair<K, V>> InternalSet { get { Debug.Assert(_set.IsRootFrozen); return _set; } }
		public IEqualityComparer<K> KeyComparer { get { return _keyComparer; } }
		
		#region Key comparison interface (with explanation)

		/// <summary>Not intended to be called by users.</summary>
		/// <remarks>
		/// The user can provide a <see cref="IEqualityComparer{K}"/> to compare keys. 
		/// However, InternalSet&lt;KeyValuePair&lt;K, V>> requires a comparer that 
		/// can compare <see cref="KeyValuePair<K, V>"/> values. Therefore, Map 
		/// implements IEqualityComparer&lt;KeyValuePair&lt;K, V>> to provide the 
		/// necessary comparer without an unnecessary memory allocation.
		/// </remarks>
		bool IEqualityComparer<KeyValuePair<K, V>>.Equals(KeyValuePair<K, V> x, KeyValuePair<K, V> y)
		{
			return _keyComparer.Equals(x.Key, y.Key);
		}
		/// <summary>Not intended to be called by users.</summary>
		int IEqualityComparer<KeyValuePair<K, V>>.GetHashCode(KeyValuePair<K, V> obj)
		{
			return _keyComparer.GetHashCode(obj.Key);
		}

		#endregion

		#region IDictionary<K,V>

		public void Add(K key, V value)
		{
			throw new ReadOnlyException();
		}
		public bool ContainsKey(K key)
		{
			var kvp = new KeyValuePair<K, V>(key, default(V));
			return _set.Find(ref kvp, Comparer);
		}
		public ICollection<K> Keys
		{
			get { return new KeyCollection<K, V>(this); }
		}
		public bool Remove(K key)
		{
			throw new ReadOnlyException();
		}
		public bool TryGetValue(K key, out V value)
		{
			Debug.Assert(_set.IsRootFrozen);
			var kvp = new KeyValuePair<K, V>(key, default(V));
			bool result = _set.Find(ref kvp, Comparer);
			value = kvp.Value;
			return result;
		}
		public ICollection<V> Values
		{
			get { return new ValueCollection<K, V>(this); }
		}
		public V this[K key]
		{
			get {
				Debug.Assert(_set.IsRootFrozen);
				var kvp = new KeyValuePair<K, V>(key, default(V));
				if (_set.Find(ref kvp, Comparer))
					return kvp.Value;
				throw new KeyNotFoundException();
			}
		}
		V IDictionary<K,V>.this[K key]
		{
			get { return this[key]; }
			set { throw new ReadOnlyException(); }
		}

		#endregion

		#region ICollection<KeyValuePair<K,V>>

		void ICollection<KeyValuePair<K,V>>.Add(KeyValuePair<K, V> item)
		{
			throw new ReadOnlyException();
		}
		void ICollection<KeyValuePair<K,V>>.Clear()
		{
			throw new ReadOnlyException();
		}
		public bool Contains(KeyValuePair<K, V> item)
		{
			V value;
			if (!TryGetValue(item.Key, out value))
				return false;
			return object.Equals(value, item.Value);
		}
		public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
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

		bool ICollection<KeyValuePair<K, V>>.Remove(KeyValuePair<K, V> item)
		{
			throw new ReadOnlyException();
		}

		public InternalSet<KeyValuePair<K, V>>.Enumerator GetEnumerator()
		{
			return _set.GetEnumerator();
		}
		IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator() { return GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

		#endregion

		public V TryGetValue(K key, V defaultValue)
		{
			var kvp = new KeyValuePair<K, V>(key, defaultValue);
			_set.Find(ref kvp, Comparer);
			return kvp.Value;
		}

		#region Persistent map operations: With, Without, Union, Except, Intersect, Xor

		/// <summary>Returns a copy of the current map with an additional key-value pair.</summary>
		/// <paparam name="replaceIfPresent">If true, the existing key-value pair is replaced if present. 
		/// Otherwise, the existing key-value pair is left unchanged.</paparam>
		/// <returns>A map with the specified key. If the key was already present 
		/// and replaceIfPresent is false, the same set ('this') is returned.</remarks>
		public Map<K, V> With(K key, V value, bool replaceIfPresent = true)
		{
			Debug.Assert(_set.IsRootFrozen);
			var set = _set;
			var item = new KeyValuePair<K, V>(key, value);
			if (set.Add(ref item, Comparer, replaceIfPresent))
				return new Map<K, V>(set, _keyComparer, _count + 1);
			if (replaceIfPresent)
				return new Map<K, V>(set, _keyComparer, _count);
			return this;
		}
		/// <summary>Returns a copy of the current map without the specified key.</summary>
		/// <returns>A map without the specified key. If the key was not present,
		/// the same set ('this') is returned.</remarks>
		public Map<K, V> Without(K key)
		{
			Debug.Assert(_set.IsRootFrozen);
			var set = _set;
			var item = new KeyValuePair<K, V>(key, default(V));
			if (set.Remove(ref item, Comparer))
				return new Map<K, V>(set, _keyComparer, _count - 1);
			return this;
		}
		/// <summary>Returns a copy of the current map with the specified items added.</summary>
		/// <param name="replaceWithValuesFromOther">When a key is present in both maps, 
		/// the values from 'other' replace the values in the current map.</param>
		public Map<K, V> Union(Map<K, V> other, bool replaceWithValuesFromOther = false) { return Union(other._set, replaceWithValuesFromOther); }
		/// <inheritdoc cref="Union(Map{K,V}, bool)"/>
		public Map<K,V> Union(MMap<K,V> other, bool replaceWithValuesFromOther = false) { return Union(other._set, replaceWithValuesFromOther); }
		internal Map<K,V> Union(InternalSet<KeyValuePair<K,V>> other, bool replaceWithValuesFromOther = false)
		{
			Debug.Assert(_set.IsRootFrozen);
			var set = _set;
			int count2 = _count + set.UnionWith(other, Comparer, replaceWithValuesFromOther);
			return new Map<K,V>(set, _keyComparer, count2);
		}
		/// <summary>Returns a copy of the current map with all keys removed from 
		/// this map that are not present in the other map. The <see cref="Values"/>
		/// in 'other' are ignored.</summary>
		public Map<K, V> Intersect(Map<K, V> other) { return Intersect(other._set, other.Comparer); }
		/// <inheritdoc cref="Intersect(Map{K,V})"/>
		public Map<K, V> Intersect(MMap<K, V> other) { return Intersect(other._set, other.Comparer); }
		internal Map<K,V> Intersect(InternalSet<KeyValuePair<K,V>> other, IEqualityComparer<KeyValuePair<K,V>> otherComparer)
		{
			Debug.Assert(_set.IsRootFrozen);
			var set = _set;
			int count2 = _count - set.IntersectWith(other, otherComparer);
			return new Map<K,V>(set, _keyComparer, count2);
		}
		/// <summary>Returns a copy of the current map with all keys removed from 
		/// this map that are present in the other map. The <see cref="Values"/>
		/// in 'other' are ignored.</summary>
		public Map<K, V> Except(Map<K, V> other) { return Except(other._set); }
		public Map<K,V> Except(MMap<K,V> other) { return Except(other._set); }
		internal Map<K,V> Except(InternalSet<KeyValuePair<K,V>> other)
		{
			Debug.Assert(_set.IsRootFrozen);
			var set = _set;
			int count2 = _count - set.ExceptWith(other, Comparer);
			return new Map<K,V>(set, _keyComparer, count2);
		}
		//     
		/// <summary>Duplicates the current map and then modifies it so that it 
		/// contains only keys that are present either in the current map or in 
		/// the specified other map, but not both.</summary>
		public Map<K, V> Xor(Map<K, V> other) { return Xor(other._set); }
		public Map<K,V> Xor(MMap<K,V> other) { return Xor(other._set); }
		internal Map<K,V> Xor(InternalSet<KeyValuePair<K,V>> other)
		{
			Debug.Assert(_set.IsRootFrozen);
			var set = _set;
			int count2 = _count + set.SymmetricExceptWith(other, Comparer);
			return new Map<K, V>(set, _keyComparer, count2);
		}

		#endregion

		public static explicit operator MMap<K, V>(Map<K, V> copy) { return new MMap<K, V>(copy._set, copy._keyComparer, copy._count); }
	}
}
