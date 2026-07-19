using Loyc.MiniTest;
using System;
using System.Linq;

namespace Loyc.SyncLib.Tests
{
	// Runs the whole shared SyncLibTests suite (value coverage, object round-trips,
	// generative fuzzing) against the Protobuf Reader/Writer pair. Because Protobuf keys
	// fields by integer ID, this also exercises the auto-numbering and reordering paths
	// that SyncBinary/SyncJson don't. Protobuf-specific tests are added below.
	public class SyncProtobufTests : SyncLibTests<SyncProtobuf.Reader, SyncProtobuf.Writer>
	{
		SyncProtobuf.Options _options = new SyncProtobuf.Options();
		ObjectMode _extraMode;    // extra root-mode flags for the nonDefaultSettings fixture
		ObjectMode _lastRootMode; // the root mode used by the last Write

		// The Protobuf format is binary, so its byte stream is not valid UTF-8.
		protected override bool IsUTF8 => false;

		public SyncProtobufTests(bool nonDefaultSettings)
		{
			if (nonDefaultSettings)
			{
				// A tiny initial buffer stresses the writer's buffer growth and in-place
				// length patching, and deduplicating the root exercises the dedup-wrapper
				// encoding on every test.
				_options = new SyncProtobuf.Options {
					Write = { InitialBufferSize = 1 },
				};
				_extraMode = ObjectMode.Deduplicate;
			}
		}

		protected override T Read<T>(byte[] data, SyncObjectFunc<SyncProtobuf.Reader, T> sync)
		{
			// In SyncProtobuf the Deduplicate flag changes the wire format, so unlike
			// SyncBinary (whose markers make the flag self-describing), the reader must
			// use the same root mode the writer used.
			_options.RootMode = _lastRootMode;
			return SyncProtobuf.Read<T>(data, sync, _options)!;
		}

		protected override byte[] Write<T>(T value, SyncObjectFunc<SyncProtobuf.Writer, T> sync, ObjectMode mode)
		{
			_options.RootMode = _lastRootMode = mode | _extraMode;
			return SyncProtobuf.Write(value, sync, _options).ToArray();
		}

		#region Protobuf-specific tests

		// Proves the wire format really is Protocol Buffers: the output for field 1 as a
		// varint of 150 is 0x08 0x96 0x01 — the canonical example from the Protobuf
		// encoding documentation — with no envelope or markers of any kind.
		[Test]
		public void WireFormat_IsRealProtobuf()
		{
			byte[] data = SyncProtobuf.Write<int>(150, (SyncProtobuf.Writer sm, int _) => {
				sm.Sync(("x", 1), 150);
				return 150;
			}).ToArray();

			ExpectList(data, new byte[] {
				0x08,             // field 1, wire type VARINT(0) -> (1<<3)|0
				0x96, 0x01,       // varint 150
			});
		}

		// A negative int uses a full 10-byte two's-complement varint, exactly like a
		// Protobuf int32/int64 negative value.
		[Test]
		public void WireFormat_NegativeIntIsTenByteVarint()
		{
			byte[] data = SyncProtobuf.Write<int>(0, (SyncProtobuf.Writer sm, int _) => {
				sm.Sync(("x", 1), -1);
				return 0;
			}).ToArray();

			ExpectList(data, new byte[] {
				0x08,
				0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01,
			});
		}

		// A list is a nested container message whose elements are packed into field 1 —
		// so the packed block itself is byte-identical to Protobuf's packed encoding.
		[Test]
		public void WireFormat_PackedList()
		{
			byte[] data = SyncProtobuf.Write(new[] { 1, 2, 3 }, (SyncProtobuf.Writer sm, int[]? v) => {
				sm.SyncList(("list", 1), v);
				return v!;
			}).ToArray();

			ExpectList(data, new byte[] {
				0x0A, 0x05,       // field 1 (the list container), LEN, 5 bytes
				0x0A, 0x03,       // field 1 within the container (packed items), LEN, 3 bytes
				0x01, 0x02, 0x03, // varints 1, 2, 3
			});
		}

		// A string field is a plain Protobuf string; a UTF-8 test.
		[Test]
		public void WireFormat_String()
		{
			byte[] data = SyncProtobuf.Write<string>("testing", (SyncProtobuf.Writer sm, string? _) => {
				sm.Sync(("s", 2), "testing");
				return "";
			}).ToArray();

			ExpectList(data, new byte[] {
				0x12, 0x07,       // field 2, LEN, 7 bytes
				(byte)'t', (byte)'e', (byte)'s', (byte)'t', (byte)'i', (byte)'n', (byte)'g',
			});
		}

		// A null root is zero bytes; a non-null root with no fields gets the reserved
		// _present marker so the two remain distinguishable.
		[Test]
		public void NullRootVersusEmptyRoot()
		{
			var options = new SyncProtobuf.Options();
			byte[] nullRoot = SyncProtobuf.Write<Person>(null!, new PersonSync<SyncProtobuf.Writer>().Sync, options).ToArray();
			ExpectList(nullRoot, new byte[0]);
			Assert.IsNull(SyncProtobuf.Read<Person>(nullRoot, new PersonSync<SyncProtobuf.Reader>().Sync, options));

			// An object whose every field is null writes no ordinary fields at all
			byte[] emptyRoot = SyncProtobuf.Write<object>(new object(), (SyncProtobuf.Writer sm, object? _) => {
				sm.Sync("a", (int?)null);
				return new object();
			}, options).ToArray();
			Assert.IsTrue(emptyRoot.Length > 0, "empty root must be marked as present");
			var read = SyncProtobuf.Read<(bool, int?)>(emptyRoot, (SyncProtobuf.Reader sm, (bool, int?) _) => {
				return (true, sm.Sync("a", (int?)null));
			}, options);
			Assert.AreEqual((true, (int?)null), read);
		}

