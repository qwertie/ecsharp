using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Collections
{
	/// <summary>
	/// Encapsulates a <see cref="ListChanged"/> event that notifies listeners 
	/// that a list has changed, such as when items are added or removed or the 
	/// whole list is refreshed.
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
	/// In contrast, the <see cref="ListChanged"/> event includes only one list 
	/// that specifies the set of new items. No list of old items is provided.
	/// <para/>
	/// An optimization is available when only a single item is being added or
	/// changed. In that case, the collection class should create a lightweight 
	/// read-only single-item list by calling <see cref="ListExt.Single{T}(T)"/>.
	/// Such a list has less overhead than <see cref="List{T}"/> and the same
	/// overhead as an array of one item.
	/// </remarks>
	public interface INotifyListChanged<T, TCollection>
	{
		/// <summary>Occurs when the collection associated with this interface has
		/// changed.</summary>
		/// <remarks>
		/// The event handler receives a <see cref="ListChangeInfo{T}"/> argument,
		/// which describes the change.
		/// <para/>
		/// The event handler should not modify the list that is changing while 
		/// it is handling the event, because there may be other event handlers
		/// that have not received the event; if you modify the collection, more
		/// ListChanged events will occur before the initial event is over, which
		/// could cause other handlers to receive notifications in the wrong order.
		/// Event handlers should not throw: other event handlers attached to the 
		/// same event will not receive the change notification, and the collection 
		/// will not revert to its old state.
		/// </remarks>
		event ListChangingHandler<T, TCollection> ListChanged;
	}

	/// <summary>A version of <see cref="INotifyListChanging{T, TCollection}"/> 
	/// in which TCollection is fixed at <see cref="IListSource{T}"/>.</summary>
	public interface INotifyListChanged<T> : INotifyListChanged<T, IListSource<T>> { }
}
