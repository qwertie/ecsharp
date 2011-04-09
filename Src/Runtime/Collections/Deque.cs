using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Loyc.Runtime
{
	[Serializable()]
	public class Deque<T> : IList<T>, IArray<T>, IDeque<T>
	{
		protected T[] _array = InternalList<T>.EmptyArray;
		protected int _count, _start;

		public Deque(int capacity)     { Capacity = capacity; }
		public Deque(IIterable<T>   items) { PushLast(items); }
		public Deque(ISource<T>     items) { PushLast(items); }
		public Deque(ICollection<T> items) { PushLast(items); }
		public Deque(IEnumerable<T> items) { PushLast(items); }
		public Deque() { }

		private int FirstHalfSize { get { return Math.Min(_array.Length - _start, _count); } }
		private int SecondHalfSize { get { return _count - FirstHalfSize; } }

		private int Internalize(int index)
		{
			index += _start;
			if (index - _array.Length >= 0)
				return index - _array.Length;
			return index;
		}
		private int IncMod(int index)
		{
			if (++index == _array.Length)
				index -= _array.Length;
			return index;
		}
		private int IncMod(int index, int amount)
		{
			if ((index += amount) >= _array.Length)
				index -= _array.Length;
			return index;
		}
		private int DecMod(int index)
		{
			if (index == 0)
				return _array.Length - 1;
			return index - 1;
		}
		private int DecMod(int index, int amount)
		{
			if ((index -= amount) < 0)
				index += _array.Length;
			return index;
		}
		private void CheckPopCount(int amount)
		{
			if (amount < 0)
	 			throw new InvalidOperationException(string.Format("Can't pop a negative number of elements ({0})", amount));
			if (amount > _count)
	 			throw new InvalidOperationException(string.Format("Can't pop more elements than Deque<{0}> contains ({1}>{2})", typeof(T).Name, amount, _count));
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
			CheckPopCount(amount);
			
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
			CheckPopCount(amount);
			
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
		private void AutoEnlarge(int more)
		{
			if (_count + more > _array.Length)
				Capacity = InternalList.NextLargerSize(_count + more - 1);
		}

		public int Capacity
		{
			get { return _array.Length; }
			set {
				int delta = value - _array.Length;
				if (delta == 0)
					return;

				if (value < _count)
					throw new ArgumentOutOfRangeException(string.Format("Capacity is too small ({0}<{1})", value, _count));
				
				// Copy second half into new array
				int size1 = FirstHalfSize;
				var newArray = InternalList.CopyToNewArray(_array, _count - size1, value);
				
				// Copy first half
				for (int i = _start; i < _start + size1; i++)
					newArray[i + delta] = _array[i];

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
			if ((uint)index > (uint)_count)
				throw new IndexOutOfRangeException(string.Format("Invalid index in Deque<{0}> ({1}∉[0,{2}])", typeof(T).Name, index, _count));
			
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
			if (amount < 0)
				throw new ArgumentOutOfRangeException("amount");

			RemoveHelper(index, amount);
		}

		private void RemoveHelper(int index, int amount)
		{
			if ((uint)index > (uint)_count || (uint)(index + amount) > (uint)_count)
				throw new IndexOutOfRangeException(string.Format("Invalid removal range in Deque<{0}> ([{1},{2})⊈[0,{3}))", typeof(T).Name, index, index + amount, _count));

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
			get {
				CheckIndex(index);
				return _array[Internalize(index)];
			}
			set {
				CheckIndex(index);
				_array[Internalize(index)] = value;
			}
		}
		private void CheckIndex(int index)
		{
			if ((uint)index >= (uint)_count)
				throw new IndexOutOfRangeException(string.Format("Invalid index in Deque<{0}> ({1}∉[0,{2}))", typeof(T).Name, index, _count));
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
			if (array == null || array.Length < _count)
				throw new ArgumentOutOfRangeException("array");
			if (arrayIndex < 0 || array.Length - arrayIndex < _count)
				throw new ArgumentOutOfRangeException("arrayIndex");
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

		private int Checksum
		{
			get { return (_count << 16) + _start; }
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
			return GetIterator().ToEnumerator();
		}

		public Iterator<T> GetIterator()
		{
			int checksum = Checksum;
			int size1 = FirstHalfSize;
			int stop = _start + size1;
			int stop2 = _count - size1;
			int i = _start;
			
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

				if (checksum != Checksum)
					throw new InvalidOperationException("The collection was modified after enumeration started.");

				return _array[i++];
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
	}

	[Serializable()]
	public class Deque : Deque<object>, System.Collections.IList
	{
		public bool IsFixedSize
		{
			get { return false; }
		}
		public void CopyTo(Array array, int arrayIndex)
		{
			if (array == null || array.Length < _count)
				throw new ArgumentOutOfRangeException("array");
			if (arrayIndex < 0 || array.Length - arrayIndex < _count)
				throw new ArgumentOutOfRangeException("arrayIndex");
			
			foreach(object obj in this)
				array.SetValue(obj, arrayIndex++);
		}
		public bool IsSynchronized
		{
			get { return false; }
		}
		public object SyncRoot
		{
			get { return this; }
		}
		public new void Remove(object obj)
		{
			base.Remove(obj);
		}
		public new int Add(object obj)
		{
			base.Add(obj);
			return _count - 1;
		}
	}
}
