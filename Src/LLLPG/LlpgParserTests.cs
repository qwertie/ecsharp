using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using Loyc.Utilities;
using Loyc.Collections;

namespace Loyc.LLParserGenerator
{
	using S = CodeSymbols;

	[TestFixture]
	class LlpgParserTests : Assert
	{
		static LNodeFactory F = new LNodeFactory(EmptySourceFile.Default);
		static Symbol Seq = LllpgStageOneParser._Seq;
		static Symbol AndNot = GSymbol.Get("#&!");
		static LNode a = F.Id("a"), b = F.Id("b"), c = F.Id("c");

		[SetUp]
		void SetUp()
		{
			LanguageService.Current = LesLanguageService.Value;
			MessageSink.Current = MessageSink.Console;
		}

		[Test]
		public void Stage1Les_SimpleTests()
		{
			TestStage1("a", a);
			TestStage1("'a'", F.Literal('a'));
			TestStage1("123", F.Literal(123));
			TestStage1("a..b", F.Call(S.DotDot, a, b));
			TestStage1("~a", F.Call(S.NotBits, a));
			TestStage1("a | b", F.Call(S.OrBits, a, b));
			TestStage1("a / b", F.Call(S.Div, a, b));
			TestStage1("a(b | c)", F.Call(a, F.Call(S.OrBits, b, c)));
			TestStage1("()", F.Call(Seq));
			TestStage1("a b", F.Call(Seq, a, b));
			TestStage1("(a) (b)", F.Call(Seq, a, b));
			TestStage1("{ a() }", F.Braces(F.Call(a)));
			TestStage1("&{ a b | c; }", F.Call(S.AndBits, F.Braces(F.Call(a, F.Call(S.OrBits, b, c)))));
			TestStage1("&!{ a(); b(); }", F.Call(AndNot, F.Braces(F.Call(a), F.Call(b))));
		}
		[Test]
		public void Stage1Les_MoreTests()
		{
			TestStage1("~a..b", F.Call(S.NotBits, F.Call(S.DotDot, a, b)));
			TestStage1("{ a(); } b c", F.Call(Seq, F.Braces(F.Call(a)), b, c));
			TestStage1("a (b c)", F.Call(Seq, a, F.Call(Seq, b, c)));
			TestStage1("a | (a b c)", F.Call(S.OrBits, a, F.Call(Seq, a, b, c)));
			TestStage1("a(b c)", F.Call(a, F.Call(b, c)));
			TestStage1("a | b / c", F.Call(S.Div, F.Call(S.OrBits, a, b), c));
			TestStage1("a b | c", F.Call(S.OrBits, F.Call(Seq, a, b), c));
			TestStage1("a b / c", F.Call(S.Div, F.Call(Seq, a, b), c));
			TestStage1("a..b | c", F.Call(S.OrBits, F.Call(S.DotDot, a, b)));
		}

		void TestStage1(string text, LNode expected)
		{
			var lexer = LanguageService.Current.Tokenize(text, MessageSink.Console);
			var tokens = lexer.Buffered();
			var parser = new LllpgStageOneParser(tokens, lexer.Source, MessageSink.Console);
			LNode result = parser.Parse();
			AreEqual(expected, result);
		}
	}
}
