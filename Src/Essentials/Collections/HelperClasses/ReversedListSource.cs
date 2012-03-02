/*
 * Created by SharpDevelop.
 * User: Pook
 * Date: 4/10/2011
 * Time: 9:06 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace Loyc.Collections
{
	/// <summary>
	/// Reversed view of IListSource returned from <see cref="LCExt.ReverseView{T}"/>
	/// </summary>
	[Serializable]
	public class ReversedListSource<T> : IterableBase<T>, IListSource<T>
	{
		IListSource<T> _list;
		public ReversedListSource(IListSource<T> list) { _list = list; }

		public IListSource<T> OriginalList { get { return _list; } }
		
		public T this[int index]
		{
			get { return _list[_list.Count - 1 - index]; }
		}
		public T TryGet(int index, ref bool fail)
		{
			return _list.TryGet(_list.Count - 1 - index, ref fail);
		}
		public int Count
		{
			get { return _list.Count; }
		}
		public override Iterator<T> GetIterator()
		{
			int i = _list.Count;;
			return delegate(ref bool fail)
			{
				return TryGet(--i, ref fail);
			};
		}
	}
}
