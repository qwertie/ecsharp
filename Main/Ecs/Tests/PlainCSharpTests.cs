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
		public void CsSimpleAtoms()
		{
			Expr("Foo",      Foo);
			Expr("1024",     F.Literal(1024));
			Expr("0.5",      F.Literal(0.5));
			Expr("'$'",      F.Literal('$'));
			Expr(@"""hi""",  F.Literal("hi"));
			Expr("null",     F.Literal(null));
			Expr("true",     F.Literal(true));
		}

		[Test]
		public void CsLiterals()
		{
			// See also: EcsLexerTests
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
			Expr("-1",       F.Literal(-1), Mode.PrinterTest);
			Expr("0xff",     F.Literal(0xFF).SetBaseStyle(NodeStyle.HexLiteral));
			Expr("null",     F.Literal(null));
			Expr("false",    F.Literal(false));
			Expr("true",     F.Literal(true));
			Expr("'$'",      F.Literal('$'));
			Expr(@"'\0'",    F.Literal('\0'));
			Expr(@"""hi""",  F.Literal("hi"));
			Expr(@"@""hi""", F.Literal("hi").SetBaseStyle(NodeStyle.VerbatimStringLiteral));
			Expr("@\"\n\"",  F.Literal("\n").SetBaseStyle(NodeStyle.VerbatimStringLiteral));
			Expr("123456789123456789uL", F.Literal(123456789123456789uL));
			Expr("0xffffffffffffffffuL", F.Literal(0xFFFFFFFFFFFFFFFFuL).SetBaseStyle(NodeStyle.HexLiteral));
			Expr("1.234568E+08f",F.Literal(1.234568E+08f));
			Expr("12345678.9", F.Literal(12345678.9));
			Expr("1.23456789012346E+17d",F.Literal(1.23456789012346E+17d));
		}

		[Test]
		public void CsInfixOperators()
		{
			Expr("a",                a);
			Expr("a.b.c",            F.Dot(a, b, c));
			Expr("a + b",            F.Call(S.Add, a, b));
			Expr("a + b + c",        F.Call(S.Add, F.Call(S.Add, a, b), c));
			Expr("a * b / c % 2",    F.Call(S.Mod, F.Call(S.Div, F.Call(S.Mul, a, b), c), two));
			Expr("a >= b && a <= c", F.Call(S.And, F.Call(S.GE, a, b), F.Call(S.LE, a, c)));
			Expr("a > b || a < c",   F.Call(S.Or, F.Call(S.GT, a, b), F.Call(S.LT, a, c)));
			Expr("a == b ^^ a != c", F.Call(S.Xor, F.Call(S.Eq, a, b), F.Call(S.Neq, a, c)));
			Expr("a & b | c ^ 1",    F.Call(S.OrBits, F.Call(S.AndBits, a, b), F.Call(S.XorBits, c, one)));
			Expr("a = b ?? c",       F.Call(S.Assign, a, F.Call(S.NullCoalesce, b, c)));
			Expr("a >>= b <<= c",    F.Call(S.ShrAssign, a, F.Call(S.ShlAssign, b, c)));
			Expr("a.b - a::b",       F.Call(S.Sub, F.Call(S.Dot, a, b), F.Call(S.ColonColon, a, b)));
			Expr("a << 1 | b >> 1",     F.Call(S.OrBits, F.Call(S.Shl, a, one), F.Call(S.Shr, b, one)));
		}

		[Test]
		public void CsPrefixOperators()
		{
			Expr("-a",     F.Call(S._Negate, a));
			Expr("+a",     F.Call(S._UnaryPlus, a));
			Expr("~a",     F.Call(S.NotBits, a));
			Expr("!a",     F.Call(S.Not, a));
			Expr("++a",    F.Call(S.PreInc, a));
			Expr("--a",    F.Call(S.PreDec, a));
			Expr("*a",     F.Call(S._Dereference, a));
			Expr("&a",     F.Call(S._AddressOf, a));
			Expr("**a",    F.Call(S._Dereference, F.Call(S._Dereference, a)));
		}

		[Test]
		public void CsMiscOperators()
		{
			Expr("a++ + a--",           F.Call(S.Add, F.Call(S.PostInc, a), F.Call(S.PostDec, a)));
			Expr("a ? b : c",           F.Call(S.QuestionMark, a, b, c));
			Expr("a is Foo ? a as Foo : b", F.Call(S.QuestionMark, F.Call(S.Is, a, Foo), F.Call(S.As, a, Foo), b));
		}

		[Test]
		public void CsSimpleExpressions()
		{
			Expr("a + b",        F.Call(S.Add, a, b));
			Expr("a + b + c",    F.Call(S.Add, F.Call(S.Add, a, b), c));
			Expr("+a",           F.Call(S.Add, a));
			Expr("a >> b",       F.Call(S.Shr, a, b));
			Expr("a = b + c",    F.Assign(a, F.Call(S.Add, b, c)));
			Expr("a(b)(c)",      F.Call(F.Call(a, b), c));
			Expr("a++--",        F.Call(S.PostDec, F.Call(S.PostInc, a)));
			Expr("x => x + 1",   F.Call(S.Lambda, x, F.Call(S.Add, x, one)));
		}

		[Test]
		public void CsSimpleCallsAndTypeParams()
		{
			Expr("a", a);
			Expr("(a)",      F.InParens(a));
			Expr("((a))",    F.InParens(F.InParens(a)));
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
		public void CsSimpleVarDecls()
		{
			Stmt("Foo a;",   F.Vars(Foo, a));
			Stmt("Foo.x a;", F.Vars(F.Dot(Foo, x), a));
			Stmt("int a;",   F.Vars(F.Int32, a));
			Stmt("int[] a;", F.Vars(F.Of(S.Array, S.Int32), a));
			Stmt("Foo[] a;", F.Vars(F.Of(S.Array, Foo), a));
			Stmt("Foo a, b = c;", F.Vars(Foo, a, F.Call(S.Assign, b, c)));
			// The parser doesn't worry about treating this as a keyword
			Stmt("dynamic a;", F.Vars(_("dynamic"), a));
			// Note: plain C# doesn't see a difference here, but EC# does
			Stmt("var a;",   F.Vars(_(S.Missing), a));
			Stmt("@var a;",  F.Vars(_("var"), a));
		}

		[Test]
		public void CsAnonymousLambdaFunctions()
		{
			Stmt("delegate(T a) {\n  return a;\n};",
				F.Call(S.Lambda, F.List(F.Var(T, a)), F.Braces(F.Call(S.Return, a))).SetBaseStyle(NodeStyle.OldStyle));
			Expr("a => a + 1", F.Call(S.Lambda, a, F.Call(S.Add, a, one)));
			Expr("1 + a => 2 + b => c", F.Call(S.Add, one, F.Call(S.Lambda, a, F.Call(S.Add, two, F.Call(S.Lambda, b, c)))));
			Expr("(Foo a) => a", F.Call(S.Lambda, F.InParens(F.Vars(Foo, a)), a));
			Expr("(Foo a, Foo b) => a(b)", F.Call(S.Lambda, F.Tuple(F.Var(Foo, a), F.Var(Foo, b)), F.Call(a, b)));
			Expr("(Foo a, Foo b) => a ?? b", F.Call(S.Lambda, F.Tuple(F.Var(Foo, a), F.Var(Foo, b)), F.Call(S.NullCoalesce, a, b)));
			Expr("(Foo a, Foo b) => {\n  a(b);\n}", F.Call(S.Lambda, F.Tuple(F.Var(Foo, a), F.Var(Foo, b)), F.Braces(F.Call(a, b))));
		}

		[Test]
		public void CsSpecialOperators()
		{
			Expr("x++",              F.Call(S.PostInc, x));
			Expr("x--",              F.Call(S.PostDec, x));
			Expr("c ? Foo(x) : a + b", F.Call(S.QuestionMark, c, F.Call(Foo, x), F.Call(S.Add, a, b)));
			Expr("Foo[x]",           F.Call(S.IndexBracks, Foo, x));
			Expr("Foo[a, b]",        F.Call(S.IndexBracks, Foo, a, b));
			Expr("Foo[a - 1]",       F.Call(S.IndexBracks, Foo, F.Call(S.Sub, a, one)));
			Expr("Foo[]",            F.Call(S.IndexBracks, Foo)); // "Foo[]" means #of(#`[]`, Foo) only in a type context
			Expr("(Foo) x",          F.Call(S.Cast, x, Foo));
			Expr("x as Foo",         F.Call(S.As, x, Foo));
			Expr("x is Foo",         F.Call(S.Is, x, Foo));
		}

		[Test]
		public void CsCallStyleOperators()
		{
			Expr("checked(a + b)",        F.Call(S.Checked, F.Call(S.Add, a, b)));
			Expr("unchecked(a << b)",     F.Call(S.Unchecked, F.Call(S.Shl, a, b)));
			Expr("checked(Foo[1])",        F.Call(S.Checked,   F.Call(S.IndexBracks, Foo, one)));
			Expr("unchecked(Foo[1])",      F.Call(S.Unchecked, F.Call(S.IndexBracks, Foo, one)));

			Expr("default(Foo)",          F.Call(S.Default, Foo));
			Expr("default(int)",          F.Call(S.Default, F.Int32));
			Expr("typeof(Foo)",           F.Call(S.Typeof, Foo));
			Expr("typeof(int)",           F.Call(S.Typeof, F.Int32));
			Expr("typeof(Foo<int>)",      F.Call(S.Typeof, F.Call(S.Of, Foo, F.Int32)));
			Expr("sizeof(Foo<int>)",      F.Call(S.Sizeof, F.Call(S.Of, Foo, F.Int32)));

			Expr("default(int[])",        F.Call(S.Default,   F.Call(S.Of, _(S.Array), F.Int32)));
			Expr("typeof(int[])",         F.Call(S.Typeof,    F.Call(S.Of, _(S.Array), F.Int32)));
			Expr("sizeof(int[])",         F.Call(S.Sizeof,    F.Call(S.Of, _(S.Array), F.Int32)));
			Expr("sizeof(Foo[])",         F.Call(S.Sizeof,    F.Call(S.Of, _(S.Array), Foo)));
		}

		[Test]
		public void CsOperatorNew()
		{
			Expr("new Foo(x)",            F.Call(S.New, F.Call(Foo, x)));
			Expr("new Foo(x) { a }",      F.Call(S.New, F.Call(Foo, x), a));
			Expr("new Foo()",             F.Call(S.New, F.Call(Foo)));
			Expr("new Foo { a }",         F.Call(S.New, F.Call(Foo), a));      // new Foo() { a } would also be ok
			Expr("new int[] { a, b }",    F.Call(S.New, F.Call(F.Of(S.Array, S.Int32)), a, b));
			Expr("new[] { a, b }",        F.Call(S.New, F.Call(S.Array), a, b));
			Expr("new[] { }",             F.Call(S.New, F.Call(S.Array)));
			Expr("new int[][,] { a }",    F.Call(S.New, F.Call(F.Of(_(S.Array), F.Of(S.TwoDimensionalArray, S.Int32))), a));
			// This expression is illegal since it requires an initializer list, but it's parsable so should print ok
			Expr("new int[][,][,,]",      F.Call(S.New, F.Call(F.Of(_(S.Array), F.Of(_(S.TwoDimensionalArray), F.Of(S.GetArrayKeyword(3), S.Int32))))), Mode.Both | Mode.ExpectAndDropParserError);
			Expr("new int[10][,] { a }",  F.Call(S.New, F.Call(F.Of(_(S.Array), F.Of(S.TwoDimensionalArray, S.Int32)), F.Literal(10)), a));
			Expr("new int[x, x][]",       F.Call(S.New, F.Call(F.Of(_(S.TwoDimensionalArray), F.Of(S.Array, S.Int32)), x, x)));
			Expr("new int[,]",            F.Call(S.New, F.Call(F.Of(S.TwoDimensionalArray, S.Int32))), Mode.Both | Mode.ExpectAndDropParserError);
			Option(Mode.PrintBothParseFirst, "#new(@`#[,]`!([Foo] int)());", "new int[,];",
				F.Call(S.New, F.Call(F.Of(_(S.TwoDimensionalArray), Attr(Foo, F.Int32)))), p => p.DropNonDeclarationAttributes = true);
			Expr("new { a = 1, b = 2 }",  F.Call(S.New, F.Missing, F.Call(S.Assign, a, one), F.Call(S.Assign, b, two)));
		}

		[Test]
		public void CsDataTypes()
		{
			Stmt("double x;",            F.Vars(F.Double, x));
			Stmt("int[] x;",             F.Vars(F.Of(S.Array, S.Int32), x));
			Stmt("long* x;",             F.Vars(F.Of(S._Pointer, S.Int64), x));
			Stmt("string[][,] x;",       F.Vars(F.Of(_(S.Array), F.Of(S.TwoDimensionalArray, S.String)), x));
			Stmt("typeof(float*);",      F.Call(S.Typeof, F.Of(S._Pointer, S.Single)));
			Stmt("decimal[,,,] x;",      F.Vars(F.Of(S.GetArrayKeyword(4), S.Decimal), x));
			Stmt("double? x;",           F.Vars(F.Of(S.QuestionMark, S.Double), x));
			Stmt("Foo<a.b.c>? x;",       F.Vars(F.Of(_(S.QuestionMark), F.Of(Foo, F.Dot(a, b, c))), x));
			Stmt("Foo<a?,b.c[,]>[] x;",  F.Vars(F.Of(_(S.Array), F.Of(Foo, F.Of(_(S.QuestionMark), a), F.Of(_(S.TwoDimensionalArray), F.Dot(b, c)))), x));
		}

		[Test]
		public void BlocksOfStmts()
		{
			Stmt("{\n  a();\n  b = c;\n}",        F.Braces(F.Call(a), F.Assign(b, c)));
			// TODO
			//Stmt("#@{\n  Foo(x);\n  b **= 2\n};", F.List(F.Call(Foo, x), F.Result(F.Call(S.ExpSet, b, two))), Mode.ParseOnly);
		}

		[Test]
		public void SimpleExecutableStmts()
		{
			// S.Break, S.Continue, S.Goto, S.GotoCase, S.Return, S.Throw
			Stmt("break;",             F.Call(S.Break));
			Stmt("break outer;",       F.Call(S.Break, _("outer")));
			Stmt("continue;",          F.Call(S.Continue));
			Stmt("continue outer;",    F.Call(S.Continue, _("outer")));
			Stmt("goto end;",          F.Call(S.Goto, _("end")));
			Stmt("goto case 1;",       F.Call(S.GotoCase, one));
			Stmt("goto case default;", F.Call(S.GotoCase, _(S.Default)));
			Stmt("return;",            F.Call(S.Return));
			Stmt("return 1;",          F.Call(S.Return, one));
			Stmt("throw;",             F.Call(S.Throw));
			Stmt("throw new Foo();",   F.Call(S.Throw, F.Call(S.New, F.Call(Foo))));
		}

		[Test]
		public void CsUsingStmts()
		{
			Stmt("using Foo.x;",       F.Call(S.Import, F.Dot(Foo, x)));
			Stmt("using Foo = x;",     Attr(F.Id(S.FilePrivate), F.Call(S.Alias, F.Call(S.Assign, Foo, x), F.List())));
			Stmt("using (var x = Foo)\n  x.a();", F.Call(S.UsingStmt, F.Var(F.Missing, x.Name, Foo), F.Call(F.Dot(x, a))));
		}

		[Test]
		public void EcsUsingStmts()
		{
			Stmt("using Loyc(@``, .Syntax);", F.Call(S.Import, F.Call(_("Loyc"), F.Missing, F.Dot(_("Syntax")))));
			Stmt("using Loyc(, .Syntax);",    F.Call(S.Import, F.Call(_("Loyc"), F.Missing, F.Dot(_("Syntax")))), Mode.ParserTest);
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
			                                                F.Call(S.Mul, F.Literal(0xBAAD).SetBaseStyle(NodeStyle.HexLiteral), 
			                                                              F.Literal(0xF00D).SetBaseStyle(NodeStyle.HexLiteral))))));

			Stmt("do\n  a();\nwhile (c);",              F.Call(S.DoWhile, F.Call(a), c));
			Stmt("do {\n  a();\n} while (c);",          F.Call(S.DoWhile, F.Braces(F.Call(a)), c));
			Stmt("do {\n  a\n} while (c);",             F.Call(S.DoWhile, F.Braces(F.Result(a)), c));
			//TODO
			//Stmt("do #@{\n  a();\n}; while (c);",       F.Call(S.DoWhile, F.List(F.Call(a)), c), Mode.ParseOnly);

			var amp_b_c = F.Call(S._AddressOf, F.Call(S.PtrArrow, b, c));
			var int_a_amp_b_c = F.Var(F.Of(_(S._Pointer), F.Int32), a.Name, amp_b_c);
			Stmt("fixed (int* a = &b->c)\n  Foo(a);",    F.Call(S.Fixed, int_a_amp_b_c, F.Call(Foo, a)));
			Stmt("fixed (int* a = &b->c) {\n  Foo(a);\n}", F.Call(S.Fixed, int_a_amp_b_c, F.Braces(F.Call(Foo, a))));
			var stmt = F.Call(S.Fixed, F.Vars(F.Of(_(S._Pointer), F.Int32), F.Assign(x, F.Call(S._AddressOf, Foo))), F.Call(a, x));
			Stmt("fixed (int* x = &Foo)\n  a(x);", stmt);

			stmt = F.Call(S.While, F.Call(S.GT, x, one), F.Call(S.PostDec, x));
			Stmt("while (x > 1)\n  x--;", stmt);
			stmt = F.Call(S.UsingStmt, F.Var(F.Missing, x.Name, F.Call(S.New, F.Call(Foo))), F.Call(F.Dot(x, a)));
			Stmt("using (var x = new Foo())\n  x.a();", stmt);
			stmt = F.Call(S.Lock, Foo, F.Braces(F.Call(F.Dot(Foo, Foo))));
			Stmt("lock (Foo) {\n  Foo.Foo();\n}", stmt);

			stmt = F.Call(S.Try, F.Call(Foo), F.Call(S.Catch, F.Missing, F.Missing, F.Braces()));
			Stmt("try\n  Foo();\ncatch {\n}", stmt);
			stmt = F.Call(S.Try, F.Call(Foo), F.Call(S.Catch, F.Vars(_("Exception"), x), F.Missing, F.Braces(F.Call(S.Throw))), F.Call(S.Finally, F.Call(_("hi_mom"))));
			Stmt("try\n  Foo();\n"+
				 "catch (Exception x) {\n  throw;\n"+
				 "} finally\n  hi_mom();", stmt);

			stmt = F.Call(S.ForEach, F.Vars(F.Missing, x), Foo, F.Call(a, x));
			Stmt("foreach (var x in Foo)\n  a(x);", stmt);
			stmt = F.Call(S.ForEach, F.Missing, F.Call(S.In, x, Foo), F.Call(a, x));
			Stmt("foreach (x in Foo)\n  a(x);", stmt, Mode.ParserTest);
			// TODO reconsider
			//stmt = F.Call(S.ForEach, F.Call(S.Add, a, b), c, F.Braces());
			//Stmt("foreach (a + b in c) {\n}", stmt);
			//stmt = F.Call(S.ForEach, F.Set(a, x), F.Set(b, x), F.List());
			//Stmt("foreach (a `=` x in b = x) #{\n}", stmt);
		}

		[Test]
		public void ForLoops()
		{
			var forArgs = new LNode[] {
				F.List(), F.Missing, F.List(), F.Braces()
			};
			Stmt("for (;;) {\n}",   F.Call(S.For, forArgs));
			forArgs = new LNode[] {
				F.List(F.Var(F.Int32, x.Name, F.Literal(0))),
				F.Call(S.LT, x, F.Literal(10)),
				F.List(F.Call(S.PostInc, x)),
				F.Braces()
			};
			Stmt("for (int x = 0; x < 10; x++) {\n}",   F.Call(S.For, forArgs));
			forArgs = new LNode[] {
				F.List(F.Assign(a, zero), F.Assign(b, one)),
				F.Call(S.LT, a, F.Literal(10)),
				F.List(F.Call(S.PostInc, a), F.Call(S.PreInc, b)),
				F.Braces()
			};
			Stmt("for (a = 0, b = 1; a < 10; a++, ++b) {\n}",   F.Call(S.For, forArgs));
			// TODO: The tree isn't quite right on this one. It still works 
			// because the parser and printer treat it the same way. It seems 
			// like we should fix this eventually, but when we do, we also have
			// to decide how to print #(int x = 5, y = "Y") so that the second 
			// part is not mistaken for part of the variable decl, see? But how?
			forArgs = new LNode[] {
				F.List(F.Var(F.Int32, a, zero), F.Assign(b, one), c),
				F.Call(S.LT, a, F.Literal(10)),
				F.List(F.Call(S.PostInc, a)),
				F.Braces()
			};
			Stmt("for (int a = 0, b = 1, c; a < 10; a++) {\n}",   F.Call(S.For, forArgs));
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
			stmt = F.Braces(
				F.Call(S.Case, Foo),
				F.Call(Foo));
			Stmt("{\ncase Foo:\n  Foo();\n}", stmt);
		}

		[Test]
		public void ArrayInitializers()
		{
			Stmt("int[,] Foo = new[,] { { 0 }, { 1, 2 } };", F.Call(S.Var, F.Of(S.TwoDimensionalArray, S.Int32),
				F.Call(S.Assign, Foo, F.Call(S.New, F.Call(S.TwoDimensionalArray),
					AsStyle(NodeStyle.Expression, F.Braces(zero)),
					AsStyle(NodeStyle.Expression, F.Braces(one, two))))));
			Stmt("int[] Foo = { 0, 1, 2\n};", F.Call(S.Var, F.Of(S.Array, S.Int32),
				F.Call(S.Assign, Foo, F.Call(S.ArrayInit, zero, one, two))));
			// TODO: The printer's newline choices are odd. See if we can improve them.
			Stmt("int[,] Foo = { { 0\n}, { 1, 2\n}\n};", F.Call(S.Var, F.Of(S.TwoDimensionalArray, S.Int32),
				F.Call(S.Assign, Foo, F.Call(S.ArrayInit,
					AsStyle(NodeStyle.Expression, F.Braces(zero)),
					AsStyle(NodeStyle.Expression, F.Braces(one, two))))));
		}

		[Test]
		public void CsParsingChallenges() // See also: ParserOnlyTests()
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
		public void SpaceStmts()
		{
			// Spaces: S.Struct, S.Class, S.Trait, S.Enum, S.Alias, S.Interface, S.Namespace
			var public_x = Attr(@public, F.Vars(F.Int32, x));
			Stmt("struct Foo;",        F.Call(S.Struct, Foo, F.List()));
			Stmt("struct Foo : IFoo;", F.Call(S.Struct, Foo, F.List(IFoo)));
			Stmt("struct Foo\n{\n}",   F.Call(S.Struct, Foo, F.List(), F.Braces()));
			Stmt("struct Foo\n{\n" +
				"  public int x;\n}",  F.Call(S.Struct, Foo, F.List(), F.Braces(public_x)));
			Stmt("class Foo : IFoo\n{\n}", F.Call(S.Class, Foo, F.List(IFoo), F.Braces()));
			var a_where = Attr(F.Call(S.Where, @class), a);
			var b_where = Attr(F.Call(S.Where, a), b);
			var c_where = Attr(F.Call(S.Where, F.Call(S.New)), c);
			var stmt = F.Call(S.Class, F.Of(Foo, a_where, b_where), F.List(IFoo), F.Braces());
			Stmt("class Foo<a,b> : IFoo where a: class where b: a\n{\n}", stmt);
			stmt = F.Call(S.Class, F.Of(Foo, Attr(_(S.Out), a_where)), F.List(), F.Braces());
			Stmt("class Foo<out a> where a: class\n{\n}", stmt);
			stmt = F.Call(S.Class, F.Of(Foo, Attr(_(S.Out), c_where)), F.List(IFoo), F.Braces());
			Stmt("class Foo<out c> : IFoo where c: new()\n{\n}", stmt);
			stmt = F.Call(S.Class, F.Of(Foo, Attr(_(S.In), T)), F.List(), F.Braces());
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
			stmt = F.Call(S.Enum, F.Call(S.Substitute, F.Dot(Foo, x)), F.List(), F.Braces(F.Assign(a, one)));
			Stmt("enum $(Foo.x)\n{\n  a = 1\n}", stmt);
			LNode anyList = F.Call(S.Substitute, F.Call(S.DotDotDot, _("_")));
			Stmt("enum $x : $(..._)\n{\n  $(..._)\n}", F.Call(S.Enum, F.Call(S.Substitute, x), F.List(anyList), F.Braces(anyList)));

			stmt = F.Call(S.Interface, F.Of(Foo, Attr(@out, T)), F.List(F.Of(_("IEnumerable"), T)), F.Braces(public_x));
			Stmt("interface Foo<out T> : IEnumerable<T>\n{\n  public int x;\n}", stmt);
			Expr("#interface(Foo!(out T), #(IEnumerable<T>), {\n  public int x;\n})", stmt);

			stmt = F.Call(S.Namespace, F.Of(Foo, T), F.List(), F.Braces(public_x));
			Stmt("namespace Foo<T>\n{\n  public int x;\n}", stmt);
			Expr("#namespace(Foo<T>, #(), {\n  public int x;\n})", stmt);

			stmt = F.Call(S.Class, F.Assign(F.Of(_("L"), T), F.Of(_("List"), T)), F.List());
			Stmt("#class(L<T> = List<T>, #());", stmt);
		}

		[Test]
		public void MethodDefinitionStmts()
		{
			// #fn and #delegate
			LNode int_x = F.Vars(F.Int32, x), list_int_x = F.List(int_x), x_mul_x = F.Call(S.Mul, x, x);
			LNode stmt;
			stmt = F.Call(S.Delegate, F.Void, F.Of(Foo, T), F.List(F.Vars(T, a), F.Vars(T, b)));
			Stmt("delegate void Foo<T>(T a, T b);", stmt);
			Expr("#delegate(void, Foo<T>, #([] T a, [] T b))", stmt);
			stmt = F.Call(S.Delegate, F.Void, F.Of(Foo, Attr(F.Call(S.Where, _(S.Class), x), T)), F.List(F.Vars(T, x)));
			Stmt("delegate void Foo<T>(T x) where T: class, x;", stmt);
			Expr("#delegate(void, Foo!([#where(#class, x)] T), #([] T x))", stmt);
			stmt = Attr(@public, @new, partialWA, F.Fn(F.String, Foo, list_int_x));
			Stmt("public new partial string Foo(int x);", stmt);
			// The printer does not print trivia attributes, but the parsing test will fail if the trivia is missing
			Expr("[#public, #new, "         +            "#partial] #fn(string, Foo, #([] int x))", stmt, Mode.PrinterTest);
			Expr("[#public, #new, [#trivia_wordAttribute] #partial] #fn(string, Foo, #([] int x))", stmt, Mode.ParserTest);
			stmt = F.Fn(F.Int32, Foo, list_int_x, F.Braces(F.Result(x_mul_x)));
			Stmt("int Foo(int x)\n{\n  x * x\n}", stmt);
			Expr("#fn(int, Foo, #([] int x), {\n  x * x\n})", stmt);
			stmt = F.Fn(F.Int32, Foo, list_int_x, F.Braces(F.Call(S.Return, x_mul_x)));
			Stmt("int Foo(int x)\n{\n  return x * x;\n}", stmt);
			Expr("#fn(int, Foo, #([] int x), {\n  return x * x;\n})", stmt);
			stmt = F.Fn(F.Decimal, Foo, list_int_x, F.Call(S.Forward, F.Dot(a, b)));
			Stmt("decimal Foo(int x) ==> a.b;", stmt);
			Expr("#fn(decimal, Foo, #([] int x), ==> a.b)", stmt);
			stmt = F.Fn(_("IEnumerator"), F.Dot(_("IEnumerable"), _("GetEnumerator")), F.List(), F.Braces());
			Stmt("IEnumerator IEnumerable.GetEnumerator()\n{\n}", stmt);
			Expr("#fn(IEnumerator, IEnumerable.GetEnumerator, #(), {\n})", stmt);
		}

		[Test]
		public void CsOperatorDefinitions()
		{
			LNode @operator = _(S.TriviaUseOperatorKeyword), cast = _(S.Cast), operator_cast = Attr(@operator, cast);
			LNode Foo_a = F.Vars(Foo, a), Foo_b = F.Vars(Foo, b);
			LNode stmt = Attr(@static, F.Fn(F.Bool, Attr(@operator, _(S.Eq)), F.List(F.Vars(T, a), F.Vars(T, b)), F.Braces()));
			Stmt("static bool operator==(T a, T b)\n{\n}", stmt);
			Expr("static #fn(bool, operator==, #([] T a, [] T b), {\n})", stmt);
			stmt = Attr(@static, _(S.Implicit), F.Fn(T, operator_cast, F.List(Foo_a), F.Braces()));
			Stmt("static implicit operator T(Foo a)\n{\n}", stmt);
			Expr("static implicit #fn(T, operator`#cast`, #([] Foo a), {\n})", stmt);

			stmt = Attr(F.Call(Foo), @static,
			       F.Fn(Attr(Foo, F.Bool),
			             Attr(@operator, _(S.Neq)),
			             F.List(F.Vars(T, a), F.Vars(T, b)),
			             F.Braces(F.Result(F.Call(S.Neq, F.Dot(a, x), F.Dot(b, x))))));
			Stmt("[return: Foo]\n[Foo()]\nstatic bool operator!=(T a, T b)\n{\n  a.x != b.x\n}", stmt);
		}

		[Test]
		public void CsExpressionAsMethodBody()
		{
			Stmt("void Foo() => x;",     F.Fn(F.Void, Foo, F.List(), x));
			Stmt("void Foo() => x = 0;", F.Fn(F.Void, Foo, F.List(), F.Assign(x, zero)));
			Stmt("Foo.a Foo() => x = 0;", F.Fn(F.Dot(Foo, a), Foo, F.List(), F.Assign(x, zero)));
			Stmt("partial void Foo => x;",       Attr(partialWA, F.Property(F.Void, Foo, x)));
			Stmt("partial void Foo => x = 0;",   Attr(partialWA, F.Property(F.Void, Foo, F.Assign(x, zero))));
			Stmt("partial Foo.a Foo => x = 0;",  Attr(partialWA, F.Property(F.Dot(Foo, a), Foo, F.Assign(x, zero))));
		}

		[Test]
		public void PropertyStmts()
		{
			LNode stmt = F.Property(F.Int32, Foo, F.Braces(get, set));
			Stmt("int Foo { get; set; }", stmt);
			// was: #property(int, Foo, @``, {\n  get;\n  set;\n})
			// but now, property expressions are supported
			Expr("int Foo { get; set; }", stmt);
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
			Stmt("int Foo\n{\n  get(@`'==>`(a, b));\n}", stmt);
			stmt = F.Property(F.Int32, Foo, F.Braces(
								Attr(trivia_forwardedProperty, F.Call(get, F.Call(S.Forward, a, b)))));
			Stmt("int Foo\n{\n  get(@`'==>`(a, b));\n}", stmt, Mode.PrinterTest);
			Stmt("int Foo { protected get; private set; }",
				F.Property(F.Int32, Foo, F.Braces(
					F.Attr(F.Protected, get), F.Attr(F.Private, set))));

			stmt = F.Property(Foo, F.@this, F.List(F.Var(F.Int64, x)), F.Braces(get, set));
			Stmt("Foo this[long x] { get; set; }", stmt);
			stmt = Attr(F.Private, F.Property(F.Of(Foo, T), F.Of(F.@this, T), F.List(F.Var(T, x)), F.Braces(get, set)));
			Stmt("private Foo<T> this<T>[T x] { get; set; }", stmt);
		}

		[Test]
		public void EventStmts()
		{
			LNode EventHandler = _("EventHandler"), add = _("add"), remove = _("remove");
			var stmt = F.Call(S.Event, F.Of(EventHandler, T), _("Click"));
			Stmt("event EventHandler<T> Click;", stmt);
			Expr("#event(EventHandler<T>, Click)", stmt);
			stmt = F.Call(S.Event, EventHandler, F.List(a, b));
			Stmt("event EventHandler a, b;", stmt);
			Expr("#event(EventHandler, #(a, b))", stmt);
			stmt = F.Call(S.Event, EventHandler, a, F.Braces(
				AsStyle(NodeStyle.Special, F.Call(add, F.Braces())),
				AsStyle(NodeStyle.Special, F.Call(remove, F.Braces()))));
			Stmt("event EventHandler a\n{\n  add {\n  }\n  remove {\n  }\n}", stmt);
			Expr("#event(EventHandler, a, {\n  add {\n  }\n  remove {\n  }\n})", stmt);
		}

		[Test]
		public void AssemblyAttribute()
		{
			Stmt("[assembly: Foo]", F.Call(S.Assembly, Foo));
			Stmt("{\n  [assembly: CLSCompliant(false)]\n  Foo;\n}",
				F.Braces(F.Call(S.Assembly, F.Call(_("CLSCompliant"), F.@false)), Foo));
		}

		[Test]
		public void KeywordAttributes()
		{
			Stmt("public static void Main()\n{\n}", F.Fn(F.Void, _("Main"), F.List(), F.Braces()).PlusAttrs(@public, @static));
			Stmt("public static Foo Main()\n{\n}",  F.Fn(Foo,    _("Main"), F.List(), F.Braces()).PlusAttrs(@public, @static));
			Stmt("public static List<Foo> Main()\n{\n}",  F.Fn(F.Of(_("List"), Foo), _("Main"), F.List(), F.Braces()).PlusAttrs(@public, @static));
			Stmt("new void Main();",                F.Fn(F.Void, _("Main"), F.List()).PlusAttrs(@new));
			Stmt("new partial int Main()\n{\n}",    F.Fn(F.Int32, _("Main"), F.List(), F.Braces()).PlusAttrs(@new, partialWA));
			Stmt("partial new int Main()\n{\n}",    F.Fn(F.Int32, _("Main"), F.List(), F.Braces()).PlusAttrs(partialWA, @new));
			Stmt("new partial Foo Main()\n{\n}",    F.Fn(Foo, _("Main"), F.List(), F.Braces()).PlusAttrs(@new, partialWA));
			Stmt("partial new Foo Main()\n{\n}",    F.Fn(Foo, _("Main"), F.List(), F.Braces()).PlusAttrs(partialWA, @new));
			Stmt("public new int x;",               F.Vars(F.Int32, x).PlusAttrs(@public, @new));
			Stmt("new public int x;",               F.Vars(F.Int32, x).PlusAttrs(@new, @public));
			Stmt("public new Foo x;",               F.Vars(Foo, x).PlusAttrs(@public, @new));
			Stmt("new public Foo x;",               F.Vars(Foo, x).PlusAttrs(@new, @public));
			Stmt("protected override Foo Foo { get; }",F.Property(Foo, Foo, F.Braces(get)).PlusAttrs(F.Protected, _(S.Override)));
			Stmt("protected override sealed int Foo { get; }",F.Property(F.Int32, Foo, F.Braces(get)).PlusAttrs(F.Protected, _(S.Override), _(S.Sealed)));
			Stmt("new partial Foo Foo { get; }",    F.Property(Foo, Foo, F.Braces(get)).PlusAttrs(@new, partialWA));
			Stmt("partial new Foo Foo { get; }",    F.Property(Foo, Foo, F.Braces(get)).PlusAttrs(partialWA, @new));
			Stmt("new partial List<Foo> Foo { get; }",F.Property(F.Of(_("List"), Foo), Foo, F.Braces(get)).PlusAttrs(@new, partialWA));
			Stmt("partial new List<Foo> Foo { get; }",F.Property(F.Of(_("List"), Foo), Foo, F.Braces(get)).PlusAttrs(partialWA, @new));
			Stmt("new public List<Foo> Foo { get; }",F.Property(F.Of(_("List"), Foo), Foo, F.Braces(get)).PlusAttrs(@new, F.Public));
			Stmt("public new List<Foo> Foo { get; }",F.Property(F.Of(_("List"), Foo), Foo, F.Braces(get)).PlusAttrs(F.Public, @new));
			Stmt("partial public List<Foo> Foo { get; }",F.Property(F.Of(_("List"), Foo), Foo, F.Braces(get)).PlusAttrs(@partialWA, F.Public));
			Stmt("public partial List<Foo> Foo { get; }",F.Property(F.Of(_("List"), Foo), Foo, F.Braces(get)).PlusAttrs(F.Public, @partialWA));
			Stmt("sealed override Foo Foo { get; }",F.Property(Foo, Foo, F.Braces(get)).PlusAttrs(_(S.Sealed), _(S.Override)));
			Stmt("Foo(out a, ref b);",              F.Call(Foo, F.Attr(@out, a), F.Attr(@ref, b)));
			Stmt("yield return x;",                 F.Call(S.Return, x).PlusAttrs(WordAttr("yield")));
		}

		[Test]
		public void ExtensionMethod()
		{
			// 2016-04 OMG how did I forget to test this? So of course extension methods broke _again_
			Stmt("Foo Method(this Foo x)\n{\n}", F.Fn(Foo, _("Method"), F.List(F.Attr(F.@this, F.Var(Foo, x))), F.Braces()));
			Stmt("Foo Method(this Foo<T> x)\n{\n}", F.Fn(Foo, _("Method"), F.List(F.Attr(F.@this, F.Var(F.Of(Foo, T), x))), F.Braces()));
		}

		[Test]
		public void CSharp5AsyncAwait()
		{
			// "async" is just an ordinary word attribute so it is already supported
			Stmt("async Task Foo()\n{\n}", F.Attr(WordAttr("async"), F.Fn(_("Task"), Foo, F.List(), F.Braces())));

			// Eventually we should convince the printer to support `await`, but
			// it's a low priority since await(x) works just as well as `await x`;
			// so for now test the parser and printer separately.
			Expr("await x ** 2", F.Call(S.Exp, F.Call(_await, x), F.Literal(2)), Mode.ParserTest);
			Expr("await Foo.x", F.Call(_await, F.Dot(Foo, x)), Mode.ParserTest);
			Expr("await(Foo.x)", F.Call(_await, F.Dot(Foo, x)), Mode.PrinterTest);
			Expr("a * await Foo.x", F.Call(S.Mul, a, F.Call(_await, F.Dot(Foo, x))), Mode.ParserTest);
			Expr("await ++x", F.Call(_await, F.Call(S.PreInc, x)), Mode.ParserTest);

			// Parsing this successfully is not worth the hassle...
			//Expr("await++ + x", F.Call(S.Add, F.Call(S.PostInc, await), x));
			// ... but we do support this:
			Expr("@await++ + x", F.Call(S.Add, F.Call(S.PostInc, _await), x), Mode.ParserTest);

			// Uh-oh, it looks like the parsing of this should depend on whether the
			// enclosing function has the `async` keyword or not. If it does, it
			// should parse as `await((a).b)`, otherwise it should be `(await(a)).b`.
			// But EC# doesn't change modes in this way, so it's always parsed as
			// `await((a).b)`
			Expr("await(a).b", F.Call(_await, F.Dot(F.InParens(a), b)), Mode.ParserTest);
			Expr("await((a).b)", F.Call(_await, F.Dot(F.InParens(a), b)), Mode.PrinterTest);
			// @await is treated slightly differently, but currently the node Name is
			// "await" either way. Should it be #await when the @ sign is absent?
			Expr("@await(a).b", F.Dot(F.Call(_await, a), b), Mode.ParserTest);
		}

		[Test]
		public void CSharp6Features()
		{
			Expr("a?.b",             F.Call(S.NullDot, a, b));
			Expr("1.a?.b.c",         F.Call(S.NullDot, F.Dot(one, a), F.Dot(b, c)));
			Stmt("using static Foo.x;", Attr(F.Id(S.Static), F.Call(S.Import, F.Dot(Foo, x))));
			// Tentative tree structure - there is an undesirable inconsistency between ?. and ?[]
			Stmt("a?.b?[x].Foo;",       F.Call(S.NullDot, a, F.Dot(F.Call(S.NullIndexBracks, b, F.List(x)), Foo)));
			Stmt("int Foo(int x) => x * x;",           F.Fn(F.Int32, Foo, F.List(F.Var(F.Int32, x)), F.Call(S.Mul, x, x)));
			Stmt("int Foo => 5;",                      F.Property(F.Int32, Foo, F.Literal(5)));
			Stmt("int Foo { get; } = x * 5;",     F.Property(F.Int32, Foo, F.Missing, F.Braces(get), F.Call(S.Mul, x, F.Literal(5))));
			Stmt("public Foo this[long x] => get(x);", Attr(F.Public, F.Property(Foo, F.@this, F.List(F.Var(F.Int64, x)), F.Call(get, x))));
			Stmt("new Foo { [0] = a, [1] = b };",
				F.Call(S.New, F.Call(Foo), F.Call(S.InitializerAssignment, zero, a), F.Call(S.InitializerAssignment, one, b)));
			Stmt("new Foo { [0, 1] = a, [2] = b };",
				F.Call(S.New, F.Call(Foo), F.Call(S.InitializerAssignment, zero, one, a), F.Call(S.InitializerAssignment, two, b)));
			Stmt("try {\n  Foo();\n} catch when (true) {\n}",
				F.Call(S.Try, F.Braces(F.Call(Foo)), F.Call(S.Catch, F.Missing, F.True, F.Braces())));
			Stmt("try {\n} catch (Foo b) when (c) {\n  x();\n}",
				F.Call(S.Try, F.Braces(), F.Call(S.Catch, F.Var(Foo, b), c, F.Braces(F.Call(x)))));
		}
	}
}
