using Loyc.Collections.Impl;
using Loyc.MiniTest;
using Loyc.SyncLib.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Loyc.SyncLib.Tests
{
	/// <summary>
	///   Format-agnostic value coverage tests: every primitive type at its extremes,
	///   nullable variants, strings with difficult contents, nested/empty/null lists,
	///   enums, dates, dictionaries, deep nesting and deduplication. These run against
	///   every (Reader, Writer) pair registered in Program.cs, so a new ISyncManager
	///   implementation gets this whole suite by writing one small fixture subclass.
	/// </summary>
	public abstract partial class SyncLibTests<Reader, Writer>
		where Writer : ISyncManager
		where Reader : ISyncManager
	{
		/// <summary>Syncs each array element as a separate scalar field via
		///   DefaultSynchronizer, exercising the per-type Sync(FieldId, T) methods.</summary>
		protected static T[] SyncScalars<SM, T>(SM sm, T[]? value) where SM : ISyncManager
		{
			int len = sm.Sync("len", value?.Length ?? 0);
			var result = new T[len];
			for (int i = 0; i < len; i++)
				result[i] = DefaultSynchronizer.Sync(ref sm, "x" + i, value != null ? value[i] : default)!;
			return result;
		}

		protected void ScalarRoundTrip<T>(params T[] values)
			=> RoundTripTest<T[], T>(values, SyncScalars<Writer, T>, SyncScalars<Reader, T>);

		[Test]
		public void RoundTripIntegerScalarExtremes()
		{
			ScalarRoundTrip(sbyte.MinValue, (sbyte)-1, (sbyte)0, (sbyte)1, sbyte.MaxValue);
			ScalarRoundTrip(byte.MinValue, (byte)1, (byte)0x7F, (byte)0x80, byte.MaxValue);
			ScalarRoundTrip(short.MinValue, (short)-1, (short)0, (short)1, short.MaxValue);
			ScalarRoundTrip(ushort.MinValue, (ushort)1, (ushort)0x7FFF, (ushort)0x8000, ushort.MaxValue);
			ScalarRoundTrip(int.MinValue, -1, 0, 1, int.MaxValue);
			ScalarRoundTrip(uint.MinValue, 1u, 0x7FFF_FFFFu, 0x8000_0000u, uint.MaxValue);
			ScalarRoundTrip(long.MinValue, -1L, 0L, 1L, long.MaxValue);
			ScalarRoundTrip(ulong.MinValue, 1uL, 0x7FFF_FFFF_FFFF_FFFFuL, 0x8000_0000_0000_0000uL, ulong.MaxValue);
		}

		[Test]
		public void RoundTripOtherScalarExtremes()
		{
			ScalarRoundTrip(false, true);
			ScalarRoundTrip('\0', 'A', 'é', '中', '￿');
			ScalarRoundTrip(float.MinValue, float.MaxValue, float.Epsilon, float.NaN,
				float.PositiveInfinity, float.NegativeInfinity, 0f, -1.5f);
			ScalarRoundTrip(double.MinValue, double.MaxValue, double.Epsilon, double.NaN,
				double.PositiveInfinity, double.NegativeInfinity, 0.0, -1.5);
			ScalarRoundTrip(decimal.MinValue, decimal.MaxValue, decimal.Zero, decimal.MinusOne,
				0.000_000_1m, -12345.6789m);
			ScalarRoundTrip(BigInteger.Zero, BigInteger.MinusOne,
				BigInteger.Pow(2, 200), -BigInteger.Pow(2, 200),
				BigInteger.Parse("12345678901234567890123456789012345678901234567890"));
		}

		[Test]
		public void RoundTripNullableScalars()
		{
			ScalarRoundTrip<int?>(null, int.MinValue, 0, int.MaxValue, null);
			ScalarRoundTrip<long?>(null, long.MinValue, long.MaxValue);
			ScalarRoundTrip<float?>(null, float.NaN, 1.5f);
			ScalarRoundTrip<double?>(null, double.NegativeInfinity, -2.5);
			ScalarRoundTrip<decimal?>(null, decimal.MinValue, decimal.MaxValue);
			ScalarRoundTrip<bool?>(null, true, false);
			ScalarRoundTrip<char?>(null, 'x', '￿');
			ScalarRoundTrip<BigInteger?>(null, BigInteger.Pow(10, 40));
			ScalarRoundTrip<sbyte?>(null, sbyte.MinValue, sbyte.MaxValue);
			ScalarRoundTrip<byte?>(null, byte.MaxValue);
			ScalarRoundTrip<short?>(null, short.MinValue);
			ScalarRoundTrip<ushort?>(null, ushort.MaxValue);
			ScalarRoundTrip<uint?>(null, uint.MaxValue);
			ScalarRoundTrip<ulong?>(null, ulong.MaxValue);
		}

		[Test]
		public void RoundTripDifficultStrings()
		{
			ScalarRoundTrip<string?>(
				null,
				"",
				"plain ASCII",
				"quotes \" and 'apostrophes' and \\backslashes\\",
				"control chars: \0 \b \t \n \r  ",
				"slash/es and \f form feeds",
				"accented éàü, CJK 中文字, Hebrew עברית, emoji 😀🎉, combining é",
				"�￾￿", // specials and noncharacters
				new string('x', 10_000),
				string.Concat(Enumerable.Range(1, 127).Select(i => (char)i)));
		}

		[Test]
		public void RoundTripPrimitiveListExtremes()
		{
			// The SyncList extension methods are generated per element type, so each
			// type needs its own little generic sync function.
			RoundTripTest<sbyte[], sbyte>(new[] { sbyte.MinValue, (sbyte)0, sbyte.MaxValue }, LSbyte<Writer>, LSbyte<Reader>);
			RoundTripTest<byte[], byte>(new[] { byte.MinValue, (byte)0x80, byte.MaxValue }, LByte<Writer>, LByte<Reader>);
			RoundTripTest<short[], short>(new[] { short.MinValue, (short)0, short.MaxValue }, LShort<Writer>, LShort<Reader>);
			RoundTripTest<ushort[], ushort>(new[] { ushort.MinValue, (ushort)0x8000, ushort.MaxValue }, LUshort<Writer>, LUshort<Reader>);
			RoundTripTest<int[], int>(new[] { int.MinValue, 0, int.MaxValue }, LInt<Writer>, LInt<Reader>);
			RoundTripTest<uint[], uint>(new[] { uint.MinValue, 0x8000_0000u, uint.MaxValue }, LUint<Writer>, LUint<Reader>);
			RoundTripTest<long[], long>(new[] { long.MinValue, 0L, long.MaxValue }, LLong<Writer>, LLong<Reader>);
			RoundTripTest<ulong[], ulong>(new[] { ulong.MinValue, 0x8000_0000_0000_0000uL, ulong.MaxValue }, LUlong<Writer>, LUlong<Reader>);
			RoundTripTest<bool[], bool>(new[] { true, false, true, true, false }, LBool<Writer>, LBool<Reader>);
			RoundTripTest<char[], char>(new[] { '\0', 'a', 'Z', '￿', '中' }, LChar<Writer>, LChar<Reader>);
			RoundTripTest<BigInteger[], BigInteger>(new[] { BigInteger.Zero, BigInteger.Pow(2, 100), -BigInteger.Pow(2, 100) }, LBigInt<Writer>, LBigInt<Reader>);
			RoundTripTest<float[], float>(new[] { float.MinValue, float.NaN, float.MaxValue }, LFloat<Writer>, LFloat<Reader>);
			RoundTripTest<double[], double>(new[] { double.MinValue, double.NaN, double.MaxValue }, LDouble<Writer>, LDouble<Reader>);
			RoundTripTest<decimal[], decimal>(new[] { decimal.MinValue, decimal.Zero, decimal.MaxValue }, LDecimal<Writer>, LDecimal<Reader>);
		}

		static sbyte[] LSbyte<SM>(SM sm, sbyte[]? v) where SM : ISyncManager => sm.SyncArray("list", v)!;
		static byte[] LByte<SM>(SM sm, byte[]? v) where SM : ISyncManager => sm.SyncArray("list", v)!;
		static short[] LShort<SM>(SM sm, short[]? v) where SM : ISyncManager => sm.SyncArray("list", v)!;
		static ushort[] LUshort<SM>(SM sm, ushort[]? v) where SM : ISyncManager => sm.SyncArray("list", v)!;
		static int[] LInt<SM>(SM sm, int[]? v) where SM : ISyncManager => sm.SyncArray("list", v)!;
		static uint[] LUint<SM>(SM sm, uint[]? v) where SM : ISyncManager => sm.SyncArray("list", v)!;
		static long[] LLong<SM>(SM sm, long[]? v) where SM : ISyncManager => sm.SyncArray("list", v)!;
		static ulong[] LUlong<SM>(SM sm, ulong[]? v) where SM : ISyncManager => sm.SyncArray("list", v)!;
		static bool[] LBool<SM>(SM sm, bool[]? v) where SM : ISyncManager => sm.SyncArray("list", v)!;
		static char[] LChar<SM>(SM sm, char[]? v) where SM : ISyncManager => sm.SyncArray("list", v)!;
		static float[] LFloat<SM>(SM sm, float[]? v) where SM : ISyncManager => sm.SyncArray("list", v)!;
		static double[] LDouble<SM>(SM sm, double[]? v) where SM : ISyncManager => sm.SyncArray("list", v)!;
		static decimal[] LDecimal<SM>(SM sm, decimal[]? v) where SM : ISyncManager => sm.SyncArray("list", v)!;
		static BigInteger[] LBigInt<SM>(SM sm, BigInteger[]? v) where SM : ISyncManager => sm.SyncArray("list", v)!;

		[Test]
		public void RoundTripEmptyAndNullLists()
		{
			RoundTripTest<int[], int>(new int[0], LInt<Writer>, LInt<Reader>);
			RoundTripTest<int[]?>(null, SyncIntList<Writer>, SyncIntList<Reader>);

			static int[]? SyncIntList<SM>(SM sm, int[]? value) where SM : ISyncManager
				=> sm.SyncArray("list", value);
		}

		[Test]
		public void RoundTripNestedLists()
		{
			var value = new List<List<int>> {
				new List<int> { 1, 2, 3 },
				new List<int>(),
				new List<int> { int.MinValue, int.MaxValue },
			};
			RoundTripTest(value, SyncNested<Writer>, SyncNested<Reader>, 0, (a, b) => {
				Assert.AreEqual(b.Count, a!.Count);
				for (int i = 0; i < b.Count; i++)
					ExpectList(a[i], b[i]);
			});

			static List<List<int>> SyncNested<SM>(SM sm, List<List<int>>? value) where SM : ISyncManager
				=> sm.SyncList("outer", value,
					(SM sm2, List<int>? inner) => sm2.SyncList("inner", inner)!,
					ObjectMode.Normal)!;
		}

		[Test]
		public void RoundTripStringListWithNullsAndDuplicates()
		{
			var strings = new string?[] { "dup", null, "", "dup", "unique" };
			RoundTripTest<string?[], string?>(strings, SyncStrings<Writer>, SyncStrings<Reader>);

			static string?[] SyncStrings<SM>(SM sm, string?[]? value) where SM : ISyncManager
			{
				int len = sm.Sync("len", value?.Length ?? 0);
				var result = new string?[len];
				for (int i = 0; i < len; i++)
					result[i] = sm.Sync("s" + i, value != null ? value[i] : null);
				return result;
			}
		}

		[Test]
		public void RoundTripDeduplicatedStrings()
		{
			// Note: this asserts value equality only. Whether the encoding actually
			// deduplicates is format-specific (SyncBinary does; SyncJson currently
			// writes strings plainly), but both must round-trip the values.
			var strings = new string?[] { "repeat", "repeat", null, "repeat" };
			RoundTripTest<string?[], string?>(strings, SyncDedup<Writer>, SyncDedup<Reader>);

			static string?[] SyncDedup<SM>(SM sm, string?[]? value) where SM : ISyncManager
			{
				int len = sm.Sync("len", value?.Length ?? 0);
				var result = new string?[len];
				for (int i = 0; i < len; i++)
					result[i] = sm.Sync("s" + i, value != null ? value[i] : null, ObjectMode.Deduplicate);
				return result;
			}
		}

		enum Rainbow { Red, Orange, Yellow, Green, Blue, Indigo, Violet }
		[Flags] enum Toppings { None = 0, Cheese = 1, Mushrooms = 2, Olives = 4, All = 7 }

		[Test]
		public void RoundTripEnums()
		{
			ScalarRoundTrip((int)Rainbow.Green, (int)Toppings.All); // as their underlying type

			var enums = new[] { Rainbow.Red, Rainbow.Violet, (Rainbow)99 };
			RoundTripTest<Rainbow[], Rainbow>(enums, SyncEnums<Writer, Rainbow>, SyncEnums<Reader, Rainbow>);
			var flags = new[] { Toppings.None, Toppings.Cheese | Toppings.Olives, Toppings.All };
			RoundTripTest<Toppings[], Toppings>(flags, SyncEnums<Writer, Toppings>, SyncEnums<Reader, Toppings>);

			static E[] SyncEnums<SM, E>(SM sm, E[]? value) where SM : ISyncManager where E : struct, Enum
			{
				int len = sm.Sync("len", value?.Length ?? 0);
				var result = new E[len];
				for (int i = 0; i < len; i++)
					result[i] = new SyncEnumAsString<SM, E>().Sync(ref sm, "e" + i, value != null ? value[i] : default);
				return result;
			}
		}

		[Test]
		public void RoundTripDatesAndTimeSpans()
		{
			// Note: dates outside the OLE Automation range (0100-9999) can't be used
			// here because SyncDateAsDayNumber is lossy for them by design of OA dates
			var dates = new[] {
				new DateTime(2026, 6, 12, 13, 45, 59),
				new DateTime(1600, 2, 29),
				new DateTime(9999, 12, 31, 23, 59, 59),
			};
			RoundTripTest<DateTime[], DateTime>(dates, SyncDates<Writer>, SyncDates<Reader>);

			var spans = new[] {
				TimeSpan.Zero,
				TimeSpan.FromSeconds(90),
				TimeSpan.FromDays(-3650) + TimeSpan.FromSeconds(5),
			};
			RoundTripTest<TimeSpan[], TimeSpan>(spans, SyncSpans<Writer>, SyncSpans<Reader>);

			static DateTime[] SyncDates<SM>(SM sm, DateTime[]? value) where SM : ISyncManager
			{
				int len = sm.Sync("len", value?.Length ?? 0);
				var result = new DateTime[len];
				for (int i = 0; i < len; i++) {
					var d = value != null ? value[i] : default;
					result[i] = sm.SyncDateAsString("str" + i, d);
					var day = sm.SyncDateAsDayNumber("day" + i, d.Date);
					// When reading, result[i] holds the date that was just read
					Assert.AreEqual(result[i].Date, day);
				}
				return result;
			}
			static TimeSpan[] SyncSpans<SM>(SM sm, TimeSpan[]? value) where SM : ISyncManager
			{
				int len = sm.Sync("len", value?.Length ?? 0);
				var result = new TimeSpan[len];
				for (int i = 0; i < len; i++) {
					var t = value != null ? value[i] : default;
					result[i] = sm.SyncTimeAsString("str" + i, t);
					var secs = sm.SyncTimeAsSeconds("sec" + i, t);
					Assert.IsTrue(System.Math.Abs(result[i].TotalSeconds - secs.TotalSeconds) < 0.001);
				}
				return result;
			}
		}

		[Test]
		public void RoundTripDictionary()
		{
			var dict = new Dictionary<string, int> { ["one"] = 1, ["two"] = 2, ["minus"] = int.MinValue };
			RoundTripTest(dict, SyncStrIntDict<Writer>, SyncStrIntDict<Reader>, 0, (a, b) => {
				Assert.AreEqual(b.Count, a!.Count);
				foreach (var kvp in b)
					Assert.AreEqual(kvp.Value, a[kvp.Key]);
			});

			static Dictionary<string, int> SyncStrIntDict<SM>(SM sm, Dictionary<string, int>? value) where SM : ISyncManager
				=> sm.SyncDict("dict", value,
					(SM sm2, KeyValuePair<string, int> kvp) => new KeyValuePair<string, int>(
						sm2.Sync("k", kvp.Key)!, sm2.Sync("v", kvp.Value)),
					ObjectMode.Normal)!;
		}

		class Node
		{
			public int Value;
			public Node? Child;
		}

		[Test]
		public void RoundTripDeeplyNestedObjects()
		{
			const int Depth = 30;
			Node root = new Node { Value = 0 };
			Node tail = root;
			for (int i = 1; i < Depth; i++)
				tail = tail.Child = new Node { Value = i };

			RoundTripTest(root, SyncNode<Writer>, SyncNode<Reader>, 0, (a, b) => {
				for (int i = 0; i < Depth; i++, a = a!.Child, b = b!.Child) {
					Assert.IsNotNull(a);
					Assert.AreEqual(b!.Value, a!.Value);
				}
				Assert.IsNull(a);
				Assert.IsNull(b);
			});

			static Node SyncNode<SM>(SM sm, Node? node) where SM : ISyncManager
			{
				node ??= new Node();
				node.Value = sm.Sync("v", node.Value);
				node.Child = sm.Sync("child", node.Child, SyncNode<SM>, ObjectMode.Normal);
				return node;
			}
		}
	}
}
