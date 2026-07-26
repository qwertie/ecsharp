using System;
using System.Numerics;
using Loyc.MiniTest;

namespace Loyc.Math
{
	/// <summary>Tests for the generated fixed-point types. FPL32 (the only type whose
	/// Frac equals 32) was entirely broken because "1 &lt;&lt; Frac" was evaluated in
	/// int arithmetic, so the shift count was masked to 0 and Unit came out as 1.</summary>
	[TestFixture]
	public class FixedPointTests : Assert
	{
		[Test]
		public void UnitAndMaskAreCorrect()
		{
			Assert.AreEqual(1 << 8, FPI8.Unit);
			Assert.AreEqual(1 << 16, FPI16.Unit);
			Assert.AreEqual(1 << 23, FPI23.Unit);
			Assert.AreEqual(1L << 16, FPL16.Unit);
			Assert.AreEqual(1L << 32, FPL32.Unit); // was 1

			Assert.AreEqual((1 << 8) - 1, FPI8.Mask);
			Assert.AreEqual((1 << 23) - 1, FPI23.Mask);
			Assert.AreEqual((1L << 16) - 1, FPL16.Mask);
			Assert.AreEqual((1L << 32) - 1, FPL32.Mask); // was 0
		}

		[Test]
		public void DoubleRoundTrip()
		{
			foreach (double d in new[] { 0.0, 1.0, 3.5, -7.25, 100.125, -0.5 }) {
				Assert.AreEqual(d, (double)(FPL32)d, "FPL32 " + d);
				Assert.AreEqual(d, (double)(FPL16)d, "FPL16 " + d);
				Assert.AreEqual(d, (double)(FPI23)d, "FPI23 " + d);
				Assert.AreEqual(d, (double)(FPI16)d, "FPI16 " + d);
			}
			Assert.AreEqual(3.5f, (float)(FPL32)3.5, "FPL32 float cast");
			// The raw representation of 1.0 is exactly Unit
			Assert.AreEqual(FPL32.Unit, ((FPL32)1.0).N);
			Assert.AreEqual(FPL16.Unit, ((FPL16)1.0).N);
		}

		[Test]
		public void MaxDoubleIsInRange()
		{
			// MaxDouble was Int64.MaxValue / 1.0 for FPL32, i.e. 9.2e18 instead of ~2.1e9
			Assert.IsTrue(FPL32.MaxDouble > 2.1e9 && FPL32.MaxDouble < 2.2e9,
				"FPL32.MaxDouble = " + FPL32.MaxDouble);
			Assert.IsTrue(FPL32.MinDouble < -2.1e9 && FPL32.MinDouble > -2.2e9,
				"FPL32.MinDouble = " + FPL32.MinDouble);
			Assert.IsTrue(FPL16.MaxDouble > 1.4e14 && FPL16.MaxDouble < 1.5e14,
				"FPL16.MaxDouble = " + FPL16.MaxDouble);
		}

		[Test]
		public void FloorCeilingAndIncrement()
		{
			// Floor/Ceiling use Mask, and ++/-- use Unit; all were no-ops for FPL32
			Assert.AreEqual(3.0, (double)((FPL32)3.75).Floor());
			Assert.AreEqual(4.0, (double)((FPL32)3.75).Ceiling());
			Assert.AreEqual(-4.0, (double)((FPL32)(-3.75)).Floor());
			Assert.AreEqual(-3.0, (double)((FPL32)(-3.75)).Ceiling());

			var v = (FPL32)5.5;
			v++;
			Assert.AreEqual(6.5, (double)v);
			v--; v--;
			Assert.AreEqual(4.5, (double)v);
		}

		[Test]
		public void Arithmetic()
		{
			Assert.AreEqual(7.0, (double)((FPL32)2.5 + (FPL32)4.5));
			Assert.AreEqual(-2.0, (double)((FPL32)2.5 - (FPL32)4.5));
			Assert.AreEqual(11.25, (double)((FPL32)2.5 * (FPL32)4.5));
			Assert.IsTrue((FPL32)2.5 < (FPL32)4.5);
			Assert.AreEqual(7.0, (double)((FPL16)2.5 + (FPL16)4.5));
			Assert.AreEqual(11.25, (double)((FPI23)2.5 * (FPI23)4.5));

			Assert.AreEqual(2.5, (double)((FPL32)11.25 / (FPL32)4.5));
			Assert.AreEqual(2.5, (double)((FPL16)11.25 / (FPL16)4.5));
			Assert.AreEqual(2.5, (double)((FPI23)11.25 / (FPI23)4.5));
		}

		/// <summary>Exact expected value of a/b for a Frac-bit fixed-point type: the
		/// 128-bit quotient (a.N &lt;&lt; Frac) / b.N, truncated toward zero and wrapped
		/// to 64 bits (the same thing FPI16.operator/ does with 32-bit N).</summary>
		static long ExpectedDivN(long aN, long bN, int frac)
		{
			BigInteger q = BigInteger.Divide(new BigInteger(aN) << frac, bN);
			return unchecked((long)(ulong)(q & ulong.MaxValue));
		}

