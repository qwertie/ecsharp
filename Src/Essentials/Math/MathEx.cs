using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Essentials;

namespace Loyc.Math
{
	public static class MathEx
	{
		public static int InRange(this int n, int min, int max)
		{
			if (n < min)
				return min;
			if (n > max)
				return max;
			return n;
		}
		public static long InRange(this long n, long min, long max)
		{
			if (n < min)
				return min;
			if (n > max)
				return max;
			return n;
		}
		public static double InRange(this double n, double min, double max)
		{
			if (n < min)
				return min;
			if (n > max)
				return max;
			return n;
		}
		public static float InRange(this float n, float min, float max)
		{
			if (n < min)
				return min;
			if (n > max)
				return max;
			return n;
		}
		public static bool IsInRange(this int n, int min, int max)
		{
			return n >= min && n <= max;
		}
		public static bool IsInRange(this long n, long min, long max)
		{
			return n >= min && n <= max;
		}
		public static bool IsInRange(this double n, double min, double max)
		{
			return n >= min && n <= max;
		}
		public static bool IsInRange(this float n, float min, float max)
		{
			return n >= min && n <= max;
		}

		////////////////////////////////////////////////////////////////////////////////
		/// Algorithms from http://aggregate.org/MAGIC and
		/// http://www.devmaster.net/articles/fixed-point-optimizations/

		public static uint Sqrt(ulong value)
		{
			if (value == 0)
				return 0;

			uint g = 0;
			int bshft = Log2Floor(value) >> 1;
			uint b = 1u << bshft;
			do {
				ulong temp = ((ulong)(g + g + b) << bshft);

				if (value >= temp)
				{
					g += b;
					value -= temp;
				}
				b >>= 1;
			} while (bshft-- > 0);

			return g;
		}
		public static uint Sqrt(uint value)
		{
			if (value == 0)
				return 0;

			uint g = 0;
			int bshft = Log2Floor(value) >> 1;
			uint b = 1u << bshft;
			do {
				uint temp = (g + g + b) << bshft;
				if (value >= temp)
				{
					g += b;
					value -= temp;
				}
				b >>= 1;
			} while (bshft-- > 0);

			return g;
		}

		/// <summary>Returns the number of 'on' bits in x</summary>
		public static int CountOnes(byte x)
		{
			int X = x;
			X -= ((X >> 1) & 0x55);
			X = (((X >> 2) & 0x33) + (X & 0x33));
			return (X & 0x0F) + (X >> 4);
		}
		public static int CountOnes(ushort x)
		{
			int X = x;
			X -= ((X >> 1) & 0x5555);
			X = (((X >> 2) & 0x3333) + (X & 0x3333));
			X = (((X >> 4) + X) & 0x0f0f);
			X += (X >> 8);
			return (X & 0x001f);
		}
		public static int CountOnes(int x) { return CountOnes((uint)x); }
		public static int CountOnes(uint x)
		{
			/* 
			 * 32-bit recursive reduction using SWAR... but first step 
			 * is mapping 2-bit values into sum of 2 1-bit values in 
			 * sneaky way
			 */
			x -= ((x >> 1) & 0x55555555);
			x = (((x >> 2) & 0x33333333) + (x & 0x33333333));
			x = (((x >> 4) + x) & 0x0f0f0f0f);
			x += (x >> 8);
			x += (x >> 16);
			return (int)(x & 0x0000003f);
		}
		public static int CountOnes(long x) { return CountOnes((ulong)x); }
		public static int CountOnes(ulong x)
		{
			x -= ((x >> 1) & 0x5555555555555555u);
			x = (((x >> 2) & 0x3333333333333333u) + (x & 0x3333333333333333u));
			x = (((x >> 4) + x) & 0x0f0f0f0f0f0f0f0fu);
			x += (x >> 8);
			x += (x >> 16);
			int x32 = (int)x + (int)(x >> 32);
			return (int)(x32 & 0x0000007f);
		}

