using Loyc.MiniTest;
using System;

namespace Loyc.SyncLib.Tests
{
	// Runs the whole shared SyncLibTests suite (value coverage, object round-trips,
	// generative fuzzing) against the Protobuf Reader/Writer pair. Because Protobuf keys
	// fields by integer ID, this also exercises the auto-numbering and reordering paths
	// that SyncBinary/SyncJson don't. Protobuf-specific tests are added below.
	public class SyncProtobufTests : SyncLibTests<SyncProtobuf.Reader, SyncProtobuf.Writer>
	{
		SyncProtobuf.Options _options = new SyncProtobuf.Options();
		ObjectMode _saveMode;

		// The Protobuf format is binary, so its byte stream is not valid UTF-8.
		protected override bool IsUTF8 => false;

		public SyncProtobufTests(bool nonDefaultSettings)
		{
			if (nonDefaultSettings)
			{
				// A tiny initial buffer stresses the writer's buffer growth and in-place
				// length back-patching; reading with RootMode=Deduplicate while most tests
				// write with RootMode=Normal exercises the "toggle the dedup flag" tolerance.
				_options = new SyncProtobuf.Options {
					Write = { InitialBufferSize = 1 },
				};
				_saveMode = ObjectMode.Deduplicate;
			}
		}

		protected override T Read<T>(byte[] data, SyncObjectFunc<SyncProtobuf.Reader, T> sync)
		{
			_options.RootMode = _saveMode;
			return SyncProtobuf.Read<T>(data, sync, _options)!;
		}

		protected override byte[] Write<T>(T value, SyncObjectFunc<SyncProtobuf.Writer, T> sync, ObjectMode mode)
		{
			_options.RootMode = mode;
			return SyncProtobuf.Write(value, sync, _options).ToArray();
		}

		#region Protobuf-specific tests

		// Proves the wire format really is Protocol Buffers: field 1 as a varint of 150 is
		// the canonical Protobuf example (0x08 0x96 0x01). The extra 0x0A/0x04 is SyncLib's
		// length-delimited root envelope, and 0x00 is the (no-dedup) object framing marker.
		[Test]
		public void WireFormat_IsRealProtobuf()
		{
			byte[] data = SyncProtobuf.Write<int>(150, (SyncProtobuf.Writer sm, int _) => {
				sm.Sync(("x", 1), 150);
				return 150;
			}).ToArray();

			ExpectList(data, new byte[] {
				0x0A,             // root tag: field 1, wire type LEN(2) -> (1<<3)|2
				0x04,             // root message length
				0x00,             // object framing marker: not deduplicated
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

			// root tag, len(12), marker, field tag, then ten varint bytes (0xFF*9, 0x01)
			ExpectList(data, new byte[] {
				0x0A, 0x0C, 0x00, 0x08,
				0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01,
			});
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

		#endregion
	}
}
