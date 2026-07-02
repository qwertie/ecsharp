using Loyc.Collections.Impl;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace Loyc.SyncLib.Tests;

[TestFixture]
public class SyncBinaryReaderTests : TestHelpers
{
	static byte[] Bytes(params object[] parts)
	{
		var list = new List<byte>();
		foreach (object part in parts) {
			if (part is char c)
				list.Add(checked((byte) c));
			else if (part is int i)
				list.Add(checked((byte) i));
			else if (part is byte[] arr)
				list.AddRange(arr);
			else if (part is string s)
				list.AddRange(Encoding.UTF8.GetBytes(s));
			else
				throw new ArgumentException("unexpected type in Bytes()");
		}
		return list.ToArray();
	}

	[Test]
	public void ReadStringsWithDeduplication()
	{
		// The example byte sequence documented in SyncBinary.cs (default: ListStart marker)
		var data = Bytes('#', 1, '[', 5, "Hello", 0xFF, '#', 2, '[', 4, "😀");
		var options = new SyncBinary.Options { Markers = SyncBinary.Markers.ListStart };

		var strings = SyncBinary.Read<string?[]>(data, (SyncBinary.Reader sm, string?[]? _) => new[] {
			sm.Sync("1", null, ObjectMode.Deduplicate),
			sm.Sync("2", null, ObjectMode.Deduplicate),
			sm.Sync("3", null, ObjectMode.Deduplicate),
		}, options)!;

		ExpectList(strings, "Hello", null, "😀");
	}

	[Test]
	public void ReadStringBackReferenceReturnsSameInstance()
	{
		var data = Bytes('#', 1, '[', 5, "Hello", ']', '@', 1);
		var options = new SyncBinary.Options { Markers = SyncBinary.Markers.Lists };

		var strings = SyncBinary.Read<string?[]>(data, (SyncBinary.Reader sm, string?[]? _) => new[] {
			sm.Sync("1", null, ObjectMode.Deduplicate),
			sm.Sync("2", null, ObjectMode.Deduplicate),
		}, options)!;

		Assert.AreEqual("Hello", strings[0]);
		Assert.AreSame(strings[0], strings[1]);
	}

	[Test]
	public void ReadDeduplicatedStringsWithoutMarkers()
	{
		// With markers disabled, the Deduplicate flag alone must tell the reader
		// to expect '#'/'@' prefixes.
		var options = new SyncBinary.Options { Markers = SyncBinary.Markers.None };
		var data = SyncBinary.Write(new object(), (sm, _) => {
			sm.Sync("1", "dup", ObjectMode.Deduplicate);
			sm.Sync("2", "dup", ObjectMode.Deduplicate);
			sm.Sync("3", "plain");
			return _;
		}, options).ToArray();

		var strings = SyncBinary.Read<string?[]>(data, (SyncBinary.Reader sm, string?[]? _) => new[] {
			sm.Sync("1", null, ObjectMode.Deduplicate),
			sm.Sync("2", null, ObjectMode.Deduplicate),
			sm.Sync("3", (string?) null),
		}, options)!;

		ExpectList(strings, "dup", "dup", "plain");
		Assert.AreSame(strings[0], strings[1]);
	}

	[Test]
	public void ReadBitfields_DocumentedExamples()
	{
		// Example from SyncBinary.cs: a 20-bit bitfield followed by a 4-bit bitfield
		var data = new byte[] { 0b00000011, 0b00000001, 0b11110000 };
		var options = new SyncBinary.Options { Markers = SyncBinary.Markers.None };

		var values = SyncBinary.Read<int[]>(data, (SyncBinary.Reader sm, int[]? _) => new[] {
			sm.Sync("a", 0, 20, true),
			sm.Sync("b", 0, 4, true),
		}, options)!;
		ExpectList(values, 259, -1);

		// Second example: 10-bit and 4-bit bitfields, then a normal variable-length int.
		// The top two bits of the second byte are padding and must be ignored.
		data = new byte[] { 0b00000011, 0b00111101, 0b00000111 };
		values = SyncBinary.Read<int[]>(data, (SyncBinary.Reader sm, int[]? _) => new[] {
			sm.Sync("a", 0, 10, true),
			sm.Sync("b", 0, 4, true),
			sm.Sync("c", 0),
		}, options)!;
		ExpectList(values, 259, -1, 7);
	}

	[Test]
	public void ReadBitfields_SignExtension()
	{
		// Documented claim: 0b1111_1111 read as 8 bits is -1 if signed, 255 if unsigned.
		// RootMode must be NotNull because the data starts with 0xFF, which would
		// otherwise be read as a null root object.
		var data = new byte[] { 0xFF, 0xFF };
		var options = new SyncBinary.Options {
			Markers = SyncBinary.Markers.None,
			RootMode = ObjectMode.NotNull,
		};

		var values = SyncBinary.Read<long[]>(data, (SyncBinary.Reader sm, long[]? _) => new[] {
			sm.Sync("a", 0L, 8, true),
			sm.Sync("b", 0L, 8, false),
		}, options)!;
		ExpectList(values, -1L, 255L);
	}

	[Test]
	public void RoundTripBitfields()
	{
		var options = new SyncBinary.Options { Markers = SyncBinary.Markers.None };
		BigInteger big = BigInteger.Parse("1234567890123456789012345678");
		BigInteger negBig = -big;

		var data = SyncBinary.Write(new object(), (sm, _) => {
			sm.Sync("a", 1234, 16, true);
			sm.Sync("b", -7, 5, true);
			sm.Sync("c", 0x123456789ABCDEFL, 61, true);
			sm.Sync("d", -40000000000L, 39, true);
			sm.Sync("e", big, 96, true);
			sm.Sync("f", negBig, 96, true);
			sm.Sync("g", 3, 2, false);
			return _;
		}, options).ToArray();

		SyncBinary.Read<object>(data, (SyncBinary.Reader sm, object? _) => {
			Assert.AreEqual(1234, sm.Sync("a", 0, 16, true));
			Assert.AreEqual(-7, sm.Sync("b", 0, 5, true));
			Assert.AreEqual(0x123456789ABCDEFL, sm.Sync("c", 0L, 61, true));
			Assert.AreEqual(-40000000000L, sm.Sync("d", 0L, 39, true));
			Assert.AreEqual(big, sm.Sync("e", default(BigInteger), 96, true));
			Assert.AreEqual(negBig, sm.Sync("f", default(BigInteger), 96, true));
			Assert.AreEqual(3, sm.Sync("g", 0, 2, false));
			return _!;
		}, options);
	}

	[Test]
	public void RoundTripTypeTag()
	{
		foreach (var markers in new[] { SyncBinary.Markers.Default, SyncBinary.Markers.None }) {
			var options = new SyncBinary.Options { Markers = markers };
			var data = SyncBinary.Write(new object(), (sm, _) => {
				sm.SyncTypeTag("MyType");
				sm.Sync("x", 42);
				return _;
			}, options).ToArray();

			SyncBinary.Read<object>(data, (SyncBinary.Reader sm, object? _) => {
				Assert.AreEqual("MyType", sm.SyncTypeTag(null));
				Assert.AreEqual(42, sm.Sync("x", 0));
				return _!;
			}, options);
		}
	}
}
