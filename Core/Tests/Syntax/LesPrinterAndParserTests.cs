using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.MiniTest;
using Loyc.Collections;
using Loyc.Utilities;

namespace Loyc.Syntax.Les
{
	using S = CodeSymbols;

	// Tests shared between the printer and the parser. Both tests together verify 
	// round-tripping from AST -> text -> AST, although the other kind of round-
	// tripping, text -> AST -> text, is not fully verified (and is not designed to
	// be fully supported, as the printer is not designed to preserve spacing.)
	[TestFixture]
	public abstract class LesPrinterAndParserTests : Assert
	{
		protected static LNodeFactory F = new LNodeFactory(EmptySourceFile.Unknown);
		
		protected LNode a = F.Id("a"), b = F.Id("b"), c = F.Id("c"), x = F.Id("x");
		protected LNode Foo = F.Id("Foo"), IFoo = F.Id("IFoo"), T = F.Id("T");
		protected LNode zero = F.Literal(0), one = F.Literal(1), two = F.Literal(2);
		protected LNode _(string name) { return F.Id(name); }
		protected LNode _(Symbol name) { return F.Id(name); }

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
			Exact("-x;", F.Call(S.Sub, x));
			Stmt ("-2u;", F.Call(S.Sub, F.Literal(2u)));
			Stmt ("-2uL;", F.Call(S.Sub, F.Literal(2uL)));
			Exact("- 2;", F.Call(S.Sub, two));
			Exact("-2;", F.Literal(-2));
			Stmt ("-111222333444;", F.Literal(-111222333444));
			Exact("-2L;", F.Literal(-2L));
			Stmt ("-2.0;", F.Literal(-2.0));
			Stmt ("-2d;",  F.Literal(-2.0));
			Exact("-2f;",   F.Literal(-2.0f));
			Stmt ("-2.0f;", F.Literal(-2.0f));
		}

		[Test]
		public void BinaryOps()
		{
			Exact("x + 1;",        F.Call(S.Add, x, one));
			Exact("a + b + 1;",    F.Call(S.Add, F.Call(S.Add, a, b), one));
			Exact("x * 2 + 1;",    F.Call(S.Add, F.Call(S.Mul, x, two), one));
			Exact("a >= b .. c;",  F.Call(S.GE, a, F.Call(S.DotDot, b, c)));
			Exact("a == b && c != 0;", F.Call(S.And, F.Call(S.Eq, a, b), F.Call(S.Neq, c, zero)));
			Exact("(a ? b : c);",  F.InParens(F.Call(S.QuestionMark, a, F.Call(S.Colon, b, c))));
			Exact("a ?? b <= c;",  F.Call(S.LE, F.Call(S.NullCoalesce, a, b), c));
			Exact("a - b / c**2;", F.Call(S.Sub, a, F.Call(S.Div, b, F.Call(S.Exp, c, two))));
			Exact("a >>= 1;",      F.Call(S.ShrSet, a, one));
			Exact("a.b?.c(x);",    F.Call(S.NullDot, F.Dot(a, b), F.Call(c, x)));
			
			// Custom ops
			Exact("a |-| b + c;",     F.Call("|-|", a, F.Call(S.Add, b, c)));
			Exact("a.b!!!c .?. 1;", F.Call(".?.", F.Call("!!!", F.Dot(a, b), c), one));
			Exact("a /+ b +* c;",     F.Call("/+", a, F.Call("+*", b, c)));
			//Expr(@"a \Foo b",     F.Call("Foo", a, b));
		}

		[Test]
		public void Tuples()
		{
			Stmt("(a);", F.InParens(a));
			Stmt("(a;);", F.Tuple(a));
			Stmt("(a; @``;);", F.Tuple(a, _("")));
			Expr("(a;)", F.Tuple(a));
			Stmt("(a; b);", F.Tuple(a, b));
			Stmt("(a; b; c + x);", F.Tuple(a, b, F.Call(S.Add, c, x)));
		}

		[Test]
		public void PrefixOps()
		{
			Stmt("/x;", F.Call(S.Div, x));
			Stmt("-a * b;", F.Call(S.Mul, F.Call(S._Negate, a), b));
			Stmt("-x ** +x / ~x + &x & *x && !x = ^x;",
				F.Call(S.Assign, F.Call(S.And, F.Call(S.AndBits, F.Call(S.Add, F.Call(S.Div, F.Call(S.Exp,
					F.Call(S._Negate, x), F.Call(S._UnaryPlus, x)), F.Call(S.NotBits, x)),
					F.Call(S._AddressOf, x)), F.Call(S._Dereference, x)), F.Call(S.Not, x)), F.Call(S.XorBits, x)));
			Stmt("| a = %b;", F.Call(S.OrBits, F.Call(S.Assign, a, F.Call(S.Mod, b))));
			Stmt(".. a + b && c;", F.Call(S.And, F.Call(S.DotDot, F.Call(S.Add, a, b)), c));
		}

		[Test]
		public void SuffixOps()
		{
			Stmt("a++ + ++a;", F.Call(S.Add, F.Call(S.PostInc, a), F.Call(S.PreInc, a)));
			Stmt(@"a.b --;", F.Call(@"suf--", F.Call(S.Dot, a, b)));
			Stmt(@"a + b -<>-;", F.Call(S.Add, a, F.Call(@"suf-<>-", b)));
		}

