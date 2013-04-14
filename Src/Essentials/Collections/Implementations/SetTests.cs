using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Diagnostics;

namespace Loyc.Collections.Impl
{
	/// <summary>An class used for testing purposes with three parts: a hashcode,
	/// a mutable value (ignored for equality testing) and an extended key int
	/// which affects equality but not the hashcode.</summary>
	public class SetTestItem : IEquatable<SetTestItem>
	{
		public int HashCode;
		public object ExtKey;
		public object Value;

		public static implicit operator SetTestItem(int hashCode) { return new SetTestItem { HashCode = hashCode }; }
		public override int GetHashCode() { return HashCode; }
		public override bool Equals(object obj) { return Equals(obj as SetTestItem); }
		public bool Equals(SetTestItem other)
		{
			return other != null && HashCode == other.HashCode && object.Equals(ExtKey, other.ExtKey);
		}
		public override string ToString()
		{
			return string.Format("HashCode=0x{0:X}, ExtKey={1}, Value={2}", HashCode, ExtKey, Value);
		}
	}

	public abstract class MutableSetTests<S, T> : TestHelpers 
		where S : ISet<T>, ICloneable<S>
		where T : class 
	{
		protected static Random Random;
		static MutableSetTests() {
			int seed = Environment.TickCount;
			Console.WriteLine("{0} seed={1}", MemoizedTypeName.GetGenericName(typeof(MutableSetTests<S, T>)), seed);
			Random = new Random(seed);
		}

		protected abstract T Item(int hashCode, object extKey = null);

		protected abstract S NewSet();
		protected abstract S NewSet(IEnumerable<T> contents);
		protected S NewSet(params T[] contents) { return NewSet(contents as IEnumerable<T>); }

		[Test]
		public void SmallSetTests()
		{
			for (int iter = 0; iter < 5; iter++) {
				T one = Item(1), two = Item(2), two2 = Item(2);
				T three = Item(Random.Next(10), "#3"), four = Item(Random.Next(40), "#4");
				T five = Item(Random.Next(int.MinValue, int.MaxValue), "#5");

				S a = NewSet(one, two);
				S b = NewSet(five, four, three, two2);
				Assert.AreEqual(2, a.Count);
				Assert.AreEqual(4, b.Count);
				S c = a.Clone();
				c.UnionWith(b);
				ExpectSet(c, one, two, three, four, five);
				c = a.Clone();
				c.IntersectWith(b);
				ExpectSet(c, two);
				c = a.Clone();
				c.ExceptWith(b);
				ExpectSet(c, one);
				c = a.Clone();
				c.SymmetricExceptWith(b);
				ExpectSet(c, one, three, four, five);

				Assert.IsTrue(a.Add(three));
				c = a.Clone();
				c.IntersectWith(b);
				ExpectSet(c, two, three);
				c = a.Clone();
				c.SymmetricExceptWith(b);
				ExpectSet(c, one, four, five);

				Assert.IsTrue(a.Overlaps(b));
				Assert.IsTrue(b.Overlaps(a));
				Assert.IsFalse(a.SetEquals(b));
				Assert.IsFalse(a.IsSubsetOf(b));
				Assert.IsFalse(a.IsSupersetOf(b));
				Assert.IsFalse(a.IsProperSubsetOf(b));
				Assert.IsFalse(a.IsProperSupersetOf(b));

				Assert.IsTrue(b.Add(one));
				ExpectSet(a, one, two, three);
				ExpectSet(b, one, two, three, four, five);
				Assert.IsTrue(a.Overlaps(b));
				Assert.IsFalse(a.SetEquals(b));
				Assert.IsTrue(a.IsSubsetOf(b));
				Assert.IsFalse(b.IsSubsetOf(a));
				Assert.IsFalse(a.IsSupersetOf(b));
				Assert.IsTrue(b.IsSupersetOf(a));
				Assert.IsTrue(a.IsProperSubsetOf(b));
				Assert.IsFalse(b.IsProperSubsetOf(a));
				Assert.IsFalse(a.IsProperSupersetOf(b));
				Assert.IsTrue(b.IsProperSupersetOf(a));

				Assert.IsTrue(a.Add(four));
				Assert.IsTrue(a.Add(five));
				Assert.IsFalse(a.Add(five));
				ExpectSet(a, one, two, three, four, five);
				Assert.IsTrue(a.Overlaps(b));
				Assert.IsTrue(a.SetEquals(b));
				Assert.IsTrue(a.IsSubsetOf(b));
				Assert.IsTrue(a.IsSupersetOf(b));
				Assert.IsTrue(b.IsSubsetOf(a));
				Assert.IsTrue(b.IsSupersetOf(a));
				Assert.IsFalse(a.IsProperSubsetOf(b));
				Assert.IsFalse(a.IsProperSupersetOf(b));
				Assert.IsFalse(b.IsProperSubsetOf(a));
				Assert.IsFalse(b.IsProperSupersetOf(a));
			}
		}
		[Test]
		public void RandomSetTests()
		{
			for (int shift = 1; shift <= 10; shift++) {
				// Do some tests with sets of substantially different sizes,
				// and also some tests with sets of similar size.
				RandomSetTests(1 << shift, 8 << shift, false);
				RandomSetTests(8 << shift, 1 << shift, false);
				RandomSetTests(2 << shift, 2 << shift, false);
			}
			// Finally, we'll do some tests with lots of hash collisions
			RandomSetTests(10, 100, true);
			RandomSetTests(100, 50, true);
			RandomSetTests(200, 25, true);
		}
		void RandomSetTests(int maxSizeA, int maxSizeB, bool limitHashCodes)
		{
			int max = System.Math.Max(maxSizeA, maxSizeB);
			BitArray aMembers, bMembers;
			S a = RandomSet(maxSizeA, out aMembers, limitHashCodes);
			S b = RandomSet(maxSizeB, out bMembers, limitHashCodes);
			List<T> aItems = a.ToList();
			
			// Check that the set is correct to begin with
			CheckResult(a, aMembers, bMembers, max, limitHashCodes, (bool hasA, bool hasB) => hasA);
			CheckResult(b, aMembers, bMembers, max, limitHashCodes, (bool hasA, bool hasB) => hasB);
			
			S c = a.Clone(); c.UnionWith(b);
			CheckResult(c, aMembers, bMembers, max, limitHashCodes, (bool hasA, bool hasB) => hasA || hasB);
			c = a.Clone(); c.IntersectWith(b);
			CheckResult(c, aMembers, bMembers, max, limitHashCodes, (bool hasA, bool hasB) => hasA && hasB);
			c = a.Clone(); c.ExceptWith(b);
			CheckResult(c, aMembers, bMembers, max, limitHashCodes, (bool hasA, bool hasB) => hasA && !hasB);
			c = a.Clone(); c.SymmetricExceptWith(b);
			CheckResult(c, aMembers, bMembers, max, limitHashCodes, (bool hasA, bool hasB) => hasA ^ hasB);

			ExpectList(a, aItems);
			// Now mutate a, one element at a time, until it looks like b.
			for (int i = 0; i < max; i++) {
				bool hasA = At(aMembers, i), hasB = At(bMembers, i);
				T item = Item(i, limitHashCodes);
				if (hasB)
					Assert.AreEqual(!hasA, a.Add(item));
				else
					Assert.AreEqual(hasA, a.Remove(item));
			}
			ExpectSet(a, b.ToArray());
		}
		private S RandomSet(int maxSize, out BitArray members, bool limitHashCodes)
		{
			S set = NewSet();
			members = new BitArray(maxSize);
			for (int i = 0; i < maxSize; i++) {
				if (Random.Next(2) != 0) {
					members[i] = true;
					set.Add(Item(i, limitHashCodes));
				}
			}
			return set;
		}
		T Item(int i, bool limitHashCodes)
		{
			if (limitHashCodes)
				return Item((i & 0x13) ^ 0x12345600, i);
			else
				return Item(i);
		}
		void CheckResult(S set, BitArray aMembers, BitArray bMembers, int max, bool limitHashCodes, Func<bool, bool, bool> combinator)
		{
			int count = 0;
			for (int i = 0; i < max; i++) {
				T item = Item(i, limitHashCodes);
				bool exists = set.Contains(item);
				bool hasA = At(aMembers, i);
				bool hasB = At(bMembers, i);
				Assert.AreEqual(combinator(hasA, hasB), exists);
				if (exists)
					count++;
			}
			Assert.AreEqual(count, set.Count);
		}
		static bool At(BitArray a, int i) { return i < a.Count && a[i]; }

		[Test]
		public void SetWithNull()
		{
			// Ensure a null key works (2nd version of InternalSet only)
			var a = NewSet((T)null);
			var b = NewSet(null, Item(10));
			var c = NewSet(Item(10), Item(20));
			Assert.That(!c.Contains(null));
			Assert.That(c.Add(null));
			Assert.That(c.Contains(null));
			Assert.That(c.Remove(null));
			Assert.That(!c.Remove(null));
			b.IntersectWith(c);
			ExpectSet(b, Item(10));
			a.UnionWith(c);
			ExpectSet(a, null, Item(10), Item(20));
			ExpectSet(c, Item(10), Item(20));
		}
	}

