using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Loyc.Syntax.Les
{
	using S = CodeSymbols;

	[TestFixture]
	public class Les3PrinterTests : Les3PrinterAndParserTests
	{
		[Test]
		public void PrinterSpecificCustomLiterals()
		{
			// There are certain instances of CustomLiteral that the parser will 
			// not produce, which come out as ordinary literals when printed:
			Exact("1234", F.Literal(new CustomLiteral("1234", (Symbol)"n")));
			Exact("1234.5f00bar", F.Literal(new CustomLiteral("1234.5", (Symbol)"f00bar")));
			Exact(@"0x1234`f00bar`",  F.Literal(new CustomLiteral(0x1234, (Symbol)"f00bar")).SetBaseStyle(NodeStyle.HexLiteral));
			Exact(@"0x1234`WTF!\n`",  F.Literal(new CustomLiteral(0x1234, (Symbol)"WTF!\n")).SetBaseStyle(NodeStyle.HexLiteral));
			Exact(@"re""[hH]ello!""", F.Literal(new CustomLiteral(
				new System.Text.RegularExpressions.Regex("[hH]ello!"), (Symbol)"re")));
			// Support in parser planned soon
			Exact("123456789012345678901234567890z", F.Literal(BigInteger.Parse("123456789012345678901234567890")));
			// Ensure we can't trick printer into printing non-number as number
			Exact(@"f00bar""1234.5.6""", F.Literal(new CustomLiteral("1234.5.6", (Symbol)"f00bar")));
			Exact(@"n""1234.5.6""", F.Literal(new CustomLiteral("1234.5.6", (Symbol)"n")));
			Exact(@"n""1234e5.6""", F.Literal(new CustomLiteral("1234e5.6", (Symbol)"n")));
			Exact(@"n""1234567.""", F.Literal(new CustomLiteral("1234567.", (Symbol)"n")));
		}

		[Test]
		public void EvilComments()
		{
			// Normal comments
			Exact("Foo	// Comment",         Foo.PlusTrailingTrivia(F.Trivia(S.TriviaSLComment, " Comment")));
			Test(Mode.Exact, 0, "Foo	// Comment\nx()", Foo.PlusTrailingTrivia(F.Trivia(S.TriviaSLComment, " Comment")), F.Call(x));
			
			// Comments that could cause the printer to produce invalid output unless the text is changed
			Exact(@"x	// Comment\\ = 1",   F.Assign(x.PlusTrailingTrivia(F.Trivia(S.TriviaSLComment, " Comment")), one));
			Exact(@"x	// Comment\ \\ = 1", F.Assign(x.PlusTrailingTrivia(F.Trivia(S.TriviaSLComment, @" Comment\")), one));
			Exact("Foo	// C:\\\u200B\\dir\\file", Foo.PlusTrailingTrivia(F.Trivia(S.TriviaSLComment, @" C:\\dir\file")));
			Exact("Foo /***/",               Foo.PlusTrailingTrivia(F.Trivia(S.TriviaMLComment, "*")));
			Exact("Foo /* /**/ */",          Foo.PlusTrailingTrivia(F.Trivia(S.TriviaMLComment, " /**/ ")));
			Exact("Foo /* /*/* */*/*/",      Foo.PlusTrailingTrivia(F.Trivia(S.TriviaMLComment, " /*/* ")));
			Exact("Foo /* /*/**/*/ */",      Foo.PlusTrailingTrivia(F.Trivia(S.TriviaMLComment, " /*/**/*/ ")));
			Exact("Foo /* /**/*\\ */",       Foo.PlusTrailingTrivia(F.Trivia(S.TriviaMLComment, " /**/*/ ")));
			Exact("Foo\t/* \\\\ \n"+
				"	*\\*/",                   Foo.PlusTrailingTrivia(F.Trivia(S.TriviaSLComment, " \\\\ \n	*/")));
		}

		[Test]
		public void ImmiscibilityErrors()
		{
			Exact("a & b | c", F.Call(S.OrBits, F.Call(S.AndBits, a, b), c));
			Exact("x & `'==`(Foo, 0)", F.Call(S.AndBits, x, F.Call(S.Eq, Foo, zero)));
			Exact("`'&`(x, Foo) == 0", F.Call(S.Eq, F.Call(S.AndBits, x, Foo), zero));
			Exact("x >> 1 == a", F.Call(S.Eq, F.Call(S.Shr, x, one), a));
			Exact("x >> `'+`(a, 1)", F.Call(S.Shr, x, F.Call(S.Add, a, one)));
			Exact("x << `'-`(a, 2)", F.Call(S.Shl, x, F.Call(S.Sub, a, two)));
			Exact("`'>>`(x, a) + 1", F.Call(S.Add, F.Call(S.Shr, x, a), one));
			Exact("`'>>`(x, a) * 2", F.Call(S.Mul, F.Call(S.Shr, x, a), two));
			Exact("x >> a**1", F.Call(S.Shr, x, F.Call(S.Exp, a, one)));
			// No longer classified as immiscible
			Exact("x >> a / 2", F.Call(S.Shr, x, F.Call(S.Div, a, two)));
			Exact("a * 2 >> x", F.Call(S.Shr, F.Call(S.Mul, a, two), x));

			Exact("x Foo `'*`(a, b)", Op(F.Call("'Foo", x, F.Call(S.Mul, a, b))));
			Exact("`'+`(a, b) Foo c", Op(F.Call("'Foo", F.Call(S.Add, a, b), c)));
			Exact("x Foo a**b", Op(F.Call("'Foo", x, F.Call(S.Exp, a, b))));
			Exact("x Foo 1 == a", F.Call(S.Eq, Op(F.Call("'Foo", x, one)), a));
			Exact(".. `'&`(a, b) && c", F.Call(S.And, F.Call(S.DotDot, F.Call(S.AndBits, a, b)), c));
			Exact(".. a & b && c", F.Call(S.And, F.Call(S.AndBits, F.Call(S.DotDot, a), b), c));
		}

		protected override MessageHolder Test(Mode mode, int parseErrors, string expected, params LNode[] inputs)
		{
			var messages = new MessageHolder();
			var options = new Les3PrinterOptions { IndentString = "  " };
			if (parseErrors == 0) {
				if (mode == Mode.Exact) {
					var result = Les3LanguageService.Value.Print(inputs, messages, ParsingMode.Statements, options);
					Assert.AreEqual(expected, result);
				} else {
					// Start by parsing. If parsing fails, just stop; such errors are 
					// already reported by LesParserTests so we need not report them here.
					var _ = Les3LanguageService.Value.Parse(expected, msgs: messages);
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
			var printed = Les3LanguageService.Value.Print(input, messages, null);
			Assert.AreEqual(0, messages.List.Count);
			var reparsed = Les3LanguageService.Value.Parse(printed, msgs: messages);
			if (messages.List.Count != 0)
				Assert.Fail("Printed node «{0}» causes error on parsing: {1}", printed, messages.List[0].Formatted);
			Assert.AreEqual(1, reparsed.Count);
			Assert.AreEqual(input, reparsed[0],
				"Printed node «{0}» is different from original node.\n  Old: «{1}»\n  New: «{2}»", printed, input, reparsed[0]);
		}

		#region Pretty printer tests

		[Test]
		public void PrettyPrinter()
		{
			// Console output: impossible to test? Just do a visual test
			new Les3PrettyPrinter().PrintToConsole(
				F.Call(F.Dot(_("Console"), _("WriteLine")),
					F.Call(S.Add, F.Literal("Pretty print: "), F.InParens(F.True)))
					.PlusTrailingTrivia(F.Trivia(S.TriviaMLComment, " success "))
					.PlusAttr(F.Literal(123)));

			// The text in these tests indicates where each token should begin and 
			// end; the text is used to derive a "raw" test (which checks the
			// underlying control codes) and an HTML test. The end marker for
			// normal tokens is {0}, but for {Id} it is {/Id} so that the HTML test
			// (in which there is no <span> for identifiers) can easily remove both
			// {Id} and {/Id} from the string.
			TestPrettyPrint("{Id}Foo{/Id}({KeywordLiteral}null{0})", F.Call(Foo, F.Null), htmlTest: false);
			TestPrettyPrint("{Id}Foo{/Id}({KeywordLiteral}null{0})", F.Call(Foo, F.Null), rawTest: false);
			TestPrettyPrint("{KeywordLiteral}true{0} {Comment}/* hello */{0}", F.True.PlusTrailingTrivia(F.Trivia(S.TriviaMLComment, " hello ")));
			TestPrettyPrint("{Id}x{/Id} {Operator}={0} {String}'x'{0}", F.Call(S.Assign, x, F.Literal('x')));
			TestPrettyPrint("{Id}x{/Id} {Operator}+={0} {Number}123{0}", F.Call(S.AddAssign, x, F.Literal(123)));
			TestPrettyPrint("{Id}Babies{/Id} {Operator}like{0} {String}'''Shiny objects'''{0}",
				F.Call((Symbol)"'like", F.Id("Babies"), F.Literal("Shiny objects").SetBaseStyle(NodeStyle.TQStringLiteral)));
			TestPrettyPrint("{Id}x{/Id}[{CustomLiteral}s\"index\"{0}]{Operator}++{0}",
				F.Call(S.PostInc, F.Call(S.IndexBracks, x, F.Literal((Symbol)"index"))));
			TestPrettyPrint("{Id}Foo{/Id}{Operator}.{0}{Id}x{/Id}()", F.Call(F.Dot(Foo, x)));
		}

		public void PrettyPrinterAttributes()
		{
			TestPrettyPrint("{Attribute}@{0}({Id}Foo{/Id}{Closer}(){0}) x", x.PlusAttr(F.Call(Foo)));
			
			// The HTML writer extends the attribute coloring to the token afterward
			// if it is an identifier or a number.
			TestPrettyPrint("{Attribute}@Foo{0} {Attribute}@123{0} {Number}1234.5{0}", 
				F.Literal(1234.5).PlusAttr(Foo).PlusAttr(F.Literal(123)), rawTest: false, htmlTest: true);
		}

		static object[] ControlCodeTable = AddEnumPairs(typeof(LesColorCode), new List<object> { '\0' }).ToArray();
		static List<object> AddEnumPairs(Type type, List<object> list)
		{
			foreach (IConvertible value in Enum.GetValues(type)) {
				list.Add(value.ToString());
				list.Add((char)value.ToInt32(null));
			}
			return list;
		}
		static object[] HtmlCodeTable = GetHtmlCodeTable().ToArray();
		static List<object> GetHtmlCodeTable()
		{
			var list = new List<object>();
			list.Add("</span>");
			foreach (IConvertible value in Enum.GetValues(typeof(LesColorCode))) {
				var cssClass = Les3PrettyPrinter.DefaultCssClassTable[value.ToInt32(null)];
				if (cssClass != null) {
					list.Add(value.ToString());
					list.Add("<span class='" + cssClass + "'>");
				}
			}
			return list;
		}

		private void TestPrettyPrint(string pretty, LNode node, bool addPreCode = false, bool rawTest = true, bool htmlTest = true)
		{
			if (rawTest) {
				// Raw output test: must add {Opener}, {Closer} and {Separator} markers,
				// change {EndId} to {0} and remove excess {0}s
				var pretty2 = pretty.Replace("(", "{Opener}({0}").Replace("[", "{Opener}[{0}").Replace("{{", "{Opener}{{{0}")
									.Replace(")", "{Closer}){0}").Replace("]", "{Closer}]{0}").Replace("}}", "{Closer}}}{0}")
									.Replace("{/Id}", "{0}").Replace(",", "{Separator},{0}").Replace(";", "{Separator};{0}");
				var pretty3 = (pretty2.EndsWith("{0}") ? pretty2.Substring(0, pretty2.Length - 3) : pretty2).Replace("{0}{", "{");
				var expected = pretty3.FormatCore(ControlCodeTable);
				var pp = new Les3PrettyPrinter();
				StringBuilder result = pp.Print(node);
				AreEqual(expected, result.ToString());
			}
			if (htmlTest) {
				// HTML test: no spans are created for Ids so eliminate {Id} and {/Id}.
				var pretty2 = pretty.Replace("{Id}", "").Replace("{/Id}", "");
				var expected = pretty2.FormatCore(HtmlCodeTable);
				if (addPreCode)
					expected = "<pre class='highlight'><code>" + expected + "</code></pre>";
				var result = new Les3PrettyPrinter().PrintToHtml(node, addPreCode: addPreCode);
				AreEqual(expected, result.ToString());
			}
		}

		#endregion
	}
}
