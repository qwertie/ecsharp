using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Loyc.Collections.Impl;
using Loyc.Math;

namespace Loyc.Collections.MutableListExtensionMethods
{
	public static partial class IListExt
	{
		public static ArraySlice<T> Slice<T>(this T[] list, int start, int length = int.MaxValue)
		{
			return new ArraySlice<T>(list, start, length);
		}
	}
}

namespace Loyc.Collections
{
	/// <summary>Adapter: Provides access to a section of an array.</summary>
	/// <remarks>As of version 30.1, this is a wrapper around <see cref="Memory{T}"/>.</remarks>
	public struct ArraySlice<T> : IMRange<T>, ICloneable<ArraySlice<T>>, IIsEmpty
	{
		Memory<T> _mem;

		public static implicit operator ArraySlice<T>(T[] array) { return new ArraySlice<T>(array); }
		public static implicit operator ArraySlice<T>(Memory<T> array) { return new ArraySlice<T>(array); }
		public static implicit operator Memory<T>(ArraySlice<T> array) { return array._mem; }

		/// <summary>Initializes an array slice.</summary>
		/// <exception cref="ArgumentException">The start index was below zero.</exception>
		/// <remarks>The (start, count) range is allowed to be invalid, as long
		/// as 'start' is zero or above. 
		/// <ul>
		/// <li>If 'count' is below zero, or if 'start' is above the original Length, 
		/// the Count of the new slice is set to zero.</li>
		/// <li>if (start + count) is above the original Length, the Count of the new
		/// slice is reduced to <c>list.Length - start</c>.</li>
		/// </ul>
		/// </remarks>
		public ArraySlice(T[] list, int start, int count)
		{
			if ((uint)(start + count) > (uint)list.Length)
				count = list.Length - start;
			_mem = list.AsMemory(start, count);
		}
		public ArraySlice(T[] list) : this(list.AsMemory()) { }
		public ArraySlice(Memory<T> mem) => _mem = mem;

		public int Count => _mem.Length;
		public bool IsEmpty => _mem.Length == 0;
		public T First
		{
			get { return this[0]; }
			set { this[0] = value; }
		}
		public T Last
		{
			get { return this[Count - 1]; }
			set { this[Count - 1] = value; }
		}

		[return: MaybeNull] // There's no attribute like [return: MaybeNullIf("empty")]
		public T PopFirst(out bool empty)
		{
			if (Count != 0) {
				empty = false;
				var first = _mem.Span[0];
				_mem = _mem.Slice(1);
				return first;
			}
			empty = true;
			return default(T);
		}
		[return: MaybeNull] // There's no attribute like [return: MaybeNullIf("empty")]
		public T PopLast(out bool empty)
		{
			if (Count != 0) {
				empty = false;
				var last = _mem.Span[_mem.Length - 1];
				_mem = _mem.Slice(0, _mem.Length - 1);
				return last;
			}
			empty = true;
			return default(T);
		}

		IFRange<T> ICloneable<IFRange<T>>.Clone() { return this; }
		IBRange<T> ICloneable<IBRange<T>>.Clone() { return this; }
		IRange<T> ICloneable<IRange<T>>.Clone() { return this; }
		ArraySlice<T> ICloneable<ArraySlice<T>>.Clone() { return this; }

		IEnumerator<T> IEnumerable<T>.GetEnumerator() { return GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		public InternalList.Enumerator<T> GetEnumerator()
		{
			return new InternalList.Enumerator<T>(_mem);
		}

		public T this[int index]
		{
			get => _mem.Span[index];
			set => _mem.Span[index] = value;
		}
		public T this[int index, T defaultValue]
		{
			get { 
				if ((uint)index < (uint)_mem.Length)
					return _mem.Span[index];
				return defaultValue;
			}
		}
		[return: MaybeNull] // There's no attribute like [return: MaybeNullIf("fail")]
		public T TryGet(int index, out bool fail)
		{
			if ((uint)index < (uint)_mem.Length) {
				fail = false;
				return _mem.Span[index];
			}
			fail = true;
			return default(T);
		}
		IListSource<T> IListSource<T>.Slice(int start, int count) { return Slice(start, count); }
		public ArraySlice<T> Slice(int start, int count = int.MaxValue)
		{
			if (count < 0)
				count = 0;
			if ((uint)(start + count) >= (uint)_mem.Length)
				return new ArraySlice<T>(_mem.Slice(start));
			return new ArraySlice<T>(_mem.Slice(start, count));
		}

		public T[] ToArray() => _mem.ToArray();

		public Memory<T> AsMemory() => _mem;

		/// <summary>Returns the original array.</summary>
		/// <remarks>Ideally, to protect the array there would be no way to access
		/// its contents beyond the boundaries of the slice. However, the 
		/// reality in .NET today is that many methods accept "slices" in the 
		/// form of a triple (list, start index, count). In order to call such an
		/// old-style API using a slice, one must be able to extract the internal
		/// list and start index values.</remarks>
		[Obsolete("Please use MemoryMarshal.TryGetArray(this.AsMemory(), out var seg) and read seg.Array")]
		public T[]? InternalList {
			get {
				if (!MemoryMarshal.TryGetArray(_mem, out ArraySegment<T> seg))
					throw new InvalidOperationException();
				return seg.Array;
			}
		}
		[Obsolete("Please use MemoryMarshal.TryGetArray(this.AsMemory(), out var seg) and read seg.Offset")]
		public int InternalStart {
			get {
				if (!MemoryMarshal.TryGetArray(_mem, out ArraySegment<T> seg))
					throw new InvalidOperationException();
				return seg.Offset;
			}
		}
		[Obsolete("Please use MemoryMarshal.TryGetArray(this.AsMemory(), out var seg) and read seg.Offset + seg.Count")]
		public int InternalStop {
			get {
				if (!MemoryMarshal.TryGetArray(_mem, out ArraySegment<T> seg))
					throw new InvalidOperationException();
				return seg.Offset + seg.Count;
			}
		}
	}
}
