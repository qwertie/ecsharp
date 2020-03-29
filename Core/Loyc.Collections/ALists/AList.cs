using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Diagnostics;
using Loyc.Collections.Impl;
using Loyc.Math;

namespace Loyc.Collections
{
	/// <summary>
	/// An all-purpose list structure with the following additional features beyond 
	/// what's offered by <see cref="List{T}"/>: fast insertion and deletion 
	/// (O(log N)), batch insertion and deletion, observability, fast cloning, 
	/// freezability, and fast splitting and joining of large collections.
	/// </summary>
	/// An <a href="http://core.loyc.net/collections/alists-part1.html">article</a>
	/// about this class is available.
	/// <para/>
	/// <typeparam name="T">Type of each element in the list</typeparam>
	/// <remarks>
	/// The name "A-List" is short for All-purpose List. It is so named because
	/// it has a very large amount of functionality and extension points for 
	/// further extending this functionality. Essentially, this data structure
	/// is jack-of-all-trades, master of none.
	/// <para/>
	/// Structurally, ALists (like BLists) are very similar to B+trees. They use
	/// memory almost as efficiently as arrays, and offer O(log N) insertion and
	/// deletion in exchange for a O(log N) indexer, which is distinctly slower 
	/// than the indexer of <see cref="List{T}"/>. They use slightly more memory 
	/// than <see cref="List{T}"/> for all list sizes.
	/// <para/>
	/// That said, you should use an AList whenever you know that the list might be 
	/// large and need insertions or deletions somewhere in the middle. If you 
	/// expect to do insertions and deletions at random locations, but only 
	/// occasionally, <see cref="DList{T}"/> is sometimes a better choice because 
	/// it has a faster indexer. Both classes provide fast enumeration (O(1) per 
	/// element), but <see cref="DList{T}"/> enumerators initialize faster.
	/// <para/>
	/// Although AList isn't the fastest or smallest data structure for any 
	/// single task, it is very useful when you need several different 
	/// capabilities, and there are certain tasks for which it excels; for 
	/// example, have you ever wanted to remove all items that meet certain 
	/// criteria from a list? You cannot accomplish this with a foreach loop 
	/// such as this:
	/// <para/>
	/// foreach (T item in list)
	///    if (MeetsCriteria(item))
	///       list.Remove(item);
	///       // Exception occurs! foreach loop cannot continue after Remove()!
	/// <para/>
	/// When you are using a <see cref="List{T}"/>, you might try to solve this 
	/// problem with a reverse for-loop such as this:
	/// <para/>
	/// for (int i = list.Count - 1; i >= 0; i--)
	///    if (MeetsCriteria(list[i]))
	///       list.RemoveAt(i);
	/// <para/>
	/// This works, but it runs in O(N^2) time, so it's very slow if the list is 
	/// large. The easiest way to solve this problem that is also efficient is to 
	/// duplicate all the items that you want to keep in a new list:
	/// <para/>
	/// var list2 = new List&lt;T>();
	/// foreach (T item in list)
	///    if (!MeetsCriteria(item))
	///       list2.Add(item);
	/// list = list2;
	/// <para/>
	/// But what if you didn't think of that solution and already wrote the O(N^2)
	/// version? There's a lot of code out there already that relies on slow 
	/// <see cref="List{T}"/> operations. An easy way to solve performance caused
	/// by poor use of <see cref="List{T}"/> is simply to add "A" in front. AList
	/// is pretty much a drop-in replacement for List, so you can convert O(N^2) 
	/// into faster O(N log N) code simply by using an AList instead of a List.
	/// <para/>
	/// I like to think of AList as the ultimate novice data structure. Novices
	/// like indexed lists, although for many tasks they are not the most efficient
	/// choice. AList isn't optimized for any particular task, but it isn't 
	/// downright slow for any task except <c>IndexOf</c>, so it's very 
	/// friendly to novices that don't know about all the different types of data
	/// structures and how to choose one. Don't worry about it! Just pick AList.
	/// It's also a good choice when you're just too busy to think about 
	/// performance, such as in a scripting environment.
	/// <para/>
	/// Plus, you can subscribe to the <see cref="AListBase{K,T}.ListChanging"/> 
	/// event to find out when the list changes. AList's observability is more 
	/// lightweight than that of ObservableCollection{T}.
	/// <para/>
	/// Although single insertions, deletions, and random access require O(log N)
	/// time, you can get better performance using any overload of 
	/// <see cref="InsertRange"/> or <see cref="AListBase{K,T}.RemoveRange"/>.
	/// These methods require only O(log N + M) time, where M is the number of 
	/// elements you are inserting, removing or enumerating.
	/// <para/>
	/// AList is an excellent choice if you need to make occasional snapshots of
	/// the tree. Cloning is fast and memory-efficient, because none of the tree
	/// is copied at first. The root node is simply marked as frozen, and nodes 
	/// are duplicated on-demand as changes are made. Thus, AList can be used as a 
	/// so-called "persistent" data structure, but it is fairly expensive to clone 
	/// the tree after every modification. When modifying a tree that was just 
	/// cloned (remember, AList is really a tree), the leaf node being changed and 
	/// all of its ancestors must be duplicated. Therefore, it's better if you can 
	/// arrange to have a high ratio of changes to clones.
	/// <para/>
	/// AList is also freezable, which is useful if you need to construct a list 
	/// in a read-only or freezable data structure. You could also freeze the list
	/// in order to return a read-only copy of it, which, compared to cloning, has 
	/// the advantage that no memory allocation is required at the time you return 
	/// the list. If you need to edit the list later, you can clone the list (the 
	/// clone can be modified).
	/// <para/>
	/// As explained in the documentation of <see cref="AListBase{T}"/>, this class
	/// is NOT multithread-safe. Multiple concurrent readers are allowed, as long 
	/// as the collection is not modified, so frozen instances ARE multithread-safe.
	/// </remarks>
	/// <seealso cref="BList{T}"/>
	/// <seealso cref="BDictionary{K,V}"/>
	/// <seealso cref="DList{T}"/>
	[Serializable]
	[DebuggerTypeProxy(typeof(ListSourceDebugView<>)), DebuggerDisplay("Count = {Count}")]
	public class AList<T> : AListBase<T>, IListEx<T>, IListRangeMethods<T>, ICloneable<AList<T>>
	{
		#region Constructors

