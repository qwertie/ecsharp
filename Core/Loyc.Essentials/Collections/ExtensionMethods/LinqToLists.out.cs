// Generated from LinqToLists.ecs by LeMP custom tool. LeMP version: 2.7.0.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Collections.Generic;
using System.Linq;

namespace Loyc.Collections
{
	/// <summary>
	/// This class enhances LINQ-to-Objects with extension methods that preserve the
	/// interface (e.g. Take(IList&lt;int>) returns a struct that implements IList&lt;int>)
	/// or have higher performance than the ones in System.Linq.Enumerable.
	/// </summary><remarks>
	/// Helpful article: http://core.loyc.net/essentials/linq-to-lists.html
	/// <para/>
	/// For example, the <see cref="Enumerable.Last{T}"/> extension 
	/// method scans the entire list before returning the last item, while 
	/// <see cref="Last{T}(IReadOnlyList{T})"/> simply returns the last item directly.
	/// </remarks>
	public static partial class LinqToLists
	{
		// *** Visual Studio lets me edit the generated output, so I'm sprinkling notes to myself not to do that.
		public static int Count<T>(this IReadOnlyCollection<T> list) => list.Count;
		public static int Count<T>(this INegListSource<T> list) => list.Count;
	
		public static T FirstOrDefault<T>(this IListSource<T> list)
		{
			bool _;
			return list.TryGet(0, out _);
		}
		public static T FirstOrDefault<T>(this IListSource<T> list, T defaultValue)
		{
			bool fail;
			var result = list.TryGet(0, out fail);
			if (fail)
				return defaultValue;
			return result;
		}
	
		// *** Reminder: do not edit the generated output! ***
		/// <summary>Gets the last item from the list (at <c>list.Max</c>).</summary>
		/// <exception cref="EmptySequenceException">The list is empty</exception>
		public static T Last<T>(this IReadOnlyList<T> list)
		{
			int last = list.Count - 1;
			if (last < 0)
				throw new EmptySequenceException();
			return list[last];
		}
		/// <summary>Gets the last item from the list (Count - 1), or <c>defaultValue</c> if the list is empty.</summary>
		public static T LastOrDefault<T>(this IReadOnlyList<T> list, T defaultValue = default(T))
		{
			int last = list.Count - 1;
			return last < 0 ? defaultValue : list[last];
		}
		// *** Reminder: do not edit the generated output! ***
		public static T Last<T>(this IListAndListSource<T> list) { return Last((IListSource<T>) list); }
	
		/// <summary>Gets the last item from the list (at <c>list.Max</c>).</summary>
		/// <exception cref="EmptySequenceException">The list is empty</exception>
		public static T Last<T>(this INegListSource<T> list)
		{
			int last = list.Max;
			if (last < list.Min)
				throw new EmptySequenceException();
			return list[last];
		}
		/// <summary>Gets the last item from the list (at <c>list.Max</c>), or <c>defaultValue</c> if the list is empty.</summary>
		public static T LastOrDefault<T>(this INegListSource<T> list, T defaultValue = default(T))
		{
			int last = list.Max;
			return last < list.Min ? defaultValue : list[last];
		}
	
		// *** Reminder: do not edit the generated output! ***
		/// <summary>Skips the specified number of elements immediately and 
		/// returns a slice of part of the list that remains, or an empty 
		/// slice if <c>start</c> is greater than or equal to the <c>list.Count</c>.</summary>
		public static Slice_<T> Skip<T>(this IListSource<T> list, int start)
		{
			return new Slice_<T>(list, start);
		}
		/// <summary>Returns a slice of the specified number of elements from 
		/// the beginning of the list, or a slice of the entire list if <c>count</c> 
		/// is greater than or equal to the <c>list.Count</c>.</summary>
		public static Slice_<T> Take<T>(this IListSource<T> list, int count)
		{
			return new Slice_<T>(list, 0, count);
		}
		// *** Reminder: do not edit the generated output! ***
		/// <summary>Skips the specified number of elements immediately and 
		/// returns a slice of part of the list that remains, or an empty 
		/// slice if <c>start</c> is greater than or equal to the <c>list.Count</c>.</summary>
		public static ListSlice<T> Skip<T>(this IListAndListSource<T> list, int start)
		{
			return new ListSlice<T>(list, start);
		}
		/// <summary>Returns a slice of the specified number of elements from 
		/// the beginning of the list, or a slice of the entire list if <c>count</c> 
		/// is greater than or equal to the <c>list.Count</c>.</summary>
		public static ListSlice<T> Take<T>(this IListAndListSource<T> list, int count)
		{
			return new ListSlice<T>(list, 0, count);
		}
		// *** Reminder: do not edit the generated output! ***
		public static NegListSlice<T> Skip<T>(this INegListSource<T> list, int count)
		{
			CheckParam.IsNotNegative("count", count);
			return new NegListSlice<T>(list, checked(list.Min + count), int.MaxValue);
		}
		public static NegListSlice<T> Take<T>(this INegListSource<T> list, int count)
		{
			CheckParam.IsNotNegative("count", count);
			return new NegListSlice<T>(list, list.Min, count);
		}
	
