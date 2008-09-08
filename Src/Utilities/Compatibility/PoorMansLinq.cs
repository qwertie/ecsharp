using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Compatibility.Linq
{
	/// <summary>A structure that provides Linq functionality in C# 2.0</summary>
	/// <typeparam name="T">Type of elements in the sequence to be processed</typeparam>
	/// <remarks>Please note that if you have access to C# 3.0, you do not need to
	/// use this class. Even if you are using .NET Framework 2.0, C# still allows
	/// you to use LINQ syntax; just add a reference to LinqBridge.dll in your
	/// project (LinqBridge provides an implementation of LINQ to Objects for the
	/// .NET framework 2.0. It is included in the Loyc distribution.)
	/// <para/>
	/// PoorMansLinq(of T) does not include all functionality of the static class
	/// Enumerable. It cannot not include specializations for specific kinds of T,
	/// such as Average&lt;double&gt;(). Nor does it include static methods such as
	/// Empty() and Range(first, last) that are not extension methods. And it
	/// doesn't include AsEnumerable(), which makes no sense without the extension
	/// methods feature.
	/// </remarks><example>
	/// <code>
	/// class Program
	/// {
	///     public static void Main(string[] args)
	///     {
	///         string[] words = new string[] {
	///             "Pies", "Are", "Good", "In", "Lovely", "Apples" };
	///         // Pies Are Good
	///         Console.WriteLine(string.Join(" ", Linq(words).Take(3).ToArray()));
	///         // Apples Are Good In Lovely Pies
	///         Console.WriteLine(string.Join(" ", Linq(words).Sorted().ToArray()));
	///
	///         int[] numbers = new int[] { 4, 95, 309, 357, 233, 2 };
	///         // 1000
	///         Console.WriteLine(Enumerable.Sum(numbers));
	///         // 666
	///         Console.WriteLine(Enumerable.Sum(Linq(numbers)
	///             .Where(delegate(int x) { return x &gt; 300; })));
	///     }
	///     static PoorMansLinq&lt;T&gt; Linq&lt;T&gt;(IEnumerable&lt;T&gt; source)
	///     {
	///         return new PoorMansLinq&lt;T&gt;(source);
	///     }
	/// }
	/// </code>
	/// </example>
	public struct PoorMansLinq<T> : IEnumerable<T>
	{
		private IEnumerable<T> _source;

		public PoorMansLinq(IEnumerable<T> source) { _source = source; }

		#region Aggregate
		public T Aggregate(Func<T, T, T> func)
			{ return Enumerable.Aggregate(_source, func); }
		public TAccumulate Aggregate<TAccumulate>(TAccumulate seed, Func<TAccumulate, T, TAccumulate> func)
			{ return Enumerable.Aggregate<T, TAccumulate>(_source, seed, func); }
		public TResult Aggregate<TAccumulate, TResult>(TAccumulate seed, Func<TAccumulate, T, TAccumulate> func, Func<TAccumulate, TResult> resultSelector)
			{ return Enumerable.Aggregate<T, TAccumulate, TResult>(_source, seed, func, resultSelector); }
		#endregion

		#region All
		public bool All(Func<T, bool> predicate)
			{ return Enumerable.All<T>(_source, predicate); }
		#endregion

		#region Any
		public bool Any()
			{ return Enumerable.Any<T>(_source); }
		public bool Any(Func<T, bool> predicate)
			{ return Enumerable.Any<T>(_source); }
		#endregion

		#region Average
		/*
		// The following overloads cannot be supported here. Call Enumerable.Average() instead.
		public double Average(this IEnumerable<int> source)
		public double? Average(this IEnumerable<int?> source)
		public double Average(this IEnumerable<long> source)
		public double? Average(this IEnumerable<long?> source)
		public double Average(this IEnumerable<double> source)
		public double? Average(this IEnumerable<double?> source)
		public decimal Average(this IEnumerable<decimal> source)
		public decimal? Average(this IEnumerable<decimal?> source)
		*/
		public double Average(Func<T, int> selector)
			{ return Enumerable.Average<T>(_source, selector); }
		public double? Average(Func<T, int?> selector)
			{ return Enumerable.Average<T>(_source, selector); }
		public double Average(Func<T, long> selector)
			{ return Enumerable.Average<T>(_source, selector); }
		public double? Average(Func<T, long?> selector)
			{ return Enumerable.Average<T>(_source, selector); }
		public double Average(Func<T, double> selector)
			{ return Enumerable.Average<T>(_source, selector); }
		public double? Average(Func<T, double?> selector)
			{ return Enumerable.Average<T>(_source, selector); }
		public decimal Average(Func<T, decimal> selector)
			{ return Enumerable.Average<T>(_source, selector); }
		public decimal? Average(Func<T, decimal?> selector)
			{ return Enumerable.Average<T>(_source, selector); }
		#endregion

		#region Concat
		public PoorMansLinq<T> Concat(IEnumerable<T> second)
			{ return new PoorMansLinq<T>(Enumerable.Concat(_source, second)); }
		#endregion

		#region Contains
		public bool Contains(T value)
			{ return Enumerable.Contains<T>(_source, value); }
		public bool Contains(T value, IEqualityComparer<T> comparer)
			{ return Enumerable.Contains<T>(_source, value, comparer); }
		#endregion

		#region Count
		public int Count()
			{ return Enumerable.Count<T>(_source); }
		public int Count(Func<T, bool> selector)
			{ return Enumerable.Count<T>(_source, selector); }
		#endregion

		#region DefaultIfEmpty
		public PoorMansLinq<T> DefaultIfEmpty()
			{ return new PoorMansLinq<T>(Enumerable.DefaultIfEmpty(_source)); }
		public PoorMansLinq<T> DefaultIfEmpty(T defaultValue)
			{ return new PoorMansLinq<T>(Enumerable.DefaultIfEmpty(_source, defaultValue)); }
		#endregion

		#region Distinct
		public PoorMansLinq<T> Distinct()
			{ return new PoorMansLinq<T>(Enumerable.Distinct<T>(_source)); }
		public PoorMansLinq<T> Distinct(IEqualityComparer<T> comparer)
			{ return new PoorMansLinq<T>(Enumerable.Distinct<T>(_source, comparer)); }
		#endregion

		#region ElementAt
		public T ElementAt(int index)
			{ return Enumerable.ElementAt<T>(_source, index); }
		#endregion

		#region ElementAtOrDefault
		public T ElementAtOrDefault(int index)
			{ return Enumerable.ElementAtOrDefault<T>(_source, index); }
		#endregion

		#region Except
		public PoorMansLinq<T> Except(IEnumerable<T> second)
			{ return new PoorMansLinq<T>(Enumerable.Except<T>(_source, second)); }
		public PoorMansLinq<T> Except(IEnumerable<T> second, IEqualityComparer<T> comparer)
			{ return new PoorMansLinq<T>(Enumerable.Except<T>(_source, second, comparer)); }
		#endregion

		#region First
		public T First()
			{ return Enumerable.First<T>(_source); }
		public T First(Func<T, bool> predicate)
			{ return Enumerable.First<T>(_source, predicate); }
		#endregion

		#region FirstOrDefault
		public T FirstOrDefault()
			{ return Enumerable.FirstOrDefault<T>(_source); }
		public T FirstOrDefault(Func<T, bool> predicate)
			{ return Enumerable.FirstOrDefault<T>(_source, predicate); }
		#endregion

		#region GroupBy
		public IEnumerable<IGrouping<TKey, T>> GroupBy<TKey>
			(Func<T, TKey> keySelector)
			{ return Enumerable.GroupBy<T, TKey>(_source, keySelector); }
		public IEnumerable<IGrouping<TKey, T>> GroupBy<TKey>
			(Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer)
			{ return Enumerable.GroupBy<T, TKey>(_source, keySelector, comparer); }
		public IEnumerable<IGrouping<TKey, TElement>> GroupBy<TKey, TElement>
			(Func<T, TKey> keySelector, Func<T, TElement> elementSelector)
			{ return Enumerable.GroupBy<T, TKey, TElement>(_source, keySelector, elementSelector); }
		public IEnumerable<IGrouping<TKey, TElement>> GroupBy<TKey, TElement>
			(Func<T, TKey> keySelector, Func<T, TElement> elementSelector, IEqualityComparer<TKey> comparer)
			{ return Enumerable.GroupBy<T, TKey, TElement>(_source, keySelector, elementSelector, comparer); }
		#endregion

		# region GroupJoin
		public IEnumerable<TResult> GroupJoin<TInner, TKey, TResult>
			(IEnumerable<TInner> inner, Func<T, TKey> outerKeySelector,
			Func<TInner, TKey> innerKeySelector, Func<T, IEnumerable<TInner>, TResult> resultSelector)
			{ return new PoorMansLinq<TResult>(Enumerable.GroupJoin<T, TInner, TKey, TResult>
				(_source, inner, outerKeySelector, innerKeySelector, resultSelector)); }
		public IEnumerable<TResult> GroupJoin<TInner, TKey, TResult>
			(IEnumerable<TInner> inner, Func<T, TKey> outerKeySelector,
			Func<TInner, TKey> innerKeySelector, Func<T, IEnumerable<TInner>, TResult> resultSelector,
			IEqualityComparer<TKey> comparer)
			{ return new PoorMansLinq<TResult>(Enumerable.GroupJoin<T, TInner, TKey, TResult>
				(_source, inner, outerKeySelector, innerKeySelector, resultSelector, comparer)); }
		#endregion

		#region Intersect
		public IEnumerable<T> Intersect(IEnumerable<T> second)
			{ return new PoorMansLinq<T>(Enumerable.Intersect<T>(_source, second)); }
		#endregion

		#region Join
		public IEnumerable<TResult> Join<TInner, TKey, TResult>
			(IEnumerable<TInner> inner, Func<T, TKey> outerKeySelector,
			Func<TInner, TKey> innerKeySelector, Func<T, TInner, TResult> resultSelector)
			{ return new PoorMansLinq<TResult>(Enumerable.Join<T, TInner, TKey, TResult>
				(_source, inner, outerKeySelector, innerKeySelector, resultSelector)); }
		public IEnumerable<TResult> Join<TInner, TKey, TResult>
			(IEnumerable<TInner> inner, Func<T, TKey> outerKeySelector,
			Func<TInner, TKey> innerKeySelector, Func<T, TInner, TResult> resultSelector, IEqualityComparer<TKey> comparer)
			{ return new PoorMansLinq<TResult>(Enumerable.Join<T, TInner, TKey, TResult>
				(_source, inner, outerKeySelector, innerKeySelector, resultSelector, comparer)); }
		#endregion

		#region Last
		public T Last()
			{ return Enumerable.Last<T>(_source); }
		public T Last(Func<T, bool> predicate)
			{ return Enumerable.Last<T>(_source, predicate); }
		#endregion

		#region LastOrDefault
		public T LastOrDefault()
			{ return Enumerable.LastOrDefault<T>(_source); }
		public T LastOrDefault(Func<T, bool> predicate)
			{ return Enumerable.LastOrDefault<T>(_source, predicate); }
		#endregion

		#region LongCount
		public long LongCount()
			{ return Enumerable.LongCount<T>(_source); }
		public long LongCount(Func<T, bool> selector)
			{ return Enumerable.LongCount<T>(_source, selector); }
		#endregion

		#region Max
		/*
		// The following overloads cannot be supported here. Call Enumerable.Max() instead.
		public int Max(this IEnumerable<int> source)
		public int? Max(this IEnumerable<int?> source)
		public long Max(this IEnumerable<long> source)
		public long? Max(this IEnumerable<long?> source)
		public double Max(this IEnumerable<double> source)
		public double? Max(this IEnumerable<double?> source)
		public decimal Max(this IEnumerable<decimal> source)
		public decimal? Max(this IEnumerable<decimal?> source)
		*/
		public T Max()
			{ return Enumerable.Max<T>(_source); }
		public int Max(Func<T, int> selector)
			{ return Enumerable.Max<T>(_source, selector); }
		public int? Max(Func<T, int?> selector)
			{ return Enumerable.Max<T>(_source, selector); }
		public long Max(Func<T, long> selector)
			{ return Enumerable.Max<T>(_source, selector); }
		public long? Max(Func<T, long?> selector)
			{ return Enumerable.Max<T>(_source, selector); }
		public double Max(Func<T, double> selector)
			{ return Enumerable.Max<T>(_source, selector); }
		public double? Max(Func<T, double?> selector)
			{ return Enumerable.Max<T>(_source, selector); }
		public decimal Max(Func<T, decimal> selector)
			{ return Enumerable.Max<T>(_source, selector); }
		public decimal? Max(Func<T, decimal?> selector)
			{ return Enumerable.Max<T>(_source, selector); }
		public TResult Max<TResult>(Func<T, TResult> selector)
			{ return Enumerable.Max<T, TResult>(_source, selector); }
		#endregion

		#region Min
		/*
		// The following overloads cannot be supported here. Call Enumerable.Min() instead.
		public int Min(this IEnumerable<int> source)
		public int? Min(this IEnumerable<int?> source)
		public long Min(this IEnumerable<long> source)
		public long? Min(this IEnumerable<long?> source)
		public double Min(this IEnumerable<double> source)
		public double? Min(this IEnumerable<double?> source)
		public decimal Min(this IEnumerable<decimal> source)
		public decimal? Min(this IEnumerable<decimal?> source)
		*/
		public T Min()
			{ return Enumerable.Min<T>(_source); }
		public int Min(Func<T, int> selector)
			{ return Enumerable.Min<T>(_source, selector); }
		public int? Min(Func<T, int?> selector)
			{ return Enumerable.Min<T>(_source, selector); }
		public long Min(Func<T, long> selector)
			{ return Enumerable.Min<T>(_source, selector); }
		public long? Min(Func<T, long?> selector)
			{ return Enumerable.Min<T>(_source, selector); }
		public double Min(Func<T, double> selector)
			{ return Enumerable.Min<T>(_source, selector); }
		public double? Min(Func<T, double?> selector)
			{ return Enumerable.Min<T>(_source, selector); }
		public decimal Min(Func<T, decimal> selector)
			{ return Enumerable.Min<T>(_source, selector); }
		public decimal? Min(Func<T, decimal?> selector)
			{ return Enumerable.Min<T>(_source, selector); }
		public TResult Min<TResult>(Func<T, TResult> selector)
			{ return Enumerable.Min<T, TResult>(_source, selector); }
		#endregion

		#region OrderBy
		public PoorMansLinq<T> OrderBy<TKey>(Func<T, TKey> keySelector)
			{ return new PoorMansLinq<T>(Enumerable.OrderBy(_source, keySelector)); }
		public PoorMansLinq<T> OrderBy<TKey>
			(Func<T, TKey> keySelector, IComparer<TKey> comparer)
			{ return new PoorMansLinq<T>(Enumerable.OrderBy(_source, keySelector, comparer)); }
		#endregion

		#region OrderByDescending
		public PoorMansLinq<T> OrderByDescending<TKey>
			(Func<T, TKey> keySelector)
			{ return new PoorMansLinq<T>(Enumerable.OrderByDescending<T, TKey>(_source, keySelector)); }
		public PoorMansLinq<T> OrderByDescending<TKey>
			(Func<T, TKey> keySelector, IComparer<TKey> comparer)
			{ return new PoorMansLinq<T>(Enumerable.OrderByDescending<T, TKey>(_source, keySelector, comparer)); }
		#endregion

		#region Reverse
		public PoorMansLinq<T> Reverse()
			{ return new PoorMansLinq<T>(Enumerable.Reverse(_source)); }
		#endregion

		#region Select
		public IEnumerable<TResult> Select<TResult>(Func<T, TResult> selector)
			{ return Enumerable.Select<T, TResult> (_source, selector); }
		public IEnumerable<TResult> Select<TResult>(Func<T, int, TResult> selector)
			{ return Enumerable.Select<T, TResult> (_source, selector); }
		#endregion

		#region SelectMany
		public IEnumerable<TResult> SelectMany<TResult>(Func<T, IEnumerable<TResult>> selector)
			{ return Enumerable.SelectMany<T, TResult>(_source, selector); }
		public IEnumerable<TResult> SelectMany<TResult>(Func<T, int, IEnumerable<TResult>> selector)
			{ return Enumerable.SelectMany<T, TResult>(_source, selector); }
		#endregion

		#region Single

		public T Single()
			{ return Enumerable.Single<T>(_source); }
		public T Single(Func<T, bool> predicate)
			{ return Enumerable.Single<T>(_source, predicate); }
		#endregion

		#region SingleOrDefault
		public T SingleOrDefault()
			{ return Enumerable.SingleOrDefault<T>(_source); }
		public T SingleOrDefault(Func<T, bool> predicate)
			{ return Enumerable.SingleOrDefault<T>(_source, predicate); }
		#endregion

		#region Skip
		public IEnumerable<T> Skip(int count)
			{ return new PoorMansLinq<T>(Enumerable.Skip<T>(_source, count)); }
		#endregion

		#region SkipWhile
		public IEnumerable<T> SkipWhile(Func<T, bool> predicate)
			{ return Enumerable.SkipWhile<T>(_source, predicate); }
		public IEnumerable<T> SkipWhile(Func<T, int, bool> predicate)
			{ return new PoorMansLinq<T>(Enumerable.SkipWhile(_source, predicate)); }
		#endregion

		#region Sum
		/*
		// The following overloads cannot be supported here. Call Enumerable.Sum() instead.
		public int Sum(this IEnumerable<int> source)
		public int? Sum(this IEnumerable<int?> source)
		public long Sum(this IEnumerable<long> source)
		public long? Sum(this IEnumerable<long?> source)
		public double Sum(this IEnumerable<double> source)
		public double? Sum(this IEnumerable<double?> source)
		public decimal Sum(this IEnumerable<decimal> source)
		public decimal? Sum(this IEnumerable<decimal?> source)
		*/
		public int Sum(Func<T, int> selector)
			{ return Enumerable.Sum<T>(_source, selector); }
		public int? Sum(Func<T, int?> selector)
			{ return Enumerable.Sum<T>(_source, selector); }
		public long Sum(Func<T, long> selector)
			{ return Enumerable.Sum<T>(_source, selector); }
		public long? Sum(Func<T, long?> selector)
			{ return Enumerable.Sum<T>(_source, selector); }
		public double Sum(Func<T, double> selector)
			{ return Enumerable.Sum<T>(_source, selector); }
		public double? Sum(Func<T, double?> selector)
			{ return Enumerable.Sum<T>(_source, selector); }
		public decimal Sum(Func<T, decimal> selector)
			{ return Enumerable.Sum<T>(_source, selector); }
		public decimal? Sum(Func<T, decimal?> selector)
			{ return Enumerable.Sum<T>(_source, selector); }
		#endregion
		
		#region Take
		public PoorMansLinq<T> Take(int count)
			{ return new PoorMansLinq<T>(Enumerable.Take<T>(_source, count)); }
		#endregion

		#region TakeWhile
		public PoorMansLinq<T> TakeWhile(Func<T, bool> predicate)
			{ return new PoorMansLinq<T>(Enumerable.TakeWhile<T>(_source, predicate)); }
		public PoorMansLinq<T> TakeWhile(Func<T, int, bool> predicate)
			{ return new PoorMansLinq<T>(Enumerable.TakeWhile<T>(_source, predicate)); }
		#endregion

		#region ThenBy
		public PoorMansLinq<T> ThenBy<TKey>(Func<T, TKey> keySelector)
			{ return new PoorMansLinq<T>(Enumerable.ThenBy<T, TKey>((OrderedSequence<T>)_source, keySelector)); }
		public PoorMansLinq<T> ThenBy<TKey>
			(Func<T, TKey> keySelector, IComparer<TKey> comparer)
			{ return new PoorMansLinq<T>(Enumerable.ThenBy<T, TKey>((OrderedSequence<T>)_source, keySelector, comparer)); }
		#endregion

		#region ThenByDescending
		public PoorMansLinq<T> ThenByDescending<TKey>(Func<T, TKey> keySelector)
			{ return new PoorMansLinq<T>(Enumerable.ThenBy<T, TKey>((OrderedSequence<T>)_source, keySelector)); }
		public PoorMansLinq<T> ThenByDescending<TKey>
			(Func<T, TKey> keySelector, IComparer<TKey> comparer)
			{ return new PoorMansLinq<T>(Enumerable.ThenBy<T, TKey>((OrderedSequence<T>)_source, keySelector, comparer)); }
		#endregion

		#region ToArray
		public T[] ToArray()
			{ return Enumerable.ToArray<T>(_source); }
		#endregion

		#region ToDictionary
		public Dictionary<TKey, TElement> ToDictionary<TKey, TElement>
			(Func<T, TKey> keySelector, Func<T, TElement> elementSelector)
			{ return Enumerable.ToDictionary<T, TKey, TElement>(_source, keySelector, elementSelector); }
		public Dictionary<TKey, TElement> ToDictionary<TKey, TElement>
			(Func<T, TKey> keySelector, Func<T, TElement> elementSelector, IEqualityComparer<TKey> comparer)
			{ return Enumerable.ToDictionary<T, TKey, TElement>(_source, keySelector, elementSelector, comparer); }
		#endregion

		#region ToList
		public List<T> ToList()
			{ return Enumerable.ToList<T>(_source); }
		#endregion

		#region ToLookup
		public Lookup<TKey, T> ToLookup<TKey>(Func<T, TKey> keySelector)
			{ return Enumerable.ToLookup<T, TKey>(_source, keySelector); }
		public Lookup<TKey, T> ToLookup<TKey>
			(Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer)
			{ return Enumerable.ToLookup<T, TKey>(_source, keySelector, comparer); }
		public Lookup<TKey, TElement> ToLookup<TKey, TElement>
			(Func<T, TKey> keySelector, Func<T, TElement> elementSelector)
			{ return Enumerable.ToLookup<T, TKey, TElement>(_source, keySelector, elementSelector); }
		public Lookup<TKey, TElement> ToLookup<TKey, TElement>
			(Func<T, TKey> keySelector, Func<T, TElement> elementSelector, IEqualityComparer<TKey> comparer)
			{ return Enumerable.ToLookup<T, TKey, TElement>(_source, keySelector, elementSelector, comparer); }
		#endregion

		#region Union
		public PoorMansLinq<T> Union(IEnumerable<T> second)
			{ return new PoorMansLinq<T>(Enumerable.Union<T>(_source, second)); }
		#endregion

		#region Where
		public PoorMansLinq<T> Where(Func<T, bool> predicate)
			{ return new PoorMansLinq<T>(Enumerable.Where<T>(_source, predicate)); }
		public PoorMansLinq<T> Where(Func<T, int, bool> predicate)
			{ return new PoorMansLinq<T>(Enumerable.Where<T>(_source, predicate)); }
		#endregion

		#region Cast, OfType
		public PoorMansLinq<TResult> Cast<TResult>()
		{
			return new PoorMansLinq<TResult>(Enumerable.Cast<TResult>(_source));
		}
		public PoorMansLinq<TResult> OfType<TResult>()
		{
			return new PoorMansLinq<TResult>(Enumerable.OfType<TResult>(_source));
		}
		#endregion

		// These methods are not included in the
		// .NET Standard Query Operators Specification,
		// but they provide additional useful commands

		public int IndexOf(T item, IEqualityComparer<T> comparer)
			{ return Enumerable.IndexOf<T>(_source, item, comparer); }
		public int IndexOf(T item)
			{ return Enumerable.IndexOf<T>(_source, item); }
		public PoorMansLinq<T> Sorted()
			{ return new PoorMansLinq<T>(Enumerable.OrderBy<T, T>(_source, delegate(T t) { return t; })); }

		#region IEnumerable<T> Members

		public IEnumerator<T> GetEnumerator()
		{
			return _source.GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return _source.GetEnumerator();
		}

		#endregion
	}
}