		/// <summary>
		/// Returns the floor of the base-2 logarithm of x. e.g. 1024 -> 10, 1000 -> 9
		/// </summary><remarks>
		/// The return value is -1 for an input of zero (for which the logarithm is 
		/// technically undefined.)
		/// </remarks>
		public static int Log2Floor(uint x)
		{
			x |= (x >> 1);
			x |= (x >> 2);
			x |= (x >> 4);
			x |= (x >> 8);
			x |= (x >> 16);
			return (CountOnes(x) - 1);
		}
		public static int Log2Floor(int x)
		{
			if (x < 0)
				throw new ArgumentException(Localize.From("Log2Floor({0}) called", x));
			return Log2Floor((uint)x);
		}
		public static int Log2Floor(ulong x)
		{
			uint xHi = (uint)(x >> 32);
			if (xHi != 0)
				return 32 + Log2Floor(xHi);
			return Log2Floor((uint)x);
		}
		public static int Log2Floor(long x)
		{
			if (x < 0)
				throw new ArgumentException(Localize.From("Log2Floor({0}) called", x));
			return Log2Floor((ulong)x);
		}

		/// <summary>Returns the bit position of the first '1' bit in a uint, or -1 
		/// the input is zero.</summary>
		public static int FindFirstOne(uint i)
		{
			int result = 0;
			if ((ushort)i == 0)
			{
				i >>= 16;
				result += 16;
			}
			if ((byte)i == 0)
			{
				i >>= 8;
				result += 8;
			}
			if ((i & 0xF) == 0)
			{
				i >>= 4;
				result += 4;
			}
			if ((i & 3) == 0)
			{
				i >>= 2;
				result += 2;
			}
			if ((i & 1) == 0)
			{
				result += 1;
				if ((i & 2) == 0)
				{
					Debug.Assert(result == 31);
					return -1;
				}
			}
			return result;
		}

		/// <summary>Returns the bit position of the first '0' bit in a uint, or -1
		/// if there are no zeros.</summary>
		public static int FindFirstZero(uint i)
		{
			return FindFirstOne(~i);
		}

		/// <summary>Returns the bit position of the first '1' bit in a uint, or -1 
		/// the input is zero.</summary>
		public static int FindLastOne(uint i)
		{
			int result = 31;
			if (i >> 16 == 0)
			{
				i <<= 16;
				result -= 16;
			}
			if (i >> 24 == 0)
			{
				i <<= 8;
				result -= 8;
			}
			if (i >> 28 == 0)
			{
				i <<= 4;
				result -= 4;
			}
			if (i >> 30 == 0)
			{
				i <<= 2;
				result -= 2;
			}
			if (i >> 31 == 0)
			{
				result -= 1;
				if (i == 0)
				{
					Debug.Assert(result == 0);
					return -1;
				}
			}
			return result;
		}

		public static int FindLastZero(uint i)
		{
			return FindLastOne(~i);
		}

		public static float NextHigher(float a)
		{
			// Remember: 1 sign bit, 23 mantissa bits, 8 exponent bits
			// 0x00000000              : zero
			// 0x00000001 to 0x007FFFFF: denormalized
			// 0x00800000 to 0x7F7FFFFF: normalized
			// 0x7F800000              : infinity
			// 0x7F800001 to 0x7FFFFFFF: NaN
			byte[] buf = BitConverter.GetBytes(a);
			uint bits = BitConverter.ToUInt32(buf, 0);
			uint bitsU = bits & 0x7FFFFFFFu;
			if (bitsU >= 0x7F800000)
				return a; // Number is infinite or NaN; do not change

			if (bitsU == 0)
				bits = 1;
			else if (bits != bitsU)
				bits--;
			else
				bits++;
			
			return Export(bits, buf);
		}
		public static float NextLower(float a)
		{
			byte[] buf = BitConverter.GetBytes(a);
			uint bits = BitConverter.ToUInt32(buf, 0);
			uint bitsU = bits & 0x7FFFFFFFu;
			if (bitsU >= 0x7F800000)
				return a; // Number is infinite or NaN; do not change

			if (bitsU == 0)
				bits = 0x80000001u;
			else if (bits != bitsU)
				bits++;
			else
				bits--;

			return Export(bits, buf);
		}
		private static float Export(uint bits, byte[] buf)
		{
			buf[0] = (byte)bits;
			buf[1] = (byte)(bits >> 8);
			buf[2] = (byte)(bits >> 16);
			buf[3] = (byte)(bits >> 24);
			return BitConverter.ToSingle(buf, 0);
		}

