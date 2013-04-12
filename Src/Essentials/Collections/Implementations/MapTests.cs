using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Loyc.Collections.Impl
{
	[TestFixture]
	public class MapTests : DictionaryTests<MMap<object, object>>
	{
		public MapTests() : base(true) { }

		public Map<object,object> Imm(params KeyValuePair<object, object>[] members)
		{
			return new Map<object, object>(members);
		}

		[Test]
		public void TestPersistentSetOperations()
		{
			var set1m = New(P(1,1), P(2,2), P(3,3));
			var set1i = Imm(P(1,1), P(2,2), P(3,3));
			var set2m = New(P(1,1), P("a",1));
			var set2i = Imm(P(1,1), P("a",1));
			ExpectSet(set2m.With("b", 2), P(1, 1), P("a", 1), P("b", 2));
			ExpectSet(set2i.With("b", 2), P(1, 1), P("a", 1), P("b", 2));
			ExpectSet(set1m.Without(2), P(1, 1), P(3, 3));
			ExpectSet(set1i.Without(2), P(1, 1), P(3, 3));
			ExpectSet(set1m.Union(set2m), P(1, 1), P(2, 2), P(3, 3), P("a", 1));
			ExpectSet(set1i.Union(set2i), P(1, 1), P(2, 2), P(3, 3), P("a", 1));
			ExpectSet(set1m.Union(set2i), P(1, 1), P(2, 2), P(3, 3), P("a", 1));
			ExpectSet(set1i.Union(set2m), P(1, 1), P(2, 2), P(3, 3), P("a", 1));
			ExpectSet(set1m.Intersect(set2i), P(1, 1));
			ExpectSet(set1i.Intersect(set2m), P(1, 1));
			ExpectSet(set1m.Except(set2i), P(2, 2), P(3, 3));
			ExpectSet(set1i.Except(set2m), P(2, 2), P(3, 3));
			ExpectSet(set1m.Xor(set2m), P(2, 2), P(3, 3), P("a", 1));
			ExpectSet(set1i.Xor(set2i), P(2, 2), P(3, 3), P("a", 1));
			ExpectSet(set1m, P(1, 1), P(2, 2), P(3, 3));
			ExpectSet(set1i, P(1, 1), P(2, 2), P(3, 3));
			ExpectSet(set2m, P(1, 1), P("a", 1));
			ExpectSet(set2i, P(1, 1), P("a", 1));
		}

		[Test]
		public void TestExtraFunctions()
		{
			var imm = Imm(P(1, 1), P(2, 2), P(3, 3));
			var map = (MMap<object, object>)imm;
			Assert.AreEqual(1, map.TryGetValue(1, -1));
			Assert.AreEqual(1, imm.TryGetValue(1, -1));
			Assert.AreEqual(-1, map.TryGetValue(4, -1));
			Assert.AreEqual(-1, imm.TryGetValue(4, -1));
			Assert.That(map.AddIfNotPresent(0, 0));
			Assert.That(!map.AddIfNotPresent(1, "negatory"));
			ExpectSet(map, P(0, 0), P(1, 1), P(2, 2), P(3, 3));
			var pX = P("X", null);
			var p0 = P(0, "zero");
			Assert.That(!map.AddOrFind(ref p0, false));
			Assert.AreEqual(0, p0.Key);
			Assert.AreEqual(0, p0.Value);
			Assert.AreEqual(0, map[p0.Key]);
			p0 = P(0, "zero");
			Assert.That(!map.AddOrFind(ref p0, true));
			Assert.AreEqual(0, p0.Key);
			Assert.AreEqual(0, p0.Value);
			Assert.AreEqual("zero", map[p0.Key]);
			Assert.That(map.AddOrFind(ref pX, true));
			ExpectSet(map, P(0, "zero"), P(1, 1), P(2, 2), P(3, 3), P("X", null));
			ExpectSet(imm, P(1, 1), P(2, 2), P(3, 3));

			object value = "value";
			Assert.That(!map.GetAndRemove("nonexistant!", ref value));
			Assert.AreEqual("value", value);
			Assert.That(map.GetAndRemove("X", ref value));
			Assert.AreEqual(null, value);
			Assert.That(map.GetAndRemove(ref p0));
			Assert.AreEqual("zero", p0.Value);
			p0 = P(0, "not removed");
			Assert.That(!map.GetAndRemove(ref p0));
			Assert.AreEqual("not removed", p0.Value);
		}
	}
}
