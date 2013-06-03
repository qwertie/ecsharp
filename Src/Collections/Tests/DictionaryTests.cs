using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Loyc.Math;

namespace Loyc.Collections.Impl
{
	[TestFixture]
	public class DictionaryTests<DictT> : TestHelpers where DictT : ICollection<KeyValuePair<object,object>>, IDictionary<object,object>, IAddRange<KeyValuePair<object,object>>, ICloneable<DictT>, new()
	{
		bool _tryNullKey;
		public DictionaryTests(bool tryNullKey = false) { _tryNullKey = tryNullKey; }

		protected KeyValuePair<object, object> P(object key, object value) { return new KeyValuePair<object, object>(key, value); }
		protected KeyValuePair<object, object>[] Ps(params KeyValuePair<object, object>[] ps) { return ps; }
		
		Random _r = new Random();

		public DictT New(params KeyValuePair<object, object>[] members)
		{
			var dict = new DictT();
			dict.AddRange(members);
			return dict;
		}

		protected bool Remove(DictT dict, KeyValuePair<object, object> pair) { return ((ICollection<KeyValuePair<object, object>>)dict).Remove(pair); }
		protected bool Remove(DictT dict, object key, object value) { return Remove(dict, P(key, value)); }
		protected int Count(DictT dict) { return ((IDictionary<object, object>)dict).Count; }

		[Test]
		public void TestAllTheBasics()
		{
			object value;
			var dict = new DictT();
			Assert.IsFalse(dict.IsReadOnly);
			Assert.AreEqual(0, Count(dict));
			dict["A"] = "B";
			dict.Add(1, 1);
			ExpectSet(dict, P("A", "B"), P(1, 1));
			dict[1] = 2;
			ExpectSet(dict, P("A", "B"), P(1, 2));
			Assert.That(dict.Remove("A"));
			dict.Add("A", 1);
			dict.Add("C", "3PO");

			Assert.AreEqual(3, Count(dict));
			ExpectSet(dict, P("A", 1), P(1, 2), P("C", "3PO"));
			ExpectSet(dict.Keys, "A", 1, "C");
			ExpectSet(dict.Values, 1, 2, "3PO");
			Assert.That(dict.ContainsKey("A"));
			Assert.That(dict.ContainsKey("C"));
			Assert.That(dict.TryGetValue("C", out value));
			Assert.AreEqual("3PO", value);
			Assert.That(dict.Remove("C"));
			Assert.That(!dict.ContainsKey("C"));
			Assert.That(!dict.TryGetValue("C", out value));
			Assert.AreEqual(value, null);
			Assert.AreEqual(1, dict["A"]);
			Assert.AreEqual(2, dict[1]);
			Assert.Throws(typeof(KeyNotFoundException), () => { var _ = dict["C"]; });
			dict.Clear();
			ExpectSet(dict);

			dict.Add("2", null);
			ExpectSet(dict, P("2", null));
			dict.Add(2.0, null);
			dict.Add(2F, null);
			dict.Add(2UL, "You're a 2ul!");
			ExpectSet(dict, P("2", null), P(2.0, null), P(2F, null), P(2UL, "You're a 2ul!"));
			Assert.That(dict.Contains(P(2.0, null)));
			Assert.That(!dict.Contains(P(2.0, 2.0)));
			
			var array = new KeyValuePair<object, object>[6];
			dict.CopyTo(array, 1);
			Assert.AreEqual(array[0], array[5]);
			array[5] = P(-1, null); // The standard Dictionary throws a nonsensical 
				// ArgumentNullException if you call dict.Contains(P(null, anything))
				// even though KeyValuePair obviously can't be null. But IMO that's
				// not as bad as the fact that dict.TryGetValue(x, out y) throws an 
				// exception if x is ever null.
			for (int i = 1; i < array.Length; i++) {
				Assert.AreNotEqual(array[i], array[i-1]);
				Assert.AreEqual(i < 5, dict.Contains(array[i]));
			}
			Assert.IsFalse(Remove(dict, 2.0, 2.0));
			Assert.IsTrue(Remove(dict, 2.0, null));
			ExpectSet(dict, P("2", null), P(2F, null), P(2UL, "You're a 2ul!"));
			Assert.Throws(typeof(ArgumentException), () => dict.Add("2", 2));
			Assert.IsNull(dict["2"]);
			dict["2"] = 2;
			Assert.AreEqual(2, dict["2"]);
			ExpectSet(dict, P("2", 2), P(2F, null), P(2UL, "You're a 2ul!"));

			if (_tryNullKey) {
				Assert.That(!dict.TryGetValue(null, out value));
				dict.Add(null, 15);
				ExpectSet(dict, P(null, 15), P("2", 2), P(2F, null), P(2UL, "You're a 2ul!"));
				Assert.That(dict.ContainsKey(null));
				Assert.That(dict.Contains(P(null, 15)));
				Assert.That(!dict.Contains(P(null, null)));
				Assert.AreEqual(15, dict[null]);
				Assert.That(dict.TryGetValue(null, out value));
				Assert.AreEqual(15, value);
				dict.Remove(2UL);
				dict[null] = "(Nothing in Visual Basic)";
				ExpectSet(dict, P(null, "(Nothing in Visual Basic)"), P("2", 2), P(2F, null));
			}
		}

