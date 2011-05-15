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
	
	/// <summary>
	/// An all-purpose list structure with the following additional features beyond 
	/// what's offered by <see cref="List{T}"/>: fast insertion and deletion 
	/// (O(log N)), observability, fast cloning, freezability, and extensibility.
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
	/// Structurally, AList (and BList) are very similar to B+trees. They uses 
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
	/// it has a faster indexer. Note that both classes provide fast enumeration.
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
	/// TODO: carefully consider thread safety
	/// <para/>
	/// AList is also freezable, which is useful if you need to construct a list 
	/// in a read-only or freezable data structure. You could also freeze the list
	/// in order to return a read-only copy of it, which, compared to cloning, has 
	/// the advantage that no memory allocation is required at the time you return 
	/// the list. If you need to edit the list later, you can clone the list (the 
	/// clone can be modified).
	/// <para/>
	/// Finally, by writing a derived class, you can take control of node creation 
	/// and disposal, in order to add special features or metadata to the list.
	/// For example, this can be used for indexing--maintaining one or more indexes 
	/// that can help you find items quickly based on attributes of list items.
	/// <para/>
	/// AList{T} can support multiple readers concurrently, as long as the 
	/// collection is not modified. An instance of AList{T} must not be accessed 
	/// from other threads during any modification. AList{T} has a mechanism to 
	/// detect illegal concurrent access and throw InvalidOperationException if 
	/// it is detected, but this is not designed to be reliable; its main purpose
	/// is to help you find bugs. If concurrent modification is not detected, 
	/// the AList will probably become corrupted and produce strange exceptions,
	/// or fail an assertion.
	/// </remarks>
	/// <seealso cref="BList{T}"/>
	/// <seealso cref="BTree{T}"/>
	/// <seealso cref="DList{T}"/>
	[Serializable]
	//[DebuggerTypeProxy(typeof(ListSourceDebugView<>)), DebuggerDisplay("Count = {Count}")]
	public class AList<T> : IListEx<T>, IInsertRemoveRange<T>, IGetIteratorSlice<T>, ICloneable<AList<T>>
	{
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

		protected virtual AListLeaf<T> CreateRoot()
		{
			return new AListLeaf<T>(_maxNodeSize);
		}
		protected virtual AListInner<T> SplitRoot(AListNode<T> left, AListNode<T> right)
		{
			return new AListInner<T>(left, right, AListInner<T>.DefaultMaxNodeSize);
		}
		private void AutoCreateRoot()
		{
			if (_root == null) {
				Debug.Assert(_count == 0);
				_root = CreateRoot();
				_treeHeight = 1;
			}
		}
		private void AutoSplit(AListNode<T> splitLeft, AListNode<T> splitRight)
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
		private void AutoThrow()
		{
			if (_freezeMode != NotFrozen) {
				if (_freezeMode == FrozenForListChanging)
					throw new InvalidOperationException("Cannot insert or remove items in AList during a ListChanging event.");
				else if (_freezeMode == FrozenForConcurrency)
					throw new InvalidOperationException("AList was accessed concurrently while being modified.");
				else if (_freezeMode == Frozen)
					throw new InvalidOperationException("Cannot modify AList that is frozen.");
			}
		}

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
			if ((uint)index > (uint)_count)
				throw new IndexOutOfRangeException();
			AutoThrow();
			if (ListChanging != null)
				CallListChanging(new ListChangeInfo<T>(NotifyCollectionChangedAction.Add, index, source.Count, source));

			int sourceIndex = 0;
			try {
				_freezeMode = FrozenForConcurrency;

				AutoCreateRoot();
				while (sourceIndex < source.Count)
				{
					AListNode<T> splitLeft, splitRight;
					splitLeft = _root.Insert((uint)index, source, ref sourceIndex, out splitRight);
					AutoSplit(splitLeft, splitRight);
				}
			}
			finally {
				++_version;
				_freezeMode = NotFrozen;
				checked { _count += (uint)sourceIndex; };
				Debug.Assert(_count == _root.TotalCount);
			}
		}

		public void Resize(int newSize)
		{
			if (newSize < Count)
				RemoveRange(newSize, Count - newSize);
			else if (newSize > Count)
				InsertRange(Count, Iterable.Repeat(default(T), newSize - Count));
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

		public void RemoveAt(int index)
		{
			if ((uint)index >= (uint)Count)
				throw new IndexOutOfRangeException();
			
			RemoveInternal(index, 1);
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
		private void HandleClonedOrUndersizedRoot(AListNode<T> result)
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

		private void CallListChanging(ListChangeInfo<T> listChangeInfo)
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

		public T this[int index]
		{
			get {
				if ((uint)index >= (uint)Count)
					throw new IndexOutOfRangeException();
				if (_freezeMode == FrozenForConcurrency)
					AutoThrow();
				return _root[(uint)index];
			}
			set {
				if ((_freezeMode & 1) != 0) // Frozen or FrozenForConcurrency, but not FrozenForListChanging
					AutoThrow();
				if ((uint)index >= (uint)Count)
					throw new IndexOutOfRangeException();
				if (ListChanging != null)
					CallListChanging(new ListChangeInfo<T>(NotifyCollectionChangedAction.Replace, index, 0, Iterable.Single(value)));
				_root = _root.SetAt((uint)index, value) ?? _root;
			}
		}

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

		public virtual void Clear()
		{
			AutoThrow();
			_root = null;
			_count = 0;
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
			get { return false; }
		}

		public bool Remove(T item)
		{
			int index = IndexOf(item);
			if (index <= -1)
				return false;
			RemoveAt(index);
			return true;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return GetIterator().AsEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetIterator().AsEnumerator();
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
			if (start >= _count) {
				if (start == _count)
					return EmptyIterator<T>.Value;
				throw new ArgumentOutOfRangeException("start");
			}
			if (_root == null || subcount == 0)
				return EmptyIterator<T>.Value;
			
			var rootLeaf = _root as AListLeaf<T>;
			if (rootLeaf != null)
			{
				if (_treeHeight != 1) throw new InvalidStateException();
				return rootLeaf.GetIterator((int)start, (int)subcount);
			}
			
			var stack = new Pair<AListInner<T>, int>[_treeHeight - 1];
			var node = _root as AListInner<T>;
			int sub_i = 0;
			for (int i = 0; i < stack.Length; i++)
			{
				if (node == null) throw new InvalidStateException();
				sub_i = 0;
				if (start != 0) {
					sub_i = node.BinarySearch(start);
					start -= node.ChildIndexOffset(sub_i);
				}
				stack[i] = Pair.Create(node, sub_i);
				node = node.Child(sub_i) as AListInner<T>;
			}
			if (node != null) throw new InvalidStateException();

			AListLeaf<T> leaf = (AListLeaf<T>)stack[stack.Length-1].Item1.Child(sub_i);
			Debug.Assert(start < leaf.LocalCount);
			int leafIndex = (int)start-1;
			byte expectedVersion = _version;

			return delegate(ref bool ended)
			{
				if (subcount == 0)
					goto end;
				--subcount;

				if (++leafIndex >= leaf.LocalCount) {
					if (expectedVersion != _version)
						throw new EnumerationException();
					if (stack == null)
						goto end;
					else {
						int s = stack.Length - 1;
						while (++stack[s].Item2 >= stack[s].Item1.LocalCount) {
							if (--s < 0)
								goto end;
						}
						while (++s < stack.Length)
							stack[s] = Pair.Create((AListInner<T>)stack[s-1].Item1.Child(stack[s-1].Item2), 0);
						leaf = (AListLeaf<T>)stack[stack.Length-1].Item1.Child(stack[stack.Length-1].Item2);
						leafIndex = 0;
					}
				}
				return leaf[(uint)leafIndex];

			end:
				stack = null;
				ended = true;
				return default(T);
			};
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
			if (ListChanging != null)
				ListChanging(this, new ListChangeInfo<T>(NotifyCollectionChangedAction.Replace, index, 0, Iterable.Single(value)));
			_root = _root.SetAt((uint)index, value) ?? _root;
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

		public AList<T> Clone()
		{
			return new AList<T>(this);
		}
	}
}
