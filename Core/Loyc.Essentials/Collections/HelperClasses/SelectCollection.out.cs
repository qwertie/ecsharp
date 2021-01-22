// Generated from SelectCollection.ecs by LeMP custom tool. LeMP version: 2.9.1.0
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

namespace Loyc.Collections
{

	/// <summary>Helper class for <see cref="LinqToLists"/>.</summary>
	public class SelectCollection<ListT, T, TResult> : ReadOnlyCollectionBase<TResult> where ListT: ICollection<T>
	
	{
		protected ListT _list;
		protected Func<T, TResult> _selector;
		public SelectCollection(ListT list, Func<T, TResult> selector) {
			_list = list;
			_selector = selector;
			if (_list == null || _selector == null)
				throw new ArgumentNullException();
		}

		public sealed override IEnumerator<TResult> GetEnumerator()
		{
			return Enumerable.Select(_list, _selector).GetEnumerator();
		}
		public sealed override int Count
		{
			get { return _list.Count; }
		}
	}

	/// <summary>Helper class for <see cref="LinqToLists"/>.</summary>
	public class SelectReadOnlyCollection<ListT, T, TResult> : ReadOnlyCollectionBase<TResult> where ListT: IReadOnlyCollection<T>
	
	{
		protected ListT _list;
		protected Func<T, TResult> _selector;
		public SelectReadOnlyCollection(ListT list, Func<T, TResult> selector) {
			_list = list;
			_selector = selector;
			if (_list == null || _selector == null)
				throw new ArgumentNullException();
		}

		public sealed override IEnumerator<TResult> GetEnumerator()
		{
			return Enumerable.Select(_list, _selector).GetEnumerator();
		}
		public sealed override int Count
		{
			get { return _list.Count; }
		}
	}
}