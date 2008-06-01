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
using Loyc.Runtime;
using System.Threading;

namespace Loyc.Utilities
{
	/// <summary>
	/// VListBlock implements the core functionality of VList and RVList. It is
	/// not intended to be used directly.
	/// </summary><remarks>
	/// VList is a persistent list data structure described in Phil Bagwell's 2002
	/// paper "Fast Functional Lists, Hash-Lists, Deques and Variable Length
	/// Arrays". RVList is the name I (DLP) give to a variant of this structure in
	/// which the elements are considered to be in reverse order so that new
	/// elements are added at the back (end) of the list rather than at the front 
	/// (beginning).
	/// <para/>
	/// A persistent list is a list that is normally considered immutable, so
	/// adding an item implies creating a new list rather than changing the one
	/// you've got. This is fast because persistent lists have a sort of
	/// copy-on-write semantics, so that "copying" a list is a trivial O(1)
	/// operation, but modifying a list is sometimes less efficient than you would
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
	/// VListBlock implements a single "node" or "sub-array" within a VList. It
	/// contains a fixed-size array. When adding a new item to a VListBlock that
	/// is already full, a new empty VListBlock is created (with a larger array),
	/// whose _prior reference points to the old VListBlock. See Phil Bagwell's 
	/// paper (or Wikipedia) for details.
	/// <para/>
	/// VListBlock adds one new member to the structure Phil Bagwell described,
	/// PriorCount, a count of elements in other (smaller) lists to which this list
	/// is linked. This makes TotalCount an O(1) operation instead of O(log N),
	/// which is necessary so that RVList[i] is also O(1) on average.
	/// <para/>
	/// Independent instances of VList and RVList can be accessed from independent 
	/// threads even though they may share some of the same memory. Individual 
	/// instances of VList or RVList, however, are not synchronized. VListBlock
	/// itself might not be thread-safe if used directly.
	/// </remarks>
	/// <typeparam name="T">The type of elements in the list</typeparam>
	public abstract class VListBlock<T>
	{
		protected int _localCount;   // number of elements used in our local array

		public abstract int PriorCount { get; }
		public abstract VList<T> Prior { get; }
		public int LocalCount { get { return _localCount; } }
		public int TotalCount { get { return PriorCount + _localCount; } }

		/// <summary>Returns the specified value at the specified index of this
		/// block's array, or, if localIndex is negative, searches recursively in
		/// previous blocks for the desired index.</summary>
		/// <remarks>A VList computes localIndex as VList._localCount-1-index.
		/// 
		/// VList/RVList is responsible for checking that the user's index is 
		/// valid and throwing IndexOutOfRangeException if not.
		/// </remarks>
		public abstract T this[int localIndex] { get; }

		/// <summary>Returns the "front" item in a VList associated with this block
		/// (or back item of a RVList) where localCount is the number of items in
		/// the VList's first block.
		/// </summary>
		public abstract T Front(int localCount);

		/// <summary>Inserts a new item at the "front" of a VList where localCount
		/// is the number of items currently in the VList's first block.
		/// </summary>
		public abstract VListBlock<T> Add(int localCount, T item);

		public static VListBlock<T> Add(VListBlock<T> self, int localCount, T item)
		{
			if (self != null)
				return self.Add(localCount, item);
			else
				return new VListBlockOfTwo<T>(item);
		}

		/// <summary>
		/// Returns a list in which this[localIndex-1] is the first item.
		/// Nonpositive indexes are allowed and refer to prior lists; SubList
		/// returns an empty list if localIndex is so low that it goes past the back
		/// of the list.
		/// </summary>
		public abstract VList<T> SubList(int localIndex);
		public static VList<T> SubList(VListBlock<T> self, int localCount, int offset)
		{
			if (offset < 0)
				throw new IndexOutOfRangeException();
			if (self == null)
				return new VList<T>();
			else {
				Debug.Assert(localCount > 0 && localCount <= self._localCount);
				return self.SubList(localCount - offset);
			}
		}

