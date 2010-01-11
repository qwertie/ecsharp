using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace Loyc.Utilities
{
	[TestFixture]
	public class JPTrieTests
	{
		[Test]
		public void LeafTests()
		{
			string[] ss = new string[] {
				"A",
				"An",
				"Ant",
				"Apple",
				"Hundred",
				"Hun",
				"",
			};

			JPStringTrie<string> trie = new JPStringTrie<string>();

			for (int i = 0; i < ss.Length; i++)
				trie.Add(ss[i], ss[i]);
		}
	}
}