		public AList() { }
		public AList(IEnumerable<T> items) { InsertRange(0, items); }
		public AList(IListSource<T> items) { InsertRange(0, items); }
		public AList(int maxNodeSize) : base(maxNodeSize) { }
		public AList(int maxLeafSize, int maxInnerSize) : base(maxLeafSize, maxInnerSize) { }
		public AList(AList<T> items, bool keepListChangingHandlers) : base(items, keepListChangingHandlers) { }
		protected AList(AListBase<int, T> original, AListNode<int, T> section) : base(original, section) { }
		
		#endregion

		#region General supporting protected methods

		protected override AListNode<int, T> NewRootLeaf()
		{
			return new AListLeaf<T>(_maxLeafSize);
		}
		
		#endregion

		void IAddRange<T>.AddRange(IReadOnlyCollection<T> source) { AddRange(source); }

		#region Add, AddRange, Insert, InsertRange

		public sealed override void Add(T item)
		{
			Insert(Count, item);
		}
		public void AddRange(IEnumerable<T> list)
		{
			InsertRange(Count, list);
		}

		public void InsertRange(int index, AList<T> source) { InsertRange(index, source, false); }
		public void InsertRange(int index, AList<T> source, bool move) { base.InsertRange(index, source, move); }
		void IListRangeMethods<T>.InsertRange(int index, IReadOnlyCollection<T> source) { InsertRange(index, source); }

