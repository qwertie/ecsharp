/*
	VList processing library: Copyright 2009 by David Piepgrass

	This library is free software: you can redistribute it and/or modify it 
	it under the terms of the GNU Lesser General Public License as published 
	by the Free Software Foundation, either version 3 of the License, or (at 
	your option) any later version. It is provided without ANY warranties.
	Please note that it is fairly complex. Therefore, it may contain bugs 
	despite my best efforts to test it.

	If you did not receive a copy of the License with this library, you can 
	find it at http://www.gnu.org/licenses/
*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Loyc.MiniTest;

namespace Loyc.Collections
{
    /// <summary>
    /// A reference to a FVList, a so-called persistent list data structure.
    /// </summary>
    /// <remarks>
	/// An <a href="http://www.codeproject.com/Articles/26171/VList-data-structures-in-C">article</a>
	/// is available online about the VList data types.
	/// <para/>
	/// See the remarks of <see cref="VListBlock{T}"/> for more information
    /// about VLists. Items are normally added to, and removed from, the front of a 
	/// FVList or to the back of an RVList; adding, removing or changing items at any 
	/// other position is inefficient. You can call ToRVList() to convert a FVList to 
	/// its equivalent RVList, which is a reverse-order view of the same list that 
	/// shares the same memory.</remarks>
	[DebuggerTypeProxy(typeof(CollectionDebugView<>)),
	 DebuggerDisplay("Count = {Count}")]
	public struct FVList<T> : IListAndListSource<T>, ICloneable<FVList<T>>, ICloneable
	{
		// BTW: Normally the invariant (_localCount == 0) == (_block == null) holds.
		// However, sometimes FVList is used internally to reference a mutable FWList 
		// or RWList. In that case it is possible that _block != null but the block
		// is empty. RVList, in contrast, is not normally used to refer to a mutable 
		// list so it can always assume (_localCount == 0) == (_block == null).

		internal VListBlock<T> _block;
		internal int _localCount;

		#region Constructors

		internal static EqualityComparer<T> EqualityComparer = VListBlock<T>.EqualityComparer;

		internal FVList(VListBlock<T> block, int localCount)
		{
			_block = block;
			_localCount = localCount;
		}
		public FVList(T firstItem)
		{
			_block = new VListBlockOfTwo<T>(firstItem, false);
			_localCount = 1;
		}
		public FVList(T itemZero, T itemOne)
		{
			// Reverse order when constructing block because the second argument is
			// conceptually added second, so it will be at index [0].
			_block = new VListBlockOfTwo<T>(itemOne, itemZero, false);
			_localCount = 2;
		}
		public FVList(T[] array)
		{
			_block = null;
			_localCount = 0;
			for (int i = array.Length-1; i > 0; i--)
				Add(array[i]);
		}
		public FVList(IList<T> list)
		{
			_block = null;
			_localCount = 0;
			AddRange(list);
		}
		
		#endregion

		#region Obtaining sublists
		
		public FVList<T> WithoutFirst(int offset)
		{
			return VListBlock<T>.SubList(_block, _localCount, offset);
		}
		/// <summary>Returns a list without the first item. If the list is empty, 
		/// an empty list is retured.</summary>
		public FVList<T> Tail
		{
			get {
				return VListBlock<T>.TailOf(this);
			}
		}
		public FVList<T> PreviousIn(FVList<T> largerList)
		{
			return VListBlock<T>.BackUpOnce(this, largerList);
		}
		public FVList<T> Last(int count)
		{
			int c = Count;
			if (count >= c)
				return this;
			if (count <= 0)
				return Empty;
			return WithoutFirst(c - count);
		}
		
		#endregion

		#region Equality testing and GethashCode()

		/// <summary>Returns whether the two list references are the same.
		/// Does not compare the contents of the lists.</summary>
		public static bool operator ==(FVList<T> lhs, FVList<T> rhs)
		{
			return lhs._localCount == rhs._localCount && lhs._block == rhs._block;
		}
		/// <summary>Returns whether the two list references are different.
		/// Does not compare the contents of the lists.</summary>
		public static bool operator !=(FVList<T> lhs, FVList<T> rhs)
		{
			return lhs._localCount != rhs._localCount || lhs._block != rhs._block;
		}
		/// <summary>Returns whether the two list references are the same.
		/// Does not compare the contents of the lists.</summary>
		public override bool Equals(object rhs_)
		{
			try {
				FVList<T> rhs = (FVList<T>)rhs_;
				return this == rhs;
			} catch(InvalidCastException) {
				return false;
			}
		}
		public override int GetHashCode()
		{
			Debug.Assert((_localCount == 0) == (_block == null));
			if (_block == null)
				return 2468; // any ol' number will do
			return _block.GetHashCode() ^ _localCount;
		}
		
		#endregion

		#region AddRange, InsertRange, RemoveRange

		public FVList<T> AddRange(FVList<T> list) { return AddRange(list, new FVList<T>()); }
		public FVList<T> AddRange(FVList<T> list, FVList<T> excludeSubList)
		{
			this = VListBlock<T>.AddRange(_block, _localCount, list, excludeSubList);
			return this;
		}
		public FVList<T> AddRange(IList<T> list)
		{
			this = VListBlock<T>.AddRange(_block, _localCount, list, false);
			return this;
		}
		public FVList<T> InsertRange(int index, IList<T> list)
		{
			this = VListBlock<T>.InsertRange(_block, _localCount, list, index, false);
			return this;
		}
		public FVList<T> RemoveRange(int index, int count)
		{
			if (count != 0)
				this = _block.RemoveRange(_localCount, index, count);
			return this;
		}

		#endregion

		#region Other stuff

		/// <summary>Returns the front item of the list (at index 0), which is the head of the list.</summary>
		public T First
		{
			get {
				return _block.Front(_localCount);
			}
		}
		public bool IsEmpty
		{
			get {
				Debug.Assert((_localCount == 0) == (_block == null)
				          || (_localCount == 0 && _block.ImmCount == 0));
				return _localCount == 0 && _block == null;
			}
		}
		/// <summary>Removes the front item (at index 0) from the list and returns it.</summary>
		public T Pop()
		{
			if (_block == null)
				throw new InvalidOperationException("Pop: The list is empty.");
			T item = First;
			this = WithoutFirst(1);
			return item;
		}
		/// <summary>Synonym for Add(); adds an item to the front of the list.</summary>
		public FVList<T> Push(T item) { return Add(item); }

		/// <summary>Returns this list as an RVList, which effectively reverses the
		/// order of the elements.</summary>
		/// <remarks>This is a trivial operation; the RVList shares the same memory.</remarks>
		public static explicit operator RVList<T>(FVList<T> list)
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

		/// <summary>Returns this list as a FWList.</summary>
		/// <remarks>The list contents are not copied until you modify the FWList.</remarks>
		public static explicit operator FWList<T>(FVList<T> list) { return list.ToWList(); }
		/// <summary>Returns this list as a FWList.</summary>
		/// <remarks>The list contents are not copied until you modify the FWList.</remarks>
		public FWList<T> ToWList()
		{
			return new FWList<T>(_block, _localCount, false);
		}

		/// <summary>Returns this list as a RWList, which effectively reverses the
		/// order of the elements.</summary>
		/// <remarks>The list contents are not copied until you modify the RWList.</remarks>
		public static explicit operator RWList<T>(FVList<T> list) { return list.ToRWList(); }
		/// <summary>Returns this list as a RWList, which effectively reverses the
		/// order of the elements.</summary>
		/// <remarks>The list contents are not copied until you modify the RWList.</remarks>
		public RWList<T> ToRWList()
		{
			return new RWList<T>(_block, _localCount, false);
		}

		/// <summary>Returns the FVList converted to an array.</summary>
		public T[] ToArray()
		{
			return VListBlock<T>.ToArray(_block, _localCount, false);
		}

		/// <summary>Gets the number of blocks used by this list.</summary>
		/// <remarks>You might look at this property when optimizing your program,
		/// because the runtime of some operations increases as the chain length 
		/// increases. This property runs in O(BlockChainLength) time. Ideally,
		/// BlockChainLength is proportional to log_2(Count), but certain FVList 
		/// usage patterns can produce long chains.</remarks>
		public int BlockChainLength
		{
			get { return _block == null ? 0 : _block.ChainLength; }
		}

		public static readonly FVList<T> Empty = new FVList<T>();

		/// <summary>Adds the specified item to the list, or 
		/// original.WithoutFirst(original.Count - Count - 1) 
		/// if doing so is equivalent.</summary>
		/// <param name="item">Item to add</param>
		/// <param name="original">An old version of the list</param>
		/// <returns>Returns this.</returns>
		/// <remarks>
		/// This method helps write functional code in which you process an input 
		/// list and produce an output list that may or may not be the same as the 
		/// input list. In case the output list is identical, you would prefer
		/// to return the original input list rather than wasting memory on a new 
		/// list. SmartAdd() helps you do this. The following method demonstrates
		/// SmartAdd() by removing all negative numbers from a list:
		/// <example>
		/// FVList&lt;int&gt; RemoveNegative(FVList&lt;int&gt; input)
		/// {
		///     var output = FVList&lt;int&gt;.Empty;
		///     // Enumerate tail-to-head
		///     foreach (int n in (RVList&lt;int&gt;)input)
		///         if (n >= 0)
		///             output.SmartAdd(n, input);
		///     return output;
		/// }
		/// </example>
		/// You could also do the same thing with input.Filter(delegate(int i) { return i; } >= 0)
		/// </remarks>
		public FVList<T> SmartAdd(T item, FVList<T> original)
		{
			return SmartAdd(item, ref original);
		}
		public FVList<T> SmartAdd(T item, ref FVList<T> original)
		{
			if (original._block != null && this._block != null)
			{
				int thisCount = _localCount;
				int oldCount = original._localCount;
				if (original._block != this._block)
				{
					thisCount += _block.PriorCount;
					oldCount += original._block.PriorCount;
				}
				if (oldCount > thisCount)
				{
					int locali = original._localCount - (oldCount - thisCount);
					if (EqualityComparer.Equals(item, original._block[locali]) &&
						original._block.SubList(locali) == this)
						return original._block.SubList(locali + 1);
				}
				original = new FVList<T>();
			}
			return Add(item);
		}

		#endregion

		#region IList<T> Members

        /// <summary>Searches for the specified object and returns the zero-based
        /// index of the first occurrence (lowest index) within the entire
        /// FVList.</summary>
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
		public FVList<T> Insert(int index, T item)
		{
			_block = VListBlock<T>.Insert(_block, _localCount, item, index);
			_localCount = _block.ImmCount;
			return this;
		}

		void IList<T>.RemoveAt(int index) { RemoveAt(index); }
		public FVList<T> RemoveAt(int index)
		{
			this = _block.RemoveAt(_localCount, index);
			return this;
		}

		public T this[int index]
		{
			get {
				return _block.FGet(index, _localCount);
			}
			set {
				this = _block.ReplaceAt(_localCount, value, index);
			}
		}
		/// <summary>Gets an item from the list at the specified index; returns 
		/// defaultValue if the index is not valid.</summary>
		public T this[int index, T defaultValue]
		{
			get {
				_block.FGet(index, _localCount, ref defaultValue);
				return defaultValue;
			}
		}

		#endregion

		#region ICollection<T> Members

		/// <summary>Inserts an item at the front (index 0) of the FVList.</summary>
		void ICollection<T>.Add(T item) { Add(item); }
		/// <summary>Inserts an item at the front (index 0) of the FVList.</summary>
		public FVList<T> Add(T item)
		{
			_block = VListBlock<T>.Add(_block, _localCount, item);
			_localCount = _block.ImmCount;
			return this;
		}

		void ICollection<T>.Clear() { Clear(); }
		public FVList<T> Clear()
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

		/// <summary>Enumerator for FVList; also used by FWList.</summary>
		public struct Enumerator : IEnumerator<T>
		{
			// _tail: rest of the list. May include mutable items if a FWList is 
			// enumerated; a FVList with mutable items is never publicly exposed.
			FVList<T> _tail;
			T _current;

			public Enumerator(FVList<T> list) { _tail = list; _current = default(T); }
			public Enumerator(RVList<T> list) { _tail = (FVList<T>)list; _current = default(T); }

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
					_current = _tail.First;
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

		#region IListSource<T> Members

		public T TryGet(int index, out bool fail)
		{
			T value = default(T);
			fail = !_block.FGet(index, _localCount, ref value);
			return value;
		}

		IRange<T> IListSource<T>.Slice(int start, int count) { return Slice(start, count); }
		public Slice_<T> Slice(int start, int count = int.MaxValue) { return new Slice_<T>(this, start, count); }
		
		#endregion 

		#region ICloneable Members

		public FVList<T> Clone() { return this; }
		object ICloneable.Clone() { return this; }

		#endregion

		#region Optimized LINQ-like methods
		// Note that unlike LINQ methods, these methods are greedy. They
		// perform the requested operation immediately, not on-demand.

		/// <summary>Applies a filter to a list, to exclude zero or more items.</summary>
		/// <param name="filter">A function that chooses which items to include
		/// (exclude items by returning false).</param>
		/// <returns>The list after filtering has been applied. The original list
		/// structure is not modified.</returns>
		/// <remarks>
		/// If the predicate keeps the first N items it is passed (which are the
		/// last items in a FVList), those N items are typically not copied, but 
		/// shared between the existing list and the new one.
		/// </remarks>
		public FVList<T> Where(Predicate<T> filter)
		{
			if (_localCount == 0)
				return this;
			else
				return _block.Where(_localCount, filter, null);
		}

		/// <summary>Filters and maps a list with a user-defined function.</summary>
		/// <param name="filter">A function that chooses which items to include
		/// in a new list, and what to change them to.</param>
		/// <returns>The list after filtering has been applied. The original list
		/// structure is not modified.</returns>
		/// <remarks>
		/// This is a smart function. If the filter does not modify the first N 
		/// items it is passed (which are the last items in a FVList), those N items 
		/// are typically not copied, but shared between the existing list and the 
		/// new one.
		/// </remarks>
		public FVList<T> WhereSelect(Func<T,Maybe<T>> filter)
		{
			if (_localCount == 0)
				return this;
			else
				return _block.WhereSelect(_localCount, filter, null);
		}
		
		/// <summary>Maps a list to another list of the same length.</summary>
		/// <param name="map">A function that transforms each item in the list.</param>
		/// <returns>The list after the map function is applied to each item. The 
		/// original FVList structure is not modified.</returns>
		/// <remarks>
		/// This method is called "Smart" because of what happens if the map
		/// doesn't do anything. If the map function returns the first N items
		/// unmodified, those N items are typically not copied, but shared between
		/// the existing list and the new one. This is useful for functional code
		/// that sometimes processes a list without modifying it at all.
		/// </remarks>
		public FVList<T> SmartSelect(Func<T, T> map)
		{
			if (_localCount == 0)
				return this;
			else
				return _block.SmartSelect(_localCount, map, null);
		}
		
		/// <summary>Maps a list to another list of the same length.</summary>
		/// <param name="map">A function that transforms each item in the list.</param>
		/// <returns>The list after the map function is applied to each item. The 
		/// original FVList structure is not modified.</returns>
		public FVList<Out> Select<Out>(Func<T, Out> map)
		{
			return VListBlock<T>.Select<Out>(_block, _localCount, map, null);
		}

		/// <summary>Transforms a list (combines filtering with selection and more).</summary>
		/// <param name="x">Method to apply to each item in the list</param>
		/// <returns>A list formed from transforming all items in the list</returns>
		/// <remarks>
		/// This is my attempt to make an optimized multi-purpose routine for
		/// transforming a FVList or RVList. It is slightly cumbersome to use,
		/// but allows you to do several common operations in one transformer
		/// method.
		/// <para/>
		/// The VListTransformer method takes two arguments: an item and its index 
		/// in the FVList or RVList. It can modify the item if desired, and then it
		/// returns a XfAction value, which indicates the action to take. Most
		/// often you will return XfAction.Drop, XfAction.Keep, XfAction.Change, 
		/// which, repectively, drop the item from the output list, copy the item 
		/// to the output list unchanged (even if you modified the item), and 
		/// copy the item to the output list (assuming you changed it).
		/// <para/>
		/// Transform() needs to know if the item changed, at least at first,
		/// because if the first items are kept without changes, then the output
		/// list can share a common tail with the input list. If the transformer
		/// method returns XfAction.Keep for every element, then the output list
		/// is exactly the same (operator== returns true).
		/// <para/>
		/// Of course, it would have been simpler just to return a boolean
		/// indicating whether to keep the item, and the Transform method itself 
		/// could check whether the item changed. But checking for equality is
		/// a tad slow in the .NET framework, because there is no bitwise 
		/// equality operator in .NET, so a virtual function would have to be 
		/// called instead to test equality, which is especially slow if T is a 
		/// value type that does not implement IEquatable(of T).
		/// <para/>
		/// The final possible action, XfAction.Repeat, is like XfAction.Change 
		/// except that Transform() calls the VListTransformer again. The second 
		/// call has the form x(~i, ref item), where ~i is the bitwise NOT of the 
		/// index i, and item is the same item that x returned the first time it 
		/// was called. On the second call, x() can return XfAction.Change again 
		/// to get a third call, if it wants.
		/// <para/>
		/// XfAction.Repeat is best explained by example. In the following
		/// examples, assume "list" is an RVList holding the numbers (1, 2, 3):
		/// <example>
		/// output = list.Transform((i, ref n) => {
		///     // This example produces (1, 1, 2, 2, 3, 3)
		///     return i >= 0 ? XfAction.Repeat : XfAction.Keep;
		/// });
		/// 
		/// output = list.Transform((i, ref n) => {
		///     // This example produces (1, 10, 2, 20, 3, 30)
		///     if (i >= 0) 
		///         return XfAction.Repeat;
		///     n *= 10;
		///     return XfAction.Change;
		/// });
		/// 
		/// output = list.Transform((i, ref n) => {
		///     // This example produces (10, 1, 20, 2, 30, 3)
		///     if (i >= 0) {
		///         n *= 10;
		///         return XfAction.Repeat;
		///     }
		///     return XfAction.Keep;
		/// });
		/// 
		/// output = list.Transform((i, ref n) => {
		///     // This example produces (10, 100, 1000, 20, 200, 30, 300)
		///     n *= 10;
		///     if (n > 1000)
		///         return XfAction.Drop;
		///     return XfAction.Repeat;
		/// });
		/// </example>
		/// And now for some examples using XfAction.Keep, XfAction.Drop and
		/// XfAction.Change. Assume list is an RVList holding the following 
		/// integers: (-1, 2, -2, 13, 5, 8, 9)
		/// <example>
		/// output = list.Transform((i, ref n) =>
		/// {   // Keep every second item: (2, 13, 8)
		///     return (i % 2) == 1 ? XfAction.Keep : XfAction.Drop;
		/// });
		/// 
		/// output = list.Transform((i, ref n) =>
		/// {   // Keep odd numbers: (-1, 13, 5, 9)
		///     return (n % 2) != 0 ? XfAction.Keep : XfAction.Drop;
		/// });
		/// 
		/// output = list.Transform((i, ref n) =>
		/// {   // Keep and square all odd numbers: (1, 169, 25, 81)
		///     if ((n % 2) != 0) {
		///         n *= n;
		///         return XfAction.Change;
		///     } else
		///         return XfAction.Drop;
		/// });
		/// 
		/// output = list.Transform((i, ref n) =>
		/// {   // Increase each item by its index: (-1, 3, 0, 16, 9, 13, 15)
		///     n += i;
		///     return i == 0 ? XfAction.Keep : XfAction.Change;
		/// });
		/// </example>
		/// </remarks>
		public FVList<T> Transform(VListTransformer<T> x)
		{
			return VListBlock<T>.Transform(_block, _localCount, x, false, null);
		}

		#endregion

	}

	[TestFixture]
	public class VListTests
	{
		[Test]
		public void SimpleTests()
		{
            // In this simple test, I only add and remove items from the front
            // of a FVList, but forking is also tested.

			FVList<int> list = new FVList<int>();
			Assert.That(list.IsEmpty);
			
			// Adding to VListBlockOfTwo
			list = new FVList<int>(10, 20);
			ExpectList(list, 10, 20);

			list = new FVList<int>();
			list.Add(1);
			Assert.That(!list.IsEmpty);
			list.Add(2);
			ExpectList(list, 2, 1);

			// A fork in VListBlockOfTwo. Note that list2 will use two VListBlocks
			// here but list will only use one.
			FVList<int> list2 = list.WithoutFirst(1);
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
			FVList<int> list3 = list2;
			Assert.AreEqual(11, (list3 = list3.PreviousIn(list))[0]);
			Assert.AreEqual(12, (list3 = list3.PreviousIn(list))[0]);
			Assert.AreEqual(13, (list3 = list3.PreviousIn(list))[0]);
			Assert.AreEqual(14, (list3 = list3.PreviousIn(list)).First);
			Assert.AreEqual(15, (list3 = list3.PreviousIn(list)).First);
			Assert.AreEqual(16, (list3 = list3.PreviousIn(list)).First);
			AssertThrows<Exception>(delegate() { list3.PreviousIn(list); });

			// Tail
			Assert.AreEqual(10, (list3 = list3.WithoutFirst(6))[0]);
			Assert.AreEqual(9, (list3 = list3.Tail)[0]);
			Assert.AreEqual(8, (list3 = list3.Tail)[0]);
			Assert.AreEqual(7, (list3 = list3.Tail).First);
			Assert.AreEqual(6, (list3 = list3.Tail).First);
			Assert.AreEqual(5, (list3 = list3.Tail).First);
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
			Assert.That(list3.PreviousIn(list).First == 11);
			AssertThrows<InvalidOperationException>(delegate() { list2.PreviousIn(list); });
			
			list2 = list2.WithoutFirst(3);
			Assert.That(list3 == list2);
		}

		private void AssertThrows<Type>(Action @delegate)
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
			FVList<int> list = new FVList<int>(9);
			FVList<int> list2 = new FVList<int>(10, 11);
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
			FVList<int> oneTwo = new FVList<int>(1, 2);
			FVList<int> threeFour = new FVList<int>(3, 4);
			FVList<int> list = oneTwo;
			FVList<int> list2 = threeFour;

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
			FVList<int> a = new FVList<int>();
			FVList<int> b = new FVList<int>();
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
			b.Remove(a.First);
			Assert.That(b.IsEmpty);

			AssertThrows<InvalidOperationException>(delegate() { a.PreviousIn(b); });
		}

		[Test]
		public void TestToArray()
		{
			FVList<int> list = new FVList<int>();
			int[] array = list.ToArray();
			Assert.AreEqual(array.Length, 0);

			array = list.Add(1).ToArray();
			ExpectList(array, 1);

			array = list.Add(2).ToArray();
			ExpectList(array, 2, 1);
			
			array = list.Add(3).ToArray();
			ExpectList(array, 3, 2, 1);

			array = list.AddRange(new int[] { 8, 7, 6, 5, 4 }).ToArray();
			ExpectList(array, 8, 7, 6, 5, 4, 3, 2, 1);
		}

		[Test]
		public void TestMultithreadedAdds()
		{
			object @lock = new object();
			FVList<int> basisList = new FVList<int>();
			List<Thread> threads = new List<Thread>();
			foreach (int seed_ in new int[] { 0, 10000, 20000 })
			{
				int seed = seed_; // capture loop variable
				Thread t = new Thread(delegate()
				{
					FVList<int> list;
					int count;
					for (int i = 0; i < 10000; i++) {
						lock (@lock) {
							list = basisList;
							count = list.Count;
						}

						list.Add(seed + i);
						Assert.AreEqual(count + 1, list.Count);
						Assert.AreEqual(seed + i, list.First);

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

		[Test]
		public void TestWhere()
		{
			FVList<int> one = new FVList<int>(3);
			FVList<int> two = one.Clone().Add(2);
			FVList<int> thr = two.Clone().Add(1);
			ExpectList(one.Where(delegate(int i) { return false; }));
			ExpectList(two.Where(delegate(int i) { return false; }));
			ExpectList(thr.Where(delegate(int i) { return false; }));
			Assert.That(one.Where(delegate(int i) { return true; }) == one);
			Assert.That(two.Where(delegate(int i) { return true; }) == two);
			Assert.That(thr.Where(delegate(int i) { return true; }) == thr);
			Assert.That(two.Where(delegate(int i) { return i==3; }) == one);
			Assert.That(thr.Where(delegate(int i) { return i==3; }) == one);
			Assert.That(thr.Where(delegate(int i) { return i>1; }) == two);
			ExpectList(two.Where(delegate(int i) { return i==2; }), 2);
			ExpectList(thr.Where(delegate(int i) { return i==2; }), 2);
		}

		[Test]
		public void TestSelect()
		{
			FVList<int> one = new FVList<int>(3);
			FVList<int> two = one.Clone().Add(2);
			FVList<int> thr = two.Clone().Add(1);
			ExpectList(thr, 1, 2, 3);

			ExpectList(one.Select(delegate(int i) { return i+1; }), 4);
			ExpectList(two.Select(delegate(int i) { return i+1; }), 3, 4);
			ExpectList(thr.Select(delegate(int i) { return i+1; }), 2, 3, 4);
			ExpectList(two.Select(delegate(int i) { return i==3 ? 3 : 0; }), 0, 3);
			ExpectList(thr.Select(delegate(int i) { return i==3 ? 3 : 0; }), 0, 0, 3);
			ExpectList(thr.Select(delegate(int i) { return i==1 ? 0 : i; }), 0, 2, 3);

			Assert.That(one.SmartSelect(delegate(int i) { return i; }) == one);
			Assert.That(two.SmartSelect(delegate(int i) { return i; }) == two);
			Assert.That(thr.SmartSelect(delegate(int i) { return i; }) == thr);
			ExpectList(one.SmartSelect(delegate(int i) { return i+1; }), 4);
			ExpectList(two.SmartSelect(delegate(int i) { return i+1; }), 3, 4);
			ExpectList(thr.SmartSelect(delegate(int i) { return i+1; }), 2, 3, 4);
			ExpectList(two.SmartSelect(delegate(int i) { return i==3 ? 3 : 0; }), 0, 3);
			ExpectList(thr.SmartSelect(delegate(int i) { return i==3 ? 3 : 0; }), 0, 0, 3);
			ExpectList(thr.SmartSelect(delegate(int i) { return i==1 ? 0 : i; }), 0, 2, 3);
			Assert.That(thr.SmartSelect(delegate(int i) { return i==1 ? 0 : i; }).WithoutFirst(1) == two);
		}

		[Test]
		public void TestTransform()
		{
			// Test transforms on 1-item lists. The helper method TestTransform() 
			// creates a list of the specified length, counting up from 1 at the 
			// tail. For instance, TestTransform(3, ...) will start with a FVList of 
			// (3, 2, 1). Its transform function always multiplies the item by 10,
			// then it returns the next action in the list. FVList<int>.Transform()
			// transforms the tail first, so for example,
			// 
			//    TestTransform(4, ..., XfAction.Keep, XfAction.Change, 
			//                          XfAction.Drop, XfAction.Keep);
			// 
			// ...should produce a result of (4, 20, 1) as a FVList, which is 
			// equivalent to the RVList (1, 20, 4).
			
			// Tests on 1-item lists
			TestTransform(1, new int[] {},   0, XfAction.Drop);
			TestTransform(1, new int[] {1},  1, XfAction.Keep);
			TestTransform(1, new int[] {10}, 0, XfAction.Change);
			TestTransform(1, new int[] {10}, 0, XfAction.Repeat, XfAction.Drop);

			// Tests on 2-item lists
			TestTransform(2, new int[] {},         0, XfAction.Drop, XfAction.Drop);
			TestTransform(2, new int[] {2},        0, XfAction.Drop, XfAction.Keep);
			TestTransform(2, new int[] {20},       0, XfAction.Drop, XfAction.Change);
			TestTransform(2, new int[] {20, 2},    0, XfAction.Drop, XfAction.Repeat, XfAction.Keep);
			TestTransform(2, new int[] {1},        1, XfAction.Keep, XfAction.Drop);
			TestTransform(2, new int[] {1, 2},     2, XfAction.Keep, XfAction.Keep);
			TestTransform(2, new int[] {1, 20},    0, XfAction.Keep, XfAction.Change);
			TestTransform(2, new int[] {1, 20},    0, XfAction.Keep, XfAction.Repeat, XfAction.Drop);
			TestTransform(2, new int[] {10},       0, XfAction.Change, XfAction.Drop);
			TestTransform(2, new int[] {10, 2},    0, XfAction.Change, XfAction.Keep);
			TestTransform(2, new int[] {10, 20},   0, XfAction.Change, XfAction.Change);
			TestTransform(2, new int[] {10,20,200},0, XfAction.Change, XfAction.Repeat, XfAction.Change);
			TestTransform(2, new int[] {10},       0, XfAction.Repeat, XfAction.Drop, XfAction.Drop);
			TestTransform(2, new int[] {10,1,2},   0, XfAction.Repeat, XfAction.Keep, XfAction.Keep);
			TestTransform(2, new int[] {10,100,20},0, XfAction.Repeat, XfAction.Change, XfAction.Change);
			TestTransform(2, new int[] {10,100,1000,2}, 0, XfAction.Repeat, XfAction.Repeat, XfAction.Change, XfAction.Keep);
			TestTransform(2, new int[] {10,100,1000,1}, 0, XfAction.Repeat, XfAction.Repeat, XfAction.Repeat, XfAction.Keep, XfAction.Drop);

			TestTransform(3, new int[] { 20, 2, 30 },   0, XfAction.Drop, XfAction.Repeat, XfAction.Keep, XfAction.Change);
			TestTransform(3, new int[] { 10, 100, 3 },  0, XfAction.Repeat, XfAction.Change, XfAction.Drop, XfAction.Keep);
			TestTransform(3, new int[] { 1, 2, 3 },     3, XfAction.Keep, XfAction.Keep, XfAction.Keep);

			TestTransform(4, new int[] { 1, 2, 40 },    2, XfAction.Keep, XfAction.Keep, XfAction.Drop, XfAction.Change);
			TestTransform(4, new int[] { 1, 2, 3, 4 },  4, XfAction.Keep, XfAction.Keep, XfAction.Keep, XfAction.Keep);
			TestTransform(4, new int[] { 1, 2, 3 },     3, XfAction.Keep, XfAction.Keep, XfAction.Keep, XfAction.Drop);
			TestTransform(4, new int[] { 1, 2, 3, 40 }, 2, XfAction.Keep, XfAction.Keep, XfAction.Keep, XfAction.Change);
		}

		private void TestTransform(int count, int[] expect, int commonTailLength, params XfAction[] actions)
		{
			FVList<int> list = new FVList<int>();
			for (int i = 0; i < count; i++)
				list.Add(i + 1);

			int counter = 0;
			FVList<int> result =
				list.Transform(delegate(int i, ref int item) {
					if (i >= 0)
						Assert.AreEqual(list[i], item);
					item *= 10;
					return actions[counter++];
				});
			
			Assert.AreEqual(counter, actions.Length);
			
			ExpectList(result.ToRVList(), expect);
			
			Assert.That(result.WithoutFirst(result.Count - commonTailLength)
			         == list.WithoutFirst(list.Count - commonTailLength));
		}
	}
}
