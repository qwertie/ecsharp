using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Math;
using Loyc.MiniTest;
using Loyc.Syntax;

namespace Loyc.Essentials.Tests
{
	[TestFixture]
	public class PrintHelpersTests : Assert
	{
		[Test]
		public void IntegerToStringTests()
		{
			AreEqual(PrintHelpers.IntegerToString(0, "", 10, 3, '_'), "0");
			AreEqual(PrintHelpers.IntegerToString(123, "0d", 10, 3, '_'), "0d123");
			AreEqual(PrintHelpers.IntegerToString(0x123, "0x", 16, 3, '_'), "0x123");
			AreEqual(PrintHelpers.IntegerToString(126uL, "0b", 2, 4, '_'), "0b111_1110");
			AreEqual(PrintHelpers.IntegerToString(9876, "", 10, 3, ','), "9,876");
			AreEqual(PrintHelpers.IntegerToString(-1234567, "0d", 10, 3, '\''), "-0d1'234'567");
			AreEqual(PrintHelpers.IntegerToString(-1234567, "0d", 10, 0, '\''), "-0d1234567");
			AreEqual(PrintHelpers.IntegerToString(-0x1234567890ABCD, "0x", 16, 4, '_'), "-0x12_3456_7890_ABCD");
			AreEqual(PrintHelpers.IntegerToString(0xFEEDFEEDFEEDFEEDuL, "0x", 16, 4, '_'), "0xFEED_FEED_FEED_FEED");
		}

		[Test]
		public void SeparatorsAreCountedFromTheLeastSignificantDigit()
		{
			// The '-' sign and the prefix must not be counted as digits.
			AreEqual("132",       PrintHelpers.IntegerToString(132, "", 10, 3, '_'));
			AreEqual("-132",      PrintHelpers.IntegerToString(-132, "", 10, 3, '_'));
			AreEqual("1_234",     PrintHelpers.IntegerToString(1234, "", 10, 3, '_'));
			AreEqual("-1_234",    PrintHelpers.IntegerToString(-1234, "", 10, 3, '_'));
			AreEqual("-1_000",    PrintHelpers.IntegerToString(-1000, "", 10, 3, '_'));
			AreEqual("1_234_567", PrintHelpers.IntegerToString(1234567, "", 10, 3, '_'));
			AreEqual("-1_234_567", PrintHelpers.IntegerToString(-1234567, "", 10, 3, '_'));
			AreEqual("-12_345_678", PrintHelpers.IntegerToString(-12345678, "", 10, 3, '_'));
			// Hex, grouped every 4 digits (boundaries: 4 and 8 digits)
			AreEqual("0xABCD",      PrintHelpers.IntegerToString(0xABCD, "0x", 16, 4, '_'));
			AreEqual("-0xABCD",     PrintHelpers.IntegerToString(-0xABCD, "0x", 16, 4, '_'));
			AreEqual("0x1_2345",    PrintHelpers.IntegerToString(0x12345, "0x", 16, 4, '_'));
			AreEqual("-0x1_2345",   PrintHelpers.IntegerToString(-0x12345, "0x", 16, 4, '_'));
			AreEqual("0x1234_5678", PrintHelpers.IntegerToString(0x12345678, "0x", 16, 4, '_'));
			AreEqual("-0x1234_5678", PrintHelpers.IntegerToString(-0x12345678, "0x", 16, 4, '_'));
			// Binary, grouped every 8 digits (boundaries: 8 and 16 digits)
			AreEqual("0b11111111",   PrintHelpers.IntegerToString(255, "0b", 2, 8, '_'));
			AreEqual("-0b11111111",  PrintHelpers.IntegerToString(-255, "0b", 2, 8, '_'));
			AreEqual("-0b1_00000000", PrintHelpers.IntegerToString(-256, "0b", 2, 8, '_'));
			AreEqual("-0b11111111_11111111", PrintHelpers.IntegerToString(-0xFFFF, "0b", 2, 8, '_'));
			// long.MinValue must not overflow into a wrong string
			AreEqual("-9_223_372_036_854_775_808", PrintHelpers.IntegerToString(long.MinValue, "", 10, 3, '_'));
		}

		[Test]
		public void UnescapedControlCharIsEmittedAsACharacter()
		{
			// Regression: `@out.Append(c)` where c is an int bound to
			// StringBuilder.Append(int), so an unescaped control character came out as
			// its decimal digits ("1" for U+0001) instead of the character itself.
			// EscapeC.Minimal does not include Control, so U+0001 must pass through.
			string input = "a\u0001b";
			AreEqual(input, PrintHelpers.EscapeCStyle(input, EscapeC.Minimal));
			// U+001F would have come out as the two characters "31"
			AreEqual("a\u001Fb", PrintHelpers.EscapeCStyle("a\u001Fb", EscapeC.Minimal));
			// With EscapeC.Control the character IS escaped, which was always correct
			AreEqual(@"a\u0001b", PrintHelpers.EscapeCStyle(input, EscapeC.Control));
			AreEqual(@"a\x01b", PrintHelpers.EscapeCStyle(input, EscapeC.Control | EscapeC.BackslashX));
		}

		[Test]
		public void EscapeCStyleReturnsOriginalStringWhenNothingEscaped()
		{
			// Regression: UString.PopFirst mutates the struct, so by the end of the loop
			// s.Length was 0 and the "nothing was escaped" fast path could never trigger.
			string plain = "hello world";
			IsTrue(object.ReferenceEquals(plain, PrintHelpers.EscapeCStyle(plain, EscapeC.Default)));
			// A slice of a larger string must NOT return the whole backing string
			AreEqual("ello", PrintHelpers.EscapeCStyle(((UString)plain).Substring(1, 4), EscapeC.Default));
			// And escaping still works
			AreEqual(@"a\nb", PrintHelpers.EscapeCStyle("a\nb", EscapeC.Default));
		}
	}
}
