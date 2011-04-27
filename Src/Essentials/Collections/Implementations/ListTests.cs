using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Loyc.Essentials.Collections.Implementations
{
	/*
	[TestFixture]
	public class RVListTests
	{
		[Test]
		public void SimpleTests()
		{
			// In this simple test, I only add and remove items from the back
			// of an RVList, but forking is also tested.

			RVList<int> list = new RVList<int>();
			Assert.That(list.IsEmpty);

			// Adding to VListBlockOfTwo
			list = new RVList<int>(10, 20);
			ExpectList(list, 10, 20);

			list = new RVList<int>();
			list.Add(1);
			Assert.That(!list.IsEmpty);
			list.Add(2);
			ExpectList(list, 1, 2);

			// A fork in VListBlockOfTwo. Note that list2 will use two VListBlocks
			// here but list will only use one.
			RVList<int> list2 = list.WithoutLast(1);
			list2.Add(3);
			ExpectList(list, 1, 2);
			ExpectList(list2, 1, 3);

			// Try doubling list2
			list2.AddRange(list2);
			ExpectList(list2, 1, 3, 1, 3);

			// list now uses two arrays
			list.Add(4);
			ExpectList(list, 1, 2, 4);

			// Try doubling list using a different overload of AddRange()
			list.AddRange((IList<int>)list);
			ExpectList(list, 1, 2, 4, 1, 2, 4);
			list = list.WithoutLast(3);
			ExpectList(list, 1, 2, 4);

			// Remove(), Pop()
			Assert.AreEqual(3, list2.Pop());
			ExpectList(list2, 1, 3, 1);
			Assert.That(!list2.Remove(0));
			Assert.AreEqual(1, list2.Pop());
			Assert.That(list2.Remove(3));
			ExpectList(list2, 1);
			Assert.That(list2.Remove(1));
			ExpectList(list2);
			AssertThrows<Exception>(delegate() { list2.Pop(); });

			// Add many, SubList(). This will fill 3 arrays (sizes 8, 4, 2) and use
			// 1 element of a size-16 array. Oh, and test the enumerator.
			for (int i = 5; i <= 16; i++)
				list.Add(i);
			ExpectList(list, 1, 2, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
			list2 = list.WithoutLast(6);
			ExpectListByEnumerator(list2, 1, 2, 4, 5, 6, 7, 8, 9, 10);
			AssertThrows<IndexOutOfRangeException>(delegate() { int i = list[-1]; });
			AssertThrows<IndexOutOfRangeException>(delegate() { int i = list[15]; });

			// IndexOf, contains
			Assert.That(list.Contains(11));
			Assert.That(!list2.Contains(11));
			Assert.That(list[list.IndexOf(2)] == 2);
			Assert.That(list[list.IndexOf(1)] == 1);
			Assert.That(list[list.IndexOf(15)] == 15);
			Assert.That(list.IndexOf(3) == -1);

			// PreviousIn(), Back
			RVList<int> list3 = list2;
			Assert.AreEqual(11, (list3 = list3.NextIn(list)).Back);
			Assert.AreEqual(12, (list3 = list3.NextIn(list)).Back);
			Assert.AreEqual(13, (list3 = list3.NextIn(list)).Back);
			Assert.AreEqual(14, (list3 = list3.NextIn(list)).Back);
			Assert.AreEqual(15, (list3 = list3.NextIn(list)).Back);
			Assert.AreEqual(16, (list3 = list3.NextIn(list)).Back);
			AssertThrows<Exception>(delegate() { list3.NextIn(list); });

			// Next
			Assert.AreEqual(10, (list3 = list3.WithoutLast(6)).Back);
			Assert.AreEqual(9, (list3 = list3.Tail).Back);
			Assert.AreEqual(8, (list3 = list3.Tail).Back);
			Assert.AreEqual(7, (list3 = list3.Tail).Back);
			Assert.AreEqual(6, (list3 = list3.Tail).Back);
			Assert.AreEqual(5, (list3 = list3.Tail).Back);
			Assert.AreEqual(4, (list3 = list3.Tail).Back);
			Assert.AreEqual(2, (list3 = list3.Tail).Back);
			Assert.AreEqual(1, (list3 = list3.Tail).Back);
			Assert.That((list3 = list3.Tail).IsEmpty);

			// list2 is still the same
			ExpectList(list2, 1, 2, 4, 5, 6, 7, 8, 9, 10);

			// ==, !=, Equals(), AddRange(a, b)
			Assert.That(!list2.Equals("hello"));
			list3 = list2;
			Assert.That(list3.Equals(list2));
			Assert.That(list3 == list2);
			// This AddRange forks the list. List2 ends up with block sizes 8 (3
			// used), 8 (3 used), 4, 2.
			list2.AddRange(list2, list2.WithoutLast(3));
			ExpectList(list2, 1, 2, 4, 5, 6, 7, 8, 9, 10, 8, 9, 10);
			Assert.That(list3 != list2);

			// List3 is a sublist of list, but list2 no longer is
			Assert.That(list3.NextIn(list).Back == 11);
			AssertThrows<InvalidOperationException>(delegate() { list2.NextIn(list); });

			list2 = list2.WithoutLast(3);
			Assert.That(list3 == list2);
		}

		private void AssertThrows<Type>(TestDelegate @delegate)
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
		public void TestInsertRemove()
		{
			RVList<int> list = new RVList<int>(9);
			RVList<int> list2 = new RVList<int>(10, 11);
			list.Insert(0, 12);
			list.Insert(1, list2[1]);
			list.Insert(2, list2[0]);
			ExpectList(list, 12, 11, 10, 9);
			for (int i = 0; i < 9; i++)
				list.Insert(4, i);
			ExpectList(list, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);

			list2 = list;
			for (int i = 1; i <= 6; i++)
				list2.RemoveAt(i);
			ExpectList(list2, 12, 10, 8, 6, 4, 2, 0);
			ExpectList(list, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0); // unchanged

			Assert.AreEqual(0, list2.Pop());
			list2.Insert(5, -2);
			ExpectList(list2, 12, 10, 8, 6, 4, -2, 2);
			list2.Insert(5, -1);
			ExpectList(list2, 12, 10, 8, 6, 4, -1, -2, 2);

			// Test changing items
			list = list2;
			for (int i = 0; i < list.Count; i++)
				list[i] = i;
			ExpectList(list, 0, 1, 2, 3, 4, 5, 6, 7);
			ExpectList(list2, 12, 10, 8, 6, 4, -1, -2, 2);

			list2.Clear();
			ExpectList(list2);
			Assert.AreEqual(5, list[5]);
		}

		[Test]
		public void TestInsertRemoveRange()
		{
			RVList<int> oneTwo = new RVList<int>(1, 2);
			RVList<int> threeFour = new RVList<int>(3, 4);
			RVList<int> list = oneTwo;
			RVList<int> list2 = threeFour;

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

			list = oneTwo;
			list.AddRange(threeFour);
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
			RVList<int> a = new RVList<int>();
			RVList<int> b = new RVList<int>();
			a.AddRange(b);
			a.InsertRange(0, b);
			a.RemoveRange(0, 0);
			Assert.That(!a.Remove(0));
			Assert.That(a.IsEmpty);

			a.Add(1);
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
			b.Remove(a.Back);
			Assert.That(b.IsEmpty);
			
			AssertThrows<InvalidOperationException>(delegate() { a.NextIn(b); });
		}

		[Test]
		public void TestToArray()
		{
			RVList<int> list = new RVList<int>();
			int[] array = list.ToArray();
			Assert.AreEqual(array.Length, 0);

			array = list.Add(1).ToArray();
			ExpectList(array, 1);

			array = list.Add(2).ToArray();
			ExpectList(array, 1, 2);

			array = list.Add(3).ToArray();
			ExpectList(array, 1, 2, 3);

			array = list.AddRange(new int[] { 4, 5, 6, 7, 8 }).ToArray();
			ExpectList(array, 1, 2, 3, 4, 5, 6, 7, 8);
		}

		[Test]
		void TestAddRangePair()
		{
			RVList<int> list = new RVList<int>();
			RVList<int> list2 = new RVList<int>();
			list2.AddRange(new int[] { 1, 2, 3, 4 });
			list.AddRange(list2, list2.WithoutLast(1));
			list.AddRange(list2, list2.WithoutLast(2));
			list.AddRange(list2, list2.WithoutLast(3));
			list.AddRange(list2, list2.WithoutLast(4));
			ExpectList(list, 1, 2, 3, 1, 2, 1);

			AssertThrows<InvalidOperationException>(delegate() { list2.AddRange(list2.WithoutLast(1), list2); });
			AssertThrows<InvalidOperationException>(delegate() { list2.AddRange(RVList<int>.Empty, list2); });
		}
		
		[Test]
		public void TestSublistProblem()
		{
			// This problem affects FVList.PreviousIn(), RVList.NextIn(),
			// AddRange(list, excludeSubList), RVList.Enumerator when used with a
			// range.

			// Normally this works fine:
			RVList<int> subList = new RVList<int>(), list;
			subList.AddRange(new int[] { 1, 2, 3, 4, 5, 6, 7 });
			list = subList;
			list.Add(8);
			Assert.That(subList.NextIn(list).Back == 8);

			// But try it a second time and the problem arises, without some special
			// code in VListBlock<T>.FindNextBlock() that has been added to
			// compensate. I call the problem copy-causing-sharing-failure. You see,
			// right now subList is formed from three blocks: a size-8 block that
			// contains {7}, a size-4 block {3, 4, 5, 6} and a size-2 block {1, 2}.
			// But the size-8 block actually has two items {7, 8} and when we
			// attempt to add 9, a new array must be created. It might waste a lot
			// of memory to make a new block {9} that links to the size-8 block that
			// contains {7}, so instead a new size-8 block {7, 9} is created that
			// links directly to {3, 4, 5, 6}. That way, the block {7, 8} can be
			// garbage-collected if it is no longer in use. But a side effect is
			// that subList no longer appears to be a part of list. The fix is to
			// notice that list (block {7, 9}) and subList (block that contains {7})
			// have the same prior list, {3, 4, 5, 6}, and that the remaining 
			// item(s) in subList (just one item, {7}, in this case) are also
			// present in list.
			list = subList;
			list.Add(9);
			Assert.AreEqual(9, subList.NextIn(list).Back);
		}

		[Test]
		public void TestExampleTransforms()
		{
			// These examples are listed in the documentation of FVList.Transform().
			// There are more Transform() tests in VListTests() and RWListTests().

			RVList<int> list = new RVList<int>(new int[] { -1, 2, -2, 13, 5, 8, 9 });
			RVList<int> output;

			output = list.Transform((int i, ref int n) =>
			{   // Keep every second item
			    return (i % 2) == 1 ? XfAction.Keep : XfAction.Drop;
			});
			ExpectList(output, 2, 13, 8);
			
			output = list.Transform((int i, ref int n) =>
			{   // Keep odd numbers
			    return (n % 2) != 0 ? XfAction.Keep : XfAction.Drop;
			});
			ExpectList(output, -1, 13, 5, 9);
			
			output = list.Transform((int i, ref int n) =>
			{   // Keep and square all odd numbers
			    if ((n % 2) != 0) {
			        n *= n;
			        return XfAction.Change;
			    } else
			        return XfAction.Drop;
			});
			ExpectList(output, 1, 169, 25, 81);
			
			output = list.Transform((int i, ref int n) =>
			{   // Increase each item by its index
			    n += i;
			    return i == 0 ? XfAction.Keep : XfAction.Change;
			});
			ExpectList(output, -1, 3, 0, 16, 9, 13, 15);

			list = new RVList<int>(new int[] { 1, 2, 3 });

			output = list.Transform(delegate(int i, ref int n) {
				return i >= 0 ? XfAction.Repeat : XfAction.Keep;
			});
			ExpectList(output, 1, 1, 2, 2, 3, 3);

			output = list.Transform(delegate(int i, ref int n) {
				if (i >= 0) 
				 return XfAction.Repeat;
				n *= 10;
				return XfAction.Change;
			});
			ExpectList(output, 1, 10, 2, 20, 3, 30);

			output = list.Transform(delegate (int i, ref int n) {
				if (i >= 0) {
				 n *= 10;
				 return XfAction.Repeat;
				}
				return XfAction.Keep;
			});
			ExpectList(output, 10, 1, 20, 2, 30, 3);

			output = list.Transform(delegate (int i, ref int n) {
				n *= 10;
				if (n > 1000)
				 return XfAction.Drop;
				return XfAction.Repeat;
			});
			ExpectList(output, 10, 100, 1000, 20, 200, 30, 300);
		}
	}*/
}
