using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Loyc.Collections.Impl;

namespace Loyc.Collections
{
	[TestFixture]
	public class BListTests : AListTestBase<BList<int>, int>
	{
		int _maxInnerSize, _maxLeafSize;

		public BListTests() : this(Environment.TickCount, BListLeaf<int,int>.DefaultMaxNodeSize, BListInner<int,int>.DefaultMaxNodeSize) { }
		public BListTests(int randomSeed, int maxLeafSize, int maxInnerSize) : base(randomSeed)
		{
			_maxInnerSize = maxInnerSize;
			_maxLeafSize = maxLeafSize;
		}

		#region Implementations of abstract methods

		protected override BList<int> NewList()
		{
			return new BList<int>(_maxLeafSize, _maxInnerSize);
		}
		protected override int AddToBoth(BList<int> blist, List<int> list, int item, int preferredIndex)
		{
			int i = blist.FindLowerBound(item);
			blist.Add(item);
			list.Insert(i, item);
			return i;
		}
		protected override BList<int> CopySection(BList<int> blist, int start, int subcount)
		{
			return blist.CopySection(start, subcount);
		}
		protected override BList<int> RemoveSection(BList<int> blist, int start, int subcount)
		{
			return blist.RemoveSection(start, subcount);
		}
		protected override bool RemoveFromBoth(BList<int> blist, List<int> list, int item)
		{
			int i = blist.IndexOf(item);
			if (i == -1)
				return false;
			blist.Remove(item);
			list.RemoveAt(i);
			return true;
		}
		protected override int GetKey(int item)
		{
			return item;
		}

		#endregion

		[Test]
		public void TestStandardOperations()
		{
			List<int> list = new List<int>();
			BList<int> blist = NewList();

			// Ensure standard operations work for various list sizes
			int next = 0, item;
			for (int size = 5; size <= 125; size *= 5)
			{
				while (blist.Count < size)
					AddToBoth(blist, list, next += 2, -1);

				ExpectList(blist, list, _r.Next(2) == 0);

				// Add one in the middle
				int at = blist.FindLowerBound(size);
				blist.Do(AListOperation.Add, size);
				list.Insert(at, size);

				// Remove in different ways
				item = _r.Next(size * 2);
				Assert.AreEqual(list.Remove(item), blist.Remove(item));
				item = _r.Next(size * 2);
				Assert.AreEqual(list.Remove(item), blist.Do(AListOperation.Remove, item) == -1);

				// IndexOf/Contains
				item = _r.Next(size * 2);
				Assert.AreEqual(list.IndexOf(item), blist.IndexOf(item));
				item = _r.Next(size * 2);
				Assert.AreEqual(list.Contains(item), blist.Contains(item));

				ExpectList(blist, list, _r.Next(2) == 0);

				AssertThrows<KeyAlreadyExistsException>(() => blist.Do(AListOperation.AddOrThrow, blist.First));

				// Replace an item with itself (with ints, this command can never 
				// do anything, but we'll try it in for completeness). Also,
				// try no-op AddOrReplace and AddIfNotPresent operations to verify
				// that they do nothing.
				Assert.AreEqual(0, blist.Do(AListOperation.ReplaceIfPresent, _r.Next(size)));
				Assert.AreEqual(0, blist.Do(AListOperation.AddOrReplace, blist.Last));
				Assert.AreEqual(0, blist.Do(AListOperation.AddIfNotPresent, blist.First));
				
				// Also do an AddOrReplace and AddIfNotPresent operation with an 
				// item that does not already exist.
				blist.Do(AListOperation.AddOrReplace, next + 3); // end of the list
				blist.Do(AListOperation.AddIfNotPresent, next + 1);
				blist.Do(AListOperation.Add, next + 1);
				list.Add(next + 1);
				list.Add(next + 1);
				list.Add(next + 3);
				
				ExpectList(blist, list, _r.Next(2) == 0);

				Assert.AreEqual(2, blist.RemoveAll(next + 1));
			}
		}

		[Test]
		public void TestRangeOperations()
		{
			BList<int> blist = NewList();
			var primes = new int[] { 2, 3, 5, 7, 11, 13, 17, 23 };
			blist.AddRange(new int[] { });
			blist.AddRange(primes);
			blist.AddRange(new int[] { });
			ExpectList(blist, primes);

			Assert.AreEqual(4, blist.AddRange(new int[] { 9, 9, 29, 9 }));
			ExpectList(blist, 2, 3, 5, 7, 9, 9, 9, 11, 13, 17, 23, 29);

			Assert.AreEqual(2, blist.RemoveRange(new int[] { 9, 9 }));
			ExpectList(blist, 2, 3, 5, 7, 9, 11, 13, 17, 23, 29);

			Assert.AreEqual(2, blist.RemoveRange(new int[] { 9, 9, 29, 9 }));
			ExpectList(blist, 2, 3, 5, 7, 11, 13, 17, 23);
		}

		[Test]
		public void TestUpperAndLowerBound()
		{
			BList<int> blist = NewList();
			blist.AddRange(new int[] { 0, 0, 10, 10, 20, 20, 25, 30, 30, 40, 40, 50, 50 });

			int item = 25;
			Assert.AreEqual(blist.FindUpperBound(item), 1 + blist.FindLowerBound(item));
			
			item = 10;
			Assert.That(blist.FindUpperBound(ref item) == 4 && item == 20);
			item = 0;
			Assert.That(blist.FindUpperBound(ref item) == 2 && item == 10);
			item = 999;
			Assert.That(blist.FindUpperBound(ref item) == blist.Count && item == 999);
			
			bool found;
			item = 5;
			Assert.That(blist.FindLowerBound(ref item, out found) == 2 && item == 10 && !found);
			item = 20;
			Assert.That(blist.FindLowerBound(ref item, out found) == 4 && item == 20 && found);
			item = 0;
			Assert.That(blist.FindLowerBound(ref item, out found) == 0 && item == 0 && found);
			item = 999;
			Assert.That(blist.FindLowerBound(ref item, out found) == blist.Count && item == 999 && !found);
		}
	}
}