		[Test]
		public void FPL32Division()
		{
			// The old code did (a.N % b.N) << Frac in Int64, which overflows for
			// FPL32 whenever the remainder is >= 0.5, i.e. for most divisors >= 0.5.
			Assert.AreEqual(0.5, (double)((FPL32)1.0 / (FPL32)2.0));  // was 0
			Assert.AreEqual(1.5, (double)((FPL32)3.0 / (FPL32)2.0));  // was 1
			Assert.AreEqual(0.5, (double)((FPL32)0.25 / (FPL32)0.5));
			Assert.AreEqual(4.0, (double)((FPL32)10.0 / (FPL32)2.5));

			// 2/3 = 0.10101...b, truncated toward zero
			Assert.AreEqual(2L * (1L << 32) / 3, ((FPL32)2.0 / (FPL32)3.0).N);

			// 7 / 2.5 = 2.8 is not representable; the exact result truncates toward zero
			long q = 2L * 7 * (1L << 32) / 5;
			Assert.AreEqual(q, ((FPL32)7.0 / (FPL32)2.5).N);
			Assert.AreEqual(-q, ((FPL32)7.0 / (FPL32)(-2.5)).N);
			Assert.AreEqual(-q, ((FPL32)(-7.0) / (FPL32)2.5).N);
			Assert.AreEqual(q, ((FPL32)(-7.0) / (FPL32)(-2.5)).N);
		}

		[Test]
		public void FPL16Division()
		{
			Assert.AreEqual(0.5, (double)((FPL16)1.0 / (FPL16)2.0));
			Assert.AreEqual(1.5, (double)((FPL16)3.0 / (FPL16)2.0));
			Assert.AreEqual(2L * (1L << 16) / 3, ((FPL16)2.0 / (FPL16)3.0).N);

			long q = 2L * 7 * (1L << 16) / 5;
			Assert.AreEqual(q, ((FPL16)7.0 / (FPL16)2.5).N);
			Assert.AreEqual(-q, ((FPL16)7.0 / (FPL16)(-2.5)).N);
			Assert.AreEqual(-q, ((FPL16)(-7.0) / (FPL16)2.5).N);
			Assert.AreEqual(q, ((FPL16)(-7.0) / (FPL16)(-2.5)).N);
		}

		[Test]
		public void DivisionOverflowWrapsLikeFPI16()
		{
			// FPI16 wraps when the quotient doesn't fit in N; FPL16/FPL32 must do the same
			// x / Epsilon == x.N << Frac, which is exactly 2^(bits of N) in these cases
			Assert.AreEqual(0, (FPI16.One / FPI16.Epsilon).N);
			Assert.AreEqual(0L, (FPL32.One / FPL32.Epsilon).N);
			Assert.AreEqual(0L, (FPL16.Prescaled(1L << 48) / FPL16.Epsilon).N);

			// One extra Epsilon in the numerator survives the wrap as one whole unit
			Assert.AreEqual(1 << 16, (FPI16.Prescaled((1 << 16) + 1) / FPI16.Epsilon).N);
			Assert.AreEqual(1L << 32, (FPL32.Prescaled((1L << 32) + 1) / FPL32.Epsilon).N);
			Assert.AreEqual(1L << 16, (FPL16.Prescaled((1L << 48) + 1) / FPL16.Epsilon).N);
		}

		[Test]
		public void DivisionMatchesExact128BitSemantics()
		{
			long[] operands = {
				1, -1, 2, -2, 3, 12345, -12345, (1L << 16) - 1, 1L << 16, (1L << 31) - 1,
				1L << 31, -(1L << 31), (1L << 32) + 1, -3L << 32, 1L << 47, long.MaxValue,
				long.MinValue, long.MinValue + 1, 0x0123456789ABCDEFL, -0x7EDCBA9876543210L,
			};
			foreach (long aN in operands) {
				foreach (long bN in operands) {
					Assert.AreEqual(ExpectedDivN(aN, bN, 32),
						(FPL32.Prescaled(aN) / FPL32.Prescaled(bN)).N, "FPL32 " + aN + "/" + bN);
					Assert.AreEqual(ExpectedDivN(aN, bN, 16),
						(FPL16.Prescaled(aN) / FPL16.Prescaled(bN)).N, "FPL16 " + aN + "/" + bN);
				}
			}
		}

		[Test]
		public void FPL32CheckedCastFromUInt()
		{
			// This overload was missing: the .tt thought FPL32.MaxInt was Int64.MaxValue
			Assert.AreEqual(1L << 32, FPL32.CheckedCast((uint)1).N);
			Assert.AreEqual((long)int.MaxValue << 32, FPL32.CheckedCast((uint)int.MaxValue).N);
			Assert.ThrowsAny<OverflowException>(() => FPL32.CheckedCast((uint)int.MaxValue + 1));
			Assert.ThrowsAny<OverflowException>(() => FPL32.CheckedCast(uint.MaxValue));
		}
	}
}
