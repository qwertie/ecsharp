using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Loyc.CompilerCore;
using S = Loyc.CompilerCore.CodeSymbols;
using Loyc.Utilities;
using Loyc.Essentials;

namespace ecs
{
	// A main goal of these tests should be to make the EcsNodePrinter print 
	// something that doesn't round-trip perfectly. But before I can do that, 
	// I need to build and test the parser. So for now, a bunch of basic tests...
	[TestFixture]
	class EcsNodePrinterTests : Assert
	{
		int _testNum;
		void CheckIsComplexIdentifier(bool? result, GreenNode expr)
		{
			_testNum++;
			var is1 = EcsNodePrinter.IsComplexIdentifier(expr);
			var is2 = EcsNodePrinter.IsComplexIdentifierOrNull(expr.Head);
			if (result == null && !is1 && is2)
				return;
			else if (result == is1 && result == is2)
				return;

			Assert.Fail(string.Format(
				"IsComplexIdentifier: fail on test #{0} '{1}'. Expected {2}, got {3}/{4}",
				_testNum, expr.ToString(), result, is1, is2));
		}

		[Test]
		public void IsComplexIdentifierTests()
		{
			_testNum = 0;
			CheckIsComplexIdentifier(true, a);                             // a
			CheckIsComplexIdentifier(null, F.InParens(a));                 // (a)
			CheckIsComplexIdentifier(true, F.Dot(a, b));                   // a.b
			CheckIsComplexIdentifier(true, F.Dot(a, b, c));                // a.b.c
			CheckIsComplexIdentifier(null, F.Dot(F.Dot(a, b), c));         // #.(a.b, c)
			CheckIsComplexIdentifier(null, F.Dot(a, F.Dot(b, c)));         // #.(a, b.c)
			CheckIsComplexIdentifier(true, F.Of(a, b));                    // a<b>        == #of(a,b)          ==> true
			CheckIsComplexIdentifier(true, F.Of(F.Symbol(S.Bracks), b));   // a[]         == #of(#[],a)        ==> true
			CheckIsComplexIdentifier(true, F.Of(F.Dot(a,b),F.Dot(c,x)));   // a.b<c.x>    == #of(#.(a,b),#.(c,x)) ==> true
			CheckIsComplexIdentifier(null, F.Call(a, x));                  // a(x)                             ==> true for Head
			CheckIsComplexIdentifier(null, F.Call(F.Dot(a,b), x));         // a.b(x)      == #.(a,b)(x)        ==> true for Head
			CheckIsComplexIdentifier(null, F.Call(F.Of(F.Dot(a,b),c), c)); // a.b<c>(x)   == #of(#.(a,b),c)(x) ==> true for Head
			CheckIsComplexIdentifier(false, F.Call(F.InParens(a), x));     // (a)(x)                           ==> false
			CheckIsComplexIdentifier(false, F.Call(F.InParens(F.Dot(a,b)),x));// (a.b)(x) == (#.(a,b))(x)      ==> false
			CheckIsComplexIdentifier(null, F.Of(F.Of(a,b),c));             // #of(a<b>,c) == #of(#of(a,b),c)   ==> false
		}


		static GreenFactory F = new GreenFactory(EmptySourceFile.Unknown);
		GreenNode a = F.Symbol("a"), b = F.Symbol("b"), c = F.Symbol("c"), x = F.Symbol("x");
		GreenNode Foo = F.Symbol("Foo"), @partial = F.Symbol("#partial"), one = F.Literal(1);
		GreenNode @public = F.Symbol(S.Public), @static = F.Symbol(S.Static);

