using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;

namespace Loyc.Collections
{
	/// <summary>Unit tests for <see cref="ListExt"/>.</summary>
	[TestFixture]
	public class ListExtTests
	{
		const int FuzzTrials = 200, MaxListSize = 200, KeyRange = 150;
		Random _r = new Random();

		[Test] public void TestBinarySearch()
		{
			IList<int> list = new int[] { };
			Assert.AreEqual(~0, ListExt.BinarySearch(list, 15));
			Assert.AreEqual(~0, ListExt.BinarySearch(list, -15));
			list = new int[] { 5 };
			Assert.AreEqual(0, ListExt.BinarySearch(list, 5));
			Assert.AreEqual(~0, ListExt.BinarySearch(list, 0));
			Assert.AreEqual(~1, ListExt.BinarySearch(list, 10));
			list = new int[] { 5, 7 };
			Assert.AreEqual(~0, ListExt.BinarySearch(list, 0));
			Assert.AreEqual( 0, ListExt.BinarySearch(list, 5));
			Assert.AreEqual(~1, ListExt.BinarySearch(list, 6));
			Assert.AreEqual( 1, ListExt.BinarySearch(list, 7));
			Assert.AreEqual(~2, ListExt.BinarySearch(list, 10));
			list = new int[] { 1, 5, 7, 13, 17, 29, 29, 31 };
			Assert.AreEqual(~0, ListExt.BinarySearch(list, -1));
			Assert.AreEqual( 0, ListExt.BinarySearch(list, 1));
			Assert.AreEqual(~1, ListExt.BinarySearch(list, 2));
			Assert.AreEqual( 1, ListExt.BinarySearch(list, 5));
			Assert.AreEqual(~2, ListExt.BinarySearch(list, 6));
			Assert.AreEqual( 2, ListExt.BinarySearch(list, 7));
			Assert.AreEqual(~3, ListExt.BinarySearch(list, 10));
			Assert.AreEqual( 3, ListExt.BinarySearch(list, 13));
			Assert.AreEqual(~4, ListExt.BinarySearch(list, 16));
			Assert.AreEqual( 4, ListExt.BinarySearch(list, 17));
			Assert.AreEqual(~5, ListExt.BinarySearch(list, 28));
			int i = ListExt.BinarySearch(list, 29);
			Assert.IsTrue(i == 5 || i == 6);
			Assert.AreEqual(~7, ListExt.BinarySearch(list, 30));
			Assert.AreEqual( 7, ListExt.BinarySearch(list, 31));
			Assert.AreEqual(~8, ListExt.BinarySearch(list, 1000));
		}
		[Test] public void TestPredicatedBinarySearch()
		{
			Comparison<int> p = G.ToComparison<int>();
			IList<int> list = new int[] { };
			Assert.AreEqual(~0, ListExt.BinarySearch(list, 15, p));
			Assert.AreEqual(~0, ListExt.BinarySearch(list, -15, p));
			list = new int[] { 5 };
			Assert.AreEqual(0, ListExt.BinarySearch(list, 5, p));
			Assert.AreEqual(~0, ListExt.BinarySearch(list, 0, p));
			Assert.AreEqual(~1, ListExt.BinarySearch(list, 10, p));
			list = new int[] { 5, 7 };
			Assert.AreEqual(~0, ListExt.BinarySearch(list, 0, p));
			Assert.AreEqual( 0, ListExt.BinarySearch(list, 5, p));
			Assert.AreEqual(~1, ListExt.BinarySearch(list, 6, p));
			Assert.AreEqual( 1, ListExt.BinarySearch(list, 7, p));
			Assert.AreEqual(~2, ListExt.BinarySearch(list, 10, p));
			list = new int[] { 1, 5, 7, 13, 17, 29, 29, 31 };
			Assert.AreEqual(~0, ListExt.BinarySearch(list, -1, p));
			Assert.AreEqual( 0, ListExt.BinarySearch(list, 1, p));
			Assert.AreEqual(~1, ListExt.BinarySearch(list, 2, p));
			Assert.AreEqual( 1, ListExt.BinarySearch(list, 5, p));
			Assert.AreEqual(~2, ListExt.BinarySearch(list, 6, p));
			Assert.AreEqual( 2, ListExt.BinarySearch(list, 7, p));
			Assert.AreEqual(~3, ListExt.BinarySearch(list, 10, p));
			Assert.AreEqual( 3, ListExt.BinarySearch(list, 13, p));
			Assert.AreEqual(~4, ListExt.BinarySearch(list, 16, p));
			Assert.AreEqual( 4, ListExt.BinarySearch(list, 17, p));
			Assert.AreEqual(~5, ListExt.BinarySearch(list, 28, p));
			int i = ListExt.BinarySearch(list, 29, p);
			Assert.IsTrue(i == 5 || i == 6);
			Assert.AreEqual(~7, ListExt.BinarySearch(list, 30, p));
			Assert.AreEqual( 7, ListExt.BinarySearch(list, 31, p));
			Assert.AreEqual(~8, ListExt.BinarySearch(list, 1000, p));
			
			// This tests another code path in G.ToComparison<T>()
			var p2 = G.ToComparisonFunc<string>();
			IList<string> strs = new string[] {"1", "3", "5", "7", "9"};
			Assert.AreEqual(1, ListExt.BinarySearch2(strs, "3", p2));
			Assert.AreEqual(~4, ListExt.BinarySearch2(strs, "7b", p2));
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
	}
}
