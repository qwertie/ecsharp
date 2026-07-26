using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;
using Loyc.Collections.MutableListExtensionMethods;
using Loyc.Collections;

namespace Loyc.Essentials.Tests
{
	/// <summary>Unit tests for <see cref="ListExt"/>.</summary>
	[TestFixture]
	public class ListExtTests : Loyc.Collections.Impl.TestHelpers
	{
		const int FuzzTrials = 200, MaxListSize = 200, KeyRange = 150;
		Random _r = new Random();

		[Test] public void TestBinarySearch()
		{
			IList<int> list = new int[] { };
			Assert.AreEqual(~0, IListExt.BinarySearch(list, 15));
			Assert.AreEqual(~0, IListExt.BinarySearch(list, -15));
			list = new int[] { 5 };
			Assert.AreEqual(0, IListExt.BinarySearch(list, 5));
			Assert.AreEqual(~0, IListExt.BinarySearch(list, 0));
			Assert.AreEqual(~1, IListExt.BinarySearch(list, 10));
			list = new int[] { 5, 7 };
			Assert.AreEqual(~0, IListExt.BinarySearch(list, 0));
			Assert.AreEqual( 0, IListExt.BinarySearch(list, 5));
			Assert.AreEqual(~1, IListExt.BinarySearch(list, 6));
			Assert.AreEqual( 1, IListExt.BinarySearch(list, 7));
			Assert.AreEqual(~2, IListExt.BinarySearch(list, 10));
			list = new int[] { 1, 5, 7, 13, 17, 29, 29, 31 };
			Assert.AreEqual(~0, IListExt.BinarySearch(list, -1));
			Assert.AreEqual( 0, IListExt.BinarySearch(list, 1));
			Assert.AreEqual(~1, IListExt.BinarySearch(list, 2));
			Assert.AreEqual( 1, IListExt.BinarySearch(list, 5));
			Assert.AreEqual(~2, IListExt.BinarySearch(list, 6));
			Assert.AreEqual( 2, IListExt.BinarySearch(list, 7));
			Assert.AreEqual(~3, IListExt.BinarySearch(list, 10));
			Assert.AreEqual( 3, IListExt.BinarySearch(list, 13));
			Assert.AreEqual(~4, IListExt.BinarySearch(list, 16));
			Assert.AreEqual( 4, IListExt.BinarySearch(list, 17));
			Assert.AreEqual(~5, IListExt.BinarySearch(list, 28));
			int i = IListExt.BinarySearch(list, 29);
			Assert.IsTrue(i == 5 || i == 6);
			Assert.AreEqual(~7, IListExt.BinarySearch(list, 30));
			Assert.AreEqual( 7, IListExt.BinarySearch(list, 31));
			Assert.AreEqual(~8, IListExt.BinarySearch(list, 1000));
		}
		[Test] public void TestPredicatedBinarySearch()
		{
			Comparison<int> p = G.ToComparison<int>();
			IList<int> list = new int[] { };
			Assert.AreEqual(~0, IListExt.BinarySearch(list, 15, p));
			Assert.AreEqual(~0, IListExt.BinarySearch(list, -15, p));
			list = new int[] { 5 };
			Assert.AreEqual(0, IListExt.BinarySearch(list, 5, p));
			Assert.AreEqual(~0, IListExt.BinarySearch(list, 0, p));
			Assert.AreEqual(~1, IListExt.BinarySearch(list, 10, p));
			list = new int[] { 5, 7 };
			Assert.AreEqual(~0, IListExt.BinarySearch(list, 0, p));
			Assert.AreEqual( 0, IListExt.BinarySearch(list, 5, p));
			Assert.AreEqual(~1, IListExt.BinarySearch(list, 6, p));
			Assert.AreEqual( 1, IListExt.BinarySearch(list, 7, p));
			Assert.AreEqual(~2, IListExt.BinarySearch(list, 10, p));
			list = new int[] { 1, 5, 7, 13, 17, 29, 29, 31 };
			Assert.AreEqual(~0, IListExt.BinarySearch(list, -1, p));
			Assert.AreEqual( 0, IListExt.BinarySearch(list, 1, p));
			Assert.AreEqual(~1, IListExt.BinarySearch(list, 2, p));
			Assert.AreEqual( 1, IListExt.BinarySearch(list, 5, p));
			Assert.AreEqual(~2, IListExt.BinarySearch(list, 6, p));
			Assert.AreEqual( 2, IListExt.BinarySearch(list, 7, p));
			Assert.AreEqual(~3, IListExt.BinarySearch(list, 10, p));
			Assert.AreEqual( 3, IListExt.BinarySearch(list, 13, p));
			Assert.AreEqual(~4, IListExt.BinarySearch(list, 16, p));
			Assert.AreEqual( 4, IListExt.BinarySearch(list, 17, p));
			Assert.AreEqual(~5, IListExt.BinarySearch(list, 28, p));
			int i = IListExt.BinarySearch(list, 29, p);
			Assert.IsTrue(i == 5 || i == 6);
			Assert.AreEqual(~7, IListExt.BinarySearch(list, 30, p));
			Assert.AreEqual( 7, IListExt.BinarySearch(list, 31, p));
			Assert.AreEqual(~8, IListExt.BinarySearch(list, 1000, p));
			
			// This tests another code path in G.ToComparison<T>()
			var p2 = G.ToComparisonFunc<string>();
			IList<string> strs = new string[] {"1", "3", "5", "7", "9"};
			Assert.AreEqual(1, IListExt.BinarySearch2(strs, "3", p2));
			Assert.AreEqual(~4, IListExt.BinarySearch2(strs, "7b", p2));
		}

