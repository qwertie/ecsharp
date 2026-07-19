using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Loyc.Collections;

// They want me to put a "where K: notnull" constraint on these. I disagree. The warning says:
// type 'K' cannot be used as...'TKey' in...'IDictionary<TKey, TValue>'. Nullability of...'K' doesn't match 'notnull' constraint.
#pragma warning disable 8714 

namespace Loyc.Collections
{
	/// <summary>A dictionary wrapper that provides ListChanging and ListChanged events.
	/// Shorthand for Loyc.Collections.Impl.DictionaryWithChangeEvents{K,V,IDictionary{K,V}}.</summary>
	public class DictionaryWithChangeEvents<K, V> : Impl.DictionaryWithChangeEvents<K, V, IDictionary<K, V>>
	{
		public DictionaryWithChangeEvents(IDictionary<K, V> wrappedObject) : base(wrappedObject) { }
		public DictionaryWithChangeEvents() : base(new Dictionary<K, V>()) { }
	}
}

namespace Loyc.Collections.Impl
{
	///	<summary>A dictionary wrapper that provides ListChanging and ListChanged events.
	///	You can also implement custom behavior by overriding its methods.</summary>
	///	<remarks>
	///	The Keys and Values properties return ICollection, but this class assumes that 
	///	mutating these collections is not allowed (mutating them is not allowed, for 
	///	example, by Dictionary{K,V}). Therefore change notification is not implemented 
	///	for changes to these collections; these properties simply return the original 
	///	collection.
	/// </remarks>
	///	<seealso cref="ListWrapper{TList,T}"/>
	public class DictionaryWithChangeEvents<K, V, TDictionary> : DictionaryWrapper<K, V, TDictionary>,
		IDictionaryWithChangeEvents<K, V>, IAddRange<KeyValuePair<K, V>>
		where TDictionary : IDictionary<K, V>
	{
		public DictionaryWithChangeEvents(TDictionary dictionary) : base(dictionary) { }

		public virtual event ListChangingHandler<KeyValuePair<K, V>, IDictionary<K, V>>? ListChanging;
		public virtual event ListChangingHandler<KeyValuePair<K, V>, IDictionary<K, V>>? ListChanged;

		public override V this[K key]
		{
			get => _obj[key];
			set
			{
				if ((ListChanged ?? ListChanging) == null)
					_obj[key] = value;
				else
					FancySet(key, value);
			}
		}

		private void FancySet(K key, V value)
		{
			var newItem = ListExt.Single(new KeyValuePair<K, V>(key, value));
			IListSource<KeyValuePair<K, V>> oldItem = EmptyList<KeyValuePair<K, V>>.Value;
			var sizeChange = 1;
			var action = NotifyCollectionChangedAction.Add;
			if (_obj.TryGetValueSafe(key, out V? oldValue))
			{
				oldItem = ListExt.Single(new KeyValuePair<K, V>(key, oldValue));
				sizeChange = 0;
				action = NotifyCollectionChangedAction.Replace;
			}
			var info = new ListChangeInfo<KeyValuePair<K, V>>(action, int.MinValue, sizeChange, newItem, oldItem);
			ListChanging?.Invoke(this, info);
			_obj[key] = value;
			ListChanged?.Invoke(this, info);
		}

		public virtual bool TryAdd(K key, V value)
		{
			if (_obj.ContainsKey(key))
				return false;
			if ((ListChanged ?? ListChanging) == null)
				_obj.Add(key, value);
			else {
				var newItem = ListExt.Single(new KeyValuePair<K, V>(key, value));
				var info = new ListChangeInfo<KeyValuePair<K, V>>(NotifyCollectionChangedAction.Add, int.MinValue, 1, newItem, EmptyList<KeyValuePair<K, V>>.Value);
				ListChanging?.Invoke(this, info);
				_obj.Add(key, value);
				ListChanged?.Invoke(this, info);
			}
			return true;
		}
		public override void Add(KeyValuePair<K, V> item)
		{
			if ((ListChanged ?? ListChanging) == null || !TryAdd(item.Key, item.Value))
				_obj.Add(item);
		}
		public override void Add(K key, V value)
		{
			if ((ListChanged ?? ListChanging) == null || !TryAdd(key, value))
				_obj.Add(key, value);
		}

		public override void Clear()
		{
			if ((ListChanged ?? ListChanging) == null)
				_obj.Clear();
			else if (!IsEmpty) {
				var oldItems = new DList<KeyValuePair<K, V>>(_obj);
				var info = new ListChangeInfo<KeyValuePair<K, V>>(NotifyCollectionChangedAction.Reset, int.MinValue, -oldItems.Count, EmptyList<KeyValuePair<K, V>>.Value, oldItems);
				ListChanging?.Invoke(this, info);
				_obj.Clear();
				ListChanged?.Invoke(this, info);
			}
		}

		public override bool Remove(KeyValuePair<K, V> item)
		{
			if ((ListChanged ?? ListChanging) == null)
				return _obj.Remove(item);
			else
			{
				if (!_obj.Contains(item))
					return false;
				var oldItem = ListExt.Single(item);
				var info = new ListChangeInfo<KeyValuePair<K, V>>(NotifyCollectionChangedAction.Remove, int.MinValue, -1, EmptyList<KeyValuePair<K, V>>.Value, oldItem);
				ListChanging?.Invoke(this, info);
				var result = _obj.Remove(item);
				ListChanged?.Invoke(this, info);
				return result;
			}
		}

		public override bool Remove(K key)
		{
			if ((ListChanged ?? ListChanging) == null)
				return _obj.Remove(key);
			else
			{
				if (!_obj.TryGetValueSafe(key, out V? value))
					return false;
				var oldItem = ListExt.Single(new KeyValuePair<K, V>(key, value));
				var info = new ListChangeInfo<KeyValuePair<K, V>>(NotifyCollectionChangedAction.Remove, int.MinValue, -1, EmptyList<KeyValuePair<K, V>>.Value, oldItem);
				ListChanging?.Invoke(this, info);
				var result = _obj.Remove(key);
				ListChanged?.Invoke(this, info);
				return result;
			}
		}

		public virtual void AddRange(IEnumerable<KeyValuePair<K, V>> list)
		{
			if ((ListChanged ?? ListChanging) == null)
				DictionaryExt.AddRange(_obj, list);
			else
			{
				var list2 = new DList<KeyValuePair<K, V>>(list);
				if (list2.Count != 0) {
					var info = new ListChangeInfo<KeyValuePair<K, V>>(NotifyCollectionChangedAction.Add, int.MinValue, list2.Count, list2, EmptyList<KeyValuePair<K, V>>.Value);
					ListChanging?.Invoke(this, info);
					DictionaryExt.AddRange(_obj, list);
					ListChanged?.Invoke(this, info);
				}
			}
		}

		public virtual void AddRange(IReadOnlyCollection<KeyValuePair<K, V>> list) => AddRange(list as IEnumerable<KeyValuePair<K, V>>);
	}
}
