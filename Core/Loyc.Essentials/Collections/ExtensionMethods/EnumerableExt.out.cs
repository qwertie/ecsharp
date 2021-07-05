// Generated from EnumerableExt.ecs by LeMP custom tool. LeMP version: 2.9.1.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>
	/// Additional extension methods for <see cref="IEnumerable{T}"/>,
	/// <see cref="IReadOnlyCollection{T}"/>, and <see cref="ICollection{T}"/>,
	/// beyond what LINQ provides.
	/// </summary>
	/// <remarks>
	/// The methods include <see cref="WithIndexes{T}"/>, which pairs each item of 
	/// a sequence with a 0-based index of that item; <see cref="ForEach{T}"/>, which 
	/// runs a lambda for each member of a sequence; <see cref="IndexWhere{T}"/>, which
	/// finds the index where a predicate is true; <see cref="AdjacentPairs{T}"/>, 
	/// which pairs each list item with the next one, and <see cref="MinOrDefault"/>,
	/// which finds the item such that some associated value is minimized (in contrast 
	/// to LINQ's Min(), which just returns the minimum value itself.) And there's more.
	/// </remarks>
	public static partial class EnumerableExt
	{
		public static void ForEach<T>(this IEnumerable<T> list, Action<T> action)
		{
			foreach (T item in list)
				action(item);
		}

		public static IEnumerable<KeyValuePair<int, T>> WithIndexes<T>(this IEnumerable<T> c)
		{
			int i = 0;
			foreach (T item in c) {
				yield return new KeyValuePair<int, T>(i, item);
				i++;
			}
		}

		/// <summary>Gets the lowest index at which a condition is true, or null if nowhere.</summary>
		public static int? FirstIndexWhere<T>(this IEnumerable<T> list, Func<T, bool> pred)
		{
			int i = 0;
			foreach (var item in list)
			{
				if (pred(item))
					return i;
				i++;
			}
			return null;
		}
		/// <summary>Gets the lowest index at which a condition is true, or -1 if nowhere.</summary>
		[Obsolete("Please use FirstIndexWhere, which returns null if nothing matches")] 
		public static int IndexWhere<T>(this IEnumerable<T> list, Func<T, bool> pred) => FirstIndexWhere(list, pred) ?? -1;

		
		/// <summary>Finds the minimum element's index in the list</summary>
		public static int IndexOfMin(this IEnumerable<int> source)
		{
			var e = source.GetEnumerator();
			if (!e.MoveNext())
				return -1;
			int i = 0, min_i = 0;
			for (int min = e.Current; e.MoveNext(); i++) {
				if (e.Current < min) {
					min = e.Current;
					min_i = i + 1;
				}
			}
			return min_i;
		}

		/// <summary>Finds the minimum element's index in the list</summary>
		public static int IndexOfMin<T>(this IEnumerable<T> source, Func<T, int> selector) => 
		IndexOfMin < T > (source, selector, out T _);

		/// <summary>Finds the minimum element's index in the list, and returns it with the item itself</summary>
		public static int IndexOfMin<T>(this IEnumerable<T> source, Func<T, int> selector, [MaybeNull] out T min)
		{
			min = default(T);
			var e = source.GetEnumerator();
			if (!e.MoveNext())
				return -1;
			int i = 0, min_i = 0, min_sel, cur_sel;
			for (min_sel = selector(min = e.Current); e.MoveNext(); i++) {
				T cur;
				if ((cur_sel = selector(cur = e.Current)) < min_sel) {
					min = cur;
					min_sel = cur_sel;
					min_i = i + 1;
				}
			}
			return min_i;
		}

		/// <summary>Finds the minimum element's index in the list</summary>
		public static int IndexOfMin<T>(this IEnumerable<T> source)
		{
			var e = source.GetEnumerator();
			if (!e.MoveNext())
				return -1;

			Comparer<T> comparer = Comparer<T>.Default;
			T min, cur;
			int i = 0, min_i = 0;
			for (min = e.Current; min == null; i++, min = e.Current)
				if (!e.MoveNext())
					return -1;
			while (e.MoveNext()) {
				i++;
				if ((cur = e.Current) != null && comparer.Compare(cur, min) < 0) {
					min = cur;
					min_i = i;
				}
			}
			return min_i;
		}

		/// <summary>Finds the minimum element's index in the list</summary>
		public static int IndexOfMin<T, R>(this IEnumerable<T> source, Func<T, R> selector) => 
		IndexOfMin<T, R>(source, selector, out T _);
		/// <summary>Finds the minimum element's index in the list</summary>
		public static int IndexOfMin<T, R>(this IEnumerable<T> source, Func<T, R> selector, [MaybeNull] out T min)
		{
			min = default(T);
			var e = source.GetEnumerator();
			if (!e.MoveNext())
				return -1;

			Comparer<R> comparer = Comparer<R>.Default;
			int i = 0, min_i = 0;
			R min_sel, cur_sel;
			for (min_sel = selector(min = e.Current); e.MoveNext(); i++) {
				T cur;
				if (comparer.Compare(cur_sel = selector(cur = e.Current), min_sel) < 0) {
					min = cur;
					min_sel = cur_sel;
					min_i = i + 1;
				}
			}
			return min_i;
		}
		[return: MaybeNull] 
		/// <summary>Finds the minimum element in the list (given some selector) and returns it</summary>
		/// <param name="list">A list that will be scanned from beginning to end</param>
		/// <param name="selector">A function that gets a comparable value for each item</param>
		/// <param name="defaultValue">A value to return if the list is empty (or all nulls)</param>
		/// <remarks>Unfortunately, the standard LINQ methods Max(lambda) and 
		/// Min(lambda) return the minimum or maximum value returned from the 
		/// lambda function, which is unfortunate because you often want the
		/// original value from the list, not the number returned by the lambda.
		/// If the developer actually wanted the min/max <i>number</i>, he 
		/// could have just used <c>list.Select(lambda).Max()</c>.
		/// </remarks>
		public static T MinItemOrDefault<T>(this IEnumerable<T> list, Func<T, int> selector, [AllowNull] T defaultValue = default(T))
		{
			int i = IndexOfMin(list, selector, out T? value);
			return i > -1 ? value : defaultValue;
		}
		[return: MaybeNull]	// Issue #137: https://github.com/qwertie/ecsharp/issues/137
		public static T MinItemOrDefault<T, S>(this IEnumerable<T> list, Func<T, S> selector, [AllowNull] T defaultValue = default(T))
		{
			int i = IndexOfMin(list, selector, out T? value);
			return i > -1 ? value : defaultValue;
		}

		// Passing `selector` from MinOrDefault to MinItemOrDefault causes an apparently 
		// spurious warning. Surprisingly it appears to be a bug in the C# compiler
		#pragma warning disable 8620
		[return: MaybeNull] 
		
		[Obsolete("This has been renamed to MinItemOrDefault")] 
		public static T MinOrDefault<T>(this IEnumerable<T> list, Func<T, int> selector, [AllowNull] T defaultValue = default(T)) => 
		MinItemOrDefault(list, selector, defaultValue);

		#pragma warning disable 8604	// Can't use ! suffix (issue #139)
		/// <summary>Finds the minimum element (as determined by the selector) and returns it.
		/// If the list is empty, an empty <see cref="Maybe{T}"/> value is returned.</summary>
		public static Maybe<T> MinItem<T>(this IEnumerable<T> list, Func<T, int> selector)
		{
			int i = IndexOfMin(list, selector, out T? value);
			return i > -1 ? value : new Maybe<T>();
		}
		/// <summary>Finds the minimum element (as determined by the selector) and returns it.
		/// If the list is empty, an empty <see cref="Maybe{T}"/> value is returned.</summary>
		public static Maybe<T> MinItem<T, S>(this IEnumerable<T> list, Func<T, S> selector)
		{
			int i = IndexOfMin(list, selector, out T? value);
			return i > -1 ? value : new Maybe<T>();
		}
		/// <summary>Finds the maximum element's index in the list</summary>
		public static int IndexOfMax(this IEnumerable<int> source)
		{
			var e = source.GetEnumerator();
			if (!e.MoveNext())
				return -1;
			int i = 0, min_i = 0;
			for (int min = e.Current; e.MoveNext(); i++) {
				if (e.Current > min) {
					min = e.Current;
					min_i = i + 1;
				}
			}
			return min_i;
		}

		/// <summary>Finds the maximum element's index in the list</summary>
		public static int IndexOfMax<T>(this IEnumerable<T> source, Func<T, int> selector) => 
		IndexOfMax < T > (source, selector, out T _);

		/// <summary>Finds the maximum element's index in the list, and returns it with the item itself</summary>
		public static int IndexOfMax<T>(this IEnumerable<T> source, Func<T, int> selector, [MaybeNull] out T min)
		{
			min = default(T);
			var e = source.GetEnumerator();
			if (!e.MoveNext())
				return -1;
			int i = 0, min_i = 0, min_sel, cur_sel;
			for (min_sel = selector(min = e.Current); e.MoveNext(); i++) {
				T cur;
				if ((cur_sel = selector(cur = e.Current)) > min_sel) {
					min = cur;
					min_sel = cur_sel;
					min_i = i + 1;
				}
			}
			return min_i;
		}

		/// <summary>Finds the maximum element's index in the list</summary>
		public static int IndexOfMax<T>(this IEnumerable<T> source)
		{
			var e = source.GetEnumerator();
			if (!e.MoveNext())
				return -1;

			Comparer<T> comparer = Comparer<T>.Default;
			T min, cur;
			int i = 0, min_i = 0;
			for (min = e.Current; min == null; i++, min = e.Current)
				if (!e.MoveNext())
					return -1;
			while (e.MoveNext()) {
				i++;
				if ((cur = e.Current) != null && comparer.Compare(cur, min) > 0) {
					min = cur;
					min_i = i;
				}
			}
			return min_i;
		}

		/// <summary>Finds the maximum element's index in the list</summary>
		public static int IndexOfMax<T, R>(this IEnumerable<T> source, Func<T, R> selector) => 
		IndexOfMin<T, R>(source, selector, out T _);
		/// <summary>Finds the maximum element's index in the list</summary>
		public static int IndexOfMax<T, R>(this IEnumerable<T> source, Func<T, R> selector, [MaybeNull] out T min)
		{
			min = default(T);
			var e = source.GetEnumerator();
			if (!e.MoveNext())
				return -1;

			Comparer<R> comparer = Comparer<R>.Default;
			int i = 0, min_i = 0;
			R min_sel, cur_sel;
			for (min_sel = selector(min = e.Current); e.MoveNext(); i++) {
				T cur;
				if (comparer.Compare(cur_sel = selector(cur = e.Current), min_sel) > 0) {
					min = cur;
					min_sel = cur_sel;
					min_i = i + 1;
				}
			}
			return min_i;
		}
		[return: MaybeNull] 
		/// <summary>Finds the maximum element in the list (given some selector) and returns it</summary>
		/// <param name="list">A list that will be scanned from beginning to end</param>
		/// <param name="selector">A function that gets a comparable value for each item</param>
		/// <param name="defaultValue">A value to return if the list is empty (or all nulls)</param>
		/// <remarks>Unfortunately, the standard LINQ methods Max(lambda) and 
		/// Min(lambda) return the minimum or maximum value returned from the 
		/// lambda function, which is unfortunate because you often want the
		/// original value from the list, not the number returned by the lambda.
		/// If the developer actually wanted the min/max <i>number</i>, he 
		/// could have just used <c>list.Select(lambda).Max()</c>.
		/// </remarks>
		public static T MaxItemOrDefault<T>(this IEnumerable<T> list, Func<T, int> selector, [AllowNull] T defaultValue = default(T))
		{
			int i = IndexOfMax(list, selector, out T? value);
			return i > -1 ? value : defaultValue;
		}
		[return: MaybeNull]	// Issue #137: https://github.com/qwertie/ecsharp/issues/137
		public static T MaxItemOrDefault<T, S>(this IEnumerable<T> list, Func<T, S> selector, [AllowNull] T defaultValue = default(T))
		{
			int i = IndexOfMax(list, selector, out T? value);
			return i > -1 ? value : defaultValue;
		}

		// Passing `selector` from MinOrDefault to MinItemOrDefault causes an apparently 
		// spurious warning. Surprisingly it appears to be a bug in the C# compiler
		#pragma warning disable 8620
		[return: MaybeNull] 
		
		[Obsolete("This has been renamed to MaxItemOrDefault")] 
		public static T MaxOrDefault<T>(this IEnumerable<T> list, Func<T, int> selector, [AllowNull] T defaultValue = default(T)) => 
		MaxItemOrDefault(list, selector, defaultValue);

		#pragma warning disable 8604	// Can't use ! suffix (issue #139)
		/// <summary>Finds the maximum element (as determined by the selector) and returns it.
		/// If the list is empty, an empty <see cref="Maybe{T}"/> value is returned.</summary>
		public static Maybe<T> MaxItem<T>(this IEnumerable<T> list, Func<T, int> selector)
		{
			int i = IndexOfMax(list, selector, out T? value);
			return i > -1 ? value : new Maybe<T>();
		}
		/// <summary>Finds the maximum element (as determined by the selector) and returns it.
		/// If the list is empty, an empty <see cref="Maybe{T}"/> value is returned.</summary>
		public static Maybe<T> MaxItem<T, S>(this IEnumerable<T> list, Func<T, S> selector)
		{
			int i = IndexOfMax(list, selector, out T? value);
			return i > -1 ? value : new Maybe<T>();
		}

		public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> list) where T: class
		{
			foreach (var item in list)
				if (item != null)
					yield return item;
		}
		public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> list) where T: struct
		{
			foreach (var item in list)
				if (item != null)
					yield return item.Value;
		}

		/// <summary>Combines 'Select' and 'Where' in a single operation.</summary>
		/// <param name="filter">If this function returns <see cref="Maybe{O}.NoValue"/> 
		/// then the element is suppressed from the output; otherwise the 
		/// <see cref="Maybe{T}.Value"/> is sent to the output.</param>
		/// <returns>A sequence filtered and changed by <c>filter</c>.</returns>
		public static IEnumerable<Out> SelectFilter<T, Out>(this IEnumerable<T> list, Func<T, Maybe<Out>> filter)
		{
			foreach (var item in list) {
				var maybe = filter(item);
				if (maybe.HasValue)
					yield return maybe.Value;
			}
		}

		/// <summary>Determines the index of a specific value.</summary>
		/// <returns>The index of the value, if found, or -1 if it was not found.</returns>
		/// <remarks>
		/// At first, this method was a member of IListSource itself, just in 
		/// case the source might have some kind of fast lookup logic (e.g. binary 
		/// search) or custom comparer. However, since the item to find is an "in" 
		/// argument, it would prevent IListSource from being marked covariant when
		/// I upgrade to C# 4.
		/// </remarks>
		public static int IndexOf<T>(this IEnumerable<T> list, T item) => IndexOf(list, item, EqualityComparer<T>.Default);
		public static int IndexOf<T>(this IEnumerable<T> list, T item, IEqualityComparer<T> comp)
		{
			var e = list.GetEnumerator();
			for (int index = 0; e.MoveNext(); index++)
				if (comp.Equals(e.Current, item)) {
					e.Dispose();
					return index;
				}
			e.Dispose();
			return -1;
		}

		/// <summary>A companion to <see cref="Enumerable.SequenceEqual{T}"/> that 
		/// computes a hashcode for a list.</summary>
		public static int SequenceHashCode<T>(this IEnumerable<T> list)
		{
			return SequenceHashCode(list, EqualityComparer<T>.Default);
		}
		public static int SequenceHashCode<T>(this IEnumerable<T> list, IEqualityComparer<T> comp)
		{
			// I am no expert in hash functions.
			int hc = 517617279;	// a random number
			foreach (T item in list)
				// GetHashCode(null) works (returns 0) but gives us a warning anyway
				hc = hc * 257 ^ comp.GetHashCode(item!);
			return hc;
		}

		/// <summary>Upcasts a sequence.</summary>
		/// <remarks>In .NET 4+ this is a no-op that just returns <c>list</c>,
		/// but in .NET 3.5 that's illegal, so this method creates an adapter.</remarks>
		[Obsolete(".NET 4+ can upcast by itself without this method")] 
		public static IEnumerable<Base> Upcast<Base, Derived>(this IEnumerable<Derived> list) where Derived: class, Base
		{
			return list;
		}

		/// <summary>Returns all adjacent pairs (e.g. for the list {1,2,3}, returns {(1,2),(2,3)})</summary>
		public static IEnumerable<Pair<T, T>> AdjacentPairs<T>(this IEnumerable<T> list) { return AdjacentPairs(list.GetEnumerator()); }
		public static IEnumerable<Pair<T, T>> AdjacentPairs<T>(this IEnumerator<T> e)
		{
			if (e.MoveNext()) {
				T prev = e.Current;
				while (e.MoveNext()) {
					T cur = e.Current;
					yield return new Pair<T, T>(prev, cur);
					prev = cur;
				}
			}
		}

		/// <summary>Returns all adjacent pairs, treating the first and last 
		/// pairs as adjacent (e.g. for the list {1,2,3,4}, returns the pairs
		/// {(1,2),(2,3),(3,4),(4,1)}.)</summary>
		public static IEnumerable<Pair<T, T>> AdjacentPairsCircular<T>(this IEnumerable<T> list) { return AdjacentPairs(list.GetEnumerator()); }
		public static IEnumerable<Pair<T, T>> AdjacentPairsCircular<T>(this IEnumerator<T> e)
		{
			if (e.MoveNext()) {
				T first = e.Current, prev = first;
				while (e.MoveNext()) {
					T cur = e.Current;
					yield return new Pair<T, T>(prev, cur);
					prev = cur;
				}
				yield return new Pair<T, T>(prev, first);
			}
		}

		public static List<T> ToList<T>(this IEnumerator<T> e)
		{
			var list = new List<T>();
			while (e.MoveNext())
				list.Add(e.Current);
			return list;
		}
	}
}