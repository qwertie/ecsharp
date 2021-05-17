using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.MiniTest;

namespace LeMP.Tests
{
	[TestFixture]
	public class TestUnrollMacro : MacroTesterBase
	{
		[Test]
		public void TestOldUnroll()
		{
			TestLes(@"unroll X `in` (X; Y) { sum += X; }",
					  "sum += X; sum += Y;");
			TestBoth(@"unroll X `in` (X; Y) { sum += X; }",
					  "unroll (X in (X, Y)) { sum += X; }",
					  "sum += X; sum += Y;");
			TestEcs("unroll (X in (X, Y)) { sum += X; Console.WriteLine(sum); }",
					"sum += X; Console.WriteLine(sum);" +
					"sum += Y; Console.WriteLine(sum);");
			TestEcs("unroll (X in (A(), [Oof] B)) { X(X, [Foo] X); }",
					"A()(A(), [Foo] A());" +
					"([Oof] B)([Oof] B, [Foo, Oof] B);");
			TestEcs("unroll ((type, name) in ((int, x), (uint, y), (float, z))) { type name = 0; }",
					"int x = 0; " +
					"uint y = 0; " +
					"float z = 0;");
			TestEcs("unroll (X in (int X = 41, X++, Console.WriteLine(X))) { X; }",
					"int X = 41; X++; Console.WriteLine(X);");
		}
		[Test]
		public void TestOldUnrollWithBraces()
		{
			TestEcs("unroll (Var in { int X; float Y; string Z; }) { internal static Var; }",
					"internal static int X; internal static float Y; internal static string Z;");
		}
		[Test]
		public void TestOldUnrollPreservesTrivia()
		{
			TestEcs(@"/*before*/ 
				unroll (Var in { int X; float Y; string Z; }) { 
					internal static Var;
				} /*after*/",
				@"/*before*/
				internal static int X;
				internal static float Y;
				internal static string Z; /*after*/");
		}

		[Test]
		public void TestUnroll()
		{
			TestLes("##unroll $X `in` (X; Y) { sum += $X; };",
			        "sum += X; sum += Y;");
			TestBoth("##unroll $A `in` (X; Y) { total += $A; }",
			         "##unroll ($A in (X, Y)) { total += $A; }",
			         "total += X; total += Y;");

			// Nontrivial pattern in braces
			TestEcs("##unroll ({ $T $x; } in { int X; float Y; string Z; }) { static $T $x = ($T) 0; }",
			        "static int X = (int) 0; static float Y = (float) 0; static string Z = (string) 0;");
			
			// Weird ones
			TestEcs("##unroll ($X in (A(), [Oof] B)) { $X($X, [Foo] $X); }",
			        "A()(A(), [Foo] A());" +
			        "([Oof] B)([Oof] B, [Foo, Oof] B);");
			using (MessageSink.SetDefault(_msgHolder)) { // suppress warnings from DollarSignVariable
				TestEcs(@"##unroll ($$X in ($X, $Y)) {
						   sum += $X; Console.WriteLine(sum);
						}
				   ", @"sum += X; Console.WriteLine(sum);
						sum += Y; Console.WriteLine(sum);");
			}

			// Match pairs of input elements in the pattern
			TestEcs("##unroll ({ $type; $name; } in (int, x, uint, y, float, z)) { $type $name = 0; }",
			        "int x = 0; " +
			        "uint y = 0; " +
			        "float z = 0;");

			// Try the other allowed list specs
			TestEcs("##unroll ($X in #splice(int X = 41, X++, Console.WriteLine(X))) { $X; }",
			        "int X = 41; X++; Console.WriteLine(X);");
			TestEcs("##unroll ($X in @`'[]`(1, 2, 3)) { $X; }",
			        "1; 2; 3;");
		}

		[Test]
		public void TestUnrollInputListIsPreprocessed()
		{
			TestEcs("#snippet LIST = (1, 2, 3);" +
			        "##unroll ($x in $LIST) { WriteLine($x); }",
			        "WriteLine(1); WriteLine(2); WriteLine(3); ");
		}
		
		[Test]
		public void TestUnrollError()
		{
			// Workaround: ##unroll accepts @in in lieu of 'in operator. Use this because
			// there is a macro for the `in` operator whose effects we don't want to
			// observe here. Wish we had a way to import or un-import individual macros...
			TestEcs("##unroll (@in($X, (X, Y))) {\n $X; }", "X;\nY;");

			// Illegal list target produces an error
			using (MessageSink.SetDefault(_msgHolder)) {
				TestEcs("##unroll (@in($X, Foo(X; Y))) { $X; }",
						"##unroll (@in($X, Foo(X; Y))) { $X; }");
				Assert.AreEqual(1, _msgHolder.List.Count(msg => msg.Severity == Severity.Error));
			}
		}
		
		[Test]
		public void TestUnrollPreservesTrivia()
		{
			TestEcs(@"/*before*/ 
				unroll (Var in { 
					int X; 
					float Y; 
					string Z;
				}) { 
					internal static Var;
				} /*after*/",
				@"/*before*/
				internal static int X;
				internal static float Y;
				internal static string Z; /*after*/");
		}

	}
}