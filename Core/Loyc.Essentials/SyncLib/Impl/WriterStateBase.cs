using Loyc.Collections.Impl;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Loyc.SyncLib.Impl
{
	public partial class WriterStateBase
	{
		protected internal IBufferWriter<byte> _output;
		//protected Memory<byte> _buf; // a sub-buffer returned from _output
		protected int _i = 0; // next index within _out to write

		// Was System.Runtime.Serialization.ObjectIDGenerator, which Microsoft marked
		// obsolete in .NET 8 (SYSLIB0050). ObjectIdTable is behaviour-compatible:
		// reference identity, IDs starting at one, same GetId(obj, out firstTime) shape.
		protected internal ObjectIdTable _idGen = new ObjectIdTable();

		const int MinimumBufSize = 1024;

		protected Memory2<byte> _buf;

		public WriterStateBase(IBufferWriter<byte> output) => _output = output;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected Span<byte> GetOutSpan(int requiredBytes)
		{
			if (_i + requiredBytes < _buf.Length) {
				return _buf.Span;
			} else {
				return FlushAndGetOutSpan(requiredBytes);
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		protected Span<byte> FlushAndGetOutSpan(int requiredBytes)
		{
			Flush();
			_buf = _output.GetMemory(System.Math.Max(requiredBytes, MinimumBufSize));
			return _buf.Span;
		}
		public IBufferWriter<byte> Flush()
		{
			_output.Advance(_i);
			_i = 0;
			return _output;
		}
	}
}
