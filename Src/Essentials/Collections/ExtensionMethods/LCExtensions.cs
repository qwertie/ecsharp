/*
 * Created by SharpDevelop.
 * User: Pook
 * Date: 4/10/2011
 * Time: 8:42 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

namespace Loyc.Collections
{
	public static class LCExtensions
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
		
		public static ReversedListSource<T> Reversed<T>(this IListSource<T> c)
		{
			return new ReversedListSource<T>(c);
		}
		
		public static ListSourceSlice<T> Slice<T>(this IListSource<T> list, int start, int length)
		{
			return new ListSourceSlice<T>(list, start, length);
		}
	}
}
