using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections.Impl;
using System.Diagnostics;

namespace Loyc.Collections
{
	/// <summary>
	/// A dictionary class built on top of <c>InternalSet&lt;KeyValuePair&lt;K,V>></c>.
	/// </summary>
	/// <typeparam name="K"></typeparam>
	/// <typeparam name="V"></typeparam>
	/// <remarks>
	/// Benchmarks show that this class is not as fast as the standard <see 
	/// cref="Dictionary{K,V}"/> in most cases. however, it does have some 
	/// advantages:
	/// <ul>
	/// <li>MMap allows null as a key (assuming it is based on the second version
	/// of <see cref="Impl.InternalSet{T}"/>).</li>
	/// <li><see cref="MapOrMMap{K, V}.TryGet"/> and <see cref="MapOrMMap{K, V}.ContainsKey"/> do not throw
	/// an incredibly annoying exception if you have the audacity to ask whether 
	/// there is a null key in the collection.</li>
	/// <li>This class supports fast cloning in O(1) time.</li>
	/// <li>You can convert a mutable <see cref="MMap{K,V}"/> into an immutable
	/// <see cref="Map{K,V}"/>, a read-only dictionary that does not change when 
	/// you change the original MMap.</li>
	/// <li>This class has bonus methods defined by <see cref="IDictionaryEx{K, V}"/>.</li>
	/// <li>The persistent map operations <see cref="Union"/>, 
	/// <see cref="Intersect"/>, <see cref="Except"/> and <see cref="Xor"/> 
	/// combine two dictionaries to create a new dictionary, without modifying 
	/// either of the original dictionaries.</li>
	/// <li>The methods <see cref="With"/> and <see cref="Without"/> create a new 
	/// dictionary with a single item added or removed.</li>
	/// </ul>
	/// The documentation of <see cref="InternalSet{T}"/> describes how the data 
	/// structure works.
	/// </remarks>
	[Serializable]
	[DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
	[DebuggerDisplay("Count = {Count}")]
	public class MMap<K, V> : MapOrMMap<K, V>, IDictionaryEx<K, V>, ICollection<KeyValuePair<K, V>>, ICloneable<MMap<K, V>>, IAddRange<KeyValuePair<K, V>>, ISetOperations<KeyValuePair<K, V>, MapOrMMap<K, V>, MMap<K, V>>
	{
		public MMap() : base() { }
		/// <summary>Creates an empty map with the specified key comparer.</summary>
		public MMap(IEqualityComparer<K> comparer) : base(comparer) { }
		/// <summary>Creates a map with the specified elements.</summary>
		public MMap(IEnumerable<KeyValuePair<K, V>> copy) : base(copy) { }
		/// <summary>Creates a map with the specified elements and key comparer.</summary>
		public MMap(IEnumerable<KeyValuePair<K,V>> copy, IEqualityComparer<K> comparer) : base(copy, comparer) { }
		internal MMap(InternalSet<KeyValuePair<K, V>> set, IEqualityComparer<K> keyComparer, int count) : base(set, keyComparer, count) { }

		#region IDictionary<K,V>

		public void Add(K key, V value)
		{
			Add(new KeyValuePair<K,V>(key,value));
		}
		public new KeyCollection<K, V> Keys
		{
			get { return new KeyCollection<K, V>(this); }
		}
		public bool Remove(K key)
		{
			var kvp = new KeyValuePair<K, V>(key, default(V));
			return GetAndRemove(ref kvp);
		}
		public new ValueCollection<K, V> Values
		{
			get { return new ValueCollection<K, V>(this); }
		}
		public new V this[K key]
		{
			get { return base[key]; }
			set {
				var kvp = new KeyValuePair<K, V>(key, value);
				if (_set.Add(ref kvp, Comparer, true))
					_count++;
			}
		}
		// public V this[K key, V defaultValue] inherited from base class.
		// IReadOnlyDictionary.Keys and IReadOnlyDictionary.Values are also inherited.
		ICollection<K> IDictionary<K, V>.Keys => Keys;
		ICollection<V> IDictionary<K, V>.Values => Values;
		ICollection<K> IDictionaryEx<K, V>.Keys => Keys;
		ICollection<V> IDictionaryEx<K, V>.Values => Values;

		#endregion

		#region ICollection<KeyValuePair<K,V>>

		/// <inheritdoc cref="InternalSet{T}.IsProperSubsetOf(ISet{T}, int)"/>
		public void Add(KeyValuePair<K, V> item)
		{
			if (_set.Add(ref item, Comparer, false)) {
				_count++;
				return;
			}
			throw new ArgumentException("The specified key already exists in the map.");
		}
		public void Clear()
		{
			_set.Clear();
			_count = 0;
		}
		public bool IsReadOnly
		{
			get { return false; }
		}

		/// <summary>Removes a pair from the map.</summary>
		/// <remarks>The removal occurs only if the value provided matches the 
		/// value that is already associated with the key (value comparison is 
		/// performed using object.Equals()).</remarks>
		/// <returns>True if the pair was removed, false if not.</returns>
		public bool Remove(KeyValuePair<K, V> item)
		{
			V value;
			if (TryGetValue(item.Key, out value))
				if (object.Equals(item.Value, value))
					return Remove(item.Key);
			return false;
		}

		#endregion

		#region Additional functionality: Clone, AddRange, AddIfNotPresent, AddOrFind, GetAndRemove, alt. TryGetValue

		/// <summary>Creates a copy of this map in O(1) time, by marking the current
		/// root node as frozen.</summary>
		public virtual MMap<K, V> Clone()
		{
			return new MMap<K, V>(_set.CloneFreeze(), _keyComparer, _count);
		}
		IDictionaryEx<K, V> ICloneable<IDictionaryEx<K, V>>.Clone() => Clone();

		/// <summary>Merges the contents of the specified map into this map.</summary>
		/// <param name="replaceIfPresent">If true, values in the other collection
		/// replace values in this one. If false, the existing pairs in this map
		/// are not overwritten.</param>
		/// <returns>The number of items that were added.</returns>
		public int AddRange(MMap<K, V> data, bool replaceIfPresent = true)
		{
			int added = _set.UnionWith(data._set, Comparer, replaceIfPresent);
			_count += added;
			return added;
		}
		void IAddRange<KeyValuePair<K, V>>.AddRange(IEnumerable<KeyValuePair<K, V>> data) { AddRange(data, true); }
		void IAddRange<KeyValuePair<K, V>>.AddRange(IReadOnlyCollection<KeyValuePair<K, V>> data) { AddRange(data, true); }

		/// <summary>Merges the contents of the specified sequence into this map.</summary>
		/// <param name="replaceIfPresent">If true, values in the other collection
		/// replace values in this one. If false, the existing pairs in this map
		/// are not overwritten.</param>
		/// <returns>The number of new pairs added, whose keys didn't already exist.</returns>
		/// <remarks>Duplicates are allowed in the source data. If 
		/// <c>replaceIfPresent</c> is true, later values take priority over 
		/// earlier values, otherwise earlier values take priority.</remarks>
		public int AddRange(IEnumerable<KeyValuePair<K, V>> data, bool replaceIfPresent = true)
		{
			int added = _set.UnionWith(data, Comparer, replaceIfPresent);
			_count += added;
			return added;
		}

		public int AddRange(IEnumerable<KeyValuePair<K, V>> data, DictEditMode mode)
		{
			if ((mode & DictEditMode.AddIfNotPresent) != 0) {
				int added = _set.UnionWith(data, Comparer, (mode & DictEditMode.ReplaceIfPresent) != 0);
				_count += added;
				return added;
			} else
				return DictionaryExt.AddRange(this, data, mode);
		}

		/// <inheritdoc cref="IDictionaryEx{K,V}.GetAndEdit"/>
		public bool GetAndEdit(ref K key, ref V value, DictEditMode mode)
		{
			if ((mode & DictEditMode.AddIfNotPresent) == 0 && !ContainsKey(key))
				return false;
			var pair = new KeyValuePair<K, V>(key, value);
			bool result = !AddOrFind(ref pair, (mode & DictEditMode.ReplaceIfPresent) != 0);
			key = pair.Key;
			value = pair.Value;
			return result;
		}

		// TODO: make private (must alter tests)
		/// <summary>For internal use. Adds a pair to the map if the key is not 
		/// present, retrieves the existing key-value pair if the key is present, 
		/// and optionally replaces the existing pair with a new pair.</summary>
		/// <param name="pair">When calling this method, pair.Key specifies the
		/// key that you want to search for in the map. If the key is not found
		/// then the pair is added to the map; if the key is found, the pair is
		/// replaced with the existing pair that was found in the map.</param>
		/// <param name="replaceIfPresent">This parameter specifies what to do
		/// if the key is found in the map. If this parameter is true, the 
		/// existing pair is replaced with the specified new pair (in fact the
		/// pair in the map is swapped with the <c>pair</c> parameter). If this
		/// parameter is false, the existing pair is left unmodified and a copy
		/// of it is stored in the <c>pair</c> parameter.</param>
		/// <returns>True if the pair's key did NOT exist and was added, false 
		/// if the key already existed.</returns>
		public bool AddOrFind(ref KeyValuePair<K, V> pair, bool replaceIfPresent)
		{
			if (_set.Add(ref pair, Comparer, replaceIfPresent)) {
				_count++;
				return true;
			}
			return false;
		}

		/// <summary>Gets the value associated with the specified key, then
		/// removes the pair with that key from the dictionary.</summary>
		/// <param name="key">Key to search for.</param>
		/// <returns>The value that was removed. If the key is not found, 
		/// the result has no value (<see cref="Maybe{V}.HasValue"/> is false).</returns>
		/// <remarks>This method shall not throw when the key is null.</remarks>
		public Maybe<V> GetAndRemove(K key)
		{
			var kvp = new KeyValuePair<K, V>(key, default(V));
			if (_set.Remove(ref kvp, Comparer)) {
				_count--;
				return kvp.Value;
			}
			return default(Maybe<V>);
		}

		/// <summary>Gets the pair associated with <c>pair.Key</c>, then
		/// removes the pair with that key from the dictionary.</summary>
		/// <param name="pair">Specifies the key to search for. On return, if the
		/// key was found, this holds both the key and value that used to be in
		/// the dictionary.</param>
		/// <returns>True if a pair was removed, false if not.</returns>
		public bool GetAndRemove(ref KeyValuePair<K, V> pair)
		{
			if (_set.Remove(ref pair, Comparer)) {
				_count--;
				return true;
			}
			return false;
		}

		#endregion

		#region Persistent map operations: With, Without, Union, Except, Intersect, Xor

		/// <inheritdoc cref="Map{K,V}.With"/>
		public MMap<K, V> With(K key, V value, bool replaceIfPresent = true)
		{
			var set = _set.CloneFreeze();
			var item = new KeyValuePair<K, V>(key, value);
			if (set.Add(ref item, Comparer, replaceIfPresent))
				return new MMap<K, V>(set, _keyComparer, _count + 1);
			if (replaceIfPresent)
				return new MMap<K, V>(set, _keyComparer, _count);
			return this;
		}
		public MMap<K, V> With(KeyValuePair<K, V> item) { return With(item.Key, item.Value); }
		/// <inheritdoc cref="Map{K,V}.Without"/>
		public MMap<K, V> Without(K key)
		{
			var set = _set.CloneFreeze();
			var item = new KeyValuePair<K, V>(key, default(V));
			if (set.Remove(ref item, Comparer))
				return new MMap<K, V>(set, _keyComparer, _count - 1);
			return this;
		}
		MMap<K, V> ISetOperations<KeyValuePair<K, V>, MapOrMMap<K, V>, MMap<K, V>>.Without(KeyValuePair<K, V> item)
		{
			V value;
			if (TryGetValue(item.Key, out value))
				if (DefaultValueComparer.Equals(item.Value, value))
					return Without(item.Key);
			return this;
		}
		/// <inheritdoc cref="Map{K,V}.Union"/>
		public MMap<K, V> Union(MapOrMMap<K, V> other) { return Union(other, false); }
		/// <inheritdoc cref="Map{K,V}.Union"/>
		public MMap<K, V> Union(MapOrMMap<K, V> other, bool replaceWithValuesFromOther)
		{
			var set = _set.CloneFreeze();
			int count2 = _count + set.UnionWith(other._set, Comparer, replaceWithValuesFromOther);
			return new MMap<K,V>(set, _keyComparer, count2);
		}
		/// <inheritdoc cref="Map{K,V}.Intersect"/>
		public MMap<K, V> Intersect(MapOrMMap<K, V> other)
		{
			var set = _set.CloneFreeze();
			int count2 = _count - set.IntersectWith(other._set, other.Comparer);
			return new MMap<K, V>(set, _keyComparer, count2);
		}
		/// <inheritdoc cref="Map{K,V}.Except"/>
		public MMap<K, V> Except(MapOrMMap<K, V> other)
		{
			var set = _set.CloneFreeze();
			int count2 = _count - set.ExceptWith(other._set, Comparer);
			return new MMap<K, V>(set, _keyComparer, count2);
		}
		/// <inheritdoc cref="Map{K,V}.Xor"/>
		public MMap<K, V> Xor(MapOrMMap<K, V> other)
		{
			var set = _set.CloneFreeze();
			int count2 = _count + set.SymmetricExceptWith(other._set, Comparer);
			return new MMap<K, V>(set, _keyComparer, count2);
		}

		#endregion

		public Map<K, V> AsImmutable() { return (Map<K, V>)this; }
		public static explicit operator Map<K, V>(MMap<K, V> copy) 
		{
			var map = new Map<K, V>(copy._set, copy._keyComparer, copy._count);
			Debug.Assert(copy._set.IsRootFrozen);
			return map;
		}
	}
}
