using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;
using Loyc.Collections;

namespace Loyc.LLParserGenerator
{
	[TestFixture]
	public class IntSetTests : Assert
	{
		[Test]
		public void BasicTests()
		{
			var ac = IntSet.WithCharRanges('A', 'C', '$', '$');
			AreEqual(ac, IntSet.With('$', '$', 'A', 'B', 'C'));
			var ad = IntSet.WithCharRanges('A', 'D');
			AreNotEqual(ad, IntSet.WithChars('A', 'B', 'C'));
			
			IsTrue(ac.Contains('$') && !ad.Contains('$'));
			IsTrue(ac.Contains('A') && ad.Contains('A'));
			IsTrue(ac.Contains('C') && ad.Contains('C'));
			IsTrue(!ac.Contains('D') && ad.Contains('D'));

			var ad2 = ac.Union(ad);
			IsTrue(ad2.Contains('$') && ad2.Contains('D'));
			CheckRanges(ad2, new IntRange('$'), new IntRange('A', 'D'));
			var ac2 = ac.Intersection(ad);
			CheckRanges(ac2, new IntRange('A', 'C'));
			CheckRanges(ac.Union(IntSet.With('@')), new IntRange('$'), new IntRange('@', 'C'));
		}

		void CheckRanges(IntSet set, params IntRange[] ranges)
		{
			AreEqual(ranges.Length, set.Count);
			for (int i = 0; i < ranges.Length; i++)
				AreEqual(ranges[i], set[i]);
		}

		[Test]
		public void EdgeCases()
		{
			var min = IntSet.With(int.MinValue);
			var max = IntSet.With(int.MaxValue);
			var min2 = IntSet.WithoutRanges(int.MinValue+1, int.MaxValue);
			var max2 = IntSet.WithoutRanges(int.MinValue, int.MaxValue-1);
			IsTrue(min.Equals(min2, IntSet.S_Equivalent));
			IsTrue(max.Equals(max2, IntSet.S_Equivalent));
			IsFalse(min.Equals(min2, IntSet.S_Identical));
			IsFalse(max.Equals(max2, IntSet.S_Identical));
			IsTrue(min.EquivalentInverted().Equals(min2, IntSet.S_Identical));
			IsTrue(max.EquivalentInverted().Equals(max2, IntSet.S_Identical));
			var minmax = min.Union(max);
			var minmax2 = min2.Union(max2);
			IsFalse(minmax.IsInverted);
			IsTrue(minmax2.IsInverted);
			CheckRanges(minmax, new IntRange(int.MinValue), new IntRange(int.MaxValue));
			CheckRanges(minmax2, new IntRange(int.MinValue+1, int.MaxValue-1));

			var none = IntSet.Empty;
			var all = IntSet.All;
			IsFalse(all.Equals(none, IntSet.S_Equivalent));
			IsTrue(all.Equals(none, IntSet.S_SameRangeList));
			CheckRanges(all);
			AreEqual(all, all.Union(none));
			AreEqual(all, all.Union(min));
			AreEqual(all, all.Union(min2));
			AreEqual(none, all.Intersection(none));
			AreEqual(min, all.Intersection(min));
			AreEqual(max, all.Intersection(max2));
			AreEqual(min, none.Union(min2));
			AreEqual(max, none.Union(max));
			AreEqual(none, min.Intersection(max));
			AreEqual(none, min2.Intersection(max2));

			IsTrue(min2.IsInverted && max2.IsInverted);
			min2 = min2.Inverted();
			max2 = max2.Inverted();
			AreEqual(all, min2.Union(max2));
			CheckRanges(min2.Intersection(max2), new IntRange(int.MinValue+1, int.MaxValue-1));
		}

