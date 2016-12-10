using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.MiniTest;

namespace LeMP.Tests
{
	[TestFixture]
	public class TestReplaceMacro : MacroTesterBase
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
			TestLes("replace (MS => MessageSink; C => Current; W => Write; S => Severity; B => \"Bam!\")\n"+
			        "    { MS.C.W(S.Error, @null, B); }",
			        @"MessageSink.Current.Write(Severity.Error, @null, ""Bam!"");");
			TestLes(@"replace (Write => Store; Console.Write => Console.Write) "+
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
			TestEcs("replace({{ $(params before); on_exit { $(params command); } $(params after); }} =>\n"+
			        "        {{ $before;          try  { $after; } finally { $command; }         }}) \n"+
			        "{{ var foo = new Foo(); on_exit { foo.Dispose(); } Combobulate(foo); return foo; }}",
			        " { var foo = new Foo(); try { Combobulate(foo); return foo; } finally { foo.Dispose(); } }");
			
			TestLes("replace ($($format; $(..args)) => String.Format($format, $args))\n"+
			        @"   { MessageBox.Show($(""I hate {0}""; noun)); }",
			        @"MessageBox.Show(String.Format(""I hate {0}"", noun));");
			TestLes("replace ($($format; $(..args)) => String.Format($format, $args))\n"+
			        @"   { MessageBox.Show($(""I hate {0}ing {1}s""; verb; noun), $(""FYI"";)); }",
			        @"MessageBox.Show(String.Format(""I hate {0}ing {1}s"", verb, noun), String.Format(""FYI""));");
		}

		[Test(Fails = "Not Implemented")]
		public void TestReplace_match_attributes()
		{
			// [foo] a([attr] Foo) `MatchesPattern` 
			// [#trivia_, bar] a([$attr] $foo)
			
			// ([foo] F([x] X, [y] Y, [a1(...), a2(...)] Z) `MatchesPattern`
			//        F(X, $Y, $(params P), [$A, a1($(params args))] $Z)) == false cuz [x] is unmatched

			// TODO
		}

		[Test]
		public void TestReplaceInTokenTree()
		{
			TestLes("replace (foo => bar; bar => foo) { foo(@{ foo(*****) }); }", "bar(@{ bar(*****) });");
			TestLes("replace (foo => bar; bar => foo) { foo(@{ foo(%bar%) }); }", "bar(@{ bar(%foo%) });");
			TestLes("replace (foo => bar; bar => foo) { foo(@{ ***(%bar%) }); }", "bar(@{ ***(%foo%) });");
			TestLes(@"unroll ((A; B) `in` ((Eh; Bee); ('a'; ""b""); (1; 2d))) { foo(B - A, @{A + B}); }",
				@"foo(Bee - Eh,  @{Eh + Bee});
				  foo(""b"" - 'a', @{'a' + ""b""});
				  foo(2d - 1, @{1 + 2d});");
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
		public void TestReplaceFn()
		{
			TestEcs(@"replace NL() { Console.WriteLine(); } NL(); NL();",
					@"Console.WriteLine(); Console.WriteLine();");
			TestEcs(@"replace Methods($T) { void F($T arg) {} void G($T arg) {} } Methods(int); Methods(List<int>);",
					@"void F(int arg) {} void G(int arg) {} void F(List<int> arg) {} void G(List<int> arg) {}");
			TestEcs(@"replace WL($format, $(..args)) => Console.WriteLine($format, $args); WL(1, 2, 3);",
					@"Console.WriteLine(1, 2, 3);");
			TestEcs(@"[Passive] replace operator=(Foo[$index], $value) => Foo.SetAt($index, $value); x = Foo[y] = z;",
					@"x = Foo.SetAt(y, z);");
			// Test warnings about `$`
			using (MessageSink.SetDefault(new SeverityMessageFilter(_msgHolder, Severity.Debug))) {
				_msgHolder.List.Clear();
				TestEcs(@"replace Foo(w, $x, y, $z) => (x, $y);", @"");
				Assert.AreEqual(2, _msgHolder.List.Count);
				TestEcs(@"replace Foo(a, b) => (a, b);", @"");
				Assert.AreEqual(4, _msgHolder.List.Count);
				Assert.IsTrue(_msgHolder.List.All(msg => msg.Severity == Severity.Warning));
				_msgHolder.WriteListTo(MessageSink.Trace);
			}
		}
	}
}
