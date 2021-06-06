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
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Linq.Expressions;

namespace Loyc
{
	public delegate string WriterDelegate(string format, params object[] args);

	/// <summary>Contains global functions that don't belong in any specific class.</summary>
	/// <remarks>Note: helper methods for parsing and printing tokens and hex 
	/// digits have been moved to <see cref="Loyc.Syntax.ParseHelpers"/>.</remarks>
	public static partial class G
	{
		public static void Swap<T>(ref T a, ref T b)
		{
			T tmp = a;
			a = b;
			b = tmp;
		}

		public static bool SortPair<T>(ref T lo, ref T hi, Comparison<T> comp)
		{
			if (comp(lo, hi) > 0) {
				Swap(ref lo, ref hi);
				return true;
			}
			return false;
		}
		public static bool SortPair<T>(ref T lo, ref T hi) where T:IComparable<T>
		{
			if (lo.CompareTo(hi) > 0) {
				Swap(ref lo, ref hi);
				return true;
			}
			return false;
		}

		public static bool IsOneOf<T>(this T value, T item1, T item2) where T : IEquatable<T>
		{
			if (value == null)
				return item1 == null || item2 == null;
			else
				return value.Equals(item1) || value.Equals(item2);
		}
		public static bool IsOneOf<T>(this T value, T item1, T item2, T item3) where T : IEquatable<T>
		{
			if (value == null)
				return item1 == null || item2 == null || item3 == null;
			else
				return value.Equals(item1) || value.Equals(item2) || value.Equals(item3);
		}
		public static bool IsOneOf<T>(this T value, T item1, T item2, T item3, T item4) where T : IEquatable<T>
		{
			if (value == null)
				return item1 == null || item2 == null || item3 == null || item4 == null;
			else
				return value.Equals(item1) || value.Equals(item2) || value.Equals(item3) || value.Equals(item4);
		}
		public static bool IsOneOf<T>(this T value, T item1, T item2, T item3, T item4, T item5) where T : IEquatable<T>
		{
			if (value == null)
				return item1 == null || item2 == null || item3 == null || item4 == null || item5 == null;
			else
				return value.Equals(item1) || value.Equals(item2) || value.Equals(item3) || value.Equals(item4) || value.Equals(item5);
		}
		public static bool IsOneOf<T>(this T value, params T[] set) where T : IEquatable<T>
		{
			if (value == null) {
				for (int i = 0; i < set.Length; i++)
					if (set[i] == null)
						return true;
			} else {
				for (int i = 0; i < set.Length; i++)
					if (value.Equals(set[i]))
						return true;
			}
			return false;
		}

		/// <summary>Calls <c>action(obj)</c>, then returns the same object.</summary>
		/// <returns>obj</returns>
		/// <remarks>
		/// This is the plain-C# equivalent of the <c>with(obj)</c> statement. Compared
		/// to the Enhanced C# statement, <c>With()</c> is disadvantageous since it 
		/// requires a memory allocation to create the closure in many cases, as well
		/// as a delegate invocation that probably will not be inlined.
		/// <para/>
		/// Caution: you cannot mutate mutable structs with this method. Call the 
		/// other overload of this method if you will be modifying a mutable struct.
		/// </remarks>
		/// <example>
		/// Foo(new Person() { Name = "John Doe" }.With(p => p.Commit(dbConnection)));
		/// </example>
		public static T With<T>(this T obj, Action<T> action)
		{
			action(obj);
			return obj;
		}
		
		public delegate void ActionRefT<T>(ref T arg);
		/// <summary>Calls <c>action(ref obj)</c>, then returns the same object.</summary>
		public static T With<T>(this T obj, ActionRefT<T> action)
		{
			action(ref obj);
			return obj;
		}

		/// <summary>Returns <c>action(obj)</c>. This is similar to the other overload 
		/// of this method, except that the action has a return value.</summary>
		public static T With<T>(this T obj, Func<T, T> action) => Do(obj, action);
		
		/// <summary>Returns <c>action(obj)</c>. This method lets you embed statements 
		/// in any expression.</summary>
		public static R Do<T, R>(this T obj, Func<T, R> action) => action(obj);

		/// <summary>Returns true. This method has no effect; it is used to do an action 
		/// in a conditional expression.</summary>
		/// <param name="value">Ignored.</param>
		/// <returns>True.</returns>
		public static bool True<T>(T value) => true;
		
