using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;
using Loyc;
using Loyc.Syntax;
using S = Loyc.Syntax.CodeSymbols;
using Ecs.Parser;
using Loyc.Syntax.Lexing;
using Loyc.Collections;

namespace Ecs
{
	// Tests shared between the printer and the parser. Both tests together verify 
	// round-tripping from AST -> text -> AST, although the other kind of round-
	// tripping, text -> AST -> text, is not fully verified (and is not designed to
	// be fully supported, as the printer is not designed to preserve spacing.)
	abstract class EcsPrinterAndParserTests : Assert
	{
		protected static LNodeFactory F = new LNodeFactory(EmptySourceFile.Unknown);
		protected LNode a = F.Id("a"), b = F.Id("b"), c = F.Id("c"), x = F.Id("x");
		protected LNode Foo = F.Id("Foo"), IFoo = F.Id("IFoo"), T = F.Id("T");
		protected LNode zero = F.Literal(0), one = F.Literal(1), two = F.Literal(2);
		protected LNode @class = F.Id(S.Class), @partial = F.Id(S.Partial);
		protected LNode partialWA = F.Attr(F.Id(S.TriviaWordAttribute), F.Id(S.Partial));
		protected LNode @public = F.Id(S.Public), @static = F.Id(S.Static);
		protected LNode fooKW = F.Id("#foo"), fooWA = F.Attr(F.Id(S.TriviaWordAttribute), F.Id("#foo"));
		protected LNode @lock = F.Id(S.Lock), @if = F.Id(S.If);
		protected LNode @out = F.Id(S.Out), @ref = F.Id(S.Ref), @new = F.Id(S.New);
		protected LNode trivia_forwardedProperty = F.Id(S.TriviaForwardedProperty);
		protected LNode get = F.Id("get"), set = F.Id("set"), value = F.Id("value");
		protected LNode _(string name) { return F.Id(name); }
		protected LNode _(Symbol name) { return F.Id(name); }

		// Allows a particular test to exclude the printer or the parser
		protected enum Mode { 
			Both, PrintOnly, ParseOnly, 
			PrintBothParseFirst // for Option()
		};
		
		// The tests were originally designed for printer tests, so they take 
		// an Action<EcsNodePrinter> lambda. But the parser accepts no special 
		// configuration, so EcsParserTests will just ignore the lambda.
		protected abstract void Stmt(string text, LNode code, Action<EcsNodePrinter> configure = null, bool exprMode = false, Mode mode = Mode.Both);

		protected void Expr(string text, LNode code, Mode mode)
		{
			Stmt(text, code, null, true, mode);
		}
		protected void Expr(string text, LNode code, Action<EcsNodePrinter> configure = null, Mode mode = Mode.Both)
		{
			Stmt(text, code, configure, true, mode);
		}
		protected void Stmt(string text, LNode code, Mode mode)
		{
			Stmt(text, code, null, false, mode);
		}
		protected void Option(Mode mode, string before, string after, LNode code, Action<EcsNodePrinter> configure = null, bool exprMode = false)
		{
			Stmt(before, code, null,     exprMode, mode == Mode.PrintBothParseFirst ? Mode.Both      : mode);
			Stmt(after, code, configure, exprMode, mode == Mode.PrintBothParseFirst ? Mode.PrintOnly : mode);
		}

		[Test]
		public void SimpleAtoms()
		{
			Expr("Foo",      Foo);
			Expr("1024",     F.Literal(1024));
			Expr("0.5",      F.Literal(0.5));
			Expr("'$'",      F.Literal('$'));
			Expr(@"""hi""",  F.Literal("hi"));
			Expr("null",     F.Literal(null));
			Expr("true",     F.Literal(true));
			Expr(@"@@hello", F.Literal(GSymbol.Get("hello")));
		}

		[Test]
		public void SimpleInfixOperators()
		{
			Expr("a",                a);
			Expr("a + b",            F.Call(S.Add, a, b));
			Expr("a + b + c",        F.Call(S.Add, F.Call(S.Add, a, b), c));
			Expr("a**b**c",          F.Call(S.Exp, F.Call(S.Exp, a, b), c));
			Expr("a * b / c % 2",    F.Call(S.Mod, F.Call(S.Div, F.Call(S.Mul, a, b), c), two));
			Expr("a + b**c - 1",     F.Call(S.Sub, F.Call(S.Add, a, F.Call(S.Exp, b, c)), one));
			Expr("a..b",             F.Call(S.DotDot, a, b));
			Expr("a >= b && a <= c", F.Call(S.And, F.Call(S.GE, a, b), F.Call(S.LE, a, c)));
			Expr("a > b || a < c",   F.Call(S.Or, F.Call(S.GT, a, b), F.Call(S.LT, a, c)));
			Expr("a == b ^^ a != c", F.Call(S.Xor, F.Call(S.Eq, a, b), F.Call(S.Neq, a, c)));
			Expr("a & b | c ^ 1",    F.Call(S.OrBits, F.Call(S.AndBits, a, b), F.Call(S.XorBits, c, one)));
			Expr("a = b ?? c",       F.Call(S.Assign, a, F.Call(S.NullCoalesce, b, c)));
			Expr("a += b ~ c",       F.Call(S.AddSet, a, F.Call(S.NotBits, b, c)));
			Expr("a >>= b <<= c",    F.Call(S.ShrSet, a, F.Call(S.ShlSet, b, c)));
			Expr("a.b - a::b",       F.Call(S.Sub, F.Call(S.Dot, a, b), F.Call(S.ColonColon, a, b)));
			Expr("a. 2",             F.Dot(a, two));
			Expr("a::b.c. 2",        F.Dot(F.Call(S.ColonColon, a, b), c, two));
			Expr("a?.b",             F.Call(S.NullDot, a, b));
			Expr("1.a?.b.c",         F.Call(S.NullDot, F.Dot(one, a), F.Dot(b, c)));
		}

		[Test]
		public void PrefixOperators()
		{
			Expr("-a",     F.Call(S._Negate, a));
			Expr("+a",     F.Call(S._UnaryPlus, a));
			Expr("~a",     F.Call(S.NotBits, a));
			Expr("!a",     F.Call(S.Not, a));
			Expr("++a",    F.Call(S.PreInc, a));
			Expr("--a",    F.Call(S.PreDec, a));
			Expr("*a",     F.Call(S._Dereference, a));
			Expr("&a",     F.Call(S._AddressOf, a));
			Expr("$a",     F.Call(S.Substitute, a));
			Expr("$(-$a)", F.Call(S.Substitute, F.Call(S._Negate, F.Call(S.Substitute, a))));
			Expr("**a",    F.Call(S._Dereference, F.Call(S._Dereference, a)));
			Expr(".(-a)",  F.Call(S.Dot, F.Call(S._Negate, a)));
		}
		
		[Test]
		public void MiscOperators()
		{
			Expr("a << 1 | b >> 1",     F.Call(S.OrBits, F.Call(S.Shl, a, one), F.Call(S.Shr, b, one)));
			Expr("a++ + a--",           F.Call(S.Add, F.Call(S.PostInc, a), F.Call(S.PostDec, a)));
			Expr("a ? b : c",           F.Call(S.QuestionMark, a, b, c));
			Expr("a => a + 1",          F.Call(S.Lambda, a, F.Call(S.Add, a, one)));
			Expr("1 + a => 2 + b => c", F.Call(S.Add, one, F.Call(S.Lambda, a, F.Call(S.Add, two, F.Call(S.Lambda, b, c)))));
			Expr("a is Foo ? a as Foo : b", F.Call(S.QuestionMark, F.Call(S.Is, a, Foo), F.Call(S.As, a, Foo), b));
			Expr("a ??= b using Foo",   F.Call(S.NullCoalesceSet, a, F.Call(S.UsingCast, b, Foo)));
		}

		[Test]
		public void SimpleCallsAndTypeParams()
		{
			Expr("a", a);
			Expr("(a)",      F.InParens(a));
			Expr("((a))",    F.InParens(F.InParens(a)));
			Expr("#public",  @public);
			Expr("@public",  _("public"));
			Expr("a(b)",     F.Call(a, b));
			Expr("a.b.c",    F.Dot(a, b, c));
			Expr("a.b(c)",   F.Call(F.Dot(a, b), c));
			Expr("a<b>.c",   F.Dot(F.Of(a, b), c));
			Expr("a.b<c>",   F.Of(F.Dot(a, b), c));
			Expr("a<b.c>",   F.Of(a, F.Dot(b, c)));
			Expr("a<b,c>",   F.Of(a, b, c));
			Expr("a<b>(c)",  F.Call(F.Of(a, b), c));
			Expr("a().b<c>", F.Of(F.Dot(F.Call(a), b), c));
		}

		[Test]
		public void SimpleExprStatements()
		{
			// These are similar/identical to above expressions, but parsed/printed 
			// as statements. This test ensures that statement-related logic (such 
			// as the rule against printing "a * b" as a statement) does not 
			// interfere with expressions that can be printed as statements.
			Stmt("a;",        a);
			Stmt("(a);",      F.InParens(a));
			Stmt("((a));",    F.InParens(F.InParens(a)));
			Stmt("a(b);",     F.Call(a, b));
			Stmt("a.b.c;",    F.Dot(a, b, c));
			Stmt("a.b(c);",   F.Call(F.Dot(a, b), c));
			Stmt("a<b>.c;",   F.Dot(F.Of(a, b), c));
			Stmt("a.b<c>;",   F.Of(F.Dot(a, b), c));
			Stmt("a<b,c>;",   F.Of(a, b, c));
			Stmt("a<b>(c);",  F.Call(F.Of(a, b), c));
			Stmt("a?.b(c);",   F.Call(S.NullDot, a, F.Call(b, c)));
			Stmt("a + b + c;",           F.Call(S.Add, F.Call(S.Add, a, b), c));
			// To be safe, the printer treats 'a * b' like a pointer decl so it won't print it
			Stmt("@`*`(a, b) / c % 2;",  F.Call(S.Mod, F.Call(S.Div, F.Call(S.Mul, a, b), c), two));
			Stmt("a * b / c % 2;",       F.Call(S.Mod, F.Call(S.Div, F.Call(S.Mul, a, b), c), two), Mode.ParseOnly);
			Stmt("a / b * c % 2;",       F.Call(S.Mod, F.Call(S.Mul, F.Call(S.Div, a, b), c), two));
			Stmt("a `*` b ? c : 0;",     F.Call(S.QuestionMark, F.Call(S.Mul, a, b), c, zero));
			Stmt("a * b ? c : 0;",       F.Call(S.QuestionMark, F.Call(S.Mul, a, b), c, zero), Mode.ParseOnly);
			Stmt("a << 1 | b >> 1;",     F.Call(S.OrBits, F.Call(S.Shl, a, one), F.Call(S.Shr, b, one)));
			Stmt("a++ + a--;",           F.Call(S.Add, F.Call(S.PostInc, a), F.Call(S.PostDec, a)));
			Stmt("a ? b : c;",           F.Call(S.QuestionMark, a, b, c));
			Stmt("a => a + 1;",          F.Call(S.Lambda, a, F.Call(S.Add, a, one)));
			Stmt("1 + a => 2 + b => c;", F.Call(S.Add, one, F.Call(S.Lambda, a, F.Call(S.Add, two, F.Call(S.Lambda, b, c)))));
			Stmt("a is Foo ? a as Foo : b;", F.Call(S.QuestionMark, F.Call(S.Is, a, Foo), F.Call(S.As, a, Foo), b));
			Stmt("a ??= b using Foo;",   F.Call(S.NullCoalesceSet, a, F.Call(S.UsingCast, b, Foo)));
			Stmt("(a + b).b<c>;", F.Of(F.Dot(F.InParens(F.Call(S.Add, a, b)), b), c));
		}

		[Test]
		public void SimpleVarDecls()
		{
			Stmt("Foo a;",   F.Vars(Foo, a));
			Stmt("Foo.x a;", F.Vars(F.Dot(Foo, x), a));
			Stmt("int a;",   F.Vars(F.Int32, a));
			Stmt("int[] a;", F.Vars(F.Of(S.Bracks, S.Int32), a));
			Stmt("var a;",   F.Vars(_(S.Missing), a));
			Stmt("@var a;",  F.Vars(_("var"), a));
			Stmt(@"$Foo x;", F.Vars(F.Call(S.Substitute, Foo), x));
			Stmt(@"$(a(b)) x;", F.Vars(F.Call(S.Substitute, F.Call(a, b)), x));
			Stmt("Foo a, b = c;", F.Vars(Foo, a, F.Call(S.Assign, b, c)));
		}

