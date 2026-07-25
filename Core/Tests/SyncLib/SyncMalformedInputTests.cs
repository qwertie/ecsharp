using Loyc.Collections.Impl;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.SyncLib.Tests
{
	/// <summary>
	///   Malformed-input fuzzing of the readers: valid streams are corrupted with
	///   seeded random mutations (bit flips, truncations, insertions, deletions,
	///   splices, marker bytes) and pure-random streams, then fed to each reader.
	///   The contract being verified: the reader must terminate and may throw only
	///   the documented exception types - never IndexOutOfRangeException,
	///   NullReferenceException, ArgumentException or the like, and it must not
	///   allocate huge buffers based on corrupt length prefixes.
	///   Set SYNCLIB_FUZZ_ITERS to change the mutation count per corpus entry.
	/// </summary>
	[TestFixture]
	public class SyncMalformedInputTests : TestHelpers
	{
		// Exception types the readers are allowed to throw for corrupt input.
		// FormatException is the documented error type; InvalidCastException occurs
		// when a corrupt stream makes a typed value read back as null or a wrong type;
		// EndOfStreamException may be thrown by scanners.
		static readonly Type[] AllowedExceptions = {
			typeof(FormatException),
			typeof(InvalidCastException),
			typeof(OverflowException),
			typeof(System.IO.EndOfStreamException),
		};

		static int Iterations
			=> int.TryParse(Environment.GetEnvironmentVariable("SYNCLIB_FUZZ_ITERS"), out int n) ? n : 1000;

		static Person SyncPerson<SM>(SM sm, Person? p) where SM : ISyncManager
			=> new PersonSync<SM>().Sync(sm, p)!;

		static object SyncVarious<SM>(SM sm, object? _) where SM : ISyncManager
		{
			sm.Sync("int", 0);
			sm.Sync("str", "hello", ObjectMode.Deduplicate);
			sm.Sync("dup", "hello", ObjectMode.Deduplicate);
			sm.SyncArray("list", (int[]?)null);
			sm.Sync("big", default(System.Numerics.BigInteger));
			sm.Sync("dbl", 0.0);
			sm.SyncArray("bytes", (byte[]?)null);
			sm.Sync("nul", (int?)null);
			return _ ?? new object();
		}

		#region Corpus construction

		static List<byte[]> BinaryCorpus(SyncBinary.Options options)
		{
			var corpus = new List<byte[]>();
			corpus.Add(SyncBinary.Write(SyncLibTests<SyncBinary.Reader, SyncBinary.Writer>.Jack(),
				new PersonSync<SyncBinary.Writer>().Sync, options).ToArray());
			corpus.Add(SyncBinary.Write(new object(), SyncVarious, options).ToArray());
			corpus.Add(SyncBinary.Write(new int[] { 1, -1, int.MaxValue, int.MinValue },
				(sm, v) => sm.SyncArray("l", v)!, options).ToArray());
			return corpus;
		}

		static List<byte[]> ProtobufCorpus(SyncProtobuf.Options options)
		{
			var corpus = new List<byte[]>();
			corpus.Add(SyncProtobuf.Write(SyncLibTests<SyncProtobuf.Reader, SyncProtobuf.Writer>.Jack(),
				new PersonSync<SyncProtobuf.Writer>().Sync, options).ToArray());
			corpus.Add(SyncProtobuf.Write(new object(), SyncVarious, options).ToArray());
			corpus.Add(SyncProtobuf.Write(new int[] { 1, -1, int.MaxValue, int.MinValue },
				(sm, v) => sm.SyncArray("l", v)!, options).ToArray());
			return corpus;
		}

		static List<byte[]> JsonCorpus(SyncJson.Options options)
		{
			var corpus = new List<byte[]>();
			corpus.Add(SyncJson.Write(SyncLibTests<SyncJson.Reader, SyncJson.Writer>.Jack(),
				new PersonSync<SyncJson.Writer>().Sync, options).ToArray());
			corpus.Add(SyncJson.Write(new object(), SyncVarious, options).ToArray());
			corpus.Add(SyncJson.Write(new byte[] { 0, 92, 34, 8, 255 },
				(sm, v) => sm.SyncArray("b", v)!, options).ToArray());
			return corpus;
		}

		#endregion

		#region Mutation engine

		static byte[] Mutate(Random rng, byte[] original)
		{
			var data = (byte[])original.Clone();
			int mutations = rng.Next(1, 4);
			for (int m = 0; m < mutations; m++) {
				if (data.Length == 0)
					break;
				switch (rng.Next(7)) {
					case 0: // flip random bits
						data[rng.Next(data.Length)] ^= (byte)(1 << rng.Next(8));
						break;
					case 1: // overwrite with an interesting byte
						data[rng.Next(data.Length)] = new byte[] {
							0x00, 0x7F, 0x80, 0xC0, 0xE0, 0xF0, 0xFE, 0xFF,
							(byte)'#', (byte)'@', (byte)'[', (byte)']', (byte)'{', (byte)'}', (byte)'"', (byte)'\\',
						}[rng.Next(16)];
						break;
					case 2: // truncate
						data = data.Take(rng.Next(data.Length)).ToArray();
						break;
					case 3: // delete a byte
						data = data.Where((_, i) => i != rng.Next(data.Length)).ToArray();
						break;
					case 4: { // insert a random byte
						var list = data.ToList();
						list.Insert(rng.Next(data.Length + 1), (byte)rng.Next(256));
						data = list.ToArray();
						break;
					}
					case 5: { // duplicate a random slice
						int start = rng.Next(data.Length), len = rng.Next(1, System.Math.Min(9, data.Length - start + 1));
						var list = data.ToList();
						list.InsertRange(rng.Next(data.Length + 1), data.Skip(start).Take(len));
						data = list.ToArray();
						break;
					}
					case 6: // swap two bytes
						int a = rng.Next(data.Length), b = rng.Next(data.Length);
						(data[a], data[b]) = (data[b], data[a]);
						break;
				}
			}
			return data;
		}

		static void MutationFuzz(string label, List<byte[]> corpus, Action<byte[]> read)
		{
			int baseSeed = Environment.TickCount;
			int iterations = Iterations;
			foreach (byte[] original in corpus) {
				for (int i = 0; i < iterations; i++) {
					int caseSeed = baseSeed + i;
					var rng = new Random(caseSeed);
					byte[] data = Mutate(rng, original);
					CheckReadContract(label, caseSeed, data, read);
				}
			}

			// Pure-random streams
			for (int i = 0; i < iterations; i++) {
				int caseSeed = baseSeed - 1 - i;
				var rng = new Random(caseSeed);
				var data = new byte[rng.Next(0, 100)];
				rng.NextBytes(data);
				CheckReadContract(label, caseSeed, data, read);
			}
		}

		static void CheckReadContract(string label, int caseSeed, byte[] data, Action<byte[]> read)
		{
			try {
				read(data); // reading garbage "successfully" is fine; crashing is not
			} catch (Exception e) {
				for (Exception? x = e; x != null; x = x.InnerException)
					if (AllowedExceptions.Any(t => t.IsInstanceOfType(x)))
						return; // an allowed exception type (possibly wrapped)
				Fail("{0} reader broke its error contract on fuzzed input (seed {1}, {2} bytes: {3}): {4}",
					label, caseSeed, data.Length, string.Join(",", data.Take(48).Select(b => b.ToString("X2"))), e);
			}
		}

		#endregion

		[Test]
		public void FuzzBinaryReaderWithMutations()
		{
			foreach (var markers in new[] { SyncBinary.Markers.Default, SyncBinary.Markers.None, SyncBinary.Markers.All }) {
				var options = new SyncBinary.Options { Markers = markers };
				MutationFuzz("SyncBinary(" + markers + ")", BinaryCorpus(options), data => {
					var readOptions = new SyncBinary.Options { Markers = markers };
					SyncBinary.Read<Person>(data, new PersonSync<SyncBinary.Reader>().Sync, readOptions);
				});
			}
		}

		[Test]
		public void FuzzProtobufReaderWithMutations()
		{
			var options = new SyncProtobuf.Options();
			MutationFuzz("SyncProtobuf", ProtobufCorpus(options), data => {
				SyncProtobuf.Read<Person>(data, new PersonSync<SyncProtobuf.Reader>().Sync, new SyncProtobuf.Options());
			});
		}

		[Test]
		public void HandcraftedEvilProtobufInputs()
		{
			var options = new SyncProtobuf.Options();
			var evil = new List<byte[]> {
				new byte[0],                                  // a null root; reading it is fine
				new byte[] { 0x0A },                          // tag with no length
				new byte[] { 0x0A, 0x7F },                    // length 127 but no data
				new byte[] { 0x0A, 0x01 },                    // length 1 but no body byte
				new byte[] { 0x0A, 0x01, 0x02 },              // sub-message containing a bare 0x02
				new byte[] { 0x0A, 0x02, 0x01, 0xFF },        // sub-message with a truncated field
				new byte[] { 0x08, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, // huge varint at root
				new byte[] { 0x0A, 0x05, 0x00, 0x08, 0xFF, 0xFF, 0xFF }, // varint field value truncated
				Enumerable.Range(0, 10_000).Select(_ => (byte)0x0A).ToArray(), // deep nesting must not overflow the stack
			};
			foreach (byte[] data in evil)
				CheckReadContract("SyncProtobuf(evil)", 0, data,
					d => SyncProtobuf.Read<Person>(d, new PersonSync<SyncProtobuf.Reader>().Sync, options));

			// A corrupt list length prefix must fail fast rather than allocate or loop.
			var list = SyncProtobuf.Write(new int[] { 1, 2, 3 }, (SyncProtobuf.Writer sm, int[]? v) => sm.SyncArray("l", v)!, options).ToArray();
			for (int i = 0; i < list.Length; i++) {
				var corrupt = (byte[])list.Clone();
				corrupt[i] = 0xFF; // oversized length varints, bad tags, etc.
				CheckReadContract("SyncProtobuf(evil)", 100 + i, corrupt,
					d => SyncProtobuf.Read<int[]>(d, (SyncProtobuf.Reader sm, int[]? v) => sm.SyncArray("l", v)!, options));
			}
		}

		[Test]
		public void FuzzJsonReaderWithMutations()
		{
			foreach (bool newtonCompat in new[] { false, true }) {
				var options = new SyncJson.Options { NewtonsoftCompatibility = newtonCompat };
				MutationFuzz("SyncJson(newton:" + newtonCompat + ")", JsonCorpus(options), data => {
					var readOptions = new SyncJson.Options { NewtonsoftCompatibility = newtonCompat };
					SyncJson.Read<Person>(data, new PersonSync<SyncJson.Reader>().Sync, readOptions);
				});
			}
		}

		[Test]
		public void HandcraftedEvilBinaryInputs()
		{
			var options = new SyncBinary.Options { Markers = SyncBinary.Markers.Default };
			var evil = new List<byte[]> {
				new byte[0],
				new byte[] { (byte)'{' },                     // unterminated object
				new byte[] { (byte)'@', 0x05 },               // back-reference to nothing
				new byte[] { (byte)'#', 0xFF },               // null dedup id
				new byte[] { (byte)'{', 0xFE, 0xFF },         // number with null length prefix
				new byte[] { (byte)'{', 0xFE, 0xFE },         // length prefix that is itself prefixed
				new byte[] { (byte)'{', 0xFE, 0x7F },         // length prefix 127 with no data
				Enumerable.Repeat((byte)'{', 10_000).ToArray(), // deep nesting must not overflow the stack
			};
			foreach (byte[] data in evil)
				CheckReadContract("SyncBinary(evil)", 0, data,
					d => SyncBinary.Read<Person>(d, new PersonSync<SyncBinary.Reader>().Sync, options));

			// A corrupt list length must fail fast rather than allocate or loop
			var truncatedBigList = SyncBinary.Write(new int[] { 1, 2, 3 }, (SyncBinary.Writer sm, int[]? v) => sm.SyncArray("l", v)!, options)
				.ToArray();
			// Patch the length byte (value 3, following '{' '[') to 0x7F = 127
			int lengthIndex = Array.IndexOf(truncatedBigList, (byte)3);
			truncatedBigList[lengthIndex] = 0x7F;
			CheckReadContract("SyncBinary(evil)", 1, truncatedBigList,
				d => SyncBinary.Read<int[]>(d, (SyncBinary.Reader sm, int[]? v) => sm.SyncArray("l", v)!, options));
		}

		[Test]
		public void HandcraftedEvilJsonInputs()
		{
			var options = new SyncJson.Options();
			var evil = new List<string> {
				"", "{", "}", "[", "\"", "{\"Name\":", "{\"Name\":\"unterminated",
				"{\"Name\":\"bad escape \\q\"}",
				"{\"Name\":\"bad unicode \\u12\"}",
				"{\"Name\":1e99999999999999}",
				"{\"Name\":-}",
				"{\"Name\":\"x\", \"Name\":\"x\", \"Name\":\"x\"", // missing brace
				"{\"\\r\":99}",                                    // backref to nothing
				"{\"$ref\":\"nonexistent\"}",
				new string('[', 100_000),                          // deep nesting: MaxDepth, not stack overflow
				"{\"Age\":" + new string('9', 100_000) + "}",      // enormous number
			};
			foreach (string json in evil)
				CheckReadContract("SyncJson(evil)", 0, Encoding.UTF8.GetBytes(json),
					d => SyncJson.Read<Person>(d, new PersonSync<SyncJson.Reader>().Sync, options));

			// Invalid UTF-8 bytes inside a string
			CheckReadContract("SyncJson(evil)", 1, new byte[] { (byte)'{', (byte)'"', 0xC3, (byte)'"', (byte)':', (byte)'1', (byte)'}' },
				d => SyncJson.Read<Person>(d, new PersonSync<SyncJson.Reader>().Sync, options));
		}

		// Regression test for a denial-of-service bug found by FuzzJsonReaderWithMutations:
		// a null list element, or one that DetectTypeOfUnparsedValue misdetected as null (any
		// token starting with 'n') was returned by BeginSubObject WITHOUT advancing the reader.
		// The list-reading loop in ListLoader.Sync then iterated int.MaxValue times -
		// taking ~90 seconds and allocating gigabytes before finally terminating.
		[Test]
		public void Bug2026_07_NullLikeListElementFailsFast()
		{
			var options = new SyncJson.Options();
			var evil = new List<string> {
				"{\"Name\":\"x\",\"Age\":1,\"Siblings\":[n]}",              // bare 'n', minimal repro
				"{\"Name\":\"x\",\"Age\":1,\"Siblings\":[n \"Name\":\"y\"]}", // 'n' with trailing junk
				@"{""Name"":""x"",""Age"":1,""Siblings"":[null, ""y""]}",
				// The exact shape the fuzzer found: a sibling object's '{' overwritten by 'n'.
				"{\"Name\":\"Jack\",\"Age\":11,\"Siblings\":[n \"\\f\":1,\"Name\":\"Jill\",\"Age\":9,\"Siblings\":[]]}",
			};
			foreach (string json in evil) {
				var data = Encoding.UTF8.GetBytes(json);
				var timer = System.Diagnostics.Stopwatch.StartNew();
				CheckReadContract("SyncJson(null-in-list)", 0, data,
					d => SyncJson.Read<Person>(d, new PersonSync<SyncJson.Reader>().Sync, options));
				timer.Stop();
				// The bug took ~90 seconds; a healthy reader finishes in microseconds. The
				// 5-second bound cleanly separates the two without false-positives on slow CI.
				Less(timer.ElapsedMilliseconds, 5000,
					"Reading malformed list took too long ({0} ms): {1}", timer.ElapsedMilliseconds, json);
			}

			// Also, a genuine null list element must still read successfully. (This shape would
			// also have hung before the fix; it worked in practice only because no writer
			// emits a null element into an array of objects.)
			var jack = SyncJson.Read<Person>(
				Encoding.UTF8.GetBytes("{\"Name\":\"x\",\"Age\":1,\"Siblings\":[null,{\"Name\":\"y\",\"Age\":2,\"Siblings\":null}]}"),
				new PersonSync<SyncJson.Reader>().Sync, options);
			IsNotNull(jack);
			AreEqual(2, jack!.Siblings!.Length);
			IsNull(jack.Siblings[0]);
			AreEqual("y", jack.Siblings[1]!.Name);
		}
	}
}
