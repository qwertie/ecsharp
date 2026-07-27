using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;

namespace Loyc.Math
{
	[TestFixture]
	public class MathExTests : Assert
	{
		[Test]
		public void TestMulDivSaturates()
		{
			// The docs always promised saturation, but the int/uint overloads wrapped
			AreEqual(6, MathEx.MulDiv(4, 3, 2));
			AreEqual(-6, MathEx.MulDiv(4, -3, 2, out int r) + r);
			AreEqual(int.MaxValue, MathEx.MulDiv(int.MaxValue, 3, 2));
			AreEqual(int.MinValue, MathEx.MulDiv(int.MaxValue, -3, 2));
			AreEqual(int.MaxValue, MathEx.MulDiv(int.MaxValue, 2, 1, out r));
			AreEqual(0, r);
			AreEqual(uint.MaxValue, MathEx.MulDiv(uint.MaxValue, 3u, 2u));
			AreEqual(uint.MaxValue, MathEx.MulDiv(uint.MaxValue, 3u, 2u, out uint ur));
			AreEqual(1u, ur);
			AreEqual(long.MaxValue, MathEx.MulDiv(long.MaxValue, 3, 2));
			AreEqual(long.MinValue, MathEx.MulDiv(long.MaxValue, -3, 2));
		}

		[Test]
		public void TestInRange()
		{
			Assert.IsFalse(1.IsInRange(2, 5));
			Assert.IsTrue(2.IsInRange(2, 5));
			Assert.IsTrue(3.IsInRange(2, 5));
			Assert.IsTrue(4.IsInRange(2, 5));
			Assert.IsTrue(5.IsInRange(2, 5));
			Assert.IsFalse(6.IsInRange(2, 5));
			Assert.IsFalse(2.IsInRange(5, 2));
			Assert.IsFalse(3.IsInRange(5, 2));
			Assert.IsFalse(5.IsInRange(5, 2));
		}
		[Test]
		public void PutInRange()
		{
			Assert.AreEqual(2, (-1).PutInRange(2, 5));
			Assert.AreEqual(2,    1.PutInRange(2, 5));
			Assert.AreEqual(2,    2.PutInRange(2, 5));
			Assert.AreEqual(3,    3.PutInRange(2, 5));
			Assert.AreEqual(4,    4.PutInRange(2, 5));
			Assert.AreEqual(5,    5.PutInRange(2, 5));
			Assert.AreEqual(5,    6.PutInRange(2, 5));
		}

		[Test]
		public void IsPrime()
		{
			var knownprimes = new int[] { // Primes up to 1000
				  2,   3,   5,   7,  11,  13,  17,  19,  23,  29,
				 31,  37,  41,  43,  47,  53,  59,  61,  67,  71,
				 73,  79,  83,  89,  97, 101, 103, 107, 109, 113,
				127, 131, 137, 139, 149, 151, 157, 163, 167, 173,
				179, 181, 191, 193, 197, 199, 211, 223, 227, 229,
				233, 239, 241, 251, 257, 263, 269, 271, 277, 281,
				283, 293, 307, 311, 313, 317, 331, 337, 347, 349,
				353, 359, 367, 373, 379, 383, 389, 397, 401, 409,
				419, 421, 431, 433, 439, 443, 449, 457, 461, 463,
				467, 479, 487, 491, 499, 503, 509, 521, 523, 541,
				547, 557, 563, 569, 571, 577, 587, 593, 599, 601,
				607, 613, 617, 619, 631, 641, 643, 647, 653, 659,
				661, 673, 677, 683, 691, 701, 709, 719, 727, 733,
				739, 743, 751, 757, 761, 769, 773, 787, 797, 809,
				811, 821, 823, 827, 829, 839, 853, 857, 859, 863,
				877, 881, 883, 887, 907, 911, 919, 929, 937, 941,
				947, 953, 967, 971, 977, 983, 991, 997,//1009,1013 
			};
			for (int i = 0; i < 1000; i++)
				Assert.AreEqual(knownprimes.Contains(i), MathEx.IsPrime(i));
		}