	[TestFixture]
	public class MSetTests : MutableSetTests<MSet<SetTestItem>, SetTestItem>
	{
		protected override SetTestItem Item(int hashCode, object extKey = null) 
			{ return new SetTestItem { HashCode = hashCode, ExtKey = extKey }; }
		protected override MSet<SetTestItem> NewSet() 
			{ return new MSet<SetTestItem>(); }
		protected override MSet<SetTestItem> NewSet(IEnumerable<SetTestItem> contents) 
			{ return new MSet<SetTestItem>(contents); }
		protected SetTestItem EKO(int hashCode) // extended key only
			{ return new SetTestItem { HashCode = 0x123, ExtKey = hashCode }; }

		[Test]
		public void OperatorTests()
		{
			// Just to be different from MSet<Symbol>Tests, use same hashcode for all items.
			var a = NewSet(EKO(00), EKO(11), EKO(22), EKO(33), EKO(44));
			var b = NewSet(EKO(33), EKO(44), EKO(55));
			ExpectSet(a | b, EKO(00), EKO(11), EKO(22), EKO(33), EKO(44), EKO(55));
			ExpectSet(a, EKO(00), EKO(11), EKO(22), EKO(33), EKO(44));
			ExpectSet(b, EKO(33), EKO(44), EKO(55));
			ExpectSet(a & b, EKO(33), EKO(44));
			ExpectSet(a - b, EKO(00), EKO(11), EKO(22));
			ExpectSet(a ^ b, EKO(00), EKO(11), EKO(22), EKO(55));
		}

