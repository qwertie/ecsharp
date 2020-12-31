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
	public class Les2PrinterTests : Les2PrinterAndParserTests
	{
		[Test]
		public void TriviaTest_CommentsInPrinter()
		{
			LNode node;
			node = F.Call(F.Id(S.Eq).PlusAttr(F.Trivia(S.TriviaSLComment, "[")).PlusTrailingTrivia(F.Trivia(S.TriviaSLComment, "]")), 
			                     Foo.PlusAttr(F.Trivia(S.TriviaMLComment, "<")).PlusTrailingTrivia(F.Trivia(S.TriviaMLComment, ">")),
			                       x.PlusAttr(F.Trivia(S.TriviaMLComment, "{")).PlusTrailingTrivia(F.Trivia(S.TriviaMLComment, "}"))
			             ).PlusTrailingTrivia(F.Trivia(S.TriviaMLComment, " after "));
			Exact("/*<*/Foo /*>*/ //[\n  == //]\n  /*{*/x /*}*/; /* after */", node);
			node = F.Call(Foo).PlusAttrs(a, F.Trivia(S.TriviaSLComment, "Comment after a"), b, F.Trivia(S.TriviaMLComment, "Comment after b"), c);
			Exact("@[a] //Comment after a\n"+
			      "@[b] /*Comment after b*/@[c] Foo();", node);
		}

		[Test]
		public void TriviaTest_LineBreakBetweenAttrsInPrinter()
		{
			Exact("@[a] \nFoo();", F.Call(Foo).PlusAttrs(a, F.TriviaNewline));
			Exact("@[a, b] \n@[c] \nFoo();", F.Call(Foo).PlusAttrs(a, b, F.TriviaNewline, c, F.TriviaNewline));
		}

		[Test]
		public void TrickyPrinterCases()
		{
			// Gotta be careful how we print operators that appear to start comments,
			// and suffix operators used as prefix/infix.
			Stmt(@"a +/* b;",    Op(F.Call(_("'+/*"), a, b)));
			Stmt(@"a `'>s` b;",  Op(F.Call(_("'>s"), a, b)));
			Stmt(@"a `'/*` b;",  Op(F.Call(_("'/*"), a, b)));
			Stmt(@"a `'//` b;",  Op(F.Call(_("'//"), a, b)));
			Stmt(@"a `'_++` b;", Op(F.Call(S.PostInc, a, b)));
			Stmt(@"`'_++` b;",   Op(F.Call(S.PostInc, b)));
			Stmt(@"`'/\\` b;",     Op(F.Call(_("'/\\"), b)));
			Stmt(@"a `':` b();",   Op(F.Call(S.Colon, a, F.Call(b))));
			Stmt(@"if @a++(b);",  F.Call(_("if"), Op(F.Call(_("a++"), F.Call(b)))).SetBaseStyle(NodeStyle.Special));
		}

		[Test]
		public void ImmiscibilityErrors()
		{
			Exact("x & @'==(Foo, 0);", F.Call(S.AndBits, x, F.Call(S.Eq, Foo, zero)));
			Exact("@'&(x, Foo) == 0;", F.Call(S.Eq, F.Call(S.AndBits, x, Foo), zero));
			Exact("x >> 1 == a;",    F.Call(S.Eq, F.Call(S.Shr, x, one), a));
			Exact("x >> @'+(a, 1);", F.Call(S.Shr, x, F.Call(S.Add, a, one)));
			Exact("@'>>(x, a) + 1;", F.Call(S.Add, F.Call(S.Shr, x, a), one));
			Exact("x >> a**1;",      F.Call(S.Shr, x, F.Call(S.Exp, a, one)));
			//Exact("x `Foo` a .. b;", F.Call(S.DotDot, F.Call(Foo, x, a).SetStyle(NodeStyle.Operator), b));
			Exact("x `Foo` @'*(a, b);", F.Call(Foo, x, F.Call(S.Mul, a, b)).SetStyle(NodeStyle.Operator));
			Exact("x `Foo` a**b;",   F.Call(Foo, x, F.Call(S.Exp, a, b))   .SetStyle(NodeStyle.Operator));
			Exact("x `Foo` 1 == a;", F.Call(S.Eq, F.Call(Foo, x, one).SetStyle(NodeStyle.Operator), a));
			Exact(".. @'&(a, b) && c;", F.Call(S.And, F.Call(S.DotDot, F.Call(S.AndBits, a, b)), c));
			Exact("..a & b && c;",   F.Call(S.And, F.Call(S.AndBits, F.Call(S.DotDot, a), b), c));
		}

		[Test]
		public void SaveRangeIsCalled()
		{
			var ranges = new List<Triplet<ILNode, IndexRange, int>>();
			var options = new LNodePrinterOptions { SaveRange = (n, r, d) => ranges.Add(Triplet.Create(n, r, d)) };

			LNode node = F.Call(_("var"), F.Call(S.Assign, x, F.Call(S.Sub, two)));
			Stmt("var(x = -2);", node);
			string output = Les2LanguageService.Value.Print(node, null, ParsingMode.Statements, options);
			ExpectSavedRange(ranges, output, node, "var(x = -2);");
			// "(" is part of the target because if there is %trailing trivia on the target, it appears after "("
			ExpectSavedRange(ranges, output, node.Target, "var(");
			ExpectSavedRange(ranges, output, node[0], "x = -2");
			ExpectSavedRange(ranges, output, node[0][0], "x");
			ExpectSavedRange(ranges, output, node[0][1], "-2");
			ExpectSavedRange(ranges, output, node[0][1].Target, "-");
			ExpectSavedRange(ranges, output, node[0][1][0], "2");
			// The space is included because suffix trivia on an operator Target is printed after the space
			ExpectSavedRange(ranges, output, node[0].Target, "= ");

			ranges.Clear();
			LNode body, signature;
			node = F.Call(S.Lambda, signature = F.Call("MyMethod", F.Call(S.Colon, x, F.Call(S.Array, _("int")))),
			               F.Braces(body = F.Call(Foo, F.Call("'.+", x, F.Literal(123)), F.Tuple(a, b))));
			// There's extra indent inside the braces because the braces are a subexpression of `=>`
			Exact("MyMethod(x : [int]) => {\n    Foo(x .+ 123, (a; b));\n  };", node);
			output = Les2LanguageService.Value.Print(node, null, ParsingMode.Statements, options);
			ExpectSavedRange(ranges, output, node.Target, "=> ");
			ExpectSavedRange(ranges, output, signature, "MyMethod(x : [int])");
			ExpectSavedRange(ranges, output, signature.Target, "MyMethod(");
			ExpectSavedRange(ranges, output, body, "Foo(x .+ 123, (a; b));");
			ExpectSavedRange(ranges, output, body.Target, "Foo(");
			ExpectSavedRange(ranges, output, signature[0], "x : [int]");
			ExpectSavedRange(ranges, output, signature[0].Target, ": ");
			ExpectSavedRange(ranges, output, signature[0][1], "[int]");
			ExpectSavedRange(ranges, output, signature[0][1][0], "int");
			// It could be argued that the comma shouldn't be included, but it allows suffix trivia to appear after the comma
			ExpectSavedRange(ranges, output, body[0], "x .+ 123, ");
			ExpectSavedRange(ranges, output, body[0][0], "x");
			ExpectSavedRange(ranges, output, body[0][1], "123");
			ExpectSavedRange(ranges, output, body[0].Target, ".+ ");
			ExpectSavedRange(ranges, output, body[1], "(a; b)");
			ExpectSavedRange(ranges, output, body[1][0], "a; ");
			ExpectSavedRange(ranges, output, body[1][1], "b");
		}

		private void ExpectSavedRange(List<Triplet<ILNode, IndexRange, int>> ranges, string output, LNode node, string expectedSubstring)
		{
			foreach (var pair in ranges)
			{
				// Subtlety: if(node==pair.A) doesn't work for typical Target nodes, which 
				//           are regenerated each time Target is called; use Equals instead
				if (node.Equals(pair.A))
				{
					AreEqual(expectedSubstring, output.Substring(pair.B.StartIndex, pair.B.Length));
					return;
				}
			}
			Fail("Saved range not found for {0}", node);
		}


		protected override MessageHolder Test(Mode mode, int parseErrors, string expected, params LNode[] inputs)
		{
			var messages = new MessageHolder();
			var options = new Les2PrinterOptions { IndentString = "  " };
			if (parseErrors == 0) {
				if (mode == Mode.Exact) {
					var result = Les2LanguageService.Value.Print(inputs, messages, ParsingMode.Statements, options);
					Assert.AreEqual(expected, result);
				} else {
					// Start by parsing. If parsing fails, just stop; such errors are 
					// already reported by LesParserTests so we need not report them here.
					var _ = Les2LanguageService.Value.Parse(expected, msgs: messages);
					if (messages.List.All(msg => msg.Severity < Severity.Error))
						foreach (LNode input in inputs)
							DoPrinterTest(input);
				}
			}
			return messages;
		}

		private void DoPrinterTest(LNode input)
		{
			var messages = new MessageHolder();
			var printed = Les2LanguageService.Value.Print(input, messages, null);
			Assert.AreEqual(0, messages.List.Count);
			var reparsed = Les2LanguageService.Value.Parse(printed, msgs: messages);
			if (messages.List.Count != 0)
				Assert.Fail("Printed node «{0}» causes error on parsing: {1}", printed, messages.List[0].Formatted);
			Assert.AreEqual(1, reparsed.Count);
			Assert.AreEqual(input, reparsed[0],
				"Printed node «{0}» is different from original node.\n  Original: «{1}»\n  Reparsed: «{2}»", printed, input, reparsed[0]);
		}
	}
}
