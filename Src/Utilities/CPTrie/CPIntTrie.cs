// http://www.codeproject.com/KB/recipes/cptrie.aspx
using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Essentials;
using System.Diagnostics;
using Loyc.Collections.CPTrie;

namespace Loyc.Collections
{
	/// <summary>A trie that supports signed and unsigned keys with sizes up to 64
	/// bits. Special encodings are used to preserve the sort order among integers
	/// of different sizes while using variable-length keys.</summary>
	/// <typeparam name="TValue">Type of value associated with each key.</typeparam>
	/// <remarks>This trie allows you to insert integers of different sizes. Two
	/// integers of different sizes that have the same value are considered
	/// equivalent. You can insert a key as an Int16, then extract it as an Int32,
	/// for example.
	/// <para/>
	/// This collection implements two versions of IDictionary: one using Int32
	/// keys and another using Int64 keys. When calling a method on the class
	/// itself (i.e. not on an interface), Int64 is used in case of ambiguity (for
	/// example, GetEnumerator() doesn't know if you want to decode the keys into
	/// Int32 or Int64, so it uses Int64.) When not accessing CPIntTrie through an
	/// interface, most methods accept any of the following integer types: Int16,
	/// UInt16, Int32, UInt32, Int64, UInt64.
	/// <para/>
	/// The trie does not choose the key length based on an integer's size (e.g.
	/// Int32 or Int64), rather it is chosen based on the key's magnitude. Keys
	/// ranging from -0x10000 to 0xFAFFFF are encoded most compactly (usually in one
	/// 4-byte cell); 40-bit keys as low as -0xFFFFFFFFFF and as high as
	/// 0xFFFFFFFFFF are usually encoded in 2 cells; and all larger keys require 3 
	/// cells.
	/// <para/>
	/// I say "usually" because normally CPSNode can hold 3 bytes per cell, but
	/// keys whose low-order byte is 0xFD, 0xFE or 0xFF require one extra cell.
	/// <para/>
	/// Negative 64-bit numbers can be held in the same trie as unsigned 64-bit 
	/// numbers; therefore, there is no way to represent all keys using a single
	/// primitive data type. The LongEnumerator returns Int64s, and you will find 
	/// that if a huge unsigned number like 0x8877665544332211 is in the trie, it 
	/// will come out as a negative Int64. To find out whether the long number is 
	/// signed or unsigned, call ContainsKey(Int64) - if it returns true, the
	/// number is signed, otherwise it is unsigned (so its signed representation
	/// was not found). If it is unsigned, convert the key returned by the
	/// enumerator to UInt64.
	/// </remarks>
	public class CPIntTrie<TValue> : CPTrie<TValue>, IDictionary<int, TValue>, IDictionary<long, TValue>
	{
		private static ScratchBuffer<byte[]> _intScratchBuffer;

		public CPIntTrie() { }
		public CPIntTrie(CPIntTrie<TValue> clone) : base(clone) { }

		public new int CountMemoryUsage(int sizeOfT) { return base.CountMemoryUsage(sizeOfT); }

		#region Private helper methods
		
		const int MaxKeyLen = 9;

