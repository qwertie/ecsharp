using Loyc.Collections.Impl;
using Loyc.Collections;
using Loyc.Collections.MutableListExtensionMethods;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Loyc.Collections.Tests
{
	public class ReadOnlyDictionaryTests<DictT, V> : TestHelpers where DictT : IReadOnlyDictionary<int, V>
	{
		DictT _dict;
		Dictionary<int, V> _expect;

		public ReadOnlyDictionaryTests(DictT dict, params KeyValuePair<int, V>[] data) {
			_dict = dict;
			_expect = new Dictionary<int, V>();
			_expect.SetRange(data);
		}

		Random _r = new Random();

		[Test]
		public void GeneralTests()
		{
			AreEqual(_dict.Count, _expect.Count);

			HashSet<int> keysToCheck = new HashSet<int> { -1, 0, 1, 2, 3, 4, 5, 10, 100, 1000, 10000, 100000 };
			keysToCheck.AddRange(_expect.Keys);

			V v;
			foreach (var k in keysToCheck) {
				bool contains = _expect.ContainsKey(k);
				AreEqual(contains, _dict.ContainsKey(k));
				AreEqual(contains, _dict.TryGetValue(k, out v));
				if (contains) {
					AreEqual(_expect[k], v);
					AreEqual(_expect[k], _dict[k]);
				} else {
					AreEqual(default(V), v);
					Throws<KeyNotFoundException>(() => { var _ = _dict[k]; });
				}
			}
		}

		[Test]
		public void TestEnumerator()
		{
			Dictionary<int, V> copy = new Dictionary<int, V>();
			for (var e = _dict.GetEnumerator(); e.MoveNext();)
				copy[e.Current.Key] = e.Current.Value;
			AreEqual(_expect.Count, copy.Count);
			IsTrue(new HashSet<int>(copy.Keys).SetEquals(_expect.Keys));
			IsTrue(new HashSet<V>(copy.Values).SetEquals(_expect.Values));
		}

		[Test]
		public void TestKeysAndValuesProperties()
		{
			var dictKeys = new HashSet<int>(_dict.Keys);
			var dictVals = new HashSet<V>(_dict.Values);
			var expectKeys = new HashSet<int>(_expect.Keys);
			var expectVals = new HashSet<V>(_expect.Values);
			IsTrue(expectKeys.SetEquals(dictKeys));
			IsTrue(expectVals.SetEquals(dictVals));
		}
	}

	public class SelectDictionaryFromKeysTests
	{
		static int[] testKeys = new[] { 0, 1, 42, 123 };
		static KeyValuePair<int, string>[] expect = 
			testKeys.Select(k => new KeyValuePair<int, string>(k, k.ToString())).ToArray();
		static Func<int, Maybe<string>> TryGetValueIn(int[] testKeys) =>
			i => testKeys.Contains(i) ? (Maybe<string>)i.ToString() : default(Maybe<string>);

		public static object[] TestObjects = new[]
		{
			// Test #1: do not provide optional getValue function
			new ReadOnlyDictionaryTests<SelectDictionaryFromKeys<int, string>, string>(
				new SelectDictionaryFromKeys<int, string>(
					testKeys.AsReadOnly(), TryGetValueIn(testKeys)), expect),
				
			// Test #2: provide getValue function
			new ReadOnlyDictionaryTests<SelectDictionaryFromKeys<int, string>, string>(
				new SelectDictionaryFromKeys<int, string>(
					testKeys.AsReadOnly(), TryGetValueIn(testKeys), i => i.ToString()), expect)
		};
	}
}
