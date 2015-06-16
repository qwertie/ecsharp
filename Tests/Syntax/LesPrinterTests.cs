using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Loyc.MiniTest;

namespace Loyc.Syntax.Les
{
	using S = CodeSymbols;

	/// <summary>Uses the parser tests as the basis for printer tests. Since the
	/// printer isn't done yet, we simply make sure that after parsing something, 
	/// we can print it out and re-parse what was printed to get the same Loyc tree.</summary>
	[TestFixture]
	public class LesPrinterTests : LesPrinterAndParserTests
	{
		[Test]
		public void TrickyPrinterCases()
		{
			// Gotta be careful how we print operators that appear to start comments,
			// and suffix operators used as prefix/infix.
			Stmt(@"a +/* b;",  AsOperator(F.Call(_("+/*"), a, b)));
			Stmt(@"a \/* b;",  AsOperator(F.Call(_("/*"), a, b)));
			Stmt(@"a `/*` b;", AsOperator(F.Call(_("/*"), a, b)));
			Stmt(@"a `//` b;", AsOperator(F.Call(_("//"), a, b)));
			Stmt(@"a `suf++` b;", AsOperator(F.Call(_("suf++"), a, b)));
			Stmt(@"`suf++` b;",   AsOperator(F.Call(_("suf++"), b)));
			Stmt(@"`/\` b;",      AsOperator(F.Call(_("/\\"), b)));
			Stmt(@"a `:` b();",   AsOperator(F.Call(S.Colon, a, F.Call(b))));
			Stmt(@"if @a++(b);",  AsStyle(NodeStyle.Special, F.Call(_("if"), AsOperator(F.Call(_("a++"), F.Call(b))))));
		}

		protected override void Test(bool exprMode, int parseErrors, string str, params LNode[] nodes)
		{
			if (parseErrors >= 0)
				return;
			// Start by parsing. If parsing fails, just stop; such errors are 
			// already reported by LesParserTests so we need not report them here.
			var messages = new MessageHolder();
			var results = LesLanguageService.Value.Parse(str, messages);
			if (messages.List.Count != 0)
				return;

			var sb = new StringBuilder();
			foreach (LNode node in nodes)
				DoPrinterTest(node, sb);
		}

		MessageHolder _messages = new MessageHolder();

		private void DoPrinterTest(LNode node, StringBuilder sb)
		{
			sb.Length = 0;
			_messages.List.Clear();
			var p = LesNodePrinter.New(sb, "\t", "\n", _messages);
			p.Print(node);
			Assert.AreEqual(0, _messages.List.Count);
			var reparsed = LesLanguageService.Value.Parse(sb.ToString(), _messages);
			Assert.AreEqual(0, _messages.List.Count);
			Assert.AreEqual(1, reparsed.Count);
			Assert.AreEqual(node, reparsed[0]);
		}
	}
}
