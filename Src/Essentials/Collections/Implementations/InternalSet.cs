// Author: David Piepgrass
// License: LGPL
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Math;

namespace Loyc.Collections.Impl
{
#if true
	/// <summary>A hash-trie data structure for use inside other data structures.</summary>
	/// <remarks>
	/// <see cref="InternalSet{T}"/> is a dual-mode mutable/immutable "hash trie",
	/// which is a kind of tree that is built from the hashcode of the items it 
	/// contains. It supports fast cloning, and is suitable as a persistent data
	/// structure.
	/// <para/>
	/// InternalSet&lt;T> is not designed to be used by itself, but as a building
	/// block for other data structures. It has no Count property because it does 
	/// not know its own size; the outer data structure must track the size if 
	/// the size is needed. The lack of a Count property allows an empty 
	/// InternalSet to use a mere single word of memory!
	/// <para/>
	/// This is my second implementation of InternalSet. The original version
	/// used memory very efficiently for reference types, but required boxing for
	/// value types; this version needs more memory, but is moderately faster in
	/// most cases and supports value types without boxing. I estimate that 
	/// InternalSet (the second version) uses roughly the same amount of memory as 
	/// <see cref="HashSet{T}"/> (actually more or less depending on the number of 
	/// items in the set, and on the hashcode distribution.)
	/// <para/>
	/// Collection classes based on InternalSet are most efficient for small sets,
	/// but if you always need small sets then a simple wrapper around HashSet 
	/// would suffice. In fact, despite my best efforts, this data type rarely 
	/// outperforms HashSet, but this is because <see cref="HashSet{T}"/> is quite
	/// fast, not because <see cref="InternalSet{T}"/> is slow. Still, there are
	/// several reasons to consider using a collection class based on InternalSet
	/// instead of <see cref="HashSet{T}"/>:
	/// <ul>
	/// <li>All of my set collections offer read-only variants. You can instantly 
	/// convert any mutable set or dictionary into an immutable one, and convert 
	/// any immutable set or dictionary back into a mutable one in O(1) time; 
	/// this relies on the same fast-cloning technique I developed for 
	/// <see cref="AList{T}"/>*.</li>
	/// <li>All of my set collections offer set operators that combine or intersect
	/// two sets without modifying the source sets ("|" for union, "&" for
	/// intersection, "-" for subtraction); these operators are available on both
	/// the mutable and immutable versions of the sets.</li>
	/// <li><see cref="InternalSet{T}"/> supports combined "get-and-replace" and 
	/// "get-and-remove" operations, which is mainly useful when it is being used
	/// as a dictionary (i.e. when T is a key-value pair). There are two "Add" 
	/// modes, "add if not present" and "add or replace"; both modes retrieve the 
	/// existing value if the key was already present, and the "add or replace" 
	/// mode furthermore changes the value. Also, when removing an item, you can
	/// get the value that was removed.</li>
	/// <li><see cref="InternalSet{T}"/>'s enumerator allows you to change or
	/// delete the current value (this feature is used internally by set operations
	/// such as <see cref="UnionWith"/> and <see cref="IntersectWith"/>).</li>
	/// <li><see cref="InternalSet{T}"/> was inspired by Clojure's PersistentHashMap,
	/// or rather by Karl Krukow's blog posts about PersistentHashMap**, and so it
	/// is designed so that you can use it as a fully persistent set, which means
	/// that you can keep a copy of every old version of the set that has ever 
	/// existed, if you want. The "+" and "-" operators (provided on the wrapper 
	/// classes, not on <see cref="InternalSet{T}"/> itself) allow you to add or 
	/// remove a single item without modifying the original set. There is a 
	/// substantial performance penalty for overusing these operators, but these
	/// operators are cheaper than duplicating a <see cref="HashSet<T>"/> every 
	/// time you modify it.</li>
	/// </ul>
	/// * After developing <see cref="AList{T}"/> and Loyc trees, I realized that
	///   freezable classes are error-prone, because it is sometimes difficult for
	///   a developer to figure out (before run-time) whether a given object could
	///   be frozen. If an object is frozen and you modify it, the compiler will
	///   never detect your mistake in advance and warn you. The collections based 
	///   on <see cref="InternalSet{T}"/> fix this problem by having separate data 
	///   types for frozen and unfrozen (a.k.a. immutable and mutable) collections.<br/>
	/// ** http://blog.higher-order.net/2010/08/16/assoc-and-clojures-persistenthashmap-part-ii/
	/// <para/>
	/// InternalSet is not efficient for Ts that are expensive to compare; unlike 
	/// standard .NET collections, this data structure does not store the hashcode 
	/// of each item inside the collection. The memory saved by not storing the 
	/// hashcode compensates for the extra memory required by the fact that an 
	/// InternalSet consists of many small nodes, instead of a single large array
	/// like <see cref="HashSet"/> uses.
	/// <para/>
	/// As I was saying, this data structure is inspired by Clojure's 
	/// PersistentHashMap. Whereas PersistentHashMap uses nodes of size 32, I chose 
	/// to use nodes of size 16 in order to increase space efficiency for small 
	/// sets; for some reason I tend to design programs that use many small 
	/// collections and a few big ones, so I tend to prefer designs that stay 
	/// efficient at small sizes.
	/// <para/>
	/// So InternalSet is a tree of nodes, with each level of the tree 
	/// representing 4 bits of the hashcode. Slots in the root node are selected
	/// based on bits 0 to 3 of the hashcode, slots in children of the root are
	/// selected based on bits 4 to 7 of the hashcode, and so forth. I'd draw
	/// you a diagram, but I can't.
	/// <para/>
	/// Technically, this data structure has O(log N) time complexity for search,
	/// insertion and removal. However, it's a base-16 logarithm and maxes out at
	/// 8 levels, so it is faster than typical O(log N) algorithms that are 
	/// base-2. At smaller sizes, its speed is similar to a conventional hashtable,
	/// and some operations are still efficient at large sizes, too.
	/// <para/>
	/// Unlike <see cref="InternalList{T}"/>, <c>new InternalSet&lt;T>()</c> is a 
	/// valid empty set. Moreover, because the root node is never changed after
	/// it is created (unless you modify it while it is frozen), it is safe to 
	/// make copies of an <see cref="InternalSet{T}"/> provided that you call
	/// <see cref="Thaw()"/> first; see that method for details.
	/// <para/>
	/// The neatest feature of this data structure is fast cloning and subtree 
	/// sharing. You can call <see cref="CloneFreeze"/> to freeze/clone the trie 
	/// in O(1) time; this freezes the root node (a transitive property that 
	/// implicitly affects all children), but still permits the hashtrie to be 
	/// modified by copying nodes on-demand. Thus the trie is actually frozen, but 
	/// copy-on-write behavior provides the illusion that it is still editable.
	/// <para/>
	/// This data structure is designed to support classes that contain mutable
	/// data, so that it can be used to construct dictionaries; that is, it allows
	/// T values that have an immutable "key" part and a mutable "value" part.
	/// Call <see cref="Find"/> to retrieve the value associated with a key, and
	/// call <see cref="Add"/> with replaceIfPresent=true to change the "value" 
	/// associated with a key. Of course, this feature has limited utility given
	/// that T should be a reference type, but I'm thinking about making a 
	/// variation on this data structure, later, that supports value types better.
	/// <para/>
	/// <b>How it works</b>: I call this data structure a "hash-trie" because it
	/// blends properties of hashtables and tries. It places items into a tree 
	/// by taking their hashcode and dividing it into 8 groups of 4 bits, starting 
	/// at the least significant bits. Each group of 4 bits is used to select a
	/// location in the tree/trie, and each node of the tree always has 16 items. 
	/// For example, consider a tree with 7 items that have the following hash 
	/// codes:
	/// <para/>
	/// - J: 0x89BC98B1 <br/>
	/// - K: 0xB173A12C <br/>
	/// - L: 0x20913491 <br/>
	/// - M: 0x1977FEB3 <br/>
	/// - N: 0x01299451 <br/>
	/// - O: 0x0732AF01 <br/>
	/// - P: 0x0732AF01 (Note: O.Equals(P)==false, but the hashcodes are equal)
	/// <para/>
	/// The top level of the trie represents the lowest 4 bits of the hashcode.
	/// Since each node has 16 items, 7 items can usually fit in a single node, 
	/// but in this case there are too many hashcodes that end with "1", causing
	/// a node split:
	/// <pre>
	///                  |0|1|2|3|4|5|6|7|8|9|A|B|C|D|E|F|
	///        _root ==> | |*| |M| | | | | | | | |K| | | |
	///       
	/// * child node ==> |0|1|2|3|4|5|6|7|8|9|A|B|C|D|E|F|
	///                  |O|P| | | |N| | | |L| |J| | | | |
	/// </pre>
	/// The second level of the trie represents bits 4-7, which is the second-
	/// last hex digit. You can see, for example, that the second-last digit of 
	/// N is 5, therefore N is stored at index 5 of the child node.
	/// <para/>
	/// In case hashcodes of different objects collide at a particular digit,
	/// adjacent array elements can be used to hold the different objects that
	/// share the same 4-bit sub-hashcode; this is a bounded-time variation on 
	/// the linearly-probed hashtable. In this example, both O and P have 
	/// zero as their second-last digit. Assuming O is added first, it takes
	/// slot [0]; then P takes slot [1]. Up to 3 adjacent entries can be used
	/// for a given hashcode; therefore, when searching for an entry it is 
	/// necessary to search 4 locations in each node: the preferred location,
	/// plus 3 adjacent locations.
	/// <para/>
	/// For example, support we search for an item X that is not in the set
	/// and has hashcode 0xCCA9A241. In that case, the Find methods starts with 
	/// the least-significant digit, 1. This points us to the child slot; an 
	/// invariant of our hashtrie is that if there is a child node, all items 
	/// with the corresponding sub-hashcode must be placed in the child node. 
	/// Therefore it is impossible, for example, that X could be located at 
	/// index 2 of the root node; the existence of the child node guarantees
	/// that it is not there. So the Find method looks inside the child node,
	/// at index 4 (the second-last digit of X's hashcode) and finds nothing.
	/// It also looks at indexes 5, 6, and 7, comparing N to X in the process.
	/// Since none of these slots contain X, the Find method returns false.
	/// <para/>
	/// Something unfortunate happens if five or more objects have the same 
	/// hashcode: it forces the tree to have maximum depth. Since a particular
	/// hashcode can only be repeated four times in a single node, upon adding
	/// a fifth item with the same hashcode, child nodes are created for all
	/// 8 digits of the hashcode. At the 8th level, the set's behavior changes:
	/// instead of allowing 4 slots for a single hashcode, it allows any slot
	/// to be used for any hashcode. Thus, searching for an item at the 8th
	/// level requires comparison with all 16 slots if the item is not in the
	/// set; to avoid this problem, use a better hash function that does not
	/// create false collisions.
	/// <para/>
	/// If there are more than 16 items that share the same 28 lower-order 
	/// bits, the 8th-level node will expand to hold all of these items; this
	/// is the only way that a node can have more than 16 items.
	/// <para/>
	/// I am not aware whether a data structure like this has been described
	/// in the comp-sci literature or not. If you see something like this in a
	/// paper, let me know.
	/// <para/>
	/// One of the most irritating limitations of .NET for a data structure 
	/// designer is that an object cannot contain a fixed-length array (let alone
	/// a variable-length one). An innovative feature of InternalSet is that
	/// nodes are <i>just</i> arrays--there is no separate object holding the
	/// array! In order to mark arrays as frozen to support 
	/// <see cref="CloneFreeze"/>, all nodes have a 17th item (index [16])
	/// which holds the "frozen" flag if needed.
	/// <para/>
	/// When deletions cause a child node to contain only a single T object,
	/// the reference to that node in its parent is replaced with the single T
	/// that remains. In order to keep track of how full a node is, the last
	/// item (index [16]) can be a boxed counter in unfrozen nodes. In order
	/// to optimize <see cref="Add"/> operations, this integer is updated only 
	/// during <see cref="Remove"/> (or spill) operations; therefore it may 
	/// understate the number of non-empty entries. When removing an item, if 
	/// the counter reaches 1, <see cref="Remove"/> refreshes the counter (by 
	/// scanning the node for items) and, if there is really only one item of 
	/// type T left (rather than a child node), the parent slot is changed to 
	/// that T (as mentioned before).
	/// <para/>
	/// Interesting fact: it is possible for two sets to be equal (contain the 
	/// same items), and yet for those items to be enumerated in different orders
	/// in the two sets.
	/// <para/>
	/// Unlike the original implementation, this version allows 'null' to be a
	/// member of the set.
	/// </remarks>
	[Serializable]
	public struct InternalSet<T> : IEnumerable<T>
	{
		/// <summary>An empty set.</summary>
		/// <remarks>This property comes with a frozen, empty root node,
		/// which <see cref="Set{T}"/> uses as an "initialized" flag.</remarks>
		public static readonly InternalSet<T> Empty = new InternalSet<T> { _root = FrozenEmptyRoot() };

