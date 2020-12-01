using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.MiniTest;

namespace Loyc.Essentials.Tests
{
	public class CollectionWithChangeEventsTests : TestHelpers
	{
		static int[] A(params int[] a) => a;
		static Random _r = new Random();

		// Modification checklist: Add, TryAdd, Remove, Clear

		[Test]
		public void TestEventsOnList()
		{
			// Build list { 5, 6, 5, 6 }, then remove stuff
			var list = new CollectionWithChangeEvents<int>(new List<int>());
			CheckListChanging(list, l => l.Add(5), int.MinValue, A(5), A());
			CheckListChanged(list, l => l.Add(6), int.MinValue, A(6), A(), 5, 6);
			CheckListChanging(list, l => IsTrue(l.TryAdd(5)), int.MinValue, A(5), A(), 5, 6);
			CheckListChanged(list, l => IsTrue(l.TryAdd(6)), int.MinValue, A(6), A(), 5, 6, 5, 6);
			CheckListChanging(list, l => IsTrue(l.Remove(5)), int.MinValue, A(), A(5), 5, 6, 5, 6);
			CheckListChanged(list, l => IsTrue(l.Remove(5)), int.MinValue, A(), A(5), 6, 6);
			CheckNoEventOccurs(list, l => IsFalse(l.Remove(5)));
			IsFalse(list.IsEmpty);
			CheckListChanging(list, l => l.Clear(), 0, A(), A(6, 6), 6, 6);
			IsTrue(list.IsEmpty);
			CheckNoEventOccurs(list, l => l.Clear());
		}

		[Test]
		public void TestEventsOnSet()
		{
			// Build set { 5, 6, 7, 8 }, then remove stuff
			var list = new CollectionWithChangeEvents<int>(new HashSet<int>());
			CheckListChanging(list, l => l.Add(5), int.MinValue, A(5), A());
			CheckListChanged(list, l => l.Add(6), int.MinValue, A(6), A(), 5, 6);
			CheckNoEventOccurs(list, l => IsFalse(l.TryAdd(5)));
			CheckNoEventOccurs(list, l => l.Add(6));
			CheckListChanging(list, l => IsTrue(l.TryAdd(7)), int.MinValue, A(7), A(), 5, 6);
			CheckListChanged(list, l => IsTrue(l.TryAdd(8)), int.MinValue, A(8), A(), 5, 6, 7, 8);
			CheckNoEventOccurs(list, l => IsFalse(l.Remove(88)));
			CheckListChanging(list, l => IsTrue(l.Remove(6)), int.MinValue, A(), A(6), 5, 6, 7, 8);
			CheckListChanged(list, l => IsTrue(l.Remove(8)), int.MinValue, A(), A(8), 5, 7);
			CheckListChanged(list, l => l.Clear(), 0, A(), A(5, 7));
			CheckNoEventOccurs(list, l => l.Clear());
		}

		private void CheckNoEventOccurs<List>(List list, Action<List> act) where List : ICollectionWithChangeEvents<int>
				=> CheckListChange(list, act, -1, _r.Next(2) == 0, false, A(), A());
		private void CheckListChanging<List, T>(List list, Action<List> act, int index, IEnumerable<T> newItems,
				IEnumerable<T> oldItems, params T[] listDuringEvent) where List : ICollectionWithChangeEvents<T>
				=> CheckListChange(list, act, index, false, true, newItems, oldItems, listDuringEvent);
		private void CheckListChanged<List, T>(List list, Action<List> act, int index, IEnumerable<T> newItems,
				IEnumerable<T> oldItems, params T[] listDuringEvent) where List : ICollectionWithChangeEvents<T>
				=> CheckListChange(list, act, index, true, true, newItems, oldItems, listDuringEvent);
		private void CheckListChange<List, T>(List list, Action<List> act, int index, bool checkChangedEvent, bool expectEvent,
				IEnumerable<T> newItems, IEnumerable<T> oldItems, params T[] listDuringEvent) where List : ICollectionWithChangeEvents<T>
		{
			int numEvents = 0;
			ListChangingHandler<T, ICollection<T>> handler = (sender, args) =>
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
