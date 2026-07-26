using System;
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

			// NOTE: FPL32.operator/ is still broken and is deliberately not asserted
			// here. It computes (remainder << Frac) in Int64, which overflows for
			// Frac == 32; fixing it needs a 128-bit intermediate (as operator* already
			// uses via MathEx.MulShift). Division works for the smaller-Frac types:
			Assert.AreEqual(2.5, (double)((FPL16)11.25 / (FPL16)4.5));
			Assert.AreEqual(2.5, (double)((FPI23)11.25 / (FPI23)4.5));
		}
	}
}
