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
		public void TriviaTest_Comments()
		{
			LNode node;
			node = Foo.PlusAttr(F.Trivia(S.TriviaMLComment, " before ")).PlusTrailingTrivia(F.Trivia(S.TriviaMLComment, " after "));
			Exact("/* before */Foo; /* after */", node);
			node = one.PlusAttr(F.Trivia(S.TriviaSLComment, " before ")).PlusTrailingTrivia(F.Trivia(S.TriviaSLComment, " after "));
			Exact("// before \n1;\t// after ", node);
			node = F.Call(F.Id(S.Eq).PlusAttr(F.Trivia(S.TriviaSLComment, "[")).PlusTrailingTrivia(F.Trivia(S.TriviaSLComment, "]")), 
			                     Foo.PlusAttr(F.Trivia(S.TriviaMLComment, "<")).PlusTrailingTrivia(F.Trivia(S.TriviaMLComment, ">")),
			                       x.PlusAttr(F.Trivia(S.TriviaMLComment, "{")).PlusTrailingTrivia(F.Trivia(S.TriviaMLComment, "}"))
			             ).PlusTrailingTrivia(F.Trivia(S.TriviaMLComment, " after "));
			Exact("/*<*/Foo /*>*///[\n==\t//]\n /*{*/x /*}*/; /* after */", node);
			node = F.Call(Foo).PlusAttrs(a, F.Trivia(S.TriviaSLComment, "Comment after a"), b, F.Trivia(S.TriviaMLComment, "Comment after b"), c);
			Exact("@[a] //Comment after a\n"+
			      "@[b] /*Comment after b*/@[c] Foo();", node);
		}

		[Test]
		public void TriviaTest_LineBreakBetweenAttrs()
		{
			Exact("@[a] \nFoo();", F.Call(Foo).PlusAttrs(a, F.TriviaNewline));
			Exact("@[a, b] \n@[c] \nFoo();", F.Call(Foo).PlusAttrs(a, b, F.TriviaNewline, c, F.TriviaNewline));
		}

		[Test]
		public void TriviaTest_BlankLinesBetweenStmts()
		{
			Test(Mode.Exact, 0, "a();\n\nb();\n\nc();",
				NewlineAfter(F.Call(a)),
				NewlineAfter(F.Call(b)),
				F.Call(c));
			Exact("{\n\n  a();\n\n  b();\n\n  c();\n\n};",
				F.Call(NewlineAfter(F.Id(S.Braces)),
					NewlineAfter(F.Call(a)),
					NewlineAfter(F.Call(b)),
					NewlineAfter(F.Call(c))));
		}

		[Test]
		public void TriviaTest_BlankLinesBetweenArgs()
		{
			// TODO: the arguments should be indented,
			// and maybe newlines should appear after `(` or `,`
			Exact("Foo(\na, \nb, \nc);",
				F.Call(Foo, OnNewLine(a), OnNewLine(b), OnNewLine(c)));
			Exact("Foo(\n\na, \n\nb, \n\nc);",
				F.Call(NewlineAfter(Foo), NewlineAfter(OnNewLine(a)),
					NewlineAfter(OnNewLine(b)),
					OnNewLine(c)));
		}

		[Test]
		public void TrickyPrinterCases()
		{
			// Gotta be careful how we print operators that appear to start comments,
			// and suffix operators used as prefix/infix.
			Stmt(@"a +/* b;",    AsOperator(F.Call(_("'+/*"), a, b)));
			Stmt(@"a `'>s` b;",  AsOperator(F.Call(_("'>s"), a, b)));
			Stmt(@"a `'/*` b;",  AsOperator(F.Call(_("'/*"), a, b)));
			Stmt(@"a `'//` b;",  AsOperator(F.Call(_("'//"), a, b)));
			Stmt(@"a `'++suf` b;", AsOperator(F.Call(_("'++suf"), a, b)));
			Stmt(@"`'++suf` b;",   AsOperator(F.Call(_("'++suf"), b)));
			Stmt(@"`'/\\` b;",     AsOperator(F.Call(_("'/\\"), b)));
			Stmt(@"a `':` b();",   AsOperator(F.Call(S.Colon, a, F.Call(b))));
			Stmt(@"if @a++(b);",  AsStyle(NodeStyle.Special, F.Call(_("if"), AsOperator(F.Call(_("a++"), F.Call(b))))));
		}

		[Test]
		public void MiscibilityErrors()
		{
			Exact("x & @'==(Foo, 0);", F.Call(S.AndBits, x, F.Call(S.Eq, Foo, zero)));
			Exact("@'&(x, Foo) == 0;", F.Call(S.Eq, F.Call(S.AndBits, x, Foo), zero));
			Exact("x >> 1 == a;", F.Call(S.Eq, F.Call(S.Shr, x, one), a));
			Exact("x >> @'+(a, 1);", F.Call(S.Shr, x, F.Call(S.Add, a, one)));
			Exact("@'>>(x, a) + 1;", F.Call(S.Add, F.Call(S.Shr, x, a), one));
			Exact("x >> a**1;", F.Call(S.Shr, x, F.Call(S.Exp, a, one)));
			Exact("x `Foo` @'..(a, b);", F.Call(Foo, x, F.Call(S.DotDot, a, b)).SetStyle(NodeStyle.Operator));
			Exact("x `Foo` @'*(a, b);", F.Call(Foo, x, F.Call(S.Mul, a, b))    .SetStyle(NodeStyle.Operator));
			Exact("x `Foo` a**b;", F.Call(Foo, x, F.Call(S.Exp, a, b))        .SetStyle(NodeStyle.Operator));
			Exact("x `Foo` 1 == a;", F.Call(S.Eq, F.Call(Foo, x, one).SetStyle(NodeStyle.Operator), a));
			Exact(".. @'&(a, b) && c;", F.Call(S.And, F.Call(S.DotDot, F.Call(S.AndBits, a, b)), c));
			Exact("@'..(a) & b && c;", F.Call(S.And, F.Call(S.AndBits, F.Call(S.DotDot, a), b), c));
		}

		protected override MessageHolder Test(Mode mode, int parseErrors, string str, params LNode[] nodes)
		{
			var messages = new MessageHolder();
			if (parseErrors == 0) {
				if (mode == Mode.Exact) {
					var sb = new StringBuilder();

					Les2LanguageService.Value.Printer.PrintMultiple(nodes, messages, null, "  ", "\n", sb);
					Assert.AreEqual(str, sb.ToString());
				} else {
					// Start by parsing. If parsing fails, just stop; such errors are 
					// already reported by LesParserTests so we need not report them here.
					var results = Les2LanguageService.Value.Parse(str, messages);
					if (messages.List.Count == 0)
					{
						var sb = new StringBuilder();
						foreach (LNode node in nodes)
							DoPrinterTest(node, sb);
					}
				}
			}
			return messages;
		}

		MessageHolder _messages = new MessageHolder();

		private void DoPrinterTest(LNode node, StringBuilder sb)
		{
			sb.Length = 0;
			_messages.List.Clear();
			var p = LesNodePrinter.New(sb, "\t", "\n", _messages);
			p.Print(node);
			Assert.AreEqual(0, _messages.List.Count);
			var reparsed = Les2LanguageService.Value.Parse(sb.ToString(), _messages);
			Assert.AreEqual(0, _messages.List.Count);
			Assert.AreEqual(1, reparsed.Count);
			Assert.AreEqual(node, reparsed[0]);
		}
	}
}
