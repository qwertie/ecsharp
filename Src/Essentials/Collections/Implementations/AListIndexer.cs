using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Loyc.Collections.Impl
{
	public abstract class AListIndexerBase<T> : AListNodeObserver<T, T>
	{
		public AListIndexerBase(AListNode<T, T> root) : base(root) { }

		/// <summary>
		/// Used for sanity checks. Count should equal the number of items in the 
		/// tree, unless the root node is a leaf, in which case this class can return 
		/// 0 and refuse to keep track of any items.
		/// </summary>
		public abstract uint Count { get; }

		public abstract Iterator<AListLeaf<T, T>> PotentialLeavesFor(T item);
		public abstract AListInnerBase<T, T> FindParent(AListNode<T, T> child, out uint baseIndex);

		public uint FindBaseIndex(AListLeaf<T, T> leaf)
		{
			uint total = 0, baseIndex;
			AListNode<T, T> node = leaf, prev = leaf;
			for (node = leaf; (node = FindParent(node, out baseIndex)) != null; prev = node)
				total += baseIndex;
			Debug.Assert(prev == Root);
			return total;
		}
		public int IndexOf(T item)
		{
			AListLeaf<T, T> leaf;
			var leaves = PotentialLeavesFor(item);
			while (leaves.MoveNext(out leaf))
			{
				int subI = leaf.IndexOf(item, 0);
				if (subI != -1)
				{
					uint baseI = FindBaseIndex(leaf);
					return (int)baseI + subI;
				}
			}
			return -1;
		}
		public IEnumerator<uint> IndexesOf(T item, uint minIndex, uint maxIndex)
		{
			AListLeaf<T, T> leaf;
			var leaves = PotentialLeavesFor(item);
			while (leaves.MoveNext(out leaf))
			{
				int subI = leaf.IndexOf(item, 0);
				if (subI != -1)
				{
					uint baseI = FindBaseIndex(leaf);
					if (baseI + leaf.LocalCount < minIndex || baseI > maxIndex)
						continue; // leaf node is out of desired range
					do
					{
						uint index = baseI + (uint)subI;
						if (index >= minIndex && index <= maxIndex)
							yield return index;
					} while ((subI = leaf.IndexOf(item, subI + 1)) != -1);
				}
			}
		}
	}

	public class AListIndexer<T> : AListIndexerBase<T>
	{
		KeylessHashtable<AListLeaf<T, T>> _items;
		Dictionary<AListNode<T, T>, AListInnerBase<T, T>> _nodes;

		public AListIndexer(AListNode<T, T> root) : base(root) { Attach(root); }

		protected static void Verify(bool condition) { System.Diagnostics.Debug.Assert(condition); }

		/// <summary>Builds an index, given a tree root and the number of items in 
		/// the tree. Also sets <see cref="Root"/> to the given root node.</summary>
		/// <remarks>
		/// This must be the first method called after construction. It can be called
		/// again later to rebuild the index, if the tree has been rearranged (e.g. 
		/// sorted). The root is normally an inner node, because if the tree contains 
		/// only a leaf node then this class offers no performance benefit and should
		/// not be used.
		/// </remarks>
		public void Attach(AListNode<T, T> root)
		{
			uint count = 0;
			if (root != null)
			{
				count = root.TotalCount;
				if ((int)count < 0)
					throw new NotSupportedException("Indexing is not supported for AList with over 2.1 billion items");
			}
			_root = root;
			BuildIndex((int)(count + (count >> 1)));
		}
		private void BuildIndex(int capacity)
		{
			var root2 = Root as AListInnerBase<T, T>;

			if (root2 != null)
			{
				_items = KeylessHashtable<AListLeaf<T, T>>.New(capacity);
				_nodes = new Dictionary<AListNode<T, T>, AListInnerBase<T, T>>();
				BuildIndexHelper(root2);
			}
			else
			{
				_items = null;
				_nodes = null;
			}
		}
		private void BuildIndexHelper(AListInnerBase<T, T> inner)
		{
			for (int i = 0; i < inner.LocalCount; i++)
			{
				var child = inner.Child(i);
				OnNodeAdded(child, inner, false);
				var child2 = child as AListInnerBase<T, T>;
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
			for (; ; )
			{
				var item = it(ref ended);
				if (ended) break;
				OnItemAdded(item, leaf, false);
			}
		}

		public override AListNode<T, T> Root
		{
			set
			{
				_root = value;
				if ((_items == null) == (_root is AListInnerBase<T, T>))
					Attach(value);
			}
		}

		protected internal sealed override void OnItemAdded(T item, AListLeaf<T, T> parent, bool isMoving)
		{
			if (_items != null)
			{
				if (_items.Count >= _items.Capacity)
					Attach(_root);
				if (item == null)
					_items.Add((uint)0, parent);
				else
					_items.Add(item, parent);
			}
			base.OnItemAdded(item, parent, isMoving);
		}

		protected internal override void OnItemRemoved(T item, AListLeaf<T, T> parent, bool isMoving)
		{
			if (_items != null)
				Verify(_items.Remove(item, parent));
			base.OnItemRemoved(item, parent, isMoving);
		}

		protected internal override void OnNodeAdded(AListNode<T, T> child, AListInnerBase<T, T> parent, bool isMoving)
		{
			if (_nodes != null)
				_nodes.Add(child, parent);
			base.OnNodeAdded(child, parent, isMoving);
		}

		protected internal override void OnNodeRemoved(AListNode<T, T> child, AListInnerBase<T, T> parent, bool isMoving)
		{
			if (_nodes != null)
				Verify(_nodes.Remove(child));
			base.OnNodeRemoved(child, parent, isMoving);
		}

		public override uint Count { get { return _items == null ? 0 : (uint)_items.Count; } }

		public override Iterator<AListLeaf<T, T>> PotentialLeavesFor(T item)
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

		public override AListInnerBase<T, T> FindParent(AListNode<T, T> child, out uint baseIndex)
		{
			if (_nodes != null)
			{
				AListInnerBase<T, T> parent;
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
