using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Collections.Impl
{
	///	<summary>A simple base class that helps you use the decorator pattern on a dictionary.
	///	By default, all it does is forward every method to the underlying collection
	///	(including GetHashCode, Equals and ToString). You can change its behavior by
	///	overriding methods.</summary>
	///	<remarks>This could be used, for example, to help you implement a collection
	///	that needs to take some kind of action whenever the collection is modified.
	/// </remarks>
	///	<seealso cref="ListWrapper{TList,T}"/>
	///	<seealso cref="DictionaryWithChangeEvents{TDictionary, K, V}"/>
	public abstract class DictionaryWrapper<K, V, TDictionary> : CollectionWrapper<KeyValuePair<K,V>, TDictionary>, 
		IDictionaryAndReadOnly<K, V> where TDictionary : IDictionary<K, V>
	{
		public DictionaryWrapper(TDictionary dictionary) : base(dictionary) { }

		protected TDictionary Dictionary => _obj;

		public virtual V this[K key] {
			get => _obj[key];
			set => _obj[key] = value;
		}

		public virtual void Add(K key, V value) => _obj.Add(key, value);
		public virtual bool Remove(K key) => _obj.Remove(key);

		public virtual bool ContainsKey(K key) => _obj.ContainsKey(key);
		public virtual bool TryGetValue(K key, out V value) => _obj.TryGetValue(key, out value);
		public V TryGet(K key, out bool fail)
		{
			if (key != null) {
				fail = !TryGetValue(key, out V value);
				return value;
			} else {
				fail = true;
				return default(V);
			}
		}

		public virtual ICollection<K> Keys => _obj.Keys;
		public virtual ICollection<V> Values => _obj.Values;
		IEnumerable<K> IReadOnlyDictionary<K, V>.Keys => Keys;
		IEnumerable<V> IReadOnlyDictionary<K, V>.Values => Values;
	}
}
