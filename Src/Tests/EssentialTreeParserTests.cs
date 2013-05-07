using System;
using System.Collections.Generic;
using System.Text;
using Loyc.BooStyle;
using Loyc.Essentials;
using NUnit.Framework;

namespace Loyc.CompilerCore
{
	[TestFixture]
	public class EssentialTreeParserTests
	{
		[Test]
		public void Test1() {
			DoTest(
				"class Foo {\n" +
				"  int seven() { return 7; }\n" +
				"}",
				false, true,
				"_class ID LBRACE RBRACE",
					"NEWLINE _int ID LPAREN RPAREN LBRACE RBRACE NEWLINE",
						"_return INT EOS");
			DoTest(
				"class Foo:\n" +
				"  def Seven():\n" + 
				"    return _seven\n" +
				"  _seven = 7",
				true, true,
				"_class ID COLON INDENT DEDENT",
					"NEWLINE _def ID LPAREN RPAREN COLON INDENT DEDENT ID PUNC INT EOS",
						"NEWLINE _return ID NEWLINE EOS");
		}

		public void DoTest(string input, bool boo, bool success, params object[] outputs)
		{
			ILanguageStyle lang;
			ISourceFile src;
			IEnumerable<AstNode> lexer;
			if (boo) {
				lang = new BooLanguage();
				src = new StringCharSourceFile(input);
				lexer = new BooLexer(src, lang.StandardKeywords, false);
			} else {
				lang = new BooLanguage();
				src = new StringCharSourceFile(input);
				lexer = new BooLexerCore(src, lang.StandardKeywords);
			}
			EssentialTreeParser etp = new EssentialTreeParser();
			AstNode root = AstNode.New(SourceRange.Nowhere, GSymbol.Empty);

			Assert.AreEqual(success, etp.Parse(ref root, lexer));
			CheckOutput(root, outputs, 0);
		}

		private void CheckOutput(AstNode node, object[] data, int dataIndex)
		{
			string expect_s = data[dataIndex].ToString();
			if (expect_s == null)
				return; // null means "ignore these child tokens"
			string[] expTokens = expect_s.Split(' ');
			Assert.AreEqual(expTokens.Length, node.ChildCount);
			for (int i = 0; i < expTokens.Length; i++) {
				Assert.AreEqual(expTokens[i], node.Children[i].NodeType.Name);
				if (node.Children[i].ChildCount > 0)
					CheckOutput(node.Children[i], data, ++dataIndex);
			}
		}
	}
}
