using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Loyc.Collections
{
	public struct MemoryComparer<T> : IEqualityComparer<Memory<T>>, IEqualityComparer<ReadOnlyMemory<T>> where T: IEquatable<T>
	{
		public bool Equals(Memory<T> x, Memory<T> y) 
			=> MemoryExtensions.SequenceEqual((ReadOnlySpan<T>) x.Span, (ReadOnlySpan<T>) y.Span);
		public bool Equals(ReadOnlyMemory<T> x, ReadOnlyMemory<T> y) 
			=> MemoryExtensions.SequenceEqual((ReadOnlySpan<T>) x.Span, (ReadOnlySpan<T>) y.Span);

		public int GetHashCode(Memory<T> obj) => ListExt.SequenceHashCode((ReadOnlySpan<T>) obj.Span);
		public int GetHashCode(ReadOnlyMemory<T> obj) => ListExt.SequenceHashCode(obj.Span);
	}

	public struct ByteComparer : IEqualityComparer<Memory<byte>>, IEqualityComparer<ReadOnlyMemory<byte>>
	{
		public bool Equals(Memory<byte> x, Memory<byte> y) 
			=> MemoryExtensions.SequenceEqual((ReadOnlySpan<byte>) x.Span, (ReadOnlySpan<byte>) y.Span);
		public bool Equals(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y) 
			=> MemoryExtensions.SequenceEqual((ReadOnlySpan<byte>) x.Span, (ReadOnlySpan<byte>) y.Span);

		public int GetHashCode(Memory<byte> obj) => ListExt.SequenceHashCode((ReadOnlySpan<byte>) obj.Span);
		public int GetHashCode(ReadOnlyMemory<byte> obj) => ListExt.SequenceHashCode(obj.Span);
	}
}