		// *** Reminder: do not edit the generated output! ***
		/// <summary>Returns a slice of the initial elements of the list that meet the provided criteria. 
		/// The word "now" is added to the name because unlike Enumerable.TakeWhile, this method scans 
		/// the list immediately.</summary>
		/// <remarks>Example: new[] { 13, 16, 19, 2, 11, 12 }.TakeNowWhile(n => n > 10) returns a slice 
		/// (not a copy) of the first 3 elements.</remarks>
		public static Slice_<T> TakeNowWhile<T>(this IListSource<T> list, Func<T, bool> predicate)
		{
			Maybe<T> value;
			for (int i = 0;; i++) {
				if (!(value = list.TryGet(i)).HasValue)
					return new Slice_<T>(list);
				else if (!predicate(value.Value))
					return new Slice_<T>(list, 0, i);
			}
		}
		/// <summary>Returns a slice without the initial elements of the list that meet the specified
		/// criteria. The word "now" is added to the name because unlike Enumerable.SkipWhile, this 
		/// method scans the list immediately.</summary>
		/// <remarks>Example: new[] { 24, 28, 2, 12, 11 }.SkipNowWhile(n => n > 10) returns a slice 
		/// (not a copy) of the last 2 elements.</remarks>
		public static Slice_<T> SkipNowWhile<T>(this IListSource<T> list, Func<T, bool> predicate)
		{
			Maybe<T> value;
			for (int i = 0;; i++) {
				if (!(value = list.TryGet(i)).HasValue)
					return new Slice_<T>();
				else if (!predicate(value.Value))
					return new Slice_<T>(list, i);
			}
		}
		// *** Reminder: do not edit the generated output! ***
		/// <summary>Returns a slice of the initial elements of the list that meet the provided criteria. 
		/// The word "now" is added to the name because unlike Enumerable.TakeWhile, this method scans 
		/// the list immediately.</summary>
		/// <remarks>Example: new[] { 13, 16, 19, 2, 11, 12 }.TakeNowWhile(n => n > 10) returns a slice 
		/// (not a copy) of the first 3 elements.</remarks>
		public static NegListSlice<T> TakeNowWhile<T>(this INegListSource<T> list, Func<T, bool> predicate)
		{
			Maybe<T> value;
			for (int i = list.Min;; i++) {
				if (!(value = list.TryGet(i)).HasValue)
					return new NegListSlice<T>(list);
				else if (!predicate(value.Value))
					return new NegListSlice<T>(list, 0, i);
			}
		}
		/// <summary>Returns a slice without the initial elements of the list that meet the specified
		/// criteria. The word "now" is added to the name because unlike Enumerable.SkipWhile, this 
		/// method scans the list immediately.</summary>
		/// <remarks>Example: new[] { 24, 28, 2, 12, 11 }.SkipNowWhile(n => n > 10) returns a slice 
		/// (not a copy) of the last 2 elements.</remarks>
		public static NegListSlice<T> SkipNowWhile<T>(this INegListSource<T> list, Func<T, bool> predicate)
		{
			Maybe<T> value;
			for (int i = list.Min;; i++) {
				if (!(value = list.TryGet(i)).HasValue)
					return new NegListSlice<T>();
				else if (!predicate(value.Value))
					return new NegListSlice<T>(list, i);
			}
		}
		// *** Reminder: do not edit the generated output! ***
		/// <summary>Copies the contents of a list to an array.</summary>
		public static T[] ToArray<T>(this IReadOnlyList<T> c)
		{
			var array = new T[c.Count];
			for (int i = 0; i < array.Length; i++)
				array[i] = c[i];
			return array;
		}
	
		public static T[] ToArray<T>(this IListAndListSource<T> c) => MutableListExtensionMethods.LinqToLists.ToArray(c);
	
		/// <summary>Copies the contents of an <see cref="INegListSource{T}"/> to an array.</summary>
		public static T[] ToArray<T>(this INegListSource<T> c)
		{
			var array = new T[c.Count];
			int min = c.Min;
			for (int i = 0; i < array.Length; i++)
				array[i] = c[i + min];
			return array;
		}
	
