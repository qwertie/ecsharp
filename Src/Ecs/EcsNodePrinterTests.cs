using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Loyc.CompilerCore;
using S = ecs.CodeSymbols;
using Loyc.Utilities;
using Loyc.Essentials;
using Loyc;
using GreenNode = Loyc.Syntax.LNode;
using Node = Loyc.Syntax.LNode;
using Loyc.Syntax;

namespace ecs
{
	// A main goal of these tests should be to make the EcsNodePrinter print 
	// something that doesn't round-trip perfectly.
	[TestFixture]
	class EcsNodePrinterTests : Assert
	{
		int _testNum;
		void CheckIsComplexIdentifier(bool? result, GreenNode expr)
		{
			var np = new EcsNodePrinter(expr, null);
			_testNum++;
			var is1 = np.IsComplexIdentifier(expr);
			var is2 = np.IsComplexIdentifierOrNull(expr.Target);
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
			CheckIsComplexIdentifier(true, F.Dot(a, b));                   // a.b
			CheckIsComplexIdentifier(null, F.Call(a, b, c));               // #.(a, b, c)                      ==> true for target
			CheckIsComplexIdentifier(true, F.Dot(F.Dot(a, b), c));         // a.b.c       == #.(#.(a, b), c)   ==> true
			CheckIsComplexIdentifier(true, F.Dot(a, b, c));                // a.b.c       == #.(#.(a, b), c)   ==> true
			CheckIsComplexIdentifier(null, F.Dot(a, F.Dot(b, c)));         // #.(a, b.c)
			CheckIsComplexIdentifier(true, F.Of(a, b));                    // a<b>        == #of(a,b)          ==> true
			CheckIsComplexIdentifier(true, F.Of(_(S.Bracks), b));          // a[]         == #of(#[],a)        ==> true
			CheckIsComplexIdentifier(true, F.Of(F.Dot(a,b),F.Dot(c,x)));   // a.b<c.x>    == #of(#.(a,b),#.(c,x)) ==> true
			CheckIsComplexIdentifier(null, F.Call(a, x));                  // a(x)                             ==> true for target
			CheckIsComplexIdentifier(null, F.Call(F.Dot(a,b), x));         // a.b(x)      == #.(a,b)(x)        ==> true for target
			CheckIsComplexIdentifier(null, F.Call(F.Of(F.Dot(a,b),c), c)); // a.b<c>(x)   == #of(#.(a,b),c)(x) ==> true for target
			CheckIsComplexIdentifier(false, F.Call(F.InParens(a), x));     // (a)(x)                           ==> false
			CheckIsComplexIdentifier(false, F.Call(F.InParens(F.Dot(a,b)),x));// (a.b)(x) == (#.(a,b))(x)      ==> false
			CheckIsComplexIdentifier(null, F.Of(F.Of(a,b),c));             // #of(a<b>,c) == #of(#of(a,b),c)   ==> false
		}

		static LNodeFactory F = new LNodeFactory(EmptySourceFile.Unknown);
		GreenNode a = F.Id("a"), b = F.Id("b"), c = F.Id("c"), x = F.Id("x");
		GreenNode Foo = F.Id("Foo"), IFoo = F.Id("IFoo"), T = F.Id("T");
		GreenNode zero = F.Literal(0), one = F.Literal(1), two = F.Literal(2);
		GreenNode @class = F.Id(S.Class), @partial = F.Id("#partial");
		GreenNode @public = F.Id(S.Public), @static = F.Id(S.Static), fooKW = F.Id("#foo");
		GreenNode @lock = F.Id(S.Lock), @if = F.Id(S.If), @out = F.Id(S.Out), @new = F.Id(S.New);
		GreenNode trivia_macroCall = F.Id(S.TriviaMacroCall), trivia_forwardedProperty = F.Id(S.TriviaForwardedProperty);
		GreenNode get = F.Id("get"), set = F.Id("set"), value = F.Id("value");
		GreenNode _(string name) { return F.Id(name); }
		GreenNode _(Symbol name) { return F.Id(name); }

		[Test]
		public void SimpleCallsAndVarDecls()
		{
			Expr("a",        a);
			Expr("(a)",      F.InParens(a));
			Expr("((a))",    F.InParens(F.InParens(a)));
			Expr("#public",  @public);
			Expr("@public",  _("public"));
			Expr("a.b.c",    F.Dot(a, b, c));
			Expr("a<b>.c",   F.Dot(F.Of(a, b), c));
			Expr("a.b<c>",   F.Of(F.Dot(a, b), c));
			Expr("a<b.c>",   F.Of(a, F.Dot(b, c)));
			Expr("a<b,c>",   F.Of(a, b, c));
			Expr("a(b)",     F.Call(a, b));
			Expr("a<b>(c)",  F.Call(F.Of(a, b), c));
			Expr("a.b(c)",   F.Call(F.Dot(a, b), c));
			Stmt("Foo a;",   F.Var(Foo, a));
			Stmt("int a;",   F.Var(F.Int32, a));
			Stmt("int[] a;", F.Var(F.Of(S.Bracks, S.Int32), a));
			Stmt("var a;",   F.Var(_(S.Missing), a));
			Stmt("@var a;",  F.Var(_("var"), a));
			Stmt(@"\Foo x;", F.Var(F.Call(S.Substitute, Foo), x));
			Stmt(@"\(a(b)) x;", F.Var(F.Call(S.Substitute, F.Call(a, b)), x));
		}

		protected virtual void Expr(string result, GreenNode input, Action<EcsNodePrinter> configure = null)
		{
			Stmt(result, input, configure, true);
		}
		protected virtual void Stmt(string result, GreenNode input, Action<EcsNodePrinter> configure = null, bool exprMode = false)
		{
			var sb = new StringBuilder();
			var printer = input.NewEcsPrinter(sb, "  ");
			printer.NewlineOptions &= ~(NewlineOpt.AfterOpenBraceInNewExpr | NewlineOpt.BeforeCloseBraceInNewExpr);
			if (configure != null)
				configure(printer);
			if (exprMode)
				printer.PrintExpr();
			else
				printer.PrintStmt();
			AreEqual(result, sb.ToString());
		}
		protected void Option(string before, string after, GreenNode input, Action<EcsNodePrinter> configure = null, bool exprMode = false)
		{
			Stmt(before, input, null, exprMode);
			Stmt(after, input, configure, exprMode);
		}
		
		GreenNode Attr(GreenNode attr, GreenNode node)
		{
			return node.WithAttrs(node.Attrs.Insert(0, attr));
		}
		GreenNode Attr(params GreenNode[] attrsAndNode)
		{
			LNode node = attrsAndNode[attrsAndNode.Length - 1];
			var attrs = node.Attrs;
			for (int i = 0; i < attrsAndNode.Length - 1; i++)
				attrs.Insert(i, attrsAndNode[i]);
			return node.WithAttrs(attrs);
		}

		[Test]
		public void SimpleCallsAndAttributes()
		{
			Stmt("[Foo] a;",              Attr(Foo, a));
			Stmt("[Foo] a.b.c;",          Attr(Foo, F.Dot(a, b, c)));
			Stmt("[Foo] a<b,c>;",         Attr(Foo, F.Of(a, b, c)));
			Stmt("#.([Foo] a, b).c;",     F.Dot(Attr(Foo, a), b, c));
			Stmt("#.(a, [Foo] b).c;",     F.Dot(a, Attr(Foo, b), c));
			Stmt("#.(a.b, [Foo] c);",     F.Dot(a, b, Attr(Foo, c)));
			Stmt("#.([Foo] a, b, c);",    F.Call(S.Dot, Attr(Foo, a), b, c));
			Stmt("#.(a, b, [Foo] c);",    F.Call(S.Dot, a, b, Attr(Foo, c)));
			Stmt("#of([Foo] a, b, c);",   F.Of(Attr(Foo, a), b, c));
			Stmt("a.[b,[Foo] c];",        F.Of(a, b, Attr(Foo, c)));
			Stmt("a.[b,Foo + c];",        F.Of(a, b, F.Call(S.Add, Foo, c)));
			Stmt("#of(Foo<a>, b);",       F.Of(F.Of(Foo, a), b));
			Stmt("public a;",             Attr(@public, a));
			Stmt("[Foo] public a(b);",    Attr(Foo, Attr(@public, F.Call(a, b))));
			Stmt("[Foo] static int a;",   Attr(Foo, Attr(@static, F.Var(F.Int32, a))));
			Stmt("partial public int a;", Attr(partial, Attr(@public, F.Var(F.Int32, a))));
			Stmt("[#lock] static int a;", Attr(@lock, Attr(@static, F.Var(F.Int32, a))));
			Stmt("[#public, Foo] int a;", Attr(@public, Attr(Foo, F.Var(F.Int32, a))));
			Stmt("public #foo;",          Attr(@public, fooKW));
		}

		GreenNode Alternate(GreenNode node)
		{
			node.Style |= NodeStyle.Alternate;
			return node;
		}
		GreenNode AsStyle(GreenNode node, NodeStyle s)
		{
			node.BaseStyle = s;
			return node;
		}
		GreenNode Operator(GreenNode node) { return AsStyle(node, NodeStyle.Operator); }
		GreenNode StmtStyle(GreenNode node) { return AsStyle(node, NodeStyle.Statement); }
		GreenNode ExprStyle(GreenNode node) { return AsStyle(node, NodeStyle.Expression); }

		[Test]
		public void Literals()
		{
			Expr("6",        F.Literal(6));
			Expr("5m",       F.Literal(5m));
			Expr("4L",       F.Literal(4L));
			Expr("3.5d",     F.Literal(3.5d));
			Expr("3d",       F.Literal(3d));
			Expr("2.5f",     F.Literal(2.5f));
			Expr("2f",       F.Literal(2f));
			Expr("1u",       F.Literal(1u));
			Expr("0uL",      F.Literal(0uL));
			Expr("-1",       F.Literal(-1));
			Expr("0xff",     Alternate(F.Literal(0xFF)));
			Expr("null",     F.Literal(null));
			Expr("false",    F.Literal(false));
			Expr("true",     F.Literal(true));
			Expr("'$'",      F.Literal('$'));
			Expr(@"'\0'",    F.Literal('\0'));
			Expr(@"""hi""",  F.Literal("hi"));
			Expr(@"@""hi""", Alternate(F.Literal("hi")));
			Expr("@\"\n\"",  Alternate(F.Literal("\n")));
			Expr("@@\"\n\"", Attr(_(S.TriviaDoubleVerbatim), F.Literal("\n")));
			Expr("void",     F.Literal(@void.Value));
			Expr("$hello",   F.Literal(GSymbol.Get("hello")));
			Expr("$int",     F.Literal(GSymbol.Get("int")));
			Expr("$`#int`",  F.Literal(GSymbol.Get("#int")));
			Expr("$`\\t`",   F.Literal(GSymbol.Get("\t")));    // Symbols take non-verbatim backquoted strings
			Expr("$`1+1`",   F.Literal(GSymbol.Get("1+1")));
			Expr("$`1`",     F.Literal(GSymbol.Get("1")));
			Expr("123456789123456789uL", F.Literal(123456789123456789uL));
			Expr("0xffffffffffffffffuL", Alternate(F.Literal(0xFFFFFFFFFFFFFFFFuL)));
		}

