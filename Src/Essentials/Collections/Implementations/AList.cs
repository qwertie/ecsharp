using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Diagnostics;
using Loyc.Collections.Impl;
using Loyc.Collections.Linq;

namespace Loyc.Collections
{
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
	/// data structure with a faster indexer and lower memory requirements.
	/// <para/>
	/// Structurally, A-list data structure is very similar to a B+tree. It uses 
	/// memory almost as efficiently as an array, and offers O(log N) insertion and
	/// deletion in exchange for a O(log N) indexer, which is slower than the 
	/// indexer of <see cref="List{T}"/>. It uses slightly more memory than <see 
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
			_root = _root.Insert(index, item, _maxNodeSize);
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
			_root = new AListLeaf<T>();
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
		public abstract AListInner<T> Insert(int index, T item, int maxNodeSize);
		public abstract int LCount { get; }
		public abstract T this[int index] { get; set; }

		internal abstract void TakeFromRight(AListNode<T> child);
		internal abstract void TakeFromLeft(AListNode<T> child);
	}
	
	public class AListInner<T> : AListNode<T>
	{
		protected struct Entry
		{
			public int Index;
			public AListNode<T> Node;
			public static Func<Entry, int, int> Compare = delegate(Entry e, int index)
			{
				return e.Index.CompareTo(index);
			};
		}
		
		protected InternalDList<Entry> _children = InternalDList<Entry>.Empty;

		public AListInner(AListNode<T> left, AListNode<T> right)
		{
			_children.Add(new Entry { Node = left, Index = 0 });
			_children.Add(new Entry { Node = right, Index = left.LCount });
		}

		protected AListInner(ListSourceSlice<Entry> slice)
		{
			_children.PushLast(slice);
		}

		public override AListInner<T> Insert(int index, T item, int maxNodeSize)
		{
			// Choose a child node [i] = entry {child, baseIndex} in which to insert the item(s)
			int i0 = _children.BinarySearch(index, Entry.Compare);
			int i = i0 >= 0 ? i0 : ~i0 - 1;
			AListNode<T> child = _children[i].Node;
			int baseIndex = _children[i].Index;
			if (i0 > 0) {
				AListNode<T> childL = _children[i0-1].Node;
				if (childL.LCount < child.LCount) {
					child = childL;
					baseIndex = _children[i0 - 1].Index;
					i--;
				}
			}
			
			// If the child is full, consider shifting an element to a sibling
			if (child.LCount >= maxNodeSize)
			{
				AListNode<T> childL, childR;
				// Check the left sibling
				if (i > 0 && (childL = _children[i - 1].Node).LCount < maxNodeSize)
				{
					childL.TakeFromRight(child);
					_children.InternalArray[_children.Internalize(i)].Index++;
				}
				// Check the right sibling
				else if (i + 1 < _children.Count && (childR = _children[i + 1].Node).LCount < maxNodeSize)
				{
					childR.TakeFromLeft(child);
					_children.InternalArray[_children.Internalize(i + 1)].Index--;
				}
			}

			// Perform the insert, and adjust base index of nodes that follow
			var split = child.Insert(index - baseIndex, item, maxNodeSize);
			for (int iR = i + 1; iR < _children.Count; iR++)
				_children.InternalArray[_children.Internalize(iR)].Index++;

			// Handle child split
			if (split != null)
			{
				Debug.Assert(split.LCount == 2);
				_children.AutoEnlarge(1, maxNodeSize + 1);
				_children.InternalArray[_children.Internalize(i)].Node = split.Child(0);
				_children.Insert(i + 1, new Entry { Node = split.Child(1), Index = baseIndex + split.Child(0).LCount });
				
				if (_children.Count <= maxNodeSize)
					return null;
				else {
					int divAt = _children.Count >> 1;
					var left = new AListInner<T>(_children.Slice(0, divAt));
					var right = new AListInner<T>(_children.Slice(divAt, _children.Count - divAt));
					return new AListInner<T>(left, right);
				}
			}
			return null;
		}

		public AListNode<T> Child(int index)
		{
			return _children[index].Node;
		}

		public override int LCount
		{
			get { throw new NotImplementedException(); }
		}

		public override T this[int index]
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		internal override void TakeFromRight(AListNode<T> child)
		{
			var right = (AListInner<T>)child;
			_children.PushLast(right._children.PopFirst());
		}

		internal override void TakeFromLeft(AListNode<T> child)
		{
			var left = (AListInner<T>)child;
			_children.PushFirst(left._children.PopLast());
		}
	}


	public class AListLeaf<T> : AListNode<T>
	{
		protected InternalDList<T> _list = InternalDList<T>.Empty;

		public AListLeaf() { }
		public AListLeaf(ListSourceSlice<T> slice)
		{
			_list = new InternalDList<T>(slice.Count + 1);
			_list.PushLast(slice);
		}
		
		public override AListInner<T> Insert(int index, T item, int maxNodeSize)
		{
			if (_list.Count < maxNodeSize)
			{
				_list.AutoEnlarge(1, maxNodeSize);
				_list.Insert(index, item);
				return null;
			}
			else
			{
				int divAt = _list.Count >> 1;
				var left = new AListLeaf<T>(_list.Slice(0, divAt));
				var right = new AListLeaf<T>(_list.Slice(divAt, _list.Count - divAt));
				return new AListInner<T>(left, right);
			}
		}

		public override int LCount
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
