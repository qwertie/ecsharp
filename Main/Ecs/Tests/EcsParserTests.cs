using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Ecs.Parser;
using Loyc.MiniTest;
using Loyc.Syntax;
using Loyc.Syntax.Lexing;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.Ecs.Tests
{
	/// <summary>EC# parser tests</summary>
	[TestFixture]
	public class EcsParserTests : EcsPrinterAndParserTests
	{
		protected override void Stmt(string text, LNode expected, Action<EcsNodePrinter> configure = null, Mode mode = Mode.Both)
		{
			bool exprMode = (mode & Mode.Expression) != 0;
			if ((mode & Mode.ParserTest) == 0)
				return;
			// This is the easy way: 
			//LNode result = EcsLanguageService.Value.ParseSingle(text, MessageSink.Console, exprMode ? ParsingService.Exprs : ParsingService.Stmts);
			// But to make debugging easier, I'll do it the long way:
			ILexer<Token> lexer = EcsLanguageService.Value.Tokenize(new UString(text), "", MessageSink.Console);
			var preprocessed = new EcsPreprocessor(lexer);
			var treeified = new TokensToTree(preprocessed, false);
			var sink = (mode & Mode.ExpectAndDropParserError) != 0 ? new MessageHolder() : (IMessageSink)MessageSink.Console;
			var parser = new EcsParser(treeified.Buffered(), lexer.SourceFile, sink);
			LNode result = exprMode ? parser.ExprStart(false) : LNode.List(parser.ParseStmtsGreedy()).AsLNode(S.Splice);

			AreEqual(TokenType.EOF, parser.LT0.Type(), string.Format("Parser stopped before EOF at [{0}] in {1}", parser.LT0.StartIndex, text));
			AreEqual(expected, result);
			if (sink is MessageHolder)
				GreaterOrEqual(((MessageHolder)sink).List.Count, 1, "Expected an error but got none for "+text);
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
			Stmt("int[] Foo = { 0, 1, 2, };", F.Call(S.Var, F.Of(S.Array, S.Int32), 
				F.Call(S.Assign, Foo, F.Call(S.ArrayInit, zero, one, two))));
			Stmt("int[,] Foo = { { 0 }, { 1, 2, }, };", F.Call(S.Var, F.Of(S.TwoDimensionalArray, S.Int32), 
				F.Call(S.Assign, Foo, F.Call(S.ArrayInit, F.Braces(zero), F.Braces(one, two)))));
			Stmt("int[,] Foo = new[,] { { 0 }, { 1, 2, }, };", F.Call(S.Var, F.Of(S.TwoDimensionalArray, S.Int32), 
				F.Call(S.Assign, Foo, F.Call(S.New, F.Call(S.TwoDimensionalArray), F.Braces(zero), F.Braces(one, two)))));

			// 2015-05-20: parsed incorrectly
			Expr("Foo[a-1]",         F.Call(S.IndexBracks, Foo, F.Call(S.Sub, a, one)), Mode.ParserTest);
		}
	}
}
