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
	/// for example, an indexed list of <see cref="AList{Object}"/> requires 
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
	[Serializable]
	public class AListIndexer<K, T> : IAListTreeObserver<K, T>
	{
		BMultiMap<T, AListLeafBase<K, T>> _items;
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
		protected static Func<AListLeafBase<K, T>,  AListLeafBase<K, T>,  int> CompareLeafHashCodes = CompareHashCodes<AListNode<K, T>>;
		protected static Func<AListInnerBase<K, T>, AListInnerBase<K, T>, int> CompareInnerHashCodes = CompareHashCodes<AListInnerBase<K, T>>;

		public AListIndexer()
		{
			_items = new BMultiMap<T, AListLeafBase<K, T>>(CompareTHashCodes, CompareLeafHashCodes);
		}

		void BadState()
		{
			BadState("AListIndexer is inconsistent with the list it is attached to.");
		}
		void BadState(string msg) 
		{
			throw new InvalidStateException(msg);
		}

		public bool? Attach(AListBase<K, T> list) => true;
		public void Detach(AListBase<K,T> list, AListNode<K, T> root) => RootChanged(list, null, true);

		public void RootChanged(AListBase<K, T> list, AListNode<K, T> newRoot, bool clear)
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
		
		public void ItemAdded(T item, AListLeafBase<K, T> parent)
		{
			_items.Add(new KeyValuePair<T,AListLeafBase<K,T>>(item, parent));
		}
		public void ItemRemoved(T item, AListLeafBase<K, T> parent)
		{
			int index = _items.IndexOfExact(new KeyValuePair<T, AListLeafBase<K, T>>(item, parent));
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
				var leaf = (AListLeafBase<K, T>)node;
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
				var leaf = (AListLeafBase<K, T>)node;
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
			AListLeafBase<K, T> leaf;
			bool found;

			_items.FindLowerBoundExact(ref item, out leaf, out found);
			if (!found)
				return -1;
			return ReconstructIndex(item, leaf);
		}

		public List<int> IndexesOf(T item)
		{
			AListLeafBase<K, T> leaf;
			bool found;
			int i = _items.FindLowerBoundExact(ref item, out leaf, out found);
			if (!found)
				return null;

			var list = new List<int>();
			list.Add(ReconstructIndex(item, leaf));

			object searchFor = item;
			KeyValuePair<T,AListLeafBase<K,T>> kvp;
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
		protected int ReconstructIndex(T item, AListLeafBase<K, T> leaf)
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
			
			if (_nodes == null) {
				if (!_root.IsLeaf)
					AddError(ref e, "AListIndexer is unaware that the AList is a tree");
				if (_root.LocalCount != _items.Count)
					AddError(ref e, "Count {0} != indexed count {1}.", _root.LocalCount, _items.Count);
			} else {
				int totalChildren = 0;
				foreach (var pair in _nodes)
				{
					var child = pair.Key;
					var parent = pair.Value;
					if (parent.IndexOf(child) <= -1)
						AddError(ref e, "Outdated record: inner {0:X} no longer contains node {1:X}.", parent.GetHashCode() & 0xFFFF, child.GetHashCode() & 0xFFFF);
					if (parent != _root && _nodes.IndexOfExact(parent) <= -1)
						AddError(ref e, "Inner {0:X} has no known parent but is not the root.", parent.GetHashCode() & 0xFFFF);
					var leaf = child as AListLeafBase<K, T>;
					if (leaf != null)
						for (uint i = 0; i < leaf.LocalCount; i++)
							if (_items.IndexOfExact(leaf[i]) <= -1)
								AddError(ref e, "Leaf {0:X} contains non-indexed item '{1}'.", leaf.GetHashCode() & 0xFFFF, leaf[i]);
					totalChildren += pair.Key.LocalCount;
				}
				if (totalChildren + _root.LocalCount != _items.Count + _nodes.Count)
					AddError(ref e, "Computed count {0}+{1} != indexed count {2}+{3}.", totalChildren, _root.LocalCount, _items.Count, _nodes.Count);
			}
			
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

		/// <summary>Counts memory used by the index itself (not including the AList nodes)</summary>
		public long CountMemory(int sizeOfElement) =>
			IntPtr.Size * 5 + _items.CountSizeInBytes(sizeOfElement + IntPtr.Size) + 
			                 (_nodes?.CountSizeInBytes(IntPtr.Size * 2) ?? 0);
	}
}
