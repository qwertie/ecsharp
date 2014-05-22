using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>
	/// Provides view of an <see cref="IList{T}"/> in which element [i] is a 
	/// <see cref="NegList{T}"/> N such that N[0] refers to element [i] in the 
	/// original list. See <see cref="LCExt.NegLists{T}(IList{T})"/> for more 
	/// information.
	/// </summary>
	/// <seealso cref="SelectNegListSources{T}"/>
	[Serializable]
	public class SelectNegLists<T> : ListSourceBase<NegList<T>>
	{
		protected IList<T> _list;

		public SelectNegLists(IList<T> list) { _list = list; }

		public IList<T> OriginalList { get { return _list; } }
		
		public sealed override NegList<T> TryGet(int index, out bool fail)
		{
			fail = (uint)index >= (uint)_list.Count;
			return new NegList<T>(_list, index);
		}
		public sealed override int Count
		{
			get { return _list.Count; }
		}
	}
}
