using System;
using System.Collections.Generic;
using System.Diagnostics;
using Loyc.Runtime;

namespace Loyc.Utilities
{
	/// <summary>WList implementation in which the WList operations are only 
	/// accessible to a derived class.</summary>
	/// <typeparam name="T">The type of elements in the list</typeparam>
	/// <remarks>
	/// This base class is used in the same way one would use protected inheritance 
	/// in C++: it provides the derived class with access to a WList/RWList, but it
	/// does not allow users of the derived class to access the list.
	/// <para/>
	/// I plan to use this base class as an optimization, to implement IExtra in 
	/// Loyc AST nodes. It is important that AST nodes, which are immutable, use as 
	/// little memory as possible so that copies can be made as quickly as possible
	/// (and so that Loyc isn't a memory hog). By using WListProtected as a base 
	/// class, AST nodes can have extra attributes attached to them without 
	/// allocating a separate heap object that would have to be cloned every time 
	/// the node's immutable attributes change. Consequently, nodes use 12 bytes 
	/// less memory and can be copied faster.
	/// <para/>
	/// By default, the list will act like a WList. If you want the list to act 
	/// like an RWList instead, override AdjustWListIndex and GetWListEnumerator 
	/// as follows:
	/// <code>
	///	protected override int AdjustWListIndex(int index, int size) 
	///		{ return Count - size - index; }
	///	protected override IEnumerator<T> GetWListEnumerator()
	///		{ return new RVList<T>.Enumerator(InternalVList); }
	/// </code>
	/// </remarks>
	public abstract class WListProtected<T>
	{
		private VListBlock<T> _block;
		private int _localCount;
		private const int IsOwnerFlag = 0x40000000;
		private const int LocalCountMask = IsOwnerFlag - 1;

		/// <summary>Reference to VListBlock that contains items at the "front" 
		/// of the list. _block can be null if the list is empty.</summary>
		protected internal VListBlock<T> Block
		{
			get { return _block; }
			internal set { _block = value; }
		}
		/// <summary>Number of items in _block that belong to this list.</summary>
		protected internal int LocalCount
		{
			get { return _localCount & LocalCountMask; }
			internal set { _localCount = (_localCount & IsOwnerFlag) | value; }
		}
		/// <summary>Specifies whether this object owns the mutable part of _block.</summary>
		/// <remarks>
		/// Two WLists can have pointers to the same mutable block, but only one 
		/// can be the owner. The non-owner must not modify the block.
		/// <para/>
		/// This flag is false if _block.IsMutable is false.</remarks>
		protected internal bool IsOwner
		{
			get { return (_localCount & IsOwnerFlag) != 0; }
			internal set {
				_localCount = (_localCount & ~IsOwnerFlag);
				if (value) _localCount |= IsOwnerFlag;
			}
		}

		/// <summary>Returns this list as a VList without marking all items as 
		/// immutable. This is for internal use only; a VList with mutable items 
		/// is never returned from a public method.</summary>
		protected internal VList<T> InternalVList { get { return new VList<T>(Block, LocalCount); } }

		/// <summary>This method implements the difference between WList and RWList:
		/// In WList it returns <c>index</c>, but in RWList it returns 
		/// <c>Count-size-index</c>.</summary>
		/// <param name="index">Index to adjust</param>
		/// <param name="size">Number of elements being accessed or removed</param>
		/// <remarks>Solely as an optimization, WList and RWList also have separate 
		/// versions of this[], InsertAt and RemoveAt.</remarks>
		protected virtual int AdjustWListIndex(int index, int size) { return index; }

		#region Constructors

		protected internal WListProtected() { }
		protected internal WListProtected(VListBlock<T> block, int localCount, bool isOwner)
		{
			Block = block;
			LocalCount = localCount;
			IsOwner = isOwner;
			Debug.Assert(LocalCount <= (Block == null ? 0 : Block.Capacity));
			Debug.Assert(!IsOwner || (Block != null && Block.Capacity > Block.ImmCount));
		}
		
