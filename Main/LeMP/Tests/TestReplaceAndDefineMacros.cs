using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.MiniTest;

namespace LeMP.Tests
{
	[TestFixture]
	public class TestReplaceAndDefineMacros : MacroTesterBase
	{
		[Test]
		public void TestReplace_basics()
		{
			// Simple cases
			using (MessageSink.SetDefault(_msgHolder)) {
				TestLes(@"replace (nothing => nobody) {nowhere;}", "nowhere;");
				Assert.IsTrue(_msgHolder.List.Count > 0, "expected warning about 'no replacements'");
			}
			TestLes(@"replace (a => b) {a;}", "b;");
			TestLes(@"replace (7 => seven) {x = 7;}", "x = seven;");
			TestLes(@"replace (7() => ""seven"") {x = 7() + 7;}", @"x = ""seven"" + 7;");
			TestLes(@"replace (a => b) {@[Hello] a; a(a);}", "@[Hello] b; b(b);");
			TestLes("replace (MS => MessageSink; C => Current; W => Write; S => Severity; B => \"Bam!\")\n" +
					"    { MS.C.W(S.Error, @null, B); }",
					@"MessageSink.Current.Write(Severity.Error, @null, ""Bam!"");");
			TestLes(@"replace (Write => Store; Console.Write => Console.Write) " +
					@"{ Write(x); Console.Write(x); }", "Store(x); Console.Write(x);");

			// Swap
			TestLes("replace (foo => bar; bar => foo) {foo() = bar;}", "bar() = foo;");
			TestLes("replace (a => 'a'; 'a' => A; A => a) {'a' = A - a;}", "A = a - 'a';");

			// Captures
			TestBoth("replace (input($capture) => output($capture)) { var i = 21; input(i * 2); };",
					 "replace (input($capture) => output($capture)) { var i = 21; input(i * 2); }",
					 "var i = 21; output(i * 2);");
		}

		[Test]
		public void TestReplace_params()
		{
			TestEcs("replace({{ $(params before); on_exit { $(params command); } $(params after); }} =>\n" +
					"        {{ $before;          try  { $after; } finally { $command; }         }}) \n" +
					"{{ var foo = new Foo(); on_exit { foo.Dispose(); } Combobulate(foo); return foo; }}",
					" { var foo = new Foo(); try { Combobulate(foo); return foo; } finally { foo.Dispose(); } }");

			TestLes("replace ($($format; $(..args)) => String.Format($format, $(..args)))\n" +
					@"   { MessageBox.Show($(""I hate {0}""; noun)); }",
					@"MessageBox.Show(String.Format(""I hate {0}"", noun));");
			TestLes("replace ($($format; $(..args)) => String.Format($format, $(..args)))\n" +
					@"   { MessageBox.Show($(""I hate {0}ing {1}s""; verb; noun), $(""FYI"";)); }",
					@"MessageBox.Show(String.Format(""I hate {0}ing {1}s"", verb, noun), String.Format(""FYI""));");
		}

		[Test(Fails = "Not Implemented")]
		public void TestReplace_match_attributes()
		{
			// [foo] a([attr] Foo) `MatchesPattern` 
			// [`%`, bar] a([$attr] $foo)

			// ([foo] F([x] X, [y] Y, [a1(...), a2(...)] Z) `MatchesPattern`
			//        F(X, $Y, $(params P), [$A, a1($(params args))] $Z)) == false cuz [x] is unmatched

			// TODO
		}

