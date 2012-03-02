using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using NUnit.Framework;
using Loyc.BooStyle;
using Loyc.Essentials;
using Loyc.Utilities;
using Loyc.CompilerCore;
using Loyc.CompilerCore.ExprParsing;

namespace Loyc.BooStyle.Tests
{
	[TestFixture]
	public class BooLexerCoreTest
	{
		OneTest[] tests = new OneTest[] {
			// [0]
			new OneTest(@"a \ b", @"ID:a, PUNC:\, ID:b"),
			new OneTest(@"a\b", @"ID:a\b"),
			new OneTest(@"a\&b", @"ID:a\&b"),
			new OneTest(@"a&\b", @"ID:a,PUNC:&,ID:\b"),
			new OneTest("\\\"ID with spaces\"", "ID:\\\"ID with spaces\""),
			// [5]
			new OneTest(@"\123+=4;", @"ID:\123,PUNC:+=,INT:4,EOS:;"),
			new OneTest(@"?//* foo *//\\//\\bar", 
				@"PUNC:?,SL_COMMENT://* foo *//\\,SL_COMMENT://\\,ID:bar"),
			new OneTest(@"'`hello`'`'world'`",
				@"SQ_STRING:'`hello`',BQ_STRING:`'world'`"),
			new OneTest("\"'hello'\"\"\"\"again\"\"\"",
				"DQ_STRING:\"'hello'\",TQ_STRING:\"\"\"again\"\"\""),
			new OneTest(@"'\E\S\C sequences'", @"SQ_STRING:'\E\S\C sequences'"),
			// [10]
			new OneTest(@"56", @"INT:56"),
			new OneTest(@"5_6", @"INT:5,ID:_6"),
			new OneTest(@"5_65", @"INT:5,ID:_65"),
			new OneTest(@"5_654", @"INT:5_654"),
			new OneTest(@"34_567_890", @"INT:34_567_890"),
			// [15]
			new OneTest(@"/not a#regex/", @"PUNC:/,ID:not,ID:a,SL_COMMENT:#regex/"),
			new OneTest(@"/!a regex/",    @"PUNC:/!,ID:a,ID:regex,PUNC:/"),
			new OneTest(@"/this*is-a[regex]/", @"RE_STRING:/this*is-a[regex]/"),
			new OneTest(@"/this*is-a#regex/",  @"RE_STRING:/this*is-a#regex/"),
			new OneTest(@"@/(and[this]too)/",  @"RE_STRING:@/(and[this]too)/"),
			new OneTest(@"@/RE with spaces/",  @"RE_STRING:@/RE with spaces/"),
			// FLOATING-POINT and non-simple integers not yet implemented
			//new OneTest(@"12.34.56", @"REAL:12.34,REAL:.56"),
			//new OneTest(@"0x12 34f 56l", @"INT:0x12,WS: ,REAL:34f,WS: ,INT:56l"),
			//new OneTest(@"0x12 34F 56L", @"INT:0x12,WS: ,REAL:34F,WS: ,INT:56L"),
			//new OneTest(@"0x789a.b 0x.cdef", @"REAL:0x789a.b,WS: ,REAL:0x.cdef"),
			//new OneTest(@"0x789A.B 0x.CDEF", @"REAL:0x789A.B,WS: ,REAL:0x.CDEF"),
		};
		struct OneTest {
			public OneTest(string inp, string toks) { Input=inp;Toks=toks; }
			public string Input;
			public string Toks;
			public void Test(int testNum) 
			{
				StringCharSourceFile input = new StringCharSourceFile(Input, "Boo");
				BooLexerCore lexer = new BooLexerCore(input, new Dictionary<string, Symbol>());
				IEnumerator<AstNode> lexerE = lexer.GetEnumerator();

				string[] toks = Toks.Split(',');
				AstNode t;
				for(int i = 0; i < toks.Length; i++) {
					var _ = StringExt.SplitAt(toks[i], ':');
					string wantType = _.A, wantText = _.B;
					
					wantType = wantType.Trim();
					
					// Get the next token
					Assert.IsTrue(lexerE.MoveNext());
					t = lexerE.Current;
					string type = t.NodeType.Name;
					string msg = string.Format("Test[{0}][{1}]: Expected {2}<{3}>, got {4}<{5}>", 
						testNum, i, wantType, wantText, type, t.SourceText);
					Assert.AreEqual(wantType, type, msg);
					Assert.AreEqual(wantText, t.SourceText, msg);
				}
				Assert.IsFalse(lexerE.MoveNext());
			}
		}
		
		[Test]
		public void TestValidStrings()
		{
			int i = 0;
			foreach (OneTest t in tests) {
				t.Test(i++);
			}
		}
	}
}