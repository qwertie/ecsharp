using System;
using System.Collections.Generic;
using System.Linq;
using Loyc.Collections.Impl;
using Loyc.MiniTest;
using System.Threading;

namespace Loyc.Collections
{
	[TestFixture]
	public class FVListTests : TestHelpers
	{
		[Test]
		public void SimpleTests()
		{
            // In this simple test, I only add and remove items from the front
            // of a FVList, but forking is also tested.

			FVList<int> list = new FVList<int>();
			Assert.That(list.IsEmpty);
			
			// Adding to VListBlockOfTwo
			list = new FVList<int>(10, 20);
			ExpectList(list, 10, 20);

			list = new FVList<int>();
			list.Add(1);
			Assert.That(!list.IsEmpty);
			list.Add(2);
			ExpectList(list, 2, 1);

			// A fork in VListBlockOfTwo. Note that list2 will use two VListBlocks
			// here but list will only use one.
			FVList<int> list2 = list.WithoutFirst(1);
			list2.Add(3);
			ExpectList(list, 2, 1);
			ExpectList(list2, 3, 1);

			// Try doubling list2
			list2.AddRange(list2);
			ExpectList(list2, 3, 1, 3, 1);

			// list now uses two arrays
			list.Add(4);
			ExpectList(list, 4, 2, 1);

			// Try doubling list using a different overload of AddRange()
			list.AddRange((IList<int>)list);
			ExpectList(list, 4, 2, 1, 4, 2, 1);
			list = list.WithoutFirst(3);
			ExpectList(list, 4, 2, 1);

			// Remove(), Pop()
			Assert.That(list2.Remove(3));
			ExpectList(list2, 1, 3, 1);
			Assert.That(!list2.Remove(0));
			Assert.AreEqual(1, list2.Pop());
			Assert.That(list2.Remove(3));
			ExpectList(list2, 1);
			Assert.AreEqual(1, list2.Pop());
			ExpectList(list2);
			AssertThrows<Exception>(delegate() { list2.Pop(); });

			// Add many, SubList(). This will fill 3 arrays (sizes 8, 4, 2) and use
			// 1 element of a size-16 array. Oh, and test the enumerator.
			for (int i = 5; i <= 16; i++)
				list.Add(i);
			ExpectList(list, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 2, 1);
			list2 = list.WithoutFirst(6);
			ExpectListByEnumerator(list2, 10, 9, 8, 7, 6, 5, 4, 2, 1);
			AssertThrows<IndexOutOfRangeException>(delegate() { int i = list[-1]; });
			AssertThrows<IndexOutOfRangeException>(delegate() { int i = list[15]; });
			
			// IndexOf, contains
			Assert.That(list.Contains(11));
			Assert.That(!list2.Contains(11));
			Assert.That(list[list.IndexOf(2)] == 2);
			Assert.That(list[list.IndexOf(1)] == 1);
			Assert.That(list[list.IndexOf(15)] == 15);
			Assert.That(list.IndexOf(3) == -1);

			// PreviousIn(), this[], Front
			FVList<int> list3 = list2;
			Assert.AreEqual(11, (list3 = list3.PreviousIn(list))[0]);
			Assert.AreEqual(12, (list3 = list3.PreviousIn(list))[0]);
			Assert.AreEqual(13, (list3 = list3.PreviousIn(list))[0]);
			Assert.AreEqual(14, (list3 = list3.PreviousIn(list)).First);
			Assert.AreEqual(15, (list3 = list3.PreviousIn(list)).First);
			Assert.AreEqual(16, (list3 = list3.PreviousIn(list)).First);
			AssertThrows<Exception>(delegate() { list3.PreviousIn(list); });

			// Tail
			Assert.AreEqual(10, (list3 = list3.WithoutFirst(6))[0]);
			Assert.AreEqual(9, (list3 = list3.Tail)[0]);
			Assert.AreEqual(8, (list3 = list3.Tail)[0]);
			Assert.AreEqual(7, (list3 = list3.Tail).First);
			Assert.AreEqual(6, (list3 = list3.Tail).First);
			Assert.AreEqual(5, (list3 = list3.Tail).First);
			Assert.AreEqual(4, (list3 = list3.Tail)[0]);
			Assert.AreEqual(2, (list3 = list3.Tail)[0]);
			Assert.AreEqual(1, (list3 = list3.Tail)[0]);
			Assert.That((list3 = list3.Tail).IsEmpty);

			// list2 is still the same
			ExpectList(list2, 10, 9, 8, 7, 6, 5, 4, 2, 1);

			// ==, !=, Equals(), AddRange(a, b)
			Assert.That(!list2.Equals("hello"));
			list3 = list2;
			Assert.That(list3.Equals(list2));
			Assert.That(list3 == list2);
            // This AddRange forks the list. List2 end up with block sizes 8 (3
            // used), 8 (3 used), 4, 2.
			list2.AddRange(list2, list2.WithoutFirst(3));
			ExpectList(list2, 10,9,8,10,9,8,7,6,5,4,2,1);
			Assert.That(list3 != list2);
			
			// List3 is a sublist of list, but list2 no longer is
			Assert.That(list3.PreviousIn(list).First == 11);
			AssertThrows<InvalidOperationException>(delegate() { list2.PreviousIn(list); });
			
			list2 = list2.WithoutFirst(3);
			Assert.That(list3 == list2);
		}

