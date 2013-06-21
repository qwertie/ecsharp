using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Loyc.Syntax.Les
{
	using S = CodeSymbols;

	[TestFixture]
	public class LesParserTests
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
		public void BinaryOps()
		{
			Expr("x + 1",        F.Call(S.Add, x, one));
			Expr("x * 2 + 1",    F.Call(S.Add, F.Call(S.Mul, x, two), one));
			Expr("a >= b..c",    F.Call(S.GE, a, F.Call(S.DotDot, b, c)));
			Expr("a == b && c != 0", F.Call(S.And, F.Call(S.Eq, a, b), F.Call(S.Neq, c, zero)));
			Expr("(a ? b : c)",  F.InParens(F.Call(S.QuestionMark, a, F.Call(S.Colon, b, c))));
			Expr("a ?? b <= c",  F.Call(S.LE, F.Call(S.NullCoalesce, a, b), c));
			Expr("a >> b + 1",   F.Call(S.Add, F.Call(S.Shr, a, b), one));
			Expr("a - b / c**2", F.Call(S.Sub, F.Call(S.Div, b, F.Call(S.Exp, c, two))));
			Expr("a >>= 1",      F.Call(S.ShrSet, a, one));
			Expr("a.b?.c(x)",    F.Call(S.NullDot, F.Dot(a, b), F.Call(c, x)));
			
			// Custom ops
			Expr("a |-| b+c",     F.Call("#|-|", a, F.Call(S.Add, b, c)));
			Expr("a.b!!!c .?. 1", F.Call("#.?.", F.Call("#!!!", F.Dot(a, b), c), one));
			Expr("a /+ b+*c",     F.Call("#/+", a, F.Call("#+*", b, c)));
			Expr(@"a \Foo b",     F.Call("Foo", a, b));
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
			Stmt(".. a & b && c;", F.Call(S.And, F.Call(S.DotDot, F.Call(S.AndBits, a, b))));
		}

		[Test]
		public void SuffixOps()
		{
			Stmt("a++ + ++a;", F.Call(S.Add, F.Call(S.PostInc, a), F.Call(S.PreInc, a)));
			Stmt("a ** b \\foo;", F.Call(@"\foo", F.Call(S.Exp, a, b)));
			Stmt("a + b \\foo;", F.Call(S.Add, a, F.Call(@"\foo", b)));
		}

		[Test]
		public void Stmts()
		{
			
		}

		[Test]
		public void SuperExprs()
		{
			
		}


		private void Stmt(string str, LNode node) { Expr(str, node); }
		private void Expr(string p, LNode lNode)
		{
			throw new NotImplementedException();
		}
	}
}
