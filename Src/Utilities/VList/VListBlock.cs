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
using System.Diagnostics;
using System.Threading;
using Loyc.Runtime;

namespace Loyc.Utilities
{
	/// <summary>
	/// VListBlock implements the core functionality of VList, RVList, WList and 
	/// RWList. It is not intended to be used directly.
	/// </summary><remarks>
	/// VList is a persistent list data structure described in Phil Bagwell's 2002
	/// paper "Fast Functional Lists, Hash-Lists, Deques and Variable Length
	/// Arrays". RVList is the name I (David P) give to a variant of this structure 
	/// in which the elements are considered to be in reverse order so that new
	/// elements are added at the back (end) of the list rather than at the front 
	/// (beginning). WList and RWList are the names I picked for the mutable 
	/// (Writable) variants of VList and RVList.
	/// <para/>
	/// A persistent list is a list that is normally considered immutable, so
	/// adding an item implies creating a new list rather than changing the one
	/// you've got. This is fast because persistent lists have a sort of
	/// copy-on-write semantics, so that "copying" a list is a trivial O(1)
	/// operation, but modifying a list is often less efficient than you would
	/// expect from a mutable List(of T). My implementation of VLists presents a
	/// mutable IList(of T) interface, but this is only to adhere to .NET Framework
	/// conventions. VList and RVList are value types that update their own
	/// references to the list when they are modified. Thus, "Copying" a list is
	/// done with a simple assignment statement. For example:
	/// <code lang="C#">
	/// RVList&lt;int&gt; a = new RVList&lt;int&gt;(), b = new RVList&lt;int&gt;();
	/// a.Add(1);
	/// a.Add(2);
	/// b = a;             // copy the list
	/// a.Add(3);          // a[0] is 1, a[1] is 2, a[2] is 3
	/// b.Add(97);         // b[0] is 1, b[1] is 2, b[2] is 97
	/// </code>
	/// Traditionally, this kind of behavior was accomplished with singly-linked
	/// lists, but VList does it with (in essence) a singly-linked list of arrays
	/// and thereby saves memory while allowing some operations to be done faster
	/// than they were done with linked lists. In pathological cases, however, VList
	/// can use much more memory than a linked list and degenerate so that its
	/// performance characteristics are almost as bad as a linked list. One major 
	/// problem that comes to mind is if you keep changing the last item:
	/// <code lang="C#">
	/// RVList&lt;int&gt; list = new RVList&lt;int&gt;();
	/// ... add some items to list ...
	/// for(int n;; n++)
	///     list[list.Count-1] = n;
	/// </code>
	/// Unlike, for example, a C++-based VList implementation, it is impossible for
	/// the VList or RVList, which are value types, to know if the list has been 
	/// "copied" or not. In case the list has been copied, changing any element 
	/// requires a copy to be made of the VListBlock that contains that element, as 
	/// well as any subsequent blocks (in this example, only the last block must be 
	/// copied). Thus, the above example produces a lot of garbage very quickly; in 
	/// fact the rate of garbage production is (very roughly) proportional to the 
	/// list length. The performance will be equally bad if you repeatedly remove 
	/// the last item and then re-add it.
	/// <para/>
	/// Since this kind of problem tends to get worse as the list gets larger, Phil
	/// Bagwell proposed using a two- or three-dimentional list arrangement so that
	/// no single block could exceed a certain size. I have not implemented that
	/// suggestion due to lack of free time and because I did not understand the
	/// details of his suggested implementation, but I have placed a size limit of
	/// 1024 elements on any given block. Unfortunately, this means that some
	/// operations listed below degrade toward O(N) when the list is large, most 
	/// notably including the indexer, which requires over 1000 iterations to look 
	/// up element zero in an RVList that has one million elements.
	/// <para/>
	/// Due to the slow performance you get from operations like this, I decided 
	/// to implement WList, a mutable version of the VList, which I'll discuss 
	/// later.
	/// <para/>
	/// Similarly to a persistent linked list,
	/// <ul>
	/// <li>Adding an item to the front of a VList or the end of an RVList is always
	///   O(1) in time, and often O(1) in space (though, unlike a linked list, it
	///   may be much more)</li>
	/// <li>Removing an item from the front of a VList or the end of an RVList is 
	///   O(1) in time, although space not necessarily reclaimed.</li>
	/// <li>Adding or removing an item at the end of a VList or the front of an 
	///   RVList is O(N) and requires making a copy of the entire list.</li>
	/// <li>Inserting or removing a list of M items at the end of a VList or the 
	///   front of an RVList is O(N + M).</li>
	/// <li>Changing an item at an arbitrary position should be avoided, as it
	///   performs as poorly as inserting or removing an item at that position.</li>
	/// </ul>
	/// VLists, however, offer some operations that singly-linked lists cannot 
	/// provide efficiently:
	/// <ul>
	/// <li>Access by index averages O(1) in ideal conditions</li>
	/// <li>Getting the list length is typically O(log N), but O(1) in my version</li>
	/// <li>If a sublist points somewhere within a larger list, its index within the
	///   larger list can be obtained in between O(1) and O(log N) time.
	///   Consequently, reverse enumeration is possible without creating a 
	///   temporary stack or list.</li>
	/// </ul>
	/// Also, VLists can (in the best cases) store data almost as compactly as
	/// ordinary arrays.
	/// <para/>
	/// I suspect VList(of T) and RVList(of T) almost always outperforms 
	/// LinkedList(of T) in both time and space, if you are always adding and
	/// removing items at the correct end of the list. And it should perform as 
	/// well as List(of T) in some situations while providing an illusion of 
	/// immutability that List(of T) can't. For lists of 0 to 2 items, VList and 
	/// RVList use less space than List(of T) (in fact, no object is allocated 
	/// for an empty VList or RVList.)
	/// <para/>
	/// The WList is built on the same foundation as the VList (a linked list of
	/// VListBlock objects whose size increases exponentially), but it allows 
	/// you to modify the list just like List&lt;T&gt;. WList is a hybrid 
	/// mutable-immutable data structure: a single list can be partly mutable 
	/// and partly immutable. More specifically, a WList is conceptually divided 
	/// into two "halves": the front half is mutable, and the tail half is 
	/// immutable. The two halves need not be the same size (in fact, very often 
	/// one half is zero-size).
	/// <para/>
	/// Because some or all of a WList can be immutable, a VList can be converted
	/// to a WList, or vice versa, in typically O(log N) time. If you modify a 
	/// WList after calling its ToVList() method, a portion of the list is first 
	/// copied into a mutable block and then modified, and this copy operation 
	/// typically takes O(N) time.
	/// <para/>
	/// RWList is like WList except that new items are added at index Count 
	/// instead of index zero. The head of a WList is at index 0 and is returned 
	/// from the Front property; the head of an RWList is at index Count-1 and is
	/// returned from the Back property.
	/// <para/>
	/// VListBlock implements a single "node" or "sub-array" within a VList. It
	/// contains a fixed-size array. When adding a new item to a VListBlock that
	/// is already full, a new empty VListBlock is created (with a larger array),
	/// whose _prior reference points to the old VListBlock. See Phil Bagwell's 
	/// paper (or Wikipedia) for details.
	/// <para/>
	/// VListBlock adds one new member to the structure Phil Bagwell described,
	/// PriorCount, a count of elements in other (smaller) lists to which this list
	/// is linked. This makes TotalCount an O(1) operation instead of O(log N),
	/// which is necessary so that RVList[i] and RWList[i] are O(1) on average.
	/// <para/>
	/// Independent instances of VList, RVList, WList and RWList can be accessed 
	/// from independent threads even though they may share some of the same 
	/// memory. Individual instances of these objects, however, are not 
	/// synchronized.
	/// </remarks>
	/// <typeparam name="T">The type of elements in the list</typeparam>
	public abstract class VListBlock<T>
	{
		/// <summary>number of immutable elements in our local array, plus a 
		/// "mutable" flag in bit 30.</summary>
		/// <remarks>Aside from the mutable flag, this value only increases, 
		/// never decreases.
		/// <para/>
		/// If the some or all of the block is mutable, _immCount bit 30 is set 
		/// (0x40000000), and the low bits contain the number of immutable items.
		/// In that case the total number of items in use, including mutable 
		/// items, is only known by the WList or RWList that encapsulates the 
		/// block.
		/// <para/>
		/// The mutable flag is part of this field instead of being a separate 
		/// flag for two reasons: 
		/// (1) Saving space. A separate boolean would enlarge the object 4 bytes.
		/// (2) High-performance thread safety. Instead of using locks, I use 
		///     interlocked changes to obtain thread safety.
		/// <para/>
		/// I don't know how fast or slow .NET locking is, but I assume you can't
		/// get faster than a single Interlocked.CompareExchange, so I have 
		/// designed thread safety around _immCount.
		/// <para/>
		/// I hate trying to guarantee thread safety because I don't know how to 
		/// prove correctness. I know that thread safety must be considered for 
		/// at least the following operations:
		/// <ul>
		/// <li>Adding an item at the end of an immutable VListBlock: two VLists 
		/// on different threads may try to add an item to the "front" of the same 
		/// block at the same time.</li>
		/// <li>Reserving mutable items in a list: two threads may do this at once, 
		/// or an immutable VList may add an item at the same instant.</li>
		/// </ul>
		/// Interlocked.CompareExchange() is used in both cases, which ensures 
		/// that only one thread succeeds and any threads that fail do not alter 
		/// the value of the field.
		/// <para/>
		/// A mutable block can be made immutable again by clearing bit 30. No
		/// interlocked exchange is required for this, since any thread that 
		/// notices bit 30 is set will not attempt to modify this field in the 
		/// first place.
		/// <para/>
		/// We need not worry about thread safety in order to obtain the immutable
		/// tail of a list (or equivalently, to remove items from the "front") 
		/// because that operation doesn't make use of this field (Remember, each 
		/// instance of VList has its own private _localCount.) Nor do we need to 
		/// worry about enumerating or modifying an immutable list (the latter is 
		/// just an illusion, after all). 
		/// <para/>
		/// I have not concerned myself with thread safety when a single VList 
		/// instance (whether mutable or immutable) is accessed from multiple 
		/// threads, because doing so is not supported. It occurs to me, however, 
		/// that there could be security concerns if untrusted code is given 
		/// access to any kind of VList; e.g. perhaps malicious code could 
		/// corrupt a VListBlock somehow by exploiting lack of thread safety.
		/// <para/>
		/// Theoretically you shouldn't modify an WList/RWList while it is being
		/// enumerated, but the danger is limited to an incorrect sequence of 
		/// items being returned from the enumerator; a "subList is not within 
		/// list" exception is also possible.
		/// <para/>
		/// Important things to note: 
		/// (1) once items are switched from mutable to immutable, they can never 
		///     be made mutable again, since there is no way to know if any 
		///     immutable VList references still exist.
		/// (2) mutable items always belong to exactly one WList or one RWList,
		///     but a VListBlock doesn't know what WList it belongs to. A WList 
		///     or RWList is detached from its VListBlock when Clear() is called,
		///     making the block immutable again.
		/// (3) if not all the items in a VListBlock are mutable, then the Prior 
		///     list is guaranteed to be immutable. In other words, mutable and 
		///     immutable items are not interleaved; mutable items are always at 
		///     the "front" and immutable items are always at the "back".
		/// (4) When the mutable flag is set, _immCount appears to be a very 
		///     large number. Code that uses _immCount directly instead of 
		///     calling ImmCount is taking advantage of that fact.
		/// </remarks>
		protected int _immCount;