		protected bool IsShortKey(int key)
		{
			return key >= -0x10000 && key < 0xFB0000;
		}
		protected bool IsShortKey(uint key)
		{
			return key < 0xFB0000;
		}
		protected bool IsLongKey(long key)
		{
			return key <= -0x10000000000 || key >= 0x10000000000;
		}
		protected bool IsLongKey(ulong key)
		{
			return key >= 0x10000000000;
		}
		protected KeyWalker EncodeShort(int key)
		{
			byte[] buf = _intScratchBuffer.Value;
			if (buf == null)
				buf = _intScratchBuffer.Value = new byte[MaxKeyLen];

			int key2 = key + 0x30000;
			Debug.Assert(key2 >= 0x20000 && key2 < 0xFE0000);
			buf[2] = (byte)key;
			buf[1] = (byte)(key2 >> 8);
			buf[0] = (byte)(key2 >> 16);
			return new KeyWalker(buf, 3);
		}
		protected KeyWalker EncodeMed(int key)
		{
			byte[] buf = _intScratchBuffer.Value;
			if (buf == null)
				buf = _intScratchBuffer.Value = new byte[MaxKeyLen];

			buf[5] = (byte)key;
			buf[4] = (byte)(key >> 8);
			buf[3] = (byte)(key >> 16);
			buf[2] = (byte)(key >> 24);
			if (key < 0)
			{
				Debug.Assert(key < -0x10000);
				buf[0] = 1;
				buf[1] = 0xFF;
			} else {
				Debug.Assert(key >= 0xFB0000);
				buf[0] = 0xFE;
				buf[1] = 0;
			}
			return new KeyWalker(buf, 6);
		}
		protected KeyWalker EncodeMed(long key)
		{
			uint key32 = (uint)key;

			byte[] buf = _intScratchBuffer.Value;
			if (buf == null)
				buf = _intScratchBuffer.Value = new byte[MaxKeyLen];

			buf[5] = (byte)key32;
			buf[4] = (byte)(key32 >> 8);
			buf[3] = (byte)(key32 >> 16);
			buf[2] = (byte)(key32 >> 24);
			buf[1] = (byte)(key >> 32);
			if (key < 0) {
				Debug.Assert(key < -0x10000);
				buf[0] = 1;
			} else {
				Debug.Assert(key >= 0xFB0000);
				buf[0] = 0xFE;
			}
			return new KeyWalker(buf, 6);
		}
		protected KeyWalker EncodeLong(long key)
		{
			Debug.Assert(key <= -0x10000000000 || key >= 0x10000000000);
			uint key32 = (uint)key;

			byte[] buf = _intScratchBuffer.Value;
			if (buf == null)
				buf = _intScratchBuffer.Value = new byte[MaxKeyLen];

			buf[8] = (byte)(key32);
			buf[7] = (byte)(key32 >> 8);
			buf[6] = (byte)(key32 >> 16);
			buf[5] = (byte)(key32 >> 24);
			buf[4] = (byte)(key >> 32);
			buf[3] = (byte)(key >> 40);
			buf[2] = (byte)(key >> 48);
			buf[1] = (byte)(key >> 56);
			buf[0] = (byte)(key < 0 ? 0 : 0xFF);

			return new KeyWalker(buf, 9);
		}
		protected KeyWalker EncodeLong(ulong key)
		{
			Debug.Assert(key >= 0x10000000000);
			uint key32 = (uint)key;

			byte[] buf = _intScratchBuffer.Value;
			if (buf == null)
				buf = _intScratchBuffer.Value = new byte[MaxKeyLen];

			buf[8] = (byte)(key32);
			buf[7] = (byte)(key32 >> 8);
			buf[6] = (byte)(key32 >> 16);
			buf[5] = (byte)(key32 >> 24);
			buf[4] = (byte)(key >> 32);
			buf[3] = (byte)(key >> 40);
			buf[2] = (byte)(key >> 48);
			buf[1] = (byte)(key >> 56);
			buf[0] = 0xFF;

			return new KeyWalker(buf, 9);
		}

		private void ThrowKeyAlreadyExists(long key)
		{
			throw new ArgumentException(Localize.From("Key already exists: ") + key.ToString());
		}

		private KeyWalker Encode(int key)
		{
			return IsShortKey(key) ? EncodeShort(key) : EncodeMed(key);
		}
		private KeyWalker Encode(uint key)
		{
			return IsShortKey(key) ? EncodeShort((int)key) : EncodeMed((long)key);
		}
		private KeyWalker Encode(long key)
		{
			int key32 = (int)key;
			if (key32 == key)
				return Encode(key32);
			else
				return IsLongKey(key) ? EncodeLong(key) : EncodeMed(key);
		}
		private KeyWalker Encode(ulong key)
		{
			uint key32 = (uint)key;
			if (key32 == key)
				return Encode(key32);
			else
				return IsLongKey(key) ? EncodeLong(key) : EncodeMed((long)key);
		}

