namespace Loyc.Collections
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Collections.Specialized;
	using System.Diagnostics;
	using Loyc.Collections.Impl;
	using Loyc.Collections.Linq;
	using Loyc.Essentials;
	using Loyc.Math;
	
	/// <summary>
	/// An all-purpose list structure with the following additional features beyond 
	/// what's offered by <see cref="List{T}"/>: fast insertion and deletion 
	/// (O(log N)), batch insertion and deletion, observability, fast cloning, 
	/// freezability, and fast splitting and joining of large collections.
	/// </summary>
	/// <typeparam name="T">Type of each element in the list</typeparam>
	/// <remarks>
	/// TODO: AList is not quite finished. BList/BTree is not even started.
	/// <para/>
	/// AList and BList are excellent data structures to choose if you aren't sure 
	/// what your requirements are. The main difference between them is that BList
	/// is sorted and AList is not. <see cref="DList{T}"/>, meanwhile, is a simpler 
	/// data structure with a faster indexer and lower memory requirements. In fact,
	/// <see cref="DListInternal{T}"/> is the building block of AList.
	/// <para/>
	/// Structurally, AList (and BList) are very similar to B+trees. They use
	/// memory almost as efficiently as arrays, and offer O(log N) insertion and
	/// deletion in exchange for a O(log N) indexer, which is slower than the 
	/// indexer of <see cref="List{T}"/>. They use slightly more memory than <see 
	/// cref="List{T}"/> for all list sizes; and most notably, for large lists 
	/// there is an extra overhead of about 30% (TODO: calculate accurately).
	/// <para/>
	/// That said, you should use an AList whenever you know that the list might be 
	/// large and need insertions or deletions somewhere in the middle. If you 
	/// expect to do insertions and deletions at random locations, but only 
	/// occasionally, <see cref="DList{T}"/> is sometimes a better choice because 
	/// it has a faster indexer. Both classes provide fast enumeration (O(1) per 
	/// element), but <see cref="DList{T}"/> enumerators initialize faster.
	/// <para/>
	/// In addition, you can subscribe to the <see cref="ListChanging"/> event to
	/// find out when the list changes. AList's observability is more lightweight 
	/// than that of <see cref="ObservableCollection{T}"/>.
	/// <para/>
	/// Although single insertions, deletions, and random access require O(log N)
	/// time, you can get better performance using any overload of 
	/// <see cref="InsertRange"/>, <see cref="RemoveRange"/>, <see 
	/// cref="GetIterator"/> or <see cref="Resize"/>. These methods require only
	/// O(log N + M) time, where M is the number of elements you are inserting,
	/// removing or enumerating.
	/// <para/>
	/// AList is an excellent choice if you need to make occasional snapshots of
	/// the tree. Cloning is fast and memory-efficient, because only the root 
	/// node is cloned. All other nodes are duplicated on-demand as changes are 
	/// made. Thus, AList can be used as a so-called "persistent" data structure, 
	/// but it is relatively expensive to clone the tree after every modification. 
	/// When modifying a tree that was just cloned (remember, AList is really a 
	/// tree), the leaf node being changed and all of its ancestors must be 
	/// duplicated. Therefore, it's better if you can arrange to have a high ratio 
	/// of changes to clones.
	/// <para/>
	/// AList is also freezable, which is useful if you need to construct a list 
	/// in a read-only or freezable data structure. You could also freeze the list
	/// in order to return a read-only copy of it, which, compared to cloning, has 
	/// the advantage that no memory allocation is required at the time you return 
	/// the list. If you need to edit the list later, you can clone the list (the 
	/// clone can be modified).
	/// <para/>
	/// In general, AList is NOT multithread-safe; concurrency support is the only 
	/// major feature that AList lacks. AList{T} can support multiple readers 
	/// concurrently, as long as the collection is not modified. An instance of 
	/// AList{T} must not be accessed from other threads during any modification. 
	/// AList{T} has a mechanism to detect illegal concurrent access and throw 
	/// InvalidOperationException if it is detected, but this is not designed to 
	/// be reliable; its main purpose is to help you find bugs. If concurrent 
	/// modification is not detected, the AList will probably become corrupted and 
	/// produce strange exceptions, or fail an assertion.
	/// </remarks>
	/// <seealso cref="BList{T}"/>
	/// <seealso cref="BTree{T}"/>
	/// <seealso cref="DList{T}"/>
	[Serializable]
	//[DebuggerTypeProxy(typeof(ListSourceDebugView<>)), DebuggerDisplay("Count = {Count}")]
	public class AList<T> : IListEx<T>, IListRangeMethods<T>, IGetIteratorSlice<T>, ICloneable<AList<T>>
	{
		#region Data members
		
		public event Action<object, ListChangeInfo<T>> ListChanging;
		protected AListNode<T> _root;
		protected uint _count;
		protected byte _maxNodeSize;
		protected byte _version;
		private byte _treeHeight;
		private byte _freezeMode = NotFrozen;
		private const byte NotFrozen = 0;
		private const byte Frozen = 1;
		private const byte FrozenForListChanging = 2;
		private const byte FrozenForConcurrency = 3;

		#endregion

		#region Constructors
		
		public AList(AList<T> items)
		{
			if (items._freezeMode == FrozenForConcurrency)
				items.AutoThrow(); // cannot clone concurrently!
			if ((_root = items._root) != null)
				_root.Freeze();
			_count = items._count;
			_maxNodeSize = items._maxNodeSize;
			_treeHeight = items._treeHeight;
			// Leave _freezeMode at NotFrozen and _version at 0
		}
		public AList() : this(48) { }
		public AList(IEnumerable<T> items) { InsertRange(0, items); }
		public AList(IListSource<T> items) { InsertRange(0, items); }
		public AList(int maxNodeSize)
		{
			_maxNodeSize = (byte)Math.Min(maxNodeSize, 0xFF);
		}

		private AList(AListNode<T> root, byte maxNodeSize, byte treeHeight)
		{
			if (root != null) {
				_count = root.TotalCount;
				HandleClonedOrUndersizedRoot(root);
			}
			_maxNodeSize = maxNodeSize;
			_treeHeight = treeHeight;
		}
		
		#endregion

		#region General supporting methods

		protected virtual AListLeaf<T> CreateRoot()
		{
			return new AListLeaf<T>(_maxNodeSize);
		}
		protected virtual AListInner<T> SplitRoot(AListNode<T> left, AListNode<T> right)
		{
			return new AListInner<T>(left, right, AListInner<T>.DefaultMaxNodeSize);
		}
		protected void AutoThrow()
		{
			if (_freezeMode != NotFrozen) {
				if (_freezeMode == FrozenForListChanging)
					throw new InvalidOperationException("Cannot insert or remove items in AList during a ListChanging event.");
				else if (_freezeMode == FrozenForConcurrency)
					throw new ConcurrentModificationException("AList was accessed concurrently while being modified.");
				else {
					Debug.Assert(_freezeMode == Frozen);
					throw new ReadOnlyException("Cannot modify AList that is frozen.");
				}
			}
		}
		protected void CallListChanging(ListChangeInfo<T> listChangeInfo)
		{
			Debug.Assert(_freezeMode == NotFrozen);
			if (ListChanging != null)
			{
				// Freeze the list during ListChanging
				_freezeMode = FrozenForListChanging;
				try {
					ListChanging(this, listChangeInfo);
				} finally {
					_freezeMode = NotFrozen;
				}
			}
		}
		protected void AutoCreateRoot()
		{
			if (_root == null) {
				Debug.Assert(_count == 0);
				_root = CreateRoot();
				_treeHeight = 1;
			}
		}
		protected void AutoSplit(AListNode<T> splitLeft, AListNode<T> splitRight)
		{
			if (splitLeft != null) {
				if (splitRight == null)
					_root = splitLeft;
				else {
					_root = SplitRoot(splitLeft, splitRight);
					_treeHeight++;
				}
			}
		}
		protected void HandleClonedOrUndersizedRoot(AListNode<T> result)
		{
			_root = result;
			while (_root.LocalCount <= 1)
			{
				if (_root.LocalCount == 0) {
					_root = null;
					_treeHeight = 0;
				} else if (_root is AListInner<T>) {
					_root = ((AListInner<T>)_root).Child(0);
					checked { _treeHeight--; }
					continue;
				}
				return;
			}
		}

		#endregion

		#region Insert, InsertRange

		public void Insert(int index, T item)
		{
			if ((uint)index > (uint)_count)
				throw new IndexOutOfRangeException();
			AutoThrow();
			if (ListChanging != null)
				CallListChanging(new ListChangeInfo<T>(NotifyCollectionChangedAction.Add, index, 1, Iterable.Single(item)));

			try {
				_freezeMode = FrozenForConcurrency;
				AutoCreateRoot();
				
				AListNode<T> splitLeft, splitRight;
				splitLeft = _root.Insert((uint)index, item, out splitRight);
				if (splitLeft != null) // redundant 'if' optimization
					AutoSplit(splitLeft, splitRight);

				++_version;
				checked { ++_count; }
				Debug.Assert(_count == _root.TotalCount);
			} finally {
				_freezeMode = NotFrozen;
			}
		}

		public void InsertRange(int index, IEnumerable<T> list)
		{
			if ((uint)index > (uint)_count)
				throw new IndexOutOfRangeException();

			var source = list as IListSource<T>;
			if (source == null)
				source = new InternalList<T>(list.AsIterable().GetIterator());

			InsertRange(index, source);
		}
		public void InsertRange(int index, IListSource<T> source)
		{
			int sourceIndex = 0;
			BeginInsertRange(index, source);
			try {
				while (sourceIndex < source.Count)
				{
					AListNode<T> splitLeft, splitRight;
					splitLeft = _root.Insert((uint)index, source, ref sourceIndex, out splitRight);
					AutoSplit(splitLeft, splitRight);
				}
			} finally {
				DoneInsertRange(sourceIndex);
			}
		}
		// Helper method that is also used by Append() and Prepend()
		private void BeginInsertRange(int index, IListSource<T> items)
		{
			if ((uint)index > (uint)_count)
				throw new IndexOutOfRangeException();
			
			AutoThrow();
			if (ListChanging != null)
				CallListChanging(new ListChangeInfo<T>(NotifyCollectionChangedAction.Add, index, items.Count, items));

			_freezeMode = FrozenForConcurrency;
			AutoCreateRoot();
		}
		// Helper method that is also used by Append() and Prepend()
		private void DoneInsertRange(int amountInserted)
		{
			if (amountInserted != 0)
				++_version;
			_freezeMode = NotFrozen;
			checked { _count += (uint)amountInserted; };
			Debug.Assert(_count == _root.TotalCount);
		}

		public void InsertRange(int index, AList<T> source)
		{
			if (source._root is AListLeaf<T> || source._maxNodeSize != _maxNodeSize)
				InsertRange(index, (IListSource<T>)source);
			else {
				AList<T> rightSection = null;
				int rightSize;
				if ((rightSize = Count - index) != 0)
					rightSection = RemoveSection(index, rightSize);
				Append(source);
				if (rightSection != null)
					Append(rightSection);
			}
		}

		#endregion

		#region Add, AddRange, Resize

		public void Add(T item)
		{
			Insert(Count, item);
		}
		public void AddRange(IEnumerable<T> list)
		{
			InsertRange(Count, list);
		}
		public void AddRange(IListSource<T> source)
		{
			InsertRange(Count, source);
		}
		public void AddRange(AList<T> source)
		{
			InsertRange(Count, source);
		}

		public void Resize(int newSize)
		{
			if (newSize < Count)
				RemoveRange(newSize, Count - newSize);
			else if (newSize > Count)
				InsertRange(Count, Iterable.Repeat(default(T), newSize - Count));
		}

		#endregion

		#region Remove, RemoveAt, RemoveRange

		public bool Remove(T item)
		{
			int index = IndexOf(item);
			if (index <= -1)
				return false;
			RemoveAt(index);
			return true;
		}

		public void RemoveAt(int index)
		{
			if ((uint)index >= (uint)Count)
				throw new IndexOutOfRangeException();
			
			RemoveInternal(index, 1);
		}

		public void RemoveRange(int index, int amount)
		{
			if (amount == 0)
				return;
			if ((uint)index > (uint)Count)
				throw new IndexOutOfRangeException();
			if (amount <= 0 || (uint)(index + amount) > (uint)Count)
				throw new ArgumentOutOfRangeException("amount");
			
			RemoveInternal(index, amount);
		}

		private void RemoveInternal(int index, int amount)
		{
			AutoThrow();
			if (ListChanging != null)
				CallListChanging(new ListChangeInfo<T>(NotifyCollectionChangedAction.Remove, index, -amount, null));

			try {
				_freezeMode = FrozenForConcurrency;

				var result = _root.RemoveAt((uint)index, (uint)amount);
				if (result != null)
					HandleClonedOrUndersizedRoot(result);

				++_version;
				checked { _count -= (uint)amount; }
			} finally {
				_freezeMode = NotFrozen;
				Debug.Assert(_count == (_root == null ? 0 : _root.TotalCount));
			}
		}

		#endregion

		#region Other standard methods: Clear, IndexOf, Contains, CopyTo, Count, IsReadOnly
		
		public virtual void Clear()
		{
			AutoThrow();
			if (ListChanging != null)
				CallListChanging(new ListChangeInfo<T>(NotifyCollectionChangedAction.Remove, 0, -Count, null));
			
			_freezeMode = FrozenForConcurrency;
			try {
				_count = 0;
				_root = null;
				_treeHeight = 0;
			} finally {
				_version++;
				_freezeMode = NotFrozen;
			}
		}

		public int IndexOf(T item)
		{
			return IndexOf(item, EqualityComparer<T>.Default);
		}
		public int IndexOf(T item, EqualityComparer<T> comparer)
		{
			bool ended = false;
			var it = GetIterator();
			for (uint i = 0; i < _count; i++)
			{
				T current = it(ref ended);
				Debug.Assert(!ended);
				if (comparer.Equals(item, current))
					return (int)i;
			}
			return -1;
		}
		
		public bool Contains(T item)
		{
			return IndexOf(item) > -1;
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			LCInterfaces.CopyTo(this, array, arrayIndex);
		}

		public int Count
		{
			get { return (int)_count; }
		}

		public bool IsReadOnly
		{
			get { return IsFrozen; }
		}

		#endregion

		#region GetEnumerator, GetIterator

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		public IEnumerator<T> GetEnumerator()
		{
			Debug.Assert((_root == null) == (_treeHeight == 0));
			if (_root == null)
				return EmptyEnumerator<T>.Value;

			return new Enumerator(this, 0, _count);
		}

		public Iterator<T> GetIterator()
		{
			return GetIterator(0, _count);
		}
		public Iterator<T> GetIterator(int startIndex)
		{
			return GetIterator((uint)startIndex, uint.MaxValue);
		}
		public Iterator<T> GetIterator(int start, int subcount)
		{
			if (subcount < 0)
				throw new ArgumentOutOfRangeException("subcount");

			return GetIterator((uint)start, (uint)subcount);
		}
		public Iterator<T> GetIterator(int startIndex, int subcount, bool countDown)
		{
			if (!countDown)
				return GetIterator(startIndex, subcount);

			throw new NotImplementedException(); // TODO
		}
		protected Iterator<T> GetIterator(uint start, uint subcount)
		{
			if (start >= _count)
			{
				if (start == _count)
					return EmptyIterator<T>.Value;
				throw new ArgumentOutOfRangeException("start");
			}
			Debug.Assert(_root != null);
			if (subcount == 0)
				return EmptyIterator<T>.Value;

			if (_treeHeight == 1)
			{
				if (!(_root is AListLeaf<T>)) throw new InvalidStateException();
				return ((AListLeaf<T>)_root).GetIterator((int)start, (int)subcount);
			}

			return new Enumerator(this, start, subcount).MoveNext;
		}

		protected class Enumerator : IEnumerator<T>
		{
			protected readonly AList<T> _self;
			protected Pair<AListInner<T>, int>[] _stack;
			protected internal AListLeaf<T> _leaf;
			protected internal int _leafIndex;
			protected byte _expectedVersion;
			protected T _current;
			public readonly uint StartIndex;
			protected internal uint _countLeft;

			public Enumerator(AList<T> self, uint start, uint subcount)
			{
				_self = self;
				StartIndex = start;
				Debug.Assert(_self._root != null); // null root not supported

				if (self._treeHeight > 1)
				{
					_stack = new Pair<AListInner<T>, int>[self._treeHeight - 1];
					var node = self._root as AListInner<T>;
					int sub_i = 0;
					for (int i = 0; i < _stack.Length; i++)
					{
						if (node == null) throw new InvalidStateException();
						sub_i = 0;
						if (start != 0)
						{
							sub_i = node.BinarySearch(start);
							start -= node.ChildIndexOffset(sub_i);
						}
						_stack[i] = Pair.Create(node, sub_i);
						node = node.Child(sub_i) as AListInner<T>;
					}
					if (node != null) throw new InvalidStateException();

					_leaf = (AListLeaf<T>)_stack[_stack.Length - 1].Item1.Child(sub_i);
				} else
					_leaf = (AListLeaf<T>)self._root;

				Debug.Assert(start < _leaf.LocalCount);
				_leafIndex = (int)start - 1;
				_expectedVersion = self._version;

				this._countLeft = subcount;
			}
			
			public Enumerator(Enumerator copy)
			{
				_self = copy._self;
				if (copy._stack != null)
					_stack = InternalList.CopyToNewArray(copy._stack);
				_leaf = copy._leaf;
				_expectedVersion = copy._expectedVersion;
				_countLeft = copy._countLeft;
				_leafIndex = copy._leafIndex;
				_current = copy._current;
				StartIndex = copy.StartIndex;
			}

			protected internal T MoveNext(ref bool ended)
			{
				if (_countLeft == 0)
					goto end;
				--_countLeft;

				if (++_leafIndex >= _leaf.LocalCount)
				{
					if (_expectedVersion != _self._version)
						throw new EnumerationException();
					if (_self._freezeMode == FrozenForConcurrency)
						throw new ConcurrentModificationException();

					var stack = _stack;
					if (stack == null)
						goto end;
					else {
						int s = stack.Length - 1;
						while (++stack[s].Item2 >= stack[s].Item1.LocalCount)
						{
							if (--s < 0)
								goto end;
						}
						while (++s < stack.Length)
							stack[s] = Pair.Create((AListInner<T>)stack[s - 1].Item1.Child(stack[s - 1].Item2), 0);
						_leaf = (AListLeaf<T>)stack[stack.Length - 1].Item1.Child(stack[stack.Length - 1].Item2);
						_leafIndex = 0;
					}
				}
				return _leaf[(uint)_leafIndex];

			end:
				_stack = null;
				ended = true;
				return default(T);
			}

			#region IEnumerator<T> (bonus: includes a setter for Current)

			public bool MoveNext() { bool ended = false; _current = MoveNext(ref ended); return !ended; }
			object System.Collections.IEnumerator.Current { get { return _current; } }
			void System.Collections.IEnumerator.Reset() { throw new NotImplementedException(); }
			public void Dispose() { }

			public T Current
			{
				get { return _current; }
				set {
					if (_leafIndex >= _leaf.LocalCount)
						throw new InvalidOperationException();
					if (_expectedVersion != _self._version)
						throw new EnumerationException();
					
					LLSetCurrent(value);
				}
			}
			internal void LLSetCurrent(T value)
			{
				_current = value;
				var clone = _leaf.SetAt((uint)_leafIndex, value);
				if (clone != null)
					HandleLeafCloned(clone);
			}

			protected internal void HandleLeafCloned(AListNode<T> clone)
			{
				// In the face of cloning, all enumerators except this one must 
				// now be considered invalid.
				++_self._version;
				++_expectedVersion;

				// This lazy node cloning feature is a pain in the butt
				_leaf = (AListLeaf<T>)clone;
				var stack = _stack;
				if (stack == null) {
					Debug.Assert(_self._treeHeight == 1 && _self._root == _leaf);
					_self._root = _leaf;
				} else {
					AListInner<T> clone2 = null;
					for (int s = stack.Length - 1; ; s--)
					{
						clone = clone2 = stack[s].Item1.HandleChildCloned(stack[s].Item2, clone);
						if (clone == null)
							break;
						stack[s].Item1 = clone2;
						if (s == 0) {
							_self._root = clone2;
							break;
						}
					}
				}
			}
			
			#endregion
		}

		#endregion

		#region Indexer (this[int]), TryGet, TrySet
		
		public T this[int index]
		{
			get {
				if (_freezeMode == FrozenForConcurrency)
					AutoThrow();
				if ((uint)index >= (uint)Count)
					throw new IndexOutOfRangeException();
				return _root[(uint)index];
			}
			set {
				if ((_freezeMode & 1) != 0) // Frozen or FrozenForConcurrency, but not FrozenForListChanging
					AutoThrow();
				if ((uint)index >= (uint)Count)
					throw new IndexOutOfRangeException();
				SetHelper((uint)index, value);
			}
		}

		private void SetHelper(uint index, T value)
		{
			if (ListChanging != null)
				CallListChanging(new ListChangeInfo<T>(NotifyCollectionChangedAction.Replace, (int)index, 0, Iterable.Single(value)));
			++_version;
			_root = _root.SetAt(index, value) ?? _root;
		}

		public bool TrySet(int index, T value)
		{
			if (_freezeMode != 0)
			{
				if (_freezeMode == FrozenForConcurrency)
					AutoThrow();
				if (_freezeMode == Frozen)
					return false;
			}
			if ((uint)index >= (uint)Count)
				return false;
			SetHelper((uint)index, value);
			return true;
		}
		public T TryGet(int index, ref bool fail)
		{
			if (_freezeMode == FrozenForConcurrency)
				AutoThrow();
			if ((uint)index < (uint)_count)
				return _root[(uint)index];
			fail = true;
			return default(T);
		}

		#endregion
		
		#region Bonus features: Freeze, Clone, RemoveSection, CopySection, Append, Prepend, Swap

		public void Freeze()
		{
			if (_freezeMode > Frozen)
				AutoThrow();
			_freezeMode = Frozen;
		}
		public bool IsFrozen
		{
			get { return _freezeMode == Frozen; }
		}

		public AList<T> Clone()
		{
			return Clone(true);
		}
		public AList<T> Clone(bool keepObservers)
		{
			AList<T> clone = new AList<T>(this);
			if (!keepObservers)
				clone.ListChanging = null;
			return clone;
		}

		public AList<T> RemoveSection(int index, int count)
		{
			if ((uint)index > _count)
				throw new ArgumentOutOfRangeException("index");
			if ((uint)count > _count - (uint)index)
				throw new ArgumentOutOfRangeException(count < 0 ? "count" : "index+count");

			AList<T> section = CopySection(index, count);
			RemoveRange(index, count);
			return section;
		}
		
		public AList<T> CopySection(int start, int subcount)
		{
			if (subcount < 0)
				throw new ArgumentOutOfRangeException("subcount");
			return CopySection((uint)start, (uint)subcount);
		}
		protected AList<T> CopySection(uint start, uint subcount)
		{
			if (start > _count)
				throw new ArgumentOutOfRangeException("index");
			if (subcount > _count - start)
				subcount = _count - start;
			if (subcount == 0)
				return new AList<T>(_maxNodeSize);

			var section = _root.CopySection(start, subcount);
			return new AList<T>(section, _maxNodeSize, _treeHeight);
		}

		/// <summary>Appends another AList to this list in sublinear time. TODO: UNIT TEST!</summary>
		/// <param name="source">A list of items to be added to this list.</param>
		/// <remarks>When the 'source' list is short, Append() doesn't perform any 
		/// better than a standard AddRange() operation. However, when 'source' has
		/// hundreds or thousands of items, the append operation is performed in
		/// roughly constant time. To accomplish this, the 'source' list is frozen 
		/// </remarks>
		public virtual void Append(AList<T> source)
		{
			int heightDifference = _treeHeight - source._treeHeight;
			if (!(source._root is AListInner<T>))
				InsertRange(Count, (IListSource<T>)source);
			else if (heightDifference < 0)
			{
				AList<T> newSelf = source.Clone();
				newSelf.Prepend(this);
				Swap(newSelf);
			}
			else
			{	// source tree is the same height or less tall
				BeginInsertRange(Count, source);
				int amtInserted = 0;
				try {
					AListNode<T>.AutoClone(ref _root);
					AListNode<T> splitLeft, splitRight;
					splitLeft = ((AListInner<T>)_root).Append((AListInner<T>)source._root, heightDifference, out splitRight);
					AutoSplit(splitLeft, splitRight);
					amtInserted = source.Count;
				} finally {
					DoneInsertRange(amtInserted);
				}
			}
		}

		/// <summary>Prepends an AList to this list in sublinear time. TODO: UNIT TEST!</summary>
		/// <param name="source">A list of items to be added to the front of this list (at index 0).</param>
		public virtual void Prepend(AList<T> source)
		{
			int heightDifference = _treeHeight - source._treeHeight;
			if (!(source._root is AListInner<T>))
				InsertRange(0, (IListSource<T>)source);
			else if (heightDifference < 0)
			{
				AList<T> newSelf = source.Clone();
				newSelf.Append(this);
				Swap(newSelf);
			}
			else
			{	// source tree is the same height or less tall
				BeginInsertRange(0, source);
				int amtInserted = 0;
				try {
					AListNode<T>.AutoClone(ref _root);
					AListNode<T> splitLeft, splitRight;
					splitLeft = ((AListInner<T>)_root).Prepend((AListInner<T>)source._root, heightDifference, out splitRight);
					AutoSplit(splitLeft, splitRight);
					amtInserted = source.Count;
				} finally {
					DoneInsertRange(amtInserted);
				}
			}
		}

		/// <summary>Swaps two instances of <see cref="AList{T}"/> in O(1) time.</summary>
		public virtual void Swap(AList<T> other)
		{
			AutoThrow();
			other.AutoThrow();

			Debug.Assert(_freezeMode == 0 && other._freezeMode == 0);

			_freezeMode = other._freezeMode = FrozenForConcurrency;
			try {
				MathEx.Swap(ref ListChanging, ref other.ListChanging);
				MathEx.Swap(ref _root, ref other._root);
				MathEx.Swap(ref _count, ref other._count);
				MathEx.Swap(ref _maxNodeSize, ref other._maxNodeSize);
				MathEx.Swap(ref _treeHeight, ref other._treeHeight);
				MathEx.Swap(ref _version, ref other._version);
			} finally {
				_freezeMode = other._freezeMode = NotFrozen;
			}
		}

		#endregion
		
		#region Sort

		/// <summary>Uses a specialized "tree quicksort" algorithm to sort this 
		/// list using <see cref="Comparer{T}.Default"/>.</summary>
		public void Sort()
		{
			Sort(Comparer<T>.Default.Compare);
		}
		/// <summary>Uses a specialized "tree quicksort" algorithm to sort this 
		/// list using the specified <see cref="Comparer{T}"/>.</summary>
		public void Sort(Comparer<T> comp)
		{
			Sort(comp.Compare);
		}
		/// <summary>Uses a specialized "tree quicksort" algorithm to sort this 
		/// list using the specified <see cref="Comparison{T}"/>.</summary>
		public void Sort(Comparison<T> comp)
		{
			Sort(0, _count, comp);
		}
		/// <inheritdoc cref="Sort(Comparison{T})"/>
		/// <param name="start">Index of first item in a range of items to sort.</param>
		/// <param name="subcount">Size of the range of items to sort.</param>
		public void Sort(int start, int subcount, Comparison<T> comp)
		{
			Sort((uint)start, (uint)subcount, comp);
		}

		protected void Sort(uint start, uint subcount, Comparison<T> comp)
		{
			if (start > _count)
				throw new ArgumentOutOfRangeException("start");
			if (subcount > _count - start)
				throw new ArgumentOutOfRangeException("subcount");
			AutoThrow();
			if (ListChanging != null && subcount > 1)
			{
				// Although the entire list might not be changing, a Reset is the
				// only applicable notification, because we can't provide a list of 
				// NewItems as required by Replace.
				CallListChanging(new ListChangeInfo<T>(NotifyCollectionChangedAction.Reset, 0, 0, null));
			}

			// Ideally we'd set _freezeMode = FrozenForConcurrency here, but we
			// can't do that because SortCore() relies on Enumerator to scan the 
			// list, and Enumerator throws ConcurrentModificationException() when
			// it notices the _freezeMode. We might still detect concurrent 
			// modification on another thread (for the same reson), but we can't
			// detect simultaneous reading on another thread during the sort.
			SortCore(start, subcount, comp);
			++_version;
		}

		protected virtual void SortCore(uint start, uint subcount, Comparison<T> comp)
		{
			Debug.Assert((_treeHeight == 0) == (_root == null));
			if (_root == null)
				return;

			if (_treeHeight == 1)
			{
				var leaf = (AListLeaf<T>)_root;
				if (leaf.IsFrozen)
					_root = leaf = (AListLeaf<T>)leaf.Clone();
				leaf.Sort((int)start, (int)subcount, comp);
			}
			else
			{
				// The quicksort algorithm requires pre-thawed nodes
				ForceThaw(start, subcount);
				TreeSort(start, subcount, comp);
			}
		}

		protected void ForceThaw(uint start, uint subcount)
		{
			if (_root == null || subcount == 0)
				return;

			var e = new Enumerator(this, start, subcount);
			do {
				Verify(e.MoveNext());
				if (e._leaf.IsFrozen)
					e.HandleLeafCloned(e._leaf.Clone());
				
				// move to the end of this leaf
				int advancement = e._leaf.LocalCount - e._leafIndex - 1;
				e._leafIndex += advancement;
				e._countLeft -= (uint)advancement;
				
				// stop when _countLeft is 0 or underflows (remember, it's unsigned)
			} while(e._countLeft-1 < subcount);
		}

		static Random _r = new Random();
		/// When sorting data that is smaller than this threshold, yet spans 
		/// multiple leaf nodes, the data is copied to an array, sorted, then
		/// copied back to the AList. This minimizes use of the indexer and
		/// minimizes the number of copies of Enumerator that are made.
		const int SortCopyThreshold = 30;

		private void TreeSort(uint start, uint count, Comparison<T> comp)
		{
			for (;;) {
				if (count <= 1)
					return;

				var e = new Enumerator(this, start, count);
				Verify(e.MoveNext());
				if (count <= e._leaf.LocalCount - e._leafIndex) {
					// Do fast sort inside leaf
					AListNode<T> node = e._leaf;
					if (AListNode<T>.AutoClone(ref node))
						e.HandleLeafCloned(node);
					e._leaf.Sort(e._leafIndex, (int)count, comp);
					return;
				}

				if (count <= SortCopyThreshold)
				{
					SortCopy(e, comp);
					return;
				}

				TreeQuickSort(ref start, ref count, e, comp);
			}
		}

		private void TreeQuickSort(ref uint start, ref uint count, Enumerator e, Comparison<T> comp)
		{
			// This is more difficult than a standard quicksort because we must 
			// avoid the O(log N) indexer, and use a minimal number of Enumerators 
			// because building or cloning one also takes O(log N) time.
			uint offset1 = 0;
			T pivot1 = e.Current;

			{	// Start by choosing a pivot based the median of three 
				// values. Two candidates are random, and one is the first element.
				uint offset0 = (uint)_r.Next((int)count - 2) + 1;
				uint offset2 = (uint)_r.Next((int)count - 1) + 1;
				T pivot0 = _root[start + offset0];
				T pivot2 = _root[start + offset2];
				if (comp(pivot0, pivot1) > 0)
				{
					MathEx.Swap(ref pivot0, ref pivot1);
					MathEx.Swap(ref offset0, ref offset1);
				}
				if (comp(pivot1, pivot2) > 0)
				{
					pivot1 = pivot2;
					offset1 = offset2;
					if (comp(pivot0, pivot1) > 0)
					{
						pivot1 = pivot0;
						offset1 = offset0;
					}
				}
			}

			var eBegin = new Enumerator(e);
			var eOut = new Enumerator(e);
			if (offset1 != 0)
			{
				// Swap the pivot to the beginning of the range
				Verify(_root.SetAt(start + offset1, eBegin.Current) == null);
				eBegin.LLSetCurrent(pivot1);
			}

			e.MoveNext();

			T temp;
			bool swapEqual = true;
			// Quick sort pass
			do {
				int order = comp(e.Current, pivot1);
				// Swap *e and *eOut if e.Current is less than pivot. Note: in 
				// case the list contains many duplicate values, we want the 
				// size of the two partitions to be nearly equal. To that end, 
				// we alternately swap or don't swap values that are equal to 
				// the pivot, and we stop swapping in the right half.
				if (order < 0 || (order == 0 && eOut._countLeft > (count >> 1) && (swapEqual = !swapEqual)))
				{
					Verify(eOut.MoveNext());
					temp = e.Current;
					e.LLSetCurrent(eOut.Current);
					eOut.LLSetCurrent(temp);
				}
			} while (e.MoveNext());

			// Finally, put the pivot element in the middle (at *eOut)
			temp = eBegin.Current;
			eBegin.LLSetCurrent(eOut.Current);
			eOut.LLSetCurrent(temp);

			// Now we need to sort the left and right sub-partitions. Use a 
			// recursive call only to sort the smaller partition, in order to 
			// guarantee O(log N) stack space usage.
			uint rightSize = eOut._countLeft;
			uint leftSize = eBegin._countLeft - eOut._countLeft;
			Debug.Assert(leftSize == count - 1 - eOut._countLeft);
			if (eOut._countLeft > (count >> 1))
			{
				// Recursively sort the left partition; iteratively sort the right
				TreeSort(start, leftSize, comp);
				start += leftSize + 1;
				count = rightSize;
			}
			else
			{
				// Recursively sort the right partition; iteratively sort the left
				TreeSort(start + leftSize + 1, rightSize, comp);
				count = leftSize;
			}
		}

		private static void SortCopy(Enumerator e, Comparison<T> comp)
		{
			Enumerator eOut = new Enumerator(e);

			T[] temp = new T[e._countLeft + 1];
			for (int i = 0; i < temp.Length; i++)
			{
				temp[i] = e.Current;
				e.MoveNext();
			}
			
			Array.Sort(temp, comp);
			
			for (int i = 0; i < temp.Length; i++)
			{
				eOut.LLSetCurrent(temp[i]);
				eOut.MoveNext();
			}
		}

		private static T MedianOf3(T a, T b, T c, Comparison<T> comp)
		{
			MathEx.SortPair(ref a, ref b, comp);
			if (comp(b, c) > 0)
			{
				b = c;
				if (comp(a, b) > 0)
					b = a;
			}
			return b;
		}
		static void Verify(bool condition)
		{
			Debug.Assert(condition);
		}
		
		#endregion
	}
}