		struct IntPair : IComparable<IntPair>
		{
			public int Key, Value;
			public int CompareTo(IntPair other) { return Key.CompareTo(other.Key); }
			public override string ToString() { return Key + ":" + Value; }
		}

		[Test]
		public void StableSortFuzzTest()
		{
			var list = new List<IntPair>(100);
			for (int t = 0; t < FuzzTrials; t++) {
				MakeRandomList(list);
				var TEMP = new List<IntPair>(list);
				list.StableSort();
				for (int i = 1; i < list.Count; i++) {
					IntPair a = list[i-1], b = list[i];
					Assert.LessOrEqual(a.Key, b.Key);
					if (a.Key == b.Key)
						Assert.Less(a.Value, b.Value);
				}
			}
		}

		void MakeRandomList(List<IntPair> list)
		{
			list.Clear();
			list.AddRange(Enumerable.Range(0, _r.Next(MaxListSize)).Select(i => 
				new IntPair { Key = _r.Next(KeyRange), Value = i }));
		}

		[Test]
		public void SelectionFuzzTest()
		{
			var list = new List<IntPair>(100);
			
			var numSortedAfter = new Dictionary<int, int>(); // histogram
			
			for (int t = 0; t < FuzzTrials; t++)
			{
				MakeRandomList(list);
				if (list.Count < 2)
					continue;

				int k = _r.Next(1, list.Count);
				if ((t & 1) == 1)
					list.SortLowestKStable(k);
				else
					list.SortLowestK(k);
				int i;
				for (i = 1; i <= k; i++) {
					IntPair a = list[i-1], b = list[i];
					Assert.LessOrEqual(a.Key, b.Key);
					if ((t & 1) == 1 && a.Key == b.Key)
						Assert.Less(a.Value, b.Value);
				}
				
				// Ensure that everything afterward is greater
				for (; i < list.Count; i++)
					Assert.LessOrEqual(list[k - 1].Key, list[i].Key);
				
				// Also, measure how much of the list is sorted after list[k].
				// It is common that after k there is some sorting (especially
				// because ListExt.Sort() has an insertion sort mode for small
				// sublists, which ignores k), I just want to make sure that
				// excessive sorting is not too common by using the debugger
				// to look at the histogram.
				for (i = k + 1; i < list.Count; i++) {
					IntPair a = list[i - 1], b = list[i];
					if (a.Key > b.Key) break;
				}
				numSortedAfter[i - k] = numSortedAfter.TryGetValue(i - k, 0) + 1;
			}
			Assert.That(numSortedAfter.ContainsKey(1));
		}