		public static double NextHigher(double num)
		{
			// Remember: 1 sign bit, 52 mantissa bits, 11 exponent bits
			// 0x0000_0000_0000_0000                         : zero
			// 0x0000_0000_0000_0001 to 0x000F_FFFF_FFFF_FFFF: denormalized
			// 0x0010_0000_0000_0000 to 0x7FEF_FFFF_FFFF_FFFF: normalized
			// 0x7FF0_0000_0000_0000                         : infinity
			// 0x7FF0_0000_0000_0001 to 0x7FFF_FFFF_FFFF_FFFF: NaN
			ulong bits = (ulong)BitConverter.DoubleToInt64Bits(num);
			ulong bitsU = bits & 0x7FFFFFFFFFFFFFFFu;
			if (bitsU >= 0x7FF0000000000000)
				return num; // Number is infinite or NaN; do not change

			if (bitsU == 0)
				bits = 1;
			else if (bits != bitsU)
				bits--;
			else
				bits++;
			
			return BitConverter.Int64BitsToDouble((long)bits);
		}
		public static double NextLower(double num)
		{
			ulong bits = (ulong)BitConverter.DoubleToInt64Bits(num);
			ulong bitsU = bits & 0x7FFFFFFFFFFFFFFFu;
			if (bitsU >= 0x7FF0000000000000)
				return num; // Number is infinite or NaN; do not change

			if (bitsU == 0)
				bits = 0x8000000000000001;
			else if (bits != bitsU)
				bits--;
			else
				bits++;
			
			return BitConverter.Int64BitsToDouble((long)bits);
		}

		public static double ShiftLeft(double num, int amount)
		{
			ulong bits = (ulong)BitConverter.DoubleToInt64Bits(num);
			uint exp = (uint)(bits >> 52) & 0x7FF;
			if (exp == 0x7FF)
				return num; // Number is infinite or NaN; do not change
			if (exp == 0)
			{
				// The number is denormalized. I'm tempted to just hand this off to
				// normal FP math: num * (1 << amount), but what if amount > 31?
				if (amount <= 0)
					if (amount == 0)
						return num;
					else
						return ShiftRight(num, -amount);
					
				ulong sign = bits & 0x8000000000000000;
				while ((bits <<= 1) <= 0x000FFFFFFFFFFFFF)
					if (--amount == 0)
						return BitConverter.Int64BitsToDouble((long)(bits | sign));
				bits |= sign;
				exp = 1;
			}

			// Normal case: num is normalized
			if ((exp += (uint)amount) < 0x7FF)
				return BitConverter.Int64BitsToDouble((long)(bits & 0x800FFFFFFFFFFFFFu) | ((long)exp << 52));
			
			// negative shift is not supported for integers, but it works okay for floats
			if (amount < 0)
				return ShiftRight(num, -amount);

			return (long)bits >= 0 ? double.PositiveInfinity : double.NegativeInfinity;
		}
		public static double ShiftRight(double num, int amount)
		{
			ulong bits = (ulong)BitConverter.DoubleToInt64Bits(num);
			uint exp = (uint)(bits >> 52) & 0x7FF;
			if (exp == 0x7FF)
				return num; 
			uint newExp = exp - (uint)amount;
			if (newExp - 1 < 0x7FF)
				return BitConverter.Int64BitsToDouble((long)(bits & 0x800FFFFFFFFFFFFFu) | ((long)newExp << 52));
			
			if (amount < 0)
				return ShiftLeft(num, -amount);

			// The result is denormalized.
			ulong sign = bits & 0x8000000000000000;
			bits &= 0x001FFFFFFFFFFFFF;
			// But was num denormalized already?
			if (exp > 1) {
				// not really, so let's get it ready for a denormalized right shift.
				amount -= ((int)exp - 1);
				Debug.Assert(amount >= 0);
				bits |= 0x0010000000000000;
			}
			if (amount > 53)
				return 0;
			
			return BitConverter.Int64BitsToDouble((long)(sign | (bits >> amount)));
		}
		public static float ShiftLeft(float num, int amount)
		{
			return (float)ShiftLeft((double)num, amount);
		}
		public static float ShiftRight(float num, int amount)
		{
			return (float)ShiftRight((double)num, amount);
		}
	}
}
