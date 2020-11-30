using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.Collections.Tests;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Essentials.Tests
{
	public class ListWithChangeEventsForTesting<T> : ListWithChangeEvents<T, List<T>>, ICloneable<ListWithChangeEventsForTesting<T>>
	{
		public ListWithChangeEventsForTesting() : base(new List<T>()) { }
		public ListWithChangeEventsForTesting(List<T> obj) : base(obj) { }

		public ListWithChangeEventsForTesting<T> Clone() => new ListWithChangeEventsForTesting<T>(new List<T>(_obj));
	}

	public class ListWithChangeEventsTests : ListTests<ListWithChangeEventsForTesting<int>>
	{
		public ListWithChangeEventsTests(int randomSeed)
			: base(true, size => new ListWithChangeEventsForTesting<int>().With(l => l.Resize(size)), randomSeed) { }

		static int[] A(params int[] a) => a;

		// Modification checklist: Add, Clear, Insert, Remove, RemoveAt, AddRange, InsertRange, RemoveRange, indexer

		[Test]
		public void TestEvents1()
		{
			// Build list { 5, 6 }, then clear
			var list = _newList(0);
			CheckListChanging(list, l => l.Add(5), 0, A(5), A());
			CheckListChanged(list, l => l.Add(6), 1, A(6), A(), 5, 6);
			CheckListChanging(list, l => l.Clear(), 0, A(), A(5, 6), 5, 6);
			CheckNoEventOccurs(list, l => l.Clear());
			CheckNoEventOccurs(list, l => IsFalse(l.Remove(5)));
		}

		[Test]
		public void TestEvents2()
		{
			// Build list { 5, 6, 7, 8, 9 }, then remove 5, 6, 8, 9 and clear
			var list = _newList(0);
			CheckListChanged(list, l => l.Insert(0, 5), 0, A(5), A(), 5);
			CheckNoEventOccurs(list, l => ThrowsAny<SystemException>(() => l.Insert(-1, 6)));
			CheckNoEventOccurs(list, l => ThrowsAny<SystemException>(() => l.Insert(2, 6)));
			CheckListChanging(list, l => l.Insert(1, 7), 1, A(7), A(), 5);
			CheckListChanging(list, l => l.Insert(2, 8), 2, A(8), A(), 5, 7);
			CheckListChanged(list, l => l.Insert(1, 6), 1, A(6), A(), 5, 6, 7, 8);
			CheckListChanged(list, l => l.Insert(4, 9), 4, A(9), A(), 5, 6, 7, 8, 9);
			CheckNoEventOccurs(list, l => ThrowsAny<SystemException>(() => l.RemoveAt(-1)));
			CheckNoEventOccurs(list, l => ThrowsAny<SystemException>(() => l.RemoveAt(5)));
			CheckListChanging(list, l => l.RemoveAt(1), 1, A(), A(6), 5, 6, 7, 8, 9);
			CheckListChanged(list, l => l.RemoveAt(0), 0, A(), A(5), 7, 8, 9);
			CheckListChanging(list, l => IsTrue(l.Remove(8)), 1, A(), A(8), 7, 8, 9);
			CheckListChanged(list, l => IsTrue(l.Remove(9)), 1, A(), A(9), 7);
			CheckNoEventOccurs(list, l => IsFalse(l.Remove(9)));
			CheckListChanged(list, l => l.Clear(), 0, A(), A(7));
		}

		[Test]
		public void TestEvents3()
		{
			// Build list { 0, 1, 2, 3, 4, 5, 6 } with AddRange, InsertRange, RemoveRange, TrySet, and indexer
			var list = _newList(0);
			CheckListChanging(list, l => l.AddRange(A(4, 5, 6)), 0, A(4, 5, 6), A());
			CheckListChanged(list, l => l.AddRange(A(9, 10)), 3, A(9, 10), A(), 4, 5, 6, 9, 10);
			CheckNoEventOccurs(list, l => ThrowsAny<SystemException>(() => l.InsertRange(-1, A(-1))));
			CheckNoEventOccurs(list, l => ThrowsAny<SystemException>(() => l.InsertRange(6, A())));
			CheckNoEventOccurs(list, l => l.InsertRange(0, A()));
			CheckListChanging(list, l => l.InsertRange(3, A(7, 8)), 3, A(7, 8), A(), 4, 5, 6, 9, 10);
			CheckListChanged(list, l => l.InsertRange(0, A(3, 2, 1)), 0, A(3, 2, 1), A(), 3, 2, 1, 4, 5, 6, 7, 8, 9, 10);
			CheckListChanging(list, l => l[0] = 1, 0, A(1), A(3), 3, 2, 1, 4, 5, 6, 7, 8, 9, 10);
			CheckListChanged(list, l => l[2] = 3, 2, A(3), A(1), 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
			CheckListChanged(list, l => l.InsertRange(0, A(0)), 0, A(0), A(), 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
			CheckNoEventOccurs(list, l => l.RemoveRange(0, 0));
			CheckNoEventOccurs(list, l => l.RemoveRange(11, 0));
			CheckNoEventOccurs(list, l => l.RemoveRange(11, 11));
			CheckNoEventOccurs(list, l => ThrowsAny<SystemException>(() => l.RemoveRange(-1, 2)));
			CheckNoEventOccurs(list, l => l.RemoveRange(12, 2));
			CheckListChanging(list, l => l.RemoveRange(10, 999), 10, A(), A(10), 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
			CheckListChanged(list, l => l.RemoveRange(6, 3), 6, A(), A(6, 7, 8), 0, 1, 2, 3, 4, 5, 9);
			CheckNoEventOccurs(list, l => IsFalse(l.TrySet(-1, -1)));
			CheckListChanged(list, l => IsTrue(l.TrySet(6, 6)), 6, A(6), A(9), 0, 1, 2, 3, 4, 5, 6);
		}

		private void CheckNoEventOccurs<List>(List list, Action<List> act) where List : IListWithChangeEvents<int>
				=> CheckListChange(list, act, -1, _r.Next(2) == 0, false, A(), A());
		private void CheckListChanging<List, T>(List list, Action<List> act, int index, IEnumerable<T> newItems,
				IEnumerable<T> oldItems, params T[] listDuringEvent) where List : IListWithChangeEvents<T>
				=> CheckListChange(list, act, index, false, true, newItems, oldItems, listDuringEvent);
		private void CheckListChanged<List, T>(List list, Action<List> act, int index, IEnumerable<T> newItems,
				IEnumerable<T> oldItems, params T[] listDuringEvent) where List : IListWithChangeEvents<T>
				=> CheckListChange(list, act, index, true, true, newItems, oldItems, listDuringEvent);
		private void CheckListChange<List,T>(List list, Action<List> act, int index, bool checkChangedEvent, bool expectEvent, 
				IEnumerable<T> newItems, IEnumerable<T> oldItems, params T[] listDuringEvent) where List:IListWithChangeEvents<T>
		{
			int numEvents = 0;
			ListChangingHandler<T, IListSource<T>> handler = (sender, args) =>
			{
				numEvents++;
				ExpectList(args.OldItems, oldItems);
				ExpectList(args.NewItems, newItems);
				ExpectList(sender, listDuringEvent);
				AreEqual(index, args.Index);
				AreEqual(args.SizeChange, args.NewItems.Count - args.OldItems.Count);
			};
			if (checkChangedEvent)
			{
				list.ListChanged += handler;
				act(list);
				list.ListChanged -= handler;
			}
			else
			{
				list.ListChanging += handler;
				act(list);
				list.ListChanging -= handler;
			}
			if (expectEvent)
				AreEqual(1, numEvents);
			else
				AreEqual(0, numEvents);
		}
	}
}
