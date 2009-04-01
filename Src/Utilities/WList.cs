using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using NUnit.Framework;
using System.Threading;
using Loyc.Runtime;

namespace Loyc.Utilities
{
	/// <summary>Shared base class of WList and RWList.</summary>
	/// <typeparam name="T">The type of elements in the list</typeparam>
	[DebuggerTypeProxy(typeof(CollectionDebugView<>)),
	 DebuggerDisplay("Count = {Count}")]
	public abstract class WListBase<T> : IList<T>
	{
		/// <summary>Reference to VListBlock that contains items at the "front" 
		/// of the list. _block can be null if the list is empty.</summary>
		internal VListBlock<T> _block;
		/// <summary>Number of items in _block that belong to this list.</summary>
		internal int _localCount;
		/// <summary>Specifies whether this object owns the mutable part of _block.</summary>
		/// <remarks>
		/// Two WLists can have pointers to the same mutable block, but only one 
		/// can be the owner. The non-owner must not modify the block.
		/// <para/>
		/// This flag is false if _block.IsMutable is false.</remarks>
		internal bool _isOwner;

		/// <summary>Returns this list as a VList without marking all items as 
		/// immutable. This is for internal use only; a VList with mutable items 
		/// is never returned from a public method.</summary>
		internal VList<T> InternalVList { get { return new VList<T>(_block, _localCount); } }

		#region Constructors

		internal WListBase() { }
		internal WListBase(VListBlock<T> block, int localCount, bool isOwner)
		{
			_block = block;
			_localCount = localCount;
			_isOwner = isOwner;
			Debug.Assert(_localCount <= (_block == null ? 0 : _block.Capacity));
			Debug.Assert(!_isOwner || (_block != null && _block.Capacity > _block.ImmCount));
		}
		
		#endregion

		#region IList<T> Members

		public abstract T this[int index] { get; set; }
		
		/// <summary>Inserts an item at the "front" of the list, 
		/// which is index 0 for WList, or Count for RWList.</summary>
		public void Add(T item)
		{
 			VListBlock<T>.MuAdd(this, item);
		}

		/// <summary>Clears the list and frees the memory it used.</summary>
		public void Clear()
		{
			if (_block != null) {
				if (_isOwner)
					_block.MuClear(_localCount);
				_block = null;
				_localCount = 0;
				_isOwner = false;
			}
		}

		public abstract void Insert(int index, T item);
		public abstract void RemoveAt(int index);

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
			foreach (T candidate in this)
			{
				if (comparer.Equals(candidate, item))
					return i;
				i++;
			}
			return -1;
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
				if (_block == null) {
					Debug.Assert(_localCount == 0);
					return 0;
				}
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

