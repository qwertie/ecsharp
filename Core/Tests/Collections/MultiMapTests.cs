using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Loyc.MiniTest;

namespace Loyc.Collections.Tests
{
	public class MultiMapTests : CollectionTests<MultiMap<int,int>, KeyValuePair<int,int>>
	{
		static KeyValuePair<int, int> P(int x) => new KeyValuePair<int, int>(x, -x);
		static KeyValuePair<int, int> P(int x, int y) => new KeyValuePair<int, int>(x, -x);

		Random _r;
		int _counter;

		public MultiMapTests(int randomSeed) : base(() => new MultiMap<int, int>(),
			new[] { P(0), P(1), P(2), P(3), P(4), P(int.MinValue), P(int.MaxValue) },
			new[] { P(40, 4), P(40, 44), P(40, 444), P(40, 4444), P(30, 3), P(20, 2), P(10, 1), P(30, 33), P(30, 333), P(20, 22) },
			// This contains some duplicate keys
			new byte[300].With(array => new Random(randomSeed).NextBytes(array)).Select((x, i) => P(x, i)).ToArray())
			=> _r = new Random(randomSeed);

		CollectionTests<MultiMap<int, int>.ValueList, int> SubcollectionTests()
		{
			var mmap = new MultiMap<int, int>();
			return new CollectionTests<MultiMap<int, int>.ValueList, int>(() => mmap[++_counter],
				new[] { 1, 2, 3, 4 },
				new[] { 0, int.MinValue, int.MaxValue },
				new[] { 90, 80, 70, 60, 50, 40, 30, 20, 10, 0 },
				new byte[200].With(array => _r.NextBytes(array))
					.Select(x => x * 10).ToArray());
		}

		[Test]
		public void Subcollection_TestAddAndContains() => SubcollectionTests().TestAddAndContains();
		
		[Test]
		public void Subcollection_TestAddAndRemoveAndCopyTo() => SubcollectionTests().TestAddAndRemoveAndCopyTo();

		[Test]
		public void MiscTest()
		{
			// Tests for .ContainsKey, .TryGetValue, and the way .KeyCount, .Count is updated
			foreach (var dataSet in _dataSets)
			{
				var mmap = new MultiMap<int, int>();
				var counts = new Dictionary<int, int>();
				int valueCount = 0;

				foreach (var pair in dataSet) {
					counts.TryGetValue(pair.Key, out int count);
					Assert.AreEqual(count > 0, mmap.ContainsKey(pair.Key));
					Assert.AreEqual(count > 0, mmap.TryGetValue(pair.Key, out _));

					mmap[pair.Key].Add(pair.Value);
					counts[pair.Key] = count + 1;

					Assert.AreEqual(counts.Count, mmap.KeyCount);
					Assert.AreEqual(++valueCount, mmap.Count);
				}

				ExpectList(counts.Keys, mmap.Keys);
				ExpectList(dataSet.Select(p => p.Value).OrderBy(x => x), mmap.Values.OrderBy(x => x));

				foreach (var pair in dataSet) {
					counts.TryGetValue(pair.Key, out int count);
					if (_r.Next(2) != 0) {
						valueCount -= count;
						counts.Remove(pair.Key);
						mmap.Remove(pair.Key);
					} else {
						bool removed;
						if (_r.Next(2) != 0)
							removed = mmap[pair.Key].Remove(pair.Value);
						else
							removed = mmap.Remove(new KeyValuePair<int, int>(pair.Key, pair.Value));
						Assert.AreEqual(count > 0, removed);
						if (removed) {
							valueCount--;
							if (--counts[pair.Key] == 0)
								counts.Remove(pair.Key);
						}
					}
					Assert.AreEqual(counts.Count, mmap.KeyCount);
					Assert.AreEqual(valueCount, mmap.Count);
				}
			}
		}
	}
}