		/// <summary>This is <see cref="EqualityComparer{T}.Default"/>, or
		/// null if T implements <see cref="IReferenceComparable"/>.</summary>
		public static readonly IEqualityComparer<T> DefaultComparer = typeof(IReferenceComparable).IsAssignableFrom(typeof(T)) ? null : EqualityComparer<T>.Default;

		const int BitsPerLevel = 4;
		const int FanOut = 1 << BitsPerLevel;
		const int Mask = FanOut - 1;
		const int MaxDepth = 7;
		const uint FlagMask = (uint)((1L << FanOut) - 1);
		const int CounterPerChild = FanOut;
		const short OverflowFlag = 1 << 12;

		internal class Node
		{
			internal T[] _items;
			internal Node[] _children;
			internal uint _used;     // b0-15 indicates which items are used; b16-31 are 'deleted' flags.
			                         //       these flags indicate usage of _items only, not _children
			internal short _counter; // b0-3  items count, b4-7 child count, b8 overflow flag
			internal byte _depth;    // 0=root, 7=max
			internal bool _isFrozen;
			internal bool IsFrozen { get { return _isFrozen; } }
			internal uint DeletedFlags { get { return (uint)(_used >> FanOut); } }
			internal bool IsEmpty { get { return _counter == 0; } }
			internal bool HasOverflow { get { return (_counter & OverflowFlag) != 0; } }
			internal int Counter { get { return _counter; } }
			// Note: unnecessary writes could cause 'false sharing' slowdowns when multithreading
			internal void Freeze() { if (!_isFrozen) _isFrozen = true; }
			[Conditional("DEBUG")]
			internal void CheckCounter()
			{
				int used = (int)(_used & FlagMask), deleted = (int)(_used >> 16);
				Debug.Assert((used & deleted) == 0);
				int ones = MathEx.CountOnes(used), children = _children == null ? 0 : _children.Count(n => n != null);
				bool overflow = this is MaxDepthNode && !((MaxDepthNode)this)._overflow.IsEmpty;
				Debug.Assert(ones + children * CounterPerChild + (overflow ? OverflowFlag : 0) == _counter);
			}
			public Node(int depth)
			{
				_depth = (byte)depth;
				_items = new T[FanOut];
			}
			internal Node(Node frozen) // thawing constructor
			{
				_items = InternalList.CopyToNewArray(frozen._items);
				_used = frozen._used;
				_counter = frozen._counter;
				_depth = frozen._depth;
				if (frozen._children != null) {
					_children = InternalList.CopyToNewArray(frozen._children);
					for (int i = 0; i < _children.Length; i++) {
						var c = _children[i];
						if (c != null)
							c.Freeze();
					}
				}
			}
			internal virtual Node Clone()
			{
				return new Node(this);
			}

