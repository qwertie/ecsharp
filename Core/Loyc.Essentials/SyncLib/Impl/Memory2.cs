using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Loyc.SyncLib.Impl;

/// <summary><see cref="Memory{T}.Span"/> is slower than you'd expect (even on modern 
///   .NET) because it holds an `object` reference and every call to `Span` must figure 
///   out what kind of object it is (there are three cases). This type exists to speed 
///   up that property in the usual case that it's an array.</summary>
public readonly struct Memory2<T>
{
	readonly Memory<T> _mem;
	readonly T[]? _array;
	readonly int _arrayOffset;

	public Memory2(Memory<T> mem)
	{
		_mem = mem;
		if (MemoryMarshal.TryGetArray((ReadOnlyMemory<T>)mem, out ArraySegment<T> seg)) {
			_array = seg.Array;
			_arrayOffset = seg.Offset;
		} else {
			_array = null;
			_arrayOffset = 0;
		}
	}

	public Span<T> Span {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _array != null
			? new Span<T>(_array, _arrayOffset, _mem.Length)
			: _mem.Span;
	}

	public int Length {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _mem.Length;
	}

	/// <summary>The underlying <see cref="Memory{T}"/>.</summary>
	public Memory<T> Memory => _mem;

	// Slice returns a plain Memory<T> (not another Memory2): see ReadOnlyMemory2.Slice.
	public Memory<T> Slice(int start) => _mem.Slice(start);
	public Memory<T> Slice(int start, int length) => _mem.Slice(start, length);

	public T[] ToArray() => _mem.ToArray();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Memory2<T>(Memory<T> mem) => new Memory2<T>(mem);
}

/// <summary><see cref="ReadOnlyMemory{T}.Span"/> is slower than you'd expect (even on 
///   modern .NET) because it holds an `object` reference and every call to `Span` must
///   figure out what kind of object it is (there are three cases). This type exists to 
///   speed up that property in the usual case that it's an array.</summary>
public readonly struct ReadOnlyMemory2<T>
{
	readonly ReadOnlyMemory<T> _mem;
	readonly T[]? _array;
	readonly int _arrayOffset;

	public ReadOnlyMemory2(ReadOnlyMemory<T> mem)
	{
		_mem = mem;
		if (MemoryMarshal.TryGetArray(mem, out ArraySegment<T> seg)) {
			_array = seg.Array;
			_arrayOffset = seg.Offset;
		} else {
			_array = null;
			_arrayOffset = 0;
		}
	}

	public ReadOnlySpan<T> Span {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _array != null
			? new ReadOnlySpan<T>(_array, _arrayOffset, _mem.Length)
			: _mem.Span;
	}

	public int Length {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _mem.Length;
	}

	/// <summary>The underlying <see cref="ReadOnlyMemory{T}"/>.</summary>
	public ReadOnlyMemory<T> Memory => _mem;

	// Slice returns a plain ReadOnlyMemory<T> (not another ReadOnlyMemory2): slices
	// are used to hand off sub-regions to code that expects the standard type, and the
	// per-access-Span optimization only matters for the buffer that is read in a tight
	// loop, not for these one-off sub-regions.
	public ReadOnlyMemory<T> Slice(int start) => _mem.Slice(start);
	public ReadOnlyMemory<T> Slice(int start, int length) => _mem.Slice(start, length);

	public T[] ToArray() => _mem.ToArray();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator ReadOnlyMemory2<T>(ReadOnlyMemory<T> mem) => new ReadOnlyMemory2<T>(mem);
}
