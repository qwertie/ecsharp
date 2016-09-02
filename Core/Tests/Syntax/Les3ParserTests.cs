using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.Syntax.Les
{
	[TestFixture]
	public class Les3ParserTests : Les3PrinterAndParserTests
	{
		[Test]
		public void PrefixOpParseErrors()
		{
			Test(Mode.Stmt, 1, "?x;", F.Call(S.QuestionMark, x));
			Test(Mode.Stmt, 1, "=x;", F.Call(S.Assign, x));
			Test(Mode.Stmt, 1, ">x;", F.Call(S.GT, x));
			Test(Mode.Stmt, 1, "1 + <x;", F.Call(S.Add, one, F.Call(S.LT, x)));
			Test(Mode.Stmt, 1, "'sqrt x;", F.Call("'sqrt", x));
		}

		[Test]
		public void OtherParseErrors()
		{
			Test(Mode.Stmt, 1, "-2u;", F.Literal(new CustomLiteral("-2", (Symbol)"u")));
		}

		protected override MessageHolder Test(Mode mode, int errorsExpected, string text, params LNode[] expected)
		{
			var messages = new MessageHolder();
			var results = Les3LanguageService.Value.Parse(text, messages, mode == Mode.Expr ? ParsingMode.Expressions : ParsingMode.Statements).ToList();
			for (int i = 0; i < expected.Length; i++)
				AreEqual(expected[i], results[i]);
			AreEqual(expected.Length, results.Count);
			if (messages.List.Count != System.Math.Max(errorsExpected, 0))
			{
				messages.WriteListTo(MessageSink.Console);
				AreEqual(errorsExpected, messages.List.Count, 
					"{0} error(s) unexpected for «{1}»", messages.List.Count, text); // fail
			}
			return messages;
		}
	}
}