			internal bool TAt(int i) { return (_used & (1u << i)) != 0; }
			internal void Assign(T item, int i)
			{
				Debug.Assert(i < FanOut && (_used & (1u << i)) == 0);
				// set used flag, clear deleted flag
				_used = (_used | (1u << i)) & ~((FlagMask+1u) << i);
				_items[i] = item;
				_counter++;
				CheckCounter();
			}

			/// <summary>Clears the value at _items[i] and updates the bookkeeping
			/// information in _used and _counter.
			/// </summary>
			internal void ClearTAt(int i)
			{
				Debug.Assert(!IsFrozen);
				_items[i] = default(T);
				// clear used flag, set deleted flag
				_used = (_used | ((FlagMask+1u) << i)) & ~(1u << i);
				_counter--;
				CheckCounter();
			}

			internal void Clear()
			{
				Debug.Assert(!IsFrozen);
				_used = 0;
				_counter = 0;
				for (int i = 0; i < _items.Length; i++)
					_items[i] = default(T);
				if (_children != null)
					for (int i = 0; i < _children.Length; i++)
						_children[i] = null;
			}
		}

		internal class MaxDepthNode : Node
		{
			internal InternalList<T> _overflow = InternalList<T>.Empty;

			internal MaxDepthNode() : base(MaxDepth) { }
			internal MaxDepthNode(MaxDepthNode frozen) : base(frozen) // thawing constructor
			{
				_overflow = frozen._overflow.Clone();
			}
			internal override Node Clone()
			{
				return new MaxDepthNode(this);
			}

			internal int ScanOverflowFor(T item, IEqualityComparer<T> comparer, out T existing)
			{
				for (int i = 0; i < _overflow.Count; i++) {
					existing = _overflow[i];
					if (comparer == null ? object.ReferenceEquals(existing, item) : comparer.Equals(existing, item))
						return i;
				}
				existing = default(T);
				return -1;
			}

			internal void AddOverflowItem(T item)
			{
				_overflow.Add(item);
				_counter |= OverflowFlag;
				CheckCounter();
			}
			internal void RemoveOverflowItem(int i)
			{
				_overflow[i] = _overflow.Last;
				_overflow.Pop();
				if (_overflow.IsEmpty)
					_counter &= ~OverflowFlag;
				CheckCounter();
			}
		}

		static Node FrozenEmptyRoot()
		{
			var node = new Node(0);
			node.Freeze();
			return node;
		} 

		Node _root;

		public InternalSet(IEnumerable<T> list, IEqualityComparer<T> comparer, out int count)
		{
			_root = null;
			count = UnionWith(list, comparer, false);
		}
		public InternalSet(IEnumerable<T> list, IEqualityComparer<T> comparer)
		{
			_root = null;
			UnionWith(list, comparer, false);
		}

		#region CloneFreeze, Thaw, IsRootFrozen, HasRoot

		/// <summary>Freezes the hashtrie so that any further changes require paths 
		/// in the tree to be copied.</summary>
		/// <remarks>This is an O(1) operation. It causes all existing copies of 
		/// this <see cref="InternalSet{T}"/>, as well as any other copies you make
		/// in the future, to become independent of one another so that 
		/// modifications to one copy do not affect any of the others.
		/// <para/>
		/// To unfreeze the hashtrie, simply modify it as usual with (for example)
		/// a call to <see cref="Add"/> or <see cref="Remove"/>, or call 
		/// <see cref="Thaw"/>. Frozen parts of the trie are copied on-demand.
		/// </remarks>
		public InternalSet<T> CloneFreeze()
		{
			if (_root != null)
				_root.Freeze();
			return this;
		}

		/// <summary>Thaws a frozen root node by duplicating it, or creates the 
		/// root node if the set doesn't have one.</summary>
		/// <remarks>Since <see cref="InternalSet{T}"/> is a structure rather
		/// than a class, it's not immediately obvious what the happens when you 
		/// copy it with the '=' operator. The <see cref="InternalList{T}"/> 
		/// structure, for example, it is unsafe to copy (in general) because
		/// as the list length changes, the two (or more) copies immediately
		/// go "out of sync" because each copy has a separate Count property 
		/// and a separate array pointer--and yet they will share the same array,
		/// at least temporarily, which can produce strange results.
		/// <para/>
		/// It is mostly safe to copy InternalSet instances, however, because 
		/// they only contain a single piece of data (a reference to the root
		/// node), and the root node only changes in two situations:
		/// <ol>
		/// <li>When the root node is null and you call <see cref="Add"/> or this method</li>
		/// <li>When the root node is frozen and you modify the set or call this method</li>
		/// </ol>
		/// In the second case, when you have frozen a set with <see cref="CloneFreeze()"/>,
		/// all existing copies are frozen, and further changes affect only 
		/// the specific copy that you change. You can also call <see cref="Thaw()"/>
		/// if you need to make copies that are kept in sync, without 
		/// actually modifying the set first.
		/// <para/>
		/// This method has no effect if the root node is already thawed.
		/// </remarks>
		public void Thaw()
		{
			if (_root == null)
				_root = new Node(0);
			else if (_root.IsFrozen)
				_root = _root.Clone();
		}

		public bool IsRootFrozen { get { return _root == null || _root.IsFrozen; } }
		public bool HasRoot { get { return _root != null; } }

		#endregion

		#region Helper methods

		static int Adj(int i, int n) { return (i + n) & Mask; }
		