		[Test]
		public void SimpleExpressions()
		{
			Expr("a + b",        F.Call(S.Add, a, b));
			Expr("a + b + c",    F.Call(S.Add, F.Call(S.Add, a, b), c));
			Expr("+a",           F.Call(S.Add, a));
			Expr("#+(a, b, c)",  F.Call(S.Add, a, b, c));
			Expr("a >> b",       F.Call(S.Shr, a, b));
			Expr("a = b + c",    F.Call(S.Set, a, F.Call(S.Add, b, c)));
			Expr("a(b)(c)",      F.Call(F.Call(a, b), c));
			Expr("a++--",        F.Call(S.PostDec, F.Call(S.PostInc, a)));
			Expr("x => x + 1",   F.Call(S.Lambda, x, F.Call(S.Add, x, one)));
			Stmt("[Foo] a = b;", Attr(Foo, F.Call(S.Set, a, b)));
		}

		[Test]
		public void TuplesAndVarDeclsInExpressions()
		{
			Stmt("Foo a, b, c;",          F.Var(Foo, a, b, c));
			Stmt("Foo? a, b, c;",         F.Var(F.Of(_(S.QuestionMark), Foo), a, b, c));
			Stmt("(#var(Foo, a, b, c));", F.InParens(F.Var(Foo, a, b, c)));
			Stmt("(Foo a) = x;",          F.Call(S.Set, F.InParens(F.Var(Foo, a)), x));
			Stmt("(Foo a) => a;",         F.Call(S.Lambda, F.InParens(F.Var(Foo, a)), a));
			Stmt("(#var(Foo, a)) + x;",   F.Call(S.Add, F.InParens(F.Var(Foo, a)), x));
			var x_1 = F.Call(S.Tuple, x, one);
			Stmt("(a, b) = (x, 1);",      F.Call(S.Set, F.Call(S.Tuple, a, b), x_1));
			Stmt("(a,) = (x,);",          F.Call(S.Set, F.Call(S.Tuple, a), F.Call(S.Tuple, x)));
			Stmt("(a, Foo b) = (x, 1);",  F.Call(S.Set, F.Call(S.Tuple, a, F.Var(Foo, b)), x_1));
			Stmt("(Foo a, b) = (x, 1);",  F.Call(S.Set, F.Call(S.Tuple, F.Var(Foo, a), b), x_1));
			Stmt("(#var(Foo, a) + 1, b) = (x, 1);", F.Call(S.Set, F.Call(S.Tuple, F.Call(S.Add, F.Var(Foo, a), one), b), x_1));
			Stmt("(Foo a,) = (x,);",      F.Call(S.Set, F.Call(S.Tuple, F.Var(Foo, a)), F.Call(S.Tuple, x)));
		}

		[Test]
		public void SpecialOperators()
		{
			Expr("c ? Foo(x) : a + b", F.Call(S.QuestionMark, c, F.Call(Foo, x), F.Call(S.Add, a, b)));
			Expr("Foo[x]",           F.Call(S.Bracks, Foo, x));
			Expr("Foo[a, b]",        F.Call(S.Bracks, Foo, a, b));
			Expr("Foo[]",            F.Call(S.Bracks, Foo)); // "Foo[]" means #of(#[], Foo) only in a type context
			Expr("#[]()",            F.Call(S.Bracks));
			Expr("(Foo) x",          F.Call(S.Cast, x, Foo));
			Expr("x(->Foo)",         Alternate(F.Call(S.Cast, x, Foo)));
			Expr("x(->a + b)",       F.Call(S.Cast, x, F.Call(S.Add, a, b)));
			Expr("x as Foo",         F.Call(S.As, x, Foo));
			Expr("x using Foo",      F.Call(S.UsingCast, x, Foo));
			Expr("x(as Foo)",        Alternate(F.Call(S.As, x, Foo)));
			Expr("x(using Foo)",     Alternate(F.Call(S.UsingCast, x, Foo)));
			Expr("x++",              F.Call(S.PostInc, x));
			Expr("x--",              F.Call(S.PostDec, x));
			Expr("#postInc(a, b)",   F.Call(S.PostInc, a, b));
			Expr("#postDec()",       F.Call(S.PostDec));
			Expr("@(a = b)",         F.Call(S.CodeQuote, F.Call(S.Set, a, b)));
			Expr("@(a = b, Foo())",  F.Call(S.CodeQuote, F.Call(S.Set, a, b), F.Call(Foo)));
			Expr("@@(a = b)",        F.Call(S.CodeQuoteSubstituting, F.Call(S.Set, a, b)));
			Expr("@@(a = b, Foo())", F.Call(S.CodeQuoteSubstituting, F.Call(S.Set, a, b), F.Call(Foo)));
		}

		[Test]
		public void ExpressionsAndAttrs()
		{
			// The printer must use prefix notation if the arguments passed to an 
			// operator have attributes.
			Expr("#+([Foo] a, b)",    F.Call(S.Add, Attr(Foo, a), b));
			Expr("#+(a, [Foo] b)",    F.Call(S.Add, a, Attr(Foo, b)));
			Expr("#[]([Foo] a, b)",   F.Call(S.Bracks, Attr(Foo, a), b));
			Expr("a[[Foo] b]",        F.Call(S.Bracks, a, Attr(Foo, b)));
			Expr("#?([Foo] c, a, b)", F.Call(S.QuestionMark, Attr(Foo, c), a, b));
			Expr("#?(c, [Foo] a, b)", F.Call(S.QuestionMark, c, Attr(Foo, a), b));
			Expr("#?(c, a, [Foo] b)", F.Call(S.QuestionMark, c, a, Attr(Foo, b)));
		}

		[Test]
		public void BugFixes()
		{
			Stmt("#+(a, b)(c, 1);", F.Call(F.Call(S.Add, a, b), c, one)); // was: "c+1"
			// was "partial #var(Foo, a);" which would be parsed as a method declaration
			Stmt("([#partial] #var(Foo, a));", F.InParens(Attr(@partial, F.Var(Foo, a))));
		}

		[Test]
		public void BracesInExpr()
		{
			var stmt1 = F.Call(S.QuickBind, F.Dot(Foo, x), a);
			var stmt2 = F.Call(S.Add, F.Call(S.Mul, a, a), a);
			Expr("b + #(Foo.x:::a, a * a + a)",              F.Call(S.Add, b, F.List(stmt1, stmt2)));
			Expr("b + #{\n  Foo.x:::a;\n  #*(a, a) + a;\n}", F.Call(S.Add, b, StmtStyle(F.List(stmt1, stmt2))));
			Expr("b + {\n  Foo.x:::a;\n  #*(a, a) + a;\n}",  F.Call(S.Add, b, F.Braces(stmt1, stmt2)));
			Expr("b + #{}(Foo.x:::a, a * a + a)",            F.Call(S.Add, b, AsStyle(F.Braces(stmt1, stmt2), NodeStyle.PrefixNotation)));
		}

		[Test]
		public void PrecedenceChallenges()
		{
			Expr(@"#.(a, -b)",      F.Dot(a, F.Call(S._Negate, b)));     // a.-b would be ideal, but this will do
			Expr(@"#.(a, -b).c",    F.Dot(a, F.Call(S._Negate, b), c));  // a.-b.c might be parsable, we'll see
			Expr(@"#.(a, -b.c)",    F.Dot(a, F.Call(S._Negate, F.Dot(b, c))));
			Expr(@"a.(b)(c)",       F.Call(F.Dot(a, F.InParens(b)), c));
			// The printer should revert to prefix notation in certain cases in 
			// order to faithfully represent the original tree.
			Expr(@"a * b + c",      F.Call(S.Add, F.Call(S.Mul, a, b), c));
			Expr(@"(a + b) * c",    F.Call(S.Mul, F.InParens(F.Call(S.Add, a, b)), c));
			Expr(@"#+(a, b) * c",   F.Call(S.Mul, F.Call(S.Add, a, b), c));
			Expr(@"--a++",          F.Call(S.PreDec, F.Call(S.PostInc, a)));
			Expr(@"(--a)++",        F.Call(S.PostInc, F.InParens(F.Call(S.PreDec, a))));
			Expr(@"#--(a)++",       F.Call(S.PostInc, F.Call(S.PreDec, a)));
			GreenNode a_b = F.Dot(a, b), a_b__c = F.Call(S.NullDot, F.Dot(a, b), c);
			Expr(@"a.b??.c.x",      F.Call(S.NullDot, a_b, F.Dot(c, x)));
			Expr(@"(a.b??.c).x",    F.Dot(F.InParens(a_b__c), x));
			Expr(@"#??.(a.b, c).x", F.Dot(a_b__c, x));
			Expr(@"++\x",           F.Call(S.PreInc, F.Call(S.Substitute, x)));
			Expr(@"++\([Foo] x)",   F.Call(S.PreInc, F.Call(S.Substitute, Attr(Foo, x))));
			Expr(@"a ? b : c",      F.Call(S.QuestionMark, a, b, c));
			Expr(@"a ? b + x : c + x",  F.Call(S.QuestionMark, a, F.Call(S.Add, b, x), F.Call(S.Add, c, x)));
			Expr(@"a ? b = x : (c = x)",F.Call(S.QuestionMark, a, F.Call(S.Set, b, x), F.InParens(F.Call(S.Set, c, x))));
			// A prefix operator can appear on the right-hand side of any infix/
			// prefix operator regardless of the precedence of the two operators.
			Expr(@"++\x",           F.Call(S.PreInc, F.Call(S.Substitute, x))); // easy
			Expr(@"++--x",          F.Call(S.PreInc, F.Call(S.PreDec, x)));     // easy
			Expr(@"\++x",           F.Call(S.Substitute, F.Call(S.PreInc, x)));
			Expr(@"#.(~x)",         F.Call(S.Dot, F.Call(S.NotBits, x))); // a.~x would be ideal, but this will do
			// Note: an analagous rule does NOT exist for suffix operators because 
			// (1) x++ and x-- do not need this rule to work in expressions 
			//     like "x++--.Foo" because ++, --, and . have the same precedence
			//     and can be used together already with no special rule.
			// (2) The other suffix operator, the `backtick`, does not use this rule
			//     because input like "a `foo`.x" would be ambiguous: it could be 
			//     parsed as "(a `foo`).x" or as "a `foo` (.x)"
			Expr(@"x++.Foo",        F.Dot(F.Call(S.PostInc, x), Foo));
			Expr(@"x++.Foo()",      F.Call(F.Dot(F.Call(S.PostInc, x), Foo)));
			Expr(@"x++--.Foo",      F.Dot(F.Call(S.PostDec, F.Call(S.PostInc, x)), Foo));
			// Due to its high precedence, the argument of a the \ operator must 
			// be in parens unless it is trivial.
			Expr(@"\x",             F.Call(S.Substitute, x));
			Expr(@"\(x++)",         F.Call(S.Substitute, F.Call(S.PostInc, x)));
			Expr(@"\(Foo(x))",      F.Call(S.Substitute, F.Call(Foo, x)));
			Expr(@"\(a.b)",         F.Call(S.Substitute, F.Dot(a, b)));
			Expr(@"\(a.b<c>)",      F.Call(S.Substitute, F.Of(F.Dot(a, b), c)));
			Expr(@"\((Foo) x)",     F.Call(S.Substitute, F.Call(S.Cast, x, Foo)));
			Expr(@"\(x(->Foo))",    F.Call(S.Substitute, Alternate(F.Call(S.Cast, x, Foo))));
		}