		public sealed override void Insert(int index, T item)
		{
			if ((uint)index > (uint)_count)
				throw new IndexOutOfRangeException();
			DetectSizeOverflow(1);
			AutoThrow();
			if (_listChanging != null)
				CallListChanging(new ListChangeInfo<T>(NotifyCollectionChangedAction.Add, index, 1, ListExt.Single(item)));

			try {
				_freezeMode = FreezeMode.FrozenForConcurrency;
				if (_root == null || _root.IsFrozen)
					AutoCreateOrCloneRoot();

				AListNode<int, T> splitLeft, splitRight;
				splitLeft = _root.Insert((uint)index, item, out splitRight, _observer);
				if (splitLeft != null) // redundant 'if' optimization
					AutoSplit(splitLeft, splitRight);

				++_version;
				Debug.Assert(_count != int.MaxValue);
				++_count;
				CheckPoint();
			} finally {
				_freezeMode = FreezeMode.NotFrozen;
			}
		}

		public sealed override void InsertRange(int index, IEnumerable<T> list)
		{
			if ((uint)index > (uint)_count)
				throw new IndexOutOfRangeException();

			IListSource<T> source = list as IListSource<T>;
			if (source == null)
				source = new InternalList<T>(list);

			InsertRange(index, source);
		}
		public sealed override void InsertRange(int index, IListSource<T> source)
		{
			int sourceIndex = 0;
			int sourceCount = source.Count;
			BeginInsertRange(index, source, sourceCount);
			try {
				while (sourceIndex < sourceCount)
				{
					AListNode<int, T> splitLeft, splitRight;
					splitLeft = _root.InsertRange((uint)index, source, ref sourceIndex, out splitRight, _observer);
					AutoSplit(splitLeft, splitRight);
				}
			} finally {
				DoneInsertRange(sourceIndex);
			}
		}

		#endregion

		#region Indexer (this[int]), TrySet()

		public sealed override T this[int index]
		{
			get {
				// This benchmarks as slightly faster than `return _root[index]` when the root
				// is a leaf (AListLeaf<T> is sealed); effect on deeper trees is negligible.
				var leaf = _root as AListLeaf<T>;
				return leaf != null ? leaf._list[index] : ((AListInner<T>)_root)[(uint)index];
			}
			set {
				if ((_freezeMode & (FreezeMode)1) != 0) // Frozen or FrozenForConcurrency, but not FrozenForListChanging
					AutoThrow();
				if ((uint)index >= (uint)Count)
					throw new IndexOutOfRangeException();
				SetHelper((uint)index, value);
			}
		}

		public sealed override bool TrySet(int index, T value)
		{
			if (_freezeMode != 0)
			{
				if (_freezeMode == FreezeMode.FrozenForConcurrency)
					AutoThrow();
				if (_freezeMode == FreezeMode.Frozen)
					return false;
			}
			if ((uint)index >= (uint)Count)
				return false;
			SetHelper((uint)index, value);
			return true;
		}
		
		private void SetHelper(uint index, T value)
		{
			if (_listChanging != null)
				CallListChanging(new ListChangeInfo<T>(NotifyCollectionChangedAction.Replace, (int)index, 0, ListExt.Single(value)));
			++_version;
			if (_root.IsFrozen)
				AutoCreateOrCloneRoot();
			_root.SetAt(index, value, _observer);
			CheckPoint();
		}
		
		#endregion

		#region Features delegated to AListBase: Clone, CopySection, RemoveSection, Swap

		protected override void Clone(out AListBase<T> clone)
		{
			clone = Clone();
		}
		public new AList<T> Clone()
		{
			return Clone(false);
		}
		public AList<T> Clone(bool keepListChangingHandlers)
		{
			return new AList<T>(this, keepListChangingHandlers);
		}
		public AList<T> CopySection(int start, int subcount)
		{
			return new AList<T>(this, CopySectionHelper(start, subcount));
		}
		protected override AListBase<T> cov_RemoveSection(int start, int count) { return RemoveSection(start, count); }
		public new AList<T> RemoveSection(int start, int count)
		{
			if ((uint)count > _count - (uint)start)
				throw new ArgumentOutOfRangeException(count < 0 ? "count" : "start+count");
			
			var newList = new AList<T>(this, CopySectionHelper(start, count));
			// bug fix: we must RemoveRange after creating the new list, because 
			// the section is expected to have the same height as the original tree 
			// during the constructor of the new list.
			RemoveRange(start, count);
			return newList;
		}
		/// <summary>Swaps the contents of two <see cref="AList{T}"/>s in O(1) time.</summary>
		/// <remarks>Any observers are also swapped.</remarks>
		public void Swap(AList<T> other)
		{
			base.SwapHelper(other, true);
		}

