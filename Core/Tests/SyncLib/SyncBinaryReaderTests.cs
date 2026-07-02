using Loyc.Collections.Impl;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Text;

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
}
