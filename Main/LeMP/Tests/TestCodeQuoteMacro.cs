using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;

namespace LeMP.Tests
{
	[TestFixture]
	public class TestCodeQuoteMacro : MacroTesterBase
	{
		[Test]
		public void TestCodeQuote()
		{
			TestEcs("quote { F(); }",
				   @"LNode.Call((Symbol)""F"");");
			TestEcs("quote(F(x, 0));",
				   @"LNode.Call((Symbol)""F"", LNode.List(LNode.Id((Symbol) ""x""), LNode.Literal(0)));");
			TestEcs("quote { x = x + 1; }",
				   @"LNode.Call(CodeSymbols.Assign, LNode.List(LNode.Id((Symbol) ""x""), LNode.Call(CodeSymbols.Add, LNode.List(LNode.Id((Symbol) ""x""), LNode.Literal(1))).SetStyle(NodeStyle.Operator))).SetStyle(NodeStyle.Operator);");
			TestEcs("quote { Console.WriteLine(\"Hello\"); }",
				   @"LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) ""Console""), LNode.Id((Symbol) ""WriteLine""))).SetStyle(NodeStyle.Operator), LNode.List(LNode.Literal(""Hello"")));");
			TestEcs("q = quote({ while (Foo<T>) Yay(); });",
				   @"q = LNode.Call(CodeSymbols.While, LNode.List(LNode.Call(CodeSymbols.Of, LNode.List(LNode.Id((Symbol) ""Foo""), LNode.Id((Symbol) ""T""))), LNode.Call((Symbol) ""Yay"")));");
			TestEcs("q = quote({ if (true) { Yay(); } });",
				   @"q = LNode.Call(CodeSymbols.If, LNode.List(LNode.Literal(true), LNode.Call(CodeSymbols.Braces, LNode.List(LNode.Call((Symbol) ""Yay""))).SetStyle(NodeStyle.Statement)));");
			TestEcs("q = quote { Yay(); break; };",
				   @"q = LNode.Call(CodeSymbols.Splice, LNode.List(LNode.Call((Symbol) ""Yay""), LNode.Call(CodeSymbols.Break)));");
			TestEcs("q = quote { $(dict[key]) = 1; };",
				   @"q = LNode.Call(CodeSymbols.Assign, LNode.List(dict[key], LNode.Literal(1))).SetStyle(NodeStyle.Operator);");
			TestEcs("q = quote(hello + $x);",
				   @"q = LNode.Call(CodeSymbols.Add, LNode.List(LNode.Id((Symbol) ""hello""), x)).SetStyle(NodeStyle.Operator);");
			TestEcs("quote { (x); }",
				   @"LNode.Id(LNode.List(LNode.InParensTrivia), (Symbol) ""x"");");
			TestEcs("rawQuote { Func($Foo); }",
					@"LNode.Call((Symbol) ""Func"", LNode.List(LNode.Call(CodeSymbols.Substitute, LNode.List(LNode.Id((Symbol) ""Foo"")))));");
			TestEcs("quote(Foo($first, $(...rest)));",
				   @"LNode.Call((Symbol) ""Foo"", LNode.List().Add(first).AddRange(rest));");
			TestEcs("quote(Foo($(...args)));",
				   @"LNode.Call((Symbol) ""Foo"", LNode.List(args));");
			TestEcs("quote { [$(...attrs)] public X; }",
				   @"LNode.Id(LNode.List().AddRange(attrs).Add(LNode.Id(CodeSymbols.Public)), (Symbol)""X"");");
			TestEcs("quote(a, b);",
				   @"LNode.Call(CodeSymbols.Splice, LNode.List(LNode.Id((Symbol)""a""), LNode.Id((Symbol)""b"")));");
			TestEcs("quote { a; b; }",
				   @"LNode.Call(CodeSymbols.Splice, LNode.List(LNode.Id((Symbol)""a""), LNode.Id((Symbol)""b"")));");
		}
	}
}
