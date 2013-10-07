using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Loyc.Collections.Impl
{
	/// <summary>Observes changes and builds a table of items in the tree.</summary>
	/// <remarks>
	/// The <see cref="IndexedAList{T}"/> class uses one of these objects to speed
	/// up methods that search for items in an <see cref="AList{T}"/> (IndexOf,
	/// Contains, and Remove). The amount of speedup is limited by the size of
	/// the nodes in the list being indexed; see <see cref="IndexOfAny"/>.
	/// <para/>
	/// It is wasteful to use an AListIndexer if the list is small. AListIndexer
	/// is designed to accelerate searches in very large lists, and it offers no
	/// performance benefit to small lists; to the contrary, it just wastes time
	/// and memory in small lists.
	/// <para/>
	/// It is recommended to use IndexedAList instead of instantiating this class 
	/// directly.
	/// <para/>
	/// In general, AListIndexer requires more memory than the list that is being
	/// indexed. Specifically, if pointers use P bytes, then AListIndexer itself
	/// consumes moderately MORE than X+P*N bytes of memory, where X is the size 
	/// of the list being indexed, and N is the number of items in the list. Thus,
	/// for example, an indexed list of <see cref="AList{object}"/> requires 
	/// approximately three times as much memory as an AList that is not indexed.
	/// <para/>
	/// Moreover, changing an indexed list takes at least twice as much time, since 
	/// the indexer must be notified of each change and updates to the index take
	/// O(log N) time per update. Batch operations involving X items that take 
	/// O(log N) time without an indexer (e.g. RemoveRange(i, X)) will take 
	/// O(X log N) time instead, because the indexer must be notified about each 
	/// item changed.
	/// <para/>
	/// Still, these costs are worthwhile in applications that frequently search
	/// for items in the list.
	/// </remarks>
	public class AListIndexer<K, T> : IAListTreeObserver<K, T>
	{
		BMultiMap<T, AListLeaf<K, T>> _items;
		BMultiMap<AListNode<K,T>, AListInnerBase<K, T>> _nodes;
		AListNode<K, T> _root;

		// This is not a valid comparison function for a normal dictionary, 
		// because two unrelated objects can have the same hashcode. However,
		// we have no other way to construct an ordering function for an
		// arbitrary item type "T" and for AListNodes. Using the hashcodes
		// will work OK, provided that we 
		// (1) store the objects in a collection that allows duplicates
		//     (i.e. BMultiMap), and
		// (2) are careful to handle the situation where we search for one 
		//     object and find an unrelated object instead (or in addition).
		static int CompareHashCodes<X>(X a, X b)
		{
			return a.GetHashCode().CompareTo(b.GetHashCode());
		}
		static bool Equals(T a, T b)
		{
			return a == null ? b == null : a.Equals(b);
		}

		protected static Func<T, T, int>                                       CompareTHashCodes = CompareHashCodes<T>;
		protected static Func<AListNode<K, T>,      AListNode<K, T>,      int> CompareNodeHashCodes = CompareHashCodes<AListNode<K, T>>;
		protected static Func<AListLeaf<K, T>,      AListLeaf<K, T>,      int> CompareLeafHashCodes = CompareHashCodes<AListNode<K, T>>;
		protected static Func<AListInnerBase<K, T>, AListInnerBase<K, T>, int> CompareInnerHashCodes = CompareHashCodes<AListInnerBase<K, T>>;

		public AListIndexer()
		{
			_items = new BMultiMap<T, AListLeaf<K, T>>(CompareTHashCodes, CompareLeafHashCodes);
		}

		void BadState()
		{
			BadState("AListIndexer is inconsistent with the list it is attached to.");
		}
		void BadState(string msg) 
		{
			throw new InvalidStateException(msg);
		}

		public void Attach(AListBase<K, T> list, Action<bool> populate)
		{
			populate(true);
		}
		public void Detach()
		{
			RootChanged(null, true);
		}
		public void RootChanged(AListNode<K, T> newRoot, bool clear)
		{
			if (newRoot == null)
			{
				if (!clear && _items.Count != 0)
					BadState();
				_items.Clear();
				_nodes = null;
			}
			_root = newRoot;
		}
		
		public void ItemAdded(T item, AListLeaf<K, T> parent)
		{
			_items.Add(new KeyValuePair<T,AListLeaf<K,T>>(item, parent));
		}
		public void ItemRemoved(T item, AListLeaf<K, T> parent)
		{
			int index = _items.IndexOfExact(new KeyValuePair<T, AListLeaf<K, T>>(item, parent));
			if (index <= -1) BadState();
			_items.RemoveAt(index);
		}
		public void NodeAdded(AListNode<K, T> child, AListInnerBase<K, T> parent)
		{
			if (_nodes == null)
				_nodes = new BMultiMap<AListNode<K, T>, AListInnerBase<K, T>>(CompareNodeHashCodes, CompareInnerHashCodes);
			_nodes.Add(new KeyValuePair<AListNode<K,T>,AListInnerBase<K,T>>(child, parent));
		}
		public void NodeRemoved(AListNode<K, T> child, AListInnerBase<K, T> parent)
		{
			int index = _nodes.IndexOfExact(new KeyValuePair<AListNode<K, T>, AListInnerBase<K, T>>(child, parent));
			if (index <= -1) BadState();
			_nodes.RemoveAt(index);
		}

		public void RemoveAll(AListNode<K, T> node)
		{
			var inner = node as AListInnerBase<K, T>;
			if (inner != null)
				for (int i = 0; i < inner.LocalCount; i++)
					NodeRemoved(inner.Child(i), inner);
			else {
				var leaf = (AListLeaf<K, T>)node;
				for (int i = 0; i < leaf.LocalCount; i++)
					ItemRemoved(leaf[(uint)i], leaf);
			}
		}
		public void AddAll(AListNode<K, T> node)
		{
			var inner = node as AListInnerBase<K, T>;
			if (inner != null)
				for (int i = 0; i < inner.LocalCount; i++)
					NodeAdded(inner.Child(i), inner);
			else {
				var leaf = (AListLeaf<K, T>)node;
				for (int i = 0; i < leaf.LocalCount; i++)
					ItemAdded(leaf[(uint)i], leaf);
			}
		}

		public int ItemCount
		{
			get { return _items.Count; }
		}

		public void CheckPoint()
		{
			Debug.Assert(_items.Count == (_root == null ? 0 : _root.TotalCount));
		}

		/// <summary>Returns an index at which the specified item can be found.</summary>
		/// <param name="item">Item to find.</param>
		/// <returns>The index of the item in the list being indexed by this 
		/// object, or -1 if the item does not exist in the list.</returns>
		/// <remarks>
		/// The search takes O(M log^2 N) time, where N is the size of the list 
		/// and M is the maximum size of nodes in the list. Due to the "M" factor,
		/// A-lists with large nodes are searched more slowly than A-lists with 
		/// small nodes; however, the "log N" part is a base-M logarithm, so you
		/// don't actually gain performance by using very small nodes. This is
		/// because very small nodes require deeply nested trees, and deep trees 
		/// are slow. The <see cref="AListBase{K,T}"/> documentation discusses 
		/// the effect of node size further.
		/// </remarks>
		public int IndexOfAny(T item)
		{
			AListLeaf<K, T> leaf;
			bool found;

			_items.FindLowerBoundExact(ref item, out leaf, out found);
			if (!found)
				return -1;
			return ReconstructIndex(item, leaf);
		}

		public List<int> IndexesOf(T item)
		{
			AListLeaf<K, T> leaf;
			bool found;
			int i = _items.FindLowerBoundExact(ref item, out leaf, out found);
			if (!found)
				return null;

			var list = new List<int>();
			list.Add(ReconstructIndex(item, leaf));

			object searchFor = item;
			KeyValuePair<T,AListLeaf<K,T>> kvp;
			for(;;) {
				i++;
				if (i >= _items.Count)
					break;
				kvp = _items[i];
				if (CompareHashCodes(kvp.Key, item) != 0)
					break;
				if (kvp.Key.Equals(searchFor))
					list.Add(ReconstructIndex(kvp.Key, kvp.Value));
			}

			return list;
		}

		/// <summary>Given an item and a leaf that is known to contain a copy of 
		/// the item, this method returns the index of the item in the tree as 
		/// a whole. Requires O(M )</summary>
		protected int ReconstructIndex(T item, AListLeaf<K, T> leaf)
		{
			AListInnerBase<K, T> inner;
			AListNode<K, T> node;
			bool found;

			int index = leaf.IndexOf(item, 0), localIndex;
			if (index <= -1)
				BadState();

			node = leaf;
			while (node != _root)
			{
				Debug.Assert(node != null);
				_nodes.FindLowerBoundExact(ref node, out inner, out found);
				if (!found)
					BadState();
				if ((localIndex = (int)inner.BaseIndexOf(node)) <= -1)
					BadState();
				node = inner;
				index += localIndex;
			}

			return index;
		}

		/// <summary>Scans the index to verify that it matches the tree that is 
		/// being indexed. The scan takes O(N log N + N M) time for a list of 
		/// length N with maximum node size M.</summary>
		/// <exception cref="InvalidStateException">
		/// The index is out of sync with the tree. 
		/// <para/>
		/// This could indicate a bug somewhere in the A-list code, but it could
		/// also be caused by other rogue code, such as items that change their
		/// sort order or hashcode after being added to the collection, an observer
		/// that has thrown exceptions when it's not allowed to, or buggy 
		/// multithreading (modifying a list from two threads at once).
		/// </exception>
		/// <remarks>
		/// Tree observability is a difficult feature to implement correctly, so 
		/// this method is called a lot in unit tests to help work out the bugs.
		/// </remarks>
		public void VerifyCorrectness()
		{
			StringBuilder e = null; // error message

			if (_items.Count != (_root == null ? 0 : _root.TotalCount))
				AddError(ref e, "Recorded # of items ({0}) differs from actual list Count ({1}).", _items.Count, _root.TotalCount);

			foreach (var pair in _items)
			{
				var leaf = pair.Value;
				if (leaf.IndexOf(pair.Key, 0) <= -1)
					AddError(ref e, "Outdated record: leaf {0:X} no longer contains item '{1}'.", leaf.GetHashCode() & 0xFFFF, pair.Key);
				if (leaf != _root && _nodes.IndexOfExact(leaf) <= -1)
					AddError(ref e, "Leaf {0:X} has no known parent but is not the root.", leaf.GetHashCode() & 0xFFFF);
			}
			
			int totalChildren = 0;
			foreach (var pair in _nodes)
			{
				var child = pair.Key;
				var parent = pair.Value;
				if (parent.IndexOf(child) <= -1)
					AddError(ref e, "Outdated record: inner {0:X} no longer contains node {1:X}.", parent.GetHashCode() & 0xFFFF, child.GetHashCode() & 0xFFFF);
				if (parent != _root && _nodes.IndexOfExact(parent) <= -1)
					AddError(ref e, "Inner {0:X} has no known parent but is not the root.", parent.GetHashCode() & 0xFFFF);
				var leaf = child as AListLeaf<K, T>;
				if (leaf != null)
					for (uint i = 0; i < leaf.LocalCount; i++)
						if (_items.IndexOfExact(leaf[i]) <= -1)
							AddError(ref e, "Leaf {0:X} contains non-indexed item '{1}'.", leaf.GetHashCode() & 0xFFFF, leaf[i]);
				totalChildren += pair.Key.LocalCount;
			}
			if (totalChildren + _root.LocalCount != _items.Count + _nodes.Count)
				AddError(ref e, "Computed count {0}+{1} != indexed count {2}+{3}.", totalChildren, _root.LocalCount, _items.Count, _nodes.Count);
			
			if (e != null)
				BadState(e.ToString());
		}
		void AddError(ref StringBuilder sb, string fmt, params object[] args)
		{
			if (sb == null)
				sb = new StringBuilder("AListIndexer is inconsistent with the list it is attached to.");
			if (sb.Length < 1000)
			{
				if (args == null)
					sb.Append(fmt);
				else
					sb.AppendFormat(fmt, args);
				
				if (sb.Length > 1000)
				{
					sb.Remove(1000, sb.Length - 1000);
					sb.Append("...");
				}
			}
		}
	}


	/*
	public abstract class AListIndexerBase<T> : AListTreeObserverMgr<T, T>
	{
		public AListIndexerBase(AList<T> list, AListNode<T, T> root) : base(list, root, null) { }

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
			Debug.Assert(prev == _root);
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

	public class AListIndexer<T> : AListIndexerBase<T>, IAListTreeObserver<T, T>
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
			var root2 = _root as AListInnerBase<T, T>;

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
				NodeAdded(child, inner, false);
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
				ItemAdded(item, leaf, false);
			}
		}

		public new void ItemAdded(T item, AListLeaf<T, T> parent, bool isMoving)
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
			base.ItemAdded(item, parent, isMoving);
		}

		public new void ItemRemoved(T item, AListLeaf<T, T> parent, bool isMoving)
		{
			if (_items != null)
				Verify(_items.Remove(item, parent));
			base.ItemRemoved(item, parent, isMoving);
		}

		public new void NodeAdded(AListNode<T, T> child, AListInnerBase<T, T> parent, bool isMoving)
		{
			if (_nodes != null)
				_nodes.Add(child, parent);
			base.NodeAdded(child, parent, isMoving);
		}

		public new void NodeRemoved(AListNode<T, T> child, AListInnerBase<T, T> parent, bool isMoving)
		{
			if (_nodes != null)
				Verify(_nodes.Remove(child));
			base.NodeRemoved(child, parent, isMoving);
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
			Debug.Assert(child == _root);
			baseIndex = 0;
			return null;
		}
	}*/
}
