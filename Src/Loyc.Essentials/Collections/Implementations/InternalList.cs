// Came from Loyc. Licence: LGPL
namespace Loyc.Collections.Impl
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Diagnostics;
	using System.Linq;
	using Loyc.Math;

	/// <summary>A compact auto-enlarging array structure that is intended to be 
	/// used within other data structures. It should only be used internally in
	/// "private" or "protected" members of low-level code.
	/// </summary>
	/// <remarks>
	/// InternalList is a struct, not a class, in order to save memory; and for 
	/// maximum performance, it asserts rather than throwing an exception 
	/// when an incorrect array index is used. Besides that, it has an 
	/// InternalArray property that provides access to the internal array. 
	/// For all these reasons one should not expose it in a public API, and 
	/// it should only be used when performance trumps all other concerns.
	/// <para/>
	/// Passing this structure by value is dangerous because changes to a copy 
	/// of the structure may or may not be reflected in the original list. It's
	/// best not to pass it around at all, but if you must pass it, pass it by
	/// reference.
	/// <para/>
	/// Also, do not use the default contructor. Always specify an initial 
	/// capacity or copy InternalList.Empty so that _array gets a value. 
	/// This is required because methods such as Add(), Insert() and Resize() 
	/// assume _array is not null.
	/// <para/>
	/// InternalList has one nice thing that List(of T) lacks: a <see cref="Resize"/>
	/// method and an equivalent Count setter. Which dork at Microsoft decided no 
	/// one should be allowed to set the list length directly? This type also 
	/// provides a handy <see cref="Last"/> property and a <see cref="Pop"/> 
	/// method to respectively get or remove the last item.
	/// <para/>
	/// Finally, alongside InternalList(T), the static class InternalList comes 
	/// with some static methods (CopyToNewArray, Insert, RemoveAt, Move) to help
	/// manage raw arrays. You might want to use these in a data structure 
	/// implementation even if you choose not to use InternalList(T) instances.
	/// </remarks>
	[Serializable]
	public struct InternalList<T> : IListAndListSource<T>, IListRangeMethods<T>, ICloneable<InternalList<T>>//, IGetIteratorSlice<T>
	{
		public static readonly T[] EmptyArray = new T[0];
		public static readonly InternalList<T> Empty = new InternalList<T>(0);
		private T[] _array;
		private int _count;
		public const int BaseCapacity = 4;

		public InternalList(int capacity)
		{
			_count = 0;
			_array = capacity != 0 ? new T[capacity] : EmptyArray;
		}
		public InternalList(T[] array, int count)
		{
			_array = array;
			_count = count;
		}
		public InternalList(IEnumerable<T> items) : this(items.GetEnumerator()) { }
		public InternalList(IEnumerator<T> items)
		{
			_count = 0;
			_array = EmptyArray;
			AddRange(items);
		}

		public int Count
		{
			[DebuggerStepThrough]
			get { return _count; }
			set { Resize(value); }
		}

		public bool IsEmpty
		{
			[DebuggerStepThrough]
			get { return _count == 0; }
		}

		/// <summary>Gets or sets the array length.</summary>
		/// <remarks>Changing this property requires O(Count) time and temporary 
		/// space. Attempting to set the capacity lower than Count has no effect.
		/// </remarks>
		public int Capacity
		{
			[DebuggerStepThrough]
			get { return _array.Length; }
			set {
				if (_array.Length != value && value >= _count)
					_array = InternalList.CopyToNewArray(_array, _count, value);
			}
		}


		public void AutoRaiseCapacity(int more, int capacityLimit)
		{
			var array = InternalList.AutoRaiseCapacity(_array, _count, more, capacityLimit);
			if (_array != array)
				_array = array;
		}

		private void IncreaseCapacity()
		{
			// 4, 8, 14, 22, 34, 52, 80...
			Capacity = InternalList.NextLargerSize(_array.Length);
		}

		/// <summary>Makes the list larger or smaller, depending on whether 
		/// <c>newSize</c> is larger or smaller than <see cref="Count"/>.</summary>
		/// <param name="allowReduceCapacity">If this is true, and the new size is 
		/// smaller than one quarter the current <see cref="Capacity"/>, the array
		/// is reallocated to a smaller size. If this parameter is false, the array 
		/// is never reallocated when shrinking the list.</param>
		/// <param name="newSize">New value of <see cref="Count"/>. If the Count
		/// increases, copies of default(T) are added to the end of the the list; 
		/// otherwise items are removed from the end of the list.</param>
		public void Resize(int newSize) { Resize(newSize, true); }
		/// <inheritdoc cref="Resize(int)"/>
		public void Resize(int newSize, bool allowReduceCapacity)
		{
			if (newSize > _count)
			{
				if (newSize > _array.Length)
				{
					if (newSize <= _array.Length + (_array.Length >> 2)) {
						IncreaseCapacity();
						Debug.Assert(Capacity > newSize);
					} else
						Capacity = newSize;
				}
				_count = newSize;
			}
			else if (newSize < _count)
			{
				if (allowReduceCapacity && newSize < (_array.Length >> 2)) {
					_count = newSize;
					Capacity = newSize;
				} else {
					for (int i = newSize; i < _count; i++)
						_array[i] = default(T);
					_count = newSize;
				}
			}
		}
		
		public void Insert(int index, T item)
		{
			_array = InternalList.Insert(index, item, _array, _count);
			_count++;
		}
		public void InsertRange(int index, ICollectionAndReadOnly<T> items)
		{
			InsertRange(index, items, ((IReadOnlyCollection<T>)items).Count);
		}
		public void InsertRange(int index, IReadOnlyCollection<T> items)
		{
			InsertRange(index, items, items.Count);
		}
		public void InsertRange(int index, ICollection<T> items)
		{
			InsertRange(index, items, items.Count);
		}
		private void InsertRange(int index, IEnumerable<T> items, int count)
		{
			_array = InternalList.InsertRangeHelper(index, count, _array, _count);
			_count += count;
			
			int stop = index + count;
			foreach (var item in items)
			{
				if (index >= stop)
					InsertRangeSizeMismatch();
				_array[index++] = item;
			}
			if (index < stop)
				InsertRangeSizeMismatch();
		}
		public void InsertRange(int index, IEnumerable<T> e)
		{
			var s = e as IReadOnlyCollection<T>;
			if (s != null)
				InsertRange(index, s);
			var c = e as ICollection<T>;
			if (c != null)
				InsertRange(index, c);
			else
				InsertRange(index, (ICollection<T>)new List<T>(e));
		}

		public void AddRange(IReadOnlyCollection<T> items)
		{
			InsertRange(_count, items);
		}
		public void AddRange(ICollection<T> items)
		{
			InsertRange(_count, items);
		}
		public void AddRange(IEnumerable<T> e)
		{
			foreach (T item in e)
				Insert(_count, item);
		}
		public void AddRange(ICollectionAndReadOnly<T> items)
		{
			InsertRange(_count, (IReadOnlyCollection<T>)items);
		}

		private void InsertRangeSizeMismatch()
		{
			throw new ArgumentException("InsertRange: Input collection's Count is different from the number of items enumerated");
		}

		public void Add(T item)
		{
			if (_count == _array.Length)
				IncreaseCapacity();
			_array[_count++] = item;
		}
		public void AddRange(IEnumerator<T> items)
		{
			while (items.MoveNext())
				Add(items.Current);
		}

		/// <summary>Clears the list and frees the memory used by the list. Can 
		/// also be used to initialize a list whose constructor was never called.</summary>
		public void Clear()
		{
			_count = 0;
			_array = EmptyArray;
		}

		public void RemoveAt(int index)
		{
			_count = InternalList.RemoveAt(index, _array, _count);
		}
		public void RemoveRange(int index, int count)
		{
			_count = InternalList.RemoveAt(index, count, _array, _count);
		}

        public T this[int index]
		{
			[DebuggerStepThrough]
			get { 
				Debug.Assert((uint)index < (uint)_count);
				return _array[index];
			}
			set {
				Debug.Assert((uint)index < (uint)_count);
				_array[index] = value;
			}
		}

		public T First
		{
			get { return _array[0]; }
			set { _array[0] = value; }
		}
		public T Last
		{
			get {
				return _array[_count - 1];
			}
			set {
				_array[_count - 1] = value;
			}
		}
		public void Pop()
		{
			_array[_count - 1] = default(T);
			_count--;
		}

		/// <summary>Makes a copy of the list with the same capacity</summary>
		public InternalList<T> Clone()
		{
			return new InternalList<T>(InternalList.CopyToNewArray(_array, _count, _array.Length), _count);
		}
		/// <summary>Makes a copy of the list with Capacity = Count</summary>
		public InternalList<T> CloneAndTrim()
		{
			return new InternalList<T>(InternalList.CopyToNewArray(_array, _count, _count), _count);
		}
		/// <summary>Makes a copy of the list, as an array</summary>
		public T[] ToArray()
		{
			return InternalList.CopyToNewArray(_array, _count, _count);
		}

		public int BinarySearch(T lookFor)
		{
			return InternalList.BinarySearch(_array, _count, lookFor, Comparer<T>.Default, false);
		}
		public int BinarySearch(T lookFor, Comparer<T> comp)
		{
			return InternalList.BinarySearch(_array, _count, lookFor, comp, false);
		}
		public int BinarySearch(T lookFor, Comparer<T> comp, bool lowerBound)
		{
			return InternalList.BinarySearch(_array, _count, lookFor, comp, lowerBound);
		}
		public int BinarySearch<K>(K lookFor, Func<T, K, int> func, bool lowerBound)
		{
			return InternalList.BinarySearch(_array, _count, lookFor, func, lowerBound);
		}

		/// <summary>Slides the array entry at [from] forward or backward in the
		/// list, until it reaches [to].</summary>
		/// <remarks>
		/// For example, if a list of integers is [0, 1, 2, 3, 4, 5] then Move(4,1)
		/// produces the following result: [0, 4, 1, 2, 3, 5].
		/// </remarks>
		public void Move(int from, int to)
		{
			Debug.Assert((uint)from < (uint)_count);
			Debug.Assert((uint)to < (uint)_count);
			InternalList.Move(_array, from, to);
		}

		#region Boilerplate

		public int IndexOf(T item) { return IndexOf(item, 0); }
		public int IndexOf(T item, int index)
		{
			EqualityComparer<T> comparer = EqualityComparer<T>.Default;
			for (; index < Count; index++)
				if (comparer.Equals(this[index], item))
					return index;
			return -1;
		}
		public bool Contains(T item)
		{
			return IndexOf(item) != -1;
		}
		public void CopyTo(T[] array, int arrayIndex)
		{
			Array.Copy(_array, 0, array, arrayIndex, _count);
		}
		public bool IsReadOnly
		{
			get { return false; }
		}
		public bool Remove(T item)
		{
			int i = IndexOf(item);
			if (i == -1)
				return false;
			RemoveAt(i);
			return true;
		}
		System.Collections.IEnumerator
				System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 0; i < Count; i++)
				yield return this[i];
		}
		public T[] InternalArray
		{
			[DebuggerStepThrough]
			get { return _array; }
		}

		#endregion

		//public Iterator<T> GetIterator(int start, int subcount)
		//{
		//    Debug.Assert(subcount >= 0 && (uint)start <= (uint)_count);
		//    if (subcount > _count - start)
		//        subcount = _count - start;
		//    return InternalList.GetIterator(_array, start, subcount);
		//}
		public T TryGet(int index, out bool fail)
		{
			if ((uint)index < (uint)_count) {
				fail = false;
				return _array[index];
			}
			fail = true;
			return default(T);
		}

		public void Sort(Comparison<T> comp) { Sort(0, Count, comp); }
		public void Sort(int index, int count, Comparison<T> comp)
		{
			Debug.Assert(index + count <= _count);
			InternalList.Sort(_array, index, count, comp);
		}

		IRange<T> IListSource<T>.Slice(int start, int count)
		{
			return new Slice_<T>(this, start, count);
		}
		public Slice_<T> Slice(int start, int count)
		{
			return new Slice_<T>(this, start, count);
		}

		public InternalList<T> CopySection(int start, int subcount)
		{
			Debug.Assert((uint)start <= (uint)_count && subcount >= 0);
			if (subcount > _count - start)
				subcount = _count - start;

			T[] copy = new T[subcount];
			Array.Copy(_array, start, copy, 0, subcount);
			return new InternalList<T>(copy, subcount);
		}
	}

	/// <summary>
	/// Contains static methods to help manage raw arrays with even less
	/// overhead than <see cref="InternalList{T}"/>.
	/// </summary>
	/// <remarks>
	/// The methods of this class are used by some data structures that contain
	/// arrays but, for whatever reason, don't use <see cref="InternalList{T}"/>.
	/// These methods are also used by InternalList(T) itself.
	/// </remarks>
	public static class InternalList
	{
		public static T[] CopyToNewArray<T>(T[] _array, int _count, int newCapacity)
		{
			T[] a = new T[newCapacity];
			if (_array == null)
				return a;

			Debug.Assert(_count <= _array.Length);
			Debug.Assert(_count <= newCapacity);
			if (_count <= 4)
			{	
				// Unroll loop for small list
				if (_count == 4) {
					// Most common case, assuming BaseCapacity==4
					a[3] = _array[3];
					a[2] = _array[2];
					a[1] = _array[1];
					a[0] = _array[0];
				} else if (_count >= 1) {
					a[0] = _array[0];
					if (_count >= 2) {
						a[1] = _array[1];
						if (_count >= 3)
							a[2] = _array[2];
					}
				}
			} else {
				Array.Copy(_array, a, _count);
			}
			return a;
		}
		
		public static T[] CopyToNewArray<T>(T[] array)
		{
			return CopyToNewArray(array, array.Length, array.Length);
		}

		public static void Fill<T>(T[] array, T value)
		{
			for (int i = 0; i < array.Length; i++)
				array[i] = value;
		}
		
		public static void Fill<T>(T[] array, int start, int count, T value)
		{
			if (count > 0)
			{
				// Just for fun, let's unroll the loop
				start--;
				if ((count & 1) != 0)
					array[++start] = value;
				while ((count -= 2) >= 0)
				{
					array[++start] = value;
					array[++start] = value;
				}
			}
		}
		
		public static int BinarySearch<T>(T[] array, int count, T k, Comparer<T> comp, bool lowerBound)
		{
			int low = 0;
			int high = count - 1;
			int invert = -1;

			while (low <= high)
			{
				int mid = low + ((high - low) >> 1);
				T midk = array[mid];
				int c = comp.Compare(midk, k);
				if (c < 0)
					low = mid + 1;
				else {
					high = mid - 1;
					if (c == 0)
					{
						if (lowerBound)
							invert = 0;
						else
							return mid;
					}
				}
			}

			return low ^ invert;
		}

		/// <summary>Performs a binary search with a custom comparison function.</summary>
		/// <param name="_array">Array to search</param>
		/// <param name="_count">Number of elements used in the array</param>
		/// <param name="k">A key to compare with elements of the array</param>
		/// <param name="compare">Lambda function that knows how to compare Ts with 
		/// Ks (T and K can be the same). It is passed a series of elements from 
		/// the array. It must return 0 if the element has the desired value, 1 if 
		/// the supplied element is higher than desired, and -1 if it is lower than 
		/// desired.</param>
		/// <param name="lowerBound">Whether to find the "lower bound" in case there
		/// are duplicates in the list. If duplicates exist of the search key k, the 
		/// lowest index of a matching duplicate is returned. This search mode may be 
		/// slightly slower when a match exists.</param>
		/// <returns>The index of the matching array entry, if found. If no exact
		/// match was found, this method returns the bitwise complement of an
		/// insertion location that would preserve the order.</returns>
		/// <example>
		///     // The first 6 elements are sorted. The seventh is invalid,
		///     // and must be excluded from the binary search.
		///     int[] array = new int[] { 0, 10, 20, 30, 40, 50, -1 };
		///     // The result will be 2, because array[2] == 20.
		///     int a = InternalList.BinarySearch(array, 6, i => i.CompareTo(20));
		///     // The result will be ~2, which equals -3, because index 2 would
		///     // be the correct place to insert 17 to preserve the sort order.
		///     int b = InternalList.BinarySearch(array, 6, i => i.CompareTo(17));
		/// </example>
		public static int BinarySearch<T, K>(T[] _array, int _count, K k, Func<T, K, int> compare, bool lowerBound)
		{
			int low = 0;
			int high = _count - 1;
			int invert = -1;

			while (low <= high)
			{
				int mid = low + ((high - low) >> 1);
				int c = compare(_array[mid], k);
				if (c < 0)
					low = mid + 1;
				else {
					high = mid - 1;
					if (c == 0)
					{
						if (lowerBound)
							invert = 0;
						else
							return mid;
					}
				}
			}

			return low ^ invert;
		}

		/// <summary>A binary search function that knows nothing about the list 
		/// being searched.</summary>
		/// <typeparam name="Anything">Any data type relevant to the caller.</typeparam>
		/// <param name="data">State information to be passed to compare()</param>
		/// <param name="count">Number of items in the list being searched</param>
		/// <param name="compare">Comparison method that is given the current index 
		/// to examine and the state parameter "data".</param>
		/// <param name="lowerBound">Whether to find the "lower bound" in case there
		/// are duplicates in the list. If duplicates exist of the search key k 
		/// exist, the lowest index of a matching duplicate is returned. This
		/// search mode may be slightly slower when a match exists.</param>
		/// <returns>The index of the matching index, if found. If no exact
		/// match was found, this method returns the bitwise complement of an
		/// insertion location that would preserve the sort order.</returns>
		public static int BinarySearchByIndex<Anything>(Anything data, int count, Func<int, Anything, int> compare, bool lowerBound)
		{
			int low = 0;
			int high = count - 1;
			int invert = -1;

			while (low <= high)
			{
				int mid = low + ((high - low) >> 1);
				int c = compare(mid, data);
				if (c < 0)
					low = mid + 1;
				else {
					high = mid - 1;
					if (c == 0)
					{
						if (lowerBound)
							invert = 0;
						else
							return mid;
					}
				}
			}

			return low ^ invert;
		}
		
		/// <summary>As an alternative to the typical enlarging pattern of doubling
		/// the array size when it overflows, this function proposes a 75% size
		/// increase instead (100% when the array is small), while ensuring that
		/// the array length stays even.</summary>
		/// <remarks>
		/// With a seed of 0, 2, or 4: 0, 2, 4, 8, 16, 30, 54, 96, 170, 298, 522...<br/>
		/// With a seed of 1: 1, 2, 4, 8, 16, 30, 54, 96, 170, 298, 522...<br/>
		/// With a seed of 3: 3, 6, 12, 22, 40, 72, 128, 226, 396...<br/>
		/// With a seed of 5: 5, 10, 18, 32, 58, 102, 180, 316, 554...<br/>
		/// With a seed of 7: 7, 14, 26, 46, 82, 144, 254, 446, 782...
		/// <para/>
		/// 75% size increases require 23.9% more allocations than size doubling
		/// (1.75 to the 1.239th power is about 2.0), but memory utilization is
		/// increased. With size doubling, the average list uses 2/3 of its 
		/// entries, but with this resizing pattern, the average list uses 72.72%
		/// of its entries. The average size of a list is 8.3% lower. Originally
		/// I used 50% size increases, but they required 71% more allocations, 
		/// which seemed like too much.
		/// </remarks>
		public static int NextLargerSize(int than)
		{
			return ((than << 1) - (than >> 2) + 2) & ~1;
		}
		/// <summary>Same as <see cref="NextLargerSize(int)"/>, but allows you to 
		/// specify a capacity limit, to avoid wasting memory when a collection has 
		/// a known maximum size.</summary>
		/// <param name="than">Return value will be larger than this number.</param>
		/// <param name="capacityLimit">Maximum value to return. This parameter is
		/// ignored if it than >= capacityLimit.</param>
		/// <returns>Produces the same result as <see cref="NextLargerSize(int)"/>
		/// unless the return value would be near capacityLimit (and capacityLimit
		/// > than). If the return value would be more than capacityLimit, 
		/// capacityLimit is returned instead. If the return value would be slightly
		/// less than capacityLimit (within 20%) then capacityLimit is returned, 
		/// to ensure that another reallocation will not be required later.</returns>
		public static int NextLargerSize(int than, int capacityLimit)
		{
			int larger = NextLargerSize(than);
			if (larger + (larger >> 2) > capacityLimit && than < capacityLimit)
				return capacityLimit;
			return larger;
		}

		public static T[] Insert<T>(int index, T item, T[] array, int count)
		{
			Debug.Assert((uint)index <= (uint)count);
			if (count == array.Length)
			{
				int newCap = NextLargerSize(array.Length);
				array = CopyToNewArray(array, count, newCap);
			}
			for (int i = count; i > index; i--)
				array[i] = array[i - 1];
			array[index] = item;
			return array;
		}

		public static T[] InsertRangeHelper<T>(int index, int spaceNeeded, T[] array, int count)
		{
			Debug.Assert((uint)index <= (uint)count);
			array = AutoRaiseCapacity(array, count, spaceNeeded, int.MaxValue);
			for (int i = count; i > index; i--)
				array[i + spaceNeeded - 1] = array[i - 1];
			return array;
		}

		public static T[] AutoRaiseCapacity<T>(T[] array, int count, int more, int capacityLimit)
		{
			if (count + more > array.Length)
			{
				int newCapacity = NextLargerSize(count + more - 1, capacityLimit);
				return CopyToNewArray(array, count, newCapacity);
			}
			return array;
		}
		
		public static int RemoveAt<T>(int index, T[] array, int count)
		{
			Debug.Assert((uint)index < (uint)count);
			for (int i = index; i + 1 < count; i++)
				array[i] = array[i + 1];
			array[count - 1] = default(T);
			return count - 1;
		}
		
		public static int RemoveAt<T>(int index, int removeCount, T[] array, int count)
		{
			Debug.Assert((uint)index <= (uint)count);
			Debug.Assert((uint)(index + removeCount) <= (uint)count);
			Debug.Assert(removeCount >= 0);
			if (removeCount > 0)
			{
				for (int i = index; i + removeCount < count; i++)
					array[i] = array[i + removeCount];
				for (int i = count - removeCount; i < count; i++)
					array[i] = default(T);
				return count - removeCount;
			}
			return count;
		}

		public static void Move<T>(T[] array, int from, int to)
		{
			T saved = array[from];
			if (to < from) {
				for (int i = from; i > to; i--)
					array[i] = array[i - 1];
				array[to] = saved;
			} else if (from < to) {
				for (int i = from; i < to; i++)
					array[i] = array[i + 1];
				array[to] = saved;
			}
		}
		
		internal const int QuickSortThreshold = 9;
		internal const int QuickSortMedianThreshold = 15;

		/// <summary>Performs a quicksort using a Comparison function.</summary>
		/// <remarks>
		/// Normally one uses Array.Sort for sorting arrays.
		/// This method exists because there is no Array.Sort overload that
		/// accepts both a Comparison and a range (index, count), nor does the
		/// .NET framework provide access to its internal adapter that converts 
		/// Comparison to IComparer.
		/// <para/>
		/// This quicksort algorithm uses a best-of-three pivot so that it remains
		/// performant (fast) if the input is already sorted. It is designed to 
		/// perform reasonably well in case the data contains many duplicates (not
		/// verified). It is also designed to avoid using excessive stack space if 
		/// a worst-case input occurs that requires O(N^2) time.
		/// </remarks>
		public static void Sort<T>(T[] array, int index, int count, Comparison<T> comp)
		{
			Debug.Assert((uint)index <= (uint)array.Length);
			Debug.Assert((uint)count <= (uint)array.Length - (uint)index);
			
			for (;;) {
				if (count < QuickSortThreshold)
				{
					if (count <= 2) {
						if (count == 2)
							MathEx.SortPair(ref array[index], ref array[index+1], comp);
					} else {
						InsertionSort(array, index, count, comp);
					}
					return;
				}

				int iPivot = PickPivot(array, index, count, comp);

				int iBegin = index;
				// Swap the pivot to the beginning of the range
				T pivot = array[iPivot];
				if (iBegin != iPivot)
					MathEx.Swap(ref array[iBegin], ref array[iPivot]);

				int i = iBegin + 1;
				int iOut = iBegin;
				int iStop = index + count;
				int leftSize = 0; // size of left partition

				// Quick sort pass
				do {
					int order = comp(array[i], pivot);
					if (order < 0 || (order == 0 && leftSize < (count >> 1)))
					{
						++iOut;
						++leftSize;
						if (i != iOut)
							MathEx.Swap(ref array[i], ref array[iOut]);
					}
				} while (++i != iStop);

				// Finally, put the pivot element in the middle (at iOut)
				MathEx.Swap(ref array[iBegin], ref array[iOut]);

				// Now we need to sort the left and right sub-partitions. Use a 
				// recursive call only to sort the smaller partition, in order to 
				// guarantee O(log N) stack space usage.
				int rightSize = count - 1 - leftSize;
				if (leftSize < rightSize)
				{
					// Recursively sort the left partition; iteratively sort the right
					Sort(array, index, leftSize, comp);
					index += leftSize + 1;
					count = rightSize;
				}
				else
				{	// Iteratively sort the left partition; recursively sort the right
					count = leftSize;
					Sort(array, index + leftSize + 1, rightSize, comp);
				}
			}
		}

		internal static int PickPivot<T>(IList<T> list, int index, int count, Comparison<T> comp)
		{
			// Choose the median of the first, last and middle item
			int iPivot0 = index;
			int iPivot1 = index + (count >> 1);
			int iPivot2 = index + count - 1;
			if (comp(list[iPivot0], list[iPivot1]) > 0)
				MathEx.Swap(ref iPivot0, ref iPivot1);
			if (comp(list[iPivot1], list[iPivot2]) > 0)
			{
				iPivot1 = iPivot2;
				if (comp(list[iPivot0], list[iPivot1]) > 0)
					iPivot1 = iPivot0;
			}
			return iPivot1;
		}

		/// <summary>Performs an insertion sort.</summary>
		/// <remarks>The insertion sort is a stable sort algorithm that is slow in 
		/// general (O(N^2)). It should be used only when (a) the list to be sorted
		/// is short (less than about 20 elements) or (b) the list is very nearly
		/// sorted already.</remarks>
		/// <seealso cref="ListExt.InsertionSort"/>
		public static void InsertionSort<T>(T[] array, int index, int count, Comparison<T> comp)
		{
			for (int i = index + 1; i < index + count; i++)
			{
				int j = i;
				do
					if (!MathEx.SortPair(ref array[j - 1], ref array[j], comp))
						break;
				while (--j > index);
			}
		}

		public static bool AllEqual<T>(this InternalList<T> a, InternalList<T> b) where T : IEquatable<T>
		{
			return a.Count == b.Count && AllEqual(a.InternalArray, b.InternalArray, a.Count);
		}
		public static bool AllEqual<T>(T[] a, T[] b, int count) where T : IEquatable<T>
		{
			for (int i = 0; i < count; i++)
				if (!a[i].Equals(b[i]))
					return false;
			return true;
		}
	}
}
