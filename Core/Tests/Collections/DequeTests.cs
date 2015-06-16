using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;

namespace Loyc.Collections.Tests
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

			for (int i = 1; i <= Iterations; i++)
			{
				list.PushFirst(i);

				Assert.AreEqual(i, list.Count);
				Assert.AreEqual(i, list.First);
				Assert.AreEqual(i, list.PeekFirst());
				Assert.AreEqual(i, list.TryPeekFirst().Value);
				Assert.AreEqual(i, list.TryPeekFirst().Or(-5));
			}
		}

		[Test]
		public void TestPushPeekLast()
		{
			ListT list = _newDeque();
			// Try the extension methods too
			Assert.IsFalse(list.TryPeekLast().HasValue);
			Assert.AreEqual(-5, list.TryPeekLast().Or(-5));

			for (int i = 1; i <= Iterations; i++)
			{
				list.PushLast(i);

				Assert.AreEqual(i, list.Count);
				Assert.AreEqual(i, list.Last);
				Assert.That(list.TryPeekLast().HasValue);
				Assert.AreEqual(i, list.TryPeekLast().Value);
			}
		}

		[Test]
		public void TestPushPopLast()
		{
			ListT list = _newDeque();
			// Try the extension methods too
			Assert.IsFalse(list.TryPopLast().HasValue);

			for (int i = 1; i <= Iterations; i++)
			{
				list.PushLast(i);
				if ((i & 1) != 0)
					Assert.AreEqual(i, list.TryPopLast().Value);
				Assert.AreEqual(i >> 1, list.Count);
			}
			while (list.Count > 0)
				Assert.AreEqual(list.Count * 2, list.PopLast());
		}

		[Test]
		public void TestPushPopFirst()
		{
			ListT list = _newDeque();
			// Try the extension methods too
			Assert.IsFalse(list.TryPopFirst().HasValue);

			for (int i = 1; i <= Iterations; i++)
			{
				list.PushFirst(i);
				if ((i & 1) != 0)
					Assert.AreEqual(i, list.TryPopFirst().Or(-5));
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
