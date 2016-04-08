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
			var printer = EcsNodePrinter.New(input, sb, "  ");
			printer.AllowChangeParentheses = false;
			printer.NewlineOptions &= ~(NewlineOpt.AfterOpenBraceInNewExpr | NewlineOpt.BeforeCloseBraceInNewExpr);
			if (configure != null)
				configure(printer);
			if (exprMode)
				printer.PrintExpr();
			else
				printer.PrintStmt();
			AreEqual(result, sb.ToString());
		}

		[Test]
		public void RawText()
		{
			var stmt = Attr(F.Trivia(S.TriviaRawTextBefore, "Eat my shorts!"), 
				F.Trivia(S.TriviaRawTextAfter, "...then do it again!"), F.Missing);
			Stmt("Eat my shorts!;...then do it again!", stmt);
			stmt = Attr(F.Trivia(S.TriviaRawTextAfter, " // end if"), F.Call(S.If, a, F.Call(x)));
			Stmt("if (a)\n  x(); // end if", stmt);
			Stmt("if (a)\n  x();", stmt, p => p.OmitRawText = true);
			
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
			var np = new EcsNodePrinter(expr, null);
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
			AreEqual("operator+",        EcsNodePrinter.PrintId(GSymbol.Get("+"), true));
			AreEqual("operator`frack!`", EcsNodePrinter.PrintId(GSymbol.Get("frack!"), true));
			AreEqual(@"@@`frack!`",      EcsNodePrinter.PrintSymbolLiteral(GSymbol.Get("frack!")));
			AreEqual(@"@@this",          EcsNodePrinter.PrintSymbolLiteral(GSymbol.Get("this")));
		}
	}
}
