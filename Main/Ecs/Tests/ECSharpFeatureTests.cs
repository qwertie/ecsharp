using System;
using System.Collections.Generic;
using System.Linq;
using Loyc.Ecs.Parser;
using Loyc.MiniTest;
using Loyc.Syntax;
using Loyc.Syntax.Lexing;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.Ecs.Tests
{
	partial class EcsPrinterAndParserTests
	{
		[Test]
		public void EcsLiteralsAndIdentifiers()
		{
			// See also: EcsLexerTests
			Expr(@"@@hello",   F.Literal(GSymbol.Get("hello")));
			Expr(@"@@int",     F.Literal(GSymbol.Get("int")));
			Expr(@"@@#int32",  F.Literal(GSymbol.Get("#int32")));
			Expr(@"@@`\t`",    F.Literal(GSymbol.Get("\t")));    // Symbols take non-verbatim backquoted strings
			Expr(@"@@`1+1`",   F.Literal(GSymbol.Get("1+1")));
			Expr(@"@@1",       F.Literal(GSymbol.Get("1")));
			Expr(@"'''hello'''", F.Literal("hello"), Mode.ParserTest);
			Expr("default(void)", F.Literal(@void.Value), Mode.PrinterTest);
			Expr("#public",  @public);
			Expr("Don't",  _("Don't"));
		}

		public void EcsPrefixOperators()
		{
			Expr("$a",     F.Call(S.Substitute, a));
			Expr("$(-$a)", F.Call(S.Substitute, F.Call(S._Negate, F.Call(S.Substitute, a))));
			Expr(".(-a)",  F.Call(S.Dot, F.Call(S._Negate, a)));
			Expr("..a", F.Call(S.DotDot, a));
			Expr("..<a", F.Call(S.DotDot, a));
			Expr("...a", F.Call(S.DotDotDot, a));
		}

		public void EcsInfixOperators()
		{
			Expr("a**b**c",      F.Call(S.Exp, F.Call(S.Exp, a, b), c));
			Expr("a + b**c - 1", F.Call(S.Sub, F.Call(S.Add, a, F.Call(S.Exp, b, c)), one));
			Expr("a..b",         F.Call(S.DotDot, a, b));
			Expr("a += b ~ c",   F.Call(S.AddSet, a, F.Call(S.NotBits, b, c)));
			Expr("a ??= b",      F.Call(S.NullCoalesceSet, a, b));
			Expr("a..b",         F.Call(S.DotDot, a, b));
			Expr("a..<b",        F.Call(S.DotDot, a, b));
			Expr("a...b",        F.Call(S.DotDotDot, a, b));
		}

		public void EcsOtherOperatorTests()
		{
			Expr("a. 2",         F.Dot(a, two));
			Expr("a::b.c. 2",    F.Dot(F.Call(S.ColonColon, a, b), c, two));
			Expr("b using Foo",  F.Call(S.UsingCast, b, Foo));
			Expr("1 `Foo` 2",    F.Call(Foo, F.Literal(1), F.Literal(2)));
			Stmt("a ??= b using Foo;", F.Call(S.NullCoalesceSet, a, F.Call(S.UsingCast, b, Foo)));
			Expr("(Foo) x",          F.Call(S.Cast, x, Foo));
			Expr("x(->Foo)",     F.Call(S.Cast, x, Foo).SetStyle(NodeStyle.Alternate));
			Expr("x(->a + b)",   F.Call(S.Cast, x, F.Call(S.Add, a, b)).SetStyle(NodeStyle.Alternate));
			Expr("x(as Foo)",    F.Call(S.As, x, Foo).SetStyle(NodeStyle.Alternate));
			Expr("x(using Foo)", F.Call(S.UsingCast, x, Foo).SetStyle(NodeStyle.Alternate));
			// Printer detects a possible ambiguity between multiplication and a pointer declaration?
			Stmt("a `*` b ? c : 0;", F.Call(S.QuestionMark, F.Call(S.Mul, a, b), c, zero));
			Stmt("a * b ? c : 0;", F.Call(S.QuestionMark, F.Call(S.Mul, a, b), c, zero), Mode.ParserTest);
		}

		[Test]
		public void EcsSimpleExprStatements()
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
			Stmt("a * b / c % 2;",       F.Call(S.Mod, F.Call(S.Div, F.Call(S.Mul, a, b), c), two), Mode.ParserTest);
			Stmt("a / b * c % 2;",       F.Call(S.Mod, F.Call(S.Mul, F.Call(S.Div, a, b), c), two));
			Expr("@`+`(a, b, c)", F.Call(S.Add, a, b, c));
			Stmt("a << 1 | b >> 1;",     F.Call(S.OrBits, F.Call(S.Shl, a, one), F.Call(S.Shr, b, one)));
			Stmt("a++ + a--;",           F.Call(S.Add, F.Call(S.PostInc, a), F.Call(S.PostDec, a)));
			Stmt("a ? b : c;",           F.Call(S.QuestionMark, a, b, c));
			Stmt("a => a + 1;",          F.Call(S.Lambda, a, F.Call(S.Add, a, one)));
			Stmt("1 + a => 2 + b => c;", F.Call(S.Add, one, F.Call(S.Lambda, a, F.Call(S.Add, two, F.Call(S.Lambda, b, c)))));
			Stmt("a is Foo ? a as Foo : b;", F.Call(S.QuestionMark, F.Call(S.Is, a, Foo), F.Call(S.As, a, Foo), b));
			Stmt("(a + b).b<c>;", F.Of(F.Dot(F.InParens(F.Call(S.Add, a, b)), b), c));
			Stmt("x::a.b;",      F.Dot(F.Call(S.ColonColon, x, a), b));
		}

		[Test]
		public void EcsSubstitutionVarDecls()
		{
			Stmt(@"$Foo x;", F.Vars(F.Call(S.Substitute, Foo), x));
			Stmt(@"$(a(b)) x;", F.Vars(F.Call(S.Substitute, F.Call(a, b)), x));
			Stmt(@"$Foo $x;", F.Vars(F.Call(S.Substitute, Foo), F.Call(S.Substitute, x)));
			Stmt(@"Foo<$x> $a;", F.Vars(F.Of(Foo, F.Call(S.Substitute, x)), F.Call(S.Substitute, a)));
			Stmt(@"$Foo<$x> $a;", F.Vars(F.Of(F.Call(S.Substitute, Foo), F.Call(S.Substitute, x)), F.Call(S.Substitute, a)));
		}

		[Test]
		public void EcsDataTypes()
		{
			// Sure, why not
			Stmt("int<decimal[]> x;",    F.Vars(F.Of(_(S.Int32), F.Of(S.Array, S.Decimal)), x));
			// Very weird case. Consider printing as "`[,]`<float>?[] x;" instead
			Stmt("float[,]?[] x;",       F.Vars(F.Of(_(S.Array), F.Of(_(S.QuestionMark), F.Of(S.TwoDimensionalArray, S.Single))), x));
		}

		[Test]
		public void EcsSimpleCallsAndAttributes()
		{
			Stmt("[Foo]\na;",             Attr(Foo, a));
			Stmt("[Foo]\na.b.c;",         Attr(Foo, F.Dot(a, b, c)));
			Stmt("[Foo]\na<b,c>;",        Attr(Foo, F.Of(a, b, c)));
			Stmt("[Foo]\na = b;",         Attr(Foo, F.Assign(a, b)));
			Stmt("#of([Foo] a, b, c);",   F.Of(Attr(Foo, a), b, c));
			Stmt("a!(b,[Foo] c);",        F.Of(a, b, Attr(Foo, c)));
			Stmt("a!(b,Foo + c);",        F.Of(a, b, F.Call(S.Add, Foo, c)));
			Stmt("#of(Foo<a>, b);",       F.Of(F.Of(Foo, a), b));
			Stmt("public a;",             F.Attr(@public, a));
			Stmt("[Foo]\npublic a(b);",    F.Attr(Foo, @public, F.Call(a, b)));
			Stmt("public #foo;",          F.Attr(@public, fooKW));
		}

		[Test]
		public void EcsVariablesWithAttributes()
		{
			Stmt("[Foo]\nstatic int a;",   F.Attr(Foo, @static, F.Vars(F.Int32, a)));
			Stmt("partial public int a;", F.Attr(partialWA, @public, F.Vars(F.Int32, a)));
			Stmt("public partial int a;", F.Attr(@public, partialWA, F.Vars(F.Int32, a)));
			Stmt("[#lock]\nstatic int a;", F.Attr(@lock, @static, F.Vars(F.Int32, a)));
			Stmt("[#public, Foo]\nint a;", F.Attr(@public, Foo, F.Vars(F.Int32, a)));
		}

		[Test]
		public void EcsExpressionsWithAttrs()
		{
			// The printer must use prefix notation if the arguments passed to an 
			// operator have attributes.
			Stmt("[Foo]\nget;",          Attr(Foo, get));
			Expr("a[[Foo] b]",          F.Call(S.IndexBracks, a, Attr(Foo, b)));
		}

		[Test]
		public void EcsAttributesInFunnyPlaces()
		{
			Expr("new int[[Foo] x, x][]", F.Call(S.New, F.Call(F.Of(_(S.TwoDimensionalArray), F.Of(S.Array, S.Int32)), Attr(Foo, x), x)));
			Stmt("goto case 1;",       F.Call(S.GotoCase, one));
			Stmt("goto case [Foo] 1;", F.Call(S.GotoCase, Attr(Foo, one)));
		}

		[Test]
		public void EcsBracesInExpr()
		{
			var stmt1 = F.Call(S.QuickBind, F.Dot(Foo, x), a);
			var stmt2 = F.Call(S.Add, F.Call(S.Mul, a, a), a);
			Expr("b + #(Foo.x=:a, a * a + a)",                F.Call(S.Add, b, F.List(stmt1, stmt2)));
			//Expr("b + #@{\n  Foo.x=:a;\n @`*`(a, a) + a;\n}", F.Call(S.Add, b, F.List(stmt1, stmt2)), Mode.ParseOnly);
			Expr("b + {\n  Foo.x=:a;\n  @`*`(a, a) + a;\n}",  F.Call(S.Add, b, F.Braces(stmt1, stmt2)));
			Expr("b + @`{}`(Foo.x=:a, a * a + a)", F.Call(S.Add, b, AsStyle(NodeStyle.PrefixNotation, F.Braces(stmt1, stmt2))));
		}

		[Test]
		public void EcsTuplesAndVarDeclsInExpressions()
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
			Stmt("(Foo a, b) = (x, 1);",  F.Assign(F.Tuple(F.Vars(Foo, a), b), x_1));
			Stmt("(#var(Foo, a) + 1, b) = (x, 1);", F.Assign(F.Tuple(F.Call(S.Add, F.Vars(Foo, a), one), b), x_1));
			Stmt("(Foo a,) = (x,);",      F.Assign(F.Tuple(F.Vars(Foo, a)), F.Tuple(x)));
			
			// TODO: drop support for this syntax in the printer.
			// The parser doesn't get it... and that's okay.
			//Stmt("(a, (Foo b)) = (x, 1);", F.Call(S.Assign, F.Tuple(a, F.InParens(F.Vars(Foo, b))), x_1));
		}

		[Test]
		public void EcsBacktick()
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
		public void EcsAliasStmts()
		{
			var public_x = Attr(@public, F.Vars(F.Int32, x));

			Stmt("alias(a = b);", F.Call(GSymbol.Get("alias"), F.Assign(a, b)));
			LNode stmt = F.Call(S.Alias, F.Assign(F.Of(_("Map"), a, b), F.Of(_("Dictionary"), a, b)), F.List());
			Stmt("alias Map<a,b> = Dictionary<a,b>;", stmt);
			Expr("#alias(Map<a,b> = Dictionary<a,b>, #())", stmt);
			stmt = F.Call(S.Alias, F.Assign(Foo, fooKW), F.List(IFoo), F.Braces(public_x));
			Stmt("alias Foo = #foo : IFoo\n{\n  public int x;\n}", stmt);
			Expr("#alias(Foo = #foo, #(IFoo), {\n  public int x;\n})", stmt);

			// An alias must have an "=" node as its first argument; other spaces
			// must have type names as their first argument.
			stmt = F.Call(S.Alias, Foo, F.List(IFoo), F.Braces(public_x));
			Stmt("#alias(Foo, #(IFoo), {\n  public int x;\n});", stmt);
		}

		[Test]
		public void EcsMissing()
		{
			Stmt(";", F.Missing);
			Action<EcsNodePrinter> oma = o => o.OmitMissingArguments = true;
			Stmt("Foo(@``);", F.Call(Foo, F.Missing), oma);
			Stmt("Foo(@``, b);", F.Call(Foo, F.Missing, b));
			Stmt("Foo(, b);", F.Call(Foo, F.Missing, b), oma);
			Stmt("Foo(a,);", F.Call(Foo, a, F.Missing), oma);
			Stmt("Foo(,);", F.Call(Foo, F.Missing, F.Missing), oma);
			Stmt("for (;;) {\n  a();\n}", F.Call(S.For, F.Missing, F.Missing, F.Missing, F.Braces(F.Call(a))));
			Stmt("for (;; @``())\n  ;", F.Call(S.For, F.Missing, F.Missing, F.Call(F.Missing), F.Missing));
		}

		[Test]
		public void EcsBlockCallVsNormalCall()
		{
			Stmt("a(Foo < T > b);", F.Call(a, F.Call(S.GT, F.Call(S.LT, Foo, T), b)));
			Stmt("a (Foo<T> b) {\n  Foo();\n}", AsStyle(NodeStyle.Special, F.Call(a,
				F.Var(F.Of(Foo, T), b), F.Braces(F.Call(Foo)))));
		}

		[Test]
		public void EcsStmtsWithAttributes()
		{
			LNode[] args = new LNode[4] { Foo, fooWA, @public, null };
			args[3] = F.Call(S.Struct, Foo, F.List(), F.Braces(F.Vars(F.String, x)));
			Stmt("[Foo]\nfoo public struct Foo\n{\n  string x;\n}", Attr(args));
			args[3] = F.Fn(F.String, Foo, F.List(), F.Braces(F.Result(x)));
			Stmt("[Foo]\nfoo public string Foo()\n{\n  x\n}", Attr(args));
			args[3] = F.Call(S.Break);
			Stmt("[Foo]\nfoo public break;", Attr(args));
			args[3] = F.Call(S.GotoCase, x);
			Stmt("[Foo]\nfoo public goto case x;", Attr(args));
			args[3] = F.Call(S.Return, one);
			Stmt("[Foo]\nfoo public return 1;", Attr(args));
			args[3] = F.Call(S.Unchecked, F.Braces(F.Assign(a, F.Call(S.Shl, b, c))));
			Stmt("[Foo]\nfoo public unchecked {\n  a = b << c;\n}", Attr(args));
			// TODO reconsider
			//args[3] = F.Call(S.If, F.Call(S.Eq, a, b), F.Call(c));
			//Stmt("[Foo, #foo] public if (a == b)\n  c();", Attr(args));
			args[3] = F.Call(S.DoWhile, F.Call(a), c);
			Stmt("[Foo]\nfoo public do\n  a();\nwhile (c);", Attr(args));
			args[3] = F.Call(S.UsingStmt, Foo, F.Braces(F.Call(a, Foo)));
			Stmt("[Foo]\nfoo public using (Foo) {\n  a(Foo);\n}", Attr(args));
			args[3] = F.Call(S.For, a, b, c, x);
			Stmt("[Foo]\nfoo public for (a; b; c)\n  x;", Attr(args));
			args[3] = F.Braces(F.Call(a));
			if (this is EcsNodePrinterTests)
				Stmt("[Foo, #foo]\npublic {\n  a();\n}", Attr(args));
			args[1] = fooKW;
			args[3] = F.Braces(F.Call(a));
			Stmt("[Foo, #foo]\npublic {\n  a();\n}", Attr(args));
			args[3] = F.Call(a);
			Stmt("[Foo, #foo]\npublic a();", Attr(args));
		}

		[Test]
		public void EcsExpressionAsMethodBody()
		{
			Token[] xToken = new[] { new Token((int)TokenType.Id, 0, 0, 0, x.Name) };
			LNode def = F.Fn(F.Void, Foo, F.List(), F.Literal(new TokenTree(F.File, (ICollection<Token>) xToken)));
			LNode prop = F.Property(F.Void, Foo, F.Literal(new TokenTree(F.File, (ICollection<Token>)xToken)));
			Stmt("void Foo() => @{ x };", def);
			Stmt("void Foo => @{ x };", prop);
			Stmt("partial void Foo() => @{ x };", Attr(partialWA, def));
			Stmt("partial void Foo => @{ x };", Attr(partialWA, prop));
			Stmt("Foo.a Foo() => @{ x };", F.Fn(F.Dot(Foo, a), Foo, F.List(), F.Literal(new TokenTree(F.File, (ICollection<Token>)xToken))));
			Stmt("Foo Foo.a() => @{ x };", F.Fn(Foo, F.Dot(Foo, a), F.List(), F.Literal(new TokenTree(F.File, (ICollection<Token>)xToken))));
			Stmt("void Foo() @{ x };", def, Mode.ParserTest);
		}

		[Test]
		public void EcsMacroStmts()
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

		LNode AddWords(LNode stmt, bool partialIsWA = true) { return stmt.PlusAttrs(@public, @new, partialIsWA ? partialWA : partial, @static); }

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
		public void EcsWordAttributes()
		{
			Stmt("Foo(out a, ref b);",                            F.Call(Foo, F.Attr(@out, a), F.Attr(@ref, b)));
			Stmt("public new partial static void Main()\n{\n}",   AddWords(F.Fn(F.Void, F.Id("Main"), F.List(), F.Braces())));
			Stmt("public new partial static void Main();",        AddWords(F.Fn(F.Void, F.Id("Main"), F.List())));
			Stmt("public new partial static int x;",              AddWords(F.Vars(F.Int32, x)));
			Stmt("public new partial static Foo x;",              AddWords(F.Vars(Foo, x)));
			Stmt("public new partial static Foo operator==;",     AddWords(F.Vars(Foo, Attr(_(S.TriviaUseOperatorKeyword), _(S.Eq)))));
			Stmt("public new partial static int x\n{\n  get;\n}", AddWords(F.Property(F.Int32, x, F.Braces(get))));
			Stmt("public new partial static interface Foo\n{\n}", AddWords(F.Call(S.Interface, Foo, F.List(), F.Braces())));
			Stmt("public new partial static delegate void x();",  AddWords(F.Call(S.Delegate, F.Void, x, F.List())));
			Stmt("public new partial static alias a = Foo;",      AddWords(F.Call(S.Alias, F.Assign(a, Foo), F.List())));
			Stmt("public new partial static event Foo x;",        AddWords(F.Call(S.Event, Foo, x)));
			Stmt("public new partial static Foo a ==> b;",        AddWords(F.Property(Foo, a, F.Call(S.Forward, b))));
			Stmt("Foo(public new partial static int x = 0);",     F.Call(Foo, AddWords(F.Var(F.Int32, x.Name, zero))));
			Stmt("Foo([#public, #new, #partial] static x);",      F.Call(Foo, AddWords(x, false)));
			Stmt("class Foo\n{\n  [#partial]\n  Foo();\n}",          F.Call(S.Class, Foo, F.List(), F.Braces(
			                                                          Attr(partial, F.Call(S.Constructor, F.Missing, Foo, F.List())))));
			Stmt("class Foo\n{\n  partial this();\n}",            F.Call(S.Class, Foo, F.List(), F.Braces(
			                                                          Attr(partialWA, F.Call(S.Constructor, F.Missing, F.Id(S.This), F.List())))));
			Stmt("public new partial static this();",             AddWords(F.Call(S.Constructor, F.Missing, F.Id(S.This), F.List())));
			Stmt("[#public, #new]\npartial static break;",         AddWords(F.Call(S.Break)));
			Stmt("[#public, #new]\npartial static return x;",      AddWords(F.Call(S.Return, x)));
			Stmt("[#public, #new]\npartial static goto case x;",   AddWords(F.Call(S.GotoCase, x)));
			Stmt("[#public, #new, #partial]\nstatic if (Foo)\n  Foo();", AddWords(F.Call(S.If, Foo, F.Call(Foo)), false));
			Stmt("[#public, #new]\npartial static try {\n} catch {\n}",  AddWords(F.Call(S.Try, F.Braces(), F.Call(S.Catch, F.Missing, F.Missing, F.Braces()))));
			Stmt("[#public, #new]\npartial static while (x)\n  Foo();",  AddWords(F.Call(S.While, x, F.Call(Foo))));
			Stmt("[#public, #new]\npartial static Foo:",           AddWords(F.Call(S.Label, Foo)));
			Stmt("[#public, #new, #partial]\nstatic new Foo();",   AddWords(F.Call(S.New, F.Call(Foo)), false));
			Stmt("[#public, #new, #partial]\nstatic x = 0;",       AddWords(F.Assign(x, zero), false));
			Stmt("[#public, #new, #partial]\nstatic Foo(x = 0);",  AddWords(F.Call(Foo, F.Assign(x, zero)), false));
			Stmt("[#public, #new, #partial]\nstatic Foo(x = 0);",  AddWords(F.Call(Foo, F.Assign(x, zero)), false));
			Stmt("[#public, #new, #partial]\nstatic get ==> b;",   AddWords(Attr(trivia_forwardedProperty, F.Call(get, F.Call(S.Forward, b))), false));
			Stmt("[#public, #new, #partial]\nstatic ;",            AddWords(F.Missing, false));
			Stmt("this int x;",                                   F.Vars(F.Int32, x).PlusAttr(F.@this));
			Stmt("this Foo x;",                                   F.Vars(Foo, x).PlusAttr(F.@this));
			Stmt("[this]\na(b);",                                  F.Call(a, b).PlusAttr(F.@this));
		}

		[Test]
		public void EcsVarAnywhere()
		{
			// An unassigned variable is allowed when an expression has attributes, such as 'out'
			Stmt("Foo(out List<Foo> x);", F.Call(Foo, F.Attr(_(S.Out), F.Var(F.Of(_("List"), Foo), x))));
			Stmt("Foo([x] List<Foo> x);", F.Call(Foo, F.Attr(x,        F.Var(F.Of(_("List"), Foo), x))));
			// Allow "var" and type keywords like "int" even in places where 
			// unassigned variable declarations are normally prohibited. This is 
			// allowed to provide extra flexibility to macros that need them.
			// Note that non-keyword types like "Foo x" are still not allowed.
			// Rationale: "List<T> x" is not allowed in arbitrary locations 
			// because it is parsed as "(List < T) > x" for backward compatibility 
			// with C#. We don't allow "Foo x" so that (1) non-generic types 
			// aren't "more priveleged" - usable in more places than generic
			// types - and (2) so that macro authors are more likely to notice 
			// the parsing problem. In case a macro author _really needs_ to 
			// allow an unassigned variable declaration, they can support 
			// "var x" and "var Foo x" (and manually remove the keyword attribute 
			// #var from the latter declaration).
			Stmt("Foo(var x, int x);", F.Call(Foo, F.Var(F.Missing, x), F.Var(F.Int32, x)));
		}

		[Test]
		public void EcsPropertyDefinitionExpr()
		{
			Stmt("this(int Foo { get; }) {}", F.Call(S.Constructor, F.Missing, F.@this, F.List(
				F.Property(F.Int32, Foo, F.Missing, F.Braces(get))), F.Braces()));
			Stmt("Foo(x, int Foo { get; } = 0);", F.Call(Foo, x,
				F.Property(F.Int32, Foo, F.Missing, F.Braces(get), zero)));
		}

		[Test]
		public void EcsMiscTests()
		{
			Stmt("using static Foo.x;", Attr(F.Id(S.Static), F.Call(S.Import, F.Dot(Foo, x))));
			Stmt("static using Foo.x;", Attr(F.Id(S.Static), F.Call(S.Import, F.Dot(Foo, x))), Mode.ParserTest);
		}
	}
}