		protected TValue GetValue(ref KeyWalker key, int keyI)
		{
			CPNode<TValue> head = Head;
			if (head != null)
			{
				TValue value = default(TValue);
				if (head.Set(ref key, ref value, ref head, CPMode.Find))
					return value;
			}
			throw new KeyNotFoundException(Localize.From("Key not found: ") + keyI.ToString());
		}
		protected TValue GetValue(ref KeyWalker key, long keyI)
		{
			CPNode<TValue> head = Head;
			if (head != null)
			{
				TValue value = default(TValue);
				if (head.Set(ref key, ref value, ref head, CPMode.Find))
					return value;
			}
			throw new KeyNotFoundException(Localize.From("Key not found: ") + keyI.ToString());
		}
		protected TValue GetValue(ref KeyWalker key, ulong keyI)
		{
			CPNode<TValue> head = Head;
			if (head != null)
			{
				TValue value = default(TValue);
				if (head.Set(ref key, ref value, ref head, CPMode.Find))
					return value;
			}
			throw new KeyNotFoundException(Localize.From("Key not found: ") + keyI.ToString());
		}

		internal static int DecodeInt(byte[] buf, int len)
		{
			if (len == 3)
			{
				Debug.Assert(buf[0] > 1 && buf[0] < 0xFE);
				return (buf[2] + (buf[1] << 8) + (buf[0] << 16)) - 0x30000;
			}
			if (len == 6)
			{
				Debug.Assert(buf[0] == 1 || buf[0] == 0xFE);
				return (buf[5] + (buf[4] << 8)) + ((buf[3] << 16) + (buf[2] << 24));
			}
			if (len == 9)
			{
				Debug.Assert(buf[0] == 0 || buf[0] == 0xFF);
				return (buf[8] + (buf[7] << 8)) + ((buf[6] << 16) + (buf[5] << 24));
			}
			throw new InvalidOperationException("CPIntTrie contains an invalid key");
		}
		internal static long DecodeLong(byte[] buf, int len)
		{
			if (len == 3)
			{
				Debug.Assert(buf[0] > 1 && buf[0] < 0xFE);
				return (buf[2] + (buf[1] << 8) + (buf[0] << 16)) - 0x30000;
			}
			if (len == 6)
			{
				Debug.Assert(buf[0] == 1 || buf[0] == 0xFE);
				long res = (buf[5] + (buf[4] << 8) + (buf[3] << 16))
					+ (((uint)buf[2] << 24) + ((long)buf[1] << 32));
				if (buf[0] <= 1)
					res |= unchecked((long)0xFFFFFF0000000000);
				return res;
			}
			if (len == 9)
			{
				Debug.Assert(buf[0] == 0 || buf[0] == 0xFF);
				return   (buf[8] + (buf[7] << 8) + (buf[6] << 16) + ((uint)buf[5] << 24)) +
				  ((long)(buf[4] + (buf[3] << 8) + (buf[2] << 16) + ((uint)buf[1] << 24)) << 32);
			}
			throw new InvalidOperationException("CPIntTrie contains an invalid key");
		}

		#endregion

		#region Members of IDictionary<int,TValue>, IDictionary<long,TValue>, etc.

