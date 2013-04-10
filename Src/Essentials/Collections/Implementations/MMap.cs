using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections.Impl;

namespace Loyc.Collections
{
	public class MMap<K,V> : IDictionary<K,V>, ICollection<KeyValuePair<K,V>>, ICloneable<MMap<K,V>>, IAddRange<KeyValuePair<K,V>>, IEqualityComparer<KeyValuePair<K, V>>
	{
		internal InternalSet<KeyValuePair<K, V>> _set;
		private IEqualityComparer<K> _keyComparer;
		internal IEqualityComparer<KeyValuePair<K, V>> Comparer { get { return this; } }
		private int _count;

		public MMap() : this(InternalSet<K>.DefaultComparer) { }
		public MMap(IEqualityComparer<K> comparer) { _keyComparer = comparer; }
		public MMap(IEnumerable<KeyValuePair<K, V>> copy) : this(copy, InternalSet<K>.DefaultComparer) { }
		public MMap(IEnumerable<KeyValuePair<K,V>> copy, IEqualityComparer<K> comparer) { _keyComparer = comparer; AddRange(copy); }
		public MMap(MMap<K, V> clone) : this(clone._set, clone._keyComparer, clone._count) { }
		internal MMap(InternalSet<KeyValuePair<K, V>> set, IEqualityComparer<K> keyComparer, int count)
		{
			_set = set;
			_keyComparer = keyComparer;
			_count = count;
			_set.CloneFreeze();
		}

		public IEqualityComparer<K> KeyComparer { get { return _keyComparer; } }

		#region Key comparison interface (with explanation)
		// The user can provide a IEqualityComparer<K> to compare keys. However, 
		// InternalSet<KeyValuePair<K, V>> requires a comparer that can compare
		// KeyValuePair<K, V> values (and normally the comparer will only compare
		// the Key part of the pair). To provide this without an extra memory
		// allocation, MMap itself implements IEqualityComparer<KeyValuePair<K, V>>.
		// End-users should ignore this interface.

		bool IEqualityComparer<KeyValuePair<K, V>>.Equals(KeyValuePair<K,V> x, KeyValuePair<K,V> y)
		{
 			return _keyComparer.Equals(x.Key, y.Key);
		}
		int IEqualityComparer<KeyValuePair<K, V>>.GetHashCode(KeyValuePair<K,V> obj)
		{
 			return _keyComparer.GetHashCode(obj.Key);
		}

		#endregion
		
		#region IDictionary<K,V>

		public void Add(K key, V value)
		{
			Add(new KeyValuePair<K,V>(key,value));
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
			var kvp = new KeyValuePair<K, V>(key, default(V));
			return GetAndRemove(ref kvp);
		}
		public bool TryGetValue(K key, out V value)
		{
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
				var kvp = new KeyValuePair<K, V>(key, default(V));
				if (_set.Find(ref kvp, Comparer))
					return kvp.Value;
				throw new KeyNotFoundException();
			}
			set {
				var kvp = new KeyValuePair<K, V>(key, value);
				if (_set.Add(ref kvp, Comparer, true))
					_count++;
			}
		}

		#endregion

		#region ICollection<KeyValuePair<K,V>>

