using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Collections
{
	partial class DictionaryExt
	{
		// They want me to put a "where K: notnull" constraint on this. I disagree. The warning says:
		// type 'K' cannot be used as...'TKey' in...'IDictionary<TKey, TValue>'. Nullability of...'K' doesn't match 'notnull' constraint.
		#pragma warning disable 8714 

		/// <summary>Adapts a dictionary to the <see cref="IDictionarySink{K, V}"/> interface.</summary>
		public static IDictionarySink<K, V> AsSink<K, V>(this IDictionary<K, V> dict)
			=> new DictionaryAsSink<K, V, IDictionary<K, V>>(dict);
	}

	/// <summary>Helps implement extension method <see cref="DictionaryExt.AsSink{K, V}(IDictionary{K, V})"/>.</summary>
	public class DictionaryAsSink<K, V, Dict> : CollectionAsSink<KeyValuePair<K, V>, Dict>, IDictionarySink<K, V> where Dict : IDictionary<K, V>
	{
		public DictionaryAsSink(Dict dict) : base(dict) { }

		public V this[K key] { set => _obj[key] = value; }

		public void Add(K key, V value) => _obj.Add(key, value);

		public bool Remove(K key) => _obj.Remove(key);
	}
}
