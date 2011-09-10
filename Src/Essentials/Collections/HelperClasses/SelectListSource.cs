using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections.Linq;

namespace Loyc.Collections
{
	/// <summary>
	/// Provides a modified view of an IListSource by transforming each element 
	/// on-demand. Objects of this type are returned from 
	/// <see cref="LCExt.Select{T,TResult}(IListSource{T},Func{T,TResult})"/>
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class SelectListSource<T,TResult> : IterableBase<TResult>, IListSource<TResult>
	{
		protected IListSource<T> _list;
		protected Func<T, TResult> _selector;

		public SelectListSource(IListSource<T> list, Func<T,TResult> selector)
			{ _list = list; _selector = selector; }

		public IListSource<T> OriginalList { get { return _list; } }
		
		public TResult this[int index]
		{
			get { return _selector(_list[index]); }
		}
		public TResult TryGet(int index, ref bool fail)
		{
			T t = _list.TryGet(index, ref fail);
			if (!fail)
				return _selector(t);
			return default(TResult);
		}
		public int Count
		{
			get { return _list.Count; }
		}
		public sealed override Iterator<TResult> GetIterator()
		{
			var it = _list.GetIterator();
			return delegate(ref bool ended)
			{
				T current = it(ref ended);
				if (ended) return default(TResult);
				return _selector(current);
			};
		}
	}
}