		public void Add(KeyValuePair<K, V> item)
		{
			if (_set.Add(ref item, Comparer, false))
				_count++;
			throw new ArgumentException("The specified key already exists in the map.");
		}
		public void Clear()
		{
			_set.Clear();
			_count = 0;
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

		public InternalSet<KeyValuePair<K, V>>.Enumerator GetEnumerator()
		{
			return _set.GetEnumerator();
		}
		IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator() { return GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

		#endregion

		#region Additional functionality: Clone, AddRange, AddIfNotPresent, AddOrFind, GetAndRemove, alt. TryGetValue

		public virtual MMap<K, V> Clone()
		{
			return new MMap<K, V>(_set, _keyComparer, _count);
		}

		public int AddRange(MMap<K, V> data, bool replaceIfPresent = true)
		{
			int added = _set.UnionWith(data._set, Comparer, replaceIfPresent);
			_count += added;
			return added;
		}
		void IAddRange<KeyValuePair<K, V>>.AddRange(IEnumerable<KeyValuePair<K, V>> data) { AddRange(data, true); }
		void IAddRange<KeyValuePair<K, V>>.AddRange(IListSource<KeyValuePair<K, V>> data) { AddRange(data, true); }
		public int AddRange(IEnumerable<KeyValuePair<K, V>> data, bool replaceIfPresent = true)
		{
			int added = _set.UnionWith(data, Comparer, replaceIfPresent);
			_count += added;
			return added;
		}

		public bool AddIfNotPresent(K key, V value)
		{
			var kvp = new KeyValuePair<K, V>(key, value);
			return AddOrFind(ref kvp, false);
		}
		public bool AddOrFind(ref KeyValuePair<K, V> pair, bool replaceIfPresent)
		{
			if (_set.Add(ref pair, Comparer, replaceIfPresent)) {
				_count++;
				return true;
			}
			return false;
		}
		public bool GetAndRemove(K key, ref V valueRemoved)
		{
			var kvp = new KeyValuePair<K, V>(key, default(V));
			if (_set.Remove(ref kvp, Comparer)) {
				_count--;
				valueRemoved = kvp.Value;
				return true;
			}
			return false;
		}
		public bool GetAndRemove(ref KeyValuePair<K, V> pair)
		{
			if (_set.Remove(ref pair, Comparer)) {
				_count--;
				return true;
			}
			return false;
		}
		public V TryGetValue(K key, V defaultValue)
		{
			var kvp = new KeyValuePair<K, V>(key, defaultValue);
			_set.Find(ref kvp, Comparer);
			return kvp.Value;
		}
		
		#endregion

		#region Persistent map operations: With, Without, Union, Except, Intersect, Xor

		/// <summary>Returns a copy of the current map with an additional key-value pair.</summary>
		/// <paparam name="replaceIfPresent">If true, the existing key-value pair is replaced if present. 
		/// Otherwise, the existing key-value pair is left unchanged.</paparam>
		/// <returns>A map with the specified key. If the key was already present 
		/// and replaceIfPresent is false, the same set ('this') is returned.</remarks>
		MMap<K, V> With(K key, V value, bool replaceIfPresent = true)
		{
			var set = _set.CloneFreeze();
			var item = new KeyValuePair<K, V>(key, value);
			if (set.Add(ref item, Comparer, replaceIfPresent))
				return new MMap<K, V>(set, _keyComparer, _count + 1);
			if (replaceIfPresent)
				return new MMap<K, V>(set, _keyComparer, _count);
			return this;
		}
		/// <summary>Returns a copy of the current map without the specified key.</summary>
		/// <returns>A map without the specified key. If the key was not present,
		/// the same set ('this') is returned.</remarks>
		MMap<K, V> Without(K key)
		{
			var set = _set.CloneFreeze();
			var item = new KeyValuePair<K, V>(key, default(V));
			if (set.Remove(ref item, Comparer))
				return new MMap<K, V>(set, _keyComparer, _count - 1);
			return this;
		}
		public MMap<K,V> Union(Map<K,V> other, bool replaceWithValuesFromOther = false) { return Union(other._set, replaceWithValuesFromOther); }
		public MMap<K,V> Union(MMap<K,V> other, bool replaceWithValuesFromOther = false) { return Union(other._set, replaceWithValuesFromOther); }
		internal MMap<K,V> Union(InternalSet<KeyValuePair<K,V>> other, bool replaceWithValuesFromOther = false)
		{
			var set = _set.CloneFreeze();
			int count2 = _count + set.UnionWith(other, Comparer, replaceWithValuesFromOther);
			return new MMap<K,V>(set, _keyComparer, count2);
		}
		public MMap<K,V> Intersect(Map<K,V> other) { return Intersect(other._set, other.Comparer); }
		public MMap<K,V> Intersect(MMap<K,V> other) { return Intersect(other._set, other.Comparer); }
		internal MMap<K,V> Intersect(InternalSet<KeyValuePair<K,V>> other, IEqualityComparer<KeyValuePair<K,V>> otherComparer)
		{
			var set = _set.CloneFreeze();
			int count2 = _count - set.IntersectWith(other, otherComparer);
			return new MMap<K, V>(set, _keyComparer, count2);
		}
		public MMap<K,V> Except(Map<K,V> other) { return Except(other._set); }
		public MMap<K,V> Except(MMap<K,V> other) { return Except(other._set); }
		internal MMap<K,V> Except(InternalSet<KeyValuePair<K,V>> other)
		{
			var set = _set.CloneFreeze();
			int count2 = _count - set.ExceptWith(other, Comparer);
			return new MMap<K, V>(set, _keyComparer, count2);
		}
		public MMap<K,V> Xor(Map<K,V> other) { return Xor(other._set); }
		public MMap<K,V> Xor(MMap<K,V> other) { return Xor(other._set); }
		internal MMap<K,V> Xor(InternalSet<KeyValuePair<K,V>> other)
		{
			var set = _set.CloneFreeze();
			int count2 = _count + set.SymmetricExceptWith(other, Comparer);
			return new MMap<K, V>(set, _keyComparer, count2);
		}

		#endregion
	}
}