		protected const int MutableFlag = 0x40000000;
		protected const int ImmCountMask = MutableFlag - 1;

		#region Properties

		/// <summary>Returns true if part or all of the block is mutable.</summary>
		public bool IsMutable { get { return (_immCount & MutableFlag) != 0; } }

		/// <summary>Returns the number of immutable items in all previous 
		/// blocks.</summary>
		public abstract int PriorCount { get; }
		
		/// <summary>Returns a VList representing the tail of the chain of 
		/// VListBlocks.</summary>
		/// <remarks>Warning: Normally VList can only contain a reference to an
		/// immutable list, but this property may return a reference to a 
		/// mutable block if the current block is 100% mutable. Be careful with 
		/// this value, as VList is not designed to handle mutable contents!</remarks>
		public abstract VList<T> Prior { get; }

		public VListBlock<T> PriorBlock { get { return Prior._block; } }

		/// <summary>Returns true if this block has exclusive ownership of mutable 
		/// items in the prior block. Returns false if the prior block is entirely 
		/// immutable, if we don't have ownership, or if there is no prior block.</summary>
		/// <remarks>This one's hard to explain without a diagram. Note: since 
		/// there is no independent flag to indicate ownership, the logic in this 
		/// property relies on the fact that a new mutable block is never created 
		/// until the prior block is full; if one creates a new mutable block when 
		/// there is free space but no mutable items allocated in the prior block, 
		/// this property returns false because it assumes the free space was 
		/// reserved by some other WList than the list that owns this block.</remarks>
		public bool PriorIsOwned {
			get {
				VList<T> p = Prior;
				if (p._block == null)
					return false;
				// Assert: if this block has immutables, the previous block does not.
				Debug.Assert(ImmCount == 0 || p._block.ImmCount >= p._localCount);
				bool isOwned = p._block.IsMutable && p._block.ImmCount < p._localCount;
				// Assert: if PriorIsOwned, this block has no immutables
				Debug.Assert(!isOwned || ImmCount == 0);
				return isOwned;
			}
		}