		[Test]
		public void TestReplaceInTokenTree()
		{
			// Subtlety: the test fails if there is no newline after `replace(...) {`.
			// Without the newline, %appendStatement is attached to foo(), which the test does not expect.
			TestLes("replace (foo => bar; bar => foo) {\n  foo(@{ foo(*****) }); }", "bar(@{ bar(*****) });");
			TestLes("replace (foo => bar; bar => foo) {\n  foo(@{ foo(%bar%) }); }", "bar(@{ bar(%foo%) });");
			TestLes("replace (foo => bar; bar => foo) {\n  foo(@{ ***(%bar%) }); }", "bar(@{ ***(%foo%) });");
			TestLes("unroll ((A; B) `in` ((Eh; Bee); ('a'; \"b\"); (1; 2.0))) {\n  foo(B - A, @{A + B}); }",
				@"foo(Bee - Eh,  TokenTree""Eh '+ Bee"");
				  foo(""b"" - 'a', TokenTree""'a' '+ \""b\"""");
				  foo(2.0 - 1, TokenTree""1 '+ 2.0"");");
		}

		[Test]
		public void TestReplace_advanced()
		{
			// Nested replacements
			TestEcs(@"replace(X => Y, X($(params p)) => X($p)) { X = X(X, Y); }",
					@"Y = X(Y, Y);");
			// Note: $a * $b doesn't work because it is seen as a variable decl
			TestEcs(@"replace(($a + $b + $c) => Add($a, $b, $c), ($a`'*`$b) => Mul($a, $b))
			          { var y = 2*x*2 + 3*x + 4; }",
					@"var y = Add(Mul(Mul(2, x), 2), Mul(3, x), 4);");

			//TestEcs(@"replace(TO=>DO) {}", @"");
		}

		[Test]
		public void TestReplace_RemainingNodes()
		{
			TestEcs(@"{ replace(C => Console, WL => WriteLine); string name = ""Bob""; C.WL(""Hi ""+name); }",
					@"{ string name = ""Bob""; Console.WriteLine(""Hi ""+name); }");
		}

		[Test]
		public void TestDefineEverything()
		{
			var holder = new MessageHolder();
			using (MessageSink.SetDefault(holder)) // suppress errors
			{
				TestEcs(@"define ($anything) { $anything + 1; }
					X();",
					@"define ($anything) { $anything + 1; }
					X();");
				Assert.AreEqual(1, holder.List.Count(msg => 
					msg.Format.EndsWith("Defining a macro that could match everything is not allowed.")));
			}
		}

		[Test]
		public void TestDefineFn()
		{
			TestLes(@"define WL() { Console.WriteLine(); }; WL(); WL();",
					@"Console.WriteLine(); Console.WriteLine();");
			TestEcs(@"define NL() => Console.WriteLine(); NL(); NL();",
					@"Console.WriteLine(); Console.WriteLine();");
			TestEcs(@"define Methods($T) { void F($T arg) {} void G($T arg) {} } Methods(int); Methods(List<int>);",
					@"void F(int arg) {} void G(int arg) {} void F(List<int> arg) {} void G(List<int> arg) {}");
			TestEcs(@"define WL($format, $(..args)) => Console.WriteLine($format, $(..args)); WL(1, 2, 3);",
					@"Console.WriteLine(1, 2, 3);");
			TestLes(@"define WL($format, $(..args)) { Console.WriteLine($format, $(..args)); }; WL(1, 2, 3);",
					@"Console.WriteLine(1, 2, 3);");
			TestEcs(@"[Passive] define operator=(Foo[$index], $value) => Foo.SetAt($index, $value); x = Foo[y] = z;",
					@"x = Foo.SetAt(y, z);");
			// Test warnings about `$`
			using (MessageSink.SetDefault(new SeverityMessageFilter(_msgHolder, Severity.DebugDetail))) {
				_msgHolder.List.Clear();
				TestEcs(@"define Foo(w, $x, y, $z) => (x, $y);", @"");
				Assert.AreEqual(2, _msgHolder.List.Count);
				TestEcs(@"define Foo(a, b) => (a, b);", @"");
				Assert.AreEqual(4, _msgHolder.List.Count);
				Assert.IsTrue(_msgHolder.List.All(msg => msg.Severity == Severity.Warning));
				_msgHolder.WriteListTo(TraceMessageSink.Value);
			}
		}

		[Test]
		public void TestDefineIdentifier()
		{
			TestEcs(@"define WL { Console.WriteLine; } WL(122);",
					@"Console.WriteLine(122);");
			TestEcs(@"define WL => Console.WriteLine; WL(123);",
					@"Console.WriteLine(123);");
			TestLes(@"define WL { Console.WriteLine; }; WL(124);",
					@"Console.WriteLine(124);");
			TestLes(@"#define WL { Console.WriteLine; }; WL(125);",
					@"Console.WriteLine(125);");
		}

		[Test]
		public void TestDefineLiterals()
		{
			TestEcs(@"define ('x') { 'X'; }
					Console.WriteLine('x');",
					@"Console.WriteLine('X');");
			TestEcs(@"define (123) { 456; }
					Console.WriteLine(123 + 456 + 789);",
					@"Console.WriteLine(456 + 456 + 789);");
		}

		[Test]
		public void TestDefineComplexCall()
		{
			TestEcs(@"define ($x << $y) { $x.Append($y); }
					sb << ""Hello"";",
					@"sb.Append(""Hello"");");
			TestEcs(@"define (Foo()()()) { Bar(); }
					define (Invalid.$F($X)) { Valid.$F($X); }
					x = Foo()()()(x);
					Invalid.Foo(x);",
					@"x = Bar()(x);
					Valid.Foo(x);");
		}

		[Test]
		public void TestMulDiv()
		{
			TestEcs(@"
				replace (($x * $y / $z) => { MulDiv($x, $y, $z); }) {
					var x = 2 * x / 4.0;
				}",
				@"var x = MulDiv(2, x, 4d);");
			TestEcs(@"
				// [Passive]
				define operator/(($x * $y), $z) { MulDiv($x, $y, $z); }
				var x = 2 * x / 4.0;",
				@"var x = MulDiv(2, x, 4d);");
		}

		[Test]
		public void TestDefineUsingTempVars()
		{
			TestEcs(@"
				define change_temporarily($lhs = $rhs) {
					var old_unique# = $lhs;
					$lhs = $rhs;
					on_finally { $lhs = old_unique#; }
				}
				void DoStuff() {
					change_temporarily(a = 123);
					change_temporarily(b = 456);
					Act();
				}", @"
				void DoStuff() {
					var old_A = a;
					a = 123;
					try {
						var old_B = b;
						b = 456;
						try {
							Act();
						} finally { b = old_B; }
					} finally { a = old_A; }
				}"	.Replace("old_A", "old_" + MacroProcessor.NextTempCounter)
					.Replace("old_B", "old_" + (MacroProcessor.NextTempCounter + 1)));
			
			TestEcs(@"
				define change_temporarily($lhs.$prop = $rhs) {
					var temp#_ = $lhs;
					var old_unique# = temp#_.$prop;
					temp#_.$prop = $rhs;
					on_finally { temp#_.$prop = old_unique#; }
				}
				void DoStuffs() {
					change_temporarily(List[i].a = 123);
					change_temporarily(List[i].b = 456);
					Stuff1();
					Stuff2();
				}", @"
				void DoStuffs() {
					var tempA_ = List[i];
					var old_A = tempA_.a;
					tempA_.a = 123;
					try {
						var tempB_ = List[i];
						var old_B = tempB_.b;
						tempB_.b = 456;
						try {
							Stuff1();
							Stuff2();
						} finally { tempB_.b = old_B; }
					} finally { tempA_.a = old_A; }
				}".Replace("tempA", "temp" + MacroProcessor.NextTempCounter)
				  .Replace("old_A", "old_" + (MacroProcessor.NextTempCounter + 1))
				  .Replace("tempB", "temp" + (MacroProcessor.NextTempCounter + 2))
				  .Replace("old_B", "old_" + (MacroProcessor.NextTempCounter + 3)));
		}

	}
}