		[Test]
		public void SpecialEcsChallenges()
		{
			Expr("Foo x = a",            F.Var(Foo, x.Name, a));
			Expr("(Foo x = a) + 1",      F.Call(S.Add, F.InParens(F.Var(Foo, x.Name, a)), one));
			Expr("#var(Foo, x(a)) + 1",  F.Call(S.Add, F.Var(Foo, x.Name, a), one));
			Expr("#var(Foo, a) = x",     F.Call(S.Set, F.Var(Foo, a), x));
			Expr("#var(Foo, a) + x",     F.Call(S.Add, F.Var(Foo, a), x));
			Expr("x + #var(Foo, a)",     F.Call(S.Add, x, F.Var(Foo, a)));
			Expr("#label(Foo)",          F.Call(S.Label, Foo));
			Stmt("Foo:",                 F.Call(S.Label, Foo));
			GreenNode Foo_a = F.Call(S.NamedArg, Foo, a);
			Expr("Foo: a",               Foo_a);
			Stmt("#namedArg(Foo, a);",   Foo_a);
			Expr("#namedArg(Foo(x), a)", F.Call(S.NamedArg, F.Call(Foo, x), a));
			Expr("b + (Foo: a)",         F.Call(S.Add, b, F.InParens(Foo_a)));
			Expr("b + #namedArg(Foo, a)",F.Call(S.Add, b, Foo_a));
			// Ambiguity between multiplication and pointer declarations:
			// - multiplication at stmt level => prefix notation, except in #result or when lhs is not a complex identifier
			// - pointer declaration inside expr => generic, not pointer, notation
			Expr("a * b",                F.Call(S.Mul, a, b));
			Stmt("a `#*` b;",            F.Call(S.Mul, a, b));
			Stmt("a() * b;",             F.Call(S.Mul, F.Call(a), b));
			Expr("#result(a * b)",       F.Result(F.Call(S.Mul, a, b)));
			Stmt("{\n  a * b\n}",        F.Braces(F.Result(F.Call(S.Mul, a, b))));
			Stmt("Foo* a = x;",          F.Var(F.Of(_(S._Pointer), Foo), F.Call(a, x)));
			Expr("#*<Foo> a = x",        F.Var(F.Of(_(S._Pointer), Foo), F.Call(a, x)));
			// Ambiguity between bitwise not and destructor declarations
			Expr("~Foo()",               F.Call(S.NotBits, F.Call(Foo)));
			Stmt("#~(Foo());",           F.Call(S.NotBits, F.Call(Foo)));
			Stmt("~Foo;",                F.Call(S.NotBits, Foo));
		}

		[Test]
		public void TypeContext()
		{
			// Certain syntax trees can print differently in a "type context" than elsewhere.
			var FooBracks = F.Call(S.Bracks, Foo);
			var FooArray = F.Of(_(S.Bracks), Foo);
			var FooNullable = F.Of(_(S.QuestionMark), Foo);
			var FooPointer = F.Of(_(S._Pointer), Foo);
			Expr("Foo[]",             FooBracks);
			Expr("#[]<Foo>",          FooArray);
			Expr("#?<Foo>",           FooNullable);
			Expr("#*<Foo>",           FooPointer);
			Stmt("#var(Foo[], a);",   F.Var(FooBracks, a));
			Stmt("Foo[] a;",          F.Var(FooArray, a));
			Stmt("typeof(Foo?);",     F.Call(S.Typeof, FooNullable));
			Stmt("default(Foo*);",    F.Call(S.Default, FooPointer));
			Stmt("(Foo[]) a;",        F.Call(S.Cast, a, FooArray));
			Stmt("a(->Foo?);",        Alternate(F.Call(S.Cast, a, FooNullable)));
			Stmt("a(as Foo*);",       Alternate(F.Call(S.As, a, FooPointer)));
			Stmt("Foo.[#(Foo[])];",   F.Of(Foo, F.List(FooBracks)));
			Stmt("Foo.[#(#*<Foo>)];", F.Of(Foo, F.List(FooPointer)));
			Expr("checked(Foo[])",    F.Call(S.Checked, FooBracks));
			Stmt("Foo<a*> x;",        F.Var(F.Of(Foo, F.Of(_(S._Pointer), a)), x));
		}

		[Test]
		public void OptionsTest()
		{
			// MixImmiscibleOperators is tested elsewhere
			Action<EcsNodePrinter> parens = p => p.AllowExtraParenthesis = true;
			Action<EcsNodePrinter> oldCasts = p => p.PreferOldStyleCasts = true;
			//Action<EcsNodePrinter> allowPtrs = p => p.AllowPointers = true;
			Action<EcsNodePrinter> dropAttrs = p => p.DropNonDeclarationAttributes = true;
			Stmt(@"(Foo) b;",          Alternate(F.Call(S.Cast, b, Foo)), oldCasts);
			Stmt(@"b(->Foo)(x);",      F.Call(F.Call(S.Cast, b, Foo), x));
			Stmt(@"#cast(b, Foo)(x);", F.Call(F.Call(S.Cast, b, Foo), x), oldCasts);
			Stmt(@"((Foo) b)(x);",     F.Call(F.Call(S.Cast, b, Foo), x), p => p.SetPlainCSharpMode());
			Option(@"#+(a, b) / c;", @"(a + b) / c;", F.Call(S.Div, F.Call(S.Add, a, b), c), parens);
			Option(@"#-(a)++;",      @"(-a)++;",      F.Call(S.PostInc, F.Call(S._Negate, a)), parens);
			Option(@"b(x)(->Foo);",  @"(Foo) b(x);",  Alternate(F.Call(S.Cast, F.Call(b, x), Foo)), oldCasts);
			
			// Put attributes in various locations and watch them all disappear
			Option(@"[Foo] a + b;",        @"a + b;",     Attr(Foo, F.Call(S.Add, a, b)), dropAttrs);
			Option(@"public a(x);",        @"a(x);",      Attr(@public, F.Call(a, x)), dropAttrs);
			Option(@"a([#foo] x);",        @"a(x);",      F.Call(a, Attr(fooKW, x)), dropAttrs);
			Option(@"##([Foo] a)(x);",     @"a(x);",      F.Call(Attr(Foo, a), x), dropAttrs);
			Option(@"x[[Foo] a];",         @"x[a];",      F.Call(S.Bracks, x, Attr(Foo, a)), dropAttrs);
			Option(@"#[](static x, a);",   @"x[a];",      F.Call(S.Bracks, Attr(@static, x), a), dropAttrs);
			Option(@"#+([Foo] a, 1);",     @"a + 1;",     F.Call(S.Add, Attr(Foo, a), one), dropAttrs);
			Option(@"#+(a, [Foo] 1);",     @"a + 1;",     F.Call(S.Add, a, Attr(Foo, one)), dropAttrs);
			Option(@"#?(a, [#foo] b, c);", @"a ? b : c;", F.Call(S.QuestionMark, a, Attr(fooKW, b), c), dropAttrs);
			Option(@"#?(a, b, public c);", @"a ? b : c;", F.Call(S.QuestionMark, a, b, Attr(@public, c)), dropAttrs);
			Option(@"#++([Foo] x);",       @"++x;",       F.Call(S.PreInc, Attr(Foo, x)), dropAttrs);
			Option(@"#postInc([Foo] x);",  @"x++;",       F.Call(S.PostInc, Attr(Foo, x)), dropAttrs);
			Option(@"x(->static Foo);",    @"(Foo) x;",   F.Call(S.Cast, x, Attr(@static, Foo)), dropAttrs);
			Option(@"#var(static Foo, x);", @"Foo x;",    F.Var(Attr(@static, Foo), x), dropAttrs);
			Option(@"#var(Foo, static x);", @"Foo x;",    F.Var(Foo, Attr(@static, x)), dropAttrs);
			Option(@"#var(Foo<a>, [#foo] b, c(1));", @"Foo<a> b, c = 1;", F.Var(F.Of(Foo, a), Attr(fooKW, b), F.Call(c, one)), dropAttrs);
			Option(@"#var(Foo.[static a], b);", @"Foo<a> b;",             F.Var(F.Of(Foo, Attr(@static, a)), b), dropAttrs);
			Option(@"#var(#of(static Foo, a), b);", @"Foo<a> b;",         F.Var(F.Of(Attr(@static, Foo), a), b), dropAttrs);
		}

		[Test]
		public void ExprSpacingTests()
		{
			// TODO
		}

