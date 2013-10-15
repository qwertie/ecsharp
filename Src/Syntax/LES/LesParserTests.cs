using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Loyc.Collections;

namespace Loyc.Syntax.Les
{
	using S = CodeSymbols;
	using Loyc.Utilities;
	using System.Diagnostics;

	[TestFixture]
	public class LesParserTests : Assert
	{
		static LNodeFactory F = new LNodeFactory(EmptySourceFile.Unknown);
		
		LNode a = F.Id("a"), b = F.Id("b"), c = F.Id("c"), x = F.Id("x");
		LNode Foo = F.Id("Foo"), IFoo = F.Id("IFoo"), T = F.Id("T");
		LNode zero = F.Literal(0), one = F.Literal(1), two = F.Literal(2);
		LNode _(string name) { return F.Id(name); }
		LNode _(Symbol name) { return F.Id(name); }

		[Test]
		public void SimpleCalls()
		{
			Expr("x", x);
			Expr("x()", F.Call(x));
			Expr(@"x(1, ""Hello"", '!', 1.0)", F.Call(x, one, F.Literal("Hello"), F.Literal('!'), F.Literal(1.0)));
			Expr(@"x(@true, @false, @null)",   F.Call(x, F.Literal(true), F.Literal(false), F.Literal(null)));
			Expr("Foo(a, b, c)", F.Call(Foo, a, b, c));
			Expr("Foo(a(b, c), b(c))", F.Call(Foo, F.Call(a, b, c), F.Call(b, c)));
		}

		[Test]
		public void NegativeLiteral()
		{
			Expr("-x", F.Call(S.Sub, x));
			Expr("-2u", F.Call(S.Sub, F.Literal(2u)));
			Expr("-2uL", F.Call(S.Sub, F.Literal(2uL)));
			Expr("- 2", F.Call(S.Sub, two));
			Expr("-2", F.Literal(-2));
			Expr("-111222333444", F.Literal(-111222333444));
			Expr("-2L", F.Literal(-2L));
			Expr("-2.0", F.Literal(-2.0));
			Expr("-2.0f", F.Literal(-2.0f));
		}

		[Test]
		public void BinaryOps()
		{
			Expr("x + 1",        F.Call(S.Add, x, one));
			Expr("x * 2 + 1",    F.Call(S.Add, F.Call(S.Mul, x, two), one));
			Expr("a >= b..c",    F.Call(S.GE, a, F.Call(S.DotDot, b, c)));
			Expr("a == b && c != 0", F.Call(S.And, F.Call(S.Eq, a, b), F.Call(S.Neq, c, zero)));
			Expr("(a ? b : c)",  F.InParens(F.Call(S.QuestionMark, a, F.Call(S.Colon, b, c))));
			Expr("a ?? b <= c",  F.Call(S.LE, F.Call(S.NullCoalesce, a, b), c));
			Expr("a >> b + 1",   F.Call(S.Add, F.Call(S.Shr, a, b), one));
			Expr("a - b / c**2", F.Call(S.Sub, a, F.Call(S.Div, b, F.Call(S.Exp, c, two))));
			Expr("a >>= 1",      F.Call(S.ShrSet, a, one));
			Expr("a.b?.c(x)",    F.Call(S.NullDot, F.Dot(a, b), F.Call(c, x)));
			
			// Custom ops
			Expr("a |-| b+c",     F.Call("#|-|", a, F.Call(S.Add, b, c)));
			Expr("a.b!!!c .?. 1", F.Call("#.?.", F.Call("#!!!", F.Dot(a, b), c), one));
			Expr("a /+ b+*c",     F.Call("#/+", a, F.Call("#+*", b, c)));
			Expr(@"a \Foo b",     F.Call("Foo", a, b));
		}

		[Test]
		public void Tuples()
		{
			Expr("(a)", a);
			Expr("(a,)", F.Tuple(a));
			Expr("(a, @``)", F.Tuple(a, _("")));
			Expr("(a, b)", F.Tuple(a, b));
			Expr("(a, b, c + x)", F.Tuple(a, b, F.Call(S.Add, c, x)));
		}

		[Test]
		public void PrefixOps()
		{
			Stmt("/x;", F.Call(S.Div, x));
			Stmt("-a * b;", F.Call(S.Mul, F.Call(S._Negate, a), b));
			Stmt("-x ** +x / ~x + &x & *x && !x = ^x;",
				F.Call(S.Set, F.Call(S.And, F.Call(S.AndBits, F.Call(S.Add, F.Call(S.Div, F.Call(S.Exp,
					F.Call(S._Negate, x), F.Call(S._UnaryPlus, x)), F.Call(S.NotBits, x)),
					F.Call(S._AddressOf, x)), F.Call(S._Dereference, x)), F.Call(S.Not, x)), F.Call(S.XorBits, x)));
			Stmt("| a = %b;", F.Call(S.OrBits, F.Call(S.Set, a, F.Call(S.Mod, b))));
			Stmt(".. a & b && c;", F.Call(S.And, F.Call(S.DotDot, F.Call(S.AndBits, a, b)), c));
		}

