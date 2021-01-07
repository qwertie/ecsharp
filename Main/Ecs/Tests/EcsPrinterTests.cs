using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;
using Loyc.Syntax;
using S = Loyc.Ecs.EcsCodeSymbols;

namespace Loyc.Ecs.Tests
{
	/// <summary>EC# node printer tests. Most of the tests are inherited.</summary>
	[TestFixture]
	public class EcsNodePrinterTests : EcsPrinterAndParserTests
	{
		protected override void Stmt(string expected, LNode input, Action<EcsPrinterOptions> configure = null, Mode mode = Mode.Both)
		{
			bool exprMode = (mode & Mode.Expression) != 0;
			if ((mode & Mode.PrinterTest) == 0)
				return;

			var options = new EcsPrinterOptions();
			options.IndentString = "  ";
			// by default, test the mode that is more difficult to get right
			options.AllowChangeParentheses = false;
			// TODO: make round tripping work without this
			options.NewlineOptions &= ~(NewlineOpt.AfterOpenBraceInNewExpr | NewlineOpt.BeforeCloseBraceInNewExpr);
			if (configure != null)
				configure(options);
			var sb = new StringBuilder();
			var mode2 = exprMode ? ParsingMode.Expressions : ParsingMode.Statements;
			if (input.Calls(S.Splice) && !input.HasAttrs)
				EcsLanguageService.Value.Print(input.Args, sb, MessageSink.Default, mode2, options);
			else
				EcsLanguageService.Value.Print(input, sb, MessageSink.Default, mode2, options);
			AreEqual(expected, sb.ToString());
		}

		[Test]
		public void CommentTriviaPrinterTest()
		{
			// Test %spaces, which the parser/injector never produces:
			var stmt = Attr(F.Trivia(S.TriviaSLComment, "bx"), F.Call(S.TriviaTrailing, F.Trivia(S.TriviaSpaces, "\t\t"), F.Trivia(S.TriviaSLComment, "ax")), x);
			Stmt("//bx\nx;\t\t//ax", stmt);
			Expr("//bx\nx\t//ax",    stmt, p => p.OmitSpaceTrivia = true);
			Expr("//bx\nx\t\t//ax",  stmt);
			Stmt("//bx\nx;\t//ax",   stmt, p => p.OmitSpaceTrivia = true);
			Stmt("x;\t\t",           stmt, p => p.OmitComments = true);

			// Attach /*the variable*/ to child node `x` 
			// (this doesn't currently round-trip, but maybe it should)
			stmt = 
				Attr(F.Trivia(S.TriviaSLComment, " a block"), 
					F.Call(S.TriviaTrailing, F.Trivia(S.TriviaSLComment, " end of block")), 
					F.Braces(
						Attr(F.Trivia(S.TriviaSLComment, " set x to zero"),
							F.Call(S.TriviaTrailing,
								F.Trivia(S.TriviaSpaces, "  "),
								F.Trivia(S.TriviaSLComment, " x was set to zero")),
							F.Call(Attr(F.Trivia(S.TriviaMLComment, "is set to"), F.Id(S.Assign)), 
							    x, Attr(F.Trivia(S.TriviaMLComment, "its new value"), zero)))));
			Stmt("// a block\n{\n"+
				"  // set x to zero\n  x /*is set to*/= /*its new value*/0;  // x was set to zero\n"+
				"}\t// end of block", stmt);
		}

		[Test]
		public void RawText()
		{
			var stmt = F.Missing.PlusAttr(F.Trivia(S.TriviaRawText, "Eat my shorts!"))
				.PlusTrailingTrivia(F.Trivia(S.TriviaRawText, "...then do it again!"));
			Stmt("Eat my shorts!;...then do it again!", stmt);
			stmt = F.Call(S.If, a, F.Call(x)).PlusTrailingTrivia(F.Trivia(S.TriviaCsRawText, " // end if"));
			Stmt("if (a)\n  x(); // end if", stmt);
			Stmt("if (a)\n  x();", stmt, p => { p.ObeyRawText = false; p.OmitUnknownTrivia = true; });

			stmt = F.Call(S.Assign, x, F.Call(Foo))
				.PlusAttr(F.Trivia(S.TriviaCsPPRawText, "#if DoTheFoo"))
				.PlusTrailingTrivia(F.Trivia(S.TriviaCsPPRawText, "#endif"));
			Stmt("#if DoTheFoo\nx = Foo();\n#endif", stmt);

			var raw = F.Trivia(S.RawText, "hello!");
			Stmt("x(hello!);", F.Call(x, raw));
			Stmt("hello!();", F.Call(raw));
			Stmt("hello!();", F.Call(F.Trivia(S.CsRawText, "hello!")));
			Stmt("hello!", raw);
			Stmt("hello!", F.Call(S.RawText, F.Literal("hello!")));
			Stmt("hello!", F.Call(S.CsRawText, F.Literal("hello!")));
		}

