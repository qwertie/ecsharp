using Loyc.Collections.Impl;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Loyc.SyncLib.Tests
{
	public abstract class SyncLibTests<Reader, Writer> : TestHelpers
		where Writer : ISyncManager
		where Reader : ISyncManager
	{
		protected abstract byte[] Write<T>(T value, SyncObjectFunc<Writer, T> sync, ObjectMode mode);
		protected abstract T? Read<T>(byte[] data, SyncObjectFunc<Reader, T> sync);
		protected virtual bool IsUTF8 => true;

		[Test]
		public void ExtremelyBasicTest()
		{
			RoundTripTest("example", StringSync, StringSync);
		}
		public string StringSync<SM>(SM sm, string? value) where SM : ISyncManager
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
			RoundTripTest(new StandardFields(110), new BigStandardModelSync<Writer>().Sync, new BigStandardModelSync<Reader>().Sync);
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

		[Test]
		public void RoundTripSmallObject()
		{
			var obj = new SmallObject {
				Field1 = 1,
				Field2 = "two",
				Field3 = 3.3,
			};
			RoundTripTest(obj, new SmallObjectSync<Writer>().Sync, new SmallObjectSync<Reader>().Sync, ObjectMode.Normal);
			RoundTripTest(obj, new SmallObjectSync<Writer>().Sync, new SmallObjectSync<Reader>().Sync, ObjectMode.Deduplicate);
		}

		[Test]
		public void RoundTripNumberArray()
		{
			var floats = new float[] {
				// The first 6 values are skipped on
				// the decimals round trip test, as they cannot 
				// be properly type casted.
				float.MinValue,
				float.MaxValue,
				float.PositiveInfinity,
				float.NegativeInfinity,
				float.Epsilon,
				float.NaN,
				0, -1,
				.1f, .2f, .3f, .4f, .5f, .6f, .7f, .8f, .9f, 1,
				1.1f, 1.2f, 1.3f, 1.4f, 1.5f, 1.6f, 1.7f, 1.8f, 1.9f, 2,
				999_999,
			};

			var doubles = floats.Select((n) => (double)n).ToList();
			doubles.Add(double.MinValue);
			doubles.Add(double.MaxValue);
			doubles.Add(double.Epsilon);

			var decimals = floats.Skip(6).Select((n) => (decimal)n).ToList();
			decimals.Add(1 / 3);
			decimals.Add(2 / 3);
			decimals.Add(1 / 5);
			decimals.Add(decimal.MinValue);
			decimals.Add(decimal.MaxValue);

			RoundTripTest<float[], float>(floats, SyncFloats, SyncFloats);
			RoundTripTest<List<double>, double>(doubles, SyncDoublesList, SyncDoublesList);
			RoundTripTest<List<decimal>, decimal>(decimals, SyncDecimalsList, SyncDecimalsList);

			static float[] SyncFloats<SM>(SM sm, float[]? value) where SM : ISyncManager
			{
				value = sm.SyncList("FloatsArray", value);
				return value!;
			}

			static List<double> SyncDoublesList<SM>(SM sm, List<double>? value) where SM : ISyncManager
			{
				value = sm.SyncList("DoublesList", value);
				return value!;
			}

			static List<decimal> SyncDecimalsList<SM>(SM sm, List<decimal>? value) where SM : ISyncManager
			{
				value = sm.SyncList("DecimalsList", value);
				return value!;
			}
		}

		protected void RoundTripTest<List, T>(List value, SyncObjectFunc<Writer, List> writer, SyncObjectFunc<Reader, List> reader, ObjectMode saveMode = 0) where List: IEnumerable<T>
			=> RoundTripTest(value, writer, reader, saveMode, (a, b) => ExpectList(a!, b));

		protected void RoundTripTest<T>(T value, SyncObjectFunc<Writer, T> writer, SyncObjectFunc<Reader, T> reader, ObjectMode saveMode = 0, Action<T?, T>? assertEquals = null)
		{
			var data = Write(value, writer, saveMode);
			// To aid debugging, get a string version of the written data
			var str = IsUTF8 ? Encoding.UTF8.GetString(data) : new string(data.Select(b => (char)b).ToArray());
			var value2 = Read(data, reader);

			var data2 = Write(value, writer, saveMode);
			var str2 = IsUTF8 ? Encoding.UTF8.GetString(data2) : new string(data2.Select(b => (char)b).ToArray());

			if (assertEquals is not null)
				assertEquals(value2, value);
			else
				Assert.AreEqual(value, value2);
			ExpectList(data, data2);
		}
	}
}
	