		/// <summary>Gets the number of immutable elements in-use in our local array.</summary>
		/// <remarks>Mutable items are not included in the count.</remarks>
		public int ImmCount { get { return _immCount & ImmCountMask; } }

		/// <summary>Returns the number of immutable elements in-use in the entire chain</summary>
		public int TotalCount { get { return PriorCount + (_immCount & ImmCountMask); } }

		/// <summary>Returns the maximum number of elements in this block</summary>
		public abstract int Capacity { get; }

		/// <summary>Gets/sets the specified value at the specified index of this
		/// block's array, or, if localIndex is negative, searches recursively in
		/// previous blocks for the desired index.</summary>
		/// <remarks>A VList computes localIndex as VList._localCount-1-index.
		/// <para/>
		/// VList/RVList is responsible for checking that the user's index is 
		/// valid and throwing IndexOutOfRangeException if not.
		/// <para/>
		/// The setter can only be called on mutable indices!
		/// </remarks>
		public abstract T this[int localIndex] { get; set; }

		public int ChainLength {
			get {
				int len;
				VListBlock<T> b = this;
				for(len = 1; (b = b.Prior._block) != null; len++) {}
				return len;
			}
		}

		#endregion

		#region Methods for immutable lists

		/// <summary>Inserts a new item at the "front" of a VList where localCount
		/// is the number of items currently in the VList's first block.
		/// </summary>
		public abstract VListBlock<T> Add(int localCount, T item);

		/// <summary>Adds an item to the "front" of an immutable VList.</summary>
		public static VListBlock<T> Add(VListBlock<T> self, int localCount, T item)
		{
			if (self != null)
				return self.Add(localCount, item);
			else
				return new VListBlockOfTwo<T>(item, false);
		}

		/// <summary>
		/// Returns a list in which this[localIndex-1] is the first item.
		/// Nonpositive indexes are allowed and refer to prior lists; SubList
		/// returns an empty list if localIndex is so low that it goes past the back
		/// of the list.
		/// </summary>
		/// <remarks>Warning: Normally VList can only contain a reference to an
		/// immutable list, but this method can return a reference that includes 
		/// mutable items.</remarks>
		public abstract VList<T> SubList(int localIndex);
		public static VList<T> SubList(VListBlock<T> self, int localCount, int offset)
		{
			if (offset < 0)
				throw new IndexOutOfRangeException();
			if (self == null)
				return new VList<T>();
			else {
				Debug.Assert((uint)localCount <= (uint)self._immCount);
				return self.SubList(localCount - offset);
			}
		}
		public static VList<T> TailOf(VList<T> list)
		{
			if (!list.IsEmpty)
				if (--list._localCount <= 0)
					list = list._block.Prior;
			return list;
		}

