using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections.Impl;
using Loyc.Math;
using System.Diagnostics;

namespace Loyc.Collections
{
	/// <summary>Extension methods and helper methods for <see cref="List{T}"/>,
	/// <see cref="IList{T}"/>, <see cref="IListSource<T>"/>, arrays, and for 
	/// related mutable interfaces such as <see cref="IArray{T}"/>. 
	/// </summary>
	/// <remarks>Extension methods that only apply to Loyc's new interfaces will 
	/// go in <see cref="LCExt"/>.</remarks>
	public static class ListExt
	{
		public static void CopyTo<T>(this IReadOnlyCollection<T> c, T[] array, int arrayIndex)
		{
			int space = array.Length - arrayIndex;
			if (c.Count > space)
				throw new ArgumentException(Localize.From("CopyTo: array is too small ({0} < {1})", space, c.Count));
			foreach (var item in c)
				array[arrayIndex++] = item;
		}

		public static T TryGet<T>(this T[] list, int index, T defaultValue)
		{
			if ((uint)index < (uint)list.Length)
				return list[index];
			return defaultValue;
		}
		public static T TryGet<T>(this List<T> list, int index, T defaultValue)
		{
			if ((uint)index < (uint)list.Count)
				return list[index];
			return defaultValue;
		}
		public static T TryGet<T>(this IList<T> list, int index, T defaultValue)
		{
			if ((uint)index < (uint)list.Count)
				return list[index];
			return defaultValue;
		}

		public static void RemoveRange<T>(this IList<T> list, int index, int count)
		{
			if (index < 0)
				throw new IndexOutOfRangeException(index.ToString() + " < 0");
			if (count > list.Count - index)
				throw new IndexOutOfRangeException(index.ToString() + " + " + count.ToString() + " > " + list.Count.ToString());
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
				while (--dif != 0);
			} else if (dif < 0) {
				int i = list.Count;
				do list.RemoveAt(--i);
				while (++dif != 0);
			}
		}
		public static void Resize<T>(this IList<T> list, int newSize)
		{
			int dif = newSize - list.Count;
			if (dif > 0) {
				do list.Add(default(T));
				while (--dif != 0);
			} else if (dif < 0) {
				int i = list.Count;
				do list.RemoveAt(--i);
				while (++dif != 0);
			}
		}

		public static IEnumerable<Pair<A, B>> Zip<A, B>(this IEnumerable<A> a, IEnumerable<B> b)
		{
			IEnumerator<A> ea = a.GetEnumerator();
			IEnumerator<B> eb = b.GetEnumerator();
			while (ea.MoveNext() && eb.MoveNext())
				yield return new Pair<A, B>(ea.Current, eb.Current);
		}
		public static IEnumerable<Pair<A, B>> ZipLeft<A, B>(this IEnumerable<A> a, IEnumerable<B> b, B defaultB)
		{
			IEnumerator<A> ea = a.GetEnumerator();
			IEnumerator<B> eb = b.GetEnumerator();
			bool successA;
			while ((successA = ea.MoveNext()) && eb.MoveNext())
				yield return new Pair<A, B>(ea.Current, eb.Current);
			if (successA) do
				yield return new Pair<A, B>(ea.Current, defaultB);
			while (ea.MoveNext());
		}
		public static IEnumerable<C> ZipLeft<A, B, C>(this IEnumerable<A> a, IEnumerable<B> b, B defaultB, Func<A, B, C> resultSelector)
		{
			foreach (var pair in ZipLeft(a, b, defaultB))
				yield return resultSelector(pair.A, pair.B);
		}
		public static IEnumerable<Pair<A, B>> ZipLonger<A, B>(this IEnumerable<A> a, IEnumerable<B> b)
		{
			return ZipLonger(a, b, default(A), default(B));
		}
		public static IEnumerable<Pair<A, B>> ZipLonger<A, B>(this IEnumerable<A> a, IEnumerable<B> b, A defaultA, B defaultB)
		{
			IEnumerator<A> ea = a.GetEnumerator();
			IEnumerator<B> eb = b.GetEnumerator();
			bool successA, successB;
			while ((successA = ea.MoveNext()) & (successB = eb.MoveNext()))
				yield return new Pair<A, B>(ea.Current, eb.Current);
			if (successA) do
				yield return new Pair<A, B>(ea.Current, defaultB);
				while (ea.MoveNext());
			else if (successB)
				do
					yield return new Pair<A, B>(defaultA, eb.Current);
				while (eb.MoveNext());
		}
		public static IEnumerable<C> ZipLonger<A, B, C>(this IEnumerable<A> a, IEnumerable<B> b, A defaultA, B defaultB, Func<A, B, C> resultSelector)
		{
			foreach (var pair in ZipLonger(a, b, defaultA, defaultB))
				yield return resultSelector(pair.A, pair.B);
		}