		/// <summary>Adds the specified key-value pair only if the specified key is
		/// not already present in the trie.</summary>
		/// <param name="value">A value to associate with the specified key if the
		/// key does not already exist.</param>
		/// <returns>Returns true if the key-value pair was added or false if
		/// the key already existed. In the false case, the trie is not modified.</returns>
		public bool TryAdd(int key, TValue value)
		{
			KeyWalker kw = IsShortKey(key) ? EncodeShort(key) : EncodeMed(key);
			return !base.Set(ref kw, ref value, CPMode.Create);
		}
		/// <summary>Adds the specified key-value pair only if the specified key is
		/// not already present in the trie.</summary>
		/// <param name="value">On entry, value specifies the value to associate
		/// with the specified key, but if the key already exists, value is changed
		/// to the value associated with the existing key.</param>
		/// <returns>Returns true if the key-value pair was added or false if
		/// the key already existed. In the false case, the trie is not modified.</returns>
		public bool TryAdd(int key, ref TValue value)
		{
			KeyWalker kw = IsShortKey(key) ? EncodeShort(key) : EncodeMed(key);
			return !base.Set(ref kw, ref value, CPMode.Create);
		}
		public bool TryAdd(uint key, TValue value)
		{
			KeyWalker kw = Encode(key);
			return !base.Set(ref kw, ref value, CPMode.Create);
		}
		public bool TryAdd(uint key, ref TValue value)
		{
			KeyWalker kw = Encode(key);
			return !base.Set(ref kw, ref value, CPMode.Create);
		}
		public bool TryAdd(short key, TValue value)
		{
			KeyWalker kw = EncodeShort(key);
			return !base.Set(ref kw, ref value, CPMode.Create);
		}
		public bool TryAdd(short key, ref TValue value)
		{
			KeyWalker kw = EncodeShort(key);
			return !base.Set(ref kw, ref value, CPMode.Create);
		}
		public bool TryAdd(ushort key, TValue value)
		{
			KeyWalker kw = EncodeShort(key);
			return !base.Set(ref kw, ref value, CPMode.Create);
		}
		public bool TryAdd(ushort key, ref TValue value)
		{
			KeyWalker kw = EncodeShort(key);
			return !base.Set(ref kw, ref value, CPMode.Create);
		}
		public bool TryAdd(long key, TValue value)
		{
			KeyWalker kw = Encode(key);
			return !base.Set(ref kw, ref value, CPMode.Create);
		}
		public bool TryAdd(long key, ref TValue value)
		{
			KeyWalker kw = Encode(key);
			return !base.Set(ref kw, ref value, CPMode.Create);
		}
		public bool TryAdd(ulong key, TValue value)
		{
			KeyWalker kw = Encode(key);
			return !base.Set(ref kw, ref value, CPMode.Create);
		}
		public bool TryAdd(ulong key, ref TValue value)
		{
			KeyWalker kw = Encode(key);
			return !base.Set(ref kw, ref value, CPMode.Create);
		}

		/// <summary>Adds the specified key-value pair to the trie, throwing an
		/// exception if the key is already present.</summary>
		public void Add(int key, TValue value)
		{
			if (!TryAdd(key, value))
				ThrowKeyAlreadyExists(key);
		}
		public void Add(uint key, TValue value)
		{
			if (!TryAdd(key, value))
				ThrowKeyAlreadyExists(key);
		}
		public void Add(short key, TValue value)
		{
			if (!TryAdd(key, value))
				ThrowKeyAlreadyExists(key);
		}
		public void Add(ushort key, TValue value)
		{
			if (!TryAdd(key, value))
				ThrowKeyAlreadyExists(key);
		}
		public void Add(long key, TValue value)
		{
			if (!TryAdd(key, value))
				ThrowKeyAlreadyExists(key);
		}
		public void Add(ulong key, TValue value)
		{
			if (!TryAdd(key, value))
				throw new ArgumentException(Localize.From("Key already exists: ") + key.ToString());
		}
		
		/// <summary>Searches for the specified key, returning true if it is
		/// present in the trie.</summary>
		public bool ContainsKey(int key)
		{
			TValue _;
			return TryGetValue(key, out _);
		}
		public bool ContainsKey(uint key)
		{
			TValue _;
			return TryGetValue(key, out _);
		}
		public bool ContainsKey(short key)
		{
			TValue _;
			return TryGetValue(key, out _);
		}
		public bool ContainsKey(ushort key)
		{
			TValue _;
			return TryGetValue(key, out _);
		}
		public bool ContainsKey(long key)
		{
			TValue _;
			return TryGetValue(key, out _);
		}
		public bool ContainsKey(ulong key)
		{
			TValue _;
			return TryGetValue(key, out _);
		}

