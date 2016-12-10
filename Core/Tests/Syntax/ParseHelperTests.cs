using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Math;
using Loyc.MiniTest;
using Loyc.Syntax;

namespace Loyc.Tests
{
	[TestFixture]
	public class ParseHelpersTests : Assert
	{
		[Test] public void TestUnescape()
		{
			Assert.AreEqual("", ParseHelpers.UnescapeCStyle(""));
			Assert.AreEqual("foobar", ParseHelpers.UnescapeCStyle("foobar"));
			Assert.AreEqual("foo\nbar", ParseHelpers.UnescapeCStyle(@"foo\nbar"));
			Assert.AreEqual("\u2222\n\r\t", ParseHelpers.UnescapeCStyle(@"\u2222\n\r\t"));
			Assert.AreEqual("\a\b\f\vA", ParseHelpers.UnescapeCStyle(@"\a\b\f\v\x41"));
			Assert.AreEqual("ba\\z", ParseHelpers.UnescapeCStyle((UString)@"ba\z", false));
			Assert.AreEqual("baz", ParseHelpers.UnescapeCStyle((UString)@"ba\z", true));
			Assert.AreEqual("!!\n!!", ParseHelpers.UnescapeCStyle(@"<'!!\n!!'>".Slice(2, 6), true));
		}

		[Test] public void TestUnescape2()
		{
			EscapeC encountered;
			Assert.AreEqual(@"abcd", ParseHelpers.UnescapeCStyle(@"abcd", out encountered, false).ToString());
			Assert.AreEqual(encountered, default(EscapeC));
			Assert.AreEqual("\0ab", ParseHelpers.UnescapeCStyle(@"\0abc".Slice(0, 4), out encountered, false).ToString());
			Assert.AreEqual(EscapeC.HasEscapes, encountered);
			Assert.AreEqual("a\bc", ParseHelpers.UnescapeCStyle(@"a\bc", out encountered, false).ToString());
			Assert.AreEqual(EscapeC.HasEscapes | EscapeC.ABFV, encountered);
			Assert.AreEqual("\n", ParseHelpers.UnescapeCStyle(@"\u000A", out encountered, false).ToString());
			Assert.AreEqual(EscapeC.HasEscapes | EscapeC.Control, encountered);
			Assert.AreEqual("xx\x88", ParseHelpers.UnescapeCStyle(@"xx\x88", out encountered, false).ToString());
			Assert.AreEqual(EscapeC.HasEscapes | EscapeC.NonAscii | EscapeC.BackslashX, encountered);
			Assert.AreEqual("\u0088"+"99", ParseHelpers.UnescapeCStyle(@"\x8899", out encountered, false).ToString());
			Assert.AreEqual(EscapeC.HasEscapes | EscapeC.NonAscii | EscapeC.BackslashX, encountered);
			Assert.AreEqual("ba\\z", ParseHelpers.UnescapeCStyle(@"ba\z", out encountered, false).ToString());
			Assert.AreEqual(EscapeC.HasEscapes | EscapeC.Unrecognized, encountered);
			Assert.AreEqual("baz", ParseHelpers.UnescapeCStyle(@"ba\z", out encountered, true).ToString());
			Assert.AreEqual(EscapeC.HasEscapes | EscapeC.Unrecognized, encountered);
		}

