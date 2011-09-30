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
	public abstract class AListIndexerBase<T>
	{
		/// <summary>Builds an index, given a tree root and the number of items in 
		/// the tree. Also sets <see cref="Root"/> to the given root node.</summary>
		/// <remarks>
		/// This must be the first method called after construction. It can be called
		/// again later to rebuild the index, if the tree has been rearranged (e.g. 
		/// sorted). The root is normally an inner node, because if the tree contains 
		/// only a leaf node then this class offers no performance benefit and should
		/// not be used.
		/// </remarks>
		public abstract void BuildIndex(AListNode<T> root);

		protected AListNode<T> _root;
		/// <summary>Root node of the tree, kept up-to-date by AList.</summary>
		public virtual AListNode<T> Root { get { return _root; } set { _root = value; } }

		public abstract void ItemAdded(T item, AListLeaf<T> parent);
		public abstract void ItemRemoved(T item, AListLeaf<T> parent);
		public abstract void NodeAdded(AListNode<T> child, AListInner<T> parent);
		public abstract void NodeRemoved(AListNode<T> child, AListInner<T> parent);


		/// <summary>
		/// Used for sanity checks. Count should equal the number of items in the 
		/// tree, unless the root node is a leaf, in which case this class can return 
		/// 0 and refuse to keep track of any items.
		/// </summary>
		public abstract uint Count { get; }
		
		public abstract Iterator<AListLeaf<T>> PotentialLeavesFor(T item);
		public abstract AListInner<T> FindParent(AListNode<T> child, out uint baseIndex);
		
		public uint FindBaseIndex(AListLeaf<T> leaf)
		{
			uint total = 0, baseIndex;
			AListNode<T> node = leaf, prev = leaf;
			for (node = leaf; (node = FindParent(node, out baseIndex)) != null; prev = node)
				total += baseIndex;
			Debug.Assert(prev == Root);
			return total;
		}
		public int IndexOf(T item)
		{
			AListLeaf<T> leaf;
			var leaves = PotentialLeavesFor(item);
			while (leaves.MoveNext(out leaf))
			{
				int subI = leaf.IndexOf(item, 0);
				if (subI != -1) {
					uint baseI = FindBaseIndex(leaf);
					return (int)baseI + subI;
				}
			}
			return -1;
		}
		public IEnumerator<uint> IndexesOf(T item, uint minIndex, uint maxIndex)
		{
			AListLeaf<T> leaf;
			var leaves = PotentialLeavesFor(item);
			while (leaves.MoveNext(out leaf))
			{
				int subI = leaf.IndexOf(item, 0);
				if (subI != -1)
				{
					uint baseI = FindBaseIndex(leaf);
					if (baseI + leaf.LocalCount < minIndex || baseI > maxIndex)
						continue; // leaf node is out of desired range
					do {
						uint index = baseI + (uint)subI;
						if (index >= minIndex && index <= maxIndex)
							yield return index;
					} while ((subI = leaf.IndexOf(item, subI+1)) != -1);
				}
			}
		}

		internal void AddingItems(ListSourceSlice<T> list, AListLeaf<T> parent)
		{
			for (int i = 0; i < list.Count; i++)
				ItemAdded(list[i], parent);
		}
		internal void RemovingItems(InternalDList<T> list, int index, int count, AListLeaf<T> parent)
		{
			for (int i = index; i < index + count; i++)
				ItemRemoved(list[i], parent);
		}

		public void ItemMoved(T item, AListLeaf<T> oldParent, AListLeaf<T> newParent)
		{
			ItemRemoved(item, oldParent);
			ItemAdded(item, newParent);
		}
		public void NodeMoved(AListNode<T> child, AListInner<T> oldParent, AListInner<T> newParent)
		{
			NodeRemoved(child, oldParent);
			NodeAdded(child, newParent);
		}

		internal void HandleChildReplaced(AListNode<T> oldNode, AListNode<T> newLeft, AListNode<T> newRight, AListInner<T> parent)
		{
			HandleNodeReplaced(oldNode, newLeft, newRight);
			if (parent != null)
			{
				NodeRemoved(oldNode, parent);
				NodeAdded(newLeft, parent);
				if (newRight != null)
					NodeAdded(newRight, parent);
			}
		}

		public void HandleNodeReplaced(AListNode<T> oldNode, AListNode<T> newLeft, AListNode<T> newRight)
		{
			if (newRight == null)
			{	// cloned, not split
				Debug.Assert(oldNode.IsFrozen && !newLeft.IsFrozen);
				Debug.Assert(oldNode.LocalCount == newLeft.LocalCount);
			}
			if (oldNode is AListLeaf<T>)
			{
				RemovingItems((AListLeaf<T>)oldNode);
				AddingItems((AListLeaf<T>)newLeft);
				if (newRight != null)
					AddingItems((AListLeaf<T>)newRight);
			}
			else
			{
				RemovingItems((AListInner<T>)oldNode);
				AddingItems((AListInner<T>)newLeft);
				if (newRight != null)
					AddingItems((AListInner<T>)newRight);
			}
		}

		private void AddingItems(AListLeaf<T> node)
		{
			for (int i = 0; i < node.LocalCount; i++)
				ItemAdded(node[(uint)i], node);
		}
		private void RemovingItems(AListLeaf<T> node)
		{
			for (int i = 0; i < node.LocalCount; i++)
				ItemRemoved(node[(uint)i], node);
		}
		public void AddingItems(AListInner<T> node)
		{
			for (int i = 0; i < node.LocalCount; i++)
				NodeAdded(node.Child(i), node);
		}
		public void RemovingItems(AListInner<T> node)
		{
			for (int i = 0; i < node.LocalCount; i++)
				NodeRemoved(node.Child(i), node);
		}
	}
	
	public class AListIndexer<T> : AListIndexerBase<T>
	{
		KeylessHashtable<AListLeaf<T>> _items;
		Dictionary<AListNode<T>, AListInner<T>> _nodes;

		protected static void Verify(bool condition) { System.Diagnostics.Debug.Assert(condition); }

		public AListIndexer() { }

		public override void BuildIndex(AListNode<T> root)
		{
			uint count = 0;
			if (root != null) {
				count = root.TotalCount;
				if ((int)count < 0)
					throw new NotSupportedException("Indexing is not supported for AList with over 2.1 billion items");
			}
			_root = root;
			BuildIndex((int)(count + (count >> 1)));
		}
		private void BuildIndex(int capacity)
		{
			var root2 = Root as AListInner<T>;

			if (root2 != null) {
				_items = KeylessHashtable<AListLeaf<T>>.New(capacity);
				_nodes = new Dictionary<AListNode<T>, AListInner<T>>();
				BuildIndexHelper(root2);
			} else {
				_items = null;
				_nodes = null;
			}
		}
		private void BuildIndexHelper(AListInner<T> inner)
		{
			for (int i = 0; i < inner.LocalCount; i++)
			{
				var child = inner.Child(i);
				NodeAdded(child, inner);
				var child2 = child as AListInner<T>;
				if (child2 != null)
					BuildIndexHelper(child2);
				else
					BuildIndexHelper((AListLeaf<T>)child);
			}
		}
		private void BuildIndexHelper(AListLeaf<T> leaf)
		{
			bool ended = false;
			var it = leaf.GetIterator(0, leaf.LocalCount);
			for (;;)
			{
				var item = it(ref ended);
				if (ended) break;
				ItemAdded(item, leaf);
			}
		}

		public override AListNode<T> Root
		{ 
			set {
				_root = value;
				if ((_items == null) == (_root is AListInner<T>))
					BuildIndex(value);
			}
		}

		public sealed override void ItemAdded(T item, AListLeaf<T> parent)
		{
			if (_items != null)
			{
				if (_items.Count >= _items.Capacity)
					BuildIndex(_root);
				if (item == null)
					_items.Add((uint)0, parent);
				else
					_items.Add(item, parent);
			}
		}

		public override void ItemRemoved(T item, AListLeaf<T> parent)
		{
			if (_items != null)
				Verify(_items.Remove(item, parent));
		}

		public override void NodeAdded(AListNode<T> child, AListInner<T> parent)
		{
			if (_nodes != null)
				_nodes.Add(child, parent);
		}

		public override void NodeRemoved(AListNode<T> child, AListInner<T> parent)
		{
			if (_nodes != null)
				Verify(_nodes.Remove(child));
		}

		public override uint Count { get { return _items == null ? 0 : (uint)_items.Count; } }

		public override Iterator<AListLeaf<T>> PotentialLeavesFor(T item)
		{
			if (_items != null)
			{
				if (item != null)
					return _items.Find(item);
				else
					return _items.Find((uint)0);
			}
			else if (_root != null)
				return Iterator.Single((AListLeaf<T>)Root);
			else
				return Iterator.Empty<AListLeaf<T>>();
		}

		public override AListInner<T> FindParent(AListNode<T> child, out uint baseIndex)
		{
			if (_nodes != null) {
				AListInner<T> parent;
				if (_nodes.TryGetValue(child, out parent))
				{
					baseIndex = parent.BaseIndexOf(child);
					return parent;
				}
			}
			Debug.Assert(child == Root);
			baseIndex = 0;
			return null;
		}
	}
}
