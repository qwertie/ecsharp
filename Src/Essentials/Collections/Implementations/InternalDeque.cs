/*
 * Created by SharpDevelop.
 * User: Pook
 * Date: 4/12/2011
 * Time: 9:15 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
namespace Loyc.Collections.Impl
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;

	/// <summary>A compact auto-enlarging deque structure that is intended to be 
	/// used within other data structures. It should only be used internally in
	/// "private" or "protected" members of low-level code. In most cases, you
	/// should use <see cref="Deque{T}"/> instead.
	/// </summary>
	/// <remarks>
	/// InternalDeque is a struct, not a class, in order to save memory; and for 
	/// maximum performance, it asserts rather than throwing an exception 
	/// when an incorrect array index is used (the one exception is the iterator,
	/// which throws in case the collection is modified during enumeration; this 
	/// is for the sake of <see cref="Deque{T}"/>. For these and other reasons, one
	/// should not expose it in a public API, and it should only be used when 
	/// performance trumps all other concerns.
	/// <para/>
	/// Also, do not use the default contructor. Always specify an initial 
	/// capacity or copy InternalDeque.Empty so that the internal array gets a 
	/// value. All methods in this structure assume _array is not null.
	/// <para/>
	/// </remarks>
	[Serializable()]
	public struct InternalDeque<T> : IListEx<T>, IDeque<T>
	{
		public static readonly T[] EmptyArray = InternalList<T>.EmptyArray;
		public static readonly InternalDeque<T> Empty = new InternalDeque<T>(0);
		internal T[] _array;
		internal int _count, _start;

		public InternalDeque(int capacity)
		{
			_count = _start = 0;
			_array = capacity != 0 ? new T[capacity] : EmptyArray;
		}

		private int FirstHalfSize { get { return Math.Min(_array.Length - _start, _count); } }

		public T[] InternalArray { get { return _array; } }

		public int Internalize(int index)
		{
			Debug.Assert((uint)index <= (uint)_count);
			index += _start;
			if (index - _array.Length >= 0)
				return index - _array.Length;
			return index;
		}

		public int IncMod(int index)
		{
			if (++index == _array.Length)
				index -= _array.Length;
			return index;
		}
		public int IncMod(int index, int amount)
		{
			if ((index += amount) >= _array.Length)
				index -= _array.Length;
			return index;
		}
		public int DecMod(int index)
		{
			if (index == 0)
				return _array.Length - 1;
			return index - 1;
		}
		public int DecMod(int index, int amount)
		{
			if ((index -= amount) < 0)
				index += _array.Length;
			return index;
		}

		public int IndexOf(T item)
		{
			EqualityComparer<T> comparer = EqualityComparer<T>.Default;
			int size1 = FirstHalfSize;
			int stop = _start + size1;
			int stop2 = _count - size1;
			int offs = 0;
			
			for (int i = _start;;) {
				for (; i < stop; i++) {
					if (comparer.Equals(item, this[i]))
						return offs + i;
				}
				if (stop == stop2)
					return -1;
				stop = stop2;
				offs = size1;
				i = 0;
			}
		}

		public void PushLast(ICollection<T> items)
		{
			AutoEnlarge(items.Count);
			PushLast((IEnumerable<T>)items);
		}
		public void PushLast(IEnumerable<T> items)
		{
			foreach(T item in items)
				PushLast(item);
		}
		public void PushLast(ISource<T> items)
		{
			AutoEnlarge(items.Count);
			PushLast((IIterable<T>)items);
		}
		public void PushLast(IIterable<T> items)
		{
			for (Iterator<T> it = items.GetIterator();;)
			{
				bool ended = false;
				T item = it(ref ended);
				if (ended) break;
				PushLast(item);
			}
		}

		public void PushLast(T item)
		{
			AutoEnlarge(1);
			
			int i = _start + _count;
			if (i > _array.Length)
				i -= _array.Length;
			_array[i] = item;
			++_count;
		}
		
		public void PushFirst(T item)
		{
			AutoEnlarge(1);

			if (--_start < 0)
				_start += _array.Length;
			_array[_start] = item;
		}

		public void PopLast(int amount)
		{
			if (amount == 0)
				return;
			Debug.Assert((uint)amount < (uint)_count);
			
			_count -= amount;
			int i = IncMod(_start, _count);
			for (;;) {
				_array[i] = default(T);
				if (--amount == 0)
					break;
				i = IncMod(i);
			}

			AutoShrink();
		}

		public void PopFirst(int amount)
		{
			Debug.Assert(amount <= _count);
			
			int i = _start;
			_start = IncMod(_start, amount);
			while (i != _start) {
				_array[i] = default(T);
				i = IncMod(i);
			}

			AutoShrink();
		}

		private void AutoShrink()
		{
 			if ((_count << 1) + 2 < _array.Length)
				Capacity = _count + 2;
		}
		public void AutoEnlarge(int more)
		{
			if (_count + more > _array.Length)
				Capacity = InternalList.NextLargerSize(_count + more - 1);
		}
		public void AutoEnlarge(int more, int capacityLimit)
		{
			if (_count + more > _array.Length)
				Capacity = InternalList.NextLargerSize(_count + more - 1, capacityLimit);
		}

		public int Capacity
		{
			get { return _array.Length; }
			set {
				int delta = value - _array.Length;
				if (delta == 0)
					return;

				Debug.Assert(value >= _count);

				T[] newArray = new T[value];
				
				int size1 = FirstHalfSize;
				int size2 = _count - size1;
				Array.Copy(_array, _start, newArray, 0, size1);
				if (size2 > 0)
				    Array.Copy(_array, 0, newArray, size1, size2);
				
				_start = 0;
				_array = newArray;
			}
		}

		public void Insert(int index, T item)
		{
			InsertHelper(index, 1);
			_array[Internalize(index)] = item;
		}

		public void InsertRange(int index, ICollection<T> items)
		{
			// Note: this is written so that the invariants hold if the
			// collection throws or returns an incorrect Count.
			int amount = items.Count;
			InsertHelper(index, amount);
			
			int iindex = Internalize(index);
			var it = items.GetEnumerator();
			for (int copied = 0; copied < amount; copied++)
			{
				if (!it.MoveNext())
					break;
				_array[iindex] = it.Current;
				iindex = IncMod(iindex);
			}
		}

		public void InsertRange(int index, ISource<T> items)
		{
			// Note: this is written so that the invariants hold if the
			// collection throws or returns an incorrect Count.
			int amount = items.Count;
			InsertHelper(index, amount);
			
			int iindex = Internalize(index);
			var it = items.GetIterator();
			for (int copied = 0; copied < amount; copied++)
			{
				if (!it.MoveNext(out _array[iindex]))
					break;
				iindex = IncMod(iindex);
			}
		}

		private void InsertHelper(int index, int amount)
		{
			Debug.Assert((uint)index <= (uint)_count);
			
			AutoEnlarge(amount);

			int deltaB = _count - index;
			if (index < deltaB)
			{
				int iFrom = Internalize(0);
				int iTo = Internalize(-amount);
				for (int left = deltaB; left > 0; left--)
				{
					_array[iTo] = _array[iFrom];
					iFrom = IncMod(iFrom);
					iTo = IncMod(iTo);
				}

				_start = DecMod(_start, amount);
			}
			else
			{
				int iFrom = Internalize(_count - 1);
				int iTo = Internalize(Count - 1 + amount);
				for (int left = deltaB; left > 0; left--) {
					_array[iTo] = _array[iFrom];
					iFrom = DecMod(iFrom);
					iTo = DecMod(iTo);
				}
			}

			_count += amount;
		}

		public void RemoveAt(int index)
		{
			RemoveHelper(index, 1);
		}
		public void RemoveRange(int index, int amount)
		{
			Debug.Assert (amount >= 0);

			RemoveHelper(index, amount);
		}

		private void RemoveHelper(int index, int amount)
		{
			Debug.Assert((uint)index <= (uint)_count && (uint)(index + amount) <= (uint)_count);

			_count -= amount;
			int deltaB = _count - index;
			if (index < deltaB)
			{
				// Collapse front half
				_start = IncMod(_start, amount);

				int iFrom = Internalize(index - 1);
				int iTo = Internalize(index - 1 + amount);
				for (int i = 0; i < index; i++)
				{
					_array[iTo] = _array[iFrom];
					iTo = DecMod(iTo);
					iFrom = DecMod(iFrom);
				}
			}
			else
			{
				// Collapse back half
				int iTo = Internalize(index);
				int iFrom = Internalize(index + amount);
				for (int i = 0; i < deltaB; i++)
				{
					_array[iTo] = _array[iFrom];
					iTo = IncMod(iTo);
					iFrom = IncMod(iFrom);
				}
			}

			AutoShrink();
		}

		public T this[int index]
		{
			[DebuggerStepThrough]
			get {
				Debug.Assert((uint)index < (uint)_count);
				return _array[Internalize(index)];
			}
			[DebuggerStepThrough]
			set {
				Debug.Assert((uint)index < (uint)_count);
				_array[Internalize(index)] = value;
			}
		}

		public bool TrySet(int index, T value)
		{
			if ((uint)index >= (uint)_count)
				return false;
			_array[Internalize(index)] = value;
			return true;
		}
		public T TryGet(int index, ref bool fail)
		{
			if ((uint)index < (uint)_count)
				return _array[Internalize(index)];
			else {
				fail = true;
				return default(T);
			}
		}

		/// <summary>An alias for PushLast().</summary>
		public void Add(T item)
		{
			PushLast(item);
		}

		public void Clear()
		{
			_array = InternalList<T>.EmptyArray;
			_count = _start = 0;
		}

		public bool Contains(T item)
		{
			return IndexOf(item) > -1;
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			Debug.Assert(array != null && array.Length >= _count);
			Debug.Assert(arrayIndex >= 0 && array.Length - arrayIndex >= _count);
			int iindex = _start;
			for (int i = 0; i < _count; i++) {
				array[i + arrayIndex] = _array[iindex];
				iindex = IncMod(iindex);
			}
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
			int i = IndexOf(item);
			if (i <= -1)
				return false;
			RemoveAt(i);
			return true;
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetEnumerator();
			/*int checksum = Checksum;
			int size1 = FirstHalfSize;
			int stop = _start + size1;
			int stop2 = _count - size1;
			
			for (int i = _start;;) {
				for (; i < stop; i++) {
					if (checksum != Checksum)
						throw new InvalidOperationException("The collection was modified after enumeration started.");
					yield return _array[i];
				}
				if (stop == stop2)
					yield break;
				stop = stop2;
				i = 0;
			}*/
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		public IEnumerator<T> GetEnumerator()
		{
			return GetIterator().AsEnumerator();
		}

		public Iterator<T> GetIterator()
		{
			int size1 = FirstHalfSize;
			int stop = _start + size1;
			int stop2 = _count - size1;
			int i = _start;
			// we must make a copy because the iterator could be called after the 
			// InternalDeque ceases to exist.
			T[] array = _array;
			
			return delegate(ref bool ended)
			{
				if (i >= stop) {
					if (stop == stop2) {
						ended = true;
						return default(T);
					}
					stop = stop2;
					i = 0;
				}

				return array[i++];
			};
		}

		#region IDeque<T>

		public T TryPopFirst(ref bool isEmpty)
		{
			T value = TryPeekFirst(ref isEmpty);
			if (!isEmpty)
				PopFirst(1);
			return value;
		}
		public T TryPeekFirst(ref bool isEmpty)
		{
			if (_count > 0)
				return First;
			isEmpty = true;
			return default(T);
		}
		public T TryPopLast(ref bool isEmpty)
		{
			T value = TryPeekLast(ref isEmpty);
			if (!isEmpty)
				PopLast(1);
			return value;
		}
		public T TryPeekLast(ref bool isEmpty)
		{
			if (_count > 0)
				return Last;
			isEmpty = true;
			return default(T);
		}

		public T First
		{
			get { return this[0]; }
			set { this[0] = value; }
		}
		public T Last
		{
			get { return this[_count - 1]; }
			set { this[_count - 1] = value; }
		}
		public bool IsEmpty
		{
			get { return _count <= 0; }
		}

		#endregion

		public int BinarySearch(T k, Comparer<T> comp)
		{
			int low = 0;
			int high = _count - 1;
			while (low <= high)
			{
				int mid = low + ((high - low) >> 1);
				T midk = this[mid];
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

		public int BinarySearch<K>(K k, Func<T, K, int> compare)
		{
			int low = 0;
			int high = _count - 1;
			while (low <= high)
			{
				int mid = low + ((high - low) >> 1);
				T midk = this[mid];
				int c = compare(midk, k);
				if (c < 0)
					low = mid + 1;
				else if (c > 0)
					high = mid - 1;
				else
					return mid;
			}

			return ~low;
		}
	}
}
