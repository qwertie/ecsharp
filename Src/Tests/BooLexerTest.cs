using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using NUnit.Framework;
using Loyc.BooStyle;
using Loyc.Runtime;
using Loyc.Utilities;
using Loyc.CompilerCore;
using Loyc.CompilerCore.ExprParsing;

namespace Loyc.BooStyle.Tests
{
	/// <summary>
	/// Tests the indentation interpretation of the BooLexer. Note that
	/// BooLexerCore is tested elsewhere, in BooLexerCoreTest.cs.
	/// </summary>
	[TestFixture]
	public class BooLexerTest
	{
		[Test]
		public void TestValidStrings()
		{
			Try("", "");
			Try("  ", "0, WS:  ");
			Try("  foo", "2, WS:  , INDENT, ID:foo, EOS, DEDENT");
			Try("  \nfoo ", "2, WS:  , NEWLINE:\n, 0, ID:foo, EOS");
			Try("  \n foo", "2, WS:  , NEWLINE:\n, 1, INDENT, ID:foo, EOS, DEDENT");
			Try("  foo\nbar", "2, WS:  , INDENT, ID:foo, NEWLINE:\n, 0, EOS, DEDENT, ID:bar, EOS");
			Try("foo: bar\n", "0, ID:foo, COLON::, INDENT, ID:bar, NEWLINE:\n, EOS, DEDENT");
			Try("foo:\n  bar", "0, ID:foo, COLON::, INDENT, NEWLINE:\n,"+
			                           "2, ID:bar, EOS, DEDENT");
			Try("foo:\n  bar\n//end", 
				"0, ID:foo, COLON::, INDENT, NEWLINE:\n,"+
			    "2, ID:bar, NEWLINE:\n,"+
				"0, SL_COMMENT://end, EOS, DEDENT");
			Try("foo:\n\tbar", "0, ID:foo, COLON::, INDENT, NEWLINE:\n,"+
			                           "2, WS:\t, ID:bar, EOS, DEDENT");
			Try("foo: bar", "0, ID:foo, COLON::, INDENT, ID:bar, EOS, DEDENT");
			Try("foo: //bar", "0, ID:foo, COLON::, INDENT, SL_COMMENT://bar, DEDENT");
			Try(".\t//hello", "2, WS:., WS:\t, SL_COMMENT://hello");
			Try(". ", "WS:.");
			Try(". \n", "2, WS:., NEWLINE:\n");
			Try(". .foo", "2, WS:., INDENT, PUNC:., ID:foo, EOS, DEDENT");
			Try(". .", "2, WS:., INDENT, PUNC:., EOS, DEDENT");
			Try("a\n. \t . b\n. c",
				"0, ID:a, NEWLINE:\n,"+
				"4, WS:., WS:\t , WS:., EOS, INDENT, ID:b, NEWLINE:\n, "+
				"2, WS:., EOS, PARTIAL_DEDENT, ID:c, EOS, DEDENT");
			Try("line\n|continuation",
				"ID:line, NEWLINE:\n, LINE_CONTINUATION:|, ID:continuation, EOS");
			Try("line\n | continuation",
				"ID:line, NEWLINE:\n, 1, LINE_CONTINUATION:|, ID:continuation, EOS");
			Try("Test1",
				"if false: raise FalseIsTrueException()\n" +
				"else: print 'QC OK!'",
				"_if, _false, COLON::, INDENT, "+
				"_raise, ID:FalseIsTrueException, LPAREN:(, RPAREN:), NEWLINE:\n, " +
				"EOS, DEDENT, _else, COLON::, INDENT, "+
				"ID:print, SQ_STRING:'QC OK!', EOS, DEDENT");
			Try("Test2",
				"if false: \n"+
				"  raise FalseIsTrueException()\n" +
				"else:\n"+
				"  print 'QC OK!'\n",
				"0, _if, _false, COLON::, INDENT, NEWLINE:\n, " +
				"2, _raise, ID:FalseIsTrueException, LPAREN:(, RPAREN:), NEWLINE:\n, " +
				"0, EOS, DEDENT, _else, COLON::, INDENT, NEWLINE:\n, " +
				"2, ID:print, SQ_STRING:'QC OK!', NEWLINE:\n, EOS, DEDENT");
			Try("Test3",
				" /*only a test*/\n" +
				"class Foo:\n"+
				"   def Bar:\n"+
				"     print x\n"+
				"   /*int*/ x=2\n",
				"1, WS: , ML_COMMENT:/*only a test*/, NEWLINE,"+
				"0, _class, ID:Foo, COLON::, INDENT, NEWLINE,"+
				"3, _def, ID:Bar, COLON::, INDENT, NEWLINE,"+
				"5, ID:print, ID:x, NEWLINE,"+
				"3, ML_COMMENT:/*int*/, EOS, DEDENT, ID:x, PUNC:=, INT:2, NEWLINE,"+
				"   EOS, DEDENT");
			Try("Test4",
				"if (x ?\n  y : z): print z",
				"0, _if, LPAREN:(, ID:x, PUNC:?, NEWLINE:\n, " +
				"2, ID:y, COLON::, ID:z, RPAREN:), COLON::, INDENT, ID:print, ID:z,"+
				"   EOS, DEDENT");
		}
		
		void Try(string inp, string toks) { Try(inp, inp, toks); }
		public void Try(string testName, string inputStr, string tokStrs)
		{
			StringCharSourceFile input = new StringCharSourceFile(inputStr, "Boo");
			BooLexer lexer = new BooLexer(input, BooLanguage.StandardKeywords, false, 2);
			IEnumerator<AstNode> lexerE = lexer.GetEnumerator();

			string[] toks = tokStrs.Split(',');
			AstNode t;
			int expectedIndent = 0;
			for (int i = 0; i < toks.Length; i++)
			{
				string wantType, wantText;
				if (Strings.SplitAt(toks[i], ':', out wantType, out wantText))
					wantType = wantType.Trim();
				else {
					if (toks[i].Length == 0)
						continue;
					int temp;
					if (int.TryParse(toks[i], out temp)) {
						expectedIndent = temp;
						continue;
					}
					wantType = wantType.Trim();
					if (wantType[0] == '_')
						wantText = wantType.Substring(1);
					else if (wantType == "NEWLINE")
						wantText = "\n";
					else
						wantText = "";
				}
				

				// Get the next token
				Assert.IsTrue(lexerE.MoveNext());
				t = lexerE.Current;
				string type = t.NodeType.Name;
				
				string msg = string.Format("\"{0}\"[{1}]: Expected {2}<{3}>({4}), got {5}<{6}>({7})",
					testName, i, wantType, wantText, expectedIndent, type, t.SourceText, t.GetTag("LineIndentation"));
				msg = msg.Replace("\n", "\\n");

				Assert.AreEqual(wantType, type, msg);
				Assert.AreEqual(wantText, t.SourceText, msg);
				if (t.NodeType != Tokens.WS && t.NodeType != Tokens.DEDENT && t.HasTag("LineIndentation"))
					Assert.AreEqual(expectedIndent, (int)t.GetTag("LineIndentation"), msg);
			}
			Assert.IsFalse(lexerE.MoveNext());
		}
	}
}