using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;
using Loyc.Collections;

namespace Loyc.Syntax.Les
{
	using S = CodeSymbols;

	[TestFixture]
	public class LesParserTests : LesPrinterAndParserTests
	{
		[Test]
		public void EmptyInput()
		{
			Test(Mode.Stmt, 0, "");
		}

		[Test]
		public void ParseErrors()
		{
			MessageHolder msgs;
			// Expected ';'
			msgs = Test(Mode.Stmt, 2, "a = ) b c 1", F.Call(S.Assign, a, F.Missing));
			ExpectMessageContains(msgs, "';'");
			// Missing subexpression
			msgs = Test(Mode.Stmt, 1, @"a ** b + ;", F.Call(S.Add, F.Call(S.Exp, a, b), F.Missing));
			ExpectMessageContains(msgs, "expected a particle");
			// Invalid call
			msgs = Test(Mode.Stmt, 1, "x = Foo ();", F.Call(S.Assign, x, Foo));
			ExpectMessageContains(msgs, "call was intended", "space(s) before '('");
			msgs = Test(Mode.Stmt, 1, "5 (x);", F.Literal(5));
			ExpectMessageContains(msgs, "call was intended", "space(s) before '('");
			// Invalid superexpressions
			msgs = Test(Mode.Stmt, 1, "a; 5 c b; x();", a, F.Literal(5));
			ExpectMessageContains(msgs, "';'");
			msgs = Test(Mode.Stmt, 1, "get Foo { x } = 0;", F.Call(S.get, Foo, F.Braces(x)));
			ExpectMessageContains(msgs, "Assignment", "';'");
			msgs = Test(Mode.Stmt, 1, "if(a) > b { c(); };", F.Call(S.GT, F.Call("if", a), b));
			ExpectMessageContains(msgs, "'{'", "';'");
			msgs = Test(Mode.Stmt, 1, "{ a + b c }; Foo();", F.Braces(F.Call(S.Add, a, b)), F.Call(Foo));
			ExpectMessageContains(msgs, "Id", "'}'");
			msgs = Test(Mode.Stmt, 1, "a.b c", F.Dot(a, b));
			msgs = Test(Mode.Stmt, 1, "a + b.c {} Foo", F.Call(S.Add, a, F.Dot(b, c)));
			msgs = Test(Mode.Stmt, 1, "a(b) c", F.Call(a, b));
			msgs = Test(Mode.Stmt, 1, "a.Foo(b) c", F.Call(F.Dot(a, Foo), b));
			msgs = Test(Mode.Stmt, 1, @"a(); if c (b)
				Foo()", F.Call(a), F.Call("if", c, F.InParens(b), Foo, F.Tuple()));
			ExpectMessageContains(msgs, "expected a space before '('");
		}

		[Test]
		public void SemicolonCommaErrors()
		{
			MessageHolder msgs;
			msgs = Test(Mode.Expr, 0, "a, b", a, b);
			msgs = Test(Mode.Stmt, 1, "a, b", a, b);
			ExpectMessageContains(msgs, "';'", "','");
			msgs = Test(Mode.Stmt, 1, "(a, b)", F.Tuple(a, b));
			ExpectMessageContains(msgs, "';'");
			msgs = Test(Mode.Stmt, 1, "{a, b}", F.Braces(a, b));
			ExpectMessageContains(msgs, "';'");
			Test(Mode.Stmt, 0, "Foo(a; b)", F.Call(Foo, a, b));
			Test(Mode.Stmt, 0, "Foo!(a, b)", F.Of(Foo, a, b));
			Test(Mode.Stmt, 0, "Foo!(a; b)", F.Of(Foo, a, b));
			Test(Mode.Stmt, 2, "Foo(a; b, c, 0)", F.Call(Foo, a, b, c, zero));
			Test(Mode.Stmt, 3, "Foo!(a, b; c; 0;)", F.Of(Foo, a, b, c, zero));
		}

		[Test]
		public void MiscibilityErrors()
		{
			var msgs = Test(Mode.Expr, 1, "x & Foo == 0",   F.Call(S.AndBits, x, F.Call(S.Eq, Foo, zero)));
			ExpectMessageContains(msgs, "'==' is not allowed in this context");
			Test(Mode.Expr, 0, "x >> 1 == a",    F.Call(S.Eq, F.Call(S.Shr, x, one), a));
			Test(Mode.Expr, 1, "x >> a + 1",     F.Call(S.Shr, x, F.Call(S.Add, a, one)));
			Test(Mode.Expr, 0, "x >> a**1",      F.Call(S.Shr, x, F.Call(S.Exp, a, one)));
			Test(Mode.Expr, 1, "x `Foo` a .. b", F.Call(Foo, x, F.Call(S.DotDot, a, b)));
			Test(Mode.Expr, 1, "x `Foo` a*b",    F.Call(Foo, x, F.Call(S.Mul, a, b)));
			Test(Mode.Stmt, 0, "x `Foo` a**b;",  F.Call(Foo, x, F.Call(S.Exp, a, b)));
			Test(Mode.Expr, 0, "x `Foo` 1 == a", F.Call(S.Eq, F.Call(Foo, x, one), a));
			Test(Mode.Expr, 1, "Foo * a `*` b * c", F.Call(S.Mul, F.Call(S.Mul, Foo, a), F.Call(S.Mul, b, c)));
		}

		[Test]
		public void PythonModeAkaISM()
		{
			// See also IndentTokenGeneratorTests, which tests LesIndentTokenGenerator
			Stmt(@"Foo:
					a(b)", F.Call(Foo, F.Braces(F.Call(a, b))));
			Stmt(@"
				try:
					eat;
				:catch:
					crumbs
				:finally:
					hunger satisfied;".Replace("\t\t\t\t", ""),
				F.Call("try", F.Braces(_("eat")),
					 _("catch"), F.Braces(_("crumbs")),
					 _("finally"), F.Braces(F.Call("hunger", _("satisfied")))));
			Test(Mode.Stmt, 0, @"
				if a:
					a();
				:else if b:
				.	c = b();
				.	while Foo:
				.	.	c()
				return:
				.	Foo".Replace("\t\t\t\t", ""),
				F.Call("if", a, F.Braces(F.Call(a)), _("else"), _("if"), b,
					F.Braces(F.Call(S.Assign, c, F.Call(b)), F.Call("while", Foo, F.Braces(F.Call(c))))),
				F.Call("return", F.Braces(Foo)));
		}

		protected override MessageHolder Test(Mode mode, int errorsExpected, string str, params LNode[] expected)
		{
			var messages = new MessageHolder();
			var results = LesLanguageService.Value.Parse(str, messages, mode == Mode.Expr ? ParsingMode.Exprs : ParsingMode.Stmts).ToList();
			for (int i = 0; i < expected.Length; i++)
				AreEqual(expected[i], results[i]);
			AreEqual(expected.Length, results.Count);
			if (messages.List.Count != System.Math.Max(errorsExpected, 0))
			{
				messages.WriteListTo(MessageSink.Console);
				AreEqual(errorsExpected, messages.List.Count); // fail
			}
			return messages;
		}
	}
}
