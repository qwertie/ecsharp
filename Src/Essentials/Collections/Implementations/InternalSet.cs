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
	/// InternalSet to use only a single word of memory!
	/// <para/>
	/// This data structure is inspired by Clojure's PersistentHashMap. It is 
	/// designed to be very simple and efficient; it is optimized for sets of 
	/// Symbols and therefore has a reference-equality operating mode which
	/// considers two T values equal if and only if the references point to the 
	/// same object; to use this mode, specify 'null' as the 'comparer'
	/// (<see cref="IEqualityComparer{T}"/>) argument to all methods.
	/// <para/>
	/// Unlike <see cref="InternalList{T}"/>, <c>new InternalSet&lt;T>()</c> is a 
	/// valid empty set. Nevertheless, there is a static <see cref="Empty"/> 
	/// property.
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
		// structure with Object, consider what would happen if T were Object[]: 
		// then there would be no way to tell whether a given slot held a T value 
		// or a list of children! That said, it is very unlikely that anyone would 
		// set T to Object[], and it might be worthwhile to replace Slot with 
		// Object and see if it improves performance.
		struct Slot
		{
			public object Value;
		}
		
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
			int i = (int)hc & 15;
			if (QuickPut(ref slots[i], ref item, comparer, replaceIfPresent, ref slots))
				return slots;

			if (depth > MaxDepth) {
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

			Slot[] children = slots[i].Value as Slot[];
			if (children != null) {
				var children2 = Put(children, ref item, hc >> 4, depth + 1, comparer, replaceIfPresent);
				if (children2 != children && children2 != null)
					slots[i].Value = children2;
				return children2;
			} else {
				if (!QuickPut(ref slots[Adj(i, 1)], ref item, comparer, replaceIfPresent, ref slots) &&
					!QuickPut(ref slots[Adj(i, 2)], ref item, comparer, replaceIfPresent, ref slots) &&
					!QuickPut(ref slots[Adj(i, 3)], ref item, comparer, replaceIfPresent, ref slots)) {
					children = Spill(slots, i, depth, comparer);
					var children2 = Put(children, ref item, hc >> 4, depth + 1, comparer, replaceIfPresent);
					Debug.Assert(children2 == children);
				}
				return slots;
			}
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
