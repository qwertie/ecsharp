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
			Test(Mode.Expr, 1, "a MOD b * c",   F.Call("'MOD", a, F.Call(S.Mul, b, c)));
			Test(Mode.Expr, 1, "a MOD b - c",   F.Call(S.Sub, F.Call("'MOD", a, b), c));
			Test(Mode.Expr, 1, "a + b MOD c",   F.Call(S.Add, a, F.Call("'MOD", b, c)));
		}

		[Test]
		public void MissingExpressions()
		{
			// Given that {;} is not an error, the grammar is a bit simpler if
			// other things like Foo(x,) are also not errors.
			Test(Mode.Stmt, 0, "{;}", F.Braces(F.Missing));
			Test(Mode.Stmt, 0, "(x;)", F.Call(S.Tuple, x));
			Test(Mode.Stmt, 0, "(;;)", F.Call(S.Tuple, F.Missing, F.Missing));
			Test(Mode.Stmt, 0, "(x,)", F.Call(S.Tuple, x, F.Missing));
			Test(Mode.Stmt, 0, "(,x)", F.Call(S.Tuple, F.Missing, x));
			Test(Mode.Stmt, 0, "(,)", F.Call(S.Tuple, F.Missing, F.Missing));
			Test(Mode.Stmt, 0, "Foo(, x)", F.Call(Foo, F.Missing, x));
			Test(Mode.Stmt, 0, "Foo(x,)", F.Call(Foo, x, F.Missing));
			Test(Mode.Stmt, 0, "Foo(x;)", F.Call(Foo, x));
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
			Test(Mode.Stmt, 1, @"{ b(x }", F.Braces(F.Call(b, x)));
			Test(Mode.Stmt, 1, @"{ b(x } + 1", F.Call(S.Add, F.Braces(F.Call(b, x)), one));
		}

		[Test]
		public void OtherParseErrors()
		{
			Test(Mode.Stmt, 1, "_u\"-2\";", F.Literal(new CustomLiteral("-2", (Symbol)"_u")));
			Test(Mode.Stmt, 1, "Foo(", F.Call(Foo));
			Test(Mode.Stmt, 1, "{Foo", F.Braces(Foo));
			Test(Mode.Stmt, 1, "{Foo(x", F.Braces(F.Call(Foo, x)));
			Test(Mode.Stmt, 1, "{Foo(x,", F.Braces(F.Call(Foo, x, F.Missing)));
			Test(Mode.Stmt, 1, "{Foo(x}\n a \n b (c)", F.Braces(F.Call(Foo, x)), a, F.Call(b, c));
			Test(Mode.Stmt, 1, ".for (", F.Call(S.For, F.Tuple()));
			// Ensure that the line b = x is not discarded
			Test(Mode.Stmt, 1, @"{
				a = 1)
				b = 2
			}", F.Braces(F.Call(S.Assign, a, one), F.Call(S.Assign, b, two)));

			// semicolons are allowed after args
			Test(Mode.Stmt, 0, "Foo(a; b)", F.Call(Foo, a, b));
			Test(Mode.Stmt, 0, "Foo(a; b;)", F.Call(Foo, a, b));

			Test(Mode.Expr, 2, ".`foo`", F.Id("foo"));
		}

		[Test]
		public void ParseBug()
		{
			Test(Mode.Stmt, 0, "a - 2 ** b", F.Call(S.Sub, a, F.Call(S.Exp, two, b)));
			// This was parsed as (a - 2) ** b
			Test(Mode.Stmt, 0, "a -2 ** b", F.Call(S.Sub, a, F.Call(S.Exp, two, b)));
		}

		[Test(Fails = "TODO")]
		public void LineContinuators()
		{
			// issue 86: https://github.com/qwertie/ecsharp/issues/86
		}

		[Test(Fails = "TODO")]
		public void TokenLists()
		{
		}

		protected override MessageHolder Test(Mode mode, int errorsExpected, string text, params LNode[] expected)
		{
			var messages = new MessageHolder();
			var results = Les3LanguageService.Value.Parse(text, messages, mode == Mode.Expr ? ParsingMode.Expressions : ParsingMode.Statements, true).ToList();
			if (messages.List.Count != System.Math.Max(errorsExpected, 0))
			{
				messages.WriteListTo(ConsoleMessageSink.Value);
				AreEqual(errorsExpected, messages.List.Count, 
					"Error count was {0} for «{1}»", messages.List.Count, text); // fail
			}
			for (int i = 0; i < expected.Length; i++)
				AreEqual(expected[i], results.TryGet(i, null));
			AreEqual(expected.Length, results.Count, "Got more result nodes than expected");
			return messages;
		}
	}
}
