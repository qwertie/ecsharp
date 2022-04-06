using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Loyc.SyncLib.Impl
{
	internal partial class WriterStateBase
	{
		protected IBufferWriter<byte> _output;
		//protected Memory<byte> _buf; // a sub-buffer returned from _output
		protected int _i = 0; // next index within _out to write

		protected ObjectIDGenerator _idGen = new ObjectIDGenerator(); // IDs start at one
			
		public WriterStateBase(IBufferWriter<byte> output) => _output = output;
		protected Span<byte> GetOutBuf(int requiredBytes)
		{
			Flush();
			return _output.GetMemory(requiredBytes).Span;
		}
		internal void Flush()
		{
			_output.Advance(_i);
			_i = 0;
		}
	}
}