		#endregion

		#region Bonus features (Append, Prepend)

		/// <inheritdoc cref="Append(AList{T}, bool)"/>
		public virtual void Append(AList<T> other) { Combine(other, false, true); }

		/// <summary>Appends another AList to this list in sublinear time.</summary>
		/// <param name="other">A list of items to be added to this list.</param>
		/// <param name="move">If this parameter is true, items from the other list 
		/// are transferred to this list, causing the other list to be cleared. 
		/// This parameter does not affect the speed of this method itself, but
		/// if you use "true" then future modifications to the combined list may
		/// be faster. If this parameter is "false" then it will be necessary to 
		/// freeze the contents of the other list so that both lists can share
		/// the same tree nodes. Using "true" instead avoids the freeze operation,
		/// which in turn avoids the performance penalty on future modifications.</param>
		/// <remarks>
		/// The default value of the 'move' parameter is false.
		/// <para/>
		/// When the 'source' list is short, this method doesn't perform 
		/// any better than a standard AddRange() operation (in fact, the operation 
		/// is delegated to <see cref="InsertRange"/>()). However, when 'source' 
		/// has several hundred or thousand items, the append/prepend operation is 
		/// performed in roughly O(log N) time where N is the combined list size.
		/// <para/>
		/// Parts of the tree that end up shared between this list and the other 
		/// list will be frozen. Frozen parts of the tree must be cloned in order
		/// to be modified, which will slow down future operations on the tree.
		/// In order to avoid this problem, use move semantics (which clears the
		/// other list).
		/// </remarks>
		public virtual void Append(AList<T> other, bool move) { Combine(other, move, true); }

		/// <summary>Prepends an AList to this list in sublinear time.</summary>
		/// <param name="other">A list of items to be added to the front of this list (at index 0).</param>
		/// <inheritdoc cref="Append(AList{T}, bool)"/>
		public virtual void Prepend(AList<T> other) { Combine(other, false, false); }
		
		/// <summary>Prepends an AList to this list in sublinear time.</summary>
		/// <param name="other">A list of items to be added to the front of this list (at index 0).</param>
		/// <inheritdoc cref="Append(AList{T}, bool)"/>
		public virtual void Prepend(AList<T> other, bool move) { Combine(other, move, false); }

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
		/// <remarks></remarks>
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
			if (_listChanging != null && subcount > 1)
			{
				// Although the entire list might not be changing, a Reset is the
				// only applicable notification, because we can't provide a list of 
				// NewItems as required by Replace.
				CallListChanging(new ListChangeInfo<T>(NotifyCollectionChangedAction.Reset, 0, 0, null));
			}

			Debug.Assert((_treeHeight == 0) == (_root == null));
			if (_root == null)
				return;

			// Ideally we'd set _freezeMode = FrozenForConcurrency here, but we
			// can't do that because SortCore() relies on Enumerator to scan the 
			// list, and Enumerator throws ConcurrentModificationException() when
			// it notices the _freezeMode. We might still detect concurrent 
			// modification on another thread (for the same reason), but we can't
			// detect simultaneous reading on another thread during the sort.
			SortCore(start, subcount, comp);
			++_version;
		}