		/// <summary>Removes the specified key and associated value, returning true
		/// if the entry was found and removed.</summary>
		public bool Remove(int key)
		{
			KeyWalker kw = Encode(key);
			return base.Remove(ref kw);
		}
		public bool Remove(uint key)
		{
			KeyWalker kw = Encode(key);
			return base.Remove(ref kw);
		}
		public bool Remove(short key)
		{
			KeyWalker kw = EncodeShort(key);
			return base.Remove(ref kw);
		}
		public bool Remove(ushort key)
		{
			KeyWalker kw = EncodeShort(key);
			return base.Remove(ref kw);
		}
		public bool Remove(long key)
		{
			KeyWalker kw = Encode(key);
			return base.Remove(ref kw);
		}
		public bool Remove(ulong key)
		{
			KeyWalker kw = Encode(key);
			return base.Remove(ref kw);
		}

		/// <summary>Removes the specified key and associated value, returning true
		/// if the entry was found and removed.</summary>
		/// <param name="key">Key to remove.</param>
		/// <param name="oldValue">If the key is found, the associated value is
		/// assigned to this parameter. Otherwise, this parameter is not changed.</param>
		public bool Remove(int key, ref TValue oldValue)
		{
			KeyWalker kw = Encode(key);
			return base.Remove(ref kw, ref oldValue);
		}
		public bool Remove(uint key, ref TValue oldValue)
		{
			KeyWalker kw = Encode(key);
			return base.Remove(ref kw, ref oldValue);
		}
		public bool Remove(short key, ref TValue oldValue)
		{
			KeyWalker kw = EncodeShort(key);
			return base.Remove(ref kw, ref oldValue);
		}
		public bool Remove(ushort key, ref TValue oldValue)
		{
			KeyWalker kw = EncodeShort(key);
			return base.Remove(ref kw, ref oldValue);
		}
		public bool Remove(long key, ref TValue oldValue)
		{
			KeyWalker kw = Encode(key);
			return base.Remove(ref kw, ref oldValue);
		}
		public bool Remove(ulong key, ref TValue oldValue)
		{
			KeyWalker kw = Encode(key);
			return base.Remove(ref kw, ref oldValue);
		}

		/// <summary>Finds the specified key and gets its associated value,
		/// returning true if the key was found.</summary>
		public bool TryGetValue(int key, out TValue value)
		{
			KeyWalker kw = Encode(key);
			value = default(TValue);
			return base.Find(ref kw, ref value);
		}
		public bool TryGetValue(uint key, out TValue value)
		{
			KeyWalker kw = Encode(key);
			value = default(TValue);
			return base.Find(ref kw, ref value);
		}
		public bool TryGetValue(short key, out TValue value)
		{
			KeyWalker kw = EncodeShort(key);
			value = default(TValue);
			return base.Find(ref kw, ref value);
		}
		public bool TryGetValue(ushort key, out TValue value)
		{
			KeyWalker kw = EncodeShort(key);
			value = default(TValue);
			return base.Find(ref kw, ref value);
		}
		public bool TryGetValue(long key, out TValue value)
		{
			KeyWalker kw = Encode(key);
			value = default(TValue);
			return base.Find(ref kw, ref value);
		}
		public bool TryGetValue(ulong key, out TValue value)
		{
			KeyWalker kw = Encode(key);
			value = default(TValue);
			return base.Find(ref kw, ref value);
		}
		
		/// <summary>Finds the specified key and returns its associated value. If 
		/// the key did not exist, returns defaultValue instead.</summary>
		public TValue this[int key, TValue defaultValue]
		{
			get {
				KeyWalker kw = Encode(key);
				base.Find(ref kw, ref defaultValue);
				return defaultValue;
			}
		}
		public TValue this[uint key, TValue defaultValue]
		{
			get {
				KeyWalker kw = Encode(key);
				base.Find(ref kw, ref defaultValue);
				return defaultValue;
			}
		}
		public TValue this[short key, TValue defaultValue]
		{
			get {
				KeyWalker kw = EncodeShort(key);
				base.Find(ref kw, ref defaultValue);
				return defaultValue;
			}
		}
		public TValue this[ushort key, TValue defaultValue]
		{
			get {
				KeyWalker kw = EncodeShort(key);
				base.Find(ref kw, ref defaultValue);
				return defaultValue;
			}
		}
		public TValue this[long key, TValue defaultValue]
		{
			get {
				KeyWalker kw = Encode(key);
				base.Find(ref kw, ref defaultValue);
				return defaultValue;
			}
		}
		public TValue this[ulong key, TValue defaultValue]
		{
			get {
				KeyWalker kw = Encode(key);
				base.Find(ref kw, ref defaultValue);
				return defaultValue;
			}
		}