		[Test]
		public void CastComplications()
		{
			Expr(@"(a using Foo)(x)",F.Call(F.InParens(F.Call(S.UsingCast, a, Foo)), x));
			Expr(@"a(using Foo)(x)", F.Call(F.Call(S.UsingCast, a, Foo), x));
			Expr(@"(a) b(x)",        F.Call(S.Cast, F.Call(b, x), a));
			Expr(@"b(->a)(x)",       F.Call(F.Call(S.Cast, b, a), x));
			Expr(@"a(as Foo).b",     F.Dot(F.Call(S.As, a, Foo), b));
			Expr(@"\(a using Foo)",  F.Call(S.Substitute, F.Call(S.UsingCast, a, Foo)));
			Expr(@"\(a(using Foo))", F.Call(S.Substitute, Alternate(F.Call(S.UsingCast, a, Foo))));
		}

		[Test]
		public void SpecialCSharpChallenges()
		{
			// Cases that are difficult to handle due to ambiguities inherited from C#
			var neg_a = F.Call(S._Negate, a);
			Expr("(Foo) - a",         F.Call(S.Sub, F.InParens(Foo), a));
			Expr("(Foo) (-a)",        F.Call(S.Cast, F.InParens(neg_a), Foo));
			Expr("(Foo) #-(a)",       F.Call(S.Cast, neg_a, Foo));
			Expr("(Foo) #+(a)",       F.Call(S.Cast, F.Call(S._UnaryPlus, a), Foo));
			var Foo_a = F.Of(Foo, a); 
			Expr("(Foo<a>) (-a)",     F.Call(S.Cast, F.InParens(neg_a), Foo_a));
			Expr("([ ] Foo)(-a)",     F.Call(F.InParens(Foo), neg_a));   // [] certifies "this is not a cast!";
			Expr("([ ] Foo<a>)(-a)",  F.Call(F.InParens(Foo_a), neg_a)); // extra parenthesis would also work
			Expr("((Foo<a>))(-a)",    F.Call(F.InParens(Foo_a), neg_a), p => p.AllowExtraParenthesis = true);
			Expr("(a.b<c>) x",        F.Call(S.Cast, x, F.Of(F.Dot(a, b), c)));
			Expr("(a.b.[c > 1]) x",   F.Call(S.Cast, x, F.Of(F.Dot(a, b), F.Call(S.GT, c, one))));
			Expr("x(->[Foo] a.b<c>)", F.Call(S.Cast, x, Attr(Foo, F.Of(F.Dot(a, b), c))));
			Expr("x(->a * b)",        F.Call(S.Cast, x, F.Call(S.Mul, a, b)));
			Stmt("Foo* a;",           F.Var(F.Of(_(S._Pointer), Foo), a));
			Stmt("Foo `#*` a = b;",   F.Call(S.Set, F.Call(S.Mul, Foo, a), b)); // #*(Foo, a) = b; would also be acceptable
		}

		[Test]
		public void AttrInHead()
		{
			// Normally we can use prefix notation when children have attributes...
			Stmt("#+=([a] b, c);",      F.Call(S.AddSet, Attr(a, b), c));
			// But this is no solution if the head of a node has attributes. I needed a
			// new syntax to support this case specifically... oops! hadn't planned for this.
			Stmt("[a] ##([b] c)(x);",   Attr(a, F.Call(Attr(b, c), x)));
			Stmt("[a] ##([b] c())(x);", Attr(a, F.Call(Attr(b, F.Call(c)), x)));
		}

		[Test]
		public void Backtick()
		{
			GreenNode foo_a = F.Call(fooKW, a), foo_a_b = F.Call(fooKW, a, b);
			Expr("a `Foo` b", Operator(F.Call(Foo, a, b)));
			Expr("#foo(a, b)", foo_a_b);
			Expr("a `#foo` b", Operator(foo_a_b));
			Expr("#foo(a)", foo_a);
			Stmt("a = b `Foo` c;", F.Call(S.Set, a, Operator(F.Call(Foo, b, c))));
			Stmt("a = b `Foo` c**x;", F.Call(S.Set, a, Operator(F.Call(Foo, b, F.Call(S.Exp, c, x)))));
		}

		[Test]
		public void Immiscibility()
		{
			Action<EcsNodePrinter> parens = p => p.AllowExtraParenthesis = true;
			Action<EcsNodePrinter> mixImm = p => p.MixImmiscibleOperators = true;
			// Of course, operators can be mixed with themselves.
			Stmt("a + b + c;", F.Call(S.Add, F.Call(S.Add, a, b), c), parens);
			Stmt("a = b = c;", F.Call(S.Set, a, F.Call(S.Set, b, c)), parens);
			// But some cannot be mixed with each other, unless requested (with mixImm).
			Option("#<<(a, b) + 1;",    "(a << b) + 1;",    F.Call(S.Add, F.Call(S.Shl, a, b), one), parens);
			Option("#+(a, b) << 1;",    "(a + b) << 1;",    F.Call(S.Shl, F.Call(S.Add, a, b), one), parens);
			Option("#+(a, b) << 1;",    "a + b << 1;",      F.Call(S.Shl, F.Call(S.Add, a, b), one), mixImm);
			// "#&(a, b) == 1;" would also be acceptable output on the left:
			Option("a `#&` b == 1;",    "(a & b) == 1;",    F.Call(S.Eq, F.Call(S.AndBits, a, b), one), parens);
			Option("#==(a, b) & 1;",    "(a == b) & 1;",    F.Call(S.AndBits, F.Call(S.Eq, a, b), one), parens);
			Option("#==(a, b) & 1;",    "a == b & 1;",      F.Call(S.AndBits, F.Call(S.Eq, a, b), one), mixImm);
			Option("Foo(a, b) + 1;",    "(a `Foo` b) + 1;", F.Call(S.Add, Operator(F.Call(Foo, a, b)), one), parens);
			// #+(a, b) `foo` 1; would also be acceptable output on the left:
			Option("a `#+` b `Foo` 1;", "(a + b) `Foo` 1;", Operator(F.Call(Foo, F.Call(S.Add, a, b), one)), parens);
		}

		[Test]
		public void CallStyleOperators()
		{
			Expr("checked(a + b)",       F.Call(S.Checked, F.Call(S.Add, a, b)));
			Expr("unchecked(a << b)",    F.Call(S.Unchecked, F.Call(S.Shl, a, b)));
			Expr("default(Foo)",         F.Call(S.Default, Foo));
			Expr("default(int)",         F.Call(S.Default, F.Int32));
			Expr("typeof(Foo)",          F.Call(S.Typeof, Foo));
			Expr("typeof(int)",          F.Call(S.Typeof, F.Int32));
			Expr("typeof(Foo<int>)",     F.Call(S.Typeof, F.Call(S.Of, Foo, F.Int32)));
			Expr("sizeof(Foo<int>)",     F.Call(S.Sizeof, F.Call(S.Of, Foo, F.Int32)));
			
			Expr("default(int[])",       F.Call(S.Default,   F.Call(S.Of, _(S.Bracks), F.Int32)));
			Expr("typeof(int[])",        F.Call(S.Typeof,    F.Call(S.Of, _(S.Bracks), F.Int32)));
			Expr("sizeof(int[])",        F.Call(S.Sizeof,    F.Call(S.Of, _(S.Bracks), F.Int32)));
			Expr("checked(#[]<int>)",    F.Call(S.Checked,   F.Call(S.Of, _(S.Bracks), F.Int32)));
			Expr("unchecked(#[]<int>)",  F.Call(S.Unchecked, F.Call(S.Of, _(S.Bracks), F.Int32)));
		}

		[Test]
		public void OperatorNew()
		{
			Expr("new Foo(x)",            F.Call(S.New, F.Call(Foo, x)));
			Expr("new Foo(x) { a }",      F.Call(S.New, F.Call(Foo, x), a));
			Expr("new Foo(x) { [a] b = c }", 
			                              F.Call(S.New, F.Call(Foo, x), Attr(a, F.Call(S.Set, b, c))));
			Option("#new([#foo] Foo(x), a);", "new Foo(x) { a };", 
			                              F.Call(S.New, Attr(fooKW, F.Call(Foo, x)), a), p => p.DropNonDeclarationAttributes = true);
			Expr("new Foo()",             F.Call(S.New, F.Call(Foo)));
			Expr("new Foo { a }",         F.Call(S.New, F.Call(Foo), a));      // new Foo() { a } would also be ok
			Expr("#new([x] Foo(), a)",    F.Call(S.New, Attr(x, F.Call(Foo)), a));
			Expr("#new(##([x] Foo)(), a)",F.Call(S.New, F.Call(Attr(x, Foo)), a));
			Expr("new Foo { a }",         F.Call(S.New, F.Call(Foo), a));
			Expr("#new(Foo, a)",          F.Call(S.New, Foo, a));
			Expr("#new(Foo)",             F.Call(S.New, Foo));
			Expr("new #+(a, b)",          F.Call(S.New, F.Call(S.Add, a, b))); // #new(#+(a, b)) would also be ok
			Expr("new int[] { a, b }",    F.Call(S.New, F.Call(F.Of(S.Bracks, S.Int32)), a, b));
			Expr("new[] { a, b }",        F.Call(S.New, F.Call(S.Bracks), a, b));
			Expr("new[] { }",             F.Call(S.New, F.Call(S.Bracks)));
			Expr("#new(Foo()(), a)",      F.Call(S.New, F.Call(F.Call(Foo)), a));
			Expr("new int[][,] { a }",    F.Call(S.New, F.Call(F.Of(_(S._Array), F.Of(S.TwoDimensionalArray, S.Int32))), a));
			// This expression is illegal since it requires an initializer list, but it's parsable so should print ok
			Expr("new int[][,][,,]",      F.Call(S.New, F.Call(F.Of(_(S._Array), F.Of(_(S.TwoDimensionalArray), F.Of(S.GetArrayKeyword(3), S.Int32))))));
			Expr("new int[10][,] { a }",  F.Call(S.New, F.Call(F.Of(_(S._Array), F.Of(S.TwoDimensionalArray, S.Int32)), F.Literal(10)), a));
			Expr("new int[x, x][]",       F.Call(S.New, F.Call(F.Of(_(S.TwoDimensionalArray), F.Of(S._Array, S.Int32)), x, x)));
			Expr("new int[[Foo] x, x][]", F.Call(S.New, F.Call(F.Of(_(S.TwoDimensionalArray), F.Of(S._Array, S.Int32)), Attr(Foo, x), x)));
			Expr("new int[,]",            F.Call(S.New, F.Call(F.Of(S.TwoDimensionalArray, S.Int32))));
			Expr("new int[,]",            F.Call(S.New, F.Call(F.Of(_(S.TwoDimensionalArray), Attr(Foo, F.Int32)))), p => p.DropNonDeclarationAttributes = true);
			Expr("#new(@`#[,]`.[[Foo] int]())",F.Call(S.New, F.Call(F.Of(_(S.TwoDimensionalArray), Attr(Foo, F.Int32)))));

			//int[,] a = null;
			//int[][,] aa = new int[][,] { a };
			//int test = aa[0][1, 2];
		}

