using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Threading;
using Loyc.Collections.Impl;

namespace Loyc.Collections
{
	/// <summary>
	/// A dictionary in which the values are weak. When a value has been garbage-
	/// collected, the dictionary acts as if the key is not present (except the
	/// Remove() method, which saves time by not checking whether the value is 
	/// dead.)
	/// </summary>
	/// <remarks>
	/// This is implemented on top of a standard Dictionary. In order to allow the 
	/// dictionary to be read safely while it is being enumerated, cleanup 
	/// operations (to remove entries whose value has been garbage-collected) do 
	/// not occur automatically during read operations. You can allow cleanups
	/// by calling AutoCleanup().
	/// </remarks>
	public class WeakValueDictionary<K,V> : BaseDictionary<K,V> where V : class
	{
		static readonly WeakReference<V> WeakNull = new WeakReference<V>(null);
		Dictionary<K, WeakReference<V>> _dict = new Dictionary<K, WeakReference<V>>();
		int _accessCounter;

		/// <summary>Periodically removes entries with garbage-collected values from the dictionary</summary>
		public bool AutoCleanup()
		{
			if (_accessCounter++ > (Count << 2)) {
				Cleanup();
				return true;
			}
			return false;
		}
		/// <summary>Removes entries with garbage-collected values from the dictionary.</summary>
		public void Cleanup()
		{
			List<K> _removeQueue = new List<K>();
			foreach (var kvp in _dict)
				if (!kvp.Value.IsAlive() && kvp.Value != WeakNull)
					_removeQueue.Add(kvp.Key);
			for (int i = 0; i < _removeQueue.Count; i++)
				_dict.Remove(_removeQueue[i]);
			_accessCounter = -1;
		}

		/// <summary>Returns the number of dictionary entries. This value may be
		/// greater than the number of elements that are still alive.</summary>
		public override int Count
		{
			get { return _dict.Count; }
		}

		public override void Clear()
		{
			_accessCounter = -1;
			_dict.Clear();
		}

		public override void Add(K key, V value)
		{
			_accessCounter += 4;
			WeakReference<V> wv = _dict.TryGetValue(key, null);
			if (wv != null) {
				if (wv.IsAlive() || wv == WeakNull)
					throw new KeyAlreadyExistsException();
				else if (value != null) {
					wv.Target = value;
					return;
				}
			}
			_dict[key] = value == null ? WeakNull : new WeakReference<V>(value);
		}

		public override bool ContainsKey(K key)
		{
			_accessCounter++;
			WeakReference<V> wv = _dict.TryGetValue(key, null);
			if (wv != null)
				if (wv.IsAlive())
					return true;
				else
					_dict.Remove(key);
			return false;
		}

		public override bool Remove(K key)
		{
			_accessCounter++;
			return _dict.Remove(key);
		}

		public override bool TryGetValue(K key, out V value)
		{
			_accessCounter++;
			WeakReference<V> wv = _dict.TryGetValue(key, null);
			if (wv != null)
			{
				value = wv.Target();
				if (value != null || wv == WeakNull)
					return true;
				else
					_dict.Remove(key);
			}
			value = default(V);
			return false;
		}

		public override IEnumerator<KeyValuePair<K, V>> GetEnumerator()
		{
			foreach (var kvp in _dict)
			{
				var target = kvp.Value.Target();
				if (target != null || kvp.Value == WeakNull)
					yield return new KeyValuePair<K, V>(kvp.Key, target);
			}
			_accessCounter += Count;
		}

		protected override void SetValue(K key, V value)
		{
			_accessCounter += 3;
			WeakReference<V> wv = _dict.TryGetValue(key, null);
			if (wv != null && (value == null) == (wv == WeakNull))
				wv.Target = value;
			else
				_dict[key] = value == null ? WeakNull : new WeakReference<V>(value);
		}
	}
}