		protected abstract IEnumerator<T> GetEnumerator2();
		IEnumerator<T> IEnumerable<T>.GetEnumerator() { return GetEnumerator2(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() 
			{ return ((IEnumerable<T>)this).GetEnumerator(); }

		#endregion

		#region Helper methods for derived class to use

		protected void AddRange(IEnumerator<T> items)
		{
			while(items.MoveNext())
				Add(items.Current);
		}

		protected void RemoveBase(int distanceFromFront)
		{
			RemoveRangeBase(distanceFromFront, 1);
		}
		protected void RemoveRangeBase(int distanceFromFront, int count)
		{
			if (distanceFromFront > 0) {
				VListBlock<T>.EnsureMutable(this, distanceFromFront + count);
				VListBlock<T>.MuMove(this, 0, count, distanceFromFront);
			}
			VListBlock<T>.MuRemoveFront(this, count);
		}

		protected void AddRangeBase(IList<T> items, bool isRWList)
		{
			InsertRangeBase(0, items, isRWList);
		}
		protected void InsertRangeBase(int distanceFromFront, IList<T> items, bool isRWList)
		{
			int count = items.Count; // (may throw NullReferenceException)
			if ((uint)distanceFromFront > (uint)Count)
				throw new IndexOutOfRangeException();

			if (items == this) {
				// Inserting a copy of the list into itself requires some care...
				if (distanceFromFront == 0)
					items = InternalVList; // this sublist won't change during the insert
				else
					items = ToVList();     // get an immutable version
			}
			VListBlock<T>.EnsureMutable(this, distanceFromFront);
			VListBlock<T>.MuAddEmpty(this, count);
			VListBlock<T>.MuMove(this, count, 0, distanceFromFront);
			if (isRWList) {
				// Add items in forward order for RWList
				for (int i = 0; i < count; i++)
					Set(distanceFromFront + count - 1 - i, items[i]);
			} else {
				// Add items in reverse order for WList
				for (int i = 0; i < count; i++)
					Set(distanceFromFront + i, items[i]);
			}
		}
		protected void InsertBase(int distanceFromFront, T item)
		{
			VListBlock<T>.EnsureMutable(this, distanceFromFront);
			VListBlock<T>.MuAddEmpty(this, 1);
			VListBlock<T>.MuMove(this, 1, 0, distanceFromFront);
			Set(distanceFromFront, item);
		}

		protected T Get(int distanceFromFront) 
			{ return _block[_localCount - 1 - distanceFromFront]; }
		protected void Set(int distanceFromFront, T item) 
			{ _block[_localCount - 1 - distanceFromFront] = item; }

		#endregion

		#region Other stuff

		/// <summary>Gets the number of blocks used by this list.</summary>
		/// <remarks>You might look at this property when optimizing your program,
		/// because the runtime of some operations increases as the chain length 
		/// increases. This property runs in O(BlockChainLength) time. Ideally,
		/// BlockChainLength is proportional to log_2(Count), but if you produced 
		/// the WList by converting it from a VList, certain VList usage patterns 
		/// can produce long chains.</remarks>
		public int BlockChainLength
		{
			get { return _block.ChainLength; }
		}

		/// <summary>Synonym for Add(); adds an item to the front of the list.</summary>
		public void Push(T item) { Add(item); }

		/// <summary>Returns this list as a VList; if this is a RWList, the order 
		/// of the elements is reversed at the same time.</summary>
		/// <remarks>This operation marks the items of the WList or RWList as 
		/// immutable. You can still modify the list afterward, but some or all
		/// of the list may have to be copied.</remarks>
		public VList<T> ToVList()
		{
			return VListBlock<T>.EnsureImmutable(_block, _localCount);
		}
		/// <summary>Returns this list as a VList; if this is a RWList, the order 
		/// of the elements is reversed at the same time.</summary>
		/// <remarks>This operation marks the items of the WList or RWList as 
		/// immutable. You can still modify the list afterward, but some or all
		/// of the list may have to be copied.</remarks>
		public static explicit operator VList<T>(WListBase<T> list)
		{
			return list.ToVList();
		}

		/// <summary>Returns this list as an RVList; if this is a WList, the order 
		/// of the elements is reversed at the same time.</summary>
		/// <remarks>This operation marks the items of the WList or RWList as 
		/// immutable. You can still modify the list afterward, but some or all
		/// of the list may have to be copied.</remarks>
		public RVList<T> ToRVList()
		{
			return VListBlock<T>.EnsureImmutable(_block, _localCount).ToRVList();
		}
		/// <summary>Returns this list as an RVList; if this is a WList, the order 
		/// of the elements is reversed at the same time.</summary>
		/// <remarks>This operation marks the items of the WList or RWList as 
		/// immutable. You can still modify the list afterward, but some or all
		/// of the list may have to be copied.</remarks>
		public static explicit operator RVList<T>(WListBase<T> list)
		{
			return list.ToRVList();
		}

		#endregion
	}

	/// <summary>
	/// WList is the mutable variant of the VList data structure.
	/// </summary>
	/// <remarks>See the remarks of <see cref="VListBlock{T}"/> for more information
	/// about VLists and WLists. It is most efficient to add items to the front of
	/// a WList (at index 0) or the back of an RWList (at index Count-1).</summary>
	public sealed class WList<T> : WListBase<T>, IList<T>, ICloneable
	{
		#region Constructors

		internal WList(VListBlock<T> block, int localCount, bool isOwner)
			: base(block, localCount, isOwner) {}
		public WList() {} // empty list is all null
		public WList(T firstItem)
		{
			_block = new VListBlockOfTwo<T>(firstItem, true);
			_localCount = 1;
		}
		public WList(T itemZero, T itemOne)
		{
			// Reverse order when constructing block because the second argument is
			// conceptually added second, so it will be at index [0].
			_block = new VListBlockOfTwo<T>(itemOne, itemZero, true);
			_localCount = 2;
		}
		
		#endregion
		
		#region AddRange, InsertRange, RemoveRange

		public void AddRange(IList<T> list) { AddRangeBase(list, false); }
		public void InsertRange(int index, IList<T> list) { InsertRangeBase(index, list, false); }
		public void RemoveRange(int index, int count)     { RemoveRangeBase(index, count); }

		#endregion

		#region IList<T> Members

		public override void Insert(int index, T item) { InsertBase(index, item); }

		public override void RemoveAt(int index) { RemoveBase(index); }

		public override T this[int index]
		{
			get {
				// TODO: consider moving range check to VListBlock
				if ((uint)index >= (uint)Count)
					throw new IndexOutOfRangeException();
				return Get(index);
			}
			set {
				if ((uint)index >= (uint)Count)
					throw new IndexOutOfRangeException();
				VListBlock<T>.EnsureMutable(this, index + 1);
				Set(index, value);
			}
		}

		#endregion

		#region IEnumerable<T> Members

		protected override IEnumerator<T> GetEnumerator2() { return GetEnumerator(); }
		public VList<T>.Enumerator GetEnumerator()
		{
			return new VList<T>.Enumerator(InternalVList);
		}
		public RVList<T>.Enumerator ReverseEnumerator()
		{
			return new RVList<T>.Enumerator(InternalVList);
		}

		#endregion

		#region ICloneable Members

		public WList<T> Clone() {
			VListBlock<T>.EnsureImmutable(_block, _localCount);
			return new WList<T>(_block, _localCount, false);
		}
		object ICloneable.Clone() { return Clone(); }

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
				return Count == 0;
			}
		}
		public T Pop()
		{
			if (_block == null)
				throw new InvalidOperationException("Pop: The list is empty.");
			T item = Front;
			RemoveAt(0);
			return item;
		}

