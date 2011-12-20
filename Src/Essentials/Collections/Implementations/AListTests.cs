using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Loyc.Essentials;
using Loyc.Collections.Impl;

namespace Loyc.Collections
{
	[TestFixture]
	public class AListTests : AListTestBase<AList<int>, int>
	{
		int _maxInnerSize, _maxLeafSize;

		public AListTests() : this(true) { }
		public AListTests(bool testExceptions) : this(testExceptions, Environment.TickCount, AListLeaf<int>.DefaultMaxNodeSize, AListInner<int>.DefaultMaxNodeSize) { }
		public AListTests(bool testExceptions, int randomSeed, int maxLeafSize, int maxInnerSize)
			: base(testExceptions, randomSeed)
		{
			_maxInnerSize = maxInnerSize;
			_maxLeafSize = maxLeafSize;
		}

		#region Implementations of abstract methods

		protected override AList<int> NewList()
		{
			return new AList<int>(_maxLeafSize, _maxInnerSize);
		}
		protected override int AddToBoth(AList<int> alist, List<int> list, int item, int preferredIndex)
		{
			alist.Insert(preferredIndex, item);
			list.Insert(preferredIndex, item);
			return preferredIndex;
		}
		protected override int Add(AList<int> alist, int item, int preferredIndex)
		{
			alist.Insert(preferredIndex, item);
			return preferredIndex;
		}
		protected override AList<int> CopySection(AList<int> alist, int start, int subcount)
		{
			return alist.CopySection(start, subcount);
		}
		protected override AList<int> RemoveSection(AList<int> alist, int start, int subcount)
		{
			return alist.RemoveSection(start, subcount);
		}
		protected override bool RemoveFromBoth(AList<int> alist, List<int> list, int item)
		{
			int i = alist.IndexOf(item);
			if (i == -1)
				return false;
			alist.Remove(item);
			list.RemoveAt(i);
			return true;
		}
		protected override int GetKey(int item)
		{
			return item;
		}

		#endregion

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
			//
			// TODO: Test Prepend/Append with move semantics
			//

			List<int> list = new List<int>();
			AList<int> alist = NewList();
			
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
				if (_r.Next(2) == 0) {
					alist.Append(alists[whirl]);
					list.AddRange(lists[whirl]);
				} else {
					alist.Prepend(alists[whirl]);
					list.InsertRange(0, lists[whirl]);
				}
			}
		}

		[Test]
		public void TestSetter()
		{
			for (int size = 5; size <= 125; size *= 5)
			{
				List<int> list;
				AList<int> alist = NewList(size, out list);
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
		public void TestObservedInserts()
		{

		}

		// Note: we don't need to test Sort. It's tested already by ListRangeTests<AList<int>>
	}
}
