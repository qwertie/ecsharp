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
	/// <summary>EC# parser tests. Most of the tests are inherited.</summary>
	[TestFixture]
	public class EcsParserTests : EcsPrinterAndParserTests
	{
		Func<Token, string> _oldTSS;
		[SetUp] public void SetUp()
		{
			_oldTSS = Token.ToStringStrategy;
			Token.ToStringStrategy = TokenExt.ToString;
		}
		[TearDown] public void TearDown()
		{
			Token.ToStringStrategy = _oldTSS;
		}

		protected override void Stmt(string text, LNode expected, Action<EcsPrinterOptions> configure = null, Mode mode = Mode.Both)
		{
			bool exprMode = (mode & Mode.Expression) != 0;
			if ((mode & Mode.ParserTest) == 0)
				return;
			var sink = (mode & Mode.ExpectAndDropParserError) != 0 ? new MessageHolder() : (IMessageSink)MessageSink.Console;
			// This is the easy way: 
			//LNode result = EcsLanguageService.Value.ParseSingle(text, sink, exprMode ? ParsingMode.Expressions : ParsingMode.Statements, preserveComments: true);
			// But to make debugging easier, I'll do it the long way:
			ILexer<Token> lexer = EcsLanguageService.Value.Tokenize(new UString(text), "", MessageSink.Console);
			var preprocessed = new EcsPreprocessor(lexer, true);
			var treeified = new TokensToTree(preprocessed, false);
			var parser = new EcsParser(treeified.Buffered(), lexer.SourceFile, sink);
			VList<LNode> results = exprMode ? LNode.List(parser.ExprStart(false)) : LNode.List(parser.ParseStmtsGreedy());
			//if (!preprocessed.TriviaList.IsEmpty) 
			{
				// Inject comments
				var injector = new EcsTriviaInjector(preprocessed.TriviaList, preprocessed.SourceFile, (int)TokenType.Newline, "/*", "*"+"/", "//");
				results = LNode.List(injector.Run(results.GetEnumerator()).ToList());
			}
			LNode result = results.AsLNode(S.Splice);
			AreEqual(TokenType.EOF, parser.LT0.Type(), string.Format("Parser stopped before EOF at [{0}] in {1}", parser.LT0.StartIndex, text));

			if ((mode & Mode.IgnoreTrivia) != 0)
				result = result.ReplaceRecursive(n => n.IsTrivia ? Maybe<LNode>.NoValue : n, LNode.ReplaceOpt.ProcessAttrs).Value;
			AreEqual(expected, result);
			if (sink is MessageHolder)
				GreaterOrEqual(((MessageHolder)sink).List.Count, 1, "Expected an error but got none for "+text);
		}

		[Test]
		public void PreprocessorIfAndDefineTests()
		{
			LNode intFoo = F.Call(S.Var, F.Int32, F.Assign(Foo, zero));
			Stmt("int \n#if Foo   \n <there is no foo> \n #endif \nFoo = 0;", intFoo, Mode.IgnoreTrivia);
			Stmt("int Foo \n #if Foo   \n <there is no foo> \n #endif \n = 0;", intFoo, Mode.IgnoreTrivia);
			Stmt("int Foo \n #if true  \n        = 0        \n #endif \n;", intFoo, Mode.IgnoreTrivia);
			Stmt("int Foo \n #if true&&false \n  = 0        \n #endif \n;", F.Call(S.Var, F.Int32, Foo), Mode.IgnoreTrivia);
			Stmt("#if Foo \n int Foo;  \n #else \n int Foo = 0; \n #endif", intFoo, Mode.IgnoreTrivia);
			Stmt("#if Foo \n int Foo;  \n #elif (!true) || true \n int Foo = 0; \n #else \n #error FAIL \n #endif", intFoo, Mode.IgnoreTrivia);
			Stmt("#if Foo \n int Foo;  \n #elif (!true) || true \n int Foo = 0; \n #else \n #error FAIL \n #endif\n", intFoo, Mode.IgnoreTrivia);
			Stmt("#define Foo \n #if Foo \n int Foo = 0; \n #else \n #warning FAIL \n #endif", intFoo, Mode.IgnoreTrivia);
			Stmt("#define Foo \n #undef Foo \n #if Foo \n #warning FAIL \n #else \n int Foo = 0; \n #endif", intFoo, Mode.IgnoreTrivia);
			Stmt("#if false // fake code \n #if true \n <yes> \n #else \n <no> \n #endif \n"+
			     "#else     // real code \n int Foo \n #if true \n = 0 \n #endif \n ; \n #endif", intFoo, Mode.IgnoreTrivia);
		}

		[Test]
		public void PreprocessorIfAndDefineWithTriviaTests()
		{
			// TODO: save trivia for #if...#endif blocks and add more tests
			// in which we check that the trivia round-trips well
			LNode intFoo = F.Call(S.Var, F.Int32, OnNewLine(F.Call(S.Assign, Foo, zero)));
			Stmt("int \n#if Foo   \n <there is no foo> \n #endif \nFoo = 0;", intFoo);
		}

		[Test]
		public void PreprocessorOtherTests()
		{
			LNode intFoo = F.Call(S.Var, F.Int32, F.Assign(Foo, zero));
			// Deliberately use 'Foo' which is ordinarily not a valid token
			Stmt("#region Behold, a variable named 'Foo':\n  int Foo = 0;  \n#endregion", intFoo, Mode.IgnoreTrivia);
			// We can also write '/*' and it will have no effect in a #region line
			Stmt("#region /*\n  int Foo = 0;  \n#endregion", intFoo, Mode.IgnoreTrivia);
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

		[Test]
		public void ParserTriviaBugFix()
		{
			// This bug caused trivia to move upward so it is above the property.
			// Cause: LNodeFactory.Property() wasn't updated to assign a definite target range on #property,
			//        oops, I fixed it without checking why the autodetected range was so far off.
			LNode tree = F.Splice(
				F.Attr(
					F.Trivia(S.TriviaSLComment, " This bug is only triggered for properties in certain conditions,"),
					F.Trivia(S.TriviaSLComment, " e.g. we need this comment."),
					F.Property(F.Int32, a, F.Braces(F.Call(S.get, F.Braces(F.Call(S.Return, x))))))
					.PlusTrailingTrivia(F.TriviaNewline),
				F.Attr(
					F.Trivia(S.TriviaSLComment, " The newlines seem important too."),
					F.Var(F.Int32, b))
			);
			Stmt(@"// This bug is only triggered for properties in certain conditions,
				// e.g. we need this comment.
				int a {
					get { 
						return x;
					}
				}
				
				// The newlines seem important too.
				int b;", 
				tree, Mode.ParserTest);
		}
	}
}
