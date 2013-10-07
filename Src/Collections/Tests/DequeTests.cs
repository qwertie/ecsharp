using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Loyc.Collections
{
	[TestFixture]
	public class DequeTests<ListT> where ListT : IDeque<int>, ICloneable<ListT>
	{
		protected Func<ListT> _newDeque;
		protected int _randomSeed;
		protected Random _r;
		protected int Iterations = 100;
		protected int StressTestIterations = 1000;

		public DequeTests(Func<ListT> newEmptyDeque) : this(newEmptyDeque, Environment.TickCount) { }
		public DequeTests(Func<ListT> newEmptyDeque, int randomSeed)
		{
			_r = new Random(_randomSeed = randomSeed);
			_newDeque = newEmptyDeque;
		}

		[Test]
		public void TestPushPeekFirst()
		{
			ListT list = _newDeque();
			bool isEmpty;
			int value;
			// Try the extension methods too
			Assert.That(list.TryPeekFirst(out isEmpty) == 0 && isEmpty);
			Assert.That(!list.TryPeekFirst(out value) && value == 0);
			Assert.AreEqual(-5, list.TryPeekFirst(-5));
			Assert.AreEqual(0, list.TryPeekFirst());

			for (int i = 1; i <= Iterations; i++)
			{
				list.PushFirst(i);

				Assert.AreEqual(i, list.Count);
				Assert.AreEqual(i, list.First);
				Assert.AreEqual(i, list.TryPeekFirst());
				Assert.AreEqual(i, list.TryPeekFirst(-5));
				Assert.That(list.TryPeekFirst(out isEmpty) == i && !isEmpty);
				Assert.That(list.TryPeekFirst(out value) && value == i);
			}
		}

		[Test]
		public void TestPushPeekLast()
		{
			ListT list = _newDeque();
			bool isEmpty;
			int value;
			// Try the extension methods too
			Assert.That(list.TryPeekLast(out isEmpty) == 0 && isEmpty);
			Assert.That(!list.TryPeekLast(out value) && value == 0);
			Assert.AreEqual(-5, list.TryPeekLast(-5));
			Assert.AreEqual(0, list.TryPeekLast());

			for (int i = 1; i <= Iterations; i++)
			{
				list.PushLast(i);

				Assert.AreEqual(i, list.Count);
				Assert.AreEqual(i, list.Last);
				Assert.AreEqual(i, list.TryPeekLast());
				Assert.AreEqual(i, list.TryPeekLast(-5));
				Assert.That(list.TryPeekLast(out isEmpty) == i && !isEmpty);
				Assert.That(list.TryPeekLast(out value) && value == i);
			}
		}

		[Test]
		public void TestPushPopLast()
		{
			ListT list = _newDeque();
			bool isEmpty;
			int value;
			// Try the extension methods too
			Assert.That(list.TryPopLast(out isEmpty) == 0 && isEmpty);
			Assert.That(!list.TryPopLast(out value) && value == 0);
			Assert.AreEqual(-5, list.TryPopLast(-5));
			Assert.AreEqual(0, list.TryPopLast());

			for (int i = 1; i <= Iterations; i++)
			{
				list.PushLast(i);

				if ((i & 1) != 0)
				{
					Assert.AreEqual(i, list.TryPopLast());
					list.PushLast(i);
					Assert.AreEqual(i, list.TryPopLast(-5));
					list.PushLast(i);
					Assert.That(list.TryPopLast(out isEmpty) == i && !isEmpty);
					list.PushLast(i);
					Assert.That(list.TryPopLast(out value) && value == i);
				}
				Assert.AreEqual(i >> 1, list.Count);
			}
			while (list.Count > 0)
				Assert.AreEqual(list.Count * 2, list.PopLast());
		}

		[Test]
		public void TestPushPopFirst()
		{
			ListT list = _newDeque();
			bool isEmpty;
			int value;
			// Try the extension methods too
			Assert.That(list.TryPopFirst(out isEmpty) == 0 && isEmpty);
			Assert.That(!list.TryPopFirst(out value) && value == 0);
			Assert.AreEqual(-5, list.TryPopFirst(-5));
			Assert.AreEqual(0, list.TryPopFirst());

			for (int i = 1; i <= Iterations; i++)
			{
				list.PushFirst(i);

				if ((i & 1) != 0)
				{
					Assert.AreEqual(i, list.TryPopFirst());
					list.PushFirst(i);
					Assert.AreEqual(i, list.TryPopFirst(-5));
					list.PushFirst(i);
					Assert.That(list.TryPopFirst(out isEmpty) == i && !isEmpty);
					list.PushFirst(i);
					Assert.That(list.TryPopFirst(out value) && value == i);
				}
				Assert.AreEqual(i >> 1, list.Count);
			}
			while (list.Count > 0)
				Assert.AreEqual(list.Count * 2, list.PopFirst());
		}
		
		[Test]
		public void StressTest()
		{
			ListT list = _newDeque();
			int min = 1, max = 0;

			// Do a random series of pushes and pops.
			int i;
			for (i = 0; i < StressTestIterations || list.Count > 0; i++)
			{
				Assert.AreEqual(max+1-min, list.Count);
				Assert.AreEqual(max+1==min, list.IsEmpty);

				int op = _r.Next(i >= StressTestIterations ? 2 : 6);
				if (op < 2) {
					if (!list.IsEmpty) {
						if (op == 0)
							Assert.AreEqual(min++, list.PopFirst());
						else
							Assert.AreEqual(max--, list.PopLast());
					}
				} else {
					if (op < 4) {
						list.PushFirst(--min);
						Assert.AreEqual(min, list.First);
					} else {
						list.PushLast(++max);
						Assert.AreEqual(max, list.Last);
					}
				}
			}
		}
	}
}
