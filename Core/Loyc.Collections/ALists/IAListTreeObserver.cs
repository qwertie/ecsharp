using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using System.Diagnostics;

namespace Loyc.Collections.Impl
{
	/// <summary>
	/// An interface that is called to notify observers when items or nodes in the 
	/// tree of a class derived from <see cref="AListBase{K,T}"/> (e.g. AList or 
	/// BList) are added or removed.
	/// </summary>
	/// <typeparam name="K"></typeparam>
	/// <typeparam name="T"></typeparam>
	/// <remarks>
	/// This interface is useful for keeping track of information about a collection 
	/// that is impossible to track efficiently by any other means. It can be used 
	/// to:
	/// <ul>
	/// <li>- Maintain an inverse dictionary of items in an AList, so you can get 
	/// the (integer) index of an item in O(log N) time. This was the original 
	/// purpose for which the observer idea was developed.</li>
	/// <li>- Keep certain statistics about an unsorted list up-to-date efficiently,
	/// such as the maximum, minimum or average.</li>
	/// <li>- Connect AList/BList to Update Controls.</li>
	/// </ul>
	/// An observer object should be attached to only one list, because change 
	/// notifications do not indicate which list is being changed.
	/// <para/>
	/// When an A-list is cloned, observers do are not included in the clone.
	/// <para/>
	/// IMPORTANT: unless otherwise noted, implementations of this interface 
	/// must not throw exceptions because these methods are called during 
	/// operations in progress. If you throw an exception, the tree can be 
	/// left in an invalid state. Attach() can safety throw, but the exception 
	/// will propagate out of the <see cref="AListBase{K,T}.AddObserver"/> method.
	/// </remarks>
	public interface IAListTreeObserver<K,T>
	{
		/// <summary>Called when the observer is being attached to an AList.</summary>
		/// <param name="list">The list that the observer is being attached to.</param>
		/// <param name="populate">The observer can invoke this delegate to cause 
		/// notifications to be sent about all the nodes in the tree through a
		/// depth-first search that calls <see cref="AddAll"/> for each node in
		/// the tree. When calling this delegate, use a parameter of True if you 
		/// want AddAll to be called for children before parents (roughly, leaves 
		/// first). Use False if you want AddAll to be called for inner nodes 
		/// before their children. populate() also calls <see cref="RootChanged"/>()
		/// before scanning the tree.
		/// </param>
		/// <remarks>
		/// If Attach() throws an exception, <see cref="AListBase{K,T}"/> will
		/// cancel the AddObserver() operation and it will not catch the exception.
		/// </remarks>
		void Attach(AListBase<K,T> list, Action<bool> populate);
		
		/// <summary>Called when the observer is being detached from an AList.</summary>
		void Detach();
		
		/// <summary>Called when the root of the tree changes, or when the
		/// list is cleared.</summary>
		/// <param name="clear">true if the root is changing due to a Clear() 
		/// operation. If this parameter is true, the observer should clear its
		/// own state. If this parameter is false but newRoot is null, it means
		/// that the list was cleared by removing all the items (rather than 
		/// by calling Clear() on the list). In that case, if the observer still 
		/// believes that any items exist in leaf nodes, it means that there is 
		/// a bookkeeping error somewhere.</param>
		/// <param name="newRoot">The new root (null if the tree is cleared).</param>
		void RootChanged(AListNode<K, T> newRoot, bool clear);
		
		/// <summary>Called when an item is added to a leaf node.</summary>
		/// <remarks>Note: this may be called as part of a move operation (remove+add)</remarks>
		void ItemAdded(T item, AListLeaf<K, T> parent);
		
		/// <summary>Called when an item is removed from a leaf node.</summary>
		/// <remarks>Note: this may be called as part of a move operation (remove+add)</remarks>
		void ItemRemoved(T item, AListLeaf<K, T> parent);
		
		/// <summary>Called when a child node is added to an inner node.</summary>
		/// <remarks>Note: this may be called as part of a move operation (remove+add)</remarks>
		void NodeAdded(AListNode<K, T> child, AListInnerBase<K, T> parent);
		
		/// <summary>Called when a child node is removed from an inner node.</summary>
		/// <remarks>Note: this may be called as part of a move operation (remove+add)</remarks>
		void NodeRemoved(AListNode<K, T> child, AListInnerBase<K, T> parent);
		
		/// <summary>Called when all children are being removed from a node (leaf 
		/// or inner). Notifications are not sent for individual children.</summary>
		void RemoveAll(AListNode<K, T> node);
		
		/// <summary>Called when all children are being added to a node (leaf 
		/// or inner). Notifications are not sent for individual children.</summary>
		void AddAll(AListNode<K, T> node);

