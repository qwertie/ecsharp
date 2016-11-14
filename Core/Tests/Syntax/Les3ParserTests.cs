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
			Test(Mode.Stmt, 1, "'sqrt x;", Op(F.Call("'sqrt", x)));
		}

		[Test]
		public void MiscibilityErrors()
		{
			Test(Mode.Expr, 1, "x & Foo == 0;", F.Call(S.AndBits, x, F.Call(S.Eq, Foo, zero)));
			Test(Mode.Expr, 1, "0 == x & Foo;", F.Call(S.AndBits, F.Call(S.Eq, zero, x), Foo));
			Test(Mode.Expr, 1, "x >> a + 1;", F.Call(S.Add, F.Call(S.Shr, x, a), one));
			Test(Mode.Expr, 1, "1 + x << a;", F.Call(S.Add, one, F.Call(S.Shl, x, a)));
			Test(Mode.Expr, 1, "x 'Foo a + b;", Op(F.Call("'Foo", x, Op(F.Call(S.Add, a, b)))));
		}

		[Test]
		public void WordOperators()
		{
			// TODO: move this test to base class when the printer supports these new word operators
			Exact("a b c;", Op(F.Call("'b", a, c)));
			Exact("a is b as c;", Op(F.Call("'is", a, Op(F.Call("'as", b, c)))));
			Exact("a + 1 s> b && c;", Op(F.Call(S.And, Op(F.Call("'s>", Op(F.Call(S.Add, a, one)), b)), c)));
			Exact("(a) tree== [b];", Op(F.Call("'tree==", F.InParens(a), F.Call(S.Array, b))));
			Exact("{ a; } Foo (b);", Op(F.Call("'Foo", F.Braces(a), F.InParens(b))));
		}

		[Test]
		public void OtherParseErrors()
		{
			Test(Mode.Stmt, 1, "-2u;", F.Literal(new CustomLiteral("-2", (Symbol)"u")));
		}

		protected override MessageHolder Test(Mode mode, int errorsExpected, string text, params LNode[] expected)
		{
			var messages = new MessageHolder();
			var results = Les3LanguageService.Value.Parse(text, messages, mode == Mode.Expr ? ParsingMode.Expressions : ParsingMode.Statements, true).ToList();
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