		#region Regression tests

		static ulong Isqrt(ulong v) // reference integer square root
		{
			ulong r = (ulong)System.Math.Sqrt(v);
			if (r > uint.MaxValue) r = uint.MaxValue;
			while (r > 0 && r > v / r) r--;
			while (r < uint.MaxValue && (r + 1) <= v / (r + 1)) r++;
			return r;
		}
		static ulong RoRRef(ulong v, int amt) => amt == 0 ? v : (v >> amt) | (v << (64 - amt));
		static ulong RoLRef(ulong v, int amt) => amt == 0 ? v : (v << amt) | (v >> (64 - amt));

		[Test]
		public void TestRotate()
		{
			// Regression: RoR(long, int) rotated by (32 - amt), so it was wrong for
			// every input; RoL(long) was correct.
			Assert.AreEqual(0x0123456789ABCDEFL, MathEx.RoR(0x123456789ABCDEF0L, 4));
			Assert.AreEqual(0x23456789ABCDEF01L, MathEx.RoL(0x123456789ABCDEF0L, 4));
			Assert.AreEqual(-1L, MathEx.RoR(-1L, 63));
			Assert.AreEqual(unchecked((long)0x8000000000000000UL), MathEx.RoR(1L, 1));

			for (int amt = 0; amt < 64; amt++) {
				ulong v = 0xDEADBEEFCAFEBABEUL;
				Assert.AreEqual(RoRRef(v, amt), MathEx.RoR(v, amt));
				Assert.AreEqual(RoLRef(v, amt), MathEx.RoL(v, amt));
				Assert.AreEqual((long)RoRRef(v, amt), MathEx.RoR((long)v, amt));
				Assert.AreEqual((long)RoLRef(v, amt), MathEx.RoL((long)v, amt));
			}
			for (int amt = 0; amt < 32; amt++) {
				uint v = 0xDEADBEEFu;
				uint rr = amt == 0 ? v : (v >> amt) | (v << (32 - amt));
				uint rl = amt == 0 ? v : (v << amt) | (v >> (32 - amt));
				Assert.AreEqual(rr, MathEx.RoR(v, amt));
				Assert.AreEqual(rl, MathEx.RoL(v, amt));
			}
		}

		[Test]
		public void TestSqrt()
		{
			// Regression: Sqrt(ulong) computed (g + g + b) in uint arithmetic, which
			// overflowed once value >= 2^62, so results above that were garbage.
			Assert.AreEqual(3037000499u, MathEx.Sqrt((ulong)long.MaxValue));
			Assert.AreEqual(4294967295u, MathEx.Sqrt(ulong.MaxValue));
			Assert.AreEqual(2147483648u, MathEx.Sqrt((1UL << 62) + 1));

			for (int e = 0; e < 64; e++) {
				ulong v = 1UL << e;
				Assert.AreEqual((uint)Isqrt(v), MathEx.Sqrt(v));
				Assert.AreEqual((uint)Isqrt(v - 1), MathEx.Sqrt(v - 1));
			}
			for (uint i = 0; i < 1000; i++) {
				Assert.AreEqual((uint)Isqrt(i), MathEx.Sqrt(i));
				Assert.AreEqual((uint)Isqrt(i * i), MathEx.Sqrt(i * i));
			}
			Assert.AreEqual(0u, MathEx.Sqrt(0UL));
			Assert.AreEqual(0, MathEx.Sqrt(0));
			Assert.Throws<ArgumentOutOfRangeException>(() => MathEx.Sqrt(-1));
			Assert.Throws<ArgumentOutOfRangeException>(() => MathEx.Sqrt(-1L));
		}

