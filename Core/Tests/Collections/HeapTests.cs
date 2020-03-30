using Loyc.Collections.Impl;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections.Tests
{
	[TestFixture]
	public class HeapTests : TestHelpers
	{
		Random _r;
		public HeapTests(int seed = 1234) { _r = new Random(seed); }

		[Test]
		public void TestPopMax()
		{
			var heap = new InternalList<int>(new[] { 10, 9, 6, 8, 7, 1, 0, 0 }, 6).AsMaxHeap();
			Assert.AreEqual(10, heap.Pop());
			Assert.AreEqual(9, heap.Pop());
			Assert.AreEqual(8, heap.Pop());
			Assert.AreEqual(7, heap.Pop());
			Assert.AreEqual(6, heap.Pop());
			Assert.AreEqual(1, heap.Pop());
			Assert.ThrowsAny<EmptySequenceException>(() => heap.Pop());
		}

		[Test]
		public void TestPopMin()
		{
			var heap = new InternalList<int>(new[] { 0, 1, 5, 6, 1, 11, 9, 0 }, 7).AsMinHeap();
			Assert.AreEqual(0, heap.Pop());
			Assert.AreEqual(1, heap.Pop());
			Assert.AreEqual(1, heap.Pop());
			Assert.AreEqual(5, heap.Pop());
			Assert.AreEqual(6, heap.Pop());
			Assert.AreEqual(9, heap.Pop());
			Assert.AreEqual(11, heap.Pop());
			Assert.ThrowsAny<EmptySequenceException>(() => heap.Pop());
		}

		[Test]
		public void TestMaxHeapAdd()
		{
			// Add is a synonym for Push and `new Xyz { ... }` calls Add repeatedly.
			var heap = new MaxHeap<int> { 25, 19, 36, 17, 3, 100, 1, 2, 7 };
			//         100
			//    19        36
			// 17    3    25   1
			//2  7
			ExpectList(heap.List, 100, 19, 36, 17, 3, 25, 1, 2, 7);
		}
		[Test]
		public void TestMinHeapAdd()
		{
			// Add is a synonym for Push and `new Xyz { ... }` calls Add repeatedly.
			var heap = new MinHeap<int> { 25, 19, 36, 17, 3, 100, 1, 2, 7 };
			//        1     
			//    2        3
			//  7   19   100  36
			//25 17
			ExpectList(heap.List, 1, 2, 3, 7, 19, 100, 36, 25, 17);
		}

		[Test]
		public void TestMaxHeapify()
		{
			var heap = new[] { 1, 3, 36, 2, 19, 25, 100, 17, 7 }.ToMaxHeap();
			ExpectList(heap.List, 100, 19, 36, 17, 3, 25, 1, 2, 7);
		}

		[Test]
		public void TestMinHeapify()
		{
			//          1
			//     3        36
			//   2   19   25  100
			// 17 7
			var heap = new[] { 1, 3, 36, 2, 19, 25, 100, 17, 7 }.ToMinHeap();
			//           1
			//      2        25
			//   3    19   36  100
			// 17 7
			ExpectList(heap.List, 1, 2, 25, 3, 19, 36, 100, 17, 7);
		}

		[Test]
		public void TestPopAndPush()
		{
			var heap = new[] { 10, 20, 30, 40, 50, 60 }.ToMinHeap();
			AreEqual(10, heap.PopAndPush(25));
			ExpectList(heap.List, 20, 25, 30, 40, 50, 60);
			AreEqual(20, heap.PopAndPush(65));
			ExpectList(heap.List, 25, 40, 30, 65, 50, 60);
		}

		class Item : IComparable<Item>
		{
			public int Index;
			public int Priority;
			public Item(int p) { Priority = p; }
			public int CompareTo(Item other) { return Priority.CompareTo(other.Priority); }
		}

		[Test]
		public void TestItemMoveDetection()
		{
			var heap = new MaxHeap<Item>(null, (item, index) => item.Index = index);
			Action Check = () => {
				for (int i = 0; i < heap.Count; i++)
					AreEqual(i, heap.List[i].Index);
			};

			// Add a bunch of items (probability of a collision is very nearly zero)
			for (int i = 0; i < 50; i++) {
				heap.Push(new Item(_r.Next()));
				Check();
			}
			// Change priority of each item
			for (int index = 0; index < heap.Count; index++) {
				var item = heap.List[index];
				AreEqual(index, item.Index);
				item.Priority = _r.Next();
				heap.PriorityChanged(index);
				Check();
			}
			// Remove each item
			Item prev = null;
			while (!heap.IsEmpty) {
				var item = heap.Pop();
				AreEqual(item.Index, -1);
				if (prev != null)
					Less(item.Priority, prev.Priority);
				prev = item;
			}
		}
	}
}
