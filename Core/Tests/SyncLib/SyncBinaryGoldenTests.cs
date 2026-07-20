using Loyc.Collections.Impl;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Loyc.SyncLib.Tests;

/// <summary>
///   Golden-vector tests for the SyncBinary wire format: every byte sequence
///   documented in SyncBinary.cs is checked in both directions from one shared
///   table - the writer must produce exactly these bytes, and the reader must
///   decode exactly these values. Vectors marked writerCheck=false are legal
///   non-canonical encodings that only the reader must accept. This doubles as
///   a tripwire against accidental wire-format changes.
/// </summary>
[TestFixture]
public class SyncBinaryGoldenTests : TestHelpers
{
	delegate object? GoldenSync(ISyncManager sm);

	class Golden
	{
		public Golden(string name, byte[] bytes, GoldenSync sync, object? expected,
			SyncBinary.Markers markers = SyncBinary.Markers.None, bool writerCheck = true)
		{
			Name = name; Bytes = bytes; Sync = sync; Expected = expected;
			Markers = markers; WriterCheck = writerCheck;
		}
		public string Name;
		public byte[] Bytes;
		public GoldenSync Sync;
		public object? Expected;
		public SyncBinary.Markers Markers;
		public bool WriterCheck;
	}

	static byte[] B(params int[] bytes) => bytes.Select(b => checked((byte)b)).ToArray();

	static List<Golden> Vectors() => new List<Golden> {
		// ---- Integer formats (SyncBinary.cs "Integers" section) ----
		new Golden("int 5 is one byte", B(0x05),
			sm => sm.Sync("f", 5), 5),
		new Golden("int -2 is 0b01111110", B(0x7E),
			sm => sm.Sync("f", -2), -2),
		new Golden("int -1 is 0x7F", B(0x7F),
			sm => sm.Sync("f", -1), -1),
		new Golden("int -3 canonical form", B(0x7D),
			sm => sm.Sync("f", -3), -3),
		new Golden("signed 72 is two bytes", B(0b10000000, 0b01001000),
			sm => sm.Sync("f", 72), 72),
		new Golden("unsigned 72 is one byte", B(0b01001000),
			sm => sm.Sync("f", 72u), 72u),
		new Golden("int 300 is 0x81 0x2C", B(0x81, 0x2C),
			sm => sm.Sync("f", 300), 300),
		new Golden("0x12345 is C1 23 45", B(0xC1, 0x23, 0x45),
			sm => sm.Sync("f", 0x12345), 0x12345),
		new Golden("null int? is 0xFF", B(0xFF),
			sm => sm.Sync("f", (int?)null), null),
		new Golden("reader accepts non-canonical -3", B(0b10111111, 0b11111101),
			sm => sm.Sync("f", -3), -3, writerCheck: false),
		new Golden("reader accepts length-prefixed 300", B(254, 2, 1, 44),
			sm => sm.Sync("f", 300), 300, writerCheck: false),

		// ---- Characters ----
		new Golden("char 'A' is 0x41", B(0x41),
			sm => sm.Sync("f", 'A'), 'A'),
		new Golden("char bullet is A0 22", B(0b1010_0000, 0b0010_0010),
			sm => sm.Sync("f", '•'), '•'),

		// ---- Booleans ----
		new Golden("true is 1", B(1), sm => sm.Sync("f", true), true),
		new Golden("false is 0", B(0), sm => sm.Sync("f", false), false),
		new Golden("null bool? is 0xFF", B(0xFF), sm => sm.Sync("f", (bool?)null), null),
		new Golden("int read as bool is true if nonzero", B(0x05),
			sm => sm.Sync("f", true), true, writerCheck: false),

		// ---- Floating point ----
		new Golden("float 1.0 is little-endian IEEE", B(0x00, 0x00, 0x80, 0x3F),
			sm => sm.Sync("f", 1.0f), 1.0f),
		new Golden("null float? is 'ahoy' NaN", B(0xE0, 0x68, 0xF3, 0xFF),
			sm => sm.Sync("f", (float?)null), null),
		new Golden("null double? is 'null' NaN", B(0xFE, 0x06, 0x6E, 0x75, 0x6C, 0x6C, 0xFE, 0xFF),
			sm => sm.Sync("f", (double?)null), null),

		// ---- Decimal (trailing zeros are significant: 7.00m != 7m on the wire) ----
		new Golden("decimal 7m", B(7, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0),
			sm => sm.Sync("f", 7m), 7m),
		new Golden("decimal 7.00m has exponent 2", B(0xBC, 2, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 2, 0),
			sm => sm.Sync("f", 7.00m), 7.00m),
		new Golden("decimal -7m has sign byte 0x80", B(7, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0x80),
			sm => sm.Sync("f", -7m), -7m),
		new Golden("null decimal? is 16 x 0xFF", Enumerable.Repeat((byte)0xFF, 16).ToArray(),
			sm => sm.Sync("f", (decimal?)null), null),

		// ---- Strings (length-prefixed WTF-8) ----
		new Golden("string Hello, null, smiley", B('[', 5, 'H', 'e', 'l', 'l', 'o', 0xFF, '[', 4, 0xF0, 0x9F, 0x98, 0x80),
			sm => new object?[] { sm.Sync("a", "Hello"), sm.Sync("b", (string?)null), sm.Sync("c", "😀") },
			new object?[] { "Hello", null, "😀" },
			markers: SyncBinary.Markers.ListStart),
		new Golden("empty string", B(0),
			sm => sm.Sync("f", ""), ""),

		// ---- Lists ----
		new Golden("int list without markers", B(4, 1, 10, 0x80, 100, 0b10000011, 0b11101000),
			sm => sm.SyncArray("f", new[] { 1, 10, 100, 1000 }), new[] { 1, 10, 100, 1000 }),
		new Golden("int list with start marker", B('[', 4, 1, 10, 0x80, 100, 0b10000011, 0b11101000),
			sm => sm.SyncArray("f", new[] { 1, 10, 100, 1000 }), new[] { 1, 10, 100, 1000 },
			markers: SyncBinary.Markers.ListStart),
		new Golden("null list is 0xFF", B(0xFF),
			sm => sm.SyncArray("f", (int[]?)null), null,
			markers: SyncBinary.Markers.ListStart),
		new Golden("empty list", B('[', 0),
			sm => sm.SyncArray("f", new int[0]), new int[0],
			markers: SyncBinary.Markers.ListStart),

		// ---- BigInteger via the length-prefixed format ----
		new Golden("BigInteger 2^64 uses 0xFE format", B(0xFE, 9, 1, 0, 0, 0, 0, 0, 0, 0, 0),
			sm => sm.Sync("f", (BigInteger)1 << 64), (BigInteger)1 << 64),
	};