		/// <summary>Inserts a new item in a VList where localCount is the number
		/// of items in the VList's first block and distanceFromFront is the
		/// insertion position (0=front).
		/// </summary>
		/// <exception cref="IndexOutOfRangeException">distanceFromFront was out of range.</exception>
		/// <returns>The block resulting from the insert (may or may not be 'this')</returns>
		public static VListBlock<T> Insert(VListBlock<T> self, int localCount, T item, int distanceFromFront)
		{
			if (self == null) {
				Debug.Assert(localCount == 0);
				if (distanceFromFront != 0)
					throw new IndexOutOfRangeException();
				return new VListBlockOfTwo<T>(item);
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
				replace2._localCount = replace2._block._localCount;
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

		public static VList<T> AddRange(VListBlock<T> self, int localCount, IList<T> items, bool isRVList)
		{
			int itemCount = items.Count;
			if (isRVList) {
				// Add items in forward order for RVList
				for (int i = 0; i < itemCount; i++) {
					self = Add(self, localCount, items[i]);
					localCount = self._localCount;
				}
			} else {
				// Add items in reverse order for VList
				for (int i = itemCount - 1; i >= 0; i--) {
					self = Add(self, localCount, items[i]);
					localCount = self._localCount;
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
		/// </remarks>
		public static VList<T> AddRange(VListBlock<T> self, int localCount, VList<T> front, VList<T> back)
		{
			if (front == back || front.IsEmpty)
				return new VList<T>(self, localCount);
			else {
				back = BackUpOnce(back, front);
				VListBlock<T> newBlock = Add(self, localCount, back.Front);
				newBlock = newBlock.AddRange(front, back);
				return new VList<T>(newBlock, newBlock._localCount);
			}
		}

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
		/// <returns>The list prior to subList, or an empty list if list and subList
		/// are in the same block.</returns>
		/// <remarks>
		/// Because of the copy-causing-sharing-failure problem (described in a
		/// comment in RVListTests.TestSublistProblem()), FindNextBlock may have to
		/// change subList in certain cases so that it really is a sublist of list.
		/// Therefore it is a ref argument.
		/// </remarks>
		public static VList<T> FindNextBlock(ref VList<T> subList, VList<T> list, out int localCountOfSubList)
		{
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
									fail = true;
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

		/// <summary>Appends a range of items to the "front" of this block.</summary>
		/// <returns>This block, or a new block if a new block had to be allocated.</returns>
		protected VListBlock<T> AddRange(VList<T> front, VList<T> back)
		{
			RVList<T>.Enumerator e = new RVList<T>.Enumerator(front, back);
			VListBlock<T> block = this;
			while (e.MoveNext())
				block = block.Add(block._localCount, e.Current);
			return block;
		}
	}

	/// <summary>
	/// Implementation of VListBlock(of T) that contains an array. It is always
	/// initialized with at least one item and items cannot be removed.
	/// </summary>
	/// <remarks>
	/// _prior._block is never null because the last block in a chain is always a
	/// VListBlockOfTwo. _array is never null either and _priorCount is at least 2.
	/// </remarks>
	public sealed class VListBlockArray<T> : VListBlock<T>
	{
		/// <summary>Combined length of prior blocks</summary>
		private readonly int _priorCount;
		/// <summary>The prior list, to which this list adds more items.</summary>
		private readonly VList<T> _prior;

		/// <summary>The local array (elements [0.._localCount - 1] are in use).
		/// _array[_localCount-1] is the "front" of the list according to the
		/// terminology of a VList, but it's the back of the list in the
		/// terminology of a RVList.</summary>
		/// <remarks>
		/// The method descriptions here use the terminology of a VList, so if
		/// we're talking about _array[i], and i is increasing, then we are getting
		/// closer to the "front" of the list.
		/// </remarks>
		internal readonly T[] _array;        // local array

		// Copy a block instead of referencing it if less than 1/3 of 
		// it is in use.
		const int GARBAGE_AVODANCE_NUMERATOR = 1;
		const int GARBAGE_AVODANCE_DENOMINATOR = 3;
		const int MAX_BLOCK_LEN = 1024;

		public VListBlockArray(VList<T> prior, T firstItem)
			: this(prior)
		{
			_array[0] = firstItem;
			_localCount = 1;
		}
		private VListBlockArray(VList<T> prior, int initialUsed)
			: this(prior)
		{
			Debug.Assert(initialUsed > 0 && initialUsed <= _array.Length);
			_localCount = initialUsed;
		}
		private VListBlockArray(VList<T> prior)
		{
			if (prior._block == null)
				throw new ArgumentNullException("prior");

			_prior = prior;
			_priorCount = prior.Count;
			int capacity = Math.Min(MAX_BLOCK_LEN,
						   Math.Max(prior._localCount * 2, prior._block.LocalCount));
			_array = new T[capacity];
		}

		public override int PriorCount { get { return _priorCount; } }
		public override VList<T> Prior { get { return _prior; } }

		public override T this[int localIndex]
		{
			get {
				if (localIndex >= 0) {
					Debug.Assert(localIndex < _localCount);
					return _array[localIndex];
				} else {
					return _prior._block[localIndex + _prior._localCount];
				}
			}
		}

		public override T Front(int localCount)
		{
			Debug.Assert(localCount > 0 && localCount <= _localCount);
			return _array[localCount - 1];
		}

		public override VListBlock<T> Add(int localIndex, T item)
		{
			// if localIndex is 0, call Prior.Add(item, Prior.LocalCount) instead
			Debug.Assert(localIndex > 0 && localIndex <= _localCount);
			Debug.Assert(_array != null);

			if (localIndex == _localCount && _localCount < _array.Length) {
				// Optimal case, just set a new array entry. But deal with a
				// multithreading race condition in which multiple threads
				// simultaneously decide they can increment _localCount to add an
				// item to different list instances. Note that in rare cases this
				// will cause _localCount to exceed _array.Length momentarily.
				// 
				// There are a lot of places that read the value of VBlockList<T>.
				// _localCount, and you might wonder whether this is actually
				// thread-safe. Well, if you search for references to it, you'll
				// find that _localCount is normally being read from the return
				// value of VBlockList<T>.Add() or some other function that is
				// guaranteed to call Add(). This is thread-safe (assuming the
				// end-user is using independent VList or RVList instances in
				// different threads and not trying to work with VListBlocks
				// directly) because no other thread will modify the _localCount of
				// the block returned by Add(). That's because either it is a
				// newly-allocated block, or the block has one more array element
				// used so other threads know they cannot modify the block.
				if (Interlocked.Increment(ref _localCount) != localIndex + 1)
					Interlocked.Decrement(ref _localCount); // undo
				else {
					_array[localIndex] = item;
					return this;
				}
			}
			
			// Oops, have to make a new VListBlock. Now, something to think
			// about here is that the remainder of this VListBlock
			// (_array.Length - _localCount) may be in use, or it may be that no
			// other list is using it. The VList as described by Phil Bagwell
			// has problems in the face of certain modification pattens. A
			// pattern such as repeatedly pushing two items and popping one
			// could cause a series of blocks to be allocated that hold only a
			// single item; these blocks are not garbage and no part of them can
			// be reclaimed.
			// 
			// To reduce the impact of this memory-hogging disaster, I copy the
			// block if a lot of it is unused; that way, the largely-unused
			// block can be collected if no one else is using it. The copy
			// operation will take extra time and will also use extra space
			// unnecessarily if list sharing was hapenning, but occasionally
			// slower execution is worthwhile to avoid the risk of sucking up
			// all a machine's RAM. The exact threshold to use is something that
			// deserves more analysis, but for now I'm just guessing 1/3 of the
			// block size is a reasonable threshold.
			// 
			// With this threshold, the push-twice-pop-once pattern now leads to
			// blocks that are only 1/3 full--bad, but a major improvement--but
			// on the flip side, the push-once-pop-once pattern (which
			// invariably produces a ton of garbage blocks) may, with 1/3
			// probability, end up copying up to 1/3*_array.Count elements on
			// each iteration.
			const int n = GARBAGE_AVODANCE_NUMERATOR, d = GARBAGE_AVODANCE_DENOMINATOR;
			// example: if n/d is 1/3, make a copy if localIndex<_array.Count/3
			if (localIndex * d >= _array.Length * n) {
				// Simple case, just make a new block with one item
				return new VListBlockArray<T>(new VList<T>(this, localIndex), item);
			} else {
				VListBlockArray<T> @new = new VListBlockArray<T>(_prior, localIndex + 1);
				for (int i = 0; i < localIndex; i++)
					@new._array[i] = _array[i];
				@new._array[localIndex] = item;
				return @new;
			}
		}

		public override VList<T> SubList(int localIndex)
		{
			if (-localIndex >= PriorCount)
				return new VList<T>(); // empty

			VList<T> list = new VList<T>(this, localIndex);
			while (list._localCount <= 0) {
				VList<T> prior = list._block.Prior;
				list._block = prior._block;
				list._localCount += prior._localCount;
			}
			return list;
		}
	}

	/// <summary>The tail of a VList contains only one or two items. To improve 
	/// efficiency slightly, these two-item lists are represented by a
	/// VListBlockOfTwo, which is more compact than VListBlockArray.</summary>
	/// <remarks>
	/// I could have optimized away the _priorCount, _localCount, _prior and _array
	/// variables for this one-item block, but it would have required VListBlock
	/// to be an abstract base class with lots of abstract functions. I decided it
	/// wouldn't be worth the performance hit in the general case, so instead, 
	/// VListBlock has no virtual functions but VListBlockOfTwo uses 20 more
	/// bytes than strictly necessary.
	/// 
	/// TODO: create a more efficient version using a "fixed T _array2[2]" in an
	/// unsafe code block (assuming it is generics-compatible).
	/// </remarks>
	public sealed class VListBlockOfTwo<T> : VListBlock<T>
	{
		internal T _1;
		internal T _2;

		public VListBlockOfTwo(T firstItem)
		{
			_1 = firstItem;
			_localCount = 1;
		}
		/// <summary>Initializes a block with two items.</summary>
		/// <remarks>The secondItem is added second, so it will occupy position [0]
		/// of a VList or position [1] of an RVList.</remarks>
		public VListBlockOfTwo(T firstItem, T secondItem)
		{
			_1 = firstItem;
			_2 = secondItem;
			_localCount = 2;
		}

		public override int PriorCount { get { return 0; } }
		public override VList<T> Prior { get { return new VList<T>(); } }

		public override T this[int localIndex]
		{
			get
			{
				Debug.Assert(localIndex == 0 || localIndex == 1);
				if (localIndex == 0)
					return _1;
				else
					return _2;
			}
		}

		public override T Front(int localCount)
		{
			Debug.Assert(localCount == 1 || localCount == 2);
			if (localCount == 1)
				return _1;
			else
				return _2;
		}

		public override VListBlock<T> Add(int localIndex, T item)
		{
			// localIndex == 0 is impossible, as the caller is only supposed to Add
			// items to the end of a list.
			Debug.Assert(localIndex > 0);

			if (localIndex == 1 && _localCount == 1) {
				// Note that _localCount can be 3 for an instant if two threads
				// call Add() on this object at the same time (it could be even
				// more if there are more threads, but it's highly unlikely.) 
				// Other code in this class must be aware of this fact.
				if (Interlocked.Increment(ref _localCount) > 2)
					Interlocked.Decrement(ref _localCount); // undo
				else {
					_2 = item;
					return this;
				}
			}
			return new VListBlockArray<T>(new VList<T>(this, localIndex), item);
		}

		public override VList<T> SubList(int localIndex)
		{
			if (localIndex <= 0)
				return new VList<T>(); // empty
			else {
				Debug.Assert(localIndex <= _localCount && _localCount <= 3);
				return new VList<T>(this, localIndex);
			}
		}
	}
}