		/// <summary>Inserts a new item in a VList where localCount is the number
		/// of items in the VList's first block and distanceFromFront is the
		/// insertion position (0=front).
		/// </summary>
		/// <exception cref="IndexOutOfRangeException">distanceFromFront was out of range.</exception>
		/// <returns>The block resulting from the insert (may or may not be 'this')</returns>
		/// <remarks>This method is for use by immutable VLists only.</remarks>
		public static VListBlock<T> Insert(VListBlock<T> self, int localCount, T item, int distanceFromFront)
		{
			if (self == null) {
				Debug.Assert(localCount == 0);
				if (distanceFromFront != 0)
					throw new IndexOutOfRangeException();
				return new VListBlockOfTwo<T>(item, false);
			} else {
				if ((uint)distanceFromFront > (uint)(self.PriorCount + localCount))
					throw new IndexOutOfRangeException();
				if (distanceFromFront == 0)
					return self.Add(localCount, item);
				else {
					VList<T> front = new VList<T>(self, localCount);
					VList<T> insertAt = self.SubList(localCount - distanceFromFront);
					VListBlock<T> newBlock = Add(insertAt._block, insertAt._localCount, item);
					return newBlock.AddRange(front, insertAt);
				}
			}
		}
		
		/// <summary>Inserts a list of items in the middle of a VList, where
		/// localCount is the number of items in the VList's first block and
		/// distanceFromFront is the insertion position (0=front).
		/// </summary>
		/// <param name="isRVList">Indicates the insertion order. If isRVList==true,
		/// the items[0] is inserted first (which is appropriate for an RVList),
		/// otherwise it is inserted last (which is appropriate for a VList)</param>
		/// <exception cref="IndexOutOfRangeException">distanceFromFront was out of
		/// range.</exception>
		/// <returns>The VList containing the inserted items.</returns>
		/// <remarks>This method is for use by immutable VLists only.</remarks>
		public static VList<T> InsertRange(VListBlock<T> self, int localCount, IList<T> items, int distanceFromFront, bool isRVList)
		{
			if (self == null) {
				Debug.Assert(localCount == 0);
				if (distanceFromFront != 0)
					throw new IndexOutOfRangeException();
				return AddRange(self, localCount, items, isRVList);
			} else {
				if ((uint)distanceFromFront > (uint)(self.PriorCount + localCount))
					throw new IndexOutOfRangeException();

				VList<T> originalList = new VList<T>(self, localCount);
				VList<T> insertAt = self.SubList(localCount - distanceFromFront);
				VList<T> newList = AddRange(insertAt._block, insertAt._localCount, items, isRVList);
				newList = AddRange(newList._block, newList._localCount, originalList, insertAt);
				return newList;
			}
		}

		/// <summary>Replaces an item in a VList with another, where localCount is
		/// the number of items in the VList's first block and distanceFromFront is
		/// the element index to replace (0=front).
		/// </summary>
		/// <returns>The list resulting from the change. Note that this operation
		/// is inefficient; it aways allocates a new block.</returns>
		/// <remarks>This method is for use by immutable VLists only.</remarks>
		public VList<T> ReplaceAt(int localCount, T item, int distanceFromFront)
		{
			if ((uint)distanceFromFront >= (uint)PriorCount + localCount)
				throw new IndexOutOfRangeException();
			if (distanceFromFront == 0) {
				VList<T> list = SubList(localCount - 1);
				list.Add(item);
				return list;
			} else {
				VList<T> front = new VList<T>(this, localCount);
				VList<T> replace1 = SubList(localCount - distanceFromFront);
				VList<T> replace2 = replace1.WithoutFirst(1);
				replace2.Add(item);
				replace2._block = replace2._block.AddRange(front, replace1);
				replace2._localCount = replace2._block._immCount; // no competing threads
				return replace2;
			}
		}

		/// <summary>
		/// Removes the specified number of items from a VList where localCount is
		/// the number of items in the VList's first block, distanceFromFront is the
		/// first removal position (minimum 0) and count is the number of items to
		/// remove. Of course, the terminology used here is to be understood in the
		/// context of a VList (in which items are inserted at the front of the
		/// list).
		/// </summary>
		/// <returns>The modified list.</returns>
		/// <remarks>This method is for use by immutable VLists only.</remarks>
		public VList<T> RemoveAt(int localCount, int distanceFromFront) { return RemoveRange(localCount, distanceFromFront, 1); }
		public VList<T> RemoveRange(int localCount, int distanceFromFront, int count)
		{
			if ((uint)distanceFromFront + count > (uint)PriorCount + localCount)
				throw new IndexOutOfRangeException();
			if (distanceFromFront == 0)
				return SubList(localCount - count);
			else {
				VList<T> front = new VList<T>(this, localCount);
				VList<T> removeFront = SubList(localCount - distanceFromFront);
				VList<T> removeBack = removeFront.WithoutFirst(count);

				removeBack.AddRange(front, removeFront);
				return removeBack;
			}
		}

		/// <summary>Adds a list of items to an immutable RVList (not a VList).</summary>
		/// <remarks>This method is for use by immutable RVLists only.</remarks>
		public static RVList<T> AddRange(VListBlock<T> self, int localCount, IEnumerator<T> items)
		{
			while (items.MoveNext())
			{
				self = Add(self, localCount, items.Current);
				localCount = self._immCount; // no competing threads at this point
			}
			return new RVList<T>(self, localCount);
		}