		protected virtual void SortCore(uint start, uint subcount, Comparison<T> comp)
		{
			if (_treeHeight == 1)
			{
				if (_root.IsFrozen)
					AutoCreateOrCloneRoot();
				var leaf = (AListLeaf<T>)_root;
				leaf.Sort((int)start, (int)subcount, comp);
			}
			else
			{
				// The quicksort algorithm requires pre-thawed nodes
				ForceThaw(start, subcount);

				if (_observer != null) {
					var e = new Enumerator(this, start-1, start, start+subcount);
					while (e.MoveNext())
						_observer.ItemRemoved(e.Current, (AListLeaf<int,T>)e._leaf);
				}

				TreeSort(start, subcount, comp);

				if (_observer != null) {
					var e = new Enumerator(this, start-1, start, start+subcount);
					while (e.MoveNext())
						_observer.ItemAdded(e.Current, (AListLeaf<int,T>)e._leaf);
					CheckPoint();
				}
			}
		}

		protected void ForceThaw(uint start, uint subcount)
		{
			if (_root == null || subcount == 0)
				return;

			var e = new Enumerator(this, start-1, start, subcount);
			while (e.MoveNext())
			{
				if (e._leaf.IsFrozen)
					e.UnfreezeCurrentLeaf();
				
				// move to the end of this leaf
				int advancement = e._leaf.LocalCount - e._leafIndex - 1;
				e._leafIndex += advancement;
				e._currentIndex += (uint)advancement;
			}
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

				var e = new Enumerator(this, start-1, start, start+count);
				Verify(e.MoveNext());
				if (count <= e._leaf.LocalCount - e._leafIndex) {
					// Do fast sort inside leaf
					if (e._leaf.IsFrozen)
						e.UnfreezeCurrentLeaf();
					((AListLeaf<int,T>)e._leaf).Sort(e._leafIndex, (int)count, comp);
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
					G.Swap(ref pivot0, ref pivot1);
					G.Swap(ref offset0, ref offset1);
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
				_root.SetAt(start + offset1, eBegin.Current, _observer);
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
				if (order < 0 || (order == 0 && (eOut.LastIndex - eOut._currentIndex) > (count >> 1) && (swapEqual = !swapEqual)))
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
			uint rightSize = eOut.LastIndex - eOut._currentIndex - 1;
			uint leftSize = eOut._currentIndex - eBegin._currentIndex;
			Debug.Assert(leftSize + rightSize + 1 == count);
			if (rightSize > (count >> 1))
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

			T[] temp = new T[e.LastIndex - e._currentIndex];
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
			G.SortPair(ref a, ref b, comp);
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

	/// <summary>
	/// Common base class of <see cref="AList{T}"/> and <see cref="SparseAList{T}"/>.
	/// Most of the functionality of the two types is identical, so this class is 
	/// used to share code between them.
	/// </summary>
	/// <remarks>
	/// This class exists for code sharing only. Clients should ignore it.
	/// <para/>
	/// The difference between <see cref="AListBase{K,T}"/> and <see cref="AListBase{T}"/>
	/// is that the first one is the base class of all data structures in the A-List 
	/// family (including <see cref="BList{T}"/>, <see cref="BDictionary{K,V}"/>, 
	/// etc.) while the second one is only the base class of non-organized ALists 
	/// (<see cref="AList{T}"/> and <see cref="SparseAList{T}"/>).
	/// </remarks>
	[Serializable]
	[DebuggerTypeProxy(typeof(ListSourceDebugView<>)), DebuggerDisplay("Count = {Count}")]
	public abstract class AListBase<T> : AListBase<int, T>, IListAndListSource<T>, ICloneable<AListBase<T>>, IDeque<T>
	{
		#region Constructors

		public AListBase() { }
		public AListBase(int maxNodeSize) : base(maxNodeSize) { }
		public AListBase(int maxLeafSize, int maxInnerSize) : base(maxLeafSize, maxInnerSize) { }
		public AListBase(AListBase<T> items, bool keepListChangingHandlers) : base(items, keepListChangingHandlers) { }
		protected AListBase(AListBase<int, T> original, AListNode<int, T> section) : base(original, section) { }

		#endregion

		#region Properties

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		new public T First
		{
			get { return this[0]; }
			set { this[0] = value; }
		}
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		new public T Last
		{
			get { return base.Last; }
			set { this[Count - 1] = value; }
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool ICollection<T>.IsReadOnly => IsFrozen;

		#endregion

		#region General supporting protected methods

		protected override AListInnerBase<int, T> SplitRoot(AListNode<int, T> left, AListNode<int, T> right)
		{
			return new AListInner<T>(left, right, _maxInnerSize);
		}
		protected internal override int GetKey(T item)
		{
			throw new NotSupportedException();
		}
		/// <summary>Throws <see cref="OverflowException"/> if inserting the 
		/// specified number of items would cause Count to overflow.</summary>
		protected void DetectSizeOverflow(int insertSize)
		{
			checked { insertSize += Count; };
		}
		
		#endregion

		public abstract void Add(T item);
		public abstract void Insert(int index, T item);
		public abstract void InsertRange(int index, IListSource<T> source);
		public new abstract T this[int index] { get; set; }
		public abstract bool TrySet(int index, T value);

		// Workaround for a design flaw in C#, see http://stackoverflow.com/questions/3674368/overload-resolution-and-virtual-methods/3677779#3677779
		// Without this method, it is impossible to call InsertRange(IListSource)
		// in the derived class.
		public abstract void InsertRange(int index, IEnumerable<T> list);

		#region Helper methods for InsertRange(), Append() and Prepend()

		protected void BeginInsertRange(int index, IListSource<T> items, int itemsCount)
		{
			if ((uint)index > (uint)_count)
				throw new IndexOutOfRangeException();
			DetectSizeOverflow(itemsCount);
			
			AutoThrow();
			if (_listChanging != null)
				CallListChanging(new ListChangeInfo<T>(NotifyCollectionChangedAction.Add, index, itemsCount, items));

			_freezeMode = FreezeMode.FrozenForConcurrency;
			AutoCreateOrCloneRoot();
		}

		protected void DoneInsertRange(int amountInserted)
		{
			if (amountInserted != 0)
				++_version;
			_freezeMode = FreezeMode.NotFrozen;
			checked { _count += (uint)amountInserted; };
			CheckPoint();
		}

		protected void InsertRange(int index, AListBase<T> source, bool move)
		{
			if (source._root.IsLeaf || source._maxLeafSize != _maxLeafSize) {
				InsertRange(index, (IListSource<T>)source);
				if (move)
					source.Clear();
			} else {
				AListBase<T> rightSection = null;
				int rightSize;
				if ((rightSize = Count - index) != 0)
					rightSection = RemoveSection(index, rightSize);
				Combine(source, move, true);
				if (rightSection != null)
					Combine(rightSection, true, true);
			}
		}

		#endregion

		#region Add, AddRange, Resize

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
				InsertRange(Count, new Repeated<T>(default(T), newSize - Count));
		}
		
		#endregion

		#region IndexOf, Contains, Remove, RemoveAll

		/// <summary>Finds an index of an item in the list.</summary>
		/// <param name="item">An item for which to search.</param>
		/// <returns>An index of the item.</returns>
		/// <remarks>
		/// The default implementation simply calls <see cref="AListBase{K,T}.LinearScanFor"/>.
		/// This method is called by <see cref="Remove"/> and <see cref="Contains"/>.
		/// </remarks>
		public virtual int IndexOf(T item)
		{
			return FirstIndexOf(item, 0, EqualityComparer<T>.Default) ?? -1;
		}

		/// <summary>Returns true if-and-only-if the specified item exists in the list.</summary>
		public bool Contains(T item)
		{
			return IndexOf(item) > -1;
		}

		/// <summary>Finds a specific item and removes it. If duplicates of the item exist, 
		/// only the first occurrence is removed.</summary>
		/// <returns>True if an item was removed, false if not.</returns>
		public bool Remove(T item)
		{
			int index = IndexOf(item);
			if (index <= -1)
				return false;
			RemoveAt(index);
			return true;
		}

		#endregion

		protected abstract void Clone(out AListBase<T> clone);
		public AListBase<T> Clone()
		{
			AListBase<T> clone;
			Clone(out clone);
			return clone;
		}

		protected abstract AListBase<T> cov_RemoveSection(int start, int count);
		public AListBase<T> RemoveSection(int start, int count)
		{
			return cov_RemoveSection(start, count);
		}

		public new ListSlice<T> Slice(int start, int length)
		{
			return new ListSlice<T>(this, start, length);
		}

		#region Combine: helper method for Append, Prepend

		protected virtual void Combine(AListBase<T> other, bool move, bool append)
		{
			int heightDifference = _treeHeight - other._treeHeight;
			int insertAt = append ? Count : 0;
			
			if (!(other._root is AListInner<T>))
				goto insertRange;
			else if (heightDifference < 0)
			{
				// The other tree is taller (bigger). We can only append/prepend a smaller
				// tree; therefore, swap the trees and then append/prepend the smaller one.
				// With the tree contents swapped, the notifications to ListChanging
				// must be fudged. If we have a tree _observer, the situation is too 
				// complex and unusual to handle, so we fall back on InsertRange().
				if (_observer != null || (other._observer != null && move))
					goto insertRange;
				
				AListBase<T> other2 = other;
				if (!move)
					other.Clone(out other2);

				// Fire ListChanging on both lists, and block further notifications
				var temp = _listChanging;
				var tempO = other._listChanging;
				Exception e = null;
				if (temp != null)
					CallListChanging(new ListChangeInfo<T>(NotifyCollectionChangedAction.Add, insertAt, other.Count, other));
				if (tempO != null) {
					try {
						other.CallListChanging(new ListChangeInfo<T>(NotifyCollectionChangedAction.Remove, 0, -other.Count, null));
					} catch(Exception e_) {
						// Ugh. We already notified the first list about the insert, 
						// so it is too late to abort. Finish the operation and 
						// throw the exception afterward.
						e = e_;
					}
				}

				try {
					_listChanging = null;
					other._listChanging = null;
					other2.Combine(this, move, !append);
				} finally {
					_listChanging = temp;
					other._listChanging = tempO;
				}
				base.SwapHelper(other2, false);
				
				if (e != null)
					throw e;
			}
			else
			{	// other tree is the same height or less tall
				BeginInsertRange(insertAt, other, other.Count);
				int amtInserted = 0;
				try {
					AListNode<int, T> splitLeft, splitRight;
					splitLeft = ((AListInner<T>)_root).Combine((AListInner<T>)other._root, heightDifference, out splitRight, _observer, move, append);
					amtInserted = other.Count;
					if (move)
						other.ClearInternal(true);
					AutoSplit(splitLeft, splitRight);
				}
				finally
				{
					DoneInsertRange(amtInserted);
				}
			}
			return;
		
		insertRange:
			InsertRange(insertAt, (IListSource<T>)other);
			if (move)
				other.ClearInternal(true);
		}

		#endregion

		#region IDeque<T>
		// implemented explicitly because it's all just aliases for existing functionality

		void IDeque<T>.PushFirst(T item)
		{
			Insert(0, item);
		}

		void IDeque<T>.PushLast(T item)
		{
			Insert(Count, item);
		}

		Maybe<T> IDeque<T>.TryPopFirst()
		{
			bool isEmpty;
			var result = TryGet(0, out isEmpty);
			if (isEmpty)
				return Maybe<T>.NoValue;
			RemoveAt(0);
			return result;
		}

		Maybe<T> IDeque<T>.TryPeekFirst()
		{
			return this.TryGet(0);
		}

		Maybe<T> IDeque<T>.TryPopLast()
		{
			bool isEmpty;
			var result = TryGet(Count - 1, out isEmpty);
			if (isEmpty)
				return Maybe<T>.NoValue;
			RemoveAt(Count-1);
			return result;
		}

		Maybe<T> IDeque<T>.TryPeekLast()
		{
			return this.TryGet(Count-1);
		}
		
		#endregion
	}
}
