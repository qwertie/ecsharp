using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections.Impl;
using System.Diagnostics;

namespace Loyc.Collections
{
	/// <summary>Common base class that contains code shared between 
	/// <see cref="Map{K,V}"/> and <see cref="MMap{K,V}"/>.</summary>
	/// <remarks>You might notice that although <see cref="Map{K,V}"/> and 
	/// <see cref="MMap{K,V}"/> have a common base class, <see cref="Set{T}"/> and
	/// <see cref="MSet{T}"/> do not, and this is a mere implementation detail. 
	/// Since <see cref="Set{T}"/> is immutable, and small, and its fields can 
	/// safely be initialized to 0 or null, its default value is a valid set and 
	/// it makes sense to implement is as a struct. The same observation would 
	/// apply to <see cref="Map{K,V}"/> except for one problem: the comparer. The
	/// user can supply a comparer of type <c>IEqualityComparer&lt;K></c>, but
	/// but <see cref="Map{K,V}"/> contains a set of type 
	/// <c>InternalSet&lt;KeyValuePair&lt;K,V>></c>, which requires a comparer of 
	/// type <c>IEqualityComparer&lt;KeyValuePair&lt;K,V>></c>. In general, a 
	/// wrapper object is necessary to provide this comparer, and I decided to use
	/// the set itself as the wrapper object. Therefore, <see cref="Map{K,V}"/> 
	/// implements this interface, and it must be a class so that it is not boxed
	/// every time it is converted to this interface.
	/// <para/>
	/// Finally, since <see cref="Map{K,V}"/> and <see cref="MMap{K,V}"/> are both 
	/// classes and share some of the same code, I decided to factor out the 
	/// common code into this base class. The end.
	/// </remarks>
	[Serializable]
	[DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
	[DebuggerDisplay("Count = {Count}")]
	public class MapOrMMap<K, V> : IReadOnlyCollection<KeyValuePair<K, V>>, IEqualityComparer<KeyValuePair<K, V>>, IReadOnlyDictionary<K, V>
	{
		internal InternalSet<KeyValuePair<K, V>> _set;
		// compares keys; never null (if user specifies null, ValueComparer<K>.Default is used)
		internal IEqualityComparer<K> _keyComparer;
		internal IEqualityComparer<KeyValuePair<K, V>> Comparer { get { return this; } }
		// I wonder if this should use IntPtr, given that it'll probably be padded
		// to 8 bytes on x64 anyway? Nah, probably the C# compiler would make it 
		// slow, and InternalSet wasn't designed for collections larger than 2 
		// billion even if technically it can handle them.
		internal int _count;
		protected static readonly EqualityComparer<V> DefaultValueComparer = EqualityComparer<V>.Default;

		protected MapOrMMap() : this(InternalSet<K>.DefaultComparer) { }
		protected MapOrMMap(IEqualityComparer<K> comparer) { _keyComparer = comparer ?? ValueComparer<K>.Default; }
		protected MapOrMMap(IEnumerable<KeyValuePair<K, V>> list) : this(list, InternalSet<K>.DefaultComparer) { }
		protected MapOrMMap(IEnumerable<KeyValuePair<K, V>> list, IEqualityComparer<K> comparer)
		{
			_keyComparer = comparer ?? ValueComparer<K>.Default;
			_set = new InternalSet<KeyValuePair<K, V>>(list, this, out _count);
		}
		internal MapOrMMap(InternalSet<KeyValuePair<K, V>> set, IEqualityComparer<K> keyComparer, int count)
		{
			_set = set;
			_keyComparer = keyComparer;
			_count = count;
			_set.CloneFreeze();
		}

		public bool IsEmpty { get { return _count == 0; } }
		public IEqualityComparer<K> KeyComparer { get { return _keyComparer; } }
		public InternalSet<KeyValuePair<K, V>> FrozenInternalSet { get { _set.CloneFreeze(); return _set; } }

		#region Key comparison interface (with explanation)

		/// <summary>Not intended to be called by users.</summary>
		/// <remarks>
		/// The user can provide a <see cref="IEqualityComparer{K}"/> to compare keys. 
		/// However, InternalSet&lt;KeyValuePair&lt;K, V>> requires a comparer that 
		/// can compare <see cref="KeyValuePair<K, V>"/> values. Therefore, MapOrMMap 
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

		public bool ContainsKey(K key)
		{
			var kvp = new KeyValuePair<K, V>(key, default(V));
			return _set.Find(ref kvp, Comparer);
		}
		public bool TryGetValue(K key, out V value)
		{
			var kvp = new KeyValuePair<K, V>(key, default(V));
			bool result = _set.Find(ref kvp, Comparer);
			value = kvp.Value;
			return result;
		}
		public V this[K key]
		{
			get {
				var kvp = new KeyValuePair<K, V>(key, default(V));
				if (_set.Find(ref kvp, Comparer))
					return kvp.Value;
				throw new KeyNotFoundException();
			}
		}
		/// <summary>Retrieves the value associated with the specified key,
		/// or returns <c>defaultValue</c> if the key is not found.</summary>
		public V this[K key, V defaultValue]
		{
			get { 
				var kvp = new KeyValuePair<K, V>(key, defaultValue);
				_set.Find(ref kvp, Comparer);
				return kvp.Value;
			}
		}

		public bool Contains(KeyValuePair<K, V> item)
		{
			V value;
			if (!TryGetValue(item.Key, out value))
				return false;
			return DefaultValueComparer.Equals(value, item.Value);
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

		public InternalSet<KeyValuePair<K, V>>.Enumerator GetEnumerator()
		{
			return _set.GetEnumerator();
		}
		IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator() { return GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

		/// <summary>Synonym for this[key, defaultValue].</summary>
		public V TryGetValue(K key, V defaultValue)
		{
			return this[key, defaultValue];
		}

		/// <summary>Measures the total size of all objects allocated to this 
		/// collection, in bytes, including the size of this object itself; see
		/// <see cref="InternalSet{T}.CountMemory"/>.</summary>
		public virtual long CountMemory(int sizeOfPair)
		{
			return IntPtr.Size * 4 + _set.CountMemory(sizeOfPair);
		}

		public IEnumerable<K> Keys
		{
			get { return new KeyCollection<K, V>(this); }
		}

		public IEnumerable<V> Values
		{
			get { return new ValueCollection<K, V>(this); }
		}
	}

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
	public class Map<K, V> : MapOrMMap<K, V>, IDictionary<K, V>, ICollection<KeyValuePair<K, V>>, ISetOperations<KeyValuePair<K,V>, MapOrMMap<K, V>, Map<K, V>>
	{
		public static readonly Map<K, V> Empty = new Map<K, V>(InternalSet<K>.DefaultComparer);

		/// <summary>Creates an empty map. Consider using <see cref="Empty"/> instead.</summary>
		/// <remarks>This is marked <c>Obsolete</c> instead of <c>protected</c> so 
		/// that this class is compatible with the generic constraint known in C# 
		/// as <c>new()</c>.</remarks>
		[Obsolete("It is recommended to use Map<K,V>.Empty instead, to avoid an unnecessary memory allocation.")]
		public Map() : base() { }
		/// <summary>Creates an empty map with the specified key comparer.</summary>
		public Map(IEqualityComparer<K> comparer) : base(comparer) { }
		/// <summary>Creates a map with the specified elements.</summary>
		public Map(IEnumerable<KeyValuePair<K, V>> list) : this(list, InternalSet<K>.DefaultComparer) { }
		/// <summary>Creates a map with the specified elements and key comparer.</summary>
		public Map(IEnumerable<KeyValuePair<K, V>> list, IEqualityComparer<K> comparer) : base(list, comparer) { _set.CloneFreeze(); }
		internal Map(InternalSet<KeyValuePair<K, V>> set, IEqualityComparer<K> keyComparer, int count) : base(set, keyComparer, count) { }

		public new InternalSet<KeyValuePair<K, V>> FrozenInternalSet { get { Debug.Assert(_set.IsRootFrozen); return _set; } }

		#region IDictionary<K,V>

		void IDictionary<K,V>.Add(K key, V value)
		{
			throw new ReadOnlyException();
		}
		public new ICollection<K> Keys
		{
			get { return new KeyCollection<K, V>(this); }
		}
		bool IDictionary<K,V>.Remove(K key)
		{
			throw new ReadOnlyException();
		}
		public new ICollection<V> Values
		{
			get { return new ValueCollection<K, V>(this); }
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
		public bool IsReadOnly
		{
			get { return true; }
		}
		bool ICollection<KeyValuePair<K, V>>.Remove(KeyValuePair<K, V> item)
		{
			throw new ReadOnlyException();
		}

		#endregion

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
		public Map<K, V> With(KeyValuePair<K, V> item) { return With(item.Key, item.Value); }
		Map<K, V> ISetOperations<KeyValuePair<K,V>, MapOrMMap<K, V>, Map<K, V>>.Without(KeyValuePair<K, V> item)
		{
			V value;
			if (TryGetValue(item.Key, out value))
				if (DefaultValueComparer.Equals(item.Value, value))
					return Without(item.Key);
			return this;
		}

		/// <summary>Returns a copy of the current map with the specified items 
		/// added; each item is added only if the key is not already present.</summary>
		public Map<K,V> Union(MapOrMMap<K, V> other) { return Union(other, false); }
		/// <summary>Returns a copy of the current map with the specified items added.</summary>
		/// <param name="replaceWithValuesFromOther">When a key is present in both maps, 
		/// the values from 'other' replace the values in the current map.</param>
		public Map<K, V> Union(MapOrMMap<K, V> other, bool replaceWithValuesFromOther)
		{
			Debug.Assert(_set.IsRootFrozen);
			var set = _set;
			int count2 = _count + set.UnionWith(other._set, Comparer, replaceWithValuesFromOther);
			return new Map<K,V>(set, _keyComparer, count2);
		}
		/// <summary>Returns a copy of the current map with all keys removed from 
		/// this map that are not present in the other map. The <see cref="Values"/>
		/// in 'other' are ignored.</summary>
		public Map<K,V> Intersect(MapOrMMap<K,V> other)
		{
			Debug.Assert(_set.IsRootFrozen);
			var set = _set;
			int count2 = _count - set.IntersectWith(other._set, other.Comparer);
			return new Map<K,V>(set, _keyComparer, count2);
		}
		/// <summary>Returns a copy of the current map with all keys removed from 
		/// this map that are present in the other map. The <see cref="Values"/>
		/// in 'other' are ignored.</summary>
		public Map<K,V> Except(MapOrMMap<K,V> other)
		{
			Debug.Assert(_set.IsRootFrozen);
			var set = _set;
			int count2 = _count - set.ExceptWith(other._set, Comparer);
			return new Map<K,V>(set, _keyComparer, count2);
		}
		/// <summary>Duplicates the current map and then modifies it so that it 
		/// contains only keys that are present either in the current map or in 
		/// the specified other map, but not both.</summary>
		public Map<K,V> Xor(MapOrMMap<K,V> other)
		{
			Debug.Assert(_set.IsRootFrozen);
			var set = _set;
			int count2 = _count + set.SymmetricExceptWith(other._set, Comparer);
			return new Map<K, V>(set, _keyComparer, count2);
		}

		#endregion

		public static explicit operator MMap<K, V>(Map<K, V> copy)
		{
			return new MMap<K, V>(copy._set, copy._keyComparer, copy._count);
		}
	}
}
