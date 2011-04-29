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
	/// there is an extra overhead of about 25% (TODO: calculate accurately).
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
	/// relatively expensive to clone the tree after every modification. It's 
	/// better if you can arrange to have a high ratio of changes to clones.
	/// <para/>
	/// TODO: carefully consider thread safety
	/// <para/>
	/// AList is also freezable, which is useful if you need to construct a list 
	/// in a read-only or freezable data structure. You can also freeze the list
	/// if you want to return a read-only copy of it. After returning a frozen
	/// copy from your class, you can always replace the frozen reference with a
	/// clone if you need to modify list again later. In contrast to cloning, 
	/// freezing does not require a copy of the root node to be created, which is 
	/// ideal if you cannot foresee whether the list will need to be modified 
	/// later.
	/// <para/>
	/// Finally, by writing a derived class, you can take control of node creation 
	/// and disposal, in order to add special features or metadata to the list.
	/// For example, this can be used for indexing--maintaining one or more indexes 
	/// that can help you find items quickly based on attributes of list items.
	/// </remarks>
	/// <seealso cref="BList{T}"/>
	/// <seealso cref="BTree{T}"/>
	/// <seealso cref="DList{T}"/>
	public class AList<T> : IListEx<T>
	{
		public event Action<object, ListChangeInfo<T>> ListChanging;
		protected AListNode<T> _root;
		protected int _count;
		protected byte _maxNodeSize;

		public AList()
		{
			_maxNodeSize = 40;
		}

		public int IndexOf(T item)
		{
			return IndexOf(item, EqualityComparer<T>.Default);
		}
		public int IndexOf(T item, EqualityComparer<T> comparer)
		{
			bool ended = false;
			var it = GetIterator();
			for (int i = 0; i < _count; i++)
			{
				T current = it(ref ended);
				Debug.Assert(!ended);
				if (comparer.Equals(item, current))
					return i;
			}
			return -1;
		}

		public void Insert(int index, T item)
		{
			_root = _root.Insert(index, item);
		}

		public void RemoveAt(int index)
		{
			if (ListChanging != null)
				ListChanging(this, new ListChangeInfo<T>(NotifyCollectionChangedAction.Remove, index, -1, null));
			throw new NotImplementedException();
		}

		public T this[int index]
		{
			get { return _root[index]; }
			set {
				if (ListChanging != null)
					ListChanging(this, new ListChangeInfo<T>(NotifyCollectionChangedAction.Replace, index, 0, Iterable.Single(value)));
				_root[index] = value;
			}
		}

		public void Add(T item)
		{
			Insert(Count, item);
		}

		public void Clear()
		{
			_root = new AListLeaf<T>(_maxNodeSize);
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
			get { return _count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(T item)
		{
			throw new NotImplementedException();
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

		public bool TrySet(int index, T value)
		{
			if ((uint)index < (uint)_count) {
				_root[index] = value;
				return true;
			}
			return false;
		}
		public T TryGet(int index, ref bool fail)
		{
			if ((uint)index < (uint)_count)
				return _root[index];
			fail = true;
			return default(T);
		}
	}

	public abstract class AListNode<T>
	{
		public abstract AListInner<T> Insert(int index, T item);
		public abstract int TotalCount { get; }
		public abstract int LocalCount { get; }
		public abstract bool IsFullLeaf { get; }
		public abstract T this[int index] { get; set; }
		public abstract RemoveResult RemoveAt(int index);
		public enum RemoveResult { OK, Underflow };

		internal abstract void TakeFromRight(AListNode<T> child);
		internal abstract void TakeFromLeft(AListNode<T> child);
	}

	public class AListInner<T> : AListNode<T>
	{
		struct Entry
		{
			public int Index;
			public AListNode<T> Node;
			public static Func<Entry, int, int> Compare = delegate(Entry e, int index)
			{
				return e.Index.CompareTo(index);
			};
		}

		/// <summary>List of child nodes. Empty children are null.</summary>
		/// <remarks>Binary search is optimized for Length of 4 or 8. 
		/// _children[0].Index will be used for some undecided special purpose (not an index).
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
			_children[1] = new Entry { Node = right, Index = left.LocalCount };
		}
		protected AListInner(ListSourceSlice<Entry> slice, int baseIndex)
		{
			if (slice.Count >= MaxNodeSize/2)
				_children = new Entry[Math.Max(MaxNodeSize, slice.Count)];

			for (int i = 0; i < slice.Count; i++)
			{
				_children[i] = slice[i];
				_children[i].Index -= baseIndex;
			}
			_children[0].Index = slice.Count;
		}

		public int BinarySearch(int index)
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
				int i = InternalList.BinarySearch(_children, _children.Length, index, Entry.Compare);
				return i >= 0 ? i : ~i - 1;
			}
			if (index < _children[2].Index)
				return (index >= _children[1].Index ? 1 : 0);
			else
				return 2 + (index >= _children[3].Index ? 1 : 0);
		}

		public override AListInner<T> Insert(int index, T item)
		{
			Debug.Assert((uint)index <= (uint)TotalCount);

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
					childL.TakeFromRight(e.Node);
					_children[i].Index++;
				}
				// Check the right sibling
				else if (i + 1 < _children.Length && (childR = _children[i + 1].Node) != null && !childR.IsFullLeaf)
				{
					childR.TakeFromLeft(e.Node);
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
				_children[i + 1].Index = e.Index + _children[i].Node.LocalCount;
				
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

		[Conditional("DEBUG")]
		private void AssertValid()
		{
			Debug.Assert(LocalCount > 0 && LocalCount <= _children.Length);
			Debug.Assert(_children[0].Node != null);

			int @base = 0;
			for (int i = 1; i < LocalCount; i++) {
				Debug.Assert(_children[i].Node != null);
				Debug.Assert(_children[i].Index == (@base += _children[i-1].Node.TotalCount));
			}
			for (int i = LocalCount; i < _children.Length; i++)
				Debug.Assert(_children[i].Node == null);
		}

		private void LLInsert(int i, AListNode<T> child, int indexAdjustment)
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
			_children[0].Index = (_children[0].Index & ~0xFF) + value;
		}

		public sealed override int TotalCount
		{
			get {
				var e = _children[LocalCount - 1];
				return e.Index + e.Node.TotalCount;
			}
		}

		public override T this[int index]
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

		public Entry GetEntry(int i)
		{
			Entry e = _children[i];
			if (i == 0)
				e.Index = i;
			return e;
		}
		public abstract RemoveResult RemoveAt(int index)
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
				int index = _children[i].Index;
				int indexAdjustment = _children[i + 1].Index - (i > 0 ? index : 0);
				for (int j = i; j < _children.Length - 1; j++)
				{
					_children[j] = _children[j + 1];
					_children[j].Index -= indexAdjustment;
				}
				_children[i].Index = index;
			}
			_children[LocalCount - 1].Node = null;
			--_children[0].Index; // decrement LocalCount
		}

		internal override void TakeFromRight(AListNode<T> sibling)
		{
			throw new NotSupportedException();
			//var right = (AListInner<T>)sibling;
			//var last = _children[LCount - 1];
			//LLInsert(LCount, right.Child(0), 0);
			//_children[LCount].Index = last.Index + last.Node.TotalCount;
			//AssertValid();
		}

		internal override void TakeFromLeft(AListNode<T> sibling)
		{
			throw new NotSupportedException();
			//var left = (AListInner<T>)sibling;
			//var first = _children[0];
			//var child = left.Child(left.LCount-1);
			//LLInsert(0, child, child.TotalCount);
			//AssertValid();
		}
	}

	/// <summary>
	/// Leaf node of <see cref="AList{T}"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class AListLeaf<T> : AListNode<T>
	{
		protected InternalDList<T> _list = InternalDList<T>.Empty;
		private byte _maxNodeSize;
		private byte _isFrozen;
		private short _userData;
		
		protected short UserData { get { return _userData; } set { _userData = value; } }

		public AListLeaf(byte maxNodeSize)
		{
			_maxNodeSize = maxNodeSize;
		}
		public AListLeaf(byte maxNodeSize, ListSourceSlice<T> slice) : this(maxNodeSize)
		{
			_list = new InternalDList<T>(slice.Count + 1);
			_list.PushLast(slice);
		}
		
		public override AListInner<T> Insert(int index, T item)
		{
			if (_list.Count < _maxNodeSize)
			{
				_list.AutoEnlarge(1, _maxNodeSize);
				_list.Insert(index, item);
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
					right.Insert(index - divAt, item);
				return new AListInner<T>(left, right);
			}
		}

		public override int LocalCount
		{
			get { return _list.Count; }
		}

		public override T this[int index]
		{
			get { return _list[index]; }
			set { _list[index] = value; }
		}

		internal override void TakeFromRight(AListNode<T> child)
		{
			var right = (AListLeaf<T>)child;
			_list.PushLast(right._list.PopFirst());
		}
		internal override void TakeFromLeft(AListNode<T> child)
		{
			var left = (AListLeaf<T>)child;
			_list.PushFirst(left._list.PopLast());
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
