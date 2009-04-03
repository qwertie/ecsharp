using System;
using System.Diagnostics;
using System.Threading;

namespace Loyc.Utilities
{
	/// <summary>The tail of a VList contains only one or two items. To improve 
	/// efficiency slightly, these two-item lists are represented by a
	/// VListBlockOfTwo, which is more compact than VListBlockArray.</summary>
	/// <remarks>
	/// TODO: create a more efficient version using a "fixed T _array2[2]" in an
	/// unsafe code block (assuming it is generics-compatible).
	/// </remarks>
	public sealed class VListBlockOfTwo<T> : VListBlock<T>
	{
		internal T _1;
		internal T _2;

		/// <summary>Initializes a mutable block with no items.</summary>
		public VListBlockOfTwo()
		{
			_immCount = MutableFlag;
		}
		public VListBlockOfTwo(T firstItem, bool mutable)
		{
			_1 = firstItem;
			_immCount = mutable ? MutableFlag|1 : 1;
		}
		/// <summary>Initializes a block with two items.</summary>
		/// <remarks>The secondItem is added second, so it will occupy position [0]
		/// of a VList or position [1] of an RVList.</remarks>
		public VListBlockOfTwo(T firstItem, T secondItem, bool mutable)
		{
			_1 = firstItem;
			_2 = secondItem;
			_immCount = mutable ? MutableFlag|2 : 2;
		}

		public override int PriorCount { get { return 0; } }
		public override VList<T> Prior { get { return new VList<T>(); } }
		
		public override int Capacity { get { return 2; } }

		public override T this[int localIndex]
		{
			get {
				Debug.Assert(localIndex == 0 || localIndex == 1);
				if (localIndex == 0)
					return _1;
				else
					return _2;
			}
			set {
				Debug.Assert(localIndex >= ImmCount);
				Debug.Assert(localIndex == 0 || localIndex == 1);
				if (localIndex == 0)
					_1 = value;
				else
					_2 = value;
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
			// items to the end of an immutable block, which is never empty.
			Debug.Assert(localIndex == 1 || localIndex == 2);

			if (localIndex == 1 && _immCount == 1) {
				if (Interlocked.CompareExchange(ref _immCount, 2, 1) == 1) {
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
				Debug.Assert(localIndex <= Math.Min(_immCount, 2) && ImmCount <= 2);
				return new VList<T>(this, localIndex);
			}
		}

		public override void MuClear(int localCountWithMutables)
		{
			Debug.Assert(IsMutable);
			Debug.Assert(ImmCount <= localCountWithMutables && localCountWithMutables <= 2);
			// If _localCount == 0 then there are no shared copies of this object, 
			// so this object is about to become garbage, and there is no need to 
			// clear the items. If _localCount is 2, there is nothing to clear.
			if (_immCount == 1)
				_2 = default(T);
			_immCount &= ImmCountMask;
		}

		protected override void BlockToArray(T[] array, int arrayOffset, int localCount, bool isRList)
		{
			if (localCount == 1) {
				array[arrayOffset] = _1;
			} else if (localCount == 2) {
				if (isRList) {
					array[arrayOffset] = _1;
					array[arrayOffset+1] = _2;
				} else {
					array[arrayOffset] = _2;
					array[arrayOffset+1] = _1;
				}
			} else
				Debug.Assert(localCount == 0);
		}
	}
}