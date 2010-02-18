using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.Diagnostics;

namespace Loyc.Utilities
{
	[TestFixture]
	public class CPTrieTests
	{
		IEnumerable<string> BasicTestStrings()
		{
			string[] ss = new string[] {
				" --- THIS IS A TEST --- ",
				"",
				" ",
				"PREAMBLE:",
				"a Judish Patricia Trie",
				"contains, on occasion, long strings;",
				"if long enough, they could cause the JPLeaf to split",
				"it is interesting to note that several longish keys can fit in a JPLeaf",
				"so, I add several sets of strings to ensure that the leaves split.",
				"Noté thé Ŭnicodé.",
				@"C:\Windows",
				@"C:\Program Files",
				@"C:\Documents",				
				@"C:\Temp",
				@"C:\Temp\temp.txt",
				@"C:\Temp\temp.doc",
				"A",
				"An",
				"Ant",
				"Antler",
				"Apple",
 				"Bedtime",
				"Bed",
				"This",
				"is",
				"a",
				"test",
				"of",
				"the",
				"single",
				"word",
				"case",
				@"C:\AUTOEXEC.BAT",
			};
			
			for (int set = 0; set < 16; set++)
			{
				string prefix = "";
				if (set > 0)
					prefix = (1 << set).ToString();

				for (int i = 0; i < ss.Length; i++)
					yield return prefix + ss[i];
			}
		}
		
		[Test]
		public void BasicTests()
		{
			CPStringTrie<string> trie = new CPStringTrie<string>();
			int count = 0;

			List<string> testStrings = new List<string>(BasicTestStrings());

			// Test insertion
			foreach (string key in testStrings)
			{
				trie.Add(key, key);
				Assert.AreEqual(++count, trie.Count);
			}
			
			Assert.That(trie.Contains(new KeyValuePair<string,string>("a", "a")));
			Assert.That(!trie.Contains(new KeyValuePair<string,string>("a", "")));

			// Test forward enumeration
			count = 0;
			string last = null;
			foreach (KeyValuePair<string, string> p in trie)
			{
				Assert.That(p.Key == p.Value);
				Assert.That(last == null || string.Compare(last, p.Value, StringComparison.Ordinal) < 0);
				last = p.Value;
				count++;
			}
			Assert.AreEqual(count, trie.Count);

			// Test deletion
			Debug.Assert(testStrings.Count % 16 == 0);
			for (int i = 0; i < testStrings.Count; i++)
			{
				// Remove keys in a different order than they were added
				string key = testStrings[i ^ 0xF];
				Assert.That(trie.Contains(new KeyValuePair<string,string>(key, key)));
				Assert.That(trie[key] == key);
				trie[key] = "!";
				Assert.That(trie[key] == "!");
				Assert.That(trie.Remove(key));
				Assert.That(!trie.ContainsKey(key));
			}
			Assert.AreEqual(0, trie.Count);
		}
	}
}
