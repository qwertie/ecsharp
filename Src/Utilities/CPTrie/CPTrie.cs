/*
	CPTrie library: Copyright 2010 by David Piepgrass

	This library is free software: you can redistribute it and/or modify it 
	it under the terms of the GNU Lesser General Public License as published 
	by the Free Software Foundation, either version 3 of the License, or (at 
	your option) any later version. It is provided without ANY warranties.
	Please note that it is fairly complex. Therefore, it may contain bugs 
	despite my best efforts to test it.

	If you did not receive a copy of the License with this library, you can 
	find it at http://www.gnu.org/licenses/lgpl.html
*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Loyc.Runtime;
using Loyc.Utilities.CPTrie;

namespace Loyc.Utilities
{
	/// <summary>Compact patricia tree class that stores keys as byte arrays.
	/// This class is intended to be use as a base class; a derived class can
	/// give meaning to the byte arrays, e.g. CPStringTrie encodes strings into
	/// byte arrays so they can be placed in the trie.</summary>
	/// <typeparam name="T">Type of values to be associated with the keys. CPTrie
	/// can save memory if many or all values are null; therefore, if you wish
	/// to store a set rather than a dictionary, set T=object and associate null
	/// with every key.</typeparam>
	public class CPTrie<T>
	{
		public CPTrie() { }
		public CPTrie(CPTrie<T> copy)
		{
			_head = copy._head.CloneAndOptimize();
			_count = copy._count;
		}
		
		private CPNode<T> _head;
		internal CPNode<T> Head { get { return _head; } }
		private int _count;

		protected internal int Count { get { return _count; } }

		protected static Comparer<T> DefaultComparer = Comparer<T>.Default;
		private static ScratchBuffer<byte[]> _stringScratchBuffer;
		private const int StringScratchBufferLen = 48;
		
		/// <summary>Converts a string to a sequence of bytes suitable for use in 
		/// the trie. For speed, a simplified UTF-8 encoding is used, where 
		/// surrogate pairs are not specially handled.</summary>
		/// <param name="key">Key to convert to bytes.</param>
		/// <returns>The key encoded in bytes.</returns>
		protected internal static KeyWalker StringToBytes(string key)
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
		/// <remarks>The buffer length is not relevant, as this method may store 
		/// the key in a scratch buffer that is longer than the key; therefore
		/// the second parameter specifies the length.</remarks>
		protected internal static string BytesToString(byte[] key, int keyLength)
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

		protected bool Find(ref KeyWalker key, CPEnumerator<T> e)
		{
			e.Reset();
			if (_head != null) {
				if (_head.Find(ref key, e))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Retrieves the value associated with the specified key; does nothing if
		/// the key does not exist.
		/// </summary>
		/// <returns>Returns true if the key was found.</returns>
		protected bool Find(ref KeyWalker key, ref T value)
		{
			if (_head != null)
				return _head.Set(ref key, ref value, ref _head, CPMode.Find);
			return false;
		}
		protected bool ContainsKey(ref KeyWalker key)
		{
			T dummy = default(T);
			if (_head != null)
				return _head.Set(ref key, ref dummy, ref _head, CPMode.Find);
			return false;
		}

		/// <summary>
		/// Associates the specified value with the specified key.
		/// </summary>
		/// <param name="key">A key to find or create; if key.Offset > 0, bytes
		/// before that offset are ignored.</param>
		/// <param name="value">Value to assign to the node, depending on the value
		/// of mode. On return, value is set to the previous value for the given key.</param>
		/// <param name="mode">Specifies whether to create an entry if the key is
		/// not present, and whether to change an existing entry. If mode is Find,
		/// Set() only retrieves an existing value; it does not change the trie.</param>
		/// <returns>Returns true if the specified key already existed and false if 
		/// it did not.</returns>
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
				_head = new CPSNode<T>(ref key, value);
				_count = 1;
			}
			return false;
		}

		/// <summary>
		/// Removes the specified key and associated value.
		/// </summary>
		/// <param name="key">Key to find; if key.Offset > 0, bytes before that 
		/// offset are ignored.</param>
		/// <param name="value">If the key was found, its associated value is
		/// stored in this parameter; otherwise, the parameter is left unchanged.</param>
		/// <returns>Returns true if the specified key was found and removed.</returns>
		protected bool Remove(ref KeyWalker key, ref T value)
		{
			if (_head != null) {
				if (_head.Remove(ref key, ref value, ref _head))
				{
					_count--;
					Debug.Assert((_count == 0) == (_head == null));
					Debug.Assert(_head == null || _head.LocalCount <= _count);
					return true;
				}
			}
			return false;
		}
		protected bool Remove(ref KeyWalker key)
		{
			T dummy = default(T);
			if (_head != null)
				if (_head.Remove(ref key, ref dummy, ref _head))
				{
					_count--;
					Debug.Assert((_count == 0) == (_head == null));
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
		/// sizeof(T), as it would force the code to be marked "unsafe".</param>
		/// <returns>Estimated number of bytes used by this object</returns>
		protected int CountMemoryUsage(int sizeOfT)
		{
			int size = 16;
			if (_head != null)
				size += _head.CountMemoryUsage(sizeOfT);
			return size;
		}

		protected internal CPEnumerator<T> ValueEnumerator()
		{
			return new CPEnumerator<T>(this);
		}
	}

	/// <summary>Provides read-only access to the values of a CPTrie.</summary>
	/// <typeparam name="T">Type of values in the collection</typeparam>
	public class CPValueCollection<T> : ICollection<T>
	{
		CPTrie<T> _trie;
		public CPValueCollection(CPTrie<T> trie) { _trie = trie; }

		#region ICollection<T> Members

		public void Add(T item)    { throw new NotSupportedException(); }
		public void Clear()        { throw new NotSupportedException(); }
		public bool Remove(T item) { throw new NotSupportedException(); }
		public bool IsReadOnly     { get { return true; } }
		public int Count           { get { return _trie.Count; } }

		public bool Contains(T item)
		{
			EqualityComparer<T> comp = EqualityComparer<T>.Default;
			foreach (T value in this)
				if (comp.Equals(value, item))
					return true;
			return false;
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			if (arrayIndex < 0)
				throw new ArgumentOutOfRangeException();
			if (arrayIndex + _trie.Count > array.Length)
				throw new ArgumentException();
			foreach (T value in this)
				array[arrayIndex++] = value;
		}

		public CPEnumerator<T> GetEnumerator()
		{
			return _trie.ValueEnumerator();
		}
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}
}