		[Test]
		public void ParsingAndPrinting()
		{
			IntSet empty = IntSet.Empty, all = IntSet.All;
			PrintAndParse(empty, "()");
			empty.IsCharSet = true;
			PrintAndParse(empty, "[]");
			empty.IsCharSet = false; // design flaw here - mutable property on readonly static field 
			PrintAndParse(all, "~()");
			all.IsCharSet = true;
			PrintAndParse(all, "[^]");
			all.IsCharSet = false; // design flaw here - mutable property on readonly static field 

			AreEqual(IntSet.WithCharRanges('a', 'd'), IntSet.Parse("[dacb]"));
			AreEqual(IntSet.With(1234), IntSet.Parse("(1234)"));
			AreEqual(IntSet.With(0x1234, '!'), IntSet.Parse("[\u1234!]"));
			AreEqual(IntSet.With(0x12, '!'), IntSet.Parse("[\x12!]"));
			IsNull(IntSet.TryParse("(12345678901)"));
			PrintAndParse(IntSet.WithCharRanges('a', 'd'), "[a-d]");
			PrintAndParse(IntSet.WithChars('$', '-', '[', ']'), @"[$\-[\]]");
			PrintAndParse(IntSet.WithoutCharRanges(-1,-1, '\n','\n', '0','9', '^','^'), @"[^\$\n0-9^]");
			PrintAndParse(IntSet.With(2, 3, 5, 7, 11), "(2..3, 5, 7, 11)");
			PrintAndParse(IntSet.WithRanges(int.MinValue, 0), string.Format("({0}..0)", int.MinValue));
			PrintAndParse(IntSet.WithoutRanges(1, int.MaxValue), string.Format("~(1..{0})", int.MaxValue));

			// See the error? That's really a single backslash; to add backslash to 
			// the set we need another backslash.
			int errorIndex;
			var set = IntSet.TryParse("[^\n\\]", out errorIndex);
			AreEqual(errorIndex, 4);
			AreEqual(IntSet.WithoutChars('\n', '\\'), IntSet.Parse("[^\n\\\\]"));
		}

		private void PrintAndParse(IntSet set, string expect)
		{
			string s = set.ToString();
			AreEqual(expect, s);
			AreEqual(IntSet.Parse(expect), set);
		}

		[Test]
		public void RandomTests()
		{
			int seed = Environment.TickCount;
			for (int test = 0; test < 100; test++)
			{
				var r = new Random(seed + test);
				IntSet a = RandomIntSet(r), b = RandomIntSet(r);
				IsFalse(a.IsEmptySet);
				IsFalse(b.IsEmptySet);
				var union = a.Union(b);
				var intsc = a.Intersection(b);
				var dif   = a.Subtract(b);
				AreEqual(union.Equals(intsc), a.Equals(b));

				for (int i = -10; i < 20; i++) {
					bool ina = a.Contains(i), inb = b.Contains(i);
					AreEqual(ina || inb, union.Contains(i));
					AreEqual(ina && inb, intsc.Contains(i));
					AreEqual(ina && !inb, dif.Contains(i));
				}

				var eInv = a.EquivalentInverted();
				IsTrue(a.Equals(eInv, IntSet.S_Equivalent));
				IsFalse(a.Equals(eInv, IntSet.S_Identical));
				IsFalse(a.Equals(eInv, IntSet.S_SameRangeList));

				var inv = eInv.Clone();
				inv = inv.Inverted();
				IsFalse(inv.Equals(eInv));
				IsTrue(inv.Equals(eInv, IntSet.S_SameRangeList));

				IntSet all = a.Union(inv), none = a.Intersection(inv);
				IsFalse(all.IsEmptySet);
				IsTrue(none.IsEmptySet);
				IsFalse(all.Equals(none));
				AreEqual(all.IsInverted ? 0 : 1, all.Count);
				AreEqual(none.IsInverted ? 1 : 0, none.Count);
				AreEqual(all, all.Union(none));
				AreEqual(all, all.Union(a));
				AreEqual(none, all.Intersection(none));
				AreEqual(a, all.Intersection(a));
				AreEqual(a, a.Subtract(none));
				
				all = all.Inverted();
				IsTrue(all.Equals(none));
			}
		}

		IntRange[] scratch = new IntRange[29];
		IntSet RandomIntSet(Random r)
		{
			int i, lo, scale = 4;
			for (i = 0; ; i++, scale <<= 1)
			{
				scratch[i] = new IntRange(lo = r.Next(scale << 1) - (scale >> 1), lo + r.Next(scale));
				if (r.Next(2) != 0 || i+1 == scratch.Length)
					break;
			}
			return new IntSet(false, r.Next(3) == 0, scratch.Slice(0, i+1).ToArray());
		}

		[Test]
		public void Regressions()
		{
			IntSet a = IntSet.Parse("[^ \t\r\n\\\\]"), b = IntSet.Parse("[ \t]");
			AreEqual(IntSet.Parse("[^\r\n\\\\]"), b.Union(a));
			AreEqual(IntSet.Parse("[^\r\n\\\\]"), a.Union(b)); // a bug here produced [^]
		}
	}
}
