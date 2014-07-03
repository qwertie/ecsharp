using Ecs.Parser;
using LeMP;
using Loyc;
using Loyc.Collections;
using Loyc.MiniTest;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LeMP
{
	[TestFixture]
	public class StandardMacroTests
	{
		SeverityMessageFilter _sink = new SeverityMessageFilter(MessageSink.Console, Severity.Debug);

		[Test]
		public void TestNullDot()
		{
			TestBoth(@"a = b.c?.d;", @"a = b.c?.d;",
			          "a = b.c != null ? b.c.d : null;");
			TestEcs(@"a = b.c?.d.e;",
			         "a = b.c != null ? b.c.d.e : null;");
			TestBoth(@"a = b?.c[d];", @"a = b?.c[d];",
			          "a = b != null ? b.c[d] : null;");
			TestEcs(@"a = b?.c?.d();",
			         "a = b != null ? b.c != null ? b.c.d() : null : null;");
			TestBoth(@"return a.b?.c().d!x;", @"return a.b?.c().d<x>;",
			          "return a.b != null ? a.b.c().d<x> : null;");
		}

		[Test(Fails="Tuple macro is disabled because it interferes with constructs like #def(void, Foo, (arg, arg))")]
		public void TestTuples()
		{
			TestEcs("use_default_tuple_makers();", "");
			TestBoth("(1, a) + (2, a, b) + (3, a, b, c);",
			         "(1, a) + (2, a, b) + (3, a, b, c);",
			         "Pair.Create(1, a) + Tuple.Create(2, a, b) + Tuple.Create(3, a, b, c);");
			TestEcs("set_tuple_maker(Sum); a = (1,) + (1, 2) + (1, 2, 3, 4, 5);",
			        "a = Sum(1) + Sum(1, 2) + Sum(1, 2, 3, 4, 5);");
			TestEcs("set_tuple_maker(Tuple.Create); set_tuple_maker(2, Pair.Create);"+
			        "a = (1,) + (1, 2) + (1, 2, 3, 4, 5);",
			        "a = Tuple.Create(1) + Pair.Create(1, 2) + Tuple.Create(1, 2, 3, 4, 5);");
			
			TestBoth("(a, b, c) = foo;", "(a, b, c) = foo;",
			        "a = foo.Item1; b = foo.Item2; c = foo.Item3;");
			int n = StandardMacros.NextTempCounter;
			TestEcs("(a, b.c.d) = Foo;",
			        "var tmp_"+n+" = Foo; a = tmp_"+n+".Item1; b.c.d = tmp_"+n+".Item2;");
			n = StandardMacros.NextTempCounter;
			TestEcs("(a, b, c, d) = X.Y();",
			        "var tmp_"+n+" = X.Y(); a = tmp_"+n+".Item1; b = tmp_"+n+".Item2; c = tmp_"+n+".Item3; d = tmp_"+n+".Item4;");
		}

		[Test]
		public void TestStringInterpolation()
		{
			Assert.Fail("TODO");
		}

		[Test]
		public void TestUnroll()
		{
			TestLes(@"unroll X \in (X, Y) { sum += X; }",
			          "sum += X; sum += Y;");
			TestBoth(@"unroll X \in (X, Y) { sum += X; }",
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
		public void TestReplace_basics()
		{
			// Simple cases
			TestLes(@"replace(nothing => nobody) {nowhere;}", "nowhere;");
			TestLes(@"replace(a => b) {a;}", "b;");
			TestLes(@"replace(7 => seven) {x = 7;}", "x = seven;");
			TestLes(@"replace(7() => ""seven"") {x = 7() + 7;}", @"x = ""seven"" + 7;");
			TestLes(@"replace(a => b) {[Hello] a; a(a);}", "[Hello] b; b(b);");
			TestLes("replace(MS => MessageSink, C => Current, W => Write, S => Severity, B => \"Bam!\")\n"+
			        "    { MS.C.W(S.Error, @null, B); }",
			        @"MessageSink.Current.Write(Severity.Error, @null, ""Bam!"");");
			TestLes(@"replace(Write => Store, Console.Write => Console.Write)"+
			        @"{ Write(x); Console.Write(x); }", "Store(x); Console.Write(x);");
			
			// Swap
			TestLes("replace(foo => bar, bar => foo) {foo() = bar;}", "bar() = foo;");
			TestLes("replace(a => 'a', 'a' => A, A => a) {'a' = A - a;}", "A = a - 'a';");

			// replace(Foo => Bar, System.Foo => System.Foo)

			// Captures
			TestBoth("replace(input($capture) => output($capture)) { var i = 21; input(i * 2); };",
			         "replace(input($capture) => output($capture)) { var i = 21; input(i * 2); }",
			         "var i = 21; output(i * 2);");
		}

		[Test]
		public void TestReplace_params()
		{
			TestEcs("replace({ $(params before); on_exit { $(params command); } $(params after); } =>\n"+
			        "        { $before;          try  { $after; } finally { $command; }         }) \n"+
			        "{{ var foo = new Foo(); on_exit { foo.Dispose(); } Combobulate(foo); return foo; }}",
			        " { var foo = new Foo(); try { Combobulate(foo); return foo; } finally { foo.Dispose(); } }");
			
			TestLes("replace($($format, $([#params] args)) => String.Format($format, $args) )\n"+
			        @"   { MessageBox.Show($(""I hate {0}"", noun)); }",
			        @"MessageBox.Show(String.Format(""I hate {0}"", noun));");
			TestLes("replace($($format, $([#params] args)) => String.Format($format, $args) )\n"+
			        @"   { MessageBox.Show($(""I hate {0}ing {1}s"", verb, noun), $(""FYI"",)); }",
			        @"MessageBox.Show(String.Format(""I hate {0}ing {1}s"", verb, noun), String.Format(""FYI""));");
		}

		[Test]
		public void TestReplace_match_attributes()
		{
			// [foo] a([attr] Foo) `MatchesPattern` 
			// [#trivia_, bar] a([$attr] $foo)
			
			// ([foo] F([x] X, [y] Y, [a1(...), a2(...)] Z) `MatchesPattern`
			//        F(X, $Y, $(params P), [$A, a1($(params args))] $Z)) == false cuz [x] is unmatched
			TestEcs(@"replace(TO=>DO) {}", @"");
		}

		[Test]
		public void TestReplace_advanced()
		{
			// Nested replacements
			TestEcs(@"replace(X => Y, X($(params p)) => X($p)) { X = X(X, Y); }",
			        @"Y = X(Y, Y);");
			TestEcs(@"replace(($a + $b + $c) => Add($a, $b, $c), ($a * $b) => Mul($a, $b)) { var y = 2*x*2 + 3*x + 4; }", 
			        @"var y = Add(Mul(Mul(2, x), 2), Mul(3, x), 4);");

			TestEcs(@"replace(TO=>DO) {}", @"");
		}

		private void TestLes(string input, string outputLes, int maxExpand = 0xFFFF)
		{
			Test(input, LesLanguageService.Value, outputLes, LesLanguageService.Value, maxExpand);
		}
		private void TestEcs(string input, string outputEcs, int maxExpand = 0xFFFF)
		{
			Test(input, EcsLanguageService.Value, outputEcs, EcsLanguageService.Value, maxExpand);
		}
		private void TestBoth(string inputLes, string inputEcs, string outputEcs, int maxExpand = 0xFFFF)
		{
			Test(inputLes, LesLanguageService.Value, outputEcs, EcsLanguageService.Value, maxExpand);
			Test(inputEcs, EcsLanguageService.Value, outputEcs, EcsLanguageService.Value, maxExpand);
		}
		private void Test(string input, IParsingService inLang, string expected, IParsingService outLang, int maxExpand = 0xFFFF)
		{
			var lemp = NewLemp(maxExpand);
			var inputCode = new RVList<LNode>(inLang.Parse(input, _sink));
			var results = lemp.ProcessSynchronously(inputCode);
			var expectCode = outLang.Parse(expected, _sink);
			if (!results.SequenceEqual(expectCode))
			{	// TEST FAILED, print error
				string resultStr = results.Select(n => outLang.Print(n, _sink)).Join("\n");
				Assert.AreEqual(TestCompiler.StripExtraWhitespace(expected), 
				                TestCompiler.StripExtraWhitespace(resultStr));
			}
		}
		MacroProcessor NewLemp(int maxExpand)
		{
			var lemp = new MacroProcessor(typeof(LeMP.Prelude.Macros), _sink);
			lemp.AddMacros(typeof(LeMP.Prelude.Les.Macros));
			lemp.AddMacros(typeof(LeMP.StandardMacros));
			lemp.PreOpenedNamespaces.Add(GSymbol.Get("LeMP"));
			lemp.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude"));
			lemp.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude.Les"));
			lemp.MaxExpansions = maxExpand;
			return lemp;
		}
	}
}
