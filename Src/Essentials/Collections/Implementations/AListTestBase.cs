
namespace Loyc.Collections
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Collections.Specialized;
	using System.Diagnostics;
	using NUnit.Framework;
	using Loyc.Essentials;
	using Loyc.Math;
	using System.ComponentModel;

	/// <summary>
	/// Contains tests common to AList, BList and BDictionary.
	/// </summary>
	public abstract class AListTestBase<AList, T> : TestHelpers where AList : AListBase<int, T>, ICloneable<AList>
	{
		protected int _randomSeed;
		protected Random _r;

		public AListTestBase(int randomSeed)
		{
			_r = new Random(_randomSeed = randomSeed);
		}
		
		protected abstract AList NewList();
		
		/// <summary>Adds an item to an AList and then to a List at the same place.</summary>
		/// <param name="preferredIndex">Index to use if alist is an AList. 
		/// If it is a B+ tree then the item is always added in sorted order.</param>
		protected abstract int AddToBoth(AList alist, List<T> list, int item, int preferredIndex);

		protected abstract AList CopySection(AList alist, int start, int subcount);

		protected abstract AList RemoveSection(AList alist, int start, int subcount);

		protected abstract bool RemoveFromBoth(AList alist, List<T> list, int item);
		
		/// <summary>Remove an item from an AList and then from a List at the same location.</summary>
		protected void RemoveAtInBoth(AList alist, List<T> list, int index)
		{
			Assert.AreEqual(list.Count, alist.Count);
			Debug.Assert(index < list.Count);
			alist.RemoveAt(index);
			list.RemoveAt(index);
		}

		protected abstract int GetKey(T item);

		protected AList NewList(int initialCount, out List<T> list)
		{
			AList alist = NewList();
			list = new List<T>();

			// Add the items in such a way that we end up with a sorted list 
			// whether it is an AList or a B+ tree, but don't simply add the
			// items in order because we want to give the tree more chances 
			// to malfunction during the test :)
			int middle = Math.Max(initialCount * 2 / 3, 1);
			for (int i = middle; i < initialCount; i++)
				AddToBoth(alist, list, i, i - middle);
			ExpectList(alist, list, true);

			for (int i = 1; i < middle; i++)
				AddToBoth(alist, list, i, i - 1);
			if (initialCount > 0)
				AddToBoth(alist, list, 0, 0);
			ExpectList(alist, list, true);
			return alist;
		}


		[Test]
		public void NewListTest()
		{
			List<T> list;
			AList alist = NewList(10, out list);
			alist = NewList(50, out list);
			alist = NewList(100, out list);
			alist = NewList(1000, out list);
		}
		
		[Test]
		public void RemoveFirstTest()
		{
			List<T> list;
			AList alist = NewList(500, out list);
			for (int i = 0; i < 500; i++)
			{
				Assert.AreEqual(i, alist.First);
				alist.RemoveAt(0);
			}
			Assert.AreEqual(0, alist.Count);
		}
		
		[Test]
		public void RemoveLastTest()
		{
			List<T> list;
			AList alist = NewList(500, out list);
			for (int i = alist.Count-1; i >= 0; i--)
			{
				Assert.AreEqual(i, alist.Last);
				RemoveAtInBoth(alist, list, i);
				if (i % 100 == 0)
					ExpectList(alist, list, false);
			}
			Assert.AreEqual(0, alist.Count);
		}

		[Test]
		public void RemoveByIndex()
		{
			List<T> list;
			AList alist = NewList(500, out list);
			while (list.Count > 0) 
			{
				RemoveAtInBoth(alist, list, _r.Next(list.Count));
				if ((list.Count & 7) == 0)
					ExpectList(alist, list, false);
			}
			Assert.AreEqual(0, alist.Count);
		}

		[Test]
		public void RemoveByValue()
		{
			List<T> list;
			AList alist = NewList(500, out list);

			Assert.IsFalse(RemoveFromBoth(alist, list, -999));

			while (list.Count > 0)
			{
				int index = _r.Next(list.Count);
				Assert.IsTrue(RemoveFromBoth(alist, list, GetKey(list[index])));
				if ((list.Count & 7) == 0)
					ExpectList(alist, list, false);
			}
			Assert.AreEqual(0, alist.Count);
		}

		[Test]
		public void CloneAndObserveChanges()
		{
			List<T> list1, list2;
			AList alist1 = NewList(200, out list1);
			AList alist2 = alist1.Clone();
			list2 = new List<T>(list1);

			// This test checks two separate issues that shouldn't interact, but 
			// could interact in case of a bug:
			// (1) It checks whether list change events are sent and contain the 
			//     correct information
			// (2) It checks whether cloned lists can be modified independently.
			
			int changeIndex = -1, changeItem = -1, sizeChange = 0;
			ListChangingHandler<T> changeHandler = (sender, args) =>
			{
				Assert.AreEqual(sizeChange, args.SizeChange);
				sizeChange = 0;
				Assert.AreEqual(changeIndex, args.Index);

				// Note: change notifications are sent before the add/remove happens
				if (sizeChange > 0)
				{
					Assert.AreEqual(NotifyCollectionChangedAction.Add, args.Action);
					Assert.That(args.NewItems != null && args.NewItems.Count == 1);
					Assert.AreEqual(changeItem, GetKey(args.NewItems[0]));
				}
				else if (sizeChange < 0) 
				{
					Assert.AreEqual(NotifyCollectionChangedAction.Remove, args.Action);
					Assert.AreEqual(changeItem, GetKey(sender[args.Index]));
				}
			};
			alist1.ListChanging += changeHandler;
			alist2.ListChanging += changeHandler;

			int nextNew = 1000;
			int i = 0;
			while (alist1.Count > 0)
			{
				// Modify first list
				sizeChange += -1;
				changeItem = GetKey(alist1[changeIndex = _r.Next(alist1.Count)]);
				RemoveAtInBoth(alist1, list1, changeIndex);

				// Modify second list
				sizeChange += 1;
				AddToBoth(alist2, list2, changeItem = nextNew++, changeIndex = alist2.Count);
				sizeChange += -1;
				changeItem = GetKey(alist2[changeIndex = _r.Next(alist2.Count)]);
				RemoveAtInBoth(alist2, list2, changeIndex);

				// verify results at every power of 2
				if (MathEx.CountOnes(++i) == 1 || alist1.Count == 0)
				{
					ExpectList(alist1, list1, (i & 1) == 0);
					ExpectList(alist2, list2, (i & 1) == 0);
				}
			}

			// Call changeHandler again to verify that all notifications were sent
			changeIndex = 0;
			changeHandler(alist1, new ListChangeInfo<T>(NotifyCollectionChangedAction.Reset, 0, 0, null));
		}

		[Test]
		public void CheckThatCloneIsUnfrozen()
		{
			// A clone of an AListBase is not frozen like the original.
			List<T> list;
			AList alist1 = NewList(100, out list);
			
			alist1.Freeze();
			AList alist2 = alist1.Clone();
			
			alist2.RemoveAt(alist2.Count - 10);
			ExpectList(alist1, list, false);
			list.RemoveAt(list.Count - 10);
			ExpectList(alist2, list, false);
			AssertThrows<ReadOnlyException>(() => { alist1.RemoveAt(0); });
		}

		[Test]
		public void TestCopySection()
		{
			List<T> list;
			AList alist = NewList(10, out list);
			
			for (int trial = 0; trial < 15; trial++)
			{
				int start = _r.Next(alist.Count - 1);
				int count = _r.Next(alist.Count - start);
				AList sec = CopySection(alist, start, count);
				for (int s = 0; s < sec.Count; s++)
					Assert.AreEqual(sec[s], list[start + s]);
				
				if (trial == 5)
					alist = NewList(100, out list); // try a bigger list
				if (trial == 10)
					alist.Freeze(); // Shouldn't make any difference
			}
		}

		[Test]
		public void ListChangingExceptionVetoesChanges()
		{
			List<T> list;
			AList alist = NewList(100, out list);

			// ListChanging could throw an exception, which blocks changes.
			// We are also testing that alist doesn't go into an invalid state.
			ListChangingHandler<T> veto = (sender, args) => { throw new ApplicationException(); };
			alist.ListChanging += veto;
			AssertThrows<ApplicationException>(() => alist.RemoveAt(10));
			AssertThrows<ApplicationException>(() => alist.RemoveRange(10, 80));
			ExpectList(alist, list, false);
			AssertThrows<ApplicationException>(() => AddToBoth(alist, list, 10, 10));
			ExpectList(alist, list, true);
			
			// We can remove the veto and then changes should succeed.
			alist.ListChanging -= veto;
			RemoveAtInBoth(alist, list, 10);
			ExpectList(alist, list, false);
			AddToBoth(alist, list, 10, 10);
			ExpectList(alist, list, true);
		}

		[Test]
		public void ReverseViewTest()
		{
			List<T> list;
			AList alist = NewList(100, out list);
			list.Reverse();
			ExpectList(alist.ReverseView, list, false);
			ExpectList(alist.ReverseView, list, true);
		}

		[Test]
		public void RemoveRangeTest()
		{
			List<T> list;
			AList alist = NewList(1000, out list);

			for (int trial = 0; alist.Count > 0; trial++)
			{
				int start = _r.Next(alist.Count - 1);
				int count = Math.Min(_r.Next(100), alist.Count - start);
				alist.RemoveRange(start, count);
				list.RemoveRange(start, count);
				ExpectList(alist, list, (trial & 1) == 0);
			}
		}

		[Test]
		public void RemoveSectionTest()
		{
			List<T> list;
			AList alist = NewList(1000, out list);

			for (int trial = 0; alist.Count > 0; trial++)
			{
				int start = _r.Next(alist.Count - 1);
				int count = Math.Min(_r.Next(100), alist.Count - start);
				
				AList sec = RemoveSection(alist, start, count);
				for (int i = 0; i < sec.Count; i++)
					Assert.AreEqual(list[start + i], sec[i]);
				list.RemoveRange(start, count);

				ExpectList(alist, list, (trial & 1) == 0);
				
				if ((trial & 1) == 0)
					sec.Freeze(); // should have no effect
			}
		}

		[Test]
		public void TestClear()
		{
			List<T> list;
			int sizeChange = 0;
			ListChangingHandler<T> clearCheck = (sender, args) =>
			{
				Assert.AreEqual(NotifyCollectionChangedAction.Remove, args.Action);
				Assert.AreEqual(0, args.Index);
				sizeChange += args.SizeChange;
			};

			AList alist = NewList(1, out list);
			alist.ListChanging += clearCheck;
			alist.Clear();
			ExpectList(alist);

			alist = NewList(10, out list);
			alist.ListChanging += clearCheck;
			alist.Clear();
			ExpectList(alist);

			alist = NewList(100, out list);
			alist.ListChanging += clearCheck;
			alist.Clear();
			ExpectList(alist);

			Assert.AreEqual(-111, sizeChange);
		}

		[Test]
		public void TestConcurrencyException()
		{
			List<T> list;
			AList alist = NewList(100, out list);
			int workerOp = 0;
			Exception error = null;

			// The plan: do a series of editing operations and make sure that we 
			// can get a ConcurrentModificationException from doing read operations
			// on another thread at the same time. It would be nice to test 
			// simultaneous writing on two threads, but concurrent access is not 
			// guaranteed to throw EVERY TIME so maybe the list could end up in a 
			// bizarre state whose behavior is undefined, so I'm not sure how to 
			// test that.
			for (;;)
			{
				// Start a thread
				ThreadEx thread = new ThreadEx(() =>
				{
					try {
						error = null;
						T tmp;
						
						++workerOp;
						while(workerOp != -1)
						{
							// Do one of a few read operations. All of them should throw,
							// but it may take a few attempts for them to throw during a
							// modification in progress on the main thread.
							// (Note: for performance reasons, Count does not throw.)
							switch(workerOp)
							{
								case 1: tmp = alist.First; break;
								case 2: tmp = alist.Last; break;
								case 3: alist.GetEnumerator().MoveNext(); break;
								case 4: tmp = alist[0]; break;
								default: workerOp = -1; break;
							}
						}
					} catch (Exception e) {
						error = e;
					}
				});
				thread.Start();

				var timer = new SimpleTimer();
				while(thread.IsAlive)
				{
					AddToBoth(alist, list, 999, list.Count);
					RemoveAtInBoth(alist, list, list.Count-1);
					Assert.That(timer.Millisec < 5000);
				}
				
				if (workerOp == -1)
					break; // done

				Assert.IsInstanceOfType(typeof(ConcurrentModificationException), error);
			}
		}
	}
}
