using Loyc.Collections.Impl;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Loyc.Collections
{
	/// <summary>Extension methods for Loyc Collection interfaces 
	/// (such as <see cref="IListSource{T}"/>).</summary>
	/// <remarks>
	/// The source code for adapter extension methods such as AsReadOnly() is now 
	/// placed in the source file for each adapter class (e.g. CollectionAsReadOnly.cs)
	/// to make it easier to use parts of Loyc.Essentials rather than the entire 
	/// library (other "decoupling" suggestions are welcome.)
	/// </remarks>
	public static partial class LCExt
	{
		#region Conversion between Loyc and BCL collection interfaces

		[Obsolete(".NET 4+ can upcast by itself without this method")]
		public static IReadOnlyCollection<TResult> UpCast<T, TResult>(this IReadOnlyCollection<T> source) where T : class, TResult
		{
			return source;
		}

		[Obsolete(".NET 4+ can upcast by itself without this method")]
		public static IListSource<TResult> UpCast<T, TResult>(this IListSource<T> source) where T : class, TResult
		{
			return source;
		}

		#endregion

		public static string Join(this System.Collections.IEnumerable list, string separator)
		{
			return StringExt.Join(separator, list.GetEnumerator());
		}
	}

	/// <summary>Helper class for treating a collection of a derived type as a collection of a base type or interface.</summary>
	/// <see cref="LCExt.UpCast{T, TResult}(IListSource{T})"/>
	[Obsolete("Not being used, will probably remove in the future")]
	public class UpCastSource<T, TOut> : ReadOnlyCollectionBase<TOut> where T : TOut
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

	/// <summary>Helper class for treating a collection of a derived type as a collection of a base type or interface.</summary>
	/// <see cref="LCExt.UpCast{T, TResult}(IReadOnlyCollection{T})"/>
	/// <remarks>This class is rarely needed because generic variance allows casting 
	/// IListSource{DerivedClass} to IListSource{BaseClass}. However, it is still useful
	/// to convert a list of structs from IListSource{Struct} to IListSource{IInterface}.</remarks>
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
