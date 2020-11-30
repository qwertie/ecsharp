using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.Collections.Tests;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Essentials.Tests
{
	public class DictionaryWithChangeEventsForTesting<K, V> : DictionaryWithChangeEvents<K, V, Dictionary<K, V>>,
		ICloneable<DictionaryWithChangeEventsForTesting<K, V>>
	{
		public DictionaryWithChangeEventsForTesting() : base(new Dictionary<K, V>()) { }
		public DictionaryWithChangeEventsForTesting(Dictionary<K, V> obj) : base(obj) { }

		public DictionaryWithChangeEventsForTesting<K, V> Clone() => new DictionaryWithChangeEventsForTesting<K, V>(new Dictionary<K, V>(_obj));
	}

	// This wrapper always subscribes to an event, causing different code paths to be used in the basic tests
	public class DictionaryWithChangeEventsForTesting2<K, V> : DictionaryWithChangeEvents<K, V, Dictionary<K, V>>,
		ICloneable<DictionaryWithChangeEventsForTesting2<K, V>>
	{
		public DictionaryWithChangeEventsForTesting2() : base(new Dictionary<K, V>()) { Subscribe(); }
		public DictionaryWithChangeEventsForTesting2(Dictionary<K, V> obj) : base(obj) { Subscribe(); }

		public DictionaryWithChangeEventsForTesting2<K, V> Clone() => new DictionaryWithChangeEventsForTesting2<K, V>(new Dictionary<K, V>(_obj));

		static Random _random = new Random();
		private void Subscribe()
		{
			if (_random.Next(2) != 0)
				ListChanging += ListChangeHandler;
			else
				ListChanged += ListChangeHandler;
		}
		private void ListChangeHandler(IDictionary<K, V> sender, ListChangeInfo<KeyValuePair<K, V>> args)
		{
			Assert.AreEqual(int.MinValue, args.Index);
			Assert.AreEqual(args.SizeChange, args.NewItems.Count - args.OldItems.Count);
			if (args.Action == NotifyCollectionChangedAction.Remove)
				Assert.Less(args.SizeChange, 0);
			if (args.Action == NotifyCollectionChangedAction.Add)
				Assert.Greater(args.SizeChange, 0);
		}
	}

	public class DictionaryWithChangeEventsTests2 : DictionaryTests<DictionaryWithChangeEventsForTesting2<object, object>>
	{
	}

	public class DictionaryWithChangeEventsTests : DictionaryTests<DictionaryWithChangeEventsForTesting<object, object>>
	{
		Random _r = new Random();

		// Helper methods for making key-value pairs and lists of pairs
		static KeyValuePair<object, object> P(object key, object value) => new KeyValuePair<object, object>(key, value);
		static KeyValuePair<object, object>[] A(params object[] pairs)
		{
			AreEqual(0, pairs.Length & 1);
			var a = new KeyValuePair<object, object>[pairs.Length >> 1];
			for (int i = 0; i < a.Length; i++)
				a[i] = P(pairs[i * 2], pairs[i * 2 + 1]);
			return a;
		}

		// Modification checklist: Add (two overloads), Remove (two overloads), Clear, indexer, AddRange (two overloads)
		// - Test indexer replacing an old item and creating a new item

		[Test]
		public void TestEvents1()
		{
			// Build list { ["five"] = 5, ["six"] = 6, ["seven"] = 7 }, then clear
			var dict = new DictionaryWithChangeEventsForTesting<object, object>();
			CheckListChanging(dict, d => d.Add("five", 5), A("five", 5), A());
			CheckListChanged(dict, d => d.Add("six", 6), A("six", 6), A(), "five", 5, "six", 6);
			CheckListChanging(dict, d => d.Add(P("two", 2)), A("two", 2), A(), "five", 5, "six", 6);
			CheckListChanged(dict, d => d.Add(P("seven", 7)), A("seven", 7), A(), "five", 5, "six", 6, "two", 2, "seven", 7);
			CheckNoEventOccurs(dict, d => IsFalse(d.Remove(P("two", 123))));
			CheckListChanged(dict, d => IsTrue(d.Remove("two")), A(), A("two", 2), "five", 5, "six", 6, "seven", 7);
			CheckListChanging(dict, d => d.Clear(), A(), A("five", 5, "six", 6, "seven", 7), "five", 5, "six", 6, "seven", 7);
			CheckNoEventOccurs(dict, d => d.Clear());
			CheckNoEventOccurs(dict, d => IsFalse(d.Remove(123)));
		}

		[Test]
		public void TestEvents2()
		{
			// Build list { [1] = 11, [2] = 22, [3] = 33, [4] = 44, [5] = 55, [6] = 66 }, then remove some things and clear
			var dict = new DictionaryWithChangeEventsForTesting<object, object>();
			dict.Add(1, 11);
			CheckNoEventOccurs(dict, d => ThrowsAny<SystemException>(() => d.Add(P(1, 111))));
			dict.Add(2, 22);
			CheckNoEventOccurs(dict, d => ThrowsAny<SystemException>(() => d.Add(2, 222)));
			dict.Add(3, 3333);
			CheckListChanging(dict, d => d[3] = 33, A(3, 33), A(3, 3333), 1, 11, 2, 22, 3, 3333);
			dict.Add(4, 4444);
			CheckListChanged(dict, d => d[4] = 44, A(4, 44), A(4, 4444), 1, 11, 2, 22, 3, 33, 4, 44);
			CheckListChanging(dict, d => d[5] = 55, A(5, 55), A(), 1, 11, 2, 22, 3, 33, 4, 44);
			CheckListChanged(dict, d => d[6] = 66, A(6, 66), A(), 1, 11, 2, 22, 3, 33, 4, 44, 5, 55, 6, 66);

			CheckNoEventOccurs(dict, d => IsFalse(d.Remove(123)));
			CheckListChanging(dict, d => IsTrue(d.Remove(2)), A(), A(2, 22), 1, 11, 2, 22, 3, 33, 4, 44, 5, 55, 6, 66);
			CheckListChanged(dict, d => IsTrue(d.Remove(3)), A(), A(3, 33), 1, 11, 4, 44, 5, 55, 6, 66);
			CheckNoEventOccurs(dict, d => IsFalse((d as ICollection<KeyValuePair<object,object>>).Remove(P(1, -111))));
			CheckListChanging(dict, d => IsTrue((d as ICollection<KeyValuePair<object, object>>).Remove(P(4, 44))), A(), A(4, 44), 1, 11, 4, 44, 5, 55, 6, 66);
			CheckListChanged(dict, d => IsTrue((d as ICollection<KeyValuePair<object, object>>).Remove(P(6, 66))), A(), A(6, 66), 1, 11, 5, 55);

			CheckListChanged(dict, d => d.Clear(), A(), A(1, 11, 5, 55));
			CheckNoEventOccurs(dict, d => IsFalse(d.Remove(1)));
			CheckNoEventOccurs(dict, d => d.Clear());
		}

		[Test]
		public void TestEvents3()
		{
			// Use both overloads of AddRange to build a list { [1] = 11, [2] = 22, ... [7] = 77 }
			var dict = new DictionaryWithChangeEventsForTesting<object, object>();
			CheckListChanging(dict, d => d.AddRange(A(1, 11, 3, 33) as IEnumerable<KeyValuePair<object, object>>), A(1, 11, 3, 33), A());
			CheckListChanged(dict, d => d.AddRange(A(5, 55) as IEnumerable<KeyValuePair<object, object>>), A(5, 55), A(), 1, 11, 3, 33, 5, 55);
			CheckNoEventOccurs(dict, d => d.AddRange(A() as IEnumerable<KeyValuePair<object, object>>));
			CheckNoEventOccurs(dict, d => d.AddRange(A()));
			CheckListChanging(dict, d => d.AddRange(A(7, 77, 2, 22, 4, 44)), A(7, 77, 2, 22, 4, 44), A(), 1, 11, 3, 33, 5, 55);
			CheckListChanged(dict, d => d.AddRange(A(6, 66)), A(6, 66), A(), 1, 11, 3, 33, 5, 55, 7, 77, 2, 22, 4, 44, 6, 66);
			
			CheckListChanged(dict, d => d.Clear(), A(), A(1, 11, 3, 33, 5, 55, 7, 77, 2, 22, 4, 44, 6, 66));
		}

		private void CheckNoEventOccurs<Dict>(Dict list, Action<Dict> act) where Dict : IDictionaryWithChangeEvents<object, object>
				=> CheckListChange(list, act, _r.Next(2) == 0, false, A(), A());
		private void CheckListChanging<Dict>(Dict list, Action<Dict> act, IEnumerable<KeyValuePair<object,object>> newItems,
				IEnumerable<KeyValuePair<object, object>> oldItems, params object[] listDuringEvent) where Dict : IDictionaryWithChangeEvents<object,object>
				=> CheckListChange(list, act, false, true, newItems, oldItems, A(listDuringEvent));
		private void CheckListChanged<Dict>(Dict list, Action<Dict> act, IEnumerable<KeyValuePair<object, object>> newItems,
				IEnumerable<KeyValuePair<object, object>> oldItems, params object[] listDuringEvent) where Dict : IDictionaryWithChangeEvents<object,object>
				=> CheckListChange(list, act, true, true, newItems, oldItems, A(listDuringEvent));
		private void CheckListChange<Dict, K, V>(Dict list, Action<Dict> act, bool checkChangedEvent, bool expectEvent, IEnumerable<KeyValuePair<K, V>> newItems,
				IEnumerable<KeyValuePair<K, V>> oldItems, params KeyValuePair<K, V>[] listDuringEvent) where Dict : IDictionaryWithChangeEvents<K,V>
		{
			int numEvents = 0;
			ListChangingHandler<KeyValuePair<K,V>, IDictionary<K,V>> handler = (sender, args) =>
			{
				numEvents++;
				ExpectList(args.OldItems, oldItems);
				ExpectList(args.NewItems, newItems);
				ExpectList(sender, listDuringEvent);
				AreEqual(int.MinValue, args.Index);
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
