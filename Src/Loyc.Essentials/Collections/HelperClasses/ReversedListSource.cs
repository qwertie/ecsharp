/*
 * Created by SharpDevelop.
 * User: Pook
 * Date: 4/10/2011
 * Time: 9:06 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

namespace Loyc.Collections
{
	/// <summary>
	/// Reversed view of IListSource returned from <see cref="LCExt.ReverseView{T}"/>
	/// </summary>
	[Serializable]
	public class ReversedListSource<T> : ListSourceBase<T>
	{
		IListSource<T> _list;
		public ReversedListSource(IListSource<T> list) { _list = list; }

		public IListSource<T> OriginalList { get { return _list; } }
		
		public new T this[int index]
		{
			get { return _list[_list.Count - 1 - index]; }
		}
		public sealed override T TryGet(int index, out bool fail)
		{
			return _list.TryGet(_list.Count - 1 - index, out fail);
		}
		public sealed override int Count
		{
			get { return _list.Count; }
		}
	}
}
