using Ecs.Parser;
using LeMP;
using Loyc;
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
			        "var tmp_"+n+" = Foo; a = tmp_"+n+".Item1; b.c.d = tmp_"+n+".Item2");
			n = StandardMacros.NextTempCounter;
			TestEcs("(a, b, c, d) = X.Y();",
			        "var tmp_"+n+" = X.Y(); a = tmp_"+n+".Item1; b = tmp_"+n+".Item2; c = tmp_"+n+".Item3; d = tmp_"+n+".Item4");
		}

		[Test]
		public void TestStringInterpolation()
		{
		}

		
		SeverityMessageFilter _sink = new SeverityMessageFilter(MessageSink.Console, Severity.Debug);

		// Output is still EC#
		private void TestLes(string input, string outputEcs, int maxExpand = 0xFFFF)
		{
			using (ParsingService.PushCurrent(LesLanguageService.Value))
				TestCompiler.Test(input, outputEcs, _sink, maxExpand);
		}
		private void TestEcs(string input, string outputEcs, int maxExpand = 0xFFFF)
		{
			using (ParsingService.PushCurrent(EcsLanguageService.Value))
				TestCompiler.Test(input, outputEcs, _sink, maxExpand);
		}
		private void TestBoth(string inputLes, string inputEcs, string outputEcs, int maxExpand = 0xFFFF)
		{
			TestLes(inputLes, outputEcs, maxExpand);
			TestEcs(inputEcs, outputEcs, maxExpand);
		}
	}
}
