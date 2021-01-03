using System;
using System.Collections.Generic;
using System.Linq;
using Loyc.Ecs.Parser;
using Loyc.MiniTest;
using Loyc.Syntax;
using Loyc.Syntax.Lexing;
using S = Loyc.Ecs.EcsCodeSymbols;

namespace Loyc.Ecs.Tests
{
	partial class EcsPrinterAndParserTests
	{
		[Test]
		public void EcsLiteralsAndIdentifiers()
		{
			// See also: EcsLexerTests
			Expr(@"@@hello", F.Literal(GSymbol.Get("hello")));
			Expr(@"@@int", F.Literal(GSymbol.Get("int")));
			Expr(@"@@#int32", F.Literal(GSymbol.Get("#int32")));
			Expr(@"@@`\t`", F.Literal(GSymbol.Get("\t")));    // Symbols take non-verbatim backquoted strings
			Expr(@"@@`1+1`", F.Literal(GSymbol.Get("1+1")));
			Expr(@"@@1", F.Literal(GSymbol.Get("1")));
			// Not yet supported in printer (which will use @"...")
			Expr(@"'''hello'''", F.Literal("hello").SetBaseStyle(NodeStyle.TQStringLiteral), Mode.ParserTest);
			Expr("default(void)", F.Literal(@void.Value), Mode.PrinterTest);
			Expr("#public", @public);
			Expr("Don't", _("Don't"));
		}

		[Test]
		public void EcsPrefixOperators()
		{
			Expr("$a", F.Call(S.Substitute, a));
			Expr("$(-$a)", F.Call(S.Substitute, F.Call(S._Negate, F.Call(S.Substitute, a))));
			Expr(".(-a)", F.Call(S.Dot, F.Call(S._Negate, a)));
			Expr("..a", F.Call(S.DotDot, a));
			Expr("..<a", F.Call(S.DotDot, a), mode: Mode.ParserTest);
			Expr("...a", F.Call(S.DotDotDot, a));
		}

		[Test]
		public void EcsInfixOperators()
		{
			Expr("a**b**c", F.Call(S.Exp, F.Call(S.Exp, a, b), c));
			Expr("a + b**c - 1", F.Call(S.Sub, F.Call(S.Add, a, F.Call(S.Exp, b, c)), one));
			Expr("a..b", F.Call(S.DotDot, a, b));
			Expr("a += b ~ c", F.Call(S.AddAssign, a, F.Call(S.NotBits, b, c)));
			Expr("a ??= b", F.Call(S.NullCoalesceAssign, a, b));
			Expr("a..b", F.Call(S.DotDot, a, b));
			Expr("a..<b", F.Call(S.DotDot, a, b), mode: Mode.ParserTest);
			Expr("a...b", F.Call(S.DotDotDot, a, b));
		}

		[Test]
		public void EcsOtherOperatorTests()
		{
			Expr("a. 2", F.Dot(a, two));
			Expr("a::b.c. 2", F.Dot(F.Call(S.ColonColon, a, b), c, two));
			Expr("1 `Foo` 2", F.Call(Foo, F.Literal(1), F.Literal(2)).SetStyle(NodeStyle.Operator));
			Stmt("a ??= b using Foo;", F.Call(S.NullCoalesceAssign, a, F.Call(S.UsingCast, b, Foo)));
			Expr("(Foo) x", F.Call(S.Cast, x, Foo));
			Expr("x(->Foo)", F.Call(S.Cast, x, Foo).SetStyle(NodeStyle.Alternate));
			Expr("x(->a<b>)", F.Call(S.Cast, x, F.Of(a, b)).SetStyle(NodeStyle.Operator | NodeStyle.Alternate));
			Expr("x as Foo", F.Call(S.As, x, Foo));
			Expr("x(as Foo)", F.Call(S.As, x, Foo).SetStyle(NodeStyle.Alternate));
			Expr("x using Foo", F.Call(S.UsingCast, x, Foo));
			Expr("x(using Foo)", F.Call(S.UsingCast, x, Foo).SetStyle(NodeStyle.Alternate));
			// Printer detects a possible ambiguity between multiplication and a pointer declaration?
			Stmt("a `'*` b ? x : 0;", F.Call(S.QuestionMark, F.Call(S.Mul, a, b), x, zero));
			Stmt("a * b ? c : 0;", F.Call(S.QuestionMark, F.Call(S.Mul, a, b), c, zero), Mode.ParserTest);
		}