		[Test]
		public void TestTryParseInt()
		{
			TestParse(true, 10, "0", 0, 0, 1);
			TestParse(true, 10, "-0", 0, 0, 2);
			TestParse(true, 3, "0", 0, 0, 1, false);
			TestParse(true, 10, "123", 123, 0, 3);
			TestParse(true, 10, "??123", 123, 2, 5);
			TestParse(true, 10, "??  123abc", 123, 2, 7);
			TestParse(true, 10, "??\t 123  abc", 123, 2, 7);
			TestParse(true, 3, "210", 21, 0, 3);
			TestParse(true, 3, " \t 210", 21, 0, 6);
			TestParse(false,3, " \t 210", 0, 0, 0, false);
			TestParse(true, 10, "1 -2", -2, 1, 4, true);
			TestParse(false,10, "1 -2", 0, 1, 1, false);
			TestParse(true, 10, "1 -22", -22, 1, 5, true);
			TestParse(true, 4, "1 -22", -10, 1, 5, true);
			TestParse(false,4, "1 -22", 0, 1, 1, false);
			TestParse(true, 16, "F9", 0xF9, 0, 2);
			TestParse(true, 16, "f9", 0xF9, 0, 2);
			TestParse(true, 16, "abcdef", 0xabcdef, 0, 6);
			TestParse(true, 16, "ABCDEF", 0xabcdef, 0, 6);
			TestParse(true, 16, "abcdefgh", 0xabcdef, 0, 6);
			TestParse(true, 16, "ABCDEFGH", 0xabcdef, 0, 6);
			TestParse(true, 36, "az", 10*36+35, 0, 2);
			TestParse(true, 100, "AZ1234", 103501020304, 0, 6);
			TestParse(true, 100, " -AZ1234", -103501020304, 0, 8);
			string s;
			TestParse(true, 10, s = long.MaxValue.ToString(), long.MaxValue, 0, s.Length);
			TestParse(true, 10, s = long.MinValue.ToString(), long.MinValue, 0, s.Length);
			TestParse(true, 2, "111100010001000100010001000100010001", 0xF11111111, 0, 36);
			TestParse(false, 10, "", 0, 0, 0);
			TestParse(false, 10, "?!", 0, 0, 0);
			TestParse(false, 10, " eh?", 0, 0, 1);
			TestParse(false, 10, "123 eh?", 0, 3, 4);
			TestParse(false, 16, "10123456789abcdef", 0x0123456789abcdef, 0, 17);
			TestParse(false, 10, "- 1", 0, 0, 0, false);
			TestParse(true, 10, "- 1", -1, 0, 3);

			int i, result;
			i = 1;
			IsTrue(ParseHelpers.TryParseInt(" -AZ", ref i, out result, 100, false));
			AreEqual(-1035, result);
			AreEqual(i, 4);
			i = 0;
			IsFalse(ParseHelpers.TryParseInt(" -A123456Z", ref i, out result, 100, true));
			AreEqual(unchecked((int)-1001020304050635), result);
			AreEqual(i, 10);
			i = 1;
			IsTrue(ParseHelpers.TryParseInt(s = "0" + int.MinValue.ToString(), ref i, out result));
			AreEqual(int.MinValue, result);
			AreEqual(i, s.Length);
			i = 0;
			IsFalse(ParseHelpers.TryParseInt(s = ((long)int.MinValue - 1).ToString(), ref i, out result));
			AreEqual(int.MaxValue, result);
			AreEqual(i, s.Length);
		}
		private void TestParse(bool expectSuccess, int radix, string input, long expected, int i, int i_out, bool skipSpaces = true)
		{
			long result;
			UString input2 = input.Slice(i);
			bool success = ParseHelpers.TryParseInt(ref input2, out result, radix, 
				skipSpaces ? ParseNumberFlag.SkipSpacesInFront : 0);
			AreEqual(expected, result);
			AreEqual(expectSuccess, success);
			AreEqual(i_out, input2.InternalStart);
		}

