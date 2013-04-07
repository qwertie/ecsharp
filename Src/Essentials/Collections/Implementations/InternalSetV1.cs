using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Loyc.Collections.Impl
{
// Replaced with second version (in InternalSet.cs), which is faster, and uses more 
// memory for reference types but less memory for value types.
#if false
	/// <summary>A hash-trie data structure for use inside other data structures.</summary>
	/// <remarks>
	/// InternalSet&lt;T> is not designed to be used by itself, but as a building
	/// block for other data structures. It has no Count property because it does 
	/// not know its own size; the outer data structure must track the size if 
	/// the size is needed. The lack of a Count property allows an empty 
	/// InternalSet to use a mere single word of memory!
	/// <para/>
	/// In order to use just one word of memory, this data structure does not
	/// keep track of its <see cref="IEqualityComparer{T}"/>; this object must be
	/// passed to every method (and cannot change after items have been added).
	/// <para/>
	/// The hash-trie is designed to hold reference types. It is not efficient 
	/// for value types because each T value will be boxed in that case. It is
	/// also not efficient for Ts that are expensive to compare; unlike standard 
	/// .NET collections, this data structure does not store the hashcode of each
	/// item in the collection. Therefore it uses less memory, but will spend more
	/// time comparing unless the provided <see cref="IEqualityComparer{T}"/> is
	/// very fast (or null, which causes reference comparison.)
	/// <para/>
	/// This data structure is inspired by Clojure's PersistentHashMap. It is 
	/// designed to be fairly simple and efficient; it is optimized for sets of 
	/// Symbols and therefore has a reference-equality operating mode which
	/// considers two T values equal if and only if the references point to the 
	/// same object; to use this mode, specify 'null' as the 'comparer'
	/// (<see cref="IEqualityComparer{T}"/>) argument to all methods. Whereas
	/// PersistentHashMap uses nodes of size 32, I chose to use nodes of size
	/// 16 in order to increase space efficiency for small sets; for some reason
	/// I tend to design programs that use many small collections and a few big
	/// ones, so I tend to prefer designs that stay efficient at small sizes.
	/// <para/>
	/// Technically, this data structure has O(log N) time complexity for search,
	/// insertion and removal. However, it's a base-16 logarithm, which is faster
	/// than typical O(log N) algorithms that are base-2. At smaller sizes, its 
	/// speed should be similar to a conventional hashtable. TODO: benchmark.
	/// <para/>
	/// Unlike <see cref="InternalList{T}"/>, <c>new InternalSet&lt;T>()</c> is a 
	/// valid empty set. Moreover, because the root node is never changed after
	/// it is created (unless you modify it while it is frozen), it is safe to 
	/// make copies of an <see cref="InternalSet{T}"/> provided that you call
	/// <see cref="Thaw()"/> first; see that method for details.
	/// <para/>
	/// This data structure supports another handy feature that I first developed
	/// for <see cref="AList{T}"/>, namely fast cloning and subtree sharing. You 
	/// can call <see cref="CloneFreeze"/> to freeze/clone the trie in O(1) time; 
	/// this freezes the root node (a transitive property that implicitly affects
	/// all children), but still permits the hashtrie to be modified by copying 
	/// nodes on-demand. Thus the trie is actually frozen, but copy-on-write 
	/// behavior provides the illusion that it is still editable.
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
	/// </remarks>
	public struct InternalSet<T> : IEnumerable<T>
	{
		/// <summary>An empty set.</summary>
		/// <remarks>This property comes with a frozen, empty root node,
		/// which <see cref="ObjectSetI{T}"/> uses as an "initialized" flag.</remarks>
		public static readonly InternalSet<T> Empty = new InternalSet<T> { _root = FrozenEmptyNode() };

		const int BitsPerLevel = 4;
		const int FanOut = 1 << BitsPerLevel;
		const int Mask = FanOut - 1;

		static Object[] FrozenEmptyNode()
		{
			var node = new Object[FanOut + 1];
			node[FanOut] = FrozenFlag.Value;
			return node;
		} 

		static readonly object[] Counter = new object[] { 
			0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
			16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31
		};

		const int MaxDepth = 7;
		Object[] _root;

		// You might wonder: why do I use this structure instead of simply using
		// Object directly? Well, a Object's Value can either be a value of type T, 
		// or an array of children, type Object[]. If we replaced this Object 
		// structure with Object, consider what would happen if T were "Object[]": 
		// then there would be no way to tell whether a given slot held a T value 
		// or a list of children! That said, it is very unlikely that anyone would 
		// set T to Object[], and it might be worthwhile to replace Object with 
		// Object to find out whether it improves performance. On the other hand,
		// Object[] requires one extra word of memory compared to Object[], see
		// http://stackoverflow.com/questions/1589669/overhead-of-a-net-array
		// plus, writing to Object[] requires an extra check due to array 
		// covariance, a feature that .NET copied from Java.
		//[DebuggerDisplay("{Value}")]
		//internal struct Object
		//{
		//    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		//    public object Value;
		//}

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

		// Current InternalSet<T> (64-bit):
		//    8*3+8*17=160 bytes per node
		// Value-type T implementation (64-bit) would be:
		//   one array: (64-bit)
		//      8*3+16*17=24+272=296 bytes
		//   two arrays + node class:
		//      8*5+8*3+16*8=64+128=192 for leaves
		//      192 + 8*4+16*8=192+32+128=352 for nodes with children
		//   leaves are likely much more common, so 2nd implementation is better.
		// When EC# supports templates, I could extend InternalSet<T> to support
		// both the current implementation (optimized for reference types) and the 
		// one I have in mind (optimized for value types) in a single codebase.

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
				_root[FanOut] = FrozenFlag.Value;
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
				_root = new Object[FanOut + 1];
			else if (IsFrozen(_root))
				Thaw(ref _root);
		}

		public bool IsRootFrozen { get { return IsFrozen(_root); } }
		public bool HasRoot { get { return _root != null; } }

		#endregion

		#region Helper methods

		static int Adj(int i, int n) { return (i + n) & Mask; }
		
		static bool QuickGet(Object value, ref T item, IEqualityComparer<T> comparer)
		{
			if (default(T) == null && object.ReferenceEquals(value, item))
				return true;
			if (comparer != null && value is T) {
				var Tvalue = (T)value;
				if (comparer.Equals(Tvalue, item)) {
					item = Tvalue;
					return true;
				}
			}
			return false;
		}
		static bool Equals(Object value, ref T item, IEqualityComparer<T> comparer)
		{
			if (default(T) == null && object.ReferenceEquals(value, item))
				return true;
			if (comparer != null && value is T) {
				var Tvalue = (T)value;
				if (comparer.Equals(Tvalue, item)) {
					item = Tvalue; // return old value of the item
					return true;
				}
			}
			return false;
		}
		static uint GetHashCode(T item, IEqualityComparer<T> comparer)
		{
			return (uint)(comparer == null ? item.GetHashCode() : comparer.GetHashCode(item));
		}

		private static bool IsFrozen(Object[] slots)
		{
			return slots[slots.Length - 1] == FrozenFlag.Value;
		}
		private static void Thaw(ref Object[] slots)
		{
			//Debug.Assert(IsFrozen(slots)); frozen may just be "implied" during a remove op
			var thawed = new Object[slots.Length];
			int used = 0;
			for (int i = 0; i < slots.Length-1; i++) {
				Object slot;
				if ((thawed[i] = slot = slots[i]) != null) {
					used++;
					// The Frozen flag is transitive; when a tree is frozen, 
					// only the root node is marked frozen at first, and the
					// fact that the children are frozen is implied. When a 
					// node is thawed, its children are still frozen so we must
					// mark them explicitly.
					var children = slot as Object[];
					if (children != null)
						children[children.Length - 1] = FrozenFlag.Value;
				}
			}
			slots = thawed;
			thawed[thawed.Length - 1] = used < Counter.Length ? Counter[used] : (object)used;
		}
		static void PropagateFrozenFlag(Object[] parent, Object[] children)
		{
			if (IsFrozen(parent))
				children[children.Length - 1] = FrozenFlag.Value;
		}

		#endregion

		#region Add() and helpers

		/// <summary>Tries to add an item to the set, and retrieves the existing item if present.</summary>
		/// <returns>true if the item was added, false if it was already present.</returns>
		public bool Add(ref T item, IEqualityComparer<T> comparer, bool replaceIfPresent)
		{
			if (_root == null)
				_root = new Object[FanOut + 1];
			return Put(ref _root, ref item, GetHashCode(item, comparer), 0, comparer, replaceIfPresent);
		}

		static bool Put(ref Object[] slots, ref T item, uint hc, int depth, IEqualityComparer<T> comparer, bool replaceIfPresent)
		{
			int iHome = (int)hc & Mask; // the "home" slot of the new item
			object itemO;
			//if (IsFrozen(slots))
			//    Thaw(ref slots);

		retry:
			Object slot = slots[iHome];
			// Detect simplest case up-front
			if (slot == null) {
				if (IsFrozen(slots))
					Thaw(ref slots);
				slots[iHome] = item;
				return true;
			}

			int iAdj = iHome;
			Object[] children = slot as Object[];
			bool added;
			if (children != null) {
				var old = children;
				PropagateFrozenFlag(slots, children);
				added = Put(ref children, ref item, hc >> BitsPerLevel, depth + 1, comparer, replaceIfPresent);
				if (old != children) {
					//slots = ThawAndSet(slots, iHome, children);
					itemO = children;
					goto thawAndSet;
				}
				return added;
			}

			// Now handle all other cases.
			itemO = item;
			// If a deleted slot is found, we must ignore it (and any 'null'
			// slots afterward) until we prove that the item being inserted is 
			// not already present in the node. If we confirm that the item 
			// doesn't exist, then we can replace the first deleted slot with 
			// the new item.
			int iDeleted = -1;
			for (int adj = 0;;) {
				if (slot == DeletedFlag.Value) {
					if (iDeleted == -1)
						iDeleted = iAdj;
				} else if (slot == null) {
					if (iDeleted == -1) {
						//slots = ThawAndSet(slots, iAdj, item);
						added = true;
						goto thawAndSet;
					}
				} else if (Equals(slot, ref item, comparer)) {
					added = false;
					if (replaceIfPresent)
						//slots = ThawAndSet(slots, iAdj, item);
						goto thawAndSet;
					return false;
				}
				if (depth < MaxDepth) {
					iAdj = Adj(iHome, ++adj);
					if (adj >= 4)
						break;
				} else {
					iAdj = adj;
					if (++adj == slots.Length) {
						if (iDeleted >= 0)
							break;
						int len = slots.Length - 1;
						slots = InternalList.CopyToNewArray(slots, len, (len << 1) + 1);
						Debug.Assert(slots[len] == null);
						slots[len] = item;
						return true;
					}
				}
				slot = slots[iAdj];
			}
			if (iDeleted >= 0) {
				//slots = ThawAndSet(slots, iDeleted, item);
				iAdj = iDeleted;
				added = true;
				goto thawAndSet;
			}

			Debug.Assert(depth < MaxDepth);
			// At this point we know that all four slots are occupied, so we
			// must spill into a child node.
			if (IsFrozen(slots))
				Thaw(ref slots);
			int spill_i = SelectBucketToSpill(slots, iHome, depth, comparer);
			Spill(slots, spill_i, depth, comparer);
			Debug.Assert(slots[spill_i] is Object[]);
			goto retry;

		thawAndSet:
			if (IsFrozen(slots))
				Thaw(ref slots);
			slots[iAdj] = itemO;
			return added;
		}

		//private static Object[] ThawAndSet(Object[] slots, int i, object item)
		//{
		//    if (IsFrozen(slots))
		//        Thaw(ref slots);
		//    slots[i].Value = item;
		//    return slots;
		//}

		static int SelectBucketToSpill(Object[] slots, int i0, int depth, IEqualityComparer<T> comparer)
		{
			int[] count = new int[FanOut];
			int max = count[i0] = 1, max_i = i0;

			for (int adj = 0; adj < 4; adj++)
			{
				int iAdj = Adj(i0, adj);
				object value = slots[iAdj];
				Debug.Assert(value != null);
				if (value is T) {
					int hc = (int)(GetHashCode((T)value, comparer) >> (depth * BitsPerLevel)) & Mask;
					if (++count[hc] > max) {
						max = count[hc];
						max_i = hc;
					}
				}
			}
			return max_i;
		}
		static Object[] Spill(Object[] parent, int i0, int parentDepth, IEqualityComparer<T> comparer)
		{
			var children = new Object[FanOut + 1];
			for (int adj = 0; adj < 4; adj++)
			{
				int iAdj = Adj(i0, adj);
				object value = parent[iAdj];
				if (value is T) {
					T t = (T)value;
					int shift = parentDepth * BitsPerLevel;
					uint hc = GetHashCode(t, comparer) >> shift;
					if ((hc & Mask) == i0) {
						bool @true = Put(ref children, ref t, hc >> BitsPerLevel, parentDepth + 1, comparer, false);
						Debug.Assert(@true);
						ClearTAt(parent, iAdj);
					}
				}
			}

			var old = parent[i0];
			parent[i0] = children;
			//Increment(ref slots[FanOut].Value);

			if (old != DeletedFlag.Value) {
				T leftover = (T)old;
				// oops, the item that used to occupy this slot has been displaced.
				// re-insert it (in special cases this can cause another spill).
				int shift = parentDepth * BitsPerLevel;
				uint hc = GetHashCode(leftover, comparer) >> shift;
				bool @true = Put(ref parent, ref leftover, hc, parentDepth, comparer, false);
				Debug.Assert(@true);
			}
			return children;
		}

		/// <summary>
		/// Marks a slot with the "DeletedFlag" and decrements the "items 
		/// remaining" counter, if any, at the end of the slot array.
		/// </summary><remarks>
		/// After implementing InternalSet, the unit tests found a flaw in my
		/// design. When adding an item to the set, Put() takes the first free 
		/// slot: e.g. if the 'home' index is 9, Put() uses slot 9 if it is 
		/// empty, otherwise it tries slots 0xA, 0xB and 0xC in that order. But 
		/// what if the item being added is a duplicate? If removals have 
		/// happened, it's possible that there could be an "early" free slot as
		/// well as a duplicate. For instance, imagine that the root node 
		/// contains two items whose hashcodes both end in 0x9, and these occupy 
		/// slots 9 and 0xA. Then the user deletes the item in slot 9, and 
		/// attempts to add a copy of the item that is already in slot 0xA. 
		/// Since this item's hashcode ends in 0x9, Put() immediately puts it in
		/// slot 9, and the set will contain a duplicate!
		/// <para/>
		/// At first I wanted to solve this problem during removals, by moving
		/// items into "better" locations. In this example, when the item in 
		/// slot 9 is removed, the item in slot 0xA would be moved to slot 9. In 
		/// general, a series of move-left operations would be required:
		/// <para/>
		/// Before moving (assuming Adj(i,n) = (i+n)&Mask):
		/// indexes      [0|1|2|3|4|5|6|7|8|9|A|B|C|D|E|F]
		/// subhashcodes |E| | |3|4| |6|6| |*|9|A|A|C|C|C|
		/// <para/>
		/// After moving:
		/// indexes      [0|1|2|3|4|5|6|7|8|9|A|B|C|D|E|F]
		/// subhashcodes | | | |3|4| |6|6| |9|A|A|C|C|C|E|
		/// <para/>
		/// This plan had an annoying flaw, however, because the Enumerator has 
		/// a RemoveCurrent() method which allows the enumerator to remain valid
		/// when an item is removed (RemoveCurrent is used by IntersectWith().)
		/// The item marked '*' might be removed by an Enumerator, so imagine
		/// that the '*' represents the Current item that the Enumerator points
		/// to. As you call MoveNext(), the Enumerator iterates left-to-right
		/// through the node. The problem is the value with subhashcode 'E'. 
		/// Originally it was at the beginning of the node, now it has moved to
		/// the end; consequently it will be enumerated twice!
		/// <para/>
		/// I decided this was unacceptable, so instead I invented DeletedFlag.
		/// The DeletedFlag tells Put() that one of these "move" operations is
		/// required, and Put() does it. This solves the Enumerator problem, since
		/// you are not supposed to insert new items while enumerating.
		/// </remarks>
		static void ClearTAt(Object[] slots, int i)
		{
			Debug.Assert(!IsFrozen(slots));
			Debug.Assert(slots[i] is T);
			slots[i] = DeletedFlag.Value;
			AutoDecrement(ref slots[slots.Length-1]);
		}
		static void AutoDecrement(ref object count)
		{
			Debug.Assert(count != FrozenFlag.Value);
			if (count != null) {
				int dec = (int)count - 1;
				if ((uint)dec < (uint)Counter.Length)
					count = Counter[dec];
				else if (dec < 0)
					count = null;
				else
					count = (object)dec;
			}
		}

		#endregion

		#region Remove() and helpers

		/// <summary>Removes an item from the set.</summary>
		/// <returns>true if the item was removed, false if it was not found.</returns>
		public bool Remove(ref T item, IEqualityComparer<T> comparer)
		{
			if (_root == null)
				return false;
			return Remove(ref _root, ref item, GetHashCode(item, comparer), 0, comparer, false) != RemoveResult.NotFound;
		}
		enum RemoveResult
		{
			NotFound = 0,    // could not remove item
			RemovedHere = 1, // item was removed, and the immediate child node has changed
			Removed = 2,     // the item was removed but no action is required in the parent
		}
		private RemoveResult Remove(ref Object[] slots, ref T item, uint hc, int depth, IEqualityComparer<T> comparer, bool frozenImplied)
		{
			int i = (int)hc & Mask;
			if (QuickGet(slots[i], ref item, comparer)) {
				slots = ClearTAt(ref slots, i, frozenImplied);
				return RemoveResult.RemovedHere;
			}

			if (depth < MaxDepth) {
				Object[] children = slots[i] as Object[];
				if (children != null) {
					var old = children;
					bool frozen = frozenImplied || IsFrozen(slots);
					var r = Remove(ref children, ref item, hc >> BitsPerLevel, depth + 1, comparer, frozen);
					if (r != RemoveResult.NotFound) {
						if (frozen) {
							Debug.Assert(old != children);
							Thaw(ref slots);
							slots[i] = children;
						} else if (old != children)
							slots[i] = children;
						if (r == RemoveResult.RemovedHere)
							r = DetectEmpty(children, ref slots[i]);
					}
					return r;
				} else {
					int iAdj;
					
					if (QuickGet(slots[iAdj = Adj(i, 1)], ref item, comparer) ||
						QuickGet(slots[iAdj = Adj(i, 2)], ref item, comparer) ||
						QuickGet(slots[iAdj = Adj(i, 3)], ref item, comparer)) {
						ClearTAt(ref slots, iAdj, frozenImplied);
						return RemoveResult.RemovedHere;
					}
				}
			} else {
				for (i = 0; i < slots.Length - 1; i++) {
					if (QuickGet(slots[i], ref item, comparer)) {
						ClearTAt(ref slots, i, frozenImplied);
						return RemoveResult.RemovedHere;
					}
				}
			}
			return RemoveResult.NotFound;
		}

		private static Object[] ClearTAt(ref Object[] slots, int i, bool frozenImplied)
		{
			if (frozenImplied || IsFrozen(slots))
				Thaw(ref slots);
			ClearTAt(slots, i);
			return slots;
		}

		private static RemoveResult DetectEmpty(Object[] children, ref Object parent)
		{
			// Each non-frozen node can have an item counter in the last slot,
			// which counts the number of non-empty entries. Since the counter is 
			// a boxed int, keeping it up-to-date is more expensive than a normal 
			// int (plus it's at the end of the array, which is more likely to be
			// a cache miss). So, to optimize the Add() operation, Add() does not 
			// update the counter, only Remove() does. Therefore, the counter can
			// be lower than the true count, but never higher.

			Debug.Assert(!IsFrozen(children));
			object countObj = children[children.Length - 1];
			int count = 0, last = 0;
			if (countObj == null)
				count = RefreshCounter(children, ref last);
			else
				count = (int)countObj;
			if (count > 1)
				return RemoveResult.Removed;
			if (countObj != null) {
				// Don't trust a low counter unless we counted it just now. Plus,
				// if count==1 we need to learn the index of the remaining item.
				count = RefreshCounter(children, ref last);
			}
			if (count == 0) {
				parent = DeletedFlag.Value;
				return RemoveResult.RemovedHere;
			} else if (count == 1 && children[last] is T) {
				parent = children[last];
				return RemoveResult.RemovedHere;
			}
			return RemoveResult.Removed;
		}
		private static int RefreshCounter(Object[] children, ref int lastUsed)
		{
			int count = 0;
			for (int i = 0; i < children.Length - 1; i++) {
				object v = children[i];
				if (v != null && v != DeletedFlag.Value) {
					count++;
					lastUsed = i;
				}
			}
			children[children.Length - 1] = count;
			return count;
		}

		#endregion

		#region Find() and helper

		public bool Find(ref T s, IEqualityComparer<T> comparer)
		{
			if (_root == null)
				return false;
			Object[] slots = _root;
			uint hc = (uint)s.GetHashCode();

			for (int depth = 0; ; depth++) {
				int i = (int)hc & Mask;
				Object slot = slots[i];
				
				if (Equals(slot, ref s, comparer))
					return true;
				if (slot == null)
					return false;

				Object[] children = slot as Object[];
				if (children != null) {
					slots = children;
					hc >>= BitsPerLevel;
					depth++;
					continue;
				} else if (depth >= MaxDepth) {
					for (i = 0; i < slots.Length; i++)
						if (Equals(slots[i], ref s, comparer))
							return true;
				} else {
					slot = slots[Adj(i, 1)];
					if (slot == null) return false;
					if (Equals(slot, ref s, comparer)) return true;
					slot = slots[Adj(i, 2)];
					if (slot == null) return false;
					if (Equals(slot, ref s, comparer)) return true;
					slot = slots[Adj(i, 3)];
					if (slot == null) return false;
					if (Equals(slot, ref s, comparer)) return true;
				}
				return false;
			}
		}
		
		#endregion

		#region Enumerator

		public struct Enumerator : IEnumerator<T>
		{
			internal T _current;
			internal InternalList<Object[]> _stack;
			int _i;   // index in the current node
			uint _hc; // stack of indexes, which are also partial hashcodes

			public Enumerator(InternalSet<T> set) : this()
			{
				Reset(set);
			}
			internal Enumerator(int stackCapacity) : this(Empty)
			{
				_stack.Capacity = stackCapacity;
			}

			/// <summary>Allows one to re-use the stack space allocated to an 
			/// existing Enumerator, for the same set or a new set. Also can
			/// be used with an Enumerator whose constructor was never called.</summary>
			/// <remarks>In general you can't Reset without providing the set, 
			/// because when you reach the end, _stack becomes empty and the 
			/// enumerator no longer has a reference to the set's root node.</remarks>
			public void Reset(InternalSet<T> set)
			{
				if (_stack.InternalArray == null) {
					// We don't use Clear() normally, because it frees the array.
					// However, Resize() requires InternalArray to be non-null.
					_stack.Clear();
				} else
					_stack.Resize(0, false);
				_i = -1;
				_hc = 0;
				_current = default(T);
				if (set._root != null) {
					_stack.Capacity = 8;
					_stack.Add(set._root);
				}
			}

			public bool MoveNext()
			{
				if (_stack.IsEmpty)
					return false;
				for (;;) {
					_i++;
					Object[] a = _stack.Last;
					if ((uint)_i + 1 >= (uint)a.Length) {
						Pop();
						if (_stack.IsEmpty)
							return false;
						continue;
					}

					object value = a[_i];
					if (value == null || value == DeletedFlag.Value)
						continue;
					if (value is T) {
						_current = (T)a[_i];
						return true;
					}
					var children = (Object[])value;
					Debug.Assert(_i < FanOut && _stack.Count < 8);
					_hc |= (uint)_i << ((_stack.Count - 1) * BitsPerLevel);
					_stack.Add(children);
					// Just in case user modifies/deletes Current, copy Frozen flag
					PropagateFrozenFlag(a, children);
					_i = -1;
				}
			}
			private void Pop()
			{
				int shift = (_stack.Count - 2) * BitsPerLevel;
				_stack.Pop();
				_i = (int)(_hc >> shift);
				_hc &= (1u << shift) - 1u;
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
				Object[] a = AutoThawCurrentNode(ref set);
				Debug.Assert(value != null);
				if (comparer != null && !comparer.Equals((T)a[_i], value))
					throw new ArgumentException("SetCurrentValue: the new key does not match the old key.");
				a[_i] = value;
			}

			/// <summary>Removes the current item from the set, and moves to the 
			/// next item.</summary>
			/// <returns>As with <see cref="MoveNext"/>, returns true if there is 
			/// another item after the current one and false if not.</returns>
			public bool RemoveCurrent(ref InternalSet<T> set)
			{
				Object[] child = AutoThawCurrentNode(ref set);
				ClearTAt(child, _i);

				int depth = _stack.Count - 2;
				while (depth >= 0) {
					Object[] parent;
					int i = GetCurrentIndexAt(depth, out parent);
					Debug.Assert(parent[i] == child);
					if (DetectEmpty(child, ref parent[i]) == RemoveResult.RemovedHere) {
						Pop();
						_i--;
					} else
						break;
					depth--;
					child = parent;
				}
				
				return MoveNext();
			}

			private int GetCurrentIndexAt(int level, out Object[] parent)
			{
				parent = _stack[level];
				return (int)(_hc >> (level * BitsPerLevel)) & Mask;
			}

			private Object[] AutoThawCurrentNode(ref InternalSet<T> set)
			{
				int i = _stack.Count - 1;
				Object[] current = _stack[i];
				if (IsFrozen(current)) {
					// Find the topmost frozen node thaw starting there.
					while (i > 0 && IsFrozen(_stack[i - 1])) i--;

					for (; i < _stack.Count; i++) {
						Object[] old = current = _stack[i];
						Thaw(ref current);
						_stack[i] = current;
						if (i == 0)
							set._root = current;
						else {
							Object[] parent;
							int indexInParent = GetCurrentIndexAt(i - 1, out parent);
							Debug.Assert(parent[indexInParent] == old);
							parent[indexInParent] = current;
						}
					}
				}
				return current;
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
				if (IsFrozen(_root))
					_root = null;
				else
					for (int i = 0; i < _root.Length; i++)
						_root[i] = null;
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
		public int ExceptWith(IEnumerable<T> other, IEqualityComparer<T> thisComparer)
		{
			int removed = 0;
			foreach (var t in other) {
				var t2 = t;
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
				var t = e.Current;
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
		public int SymmetricExceptWith(IEnumerable<T> other, IEqualityComparer<T> comparer, bool xorDuplicates = true)
		{
			if (!xorDuplicates) {
				var set = new InternalSet<T>(other, comparer);
				return SymmetricExceptWith(other, comparer);
			} else {
				int delta = 0;
				foreach (var t in other) {
					var t2 = t;
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

	struct FrozenFlag {
		public static readonly object Value = new FrozenFlag();
	}
	struct DeletedFlag {
		public static readonly object Value = new DeletedFlag();
	}

#endif
}
