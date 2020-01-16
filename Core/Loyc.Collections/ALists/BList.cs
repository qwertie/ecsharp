namespace Loyc.Collections
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Collections.Specialized;
	using System.Diagnostics;
	using Loyc.Collections.Impl;
	using Loyc.Math;
	
	/// <summary>
	/// An sorted in-memory list that is efficient for all operations and offers 
	/// indexed access to its list.
	/// </summary>
	/// <remarks>
	/// An <a href="http://core.loyc.net/collections/alists-part2.html">article</a>
	/// about the BList classes is available.
	/// <para/>
	/// When you need a sorted list of items, there's nothing quite like a BList. BList offers
	/// numerous features that none of the standard .NET collections can offer:
	/// <ul>
	/// <li>O(log N) efficiency for all standard list operations (Add, Remove, 
	/// IndexOf, this[]) plus and O(1) fast cloning and O(1)-per-element enumeration.</li>
	/// <li>Changes can be observed through the <see cref="AListBase{K,T}.ListChanging"/> event.
	/// The performance penalty for this feature is lower than for the standard
	/// ObservableCollection{T} class.</li>
	/// <li>Changes to the tree structure can be observed too (see <see cref="IAListTreeObserver{K,T}"/>).</li>
	/// <li>The list can be frozen with <see cref="AListBase{K,T}.Freeze"/>, making it read-only.</li>
	/// <li><see cref="FindLowerBound"/> and <see cref="FindUpperBound"/> operations
	/// that find the nearest item equal to or greater than a specified item.</li>
	/// <li>A reversed view of the list is available through the <see cref="AListBase{K,T}.ReverseView"/> 
	/// property, and the list can be enumerated backwards, also in O(1) time 
	/// per element.</li>.
	/// <li>A BList normally uses less memory than a <see cref="SortedDictionary{K,V}"/> 
	/// or a hashtable such as <see cref="HashSet{T}"/> or <see cref="Dictionary{K,V}"/>.</li>
	/// <li>Other features inherited from <see cref="AListBase{T}"/></li>
	/// </ul>
	/// Please note, however, that <see cref="BList{T}"/> is generally slower than
	/// <see cref="Dictionary{K,V}"/> and <see cref="HashSet{T}"/>, so you should
	/// only use it when you need a sorted list of items, or when you need its
	/// special features such as <see cref="FindLowerBound"/> or observability.
	/// <para/>
	/// Caution: items must not be modified in a way that affects their sort order 
	/// after they are added to the list. If the list ever stops being sorted, it
	/// will malfunction, as it will no longer be possible to find some of the 
	/// items.
	/// </remarks>
	[Serializable]
	[DebuggerTypeProxy(typeof(ListSourceDebugView<>)), DebuggerDisplay("Count = {Count}")]
	public class BList<T> : AListBase<T, T>, IListSource<T>, ICollectionEx<T>, IAddRange<T>, ICloneable<BList<T>>
	{
		#region Constructors

		/// <summary>Initializes an empty BList.</summary>
		/// <remarks>By default, elements of the list will be compared using
		/// <see cref="Comparer{T}.Default"/>.Compare.</remarks>
		public BList() 
			: this(AListLeaf<T, T>.DefaultMaxNodeSize, AListInnerBase<T, T>.DefaultMaxNodeSize) { }
		/// <inheritdoc cref="BList(Func{T,T,int}, int, int)"/>
		public BList(int maxLeafSize)
			: this(maxLeafSize, AListInnerBase<T, T>.DefaultMaxNodeSize) { }
		/// <inheritdoc cref="BList(Func{T,T,int}, int, int)"/>
		public BList(int maxLeafSize, int maxInnerSize)
			: this(Comparer<T>.Default.Compare, maxLeafSize, maxInnerSize) { }
		/// <inheritdoc cref="BList(Func{T,T,int}, int, int)"/>
		public BList(Func<T, T, int> compareItems)
			: this(compareItems, AListLeaf<T, T>.DefaultMaxNodeSize, AListInnerBase<T, T>.DefaultMaxNodeSize) { }
		/// <inheritdoc cref="BList(Func{T,T,int}, int, int)"/>
		public BList(Func<T,T,int> compareItems, int maxLeafSize)
			: this(compareItems, maxLeafSize, AListInnerBase<T, T>.DefaultMaxNodeSize) { }
		
		/// <summary>Initializes an empty BList.</summary>
		/// <param name="compareItems">A method that compares two items and returns 
		/// -1 if the first item is smaller than the second item, 0 if it is equal,
		/// and 1 if it is greater.</param>
		/// <param name="maxLeafSize">Maximum number of elements to place in a leaf node of the B+ tree.</param>
		/// <param name="maxInnerSize">Maximum number of elements to place in an inner node of the B+ tree.</param>
		/// <remarks>
		/// If present, the compareKeys parameter must be a "Func" delegate instead 
		/// of the more conventional <see cref="Comparison{T}"/> delegate for an 
		/// obscure technical reason (specifically, it is the type required by 
		/// <see cref="AListSingleOperation{K,T}.CompareToKey"/>). You should not 
		/// notice any difference between the two, but the stupid .NET type system 
		/// insists that the two types are not compatible. So, if (for some reason) 
		/// you already happen to have a <see cref="Comparison{T}"/> delegate, you
		/// must explicitly convert it to a Func delegate with code such as 
		/// "new Func&lt;T,T,int>(comparisonDelegate)".
		/// <para/>
		/// If you leave out the compareKeys parameter, <see cref="Comparer{T}.Default"/>.Compare
		/// will be used by default.
		/// <para/>
		/// See the documentation of <see cref="AListBase{K,T}"/> for a discussion
		/// about node sizes.
		/// <para/>
		/// An empty BList is created with no root node, so it consumes much less 
		/// memory than a BList with a single element.
		/// </remarks>
		public BList(Func<T,T,int> compareItems, int maxLeafSize, int maxInnerSize)
			: base(maxLeafSize, maxInnerSize) { _compareItems = compareItems; }

		/// <inheritdoc cref="Clone(bool)"/>
		/// <param name="items">A list of items to be cloned.</param>
		public BList(BList<T> items, bool keepListChangingHandlers) 
			: base(items, keepListChangingHandlers) { _compareItems = items._compareItems; }

		protected BList(BList<T> original, AListNode<T, T> section) 
			: base(original, section) { _compareItems = original._compareItems; }

		#endregion

		/// <summary>Compares two items. See <see cref="Comparison{T}"/>.</summary>
		/// <remarks>Not marked readonly because the derived class constructor for BMultiMap needs to change it.</remarks>
		protected Func<T, T, int> _compareItems;

		#region General supporting protected methods

		protected override AListNode<T, T> NewRootLeaf()
		{
			return new BListLeaf<T, T>(_maxLeafSize);
		}
		protected override AListInnerBase<T, T> SplitRoot(AListNode<T, T> left, AListNode<T, T> right)
		{
			return new BListInner<T>(left, right, _maxInnerSize);
		}
		protected internal override T GetKey(T item)
		{
			return item;
		}

		#endregion

		#region Add, Remove, RemoveAll, Do

		void ICollection<T>.Add(T item) { Add(item); }
		void IAdd<T>.Add(T item) { Add(item); }
		public void Add(T item)
		{
			AListSingleOperation<T, T> op = new AListSingleOperation<T, T>();
			op.Mode = AListOperation.Add;
			op.CompareKeys = _compareItems;
			op.CompareToKey = _compareItems;
			op.Key = op.Item = item;
			DoSingleOperation(ref op);
		}

		/// <summary>Removes a single instance of the specified item.</summary>
		public bool Remove(T item)
		{
			return Do(AListOperation.Remove, item) != 0;
		}

		/// <summary>Removes all instances of the specified item.</summary>
		/// <param name="item">Item to remove</param>
		/// <returns>The number of instances removed (0 if none).</returns>
		/// <remarks>This method is not optimized. It takes twice as long as 
		/// <see cref="Remove(T)"/> if there is only one instance, because the 
		/// tree is searched twice.</remarks>
		public int RemoveAll(T item)
		{
			int change, total = 0;
			do
				total += (change = Do(AListOperation.Remove, item));
			while (change != 0);
			return -total;
		}

		/// <summary>Adds, removes, or replaces an item in the list.</summary>
		/// <param name="mode">Indicates the operation to perform.</param>
		/// <param name="item">An item to be added or removed in the list. If the
		/// item is passed by reference, and a matching item existed in the tree
		/// already, this method returns the old version of the item via this 
		/// parameter.</param>
		/// <returns>Returns the change in Count: 1 if the item was added, -1 if
		/// the item was removed, and 0 if the item replaced an existing item or 
		/// if nothing happened.</returns>
		public int Do(AListOperation mode, ref T item)
		{
			AListSingleOperation<T, T> op = new AListSingleOperation<T, T>();
			op.Mode = mode;
			op.CompareKeys = _compareItems;
			op.CompareToKey = _compareItems;
			op.Key = op.Item = item;
			int result = DoSingleOperation(ref op);
			item = op.Item;
			return result;
		}

		/// <inheritdoc cref="Do(AListOperation, T)"/>
		public int Do(AListOperation mode, T item)
		{
			AListSingleOperation<T, T> op = new AListSingleOperation<T, T>();
			op.Mode = mode;
			op.CompareKeys = _compareItems;
			op.CompareToKey = _compareItems;
			op.Key = op.Item = item;
			return DoSingleOperation(ref op);
		}

		#endregion

		#region AddRange, RemoveRange, DoRange

		/// <summary>Adds a set of items to the list, one at a time.</summary>
		/// <param name="e">A list of items to be added.</param>
		/// <returns>Returns the number of items that were added.</returns>
		/// <seealso cref="DoRange"/>
		void IAddRange<T>.AddRange(IReadOnlyCollection<T> e) { AddRange(e); }
		void IAddRange<T>.AddRange(IEnumerable<T> e) { AddRange(e); }

		/// <summary>Adds a set of items to the list, one at a time.</summary>
		/// <param name="e">A list of items to be added.</param>
		/// <returns>Returns the number of items that were added.</returns>
		/// <seealso cref="DoRange"/>
		public int AddRange(IEnumerable<T> e)
		{
			return DoRange(AListOperation.Add, e);
		}

		/// <summary>Removes a set of items from the list, one at a time.</summary>
		/// <param name="e">A list of items to be removed.</param>
		/// <returns>Returns the number of items that were found and removed.</returns>
		/// <seealso cref="DoRange"/>
		public int RemoveRange(IEnumerable<T> e)
		{
			return -DoRange(AListOperation.Remove, e);
		}

		/// <summary>Performs the same operation for each item in a series.
		/// Equivalent to calling <see cref="Do(AListOperation,T)"/> on each item.</summary>
		/// <param name="mode">Indicates the operation to perform.</param>
		/// <param name="e">A list of items to act upon.</param>
		/// <returns>Returns the change in Count: positive if items were added,
		/// negative if items were removed, and 0 if all items were unchanged or
		/// replaced.</returns>
		public int DoRange(AListOperation mode, IEnumerable<T> e)
		{
			AListSingleOperation<T, T> op = new AListSingleOperation<T, T>();
			op.Mode = mode;
			op.CompareKeys = _compareItems;
			op.CompareToKey = _compareItems;

			int delta = 0;
			foreach (T item in e)
			{
				op.Key = op.Item = item;
				// Some fields must be cleared before each operation
				op.BaseIndex = 0;
				op.Found = false;
				op.AggregateChanged = 0;
				delta += DoSingleOperation(ref op);
				Debug.Assert(op.Mode == mode);
			}
			return delta;
		}

		#endregion

		#region Bonus features delegated to AListBase: Clone, CopySection, RemoveSection, IsReadOnly

		/// <inheritdoc cref="Clone(bool)"/>
		public BList<T> Clone()
		{
			return Clone(true);
		}
		/// <summary>Clones a BList.</summary>
		/// <param name="keepListChangingHandlers">If true, ListChanging handlers
		/// will be copied from the existing list of items to the new list. Note: 
		/// if it exists, the NodeObserver is never copied. 
		/// <see cref="AListBase{K,T}.ObserverCount"/> will be zero in the new list.</param>
		/// <remarks>
		/// Cloning is performed in O(1) time by marking the tree root as frozen 
		/// and sharing it between the two lists. However, the new list itself will 
		/// not be frozen, even if the original list was marked as frozen. Instead,
		/// nodes will be copied on demand when you modify the new list.
		/// </remarks>
		public BList<T> Clone(bool keepListChangingHandlers)
		{
			return new BList<T>(this, keepListChangingHandlers);
		}

		public BList<T> CopySection(int start, int subcount)
		{
			return new BList<T>(this, CopySectionHelper(start, subcount));
		}
		public BList<T> RemoveSection(int start, int count)
		{
			if ((uint)count > _count - (uint)start)
				throw new ArgumentOutOfRangeException(count < 0 ? "count" : "start+count");
			
			var newList = new BList<T>(this, CopySectionHelper(start, count));
			// bug fix: we must RemoveRange after creating the new list, because 
			// the section is expected to have the same height as the original tree 
			// during the constructor of the new list.
			RemoveRange(start, count);
			return newList;
		}
		bool ICollection<T>.IsReadOnly
		{
			get { return IsFrozen; }
		}

		#endregion

		#region IndexOf, Contains, FindUpperBound, FindLowerBound, IndexOfExact

		/// <summary>Finds the lowest index of an item that is equal to or greater than the specified item.</summary>
		/// <param name="item">Item to find.</param>
		/// <returns>The lower-bound index, or Count if the item is greater than all items in the list.</returns>
		public int IndexOf(T item)
		{
			var op = new AListSingleOperation<T, T>();
			op.CompareKeys = _compareItems;
			op.CompareToKey = _compareItems;
			op.Key = item;
			op.LowerBound = true;
			OrganizedRetrieve(ref op);
			if (!op.Found)
				return -1;
			return (int)op.BaseIndex;
		}

		/// <summary>Returns true if the list contains the specified item, and false if not.</summary>
		public bool Contains(T item)
		{
			return IndexOf(item) > -1;
		}

		/// <summary>Finds the lowest index of an item that is equal to or greater than the specified item.</summary>
		/// <param name="item">The item to find. If passed by reference, when this 
		/// method returns, item is set to the item that was found, or to the next 
		/// greater item if the item was not found. If the item passed in is higher 
		/// than all items in the list, it will be left unchanged when this method 
		/// returns.</param>
		/// <param name="found">Set to true if the item was found, false if not.</param>
		/// <returns>The index of the item that was found, or of the next
		/// greater item, or Count if the given item is greater than all items 
		/// in the list.</returns>
		public int FindLowerBound(T item)
		{
			bool found;
			return FindLowerBound(item, out found);
		}
		/// <inheritdoc cref="FindLowerBound(T)"/>
		public int FindLowerBound(T item, out bool found)
		{
			var op = new AListSingleOperation<T, T>();
			op.CompareKeys = _compareItems;
			op.CompareToKey = _compareItems;
			op.Key = item;
			op.LowerBound = true;
			OrganizedRetrieve(ref op);
			found = op.Found;
			return (int)op.BaseIndex;
		}
		/// <inheritdoc cref="FindLowerBound(T)"/>
		public int FindLowerBound(ref T item)
		{
			bool found;
			return FindLowerBound(ref item, out found);
		}
		/// <inheritdoc cref="FindLowerBound(T)"/>
		public int FindLowerBound(ref T item, out bool found)
		{
			var op = new AListSingleOperation<T, T>();
			op.CompareKeys = _compareItems;
			op.CompareToKey = _compareItems;
			op.Key = item;
			op.LowerBound = true;
			OrganizedRetrieve(ref op);
			if (found = op.Found)
				item = op.Item;
			else if ((int)op.BaseIndex < Count)
				item = this[(int)op.BaseIndex];
			return (int)op.BaseIndex;
		}

		/// <summary>Finds the index of the first item in the list that is greater 
		/// than the specified item.</summary>
		/// <param name="item">The item to find. If passed by reference, when this 
		/// method returns, item is set to the next greater item than the item you 
		/// searched for, or left unchanged if there is no greater item.</param>
		/// <returns>The index of the next greater item that was found,
		/// or Count if the given item is greater than all items in the list.</returns>
		public int FindUpperBound(T item)
		{
			var op = new AListSingleOperation<T, T>();
			// When searchKey==candidate, act like searchKey>candidate.
			Func<T, T, int> upperBoundCmp = (candidate, searchKey) => -(_compareItems(searchKey, candidate) | 1);
			op.CompareKeys = upperBoundCmp;
			op.CompareToKey = upperBoundCmp;
			op.Key = item;
			OrganizedRetrieve(ref op);
			return (int)op.BaseIndex;
		}
		public int FindUpperBound(ref T item)
		{
			int index = FindUpperBound(item);
			if (index < Count)
				item = this[index];
			return index;
		}

		/// <summary>
		/// Specialized search function that finds the index of an item that not 
		/// only compares equal to the specified item according to the comparison 
		/// function for this collection, but is also equal according to 
		/// <see cref="Object.Equals"/>. This function works properly even if 
		/// duplicate items exist in addition that do NOT compare equal according 
		/// to <see cref="Object.Equals"/>.
		/// </summary>
		/// <remarks>
		/// This method is useful when the items in this collection are sorted by
		/// hashcode, or when they are sorted by key but not sorted by value. In 
		/// such cases, two items may be equal according to the comparison function 
		/// but unequal in reality.
		/// <para/>
		/// Implementation note: this method does a scan across the equal items to
		/// find the correct one, unlike the search technique controlled by
		/// <see cref="AListSingleOperation{K,T}.RequireExactMatch"/>, which is
		/// not guaranteed to work in case of duplicates.
		/// </remarks>
		public int IndexOfExact(T item)
		{
			T searchFor = item;
			bool found;
			int index = FindLowerBound(ref item, out found);
			if (!found)
				return -1;

			object searchFor2 = searchFor;
			while (item == null ? searchFor != null : !item.Equals(searchFor2))
			{
				if (++index >= Count)
					return -1;
				item = this[index];
				if (_compareItems(item, searchFor) != 0)
					return -1;
			}
			return index;
		}

		public override long CountSizeInBytes(int sizeOfElement, int sizeOfKey = 8) =>
			base.CountSizeInBytes(sizeOfElement) + IntPtr.Size;

		#endregion
	}
}
