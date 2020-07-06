using System;
using System.Collections.Generic;
using System.Linq;
using Loyc.Collections.Impl;
using Loyc.MiniTest;

namespace Loyc.Collections
{
	[TestFixture]
	public class VListTests : TestHelpers
	{
		[Test]
		public void SimpleTests()
		{
			// In this simple test, I only add and remove items from the back
			// of a VList, but forking is also tested.

			VList<int> list = new VList<int>();
			Assert.That(list.IsEmpty);

			// Adding to VListBlockOfTwo
			list = new VList<int>(10, 20);
			ExpectList(list, 10, 20);

			list = new VList<int>();
			list.Add(1);
			Assert.That(!list.IsEmpty);
			list.Add(2);
			ExpectList(list, 1, 2);

			// A fork in VListBlockOfTwo. Note that list2 will use two VListBlocks
			// here but list will only use one.
			VList<int> list2 = list.WithoutLast(1);
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

			// PreviousIn(), Last
			VList<int> list3 = list2;
			Assert.AreEqual(11, (list3 = list3.NextIn(list)).Last);
			Assert.AreEqual(12, (list3 = list3.NextIn(list)).Last);
			Assert.AreEqual(13, (list3 = list3.NextIn(list)).Last);
			Assert.AreEqual(14, (list3 = list3.NextIn(list)).Last);
			Assert.AreEqual(15, (list3 = list3.NextIn(list)).Last);
			Assert.AreEqual(16, (list3 = list3.NextIn(list)).Last);
			AssertThrows<Exception>(delegate() { list3.NextIn(list); });

			// Next
			Assert.AreEqual(10, (list3 = list3.WithoutLast(6)).Last);
			Assert.AreEqual(9, (list3 = list3.Tail).Last);
			Assert.AreEqual(8, (list3 = list3.Tail).Last);
			Assert.AreEqual(7, (list3 = list3.Tail).Last);
			Assert.AreEqual(6, (list3 = list3.Tail).Last);
			Assert.AreEqual(5, (list3 = list3.Tail).Last);
			Assert.AreEqual(4, (list3 = list3.Tail).Last);
			Assert.AreEqual(2, (list3 = list3.Tail).Last);
			Assert.AreEqual(1, (list3 = list3.Tail).Last);
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
			Assert.That(list3.NextIn(list).Last == 11);
			AssertThrows<InvalidOperationException>(delegate() { list2.NextIn(list); });

			list2 = list2.WithoutLast(3);
			Assert.That(list3 == list2);
		}

		[Test]
		public void TestInsertRemove()
		{
			VList<int> list = new VList<int>(9);
			VList<int> list2 = new VList<int>(10, 11);
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
			VList<int> oneTwo = new VList<int>(1, 2);
			VList<int> threeFour = new VList<int>(3, 4);
			VList<int> list = oneTwo;
			VList<int> list2 = threeFour;

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
			VList<int> a = new VList<int>();
			VList<int> b = new VList<int>();
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
			b.Remove(a.Last);
			Assert.That(b.IsEmpty);
			
			AssertThrows<InvalidOperationException>(delegate() { a.NextIn(b); });
		}

		[Test]
		public void TestToArray()
		{
			VList<int> list = new VList<int>();
			int[] array = list.ToArray();
			Assert.AreEqual(array.Length, 0);

			array = list.Add(1).ToArray();
			ExpectList(array.AsListSource(), 1);

			array = list.Add(2).ToArray();
			ExpectList(array.AsListSource(), 1, 2);

			array = list.Add(3).ToArray();
			ExpectList(array.AsListSource(), 1, 2, 3);

			array = list.AddRange(new int[] { 4, 5, 6, 7, 8 }).ToArray();
			ExpectList(array.AsListSource(), 1, 2, 3, 4, 5, 6, 7, 8);
		}

		[Test]
		void TestAddRangePair()
		{
			VList<int> list = new VList<int>();
			VList<int> list2 = new VList<int>();
			list2.AddRange(new int[] { 1, 2, 3, 4 });
			list.AddRange(list2, list2.WithoutLast(1));
			list.AddRange(list2, list2.WithoutLast(2));
			list.AddRange(list2, list2.WithoutLast(3));
			list.AddRange(list2, list2.WithoutLast(4));
			ExpectList(list, 1, 2, 3, 1, 2, 1);

			AssertThrows<InvalidOperationException>(delegate() { list2.AddRange(list2.WithoutLast(1), list2); });
			AssertThrows<InvalidOperationException>(delegate() { list2.AddRange(VList<int>.Empty, list2); });
		}
		
