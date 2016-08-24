using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using Loyc.Collections;
using Loyc.MiniTest;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.Syntax.Les
{
	public abstract class Les3PrinterAndParserTests : Assert
	{
		protected static LNodeFactory F = new LNodeFactory(EmptySourceFile.Unknown);
		
		protected LNode a = F.Id("a"), b = F.Id("b"), c = F.Id("c"), x = F.Id("x");
		protected LNode Foo = F.Id("Foo"), IFoo = F.Id("IFoo"), T = F.Id("T");
		protected LNode zero = F.Literal(0), one = F.Literal(1), two = F.Literal(2);
		protected LNode _(string name) { return F.Id(name); }
		protected LNode _(Symbol name) { return F.Id(name); }

		protected static LNode AsOperator(LNode node) { return AsStyle(NodeStyle.Operator, node); }
		protected static LNode AsStyle(NodeStyle s, LNode node)
		{
			node.BaseStyle = s;
			return node;
		}

		#region Literals

		[Test]
		public void NumericLiterals()
		{
			Exact(@"123", F.Literal(123));
			Exact(@"123Z", F.Literal(new BigInteger(123)));
			Exact(@"(123)", F.InParens(F.Literal(123)));
			Exact(@"123uL", F.Literal(123uL));
			Exact(@"123.25", F.Literal(123.25));
			Exact(@"123.25f", F.Literal(123.25f));
		}

		[Test]
		public void StringLiterals()
		{
			Exact(@"'!'", F.Literal('!'));
			Exact(@"""!""", F.Literal("!"));
			Exact(@"'''!'''", F.Literal("!").SetBaseStyle(NodeStyle.TQStringLiteral));
			Exact(@"""""""!""""""", F.Literal("!").SetBaseStyle(NodeStyle.TDQStringLiteral));
		}

		[Test]
		public void SpecialLiterals()
		{
			Exact(@"special""!""", F.Literal(new SpecialLiteral("!", (Symbol)"special")));
			Exact(@"special'''!'''", F.Literal(new SpecialLiteral("!", (Symbol)"special")).SetBaseStyle(NodeStyle.TQStringLiteral));
			Exact(@"0x123special", F.Literal(new SpecialLiteral(0x123, (Symbol)"special")).SetBaseStyle(NodeStyle.HexLiteral));
		}

		[Test]
		public void NegativeLiteral()
		{
			Exact("-x;", F.Call(S.Sub, x));
			Stmt("-2u;", F.Call(S.Sub, F.Literal(2u)));
			Stmt("-2uL;", F.Call(S.Sub, F.Literal(2uL)));
			Exact("- 2;", F.Call(S.Sub, two));
			Exact("-2;", F.Literal(-2));
			Exact("-2Z;", F.Literal(new BigInteger(-2)));
			Stmt("-111222333444;", F.Literal(-111222333444));
			Exact("-2L;", F.Literal(-2L));
			Stmt("-2.0;", F.Literal(-2.0));
			Stmt("-2d;", F.Literal(-2.0));
			Exact("-2f;", F.Literal(-2.0f));
			Stmt("-2.0f;", F.Literal(-2.0f));
		}
		
		[Test]
		public void NamedFloatLiteral()
		{
			Exact("@@-inf_f;", F.Literal(float.NegativeInfinity));
			Exact("@@inf_f;", F.Literal(float.PositiveInfinity));
			Exact("@@nan_f;", F.Literal(float.NaN));
			Exact("@@-inf_d;", F.Literal(double.NegativeInfinity));
			Exact("@@inf_d;", F.Literal(double.PositiveInfinity));
			Exact("@@nan_d;", F.Literal(double.NaN));

			// Ensure identifiers with the same name don't count as literals
			Exact("inf_f(nan_f);", F.Call(_("inf_f"), F.Id("nan_f")));
			Exact("inf_d(nan_d);", F.Call(_("inf_d"), F.Id("nan_d")));
		}


		#endregion

		#region Basic expressions: calls, unary & binary operators

		[Test]
		public void SimpleCalls()
		{
			Exact("x", x);
			Exact("x()", F.Call(x));
			Exact("Foo(a, b, c)", F.Call(Foo, a, b, c));
			Exact("Foo(a(b, c), b(c))", F.Call(Foo, F.Call(a, b, c), F.Call(b, c)));
		}

		[Test]
		public void LiteralKeywords()
		{
			Exact(@"x(`true`,`false`,`null`)", F.Call(x, F.Id("true"), F.Id("false"), F.Id("null")));
			Expr(@"x( true,  false,  null)", F.Call(x, F.Literal(true), F.Literal(false), F.Literal(null)));
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
			Exact("a >>= 1;",      F.Call(S.ShrAssign, a, one));
			Exact("a.b?.c(x);",    F.Call(S.NullDot, F.Dot(a, b), F.Call(c, x)));
			
			// Custom ops
			Exact("a |-| b + c;",   F.Call("'|-|", a, F.Call(S.Add, b, c)));
			Exact("a.b!!!c .?. 1;", F.Call("'.?.", F.Call("'!!!", F.Dot(a, b), c), one));
			Exact("a +/ b *+ c;",   F.Call("'+/", a, F.Call("'*+", b, c)));
		}

		[Test]
		public void SingleQuotedBinaryOps()
		{
			Exact("x '* 2 '+ 1;", F.Call(S.Add, F.Call(S.Mul, x, two), one));
			Exact("a '*s b '>u c;", F.Call("'>u", F.Call("'*s", a, b), c));
			Exact("a '>= 0 '&& b '> 1;", F.Call(S.And, F.Call(S.GE, a, zero), F.Call(S.GT, b, one)));
		}

		[Test]
		public void SingleQuotedNamedOps()
		{
			Stmt(@"a 'x b 'y c", F.Call(_("'y"), F.Call(_("'x"), a, b), c));
			Stmt(@"a 'X b 'Y c", F.Call(_("'Y"), F.Call(_("'X"), a, b), c));
			Stmt(@"a 'implies b 'Likes c", F.Call(_("'implies"), a, F.Call(_("'Likes"), b, c)));
			Stmt(@"a 'implies b == c", F.Call(_("'implies"), a, F.Call(S.Eq, b, c)));
			Stmt(@"a 'Likes b && b 'Likes c", F.Call(S.And, F.Call(_("'Likes"), a, b), F.Call(_("'Likes"), b, c)));
		}

		[Test]
		public void PrefixOps()
		{
			Stmt("-a * b;", F.Call(S.Mul, F.Call(S._Negate, a), b));
			Stmt("-x ** +x / ~x + &x & *x && !x == ^x;",
				F.Call(S.And, F.Call(S.AndBits, F.Call(S.Add, F.Call(S.Div, F.Call(S.Exp,
					F.Call(S._Negate, x), F.Call(S._UnaryPlus, x)), F.Call(S.NotBits, x)),
					F.Call(S._AddressOf, x)), F.Call(S._Dereference, x)),
					F.Call(S.Eq, F.Call(S.Not, x), F.Call(S.XorBits, x))));
			Stmt("| a = %b;", F.Call(S.OrBits, F.Call(S.Assign, a, F.Call(S.Mod, b))));
			Stmt(".. a + b && c;", F.Call(S.And, F.Call(S.DotDot, F.Call(S.Add, a, b)), c));
		}

		[Test]
		public void SuffixOps()
		{
			Stmt("a++ + ++a;", F.Call(S.Add, F.Call(S.PostInc, a), F.Call(S.PreInc, a)));
			Stmt(@"a.b --;", F.Call(@"'--suf", F.Call(S.Dot, a, b)));
			Stmt(@"a + b -<>-;", F.Call(S.Add, a, F.Call(@"'-<>-suf", b)));
		}

		[Test]
		public void SubtractNegativeLiteral()
		{
			Expr("a-b", F.Call(S.Sub, a, b));
			// TEMP: this should be a subtraction instead
			Expr("x-2", F.Call(S.Add, x, F.Literal(-2)));
		}

		#endregion

		#region Braces, tuples, lists

		[Test]
		public void Tuples()
		{
			Stmt("(a);", F.InParens(a));
			Stmt("(a;);", F.Tuple(a));
			Stmt("(;);", F.Tuple(_("")));
			Exact("(``;);", F.Tuple(_("")));
			Stmt("(a; ;);", F.Tuple(a, _("")));
			Exact("(a; ``;);", F.Tuple(a, _("")));
			Stmt("(a; b);", F.Tuple(a, b));
			Stmt("(a; b; c + x);", F.Tuple(a, b, F.Call(S.Add, c, x)));
		}

		[Test]
		public void Stmts()
		{
			Test(Mode.Stmt, -1, "a; b; c;", a, b, c);
			Stmt("a.b(c);", F.Call(F.Dot(a, b), c));
			Expr("{ b(c); } + { ; Foo() }", F.Call(S.Add, F.Braces(F.Call(b, c)), F.Braces(F.Missing, F.Call(Foo))));
			Stmt("a.{b;c;}();", F.Call(F.Dot(a, F.Braces(b, c))));
			Stmt(@"{ ""key"": ""value"", {""KEY"": ""VALUE""} }", F.Braces(
				F.Call(S.Colon, F.Literal("key"), F.Literal("value")), F.Braces(
				F.Call(S.Colon, F.Literal("KEY"), F.Literal("VALUE")))));
		}

		[Test]
		public void ListLiterals()
		{
			Exact(@"x = [1, 2, ""three""];", F.Call(S.Assign, x, 
				F.Call(S.Array, one, two, F.Literal("three"))));
			Stmt(@"{ ""a"" : [null], ""b"" : [] }", F.Braces(
				F.Call(S.Colon, F.Literal("a"), F.Call(S.Array, F.Null)), 
				F.Call(S.Colon, F.Literal("b"), F.Call(S.Array))));
			Exact("++[x];", F.Call(S.PreInc, F.Call(S.Array, x)));
			Exact("Foo = [a, b, c];", F.Call(S.Assign, Foo, F.Call(S.Array, a, b, c)));
			Exact("Foo = [a, b, c] + [x];", F.Call(S.Assign, Foo, F.Call(S.Add, F.Call(S.Array, a, b, c), F.Call(S.Array, x))));
		}

		#endregion
		
		#region Generics, indexers

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
		public void IndexBracks()
		{
			Exact("a[];", F.Call(S.IndexBracks, a));
			Exact("a[b];", F.Call(S.IndexBracks, a, b));
			Exact("a[b, c];", F.Call(S.IndexBracks, a, b, c));
		}

		#endregion

		#region Block expressions, juxtaposition, and keyword statements

		[Test]
		public void BlockExpressions()
		{
			Exact("a (b) { c; };", F.Call(a, b, F.Braces(c)));
			Exact("a (b, c) {};", F.Call(a, b, c, F.Braces()));
			Exact("Foo = a (b) { c; };", F.Call(S.Assign, Foo, F.Call(a, b, F.Braces(c))));
			Exact("Foo = a (b) { c; } + x;", F.Call(S.Assign, Foo, F.Call(S.Add, F.Call(a, b, F.Braces(c)), x)));
			Exact("Foo = if (c) { a; } else { b; };", F.Call(S.Assign, Foo, F.Call(_("if"), c, F.Braces(a), F.Call(_("'else"), F.Braces(b)))));
			Exact("Foo = quote { a; };", F.Call(S.Assign, Foo, F.Call(_("quote"), F.Braces(a))));
			Exact("Foo = do { a; } where (b);", F.Call(S.Assign, Foo, F.Call(_("do"), F.Braces(a), F.Call(_("'where"), b))));
		}

		[Test]
		public void BlockExpressionsWithoutOptionalSemicolon()
		{
			Test(Mode.Stmt, 0, "a (b) { c; }\n a { c; }\n a();", F.Call(a, b, F.Braces(c)), 
			                                                     F.Call(a, F.Braces(c)), F.Call(a));
		}

		[Test]
		public void KeywordStatements()
		{
			Stmt("#return x + 1;", F.Call(S.Return, F.Call(S.Add, x, one)));
			Stmt("#if Foo { a; }", F.Call(S.If, Foo, F.Braces(a)));
			Stmt("#if Foo { a; } else { b; }", F.Call(S.If, Foo, F.Braces(a), F.Call("'else", F.Braces(b))));
			Stmt("#class Foo!T 'where T:IFoo { a; }", F.Call(S.Class, 
				F.Call("'where", F.Of(Foo, T), F.Call(S.Colon, T, F.Id("IFoo"))), F.Braces(a)));
		}

		[Test]
		public void BasicJuxtaposition()
		{
			Expr("not x", F.Call("not", x));
			Expr("not a.b", F.Call("not", F.Dot(a, b)));
			Expr("not $x", F.Call("not", F.Call(S.Substitute, x)));
			Expr("neg 1234", F.Call("neg", F.Literal(1234)));
			Expr("sin sqrt x", F.Call("sin", F.Call("sqrt", x)));
			Expr("`i32.eqz` x", F.Call("i32.eqz", x));
		}

		[Test]
		public void JuxtapositionDisambiguation()
		{
			// Only the first of these counts as a juxtaposition
			Expr("not a.b.c", F.Call("not", F.Dot(a, b, c)));
			Expr("not (a).b.c", F.Dot(F.Call("not", a), b, c));
			Expr("not [a].b.c", F.Dot(F.Call(S.IndexBracks, F.Id("not"), a), b, c));
			Expr("not {a}.b.c", F.Dot(F.Call("not", F.Braces(a)), b, c));
			Expr("not -x", F.Call(S.Sub, F.Id("not"), x));
		}

		#endregion

		[Test]
		public void PrefixAttributes()
		{
			Exact("@Foo a = b(c);",     F.Attr(Foo, F.Call(S.Assign, a, F.Call(b, c))));
			Exact("@Foo (a)(b);",       F.Attr(Foo, F.Call(F.InParens(a), b)));
			Stmt("@(Foo) a = b(c);",    F.Attr(Foo, F.Call(S.Assign, a, F.Call(b, c))));
			Exact("@((Foo)) b(c);",     F.Attr(F.InParens(Foo), F.Call(b, c)));
			Stmt("@(Foo(x)) a = b(c);", F.Attr(F.Call(Foo, x), F.Call(S.Assign, a, F.Call(b, c))));
			Exact("@(@0 Foo(x)) b(c);", F.Attr(F.Attr(zero, F.Call(Foo, x)), F.Call(b, c)));
			Exact("@(a == c) b(c);",    F.Attr(F.Call(S.Eq, a, c), F.Call(b, c)));
			Exact("(@Foo a) = b(c);",   F.Call(S.Assign, F.Attr(Foo, a), F.Call(b, c)));
			Exact("((@Foo a)) = b(c);", F.Call(S.Assign, F.InParens(F.Attr(Foo, a)), F.Call(b, c)));
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

	[TestFixture]
	public class Les3ParserTests : Les3PrinterAndParserTests
	{
		[Test]
		public void PrefixOpParseErrors()
		{
			Test(Mode.Stmt, 1, "?x;", F.Call(S.QuestionMark, x));
			Test(Mode.Stmt, 1, "/x;", F.Call(S.Div, x));
			Test(Mode.Stmt, 1, "=x;", F.Call(S.Assign, x));
			Test(Mode.Stmt, 1, ">x;", F.Call(S.GT, x));
			Test(Mode.Stmt, 1, "1 + <x;", F.Call(S.Add, one, F.Call(S.LT, x)));
			Test(Mode.Stmt, 1, "'sqrt x;", F.Call("'sqrt", x));
		}

		protected override MessageHolder Test(Mode mode, int errorsExpected, string text, params LNode[] expected)
		{
			var messages = new MessageHolder();
			var results = Les3LanguageService.Value.Parse(text, messages, mode == Mode.Expr ? ParsingMode.Expressions : ParsingMode.Statements).ToList();
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