		/// <summary>This method simply assigns a value to a variable and returns the 
		/// value. For example, <c>G.Var(out int x, 777)</c> creates a variable called 
		/// x with a value of 777, and returns 777.</summary>
		/// <returns>True.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T Var<T>(out T var, T value) => var = value;

		/// <summary>This method simply calls the delegate provided and returns true. It is used to do an action in a conditional expression.</summary>
		/// <returns>True</returns>
		public static bool True(Action action) { action(); return true; }

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
				return (Comparison<T>)Delegate.CreateDelegate(typeof(Comparison<T>), null, typeof(IComparable<T>).GetMethod("CompareTo")!);
			}
			static Func<T, T, int> GetF() {
				if (typeof(T).IsValueType)
					return (a, b) => a.CompareTo(b);
				return (Func<T, T, int>)Delegate.CreateDelegate(typeof(Func<T, T, int>), null, typeof(IComparable<T>).GetMethod("CompareTo")!);
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
		/// <summary>Converts an <see cref="IComparer{T}"/> to a Func(T,T,int).</summary>
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

		static char[]? _invalids;

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

		/// <summary>Same as <c>Debug.Assert</c> except that the argument is 
		/// evaluated even in a Release build.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Verify(bool condition)
		{
			Debug.Assert(condition);
			return condition;
		}

		static Dictionary<char, string>? HtmlEntityTable;

		/// <summary>Gets a bare HTML entity name for an ASCII character, or null if
		/// there is no entity name for the given character, e.g. 
		/// <c>BareHtmlEntityNameForAscii('"') == "quot"</c>.
		/// </summary><remarks>
		/// The complete entity name is an ampersand (&amp;) plus <c>BareHtmlEntityNameForAscii(c) + ";"</c>.
		/// Some HTML entities have multiple names; this function returns one of them.
		/// There is a name in this table for all ASCII punctuation characters.
		/// </remarks>
		public static string? BareHtmlEntityNameForAscii(char c)
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
			HtmlEntityTable.TryGetValue(c, out string? name);
			return name;
		}

		#region Math methods don't really belong here
		// These methods are used by Loyc.Syntax which doesn't reference Loyc.Math and 
		// therefore can't use MathEx

		/// <summary>Returns the number of bits that are set in the specified integer.</summary>
		public static int CountOnes(byte x)
		{
			int X = x;
			X -= ((X >> 1) & 0x55);
			X = (((X >> 2) & 0x33) + (X & 0x33));
			return (X & 0x0F) + (X >> 4);
		}

		/// <summary>Returns the number of bits that are set in the specified integer.</summary>
		/// <remarks>This is a duplicate of MathEx.CountOnes() needed by Loyc.Collections,
		/// which does not have a reference to Loyc.Math.dll which contains MathEx.
		/// However this uses a compact SWAR implementation, whereas Loyc.Math uses
		/// a potentially faster lookup table.</remarks>
		public static int CountOnes(int x) { return CountOnes((uint)x); }
		/// <inheritdoc cref="CountOnes(int)"/>
		public static int CountOnes(uint x)
		{
			// 32-bit recursive reduction using SWAR... but first step 
			// is mapping 2-bit values into sum of 2 1-bit values in 
			// sneaky way
			x -= ((x >> 1) & 0x55555555);
			x = (((x >> 2) & 0x33333333) + (x & 0x33333333));
			x = (((x >> 4) + x) & 0x0f0f0f0f);
			x += (x >> 8);
			x += (x >> 16);
			return (int)(x & 0x0000003f);
		}


		public static double Int64BitsToDouble(long bits)
		{
			 return BitConverter.Int64BitsToDouble(bits);
		}
		public static long DoubleToInt64Bits(double value)
		{
			 return BitConverter.DoubleToInt64Bits(value);
		}

		/// <inheritdoc cref="Log2Floor(int)"/>
		public static int Log2Floor(uint x)
		{
			x |= (x >> 1);
			x |= (x >> 2);
			x |= (x >> 4);
			x |= (x >> 8);
			x |= (x >> 16);
			return (CountOnes(x) - 1);
		}
		/// <summary>
		/// Returns the floor of the base-2 logarithm of x. e.g. 1024 -> 10, 1000 -> 9
		/// </summary><remarks>
		/// The return value is -1 for an input that is zero or negative.
		/// <para/>
		/// Some processors have a dedicated instruction for this operation, but
		/// the .NET framework provides no access to it.
		/// </remarks>
		public static int Log2Floor(int x)
		{
			if (x < 0)
				return -1;
			return Log2Floor((uint)x);
		}

