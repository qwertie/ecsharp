using Loyc.Collections;
using Loyc.Collections.Impl;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Loyc.Collections
{
	public struct ReadOnlyArraySlice<T> : IListSource<T>, ICloneable<ReadOnlyArraySlice<T>>, IIsEmpty, IScannable<T>
	{
		ReadOnlyMemory<T> _mem;

		public static implicit operator ReadOnlyArraySlice<T>(T[] array) { return new ReadOnlyArraySlice<T>(array); }
		public static implicit operator ReadOnlyArraySlice<T>(ReadOnlyMemory<T> array) { return new ReadOnlyArraySlice<T>(array); }
		public static implicit operator ReadOnlyArraySlice<T>(Memory<T> array) { return new ReadOnlyArraySlice<T>(array); }
		public static implicit operator ReadOnlyMemory<T>(ReadOnlyArraySlice<T> array) { return array._mem; }

		public ReadOnlyArraySlice(T[] list, int start, int count) : this(new ReadOnlyMemory<T>(list, start, count)) { }
		public ReadOnlyArraySlice(T[] list) : this(new ReadOnlyMemory<T>(list)) { }
		public ReadOnlyArraySlice(ReadOnlyMemory<T> mem) => _mem = mem;

		public T this[int index] => _mem.Span[index];

		public int Count => _mem.Length;

		public bool IsEmpty => _mem.Length == 0;

		public ReadOnlyArraySlice<T> Clone() => new ReadOnlyArraySlice<T>(_mem);

		public IEnumerator<T> GetEnumerator() => new InternalList.Enumerator<T>(_mem);
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		IListSource<T> IListSource<T>.Slice(int start, int count) => Slice(start, count);
		public ReadOnlyArraySlice<T> Slice(int start, int count = int.MaxValue)
		{
			if (count < 0)
				count = 0;
			if ((uint)(start + count) >= (uint)_mem.Length)
				return new ReadOnlyArraySlice<T>(_mem.Slice(start));
			return new ReadOnlyArraySlice<T>(_mem.Slice(start, count));
		}

		[return: MaybeNull]
		public T TryGet(int index, out bool fail)
		{
			if ((uint)index < (uint)_mem.Length) {
				fail = false;
				return _mem.Span[index];
			}
			fail = true;
			return default(T);
		}

		public InternalList.Scanner<T> Scan() => new InternalList.Scanner<T>(_mem);
		IScanner<T> IScan<T>.Scan() => Scan();
	}
}
