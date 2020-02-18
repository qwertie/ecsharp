using Loyc.Collections.Impl;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Collections.Tests
{
	// TODO: write (a lot more) tests for BMultiMap
	[TestFixture]
	public class BMultiMapTests : TestHelpers
	{
		int _nodeSize;
		public BMultiMapTests(int nodeSize) => _nodeSize = nodeSize;

		[Test]
		public void BasicTest()
		{
			var map = new BMultiMap<int, string>(_nodeSize);

			map.Add(1, "one");
			map.Add(2, "two");
			map.Add(2, "to");
			map.Add(2, "too");
			map.Add(2, "tew");

			Assert.AreEqual(5, map.Count);
			Assert.IsFalse(map[0].Contains("zero"));
			Assert.IsFalse(map[2].Contains("doesn't contain this"));
			Assert.IsTrue(map[2].Contains("too"));

			ExpectList(map, P(1, "one"), P(2, "tew"), P(2, "to"), P(2, "too"), P(2, "two"));

			Assert.IsFalse(map[2].Remove("not actually in collection"));
			ExpectList(map, P(1, "one"), P(2, "tew"), P(2, "to"), P(2, "too"), P(2, "two"));
			Assert.IsTrue(map[2].Remove("to"));
			ExpectList(map, P(1, "one"), P(2, "tew"), P(2, "too"), P(2, "two"));
		}


		[Test]
		public void TestMissingValueComparisonFunc()
		{
			// Constructor parameters (null, null) cause the value comparer to
			// pretend all values are equal. This causes Contains() and Remove()
			// to malfunction in specific ways as this test shows.
			var map = new BMultiMap<int, string>(null, null, _nodeSize);

			map.Add(1, "one");
			map.Add(2, "two");
			map.Add(2, "to");
			map.Add(2, "too");
			map.Add(2, "tew");

			Assert.AreEqual(5, map.Count);
			Assert.IsFalse(map[0].Contains("zero"));
			Assert.IsTrue(map[2].Contains("doesn't contain this"));

			ExpectList(map, P(1, "one"), P(2, "tew"), P(2, "too"), P(2, "to"), P(2, "two"));

			Assert.IsTrue(map[2].Remove("two"));
			ExpectList(map, P(1, "one"), P(2, "tew"), P(2, "to"), P(2, "two"));
			Assert.IsTrue(map[2].Remove("not actually in collection"));
			ExpectList(map, P(1, "one"), P(2, "to"), P(2, "two"));
		}

		static KeyValuePair<K, V> P<K, V>(K k, V v) => new KeyValuePair<K, V>(k, v);
	}
}
