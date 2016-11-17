using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Numerics;
using Loyc.Math;
using Loyc.Threading;

namespace Loyc.Syntax
{
	/// <summary>Static methods that help with common parsing jobs, such as 
	/// parsing integers, floats, and strings with C escape sequences.</summary>
	/// <remarks>This class also contains a few <i>inverse</i> methods, e.g. 
	/// <see cref="EscapeCStyle(UString, EscapeC)"/> is the inverse of the
	/// C-style string parser <see cref="UnescapeCStyle"/>.</remarks>
	public static class ParseHelpers
	{
		/// <summary>A simple method to parse a sequence of hex digits, without
		/// overflow checks or other features supported by methods like 
		/// <see cref="TryParseInt(string, ref int, out int, int, bool)"/>.</summary>
		/// <returns><c>true</c> iff the entire string was consumed and the string was nonempty.</returns>
		public static bool TryParseHex(UString s, out int value)
		{
			int count = TryParseHex(ref s, out value);
			return count > 0 && s.IsEmpty;
		}
		/// <summary>A simple method to parse a sequence of hex digits, without
		/// overflow checks or other features supported by methods like 
		/// <see cref="TryParseInt(string, ref int, out int, int, bool)"/>.</summary>
		/// <returns>The number of digits parsed</returns>
		public static int TryParseHex(ref UString s, out int value)
		{
			value = 0;
			int len = 0;
			for(;; len++, s = s.Slice(1))
			{
				int digit = HexDigitValue(s[0, '\0']);
				if (digit == -1)
					return len;
				else
					value = (value << 4) + digit;
			}
		}

		/// <summary>Gets the integer value for the specified hex digit, or -1 if 
		/// the character is not a hex digit.</summary>
		public static int HexDigitValue(char c)
		{
			if (c >= '0' && c <= '9')
				return c - '0';
			if (c >= 'A' && c <= 'F')
				return c - 'A' + 10;
			if (c >= 'a' && c <= 'f')
				return c - 'a' + 10;
			else
				return -1;
		}
		/// <summary>Gets the integer value for the specified digit, where 'A' maps 
		/// to 10 and 'Z' maps to 35, or -1 if the character is not a digit or
		/// letter.</summary>
		public static int Base36DigitValue(char c)
		{
			if (c >= '0' && c <= '9')
				return c - '0';
			if (c >= 'A' && c <= 'Z')
				return c - 'A' + 10;
			if (c >= 'a' && c <= 'z')
				return c - 'a' + 10;
			else
				return -1;
		}

		/// <summary>Gets the hex digit character for the specified value, 
		/// or '?' if the value is not in the range 0...15. Uses uppercase.</summary>
		public static char HexDigitChar(int value)
		{
			if ((uint)value < 10)
				return (char)('0' + value);
			else if ((uint)value < 16)
				return (char)('A' - 10 + value);
			else
				return '?';
		}

		/// <summary>Escapes characters in a string using C style, e.g. the string 
		/// <c>"Foo\"\n"</c> maps to <c>"Foo\\\"\\\n"</c> by default.</summary>
		public static string EscapeCStyle(UString s, EscapeC flags = EscapeC.Default)
		{
			return EscapeCStyle(s, flags, '\0');
		}
		/// <summary>Escapes characters in a string using C style.</summary>
		/// <param name="flags">Specifies which characters should be escaped.</param>
		/// <param name="quoteType">Specifies a character that should always be 
		/// escaped (typically one of <c>' " `</c>)</param>
		public static string EscapeCStyle(UString s, EscapeC flags, char quoteType)
		{
			StringBuilder s2 = new StringBuilder(s.Length+1);
			bool usedEscapes = false, fail;
			for (;;) {
				int c = s.PopFirst(out fail);
				if (fail) break;
				usedEscapes |= EscapeCStyle(c, s2, flags, quoteType);
			}
			if (!usedEscapes && s.InternalString.Length == s.Length)
				return s.InternalString;
			return s2.ToString();
		}

		static void EscapeU(int c, StringBuilder @out, EscapeC flags)
		{
			if (c <= 255 && (flags & EscapeC.BackslashX) != 0)
				@out.Append(@"\x");
			else {
				@out.Append(@"\u");
				if (c > 0xFFFF || (flags & EscapeC.HasLongEscape) != 0) {
					Debug.Assert(c <= 0x10FFFF);
					@out.Append(HexDigitChar((c >> 20) & 0xF));
					@out.Append(HexDigitChar((c >> 16) & 0xF));
				}
				@out.Append(HexDigitChar((c >> 12) & 0xF));
				@out.Append(HexDigitChar((c >> 8) & 0xF));
			}
			@out.Append(HexDigitChar((c >> 4) & 0xF));
			@out.Append(HexDigitChar(c & 0xF));
		}

