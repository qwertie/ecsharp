using System;
using System.Collections.Generic;
using Loyc.Essentials;
using Loyc.Collections.Linq;

namespace Loyc.Collections
{
	public static class LCExt
	{
		#region Conversion between Loyc and BCL collection interfaces
		
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
		
		/// <summary>Converts any ICollection{T} object to ISource{T}.</summary>
		/// <remarks>This method is named "AsSource" and not "ToSource" because,
		/// in contrast to methods like ToArray(), and ToList() it does not make a 
		/// copy of the sequence.</remarks>
		public static ISource<T> AsSource<T>(this ICollection<T> c)
		{
			var list = c as ISource<T>;
			if (list != null)
				return list;
			return new CollectionAsSource<T>(c);
		}
		
		/// <summary>Converts any ISource{T} object to a read-only ICollection{T}.</summary>
		/// <remarks>This method is named "AsCollection" and not "ToCollection" 
		/// because, in contrast to methods like ToArray() and ToList(), it does not 
		/// make a copy of the sequence.</remarks>
		public static ICollection<T> AsCollection<T>(this ISource<T> c)
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
		
		#endregion

		/// <summary>See <see cref="IteratorToIterableAdapter{T}"/> for more information.</summary>
		public static IteratorToIterableAdapter<T> ToIIterableUnsafe<T>(this Iterator<T> it)
		{
			return new IteratorToIterableAdapter<T>(it);
		}

		public static ReversedListSource<T> ReverseView<T>(this IListSource<T> c)
		{
			return new ReversedListSource<T>(c);
		}
		
		public static ListSourceSlice<T> Slice<T>(this IListSource<T> list, int start, int length)
		{
			return new ListSourceSlice<T>(list, start, length);
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

		#region Zip for IEnumerable (TODO: IIterable)
		
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

		public static IEnumerable<Pair<T, T>> AdjacentPairs<T>(this IEnumerable<T> list)
		{
			return Iterable.AdjacentPairs(list.AsIterable());
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
	}
}
