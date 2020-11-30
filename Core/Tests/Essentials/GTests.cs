using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections.Impl;
using Loyc.MiniTest;

namespace Loyc.Essentials.Tests
{
	[TestFixture]
	public class GTests : TestHelpers
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

		[Test]
		public void TestWordWrapping()
		{
			Pair<int,int> VariableWidth(char c) => 
				Pair.Create((int) c, char.IsDigit((char)c) ? c - '0' : 2);

			ExpectList(G.WordWrap("", 8));
			ExpectList(G.WordWrap("   ", 8), "   ");
			ExpectList(G.WordWrap("---", 8), "---");
			ExpectList(G.WordWrap("quick test", 10), "quick test");
			ExpectList(G.WordWrap("quick test", 9), "quick ", "test");
			ExpectList(G.WordWrap("The quick brown fox jumped higher than the lazy dog could have possibly imagined.", 13),
				"The quick ", "brown fox ", "jumped higher ", "than the lazy ", "dog could ", "have possibly ", "imagined.");
			// Word longer than the line length limit
			ExpectList(G.WordWrap("One time I saw a huge tyrannosaur... in a museum.", 9),
				"One time ", "I saw a ", "huge ", "tyrannosa", "ur... in ", "a museum.");
			// Hyphen test
			ExpectList(G.WordWrap("The bar-keep talked to so-and-so on Tuesday—you shoulda seen it!", 8),
				"The bar-", "keep ", "talked ", "to so-", "and-so ", "on ", "Tuesday—", "you ", "shoulda ", "seen it!");
			// The default char categorizer is programmed to break before '\0' 
			// (no common ASCII characters need this behavior, so '\0' is used just for testing purposes)
			ExpectList(G.WordWrap("\0A B\0CD\0\0E F\0G H\0", 4),
				"\0A B", "\0CD\0", "\0E F", "\0G H", "\0");
			ExpectList(G.WordWrap("Look at\tthis!\tUnusual    spaces\u200Bhere!", 7),
				"Look at\t", "this!\t", "Unusual    ", "spaces\u200B", "here!");
			// Here, the single character "9" is wider than the line width. I expected
			// the output to include "9", " is " rather than "9 ", "is " but the latter 
			// result seems better anyway, and the next test seems to indicate that the
			// algorithm works correctly even when it has to split the word "9A".
			ExpectList(G.WordWrap("Digit 9 is wider than a line but 0000 is less than 11 is".Select(VariableWidth), 8),
				"Digi", "t ", "9 ", "is ", "wide", "r ", "than ", "a ", "line ",
				"but 0000 ", "is ", "less ", "than ", "11 is");
			ExpectList(G.WordWrap("Hex 9A is 154".Select(VariableWidth), 8),
				"Hex ", "9", "A is ", "15", "4");
			ExpectList(G.WordWrap("Room 9-A is mine".Select(VariableWidth), 8),
				"Room ", "9", "-A ", "is ", "mine");
		}
	}
}