		#region ShiftLeft and ShiftRight for floating point

		public static double ShiftLeft(double num, int amount)
		{
			ulong bits = (ulong)DoubleToInt64Bits(num);
			uint exp = (uint)(bits >> 52) & 0x7FF;
			if (exp == 0x7FF)
				return num; // Number is infinite or NaN; do not change
			if (exp == 0)
			{
				// The number is denormalized. I'm tempted to just hand this off to
				// normal FP math: num * (1 << amount), but what if amount > 31?
				if (amount <= 0)
					if (amount == 0)
						return num;
					else
						return ShiftRight(num, -amount);

				ulong sign = bits & 0x8000000000000000;
				while ((bits <<= 1) <= 0x000FFFFFFFFFFFFF)
					if (--amount == 0)
						return Int64BitsToDouble((long)(bits | sign));
				bits |= sign;
				exp = 1;
			}

			// Normal case: num is normalized
			if ((exp += (uint)amount) < 0x7FFu)
				return Int64BitsToDouble((long)(bits & 0x800FFFFFFFFFFFFFu) | ((long)exp << 52));
			
			// negative shift is not supported for integers, but it works okay for floats
			if (amount < 0)
				return ShiftRight(num, -amount);

			return (long)bits >= 0 ? double.PositiveInfinity : double.NegativeInfinity;
		}

		public static double ShiftRight(double num, int amount)
		{
			ulong bits = (ulong)DoubleToInt64Bits(num);
			uint exp = (uint)(bits >> 52) & 0x7FF;
			if (exp == 0x7FF)
				return num;
			uint newExp = exp - (uint)amount;
			if (newExp - 1 < 0x7FF)
				return Int64BitsToDouble((long)(bits & 0x800FFFFFFFFFFFFFu) | ((long)newExp << 52));

			if (amount < 0)
				return ShiftLeft(num, -amount);

			// The result is denormalized.
			ulong sign = bits & 0x8000000000000000;
			bits &= 0x001FFFFFFFFFFFFF;
			// But was num denormalized already?
			if (exp > 1)
			{
				// not really, so let's get it ready for a denormalized right shift.
				amount -= ((int)exp - 1);
				Debug.Assert(amount >= 0);
				bits |= 0x0010000000000000;
			}
			if (amount > 53)
				return 0;

			return Int64BitsToDouble((long)(sign | (bits >> amount)));
		}

		#endregion

		#endregion

		public static int DecodeBase64Digit(char digit, string digit62 = "+-.~", string digit63 = "/_,")
		{
			if (digit >= 'A' && digit <= 'Z') return digit - 'A';
			if (digit >= 'a' && digit <= 'z') return digit + (26 - 'a');
			if (digit >= '0' && digit <= '9') return digit + (52 - '0');
			if (digit62.IndexOf(digit) > -1)  return 62;
			if (digit63.IndexOf(digit) > -1)  return 63;
			return -1;
		}

		public static char EncodeBase64Digit(int digit, char digit62 = '+', char digit63 = '/')
		{
			digit &= 63;
			if (digit < 52)
				return (char)(digit < 26 ? digit + 'A' : digit + ('a' - 26));
			else if (digit < 62)
				return (char)(digit + ('0' - 52));
			else
				return digit == 62 ? digit62 : digit63;
		}

		static Func<int, WordWrapCharType> _getWordWrapCharType = GetWordWrapCharType;

		/// <summary>This function controls the default character categorization used by 
		/// overloads of <see cref="WordWrap(IEnumerable{Pair{int, int}}, int, Func{int, WordWrapCharType})"/>.</summary>
		public static WordWrapCharType GetWordWrapCharType(int c)
		{
			switch (c)
			{
				case ' ': case '\u200B': case '\t': case '\r': // 200B=zero-width space
					return WordWrapCharType.Space;
				// There are a lot of other dash/hyphen characters but these are some common ones
				case '-': case '\u00AD': case '\u2010':
				case '\u2012': case '\u2013': case '\u2014':
				case ',': case ';': case '/': // 00AD=soft hyphen
					return WordWrapCharType.BreakAfter;
				case '\0': // included for unit-testing purposes
					return WordWrapCharType.BreakBefore;
				case '\n':
					return WordWrapCharType.Newline;
				default:
					return WordWrapCharType.NoWrap;
			}
		}