		// A null nullable field is omitted entirely (absent == null), so it takes no bytes
		// while still keeping later fields' auto-assigned numbers aligned.
		[Test]
		public void NullFieldsAreOmitted()
		{
			byte[] withValue = SyncProtobuf.Write<int>(0, (SyncProtobuf.Writer sm, int _) => {
				sm.Sync("a", (int?)5); sm.Sync("b", 7); return 0;
			}).ToArray();
			byte[] withNull = SyncProtobuf.Write<int>(0, (SyncProtobuf.Writer sm, int _) => {
				sm.Sync("a", (int?)null); sm.Sync("b", 7); return 0;
			}).ToArray();

			Assert.IsTrue(withNull.Length < withValue.Length, "null field should be omitted");

			// Even though field "a" (auto id 1) is absent, "b" (auto id 2) still reads back.
			var read = SyncProtobuf.Read<(int?, int)>(withNull, (SyncProtobuf.Reader sm, (int?, int) _) => {
				int? a = sm.Sync("a", (int?)0);
				int b = sm.Sync("b", 0);
				return (a, b);
			});
			Assert.AreEqual((int?)null, read.Item1);
			Assert.AreEqual(7, read.Item2);
		}

		// Fields written in one order can be read in any other order, located by integer ID.
		[Test]
		public void FieldsCanBeReadOutOfOrder()
		{
			byte[] data = SyncProtobuf.Write<int>(0, (SyncProtobuf.Writer sm, int _) => {
				sm.Sync(("a", 1), 11);
				sm.Sync(("b", 2), 22);
				sm.Sync(("c", 3), 33);
				return 0;
			}).ToArray();

			var read = SyncProtobuf.Read<(int, int, int)>(data, (SyncProtobuf.Reader sm, (int, int, int) _) => {
				int c = sm.Sync(("c", 3), 0);
				int a = sm.Sync(("a", 1), 0);
				int b = sm.Sync(("b", 2), 0);
				return (a, b, c);
			});

			Assert.AreEqual((11, 22, 33), read);
		}

		// Per the Protobuf spec, when a non-repeated field occurs twice, the last wins.
		[Test]
		public void DuplicateFieldLastOneWins()
		{
			byte[] data = {
				0x08, 0x01, // field 1 = 1
				0x08, 0x02, // field 1 = 2 (again)
			};
			int read = SyncProtobuf.Read<int>(data, (SyncProtobuf.Reader sm, int _) => sm.Sync(("x", 1), 0));
			Assert.AreEqual(2, read);
		}

		// The reader reports NeedsIntegerIds, and NextField exposes the integer field IDs
		// (with auto-numbering starting at 1).
		[Test]
		public void ReaderExposesIntegerFieldIds()
		{
			byte[] data = SyncProtobuf.Write<int>(0, (SyncProtobuf.Writer sm, int _) => {
				Assert.IsTrue(sm.NeedsIntegerIds);
				sm.Sync("first", 5);
				sm.Sync("second", 6);
				return 0;
			}).ToArray();

			SyncProtobuf.Read<int>(data, (SyncProtobuf.Reader sm, int _) => {
				Assert.IsTrue(sm.NeedsIntegerIds);
				Assert.IsTrue(sm.SupportsReordering);
				Assert.IsTrue(sm.SupportsNextField);
				Assert.AreEqual(1, sm.NextField.Id);
				int a = sm.Sync(null, 0);
				Assert.AreEqual(2, sm.NextField.Id);
				int b = sm.Sync(null, 0);
				Assert.AreEqual(FieldId.Missing.Id, sm.NextField.Id);
				Assert.AreEqual(5, a);
				Assert.AreEqual(6, b);
				return 0;
			});
		}

		// Explicit integer field IDs (as PersonSync uses) survive a round trip, and gaps in
		// the numbering are fine.
		[Test]
		public void ExplicitFieldIdsWithGaps()
		{
			byte[] data = SyncProtobuf.Write<int>(0, (SyncProtobuf.Writer sm, int _) => {
				sm.Sync(("a", 5), 100);
				sm.Sync(("b", 900), 200);
				return 0;
			}).ToArray();

			var read = SyncProtobuf.Read<(int, int)>(data, (SyncProtobuf.Reader sm, (int, int) _) => {
				int a = sm.Sync(("a", 5), 0);
				int b = sm.Sync(("b", 900), 0);
				return (a, b);
			});
			Assert.AreEqual((100, 200), read);
		}

		// Field numbers that Protobuf does not permit are rejected up front.
		[Test]
		public void InvalidFieldNumbersAreRejected()
		{
			foreach (int badId in new[] { 0, -1, 19000, 19500, 19999, 536_870_910, 536_870_911, int.MaxValue }) {
				try {
					SyncProtobuf.Write<int>(0, (SyncProtobuf.Writer sm, int _) => {
						sm.Sync(("x", badId), 1);
						return 0;
					});
					Fail("Field number {0} should have been rejected", badId);
				} catch (ArgumentException) { }
			}
		}

		// byte[] is stored as a Protobuf `bytes` value, not as a list container.
		[Test]
		public void WireFormat_ByteArrayIsBytes()
		{
			byte[] data = SyncProtobuf.Write(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, (SyncProtobuf.Writer sm, byte[]? v) => {
				sm.SyncList(("blob", 1), v);
				return v!;
			}).ToArray();

			ExpectList(data, new byte[] {
				0x0A, 0x04,             // field 1, LEN, 4 bytes
				0xDE, 0xAD, 0xBE, 0xEF, // raw bytes, no per-byte encoding
			});
		}

		#endregion
	}
}