		#endregion

		#region IList<T>/ICollection<T> Members

		/// <summary>Gets an item from a WList or RWList at the specified index.</summary>
		protected T GetAt(int index)
		{
			int v_index = AdjustWListIndex(index, 1);
			if ((uint)v_index >= (uint)Count)
				throw new IndexOutOfRangeException();
			return GetAtDff(v_index);
		}
		/// <summary>Sets an item in a WList or RWList at the specified index.</summary>
		protected void SetAt(int index, T value)
		{
			int v_index = AdjustWListIndex(index, 1);
			if ((uint)v_index >= (uint)Count)
				throw new IndexOutOfRangeException();
			VListBlock<T>.EnsureMutable(this, v_index + 1);
			SetAtDff(v_index, value);
		}
		
		/// <summary>Inserts an item at the "front" of the list, 
		/// which is index 0 for WList, or Count for RWList.</summary>
		protected void Add(T item)
		{
 			VListBlock<T>.MuAdd(this, item);
		}

		/// <summary>Clears the list and frees the memory it used.</summary>
		protected void Clear()
		{
			if (Block != null) {
				if (IsOwner)
					Block.MuClear(LocalCount);
				Block = null;
				LocalCount = 0;
				IsOwner = false;
			}
		}
		protected void Insert(int index, T item) { InsertAtDff(AdjustWListIndex(index, 0), item); }

		protected void RemoveAt(int index) { RemoveBase(AdjustWListIndex(index, 1)); }

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
		protected int IndexOf(T item)
		{
			EqualityComparer<T> comparer = EqualityComparer<T>.Default;
			int i = 0;
			IEnumerator<T> e = GetWListEnumerator();
			while (e.MoveNext())
			{
				if (comparer.Equals(e.Current, item))
					return i;
				i++;
			}
			return -1;
		}
		
		protected bool Contains(T item)
		{
			return IndexOf(item) != -1;
		}

		protected void CopyTo(T[] array, int arrayIndex)
		{
			IEnumerator<T> e = GetWListEnumerator();
			while (e.MoveNext())
				array[arrayIndex++] = e.Current;
		}

		protected internal int Count
		{
			get {
				if (Block == null) {
					Debug.Assert(LocalCount == 0);
					return 0;
				}
				return LocalCount + Block.PriorCount;
			}
		}

		protected bool Remove(T item)
		{
			int i = IndexOf(item);
			if (i == -1)
				return false;
			RemoveAt(i);
			return true;
		}

