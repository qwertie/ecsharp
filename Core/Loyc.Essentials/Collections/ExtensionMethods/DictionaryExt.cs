using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>Extension methods for <see cref="Dictionary{K,V}"/>, 
	/// <see cref="IDictionary{K,V}"/> and <see cref="IDictionaryEx{K, V}"/>.</summary>
	public static partial class DictionaryExt
	{
		/// <summary>An alternate version TryGetValue that returns a default value 
		/// if the key was not found in the dictionary, and that does not throw if 
		/// the key is null.</summary>
		/// <returns>The value associated with the specified key, or defaultValue 
		/// if no value is associated with the key.</returns>
		public static V TryGetValue<K, V>(this Dictionary<K, V> dict, K key, V defaultValue)
		{
			V value;
			if (key == null || !dict.TryGetValue(key, out value))
				return defaultValue;
			return value;
		}
		/// <inheritdoc cref="TryGetValue{K,V}(Dictionary{K,V},K,V)"/>
		public static V TryGetValue<K, V>(this IDictionary<K, V> dict, K key, V defaultValue)
		{
			V value;
			if (key == null || !dict.TryGetValue(key, out value))
				return defaultValue;
			return value;
		}
		/// <inheritdoc cref="TryGetValue{K,V}(Dictionary{K,V},K,V)"/>
		public static V TryGetValue<K, V>(this IReadOnlyDictionary<K, V> dict, K key, V defaultValue)
		{
			V value;
			if (key == null || !dict.TryGetValue(key, out value))
				return defaultValue;
			return value;
		}

		/// <summary>Same as IDictionary.TryGetValue() except that this method does 
		/// not throw an exception when <c>key==null</c> (it simply returns NoValue),
		/// and it returns the result as <see cref="Maybe{V}"/> instead of storing
		/// the result in an "out" parameter.</summary>
		public static Maybe<V> TryGetValue<K, V>(this IDictionary<K, V> dict, K key)
		{
			V value;
			if (key == null || !dict.TryGetValue(key, out value))
				return Maybe<V>.NoValue;
			return value;
		}
		/// <inheritdoc cref="TryGetValue{K,V}(IDictionary{K,V},K)"/>
		public static Maybe<V> TryGetValue<K, V>(this IReadOnlyDictionary<K, V> dict, K key)
		{
			V value;
			if (key == null || !dict.TryGetValue(key, out value))
				return Maybe<V>.NoValue;
			return value;
		}
		
		// See issue #84
		/// <inheritdoc cref="TryGetValue{K,V}(Dictionary{K,V},K,V)"/>
		public static Maybe<V> TryGetValue<K, V>(this IDictionaryAndReadOnly<K, V> dict, K key)
		{
			return TryGetValue((IReadOnlyDictionary<K, V>)dict, key);
		}
		/// <inheritdoc cref="TryGetValue{K,V}(Dictionary{K,V},K,V)"/>
		public static Maybe<V> TryGetValue<K, V>(this Dictionary<K, V> dict, K key)
		{
			return TryGetValue((IReadOnlyDictionary<K, V>)dict, key);
		}

		/// <summary>Same as IDictionary.TryGetValue() except that this method does 
		/// not throw an exception when <c>key==null</c> (it simply returns false).</summary>
		public static bool TryGetValueSafe<K, V>(this IDictionary<K, V> dict, K key, out V value)
		{
			if (key != null)
				return dict.TryGetValue(key, out value);
			else {
				value = default(V);
				return false;
			}
		}

		/// <summary>Adds data to a dictionary (<c>dict.Add(key, value)</c> for all pairs in a sequence.)</summary>
		public static int AddRange<K, V>(this IDictionary<K, V> dict, IEnumerable<KeyValuePair<K, V>> list)
		{
			int count = 0;
			foreach (var item in list) {
				dict.Add(item.Key, item.Value);
				count++;
			}
			return count;
		}
		/// <summary>Adds data to a dictionary (<c>dict[key] = value</c> for all pairs in a sequence.)</summary>
		public static void SetRange<K, V>(this IDictionary<K, V> dict, IEnumerable<KeyValuePair<K, V>> list)
		{
			foreach (var item in list) {
				dict[item.Key] = item.Value;
			}
		}
		/// <summary>Tries to remove a set of key-values from a dictionary based on their keys.</summary>
		/// <returns>The number of keys that were found and removed.</returns>
		public static int RemoveRange<K,V>(this IDictionary<K, V> dict, IEnumerable<K> list)
		{
			int removed = 0;
			foreach (var key in list)
				if (dict.Remove(key))
					removed++;
			return removed;
		}
	}
}
