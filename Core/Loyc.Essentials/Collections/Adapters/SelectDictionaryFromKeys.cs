using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Collections
{
	public static partial class EnumerableExt
	{
		/// <summary>Converts a collection of keys to an IReadOnlyDictionary,
		/// based on a function that can obtain a value for a given key.</summary>
		/// <param name="keys">A collection of dictionary keys.</param>
		/// <param name="tryGetValue">This function is used both to test membership and to get values.</param>
		/// <param name="getValue">This function is optional. It is used to get values when it is known 
		/// in advance that the key exists (in GetEnumerator() and in the Values property). If this is 
		/// null, tryGetValue is used instead. Providing this function can increase performance.</param>
		/// <example>
		/// This function is useful, for example, when you need to implement an interface that 
		/// provides a dictionary of values, but your data is in the wrong format. You don't
		/// want to convert the entire dictionary, since the caller might only need to look
		/// up one item from it.
		/// <code>
		/// interface ICompany
		/// {
		/// 	IReadOnlyDictionary&lt;long, string> Employees { get; }
		/// 	...
		/// }
		/// class Company : ICompany
		/// {
		/// 	Dictionary&lt;int, Person> _employees = new Dictionary&lt;int, Person>();
		/// 
		///		public IReadOnlyDictionary&lt;long, string> Employees => 
		///			LinqToLists.Select(_employees.Keys, k => (long)k)
		///			.AsReadOnlyDictionary(k => {
		///			    var v = _employees.TryGetValue((int)k);
		///			    return v.HasValue ? (Maybe&lt;string>)v.Value.ToString() : Maybe&lt;string>.NoValue;
		///			});		
		/// 	...
		/// }
		/// class Person
		/// {
		/// 	string FirstName, LastName;
		/// 	public override string ToString() => FirstName + " " + LastName;
		/// }
		/// </code>
		/// </example>
		public static IReadOnlyDictionary<K, V> AsReadOnlyDictionary<K, V>(this IReadOnlyCollection<K> keys, Func<K, Maybe<V>> tryGetValue, Func<K, V> getValue = null)
		{
			return new SelectDictionaryFromKeys<K, V>(keys, tryGetValue, getValue);
		}
	}

	/// <summary>An adapter that converts a collection of keys to an IReadOnlyDictionary.
	/// Used by <see cref="AsReadOnlyDictionary"/>based on a function that can obtain a value for a given key.</summary>
	/// <typeparam name="K">Key type</typeparam>
	/// <typeparam name="V">Value type</typeparam>
	public class SelectDictionaryFromKeys<K, V> : IReadOnlyDictionary<K, V>
	{
		IReadOnlyCollection<K> _keys;
		Func<K, Maybe<V>> _tryGetValue;
		Func<K, V> _getValue;

		/// <summary>Initializes the adapter.</summary>
		/// <param name="keys">A collection of dictionary keys.</param>
		/// <param name="tryGetValue">This function is used both to test membership and to get values.</param>
		/// <param name="getValue">This function is optional. It is used to get values when it is known 
		/// in advance that the key exists (in GetEnumerator() and in the Values property). If this is 
		/// null, tryGetValue is used instead. Providing this function can increase performance.</param>
		public SelectDictionaryFromKeys(IReadOnlyCollection<K> keys, Func<K, Maybe<V>> tryGetValue, Func<K, V> getValue = null)
		{
			if (keys == null)        throw new ArgumentNullException("keys");
			if (tryGetValue == null) throw new ArgumentNullException("tryGetValue");
			_keys = keys;
			_tryGetValue = tryGetValue;
			_getValue = getValue;
		}

		public V this[K key] {
			get {
				var v = _tryGetValue(key);
				if (v.HasValue)
					return v.Value;
				throw new KeyNotFoundException();
			}
		}

		public IEnumerable<K> Keys => _keys;

		public IEnumerable<V> Values => _getValue != null 
			? _keys.Select(k => _getValue(k))
			: _keys.Select(k => _tryGetValue(k).Or(default(V)));

		public int Count => _keys.Count;

		public bool ContainsKey(K key) => _tryGetValue(key).HasValue;

		public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
		{
			return (_getValue != null
				? _keys.Select(k => new KeyValuePair<K, V>(k, _getValue(k)))
				: _keys.Select(k => new KeyValuePair<K, V>(k, _tryGetValue(k).Value)))
				.GetEnumerator();
		}

		public bool TryGetValue(K key, out V value)
		{
			var result = _tryGetValue(key);
			value = result.Or(default(V));
			return result.HasValue;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
