using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Loyc.Collections.Impl;
using Loyc.MiniTest;

namespace Loyc.Collections.Tests
{
	public class AListTestHelpers : AListTestHelpersBase<AList<int>>
	{
		public AListTestHelpers(int maxLeafSize, int maxInnerSize) : base(maxLeafSize, maxInnerSize) { }

		#region Implementations of abstract methods

		public override AList<int> NewList()
		{
			return new AList<int>(MaxLeafSize, MaxInnerSize);
		}
		public override AList<int> CopySection(AList<int> alist, int start, int subcount)
		{
			return alist.CopySection(start, subcount);
		}
		public override AList<int> RemoveSection(AList<int> alist, int start, int subcount)
		{
			return alist.RemoveSection(start, subcount);
		}

		#endregion
	}

	public abstract class AListTests<AList> : AListBaseTests<AList, int> where AList : AListBase<int>, ICloneable<AList>
	{
		public AListTests(AListTestHelpersBase<AList> helpers, bool testExceptions)
			: this(helpers, testExceptions, Environment.TickCount) { }
		public AListTests(AListTestHelpersBase<AList> helpers, bool testExceptions, int randomSeed)
			: base(helpers, testExceptions, randomSeed) { }

		[Test]
		public void TestSetter()
		{
			for (int size = 5; size <= 125; size *= 5)
			{
				List<int> list;
				AList alist = NewList(size, out list);
				Assert.IsFalse(alist.TrySet(-1, -1));
				Assert.IsFalse(alist.TrySet(size, size));
				int i = _r.Next(size);
				Assert.IsTrue(alist.TrySet(0, -999));
				Assert.IsTrue(alist.TrySet(i, i * 2));
				list[0] = -999;
				list[i] = i * 2;
				alist[size / 2] = 0;
				list[size / 2] = 0;
				alist[size - 1] = 999;
				list[size - 1] = 999;
				ExpectList(alist, list, false);
			}
		}

		[Test]
		public void TestRemoveAll()
		{
			List<int> list;
			AList alist = NewList(100, out list);
			list .RemoveAll(i => i % 7 == 3);
			alist.RemoveAll(i => i % 7 == 3);
			ExpectList(alist, list);

			list .RemoveAll(i => i < 10 || i % 2 != 0 || i > 90);
			alist.RemoveAll(i => i < 10 || i % 2 != 0 || i > 90);
			ExpectList(alist, list);

			list.RemoveAll(item => true);
			alist.RemoveAll(item => true);
			ExpectList(alist, list);
		}
	}

	[TestFixture]
	public class AListTests : AListTests<AList<int>>
	{
		public AListTests() : this(true) { }
		public AListTests(bool testExceptions) 
			: this(testExceptions, Environment.TickCount, AListLeaf<int>.DefaultMaxNodeSize, AListInner<int>.DefaultMaxNodeSize) { }
		public AListTests(bool testExceptions, int randomSeed, int maxLeafSize, int maxInnerSize) 
			: base(new AListTestHelpers(maxLeafSize, maxInnerSize), testExceptions, randomSeed) { }

		// Note: we don't need to test Sort. It's tested already by ListRangeTests<AList<int>>

		[Test]
		public void TestSwap()
		{
			List<int> list1, list2;
			AList<int> alist1 = NewList(10, out list1);
			AList<int> alist2 = NewList(100, out list2);
			
			// Can't Swap with a frozen list
			AList<int> frozen = alist1.Clone();
			frozen.Freeze();
			if (_testExceptions)
				AssertThrows<ReadOnlyException>(() => alist1.Swap(frozen));

			// Swap, and ensure that ListChanging and NodeObserver are swapped.
			alist1.ListChanging += (sender, args) => Assert.Fail();
			alist1.AddObserver(new AListTestObserver<int, int>());
			alist1.Swap(alist2);
			Assert.AreEqual(0, alist1.ObserverCount);
			Assert.AreEqual(1, alist2.ObserverCount);

			list2.Add(999);
			alist1.Add(999);
			ExpectList(alist1, list2, false);
			ExpectList(alist2, list1, true);
		}

