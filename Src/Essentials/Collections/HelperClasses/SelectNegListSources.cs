using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>
	/// Provides view of an <see cref="IListSource{T}"/> in which element [i] is a
	/// <see cref="NegListSource{T}"/> N such that N[0] refers to element [i] in the 
	/// original list. See <see cref="LCExt.NegLists{T}(IListSource{T})"/> for more 
	/// information.
	/// </summary>
	/// <seealso cref="SelectNegLists{T}"/>
	[Serializable]
	public class SelectNegListSources<T> : ListSourceBase<NegListSource<T>>
	{
		protected IListSource<T> _list;

		public SelectNegListSources(IListSource<T> list) { _list = list; }

		public IListSource<T> OriginalList { get { return _list; } }
		
		public sealed override NegListSource<T> TryGet(int index, ref bool fail)
		{
			fail = (uint)index >= (uint)_list.Count;
			return new NegListSource<T>(_list, index);
		}
		public sealed override int Count
		{
			get { return _list.Count; }
		}
	}
}
