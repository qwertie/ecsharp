using Loyc.Collections.Impl;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Loyc.SyncLib.Tests
{
	public abstract partial class SyncLibTests<Reader, Writer> : TestHelpers
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
		public void RoundTripDeduplicatedLists()
		{
			// Bug fixed in Nov 2026: ListLoader didn't register a deduplicated list's ID, so
			// reading a second reference to the same list threw "no object found for
			// back-reference" (strings and byte lists were unaffected)
			var numbers = new[] { 1, 2, 3 };
			var names = new List<string?> { "a", "b" };
			var obj = new SharedLists { A = numbers, B = numbers, C = names, D = names };
			RoundTripTest(obj, SharedLists.Sync, SharedLists.Sync, ObjectMode.Normal, (result, expected) => {
				ExpectList(result!.A!, numbers);
				ExpectList(result.C!, names);
				Assert.That(object.ReferenceEquals(result.A, result.B), "the int[] was not deduplicated");
				Assert.That(object.ReferenceEquals(result.C, result.D), "the List<string> was not deduplicated");
			});
		}
		class SharedLists
		{
			public int[]? A, B;
			public List<string?>? C, D;

			public static SharedLists Sync<SM>(SM sm, SharedLists? obj) where SM : ISyncManager
			{
				obj ??= new SharedLists();
				var mode = ObjectMode.List | ObjectMode.Deduplicate;
				obj.A = sm.SyncArray("A", obj.A, mode);
				obj.B = sm.SyncArray("B", obj.B, mode);
				obj.C = sm.SyncList("C", obj.C, mode);
				obj.D = sm.SyncList("D", obj.D, mode);
				return obj;
			}
		}

		[Test]
		public void DefaultSynchronizerTreatsAllListsOfBytesAlike()
		{
			// Bug fixed in Nov 2026: DefaultSynchronizer gave byte[], char[] and bool[] the
			// sync manager's special treatment (e.g. raw bytes in SyncBinary, a string in
			// SyncJson), but synchronized other kinds of lists of these types one item at a
			// time, which differed from what SyncManagerExt.SyncList does.
			ExpectSameFormatAsSyncList(new byte[] { 1, 200, 255 }, (Writer sm, List<byte> v) => sm.SyncList("f", v)!);
			ExpectSameFormatAsSyncList(new[] { 'h', 'i', 'é' }, (Writer sm, List<char> v) => sm.SyncList("f", v)!);
			ExpectSameFormatAsSyncList(new[] { true, false, true }, (Writer sm, List<bool> v) => sm.SyncList("f", v)!);
		}
		void ExpectSameFormatAsSyncList<T>(T[] items, SyncObjectFunc<Writer, List<T>> syncList)
		{
			byte[] expected = Write(items.ToList(), syncList, ObjectMode.Normal);
			ExpectReadable(expected, items.ToList());
			ExpectReadable(expected, (IList<T>)items.ToList());
			ExpectReadable(expected, (IReadOnlyList<T>)items.ToList());

			void ExpectReadable<List>(byte[] expected, List list) where List : IEnumerable<T>
			{
				ExpectList(WriteViaDefaultSynchronizer(list), expected);
				var read = Read(expected, (Reader sm, List? v) => DefaultSynchronizer.Sync(ref sm, "f", v))!;
				ExpectList(read, items);
			}
		}
		byte[] WriteViaDefaultSynchronizer<T>(T value)
			=> Write(value, (Writer sm, T v) => DefaultSynchronizer.Sync(ref sm, "f", v)!, ObjectMode.Normal);

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
				value = sm.SyncArray("FloatsArray", value);
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

		[Test]
		public void RoundTripValueTuples()
		{
			RoundTripTest(ValueTuple.Create(42), SyncTuple<Writer, ValueTuple<int>>, SyncTuple<Reader, ValueTuple<int>>);
			RoundTripTest((42, "hi"), SyncTuple<Writer, (int, string)>, SyncTuple<Reader, (int, string)>);
			RoundTripTest((1, 2.5, "three"), SyncTuple<Writer, (int, double, string)>, SyncTuple<Reader, (int, double, string)>);
			RoundTripTest((1, 2, 3, 4, 5, 6, 7), SyncTuple<Writer, (int, int, int, int, int, int, int)>, SyncTuple<Reader, (int, int, int, int, int, int, int)>);
			// Arity 8+ uses a nested ValueTuple in `Rest`
			RoundTripTest((1, 2, 3, 4, 5, 6, 7, 8, 9), SyncTuple<Writer, (int, int, int, int, int, int, int, int, int)>, SyncTuple<Reader, (int, int, int, int, int, int, int, int, int)>);
			// Nested tuple as a tuple item
			RoundTripTest((1, (2.5, "deep")), SyncTuple<Writer, (int, (double, string))>, SyncTuple<Reader, (int, (double, string))>);
		}

		[Test]
		public void RoundTripNullableValueTuples()
		{
			// Non-null values round-tripped through the nullable ValueTuple<...>? overloads
			RoundTripTest<ValueTuple<int>?>(ValueTuple.Create(42), SyncTuple<Writer, ValueTuple<int>?>, SyncTuple<Reader, ValueTuple<int>?>);
			RoundTripTest<(int, string)?>((42, "hi"), SyncTuple<Writer, (int, string)?>, SyncTuple<Reader, (int, string)?>);
			RoundTripTest<(int, double, string)?>((1, 2.5, "three"), SyncTuple<Writer, (int, double, string)?>, SyncTuple<Reader, (int, double, string)?>);
			RoundTripTest<(int, int, int, int)?>((1, 2, 3, 4), SyncTuple<Writer, (int, int, int, int)?>, SyncTuple<Reader, (int, int, int, int)?>);
			RoundTripTest<(int, int, int, int, int)?>((1, 2, 3, 4, 5), SyncTuple<Writer, (int, int, int, int, int)?>, SyncTuple<Reader, (int, int, int, int, int)?>);
			RoundTripTest<(int, int, int, int, int, int)?>((1, 2, 3, 4, 5, 6), SyncTuple<Writer, (int, int, int, int, int, int)?>, SyncTuple<Reader, (int, int, int, int, int, int)?>);
			RoundTripTest<(int, int, int, int, int, int, int)?>((1, 2, 3, 4, 5, 6, 7), SyncTuple<Writer, (int, int, int, int, int, int, int)?>, SyncTuple<Reader, (int, int, int, int, int, int, int)?>);
			// Arity 8+ uses a nested ValueTuple in `Rest`
			RoundTripTest<(int, int, int, int, int, int, int, int, int)?>((1, 2, 3, 4, 5, 6, 7, 8, 9), SyncTuple<Writer, (int, int, int, int, int, int, int, int, int)?>, SyncTuple<Reader, (int, int, int, int, int, int, int, int, int)?>);
			// Nested tuple as a tuple item
			RoundTripTest<(int, (double, string))?>((1, (2.5, "deep")), SyncTuple<Writer, (int, (double, string))?>, SyncTuple<Reader, (int, (double, string))?>);

			// A null ValueTuple? should round-trip as null, at every arity
			RoundTripTest<ValueTuple<int>?>(null, SyncTuple<Writer, ValueTuple<int>?>, SyncTuple<Reader, ValueTuple<int>?>);
			RoundTripTest<(int, string)?>(null, SyncTuple<Writer, (int, string)?>, SyncTuple<Reader, (int, string)?>);
			RoundTripTest<(int, double, string)?>(null, SyncTuple<Writer, (int, double, string)?>, SyncTuple<Reader, (int, double, string)?>);
			RoundTripTest<(int, int, int, int)?>(null, SyncTuple<Writer, (int, int, int, int)?>, SyncTuple<Reader, (int, int, int, int)?>);
			RoundTripTest<(int, int, int, int, int)?>(null, SyncTuple<Writer, (int, int, int, int, int)?>, SyncTuple<Reader, (int, int, int, int, int)?>);
			RoundTripTest<(int, int, int, int, int, int)?>(null, SyncTuple<Writer, (int, int, int, int, int, int)?>, SyncTuple<Reader, (int, int, int, int, int, int)?>);
			RoundTripTest<(int, int, int, int, int, int, int)?>(null, SyncTuple<Writer, (int, int, int, int, int, int, int)?>, SyncTuple<Reader, (int, int, int, int, int, int, int)?>);
			RoundTripTest<(int, int, int, int, int, int, int, int, int)?>(null, SyncTuple<Writer, (int, int, int, int, int, int, int, int, int)?>, SyncTuple<Reader, (int, int, int, int, int, int, int, int, int)?>);
		}

		[Test]
		public void RoundTripReferenceTuples()
		{
			RoundTripTest(Tuple.Create(42, "hi"), SyncTuple<Writer, Tuple<int, string>>, SyncTuple<Reader, Tuple<int, string>>);
			RoundTripTest(Tuple.Create(1.5f, 2.5, "three", 4L), SyncTuple<Writer, Tuple<float, double, string, long>>, SyncTuple<Reader, Tuple<float, double, string, long>>);
			// A null Tuple should round-trip as null
			RoundTripTest<Tuple<int, string>?>(null, SyncTuple<Writer, Tuple<int, string>?>, SyncTuple<Reader, Tuple<int, string>?>);
		}

		protected static T SyncTuple<SM, T>(SM sm, T? value) where SM : ISyncManager
			=> DefaultSynchronizer.Sync(ref sm, "tuple", value)!;

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
	