using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Loyc.Collections
{
	/// <summary>A bijection is a one-to-one function and its inverse. It is 
	/// implemented with a pair of dictionaries, one that maps K1 to K2 and
	/// another that maps K2 to K1.</summary>
	/// <typeparam name="K1">Default key.</typeparam>
	/// <typeparam name="K2">Inverse key.</typeparam>
	/// <remarks>
	/// The Bijection object that you first create is a dictionary from K1 to K2.
	/// Call the <see cref="Inverse"/> property to get the inverse dictionary from
	/// K2 back to K1. Whenever you modify one of the dictionaries, the other 
	/// dictionary is also modified in the same way. 
	/// <para/>
	/// For example, if you do this:
	/// <code>
	///   var map = new Bijection&lt;int,string>() { { 1, "one" }, {2, "two"} };
	///   map.Add(3, "three");
	///   map.Inverse.Add("four", 4);
	///   map.Inverse.Remove("two");
	/// </code>
	/// Then the bijection will contain three pairs: <c>{1, "one"}, {3, "three"}</c> 
	/// and <c>{4, "four"}</c>. The inverse bijection object map.Inverse is "first-
	/// class" and can be used just like the original map. The two interfaces, <c>map</c>
	/// and <c>map.Inverse</c>, are linked to each other, so 
	/// <c>map.Inverse.Inverse == map</c>.
	/// <para/>
	/// This collection itself is not safe for multithreaded access, even if it is 
	/// constructed out of two ConcurrentDictionary{K,V} objects.
	/// </remarks>
	public class Bijection<K1,K2> : IDictionary<K1, K2>, IReadOnlyDictionary<K1, K2>
	{
		IDictionary<K1,K2> _map;
		Bijection<K2,K1> _inverse;

		/// <summary>Constructs a bijection out of two <see cref="Dictionary{TKey,TValue}"/> objects.</summary>
		public Bijection() : this(new Dictionary<K1, K2>(), new Dictionary<K2, K1>()) { }
		
		/// <summary>Constructs a bijection out of two <see cref="Dictionary{TKey,TValue}"/> 
		/// objects, each with the specified capacity.</summary>
		public Bijection(int capacity) : this(new Dictionary<K1, K2>(capacity), new Dictionary<K2, K1>(capacity)) { }
		
		/// <summary>Constructs a bijection out of two <see cref="Dictionary{TKey,TValue}"/> 
		/// objects, copying the specified initial contents, and using the initial 
		/// Count as the capacity.</summary>
		/// <exception cref="KeyAlreadyExistsException">The input had a duplicate key or value.</exception>
		/// <remarks>The bijection is mutable even though the input is not.</remarks>
		public Bijection(IReadOnlyCollection<KeyValuePair<K1,K2>> input) : this((IEnumerable<KeyValuePair<K1,K2>>)input, input.Count) { }
		
		/// <summary>Constructs a bijection out of two <see cref="Dictionary{TKey,TValue}"/> 
		/// objects, copying the specified initial contents.</summary>
		/// <exception cref="KeyAlreadyExistsException">The input had a duplicate key or value.</exception>
		public Bijection(IEnumerable<KeyValuePair<K1,K2>> input, int capacity = 0) 
			: this(new Dictionary<K1, K2>(capacity), new Dictionary<K2, K1>(capacity)) 
		{
			foreach (var pair in input)
				Add(pair.Key, pair.Value);
		}
		
		/// <summary>Constructs a bijection out of two existing dictionaries.</summary>
		/// <remarks>
		/// To save time, the constructor does not verify that the two dictionaries 
		/// are a proper bijection; instead it works "on the honor system", and only
		/// checks to ensure that the two dictionaries have the same <c>Count</c>. 
		/// The two dictionaries must already be a bijection (one-to-one maps into 
		/// each other) and should not be modified after they are attached to this
		/// object. Of course, you can supply two empty dictionaries of any type.
		/// </remarks>
		public Bijection(IDictionary<K1, K2> cur, IDictionary<K2, K1> inv)
		{
			_map = cur;
			_inverse = new Bijection<K2, K1>(inv, this);
			if (cur.Count != inv.Count)
				throw new InvalidStateException("Bijection(): invalid input, dictionaries have different lengths");
			if (cur.IsReadOnly != inv.IsReadOnly)
				throw new ArgumentException("Bijection(): IsReadOnly mismatch");
		}
		protected Bijection(IDictionary<K1, K2> cur, Bijection<K2, K1> other)
		{
			_map = cur;
			_inverse = other;
		}

		/// <summary>Returns the inverse dictionary. Note: <c>this.Inverse.Inverse == this</c>.</summary>
		public Bijection<K2, K1> Inverse { [DebuggerStepThrough] get { return _inverse; } }

		public void Add(K1 key, K2 value)
		{
			_map.Add(key, value);
			try {
				_inverse.Add(value, key);
			} catch (InvalidOperationException) {
				_map.Remove(key);
				CheckSync();
				throw;
			}
		}
		public bool ContainsKey(K1 key)
		{
			return _map.ContainsKey(key);
		}
		public ICollection<K1> Keys
		{
			get { return _map.Keys; }
		}
		public ICollection<K2> Values
		{
			get { return _map.Values; }
		}
		IEnumerable<K1> IReadOnlyDictionary<K1, K2>.Keys
		{
			get { return Keys; }
		}
		IEnumerable<K2> IReadOnlyDictionary<K1, K2>.Values
		{
			get { return Values; }
		}

		public bool Remove(K1 key)
		{
			K2 value;
			if (_map.TryGetValue(key, out value)) {
				G.Verify(_map.Remove(key));
				G.Verify(_inverse._map.Remove(value));
				CheckSync();
				return true;
			}
			return false;
		}

		private void CheckSync()
		{
			if (_map.Count != _inverse._map.Count)
				throw new InvalidStateException(Localize.Localized("{0}: out of sync! ({1}!={2})",
					GetType().NameWithGenericArgs(), _map.Count, _inverse._map.Count));
		}

		public bool TryGetValue(K1 key, out K2 value)
		{
			return _map.TryGetValue(key, out value);
		}

		public K2 this[K1 key]
		{
			get { return _map[key]; }
			set
			{
				K2 oldValue;
				if (_map.TryGetValue(key, out oldValue))
					Remove(key);
				Add(key, value);
			}
		}

		public void Add(KeyValuePair<K1, K2> item)
		{
			Add(item.Key, item.Value);
		}

		public void Clear()
		{
			try {
				_map.Clear();
				_inverse._map.Clear();
			} catch {
				CheckSync();
				throw;
			}
		}

		public bool Contains(KeyValuePair<K1, K2> item)
		{
			// Note that only the forward _map knows how to compare two keys for 
			// equality and only the inverse map knows how to compare two values 
			// for equality. So we cannot, say, call _map.TryGetValue and then try 
			// to compare item.Value to the retrieved value by ourselves.
			return _map.ContainsKey(item.Key) && _inverse._map.ContainsKey(item.Value);
		}

		public void CopyTo(KeyValuePair<K1, K2>[] array, int arrayIndex)
		{
			_map.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return _map.Count; }
		}

		public bool IsReadOnly
		{
			get { return _map.IsReadOnly; }
		}

		public bool Remove(KeyValuePair<K1, K2> item)
		{
			if (Contains(item))
				return Remove(item.Key);
			return false;
		}

		public IEnumerator<KeyValuePair<K1, K2>> GetEnumerator()
		{
			return _map.GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
