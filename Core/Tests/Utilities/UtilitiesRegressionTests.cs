using System;
using System.Collections.Generic;
using System.Linq;
using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.MiniTest;

namespace Loyc.Utilities.Tests
{
	/// <summary>Regression tests for bugs fixed in Loyc.Utilities whose subjects have
	/// no other natural home among the existing fixtures (CPTrie's UTF-8 decoder and
	/// bit-array leaf, Statistic.Merge, and GoInterface's numeric-overload ranking).</summary>
	[TestFixture]
	public class UtilitiesRegressionTests : TestHelpers
	{
		#region CPTrie

		[Test]
		public void CPStringTrieRoundTripsNonAsciiKeys()
		{
			// Regression: the 3-byte UTF-8 decode path used k2 twice and never used k3,
			// silently corrupting every key containing a character >= U+0800.
			string[] keys = {
				"abc",          // 1 byte/char
				"café",    // 2-byte char
				"߿",       // last 2-byte char
				"ࠀ",       // first 3-byte char
				"中文", // CJK
				"€100",    // euro sign
				"￿",       // last 3-byte char
			};
			var trie = new CPStringTrie<int>();
			for (int i = 0; i < keys.Length; i++)
				trie[keys[i]] = i;

			Assert.AreEqual(keys.Length, trie.Count);
			for (int i = 0; i < keys.Length; i++) {
				Assert.IsTrue(trie.ContainsKey(keys[i]), "missing key " + i);
				Assert.AreEqual(i, trie[keys[i]]);
			}

			// Enumeration reconstructs each key from its stored bytes, which is the
			// path that was corrupting them.
			var got = trie.Select(kv => kv.Key).OrderBy(k => k, StringComparer.Ordinal).ToList();
			var want = keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
			ExpectList(got.AsListSource(), want);
		}

		[Test]
		public void CPByteTrieCloneWithDenseNode()
		{
			// Regression: CPBitArrayLeaf's clone constructor copied "_values" (its own
			// still-null field) instead of "clone._values", so cloning any trie dense
			// enough to hold a bit-array leaf threw NullReferenceException.
			var trie = new CPByteTrie<int>();
			for (int i = 0; i < 256; i++)
				trie[new byte[] { (byte)i }] = i;

			var clone = new CPByteTrie<int>(trie);
			Assert.AreEqual(256, clone.Count);
			for (int i = 0; i < 256; i++)
				Assert.AreEqual(i, clone[new byte[] { (byte)i }]);

			// The clone is independent of the original
			clone[new byte[] { 7 }] = -1;
			Assert.AreEqual(7, trie[new byte[] { 7 }]);
		}

		[Test]
		public void CPByteTrieMoveLastFindsHighestKey()
		{
			// Regression: LastKeyInUse() called PositionOfLeastSignificantOne, so with
			// all 256 single-byte keys present it reported 224 (7<<5) instead of 255.
			var trie = new CPByteTrie<int>();
			for (int i = 0; i < 256; i++)
				trie[new byte[] { (byte)i }] = i;

			var e = trie.GetEnumerator();
			Assert.IsTrue(e.MovePrev());
			Assert.AreEqual(255, e.CurrentKey[0]);
			Assert.AreEqual(255, e.CurrentValue);

			// Walking backwards visits every key in descending order
			int expected = 254;
			while (e.MovePrev())
				Assert.AreEqual(expected--, e.CurrentKey[0]);
			Assert.AreEqual(-1, expected);
		}

		#endregion

		#region Statistic

		[Test]
		public void StatisticMergeTracksMax()
		{
			// Regression: Merge computed Max as Math.Max(total.Min, data[i].Min), so
			// the merged Max was really a second copy of the merged Min.
			var a = new Statistic();
			a.Add(1); a.Add(2);
			var b = new Statistic();
			b.Add(100); b.Add(200);

			var m = Statistic.Merge(a, b);
			Assert.AreEqual(1.0, m.Min);
			Assert.AreEqual(200.0, m.Max);
			Assert.AreEqual(4, m.Count);
			Assert.AreEqual(303.0, m.SumTotal);

			// Order must not matter
			var m2 = Statistic.Merge(b, a);
			Assert.AreEqual(1.0, m2.Min);
			Assert.AreEqual(200.0, m2.Max);
		}

		#endregion

		#region GoInterface

		public interface IWidener
		{
			// Both overloads below are applicable to a uint argument; GoInterface must
			// rank them consistently rather than comparing leftT against itself.
			string Widen(uint value);
		}
		public class Widener
		{
			public string Widen(long value) { return "long:" + value; }
			public string Widen(ulong value) { return "ulong:" + value; }
		}

		[Test]
		public void GoInterfaceStillWrapsAfterOverloadRankingFix()
		{
			// Regression: the signed/unsigned overload comparison called
			// PrimSize(leftT, out rightUnsigned) -- measuring the left type twice.
			var w = GoInterface<IWidener>.From(new Widener());
			Assert.IsNotNull(w);
			var result = w.Widen(7u);
			Assert.IsTrue(result == "long:7" || result == "ulong:7", "got " + result);
		}

		#endregion
	}
}