		protected LNode Attr(LNode attr, LNode node)
		{
			return node.WithAttrs(node.Attrs.Insert(0, attr));
		}
		protected LNode Attr(params LNode[] attrsAndNode)
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
			Stmt("@`.`([Foo] a, b).c;",   F.Dot(Attr(Foo, a), b, c));
			Stmt("@`.`(a, [Foo] b).c;",   F.Dot(a, Attr(Foo, b), c));
			Stmt("@`.`(a.b, [Foo] c);",   F.Dot(a, b, Attr(Foo, c)));
			Stmt("@`.`([Foo] a, b, c);",  F.Call(S.Dot, Attr(Foo, a), b, c));
			Stmt("@`.`(a, b, [Foo] c);",  F.Call(S.Dot, a, b, Attr(Foo, c)));
			Stmt("#of([Foo] a, b, c);",   F.Of(Attr(Foo, a), b, c));
			Stmt("a!(b,[Foo] c);",        F.Of(a, b, Attr(Foo, c)));
			Stmt("a!(b,Foo + c);",        F.Of(a, b, F.Call(S.Add, Foo, c)));
			Stmt("#of(Foo<a>, b);",       F.Of(F.Of(Foo, a), b));
			Stmt("public a;",             Attr(@public, a));
			Stmt("[Foo] public a(b);",    Attr(Foo, Attr(@public, F.Call(a, b))));
			Stmt("[Foo] static int a;",   Attr(Foo, Attr(@static, F.Vars(F.Int32, a))));
			Stmt("partial public int a;", Attr(partialWA, Attr(@public, F.Vars(F.Int32, a))));
			Stmt("[#lock] static int a;", Attr(@lock, Attr(@static, F.Vars(F.Int32, a))));
			Stmt("[#public, Foo] int a;", Attr(@public, Attr(Foo, F.Vars(F.Int32, a))));
			Stmt("public #foo;",          Attr(@public, fooKW));
		}

		[Test]
		public void ParsingChallenges() // See also: ParserOnlyTests()
		{
			Stmt("Foo? a = 0, b;",        F.Vars(F.Of(_(S.QuestionMark), Foo), F.Call(S.Assign, a, zero), b));
			Stmt("Foo? a = b ? c : null;", F.Var(F.Of(_(S.QuestionMark), Foo), F.Call(S.Assign, a,
			                              F.Call(S.QuestionMark, b, c, F.Literal(null)))));
			// Note: To simplify the parser, EC# cannot parse the standalone 
			//       statement "Foo ? a = 0 : b;" - you must use parentheses.
			Stmt("(Foo? a = 0, b);",      F.Tuple(F.Var(F.Of(_(S.QuestionMark), Foo), F.Call(S.Assign, a, zero)), b));
			Stmt("(Foo ? a = 0 : b);",    F.InParens(F.Call(S.QuestionMark, Foo, F.Call(S.Assign, a, zero), b)));
			Stmt("(Foo? a = b ? c : null);", F.InParens(
				F.Vars(F.Of(_(S.QuestionMark), Foo), F.Call(S.Assign, a, F.Call(S.QuestionMark, b, c, F.Literal(null))))));
			Stmt("(Foo ? a = b ? c : null : 0);", F.InParens(
				F.Call(S.QuestionMark, Foo, F.Call(S.Assign, a, F.Call(S.QuestionMark, b, c, F.Literal(null))), zero)));
		}

		[Test]
		public void PrintingRegressions()
		{
			Stmt("\"Hello\";", F.Literal("Hello")); // bug: was handled as an empty statement because Name.Name=="" for a literal
			Stmt("new Foo().x;", F.Dot(F.Call(S.New, F.Call(Foo)), x));            // this worked
			Stmt("new Foo().x();", F.Call(F.Dot(F.Call(S.New, F.Call(Foo)), x)));  // but this used to Assert
			// bug: 'public' attribute was suppressed by DropNonDeclarationAttributes
			Stmt("class Foo\n{\n  public Foo()\n  {\n  }\n}",
				F.Call(S.Class, Foo, F._Missing, F.Braces(
					Attr(@public, F.Call(S.Cons, F._Missing, Foo, F.List(), F.Braces())))), 
				p => p.DropNonDeclarationAttributes = true);
			// bug: 'ref' and 'out' attributes were suppressed by DropNonDeclarationAttributes
			Option(Mode.PrintBothParseFirst, 
				"Foo(out a, ref b, public static c, [#partial] x);", "Foo(out a, ref b, c, x);",  
				F.Call(Foo, Attr(@out, a), Attr(@ref, b), Attr(@public, @static, c), Attr(@partial, x)),
				p => p.DropNonDeclarationAttributes = true);
		}

		static LNode Alternate(LNode node)
		{
			node.Style |= NodeStyle.Alternate;
			return node;
		}
		static LNode AsStyle(NodeStyle s, LNode node)
		{
			node.BaseStyle = s;
			return node;
		}
		LNode Operator(LNode node) { return AsStyle(NodeStyle.Operator, node); }
		LNode StmtStyle(LNode node) { return AsStyle(NodeStyle.Statement, node); }
		LNode ExprStyle(LNode node) { return AsStyle(NodeStyle.Expression, node); }

		[Test]
		public void Literals()
		{
			Expr("6",        F.Literal(6));
			Expr("5m",       F.Literal(5m));
			Expr("4L",       F.Literal(4L));
			Expr("3.5",      F.Literal(3.5d));
			Expr("3d",       F.Literal(3d));
			Expr("2.5f",     F.Literal(2.5f));
			Expr("2f",       F.Literal(2f));
			Expr("1u",       F.Literal(1u));
			Expr("0uL",      F.Literal(0uL));
			Expr("-1",       F.Call(S._Negate, F.Literal(1)));
			Expr("-1",       F.Literal(-1), Mode.PrintOnly);
			Expr("0xff",     Alternate(F.Literal(0xFF)));
			Expr("null",     F.Literal(null));
			Expr("false",    F.Literal(false));
			Expr("true",     F.Literal(true));
			Expr("'$'",      F.Literal('$'));
			Expr(@"'\0'",    F.Literal('\0'));
			Expr(@"""hi""",  F.Literal("hi"));
			Expr(@"@""hi""", Alternate(F.Literal("hi")));
			Expr("@\"\n\"",  Alternate(F.Literal("\n")));
			//Expr("@@\"\n\"", Attr(_(S.TriviaDoubleVerbatim), F.Literal("\n")));
			Expr(@"@@hello",   F.Literal(GSymbol.Get("hello")));
			Expr(@"@@int",     F.Literal(GSymbol.Get("int")));
			Expr(@"@@#int32",  F.Literal(GSymbol.Get("#int32")));
			Expr(@"@@`\t`",    F.Literal(GSymbol.Get("\t")));    // Symbols take non-verbatim backquoted strings
			Expr(@"@@`1+1`",   F.Literal(GSymbol.Get("1+1")));
			Expr(@"@@1",       F.Literal(GSymbol.Get("1")));
			Expr("123456789123456789uL", F.Literal(123456789123456789uL));
			Expr("0xffffffffffffffffuL", Alternate(F.Literal(0xFFFFFFFFFFFFFFFFuL)));
			Expr("1.234568E+08f",F.Literal(1.234568E+08f));
			Expr("12345678.9", F.Literal(12345678.9));
			Expr("1.23456789012346E+17d",F.Literal(1.23456789012346E+17d));
			if (this is EcsNodePrinterTests)
				Expr("default(void)", F.Literal(@void.Value));
		}

		[Test]
		public void SimpleExpressions()
		{
			Expr("a + b",        F.Call(S.Add, a, b));
			Expr("a + b + c",    F.Call(S.Add, F.Call(S.Add, a, b), c));
			Expr("+a",           F.Call(S.Add, a));
			Expr("@`+`(a, b, c)", F.Call(S.Add, a, b, c));
			Expr("a >> b",       F.Call(S.Shr, a, b));
			Expr("a = b + c",    F.Assign(a, F.Call(S.Add, b, c)));
			Expr("a(b)(c)",      F.Call(F.Call(a, b), c));
			Expr("a++--",        F.Call(S.PostDec, F.Call(S.PostInc, a)));
			Expr("x => x + 1",   F.Call(S.Lambda, x, F.Call(S.Add, x, one)));
			Stmt("[Foo] a = b;", Attr(Foo, F.Assign(a, b)));
			Stmt("x::a.b;",      F.Dot(F.Call(S.ColonColon, x, a), b));
		}

		[Test]
		public void TuplesAndVarDeclsInExpressions()
		{
			Stmt("Foo a, b, c;",          F.Vars(Foo, a, b, c));
			Stmt("Foo? a, b, c;",         F.Vars(F.Of(_(S.QuestionMark), Foo), a, b, c));
			Stmt("(#var(Foo, a, b, c));", F.InParens(F.Vars(Foo, a, b, c)));
			Stmt("(Foo a) = x;",          F.Assign(F.InParens(F.Vars(Foo, a)), x));
			Stmt("(Foo a) => a;",         F.Call(S.Lambda, F.InParens(F.Vars(Foo, a)), a));
			Stmt("(#var(Foo, a)) + x;",   F.Call(S.Add, F.InParens(F.Vars(Foo, a)), x));
			var x_1 = F.Tuple(x, one);
			Stmt("(a, b) = (x, 1);",      F.Assign(F.Tuple(a, b), x_1));
			Stmt("(a,) = (x,);",          F.Assign(F.Tuple(a), F.Tuple(x)));
			Stmt("(a, Foo b) = (x, 1);",  F.Assign(F.Tuple(a, F.Vars(Foo, b)), x_1));
			// TODO: drop support for this syntax in the printer.
			// The parser doesn't get it, and that's okay.
			//Stmt("(a, (Foo b)) = (x, 1);",F.Set(F.Tuple(a, F.InParens(F.Vars(Foo, b))), x_1));
			Stmt("(Foo a, b) = (x, 1);",  F.Assign(F.Tuple(F.Vars(Foo, a), b), x_1));
			Stmt("(#var(Foo, a) + 1, b) = (x, 1);", F.Assign(F.Tuple(F.Call(S.Add, F.Vars(Foo, a), one), b), x_1));
			Stmt("(Foo a,) = (x,);",      F.Assign(F.Tuple(F.Vars(Foo, a)), F.Tuple(x)));
		}

		[Test]
		public void SpecialOperators()
		{
			Expr("c ? Foo(x) : a + b", F.Call(S.QuestionMark, c, F.Call(Foo, x), F.Call(S.Add, a, b)));
			Expr("Foo[x]",           F.Call(S.Bracks, Foo, x));
			Expr("Foo[a, b]",        F.Call(S.Bracks, Foo, a, b));
			Expr("Foo[a - 1]",       F.Call(S.Bracks, Foo, F.Call(S.Sub, a, one)));
			Expr("Foo[]",            F.Call(S.Bracks, Foo)); // "Foo[]" means #of(#`[]`, Foo) only in a type context
			Expr("@`[]`()",          F.Call(S.Bracks));
			Expr("(Foo) x",          F.Call(S.Cast, x, Foo));
			Expr("x(->Foo)",         Alternate(F.Call(S.Cast, x, Foo)));
			// TODO
			//Expr("x(->a + b)",       F.Call(S.Cast, x, F.Call(S.Add, a, b)));
			Expr("x as Foo",         F.Call(S.As, x, Foo));
			Expr("x using Foo",      F.Call(S.UsingCast, x, Foo));
			Expr("x(as Foo)",        Alternate(F.Call(S.As, x, Foo)));
			Expr("x(using Foo)",     Alternate(F.Call(S.UsingCast, x, Foo)));
			Expr("x++",              F.Call(S.PostInc, x));
			Expr("x--",              F.Call(S.PostDec, x));
			Expr("@`suf++`(a, b)",  F.Call(S.PostInc, a, b));
			Expr("@`suf--`()",      F.Call(S.PostDec));

			// TODO: reevaluate how we're going to do code quotes
			//Expr("@(a = b)",         F.Call(S.CodeQuote, F.Set(a, b)));
			//Expr("@(a = b, Foo())",  F.Call(S.CodeQuote, F.Set(a, b), F.Call(Foo)));
			//Expr("@@(a = b)",        F.Call(S.CodeQuoteSubstituting, F.Set(a, b)));
			//Expr("@@(a = b, Foo())", F.Call(S.CodeQuoteSubstituting, F.Set(a, b), F.Call(Foo)));
		}

