using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LeMP.Tests
{
	[TestFixture]
	public class TestFilterSyntaxMacros : MacroTesterBase
	{
		[Test]
		public void SimpleTests()
		{
			TestLes("list = ##filter([a.b, b.c, a.c], a . $x);",
			        "list = [a.b, a.c];");
			TestLes("##filter(#splice(w, x(), y(), z), $x());",
			        "x();\ny();");
			TestLes("##filter(#splice(w, x(1), y(), z(2)), $x(), $x(1));",
			        "x(1);\ny();");
			TestLes("list = ##filterOut([a + b, 3, x + y], $x + $y);",
			        "list = [3];");
			TestLes("list = ##filterOut([a + b, 3 * 4, x - y, a / b], $x + $y, $x - $y);",
			        "list = [3 * 4, a / b];");
		}

		[Test]
		public void SkipSpecTest()
		{
			TestEcs("var list = ##filter(new[] { x(1), x(null), x }, 1, x($x));",
			        "var list = new[] { x(1), x(null) };");
			TestLes("##filter(Process(unchanged, a, b, c, untouched), 1 .. ^1);",
			        "Process(unchanged, untouched);");
			TestLes("##filter(Process(x, y, z), (-1) .. 2, x);",
			        @"#splice()(x, z);");
			TestLes("##filterOut(Process(w, x, y, x), (-1) .. 2, x);",
			        @"Process(w, y, x);");
			TestLes("##filterOut(@[7, 76 + 1, 777] (X + 1)(x), -999999 .. -1, $x + 1);",
			        @"@[7, 777] (X + 1)(x)");
			TestLes("##filterOut(@[7, 76 + 1, 777] (X + 1)(x), -999999 .. 0, $x + 1);",
			        @"@[7, 777] #splice()(x)");
			TestLes("##filterOut(@[8 + 8, 8, 88 + 88] (i + i)(), -999999 .. -2, $x + $x);",
			        @"@[8, 88 + 88] (i + i)()");
		}
	}
}
