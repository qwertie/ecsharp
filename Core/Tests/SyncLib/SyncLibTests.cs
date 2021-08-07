using Loyc.SyncLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.SyncLib.Tests
{
	public abstract class SyncLibTests<Manager>
	{
	}
	
	public abstract class SyncLibWriterTests<Writer> : SyncLibTests<Writer>
	{
	}
}
