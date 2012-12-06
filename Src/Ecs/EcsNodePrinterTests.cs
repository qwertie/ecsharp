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
		static GreenFactory F = new GreenFactory(EmptySourceFile.Unknown);
		GreenNode a = F.Symbol("a"), b = F.Symbol("b"), c = F.Symbol("c"), x = F.Symbol("x");
		GreenNode Foo = F.Symbol("Foo"), @partial = F.Symbol("#partial");
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
		}

		void Check(string result, GreenNode input)
		{
			AreEqual(result, input.Print());
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

		[Test]
		public void AttrInHead()
		{
			// This is not a faithful representation. oops. maybe we need new syntax for this....
			Check("[a] [b] c(x);", Attr(a, F.Call(Attr(b, c), x)));
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
			Check("a+b;",      F.Call(S.Add, a, b));
			Check("+a;",       F.Call(S.Add, a));
			Check("#+(a,b,c);",F.Call(S.Add, a, b, c));
			Check("a>>b;",     F.Call(S.Shr, a, b));
			Check("a=b+c;",    F.Call(S.Set, a, F.Call(S.Add, b, c)));
		}

		[Test]
		public void PrecedenceChallenges()
		{
			GreenNode Foo_a = F.Call(S.NamedArg, Foo, a);
			Check(@"Foo:a;",                Foo_a);
			Check(@"#namedArg(Foo(x), a);", F.Call(S.NamedArg, F.Call(Foo, x), a));
			Check(@"b+(Foo:a);",            F.Call(S.Add, b, F.InParens(Foo_a)));
			Check(@"b+#namedArg(Foo, a);",  F.Call(S.Add, b, Foo_a));
			Check(@"--a++;",          F.Call(S.PreDec, F.Call(S.PostInc, a)));
			Check(@"(--a)++;",        F.Call(S.PostInc, F.InParens(F.Call(S.PreDec, a))));
			Check(@"#postInc(--a);",  F.Call(S.PostInc, F.Call(S.PreDec, a)));
			Check(@"(a) b(x)",        F.Call(S.Cast, F.Call(b, x), a));
			Check(@"#cast(b, a)(x)",  F.Call(F.Call(S.Cast, b, a), x));
			GreenNode a_b = F.Dot(a, b), a_b__c = F.Call(S.NullDot, F.Dot(a, b), c);
			Check(@"a.b??.c.x",       F.Call(S.NullDot, a_b, F.Dot(c, x)));
			Check(@"(a.b??.c).x",     F.Dot(F.InParens(a_b__c), x));
			Check(@"#.(a.b??.c, x)",  F.Dot(a_b__c, x));
			Check(@"++\x",            F.Call(S.PreInc, F.Call(S.Substitute, x)));
			Check(@"\++x",            F.Call(S.Substitute, F.Call(S.PreInc, x)));
			Check(@"a?b:c",           F.Call(S.QuestionMark, a, b, c));
			Check(@"a?b+x:c+x",       F.Call(S.QuestionMark, a, F.Call(S.Add, b, x), F.Call(S.Add, c, x)));
			Check(@"a?b=x:(c=x)",     F.Call(S.QuestionMark, a, F.Call(S.Set, b, x), F.InParens(F.Call(S.Set, c, x))));
		}

		[Test]
		public void SimpleStatements()
		{
			var F = new GreenFactory(EmptySourceFile.Unknown);
			GreenNode a = F.Symbol("a"), b = F.Symbol("b"), c = F.Symbol("c");
			GreenNode Foo = F.Symbol("Foo");
			// TODO
			Check("a+b;",      F.Call(S.Add, a, b));
		}

		int _testNum;
		void CheckIsComplexIdentifier(bool? result, GreenNode expr)
		{
			_testNum++;
			var is1 = EcsNodePrinter.IsComplexIdentifier(expr);
			var is2 = EcsNodePrinter.IsComplexIdentifierOrNull(expr.Head);
			if (result == null && is1 && !is2)
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
			// TODO: Some things to try:
			CheckIsComplexIdentifier(true, a);                             // a
			CheckIsComplexIdentifier(null, F.InParens(a));                 // (a)
			CheckIsComplexIdentifier(true, F.Dot(a, b));                   // a.b
			CheckIsComplexIdentifier(true, F.Dot(a, b, c));                // a.b.c
			CheckIsComplexIdentifier(false, F.Dot(F.Dot(a, b), c));        // #.(a.b, c)
			CheckIsComplexIdentifier(false, F.Dot(a, F.Dot(b, c)));        // #.(a, b.c)
			CheckIsComplexIdentifier(true, F.Of(a, b));                    // a<b>        == #of(a,b)          ==> true
			CheckIsComplexIdentifier(true, F.Of(F.Symbol(S.Bracks), b));   // a[]         == #of(#[],a)        ==> true
			CheckIsComplexIdentifier(true, F.Of(F.Dot(a,b),F.Dot(c,x)));   // a.b<c.x>    == #of(#.(a,b),#.(c,x)) ==> true
			CheckIsComplexIdentifier(null, F.Call(a, x));                  // a(x)                             ==> true for Head
			CheckIsComplexIdentifier(null, F.Call(F.Dot(a,b), x));         // a.b(x)      == #.(a,b)(x)        ==> true for Head
			CheckIsComplexIdentifier(null, F.Call(F.Of(F.Dot(a,b),c), c)); // a.b<c>(x)   == #of(#.(a,b),c)(x) ==> true for Head
			CheckIsComplexIdentifier(false, F.Call(F.InParens(a), x));     // (a)(x)                           ==> false
			CheckIsComplexIdentifier(false, F.Call(F.InParens(F.Dot(a,b)),x));// (a.b)(x) == (#.(a,b))(x)      ==> false
			CheckIsComplexIdentifier(false, F.Of(F.Of(a,b),c));            // #of(a<b>,c) == #of(#of(a,b),c)   ==> false
		}
	}
}
