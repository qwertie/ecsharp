using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.MiniTest;
using Loyc.Syntax.Les;
using TT = Loyc.Syntax.Les.TokenType;
using S = Loyc.Syntax.CodeSymbols;
using Loyc.Syntax.Lexing;

namespace Loyc.Syntax
{
	[TestFixture]
	public class StandardTriviaInjectorTests : Assert
	{
		static LNodeFactory F = new LNodeFactory(EmptySourceFile.Default);

		static Token T(TT type, int startIndex, int length, object value) {
			return new Token((int)type, startIndex, length, value);
		}

		[Test]
		public void UnitTest()
		{
			// Testing the injector in isolation (without parser integration) is clunky, 
			// so I'm only doing it once.
			//00	{
			//10		// Leading Comment 1
			//20		/* Leading Comment 2 */
			//30		/* Leading Comment 3 */ x = y; // Trailing Comment 1
			//40		/* Trailing Comment 2 */
			//50		
			//60		y = z; TheEnd();
			//70	}
			var trivia = new DList<Token> {
				T(TT.Newline, 10, 1, WhitespaceTag.Value),
				T(TT.SLComment, 11, 1, "// Leading Comment 1"),
				T(TT.Newline, 20, 1, WhitespaceTag.Value),
				T(TT.MLComment, 21, 1, "/* Leading Comment 2 */"),
				T(TT.Newline, 30, 1, WhitespaceTag.Value),
				T(TT.MLComment, 31, 1, "/* Leading Comment 3 */"),
				T(TT.SLComment, 39, 1, "// Trailing Comment 1"),
				T(TT.Newline, 40, 1, WhitespaceTag.Value),
				T(TT.MLComment, 41, 1, "/* Trailing Comment 2 */"),
				T(TT.Newline, 50, 1, WhitespaceTag.Value),
				T(TT.Newline, 60, 1, WhitespaceTag.Value),
				T(TT.Newline, 70, 1, WhitespaceTag.Value),
			};
			var root = F.Braces(LNode.List(
				F.Call(S.Assign, LNode.List(F.Id("x", 33, 1), F.Id("y", 37, 1)), 33, 37),
				F.Call(S.Assign, LNode.List(F.Id("y", 61, 1), F.Id("z", 65, 1)), 61, 65),
				F.Call("TheEnd", 66, 67)
			), 0, 71);
			// Expected results
			var expected = Les2LanguageService.Value.Parse(
				@"@[#trivia_SLComment("" Leading Comment 1""),
				  #trivia_MLComment("" Leading Comment 2 ""),
				  #trivia_newline,
				  #trivia_MLComment("" Leading Comment 3 ""),
				  #trivia_trailing(
				    #trivia_SLComment("" Trailing Comment 1""),
				    #trivia_MLComment("" Trailing Comment 2 ""),
				    #trivia_newline)] 
				x = y;
				y = z;
				@[#trivia_appendStatement] TheEnd();", preserveComments: false);

			var injector = new StandardTriviaInjector(trivia, F.File, (int)TT.Newline, "/*", "*/", "//");
			{
				var results = injector.Run(root.Args.GetEnumerator()).ToList();
				AreEqual(3, results.Count);
				AreEqual(expected[0].PlusAttrBefore(F.Id(S.TriviaNewline)), results[0]);
				AreEqual(expected[1], results[1]);
				AreEqual(expected[2], results[2]);
			}
			{
				var results = injector.Run(new List<LNode> { root }.GetEnumerator()).ToList();
				AreEqual(1, results.Count);
				var result = results[0];
				IsTrue(result.Calls(S.Braces, 3));
				AreEqual(expected[0], result[0]);
				AreEqual(expected[1], result[1]);
				AreEqual(expected[2], result[2]);
			}
		}
	}
}
