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
using System.Threading;
using Loyc.MiniTest;
using Loyc.Collections;
using System.Linq;
using Loyc.Collections.Impl;

namespace Loyc.Collections
{
	/// <summary>
	/// VList represents a reference to a reverse-order FVList.
	/// </summary><remarks>
	/// An <a href="http://www.codeproject.com/Articles/26171/VList-data-structures-in-C">article</a>
	/// is available online about the VList data types.
	/// <para/>
	/// The VList is a persistent list data structure described in Phil Bagwell's 
	/// 2002 paper "Fast Functional Lists, Hash-Lists, Deques and Variable Length
	/// Arrays". Originally, this type was called RVList because it works in the
	/// reverse order to the original VList type: new items are normally added at
	/// the <i>beginning</i> of a VList, which is normal in functional languages,
	/// but <i>this</i> VList acts like a normal .NET list, so it is optimized for
	/// new items to be added at the end. The name "RVList" is ugly, though, since
	/// it misleadingly appears to be related to Recreational Vehicles. So as 
	/// of LeMP 1.5, it's called simply VList.
	/// <para/>
	/// In contrast, the <see cref="FVList{T}"/> type acts like the original VList;
	/// its Add method puts new items at the beginning (index 0).
	/// <para/>
	/// See the remarks of <see cref="VListBlock{T}"/> for a more detailed 
	/// description.
	/// </remarks>
	[DebuggerTypeProxy(typeof(CollectionDebugView<>)),
	 DebuggerDisplay("Count = {Count}")]
	public struct VList<T> : IListAndListSource<T>, ICloneable<VList<T>>, ITryPop<T>, ICloneable
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
			_block = new VListBlockOfTwo<T>(itemZero, itemOne, false);
			_localCount = 2;
		}
		public VList(T[] array)
		{
			_block = null;
			_localCount = 0;
			for (int i = 0; i < array.Length; i++)
				Add(array[i]);
		}
		public VList(IEnumerable<T> list)
		{
			_block = null;
			_localCount = 0;
			AddRange(list);
		}
		public VList(VList<T> list)
		{
			this = list;
		}

		#endregion

		#region Obtaining sublists

		public VList<T> WithoutLast(int offset)
		{
			return VListBlock<T>.SubList(_block, _localCount, offset).ToVList();
		}
		/// <summary>Returns a list without the last item. If the list is empty, 
		/// an empty list is retured.</summary>
		public VList<T> Tail
		{
			get {
				return VListBlock<T>.TailOf(ToFVList()).ToVList();
			}
		}
		public VList<T> NextIn(VList<T> largerList)
		{
			return VListBlock<T>.BackUpOnce(this, largerList);
		}
		public VList<T> First(int count)
		{
			int c = Count;
			if (count >= c)
				return this;
			if (count <= 0)
				return Empty;
			return WithoutLast(c - count);
		}

		#endregion

		#region Equality testing and GetHashCode()

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
			if (rhs_ == null)
				return false;
			try {
				VList<T> rhs = (VList<T>)rhs_;
				return this == rhs;
			} catch(InvalidCastException) {
				return false;
			}
		}
		public override int GetHashCode()
		{
			Debug.Assert((_localCount == 0) == (_block == null));
			if (_block == null)
				return 2357; // any ol' number will do
			return _block.GetHashCode() ^ _localCount;
		}

		#endregion

		#region AddRange, InsertRange, RemoveRange

		public VList<T> AddRange(VList<T> list)
		{
			if (IsEmpty)
				return this = list;
			return AddRange(list, new VList<T>());
		}
		public VList<T> AddRange(VList<T> list, VList<T> excludeSubList)
		{
			this = VListBlock<T>.AddRange(_block, _localCount, list.ToFVList(), excludeSubList.ToFVList()).ToVList();
			return this;
		}
		public VList<T> AddRange(IReadOnlyList<T> list)
		{
			this = VListBlock<T>.AddRange(_block, _localCount, list, true).ToVList();
			return this;
		}
		public VList<T> AddRange(IEnumerable<T> list)
		{
			this = VListBlock<T>.AddRange(_block, _localCount, list.GetEnumerator());
			return this;
		}
		public VList<T> InsertRange(int index, IReadOnlyList<T> list)
		{
			this = VListBlock<T>.InsertRange(_block, _localCount, list, Count - index, true).ToVList();
			return this;
		}
		public VList<T> RemoveRange(int index, int count)
		{
			if (count != 0)
				this = _block.RemoveRange(_localCount, Count - (index + count), count).ToVList();
			return this;
		}

		#endregion

		#region Other stuff

		/// <summary>Returns the last item of the list (at index Count-1), which is the head of the list.</summary>
		public T Last
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
		/// <summary>Removes the last item (at index Count-1) from the list and returns it.</summary>
		/// <param name="isEmpty">When the list is empty, this is set to true and default(T) is returned.</param>
		public T TryPop(out bool isEmpty)
		{
			if (!(isEmpty = _block == null))
			{
				T item = Last;
				this = WithoutLast(1);
				return item;
			}
			return default(T);
		}

		/// <summary>Gets the last item in the list (at index Count-1).</summary>
		/// <param name="isEmpty">When the list is empty, this is set to true and default(T) is returned.</param>
		public T TryPeek(out bool isEmpty)
		{
			if (!(isEmpty = _block == null))
				return Last;
			return default(T);
		}

		/// <summary>Removes the last item (at index Count-1) from the list and returns it.</summary>
		public T Pop()
		{
			if (_block == null)
				throw new InvalidOperationException("Pop: The list is empty.");
			T item = Last;
			this = WithoutLast(1);
			return item;
		}
		/// <summary>Synonym for Add(); adds an item to the front of the list.</summary>
		public VList<T> Push(T item) { return Add(item); }

		/// <summary>Returns this list as a FVList, which effectively reverses the
		/// order of the elements.</summary>
		/// <remarks>This is a trivial operation; the FVList shares the same memory.</remarks>
		public static explicit operator FVList<T>(VList<T> list)
		{
			return new FVList<T>(list._block, list._localCount);
		}
		/// <summary>Returns this list as a FVList, which effectively reverses the
		/// order of the elements.</summary>
		/// <returns>This is a trivial operation; the FVList shares the same memory.</returns>
		public FVList<T> ToFVList()
		{
			return new FVList<T>(_block, _localCount);
		}

		/// <summary>Returns this list as a FWList, which effectively reverses the
		/// order of the elements.</summary>
		/// <remarks>The list contents are not copied until you modify the FWList.</remarks>
		public static explicit operator FWList<T>(VList<T> list) { return list.ToFWList(); }
		/// <summary>Returns this list as a FWList, which effectively reverses the
		/// order of the elements.</summary>
		/// <remarks>The list contents are not copied until you modify the FWList.</remarks>
		public FWList<T> ToFWList()
		{
			return new FWList<T>(_block, _localCount, false);
		}

		/// <summary>Returns this list as an WList.</summary>
		/// <remarks>The list contents are not copied until you modify the WList.</remarks>
		public static explicit operator WList<T>(VList<T> list) { return list.ToWList(); }
		/// <summary>Returns this list as an WList.</summary>
		/// <remarks>The list contents are not copied until you modify the WList.</remarks>
		public WList<T> ToWList()
		{
			return new WList<T>(_block, _localCount, false);
		}

		/// <summary>Returns the VList converted to an array.</summary>
		public T[] ToArray()
		{
			return VListBlock<T>.ToArray(_block, _localCount, true);
		}

		/// <summary>Gets the number of blocks used by this list.</summary>
		/// <remarks>You might look at this property when optimizing your program,
		/// because the runtime of some operations increases as the chain length 
		/// increases. This property runs in O(BlockChainLength) time. Ideally,
		/// BlockChainLength is proportional to log_2(Count), but certain VList 
		/// usage patterns can produce long chains.</remarks>
		public int BlockChainLength
		{
			get { return _block == null ? 0 : _block.ChainLength; }
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
        /// This method performs a linear search, and is typically an O(n)
        /// operation, where n is Count. However, because the list is searched
        /// upward from index 0 to Count-1, if the list's blocks do not increase in
        /// size exponentially (due to the way that the list has been modified in
        /// the past), the search can have worse performance; the (unlikely) worst
        /// case is O(n^2). FVList(of T).IndexOf() doesn't have this problem.
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
			_block = VListBlock<T>.Insert(_block, _localCount, item, Count - index);
			_localCount = _block.ImmCount;
			return this;
		}

		void IList<T>.RemoveAt(int index) { RemoveAt(index); }
		public VList<T> RemoveAt(int index)
		{
			this = _block.RemoveAt(_localCount, Count - (index + 1)).ToVList();
			return this;
		}

		public T this[int index]
		{
			get {
				return _block.RGet(index, _localCount);
			}
			set {
				this = _block.ReplaceAt(_localCount, value, Count - 1 - index).ToVList();
			}
		}
		/// <summary>Gets an item from the list at the specified index; returns 
		/// defaultValue if the index is not valid.</summary>
		public T this[int index, T defaultValue]
		{
			get {
				if (_block != null)
					_block.RGet(index, _localCount, ref defaultValue);
				return defaultValue;
			}
		}

		#endregion

		#region ICollection<T> Members

		/// <summary>Inserts an item at the back (index Count) of the VList.</summary>
		void ICollection<T>.Add(T item) { Add(item); }
		/// <summary>Inserts an item at the back (index Count) of the VList.</summary>
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
				Debug.Assert((_localCount == 0) == (_block == null));
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

        /// <summary>Enumerates through a VList from index 0 up to index Count-1.
        /// </summary><remarks>
        /// Normally, enumerating the list takes O(Count + log(Count)^2) = O(Count)
		/// time. However, if the list's block chain does not increase in size 
		/// exponentially (due to the way that the list has been modified in the 
		/// past), the search can have worse performance; the worst case is O(n^2),
		/// but this is unlikely. FVList's Enumerator doesn't have this problem 
		/// because it enumerates in the other direction.</remarks>
		public struct Enumerator : IEnumerator<T>
		{
			ushort _localIndex;
			ushort _localCount;
			VListBlock<T> _curBlock;
			VListBlock<T> _nextBlock;
			FVList<T> _outerList;

			public Enumerator(VList<T> list)
				: this(new FVList<T>(list._block, list._localCount), new FVList<T>()) { }
			public Enumerator(VList<T> list, VList<T> subList)
				: this(new FVList<T>(list._block, list._localCount),
					   new FVList<T>(subList._block, subList._localCount)) { }
			public Enumerator(FVList<T> list) : this(list, new FVList<T>()) { }
			public Enumerator(FVList<T> list, FVList<T> subList)
			{
				_outerList = list;
				int localCount;
				_nextBlock = VListBlock<T>.FindNextBlock(ref subList, _outerList, out localCount)._block;
				_localIndex = (ushort)(checked((ushort)subList._localCount) - 1);
				_curBlock = subList._block;
				_localCount = checked((ushort)localCount);
			}

			public T Current
			{
				get { return _curBlock[_localIndex]; }
			}
			object System.Collections.IEnumerator.Current
			{
				get { return Current; }
			}
			public bool MoveNext()
			{
				if (++_localIndex >= _localCount) {
					_curBlock = _nextBlock;
					if (_curBlock == null)
						return false;

					int localCount;
					// The FVList constructed here usually violates the invariant
					// (_localCount == 0) == (_block == null), but FindNextBlock
					// doesn't mind. It's necessary to avoid the "subList is not
					// within list" exception in all cases.
					FVList<T> subList = new FVList<T>(_curBlock, 0);
					_nextBlock = VListBlock<T>.FindNextBlock(
						ref subList, _outerList, out localCount)._block;
					_localCount = checked((ushort)localCount);
					_localIndex = 0;
				}
				return true;
			}
			public void Reset()
			{
				throw new NotImplementedException();
			}
			public void Dispose()
			{
			}
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
			fail = _block == null || !_block.RGet(index, _localCount, ref value);
			return value;
		}

		IRange<T> IListSource<T>.Slice(int start, int count) { return Slice(start, count); }
		public Slice_<T> Slice(int start, int count = int.MaxValue) { return new Slice_<T>(this, start, count); }
		
		#endregion 

		#region ICloneable Members

		public VList<T> Clone() { return this; }
		object ICloneable.Clone() { return this; }

		#endregion

		#region Optimized LINQ-like methods

		/// <summary>Applies a filter to a list, to exclude zero or more
		/// items.</summary>
		/// <param name="keep">A function that chooses which items to include
		/// (exclude items by returning false).</param>
		/// <returns>The list after filtering has been applied. The original list
		/// structure is not modified.</returns>
		/// <remarks>
		/// If the predicate keeps the first N items it is passed, those N items are
		/// typically not copied, but shared between the existing list and the new 
		/// one.
		/// </remarks>
		public VList<T> SmartWhere(Func<T, bool> keep)
		{
			if (_localCount == 0)
				return this;
			else
				return (VList<T>)_block.Where(_localCount, keep, null);
		}

		/// <summary>Filters and maps a list with a user-defined function.</summary>
		/// <param name="filter">A function that chooses which items to include
		/// in a new list, and what to change them to.</param>
		/// <returns>The list after filtering has been applied. The original list
		/// structure is not modified.</returns>
		/// <remarks>
		/// This is a smart function. If the filter keeps the first N items it is 
		/// passed, those N items are typically not copied, but shared between the 
		/// existing list and the new one.
		/// </remarks>
		public VList<T> WhereSelect(Func<T,Maybe<T>> filter)
		{
			return Transform((int i, ref T item) => {
				var maybe = filter(item);
				if (!maybe.HasValue)
					return XfAction.Drop;
				else if (VListBlock<T>.EqualityComparer.Equals(item, item = maybe.Value))
					return XfAction.Keep;
				else
					return XfAction.Change;
			});
		}

		/// <summary>Maps a list to another list of the same length.</summary>
		/// <param name="map">A function that transforms each item in the list.</param>
		/// <returns>The list after the map function is applied to each item. The 
		/// original VList structure is not modified.</returns>
		/// <remarks>
		/// This method is called "Smart" because of what happens if the map
		/// doesn't do anything. If the map function returns the first N items
		/// unmodified, those N items are typically not copied, but shared between
		/// the existing list and the new one. This is useful for functional code
		/// that often processes a list without modifying it at all.
		/// </remarks>
		public VList<T> SmartSelect(Func<T, T> map)
		{
			if (_localCount == 0)
				return this;
			else
				return (VList<T>)_block.SmartSelect(_localCount, map, null);
		}

		/// <summary>Maps a list to another list by concatenating the outputs of a mapping function.</summary>
		/// <param name="map">A function that transforms each item in the list to a list of items.</param>
		/// <returns>A list that contains all the items returned from `map`.</returns>
		/// <remarks>
		/// This method is called "Smart" because of what happens if the map
		/// doesn't do anything. If, for the first N items, the `map` returns a 
		/// list of length 1, and that one item is the same item that was passed 
		/// in, then those N items are typically not copied, but shared between
		/// the existing list and the new one. This is useful for functional code
		/// that often processes a list without modifying it at all.
		/// </remarks>
		public VList<T> SmartSelectMany(Func<T, IReadOnlyList<T>> map)
		{
			if (_localCount == 0)
				return this;
			else
				return (VList<T>)_block.SelectMany(_localCount, map, true, null);
		}

		/*/// <summary>Maps a list to another list of the same length.</summary>
		/// <param name="map">A function that transforms each item in the list.</param>
		/// <returns>The list after the map function is applied to each item. The 
		/// original VList structure is not modified.</returns>
		public VList<Out> Select<Out>(Func<T, Out> map)
		{
			return (VList<Out>)VListBlock<T>.Select<Out>(_block, _localCount, map, null);
		}*/

		/// <summary>Transforms a list (combines filtering with selection and more).</summary>
		/// <param name="x">Method to apply to each item in the list</param>
		/// <returns>A list formed from transforming all items in the list</returns>
		/// <remarks>See the documentation of FVList.Transform() for more information.</remarks>
		public VList<T> Transform(VListTransformer<T> x)
		{
			return (VList<T>)VListBlock<T>.Transform(_block, _localCount, x, true, null);
		}

		#endregion
	}
}
