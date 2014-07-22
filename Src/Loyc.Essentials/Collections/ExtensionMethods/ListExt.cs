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
	/// <see cref="IList{T}"/>, <see cref="IReadOnlyList{T}"/>, arrays, 
	/// <see cref="IListSource{T}"/>, and for related mutable interfaces such as 
	/// <see cref="IArray{T}"/>. 
	/// </summary>
	/// <remarks>
	/// Extension methods that only apply to Loyc's new interfaces, or adapt a 
	/// list to those interfaces, will go in <see cref="LCExt"/> instead.
	/// <para/>
	/// The source code for adapter extension methods such as the Slice() method for 
	/// arrays, which returns an <see cref="ArraySlice{T}"/> adapter, is now 
	/// placed in the source file for each adapter class (e.g. ArraySlice.cs)
	/// to make it easier to create custom versions of Loyc.Essentials with parts 
	/// removed.
	/// </remarks>
	public static partial class ListExt
	{
		public static int BinarySearch<T>(this IList<T> list, T value) where T : IComparable<T>
		{
			return BinarySearch<T, T>(list, value, G.ToComparisonFunc<T>());
		}
		public static int BinarySearch<T>(this IList<T> list, T value, IComparer<T> pred)
		{
			return BinarySearch<T, T>(list, value, G.ToComparisonFunc(pred));
		}
		public static int BinarySearch<T, K>(this IList<T> list, K find, Func<T, K, int> compare)
		{
			int low = 0, high = list.Count - 1;
			int invert = -1;

			while (low <= high)
			{
				int mid = low + ((high - low) >> 1);
				int c = compare(list[mid], find);
				if (c < 0)
					low = mid + 1;
				else {
					high = mid - 1;
					if (c == 0)
						invert = 0;
				}
			}
			return low ^ invert;
		}

		public static int BinarySearch<T>(this IReadOnlyList<T> list, T value) where T : IComparable<T>
		{
			return BinarySearch<T,T>(list, value, G.ToComparisonFunc<T>());
		}
		public static int BinarySearch<T>(this IReadOnlyList<T> list, T value, IComparer<T> comparer)
		{
			return BinarySearch<T,T>(list, value, G.ToComparisonFunc(comparer));
		}
		public static int BinarySearch<T, K>(this IReadOnlyList<T> list, K find, Func<T, K, int> compare)
		{
			int low = 0, high = list.Count - 1;
			int invert = -1;

			while (low <= high)
			{
				int mid = low + ((high - low) >> 1);
				int c = compare(list[mid], find);
				if (c < 0)
					low = mid + 1;
				else {
					high = mid - 1;
					if (c == 0)
						invert = 0;
				}
			}
			return low ^ invert;
		}

		public static int BinarySearch<T>(this IListAndListSource<T> list, T value) where T : IComparable<T>
		{
			return BinarySearch<T,T>((IList<T>)list, value, G.ToComparisonFunc<T>());
		}
		public static int BinarySearch<T>(this IListAndListSource<T> list, T value, IComparer<T> comparer)
		{
			return BinarySearch<T,T>((IList<T>)list, value, G.ToComparisonFunc(comparer));
		}
		public static int BinarySearch<T, K>(this IListAndListSource<T> list, K value, Func<T,K,int> compare)
		{
			return BinarySearch<T,K>((IList<T>)list, value, compare);
		}

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
		public static T TryGet<T>(this IListAndListSource<T> list, int index, T defaultValue)
		{
			return ((IList<T>)list).TryGet(index, defaultValue);
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
		public static void MaybeEnlarge<T>(this List<T> list, int minSize)
		{
			int dif = minSize - list.Count;
			while (dif-- > 0)
				list.Add(default(T));
		}
		public static void MaybeEnlarge<T>(this IList<T> list, int minSize)
		{
			int dif = minSize - list.Count;
			while (dif-- > 0)
				list.Add(default(T));
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

		/// <inheritdoc cref="Sort{T}(IList{T}, int, int, Comparison{T})"/>
		public static void Sort<T>(this IList<T> list)
		{
			Sort(list, Comparer<T>.Default.Compare);
		}
		/// <inheritdoc cref="Sort{T}(IList{T}, int, int, Comparison{T})"/>
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

		/// <summary>Uses a partial quicksort, known as "quickselect", to find and
		/// sort the lowest k elements in a list.</summary>
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
		public static ListSlice<T> SortLowestK<T>(this IList<T> list, int k)
		{
			return SortLowestK(list, k, Comparer<T>.Default.Compare);
		}
		public static ListSlice<T> SortLowestK<T>(this IList<T> list, int k, Comparison<T> comp)
		{
			return SortLowestK(list, 0, list.Count, k, comp);
		}
		public static ListSlice<T> SortLowestK<T>(this IList<T> list, int index, int count, int k, Comparison<T> comp)
		{
			Sort(list, index, count, comp, null, k);
			return new ListSlice<T>(list, index, System.Math.Min(count, k));
		}

		/// <summary>A stable version of <see cref="SortLowestK{T}(IList{T},int)"/>. This means 
		/// that when k>1 and adjacent results at the beginning of <c>list</c> 
		/// compare equal, they keep the same order that they had originally.</summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list">A list that will be partially sorted.</param>
		/// <param name="k">Number of elements that will be sorted at the beginning 
		/// of the list when this method returns. If <c>k > list.Count</c>, the 
		/// entire list is sorted.</param>
		/// <returns>This method uses the quickselect algorithm and stability is
		/// achieved using a temporary array of <c>list.Count</c> integers.</returns>
		public static ListSlice<T> SortLowestKStable<T>(this IList<T> list, int k)
		{
			return SortLowestKStable(list, k, Comparer<T>.Default.Compare);
		}
		public static ListSlice<T> SortLowestKStable<T>(this IList<T> list, int k, Comparison<T> comp)
		{
			Sort(list, 0, list.Count, comp, RangeArray(list.Count), k);
			return new ListSlice<T>(list, 0, k);
		}

		private static void Sort<T>(this IList<T> list, int index, int count, Comparison<T> comp, 
		                            int[] indexes, int quickSelectElems = int.MaxValue)
		{
			CheckParam.IsInRange("index", index, 0, list.Count);
			CheckParam.IsInRange("count", count, 0, list.Count - index);
			SortCore(list, index, count, comp, indexes, quickSelectElems);
		}

		// Used by Sort, StableSort, SortLowestK, SortLowestKStable.
		private static void SortCore<T>(this IList<T> list, int index, int count, Comparison<T> comp, 
		                                int[] indexes, int quickSelectElems)
		{
			// This code duplicates the code in InternalList.Sort(), except
			// that it also supports stable sorting (indexes parameter) and
			// quickselect (sorting the first 'quickSelectElems' elements). This 
			// version is slower; two versions exist so that array sorting can 
			// be done faster.
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
					SortCore(list, index, leftSize, comp, indexes, quickSelectElems);
					index += leftSize + 1;
					count = rightSize;
					if ((quickSelectElems -= leftSize + 1) <= 0)
						break;
				}
				else
				{	// Iteratively sort the left partition; recursively sort the right
					count = leftSize;
					SortCore(list, index + leftSize + 1, rightSize, comp, indexes, quickSelectElems - (leftSize + 1));
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

		/// <summary>Gets the highest index at which a condition is true, or -1 if nowhere.</summary>
		public static int LastIndexWhere<T>(this IList<T> list, Func<T, bool> pred)
		{
			return LCInterfaces.LastIndexWhere(list.AsListSource(), pred);
		}
		/// <summary>Gets the highest index at which a condition is true, or -1 if nowhere.</summary>
		public static int LastIndexWhere<T>(this IListAndListSource<T> list, Func<T, bool> pred)
		{
			return LCInterfaces.LastIndexWhere(list, pred);
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
		/// <summary>Quickly makes a copy of a list, as an array, in random order.</summary>
		public static T[] Randomized<T>(this IListSource<T> list)
		{
			T[] copy = new T[list.Count];
			list.CopyTo(copy, 0);
			Randomize(copy);
			return copy;
		}
		/// <summary>Quickly makes a copy of a list, as an array, in random order.</summary>
		public static T[] Randomized<T>(this IListAndListSource<T> list)
		{
			return ((IList<T>)list).Randomized();
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

		/// <summary>Maps a list to an array of the same length.</summary>
		public static R[] SelectArray<T, R>(this IListAndListSource<T> input, Func<T,R> selector)
		{
			return SelectArray((IListSource<T>)input, selector);
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

		public static void ReverseInPlace<T>(this IList<T> list) 
		{
			int c = list.Count;
			for (int i = 0; i < (c >> 1); i++)
				list.Swap(i, c - i);
		}
		public static void ReverseInPlace<T>(this IArray<T> list) 
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

		public static int InsertRange<T>(this IList<T> list, int index, IReadOnlyCollection<T> source)
		{
			return InsertRange(list, index, source.Count, source);
		}
		public static int InsertRange<T>(this IList<T> list, int index, ICollection<T> source)
		{
			return InsertRange(list, index, source.Count, source);
		}
		public static int InsertRange<T>(this IList<T> list, int index, IListAndListSource<T> source)
		{
			return InsertRange(list, index, ((ICollection<T>)source).Count, source);
		}
		public static int InsertRange<T>(this IList<T> list, int index, ICollectionAndReadOnly<T> source)
		{
			return InsertRange(list, index, ((ICollection<T>)source).Count, source);
		}
		public static int InsertRange<T>(this IList<T> list, int index, int count, IEnumerable<T> source)
		{
			InsertRangeHelper(list, index, count);
			var e = source.GetEnumerator();
			for (int i = 0; i < count; i++) {
				if (!e.MoveNext())
					throw new InvalidStateException(Localize.From("InsertRange: source enumerator ended early ({0}/{1})", i, count));
				list[index++] = e.Current;
			}
			return count;
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

		public static bool SequenceEqual<TSource>(this IList<TSource> first, IList<TSource> second)
		{
			return first.Count == second.Count && Enumerable.SequenceEqual(first, second);
		}
		public static bool SequenceEqual<TSource>(this IReadOnlyCollection<TSource> first, IReadOnlyCollection<TSource> second)
		{
			return first.Count == second.Count && Enumerable.SequenceEqual(first, second);
		}
		public static bool SequenceEqual<TSource>(this IListAndListSource<TSource> first, IListAndListSource<TSource> second)
		{
			return ((IList<TSource>)first).Count == ((IList<TSource>)second).Count && Enumerable.SequenceEqual(first, second);
		}
	}
}
