using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Loyc.MiniTest;

namespace Loyc.Collections.Tests
{
	[TestFixture]
	public class SparseAListTests : AListTests<SparseAList<int>>
	{
		public SparseAListTests() : this(true) { }
		public SparseAListTests(bool testExceptions) : base(testExceptions) { }
		public SparseAListTests(bool testExceptions, int randomSeed, int maxLeafSize, int maxInnerSize) : base(testExceptions, randomSeed, maxLeafSize, maxInnerSize) { }

		[Test(Fails = "Tree observers are not implemented (and are broken) in SparseALists")]
		public override void SimpleObserverTests1() 
			{ if (_testExceptions) Fail("Support for tree observers is not implemented."); }
		public override void SimpleObserverTests2() {}
		public override void ObserveRemoveSection() {}

		#region Implementations of abstract methods

		protected override SparseAList<int> NewList()
		{
			return new SparseAList<int>(_maxLeafSize, _maxInnerSize);
		}
		protected override SparseAList<int> CopySection(SparseAList<int> alist, int start, int subcount)
		{
			return alist.CopySection(start, subcount);
		}
		protected override SparseAList<int> RemoveSection(SparseAList<int> alist, int start, int subcount)
		{
			return alist.RemoveSection(start, subcount);
		}

		#endregion

		protected override SparseAList<int> NewList(int initialCount, out List<int> list)
		{
			return NewList(initialCount, _r.Next(initialCount + 1), out list);
		}
		protected SparseAList<int> NewList(int initialCount, int realCount, out List<int> list)
		{
			Debug.Assert(realCount <= initialCount);
			SparseAList<int> alist = NewList();
			list = new List<int>();

			// Make a list of random size <= initialCount
			for (int i = 0; i < realCount; i++)
				AddToBoth(alist, list, i, i);

			// Add empty spaces until Count == initialCount
			while (alist.Count < initialCount)
			{
				int i = _r.Next(alist.Count + 1);
				alist.InsertSpace(i);
				list.Insert(i, 0);
			}

			return alist;
		}

		[Test]
		public void TestSwap()
		{
			List<int> list1, list2;
			SparseAList<int> alist1 = NewList(10, out list1);
			SparseAList<int> alist2 = NewList(100, out list2);

			// Can't Swap with a frozen list
			SparseAList<int> frozen = alist1.Clone();
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

			List<int>[] lists = new List<int>[13];
			SparseAList<int>[] alists = new SparseAList<int>[]
			{
				NewList(0, 0, out lists[0]),
				NewList(2, 1, out lists[1]),
				NewList(6, 5, out lists[2]),
				NewList(10, 5, out lists[3]),
				NewList(15, 11, out lists[4]),
				NewList(30, 11, out lists[5]),
				NewList(30, 20, out lists[6]),
				NewList(60, 20, out lists[7]),
				NewList(50, 32, out lists[8]),
				NewList(100, 32, out lists[9]),
				NewList(80, 53, out lists[10]),
				NewList(150, 53, out lists[11]),
				NewList(150, 100, out lists[12]),
			};
			Assert.AreEqual(alists.Length, lists.Length);

			// So, let's just do a random series of Append and Prepend operations,
			// clearing the list occasionally so that both list sizes vary a lot,
			// which will cause the code paths to vary (important because there
			// are several different ways these operations can be done).
			for (int trial = 0; trial < 20; trial++)
			{
				if (trial % 4 == 0)
				{
					alist.Clear();
					list.Clear();
				}
				int whirl = _r.Next(alists.Length);
				SparseAList<int> other = alists[whirl];
				bool append = _r.Next(2) == 0;

				int ric = alist.GetRealItemCount(), otherRic = other.GetRealItemCount(), oldTH = alist.TreeHeight;
				if (append)
				{
					alist.Append(other);
					list.AddRange(lists[whirl]);
				}
				else
				{
					alist.Prepend(other);
					list.InsertRange(0, lists[whirl]);
				}
				Assert.That(other.GetImmutableCount() == other.Count || other.TreeHeight <= 1);
				Assert.That(alist.GetRealItemCount() == ric + otherRic);
				Assert.That(alist.GetImmutableCount() >= other.GetImmutableCount() || oldTH == 1);
			}
		}

		SparseAList<int> NewList(int start, int count, ListChangingHandler<int> observer, out List<int> list)
		{
			var alist = new SparseAList<int>(_maxLeafSize, _maxInnerSize);
			list = new List<int>();
			for (int i = 0; i < count; i++)
			{
				if (_r.Next(2) == 0)
				{
					alist.Add(start + i);
					list.Add(start + i);
				}
				else
				{
					alist.InsertSpace(i);
					list.Add(0);
				}
			}
			if (observer != null)
				alist.ListChanging += observer;
			return alist;
		}

		[Test]
		public void TestAppendMove()
		{
			// Append something far larger (taller tree)
			int sizeChange = 0, sizeChangeTemp = 0;
			List<int> list, tempList;
			var alist = NewList(0, 100, (l, e) => sizeChange += e.SizeChange, out list);
			var temp = NewList(100, 1000, (l, e) => sizeChangeTemp += e.SizeChange, out tempList);
			alist.Append(temp, true);
			list.InsertRange(list.Count, tempList);
			Assert.AreEqual(sizeChange, 1000);
			Assert.AreEqual(temp.Count, 0);
			ExpectList(alist, list);
			Assert.AreEqual(alist.GetImmutableCount(), 0);

			// Append something far smaller (smaller tree)
			temp = NewList(1000, 50, (l, e) => sizeChangeTemp += e.SizeChange, out tempList);
			alist.Append(temp, true);
			list.InsertRange(list.Count, tempList);
			Assert.AreEqual(temp.Count, 0);
			ExpectList(alist, list);
			Assert.AreEqual(sizeChange, 1050);
			Assert.AreEqual(sizeChange, -sizeChangeTemp);
			Assert.AreEqual(alist.GetImmutableCount(), 0);
		}

		[Test]
		public void TestPrependMove()
		{
			// Prepend something far larger (taller tree)
			int sizeChange = 0, sizeChangeTemp = 0;
			List<int> list, tempList;
			var alist = NewList(900, 100, (l, e) => sizeChange += e.SizeChange, out list);
			var temp = NewList(0, 900, (l, e) => sizeChangeTemp += e.SizeChange, out tempList);
			alist.Prepend(temp, true);
			list.InsertRange(0, tempList);
			Assert.AreEqual(sizeChange, 900);
			Assert.AreEqual(temp.Count, 0);
			ExpectList(alist, list);
			Assert.AreEqual(alist.GetImmutableCount(), 0);

			// Prepend something far smaller (smaller tree)
			temp = NewList(-50, 50, (l, e) => sizeChangeTemp += e.SizeChange, out tempList);
			alist.Prepend(temp, true);
			list.InsertRange(0, tempList);
			Assert.AreEqual(temp.Count, 0);
			ExpectList(alist, list);
			Assert.AreEqual(sizeChange, 950);
			Assert.AreEqual(sizeChange, -sizeChangeTemp);
			Assert.AreEqual(alist.GetImmutableCount(), 0);
		}
	}
}
