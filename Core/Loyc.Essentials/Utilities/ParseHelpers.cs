using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Loyc.Math;

namespace Loyc.Syntax
{
	/// <summary>Static methods that help with common parsing jobs, such as 
	/// parsing integers, floats, and strings with C escape sequences.</summary>
	/// <remarks>This class also contains a few inverse methods, e.g. 
	/// <see cref="EscapeCStyle(UString, EscapeC)"/> is the inverse of the
	/// C-style string parser <see cref="UnescapeCStyle"/>.</remarks>
	public static class ParseHelpers
	{
		public static bool TryParseHex(UString s, out int value)
		{
			int count = TryParseHex(ref s, out value);
			return count > 0 && s.IsEmpty;
		}
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
					value = value * 16 + digit;
			}
		}
		/// <summary>Gets the integer value for the specified hex digit, or -1 if the character is not a hex digit.</summary>
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
		/// <summary>Gets the hex digit character for the specified value, or '?' if the value is not in the range 0...15.</summary>
		public static char HexDigitChar(int value)
		{
			if ((uint)value < 10)
				return (char)('0' + value);
			else if ((uint)value < 16)
				return (char)('A' - 10 + value);
			else
				return '?';
		}

		public static string EscapeCStyle(UString s, EscapeC flags = EscapeC.Default)
		{
			return EscapeCStyle(s, flags, '\0');
		}
		public static string EscapeCStyle(UString s, EscapeC flags, char quoteType)
		{
			StringBuilder s2 = new StringBuilder(s.Length+1);
			bool any = false;
			for (int i = 0; i < s.Length; i++) {
				char c = s[i];
				any |= EscapeCStyle(c, s2, flags, quoteType);
			}
			if (!any && s.InternalString.Length == s.Length)
				return s.InternalString;
			return s2.ToString();
		}

		public static bool EscapeCStyle(char c, StringBuilder @out, EscapeC flags = EscapeC.Default, char quoteType = '\0')
		{
			if (c > 255 && (flags & (EscapeC.Unicode | EscapeC.NonAscii)) != 0) {
				@out.AppendFormat((IFormatProvider)null, @"\u{0:x0000}", (int)c);
			} else if (c == '\"' && (flags & EscapeC.DoubleQuotes) != 0) {
				@out.Append("\\\"");
			} else if (c == '\'' && (flags & EscapeC.SingleQuotes) != 0) {
				@out.Append("\\'");
			} else if (c == quoteType) {
				@out.Append('\\');
				@out.Append(c);
			}
			else if (c < 32)
			{
				if (c == '\n')
					@out.Append(@"\n");
				else if (c == '\r')
					@out.Append(@"\r");
				else if (c == '\0')
					@out.Append(@"\0");
				else {
					if ((flags & EscapeC.ABFV) != 0)
					{
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
					if ((flags & EscapeC.Control) != 0)
					{
						if (c == '\t')
							@out.Append(@"\t");
						else
							@out.AppendFormat(null, @"\x{0:X2}", (int)c);
					}
					else
						@out.Append(c);
				}
			}
			else if (c == '\\') {
				@out.Append(@"\\");
			} else if (c > 127 && (flags & EscapeC.NonAscii) != 0) {
				@out.AppendFormat(null, @"\x{0:X2}", (int)c);
			} else {
				@out.Append(c);
				return false;
			}
			return true;
		}
		/// <summary>Unescapes a string that uses C-style escape sequences, e.g. "\n\r" becomes @"\n\r".</summary>
		public static string UnescapeCStyle(string s)
		{
			return UnescapeCStyle(s, 0, s.Length, false);
		}

		/// <summary>Unescapes a string that uses C-style escape sequences, e.g. "\n\r" becomes @"\n\r".</summary>
		/// <param name="removeUnnecessaryBackslashes">Causes the backslash before 
		/// an unrecognized escape sequence to be removed, e.g. "\z" => "z".</param>
		public static string UnescapeCStyle(string s, int index, int length, bool removeUnnecessaryBackslashes)
		{
			StringBuilder s2 = new StringBuilder(length);
			for (int i = index; i < index + length; ) {
				int oldi = i;
				char c = UnescapeChar(s, ref i);
				if (removeUnnecessaryBackslashes && c == '\\' && i == oldi + 1)
					continue;
				s2.Append(c);
			}
			return s2.ToString();
		}

		public static char UnescapeChar(ref UString s)
		{
			int i = s.InternalStart, i0 = i;
			char c = UnescapeChar(s.InternalString, ref i);
			s = new UString(s.InternalString, i, s.Length - (i - i0));
			return c;
		}

		/// <summary>Unescapes a single character of a string, e.g. 
		/// <c>int = 3; UnescapeChar("foo\\n", ref i) == '\n'</c>. Returns the 
		/// character at 'index' if it is not a backslash, or if it is a 
		/// backslash but no escape sequence could be discerned.</summary>
		/// <exception cref="IndexOutOfRangeException">The index was invalid.</exception>
		public static char UnescapeChar(string s, ref int i)
		{
			char c = s[i++];
			if (c == '\\' && i < s.Length) {
				int code;
				UString slice;
				switch (s[i++]) {
				case 'u':
					slice = s.Slice(i, 4);
					if (TryParseHex(slice, out code)) {
						i += slice.Length;
						return (char)code;
					} else
						break;
				case 'x':
					slice = s.Slice(i, 4);
					if (TryParseHex(slice, out code)) {
						i += slice.Length;
						return (char)code;
					} else
						break;
				case '\\':
					return '\\';
				case '\"':
					return '\"';
				case '\'':
					return '\'';
				case '`':
					return '`';
				case 't':
					return '\t';
				case 'n':
					return '\n';
				case 'r':
					return '\r';
				case 'a':
					return '\a';
				case 'b':
					return '\b';
				case 'f':
					return '\f';
				case 'v':
					return '\v';
				case '0':
					return '\0';
				}
				i--;
			}
			return c;
		}

		/// <summary>Tries to parse a string to an integer. Unlike <see cref="Int32.TryParse(string, out int)"/>,
		/// this method allows parsing to start at any point in the string, it 
		/// allows non-numeric data after the number, and it can parse numbers that
		/// are not in base 10.</summary>
		/// <param name="radix">Number base, e.g. 10 for decimal and 2 for binary.</param>
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
			int oldStart = s.InternalStart;
			
			for (;; s = s.Slice(1))
			{
				char c = s[0, '\0'];
				uint digit = (uint)Base36DigitValue(c);
				if (digit >= radix) {
					if ((c == ' ' || c == '\t') && (flags & ParseNumberFlag.SkipSpacesInsideNumber) != 0)
						continue;
					else if (c == '_' && (flags & ParseNumberFlag.SkipUnderscores) != 0)
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
		/// where Digits refers to one digits in the requested base, possibly 
		/// including underscores or spaces if the flags allow it; similarly, 
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

		/// <summary>Gets the index of the first non-space character after the specified index.</summary>
		/// <remarks>Only ' ' and '\t' are treated as spaces. If the index is invalid, it is returned unchanged.</remarks>
		public static int SkipSpaces(string s, int index)
		{
			for (;;) {
				if ((uint)index >= (uint)s.Length)
					break;
				char c = s[index];
				if (c == ' ' || c == '\t')
					index++;
				else
					break;
			}
			return index;
		}

		/// <summary>Returns a string with any spaces and tabs removed from the beginning.</summary>
		public static UString SkipSpaces(UString s)
		{
			char c;
			while ((c = s[0, '\0']) == ' ' || c == '\t')
				s = s.Slice(1);
			return s;
		}
	}

	/// <summary>Flags to control <see cref="ParseHelpers.EscapeCStyle(UString, EscapeC)"/>.</summary>
	[Flags()]
	public enum EscapeC
	{
		Minimal = 0,  // Only \r, \n, \0 and backslash are escaped.
		Default = Control | Quotes,
		Unicode = 2,  // Escape all characters with codes above 255 as \uNNNN
		NonAscii = 1, // Escape all characters with codes above 127 as \xNN
		Control = 4,  // Escape all characters with codes below 32  as \xNN, and also \t
		ABFV = 8,     // Use \a \b \f and \v (overrides \xNN)
		DoubleQuotes = 16, // Escape double quotes as \"
		SingleQuotes = 32, // Escape single quotes as \'
		Quotes = 48,
	}

	/// <summary>Flags that can be used with 
	/// <see cref="ParseHelpers.TryParseUInt(UString, out ulong, int, ParseNumberFlag)"/>
	/// </summary>
	public enum ParseNumberFlag
	{
		/// <summary>Skip spaces before the number. Without this flag, spaces make parsing fail.</summary>
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
		/// <summary>Whether to treat comma as a decimal point when parsing a float. 
		/// The dot '.' is always treated as a decimal point.</summary>
		AllowCommaDecimalPoint = 8,
	}
}
