namespace Loyc
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Text;
	using System.Linq;
	using Loyc.Math;
	using Loyc.Collections.Impl;
	using System.Diagnostics;
	using Loyc.Collections;
	using Loyc.Essentials;

	public static partial class StringExt
	{
		/// <summary>Splits a string in two pieces.</summary>
		/// <param name="s">String to split</param>
		/// <param name="c">Dividing character.</param>
		/// <returns>Returns the string to the left and to the right of the
		/// first occurance of 'c' in the string, not including 'c' itself.
		/// If 'c' was not found in 's', the pair (s, null) is returned.</returns>
		public static Pair<string,string> SplitAt(this string s, char c)
		{
			int i = s.IndexOf(c);
			if (i == -1)
				return new Pair<string, string>(s, null);
			else
				return new Pair<string, string>(s.Substring(0, i), s.Substring(i + 1));
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
		
		/// <summary>Converts a series of values to strings, and concatenates them 
		/// with a given separator between them.</summary>
		/// <example>Join(" + ", new[] { 1,2,3 }) returns "1 + 2 + 3".</example>
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

	/// <summary>Extension methods and helper methods for <see cref="List{T}"/> and <see cref="IList{T}"/>.</summary>
	public static class ListExt
	{
		public static void RemoveRange<T>(this List<T> list, int index, int count)
		{
			if (index + count > list.Count)
				throw new IndexOutOfRangeException(index.ToString() + " + " + count.ToString() + " > " + list.Count.ToString());
			if (index < 0)
				throw new IndexOutOfRangeException(index.ToString() + " < 0");
			if (count > 0) {
				for (int i = index; i < list.Count - count; i++)
					list[i] = list[i + count];
				Resize(list, list.Count - count);
			}
		}
		public static void RemoveRange<T>(this IList<T> list, int index, int count)
		{
			if (index + count > list.Count)
				throw new IndexOutOfRangeException(index.ToString() + " + " + count.ToString() + " > " + list.Count.ToString());
			if (index < 0)
				throw new IndexOutOfRangeException(index.ToString() + " < 0");
			if (count > 0) {
				for (int i = index; i < list.Count - count; i++)
					list[i] = list[i + count];
				Resize(list, list.Count - count);
			}
		}
		public static void Resize<T>(this List<T> list, int newSize)
		{
			int dif = newSize - list.Count;
			if (dif > 0) {
				do list.Add(default(T));
				while (--dif > 0);
			} else if (dif < 0) {
				int i = list.Count;
				do list.RemoveAt(--i);
				while (--dif > 0);
			}
		}
		public static void Resize<T>(this IList<T> list, int newSize)
		{
			int dif = newSize - list.Count;
			if (dif > 0) {
				do list.Add(default(T));
				while (--dif > 0);
			} else if (dif < 0) {
				int i = list.Count;
				do list.RemoveAt(--i);
				while (--dif > 0);
			}
		}

		public static IEnumerable<Pair<A, B>> Zip<A, B>(this IEnumerable<A> a, IEnumerable<B> b)
		{
			var ea = a.GetEnumerator();
			var eb = b.GetEnumerator();
			while (ea.MoveNext() && eb.MoveNext())
				yield return Pair.Create(ea.Current, eb.Current);
		}
		public static IEnumerable<Pair<A, B>> ZipLonger<A, B>(this IEnumerable<A> a, IEnumerable<B> b)
		{
			return ZipLonger(a, b, default(A), default(B));
		}
		public static IEnumerable<Pair<A, B>> ZipLonger<A, B>(this IEnumerable<A> a, IEnumerable<B> b, A defaultA, B defaultB)
		{
			var ea = a.GetEnumerator();
			var eb = b.GetEnumerator();
			bool haveA, haveB;
			for (; ; ) {
				haveA = ea.MoveNext();
				haveB = eb.MoveNext();
				if (!haveA && !haveB)
					break;
				yield return Pair.Create(haveA ? ea.Current : defaultA, haveB ? eb.Current : defaultB);
			}
		}

		static int[] RangeArray(int count)
		{
			var n = new int[count];
			for (int i = 0; i < n.Length; i++) n[i] = i;
			return n;
		}

		/// <inheritdoc cref="Sort(IList{T}, int, int, Comparison{T})"/>
		public static void Sort<T>(this IList<T> list)
		{
			Sort(list, Comparer<T>.Default.Compare);
		}
		/// <inheritdoc cref="Sort(IList{T}, int, int, Comparison{T})"/>
		public static void Sort<T>(this IList<T> list, Comparison<T> comp)
		{
			Sort(list, 0, list.Count, comp, null);
		}
		/// <summary>Performs a quicksort using a Comparison function.</summary>
		/// <param name="index">Index at which to begin sorting a portion of the list.</param>
		/// <param name="count">Number of items to sort starting at 'index'.</param>
		/// <remarks>
		/// This method exists because the .NET framework offers no method to
		/// sort <see cref="IList{T}"/>--you can sort arrays and <see cref="List{T}"/>, 
		/// but not IList.
		/// </remarks>
		public static void Sort<T>(this IList<T> list, int index, int count, Comparison<T> comp)
		{
			Sort(list, index, count, comp, null);
		}

		/// <summary>Performs a stable sort, i.e. a sort that preserves the 
		/// relative order of items that compare equal.</summary>
		/// <remarks>
		/// This algorithm uses a quicksort and therefore runs in O(N log N) time,
		/// but it requires O(N) temporary space (specifically, an array of N 
		/// integers) and is slower than a standard quicksort, so you should use
		/// it only if you need a stable sort.
		/// </remarks>
		public static void StableSort<T>(this IList<T> list, Comparison<T> comp)
		{
			Sort(list, 0, list.Count, comp, RangeArray(list.Count));
		}
		public static void StableSort<T>(this IList<T> list)
		{
			StableSort(list, Comparer<T>.Default.Compare);
		}

		private static void Sort<T>(this IList<T> list, int index, int count, Comparison<T> comp, int[] indexes)
		{
			// This code duplicates the code in InternalList.Sort(), except
			// that it also supports stable sorting. This version is slower;
			// Two versions exist so that array sorting can be done faster.
			CheckParam.Range("index", index, 0, list.Count);
			CheckParam.Range("count", count, 0, list.Count - index);

			for (;;) {
				if (count < InternalList.QuickSortThreshold)
				{
					if (count <= 2) {
						if (count == 2)
							SortPair(list, index, index + 1, comp);
					} else {
						InsertionSort(list, index, count, comp);
					}
					return;
				}

				int iPivot = InternalList.PickPivot(list, index, count, comp);

				int iBegin = index;
				// Swap the pivot to the beginning of the range
				T pivot = list[iPivot];
				if (iBegin != iPivot) {
					Swap(list, iBegin, iPivot);
					if (indexes != null)
						MathEx.Swap(ref indexes[iPivot], ref indexes[iBegin]);
				}

				int i = iBegin + 1;
				int iOut = iBegin;
				int iStop = index + count;
				int leftSize = 0; // size of left partition

				// Quick sort pass
				do {
					int order = comp(list[i], pivot);
					if (order > 0)
						continue;
					if (order == 0) {
						if (indexes != null) {
							if (indexes[i] > indexes[iBegin])
								continue;
						} else if (leftSize < (count >> 1))
							continue;
					}
					
					++iOut;
					++leftSize;
					if (i != iOut) {
						Swap(list, i, iOut);
						if (indexes != null)
							MathEx.Swap(ref indexes[i], ref indexes[iOut]);
					}
				} while (++i != iStop);

				// Finally, put the pivot element in the middle (at iOut)
				Swap(list, iBegin, iOut);
				if (indexes != null)
					MathEx.Swap(ref indexes[iBegin], ref indexes[iOut]);

				// Now we need to sort the left and right sub-partitions. Use a 
				// recursive call only to sort the smaller partition, in order to 
				// guarantee O(log N) stack space usage.
				int rightSize = count - 1 - leftSize;
				if (leftSize < rightSize)
				{
					// Recursively sort the left partition; iteratively sort the right
					Sort(list, index, leftSize, comp, indexes);
					index += leftSize + 1;
					count = rightSize;
				}
				else
				{	// Iteratively sort the left partition; recursively sort the right
					count = leftSize;
					Sort(list, index + leftSize + 1, rightSize, comp, indexes);
				}
			}
		}

		/// <summary>Performs an insertion sort.</summary>
		/// <remarks>The insertion sort is a stable sort algorithm that is slow in 
		/// general (O(N^2)). It should be used only when (a) the list to be sorted
		/// is short (less than 10-20 elements) or (b) the list is very nearly
		/// sorted already.</remarks>
		/// <seealso cref="InternalList.InsertionSort"/>
		public static void InsertionSort<T>(this IList<T> array, int index, int count, Comparison<T> comp)
		{
			for (int i = index + 1; i < index + count; i++)
			{
				int j = i;
				do
					if (!SortPair(array, j - 1, j, comp))
						break;
				while (--j > index);
			}
		}

		/// <summary>Sorts two items to ensure that list[i] is less than list[j].</summary>
		/// <returns>True if the array elements were swapped, false if not.</returns>
		public static bool SortPair<T>(this IList<T> list, int i, int j, Comparison<T> comp)
		{
			if (i != j && comp(list[i], list[j]) > 0) {
				Swap(list, i, j);
				return true;
			}
			return false;
		}

		/// <summary>Swaps list[i] with list[j].</summary>
		public static void Swap<T>(this IList<T> list, int i, int j)
		{
			T tmp = list[i];
			list[i] = list[j];
			list[j] = tmp;
		}

		/// <summary>Gets the lowest index at which a condition is true, or -1 if nowhere.</summary>
		public static int IndexWhere<T>(this IList<T> list, Func<T, bool> pred)
		{
			return LCInterfaces.IndexWhere(list.AsListSource(), pred);
		}
		/// <summary>Gets the highest index at which a condition is true, or -1 if nowhere.</summary>
		public static int LastIndexWhere<T>(this IList<T> list, Func<T, bool> pred)
		{
			return LCInterfaces.LastIndexWhere(list.AsListSource(), pred);
		}
		public static ListSlice<T> Slice<T>(this IList<T> list, int start, int length)
		{
			return new ListSlice<T>(list, start, length);
		}
	}

	public static class DictionaryExt
	{
		/// <summary>An alternate version TryGetValue that returns a default value 
		/// if the key was not found in the dictionary, and that does not throw if 
		/// the key is null.</summary>
		/// <returns>The value associated with the specified key, or defaultValue 
		/// if no value is associated with the key.</returns>
		public static V TryGetValue<K, V>(this Dictionary<K, V> dict, K key, V defaultValue)
		{
			V value;
			if (key == null || !dict.TryGetValue(key, out value))
				return defaultValue;
			return value;
		}
		/// <inheritdoc cref="TryGetValue{K,V}(Dictionary{K,V},K,V)"/>
		public static V TryGetValue<K, V>(this IDictionary<K, V> dict, K key, V defaultValue)
		{
			V value;
			if (key == null || !dict.TryGetValue(key, out value))
				return defaultValue;
			return value;
		}
	}

	public static class TypeExt
	{
		public static string NameWithGenericArgs(this Type type)
		{
			string result = type.Name;
			if (type.IsGenericType)
			{
				// remove generic parameter count (e.g. `1)
				int i = result.LastIndexOf('`');
				if (i > 0)
					result = result.Substring(0, i);

				result = string.Format(
					"{0}<{1}>",
					result,
					StringExt.Join(", ", type.GetGenericArguments()
					                     .Select(t => NameWithGenericArgs(t))));
			}
			return result;
		}
	}

	public static class ExceptionExt
	{
		public static string ToDetailedString(this Exception ex) { return ToDetailedString(ex, 3); }
		
		public static string ToDetailedString(this Exception ex, int maxInnerExceptions)
		{
			StringBuilder sb = new StringBuilder();
			try {
				for (;;)
				{
					sb.AppendFormat("{0}: {1}\n", ex.GetType().Name, ex.Message);
					AppendDataList(ex.Data, sb, "  ", " = ", "\n");
					sb.Append(ex.StackTrace);
					if ((ex = ex.InnerException) == null)
						break;
					sb.Append("\n\n");
					sb.Append(Localize.From("Inner exception:"));
					sb.Append(' ');
				}
			} catch { }
			return sb.ToString();
		}

		public static string DataList(this Exception ex)
		{
			return DataList(ex, "", " = ", "\n");
		}
		public static string DataList(this Exception ex, string linePrefix, string keyValueSeparator, string newLine)
		{
			return AppendDataList(ex.Data, null, linePrefix, keyValueSeparator, newLine).ToString();
		}

		public static StringBuilder AppendDataList(IDictionary dict, StringBuilder sb, string linePrefix, string keyValueSeparator, string newLine)
		{
			sb = sb ?? new StringBuilder();
			foreach (DictionaryEntry kvp in dict)
			{
				sb.Append(linePrefix);
				sb.Append(kvp.Key);
				sb.Append(keyValueSeparator);
				sb.Append(kvp.Value);
				sb.Append(newLine);
			}
			return sb;
		}
	}
}
