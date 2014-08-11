using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>Extension methods for <see cref="Dictionary{K,V}"/> and <see cref="IDictionary{K,V}"/>.</summary>
	public static class DictionaryExt
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
	}

}
