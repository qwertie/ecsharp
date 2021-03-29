using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Diagnostics;
using System.ComponentModel;

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
	public struct ListChangeInfo<T>
	{
		/// <summary>Initializes the members of <see cref="ListChangeInfo{T}"/>.</summary>
		public ListChangeInfo(NotifyCollectionChangedAction action, int index, int sizeChange, IListSource<T>? newItems) 
			: this(action, index, sizeChange, newItems, null) { }

		public ListChangeInfo(NotifyCollectionChangedAction action, int index, int sizeChange, IListSource<T>? newItems, IListSource<T>? oldItems)
		{
			Action = action;
			Index = index;
			SizeChange = sizeChange;
			_newItems = newItems;
			_oldItems = oldItems;
			_collection = null;
			Debug.Assert(
				(action == NotifyCollectionChangedAction.Add && newItems != null && newItems.Count == sizeChange) ||
				(action == NotifyCollectionChangedAction.Remove && (newItems == null || newItems.Count == 0) && sizeChange < 0) ||
				(action == NotifyCollectionChangedAction.Replace && newItems != null && sizeChange == 0) ||
				(action == NotifyCollectionChangedAction.Move && sizeChange == 0) ||
				(action == NotifyCollectionChangedAction.Reset));
		}

		/// <summary>This contructor is meant for ListChanging events only (not ListChanged).
		/// It computes the OldItems property automatically, on-demand, if the user gets it.</summary>
		/// <param name="collection">The list that is about to change.</param>
		/// <param name="action">Should be Remove, Reset or Replace.</param>
		public ListChangeInfo(IListSource<T> collection, NotifyCollectionChangedAction action, int index, int sizeChange, IListSource<T>? newItems = null)
			: this(action, index, sizeChange, newItems, null) => _collection = collection;

		/// <summary>Gets a value that indicates the type of change being made to 
		/// the collection.</summary>
		public readonly NotifyCollectionChangedAction Action;

		/// <summary>Gets the index at which the add, remove, or change operation starts.
		/// If the collection is not a list (e.g. it is a dictionary), this should be 
		/// Int32.MinValue.</summary>
		public readonly int Index;

		/// <summary>Returns <see cref="Index"/>. Exists for compatibility with NotifyCollectionChangedEventArgs.</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int NewStartingIndex => Index;
		/// <summary>Returns <see cref="Index"/>. Exists for compatibility with NotifyCollectionChangedEventArgs.</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int OldStartingIndex => Index;

		/// <summary>Gets the amount by which the collection size changes. When 
		/// items are being added, this is positive, and when items are being
		/// removed, this is negative. This is 0 when existing items are only being 
		/// replaced.</summary>
		public readonly int SizeChange;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly IListSource<T>? _newItems;
		private IListSource<T>? _oldItems, _collection;

		/// <summary>Represents either new item(s) that are being added to the 
		/// collection, or item(s) that are replacing existing item(s) in the 
		/// collection. When items are being removed from an indexed list,
		/// this member is either null or empty.</summary>
		/// <remarks>
		/// In a ListChanged event, the collection returned from this property 
		/// could be a slice that will be invalid after the event is over.
		/// Avoid storing it without making a copy.
		/// </remarks>
		public IListSource<T>? NewItems => _newItems;

		/// <summary>This member may provide a list of old items that are being 
		/// removed or replaced in the collection. It may be null when items are
		/// being added.</summary>
		/// <remarks>
		/// In a ListChanging event, the collection returned from this property 
		/// may be empty if the list is being sorted in-place, since it is not
		/// known what the new list will be. In any case, this property is
		/// typically a slice and therefore is not valid after the event is over;
		/// avoid storing it without making a copy.
		/// </remarks>
		public IListSource<T>? OldItems => _oldItems ?? ComputeOldItems();

		private IListSource<T>? ComputeOldItems()
		{
			if (_collection == null)
				return null;
			return _oldItems = _collection.Slice(Index, (NewItems?.Count ?? 0) - SizeChange);
		}
	}
}