		[Test]
		public void ExpressionsAndAttrs()
		{
			// The printer must use prefix notation if the arguments passed to an 
			// operator have attributes.
			Expr("@`+`([Foo] a, b)",    F.Call(S.Add, Attr(Foo, a), b));
			Expr("@`+`(a, [Foo] b)",    F.Call(S.Add, a, Attr(Foo, b)));
			Expr("@`[]`([Foo] a, b)", F.Call(S.Bracks, Attr(Foo, a), b));
			Expr("a[[Foo] b]",         F.Call(S.Bracks, a, Attr(Foo, b)));
			Expr("@`?`([Foo] c, a, b)", F.Call(S.QuestionMark, Attr(Foo, c), a, b));
			Expr("@`?`(c, [Foo] a, b)", F.Call(S.QuestionMark, c, Attr(Foo, a), b));
			Expr("@`?`(c, a, [Foo] b)", F.Call(S.QuestionMark, c, a, Attr(Foo, b)));
		}

		[Test]
		public void BugFixes()
		{
			Expr("(a + b).b<c>()", F.Call(F.Of(F.Dot(F.InParens(F.Call(S.Add, a, b)), b), c)));
			Stmt("@`+`(a, b)(c, 1);", F.Call(F.Call(S.Add, a, b), c, one)); // was: "c+1"
			// was "partial #var(Foo, a);" which would be parsed as a method declaration
			Stmt("([#partial] #var(Foo, a));", F.InParens(Attr(@partial, F.Vars(Foo, a))));
		}

		[Test]
		public void BracesInExpr()
		{
			var stmt1 = F.Call(S.QuickBind, F.Dot(Foo, x), a);
			var stmt2 = F.Call(S.Add, F.Call(S.Mul, a, a), a);
			Expr("b + #(Foo.x=:a, a * a + a)",                F.Call(S.Add, b, F.List(stmt1, stmt2)));
			Expr("b + #@{\n  Foo.x=:a;\n @`*`(a, a) + a;\n}", F.Call(S.Add, b, F.List(stmt1, stmt2)), Mode.ParseOnly);
			Expr("b + {\n  Foo.x=:a;\n  @`*`(a, a) + a;\n}",  F.Call(S.Add, b, F.Braces(stmt1, stmt2)));
			Expr("b + @`{}`(Foo.x=:a, a * a + a)", F.Call(S.Add, b, AsStyle(NodeStyle.PrefixNotation, F.Braces(stmt1, stmt2))));
		}

		[Test]
		public void PrecedenceChallenges()
		{
			Expr(@"@`.`(a, -b)",     F.Dot(a, F.Call(S._Negate, b)));
			Expr(@"@`.`(a, -b).c",   F.Dot(a, F.Call(S._Negate, b), c));
			Expr(@"@`.`(a, -b.c)",   F.Dot(a, F.Call(S._Negate, F.Dot(b, c))));
			Expr(@"a.(-b)(c)",       F.Call(F.Dot(a, F.InParens(F.Call(S._Negate, b))), c));
			// The printer should revert to prefix notation in certain cases in 
			// order to faithfully represent the original tree.
			Expr(@"a * b + c",       F.Call(S.Add, F.Call(S.Mul, a, b), c));
			Expr(@"(a + b) * c",     F.Call(S.Mul, F.InParens(F.Call(S.Add, a, b)), c));
			Expr(@"@`+`(a, b) * c",  F.Call(S.Mul, F.Call(S.Add, a, b), c));
			Expr(@"--a++",           F.Call(S.PreDec, F.Call(S.PostInc, a)));
			Expr(@"(--a)++",         F.Call(S.PostInc, F.InParens(F.Call(S.PreDec, a))));
			Expr(@"@`--`(a)++",      F.Call(S.PostInc, F.Call(S.PreDec, a)));
			LNode a_b = F.Dot(a, b), a_b__c = F.Call(S.NullDot, F.Dot(a, b), c);
			Expr(@"a.b?.c.x",        F.Call(S.NullDot, a_b, F.Dot(c, x)));
			Expr(@"(a.b?.c).x",      F.Dot(F.InParens(a_b__c), x));
			Expr(@"@`?.`(a.b, c).x", F.Dot(a_b__c, x));
			Expr(@"++$x",            F.Call(S.PreInc, F.Call(S.Substitute, x)));
			Expr(@"++$([Foo] x)",    F.Call(S.PreInc, F.Call(S.Substitute, Attr(Foo, x))));
			Expr(@"a ? b : c",       F.Call(S.QuestionMark, a, b, c));
			Expr(@"a ? b + x : c + x",  F.Call(S.QuestionMark, a, F.Call(S.Add, b, x), F.Call(S.Add, c, x)));
			Expr(@"a ? b = x : (c = x)",F.Call(S.QuestionMark, a, F.Assign(b, x), F.InParens(F.Assign(c, x))));
			// A prefix operator can appear on the right-hand side of any infix/
			// prefix operator regardless of the precedence of the two operators.
			Expr(@"++$x",            F.Call(S.PreInc, F.Call(S.Substitute, x))); // easy
			Expr(@"++--x",           F.Call(S.PreInc, F.Call(S.PreDec, x)));     // easy
			Expr(@"$(++x)",          F.Call(S.Substitute, F.Call(S.PreInc, x)));
			Expr(@".(~x)",           F.Call(S.Dot, F.Call(S.NotBits, x)));
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
			// Due to its high precedence, the argument of a the $ operator must 
			// be in parens unless it is trivial.
			Expr(@"$x",             F.Call(S.Substitute, x));
			Expr(@"$(x++)",         F.Call(S.Substitute, F.Call(S.PostInc, x)));
			Expr(@"$(Foo(x))",      F.Call(S.Substitute, F.Call(Foo, x)));
			Expr(@"$(a.b)",         F.Call(S.Substitute, F.Dot(a, b)));
			Expr(@"$(a.b<c>)",      F.Call(S.Substitute, F.Of(F.Dot(a, b), c)));
			Expr(@"$((Foo) x)",     F.Call(S.Substitute, F.Call(S.Cast, x, Foo)));
			Expr(@"$(x(->Foo))",    F.Call(S.Substitute, Alternate(F.Call(S.Cast, x, Foo))));
		}

		[Test]
		public void SpecialEcsChallenges()
		{
			Expr("Foo x = a",            F.Var(Foo, x.Name, a));
			Expr("(Foo x = a) + 1",      F.Call(S.Add, F.InParens(F.Var(Foo, x.Name, a)), one));
			Expr("#var(Foo, x = a) + 1", F.Call(S.Add, F.Var(Foo, x.Name, a), one));
			Expr("#var(Foo, a) = x",     F.Assign(F.Vars(Foo, a), x));
			Expr("#var(Foo, a) + x",     F.Call(S.Add, F.Vars(Foo, a), x));
			Expr("x + #var(Foo, a)",     F.Call(S.Add, x, F.Vars(Foo, a)));
			Expr("#label(Foo)",          F.Call(S.Label, Foo));
			Stmt("Foo:",                 F.Call(S.Label, Foo));
			LNode Foo_a = F.Call(S.NamedArg, Foo, a);
			Expr("Foo: a",               Foo_a);
			Stmt("#namedArg(Foo, a);",   Foo_a);
			Expr("#namedArg(Foo(x), a)", F.Call(S.NamedArg, F.Call(Foo, x), a));
			Expr("b + (Foo: a)",         F.Call(S.Add, b, F.InParens(Foo_a)));
			Expr("b + #namedArg(Foo, a)",F.Call(S.Add, b, Foo_a));
			// Ambiguity between multiplication and pointer declarations:
			// - multiplication at stmt level => prefix notation, except in #result or when lhs is not a complex identifier
			// - pointer declaration inside expr => generic, not pointer, notation
			Expr("a * b",                F.Call(S.Mul, a, b));
			Stmt("a `*` b;",             F.Call(S.Mul, a, b));
			Stmt("a() * b;",             F.Call(S.Mul, F.Call(a), b));
			Expr("#result(a * b)",       F.Result(F.Call(S.Mul, a, b)));
			Stmt("{\n  a * b\n}",        F.Braces(F.Result(F.Call(S.Mul, a, b))));
			Stmt("Foo* a = x;",          F.Var(F.Of(_(S._Pointer), Foo), a.Name, x));
			Expr("@`*`<Foo> a = x",      F.Var(F.Of(_(S._Pointer), Foo), a.Name, x));
			// Ambiguity between bitwise not and destructor declarations
			Expr("~Foo()",               F.Call(S.NotBits, F.Call(Foo)));
			Stmt("@`~`(Foo());",         F.Call(S.NotBits, F.Call(Foo)));
			Stmt("~Foo;",                F.Call(S.NotBits, Foo));
			Stmt("$Foo $x;",             F.Var(F.Call(S.Substitute, Foo), F.Call(S.Substitute, x)));
			Stmt("$Foo $x = 1;",         F.Var(F.Call(S.Substitute, Foo), F.Call(S.Substitute, x), one));
			Stmt("$Foo<$2> $x = 1;",     F.Var(F.Of(F.Call(S.Substitute, Foo), F.Call(S.Substitute, two)), 
			                                                              F.Call(S.Substitute, x), one));
			Expr("$Foo $x = 1",         F.Var(F.Call(S.Substitute, Foo), F.Call(S.Substitute, x), one));
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
			Expr("@`[]`<Foo>",        FooArray);
			Expr("@`?`<Foo>",         FooNullable);
			Expr("@`*`<Foo>",         FooPointer);
			Stmt("#var(Foo[], a);",   F.Vars(FooBracks, a));
			Stmt("Foo[] a;",          F.Vars(FooArray, a));
			Stmt("typeof(Foo?);",     F.Call(S.Typeof, FooNullable));
			Stmt("default(Foo*);",    F.Call(S.Default, FooPointer));
			Stmt("(Foo[]) a;",        F.Call(S.Cast, a, FooArray));
			Stmt("a(->Foo?);",        Alternate(F.Call(S.Cast, a, FooNullable)));
			Stmt("a(as Foo*);",       Alternate(F.Call(S.As, a, FooPointer)));
			Stmt("Foo!(#(Foo[]));",   F.Of(Foo, F.List(FooBracks)));
			Stmt("Foo!(#(@`*`<Foo>));", F.Of(Foo, F.List(FooPointer)));
			Expr("checked(Foo[])",    F.Call(S.Checked, FooBracks));
			Stmt("Foo<a*> x;",        F.Vars(F.Of(Foo, F.Of(_(S._Pointer), a)), x));
		}