		static bool Equals(T value, T item, IEqualityComparer<T> comparer)
		{
			return comparer == null ? object.ReferenceEquals(value, item) : comparer.Equals(value, item);
		}
		static bool Equals(T value, ref T item, IEqualityComparer<T> comparer)
		{
			if (comparer == null)
				return object.ReferenceEquals(value, item);
			else if (comparer.Equals(value, item)) {
				item = value;
				return true;
			}
			return false;
		}
		static uint GetHashCode(T item, IEqualityComparer<T> comparer)
		{
			if (comparer == null)
				return (uint)(item == null ? 0 : item.GetHashCode());
			return (uint)comparer.GetHashCode(item);
		}

		static void PropagateFrozenFlag(Node parent, Node children)
		{
			if (parent.IsFrozen)
				children.Freeze();
		}

		#endregion

		#region Add(), Remove() and helpers

		/// <summary>Tries to add an item to the set, and retrieves the existing item if present.</summary>
		/// <returns>true if the item was added, false if it was already present.</returns>
		public bool Add(ref T item, IEqualityComparer<T> comparer, bool replaceIfPresent)
		{
			if (_root == null)
				_root = new Node(0);
			return AddOrRemove(ref _root, ref item, GetHashCode(item, comparer), comparer, 
			                   replaceIfPresent ? AddOrReplace : AddIfNotPresent);
		}

		/// <summary>Removes an item from the set.</summary>
		/// <returns>true if the item was removed, false if it was not found.</returns>
		public bool Remove(ref T item, IEqualityComparer<T> comparer)
		{
			if (_root == null)
				return false;
			return AddOrRemove(ref _root, ref item, GetHashCode(item, comparer), comparer, RemoveMode);
		}

		delegate bool OnFoundExisting(ref Node slots, int i, T item);
		static readonly OnFoundExisting AddIfNotPresent = _IgnoreExisting_;
		static readonly OnFoundExisting AddOrReplace = _ReplaceExisting_;
		static readonly OnFoundExisting RemoveMode = _DeleteExisting_;
		static bool _IgnoreExisting_(ref Node slots, int i, T item)
		{
			return false;
		}
		static bool _ReplaceExisting_(ref Node slots, int i, T item)
		{
			Debug.Assert(slots.TAt(i));
			if (slots.IsFrozen)
				slots = slots.Clone();
			slots._items[i] = item;
			return false;
		}
		static bool _DeleteExisting_(ref Node slots, int i, T item)
		{
			Debug.Assert(slots.TAt(i));
			if (slots.Counter == 1) {
				slots.CheckCounter();
				slots = null;
			} else {
				if (slots.IsFrozen)
					slots = slots.Clone();
				slots.ClearTAt(i);
				Debug.Assert(!slots.IsEmpty);
			}
			return true;
		}

		// TODO: try holding the arguments item, hc, depth, comparer, notFound in a structure to see if 
		// performance improves.
		static bool AddOrRemove(ref Node slots, ref T item, uint hc, IEqualityComparer<T> comparer, OnFoundExisting mode)
		{
			int iHome = (int)hc & Mask; // the "home" slot of the new item
		retry:
			Node children;
			bool added;
			if (slots._children != null && (children = slots._children[iHome]) != null) {
				var old = children;
				PropagateFrozenFlag(slots, children);
				Debug.Assert(children._depth == slots._depth + 1);
				added = AddOrRemove(ref children, ref item, hc >> BitsPerLevel, comparer, mode);
				if (old != children) {
					if (slots.IsFrozen)
						slots = slots.Clone();
					slots._children[iHome] = children;
					if (children == null) {
						Debug.Assert(object.ReferenceEquals(mode, RemoveMode));
						slots._counter -= CounterPerChild; // child node counts as 2
						slots.CheckCounter();
						if (slots.IsEmpty)
							slots = null;
					}
				}
				return added;
			}

			uint used = slots._used;
			uint deleted = slots.DeletedFlags;
			uint usedOrDeleted = (used | deleted) & FlagMask;
			deleted |= deleted << FanOut;
			deleted >>= iHome;
			usedOrDeleted |= usedOrDeleted << FanOut;
			usedOrDeleted = (usedOrDeleted >> iHome) & Mask;
			int target = 0;
			int iAdj;
			T existing;
			if (comparer == null && item != null) {
				// Use reference equality (e.g. for T=Symbol); too bad .NET doesn't
				// support bitwise equality or we'd use this code for integers too.
				// In this branch we'll compare with all four items to simplify the
				// code (this approach needs an extra lookup table, _targetTable.)
				// It would be foolish to use this approach for normal comparison,
				// since comparison may be expensive in general (and besides, we 
				// should not call comparer.Equals() on slots that may be empty);
				// but we know that reference comparison is trivial. This 
				// optimization cannot be used when item==default(T), hence the 
				// check for item!=null above.
				if ((object)item == (object)slots._items[iAdj = iHome] ||
					(object)item == (object)slots._items[iAdj = Adj(iHome, 1)] ||
					(object)item == (object)slots._items[iAdj = Adj(iHome, 2)] ||
					(object)item == (object)slots._items[iAdj = Adj(iHome, 3)])
					return mode(ref slots, iAdj, item);

				deleted &= Mask;
				target = InternalSet_TargetTable.Value[usedOrDeleted | (deleted << BitsPerLevel)];
			} else {
				switch (usedOrDeleted) {
					case 15:
						target++;
						if ((deleted & 8) != 0) target = 0;
						else if (comparer.Equals(existing = slots._items[iAdj = Adj(iHome, 3)], item)) goto found;
						goto case 7;
					case 7:
						target++;
						if ((deleted & 4) != 0) target = 0;
						else if (comparer.Equals(existing = slots._items[iAdj = Adj(iHome, 2)], item)) goto found;
						goto case 3;
					case 3: case 11:
						target++;
						if ((deleted & 2) != 0) target = 0;
						else if (comparer.Equals(existing = slots._items[iAdj = Adj(iHome, 1)], item)) goto found;
						goto case 1;
					case 1: case 5: case 9: case 13:
						target++;
						if ((deleted & 1) != 0) target = 0;
						else if (comparer.Equals(existing = slots._items[iAdj = iHome], item)) goto found;
						goto case 0;
					case 0:	case 2: case 4: case 6: case 8: case 10: case 12: case 14:
						break;
				}
			}

			// At maximum depth, we may have to look at the overflow list too
			MaxDepthNode mdSlots = null;
			if (slots._depth >= MaxDepth) {
				mdSlots = (MaxDepthNode)slots;
				int i = mdSlots.ScanOverflowFor(item, comparer, out existing);
				if (i != -1)
					return OnFoundInOverflow(ref slots, i, ref item, mode, existing);
			}

			// item does not exist in set
			// (Thanks to SlimTune profiler for identifying Delegate.operator== as a slowdown)
			if ((object)mode == (object)RemoveMode)
				return false;

			// Add new item
			if (slots.IsFrozen)
				slots = slots.Clone();
			if (target <= 3) {
				slots.Assign(item, Adj(iHome, target));
				return true;
			}

			// At this point we know that all four slots are occupied, so we
			// must spill into a child node. Except, of course, at maximum depth.
			if (mdSlots == null) {
				int spill_i = SelectBucketToSpill(slots, iHome, comparer);
				Spill(slots, spill_i, comparer);
				goto retry;
			} else {
				if (mdSlots.IsFrozen)
					mdSlots = (MaxDepthNode)slots;
				mdSlots.AddOverflowItem(item);
				return true;
			}
		
		found:
			bool result = mode(ref slots, iAdj, item);
			item = existing;
			return result;
		}
		static bool OnFoundInOverflow(ref Node slots, int i, ref T item, OnFoundExisting mode, T existing)
		{
			if (!object.ReferenceEquals(mode, AddIfNotPresent)) {
				if (slots.IsFrozen)
					slots = slots.Clone();
				
				var mdSlots = (MaxDepthNode)slots;
				if (object.ReferenceEquals(mode, RemoveMode)) {
					mdSlots.RemoveOverflowItem(i);
					item = existing;
					return true;
				} else {
					Debug.Assert(object.ReferenceEquals(mode, AddOrReplace));
					mdSlots._overflow[i] = item;
				}
			}
			item = existing;
			return false;
		}

