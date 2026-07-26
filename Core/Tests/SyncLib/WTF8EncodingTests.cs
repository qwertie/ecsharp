using System;
using System.Linq;
using System.Text;
using Loyc.Collections.Impl;
using Loyc.MiniTest;

namespace Loyc.SyncLib.Tests
{
	/// <summary>Tests <see cref="WTF8Encoding"/> and its use by SyncBinary: any
	///   string, even one with unpaired surrogates, must round-trip losslessly.</summary>
	[TestFixture]
	public class WTF8EncodingTests : TestHelpers
	{
		static readonly WTF8Encoding E = WTF8Encoding.Instance;

		static byte[] Encode(string s)
		{
			var bytes = new byte[E.GetByteCount(s.AsSpan())];
			AreEqual(bytes.Length, E.GetBytes(s.AsSpan(), bytes.AsSpan()));
			return bytes;
		}
		static string Decode(byte[] b) => E.GetString(b.AsSpan());

		[Test]
		public void MatchesUtf8ForWellFormedStrings()
		{
			foreach (var s in new[] {
				"", "hello", "héllo wörld", "߿ࠀ퟿�",
				"astral \U0001F600 pair", "\U0010FFFF", "mixed £5 → 𝔘𝔫𝔦𝔠𝔬𝔡𝔢",
			}) {
				ExpectList(Encode(s), Encoding.UTF8.GetBytes(s));
				AreEqual(s, Decode(Encoding.UTF8.GetBytes(s)));
				AreEqual(Encoding.UTF8.GetBytes(s).Length, E.GetByteCount(s.ToCharArray(), 0, s.Length));
			}
		}

		[Test]
		public void UnpairedSurrogatesRoundTrip()
		{
			foreach (var s in new[] {
				"\uD800", "\uDFFF", "a\uD955z", "\uDC01\uD802",  // lone low + lone high (reversed pair)
				"ends with high \uDBFF", "\uDEAD beef",
				"pair 😀 then lone \uD83D!",
			}) {
				var bytes = Encode(s);
				AreEqual(s, Decode(bytes));
				// and each unpaired surrogate costs 3 bytes, same as U+FFFD would
				AreEqual(Encoding.UTF8.GetByteCount(s), bytes.Length);
				// ...but Encoding.UTF8 destroys the surrogate, proving this test matters
				IsFalse(s == Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(s)));
			}
		}

		[Test]
		public void SurrogateByteSequencesDecode()
		{
			// ED A0 80 = U+D800; ED B0 90 = U+DC10; ED 9F BF = U+D7FF (plain UTF-8)
			ExpectList(Decode(new byte[] { 0xED, 0xA0, 0x80 }), "\uD800");
			ExpectList(Decode(new byte[] { 0x41, 0xED, 0xB0, 0x90, 0x42 }), "A\uDC10B");
			ExpectList(Decode(new byte[] { 0xED, 0x9F, 0xBF }), "퟿");
		}

		[Test]
		public void MalformedBytesBecomeReplacementChars()
		{
			// Truncated or invalid 0xED sequences must not crash or desync
			ExpectList(Decode(new byte[] { 0xED }), "�");
			ExpectList(Decode(new byte[] { 0xED, 0xA0 }), "�");
			ExpectList(Decode(new byte[] { 0xED, 0x41 }), "�A");
			ExpectList(Decode(new byte[] { 0x61, 0xED, 0xED, 0xA0, 0x80 }), "a�\uD800");
			// GetCharCount must agree with GetChars
			foreach (var b in new[] { new byte[] { 0xED }, new byte[] { 0xED, 0x41, 0xED, 0xA0, 0x80 } })
				AreEqual(Decode(b).Length, E.GetCharCount(b, 0, b.Length));
		}

		[Test]
		public void SyncBinaryStringsRoundTripThroughWtf8()
		{
			foreach (var s in new[] {
				"plain", "una\uD800ired", "\uDFFF\uD800", "emoji 😀 + lone \uDE00",
			}) {
				var data = SyncBinary.Write(s, (SyncBinary.Writer sync, string? v) => sync.Sync(null, v));
				var read = SyncBinary.Read<string>(data.ToArray(), (SyncBinary.Reader sync, string? v) => sync.Sync(null, v));
				AreEqual(s, read);
			}
		}

		[Test]
		public void SyncBinaryFloatsRoundTrip()
		{
			foreach (var f in new[] {
				0f, -0f, 1.5f, float.MaxValue, float.Epsilon, float.PositiveInfinity, (float)System.Math.PI,
			}) {
				var data = SyncBinary.Write(f, (SyncBinary.Writer sync, float v) => sync.Sync(null, v));
				var read = SyncBinary.Read<float>(data.ToArray(), (SyncBinary.Reader sync, float v) => sync.Sync(null, v));
				AreEqual(BitConverter.DoubleToInt64Bits(f), BitConverter.DoubleToInt64Bits(read));
			}
			foreach (var d in new[] {
				0.0, -0.0, 1.5, double.MaxValue, double.Epsilon, double.NegativeInfinity, System.Math.PI,
			}) {
				var data = SyncBinary.Write(d, (SyncBinary.Writer sync, double v) => sync.Sync(null, v));
				var read = SyncBinary.Read<double>(data.ToArray(), (SyncBinary.Reader sync, double v) => sync.Sync(null, v));
				AreEqual(BitConverter.DoubleToInt64Bits(d), BitConverter.DoubleToInt64Bits(read));
			}
		}
	}
}
