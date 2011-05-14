namespace Loyc.Collections
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using Loyc.Collections;
	using NUnit.Framework;
	using Loyc.Collections.Linq;
	using Loyc.Essentials;

	[TestFixture]
	public class ListRangeTests<ListT> where ListT : IGetIteratorSlice<int>, IInsertRemoveRange<int>, ICloneable<ListT>
	{
		protected Func<ListT> _newList;
		protected int _randomSeed;
		protected Random _r;
		protected bool _testExceptions;

		public ListRangeTests(bool testExceptions, Func<ListT> newList) : this(testExceptions, newList, Environment.TickCount) { }
		public ListRangeTests(bool testExceptions, Func<ListT> newList, int randomSeed)
		{
			_testExceptions = testExceptions;
			_r = new Random(_randomSeed = randomSeed);
			_newList = newList;
		}

		[Test]
		public void TestAddRange()
		{
			var list = _newList();
			list.AddRange(Iterable.Range(1, 3));
			list.AddRange(Enumerable.Range(4, 3));
			ExpectList(list, 1, 2, 3, 4, 5, 6);
			list.AddRange(Iterable.Range(7, 2));
			list.AddRange(Enumerable.Range(9, 2));
			ExpectList(list, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
			list.AddRange(Iterable.Single(11));
			list.AddRange(Enumerable.Range(12, 1));
			ExpectList(list, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
			list.AddRange(Iterable.Repeat(0, 0));
			list.AddRange(Enumerable.Repeat(0, 0));
			ExpectList(list, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
		}

		protected static void ExpectList<T>(IEnumerable<T> list, params T[] expected)
		{
			ExpectList(list, (IList<T>)expected);
		}
		protected static void ExpectList<T>(Iterator<T> it, params T[] expected)
		{
			ExpectList(it, (IList<T>)expected);
		}
		protected static void ExpectList<T>(IEnumerable<T> list, IList<T> expected)
		{
			int i = 0;
			foreach (T item in list)
			{
				Assert.Less(i, expected.Count);
				Assert.AreEqual(expected[i], item);
				i++;
			}
			Assert.AreEqual(expected.Count, i);
		}
		protected static void ExpectList<T>(Iterator<T> it, IList<T> expected)
		{
			bool ended = false;
			T next;
			for (int i = 0; i < expected.Count; i++) {
				next = it(ref ended);
				Assert.IsFalse(ended);
				Assert.AreEqual(expected[i], next);
			}
			next = it(ref ended);
			Assert.That(ended);
		}

		[Test]
		public void TestInsertRange()
		{
			ListT list = _newList();
			List<int> list2 = new List<int>();

			int amount;
			int iteration = 0;
			for (int i = 0; i < 5000; i += amount)
			{
				Assert.AreEqual(list.Count, list2.Count);
				
				amount = _r.Next(100);
				int at = _r.Next(list.Count + 1);
				var e = Enumerable.Range(i, amount);
				list2.InsertRange(at, e);
				if (_r.Next(5) > 0) {
					list.InsertRange(at, e);
				} else {
					var temp = new List<int>(e);
					list.InsertRange(at, temp);
				}
				if (++iteration <= 5 || (iteration % 10) == 0)
					ExpectList(list, list2);
			}
			ExpectList(list, list2);
			ExpectList(list.GetIterator(), list2);
		}

		[Test]
		public void TestRemoveRange()
		{
			var e = Enumerable.Range(-1, 5000);
			ListT list = _newList();
			List<int> list2 = new List<int>(e);
			list.AddRange(e);

			for (int iteration = 0; list2.Count > 0; iteration++)
			{
				int at = _r.Next(list2.Count+1);
				int amount = Math.Min(list2.Count - at, _r.Next(100));

				list.RemoveRange(at, amount);
				list2.RemoveRange(at, amount);
				
				if (iteration % 10 == 0)
					ExpectList(list, list2);
			}

			ExpectList(list, list2);
			ExpectList(list.GetIterator(), list2);
		}

		[Test]
		public void TestIterateRange()
		{
			ListT list = _newList();
			list.AddRange(Enumerable.Range(0, 5000));

			for (int i = 0; i < 100; i++)
			{
				int at = _r.Next(list.Count+1);
				int amount = _r.Next(100);
				ExpectList(list.GetIterator(at, amount), 
					Enumerable.Range(at, Math.Min(amount, list.Count - at)).ToArray());
			}
		}

		protected int StressTestIterations = 1000;
		protected int MaxListSize = 10000;

		[Test]
		public void StressTest()
		{
			ListT list = _newList();
			List<int> list2 = new List<int>();

			for (int i = 0; i < StressTestIterations; i++)
			{
				Assert.AreEqual(list.Count, list2.Count);
				int at = _r.Next(list.Count + 1);
				int amount = _r.Next(30) * _r.Next(30); // parabolic distribution
				int act = _r.Next(3);

				if (act == 0 && list.Count < MaxListSize)
				{
					IEnumerable<int> e = Enumerable.Range(i * 1000, amount);
					list.InsertRange(at, e);
					list2.InsertRange(at, e);
				}
				else if (act == 2)
				{
					while (act != 0 && amount > list.Count - at)
						amount /= 2;
					list.RemoveRange(at, amount);
					list2.RemoveRange(at, amount);
				}
				else
				{
					int amount2 = Math.Min(amount, list.Count - at);
					ExpectList(list.GetIterator(at, amount), 
						list2.AsListSource().Slice(at, amount2).AsList());
				}
			}
		}
	}
}
