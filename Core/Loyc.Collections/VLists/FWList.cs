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
using System.Diagnostics;
using System.Threading;
using Loyc.MiniTest;

namespace Loyc.Collections
{
	/// <summary>
	/// FWList is the mutable variant of the FVList data structure.
	/// </summary>
	/// An <a href="http://www.codeproject.com/Articles/26171/VList-data-structures-in-C">article</a>
	/// is available online about the VList data types.
	/// <para/>
	/// <remarks>See the remarks of <see cref="VListBlock{T}"/> for more information
	/// about VLists and WLists. It is most efficient to add items to the front of
	/// a FWList (at index 0) or the back of an WList (at index Count-1).</remarks>
	public sealed class FWList<T> : WListBase<T>, IListAndListSource<T>, ICloneable<FWList<T>>, ICloneable
	{
		protected override int AdjustWListIndex(int index, int size) { return index; }

		#region Constructors

		internal FWList(VListBlock<T> block, int localCount, bool isOwner)
			: base(block, localCount, isOwner) {}
		public FWList() {} // empty list is all null
		public FWList(int initialSize)
		{
			VListBlock<T>.MuAddEmpty(this, initialSize);
		}
		public FWList(T itemZero, T itemOne)
		{
			// Reverse order when constructing block because the second argument is
			// conceptually added second, so it will be at index [0].
			Block = new VListBlockOfTwo<T>(itemOne, itemZero, true);
			LocalCount = 2;
		}
		public FWList(IList<T> list)
		{
			AddRange(list);
		}
		
		#endregion
		
		#region AddRange, InsertRange, RemoveRange

		// Note: FWList doesn't offer AddRange(IEnumerable<T>) because it would 
		// add the items in reverse order (the first item enumerated would have 
		// the highest index). AddRange(IList<T>) adds list[list.Count-1] first.

		public void AddRange(IList<T> list) { AddRangeBase(list, false); }
		public void InsertRange(int index, IList<T> list) { InsertRangeAtDff(index, list, false); }
		public void RemoveRange(int index, int count)     { RemoveRangeBase(index, count); }

		#endregion

		#region IList<T>/ICollection<T> Members

		public new T this[int index]
		{
			get {
				return Block.FGet(index, LocalCount);
			}
			set {
				if ((uint)index >= (uint)Count)
					throw new IndexOutOfRangeException();
				VListBlock<T>.EnsureMutable(this, index + 1);
				SetAtDff(index, value);
			}
		}

		public new void Insert(int index, T item) { InsertAtDff(index, item); }
		public new void RemoveAt(int index) { RemoveAtDff(index); }

		/// <summary>Gets an item from the list at the specified index; returns 
		/// defaultValue if the index is not valid.</summary>
		public T this[int index, T defaultValue]
		{
			get {
				Block.FGet(index, LocalCount, ref defaultValue);
				return defaultValue;
			}
		}

		#endregion

		#region IEnumerable<T> Members

		protected override IEnumerator<T> GetIEnumerator() { return GetEnumerator(); }
		public new FVList<T>.Enumerator GetEnumerator()
		{
			return new FVList<T>.Enumerator(InternalVList);
		}
		public VList<T>.Enumerator ReverseEnumerator()
		{
			return new VList<T>.Enumerator(InternalVList);
		}

		#endregion

		#region IListSource<T> Members

		public new T TryGet(int index, out bool fail)
		{
			T value = default(T);
			fail = Block == null || !Block.FGet(index, LocalCount, ref value);
			return value;
		}
		
		#endregion 

		#region ICloneable Members

		public FWList<T> Clone() {
			VListBlock<T>.EnsureImmutable(Block, LocalCount);
			return new FWList<T>(Block, LocalCount, false);
		}
		object ICloneable.Clone() { return Clone(); }

		#endregion

		#region LINQ-like methods