		[Test]
		public void SimpleCallsAndVarDecls()
		{
			Check("a;",       F.Symbol("a"));
			Check("(a);",     F.InParens(F.Symbol("a")));
			Check("((a));",   F.InParens(F.InParens(F.Symbol("a"))));
			Check("#public;", @public);
			Check("@public;", F.Symbol("public"));
			Check("a.b.c;",   F.Dot(a, b, c));
			Check("a<b>.c;",  F.Dot(F.Of(a, b), c));
			Check("a.b<c>;",  F.Of(F.Dot(a, b), c));
			Check("a<b.c>;",  F.Of(a, F.Dot(b, c)));
			Check("a<b, c>;", F.Of(a, b, c));
			Check("a(b);",    F.Call(a, b));
			Check("a<b>(c);", F.Call(F.Of(a, b), c));
			Check("a.b(c);",  F.Call(F.Dot(a, b), c));
			Check("Foo a;",   F.Var(Foo, a));
			Check("int a;",   F.Var(F.Int32, a));
			Check(@"\Foo x;", F.Var(F.Call(S.Substitute, Foo), x));
			Check(@"\(a(b)) x;", F.Var(F.Call(S.Substitute, F.Call(a, b)), x));
		}

		void Check(string result, GreenNode input, Action<EcsNodePrinter> configure = null, bool exprMode = false)
		{
			var sb = new StringBuilder();
			var printer = input.NewEcsPrinter(sb);
			if (configure != null)
				configure(printer);
			if (exprMode)
				printer.PrintExpr();
			else
				printer.PrintStmt();
			AreEqual(result, sb.ToString());
		}
		
		GreenNode Attr(GreenNode attr, GreenNode node)
		{
			node = node.Unfrozen();
			node.Attrs.Insert(0, attr);
			return node;
		}

		[Test]
		public void SimpleCallsAndAttributes()
		{
			Check("[Foo] a;",              Attr(Foo, a));
			Check("[Foo] a.b.c;",          Attr(Foo, F.Dot(a, b, c)));
			Check("[Foo] a<b, c>;",        Attr(Foo, F.Of(a, b, c)));
			Check("#.([Foo] a, b, c);",    F.Dot(Attr(Foo, a), b, c));
			Check("#.(a, b, [Foo] c);",    F.Dot(a, b, Attr(Foo, c)));
			Check("#of([Foo] a, b, c);",   F.Of(Attr(Foo, a), b, c));
			Check("#of(a, b, [Foo] c);",   F.Of(a, b, Attr(Foo, c)));
			Check("public a;",             Attr(@public, a));
			Check("[Foo] public a(b);",    Attr(Foo, Attr(@public, F.Call(a, b))));
			Check("[Foo] static int a;",   Attr(Foo, Attr(@static, F.Var(F.Int32, a))));
			Check("partial public int a;", Attr(partial, Attr(@public, F.Var(F.Int32, a))));
			Check("[#public, Foo] int a;", Attr(@public, Attr(Foo, F.Var(F.Int32, a))));
		}

		GreenNode SetAlternate(GreenNode node)
		{
			node = node.Unfrozen();
			node.Style |= NodeStyle.Alternate;
			return node;
		}