		[Test]
		public void OptionsTest()
		{
			// MixImmiscibleOperators is tested elsewhere
			Option(Mode.PrintBothParseFirst, @"b(->Foo)(x);", @"((Foo) b)(x);", F.Call(F.Call(S.Cast, b, Foo), x), p => p.SetPlainCSharpMode());
			Option(Mode.Both,       @"b(x)(->Foo);", @"(Foo) b(x);", Alternate(F.Call(S.Cast, F.Call(b, x), Foo)), p => p.PreferOldStyleCasts = true);
			Option(Mode.Both,       @"yield return x", @"yield return x;",  Attr(_(S.Yield), F.Call(S.Return, x)), p => p.SetPlainCSharpMode());
			
			Action<EcsNodePrinter> parens = p => p.AllowChangeParenthesis = true;
			Option(Mode.PrintBothParseFirst, @"@`+`(a, b) / c;", @"(a + b) / c;", F.Call(S.Div, F.Call(S.Add, a, b), c), parens);
			Option(Mode.PrintBothParseFirst, @"@`-`(a)++;",      @"(-a)++;",      F.Call(S.PostInc, F.Call(S._Negate, a)), parens);
			
			// Put attributes in various locations and watch them all disappear
			Action<EcsNodePrinter> dropAttrs = p => p.DropNonDeclarationAttributes = true;
			Option(Mode.PrintBothParseFirst, @"[Foo] a + b;",           @"a + b;",     Attr(Foo, F.Call(S.Add, a, b)), dropAttrs);
			Option(Mode.PrintBothParseFirst, @"public a(x);",           @"a(x);",      Attr(@public, F.Call(a, x)), dropAttrs);
			Option(Mode.PrintBothParseFirst, @"a([#foo] x);",           @"a(x);",      F.Call(a, Attr(fooKW, x)), dropAttrs);
			Option(Mode.PrintBothParseFirst, @"x[[Foo] a];",            @"x[a];",      F.Call(S.Bracks, x, Attr(Foo, a)), dropAttrs);
			Option(Mode.PrintBothParseFirst, @"@`[]`(static x, a);",   @"x[a];",      F.Call(S.Bracks, Attr(@static, x), a), dropAttrs);
			Option(Mode.PrintBothParseFirst, @"@`+`([Foo] a, 1);",     @"a + 1;",     F.Call(S.Add, Attr(Foo, a), one), dropAttrs);
			Option(Mode.PrintBothParseFirst, @"@`+`(a, [Foo] 1);",     @"a + 1;",     F.Call(S.Add, a, Attr(Foo, one)), dropAttrs);
			Option(Mode.PrintBothParseFirst, @"@`?`(a, [#foo] b, c);", @"a ? b : c;", F.Call(S.QuestionMark, a, Attr(fooKW, b), c), dropAttrs);
			Option(Mode.PrintBothParseFirst, @"@`?`(a, b, public c);", @"a ? b : c;", F.Call(S.QuestionMark, a, b, Attr(@public, c)), dropAttrs);
			Option(Mode.PrintBothParseFirst, @"@`++`([Foo] x);",         @"++x;",       F.Call(S.PreInc, Attr(Foo, x)), dropAttrs);
			Option(Mode.PrintBothParseFirst, @"@`suf++`([Foo] x);",    @"x++;",       F.Call(S.PostInc, Attr(Foo, x)), dropAttrs);
			Option(Mode.PrintBothParseFirst, @"x(->static Foo);",       @"(Foo) x;",   F.Call(S.Cast, x, Attr(@static, Foo)), dropAttrs);
			Option(Mode.PrintBothParseFirst, @"#var(static Foo, x);",   @"Foo x;",     F.Vars(Attr(@static, Foo), x), dropAttrs);
			Option(Mode.PrintBothParseFirst, @"#var(Foo, static x);",   @"Foo x;",     F.Vars(Foo, Attr(@static, x)), dropAttrs);
			Option(Mode.PrintBothParseFirst, @"#var(Foo<a>, [#foo] b, c = 1);",@"Foo<a> b, c = 1;", F.Vars(F.Of(Foo, a), Attr(fooKW, b), F.Assign(c, one)), dropAttrs);
			Option(Mode.PrintBothParseFirst, @"#var(Foo!(static a), b);", @"Foo<a> b;",             F.Vars(F.Of(Foo, Attr(@static, a)), b), dropAttrs);
			Option(Mode.PrintBothParseFirst, @"#var(#of(static Foo, a), b);", @"Foo<a> b;",         F.Vars(F.Of(Attr(@static, Foo), a), b), dropAttrs);
			if (this is EcsNodePrinterTests)
				Option(Mode.PrintBothParseFirst, @"([Foo] a)(x);",        @"a(x);",      F.Call(Attr(Foo, a), x), dropAttrs);
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
			Expr(@"$(a using Foo)",  F.Call(S.Substitute, F.Call(S.UsingCast, a, Foo)));
			Expr(@"$(a(using Foo))", F.Call(S.Substitute, Alternate(F.Call(S.UsingCast, a, Foo))));
		}

		[Test]
		public void SpecialCSharpChallenges()
		{
			// Cases that are difficult to handle due to ambiguities inherited from C#
			var neg_a = F.Call(S._Negate, a);
			Expr("(Foo) - a",         F.Call(S.Sub, F.InParens(Foo), a));
			Expr("(Foo) (-a)",        F.Call(S.Cast, F.InParens(neg_a), Foo));
			Expr("(Foo) @`-`(a)",    F.Call(S.Cast, neg_a, Foo));
			Expr("(Foo) @`+`(a)",    F.Call(S.Cast, F.Call(S._UnaryPlus, a), Foo));
			var Foo_a = F.Of(Foo, a); 
			Expr("(Foo<a>) (-a)",     F.Call(S.Cast, F.InParens(neg_a), Foo_a));
			Expr("([ ] Foo)(-a)",     F.Call(F.InParens(Foo), neg_a));
			// [] certifies "this is not a cast!"; extra parentheses also work
			Option(Mode.PrintBothParseFirst,
				"([ ] Foo<a>)(-a);", "((Foo<a>))(-a);",
				F.Call(F.InParens(Foo_a), neg_a), p => p.AllowChangeParenthesis = true);
			Expr("(a.b<c>) x",        F.Call(S.Cast, x, F.Of(F.Dot(a, b), c)));
			Expr("(a.b!(c > 1)) x",   F.Call(S.Cast, x, F.Of(F.Dot(a, b), F.Call(S.GT, c, one))));
			Expr("x(->[Foo] a.b<c>)", F.Call(S.Cast, x, Attr(Foo, F.Of(F.Dot(a, b), c))));
			// TODO
			//Expr("x(->a * b)",        F.Call(S.Cast, x, F.Call(S.Mul, a, b)));
			Stmt("Foo* a;",           F.Vars(F.Of(_(S._Pointer), Foo), a));
			Stmt("Foo `*` a = b;",   F.Assign(F.Call(S.Mul, Foo, a), b)); // @*(Foo, a) = b; would also be acceptable
		}

		[Test]
		public void Parentheses()
		{
			if (this is EcsNodePrinterTests)
				Stmt("int x;",             F.Call(S.Var, F.Int32, F.InParens(x)), p => p.AllowChangeParenthesis = true);
			Stmt("#var(int, (x));",    F.Call(S.Var, F.Int32, F.InParens(x)), p => p.AllowChangeParenthesis = false);
			Stmt("int x = (1);",       F.Call(S.Var, F.Int32, F.Call(S.Assign, x, F.InParens(one))), p => p.AllowChangeParenthesis = true);
			Stmt("#var(int, (x) = 1);",F.Call(S.Var, F.Int32, F.Call(S.Assign, F.InParens(x), one)), p => p.AllowChangeParenthesis = false);
			Stmt("#var(int, (x) = 1);",F.Call(S.Var, F.Int32, F.Call(S.Assign, F.InParens(x), one)), p => p.AllowChangeParenthesis = true);
			Option(Mode.PrintBothParseFirst, "#var(int, (x = 1));", "int x = 1;",
				F.Call(S.Var, F.Int32, F.InParens(F.Call(S.Assign, x, one))), p => p.AllowChangeParenthesis = true);
			Stmt("#var((int), x);",    F.Call(S.Var, F.InParens(F.Int32), x), p => p.AllowChangeParenthesis = false);
			Stmt("#var((int), x);",    F.Call(S.Var, F.InParens(F.Int32), x), p => p.AllowChangeParenthesis = true);
			// TODO
			//Expr("x(->(int))",         F.Call(S.Cast, x, F.InParens(F.Int32)), p => p.AllowChangeParenthesis = false);
			//Expr("x(->(int))",         F.Call(S.Cast, x, F.InParens(F.Int32)), p => p.AllowChangeParenthesis = true);
		}

		[Test]
		public void AttrInHead()
		{
			// Normally we can use prefix notation when children have attributes...
			Stmt("@`+=`([a] b, c);",    F.Call(S.AddSet, Attr(a, b), c));
			if (this is EcsNodePrinterTests)
			{
				// But this is no solution if the head of a node has attributes. The only
				// workaround is to add parenthesis.
				Stmt("[a] ([b] c)(x);", Attr(a, F.Call(Attr(b, c), x)));
				Stmt("[a] ([b] c())(x);", Attr(a, F.Call(Attr(b, F.Call(c)), x)));
			}
		}

		[Test]
		public void Backtick()
		{
			LNode foo_a = F.Call(fooKW, a), foo_a_b = F.Call(fooKW, a, b);
			Expr("a `Foo` b", Operator(F.Call(Foo, a, b)));
			Expr("#foo(a, b)", foo_a_b);
			Expr("a `#foo` b", Operator(foo_a_b));
			Expr("#foo(a)", foo_a);
			Stmt("a = b `Foo` c;", F.Assign(a, Operator(F.Call(Foo, b, c))));
			Stmt("a = b `Foo` c**x;", F.Assign(a, Operator(F.Call(Foo, b, F.Call(S.Exp, c, x)))));
		}

		[Test]
		public void Immiscibility()
		{
			Action<EcsNodePrinter> parens = p => p.AllowChangeParenthesis = true;
			Action<EcsNodePrinter> mixImm = p => p.MixImmiscibleOperators = true;
			// Of course, operators can be mixed with themselves.
			Stmt("a + b + c;", F.Call(S.Add, F.Call(S.Add, a, b), c), parens);
			Stmt("a = b = c;", F.Assign(a, F.Assign(b, c)), parens);
			// But some cannot be mixed with each other, unless requested (with mixImm).
			Option(Mode.PrintBothParseFirst, "@`<<`(a, b) + 1;",    "(a << b) + 1;",   F.Call(S.Add, F.Call(S.Shl, a, b), one), parens);
			Option(Mode.PrintBothParseFirst, "@`+`(a, b) << 1;",    "(a + b) << 1;",   F.Call(S.Shl, F.Call(S.Add, a, b), one), parens);
			Option(Mode.Both,                "@`+`(a, b) << 1;",    "a + b << 1;",     F.Call(S.Shl, F.Call(S.Add, a, b), one), mixImm);
			// "@&(a, b) == 1;" would also be acceptable output on the left:
			Option(Mode.PrintBothParseFirst, "a `&` b == 1;",    "(a & b) == 1;",    F.Call(S.Eq, F.Call(S.AndBits, a, b), one), parens);
			Option(Mode.PrintBothParseFirst, "@`==`(a, b) & 1;",    "(a == b) & 1;",   F.Call(S.AndBits, F.Call(S.Eq, a, b), one), parens);
			Option(Mode.Both,                "@`==`(a, b) & 1;",    "a == b & 1;",     F.Call(S.AndBits, F.Call(S.Eq, a, b), one), mixImm);
			Option(Mode.PrintBothParseFirst, "Foo(a, b) + 1;",    "(a `Foo` b) + 1;", F.Call(S.Add, Operator(F.Call(Foo, a, b)), one), parens);
			// @+(a, b) `foo` 1; would also be acceptable output on the left:
			Option(Mode.PrintBothParseFirst, "a `+` b `Foo` 1;", "(a + b) `Foo` 1;", Operator(F.Call(Foo, F.Call(S.Add, a, b), one)), parens);
		}

		[Test]
		public void CallStyleOperators()
		{
			Expr("checked(a + b)",        F.Call(S.Checked, F.Call(S.Add, a, b)));
			Expr("unchecked(a << b)",     F.Call(S.Unchecked, F.Call(S.Shl, a, b)));
			Expr("default(Foo)",          F.Call(S.Default, Foo));
			Expr("default(int)",          F.Call(S.Default, F.Int32));
			Expr("typeof(Foo)",           F.Call(S.Typeof, Foo));
			Expr("typeof(int)",           F.Call(S.Typeof, F.Int32));
			Expr("typeof(Foo<int>)",      F.Call(S.Typeof, F.Call(S.Of, Foo, F.Int32)));
			Expr("sizeof(Foo<int>)",      F.Call(S.Sizeof, F.Call(S.Of, Foo, F.Int32)));
			
			Expr("default(int[])",        F.Call(S.Default,   F.Call(S.Of, _(S.Bracks), F.Int32)));
			Expr("typeof(int[])",         F.Call(S.Typeof,    F.Call(S.Of, _(S.Bracks), F.Int32)));
			Expr("sizeof(int[])",         F.Call(S.Sizeof,    F.Call(S.Of, _(S.Bracks), F.Int32)));
			Expr("checked(@`[]`<int>)",  F.Call(S.Checked,   F.Call(S.Of, _(S.Bracks), F.Int32)));
			Expr("unchecked(@`[]`<int>)",F.Call(S.Unchecked, F.Call(S.Of, _(S.Bracks), F.Int32)));
		}

		[Test]
		public void OperatorNew()
		{
			Expr("new Foo(x)",            F.Call(S.New, F.Call(Foo, x)));
			Expr("new Foo(x) { a }",      F.Call(S.New, F.Call(Foo, x), a));
			Expr("new Foo(x) { [a] b = c }", 
			                              F.Call(S.New, F.Call(Foo, x), Attr(a, F.Assign(b, c))));
			Option(Mode.PrintBothParseFirst, "#new([#foo] Foo(x), a);", "new Foo(x) { a };", 
			                              F.Call(S.New, Attr(fooKW, F.Call(Foo, x)), a), p => p.DropNonDeclarationAttributes = true);
			Expr("new Foo()",             F.Call(S.New, F.Call(Foo)));
			Expr("new Foo { a }",         F.Call(S.New, F.Call(Foo), a));      // new Foo() { a } would also be ok
			Expr("#new([x] Foo(), a)",    F.Call(S.New, Attr(x, F.Call(Foo)), a));
			if (this is EcsNodePrinterTests)
				Expr("#new(([x] Foo)(), a)",  F.Call(S.New, F.Call(Attr(x, Foo)), a));
			Expr("new Foo { a }",         F.Call(S.New, F.Call(Foo), a));
			Expr("#new(Foo, a)",          F.Call(S.New, Foo, a));
			Expr("#new(Foo)",             F.Call(S.New, Foo));
			Expr("new @`+`(a, b)",          F.Call(S.New, F.Call(S.Add, a, b))); // #new(@+(a, b)) would also be ok
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
			Option(Mode.PrintBothParseFirst, "#new(@`[,]`!([Foo] int)());", "new int[,];", 
				F.Call(S.New, F.Call(F.Of(_(S.TwoDimensionalArray), Attr(Foo, F.Int32)))), p => p.DropNonDeclarationAttributes = true);
			Expr("#new",                  F.Id(S.New));
			Expr("#new()",                F.Call(S.New));
			Expr("new { a = 1, b = 2 }",  F.Call(S.New, F._Missing, F.Call(S.Assign, a, one), F.Call(S.Assign, b, two)));

			//int[,] a = null;
			//int[][,] aa = new int[][,] { a };
			//int test = aa[0][1, 2];
		}

