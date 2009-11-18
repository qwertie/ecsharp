using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Loyc.Utilities.Judish
{
	public abstract class JKeyEnumerator
	{
		/// <summary>Returns the next 'numBytes' bytes of the key in the form of a
		/// uint.</summary>
		/// <param name="numBytes">Number of bytes to get; must be between 1 and 4.</param>
		/// <returns>One or more bytes of the remaining key, with higher-order
		/// bytes in more significant bytes of the return value (in other words, 
		/// big-endian format). If fewer than four bytes were requested, then the 
		/// most significant (4-numBytes) bytes of the return value must be zero.
		/// </returns>
		/// <remarks>
		/// Example: suppose the key is the string "STRING", and the caller first
		/// calls GetBytes(4) followed by GetBytes(2). The first return value
		/// should be
		/// <code>
		/// ((int)'S' &lt;&lt; 24) | ((int)'T' &lt;&lt; 16) | ((int)'R' &lt;&lt; 8) | (int)'I'
		/// </code>
		/// and the second should be
		/// <code>
		/// ((int)'N' &lt;&lt; 8) | (int)'G'
		/// </code>
		/// Judish will never ask for more bytes than the key length, but incorrect 
		/// code might do so. Therefore, if the caller requests more bytes than the 
		/// key has, throw an exception or (if speed is critical) use Debug.Assert.
		/// </remarks>
		public abstract uint GetBytes(int numBytes);
	}

	/// <summary>Base class of all key serializers for Judish collections.</summary>
	/// <typeparam name="T">Type of item to serialize and deserialize.</typeparam>
	/// <remarks>
	/// Judish collections do not contain keys in their original form. Instead, 
	/// Judish understands a key as a sequence of bytes, and a JKeySerializer 
	/// adapter is required to convert between a key type and a byte sequence.
	/// To avoid having to allocate memory just to look up a key in a dictionary,
	/// keys are not serialized to a byte array; instead they are returned by a 
	/// specialized enumerator, JKeyEnumerator, that can return one to four 
	/// bytes per call (depending on how much of the key the caller needs at a 
	/// time).
	/// <para/>
	/// JKeySerializer is not an interface because interfaces calls have slightly 
	/// higher overhead than virtual function calls. Performance is Judish's claim 
	/// to fame, so we use an abstract class instead. The interface is also slightly
	/// unnatural in order to minimize the number of virtual function calls 
	/// required to decode or encode a T object.
	/// <para/>
	/// Usage: to serialize an item t, call BeginSerialize, then call GetBytes one 
	/// or more times to retrieve the key as a sequence of bytes. To deserialize an 
	/// item, obtain the serialized form as a JKeyEnumerator, then call 
	/// Deserialize(), passing it the JKeyEnumerator and key length.
	/// </remarks>
	public abstract class JKeySerializer<T> : JKeyEnumerator
	{
		/// <summary>Begins the process of converting a key to a sequence of bytes.</summary>
		/// <returns>Returns the length of the key when it is converted to a byte 
		/// sequence.</returns>
		/// <remarks>After BeginSerialize(), call GetBytes() to get the serialized
		/// form.</remarks>
		public abstract int BeginSerialize(T key);

		/// <summary>Converts a byte sequence (in the form of a JKeyEnumerator) 
		/// that represents a key back into a .NET object of type T.
		/// </summary>
		/// <param name="key">A source of bytes that represent the key</param>
		/// <param name="keyLength">Length of the key, in bytes</param>
		/// <returns>A deserialized T object.</returns>
		/// <exception cref="ArgumentException">
		/// The specified key could not be interpreted as a T object.</exception>
		public abstract T Deserialize(JKeyEnumerator key, int keyLength);
	}

	/// <summary>
	/// Judish uses KeyShifter internally to keep upcoming bytes of a key shifted 
	/// into the most-significant bytes of a uint.
	/// </summary>
	/// <remarks>
	/// Derived classes of JudishInternal pass keys into Judish using a KeyShifter 
	/// instead of using JKeyEnumerator directly; this allows keys that are 4 
	/// bytes or less to be constructed without any virtual method calls (just call 
	/// the constructor that takes uint).
	/// </remarks>
	public struct KeyShifter
	{
		uint _keyPart;  // Left shifted so low bytes are 0 if _bytesLeft < 4
		int _bytesLeft; // Total of enumerated-but-undiscarded and unenumerated bytes.
		JKeyEnumerator _e;

		public KeyShifter(JKeyEnumerator e, int keyLength)
		{
			_e = e;
			_bytesLeft = keyLength;
			if (4 - _bytesLeft > 0)
				_keyPart = e.GetBytes(_bytesLeft) << ((4 - _bytesLeft) << 3);
			else
				_keyPart = e.GetBytes(4);
		}
		public KeyShifter(uint key, int keyLength)
		{
			Debug.Assert(keyLength <= 4);
			_keyPart = key;
			_bytesLeft = keyLength;
			_e = null;
		}
		public KeyShifter(uint key)
		{
			_keyPart = key;
			_bytesLeft = 4;
			_e = null;
		}
		
		public int BytesLeft { get { return _bytesLeft; } }
		public uint KeyPart { get { return _keyPart; } }

		public void Advance(int bytes)
		{
			Debug.Assert(bytes <= 4);
			Debug.Assert(bytes <= _bytesLeft);
			_keyPart <<= (bytes << 3);
			int bytesNext = _bytesLeft - 4;
			if (bytesNext > 0) {
				if (bytesNext < bytes)
					_keyPart |= _e.GetBytes(bytesNext) << ((bytes - bytesNext) << 3);
				else if (bytes > 0)
					_keyPart |= _e.GetBytes(bytes);
			}
			_bytesLeft -= bytes;
		}
	}
}
