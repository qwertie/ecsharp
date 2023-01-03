using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Loyc.SyncLib.Impl
{
	internal partial class WriterStateBase
	{
		protected IBufferWriter<byte> _output;
		//protected Memory<byte> _buf; // a sub-buffer returned from _output
		protected int _i = 0; // next index within _out to write

		protected ObjectIDGenerator _idGen = new ObjectIDGenerator(); // IDs start at one

		const int MinimumBufSize = 1024;

		protected Memory<byte> _buf;

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
		internal void Flush()
		{
			_output.Advance(_i);
			_i = 0;
		}
	}
}