		[Test]
		public void DataTypes()
		{
			Stmt("double x;",            F.Vars(F.Double, x));
			Stmt("int[] x;",             F.Vars(F.Of(S._Array, S.Int32), x));
			Stmt("long* x;",             F.Vars(F.Of(S._Pointer, S.Int64), x));
			Stmt("string[][,] x;",       F.Vars(F.Of(_(S._Array), F.Of(S.TwoDimensionalArray, S.String)), x));
			Stmt("typeof(float*);",      F.Call(S.Typeof, F.Of(S._Pointer, S.Single)));
			Stmt("decimal[,,,] x;",      F.Vars(F.Of(S.GetArrayKeyword(4), S.Decimal), x));
			Stmt("double? x;",           F.Vars(F.Of(S.QuestionMark, S.Double), x));
			Stmt("Foo<a.b.c>? x;",       F.Vars(F.Of(_(S.QuestionMark), F.Of(Foo, F.Dot(a, b, c))), x));
			Stmt("Foo<a?,b.c[,]>[] x;",  F.Vars(F.Of(_(S._Array), F.Of(Foo, F.Of(_(S.QuestionMark), a), F.Of(_(S.TwoDimensionalArray), F.Dot(b, c)))), x));
			// Sure, why not
			Stmt("int<decimal[]> x;",    F.Vars(F.Of(_(S.Int32), F.Of(S._Array, S.Decimal)), x));
			// Very weird case. Consider printing as "`[,]`<float>?[] x;" instead
			Stmt("float[,]?[] x;",       F.Vars(F.Of(_(S._Array), F.Of(_(S.QuestionMark), F.Of(S.TwoDimensionalArray, S.Single))), x));
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
			Stmt("{\n  a();\n  b = c;\n}",        F.Braces(F.Call(a), F.Assign(b, c)));
			Stmt("#@{\n  Foo(x);\n  b **= 2\n};", F.List(F.Call(Foo, x), F.Result(F.Call(S.ExpSet, b, two))), Mode.ParseOnly);
		}

		[Test]
		public void SpaceStmts()
		{
			// Spaces: S.Struct, S.Class, S.Trait, S.Enum, S.Alias, S.Interface, S.Namespace
			var public_x = Attr(@public, F.Vars(F.Int32, x));
			Stmt("struct Foo;",        F.Call(S.Struct, Foo, F._Missing));
			Stmt("struct Foo : IFoo;", F.Call(S.Struct, Foo, F.List(IFoo)));
			Stmt("struct Foo\n{\n}",   F.Call(S.Struct, Foo, F._Missing, F.Braces()));
			Stmt("struct Foo\n{\n" +
				"  public int x;\n}",  F.Call(S.Struct, Foo, F._Missing, F.Braces(public_x)));
			Stmt("class Foo : IFoo\n{\n}", F.Call(S.Class, Foo, F.List(IFoo), F.Braces()));
			var a_where = Attr(F.Call(S.Where, @class), a);
			var b_where = Attr(F.Call(S.Where, a), b);
			var c_where = Attr(F.Call(S.Where, F.Call(S.New)), c);
			var stmt = F.Call(S.Class, F.Of(Foo, a_where, b_where), F.List(IFoo), F.Braces());
			Stmt("class Foo<a,b> : IFoo where a: class where b: a\n{\n}", stmt);
			stmt = F.Call(S.Class, F.Of(Foo, Attr(_(S.Out), a_where)), F._Missing, F.Braces());
			Stmt("class Foo<out a> where a: class\n{\n}", stmt);
			stmt = F.Call(S.Class, F.Of(Foo, Attr(_(S.Out), c_where)), F.List(IFoo), F.Braces());
			Stmt("class Foo<out c> : IFoo where c: new()\n{\n}", stmt);
			stmt = F.Call(S.Class, F.Of(Foo, Attr(_(S.In), T)), F._Missing, F.Braces());
			Stmt("class Foo<in T>\n{\n}", stmt);
			stmt = Attr(F.Call(S.If, F.Call(S.Eq, F.Call(S.Add, a, b), c)),
			            F.Call(S.Trait, Foo, F.List(IFoo), F.Braces(public_x)));
			
			// TODO: reconsider "if" clause
			//Stmt("trait Foo : IFoo if a + b == c\n{\n"+
			//	 "  public int x;\n}", stmt);

			// TODO: reconsider "is legal"
			//stmt = Attr(F.Call(S.If, F.Call(S.IsLegal, F.Call(S.Add, F.Call(S.Default, T), one))),
			//			F.Call(S.Struct, F.Of(Foo, F.Call(S.Substitute, T)), F._Missing));
			//Stmt(@"struct Foo<$T> if default(T) + 1 is legal;", stmt);
			//Expr(@"[@#if(default(T) + 1 is legal)] #struct(Foo<$T>, @``)", stmt);

			stmt = F.Call(S.Enum, Foo, F.List(F.UInt8), F.Braces(F.Assign(a, one), b, c, F.Assign(x, F.Literal(24))));
			Stmt("enum Foo : byte\n{\n  a = 1, b, c, x = 24\n}", stmt);
			Expr("#enum(Foo, #(byte), {\n  a = 1;\n  b;\n  c;\n  x = 24;\n})", stmt);

			stmt = F.Call(S.Interface, F.Of(Foo, Attr(@out, T)), F.List(F.Of(_("IEnumerable"), T)), F.Braces(public_x));
			Stmt("interface Foo<out T> : IEnumerable<T>\n{\n  public int x;\n}", stmt);
			Expr("#interface(Foo!(out T), #(IEnumerable<T>), {\n  public int x;\n})", stmt);

			stmt = F.Call(S.Namespace, F.Of(Foo, T), F._Missing, F.Braces(public_x));
			Stmt("namespace Foo<T>\n{\n  public int x;\n}", stmt);
			Expr("#namespace(Foo<T>, @``, {\n  public int x;\n})", stmt);

			stmt = F.Call(S.Class, F.Assign(F.Of(_("L"), T), F.Of(_("List"), T)), F._Missing);
			Stmt("#class(L<T> = List<T>, @``);", stmt);
		}

		[Test]
		public void AliasStmts()
		{
			var public_x = Attr(@public, F.Vars(F.Int32, x));

			Stmt("alias(a = b);", F.Call(GSymbol.Get("alias"), F.Assign(a, b)));
			LNode stmt = F.Call(S.Alias, F.Assign(F.Of(_("Map"), a, b), F.Of(_("Dictionary"), a, b)), F._Missing);
			Stmt("alias Map<a,b> = Dictionary<a,b>;", stmt);
			Expr("#alias(Map<a,b> = Dictionary<a,b>, @``)", stmt);
			stmt = F.Call(S.Alias, F.Assign(Foo, fooKW), F.List(IFoo), F.Braces(public_x));
			Stmt("alias Foo = #foo : IFoo\n{\n  public int x;\n}", stmt);
			Expr("#alias(Foo = #foo, #(IFoo), {\n  public int x;\n})", stmt);

			// An alias must have an "=" node as its first argument; other spaces
			// must have type names as their first argument.
			stmt = F.Call(S.Alias, Foo, F.List(IFoo), F.Braces(public_x));
			Stmt("#alias(Foo, #(IFoo), {\n  public int x;\n});", stmt);
		}