		[Test]
		public void TestInsertRemove()
		{
			FVList<int> list = new FVList<int>(9);
			FVList<int> list2 = new FVList<int>(10, 11);
			list.Insert(1, 12);
			list.Insert(1, list2[0]);
			list.Insert(2, list2[1]);
			ExpectList(list, 9, 10, 11, 12);
			for (int i = 0; i < 9; i++)
				list.Insert(i, i);
			ExpectList(list, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);

			list2 = list;
			for (int i = 1; i <= 6; i++)
				list2.RemoveAt(i);
			ExpectList(list2, 0, 2, 4, 6, 8, 10, 12);
			ExpectList(list, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12); // unchanged

			Assert.AreEqual(0, list2.Pop());
			list2.Insert(5, -2);
			ExpectList(list2, 2, 4, 6, 8, 10, -2, 12);
			list2.Insert(5, -1);
			ExpectList(list2, 2, 4, 6, 8, 10, -1, -2, 12);
			
			// Test changing items
			list = list2;
			for (int i = 0; i < list.Count; i++)
				list[i] = i;
			ExpectList(list, 0, 1, 2, 3, 4, 5, 6, 7);
			ExpectList(list2, 2, 4, 6, 8, 10, -1, -2, 12);

			list2.Clear();
			ExpectList(list2);
			Assert.AreEqual(5, list[5]);
		}

		[Test]
		public void TestInsertRemoveRange()
		{
			FVList<int> oneTwo = new FVList<int>(1, 2);
			FVList<int> threeFour = new FVList<int>(3, 4);
			FVList<int> list = oneTwo;
			FVList<int> list2 = threeFour;

			ExpectList(list, 1, 2);
			list.InsertRange(1, threeFour);
			ExpectList(list, 1, 3, 4, 2);
			list2.InsertRange(2, oneTwo);
			ExpectList(list2, 3, 4, 1, 2);

			list.RemoveRange(1, 2);
			ExpectList(list, 1, 2);
			list2.RemoveRange(2, 2);
			ExpectList(list2, 3, 4);

			list.RemoveRange(0, 2);
			ExpectList(list);
			list2.RemoveRange(1, 1);
			ExpectList(list2, 3);

			list = threeFour;
			list.AddRange(oneTwo);
			ExpectList(list, 1, 2, 3, 4);
			list.InsertRange(1, list);
			ExpectList(list, 1, 1, 2, 3, 4, 2, 3, 4);
			list.RemoveRange(1, 1);
			list.RemoveRange(4, 3);
			ExpectList(list, 1, 2, 3, 4);

			list.RemoveRange(0, 4);
			ExpectList(list);

			list2.InsertRange(0, list);
			list2.InsertRange(1, list);
			ExpectList(list2, 3);
		}

		[Test]
		public void TestEmptyListOperations()
		{
			FVList<int> a = new FVList<int>();
			FVList<int> b = new FVList<int>();
			a.AddRange(b);
			a.InsertRange(0, b);
			a.RemoveRange(0, 0);
			Assert.That(!a.Remove(0));
			Assert.That(a.IsEmpty);
			Assert.That(a.WithoutFirst(0).IsEmpty);

			a.Add(1);
			Assert.That(a.WithoutFirst(1).IsEmpty);

			b.AddRange(a);
			ExpectList(b, 1);
			b.RemoveAt(0);
			Assert.That(b.IsEmpty);
			b.InsertRange(0, a);
			ExpectList(b, 1);
			b.RemoveRange(0, 1);
			Assert.That(b.IsEmpty);
			b.Insert(0, a[0]);
			ExpectList(b, 1);
			b.Remove(a.First);
			Assert.That(b.IsEmpty);

			AssertThrows<InvalidOperationException>(delegate() { a.PreviousIn(b); });
		}

