using System;
using System.Text;

namespace Loyc
{
	/// <summary>An <see cref="Encoding"/> for WTF-8: UTF-8 extended so that unpaired
	///   UTF-16 surrogates are encoded as themselves (3 bytes, ED A0-BF 80-BF) instead
	///   of being replaced with U+FFFD. This makes every .NET string round-trippable,
	///   which <see cref="Encoding.UTF8"/> does not guarantee.
	///   See https://simonsapin.github.io/wtf-8/ </summary>
	/// <remarks>Well-formed UTF-8 input/output is unchanged, so this is a drop-in
	///   replacement for UTF-8 without byte order marks. Decoding treats surrogate
	///   sequences as valid; other malformed bytes become U+FFFD as usual. Encoding is
	///   delegated to <see cref="Encoding.UTF8"/> for surrogate-free spans, so speed
	///   is close to UTF-8 except on strings that actually contain surrogates.</remarks>
	public sealed class WTF8Encoding : Encoding
	{
		public static readonly WTF8Encoding Instance = new WTF8Encoding();

		public override string EncodingName => "WTF-8";
		public override string WebName => "wtf-8";

		public override int GetMaxByteCount(int charCount) => checked(charCount * 3);
		public override int GetMaxCharCount(int byteCount) => byteCount;

		public override int GetByteCount(char[] chars, int index, int count)
			=> EncodeCore(new ReadOnlySpan<char>(chars, index, count), default, countOnly: true);
		public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
			=> EncodeCore(new ReadOnlySpan<char>(chars, charIndex, charCount), bytes.AsSpan(byteIndex), countOnly: false);
		public override int GetCharCount(byte[] bytes, int index, int count)
			=> DecodeCore(new ReadOnlySpan<byte>(bytes, index, count), default, countOnly: true);
		public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
			=> DecodeCore(new ReadOnlySpan<byte>(bytes, byteIndex, byteCount), chars.AsSpan(charIndex), countOnly: false);

