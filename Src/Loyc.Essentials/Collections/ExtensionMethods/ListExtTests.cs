using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;

namespace Loyc.Collections
{
	[TestFixture]
	public class ListExtTests
	{
		const int FuzzTrials = 200, MaxListSize = 200, KeyRange = 150;
		Random _r = new Random();

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
					list.FindLowestKStable(k);
				else
					list.FindLowestK(k);
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
