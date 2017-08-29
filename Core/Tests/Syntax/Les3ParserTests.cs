using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;
using Loyc.Collections;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.Syntax.Les
{
	[TestFixture]
	public class Les3ParserTests : Les3PrinterAndParserTests
	{
		[Test]
		public void MiscibilityErrors()
		{
			Test(Mode.Expr, 1, "x & Foo == 0;", F.Call(S.AndBits, x, F.Call(S.Eq, Foo, zero)));
			Test(Mode.Expr, 1, "0 == x & Foo;", F.Call(S.AndBits, F.Call(S.Eq, zero, x), Foo));
			Test(Mode.Expr, 1, "x >> a + 1;", F.Call(S.Add, F.Call(S.Shr, x, a), one));
			Test(Mode.Expr, 1, "1 + x << a;", F.Call(S.Add, one, F.Call(S.Shl, x, a)));
		}

		[Test]
		public void OtherParseErrors()
		{
			Test(Mode.Stmt, 1, "u\"-2\";", F.Literal(new CustomLiteral("-2", (Symbol)"u")));
		}

		[Test]
		public void ParseBug()
		{
			Test(Mode.Stmt, 0, "a - 2 ** b", F.Call(S.Sub, a, F.Call(S.Exp, two, b)));
			// This was parsed as (a - 2) ** b
			Test(Mode.Stmt, 0, "a -2 ** b", F.Call(S.Sub, a, F.Call(S.Exp, two, b)));
		}

		protected override MessageHolder Test(Mode mode, int errorsExpected, string text, params LNode[] expected)
		{
			var messages = new MessageHolder();
			var results = Les3LanguageService.Value.Parse(text, messages, mode == Mode.Expr ? ParsingMode.Expressions : ParsingMode.Statements, true).ToList();
			if (messages.List.Count != System.Math.Max(errorsExpected, 0))
			{
				messages.WriteListTo(ConsoleMessageSink.Value);
				AreEqual(errorsExpected, messages.List.Count, 
					"Error count was {0} for «{1}»", messages.List.Count, text); // fail
			}
			for (int i = 0; i < expected.Length; i++)
				AreEqual(expected[i], results.TryGet(i, null));
			AreEqual(expected.Length, results.Count, "Got more result nodes than expected");
			return messages;
		}
	}
}
