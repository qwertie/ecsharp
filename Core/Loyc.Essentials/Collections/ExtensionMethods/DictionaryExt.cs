using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Loyc.Collections
{
	/// <summary>Extension methods for <see cref="Dictionary{K,V}"/>, 
	/// <see cref="IDictionary{K,V}"/> and <see cref="IDictionaryEx{K, V}"/>.</summary>
	public static partial class DictionaryExt
	{
		// They want me to put a "where K: notnull" constraint on these. I disagree. The warning says:
		// type 'K' cannot be used as...'TKey' in...'IDictionary<TKey, TValue>'. Nullability of...'K' doesn't match 'notnull' constraint.
		#pragma warning disable 8714 

		/// <summary>Adds a key/value pair to the dictionary if the key is not already present,
		/// and returns the existing or new value.</summary>
		/// <returns>The existing value (if the key already existed) or the new value.</returns>
		/// <remarks>This is not thread-safe. Only one thread should access the dictionary at once.</remarks>
		public static V GetOrAdd<K, V>(this IDictionary<K, V> dict, K key, V value)
		{
			if (dict.TryGetValue(key, out V? existing))
				return existing;
			dict.Add(key, value);
			return value;
		}
		/// <summary>Adds a key/value pair to the dictionary if the key is not already present, by 
		/// using the specified function to obtain a value, and returns the existing or new value.</summary>
		/// <returns>The existing value (if the key already existed) or the new value.</returns>
		/// <remarks>This is not thread-safe. Only one thread should access the dictionary at once.</remarks>
		public static V GetOrAdd<K, V>(this IDictionary<K, V> dict, K key, Func<K, V> valueFactory)
		{
			if (dict.TryGetValue(key, out V? value))
				return value;
			value = valueFactory(key);
			dict.Add(key, value);
			return value;
		}

		/// <inheritdoc cref="GetOrAdd{K,V}(IDictionary{K,V},K,V)"/>
		/// <remarks>This overload is preferred over the <see cref="IDictionary{K,V}"/>
		/// version because on .NET 6+ it needs only a single hash lookup.</remarks>
		public static V GetOrAdd<K, V>(this Dictionary<K, V> dict, K key, V value)
		{
			#if NET6_0_OR_GREATER
			ref V? slot = ref System.Runtime.InteropServices.CollectionsMarshal
				.GetValueRefOrAddDefault(dict, key, out bool exists);
			if (exists)
				return slot!;
			slot = value;
			return value;
			#else
			return GetOrAdd((IDictionary<K, V>)dict, key, value);
			#endif
		}
		/// <inheritdoc cref="GetOrAdd{K,V}(IDictionary{K,V},K,Func{K,V})"/>
		/// <remarks>This overload is preferred over the <see cref="IDictionary{K,V}"/>
		/// version because on .NET 6+ it needs only a single hash lookup.</remarks>
		public static V GetOrAdd<K, V>(this Dictionary<K, V> dict, K key, Func<K, V> valueFactory)
		{
			#if NET6_0_OR_GREATER
			ref V? slot = ref System.Runtime.InteropServices.CollectionsMarshal
				.GetValueRefOrAddDefault(dict, key, out bool exists);
			if (exists)
				return slot!;
			try {
				V value = valueFactory(key);
				slot = value;
				return value;
			} catch {
				dict.Remove(key); // don't leave a default(V) behind if the factory throws
				throw;
			}
			#else
			return GetOrAdd((IDictionary<K, V>)dict, key, valueFactory);
			#endif
		}

		/// <summary>Uses the specified functions either to add a key/value pair to the dictionary
		/// if the key does not already exist, or to update a key/value pair in the dictionary if 
		/// the key already exists.</summary>
		/// <returns>The new value associated with the key, which is either the result of addValueFactory or 
		/// updateValueFactory.</returns>
		/// <remarks>This is not thread-safe. Only one thread should access the dictionary at once.</remarks>
		public static V AddOrUpdate<K, V>(this IDictionary<K, V> dict, K key, Func<K, V> addValueFactory, Func<K, V, V> updateValueFactory)
		{
			if (dict.TryGetValue(key, out V? value))
				dict[key] = value = updateValueFactory(key, value);
			else
				dict.Add(key, value = addValueFactory(key));
			return value;
		}
		/// <summary>Adds a key/value pair to the dictionary if the key does not already exist, or, 
		/// if it does, updates a key/value pair in the dictionary using the specified function.</summary>
		/// <returns>The new value associated with the key, which is either addValue or the result 
		/// of updateValueFactory.</returns>
		/// <remarks>This is not thread-safe. Only one thread should access the dictionary at once.</remarks>
		public static V AddOrUpdate<K, V>(this IDictionary<K, V> dict, K key, V addValue, Func<K, V, V> updateValueFactory)
		{
			if (dict.TryGetValue(key, out V? value))
				dict[key] = value = updateValueFactory(key, value);
			else
				dict.Add(key, value = addValue);
			return value;
		}

		/// <inheritdoc cref="AddOrUpdate{K,V}(IDictionary{K,V},K,Func{K,V},Func{K,V,V})"/>
		/// <remarks>This overload is preferred over the <see cref="IDictionary{K,V}"/>
		/// version because on .NET 6+ it needs only a single hash lookup. The factory
		/// functions must not modify the dictionary.</remarks>
		public static V AddOrUpdate<K, V>(this Dictionary<K, V> dict, K key, Func<K, V> addValueFactory, Func<K, V, V> updateValueFactory)
		{
			#if NET6_0_OR_GREATER
			ref V? slot = ref System.Runtime.InteropServices.CollectionsMarshal
				.GetValueRefOrAddDefault(dict, key, out bool exists);
			if (exists) {
				V updated = updateValueFactory(key, slot!);
				slot = updated;
				return updated;
			}
			try {
				V added = addValueFactory(key);
				slot = added;
				return added;
			} catch {
				dict.Remove(key); // don't leave a default(V) behind if the factory throws
				throw;
			}
			#else
			return AddOrUpdate((IDictionary<K, V>)dict, key, addValueFactory, updateValueFactory);
			#endif
		}
		/// <inheritdoc cref="AddOrUpdate{K,V}(IDictionary{K,V},K,V,Func{K,V,V})"/>
		/// <remarks>This overload is preferred over the <see cref="IDictionary{K,V}"/>
		/// version because on .NET 6+ it needs only a single hash lookup. The update
		/// function must not modify the dictionary.</remarks>
		public static V AddOrUpdate<K, V>(this Dictionary<K, V> dict, K key, V addValue, Func<K, V, V> updateValueFactory)
		{
			#if NET6_0_OR_GREATER
			ref V? slot = ref System.Runtime.InteropServices.CollectionsMarshal
				.GetValueRefOrAddDefault(dict, key, out bool exists);
			V value = exists ? updateValueFactory(key, slot!) : addValue;
			slot = value;
			return value;
			#else
			return AddOrUpdate((IDictionary<K, V>)dict, key, addValue, updateValueFactory);
			#endif
		}

		/// <summary>An alternate version TryGetValue that returns a default value 
		/// if the key was not found in the dictionary, and that does not throw if 
		/// the key is null.</summary>
		/// <returns>The value associated with the specified key, or defaultValue 
		/// if no value is associated with the key.</returns>
		public static V TryGetValue<K, V>(this Dictionary<K, V> dict, K key, V defaultValue)
		{
			V? value;
			if (key == null || !dict.TryGetValue(key, out value))
				return defaultValue;
			return value;
		}
		/// <inheritdoc cref="TryGetValue{K,V}(Dictionary{K,V},K,V)"/>
		public static V TryGetValue<K, V>(this IDictionary<K, V> dict, K key, V defaultValue)
		{
			V? value;
			if (key == null || !dict.TryGetValue(key, out value))
				return defaultValue;
			return value;
		}
		/// <inheritdoc cref="TryGetValue{K,V}(Dictionary{K,V},K,V)"/>
		public static V TryGetValue<K, V>(this IReadOnlyDictionary<K, V> dict, K key, V defaultValue)
		{
			V? value;
			if (key == null || !dict.TryGetValue(key, out value))
				return defaultValue;
			return value;
		}
		/// <summary>Disambiguating overload.</summary>
		public static V TryGetValue<K, V>(this IDictionaryAndReadOnly<K, V> dict, K key, V defaultValue)
			=> TryGetValue((IReadOnlyDictionary<K,V>)dict, key, defaultValue);

		/// <summary>Same as IDictionary.TryGetValue() except that this method does 
		/// not throw an exception when <c>key==null</c> (it simply returns NoValue),
		/// and it returns the result as <see cref="Maybe{V}"/> instead of storing
		/// the result in an "out" parameter.</summary>
		public static Maybe<V> TryGetValue<K, V>(this IDictionary<K, V> dict, K key)
		{
			V? value;
			if (key == null || !dict.TryGetValue(key, out value))
				return Maybe<V>.NoValue;
			return value;
		}
		/// <inheritdoc cref="TryGetValue{K,V}(IDictionary{K,V},K)"/>
		public static Maybe<V> TryGetValue<K, V>(this IReadOnlyDictionary<K, V> dict, K key)
		{
			V? value;
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
		public static bool TryGetValueSafe<K, V>(this IDictionary<K, V> dict, K key, [MaybeNullWhen(false)] out V value)
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

		/// <summary>Default implementation of <see cref="IDictionaryEx{K, V}.AddRange"/>.
		/// Merges the contents of the specified sequence into this map.</summary>
		public static int AddRange<K, V>(this IDictionary<K, V> dict, IEnumerable<KeyValuePair<K, V>> data, DictEditMode mode)
		{
			var e = data.GetEnumerator();
			int numMissing = 0;
			foreach (var pair in data)
			{
				K key = pair.Key;
				V? val = pair.Value;
				if (!LCInterfaces.GetAndEdit(dict, key, ref val, mode))
					numMissing++;
			}
			return numMissing;
		}

		/// <summary>Default implementation of <see cref="IDictionaryEx{K, V}.GetAndRemove"/>.
		/// Gets the value associated with the specified key, then removes the 
		/// pair with that key from the dictionary.</summary>
		public static Maybe<V> GetAndRemove<K, V>(this IDictionary<K, V> dict, K key)
		{
			if (dict.TryGetValue(key, out V? value)) {
				dict.Remove(key);
				return value;
			}
			return default(Maybe<V>);
		}

		/// <inheritdoc cref="GetAndRemove{K,V}(IDictionary{K,V},K)"/>
		/// <remarks>This overload is preferred over the <see cref="IDictionary{K,V}"/>
		/// version because it needs only a single hash lookup on runtimes that offer
		/// <c>Dictionary.Remove(key, out value)</c>.</remarks>
		public static Maybe<V> GetAndRemove<K, V>(this Dictionary<K, V> dict, K key)
		{
			#if !(NETSTANDARD2_0 || NETFRAMEWORK)
			if (dict.Remove(key, out V? value))
				return value;
			return default(Maybe<V>);
			#else
			return GetAndRemove((IDictionary<K, V>)dict, key);
			#endif
		}
	}

}
