using Loyc.Collections.Impl;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Loyc.SyncLib.Impl
{
	public partial class WriterStateBase
	{
		protected internal IBufferWriter<byte> _output;
		//protected Memory<byte> _buf; // a sub-buffer returned from _output
		protected int _i = 0; // next index within _out to write

		protected ObjectIDGenerator _idGen = new ObjectIDGenerator(); // IDs start at one

		const int MinimumBufSize = 1024;

		protected Memory<byte> _buf;
		// The array backing _buf, cached so GetOutSpan can build the output span directly
		// from the array instead of going through Memory<byte>.Span on every primitive
		// write. Memory<byte>.Span must branch on whether the memory wraps an array, a
		// string, or a MemoryManager (and calls MemoryManager.GetSpan for the last case),
		// which showed up as ~10 extra instructions per write in the JIT output. The
		// buffer always comes from IBufferWriter.GetMemory and is array-backed in practice;
		// the ToArray() path below is only a correctness fallback.
		private byte[]? _bufArray;
		private int _bufArrayOffset;

		public WriterStateBase(IBufferWriter<byte> output) => _output = output;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected Span<byte> GetOutSpan(int requiredBytes)
		{
			if (_i + requiredBytes < _buf.Length) {
				return new Span<byte>(_bufArray, _bufArrayOffset, _buf.Length);
			} else {
				return FlushAndGetOutSpan(requiredBytes);
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		protected Span<byte> FlushAndGetOutSpan(int requiredBytes)
		{
			Flush();
			_buf = _output.GetMemory(System.Math.Max(requiredBytes, MinimumBufSize));
			if (System.Runtime.InteropServices.MemoryMarshal.TryGetArray<byte>(_buf, out var seg)) {
				_bufArray = seg.Array;
				_bufArrayOffset = seg.Offset;
			} else {
				_bufArray = _buf.ToArray(); // fallback (shouldn't happen with ArrayBufferWriter)
				_bufArrayOffset = 0;
			}
			return new Span<byte>(_bufArray, _bufArrayOffset, _buf.Length);
		}
		public IBufferWriter<byte> Flush()
		{
			_output.Advance(_i);
			_i = 0;
			return _output;
		}
	}
}