		/// <summary>Breaks a paragraph into lines using a simple word-wrapping algorithm.</summary>
		/// <param name="paragraph">Text to be broken apart if necessary.</param>
		/// <param name="lineWidth">Line width in characters.</param>
		/// <returns>A list of lines, each not too long.</returns>
		/// <remarks>
		/// The other overload of this function is more flexible, e.g. it supports variable-width
		/// characters.
		/// <para/>
		/// This algorithm may be unsuitable for some non-English languages, in which adjacent 
		/// characters do not have independent widths because of the way they combine. It may
		/// still be useful if you only need to make sure lines don't get too long.
		/// <para/>
		/// Whitespace characters (including tabs and zero-width spaces) at the end of a 
		/// line are not counted toward the line length limit. As a result, strings in the output 
		/// list can be longer than <c>lineWidth</c> unless you trim spaces afterward.
		/// <para/>
		/// If the input contains a newline character, a line break occurs but the newline is 
		/// preserved, e.g. "Ann\nBob" causes output like { "Ann\n", "Bob" }. Depending on
		/// how you intend to use the output, you may need to trim the newline from the end.
		/// <para/>
		/// By default, lines can be broken at the soft hyphen '\u00AD', in which case it is 
		/// advisable for the caller to replace the trailing '\u00AD' with '-' before drawing to 
		/// ensure that the hyphen is actually displayed on the screen. For simplicity, this 
		/// replacement is not part of the wrapping algorithm itself.
		/// </remarks>
		public static List<string> WordWrap(string paragraph, int lineWidth)
		{
			return WordWrap(paragraph.Select(c => Pair.Create((int) c, 1)), lineWidth, _getWordWrapCharType);
		}

		/// <summary>Breaks a paragraph into lines using a simple word-wrapping algorithm.</summary>
		/// <param name="paragraph">A sequence of characters that will be treated as a paragraph 
		/// to be broken into lines as necessary. The first item in each pair is a 21-bit unicode
		/// character; the second item is the width of that character, e.g. in pixels. The
		/// width must be non-negative and no more than half of int.MaxValue.</param>
		/// <param name="lineWidth">Line width. The unit used here is the same as the unit of
		/// the second item in each pair, e.g. pixels.</param>
		/// <param name="getCharType">A function that determines how a character is relevant to
		/// the wrapping operation; see <see cref="WordWrapCharType"/>. This function accepts
		/// 21-bit unicode characters from <c>paragraph</c>.</param>
		/// <returns>A list of lines, each not too long.</returns>
		/// <remarks>
		/// This algorithm may be unsuitable for some non-English languages, in which adjacent 
		/// characters do not have independent widths because of the way they combine. It may
		/// still be useful if you only need to make sure lines don't get too long.
		/// <para/>
		/// Characters higher than 0xFFFF are converted into UTF-16 surrogate pairs.
		/// <para/>
		/// Whitespace characters at the end of a line are not counted toward the line length 
		/// limit. As a result, strings in the output list can be longer than <c>lineWidth</c> 
		/// unless you trim off spaces afterward.
		/// <para/>
		/// If the input contains a newline character, a line break occurs but the newline is 
		/// preserved, e.g. "Foo\nBar" causes output like { "Foo\n", "Bar" }. Depending on
		/// how you intend to use the output, you may need to trim the newline from the end.
		/// <para/>
		/// By default, lines can be broken at the soft hyphen '\u00AD', in which case it is 
		/// advisable for the caller to replace the trailing '\u00AD' with '-' before drawing to 
		/// ensure that the hyphen is actually displayed on the screen. For simplicity, this 
		/// replacement is not part of the wrapping algorithm itself. 
		/// </remarks>
		public static List<string> WordWrap(IEnumerable<Pair<int, int>> paragraph, int lineWidth, Func<int, WordWrapCharType>? getCharType = null)
		{
			getCharType = getCharType ?? _getWordWrapCharType;
			if (lineWidth == int.MaxValue)
				lineWidth--; // algorithm expects lineWidth+1 > lineWidth

			var sb = new StringBuilder();
			var output = new List<string>();
			int width = 0; // width of current line
			int lastBreakPoint = 0, widthAtBreakPoint = 0;
			foreach (var pair in paragraph)
			{
				int c = pair.A;
				int cWidth = pair.B;
				var charType = getCharType(c);
				if (width <= lineWidth && (charType & WordWrapCharType.BreakBefore) != 0) {
					lastBreakPoint = sb.Length;
					widthAtBreakPoint = width;
				}
				if (c <= 0xFFFF)
					sb.Append((char)c);
				else {
					c -= 0x10000;
					sb.Append(((c >> 10) & 0x3FF) + 0xD800);
					sb.Append((c & 0x3FF) + 0xDC00);
				}
				width += cWidth;
				if ((charType & WordWrapCharType.Space) != 0) {
					lastBreakPoint = sb.Length;
					widthAtBreakPoint = width;
				} else {
					if ((charType & WordWrapCharType.Newline) != 0) {
						lastBreakPoint = sb.Length;
						widthAtBreakPoint = width = lineWidth + 1; // force break now
					}
					if (width > lineWidth) {
						if (lastBreakPoint <= 0) {
							// No suitable breakpoint exists (e.g. string lacks spaces)
							lastBreakPoint = sb.Length - 1;
							widthAtBreakPoint = width - cWidth;
							if (lastBreakPoint == 0) {
								lastBreakPoint = 1;
								Debug.Assert(widthAtBreakPoint == 0);
								widthAtBreakPoint = width;
							}
						}
						output.Add(sb.ToString(0, lastBreakPoint));
						sb.Remove(0, lastBreakPoint);
						width -= widthAtBreakPoint;
						lastBreakPoint = widthAtBreakPoint = 0;
					} else if ((charType & WordWrapCharType.BreakAfter) != 0) {
						lastBreakPoint = sb.Length;
						widthAtBreakPoint = width;
					}
				}
			}
			if (sb.Length != 0)
				output.Add(sb.ToString());
			return output;
		}