		[Test]
		public void TestSublistProblem()
		{
			// This problem affects FVList.PreviousIn(), VList.NextIn(),
			// AddRange(list, excludeSubList), VList.Enumerator when used with a
			// range.

			// Normally this works fine:
			VList<int> subList = new VList<int>(), list;
			subList.AddRange(new int[] { 1, 2, 3, 4, 5, 6, 7 });
			list = subList;
			list.Add(8);
			Assert.That(subList.NextIn(list).Last == 8);

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
			Assert.AreEqual(9, subList.NextIn(list).Last);
		}

		[Test]
		public void TestExampleTransforms()
		{
			// These examples are listed in the documentation of FVList.Transform().
			// There are more Transform() tests in VListTests() and RWListTests().

			VList<int> list = new VList<int>(new int[] { -1, 2, -2, 13, 5, 8, 9 });
			VList<int> output;

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

			list = new VList<int>(new int[] { 1, 2, 3 });

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

		Random _r = new Random();

		[Test]
		public void SelectManyTests()
		{
			// Plan: make a series of lists of different lengths and transform them
			// semi-randomly, but do the same transformation on a regular list. Then
			// ensure that the VList came out the same as the plain List. 
			//
			// Note: this test isn't included in FVListTests because the FVList.Smart
			// methods act in "reverse order" compared to the LINQ methods Select/Where/etc.
			// so the results wouldn't be the same between List<int> and FVList<int>.
			int initialLength = 0, trial = -1;
			string subtest = "";
			int same = 0, pattern = 0;
			try {
				for (initialLength = 0; initialLength < 100; initialLength++) {
					trial = -1;
					VList<int> vlist = VList<int>.Empty;
					List<int> list = new List<int>();
					for (int i = 0; i < initialLength; i++) {
						vlist.Add(i);
						list.Add(i);
					}

					// First, ensure that if the list is not changed, the same list comes out
					subtest = "unchanged";
					Assert.AreEqual(vlist, vlist.SmartSelect(i => i));
					Assert.IsTrue(vlist == vlist.SmartSelect(i => i));
					Assert.AreEqual(vlist, vlist.SmartWhere(i => true));
					Assert.IsTrue(vlist == vlist.SmartWhere(i => true));
					Assert.AreEqual(vlist, vlist.SmartSelectMany(i => new[] { i }));
					Assert.IsTrue(vlist == vlist.SmartSelectMany(i => new[] { i }));

					for (trial = 0; trial < (initialLength.IsInRange(1, 10) ? 4 : 1); trial++) {
						// Number of items to keep the same at the beginning of the transform
						same = initialLength == 0 ? 0 : _r.Next(8) % initialLength;
						pattern = _r.Next();
						int index = 0;
						Func<int, int> select = n => ++index <= same ? n : n + 1000;
						Func<int, bool> where = n => ++index <= same * 2 || ((pattern >> (index & 31)) & 1) == 0;
						Func<int, IReadOnlyList<int>> selectMany = n =>
						{
							var @out = new List<int>();
							if (index < same)
								@out.Add(n);
							else {
								for (int i = 0; i < ((pattern >> (index & 15)) & 3); i++)
									@out.Add(index * 1000 + i);
							}
							index++;
							return @out;
						};
						Func<int, IEnumerable<int>> selectManyE = n => selectMany(n);
						subtest = "SmartSelect";
						var expectS = list.Select(select); index = 0;
						var resultS = vlist.SmartSelect(select); index = 0;
						ExpectList(resultS, expectS);
						subtest = "SmartWhere";
						var expectW = list.Where(where); index = 0;
						var resultW = vlist.SmartWhere(where); index = 0;
						ExpectList(resultW, expectW);
						subtest = "SmartSelectMany";
						var expectM = list.SelectMany(selectManyE); index = 0;
						var resultM = vlist.SmartSelectMany(selectMany); index = 0;
						ExpectList(resultM, expectM);
					}
				}
			} catch (Exception e) {
				e.Data["subtest"] = subtest;
				e.Data["initialLength"] = initialLength;
				e.Data["trial"] = trial;
				e.Data["same"] = same;
				e.Data["pattern"] = pattern;
				throw;
			}
		}
	}
}