		[Test]
		public void TestTryParseFloat()
		{
			// First, let's make sure it handles integers
			TestParse(false, 10, "  ", float.NaN, ParseNumberFlag.SkipSpacesInFront);
			TestParse(true, 10, "0", 0);
			TestParse(true, 10, "-0", 0);
			TestParse(true, 10, "123", 123);
			TestParse(true, 10, "  123", 123, ParseNumberFlag.SkipSpacesInFront);
			TestParse(false, 10, "??  123abc".Slice(2), 123, ParseNumberFlag.SkipSpacesInFront);
			TestParse(false, 10, "\t 123  abc", 123, ParseNumberFlag.SkipSpacesInFront);
			TestParse(false, 10, "3 21  abc", 3);
			TestParse(false, 10, "3 21  abc", 321, ParseNumberFlag.SkipSpacesInsideNumber);
			TestParse(true, 4, "210", 36);
			TestParse(true, 4, " \t 210", 36, ParseNumberFlag.SkipSpacesInFront);
			TestParse(false,4, " \t 210", float.NaN);
			TestParse(true, 10, "-2", -2);
			TestParse(true, 10, "-22", -22);
			TestParse(true, 4, "-22", -10);
			TestParse(false,4, "-248", -2);
			TestParse(false,8, "-248", -20);
			TestParse(false, 16, "ab_cdef", 0xab);
			TestParse(true, 16, "ab_cdef", 0xabcdef, ParseNumberFlag.SkipUnderscores);
			TestParse(true, 16, "_AB__CDEF_", 0xabcdef, ParseNumberFlag.SkipUnderscores);
			TestParse(false, 16, "_", float.NaN, ParseNumberFlag.SkipUnderscores);
			TestParse(false, 16, "aBcDeFgH", 0xabcdef);
			TestParse(true, 32, "av", 10*32+31);
			TestParse(true, 10, int.MaxValue.ToString(), (float)int.MaxValue);
			TestParse(true, 10, int.MinValue.ToString(), (float)int.MinValue);
			TestParse(true, 2, "111100010001000100010001000100010001", (float)0xF11111111);

			// Now for some floats...
			TestParse(true, 8, "0.4", 0.5f);
			TestParse(true, 10, "1.5", 1.5f);
			TestParse(true, 16, "2.C", 2.75f);
			TestParse(true, 2, "11.01", 3.25f);
			TestParse(false, 10, "123.456f", 123.456f);

			TestParse(true, 10, "+123.456e4", +123.456e4f);
			TestParse(true, 10, "-123.456e30", -123.456e30f);
			TestParse(true, 10, "123.456e+10", 123.456e+10f);
			TestParse(true, 10, "123.456e-10", 123.456e-10f);
			TestParse(false, 10, "123.456e-", float.NaN);
			TestParse(true, 10, "123.456p2", 123.456f * 4f);
			TestParse(true, 10, "123.456p+1", 123.456f * 2f);
			TestParse(true, 10, "123.456p-1", 123.456f * 0.5f);
			TestParse(false, 10, "123.456p*", float.NaN);
			TestParse(false, 10, "123.456p+", float.NaN);
			TestParse(true, 10, "123.456p-1e+3", 123456f * 0.5f);
			TestParse(false, 10, "123.456e+3p-1", 123456f); // this order is NOT supported
		
			TestParse(true, 16, "1.4", 1.25f);
			TestParse(true, 16, "123.456p12", (float)0x123456);
			TestParse(true, 16, "123.456p-12", (float)0x123456 / (float)0x1000000);
			TestParse(true, 16, "123p0e+1", (float)0x123 * 10f);
			TestParse(true, 2, "1111p+8", (float)0xF00);
			TestParse(true, 4, "32.10", 14.25f);
			TestParse(true, 4, "32.10e+4", 14.25f * 10000f);
			TestParse(true, 4, "32.10p+4", 14.25f * 16f);
			TestParse(true, 8, "32.10", 26.125f);
			TestParse(true, 8, "32.10e+4", 26.125f * 10000f);
			TestParse(true, 8, "32.10p+4", 26.125f * 16f);

			// Overflow, underflow, and truncation
			TestParse(true, 10, "9876543210", 9876543210f);
			TestParse(true, 10, "9876543210_98765.4321012", 987654321098765.4321012f, ParseNumberFlag.SkipUnderscores);
			TestParse(true, 10, "1e40", float.PositiveInfinity);
			TestParse(true, 10, "-1e40", float.NegativeInfinity);
			TestParse(true, 10, "-1e-50", 0);
			TestParse(true, 10, "9876543210e5000", float.PositiveInfinity);
			TestParse(false, 10, "9876543210e9876543210", float.NaN);
			TestParse(false, 10, "9876543210p+9876543210", float.NaN);
			TestParse(true, 2, "11110001000100010001000100010001.0001", (float)0xF11111111 / 16f);
			TestParse(true, 10, "9876543210_9876543210.12345", 98765432109876543210.12345f, ParseNumberFlag.SkipUnderscores);
			TestParse(true, 10, "12345.0123456789_0123456789", 12345.01234567890123456789f, ParseNumberFlag.SkipUnderscores);
		}

		private void TestParse(bool expectSuccess, int radix, UString input, float expected, ParseNumberFlag flags = 0)
		{
			float result = ParseHelpers.TryParseFloat(ref input, radix, flags);
			bool success = !float.IsNaN(result) && input.IsEmpty;
			AreEqual(expectSuccess, success);
			IsTrue(expected == result
			    || expected == MathEx.NextLower(result)
			    || expected == MathEx.NextHigher(result)
				|| float.IsNaN(expected) && float.IsNaN(result));
		}
	}
}