		/// <summary>Returns an array of Length <c>count</c> containing the numbers 0 through <c>count-1</c>.</summary>
		public static int[] RangeArray(int count)
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
		/// <para/>
		/// This quicksort algorithm uses a best-of-three pivot so that it remains
		/// performant (fast) if the input is already sorted. It is designed to 
		/// perform reasonably well in case the data contains many duplicates (not
		/// verified). It is also designed to avoid using excessive stack space if 
		/// a worst-case input occurs that requires O(N^2) time.
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

		/// <summary>Uses a partial quicksort, known as "quickselect", to find the
		/// lowest k elements in a list.</summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list">A list that will be partially sorted.</param>
		/// <param name="k">Number of elements that will be sorted at the beginning 
		/// of the list when this method returns. If <c>k > list.Count</c>, the 
		/// entire list is sorted.</param>
		/// <returns>Although the list is modified in-place, a slice of the 
		/// beginning of the same list is returned. The slice will have k elements 
		/// (or list.Count elements, whichever is less).</returns>
		/// <remarks>Whereas quicksort typically runs in O(N log N) time,
		/// quickselect typically requires O(N) time for small values of k, 
		/// although the worst-case performance remains O(N^2).</remarks>
		public static ListSlice<T> FindLowestK<T>(this IList<T> list, int k)
		{
			return FindLowestK(list, k, Comparer<T>.Default.Compare);
		}
		public static ListSlice<T> FindLowestK<T>(this IList<T> list, int k, Comparison<T> comp)
		{
			return FindLowestK(list, 0, list.Count, k, comp);
		}
		public static ListSlice<T> FindLowestK<T>(this IList<T> list, int index, int count, int k, Comparison<T> comp)
		{
			Sort(list, index, count, comp, null, k);
			return new ListSlice<T>(list, index, System.Math.Min(count, k));
		}

		/// <summary>A stable version of <see cref="FindLowestK"/>. This means 
		/// that when k>1 and adjacent results at the beginning of <c>list</c> 
		/// compare equal, they keep the same order that they had originally.</summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list">A list that will be partially sorted.</param>
		/// <param name="k">Number of elements that will be sorted at the beginning 
		/// of the list when this method returns. If <c>k > list.Count</c>, the 
		/// entire list is sorted.</param>
		/// <returns>This method uses the quickselect algorithm and stability is
		/// achieved using a temporary array of <c>list.Count</c> integers.</returns>
		public static ListSlice<T> FindLowestKStable<T>(this IList<T> list, int k)
		{
			return FindLowestKStable(list, k, Comparer<T>.Default.Compare);
		}
		public static ListSlice<T> FindLowestKStable<T>(this IList<T> list, int k, Comparison<T> comp)
		{
			Sort(list, 0, list.Count, comp, RangeArray(list.Count), k);
			return new ListSlice<T>(list, 0, k);
		}

