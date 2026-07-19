using Loyc.Collections;
using Loyc.Collections.MutableListExtensionMethods;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
	///   var b = new byte[] { 67, 97, 116, 131, 10, 69, 255, 65, 66, 67, 68 };
	///   Assert.AreEqual(ByteArrayInString.Convert(b), "Cat\b`piE?tEB!CD");
	/// </pre>
	/// A byte sequence such as 131, 10, 69, 255 can be encoded in base 64 as 
	/// illustrated:
	/// <pre>
	///              ---131---    ---10----    ---69----  ---255---  
	///   Bytes:     1000 0011    0000 1010    0100 0101  1111 1111  
	///   Base 64:   100000   110000   101001    000101   111111   110000
	///   Encoded: 01100000 01110000 01101001  01000101 01111111 01110000
	///            ---96--- --112--- --105---  ---69--- --127--- --112---
	///               `        p        i         E        ~        p
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
	/// Since a BAIS string starts in ASCII mode by default, a sequence of
	/// bytes is normally represented as itself if it happens to be ASCII, 
	/// e.g. the byte form of "Hello!" is encoded as the string "Hello!".
	/// However, in order to unambiguously distinguish BAIS from conventional
	/// base64 encoding, the first character of any BAIS string can be set to
	/// '!'. For example, "!Hello!" is also a valid BAIS encoding of "Hello!".
	/// Consequently, if the first byte encoded is '!' (33), the BAIS encoding 
	/// will start with two '!' characters.
	/// <para/>
	/// There are many ways to encode a given byte array as BAIS. It is 
	/// possible to guarantee that a BAIS encoding is never more than one 
	/// character larger than conventional base64, but the easiest way to 
	/// guarantee this is to avoid switching to ASCII mode unless there are
	/// at least 6 ASCII characters in a row. This implementation requires 
	/// only 4 ASCII characters in a row instead, so in pathological cases 
	/// the result can be 7% longer than conventional base64, but this 
	/// should be very rare. When the string is encoded to JSON, this
	/// pathological worst case is 15% longer than base64.
	/// </remarks>
	public static class ByteArrayInString
	{
		/// <summary>Encodes a byte array to a string with BAIS encoding, which preserves 
		/// runs of ASCII characters unchanged.</summary>
		/// <param name="allowControlChars">If true, control characters under 32 are 
		///   treated as ASCII (except character 8 '\b'). When preparing output for
		///   JSON it may be more efficient to use false here, because JSON does not 
		///   officially allow control characters in strings, so if this is true, a
		///   standard JSON encoder will lengthen control characters to 6-character 
		///   escape sequences.</param>
		/// <param name="forceInitialEscape">If true, the first output character will always
		///   be '!' or '\n' to indicate that BAIS encoding is being used rather than
		///   conventional base64 encoding.</param>
		/// <returns>The encoded string.</returns>
		/// <remarks>
		/// If the byte array can be interpreted as ASCII, it is returned as characters,
		/// e.g. <c>Convert(new byte[] { 65,66,67,33 }) == "ABC!"</c>. When non-ASCII
		/// bytes are encountered, they are encoded as described in the description of
		/// this class.
		/// <para/>
		/// For simplicity, this method's base-64 encoding always encodes groups of 
		/// three bytes if possible (as four characters). This decision may, 
		/// unfortunately, cut off the beginning of some ASCII runs. Also, to ensure
		/// that the encoding is never significantly larger than base64, a switch to
		/// ASCII mode only happens if there are at least 4 ASCII characters in a row.
		/// Because of these two facts combined, there are cases in which a string of
		/// 5 ASCII characters will be encoded as base64. But if there are at least 6
		/// ASCII characters in a row, at least 4 of them will appear as ASCII in the 
		/// output.
		/// </remarks>
		public static string ConvertFromBytes(ReadOnlySpan<byte> span, bool allowControlChars, bool forceInitialEscape = false)
		{
			if (span.Length == 0)
				return forceInitialEscape ? "!" : "";

			byte b = span[0];
			var sb = new StringBuilder();
			if ((forceInitialEscape && IsAscii(b, allowControlChars)) || b == '!')
				sb.Append('!');

			for (int i = 1;;)
			{
				if (IsAscii(b, allowControlChars))
					sb.Append((char)b);
				else {
					sb.Append('\b');
					// Do binary encoding in groups of 3 bytes
					for (;; b = span[i++]) {
						int accum = b;
						if (i < span.Length) {
							b = span[i++];
							accum = (accum << 8) | b;
							if (i < span.Length) {
								b = span[i++];
								accum = (accum << 8) | b;
								sb.Append(EncodeBase64Digit(accum >> 18));
								sb.Append(EncodeBase64Digit(accum >> 12));
								sb.Append(EncodeBase64Digit(accum >> 6));
								sb.Append(EncodeBase64Digit(accum));
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
						if (i < span.Length && IsAscii(span[i], allowControlChars) &&
							(i + 1 >= span.Length || IsAscii(span[i + 1], allowControlChars)) &&
							(i + 2 >= span.Length || IsAscii(span[i + 2], allowControlChars)) &&
							(i + 3 >= span.Length || IsAscii(span[i + 3], allowControlChars))) {
							sb.Append('!');
							break; // return to ASCII mode
						}
						if (i >= span.Length)
							break;
					}
				}

				if ((uint)i >= (uint)span.Length)
					break;
				b = span[i++];
			}
			return sb.ToString();
		}

		static bool IsAscii(byte b, bool allowControlChars)
			=> b < 127 && (b >= 32 || (allowControlChars && b != '\b'));

		/// <inheritdoc cref="ConvertFromBytes(Memory{byte}, bool)"/>
		public static string ConvertFromBytes(byte[] bytes, bool allowControlChars, bool forceInitialEscape = false)
			=> ConvertFromBytes(bytes.AsSpan(), allowControlChars);


		/// <summary>Decodes a BAIS string back to a byte array.</summary>
		/// <param name="s">String to decode.</param>
		/// <exception cref="FormatException">The string cannot be interpreted as a byte array in BAIS format.</exception>
		/// <returns>Decoded byte array (use <c>Convert(s).ToArray()</c> 
		/// if you need a true array).</returns>
		public static ArraySlice<byte> ConvertToBytes(string s) =>
			TryConvertToBytes(s) ?? throw new FormatException("String cannot be interpreted as byte array".Localized());

		/// <summary>Decodes a BAIS string back to a byte array.</summary>
		/// <param name="s">String to decode.</param>
		/// <exception cref="FormatException">The string cannot be interpreted as a byte array in BAIS format.</exception>
		/// <returns>Decoded byte array (use <c>Convert(s).ToArray()</c> 
		/// if you need a true array).</returns>
		public static ArraySlice<byte> ConvertToBytes(UString s) =>
			TryConvertToBytes(s) ?? throw new FormatException("String cannot be interpreted as byte array".Localized());

		/// <summary>Decodes a BAIS string back to a byte array.</summary>
		/// <param name="s">String to decode.</param>
		/// <returns>Decoded byte array, or null if decoding fails.</returns>
		public static ArraySlice<byte>? TryConvertToBytes(UString s)
		{
			// Maybe when we go to .NET Core they'll offer a Span overload to make this efficient?
			return TryConvertToBytes(s.ToString() ?? "");
		}

		/// <summary>Decodes a BAIS string back to a byte array.</summary>
		/// <param name="s">String to decode.</param>
		/// <returns>Decoded byte array, or null if decoding fails.</returns>
		public static ArraySlice<byte>? TryConvertToBytes(string s)
		{
			byte[] b = Encoding.UTF8.GetBytes(s);
			return TryConvertToBytesInPlace(b);
		}

		public static ArraySlice<byte>? TryConvertToBytes(ReadOnlySpan<byte> ascii)
			=> TryConvertToBytesInPlace(ascii.ToArray());

		public static ArraySlice<byte>? TryConvertToBytesInPlace(Memory<byte> ascii)
		{
			if (ascii.Length == 0)
				return ascii;

			Span<byte> b = ascii.Span;
			int iStart = b[0] == '!' ? 1 : 0;
			for (int i = iStart; i < b.Length - 1; ++i)
			{
				if (b[i] == '\b')
				{
					int iOut = i++;

					for (;;)
					{
						byte cur;
						if (i < b.Length)
						{
							if ((uint)((cur = b[i]) - 63) > 63)
								return null;
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

							// Invalid states: unused bits in accumulator, or invalid base-64 char
							if ((accum & 0xFF00) != 0 || (i < b.Length && b[i] != '!'))
								return null;
							i++;

							// Start taking bytes verbatim
							while (i < b.Length && b[i] != '\b')
								b[iOut++] = b[i++];
						}
						if (i >= b.Length)
							return ascii.Slice(iStart, iOut - iStart);
						i++;
					}
				}
			}
			return ascii.Slice(iStart);
		}

		public static char EncodeBase64Digit(int digit)
			=> (char)((digit + 1 & 63) + 63);
		public static int DecodeBase64Digit(char digit)
			=> (uint)(digit - 63) <= 63 ? (digit - 64) & 63 : -1;

		/// <inheritdoc cref="ConvertFromBytes(Memory{byte}, bool)"/>
		[Obsolete("This was renamed to ConvertFromBytes")]
		public static string Convert(ArraySlice<byte> bytes, bool allowControlChars = true) 
			=> ConvertFromBytes(bytes.AsMemory().Span, allowControlChars);

		[Obsolete("This was renamed to TryConvertToBytes")]
		public static ArraySlice<byte>? TryConvert(UString s) => TryConvertToBytes(s);

		[Obsolete("This was renamed to TryConvertToBytes")]
		public static ArraySlice<byte>? TryConvert(string s) => TryConvertToBytes(s);

		[Obsolete("This was renamed to ConvertToBytes")]
		public static ArraySlice<byte> Convert(string s) => ConvertToBytes(s);

		[Obsolete("This was renamed to ConvertToBytes")]
		public static ArraySlice<byte> Convert(UString s) => ConvertToBytes(s);
	}
}
