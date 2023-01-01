using Loyc.SyncLib.Impl;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.SyncLib;

partial class SyncBinary
{
	internal class WriterState : WriterStateBase
	{
		internal Options _opt;
		internal Options.ForWriter _optWrite;
		internal bool _isInsideList;

		public WriterState(IBufferWriter<byte> output, Options options) : base(output)
		{
			_opt = options;
			_optWrite = _opt.Write;
		}
	}
}
