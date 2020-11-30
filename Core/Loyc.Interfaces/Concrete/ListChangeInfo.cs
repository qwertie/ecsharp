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
		public ListChangeInfo(NotifyCollectionChangedAction action, int index, int sizeChange, IListSource<T> newItems) : this(action, index, sizeChange, newItems, null) { }

		public ListChangeInfo(NotifyCollectionChangedAction action, int index, int sizeChange, IListSource<T> newItems, IListSource<T> oldItems)
		{
			Action = action;
			Index = index;
			SizeChange = sizeChange;
			_newItems = newItems;
			_oldItems = oldItems;
			Debug.Assert(
				(action == NotifyCollectionChangedAction.Add && newItems != null && NewItems.Count == sizeChange) ||
				(action == NotifyCollectionChangedAction.Remove && (newItems == null || newItems.Count == 0) && sizeChange < 0) ||
				(action == NotifyCollectionChangedAction.Replace && newItems != null && sizeChange == 0) ||
				(action == NotifyCollectionChangedAction.Move && sizeChange == 0) ||
				(action == NotifyCollectionChangedAction.Reset));
		}

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
		private readonly IListSource<T> _newItems, _oldItems;

		/// <summary>Represents either new item(s) that are being added to the 
		/// collection, or item(s) that are replacing existing item(s) in the 
		/// collection. When items are being removed from an indexed list,
		/// this member is either null or empty.</summary>
		public IListSource<T> NewItems => _newItems;

		/// <summary>This member is often null, but it may provide a list of 
		/// old items that are being removed or replaced in the collection.
		/// The <see cref="INotifyListChanging{T, TCollection}.ListChanging"/> event
		/// does not need to provide this list unless Index is int.MinValue, because
		/// the event receiver can figure out what the OldItems are by looking at
		/// the original collection, which has not yet changed.</summary>
		/// <remarks>
		/// This property was added in version v2.9.0, and is null when the providing
		/// collection class does not support it. Notably, the AList/BList family of 
		/// data structures never provide this list (as of v2.9.0).
		/// <para/>
		/// When you receive a ListChanging event, the set of old items is 
		/// <c>var oldItems = sender.Slice(args.Index, (args.NewItems?.Count ?? 0) - args.SizeChange)];</c>
		/// When you receive a ListChanged event, the only way to get the set of
		/// old items is to read this property. In general, your event handler can 
		/// use the following code to get the list of old items:
		/// <para/>
		/// <code>var oldItems = args.OldItems ?? sender.Slice(args.Index, (args.NewItems?.Count ?? 0) - args.SizeChange)];</code>
		/// </remarks>
		public IListSource<T> OldItems => _oldItems;
	}
}
