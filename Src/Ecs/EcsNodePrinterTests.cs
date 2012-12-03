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
	// I need to build and test the parser. So for now, just basic tests...
	[TestFixture]
	class EcsNodePrinterTests : Assert
	{
		static GreenFactory F = new GreenFactory(EmptySourceFile.Unknown);
		GreenNode a = F.Symbol("a"), b = F.Symbol("b"), c = F.Symbol("c");
		GreenNode Foo = F.Symbol("Foo"), @public = F.Symbol(S.Public), @static = F.Symbol(S.Static);

		[Test]
		void SimpleCallsAndVarDecls()
		{
			AreEqual("a;",      F.Symbol("a").Print());
			AreEqual("a.b.c;",  F.Dot(a, b, c).Print());
			AreEqual("a<b>.c;", F.Dot(F.Of(a, b), c).Print());
			AreEqual("a.b<c>;", F.Of(F.Dot(a, b), c).Print());
			AreEqual("a<b.c>;", F.Of(a, F.Dot(b, c)).Print());
			AreEqual("a(b);",   F.Call(a, b).Print());
			AreEqual("Foo a;",  F.Var(Foo, a).Print());
			AreEqual("int a;",  F.Var(F.Int32, a).Print());
		}

		GreenNode Attr(GreenNode attr, GreenNode node)
		{
			node = node.Unfrozen();
			node.Attrs.Insert(0, attr);
			return node;
		}

		[Test]
		void SimpleCallsAndAttributes()
		{
			AreEqual("[Foo] a;",            Attr(Foo, F.Symbol("a")).Print());
			AreEqual("[Foo] a.b.c;",        Attr(Foo, F.Dot(a, b, c)).Print());
			AreEqual("[Foo] a<b,c>;",       Attr(Foo, F.Of(a, b, c)).Print());
			AreEqual("#.([Foo] a, b, c);",  F.Dot(Attr(Foo, a), b, c).Print());
			AreEqual("#.(a, b, [Foo] c);",  F.Dot(a, b, Attr(Foo, c)).Print());
			AreEqual("#of([Foo] a, b, c);", F.Of(Attr(Foo, a), b, c).Print());
			AreEqual("#of(a, b, [Foo] c);", F.Of(a, b, Attr(Foo, c)).Print());
			AreEqual("public a;",           Attr(@public, a).Print());
			AreEqual("[Foo] public a(b);",  Attr(Foo, Attr(@public, F.Call(a, b))).Print());
			AreEqual("[Foo] static int a;", Attr(Foo, Attr(@static, F.Var(F.Int32, a))).Print());
			AreEqual("b public int a;",     Attr(b, Attr(@static, F.Var(F.Int32, a))).Print());
		}

		[Test]
		void SimpleExpressions()
		{
			AreEqual("a+b;",   F.Call(S.Add, a, b).Print());
			AreEqual("a>>b;",  F.Call(S.Shr, a, b).Print());
			AreEqual("a=b+c;", F.Call(S.Set, a, F.Call(S.Add, b, c)).Print());
		}

		[Test]
		void SimpleStatements()
		{
			var F = new GreenFactory(EmptySourceFile.Unknown);
			GreenNode a = F.Symbol("a"), b = F.Symbol("b"), c = F.Symbol("c");
			GreenNode Foo = F.Symbol("Foo");
			// TODO
		}

		[Test]
		void IsComplexIdentifierTests()
		{
			// TODO: Some things to try:
			// a                               ==> true
			// (a)                             ==> false
			// a.b        == #.(a,b)           ==> true
			// a.b.c      == #.(a,b,c)         ==> true
			// #.(a.b, c) == #.(#.(a,b),c)     ==> false
			// #.(a, b.c) == #.(a,#.(b,c))     ==> false
			// a<b>       == #of(a,b)          ==> true
			// a[]        == #of(#[],a)        ==> true
			// a.b<c>     == #of(#.(a,b),c)    ==> true
			// a(x)                            ==> true iff allowCall
			// a.b(x)     == #.(a,b)(x)        ==> true iff allowCall
			// a.b<c>(x)  == #of(#.(a,b),c)(x) ==> true iff allowCall
			// (a)(x)                          ==> false
			// (a.b)(x)   == (#.(a,b))(x)      ==> false
			// #of(a<b>,c) == #of(#of(a,b),c)  ==> false
		}
	}
}