		[Test]
		public void PreprocessorConflicts()
		{
			Stmt("@#error(\"FAIL!\");", F.Call(S.Error, F.Literal("FAIL!")));
			Stmt("@#if(c, Foo());",     ExprStyle(F.Call(S.If, c, F.Call(Foo))));
			Stmt("@#region(57);",       ExprStyle(F.Call(GSymbol.Get("#region"), F.Literal(57))));
		}

		// TODO : new expressions: new object() { ... }, new int[] { ... }, new [] { ... }

		//protected static string Lines(params string[] lines)
		//{
		//    return string.Join("\n", lines);
		//}

		//public class A { 
		//    public A() {  } 
		//    public readonly B b;
		//}
		//public class B { public int x; }
		//void Weird() { var gds = new A { b = { x = 1 } }; } // NullReferenceException.

		[Test]
		public void BlocksOfStmts()
		{
			Stmt("{\n  a();\n  b = c;\n}",      F.Braces(F.Call(a), F.Call(S.Set, b, c)));
			Stmt("#{\n  Foo(x);\n  b **= 2\n}", F.List(F.Call(Foo, x), F.Result(F.Call(S.ExpSet, b, two))));
		}

		[Test]
		public void SpaceStmts()
		{
			// Spaces: S.Struct, S.Class, S.Trait, S.Enum, S.Alias, S.Interface, S.Namespace
			var public_x = Attr(@public, F.Var(F.Int32, x));
			Stmt("struct Foo;",        F.Call(S.Struct, Foo, F._Missing));
			Stmt("struct Foo : IFoo;", F.Call(S.Struct, Foo, F.List(IFoo)));
			Stmt("struct Foo\n{\n}",   F.Call(S.Struct, Foo, F._Missing, F.Braces()));
			Stmt("struct Foo\n{\n" +
				"  public int x;\n}",  F.Call(S.Struct, Foo, F._Missing, F.Braces(public_x)));
			Stmt("class Foo : IFoo\n{\n}", F.Call(S.Class, Foo, F.List(IFoo), F.Braces()));
			var a_where = Attr(F.Call(S.Where, @class), a);
			var b_where = Attr(F.Call(S.Where, a), b);
			var stmt = F.Call(S.Class, F.Of(Foo, a_where, b_where), F.List(IFoo), F.Braces());
			Stmt("class Foo<a,b> : IFoo where a: class where b: a\n{\n}", stmt);
			stmt = F.Call(S.Class, F.Of(Foo, Attr(_(S.Out), a_where)), F._Missing, F.Braces());
			Stmt("class Foo<out a> where a: class\n{\n}", stmt);
			stmt = F.Call(S.Class, F.Of(Foo, Attr(_(S.In), T)), F._Missing, F.Braces());
			Stmt("class Foo<in T>\n{\n}", stmt);
			stmt = Attr(F.Call(S.If, F.Call(S.Eq, F.Call(S.Add, a, b), c)),
			            F.Call(S.Trait, Foo, F.List(IFoo), F.Braces(public_x)));
			Stmt("trait Foo : IFoo if a + b == c\n{\n"+
				 "  public int x;\n}", stmt);

			stmt = Attr(F.Call(S.If, F.Call(S.IsLegal, F.Call(S.Add, F.Call(S.Default, T), one))),
			            F.Call(S.Struct, F.Of(Foo, F.Call(S.Substitute, T)), F._Missing));
			Stmt(@"struct Foo<\T> if default(T) + 1 is legal;", stmt);
			Expr(@"[@#if(default(T) + 1 is legal)] #struct(Foo<\T>, @``)", stmt);

			stmt = F.Call(S.Enum, Foo, F.List(F.UInt8), F.Braces(F.Call(S.Set, a, one), b, c, F.Call(S.Set, x, F.Literal(24))));
			Stmt("enum Foo : byte\n{\n  a = 1, b, c, x = 24\n}", stmt);
			Expr("#enum(Foo, #(byte), {\n  a = 1;\n  b;\n  c;\n  x = 24;\n})", stmt);

			stmt = F.Call(S.Interface, F.Of(Foo, Attr(@out, T)), F.List(F.Of(_("IEnumerable"), T)), F.Braces(public_x));
			Stmt("interface Foo<out T> : IEnumerable<T>\n{\n  public int x;\n}", stmt);
			Expr("#interface(Foo.[[#out] T], #(IEnumerable<T>), {\n  public int x;\n})", stmt);

			stmt = F.Call(S.Namespace, F.Of(Foo, T), F._Missing, F.Braces(public_x));
			Stmt("namespace Foo<T>\n{\n  public int x;\n}", stmt);
			Expr("#namespace(Foo<T>, @``, {\n  public int x;\n})", stmt);

			stmt = F.Call(S.Alias, F.Call(S.Set, F.Of(_("Map"), a, b), F.Of(_("Dictionary"), a, b)), F._Missing);
			Stmt("alias Map<a,b> = Dictionary<a,b>;", stmt);
			Expr("#alias(Map<a,b> = Dictionary<a,b>, @``)", stmt);
			stmt = F.Call(S.Alias, F.Call(S.Set, Foo, fooKW), F.List(IFoo), F.Braces(public_x));
			Stmt("alias Foo = #foo : IFoo\n{\n  public int x;\n}", stmt);
			Expr("#alias(Foo = #foo, #(IFoo), {\n  public int x;\n})", stmt);
			
			// An alias must have an #= node as its first argument; other spaces
			// must have type names as their first argument.
			stmt = F.Call(S.Alias, Foo, F.List(IFoo), F.Braces(public_x));
			Stmt("#alias(Foo, #(IFoo), {\n  public int x;\n});", stmt);
			stmt = F.Call(S.Class, F.Call(S.Set, F.Of(_("L"), T), F.Of(_("List"), T)), F._Missing);
			Stmt("#class(L<T> = List<T>, @``);", stmt);
		}

		[Test]
		public void MethodDefinitionStmts()
		{
			// #def and #delegate
			GreenNode int_x = F.Var(F.Int32, x), list_int_x = F.List(int_x), x_mul_x = F.Call(S.Mul, x, x);
			GreenNode stmt;
			stmt = F.Call(S.Delegate, F.Void, F.Of(Foo, T), F.List(F.Var(T, a), F.Var(T, b)));
			Stmt("delegate void Foo<T>(T a, T b);", stmt);
			Expr("#delegate(void, Foo<T>, #(#var(T, a), #var(T, b)))", stmt);
			stmt = F.Call(S.Delegate, F.Void, F.Of(Foo, Attr(F.Call(S.Where, _(S.Class), x), T)), F.List(F.Var(T, x)));
			Stmt("delegate void Foo<T>(T x) where T: class, x;", stmt);
			Expr("#delegate(void, Foo.[[#where(#class, x)] T], #(#var(T, x)))", stmt);
			stmt = Attr(@public, @new, @partial, F.Def(F.String, Foo, list_int_x));
			Stmt("public new partial string Foo(int x);", stmt);
			Expr("[#public, #new, #partial] #def(string, Foo, #(#var(int, x)))", stmt);
			stmt = F.Def(F.Int32, Foo, list_int_x, F.Braces(F.Result(x_mul_x)));
			Stmt("int Foo(int x)\n{\n  x * x\n}", stmt);
			Expr("#def(int, Foo, #(#var(int, x)), {\n  x * x\n})", stmt);
			stmt = F.Def(F.Int32, Foo, list_int_x, F.Braces(F.Call(S.Return, x_mul_x)));
			Stmt("int Foo(int x)\n{\n  return x * x;\n}", stmt);
			Expr("#def(int, Foo, #(#var(int, x)), {\n  return x * x;\n})", stmt);
			stmt = F.Def(F.Decimal, Foo, list_int_x, F.Call(S.Forward, F.Dot(a, b)));
			Stmt("decimal Foo(int x) ==> a.b;", stmt);
			Expr("#def(decimal, Foo, #(#var(int, x)), ==> a.b)", stmt);
			stmt = F.Def(_("IEnumerator"), F.Dot(_("IEnumerable"), _("GetEnumerator")), F.List(), F.Braces());
			Stmt("IEnumerator IEnumerable.GetEnumerator()\n{\n}", stmt);
			Expr("#def(IEnumerator, IEnumerable.GetEnumerator, #(), {\n})", stmt);
			stmt = F.Def(F._Missing, _(S.This), list_int_x, F.Braces(F.Call(_(S.This), x, one), F.Call(S.Set, a, x)));
			Stmt("this(int x) : this(x, 1)\n{\n  a = x;\n}", stmt);
			Expr("#def(@``, this, #(#var(int, x)), {\n  #this(x, 1);\n  a = x;\n})", stmt);
			stmt = F.Def(F._Missing, Foo, list_int_x, F.Braces(F.Call(_(S.Base), x), F.Call(S.Set, b, x)));
			Stmt("Foo(int x) : base(x)\n{\n  b = x;\n}", stmt);
			Expr("#def(@``, Foo, #(#var(int, x)), {\n  base(x);\n  b = x;\n})", stmt);
			stmt = F.Def(F._Missing, F.Call(S._Destruct, Foo), F.List(), F.Braces());
			Stmt("~Foo()\n{\n}", stmt);
			Expr("#def(@``, ~Foo, #(), {\n})", stmt);
			stmt = F.Def(F._Missing, F.Call(S._Negate, Foo), F.List(), F.Braces());
			stmt = F.Call(S.Class, Foo, F.List(), F.Braces(F.Def(F._Missing, F.Call(S._Negate, Foo), F.List(), F.Braces())));
			Stmt("class Foo\n{\n  #def(@``, -Foo, #(), {\n  });\n}", stmt);
			GreenNode @operator = _(S.TriviaUseOperatorKeyword), cast = _(S.Cast), operator_cast = Attr(@operator, cast);
			GreenNode Foo_a = F.Var(Foo, a), Foo_b = F.Var(Foo, b); 
			stmt = Attr(@static, F.Def(F.Bool, Attr(@operator, _(S.Eq)), F.List(F.Var(T, a), F.Var(T, b)), F.Braces()));
			Stmt("static bool operator==(T a, T b)\n{\n}", stmt);
			Expr("static #def(bool, operator==, #(#var(T, a), #var(T, b)), {\n})", stmt);
			stmt = Attr(@static, _(S.Implicit), F.Def(T, operator_cast, F.List(Foo_a), F.Braces()));
			Stmt("static implicit operator T(Foo a)\n{\n}", stmt);
			Expr("static implicit #def(T, operator`#cast`, #(#var(Foo, a)), {\n})", stmt);
			stmt = Attr(@static, _(S.Explicit), 
			            F.Def(F.Of(Foo, T), F.Of(operator_cast, F.Call(S.Substitute, T)), 
			                  F.List(F.Var(F.Of(_("Bar"), T), b))));
			Stmt(@"static explicit Foo<T> operator`#cast`<\T>(Bar<T> b);", stmt);
			Expr(@"static explicit #def(Foo<T>, operator`#cast`<\T>, #(#var(Bar<T>, b)))", stmt);
			stmt = F.Def(F.Bool, Attr(@operator, _("when")), F.List(Foo_a, Foo_b), F.Braces());
			Stmt("bool operator`when`(Foo a, Foo b)\n{\n}", stmt);
			Expr("#def(bool, operator`when`, #(#var(Foo, a), #var(Foo, b)), {\n})", stmt);

			stmt = Attr(F.Call(Foo), @static,
			       F.Def(Attr(Foo, F.Bool), 
			             Attr(@operator, _(S.Neq)),
			             F.List(F.Var(T, a), F.Var(T, b)),
			             F.Braces(F.Result(F.Call(S.Neq, F.Dot(a, x), F.Dot(b, x))))));
			Stmt("[return: Foo] [Foo()] static bool operator!=(T a, T b)\n{\n  a.x != b.x\n}", stmt);
		}