		/// <summary>Given an expression that refers to a method or property, such as
		/// <c>(Class c) => c.Method(0, 0)</c>, this function returns the 
		/// System.Reflection.MethodInfo object associated with the method/property.
		/// You can use an expression like <c>(Class c) => c.Prop</c> to get a property's
		/// getter, but you can't get the setter because C# 9 doesn't support them in 
		/// expression trees.</summary>
		/// <exception cref="InvalidCastException">The expression was not in the expected format.</exception>
		public static MemberInfo GetMethodInfo<T, TResult>(Expression<Func<T, TResult>> code)
		{
			if (code.Body is MemberExpression me)
				return ((PropertyInfo)me.Member).GetGetMethod()!;
			//else if (code.Body is BinaryExpression be && be.NodeType == ExpressionType.Assign)
			//	return ((PropertyInfo)((MemberExpression)be.Left).Member).GetSetMethod()!;
			else
				return ((MethodCallExpression)code.Body).Method;
		}

		/// <summary>Given an expression that refers to a static method or property, 
		/// such as <c>() => Class.Method(0, 0)</c>, this function returns the 
		/// System.Reflection.MethodInfo object associated with the method/property.
		/// You can use an expression like <c>() => Class.Prop</c> to get a property's
		/// getter, but you can't get the setter because C# 9 doesn't support them in 
		/// expression trees.</summary>
		public static MethodInfo GetMethodInfo<TResult>(Expression<Func<TResult>> code)
		{
			if (code.Body is MemberExpression me)
				return ((PropertyInfo)me.Member).GetGetMethod()!;
			//else if (code.Body is BinaryExpression be && be.NodeType == ExpressionType.Assign)
			//	return ((PropertyInfo)((MemberExpression)be.Left).Member).GetSetMethod()!;
			else
				return ((MethodCallExpression)code.Body).Method;
		}
	}

	/// <summary>The set of character categories recognized by overloads of 
	/// <see cref="G.WordWrap(IEnumerable{Pair{int, int}}, int, Func{int, WordWrapCharType})"/>.</summary>
	[Flags]
	public enum WordWrapCharType
	{
		/// <summary>Represents a character on which not to wrap, such as a letter.</summary>
		NoWrap = 0,
		/// <summary>Represents a space character. Spaces are special because they do not 
		/// consume physical space at the end of a line, so a line break never occurs before 
		/// a space.</summary>
		Space = 1,
		/// <summary>Represents a character before which a line break can be added.</summary>
		BreakBefore = 2,
		/// <summary>Represents a character after which a line break can be added.
		/// The most common example of this category is a hyphen.</summary>
		BreakAfter = 4,
		/// <summary>Represents a forced-break (newline) character.</summary>
		Newline = 8,
	}
}