		/// <summary>Adds a list of items to an immutable VList.</summary>
		/// <remarks>This method is for use by immutable VLists only.</remarks>
		public static VList<T> AddRange(VListBlock<T> self, int localCount, IList<T> items, bool isRVList)
		{
			int itemCount = items.Count;
			if (isRVList) {
				// Add items in forward order for RVList
				for (int i = 0; i < itemCount; i++) {
					self = Add(self, localCount, items[i]);
					localCount = self._immCount; // no competing threads
				}
			} else {
				// Add items in reverse order for VList
				for (int i = itemCount - 1; i >= 0; i--) {
					self = Add(self, localCount, items[i]);
					localCount = self._immCount; // no competing threads
				}
			}
			return new VList<T>(self, localCount);
		}

		/// <summary>Adds a range of items to a VList where localCount is the
		/// number of items in the VList's first block, front points to the
		/// beginning of the range to add and back points to the end of the range.
		/// </summary>
		/// <returns>A new list with the specified range added to it.</returns>
		/// <remarks>
		/// back.Front is NOT included in the range (in fact back can be an empty
		/// list) but front.Front is included unless front is also empty.
		/// 
		/// The elements of the range are inserted in "reverse" (from back to
		/// front) so that the order of the elements in the range is preserved
		/// (adding them front-first to our front would reverse their order).
		/// <para/>
		/// This method is for use by immutable VLists only.</remarks>
		public static VList<T> AddRange(VListBlock<T> self, int localCount, VList<T> front, VList<T> back)
		{
			if (front == back || front.IsEmpty)
				return new VList<T>(self, localCount); // no change
			else {
				back = BackUpOnce(back, front);
				VListBlock<T> newBlock = Add(self, localCount, back.Front);
				newBlock = newBlock.AddRange(front, back);
				return new VList<T>(newBlock, newBlock._immCount);
			}
		}

		/// <summary>Appends a range of items to the "front" of this block.</summary>
		/// <returns>This block, or a new block if a new block had to be allocated.</returns>
		/// <remarks>This method is for use by immutable VLists only.</remarks>
		protected VListBlock<T> AddRange(VList<T> front, VList<T> back)
		{
			Debug.Assert(!IsMutable);
			RVList<T>.Enumerator e = new RVList<T>.Enumerator(front, back);
			VListBlock<T> block = this;
			while (e.MoveNext())
				block = block.Add(block._immCount, e.Current);
			return block;
		}

		#endregion

		#region FindNextBlock and BackUpOnce

		/// <summary>
		/// Finds the block that comes before 'subList' in the direction of the
		/// larger list, 'list'.
		/// </summary>
		/// <param name="subList">Sublist of list, or an empty list.</param>
		/// <param name="list">The larger, outer list. Can be an empty list if
		/// subList is empty.</param>
		/// <param name="localCountOfSubList">The value of
		/// r._block.Prior._localCount where r is the return value, or, if r is
		/// empty, the value of list._localCount.</param>
		/// <returns>The list prior to subList, or an empty block if
		/// (1) list and subList are in the same block
		/// (2) list._localCount==0 and list._block.Prior is in the same block as subList</returns>
		/// <remarks>
		/// Because of the copy-causing-sharing-failure problem (described in a
		/// comment in RVListTests.TestSublistProblem()), FindNextBlock may have to
		/// change subList in certain cases so that it really is a sublist of list.
		/// Therefore it is a ref argument.
		/// </remarks>
		public static VList<T> FindNextBlock(ref VList<T> subList, VList<T> list, out int localCountOfSubList)
		{
			if (list._localCount == 0) {
				if (list._block != null)
					list = list._block.Prior;
				else if (!subList.IsEmpty)
					throw new InvalidOperationException(Localize.From("VListBlock.FindNextBlock: specified list is empty"));
			}
			if (subList._block == list._block) {
				if ((localCountOfSubList = list._localCount) < subList._localCount)
					throw new InvalidOperationException(Localize.From("VListBlock.FindNextBlock: subList is not within list"));
				return new VList<T>();
			} else {
				// Obtain the block in list that is in front of subList.
				VList<T> prior = list, prior2;
				for (;;) {
					if (prior._block == null) {
						// Check for the copy-causing-sharing-failure problem
						VList<T> subList2 = list.WithoutFirst(list.Count - subList.Count);
						Debug.Assert(subList2.Count == subList.Count);
						if (subList2._block.Prior == subList._block.Prior) {
							Debug.Assert(subList2._localCount == subList._localCount);
							EqualityComparer<T> comparer = EqualityComparer<T>.Default;
							bool fail = false;
							for (int i = 0; i < subList._localCount; i++)
								if (!comparer.Equals(subList._block[i], subList2._block[i])) {
									fail = true; // subList is not within list.
									break;
								}
							if (!fail) {
								// Problem detected. Compensate.
								subList = subList2;
								return FindNextBlock(ref subList, list, out localCountOfSubList);
							}
						}

						// subList is not within list.
						throw new InvalidOperationException(Localize.From("VListBlock.FindNextBlock: subList is not within list"));
					} else
						prior2 = prior._block.Prior;
					if (prior2._block == subList._block)
						break;
					prior = prior2;
				}
				localCountOfSubList = prior2._localCount;
				return prior;
			}
		}
		public static RVList<T> FindNextBlock(ref RVList<T> subList, RVList<T> list, out int localCountOfSubList)
		{
			VList<T> subList2 = new VList<T>(subList._block, subList._localCount);
			VList<T> result = FindNextBlock(ref subList2,
											new VList<T>(list._block, list._localCount),
											out localCountOfSubList);
			subList = new RVList<T>(subList2._block, subList2._localCount);
			return new RVList<T>(result._block, result._localCount);
		}
		public static VList<T> BackUpOnce(VList<T> subList, VList<T> list)
		{
			int greaterLocalCount;
			VList<T> next = FindNextBlock(ref subList, list, out greaterLocalCount);
			if (subList._localCount < greaterLocalCount) {
				subList._localCount++;
				return subList;
			} else {
				if (next._localCount == 0)
					throw new InvalidOperationException(Localize.From("VListBlock.BackUpOnce: cannot back up any more."));
				next._localCount = 1;
				return next;
			}
		}
		public static RVList<T> BackUpOnce(RVList<T> subList, RVList<T> list)
		{
			int greaterLocalCount;
			RVList<T> next = FindNextBlock(ref subList, list, out greaterLocalCount);
			if (subList._localCount < greaterLocalCount) {
				subList._localCount++;
				return subList;
			} else {
				if (next._localCount == 0)
					throw new InvalidOperationException(Localize.From("VListBlock.BackUpOnce: cannot back up any more."));
				next._localCount = 1;
				return next;
			}
		}