		/// <summary>Tests handling of the constructor ambiguity</summary>
		/// <remarks>
		/// Constructors look like ordinary method calls. In fact, EC# parsing
		/// rules do not allow the parser to look at the argument list to 
		/// determine whether a method is a constructor, and method bodies are
		/// not required on methods. Furthermore, the parser does not 
		/// distinguish between executable and non-executable contexts. So
		/// it's impossible to tell whether
		/// <code>
		/// Foo(x);
		/// </code>
		/// is a method or a constructor. To resolve this conundrum, the parser
		/// keeps track of the name of the current class, for the sole purpose
		/// of detecting the constructor. The printer, meanwhile, must detect
		/// a method call that may be mistaken for a constructor and reformat 
		/// it as <c>##(Foo(x));</c> or <c>(Foo(x))</c> (<c>##(...)</c> is a
		/// special form of parenthesis that does not alter the syntax tree that
		/// the parser produces). Also, when a constructor definition is printed,
		/// the missing return type must be included if the name does not match
		/// an enclosing class:
		/// <code>
		/// @`` Foo(int x) { ... }
		/// </code>
		/// When the constructor is called 'this', this(x) is assumed to be a 
		/// constructor, but that creates a new problem in EC# because you will 
		/// be allowed to call a constructor inside a constructor body:
		/// <code>
		/// this(int x) { this(x, x); }
		/// </code>
		/// This parses successfully because the parser will not allow 
		/// constructor definitions inside methods. The printer, in turn, will
		/// track whether it is in a space definition or not. It can print a
		/// constructor that is directly within a space definition, but in other
		/// contexts will use the @`` notation to ensure that round-tripping 
		/// succeeds. When the syntax tree contains a method call to 'this' 
		/// (which is stored as #this internally, but always printed simply as 
		/// 'this'), it may have to be enclosed in parens to avoid ambiguity.
		/// <para/>
		/// Finally, a constructor with the wrong name can still be parsed if
		/// it calls some other constructor with a colon:
		/// <code>
		/// class Foo { Fub() : base() { } }
		/// </code>
		/// </remarks>
		[Test]
		public void ConstructorAmbiguities()
		{
			var emptyConstructor = F.Def(F._Missing, _(S.This), F.List(), F.Braces());
			Action<EcsNodePrinter> allowAmbig = p => p.AllowConstructorAmbiguity = true;
			Stmt("this()\n{\n}",                 emptyConstructor);
			Stmt("#this(x);",                    F.Call(S.This, x));
			Stmt("base(x);",                     F.Call(S.Base, x));
			Stmt("Foo()\n{\n}",                  F.Def(F._Missing, Foo, F.List(), F.Braces()), allowAmbig);
			Stmt("@`` Foo()\n{\n}",              F.Def(F._Missing, Foo, F.List(), F.Braces()));
			Stmt("this()\n{\n  this()\n  {\n  }\n}",     F.Def(F._Missing, _(S.This), F.List(), F.Braces(emptyConstructor)), allowAmbig);
			Stmt("this() : this(x)\n{\n}",               F.Def(F._Missing, _(S.This), F.List(), F.Braces(F.Call(S.This, x))), allowAmbig);
			Stmt("this()\n{\n  x;\n  this(x);\n}",       F.Def(F._Missing, _(S.This), F.List(), F.Braces(x, F.Call(S.This, x))), allowAmbig);
			Stmt("this()\n{\n  @`` this()\n  {\n  }\n}", F.Def(F._Missing, _(S.This), F.List(), F.Braces(emptyConstructor)));
			Stmt("class Foo\n{\n  Foo();\n}",     F.Call(S.Class, Foo, F.List(), F.Braces(
			                                          F.Def(F._Missing, Foo, F.List()))));
			Stmt("class Foo\n{\n  @`` IFoo();\n}",F.Call(S.Class, Foo, F.List(), F.Braces(
			                                          F.Def(F._Missing, IFoo, F.List()))));
			Stmt("class Foo\n{\n  IFoo() : base()\n  {\n  }\n}", F.Call(S.Class, Foo, F.List(), F.Braces(
			                                          F.Def(F._Missing, IFoo, F.List(), F.Braces(F.Call(S.Base))))));
			Stmt("class Foo\n{\n  Foo();\n}",     F.Call(S.Class, Foo, F.List(), F.Braces(F.Call(Foo))), allowAmbig);
			Stmt("class Foo\n{\n  ##(Foo());\n}", F.Call(S.Class, Foo, F.List(), F.Braces(F.Call(Foo))));
			Stmt("class Foo\n{\n  (Foo());\n}",   F.Call(S.Class, Foo, F.List(), F.Braces(F.Call(Foo))), p => p.AllowExtraParenthesis = true);
			Stmt("class Foo\n{\n  x(Foo());\n}",  F.Call(S.Class, Foo, F.List(), F.Braces(F.Call(x, F.Call(Foo)))));
			// Non-keyword attributes allowed on this() but not Foo() constructor
			Stmt("partial this()\n{\n}",          Attr(partial, F.Def(F._Missing, _(S.This), F.List(), F.Braces())));
			Stmt("class Foo\n{\n  partial this()\n  {\n  }\n}", F.Call(S.Class, Foo, F.List(), F.Braces(
			                                      Attr(partial, F.Def(F._Missing, _(S.This), F.List(), F.Braces())))));
			Stmt("class Foo\n{\n  [#partial] Foo()\n  {\n  }\n}", F.Call(S.Class, Foo, F.List(), F.Braces(
			                                      Attr(partial, F.Def(F._Missing, Foo, F.List(), F.Braces())))));
		}

		LNode AddWords(LNode stmt) { return stmt.PlusAttrs(@public, @new, partial, @static); }

		[Test]
		public void EventStmts()
		{
			GreenNode EventHandler = _("EventHandler"), add = _("add"), remove = _("remove");
			var stmt = F.Call(S.Event, F.Of(EventHandler, T), _("Click"));
			Stmt("event EventHandler<T> Click;", stmt);
			Expr("#event(EventHandler<T>, Click)", stmt);
			stmt = F.Call(S.Event, EventHandler, a, b);
			Stmt("event EventHandler a, b;", stmt);
			Expr("#event(EventHandler, a, b)", stmt);
			stmt = F.Call(S.Event, EventHandler, a, F.Braces(
				Attr(_(S.TriviaMacroCall), F.Call(add, F.Braces())), 
				Attr(_(S.TriviaMacroCall), F.Call(remove, F.Braces()))));
			Stmt("event EventHandler a\n{\n  add {\n  }\n  remove {\n  }\n}", stmt);
			Expr("#event(EventHandler, a, {\n  add {\n  }\n  remove {\n  }\n})", stmt);
			
			// A funky syntax tree causes the printer to revert to prefix notation
			stmt = F.Call(S.Event, F.Call(S.Add, a, b), _("Click"));
			Stmt("#event(a + b, Click);", stmt);
			stmt = F.Call(S.Event, EventHandler, F.Call(S.Add, a, b));
			Expr("#event(EventHandler, a + b)", stmt);
			stmt = F.Call(S.Event, EventHandler, F.Call(S.Add, a, b), F.Braces());
			Expr("#event(EventHandler, a + b, {\n})", stmt);
		}

		[Test]
		public void PropertyStmts()
		{
			GreenNode stmt = F.Property(F.Int32, Foo, F.Braces(get, set));
			Stmt("int Foo\n{\n  get;\n  set;\n}", stmt);
			Expr("#property(int, Foo, {\n  get;\n  set;\n})", stmt);
			stmt = Attr(@public, F.Property(F.Int32, Foo, F.Braces(
			                       Attr(trivia_macroCall, F.Call(get, F.Braces(F.Call(S.Return, x)))),
			                       Attr(trivia_macroCall, F.Call(set, F.Braces(F.Call(S.Set, x, value)))))));
			Stmt("public int Foo\n{\n"
			      +"  get {\n    return x;\n  }\n"
			      +"  set {\n    x = value;\n  }\n}", stmt);

			stmt = F.Property(F.Int32, Foo, F.Call(S.Forward, x));
			Stmt("int Foo ==> x;", stmt);

			stmt = F.Property(F.Int32, Foo, F.Call(S.Forward, F.List(a, b)));
			Stmt("int Foo ==> #(a, b);", stmt);

			stmt = F.Property(F.Int32, Foo, F.Braces(
			                  Attr(trivia_forwardedProperty, F.Call(get, F.Call(S.Forward, x)))));
			Stmt("int Foo\n{\n  get ==> x;\n}", stmt);
			stmt = F.Property(F.Int32, Foo, F.Braces(
			                  Attr(trivia_forwardedProperty, F.Call(get, F.Call(S.Forward, a, b)))));
			Stmt("int Foo\n{\n  get(#==>(a, b));\n}", stmt);
		}