		[Test]
		public void TestReverseInPlace()
		{
			// Regression: the swap partner was `c - i` instead of `c - 1 - i`, so this
			// threw ArgumentOutOfRangeException for EVERY list of length >= 2.
			for (int n = 0; n <= 8; n++) {
				var list = new List<int>(Enumerable.Range(0, n));
				list.ReverseInPlace();
				ExpectList(list, Enumerable.Range(0, n).Reverse().ToArray());
			}
			// The IArray<T> overload must behave identically (ListSlice<T> is an IArray)
			for (int n = 0; n <= 8; n++) {
				var backing = new List<int>(Enumerable.Range(0, n));
				IArray<int> slice = new ListSlice<int>(backing);
				slice.ReverseInPlace();
				ExpectList(backing, Enumerable.Range(0, n).Reverse().ToArray());
			}
		}

		[Test]
		public void TestAdjacentPairsCircular()
		{
			// Regression: the IEnumerable overload delegated to AdjacentPairs, dropping
			// the wrap-around pair that is this method's entire reason to exist.
			var pairs = new[] { 1, 2, 3, 4 }.AdjacentPairsCircular().ToList();
			ExpectList(pairs, Pair.Create(1, 2), Pair.Create(2, 3), Pair.Create(3, 4), Pair.Create(4, 1));
			// Degenerate cases
			ExpectList(new int[0].AdjacentPairsCircular().ToList());
			ExpectList(new[] { 7 }.AdjacentPairsCircular().ToList(), Pair.Create(7, 7));
			// Plain AdjacentPairs must NOT wrap
			ExpectList(new[] { 1, 2, 3 }.AdjacentPairs().ToList(), Pair.Create(1, 2), Pair.Create(2, 3));
		}

		[Test]
		public void TestIndexOfMinMaxSkipsLeadingNulls()
		{
			// Regression: the null-skipping loop advanced `i` but never updated `min_i`,
			// so the index of a LEADING NULL was returned instead of the real min/max.
			var withLeadingNull = new string?[] { null, "b", "a", "c" };
			Assert.AreEqual(2, withLeadingNull.IndexOfMin());  // "a"
			Assert.AreEqual(3, withLeadingNull.IndexOfMax());  // "c"
			var twoNulls = new string?[] { null, null, "z", "y" };
			Assert.AreEqual(3, twoNulls.IndexOfMin());         // "y"
			Assert.AreEqual(2, twoNulls.IndexOfMax());         // "z"
			// No nulls: unchanged behaviour
			Assert.AreEqual(1, new[] { "b", "a", "c" }.IndexOfMin());
			// All null / empty
			Assert.AreEqual(-1, new string?[] { null, null }.IndexOfMin());
			Assert.AreEqual(-1, new string?[0].IndexOfMin());
		}

		[Test]
		public void TestConcatNow()
		{
			// Regression: the IReadOnlyList overload never incremented a_i, so it
			// returned the first element of each list repeated.
			IReadOnlyList<int> a = new[] { 1, 2, 3 };
			IReadOnlyList<int> b = new[] { 4, 5 };
			ExpectList(a.ConcatNow(b), 1, 2, 3, 4, 5);
			ExpectList(a.ConcatNow(new int[0]), 1, 2, 3);
			ExpectList(((IReadOnlyList<int>)new int[0]).ConcatNow(b), 4, 5);
			ExpectList(((IReadOnlyList<int>)new int[0]).ConcatNow(new int[0]));
			// Must agree with the T[] overload
			ExpectList(a.ConcatNow(b), new[] { 1, 2, 3 }.ConcatNow(new[] { 4, 5 }));
		}
	}
}
