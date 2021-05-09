using Loyc.MiniTest;
using Loyc.SyncLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Essentials.Tests
{
	public abstract class SyncLibTests<Manager>
	{
	}
	
	public abstract class SyncLibWriterTests<Writer> : SyncLibTests<Writer>
	{
		[Test]
		public void Foo()
		{
			
		}
	}

	[TestFixture]
	public class SyncJsonWriterTests : SyncLibWriterTests<SyncJson.Writer>
	{
	}
}
