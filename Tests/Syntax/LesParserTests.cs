using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;

namespace Loyc.Syntax.Les
{
	using S = CodeSymbols;

	[TestFixture]
	public class LesParserTests : LesPrinterAndParserTests
	{
		[Test]
		public void ParseErrors()
		{
			Test(false, 1, "a, 5 c b; x();", a, F.Call(x));
			Test(false, 1, @"a ** b \foo;", F.Call(S.Exp, a, b));
			Test(false, 1, "a = ) b c 5", a); // again, interpretation is a bit weird, but ok
		}

		[Test]
		public void PythonModeAkaISM()
		{
			// See also IndentTokenGeneratorTests, which tests LesIndentTokenGenerator
			Stmt(@"Foo:
					a(b)", F.Call(Foo, F.Braces(F.Call(a, b))));
			Stmt(@"
				try:
					eat;
				:catch:
					crumbs
				:finally:
					hunger satisfied;".Replace("\t\t\t\t", ""),
				F.Call("try", F.Braces(_("eat")),
					 _("catch"), F.Braces(_("crumbs")),
					 _("finally"), F.Braces(F.Call("hunger", _("satisfied")))));
			Test(false, 0, @"
				if a:
					a();
				:else if b:
				.	c = b();
				.	while Foo:
				.	.	c()
				return:
				.	Foo".Replace("\t\t\t\t", ""),
				F.Call("if", a, F.Braces(F.Call(a)), _("else"), _("if"), b,
					F.Braces(F.Call(S.Assign, c, F.Call(b)), F.Call("while", Foo, F.Braces(F.Call(c))))),
				F.Call("return", F.Braces(Foo)));
			Test(false, 2, @"
				a()
				if c
					(b)
				Foo()", F.Call(a), F.Call("if", c, F.InParens(b)), F.Call(Foo));
		}

		protected override void Test(bool exprMode, int errorsExpected, string str, params LNode[] expected)
		{
			var messages = new MessageHolder();
			var results = LesLanguageService.Value.Parse(str, messages, exprMode ? ParsingService.Exprs : ParsingService.Stmts);
			for (int i = 0; i < expected.Length; i++)
			{
				var result = results[i]; // this is when parsing actually occurs
				AreEqual(expected[i], result);
			}
			AreEqual(expected.Length, results.Count);
			if (messages.List.Count != System.Math.Max(errorsExpected, 0))
			{
				messages.WriteListTo(MessageSink.Console);
				AreEqual(errorsExpected, messages.List.Count); // fail
			}
		}
	}
}
