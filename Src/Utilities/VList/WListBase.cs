using System;
using System.Collections.Generic;
using System.Diagnostics;
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
			if (distanceFromFront < 0 || distanceFromFront + count > Count)
				throw new IndexOutOfRangeException();
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
				if (distanceFromFront == 0) {
					// this sublist won't change during the insert
					if (isRWList) items = InternalVList.ToRVList();
					else          items = InternalVList;
				} else
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
			if ((uint)distanceFromFront > (uint)Count)
				throw new IndexOutOfRangeException();

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
			get { return _block == null ? 0 : _block.ChainLength; }
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
}