using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;

namespace Loyc.Essentials.Tests
{
	[TestFixture]
	public class HashTagsTests
	{
		private void TestTheBasics(HashTags<string> a, bool startsEmpty)
		{
			// This test is run twice, once on a set that starts empty (to
			// test the one-element code paths) and again on a set that has
			// unrelated stuff in it already.
			IEnumerator<KeyValuePair<Symbol, string>> e;

			// Sanity checks
			Assert.IsNull(a.GetTag((string?) null));
			Assert.IsFalse(a.RemoveTag("Nonexistant"));
			Assert.IsFalse(a.HasTag("Nonexistant"));

			a.SetTag("One", "Two");
			Assert.AreEqual(a.GetTag("One"), "Two");

			// Test the enumerator
			e = a.TagEnumerator();
			Assert.IsTrue(e.MoveNext());
			if (startsEmpty)
			{
				Assert.AreEqual(GSymbol.Get("One"), e.Current.Key);
				Assert.AreEqual("Two", e.Current.Value);
				Assert.IsFalse(e.MoveNext());
			}

			// Remove what we added
			Assert.IsNull(a.GetTag((string?) null));
			Assert.IsFalse(a.RemoveTag(""));
			Assert.IsTrue(a.RemoveTag("One"));
			Assert.IsNull(a.GetTag("One"));

			if (startsEmpty)
			{
				e = a.TagEnumerator();
				Assert.IsFalse(e.MoveNext());
			}

			// Do almost the same thing again: add an attr, then remove it
			a.SetTag("One", "Two");
			Assert.AreEqual("Two", a.GetTag("One"));
			Assert.IsTrue(a.HasTag("One"));
			Assert.IsTrue(a.RemoveTag("One"));
			Assert.IsNull(a.GetTag("One"));

			// A different attribute
			a.SetTag("Two", "Three");
			Assert.AreEqual("Three", a.GetTag("Two"));
			a.SetTag("Two", "Four");
			a.SetTag("Two", "Two");
			Assert.AreEqual("Two", a.GetTag("Two"));

			// Another attribute should not disturb the first
			a.SetTag("Three", "Four");
			Assert.AreEqual("Two", a.GetTag("Two"));
			Assert.IsFalse(a.HasTag("One"));
			Assert.IsTrue(a.HasTag("Two"));
			Assert.IsTrue(a.HasTag("Three"));

			// Test the enumerator
			e = a.TagEnumerator();
			Assert.IsTrue(e.MoveNext());
			Assert.IsTrue(e.MoveNext());
			if (startsEmpty)
				Assert.IsFalse(e.MoveNext());

			// Clean up by removing all that we added
			Assert.IsTrue(a.RemoveTag("Two"));
			Assert.IsTrue(a.RemoveTag("Three"));
		}
		void AddFour(HashTags<string> a)
		{
			a.SetTag("Food", "Pizza");
			a.SetTag("Drink", "Mountain Dew");
			a.SetTag("Genitals", "Male");
			a.SetTag("Disposition", "Insane");
		}

		[Test]
		public void TestFromEmpty()
		{
			HashTags<string> a = new HashTags<string>();
			TestTheBasics(a, true);
		}

		[Test]
		public void TestFromFour()
		{
			HashTags<string> a = new HashTags<string>();
			AddFour(a);
			TestTheBasics(a, false);

			// There should be four left
			IEnumerator<KeyValuePair<Symbol, string>> e = a.TagEnumerator();
			for (int i = 0; i < 4; i++)
				Assert.IsTrue(e.MoveNext());
			Assert.IsFalse(e.MoveNext());
		}

		[ExpectedException(typeof(ArgumentNullException))]
		public void TestSetNull1()
		{
			HashTags<string> a = new HashTags<string>();
			a.SetTag((Symbol?) null!, "hello");
		}
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestSetNull2()
		{
			HashTags<string> a = new HashTags<string>();
			a.SetTag("SomethingElseFirst", "hello");
			a.SetTag((string?) null!, "hi");
		}

		[Test]
		public void CopyToDoesNotClobberFirstEntry()
		{
			// Regression: once the dictionary existed, CopyTo copied it and THEN
			// overwrote array[arrayIndex] with the cached pair (which is already in the
			// dictionary), losing one entry and duplicating another.
			var a = new HashTags<string>();
			a.SetTag("one", "1");
			a.SetTag("two", "2");
			a.SetTag("three", "3");
			var coll = (ICollection<KeyValuePair<Symbol, string>>)a;
			Assert.AreEqual(3, coll.Count);

			var array = new KeyValuePair<Symbol, string>[3];
			coll.CopyTo(array, 0);

			var byKey = array.ToDictionary(kv => kv.Key.Name, kv => kv.Value);
			Assert.AreEqual(3, byKey.Count); // no duplicates
			Assert.AreEqual("1", byKey["one"]);
			Assert.AreEqual("2", byKey["two"]);
			Assert.AreEqual("3", byKey["three"]);

			// The single-tag case (no dictionary yet) must still work
			var b = new HashTags<string>();
			b.SetTag("only", "x");
			var one = new KeyValuePair<Symbol, string>[1];
			((ICollection<KeyValuePair<Symbol, string>>)b).CopyTo(one, 0);
			Assert.AreEqual("only", one[0].Key.Name);
			Assert.AreEqual("x", one[0].Value);
		}
	};
}