		private static bool OpAtMaxDepth(Node slots, ref T item, uint hc, int depth, IEqualityComparer<T> comparer, OnFoundExisting mode)
		{
			throw new NotImplementedException();
		}

		static int SelectBucketToSpill(Node slots, int i0, IEqualityComparer<T> comparer)
		{
			int[] count = new int[FanOut];
			int max = count[i0] = 1, max_i = i0;

			// The caller wants one of the items spilled to exist in the range
			// Adj(i0, 0..4). Scanning the range Adj(i0, -1..5) guarantees that 
			// this is true (given that 'max' will be at least two if it is 
			// increased from 1), whereas a larger range like -2..6 does not.
			int depth = slots._depth;
			for (int adj = -1; adj < 5; adj++)
			{
				int iAdj = Adj(i0, adj);
				T value = slots._items[iAdj];
				if (slots.TAt(iAdj)) {
					int hc = (int)(GetHashCode(value, comparer) >> (depth * BitsPerLevel)) & Mask;
					if (++count[hc] > max) {
						max = count[hc];
						max_i = hc;
					}
				}
			}
			return max_i;
		}
		static void Spill(Node parent, int i0, IEqualityComparer<T> comparer)
		{
			int parentDepth = parent._depth;
			var children = parentDepth + 1 == MaxDepth ? new MaxDepthNode() : new Node(parentDepth + 1);
			for (int adj = 0; adj < 4; adj++)
			{
				int iAdj = Adj(i0, adj);
				if (parent.TAt(iAdj)) {
					T t = parent._items[iAdj];
					uint hc = GetHashCode(t, comparer) >> (parentDepth * BitsPerLevel);
					if ((hc & Mask) == i0) {
						bool @true = AddOrRemove(ref children, ref t, hc >> BitsPerLevel, comparer, AddIfNotPresent);
						Debug.Assert(@true);
						parent.ClearTAt(iAdj);
					}
				}
			}

			if (parent._children == null)
				parent._children = new Node[FanOut];
			Debug.Assert(parent._children[i0] == null);
			parent._children[i0] = children;
			parent._counter += CounterPerChild;
		}

		#endregion

		#region Find() and helper

		public bool Find(ref T item, IEqualityComparer<T> comparer)
		{
			Node slots = _root;
			if (slots == null)
				return false;
			uint hc = GetHashCode(item, comparer);
			
			int iHome;
			for (;;) {
				iHome = (int)hc & Mask; // the "home" slot of the new item
				Node children;
				if (slots._children != null && (children = slots._children[iHome]) != null) {
					slots = children;
					hc >>= BitsPerLevel;
					//depth++;
					continue;
				}
				break;
			}

			int iAdj;
			T existing;
			if (comparer == null && item != null) {
				// Use reference equality (e.g. for T=Symbol); too bad .NET doesn't
				// support bitwise equality or we'd use this code for integers too.
				if ((object)item == (object)slots._items[iAdj = iHome] ||
					(object)item == (object)slots._items[iAdj = Adj(iHome, 1)] ||
					(object)item == (object)slots._items[iAdj = Adj(iHome, 2)] ||
					(object)item == (object)slots._items[iAdj = Adj(iHome, 3)])
					return true;
			} else {
				uint used = slots._used;
				uint deleted = slots.DeletedFlags;
				uint usedOrDeleted = (used | deleted) & FlagMask;
				deleted |= deleted << FanOut;
				deleted >>= iHome;
				usedOrDeleted |= usedOrDeleted << FanOut;
				usedOrDeleted = (usedOrDeleted >> iHome) & Mask;

				switch (usedOrDeleted) {
					case 15:
						if ((deleted & 8) != 0) {}
						else if (comparer.Equals(existing = slots._items[iAdj = Adj(iHome, 3)], item)) goto found;
						goto case 7;
					case 7:
						if ((deleted & 4) != 0) {}
						else if (comparer.Equals(existing = slots._items[iAdj = Adj(iHome, 2)], item)) goto found;
						goto case 3;
					case 3: case 11:
						if ((deleted & 2) != 0) {}
						else if (comparer.Equals(existing = slots._items[iAdj = Adj(iHome, 1)], item)) goto found;
						goto case 1;
					case 1: case 5: case 9: case 13:
						if ((deleted & 1) != 0) {}
						else if (comparer.Equals(existing = slots._items[iAdj = iHome], item)) goto found;
						goto case 0;
					case 0:	case 2: case 4: case 6: case 8: case 10: case 12: case 14:
						break;
				}
			}

			if (slots.HasOverflow) {
				Debug.Assert(slots._depth == MaxDepth);
				int i = ((MaxDepthNode)slots).ScanOverflowFor(item, comparer, out existing);
				if (i != -1)
					goto found;
			}

			return false;
		found:
			item = existing;
			return true;
		}

		#endregion

		#region Enumerator

		public struct Enumerator : IEnumerator<T>
		{
			internal T _current;
			Node _currentNode;
			InternalList<Node> _stack;
			uint _hc; // stack of indexes, which are also partial hashcodes
			int _i;   // index in the current node

			public Enumerator(InternalSet<T> set) : this()
			{
				_stack = InternalList<Node>.Empty;
				Reset(set);
			}
			internal Enumerator(int stackCapacity) : this(Empty)
			{
				_stack.Capacity = stackCapacity;
			}

			public void Reset(InternalSet<T> set)
			{
				_current = default(T);
				_stack.Resize(0, false);
				_currentNode = set._root;
				_i = -1;
				_hc = 0;
			}

