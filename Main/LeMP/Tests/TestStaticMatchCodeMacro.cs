using System;
using System.Collections.Generic;
using System.Linq;
using Loyc.MiniTest;
using Loyc;

namespace LeMP.Tests
{
	[TestFixture]
	public class TestStaticMatchCodeMacro : MacroTesterBase
	{
		[Test]
		public void OneSimpleCase()
		{
			TestLes("@[#static] matchCode (f(123)) { { f($x) } => { result = $x; etc; } };",
			        "result = 123; etc;");
			TestEcs("static matchCode(return 123) { case return $x: result = $x; goto stop; }",
			        "result = 123; goto stop;");
			TestEcs("static matchCode(return 123) { case return $x: { result = $x; goto stop; } }",
			        "{ result = 123; goto stop; }");
		}

		[Test]
		public void RunMacrosOnInput() // (but not output)
		{
			TestEcs(@"#snippet X = Foo.Food(bar, baz);
				static matchCode(#get(X)) {
					case $cls.$member($(..args)): 
						class $cls { 
							void $member(unroll(arg in $args) { int arg; }) { }
						}
				}",
			        "class Foo { void Food(int bar, int baz) { } }");
		}

		[Test]
		public void BiggerCases()
		{
			string matchStmt =
				@"static matchCode(#get(input)) {
					case $x + $y:
						Add($x, $y);
					case $call($(..args)): 
						void $call(unroll(arg in $args) { int arg; }) 
							{ base.$call($args); }
					default:
						DefaultAction($#);
				}";
			TestEcs(@"#snippet input = 123; " + matchStmt,
			        "DefaultAction(123);");
			TestEcs(@"#snippet input = a + b; " + matchStmt,
			        "Add(a, b);");
			TestEcs(@"#snippet input = Foo(a, b); " + matchStmt,
			        "void Foo(int a, int b) { base.Foo(a, b); }");
		}

		[Test]
		public void StaticMatches_OneSimpleCase()
		{
			TestLes("f(123) `staticMatches` f($x); $x",
			        "@true; 123;");
			TestLes("{ eat 123; } `staticMatches` { $f(123); }; $f;",
			        "@true; eat;");
			// $f doesn't exist so DollarSignVariable produces an error that we'll ignore
			using (MessageSink.SetDefault(MessageSink.Null))
				TestLes("{ eat 123; } `staticMatches` { $f(321); }; $f;",
						"@false; $f;");
		}

		[Test]
		public void TestStaticMatches()
		{
			TestEcs(@"bool b1 = (x * y + z) `staticMatches` ($a * $b);", @"bool b1 = false;");
			TestEcs(@"bool b2 = (x * y + z) `staticMatches` ($a + $b);", @"bool b2 = true;");
			TestEcs(@"bool b3 = (x + 1) `staticMatches` (x + 1);",  @"bool b3 = true;");
			TestEcs(@"bool b4 = (x + 1) `staticMatches` ($x + 1);", @"bool b4 = true;");
			TestEcs(@"bool b5 = (x + 2) `staticMatches` ($x + 3);", @"bool b5 = false;");
			TestEcs(@"bool b6 = { Foo(1); } `staticMatches` Foo($x);", @"bool b6 = true;");
		}
	}
}
