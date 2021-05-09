using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.SyncLib
{
	partial class SyncJson
	{
		public partial struct Writer// : ISyncManager
		{
			//static ObjectIDGenerator _idGen; // IDs start at one
			//SyncTypeRegistry _typeRegistry;
			//static Dictionary<int, object> _idTable;
			
			//System.Buffers.IBufferWriter<byte> _bufWriter;

			public Writer(System.Buffers.IBufferWriter<byte> bufWriter)
			{
			}
		}
	}
}
