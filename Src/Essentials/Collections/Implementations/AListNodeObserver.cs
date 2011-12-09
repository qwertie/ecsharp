using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Collections.Linq;

namespace Loyc.Collections.Impl
{
	/// <summary>Base class for an AList index, whose purpose is to provide fast 
	/// IndexOf() functionality by keeping track of which nodes contain which 
	/// items.</summary>
	/// <typeparam name="T">Type of item stored in the corresponding AList</typeparam>
	/// <remarks>
	/// This class is not well documented because I consider it unlikely that one 
	/// would want to write another implementation of it. Nevertheless, I split out
	/// the interface into this abstract base class, just in case someone does want 
	/// to write their own index implementation. The standard implementation is 
	/// <see cref="AListIndexer{T}"/>.
	/// 
	/// TODO:
	/// - Check that all cloning is handled.
	/// - Check that all splitting is handled.
	/// - Check that all TakeFromLeft/Right is handled.
	/// - Check that all leaf changes are handled
	/// - Factor out certain calls to indexer to shorten/simplify code
	/// - Add a method to install indexer in AList
	/// - Write unit tests
	/// - Write benchmarks
	/// - Try optimizations: _isFrozen in AListNode, _children[0].Index always 0, 
	///                      redundant-if optimization on calls to AutoClone, 
	///                      larger inner nodes
	/// </remarks>
	public class AListNodeObserver<K, T>
	{
		public AListNodeObserver(AListNode<K, T> root) { _root = root; }

		protected AListNode<K, T> _root;

		/// <summary>Root node of the tree, kept up-to-date by AList.</summary>
		public virtual AListNode<K, T> Root { get { return _root; } set { _root = value; } }

		public delegate void LeafEvent(T item, AListLeaf<K, T> parent, bool isMoving);
		public delegate void InnerEvent(AListNode<K, T> child, AListInnerBase<K, T> parent, bool isMoving);

		public event LeafEvent ItemAdded;
		public event LeafEvent ItemRemoved;
		public event InnerEvent NodeAdded;
		public event InnerEvent NodeRemoved;

		protected internal virtual void OnItemAdded(T item, AListLeaf<K, T> parent, bool isMoving)
		{
			if (ItemAdded != null)
				ItemAdded(item, parent, isMoving);
		}
		protected internal virtual void OnItemRemoved(T item, AListLeaf<K, T> parent, bool isMoving)
		{
			if (ItemRemoved != null)
				ItemRemoved(item, parent, isMoving);
		}
		protected internal virtual void OnNodeAdded(AListNode<K, T> child, AListInnerBase<K, T> parent, bool isMoving)
		{
			if (NodeAdded != null)
				NodeAdded(child, parent, isMoving);
		}
		protected internal virtual void OnNodeRemoved(AListNode<K, T> child, AListInnerBase<K, T> parent, bool isMoving)
		{
			if (NodeRemoved != null)
				NodeRemoved(child, parent, isMoving);
		}

		internal void Clear()
		{
			if (NodeRemoved != null || ItemRemoved != null)
			{
				throw new NotImplementedException("TODO: AListNodeObserver.Clear");
			}
			Root = null;
		}
		internal void AddingItems(ListSourceSlice<T> list, AListLeaf<K, T> parent, bool isMoving)
		{
			for (int i = 0; i < list.Count; i++)
				OnItemAdded(list[i], parent, isMoving);
		}
		internal void RemovingItems(InternalDList<T> list, int index, int count, AListLeaf<K, T> parent, bool isMoving)
		{
			for (int i = index; i < index + count; i++)
				OnItemRemoved(list[i], parent, isMoving);
		}

		public void ItemMoved(T item, AListLeaf<K, T> oldParent, AListLeaf<K, T> newParent)
		{
			OnItemRemoved(item, oldParent, true);
			OnItemAdded(item, newParent, true);
		}
		public void NodeMoved(AListNode<K, T> child, AListInnerBase<K, T> oldParent, AListInnerBase<K, T> newParent)
		{
			OnNodeRemoved(child, oldParent, true);
			OnNodeAdded(child, newParent, true);
		}

		internal void HandleChildReplaced(AListNode<K, T> oldNode, AListNode<K, T> newLeft, AListNode<K, T> newRight, AListInnerBase<K, T> parent)
		{
			HandleNodeReplaced(oldNode, newLeft, newRight);
			if (parent != null)
			{
				OnNodeRemoved(oldNode, parent, true);
				OnNodeAdded(newLeft, parent, true);
				if (newRight != null)
					OnNodeAdded(newRight, parent, true);
			}
		}

		public void HandleNodeReplaced(AListNode<K, T> oldNode, AListNode<K, T> newLeft, AListNode<K, T> newRight)
		{
			if (newRight == null)
			{	// cloned, not split
				Debug.Assert(oldNode.IsFrozen && !newLeft.IsFrozen);
				Debug.Assert(oldNode.LocalCount == newLeft.LocalCount);
			}
			if (oldNode is AListLeaf<K, T>)
			{
				RemovingItems((AListLeaf<K, T>)oldNode, true);
				AddingItems((AListLeaf<K, T>)newLeft, true);
				if (newRight != null)
					AddingItems((AListLeaf<K, T>)newRight, true);
			}
			else
			{
				RemovingItems((AListInnerBase<K, T>)oldNode, true);
				AddingItems((AListInnerBase<K, T>)newLeft, true);
				if (newRight != null)
					AddingItems((AListInnerBase<K, T>)newRight, true);
			}
		}

		private void AddingItems(AListLeaf<K, T> node, bool isMoving)
		{
			for (int i = 0; i < node.LocalCount; i++)
				OnItemAdded(node[(uint)i], node, isMoving);
		}
		private void RemovingItems(AListLeaf<K, T> node, bool isMoving)
		{
			for (int i = 0; i < node.LocalCount; i++)
				OnItemRemoved(node[(uint)i], node, isMoving);
		}
		public void AddingItems(AListInnerBase<K, T> node, bool isMoving)
		{
			for (int i = 0; i < node.LocalCount; i++)
				OnNodeAdded(node.Child(i), node, isMoving);
		}
		public void RemovingItems(AListInnerBase<K, T> node, bool isMoving)
		{
			for (int i = 0; i < node.LocalCount; i++)
				OnNodeRemoved(node.Child(i), node, isMoving);
		}
	}
}