		/// <summary>Writes a character <c>c</c> to a StringBuilder, either as a normal 
		/// character or as a C-style escape sequence.</summary>
		/// <param name="flags">Specifies which characters should be escaped.</param>
		/// <param name="quoteType">Specifies a character that should always be 
		/// escaped (typically one of <c>' " `</c>)</param>
		/// <returns>true if an escape sequence was emitted, false if not.</returns>
		/// <remarks><see cref="EscapeC.HasLongEscape"/> can be used to force a 6-digit 
		/// unicode escape; this may be needed if the next character after this one 
		/// is a digit.</remarks>
		public static bool EscapeCStyle(int c, StringBuilder @out, EscapeC flags = EscapeC.Default, char quoteType = '\0')
		{
			for(;;) {
				if (c >= 128) {
					if ((flags & EscapeC.NonAscii) != 0) {
						EscapeU(c, @out, flags);
					} else if (c >= 0xDC00) {
						if ((flags & EscapeC.UnicodeNonCharacters) != 0 && (
							c >= 0xFDD0 && c <= 0xFDEF || // 0xFDD0...0xFDEF 
							(c & 0xFFFE) == 0xFFFE) || // 0xFFFE, 0xFFFF, 0x1FFFE, 0x1FFFF, etc.
							(c & 0xFC00) == 0xDC00) { // 0xDC00...0xDCFF 
							EscapeU(c, @out, flags);
						} else if ((flags & EscapeC.UnicodePrivateUse) != 0 && (
							c >= 0xE000 && c <= 0xF8FF ||
							c >= 0xF0000 && c <= 0xFFFFD ||
							c >= 0x100000 && c <= 0x10FFFD)) {
							EscapeU(c, @out, flags);
						} else
							break;
					} else
						break;
				} else if (c < 32) {
					if (c == '\n')
						@out.Append(@"\n");
					else if (c == '\r')
						@out.Append(@"\r");
					else if (c == '\0')
						@out.Append(@"\0");
					else {
						if ((flags & EscapeC.ABFV) != 0) {
							if (c == '\a') { // 7 (alert)
								@out.Append(@"\a");
								return true;
							}
							if (c == '\b') { // 8 (backspace)
								@out.Append(@"\b");
								return true;
							}
							if (c == '\f') { // 12 (form feed)
								@out.Append(@"\f");
								return true;
							}
							if (c == '\v') { // 11 (vertical tab)
								@out.Append(@"\v");
								return true;
							}
						}
						if ((flags & EscapeC.Control) != 0) {
							if (c == '\t')
								@out.Append(@"\t");
							else
								EscapeU(c, @out, flags);
						} else
							@out.Append(c);
					}
				} else if (c == '\"' && (flags & EscapeC.DoubleQuotes) != 0) {
					@out.Append("\\\"");
				} else if (c == '\'' && (flags & EscapeC.SingleQuotes) != 0) {
					@out.Append("\\'");
				} else if (c == '\\')
					@out.Append(@"\\");
				else
					break;
				return true;
			}

			if (c == quoteType) {
				@out.Append('\\');
				@out.Append((char)c);
				return true;
			} else 
				@out.AppendCodePoint(c);
			return false;
		}

		/// <summary>Unescapes a string that uses C-style escape sequences, e.g. 
		/// "\\\n\\\r" becomes "\n\r".</summary>
		public static string UnescapeCStyle(UString s, bool removeUnnecessaryBackslashes = false)
		{
			EscapeC _;
			return UnescapeCStyle(s, out _, removeUnnecessaryBackslashes).ToString();
		}

		/// <summary>Unescapes a string that uses C-style escape sequences, e.g. 
		/// "\\\n\\\r" becomes "\n\r".</summary>
		/// <param name="encountered">Returns information about whether escape 
		/// sequences were encountered, and which categories.</param>
		/// <param name="removeUnnecessaryBackslashes">Causes the backslash before 
		/// an unrecognized escape sequence to be removed, e.g. "\z" => "z".</param>
		/// <remarks>See <see cref="UnescapeChar(string, ref int, ref EscapeC)"/> for details.</remarks>
		public static StringBuilder UnescapeCStyle(UString s, out EscapeC encountered, bool removeUnnecessaryBackslashes = false)
		{
			encountered = 0;
			StringBuilder @out = new StringBuilder(s.Length);
			while (s.Length > 0) {
				EscapeC encounteredHere = 0;
				int c = UnescapeChar(ref s, ref encounteredHere);
				encountered |= encounteredHere;
				if (removeUnnecessaryBackslashes && (encounteredHere & EscapeC.Unrecognized) != 0) {
					Debug.Assert(c == '\\');
					continue;
				}
				@out.AppendCodePoint(c);
			}
			return @out;
		}