		public TValue this[int key]
		{
			get {
				KeyWalker kw = Encode(key);
				return GetValue(ref kw, key);
			}
			set {
				KeyWalker kw = Encode(key);
				base.Set(ref kw, ref value, CPMode.Set | CPMode.Create);
			}
		}
		public TValue this[uint key]
		{
			get {
				KeyWalker kw = Encode(key);
				return GetValue(ref kw, key);
			}
			set {
				KeyWalker kw = Encode(key);
				base.Set(ref kw, ref value, CPMode.Set | CPMode.Create);
			}
		}
		public TValue this[short key]
		{
			get {
				KeyWalker kw = EncodeShort(key);
				return GetValue(ref kw, key);
			}
			set {
				KeyWalker kw = Encode(key);
				base.Set(ref kw, ref value, CPMode.Set | CPMode.Create);
			}
		}
		public TValue this[ushort key]
		{
			get {
				KeyWalker kw = EncodeShort(key);
				return GetValue(ref kw, key);
			}
			set {
				KeyWalker kw = Encode(key);
				base.Set(ref kw, ref value, CPMode.Set | CPMode.Create);
			}
		}
		public TValue this[long key]
		{
			get {
				KeyWalker kw = Encode(key);
				return GetValue(ref kw, key);
			}
			set {
				KeyWalker kw = Encode(key);
				base.Set(ref kw, ref value, CPMode.Set | CPMode.Create);
			}
		}
		public TValue this[ulong key]
		{
			get {
				KeyWalker kw = Encode(key);
				return GetValue(ref kw, key);
			}
			set {
				KeyWalker kw = Encode(key);
				base.Set(ref kw, ref value, CPMode.Set | CPMode.Create);
			}
		}

		public ICollection<long> Keys
		{
			get { return new KeyCollection<long, TValue>(this); }
		}
		ICollection<int> IDictionary<int,TValue>.Keys
		{
			get { return new KeyCollection<int, TValue>(this); }
		}
		public ICollection<TValue> Values
		{
			get { return new CPValueCollection<TValue>(this); }
		}

		public void Add(KeyValuePair<int, TValue> item)
		{
			Add(item.Key, item.Value);
		}
		public void Add(KeyValuePair<long, TValue> item)
		{
			Add(item.Key, item.Value);
		}

		public new void Clear()
		{
			base.Clear();
		}

		public bool Contains(KeyValuePair<int, TValue> item)
		{
			TValue value;
			return TryGetValue(item.Key, out value)
				&& DefaultComparer.Compare(value, item.Value) == 0;
		}
		public bool Contains(KeyValuePair<long, TValue> item)
		{
			TValue value;
			return TryGetValue(item.Key, out value)
				&& DefaultComparer.Compare(value, item.Value) == 0;
		}