		[Test]
		public void EcsNewOperatorTests()
		{
			Expr("a(x) |> b(#)", F.Call(S.ForwardPipeArrow, F.Call(a, x), F.Call(b, _("#"))));
			Expr("a(x) ?|> b(#)", F.Call(S.NullForwardPipeArrow, F.Call(a, x), F.Call(b, _("#"))));
			Expr("a ?? b |> c = #", F.Call(S.ForwardPipeArrow, F.Call(S.NullCoalesce, a, b), F.Assign(c, _("#"))));
			Expr("a ?? b ?|> c = #", F.Call(S.NullForwardPipeArrow, F.Call(S.NullCoalesce, a, b), F.Assign(c, _("#"))));
			Expr("a = b |> c", F.Assign(a, F.Call(S.ForwardPipeArrow, b, c)));
			Expr("x = a ?|> b ?> Foo", F.Assign(x, F.Call(S.NullForwardPipeArrow, F.Call(S.NullForwardPipeArrow, a, b), Foo)), Mode.ParserTest);
			Expr("x = a ?|> b ?|> c", F.Assign(x, F.Call(S.NullForwardPipeArrow, F.Call(S.NullForwardPipeArrow, a, b), c)));
			Expr("a = b <=> c", F.Assign(a, F.Call(S.Compare, b, c)));
			Expr("a `mod` b <=> c == 0", F.Call(S.Eq, F.Call(S.Compare, Operator(F.Call(_("mod"), a, b)), c), zero));
			Expr("a(x) |=> b", F.Call(S.ForwardAssign, F.Call(a, x), b));
			var node = F.Call(S.ForwardNullCoalesceAssign, F.Call(S.ForwardNullCoalesceAssign, F.Call(a, x), b), c);
			Expr("a(x) ?|=> b ?=> c", node, Mode.ParserTest);
			Expr("a(x) ?|=> b ?|=> c", node);
		}

		[Test]
		public void EcsSimpleExprStatements()
		{
			// These are similar/identical to above expressions, but parsed/printed 
			// as statements. This test ensures that statement-related logic (such 
			// as the rule against printing "a * b" as a statement) does not 
			// interfere with expressions that can be printed as statements.
			Stmt("a;", a);
			Stmt("(a);", F.InParens(a));
			Stmt("((a));", F.InParens(F.InParens(a)));
			Stmt("a(b);", F.Call(a, b));
			Stmt("a.b.c;", F.Dot(a, b, c));
			Stmt("a.b(c);", F.Call(F.Dot(a, b), c));
			Stmt("a<b>.c;", F.Dot(F.Of(a, b), c));
			Stmt("a.b<c>;", F.Dot(a, F.Of(b, c)));
			Stmt("a<b, c>;", F.Of(a, b, c));
			Stmt("a<b>(c);", F.Call(F.Of(a, b), c));
			Stmt("a?.b(c);", F.Call(S.NullDot, a, F.Call(b, c)));
			Stmt("a + b + c;", F.Call(S.Add, F.Call(S.Add, a, b), c));
			// To be safe, the printer treats 'a * b' like a pointer decl so it won't print it
			Stmt("@`'*`(a, b) / c % 2;", F.Call(S.Mod, F.Call(S.Div, F.Call(S.Mul, a, b), c), two));
			Stmt("a * b / c % 2;", F.Call(S.Mod, F.Call(S.Div, F.Call(S.Mul, a, b), c), two), Mode.ParserTest);
			Stmt("a / b * c % 2;", F.Call(S.Mod, F.Call(S.Mul, F.Call(S.Div, a, b), c), two));
			Expr("@`'+`(a, b, c)", F.Call(S.Add, a, b, c));
			Stmt("a << 1 | b >> 1;", F.Call(S.OrBits, F.Call(S.Shl, a, one), F.Call(S.Shr, b, one)));
			Stmt("a++ + a--;", F.Call(S.Add, F.Call(S.PostInc, a), F.Call(S.PostDec, a)));
			Stmt("a ? b : c;", F.Call(S.QuestionMark, a, b, c));
			Stmt("a => a + 1;", F.Call(S.Lambda, a, F.Call(S.Add, a, one)));
			Stmt("1 + a => 2 + b => c;", F.Call(S.Add, one, F.Call(S.Lambda, a, F.Call(S.Add, two, F.Call(S.Lambda, b, c)))));
			Stmt("a is Foo ? a as Foo : b;", F.Call(S.QuestionMark, F.Call(S.Is, a, Foo), F.Call(S.As, a, Foo), b));
			Stmt("(a + b).b<c>;", F.Dot(F.InParens(F.Call(S.Add, a, b)), F.Of(b, c)));
			Stmt("x::a.b;", F.Dot(F.Call(S.ColonColon, x, a), b));
		}

		[Test]
		public void PrecedenceChecks()
		{
			// The parser currently allows the Swift range operator ..< but translates it to ..
			// and the precedence no longer matches Swift (changed 2020/05)
			Expr("a..<b == b...c", F.Call(S.Eq, F.Call("'..", a, b), F.Call("'...", b, c)), mode: Mode.ParserTest);
			Expr("a..<b + c...x", F.Call(S.Add, F.Call("'..", a, b), F.Call("'...", c, x)), mode: Mode.ParserTest);
			Expr("a..<([] b + c)...x", F.Call("'...", F.Call("'..", a, F.Call(S.Add, b, c)), x), mode: Mode.ParserTest);
			Expr("a..b.c...x", F.Call(S.DotDotDot, F.Call(S.DotDot, a, F.Dot(b, c)), x));
			// TODO more?
		}

		[Test]
		public void EcsSubstitutionVarDecls()
		{
			Stmt(@"$Foo x;", F.Vars(F.Call(S.Substitute, Foo), x));
			Stmt(@"Foo $x;", F.Var(Foo, Operator(F.Call(S.Substitute, x))));
			Stmt(@"$(a(b)) x;", F.Vars(F.Call(S.Substitute, F.Call(a, b)), x));
			Stmt(@"$Foo $x;", F.Vars(F.Call(S.Substitute, Foo), F.Call(S.Substitute, x)));
			Stmt(@"Foo<$x> $a;", F.Vars(F.Of(Foo, F.Call(S.Substitute, x)), F.Call(S.Substitute, a)));
			Stmt(@"$Foo<$x> $a;", F.Vars(F.Of(F.Call(S.Substitute, Foo), F.Call(S.Substitute, x)), F.Call(S.Substitute, a)));
		}