		// *** Reminder: do not edit the generated output! ***
		public static SelectListSource<IListSource<T>, T, TResult> Select<T, TResult>(this IListSource<T> source, Func<T, TResult> selector)
		{
			return new SelectListSource<IListSource<T>, T, TResult>(source, selector);
		}
		public static SelectReadOnlyList<IReadOnlyList<T>, T, TResult> Select<T, TResult>(this IReadOnlyList<T> list, Func<T, TResult> selector)
		{
			return new SelectReadOnlyList<IReadOnlyList<T>, T, TResult>(list, selector);
		}
		public static SelectReadOnlyCollection<IReadOnlyCollection<T>, T, TResult> Select<T, TResult>(this IReadOnlyCollection<T> list, Func<T, TResult> selector)
		{
			return new SelectReadOnlyCollection<IReadOnlyCollection<T>, T, TResult>(list, selector);
		}
	
		/// <summary>Returns a reversed view of a read-only list.</summary>
		/// <remarks>This was originally named <c>ReverseView</c>. Changed to <c>Reverse</c> to match Linq's <c>Reverse(IEnumerable)</c>.</remarks>
		public static ReversedListSource<T> Reverse<T>(this IListSource<T> c)
		{
			return new ReversedListSource<T>(c);
		}
	
		// The following methods operate on mutable collections (contrary to the plan in 
		// #84 to avoid ambiguity errors) because there's no IReadOnlyList version of 
		// them and so we can reasonably expect the collection to implement IListAndListSource
		/// <summary>Returns an editable reversed view of a list.</summary>
		/// <remarks>This was originally named <c>ReverseView</c>. Changed to <c>Reverse</c> to match Linq's <c>Reverse(IEnumerable)</c>.</remarks>
		public static ReversedList<T> Reverse<T>(this IList<T> list) => new ReversedList<T>(list);
		public static ReversedList<T> Reverse<T>(this IListAndListSource<T> list) => new ReversedList<T>(list);
	
		public static bool SequenceEqual<TSource>(this IReadOnlyCollection<TSource> first, IReadOnlyCollection<TSource> second)
		{
			return first.Count == second.Count && Enumerable.SequenceEqual(first, second);
		}
	
			// TODO: interface-preserving version of this
			//     Projects each element of a sequence into a new form by incorporating the element's index.
			//   source:
			//     A sequence of values to invoke a transform function on.
			//   selector:
			//     A transform function to apply to each source element; the second parameter of
			//     the function represents the index of the source element.
			// public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, TResult> selector);
	}
}

namespace Loyc.Collections.MutableListExtensionMethods
{
	public static partial class LinqToLists
	{
		public static int Count<T>(this IList<T> list) => list.Count;
	
		public static T FirstOrDefault<T>(this IList<T> list, T defaultValue = default(T))
		{
			if (list.Count > 0)
				return list[0];
			return defaultValue;
		}
	
		// *** Reminder: do not edit the generated output! ***
		/// <summary>Gets the last item from the list (at <c>list.Max</c>).</summary>
		/// <exception cref="EmptySequenceException">The list is empty</exception>
		public static T Last<T>(this IList<T> list)
		{
			int last = list.Count - 1;
			if (last < 0)
				throw new EmptySequenceException();
			return list[last];
		}
		/// <summary>Gets the last item from the list (Count - 1), or <c>defaultValue</c> if the list is empty.</summary>
		public static T LastOrDefault<T>(this IList<T> list, T defaultValue = default(T))
		{
			int last = list.Count - 1;
			return last < 0 ? defaultValue : list[last];
		}
		// *** Reminder: do not edit the generated output! ***
		/// <summary>Skips the specified number of elements immediately and 
		/// returns a slice of part of the list that remains, or an empty 
		/// slice if <c>start</c> is greater than or equal to the <c>list.Count</c>.</summary>
		public static ListSlice<T> Skip<T>(this IList<T> list, int start)
		{
			return new ListSlice<T>(list, start);
		}
		/// <summary>Returns a slice of the specified number of elements from 
		/// the beginning of the list, or a slice of the entire list if <c>count</c> 
		/// is greater than or equal to the <c>list.Count</c>.</summary>
		public static ListSlice<T> Take<T>(this IList<T> list, int count)
		{
			return new ListSlice<T>(list, 0, count);
		}
		// *** Reminder: do not edit the generated output! ***
		/// <summary>Returns a slice of the initial elements of the list that meet the provided criteria. 
		/// The word "now" is added to the name because unlike Enumerable.TakeWhile, this method scans 
		/// the list immediately.</summary>
		/// <remarks>Example: new[] { 13, 16, 19, 2, 11, 12 }.TakeNowWhile(n => n > 10) returns a slice 
		/// (not a copy) of the first 3 elements.</remarks>
		public static ListSlice<T> TakeNowWhile<T>(this IList<T> list, Func<T, bool> predicate)
		{
			Maybe<T> value;
			for (int i = 0;; i++) {
				if (!(value = list.TryGet(i)).HasValue)
					return new ListSlice<T>(list);
				else if (!predicate(value.Value))
					return new ListSlice<T>(list, 0, i);
			}
		}
		/// <summary>Returns a slice without the initial elements of the list that meet the specified
		/// criteria. The word "now" is added to the name because unlike Enumerable.SkipWhile, this 
		/// method scans the list immediately.</summary>
		/// <remarks>Example: new[] { 24, 28, 2, 12, 11 }.SkipNowWhile(n => n > 10) returns a slice 
		/// (not a copy) of the last 2 elements.</remarks>
		public static ListSlice<T> SkipNowWhile<T>(this IList<T> list, Func<T, bool> predicate)
		{
			Maybe<T> value;
			for (int i = 0;; i++) {
				if (!(value = list.TryGet(i)).HasValue)
					return new ListSlice<T>();
				else if (!predicate(value.Value))
					return new ListSlice<T>(list, i);
			}
		}
		public static SelectCollection<ICollection<T>, T, TResult> Select<T, TResult>(this ICollection<T> list, Func<T, TResult> selector)
		{
			return new SelectCollection<ICollection<T>, T, TResult>(list, selector);
		}
		public static SelectList<IList<T>, T, TResult> Select<T, TResult>(this IList<T> list, Func<T, TResult> selector)
		{
			return new SelectList<IList<T>, T, TResult>(list, selector);
		}
	
