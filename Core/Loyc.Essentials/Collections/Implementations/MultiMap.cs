using Loyc.Collections.Impl;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Loyc.Collections.MutableListExtensionMethods;

namespace Loyc.Collections
{
	/// <summary>
	/// A multimap is a dictionary that allows more than one value to be associated 
	/// with each key. This is much more convenient than a Dictionary of Lists of 
	/// values, because the indexer always returns a collection (empty if there are
	/// no values associated with a key) and its methods automatically create or
	/// destroy, as appropriate, the list associated with each key.
	/// </summary><remarks>
	/// This class is implemented using a Dictionary whose entries have lists of values.
	/// <para/>
	/// This class keeps track of the number of values as well as the number of 
	/// keys. The <see cref="Count"/> property returns the number of values, while
	/// <see cref="KeyCount"/> returns the number of keys. The number of values is
	/// never less than the number of keys, because the collection associated with
	/// a key is always removed when its last item is removed.
	/// </remarks>
	public class MultiMap<K, V> : ICollectionImpl<KeyValuePair<K, V>>, IReadOnlyDictionary<K, MultiMap<K, V>.ValueList>, IIndexed<K, MultiMap<K, V>.ValueList>, ICloneable<MultiMap<K, V>>
		where K : notnull
	{
		Dictionary<K, List<V>> _dict = new Dictionary<K, List<V>>();
		int _valueCount;

		public MultiMap(IEnumerable<KeyValuePair<K, V>> pairs, IEqualityComparer<K>? comp = null) : this(comp) { this.AddRange(pairs); }
		public MultiMap(Dictionary<K, List<V>> dict) => CopyFrom(dict ?? throw new ArgumentNullException(nameof(dict)));
		public MultiMap(MultiMap<K, V> map) => CopyFrom(map._dict);
		public MultiMap(IEqualityComparer<K>? comp) => _dict = new Dictionary<K, List<V>>(comp ?? EqualityComparer<K>.Default);
		public MultiMap() => _dict = new Dictionary<K, List<V>>();
		
		void CopyFrom(Dictionary<K, List<V>> dict)
		{
			_dict = new Dictionary<K, List<V>>(dict.Count, dict.Comparer);
			_valueCount = 0;
			foreach (var pair in dict) {
				var list = new List<V>(dict[pair.Key]);
				_valueCount += list.Count;
				if (list.Count != 0)
					_dict[pair.Key] = list;
			}
		}

		public struct ValueList : ICollection<V>
		{
			MultiMap<K, V> _map;
			K _key;

			public ValueList(MultiMap<K, V> map, K key) { _map = map; _key = key; }

			List<V>? GetValues()
			{
				_map._dict.TryGetValue(_key, out var values);
				return values;
			}
			List<V> GetOrMakeValues()
			{
				var values = GetValues();
				if (values == null)
					_map._dict.Add(_key, values = new List<V>());
				return values;
			}

			public int Count => GetValues()?.Count ?? 0;

			public bool IsReadOnly => false;

			public void Add(V item)
			{
				GetOrMakeValues().Add(item);
				_map._valueCount++;
			}

			public void Clear() => _map._dict.Remove(_key);

			public bool Contains(V item) => G.Var(out var values, GetValues()) != null && values!.Contains(item);

			public void CopyTo(V[] array, int arrayIndex)
			{
				if (G.Var(out var values, GetValues()) != null)
					values!.CopyTo(array, arrayIndex);
			}

			IEnumerator<V> IEnumerable<V>.GetEnumerator() => GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
			public List<V>.Enumerator GetEnumerator()
			{
				var values = GetValues();
				return values != null ? values.GetEnumerator() : EmptyEnumerator<V>.Value;
			}

			public bool Remove(V item)
			{
				if (_map._dict.TryGetValue(_key, out var values) && values.Remove(item)) {
					_map._valueCount--;
					if (values.Count == 0)
						_map._dict.Remove(_key);
					return true;
				}
				return false;
			}
		}

		public MultiMap<K, V>.ValueList this[K key]
		{
			get => new ValueList(this, key);
		}

		public IEnumerable<K> Keys => _dict.Keys;
		public IEnumerable<V> Values => _dict.SelectMany(p => p.Value);
		IEnumerable<ValueList> IReadOnlyDictionary<K, ValueList>.Values => _dict.Select(p => new ValueList(this, p.Key));

		/// <summary>Gets the number of keys in the underlying dictionary.</summary>
		public int KeyCount => _dict.Count;
		/// <summary>Gets the number of values in all value collections.</summary>
		public int Count => _valueCount;
		int IReadOnlyCollection<KeyValuePair<K, ValueList>>.Count => KeyCount;

		bool ICollection<KeyValuePair<K, V>>.IsReadOnly => false;

		public bool ContainsKey(K key) => _dict.ContainsKey(key);

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		IEnumerator<KeyValuePair<K, ValueList>> IEnumerable<KeyValuePair<K, ValueList>>.GetEnumerator() 
			=> _dict.Keys.Select(k => new KeyValuePair<K, ValueList>(k, new ValueList(this, k))).GetEnumerator();
		public IEnumerator<KeyValuePair<K, V>> GetEnumerator() 
			=> _dict.SelectMany(p => p.Value, (p, v) => new KeyValuePair<K, V>(p.Key, v)).GetEnumerator();

		/// <summary>The collection returned is always valid, but the method 
		/// returns true only if the collection is not empty.</summary>
		public bool TryGetValue(K key, [MaybeNullWhen(false)] out ValueList value)
		{
			value = new ValueList(this, key);
			return ContainsKey(key);
		}

		public void Add(KeyValuePair<K, V> item) => new ValueList(this, item.Key).Add(item.Value);

		public void Clear()
		{
			_dict.Clear();
			_valueCount = 0;
		}

		public bool Contains(KeyValuePair<K, V> item) => new ValueList(this, item.Key).Contains(item.Value);

		public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
			=> ListExt.CopyTo<KeyValuePair<K, V>>(this, array, arrayIndex);

		public bool Remove(KeyValuePair<K, V> item) => new ValueList(this, item.Key).Remove(item.Value);
		public bool Remove(K key)
		{
			_valueCount -= new ValueList(this, key).Count;
			return _dict.Remove(key);
		}

		public MultiMap<K, V> Clone() => new MultiMap<K, V>(this);
	}
}
