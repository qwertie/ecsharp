using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Runtime
{
	public static class Strings
	{
		public static bool SplitAt(string s, char c, out string s1, out string s2)
		{
			int i = s.IndexOf(c);
			if (i == -1) {
				s1 = s; s2 = null;
				return false;
			} else {
				s1 = s.Substring(0, i);
				s2 = s.Substring(i + 1);
				return true;
			}
		}
		public static string Right(string s, int count)
		{
			if (count >= s.Length)
				return s;
			else
				return s.Substring(s.Length - count);
		}
		public static string Left(string s, int count)
		{
			if (count >= s.Length)
				return s;
			else
				return s.Substring(0, count);
		}
		public static string Join(string separator, IEnumerable value) { return Join(separator, value.GetEnumerator()); }
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
		/// DefaultFormatter will ignore the first N+1 arguments in args, where {N}
		/// is the largest numeric placeholder. It is assumed that the placeholder 
		/// name ends at the first comma or colon; hence the placeholder in this 
		/// example is called "km", not "km,6:###.00".
		/// 
		/// If a placeholder name is not found in the argument list then it is not
		/// replaced with a number before the call to string.Format, so a 
		/// FormatException will occur.
		/// </remarks>
		public static string Format(string format, params object[] args)
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
	
	public static class Arrays
	{
		public static T[] Clone<T>(T[] array) { return (T[]) array.Clone(); }
	}
}