		/// <summary>Applies a filter to a list, to exclude zero or more
		/// items.</summary>
		/// <param name="keep">A function that chooses which items to include
		/// (exclude items by returning false).</param>
		/// <returns>The list after filtering has been applied. The original VList
		/// structure is not modified.</returns>
		/// <remarks>
		/// If the predicate keeps the first N items it is passed (which are the
		/// last or "tail" items in a FWList), those N items are typically not 
		/// copied, but shared between the existing list and the new one.
		/// </remarks>
		public FWList<T> SmartWhere(Func<T, bool> keep)
		{
			FWList<T> newList = new FWList<T>();
			if (LocalCount != 0)
				Block.Where(LocalCount, keep, newList);
			return newList;
		}

		/// <summary>Filters and maps a list with a user-defined function.</summary>
		/// <param name="filter">A function that chooses which items to include
		/// in a new list, and what to change them to.</param>
		/// <returns>The list after filtering has been applied. The original list
		/// structure is not modified.</returns>
		/// <remarks>
		/// This is a smart function. If the filter does not modify the first N 
		/// items it is passed (which are the last items in a FWList), those N items 
		/// are typically not copied, but shared between the existing list and the 
		/// new one.
		/// </remarks>
		public FWList<T> WhereSelect(Func<T,Maybe<T>> filter)
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
		/// unmodified (the items at the tail of the FWList), those N items are 
		/// typically not copied, but shared between the existing list and the 
		/// new one.
		/// </remarks>
		public FWList<T> SmartSelect(Func<T, T> map)
		{
			FWList<T> newList = new FWList<T>();
			if (LocalCount != 0)
				Block.SmartSelect(LocalCount, map, newList);
			return newList;
		}

		/*/// <summary>Maps a list to another list of the same length.</summary>
		/// <param name="map">A function that transforms each item in the list.</param>
		/// <returns>The list after the map function is applied to each item. The 
		/// original VList structure is not modified.</returns>
		public FWList<Out> Select<Out>(Func<T, Out> map)
		{
			FWList<Out> newList = new FWList<Out>();
			VListBlock<T>.Select<Out>(Block, LocalCount, map, newList);
			return newList;
		}*/

		/// <summary>Transforms a list (combines filtering with selection and more).</summary>
		/// <param name="x">Method to apply to each item in the list</param>
		/// <returns>A list formed from transforming all items in the list</returns>
		/// <remarks>See the documentation of FVList.Transform() for more information.</remarks>
		public FWList<T> Transform(VListTransformer<T> x)
		{
			FWList<T> newList = new FWList<T>();
			VListBlock<T>.Transform(Block, LocalCount, x, false, newList);
			return newList;
		}

		#endregion

		#region Other stuff

		/// <summary>Returns the front item of the list (at index 0).</summary>
		public T First
		{
			get {
				return Block.Front(LocalCount);
			}
		}
		public bool IsEmpty
		{
			get {
				return Count == 0;
			}
		}
		/// <summary>Removes the front item (at index 0) from the list and returns it.</summary>
		public T Pop()
		{
			if (Block == null)
				throw new InvalidOperationException("Pop: The list is empty.");
			T item = First;
			RemoveAtDff(0);
			return item;
		}

		public FVList<T> WithoutFirst(int numToRemove)
		{
			return VListBlock<T>.EnsureImmutable(Block, LocalCount - numToRemove);
		}

		/// <summary>Returns this list as an WList, which effectively reverses 
		/// the order of the elements.</summary>
		/// <remarks>This operation marks the items of the list as immutable.
		/// You can modify either list afterward, but some or all of the list 
		/// may have to be copied.</remarks>
		public static explicit operator WList<T>(FWList<T> list) { return list.ToWList(); }
		/// <summary>Returns this list as an WList, which effectively reverses 
		/// the order of the elements.</summary>
		/// <remarks>This operation marks the items of the list as immutable.
		/// You can modify either list afterward, but some or all of the list 
		/// may have to be copied.</remarks>
		public WList<T> ToWList()
		{
			VListBlock<T>.EnsureImmutable(Block, LocalCount);
			return new WList<T>(Block, LocalCount, false);
		}

		/// <summary>Returns the FWList converted to an array.</summary>
		public T[] ToArray()
		{
			return VListBlock<T>.ToArray(Block, LocalCount, false);
		}

		#endregion
	}
}
