using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Loyc.Essentials;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using Loyc.Math;
using Loyc.Threading;

namespace Loyc
{
	public delegate string WriterDelegate(string format, params object[] args);

	/// <summary>Contains global functions that don't really belong in any class.</summary>
	public static class G
	{
		public static void Swap<T>(ref T a, ref T b)
		{
			T tmp = a;
			a = b;
			b = tmp;
		}
		public static int BinarySearch<T>(IList<T> list, T value, Comparison<T> pred)
		{
			int lo = 0, hi = list.Count, i;
			while(lo < hi) 
			{
				i = (lo+hi)/2;
				int cmp = pred(list[i], value);
				if (cmp < 0)
					lo = i+1;
				else if (cmp > 0)
					hi = i;
				else
					return i;
			}
			return ~lo;
		}
		public static int BinarySearch<T>(IList<T> list, T value) where T : IComparable<T>
		{
			return BinarySearch<T>(list, value, ToComparison<T>());
		}
		public static int BinarySearch<T>(IList<T> list, T value, IComparer<T> pred)
		{
			return BinarySearch<T>(list, value, ToComparison(pred));
		}
		public static Comparison<T> ToComparison<T>(IComparer<T> pred)
		{
			return delegate(T a, T b) { return pred.Compare(a, b); };
		}
		public static Comparison<T> ToComparison<T>() where T : IComparable<T>
		{
			return delegate(T a, T b) { return a.CompareTo(b); };
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


		public static Pair<T1, T2> Pair<T1, T2>(T1 a, T2 b) { return new Pair<T1, T2>(a, b); }
		public static Triplet<T1, T2, T3> Triplet<T1, T2, T3>(T1 a, T2 b, T3 c) { return new Triplet<T1, T2, T3>(a, b, c); }

		/// <summary>Same as Debug.Assert except that the argument is evaluated 
		/// even in a Release build.</summary>
		public static bool Verify(bool condition)
		{
			Debug.Assert(condition);
			return condition;
		}
		public static void RequireArg(bool condition)
		{
			if (!condition)
				throw new ArgumentException();
		}
		public static void RequireArg(bool condition, string argName, object argValue)
		{
			if (!condition)
				throw new ArgumentException(Localize.From("Invalid argument ({0} = '{1}')", argName, argValue));
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

		public static int TryParseHex(string s, out int value)
			{ return TryParseHex(s, 0, out value); }
		public static int TryParseHex(string s, int startAt, out int value)
		{
			value = 0;
			for (int i = startAt; i < s.Length; i++)
			{
				int digit = HexDigitValue(s[i]);
				if (digit == -1)
					return i - startAt;
				else
					value = value * 16 + digit;
			}
			return s.Length - startAt;
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
		public static char HexDigitChar(int value)
		{
			Debug.Assert((uint)value < 16);
			if ((uint)value < 10)
				return (char)('0' + value);
			else
				return (char)('A' - 10 + value);
		}

		public static string EscapeCStyle(string s, EscapeC flags, char quoteType = '\0')
		{
			StringBuilder s2 = new StringBuilder(s.Length+1);
			
			for (int i = 0; i < s.Length; i++) {
				char c = s[i];
				if (c > 255 && (flags & (EscapeC.Unicode | EscapeC.NonAscii)) != 0) {
					s2.AppendFormat((IFormatProvider)null, @"\u{0:x0000}", (int)c);
				} else if (c == '\"' && (flags & EscapeC.DoubleQuotes) != 0) {
					s2.Append("\\\"");
				} else if (c == '\'' && (flags & EscapeC.SingleQuotes) != 0) {
					s2.Append("\\\'");
				} else if (c == quoteType) {
					s2.Append('\\');
					s2.Append(c);
				} else if (c < 32) {
					if (c == '\n')
						s2.Append(@"\n");
					else if (c == '\r')
						s2.Append(@"\r");
					else if (c == '\0')
						s2.Append(@"\0");
					else {
						if ((flags & EscapeC.ABFV) != 0) {
							if (c == '\a') { // 7 (alert)
								s2.Append(@"\a");
								continue;
							} 
							if (c == '\b') { // 8 (backspace)
								s2.Append(@"\b"); 
								continue;
							} 
							if (c == '\f') { // 12 (form feed)
								s2.Append(@"\f"); 
								continue; 
							} 
							if (c == '\v') { // 11 (vertical tab)
								s2.Append(@"\v"); 
								continue;
							} 
						}
						if ((flags & EscapeC.Control) != 0) {
							if (c == '\t')
								s2.Append(@"\t");
							else
								s2.AppendFormat(null, @"\x{0:X2}", (int)c);
						} else
							s2.Append(c);
					}
				} else if (c == '\\') {
					s2.Append(@"\\");
				} else if (c > 127 && (flags & EscapeC.NonAscii) != 0) {
					s2.AppendFormat(null, @"\x{0:X2}", (int)c);
				} else
					s2.Append(c);
			}
			return s2.ToString();
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

		/// <summary>Unescapes a single character of a string, e.g. 
		/// <c>int = 3; UnescapeChar("foo\\n", ref i) == '\n'</c>. Returns the 
		/// character at 'index' if it is not a backslash, or if it is a 
		/// backslash but no escape sequence could be discerned.</summary>
		/// <exception cref="IndexOutOfRangeException">The index was invalid.</exception>
		public static char UnescapeChar(string s, ref int i)
		{
			char c = s[i++];
			if (c == '\\' && i < s.Length) {
				int len, code;
				switch (s[i++]) {
				case 'u':
					len = System.Math.Min(4, s.Length - i);
					i += G.TryParseHex(s.Substring(i, len), out code);
					return (char)code;
				case 'x':
					len = System.Math.Min(2, s.Length - i);
					i += G.TryParseHex(s.Substring(i, len), out code);
					return (char)code;
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
				default:
					i--; break;
				}
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


		/// <summary>Tries to parse a string to an integer. Unlike <see cref="Int32.TryParse"/>,
		/// this method allows parsing to start at any point in the string, it 
		/// allows non-numeric data after the number, and it can parse numbers that
		/// are not in base 10.</summary>
		/// <param name="base">Number base, e.g. 10 for decimal and 2 for binary.</param>
		/// <param name="index">Location at which to start parsing</param>
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
		public static bool TryParseAt(string s, ref int index, out int result, int @base = 10, bool skipSpaces = true)
		{
			long resultL;
			bool ok = TryParseAt(s, ref index, out resultL, @base, skipSpaces);
			result = (int)resultL;
			return ok && result == resultL;
		}
		/// <inheritdoc cref="TryParseAt(this string, ref int, out int, int, bool)"/>
		public static bool TryParseAt(string s, ref int index, out long result, int @base = 10, bool skipSpaces = true)
		{
			if (skipSpaces)
				index = SkipSpaces(s, index);

			if ((uint)index >= (uint)s.Length) {
				result = 0;
				return false;
			}

			bool negative = false;
			int i = index;
			if (s[i] == '-') {
				negative = true;
				i++;
			}

			ulong resultU;
			bool ok = TryParseAt(s, ref i, out resultU, @base, 0);
			result = negative ? -(long)resultU : (long)resultU;
			if (!(negative && i == index + 1))
				index = i;
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
			/// <summary>Changes overflow handling behavior so that the result does 
			/// not overflow, and the digit(s) at the end of the string, that would 
			/// have caused overflow, are ignored. In this case, the return value is 
			/// still false.</summary>
			StopBeforeOverflow = 4,
			/// <summary>Skip underscores inside number. Without this flag, underscores make parsing stop.</summary>
			SkipUnderscores = 8,
		}

		/// <inheritdoc cref="TryParseAt(this string, ref int, out int, int, bool)"/>
		public static bool TryParseAt(string s, ref int index, out ulong result, int @base = 10, ParseFlag flags = 0)
		{
			result = 0;
			if ((flags & ParseFlag.SkipSpacesInFront) != 0)
				index = SkipSpaces(s, index);
			
			bool overflow = false;
			int i;
			for (i = index; (uint)i < (uint)s.Length; i++)
			{
				uint digit;
				char c = s[i];
				if (c >= '0' && c <= '9') {
					digit = (uint)(c - '0');
                } else if ((c == ' ' || c == '\t') && (flags & ParseFlag.SkipSpacesInsideNumber) != 0)
                    continue;
                else if (c == '_' && (flags & ParseFlag.SkipUnderscores) != 0)
                    continue;
                else if (@base > 10) {
					if (c >= 'a' && c <= 'z')
						digit = (uint)(c - ('a' - 10));
					else if (c >= 'A' && c <= 'Z')
						digit = (uint)(c - ('A' - 10));
					else {
						break;
					}
					if (digit >= @base)
						break;
				} else
					break;

				ulong next;
				try {
					next = checked(result * (uint)@base + digit);
				} catch (OverflowException) {
					overflow = true;
					if ((flags & ParseFlag.StopBeforeOverflow) != 0) {
						index = i;
						return false;
					}
					next = result * (uint)@base + digit;
				}
				result = next;
			}
			if (i == index) {
				Debug.Assert(result == 0);
				return false;
			}
			index = i;
			return !overflow;
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
	}

	[Flags()]
	public enum EscapeC
	{
		Minimal = 0,  // Only \r, \n, \0 and backslash are escaped.
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
		[Test] public void TestBinarySearch()
		{
			int[] list = new int[] { };
			Assert.AreEqual(~0, G.BinarySearch(list, 15));
			Assert.AreEqual(~0, G.BinarySearch(list, -15));
			list = new int[] { 5 };
			Assert.AreEqual(0, G.BinarySearch(list, 5));
			Assert.AreEqual(~0, G.BinarySearch(list, 0));
			Assert.AreEqual(~1, G.BinarySearch(list, 10));
			list = new int[] { 5, 7 };
			Assert.AreEqual(~0, G.BinarySearch(list, 0));
			Assert.AreEqual( 0, G.BinarySearch(list, 5));
			Assert.AreEqual(~1, G.BinarySearch(list, 6));
			Assert.AreEqual( 1, G.BinarySearch(list, 7));
			Assert.AreEqual(~2, G.BinarySearch(list, 10));
			list = new int[] { 1, 5, 7, 13, 17, 29, 29, 31 };
			Assert.AreEqual(~0, G.BinarySearch(list, -1));
			Assert.AreEqual( 0, G.BinarySearch(list, 1));
			Assert.AreEqual(~1, G.BinarySearch(list, 2));
			Assert.AreEqual( 1, G.BinarySearch(list, 5));
			Assert.AreEqual(~2, G.BinarySearch(list, 6));
			Assert.AreEqual( 2, G.BinarySearch(list, 7));
			Assert.AreEqual(~3, G.BinarySearch(list, 10));
			Assert.AreEqual( 3, G.BinarySearch(list, 13));
			Assert.AreEqual(~4, G.BinarySearch(list, 16));
			Assert.AreEqual( 4, G.BinarySearch(list, 17));
			Assert.AreEqual(~5, G.BinarySearch(list, 28));
			int i = G.BinarySearch(list, 29);
			Assert.IsTrue(i == 5 || i == 6);
			Assert.AreEqual(~7, G.BinarySearch(list, 30));
			Assert.AreEqual( 7, G.BinarySearch(list, 31));
			Assert.AreEqual(~8, G.BinarySearch(list, 1000));
		}
		[Test] public void TestPredicatedBinarySearch()
		{
			Comparison<int> p = G.ToComparison<int>();
			int[] list = new int[] { };
			Assert.AreEqual(~0, G.BinarySearch<int>(list, 15, p));
			Assert.AreEqual(~0, G.BinarySearch<int>(list, -15, p));
			list = new int[] { 5 };
			Assert.AreEqual(0, G.BinarySearch<int>(list, 5, p));
			Assert.AreEqual(~0, G.BinarySearch<int>(list, 0, p));
			Assert.AreEqual(~1, G.BinarySearch<int>(list, 10, p));
			list = new int[] { 5, 7 };
			Assert.AreEqual(~0, G.BinarySearch<int>(list, 0, p));
			Assert.AreEqual( 0, G.BinarySearch<int>(list, 5, p));
			Assert.AreEqual(~1, G.BinarySearch<int>(list, 6, p));
			Assert.AreEqual( 1, G.BinarySearch<int>(list, 7, p));
			Assert.AreEqual(~2, G.BinarySearch<int>(list, 10, p));
			list = new int[] { 1, 5, 7, 13, 17, 29, 29, 31 };
			Assert.AreEqual(~0, G.BinarySearch<int>(list, -1, p));
			Assert.AreEqual( 0, G.BinarySearch<int>(list, 1, p));
			Assert.AreEqual(~1, G.BinarySearch<int>(list, 2, p));
			Assert.AreEqual( 1, G.BinarySearch<int>(list, 5, p));
			Assert.AreEqual(~2, G.BinarySearch<int>(list, 6, p));
			Assert.AreEqual( 2, G.BinarySearch<int>(list, 7, p));
			Assert.AreEqual(~3, G.BinarySearch<int>(list, 10, p));
			Assert.AreEqual( 3, G.BinarySearch<int>(list, 13, p));
			Assert.AreEqual(~4, G.BinarySearch<int>(list, 16, p));
			Assert.AreEqual( 4, G.BinarySearch<int>(list, 17, p));
			Assert.AreEqual(~5, G.BinarySearch<int>(list, 28, p));
			int i = G.BinarySearch<int>(list, 29, p);
			Assert.IsTrue(i == 5 || i == 6);
			Assert.AreEqual(~7, G.BinarySearch<int>(list, 30, p));
			Assert.AreEqual( 7, G.BinarySearch<int>(list, 31, p));
			Assert.AreEqual(~8, G.BinarySearch<int>(list, 1000, p));
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
		public void TestTryParseAt()
		{
			TestParse(true, "0", 0, 0, 1);
			TestParse(true, "-0", 0, 0, 2);
			TestParse(true, "0", 0, 0, 1, 3, false);
			TestParse(true, "123", 123, 0, 3);
			TestParse(true, "??123", 123, 2, 5);
			TestParse(true, "??  123abc", 123, 2, 7);
			TestParse(true, "??\t 123  abc", 123, 2, 7);
			TestParse(true, "210", 21, 0, 3, 3);
			TestParse(true, " \t 210", 21, 0, 6, 3);
			TestParse(false," \t 210", 0, 0, 0, 3, false);
			TestParse(true, "1 -2", -2, 1, 4, 10, true);
			TestParse(false,"1 -2", 0, 1, 1, 10, false);
			TestParse(true, "1 -22", -22, 1, 5, 10, true);
			TestParse(true, "1 -22", -10, 1, 5, 4, true);
			TestParse(false,"1 -22", 0, 1, 1, 4, false);
			TestParse(true, "F9", 0xF9, 0, 2, 16);
			TestParse(true, "f9", 0xF9, 0, 2, 16);
			TestParse(true, "abcdef", 0xabcdef, 0, 6, 16);
			TestParse(true, "ABCDEF", 0xabcdef, 0, 6, 16);
			TestParse(true, "abcdefgh", 0xabcdef, 0, 6, 16);
			TestParse(true, "ABCDEFGH", 0xabcdef, 0, 6, 16);
			TestParse(true, "az", 10*36+35, 0, 2, 36);
			TestParse(true, "AZ1234", 103501020304, 0, 6, 100);
			TestParse(true, " -AZ1234", -103501020304, 0, 8, 100);
			string s;
			TestParse(true, s = long.MaxValue.ToString(), long.MaxValue, 0, s.Length);
			TestParse(true, s = long.MinValue.ToString(), long.MinValue, 0, s.Length);
			TestParse(true, "111100010001000100010001000100010001", 0xF11111111, 0, 36, 2);
			TestParse(false, "", 0, 0, 0);
			TestParse(false, "?!", 0, 0, 0);
			TestParse(false, " eh?", 0, 0, 1);
			TestParse(false, "123 eh?", 0, 3, 4);
			TestParse(false, "10123456789abcdef", 0x0123456789abcdef, 0, 17, 16);
			TestParse(false, "- 1", 0, 0, 0);

			int i, result;
			i = 1;
			IsTrue(G.TryParseAt(" -AZ", ref i, out result, 100, false));
			AreEqual(-1035, result);
			AreEqual(i, 4);
			i = 0;
			IsFalse(G.TryParseAt(" -A123456Z", ref i, out result, 100, true));
			AreEqual(unchecked((int)-1001020304050635), result);
			AreEqual(i, 10);
			i = 1;
			IsTrue(G.TryParseAt(s = "0" + int.MinValue.ToString(), ref i, out result));
			AreEqual(int.MinValue, result);
			AreEqual(i, s.Length);
			i = 0;
			IsFalse(G.TryParseAt(s = ((long)int.MinValue - 1).ToString(), ref i, out result));
			AreEqual(int.MaxValue, result);
			AreEqual(i, s.Length);
		}
		private void TestParse(bool expectSuccess, string input, long expected, int i, int i_out, int @base = 10, bool skipSpaces = true)
		{
			long result;
			bool success = G.TryParseAt(input, ref i, out result, @base, skipSpaces);
			AreEqual(expected, result);
			AreEqual(expectSuccess, success);
			AreEqual(i_out, i);
		}
	}
}
