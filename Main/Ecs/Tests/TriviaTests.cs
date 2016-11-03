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

		[Test(Fails = "Test not yet fully written")]
		public void TriviaTest_Methods()
		{
			Stmt("public static void Foo(string[] x) // OK\n" +
				"{	// Before\n" +
				"  a(x);\n" +
				"  // After\n}",
				Attr(F.Public, @static,
				F.Fn(F.Void, Foo,
					F.List(F.Var(F.Of(S.Array, F.String), x)),
					F.Braces(F.Call(a, x)))));
				
			Stmt("public static void Main /* Params: */ (\n"+
				"  /*arg*/ string[] /*name->*/ x /*<-name*/) {\n"+
				"  a(x);\n}",
				Attr(F.Public, @static,
				F.Fn(F.Void, Foo,
					F.List(F.Var(F.Of(S.Array, F.String), x)),
					F.Braces(F.Call(a, x)))));

			Stmt("public	// can be used from the outside\n"+
				 "static	// can be called without an instance\n"+
				 "void	// return type\n"+
				 "/*Method*/ Foo(string[] a) {\n"+
				 "  x(a);\n}",
				Attr(F.Public, @static,
				F.Fn(F.Void, Foo,
					F.List(F.Var(F.Of(S.Array, F.String), x)),
					F.Braces(F.Call(a, x)))));
		}


		[Test(Fails = "OneLiner not working in printer yet")]
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
