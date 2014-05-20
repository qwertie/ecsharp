using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	public static class EnumerableExt
	{
		public static IEnumerable<KeyValuePair<int, T>> WithIndexes<T>(this IEnumerable<T> c)
		{
			int i = 0;
			foreach (T item in c)
				yield return new KeyValuePair<int, T>(i, item);
		}
		public static void ForEach<T>(this IEnumerable<T> list, Action<T> action)
		{
			foreach (T item in list)
				action(item);
		}
		
		/// <summary>Gets the lowest index at which a condition is true, or -1 if nowhere.</summary>
		public static int IndexWhere<T>(this IEnumerable<T> list, Func<T, bool> pred)
		{
			int i = 0;
			foreach (var item in list) {
				if (pred(item))
					return i;
				i++;
			}
			return -1;
		}

		public static int IndexOfMin(this IEnumerable<int> source)
		{
			var e = source.GetEnumerator();
			if (!e.MoveNext())
				return -1;
			int i = 0, min_i = 0;
			for (int min = e.Current; e.MoveNext(); i++) {
				if (min > e.Current) {
					min = e.Current;
					min_i = i+1;
				}
			}
			return min_i;
		}
		public static int IndexOfMin<T>(this IEnumerable<T> source, Func<T, int> selector)
		{
			int _;
			return IndexOfMin<T>(source, selector, out _);
		}
		public static int IndexOfMin<T>(this IEnumerable<T> source, Func<T, int> selector, out int min)
		{
			min = 0;
			var e = source.GetEnumerator();
			if (!e.MoveNext())
				return -1;
			int i = 0, min_i = 0, cur;
			for (min = selector(e.Current); e.MoveNext(); i++) {
				if (min > (cur = selector(e.Current))) {
					min = cur;
					min_i = i+1;
				}
			}
			return min_i;
		}
		public static int IndexOfMax(this IEnumerable<int> source)
		{
			var e = source.GetEnumerator();
			if (!e.MoveNext())
				return -1;
			int i = 0, max_i = 0;
			for (int max = e.Current; e.MoveNext(); i++) {
				if (max < e.Current) {
					max = e.Current;
					max_i = i+1;
				}
			}
			return max_i;
		}
		public static int IndexOfMax<T>(this IEnumerable<T> source, Func<T, int> selector)
		{
			int _;
			return IndexOfMax<T>(source, selector, out _);
		}
		public static int IndexOfMax<T>(this IEnumerable<T> source, Func<T, int> selector, out int max)
		{
			max = 0;
			var e = source.GetEnumerator();
			if (!e.MoveNext())
				return -1;
			int i = 0, max_i = 0, cur;
			for (max = selector(e.Current); e.MoveNext(); i++) {
				if (max < (cur = selector(e.Current))) {
					max = cur;
					max_i = i+1;
				}
			}
			return max_i;
		}


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
		public static int IndexOfMin<T, R>(this IEnumerable<T> source, Func<T, R> selector)
		{
			R _;
			return IndexOfMin<T, R>(source, selector, out _);
		}
		public static int IndexOfMin<T, R>(this IEnumerable<T> source, Func<T, R> selector, out R min)
		{
			min = default(R);
			var e = source.GetEnumerator();
			if (!e.MoveNext())
				return -1;

			Comparer<R> comparer = Comparer<R>.Default;
			R cur;
			int i = 0, min_i = 0;
			for (min = selector(e.Current); min == null; i++, min = selector(e.Current))
				if (!e.MoveNext())
					return -1;
			while (e.MoveNext()) {
				i++;
				if ((cur = selector(e.Current)) != null && comparer.Compare(cur, min) < 0) {
					min = cur;
					min_i = i;
				}
			}
			return min_i;
		}
		public static int IndexOfMax<T>(this IEnumerable<T> source)
		{
			var e = source.GetEnumerator();
			if (!e.MoveNext())
				return -1;

			Comparer<T> comparer = Comparer<T>.Default;
			T max, cur;
			int i = 0, max_i = 0;
			for (max = e.Current; max == null; i++, max = e.Current)
				if (!e.MoveNext())
					return -1;
			while (e.MoveNext()) {
				i++;
				if ((cur = e.Current) != null && comparer.Compare(cur, max) > 0) {
					max = cur;
					max_i = i;
				}
			}
			return max_i;
		}
		public static int IndexOfMax<T, R>(this IEnumerable<T> source, Func<T, R> selector)
		{
			R _;
			return IndexOfMax(source, selector, out _);
		}
		public static int IndexOfMax<T, R>(this IEnumerable<T> source, Func<T, R> selector, out R max)
		{
			max = default(R);
			var e = source.GetEnumerator();
			if (!e.MoveNext())
				return -1;

			Comparer<R> comparer = Comparer<R>.Default;
			R cur;
			int i = 0, max_i = 0;
			for (max = selector(e.Current); max == null; i++, max = selector(e.Current))
				if (!e.MoveNext())
					return -1;
			while (e.MoveNext()) {
				i++;
				if ((cur = selector(e.Current)) != null && comparer.Compare(cur, max) > 0) {
					max = cur;
					max_i = i;
				}
			}
			return max_i;
		}

		/// <summary>Returns the item in the list that has the minimum value for some selector.</summary>
		/// <inheritdoc cref="MaxOrDefault{T}(IEnumerable{T}, Func{T,int}, T)"/>
		public static T MinOrDefault<T>(this IEnumerable<T> list, Func<T, int> selector, T defaultValue = default(T))
		{
			var e = list.GetEnumerator();
			if (!e.MoveNext())
				return defaultValue;
			T minT = e.Current, curT;
			if (e.MoveNext()) {
				int min = selector(minT), cur;
				do
					if ((cur = selector(curT = e.Current)) < min) {
						min = cur;
						minT = curT;
					}
				while (e.MoveNext());
			}
			return minT;
		}
		/// <inheritdoc cref="MinOrDefault{T}(IEnumerable{T}, Func{T,int}, T)"/>
		public static T MinOrDefault<T>(this IEnumerable<T> list, Func<T, double> selector, T defaultValue = default(T))
		{
			var e = list.GetEnumerator();
			if (!e.MoveNext())
				return defaultValue;
			T minT = e.Current, curT;
			if (e.MoveNext()) {
				double min = selector(minT), cur;
				do
					if ((cur = selector(curT = e.Current)) < min) {
						min = cur;
						minT = curT;
					}
				while (e.MoveNext());
			}
			return minT;
		}

		public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> list) where T : class
		{
			foreach (var item in list)
				if (item != null)
					yield return item;
		}
		public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> list) where T : struct
		{
			foreach (var item in list)
				if (item != null)
					yield return item.Value;
		}

		/// <summary>Returns the <i>item</i> in the list that has the maximum value for some selector.</summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list">A list to search</param>
		/// <param name="selector">A function that takes a number from the list</param>
		/// <param name="defaultValue">A value </param>
		/// <remarks>Unfortunately, the standard LINQ methods Max(lambda) and 
		/// Min(lambda) return the minimum or maximum value returned from the 
		/// lambda function, which is unfortunate because you often want the
		/// original value from the list, not the number returned by the lambda.
		/// That's a flawed design, because often you want the original T value
		/// and not the projected number; if the developer actually wanted the 
		/// min/max <i>number</i>, he could have just used 
		/// <c>list.Select(lambda).Max()</c> instead of <c>list.Max(lambda)</c>.
		/// <para/>
		/// So MinOrDefault() and MaxByDefault() are different in two ways: 
		/// (1) they returns the original T value from the collection, and
		/// (2) if the collection is empty, they return a default value.
		/// </remarks>
		public static T MaxOrDefault<T>(this IEnumerable<T> list, Func<T, int> selector, T defaultValue = default(T))
		{
			var e = list.GetEnumerator();
			if (!e.MoveNext())
				return defaultValue;
			T maxT = e.Current, curT;
			if (e.MoveNext()) {
				int max = selector(maxT), cur;
				do
					if ((cur = selector(curT = e.Current)) > max) {
						max = cur;
						maxT = curT;
					}
				while (e.MoveNext());
			}
			return maxT;
		}
		/// <inheritdoc cref="MaxOrDefault{T}(IEnumerable{T}, Func{T,int}, T)"/>
		public static T MaxOrDefault<T>(this IEnumerable<T> list, Func<T, double> selector, T defaultValue = default(T))
		{
			var e = list.GetEnumerator();
			if (!e.MoveNext())
				return defaultValue;
			T maxT = e.Current, curT;
			if (e.MoveNext()) {
				double max = selector(maxT), cur;
				do
					if ((cur = selector(curT = e.Current)) > max) {
						max = cur;
						maxT = curT;
					}
				while (e.MoveNext());
			}
			return maxT;
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
		public static int IndexOf<T>(this IEnumerable<T> list, T item) { return IndexOf(list, item, EqualityComparer<T>.Default); }
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
			int hc = 517617279; // a random number
			foreach (T item in list)
				hc = hc * 257 ^ comp.GetHashCode(item);
			return hc;
		}

		public static IEnumerable<Base> Upcast<Base, Derived>(this IEnumerable<Derived> list) where Derived : class, Base
		{
			#if DotNet2 || DotNet3
			return list.Select<Derived, Base>(o => o);
			#else
			return list;
			#endif
		}
	}
}
