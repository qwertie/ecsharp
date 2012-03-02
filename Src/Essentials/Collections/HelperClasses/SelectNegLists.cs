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
	public class SelectNegLists<T> : IterableBase<NegList<T>>, IListSource<NegList<T>>
	{
		protected IList<T> _list;

		public SelectNegLists(IList<T> list) { _list = list; }

		public IList<T> OriginalList { get { return _list; } }
		
		public NegList<T> this[int index]
		{
			get { return new NegList<T>(_list, index); }
		}
		public NegList<T> TryGet(int index, ref bool fail)
		{
			fail = (uint)index >= (uint)_list.Count;
			return new NegList<T>(_list, index);
		}
		public int Count
		{
			get { return _list.Count; }
		}
		public sealed override Iterator<NegList<T>> GetIterator()
		{
			int i = -1;
			return delegate(ref bool ended)
			{
				ended = ((uint)++i >= (uint)_list.Count);
				return new NegList<T>(_list, i);
			};
		}
	}
}