		[Test]
		public void AvoidImplicitSpace()
		{
			// Minor fix: a space appeared before `b;` and `Foo;`
			Stmt("{\n  a;\n  b;\n}", F.Braces(a.PlusTrailingTrivia(F.TriviaNewline), b.PlusAttr(F.Id(S.TriviaAppendStatement))));
			Stmt("{\n  #line 1\n  Foo;\n  #line 2\n}", F.Braces(F.Trivia(S.CsPPRawText, "#line 1"), Foo, F.Trivia(S.CsPPRawText, "#line 2")));
			Stmt("{\n  #line 1\n  #line 2\n}",         F.Braces(F.Trivia(S.CsPPRawText, "#line 1"),      F.Trivia(S.CsPPRawText, "#line 2")));
		}

		[Test]
		public void ByteArrayTest()
		{
			// Printer supports byte arrays for the sake of the `binaryFile("...")` macro
			var bytes = new byte[] { 33,66,132,200 };
			Expr("new byte[] { 33,66,132,200\n}", F.Literal(bytes));
			Expr("new byte[] { 0x21,0x42,0x84,0xC8\n}", F.Literal(bytes).SetBaseStyle(NodeStyle.HexLiteral));
		}

		[Test]
		public void PrintEmptySpliceWithTrivia()
		{
			// Note (2020/09): normal parsing won't produce code like {/*comment*/} (using an 
			// empty-splice inside the braces) because StandardTriviaInjector doesn't know if the 
			// language supports it. A tree of the form Foo([@`%MLComment`("Hello")] #splice()) 
			// technically has one argument, so producing this carries the risk of confusing 
			// compiler front-ends which might treat it as a normal argument. However, LeMP can 
			// produce empty #splice() with trivia attached at the top level (file level), so the 
			// printer has support for empty-splice statements.
			Stmt("{\n  /*Hello*/\n}", F.Braces(F.Splice().PlusAttr(F.Trivia(S.TriviaMLComment, "Hello"))));
			Stmt("/*Hello?*/", F.Splice().PlusAttr(F.Trivia(S.TriviaMLComment, "Hello?")));
			Stmt("/*Hello!*/", F.Splice().PlusTrailingTrivia(F.Trivia(S.TriviaMLComment, "Hello!")));
		}

		int _testNum;
		void CheckIsComplexIdentifier(bool? result, LNode expr)
		{
			_testNum++;
			var isCI = EcsValidators.IsComplexIdentifier(expr, ICI.Default, EcsValidators.Pedantics.Strict);
			if (result == null && !isCI)
				return;
			else if (result == isCI)
				return;

			Assert.Fail(string.Format(
				"IsComplexIdentifier: fail on test #{0} '{1}'. Expected {2}, got {3}",
				_testNum, expr.ToString(), result, isCI));
		}

		[Test]
		public void IsComplexIdentifierTests()
		{
			_testNum = 0;
			CheckIsComplexIdentifier(true, a);                             // a
			CheckIsComplexIdentifier(true, F.Dot(a, b));                   // a.b
			CheckIsComplexIdentifier(null, F.Call(a, b, c));               // @`'.`(a, b, c)                       ==> true for target
			CheckIsComplexIdentifier(true, F.Dot(F.Dot(a, b), c));         // a.b.c       == @`'.`(@`'.`(a, b), c) ==> true
			CheckIsComplexIdentifier(true, F.Dot(a, b, c));                // a.b.c       == @`'.`(@`'.`(a, b), c) ==> true
			CheckIsComplexIdentifier(null, F.Dot(a, F.Dot(b, c)));         // @`'.`(a, b.c)
			CheckIsComplexIdentifier(true, F.Of(a, b));                    // a<b>        == @'of(a,b)          ==> true
			CheckIsComplexIdentifier(true, F.Of(_(S.Array), a));           // a[]         == @'of(@`[]`,a)      ==> true
			CheckIsComplexIdentifier(true, F.Dot(a, F.Of(b, F.Dot(c,x)))); // a.b<c.x>    == @'of(@'.(a,b),@'.(c,x)) ==> true
			CheckIsComplexIdentifier(false, F.Of(F.Dot(a,b),F.Dot(c,x)));  // @'of(@`'.`(a,b), @`'.`(c,x))      ==> false
			CheckIsComplexIdentifier(null, F.Call(a, x));                  // a(x)                              ==> true for target
			CheckIsComplexIdentifier(null, F.Call(F.Dot(a,b), x));         // a.b(x)      == @.(a,b)(x)         ==> true for target
			CheckIsComplexIdentifier(null, F.Call(F.Dot(a,F.Of(b,c)), c)); // a.b<c>(x)   == @'of(@'.(a,b),c)(x)==> true for target
			CheckIsComplexIdentifier(false, F.Call(F.Of(F.Dot(a,b),c), c)); // @'of(a.b, c)(c)                   ==> false
			CheckIsComplexIdentifier(false, F.Call(F.InParens(a), x));     // (a)(x)                            ==> false
			CheckIsComplexIdentifier(false, F.Call(F.InParens(F.Dot(a,b)),x));// (a.b)(x) == (@`'.`(a,b))(x)    ==> false
			CheckIsComplexIdentifier(null, F.Of(F.Of(a,b),c));             // @'of(a<b>,c) == @'of(@'of(a,b),c) ==> false
		}

