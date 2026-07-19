using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Collections.Tests
{
	public class CollectionTests<CollT, T> : TestHelpers where CollT : ICollection<T>
	{
		protected T[][] _dataSets;
		protected Func<CollT> _new;
		public CollectionTests(Func<CollT> newColl, params T[][] dataSets) => (_dataSets, _new) = (dataSets, newColl);

		[Test]
		public void TestAddAndContains()
		{
			// Assumption: if a dataSet contains duplicates, the collection can hold duplicates
			// Assumption: our use of the default comparer is OK
			var _coll = _new();
			Assert.IsFalse(_coll.IsReadOnly);
			var set = new HashSet<T>();

			foreach (var dataSet in _dataSets) {
				int count = 0;
				foreach (var datum in dataSet) {
					Assert.AreEqual(set.Contains(datum), _coll.Contains(datum));
					_coll.Add(datum);
					set.Add(datum);
					Assert.IsTrue(_coll.Contains(datum));
					Assert.AreEqual(++count, _coll.Count);
				}
				_coll.Clear();
				set.Clear();
				Assert.AreEqual(0, _coll.Count);
			}
		}

		[Test]
		public void TestAddAndRemoveAndCopyTo()
		{
			var _coll = _new();
			foreach (var dataSet in _dataSets) {
				T[] scrambled = dataSet.Randomized();
				List<T> coll2 = new List<T>();

				// Add items while removing others
				for (int i = 0; i < dataSet.Length; i++) {
					var datum = dataSet[i];
					Assert.AreEqual(_coll.Contains(datum), coll2.Contains(datum));
					_coll.Add(datum);
					coll2.Add(datum);
					Assert.IsTrue(_coll.Contains(datum));

					bool removed = _coll.Remove(scrambled[i]);
					bool removed2 = coll2.Remove(scrambled[i]);
					Assert.AreEqual(removed2, removed);
					Assert.AreEqual(coll2.Count, _coll.Count);
				}

				// Test CopyTo
				var array = new T[_coll.Count + 2];
				_coll.CopyTo(array, 2);
				var set = new HashSet<T>();
				for (int i = 2; i < array.Length; i++)
					set.Add(array[i]);
				Assert.IsTrue(set.SetEquals(coll2));

				// Remove remaining items
				foreach (var datum in dataSet) {
					bool removed = _coll.Remove(datum);
					bool removed2 = coll2.Remove(datum);
					Assert.AreEqual(removed2, removed);
					Assert.AreEqual(coll2.Count, _coll.Count);
				}
			}
		}
	}
}
