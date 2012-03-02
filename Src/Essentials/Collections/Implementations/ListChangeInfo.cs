using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Diagnostics;

namespace Loyc.Collections
{
	/// <summary>Contains information about how a collection is about to change.</summary>
	/// <typeparam name="T">Type of element in the collection</typeparam>
	/// <remarks>
	/// In contrast to <see cref="NotifyCollectionChangedEventArgs"/>, this object
	/// represents changes that are about to happen, not changes that have happened
	/// already.
	/// </remarks>
	/// <seealso cref="INotifyListChanging{T}"/>
	[Serializable]
	public class ListChangeInfo<T> : EventArgs
	{
		/// <summary>Initializes the members of <see cref="ListChangeInfo{T}"/>.</summary>
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

		/// <summary>Gets a value that indicates the type of change being made to 
		/// the collection.</summary>
		public readonly NotifyCollectionChangedAction Action;

		/// <summary>Gets the index at which the add, remove, or change operation starts.</summary>
		public readonly int Index;

		/// <summary>Gets the amount by which the collection size changes. When 
		/// items are being added, this is positive, and when items are being
		/// removed, this is negative. This is 0 when existing items are only being 
		/// replaced.</summary>
		public readonly int SizeChange;
		
		/// <summary>Represents either new items that are being added to the 
		/// collection, or items that are about to replace existing items in 
		/// the collection. This member is null or empty when items are being 
		/// removed.</summary>
		public readonly IListSource<T> NewItems;
	}
}