		[Test]
		public void RemoveAll()
		{
			// That which is added, must remove successfully...
			var set = NewSet();
			int count = 0;
			for (int i = 0; i < 30; i++)
			{
				Assert.IsTrue(set.Add(EKO(i)));
				Assert.IsTrue(set.Add(Item(i)));
				Assert.AreEqual(count += 2, set.Count);
			}
			for (int i = 0; i < 30; i++)
			{
				Assert.IsTrue(set.Remove(EKO(i)));
				Assert.IsTrue(set.Remove(Item(i)));
				Assert.IsFalse(set.Remove(EKO(i)));
				Assert.AreEqual(count -= 2, set.Count);
			}
			ExpectSet(set);
		}
	}

	[TestFixture]
	public class SymbolSetTests : MutableSetTests<MSet<Symbol>, Symbol>
	{
		// Symbol doesn't have an "extended key" concept like SetTestItem does.
		// Just include the extended key in the Name.
		protected override Symbol Item(int hashCode, object extKey = null)
			{ return GSymbol.Get(extKey == null ? hashCode.ToString() : string.Format("{0}-{1}", hashCode, extKey)); }
		protected override MSet<Symbol> NewSet()
			{ return new MSet<Symbol>(); }
		protected override MSet<Symbol> NewSet(IEnumerable<Symbol> contents)
			{ return new MSet<Symbol>(contents); }