			public bool MoveNext()
			{
				if (_currentNode == null)
					return false;
			retry:
				for (;;) {
					if (++_i < _currentNode._items.Length) {
						if (!_currentNode.TAt(_i))
							continue;
						_current = _currentNode._items[_i];
						return true;
					}

					// No more regular items in current node. Overflow list?
					if (_currentNode.HasOverflow) {
						Debug.Assert(_currentNode._depth == MaxDepth);
						var overflow = ((MaxDepthNode)_currentNode)._overflow;
						int i = _i - _currentNode._items.Length;
						if (i < overflow.Count) {
							_current = overflow[i];
							return true;
						}
					}

					// Exhausted all items in current node. Advance to next node:
					// 1. Find the first child of current node, if any
					Node[] children = _currentNode._children;
					if (children != null) {
						for (int i = 0; i < children.Length; i++) {
							Node c = children[i];
							if (c != null) {
								_hc |= (uint)(i << (_stack.Count * BitsPerLevel));
								_stack.Add(_currentNode);
								// Just in case user modifies/deletes Current, copy Frozen flag down the tree
								PropagateFrozenFlag(_currentNode, c);
								_currentNode = c;
								_i = -1;
								goto retry;
							}
						}
					}

					// 2. Find next child in parent node (if none left, we're done)
					for (;;) {
						if (_stack.Count == 0) {
							_currentNode = null;
							return false;
						} else {
							Node parent = _stack.Last;
							children = parent._children;
							int shift = (_stack.Count - 1) * BitsPerLevel;
							uint i = (_hc >> shift) & Mask;
							uint clearMask = (uint)~(Mask << shift);
							for (i++; i < parent._children.Length; i++) {
								if (children[i] != null) {
									_currentNode = children[i];
									_hc = (_hc & clearMask) | (i << shift);
									_i = -1;
									goto retry;
								}
							}
							_stack.Pop();
							_hc &= clearMask;
						}
					}
				}
			}

			public T Current
			{
				get { return _current; }
			}
			
			/// <summary>Changes the value associated with the current key.</summary>
			/// <param name="comparer">Optional. If comparer!=null, it is used to
			/// verify that the new value is equal to the old value.</param>
			/// <exception cref="ArgumentException">According to the comparer 
			/// provided, the new value is not "equal" to the old value.</exception>
			/// <remarks>The new value must compare equal to the old value, since
			/// the new value is placed at the same location in the trie. If a
			/// value is placed in the wrong location, it becomes irretrievable
			/// (except via enumerator), as search methods will be looking 
			/// elsewhere for it.</remarks>
			public void SetCurrentValue(T value, ref InternalSet<T> set, IEqualityComparer<T> comparer)
			{
				Node curNode = AutoThawCurrentNode(ref set);
				if (_i < curNode._items.Length)
					SetCurrentValueCore(ref curNode._items[_i], value, comparer);
				else
					SetCurrentValueCore(ref ((MaxDepthNode)curNode)._overflow.InternalArray[_i - curNode._items.Length], value, comparer);
			}
			static void SetCurrentValueCore(ref T slot, T value, IEqualityComparer<T> comparer)
			{
				if (comparer != null && !comparer.Equals(slot, value))
					throw new ArgumentException("SetCurrentValue: the new key does not match the old key.");
				slot = value;
			}

			/// <summary>Removes the current item from the set, and moves to the 
			/// next item.</summary>
			/// <returns>As with <see cref="MoveNext"/>, returns true if there is 
			/// another item after the current one and false if not.</returns>
			public bool RemoveCurrent(ref InternalSet<T> set)
			{
				Node child = AutoThawCurrentNode(ref set);
				if (_i < child._items.Length)
					child.ClearTAt(_i);
				else {
					var mdChild = (MaxDepthNode)child;
					mdChild.RemoveOverflowItem(_i - child._items.Length);
					_i--; // enumerate _overflow[_i - _items.Length] a second time
				}

				int depth = _stack.Count - 1;
				while (child.IsEmpty && depth >= 0) {
					Node parent = _stack[depth];
					int i = GetCurrentIndexAt(depth);
					Debug.Assert(parent._children[i] == child);
					parent._children[i] = null;
					parent._counter -= CounterPerChild;
					depth--;
					child = parent;
				}
				
				return MoveNext();
			}

			private int GetCurrentIndexAt(int level)
			{
				return (int)(_hc >> (level * BitsPerLevel)) & Mask;
			}

			private Node AutoThawCurrentNode(ref InternalSet<T> set)
			{
				int i = _stack.Count;
				Node old = _currentNode, oldParent;
				if (!_currentNode.IsFrozen)
					return _currentNode;
				_currentNode = _currentNode.Clone();
				Node current = _currentNode;

				Node[] stack = _stack.InternalArray;
				for(;;) {
					if (i == 0) {
						set._root = current;
						break;
					} else {
						i--;
						Node parent = stack[i];
						oldParent = parent;
						int indexInParent = GetCurrentIndexAt(i);
						if (parent.IsFrozen) {
							parent = parent.Clone();
							stack[i] = parent;
						}
						Debug.Assert(parent._children[indexInParent] == old);
						parent._children[indexInParent] = current;
						current = parent;
						old = oldParent;
						if (oldParent == parent)
							break;
					}
				}
				return _currentNode;
			}