		public static int UnescapeChar(string s, ref int i)
		{
			UString s2 = new UString(s, i);
			EscapeC _ = 0;
			int result = UnescapeChar(ref s2, ref _);
			i = s2.InternalStart;
			return result;
		}

		public static int UnescapeChar(ref UString s)
		{
			EscapeC _ = 0;
			return UnescapeChar(ref s, ref _);
		}

		/// <summary>Unescapes a single character of a string. Returns the 
		/// first character if it is not a backslash, or <c>\</c> if it is a 
		/// backslash but no escape sequence could be discerned.</summary>
		/// <param name="s">Slice of a string to be unescaped. When using a
		/// <c>ref UString</c> overload of this method, <c>s</c> will be shorter upon
		/// returning from this method, as the parsed character(s) are clipped 
		/// from the beginning (<c>s.InternalStart</c> is incremented by one 
		/// normally and more than one in case of an escape sequence.)</param>
		/// <param name="encountered">Bits of this parameter are set according
		/// to which escape sequence is encountered, if any.</param>
		/// <remarks>
		/// This function also decodes (non-escaped) surrogate pairs.
		/// <para/>
		/// Code points with 5 or 6 digits such as \u1F4A9 are supported.
		/// \x escapes must be two digits and set the EscapeC.BackslashX flag.
		/// \u escapes must be 4 to 6 digits. If a \u escape has more than 4 
		/// digits, the EscapeC.HasLongEscapes flag is set. Invalid 6-digit 
		/// escapes like \u123456 are "made valid" by being treated as 5 digits
		/// (the largest valid escape is <c>\u10FFFF</c>.)
		/// <para/>
		/// Supported escapes: <c>\u \x \\ \n \r \0 \' \" \` \t \a \b \f \v</c>
		/// </remarks>
		/// <example>
		/// EscapeC e = 0; 
		/// UString str = @"\nfoo";
		/// char c = UnescapeChar(ref str, ref e);
		/// Contract.Assert(str == "foo" && e == EscapeC.HasEscapes);
		/// </example>
		public static int UnescapeChar(ref UString s, ref EscapeC encountered)
		{
			bool fail;
			int c = s.PopFirst(out fail);
			if (c != '\\' || s.Length <= 0)
				return c;

			encountered |= EscapeC.HasEscapes;
			int code; // hex code after \u or \x
			UString slice, original = s;
			switch (s.PopFirst(out fail)) {
				case 'u':
					slice = s.Left(6);
					if (TryParseHex(ref slice, out code) >= 4) {
						if (code <= 0x10FFFF) {
							s = s.Substring(slice.InternalStart - s.InternalStart);
						} else {
							Debug.Assert(slice.Length == 0);
							// It appears to be 6 digits but only the first 5 can 
							// be treated as part of the escape sequence.
							s = s.Substring(5);
							code >>= 4;
							encountered |= EscapeC.HasInvalid6DigitEscape;
						}
						if (slice.InternalStart > s.InternalStart + 4)
							encountered |= EscapeC.HasLongEscape;
						if (code < 32)
							encountered |= EscapeC.Control;
						else if (code > 127)
							encountered |= EscapeC.NonAscii;
						return code;
					} else
						break;
				case 'x':
					slice = s.Left(2);
					if (TryParseHex(slice, out code)) {
						encountered |= EscapeC.BackslashX;
						if (code < 32)
							encountered |= EscapeC.Control;
						else if (code > 127)
							encountered |= EscapeC.NonAscii;
						s = s.Substring(2);
						return code;
					} else
						break;
				case '\\':
					return '\\';
				case 'n':
					return '\n';
				case 'r':
					return '\r';
				case '0':
					return '\0';
				case '\"':
					encountered |= EscapeC.DoubleQuotes;
					return '\"';
				case '\'':
					encountered |= EscapeC.SingleQuotes;
					return '\'';
				case '`':
					encountered |= EscapeC.Quotes;
					return '`';
				case 't':
					encountered |= EscapeC.Control;
					return '\t';
				case 'a':
					encountered |= EscapeC.ABFV;
					return '\a';
				case 'b':
					encountered |= EscapeC.ABFV;
					return '\b';
				case 'f':
					encountered |= EscapeC.ABFV;
					return '\f';
				case 'v':
					encountered |= EscapeC.ABFV;
					return '\v';
			}
			encountered |= EscapeC.Unrecognized;
			s = original;
			return c;
		}

