using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>
	/// A dictionary whose keys are Type objects. This dictionary behaves almost 
	/// identically to a standard dictionary, except that the TryGetValue method 
	/// will find matches on base types and interfaces, e.g. if you add a dictionary 
	/// entry for <c>IEnumerable</c>, then <c>TryGetValue(typeof(BitArray), out _)</c> 
	/// will return true.
	/// </summary>
	/// <remarks>
	/// This class is not thread-safe.
	/// <para/>
	/// This class is keeps a cache of types you've searched for with TryGetValue
	/// so it can respond to repeated queries for the same type more quickly. For
	/// example, if you add a dictionary entry for <c>IEnumerable</c> and search 
	/// for typeof(BitArray), a new cache entry is added for BitArray that
	/// holds the value associated with IEnumerable.
	/// <para/>
	/// Whenever you modify the dictionary, the cache is cleared.
	/// </remarks>
	public class TypeDictionaryWithBaseTypeLookups<Value> : IDictionaryAndReadOnly<Type, Value>, IAdd<KeyValuePair<Type, Value>>
	{
		Dictionary<Type, Value> _dict;
		Dictionary<RuntimeTypeHandle, Maybe<Value>> _cache = null;

		public TypeDictionaryWithBaseTypeLookups() => _dict = new Dictionary<Type, Value>();
		public TypeDictionaryWithBaseTypeLookups(IDictionary<Type, Value> copy) => _dict = new Dictionary<Type, Value>(copy);

		public int Count => _dict.Count;
		public bool IsReadOnly => false;
		public bool ContainsKey(Type key) => _dict.ContainsKey(key);
		public IEnumerable<Type> Keys => _dict.Keys;
		public IEnumerable<Value> Values => _dict.Values;
		ICollection<Type> IDictionary<Type, Value>.Keys => _dict.Keys;
		ICollection<Value> IDictionary<Type, Value>.Values => _dict.Values;
		public IEnumerator<KeyValuePair<Type, Value>> GetEnumerator() => _dict.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public bool Contains(KeyValuePair<Type, Value> item) => _dict.Contains(item);
		public void CopyTo(KeyValuePair<Type, Value>[] array, int arrayIndex) => _dict.CopyTo(array, arrayIndex);

		public Value this[Type key]
		{
			get => _dict[key];
			set {
				_cache = null;
				_dict[key] = value;
			}
		}

		public void Add(KeyValuePair<Type, Value> item) => Add(item.Key, item.Value);
		public void Add(Type key, Value value)
		{
			_cache = null;
			_dict.Add(key, value);
		}

		public bool Remove(Type key)
		{
			if (_dict.Remove(key)) {
				_cache = null;
				return true;
			}
			return false;
		}

		public bool Remove(KeyValuePair<Type, Value> item)
		{
			if ((_dict as ICollection<KeyValuePair<Type, Value>>).Remove(item)) {
				_cache = null;
				return true;
			}
			return false;
		}

		public void Clear()
		{
			_cache = null;
			_dict.Clear();
		}

		public bool TryGetValue(Type type, out Value value)
		{
			Dictionary<RuntimeTypeHandle, Maybe<Value>> cache = _cache;
			if (cache == null) {
				if (_dict.Count == 0) {
					value = default;
					return false;
				}
				_cache = cache = new Dictionary<RuntimeTypeHandle, Maybe<Value>>();
			} else if (cache.TryGetValue(type.TypeHandle, out var maybe)) {
				value = maybe.Or(default);
				return maybe.HasValue;
			}

			// Search for the type itself first, then base classes, then interfaces
			if (_dict.TryGetValue(type, out value)) {
				cache.Add(type.TypeHandle, value);
				return true;
			}

			for (var type2 = type; type2.BaseType != null; type2 = type2.BaseType)
				if (_dict.TryGetValue(type2.BaseType, out value)) {
					cache[type.TypeHandle] = value;
					return true;
				}

			// Officially, .NET returns interfaces in no particular order.
			// It looks like the order is the result of a depth-first search.
			foreach (var iface in type.GetInterfaces())
				if (_dict.TryGetValue(iface, out value)) {
					cache.Add(type.TypeHandle, value);
					return true;
				}

			cache.Add(type.TypeHandle, new Maybe<Value>());
			return false;
		}
	}
}
