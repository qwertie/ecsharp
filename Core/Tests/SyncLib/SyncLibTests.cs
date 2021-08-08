using Loyc.MiniTest;
using Loyc.SyncLib;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Loyc.Collections.Impl;

namespace Loyc.SyncLib.Tests
{
	public abstract class SyncLibTests<Reader, Writer> : TestHelpers
		where Writer: ISyncManager
		where Reader: ISyncManager
	{
		protected abstract byte[] Write<T>(T value, SyncObjectFunc<Writer, T> sync);
		protected abstract T Read<T>(byte[] data, SyncObjectFunc<Reader, T> sync);
		protected virtual bool IsUTF8 => true;

		[Test]
		public void RoundTripNull()
		{
			RoundTripTest<StandardFields>(null, new BigStandardModelSync<Writer>().Sync, new BigStandardModelSync<Reader>().Sync);
		}
		
		[Test]
		public void RoundTripStandardFields()
		{
			RoundTripTest(new StandardFields(50), new BigStandardModelSync<Writer>().Sync, new BigStandardModelSync<Reader>().Sync);
		}

		private void RoundTripTest<T>(T value, SyncObjectFunc<Writer, T> writer, SyncObjectFunc<Reader, T> reader)
		{
			var data = Write(value, writer);
			// To aid debugging, get a string version of the written data
			var str = IsUTF8 ? Encoding.UTF8.GetString(data) : new string(data.Select(b => (char)b).ToArray());
			var value2 = Read(data, reader);

			var data2 = Write(value, writer);
			var str2 = IsUTF8 ? Encoding.UTF8.GetString(data2) : new string(data2.Select(b => (char)b).ToArray());

			Assert.AreEqual(value, value2);
			ExpectList(data, data2);
		}
	}
}
	