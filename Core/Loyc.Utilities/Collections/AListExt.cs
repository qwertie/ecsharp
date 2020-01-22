using Loyc.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Collections
{
	public static class AListExt
	{
		public static AListSumTracker<K, double> SumTracker<K>(this AListBase<K, double> list) => SumTracker(list, n => n);
		public static AListSumTracker<K, T> SumTracker<K, T>(this AListBase<K, T> list, Func<T, double> selector) => new AListSumTracker<K, T>(selector, list);
		public static AListSumTracker<T> SumTracker<T>(this AListBase<int, T> list, Func<T, double> selector) => new AListSumTracker<T>(selector, list);
		public static AListStatisticTracker<K, T, TStatistic> StatisticTracker<K, T, TStatistic>(this AListBase<K, T> list, 
			Func<T, TStatistic> selector, Func<TStatistic[], TStatistic> aggregator, TStatistic emptyResult)
			=> new AListStatisticTracker<K, T, TStatistic>(selector, aggregator, emptyResult, list);
		public static AListStatisticTracker<K, T, TStatistic> StatisticTracker<K, T, TStatistic>(this AListBase<K, T> list, 
			Func<T, TStatistic> selector, Func<TStatistic, TStatistic, TStatistic> aggregator, TStatistic emptyResult)
			=> new AListStatisticTracker<K, T, TStatistic>(selector, aggregator, emptyResult, list);
		public static AListStatisticTracker<K, T> StatisticTracker<K, T>(this AListBase<K, T> list, Func<T, double> selector)
			=> new AListStatisticTracker<K, T>(selector, list);
		public static AListStatisticTracker<T> StatisticTracker<T>(this AListBase<int, T> list, Func<T, double> selector)
			=> new AListStatisticTracker<T>(selector, list);
	}
}