		/// <summary>Called when a tree modification operation is completed.</summary>
		/// <remarks>This is called after each modification operation (Add,
		/// Insert, Remove, Replace, etc.); the list will normally be in a
		/// read-only state ("frozen for concurrency") when this method is 
		/// called, so do not initiate changes from here.
		/// <para/>
		/// This method can safely throw an exception, and the list class will
		/// not swallow it. Note: if there are multiple observers, throwing an
		/// exception from one observers will prevent this notification from
		/// reaching other observers that have not been notified yet.
		/// </remarks>
		void CheckPoint();
	}

	/// <summary>Helper methods for <see cref="IAListTreeObserver{K,T}"/>.</summary>
	public static class AListTreeObserverExt
	{
		internal static void DoAttach<K, T>(this IAListTreeObserver<K, T> observer, AListNode<K, T> root, AListBase<K, T> list)
		{
			observer.Attach(list, childrenFirst =>
			{
				observer.RootChanged(root, false);
				if (root != null)
					AddAllRecursively(observer, childrenFirst, root);
			});
		}

		private static void AddAllRecursively<K, T>(IAListTreeObserver<K, T> observer, bool childrenFirst, AListNode<K, T> node)
		{
			if (!childrenFirst)
				observer.AddAll(node);

			var inner = node as AListInnerBase<K, T>;
			if (inner != null)
				for (int i = 0; i < inner.LocalCount; i++)
					AddAllRecursively(observer, childrenFirst, inner.Child(i));

			if (childrenFirst)
				observer.AddAll(node);
		}

		internal static void Clear<K, T>(this IAListTreeObserver<K, T> self)
		{
			self.RootChanged(null, true);
		}

		internal static void AddingItems<K, T>(this IAListTreeObserver<K, T> self, IListSource<T> list, AListLeaf<K, T> parent, bool isMoving)
		{
			for (int i = 0; i < list.Count; i++)
				self.ItemAdded(list[i], parent);
		}
		internal static void RemovingItems<K, T>(this IAListTreeObserver<K, T> self, InternalDList<T> list, int index, int count, AListLeaf<K, T> parent, bool isMoving)
		{
			for (int i = index; i < index + count; i++)
				self.ItemRemoved(list[i], parent);
		}

		internal static void ItemMoved<K, T>(this IAListTreeObserver<K, T> self, T item, AListLeaf<K, T> oldParent, AListLeaf<K, T> newParent)
		{
			self.ItemRemoved(item, oldParent);
			self.ItemAdded(item, newParent);
		}
		internal static void NodeMoved<K, T>(this IAListTreeObserver<K, T> self, AListNode<K, T> child, AListInnerBase<K, T> oldParent, AListInnerBase<K, T> newParent)
		{
			self.NodeRemoved(child, oldParent);
			self.NodeAdded(child, newParent);
		}

		internal static void HandleRootSplit<K, T>(this IAListTreeObserver<K, T> self, AListNode<K, T> oldRoot, AListNode<K, T> newLeft, AListNode<K, T> newRight, AListInnerBase<K, T> newRoot)
		{
			self.HandleNodeReplaced(oldRoot, newLeft, newRight);
			self.NodeAdded(newLeft, newRoot);
			self.NodeAdded(newRight, newRoot);
			self.RootChanged(newRoot, false);
		}

		internal static void HandleRootUnsplit<K, T>(this IAListTreeObserver<K, T> self, AListInnerBase<K, T> oldRoot, AListNode<K, T> newRoot)
		{
			Debug.Assert(oldRoot.LocalCount == 0 || (oldRoot.LocalCount == 1 && oldRoot.Child(0) == newRoot));
			self.NodeRemoved(newRoot, oldRoot);
			self.RootChanged(newRoot, false);
		}

		internal static void HandleChildReplaced<K, T>(this IAListTreeObserver<K, T> self, AListNode<K, T> oldNode, AListNode<K, T> newLeft, AListNode<K, T> newRight, AListInnerBase<K, T> parent)
		{
			self.HandleNodeReplaced(oldNode, newLeft, newRight);
			if (parent != null)
			{
				self.NodeRemoved(oldNode, parent);
				self.NodeAdded(newLeft, parent);
				if (newRight != null)
					self.NodeAdded(newRight, parent);
			}
		}

		internal static void HandleNodeReplaced<K, T>(this IAListTreeObserver<K, T> self, AListNode<K, T> oldNode, AListNode<K, T> newLeft, AListNode<K, T> newRight)
		{
			if (newRight == null)
			{	// cloned, not split
				Debug.Assert(oldNode.IsFrozen && !newLeft.IsFrozen);
				Debug.Assert(oldNode.LocalCount == newLeft.LocalCount);
			}
			self.RemoveAll(oldNode);
			self.AddAll(newLeft);
			if (newRight != null)
				self.AddAll(newRight);
		}
	}
}
