using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Loyc.Collections.Impl;
using Loyc.MiniTest;

namespace Loyc.Collections.Tests
{
	public abstract class AListTests<AList> : AListTestBase<AList, int> where AList : AListBase<int>, ICloneable<AList>
	{
		protected int _maxInnerSize, _maxLeafSize;

		public AListTests(bool testExceptions) : this(testExceptions, Environment.TickCount, AListLeaf<int>.DefaultMaxNodeSize, AListInner<int>.DefaultMaxNodeSize) { }
		public AListTests(bool testExceptions, int randomSeed, int maxLeafSize, int maxInnerSize)
			: base(testExceptions, randomSeed)
		{
			_maxInnerSize = maxInnerSize;
			_maxLeafSize = maxLeafSize;
		}

		#region Implementations of abstract methods

		protected override int AddToBoth(AList alist, List<int> list, int item, int preferredIndex)
		{
			alist.Insert(preferredIndex, item);
			list.Insert(preferredIndex, item);
			return preferredIndex;
		}
		protected override int Add(AList alist, int item, int preferredIndex)
		{
			alist.Insert(preferredIndex, item);
			return preferredIndex;
		}
		protected override bool RemoveFromBoth(AList alist, List<int> list, int item)
		{
			int i = alist.IndexOf(item);
			if (i == -1) {
				Assert.IsFalse(alist.Remove(item));
				return false;
			}
			Assert.IsTrue(alist.Remove(item));
			list.RemoveAt(i);
			return true;
		}
		protected override int GetKey(int item)
		{
			return item;
		}

		#endregion

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
		public AListTests(bool testExceptions) : base(testExceptions) { }
		public AListTests(bool testExceptions, int randomSeed, int maxLeafSize, int maxInnerSize) : base(testExceptions, randomSeed, maxLeafSize, maxInnerSize) { }

		#region Implementations of abstract methods

		protected override AList<int> NewList()
		{
			return new AList<int>(_maxLeafSize, _maxInnerSize);
		}
		protected override AList<int> CopySection(AList<int> alist, int start, int subcount)
		{
			return alist.CopySection(start, subcount);
		}
		protected override AList<int> RemoveSection(AList<int> alist, int start, int subcount)
		{
			return alist.RemoveSection(start, subcount);
		}

		#endregion

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
			var list = new AList<int>(_maxLeafSize, _maxInnerSize);
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
			ExpectList(list, Range.IntRange(0, 960));
			Assert.AreEqual(list.GetImmutableCount(), 0);

			// Append something far smaller (smaller tree)
			temp = NewList(960, 40, (l, e) => sizeChangeTemp += e.SizeChange);
			list.Append(temp, true);
			Assert.AreEqual(temp.Count, 0);
			ExpectList(list, Range.IntRange(0, 1000));
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
			ExpectList(list, Range.IntRange(0, 1000));
			Assert.AreEqual(sizeChange, 920);
			Assert.AreEqual(sizeChange, -sizeChangeTemp);
			Assert.AreEqual(list.GetImmutableCount(), 0);
		}
	}
}
