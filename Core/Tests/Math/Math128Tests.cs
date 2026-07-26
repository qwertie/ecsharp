using System;
using System.Numerics;
using Loyc.MiniTest;

namespace Loyc.Math
{
	/// <summary>Tests for <see cref="Math128"/> and the 64-bit overloads of
	/// MathEx.MulDiv/MulShift that are built on it. This code had no test coverage,
	/// which is how three shift-constant bugs survived in it.</summary>
	[TestFixture]
	public class Math128Tests : Assert
	{
		static readonly BigInteger Two64 = BigInteger.One << 64;

		static BigInteger Combine(ulong hi, ulong lo) => (BigInteger)hi * Two64 + lo;
		static BigInteger Combine(long hi, ulong lo) => (BigInteger)hi * Two64 + lo;

		// Deterministic full-range 64-bit values (Random.Next never sets the high bit,
		// which is exactly why the original spot checks missed the Multiply bug).
		static ulong[] Samples()
		{
			var r = new Random(12345);
			var buf = new byte[8];
			var list = new ulong[400];
			for (int i = 0; i < list.Length; i++) {
				r.NextBytes(buf);
				list[i] = BitConverter.ToUInt64(buf, 0);
			}
			list[0] = ulong.MaxValue;
			list[1] = 0;
			list[2] = 1;
			list[3] = 0x8000000000000000UL;
			list[4] = 0xFFFFFFFFUL;
			list[5] = 0x100000000UL;
			return list;
		}

		[Test]
		public void MultiplyUnsigned()
		{
			// Regression: the carry out of the middle partial product was added as
			// "1 << 32", but 1 is an int literal so C# masked the shift to 0 and added
			// 1 instead of 2^32. This was wrong for ~7% of random 64-bit pairs.
			ulong hi;
			ulong lo = Math128.Multiply(ulong.MaxValue, ulong.MaxValue, out hi);
			Assert.AreEqual(0xFFFFFFFFFFFFFFFEUL, hi);
			Assert.AreEqual(1UL, lo);

			var s = Samples();
			for (int i = 0; i < s.Length; i++)
				for (int j = 0; j < s.Length; j += 7) {
					lo = Math128.Multiply(s[i], s[j], out hi);
					Assert.AreEqual((BigInteger)s[i] * s[j], Combine(hi, lo));
				}
		}

		[Test]
		public void MultiplySigned()
		{
			var s = Samples();
			for (int i = 0; i < s.Length; i++)
				for (int j = 0; j < s.Length; j += 7) {
					long a = (long)s[i], b = (long)s[j];
					long hi;
					ulong lo = Math128.Multiply(a, b, out hi);
					Assert.AreEqual((BigInteger)a * b, Combine(hi, lo));
				}
			// long.MinValue used to be negated into itself by the sign-handling code
			{
				long hi;
				ulong lo = Math128.Multiply(long.MinValue, long.MinValue, out hi);
				Assert.AreEqual((BigInteger)long.MinValue * long.MinValue, Combine(hi, lo));
			}
		}

		[Test]
		public void Divide128By32()
		{
			// Regression: the 128/32-bit branch assembled its result with
			// "(ulong)(result2 << 32)". result2 is a uint, so the shift was masked to 0
			// and the high half of the quotient was dropped.
			ulong rh, rem;
			ulong rl = Math128.Divide(1, 0, 3, out rh, out rem);
			Assert.AreEqual(6148914691236517205UL, rl);
			Assert.AreEqual(0UL, rh);
			Assert.AreEqual(1UL, rem);

			var s = Samples();
			for (int i = 0; i < s.Length; i++) {
				ulong aH = s[i], aL = s[(i * 3 + 1) % s.Length];
				uint b = (uint)(s[(i * 5 + 2) % s.Length] | 1); // 32-bit, nonzero
				rl = Math128.Divide(aH, aL, b, out rh, out rem);
				BigInteger a = Combine(aH, aL);
				Assert.AreEqual(a / b, Combine(rh, rl));
				Assert.AreEqual(a % b, (BigInteger)rem);
			}
		}

		[Test]
		public void Divide128By64()
		{
			// Exercises the shift-and-subtract branch (divisor > 2^32), including the
			// "Optimization 1" fast-forward whose four "1 << (64-n)" tests were dead.
			var s = Samples();
			for (int i = 0; i < s.Length; i++) {
				ulong aH = s[i], aL = s[(i * 3 + 1) % s.Length];
				ulong b = s[(i * 7 + 3) % s.Length] | 0x100000000UL; // > 2^32
				ulong rh, rem;
				ulong rl = Math128.Divide(aH, aL, b, out rh, out rem);
				BigInteger a = Combine(aH, aL);
				Assert.AreEqual(a / b, Combine(rh, rl));
				Assert.AreEqual(a % b, (BigInteger)rem);
			}
			// Dividends with a small high word take the fast-forward path
			for (int e = 0; e < 32; e++) {
				ulong aH = 1UL << e, aL = 0x0123456789ABCDEFUL, b = 0x123456789ABCUL;
				ulong rh, rem;
				ulong rl = Math128.Divide(aH, aL, b, out rh, out rem);
				BigInteger a = Combine(aH, aL);
				Assert.AreEqual(a / b, Combine(rh, rl));
				Assert.AreEqual(a % b, (BigInteger)rem);
			}
		}

		[Test]
		public void MulDivAndMulShift()
		{
			ulong rem;
			Assert.AreEqual(6148914691236517205UL, MathEx.MulDiv(1UL << 32, 1UL << 32, 3, out rem));
			Assert.AreEqual(1UL, rem);
			Assert.AreEqual(0xFFFFFFFFFFFFFFFEUL, MathEx.MulShift(ulong.MaxValue, ulong.MaxValue, 64));

			var s = Samples();
			for (int i = 0; i < s.Length; i++) {
				ulong a = s[i], m = s[(i * 3 + 1) % s.Length], d = s[(i * 5 + 2) % s.Length] | 1;
				BigInteger expectQ = (BigInteger)a * m / d;
				ulong got = MathEx.MulDiv(a, m, d, out rem);
				if (expectQ <= ulong.MaxValue) {
					Assert.AreEqual(expectQ, (BigInteger)got);
					Assert.AreEqual((BigInteger)a * m % d, (BigInteger)rem);
				} else
					Assert.AreEqual(ulong.MaxValue, got); // saturates, per the docs

				for (int shift = 0; shift < 64; shift += 13)
					Assert.AreEqual((ulong)(((BigInteger)a * m >> shift) & ulong.MaxValue),
									MathEx.MulShift(a, m, shift));
			}
		}

		[Test]
		public void ShiftAndAdd()
		{
			ulong aL = 0xFFFFFFFFFFFFFFFFUL;
			ulong aH = Math128.ShiftLeft(0, ref aL, 1);
			Assert.AreEqual(Combine(aH, aL), (BigInteger)0xFFFFFFFFFFFFFFFFUL << 1);

			aL = 1; aH = Math128.Add(0, ref aL, ulong.MaxValue);
			Assert.AreEqual(Combine(aH, aL), (BigInteger)1 + ulong.MaxValue);

			aL = 0; aH = Math128.Subtract(1UL, ref aL, 1UL);
			Assert.AreEqual(Combine(aH, aL), Two64 - 1);
		}
	}
}