		/// <summary>Tries to parse a string to an integer. Unlike <see cref="Int32.TryParse(string, out int)"/>,
		/// this method allows parsing to start at any point in the string, it 
		/// allows non-numeric data after the number, and it can parse numbers that
		/// are not in base 10.</summary>
		/// <param name="radix">Number base, e.g. 10 for decimal and 2 for binary.
		/// Must be in the range 2 to 36.</param>
		/// <param name="index">Location at which to start parsing</param>
		/// <param name="flags"><see cref="ParseNumberFlag"/>s that affect parsing behavior.</param>
		/// <param name="skipSpaces">Whether to skip spaces before parsing. Only 
		/// the ' ' and '\t' characters are treated as spaces. No space is allowed 
		/// between '-' and the digits of a negative number, even with this flag.</param>
		/// <returns>True if a number was found starting at the specified index
		/// and it was successfully converted to a number, or false if not.</returns>
		/// <remarks>
		/// This method never throws. If parsing fails, index is left unchanged, 
		/// except that spaces are still skipped if you set the skipSpaces flag. 
		/// If base>36, parsing can succeed but digits above 35 (Z) cannot occur 
		/// in the output number. If the input number cannot fit in 'result', the 
		/// return value is false but index increases anyway, and 'result' is a 
		/// bitwise truncated version of the number.
		/// <para/>
		/// When parsing input such as "12.34", the parser stops and returns true
		/// at the dot (with a result of 12 in this case).
		/// </remarks>
		public static bool TryParseInt(string s, ref int index, out int result, int radix = 10, bool skipSpaces = true)
		{
			UString slice = s.Slice(index);
			bool success = TryParseInt(ref slice, out result, radix, skipSpaces ? ParseNumberFlag.SkipSpacesInFront : 0);
			index = slice.InternalStart;
			return success;
		}

		/// <inheritdoc cref="TryParseInt(string, ref int, out int, int, bool)"/>
		public static bool TryParseInt(ref UString s, out int result, int radix = 10, ParseNumberFlag flags = 0)
		{
			long resultL;
			bool ok = TryParseInt(ref s, out resultL, radix, flags);
			result = (int)resultL;
			return ok && result == resultL;
		}

		/// <inheritdoc cref="TryParseInt(string, ref int, out int, int, bool)"/>
		public static bool TryParseInt(ref UString input, out long result, int radix = 10, ParseNumberFlag flags = 0)
		{
			if ((flags & ParseNumberFlag.SkipSpacesInFront) != 0)
				input = SkipSpaces(input);
			UString s = input;

			bool negative = false;
			char c = s[0, '\0'];
			if (c == '-' || c == '+') {
				negative = c == '-';
				s = s.Slice(1);
			}

			ulong resultU = 0;
			int numDigits;
			bool ok = TryParseUInt(ref s, ref resultU, radix, flags, out numDigits);
			result = negative ? -(long)resultU : (long)resultU;
			if (numDigits != 0)
				input = s;
			return ok && ((result < 0) == negative || result == 0);
		}

		/// <summary>Tries to parse a string to an unsigned integer.</summary>
		/// <param name="s">A slice of a string to be parsed.</param>
		/// <param name="radix">Number base, e.g. 10 for decimal and 2 for binary.
		/// Normally in the range 2 to 36.</param>
		/// <param name="flags"><see cref="ParseNumberFlag"/>s that affect parsing behavior.</param>
		/// <returns>True if a number was found starting at the specified index
		/// and it was successfully converted to a number, or false if not.</returns>
		/// <remarks>
		/// This method never throws. It shrinks the slice <c>s</c> as it parses,
		/// so if parsing fails, <c>s[0]</c> will be the character at which parsing 
		/// fails. If base>36, parsing can succeed but digits above 35 (Z) cannot 
		/// be represented in the input string. If the number cannot fit in 
		/// <c>result</c>, the return value is false and the method's exact behavior
		/// depends on whether you used <see cref="ParseNumberFlag.StopBeforeOverflow"/>.
		/// <para/>
		/// When parsing input such as "12.34", the parser stops and returns true
		/// at the dot (with a result of 12 in this case).
		/// </remarks>
		public static bool TryParseUInt(ref UString s, out ulong result, int radix = 10, ParseNumberFlag flags = 0)
		{
			result = 0;
			int _;
			return TryParseUInt(ref s, ref result, radix, flags, out _);
		}
		static bool TryParseUInt(ref UString s, ref ulong result, int radix, ParseNumberFlag flags, out int numDigits)
		{
			numDigits = 0;
			if ((flags & ParseNumberFlag.SkipSpacesInFront) != 0)
				s = SkipSpaces(s);
			
			bool overflow = false;

			for (;; s = s.Slice(1))
			{
				char c = s[0, '\0'];
				uint digit = (uint)Base36DigitValue(c);
				if (digit >= radix) {
					if ((c == ' ' || c == '\t') && (flags & ParseNumberFlag.SkipSpacesInsideNumber) != 0)
						continue;
					else if (c == '_' && (flags & ParseNumberFlag.SkipUnderscores) != 0)
						continue;
					else if (c == '\'' && (flags & ParseNumberFlag.SkipSingleQuotes) != 0)
						continue;
					else
						break;
				}

				ulong next;
				try {
					next = checked(result * (uint)radix + digit);
				} catch (OverflowException) {
					overflow = true;
					if ((flags & ParseNumberFlag.StopBeforeOverflow) != 0)
						return false;
					next = result * (uint)radix + digit;
				}
				numDigits++;
				result = next;
			}
			return !overflow && numDigits > 0;
		}

