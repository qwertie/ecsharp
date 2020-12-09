namespace Loyc.Collections
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Collections.Specialized;
	using System.Diagnostics;
	using System.Runtime.CompilerServices;
	using Loyc.Collections.Impl;
	using Loyc.Math;
	using System.ComponentModel;

	/// <summary>
	/// Base class for the indexed tree-based data structures known as
	/// <see cref="AList{T}"/> and <see cref="BList{T}"/>.
	/// </summary>
	/// <remarks>
	/// You can read <a href="http://core.loyc.net/collections/">articles</a> about
	/// the AList family of data structures online.
	/// <para/>
	/// <see cref="AList{T}"/> and <see cref="BList{T}"/> are excellent data 
	/// structures to choose if you aren't sure what your requirements are. The 
	/// main difference between them is that BList is sorted and AList is not. 
	/// <see cref="DList{T}"/>, meanwhile, is a simpler data structure with a 
	/// faster indexer and lower memory requirements. In fact, the leaf nodes
	/// of this class are built upon <see cref="InternalDList{T}"/>.
	/// <para/>
	/// Classes derived from AListBase typically have the following abilities:
	/// <ul>
	/// <li>Items can always be inserted or removed in O(log N) time (by convention,
	/// N refers to the number of items in the list, although it is the 
	/// <see cref="Count"/> property that actually returns this number).</li>
	/// <li>Elements can also be accessed by index in O(log N) time.</li>
	/// <li>Scanning the list with an enumerator or foreach loop takes O(1) time 
	/// per element (plus O(log N) to initialize the enumerator), and it is 
	/// possible to start enumerating somewhere in the middle of the list.</li>
	/// <li>The list can be frozen with <see cref="Freeze"/>, making it read-only.</li>
	/// <li>The list can be cloned in O(1) time and space, with incremental copy-
	/// on-write semantics. Thus, changing either copy of the tree costs extra time
	/// and space in order to duplicate parts of the tree that are changing. A 
	/// frozen list can be cloned to produce a non-frozen list. In the language of
	/// comp-sci techno-babble, A-lists are related to the family of "persistent"
	/// data structures, although I could not find a term that describes the form 
	/// of persistence that A-lists support.</li>
	/// <li>Changes can be observed through the <see cref="ListChanging"/> event.
	/// The performance penalty for this feature is lower than for the standard
	/// ObservableCollection{T} class.</li>
	/// <li>Changes to the tree structure can be observed by writing a class that
	/// implements <see cref="IAListTreeObserver{K,T}"/> and calling 
	/// <see cref="AddObserver"/>.</li>
	/// <li>A section of the list can be cloned in O(log N) time, although there is 
	/// no time savings when extracting a small section.</li>
	/// <li>Removing a contiguous group of items takes O(log N + M) time, where M is
	/// the number of items being removed.</li>
	/// <li>A reversed view of the list is given by the <see cref="ReverseView"/> 
	/// property, and the list can be enumerated backwards, also in O(1) time 
	/// per element.</li>.
	/// </ul>
	/// Derived classes provide the following additional capabilities:
	/// <ul>
	/// <li><see cref="BList{T}"/> and <see cref="IndexedAList{T}"/> allow you to 
	/// find items in O(log N) time. BList is a sorted B+tree, so it achieves this 
	/// ability through a kind of binary search, while IndexedAList uses a special 
	/// kind of hashtable maintained in parallel with the tree.</li>
	/// <li><see cref="BList{T}.FindLowerBound"/>allows you to find the closest 
	/// larger item to an item that is not in the list. For example, if a BList(of 
	/// int) contains { 1, 3, 9 }, you can search for 5 in order to find 9.</li>
	/// <li><see cref="AList{T}"/> allows a contiguous group of items to be 
	/// inserted in O(log N + M) time, where M is the number of items being 
	/// inserted.</li>
	/// <li><see cref="AList{T}"/> allows you to concatenate two lists in O(log N) 
	/// time, where N is the size of the combined list, although there is no time
	/// savings when combining small lists.</li>
	/// <li><see cref="AList{T}"/> has a special sorting algorithm that allows you 
	/// to sort the list in slightly more than O(N log N) time. This is slower than
	/// sorting an array or <see cref="List{T}"/>, but faster than a quicksort 
	/// applied directly to <see cref="AList{T}"/>, which would take O(N log^2 N) 
	/// time.</li>
	/// </ul>
	/// As you can see, the A-List family of data structures allows you to 
	/// accomplish a wide variety of tasks efficiently, so the letter "A" in A-List 
	/// stands for All-purpose.
	/// <para/>
	/// A-Lists use memory efficiently because they are similar to B+trees. A-Lists 
	/// tend to use slightly more memory than <see cref="List{T}"/> but have lower 
	/// peak memory usage, because their size never suddenly doubles. A-Lists use 
	/// much less memory than <see cref="HashSet{T}"/>, so they are useful for 
	/// storing large data sets in RAM.
	/// <para/>
	/// The efficiency of various operations is affected by the maximum sizes of the
	/// inner nodes and leaf nodes, which can be set independently when constructing 
	/// an <see cref="AList{T}"/> or <see cref="BList{T}"/>. Generally, larger leaf
	/// nodes allow faster indexing but slower insertions and removals. If the list
	/// is cloned frequently, smaller inner and leaf nodes will speed up all 
	/// modifications (insertions, removals, and modification of individual items), 
	/// although small node sizes suffer increased overhead (higher memory usage and 
	/// slower indexing). The default inner node size of 16 is usually best for 
	/// performance, because the binary searches required for lookups will have good
	/// locality-of-reference. Small size limits (less than 8) have a high overhead 
	/// and are not recommended for any purpose except unit testing. The minimum
	/// allowed maximum node size is 3.
	/// <para/>
	/// In general, the tree is structured so that indexing is faster than 
	/// insertions and removals, even though these three operations have the same 
	/// scalability, O(log N).
	/// <para/>
	/// In general, this family of classes is NOT multithread-safe; concurrency 
	/// support is the only major feature that AListBase lacks. It supports 
	/// multiple readers concurrently, as long as the collection is not modified,
	/// so frozen instances ARE multithread-safe. However, classes derived from 
	/// AListBase must not be accessed from other threads during any modification. 
	/// There is a mechanism to detect illegal concurrent access and throw 
	/// InvalidOperationException if it is detected, but this is not designed to be 
	/// reliable; its main purpose is to help you find bugs. If concurrent 
	/// modification is not detected, the AList will probably become corrupted and 
	/// produce strange exceptions, or fail an assertion (in debug builds).
	/// <para/>
	/// Although AListBase provides neither concurrent operations nor safety for
	/// concurrent modification, the fast-clone feature makes it a useful parallel 
	/// data structure. Consider, for instance, a document editor that wants to 
	/// run a spell-check on a background thread, with an AList(of Char) object 
	/// representing the document. The spell checker cannot access the document 
	/// if the user might modify it concurrently; luckily, it can be cloned 
	/// instantly before starting the spell-checking thread. The root of the AList's
	/// B+ tree is marked frozen, and when the user modifies the original document,
	/// the AList will duplicate a few frozen nodes in order to modify them without
	/// crashing the spell-check operation that is running concurrently. You will,
	/// of course, have to deal with the fact that the spell-check results apply 
	/// to an old version of the document, but at least the program won't crash 
	/// and the user won't be blocked from editing while the spellcheck is running.
	/// <para/>
	/// The disadvantage of the fast-clone approach is that the immutability of the
	/// tree cannot be cancelled. So if the spell-check finishes after a half-second
	/// and the user did not modify the document, the tree still remains frozen and
	/// will still have to be copied later when the user does modify it. Still, the
	/// copying is a pay-as-you-go cost, so the app will stay responsive and the 
	/// entire tree will not be copied unless you make widespread changes to the 
	/// collection (e.g. converting the document to all-uppercase.)
	/// </remarks>
	/// <typeparam name="K">Type of keys that are used to classify items in a tree</typeparam>
	/// <typeparam name="T">Type of each element in the list. The derived class 
	/// must implement the <see cref="GetKey"/> method that converts T to K.</typeparam>
	[Serializable]
	public abstract partial class AListBase<K, T> : IListSource<T>, INotifyListChanging<T>, ITryGet<int, T>, IIndexed<int, T>
	{
		#region Data members

		protected internal ListChangingHandler<T, IListSource<T>> _listChanging; // Delegate for ListChanging
		protected internal AListNode<K, T> _root;
		protected internal IAListTreeObserver<K, T> _observer;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		protected uint _count;
		protected ushort _version;
		protected ushort _maxLeafSize;
		protected byte _maxInnerSize;
		protected byte _treeHeight; // 0=empty, 1=leaf only
		protected FreezeMode _freezeMode = FreezeMode.NotFrozen;

		[Flags]
		protected internal enum FreezeMode : byte
		{
			NotFrozen = 0,
			Frozen = 1,
			FrozenForListChanging = 2,
			FrozenForConcurrency = 3
		}

		/// <summary>Event for learning about changes in progress on a list.</summary>
		public virtual event ListChangingHandler<T, IListSource<T>> ListChanging
		{
			add {
				lock (this) { _listChanging += value; }
			}
			remove {
				lock (this) { _listChanging -= value; }
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)] // hide from IntelliSense
		[DebuggerBrowsable(DebuggerBrowsableState.Never)] // redundant
		public byte TreeHeight { get { return _treeHeight; } }

		public int Count => (int)_count;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsEmpty => _count == 0;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public T First => this[0];
		
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public T Last
		{
			get {
				if (_root == null)
					ThrowIndexOutOfRange();
				if (_freezeMode == FreezeMode.FrozenForConcurrency)
					ThrowFrozen();
				return _root.GetLastItem();
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsFrozen => _freezeMode == FreezeMode.Frozen;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public AListReverseView<K, T> ReverseView => new AListReverseView<K, T>(this);

		#endregion

		#region Constructors

		protected AListBase()
		{
			_maxLeafSize = AListLeaf<K, T>.DefaultMaxNodeSize;
			_maxInnerSize = AListInnerBase<K, T>.DefaultMaxNodeSize;
		}
		protected AListBase(int maxNodeSize) : this(maxNodeSize, maxNodeSize)
		{
		}
		protected AListBase(int maxLeafSize, int maxInnerSize)
		{
			_maxLeafSize = (ushort)Math.Min(maxLeafSize, 0xFFFF);
			if (maxLeafSize < 3)
				CheckParam.ThrowOutOfRange("maxLeafSize");
			_maxInnerSize = (byte)Math.Min(maxInnerSize, 0xFF);
			if (maxInnerSize < 3)
				CheckParam.ThrowOutOfRange("maxInnerSize");
		}

		/// <summary>Cloning constructor. Does not duplicate the observer 
		/// (<see cref="IAListTreeObserver{K,T}"/>), if any, because it may not be 
		/// cloneable.</summary>
		/// <param name="items">Original list</param>
		/// <param name="keepListChangingHandlers">Whether to duplicate the 
		/// delegate for ListChanging; if false, this new object will not include 
		/// any handlers for the ListChanging event.</param>
		/// <remarks>This constructor leaves the new clone unfrozen.</remarks>
		protected AListBase(AListBase<K, T> items, bool keepListChangingHandlers)
		{
			if (items._freezeMode == FreezeMode.FrozenForConcurrency)
				items.ThrowFrozen(); // cannot clone concurrently!
			if ((_root = items._root) != null)
				_root.Freeze();
			_count = items._count;
			_maxLeafSize = items._maxLeafSize;
			_treeHeight = items._treeHeight;
			if (keepListChangingHandlers && items._listChanging != null)
				_listChanging = (ListChangingHandler<T, IListSource<T>>)items._listChanging.Clone();
			// Leave _freezeMode at NotFrozen and _version at 0
		}

		/// <summary>
		/// This is the constructor that CopySection(), which can be defined by 
		/// derived classes, should call to create a sublist of a list. Used in 
		/// conjunction with CopySectionHelper().
		/// </summary>
		protected AListBase(AListBase<K, T> original, AListNode<K, T> section)
		{
			_maxLeafSize = original._maxLeafSize;
			_maxInnerSize = original._maxInnerSize;
			_treeHeight = original._treeHeight;
			if (section != null)
			{
				_count = section.TotalCount;
				HandleChangedOrUndersizedRoot(section);
			}
		}
		
		#endregion

		#region General supporting protected methods

		protected abstract AListNode<K, T> NewRootLeaf();
		protected abstract AListInnerBase<K, T> SplitRoot(AListNode<K, T> left, AListNode<K, T> right);
		
		protected virtual Enumerator NewEnumerator(uint start, uint firstIndex, uint lastIndex)
		{
			return new Enumerator(this, start, firstIndex, lastIndex);
		}
		/// <summary>Retrieves the key K from an item T. This method is only needed by "B" lists.</summary>
		protected internal abstract K GetKey(T item);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void CheckPoint()
		{
			//uint c;
			Debug.Assert(_count == (_root == null ? 0 : _root.TotalCount));
			//Debug.Assert(!(_observer is AListIndexerBase<T>) || (c = ((AListIndexerBase<T>)_observer).Count) == _count
			//			 || (c == 0 && _root is AListLeaf<T>));
			if (_observer != null)
				_observer.CheckPoint();
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void AutoThrow()
		{
			if (_freezeMode != FreezeMode.NotFrozen) ThrowFrozen();
		}
		private void ThrowFrozen()
		{
			string name = GetType().NameWithGenericArgs();
			if (_freezeMode == FreezeMode.FrozenForListChanging)
				throw new InvalidOperationException(Localize.Localized("Cannot insert or remove items in {0} during a ListChanging event.", name));
			else if (_freezeMode == FreezeMode.FrozenForConcurrency)
				throw new ConcurrentModificationException(Localize.Localized("{0} was accessed concurrently while being modified.", name));
			else if (_freezeMode == FreezeMode.Frozen)
				throw new ReadOnlyException(Localize.Localized("Cannot modify {0} when it is frozen.", name));
			else
				throw new InvalidStateException();
		}
		protected internal void CallListChanging(ListChangeInfo<T> listChangeInfo)
		{
			Debug.Assert(_freezeMode == FreezeMode.NotFrozen || _freezeMode == FreezeMode.FrozenForConcurrency);
			if (_listChanging != null)
			{
				// Freeze the list during ListChanging
				var old = _freezeMode;
				_freezeMode = FreezeMode.FrozenForListChanging;
				try {
					_listChanging(this, listChangeInfo);
				} finally {
					_freezeMode = old;
				}
			}
		}
		protected void AutoCreateOrCloneRoot()
		{
			if (_root == null) {
				Debug.Assert(_count == 0);
				_root = NewRootLeaf();
				_treeHeight = 1;
				if (_observer != null)
					_observer.RootChanged(this, _root, false);
			} else if (_root.IsFrozen) {
				AListNode<K,T>.AutoClone(ref _root, null, _observer);
				if (_observer != null)
					_observer.RootChanged(this, _root, false);
			}
		}
		protected void AutoSplit(AListNode<K, T> splitLeft, AListNode<K, T> splitRight)
		{
			if (splitLeft == null)
				return;
			if (splitRight == null)
			{
				var oldRoot = _root;
				HandleChangedOrUndersizedRoot(splitLeft);
				if (_observer != null && _root != oldRoot)
					_observer.RootChanged(this, _root, false);
			}
			else
			{
				var oldRoot = _root;
				_root = SplitRoot(splitLeft, splitRight);
				_treeHeight++;
				if (_observer != null)
					_observer.HandleRootSplit(this, oldRoot, splitLeft, splitRight, (AListInnerBase<K,T>)_root);
			}
		}
		protected void HandleChangedOrUndersizedRoot(AListNode<K, T> result)
		{
			_root = result;
			while (_root.LocalCount <= 1)
			{
				if (_root.LocalCount == 0) {
					if (_root.TotalCount > 0)
						break; // this can only happen in a sparse list
					_root = null;
					_treeHeight = 0;
					if (_observer != null)
						_observer.RootChanged(this, _root, false);
					return;
				} else if (_root is AListInnerBase<K, T>) {
					var inner = (AListInnerBase<K, T>)_root;
					_root = inner.Child(0);
					checked { _treeHeight--; }
					if (_observer != null)
						_observer.HandleRootUnsplit(this, inner, _root);
					Debug.Assert((_treeHeight == 1) == _root.IsLeaf);
				} else
					return; // leaf with 1 item. Leave it alone.
			}
		}

		#endregion

		#region DoSingleOperation (front-end to most operations in an organized tree)

		/// <summary>Performs an operation on a single item in an organized tree.</summary>
		/// <param name="op">Describes the operation to perform. The following 
		/// members must be initialized: Mode, CompareKeys, CompareToKey, Item, and
		/// Key. Also, set <see cref="AListSingleOperation{K,T}.RequireExactMatch"/> 
		/// if desired.</param>
		/// <returns>Returns the number of items that were added or removed, which
		/// is always 1, 0, or -1.</returns>
		/// <remarks>
		/// This method can be used to add, remove, or replace an item in an
		/// organized tree such as a B+ tree. It also finds the index of the item
		/// that was found, added, removed, or replaced, so it can be used to
		/// implement IndexOf(K) for a key K. Please note that in a dictionary, 
		/// this method cannot find an exact item (key-value pair) reliably when 
		/// duplicate keys exist.*
		/// <para/>
		/// This method could be used to find items also, but it assumes the 
		/// operation might modify the tree and therefore enables concurrent
		/// access detection and will create the root node if it doesn't exist.
		/// Therefore, you should call <see cref="OrganizedRetrieve"/> if you 
		/// only want to retrieve an item from the tree.
		/// <para/>
		/// See the documentation of <see cref="AListSingleOperation{K,T}"/> and
		/// <see cref="AListOperation"/> for more information.
		/// <para/>
		/// * Actually it is possible in a B+ tree, but requires a specially-
		///   designed derived class. Specifically, it is necessary to store 
		///   key-value pairs in inner nodes instead of just keys (so K := T),
		///   and to implement two different sorting functions. One sort function
		///   only compares keys, but this function can only be used for find
		///   and remove operations. The other function compares entire key-value 
		///   pairs in order to find an exact match (note that this requires 
		///   ordered values); this second function must be used for all add 
		///   operations, otherwise the tree may not stay sorted. In this kind
		///   of tree, replacements are generally unsafe (unless the new key and 
		///   value both compare equal to the old key and value).
		/// </remarks>
		internal int DoSingleOperation(ref AListSingleOperation<K, T> op)
		{
			AutoThrow();
			int sizeChange;
			try {
				_freezeMode = FreezeMode.FrozenForConcurrency;
				if (_root == null) {
					if (op.Mode == AListOperation.Remove || op.Mode == AListOperation.ReplaceIfPresent)
						return 0; // avoid creating unnecessary root 
					AutoCreateOrCloneRoot();
				} else if (_root.IsFrozen)
					AutoCreateOrCloneRoot();

				AListNode<K, T> splitLeft, splitRight;
				Debug.Assert(op.CompareKeys != null && op.CompareToKey != null);
				Debug.Assert(op.BaseIndex == 0 && !op.Found && op.AggregateChanged == 0);
				op.List = this;

				sizeChange = _root.DoSingleOperation(ref op, out splitLeft, out splitRight);
				
				if (splitLeft != null)
				{
					if (op.Mode == AListOperation.Remove) {
						Debug.Assert(splitRight == null);
						HandleChangedOrUndersizedRoot(splitLeft);
					} else {
						AutoSplit(splitLeft, splitRight);
					}
				}

				++_version;
				_count += (uint)sizeChange;
				CheckPoint();
			} finally {
				_freezeMode = FreezeMode.NotFrozen;
			}
			return sizeChange;
		}

		/// <summary>Performs a retrieve operation for a specific item.</summary>
		/// <param name="op">Describes the item to retrieve and receives information
		/// about the item retrieved. The following members must be initialized: 
		/// CompareKeys, CompareToKey, and Key. Also, if desired, set 
		/// <see cref="AListSingleOperation{K,T}.RequireExactMatch"/>.
		/// <para/>
		/// When this method returns, op.Found indicates whether the requested key
		/// was found, op.Item will contain the item with that key (if found), and 
		/// op.BaseIndex will contain the index of the item (see 
		/// <see cref="AListSingleOperation{K,T}.BaseIndex"/>).
		/// </param>
		internal void OrganizedRetrieve(ref AListSingleOperation<K, T> op)
		{
			Debug.Assert(op.Mode == AListOperation.Retrieve);
			Debug.Assert(op.BaseIndex == 0 && !op.Found && op.AggregateChanged == 0);
			if (_root == null)
				return;
			if (_freezeMode == FreezeMode.FrozenForConcurrency)
				ThrowFrozen();
			op.List = this;
			AListNode<K, T> splitLeft, splitRight;			
			_root.DoSingleOperation(ref op, out splitLeft, out splitRight);
		}
		
		#endregion

		#region Remove, RemoveAt, RemoveRange

		public void RemoveAt(int index)
		{
			if ((uint)index >= (uint)Count)
				throw new IndexOutOfRangeException();

			RemoveInternal((uint)index, 1u);
		}

		public void RemoveRange(int index, int amount)
		{
			if (amount == 0)
				return;
			if ((uint)index > (uint)Count)
				throw new IndexOutOfRangeException();
			if (amount < 0 || (uint)(index + amount) > (uint)Count)
				throw new ArgumentOutOfRangeException("amount");

			RemoveInternal((uint)index, (uint)amount);
		}

		private void RemoveInternal(uint index, uint amount)
		{
			AutoThrow();
			if (_listChanging != null)
				CallListChanging(new ListChangeInfo<T>(this, NotifyCollectionChangedAction.Remove, (int)index, -(int)amount, EmptyList<T>.Value));

			try
			{
				_freezeMode = FreezeMode.FrozenForConcurrency;

				if (_root.IsFrozen)
					AutoCreateOrCloneRoot();
				var result = _root.RemoveAt(index, amount, _observer);
				if (result)
					HandleChangedOrUndersizedRoot(_root);

				++_version;
				checked { _count -= amount; }
				CheckPoint();
			}
			finally
			{
				_freezeMode = FreezeMode.NotFrozen;
			}
		}
		
		/// <summary>Removes all the elements that match the conditions defined by the specified predicate.</summary>
		/// <param name="match">A lambda that defines the conditions on the elements to remove.</param>
		/// <returns>The number of elements removed from the list.</returns>
		public int RemoveAll(Predicate<T> match)
		{
			if (_freezeMode == FreezeMode.FrozenForConcurrency)
				ThrowFrozen();
			// This is not among the most efficient methods... but it'll do
			// TODO: Find a way to support this in an enumerator,
			// in order to optimize from O(N log N) to O(N)
			int numRemoved = 0;
			for (uint i = _count - 1; i != uint.MaxValue; i--) {
				if (match(_root[i])) {
					RemoveInternal(i, 1u);
					numRemoved++;
				}
			}
			return numRemoved;
		}

		#endregion

		#region Other standard methods: Clear, CopyTo, Count
		
		public virtual void Clear() { ClearInternal(false); }
		
		/// <summary>Clears the tree.</summary>
		/// <param name="forceClear">If true, the list is cleared even if 
		/// _listChanging throws an exception. If false, _listChanging can cancel 
		/// the operation by throwing an exception.</param>
		/// <remarks>If the list is frozen, it is not cleared even if forceClear=true.</remarks>
		protected virtual void ClearInternal(bool forceClear)
		{
			AutoThrow();
			if (_listChanging != null) {
				try {
					CallListChanging(new ListChangeInfo<T>(this, NotifyCollectionChangedAction.Remove, 0, -Count, EmptyList<T>.Value));
				} catch {
					if (forceClear)
						JustClear();
					throw;
				}
			}
			JustClear();
		}
		private void JustClear()
		{
			_freezeMode = FreezeMode.FrozenForConcurrency;
			try {
				_count = 0;
				_root = null;
				_treeHeight = 0;
				if (_observer != null)
					_observer.Clear(this);
			} finally {
				_version++;
				_freezeMode = FreezeMode.NotFrozen;
			}
		}

		/// <summary>
		/// Returns a sequence of integers that represent the locations where a given item appears in the list.
		/// </summary>
		public IEnumerable<int> IndexesOf(T item) { return IndexesOf(item, 0, (int)(_count-1)); }
		public virtual IEnumerable<int> IndexesOf(T item, int minIndex, int maxIndex)
		{
			var comp = EqualityComparer<T>.Default;
			IReadOnlyCollection<T> slice = this;
			if (minIndex > 0 || maxIndex < _count - 1)
				slice = Slice(minIndex, maxIndex - minIndex + 1);
			return slice.Select((current, index) => comp.Equals(item, current) ? index : -1)
				        .Where(index => index != -1);
		}

		/// <summary>Scans the list starting at startIndex and going upward, and 
		/// returns the index of an item that matches the first argument.</summary>
		/// <param name="item">Item to find</param>
		/// <param name="startIndex">Index of first element against which to compare the item.</param>
		/// <param name="comparer">Comparison object (e.g. <see cref="EqualityComparer{T}.Default"/>.)</param>
		/// <returns>Index of the matching item, or null if no matching item was found.</returns>
		/// <exception cref="ArgumentOutOfRangeException">when startIndex > Count</exception>
		public int? FirstIndexOf(T item, int startIndex, EqualityComparer<T> comparer = null)
		{
			comparer = comparer ?? EqualityComparer<T>.Default;
			bool ended = false;
			var it = GetEnumerator(startIndex);
			for (uint i = 0; i < _count; i++)
			{
				T current = it.MoveNext(ref ended);
				Debug.Assert(!ended);
				if (comparer.Equals(item, current))
					return (int)i;
			}
			return null;
		}
		[Obsolete("Please use FirstIndexOf instead, which returns null if item is not found")]
		public int LinearScanFor(T item, int startIndex, EqualityComparer<T> comparer = null) => FirstIndexOf(item, startIndex, comparer) ?? -1;

		public void CopyTo(T[] array, int arrayIndex)
		{
			LCInterfaces.CopyTo(this, array, arrayIndex);
		}

		#endregion

		#region GetEnumerator methods

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		IEnumerator<T> IEnumerable<T>.GetEnumerator() { return GetEnumerator(); }

		public Enumerator GetEnumerator() { return NewEnumerator(unchecked((uint)(-1)), 0, _count); }
		public Enumerator GetEnumerator(int startIndex) { return GetEnumerator(startIndex, int.MaxValue, false); }
		public Enumerator GetEnumerator(int start, int subcount) { return GetEnumerator(start, subcount, false); }
		public Enumerator GetEnumerator(int start, int subcount, bool startAtEnd)
		{
			CheckParam.IsNotNegative("start", start);
			CheckParam.IsNotNegative("subcount", subcount);
			return GetEnumerator((uint)start, (uint)subcount, startAtEnd);
		}
		protected internal Enumerator GetEnumerator(uint start, uint subcount, bool startAtEnd)
		{
			if (start > _count)
				start = _count;
			if (subcount > _count - start)
				subcount = _count - start;
			uint stop = start + subcount;

			if (startAtEnd)
				return NewEnumerator(stop, start, stop);
			else
				return NewEnumerator(start - 1, start, stop);
		}

		public class Enumerator : IBinumerator<T>
		{
			protected readonly AListBase<K, T> _self;
			protected Pair<AListInnerBase<K, T>, int>[] _stack;
			protected internal AListNode<K, T> _leaf;
			protected internal int _leafIndex;
			protected T _current;
			protected ushort _expectedVersion;

			protected internal uint _currentIndex;
			public readonly uint FirstIndex, LastIndex;
			public readonly uint StartIndex;

			/// <summary>
			/// Index of the last item that was enumerated. If has been enumerated 
			/// yet, this will typically be one beyond the range of indexes with 
			/// which this enumerator was initialized, e.g. -1 when enumerating 
			/// the entire list from the beginning.
			/// </summary>
			public int CurrentIndex { get { return (int)_currentIndex; } }

			/// <summary>
			/// Initializes an AList enumerator.
			/// </summary>
			/// <param name="self">AList to be enumerated.</param>
			/// <param name="start">Value of CurrentIndex after initialization; 
			/// should be firstIndex-1 if you want to enumerate forward, or 
			/// lastIndex if you want to enumerate backward.</param>
			/// <param name="firstIndex">Minimum index to enumerate. When enumerating
			/// backward, enumeration will stop after this index.</param>
			/// <param name="lastIndex">Maximum index to enumerate plus one. When 
			/// enumerating forward, enumeration will stop at this index (without 
			/// yielding the value there, if any).</param>
			/// <remarks>
			/// The Current property is never initialized by the constructor. You
			/// must call MoveNext() or MovePrevious() to initialize it.
			/// </remarks>
			protected internal Enumerator(AListBase<K, T> self, uint start, uint firstIndex, uint lastIndex)
			//public Enumerator(AListBase<K, T> self, uint start, uint subcount)
			{
				// The Enumerator state becomes invalid if MoveNext() enumerates past 
				// the end of the tree, in which case InvalidStateException is thrown.
				// Limit the index range so that that doesn't happen.
				start = Math.Min(start+1, self._count+1) - 1;
				lastIndex = Math.Min(lastIndex, self._count);

				_self = self;
				StartIndex = start;
				FirstIndex = firstIndex;
				LastIndex = lastIndex;
				_expectedVersion = self._version;

				PrepareToStart();
			}

			public void PrepareToStart()
			{
				var self = _self;
				uint start = StartIndex;

				if (self._treeHeight > 1)
				{
					_stack = new Pair<AListInnerBase<K, T>, int>[self._treeHeight - 1];
					var node = self._root as AListInnerBase<K, T>;
					int sub_i = 0;
					for (int i = 0; i < _stack.Length; i++)
					{
						if (node == null) throw new InvalidStateException();
						sub_i = 0;
						if (start != uint.MaxValue)
						{
							sub_i = node.BinarySearchI(start);
							start -= node.ChildIndexOffset(sub_i);
						}
						_stack[i] = Pair.Create(node, sub_i);
						node = node.Child(sub_i) as AListInnerBase<K, T>;
					}

					_leaf = _stack[_stack.Length - 1].Item1.Child(sub_i);
					if (!_leaf.IsLeaf || node != null)
						throw new InvalidStateException();
				}
				else
				{
					_leaf = self._root;
					if (self._count != 0 && !_leaf.IsLeaf)
						throw new InvalidStateException();
				}

				Debug.Assert(_leaf == null ? (int)start == -1 : (int)start <= _leaf.TotalCount);
				_currentIndex = StartIndex;
				_leafIndex = (int)start;
			}

			public Enumerator(Enumerator copy)
			{
				_self = copy._self;
				if (copy._stack != null)
					_stack = InternalList.CopyToNewArray(copy._stack);
				_leaf = copy._leaf;
				_leafIndex = copy._leafIndex;
				_current = copy._current;
				_expectedVersion = copy._expectedVersion;
				_currentIndex = copy._currentIndex;
				FirstIndex = copy.FirstIndex;
				LastIndex = copy.LastIndex;
				StartIndex = copy.StartIndex;
			}

			protected internal T MoveNext(ref bool ended)
			{
				// "if (_currentIndex < LastIndex - 1)" is wrong if (int)_currentIndex == -1
				if (_currentIndex + 1 < LastIndex)
				{
					if (++_leafIndex >= _leaf.TotalCount)
						MoveToNextNode();
					++_currentIndex;
					return _leaf[(uint)_leafIndex];
				}
				else
				{
					ended = true;
					return MoveNextAtEnd();
				}
			}
			private T MoveNextAtEnd()
			{
				if (_currentIndex + 1 == LastIndex)
				{
					// Advance one past the end
					_currentIndex++;
					_leafIndex++;
				}
				return default(T);
			}
			private void MoveToNextNode()
			{
				if (_expectedVersion != _self._version)
					throw new EnumerationException();
				if (_self._freezeMode == FreezeMode.FrozenForConcurrency)
					throw new ConcurrentModificationException();
				Debug.Assert(_currentIndex < LastIndex);

				var stack = _stack;
				int s;
				try {
					s = stack.Length - 1;
					while (++stack[s].Item2 >= stack[s].Item1.LocalCount)
						--s;
				}
				catch {
					throw new InvalidStateException();
				}
				while (++s < stack.Length) {
					var child = (AListInnerBase<K, T>)stack[s - 1].Item1.Child(stack[s - 1].Item2);
					stack[s] = Pair.Create(child, 0);
				}

				var tos = stack[stack.Length - 1];
				_leaf = tos.Item1.Child(tos.Item2);
				Debug.Assert(_leaf.IsLeaf);
				_leafIndex = 0;
				Debug.Assert(_leaf.LocalCount > 0);
			}

			protected internal T MovePrevious(ref bool ended)
			{
				// "if (_currentIndex > FirstIndex)" is wrong, in case (int)_currentIndex == -1
				if (_currentIndex + 1 > FirstIndex + 1)
				{
					if (--_leafIndex < 0)
						MoveToPreviousNode();
					--_currentIndex;
					return _leaf[(uint)_leafIndex];
				}
				else
				{
					ended = true;
					return MovePreviousAtEnd();
				}
			}
			private T MovePreviousAtEnd()
			{
				if (_currentIndex == FirstIndex)
				{
					// Advance one past the beginning
					_currentIndex--;
					_leafIndex--;
				}
				return default(T);
			}
			private void MoveToPreviousNode()
			{
				if (_expectedVersion != _self._version)
					throw new EnumerationException();
				if (_self._freezeMode == FreezeMode.FrozenForConcurrency)
					throw new ConcurrentModificationException();

				var stack = _stack;
				int s;
				try
				{
					s = stack.Length - 1;
					while (--stack[s].Item2 < 0)
						--s;
				}
				catch
				{
					throw new InvalidStateException();
				}

				while (++s < stack.Length)
				{
					var child = (AListInnerBase<K, T>) stack[s - 1].Item1.Child(stack[s - 1].Item2);
					stack[s] = Pair.Create(child, child.LocalCount - 1);
				}

				var tos = stack[stack.Length - 1];
				_leaf = tos.Item1.Child(tos.Item2);
				Debug.Assert(_leaf.IsLeaf);
				_leafIndex = (int) _leaf.TotalCount - 1;
				Debug.Assert(_leaf.TotalCount > 0);
			}

			#region IEnumerator<T> (bonus: includes a setter for Current)

			public bool MoveNext()
			{
				bool ended = false;
				_current = MoveNext(ref ended);
				return !ended;
			}
			public bool MovePrev()
			{
				bool ended = false;
				_current = MovePrevious(ref ended);
				return !ended;
			}
			object System.Collections.IEnumerator.Current
			{
				get { return _current; }
			}
			public void Reset()
			{
				if (_expectedVersion != _self._version)
					throw new EnumerationException();
				if (_self._freezeMode == FreezeMode.FrozenForConcurrency)
					throw new ConcurrentModificationException();

				PrepareToStart();
			}
			public void Dispose() { }

			public T Current
			{
				get { return _current; }
				set {
					if (_leafIndex >= _leaf.TotalCount)
						throw new InvalidOperationException();
					if (_expectedVersion != _self._version)
						throw new EnumerationException();
					
					LLSetCurrent(value);
				}
			}
			protected internal virtual void LLSetCurrent(T value)
			{
				_current = value;
				if (_leaf.IsFrozen)
					UnfreezeCurrentLeaf();
				_leaf.SetAt((uint)_leafIndex, value, _self._observer);
			}

			protected internal void UnfreezeCurrentLeaf()
			{
				Debug.Assert(_leaf.IsFrozen);
				var clone = _leaf.DetachedClone();

				// In the face of cloning, all enumerators except this one must 
				// now be considered invalid.
				++_self._version;
				++_expectedVersion;

				// This lazy node cloning feature is a pain in the butt
				_leaf = clone;
				var stack = _stack;
				var idx = _self._observer;
				if (stack == null) {
					Debug.Assert(_self._treeHeight == 1 && _self._root == _leaf);
					if (idx != null) idx.HandleNodeReplaced(_self._root, _leaf, null);
					_self._root = _leaf;
				} else {
					AListInnerBase<K, T> clone2 = null;
					for (int s = stack.Length - 1; ; s--)
					{
						clone = clone2 = stack[s].Item1.HandleChildCloned(stack[s].Item2, clone, idx);
						if (clone == null)
							break;
						stack[s].Item1 = clone2;
						if (s == 0) {
							if (idx != null) idx.HandleNodeReplaced(_self._root, clone2, null);
							_self._root = clone2;
							break;
						}
					}
				}
			}
			
			#endregion
		}

		#endregion
	
		#region Indexer (this[int]), TryGet
		
		public T this[int index]
		{
			get {
				// The benchmark says the cost of this check is negligible
				if (_freezeMode == FreezeMode.FrozenForConcurrency)
					ThrowFrozen();
				if ((uint)index >= (uint)Count)
					ThrowIndexOutOfRange();
				return _root[(uint)index];
			}
		}

		protected void ThrowIndexOutOfRange() => throw new ArgumentOutOfRangeException("index");

		public T TryGet(int index, out bool fail)
		{
			if (_freezeMode == FreezeMode.FrozenForConcurrency)
				ThrowFrozen();
			fail = false;
			if ((uint)index < (uint)_count)
				return _root[(uint)index];
			fail = true;
			return default(T);
		}

		#endregion

		#region Bonus features: Freeze, Clone, RemoveSectionHelper, CopySectionHelper, SwapHelper, Slice, CountSizeInBytes

		/// <summary>Prevents further changes to the list.</summary>
		/// <remarks>
		/// After calling this method, any attempt to modify the list will cause
		/// a <see cref="ReadOnlyException"/> to be thrown.
		/// <para/>
		/// Note: although they will no longer be called, any ListChanging handlers
		/// will not be forgotten, because it is possible to clone an unfrozen
		/// version of the list while keeping those handlers.
		/// <para/>
		/// This feature can be disabled in a derived class by overriding this
		/// method to throw <see cref="NotSupportedException"/>.
		/// </remarks>
		public virtual void Freeze()
		{
			if (_freezeMode > FreezeMode.Frozen)
				ThrowFrozen();
			_freezeMode = FreezeMode.Frozen;
		}

		/// <summary>Together with the <see cref="AListBase{K,T}.AListBase(AListBase{K,T},AListNode{K,T})"/>
		/// constructor, this method helps implement the CopySection() method in derived 
		/// classes, by cloning a section of the tree.</summary>
		protected AListNode<K, T> CopySectionHelper(int start, int subcount)
		{
			if (subcount < 0)
				throw new ArgumentOutOfRangeException("subcount");
			return CopySectionHelper((uint)start, (uint)subcount);
		}
		protected AListNode<K, T> CopySectionHelper(uint start, uint subcount)
		{
			if (start > _count)
				throw new ArgumentOutOfRangeException("start");
			if (subcount > _count - start)
				subcount = _count - start;
			if (subcount == 0)
				return null;

			var section = _root.CopySection(start, subcount, this);
			return section;
		}

		/// <summary>Swaps two ALists.</summary>
		/// <remarks>
		/// Usually, swapping is a useless feature, since usually you can just 
		/// swap the references to two lists instead of the contents of two lists.
		/// This method is provided anyway because <see cref="AList{T}.Append"/> 
		/// and <see cref="AList{T}.Prepend"/> need to be able to swap in-place in 
		/// some cases.
		/// <para/>
		/// The derived class must manually swap any additional data members that 
		/// it defines.
		/// </remarks>
		protected void SwapHelper(AListBase<K, T> other, bool swapObservers)
		{
			AutoThrow();
			other.AutoThrow();

			Debug.Assert(_freezeMode == 0 && other._freezeMode == 0);

			_freezeMode = other._freezeMode = FreezeMode.FrozenForConcurrency;
			try {
				if (swapObservers) {
					G.Swap(ref _listChanging, ref other._listChanging);
					G.Swap(ref _observer, ref other._observer);
				}
				G.Swap(ref _root, ref other._root);
				G.Swap(ref _count, ref other._count);
				G.Swap(ref _maxLeafSize, ref other._maxLeafSize);
				G.Swap(ref _treeHeight, ref other._treeHeight);
				G.Swap(ref _version, ref other._version);
			}
			finally
			{
				_freezeMode = other._freezeMode = FreezeMode.NotFrozen;
			}
		}

		IRange<T> IListSource<T>.Slice(int start, int count) { return Slice(start, count); }
		public Slice_<T> Slice(int start, int length)
		{
			return new Slice_<T>(this, start, length);
		}

		/// <summary>Diagnostic method. Returns the number of elements of the list
		/// that are located in immutable nodes, which will be copied if modified.
		/// Used by the test suite.</summary>
		/// <remarks>Variable time required. Scans all nodes if none are immutable; 
		/// stops at the root if the root is immutable.</remarks>
		public int GetImmutableCount()
		{
			return _root == null ? 0 : (int)_root.GetImmutableCount(false);
		}

		protected int BaseClassSizeInBytes => IntPtr.Size * 5 + (IntPtr.Size <= 4 ? 12 : 16);

		/// <summary>Tallies the memory use, in bytes, of the entire list including
		/// this object, the root node, all child nodes, and all arrays.</summary>
		/// <param name="sizeOfElement">The size in bytes of a slot of type T</param>
		/// <param name="sizeOfKey">The size in bytes of a key slot. This parameter
		/// only has an effect in <see cref="BDictionary{K, V}"/>. In sorted types,
		/// T is the combined size of the key and value, while K is the size of the
		/// key by itself. But in <see cref="BMultiMap{K, V}"/>, <see cref="BList{T}"/>,
		/// the key type is T, so this parameter is ignored. Notably, in BMultiMap,
		/// a key-value <i>pair</i> is considered to be the key type.</param>
		/// <returns>Number of bytes of heap used.</returns>
		/// <remarks>
		/// If T (or K) is a class, the element (or key) size that you pass in should
		/// be IntPtr.Size. Do not include memory used by the class objects themselves,
		/// because unused slots are counted in the total.
		/// </remarks>
		public virtual long CountSizeInBytes(int sizeOfElement, int sizeOfKey = 8) =>
			BaseClassSizeInBytes + (_root?.CountSizeInBytes(sizeOfElement, sizeOfKey) ?? 0);

		#endregion

		#region Observer management: AddObserver, RemoveObserver, ObserverCount

		/// <summary>Attaches a tree observer to this object.</summary>
		/// <returns>True if the observer was added, false if it was already attached.</returns>
		/// <remarks>
		/// The tree observer mechanism is much more advanced, and less efficient, 
		/// than the <see cref="ListChanging"/> event. You should use that event
		/// instead if you can accomplish what you need with it.
		/// <para/>
		/// Observers cannot be added when the list is frozen, although they can
		/// be removed.
		/// <para/>
		/// This feature can be disabled in a derived class by overriding this
		/// method to throw <see cref="NotSupportedException"/>.
		/// </remarks>
		public virtual bool AddObserver(IAListTreeObserver<K, T> observer)
		{
			AutoThrow();
			_freezeMode = FreezeMode.FrozenForConcurrency;
			try {
				if (_observer == null) {
					observer.DoAttach(this, _root);
					_observer = observer;
					return true;
				} else if (_observer is ObserverMgr)
					return (_observer as ObserverMgr).AddObserver(observer);
				else {
					var mgr = new ObserverMgr(this, _root, _observer);
					if (mgr.AddObserver(observer)) {
						_observer = mgr;
						return true;
					}
					return false;
				}
			} finally {
				_freezeMode = FreezeMode.NotFrozen;
			}
		}

		/// <summary>Removes a previously attached tree observer from this list.</summary>
		/// <returns>True if the observer was removed, false if it was not attached.</returns>
		public virtual bool RemoveObserver(IAListTreeObserver<K, T> observer)
		{
			if (_freezeMode == FreezeMode.FrozenForConcurrency)
				ThrowFrozen();
			
			if (_observer == observer)
			{
				observer.Detach(this, _root);
				_observer = null;
				return true;
			} 
			var mgr = _observer as ObserverMgr;
			if (mgr != null && mgr.RemoveObserver(observer))
			{
				if (mgr.ObserverCount == 0)
					_observer = null;
				return true;
			}
			
			return false;
		}

		/// <summary>Returns the number of tree observers attached to this list.</summary>
		public int ObserverCount
		{
			get { 
				if (_observer == null)
					return 0;
				if (_observer is ObserverMgr)
					return (_observer as ObserverMgr).ObserverCount;
				else
					return 1;
			}
		}

		#endregion
	}

	/// <summary>A reverse view of an AList.</summary>
	public struct AListReverseView<K, T> : IListSource<T>
	{
		AListBase<K, T> _list;
		public AListReverseView(AListBase<K, T> list) { _list = list; }

		public T this[int index]
		{
			get { return _list[_list.Count - 1 - index]; }
		}
		public T TryGet(int index, out bool fail)
		{
			return _list.TryGet(_list.Count - 1 - index, out fail);
		}
		public ReverseBinumerator<T> GetEnumerator()
		{
			return new ReverseBinumerator<T>(_list.GetEnumerator(0, _list.Count, true));
		}
		IEnumerator<T> IEnumerable<T>.GetEnumerator() { return GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		public int Count
		{
			get { return _list.Count; }
		}

		IRange<T> IListSource<T>.Slice(int start, int count) { return Slice(start, count); }
		public Slice_<T> Slice(int start, int count) { return new Slice_<T>(this, start, count); }
	}
}
