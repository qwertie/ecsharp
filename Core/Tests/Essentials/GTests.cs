using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;

namespace Loyc.Tests
{
	[TestFixture]
	public class GTests : Assert
	{
		[Test] public void TestSwap()
		{
			int a = 7, b = 13;
			G.Swap(ref a, ref b);
			Assert.AreEqual(7, b);
			Assert.AreEqual(13, a);
		}
		[Test] public void TestSplitCommandLineArguments()
		{
			// Give it some easy and some difficult arguments
			string input = "123: apple \t banana=\"a b\" Carrot\n"
				+ "!@#$%^&*() \"duck\" \"error \"foo\"!\ngrape   ";
			List<string> expected = new List<string>();
			expected.Add("123:");
			expected.Add("apple");
			expected.Add("banana=\"a b\"");
			expected.Add("Carrot");
			expected.Add("!@#$%^&*()");
			expected.Add("duck");
			expected.Add("\"error \"foo\"!");
			expected.Add("grape");
			
			List<string> output = G.SplitCommandLineArguments(input);
			Assert.AreEqual(output.Count, expected.Count);
			for (int i = 0; i < expected.Count; i++)
				Assert.AreEqual(output[i], expected[i]);
		}

		[Test]
		public void TestDecodeBase64Digit()
		{
			Assert.AreEqual(0, G.DecodeBase64Digit('A'));
			Assert.AreEqual(25, G.DecodeBase64Digit('Z'));
			Assert.AreEqual(26, G.DecodeBase64Digit('a'));
			Assert.AreEqual(51, G.DecodeBase64Digit('z'));
			Assert.AreEqual(52, G.DecodeBase64Digit('0'));
			Assert.AreEqual(61, G.DecodeBase64Digit('9'));
			Assert.AreEqual(62, G.DecodeBase64Digit('+'));
			Assert.AreEqual(62, G.DecodeBase64Digit('-'));
			Assert.AreEqual(62, G.DecodeBase64Digit('.'));
			Assert.AreEqual(62, G.DecodeBase64Digit('~'));
			Assert.AreEqual(63, G.DecodeBase64Digit('/'));
			Assert.AreEqual(63, G.DecodeBase64Digit('_'));
			Assert.AreEqual(63, G.DecodeBase64Digit(','));
			Assert.AreEqual(-1, G.DecodeBase64Digit(' '));
			Assert.AreEqual(-1, G.DecodeBase64Digit('@'));
			Assert.AreEqual(-1, G.DecodeBase64Digit('['));
			Assert.AreEqual(-1, G.DecodeBase64Digit('`'));
			Assert.AreEqual(-1, G.DecodeBase64Digit('{'));
		}
		[Test]
		public void TestEncodeBase64Digit()
		{
			Assert.AreEqual('A', G.EncodeBase64Digit(0));
			Assert.AreEqual('Z', G.EncodeBase64Digit(25));
			Assert.AreEqual('a', G.EncodeBase64Digit(26));
			Assert.AreEqual('z', G.EncodeBase64Digit(51));
			Assert.AreEqual('0', G.EncodeBase64Digit(52));
			Assert.AreEqual('9', G.EncodeBase64Digit(61));
			Assert.AreEqual('+', G.EncodeBase64Digit(62));
			Assert.AreEqual('/', G.EncodeBase64Digit(63));
			Assert.AreEqual('?', G.EncodeBase64Digit(62, '?', '@'));
			Assert.AreEqual('@', G.EncodeBase64Digit(63, '?', '@'));
			Assert.AreEqual('A', G.EncodeBase64Digit(64)); // wraparound
			Assert.AreEqual('/', G.EncodeBase64Digit(-1)); // wraparound
		}
	}
}