		protected virtual IEnumerator<T> GetWListEnumerator() {
			return new VList<T>.Enumerator(InternalVList);
		}

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
			InsertRangeAtDff(0, items, isRWList);
		}
		protected void InsertRangeAtDff(int distanceFromFront, IList<T> items, bool isRWList)
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
					SetAtDff(distanceFromFront + count - 1 - i, items[i]);
			} else {
				// Add items in reverse order for WList
				for (int i = 0; i < count; i++)
					SetAtDff(distanceFromFront + i, items[i]);
			}
		}
		protected void InsertAtDff(int distanceFromFront, T item)
		{
			if ((uint)distanceFromFront > (uint)Count)
				throw new IndexOutOfRangeException();

			VListBlock<T>.EnsureMutable(this, distanceFromFront);
			VListBlock<T>.MuAddEmpty(this, 1);
			VListBlock<T>.MuMove(this, 1, 0, distanceFromFront);
			SetAtDff(distanceFromFront, item);
		}

		protected T GetAtDff(int distanceFromFront) 
			{ return Block[LocalCount - 1 - distanceFromFront]; }
		protected void SetAtDff(int distanceFromFront, T item) 
			{ Block[LocalCount - 1 - distanceFromFront] = item; }

		#endregion

		#region Other stuff

		/// <summary>Gets the number of blocks used by this list.</summary>
		/// <remarks>You might look at this property when optimizing your program,
		/// because the runtime of some operations increases as the chain length 
		/// increases. This property runs in O(BlockChainLength) time. Ideally,
		/// BlockChainLength is proportional to log_2(Count), but if you produced 
		/// the WList by converting it from a VList, certain VList usage patterns 
		/// can produce long chains.</remarks>
		protected int BlockChainLength
		{
			get { return Block == null ? 0 : Block.ChainLength; }
		}

		/// <summary>Returns this list as a VList; if this is a RWList, the order 
		/// of the elements is reversed at the same time.</summary>
		/// <remarks>This operation marks the items of the WList or RWList as 
		/// immutable. You can still modify the list afterward, but some or all
		/// of the list may have to be copied.</remarks>
		protected VList<T> ToVList()
		{
			return VListBlock<T>.EnsureImmutable(Block, LocalCount);
		}

		/// <summary>Returns this list as an RVList; if this is a WList, the order 
		/// of the elements is reversed at the same time.</summary>
		/// <remarks>This operation marks the items of the WList or RWList as 
		/// immutable. You can still modify the list afterward, but some or all
		/// of the list may have to be copied.</remarks>
		protected RVList<T> ToRVList()
		{
			return VListBlock<T>.EnsureImmutable(Block, LocalCount).ToRVList();
		}

		#endregion
	}

	/// <summary>Shared base class of WList and RWList.</summary>
	/// <typeparam name="T">The type of elements in the list</typeparam>
	[DebuggerTypeProxy(typeof(CollectionDebugView<>)),
	 DebuggerDisplay("Count = {Count}")]
	public abstract class WListBase<T> : WListProtected<T>, IList<T>
	{
		protected internal WListBase() { }
		protected internal WListBase(VListBlock<T> block, int localCount, bool isOwner) 
			: base(block, localCount, isOwner) {}

		#region IList<T>/ICollection<T> Members

		public T this[int index]
		{
			get { return GetAt(index); }
			set { SetAt(index, value); }
		}

		public new void Add(T item) { base.Add(item); }
		public new void Clear() { base.Clear(); }
		public new void Insert(int index, T item) { base.Insert(index, item); }
		public new void RemoveAt(int index) { base.RemoveAt(index); }
		public new int IndexOf(T item) { return base.IndexOf(item); }
		public new bool Contains(T item) { return base.Contains(item); }
		public new void CopyTo(T[] array, int arrayIndex) { base.CopyTo(array, arrayIndex); }
		public new int Count { get { return base.Count; } }
		public bool IsReadOnly { get { return false; } }
		public new bool Remove(T item) { return base.Remove(item); }
		public IEnumerator<T> GetEnumerator() { return GetWListEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{ return GetWListEnumerator(); }

		#endregion

		#region Other stuff

		public new int BlockChainLength { get { return base.BlockChainLength; } }
		/// <summary>Synonym for Add(); adds an item to the front of the list.</summary>
		public void Push(T item) { Add(item); }

		/// <summary>Returns this list as a VList; if this is a RWList, the order 
		/// of the elements is reversed at the same time.</summary>
		/// <remarks>This operation marks the items of the WList or RWList as 
		/// immutable. You can still modify the list afterward, but some or all
		/// of the list may have to be copied.</remarks>
		public static explicit operator VList<T>(WListBase<T> list) { return list.ToVList(); }
		public new VList<T> ToVList() { return base.ToVList(); }

		/// <summary>Returns this list as an RVList; if this is a WList, the order 
		/// of the elements is reversed at the same time.</summary>
		/// <remarks>This operation marks the items of the WList or RWList as 
		/// immutable. You can still modify the list afterward, but some or all
		/// of the list may have to be copied.</remarks>
		public static explicit operator RVList<T>(WListBase<T> list) { return list.ToRVList(); }
		public new RVList<T> ToRVList() { return base.ToRVList(); }

		#endregion
	}
}