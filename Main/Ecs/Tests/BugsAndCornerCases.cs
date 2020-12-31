using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
		public void PrintingRegressions()
		{
			Stmt("\"Hello\";", F.Literal("Hello")); // bug: was handled as an empty statement because Name.Name=="" for a literal
			Stmt("new Foo().x;", F.Dot(F.Call(S.New, F.Call(Foo)), x));            // this worked
			Stmt("new Foo().x();", F.Call(F.Dot(F.Call(S.New, F.Call(Foo)), x)));  // but this used to Assert
			// bug: 'public' attribute was suppressed by DropNonDeclarationAttributes
			Stmt("class Foo {\n  public Foo() { }\n}",
				F.Call(S.Class, Foo, F.List(), F.Braces(
					Attr(@public, F.Call(S.Constructor, F.Missing, Foo, F.List(), F.Braces())))), 
				p => p.DropNonDeclarationAttributes = true);
			// bug: 'ref' and 'out' attributes were suppressed by DropNonDeclarationAttributes
			Option(Mode.PrintBothParseFirst, 
				"Foo(out a, ref b, public static c, [#partial] x);", "Foo(out a, ref b, c, x);",  
				F.Call(Foo, Attr(@out, a), Attr(@ref, b), Attr(@public, @static, c), Attr(@partial, x)),
				p => p.DropNonDeclarationAttributes = true);
			Stmt("private set;", F.Attr(F.Private, _("set")), p => p.DropNonDeclarationAttributes = true);
		}

		[Test]
		public void BugFixes()
		{
			LNode stmt;
			Expr("(a + b).b<c>()", F.Call(F.Dot(F.InParens(F.Call(S.Add, a, b)), F.Of(b, c))));
			Stmt("@`'+`(a, b)(c, 1);", F.Call(F.Call(S.Add, a, b), c, one)); // was: "c+1"
			// once printed as "partial #var(Foo, a);" which would be parsed as a method declaration
			Stmt("partial Foo a;", Attr(@partialWA, F.Vars(Foo, a)));
			Stmt("public partial alt class BinaryTree<T> { }", F.Attr(F.Public, partialWA, WordAttr("#alt"),
				F.Call(S.Class, F.Of(F.Id("BinaryTree"), T), F.List(), F.Braces())));
			Stmt("partial Foo.T x { get; }",  Attr(partialWA, F.Property(F.Dot(Foo, T), x, BracesOnOneLine(get))));
			Stmt("IFRange<char> ICloneable<IFRange<char>>.Clone() {\n  return Clone();\n}",
				F.Fn(F.Of(_("IFRange"), F.Char), F.Dot(F.Of(_("ICloneable"), F.Of(_("IFRange"), F.Char)), _("Clone")), F.List(), F.Braces(F.Call(S.Return, F.Call("Clone")))));
			Stmt("Foo<a> IDictionary<a, b>.Keys { }",
				F.Property(F.Of(Foo, a), F.Dot(F.Of(_("IDictionary"), a, b), _("Keys")), F.Braces()));
			Stmt("T IDictionary<Symbol, T>.this[Symbol x] { get; set; }",
				F.Property(T, F.Dot(F.Of(_("IDictionary"), _("Symbol"), T), F.@this), F.List(F.Var(_("Symbol"), x)), BracesOnOneLine(get, set)));
			Stmt("Func<T, T> x = delegate(T a) {\n  return a;\n};", F.Var(F.Of(_("Func"), T, T), x, 
				F.Call(S.Lambda, F.List(F.Var(T, a)), F.Braces(F.Call(S.Return, a))).SetBaseStyle(NodeStyle.OldStyle)));
			Stmt("public static rule EmailAddress Parse(T x) { }",
				F.Attr(F.Public, _(S.Static), WordAttr("rule"), F.Fn(_("EmailAddress"), _("Parse"), F.List(F.Var(T, x)), F.Braces())));
			// Currently we're not trying to treat this as a keyword
			Stmt("dynamic Foo();", F.Fn(_("dynamic"), Foo, F.List()));
			Stmt("dynamic x;", F.Var(_("dynamic"), x));

			Token[] token = new[] { new Token((int)TokenType.Literal, 0, 0, 0, 'a') };
			var tree = new TokenTree(F.File, (ICollection<Token>)token);
			var lexer = F.Call(_("LLLPG"), _("lexer"), F.Braces(
					Attr(F.Public, F.Property(_("rule"), a, F.Literal(tree))))).SetBaseStyle(NodeStyle.Special);
			Stmt("LLLPG (lexer) {\n  public rule a @{ 'a' };\n}", lexer, Mode.ParserTest);
			Stmt("LLLPG (lexer) {\n  public rule a => @{ 'a' };\n}", lexer);
			// 2016-04 bug: ForEachStmt failed to call Up() before returning
			Stmt("{\n  foreach (var x in Foo) { }\n  Foo();\n}", 
				F.Braces(F.Call(S.ForEach, F.Vars(F.Missing, x), Foo, F.Braces()), F.Call(Foo)));
			// 2016-10 bug: `property:` was applied to both attributes
			stmt = F.Attr(a, F.Call(S.NamedArg, F.Id("property"), b), F.Private, F.Property(F.String, Foo, BracesOnOneLine(get, set)));
			Stmt("[a, property: b] private string Foo { get; set; }", stmt);
			Stmt("[a] [property: b] public string Foo { get; set; }", stmt.WithAttrChanged(2, @public), Mode.ParserTest);
			Stmt("a = (var b = x);", F.Call(S.Assign, a, F.InParens(F.Var(F.Missing, b, x))));

			// 2017-01 bug: operator>> and operator<< wouldn't parse 
			// (because there is no dedicated token for >> or <<)
			stmt = Attr(F.Id(S.Static), F.Fn(F.Int32, Attr(trivia_operator, _(S.Shl)), F.List(F.Var(Foo, x), F.Var(F.Int32, a)), 
			                            F.Braces(F.Call(S.Return, F.Call(S.Shl, x, a)))));
			Stmt("static int operator<<(Foo x, int a) {\n  return x << a;\n}", stmt);
			stmt = Attr(F.Id(S.Static), F.Fn(F.Int32, Attr(trivia_operator, _(S.Shr)), F.List(F.Var(Foo, x), F.Var(F.Int32, a)), 
			                            F.Braces(F.Call(S.Return, F.Call(S.Shr, x, a)))));
			Stmt("static int operator>>(Foo x, int a) {\n  return x >> a;\n}", stmt);

			// https://github.com/qwertie/ecsharp/issues/90
			Expr("({\n  Foo;\n})", F.InParens(F.Braces(Foo)));
			Stmt("({\n  stuff;\n});", F.InParens(F.Braces(_("stuff"))));
		}

		[Test]
		public void PrecedenceChallenges()
		{
			Expr(@"a.([] -b)",       F.Dot(a, F.Call(S._Negate, b)));
			Expr(@"a.([] -b).c",     F.Dot(a, F.Call(S._Negate, b), c));
			Expr(@"a.([] -b.c)",     F.Dot(a, F.Call(S._Negate, F.Dot(b, c))));
			Expr(@"a::([] -b)",      F.Call(S.ColonColon, a, F.Call(S._Negate, b)));
			Expr(@"a.b->c",          F.Call(S.RightArrow, F.Dot(a, b), c));
			Expr(@"a->([] b.c)",     F.Call(S.RightArrow, a, F.Dot(b, c)));
			Expr(@"a.(-b)(c)",       F.Call(F.Dot(a, F.InParens(F.Call(S._Negate, b))), c));
			// The printer should revert to prefix notation in certain cases in 
			// order to faithfully represent the original tree.
			Expr(@"a * b + c",       F.Call(S.Add, F.Call(S.Mul, a, b), c));
			Expr(@"(a + b) * c",     F.Call(S.Mul, F.InParens(F.Call(S.Add, a, b)), c));
			Expr(@"@`'+`(a, b) * c", F.Call(S.Mul, F.Call(S.Add, a, b), c));
			Expr(@"--a++",           F.Call(S.PreDec, F.Call(S.PostInc, a)));
			Expr(@"(--a)++",         F.Call(S.PostInc, F.InParens(F.Call(S.PreDec, a))));
			Expr(@"@`'--`(a)++",     F.Call(S.PostInc, F.Call(S.PreDec, a)));
			LNode a_b = F.Dot(a, b), a_b__c = F.Call(S.NullDot, F.Dot(a, b), c);
			Expr(@"a.b?.c.x",        F.Call(S.NullDot, a_b, F.Dot(c, x)));
			Expr(@"(a.b?.c).x",      F.Dot(F.InParens(a_b__c), x));
			Expr(@"@`'?.`(a.b, c).x", F.Dot(a_b__c, x));
			Expr(@"++$x",            F.Call(S.PreInc, F.Call(S.Substitute, x)));
			Expr(@"++$([Foo] x)",    F.Call(S.PreInc, F.Call(S.Substitute, Attr(Foo, x))));
			Expr(@"a ? b : c",       F.Call(S.QuestionMark, a, b, c));
			Expr(@"a ? b + x : c + x",  F.Call(S.QuestionMark, a, F.Call(S.Add, b, x), F.Call(S.Add, c, x)));
			Expr(@"a ? b = x : (c = x)",F.Call(S.QuestionMark, a, F.Assign(b, x), F.InParens(F.Assign(c, x))));
			Expr(@"++$x",            F.Call(S.PreInc, F.Call(S.Substitute, x))); // easy
			Expr(@"++--x",           F.Call(S.PreInc, F.Call(S.PreDec, x)));     // easy
			// Note: It was decided not to bother supporting `$++x`, or even
			// `$...x` which would be more convenient than writing `$(...x)`
			Expr(@"$(++x)", F.Call(S.Substitute, F.Call(S.PreInc, x)));
			Expr(@".(~x)",          F.Call(S.Dot, F.Call(S.NotBits, x)));
			Expr(@"x++.Foo",        F.Dot(F.Call(S.PostInc, x), Foo));
			Expr(@"x++.Foo()",      F.Call(F.Dot(F.Call(S.PostInc, x), Foo)));
			Expr(@"x++--.Foo",      F.Dot(F.Call(S.PostDec, F.Call(S.PostInc, x)), Foo));
			// Due to the high precedence of `$`, its argument must be in parens
			// unless it is trivial.
			Expr(@"$x",             F.Call(S.Substitute, x));
			Expr(@"$(x++)",         F.Call(S.Substitute, F.Call(S.PostInc, x)));
			Expr(@"$(Foo(x))",      F.Call(S.Substitute, F.Call(Foo, x)));
			Expr(@"$(a<b>)",        F.Call(S.Substitute, F.Of(a, b)));
			Expr(@"$(a.b)",         F.Call(S.Substitute, F.Dot(a, b)));
			Expr(@"$(a.b<c>)",      F.Call(S.Substitute, F.Dot(a, F.Of(b, c))));
			Expr(@"$((Foo) x)",     F.Call(S.Substitute, F.Call(S.Cast, x, Foo)));
			Expr(@"$(x(->Foo))",    F.Call(S.Substitute, Alternate(F.Call(S.Cast, x, Foo))));
		}

		[Test]
		public void IsAsChallenges()
		{
			Expr("x / a as Foo / b", F.Call(S.Div, F.Call(S.As, F.Call(S.Div, x, a), Foo), b));
			Expr("x / a as Foo? / b", F.Call(S.Div, F.Call(S.As, F.Call(S.Div, x, a), F.Of(_(S.QuestionMark), Foo)), b));
			Expr("x / a is Foo? / b", F.Call(S.Div, F.Call(S.Is, F.Call(S.Div, x, a), F.Of(_(S.QuestionMark), Foo)), b));
			Expr("x / a as Foo? < b", F.Call(S.LT,  F.Call(S.As, F.Call(S.Div, x, a), F.Of(_(S.QuestionMark), Foo)), b));
			Expr("x / a is Foo? < b", F.Call(S.LT,  F.Call(S.Is, F.Call(S.Div, x, a), F.Of(_(S.QuestionMark), Foo)), b));
		}
		
		[Test(Fails = "Failure caused by a bug in LLLPG")]
		public void IsAsChallenges2()
		{
			Expr("x / a as Foo < b", F.Call(S.LT, F.Call(S.As, F.Call(S.Div, x, a), Foo), b));
		}

		[Test]
		public void SpecialEcsChallenges()
		{
			Expr("Foo x = a",            F.Var(Foo, x.Name, a));
			Expr("(Foo x = a) + 1",      F.Call(S.Add, F.InParens(F.Var(Foo, x.Name, a)), one));
			// Sadly, the way the printer uses parens in this situation does not 
			// round-trip so we need separate printer and parser tests. Perhaps 
			// ideally the parser would ignore parens when there's an attr list,
			// but then the printer might have to print extra parens in some cases.
			Expr("([] Foo x = a) + 1",   F.Call(S.Add, F.Var(Foo, x.Name, a), one));
			Expr("([] Foo a) = x",       F.Assign(F.Vars(Foo, a), x));
			Expr("([] Foo a) + x",       F.Call(S.Add, F.Vars(Foo, a), x));
			Expr("x + ([] Foo a)",       F.Call(S.Add, x, F.Vars(Foo, a)));
			Expr("#label(Foo)",          F.Call(S.Label, Foo));
			Stmt("Foo:",                 F.Call(S.Label, Foo));
			LNode Foo_a = F.Call(S.NamedArg, Foo, a);
			Expr("Foo: a",               Foo_a);
			Stmt("@`'::=`(Foo, a);",     Foo_a);
			Expr("@`'::=`(Foo(x), a)",   F.Call(S.NamedArg, F.Call(Foo, x), a));
			Expr("b + (Foo: a)",         F.Call(S.Add, b, F.InParens(Foo_a)));
			Expr("b + @`'::=`(Foo, a)", F.Call(S.Add, b, Foo_a));
			// Ambiguity between multiplication and pointer declarations:
			// - multiplication at stmt level => prefix notation, except in #result or when lhs is not a complex identifier
			// - pointer declaration inside expr => generic, not pointer, notation
			Expr("a * b",                F.Call(S.Mul, a, b));
			Stmt("a `'*` b;",             F.Call(S.Mul, a, b));
			Stmt("a() * b;",             F.Call(S.Mul, F.Call(a), b));
			Expr("#result(a * b)",       F.Result(F.Call(S.Mul, a, b)));
			Stmt("{\n  a * b\n}",        F.Braces(F.Result(F.Call(S.Mul, a, b))));
			Stmt("Foo* a = x;",          F.Var(F.Of(_(S._Pointer), Foo), a.Name, x));
			// Ambiguity between bitwise not and destructor declarations
			Expr("~Foo()",               F.Call(S.NotBits, F.Call(Foo)));
			Stmt("@`'~`(Foo());",        F.Call(S.NotBits, F.Call(Foo)));
			Stmt("~Foo;",                F.Call(S.NotBits, Foo));
			Stmt("$Foo $x;",             F.Var(F.Call(S.Substitute, Foo), F.Call(S.Substitute, x)));
			Stmt("$Foo $x = 1;",         F.Var(F.Call(S.Substitute, Foo), F.Call(S.Substitute, x), one));
			Stmt("$Foo<$2> $x = 1;",     F.Var(F.Of(F.Call(S.Substitute, Foo), F.Call(S.Substitute, two)), 
			                                                              F.Call(S.Substitute, x), one));
			Expr("$Foo $x = 1",          F.Var(F.Call(S.Substitute, Foo), F.Call(S.Substitute, x), one));
		}

		[Test]
		public void TypeContext()
		{
			// Certain syntax trees can print differently in a "type context" than elsewhere.
			var FooBracks = F.Call(S.IndexBracks, Foo);
			var FooArray = F.Of(_(S.Array), Foo);
			var Foo2DArray = F.Of(_(S.TwoDimensionalArray), Foo);
			var FooNArray = F.Of(_(S.Array), F.Of(S.QuestionMark, Foo));
			var FooNullable = F.Of(_(S.QuestionMark), Foo);
			var FooPointer = F.Of(_(S._Pointer), Foo);
			Expr("Foo[]",             FooBracks);
			Expr("@`'[]`<Foo>",        FooArray);
			Expr("@`'?`<Foo>",         FooNullable);
			Expr("@`'*`<Foo>",         FooPointer);
			Stmt("#var(Foo[], a);",   F.Vars(FooBracks, a));
			Stmt("Foo[] a;",          F.Vars(FooArray, a));
			Stmt("Foo?[] a;",         F.Vars(FooNArray, a));
			Stmt("typeof(Foo?);",     F.Call(S.Typeof, FooNullable));
			Stmt("default(Foo*);",    F.Call(S.Default, FooPointer));
			Stmt("(Foo[]) a;",        F.Call(S.Cast, a, FooArray));
			Stmt("(Foo?[]) a;",       F.Call(S.Cast, a, FooNArray));
			Stmt("(Foo[,]) a;",       F.Call(S.Cast, a, Foo2DArray));
			Stmt("a(->Foo?);",        Alternate(F.Call(S.Cast, a, FooNullable)));
			Stmt("a(as Foo*);",       Alternate(F.Call(S.As, a, FooPointer)));
			Stmt("Foo!(#(Foo[]));",   F.Of(Foo, F.List(FooBracks)));
			Stmt("Foo!(#(@`'*`<Foo>));", F.Of(Foo, F.List(FooPointer)));
			Expr("checked(Foo[])",    F.Call(S.Checked, FooBracks));
			Stmt("Foo<a*> x;",        F.Vars(F.Of(Foo, F.Of(_(S._Pointer), a)), x));
			Stmt("(Foo, Foo x) a, b;", F.Vars(F.Of(_(S.Tuple), Foo, F.Var(Foo, x)), a, b));
		}

		[Test]
		public void TypeContextTuples()
		{
			// Type Tuples are printed in a "type context" (Ambiguity.TypeContext)
			// which changes printing behavior, e.g. Foo[10] normally means 
			// @`'suf[]`(Foo), but in a type context it means @`'of`(@`'[]`, Foo)
			// (which can also be written as @`'[]`<Foo>)
			var FooBracks = F.Call(S.IndexBracks, Foo);
			var FooArray = F.Of(_(S.Array), Foo);
			var FooNullable = F.Of(_(S.QuestionMark), Foo);
			LNode stmt;
			stmt = F.Vars(F.Of(_(S.Tuple), F.Var(FooNullable, x), FooArray), a, b);
			Stmt("(Foo? x, Foo[]) a, b;", stmt);
			Stmt("tuple!(2 + 0);",    F.Of(_("tuple"), F.Call(S.Add, two, zero)));
			Stmt("@tuple!(2 + 1);",   F.Of(_("tuple"), F.Call(S.Add, two, one)), Mode.ParserTest);
			Stmt("@'tuple !(2 + 2);", F.Of(_("'tuple"), F.Call(S.Add, two, two)));
			// In a type context, FooBracks shows as @`'suf[]`(Foo) instead of the usual Foo[] 
			stmt = F.Vars(F.Of(_(S.Tuple), F.Var(FooNullable, x), FooBracks), a, b);
			Stmt("#var(@'tuple !(Foo? x, @`'suf[]`(Foo)), a, b);", stmt);
			stmt = F.Vars(F.Of(_(S.Tuple), F.Var(FooNullable, x), F.Var(FooBracks, T)), a, b);
			Stmt("#var(@'tuple !(Foo? x, #var(Foo[], T)), a, b);", stmt);
		}
	}
}
