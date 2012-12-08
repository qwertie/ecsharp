namespace Loyc.Utilities
{
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

        /// <summary>
        /// Expands environment variables (e.g. %TEMP%) and @files in a list of
        /// command-line arguments, and adds any options of the form "--opt" or
        /// "--opt=value" to a dictionary.
        /// </summary>
        /// <param name="args">The original arguments to process</param>
        /// <param name="options">Any --options found will go here, and their keys
        /// will be converted to lowercase, unless this parameter is null. Note that
        /// options are not removed or converted to lowercase in the original args
        /// list.</param>
        /// <param name="atPath">If a parameter has the form @filename, the folder
        /// specified by atPath will be searched for an options text file with that
        /// filename, and the contents of the file will be expanded into the list of
        /// arguments (split using SplitCommandLineArguments).</param>
        /// <param name="argLimit">A limit placed on the number of arguments when
        /// expanding @files. Such a file may refer to itself, and this is the only
        /// protection provided against infinite recursive expansion.</param>
        /// <remarks>
        /// Options are expected to have the form -ID=value, where ID matches the
        /// regex "[a-zA-Z_0-9]+". If there is no "=", that's okay too. For example,
        /// --ID{foo} is equivalent to --Id={foo}; both result in the name-value
        /// pair ("id", "{foo}").
        /// </remarks>
		public static void ProcessCommandLineArguments(List<string> args, Dictionary<string, string> options, string atPath, int argLimit)
		{
			for (int i = 0; i < args.Count; i++)
				if (ProcessArgument(args, i, options, atPath, argLimit))
					i--; // redo
		}
		public static readonly Regex CmdLineArgRegex = new Regex(@"^--([a-zA-Z_0-9]+)([=]?(.*))?$");

		private static bool ProcessArgument(List<string> args, int i, Dictionary<string, string> pairs, string atPath, int argLimit)
		{
			string s = args[i];
			args[i] = s = Environment.ExpandEnvironmentVariables(s);

			if (pairs != null) {
				Match m = CmdLineArgRegex.Match(s);
				if (m.Success) {
					// it's an --option
					string name = m.Groups[1].ToString();
					string value = m.Groups[3].ToString();
					try {
						pairs.Add(name.ToLower(), value);
					} catch {
						Output.Write(GSymbol.Get("Warning"), "Option {0} was specified more than once. The first value, {1}, will be used.",
							name, pairs[name]);
					}
				}
			}
			if (atPath != null && s.StartsWith("@")) {
				// e.g. "@list of options.txt"
				try {
					string fullpath = Path.Combine(atPath, s.Substring(1));
					if (File.Exists(fullpath))
					{
						string fileContents = File.OpenText(fullpath).ReadToEnd();
						List<string> list = G.SplitCommandLineArguments(fileContents);
						
						args.RemoveAt(i);
						
						int maxMore = Math.Max(0, argLimit - args.Count);
						if (list.Count > maxMore) {
							// oops, command limit exceeded
							Output.Write(GSymbol.Get("Warning"), "{0}: Limit of {1} commands exceeded", s, argLimit);
							list.RemoveRange(maxMore, list.Count - maxMore);
						}
						
						args.InsertRange(i, list);

						return true;
					}
				} catch (Exception e) {
					Output.Write(GSymbol.Get("Error"), s + ": " + e.Message);
				}
			}
			return false;
		}

		public static Pair<T1, T2> Pair<T1, T2>(T1 a, T2 b) { return new Pair<T1, T2>(a, b); }
		public static Triplet<T1, T2, T3> Triplet<T1, T2, T3>(T1 a, T2 b, T3 c) { return new Triplet<T1, T2, T3>(a, b, c); }

		[ThreadStatic] public static SimpleCache<object> _objectCache;
		[ThreadStatic] public static SimpleCache<string> _stringCache;
		
		public static string Cache(string s)
		{
			if (_stringCache == null)
				_stringCache = new SimpleCache<string>();
			return _stringCache.Cache(s);
		}
		public static object Cache(object o)
		{
			string s = o as string;
			if (s != null)
				return Cache(s);
			
			if (_objectCache == null)
				_objectCache = new SimpleCache<object>();
			return _objectCache.Cache(o);
		}

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
				} else if (c == '\n') {
					s2.Append(@"\n");
				} else if (c == '\r') {
					s2.Append(@"\r");
				} else if (c == '\0') {
					s2.Append(@"\0");
				} else if (c == '\\') {
					s2.Append(@"\\");
				} else if (c > 127 && (flags & EscapeC.NonAscii) != 0 || c < 32 && (flags & EscapeC.Control) != 0) {
					s2.AppendFormat(null, @"\x{0:X2}", (int)c);
				} else
					s2.Append(c);
			}
			return s2.ToString();
		}
		public static string UnescapeCStyle(string s)
		{
			return UnescapeCStyle(s, 0, s.Length, true);
		}
		public static string UnescapeCStyle(string s, int index, int length, bool removeUnnecessaryBackslashes)
		{
			StringBuilder s2 = new StringBuilder(length);
			for (int i = index; i < index + length; i++) {
				if (s[i] == '\\') {
					if (++i < index + length) {
						int len, code;
						switch (s[i]) {
						case 'u':
							len = Math.Min(4, s.Length - (i + 1));
							i += G.TryParseHex(s.Substring(i + 1, len), out code);
							s2.Append((char)code);
							break;
						case 'x':
							len = Math.Min(2, s.Length - (i + 1));
							i += G.TryParseHex(s.Substring(i + 1, len), out code);
							s2.Append((char)code);
							break;
						case '\\':
							s2.Append('\\'); break;
						case '\"':
							s2.Append('\"'); break;
						case '\'':
							s2.Append('\''); break;
						case 'n':
							s2.Append('\n'); break;
						case 'r':
							s2.Append('\r'); break;
						case 'a':
							s2.Append('\a'); break;
						case 'b':
							s2.Append('\b'); break;
						case 'f':
							s2.Append('\f'); break;
						case 't':
							s2.Append('\t'); break;
						case ' ':
							s2.Append(' '); break;
						default:
							if (!removeUnnecessaryBackslashes)
								s2.Append('\\');
							s2.Append(s[i]); break;
						}
						continue;
					}
				}
				else if (!removeUnnecessaryBackslashes)
					s2.Append('\\');
			}
			return s2.ToString();
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

		static G()
		{
			_ones = new byte[256];
			for (int i = 0; i < _ones.Length; i++)
				_ones[i] = (byte)MathEx.CountOnes(i);
		}
		static byte[] _ones;
		
		// This is benchmarked to be faster than MathEx.CountOnes()
		public static byte CountOnesAlt(byte x) { return _ones[x]; }
		public static int CountOnesAlt(ushort x) { return _ones[(byte)x] + _ones[x >> 8]; }
		public static int CountOnesAlt(uint x)
		{
			return (_ones[(byte)x] + _ones[(byte)(x >> 8)]) 
		         + (_ones[(byte)(x >> 16)] + _ones[x >> 24]);
		}

	}

	[Flags()]
	public enum EscapeC
	{
		Minimal = 0,  // Only \r, \n, \0 and backslash are escaped.
		Unicode = 2,  // Escape all characters with codes above 255 as \uNNNN
		NonAscii = 1, // Escape all characters with codes above 127 as \xNN
		Control = 4,  // Escape all characters with codes below 32  as \xNN
		DoubleQuotes = 8, // Escape double quotes as \"
		SingleQuotes = 16, // Escape single quotes as \'
		Quotes = 24,
	}

	[TestFixture]
	public class GTests
	{
		[Test] public void TestSwap()
		{
			int a = 7, b = 13;
			G.Swap(ref a, ref b);
			Assert.AreEqual(7, b);
			Assert.AreEqual(13, a);
		}
		[Test] public void TestInRange()
		{
			Assert.IsFalse(MathEx.IsInRange(1,2,5));
			Assert.IsTrue(MathEx.IsInRange(2,2,5));
			Assert.IsTrue(MathEx.IsInRange(3,2,5));
			Assert.IsTrue(MathEx.IsInRange(4,2,5));
			Assert.IsTrue(MathEx.IsInRange(5,2,5));
			Assert.IsFalse(MathEx.IsInRange(6,2,5));
			Assert.IsFalse(MathEx.IsInRange(2,5,2));
			Assert.IsFalse(MathEx.IsInRange(3,5,2));
			Assert.IsFalse(MathEx.IsInRange(5,5,2));
		}
		[Test] public void InRange()
		{
			Assert.AreEqual(2, MathEx.InRange(-1, 2, 5));
			Assert.AreEqual(2, MathEx.InRange(1, 2, 5));
			Assert.AreEqual(2, MathEx.InRange(2, 2, 5));
			Assert.AreEqual(3, MathEx.InRange(3, 2, 5));
			Assert.AreEqual(4, MathEx.InRange(4, 2, 5));
			Assert.AreEqual(5, MathEx.InRange(5, 2, 5));
			Assert.AreEqual(5, MathEx.InRange(6, 2, 5));
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
		[Test] public void TestProcessCommandLineArguments()
		{
			// TODO: trap warning message generated by ExpandCommandLineArguments

			// Generate two options files, where the first refers to the second
			string atPath = Environment.ExpandEnvironmentVariables("%TEMP%");
			string file1 = "test_g_expand_1.txt";
			string file2 = "test_g_expand_2.txt";
			StreamWriter w = new StreamWriter(Path.Combine(atPath, file1));
			w.WriteLine("@"+file2+" fox--jumps\n--over the hill");
			w.Close();
			w = new StreamWriter(Path.Combine(atPath, file2));
			w.WriteLine("\"%TEMP%\"");
			w.Close();

			// Expand command line and ensure that the arg limit of 4 is enforced
			List<string> args = G.SplitCommandLineArguments("\"@"+file1+"\" \"lazy dog\"");
			Dictionary<string, string> pairs = new Dictionary<string, string>();
			G.ProcessCommandLineArguments(args, pairs, atPath, 4);

			Assert.AreEqual(4, args.Count);
			Assert.AreEqual(args[0], atPath);
			Assert.AreEqual(args[1], "fox--jumps");
			Assert.AreEqual(args[2], "--over");
			Assert.AreEqual(args[3], "lazy dog");
			Assert.AreEqual(1, pairs.Count);
			Assert.AreEqual(pairs["over"], "");
		}
	}
}