		[Test]
		public void SimpleStmts()
		{
			// S.Break, S.Continue, S.Goto, S.GotoCase, S.Return, S.Throw
			Stmt("break;",             F.Call(S.Break));
			Stmt("break outer;",       F.Call(S.Break, _("outer")));
			Stmt("continue;",          F.Call(S.Continue));
			Stmt("continue outer;",    F.Call(S.Continue, _("outer")));
			Stmt("goto end;",          F.Call(S.Goto, _("end")));
			Stmt("goto case 1;",       F.Call(S.GotoCase, one));
			Stmt("goto case [Foo] 1;", F.Call(S.GotoCase, Attr(Foo, one)));
			Stmt("return;",            F.Call(S.Return));
			Stmt("return 1;",          F.Call(S.Return, one));
			Stmt("return void;",       F.Call(S.Return, F.Literal(@void.Value)));
			Stmt("throw;",             F.Call(S.Throw));
			Stmt("throw new Foo();",   F.Call(S.Throw, F.Call(S.New, F.Call(Foo))));
			Stmt("#break;",            _(S.Break));
			Stmt("#continue;",         _(S.Continue));
			Stmt("#return;",           _(S.Return));
			Stmt("#throw;",            _(S.Throw));
		}

		[Test]
		public void BlockStmts()
		{
			// S.If, S.Checked, S.Do, S.Fixed, S.For, S.ForEach, S.If, S.Lock, 
			// S.Switch, S.Try, S.Unchecked, S.UsingStmt, S.While
			Stmt("if (Foo)\n  a();",                    F.Call(S.If, Foo, F.Call(a)));
			Stmt("if (Foo)\n  a();\nelse\n  b();",      F.Call(S.If, Foo, F.Call(a), F.Call(b)));
			var ifStmt = F.Call(S.If, Foo, F.Result(a), F.Result(b));
			Stmt("{\n  if (Foo)\n    a\n  else\n    b\n}", F.Braces(ifStmt));
			Stmt("{\n  if (Foo)\n    #result(a);\n  else\n    #result(b);\n  c;\n}", F.Braces(ifStmt, c));
			ifStmt = F.Call(S.If, Foo, F.Call(a), F.Call(S.If, x, F.Braces(F.Call(b)), F.Call(c)));
			Stmt("if (Foo)\n  a();\nelse if (x) {\n  b();\n} else\n  c();", ifStmt);

			Stmt("checked {\n  x = a();\n  x * x\n}",   F.Call(S.Checked, F.Braces(F.Call(S.Set, x, F.Call(a)), 
			                                                                 F.Result(F.Call(S.Mul, x, x)))));
			Stmt("unchecked {\n  0xbaad * 0xf00d\n}",   F.Call(S.Unchecked, F.Braces(F.Result(
			                                                   F.Call(S.Mul, Alternate(F.Literal(0xBAAD)), Alternate(F.Literal(0xF00D)))))));
			
			Stmt("do\n  a();\nwhile (c);",              F.Call(S.Do, F.Call(a), c));
			Stmt("do #{\n  a();\n} while (c);",         F.Call(S.Do, F.List(F.Call(a)), c));
			Stmt("do {\n  a();\n} while (c);",          F.Call(S.Do, F.Braces(F.Call(a)), c));
			Stmt("do {\n  a\n} while (c);",             F.Call(S.Do, F.Braces(F.Result(a)), c));
			
			var amp_b_c = F.Call(S._AddressOf, F.Call(S.PtrArrow, b, c));
			var int_a_amp_b_c = F.Var(F.Of(_(S._Pointer), F.Int32), F.Call(a, amp_b_c));
			Stmt("fixed (int* a = &b->c)\n  Foo(a);",    F.Call(S.Fixed, int_a_amp_b_c, F.Call(Foo, a)));
			Stmt("fixed (int* a = &b->c) {\n  Foo(a);\n}", F.Call(S.Fixed, int_a_amp_b_c, F.Braces(F.Call(Foo, a))));
			var stmt = F.Call(S.Fixed, F.Var(F.Of(_(S._Pointer), F.Int32), F.Call(x, F.Call(S._AddressOf, Foo))), F.Call(a, x));
			Stmt("fixed (int* x = &Foo)\n  a(x);", stmt);
			
			var forArgs = new LNode[] {
				F.Var(F.Int32, F.Call(x, F.Literal(0))),
				F.Call(S.LT, x, F.Literal(10)),
				F.Call(S.PostInc, x),
				F.Braces()
			};
			Stmt("for (int x = 0; x < 10; x++) {\n}",   F.Call(S.For, forArgs));
			forArgs[3] = F.List(F.Call(a), F.Call(b));
			Stmt("for (int x = 0; x < 10; x++) #{\n  a();\n  b();\n}", F.Call(S.For, forArgs));
			forArgs[3] = F.List(F.Result(a));
			Stmt("for (int x = 0; x < 10; x++) #{\n  a\n}", F.Call(S.For, forArgs));

			stmt = F.Call(S.ForEach, F.Var(F._Missing, x), Foo, F.Call(a, x));
			Stmt("foreach (var x in Foo)\n  a(x);", stmt);
			stmt = F.Call(S.ForEach, F.Call(S.Add, a, b), c, F.Braces());
			Stmt("foreach (a + b in c) {\n}", stmt);
			stmt = F.Call(S.ForEach, F.Call(S.Set, a, x), F.Call(S.Set, b, x), F.List());
			Stmt("foreach (a `#=` x in b = x) #{\n}", stmt);

			stmt = F.Call(S.While, F.Call(S.GT, x, one), F.Call(S.PostDec, x));
			Stmt("while (x > 1)\n  x--;", stmt);
			stmt = F.Call(S.UsingStmt, F.Var(F._Missing, F.Call(x, F.Call(S.New, F.Call(Foo)))), F.Call(F.Dot(x, a)));
			Stmt("using (var x = new Foo())\n  x.a();", stmt);
			stmt = F.Call(S.Lock, Foo, F.Braces(F.Call(F.Dot(Foo, Foo))));
			Stmt("lock (Foo) {\n  Foo.Foo();\n}", stmt);

			stmt = F.Call(S.Try, F.Call(Foo), F.Call(S.Catch, F._Missing, F.Braces()));
			Stmt("try\n  Foo();\ncatch {\n}", stmt);
			stmt = F.Call(S.Try, F.Call(Foo), F.Call(S.Catch, F.Var(_("Exception"), x), F.Braces(F.Call(S.Throw))), F.Call(S.Finally, F.Call(_("hi_mom"))));
			Stmt("try\n  Foo();\n"+
				 "catch (Exception x) {\n  throw;\n"+
				 "} finally\n  hi_mom();", stmt);
		}

		[Test]
		public void Switch()
		{
			var stmt = F.Call(S.Switch, x, F.Braces(
				F.Call(S.Case, F.Literal(1)),
				F.Call(S.Case, F.Literal(2)),
				F.Call(S.GotoCase, F.Literal(3)),
				F.Call(S.Case, F.Literal(3), F.Literal(4)),
				F.Call(S.Break),
				F.Call(S.Label, F.Id(S.Default)),
				F.Call(S.Break)));
			Stmt("switch (x) {\n"+
				"case 1:\ncase 2:\n  goto case 3;\n"+
				"case 3, 4:\n  break;\n"+
				"default:\n  break;\n}", stmt);
		}

		[Test]
		public void Missing()
		{
			Stmt(";", F._Missing);
			Action<EcsNodePrinter> oma = o => o.OmitMissingArguments = true;
			Stmt("Foo(@``);", F.Call(Foo, F._Missing), oma);
			Stmt("Foo(@``, b);", F.Call(Foo, F._Missing, b));
			Stmt("Foo(, b);", F.Call(Foo, F._Missing, b), oma);
			Stmt("Foo(a,);", F.Call(Foo, a, F._Missing), oma);
			Stmt("Foo(,);", F.Call(Foo, F._Missing, F._Missing), oma);
			Stmt("for (;;) {\n  a();\n}", F.Call(S.For, F._Missing, F._Missing, F._Missing, F.Braces(F.Call(a))));
			Stmt("for (;; @``())\n  ;", F.Call(S.For, F._Missing, F._Missing, F.Call(F._Missing), F._Missing));
		}

		[Test]
		public void StmtsWithAttributes()
		{
			GreenNode[] args = new GreenNode[4] { Foo, fooKW, @public, null };
			args[3] = F.Call(S.Struct, Foo, F._Missing, F.Braces(F.Var(F.String, x)));
			Stmt("[Foo] foo public struct Foo\n{\n  string x;\n}", Attr(args));
			args[3] = F.Def(F.String, Foo, F.List(), F.Braces(F.Result(x)));
			Stmt("[Foo] foo public string Foo()\n{\n  x\n}", Attr(args));
			args[3] = F.Call(S.Break);
			Stmt("[Foo] foo public break;", Attr(args));
			args[3] = F.Call(S.GotoCase, x);
			Stmt("[Foo] foo public goto case x;", Attr(args));
			args[3] = F.Call(S.Return, one);
			Stmt("[Foo] foo public return 1;", Attr(args));
			args[3] = F.Call(S.Unchecked, F.Braces(F.Call(S.Set, a, F.Call(S.Shl, b, c))));
			Stmt("[Foo] foo public unchecked {\n  a = b << c;\n}", Attr(args));
			args[3] = F.Call(S.If, F.Call(S.Eq, a, b), F.Call(c));
			Stmt("[Foo, #foo] public if (a == b)\n  c();", Attr(args));
			args[3] = F.Call(S.Do, F.Call(a), c);
			Stmt("[Foo] foo public do\n  a();\nwhile (c);", Attr(args));
			args[3] = F.Call(S.UsingStmt, Foo, F.Braces(F.Call(a, Foo)));
			Stmt("[Foo] foo public using (Foo) {\n  a(Foo);\n}", Attr(args));
			args[3] = F.Call(S.For, a, b, c, x);
			Stmt("[Foo] foo public for (a; b; c)\n  x;", Attr(args));
			args[3] = F.Braces(F.Call(a));
			Stmt("[Foo, #foo] public {\n  a();\n}", Attr(args));
			args[3] = AsStyle(F.List(F.Call(a)), NodeStyle.Statement);
			Stmt("[Foo, #foo] public #{\n  a();\n}", Attr(args));
		}

