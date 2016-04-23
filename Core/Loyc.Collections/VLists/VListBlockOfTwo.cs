namespace Loyc.Collections
{
	using System;
	using System.Diagnostics;
	using System.Threading;
	using System.Linq;

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
			_immCount = mutable ? MutableFlag : 1;
		}
		/// <summary>Initializes a block with two items.</summary>
		/// <remarks>The secondItem is added second, so it will occupy position [0]
		/// of a FVList or position [1] of a VList.</remarks>
		public VListBlockOfTwo(T firstItem, T secondItem, bool mutable)
		{
			_1 = firstItem;
			_2 = secondItem;
			_immCount = mutable ? MutableFlag : 2;
		}

		public override int PriorCount { get { return 0; } }
		public override FVList<T> Prior { get { return new FVList<T>(); } }
		
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
		public override T FGet(int index, int localCount)
		{
			Debug.Assert((uint)localCount <= 2);
			if ((uint)index >= (uint)localCount)
				throw new IndexOutOfRangeException();
			return index + 1 >= localCount ? _1 : _2;
		}
		public override bool FGet(int index, int localCount, ref T value)
		{
			Debug.Assert((uint)localCount <= 2);
			if ((uint)index >= (uint)localCount)
				return false;
			value = (index + 1 >= localCount ? _1 : _2);
			return true;
		}
		public override T RGet(int index, int localCount)
		{
			Debug.Assert((uint)localCount <= 2);
			if ((uint)index >= (uint)localCount)
				throw new IndexOutOfRangeException();
			return index == 0 ? _1 : _2;
		}
		public override bool RGet(int index, int localCount, ref T value)
		{
			Debug.Assert((uint)localCount <= 2);
			if ((uint)index >= (uint)localCount)
				return false;
			value = (index == 0 ? _1 : _2);
			return true;
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
			return new VListBlockArray<T>(new FVList<T>(this, localIndex), item);
		}

		public override FVList<T> SubList(int localIndex)
		{
			if (localIndex <= 0)
				return new FVList<T>(); // empty
			else {
				Debug.Assert(localIndex <= Math.Min(_immCount, 2) && ImmCount <= 2);
				return new FVList<T>(this, localIndex);
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

		#region LINQ-like methods

		public override FVList<T> Where(int localCount, Func<T, bool> keep, WListProtected<T> forWList)
		{
			// Optimization
			
			Debug.Assert(localCount > 0);
			if (keep(_1))
			{
				if (localCount == 2) {
					if (keep(_2)) {
						_immCount = 2; // Mark immutable if it isn't already
						return MakeResult(this, 2, forWList);
					}
				}

				if (_immCount == MutableFlag)
					_immCount = 1; // Ensure first item is immutable

				return MakeResult(this, 1, forWList);
			}
			else
			{
				if (localCount == 2 && keep(_2))
					return MakeResult(_2, forWList);
				else
					return new FVList<T>();
			}
		}

		/*public override FVList<T> WhereSelect(int _localCount, Func<T, Maybe<T>> map, WListProtected<T> forWList)
		{	// Optimization
			Maybe<T> item, item2;

			Debug.Assert(_localCount > 0);
			if (IsSame(_1, item = map(_1))) {
				if (_localCount == 2) {
					if (IsSame(_2, item2 = map(_2))) {
						_immCount = 2; // Mark immutable if it isn't already
						return MakeResult(this, 2, forWList);
					} else {
						return MakeResult(item.Value, item2.Value, forWList);
					}
				} else {
					if (_immCount == MutableFlag)
						_immCount = 1; // Ensure first item is immutable
					return MakeResult(this, 1, forWList);
				}
			} else if (!item.HasValue) {
				if (_localCount == 1 || !(item2 = map(_2)).HasValue) {
					Debug.Assert(forWList == null || forWList.Count == 0);
					return FVList<T>.Empty;
				} else
					return MakeResult(item2.Value, forWList);
			} else {
				if (_localCount == 1 || !(item2 = map(_2)).HasValue)
					return MakeResult(item.Value, forWList);
				else 
					return MakeResult(item.Value, item2.Value, forWList);
			}
		}*/

		public override FVList<T> SmartSelect(int _localCount, Func<T, T> map, WListProtected<T> forWList)
		{	// Optimization
			T item, item2;

			Debug.Assert(_localCount > 0);
			if (EqualityComparer.Equals(item = map(_1), _1))
			{
				if (_localCount == 2) {
					if (EqualityComparer.Equals(item2 = map(_2), _2)) {
						_immCount = 2; // Mark immutable if it isn't already
						return MakeResult(this, 2, forWList);
					} else {
						return MakeResult(item, item2, forWList);
					}
				} else {
					if (_immCount == MutableFlag)
						_immCount = 1; // Ensure first item is immutable
					return MakeResult(this, 1, forWList);
				}
			} else {
				if (_localCount == 2)
					return MakeResult(item, map(_2), forWList);
				else
					return MakeResult(item, forWList);
			}
		}

		#endregion
	}
}