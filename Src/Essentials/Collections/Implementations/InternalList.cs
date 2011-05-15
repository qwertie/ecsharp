// Came from Loyc. Licence: LGPL
namespace Loyc.Collections.Impl
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Diagnostics;
	using System.Linq;
	using Loyc.Essentials;

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
	/// InternalList has one nice thing that List(of T) lacks: a Resize() method
	/// and an equivalent Count setter. Which dork at Microsoft decided no one
	/// should be allowed to set the list length directly?
	/// <para/>
	/// Finally, alongside InternalList(T), the static class InternalList comes 
	/// with some static methods (CopyToNewArray, Insert, RemoveAt, Move) to help
	/// manage raw arrays. You might want to use these in a data structure 
	/// implementation even if you choose not to use InternalList(T) instances.
	/// </remarks>
	public struct InternalList<T> : IList<T>, IListSource<T>, IInsertRemoveRange<T>, IGetIteratorSlice<T>, ICloneable<InternalList<T>>
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
		public InternalList(Iterator<T> items)
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

		private void IncreaseCapacity()
		{
			// 4, 8, 14, 22, 34, 52, 80...
			Capacity = InternalList.NextLargerSize(_array.Length);
		}

		public void Resize(int newSize)
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
				if (newSize == 0)
					Clear();
				else if (newSize < (_array.Length >> 2)) {
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
		public void InsertRange(int index, ISource<T> items)
		{
			_array = InternalList.InsertRangeHelper(index, items.Count, _array, _count);
			
			int count = items.Count;
			_count += count;
			
			int stop = index + count;
			bool ended = false;
			T item;
			var it = items.GetIterator();
			for (; index < stop; index++) {
				item = it(ref ended);
				if (ended) InsertRangeSizeMismatch();
				_array[index] = item;
			}
			item = it(ref ended);
			if (!ended) InsertRangeSizeMismatch();
		}
		public void InsertRange(int index, ICollection<T> items)
		{
			_array = InternalList.InsertRangeHelper(index, items.Count, _array, _count);
			
			int count = items.Count;
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
			var s = e as ISource<T>;
			if (s != null)
				InsertRange(index, s);
			var c = e as ICollection<T>;
			if (c != null)
				InsertRange(index, c);
			else
				InsertRange(index, new List<T>(e));
		}
		void IInsertRemoveRange<T>.InsertRange(int index, IListSource<T> s)
		{
			InsertRange(index, (ISource<T>)s);
		}

		public void AddRange(ISource<T> items)
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
		void IAddRange<T>.AddRange(IListSource<T> s)
		{
			AddRange((ISource<T>)s);
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
		public void AddRange(Iterator<T> items)
		{
			for (bool ended = false;;) {
				T item = items(ref ended);
				if (ended)
					break;
				Add(item);
			}
		}

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

		public void RemoveLast()
		{
			Debug.Assert(_count > 0);
			_array[--_count] = default(T);
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
			return InternalList.BinarySearch(_array, _count, lookFor, Comparer<T>.Default);
		}
		public int BinarySearch(T lookFor, Comparer<T> comp)
		{
			return InternalList.BinarySearch(_array, _count, lookFor, comp);
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

		public int IndexOf(T item)
		{
			EqualityComparer<T> comparer = EqualityComparer<T>.Default;
			for (int i = 0; i < Count; i++)
				if (comparer.Equals(this[i], item))
					return i;
			return -1;
		}
		public bool Contains(T item)
		{
			return IndexOf(item) != -1;
		}
		public void CopyTo(T[] array, int arrayIndex)
		{
			foreach (T item in this)
				array[arrayIndex++] = item;
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

		public Iterator<T> GetIterator()
		{
			return InternalList.GetIterator(_array, 0, _count);
		}
		public Iterator<T> GetIterator(int start, int subcount)
		{
			Debug.Assert(subcount >= 0 && (uint)start <= (uint)_count);
			if (subcount > _count - start)
				subcount = _count - start;
			return InternalList.GetIterator(_array, start, subcount);
		}
		public T TryGet(int index, ref bool fail)
		{
			if ((uint)index < (uint)_count)
				return _array[index];
			fail = true;
			return default(T);
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
		
		public static T[] CopyToNewArray<T>(T[] _array)
		{
			return CopyToNewArray(_array, _array.Length, _array.Length);
		}
		
		public static int BinarySearch<T>(T[] _array, int _count, T k, Comparer<T> comp)
		{
			int low = 0;
			int high = _count - 1;
			while (low <= high)
			{
				int mid = low + ((high - low) >> 1);
				T midk = _array[mid];
				int c = comp.Compare(midk, k);
				if (c < 0)
					low = mid + 1;
				else if (c > 0)
					high = mid - 1;
				else
					return mid;
			}

			return ~low;
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
		public static int BinarySearch<T, K>(T[] _array, int _count, K k, Func<T, K, int> compare)
		{
			int low = 0;
			int high = _count - 1;
			while (low <= high)
			{
				int mid = low + ((high - low) >> 1);
				int c = compare(_array[mid], k);
				if (c < 0)
					low = mid + 1;
				else if (c > 0)
					high = mid - 1;
				else
					return mid;
			}

			return ~low;
		}
		
		/// <summary>As an alternative to the typical enlarging pattern of doubling
		/// the array size when it overflows, this function proposes a 50% size
		/// increase instead (more when the array is small), while ensuring that
		/// the array length stays even.</summary>
		/// <remarks>
		/// With a seed of 0, 2, or 4: 0, 2, 4, 8, 14, 22, 34, 52, 80, 122,...
		/// With a seed of 1: 1, 2, 4, 8, 14, 22, 34, 52, 80, 122,...
		/// With a seed of 3: 3, 6, 10, 16, 26, 40, 62, 94, 142...
		/// With a seed of 5: 5, 8, 14, 22, 34, 52, 80, 122,...
		/// <para/>
		/// I'd like to reduce the average memory overhead of lists. However,
		/// increases of 50% might be too expensive in terms of reallocations--the
		/// array is copied 71% more often when enlarging by 50% instead of 100%
		/// (1.5 to the 1.71 power is about 2.0. Do the math.) Probably 62.5%
		/// increases would be better, since they require only 42.7% more 
		/// allocations.
		/// </remarks>
		public static int NextLargerSize(int than)
		{
			return (than + 2 + (than >> 1)) & ~1;
		}
		public static int NextLargerSize(int than, int capacityLimit)
		{
			int larger = NextLargerSize(than);
			if (larger > capacityLimit - (capacityLimit >> 2) && than < capacityLimit)
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
			if (count + spaceNeeded > array.Length)
			{
				int newCap = Math.Max(NextLargerSize(array.Length), count + spaceNeeded);
				array = CopyToNewArray(array, count, newCap);
			}
			for (int i = count; i > index; i--)
				array[i + spaceNeeded - 1] = array[i - 1];
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
		
		public static Iterator<T> GetIterator<T>(T[] array, int start, int subcount)
		{
			Debug.Assert((uint)(start + subcount) <= (uint)array.Length);
			int i = start - 1;
			return delegate(ref bool ended)
			{
				if (--subcount >= 0)
					return array[++i];
				else {
					subcount = 0;
					ended = true;
					return default(T);
				}
			};
		}
	}
}