		[Test]
		public void OperatorTests()
		{
			MSet<Symbol> a = NewSet(Item(11), Item(22), Item(33), Item(44));
			MSet<Symbol> b = NewSet(Item(33), Item(44), Item(55));
			ExpectSet(a & b, Item(33), Item(44));
			ExpectSet(a, Item(11), Item(22), Item(33), Item(44));
			ExpectSet(b, Item(33), Item(44), Item(55));
			ExpectSet(a | b, Item(11), Item(22), Item(33), Item(44), Item(55));
			ExpectSet(a - b, Item(11), Item(22));
			ExpectSet(a ^ b, Item(11), Item(22), Item(55));
			
			ExpectSet(b + Item(99), Item(33), Item(44), Item(55), Item(99));
			ExpectSet(Item(99) + b, Item(33), Item(44), Item(55), Item(99));
			ExpectSet(b - Item(99), Item(33), Item(44), Item(55));
			ExpectSet(b - Item(44), Item(33), Item(55));
		}
	}

	public class ImmSetTests : TestHelpers
	{
		protected Symbol S(string text) { return GSymbol.Get(text); }

		[Test]
		public void ImmutableSetTests()
		{
			{
				Set<string> a = new Set<string>(new[] { "11", "22", "33", "44" });
				Set<string> b = new Set<string>(new[] { "33", "44", "55" });

				ExpectSet(b + "33",   "33", "44", "55");
				ExpectSet(b + "BAM!", "33", "44", "55", "BAM!");
				ExpectSet("Qué?" + b, "33", "44", "55", "Qué?");
				ExpectSet(b - "Bob", "33", "44", "55");
				ExpectSet(b - "55", "33", "44");

				ExpectSet(a, "11", "22", "33", "44");
				ExpectSet(b, "33", "44", "55");

				ExpectSet(a | b, "11", "22", "33", "44", "55");
				ExpectSet(a & b, "33", "44");
				ExpectSet(a - b, "11", "22");
				ExpectSet(a ^ b, "11", "22", "55");
			}
			{
				Set<Symbol> a = new Set<Symbol>(new[] { S("11"), S("22"), S("33"), S("44") });
				Set<Symbol> b = new Set<Symbol>(new[] { S("33"), S("44"), S("55") });

				ExpectSet(b + S("33"), S("33"), S("44"), S("55"));
				ExpectSet(b + S("BAM!"), S("33"), S("44"), S("55"), S("BAM!"));
				ExpectSet(S("Qué?") + b, S("33"), S("44"), S("55"), S("Qué?"));
				ExpectSet(b - S("Bob"), S("33"), S("44"), S("55"));
				ExpectSet(b - S("55"), S("33"), S("44"));

				ExpectSet(a, S("11"), S("22"), S("33"), S("44"));
				ExpectSet(b, S("33"), S("44"), S("55"));

				ExpectSet(a | b, S("11"), S("22"), S("33"), S("44"), S("55"));
				ExpectSet(a & b, S("33"), S("44"));
				ExpectSet(a - b, S("11"), S("22"));
				ExpectSet(a ^ b, S("11"), S("22"), S("55"));
			}
			// Repeat the tests again for the mutable sets, just to make sure that 
			// all versions of the overloaded operators are working.
			{
				MSet<string> a = new MSet<string>(new[] { "11", "22", "33", "44" });
				MSet<string> b = new MSet<string>(new[] { "33", "44", "55" });

				ExpectSet(b + "33", "33", "44", "55");
				ExpectSet(b + "BAM!", "33", "44", "55", "BAM!");
				ExpectSet("Qué?" + b, "33", "44", "55", "Qué?");
				ExpectSet(b - "Bob", "33", "44", "55");
				ExpectSet(b - "55", "33", "44");

				ExpectSet(a, "11", "22", "33", "44");
				ExpectSet(b, "33", "44", "55");

				ExpectSet(a | b, "11", "22", "33", "44", "55");
				ExpectSet(a & b, "33", "44");
				ExpectSet(a - b, "11", "22");
				ExpectSet(a ^ b, "11", "22", "55");
			}
			{
				MSet<Symbol> a = new MSet<Symbol>(new[] { S("11"), S("22"), S("33"), S("44") });
				MSet<Symbol> b = new MSet<Symbol>(new[] { S("33"), S("44"), S("55") });

				ExpectSet(b + S("33"), S("33"), S("44"), S("55"));
				ExpectSet(b + S("BAM!"), S("33"), S("44"), S("55"), S("BAM!"));
				ExpectSet(S("Qué?") + b, S("33"), S("44"), S("55"), S("Qué?"));
				ExpectSet(b - S("Bob"), S("33"), S("44"), S("55"));
				ExpectSet(b - S("55"), S("33"), S("44"));

				ExpectSet(a, S("11"), S("22"), S("33"), S("44"));
				ExpectSet(b, S("33"), S("44"), S("55"));

				ExpectSet(a | b, S("11"), S("22"), S("33"), S("44"), S("55"));
				ExpectSet(a & b, S("33"), S("44"));
				ExpectSet(a - b, S("11"), S("22"));
				ExpectSet(a ^ b, S("11"), S("22"), S("55"));
			}
		}