		[Test]
		public void AddingAndRemoving()
		{
			var dict1 = new Dictionary<object,object>();
			var dict2 = new DictT();

			// Add a random amount, then remove a random amount. Repeatedly.
			for (int i = 0; i < 100; i++) {
				for (int j = MathEx.Sqrt(_r.Next(1000000)); j >= 0; j--) {
					var p = P(_r.Next(j * 2), i * 100 + j);
					dict1[p.Key] = p.Value;
					dict2[p.Key] = p.Value;
				}
				ExpectSet(dict2, new HashSet<KeyValuePair<object, object>>(dict1));
				double chance = _r.NextDouble();
				var removePlan = dict1.Where(p => _r.NextDouble() > chance).ToList().Randomized();
				bool removePair = _r.Next(2) != 0;
				foreach (var pair in removePlan) {
					dict1.Remove(pair.Key);
					if (removePair) {
						Assert.That(!dict2.Remove(P(pair.Key, -1)));
						Assert.That(Remove(dict2, pair));
						Assert.That(!Remove(dict2, pair));
					} else {
						Assert.That(dict2.Remove(pair.Key));
						Assert.That(!dict2.Remove(pair.Key));
					}
				}
				ExpectSet(dict2, new HashSet<KeyValuePair<object, object>>(dict1));
			}
			
			// Empty it out one item at a time.
			while (((IReadOnlyCollection<int>)dict2).Count != 0)
				Remove(dict2, dict2.First());
			ExpectSet(dict2);
		}

		[Test]
		public void AddRangeAndClone()
		{
			var dict = New(P(1, 2));
			dict.AddRange(Ps());
			dict.AddRange(Ps(P(2, 1), P("black", "white"), P("foo", "bar")));
			ExpectSet(dict, P(1, 2), P(2, 1), P("black", "white"), P("foo", "bar"));
			Assert.That(dict.Remove(2));
			var dict2 = dict.Clone();
			dict.AddRange(Ps(P(123, 321)));
			dict.AddRange(Ps(P(3, 9), P(9, 81)));
			dict2.AddRange(Ps(P(2, 4), P(4, 16), P(16, 256)));
			ExpectSet(dict, P("black", "white"), P("foo", "bar"), P(1, 2), P(3, 9), P(9, 81), P(123, 321));
			ExpectSet(dict2, P("black", "white"), P("foo", "bar"), P(1, 2), P(2, 4), P(4, 16), P(16, 256));

			if (_tryNullKey) {
				dict2.AddRange(Ps(P(null, 0)));
				Assert.That(dict2.Remove("black"));
				Assert.That(dict2.Remove("foo"));
				ExpectSet(dict2, P(null, 0), P(1, 2), P(2, 4), P(4, 16), P(16, 256));
			}
		}
	}
}
