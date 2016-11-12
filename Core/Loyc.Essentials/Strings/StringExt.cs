using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Diagnostics;

namespace Loyc
{
	/// <summary>Extension methods for strings, such as <see cref="SplitAt"/>, 
	/// <see cref="Left"/>, <see cref="Right"/>, <see cref="Format"/> and <see cref="Slice"/>.</summary>
	public static partial class StringExt
	{
		/// <summary>Gets the substrings to the left and right of a dividing character.</summary>
		/// <param name="s">String to split</param>
		/// <param name="delimiter">Dividing character.</param>
		/// <returns>Returns the string to the left and to the right of the
		/// first occurance of 'c' in the string, not including 'c' itself.
		/// If 'c' was not found in 's', the pair (s, null) is returned.</returns>
		public static Pair<UString, UString> SplitAt(this string s, char delimiter) { return ((UString)s).SplitAt(delimiter); }
		public static Pair<UString, UString> SplitAt(this string s, string delimiter) { return ((UString)s).SplitAt(delimiter); }
		
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

		/// <summary>A variation on String.Substring() that never throws.</summary>
		/// <remarks>This is best explained by examples:
		/// <example>
		/// "Hi everybody!".SafeSubstring(8, 500) == "body!"
		/// "Hi everybody!".SafeSubstring(-3, 5) == "Hi"
		/// "Hi everybody!".SafeSubstring(-5, 5) == ""
		/// "Hi everybody!".SafeSubstring(8, -5) == ""
		/// "Hi everybody!".SafeSubstring(500, 8) == ""
		/// "Hi everybody!".SafeSubstring(int.MinValue + 500, int.MaxValue) == "Hi everybody!"
		/// ((string)null).SafeSubstring(0, 1) == null
		/// </example>
		/// </remarks>
		public static string SafeSubstring(this string s, int startIndex, int length = int.MaxValue)
		{
			if (s == null)
				return null;
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

		public static UString Slice(this string str, int start, int count = int.MaxValue)
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
		/// string msg = "{man's name} meets {woman's name}.".Localized(
		///		"man's name", mansName, "woman's name", womansName);
		/// </code>
		/// The placeholder names are not case sensitive.
		/// 
		/// You can use numeric placeholders, alignment and formatting codes also:
		/// <code>
		/// string msg = "You need to run {dist,6:###.00} km to reach {0}".Localized(
		///		cityName, "dist", 2.9);
		/// </code>
		/// It is assumed that the placeholder name ends at the first comma or colon; 
		/// hence the placeholder in this example is called "dist", not "dist,6:###.00".
		/// <para/>
		/// Typically, the named arguments are expected to start at index N+1 in the 
		/// variable argument array, where {N} is the largest numeric placeholder, and 
		/// if there are no numeric placeholders then the named arguments should begin 
		/// at index 0. In this example there is a {0}, so the named arguments should
		/// start at index 1. However, since named arguments always come in pairs, an 
		/// extra rule increments the N if the number of remaining arguments starting
		/// at N is not an even number. For example, in
		/// <code>
		/// string msg = "Hello {0}, you'll go to {school name} next year.".Localized(
		///		firstName, lastName, "school name", schoolName);
		/// </code>
		/// There are three args left after the numeric ones, so the first remaining
		/// argument is ignored to make it an even number.
		/// <para/>
		/// If a placeholder name is not found in the argument list then it is not
		/// replaced with a number before the call to string.Format, so a 
		/// FormatException will occur.
		/// </remarks>
		public static string FormatCore(this string format, params object[] args)
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

			if (((args.Length - highestIndex) & 1) == 0)
				highestIndex++; // so that the number of args left is even

			StringBuilder sb = new StringBuilder(format);
			int correction = 0;
			for (int i = 0; i < sb.Length - 1; i++)
			{
				if (sb[i] == '{' && sb[i + 1] != '{')
				{
					int placeholderStart = ++i; // Placeholder name starts here.
					for (; (c = sb[i]) != '}' && c != ':' && c != ','; i++) { }
					int placeholderLen = i - placeholderStart;

					// StringBuilder lacks Substring()! Instead, get the name 
					// from the original string and keep track of a correction 
					// factor so that in subsequent iterations, we get the 
					// substring from the right position in the original string.
					UString name = format.Slice(placeholderStart + correction, placeholderLen);

					for (int arg = highestIndex + 1; arg < args.Length; arg += 2)
						if (args[arg] != null && name.Equals(args[arg] as string, ignoreCase: true))
						{
							// Matching argument found. Replace name with index:
							string idxStr = (arg + 1).ToString();
							sb.Remove(placeholderStart, placeholderLen);
							sb.Insert(placeholderStart, idxStr);
							int dif = placeholderLen - idxStr.Length;
							correction += dif;
							i -= dif;
							break;
						}
				}
				Debug.Assert(sb[i] == format[i + correction]);
			}
			return sb.ToString();
		}
	}
}