			void IDisposable.Dispose() { }
			object System.Collections.IEnumerator.Current { get { return Current; } }
			void System.Collections.IEnumerator.Reset() { throw new NotSupportedException(); }
		}
		public Enumerator GetEnumerator() { return new Enumerator(this); }
		IEnumerator<T> IEnumerable<T>.GetEnumerator() { return GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

		#endregion

		public void CopyTo(T[] array, int arrayIndex)
		{
			foreach (T value in this)
				array[arrayIndex++] = value;
		}

		public void Clear()
		{
			if (_root != null) {
				if (_root.IsFrozen)
					_root = null;
				else
					_root.Clear();
			}
		}

		public bool Contains(T item, IEqualityComparer<T> comparer)
		{
			return Find(ref item, comparer);
		}

		public int Count()
		{
			var e = new Enumerator(this);
			int count = 0;
			while (e.MoveNext()) count++;
			return count;
		}

		#region UnionWith, IntersectWith, ExceptWith, SymmetricExceptWith

		[ThreadStatic]
		static Enumerator _setOperationEnumerator = new Enumerator(8);

		/// <summary>Adds the contents of 'other' to this set.</summary>
		/// <param name="thisComparer">The comparer for this set (not for 'other', 
		/// which is simply enumerated).</param>
		/// <param name="replaceIfPresent">If items in 'other' match items in this 
		/// set, this flag causes those items in 'other' to replace the items in
		/// this set.</param>
		public int UnionWith(IEnumerable<T> other, IEqualityComparer<T> thisComparer, bool replaceIfPresent)
		{
			int numAdded = 0;
			foreach (var t in other) {
				var t2 = t;
				if (Add(ref t2, thisComparer, replaceIfPresent))
					numAdded++;
			}
			return numAdded;
		}
		/// <inheritdoc cref="UnionWith(IEnumerable{T}, IEqualityComparer{T}, bool)"/>
		public int UnionWith(InternalSet<T> other, IEqualityComparer<T> thisComparer, bool replaceIfPresent)
		{
			int numAdded = 0;
			var e = _setOperationEnumerator;
			e.Reset(other);
			while (e.MoveNext())
				if (Add(ref e._current, thisComparer, replaceIfPresent))
					numAdded++;
			return numAdded;
		}
		/// <summary>Removes all items from this set that are not present in 'other'.</summary>
		/// <param name="other">The set whose members should be kept in this set.</param>
		/// <param name="otherComparer">The comparer for 'other' (not for this set,
		/// which is simply enumerated).</param>
		/// <returns>Returns the number of items that were removed from the set.</returns>
		public int IntersectWith(InternalSet<T> other, IEqualityComparer<T> otherComparer)
		{
			int removed = 0;
			var e = _setOperationEnumerator;
			e.Reset(this);
			while (e.MoveNext())
				while (!other.Contains(e.Current, otherComparer)) {
					removed++;
					if (!e.RemoveCurrent(ref this))
						return removed;
				}
			return removed;
		}
		public int IntersectWith(ISet<T> other)
		{
			int removed = 0;
			var e = _setOperationEnumerator;
			e.Reset(this);
			while (e.MoveNext())
				while (!other.Contains(e.Current)) {
					removed++;
					if (!e.RemoveCurrent(ref this))
						return removed;
				}
			return removed;
		}
		/// <summary>Removes all items from this set that are not present in 'other'.</summary>
		/// <param name="other">The set whose members should be kept in this set.</param>
		/// <returns>Returns the number of items that were removed.</returns>
		/// <remarks>
		/// This method is costly if 'other' is not a set; a temporary set will be 
		/// constructed to answer the query. Also, this overload has the same subtle 
		/// assumption as the other overload.
		/// </remarks>
		public int IntersectWith(IEnumerable<T> other, IEqualityComparer<T> comparer)
		{
			ISet<T> otherSet = other as ISet<T>;
			if (otherSet != null)
				return IntersectWith(otherSet);
			else {
				InternalSet<T> set = new InternalSet<T>(other, comparer);
				return IntersectWith(set, comparer);
			}
		}
		/// <summary>Removes all items from this set that are present in 'other'.</summary>
		/// <param name="other">The set whose members should be removed from this set.</param>
		/// <param name="otherComparer">The comparer for this set (not for 'other',
		/// which is simply enumerated).</param>
		/// <returns>Returns the number of items that were removed.</returns>
		public int ExceptWith(IEnumerable<T> other, IEqualityComparer<T> thisComparer)
		{
			int removed = 0;
			foreach (T t in other) {
				T t2 = t;
				if (Remove(ref t2, thisComparer))
					removed++;
			}
			return removed;
		}
		/// <inheritdoc cref="ExceptWith(IEnumerable{T}, IEqualityComparer{T})"/>
		public int ExceptWith(InternalSet<T> other, IEqualityComparer<T> thisComparer)
		{
			int removed = 0;
			var e = _setOperationEnumerator;
			e.Reset(other);
			while (e.MoveNext()) {
				T t = e.Current;
				if (Remove(ref t, thisComparer))
					removed++;
			}
			return removed;
		}

		public int SymmetricExceptWith(InternalSet<T> other, IEqualityComparer<T> thisComparer)
		{
			int delta = 0;
			var e = _setOperationEnumerator;
			e.Reset(other);
			while (e.MoveNext()) {
				var t = e.Current;
				if (Remove(ref t, thisComparer))
					delta--;
				else {
					delta++;
					bool @true = Add(ref t, thisComparer, false);
					Debug.Assert(@true);
				}
			}
			return delta;
		}

		/// <summary>Modifies the current set to contain only elements that were
		/// present either in this set or in the other collection, but not both.</summary>
		/// <param name="xorDuplicates">Controls this function's behavior in case
		/// 'other' contains duplicates. If xorDuplicates is true, an even number
		/// of duplicates has no overall effect and an odd number is treated the 
		/// same as if there were a single instance of the item. Setting 
		/// xorDuplicates to false is costly, since a temporary set is constructed 
		/// in order to eliminate any duplicates. The same comparer is used for 
		/// the temporary set as for this set.</param>
		/// <remarks>Returns the change in set size (positive if items were added,
		/// negative if items were removed)</remarks>
		public int SymmetricExceptWith(IEnumerable<T> other, IEqualityComparer<T> comparer, bool xorDuplicates = true)
		{
			if (!xorDuplicates) {
				var set = new InternalSet<T>(other, comparer);
				return SymmetricExceptWith(other, comparer);
			} else {
				int delta = 0;
				foreach (T t in other) {
					T t2 = t;
					if (Remove(ref t2, comparer))
						delta--;
					else {
						delta++;
						bool @true = Add(ref t2, comparer, false);
						Debug.Assert(@true);
					}
				}
				return delta;
			}
		}

		#endregion

		#region IsSubsetOf, IsSupersetOf, Overlaps, IsProperSubsetOf, IsProperSupersetOf

		/// <summary>Returns true if all items in this set are present in the other set.</summary>
		/// <param name="myMinCount">Specifies the minimum number of items that this set contains (use 0 if unknown)</param>
		public bool IsSubsetOf(ISet<T> other, int myMinCount)
		{
			if (other.Count < myMinCount)
				return false;
			foreach (T item in this)
				if (!other.Contains(item))
					return false;
			return true;
		}
		public bool IsSubsetOf(InternalSet<T> other, IEqualityComparer<T> otherComparer)
		{
			var e = _setOperationEnumerator;
			e.Reset(this);
			while (e.MoveNext())
				if (!other.Contains(e.Current, otherComparer))
					return false;
			return true;
		}
		public bool IsSubsetOf(IEnumerable<T> other, IEqualityComparer<T> comparer, int myMinCount = 0)
		{
			ISet<T> otherSet = other as ISet<T>;
			if (otherSet != null)
				return IsSubsetOf(otherSet, myMinCount);
			else {
				if (myMinCount > 0) {
					var coll = other as ICollection<T>;
					if (coll != null && coll.Count < myMinCount)
						return false;
				}
				InternalSet<T> set = new InternalSet<T>(other, comparer);
				return IsSubsetOf(set, comparer);
			}
		}

		/// <summary>Returns true if all items in the other set are present in this set.</summary>
		public bool IsSupersetOf(IEnumerable<T> other, IEqualityComparer<T> thisComparer, int myMaxCount = int.MaxValue)
		{
			var coll = other as ICollection<T>;
			if (coll != null && coll.Count > myMaxCount)
				return false;

			foreach (T item in other)
				if (!Contains(item, thisComparer))
					return false;
			return true;
		}
		public bool IsSupersetOf(InternalSet<T> other, IEqualityComparer<T> thisComparer)
		{
			return other.IsSubsetOf(this, thisComparer);
		}

		/// <summary>Returns true if this set contains at least one item from 'other'.</summary>
		public bool Overlaps(IEnumerable<T> other, IEqualityComparer<T> thisComparer)
		{
			foreach (T item in other)
				if (Contains(item, thisComparer))
					return true;
			return false;
		}
		public bool Overlaps(InternalSet<T> other, IEqualityComparer<T> thisComparer)
		{
			var e = _setOperationEnumerator;
			e.Reset(other);
			while (e.MoveNext())
				if (Contains(e.Current, thisComparer))
					return true;
			return false;
		}

		/// <summary>Returns true if all items in this set are present in the other set, 
		/// and the other set has at least one item that is not in this set.</summary>
		/// <remarks>
		/// This implementation assumes that if the two sets use different
		/// definitions of equality (different <see cref="IEqualityComparer{T}"/>s),
		/// that neither set contains duplicates from the point of view of the other
		/// set. If this rule is broken--meaning, if either of the sets were 
		/// constructed with the comparer of the other set, that set would shrink--
		/// then the results of this method are unreliable. If both sets use the 
		/// same comparer, though, you have nothing to worry about.</remarks>
		public bool IsProperSubsetOf(ISet<T> other, int myExactCount) { return myExactCount < other.Count && IsSubsetOf(other, myExactCount); }
		
		/// <summary>Returns true if all items in this set are present in the other set, 
		/// and the other set has at least one item that is not in this set.</summary>
		/// <remarks>
		/// This method is costly if 'other' is not a set; a temporary set will be 
		/// constructed to answer the query. Also, this overload has the same subtle 
		/// assumption as the other overload.
		/// </remarks>
		public bool IsProperSubsetOf(IEnumerable<T> other, IEqualityComparer<T> comparer, int myExactCount)
		{
			ISet<T> otherSet = other as ISet<T>;
			if (otherSet != null)
				return IsProperSubsetOf(otherSet, myExactCount);
			else {
				if (other is ICount && ((ICount)other).Count <= myExactCount)
					return false;
				var set = new InternalSet<T>();
				int otherCount = set.UnionWith(other, comparer, false);
				return myExactCount < otherCount && IsSubsetOf(set, comparer);
			}
		}

		/// <summary>Returns true if all items in the other set are present in this set, 
		/// and this set has at least one item that is not in the other set.</summary>
		/// <remarks>
		/// This implementation assumes that if the two sets use different
		/// definitions of equality (different <see cref="IEqualityComparer{T}"/>s),
		/// that neither set contains duplicates from the point of view of the other
		/// set. If this rule is broken--meaning, if either of the sets were 
		/// constructed with the comparer of the other set, that set would shrink--
		/// then the results of this method are unreliable. If both sets use the 
		/// same comparer, though, you have nothing to worry about.</remarks>
		public bool IsProperSupersetOf(ISet<T> other, IEqualityComparer<T> thisComparer, int myExactCount) { return myExactCount > other.Count && IsSupersetOf(other, thisComparer, myExactCount); }
		
		/// <summary>Returns true if all items in the other set are present in this set, 
		/// and this set has at least one item that is not in the other set.</summary>
		/// <remarks>
		/// This method is costly if 'other' is not a set; a temporary set will be 
		/// constructed to answer the query. Also, this overload has the same subtle 
		/// assumption as the other overload.
		/// </remarks>
		public bool IsProperSupersetOf(IEnumerable<T> other, IEqualityComparer<T> comparer, int myExactCount)
		{
			ISet<T> otherSet = other as ISet<T>;
			if (otherSet != null)
				return IsProperSupersetOf(otherSet, comparer, myExactCount);
			else {
				if (other is ICount && ((ICount)other).Count >= myExactCount)
					return false;
				var set = new InternalSet<T>();
				int otherCount = 0;
				foreach (T item in other) {
					var item_ = item;
					if (!Contains(item, comparer))
						return false;
					if (set.Add(ref item_, comparer, false))
						if (++otherCount >= myExactCount)
							return false;
				}
				return true;
			}
		}

		/// <summary>Returns true if this set and the other set have the same items.</summary>
		/// <remarks>
		/// This implementation assumes that if the two sets use different
		/// definitions of equality (different <see cref="IEqualityComparer{T}"/>s),
		/// that neither set contains duplicates from the point of view of the other
		/// set. If this rule is broken--meaning, if either of the sets were 
		/// constructed with the comparer of the other set, that set would shrink--
		/// then the results of this method are unreliable. If both sets use the 
		/// same comparer, though, you have nothing to worry about.</remarks>
		public bool SetEquals(ISet<T> other, int myExactCount) { return myExactCount == other.Count && IsSubsetOf(other, myExactCount); }

		/// <summary>Returns true if this set and the other set have the same items.</summary>
		/// <remarks>
		/// This method is costly if 'other' is not a set; a temporary set will be 
		/// constructed to answer the query. Also, this overload has the same subtle 
		/// assumption as the other overload.
		/// </remarks>
		public bool SetEquals(IEnumerable<T> other, IEqualityComparer<T> comparer, int myExactCount)
		{
			ISet<T> otherSet = other as ISet<T>;
			if (otherSet != null)
				return myExactCount == otherSet.Count && IsSubsetOf(otherSet, myExactCount);
			else {
				var other1 = other as ICount;
				if (other1 != null && other1.Count != myExactCount)
					return false;
				var other2 = other as ICollection<T>;
				if (other2 != null && other2.Count != myExactCount)
					return false;

				var set = new InternalSet<T>();
				int otherCount = 0;
				foreach (T item in other) {
					var item_ = item;
					if (!Contains(item, comparer))
						return false;
					if (set.Add(ref item_, comparer, false))
						if (++otherCount >= myExactCount)
							return false;
				}
				return otherCount == myExactCount;
			}
		}

		#endregion

		//#region Union, Intersect, Subtract, Xor

		//public InternalSet<T> Union(InternalSet<T> other, IEqualityComparer<T> otherComparer, ref int delta)
		//    { CloneFreeze(); delta += UnionWith(other, otherComparer); return this; }
		//public InternalSet<T> Intersect(InternalSet<T> other, IEqualityComparer<T> otherComparer) 
		//    { CloneFreeze(); IntersectWith(other, otherComparer); return this; }
		//public InternalSet<T> Subtract(InternalSet<T> other, IEqualityComparer<T> thisComparer)
		//    { CloneFreeze(); ExceptWith(other, thisComparer); return this; }
		//public InternalSet<T> Xor(InternalSet<T> other, IEqualityComparer<T> thisComparer)
		//    { CloneFreeze(); SymmetricExceptWith(other, thisComparer); return this; }

		//#endregion
	}

	class InternalSet_TargetTable
	{
		public static readonly byte[] Value = TargetTable();
		static byte[] TargetTable()
		{
			var table = new byte[256];
			for (int deleted = 0; deleted < 16; deleted++) {
				for (int used = 0; used < 16; used++) {
					int target = 0;
					switch(used | deleted) {
						case 15:
							target++;
							if ((deleted & 8) != 0) target = 0;
							goto case 7;
						case 7:
							target++;
							if ((deleted & 4) != 0) target = 0;
							goto case 3;
						case 3: case 11:
							target++;
							if ((deleted & 2) != 0) target = 0;
							goto case 1;
						case 1: case 5: case 9: case 13:
							target++;
							if ((deleted & 1) != 0) target = 0;
							goto case 0;
						case 0:	case 2: case 4: case 6: case 8: case 10: case 12: case 14:
							break;
					}
					table[used | deleted | (deleted << 4)] = (byte)target;
				}
			}
			return table;
		}
	}

#endif
}
