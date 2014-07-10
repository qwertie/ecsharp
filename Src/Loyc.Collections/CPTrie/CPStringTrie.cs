// http://www.codeproject.com/KB/recipes/cptrie.aspx
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Loyc.Collections.Impl;

namespace Loyc.Collections
{
	/// <summary>A compact patricia trie that uses strings as keys.</summary>
	/// <typeparam name="TValue">Type of value associated with each key.</typeparam>
	public class CPStringTrie<TValue> : CPTrie<TValue>, IDictionary<string, TValue>
	{
		public CPStringTrie() { }
		public CPStringTrie(CPStringTrie<TValue> clone) : base(clone) { }

		public new int CountMemoryUsage(int sizeOfT) { return base.CountMemoryUsage(sizeOfT); }

		#region IDictionary<string,TValue> Members

		public void Add(string key, TValue value)
		{
			KeyWalker kw = StringToBytes(key);
			if (base.Set(ref kw, ref value, CPMode.Create))
				throw new ArgumentException(Localize.From("Key already exists: ") + key);
		}

		/// <summary>Adds the specified key-value pair only if the specified key is
		/// not already present in the trie.</summary>
		/// <returns>Returns true if the key-value pair was added or false if
		/// the key already existed. In the false case, the trie is not modified.</returns>
		public bool TryAdd(string key, TValue value)
		{
			KeyWalker kw = StringToBytes(key);
			return !base.Set(ref kw, ref value, CPMode.Set);
		}
		/// <summary>Adds the specified key-value pair only if the specified key is
		/// not already present in the trie.</summary>
		/// <param name="value">On entry, value specifies the value to associate
		/// with the specified key, but if the key already exists, value is changed
		/// to the value associated with the existing key.</param>
		/// <returns>Returns true if the key-value pair was added or false if
		/// the key already existed. In the false case, the trie is not modified.</returns>
		public bool TryAdd(string key, ref TValue value)
		{
			KeyWalker kw = StringToBytes(key);
			return !base.Set(ref kw, ref value, CPMode.Create);
		}

		public bool ContainsKey(string key)
		{
			KeyWalker kw = StringToBytes(key);
			TValue value = default(TValue);
			return base.Find(ref kw, ref value);
		}

		public bool Remove(string key)
		{
			KeyWalker kw = StringToBytes(key);
			TValue oldValue = default(TValue);
			return base.Remove(ref kw, ref oldValue);
		}
		public bool Remove(string key, ref TValue value)
		{
			KeyWalker kw = StringToBytes(key);
			return base.Remove(ref kw, ref value);
		}

		public bool TryGetValue(string key, out TValue value)
		{
			KeyWalker kw = StringToBytes(key);
			value = default(TValue);
			return base.Find(ref kw, ref value);
		}

		public TValue this[string key, TValue defaultValue]
		{
			get {
				KeyWalker kw = StringToBytes(key);
				base.Find(ref kw, ref defaultValue);
				return defaultValue;
			}
		}
		public TValue this[string key]
		{
			get {
				KeyWalker kw = StringToBytes(key);
				TValue value = default(TValue);
				if (!base.Find(ref kw, ref value))
					throw new KeyNotFoundException(Localize.From("Key not found: ") + key);
				return value;
			}
			set {
				KeyWalker kw = StringToBytes(key);
				base.Set(ref kw, ref value, CPMode.Set | CPMode.Create);
			}
		}

		ICollection<string> IDictionary<string, TValue>.Keys { get { return Keys; } }
		ICollection<TValue> IDictionary<string, TValue>.Values { get { return Values; } }
		public KeyCollection Keys
		{
			get { return new KeyCollection(this); }
		}
		public CPValueCollection<TValue> Values
		{
			get { return new CPValueCollection<TValue>(this); }
		}

		public void Add(KeyValuePair<string, TValue> item)
		{
			Add(item.Key, item.Value);
		}

		public new void Clear()
		{
			base.Clear();
		}

		public bool Contains(KeyValuePair<string, TValue> item)
		{
			KeyWalker kw = StringToBytes(item.Key);
			TValue value = default(TValue);
			if (base.Find(ref kw, ref value))
				return DefaultComparer.Compare(value, item.Value) == 0;
			return false;
		}

		public void CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex)
		{
			foreach (KeyValuePair<string, TValue> pair in this)
				array[arrayIndex++] = pair;
		}