		// Used by Sort, StableSort, FindLowestK, FindLowestKStable.
		private static void Sort<T>(this IList<T> list, int index, int count, Comparison<T> comp, 
		                            int[] indexes, int quickSelectElems = int.MaxValue)
		{
			// This code duplicates the code in InternalList.Sort(), except
			// that it also supports stable sorting (indexes parameter) and
			// quickselect (sorting the first 'quickSelectElems' elements). This 
			// version is slower; two versions exist so that array sorting can 
			// be done faster.
			CheckParam.Range("index", index, 0, list.Count);
			CheckParam.Range("count", count, 0, list.Count - index);
			if (quickSelectElems <= 0)
				return;

			for (;;) {
				if (count < InternalList.QuickSortThreshold)
				{
					if (count <= 2) {
						if (count == 2) {
							int c = comp(list[index], list[index+1]);
							if (c > 0 || (c == 0 && indexes != null && indexes[index] > indexes[index+1]))
								Swap(list, index, index+1);
						}
						return;
					} else if (indexes == null) {
						InsertionSort(list, index, count, comp);
						return;
					}
				}

				// TODO: fix slug: PickPivot does not use 'indexes'. Makes stable sort slower if many duplicate items.
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
					if ((quickSelectElems -= leftSize + 1) <= 0)
						break;
				}
				else
				{	// Iteratively sort the left partition; recursively sort the right
					count = leftSize;
					Sort(list, index + leftSize + 1, rightSize, comp, indexes, quickSelectElems - (leftSize + 1));
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
		public static void Swap<T>(this IArray<T> list, int i, int j)
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
		public static ListSlice<T> Slice<T>(this IList<T> list, int start, int length = int.MaxValue)
		{
			return new ListSlice<T>(list, start, length);
		}
		public static ArraySlice<T> Slice<T>(this T[] list, int start, int length = int.MaxValue)
		{
			return new ArraySlice<T>(list, start, length);
		}
		public static Slice_<T> Slice<T>(this IListSource<T> array, int start, int count = int.MaxValue)
		{
			return new Slice_<T>(array, start, count);
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

		static Random _r = new Random();
		public static void Randomize<T>(this IList<T> list)
		{
			int count = list.Count;
			for (int i = 0; i < count; i++)
				list.Swap(i, _r.Next(count));
		}
		public static void Randomize<T>(this T[] list)
		{
			for (int i = 0; i < list.Length; i++)
				MathEx.Swap(ref list[i], ref list[_r.Next(list.Length)]);
		}
		/// <summary>Quickly makes a copy of a list, as an array, in random order.</summary>
		public static T[] Randomized<T>(this IList<T> list)
		{
			T[] copy = new T[list.Count];
			list.CopyTo(copy, 0);
			Randomize(copy);
			return copy;
		}

		/// <summary>Maps an array to another array of the same length.</summary>
		public static R[] SelectArray<T, R>(this T[] input, Func<T,R> selector)
		{
			if (input == null)
				return null;
			R[] result = new R[input.Length];
			for (int i = 0; i < result.Length; i++)
				result[i] = selector(input[i]);
			return result;
		}

		/// <summary>Maps a list to an array of the same length.</summary>
		public static R[] SelectArray<T, R>(this ICollection<T> input, Func<T,R> selector)
		{
			if (input == null)
				return null;
			R[] result = new R[input.Count];
			var e = input.GetEnumerator();
			for (int i = 0; i < result.Length; i++) {
				e.MoveNext();
				result[i] = selector(e.Current);
			}
			return result;
		}

		/// <summary>Maps a list to an array of the same length.</summary>
		public static R[] SelectArray<T, R>(this IReadOnlyList<T> input, Func<T,R> selector)
		{
			if (input == null)
				return null;
			R[] result = new R[input.Count];
			for (int i = 0; i < result.Length; i++)
				result[i] = selector(input[i]);
			return result;
		}

		/// <summary>Removes the all the elements that match the conditions defined by the specified predicate.</summary>
		/// <returns>The number of elements removed from the list</returns>
		public static int RemoveAll<T>(this IList<T> list, Predicate<T> match)
		{
			int to = 0, c = list.Count;
			for (int i = 0; i < c; i++) {
				if (!match(list[i])) {
					if (i != to)
						list[to] = list[i];
					to++;
				}
			}
			list.RemoveRange(to, c - to);
			return c - to;
		}

		public static void Reverse<T>(this IList<T> list) 
		{
			int c = list.Count;
			for (int i = 0; i < (c >> 1); i++)
				list.Swap(i, c - i);
		}
		public static void Reverse<T>(this IArray<T> list) 
		{
			int c = list.Count;
			for (int i = 0; i < (c >> 1); i++)
				list.Swap(i, c - i);
		}

		public static void AddRange<T>(this IList<T> list, IEnumerable<T> range)
		{
			foreach (var item in range)
				list.Add(item);
		}

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
		public static T MaxOrDefault<T>(this IEnumerable<T> list, Func<T, float> selector, T defaultValue = default(T))
		{
			var e = list.GetEnumerator();
			if (!e.MoveNext())
				return defaultValue;
			T maxT = e.Current, curT;
			if (e.MoveNext()) {
				float max = selector(maxT), cur;
				do
					if ((cur = selector(curT = e.Current)) > max) {
						max = cur;
						maxT = curT;
					}
				while (e.MoveNext());
			}
			return maxT;
		}
		public static T MinOrDefault<T>(this IEnumerable<T> list, Func<T, float> selector, T defaultValue = default(T))
		{
			var e = list.GetEnumerator();
			if (!e.MoveNext())
				return defaultValue;
			T minT = e.Current, curT;
			if (e.MoveNext()) {
				float min = selector(minT), cur;
				do
					if ((cur = selector(curT = e.Current)) < min) {
						min = cur;
						minT = curT;
					}
				while (e.MoveNext());
			}
			return minT;
		}
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

		/// <summary>Increases the list size by <c>spaceNeeded</c> and copies 
		/// elements starting at <c>list[index]</c> "rightward" to make room 
		/// for inserted elements that will be initialized by the caller.</summary>
		public static void InsertRangeHelper<T>(IList<T> list, int index, int spaceNeeded)
		{
			int c = list.Count;
			list.Resize(c + spaceNeeded);
 			for (int i = c - 1; i >= index; i--)
				list[i + spaceNeeded] = list[i];
		}

		public static ReversedList<T> ReverseView<T>(this IList<T> list)
		{
			return new ReversedList<T>(list);
		}
		public static ReversedList<T> ReverseView<T>(this IListEx<T> list) // exists to avoid an ambiguity error for IListEx<T>
		{
			return new ReversedList<T>(list);
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
		public static int IndexOf<T>(this IEnumerable<T> list, T item, EqualityComparer<T> comp)
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
	}
}
