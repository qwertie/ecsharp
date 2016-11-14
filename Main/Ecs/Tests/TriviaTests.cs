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
			// and newlines should appear after `(` or `,`
			Stmt("Foo(\na, \nb, \nc);",
				F.Call(Foo, OnNewLine(a), OnNewLine(b), OnNewLine(c)));
			Stmt("Foo\n(\na\n, \nb\n, \nc);",
				F.Call(NewlineAfter(Foo), NewlineAfter(OnNewLine(a)),
					NewlineAfter(OnNewLine(b)),
					OnNewLine(c)));
		}

		[Test]
		public void TriviaTest_Appending()
		{
			LNode stmt;
			stmt = F.Splice(F.Call(a), F.Call(b), AppendStmt(F.Call(c)));
			Stmt("a();\nb(); c();", stmt);
			stmt = F.Braces(F.Call(a), F.Call(b)).SetStyle(NodeStyle.OneLiner);
			Stmt("{ a(); b(); }", stmt);
		}

		[Test]
		public void TriviaTest_BlankLinesBetweenStmts()
		{
			Stmt("int a;\n\nint b;\n\nint c;",
				F.Splice(NewlineAfter(F.Var(F.Int32, a)),
					NewlineAfter(F.Var(F.Int32, b)),
					F.Var(F.Int32, c)));
			Stmt("{\n\n  int a;\n\n  int b;\n\n  int c;\n\n}",
				F.Call(NewlineAfter(F.Id(S.Braces)),
					NewlineAfter(F.Var(F.Int32, a)),
					NewlineAfter(F.Var(F.Int32, b)),
					NewlineAfter(F.Var(F.Int32, c))));
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
		public void TriviaTest_Methods()
		{
			var node = Attr(F.Public, @static,
				F.Fn(F.Void, Foo,
					F.List(F.Var(F.Of(S.Array, F.String), x)).PlusTrailingTrivia(SLComment(" OK")),
					OnNewLine(F.Braces(
						F.Call(a, x).PlusAttr(SLComment(" Before"))
									.PlusTrailingTrivia(LNode.List(F.TriviaNewline, SLComment(" After")))))));
			Stmt("public static void Foo(string[] x)\t// OK\n" +
				"{\n"+
				"  // Before\n" +
				"  a(x);\n" +
				"  \t// After\n"+
				"}", node);

			node = Attr(F.Public, @static,
				F.Fn(F.Void, Foo,
					Attr(MLComment(" Params: "),
						F.List(Attr(F.TriviaNewline, MLComment("arg"),
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
					F.List(F.Var(F.Of(S.Array, F.String), a)),
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
			var stmt = F.Call(S.Enum, Foo, F.List(F.UInt8), OnNewLine(F.Braces(
				F.Assign(a, one), AppendStmt(b), AppendStmt(c), AppendStmt(F.Assign(x, F.Literal(24))))));
			Stmt("enum Foo : byte\n{\n  a = 1, b, c, x = 24\n}", stmt);
			stmt = F.Call(S.Enum, Foo, F.List(F.UInt8), OnNewLine(F.Braces(
				F.Assign(a, one), b, c, F.Assign(x, F.Literal(24)))).SetStyle(NodeStyle.OneLiner));
			Stmt("enum Foo : byte\n{ a = 1, b, c, x = 24 }", stmt);
			stmt = F.Call(S.Enum, Foo, F.List(F.UInt8), F.Braces(
				F.Assign(a, one), b, c, F.Assign(x, F.Literal(24)))).SetStyle(NodeStyle.OneLiner);
			Stmt("enum Foo : byte { a = 1, b, c, x = 24 }", stmt);
		}
	}
}