	[Test]
	public void WriterProducesDocumentedBytes()
	{
		foreach (var g in Vectors().Where(g => g.WriterCheck)) {
			var options = new SyncBinary.Options { Markers = g.Markers, RootMode = ObjectMode.NotNull };
			var result = SyncBinary.Write<object?>(null, (sm, _) => { g.Sync(sm); return _; }, options).ToArray();
			if (!result.SequenceEqual(g.Bytes))
				Fail("Writer bytes differ for \"{0}\":\n  expected {1}\n  actual   {2}", g.Name,
					string.Join(",", g.Bytes.Select(b => b.ToString("X2"))),
					string.Join(",", result.Select(b => b.ToString("X2"))));
		}
	}

	[Test]
	public void ReaderDecodesDocumentedBytes()
	{
		foreach (var g in Vectors()) {
			var options = new SyncBinary.Options { Markers = g.Markers, RootMode = ObjectMode.NotNull };
			object? read = null;
			try {
				SyncBinary.Read<object?>(g.Bytes, (sm, _) => { read = g.Sync(sm); return _; }, options);
			} catch (Exception e) {
				Fail("Reader failed on \"{0}\": {1}", g.Name, e);
			}
			AssertEqualValues(g.Name, g.Expected, read);
		}
	}

	static void AssertEqualValues(string name, object? expected, object? actual)
	{
		if (expected is object?[] earr) {
			var aarr = actual as object?[];
			Assert.IsNotNull(aarr, "expected an array for \"{0}\"", name);
			Assert.AreEqual(earr.Length, aarr!.Length, "array length for \"{0}\"", name);
			for (int i = 0; i < earr.Length; i++)
				AssertEqualValues(name + "[" + i + "]", earr[i], aarr[i]);
		} else if (expected is Array ea) {
			var aa = actual as Array;
			Assert.IsNotNull(aa, "expected an array for \"{0}\"", name);
			Assert.AreEqual(ea.Length, aa!.Length, "array length for \"{0}\"", name);
			for (int i = 0; i < ea.Length; i++)
				Assert.AreEqual(ea.GetValue(i), aa.GetValue(i), "element {0} of \"{1}\"", i, name);
		} else {
			Assert.AreEqual(expected, actual, "value for \"{0}\"", name);
			// Trailing zeros of decimals are significant on the wire: 7m and 7.00m
			// are equal but must preserve their distinct scales
			if (expected is decimal ed)
				Assert.AreEqual(ed.ToString(), ((decimal)actual!).ToString(), "decimal scale for \"{0}\"", name);
		}
	}
}
