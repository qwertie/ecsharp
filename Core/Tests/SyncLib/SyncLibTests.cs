using Loyc.MiniTest;
using Loyc.SyncLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.SyncLib.Tests
{
	public abstract class SyncLibTests<Reader, Writer>
	{
		[Test]
		public void RoundTripStandardFields()
		{
			var data = new StandardFields(50);
		}
	}
}
	