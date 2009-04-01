/*
	VList processing library: Copyright 2008 by David Piepgrass

	This library is free software: you can redistribute it and/or modify it 
	it under the terms of the GNU Lesser General Public License as published 
	by the Free Software Foundation, either version 3 of the License, or (at 
	your option) any later version. It is provided without ANY warranties.

	If you did not receive a copy of the License with this library, you can 
	find it at http://www.gnu.org/licenses/
*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using NUnit.Framework;
using System.Threading;
using Loyc.Runtime;

namespace Loyc.Utilities
{
    /// <summary>
    /// A reference to a VList, a so-called persistent list data structure.
    /// </summary>
    /// <remarks>See the remarks of <see cref="VListBlock{T}"/> for more information
    /// about VLists. Items are normally added to, and removed from, the front of a 
	/// VList or to the back of an RVList; adding, removing or changing items at any 
	/// other position is inefficient. You can call ToRVList() to convert a VList to 
	/// its equivalent RVList, which is a reverse-order view of the same list that 
	/// shares the same memory.</remarks>
	[DebuggerTypeProxy(typeof(CollectionDebugView<>)),
	 DebuggerDisplay("Count = {Count}")]
	public struct VList<T> : IList<T>, ICloneable
	{
		internal VListBlock<T> _block;
		internal int _localCount;

		#region Constructors

		internal VList(VListBlock<T> block, int localCount)
		{
			_block = block;
			_localCount = localCount;
		}
		public VList(T firstItem)
		{
			_block = new VListBlockOfTwo<T>(firstItem, false);
			_localCount = 1;
		}
		public VList(T itemZero, T itemOne)
		{
			// Reverse order when constructing block because the second argument is
			// conceptually added second, so it will be at index [0].
			_block = new VListBlockOfTwo<T>(itemOne, itemZero, false);
			_localCount = 2;
		}
		
		#endregion

		#region Obtaining sublists
		
		public VList<T> WithoutFirst(int offset)
		{
			return VListBlock<T>.SubList(_block, _localCount, offset);
		}
		public VList<T> Tail
		{
			get {
				return VListBlock<T>.TailOf(this);
			}
		}
		public VList<T> PreviousIn(VList<T> largerList)
		{
			return VListBlock<T>.BackUpOnce(this, largerList);
		}
		
		#endregion

		#region Equality testing and GethashCode()

		/// <summary>Returns whether the two list references are the same.
		/// Does not compare the contents of the lists.</summary>
		public static bool operator ==(VList<T> lhs, VList<T> rhs)
		{
			return lhs._localCount == rhs._localCount && lhs._block == rhs._block;
		}
		/// <summary>Returns whether the two list references are different.
		/// Does not compare the contents of the lists.</summary>
		public static bool operator !=(VList<T> lhs, VList<T> rhs)
		{
			return lhs._localCount != rhs._localCount || lhs._block != rhs._block;
		}
		/// <summary>Returns whether the two list references are the same.
		/// Does not compare the contents of the lists.</summary>
		public override bool Equals(object rhs_)
		{
			try {
				VList<T> rhs = (VList<T>)rhs_;
				return this == rhs;
			} catch {
				return false;
			}
		}
		public override int GetHashCode()
		{
			Debug.Assert((_localCount == 0) == (_block == null));
			if (_block == null)
				return 2;
			return _block.GetHashCode() ^ _localCount;
		}
		
		#endregion

		#region AddRange, InsertRange, RemoveRange

		public VList<T> AddRange(VList<T> list) { return AddRange(list, new VList<T>()); }
		public VList<T> AddRange(VList<T> list, VList<T> excludeSubList)
		{
			this = VListBlock<T>.AddRange(_block, _localCount, list, excludeSubList);
			return this;
		}
		public VList<T> AddRange(IList<T> list)
		{
			this = VListBlock<T>.AddRange(_block, _localCount, list, false);
			return this;
		}
		public VList<T> InsertRange(int index, IList<T> list)
		{
			this = VListBlock<T>.InsertRange(_block, _localCount, list, index, false);
			return this;
		}
		public VList<T> RemoveRange(int index, int count)
		{
			if (count != 0)
				this = _block.RemoveRange(_localCount, index, count);
			return this;
		}

		#endregion

		#region Other stuff

		public T Front
		{
			get {
				return _block.Front(_localCount);
			}
		}
		public bool IsEmpty
		{
			get {
				Debug.Assert((_localCount == 0) == (_block == null));
				return _block == null;
			}
		}
		public T Pop()
		{
			if (_block == null)
				throw new InvalidOperationException("Pop: The list is empty.");
			T item = Front;
			this = WithoutFirst(1);
			return item;
		}
		/// <summary>Synonym for Add(); adds an item to the front of the list.</summary>
		public VList<T> Push(T item) { return Add(item); }

		/// <summary>Returns this list as an RVList, which effectively reverses the
		/// order of the elements.</summary>
		/// <remarks>This is a trivial operation; the RVList shares the same memory.</remarks>
		public static explicit operator RVList<T>(VList<T> list)
		{
			return new RVList<T>(list._block, list._localCount);
		}
		/// <summary>Returns this list as an RVList, which effectively reverses the
		/// order of the elements.</summary>
		/// <returns>This is a trivial operation; the RVList shares the same memory.</returns>
		public RVList<T> ToRVList()
		{
			return new RVList<T>(_block, _localCount);
		}

		/// <summary>Returns this list as a WList.</summary>
		/// <remarks>The list contents are not copied until you modify the WList.</remarks>
		public static explicit operator WList<T>(VList<T> list)
		{
			return list.ToWList();
		}
		/// <summary>Returns this list as a WList.</summary>
		/// <remarks>The list contents are not copied until you modify the WList.</remarks>
		public WList<T> ToWList()
		{
			return new WList<T>(_block, _localCount, false);
		}

		/// <summary>Gets the number of blocks used by this list.</summary>
		/// <remarks>You might look at this property when optimizing your program,
		/// because the runtime of some operations increases as the chain length 
		/// increases. This property runs in O(BlockChainLength) time. Ideally,
		/// BlockChainLength is proportional to log_2(Count), but certain VList 
		/// usage patterns can produce long chains.</remarks>
		public int BlockChainLength
		{
			get { return _block.ChainLength; }
		}

		public static readonly VList<T> Empty = new VList<T>();

		#endregion

		#region IList<T> Members

        /// <summary>Searches for the specified object and returns the zero-based
        /// index of the first occurrence (lowest index) within the entire
        /// VList.</summary>
        /// <param name="item">Item to locate (can be null if T can be null)</param>
        /// <returns>Index of the item, or -1 if it was not found.</returns>
        /// <remarks>This method determines equality using the default equality
        /// comparer EqualityComparer.Default for T, the type of values in the list.
        ///
        /// This method performs a linear search; therefore, this method is an O(n)
        /// operation, where n is Count.
        /// </remarks>
		public int IndexOf(T item)
		{
			EqualityComparer<T> comparer = EqualityComparer<T>.Default;
			int i = 0;
			foreach (T candidate in this) {
				if (comparer.Equals(candidate, item))
					return i;
				i++;
			}
			return -1;
		}

		void IList<T>.Insert(int index, T item) { Insert(index, item); }
		public VList<T> Insert(int index, T item)
		{
			_block = VListBlock<T>.Insert(_block, _localCount, item, index);
			_localCount = _block.ImmCount;
			return this;
		}

		void IList<T>.RemoveAt(int index) { RemoveAt(index); }
		public VList<T> RemoveAt(int index)
		{
			this = _block.RemoveAt(_localCount, index);
			return this;
		}

		public T this[int index]
		{
			get {
				if ((uint)index >= (uint)Count)
					throw new IndexOutOfRangeException();
				return _block[_localCount - 1 - index];
			}
			set {
				this = _block.ReplaceAt(_localCount, value, index);
			}
		}

		#endregion

		#region ICollection<T> Members

		/// <summary>Inserts an item at the front (index 0) of the VList.</summary>
		void ICollection<T>.Add(T item) { Add(item); }
		/// <summary>Inserts an item at the front (index 0) of the VList.</summary>
		public VList<T> Add(T item)
		{
			_block = VListBlock<T>.Add(_block, _localCount, item);
			_localCount = _block.ImmCount;
			return this;
		}

		void ICollection<T>.Clear() { Clear(); }
		public VList<T> Clear()
		{
			_block = null;
			_localCount = 0;
			return this;
		}

		public bool Contains(T item)
		{
			return IndexOf(item) != -1;
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			foreach (T item in this)
				array[arrayIndex++] = item;
		}

		public int Count
		{
			get {
				Debug.Assert((_localCount == 0) == (_block == null)
					|| (_localCount == 0 && _block.ImmCount == 0));
				if (_block == null)
					return 0;
				return _localCount + _block.PriorCount; 
			}
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(T item)
		{
			int i = IndexOf(item);
			if (i == -1)
				return false;
			RemoveAt(i);
			return true;
		}

		#endregion

		#region IEnumerable<T> Members

		/// <summary>Enumerator for VList; also used by WList.</summary>
		public struct Enumerator : IEnumerator<T>
		{
			// _tail: rest of the list. May include mutable items if a WList is 
			// enumerated; a VList with mutable items is never publicly exposed.
			VList<T> _tail;
			T _current;

			public Enumerator(VList<T> list) { _tail = list; _current = default(T); }
			public Enumerator(RVList<T> list) { _tail = (VList<T>)list; _current = default(T); }

			#region IEnumerator<T> Members

			public T Current
			{
				get { return _current; }
			}
			object System.Collections.IEnumerator.Current
			{
				get { return _current; }
			}
			public bool MoveNext()
			{
				if (_tail._localCount > 0) {
					_current = _tail.Front;
					_tail = _tail.Tail;
					return true;
				} else
					return false;
			}
			public void Reset()
			{
				throw new NotSupportedException();
			}

			#endregion

			public void Dispose() {}
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(this);
		}
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region ICloneable Members

		VList<T> Clone() { return this; }
		object ICloneable.Clone() { return this; }

		#endregion
	}
	
	[TestFixture]
	public class VListTests
	{
		[Test]
		public void SimpleTests()
		{
            // In this simple test, I only add and remove items from the front
            // of a VList, but forking is also tested.

			VList<int> list = new VList<int>();
			Assert.That(list.IsEmpty);
			
			// Adding to VListBlockOfTwo
			list = new VList<int>(10, 20);
			ExpectList(list, 10, 20);

			list = new VList<int>();
			list.Add(1);
			Assert.That(!list.IsEmpty);
			list.Add(2);
			ExpectList(list, 2, 1);

			// A fork in VListBlockOfTwo. Note that list2 will use two VListBlocks
			// here but list will only use one.
			VList<int> list2 = list.WithoutFirst(1);
			list2.Add(3);
			ExpectList(list, 2, 1);
			ExpectList(list2, 3, 1);

			// Try doubling list2
			list2.AddRange(list2);
			ExpectList(list2, 3, 1, 3, 1);

			// list now uses two arrays
			list.Add(4);
			ExpectList(list, 4, 2, 1);

			// Try doubling list using a different overload of AddRange()
			list.AddRange((IList<int>)list);
			ExpectList(list, 4, 2, 1, 4, 2, 1);
			list = list.WithoutFirst(3);
			ExpectList(list, 4, 2, 1);

			// Remove(), Pop()
			Assert.That(list2.Remove(3));
			ExpectList(list2, 1, 3, 1);
			Assert.That(!list2.Remove(0));
			Assert.AreEqual(1, list2.Pop());
			Assert.That(list2.Remove(3));
			ExpectList(list2, 1);
			Assert.AreEqual(1, list2.Pop());
			ExpectList(list2);
			AssertThrows<Exception>(delegate() { list2.Pop(); });

			// Add many, SubList(). This will fill 3 arrays (sizes 8, 4, 2) and use
			// 1 element of a size-16 array. Oh, and test the enumerator.
			for (int i = 5; i <= 16; i++)
				list.Add(i);
			ExpectList(list, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 2, 1);
			list2 = list.WithoutFirst(6);
			ExpectListByEnumerator(list2, 10, 9, 8, 7, 6, 5, 4, 2, 1);
			AssertThrows<IndexOutOfRangeException>(delegate() { int i = list[-1]; });
			AssertThrows<IndexOutOfRangeException>(delegate() { int i = list[15]; });
			
			// IndexOf, contains
			Assert.That(list.Contains(11));
			Assert.That(!list2.Contains(11));
			Assert.That(list[list.IndexOf(2)] == 2);
			Assert.That(list[list.IndexOf(1)] == 1);
			Assert.That(list[list.IndexOf(15)] == 15);
			Assert.That(list.IndexOf(3) == -1);

			// PreviousIn(), this[], Front
			VList<int> list3 = list2;
			Assert.AreEqual(11, (list3 = list3.PreviousIn(list))[0]);
			Assert.AreEqual(12, (list3 = list3.PreviousIn(list))[0]);
			Assert.AreEqual(13, (list3 = list3.PreviousIn(list))[0]);
			Assert.AreEqual(14, (list3 = list3.PreviousIn(list)).Front);
			Assert.AreEqual(15, (list3 = list3.PreviousIn(list)).Front);
			Assert.AreEqual(16, (list3 = list3.PreviousIn(list)).Front);
			AssertThrows<Exception>(delegate() { list3.PreviousIn(list); });

			// Tail
			Assert.AreEqual(10, (list3 = list3.WithoutFirst(6))[0]);
			Assert.AreEqual(9, (list3 = list3.Tail)[0]);
			Assert.AreEqual(8, (list3 = list3.Tail)[0]);
			Assert.AreEqual(7, (list3 = list3.Tail).Front);
			Assert.AreEqual(6, (list3 = list3.Tail).Front);
			Assert.AreEqual(5, (list3 = list3.Tail).Front);
			Assert.AreEqual(4, (list3 = list3.Tail)[0]);
			Assert.AreEqual(2, (list3 = list3.Tail)[0]);
			Assert.AreEqual(1, (list3 = list3.Tail)[0]);
			Assert.That((list3 = list3.Tail).IsEmpty);

			// list2 is still the same
			ExpectList(list2, 10, 9, 8, 7, 6, 5, 4, 2, 1);

			// ==, !=, Equals(), AddRange(a, b)
			Assert.That(!list2.Equals("hello"));
			list3 = list2;
			Assert.That(list3.Equals(list2));
			Assert.That(list3 == list2);
            // This AddRange forks the list. List2 end up with block sizes 8 (3
            // used), 8 (3 used), 4, 2.
			list2.AddRange(list2, list2.WithoutFirst(3));
			ExpectList(list2, 10,9,8,10,9,8,7,6,5,4,2,1);
			Assert.That(list3 != list2);
			
			// List3 is a sublist of list, but list2 no longer is
			Assert.That(list3.PreviousIn(list).Front == 11);
			AssertThrows<InvalidOperationException>(delegate() { list2.PreviousIn(list); });
			
			list2 = list2.WithoutFirst(3);
			Assert.That(list3 == list2);
		}

		private void AssertThrows<Type>(TestDelegate @delegate)
		{
			try {
				@delegate();
			} catch (Exception exc) {
				Assert.IsInstanceOf<Type>(exc);
				return;
			}
			Assert.Fail("Delegate did not throw '{0}' as expected.", typeof(Type).Name);
		}

		private static void ExpectList<T>(IList<T> list, params T[] expected)
		{
			Assert.AreEqual(expected.Length, list.Count);
			for (int i = 0; i < expected.Length; i++)
				Assert.AreEqual(expected[i], list[i]);
		}
		private static void ExpectListByEnumerator<T>(IList<T> list, params T[] expected)
		{
			Assert.AreEqual(expected.Length, list.Count);
			int i = 0;
			foreach (T item in list) {
				Assert.AreEqual(expected[i], item);
				i++;
			}
		}

		[Test]
		public void TestInsertRemove()
		{
			VList<int> list = new VList<int>(9);
			VList<int> list2 = new VList<int>(10, 11);
			list.Insert(1, 12);
			list.Insert(1, list2[0]);
			list.Insert(2, list2[1]);
			ExpectList(list, 9, 10, 11, 12);
			for (int i = 0; i < 9; i++)
				list.Insert(i, i);
			ExpectList(list, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);

			list2 = list;
			for (int i = 1; i <= 6; i++)
				list2.RemoveAt(i);
			ExpectList(list2, 0, 2, 4, 6, 8, 10, 12);
			ExpectList(list, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12); // unchanged

			Assert.AreEqual(0, list2.Pop());
			list2.Insert(5, -2);
			ExpectList(list2, 2, 4, 6, 8, 10, -2, 12);
			list2.Insert(5, -1);
			ExpectList(list2, 2, 4, 6, 8, 10, -1, -2, 12);
			
			// Test changing items
			list = list2;
			for (int i = 0; i < list.Count; i++)
				list[i] = i;
			ExpectList(list, 0, 1, 2, 3, 4, 5, 6, 7);
			ExpectList(list2, 2, 4, 6, 8, 10, -1, -2, 12);

			list2.Clear();
			ExpectList(list2);
			Assert.AreEqual(5, list[5]);
		}

		[Test]
		public void TestInsertRemoveRange()
		{
			VList<int> oneTwo = new VList<int>(1, 2);
			VList<int> threeFour = new VList<int>(3, 4);
			VList<int> list = oneTwo;
			VList<int> list2 = threeFour;

			ExpectList(list, 1, 2);
			list.InsertRange(1, threeFour);
			ExpectList(list, 1, 3, 4, 2);
			list2.InsertRange(2, oneTwo);
			ExpectList(list2, 3, 4, 1, 2);

			list.RemoveRange(1, 2);
			ExpectList(list, 1, 2);
			list2.RemoveRange(2, 2);
			ExpectList(list2, 3, 4);

			list.RemoveRange(0, 2);
			ExpectList(list);
			list2.RemoveRange(1, 1);
			ExpectList(list2, 3);

			list = threeFour;
			list.AddRange(oneTwo);
			ExpectList(list, 1, 2, 3, 4);
			list.InsertRange(1, list);
			ExpectList(list, 1, 1, 2, 3, 4, 2, 3, 4);
			list.RemoveRange(1, 1);
			list.RemoveRange(4, 3);
			ExpectList(list, 1, 2, 3, 4);

			list.RemoveRange(0, 4);
			ExpectList(list);

			list2.InsertRange(0, list);
			list2.InsertRange(1, list);
			ExpectList(list2, 3);
		}

		[Test]
		public void TestEmptyListOperations()
		{
			VList<int> a = new VList<int>();
			VList<int> b = new VList<int>();
			a.AddRange(b);
			a.InsertRange(0, b);
			a.RemoveRange(0, 0);
			Assert.That(!a.Remove(0));
			Assert.That(a.IsEmpty);
			Assert.That(a.WithoutFirst(0).IsEmpty);

			a.Add(1);
			Assert.That(a.WithoutFirst(1).IsEmpty);

			b.AddRange(a);
			ExpectList(b, 1);
			b.RemoveAt(0);
			Assert.That(b.IsEmpty);
			b.InsertRange(0, a);
			ExpectList(b, 1);
			b.RemoveRange(0, 1);
			Assert.That(b.IsEmpty);
			b.Insert(0, a[0]);
			ExpectList(b, 1);
			b.Remove(a.Front);
			Assert.That(b.IsEmpty);
		}
		[Test]
		public void TestMultithreadedAdds()
		{
			object @lock = new object();
			VList<int> basisList = new VList<int>();
			List<Thread> threads = new List<Thread>();
			foreach (int seed_ in new int[] { 0, 10000, 20000 })
			{
				int seed = seed_; // capture loop variable
				Thread t = new Thread(delegate()
				{
					VList<int> list;
					int count;
					for (int i = 0; i < 10000; i++) {
						lock (@lock) {
							list = basisList;
							count = list.Count;
						}

						list.Add(seed + i);
						Assert.AreEqual(count + 1, list.Count);
						Assert.AreEqual(seed + i, list.Front);

						if (seed == 0)
							list.Pop();

						lock (@lock) {
							basisList = list;
						}
					}
				});
				t.Start();
				threads.Add(t);
			}
			bool done;
			do {
				done = true;
				for (int i = 0; i < threads.Count; i++) {
					if (threads[i].IsAlive)
						done = false;
				}
			} while (!done);
			Assert.That(true); // breakpoint
		}
	}
}
