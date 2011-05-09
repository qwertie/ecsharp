using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Diagnostics;

namespace Loyc.Collections
{
	public class ListChangeInfo<T> : EventArgs
	{
		public ListChangeInfo(NotifyCollectionChangedAction action, int index, int sizeChange, IListSource<T> newItems)
		{
			Action = action;
			Index = index;
			SizeChange = sizeChange;
			NewItems = newItems;
			Debug.Assert(
				(action == NotifyCollectionChangedAction.Add && newItems != null && NewItems.Count == sizeChange) ||
				(action == NotifyCollectionChangedAction.Remove && newItems == null && sizeChange < 0) ||
				(action == NotifyCollectionChangedAction.Replace && newItems != null && sizeChange == 0) ||
				(action == NotifyCollectionChangedAction.Move && sizeChange == 0) ||
				(action == NotifyCollectionChangedAction.Reset));
		}

		public readonly NotifyCollectionChangedAction Action;
		public readonly int Index;
		public readonly int SizeChange;
		public readonly IListSource<T> NewItems;
	}
}
