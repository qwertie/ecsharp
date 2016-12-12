using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;

namespace LeMP.Tests
{
	[TestFixture]
	public class TestUnrollMacro : MacroTesterBase
	{
		[Test]
		public void TestUnroll()
		{
			TestLes(@"unroll X `in` (X; Y) { sum += X; }",
			          "sum += X; sum += Y;");
			TestBoth(@"unroll X `in` (X; Y) { sum += X; }",
			          "unroll (X in (X, Y)) { sum += X; }",
			          "sum += X; sum += Y;");
			TestEcs("unroll (X in (X, Y)) { sum += X; Console.WriteLine(sum); }",
			        "sum += X; Console.WriteLine(sum);"+
			        "sum += Y; Console.WriteLine(sum);");
			TestEcs("unroll (X in (A(), [Oof] B)) { X(X, [Foo] X); }",
			        "A()(A(), [Foo] A());"+
			        "([Oof] B)([Oof] B, [Foo, Oof] B);");
			TestEcs("unroll ((type, name) in ((int, x), (uint, y), (float, z))) { type name = 0; }",
			        "int x = 0; "+
			        "uint y = 0; "+
			        "float z = 0;");
			TestEcs("unroll (X in (int X = 41, X++, Console.WriteLine(X))) { X; }",
			        "int X = 41; X++; Console.WriteLine(X);");
		}
		[Test]
		public void TestUnrollWithBraces()
		{
			TestEcs("unroll (Var in { int X; float Y; string Z; }) { internal static Var; }",
			        "internal static int X; internal static float Y; internal static string Z;");
		}
	}
}