		[Test]
		public void TestPrependAppend()
		{
			List<int> list = new List<int>();
			var alist = NewList();
			
			List<int>[] lists = new List<int>[8];
			AList<int>[] alists = new AList<int>[]
			{
				NewList(0, out lists[0]),
				NewList(1, out lists[1]),
				NewList(5, out lists[2]),
				NewList(11, out lists[3]),
				NewList(20, out lists[4]),
				NewList(32, out lists[5]),
				NewList(53, out lists[6]),
				NewList(100, out lists[7]),
			};
			Assert.AreEqual(alists.Length, lists.Length);
		
			// So, let's just do a random series of Append and Prepend operations,
			// clearing the list occasionally so that both list sizes vary a lot,
			// which will cause the code paths to vary (important because there
			// are several different ways these operations can be done).
			for (int trial = 0; trial < 20; trial++)
			{
				if (trial % 4 == 0) {
					alist.Clear();
					list.Clear();
				}
				int whirl = _r.Next(alists.Length);
				AList<int> other = alists[whirl];
				bool append = _r.Next(2) == 0;
				if (append) {
					alist.Append(other);
					list.AddRange(lists[whirl]);
				} else {
					alist.Prepend(other);
					list.InsertRange(0, lists[whirl]);
				}
				Assert.That(other.GetImmutableCount() == other.Count || other.Count <= _maxLeafSize);
				Assert.That(alist.GetImmutableCount() >= other.GetImmutableCount() || alist.Count - other.Count <= _maxLeafSize);
			}
		}

		AList<int> NewList(int start, int count, ListChangingHandler<int> observer)
		{
			var list = new AList<int>(Helpers.MaxLeafSize, Helpers.MaxInnerSize);
			for (int i = 0; i < count; i++)
				list.Add(start + i);
			if (observer != null)
				list.ListChanging += observer;
			return list;
		}

		[Test]
		public void TestAppendMove()
		{
			// Append something far larger (taller tree)
			int sizeChange = 0, sizeChangeTemp = 0;
			var list = NewList(0, 80, (l, e) => sizeChange += e.SizeChange);
			var temp = NewList(80, 880, (l, e) => sizeChangeTemp += e.SizeChange);
			list.Append(temp, true);
			Assert.AreEqual(sizeChange, 880);
			Assert.AreEqual(temp.Count, 0);
			ExpectList(list, Range.ExcludeHi(0, 960));
			Assert.AreEqual(list.GetImmutableCount(), 0);

			// Append something far smaller (smaller tree)
			temp = NewList(960, 40, (l, e) => sizeChangeTemp += e.SizeChange);
			list.Append(temp, true);
			Assert.AreEqual(temp.Count, 0);
			ExpectList(list, Range.ExcludeHi(0, 1000));
			Assert.AreEqual(sizeChange, 920);
			Assert.AreEqual(sizeChange, -sizeChangeTemp);
			Assert.AreEqual(list.GetImmutableCount(), 0);
		}

		[Test]
		public void TestPrependMove()
		{
			// Prepend something far larger (taller tree)
			int sizeChange = 0, sizeChangeTemp = 0;
			var list = NewList(920, 80, (l, e) => sizeChange += e.SizeChange);
			var temp = NewList(40, 880, (l, e) => sizeChangeTemp += e.SizeChange);
			list.Prepend(temp, true);
			Assert.AreEqual(sizeChange, 880);
			Assert.AreEqual(temp.Count, 0);
			Assert.AreEqual(list.GetImmutableCount(), 0);

			// Prepend something far smaller (smaller tree)
			temp = NewList(0, 40, (l, e) => sizeChangeTemp += e.SizeChange);
			list.Prepend(temp, true);
			Assert.AreEqual(temp.Count, 0);
			ExpectList(list, Range.ExcludeHi(0, 1000));
			Assert.AreEqual(sizeChange, 920);
			Assert.AreEqual(sizeChange, -sizeChangeTemp);
			Assert.AreEqual(list.GetImmutableCount(), 0);
		}

