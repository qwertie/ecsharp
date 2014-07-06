using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Loyc.Collections
{
	public static partial class Range
	{
		/// <summary>Returns <c>new IntRange(start, count)</c> (see <see cref="IntRange"/>).</summary>
		public static IntRange IntRange(int start, int count)
		{
			return new IntRange(start, count);
		}
	}

	/// <summary>Helper struct: treats a range of integers (e.g. 5..10) as a list. This type is returned by <see cref="Range.IntRange(int, int)"/>.</summary>
	public struct IntRange : IRange<int>, IListSource<int>, IList<int>, IIsEmpty
	{
		int _start, _count;

		public IntRange(int start, int count)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException("count", "IntRange: 'count' is less than zero.");
			_start = start;
			_count = count;
		}

		#region IListSource<T> Members

		public int TryGet(int index, out bool fail)
		{
			if ((uint)index < (uint)_count) {
				fail = false;
				return _start + index;
			}
			fail = true;
			return default(int);
		}

		public int Count
		{
			get { return _count; }
		}

		public int this[int index]
		{ 
			get {
				if ((uint)index >= (uint)_count)
					CheckParam.ThrowIndexOutOfRange(index, _count);
				return _start + index;
			}
		}
		
		public int IndexOf(int item)
		{
			int i = item - _start;
			if ((uint)i < (uint)_count)
				return i;
			return -1;
		}

		IRange<int> IListSource<int>.Slice(int start, int count)
		{
			return Slice(start, count); 
		}
		public IntRange Slice(int start, int count)
		{
			CheckParam.IsNotNegative("start", start);
			CheckParam.IsNotNegative("count", count);
			if (start > _count)
				count = 0;
			else if (count > _count - start)
				count = _count - start;
			return new IntRange(_start + start, count);
		}

		#endregion

		#region IList<T> Members

		int IList<int>.this[int index]
		{
			get { return this[index]; }
			set { throw new ReadOnlyException(); }
		}
		void IList<int>.Insert(int index, int item)
		{
			throw new ReadOnlyException();
		}
		void IList<int>.RemoveAt(int index)
		{
			throw new ReadOnlyException();
		}
		
		#endregion

		#region ICollection<int> Members

		void ICollection<int>.Add(int item)
		{
			throw new NotSupportedException("Collection is read-only.");
		}
		void ICollection<int>.Clear()
		{
			throw new NotSupportedException("Collection is read-only.");
		}
		void ICollection<int>.CopyTo(int[] array, int arrayIndex)
		{
			LCInterfaces.CopyTo(this, array, arrayIndex);
		}
		bool ICollection<int>.IsReadOnly
		{
			get { return true; }
		}
		bool ICollection<int>.Remove(int item)
		{
			throw new NotSupportedException("Collection is read-only.");
		}
		public bool Contains(int item)
		{
			return Enumerable.Contains(this, item);
		}

		#endregion

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		IEnumerator<int> IEnumerable<int>.GetEnumerator() { return GetEnumerator(); }
		public Enumerator GetEnumerator()
		{
			return new Enumerator(_start, _count);
		}

		#region IRange<int>

		public bool IsEmpty
		{
			get { return _count <= 0; }
		}

		public int Front
		{
			get { 
				if (_count <= 0) throw new EmptySequenceException(); 
				return _start; 
			}
		}
		public int Back
		{
			get { 
				if (_count <= 0) throw new EmptySequenceException(); 
				return _start + _count - 1; 
			}
		}

		public int PopFront(out bool fail)
		{
			if (!(fail = _count <= 0)) {
				_count--;
				return _start++;
			} else
				return default(int);
		}
		public int PopBack(out bool fail)
		{
			if (!(fail = _count <= 0))
				return _start + --_count;
			else
				return default(int);
		}

		IFRange<int> ICloneable<IFRange<int>>.Clone() { return Clone(); }
		IBRange<int> ICloneable<IBRange<int>>.Clone() { return Clone(); }
		IRange<int> ICloneable<IRange<int>>.Clone() { return Clone(); }
		public IntRange Clone()
		{
			return new IntRange(_start, _count);
		}

		#endregion

		public struct Enumerator : IEnumerator<int>
		{
			int _first, _cur, _last;

			internal Enumerator(int start, int count)
			{
				_first = start;
				_cur = _first - 1;
				_last = start + count - 1;
			}
			public int Current
			{
				get { return _cur; }
			}
			void IDisposable.Dispose() { }
			object System.Collections.IEnumerator.Current
			{
				get { return Current; }
			}
			public bool MoveNext()
			{
				if (_cur < _last) {
					_cur++;
					return true;
				}
				return false;
			}
			public void Reset()
			{
				_cur = _first - 1;
			}
		}
	}
}
