using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.Diagnostics;

namespace Loyc.Utilities
{
	[TestFixture]
	public class JPTrieTests
	{
		[Test]
		public void LeafTests()
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
			};

			JPStringTrie<string> trie = new JPStringTrie<string>();
			int seed = Environment.TickCount;
			int count = 0;
			Random r = new Random(seed);

			for (int set = 0; set < 16; set++)
			{
				string prefix = "";
				if (set > 0)
					prefix = (1 << set).ToString();
				for (int i = 0; i < ss.Length; i++) {
					string key = prefix + ss[i];
					trie.Add(key, ss[i]);
					Assert.AreEqual(++count, trie.Count);
				}
			}
			
			Assert.That(trie.Contains(new KeyValuePair<string,string>("a", "a")));
			Assert.That(!trie.Contains(new KeyValuePair<string,string>("a", "")));

			for (int set = 15; set > 0; set--)
			{
				string prefix = "";
				if (set > 0)
					prefix = (1 << set).ToString();
				for (int i = 0; i < ss.Length; i++) {
					string key = prefix + ss[i];
					Assert.That(trie.Contains(new KeyValuePair<string,string>(key, ss[i])));
					Assert.That(trie[key] == ss[i]);
					trie[key] = "!";
					Assert.That(trie[key] == "!");
					Assert.That(trie.Remove(key));
					Assert.That(!trie.ContainsKey(key));
				}
			}
		}
	}
}
