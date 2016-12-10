using System;
using System.Collections.Generic;
using System.Linq;
using Loyc.Collections.Impl;
using Loyc.MiniTest;

namespace Loyc.Collections
{
	[TestFixture]
	public class WListTests
	{
		[Test]
		public void SimpleTests()
		{
			// Tests simple adds and removes from the front of the list. It
			// makes part of its tail immutable, but doesn't make it mutable
			// again. Also, we test operations that don't modify the list.

			WList<int> list = new WList<int>();
			Assert.That(list.IsEmpty);
			
			// create VListBlockOfTwo
			list = new WList<int>(10, 20);
			ExpectList(list, 10, 20);

			// Add()
			list.Clear();
			list.Add(1);
			Assert.That(!list.IsEmpty);
			list.Add(2);
			Assert.AreEqual(1, list.BlockChainLength);
			list.Add(3);
			Assert.AreEqual(2, list.BlockChainLength);

			ExpectList(list, 1, 2, 3);
			VList<int> snap = list.ToVList();
			ExpectList(snap, 1, 2, 3);
			
			// AddRange(), Push(), Pop()
			list.Push(4);
			list.AddRange(new int[] { 5, 6 });
			ExpectList(list, 1, 2, 3, 4, 5, 6);
			Assert.AreEqual(list.Pop(), 6);
			ExpectList(list, 1, 2, 3, 4, 5);
			list.RemoveRange(3, 2);
			ExpectList(list, 1, 2, 3);

			// Double the list
			list.AddRange(list);
			ExpectList(list, 1, 2, 3, 1, 2, 3);
			list.RemoveRange(3, 3);

			// Fill a third block
			list.AddRange(new int[] { 4, 5, 6, 7, 8, 9 });
			list.AddRange(new int[] { 10, 11, 12, 13, 14 });
			ExpectList(list, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14);
			
			// Remove(), enumerator
			list.Remove(14);
			list.Remove(13);
			list.Remove(12);
			list.Remove(11);
			ExpectListByEnumerator(list, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
			
			// IndexOutOfRangeException
			AssertThrows<IndexOutOfRangeException>(delegate() { int i = list[-1]; });
			AssertThrows<IndexOutOfRangeException>(delegate() { int i = list[10]; });
			AssertThrows<IndexOutOfRangeException>(delegate() { list.Insert(-1, -1); });
			AssertThrows<IndexOutOfRangeException>(delegate() { list.Insert(list.Count+1, -1); });
			AssertThrows<IndexOutOfRangeException>(delegate() { list.RemoveAt(-1); });
			AssertThrows<IndexOutOfRangeException>(delegate() { list.RemoveAt(list.Count); });

			// Last, Contains, IndexOf
			Assert.That(list.Last == 10);
			Assert.That(list.Contains(9));
			Assert.That(list[list.IndexOf(2)] == 2);
			Assert.That(list[list.IndexOf(9)] == 9);
			Assert.That(list[list.IndexOf(7)] == 7);
			Assert.That(list.IndexOf(-1) == -1);

			// snap is still the same
			ExpectList(snap, 1, 2, 3);
		}

		private void AssertThrows<Type>(Action @delegate)
		{
			try {
				@delegate();
			} catch (Exception exc) {
				Assert.IsInstanceOf<Type>(exc);
				return;
			}
			Assert.Fail("Delegate did not throw '{0}' as expected.", typeof(Type).Name);
		}

		private static void ExpectList<T>(IList<T> list, params T[] expected)
		{
			Assert.AreEqual(expected.Length, list.Count);
			for (int i = 0; i < expected.Length; i++)
				Assert.AreEqual(expected[i], list[i]);
		}
		private static void ExpectListByEnumerator<T>(IList<T> list, params T[] expected)
		{
			Assert.AreEqual(expected.Length, list.Count);
			int i = 0;
			foreach (T item in list) {
				Assert.AreEqual(expected[i], item);
				i++;
			}
		}

		[Test]
		public void TestFork()
		{
			WList<int> A = new WList<int>();
			A.AddRange(new int[] { 1, 2, 3 });
			WList<int> B = A.Clone();
			
			A.Push(4);
			ExpectList(B, 1, 2, 3);
			ExpectList(A, 1, 2, 3, 4);
			B.Push(-4);
			ExpectList(B, 1, 2, 3, -4);

			Assert.That(A.WithoutLast(2) == B.WithoutLast(2));
		}

		[Test]
		public void TestMutabilification()
		{
			// Make a single block mutable
			VList<int> v = new VList<int>(1, 0);
			WList<int> w = v.ToWList();
			ExpectList(w, 1, 0);
			w[1] = 2;
			ExpectList(w, 1, 2);
			ExpectList(v, 1, 0);

			// Make another block, make the front block mutable, then the block-of-2
			v.Push(-1);
			w = v.ToWList();
			w[2] = 3;
			ExpectList(w, 1, 0, 3);
			Assert.That(w.WithoutLast(1) == v.WithoutLast(1));
			w[1] = 2;
			ExpectList(w, 1, 2, 3);
			Assert.That(w.WithoutLast(1) != v.WithoutLast(1));

			// Now for a more complicated case: create a long immutable chain by
			// using a nasty access pattern, add a mutable block in front, then 
			// make some of the immutable blocks mutable. This will cause several
			// immutable blocks to be consolidated into one mutable block, 
			// shortening the chain.
			v = new VList<int>(6);
			v = v.Add(-1).Tail.Add(5).Add(-1).Tail.Add(4).Add(-1).Tail.Add(3);
			v = v.Add(-1).Tail.Add(2).Add(-1).Tail.Add(1).Add(-1).Tail.Add(0);
			ExpectList(v, 6, 5, 4, 3, 2, 1, 0);
			// At this point, every block in the chain has only one item (it's 
			// a linked list!) and the capacity of each block is 2.
			Assert.AreEqual(7, v.BlockChainLength);

			w = v.ToWList();
			w.AddRange(new int[] { 1, 2, 3, 4, 5 });
			Assert.AreEqual(w.Count, 12);
			ExpectList(w, 6, 5, 4, 3, 2, 1, 0, 1, 2, 3, 4, 5);
			// Indices:   0  1  2  3  4  5  6  7  8  9  10 11
			// Blocks:    H| G| F| E| D| C|  B  | block A (front of chain)
			Assert.AreEqual(8, w.BlockChainLength);
			Assert.AreEqual(4, w.LocalCount);

			w[3] = -3;
			ExpectList(w, 6, 5, 4, -3, 2, 1, 0, 1, 2, 3, 4, 5);
			// Indices:   0  1  2   3  4  5  6  7  8  9  10 11
			// Blocks:    H| G| F|  block I      | block A (front of chain)
			Assert.AreEqual(5, w.BlockChainLength);
		}

		[Test]
		public void TestInsertRemove()
		{
			WList<int> list = new WList<int>();
			for (int i = 0; i <= 12; i++)
				list.Insert(0, i);
			ExpectList(list, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);

			for (int i = 1; i <= 6; i++)
				list.RemoveAt(i);
			ExpectList(list, 12, 10, 8, 6, 4, 2, 0);

			Assert.AreEqual(0, list.Pop());
			list.Insert(1, -2);
			ExpectList(list, 12, -2, 10, 8, 6, 4, 2);
			list.Insert(2, -1);
			ExpectList(list, 12, -2, -1, 10, 8, 6, 4, 2);

			Assert.That(list.Remove(-1));
			Assert.That(list.Remove(12));
			list[0] = 12;
			ExpectList(list, 12, 10, 8, 6, 4, 2);

			// Make sure WList.Clear doesn't disturb FVList
			VList<int> v = list.WithoutLast(4);
			list.Clear();
			ExpectList(list);
			ExpectList(v, 12, 10);

			// Some simple InsertRange calls where some immutable items must be
			// converted to mutable
			VList<int> oneTwo = new VList<int>(1, 2);
			VList<int> threeFour = new VList<int>(3, 4);
			list = oneTwo.ToWList();
			list.InsertRange(1, threeFour);
			ExpectList(list, 1, 3, 4, 2);
			list = threeFour.ToWList();
			list.InsertRange(0, oneTwo);
			ExpectList(list, 1, 2, 3, 4);

			// More tests...
			list.RemoveRange(2, 2);
			ExpectList(list, 1, 2);
			list.InsertRange(2, new int[] { 3, 3, 4, 4, 4, 5, 6, 7, 8, 9 });
			ExpectList(list, 1, 2, 3, 3, 4, 4, 4, 5, 6, 7, 8, 9);
			list.RemoveRange(3, 3);
			ExpectList(list, 1, 2, 3, 4, 5, 6, 7, 8, 9);
			v = list.ToVList();
			list.RemoveRange(5, 4);
			ExpectList(list, 1, 2, 3, 4, 5);
			ExpectList(v,    1, 2, 3, 4, 5, 6, 7, 8, 9);
		}

		[Test]
		public void TestEmptyListOperations()
		{
			WList<int> a = new WList<int>();
			WList<int> b = new WList<int>();
			a.AddRange(b);
			a.InsertRange(0, b);
			a.RemoveRange(0, 0);
			Assert.That(!a.Remove(0));
			Assert.That(a.IsEmpty);
			Assert.That(a.WithoutLast(0).IsEmpty);

			a.Add(1);
			Assert.That(a.WithoutLast(1).IsEmpty);

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
			b.Remove(a.Last);
			Assert.That(b.IsEmpty);
		}
		[Test]
		public void TestFalseOwnership()
		{
			// This test tries to make sure a WList doesn't get confused about what 
			// blocks it owns. It's possible for a WList to share a partially-mutable 
			// block that contains mutable items with another WList, but only one
			// WList owns the items.

			// Case 1: two WLists point to the same block but only one owns it:
			//
			//        block 0
			//      owned by A
			//        |____3|    block 1
			//        |____2|    unowned
			// A,B--->|Imm_1|--->|Imm_1|
			//        |____0|    |____0|
			//
			// (The location of "Imm" in each block denotes the highest immutable 
			// item; this diagram shows there are two immutable items in each 
			// block)
			WList<int> A = new WList<int>();
			A.Resize(4);
			for (int i = 0; i < 4; i++)
				A[i] = i;
			WList<int> B = A.Clone();
			
			// B can't add to the second block because it's not the owner, so a 
			// third block is created when we Add(1).
			B.Add(4);
			A.Add(-4);
			ExpectList(A, 0, 1, 2, 3, -4);
			ExpectList(B, 0, 1, 2, 3, 4);
			Assert.AreEqual(2, A.BlockChainLength);
			Assert.AreEqual(3, B.BlockChainLength);

			// Case 2: two WLists point to different blocks but they share a common
			// tail, where one list owns part of the tail and the other does not:
			//
			//      block 0
			//    owned by B
			//      |____8|
			//      |____7|
			//      |____6|   
			//      |____5|         block 1
			//      |____4|       owned by A
			//      |____3|   A     |____3|     block 2
			//      |____2|   |     |____2|     unowned
			//      |____1|---+---->|Imm_1|---->|Imm_1|
			// B--->|____0|         |____0|     |____0|
			//      mutable
			//
			// Actually the previous test puts us in just this state.
			//
			// I can't think of a test that uses the public interface to detect bugs
			// in this case. The most important thing is that B._block.PriorIsOwned 
			// returns false. 
			Assert.That(B.IsOwner && !B.Block.PriorIsOwned);
			Assert.That(A.IsOwner);
			Assert.That(B.Block.Prior.ToVList() == A.WithoutLast(1));
		}

		[Test]
		public void TestWhere()
		{
			WList<int> one = new WList<int>(); one.Add(3);
			WList<int> two = one.Clone();       two.Add(2);
			WList<int> thr = two.Clone();       thr.Add(1);
			ExpectList(one.Where(delegate(int i) { return false; }));
			ExpectList(two.Where(delegate(int i) { return false; }));
			ExpectList(thr.Where(delegate(int i) { return false; }));
			Assert.That(one.Where(delegate(int i) { return true; }).ToVList() == one.ToVList());
			Assert.That(two.Where(delegate(int i) { return true; }).ToVList() == two.ToVList());
			Assert.That(thr.Where(delegate(int i) { return true; }).ToVList() == thr.ToVList());
			Assert.That(two.Where(delegate(int i) { return i==3; }).ToVList() == two.WithoutLast(1));
			Assert.That(thr.Where(delegate(int i) { return i==3; }).ToVList() == thr.WithoutLast(2));
			Assert.That(thr.Where(delegate(int i) { return i>1; }).ToVList() == thr.WithoutLast(1));
			ExpectList(two.Where(delegate(int i) { return i==2; }), 2);
			ExpectList(thr.Where(delegate(int i) { return i==2; }), 2);
		}

		[Test]
		public void TestSelect()
		{
			WList<int> one = new WList<int>(); one.Add(3);
			WList<int> two = one.Clone();       two.Add(2);
			WList<int> thr = two.Clone();       thr.Add(1);
			ExpectList(thr, 3, 2, 1);

			Assert.That(one.SmartSelect(delegate(int i) { return i; }).ToVList() == one.ToVList());
			Assert.That(two.SmartSelect(delegate(int i) { return i; }).ToVList() == two.ToVList());
			Assert.That(thr.SmartSelect(delegate(int i) { return i; }).ToVList() == thr.ToVList());
			ExpectList(one.SmartSelect(delegate(int i) { return i + 1; }), 4);
			ExpectList(two.SmartSelect(delegate(int i) { return i + 1; }), 4, 3);
			ExpectList(thr.SmartSelect(delegate(int i) { return i + 1; }), 4, 3, 2);
			ExpectList(two.SmartSelect(delegate(int i) { return i == 3 ? 3 : 0; }), 3, 0);
			ExpectList(thr.SmartSelect(delegate(int i) { return i == 3 ? 3 : 0; }), 3, 0, 0);
			ExpectList(thr.SmartSelect(delegate(int i) { return i == 1 ? 0 : i; }), 3, 2, 0);
			Assert.That(thr.SmartSelect(delegate(int i) { return i == 1 ? 0 : i; }).WithoutLast(1) == thr.WithoutLast(1));
		}

		[Test]
		public void TestTransform()
		{
			// Test transforms on 1-item lists. The helper method TestTransform() 
			// creates a list of the specified length, counting up from 1 at the 
			// tail. For instance, TestTransform(3, ...) will start with a WList of 
			// (3, 2, 1). Its transform function always multiplies the item by 10,
			// then it returns the next action in the list. WList<int>.Transform()
			// transforms the tail first, so for example,
			// 
			//    TestTransform(4, ..., XfAction.Keep, XfAction.Change, 
			//                          XfAction.Drop, XfAction.Keep);
			// 
			// ...should produce a result of (4, 20, 1) as a WList, which is 
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
			WList<int> list = new WList<int>();
			for (int i = 0; i < count; i++)
				list.Add(i + 1);

			int counter = 0;
			WList<int> result =
				list.Transform(delegate(int i, ref int item) {
					if (i >= 0)
						Assert.AreEqual(list[i], item);
					item *= 10;
					return actions[counter++];
				});
			
			Assert.AreEqual(counter, actions.Length);
			
			ExpectList(result, expect);
			
			Assert.That(result.WithoutLast(result.Count - commonTailLength)
					 == list.WithoutLast(list.Count - commonTailLength));
			
			// Try to ensure there's no shared mutable memory by trashing the 
			// result starting at the head, and verifying the original list
			for (int i = result.Count - 1; i >= 0; i--)
				result[i] = -1;
			Assert.AreEqual(count, list.Count);
			for (int i = 0; i < count; i++)
				Assert.AreEqual(i + 1, list[i]);
		}
	}
}
