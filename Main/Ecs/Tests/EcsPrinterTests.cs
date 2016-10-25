using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;
using Loyc.Syntax;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.Ecs.Tests
{
	////////////////////////////////////////////////////////////////////////////
	/// <summary>EC# node printer tests</summary>
	[TestFixture]
	public class EcsNodePrinterTests : EcsPrinterAndParserTests
	{
		protected override void Stmt(string result, LNode input, Action<EcsNodePrinter> configure = null, Mode mode = Mode.Both)
		{
			bool exprMode = (mode & Mode.Expression) != 0;
			if ((mode & Mode.PrinterTest) == 0)
				return;

			var sb = new StringBuilder();
			var printer = EcsNodePrinter.New(sb, "  ");
			printer.AllowChangeParentheses = false;
			// TODO: make round tripping work without this
			printer.NewlineOptions &= ~(NewlineOpt.AfterOpenBraceInNewExpr | NewlineOpt.BeforeCloseBraceInNewExpr);
			if (configure != null)
				configure(printer);
			if (exprMode)
				printer.Print(input, ParsingMode.Expressions);
			else if (input.Calls(S.Splice))
				((LNodePrinter)printer.Print).PrintMultiple(input.Args, printer.Errors, ParsingMode.Statements, sb: sb);
			else
				printer.Print(input, ParsingMode.Statements);
			AreEqual(result, sb.ToString());
		}

		[Test]
		public void CommentTriviaPrinterTest()
		{
			// Test #trivia_spaces, which the parser/injector never produces:
			var stmt = Attr(F.Trivia(S.TriviaSLComment, "bx"), F.Id(S.TriviaBeginTrailingTrivia), F.Trivia(S.TriviaSpaces, "\t\t"), F.Trivia(S.TriviaSLComment, "ax"), x);
			Stmt("//bx\nx;\t\t//ax", stmt);
			Expr("//bx\nx\t//ax",     stmt, p => p.OmitSpaceTrivia = true);
			Expr("//bx\nx\t\t//ax",  stmt);
			Stmt("//bx\nx;\t//ax",    stmt, p => p.OmitSpaceTrivia = true);
			Stmt("x;\t\t",           stmt, p => p.OmitComments = true);

			// Attach /*the variable*/ to child node `x` 
			// (this doesn't currently round-trip, but maybe it should)
			stmt = 
				Attr(F.Trivia(S.TriviaSLComment, " a block"), 
					F.Id(S.TriviaBeginTrailingTrivia),
					F.Trivia(S.TriviaSLComment, " end of block"), F.Braces(
					Attr(F.Trivia(S.TriviaSLComment, " set x to zero"),
						F.Id(S.TriviaBeginTrailingTrivia),
						F.Trivia(S.TriviaSpaces, "  "),
						F.Trivia(S.TriviaSLComment, " x was set to zero"),
						F.Call(Attr(F.Trivia(S.TriviaMLComment, "is set to"), F.Id(S.Assign)), x,
									  Attr(F.Trivia(S.TriviaMLComment, "its new value"), zero))
					)));
			Stmt("// a block\n{\n"+
				"  // set x to zero\n  x /*is set to*/= /*its new value*/0;  // x was set to zero\n"+
				"}\t// end of block", stmt);
		}

		[Test]
		public void RawText()
		{
			var stmt = Attr(F.Trivia(S.TriviaRawTextBefore, "Eat my shorts!"), 
				F.Trivia(S.TriviaRawTextAfter, "...then do it again!"), F.Missing);
			Stmt("Eat my shorts!;...then do it again!", stmt);
			stmt = Attr(F.Trivia(S.TriviaRawTextAfter, " // end if"), F.Call(S.If, a, F.Call(x)));
			Stmt("if (a)\n  x(); // end if", stmt);
			Stmt("if (a)\n  x();", stmt, p => { p.ObeyRawText = false; p.OmitUnknownTrivia = true; });
			
			var raw = F.Trivia(S.RawText, "hello!");
			Stmt("x(hello!);", F.Call(x, raw));
			raw = F.Call(S.RawText, F.Literal("hello!"));
			Stmt("hello!", raw);
			raw = F.Call(raw);
			Stmt("hello!();", raw);
		}

		int _testNum;
		void CheckIsComplexIdentifier(bool? result, LNode expr)
		{
			_testNum++;
			var isCI = EcsValidators.IsComplexIdentifier(expr);
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
			CheckIsComplexIdentifier(null, F.Call(a, b, c));               // #.(a, b, c)                      ==> true for target
			CheckIsComplexIdentifier(true, F.Dot(F.Dot(a, b), c));         // a.b.c       == #.(#.(a, b), c)   ==> true
			CheckIsComplexIdentifier(true, F.Dot(a, b, c));                // a.b.c       == #.(#.(a, b), c)   ==> true
			CheckIsComplexIdentifier(null, F.Dot(a, F.Dot(b, c)));         // #.(a, b.c)
			CheckIsComplexIdentifier(true, F.Of(a, b));                    // a<b>        == #of(a,b)          ==> true
			CheckIsComplexIdentifier(true, F.Of(_(S.Array), a));           // a[]         == #of(@`[]`,a)      ==> true
			CheckIsComplexIdentifier(true, F.Of(F.Dot(a,b),F.Dot(c,x)));   // a.b<c.x>    == #of(@.(a,b),@.(c,x)) ==> true
			CheckIsComplexIdentifier(null, F.Call(a, x));                  // a(x)                             ==> true for target
			CheckIsComplexIdentifier(null, F.Call(F.Dot(a,b), x));         // a.b(x)      == @.(a,b)(x)        ==> true for target
			CheckIsComplexIdentifier(null, F.Call(F.Of(F.Dot(a,b),c), c)); // a.b<c>(x)   == #of(@.(a,b),c)(x) ==> true for target
			CheckIsComplexIdentifier(false, F.Call(F.InParens(a), x));     // (a)(x)                           ==> false
			CheckIsComplexIdentifier(false, F.Call(F.InParens(F.Dot(a,b)),x));// (a.b)(x) == (#.(a,b))(x)      ==> false
			CheckIsComplexIdentifier(null, F.Of(F.Of(a,b),c));             // #of(a<b>,c) == #of(#of(a,b),c)   ==> false
		}

		[Test]
		public void StaticMethods()
		{
			AreEqual("@this",            EcsNodePrinter.PrintId(GSymbol.Get("this"), false));
			AreEqual("normal_id",        EcsNodePrinter.PrintId(GSymbol.Get("normal_id"), false));
			AreEqual("operator+",        EcsNodePrinter.PrintId(S.Add, true));
			AreEqual("operator`frack!`", EcsNodePrinter.PrintId(GSymbol.Get("frack!"), true));
			AreEqual(@"@@`frack!`",      EcsNodePrinter.PrintSymbolLiteral(GSymbol.Get("frack!")));
			AreEqual(@"@@this",          EcsNodePrinter.PrintSymbolLiteral(GSymbol.Get("this")));
		}
	}
}
