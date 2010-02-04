using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Loyc.Runtime;
using Loyc.Utilities.CPTrie;

namespace Loyc.Utilities
{
	public class CPTrie<T>
	{
		CPNode<T> _head;
		int _count;

		protected int Count { get { return _count; } }

		protected static Comparer<T> DefaultComparer = Comparer<T>.Default;
		private static ScratchBuffer<byte[]> _stringScratchBuffer;
		private const int StringScratchBufferLen = 48;
		
		/// <summary>Converts a string to a sequence of bytes suitable for use in 
		/// the trie. For speed, a simplified UTF-8 encoding is used, where 
		/// surrogate pairs are not specially handled.</summary>
		/// <param name="keyLength">Length of the output. The array length is not 
		/// relevant, as this method may store the key in a scratch buffer that is 
		/// longer than the key.</param>
		/// <returns>The key encoded in bytes.</returns>
		protected static KeyWalker StringToBytes(string key)
		{
			int outSize = key.Length;
			byte[] buf = _stringScratchBuffer.Value;

			if (outSize > StringScratchBufferLen/3) {
				// Need to compute exact length if the scratch buffer might be too small
				for (int i = 0; i < key.Length; i++) {
					int c = (int)key[i];
					if (c >= 0x80)
						outSize += (c >= (1 << 11) ? 2 : 1);
				}
				if (outSize > StringScratchBufferLen)
					buf = new byte[outSize];
			}
			if (buf == null)
				_stringScratchBuffer.Value = buf = new byte[StringScratchBufferLen];
			
			int B = 0;
			for (int i = 0; i < key.Length; i++) {
				int c = (int)key[i];
				if (c < 0x80) {
					buf[B++] = (byte)c;
				} else if (c < (1 << 11)) {
					buf[B++] = (byte)((c >> 6) | 0xC0);
					buf[B++] = (byte)((c & 0x3F) | 0x80);
				} else {
					buf[B++] = (byte)((c >> 12) | 0xE0);
					buf[B++] = (byte)(((c >> 6) & 0x3F) | 0x80);
					buf[B++] = (byte)((c & 0x3F) | 0x80);
				}
			}
			
			Debug.Assert(outSize <= StringScratchBufferLen/3 || outSize == B);
			return new KeyWalker(buf, B);
		}
		
		/// <summary>Converts a sequence of bytes (key[0..keyLength-1]) that was 
		/// previously encoded with StringToBytes to a string</summary>
		protected static string BytesToString(byte[] key, int keyLength)
		{
			if (keyLength <= 1) {
				if (keyLength == 0)
					return string.Empty;
				return ((char)key[0]).ToString();
			}
			return BytesToStringBuilder(key, keyLength).ToString();
		}

		protected static StringBuilder BytesToStringBuilder(byte[] key, int keyLength)
		{
			StringBuilder sb = new StringBuilder(keyLength);
			for (int B = 0; B < keyLength; B++)
			{
				byte k = key[B];
				if (k < 0x80) {
					sb.Append((char)k);
				} else if (k < 0xE0) {
					Debug.Assert(k >= 0xC2);
					byte k2 = key[++B];
					Debug.Assert(k2 >= 0x80 && k2 <= 0xBF);
					sb.Append((char)(((k & 0x1F) << 6) + (k2 & 0x3F)));
				} else {
					Debug.Assert(k < 0xF0);
					byte k2 = key[++B];
					byte k3 = key[++B];
					Debug.Assert(k2 >= 0x80 && k2 <= 0xBF);
					Debug.Assert(k3 >= 0x80 && k3 <= 0xBF);
					sb.Append((char)(((k & 0xF) << 12) + ((k2 & 0x3F) << 6) + (k2 & 0x3F)));
				}
			}
			return sb;
		}

		protected bool Find(ref KeyWalker key, CPEnumerator e)
		{
			if (_head != null) {
				if (_head.Find(ref key, e))
					return true;
				e.Normalize();
			}
			return false;
		}

		protected bool Find(ref KeyWalker key, ref T value)
		{
			if (_head != null)
				return _head.Set(ref key, ref value, ref _head, CPMode.Find);
			return false;
		}

		protected bool Set(ref KeyWalker key, ref T value, CPMode mode)
		{
			if (_head != null) {
				bool existed = _head.Set(ref key, ref value, ref _head, mode);
				if (!existed && (mode & CPMode.Create) != (CPMode)0)
					_count++;
				return existed;
			}
			else if ((mode & CPMode.Create) != (CPMode)0)
			{
				Debug.Assert(_count == 0);
				_head = new CPLinear<T>(ref key, value);
				_count = 1;
			}
			return false;
		}
		
		protected bool Remove(ref KeyWalker key, ref T value)
		{
			if (_head != null)
				if (_head.Remove(ref key, ref value, ref _head))
				{
					_count--;
					return true;
				}
			return false;
		}

		protected void Clear()
		{
			_head = null;
			_count = 0;
		}

		/// <summary>Calculates the memory usage of this object, assuming a 32-bit
		/// architecture.</summary>
		/// <param name="sizeOfT">Size of data type T. CountMemoryUsage doesn't use
		/// sizeof(T), as it would force the code to be marked "unsafe".
		/// <returns>Estimated number of bytes used by this object</returns>
		protected int CountMemoryUsage(int sizeOfT)
		{
			int size = 16;
			if (_head != null)
				size += _head.CountMemoryUsage(sizeOfT);
			return size;
		}
	}

	public class CPStringTrie<TValue> : CPTrie<TValue>, IDictionary<string, TValue>
	{
		public int CountMemoryUsage(int sizeOfT) { return base.CountMemoryUsage(sizeOfT); }

		#region IDictionary<string,TValue> Members

		public void Add(string key, TValue value)
		{
			KeyWalker kw = StringToBytes(key);
			if (base.Set(ref kw, ref value, CPMode.Create))
				throw new ArgumentException(Localize.From("Key already exists: ") + key);
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

		public bool TryGetValue(string key, out TValue value)
		{
			KeyWalker kw = StringToBytes(key);
			value = default(TValue);
			return base.Find(ref kw, ref value);
		}
		public TValue TryGetValue(string key, TValue defaultValue)
		{
			KeyWalker kw = StringToBytes(key);
			base.Find(ref kw, ref defaultValue);
			return defaultValue;
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

		public ICollection<string> Keys
		{
			get { throw new NotImplementedException(); }
		}
		public ICollection<TValue> Values
		{
			get { throw new NotImplementedException(); }
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
			throw new NotImplementedException();
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
			if (Find(ref kw2, ref value) && DefaultComparer.Compare(value, item.Value) == 0)
				return Remove(ref kw, ref value);
			return false;
		}

		#endregion

		#region IEnumerable<KeyValuePair<string,TValue>> Members

		public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