		[Test]
		public void ImmutableSetBugsFixed()
		{
			// I was puzzled why, in the benchmark tests of adding items to sets,
			// adding items to Set using operator+ was almost as fast as 
			// adding items to MSet even though the former must duplicate 
			// nodes during every addition operation. The answer was that after
			// operator+ added a new item, it failed to freeze the new set, so
			// subsequent additions did not duplicate any nodes. Other operators
			// had the same bug.
			{
				Set<string> set = new Set<string>(new[] { "a", "b", "c", "d" });
				AreEqual(4, set.Count);
				Set<string> set2 = set + "foo", set3 = set2 + "bar", set4 = set2 + "baz";
				Set<string> set5 = set2 - "foo", set6 = set5 - "d", set7 = set6 - "c";
				ExpectSet(set, "a", "b", "c", "d");
				ExpectSet(set2, "a", "b", "c", "d", "foo");
				ExpectSet(set3, "a", "b", "c", "d", "foo", "bar");
				ExpectSet(set4, "a", "b", "c", "d", "foo", "baz");
				ExpectSet(set5, "a", "b", "c", "d");
				ExpectSet(set6, "a", "b", "c");
				ExpectSet(set7, "a", "b");

				set  = new Set<string>(new[] { "a", "b" });
				set2 = new Set<string>(new[] { "b", "c" });
				set3 = new Set<string>(new[] { "c", "d" });
				set4 = new Set<string>(new[] { "d", "a" });
				set5 = set & set2; // b
				set6 = set5 | set3; // b c d
				set7 = set6 - set4; // b c
				var set8 = set7 ^ set4; // a b c d
				ExpectSet(set, "a", "b");
				ExpectSet(set2, "b", "c");
				ExpectSet(set3, "c", "d");
				ExpectSet(set4, "d", "a");
				ExpectSet(set5, "b");
				ExpectSet(set6, "b", "c", "d");
				ExpectSet(set7, "b", "c");
				ExpectSet(set8, "a", "b", "c", "d");
			}
			{
				Set<Symbol> set = new Set<Symbol>(new[] { S("a"), S("b"), S("c"), S("d") });
				AreEqual(4, set.Count); // In Set<Symbol>, Count was 0
				Set<Symbol> set2 = set + S("foo"), set3 = set2 + S("bar"), set4 = set2 + S("baz");
				Set<Symbol> set5 = set2 - S("foo"), set6 = set5 - S("d"), set7 = set6 - S("c");
				ExpectSet(set, S("a"), S("b"), S("c"), S("d"));
				ExpectSet(set2, S("a"), S("b"), S("c"), S("d"), S("foo"));
				ExpectSet(set3, S("a"), S("b"), S("c"), S("d"), S("foo"), S("bar"));
				ExpectSet(set4, S("a"), S("b"), S("c"), S("d"), S("foo"), S("baz"));
				ExpectSet(set5, S("a"), S("b"), S("c"), S("d"));
				ExpectSet(set6, S("a"), S("b"), S("c"));
				ExpectSet(set7, S("a"), S("b"));

				set  = new Set<Symbol>(new[] { S("a"), S("b") });
				set2 = new Set<Symbol>(new[] { S("b"), S("c") });
				set3 = new Set<Symbol>(new[] { S("c"), S("d") });
				set4 = new Set<Symbol>(new[] { S("d"), S("a") });
				set5 = set & set2; // b
				set6 = set5 | set3; // b c d
				set7 = set6 - set4; // b c
				var set8 = set7 ^ set4; // a b c d
				ExpectSet(set, S("a"), S("b"));
				ExpectSet(set2, S("b"), S("c"));
				ExpectSet(set3, S("c"), S("d"));
				ExpectSet(set4, S("d"), S("a"));
				ExpectSet(set5, S("b"));
				ExpectSet(set6, S("b"), S("c"), S("d"));
				ExpectSet(set7, S("b"), S("c"));
				ExpectSet(set8, S("a"), S("b"), S("c"), S("d"));
			}
			// We may as well run the same test for MSet and MSet<Symbol>
			{
				MSet<string> set = new MSet<string>(new[] { "a", "b", "c", "d" });
				AreEqual(4, set.Count);
				MSet<string> set2 = set + "foo", set3 = set2 + "bar", set4 = set2 + "baz";
				MSet<string> set5 = set2 - "foo", set6 = set5 - "d", set7 = set6 - "c";
				ExpectSet(set, "a", "b", "c", "d");
				ExpectSet(set2, "a", "b", "c", "d", "foo");
				ExpectSet(set3, "a", "b", "c", "d", "foo", "bar");
				ExpectSet(set4, "a", "b", "c", "d", "foo", "baz");
				ExpectSet(set5, "a", "b", "c", "d");
				ExpectSet(set6, "a", "b", "c");
				ExpectSet(set7, "a", "b");

				set  = new MSet<string>(new[] { "a", "b" });
				set2 = new MSet<string>(new[] { "b", "c" });
				set3 = new MSet<string>(new[] { "c", "d" });
				set4 = new MSet<string>(new[] { "d", "a" });
				set5 = set & set2; // b
				set6 = set5 | set3; // b c d
				set7 = set6 - set4; // b c
				var set8 = set7 ^ set4; // a b c d
				ExpectSet(set, "a", "b");
				ExpectSet(set2, "b", "c");
				ExpectSet(set3, "c", "d");
				ExpectSet(set4, "d", "a");
				ExpectSet(set5, "b");
				ExpectSet(set6, "b", "c", "d");
				ExpectSet(set7, "b", "c");
				ExpectSet(set8, "a", "b", "c", "d");
			}
			{
				MSet<Symbol> set = new MSet<Symbol>(new[] { S("a"), S("b"), S("c"), S("d") });
				AreEqual(4, set.Count);
				MSet<Symbol> set2 = set + S("foo"), set3 = set2 + S("bar"), set4 = set2 + S("baz");
				MSet<Symbol> set5 = set2 - S("foo"), set6 = set5 - S("d"), set7 = set6 - S("c");
				ExpectSet(set, S("a"), S("b"), S("c"), S("d"));
				ExpectSet(set2, S("a"), S("b"), S("c"), S("d"), S("foo"));
				ExpectSet(set3, S("a"), S("b"), S("c"), S("d"), S("foo"), S("bar"));
				ExpectSet(set4, S("a"), S("b"), S("c"), S("d"), S("foo"), S("baz"));
				ExpectSet(set5, S("a"), S("b"), S("c"), S("d"));
				ExpectSet(set6, S("a"), S("b"), S("c"));
				ExpectSet(set7, S("a"), S("b"));

				set  = new MSet<Symbol>(new[] { S("a"), S("b") });
				set2 = new MSet<Symbol>(new[] { S("b"), S("c") });
				set3 = new MSet<Symbol>(new[] { S("c"), S("d") });
				set4 = new MSet<Symbol>(new[] { S("d"), S("a") });
				set5 = set & set2; // b
				set6 = set5 | set3; // b c d
				set7 = set6 - set4; // b c
				var set8 = set7 ^ set4; // a b c d
				ExpectSet(set, S("a"), S("b"));
				ExpectSet(set2, S("b"), S("c"));
				ExpectSet(set3, S("c"), S("d"));
				ExpectSet(set4, S("d"), S("a"));
				ExpectSet(set5, S("b"));
				ExpectSet(set6, S("b"), S("c"), S("d"));
				ExpectSet(set7, S("b"), S("c"));
				ExpectSet(set8, S("a"), S("b"), S("c"), S("d"));
			}
		}
	}
}