		/// <inheritdoc cref="TryParseUInt(ref UString, out ulong, int, ParseNumberFlag)"/>
		public static bool TryParseUInt(ref UString s, out BigInteger result, int radix = 10, ParseNumberFlag flags = 0)
		{
			result = 0;
			int _;
			return TryParseUInt(ref s, ref result, radix, flags, out _);
		}
		static bool TryParseUInt(ref UString s, ref BigInteger result, int radix, ParseNumberFlag flags, out int numDigits)
		{
			// TODO: OPTIMIZE THIS ALGORITHM: it is currently O(n^2) in the number of digits
			numDigits = 0;
			if ((flags & ParseNumberFlag.SkipSpacesInFront) != 0)
				s = SkipSpaces(s);

			for (;; s = s.Slice(1))
			{
				char c = s[0, '\0'];
				uint digit = (uint)Base36DigitValue(c);
				if (digit >= radix) {
					if ((c == ' ' || c == '\t') && (flags & ParseNumberFlag.SkipSpacesInsideNumber) != 0)
						continue;
					else if (c == '_' && (flags & ParseNumberFlag.SkipUnderscores) != 0)
						continue;
					else if (c == '\'' && (flags & ParseNumberFlag.SkipSingleQuotes) != 0)
						continue;
					else
						break;
				}

				result = result * (uint)radix + digit;
				numDigits++;
			}
			return numDigits > 0;
		}

		/// <summary>Low-level method that identifies the parts of a float literal
		/// of arbitrary base (typically base 2, 10, or 16) with no prefix or 
		/// suffix, such as <c>2.Cp0</c> (which means 2.75 in base 16).</summary>
		/// <param name="radix">Base of the number to parse; must be between 2
		/// and 36.</param>
		/// <param name="mantissa">Integer magnitude of the number.</param>
		/// <param name="exponentBase2">Base-2 exponent to apply, as specified by
		/// the 'p' suffix, or 0 if there is no 'p' suffix..</param>
		/// <param name="exponentBase10">Base-10 exponent to apply, as specified by
		/// the 'e' suffix, or 0 if there is no 'e' suffix..</param>
		/// <param name="exponentBaseR">Base-radix exponent to apply. This number
		/// is based on the front part of the number only (not including the 'p' or
		/// 'e' suffix). Negative values represent digits after the decimal point,
		/// while positive numbers represent 64-bit overflow. For example, if the
		/// input is <c>12.3456</c> with <c>radix=10</c>, the output will be 
		/// <c>mantissa=123456</c> and <c>exponentBaseR=-4</c>. If the input is 
		/// <c>0123_4567_89AB_CDEF_1234.5678</c> with <c>radix=16</c>, the mantissa 
		/// overflows, and the result is <c>mantissa = 0x1234_5678_9ABC_DEF1</c> 
		/// with <c>exponentBaseR=3</c>.</param>
		/// <param name="numDigits">Set to the number of digits in the number, not 
		/// including the exponent part.</param>
		/// <param name="flags">Alters parsing behavior, see <see cref="ParseFlags"/>.</param>
		/// <remarks>
		/// The syntax required is
		/// <code>
		///   ( '+'|'-' )?
		///   ( Digits ('.' Digits?)? | '.' Digits )
		///   ( ('p'|'P') ('-'|'+')? DecimalDigits+ )?
		///   ( ('e'|'E') ('-'|'+')? DecimalDigits+ )?
		/// </code>
		/// where Digits refers to one or more digits in the requested base, 
		/// possibly including underscores or spaces if the flags allow it; similarly, 
		/// DecimalDigits refers to base-10 digits and is also affected by the
		/// flags.
		/// <para/>
		/// Returns false if there was an error interpreting the input.
		/// <para/>
		/// To keep the parser relatively simple, it does not roll back in case of 
		/// error the way the int parser does. For example, given the input "23p", 
		/// the 'p' is consumed and causes the method to return false, even though
		/// the parse could have been successful if it had ignored the 'p'.
		/// </remarks>
		public static bool TryParseFloatParts(ref UString source, int radix, out bool negative, out ulong mantissa, out int exponentBaseR, out int exponentBase2, out int exponentBase10, out int numDigits, ParseNumberFlag flags = 0)
		{
			flags |= ParseNumberFlag.StopBeforeOverflow;

			if ((flags & ParseNumberFlag.SkipSpacesInFront) != 0)
				source = SkipSpaces(source);

			negative = false;
			char c = source[0, '\0'];
			if (c == '-' || c == '+') {
				negative = c == '-';
				source = source.Slice(1);
			}

			int numDigits2 = 0;
			mantissa = 0;
			exponentBase2 = 0;
			exponentBase10 = 0;
			exponentBaseR = 0;
			
			bool success = TryParseUInt(ref source, ref mantissa, radix, flags, out numDigits);
			if (!success) // possible overflow, extra digits remain if so
				numDigits += (exponentBaseR = SkipExtraDigits(ref source, radix, flags));
			
			c = source[0, '\0'];
			if (c == '.' || (c == ',' && (flags & ParseNumberFlag.AllowCommaDecimalPoint) != 0))
			{
				source = source.Slice(1);
				if (exponentBaseR == 0) {
					success = TryParseUInt(ref source, ref mantissa, radix, flags, out numDigits2);
					if ((numDigits += numDigits2) == 0)
						return false;
					exponentBaseR = -numDigits2;
				} else
					Debug.Assert(!success);
				if (!success) // possible overflow, extra digits remain if so
					numDigits += SkipExtraDigits(ref source, radix, flags);
				c = source[0, '\0'];
			}

			if (numDigits == 0)
				return false;

			success = true;
			if (c == 'p' || c == 'P')
			{
				source = source.Slice(1);
				success = TryParseInt(ref source, out exponentBase2, 10, flags) && success;
				c = source[0, '\0'];
			}
			if (c == 'e' || c == 'E')
			{
				source = source.Slice(1);
				success = TryParseInt(ref source, out exponentBase10, 10, flags) && success;
			}
			return success;
		}