		[Test]
		public void TestToArray()
		{
			FVList<int> list = new FVList<int>();
			int[] array = list.ToArray();
			Assert.AreEqual(array.Length, 0);

			array = list.Add(1).ToArray();
			ExpectList(array.AsListSource(), 1);

			array = list.Add(2).ToArray();
			ExpectList(array.AsListSource(), 2, 1);
			
			array = list.Add(3).ToArray();
			ExpectList(array.AsListSource(), 3, 2, 1);

			array = list.AddRange(new int[] { 8, 7, 6, 5, 4 }).ToArray();
			ExpectList(array.AsListSource(), 8, 7, 6, 5, 4, 3, 2, 1);
		}

		[Test]
		public void TestMultithreadedAdds()
		{
			object @lock = new object();
			FVList<int> basisList = new FVList<int>();
			List<Thread> threads = new List<Thread>();
			foreach (int seed_ in new int[] { 0, 10000, 20000 })
			{
				int seed = seed_; // capture loop variable
				Thread t = new Thread(delegate()
				{
					FVList<int> list;
					int count;
					for (int i = 0; i < 10000; i++) {
						lock (@lock) {
							list = basisList;
							count = list.Count;
						}

						list.Add(seed + i);
						Assert.AreEqual(count + 1, list.Count);
						Assert.AreEqual(seed + i, list.First);

						if (seed == 0)
							list.Pop();

						lock (@lock) {
							basisList = list;
						}
					}
				});
				t.Start();
				threads.Add(t);
			}
			bool done;
			do {
				done = true;
				for (int i = 0; i < threads.Count; i++) {
					if (threads[i].IsAlive)
						done = false;
				}
			} while (!done);
			Assert.That(true); // breakpoint
		}

		[Test]
		public void TestWhere()
		{
			FVList<int> one = new FVList<int>(3);
			FVList<int> two = one.Clone().Add(2);
			FVList<int> thr = two.Clone().Add(1);
			ExpectList(one.Where(delegate(int i) { return false; }));
			ExpectList(two.Where(delegate(int i) { return false; }));
			ExpectList(thr.Where(delegate(int i) { return false; }));
			Assert.That(one.Where(delegate(int i) { return true; }) == one);
			Assert.That(two.Where(delegate(int i) { return true; }) == two);
			Assert.That(thr.Where(delegate(int i) { return true; }) == thr);
			Assert.That(two.Where(delegate(int i) { return i==3; }) == one);
			Assert.That(thr.Where(delegate(int i) { return i==3; }) == one);
			Assert.That(thr.Where(delegate(int i) { return i>1; }) == two);
			ExpectList(two.Where(delegate(int i) { return i==2; }), 2);
			ExpectList(thr.Where(delegate(int i) { return i==2; }), 2);
		}

		[Test]
		public void TestSelect()
		{
			FVList<int> one = new FVList<int>(3);
			FVList<int> two = one.Clone().Add(2);
			FVList<int> thr = two.Clone().Add(1);
			ExpectList(thr, 1, 2, 3);

			Assert.That(one.SmartSelect(delegate(int i) { return i; }) == one);
			Assert.That(two.SmartSelect(delegate(int i) { return i; }) == two);
			Assert.That(thr.SmartSelect(delegate(int i) { return i; }) == thr);
			ExpectList(one.SmartSelect(delegate(int i) { return i+1; }), 4);
			ExpectList(two.SmartSelect(delegate(int i) { return i+1; }), 3, 4);
			ExpectList(thr.SmartSelect(delegate(int i) { return i+1; }), 2, 3, 4);
			ExpectList(two.SmartSelect(delegate(int i) { return i==3 ? 3 : 0; }), 0, 3);
			ExpectList(thr.SmartSelect(delegate(int i) { return i==3 ? 3 : 0; }), 0, 0, 3);
			ExpectList(thr.SmartSelect(delegate(int i) { return i==1 ? 0 : i; }), 0, 2, 3);
			Assert.That(thr.SmartSelect(delegate(int i) { return i==1 ? 0 : i; }).WithoutFirst(1) == two);
		}