		public void CopyTo(KeyValuePair<int, TValue>[] array, int arrayIndex)
		{
			foreach (KeyValuePair<int, TValue> pair in (IDictionary<int, TValue>)this)
				array[arrayIndex] = pair;
		}
		public void CopyTo(KeyValuePair<long, TValue>[] array, int arrayIndex)
		{
			foreach (KeyValuePair<long, TValue> pair in this)
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

		public bool Remove(KeyValuePair<int, TValue> item)
		{
			KeyWalker kw = Encode(item.Key);
			KeyWalker kw2 = kw;
			TValue value = default(TValue);
			if (Find(ref kw, ref value) && DefaultComparer.Compare(value, item.Value) == 0)
				return Remove(ref kw2, ref value);
			return false;
		}
		public bool Remove(KeyValuePair<long, TValue> item)
		{
			KeyWalker kw = Encode(item.Key);
			KeyWalker kw2 = kw;
			TValue value = default(TValue);
			if (Find(ref kw, ref value) && DefaultComparer.Compare(value, item.Value) == 0)
				return Remove(ref kw2, ref value);
			return false;
		}

		#endregion

		#region Members of IEnumerable<KeyValuePair<int,TValue>>, IEnumerable<KeyValuePair<long,TValue>>

		public LongEnumerator GetEnumerator()
		{
			return new LongEnumerator(this);
		}
		public IntEnumerator GetIntEnumerator()
		{
			return new IntEnumerator(this);
		}
		IEnumerator<KeyValuePair<int, TValue>> IEnumerable<KeyValuePair<int, TValue>>.GetEnumerator()
		{
			return new IntEnumerator(this);
		}
		IEnumerator<KeyValuePair<long, TValue>> IEnumerable<KeyValuePair<long, TValue>>.GetEnumerator()
		{
			return new LongEnumerator(this);
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return new LongEnumerator(this);
		}

		#endregion

		#region Find methods

		public IntEnumerator FindAtLeast(int key)
		{
			KeyWalker kw = Encode(key);
			IntEnumerator e = new IntEnumerator(this);
			base.Find(ref kw, e);
			return e;
		}
		public LongEnumerator FindAtLeast(long key)
		{
			KeyWalker kw = Encode(key);
			LongEnumerator e = new LongEnumerator(this);
			base.Find(ref kw, e);
			return e;
		}
		public LongEnumerator FindAtLeast(ulong key)
		{
			KeyWalker kw = Encode(key);
			LongEnumerator e = new LongEnumerator(this);
			base.Find(ref kw, e);
			return e;
		}
		
		public IntEnumerator FindExact(int key)
		{
			KeyWalker kw = Encode(key);
			IntEnumerator e = new IntEnumerator(this);
			if (!base.Find(ref kw, e))
				return null;
			Debug.Assert(e.IsValid);
			return e;
		}
		public LongEnumerator FindExact(long key)
		{
			KeyWalker kw = Encode(key);
			LongEnumerator e = new LongEnumerator(this);
			if (!base.Find(ref kw, e))
				return null;
			Debug.Assert(e.IsValid);
			return e;
		}
		public LongEnumerator FindExact(ulong key)
		{
			KeyWalker kw = Encode(key);
			LongEnumerator e = new LongEnumerator(this);
			if (!base.Find(ref kw, e))
				return null;
			Debug.Assert(e.IsValid);
			return e;
		}

		public bool Find(int key, out IntEnumerator e)
		{
			KeyWalker kw = Encode(key);
			e = new IntEnumerator(this);
			return base.Find(ref kw, e);
		}
		public bool Find(long key, out LongEnumerator e)
		{
			KeyWalker kw = Encode(key);
			e = new LongEnumerator(this);
			return base.Find(ref kw, e);
		}
		public bool Find(ulong key, out LongEnumerator e)
		{
			KeyWalker kw = Encode(key);
			e = new LongEnumerator(this);
			return base.Find(ref kw, e);
		}

		#endregion

		public CPIntTrie<TValue> Clone()
			{ return new CPIntTrie<TValue>(this); }

		public bool IsEmpty { get { return base.Count == 0; } }

		#region Enumerators

		public class IntEnumerator : CPEnumerator<TValue>, IEnumerator<KeyValuePair<int, TValue>>
		{
			internal protected IntEnumerator(CPIntTrie<TValue> trie) : base(trie) { }

			public new KeyValuePair<int, TValue> Current
			{
				get {
					return new KeyValuePair<int, TValue>(CurrentKey, CurrentValue);
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
			public new int CurrentKey
			{
				get {
					return DecodeInt(Key.Buffer, Key.Offset + Key.Left);
				}
			}
		}

		public class LongEnumerator : CPEnumerator<TValue>, IEnumerator<KeyValuePair<long, TValue>>
		{
			internal protected LongEnumerator(CPIntTrie<TValue> trie) : base(trie) {}

			public new KeyValuePair<long, TValue> Current
			{
				get {
					return new KeyValuePair<long, TValue>(CurrentKey, CurrentValue);
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
			public new long CurrentKey
			{
				get {
					return DecodeLong(Key.Buffer, Key.Offset + Key.Left);
				}
			}
		}

		#endregion
	}
}
