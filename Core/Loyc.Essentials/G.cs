using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using Loyc.MiniTest;
using Loyc.Math;
using Loyc.Threading;
using Loyc.Collections;

namespace Loyc
{
	public delegate string WriterDelegate(string format, params object[] args);

	/// <summary>Contains global functions that don't belong in any specific class.</summary>
	public static class G
	{
		public static void Swap<T>(ref T a, ref T b)
		{
			T tmp = a;
			a = b;
			b = tmp;
		}

		public static readonly object BoxedFalse = false;      //!< Singleton false cast to object.
		public static readonly object BoxedTrue = true;        //!< Singleton true cast to object.
		public static readonly object BoxedVoid = new @void(); //!< Singleton void cast to object.

		private class ComparisonFrom<T> where T : IComparable<T> 
		{
			public static readonly Comparison<T> C = GetC();
			public static readonly Func<T, T, int> F = GetF();
			static Comparison<T> GetC() {
				if (typeof(T).IsValueType)
					return (a, b) => a.CompareTo(b);
				return (Comparison<T>)Delegate.CreateDelegate(typeof(Comparison<T>), null, typeof(IComparable<T>).GetMethod("CompareTo"));
			}
			static Func<T, T, int> GetF() {
				if (typeof(T).IsValueType)
					return (a, b) => a.CompareTo(b);
				return (Func<T, T, int>)Delegate.CreateDelegate(typeof(Func<T, T, int>), null, typeof(IComparable<T>).GetMethod("CompareTo"));
			}
		}
		/// <summary>Gets a <see cref="Comparison{T}"/> for the specified type.</summary>
		/// <remarks>This method is optimized and does not allocate on every call.</remarks>
		public static Comparison<T> ToComparison<T>() where T : IComparable<T>
		{
			return ComparisonFrom<T>.C;
		}
		public static Func<T, T, int> ToComparisonFunc<T>() where T : IComparable<T>
		{
			return ComparisonFrom<T>.F;
		}
		/// <summary>Converts an <see cref="IComparer{T}"/> to a <see cref="Comparison{T}"/>.</summary>
		public static Comparison<T> ToComparison<T>(IComparer<T> pred)
		{
			return pred.Compare;
		}
		/// <summary>Converts an <see cref="IComparer{T}"/> to a <see cref="Func{T,T,int}"/>.</summary>
		public static Func<T, T, int> ToComparisonFunc<T>(IComparer<T> pred)
		{
			return pred.Compare;
		}
		
		public static List<string> SplitCommandLineArguments(string listString)
		{
			List<string> list = new List<string>();
			string regex = "(?=[^\\s\n])" // Match at least one non-whitespace character
			             + "[^\\s\n\"]*"  // Optional unquoted text
						   // (Optional quoted text, then optional unquoted text)*
			             + "(\"[^\"\n]*(\"[^\\s\n\"]*)?)*";

			Match m = Regex.Match(listString, regex, RegexOptions.IgnorePatternWhitespace);
			for (; m.Success; m = m.NextMatch()) {
				string s = m.ToString();
				if (s.StartsWith("\"") && s.EndsWith("\""))
					s = s.Substring(1, s.Length - 2);
				list.Add(s);
			}
			return list;
		}

		static char[] _invalids;

		/// <summary>Replaces characters in <c>text</c> that are not allowed in 
		/// file names with the specified replacement character.</summary>
		/// <param name="text">Text to make into a valid filename. The same string is returned if it is valid already.</param>
		/// <param name="replacement">Replacement character, or null to simply remove bad characters.</param>
		/// <param name="fancy">Whether to replace quotes and slashes with the non-ASCII characters ” and ⁄.</param>
		/// <returns>A string that can be used as a filename. If the output string would otherwise be empty, returns "_".</returns>
		public static string MakeValidFileName(string text, char? replacement = '_', bool fancy = true)
		{
			StringBuilder sb = new StringBuilder(text.Length);
			var invalids = _invalids ?? (_invalids = Path.GetInvalidFileNameChars());
			bool changed = false;
			for (int i = 0; i < text.Length; i++) {
				char c = text[i];
				if (invalids.Contains(c)) {
					var repl = replacement ?? '\0';
					if (fancy) {
						if (c == '"')       repl = '”'; // U+201D right double quotation mark
						else if (c == '\'') repl = '’'; // U+2019 right single quotation mark
						else if (c == '/')  repl = '⁄'; // U+2044 fraction slash
					}
					if (repl != '\0')
						sb.Append(repl);
				} else
					sb.Append(c);
			}
			if (sb.Length == 0)
				return "_";
			return changed ? sb.ToString() : text;
		}

