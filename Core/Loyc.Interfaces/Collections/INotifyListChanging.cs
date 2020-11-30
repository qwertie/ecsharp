using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Loyc.Collections
{
	/// <summary>
	/// Encapsulates the <see cref="ListChanging"/> event that notifies listeners 
	/// of dynamic changes to a collection, such as when items are about to be
	/// added and removed or the whole list is about to be refreshed.
	/// </summary>
	/// <typeparam name="T">Type of items in the list</typeparam>
	/// <typeparam name="TCollection">Type of the first argument of the event</typeparam>
	/// <remarks>
	/// This approach to change notification is more lightweight than the standard
	/// <see cref="INotifyCollectionChanged"/> interface because that interface
	/// sends both a list of new items and a list of old items, so many changes
	/// require a pair of temporary objects to be created that hold the two lists
	/// of items.
	/// <para/>
	/// In contrast, the <see cref="ListChanging"/> event includes only one list 
	/// that specifies the set of new items. In the case of Remove events, no change
	/// list is included. Since the collection has not been modified yet, the user
	/// handling the event can examine the list to learn which item(s) are being
	/// removed; if the list is being changed, it can similarly examine the list
	/// to see the old set of items.
	/// <para/>
	/// An optimization is available when only a single item is being added or
	/// changed. In that case, the collection class should create a lightweight 
	/// read-only single-item list by calling <see cref="ListExt.Single{T}(T)"/>.
	/// Such a list has less overhead than <see cref="List{T}"/> and the same
	/// overhead as an array of one item.
	/// </remarks>
	public interface INotifyListChanging<T, TCollection>
	{
		/// <summary>Occurs when the collection associated with this interface is 
		/// about to change.</summary>
		/// <remarks>
		/// The event handler receives a <see cref="ListChangeInfo{T}"/> argument,
		/// which describes the change.
		/// <para/>
		/// The event handler is not allowed to modify the list that is changing
		/// while it is handling the event, but it can read the list.
		/// <para/>
		/// IMPORTANT: if the event handler throws an exception, the change does 
		/// not actually happen. Collections that support this event must ensure
		/// that the collection is not left in an invalid state in the event that
		/// a ListChanging event handler throws an exception. When throwing an
		/// exception, be aware that this can cause other event handlers attached
		/// to the same event not to receive any change notification.
		/// </remarks>
		event ListChangingHandler<T, TCollection> ListChanging;
	}

	/// <summary>A version of <see cref="INotifyListChanging{T, TCollection}"/> 
	/// in which TCollection is fixed at <see cref="IListSource{T}"/>.</summary>
	public interface INotifyListChanging<T> : INotifyListChanging<T, IListSource<T>> { }

	/// <summary>Represents the method that handles the 
	/// <see cref="INotifyListChanging{T, TSender}.ListChanging"/> event.</summary>
	/// <param name="sender">The collection that changed.</param>
	/// <param name="args">Information about the change.</param>
	public delegate void ListChangingHandler<T, TSender>(TSender sender, ListChangeInfo<T> args);

	[Obsolete("Use ListChangingHandler<T, IListSource<T>> instead")]
	public delegate void ListChangingHandler<T>(IListSource<T> sender, ListChangeInfo<T> args);
}
