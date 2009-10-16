using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Loyc.Utilities.Judish
{
	public abstract class JKeyEnumerator
	{
		/// <summary>Starts or restarts key enumeration and returns the length of 
		/// the key, in bytes.</summary>
		public abstract int Start();
		/// <summary>Returns the next 'numBytes' bytes of the key in the form of a
		/// uint.</summary>
		/// <param name="numBytes">Number of bytes to get; must be between 1 and 4.</param>
		/// <returns>One or more bytes of the remaining key, with higher-order
		/// bytes in more significant bytes of the return value. If fewer than four
		/// bytes were requested, then the most significant (4-numBytes) bytes of
		/// the return value must be zero.
		/// </returns>
		/// <remarks>
		/// Example: suppose the key is the string "STRING", and the caller first
		/// calls NextBytes(4) followed by NextBytes(2). The first return value
		/// should be
		/// <code>
		/// ((int)'S' &lt;&lt; 24) | ((int)'T' &lt;&lt; 16) | ((int)'R' &lt;&lt; 8) | (int)'I'
		/// </code>
		/// and the second should be
		/// <code>
		/// ((int)'N' &lt;&lt; 8) | (int)'G'
		/// </code>
		/// Judish will never ask for more bytes than the key length.
		/// </remarks>
		public abstract uint NextBytes(int numBytes);
	}

	public struct KeyShifter
	{
		uint _keyPart;
		int _bytesLeft; // Total of enumerated and unenumerated bytes.
		JKeyEnumerator _e;

		public KeyShifter(JKeyEnumerator e)
		{
			_e = e;
			_bytesLeft = _e.Start();
			if (4 - _bytesLeft > 0)
				_keyPart = e.NextBytes(_bytesLeft) << ((4 - _bytesLeft) << 3);
			else
				_keyPart = e.NextBytes(4);
		}
		public KeyShifter(uint key)
		{
			_keyPart = key;
			_bytesLeft = 4;
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
					_keyPart |= _e.NextBytes(bytesNext) << ((bytes - bytesNext) << 3);
				else
					_keyPart |= _e.NextBytes(bytes);
			}
			_bytesLeft -= bytes;
		}
	}
}
