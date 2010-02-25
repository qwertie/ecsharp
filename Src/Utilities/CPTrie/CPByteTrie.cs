using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Runtime;
using Loyc.Utilities.CPTrie;
using System.Diagnostics;

namespace Loyc.Utilities
{
	public class CPByteTrie<TValue> : CPTrie<TValue>, IDictionary<byte[], TValue>
	{
		public CPByteTrie() { }
		public CPByteTrie(CPByteTrie<TValue> clone) : base(clone) { }

		public new int CountMemoryUsage(int sizeOfT) { return base.CountMemoryUsage(sizeOfT); }

		#region IDictionary<string,TValue> Members

		/// <summary>Adds the specified key-value pair to the trie, throwing an
		/// exception if the key is already present.</summary>
		public void Add(byte[] key, TValue value)
		{
			KeyWalker kw = new KeyWalker(key, key.Length);
			if (base.Set(ref kw, ref value, CPMode.Create))
				throw new ArgumentException(Localize.From("Key already exists: ") + key);
		}
		/// <summary>Adds the specified key-value pair to the trie, throwing an
		/// exception if the key is already present.</summary>
		/// <param name="key">An array that contains the key to add. The offset
		/// and length parameters specify a substring of this array to use as the key.</param>
		public void Add(byte[] key, int offset, int length, TValue value)
		{
			KeyWalker kw = new KeyWalker(key, offset, length);
			Check(ref kw, "Add");
			if (base.Set(ref kw, ref value, CPMode.Create))
				throw new ArgumentException(Localize.From("Key already exists: ") + key);
		}

		/// <summary>Adds the specified key-value pair only if the specified key is
		/// not already present in the trie.</summary>
		/// <param name="value">A value to associate with the specified key if the
		/// key does not already exist.</param>
		/// <returns>Returns true if the key-value pair was added or false if
		/// the key already existed. In the false case, the trie is not modified.</returns>
		public bool TryAdd(byte[] key, TValue value)
		{
			return TryAdd(key, 0, key.Length, ref value);
		}
		public bool TryAdd(byte[] key, int offset, int length, TValue value)
		{
			return TryAdd(key, 0, key.Length, ref value);
		}
		
		/// <summary>Adds the specified key-value pair only if the specified key is
		/// not already present in the trie.</summary>
		/// <param name="key">An array that contains the key to find. The offset
		/// and length parameters specify a substring of this array to use as the key.</param>
		/// <param name="value">On entry, value specifies the value to associate
		/// with the specified key, but if the key already exists, value is changed
		/// to the value associated with the existing key.</param>
		/// <returns>Returns true if the key-value pair was added or false if
		/// the key already existed. In the false case, the trie is not modified.</returns>
		public bool TryAdd(byte[] key, int offset, int length, ref TValue value)
		{
			KeyWalker kw = new KeyWalker(key, offset, length);
			Check(ref kw, "TryAdd");
			return !base.Set(ref kw, ref value, CPMode.Create);
		}

		private void Check(ref KeyWalker kw, string operation)
		{
			if ((kw.Offset | kw.Left) < 0)
				throw new ArgumentException(operation + ": " + Localize.From("offset or length are negative"));
			if (kw.Offset + kw.Left > kw.Buffer.Length)
				throw new ArgumentException(operation + ": " + Localize.From("offset+length exceeds buffer length"));
		}

		/// <summary>Searches for the specified key, returning true if it is
		/// present in the trie.</summary>
		public bool ContainsKey(byte[] key)
		{
			KeyWalker kw = new KeyWalker(key, key.Length);
			TValue value = default(TValue);
			return base.Find(ref kw, ref value);
		}
		/// <summary>Searches for the specified key, returning true if it is
		/// present in the trie.</summary>
		/// <param name="key">An array that contains the key to find. The offset
		/// and length parameters specify a substring of this array to use as the key.</param>
		public bool ContainsKey(byte[] key, int offset, int length)
		{
			KeyWalker kw = new KeyWalker(key, offset, length);
			Check(ref kw, "ContainsKey");
			TValue value = default(TValue);
			return base.Find(ref kw, ref value);
		}

		/// <summary>Removes the specified key and associated value, returning true
		/// if the entry was found and removed.</summary>
		public bool Remove(byte[] key)
		{
			TValue dummy = default(TValue);
			return Remove(key, 0, key.Length, ref dummy);
		}
		public bool Remove(byte[] key, int offset, int length)
		{
			TValue dummy = default(TValue);
			return Remove(key, offset, length, ref dummy);
		}
		/// <summary>Removes the specified key and associated value, returning true
		/// if the entry was found and removed.</summary>
		/// <param name="key">An array that contains the key to find. The offset
		/// and length parameters specify a substring of this array to use as the key.</param>
		/// <param name="oldValue">If the key is found, the associated value is
		/// assigned to this parameter. Otherwise, this parameter is not changed.</param>
		public bool Remove(byte[] key, int offset, int length, ref TValue oldValue)
		{
			KeyWalker kw = new KeyWalker(key, offset, length);
			Check(ref kw, "Remove");
			return base.Remove(ref kw, ref oldValue);
		}

