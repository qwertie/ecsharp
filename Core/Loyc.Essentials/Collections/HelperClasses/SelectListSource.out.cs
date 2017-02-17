// Generated from SelectListSource.ecs by LeMP custom tool. LeMP version: 2.4.0.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using Loyc.Collections.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>
	/// Helper class: provides a modified view of an IList by transforming each element 
	/// on-demand. Objects of this type are returned from 
	/// <see cref="LinqToLists.Select{T,TResult}(IList{T},Func{T,TResult})"/>
	/// </summary>
	/// <typeparam name="T">input type</typeparam>
	/// <typeparam name="TResult">output type</typeparam>
	public class SelectList<T, TResult> : ListSourceBase<TResult> {
		protected IList<T> _list;
		protected Func<T, TResult> _selector;
	
		public SelectList(IList<T> list, Func<T, TResult> selector)
		{ _list = list; _selector = selector; }
	
		public IList<T> OriginalList { get {
				return _list;
			} }
	
		new public TResult this[int index]
		{
			get { return _selector(_list[index]); }
		}
	
		public sealed override TResult TryGet(int index, out bool fail)
		{
			if (!(fail = ((uint) index >= (uint) _list.Count)))
				return _selector(_list[index]);
			else
				return default(TResult);
		}
	
		public sealed override int Count
		{
			get { return _list.Count; }
		}
	}

	/// <summary>
	/// Helper class: provides a modified view of an IListSource by transforming each element 
	/// on-demand. Objects of this type are returned from 
	/// <see cref="LinqToLists.Select{T,TResult}(IListSource{T},Func{T,TResult})"/>
	/// </summary>
	/// <typeparam name="T">input type</typeparam>
	/// <typeparam name="TResult">output type</typeparam>
	public class SelectListSource<T, TResult> : ListSourceBase<TResult> {
		protected IListSource<T> _list;
		protected Func<T, TResult> _selector;
	
		public SelectListSource(IListSource<T> list, Func<T, TResult> selector)
		{ _list = list; _selector = selector; }
	
		public IListSource<T> OriginalList { get {
				return _list;
			} }
	
		new public TResult this[int index]
		{
			get { return _selector(_list[index]); }
		}
	
		public sealed override TResult TryGet(int index, out bool fail)
		{
			T t = _list.TryGet(index, out fail);
			if (!fail)
				return _selector(t);
			else
				return default(TResult);
		}
	
		public sealed override int Count
		{
			get { return _list.Count; }
		}
	}
}