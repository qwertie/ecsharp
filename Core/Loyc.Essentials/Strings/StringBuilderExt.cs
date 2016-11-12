using Loyc.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc
{
	/// <summary>Extension methods that add some functionality of <c>string</c> to <c>StringBuilder</c>.</summary>
	public static class StringBuilderExt
	{
		/// <summary>Extracts a substring from the specified StringBuiler.</summary>
		public static string Substring(this StringBuilder sb, int startIndex, int length)
		{
			return sb.ToString(startIndex, length);
		}

		/// <summary>Returns the last character of the string</summary>
		public static char Last(this StringBuilder str)
		{
			if (str.Length == 0)
				throw new EmptySequenceException();
			return str[str.Length - 1];
		}

		/// <summary>Returns the last character of the string, or a default character if the string is empty.</summary>
		public static char LastOrDefault(this StringBuilder str, char @default = '\0')
		{
			if (str.Length == 0)
				return @default;
			return str[str.Length - 1];
		}

		static char[] _defaultTrimChars = new[] { ' ', '\t' };

		/// <summary>Removes all leading occurrences of spaces and tabs from the StringBuilder object.</summary>
		public static StringBuilder TrimStart(this StringBuilder sb) { return TrimStart(sb, _defaultTrimChars); }
		/// <summary>Removes all leading occurrences of the specified set of characters from the StringBuilder object.</summary>
		/// <param name="trimChars">An array of Unicode characters to remove.</param>
		public static StringBuilder TrimStart(this StringBuilder sb, params char[] trimChars)
		{
			int i = 0;
			while (i < sb.Length && trimChars.Contains(sb[i]))
				i++;
			sb.Remove(0, i);
			return sb;
		}

		/// <summary>Removes all trailing occurrences of spaces and tabs from the StringBuilder object.</summary>
		public static StringBuilder TrimEnd(this StringBuilder sb) { return TrimEnd(sb, _defaultTrimChars); }
		/// <summary>Removes all trailing occurrences of the specified set of characters from the StringBuilder object.</summary>
		/// <param name="trimChars">An array of Unicode characters to remove.</param>
		public static StringBuilder TrimEnd(this StringBuilder sb, params char[] trimChars)
		{
			int i = sb.Length;
			while (i > 0 && trimChars.Contains(sb[i - 1]))
				i--;
			sb.Remove(i, sb.Length - i);
			return sb;
		}

		/// <summary>Removes all leading and trailing occurrences of spaces and tabs from the StringBuilder object.</summary>
		public static StringBuilder Trim(this StringBuilder sb) { return Trim(sb, _defaultTrimChars); }
		/// <summary>Removes all leading and trailing occurrences of the specified set of characters from the StringBuilder object.</summary>
		/// <param name="trimChars">An array of Unicode characters to remove.</param>
		public static StringBuilder Trim(this StringBuilder sb, params char[] trimChars)
		{
			return TrimStart(TrimEnd(sb, trimChars), trimChars);
		}

		/// <summary>Gets the index of a character in a StringBuilder</summary>
		/// <returns>Index of the first instance of the specified character in the string, or -1 if not found</returns>
		public static int IndexOf(this StringBuilder sb, char value, int startIndex = 0)
		{
			for (int i = startIndex; i < sb.Length; i++)
				if (sb[i] == value)
					return i;
			return -1;
		}

		/// <summary>Gets the index of a substring in a StringBuilder</summary>
		/// <returns>Index of the first instance of the specified substring in the StringBuilder, or -1 if not found</returns>
		public static int IndexOf(this StringBuilder sb, UString searchStr, int startIndex = 0, bool ignoreCase = false)
		{
			var stopAt = sb.Length - searchStr.Length;
			for (int i = startIndex; i <= stopAt; i++)
				if (SubstringEqualHelper(sb, i, searchStr, ignoreCase))
					return i;
			return -1;
		}

		/// <summary>Gets the index of a character in a StringBuilder</summary>
		/// <returns>Index of the last instance of the specified character in the StringBuilder, or -1 if not found</returns>
		public static int LastIndexOf(this StringBuilder sb, char searchChar, int startIndex = int.MaxValue)
		{
			if (startIndex > sb.Length - 1)
				startIndex = sb.Length - 1;
			for (int i = startIndex; i >= 0; i--)
				if (sb[i] == searchChar)
					return i;
			return -1;
		}

		/// <summary>Gets the index of a substring in a StringBuilder</summary>
		/// <returns>Index of the last instance of the specified substring in the StringBuilder, or -1 if not found</returns>
		public static int LastIndexOf(this StringBuilder sb, UString searchStr, int startIndex = int.MaxValue, bool ignoreCase = false)
		{
			if (startIndex > sb.Length - searchStr.Length)
				startIndex = sb.Length - searchStr.Length;
			for (int i = startIndex; i >= 0; i--)
				if (SubstringEqualHelper(sb, i, searchStr, ignoreCase))
					return i;
			return -1;
		}

		/// <summary>Finds out whether the StringBuilder ends with the specified substring.</summary>
		public static bool EndsWith(this StringBuilder sb, UString what, bool ignoreCase = false)
		{
			if (what.Length > sb.Length)
				return false;
			return SubstringEqualHelper(sb, sb.Length - what.Length, what, ignoreCase);
		}

		/// <summary>Finds out whether the StringBuilder starts with the specified substring.</summary>
		public static bool StartsWith(this StringBuilder sb, UString what, bool ignoreCase = false)
		{
			if (what.Length > sb.Length)
				return false;
			return SubstringEqualHelper(sb, 0, what, ignoreCase);
		}

		/// <summary>Checks if the sequences of characters <c>what</c> is equal to
		/// <c>sb.Substring(start, what.Length)</c>, without actually creating a 
		/// substring object.</summary>
		public static bool SubstringEquals(StringBuilder sb, int start, UString what, bool ignoreCase = false)
		{
			CheckParam.IsNotNegative("start", start);
			if (start > sb.Length - what.Length)
				return false;
			return SubstringEqualHelper(sb, start, what, ignoreCase);
		}

		static bool SubstringEqualHelper(StringBuilder sb, int start, UString what, bool ignoreCase = false)
		{
			if (ignoreCase)
				for (int i = 0; i < what.Length; i++) {
					if (char.ToUpperInvariant(sb[start + i]) != char.ToUpperInvariant(what[i]))
						return false;
				}
			else
				for (int i = 0; i < what.Length; i++) {
					if (sb[start + i] != what[i])
						return false;
				}
			return true;
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

		/// <summary>Appends a unicode code point in the range 0 to 0x10FFFF to StringBuilder in UTF-16.</summary>
		/// <exception cref="ArgumentOutOfRangeException">Invalid character c</exception>
		/// <exception cref="NullReferenceException">null StringBuildre</exception>
		public static StringBuilder AppendCodePoint(this StringBuilder s, int c)
		{
			if ((uint)c <= 0xFFFF)
				s.Append((char)c);
			else if ((uint)c <= 0x10FFFF)
			{
				c -= 0x10000;
				s.Append((char)((c >> 10) | 0xD800));
				s.Append((char)((c & 0x3FF) | 0xDC00));
			}
			else
				throw new ArgumentOutOfRangeException("Invalid unicode character: {0}".Localized(c));
			return s;
		}
	}
}
