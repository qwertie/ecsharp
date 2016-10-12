using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.MiniTest;
using Loyc.Syntax.Les;

namespace Loyc.Syntax.Tests
{
	/// <summary>Unit tests of StreamCharSource</summary>
	[TestFixture]
	public class StreamCharSourceTests : TestHelpers
	{
		Encoding _enc;
		int _bufSize = 200;

		public StreamCharSourceTests() { _enc = UTF8Encoding.Default; }
		public StreamCharSourceTests(Encoding enc, int bufSize) { _enc = enc; _bufSize = bufSize; }

		[Test]
		public void TestWithLes()
		{
			// The idea: write some simple, correct LES code, then randomize it by
			// inserting comments in a bunch of places. Lex it both ways and verify 
			// that the non-whitespace tokens are the same either way.
			string lesCode = @"import System;
				import Loyc;
				namespace TestCode {
					class TestClass {
						public answer::float = 42.0e0f;
						public ComputeAnswer()::object {
							你好();
							// slack off while pretending to work
							Thread.Sleep(1042.0 * 1000 -> int);
							return answer;
						};
					};
				};";
			MessageHolder msgs = new MessageHolder();
			var lexer = Les2LanguageService.Value.Tokenize(lesCode, msgs);
			var tokens = lexer.ToList().Where(tok => !tok.IsWhitespace).ToList();
			Debug.Assert(msgs.List.Count == 0);

			LexWithSCSAndCompare(tokens, lesCode);

			var r = new Random();
			var lesCode2 = new StringBuilder(lesCode);
			for (int i = lesCode2.Length - 1; i >= 0; i--)
				if (lesCode2[i > 0 ? i - 1 : i] == ' ')
					if (r.Next(4) == 0)
					{
						if (r.Next(2) == 0)
							lesCode2.Insert(i, "/*你好 你好*/");
						else
							lesCode2.Insert(i, "/*2345678901234567 lárgér cómmént 45678901234567*/"); // 50
					}

			LexWithSCSAndCompare(tokens, lesCode2.ToString());
		}

		private void LexWithSCSAndCompare(List<Lexing.Token> originalTokens, string lesCode)
		{
			var msgs = new MessageHolder();
			var stream = new MemoryStream(Encoding.UTF8.GetBytes(lesCode));
			var source = new StreamCharSource(stream, Encoding.UTF8.GetDecoder(), _bufSize);
			var lexer = Les2LanguageService.Value.Tokenize(source, "StreamCharSource.les", msgs);
			var tokens = lexer.ToList().Where(tok => !tok.IsWhitespace).ToList();
			Assert.AreEqual(0, msgs.List.Count);
			ExpectList(tokens, originalTokens);

			// Now reset the lexer and read the same StreamCharSource again 
			// (different code paths are used the second time)
			lexer.Reset();
			tokens = lexer.ToList().Where(tok => !tok.IsWhitespace).ToList();
			Assert.AreEqual(0, msgs.List.Count);
			ExpectList(tokens, originalTokens);
		}
	}
}