		[Test]
		public void NamedOps()
		{
			Stmt(@"a `x` b `Foo` c", F.Call(Foo, F.Call(x, a, b), c));
			//Stmt(@"a \x b \Foo c", F.Call(Foo, F.Call(x, a, b), c));
			Stmt(@"(a `is` b) `is` bool", F.Call(_("is"), F.InParens(F.Call(_("is"), a, b)), _("bool")));
			Stmt(@"a `=` b && c", F.Call(_("&&"), F.Call(_("="), a, b), c));
			// Currently \* is equivalent to plain * (the backslash just indicates that the operator may contain letters)
			//Stmt(@"a > b \and b > c", F.Call(_("and"), F.Call(S.GT, a, b), F.Call(S.GT, b, c)));
		}

		protected static LNode AsOperator(LNode node) { return AsStyle(NodeStyle.Operator, node); }
		protected static LNode AsStyle(NodeStyle s, LNode node)
		{
			node.BaseStyle = s;
			return node;
		}

		[Test]
		public void Stmts()
		{
			Test(Mode.Stmt, -1, "a; b; c;", a, b, c);
			Stmt("a.b(c);", F.Call(F.Dot(a, b), c));
			Expr("{ b(c); } + { ; Foo() }", F.Call(S.Add, F.Braces(F.Call(b, c)), F.Braces(F._Missing, F.Call(Foo))));
			Stmt("a.{b;c;}();", F.Call(F.Dot(a, F.Braces(b, c))));
		}

		[Test]
		public void SuperExprs()
		{
			Expr("a b c", F.Call(a, b, c));
			Expr("a (b c)", F.Call(a, F.InParens(F.Call(b, c))));
			Stmt("if a > b { c(); };", F.Call("if", F.Call(S.GT, a, b), F.Braces(F.Call(c))));
			Stmt("if (a > b) { c(); };", F.Call("if", F.InParens(F.Call(S.GT, a, b)), F.Braces(F.Call(c))));
			Expr("a + (b c)", F.Call(S.Add, a, F.InParens(F.Call(b, c))));
			Expr("a b + (if c {a;} else {b;})", F.Call(a, F.Call(S.Add, b, F.InParens(F.Call(_("if"), c, F.Braces(a), _("else"), F.Braces(b))))));
			Stmt("get { x } = 0;", F.Call(S.get, F.Call(S.Assign, F.Braces(x), zero)));
		}

		[Test]
		public void Generics()
		{
			Expr("a!b", F.Of(a, b));
			Expr("a!(b)", F.Of(a, b));
			Expr("a!(b, c)", F.Of(a, b, c));
			Expr("a!()", F.Of(a));
			Expr("a.b!((x))", F.Of(F.Dot(a, b), F.InParens(x)));
			Expr("a.b!Foo(x)", F.Call(F.Of(F.Dot(a, b), Foo), x));
			Expr("a.b!(Foo.Foo)(x)", F.Call(F.Of(F.Dot(a, b), F.Dot(Foo, Foo)), x));
			Expr("a.b!(Foo(x))", F.Of(F.Dot(a, b), F.Call(Foo, x)));
			// This last one is meaningless in most programming languages, but LES does not judge
			Stmt("Foo = a.b!c!x;", F.Call(S.Assign, Foo, F.Of(F.Of(F.Dot(a, b), c), x)));
		}

		[Test]
		public void Attributes()
		{
			Exact("@[Foo] a();", F.Attr(Foo, F.Call(a)));
			Exact("@[Foo] a = b;", F.Attr(Foo, F.Call(S.Assign, a, b)));
			Exact("@[a, b] Foo();", F.Attr(a, b, F.Call(Foo)));
			Stmt("a = (       b + c);", F.Call(S.Assign, a,  F.InParens(F.Call(S.Add, b, c))));
			Stmt("a = (@[]    b + c);", F.Call(S.Assign, a,             F.Call(S.Add, b, c)));
			Stmt("a = (@[Foo] b + c);", F.Call(S.Assign, a, F.Attr(Foo, F.Call(S.Add, b, c))));
		}

		[Test]
		public void Lists()
		{
			//Exact("[x];", F.Call(S.Array, x));
			Exact("++[x];", F.Call(S.PreInc, F.Call(S.Array, x)));
			Exact("Foo = [a, b, c];", F.Call(S.Assign, Foo, F.Call(S.Array, a, b, c)));
			Exact("Foo = [a, b, c] + [x];", F.Call(S.Assign, Foo, F.Call(S.Add, F.Call(S.Array, a, b, c), F.Call(S.Array, x))));
		}

		protected virtual void Expr(string text, LNode expr, int errorsExpected = 0)
		{
			Test(Mode.Expr, errorsExpected, text, new[] { expr });
		}
		protected virtual void Stmt(string text, LNode code, int errorsExpected = 0)
		{
			Test(Mode.Stmt, errorsExpected, text, new[] { code });
		}
		protected virtual void Exact(string text, LNode code, int errorsExpected = 0)
		{
			Test(Mode.Exact, errorsExpected, text, new[] { code });
		}

		/// <summary>Runs a printer or parser test.</summary>
		/// <param name="parseErrors">-1 if the printer and parser should both 
		/// test this example. If above -1, only the parser will run this example,
		/// and this parameter specifies the number of parse errors to expect 
		/// (may be 0).</param>
		protected abstract MessageHolder Test(Mode mode, int parseErrors, string text, params LNode[] code);
		protected enum Mode
		{
			Expr = 0,  // Parse expression list
			Stmt = 1,  // Parse statement list
			Exact = 3, // Parse statements, and expect exact (rather than equivalent) printer output
		}

		protected void ExpectMessageContains(MessageHolder messages, params string[] substrings)
		{
			foreach (var msg in messages.List)
				for (int i = 0; i < substrings.Length; i++)
					if (msg.Formatted.IndexOf(substrings[i], StringComparison.InvariantCultureIgnoreCase) > -1)
						substrings[i] = null;
			Assert.AreEqual(null, substrings.WhereNotNull().FirstOrDefault());
		}
	}
}
