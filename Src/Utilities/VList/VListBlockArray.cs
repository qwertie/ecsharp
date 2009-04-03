using System;
using System.Diagnostics;
using System.Threading;

namespace Loyc.Utilities
{
	/// <summary>
	/// Implementation of VListBlock(of T) that contains an array. It is always
	/// initialized with at least one item, and items cannot be removed unless 
	/// the list is mutable.
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
		/// <remarks>Warning: if the current block is 100% mutable then _prior can 
		/// include mutable items.
		/// <para/>
		/// _prior is never changed if the block contains immutable items.</remarks>
		internal VList<T> _prior;

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
		const int GARBAGE_AVOIDANCE_NUMERATOR = 1;
		const int GARBAGE_AVOIDANCE_DENOMINATOR = 3;
		public const int MAX_BLOCK_LEN = 1024;

		/// <summary>Inits an immutable block with one item.</summary>
		public VListBlockArray(VList<T> prior, T firstItem)
			: this(prior, 0, false)
		{
			_array[0] = firstItem;
			_immCount = 1;
		}
		/// <summary>Inits an empty block.</summary>
		/// <param name="localCapacity">Max item count in this block, or zero to 
		/// let the constructor choose the capacity.</param>
		/// <remarks>If this constructor is called directly, mutable must be true, 
		/// because immutable blocks must have at least one item.
		/// </remarks>
		public VListBlockArray(VList<T> prior, int localCapacity, bool mutable)
		{
			if (prior._block == null)
				throw new ArgumentNullException("prior");

			_prior = prior;
			_priorCount = prior.Count;
			if (localCapacity <= 0)
				localCapacity = Math.Min(MAX_BLOCK_LEN,
				                Math.Max(prior._localCount * 2, prior._block.ImmCount));
			_array = new T[localCapacity];
			if (mutable)
				_immCount |= MutableFlag;
		}

		public override int PriorCount { get { return _priorCount; } }
		public override VList<T> Prior { get { return _prior; } }

		public override int Capacity { get { return _array.Length; } }

		public override T this[int localIndex]
		{
			get {
				if (localIndex >= 0) {
					Debug.Assert(localIndex < _immCount);
					return _array[localIndex];
				} else {
					return _prior._block[localIndex + _prior._localCount];
				}
			}
			set {
				// Only called by mutable VList!
				Debug.Assert(IsMutable);
				if (localIndex >= 0) {
					Debug.Assert(localIndex >= ImmCount);
					_array[localIndex] = value;
				} else {
					Debug.Assert(ImmCount == 0);
					_prior._block[localIndex + _prior._localCount] = value;
				}
			}
		}

		public override T Front(int localCount)
		{
			if (localCount == 0)
				return _prior._block.Front(_prior._localCount);
			else {
				Debug.Assert(localCount > 0 && localCount <= _immCount);
				return _array[localCount - 1];
			}
		}

		public override VListBlock<T> Add(int localIndex, T item)
		{
			// (this is the immutable Add method.)
			// if localIndex is 0, caller should have called 
			// Prior.Add(Prior._localCount, item) instead
			Debug.Assert(localIndex > 0 && localIndex <= ImmCount);
			Debug.Assert(_array != null);

			if (localIndex == _immCount && _immCount < _array.Length) {
				Debug.Assert(!IsMutable);
				// Optimal case, just set a new array entry. But deal with a
				// multithreading race condition in which multiple threads
				// simultaneously decide they can increment _localCount to add an
				// item to different list instances.
				// 
				// There are a lot of places that read the value of VListBlock<T>.
				// _localCount, and you might wonder whether this is actually
				// thread-safe. Well, if you search for references to it, you'll
				// find that _localCount is normally being read from the return
				// value of VListBlock<T>.Add() or some other function that is
				// guaranteed to call Add(). This is thread-safe (assuming the
				// end-user is using independent VList or RVList instances in
				// different threads and not trying to work with VListBlocks
				// directly) because no other thread will modify the _localCount of
				// the block returned by Add(). That's because either it is a
				// newly-allocated block, or the block has one more array element
				// used so other threads know they cannot modify the block.
				int i = localIndex;
				if (Interlocked.CompareExchange(ref _immCount, i + 1, i) == i) {
					_array[i] = item;
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
			const int n = GARBAGE_AVOIDANCE_NUMERATOR, d = GARBAGE_AVOIDANCE_DENOMINATOR;
			// example: if n/d is 1/3, make a copy if localIndex<_array.Length/3
			if (localIndex * d >= _array.Length * n) {
				// Simple case, just make a new block with one item
				return new VListBlockArray<T>(new VList<T>(this, localIndex), item);
			} else {
				VListBlockArray<T> @new = new VListBlockArray<T>(_prior, 0, false);
				@new._immCount = localIndex + 1;
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

		public override void MuClear(int localCountWithMutables)
		{
			Debug.Assert(IsMutable);
			Debug.Assert(ImmCount <= localCountWithMutables);
			// If ImmCount == 0 then there are no shared copies of this object, 
			// so this object is about to become garbage, and there is no need 
			// to clear the items.
			if (ImmCount > 0) {
				for (int i = ImmCount; i < localCountWithMutables; i++)
					_array[i] = default(T);
			}
			bool priorIsOwned = PriorIsOwned;
			_immCount &= ImmCountMask;
			if (priorIsOwned)
				_prior._block.MuClear(_prior._localCount); // tail call
		}
		
		protected override void BlockToArray(T[] array, int arrayOffset, int localCount, bool isRList)
		{
			if (isRList) {
				for (int i = 0; i < localCount; i++)
					array[arrayOffset + i] = _array[i];
			} else {
				for (int i = 0; i < localCount; i++)
					array[arrayOffset + localCount - 1 - i] = _array[i];
			}
		}
	}
}
