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
	/// TODO: This data structure is not finished.
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
	/// expect to do insertions and deletions at random locations but you need
	/// a fast indexer (because you read the list far more often than you change 
	/// it), consider using <see cref="DList{T}"/> instead.
	/// <para/>
	/// In addition, you can subscribe to the <see cref="ListChanging"/> event to
	/// find out when the list changes. AList's observability is more lightweight 
	/// than that of <see cref="ObservableCollection{T}"/>.
	/// <para/>
	/// AList is an excellent choice if you need to make occasional snapshots of
	/// the tree. Cloning is fast and memory-efficient, because only the root 
	/// node is cloned. All other nodes are duplicated on-demand as changes are 
	/// made. Thus, AList can be used as a persistent data structure, but it is
	/// relatively expensive to clone the tree after every modification. When 
	/// modifying a tree that was just cloned (remember, AList is really a tree),
	/// the leaf node being changed and all of its ancestors must be duplicated. 
	/// Therefore, it's better if you can arrange to have a high ratio of changes 
	/// to clones.
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
	/// <seealso cref="BList{T}"/>
	/// <seealso cref="BTree{T}"/>
	/// <seealso cref="DList{T}"/>
	[Serializable]
	[DebuggerTypeProxy(typeof(ListSourceDebugView<>)), DebuggerDisplay("Count = {Count}")]
	public class AList<T> : IListEx<T>, ICloneable<AList<T>>
	{
		public event Action<object, ListChangeInfo<T>> ListChanging;
		protected AListNode<T> _root;
		protected uint _count;
		protected byte _maxNodeSize;
		protected byte _version;
		private byte _userByte;
		private byte _freezeMode = NotFrozen; // 0 for writable, 1 for frozen, 2 during modification
		private const byte NotFrozen = 0;
		private const byte Frozen = 1;
		private const byte FrozenForListChanging = 2;
		private const byte FrozenForConcurrency = 3;

		protected byte UserByte { get { return _userByte; } set { _userByte = value; } }

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

		protected virtual void CreateRoot()
		{
			_root = new AListLeaf<T>(_maxNodeSize);
		}
		private void AutoCreateRoot()
		{
			if (_root == null) {
				Debug.Assert(_count == 0);
				CreateRoot();
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
					throw new InvalidOperationException("AList was modified concurrently.");
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
				_root = _root.Insert((uint)index, item);
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

			int sourceIndex = 0;
			try {
				if (ListChanging != null)
					CallListChanging(new ListChangeInfo<T>(NotifyCollectionChangedAction.Add, index, source.Count, source));
				_freezeMode = FrozenForConcurrency;

				AutoCreateRoot();
				do	_root = _root.Insert((uint)index, source, ref sourceIndex);
				while (sourceIndex < source.Count);
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

		private void RemoveRange(int index, int amount)
		{
			if (amount == 0)
				return;
			if ((uint)index > (uint)Count)
				throw new IndexOutOfRangeException();
			if (amount <= 0 || (uint)(index + amount) > (uint)Count)
				throw new ArgumentOutOfRangeException("amount");
			
			AutoThrow();
			int i = 0;
			try {
				if (ListChanging != null)
					CallListChanging(new ListChangeInfo<T>(NotifyCollectionChangedAction.Remove, index, -amount, null));
				_freezeMode = FrozenForConcurrency;

				for (; i < amount; i++)
					LLRemoveAt(index);
			} finally {
				_freezeMode = NotFrozen;
				++_version;
				_count -= (uint)i;
				Debug.Assert(_count == _root.TotalCount);
			}
		}

		private void LLRemoveAt(int index)
		{
			var result = _root.RemoveAt((uint)index);
			if (result == AListNode<T>.RemoveResult.Underflow && _root.LocalCount <= 1)
			{
				if (_root is AListInner<T>)
					_root = ((AListInner<T>)_root).Child(0);
				else if (_root.LocalCount == 0)
					_root = null;
			}
		}

		public void RemoveAt(int index)
		{
			if ((uint)index >= (uint)Count)
				throw new IndexOutOfRangeException();
			AutoThrow();

			if (ListChanging != null)
				CallListChanging(new ListChangeInfo<T>(NotifyCollectionChangedAction.Remove, index, -1, null));
			try {
				_freezeMode = FrozenForConcurrency;
				LLRemoveAt(index);
				++_version;
				checked { --_count; }
			} finally {
				_freezeMode = NotFrozen;
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
					ListChanging(this, new ListChangeInfo<T>(NotifyCollectionChangedAction.Replace, index, 0, Iterable.Single(value)));
				_root[(uint)index] = value;
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
			throw new NotImplementedException();
		}
		public Iterator<T> GetIterator(int startIndex)
		{
			return GetIterator(startIndex, int.MaxValue);
		}
		public Iterator<T> GetIterator(int startIndex, int count)
		{
			throw new NotImplementedException();
		}
		public Iterator<T> GetIterator(int startIndex, int count, bool countDown)
		{
			if (!countDown)
				return GetIterator(startIndex, count);
			throw new NotImplementedException();
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
			_root[(uint)index] = value;
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
			throw new NotImplementedException();
		}
	}




	/// <summary>
	/// Base class for nodes in an <see cref="AList{T}"/>. These nodes basically 
	/// form an in-memory B+tree, so there are two types: leaf and inner nodes.
	/// </summary>
	/// <remarks>
	/// Indexes that are passed to methods such as Index, this[] and RemoveAt are
	/// not range-checked except by assertion. The caller (AList) is expected to 
	/// ensure indexes are valid.
	/// <para/>
	/// At the root node level, indexes have the same meaning as they do in AList
	/// itself. However, below the root node, each node has a "base index" that 
	/// is subtracted from any index passed to the node. For example, if the root 
	/// node has two leaf children, and the left one has 20 items, then the right
	/// child's base index is 20. When accessing item 23, the subindex 3 is passed 
	/// to the right child. Note that the right child is not aware of its own base
	/// index (the parent node manages the base index); as far as each node is 
	/// concerned, it manages a collection of items numbered 0 to TotalCount-1.
	/// <para/>
	/// Indexes are expressed with a uint so that nodes are capable of holding up 
	/// to uint.MaxValue-1 elements. AList itself doesn't support sizes over 
	/// int.MaxValue, since it assumes indexes are signed. It should be possible 
	/// to support oversize lists in 64-bit machines by writing a derived class 
	/// based on "uint" or "long" indexes; 32-bit processes, generally, don't 
	/// have enough address space to even hold int.MaxValue bytes.
	/// </remarks>
	[Serializable]
	public abstract class AListNode<T>
	{
		/// <summary>Inserts an item at the specified index.</summary>
		/// <param name="index"></param>
		/// <param name="item"></param>
		/// <returns>Returns null if the insert completed normally. If the node 
		/// split, a pair of replacement nodes are returned in a new AListInner 
		/// object, which is a temporary object unless it becomes the new root 
		/// node.</returns>
		public abstract AListInner<T> Insert(uint index, T item);
		/// <summary>Inserts a list of items at the specified index. This method
		/// may not insert all items at once, so there is a sourceIndex parameter 
		/// which points to the next item to be inserted. When sourceIndex reaches
		/// source.Count, the insertion is complete.</summary>
		/// <returns>Returns non-null on split, as explained in the other overload.</returns>
		public abstract AListInner<T> Insert(uint index, IListSource<T> source, ref int sourceIndex);
		/// <summary>Gets the total number of (T) items in this node and all children</summary>
		public abstract uint TotalCount { get; }
		/// <summary>Gets the number of items (slots) used this node only.</summary>
		public abstract int LocalCount { get; }
		/// <summary>Returns true if the node is full and is a leaf node.</summary>
		public abstract bool IsFullLeaf { get; }
		/// <summary>Gets or sets an item at the specified sub-index.</summary>
		public abstract T this[uint index] { get; set; }
		/// <summary>Removes an item at the specified index</summary>
		/// <returns>If the result is Underflow, it means that the node size has 
		/// dropped below its normal range. Unless this is the root node, the 
		/// parent will shift items from siblings, or discard the node and 
		/// redistribute its children among existing nodes. In case of the root 
		/// node, it is only discarded if it is an inner node with a single child
		/// (the child becomes the new root node).</returns>
		public abstract RemoveResult RemoveAt(uint index);
		public enum RemoveResult { OK, Underflow };

		public abstract bool IsFrozen { get; }
		public abstract void Freeze();
	}

	[Serializable]
	public class AListInner<T> : AListNode<T>
	{
		protected struct Entry
		{
			public uint Index;
			public AListNode<T> Node;
			public static Func<Entry, uint, int> Compare = delegate(Entry e, uint index)
			{
				return e.Index.CompareTo(index);
			};
		}

		/// <summary>List of child nodes. Empty children are null.</summary>
		/// <remarks>Binary search is optimized for Length of 4 or 8. 
		/// _children[0].Index holds special information (not an index):
		/// 1. The low byte holds the number of slots used in _children.
		/// 2. The second byte holds the maximum node size.
		/// 3. The third byte marks the node as frozen when it is nonzero
		/// 4. The fourth byte is available for the derived class to use
		/// </remarks>
		Entry[] _children;
		const int MaxNodeSize = 8;

		public override bool IsFullLeaf
		{
			get { return false; }
		}

		public AListInner(AListNode<T> left, AListNode<T> right)
		{
			_children = new Entry[4];
			_children[0] = new Entry { Node = left, Index = 0 };
			_children[1] = new Entry { Node = right, Index = left.TotalCount };
			_children[2] = new Entry { Index = int.MaxValue };
			_children[3] = new Entry { Index = int.MaxValue };
		}
		protected AListInner(ListSourceSlice<Entry> slice, uint baseIndex)
		{
			if (slice.Count >= MaxNodeSize/2)
				_children = new Entry[Math.Max(MaxNodeSize, slice.Count)];

			int i;
			for (i = 0; i < slice.Count; i++)
			{
				_children[i] = slice[i];
				_children[i].Index -= baseIndex;
			}
			for (; i < _children.Length; i++)
				_children[i].Index = int.MaxValue;

			_children[0].Index = (uint)slice.Count;
		}

		public int BinarySearch(uint index)
		{
			// optimize for Length 4 and 8
			if (_children.Length == 8) {
				if (index >= _children[4].Index) {
					if (index < _children[6].Index)
						return 4 + (index >= _children[5].Index ? 1 : 0);
					else
						return 6 + (index >= _children[7].Index ? 1 : 0);
				}
			} else if (_children.Length != 4) {
				int i = InternalList.BinarySearch(_children, LocalCount, index, Entry.Compare);
				return i >= 0 ? i : ~i - 1;
			}
			if (index < _children[2].Index)
				return (index >= _children[1].Index ? 1 : 0);
			else
				return 2 + (index >= _children[3].Index ? 1 : 0);
		}

		public override AListInner<T> Insert(uint index, T item)
		{
			Debug.Assert(index <= TotalCount);

			// Choose a child node [i] = entry {child, baseIndex} in which to insert the item(s)
			int i = BinarySearch(index);
			Entry e = _children[i];
			if (i == 0)
				e.Index = 0;
			else if (e.Index == index) {
				// Check whether one slot left is a better insertion location
				Entry eL = _children[i-1];
				if (eL.Node.LocalCount < e.Node.LocalCount) {
					e = eL;
					if (--i == 0)
						e.Index = 0;
				}
			}
			
			// If the child is a full leaf, consider shifting an element to a sibling
			if (e.Node.IsFullLeaf)
			{
				AListNode<T> childL, childR;
				// Check the left sibling
				if (i > 0 && !(childL = _children[i - 1].Node).IsFullLeaf)
				{
					((AListLeaf<T>)childL).TakeFromRight(e.Node);
					_children[i].Index++;
				}
				// Check the right sibling
				else if (i + 1 < _children.Length && (childR = _children[i + 1].Node) != null && !childR.IsFullLeaf)
				{
					((AListLeaf<T>)childR).TakeFromLeft(e.Node);
					_children[i + 1].Index--;
				}
			}

			// Perform the insert, and adjust base index of nodes that follow
			var split = e.Node.Insert(index - e.Index, item);
			for (int iR = i + 1; iR < _children.Length; iR++)
				_children[iR].Index++;

			// Handle child split
			if (split != null)
			{
				Debug.Assert(split.LocalCount == 2);
				_children[i].Node = split.Child(0);
				LLInsert(i + 1, split.Child(1), 0);
				_children[i + 1].Index = e.Index + _children[i].Node.TotalCount;
				
				// Does this node need to split too?
				if (_children.Length <= MaxNodeSize)
					return null;
				else {
					int divAt = _children.Length >> 1;
					var left = new AListInner<T>(_children.AsListSource().Slice(0, divAt), 0);
					var right = new AListInner<T>(_children.AsListSource().Slice(divAt, _children.Length - divAt), _children[divAt].Index);
					return new AListInner<T>(left, right);
				}
			}
			AssertValid();
			return null;
		}

		public override AListInner<T> Insert(uint index, IListSource<T> source, ref int sourceIndex)
		{
			throw new NotImplementedException();
		}

		[Conditional("DEBUG")]
		private void AssertValid()
		{
			Debug.Assert(LocalCount > 0 && LocalCount <= _children.Length);
			Debug.Assert(_children[0].Node != null);

			uint @base = 0;
			for (int i = 1; i < LocalCount; i++) {
				Debug.Assert(_children[i].Node != null);
				Debug.Assert(_children[i].Index == (@base += _children[i-1].Node.TotalCount));
			}
			for (int i = LocalCount; i < _children.Length; i++)
				Debug.Assert(_children[i].Node == null);
		}

		private void LLInsert(int i, AListNode<T> child, uint indexAdjustment)
		{
			Debug.Assert(LocalCount <= MaxNodeSize);
			if (LocalCount == _children.Length)
				_children = InternalList.CopyToNewArray(_children, _children.Length, Math.Min(_children.Length * 2, MaxNodeSize + 1));
			for (int j = _children.Length - 1; j > i; j--)
				_children[j] = _children[j - 1]; // insert room
			if (i == 0)
				_children[1].Index = 0;
			if (indexAdjustment != 0)
				for (int j = i + 1; j < _children.Length; j++)
					_children[j].Index += indexAdjustment;
			_children[i].Node = child;
			++_children[0].Index; // increment LocalCount
		}

		public AListNode<T> Child(int i)
		{
			return _children[i].Node;
		}

		public sealed override int LocalCount
		{
			get {
				Debug.Assert((byte)_children[0].Index >= 2);
				return (byte)_children[0].Index;
			}
		}
		void SetLCount(int value)
		{
			Debug.Assert((uint)value < 0xFFu);
			_children[0].Index = (_children[0].Index & ~0xFFu) + (uint)value;
		}

		public sealed override uint TotalCount
		{
			get {
				var e = _children[LocalCount - 1];
				return e.Index + e.Node.TotalCount;
			}
		}

		public override T this[uint index]
		{
			get {
				int i = BinarySearch(index);
				if (i == 0)
					return _children[i].Node[index];
				return _children[i].Node[index - _children[i].Index];
			}
			set {
				int i = BinarySearch(index);
				if (i == 0)
					_children[i].Node[index] = value;
				_children[i].Node[index - _children[i].Index] = value;
			}
		}

		protected Entry GetEntry(int i)
		{
			Entry e = _children[i];
			if (i == 0)
				e.Index = 0;
			return e;
		}
		public override RemoveResult RemoveAt(uint index)
		{
			Debug.Assert((uint)index < (uint)TotalCount);
			int i = BinarySearch(index);
			var e = GetEntry(i);
			if (e.Node.RemoveAt(index - e.Index) == RemoveResult.Underflow)
			{
				LLDelete(i, e);
				AssertValid();
				if (LocalCount < MaxNodeSize / 2)
					return RemoveResult.Underflow;
			}
			return RemoveResult.OK;
		}
		private void LLDelete(int i, Entry e)
		{
			if (i < LocalCount-1) {
				uint index = _children[i].Index;
				uint indexAdjustment = _children[i + 1].Index - (i > 0 ? index : 0);
				for (int j = i; j < _children.Length - 1; j++)
				{
					_children[j] = _children[j + 1];
					_children[j].Index -= indexAdjustment;
				}
				_children[i].Index = index;
			}
			_children[LocalCount - 1] = new Entry { Node = null, Index = int.MaxValue };
			--_children[0].Index; // decrement LocalCount
		}

		internal void TakeFromRight(AListNode<T> sibling)
		{
			throw new NotSupportedException();
			//var right = (AListInner<T>)sibling;
			//var last = _children[LCount - 1];
			//LLInsert(LCount, right.Child(0), 0);
			//_children[LCount].Index = last.Index + last.Node.TotalCount;
			//AssertValid();
		}

		internal void TakeFromLeft(AListNode<T> sibling)
		{
			throw new NotSupportedException();
			//var left = (AListInner<T>)sibling;
			//var first = _children[0];
			//var child = left.Child(left.LCount-1);
			//LLInsert(0, child, child.TotalCount);
			//AssertValid();
		}

		protected byte UserByte
		{
			get { return (byte)(_children[0].Index >> 24); }
			set { _children[0].Index = (_children[0].Index & 0xFFFFFF) | ((uint)value << 24); }
		}

		public sealed override bool IsFrozen
		{
			get { return ((_children[0].Index >> 16) & 1) != 0; }
		}
		public override void Freeze()
		{
			_children[0].Index |= 0x10000;
		}
	}

	/// <summary>
	/// Leaf node of <see cref="AList{T}"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[Serializable]
	public class AListLeaf<T> : AListNode<T>
	{
		protected InternalDList<T> _list = InternalDList<T>.Empty;
		private byte _maxNodeSize;
		private bool _isFrozen;
		private byte _userByte;
		
		protected byte UserByte { get { return _userByte; } set { _userByte = value; } }

		public AListLeaf(byte maxNodeSize)
		{
			Debug.Assert(maxNodeSize >= 3);
			_maxNodeSize = maxNodeSize;
		}
		public AListLeaf(byte maxNodeSize, ListSourceSlice<T> slice) : this(maxNodeSize)
		{
			_list = new InternalDList<T>(slice.Count + 1);
			_list.PushLast(slice);
		}
		
		public override AListInner<T> Insert(uint index, T item)
		{
			if (_list.Count < _maxNodeSize)
			{
				_list.AutoEnlarge(1, _maxNodeSize);
				_list.Insert((int)index, item);
				return null;
			}
			else
			{
				int divAt = _list.Count >> 1;
				var left = new AListLeaf<T>(_maxNodeSize, _list.Slice(0, divAt));
				var right = new AListLeaf<T>(_maxNodeSize, _list.Slice(divAt, _list.Count - divAt));
				if (index <= divAt)
					left.Insert(index, item);
				else
					right.Insert(index - (uint)divAt, item);
				return new AListInner<T>(left, right);
			}
		}
		public override AListInner<T> Insert(uint index, IListSource<T> source, ref int sourceIndex)
		{
			throw new NotImplementedException();
		}

		public override int LocalCount
		{
			get { return _list.Count; }
		}

		public override T this[uint index]
		{
			get { return _list[(int)index]; }
			set { _list[(int)index] = value; }
		}

		internal void TakeFromRight(AListNode<T> child)
		{
			var right = (AListLeaf<T>)child;
			_list.PushLast(right._list.PopFirst());
		}

		internal void TakeFromLeft(AListNode<T> child)
		{
			var left = (AListLeaf<T>)child;
			_list.PushFirst(left._list.PopLast());
		}

		public override uint TotalCount
		{
			get { return (uint)_list.Count; }
		}

		public override bool IsFullLeaf
		{
			get { return _list.Count >= _maxNodeSize; }
		}

		public override RemoveResult RemoveAt(uint index)
		{
			_list.RemoveAt((int)index);
			return _list.Count > _maxNodeSize / 3 ? RemoveResult.OK : RemoveResult.Underflow;
		}

		public override bool IsFrozen
		{
			get { return _isFrozen; }
		}
		public override void Freeze()
		{
			_isFrozen = true;
		}
	}

	public class ListChangeInfo<T> : EventArgs
	{
		public ListChangeInfo(NotifyCollectionChangedAction action, int index, int sizeChange, IListSource<T> newItems)
		{
			Action = action;
			Index = index;
			SizeChange = sizeChange;
			NewItems = newItems;
			Debug.Assert(
				(action == NotifyCollectionChangedAction.Add && newItems != null && NewItems.Count == sizeChange) ||
				(action == NotifyCollectionChangedAction.Remove && newItems == null && sizeChange < 0) ||
				(action == NotifyCollectionChangedAction.Replace && newItems != null && sizeChange == 0) ||
				(action == NotifyCollectionChangedAction.Move && sizeChange == 0) ||
				(action == NotifyCollectionChangedAction.Reset));
		}

		public readonly NotifyCollectionChangedAction Action;
		public readonly int Index;
		public readonly int SizeChange;
		public readonly IListSource<T> NewItems;
	}
}
