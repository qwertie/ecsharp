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
	}
}
