using Loyc.Collections.Impl;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>Encapsulates algorithms for a max-heap, i.e. a priority queue 
	/// that always knows the largest item and can remove it in O(log Count) time,
	/// or add a new item in O(log Count) time.
	/// </summary>
	/// <typeparam name="T">Item type</typeparam>
	/// <typeparam name="TList">List type that the heap object is a wrapper for</typeparam>
	/// <typeparam name="TComparer">Type used to compare items; an instance of this type 
	/// must be provided to the constructor.</typeparam>
	/// <remarks>
	/// Call Push(T) to add a new item, Peek() to get the largest item, 
	/// and Pop() or TryPop(out bool) to remove the largest item.
	/// <para/>
	/// Most users will want to use the derived classes <see cref="MaxHeap{T}"/> 
	/// or <see cref="MaxHeapInList{T}"/> to avoid dealing with three type parameters.
	/// Ideally this type would be a struct in order to maximize performance. 
	/// It is a class in order to allow derived classes including <see cref="MinHeap{T}"/>.
	/// </remarks>
	/// <seealso cref="MaxHeap{T}"/>
	public class MaxHeap<T, TList, TComparer> : IPriorityQueue<T>, IEnumerable<T>
		where TList : IList<T> where TComparer : IComparer<T>
	{
		TList _list;
		/// <summary>Returns the underlying list that represents the binary heap.</summary>
		public TList List { get { return _list; } }

		TComparer _cmp;
		public TComparer Comparer { get { return _cmp; } }

		/// <summary>This optional callback is called whenever an item is placed 
		/// into the heap, removed from the heap, or moved within the <see cref="List"/>
		/// that holds the contents of the heap. The callback is useful for certain
		/// algorithms, such as the Dijkstra algorithm, in which an object's priority 
		/// may need to change while it is inside the heap.
		/// </summary><remarks>
		/// In order to change an object's priority, the object must contain mutable
		/// state that represents its priority. After changing the priority, you must
		/// call <see cref="PriorityChanged"/> to inform the heap that the priority has 
		/// changed. That method takes the current position of the object in the
		/// <see cref="List"/> as an argument. In order to keep track of the current 
		/// position you must handle the OnItemMoved event and store the new position
		/// somewhere, such as inside the object itself.
		/// <para/>
		/// The parameters to OnItemMoved are the object that has moved and the new
		/// index of the item within the <see cref="List"/>. If the item has been
		/// removed from the list, the index is -1.
		/// </remarks>
		public event Action<T, int> OnItemMoved;

		/// <summary>Initializes the heap wrapper with the list and comparer to use.
		/// Both parameters must not be null.</summary>
		/// <param name="onItemMoved">This optional callback is called whenever an item 
		/// is placed or relocated within the heap (see <see cref="OnItemMoved"/>.)</param>
		/// <remarks>If the list is not already arranged as a max-heap, you must call
		/// Heapify() after the constructor to rearrange it into a heap.</remarks>
		public MaxHeap(TList list, TComparer comparer, Action<T, int> onItemMoved = null) {
			_list = list;
			_cmp = comparer;
			OnItemMoved = onItemMoved;
		}

		/// <summary>Rearranges items to ensure that the underlying list has the heap 
		/// property. Takes O(Count) time.</summary>
		/// <returns>this.</returns>
		public MaxHeap<T, TList, TComparer> Heapify()
		{
			int count = _list.Count;
			for (int i = (count - 2) >> 1; i >= 0; i--)
				BubbleDown(_list[i], i, count);
			return this;
		}

		public bool IsEmpty { get { return _list.Count == 0; } }
		public int Count    { get { return _list.Count; } }

		/// <summary>Adds an item to the heap (synonym of Push()). Complexity: O(Count).</summary>
		public void Add(T item)
		{
			Push(item);
		}

		/// <summary>Adds an item to the heap. Complexity: O(Count).</summary>
		public void Push(T item)
		{
			int index = _list.Count;
			_list.Add(item);
			BubbleUp(index, item);
		}

		// Bubble-up / up-heap / percolate-up / sift-up operation
		private int BubbleUp(int index, T item)
		{
			T parent;
			int iParent = (index - 1) >> 1;
			while (index > 0 && _cmp.Compare(item, parent = _list[iParent]) > 0) {
				_list[index] = parent;
				OnItemMoved?.Invoke(parent, index);
				index = iParent;
				iParent = (iParent - 1) >> 1;
			}
			_list[index] = item;
			OnItemMoved?.Invoke(item, index);
			return index;
		}

		/// <summary>Notifies the heap that the priority of the item at the specified 
		/// index has changed. The item is bubbled up or down as appropriate.</summary>
		/// <returns>The new index of the same item.</returns>
		public void PriorityChanged(int index)
		{
			T item = _list[index];
			int i = BubbleUp(index, item);
			if (i == index)
				BubbleDown(item, index, _list.Count);
		}

		/// <summary>Combines a pop followed by a push into one operation that is more 
		/// efficient than a separate <see cref="Pop"/> nad <see cref="Push(T)"/>.</summary>
		public T PopAndPush(T item)
		{
			T result = _list[0];
			OnItemMoved?.Invoke(result, -1);
			BubbleDown(item, 0, _list.Count);
			return result;
		}

		// Bubble-down / down-heap / percolate-down / sift-down operation
		private void BubbleDown(T item, int i, int count)
		{
			for (int ichild = i * 2 + 1, cmp; ichild < count; ichild = i * 2 + 1) {
				T c1 = _list[ichild], c2, biggerChild = c1;
				if (ichild + 1 < count) {
					c2 = _list[ichild + 1];
					cmp = _cmp.Compare(c1, c2);
					if (cmp < 0) {
						biggerChild = c2;
						ichild++;
					}
				}
				if (_cmp.Compare(item, biggerChild) >= 0)
					break;
				_list[i] = biggerChild;
				OnItemMoved?.Invoke(biggerChild, i);
				i = ichild;
			}
			_list[i] = item;
			OnItemMoved?.Invoke(item, i);
		}

		/// <summary>Removes the largest item from the heap (or smallest item, if this is a MinHeap).</summary>
		/// <param name="isEmpty">Set to true if the heap is empty, false if not.</param>
		/// <returns>The popped value, or default(T) if the List was empty.</returns>
		public T TryPop(out bool isEmpty)
		{
			int count = _list.Count - 1;
			if (isEmpty = count < 0)
				return default(T);
			var result = _list[0];
			BubbleDown(_list[count], 0, count);
			_list.RemoveAt(count);
			OnItemMoved?.Invoke(result, -1);
			return result;
		}

		/// <summary>Removes the largest item from the heap (or smallest item, if this is a MinHeap).</summary>
		/// <exception cref="EmptySequenceException">Thrown if the List is empty.</exception>
		public T Pop()
		{
			int count = _list.Count - 1;
			if (count < 0)
				throw new EmptySequenceException();
			var result = _list[0];
			BubbleDown(_list[count], 0, count);
			_list.RemoveAt(count);
			OnItemMoved?.Invoke(result, -1);
			return result;
		}

		/// <summary>Gets the largest item from the heap if it is not empty 
		/// (or the smallest item, if this is a MinHeap).</summary>
		/// <param name="isEmpty">Set to true if the heap is empty, false if not.</param>
		/// <returns>The popped value, or default(T) if the List was empty.</returns>
		public T TryPeek(out bool isEmpty)
		{
			if (!(isEmpty = _list.Count == 0))
				return _list[0];
			return default(T);
		}

		/// <summary>Gets the largest item from the heap
		/// (or the smallest item, if this is a MinHeap).</summary>
		/// <exception cref="EmptySequenceException">Thrown if the List is empty.</exception>
		public T Peek()
		{
			try {
				return _list[0];
			} catch {
				throw new EmptySequenceException();
			}
		}

		// Enumerator functions are required in C# to allow "new MaxHeap<T> { ... }"
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return List.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return List.GetEnumerator();
		}
	}

	/// <summary>Encapsulates algorithms for a min-heap, i.e. a priority queue 
	/// that always knows the smallest item and can remove it in O(log Count) time,
	/// or add a new item in O(log Count) time.
	/// </summary>
	/// <typeparam name="T">Item type</typeparam>
	/// <typeparam name="TList">List type that the heap object is a wrapper for</typeparam>
	/// <typeparam name="TComparer">Type used to compare items; an instance of this type 
	/// must be provided to the constructor.</typeparam>
	/// <remarks>
	/// Call Push(T) to add a new item, Peek() to get the smallest item, 
	/// and Pop() or TryPop(out bool) to remove the smallest item.
	/// <para/>
	/// Most users will want to use the derived classes <see cref="MinHeap{T}"/> 
	/// or <see cref="MinHeapInList{T}"/> to avoid dealing with three type parameters.
	/// Ideally this type would be a struct in order to maximize performance. 
	/// It is a class so that it can re-use the code in <see cref="MaxHeap{T, TList, TComparer}"/>.
	/// </remarks>
	/// <seealso cref="MinHeap{T}"/>
	public class MinHeap<T, TList, TComparer> : MaxHeap<T, TList, ReverseComparer<T, TComparer>>
		where TList : IList<T> where TComparer : IComparer<T>
	{
		public MinHeap(TList list, TComparer comparer, Action<T, int> onItemMoved = null) 
			: base(list, new ReverseComparer<T, TComparer>(comparer), onItemMoved) { }
	}

	/// <summary>This is a max-heap, i.e. a priority queue that always knows the 
	/// smallest item and can remove it in O(log Count) time, or add a new item 
	/// in O(log Count) time. The ToMaxHeap extension method returns this type.</summary>
	/// <remarks>
	/// Call Push(T) to add a new item, Peek() to get the smallest item, 
	/// and Pop() or TryPop(out bool) to remove the smallest item.
	/// </remarks>
	public class MaxHeap<T> : MaxHeap<T, InternalList<T>, IComparer<T>>
	{
		public MaxHeap(IComparer<T> comparer = null, Action<T, int> onItemMoved = null)
			: base(InternalList<T>.Empty, comparer ?? Comparer<T>.Default, onItemMoved) { }
		public MaxHeap(IEnumerable<T> items, IComparer<T> comparer = null, bool heapify = true)
			: base(new InternalList<T>(items), comparer ?? Comparer<T>.Default)
		{
			if (heapify) Heapify();
		}
		public MaxHeap(InternalList<T> list, IComparer<T> comparer = null, bool heapify = true)
			: base(list, comparer ?? Comparer<T>.Default)
		{
			if (heapify) Heapify();
		}
	}

	/// <summary>This is a min-heap, i.e. a priority queue that always knows the 
	/// smallest item and can remove it in O(log Count) time, or add a new item 
	/// in O(log Count) time. The ToMinHeap extension method returns this type.</summary>
	/// <remarks>
	/// Call Push(T) to add a new item, Peek() to get the smallest item, 
	/// and Pop() or TryPop(out bool) to remove the smallest item.
	/// </remarks>
	public class MinHeap<T> : MinHeap<T, InternalList<T>, IComparer<T>>
	{
		public MinHeap(IComparer<T> comparer = null, Action<T, int> onItemMoved = null) 
			: base(InternalList<T>.Empty, comparer ?? Comparer<T>.Default, onItemMoved) { }
		public MinHeap(IEnumerable<T> items, IComparer<T> comparer = null, bool heapify = true) 
			: base(new InternalList<T>(items), comparer ?? Comparer<T>.Default)
		{
			if (heapify) Heapify();
		}
		public MinHeap(InternalList<T> list, IComparer<T> comparer = null, bool heapify = true) 
			: base(list, comparer ?? Comparer<T>.Default)
		{
			if (heapify) Heapify();
		}
	}

	/// <summary>This priority queue wrapper type is returned from the AsMaxHeap() extension method.</summary>
	public class MaxHeapInList<T> : MaxHeap<T, IList<T>, IComparer<T>>
	{
		public MaxHeapInList(IList<T> list = null, IComparer<T> comparer = null, Action<T, int> onItemMoved = null)
			: base(list ?? new List<T>(), comparer ?? Comparer<T>.Default, onItemMoved) { }
	}
	
	/// <summary>This priority queue wrapper type is returned from the AsMinHeap() extension method.</summary>
	public class MinHeapInList<T> : MinHeap<T, IList<T>, IComparer<T>>
	{
		public MinHeapInList(IList<T> list = null, IComparer<T> comparer = null, Action<T, int> onItemMoved = null)
			: base(list ?? new List<T>(), comparer ?? Comparer<T>.Default, onItemMoved) { }
	}

	public static class Heap
	{
		/// <summary>Returns a MaxHeapInList wrapper object, which treats the
		/// list as a binary max-heap (priority queue). This function assumes
		/// that the given list already represents a binary max-heap in which 
		/// the child nodes of list[x] are list[x*2+1] and list[x*2+2]. If this
		/// is not the case, you can either use ToMaxHeap() to make a copy with
		/// the heap property, or call Heapify() on the result of this function.</summary>
		/// <param name="compare">An object to be used to compare T values.</param>
		/// <param name="heapify">Whether to rearrange items so that they have the heap property.</param>
		public static MaxHeapInList<T> AsMaxHeap<T>(this IList<T> list, IComparer<T> compare = null, bool heapify = false)
		{
			var heap = new MaxHeapInList<T>(list, compare);
			if (heapify)
				heap.Heapify();
			return heap;
		}

		/// <summary>Copies the list into a new object with the items arranged
		/// as a max-heap (priority queue). Complexity: O(N).</summary>
		/// <param name="compare">An object to be used to compare T values.</param>
		/// <param name="heapify">Whether to rearrange items so that they have
		/// the heap property. If the items are already arranged with the heap 
		/// property, you can skip heapification by setting this to false.</param>
		public static MaxHeap<T> ToMaxHeap<T>(this IList<T> list, IComparer<T> compare = null, bool heapify = true)
		{
			return new MaxHeap<T>(list, compare, heapify);
		}

		/// <summary>Returns a MinHeapInList wrapper object, which treats the
		/// list as a binary min-heap (priority queue). This function assumes
		/// that the given list already represents a binary min-heap in which 
		/// the child nodes of list[x] are list[x*2+1] and list[x*2+2]. If this
		/// is not true, you can either use ToMinHeap() to make a copy with
		/// the heap property, or call Heapify() on the result of this function.</summary>
		/// <param name="compare">An object to be used to compare T values.</param>
		/// <param name="heapify">Whether to rearrange items so that they have the heap property.</param>
		public static MinHeapInList<T> AsMinHeap<T>(this IList<T> list, IComparer<T> compare = null, bool heapify = false)
		{
			var heap = new MinHeapInList<T>(list, compare);
			if (heapify)
				heap.Heapify();
			return heap;
		}

		/// <summary>Copies the list into a new object with the items arranged
		/// as a min-heap (priority queue). Complexity: O(N).</summary>
		/// <param name="compare">An object to be used to compare T values.</param>
		/// <param name="heapify">Whether to rearrange items so that they have
		/// the heap property. If the items are already arranged with the heap 
		/// property, you can skip heapification by setting this to false.</param>
		public static MinHeap<T> ToMinHeap<T>(this IList<T> list, IComparer<T> compare = null, bool heapify = true)
		{
			return new MinHeap<T>(list, compare, heapify);
		}
	}
}
