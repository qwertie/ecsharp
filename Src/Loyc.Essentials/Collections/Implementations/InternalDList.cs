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
	using Loyc.Math;

	/// <summary>A compact auto-enlarging deque structure that is intended to be 
	/// used within other data structures. It should only be used internally in
	/// "private" or "protected" members of low-level code. In most cases, you
	/// should use <see cref="DList{T}"/> instead.
	/// </summary>
	/// <remarks>
	/// This type is implemented with what is commonly called a "circular buffer".
	/// There is a single array plus a "start index" and a count. The array may or 
	/// may not be divided into two "halves", depending on the circumstances.
	/// The first element of the DList (returned from this[0] and from the
	/// First property) is located at the "start index" of the array; and if the 
	/// start index + count is greater than the array size, then the end of the 
	/// DList wraps around to the beginning of the array.
	/// <para/>
	/// InternalDeque is a struct, not a class, in order to save memory; and for 
	/// maximum performance, it asserts rather than throwing an exception 
	/// when an incorrect array index is used (the one exception is the iterator,
	/// which throws in case the collection is modified during enumeration; this 
	/// is for the sake of <see cref="DList{T}"/>.) For these and other reasons, 
	/// one should not expose it in a public API, and it should only be used when 
	/// performance is very important.
	/// <para/>
	/// Also, do not use the <c>default(InternalDList{T})</c> or the equivalent
	/// "default constructor", which only exists because C# requires it. Always 
	/// specify an initial capacity or copy InternalDeque.Empty so that the 
	/// internal array gets a value. All methods in this structure assume _array 
	/// is not null.
	/// <para/>
	/// This class does not implement <see cref="IDeque{T}"/> and <see 
	/// cref="IList{T}"/> in order to help you not to shoot yourself in the foot.
	/// The problem is that any extension methods used with those interfaces that 
	/// change the list, such as PopLast(), malfunction because the structure is
	/// implicitly boxed, producing a shallow copy. By not implementing those 
	/// interfaces, the extension methods are not available, ensuring you don't
	/// accidently box the structure. If you need those interfaces, You can always 
	/// call <see cref="AsDList"/> to construct a <see cref="DList{T}"/> in O(1) 
	/// time.
	/// <para/>
	/// You may be curious why <see cref="InternalList{T}"/>, in contrast, DOES
	/// implement <see cref="IList{T}"/>. It's because there is no way to make
	/// <see cref="List{T}"/> from <see cref="InternalList{T}"/> in O(1) time;
	/// so boxing the <see cref="InternalList{T}"/> is the only fast way to get
	/// an instance of <see cref="IList{T}"/>.
	/// </remarks>
	[Serializable()]
	#if !CompactFramework
	[DebuggerTypeProxy(typeof(ListSourceDebugView<>)), DebuggerDisplay("Count = {Count}")]
	#endif
	public struct InternalDList<T> : IListSource<T>, ICloneable<InternalDList<T>>
	{
		public static readonly T[] EmptyArray = InternalList<T>.EmptyArray;
		public static readonly InternalDList<T> Empty = new InternalDList<T>(0);
		internal T[] _array;
		internal int _count, _start;

		public InternalDList(int capacity)
		{
			_count = _start = 0;
			_array = capacity != 0 ? new T[capacity] : EmptyArray;
		}
		public InternalDList(T[] array, int count)
		{
			_array = array;
			_count = count;
			_start = 0;
		}

		private int FirstHalfSize { get { return Min(_array.Length - _start, _count); } }
		private bool IsDivided { get { return _start + _count > _array.Length; } }

		public T[] InternalArray { get { return _array; } }

		public int Internalize(int index)
		{
			Debug.Assert((uint)index <= (uint)_count);
			return InternalizeNC(index);
		}
		private int InternalizeNC(int index)
		{
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
			return IndexOf(item, 0);
		}
		public int IndexOf(T item, int startIndex)
		{
			EqualityComparer<T> comparer = EqualityComparer<T>.Default;
			int size1 = FirstHalfSize;
			int stop = _start + size1;
			int stop2 = _count - size1;
			int returnAdjustment = -_start;
			int i;
			if ((uint)startIndex < (uint)size1)
				i = _start + startIndex;
			else {
				// start in the second half
				stop = stop2;
				returnAdjustment = size1;
				i = startIndex - size1;
				if (startIndex < 0) CheckParam.ThrowOutOfRange("startIndex");
			}
			
			for (;;) {
				for (; i < stop; i++) {
					if (comparer.Equals(item, _array[i]))
						return i + returnAdjustment;
				}
				if (stop == stop2)
					return -1;
				stop = stop2;
				returnAdjustment = size1;
				i = 0;
			}
		}

		public void PushLast(ICollection<T> items)
		{
			AutoRaiseCapacity(items.Count);
			PushLast((IEnumerable<T>)items);
		}
		public void PushLast(IEnumerable<T> items)
		{
			foreach(T item in items)
				PushLast(item);
		}
		public void PushLast(IReadOnlyCollection<T> items)
		{
			AutoRaiseCapacity(items.Count);
			PushLast((IEnumerable<T>)items);
		}
		public void PushLast(ICollectionAndReadOnly<T> items)
		{
			PushLast((ICollection<T>)items);
		}

		public void PushLast(T item)
		{
			AutoRaiseCapacity(1);
			
			int i = _start + _count;
			if (i >= _array.Length)
				i -= _array.Length;
			_array[i] = item;
			++_count;
		}
		
		public void PushFirst(T item)
		{
			AutoRaiseCapacity(1);

			if (--_start < 0)
				_start += _array.Length;
			_array[_start] = item;
			_count++;
		}

		public void PopLast(int amount)
		{
			if (amount == 0)
				return;
			Debug.Assert((uint)amount <= (uint)_count);
			
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
			
			_count -= amount;
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
		public void AutoRaiseCapacity(int more)
		{
			if (_count + more > _array.Length)
				Capacity = InternalList.NextLargerSize(_count + more - 1);
		}
		public void AutoRaiseCapacity(int more, int capacityLimit)
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
			int iindex = InsertRangeHelper(index, 1);
			_array[iindex] = item;
		}

		public void InsertRange(int index, ICollection<T> items)
		{
			// Note: this is written so that the invariants hold if the
			// collection throws or returns an incorrect Count.
			int amount = items.Count;
			int iindex = InsertRangeHelper(index, amount);
			var it = items.GetEnumerator();
			for (int copied = 0; copied < amount; copied++)
			{
				if (!it.MoveNext())
					break;
				_array[iindex] = it.Current;
				iindex = IncMod(iindex);
			}
		}
		public void InsertRange(int index, ICollectionAndReadOnly<T> items)
		{
			InsertRange(index, (ICollection<T>)items);
		}
		public void InsertRange(int index, IReadOnlyCollection<T> items)
		{
			// Note: this is written so that the invariants hold if the
			// collection throws or returns an incorrect Count.
			int amount = items.Count;
			int iindex = InsertRangeHelper(index, amount);
			var it = items.GetEnumerator();
			for (int copied = 0; copied < amount; copied++)
			{
				if (!it.MoveNext())
					break;
				_array[iindex] = it.Current;
				iindex = IncMod(iindex);
			}
		}

		public int InsertRangeHelper(int index, int amount)
		{
			Debug.Assert((uint)index <= (uint)_count);

			if (amount <= 0)
				return InternalizeNC(index);
			AutoRaiseCapacity(amount);

			int deltaB = _count - index;
			if (index < deltaB)
			{
				_count += amount;
				return IH_InsertFront(index, amount);
			}
			else
			{
				if (index >= _count)
				{
					_count += amount;
					return InternalizeNC(index);
				}
				_count += amount;
				return IH_InsertBack(index, amount);
			}
		}

		private int IH_InsertFront(int index, int amount)
		{
			// Insert into front half. For example:
			// _array[20] before:     [e f g h i j k l m n o p _ _ _ _ a b c d]
			// (_count=16)                   ^index=7, amount=2        ^_start=16
			// CopyFwd 1st->1st half: [e f g h i j k l m n o p _ _ A B C D c d]
			//                                                  iTo^   ^iFrom
			// CopyFwd 2nd->1st half: [e f g h i j k l m n o p _ _ A B C D E F]
			//                         ^iFrom                           iTo^
			// CopyFwd 2nd->2nd half: [G f g h i j k l m n o p _ _ A B C D E F]
			//                      iTo^   ^iFrom
			// Space for new elems:   [G * * h i j k l m n o p _ _ A B C D E F]
			// (_count=18)               ^return value=1           ^_start=14
			_start = DecMod(_start, amount);
			if (index <= 0)
				return _start;
			
			T[] array = _array;
			int iTo = _start;
			int iFrom = iTo + amount;

			int left = index;
			int copyAmt = Min(array.Length - iFrom, left);
			if (copyAmt > 0) // 1st->1st
			{
				CopyFwd(array, iFrom, iTo, copyAmt);
				if ((left -= copyAmt) == 0)
					return iTo + copyAmt;
				iFrom += copyAmt;
				iTo += copyAmt;
			}
			Debug.Assert(iFrom >= array.Length);
			Debug.Assert(iTo < array.Length);
			iFrom -= array.Length;
			
			// 2nd->1st
			copyAmt = Min(array.Length - iTo, left);
			CopyFwd(array, iFrom, iTo, copyAmt);
			if ((left -= copyAmt) == 0)
				return iTo + copyAmt == array.Length ? 0 : iTo + copyAmt;
			iFrom += copyAmt;
			Debug.Assert(iTo + copyAmt == array.Length);
			iTo = 0;
			
			// 2nd->2nd
			copyAmt = left;
			CopyFwd(array, iFrom, iTo, copyAmt);
			return iTo + copyAmt;
		}
		private int IH_InsertBack(int index, int amount)
		{
			// Insert into back half. For example:
			// _array[20]:            [m n o p _ _ _ _ _ a b c d e f g h i j k l]
			// (_count=16)                               ^_start=9         ^index=9, amount=2
			// CopyBwd 2nd->2nd half: [m n M N O P _ _ _ a b c d e f g h i j k l]
			//                        iFrom-1^   ^iTo-1
			// CopyBwd 1st->2nd half: [K L M N O P _ _ _ a b c d e f g h i j k l]
			//                           ^iTo-1                                ^iFrom-1
			// CopyBwd 1st->1st half: [K L M N O P _ _ _ a b c d e f g h i j k J]
			//                                                      iFrom-1^   ^iTo-1
			// Space for new elems:   [K L M N O P _ _ _ a b c d e f g h i * * J]
			// (_count=14)                               ^_start=9         ^return value

			// Note: '_count' has already been increased by 'amount'
			int oldCount = _count - amount;
			T[] array = _array;
			int iTo = InternalizeNC(_count);
			int iFrom = InternalizeNC(oldCount);

			int left = oldCount - index, copyAmt;
			if (iTo <= _start)
			{
				if (iFrom < _start) // 2nd->2nd
				{
					copyAmt = Min(iFrom, left);
					CopyBwd(array, iFrom, iTo, copyAmt);
					if ((left -= copyAmt) == 0)
						return iFrom - copyAmt;
					Debug.Assert(copyAmt == iFrom);
					iFrom = array.Length;
					iTo -= copyAmt;
					Debug.Assert(iTo > 0);
				}
				// 1st->2nd
				copyAmt = Min(iTo, left);
				CopyBwd(array, iFrom, iTo, copyAmt);
				if ((left -= copyAmt) == 0)
					return iFrom - copyAmt;
				iFrom -= copyAmt;
				iTo = array.Length;
				Debug.Assert(iFrom-left > _start);
			}
			Debug.Assert(iFrom > _start && iTo > _start);
			Debug.Assert(iFrom < iTo);
			Debug.Assert(iFrom-left > _start);

			copyAmt = left;
			CopyBwd(array, iFrom, iTo, copyAmt);
			return iFrom - copyAmt;
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

		private static void CopyFwd(T[] array, int from, int to, int amount)
		{
			Debug.Assert(Math.Min(from, to) >= 0 && amount >= 0);
			Debug.Assert(Math.Max(from, to) + amount <= array.Length);
			Debug.Assert(to < from || to >= from + amount);
			int stop = to + amount;
			while (to < stop)
				array[to++] = array[from++];
		}
		private static void CopyBwd(T[] array, int fromPlusAmount, int toPlusAmount, int amount)
		{
			Debug.Assert(Math.Min(fromPlusAmount, toPlusAmount) >= amount && amount >= 0);
			Debug.Assert(Math.Max(fromPlusAmount, toPlusAmount) <= array.Length);
			Debug.Assert(toPlusAmount > fromPlusAmount || toPlusAmount <= fromPlusAmount-amount);
			int to = toPlusAmount - amount;
			while (toPlusAmount > to)
				array[--toPlusAmount] = array[--fromPlusAmount];
		}

		private void RemoveHelper(int index, int amount)
		{
			Debug.Assert((uint)index <= (uint)_count && (uint)(index + amount) <= (uint)_count);
			if (amount <= 0)
				return;

			T[] array = _array;
			int start = _start;

			int deltaB = _count - index;
			if (index < deltaB)
			{
				if (index > 0)
					RH_CollapseFront(index, amount);

				// Clear deleted elements (to be GC-friendly)
				ClearDeleted(ref _start, amount);
			}
			else
			{
				if (index < _count)
					RH_CollapseBack(index, amount);
				
				// Clear deleted elements (to be GC-friendly)
				int clearIndex = Internalize(_count - amount);
				ClearDeleted(ref clearIndex, amount);
			}
			_count -= amount;
			AutoShrink();
		}

		private void ClearDeleted(ref int start, int amount)
		{
			// start is an /internal/ index that is allowed to exceed the array 
			// length (wraps around). At the end of the method, 'start' points to 
			// the end of the range (with array.Length subtracted if it was 
			// out-of-range)
			T[] array = _array;				
			int adjusted = start + amount;
			if (adjusted >= array.Length) {
				adjusted -= array.Length;
				for (int i = start; (uint)i < array.Length; i++)
					array[(uint)i] = default(T);
				start = 0;
			}
			for (int i = start; i < adjusted; i++)
				array[i] = default(T);
			start = adjusted;
		}

		private void RH_CollapseFront(int index, int amount)
		{
			T[] array = _array;
			int start = _start;
			
			// Collapse front half. For example:
			// _array[20]:              [e f g h i j k l m n o p _ _ _ _ a b c d]
			// (_count=16)                     ^index=7, amount=2        ^_start=16
			//                         from-1^   ^to-1
			// CopyBwd within 2nd half: [e f E F G j k l m n o p _ _ _ _ a b c d]
			//                             ^to-1                         from-1^
			// CopyFwd 1st->2nd half:   [C D E F G j k l m n o p _ _ _ _ a b c d]
			//                                                       from-1^   ^to-1
			// copyFwd within 1st half: [C D E F G j k l m n o p _ _ _ _ a b A B]
			// Clear deleted elems:     [C D E F i j k l m n o p _ _ _ _ _ _ A B]
			// (_count=14)                                                   ^_start=18
			int from = start + index;
			int to = from + amount;
			if (to > array.Length) {
				to -= array.Length;
				if (from > array.Length) {
					from -= array.Length;
					CopyBwd(array, from, to, from);
					to -= from;
					from = array.Length;
				}
				if (to < from - start) {
					CopyBwd(array, from, to, to);
					from -= to;
					to = array.Length;
				}
			}
			Debug.Assert(from > _start && from <= array.Length && to <= array.Length);
			CopyBwd(array, from, to, from - start);
		}
		private void RH_CollapseBack(int index, int amount)
		{
			T[] array = _array;

			// Collapse back half. For example:
			// _array[20]:           [m n o p _ _ _ _ a b c d e f g h i j k l]
			// (_count=16)                            ^_start=8         ^index=9, amount=2
			// Copy within 1st half: [m n o p _ _ _ _ a b c d e f g h i L k l]
			//                                                        to^   ^from 
			// Copy 2nd->1st half:   [m n o p _ _ _ _ a b c d e f g h i L M N]
			//                        ^from                               ^to
			// Copy within 2nd half: [O P o p _ _ _ _ a b c d e f g h i L M N]
			//                        ^to ^from
			// Clear deleted elems:  [O P _ _ _ _ _ _ a b c d e f g h K L M N]
			// (_count=14)                                                ^_start=18

			int to = _start + index;
			int from = to + amount;
			int left = _count - index - amount;

			int copyAmt;
			if (from < array.Length)
			{
				copyAmt = Min(array.Length - from, left);
				CopyFwd(array, from, to, copyAmt);
				if ((left -= copyAmt) == 0)
					return;
				from += copyAmt;
				to += copyAmt;
			}
			from -= array.Length;

			if (to < array.Length)
			{
				copyAmt = Min(array.Length - to, left);
				CopyFwd(array, from, to, copyAmt);
				if ((left -= copyAmt) == 0)
					return;
				from += copyAmt;
				to += copyAmt;
			}
			to -= array.Length;

			copyAmt = left;
			Debug.Assert(to < from && from + left <= _start);
			CopyFwd(array, from, to, copyAmt);
		}

		static int Min(int x, int y)
		{
			return x + (((y - x) >> 31) & (y - x));
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
		public T this[int index, T defaultValue]
		{
			[DebuggerStepThrough]
			get {
				if ((uint)index < (uint)_count)
					return _array[Internalize(index)];
				else
					return defaultValue;
			}
		}
		public bool TrySet(int index, T value)
		{
			if ((uint)index >= (uint)_count)
				return false;
			_array[Internalize(index)] = value;
			return true;
		}
		public T TryGet(int index, out bool fail)
		{
			if ((uint)index < (uint)_count) {
				fail = false;
				return _array[Internalize(index)];
			} else {
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

		public void CopyTo(T[] destination, int arrayIndex)
		{
			Debug.Assert(destination != null && destination.Length >= _count);
			Debug.Assert(arrayIndex >= 0 && destination.Length - arrayIndex >= _count);
			int iindex = _start;
			for (int i = 0; i < _count; i++) {
				destination[i + arrayIndex] = _array[iindex];
				iindex = IncMod(iindex);
			}
		}

		public int Count
		{
			[DebuggerStepThrough]
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

		IRange<T> IListSource<T>.Slice(int start, int count)
		{
			return new Slice_<T>(this, start, count);
		}
		InternalDList<T> Slice(int start, int subcount)
		{
			CheckParam.IsNotNegative("start", start);
			CheckParam.IsNotNegative("subcount", subcount);
			if (start > _count)
				start = _count;
		    if (subcount > _count - start)
		        subcount = _count - start;

			return new InternalDList<T> { 
				_start = IncMod(_start, start),
				_count = subcount,
				_array = _array
			};
		}

		#region GetEnumerator

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<T> GetEnumerator() { return new Enumerator(this, null); }
		public IEnumerator<T> GetEnumerator(DList<T> wrapper) { return new Enumerator(this, wrapper); }

		class Enumerator : IEnumerator<T>
		{
			static readonly DList<T> NoWrapper = new DList<T>();

			int size1, stop, stop2, i, oldCount;
			T[] array;
			DList<T> wrapper;
			T _current;
			
			internal Enumerator(InternalDList<T> list, DList<T> wrapper)
			{
				size1 = list.FirstHalfSize;
				i = list._start;
				stop = i + size1;
				stop2 = list._count - size1;
				array = list._array;
				wrapper = wrapper ?? NoWrapper;
				this.wrapper = wrapper;
				oldCount = wrapper.Count;
			}

			public bool MoveNext()
			{
				while (i >= stop) {
					if (stop == stop2) {
						_current = default(T);
						return false;
					}
					stop = stop2;
					i = 0;
				}
				
				if (wrapper.Count != oldCount)
					throw new EnumerationException();

				_current = array[i++];
				return true;
			}

			public T Current { get { return _current; } }
		
			void  IDisposable.Dispose() { }
			object  System.Collections.IEnumerator.Current { get { return Current; } }
			void  System.Collections.IEnumerator.Reset() { throw new NotSupportedException(); }
		}

		#endregion

		#region IDeque<T>

		public T TryPopFirst(out bool isEmpty)
		{
			T value = TryPeekFirst(out isEmpty);
			if (!isEmpty)
				PopFirst(1);
			return value;
		}
		public T TryPeekFirst(out bool isEmpty)
		{
			if (isEmpty = (_count == 0))
				return default(T);
			return First;
		}
		public T TryPopLast(out bool isEmpty)
		{
			T value = TryPeekLast(out isEmpty);
			if (!isEmpty)
				PopLast(1);
			return value;
		}
		public T TryPeekLast(out bool isEmpty)
		{
			if ((isEmpty = (_count == 0)))
				return default(T);
			return Last;
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
			return BinarySearch(k, compare, false);
		}
		public int BinarySearch<K>(K k, Func<T, K, int> compare, bool lowerBound)
		{
			int low = 0;
			int high = _count - 1;
			int invert = -1;

			while (low <= high)
			{
				int mid = low + ((high - low) >> 1);
				T midk = this[mid];
				int c = compare(midk, k);
				if (c < 0)
					low = mid + 1;
				else
				{
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

		public InternalDList<T> Clone()
		{
			InternalDList<T> clone = new InternalDList<T>();
			clone._array = new T[Count];
			CopyTo(clone._array, 0);
			clone._start = 0;
			clone._count = Count;
			return clone;
		}

		public void CopyTo(int sourceIndex, T[] destination, int destinationIndex, int subcount)
		{
			Debug.Assert((uint)sourceIndex <= (uint)_count && (uint)subcount <= (uint)(_count - sourceIndex));
			
			int iindex = Internalize(sourceIndex);
			for (int i = 0; i < subcount; i++) {
				destination[i] = _array[iindex];
				iindex = IncMod(iindex);
			}
		}

		public InternalDList<T> CopySection(int start, int subcount)
		{
			Debug.Assert((uint)start <= (uint)_count && subcount >= 0);
			if (subcount > _count - start)
				subcount = _count - start;

			T[] copy = new T[subcount];
			CopyTo(start, copy, 0, subcount);
			return new InternalDList<T>(copy, subcount);
		}

		/// <summary>Returns a <see cref="DList{T}"/> wrapped around this list.</summary>
		/// <remarks>WARNING: in order to run in O(1) time, the two lists 
		/// (InternalDList and DList) share the same array, but not the same 
		/// internal state. You must stop using one list after modifying the 
		/// other, because changes to one list may have strange effects in
		/// the other list.</remarks>
		public DList<T> AsDList()
		{
			return new DList<T>(this);
		}

		public void Sort(Comparison<T> comp)
		{
			// Make the array contiguous so that we can use a fast array sort
			RearrangeToContiguous();
			InternalList.Sort(_array, _start, _count, comp);
		}
		private void RearrangeToContiguous()
		{
			if (IsDivided) {
				int blankStart = _start + _count - _array.Length;
				int blankCount = Math.Min(_array.Length - _count, _array.Length - _start);
				Array.Copy(_array, _array.Length - blankCount, _array, blankStart, blankCount);
				_start = 0;
				int i = _array.Length - blankCount;
				ClearDeleted(ref i, blankCount);
			}
		}

		public void Sort(int index, int count, Comparison<T> comp)
		{
			Debug.Assert((uint)index <= (uint)_count);
			Debug.Assert((uint)count <= (uint)_count - (uint)index);

			if (!IsDivided)
				InternalList.Sort(_array, _start + index, count, comp);
			else if (index == 0 && count == Count)
				Sort(comp);
			else 
				// Use a slower IList<T> sort because the array sort requires contiguous input
				ListExt.Sort(AsDList(), index, count, comp);
		}
	}
}