		public static T[] ToArray<T>(this ICollection<T> c)
		{
			var array = new T[c.Count];
			c.CopyTo(array, 0);
			return array;
		}
	
		public static bool SequenceEqual<TSource>(this IList<TSource> first, IList<TSource> second)
		{
			return first.Count == second.Count && Enumerable.SequenceEqual(first, second);
		}
	
		#region Disambiguating methods (for collections that support them)
		// https://github.com/qwertie/ecsharp/issues/84 describes the problem solved by these methods
		// *** Reminder: do not edit the generated output! ***
		public static T Last<T>(this IListAndListSource<T> list) => Last((IList<T>) list);
		public static T LastOrDefault<T>(this IListAndListSource<T> list, T defaultValue = default(T)) => 
		LastOrDefault((IList<T>) list, defaultValue);
		public static T FirstOrDefault<T>(this IListAndListSource<T> list, T defaultValue = default(T)) => 
		FirstOrDefault((IList<T>) list, defaultValue);
	
		public static SelectList<T[], T, TResult> Select<T, TResult>(this T[] list, Func<T, TResult> selector) => 
		new SelectList<T[], T, TResult>(list, selector);
	
		// *** Reminder: do not edit the generated output! ***
		// Avoid ambiguity errors involving collections that support the ambiguity-avoidance interfaces
		public static SelectListSource<IListSource<T>, T, TResult> Select<T, TResult>(this IListAndListSource<T> source, Func<T, TResult> selector)
		{
			return new SelectListSource<IListSource<T>, T, TResult>(source, selector);
		}
		public static SelectReadOnlyCollection<IReadOnlyCollection<T>, T, TResult> Select<T, TResult>(this ICollectionAndReadOnly<T> list, Func<T, TResult> selector)
		{
			return new SelectReadOnlyCollection<IReadOnlyCollection<T>, T, TResult>(list, selector);
		}
		public static SelectList<IList<T>, T, TResult> Select<T, TResult>(this List<T> list, Func<T, TResult> selector)
		{
			return new SelectList<IList<T>, T, TResult>(list, selector);
		}
		public static SelectCollection<ICollection<T>, T, TResult> Select<T, TResult>(this HashSet<T> list, Func<T, TResult> selector)
		{
			return new SelectCollection<ICollection<T>, T, TResult>(list, selector);
		}
		public static SelectCollection<ICollection<KeyValuePair<K, V>>, KeyValuePair<K, V>, TResult> Select<K, V, TResult>(this Dictionary<K, V> list, Func<KeyValuePair<K, V>, TResult> selector)
		{
			return new SelectCollection<ICollection<KeyValuePair<K, V>>, KeyValuePair<K, V>, TResult>(list, selector);
		}
	
		// *** Reminder: do not edit the generated output! ***
		public static ListSlice<T> TakeNowWhile<T>(this IListAndListSource<T> list, Func<T, bool> predicate)
		{
			return TakeNowWhile((IList<T>) list, predicate);
		}
		public static ListSlice<T> SkipNowWhile<T>(this IListAndListSource<T> list, Func<T, bool> predicate)
		{
			return SkipNowWhile((IList<T>) list, predicate);
		}
	
		public static bool SequenceEqual<TSource>(this IListAndListSource<TSource> first, IListAndListSource<TSource> second)
		{
			return ((IList<TSource>) first).Count == ((IList<TSource>) second).Count && Enumerable.SequenceEqual(first, second);
		}
	
		#endregion
	}
}