		private static int SkipExtraDigits(ref UString s, int radix, ParseNumberFlag flags)
		{
			for (int skipped = 0;; skipped++, s = s.Slice(1)) {
				char c = s[0, '\0'];
				uint digit = (uint)Base36DigitValue(c);
				if (digit >= radix) {
					if ((c == ' ' || c == '\t') && (flags & ParseNumberFlag.SkipSpacesInsideNumber) != 0)
						continue;
					else if (c == '_' && (flags & ParseNumberFlag.SkipUnderscores) != 0)
						continue;
					else if (c == '\'' && (flags & ParseNumberFlag.SkipSingleQuotes) != 0)
						continue;
					else
						return skipped;
				}
			}
		}

		/// <summary>Parses the parts of a floating-point string. See the other 
		/// overload for details.</summary>
		/// <param name="radix">Base of the number to parse; must be 2 (binary), 
		/// 4, 8 (octal), 10 (decimal), 16 (hexadecimal) or 32.</param>
		/// <param name="negative">true if the string began with '-'.</param>
		/// <param name="mantissa">Integer magnitude of the number.</param>
		/// <param name="exponentBase2">Base-2 exponent to apply.</param>
		/// <param name="exponentBase10">Base-10 exponent to apply.</param>
		/// <param name="numDigits">Set to the number of digits in the number, not including the exponent part.</param>
		/// <param name="flags">Alters parsing behavior, see <see cref="ParseFlags"/>.</param>
		/// <remarks>
		/// This method is a wrapper around the other overload that combines 
		/// the 'exponentBaseR' parameter with 'exponentBase2' or 'exponentBase10'
		/// depending on the radix. For example, when radix=10, this method 
		/// adds <c>exponentBaseR</c> to <c>exponentBase10</c>.
		/// </remarks>
		public static bool TryParseFloatParts(ref UString source, int radix, out bool negative, out ulong mantissa, out int exponentBase2, out int exponentBase10, out int numDigits, ParseNumberFlag flags = 0)
		{
			int radixShift = 0;
			if (radix != 10) {
				radixShift = MathEx.Log2Floor(radix);
				if (radix > 32 || radix != 1 << radixShift)
					throw new ArgumentOutOfRangeException("radix");
			}

			int exponentBaseR;
			bool success = TryParseFloatParts(ref source, radix, out negative, out mantissa, out exponentBaseR, out exponentBase2, out exponentBase10, out numDigits, flags);

			try {
				checked {
					if (radix == 10)
						exponentBase10 += exponentBaseR;
					else
						exponentBase2 += exponentBaseR * radixShift;
				}
			} catch (OverflowException) {
				return false;
			}

			return success;
		}

