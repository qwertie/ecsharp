using System;
using System.Collections.Generic;
using System.Linq;
using Loyc.MiniTest;
using Loyc;

namespace LeMP.Tests
{
	[TestFixture]
	public class TestStaticDeconstructMacro : MacroTesterBase
	{
		[Test]
		public void TrivialCase()
		{
			TestEcs("#deconstruct($WL = Console.WriteLine); $WL();", 
			        "Console.WriteLine();");
			// Scoping is handled by the macro processor itself, so this should never fail
			TestEcs("#deconstruct($M = Foo); { $M(); #deconstruct($M = Bar); $M($M); } $M(new $M($M));", 
			        "{ Foo(); Bar(Bar); } Foo(new Foo(Foo));");

			TestEcs("#deconstruct($WL = Console.WriteLine); $WL();", 
			        "Console.WriteLine();");

			using (MessageSink.SetDefault(new SeverityMessageFilter(_msgHolder, Severity.WarningDetail))) {
				TestEcs("$X = $Y;", "$X = $Y;");
				Assert.AreEqual(2, _msgHolder.List.Count, "expected errors because $X and $Y don't exist");
			}
		}

		[Test]
		public void SingleCases()
		{
			TestEcs("#deconstruct($a + $b = x + y + 123); var a = $a; var b = $b;", 
			        "var a = x + y; var b = 123;");
			TestEcs("#deconstruct(seq($(..stmts)) = seq(Foo(x), return Bar)); $stmts;", 
			        "Foo(x); return Bar;");
			TestEcs("#deconstruct({ class $C { $(.._); } } = { class Foo { } }); $C v;", 
			        "Foo v;");
		}

		[Test]
		public void ExpandsRhsAndStripBraces()
		{
			TestEcs("#deconstruct(Foo($(..parts)) = { Foo(Foo, abc, 123); }); $parts;", 
			        "Foo; abc; 123;");
			TestEcs("#deconstruct($x = { Foo(Foo, abc, 123); }); "+
			        "#deconstruct(Foo($a, $b, $c) = $x); $a $b = $c;", 
			        "Foo abc = 123;");
			TestEcs("#deconstruct({ $x; } = Hello()); #deconstruct($x = $x + 1); return $x;", 
			        "return Hello() + 1;");
		}

		[Test]
		public void MultiplePatterns()
		{
			TestEcs(@"#deconstruct({ class     $name : $(.._) { $(.._); } }
					             | { struct    $name : $(.._) { $(.._); } }
					             | { enum      $name : $(.._) { $(.._); } } 
					             | { interface $name : $(.._) { $(.._); } } 
					             = { struct Point { int X, Y; } });
					$name p;", "Point p;");
		}

		[Test]
		public void MultipleArguments() // final exam
		{
			TestEcs("#deconstruct($x + $y = 1 + 2, $z = 3 + 4); int ten = $x + $y + $z;",
			        "int ten = 1 + 2 + (3 + 4);");
			TestEcs("#deconstruct($x() + $y = Foo() + Bar(), $x - $y | $y($x) = $x($y)); int bf = $x + $y();",
			        "int bf = Bar() + Foo();");
			TestEcs(@"#deconstruct(+$x | -$x | $x = -123, 
					               +$y | -$y | $y = funky);
					Foo($x, $y);", "Foo(123, funky);");
		}
	}
}