		public static Pair<T1, T2> Pair<T1, T2>(T1 a, T2 b) { return new Pair<T1, T2>(a, b); }
		public static Triplet<T1, T2, T3> Triplet<T1, T2, T3>(T1 a, T2 b, T3 c) { return new Triplet<T1, T2, T3>(a, b, c); }

		/// <summary>Same as Debug.Assert except that the argument is evaluated 
		/// even in a Release build.</summary>
		public static bool Verify(bool condition)
		{
			Debug.Assert(condition);
			return condition;
		}
		public static void Require(bool condition)
		{
			if (!condition)
				throw new Exception(Localize.From("A required condition was false"));
		}
		public static void Require(bool condition, string msg)
		{
			if (!condition)
				throw new Exception(Localize.From("Error: {0}", msg));
		}

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
		public static char HexDigitChar(int value)
		{
			Debug.Assert((uint)value < 16);
			if ((uint)value < 10)
				return (char)('0' + value);
			else
				return (char)('A' - 10 + value);
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
					slice = s.USlice(i, 4);
					if (G.TryParseHex(slice, out code)) {
						i += slice.Length;
						return (char)code;
					} else
						break;
				case 'x':
					slice = s.USlice(i, 4);
					if (G.TryParseHex(slice, out code)) {
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

		/// <summary>Helper function for a using statement that temporarily 
		/// modifies a thread-local variable.</summary>
		/// <param name="variable">Variable to change</param>
		/// <param name="newValue">New value</param>
		/// <example>
		/// // Temporarily change the target of compiler error messages
		/// using (var _ = G.PushTLV(CompilerOutput.Writer, CustomWriter))
		/// {
		///		Warning.Write(SourcePos.Nowhere, "This message will go to a custom writer");
		/// }
		/// Warning.Write(SourcePos.Nowhere, "But this message will go to the original one");
		/// </example>
		public static PushedTLV<T> PushTLV<T>(ThreadLocalVariable<T> variable, T newValue)
		{
			return new PushedTLV<T>(variable, newValue);
		}


		/// <summary>Tries to parse a string to an integer. Unlike <see cref="Int32.TryParse(string, out int)"/>,
		/// this method allows parsing to start at any point in the string, it 
		/// allows non-numeric data after the number, and it can parse numbers that
		/// are not in base 10.</summary>
		/// <param name="radix">Number base, e.g. 10 for decimal and 2 for binary.</param>
		/// <param name="index">Location at which to start parsing</param>
		/// <param name="flags"><see cref="ParseFlag"/>s that affect parsing behavior.</param>
		/// <param name="skipSpaces">Whether to skip spaces before parsing. Only 
		/// the ' ' and '\t' characters are treated as spaces. No space is allowed 
		/// between '-' and the digits of a negative number, even with this flag.</param>
		/// <param name="stopBeforeOverflow">Changes overflow handling behavior
		/// so that the result does not overflow, and the digit(s) at the end of
		/// the string, that would have caused overflow, are ignored. In this case,
		/// the return value is still false.</param>
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
			UString slice = s.USlice(index);
			bool success = TryParseInt(ref slice, out result, radix, skipSpaces ? ParseFlag.SkipSpacesInFront : 0);
			index = slice.InternalStart;
			return success;
		}

		/// <inheritdoc cref="TryParseInt(string, ref int, out int, int, bool)"/>
		public static bool TryParseInt(ref UString s, out int result, int radix = 10, ParseFlag flags = 0)
		{
			long resultL;
			bool ok = TryParseInt(ref s, out resultL, radix, flags);
			result = (int)resultL;
			return ok && result == resultL;
		}

		/// <inheritdoc cref="TryParseInt(string, ref int, out int, int, bool)"/>
		public static bool TryParseInt(ref UString input, out long result, int radix = 10, ParseFlag flags = 0)
		{
			if ((flags & ParseFlag.SkipSpacesInFront) != 0)
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

		/// <summary>Flags that can be used with the overload of TryParseAt() 
		/// that parses unsigned integers.</summary>
		public enum ParseFlag
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

		public static bool TryParseUInt(ref UString s, out ulong result, int radix = 10, ParseFlag flags = 0)
		{
			int _;
			result = 0;
			return TryParseUInt(ref s, ref result, radix, flags, out _);
		}
		static bool TryParseUInt(ref UString s, ref ulong result, int radix, ParseFlag flags, out int numDigits)
		{
			numDigits = 0;
			if ((flags & ParseFlag.SkipSpacesInFront) != 0)
				s = SkipSpaces(s);
			
			bool overflow = false;
			int oldStart = s.InternalStart;
			
			for (;; s = s.Slice(1))
			{
				char c = s[0, '\0'];
				uint digit = (uint)Base36DigitValue(c);
				if (digit >= radix) {
					if ((c == ' ' || c == '\t') && (flags & ParseFlag.SkipSpacesInsideNumber) != 0)
						continue;
					else if (c == '_' && (flags & ParseFlag.SkipUnderscores) != 0)
						continue;
					else
						break;
				}

				ulong next;
				try {
					next = checked(result * (uint)radix + digit);
				} catch (OverflowException) {
					overflow = true;
					if ((flags & ParseFlag.StopBeforeOverflow) != 0)
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
		public static bool TryParseFloatParts(ref UString source, int radix, out bool negative, out ulong mantissa, out int exponentBaseR, out int exponentBase2, out int exponentBase10, out int numDigits, ParseFlag flags = 0)
		{
			flags |= G.ParseFlag.StopBeforeOverflow;

			if ((flags & ParseFlag.SkipSpacesInFront) != 0)
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
			if (c == '.' || (c == ',' && (flags & ParseFlag.AllowCommaDecimalPoint) != 0))
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

		private static int SkipExtraDigits(ref UString s, int radix, ParseFlag flags)
		{
			for (int skipped = 0;; skipped++, s = s.Slice(1)) {
				char c = s[0, '\0'];
				uint digit = (uint)Base36DigitValue(c);
				if (digit >= radix) {
					if ((c == ' ' || c == '\t') && (flags & ParseFlag.SkipSpacesInsideNumber) != 0)
						continue;
					else if (c == '_' && (flags & ParseFlag.SkipUnderscores) != 0)
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
		public static bool TryParseFloatParts(ref UString source, int radix, out bool negative, out ulong mantissa, out int exponentBase2, out int exponentBase10, out int numDigits, ParseFlag flags = 0)
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
		public static double TryParseDouble(ref UString source, int radix, ParseFlag flags = 0)
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
		public static float TryParseFloat(ref UString source, int radix, ParseFlag flags = 0)
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

		static Dictionary<char, string> HtmlEntityTable;

		/// <summary>Gets a bare HTML entity name for an ASCII character, or null if
		/// there is no entity name for the given character, e.g. <c>'&'=>"amp"</c>.
		/// </summary><remarks>
		/// The complete entity name is <c>"&" + GetHtmlEntityNameForAscii(c) + ";"</c>.
		/// Some HTML entities have multiple names; this function returns one of them.
		/// There is a name in this table for all ASCII punctuation characters.
		/// </remarks>
		public static string BareHtmlEntityNameForAscii(char c)
		{
			if (HtmlEntityTable == null)
				HtmlEntityTable = new Dictionary<char,string>() {
					{' ', "sp"},     {'!', "excl"},   {'\"', "quot"},  {'#', "num"},
					{'$', "dollar"}, {'%', "percnt"}, {'&', "amp"},    {'\'', "apos"},  
					{'(', "lpar"},   {')', "rpar"},   {'*', "ast"},    {'+', "plus"}, 
					{',', "comma"},  {'-', "dash"},   {'.', "period"}, {'/', "sol"},
					{':', "colon"},  {';', "semi"},   {'<', "lt"},     {'=', "equals"}, 
					{'>', "gt"},     {'?', "quest"},  {'@', "commat"}, 
					{'[', "lsqb"},   {'\\', "bsol"},  {']', "rsqb"},   {'^', "caret"}, 
					{'_', "lowbar"}, {'`', "grave"},  {'{', "lcub"},   {'}', "rcub"},
					{'|', "vert"},   {'~', "tilde"}, // {(char)0xA0, "nbsp"}
				};
			string name;
			HtmlEntityTable.TryGetValue(c, out name);
			return name;
		}
	}

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

	[TestFixture]
	public class GTests : Assert
	{
		[Test] public void TestSwap()
		{
			int a = 7, b = 13;
			G.Swap(ref a, ref b);
			Assert.AreEqual(7, b);
			Assert.AreEqual(13, a);
		}
		[Test] public void TestSplitCommandLineArguments()
		{
			// Give it some easy and some difficult arguments
			string input = "123: apple \t banana=\"a b\" Carrot\n"
				+ "!@#$%^&*() \"duck\" \"error \"foo\"!\ngrape   ";
			List<string> expected = new List<string>();
			expected.Add("123:");
			expected.Add("apple");
			expected.Add("banana=\"a b\"");
			expected.Add("Carrot");
			expected.Add("!@#$%^&*()");
			expected.Add("duck");
			expected.Add("\"error \"foo\"!");
			expected.Add("grape");
			
			List<string> output = G.SplitCommandLineArguments(input);
			Assert.AreEqual(output.Count, expected.Count);
			for (int i = 0; i < expected.Count; i++)
				Assert.AreEqual(output[i], expected[i]);
		}
		[Test] public void TestUnescape()
		{
			Assert.AreEqual("", G.UnescapeCStyle(""));
			Assert.AreEqual("foobar", G.UnescapeCStyle("foobar"));
			Assert.AreEqual("foo\nbar", G.UnescapeCStyle(@"foo\nbar"));
			Assert.AreEqual("\u2222\n\r\t", G.UnescapeCStyle(@"\u2222\n\r\t"));
			Assert.AreEqual("\a\b\f\vA", G.UnescapeCStyle(@"\a\b\f\v\x41"));
			Assert.AreEqual("ba\\z", G.UnescapeCStyle(@"ba\z", 0, 4, false));
			Assert.AreEqual("baz", G.UnescapeCStyle(@"ba\z", 0, 4, true));
			Assert.AreEqual("!!\n!!", G.UnescapeCStyle(@"<'!!\n!!'>", 2, 6, true));
		}

		[Test]
		public void TestTryParseInt()
		{
			TestParse(true, 10, "0", 0, 0, 1);
			TestParse(true, 10, "-0", 0, 0, 2);
			TestParse(true, 3, "0", 0, 0, 1, false);
			TestParse(true, 10, "123", 123, 0, 3);
			TestParse(true, 10, "??123", 123, 2, 5);
			TestParse(true, 10, "??  123abc", 123, 2, 7);
			TestParse(true, 10, "??\t 123  abc", 123, 2, 7);
			TestParse(true, 3, "210", 21, 0, 3);
			TestParse(true, 3, " \t 210", 21, 0, 6);
			TestParse(false,3, " \t 210", 0, 0, 0, false);
			TestParse(true, 10, "1 -2", -2, 1, 4, true);
			TestParse(false,10, "1 -2", 0, 1, 1, false);
			TestParse(true, 10, "1 -22", -22, 1, 5, true);
			TestParse(true, 4, "1 -22", -10, 1, 5, true);
			TestParse(false,4, "1 -22", 0, 1, 1, false);
			TestParse(true, 16, "F9", 0xF9, 0, 2);
			TestParse(true, 16, "f9", 0xF9, 0, 2);
			TestParse(true, 16, "abcdef", 0xabcdef, 0, 6);
			TestParse(true, 16, "ABCDEF", 0xabcdef, 0, 6);
			TestParse(true, 16, "abcdefgh", 0xabcdef, 0, 6);
			TestParse(true, 16, "ABCDEFGH", 0xabcdef, 0, 6);
			TestParse(true, 36, "az", 10*36+35, 0, 2);
			TestParse(true, 100, "AZ1234", 103501020304, 0, 6);
			TestParse(true, 100, " -AZ1234", -103501020304, 0, 8);
			string s;
			TestParse(true, 10, s = long.MaxValue.ToString(), long.MaxValue, 0, s.Length);
			TestParse(true, 10, s = long.MinValue.ToString(), long.MinValue, 0, s.Length);
			TestParse(true, 2, "111100010001000100010001000100010001", 0xF11111111, 0, 36);
			TestParse(false, 10, "", 0, 0, 0);
			TestParse(false, 10, "?!", 0, 0, 0);
			TestParse(false, 10, " eh?", 0, 0, 1);
			TestParse(false, 10, "123 eh?", 0, 3, 4);
			TestParse(false, 16, "10123456789abcdef", 0x0123456789abcdef, 0, 17);
			TestParse(false, 10, "- 1", 0, 0, 0, false);
			TestParse(true, 10, "- 1", -1, 0, 3);

			int i, result;
			i = 1;
			IsTrue(G.TryParseInt(" -AZ", ref i, out result, 100, false));
			AreEqual(-1035, result);
			AreEqual(i, 4);
			i = 0;
			IsFalse(G.TryParseInt(" -A123456Z", ref i, out result, 100, true));
			AreEqual(unchecked((int)-1001020304050635), result);
			AreEqual(i, 10);
			i = 1;
			IsTrue(G.TryParseInt(s = "0" + int.MinValue.ToString(), ref i, out result));
			AreEqual(int.MinValue, result);
			AreEqual(i, s.Length);
			i = 0;
			IsFalse(G.TryParseInt(s = ((long)int.MinValue - 1).ToString(), ref i, out result));
			AreEqual(int.MaxValue, result);
			AreEqual(i, s.Length);
		}
		private void TestParse(bool expectSuccess, int radix, string input, long expected, int i, int i_out, bool skipSpaces = true)
		{
			long result;
			UString input2 = input.USlice(i);
			bool success = G.TryParseInt(ref input2, out result, radix, 
				skipSpaces ? G.ParseFlag.SkipSpacesInFront : 0);
			AreEqual(expected, result);
			AreEqual(expectSuccess, success);
			AreEqual(i_out, input2.InternalStart);
		}

		[Test]
		public void TestTryParseFloat()
		{
			// First, let's make sure it handles integers
			TestParse(false, 10, "  ", float.NaN, G.ParseFlag.SkipSpacesInFront);
			TestParse(true, 10, "0", 0);
			TestParse(true, 10, "-0", 0);
			TestParse(true, 10, "123", 123);
			TestParse(true, 10, "  123", 123, G.ParseFlag.SkipSpacesInFront);
			TestParse(false, 10, "??  123abc".USlice(2), 123, G.ParseFlag.SkipSpacesInFront);
			TestParse(false, 10, "\t 123  abc", 123, G.ParseFlag.SkipSpacesInFront);
			TestParse(false, 10, "3 21  abc", 3);
			TestParse(false, 10, "3 21  abc", 321, G.ParseFlag.SkipSpacesInsideNumber);
			TestParse(true, 4, "210", 36);
			TestParse(true, 4, " \t 210", 36, G.ParseFlag.SkipSpacesInFront);
			TestParse(false,4, " \t 210", float.NaN);
			TestParse(true, 10, "-2", -2);
			TestParse(true, 10, "-22", -22);
			TestParse(true, 4, "-22", -10);
			TestParse(false,4, "-248", -2);
			TestParse(false,8, "-248", -20);
			TestParse(false, 16, "ab_cdef", 0xab);
			TestParse(true, 16, "ab_cdef", 0xabcdef, G.ParseFlag.SkipUnderscores);
			TestParse(true, 16, "_AB__CDEF_", 0xabcdef, G.ParseFlag.SkipUnderscores);
			TestParse(false, 16, "_", float.NaN, G.ParseFlag.SkipUnderscores);
			TestParse(false, 16, "aBcDeFgH", 0xabcdef);
			TestParse(true, 32, "av", 10*32+31);
			TestParse(true, 10, int.MaxValue.ToString(), (float)int.MaxValue);
			TestParse(true, 10, int.MinValue.ToString(), (float)int.MinValue);
			TestParse(true, 2, "111100010001000100010001000100010001", (float)0xF11111111);

			// Now for some floats...
			TestParse(true, 8, "0.4", 0.5f);
			TestParse(true, 10, "1.5", 1.5f);
			TestParse(true, 16, "2.C", 2.75f);
			TestParse(true, 2, "11.01", 3.25f);
			TestParse(false, 10, "123.456f", 123.456f);

			TestParse(true, 10, "+123.456e4", +123.456e4f);
			TestParse(true, 10, "-123.456e30", -123.456e30f);
			TestParse(true, 10, "123.456e+10", 123.456e+10f);
			TestParse(true, 10, "123.456e-10", 123.456e-10f);
			TestParse(false, 10, "123.456e-", float.NaN);
			TestParse(true, 10, "123.456p2", 123.456f * 4f);
			TestParse(true, 10, "123.456p+1", 123.456f * 2f);
			TestParse(true, 10, "123.456p-1", 123.456f * 0.5f);
			TestParse(false, 10, "123.456p*", float.NaN);
			TestParse(false, 10, "123.456p+", float.NaN);
			TestParse(true, 10, "123.456p-1e+3", 123456f * 0.5f);
			TestParse(false, 10, "123.456e+3p-1", 123456f); // this order is NOT supported
		
			TestParse(true, 16, "1.4", 1.25f);
			TestParse(true, 16, "123.456p12", (float)0x123456);
			TestParse(true, 16, "123.456p-12", (float)0x123456 / (float)0x1000000);
			TestParse(true, 16, "123p0e+1", (float)0x123 * 10f);
			TestParse(true, 2, "1111p+8", (float)0xF00);
			TestParse(true, 4, "32.10", 14.25f);
			TestParse(true, 4, "32.10e+4", 14.25f * 10000f);
			TestParse(true, 4, "32.10p+4", 14.25f * 16f);
			TestParse(true, 8, "32.10", 26.125f);
			TestParse(true, 8, "32.10e+4", 26.125f * 10000f);
			TestParse(true, 8, "32.10p+4", 26.125f * 16f);

			// Overflow, underflow, and truncation
			TestParse(true, 10, "9876543210", 9876543210f);
			TestParse(true, 10, "9876543210_98765.4321012", 987654321098765.4321012f, G.ParseFlag.SkipUnderscores);
			TestParse(true, 10, "1e40", float.PositiveInfinity);
			TestParse(true, 10, "-1e40", float.NegativeInfinity);
			TestParse(true, 10, "-1e-50", 0);
			TestParse(true, 10, "9876543210e5000", float.PositiveInfinity);
			TestParse(false, 10, "9876543210e9876543210", float.NaN);
			TestParse(false, 10, "9876543210p+9876543210", float.NaN);
			TestParse(true, 2, "11110001000100010001000100010001.0001", (float)0xF11111111 / 16f);
			TestParse(true, 10, "9876543210_9876543210.12345", 98765432109876543210.12345f, G.ParseFlag.SkipUnderscores);
			TestParse(true, 10, "12345.0123456789_0123456789", 12345.01234567890123456789f, G.ParseFlag.SkipUnderscores);
		}

		private void TestParse(bool expectSuccess, int radix, UString input, float expected, G.ParseFlag flags = 0)
		{
			float result = G.TryParseFloat(ref input, radix, flags);
			bool success = !float.IsNaN(result) && input.IsEmpty;
			AreEqual(expectSuccess, success);
			IsTrue(expected == result
			    || expected == MathEx.NextLower(result)
			    || expected == MathEx.NextHigher(result)
				|| float.IsNaN(expected) && float.IsNaN(result));
		}
	}
}
