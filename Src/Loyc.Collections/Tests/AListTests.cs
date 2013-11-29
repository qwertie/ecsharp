using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Loyc.Collections.Impl
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
				Assert.That(other.ImmutableCount == other.Count || other.Count <= _maxLeafSize);
				Assert.That(alist.ImmutableCount >= other.ImmutableCount || alist.Count-other.Count <= _maxLeafSize);
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
			Assert.AreEqual(list.ImmutableCount, 0);

			// Append something far smaller (smaller tree)
			temp = NewList(960, 40, (l, e) => sizeChangeTemp += e.SizeChange);
			list.Append(temp, true);
			Assert.AreEqual(temp.Count, 0);
			ExpectList(list, Range.IntRange(0, 1000));
			Assert.AreEqual(sizeChange, 920);
			Assert.AreEqual(sizeChange, -sizeChangeTemp);
			Assert.AreEqual(list.ImmutableCount, 0);
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
			Assert.AreEqual(list.ImmutableCount, 0);

			// Prepend something far smaller (smaller tree)
			temp = NewList(0, 40, (l, e) => sizeChangeTemp += e.SizeChange);
			list.Prepend(temp, true);
			Assert.AreEqual(temp.Count, 0);
			ExpectList(list, Range.IntRange(0, 1000));
			Assert.AreEqual(sizeChange, 920);
			Assert.AreEqual(sizeChange, -sizeChangeTemp);
			Assert.AreEqual(list.ImmutableCount, 0);
		}
	}

	[TestFixture]
	public class SparseAListTests : AListTests<SparseAList<int>>
	{
		public SparseAListTests() : this(true) { }
		public SparseAListTests(bool testExceptions) : base(testExceptions) { }
		public SparseAListTests(bool testExceptions, int randomSeed, int maxLeafSize, int maxInnerSize) : base(testExceptions, randomSeed, maxLeafSize, maxInnerSize) { }

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
			SparseAList<int> alist = NewList();
			list = new List<int>();

			// Make a list of random size <= initialCount
			for (int i = 0; i < _r.Next(initialCount + 1); i++)
				AddToBoth(alist, list, i, i);

			// Add empty spaces until Count == initialCount
			while (alist.Count < initialCount) {
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
			
			List<int>[] lists = new List<int>[8];
			SparseAList<int>[] alists = new SparseAList<int>[]
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
				SparseAList<int> other = alists[whirl];
				bool append = _r.Next(2) == 0;
				if (append) {
					alist.Append(other);
					list.AddRange(lists[whirl]);
				} else {
					alist.Prepend(other);
					list.InsertRange(0, lists[whirl]);
				}
				Assert.That(other.ImmutableCount == other.Count || other.Count <= _maxLeafSize);
				Assert.That(alist.ImmutableCount >= other.ImmutableCount || alist.Count-other.Count <= _maxLeafSize);
			}
		}

		SparseAList<int> NewList(int start, int count, ListChangingHandler<int> observer, out List<int> list)
		{
			var alist = new SparseAList<int>(_maxLeafSize, _maxInnerSize);
			list = new List<int>();
			for (int i = 0; i < count; i++) {
				if (_r.Next(2) == 0) {
					alist.Add(start + i);
					list.Add(start + i);
				} else {
					alist.InsertSpace(start + i);
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
			Assert.AreEqual(alist.ImmutableCount, 0);

			// Append something far smaller (smaller tree)
			temp = NewList(1000, 50, (l, e) => sizeChangeTemp += e.SizeChange, out tempList);
			alist.Append(temp, true);
			list.InsertRange(list.Count, tempList);
			Assert.AreEqual(temp.Count, 0);
			ExpectList(alist, list);
			Assert.AreEqual(sizeChange, 1050);
			Assert.AreEqual(sizeChange, -sizeChangeTemp);
			Assert.AreEqual(alist.ImmutableCount, 0);
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
			Assert.AreEqual(alist.ImmutableCount, 0);

			// Prepend something far smaller (smaller tree)
			temp = NewList(-50, 50, (l, e) => sizeChangeTemp += e.SizeChange, out tempList);
			alist.Prepend(temp, true);
			list.InsertRange(0, tempList);
			Assert.AreEqual(temp.Count, 0);
			ExpectList(alist, list);
			Assert.AreEqual(sizeChange, 950);
			Assert.AreEqual(sizeChange, -sizeChangeTemp);
			Assert.AreEqual(alist.ImmutableCount, 0);
		}
	}
}
