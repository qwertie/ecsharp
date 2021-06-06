using Loyc;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LeMP.Tests
{
	[TestFixture]
	public class TestMapSyntaxMacro : MacroTesterBase
	{
		[Test]
		public void SimpleTests()
		{
			TestLes("list = ##map([a, b, 3], $x => $x + 1);",
			        "list = [a + 1, b + 1, 3 + 1];");
			TestLes("##map(#splice(w, x, y), $x => $x * 2);",
			        "w * 2;\nx * 2;\ny * 2;");
			TestLes("list = ##map([a + b, 3, x + y], ($x + $y) => Add($x, $y));",
			        "list = [Add(a, b), 3, Add(x, y)];");
			TestLes("list = ##mapWithFilter([a + b, 3, x + y], ($x + $y) => Add($x, $y));",
			        "list = [Add(a, b), Add(x, y)];");
		}

		[Test]
		public void MultiMatchTest()
		{
			TestLes("list = ##map([a, 1, b, 2], { $a; $b; } => $a = $b);",
			        "list = [a = 1, b = 2];");
			TestLes("list = ##map([Do(), Prep(123), Do(), Prep(456)], { Prep($x); Do(); } => PrepDo($x));",
			        "list = [Do(), PrepDo(123), Prep(456)];");
			TestLes("list = ##map([Prep(111), Do(111), Prep(222), Do(333)], { Prep($x); Do($x); } => PrepDo($x));",
			        "list = [PrepDo(111), Prep(222), Do(333)];");
			TestLes("list = ##map([1, 2, DeleteMe, 3], { DeleteMe; } => { });",
			        "list = [1, 2, 3];");
			TestLes("list = ##mapWithFilter([y, x, x, z], { $n; $n; } => { $n });",
			        "list = [x];");
		}
		
		[Test]
		public void SkipSpecTest()
		{
			TestEcs("var list = ##map(new[] { a, b, 3 }, 1, $x => $x + 1);",
			        "var list = new[] { a + 1, b + 1, 3 + 1 };");
			TestLes("##map(Process(unchanged, a, b, c, untouched), 1 .. ^1, $x => Sqrt($x));",
			        "Process(unchanged, Sqrt(a), Sqrt(b), Sqrt(c), untouched);");
			TestLes("##map(Process(x), (-1) .. 1, $x => stringify($x));",
			        @"""Process""(""x"");");
			TestLes("##map(@[7, 77, 777] Process(x), -999999 .. -1, $x => - $x);",
			        @"@[-7, -77, -777] Process(x)");
			TestLes("##map(@[8, 88, 888] ident, -999999 .. -2, $x => - $x);",
			        @"@[-8, -88, 888] ident");
			TestLes("##map(@[9, 99, 999] Process(x), -2 .. 0, $x => tweaked($x));",
			        @"@[9, 99, tweaked(999)] tweaked(Process)(x)");
			TestLes("##map(@[777] Process(w, x, y, z), .. ^2, $x => - $x);",
			        @"@[777] Process(-w, -x, y, z)");
			
			TestLes("list = ##map(foo(bar, foo), -1, { foo; } => { });",
			        "list = bar();");
			
			using (MessageSink.SetDefault(_msgHolder)) {
				TestLes("list = ##map(foo(foo), -1, { foo; } => { });",
						"list = @``;");
			}
			Assert.AreEqual(1, _msgHolder.List.Count(msg => msg.Severity == Severity.Warning));
		}

		[Test]
		public void PreprocessTest()
		{
			TestEcs("define WXY() => #splice(w, x, y); \n ##map(WXY(), $x => $x * 2);",
			        "w * 2;\nx * 2;\ny * 2;");
		}

		[Test]
		public void MapTargetTest()
		{
			TestEcs("##mapTarget(foo(25), $x => $x());",
			        "foo()(25);");
			TestEcs("##mapTarget(foo(42), bar => baz, foo => bar);",
			        "bar(42);");
		}
	}
}
