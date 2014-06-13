using System;
using System.Collections.Generic;
using System.Linq;

namespace Loyc.Collections
{
	/// <summary>Extension methods for Loyc Collection interfaces 
	/// (such as <see cref="IListSource{T}"/>) and for Loyc Collection 
	/// adapters (such as <see cref="AsReadOnly{T}()"/>, which returns
	/// a <see cref="CollectionAsReadOnly{T}"/> adapter.)</summary>
	/// <remarks>
	/// The source code for adapter extension methods such as AsReadOnly() is now 
	/// placed in the source file for each adapter class (e.g. CollectionAsReadOnly.cs)
	/// to make it easier to use parts of Loyc.Essentials rather than the entire 
	/// library (other "decoupling" suggestions are welcome.)
	/// </remarks>
	public static partial class LCExt
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

		public static IReadOnlyCollection<TResult> UpCast<T, TResult>(this IReadOnlyCollection<T> source) where T : class, TResult
		{
			#if DotNet4 || DotNet4_5
			return source;
			#else
			if (source == null)
				return null;
			return new UpCastSource<T, TResult>(source);
			#endif
		}
		
		public static IListSource<TResult> UpCast<T, TResult>(this IListSource<T> source) where T : class, TResult
		{
			#if DotNet4 || DotNet4_5
			return source;
			#else
			if (source == null)
				return null;
			return new UpCastListSource<T, TResult>(source);
			#endif
		}

		#endregion

		public static string Join(this System.Collections.IEnumerable list, string separator)
		{
			return StringExt.Join(separator, list.GetEnumerator());
		}

		public static IListSource<TResult> Select<T, TResult>(this IListSource<T> source, Func<T, TResult> selector)
		{
			return new SelectListSource<T, TResult>(source, selector);
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
		
		public override TOut TryGet(int index, out bool fail)
		{
			return _list.TryGet(index, out fail);
		}
		public override int Count
		{
			get { return _list.Count; }
		}
	}
}