		[Test]
		public void EcsDataTypes()
		{
			// Sure, why not
			Stmt("int<decimal[]> x;", F.Vars(F.Of(_(S.Int32), F.Of(S.Array, S.Decimal)), x));
			// Very weird case. Consider printing as "`[,]`<float>?[] x;" instead
			Stmt("float[,]?[] x;", F.Vars(F.Of(_(S.Array), F.Of(_(S.QuestionMark), F.Of(S.TwoDimensionalArray, S.Single))), x));
		}

		[Test]
		public void EcsSimpleCallsAndAttributes()
		{
			Stmt("[Foo] a;", Attr(Foo, a));
			Stmt("[Foo] a.b.c;", Attr(Foo, F.Dot(a, b, c)));
			Stmt("[Foo] a<b, c>;", Attr(Foo, F.Of(a, b, c)));
			Stmt("[Foo] a = b;", Attr(Foo, F.Assign(a, b)));
			Stmt("@'of([Foo] a, b, c);", F.Of(Attr(Foo, a), b, c));
			Stmt("a!(b, [Foo] c);", F.Of(a, b, Attr(Foo, c)));
			Stmt("a!(b, Foo + c);", F.Of(a, b, F.Call(S.Add, Foo, c)));
			Stmt("@'of(Foo<a>, b);", F.Of(F.Of(Foo, a), b));
			Stmt("public a;", F.Attr(@public, a));
			Stmt("[Foo] public a(b);", F.Attr(Foo, @public, F.Call(a, b)));
			Stmt("public #foo;", F.Attr(@public, fooKW));
		}

		[Test]
		public void EcsVariablesWithAttributes()
		{
			Stmt("[Foo] static int a;", F.Attr(Foo, @static, F.Vars(F.Int32, a)));
			Stmt("partial public int a;", F.Attr(partialWA, @public, F.Vars(F.Int32, a)));
			Stmt("public partial int a;", F.Attr(@public, partialWA, F.Vars(F.Int32, a)));
			Stmt("[#lock] static int a;", F.Attr(@lock, @static, F.Vars(F.Int32, a)));
			Stmt("[#public, Foo] int a;", F.Attr(@public, Foo, F.Vars(F.Int32, a)));
		}

		[Test]
		public void EcsExpressionsWithAttrs()
		{
			// The printer must use prefix notation if the arguments passed to an 
			// operator have attributes.
			Stmt("[Foo] get;", Attr(Foo, get));
			Expr("a[[Foo] b]", F.Call(S.IndexBracks, a, Attr(Foo, b)));
		}

		[Test]
		public void EcsAttributesInFunnyPlaces()
		{
			Expr("new int[[Foo] x, x][]", F.Call(S.New, F.Call(F.Of(_(S.TwoDimensionalArray), F.Of(S.Array, S.Int32)), Attr(Foo, x), x)));
			Stmt("goto case 1;", F.Call(S.GotoCase, one));
			Stmt("goto case [Foo] 1;", F.Call(S.GotoCase, Attr(Foo, one)));
			Expr("new Foo(x) { [a] b = c }",
										  F.Call(S.New, F.Call(Foo, x), Attr(a, F.Assign(b, c))));
			Option(Mode.PrintBothParseFirst, "@'new([#foo] Foo(x), a);", "new Foo(x) { a };",
										  F.Call(S.New, Attr(fooKW, F.Call(Foo, x)), a), p => p.DropNonDeclarationAttributes = true);
		}

		[Test]
		public void EcsBracesInExpr()
		{
			var stmt1 = F.Call(S.QuickBind, F.Dot(Foo, x), a);
			var stmt2 = F.Call(S.Add, F.Call(S.Mul, a, a), a);
			Expr("b + #(Foo.x=:a, a * a + a)", F.Call(S.Add, b, F.List(stmt1, stmt2)));
			//Expr("b + #@{\n  Foo.x=:a;\n @`*`(a, a) + a;\n}", F.Call(S.Add, b, F.List(stmt1, stmt2)), Mode.ParseOnly);
			Expr("x + {\n  Foo.x=:a;\n  @`'*`(a, a) + a;\n}", F.Call(S.Add, x, F.Braces(stmt1, stmt2)));
			Expr("2 + @`'{}`(Foo.x=:a, a * a + a)",
				F.Call(S.Add, two, AsStyle(NodeStyle.PrefixNotation, BracesOnOneLine(stmt1, stmt2))));
		}

