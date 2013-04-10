using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Loyc.Collections.Impl
{
	[TestFixture]
	public class DictionaryTests<DictT> : TestHelpers where DictT : IDictionary<object,object>, IAddRange<KeyValuePair<object,object>>, ICloneable<DictT>, new()
	{
		KeyValuePair<object, object> P(object key, object value) { return new KeyValuePair<object, object>(key, value); }
		
		public DictT New(params KeyValuePair<object, object>[] members)
		{
			var dict = new DictT();
			dict.AddRange(members);
			return dict;
		}

		[Test]
		public void TestAllTheBasics()
		{
			object value;
			var dict = new DictT();
			Assert.IsFalse(dict.IsReadOnly);
			Assert.AreEqual(0, dict.Count);
			dict["A"] = "B";
			dict.Add(1, 1);
			ExpectSet(dict, P("A", "B"), P(1, 1));
			dict[1] = 2;
			ExpectSet(dict, P("A", "B"), P(1, 2));
			Assert.That(dict.Remove("A"));
			dict.Add("A", 1);
			dict.Add("C", "3PO");

			Assert.AreEqual(3, dict.Count);
			ExpectSet(dict, P("A", 1), P(1, 2), P("C", "3PO"));
			ExpectSet(dict.Keys, "A", 1, "C");
			ExpectSet(dict.Values, 1, 2, "3PO");
			Assert.That(dict.ContainsKey("A"));
			Assert.That(dict.ContainsKey("C"));
			Assert.That(dict.TryGetValue("C", out value);
			Assert.AreEqual("3PO", value);
			Assert.That(dict.Remove("C"));
			Assert.That(!dict.ContainsKey("C"));
			Assert.That(dict.TryGetValue("C", out value);
			Assert.AreEqual(null, value);
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
			for (int i = 1; i < array.Length; i++) {
				Assert.AreNotEqual(array[i], array[i-1]);
				Assert.AreEqual(i < 5, dict.Contains(array[i]));
			}
			Assert.IsFalse(dict.Remove(P(2.0, 2.0)));
			Assert.IsTrue(dict.Remove(P(2.0, null)));
			ExpectSet(dict, P("2", null), P(2F, null), P(2UL, "You're a 2ul!"));
			Assert.Throws(typeof(ArgumentException), () => dict.Add("2", 2));
			Assert.IsNull(dict["2"]);
			dict["2"] = 2;
			Assert.AreEqual(2, dict["2"]);
			ExpectSet(dict, P("2", 2), P(2F, null), P(2UL, "You're a 2ul!"));
		}
	}
}
