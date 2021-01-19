using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Numerics;
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
	public abstract class Les2PrinterAndParserTests : Assert
	{
		protected static LNodeFactory F = new LNodeFactory(EmptySourceFile.Unknown);
		
		protected LNode a = F.Id("a"), b = F.Id("b"), c = F.Id("c"), x = F.Id("x");
		protected LNode Foo = F.Id("Foo"), IFoo = F.Id("IFoo"), T = F.Id("T");
		protected LNode zero = F.Literal(0, "_"), one = F.Literal(1, "_"), two = F.Literal(2, "_");
		protected LNode _(string name) { return F.Id(name); }
		protected LNode _(Symbol name) { return F.Id(name); }
		protected LNode Number(object value) { return F.Literal(value, "_"); }
		protected LNode String(object value) { return F.Literal(value, (string)null); }

		protected static LNode Op(LNode node) { return node.SetBaseStyle(NodeStyle.Operator); }
		protected LNode OnNewLine(LNode node) => node.PlusAttrBefore(F.TriviaNewline);
		protected LNode NewlineAfter(LNode node) => node.PlusTrailingTrivia(F.TriviaNewline);
		protected LNode AppendStmt(LNode stmt) => stmt.PlusAttrBefore(F.Id(S.TriviaAppendStatement));
		public LNode BracesOnOneLine(params LNode[] contents) => F.Braces(contents.Select(n => AppendStmt(n)));

		[Test]
		public void SimpleCalls()
		{
			Expr("x", x);
			Expr("x()", F.Call(x));
			Expr(@"x(1, ""Hello"")", F.Call(x, one, String("Hello")));
			Expr(@"x('!', 1.0)", F.Call(x, F.Literal('!', "c"), Number(1.0)));
			Expr(@"x(@true, @false, @null)",   F.Call(x, F.Literal(true), F.Literal(false), F.Null));
			Expr("Foo(a, b, c)", F.Call(Foo, a, b, c));
			Expr("Foo(a(b, c), b(c))", F.Call(Foo, F.Call(a, b, c), F.Call(b, c)));
		}

		[Test]
		public void NegativeLiteral()
		{
			Exact("-x;", F.Call(S.Sub, x));
			Stmt ("−2u;", F.Literal((UString)"−2", "_u"), 1);
			Stmt ("−2uL;", F.Literal((UString)"−2", "_uL"), 1);
			Exact("-2;", F.Call(S.Sub, two));
			Stmt ("−3;", Number(-3));
			Exact(@"_""-4"";", Number(-4));
			Exact("-5;", F.Call(S.Sub, Number(5)));
			Exact("_z\"-6\";", F.Literal(new BigInteger(-6), "_z"));
			Stmt ("−7z;", F.Literal(new BigInteger(-7), "_z"));
			Stmt ("−111222333444z;", F.Literal(new BigInteger(-111222333444), "_z"));
			Stmt ("−111222333445;", Number(-111222333445));
			Stmt ("−2L;", F.Literal(-2L, "_L"));
			Stmt ("−2.0;", F.Literal(-2.0, "_"));
			Stmt ("−2d;",  F.Literal(-2.0, "_d"));
			Stmt ("−2f;",   F.Literal(-2.0f, "_f"));
			Stmt ("−2.0f;", F.Literal(-2.0f, "_f"));
		}

		[Test]
		public void NamedFloatLiteral()
		{
			// Test if named float literals are implemented.
			Exact("@-inf.f;", F.Literal(float.NegativeInfinity));
			Exact("@inf.f;", F.Literal(float.PositiveInfinity));
			Exact("@nan.f;", F.Literal(float.NaN));
			Exact("@-inf.d;", F.Literal(double.NegativeInfinity));
			Exact("@inf.d;", F.Literal(double.PositiveInfinity));
			Exact("@nan.d;", F.Literal(double.NaN));

			// Test old names
			Stmt("@-inf_f;", F.Literal(float.NegativeInfinity));
			Stmt("@inf_f;", F.Literal(float.PositiveInfinity));
			Stmt("@nan_f;", F.Literal(float.NaN));
			Stmt("@-inf_d;", F.Literal(double.NegativeInfinity));
			Stmt("@inf_d;", F.Literal(double.PositiveInfinity));
			Stmt("@nan_d;", F.Literal(double.NaN));

			// Make sure that round-tripping identifiers that
			// overlap with named float literals is safe.
			Exact("@`-inf.f`;", F.Id("-inf.f"));
			Exact("@`-inf_d`;", F.Id("-inf_d"));

			// Also check that we don't overreact and enclose
			// everything with backquotes.
			Exact("inf_f;", F.Id("inf_f"));
			Exact("nan_f;", F.Id("nan_f"));
			Exact("inf_d;", F.Id("inf_d"));
			Exact("nan_d;", F.Id("nan_d"));
		}

		[Test]
		public void RangeOps()
		{
			Exact("a >= b..c;",     F.Call(S.GE, a, F.Call(S.DotDot, b, c)));
			Exact("a >= b..<c;",    F.Call(S.GE, a, F.Call("'..<", b, c)));
			Exact("a + b...c;",     F.Call(S.Add, a, F.Call(S.DotDotDot, b, c)));
			Exact("@'+(a, b)...c;", F.Call(S.DotDotDot, F.Call(S.Add, a, b), c));
			Exact("a.<b + c;",      F.Call(S.Add, F.Call("'.<", a, b), c));
			Stmt("..a + b == c;",   F.Call(S.Eq, F.Call(S.Add, F.Call(S.DotDot, a), b), c));
			Exact("x >> a..b;",     F.Call(S.Shr, x, F.Call(S.DotDot, a, b)));
			Exact("a.b!!.c.?.1;", F.Call("'.?.", F.Call("'!!.", F.Dot(a, b), c), one));
		}

		[Test]
		public void BinaryOps()
		{
			Exact("x + 1;",        F.Call(S.Add, x, one));
			Exact("a + b + 1;",    F.Call(S.Add, F.Call(S.Add, a, b), one));
			Exact("x * 2 + 1;",    F.Call(S.Add, F.Call(S.Mul, x, two), one));
			Exact("a = b = 0;",    F.Call(S.Assign, a, F.Call(S.Assign, b, zero)));
			Exact("a == b && c != 0;", F.Call(S.And, F.Call(S.Eq, a, b), F.Call(S.NotEq, c, zero)));
			Exact("(a ? b : c);",  F.InParens(F.Call(S.QuestionMark, a, F.Call(S.Colon, b, c))));
			Exact("a ?? b <= c;",  F.Call(S.LE, F.Call(S.NullCoalesce, a, b), c));
			Exact("a - b / c**2;", F.Call(S.Sub, a, F.Call(S.Div, b, F.Call(S.Exp, c, two))));
			Exact("a >>= 1;",      F.Call(S.ShrAssign, a, one));
			Exact("a.b?.c(x);",    F.Call(S.NullDot, F.Dot(a, b), F.Call(c, x)));
			Exact("a.b::x.c;",     F.Call(S.ColonColon, F.Dot(a, b), F.Dot(x, c)));
			Exact(@"a + b + @'+(c, 1);", F.Call(S.Add, F.Call(S.Add, a, b), F.Call(S.Add, c, one)));

			// Custom ops
			Exact("a |-| b + c;",   F.Call("'|-|", a, F.Call(S.Add, b, c)));
			Exact("a +/ b *+ c;",   F.Call("'*+", F.Call("'+/", a, b), c));
		}

		[Test]
		public void Tuples()
		{
			Stmt("(a);", F.InParens(a));
			Stmt("(a;);", F.Tuple(a));
			Stmt("(a; @``;);", F.Tuple(a, _("")));
			Exact("(x; @``);", F.Tuple(x, _("")));
			Expr("(x;)", F.Tuple(x));
			Stmt("(a; b);", F.Tuple(a, b));
			Exact("(a; c);", F.Tuple(a, c));
			Exact("(a; b; c + x);", F.Tuple(a, b, F.Call(S.Add, c, x)));
		}

		[Test]
		public void PrefixOps()
		{
			Stmt("/x;", F.Call(S.Div, x));
			Stmt("-a * b;", F.Call(S.Mul, F.Call(S._Negate, a), b));
			Stmt("-x** +x / ~x + &x & *x && !x == ^x;",
				F.Call(S.And, F.Call(S.AndBits, F.Call(S.Add, F.Call(S.Div, 
					F.Call(S._Negate, F.Call(S.Exp, x, F.Call(S._UnaryPlus, x))), 
					F.Call(S.NotBits, x)),
					F.Call(S._AddressOf, x)),
					F.Call(S._Dereference, x)),
					F.Call(S.Eq, F.Call(S.Not, x), F.Call(S.XorBits, x))));
			Stmt("@'-(x)** +x / ~x + &x & *x && !x == ^x;",
				F.Call(S.And, F.Call(S.AndBits, F.Call(S.Add, F.Call(S.Div, F.Call(S.Exp,
					F.Call(S._Negate, x), F.Call(S._UnaryPlus, x)), F.Call(S.NotBits, x)),
					F.Call(S._AddressOf, x)), F.Call(S._Dereference, x)),
					F.Call(S.Eq, F.Call(S.Not, x), F.Call(S.XorBits, x))));
			Stmt("> a = %b;", F.Call(S.GT, F.Call(S.Assign, a, F.Call(S.Mod, b))));
			Stmt(@"!! !!a", F.Call(S.PreBangBang, F.Call(S.PreBangBang, a)));
			Stmt(".a**b;", F.Call(S.Exp, F.Call(S.Dot, a), b));
			
			Stmt("-a**b;", F.Call(S.Sub, F.Call(S.Exp, a, b)));
			Stmt("-2**b;", F.Call(S.Sub, F.Call(S.Exp, two, b)));
		}

		[Test]
		public void SuffixOps()
		{
			Exact("a++;", F.Call(S.PostInc, a));
			Exact("a++ + ++a;", F.Call(S.Add, F.Call(S.PostInc, a), F.Call(S.PreInc, a)));
			Stmt(@"a.b --;", F.Call(S.PostDec, F.Call(S.Dot, a, b)));
			Stmt(@"a + b -<>-;", F.Call(S.Add, a, F.Call(@"'suf-<>-", b)));
			// Ensure printer isn't confused by "suf" prefix which also appears on suffix operators
			Exact(@"`'suffer`x;", Op(F.Call(@"'suffer", x)));
			Exact(@"a!! !!;", F.Call(S.SufBangBang, F.Call(S.SufBangBang, a)));
			Exact(@"!!a!!;", F.Call(S.PreBangBang, F.Call(S.SufBangBang, a)));
		}

		[Test]
		public void NamedOps()
		{
			Stmt(@"a `x` b `Foo` c", F.Call(Foo, F.Call(x, a, b), c));
			//Stmt(@"a \x b \Foo c", F.Call(Foo, F.Call(x, a, b), c));
			Stmt(@"(a `is` b) `is` bool", F.Call(_("is"), F.InParens(F.Call(_("is"), a, b)), _("bool")));
			Stmt(@"a `=` b && c", F.Call(S.And, F.Call(_("="), a, b), c));
			Stmt(@"`Foo` a == `Foo` b", F.Call(S.Eq, Op(F.Call(Foo, a)), Op(F.Call(Foo, b))));
			// Ideally it would print "i `before` e `except after` (@[] c `when` x)" but this will do
			Exact("i `before` e `except after` when(c, x);", 
				Op(F.Call(_("except after"), Op(F.Call(_("before"), _("i"), _("e"))), Op(F.Call(_("when"), c, x)))));
			// Bug: printer was using LES3 precedence for operators that start with an apostrophe
			Exact("x `'is` @'or(1 `'or` 3, <0);",
				Op(F.Call(S.Is, x, Op(F.Call("'or", Op(F.Call("'or", one, Number(3))), Op(F.Call(S.LT, zero)))))));
		}

		[Test]
		public void Stmts()
		{
			Test(Mode.Stmt, -1, "a;\nb;\nc;", a, b, c);
			Stmt("a.b(c);", F.Call(F.Dot(a, b), c));
			Expr("{\n  b(c);\n} + {\n  ;\n  Foo()\n}", F.Call(S.Add, F.Braces(F.Call(b, c)), F.Braces(F.Missing, F.Call(Foo))));
			Stmt("a.{\n  b;\n  c;}();", F.Call(F.Dot(a, F.Braces(b, c))));
		}

		[Test]
		public void SuperExprs()
		{
			Expr("a b c", F.Call(a, b, c));
			Expr("a (b c)", F.Call(a, F.InParens(F.Call(b, c))));
			Expr("if a > b {\n  c();\n};", F.Call("if", F.Call(S.GT, a, b), F.Braces(F.Call(c))));
			Stmt("if (a > b) {\n  c();\n};", F.Call("if", F.InParens(F.Call(S.GT, a, b)), F.Braces(F.Call(c))));
			Stmt("a + (b c)", F.Call(S.Add, a, F.InParens(F.Call(b, c))));
			var node = F.Call(a, F.Call(S.Add, b, F.InParens(F.Call(_("if"), c, F.Braces(a), _("else"), F.Braces(b)))));
			Expr("a b + (if c {\n  a;\n} else {\n  b;\n})", node);
			Stmt("get { x } = 0;", F.Call(S.get, F.Call(S.Assign, F.Braces(AppendStmt(x)), zero)));
		}

		[Test]
		public void Generics()
		{
			Expr("a!b", F.Of(a, b));
			Expr("a!(b)", F.Of(a, b));
			Expr("a!(b, c)", F.Of(a, b, c));
			Expr("a!()", F.Of(a));
			Expr("a.b!((x))", F.Dot(a, F.Of(b, F.InParens(x))));
			Expr("(@[] a.b)!Foo", F.Of(F.Dot(a, b), Foo));
			Expr("a.b!Foo",       F.Dot(a, F.Of(b, Foo)));
			Expr("a.b!Foo(x)", F.Call(F.Dot(a, F.Of(b, Foo)), x));
			Expr("a.b!(Foo.Foo)(x)", F.Call(F.Dot(a, F.Of(b, F.Dot(Foo, Foo))), x));
			Expr("a.b!(Foo(x))", F.Dot(a, F.Of(b, F.Call(Foo, x))));
			Stmt("Foo = a.b!c!x", F.Call(S.Assign, Foo, F.Dot(a, F.Of(b, F.Of(c, x)))));
		}

		[Test]
		public void Attributes()
		{
			Exact("@[Foo] a();", F.Attr(Foo, F.Call(a)));
			Exact("@[Foo] a = b;", F.Attr(Foo, F.Call(S.Assign, a, b)));
			Exact("@[a, b] Foo();", F.Attr(a, b, F.Call(Foo)));
			Stmt("a = (       b + c);",  F.Call(S.Assign, a,  F.InParens(F.Call(S.Add, b, c))));
			Stmt("a = (@[]    b + c);",  F.Call(S.Assign, a,             F.Call(S.Add, b, c)));
			Stmt("a = (@[Foo] b + c);",  F.Call(S.Assign, a, F.Attr(Foo, F.Call(S.Add, b, c))));
			Exact("@[a] (x = 1);",        F.Call(S.Assign, x, one).PlusAttrs(a, F.Id(S.TriviaInParens)));
			Exact("((@[a] x = 1));",      F.Call(S.Assign, x, one).PlusAttrs(F.Id(S.TriviaInParens), a));
			Exact("@[a] ((@[b] x = 1));", F.Call(S.Assign, x, one).PlusAttrs(a, F.Id(S.TriviaInParens), b));
			Exact("x = (@[a] ((@[b] x + 1)));", 
			           F.Call(S.Assign, x, F.Call(S.Add, x, one).PlusAttrs(a, F.Id(S.TriviaInParens), b)));
		}

		[Test]
		public void Lists()
		{
			//Exact("[x];", F.Call(S.Array, x));
			Exact("++[x];", F.Call(S.PreInc, F.Call(S.Array, x)));
			Exact("Foo = [a, b, c];", F.Call(S.Assign, Foo, F.Call(S.Array, a, b, c)));
			Exact("Foo = [a, b, c] + [x];", F.Call(S.Assign, Foo, F.Call(S.Add, F.Call(S.Array, a, b, c), F.Call(S.Array, x))));
		}

		[Test]
		public void PrecedenceChallenge()
		{
			Exact("a.(@[] -b);",    F.Dot(a, F.Call(S._Negate, b)));
			Exact("a.(@[] -b)(x);", F.Call(F.Dot(a, F.Call(S._Negate, b)), x));
			Exact("@'::(a.b, x).c;", F.Dot(F.Call(S.ColonColon, F.Dot(a, b), x), c));
		}

		[Test]
		public void SpacingChallenge()
		{
			Exact("@'Foo .(a; b);", F.Dot(F.Id("'Foo"), F.Tuple(a, b)));
		}

		[Test]
		public void TriviaTest_Comments()
		{
			LNode node;
			node = Foo.PlusAttr(F.Trivia(S.TriviaMLComment, " before ")).PlusTrailingTrivia(F.Trivia(S.TriviaMLComment, " after "));
			Exact("/* before */Foo; /* after */", node);
			node = one.PlusAttr(F.Trivia(S.TriviaSLComment, " before ")).PlusTrailingTrivia(F.Trivia(S.TriviaSLComment, " after "));
			Exact("// before \n1;\t// after ", node);
			node = F.Call(F.Id(S.Eq).PlusAttr(                 F.Trivia(S.TriviaMLComment, "[")).PlusTrailingTrivia(F.Trivia(S.TriviaSLComment, "]")), 
			                 Foo, x.PlusAttrs(F.TriviaNewline, F.Trivia(S.TriviaMLComment, "{")).PlusTrailingTrivia(F.Trivia(S.TriviaMLComment, "}"))
			                              ).PlusAttr(   F.Trivia(S.TriviaMLComment, " before ")).PlusTrailingTrivia(F.Trivia(S.TriviaMLComment, " after "));
			Exact("/* before */Foo /*[*/== //]\n  /*{*/x /*}*/; /* after */", node);

			node = F.Call(Foo).PlusAttrs(a.PlusTrailingTrivia(F.Trivia(S.TriviaSLComment, "Comment after a")), 
			                          b, F.Trivia(S.TriviaMLComment, "Comment before c"), c);
			Exact("@[a\t//Comment after a\n"+
			      "  , b] /*Comment before c*/@[c] Foo();", node);
			// TODO: The following example parses as shown but is printed out differently.
			// Either we should change AbstractTriviaInjector to emit the comment at the 
			// "top level" of the attribute list (not attached to `a`) or we should change 
			// the node printer to print the comma before it prints trailing trivia.
			node = F.Call(Foo).PlusAttrs(a, F.Trivia(S.TriviaSLComment, "Comment after a"),
										 b, F.Trivia(S.TriviaMLComment, "Comment before c"), c);
			Exact("@[a] //Comment after a\n"+
			      "@[b] /*Comment before c*/@[c] Foo();", node);
		}

		[Test]
		public void TriviaTest_Appending()
		{
			Test(Mode.Exact, 0, "{ a();\n  b();\n};", F.Braces(AppendStmt(F.Call(a)), F.Call(b)));
			// If all statements in a block are appended, the newline before } is omitted
			Test(Mode.Exact, 0, "{ a(x); b(); };", F.Braces(AppendStmt(F.Call(a, x)), AppendStmt(F.Call(b))));
			Test(Mode.Exact, 0, "a();\nb(); c();", F.Call(a), F.Call(b), AppendStmt(F.Call(c)));
		}

		[Test]
		public void TriviaTest_TriviaBetweenAttrs()
		{
			Exact("@[a] \nFoo();", F.Call(Foo).PlusAttrs(a, F.TriviaNewline));
			Exact("@[a, b] \n@[c] \nFoo();", F.Call(Foo).PlusAttrs(a, b, F.TriviaNewline, c, F.TriviaNewline));

			var node = F.Attr(_("Test"), F.Trivia(S.TriviaSLComment, " NUnit"),
					 F.Call(_("EditorBrowsable"), F.Dot(_("EditorBrowsableState"), _("Never"))), 
					 F.TriviaNewline,
				F.Call(Foo, a, b, c));
			Exact("@[Test] // NUnit\n" +
			     "@[EditorBrowsable(EditorBrowsableState.Never)] \n" +
			     "Foo(a, b, c);", node);
		}

		[Test]
		public void TriviaTest_BlankLinesBetweenStmts()
		{
			Test(Mode.Exact, 0, "a();\n\nb();\n\nc();",
				F.Call(a),
				OnNewLine(F.Call(b)),
				OnNewLine(F.Call(c)));
			Exact("{\n  \n  a();\n  \n  b();\n  \n  c();\n  \n};",
				F.Call(S.Braces,
					OnNewLine(F.Call(a)),
					OnNewLine(F.Call(b)),
					OnNewLine(NewlineAfter(F.Call(c)))));
			Test(Mode.Exact, 0, "a();\n\n\nb();",
				NewlineAfter(NewlineAfter(F.Call(a))),
				F.Call(b));
		}

		[Test]
		public void TriviaTest_BlankLinesBetweenArgs()
		{
			Exact("Foo(\n  a, \n  b, \n  x);",
				F.Call(Foo, OnNewLine(a), OnNewLine(b), OnNewLine(x)));
			Exact("Foo(\n  \n  a, \n  \n  b, \n  \n  c);",
				F.Call(Foo, OnNewLine(OnNewLine(a)),
					OnNewLine(OnNewLine(b)),
					OnNewLine(OnNewLine(c))));
		}

		[Test]
		public void JsonCompatibility()
		{
			// TODO: more thorough testing
			var braces = F.Braces(F.Call(S.Colon, String("ex"), zero), F.Call(S.Colon, String("why"), F.Id("null")));
			Stmt("{\n  \"ex\" : 0,\n  \"why\" : null\n}", braces);
			braces = F.Braces(AppendStmt(F.Call(S.Assign, F.Id("Normal"), zero)), AppendStmt(F.Call(S.Assign, F.Id("Silly"), one))).WithStyle(NodeStyle.Expression);
			Stmt("public enum Mode { Normal = 0, Silly = 1 };", F.Call("public", F.Id("enum"), F.Id("Mode"), braces), errorsExpected: 1);
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