		[Test]
		public void Literals()
		{
			Check("6;",        F.Literal(6));
			Check("5m;",       F.Literal(5m));
			Check("4L;",       F.Literal(4L));
			Check("3.5d;",     F.Literal(3.5d));
			Check("3d;",       F.Literal(3d));
			Check("2.5f;",     F.Literal(2.5f));
			Check("2f;",       F.Literal(2f));
			Check("1u;",       F.Literal(1u));
			Check("0uL;",      F.Literal(0uL));
			Check("-1;",       F.Literal(-1));
			Check("0xff;",     SetAlternate(F.Literal(0xFF)));
			Check("null;",     F.Literal(null));
			Check("false;",    F.Literal(false));
			Check("true;",     F.Literal(true));
			Check("'$';",      F.Literal('$'));
			Check(@"'\0';",    F.Literal('\0'));
			Check(@"""hi"";",  F.Literal("hi"));
			Check(@"@""hi"";", SetAlternate(F.Literal("hi")));
			Check("@\"\n\";",  SetAlternate(F.Literal("\n")));
			Check("@@\"\n\";", Attr(F.Symbol(S.StyleDoubleVerbatim), F.Literal("\n")));
			Check("();",       F.Literal(@void.Value));
			Check("$hello;",   F.Literal(GSymbol.Get("hello")));
			Check("$int;",     F.Literal(GSymbol.Get("int")));
			Check("$#int;",    F.Literal(GSymbol.Get("#int")));
			Check("$'1+1';",   F.Literal(GSymbol.Get("1+1")));
			Check("$'1';",     F.Literal(GSymbol.Get("1")));
			Check("123456789123456789uL;", F.Literal(123456789123456789uL));
			Check("0xffffffffffffffffuL;", SetAlternate(F.Literal(0xFFFFFFFFFFFFFFFFuL)));
		}

		[Test]
		public void SimpleExpressions()
		{
			Check("a+b;",        F.Call(S.Add, a, b));
			Check("a+b+c;",      F.Call(S.Add, F.Call(S.Add, a, b), c));
			Check("+a;",         F.Call(S.Add, a));
			Check("#+(a, b, c);",F.Call(S.Add, a, b, c));
			Check("a>>b;",       F.Call(S.Shr, a, b));
			Check("a=b+c;",      F.Call(S.Set, a, F.Call(S.Add, b, c)));
			Check("a(b)(c);",    F.Call(F.Call(a, b), c));
			Check("a++--;",      F.Call(S.PostDec, F.Call(S.PostInc, a)));
			Check("x=>x+1;",     F.Call(S.Lambda, x, F.Call(S.Add, x, one)));
		}

		[Test]
		public void TuplesAndVarDeclsInExpressions()
		{
			Check("Foo a, b, c;",          F.Var(Foo, a, b, c));
			Check("Foo? a, b, c;",         F.Var(F.Of(F.Symbol(S.QuestionMark), Foo), a, b, c));
			Check("(#var(Foo, a, b, c));", F.InParens(F.Var(Foo, a, b, c)));
			Check("(Foo a)=x;",            F.Call(S.Set, F.InParens(F.Var(Foo, a)), x));
			Check("(Foo a)=>a;",           F.Call(S.Lambda, F.InParens(F.Var(Foo, a)), a));
			Check("(#var(Foo, a))+x;",     F.Call(S.Add, F.InParens(F.Var(Foo, a)), x));
			var x_1 = F.Call(S.Tuple, x, one);
			Check("(a, b)=(x, 1);",        F.Call(S.Set, F.Call(S.Tuple, a, b), x_1));
			Check("(a,)=(x,);",            F.Call(S.Set, F.Call(S.Tuple, a), F.Call(S.Tuple, x)));
			Check("(a, Foo b)=(x, 1);",    F.Call(S.Set, F.Call(S.Tuple, a, F.Var(Foo, b)), x_1));
			Check("(Foo a, b)=(x, 1);",    F.Call(S.Set, F.Call(S.Tuple, F.Var(Foo, a), b), x_1));
			Check("(#var(Foo, a)+1, b)=(x, 1);", F.Call(S.Set, F.Call(S.Tuple, F.Call(S.Add, F.Var(Foo, a), one), b), x_1));
			Check("(Foo a,)=(x,);",        F.Call(S.Set, F.Call(S.Tuple, F.Var(Foo, a)), F.Call(S.Tuple, x)));
		}

		[Test]
		public void SpecialOperators()
		{
			Check("c ? Foo(x) : a+b;", F.Call(S.QuestionMark, c, F.Call(Foo, x), F.Call(S.Add, a, b)));
			Check("Foo[x];",           F.Call(S.Bracks, Foo, x));
			Check("Foo[a, b];",        F.Call(S.Bracks, Foo, a, b));
			Check("#[](Foo);",         F.Call(S.Bracks, Foo)); // not "Foo[];" because Foo[] means #of(#[], Foo)
			Check("#[]();",            F.Call(S.Bracks));
			Check("(Foo) x;",          F.Call(S.Cast, x, Foo));
			Check("x(->Foo);",         SetAlternate(F.Call(S.Cast, x, Foo)));
			Check("x as Foo;",         F.Call(S.As, x, Foo));
			Check("x using Foo;",      F.Call(S.UsingCast, x, Foo));
			Check("x(as Foo);",        SetAlternate(F.Call(S.As, x, Foo)));
			Check("x(using Foo);",     SetAlternate(F.Call(S.UsingCast, x, Foo)));
			Check("x++;",              F.Call(S.PostInc, x));
			Check("x--;",              F.Call(S.PostDec, x));
			Check("#postInc(a, b);",   F.Call(S.PostInc, a, b));
			Check("#postDec();",       F.Call(S.PostDec));
			Check("@(a=b);",           F.Call(S.CodeQuote, F.Call(S.Set, a, b)));
			Check("@(a=b, Foo());",    F.Call(S.CodeQuote, F.Call(S.Set, a, b), F.Call(Foo)));
			Check("@@(a=b);",          F.Call(S.CodeQuoteSubstituting, F.Call(S.Set, a, b)));
			Check("@@(a=b, Foo());",   F.Call(S.CodeQuoteSubstituting, F.Call(S.Set, a, b), F.Call(Foo)));
		}

		[Test]
		public void ExpressionsAndAttrs()
		{
			// The printer must use prefix notation if the arguments passed to an 
			// operator have attributes.
			Check("#+([Foo] a, b);",    F.Call(S.Add, Attr(Foo, a), b));
			Check("#+(a, [Foo] b);",    F.Call(S.Add, a, Attr(Foo, b)));
			Check("#[]([Foo] a, b);",   F.Call(S.Bracks, Attr(Foo, a), b));
			Check("a[[Foo] b];",        F.Call(S.Bracks, a, Attr(Foo, b)));
			Check("#?([Foo] c, a, b);", F.Call(S.QuestionMark, Attr(Foo, c), a, b));
			Check("#?(c, [Foo] a, b);", F.Call(S.QuestionMark, c, Attr(Foo, a), b));
			Check("#?(c, a, [Foo] b);", F.Call(S.QuestionMark, c, a, Attr(Foo, b)));
		}

		[Test]
		public void BugFixes()
		{
			Check("#+(a, b)(c, 1);", F.Call(F.Call(S.Add, a, b), c, one)); // was: "c+1"
		}

		public void CastComplications()
		{
			Check(@"(a using Foo)(x)",F.Call(F.InParens(F.Call(S.UsingCast, a, Foo)), x), null, true);
			Check(@"a(using Foo)(x)", F.Call(F.Call(S.UsingCast, a, Foo), x), null, true);
			Check(@"(a) b(x);",       F.Call(S.Cast, F.Call(b, x), a));
			Check(@"b(->a)(x);",      F.Call(F.Call(S.Cast, b, a), x));
			//TODO: traditional cast style should only be printed if target is a data type
		}

		[Test]
		public void PrecedenceChallenges()
		{
			Check(@"#.(a, -b);",      F.Dot(a, F.Call(S._Negate, b)));        // a.-b     would be the ideal output
			Check(@"#.(a, -b, c);",   F.Dot(a, F.Call(S._Negate, b), c));     // a.-b.c   would be the ideal output
			Check(@"#.(a, -b.c);",    F.Dot(a, F.Call(S._Negate, F.Dot(b, c))));
			Check(@"#.(a, (b))(c);",  F.Call(F.Dot(a, F.InParens(b)), c)); // a.(b)(c) would be the ideal output
			// The printer should revert to prefix notation in certain cases in 
			// order to faithfully represent the original tree.
			Check(@"a*b+c;",          F.Call(S.Add, F.Call(S.Mul, a, b), c));
			Check(@"(a+b)*c;",        F.Call(S.Mul, F.InParens(F.Call(S.Add, a, b)), c));
			Check(@"#+(a, b)*c;",     F.Call(S.Mul, F.Call(S.Add, a, b), c));
			Check(@"--a++;",          F.Call(S.PreDec, F.Call(S.PostInc, a)));
			Check(@"(--a)++;",        F.Call(S.PostInc, F.InParens(F.Call(S.PreDec, a))));
			Check(@"#--(a)++;",       F.Call(S.PostInc, F.Call(S.PreDec, a)));
			GreenNode a_b = F.Dot(a, b), a_b__c = F.Call(S.NullDot, F.Dot(a, b), c);
			Check(@"a.b??.c.x;",      F.Call(S.NullDot, a_b, F.Dot(c, x)));
			Check(@"(a.b??.c).x;",    F.Dot(F.InParens(a_b__c), x));
			Check(@"#??.(a.b, c).x;", F.Dot(a_b__c, x));
			Check(@"++\x;",           F.Call(S.PreInc, F.Call(S.Substitute, x)));
			Check(@"++\([Foo] x);",   F.Call(S.PreInc, F.Call(S.Substitute, Attr(Foo, x))));
			Check(@"a ? b : c;",      F.Call(S.QuestionMark, a, b, c));
			Check(@"a ? b+x : c+x;",  F.Call(S.QuestionMark, a, F.Call(S.Add, b, x), F.Call(S.Add, c, x)));
			Check(@"a ? b=x : (c=x);",F.Call(S.QuestionMark, a, F.Call(S.Set, b, x), F.InParens(F.Call(S.Set, c, x))));
			// A prefix operator can appear on the right-hand side of any infix/
			// prefix operator regardless of the precedence of the two operators.
			Check(@"++\x;",           F.Call(S.PreInc, F.Call(S.Substitute, x))); // easy
			Check(@"++--x;",          F.Call(S.PreInc, F.Call(S.PreDec, x)));     // easy
			Check(@"\++x;",           F.Call(S.Substitute, F.Call(S.PreInc, x)));
			Check(@".~x;",            F.Call(S.Dot, F.Call(S.NotBits, x)));
			// Note: an analagous rule does NOT exist for suffix operators because 
			// (1) x++ and x-- do not need this rule to work in expressions 
			//     like "x++--.Foo" because ++, --, and . have the same precedence
			//     and can be used together already with no special rule.
			// (2) The other suffix operator, the `backtick`, does not use this rule
			//     because input like "a `foo`.x" would be ambiguous: it could be 
			//     parsed as "(a `foo`).x" or as "a `foo` (.x)"
			Check(@"x++.Foo",         F.Dot(F.Call(S.PostInc, x), Foo));
			Check(@"x++.Foo()",       F.Call(F.Dot(F.Call(S.PostInc, x), Foo)));
			Check(@"x++--.Foo",       F.Dot(F.Call(S.PostDec, F.Call(S.PostInc, x)), Foo));
			// Due to its high precedence, the argument of a the \ operator must 
			// be in parens unless it is trivial.
			Check(@"\x;",             F.Call(S.Substitute, x));
			Check(@"\(x++);",         F.Call(S.Substitute, F.Call(S.PostInc, x)));
			Check(@"\(Foo(x));",      F.Call(S.Substitute, F.Call(Foo, x)));
			Check(@"\(a.b);",         F.Call(S.Substitute, F.Dot(a, b)));
			Check(@"\(a.b<c>);",      F.Call(S.Substitute, F.Of(F.Dot(a, b), c)));
			Check(@"\((Foo) x);",     F.Call(S.Substitute, F.Call(S.Cast, x, Foo)));
			Check(@"\(x(->Foo));",    F.Call(S.Substitute, SetAlternate(F.Call(S.Cast, x, Foo))));
		}

		[Test]
		public void SpecialCSharpChallenges()
		{
			var neg_a = F.Call(S._Negate, a);
			Check(@"(Foo)(-a);",      F.Call(S.Cast, F.InParens(neg_a), Foo));
			Check(@"(Foo)#-(a);",     F.Call(S.Cast, neg_a, Foo));
			var Foo_a = F.Of(Foo, a);
			Check(@"(Foo<a>)(x);",    F.Call(S.Cast, F.InParens(x), Foo_a));
			Check(@"([] Foo<a>)(x);", F.Call(F.InParens(Foo_a), x)); // [] certifies "this is not a cast!"
		}

		[Test]
		public void OptionsTest()
		{
			Check(@"Foo* x;",           F.Var(F.Of(F.Symbol(S._Pointer), Foo), x), p => p.AllowPointers = true);
			Check(@"#*<Foo> x;",        F.Var(F.Of(F.Symbol(S._Pointer), Foo), x));
			Check(@"a*b;",              F.Call(S.Mul, a, b));
			Check(@"#*(a, b);",         F.Call(S.Mul, a, b), p => p.AllowPointers = true);
			Check(@"#+(a, b)*c;",       F.Call(S.Mul, F.Call(S.Add, a, b), c));
			Check(@"(a+b)*c;",          F.Call(S.Mul, F.Call(S.Add, a, b), c), p => p.AllowExtraParenthesis = true);
			Check(@"#-(a)++;",          F.Call(S.PostInc, F.Call(S._Negate, a)));
			Check(@"(-a)++;",           F.Call(S.PostInc, F.Call(S._Negate, a)), p => p.AllowExtraParenthesis = true);
			Check(@"b(x)(->Foo);",      SetAlternate(F.Call(S.Cast, F.Call(b, x), Foo)));
			Check(@"(Foo) b(x);",       SetAlternate(F.Call(S.Cast, F.Call(b, x), Foo)), p => p.PreferOldStyleCasts = true);
			Check(@"b(->Foo)(x);",      F.Call(F.Call(S.Cast, b, Foo), x));
			Check(@"#cast(b, Foo)(x);", F.Call(F.Call(S.Cast, b, Foo), x), p => p.PreferOldStyleCasts = true);
			Check(@"((Foo) b)(x);",     F.Call(F.Call(S.Cast, b, Foo), x), p => p.SetPlainCSharpMode());
		}

		[Test]
		public void SpecialEcsChallenges()
		{
			Check(@"Foo x = a;",            F.Var(Foo, x, a));
			Check(@"(Foo x = a) + 1;",      F.Call(S.Add, F.InParens(F.Var(Foo, x, a)), one));
			Check(@"#var(Foo, x(a)) + 1;",  F.Call(S.Add, F.Var(Foo, x, a), one));
			Check(@"#var(Foo, a) = x;",     F.Call(S.Set, F.Var(Foo, a), x));
			Check(@"#var(Foo, a) + x;",     F.Call(S.Set, F.Var(Foo, a), x));
			Check(@"x + #var(Foo, a);",     F.Call(S.Set, F.Var(Foo, a), x));
			Check(@"Foo:",                  F.Call(S.Label, Foo));
			GreenNode Foo_a = F.Call(S.NamedArg, Foo, a);
			Check(@"Foo: a",                Foo_a, null, true);
			Check(@"#namedArg(Foo, a);",    Foo_a, null, false);
			Check(@"#namedArg(Foo(x), a);", F.Call(S.NamedArg, F.Call(Foo, x), a));
			Check(@"b+(Foo: a);",           F.Call(S.Add, b, F.InParens(Foo_a)));
			Check(@"b+#namedArg(Foo, a);",  F.Call(S.Add, b, Foo_a));
		}

		[Test]
		public void AttrInHead()
		{
			// I needed a new syntax to support this case specifically... oops.
			Check("[a] [b]## c(x);", Attr(a, F.Call(Attr(b, c), x)));
			Check("[a] ([b] c())(x);", Attr(a, F.Call(Attr(b, F.Call(c)), x)));
		}


	}
}