		[Test]
		public void IsComplexIdentifierTestsWithAttributes()
		{
			_testNum = 0;
			CheckIsComplexIdentifier(true, F.Dot(OnNewLine(a), OnNewLine(b))); // a.b with trivia
			CheckIsComplexIdentifier(false, F.Dot(a, Attr(b, b)));         // a.([b] b)
			CheckIsComplexIdentifier(false, F.Dot(Attr(a, a), b));         // ([a] a).b
			CheckIsComplexIdentifier(true, F.Of(OnNewLine(a), OnNewLine(b))); // a<b> with trivia
			CheckIsComplexIdentifier(false, F.Of(Attr(a, a), b));          // a<b> with attribute on a
			CheckIsComplexIdentifier(false, F.Of(a, Attr(b, b)));          // a<b> with attribute on b
			CheckIsComplexIdentifier(true, F.Dot(a, F.Of(b, F.Dot(OnNewLine(c),OnNewLine(x))))); // a.b<c.x> with trivia
			CheckIsComplexIdentifier(false, F.Dot(a, F.Of(b, F.Dot(Attr(c, c),Attr(x, x))))); // a.b<c.x> with attributes in there
		}

		[Test]
		public void StaticMethods()
		{
			AreEqual("@this",            EcsNodePrinter.PrintId(GSymbol.Get("this"), EcsNodePrinter.IdPrintMode.Normal));
			AreEqual("normal_id",        EcsNodePrinter.PrintId(GSymbol.Get("normal_id"), EcsNodePrinter.IdPrintMode.Normal));
			AreEqual("operator+",        EcsNodePrinter.PrintId(S.Add, EcsNodePrinter.IdPrintMode.Operator));
			AreEqual("operator`frack!`", EcsNodePrinter.PrintId(GSymbol.Get("frack!"), EcsNodePrinter.IdPrintMode.Operator));
			AreEqual(@"@@`frack!`",      EcsNodePrinter.PrintSymbolLiteral(GSymbol.Get("frack!")));
			AreEqual(@"@@this",          EcsNodePrinter.PrintSymbolLiteral(GSymbol.Get("this")));
		}

		[Test]
		public void SaveRangeIsCalled()
		{
			var ranges = new List<Triplet<ILNode, IndexRange, int>>();
			var options = new LNodePrinterOptions { SaveRange = (n, r, d) => ranges.Add(Triplet.Create(n, r, d)) };

			LNode node = F.Var(F.Int32, F.Call(S.Assign, x, two));
			Stmt("int x = 2;", node);
			string output = EcsLanguageService.Value.Print(node, null, ParsingMode.Statements, options);
			ExpectSavedRange(ranges, output, node, "int x = 2;");
			ExpectSavedRange(ranges, output, node[0], "int");
			ExpectSavedRange(ranges, output, node[1], "x = 2");
			ExpectSavedRange(ranges, output, node[1][0], "x");
			ExpectSavedRange(ranges, output, node[1][1], "2");
			ExpectSavedRange(ranges, output, node[1].Target, "=");

			ranges.Clear();
			LNode body;
			node = F.Fn(F.Void, _("MyMethod"), F.AltList(), body = F.Call(Foo, F.Call(S.Add, x, one)));
			Stmt("void MyMethod() => Foo(x + 1);", node);
			output = EcsLanguageService.Value.Print(node, null, ParsingMode.Statements, options);
			ExpectSavedRange(ranges, output, node, "void MyMethod() => Foo(x + 1);");
			ExpectSavedRange(ranges, output, node[0], "void");
			ExpectSavedRange(ranges, output, node[1], "MyMethod");
			ExpectSavedRange(ranges, output, node[2], "()");
			ExpectSavedRange(ranges, output, body, "Foo(x + 1)");
			ExpectSavedRange(ranges, output, body.Target, "Foo");
			ExpectSavedRange(ranges, output, body[0], "x + 1");
			ExpectSavedRange(ranges, output, body[0][0], "x");
			ExpectSavedRange(ranges, output, body[0][1], "1");
			ExpectSavedRange(ranges, output, body[0].Target, "+");
		}

		private void ExpectSavedRange(List<Triplet<ILNode, IndexRange, int>> ranges, string output, LNode node, string expectedSubstring)
		{
			foreach (var pair in ranges)
			{
				// Subtlety: if(node==pair.A) doesn't work for typical Target nodes, which 
				//           are regenerated each time Target is called; use Equals instead
				if (node.Equals(pair.A))
				{
					AreEqual(expectedSubstring, output.Substring(pair.B.StartIndex, pair.B.Length));
					return;
				}
			}
			Fail("Saved range not found for {0}", node);
		}
	}
}
