using System;
using System.Collections.Generic;
using Loyc.Essentials;
using System.Linq;

namespace Loyc.Collections
{
	public static class LCExt
	{
		#region Conversion between Loyc and BCL collection interfaces
		
		#if false
		public static IteratorEnumerator<T> AsEnumerator<T>(this Iterator<T> it)
		{
			return new IteratorEnumerator<T>(it);
		}
		
		public static Iterator<T> AsIterator<T>(this IEnumerator<T> e)
		{
			return delegate(ref bool ended)
			{
				if (e.MoveNext())
					return e.Current;
				else
				{
					ended = true;
					return default(T);
				}
			};
		}
		
		public static IEnumerable<T> AsEnumerable<T>(this IIterable<T> list)
		{
			var listE = list as IEnumerable<T>;
			if (listE != null)
				return listE;
			return AsEnumerableCore(list);
		}
		private static IEnumerable<T> AsEnumerableCore<T>(IIterable<T> list)
		{
			bool ended = false;
			for (var it = list.GetIterator();;)
			{
				T item = it(ref ended);
				if (ended)
					yield break;
				yield return item;
			}
		}

		/// <summary>Converts any IEnumerable object to IIterable.</summary>
		/// <remarks>This method is named "AsIterable" and not "ToIterable" because,
		/// in contrast to methods like ToArray() and ToList(), it does not make a 
		/// copy of the sequence.</remarks>
		public static IIterable<T> AsIterable<T>(this IEnumerable<T> list)
		{
			var listI = list as IIterable<T>;
			if (listI != null)
				return listI;
			return new EnumerableAsIterable<T>(list);
		}
		#endif
		
		/// <summary>Converts any ICollection{T} object to ISource{T}.</summary>
		/// <remarks>This method is named "AsSource" and not "ToSource" because,
		/// in contrast to methods like ToArray(), and ToList() it does not make a 
		/// copy of the sequence.</remarks>
		public static IReadOnlyCollection<T> AsSource<T>(this ICollection<T> c)
		{
			var list = c as IReadOnlyCollection<T>;
			if (list != null)
				return list;
			return new CollectionAsSource<T>(c);
		}
		
		/// <summary>Converts any ISource{T} object to a read-only ICollection{T}.</summary>
		/// <remarks>This method is named "AsCollection" and not "ToCollection" 
		/// because, in contrast to methods like ToArray() and ToList(), it does not 
		/// make a copy of the sequence.</remarks>
		public static ICollection<T> AsCollection<T>(this IReadOnlyCollection<T> c)
		{
			var list = c as ICollection<T>;
			if (list != null)
				return list;
			return new SourceAsCollection<T>(c);
		}
		
		/// <summary>Converts any IList{T} object to IListSource{T}.</summary>
		/// <remarks>This method is named "AsListSource" and not "ToListSource" 
		/// because, in contrast to methods like ToArray() and ToList(), it does not 
		/// make a copy of the sequence.</remarks>
		public static IListSource<T> AsListSource<T>(this IList<T> c)
		{
			var listS = c as IListSource<T>;
			if (listS != null)
				return listS;
			return new ListAsListSource<T>(c);
		}
		
		/// <summary>Converts any IListSource{T} object to a read-only IList{T}.</summary>
		/// <remarks>This method is named "AsList" and not "ToList" because
		/// because, in contrast to methods like ToArray(), it does not make a copy
		/// of the sequence.</remarks>
		public static IList<T> AsList<T>(this IListSource<T> c)
		{
			var list = c as IList<T>;
			if (list != null)
				return list;
			return new ListSourceAsList<T>(c);
		}

		public static IReadOnlyCollection<TResult> UpCast<T, TResult>(this IReadOnlyCollection<T> source) where T : class, TResult
		{
			#if DotNet4
			return source;
			#else
			if (source == null)
				return null;
			return new UpCastSource<T, TResult>(source);
			#endif
		}
		
		public static IListSource<TResult> UpCast<T, TResult>(this IListSource<T> source) where T : class, TResult
		{
			#if DotNet4
			return source;
			#else
			if (source == null)
				return null;
			return new UpCastListSource<T, TResult>(source);
			#endif
		}

		#endregion

		public static ReversedListSource<T> ReverseView<T>(this IListSource<T> c)
		{
			return new ReversedListSource<T>(c);
		}

		/// <inheritdoc cref="NegListSource{T}.NegListSource"/>
		public static NegListSource<T> NegView<T>(this IListSource<T> list, int zeroOffset)
		{
			return new NegListSource<T>(list, zeroOffset);
		}
		/// <inheritdoc cref="NegList{T}.NegList"/>
		public static NegList<T> NegView<T>(this IList<T> list, int zeroOffset)
		{
			return new NegList<T>(list, zeroOffset);
		}