		#endregion

		#region Methods for mutable lists

		/// <summary>Returns an immutable VList with the specified parameters, 
		/// modifying blocks if necessary.</summary>
		/// <param name="localCount">Number of items in 'self' that belong to the 
		/// list that you want to make immutable. Nonpositive values of localCount
		/// are allowed and refer to blocks prior to 'self'.</param>
		/// <remarks>This method may change self and/or other blocks in the chain 
		/// so that the returned VList contains no mutable items.</remarks>
		public static VList<T> EnsureImmutable(VListBlock<T> self, int localCount)
		{
			// Deal with nonpositive localCount or a request for an empty list
			VList<T> prior;
			for (;;) {
				if (self == null)
					return VList<T>.Empty;
				prior = self.Prior;
				if (localCount > 0)
					break;
				self = prior._block;
				localCount += prior._localCount;
			}
			
			// Increase amount of immutable elems in this block (if necessary)
			if (self.IsMutable) {
				if (self.ImmCount < localCount) {
					// (we can't read the PriorIsOwned property after changing 
					// _immCount because we break an invariant, so read it early)
					bool priorIsOwned = self.PriorIsOwned;
					self._immCount = localCount | MutableFlag;
					
					// Make all prior blocks immutable if we own them
					VListBlock<T> cur = self;
					while (priorIsOwned) {
						Debug.Assert(prior._block.IsMutable);
						Debug.Assert(prior._block.ImmCount <= prior._localCount);
						// clear MutableFlag and make all items in prior._block immutable
						cur = prior._block;
						priorIsOwned = cur.PriorIsOwned;
						cur._immCount = prior._localCount;
						prior = cur.Prior;
					}
				}
			} else
				Debug.Assert(localCount <= self.ImmCount);

			return new VList<T>(self, localCount);
		}

