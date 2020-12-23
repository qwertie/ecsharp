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
	/// WList is the mutable variant of the VList data structure.
	/// </summary>
	/// <remarks>
	/// An <a href="http://www.codeproject.com/Articles/26171/VList-data-structures-in-C">article</a>
	/// is available online about the VList data types.
	/// <para/>
	/// See the remarks of <see cref="VListBlock{T}"/> for more information
	/// about VLists and WLists. It is most efficient to add items to the front of
	/// a FWList (at index 0) or the back of an WList (at index Count-1).</remarks>
	public sealed class WList<T> : WListBase<T>, IListAndListSource<T>, ICloneable<WList<T>>, ICloneable
	{
		protected override int AdjustWListIndex(int index, int size) { return Count - size - index; }

		#region Constructors

		internal WList(VListBlock<T> block, int localCount, bool isOwner)
			: base(block, localCount, isOwner) {}
		public WList() {} // empty list is all null
		public WList(T itemZero, T itemOne)
		{
			Block = new VListBlockOfTwo<T>(itemZero, itemOne, true);
			LocalCount = 2;
		}
		public WList(IEnumerable<T> list)
		{
			AddRange(list);
		}
		
		#endregion
		
		#region AddRange, InsertRange, RemoveRange

		public void AddRange(IEnumerable<T> items) { AddRange(items.GetEnumerator()); }
		public new void AddRange(IEnumerator<T> items) { base.AddRange(items); }
		public void AddRange(IReadOnlyList<T> list) { AddRangeBase(list, true); }
		public void InsertRange(int index, IReadOnlyList<T> list) { InsertRangeAtDff(Count - index, list, true); }
		public void RemoveRange(int index, int count)     { RemoveRangeBase(Count - (index + count), count); }

		#endregion

		#region IList<T>/ICollection<T> Members

		public new T this[int index]
		{
			get {
				return Block.RGet(index, LocalCount);
			}
			set {
				if ((uint)index >= (uint)Count)
					throw new IndexOutOfRangeException();
				int dff = Count - (index + 1);
				VListBlock<T>.EnsureMutable(this, dff + 1);
				SetAtDff(dff, value);
			}
		}

		public new void Insert(int index, T item) { InsertAtDff(Count - index, item); }
		public new void RemoveAt(int index) { RemoveAtDff(Count - (index + 1)); }

		/// <summary>Gets an item from the list at the specified index; returns 
		/// defaultValue if the index is not valid.</summary>
		public T this[int index, T defaultValue]
		{
			get {
				Block.RGet(index, LocalCount, ref defaultValue);
				return defaultValue;
			}
		}

		#endregion

		#region IEnumerable<T> Members

		protected override IEnumerator<T> GetIEnumerator() { return GetEnumerator(); }
		public new VList<T>.Enumerator GetEnumerator()
		{
			return new VList<T>.Enumerator(InternalVList); 
		}
		public FVList<T>.Enumerator ReverseEnumerator()
		{
			return new FVList<T>.Enumerator(InternalVList);
		}

		#endregion

		#region IListSource<T> Members

		public new T TryGet(int index, out bool fail)
		{
			T value = default(T);
			fail = Block == null || !Block.RGet(index, LocalCount, ref value);
			return value;
		}
		
		#endregion 

		#region ICloneable Members

		public WList<T> Clone() {
			VListBlock<T>.EnsureImmutable(Block, LocalCount);
			return new WList<T>(Block, LocalCount, false);
		}
		object ICloneable.Clone() { return Clone(); }

		#endregion

		#region LINQ-like methods

		/// <summary>Applies a filter to a list, to exclude zero or more
		/// items.</summary>
		/// <param name="filter">A function that chooses which items to include
		/// (exclude items by returning false).</param>
		/// <returns>The list after filtering has been applied. The original VList
		/// structure is not modified.</returns>
		/// <remarks>
		/// If the predicate keeps the first N items it is passed (which are the
		/// last or "tail" items in a WList), those N items are typically not 
		/// copied, but shared between the existing list and the new one.
		/// </remarks>
		public WList<T> Where(Func<T,bool> filter)
		{
			WList<T> newList = new WList<T>();
			if (LocalCount != 0)
				Block.Where(LocalCount, filter, newList);
			return newList;
		}

		/// <summary>Filters and maps a list with a user-defined function.</summary>
		/// <param name="filter">A function that chooses which items to include
		/// in a new list, and what to change them to.</param>
		/// <returns>The list after filtering has been applied. The original list
		/// structure is not modified.</returns>
		/// <remarks>
		/// This is a smart function. If the filter does not modify the first N 
		/// items it is passed those N items are typically not copied, but shared 
		/// between the existing list and the new one.
		/// </remarks>
		public WList<T> WhereSelect(Func<T,Maybe<T>> filter)
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
		/// unmodified (the items at the tail of the WList), those N items are 
		/// typically not copied, but shared between the existing list and the 
		/// new one.
		/// </remarks>
		public WList<T> SmartSelect(Func<T, T> map)
		{
			WList<T> newList = new WList<T>();
			if (LocalCount != 0)
				Block.SmartSelect(LocalCount, map, newList);
			return newList;
		}

		/*/// <summary>Maps a list to another list of the same length.</summary>
		/// <param name="map">A function that transforms each item in the list.</param>
		/// <returns>The list after the map function is applied to each item. The 
		/// original VList structure is not modified.</returns>
		public WList<Out> Select<Out>(Func<T, Out> map)
		{
			WList<Out> newList = new WList<Out>();
			VListBlock<T>.Select<Out>(Block, LocalCount, map, newList);
			return newList;
		}*/

		/// <summary>Transforms a list (combines filtering with selection and more).</summary>
		/// <param name="x">Method to apply to each item in the list</param>
		/// <returns>A list formed from transforming all items in the list</returns>
		/// <remarks>See the documentation of FVList.Transform() for more information.</remarks>
		public WList<T> Transform(VListTransformer<T> x)
		{
			WList<T> newList = new WList<T>();
			VListBlock<T>.Transform(Block, LocalCount, x, true, newList);
			return newList;
		}

		#endregion

		#region Other stuff

		/// <summary>Returns the last item of the list (at index Count-1).</summary>
		public T Last
		{
			get {
				try {
					return Block.Front(LocalCount);
				} catch (NullReferenceException) {
					throw new EmptySequenceException();
				}
			}
			set {
				if (IsEmpty) throw new EmptySequenceException();
				VListBlock<T>.EnsureMutable(this, 1);
				SetAtDff(0, value);
			}
		}
		/// <summary>Removes the back item (at index Count-1) from the list and returns it.</summary>
		public T Pop()
		{
			if (Block == null)
				throw new InvalidOperationException("Pop: The list is empty.");
			T item = Last;
			RemoveAtDff(0);
			return item;
		}

		public VList<T> WithoutLast(int numToRemove)
		{
			return VListBlock<T>.EnsureImmutable(Block, LocalCount - numToRemove).ToVList();
		}

		/// <summary>Returns this list as a FWList, which effectively reverses 
		/// the order of the elements.</summary>
		/// <remarks>This operation marks the items of the list as immutable.
		/// You can modify either list afterward, but some or all of the list 
		/// may have to be copied.</remarks>
		public static explicit operator FWList<T>(WList<T> list) { return list.ToFWList(); }
		/// <summary>Returns this list as a FWList, which effectively reverses 
		/// the order of the elements.</summary>
		/// <remarks>This operation marks the items of the list as immutable.
		/// You can modify either list afterward, but some or all of the list 
		/// may have to be copied.</remarks>
		public FWList<T> ToFWList()
		{
			VListBlock<T>.EnsureImmutable(Block, LocalCount);
			return new FWList<T>(Block, LocalCount, false);
		}

		/// <summary>Returns the WList converted to an array.</summary>
		public T[] ToArray()
		{
			return VListBlock<T>.ToArray(Block, LocalCount, true);
		}

		/// <summary>Resizes the list to the specified size.</summary>
		/// <remarks>If the new size is larger than the old size, empty elements 
		/// are added to the end. If the new size is smaller, elements are 
		/// truncated from the end.
		/// <para/>
		/// I decided not to offer a Resize() method for the FWList because the 
		/// natural place to insert or remove items in a FWList is at the beginning.
		/// For a Resize() method to do so, I felt, would come as too much of a 
		/// surprise to some programmers.
		/// </remarks>
		public void Resize(int newSize)
		{
			int change = newSize - Count;
			if (change > 0)
				VListBlock<T>.MuAddEmpty(this, change);
			else if (change < 0)
				RemoveRangeBase(0, -change);
		}

		#endregion
	}
}