		[Test]
		public void CommentTrivia()
		{
			var stmt = Attr(F.Trivia(S.TriviaMLCommentBefore, "bx"), F.Trivia(S.TriviaMLCommentAfter, "ax"), x);
			Stmt("/*bx*/x; /*ax*/",   stmt);
			Expr("/*bx*/x /*ax*/",    stmt);
			Stmt("x;",               stmt, p => p.OmitComments = true);
			stmt = Attr(F.Trivia(S.TriviaSLCommentBefore, "bx"), F.Trivia(S.TriviaSpaceAfter, "\t\t"), F.Trivia(S.TriviaSLCommentAfter, "ax"), x);
			Stmt("//bx\nx;\t\t//ax", stmt);
			Expr("//bx\nx //ax",     stmt, p => p.OmitSpaceTrivia = true);
			Expr("//bx\nx\t\t//ax",  stmt);
			Stmt("//bx\nx; //ax",    stmt, p => p.OmitSpaceTrivia = true);
			Stmt("x;\t\t",           stmt, p => p.OmitComments = true);
			stmt = 
				Attr(F.Trivia(S.TriviaSLCommentBefore, " a block"), 
					F.Trivia(S.TriviaSLCommentAfter, " end of block"), F.Braces(
					Attr(F.Trivia(S.TriviaSLCommentBefore, " set x to zero"),
						F.Trivia(S.TriviaSpaceAfter, "  "),
						F.Trivia(S.TriviaSLCommentAfter, " x was set to zero"),
						F.Call(S.Set, Attr(F.Trivia(S.TriviaMLCommentAfter, "the variable"), x),
									  Attr(F.Trivia(S.TriviaMLCommentAfter, "its new value"), zero)
					))));
			Stmt("// a block\n{\n  // set x to zero\n  x /*the variable*/= 0 /*its new value*/;  // x was set to zero\n} // end of block", stmt);
			stmt = Attr(F.Trivia(S.TriviaRawTextBefore, "Eat my shorts!"), 
				F.Trivia(S.TriviaRawTextAfter, "...then do it again!"), F._Missing);
			Stmt("Eat my shorts!;...then do it again!", stmt);
			stmt = Attr(F.Trivia(S.TriviaRawTextAfter, " // end if"), F.Call(S.If, a, F.Call(x)));
			Stmt("if (a)\n  x(); // end if", stmt);
			Stmt("if (a)\n  x();", stmt, p => p.OmitRawText = true);
			stmt = Attr(F.Trivia(S.TriviaSLCommentAfter, " leave loop"), F.Call(S.Break));
			Stmt("break; // leave loop", stmt);
			
			var raw = F.Trivia(S.RawText, "hello!");
			Stmt("x(hello!);", F.Call(x, raw));
		}

		[Test]
		public void BraceInIfClause()
		{
			// A braced block is not allowed inside an "if" clause. However we 
			// don't have a good way to prevent it.
			var stmt = Attr(F.Call(S.If, F.Call(S.Eq, a, F.Braces(b))),
			                F.Call(S.Namespace, Foo, F._Missing));
			Stmt("namespace Foo if a == #{}(b);", stmt);
			stmt = Attr(F.Call(S.If, F.Call(S.Eq, a, StmtStyle(F.List(b)))),
			            F.Call(S.Class, Foo));
			Stmt("class Foo if a == #(b);", stmt);
		}

		[Test]
		public void DanglingElseAmbiguity()
		{
			// if (a)
			//    if (b)
			//       c();
			// else
			//    x();
			var stmt = F.Call(S.If, a, F.Call(S.If, b, F.Call(c)), F.Call(x));
			Stmt("if (a)\n  #if(b, c());\nelse\n  x();", stmt);
			Stmt("if (a) {\n  if (b)\n    c();\n} else\n  x();", stmt, p => p.AllowExtraParenthesis = true);
		}

		[Test]
		public void StaticMethods()
		{
			AreEqual("@this",            EcsNodePrinter.PrintIdent(GSymbol.Get("this"), false));
			AreEqual("normal_id",        EcsNodePrinter.PrintIdent(GSymbol.Get("normal_id"), false));
			AreEqual("operator+",        EcsNodePrinter.PrintIdent(GSymbol.Get("#+"), true));
			AreEqual("operator`frack!`", EcsNodePrinter.PrintIdent(GSymbol.Get("frack!"), true));
			AreEqual("$`frack!`",        EcsNodePrinter.PrintSymbolLiteral(GSymbol.Get("frack!")));
			AreEqual("$this",            EcsNodePrinter.PrintSymbolLiteral(GSymbol.Get("this")));
		}

		/// <summary>Demonstrates where word attributes are allowed and where they are not allowed.</summary>
		/// <remarks>
		/// Reasons for disallowing non-keyword attributes (known as "word attributes"):
		/// <para/>
		/// - On expressions, consider: "partial X();"
		///   Ambiguity: is this a method declaration, or a method call X() with an attribute?
		/// - On expressions, consider: "partial x = 0;"
		///   Ambiguity: is this a variable declaration, or an assignment "x = 0" with an attribute?
		/// - On the "if" statement, consider "Foo X if (x) { get; }":
		///   Ambiguity: is it a conditionally-defined property or a regular "if" statement?
		/// - On constructors, consider "partial X() {}"
		///   Ambiguity: is this a constructor or a method that returns type "partial"?
		///   However: we can allow word attributes on a new-style constructor named "this"
		/// - On forwarded accessors, consider "foo get ==> X;"
		///   Ambiguity: is this a property forwarded to X with return type "foo", or is it
		///   a getter forwarded to X with "foo" as a word attribute?
		/// <para/>
		/// Reasons for disallowing "new":
		/// <para/>
		/// - On expressions, consider: <c>new Foo();</c> or <c>new Foo[10] = x;</c>
		///   Ambiguity: does this create a new Foo, or is it just a call to method Foo with "new" as an attribute?
		///   Ambiguity: does this create a new array, or is it just a call to an indexer on the variable Foo?
		/// <para/>
		/// Word attributes should be allowed on "return", to allow "yield return".
		/// </remarks>
		[Test]
		public void WordAttributes()
		{
			Stmt("public new partial static void Main()\n{\n}",   AddWords(F.Def(F.Void, F.Id("Main"), F.List(), F.Braces())));
			Stmt("public new partial static void Main();",        AddWords(F.Def(F.Void, F.Id("Main"), F.List())));
			Stmt("class Foo\n{\n  [#partial] Foo();\n}",          F.Call(S.Class, Foo, F.List(), F.Braces(
			                                                          Attr(partial, F.Def(F._Missing, Foo, F.List())))));
			Stmt("class Foo\n{\n  partial this();\n}",            F.Call(S.Class, Foo, F.List(), F.Braces(
			                                                          Attr(partial, F.Def(F._Missing, F.Id(S.This), F.List())))));
			Stmt("public new partial static this();",             AddWords(F.Def(F._Missing, F.Id(S.This), F.List())));
			Stmt("[#public, #new] partial static break;",         AddWords(F.Call(S.Break)));
			Stmt("[#public, #new] partial static return x;",      AddWords(F.Call(S.Return, x)));
			Stmt("[#public, #new] partial static goto case x;",   AddWords(F.Call(S.GotoCase, x)));
			Stmt("[#public, #new, #partial] static if (Foo)\n  Foo();", AddWords(F.Call(S.If, Foo, F.Call(Foo))));
			Stmt("[#public, #new] partial static try {\n} catch {\n}",  AddWords(F.Call(S.Try, F.Braces(), F.Call(S.Catch, F._Missing, F.Braces()))));
			Stmt("[#public, #new] partial static while (x)\n  Foo();",  AddWords(F.Call(S.While, x, F.Call(Foo))));
			Stmt("[#public, #new, #partial] static new Foo();",   AddWords(F.Call(S.New, F.Call(Foo))));
			Stmt("[#public, #new, #partial] static x = 0;",       AddWords(F.Call(S.Set, x, zero)));
			Stmt("[#public, #new, #partial] static Foo(x = 0);",  AddWords(F.Call(Foo, F.Call(S.Set, x, zero))));
			Stmt("[#public, #new, #partial] static Foo(x = 0);",  AddWords(F.Call(Foo, F.Call(S.Set, x, zero))));
			Stmt("public new partial static int x;",              AddWords(F.Var(F.Int32, x)));
			Stmt("public new partial static int x\n{\n  get;\n}", AddWords(F.Property(F.Int32, x, F.Braces(get))));
			Stmt("public new partial static interface Foo\n{\n}", AddWords(F.Call(S.Interface, Foo, F.List(), F.Braces())));
			Stmt("public new partial static delegate void x();",  AddWords(F.Call(S.Delegate, F.Void, x, F.List())));
			Stmt("public new partial static alias a = Foo;",      AddWords(F.Call(S.Alias, F.Call(S.Set, a, Foo), F.List())));
			Stmt("public new partial static event Foo x;",        AddWords(F.Call(S.Event, Foo, x)));
			Stmt("[#public, #new] partial static Foo:",           AddWords(F.Call(S.Label, Foo)));
			Stmt("public new partial static Foo a ==> b;",        AddWords(F.Property(Foo, a, F.Call(S.Forward, b))));
			Stmt("[#public, #new, #partial] static get ==> b;",   AddWords(Attr(trivia_forwardedProperty, F.Call(get, F.Call(S.Forward, b)))));
			Stmt("[#public, #new, #partial] static ;",            AddWords(F._Missing));
			Stmt("Foo([#public, #new, #partial] static x);",      F.Call(Foo, AddWords(x)));
			Stmt("Foo(public new partial static int x = 0);",     F.Call(Foo, AddWords(F.Var(F.Int32, F.Call(x, zero)))));
		}

		[Test]
		public void Regressions()
		{
			Stmt("\"Hello\";", F.Literal("Hello")); // bug: was handled as an empty statement because Name.Name=="" for a literal
		}
	}
}
