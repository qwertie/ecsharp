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
		protected abstract byte[] Write<T>(T value, SyncObjectFunc<Writer, T> sync, ObjectMode mode);
		protected abstract T? Read<T>(byte[] data, SyncObjectFunc<Reader, T> sync);
		protected virtual bool IsUTF8 => true;

		[Test]
		public void ExtremelyBasicTest()
		{
			RoundTripTest("example", StringSync, StringSync);
		}
		public string StringSync<SM>(SM sm, string? value) where SM: ISyncManager
		{
			value = sm.Sync("value", value);
			return value!;
		}

		[Test]
		public void RoundTripNull()
		{
			RoundTripTest<Person?>(null, new PersonSync<Writer>().Sync, new PersonSync<Reader>().Sync);
		}

		[Test]
		public void RoundTripStandardFields()
		{
			RoundTripTest(new StandardFields(50), new BigStandardModelSync<Writer>().Sync, new BigStandardModelSync<Reader>().Sync);
		}

		[Test]
		public void RoundTripBigStandardModelNoMem()
		{
			RoundTripTest(new BigStandardModelNoMem(100), new BigStandardModelSync<Writer>().Sync, new BigStandardModelSync<Reader>().Sync);
		}
		
		[Test]
		public void RoundTripJackAndJill()
		{
			RoundTripTest(Jack(), new PersonSync<Writer>().Sync, new PersonSync<Reader>().Sync, ObjectMode.Deduplicate);
		}
		public static Person Jack()
		{
			var jack = new Person { Age = 11, Name = "Jack" };
			var jill = new Person { Age = 9, Name = "Jill", Siblings = new[] { jack } };
			jack.Siblings = new[] { jill };
			return jack;
		}

		protected void RoundTripTest<T>(T value, SyncObjectFunc<Writer, T> writer, SyncObjectFunc<Reader, T> reader, ObjectMode saveMode = 0)
		{
			var data = Write(value, writer, saveMode);
			// To aid debugging, get a string version of the written data
			var str = IsUTF8 ? Encoding.UTF8.GetString(data) : new string(data.Select(b => (char)b).ToArray());
			var value2 = Read(data, reader);

			var data2 = Write(value, writer, saveMode);
			var str2 = IsUTF8 ? Encoding.UTF8.GetString(data2) : new string(data2.Select(b => (char)b).ToArray());

			Assert.AreEqual(value, value2);
			ExpectList(data, data2);
		}
	}
}
	