		#region Zip for IEnumerable
		
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
			{
				yield return new Pair<A, B>(ea.Current, eb.Current);
			}
			if (successA) do
					yield return new Pair<A, B>(ea.Current, defaultB);
				while (ea.MoveNext());
		}
		public static IEnumerable<C> ZipLeft<A, B, C>(this IEnumerable<A> a, IEnumerable<B> b, B defaultB, Func<A, B, C> resultSelector)
		{
			foreach (var pair in ZipLeft(a, b, defaultB))
				yield return resultSelector(pair.A, pair.B);
		}
		public static IEnumerable<Pair<A, B>> ZipLonger<A, B>(this IEnumerable<A> a, IEnumerable<B> b, A defaultA, B defaultB)
		{
			IEnumerator<A> ea = a.GetEnumerator();
			IEnumerator<B> eb = b.GetEnumerator();
			bool successA, successB;
			while ((successA = ea.MoveNext()) & (successB = eb.MoveNext()))
			{
				yield return new Pair<A, B>(ea.Current, eb.Current);
			}
			if (successA)
				do
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
		
		#endregion	

		public static void ForEach<T>(this IEnumerable<T> list, Action<T> action)
		{
			foreach (T item in list)
				action(item);
		}

		public static string Join(this System.Collections.IEnumerable list, string separator)
		{
			return StringExt.Join(separator, list.GetEnumerator());
		}

		/// <summary>Returns all adjacent pairs (e.g. for the list {1,2,3}, returns {(1,2),(2,3)})</summary>
		public static IEnumerable<Pair<T, T>> AdjacentPairs<T>(this IEnumerable<T> list) { return AdjacentPairs(list.GetEnumerator()); }
		public static IEnumerable<Pair<T, T>> AdjacentPairs<T>(this IEnumerator<T> e)
		{
			if (e.MoveNext()) {
				T prev = e.Current;
				while (e.MoveNext()) {
					T cur = e.Current;
					yield return new Pair<T,T>(prev, cur);
					prev = cur;
				}
			}
		}

		/// <summary>Returns all adjacent pairs, treating the first and last 
		/// pairs as adjacent (e.g. for the list {1,2,3,4}, returns the pairs
		/// {(1,2),(2,3),(3,4),(4,1)}.)</summary>
		public static IEnumerable<Pair<T, T>> AdjacentPairsCircular<T>(this IEnumerable<T> list) { return AdjacentPairs(list.GetEnumerator()); }
		public static IEnumerable<Pair<T, T>> AdjacentPairsCircular<T>(this IEnumerator<T> e)
		{
			if (e.MoveNext()) {
				T first = e.Current, prev = first;
				while (e.MoveNext()) {
					T cur = e.Current;
					yield return new Pair<T,T>(prev, cur);
					prev = cur;
				}
				yield return new Pair<T,T>(prev, first);
			}
		}

		public static IListSource<TResult> Select<T, TResult>(this IListSource<T> source, Func<T, TResult> selector)
		{
			return new SelectListSource<T, TResult>(source, selector);
		}

		public static SelectNegLists<T> NegLists<T>(this IList<T> source)
		{
			return new SelectNegLists<T>(source);
		}
		public static SelectNegListSources<T> NegLists<T>(this IListSource<T> source)
		{
			return new SelectNegListSources<T>(source);
		}

		public static BufferedSequence<T> Buffered<T>(this IEnumerator<T> source)
		{
			return new BufferedSequence<T>(source);
		}
		public static BufferedSequence<T> Buffered<T>(this IEnumerable<T> source)
		{
			return new BufferedSequence<T>(source);
		}
	}

	public class UpCastSource<T, TOut> : SourceBase<TOut> where T : TOut
	{
		protected IReadOnlyCollection<T> s;
		public UpCastSource(IReadOnlyCollection<T> source) { s = source; }

		public override int Count
		{
			get { return s.Count; }
		}
		public override IEnumerator<TOut> GetEnumerator()
		{
			return s.Select(item => (TOut)item).GetEnumerator();
		}
	}

	public class UpCastListSource<T, TOut> : ListSourceBase<TOut> where T : TOut
	{
		IListSource<T> _list;
		public UpCastListSource(IListSource<T> original) { _list = original; }
		
		public override TOut TryGet(int index, ref bool fail)
		{
			return _list.TryGet(index, ref fail);
		}
		public override int Count
		{
			get { return _list.Count; }
		}
	}
}