		[Test]
		public void ExpectThatBulkInputFillsNodesEagerly()
		{
			var list = InitialBulkAddTest(8, 1);
			var root = ((AListInner<int>)list._root);
			Assert.AreEqual(2, root.LocalCount);
			Assert.AreEqual(8, root._children[1].Index);
			Assert.AreEqual(1, root._children[1].Node.LocalCount);

			var list2 = new AList<int>(8, 8) { 9, 99, 10, 11, 12, 13, 14, 99 };
			list2.Insert(1, 99); // split root leaf node
			Assert.AreEqual(9, list2.Count);
			root = (AListInner<int>)list2._root;
			Assert.IsFalse(root._children[1].Node.IsUndersized);
			list2.RemoveAt(1);
			list2.RemoveAt(1);
			list2.RemoveAt(list2.Count - 1);
			Assert.AreEqual(6, list2.Count);
			Assert.AreEqual(2, list2._root.LocalCount);
			list.Append(list2, true);
			root = (AListInner<int>)list._root;
			Assert.AreEqual(0, list2.Count);
			Assert.AreEqual(4, list._root.LocalCount);
			Assert.AreEqual(1, root._children[1].Node.LocalCount); // undersize
			Assert.AreEqual(8, root._children[1].Index);

			ExpectList(list, Enumerable.Range(0, 15));
			list.RemoveAt(8);
			ExpectList(list, 0, 1, 2, 3, 4, 5, 6, 7, 9, 10, 11, 12, 13, 14);

			// Try a similar test, this time cloning list2 and using the standard size
			list = InitialBulkAddTest(_maxLeafSize, 1);
			list2 = InitialBulkAddTest(_maxLeafSize, 1, _maxLeafSize+1);
			var scglist = Enumerable.Range(0, (_maxLeafSize + 1) * 2).ToList();
			list.Append(list2);
			ExpectList(list, scglist);

			// The second half of the list will be frozen
			if (_maxLeafSize >= 4)
			{
				root = (AListInner<int>)list._root;
				Assert.IsTrue(root._children[1].Node.IsUndersized);
				Assert.AreEqual(_maxLeafSize, root._children[1].Index);
				Assert.IsFalse(root._children[1].Node.IsFrozen);
				Assert.IsTrue(root._children[2].Node.IsFrozen);
				Assert.IsTrue(root._children[3].Node.IsUndersized);
			}

			RemoveFromBoth(list, scglist, _maxLeafSize);
			ExpectList(list, scglist);

			// Finally let's just make something 3 levels high... 
			// currently it'll be split down the middle (balanced) 
			// except that the final leaf is undersize
			list = InitialBulkAddTest(_maxLeafSize, _maxLeafSize);
			ExpectList(list, Enumerable.Range(0, _maxLeafSize * _maxLeafSize + 1));
			Assert.AreEqual(3, list.TreeHeight);
			root = (AListInner<int>)list._root;
			Assert.AreEqual(_maxLeafSize * ((_maxLeafSize + 1) >> 1), root._children[1].Index);
			Assert.AreEqual(1, ((AListInner<int>)root._children[1].Node)._children[_maxLeafSize/2].Node.LocalCount);
		}

		AList<int> InitialBulkAddTest(int nodeSize, int leavesToFill, int firstChild = 0)
		{
			// Example: nodeSize=6, leavesToFill=2 => add 13 items 
			// and expect two 6-item nodes and a 1-item node.
			var list = new AList<int>(nodeSize, nodeSize);
			list.AddRange(Enumerable.Range(firstChild, nodeSize * leavesToFill + 1));
			var inner = (AListInner<int>)list._root;
			int iLast = inner.LocalCount - 1;
			if (leavesToFill < nodeSize)
			{
				Assert.AreEqual(leavesToFill + 1, inner.LocalCount);
				Assert.AreEqual(nodeSize * leavesToFill, inner._children[iLast].Index);
			}

			AssertLastChildContainsOneItem();
			void AssertLastChildContainsOneItem()
			{
				var lastChild = inner.Children.Last();
				while (!lastChild.IsLeaf)
					lastChild = lastChild.Children.Last();
				Assert.AreEqual(1, lastChild.LocalCount);
			}

			// Removing the last element may reduce the tree to one level;
			// adding it back restores the situation to how it was before
			list.RemoveAt(list.Count - 1);
			if (leavesToFill == 1)
				Assert.That(list._root.IsLeaf);
			list.Add(firstChild + list.Count);
			Assert.That(!list._root.IsLeaf);
			inner = (AListInner<int>)list._root;
			AssertLastChildContainsOneItem();

			return list;
		}

		[Test]
		public void LeafCapacityLimitIsRespected()
		{
			var list = new AList<int>(_maxLeafSize, _maxInnerSize);

			for (int i = 0; i < 100 + _maxLeafSize * _maxInnerSize; i++)
				list.Insert(_r.Next(list.Count + 1), i);

			VerifyLeafCapacityLimitIsRespected(list, _maxLeafSize);
		}
	}
}
