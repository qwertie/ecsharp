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
	public static class G
	{
		public static void Swap<T>(ref T a, ref T b)
		{
			T tmp = a;
			a = b;
			b = tmp;
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

		/// <summary>Same as <c>Debug.Assert</c> except that the argument is 
		/// evaluated even in a Release build.</summary>
		public static bool Verify(bool condition)
		{
			Debug.Assert(condition);
			return condition;
		}

		static Dictionary<char, string> HtmlEntityTable;

		/// <summary>Gets a bare HTML entity name for an ASCII character, or null if
		/// there is no entity name for the given character, e.g. <c>'"'=>"quot"</c>.
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
}