		[Test]
		public void MethodDefinitionStmts()
		{
			// #fn and #delegate
			LNode int_x = F.Vars(F.Int32, x), list_int_x = F.List(int_x), x_mul_x = F.Call(S.Mul, x, x);
			LNode stmt;
			stmt = F.Call(S.Delegate, F.Void, F.Of(Foo, T), F.List(F.Vars(T, a), F.Vars(T, b)));
			Stmt("delegate void Foo<T>(T a, T b);", stmt);
			Expr("#delegate(void, Foo<T>, #(#var(T, a), #var(T, b)))", stmt);
			stmt = F.Call(S.Delegate, F.Void, F.Of(Foo, Attr(F.Call(S.Where, _(S.Class), x), T)), F.List(F.Vars(T, x)));
			Stmt("delegate void Foo<T>(T x) where T: class, x;", stmt);
			Expr("#delegate(void, Foo!([#where(#class, x)] T), #(#var(T, x)))", stmt);
			stmt = Attr(@public, @new, partialWA, F.Fn(F.String, Foo, list_int_x));
			Stmt("public new partial string Foo(int x);", stmt);
			// The printer does not print trivia attributes, but the parsing test will fail if the trivia is missing
			Expr("[#public, #new, "         +            "#partial] #fn(string, Foo, #(#var(int, x)))", stmt, Mode.PrintOnly);
			Expr("[#public, #new, [#trivia_wordAttribute] #partial] #fn(string, Foo, #(#var(int, x)))", stmt, Mode.ParseOnly);
			stmt = F.Fn(F.Int32, Foo, list_int_x, F.Braces(F.Result(x_mul_x)));
			Stmt("int Foo(int x)\n{\n  x * x\n}", stmt);
			Expr("#fn(int, Foo, #(#var(int, x)), {\n  x * x\n})", stmt);
			stmt = F.Fn(F.Int32, Foo, list_int_x, F.Braces(F.Call(S.Return, x_mul_x)));
			Stmt("int Foo(int x)\n{\n  return x * x;\n}", stmt);
			Expr("#fn(int, Foo, #(#var(int, x)), {\n  return x * x;\n})", stmt);
			stmt = F.Fn(F.Decimal, Foo, list_int_x, F.Call(S.Forward, F.Dot(a, b)));
			Stmt("decimal Foo(int x) ==> a.b;", stmt);
			Expr("#fn(decimal, Foo, #(#var(int, x)), ==> a.b)", stmt);
			stmt = F.Fn(_("IEnumerator"), F.Dot(_("IEnumerable"), _("GetEnumerator")), F.List(), F.Braces());
			Stmt("IEnumerator IEnumerable.GetEnumerator()\n{\n}", stmt);
			Expr("#fn(IEnumerator, IEnumerable.GetEnumerator, #(), {\n})", stmt);
			stmt = F.Call(S.Cons, F._Missing, _(S.This), list_int_x, F.Braces(F.Call(_(S.This), x, one), F.Assign(a, x)));
			Stmt("this(int x) : this(x, 1)\n{\n  a = x;\n}", stmt);
			Expr("#cons(@``, this, #(#var(int, x)), {\n  #this(x, 1);\n  a = x;\n})", stmt);
			stmt = F.Call(S.Cons, F._Missing, Foo, list_int_x, F.Braces(F.Call(_(S.Base), x), F.Assign(b, x)));
			Stmt("Foo(int x) : base(x)\n{\n  b = x;\n}", stmt);
			Expr("#cons(@``, Foo, #(#var(int, x)), {\n  base(x);\n  b = x;\n})", stmt);
			stmt = F.Fn(F._Missing, F.Call(S._Destruct, Foo), F.List(), F.Braces());
			Stmt("~Foo()\n{\n}", stmt);
			Expr("#fn(@``, ~Foo, #(), {\n})", stmt);
			stmt = F.Fn(F._Missing, F.Call(S._Negate, Foo), F.List(), F.Braces());
			Stmt("#fn(@``, -Foo, #(), {\n});", stmt);
			stmt = F.Call(S.Class, Foo, F._Missing, F.Braces(F.Fn(F._Missing, F.Call(S._Negate, Foo), F.List(), F.Braces())));
			Stmt("class Foo\n{\n  #fn(@``, -Foo, #(), {\n  });\n}", stmt);
			LNode @operator = _(S.TriviaUseOperatorKeyword), cast = _(S.Cast), operator_cast = Attr(@operator, cast);
			LNode Foo_a = F.Vars(Foo, a), Foo_b = F.Vars(Foo, b); 
			stmt = Attr(@static, F.Fn(F.Bool, Attr(@operator, _(S.Eq)), F.List(F.Vars(T, a), F.Vars(T, b)), F.Braces()));
			Stmt("static bool operator==(T a, T b)\n{\n}", stmt);
			Expr("static #fn(bool, operator==, #(#var(T, a), #var(T, b)), {\n})", stmt);
			stmt = Attr(@static, _(S.Implicit), F.Fn(T, operator_cast, F.List(Foo_a), F.Braces()));
			Stmt("static implicit operator T(Foo a)\n{\n}", stmt);
			Expr("static implicit #fn(T, operator`#cast`, #(#var(Foo, a)), {\n})", stmt);
			stmt = Attr(@static, F.Fn(Foo, F.Of(Foo, F.Call(S.Substitute, T)), F.List()));
			Stmt(@"static Foo Foo<$T>();", stmt);
			stmt = Attr(@static, _(S.Explicit), 
			            F.Fn(F.Of(Foo, T), F.Of(operator_cast, F.Call(S.Substitute, T)), 
			                  F.List(F.Vars(F.Of(_("Bar"), T), b))));
			Stmt(@"static explicit Foo<T> operator`#cast`<$T>(Bar<T> b);", stmt);
			Expr(@"static explicit #fn(Foo<T>, operator`#cast`<$T>, #(#var(Bar<T>, b)))", stmt);
			stmt = F.Fn(F.Bool, Attr(@operator, _("when")), F.List(Foo_a, Foo_b), F.Braces());
			Stmt("bool operator`when`(Foo a, Foo b)\n{\n}", stmt);
			Expr("#fn(bool, operator`when`, #(#var(Foo, a), #var(Foo, b)), {\n})", stmt);

			stmt = Attr(F.Call(Foo), @static,
			       F.Fn(Attr(Foo, F.Bool), 
			             Attr(@operator, _(S.Neq)),
			             F.List(F.Vars(T, a), F.Vars(T, b)),
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
			var emptyConstructor = F.Call(S.Cons, F._Missing, _(S.This), F.List(), F.Braces());
			var thisColonBase    = F.Call(S.Cons, F._Missing, _(S.This), F.List(), F.Braces(F.Call(S.Base)));
			var thisConsNoBody   = F.Call(S.Cons, F._Missing, _(S.This), F.List());
			var fooConstructor   = F.Call(S.Cons, F._Missing, Foo, F.List(), F.Braces(F.Call(x)));
			var fooConsNoBody    = F.Call(S.Cons, F._Missing, Foo, F.List());
			Action<EcsNodePrinter> allowAmbig = p => p.AllowConstructorAmbiguity = true;
			Stmt("this()\n{\n}",                        emptyConstructor);
			Stmt("#cons(@``, Foo, #());",               fooConsNoBody);
			Stmt("#cons(@``, Foo, #(), {\n  x();\n});", fooConstructor);
			Stmt("#this(x);",                           F.Call(S.This, x));
			Stmt("base(x);",                            F.Call(S.Base, x));
			Option(Mode.PrintBothParseFirst, "#cons(@``, Foo, #(), {\n});", "Foo()\n{\n}",
				F.Call(S.Cons, F._Missing, Foo, F.List(), F.Braces()), allowAmbig);
			Stmt("this() : base()\n{\n}",       thisColonBase, allowAmbig);
			Stmt("this() : this(x)\n{\n  x;\n}",
				F.Call(S.Cons, F._Missing, _(S.This), F.List(), F.Braces(F.Call(S.This, x), x)), allowAmbig);
			Stmt("this()\n{\n  x;\n  this(x);\n}",
				F.Call(S.Cons, F._Missing, _(S.This), F.List(), F.Braces(x, F.Call(S.This, x))), allowAmbig);
			Stmt("this()\n{\n  this() : base()\n  {\n  }\n}",
				F.Call(S.Cons, F._Missing, _(S.This), F.List(), F.Braces(thisColonBase)));
			Stmt("this()\n{\n  this();\n}",
				F.Call(S.Cons, F._Missing, _(S.This), F.List(), F.Braces(thisConsNoBody)), allowAmbig, false, Mode.PrintOnly);
			Stmt("this()\n{\n  x;\n  this();\n}",
				F.Call(S.Cons, F._Missing, _(S.This), F.List(), F.Braces(x, F.Call(S.This))), allowAmbig);
			Stmt("this()\n{\n  #cons(@``, this, #());\n}",
				F.Call(S.Cons, F._Missing, _(S.This), F.List(), F.Braces(thisConsNoBody)));
			//Stmt("this()\n{\n  #cons(@``, this, #(), {\n  base();\n});\n}", 
			Stmt("this()\n{\n  this() : base()\n  {\n  }\n}", 
				F.Call(S.Cons, F._Missing, _(S.This), F.List(), F.Braces(thisColonBase)), allowAmbig);
			Stmt("this()\n{\n  #cons(@``, this, #(), {\n  });\n}",
				F.Call(S.Cons, F._Missing, _(S.This), F.List(), F.Braces(emptyConstructor)));
			Stmt("class Foo\n{\n  Foo().x;\n}",   F.Call(S.Class, Foo, F._Missing, F.Braces(
			                                          F.Dot(F.Call(Foo), x))));
			Stmt("class Foo\n{\n  (Foo());\n}",   F.Call(S.Class, Foo, F._Missing, F.Braces(F.InParens(F.Call(Foo)))));
			Stmt("class Foo\n{\n  (Foo());\n}",   F.Call(S.Class, Foo, F._Missing, F.Braces(F.Call(Foo))), Mode.PrintOnly);
			Stmt("class Foo\n{\n  Foo();\n}",                    F.Call(S.Class, Foo, F._Missing, F.Braces(fooConsNoBody)));
			Stmt("class Foo\n{\n  Foo()\n  {\n    x();\n  }\n}", F.Call(S.Class, Foo, F._Missing, F.Braces(fooConstructor)));
			Stmt("class Foo\n{\n  #cons(@``, IFoo, #());\n}",F.Call(S.Class, Foo, F._Missing, F.Braces(
			                                          F.Call(S.Cons, F._Missing, IFoo, F.List()))));
			Stmt("class Foo\n{\n  IFoo() : base()\n  {\n  }\n}", F.Call(S.Class, Foo, F._Missing, F.Braces(
			                                          F.Call(S.Cons, F._Missing, IFoo, F.List(), F.Braces(F.Call(S.Base))))));
			if (this is EcsNodePrinterTests)
			{
				Stmt("class Foo\n{\n  Foo();\n}", F.Call(S.Class, Foo, F._Missing, F.Braces(F.Call(Foo))), allowAmbig);
				Stmt("class Foo\n{\n  (Foo());\n}", F.Call(S.Class, Foo, F._Missing, F.Braces(F.Call(Foo))), p => p.AllowChangeParenthesis = false);
				Stmt("class Foo\n{\n  (Foo());\n}", F.Call(S.Class, Foo, F._Missing, F.Braces(F.Call(Foo))), p => p.AllowChangeParenthesis = true);
			}
			Stmt("class Foo\n{\n  x(Foo());\n}",  F.Call(S.Class, Foo, F._Missing, F.Braces(F.Call(x, F.Call(Foo)))));
			
			// Non-keyword attributes allowed on this() but not Foo() constructor
			Stmt("partial this()\n{\n}",          Attr(partialWA, F.Call(S.Cons, F._Missing, _(S.This), F.List(), F.Braces())));
			Stmt("class Foo\n{\n  partial this()\n  {\n  }\n}", F.Call(S.Class, Foo, F._Missing, F.Braces(
			                                      Attr(partialWA, F.Call(S.Cons, F._Missing, _(S.This), F.List(), F.Braces())))));
			Stmt("class Foo\n{\n  [#partial] Foo()\n  {\n  }\n}", F.Call(S.Class, Foo, F._Missing, F.Braces(
			                                      Attr(@partial,  F.Call(S.Cons, F._Missing, Foo, F.List(), F.Braces())))));
			Stmt("this() : this(x)\n{\n}",        F.Call(S.Cons, F._Missing, _(S.This), F.List(), F.Braces(F.Call(S.This, x))), allowAmbig);
		}

		LNode AddWords(LNode stmt, bool partialIsWA = true) { return stmt.PlusAttrs(@public, @new, partialIsWA ? partialWA : partial, @static); }

		[Test]
		public void EventStmts()
		{
			LNode EventHandler = _("EventHandler"), add = _("add"), remove = _("remove");
			var stmt = F.Call(S.Event, F.Of(EventHandler, T), _("Click"));
			Stmt("event EventHandler<T> Click;", stmt);
			Expr("#event(EventHandler<T>, Click)", stmt);
			stmt = F.Call(S.Event, EventHandler, a, b);
			Stmt("event EventHandler a, b;", stmt);
			Expr("#event(EventHandler, a, b)", stmt);
			stmt = F.Call(S.Event, EventHandler, a, F.Braces(
				AsStyle(NodeStyle.Special, F.Call(add, F.Braces())),
				AsStyle(NodeStyle.Special, F.Call(remove, F.Braces()))));
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
			LNode stmt = F.Property(F.Int32, Foo, F.Braces(get, set));
			Stmt("int Foo\n{\n  get;\n  set;\n}", stmt);
			Expr("#property(int, Foo, {\n  get;\n  set;\n})", stmt);
			stmt = Attr(@public, F.Property(F.Int32, Foo, F.Braces(
								   AsStyle(NodeStyle.Special, F.Call(get, F.Braces(F.Call(S.Return, x)))),
								   AsStyle(NodeStyle.Special, F.Call(set, F.Braces(F.Assign(x, value)))))));
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
			stmt = F.Property(F.Int32, Foo, F.Braces(F.Call(get, F.Call(S.Forward, a, b))));
			Stmt("int Foo\n{\n  get(@`==>`(a, b));\n}", stmt);
			if (this is EcsNodePrinterTests) {
				stmt = F.Property(F.Int32, Foo, F.Braces(
								  Attr(trivia_forwardedProperty, F.Call(get, F.Call(S.Forward, a, b)))));
				Stmt("int Foo\n{\n  get(@`==>`(a, b));\n}", stmt);
			}
			Stmt("int Foo\n{\n  protected get;\n  private set;\n}",
				F.Property(F.Int32, Foo, F.Braces(
					F.Attr(F.Protected, get), F.Attr(F.Private, set))));
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
			Stmt("throw;",             F.Call(S.Throw));
			Stmt("throw new Foo();",   F.Call(S.Throw, F.Call(S.New, F.Call(Foo))));
			Stmt("#break;",            _(S.Break));
			Stmt("#continue;",         _(S.Continue));
			Stmt("#return;",           _(S.Return));
			Stmt("#throw;",            _(S.Throw));
		}

		[Test]
		public void UsingStmts()
		{
			Stmt("using Foo.x;",       F.Call(S.Import, F.Dot(Foo, x)));
			Stmt("using Foo = x;",     Attr(F.Id(S.FilePrivate), F.Call(S.Alias, F.Call(S.Assign, Foo, x), F._Missing)));
			Stmt("using (var x = Foo)\n  x.a();", F.Call(S.UsingStmt, F.Var(F._Missing, x.Name, Foo), F.Call(F.Dot(x, a))));
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

			Stmt("checked {\n  x = a();\n  x * x\n}",   F.Call(S.Checked, F.Braces(F.Assign(x, F.Call(a)), 
			                                                                 F.Result(F.Call(S.Mul, x, x)))));
			Stmt("unchecked {\n  0xbaad * 0xf00d\n}",   F.Call(S.Unchecked, F.Braces(F.Result(
			                                                   F.Call(S.Mul, Alternate(F.Literal(0xBAAD)), Alternate(F.Literal(0xF00D)))))));
			
			Stmt("do\n  a();\nwhile (c);",              F.Call(S.DoWhile, F.Call(a), c));
			Stmt("do {\n  a();\n} while (c);",          F.Call(S.DoWhile, F.Braces(F.Call(a)), c));
			Stmt("do {\n  a\n} while (c);",             F.Call(S.DoWhile, F.Braces(F.Result(a)), c));
			Stmt("do #@{\n  a();\n}; while (c);",       F.Call(S.DoWhile, F.List(F.Call(a)), c), Mode.ParseOnly);
			
			var amp_b_c = F.Call(S._AddressOf, F.Call(S.PtrArrow, b, c));
			var int_a_amp_b_c = F.Var(F.Of(_(S._Pointer), F.Int32), a.Name, amp_b_c);
			Stmt("fixed (int* a = &b->c)\n  Foo(a);",    F.Call(S.Fixed, int_a_amp_b_c, F.Call(Foo, a)));
			Stmt("fixed (int* a = &b->c) {\n  Foo(a);\n}", F.Call(S.Fixed, int_a_amp_b_c, F.Braces(F.Call(Foo, a))));
			var stmt = F.Call(S.Fixed, F.Vars(F.Of(_(S._Pointer), F.Int32), F.Assign(x, F.Call(S._AddressOf, Foo))), F.Call(a, x));
			Stmt("fixed (int* x = &Foo)\n  a(x);", stmt);
			
			var forArgs = new LNode[] {
				F.Var(F.Int32, x.Name, F.Literal(0)),
				F.Call(S.LT, x, F.Literal(10)),
				F.Call(S.PostInc, x),
				F.Braces()
			};
			Stmt("for (int x = 0; x < 10; x++) {\n}",   F.Call(S.For, forArgs));
			// TODO
			//forArgs[3] = F.List(F.Call(a), F.Call(b));
			//Stmt("for (int x = 0; x < 10; x++) #{\n  a();\n  b();\n}", F.Call(S.For, forArgs));
			//forArgs[3] = F.List(F.Result(a));
			//Stmt("for (int x = 0; x < 10; x++) #{\n  a\n}", F.Call(S.For, forArgs));

			stmt = F.Call(S.While, F.Call(S.GT, x, one), F.Call(S.PostDec, x));
			Stmt("while (x > 1)\n  x--;", stmt);
			stmt = F.Call(S.UsingStmt, F.Var(F._Missing, x.Name, F.Call(S.New, F.Call(Foo))), F.Call(F.Dot(x, a)));
			Stmt("using (var x = new Foo())\n  x.a();", stmt);
			stmt = F.Call(S.Lock, Foo, F.Braces(F.Call(F.Dot(Foo, Foo))));
			Stmt("lock (Foo) {\n  Foo.Foo();\n}", stmt);

			stmt = F.Call(S.Try, F.Call(Foo), F.Call(S.Catch, F._Missing, F.Braces()));
			Stmt("try\n  Foo();\ncatch {\n}", stmt);
			stmt = F.Call(S.Try, F.Call(Foo), F.Call(S.Catch, F.Vars(_("Exception"), x), F.Braces(F.Call(S.Throw))), F.Call(S.Finally, F.Call(_("hi_mom"))));
			Stmt("try\n  Foo();\n"+
				 "catch (Exception x) {\n  throw;\n"+
				 "} finally\n  hi_mom();", stmt);

			stmt = F.Call(S.ForEach, F.Vars(F._Missing, x), Foo, F.Call(a, x));
			Stmt("foreach (var x in Foo)\n  a(x);", stmt);
			if (this is EcsParserTests) {
				stmt = F.Call(S.ForEach, x, Foo, F.Call(a, x));
				Stmt("foreach (x in Foo)\n  a(x);", stmt);
			}
			// TODO reconsider
			//stmt = F.Call(S.ForEach, F.Call(S.Add, a, b), c, F.Braces());
			//Stmt("foreach (a + b in c) {\n}", stmt);
			//stmt = F.Call(S.ForEach, F.Set(a, x), F.Set(b, x), F.List());
			//Stmt("foreach (a `=` x in b = x) #{\n}", stmt);
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
		public void BlockCallVsNormalCall()
		{
			Stmt("a(Foo < T > b);", F.Call(a, F.Call(S.GT, F.Call(S.LT, Foo, T), b)));
			Stmt("a (Foo<T> b) {\n  Foo();\n}", AsStyle(NodeStyle.Special, F.Call(a,
				F.Var(F.Of(Foo, T), b), F.Braces(F.Call(Foo)))));
		}

		[Test]
		public void StmtsWithAttributes()
		{
			LNode[] args = new LNode[4] { Foo, fooWA, @public, null };
			args[3] = F.Call(S.Struct, Foo, F._Missing, F.Braces(F.Vars(F.String, x)));
			Stmt("[Foo] foo public struct Foo\n{\n  string x;\n}", Attr(args));
			args[3] = F.Fn(F.String, Foo, F.List(), F.Braces(F.Result(x)));
			Stmt("[Foo] foo public string Foo()\n{\n  x\n}", Attr(args));
			args[3] = F.Call(S.Break);
			Stmt("[Foo] foo public break;", Attr(args));
			args[3] = F.Call(S.GotoCase, x);
			Stmt("[Foo] foo public goto case x;", Attr(args));
			args[3] = F.Call(S.Return, one);
			Stmt("[Foo] foo public return 1;", Attr(args));
			args[3] = F.Call(S.Unchecked, F.Braces(F.Assign(a, F.Call(S.Shl, b, c))));
			Stmt("[Foo] foo public unchecked {\n  a = b << c;\n}", Attr(args));
			// TODO reconsider
			//args[3] = F.Call(S.If, F.Call(S.Eq, a, b), F.Call(c));
			//Stmt("[Foo, #foo] public if (a == b)\n  c();", Attr(args));
			args[3] = F.Call(S.DoWhile, F.Call(a), c);
			Stmt("[Foo] foo public do\n  a();\nwhile (c);", Attr(args));
			args[3] = F.Call(S.UsingStmt, Foo, F.Braces(F.Call(a, Foo)));
			Stmt("[Foo] foo public using (Foo) {\n  a(Foo);\n}", Attr(args));
			args[3] = F.Call(S.For, a, b, c, x);
			Stmt("[Foo] foo public for (a; b; c)\n  x;", Attr(args));
			args[3] = F.Braces(F.Call(a));
			if (this is EcsNodePrinterTests)
				Stmt("[Foo, #foo] public {\n  a();\n}", Attr(args));
			args[1] = fooKW;
			args[3] = F.Braces(F.Call(a));
			Stmt("[Foo, #foo] public {\n  a();\n}", Attr(args));
			args[3] = F.Call(a);
			Stmt("[Foo, #foo] public a();", Attr(args));
		}

		[Test]
		public void MacroStmts()
		{
			Stmt("set(1, -Foo());", AsStyle(NodeStyle.Special, F.Call(set, one, F.Call(S._Negate, F.Call(Foo)))));
			Stmt("set(1, Foo());", AsStyle(NodeStyle.Special, F.Call(set, one, F.Call(Foo))));
			Stmt("set {\n  Foo();\n}", AsStyle(NodeStyle.Special, F.Call(set, F.Braces(F.Call(Foo)))));
			Stmt("set {\n  Foo();\n}", AsStyle(NodeStyle.Special, F.Call(set, F.Braces(F.Call(Foo)))), p => p.AvoidMacroSyntax = true);
			Stmt("{\n  set {\n    Foo();\n  }\n  etc;\n}", F.Braces(AsStyle(NodeStyle.Special, F.Call(set, F.Braces(F.Call(Foo)))), _("etc")));
			Stmt("protected set {\n  Foo();\n}", Attr(F.Protected, AsStyle(NodeStyle.Special, F.Call(set, F.Braces(F.Call(Foo))))));
			Stmt("set (1) {\n  Foo();\n}", AsStyle(NodeStyle.Special, F.Call(set, one, F.Braces(F.Call(Foo)))));
			Stmt("set(1, {\n  Foo();\n});", AsStyle(NodeStyle.Special, F.Call(set, one, F.Braces(F.Call(Foo)))), p => p.AvoidMacroSyntax = true);
		}

		[Test]
		public void ArrayInitializers()
		{
			// TODO: The printer's newline choices are odd. See if we can improve them.
			Stmt("int[,] Foo = new[,] { { 0\n  }, { 1, 2\n  } };", F.Call(S.Var, F.Of(S.TwoDimensionalArray, S.Int32), 
				F.Call(S.Assign, Foo, F.Call(S.New, F.Call(S.TwoDimensionalArray),
					AsStyle(NodeStyle.OldStyle, F.Braces(zero)),
					AsStyle(NodeStyle.OldStyle, F.Braces(one, two))))));
			Stmt("int[] Foo = { 0, 1, 2\n};", F.Call(S.Var, F.Of(S._Array, S.Int32),
				F.Call(S.Assign, Foo, AsStyle(NodeStyle.OldStyle, F.Call(S.ArrayInit, zero, one, two)))));
			Stmt("int[,] Foo = { { 0\n}, { 1, 2\n}\n};", F.Call(S.Var, F.Of(S.TwoDimensionalArray, S.Int32), 
				F.Call(S.Assign, Foo, F.Call(S.ArrayInit,
					AsStyle(NodeStyle.OldStyle, F.Braces(zero)),
					AsStyle(NodeStyle.OldStyle, F.Braces(one, two))))));
		}

		[Test(Fails = true)] // Parser does not yet preserve comments
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
						F.Assign(Attr(F.Trivia(S.TriviaMLCommentAfter, "the variable"), x),
									  Attr(F.Trivia(S.TriviaMLCommentAfter, "its new value"), zero)
					))));
			Stmt("// a block\n{\n  // set x to zero\n  x /*the variable*/= 0 /*its new value*/;  // x was set to zero\n} // end of block", stmt);
			stmt = Attr(F.Trivia(S.TriviaSLCommentAfter, " leave loop"), F.Call(S.Break));
			Stmt("break; // leave loop", stmt);
		}

		/* Not implementing 'if' clauses, for now
		[Test]
		public void BraceInIfClause()
		{
			// A braced block is not allowed inside an "if" clause. However we 
			// don't have a good way to prevent it.
			var stmt = Attr(F.Call(S.If, F.Call(S.Eq, a, F.Braces(b))),
			                F.Call(S.Namespace, Foo, F._Missing));
			Stmt("namespace Foo if a == @`{}`(b);", stmt);
			stmt = Attr(F.Call(S.If, F.Call(S.Eq, a, StmtStyle(F.List(b)))),
			            F.Call(S.Class, Foo));
			Stmt("class Foo if a == #(b);", stmt);
		}
		 */

		[Test]
		public void DanglingElseAmbiguity()
		{
			// if (a)
			//    if (b)
			//       c();
			// else
			//    x();
			var stmt = F.Call(S.If, a, F.Call(S.If, b, F.Call(c)), F.Call(x));
			Option(Mode.PrintBothParseFirst,
				"if (a)\n  @#if(b, c());\nelse\n  x();", 
			    "if (a)\n  {if (b)\n    c();}\nelse\n  x();", stmt, p => p.AllowExtraBraceForIfElseAmbig = true);
			stmt = F.Call(S.If, a, F.Call(S.While, Foo, F.Call(S.If, b, F.Call(c))), F.Call(x));
			Stmt("if (a)\n  while (Foo)\n    @#if(b, c());\nelse\n  x();", stmt);
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
			Stmt("Foo(out a, ref b);",                            F.Call(Foo, F.Attr(@out, a), F.Attr(@ref, b)));
			Stmt("public new partial static void Main()\n{\n}",   AddWords(F.Fn(F.Void, F.Id("Main"), F.List(), F.Braces())));
			Stmt("public new partial static void Main();",        AddWords(F.Fn(F.Void, F.Id("Main"), F.List())));
			Stmt("public new partial static int x;",              AddWords(F.Vars(F.Int32, x)));
			Stmt("public new partial static int x\n{\n  get;\n}", AddWords(F.Property(F.Int32, x, F.Braces(get))));
			Stmt("public new partial static interface Foo\n{\n}", AddWords(F.Call(S.Interface, Foo, F._Missing, F.Braces())));
			Stmt("public new partial static delegate void x();",  AddWords(F.Call(S.Delegate, F.Void, x, F.List())));
			Stmt("public new partial static alias a = Foo;",      AddWords(F.Call(S.Alias, F.Assign(a, Foo), F._Missing)));
			Stmt("public new partial static event Foo x;",        AddWords(F.Call(S.Event, Foo, x)));
			Stmt("public new partial static Foo a ==> b;",        AddWords(F.Property(Foo, a, F.Call(S.Forward, b))));
			Stmt("Foo(public new partial static int x = 0);",     F.Call(Foo, AddWords(F.Var(F.Int32, x.Name, zero))));
			Stmt("Foo([#public, #new, #partial] static x);",      F.Call(Foo, AddWords(x, false)));
			Stmt("class Foo\n{\n  [#partial] Foo();\n}",          F.Call(S.Class, Foo, F._Missing, F.Braces(
			                                                          Attr(partial, F.Call(S.Cons, F._Missing, Foo, F.List())))));
			Stmt("class Foo\n{\n  partial this();\n}",            F.Call(S.Class, Foo, F._Missing, F.Braces(
			                                                          Attr(partialWA, F.Call(S.Cons, F._Missing, F.Id(S.This), F.List())))));
			Stmt("public new partial static this();",             AddWords(F.Call(S.Cons, F._Missing, F.Id(S.This), F.List())));
			Stmt("[#public, #new] partial static break;",         AddWords(F.Call(S.Break)));
			Stmt("[#public, #new] partial static return x;",      AddWords(F.Call(S.Return, x)));
			Stmt("[#public, #new] partial static goto case x;",   AddWords(F.Call(S.GotoCase, x)));
			Stmt("[#public, #new, #partial] static if (Foo)\n  Foo();", AddWords(F.Call(S.If, Foo, F.Call(Foo)), false));
			Stmt("[#public, #new] partial static try {\n} catch {\n}",  AddWords(F.Call(S.Try, F.Braces(), F.Call(S.Catch, F._Missing, F.Braces()))));
			Stmt("[#public, #new] partial static while (x)\n  Foo();",  AddWords(F.Call(S.While, x, F.Call(Foo))));
			Stmt("[#public, #new] partial static Foo:",           AddWords(F.Call(S.Label, Foo)));
			Stmt("[#public, #new, #partial] static new Foo();",   AddWords(F.Call(S.New, F.Call(Foo)), false));
			Stmt("[#public, #new, #partial] static x = 0;",       AddWords(F.Assign(x, zero), false));
			Stmt("[#public, #new, #partial] static Foo(x = 0);",  AddWords(F.Call(Foo, F.Assign(x, zero)), false));
			Stmt("[#public, #new, #partial] static Foo(x = 0);",  AddWords(F.Call(Foo, F.Assign(x, zero)), false));
			Stmt("[#public, #new, #partial] static get ==> b;",   AddWords(Attr(trivia_forwardedProperty, F.Call(get, F.Call(S.Forward, b))), false));
			Stmt("[#public, #new, #partial] static ;",            AddWords(F._Missing, false));
			Stmt("this int x;",                                   F.Vars(F.Int32, x).PlusAttr(F.@this));
			Stmt("[this] a(b);",                                  F.Call(a, b).PlusAttr(F.@this));
		}

		[Test]
		public void ExpressionAsMethodBody()
		{
			Token[] xToken = new[] { new Token((int)TokenType.Id, 0, 0, 0, x.Name) };
			LNode def = F.Fn(F.Void, Foo, F.List(), F.Literal(new TokenTree(F.File, (ICollection<Token>) xToken)));
			LNode prop = F.Call(S.Property, F.Void, Foo, F.Literal(new TokenTree(F.File, (ICollection<Token>)xToken)));
			Stmt("void Foo() => @[ x ];", def);
			Stmt("void Foo => @[ x ];", prop);
			Stmt("partial void Foo() => @[ x ];", Attr(partialWA, def));
			Stmt("partial void Foo => @[ x ];", Attr(partialWA, prop));
			Stmt("Foo.a Foo() => @[ x ];", F.Fn(F.Dot(Foo, a), Foo, F.List(), F.Literal(new TokenTree(F.File, (ICollection<Token>)xToken))));
			Stmt("Foo Foo.a() => @[ x ];", F.Fn(Foo, F.Dot(Foo, a), F.List(), F.Literal(new TokenTree(F.File, (ICollection<Token>)xToken))));
			// Currently supported. Not sure if it'll stay that way.
			Stmt("void Foo() @[ x ];", def, Mode.ParseOnly);
		}

		// Stuff that is intentionally left broken for the time being
		[Test(Fails = true)]
		public void TODO()
		{
			Stmt("var a = (Foo ? b = c as Foo? : 0);", 
				F.Var(F._Missing, F.Call(S.Assign, a, F.InParens(
					F.Call(S.QuestionMark, Foo,
						F.Call(S.Assign, b, F.Call(S.As, c, F.Of(S.QuestionMark, Foo.Name))), zero)))));
		}
	}

	////////////////////////////////////////////////////////////////////////////
	/// <summary>EC# node printer tests</summary>
	[TestFixture]
	class EcsNodePrinterTests : EcsPrinterAndParserTests
	{
		protected override void Stmt(string result, LNode input, Action<EcsNodePrinter> configure = null, bool exprMode = false, Mode mode = Mode.Both)
		{
			if (mode == Mode.ParseOnly)
				return;
			var sb = new StringBuilder();
			var printer = EcsNodePrinter.New(input, sb, "  ");
			printer.AllowChangeParenthesis = false;
			printer.NewlineOptions &= ~(NewlineOpt.AfterOpenBraceInNewExpr | NewlineOpt.BeforeCloseBraceInNewExpr);
			if (configure != null)
				configure(printer);
			if (exprMode)
				printer.PrintExpr();
			else
				printer.PrintStmt();
			AreEqual(result, sb.ToString());
		}

		[Test]
		public void RawText()
		{
			var stmt = Attr(F.Trivia(S.TriviaRawTextBefore, "Eat my shorts!"), 
				F.Trivia(S.TriviaRawTextAfter, "...then do it again!"), F._Missing);
			Stmt("Eat my shorts!;...then do it again!", stmt);
			stmt = Attr(F.Trivia(S.TriviaRawTextAfter, " // end if"), F.Call(S.If, a, F.Call(x)));
			Stmt("if (a)\n  x(); // end if", stmt);
			Stmt("if (a)\n  x();", stmt, p => p.OmitRawText = true);
			
			var raw = F.Trivia(S.RawText, "hello!");
			Stmt("x(hello!);", F.Call(x, raw));
			raw = F.Call(S.RawText, F.Literal("hello!"));
			Stmt("hello!", raw);
			raw = F.Call(raw);
			Stmt("hello!();", raw);
		}

		int _testNum;
		void CheckIsComplexIdentifier(bool? result, LNode expr)
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
			CheckIsComplexIdentifier(true, F.Of(_(S.Bracks), b));          // a[]         == #of(@`[]`,a)      ==> true
			CheckIsComplexIdentifier(true, F.Of(F.Dot(a,b),F.Dot(c,x)));   // a.b<c.x>    == #of(@.(a,b),@.(c,x)) ==> true
			CheckIsComplexIdentifier(null, F.Call(a, x));                  // a(x)                             ==> true for target
			CheckIsComplexIdentifier(null, F.Call(F.Dot(a,b), x));         // a.b(x)      == @.(a,b)(x)        ==> true for target
			CheckIsComplexIdentifier(null, F.Call(F.Of(F.Dot(a,b),c), c)); // a.b<c>(x)   == #of(@.(a,b),c)(x) ==> true for target
			CheckIsComplexIdentifier(false, F.Call(F.InParens(a), x));     // (a)(x)                           ==> false
			CheckIsComplexIdentifier(false, F.Call(F.InParens(F.Dot(a,b)),x));// (a.b)(x) == (#.(a,b))(x)      ==> false
			CheckIsComplexIdentifier(null, F.Of(F.Of(a,b),c));             // #of(a<b>,c) == #of(#of(a,b),c)   ==> false
		}

		[Test]
		public void SanitizeIdentifierTests()
		{
			AreEqual("I_aposd", EcsNodePrinter.SanitizeIdentifier("I'd"));
			AreEqual("_123",    EcsNodePrinter.SanitizeIdentifier("123"));
			AreEqual("_plus5",  EcsNodePrinter.SanitizeIdentifier("+5" ));
			AreEqual("__",      EcsNodePrinter.SanitizeIdentifier(""   ));
			AreEqual("_lt_gt",  EcsNodePrinter.SanitizeIdentifier("<>"));
		}
		[Test]
		public void StaticMethods()
		{
			AreEqual("@this",            EcsNodePrinter.PrintId(GSymbol.Get("this"), false));
			AreEqual("normal_id",        EcsNodePrinter.PrintId(GSymbol.Get("normal_id"), false));
			AreEqual("operator+",        EcsNodePrinter.PrintId(GSymbol.Get("+"), true));
			AreEqual("operator`frack!`", EcsNodePrinter.PrintId(GSymbol.Get("frack!"), true));
			AreEqual(@"@@`frack!`",      EcsNodePrinter.PrintSymbolLiteral(GSymbol.Get("frack!")));
			AreEqual(@"@@this",          EcsNodePrinter.PrintSymbolLiteral(GSymbol.Get("this")));
		}
	}

	////////////////////////////////////////////////////////////////////////////
	/// <summary>EC# parser tests</summary>
	[TestFixture]
	class EcsParserTests : EcsPrinterAndParserTests
	{
		protected override void Stmt(string text, LNode expected, Action<EcsNodePrinter> configure = null, bool exprMode = false, Mode mode = Mode.Both)
		{
			if (mode == Mode.PrintOnly)
				return;
			// This is the easy way: 
			//LNode result = EcsLanguageService.Value.ParseSingle(text, MessageSink.Console, exprMode ? ParsingService.Exprs : ParsingService.Stmts);
			// But to make debugging easier, I'll do it the long way:
			ILexer<Token> lexer = EcsLanguageService.Value.Tokenize(new UString(text), "", MessageSink.Console);
			var preprocessed = new EcsPreprocessor(lexer);
			var treeified = new TokensToTree(preprocessed, false);
			var parser = new EcsParser(treeified.Buffered(), lexer.SourceFile, MessageSink.Console);
			
			LNode result = exprMode ? parser.ExprStart(false) : parser.Stmt();

			AreEqual(TokenType.EOF, parser.LT0.Type());
			AreEqual(expected, result);
		}

		[Test]
		public void PreprocessorIfAndDefineTests()
		{
			LNode intFoo = F.Call(S.Var, F.Int32, F.Assign(Foo, zero));
			Stmt("int Foo \n #if Foo   \n <there is no foo> \n #endif \n = 0;", intFoo);
			Stmt("int Foo \n #if true  \n        = 0        \n #endif \n;", intFoo);
			Stmt("int Foo \n #if true&&false \n  = 0        \n #endif \n;", F.Call(S.Var, F.Int32, Foo));
			Stmt("#if Foo \n int Foo;  \n #else \n int Foo = 0; \n #endif", intFoo);
			Stmt("#if Foo \n int Foo;  \n #elif (!true) || true \n int Foo = 0; \n #else \n #error FAIL \n #endif", intFoo);
			Stmt("#if Foo \n int Foo;  \n #elif (!true) || true \n int Foo = 0; \n #else \n #error FAIL \n #endif", intFoo);
			Stmt("#define Foo \n #if Foo \n int Foo = 0; \n #else \n #warning FAIL \n #endif", intFoo);
			Stmt("#define Foo \n #undef Foo \n #if Foo \n #warning FAIL \n #else \n int Foo = 0; \n #endif", intFoo);
			Stmt("#if false // fake code \n #if true \n <yes> \n #else \n <no> \n #endif \n"+
			     "#else     // real code \n int Foo \n #if true \n = 0 \n #endif \n ; \n #endif", intFoo);
		}

		[Test]
		public void PreprocessorOtherTests()
		{
			LNode intFoo = F.Call(S.Var, F.Int32, F.Assign(Foo, zero));
			// Deliberately use 'Foo' which is ordinarily not a valid token
			Stmt("#region Behold, a variable named 'Foo':\n  int Foo = 0;  \n#endregion", intFoo);
			// We can also write '/*' and it will have no effect in a #region line
			Stmt("#region /*\n  int Foo = 0;  \n#endregion", intFoo);
		}

		[Test]
		public void ParserOnlyTests()
		{
			// This method is for testing miscellaneous valid inputs that the printer never prints

			// Trailing commas
			Stmt("int[] Foo = { 0, 1, 2, };", F.Call(S.Var, F.Of(S._Array, S.Int32), 
				F.Call(S.Assign, Foo, F.Call(S.ArrayInit, zero, one, two))));
			Stmt("int[,] Foo = { { 0 }, { 1, 2, }, };", F.Call(S.Var, F.Of(S.TwoDimensionalArray, S.Int32), 
				F.Call(S.Assign, Foo, F.Call(S.ArrayInit, F.Braces(zero), F.Braces(one, two)))));
			Stmt("int[,] Foo = new[,] { { 0 }, { 1, 2, }, };", F.Call(S.Var, F.Of(S.TwoDimensionalArray, S.Int32), 
				F.Call(S.Assign, Foo, F.Call(S.New, F.Call(S.TwoDimensionalArray), F.Braces(zero), F.Braces(one, two)))));

			// 2015-05-20: parsed incorrectly
			Expr("Foo[a-1]",         F.Call(S.Bracks, Foo, F.Call(S.Sub, a, one)), Mode.ParseOnly);
		}
	}
}
