using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;
using Loyc.Collections.Impl;

namespace Loyc.Collections.Tests
{
	using IntPair = KeyValuePair<int, int>;

	[TestFixture]
	public class BDictionaryTests : AListTestBase<BDictionary<int,int>, IntPair>
	{
		int _maxInnerSize, _maxLeafSize;

		public BDictionaryTests() : this(true) { }
		public BDictionaryTests(bool testExceptions) 
			: this(testExceptions, Environment.TickCount, AListLeaf<int,int>.DefaultMaxNodeSize, AListInnerBase<int,int>.DefaultMaxNodeSize) { }
		public BDictionaryTests(bool testExceptions, int randomSeed, int maxLeafSize, int maxInnerSize)
			: base(testExceptions, randomSeed)
		{
			_maxInnerSize = maxInnerSize;
			_maxLeafSize = maxLeafSize;
		}

		#region Implementations of abstract methods

		protected override BDictionary<int,int> NewList()
		{
			return new BDictionary<int,int>(_maxLeafSize, _maxInnerSize);
		}
		
		int _nextValue = 0;

		protected override int AddToBoth(BDictionary<int, int> blist, List<IntPair> list, int key, int preferredIndex)
		{
			int i = blist.FindLowerBound(key);
			int value = _nextValue++;
			blist.Add(key, value);
			list.Insert(i, new KeyValuePair<int,int>(key, value));
			return i;
		}
		protected override int Add(BDictionary<int, int> blist, int key, int preferredIndex)
		{
			int i = blist.FindLowerBound(key);
			int value = _nextValue++;
			blist.Add(key, value);
			return i;
		}
		protected override BDictionary<int, int> CopySection(BDictionary<int, int> blist, int start, int subcount)
		{
			return blist.CopySection(start, subcount);
		}
		protected override BDictionary<int,int> RemoveSection(BDictionary<int,int> blist, int start, int subcount)
		{
			return blist.RemoveSection(start, subcount);
		}
		protected override bool RemoveFromBoth(BDictionary<int, int> blist, List<IntPair> list, int item)
		{
			int i = blist.IndexOf(item);
			if (i == -1)
				return false;
			blist.Remove(item);
			list.RemoveAt(i);
			return true;
		}
		protected override int GetKey(IntPair item)
		{
			return item.Key;
		}

		#endregion

		IntPair Pair(int key, int value) { return new IntPair(key, value); }

		[Test]
		public void TestMethodsOfICollectionOfIntPair()
		{
			var dict = NewList();
			for (int i = 0; i <= 10; i += 2)
				dict.Add(new IntPair(i, i * 2));
			Assert.That(dict.Remove(new IntPair(2, 4)));
			var items = new List<IntPair>();
			items.AddRange(new IntPair[] { Pair(0, 0), Pair(4, 8), Pair(6, 12), Pair(8, 16), Pair(10, 20) });

			for (int pass = 0; pass <= 1; pass++)
			{
				ExpectList(dict, items, pass == 1);
				foreach (var item in items)
				{
					Assert.IsFalse(dict.Contains(Pair(item.Key, item.Value - 1)));
					Assert.IsFalse(dict.Remove(Pair(item.Key, item.Value + 1)));
					Assert.AreEqual(-1, dict.IndexOf(Pair(item.Key, item.Value + 1)));
				}
				foreach (var item in items)
				{
					Assert.That(dict.Contains(item));
					// The dictionary can be indexed by index in the base class
					// and by key in the derived class. Since the keys are ints,
					// we have to cast IListSource or something to call this[index]
					Assert.AreEqual(item, ((IListSource<IntPair>)dict)[dict.IndexOf(item)]);
					Assert.That(dict.Remove(item));
				}
				Assert.AreEqual(0, dict.Count);
				
				items.Clear();
				for (int i = 0; i < 100; i++) {
					dict.Add(Pair(i, i * 2));
					items.Add(Pair(i, i * 2));
				}
			}
			ExpectList(dict, items, false);
		}

		[Test]
		public void TestMethodsOfIDictionary()
		{
			var dict = NewList();
			var list = new List<IntPair>();
			int permutation = _r.Next(256);
			
			for (int i = 0; i < 256; i++)
			{
				int key = i ^ permutation;
				// Test Add(key,i) and ContainsKey(key)
				Assert.IsFalse(dict.ContainsKey(key));
				int index = dict.FindLowerBound(key);
				dict.Add(key, i);
				list.Insert(index, Pair(key, i));
				Assert.That(dict.ContainsKey(key));
			}

			ExpectList(dict, list, false);
			// A very basic test of the properties Keys and Values
			ExpectList(dict.Keys, Enumerable.Range(0, 256));
			ExpectList(dict.Values, list.Select(pair => pair.Value));

			// Test getters
			for (int i = 0; i < 300; i++)
			{
				int value;
				Assert.AreEqual(i < 256, dict.TryGetValue(i, out value));
				if (i < 256) {
					Assert.AreEqual(i ^ permutation, value);
					Assert.AreEqual(i ^ permutation, dict[i]);
					Assert.AreEqual(i ^ permutation, dict[i, -999]);
				} else
					Assert.AreEqual(-999, dict[i, -999]);
			}
			
			// Test setter
			dict[999] = 999;
			Assert.That(dict.ContainsKey(999));
			dict[999] = -999;
			Assert.AreEqual(-999, dict[999]);

			// Test Remove (by key)
			for (int i = 0; i < 256; i++)
			{
				int key = i ^ permutation;
				Assert.IsTrue(dict.Remove(key));
				Assert.IsFalse(dict.Remove(key));
			}

			// Test Clear()
			ExpectList(dict, Pair(999, -999));
			dict.Clear();
			ExpectList(dict);
		}

		// TODO: add tests for FindLowerBound, FindUpperBound, IndexOf(key), AddRange, 
		//       RemoveRange, AddIfNotPresent, ReplaceIfPresent, ReplaceAndGetOldValue
	}
}