		[Test]
		public void EcsTuplesAndVarDeclsInExpressions()
		{
			Stmt("Foo a, b, c;", F.Vars(Foo, a, b, c));
			Stmt("Foo? a, b, c;", F.Vars(F.Of(_(S.QuestionMark), Foo), a, b, c));
			Stmt("(#var(Foo, a, b, c));", F.InParens(F.Vars(Foo, a, b, c)));
			Stmt("(Foo a) = x;", F.Assign(F.InParens(F.Vars(Foo, a)), x));
			Stmt("(Foo a) => a;", F.Call(S.Lambda, F.InParens(F.Vars(Foo, a)), a));
			Stmt("([] Foo a) + x;", F.Call(S.Add, F.Vars(Foo, a), x));
			var x_1 = F.Tuple(x, one);
			Stmt("(a, b) = (x, 1);", F.Assign(F.Tuple(a, b), x_1));
			Stmt("(a,) = (x,);", F.Assign(F.Tuple(a), F.Tuple(x)));
			Stmt("(a, Foo b) = (x, 1);", F.Assign(F.Tuple(a, F.Vars(Foo, b)), x_1));
			Stmt("(Foo a, b) = (x, 1);", F.Assign(F.Tuple(F.Vars(Foo, a), b), x_1));
			Stmt("(([] Foo a) + 1, b) = (x, 1);", F.Assign(F.Tuple(F.Call(S.Add, F.Vars(Foo, a), one), b), x_1));
			Stmt("(Foo a,) = (x,);", F.Assign(F.Tuple(F.Vars(Foo, a)), F.Tuple(x)));

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
		public void EcsGenericProperties()
		{
			LNode stmt;
			stmt = Attr(F.Private, F.Property(F.Of(Foo, T), F.Of(F.@this, T), F.List(F.Var(T, x)), BracesOnOneLine(get, set)));
			Stmt("private Foo<T> this<T>[T x] { get; set; }", stmt);

			var T_where = Attr(F.Call(S.Where, @class), T);
			stmt = Attr(F.Private, F.Property(F.Of(Foo, T), F.Of(F.@this, T_where), F.List(F.Var(T, x)), BracesOnOneLine(get, set)));
			Stmt("private Foo<T> this<T>[T x] where T: class { get; set; }", stmt);

			// where clause requires arg list
			stmt = F.Property(Foo, F.Of(x, T_where), F.List(), BracesOnOneLine(get, set));
			Stmt("Foo x<T>[] where T: class { get; set; }", stmt);
		}

		[Test]
		public void EcsPartlyEmptyTypeParameters()
		{
			Stmt("Dictionary<a, > x;", F.Var(F.Of(_("Dictionary"), a, F.Missing), x));
			Expr("[] Dictionary<, b> x", F.Var(F.Of(_("Dictionary"), F.Missing, b), x));
			Expr("typeof(Map<, Foo>) != typeof(Map<a.b, >)",
				F.Call(S.NotEq, F.Call(S.Typeof, F.Of(_("Map"), F.Missing, Foo)), F.Call(S.Typeof, F.Of(_("Map"), F.Dot(a, b), F.Missing))));
		}

		[Test]
		public void EcsForwardedMethodsAndProperties()
		{
			LNode stmt;
			stmt = F.Property(F.Int32, Foo, F.Call(S.Forward, x));
			Stmt("int Foo ==> x;", stmt);

			stmt = F.Property(F.Int32, Foo, F.Call(S.Forward, F.List(a, b)));
			Stmt("int Foo ==> #(a, b);", stmt);

			stmt = F.Property(F.Int32, Foo, F.Braces(
					  Attr(trivia_forwardedProperty, F.Call(get, F.Call(S.Forward, x)))));
			Stmt("int Foo {\n  get ==> x;\n}", stmt);
			stmt = F.Property(F.Int32, Foo, F.Braces(F.Call(get, F.Call(S.Forward, a, b))));
			Stmt("int Foo {\n  get(@`'==>`(a, b));\n}", stmt);
			stmt = F.Property(F.Int32, Foo, F.Braces(
								Attr(trivia_forwardedProperty, F.Call(get, F.Call(S.Forward, a, b)))));
			Stmt("int Foo {\n  get(@`'==>`(a, b));\n}", stmt, Mode.PrinterTest);
		}

		[Test]
		public void EcsAliasStmts()
		{
			var public_x = Attr(@public, F.Vars(F.Int32, x));

			Stmt("alias(a = b);", F.Call(GSymbol.Get("alias"), F.Assign(a, b)));
			LNode stmt = F.Call(S.Alias, F.Assign(F.Of(_("Map"), a, b), F.Of(_("Dictionary"), a, b)), F.List());
			Stmt("alias Map<a, b> = Dictionary<a, b>;", stmt);
			Expr("#alias(Map<a, b> = Dictionary<a, b>, #())", stmt);
			stmt = F.Call(S.Alias, F.Assign(Foo, fooKW), F.List(IFoo), F.Braces(public_x));
			Stmt("alias Foo = #foo : IFoo {\n  public int x;\n}", stmt);
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
			Action<EcsPrinterOptions> oma = o => o.OmitMissingArguments = true;
			Stmt("Foo(@``);", F.Call(Foo, F.Missing), oma);
			Stmt("Foo(@``, b);", F.Call(Foo, F.Missing, b));
			Stmt("Foo(, b);", F.Call(Foo, F.Missing, b), oma);
			Stmt("Foo(a,);", F.Call(Foo, a, F.Missing), oma);
			Stmt("Foo(,);", F.Call(Foo, F.Missing, F.Missing), oma);
			Stmt("for (;;) {\n  a();\n}", F.Call(S.For, F.List(), F.Missing, F.List(), F.Braces(F.Call(a))));
			Stmt("for (; @``();)\n  ;", F.Call(S.For, F.List(), F.Call(F.Missing), F.List(), ChildStmt(F.Missing)));
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
			Stmt("[Foo] foo public struct Foo {\n  string x;\n}", Attr(args));
			args[3] = F.Fn(F.String, Foo, F.List(), F.Braces(F.Result(x)));
			Stmt("[Foo] foo public string Foo() {\n  x\n}", Attr(args));
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
			args[3] = F.Call(S.DoWhile, ChildStmt(F.Call(a)), OnNewLine(c));
			Stmt("[Foo] foo public do\n  a();\nwhile (c);", Attr(args));
			args[3] = F.Call(S.UsingStmt, Foo, F.Braces(F.Call(a, Foo)));
			Stmt("[Foo, [@`%wordAttribute`] #foo] public using (Foo) {\n  a(Foo);\n}", Attr(args), Mode.PrinterTest);
			args[3] = F.Call(S.For, F.List(a), b, F.List(c), ChildStmt(x));
			Stmt("[Foo] foo public for (a; b; c)\n  x;", Attr(args));
			args[3] = F.Braces(F.Call(a));
			Stmt("[Foo, [@`%wordAttribute`] #foo] public {\n  a();\n}", Attr(args), Mode.PrinterTest);
			args[1] = fooKW;
			args[3] = F.Braces(F.Call(a));
			Stmt("[Foo, #foo] public {\n  a();\n}", Attr(args));
			args[3] = F.Call(a);
			Stmt("[Foo, #foo] public a();", Attr(args));
		}

		[Test]
		public void EcsExpressionAsMethodBody()
		{
			Token[] xToken = new[] { new Token((int)TokenType.Id, 0, 0, 0, x.Name) };
			LNode def = F.Fn(F.Void, Foo, F.List(), F.Literal(new TokenTree(F.File, (ICollection<Token>)xToken)));
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
			Stmt("Foo(out a, ref b);", F.Call(Foo, F.Attr(@out, a), F.Attr(@ref, b)));
			Stmt("public new partial static void Main() { }", AddWords(F.Fn(F.Void, F.Id("Main"), F.List(), F.Braces())));
			Stmt("public new partial static void Main();", AddWords(F.Fn(F.Void, F.Id("Main"), F.List())));
			Stmt("public new partial static int x;", AddWords(F.Vars(F.Int32, x)));
			Stmt("public new partial static Foo x;", AddWords(F.Vars(Foo, x)));
			Stmt("public new partial static Foo operator==;", AddWords(F.Vars(Foo, Attr(_(S.TriviaUseOperatorKeyword), _(S.Eq)))));
			Stmt("public new partial static int x { get; }", AddWords(F.Property(F.Int32, x, BracesOnOneLine(get))));
			Stmt("public new partial static interface Foo { }", AddWords(F.Call(S.Interface, Foo, F.List(), F.Braces())));
			Stmt("public new partial static delegate void x();", AddWords(F.Call(S.Delegate, F.Void, x, F.List())));
			Stmt("public new partial static alias a = Foo;", AddWords(F.Call(S.Alias, F.Assign(a, Foo), F.List())));
			Stmt("public new partial static event Foo x;", AddWords(F.Call(S.Event, Foo, x)));
			Stmt("public new partial static Foo a ==> b;", AddWords(F.Property(Foo, a, F.Call(S.Forward, b))));
			Stmt("Foo(public new partial static int x = 0);", F.Call(Foo, AddWords(F.Var(F.Int32, x.Name, zero))));
			Stmt("Foo([#public, #new, #partial] static x);", F.Call(Foo, AddWords(x, false)));
			Stmt("class Foo {\n  [#partial] Foo();\n}", F.Call(S.Class, Foo, F.List(), F.Braces(
																	  Attr(partial, F.Call(S.Constructor, F.Missing, Foo, F.List())))));
			Stmt("class Foo {\n  partial this();\n}", F.Call(S.Class, Foo, F.List(), F.Braces(
																	  Attr(partialWA, F.Call(S.Constructor, F.Missing, F.Id(S.This), F.List())))));
			Stmt("public new partial static this();", AddWords(F.Call(S.Constructor, F.Missing, F.Id(S.This), F.List())));
			Stmt("[#public, #new] partial static break;", AddWords(F.Call(S.Break)));
			Stmt("[#public, #new] partial static return x;", AddWords(F.Call(S.Return, x)));
			Stmt("[#public, #new] partial static goto case x;", AddWords(F.Call(S.GotoCase, x)));
			Stmt("[#public, #new, #partial] static if (Foo)\n  Foo();", AddWords(F.Call(S.If, Foo, ChildStmt(F.Call(Foo))), false));
			Stmt("[#public, #new] partial static try { } catch { }", AddWords(F.Call(S.Try, F.Braces(), F.Call(S.Catch, F.Missing, F.Missing, F.Braces()))));
			Stmt("[#public, #new] partial static while (x)\n  Foo();", AddWords(F.Call(S.While, x, ChildStmt(F.Call(Foo)))));
			Stmt("[#public, #new] partial static Foo:", AddWords(F.Call(S.Label, Foo)));
			Stmt("[#public, #new, #partial] static new Foo();", AddWords(F.Call(S.New, F.Call(Foo)), false));
			Stmt("[#public, #new, #partial] static x = 0;", AddWords(F.Assign(x, zero), false));
			Stmt("[#public, #new, #partial] static Foo(x = 0);", AddWords(F.Call(Foo, F.Assign(x, zero)), false));
			Stmt("[#public, #new, #partial] static Foo(x = 0);", AddWords(F.Call(Foo, F.Assign(x, zero)), false));
			Stmt("[#public, #new, #partial] static get ==> b;", AddWords(Attr(trivia_forwardedProperty, F.Call(get, F.Call(S.Forward, b))), false));
			Stmt("[#public, #new, #partial] static ;", AddWords(F.Missing, false));
			Stmt("this int x;", F.Vars(F.Int32, x).PlusAttr(F.@this));
			Stmt("this Foo x;", F.Vars(Foo, x).PlusAttr(F.@this));
			Stmt("[this] a(b);", F.Call(a, b).PlusAttr(F.@this));
		}

		[Test]
		public void EcsConstructorsAndDestructors()
		{
			LNode int_x = F.Vars(F.Int32, x), list_int_x = F.List(int_x), x_mul_x = F.Call(S.Mul, x, x), stmt;
			stmt = F.Call(S.Constructor, F.Missing, _(S.This), list_int_x, F.Braces(F.Call(_(S.This), x, one), F.Assign(a, x)));
			Stmt("this(int x)\n   : this(x, 1) {\n  a = x;\n}", stmt);
			Expr("#cons(@``, this, #([] int x), {\n  #this(x, 1);\n  a = x;\n})", stmt);
			stmt = F.Call(S.Constructor, F.Missing, Foo, list_int_x, F.Braces(F.Call(_(S.Base), x), F.Assign(b, x)));
			Stmt("Foo(int x)\n   : base(x) {\n  b = x;\n}", stmt);
			Expr("#cons(@``, Foo, #([] int x), {\n  base(x);\n  b = x;\n})", stmt);
			var destructor = F.Fn(F.Missing, F.Call(S._Destruct, Foo), F.List(), F.Braces());
			stmt = F.Call(S.Class, Foo, F.List(), F.Braces(destructor));
			Stmt("class Foo {\n  ~Foo() { }\n}", stmt);
			Expr("#class(Foo, #(), {\n  ~Foo() { }\n})", stmt, Mode.Both | Mode.ExpectAndDropParserError);
			Expr("#class(Foo, #(), {\n  #fn(@``, ~Foo, #(), { });\n})", stmt, Mode.ParserTest);
			// This should be parsed as a destructor despite the fact that
			// #result(~(Foo {})) is a potential interpretation.
			stmt = destructor;
			Stmt("~Foo() { }", stmt, Mode.Both | Mode.ExpectAndDropParserError);
			Expr("#fn(@``, ~Foo, #(), { })", destructor);
			stmt = F.Fn(F.Missing, F.Call(S._Negate, Foo), F.List(), F.Braces());
			Stmt("#fn(@``, -Foo, #(), { });", stmt);
			stmt = F.Call(S.Class, Foo, F.List(), F.Braces(F.Fn(F.Missing, F.Call(S._Negate, Foo), F.List(), F.Braces())));
			Stmt("class Foo {\n  #fn(@``, -Foo, #(), { });\n}", stmt);
			// You can use this syntax, but the parser has to encode it as a braced 
			// block in the syntax tree, because there is no other way to encode 
			// both statements in the method body.
			stmt = F.Call(S.Constructor, F.Missing, _(S.This), list_int_x, F.Braces(F.Call(_(S.Base), x), F.Assign(a, x)));
			Stmt("this(int x)\n   : base(x) =>\n  a = x;", stmt, Mode.ParserTest);
			// This doesn't work because Foo(...) => x; is parsed as an expression.
			// That would be mostly okay for passing code through LeMP, except that 
			// the printer drops the 'public' attribute when it is operating in 
			// plain C# mode. Perhaps this should be parsed and printed as a 
			// constructor ?
			//stmt = F.Call(S.Class, Foo, F.List(), F.Braces(Attr(@public, F.Call(S.Constructor, F.Missing, Foo, list_int_x, x))));
			//Stmt("class Foo {\n  public Foo(int x) => x;\n}", stmt);
		}

		[Test]
		public void EcsConstructorTrivia()
		{
			LNode int_x = F.Vars(F.Int32, x), list_int_x = F.List(int_x), x_mul_x = F.Call(S.Mul, x, x), stmt;
			stmt = F.Call(S.Constructor, F.Missing, _(S.This), list_int_x, OnNewLine(F.Braces(F.Call(_(S.This), x, one), F.Assign(a, x))));
			Stmt("this(int x)\n   : this(x, 1)\n{\n  a = x;\n}", stmt);
			stmt = F.Call(S.Constructor, F.Missing, _(S.This), list_int_x, F.Braces(AppendStmt(F.Call(_(S.This), x, one)), F.Assign(a, x)));
			Stmt("this(int x) : this(x, 1) {\n  a = x;\n}", stmt);
		}

		[Test]
		public void EcsTemplateArgs()
		{
			var stmt = Attr(@static, F.Fn(Foo, F.Of(Foo, F.Call(S.Substitute, T)), F.List()));
			Stmt(@"static Foo Foo<$T>();", stmt);
			// TODO consider adding more tests here
		}

		[Test]
		public void EcsOperatorDefinitions()
		{
			LNode cast = _(S.Cast), operator_cast = Attr(trivia_operator, cast);
			LNode Foo_a = F.Vars(Foo, a), Foo_b = F.Vars(Foo, b);
			LNode stmt = Attr(@static, _(S.Implicit), F.Fn(T, operator_cast, F.List(Foo_a), F.Braces()));
			Stmt("static implicit operator T(Foo a) { }", stmt);

			stmt = Attr(@static, _(S.Explicit),
						F.Fn(F.Of(Foo, T), F.Of(operator_cast, F.Call(S.Substitute, T)),
							  F.List(F.Vars(F.Of(_("Bar"), T), b))));
			Stmt(@"static explicit Foo<T> operator`'cast`<$T>(Bar<T> b);", stmt);
			Expr(@"static explicit #fn(Foo<T>, operator`'cast`<$T>, #([] Bar<T> b))", stmt);
			stmt = F.Fn(F.Bool, Attr(trivia_operator, _("when")), F.List(Foo_a, Foo_b), F.Braces());
			Stmt("bool operator`when`(Foo a, Foo b) { }", stmt);
			Expr("#fn(bool, operator`when`, #([] Foo a, [] Foo b), { })", stmt);
		}

		[Test]
		public void EcsVarAnywhere()
		{
			// An unassigned variable is allowed when an expression has attributes, such as 'out'
			Stmt("Foo(out List<Foo> x);", F.Call(Foo, F.Attr(_(S.Out), F.Var(F.Of(_("List"), Foo), x))));
			Stmt("Foo([x] List<Foo> x);", F.Call(Foo, F.Attr(x, F.Var(F.Of(_("List"), Foo), x))));
			// Allow "var" even in places where unassigned variable declarations 
			// are normally prohibited. This is allowed just for extra 
			// flexibility in case a macro needs it.
			//   Note that other types like "Foo x" are still not allowed.
			// Rationale: "List<T> x" is not allowed in arbitrary locations 
			// because it is parsed as "(List < T) > x" for backward compatibility 
			// with C#. We don't allow "Foo x" so that (1) non-generic types 
			// aren't "more priveleged" - usable in more places than generic
			// types - and (2) so that macro authors are more likely to notice 
			// the parsing problem. In case a macro author _really needs_ to 
			// allow an unassigned variable declaration in a place that doesn't
			// normally allow it, they can support "var x" and "var Foo x" (and 
			// manually remove the keyword attribute #var from the latter 
			// declaration).
			Stmt("Foo(var x);", F.Call(Foo, F.Var(F.Missing, x)), Mode.ParserTest);
			Stmt("Foo([] var x);", F.Call(Foo, F.Var(F.Missing, x)));
		}

		[Test]
		public void EcsPropertyDefinitionExpr()
		{
			Stmt("this(int Foo { get; }) { }", F.Call(S.Constructor, F.Missing, F.@this, F.List(
				F.Property(F.Int32, Foo, F.Missing, BracesOnOneLine(get))), F.Braces()));
			Stmt("Foo(x, int Foo { get; } = 0);", F.Call(Foo, x,
				F.Property(F.Int32, Foo, F.Missing, BracesOnOneLine(get), zero)));
		}

		[Test]
		public void EcsIsOperatorWithTuple()
		{
			// Enhanced C# patterns are being disabled to support C# 9 patterns (#129)
			//Expr("x is Foo(a, b)", F.Call(S.Is, x, Foo, F.List(a, b)));
			Expr("(x is Foo)(a, b)", F.Call(F.InParens(F.Call(S.Is, x, Foo)), a, b));
			//Expr("x is Foo(a, b) in c", F.Call(S.In, F.Call(S.Is, x, Foo, F.List(a, b)), c));
			//Expr("x is Foo<T>(a, b)", F.Call(S.Is, x, F.Of(Foo, T), F.List(a, b)));
			//Expr("x is $(x + 1)(a, b)", F.Call(S.Is, x, F.Call(S.Substitute, F.Call(S.Add, x, one)), F.List(a, b)));
			//Expr("x is Foo $a(b, c)", F.Call(S.Is, x, F.Var(Foo, F.Call(S.Substitute, a)), F.List(b, c)));

			// This doesn't parse because `?` is assumed not to be part of the 
			// type if it is followed by `(`, and that's OK, since disambiguating 
			// is hard and has virtually no benefit.
			Expr("x is Foo?(a, b) in c", F.Call(S.QuestionMark, F.Call(S.Is, x, Foo), F.Call(S.In, F.Tuple(a, b), c), F.Missing),
				Mode.ParserTest | Mode.ExpectAndDropParserError);
		}

		[Test]
		public void BlockCallsWithTokenLiteral()
		{
			Token[] xToken = new[] { new Token((int)TokenType.Id, 0, 0, 0, x.Name) };
			var xTreeNode = F.Literal(new TokenTree(F.File, xToken));
			// Not currently supported by printer, so test parser only
			Stmt("get @{ x };", F.Call(S.get, xTreeNode), Mode.ParserTest);
			Stmt("Foo(x) @{ x };", F.Call(Foo, x, xTreeNode), Mode.ParserTest);
			Stmt("Foo = get @{ x } + 1;", F.Call(S.Assign, Foo, F.Call(S.Add, F.Call(S.get, xTreeNode), one)), Mode.ParserTest);
		}

		[Test]
		public void EcsEasterEgg()
		{
			Expr("Foo!x.a", F.Dot(F.Of(Foo, x), a), Mode.ParserTest);
			Expr("Foo!(a+b,c).x", F.Dot(F.Of(Foo, F.Call(S.Add, a, b), c), x), Mode.ParserTest);
		}

		[Test]
		public void NamedArgOutsideMethodCall()
		{
			// Note: statements like `return a: b` and `throw a: b` don't parse because  
			// they can also be used in expressions like `c ? return x : throw y;` where 
			// the colon needs to be parsed as part of the `?` operator.
			Expr("Foo: x", F.Call(S.NamedArg, Foo, x));
			Expr("(Foo: x, a: a)", F.Tuple(F.Call(S.NamedArg, Foo, x), F.Call(S.NamedArg, a, a)));
			Stmt("using Foo: -x;", F.Call(S.Import, F.Call(S.NamedArg, Foo, F.Call(S._Negate, x))));
			Stmt("while (Foo: 1) { }", F.Call(S.While, F.Call(S.NamedArg, Foo, one), F.Braces()));
		}

		[Test]
		public void EcsMiscTests()
		{
			Stmt("using static Foo.x;", Attr(F.Id(S.Static), F.Call(S.Import, F.Dot(Foo, x))));
			Stmt("static using Foo.x;", Attr(F.Id(S.Static), F.Call(S.Import, F.Dot(Foo, x))), Mode.ParserTest);

			Stmt("[Foo] default:", Attr(Foo, F.Call(S.Label, F.Id(S.Default))));
			Stmt("[Foo] case x:", Attr(Foo, F.Call(S.Case, x)));
			Stmt("partial case x:", Attr(partialWA, F.Call(S.Case, x)));
			var braces_Foo_x = BracesOnOneLine(F.Var(Foo, Operator(F.Call(S.Substitute, x))));
			Stmt("if ({ Foo $x; })\n  Foo();", F.Call(S.If, braces_Foo_x, OnNewLine(F.Call(Foo)))); ;
		}

		[Test]
		public void ThrowReturnBreakContinueExpr()
		{
			// Note: something like "continue ? x : y" is NOT recognized as an 
			// expression because throw/return/break/continue are considered
			// statements when they appear at the beginning of a statement
			// (so that they can accept contextual keywords.)
			Stmt("#continue() ? x++ : return;", F.Call(S.QuestionMark, F.Call(S.Continue), F.Call(S.PostInc, x), F.Call(S.Return)));
			Stmt("x = #return(x) = 1;", F.Call(S.Assign, x, F.Call(S.Assign, F.Call(S.Return, x), one)));
			Stmt("x > 0 || throw new Exception();", F.Call(S.Or, F.Call(S.GT, x, zero), F.Call(S.Throw, F.Call(S.New, F.Call("Exception")))));
			Stmt("Foo(throw x ?? Foo);", F.Call(Foo, F.Call(S.Throw, F.Call(S.NullCoalesce, x, Foo))));
			Stmt("x < 0 ? return a ?? b : x;", F.Call(S.QuestionMark, F.Call(S.LT, x, zero), F.Call(S.Return, F.Call(S.NullCoalesce, a, b)), x));
			Stmt("x < 0 ? break : continue;", F.Call(S.QuestionMark, F.Call(S.LT, x, zero), F.Call(S.Break), F.Call(S.Continue)));
			Stmt("c ? break x : continue x;", F.Call(S.QuestionMark, c, F.Call(S.Break, x), F.Call(S.Continue, x)));
		}

		[Test]
		public void SwitchExpr()
		{
			var tree =
				F.Call(S.Assign, Foo, F.Call(S.Add,
					F.Call(S.SwitchStmt, x, F.Braces(
						F.Call(S.Case, a), one,
						F.Call(S.Label, F.Id(S.Default)), zero)),
					one));
			Stmt("Foo = switch (x) {\ncase a:\n  1;\ndefault:\n  0;\n} + 1;", tree);
		}

		[Test]
		public void CaseStmts()
		{
			Stmt("case Foo x:", F.Call(S.Case, F.Var(Foo, x)));
			Stmt("case Foo $x:", F.Call(S.Case, F.Var(Foo, Operator(F.Call(S.Substitute, x)))));
			Stmt("case { Foo $x; }:", F.Call(S.Case, BracesOnOneLine(F.Var(Foo, Operator(F.Call(S.Substitute, x))))));
		}

		[Test]
		public void GotoExpr()
		{
			var tree = F.Call(S.Assign, x, F.Call(F.Dot(
				F.InParens(F.Call(S.NullCoalesce, x, F.Call(S.Goto, F.Id("fail")))), Foo)));
			Stmt("x = (x ?? goto fail).Foo();", tree);
			tree = F.Call(S.QuestionMark, F.Call(S.GT, Foo, zero), F.Call(S.GotoCase, a), F.Call(S.GotoCase, b));
			Stmt("Foo > 0 ? goto case a : goto case b;", tree);
		}

		[Test]
		public void Ridiculous()
		{
			Stmt("crazy throw return goto case break continue;", F.Attr(WordAttr("crazy"),
				F.Call(S.Throw, F.Call(S.Return, F.Call(S.GotoCase, F.Call(S.Break, F.Call(S.Continue)))))));
			Stmt("typeof(List<>) != typeof(2+2);",
				F.Call(S.NotEq,
					F.Call(S.Typeof, F.Of(_("List"), F.Missing)),
					F.Call(S.Typeof, F.Call(_("#error"), F.Literal("Type expected")))),
					Mode.ParserTest | Mode.ExpectAndDropParserError);
		}
	}
}