		[Test]
		public void TestNextHigherAndNextLower()
		{
			// Regression: NextLower(double) was a copy of NextHigher(double) (its two
			// branches were never swapped), so it returned a *higher* number.
			Assert.AreEqual(0.9999999999999999, MathEx.NextLower(1.0));
			Assert.AreEqual(1.0000000000000002, MathEx.NextHigher(1.0));
			Assert.AreEqual(-1.0000000000000002, MathEx.NextLower(-1.0));
			Assert.AreEqual(-0.9999999999999999, MathEx.NextHigher(-1.0));

			foreach (double d in new[] { 1.0, -1.0, 123.456, -0.5, 1e-300, 1e300 }) {
				Assert.AreEqual(d, MathEx.NextLower(MathEx.NextHigher(d)));
				Assert.AreEqual(d, MathEx.NextHigher(MathEx.NextLower(d)));
				Assert.IsTrue(MathEx.NextLower(d) < d && d < MathEx.NextHigher(d));
			}
			foreach (float f in new[] { 1.0f, -1.0f, 123.456f, -0.5f }) {
				Assert.AreEqual(f, MathEx.NextLower(MathEx.NextHigher(f)));
				Assert.IsTrue(MathEx.NextLower(f) < f && f < MathEx.NextHigher(f));
			}

			Assert.AreEqual(double.Epsilon, MathEx.NextHigher(0.0));
			Assert.AreEqual(-double.Epsilon, MathEx.NextLower(0.0));
			Assert.AreEqual(0.0, MathEx.NextLower(double.Epsilon));

			// Infinities and NaN are returned unchanged (this differs from
			// Math.BitIncrement/BitDecrement and is the documented behaviour here)
			Assert.AreEqual(double.PositiveInfinity, MathEx.NextHigher(double.MaxValue));
			Assert.AreEqual(double.PositiveInfinity, MathEx.NextHigher(double.PositiveInfinity));
			Assert.AreEqual(double.PositiveInfinity, MathEx.NextLower(double.PositiveInfinity));
			Assert.AreEqual(double.NegativeInfinity, MathEx.NextHigher(double.NegativeInfinity));
			Assert.IsTrue(double.IsNaN(MathEx.NextHigher(double.NaN)));
			Assert.IsTrue(double.IsNaN(MathEx.NextLower(double.NaN)));
			Assert.AreEqual(float.PositiveInfinity, MathEx.NextHigher(float.MaxValue));
			Assert.AreEqual(float.PositiveInfinity, MathEx.NextLower(float.PositiveInfinity));
		}

		[Test]
		public void TestLog2FloorAndCountOnes()
		{
			Assert.AreEqual(-1, MathEx.Log2Floor(0u));
			Assert.AreEqual(-1, MathEx.Log2Floor(0UL));
			Assert.AreEqual(-1, MathEx.Log2Floor(0));
			Assert.AreEqual(-1, MathEx.Log2Floor(-5));
			Assert.AreEqual(10, MathEx.Log2Floor(1024u));
			Assert.AreEqual(9, MathEx.Log2Floor(1000u));
			for (int e = 0; e < 32; e++) {
				Assert.AreEqual(e, MathEx.Log2Floor(1u << e));
				if (e > 0) Assert.AreEqual(e - 1, MathEx.Log2Floor((1u << e) - 1));
			}
			for (int e = 0; e < 64; e++)
				Assert.AreEqual(e, MathEx.Log2Floor(1UL << e));

			Assert.AreEqual(0, MathEx.CountOnes(0u));
			Assert.AreEqual(32, MathEx.CountOnes(uint.MaxValue));
			Assert.AreEqual(64, MathEx.CountOnes(ulong.MaxValue));
			Assert.AreEqual(4, MathEx.CountOnes(0xF0u));
			Assert.AreEqual((byte)8, MathEx.CountOnes((byte)255));
			Assert.AreEqual(16, MathEx.CountOnes((ushort)0xFFFF));
			for (int e = 0; e < 32; e++)
				Assert.AreEqual(e, MathEx.CountOnes((1u << e) - 1));
		}

		#endregion
	}
}
