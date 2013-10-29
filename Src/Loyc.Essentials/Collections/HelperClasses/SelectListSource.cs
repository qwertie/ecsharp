using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>
	/// Provides a modified view of an IListSource by transforming each element 
	/// on-demand. Objects of this type are returned from 
	/// <see cref="LCExt.Select{T,TResult}(IListSource{T},Func{T,TResult})"/>
	/// </summary>
	/// <typeparam name="T">input type</typeparam>
	/// <typeparam name="TResult">output type</typeparam>
	public class SelectListSource<T,TResult> : ListSourceBase<TResult>
	{
		protected IListSource<T> _list;
		protected Func<T, TResult> _selector;

		public SelectListSource(IListSource<T> list, Func<T,TResult> selector)
			{ _list = list; _selector = selector; }

		public IListSource<T> OriginalList { get { return _list; } }
		
		public new TResult this[int index]
		{
			get { return _selector(_list[index]); }
		}
		public sealed override TResult TryGet(int index, ref bool fail)
		{
			T t = _list.TryGet(index, ref fail);
			if (!fail)
				return _selector(t);
			return default(TResult);
		}
		public sealed override int Count
		{
			get { return _list.Count; }
		}
	}
}
