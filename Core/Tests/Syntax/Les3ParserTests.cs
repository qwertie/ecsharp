using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;
using Loyc.Collections;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.Syntax.Les
{
	[TestFixture]
	public class Les3ParserTests : Les3PrinterAndParserTests
	{
		[Test]
		public void ImmiscibilityErrors()
		{
			Test(Mode.Expr, 0, "a == b > c",    F.Call(S.GT, F.Call(S.Eq, a, b), c));
			Test(Mode.Expr, 0, "2 * x << 1;",   F.Call(S.Shl, F.Call(S.Mul, two, x), one));
			Test(Mode.Expr, 0, "a & b | c;",    F.Call(S.OrBits, F.Call(S.AndBits, a, b), c));
			Test(Mode.Expr, 1, "x & Foo == 0;", F.Call(S.Eq, F.Call(S.AndBits, x, Foo), zero));
			Test(Mode.Expr, 1, "0 == x & Foo;", F.Call(S.Eq, zero, F.Call(S.AndBits, x, Foo)));
			Test(Mode.Expr, 1, "x | 1 != 1",    F.Call(S.NotEq, F.Call(S.OrBits, x, one), one));
			Test(Mode.Expr, 1, "x ^ a >= 1",    F.Call(S.GE, F.Call(S.XorBits, x, a), one));
			Test(Mode.Expr, 1, "x >> a + 1;",   F.Call(S.Add, F.Call(S.Shr, x, a), one));
			Test(Mode.Expr, 1, "x << 1 - 1",    F.Call(S.Sub, F.Call(S.Shl, x, one), one));
			Test(Mode.Expr, 1, "1 + x << a;",   F.Call(S.Add, one, F.Call(S.Shl, x, a)));
			// Uppercase word ops: immiscibility was cancelled for issue #106 backquoted operators.
			// The only remaining error is mixing shift (>> <<) with UpperCaseWords.
			Test(Mode.Expr, 0, "a MOD b * c",   F.Call("'MOD", a, F.Call(S.Mul, b, c)));
			Test(Mode.Expr, 0, "a MOD b - c",   F.Call(S.Sub, F.Call("'MOD", a, b), c));
			Test(Mode.Expr, 0, "a + b MOD c",   F.Call(S.Add, a, F.Call("'MOD", b, c)));
			Test(Mode.Expr, 1, "a >> b REM c",  F.Call("'REM", F.Call(S.Shr, a, b), c));
			Test(Mode.Expr, 1, "a REM b << c",  F.Call(S.Shl, F.Call("'REM", a, b), c));
		}

		[Test]
		public void MissingExpressions()
		{
			// Given that {;} is not an error, the grammar is a bit simpler if
			// other things like Foo(x,) are also not errors.
			Test(Mode.Stmt, 0, "{\n  ;\n}", F.Braces(F.Missing));
			Test(Mode.Stmt, 0, "(a;)", F.Call(S.Tuple, a));
			Test(Mode.Stmt, 0, "(;;)", F.Call(S.Tuple, F.Missing, F.Missing));
			Test(Mode.Stmt, 0, "(b,)", F.Call(S.Tuple, b, F.Missing));
			Test(Mode.Stmt, 0, "(,x)", F.Call(S.Tuple, F.Missing, x));
			Test(Mode.Stmt, 0, "(,)", F.Call(S.Tuple, F.Missing, F.Missing));
			Test(Mode.Stmt, 0, "Foo(, x)", F.Call(Foo, F.Missing, x));
			Test(Mode.Stmt, 0, "Foo(x,)", F.Call(Foo, x, F.Missing));
			Test(Mode.Stmt, 0, "Foo(a;)", F.Call(Foo, a));
			Test(Mode.Stmt, 0, "Foo(,)", F.Call(Foo, F.Missing, F.Missing));
		}

		[Test]
		public void BracketMismatchErrors()
		{
			Test(Mode.Stmt, 1, "(", F.Tuple());
			var tree1 = F.Call(S.Add, F.Call(a, F.Call(S.IndexBracks, b, zero)), one);
			var tree2 = F.Call(S.Add, F.Call(S.IndexBracks, a, F.Call(b, zero)), one);
			Test(Mode.Stmt, 1, "a(b[0) + 1", tree1);
			Test(Mode.Stmt, 1, "a[b(0] + 1", tree2);
			Test(Mode.Stmt, 2, @"{
				a(b[0) + 1
				a[b(0] + 1
			}", F.Braces(tree1, tree2));
			Test(Mode.Stmt, 1, @"{
				a = 1)
				b = 2
			}", F.Braces(F.Call(S.Assign, a, one), F.Call(S.Assign, b, two)));
			Test(Mode.Stmt, 1, @"{ b(x }", F.Braces(AppendStmt(F.Call(b, x))));
			Test(Mode.Stmt, 1, "{\n  b(x }", F.Braces(F.Call(b, x)));
			Test(Mode.Stmt, 1, "{\n  b(x } + 1", F.Call(S.Add, F.Braces(F.Call(b, x)), one));
		}

		[Test]
		public void OtherParseErrors()
		{
			Test(Mode.Stmt, 1, "_u\"-2\";", F.Literal((UString)"-2", (Symbol)"_u"));
			Test(Mode.Stmt, 1, "Foo(", F.Call(Foo));
			Test(Mode.Stmt, 1, "{\n  Foo", F.Braces(Foo));
			Test(Mode.Stmt, 1, "{\n  Foo(x", F.Braces(F.Call(Foo, x)));
			Test(Mode.Stmt, 1, "{\n  Foo(x,", F.Braces(F.Call(Foo, x, F.Missing)));
			Test(Mode.Stmt, 1, "{\n  Foo(x}\n a \n b (c)", F.Braces(F.Call(Foo, x)), a, F.Call(b, c));
			Test(Mode.Stmt, 1, ".for (", F.Call(S.For, F.Tuple()));
			// Ensure that the line b = x is not discarded
			Test(Mode.Stmt, 1, @"{
				a = 1)
				b = 2
			}", F.Braces(F.Call(S.Assign, a, one), F.Call(S.Assign, b, two)));

			// semicolons are allowed after args
			Test(Mode.Stmt, 0, "Foo(a; b)", F.Call(Foo, a, b));
			Test(Mode.Stmt, 0, "Foo(a; b;)", F.Call(Foo, a, b));

			Test(Mode.Expr, 1, ".`Foo`", F.Dot(F.Missing, Foo));
			Test(Mode.Stmt, 1, @"Foo(x, \hello)", F.Call(Foo, x, F.Missing));
			Test(Mode.Stmt, 1, @"(\hello)", F.Tuple());
		}

		[Test]
		public void ParseBug()
		{
			Test(Mode.Stmt, 0, "a - 2 ** b", F.Call(S.Sub, a, F.Call(S.Exp, two, b)));
			// This was parsed as (a - 2) ** b
			Test(Mode.Stmt, 0, "a -2 ** b", F.Call(S.Sub, a, F.Call(S.Exp, two, b)));
		}

		[Test]
		public void LiteralStylesArePreserved()
		{
			var les3 = Les3LanguageService.Value;
			Assert.AreEqual(les3.Parse("0x3333").Single().BaseStyle, NodeStyle.HexLiteral);
			Assert.AreEqual(les3.Parse("0b1011").Single().BaseStyle, NodeStyle.BinaryLiteral);
			Assert.AreEqual(les3.Parse("123456").Single().BaseStyle, NodeStyle.Default);
			Assert.AreEqual(les3.Parse("\"!!\"").Single().BaseStyle, NodeStyle.Default);
			Assert.AreEqual(les3.Parse("'''!'''").Single().BaseStyle, NodeStyle.TQStringLiteral);
			Assert.AreEqual(les3.Parse("\"\"\"!\"\"\"").Single().BaseStyle, NodeStyle.TDQStringLiteral);
		}

		[Test]
		public void LineContinuators()
		{
			// issue 86: https://github.com/qwertie/ecsharp/issues/86
			Test(Mode.Stmt, 0, "a =\n|b",  F.Call(S.Assign, a, OnNewLine(b)));
			Test(Mode.Stmt, 0, "b\n| = c", F.Call(OnNewLine(F.Id(S.Assign)), b, c));
			Test(Mode.Stmt, 0, "b\n|= c",  F.Call(OnNewLine(F.Id(S.Assign)), b, c));
			Test(Mode.Stmt, 0, "Foo(\n| a, b)", F.Call(Foo, OnNewLine(a), b));
			Test(Mode.Stmt, 0, "{\n  MoveTo(x, a)\n  | .LineTo(x, b)\n}", F.Braces(
				F.Call(F.Call(OnNewLine(F.Id(S.Dot)), F.Call("MoveTo", x, a), F.Id("LineTo")), x, b)));
			var stmt = OnNewLine(F.Call(
				S.If, F.Call(S.GT, a, b),
					OnNewLine(OnNewLine(F.Call(
						F.Id(S.Braces).PlusTrailingTrivia(F.Trivia(S.TriviaSLComment, " comment")),
						F.Call(_("aIsBigger"))))),
					OnNewLine(OnNewLine(F.Call("#else", 
						F.Braces(AppendStmt(F.Call(Foo))))))));
			Test(Mode.Stmt, 0, @"
				.if a > b
				|
				{   // comment
					aIsBigger()
				}
				|
				else { Foo() }",
				stmt);
		}

		protected override MessageHolder Test(Mode mode, int errorsExpected, LNodePrinterOptions printerOptions, string text, params LNode[] expected)
		{
			var messages = new MessageHolder();
			var les3 = Les3LanguageService.Value;
			var results = les3.Parse(text, messages, mode == Mode.Expr ? ParsingMode.Expressions : ParsingMode.Statements, true).ToList();
			if (messages.List.Count != System.Math.Max(errorsExpected, 0))
			{
				messages.WriteListTo(ConsoleMessageSink.Value);
				int errorCount = messages.List.Count(msg => msg.Severity >= Severity.Error);
				AreEqual(errorsExpected, errorCount, 
					"Error count was {0} for «{1}»", errorCount, text); // fail
			}
			for (int i = 0; i < expected.Length; i++) {
				LNode expect = expected[i], actual = results.TryGet(i, null);
				if (!expect.Equals(actual, LNode.CompareMode.TypeMarkers)) {
					var options = new Les3PrinterOptions { PrintTriviaExplicitly = true, IndentString = "  " };
					AreEqual(les3.Print(expect, null, null, options), les3.Print(actual, null, null, options));
					AreEqual(expect, actual);
					Fail("{0} has a different type marker than {1}", expect, actual);
				}
			}
			AreEqual(expected.Length, results.Count, "Got more result nodes than expected");
			return messages;
		}
	}
}
