using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;
using Loyc.Syntax;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.Ecs.Tests
{
	partial class EcsPrinterAndParserTests
	{
		[Test]
		public void TriviaTest_BlankLinesBetweenArgs()
		{
			// TODO: the arguments should be indented,
			// and newlines should appear after `,` (not before)
			Stmt("Foo(\na, \nb, \nc);",
				F.Call(Foo, OnNewLine(a), OnNewLine(b), OnNewLine(c)));
			Stmt("Foo(\n\na, \n\nb, \n\nc);",
				F.Call(Foo, OnNewLine(OnNewLine(a)),
					OnNewLine(OnNewLine(b)),
					OnNewLine(OnNewLine(c))));
			Stmt("Foo\n\n(\na, b, \n\nc);",
				F.Call(NewlineAfter(NewlineAfter(Foo)), 
					OnNewLine(a), b, OnNewLine(OnNewLine(c))));
		}

		[Test]
		public void TriviaTest_Appending()
		{
			LNode stmt;
			stmt = F.Splice(F.Call(a), F.Call(b), AppendStmt(F.Call(c)));
			Stmt("a();\nb(); c();", stmt);
			stmt = F.Braces(AppendStmt(F.Call(a)), F.Call(b));
			Stmt("{ a();\n  b();\n}", stmt);
			stmt = F.Braces(AppendStmt(F.Call(b, one)), AppendStmt(F.Call(a, two)));
			Stmt("{ b(1); a(2); }", stmt);
		}

		[Test]
		public void TriviaTest_BlankLinesBetweenStmts()
		{
			Stmt("int a;\n\nint b;\n\nint c;",
				F.Splice(F.Var(F.Int32, a),
					OnNewLine(F.Var(F.Int32, b)),
					OnNewLine(F.Var(F.Int32, c))));
			Stmt("{\n\n  int a;\n\n  int b;\n\n  int c;\n  \n}",
				F.Braces(
					OnNewLine(F.Var(F.Int32, a)),
					OnNewLine(F.Var(F.Int32, b)),
					NewlineAfter(OnNewLine(F.Var(F.Int32, c)))));
		}

		[Test]
		public void TriviaTest_Comments()
		{
			var stmt = Attr(F.Trivia(S.TriviaMLComment, "bx"), F.Call(S.TriviaTrailing, F.Trivia(S.TriviaMLComment, "ax")), x);
			Stmt("/*bx*/x; /*ax*/",   stmt);
			Expr("/*bx*/x /*ax*/",    stmt);
			Stmt("x;",               stmt, p => p.OmitComments = true, Mode.PrinterTest);

			stmt = Attr(F.Trivia(S.TriviaSLComment, "bx"), F.Call(S.TriviaTrailing, F.Trivia(S.TriviaSLComment, "ax")), x);
			Expr("//bx\nx\t//ax",     stmt, p => p.OmitSpaceTrivia = true);
			Stmt("//bx\nx;\t//ax",    stmt, p => p.OmitSpaceTrivia = true);
			Stmt("x;",           stmt, p => p.OmitComments = true, Mode.PrinterTest);

			stmt = Attr(F.Call(S.TriviaTrailing, F.Trivia(S.TriviaSLComment, " leave loop")), F.Call(S.Break));
			Stmt("break;\t// leave loop", stmt);

			stmt = 
				Attr(F.Trivia(S.TriviaSLComment, " a block"), 
					F.Call(S.TriviaTrailing, F.Trivia(S.TriviaSLComment, " end of block")), 
					F.Braces(
						Attr(F.Trivia(S.TriviaSLComment, " set x to zero"),
							F.Call(S.TriviaTrailing, F.Trivia(S.TriviaSLComment, " x was set to zero")),
							F.Assign(x, Attr(F.Call(S.TriviaTrailing, F.Trivia(S.TriviaMLComment, "new value")), zero)
						))));
			Stmt("// a block\n{\n"+
				"  // set x to zero\n  x = 0 /*new value*/;\t// x was set to zero\n"+
				"}\t// end of block", stmt);
		}

		[Test]
		public void TriviaTest_Attributes()
		{
			var node = Attr(_("Test"), _("Benchmark"), F.TriviaNewline, _("Test2"),
				F.Call(S.Fn, F.Void, Foo, F.AltList(), F.Braces()));
			Stmt("[Test, Benchmark] \n" +
				 "[Test2] void Foo() { }", node);

			node = Attr(_("Test"), F.TriviaNewline,
				F.Call(S.Fn, F.Void, Foo, F.AltList(), F.Braces()));
			Stmt("[Test] \n" +
				 "void Foo() { }", node);

			node = Attr(_("Test"), F.TriviaNewline, F.Var(F.Int32, Foo));
			Stmt("[Test] \n" +
				 "int Foo;", node);

			node = F.Braces(Attr(_("Test"), F.TriviaNewline, F.Var(F.Int32, Foo)));
			Stmt("{\n  [Test] \n" +
				    "  int Foo;\n"+
			     "}", node);

			node = F.Braces(Attr(_("Test"), F.TriviaNewline, 
			                F.Public, F.Var(F.Int32, Foo)));
			Stmt("{\n  [Test] \n" +
					"  public int Foo;\n" +
				 "}", node);

			node = Attr(_("Test"), SLComment(" NUnit"),
					 F.Call(_("EditorBrowsable"), F.Dot(_("EditorBrowsableState"), _("Never"))), 
					 F.TriviaNewline, F.Public,
				F.Call(S.Fn, F.Void, Foo, F.AltList(), F.Braces()));
			Stmt("[Test] // NUnit\n" +
			     "[EditorBrowsable(EditorBrowsableState.Never)] \n" +
			     "public void Foo() { }", node);
		}

		[Test]
		public void TriviaTest_Methods()
		{
			var node = Attr(F.Public, @static,
				F.Fn(F.Void, Foo,
					F.AltList(F.Var(F.Of(S.Array, F.String), x)).PlusTrailingTrivia(SLComment(" OK")),
					OnNewLine(F.Braces(
						F.Call(a, x).PlusAttr(SLComment(" Before"))
									.PlusTrailingTrivia(LNode.List(F.TriviaNewline, SLComment(" After")))))));
			Stmt("public static void Foo(string[] x)\t// OK\n" +
				"{\n"+
				"  // Before\n" +
				"  a(x);\n" +
				"  // After\n"+
				"}", node);

			node = Attr(F.Public, @static,
				F.Fn(F.Void, Foo,
					Attr(MLComment(" Params: "),
						F.AltList(Attr(F.TriviaNewline, MLComment("arg"),
							F.Var(F.Of(S.Array, F.String), Attr(MLComment("name->"), x)))
							.PlusTrailingTrivia(MLComment("<-name")))),
					F.Braces(F.Call(a, x))));
			Stmt("public static void Foo/* Params: */(\n" +
				"  /*arg*/string[] /*name->*/x /*<-name*/) {\n" +
				"  a(x);\n"+
				"}", node);

			node = Attr(F.Public, SLComment(" can be used from the outside"),
					 @static, SLComment(" can be called without an instance"),
				F.Call(S.Fn,
					F.Void.PlusTrailingTrivia(SLComment(" return type")),
					Foo.PlusAttrs(F.TriviaNewline, MLComment("Method")),
					F.AltList(F.Var(F.Of(S.Array, F.String), a)),
					F.Braces(F.Call(x, a))));
			Stmt("public // can be used from the outside\n" +
				 "static // can be called without an instance\n" +
				 "void	// return type\n" +
				 "/*Method*/Foo(string[] a) {\n" +
				 "  x(a);\n" +
				 "}", node);
		}

		[Test]
		public void TriviaTest_Enums()
		{
			var stmt = F.Call(S.Enum, Foo, F.AltList(F.UInt8), OnNewLine(F.Braces(
				F.Assign(a, one), AppendStmt(b), AppendStmt(c), AppendStmt(F.Assign(x, F.Literal(24))))));
			Stmt("enum Foo : byte\n{\n  a = 1, b, c, x = 24\n}", stmt);
			stmt = F.Call(S.Enum, Foo, F.AltList(F.UInt8), OnNewLine(BracesOnOneLine(
				F.Assign(a, two), b, c, F.Assign(x, F.Literal(24)))));
			Stmt("enum Foo : byte\n{ a = 2, b, c, x = 24 }", stmt);
			stmt = F.Call(S.Enum, Foo, F.AltList(F.UInt8), BracesOnOneLine(
				F.Assign(a, zero), F.Assign(b, one), c, F.Assign(x, F.Literal(24))));
			Stmt("enum Foo : byte { a = 0, b = 1, c, x = 24 }", stmt);
		}

		[Test]
		public void TriviaTest_Region()
		{
			// This syntax tree takes into account that the trivia injector
			// strips out certain newlines: the implicit newline before each 
			// statement in a braced block, and the implicit newline before 
			// the closing brace. However, the newline before and after each 
			// preprocessor directive is not stripped out.
			LNode node = F.Call(S.Class, Foo, F.AltList(), F.Braces(
				 Attr(F.Trivia(S.TriviaRegion, " The Variable"), F.TriviaNewline,
					 _("Attribute"), F.TriviaNewline,
					 F.Public,
					 F.Call(S.TriviaTrailing, F.TriviaNewline, F.Trivia(S.TriviaEndRegion, ""), F.TriviaNewline),
					 F.Var(F.Int32, x)),
				 Attr(
					 F.Trivia(S.TriviaRegion, " The Constructor"), F.TriviaNewline,
					 F.TriviaNewline,
					 F.Call(S.TriviaTrailing,
					   F.TriviaNewline, F.TriviaNewline,
					   F.Trivia(S.TriviaEndRegion, "!")),
					 F.Call(S.Constructor, F.Missing, Foo, F.AltList(), F.Braces(
						 F.Call(S.Assign, x, one)
					 )))
				));
			Stmt("class Foo {\n" +
			     "  #region The Variable\n" +
			     "  [Attribute] \n" +
			     "  public int x;\n" +
			     "  #endregion\n" +
			     "  \n" +
			     "  #region The Constructor\n" +
			     "  \n" +
			     "  Foo() {\n" +
			     "    x = 1;\n" +
			     "  }\n" +
			     "  \n" +
			     "  #endregion!\n" +
			     "}", node);
		}
	}
}