		#if NETSTANDARD2_0 || NETFRAMEWORK
		// On these targets the base class has no span methods to override
		public int GetByteCount(ReadOnlySpan<char> chars)
			=> EncodeCore(chars, default, countOnly: true);
		public int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes)
			=> EncodeCore(chars, bytes, countOnly: false);
		public int GetCharCount(ReadOnlySpan<byte> bytes)
			=> DecodeCore(bytes, default, countOnly: true);
		public int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars)
			=> DecodeCore(bytes, chars, countOnly: false);
		public string GetString(ReadOnlySpan<byte> bytes)
		{
			var chars = new char[DecodeCore(bytes, default, countOnly: true)];
			DecodeCore(bytes, chars, countOnly: false);
			return new string(chars);
		}
		#else
		public override int GetByteCount(ReadOnlySpan<char> chars)
			=> EncodeCore(chars, default, countOnly: true);
		public override int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes)
			=> EncodeCore(chars, bytes, countOnly: false);
		public override int GetCharCount(ReadOnlySpan<byte> bytes)
			=> DecodeCore(bytes, default, countOnly: true);
		public override int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars)
			=> DecodeCore(bytes, chars, countOnly: false);
		// Hides the base method, which would copy the input into an array
		public new string GetString(ReadOnlySpan<byte> bytes)
		{
			if (bytes.IndexOf((byte)0xED) < 0)
				return Encoding.UTF8.GetString(bytes); // plain UTF-8: single SIMD pass
			var chars = new char[DecodeCore(bytes, default, countOnly: true)];
			DecodeCore(bytes, chars, countOnly: false);
			return new string(chars);
		}
		#endif

		/// <summary>Encodes chars as WTF-8, or (if countOnly) just measures the result.</summary>
		static int EncodeCore(ReadOnlySpan<char> chars, Span<byte> bytes, bool countOnly)
		{
			int di = 0;
			while (!chars.IsEmpty) {
				int i = IndexOfSurrogate(chars);
				if (i != 0) {
					var clean = i < 0 ? chars : chars.Slice(0, i);
					di += countOnly ? Utf8ByteCount(clean) : Utf8Encode(clean, bytes.Slice(di));
					if (i < 0)
						break;
					chars = chars.Slice(i);
				}
				char c = chars[0];
				if (char.IsHighSurrogate(c) && chars.Length > 1 && char.IsLowSurrogate(chars[1])) {
					if (!countOnly) {
						int cp = char.ConvertToUtf32(c, chars[1]);
						bytes[di]     = (byte)(0xF0 | (cp >> 18));
						bytes[di + 1] = (byte)(0x80 | ((cp >> 12) & 0x3F));
						bytes[di + 2] = (byte)(0x80 | ((cp >> 6) & 0x3F));
						bytes[di + 3] = (byte)(0x80 | (cp & 0x3F));
					}
					di += 4;
					chars = chars.Slice(2);
				} else {
					// Unpaired surrogate: encode it anyway (this is what makes it WTF-8)
					if (!countOnly) {
						bytes[di]     = (byte)(0xE0 | (c >> 12));
						bytes[di + 1] = (byte)(0x80 | ((c >> 6) & 0x3F));
						bytes[di + 2] = (byte)(0x80 | (c & 0x3F));
					}
					di += 3;
					chars = chars.Slice(1);
				}
			}
			return di;
		}

		/// <summary>Decodes WTF-8 to chars, or (if countOnly) just measures the result.</summary>
		static int DecodeCore(ReadOnlySpan<byte> bytes, Span<char> chars, bool countOnly)
		{
			int ci = 0;
			while (!bytes.IsEmpty) {
				// 0xED is the only lead byte whose sequences UTF8 may reject (surrogates)
				int i = bytes.IndexOf((byte)0xED);
				if (i != 0) {
					var clean = i < 0 ? bytes : bytes.Slice(0, i);
					ci += countOnly ? Utf8CharCount(clean) : Utf8Decode(clean, chars.Slice(ci));
					if (i < 0)
						break;
					bytes = bytes.Slice(i);
				}
				if (bytes.Length >= 3 && (bytes[1] & 0xC0) == 0x80 && (bytes[2] & 0xC0) == 0x80) {
					// Covers U+D000-D7FF (valid UTF-8) and U+D800-DFFF (WTF-8 surrogates)
					if (!countOnly)
						chars[ci] = (char)(0xD000 | ((bytes[1] & 0x3F) << 6) | (bytes[2] & 0x3F));
					ci++;
					bytes = bytes.Slice(3);
				} else {
					// Malformed: one replacement char for the maximal subpart
					if (!countOnly)
						chars[ci] = '\uFFFD';
					ci++;
					bytes = bytes.Slice(bytes.Length >= 2 && (bytes[1] & 0xC0) == 0x80 ? 2 : 1);
				}
			}
			return ci;
		}

		static int IndexOfSurrogate(ReadOnlySpan<char> chars)
		{
			#if NET8_0_OR_GREATER
			return chars.IndexOfAnyInRange('\uD800', '\uDFFF');
			#else
			for (int i = 0; i < chars.Length; i++)
				if (char.IsSurrogate(chars[i]))
					return i;
			return -1;
			#endif
		}

		// UTF-8 fast paths for spans known to contain no surrogates (encoding) or no
		// 0xED lead bytes (decoding). netstandard2.0/.NET Framework lack span overloads
		// of Encoding methods, so they get scalar loops instead.
		#if NETSTANDARD2_0 || NETFRAMEWORK
		static int Utf8ByteCount(ReadOnlySpan<char> clean)
		{
			int count = 0;
			foreach (char c in clean)
				count += c < 0x80 ? 1 : c < 0x800 ? 2 : 3;
			return count;
		}
		static int Utf8Encode(ReadOnlySpan<char> clean, Span<byte> bytes)
		{
			int di = 0;
			foreach (char c in clean) {
				if (c < 0x80)
					bytes[di++] = (byte)c;
				else if (c < 0x800) {
					bytes[di++] = (byte)(0xC0 | (c >> 6));
					bytes[di++] = (byte)(0x80 | (c & 0x3F));
				} else {
					bytes[di++] = (byte)(0xE0 | (c >> 12));
					bytes[di++] = (byte)(0x80 | ((c >> 6) & 0x3F));
					bytes[di++] = (byte)(0x80 | (c & 0x3F));
				}
			}
			return di;
		}
		static int Utf8CharCount(ReadOnlySpan<byte> clean)
			=> Encoding.UTF8.GetCharCount(clean.ToArray());
		static int Utf8Decode(ReadOnlySpan<byte> clean, Span<char> chars)
		{
			var decoded = Encoding.UTF8.GetChars(clean.ToArray());
			decoded.CopyTo(chars);
			return decoded.Length;
		}
		#else
		static int Utf8ByteCount(ReadOnlySpan<char> clean) => Encoding.UTF8.GetByteCount(clean);
		static int Utf8Encode(ReadOnlySpan<char> clean, Span<byte> bytes) => Encoding.UTF8.GetBytes(clean, bytes);
		static int Utf8CharCount(ReadOnlySpan<byte> clean) => Encoding.UTF8.GetCharCount(clean);
		static int Utf8Decode(ReadOnlySpan<byte> clean, Span<char> chars) => Encoding.UTF8.GetChars(clean, chars);
		#endif
	}
}
