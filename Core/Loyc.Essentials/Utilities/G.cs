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
		public static void Swap(ref dynamic a, ref dynamic b)
		{
			var tmp = a;
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
		/// <summary>Returns <c>action(obj)</c>. This is similar to the other overload 
		/// of this method, except that the action has a return value.</summary>
		public static T With<T>(this T obj, Func<T, T> action)
		{
			return action(obj);
		}

		/// <summary>Returns true. This method has no effect; it is used to do an action in a conditional expression.</summary>
		/// <param name="value">Ignored.</param>
		/// <returns>True.</returns>
		public static bool True<T>(T value) { return true; }
		
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

		/// <summary>Same as <c>Debug.Assert</c> except that the argument is 
		/// evaluated even in a Release build.</summary>
		public static bool Verify(bool condition)
		{
			Debug.Assert(condition);
			return condition;
		}

		static Dictionary<char, string> HtmlEntityTable;

		/// <summary>Gets a bare HTML entity name for an ASCII character, or null if
		/// there is no entity name for the given character, e.g. 
		/// <c>BareHtmlEntityNameForAscii('"') == "quot"</c>.
		/// </summary><remarks>
		/// The complete entity name is an ampersand (&amp;) plus <c>BareHtmlEntityNameForAscii(c) + ";"</c>.
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
	}
}
