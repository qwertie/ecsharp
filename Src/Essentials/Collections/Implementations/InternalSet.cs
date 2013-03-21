using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Loyc.Collections.Impl
{
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
	/// valid empty set. Nevertheless, there is a static <see cref="Empty"/> 
	/// property.
	/// <para/>
	/// This data structure supports another handy feature that I developed for
	/// <see cref="AList{T}"/>, namely fast cloning and subtree sharing. You can
	/// call <see cref="CloneFreeze"/> to freeze/clone the trie in O(1) time; 
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
	/// location in the trie, and each node of the tree always has 16 items. For 
	/// example, consider a tree with 7 items that have the following hash codes:
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
	/// share the same 4-bit sub-hashcode. In this example, both O and P have 
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
	/// set; to avoid this problem, write a better hash function.
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
	/// </remarks>
	public struct InternalSet<T> : IEnumerable<T>
	{
		public static readonly InternalSet<T> Empty = new InternalSet<T>();

		struct FrozenNode { }
		static readonly FrozenNode Frozen = new FrozenNode();
		static readonly object[] Counter = new object[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

		const int MaxDepth = 7;
		Slot[] _root;

		// You might wonder: why do I use this structure instead of simply using
		// Object directly? Well, a Slot's Value can either be a value of type T, 
		// or an array of children, type Slot[]. If we replaced this Slot 
		// structure with Object, consider what would happen if T were "Object[]": 
		// then there would be no way to tell whether a given slot held a T value 
		// or a list of children! That said, it is very unlikely that anyone would 
		// set T to Object[], and it might be worthwhile to replace Slot with 
		// Object to find out whether it improves performance. On the other hand,
		// Object[] requires one extra word of memory compared to Slot[], see
		// http://stackoverflow.com/questions/1589669/overhead-of-a-net-array
		// plus, writing to Object[] requires an extra check due to array 
		// covariance, a feature copied from Java.
		struct Slot
		{
			public object Value;
		}

		#region Helper methods

		// I put this in its own method so I can experiment with different adjacency rules: (i^n) or (i+n)&15
		static int Adj(int i, int n) { return i ^ n; }
		
		static bool Equals(Slot value, T item, IEqualityComparer<T> comparer)
		{
			if (default(T) == null && object.ReferenceEquals(value.Value, item))
				return true;
			if (comparer != null && value.Value is T)
				return comparer.Equals((T)value.Value, item);
			return false;
		}
		static bool Equals(Slot value, ref T item, IEqualityComparer<T> comparer)
		{
			if (default(T) == null && object.ReferenceEquals(value.Value, item))
				return true;
			if (comparer != null && value.Value is T) {
				var Tvalue = (T)value.Value;
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

		#endregion

		#region Add() and helpers

		/// <summary>Tries to add an item to the set, and retrieves the existing item if present.</summary>
		/// <returns>true if the item was added, false if it was already present.</returns>
		public bool Add(ref T item, IEqualityComparer<T> comparer, bool replaceIfPresent)
		{
			if (_root == null)
				_root = new Slot[17];
			var r = Put(_root, ref item, GetHashCode(item, comparer), 0, comparer, replaceIfPresent);
			if (r == null)
				return false;
			Debug.Assert(r == _root);
			return true;
		}

		static Slot[] Put(Slot[] slots, ref T item, uint hc, int depth, IEqualityComparer<T> comparer, bool replaceIfPresent)
		{
		retry:
			int i = (int)hc & 15;
			if (QuickPut(ref slots[i], ref item, comparer, replaceIfPresent, ref slots))
				return slots;

			if (depth < MaxDepth) {
				Slot[] children = slots[i].Value as Slot[];
				if (children != null) {
					var children2 = Put(children, ref item, hc >> 4, depth + 1, comparer, replaceIfPresent);
					if (children2 != children && children2 != null)
						slots[i].Value = children2;
					return children2;
				} else {
					if (!QuickPut(ref slots[Adj(i, 1)], ref item, comparer, replaceIfPresent, ref slots) &&
						!QuickPut(ref slots[Adj(i, 2)], ref item, comparer, replaceIfPresent, ref slots) &&
						!QuickPut(ref slots[Adj(i, 3)], ref item, comparer, replaceIfPresent, ref slots))
					{
						int spill_i = SelectBucketToSpill(slots, i, depth, comparer);
						Spill(slots, spill_i, depth, comparer);
						Debug.Assert(slots[spill_i] is Slot[]);
						goto retry;
					}
					return slots;
				}
			} else {
				// If extreme depth, use any available entry in the array.
				for (i = 0; i < slots.Length; i++)
					if (QuickPut(ref slots[i], ref item, comparer, replaceIfPresent, ref slots))
						return slots;
				int len = slots.Length;
				slots = InternalList.CopyToNewArray(slots, len, len << 1);
				bool @true = QuickPut(ref slots[len], ref item, comparer, replaceIfPresent, ref slots);
				Debug.Assert(@true && slots != null);
				return slots;
			}
		}
		static int SelectBucketToSpill(Slot[] slots, int i0, int depth, IEqualityComparer<T> comparer)
		{
			int[] count = new int[16];
			int max = 0, max_i = -1;

			for (int adj = 0; adj < 4; adj++)
			{
				int iAdj = Adj(i0, adj);
				object value = slots[iAdj].Value;
				Debug.Assert(value != null);
				if (value is T) {
					int hc = (int)(GetHashCode((T)value, comparer) >> (depth<<2)) & 15;
					if (++count[hc] > max) {
						max = count[hc];
						max_i = hc;
					}
				}
			}
			return max_i;
		}
		static Slot[] Spill(Slot[] slots, int i0, int parentDepth, IEqualityComparer<T> comparer)
		{
			var children = new Slot[17];
			for (int adj = 0; adj < 4; adj++)
			{
				int iAdj = Adj(i0, adj);
				object value = slots[iAdj].Value;
				Debug.Assert(value != null);
				if (value is T) {
					T t = (T)value;
					int shift = parentDepth << 2;
					uint hc = GetHashCode(t, comparer) >> shift;
					if ((hc & 15) == i0) {
						bool @true = QuickPut(ref children[(hc >> 4) & 15], ref t, comparer, true, ref children);
						Debug.Assert(@true && children != null);
						ClearTAt(slots, iAdj);
					}
				}
			}

			Debug.Assert(slots[i0].Value == null);
			slots[i0].Value = children;
			Increment(ref slots[16].Value);
			return children;
		}
		static bool QuickPut(ref Slot slot, ref T item, IEqualityComparer<T> comparer, bool replaceIfPresent, ref Slot[] slots)
		{
			if (slot.Value == null) {
				slot.Value = item;
				Increment(ref slots[16].Value);
				return true;
			} else {
				T temp = item;
				if (Equals(slot, ref item, comparer)) {
					if (replaceIfPresent)
						slot.Value = temp;
					slots = null; // this is used as a signal that the item already existed
					return true;
				}
				return false;
			}
		}
		static void ClearTAt(Slot[] slots, int i)
		{
			Debug.Assert(slots[i] is T);
			slots[i].Value = null;
			Decrement(ref slots[16].Value);
		}
		static void Increment(ref object count)
		{
			if (count == null)
				count = Counter[1];
			else
				count = Counter[(int)count + 1];
		}
		static void Decrement(ref object count)
		{
			count = Counter[(int)count - 1];
		}

		#endregion

		#region Remove() and helpers

		public bool Remove(T item, IEqualityComparer<T> comparer)
		{
			return Remove(_root, item, GetHashCode(item, comparer), 0, comparer) != RemoveResult.NotFound;
		}
		enum RemoveResult
		{
			NotFound = 0,
			RemovedHere = 1,
			Removed = 2,
		}
		private RemoveResult Remove(Slot[] slots, T item, uint hc, int depth, IEqualityComparer<T> comparer)
		{
			int i = (int)hc & 15;
			if (Equals(slots[i], item, comparer)) {
				ClearTAt(slots, i);
				return RemoveResult.RemovedHere;
			}

			Slot[] children = slots[i].Value as Slot[];
			if (children != null) {
				var r = Remove(children, item, hc >> 4, depth + 1, comparer);
				if (r == RemoveResult.RemovedHere)
					r = DetectEmpty(children, ref slots[i]);
				return r;
			} else {
				int iAdj;
				if (Equals(slots[iAdj = Adj(i, 1)], item, comparer) ||
					Equals(slots[iAdj = Adj(i, 2)], item, comparer) ||
					Equals(slots[iAdj = Adj(i, 3)], item, comparer)) {
					ClearTAt(slots, iAdj);
					return RemoveResult.RemovedHere;
				}
			}
			return RemoveResult.NotFound;
		}

		private static RemoveResult DetectEmpty(Slot[] children, ref Slot parent)
		{
			if ((int)children[16].Value == 0)
				return RemoveResult.Removed;
			// Check whether all 16 slots are empty
			//for (int i = 0; i < children.Length; i++)
			//    if (children[i].Value != null)
			//        return RemoveResult.Removed;
			// All empty, so clear the parent reference
			parent.Value = null;
			return RemoveResult.RemovedHere;
		}

		#endregion

		#region Find() and helper

		public bool Find(ref T s, IEqualityComparer<T> comparer)
		{
			if (_root == null)
				return false;
			Slot[] slots = _root;
			uint hc = (uint)s.GetHashCode();

			for (int depth = 0; ; depth++) {
				int i = (int)hc & 15;
				Slot slot = slots[i];
				if (QuickGet(slot, ref s, comparer))
					return true;

				Slot[] children = slot.Value as Slot[];
				if (children != null) {
					slots = children;
					hc >>= 4;
					depth++;
					continue;
				} else if (depth > MaxDepth) {
					for (i = 0; i < slots.Length; i++)
						if (Equals(slots[i], ref s, comparer))
							return true;
				} else {
					if (QuickGet(slots[Adj(i, 1)], ref s, comparer) ||
						QuickGet(slots[Adj(i, 2)], ref s, comparer) ||
						QuickGet(slots[Adj(i, 3)], ref s, comparer))
						return true;
				}
				return false;
			}
		}
		private bool QuickGet(Slot slot, ref T s, IEqualityComparer<T> comparer)
		{
			if (Equals(slot, s, comparer)) {
				s = (T)slot.Value;
				return true;
			}
			return false;
		}
		
		#endregion

		#region Enumerator

		public struct Enumerator : IEnumerator<T>
		{
			InternalList<Slot[]> _stack;
			int _i;
			uint _hc;
			T _current;

			public Enumerator(InternalSet<T> set)
			{
				_stack = InternalList<Slot[]>.Empty;
				_i = 0;
				_hc = 0;
				_current = default(T);
				if (set._root != null)
					_stack.Add(set._root);
			}
			public bool MoveNext()
			{
				if (_stack.IsEmpty)
					return false;
				for (;;) {
					_i++;
					Slot[] a = _stack.Last;
					if ((uint)_i >= (uint)a.Length) {
						int shift = (_stack.Count - 2) << 2;
						_stack.RemoveLast();
						if (_stack.IsEmpty)
							return false;
						_i = (int)(_hc >> shift);
						_hc &= (1u << shift) - 1u;
						continue;
					}

					object value = a[_i].Value;
					if (value == null)
						continue;
					if (value is T) {
						_current = (T)a[_i].Value;
						return true;
					}
					var children = (Slot[])value;
					Debug.Assert(_i < 16 && _stack.Count < 8);
					_hc |= (uint)_i << ((_stack.Count - 1) << 2);
					_stack.Add(children);
					_i = -1;
				}
			}
			public T Current { get { return _current; } }

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
			if (_root != null)
				for (int i = 0; i < _root.Length; i++)
					_root[i].Value = null;
		}
	}
}
