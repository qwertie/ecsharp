using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Loyc.Syntax
{
	/// <summary>Static methods that help with common parsing jobs, such as 
	/// parsing integers, floats, and strings with C escape sequences.</summary>
	/// <seealso cref="PrintHelpers"/>
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
		/// <remarks>See <see cref="UnescapeChar(ref UString, ref EscapeC)"/> for details.</remarks>
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
		/// Contract.Assert(str == "foo");
		/// Contract.Assert(e == EscapeC.HasEscapes);
		/// </example>
		public static int UnescapeChar(ref UString s, ref EscapeC encountered)
		{
			bool fail;
			int c = s.PopFirst(out fail);
			if (c != '\\' || s.Length <= 0)
				return c;

			encountered |= EscapeC.HasEscapes;
			int code; // hex code after \u or \x or \U
			int len; // length of hex code after \u or \x or \U
			UString slice, original = s;
			int type = s.PopFirst(out fail);
			switch (type) {
				case 'x':
					encountered |= EscapeC.BackslashX;
					goto case 'u';
				case 'u':
					len = type == 'u' ? 4 : 2;
					slice = s.Left(len);
					if (TryParseHex(ref slice, out code) == len) {
						s = s.Substring(slice.InternalStart - s.InternalStart);

						if (code < 32)
							encountered |= EscapeC.Control;
						else if (code > 127)
							encountered |= EscapeC.NonAscii;
						return code;
					} else
						break;
				case 'U':
					encountered |= EscapeC.HasLongEscape;
					slice = s.Left(6);
					len = TryParseHex(ref slice, out code);
					if (len > 0) {
						if (code <= 0x10FFFF) {
							s = s.Substring(len);
						} else {
							Debug.Assert(slice.Length == 0);
							// It appears to be 6 digits but only the first 5 can 
							// be treated as part of the escape sequence.
							encountered |= EscapeC.HasInvalid6DigitEscape;
							s = s.Substring(5);
							code >>= 4;
						}
						if (code < 32)
							encountered |= EscapeC.Control;
						else if (code > 127)
							encountered |= EscapeC.NonAscii;
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
		/// <param name="flags">Alters parsing behavior, see <see cref="ParseNumberFlag"/>.</param>
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
		/// <param name="flags">Alters parsing behavior, see <see cref="ParseNumberFlag"/>.</param>
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
				radixShift = G.Log2Floor(radix);
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
		/// <param name="flags">Alters parsing behavior, see <see cref="ParseNumberFlag"/>.</param>
		public static double TryParseDouble(ref UString source, int radix, ParseNumberFlag flags = 0)
		{
			ulong mantissa;
			int exponentBase2, exponentBase10, numDigits;
			bool negative;
			if (!TryParseFloatParts(ref source, radix, out negative, out mantissa, out exponentBase2, out exponentBase10, out numDigits, flags))
				return double.NaN;
			else {
				double num = G.ShiftLeft((double)mantissa, exponentBase2);
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
		/// <param name="flags">Alters parsing behavior, see <see cref="ParseNumberFlag"/>.</param>
		public static float TryParseFloat(ref UString source, int radix, ParseNumberFlag flags = 0)
		{
			ulong mantissa;
			int exponentBase2, exponentBase10, numDigits;
			bool negative;
			if (!TryParseFloatParts(ref source, radix, out negative, out mantissa, out exponentBase2, out exponentBase10, out numDigits, flags))
				return float.NaN;
			else {
				float num = (float)G.ShiftLeft(mantissa, exponentBase2);
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
	}

	/// <summary>Flags that can be used with 
	/// <see cref="ParseHelpers.TryParseUInt(ref UString, out ulong, int, ParseNumberFlag)"/>
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