		public new int Count
		{
			get { return base.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(KeyValuePair<string, TValue> item)
		{
			KeyWalker kw = StringToBytes(item.Key);
			KeyWalker kw2 = kw;
			TValue value = default(TValue);
			if (Find(ref kw, ref value) && DefaultComparer.Compare(value, item.Value) == 0)
				return Remove(ref kw2, ref value);
			return false;
		}

		#endregion

		#region IEnumerable<KeyValuePair<string,TValue>> Members

		public Enumerator GetEnumerator()
		{
			return new Enumerator(this);
		}
		IEnumerator<KeyValuePair<string, TValue>> IEnumerable<KeyValuePair<string, TValue>>.GetEnumerator()
		{
			return GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region Find methods

		public Enumerator FindAtLeast(string key)
		{
			KeyWalker kw = StringToBytes(key);
			Enumerator e = new Enumerator(this);
			base.Find(ref kw, e);
			return e;
		}
		public Enumerator FindExact(string key)
		{
			KeyWalker kw = StringToBytes(key);
			Enumerator e = new Enumerator(this);
			if (!base.Find(ref kw, e))
				return null;
			Debug.Assert(e.IsValid);
			return e;
		}
		public bool Find(string key, out Enumerator e)
		{
			KeyWalker kw = StringToBytes(key);
			e = new Enumerator(this);
			return base.Find(ref kw, e);
		}

		#endregion

		public CPStringTrie<TValue> Clone()
			{ return new CPStringTrie<TValue>(this); }

		public bool IsEmpty { get { return base.Count == 0; } }

		#region Enumerator and KeyEnumerator
		// CPEnumerator<TValue> is the corresponding value enumerator

		/// <summary>Enumerates key-value pairs in a CPStringTrie.</summary>
		/// <remarks>Reading the key is more expensive than reading the value
		/// because the key must be decoded from the bytes it is made up of.
		/// If you call CurrentValue instead of Current or CurrentKey, the
		/// work of decoding the key will be avoided. If you only need to
		/// enumerate the values, enumerate the Values collection instead of 
		/// the trie class itself.
		/// </remarks>
		public class Enumerator : CPEnumerator<TValue>, IEnumerator<KeyValuePair<string, TValue>>
		{
			internal protected Enumerator(CPTrie<TValue> trie) : base(trie) {}

			public new KeyValuePair<string, TValue> Current
			{
				get {
					return new KeyValuePair<string, TValue>(CurrentKey, CurrentValue);
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
			public new string CurrentKey
			{
				get {
					return CPTrie<TValue>.BytesToString(Key.Buffer, Key.Offset + Key.Left);
				}
			}
		}

		/// <summary>Enumerates keys of a CPStringTrie.</summary>
		/// <remarks>
		/// Avoid calling Current more than once per key, as each call requires the
		/// key to be decoded from the bytes it is made up of.
		/// </remarks>
		public class KeyEnumerator : CPEnumerator<TValue>, IEnumerator<string>
		{
			internal protected KeyEnumerator(CPTrie<TValue> trie) : base(trie) {}

			public new string Current
			{
				get { return CPTrie<TValue>.BytesToString(Key.Buffer, Key.Offset + Key.Left); }
			}
			object System.Collections.IEnumerator.Current
			{
				get { return Current; }
			}
		}

		#endregion

		#region KeyCollection
		// CPValueCollection<TValue> is the corresponding value collection

		/// <summary>Return value of <see cref="CPStringTrie{T}.Keys"/.></summary>
		public class KeyCollection : ICollection<string>
		{
			CPTrie<TValue> _trie;
			public KeyCollection(CPTrie<TValue> trie) { _trie = trie; }

			#region ICollection<string> Members

			public void Add(string item)    { throw new NotSupportedException(); }
			public void Clear()             { throw new NotSupportedException(); }
			public bool Remove(string item) { throw new NotSupportedException(); }
			public bool IsReadOnly          { get { return true; } }
			public int Count                { get { return _trie.Count; } }

			public bool Contains(string item)
			{
				foreach (string value in this)
					if (value == item)
						return true;
				return false;
			}

			public void CopyTo(string[] array, int arrayIndex)
			{
				if (arrayIndex < 0)
					throw new ArgumentOutOfRangeException();
				if (arrayIndex + _trie.Count > array.Length)
					throw new ArgumentException();
				foreach (string value in this)
					array[arrayIndex++] = value;
			}

			public KeyEnumerator GetEnumerator()
			{
				return new KeyEnumerator(_trie);
			}
			IEnumerator<string> IEnumerable<string>.GetEnumerator()
			{
				return GetEnumerator();
			}
			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			#endregion
		}

		#endregion
	}
}
