using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;
using Loyc.Syntax;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.Ecs.Tests
{
	// Not all ambiguity-related tests are here - some of them are sprinkled into
	// the routine tests in PlainCSharpTests and ECSharpFeatureTests. The tests in 
	// here are the ambiguities that seemed to deserve their own specific test method.
	partial class EcsPrinterAndParserTests
	{
		[Test]
		public void CastAmbiguity()
		{
			// Bug 2015/08: (Foo).x was parsed as #cast(Foo, .x)
			Stmt("(Foo).x;", F.Dot(F.InParens(Foo), x));
			Stmt("(Foo) - x;", F.Call(S.Sub, F.InParens(Foo), x));
			Stmt("(Foo) + x;", F.Call(S.Add, F.InParens(Foo), x));
			Stmt("(Foo) * x;", F.Call(S.Mul, F.InParens(Foo), x));
			Stmt("(Foo) & x;", F.Call(S.AndBits, F.InParens(Foo), x));
			Stmt("([] Foo) ~ x;", F.Call(S.NotBits, F.InParens(Foo), x));
			Stmt("(Foo) ~x;", F.Call(S.Cast, F.Call(S.NotBits, x), Foo));
			Stmt("(Foo) x(a);", F.Call(S.Cast, F.Call(x, a).SetStyle(NodeStyle.Operator), Foo));
			Stmt("(Foo) `x` a;", F.Call(x, F.InParens(Foo), a).SetStyle(NodeStyle.Operator));
			Stmt("(Foo) @`'.`(x);", F.Call(S.Cast, F.Call(S.Dot, x).SetStyle(NodeStyle.Operator), Foo));
			Stmt("(Foo) a.b;",      F.Call(S.Cast, F.Dot(a, b).SetStyle(NodeStyle.Operator), Foo));
			Stmt("(Foo) @`'-`(x);", F.Call(S.Cast, F.Call(S._Negate, x).SetStyle(NodeStyle.Operator), Foo));
			Stmt("(Foo) (-x);",     F.Call(S.Cast, F.Call(S._Negate, x).SetStyle(NodeStyle.Operator), Foo), p => p.AllowChangeParentheses = true, Mode.PrinterTest);
			Stmt("(Foo) @`'--`(x);", F.Call(S.Cast, F.Call(S.PreDec, x).SetStyle(NodeStyle.Operator), Foo));
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
		/// Foo(x, y) { }
		/// </code>
		/// is a pair of constructors, or a method call plus a macro call. To 
		/// resolve this conundrum, the parser keeps track of the name of the 
		/// current class, for the sole purpose of detecting the constructor. 
		/// The printer, meanwhile, must detect a method call that may be 
		/// mistaken for a constructor and reformat it as <c>(Foo(x))</c>. Also, 
		/// when a constructor definition is printed, it must use prefix 
		/// notation if the name does not match the enclosing class:
		/// <code>
		/// #cons(@``, Foo, #(int x), { ... });
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
		/// contexts will use prefix notation to ensure that round-tripping 
		/// succeeds. When the syntax tree contains a method call to 'this' 
		/// (which is stored as #this internally), it may have to be enclosed 
		/// in parens or shown as `#this` to avoid ambiguity.
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
			var emptyConstructor = F.Call(S.Constructor, F.Missing, _(S.This), F.List(), F.Braces());
			var thisColonBase    = F.Call(S.Constructor, F.Missing, _(S.This), F.List(), F.Braces(F.Call(S.Base)));
			var thisConsNoBody   = F.Call(S.Constructor, F.Missing, _(S.This), F.List());
			var fooConstructor   = F.Call(S.Constructor, F.Missing, Foo, F.List(), F.Braces(F.Call(x)));
			var fooConsNoBody    = F.Call(S.Constructor, F.Missing, Foo, F.List());
			Action<EcsNodePrinter> allowAmbig = p => p.AllowConstructorAmbiguity = true;
			Stmt("this() { }",                          emptyConstructor);
			Stmt("#cons(@``, Foo, #());",               fooConsNoBody);
			Stmt("#cons(@``, Foo, #(), {\n  x();\n});", fooConstructor);
			Stmt("#this(x);",                           F.Call(S.This, x));
			Stmt("base(x);",                            F.Call(S.Base, x));
			Stmt("this()\n   : this(x) {\n  x;\n}",
				F.Call(S.Constructor, F.Missing, _(S.This), F.List(), F.Braces(F.Call(S.This, x), x)), allowAmbig);
			Option(Mode.PrintBothParseFirst, "#cons(@``, Foo, #(), { });", "Foo() { }",
				F.Call(S.Constructor, F.Missing, Foo, F.List(), F.Braces()), allowAmbig);
			Stmt("this()\n   : base() { }",       thisColonBase, allowAmbig);
			Stmt("this() {\n  x;\n  this(x);\n}",
				F.Call(S.Constructor, F.Missing, _(S.This), F.List(), F.Braces(x, F.Call(S.This, x))), allowAmbig);
			Stmt("this() {\n  this()\n     : base() { }\n}",
				F.Call(S.Constructor, F.Missing, _(S.This), F.List(), F.Braces(thisColonBase)));
			Stmt("this() {\n  this();\n}",
				F.Call(S.Constructor, F.Missing, _(S.This), F.List(), F.Braces(thisConsNoBody)), allowAmbig, Mode.PrinterTest);
			Stmt("this() {\n  x;\n  this();\n}",
				F.Call(S.Constructor, F.Missing, _(S.This), F.List(), F.Braces(x, F.Call(S.This))), allowAmbig);
			Stmt("this() {\n  #cons(@``, this, #());\n}",
				F.Call(S.Constructor, F.Missing, _(S.This), F.List(), F.Braces(thisConsNoBody)));
			//Stmt("this() {\n  #cons(@``, this, #(), {\n  base();\n});\n}", 
			Stmt("this() {\n  this()\n     : base() { }\n}", 
				F.Call(S.Constructor, F.Missing, _(S.This), F.List(), F.Braces(thisColonBase)), allowAmbig);
			Stmt("this() {\n  #cons(@``, this, #(), { });\n}",
				F.Call(S.Constructor, F.Missing, _(S.This), F.List(), F.Braces(emptyConstructor)));
			Stmt("class Foo {\n  Foo().x;\n}",   F.Call(S.Class, Foo, F.List(), F.Braces(
			                                          F.Dot(F.Call(Foo), x))));
			Stmt("class Foo {\n  (Foo());\n}",   F.Call(S.Class, Foo, F.List(), F.Braces(F.InParens(F.Call(Foo)))));
			Stmt("class Foo {\n  (Foo());\n}",   F.Call(S.Class, Foo, F.List(), F.Braces(F.Call(Foo))), Mode.PrinterTest);
			Stmt("class Foo {\n  Foo();\n}",                    F.Call(S.Class, Foo, F.List(), F.Braces(fooConsNoBody)));
			Stmt("class Foo {\n  Foo() {\n    x();\n  }\n}", F.Call(S.Class, Foo, F.List(), F.Braces(fooConstructor)));
			Stmt("class Foo {\n  #cons(@``, IFoo, #());\n}", F.Call(S.Class, Foo, F.List(), F.Braces(
			                                     F.Call(S.Constructor, F.Missing, IFoo, F.List()))));
			Stmt("class Foo {\n  IFoo()\n     : base() { }\n}", F.Call(S.Class, Foo, F.List(), F.Braces(
			                                     F.Call(S.Constructor, F.Missing, IFoo, F.List(), F.Braces(F.Call(S.Base))))));

			Stmt("class Foo {\n  x(Foo());\n}",  F.Call(S.Class, Foo, F.List(), F.Braces(F.Call(x, F.Call(Foo)))));

			// Printer test only
			Stmt("class Foo {\n  Foo();\n}", F.Call(S.Class, Foo, F.List(), F.Braces(F.Call(Foo))), allowAmbig, Mode.PrinterTest);
			Stmt("class Foo {\n  (Foo());\n}", F.Call(S.Class, Foo, F.List(), F.Braces(F.Call(Foo))), p => p.AllowChangeParentheses = false, Mode.PrinterTest);
			Stmt("class Foo {\n  (Foo());\n}", F.Call(S.Class, Foo, F.List(), F.Braces(F.Call(Foo))), p => p.AllowChangeParentheses = true, Mode.PrinterTest);

			// Non-keyword attributes allowed on this() but not Foo() constructor
			Stmt("partial this() { }",         Attr(partialWA, F.Call(S.Constructor, F.Missing, _(S.This), F.List(), F.Braces())));
			Stmt("class Foo {\n  partial this() { }\n}", F.Call(S.Class, Foo, F.List(), F.Braces(
			                                   Attr(partialWA, F.Call(S.Constructor, F.Missing, _(S.This), F.List(), F.Braces())))));
			Stmt("class Foo {\n  [#partial] Foo() { }\n}", F.Call(S.Class, Foo, F.List(), F.Braces(
			                                   Attr(@partial,  F.Call(S.Constructor, F.Missing, Foo, F.List(), F.Braces())))));
			Stmt("this()\n   : this(x) { }",    F.Call(S.Constructor, F.Missing, _(S.This), F.List(), F.Braces(F.Call(S.This, x))), allowAmbig);
			Stmt("partial this() { }",         Attr(partialWA, F.Call(S.Constructor, F.Missing, _(S.This), F.List(), F.Braces())));
			Stmt("this() {\n  x;\n  partial this(x) { }\n}",
				F.Call(S.Constructor, F.Missing, _(S.This), F.List(), F.Braces(x,
					Attr(partialWA, F.Call(S.Constructor, F.Missing, _(S.This), F.List(x), F.Braces())))), allowAmbig);
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
			Option(Mode.PrintBothParseFirst | Mode.IgnoreTrivia,
				"if (a)\n  @#if(b, c());\nelse\n  x();", 
			    "if (a){\n  if (b)\n    c();}\nelse\n  x();", stmt, p => p.AllowExtraBraceForIfElseAmbig = true);
			stmt = F.Call(S.If, a, ChildStmt(F.Call(S.While, Foo, ChildStmt(F.Call(S.If, b, F.Call(c))))), ChildStmt(F.Call(x)));
			Stmt("if (a)\n  while (Foo)\n    @#if(b, c());\nelse\n  x();", stmt);
		}

		[Test]
		public void ExprOrVarDeclAmbiguity()
		{
			Expr("Foo<x> a = b", F.Var(F.Of(Foo, x), a.Name, b));
			Expr("@`'*`<Foo> a = x", F.Var(F.Of(_(S._Pointer), Foo), a.Name, x));
			Expr("Foo? x = c ? a : b", F.Var(F.Of(_(S.QuestionMark), Foo), x.Name, F.Call(S.QuestionMark, c, a, b)));

			Expr("Foo ? b = c as Foo? : 0",
				F.Call(S.QuestionMark, Foo,
					F.Call(S.Assign, b, F.Call(S.As, c, F.Of(S.QuestionMark, Foo.Name))),
					zero));

			// Hardest case. If `??` could be a prefix operator then this would 
			// be able to parse as both a var decl and an expression.
			Expr("Foo ? b = c as Foo? ?? x : 0",
				F.Call(S.QuestionMark, Foo,
					F.Call(S.Assign, b, F.Call(S.NullCoalesce, F.Call(S.As, c, F.Of(S.QuestionMark, Foo.Name)), x)),
					zero));
		}

		/*The premise of the test is invalid, since `x as Foo?(x)` is a parse error
		[Test]
		public void QuestionMarkAmbiguity()
		{
			Expr("x as Foo?(a, b) in c",
				F.Call(S.In, F.Call(F.Call(S.As, x, F.Of(S.QuestionMark, Foo)), a, b), c));
			Expr("x as Foo ? (a, b) in c : c",
				F.Call(S.QuestionMark, F.Call(S.As, x, Foo), F.Call(S.In, F.Tuple(a, b), c), c));

			// The ultimate ambiguity. In fact this could parse successfully as
			// both a variable declaration AND as an expression. The "expression"
			// interpretation takes priority by default...
			Expr("Foo ? b = c as Foo? (x) : 0",
				F.Call(S.QuestionMark, Foo,
					F.Call(S.Assign, b,
						F.Call(F.Call(S.As, c, F.Of(S.QuestionMark, Foo.Name)), x)),
					zero));
			// But given a change of context, the variable decl interpretation
			// becomes the default instead.
			Expr("(Foo? b = c as Foo? (x) : 0) => {}",
				F.Call(S.Lambda,
					F.Var(F.Of(S.QuestionMark, Foo.Name), b,
						F.Call(S.QuestionMark, F.Call(S.As, c, Foo), F.InParens(x), zero)),
					F.Braces()));
		}*/
	}
}
