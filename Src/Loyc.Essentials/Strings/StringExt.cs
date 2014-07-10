using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Loyc
{
	/// <summary>Extension methods for strings, such as <see cref="SplitAt"/>, 
	/// <see cref="Left"/>, <see cref="Right"/>, <see cref="Format"/> and <see cref="USlice"/>.</summary>
	public static partial class StringExt
	{
		/// <summary>Gets the substrings to the left and right of a dividing character.</summary>
		/// <param name="s">String to split</param>
		/// <param name="c">Dividing character.</param>
		/// <returns>Returns the string to the left and to the right of the
		/// first occurance of 'c' in the string, not including 'c' itself.
		/// If 'c' was not found in 's', the pair (s, null) is returned.</returns>
		public static Pair<UString,UString> SplitAt(this string s, char c)
		{
			int i = s.IndexOf(c);
			if (i == -1)
				return new Pair<UString, UString>(s, UString.Null);
			else
				return new Pair<UString, UString>(s.USlice(0, i), s.USlice(i + 1));
		}
		
		/// <summary>Returns the rightmost 'count' characters of 's', or s itself if count > s.Length.</summary>
		public static string Right(this string s, int count)
		{
			if (count >= s.Length)
				return s;
			else
				return s.Substring(s.Length - count);
		}
		
		/// <summary>Returns the leftmost 'count' characters of 's', or s itself if count > s.Length.</summary>
		public static string Left(this string s, int count)
		{
			if (count >= s.Length)
				return s;
			else
				return s.Substring(0, count);
		}

		public static char? TryGet(this string s, int index)
		{
			if ((uint)index < (uint)s.Length)
				return s[index];
			return null;
		}
		public static char TryGet(this string s, int index, char defaultValue)
		{
			if ((uint)index < (uint)s.Length)
				return s[index];
			return defaultValue;
		}
		public static char? TryGet(this StringBuilder s, int index)
		{
			if ((uint)index < (uint)s.Length)
				return s[index];
			return null;
		}
		public static char TryGet(this StringBuilder s, int index, char defaultValue)
		{
			if ((uint)index < (uint)s.Length)
				return s[index];
			return defaultValue;
		}

		public static string SafeSubstring(this string s, int startIndex, int length)
		{
			if ((uint)startIndex > (uint)s.Length)
			{
				if (startIndex < 0) {
					if (length <= 0)
						return "";
					length += startIndex;
					startIndex = 0;
				} else
					return "";
			}
			if ((uint)(startIndex + length) > (uint)s.Length)
			{
				if (length > 0)
					length = s.Length - startIndex;
				else
					return "";
			}
			return s.Substring(startIndex, length);
		}
		
		/// <summary>Converts a series of values to strings, and concatenates them 
		/// with a given separator between them.</summary>
		/// <example>Join(" + ", new[] { 1,2,3 }) returns "1 + 2 + 3".</example>
		/// <remarks>This method (but taking IEnumerable{T}) exists in the BCL starting in .NET 4</remarks>
		public static string Join(string separator, IEnumerable value) { return Join(separator, value.GetEnumerator()); }
		/// <inheritdoc cref="Join(string, IEnumerable)"/>
		public static string Join(string separator, IEnumerator value) 
		{
			if (!value.MoveNext())
				return string.Empty;
			StringBuilder sb = new StringBuilder (value.Current.ToString());
			while (value.MoveNext()) {
				sb.Append(separator);
				sb.Append(value.Current.ToString());
			}
			return sb.ToString();
		}

		public static UString USlice(this string str, int start, int count = int.MaxValue)
		{
			return new UString(str, start, count);
		}
		public static UString Find(this string str, UString what, bool ignoreCase = false)
		{
			return ((UString)str).Find(what, ignoreCase);
		}


		/// <summary>
		/// This formatter works like string.Format, except that named 
		/// placeholders accepted as well as numeric placeholders. This method
		/// replaces named placeholders with numbers, then calls string.Format.
		/// </summary>
		/// <remarks>
		/// Named placeholders are useful for communicating information about a
		/// placeholder to a human translator. Here is an example:
		/// <code>
		/// Not enough memory to {load/parse} '{filename}'.
		/// </code>
		/// In some cases a translator might have difficulty translating a phrase
		/// without knowing what a numeric placeholder ({0} or {1}) refers to, so 
		/// a named placeholder can provide an important clue. The localization  
		/// system is invoked as follows:
		/// <code>
		/// string msg = Localize.From("{man's name} meets {woman's name}.",
		///		"man's name", mansName, "woman's name", womansName);
		/// </code>
		/// The placeholder names are not case sensitive.
		/// 
		/// You can use numeric placeholders, alignment and formatting codes also:
		/// <code>
		/// string msg = Localize.From("You need to run {km,6:###.00} km to reach {0}",
		///		cityName, "KM", 2.9);
		/// </code>
		/// This method will ignore the first N+1 arguments in args, where {N}
		/// is the largest numeric placeholder. It is assumed that the placeholder 
		/// name ends at the first comma or colon; hence the placeholder in this 
		/// example is called "km", not "km,6:###.00".
		/// 
		/// If a placeholder name is not found in the argument list then it is not
		/// replaced with a number before the call to string.Format, so a 
		/// FormatException will occur.
		/// </remarks>
		public static string Format(this string format, params object[] args)
		{
			format = EliminateNamedArgs(format, args);
			return string.Format(format, args);
		}

		/// <summary>Called by Format to replace named placeholders with numeric
		/// placeholders in format strings.</summary>
		/// <returns>A format string that can be used to call string.Format.</returns>
		/// <seealso cref="Format"/>
		public static string EliminateNamedArgs(string format, params object[] args)
		{
			char c;
			bool containsNames = false;
			int highestIndex = -1;

			for (int i = 0; i < format.Length - 1; i++)
				if (format[i] == '{' && format[i + 1] != '{')
				{
					int j = ++i;
					for (; (c = format[i]) >= '0' && c <= '9'; i++) { }
					if (i == j)
						containsNames = true;
					else
						highestIndex = int.Parse(format.Substring(j, i - j));
				}

			if (!containsNames)
				return format;

			StringBuilder sb = new StringBuilder(format);
			int correction = 0;
			for (int i = 0; i < sb.Length - 1; i++)
			{
				if (sb[i] == '{' && sb[i + 1] != '{')
				{
					int j = ++i; // Placeholder name starts here.
					for (; (c = format[i]) != '}' && c != ':' && c != ','; i++) { }

					// StringBuilder lacks Substring()! Instead, get the name 
					// from the original string and keep track of a correction 
					// factor so that in subsequent iterations, we get the 
					// substring from the right position in the original string.
					string name = format.Substring(j - correction, i - j);

					for (int arg = highestIndex + 1; arg < args.Length; arg += 2)
						if (args[arg] != null && string.Compare(name, args[arg].ToString(), true) == 0)
						{
							// Matching argument found. Replace name with index:
							string idxStr = (arg + 1).ToString();
							sb.Remove(j, i - j);
							sb.Insert(j, idxStr);
							int dif = i - j - idxStr.Length;
							correction += dif;
							i -= dif;
							break;
						}
				}
			}
			return sb.ToString();
		}
	}
}