		[Test]
		public void SuffixOps()
		{
			Stmt("a++ + ++a;", F.Call(S.Add, F.Call(S.PostInc, a), F.Call(S.PreInc, a)));
			Stmt(@"a ** b \foo\;", F.Call(@"foo\", F.Call(S.Exp, a, b)));
			Stmt(@"a + b \foo\;", F.Call(S.Add, a, F.Call(@"foo\", b)));
		}

		[Test]
		public void NamedOps()
		{
			Stmt(@"a `x` b `Foo` c", F.Call(Foo, F.Call(x, a, b), c));
			Stmt(@"a \x b \Foo c", F.Call(Foo, F.Call(x, a, b), c));
			Stmt(@"(a `is` b) \is bool", F.Call(_("is"), F.InParens(F.Call(_("is"), a, b)), _("bool")));
			Stmt(@"a `=` b \&& c", F.Call(_("&&"), F.Call(_("="), a, b), c));
			Stmt(@"a > b \and b > c", F.Call(_("and"), F.Call(S.GT, a, b), F.Call(S.GT, b, c)));
		}

		[Test]
		public void Stmts()
		{
			Stmt(0, "a; b; c;", a, b, c);
			Stmt("a.b(c);", F.Call(F.Dot(a, b), c));
			Expr("{ b(c); } + { ; Foo() }", F.Call(S.Add, F.Braces(F.Call(b, c)), F.Braces(F._Missing, F.Call(Foo))));
			Stmt("a.{b;c;}();", F.Call(F.Dot(a, F.Braces(b, c))));
		}

		[Test]
		public void SuperExprs()
		{
			Expr("a b c", F.Call(a, b, c));
			Expr("a (b c)", F.Call(a, F.InParens(F.Call(b, c))));
			Stmt("if a > b { c(); };",   F.Call("if", F.Call(S.GT, a, b), F.Braces(F.Call(c))));
			Stmt("if (a) > b { c(); };", F.Call("if", F.Call(S.GT, F.InParens(a), b), F.Braces(F.Call(c))));
			Stmt("if(a) > b { c(); };",  F.Call(S.GT, F.Call("if", a), F.Call(b, F.Braces(F.Call(c)))));
			Expr("a + b c", F.Call(S.Add, a, F.Call(b, c)));
			Expr("a.b c", F.Call(F.Dot(a, b), c));
			Expr("a + b.c {} Foo", F.Call(S.Add, a, F.Call(F.Dot(b, c), F.Braces(), Foo)));
			Expr("a(b) c", F.Call(a, b, c));
			Expr("a.Foo(b) c", F.Call(F.Dot(a, Foo), b, c));
		}

		[Test]
		public void Generics()
		{
			Expr("a!b", F.Of(a, b));
			Expr("a!(b)", F.Of(a, b));
			Expr("a!(b, c)", F.Of(a, b, c));
			Expr("a.b!((x))", F.Of(F.Dot(a, b), F.InParens(x)));
			Expr("a.b!Foo(x)", F.Call(F.Of(F.Dot(a, b), Foo), x));
			Expr("a.b!(Foo.Foo)(x)", F.Call(F.Of(F.Dot(a, b), F.Dot(Foo, Foo)), x));
			Expr("a.b!(Foo(x))", F.Of(F.Dot(a, b), F.Call(Foo, x)));
			// This last one may seem meaningless, but LES does not judge
			Stmt("Foo = a.b!c!x;", F.Call(S.Set, Foo, F.Of(F.Of(F.Dot(a, b), c), x)));
		}

		[Test]
		public void Attributes()
		{
			Expr("[Foo] a();", F.Attr(Foo, F.Call(a)));
			Expr("[Foo] a = b;", F.Attr(Foo, F.Call(S.Set, a, b)));
			Expr("[a, b] Foo();", F.Attr(a, b, F.Call(Foo)));
			Expr("a = [Foo] b + c;", F.Call(S.Set, a, F.Attr(Foo, F.Call(S.Add, b, c))));
		}

		[Test]
		public void Errors()
		{
			Stmt(1, "a, 5 c b; x();", a, F.Call(x));
			Stmt(1, @"a ** b \foo;", F.Call(S.Exp, a, b));
			Stmt(1, "a = ) b c 5", a); // again, interpretation is a bit weird, but ok
		}

		protected virtual void Expr(string str, LNode node, int errorsExpected = 0)
		{
			Stmt(errorsExpected, str, node);
		}
		protected virtual void Stmt(string str, LNode node) { Expr(str, node); }
		protected virtual void Stmt(int errorsExpected, string str, params LNode[] expected)
		{
			var messages = new MessageHolder();
			var results = LesLanguageService.Value.Parse(str, messages).Buffered();
			for (int i = 0; i < expected.Length; i++) {
				var result = results[i]; // this is where parsing actually occurs
				AreEqual(expected[i], result);
			}
			AreEqual(expected.Length, results.Count);
			if (messages.List.Count != errorsExpected) {
				messages.WriteListTo(MessageSink.Console);
				AreEqual(errorsExpected, messages.List.Count); // fail
			}
		}
	}

	/// <summary>Uses the parser tests as the basis for printer tests. We simply 
	/// make sure that after parsing something, we can print it out and re-parse
	/// what was printed to get the same Loyc tree.</summary>
	[TestFixture]
	public class LesPrinterTests : LesParserTests
	{
		protected override void Stmt(int errorsExpected, string str, params LNode[] nodes)
		{
			// Start by parsing. If parsing fails, just stop; such errors are 
			// already reported by LesParserTests so we need not report them here.
			if (errorsExpected != 0)
				return;
			var messages = new MessageHolder();
			var results = LesLanguageService.Value.Parse(str, messages).Buffered();
			if (messages.List.Count != 0)
				return;

			foreach (LNode node in results)
				DoPrinterTest(node);
		}

		MessageHolder messages = new MessageHolder();

		private void DoPrinterTest(LNode node)
		{
			var sb = new StringBuilder();
			messages.List.Clear();
			LesNodePrinter.Printer(node, sb, messages, null, "  ");
			Assert.AreEqual(0, messages.List.Count);
			var reparsed = LesLanguageService.Value.Parse(sb.ToString(), messages).Buffered();
			Assert.AreEqual(0, messages.List.Count);
			Assert.AreEqual(1, reparsed.Count);
			Assert.AreEqual(node, reparsed[0]);
		}
	}
}