		/// <summary>Finds the specified key and gets its associated value,
		/// returning true if the key was found.</summary>
		public bool TryGetValue(byte[] key, out TValue value)
		{
			KeyWalker kw = new KeyWalker(key, 0, key.Length);
			value = default(TValue);
			return base.Find(ref kw, ref value);
		}
		public bool TryGetValue(byte[] key, int offset, int length, out TValue value)
		{
			KeyWalker kw = new KeyWalker(key, offset, length);
			Check(ref kw, "TryGetValue");
			value = default(TValue);
			return base.Find(ref kw, ref value);
		}
		/// <summary>Finds the specified key and returns its associated value. If 
		/// the key did not exist, TryGetValue returns defaultValue instead.</summary>
		public TValue TryGetValue(byte[] key, TValue defaultValue)
		{
			KeyWalker kw = new KeyWalker(key, 0, key.Length);
			base.Find(ref kw, ref defaultValue);
			return defaultValue;
		}
		public TValue TryGetValue(byte[] key, int offset, int length, TValue defaultValue)
		{
			KeyWalker kw = new KeyWalker(key, offset, length);
			Check(ref kw, "TryGetValue");
			base.Find(ref kw, ref defaultValue);
			return defaultValue;
		}

		public TValue this[byte[] key]
		{
			get {
				KeyWalker kw = new KeyWalker(key, key.Length);
				TValue value = default(TValue);
				if (!base.Find(ref kw, ref value))
					throw new KeyNotFoundException(Localize.From("Key not found: ") + key);
				return value;
			}
			set {
				KeyWalker kw = new KeyWalker(key, key.Length);
				base.Set(ref kw, ref value, CPMode.Set | CPMode.Create);
			}
		}

		public ICollection<byte[]> Keys
		{
			get { throw new NotImplementedException(); }
		}
		public ICollection<TValue> Values
		{
			get { throw new NotImplementedException(); }
		}

		public void Add(KeyValuePair<byte[], TValue> item)
		{
			Add(item.Key, item.Value);
		}

		public new void Clear()
		{
			base.Clear();
		}

		public bool Contains(KeyValuePair<byte[], TValue> item)
		{
			KeyWalker kw = new KeyWalker(item.Key, item.Key.Length);
			TValue value = default(TValue);
			if (base.Find(ref kw, ref value))
				return DefaultComparer.Compare(value, item.Value) == 0;
			return false;
		}

		public void CopyTo(KeyValuePair<byte[], TValue>[] array, int arrayIndex)
		{
			foreach (KeyValuePair<byte[], TValue> pair in this)
				array[arrayIndex] = pair;
		}

		public new int Count
		{
			get { return base.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(KeyValuePair<byte[], TValue> item)
		{
			KeyWalker kw = new KeyWalker(item.Key, item.Key.Length);
			KeyWalker kw2 = kw;
			TValue value = default(TValue);
			if (Find(ref kw, ref value) && DefaultComparer.Compare(value, item.Value) == 0)
				return Remove(ref kw2, ref value);
			return false;
		}

		#endregion

		#region IEnumerable<KeyValuePair<byte[],TValue>> Members

		public Enumerator GetEnumerator()
		{
			return new Enumerator(this);
		}
		IEnumerator<KeyValuePair<byte[], TValue>> IEnumerable<KeyValuePair<byte[], TValue>>.GetEnumerator()
		{
			return GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		public CPByteTrie<TValue> Clone()
			{ return new CPByteTrie<TValue>(this); }

		public Enumerator FindAtLeast(byte[] key)
		{
			KeyWalker kw = new KeyWalker(key, key.Length);
			Enumerator e = new Enumerator(this);
			base.Find(ref kw, e);
			return e;
		}
		public Enumerator FindExact(byte[] key)
		{
			KeyWalker kw = new KeyWalker(key, key.Length);
			Enumerator e = new Enumerator(this);
			if (!base.Find(ref kw, e))
				return null;
			Debug.Assert(e.IsValid);
			return e;
		}
		public bool Find(byte[] key, out Enumerator e)
		{
			KeyWalker kw = new KeyWalker(key, key.Length);
			e = new Enumerator(this);
			return base.Find(ref kw, e);
		}
		public bool Find(byte[] key, int offset, int length, out Enumerator e)
		{
			KeyWalker kw = new KeyWalker(key, offset, length);
			Check(ref kw, "Find");
			e = new Enumerator(this);
			return base.Find(ref kw, e);
		}

		public bool IsEmpty { get { return base.Count == 0; } }

		public class Enumerator : CPEnumerator<TValue>, IEnumerator<KeyValuePair<byte[], TValue>>
		{
			internal protected Enumerator(CPTrie<TValue> trie) : base(trie) {}

			public new KeyValuePair<byte[], TValue> Current
			{
				get {
					return new KeyValuePair<byte[], TValue>(CurrentKey, CurrentValue);
				}
			}
			object System.Collections.IEnumerator.Current
			{
				get { return Current; }
			}
			public new TValue CurrentValue
			{
				get { return base.CurrentValue; }
			}
			public new byte[] CurrentKey
			{
				get {
					int len = Key.Offset + Key.Left;
					return InternalList<byte>.CopyToNewArray(Key.Buffer, len, len);
				}
			}
		}
	}
}
