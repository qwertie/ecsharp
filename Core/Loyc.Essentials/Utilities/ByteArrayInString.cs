using Loyc.Collections;
using Loyc.Collections.MutableListExtensionMethods;
using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc
{
	/// <summary>Encodes and decodes BAIS (Byte Array In String) encoding,
	/// which preserves runs of ASCII characters unchanged. This encoding is
	/// useful for debugging (since ASCII runs are visible) and for conversion 
	/// of bytes to JSON.</summary>
	/// <remarks>
	/// Arrays encoded with <see cref="ByteArrayInString.Convert(ArraySlice{byte}, bool)"/>
	/// tend to be slightly more compact than standard Uuencoding or Base64, 
	/// and when you use this encoding in JSON with UTF-8, the output is 
	/// typically also more compact than yEnc since double-byte characters 
	/// above 127 are avoided.
	/// <para/>
	/// A BAIS string alternates between runs of "direct" bytes (usually bytes
	/// in the ASCII range that are represented as themselves) and runs of a
	/// special base-64 encoding. The base-64 encoding is a sequence of 6-bit
	/// digits with 64 added to them, except for 63 which is mapped to itself.
	/// This is easier and faster to encode and decode than standard Base64
	/// and has an interesting property described below.
	/// <para/>
	/// A BAIS string begins in ASCII mode and switches to base 64 when the '\b'
	/// character is encountered. Base-64 mode ends, returning to ASCII, when a 
	/// '!' character is encountered.
	/// <para/>
	/// For example:
	/// <pre>
	///   //                    C   a    t       \n  E        A   B   C   D
	///   var b = new byte[] { 67, 97, 116, 128, 10, 69, 255, 65, 66, 67, 68 };
	///   Assert.AreEqual(ByteArrayInString.Convert(b), "Cat\b`@iE?tEB!CD");
	/// </pre>
	/// A byte sequence such as 128, 10, 69, 255 can be encoded in base 64 as 
	/// illustrated:
	/// <pre>
	///              ---128---    ---10----    ---69----  ---255---  
	///   Bytes:     1000 0000    0000 1010    0100 0101  1111 1111  
	///   Base 64:   100000   000000   101001    000101   111111   110000
	///   Encoded: 01100000 01000000 01101001  01000101 01111111 01110000
	///            ---96--- ---64--- --105---  ---69--- --127--- --112---
	///               `        @        i         E        ~        p
	/// </pre>
	/// <para/>
	/// An interesting property of this base-64 encoding is that when it encodes
	/// bytes between 63 and 126, those bytes appear unchanged at certain 
	/// offsets (specifically the third, sixth, ninth, etc.) In this example, 
	/// since the third byte is 'E' (69), it also appears as 'E' in the 
	/// output.
	/// <para/>
	/// When viewing BAIS strings, another thing to keep in mind is that 
	/// runs of zeroes ('\0') will tend to appear as runs of `@` characters 
	/// in the base 64 encoding, although a single zero is not always enough 
	/// to make a `@` appear. Runs of 255 will tend to appear as runs of `?`.
	/// <para/>
	/// There are many ways to encode a given byte array as BAIS.
	/// </remarks>
	public static class ByteArrayInString
	{
		/// <summary>Encodes a byte array to a string with BAIS encoding, which preserves 
		/// runs of ASCII characters unchanged.</summary>
		/// <param name="allowControlChars">If true, control characters under 32 are 
		///   treated as ASCII (except character 8 '\b').</param>
		/// <returns>The encoded string.</returns>
		/// <remarks>
		/// If the byte array can be interpreted as ASCII, it is returned as characters,
		/// e.g. <c>Convert(new byte[] { 65,66,67,33 }) == "ABC!"</c>. When non-ASCII
		/// bytes are encountered, they are encoded as described in the description of
		/// this class.
		/// <para/>
		/// For simplicity, this method's base-64 encoding always encodes groups of 
		/// three bytes if possible (as four characters). This decision may, 
		/// unfortunately, cut off the beginning of some ASCII runs.
		/// </remarks>
		public static string Convert(ArraySlice<byte> bytes, bool allowControlChars = true)
		{
			var sb = new StringBuilder();
			while (RangeExt.TryPopFirst(ref bytes, out byte b))
			{
				if (IsAscii(b, allowControlChars))
					sb.Append((char)b);
				else {
					sb.Append('\b');
					// Do binary encoding in groups of 3 bytes
					for (;; b = bytes.PopFirst(out bool _)) {
						int accum = b;
						if (RangeExt.TryPopFirst(ref bytes, out b)) {
							accum = (accum << 8) | b;
							if (RangeExt.TryPopFirst(ref bytes, out b)) {
								accum = (accum << 8) | b;
								sb.Append(EncodeBase64Digit(accum >> 18));
								sb.Append(EncodeBase64Digit(accum >> 12));
								sb.Append(EncodeBase64Digit(accum >> 6));
								sb.Append(EncodeBase64Digit(accum));
								if (bytes.IsEmpty)
									break;
							} else {
								sb.Append(EncodeBase64Digit(accum >> 10));
								sb.Append(EncodeBase64Digit(accum >> 4));
								sb.Append(EncodeBase64Digit(accum << 2));
								break;
							}
						} else {
							sb.Append(EncodeBase64Digit(accum >> 2));
							sb.Append(EncodeBase64Digit(accum << 4));
							break;
						}
						if (IsAscii(bytes.First, allowControlChars) &&
							IsAscii(bytes[1, 32], allowControlChars) &&
							IsAscii(bytes[2, 32], allowControlChars)) {
							sb.Append('!'); // return to ASCII mode
							break;
						}
					}
				}
			}
			return sb.ToString();
		}

		static bool IsAscii(byte b, bool allowControlChars)
			=> b < 127 && (b >= 32 || (allowControlChars && b != '\b'));

		/// <summary>Decodes a BAIS string back to a byte array.</summary>
		/// <param name="s">String to decode.</param>
		/// <exception cref="FormatException">The string cannot be interpreted as a byte array in BAIS format.</exception>
		/// <returns>Decoded byte array (use <c>Convert(s).ToArray()</c> 
		/// if you need a true array).</returns>
		public static ArraySlice<byte> Convert(string s) =>
			TryConvert(s) ?? throw new FormatException("String cannot be interpreted as byte array".Localized());

		/// <summary>Decodes a BAIS string back to a byte array.</summary>
		/// <param name="s">String to decode.</param>
		/// <exception cref="FormatException">The string cannot be interpreted as a byte array in BAIS format.</exception>
		/// <returns>Decoded byte array (use <c>Convert(s).ToArray()</c> 
		/// if you need a true array).</returns>
		public static ArraySlice<byte> Convert(UString s) =>
			TryConvert(s) ?? throw new FormatException("String cannot be interpreted as byte array".Localized());

		/// <summary>Decodes a BAIS string back to a byte array.</summary>
		/// <param name="s">String to decode.</param>
		/// <returns>Decoded byte array, or null if decoding fails.</returns>
		public static ArraySlice<byte>? TryConvert(UString s)
		{
			// Maybe when we go to .NET Core they'll offer a Span overload to make this efficient?
			return TryConvert(s.ToString());
		}

		/// <summary>Decodes a BAIS string back to a byte array.</summary>
		/// <param name="s">String to decode.</param>
		/// <returns>Decoded byte array, or null if decoding fails.</returns>
		public static ArraySlice<byte>? TryConvert(string s)
		{
			byte[] b = Encoding.UTF8.GetBytes(s);
			var result = ConvertToBytes(b);
			return result.InternalList == null ? (ArraySlice<byte>?)null : result;
		}

		private static ArraySlice<byte> ConvertToBytes(byte[] b)
		{
			for (int i = 0; i < b.Length - 1; ++i)
			{
				if (b[i] == '\b')
				{
					int iOut = i++;

					for (; ; )
					{
						byte cur;
						if (i >= b.Length || (uint)((cur = b[i]) - 63) > 63)
							throw new FormatException("String cannot be interpreted as a byte array".Localized());
						int digit = (cur - 64) & 63;
						int zeros = 16 - 6; // number of 0 bits on right side of accum
						int accum = digit << zeros;

						while (++i < b.Length)
						{
							if ((uint)((cur = b[i]) - 63) > 63)
								break;
							digit = (cur - 64) & 63;
							zeros -= 6;
							accum |= digit << zeros;
							if (zeros <= 8)
							{
								b[iOut++] = (byte)(accum >> 8);
								accum <<= 8;
								zeros += 8;
							}
						}

						if ((accum & 0xFF00) != 0 || (i < b.Length && b[i] != '!'))
							return default;
						i++;

						// Start taking bytes verbatim
						while (i < b.Length && b[i] != '\b')
							b[iOut++] = b[i++];
						if (i >= b.Length)
							return b.Slice(0, iOut);
						i++;
					}
				}
			}
			return b;
		}


		public static char EncodeBase64Digit(int digit)
			=> (char)((digit + 1 & 63) + 63);
		public static int DecodeBase64Digit(char digit)
			=> (uint)(digit - 63) <= 63 ? (digit - 64) & 63 : -1;
	}
}