		[Test]
		public void TestTransform()
		{
			// Test transforms on 1-item lists. The helper method TestTransform() 
			// creates a list of the specified length, counting up from 1 at the 
			// tail. For instance, TestTransform(3, ...) will start with a FVList of 
			// (3, 2, 1). Its transform function always multiplies the item by 10,
			// then it returns the next action in the list. FVList<int>.Transform()
			// transforms the tail first, so for example,
			// 
			//    TestTransform(4, ..., XfAction.Keep, XfAction.Change, 
			//                          XfAction.Drop, XfAction.Keep);
			// 
			// ...should produce a result of (4, 20, 1) as a FVList, which is 
			// equivalent to the VList (1, 20, 4).
			
			// Tests on 1-item lists
			TestTransform(1, new int[] {},   0, XfAction.Drop);
			TestTransform(1, new int[] {1},  1, XfAction.Keep);
			TestTransform(1, new int[] {10}, 0, XfAction.Change);
			TestTransform(1, new int[] {10}, 0, XfAction.Repeat, XfAction.Drop);

			// Tests on 2-item lists
			TestTransform(2, new int[] {},         0, XfAction.Drop, XfAction.Drop);
			TestTransform(2, new int[] {2},        0, XfAction.Drop, XfAction.Keep);
			TestTransform(2, new int[] {20},       0, XfAction.Drop, XfAction.Change);
			TestTransform(2, new int[] {20, 2},    0, XfAction.Drop, XfAction.Repeat, XfAction.Keep);
			TestTransform(2, new int[] {1},        1, XfAction.Keep, XfAction.Drop);
			TestTransform(2, new int[] {1, 2},     2, XfAction.Keep, XfAction.Keep);
			TestTransform(2, new int[] {1, 20},    0, XfAction.Keep, XfAction.Change);
			TestTransform(2, new int[] {1, 20},    0, XfAction.Keep, XfAction.Repeat, XfAction.Drop);
			TestTransform(2, new int[] {10},       0, XfAction.Change, XfAction.Drop);
			TestTransform(2, new int[] {10, 2},    0, XfAction.Change, XfAction.Keep);
			TestTransform(2, new int[] {10, 20},   0, XfAction.Change, XfAction.Change);
			TestTransform(2, new int[] {10,20,200},0, XfAction.Change, XfAction.Repeat, XfAction.Change);
			TestTransform(2, new int[] {10},       0, XfAction.Repeat, XfAction.Drop, XfAction.Drop);
			TestTransform(2, new int[] {10,1,2},   0, XfAction.Repeat, XfAction.Keep, XfAction.Keep);
			TestTransform(2, new int[] {10,100,20},0, XfAction.Repeat, XfAction.Change, XfAction.Change);
			TestTransform(2, new int[] {10,100,1000,2}, 0, XfAction.Repeat, XfAction.Repeat, XfAction.Change, XfAction.Keep);
			TestTransform(2, new int[] {10,100,1000,1}, 0, XfAction.Repeat, XfAction.Repeat, XfAction.Repeat, XfAction.Keep, XfAction.Drop);

			TestTransform(3, new int[] { 20, 2, 30 },   0, XfAction.Drop, XfAction.Repeat, XfAction.Keep, XfAction.Change);
			TestTransform(3, new int[] { 10, 100, 3 },  0, XfAction.Repeat, XfAction.Change, XfAction.Drop, XfAction.Keep);
			TestTransform(3, new int[] { 1, 2, 3 },     3, XfAction.Keep, XfAction.Keep, XfAction.Keep);

			TestTransform(4, new int[] { 1, 2, 40 },    2, XfAction.Keep, XfAction.Keep, XfAction.Drop, XfAction.Change);
			TestTransform(4, new int[] { 1, 2, 3, 4 },  4, XfAction.Keep, XfAction.Keep, XfAction.Keep, XfAction.Keep);
			TestTransform(4, new int[] { 1, 2, 3 },     3, XfAction.Keep, XfAction.Keep, XfAction.Keep, XfAction.Drop);
			TestTransform(4, new int[] { 1, 2, 3, 40 }, 2, XfAction.Keep, XfAction.Keep, XfAction.Keep, XfAction.Change);
		}

		private void TestTransform(int count, int[] expect, int commonTailLength, params XfAction[] actions)
		{
			FVList<int> list = new FVList<int>();
			for (int i = 0; i < count; i++)
				list.Add(i + 1);

			int counter = 0;
			FVList<int> result =
				list.Transform(delegate(int i, ref int item) {
					if (i >= 0)
						Assert.AreEqual(list[i], item);
					item *= 10;
					return actions[counter++];
				});
			
			Assert.AreEqual(counter, actions.Length);
			
			ExpectList(result.ToVList(), expect);
			
			Assert.That(result.WithoutFirst(result.Count - commonTailLength)
			           == list.WithoutFirst(list.Count - commonTailLength));
		}
	}
}