		/// <summary>Parses a string to a double-precision float, returning NaN on 
		/// failure or an infinity value on overflow.</summary>
		/// <param name="radix">Base of the number to parse; must be 2 (binary), 
		/// 4, 8 (octal), 10 (decimal), 16 (hexadecimal) or 32.</param>
		/// <param name="flags">Alters parsing behavior, see <see cref="ParseFlags"/>.</param>
		public static double TryParseDouble(ref UString source, int radix, ParseNumberFlag flags = 0)
		{
			ulong mantissa;
			int exponentBase2, exponentBase10, numDigits;
			bool negative;
			if (!TryParseFloatParts(ref source, radix, out negative, out mantissa, out exponentBase2, out exponentBase10, out numDigits, flags))
				return double.NaN;
			else {
				double num = MathEx.ShiftLeft((double)mantissa, exponentBase2);
				if (negative)
					num = -num;
				if (exponentBase10 == 0)
					return num;
				return num * System.Math.Pow(10, exponentBase10);
			}
		}

		/// <summary>Parses a string to a single-precision float, returning NaN on 
		/// failure or an infinity value on overflow.</summary>
		/// <param name="radix">Base of the number to parse; must be 2 (binary), 
		/// 4, 8 (octal), 10 (decimal), 16 (hexadecimal) or 32.</param>
		/// <param name="flags">Alters parsing behavior, see <see cref="ParseFlags"/>.</param>
		public static float TryParseFloat(ref UString source, int radix, ParseNumberFlag flags = 0)
		{
			ulong mantissa;
			int exponentBase2, exponentBase10, numDigits;
			bool negative;
			if (!TryParseFloatParts(ref source, radix, out negative, out mantissa, out exponentBase2, out exponentBase10, out numDigits, flags))
				return float.NaN;
			else {
				float num = MathEx.ShiftLeft((float)mantissa, exponentBase2);
				if (negative)
					num = -num;
				if (exponentBase10 == 0)
					return num;
				return num * (float)System.Math.Pow(10, exponentBase10);
			}
		}

		/// <summary>Returns a string with any spaces and tabs removed from the beginning.</summary>
		/// <remarks>Only ' ' and '\t' are treated as spaces.</remarks>
		public static UString SkipSpaces(UString s)
		{
			char c;
			while ((c = s[0, '\0']) == ' ' || c == '\t')
				s = s.Substring(1);
			return s;
		}

		/// <summary>Converts an integer to a string, optionally with separator characters for readability.</summary>
		/// <param name="value">Integer to be converted</param>
		/// <param name="prefix">A prefix to insert before the number, but after the '-' sign, if any (e.g. "0x" for hex). Use "" for no prefix.</param>
		/// <param name="base">Number base (e.g. 10 for decimal, 2 for binary, 16 for hex). Must be in the range 2 to 36.</param>
		/// <param name="separatorInterval">Number of digits in a group</param>
		/// <param name="separatorChar">Digit group separator</param>
		/// <returns>The number as a string.</returns>
		/// <remarks>Example: <c>IntegerToString(-1234567, "0", 10, 3, '\'') == "-01'234'567"</c></remarks>
		public static string IntegerToString(long value, string prefix = "", int @base = 10, int separatorInterval = 3, char separatorChar = '_')
		{
			return AppendIntegerTo(new StringBuilder(), value, prefix, @base, separatorInterval, separatorChar).ToString();
		}
		public static string IntegerToString(ulong value, string prefix = "", int @base = 10, int separatorInterval = 3, char separatorChar = '_')
		{
			return AppendIntegerTo(new StringBuilder(), value, prefix, @base, separatorInterval, separatorChar).ToString();
		}

		/// <summary>Same as <see cref="IntegerToString(long, string, int, int, char)"/> 
		/// except that the target StringBuilder must be provided as a parameter.</summary>
		/// <param name="value">Integer to be converted</param>
		/// <param name="prefix">A prefix to insert before the number, but after the '-' sign, if any (e.g. "0x" for hex). Use "" for no prefix.</param>
		/// <param name="base">Number base (e.g. 10 for decimal, 2 for binary, 16 for hex). Must be in the range 2 to 36.</param>
		/// <param name="separatorInterval">Number of digits in a group</param>
		/// <param name="separatorChar">Digit group separator</param>
		/// <returns>The target StringBuilder.</returns>
		public static StringBuilder AppendIntegerTo(StringBuilder target, long value, string prefix = "", int @base = 10, int separatorInterval = 3, char separatorChar = '_')
		{
			if (value < 0) {
				CheckParam.IsInRange("base", @base, 2, 36);
				target.Append('-');
				target.Append(prefix);
				return AppendIntegerTo(target, (ulong)-value, "", @base, separatorInterval, separatorChar);
			} else 
				return AppendIntegerTo(target, (ulong)value, prefix, @base, separatorInterval, separatorChar);
		}
		
