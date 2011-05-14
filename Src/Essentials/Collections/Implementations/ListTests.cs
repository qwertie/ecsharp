using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Loyc.Essentials;

namespace Loyc.Collections
{
	[TestFixture]
	public class ListTests<ListT> where ListT : IList<int>, ICloneable<ListT>
	{
		protected Func<int, ListT> _newList;
		protected int _randomSeed;
		protected Random _r;
		protected bool _testExceptions;

		public ListTests(bool testExceptions, Func<int, ListT> newListWithSize) : this(testExceptions, newListWithSize, Environment.TickCount) { }
		public ListTests(bool testExceptions, Func<int, ListT> newListWithSize, int randomSeed)
		{
			_testExceptions = testExceptions;
			_r = new Random(_randomSeed = randomSeed);
			_newList = newListWithSize;
		}

		[Test]
		public void TestAdd()
		{
			ListT list = _newList(0);
			for (int i = 0; i < 128; i++)
			{
				list.Add(i);
				for (int j = 0; j < i; j++)
					Assert.AreEqual(j, list[j]);
			}
		}

		[Test]
		public void TestInsert()
		{
			ListT list = _newList(0);
			List<int> list2 = new List<int>();
			for (int i = 0; i < 128; i++) {
				int j = _r.Next(i + 1);
				list.Insert(j, i);
				list2.Insert(j, i);
				if ((i & (i - 1)) == 0) // check every power of 2
					ExpectList(list, list2.ToArray());
			}
			ExpectList(list, list2.ToArray());
		}

		[Test]
		public void TestRemoveAt()
		{
			const int InitialCount = 128;
			Assert.AreEqual(InitialCount / 4 * 4, InitialCount); // Multiple of 4
			int i, j;
			
			ListT list = _newList(InitialCount);
			Assert.AreEqual(InitialCount, list.Count);
			for (i = 0; i < list.Count; i++)
				list[i] = i;

			for (i = 0; i < list.Count; i++)
			{
				Assert.AreEqual(i * 2, list[i]);
				list.RemoveAt(i);
				Assert.AreEqual(i * 2 + 1, list[i]);
			}
			for (i = 0; i < list.Count; i++)
				Assert.AreEqual(i * 2 + 1, list[i]);

			Assert.AreEqual(InitialCount / 2, list.Count);
			i = 1;
			j = list.Count * 2 - 1;
			while (list.Count > 0)
			{
				Assert.AreEqual(j, list[list.Count-1]);
				list.RemoveAt(list.Count - 1);
				j -= 2;
				
				Assert.AreEqual(i, list[0]);
				list.RemoveAt(0);
				i += 2;
			}
			ExpectList(list);
		}

		[Test]
		public void TestEnumerator()
		{
			ListT list = _newList(StressTestIterations);
			int i;
			for (i = 0; i < list.Count; i++)
				list[i] = i;
			IEnumerator<int> e = list.GetEnumerator();
			for (i = 0; e.MoveNext(); i++)
				Assert.AreEqual(i, e.Current);
			Assert.AreEqual(list.Count, i);
			Assert.AreEqual(list.Count, StressTestIterations);
		}

		[Test]
		public void BasicWorkout()
		{
			ListT list = _newList(0);
			Assert.AreEqual(0, list.Count);
			Assert.That(!list.IsReadOnly);

			// Add(), RemoveAt(), Clone()
			list.Add(1);
			list.Add(2);
			Assert.That(!list.IsReadOnly);
			ExpectList(list, 1, 2);
			var list2 = list.Clone();
			list2.RemoveAt(1);
			list2.Add(3);
			ExpectList(list, 1, 2);
			ExpectList(list2, 1, 3);
			list.Add(4);
			ExpectList(list, 1, 2, 4);

			// Indexer, Insert(), GetEnumerator()
			list2.Insert(list2.Count, list2[0]);
			list2.Insert(list2.Count, list2[1]);
			ExpectList(list2, 1, 3, 1, 3);
			for (int i = 0; i < list.Count; i++)
				list2.Insert(i, list[i]);
			ExpectList(list2, 1, 2, 4, 1, 3, 1, 3);
			ExpectListByEnumerator(list2, 1, 2, 4, 1, 3, 1, 3);

			// Clear list with Remove()
			Assert.That(list.Remove(1));
			ExpectList(list, 2, 4);
			Assert.That(list.Remove(2));
			Assert.That(list.Remove(4));
			Assert.That(list.Count == 0);
			Assert.That(!list.Remove(2));
			Assert.That(!list.Remove(4));
		}