		public VList<T> WithoutFirst(int offset)
		{
			return VListBlock<T>.EnsureImmutable(_block, _localCount - offset);
		}

		#endregion
	}
	
	[TestFixture]
	public class WListTests
	{
		[Test]
		public void SimpleTests()
		{
			// Tests simple adds and removes from the front of the list. It
			// makes part of its tail immutable, but doesn't make it mutable
			// again. Also, we test operations that don't modify the list.

			WList<int> list = new WList<int>();
			Assert.That(list.IsEmpty);
			
			// create VListBlockOfTwo
			list = new WList<int>(10, 20);
			ExpectList(list, 10, 20);

			// Add()
			list.Clear();
			list.Add(1);
			Assert.That(!list.IsEmpty);
			list.Add(2);
			Assert.AreEqual(1, list.BlockChainLength);
			list.Add(3);
			Assert.AreEqual(2, list.BlockChainLength);

			ExpectList(list, 3, 2, 1);
			VList<int> snap = list.ToVList();
			ExpectList(snap, 3, 2, 1);
			
			// AddRange(), Push(), Pop()
			list.Push(4);
			list.AddRange(new int[] { 6, 5 });
			ExpectList(list, 6, 5, 4, 3, 2, 1);
			Assert.AreEqual(list.Pop(), 6);
			ExpectList(list, 5, 4, 3, 2, 1);
			list.RemoveRange(0, 2);
			ExpectList(list, 3, 2, 1);

			// Double the list
			list.AddRange(list);
			ExpectList(list, 3, 2, 1, 3, 2, 1);
			list.RemoveRange(0, 3);

			// Fill a third block
			list.AddRange(new int[] { 9, 8, 7, 6, 5, 4 });
			list.AddRange(new int[] { 14, 13, 12, 11, 10 });
			ExpectList(list, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1);
			
			// Remove(), enumerator
			list.Remove(14);
			list.Remove(13);
			list.Remove(12);
			list.Remove(11);
			ExpectListByEnumerator(list, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1);
			
			// Indexer, Front
			Assert.That(list.Front == 10);
			AssertThrows<IndexOutOfRangeException>(delegate() { int i = list[-1]; });
			AssertThrows<IndexOutOfRangeException>(delegate() { int i = list[10]; });
			
			// IndexOf, contains
			Assert.That(list.Contains(9));
			Assert.That(list[list.IndexOf(2)] == 2);
			Assert.That(list[list.IndexOf(9)] == 9);
			Assert.That(list[list.IndexOf(7)] == 7);
			Assert.That(list.IndexOf(-1) == -1);

			// snap is still the same
			ExpectList(snap, 3, 2, 1);
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
		public void TestFork()
		{
			WList<int> A = new WList<int>();
			A.AddRange(new int[] { 5, 6, 7 });
			WList<int> B = A.Clone();
			
			A.Push(4);
			ExpectList(B, 5, 6, 7);
			ExpectList(A, 4, 5, 6, 7);
			B.Push(-4);
			ExpectList(B, -4, 5, 6, 7);
		}

		[Test]
		public void TestMutabilification()
		{
			// Make a single block mutable
			VList<int> v = new VList<int>(0, 1);
			WList<int> w = v.ToWList();
			ExpectList(w, 0, 1);
			w[0] = 2;
			ExpectList(w, 2, 1);
			ExpectList(v, 0, 1);

			// Make another block, make the front block mutable, then the block-of-2
			v.Push(-1);
			w = v.ToWList();
			w[0] = 3;
			ExpectList(w, 3, 0, 1);
			Assert.That(w.WithoutFirst(1) == v.WithoutFirst(1));
			w[1] = 2;
			ExpectList(w, 3, 2, 1);
			Assert.That(w.WithoutFirst(1) != v.WithoutFirst(1));

			// Now for a more complicated case: create a long immutable chain by
			// using a nasty access pattern, add a mutable block in front, then 
			// make some of the immutable blocks mutable.
			v = new VList<int>(6);
			v = v.Add(-1).Tail.Add(5).Add(-1).Tail.Add(4).Add(-1).Tail.Add(3);
			v = v.Add(-1).Tail.Add(2).Add(-1).Tail.Add(1).Add(-1).Tail.Add(0);
			ExpectList(v, 0, 1, 2, 3, 4, 5, 6);
			// At this point, every block in the chain has only one item (it's 
			// a linked list!) and the capacity of each block is 2.
			Assert.AreEqual(7, v.BlockChainLength);

			w = v.ToWList();
			w.AddRange(new int[] { 5, 4, 3, 2, 1 });
			Assert.AreEqual(8, w.BlockChainLength);
			Assert.AreEqual(w.Count, 12);
			ExpectList(w, 5, 4, 3, 2, 1, 0, 1, 2, 3, 4, 5, 6);
			// Indices:   0  1  2  3  4  5  6  7  8  9  10 11

			w[8] = -3;
			ExpectList(w, 5, 4, 3, 2, 1, 0, 1, 2, -3, 4, 5, 6);
			Assert.AreEqual(5, w.BlockChainLength);
		}

		[Test]
		public void TestInsertRemove()
		{
			WList<int> list = new WList<int>();
			for (int i = 0; i <= 12; i++)
				list.Insert(i, i);
			ExpectList(list, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);

			for (int i = 1; i <= 6; i++)
				list.RemoveAt(i);
			ExpectList(list, 0, 2, 4, 6, 8, 10, 12);

			Assert.AreEqual(0, list.Pop());
			list.Insert(5, -2);
			ExpectList(list, 2, 4, 6, 8, 10, -2, 12);
			list.Insert(5, -1);
			ExpectList(list, 2, 4, 6, 8, 10, -1, -2, 12);

			list.Remove(-1);
			list.Remove(12);
			list[list.Count - 1] = 12;
			ExpectList(list, 2, 4, 6, 8, 10, 12);

			// Make sure WList.Clear doesn't disturb VList
			VList<int> v = list.WithoutFirst(4);
			list.Clear();
			ExpectList(list);
			ExpectList(v, 10, 12);

			// Some simple InsertRange calls where we have to convert some 
			// immutable items to mutable
			VList<int> oneTwo = new VList<int>(1, 2);
			VList<int> threeFour = new VList<int>(3, 4);
			list = oneTwo.ToWList();
			list.InsertRange(1, threeFour);
			ExpectList(list, 1, 3, 4, 2);
			list = threeFour.ToWList();
			list.InsertRange(2, oneTwo);
			ExpectList(list, 3, 4, 1, 2);

			// More tests...
			list.RemoveRange(0, 2);
			ExpectList(list, 1, 2);
			list.InsertRange(2, new int[] { 3, 3, 4, 4, 4, 5, 6, 7, 8, 9 });
			ExpectList(list, 1, 2, 3, 3, 4, 4, 4, 5, 6, 7, 8, 9);
			list.RemoveRange(3, 3);
			ExpectList(list, 1, 2, 3, 4, 5, 6, 7, 8, 9);
			v = list.ToVList();
			list.RemoveRange(5, 4);
			ExpectList(list, 1, 2, 3, 4, 5);
			ExpectList(v,    1, 2, 3, 4, 5, 6, 7, 8, 9);
		}

		[Test]
		public void TestEmptyListOperations()
		{
			WList<int> a = new WList<int>();
			WList<int> b = new WList<int>();
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
	}
}