		public static StringBuilder AppendIntegerTo(StringBuilder target, ulong value, string prefix = "", int @base = 10, int separatorInterval = 3, char separatorChar = '_')
		{
			CheckParam.IsInRange("base", @base, 2, 36);
			target.Append(prefix);
			int iStart = target.Length;
			int counter = 0;
			int shift = MathEx.Log2Floor(@base);
			int mask = (1 << shift == @base ? (1 << shift) - 1 : 0);
			for (;;) {
				uint digit;
				if (mask != 0) {
					digit = (uint)value & (uint)mask;
					value >>= shift;
				} else {
					digit = (uint)(value % (uint)@base);
					value /= (uint)@base;
				}
				target.Append(HexDigitChar((int)digit));
				if (value == 0)
					break;
				if (++counter == separatorInterval) {
					counter = 0;
					target.Append(separatorChar);
				}
			}

			// Reverse the appended characters
			for (int i = ((target.Length - iStart) >> 1) - 1; i >= 0; i--)
			{
				int i1 = iStart + i, i2 = target.Length - 1 - i;
				char temp = target[i1];
				target[i1] = target[i2];
				target[i2] = temp;
			}
			return target;
		}
	}

	/// <summary>Flags to control <see cref="ParseHelpers.EscapeCStyle(UString, EscapeC)"/>.</summary>
	[Flags()]
	public enum EscapeC
	{
		/// <summary>Only \r, \n, \0 and backslash are escaped.</summary>
		Minimal = 0,  
		/// <summary>Default option for escaping</summary>
		Default = Control | DoubleQuotes | UnicodeNonCharacters | UnicodePrivateUse,
		/// <summary>Escape ALL characters with codes above 127 as \xNN or \uNNNN</summary>
		NonAscii = 1,
		/// <summary>Use \xNN instead of \u00NN for characters 1-31 and 127-255</summary>
		BackslashX = 2,
		/// <summary>Escape all characters with codes below 32, including \t</summary>
		Control = 4, 
		/// <summary>Use \a \b \f and \v (rather than \xNN or \xNN)</summary>
		ABFV = 8,
		/// <summary>Escape double quotes as \"</summary>
		DoubleQuotes = 16, 
		/// <summary>Escape single quotes as \'</summary>
		SingleQuotes = 32, 
		/// <summary>Escape single and double quotes</summary>
		Quotes = 48,
		/// <summary>Escape non-character unicode code points such as the Byte Order Mark 
		/// and unpaired surrogate pair characters.</summary>
		UnicodeNonCharacters = 64,
		/// <summary>Escape unicode private-use code points.</summary>
		UnicodePrivateUse = 128,
		/// <summary>While unescaping, a backslash was encountered.</summary>
		HasEscapes = 0x100, 
		/// <summary>While unescaping, an unrecognized escape was encountered .</summary>
		Unrecognized = 0x200,
		/// <summary>While unescaping, a valid \u escape was encountered with more than 4 digits.
		/// To detect whether the value was above 0xFFFF, however, one must check the output.</summary>
		HasLongEscape = 0x400,
		/// <summary>While unescaping, a valid \u escape was encountered with 6 digits, but the
		/// number was more than 0x10FFFF and had to be treated as 5 digits to make it valid.</summary>
		/// <remarks>Always appears with HasLongEscape | HasEscapes</remarks>
		HasInvalid6DigitEscape = 0x800,
	}

	/// <summary>Flags that can be used with 
	/// <see cref="ParseHelpers.TryParseUInt(UString, out ulong, int, ParseNumberFlag)"/>
	/// </summary>
	public enum ParseNumberFlag
	{
		/// <summary>Skip spaces before the number. Without this flag, initial spaces make parsing fail.</summary>
		SkipSpacesInFront = 1,
		/// <summary>Skip spaces inside the number. Without this flag, spaces make parsing stop.</summary>
		SkipSpacesInsideNumber = 2,
		/// <summary>Changes overflow handling behavior when parsing an integer,
		/// so that the result does not overflow (wrap), and the digit(s) at the 
		/// end of the string, that would have caused overflow, are ignored. In 
		/// this case, the return value is still false.</summary>
		StopBeforeOverflow = 4,
		/// <summary>Skip underscores inside number. Without this flag, underscores make parsing stop.</summary>
		SkipUnderscores = 8,
		/// <summary>Skip single quotes inside number. Without this flag, single quotes make parsing stop.</summary>
		SkipSingleQuotes = 16,
		/// <summary>Whether to treat comma as a decimal point when parsing a float. 
		/// The dot '.' is always treated as a decimal point.</summary>
		AllowCommaDecimalPoint = 32,
	}
}