		/// <summary>Ensures that at least the specified number of items at the 
		/// front of a WList or RWList are mutable and owned by the list.</summary>
		/// <param name="mutablesNeeded">Number of mutable items required.</param>
		public static void EnsureMutable(WListBase<T> w, int mutablesNeeded)
		{
			if (mutablesNeeded <= 0)
				return;

			Debug.Assert(mutablesNeeded <= w.Count);
			VList<T> cur = w.InternalVList;
			VList<T> post = new VList<T>();

			// Step one: count blocks that we can keep; return early if there are 
			// enough mutables to satisfy the caller's request (usually there are).
			if (w._isOwner) {
				for(;;) {
					int mutablesHere = cur._localCount - cur._block.ImmCount;
					Debug.Assert(mutablesHere >= 0);
					if (mutablesNeeded - mutablesHere <= 0)
						return;
					if (!cur._block.PriorIsOwned) {
						// no more mutables available.
						if (cur._block.ImmCount == 0) {
							// no need to copy the current block; go to the prior one
							post = cur;
							cur = cur._block.Prior;
							Debug.Assert(cur._block.ImmCount > 0);
						}
						break; 
					}
					// no need to copy the current block; go to the prior one
					mutablesNeeded -= mutablesHere;
					post = cur;
					cur = cur._block.Prior;
				}
			}

			// Step two: create a sufficient quantity of new mutable block(s) and 
			// copy formerly immutable data into them. Originally I'd planned to 
			// copy the data block-by-block, but this could lead to inefficiency 
			// because the block sizes do not, in general, follow a logarithmic
			// progression of sizes. A chain could look like this, for example:
			//
			//      block 0 (owned by w)
			//      |____8|
			// w -> |____7|
			//      |____6|   block 1
			//      |____5|   unowned   block 2   block 3
			//      |____4|   |____4|   unowned   unowned
			//      |____3|   |Imm_3|   |Imm_3|-->|Imm_3|   block 4
			//      |____2|-->|____2|   |____2|   |____2|   unowned
			//      |____1|   |____1|-->|____1|   |____1|-->|Imm_1|
			//      |Imm_0|   |____0|   |____0|   |____0|   |____0|
			//
			// (The location of "Imm" in each block denotes ImmCount, which is 
			// 1, 4, 4, 4, and 2 in blocks 0, 1, 2, 3, and 4 respectively. The 
			// arrow --> denotes the Prior list; for example, block 0's Prior
			// list points to block 1 and Prior._localCount==3.)
			//
			// In this example the chains have sizes of 8, 3, 2, 4, 2 from w's 
			// point of view. A progression that is more geometric would be more
			// efficient for random access: 2, 4, 8, 16 perhaps. On the other 
			// hand, we'd rather not copy more data than necessary. Now suppose 
			// we get a request for 12 mutable items--this means that the first 
			// three blocks have to be copied, and we have an opportunity to 
			// change the block sizes if desired, although there is another 
			// efficiency concern to keep in mind: we should avoid copying the
			// largest block(s) if it is not necessary to do so. In the above 
			// example, block 0 has one immutable item, so a copy must be made
			// to make the 0th item mutable. But if block 0 had no immutable 
			// items, there would be no need to copy it; we'd only need to copy 
			// blocks 1 and 2. In that case, this code would combine blocks 1 
			// and 2 into a single 5-item block.
			//
			// Right now, 'post' refers to the last 100% mutable block owned by
			// w (empty if there are none), and 'cur' refers to the first block
			// that must be replaced (because it contains immutable items that 
			// must be made mutable).
			bool frontBlockMustBeReplaced = post.IsEmpty;
			Debug.Assert (frontBlockMustBeReplaced == (cur == w.InternalVList));

			// Our next task: find the first block (stop) that we DON'T have to copy.
			Debug.Assert(!cur._block.PriorIsOwned);
			VList<T> stop = cur;
			int itemsToReplace = cur._localCount;

			stop = stop._block.Prior;
			while (!stop.IsEmpty && itemsToReplace < mutablesNeeded)
			{
				Debug.Assert(stop._localCount <= stop._block.ImmCount);
				itemsToReplace += stop._localCount;
				stop = stop._block.Prior;
			}
			Debug.Assert(itemsToReplace == cur.Count - stop.Count);

			// Now, let us create new blocks and enumerate backward through the 
			// immutables from stop to cur, copying the items to new block(s).
			// This is easy using RVList<T>.Enumerator and MuAddEmpty.
			RVList<T>.Enumerator e = new RVList<T>.Enumerator(cur, stop);
			WList<T> w_temp = new WList<T>(stop._block, stop._localCount, false);
			Debug.Assert(w_temp.Count == stop.Count);
			while (e.MoveNext()) {
				MuAddEmpty(w_temp, 1, frontBlockMustBeReplaced 
					? VListBlockArray<T>.MAX_BLOCK_LEN : itemsToReplace);
				w_temp._block[w_temp._localCount - 1] = e.Current;
				itemsToReplace--;
			}
			Debug.Assert(itemsToReplace == 0);
			Debug.Assert(frontBlockMustBeReplaced || w_temp._localCount == w_temp._block.Capacity);

			// Cleanup: if w owns cur, relinquish ownership of cur. This is not 
			// strictly necessary, but occasionally it allows some other VList or 
			// WList to use the list after we release it.
			bool w_owns_cur = cur._block == w._block ? w._isOwner : post._block.PriorIsOwned;
			if (w_owns_cur)
				cur._block.MuClear(cur._localCount);

			// Finally, configure post._block.Prior or w to point to w_temp.
			Debug.Assert(w_temp._isOwner);
			Debug.Assert(w_temp.Count == cur.Count);
			if (frontBlockMustBeReplaced) {
				w._block = w_temp._block;
				w._localCount = w_temp._localCount;
				w._isOwner = w_temp._isOwner;
			} else
				((VListBlockArray<T>)post._block)._prior = 
					w_temp.InternalVList;
		}

		public static int MutableCount(WListBase<T> w)
		{
			if (!w._isOwner)
				return 0;
			
			int count = 0;
			VList<T> cur = new VList<T>(w._block, w._localCount);
			for (;;) {
				Debug.Assert(cur._localCount >= cur._block.ImmCount);
				count += cur._localCount - cur._block.ImmCount;
				if (!cur._block.PriorIsOwned)
					return count;
				cur = cur._block.Prior;
			}
		}

		/// <summary>Clears all mutable items in this chain, and clears the mutable 
		/// flag. If this block owns mutable items in prior blocks, they are 
		/// cleared too.</summary>
		/// <remarks>Clearing items is unnecessary if ImmCount is zero, as there 
		/// there are no shared copies and the caller is going to discard the block,
		/// so it'll be garbage anyway.</remarks>
		public abstract void MuClear(int localCountWithMutables);

		public static void MuAdd(WListBase<T> w, T item)
		{
			MuAddEmpty(w, 1, VListBlockArray<T>.MAX_BLOCK_LEN);
			w._block[w._localCount - 1] = item;
		}

		public static void MuAddEmpty(WListBase<T> w, int count) 
			{ MuAddEmpty(w, count, VListBlockArray<T>.MAX_BLOCK_LEN); }

		/// <summary>Adds empty item(s) to the front of the list.</summary>
		/// <param name="w">List that needs items</param>
		/// <param name="count">Number of items to add</param>
		/// <param name="newBlockSizeLimit">Limit on size of new block(s); normally
		/// VListBlockArray.MAX_BLOCK_LEN (this parameter is used by EnsureMutable()).</param>
		/// <remarks>This method doesn't actually clear the items, because all 
		/// items that are not in use should already have been set to default(T).
		/// </remarks>
		public static void MuAddEmpty(WListBase<T> w, int count, int newBlockSizeLimit)
		{
			if (w._block == null) {
				w._block = new VListBlockOfTwo<T>();
				w._isOwner = true;
			}
			w._block.MuAddEmpty2(w, count, newBlockSizeLimit);
		}

