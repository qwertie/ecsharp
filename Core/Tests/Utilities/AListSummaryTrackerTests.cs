using System;
using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.MiniTest;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Collections.Tests
{
	public class AListSummaryTrackerTests<AList, T> : AListTestBase<AList, T> where AList : AListBase<int, T>, ICloneable<AList>
	{
		Random _r;
		Func<T,double> _selector;

		public AListSummaryTrackerTests(AListTestHelpersBase<AList, T> helpers, Func<T,double> selector, int randomSeed) : base(helpers) 
		{
			_selector = selector;
			_r = new Random(randomSeed);
		}
		
		[Test]
		public void BasicSumTrackerTest()
		{
			AList alist = NewList();
			AListSumTracker<T> tracker = alist.SumTracker(_selector);

			double sum = 0;
			for (int i = 0; i < 250; i++) {
				Add(alist, i, _r.Next(alist.Count));
				sum += i;
				Assert.AreEqual(sum, tracker.GetSummary());
				Assert.AreEqual(sum / (double)alist.Count, tracker.Average);
			}
		}

		[Test]
		public void BasicStatisticTrackerTest()
		{
			AList alist = NewList();
			AListStatisticTracker<T> tracker = alist.StatisticTracker(_selector);

			double sum = 0, sumOfSquares = 0;
			for (int i = 0; i < 250; i++) {
				Add(alist, i, _r.Next(alist.Count));
				sum += i;
				sumOfSquares += i*i;
				Assert.AreEqual(sum, tracker.Statistic.SumTotal);
				Assert.AreEqual(sumOfSquares, tracker.Statistic.SumOfSquares);
			}
		}

		[Test]
		public void CanTrackMultipleLists()
		{
			AList alist1 = NewList(), alist2 = NewList();
			
			AListStatisticTracker<T> tracker = alist1.StatisticTracker(_selector);
			alist2.AddObserver(tracker);

			double sum = 0, sumOfSquares = 0;
			for (int i = 0; i < 250; i++) {
				var alist = (i & 1) == 0 ? alist1 : alist2;
				Add(alist, i, _r.Next(alist.Count));
				sum += i;
				sumOfSquares += i*i;
				Assert.AreEqual(sum, tracker.Statistic.SumTotal);
				Assert.AreEqual(sumOfSquares, tracker.Statistic.SumOfSquares);
			}
		}
	}

	public class AListSummaryTrackerTests : AListSummaryTrackerTests<AList<int>, int>
	{
		public AListSummaryTrackerTests(int randomSeed)
			: base(new AListTestHelpers(6, 6), i => i, randomSeed) { }
	}
	public class BDictionarySummaryTrackerTests : AListSummaryTrackerTests<BDictionary<int, int>, KeyValuePair<int, int>>
	{
		public BDictionarySummaryTrackerTests(int randomSeed)
			: base(new BDictionaryTestHelpers(6, 6), p => p.Key, randomSeed) { }
	}
}