		[Test]
		public void BasicWorkout2()
		{
			var list = _newList(0);
			ExpectListByEnumerator(list);

			list = _newList(10);
			for (int i = 0; i < 10; i++)
				list[i] = (i+1)*2;
			ExpectListByEnumerator(list, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20);
			
			// CopyTo()
			var array = new int[list.Count+2];
			list.CopyTo(array, 2);
			ExpectList(array, 0, 0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20);

			// IndexOf(), Contains()
			Assert.That(list.Contains(2));
			Assert.That(list.IndexOf(5) == -1);
			Assert.That(list.IndexOf(4) == 1);
			Assert.That(list.IndexOf(20) == 9);
			Assert.That(!list.Contains(3));
			Assert.That(list[list.IndexOf(2)] == 2);
			Assert.That(list[list.IndexOf(10)] == 10);
			Assert.That(list[list.IndexOf(20)] == 20);

			// Clear list with Remove()
			Assert.That(list.Remove(2));
			Assert.That(list.Remove(20));
			Assert.That(!list.Remove(20));
			Assert.That(!list.Remove(2));
			for (int i = 4; i <= 19; i++)
				Assert.AreEqual((i & 1) == 0, list.Remove(i));

			if (_testExceptions)
				AssertThrows<IndexOutOfRangeException>(delegate() { int x = list[0]; });

			// Equals(), GetHashCode(), Clear()
			Assert.That(!list.Equals("hello"));
			Assert.That(list.Equals(list));
			int hashCode = list.GetHashCode();
			list.Clear();
			foreach (int i in list)
				Assert.Fail("List not empty after Clear()");
			Assert.AreEqual(0, list.Count);

			if (_testExceptions)
				// Not really an exception test, but _testExceptions==false for
				// InternalList<int>, which cannot provide a consistent hashcode.
				Assert.AreEqual(hashCode, list.GetHashCode());
		}

		protected void AssertThrows<Type>(TestDelegate @delegate)
		{
			try {
				@delegate();
			} catch (Exception exc) {
				Assert.IsInstanceOf<Type>(exc);
				return;
			}
			Assert.Fail("Delegate did not throw '{0}' as expected.", typeof(Type).Name);
		}

		protected static void ExpectList<T>(IList<T> list, params T[] expected)
		{
			Assert.AreEqual(expected.Length, list.Count);
			for (int i = 0; i < expected.Length; i++)
				Assert.AreEqual(expected[i], list[i]);
		}
		protected static void ExpectListByEnumerator<T>(IList<T> list, params T[] expected)
		{
			Assert.AreEqual(expected.Length, list.Count);
			int i = 0;
			foreach (T item in list) {
				Assert.AreEqual(expected[i], item);
				i++;
			}
		}

		protected int StressTestIterations = 1000;

		[Test]
		public void StressTest()
		{
			ListT list = _newList(0), clone = default(ListT);
			List<int> list2 = new List<int>();
			int[] clone2 = null;

			// Do a series of Insert, Add, Remove and RemoveAt operations to both 
			// lists, and ensure that we get the same results.
			int i;
			for (i = 0; i < StressTestIterations; i++)
			{
				StressTestIteration(ref list, list2, i);

				if ((i & (i - 1)) == 0) // when i is a power of 2
				{
					ExpectList(list, list2.ToArray());
					ExpectListByEnumerator(list, list2.ToArray());
					
					if (_testExceptions)
						AssertThrows<IndexOutOfRangeException>(delegate() { list.RemoveAt(list.Count); });
				}
				if (i == StressTestIterations/2)
				{
					clone = list.Clone();
					clone2 = list2.ToArray();
					ExpectList(list, clone2);
				}
			}

			i = 0;
			ExpectList(clone, clone2);
			ExpectListByEnumerator(clone, clone2);
		}

		// Note: list is passed by reference in case ListT is a value type (InternalList)
		protected virtual void StressTestIteration(ref ListT list, List<int> list2, int i)
		{
			int n = _r.Next(list2.Count + 1);
			list.Insert(n, i);
			list2.Insert(n, i);
			list.Add(i);
			list2.Add(i);

			Assert.AreEqual(list2.Count, list.Count);

			n = _r.Next(i * 2);
			Assert.AreEqual(list.IndexOf(n), list2.IndexOf(n));
			Assert.AreEqual(list.Remove(n), list2.Remove(n));

			if (n < list2.Count)
			{
				list.RemoveAt(n);
				list2.RemoveAt(n);
			}
		}
	}
}