		protected void MuAddEmpty2(WListBase<T> w, int count, int newBlockSizeLimit)
		{
			Debug.Assert(w._block == this);
			
			// First try to allocate space in the front block
			if (!w._isOwner && w._localCount == _immCount && w._localCount < Capacity)
			{
				// No WList/RWList owns this block. Let's claim it for w by 
				// atomically setting the MutableFlag in _immCount.
				Debug.Assert(!IsMutable);
				Debug.Assert(w._localCount <= ImmCount); // w._localCount == ImmCount
				int LC = w._localCount;
				if (Interlocked.CompareExchange(ref _immCount, LC | MutableFlag, LC) == LC)
					w._isOwner = true; // success
			}

			if (w._isOwner) {
				Debug.Assert(IsMutable && w._localCount >= ImmCount);
				int left = Capacity - w._localCount;
				if (count <= left) {
					w._localCount += count;
					return;
				} else {
					w._localCount += left;
					count -= left;
				}
			} else
				Debug.Assert(w._localCount <= ImmCount);
			
			// Then allocate more blocks
			while (count > 0)
			{
				int capacity = MuAllocBlock(w, newBlockSizeLimit);
				w._localCount = Math.Min(count, capacity);
				count -= capacity;
			}
		}

		/// <summary>Used by MuAddEmpty to allocate an empty mutable block.</summary>
		/// <returns>Capacity of the new block</returns>
		/// <remarks>w is changed to point to the new block (w._localCount is set to 0)</remarks>
		protected static int MuAllocBlock(WListBase<T> w, int newBlockSizeLimit)
		{
			Debug.Assert(newBlockSizeLimit > 0 && newBlockSizeLimit <= VListBlockArray<T>.MAX_BLOCK_LEN);
			Debug.Assert(!w._isOwner || w._localCount == w._block.Capacity);
			int capacity = Math.Min(newBlockSizeLimit, w.Count + 2);
			w._block = new VListBlockArray<T>(new VList<T>(w._block, w._localCount), capacity, true);
			w._localCount = 0;
			w._isOwner = true;
			return capacity;
		}

		/// <summary>Moves a series of elements from one location to another in a 
		/// mutable block.</summary>
		/// <param name="w">List to modify</param>
		/// <param name="dffFrom">Distance from front of the beginning of the block to move</param>
		/// <param name="dffTo">Distance from front of destination location</param>
		/// <param name="count">Number of elements to copy</param>
		public static void MuMove(WListBase<T> w, int dffFrom, int dffTo, int count)
		{
			if (count == 0 || dffFrom == dffTo)
				return;
			Debug.Assert(w._block != null);
			Debug.Assert(dffFrom >= 0 && dffTo >= 0);
			Debug.Assert(Math.Max(dffFrom, dffTo) + count <= MutableCount(w));

			VList<T> from = SubList(w._block, w._localCount, dffFrom);
			VList<T> to = SubList(w._block, w._localCount, dffTo);
			if (dffTo < dffFrom || dffTo - dffFrom >= count) {
				// start moving at frontmost position
				for (int i = 0; i < count; i++)
				{
					to._block[to._localCount - 1] = from._block[from._localCount - 1];
					to = to.Tail;
					from = from.Tail;
				}
			} else {
				// start moving at backmost position (slower)
				for (int i = count; i > 0; i--)
					to._block[to._localCount - i] = from._block[from._localCount - i];
			}
		}

		public static void MuRemoveFront(WListBase<T> w, int count)
		{
			if (count <= 0)
				return;
			
			// Remove mutable items (that w owns) from the front
			while(w._isOwner) {
				while (w._localCount > w._block.ImmCount) {
					w._block[--w._localCount] = default(T);
					if (--count <= 0)
						return;
				}

				// no more mutable items in this block
				if (w._block.ImmCount > 0) {
					// abandon ownership
					w._isOwner = false;
					w._block.MuClear(w._localCount);
				} else {
					// This block is empty; switch to the prior one
					VList<T> p = w._block.Prior;
					w._isOwner = w._block.PriorIsOwned;
					w._block = p._block;
					w._localCount = p._localCount;
				}
			}
			
			// Remove immutable items from the front
			VList<T> tail = SubList(w._block, w._localCount, count);
			w._block = tail._block;
			w._localCount = tail._localCount;
		}

		#endregion

		#region Other stuff

		/// <summary>Returns the "front" item in a VList/WList associated with 
		/// this block (or back item of a RVList) where localCount is the 
		/// number of items in the VList's first block.
		/// </summary>
		public abstract T Front(int localCount);

		/// <summary>Converts any kind of VList to an array, quickly.</summary>
		public static T[] ToArray(VListBlock<T> self, int localCount, bool isRList)
		{
			Debug.Assert(localCount >= 0);
			
			if (self == null)
				return new T[0];
			
			T[] array = new T[localCount + self.PriorCount];
			VList<T> p;

			if (isRList) {
				int offset = self.PriorCount;
				do {
					p = self.Prior;
					self.BlockToArray(array, offset, localCount, isRList);
					localCount = p._localCount;
					self = p._block;
					offset -= localCount;
				} while (self != null);
				Debug.Assert(offset == 0);
			} else {
				int offset = 0;
				do {
					p = self.Prior;
					self.BlockToArray(array, offset, localCount, isRList);
					offset += localCount;
					localCount = p._localCount;
					self = p._block;
				} while (self != null);
				Debug.Assert(offset == array.Length);
			}
			return array;
		}
		protected abstract void BlockToArray(T[] array, int arrayOffset, int localCount, bool isRList);

		#endregion
	}
}
