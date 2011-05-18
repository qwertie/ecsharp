using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Essentials;

namespace Loyc.Math
{
	/// <summary>
	/// Provides additional math functions that are not available in System.Math.
	/// </summary>
	public static class MathEx
	{
		#region IsInRange and InRange
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
		public static bool IsInRange<T>(this T n, T min, T max) where T : IComparable<T>
		{
			return n.CompareTo(min) >= 0 && n.CompareTo(max) <= 0;
		}
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
		public static T InRange<T>(this T n, T min, T max) where T : IComparable<T>
		{
			if (n.CompareTo(min) <= 0)
				return min;
			if (n.CompareTo(max) >= 0)
				return max;
			return n;
		}
		#endregion

		#region Sign
		/// <summary>Returns the sign of a number (-1 for negative, 1 for positive, 0 for zero).</summary>
		public static int Sign(long a)
		{
			return a == 0 ? 0 : (int)(a >> 63);
		}
		/// <summary>Returns the sign of a number (-1 for negative, 1 for positive, 0 for zero).</summary>
		public static int Sign(int a)
		{
			return a == 0 ? 0 : (a >> 31);
		}
		/// <summary>Returns the sign of a number (-1 for negative, 1 for positive, 0 for zero).</summary>
		public static int Sign(double a)
		{
			return a == 0 ? 0 : a > 0 ? 1 : -1;
		}
		#endregion

		#region MulShift
		/// <summary>Multiplies two integers, internally producing a double-size 
		/// result so that overflow is not possible, then divides the result by the 
		/// specified power of two using a right shift.</summary>
		/// <returns>a * mulBy >> shiftBy, without overflow during multiplication.</returns>
		/// <remarks>This method does not handle the case that the result is too
		/// large to fit in the original data type.</remarks>
		public static int MulShift(int a, int mulBy, int shiftBy)
		{
			return (int)((long)a * mulBy >> shiftBy);
		}
		/// <inheritdoc cref="MulShift(int,int,int)"/>
		public static uint MulShift(uint a, uint mulBy, int shiftBy)
		{
			return (uint)((ulong)a * mulBy >> shiftBy);
		}
		/// <inheritdoc cref="MulShift(int,int,int)"/>
		public static long MulShift(long a, long mulBy, int shiftBy)
		{
			long rH;
			ulong rL = Math128.Multiply(a, mulBy, out rH);
			Math128.ShiftRight(rH, ref rL, shiftBy);
			return (long)rL;
		}
		/// <inheritdoc cref="MulShift(int,int,int)"/>
		public static ulong MulShift(ulong a, ulong mulBy, int shiftBy)
		{
			ulong rH;
			ulong rL = Math128.Multiply(a, mulBy, out rH);
			Math128.ShiftRight(rH, ref rL, shiftBy);
			return rL;
		}
		#endregion
		
		#region MulDiv
		/// <summary>Multiplies two integers, internally producing a double-size 
		/// result so that overflow is not possible, then divides the result by the 
		/// specified number.</summary>
		/// <param name="remainder">The remainder of the division is placed here. 
		/// The remainder is computed properly even if the main result overflows.</param>
		/// <returns>a * mulBy / divBy, without overflow during multiplication.</returns>
		/// <remarks>If the final result does not fit in the original data type, 
		/// this method returns largest possible value of the result type 
		/// (int.MaxValue, or int.MinValue if the overflowing result is negative).
		/// </remarks>
		public static int MulDiv(int a, int mulBy, int divBy, out int remainder)
		{
			long m = (long)a * mulBy;
			remainder = (int)(m % divBy);
			return (int)(m / divBy);
		}
		/// <inheritdoc cref="MulDiv(int,int,int,out int)"/>
		/// <remarks>If the final result does not fit in the original data type, 
		/// this method returns largest possible value of the result type 
		/// (uint.MaxValue).</remarks>
		public static uint MulDiv(uint a, uint mulBy, uint divBy, out uint remainder)
		{
			ulong m = (ulong)a * mulBy;
			remainder = (uint)(m % divBy);
			return (uint)(m / divBy);
		}
		/// <inheritdoc cref="MulDiv(int,int,int,out int)"/>
		/// <remarks>If the final result does not fit in the original data type, 
		/// this method returns largest possible value of the result type 
		/// (long.MaxValue, or long.MinValue if the overflowing result is negative).
		/// </remarks>
		public static long MulDiv(long a, long mulBy, long divBy, out long remainder)
		{
			long mH;
			ulong mL = Math128.Multiply(a, mulBy, out mH);
			return Math128.Divide(mH, mL, divBy, out remainder, false);
		}
		/// <inheritdoc cref="MulDiv(int,int,int,out int)"/>
		/// <remarks>If the final result does not fit in the original data type, 
		/// this method returns largest possible value of the result type 
		/// (ulong.MaxValue).</remarks>
		public static ulong MulDiv(ulong a, ulong mulBy, ulong divBy, out ulong remainder)
		{
			ulong mH;
			ulong mL = Math128.Multiply(a, mulBy, out mH);
			return Math128.Divide(mH, mL, divBy, out remainder);
		}

		/// <inheritdoc cref="MulDiv(int a, int mulBy, int divBy, out int remainder)"/>
		public static int MulDiv(int a, int mulBy, int divBy)
		{
			return (int)((long)a * mulBy / divBy);
		}
		/// <inheritdoc cref="MulDiv(uint a, uint mulBy, uint divBy, out uint remainder)"/>
		public static uint MulDiv(uint a, uint mulBy, uint divBy)
		{
			return (uint)((ulong)a * mulBy / divBy);
		}
		/// <inheritdoc cref="MulDiv(long a, long mulBy, long divBy, out long remainder)"/>
		public static long MulDiv(long a, long mulBy, long divBy)
		{
			long mH, remainder;
			ulong mL = Math128.Multiply(a, mulBy, out mH);
			return Math128.Divide(mH, mL, divBy, out remainder, false);
		}
		/// <inheritdoc cref="MulDiv(ulong a, ulong mulBy, ulong divBy, out ulong remainder)"/>
		public static ulong MulDiv(ulong a, ulong mulBy, ulong divBy)
		{
			ulong mH, remainder;
			ulong mL = Math128.Multiply(a, mulBy, out mH);
			return Math128.Divide(mH, mL, divBy, out remainder);
		}
		#endregion

		////////////////////////////////////////////////////////////////////////////////
		/// Algorithms from http://aggregate.org/MAGIC and possibly
		/// http://www.devmaster.net/articles/fixed-point-optimizations/ or
		/// http://graphics.stanford.edu/~seander/bithacks.html

		#region Integer square roots
		public static uint Sqrt(long value)
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException("Can't compute Sqrt of a negative");
			// Maximum result: 3,037,000,499
			return Sqrt((ulong)value);
		}
		public static uint Sqrt(ulong value)
		{
			if (value == 0)
				return 0;

			uint g = 0;
			int bshft = Log2Floor(value) >> 1;
			uint b = 1u << bshft;
			do
			{
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
		public static int Sqrt(int value)
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException("Can't compute Sqrt of a negative");
			return (int)Sqrt((uint)value);
		}
		public static uint Sqrt(uint value)
		{
			if (value == 0)
				return 0;

			uint g = 0;
			int bshft = Log2Floor(value) >> 1;
			uint b = 1u << bshft;
			do
			{
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
		#endregion

		#region CountOnes
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
		#endregion

		#region Log2Floor
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
		#endregion

		#region FindFirstOne, FindLastOne, FindFirstZero, FindLastZero
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
		#endregion

		#region Bitwise conversion: int<->float, long<->double
		#if CompactFramework || Unsafe
		
		// Compact Framework lacks BitConverter.Int64BitsToDouble and 
		// DoubleToInt64Bits, but they are easy to replicate.
		public static unsafe double Int64BitsToDouble(long bits)
		{
			 return *(((double*) &bits));
		}
		public static unsafe long DoubleToInt64Bits(double value)
		{
			 return *(((long*) &value));
		}
		public static unsafe double Int32BitsToSingle(int bits)
		{
			 return *(((float*) &bits));
		}
		public static unsafe long SingleToInt32Bits(float value)
		{
			 return *(((int*) &value));
		}
		
		#else

		public static double Int64BitsToDouble(long bits)
		{
			 return BitConverter.Int64BitsToDouble(bits);
		}
		public static long DoubleToInt64Bits(double value)
		{
			 return BitConverter.DoubleToInt64Bits(value);
		}
		public static double Int32BitsToSingle(int bits)
		{
			byte[] buf = new byte[4];
			buf[0] = (byte)bits;
			buf[1] = (byte)(bits >> 8);
			buf[2] = (byte)(bits >> 16);
			buf[3] = (byte)(bits >> 24);
			return BitConverter.ToSingle(buf, 0);
		}
		public static long SingleToInt32Bits(float value)
		{
			byte[] buf = BitConverter.GetBytes(value);
			return BitConverter.ToInt32(buf, 0);
		}
		
		#endif
		#endregion

		#region NextHigher and NextLower for floating-point
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
			ulong bits = (ulong)DoubleToInt64Bits(num);
			ulong bitsU = bits & 0x7FFFFFFFFFFFFFFFu;
			if (bitsU >= 0x7FF0000000000000)
				return num; // Number is infinite or NaN; do not change

			if (bitsU == 0)
				bits = 1;
			else if (bits != bitsU)
				bits--;
			else
				bits++;

			return Int64BitsToDouble((long)bits);
		}
		public static double NextLower(double num)
		{
			ulong bits = (ulong)DoubleToInt64Bits(num);
			ulong bitsU = bits & 0x7FFFFFFFFFFFFFFFu;
			if (bitsU >= 0x7FF0000000000000)
				return num; // Number is infinite or NaN; do not change

			if (bitsU == 0)
				bits = 0x8000000000000001;
			else if (bits != bitsU)
				bits--;
			else
				bits++;

			return Int64BitsToDouble((long)bits);
		}
		#endregion

		#region ShiftLeft and ShiftRight for floating point
		public static double ShiftLeft(double num, int amount)
		{
			ulong bits = (ulong)DoubleToInt64Bits(num);
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
						return Int64BitsToDouble((long)(bits | sign));
				bits |= sign;
				exp = 1;
			}

			// Normal case: num is normalized
			if ((exp += (uint)amount) < 0x7FF)
				return Int64BitsToDouble((long)(bits & 0x800FFFFFFFFFFFFFu) | ((long)exp << 52));

			// negative shift is not supported for integers, but it works okay for floats
			if (amount < 0)
				return ShiftRight(num, -amount);

			return (long)bits >= 0 ? double.PositiveInfinity : double.NegativeInfinity;
		}
		public static double ShiftRight(double num, int amount)
		{
			ulong bits = (ulong)DoubleToInt64Bits(num);
			uint exp = (uint)(bits >> 52) & 0x7FF;
			if (exp == 0x7FF)
				return num;
			uint newExp = exp - (uint)amount;
			if (newExp - 1 < 0x7FF)
				return Int64BitsToDouble((long)(bits & 0x800FFFFFFFFFFFFFu) | ((long)newExp << 52));

			if (amount < 0)
				return ShiftLeft(num, -amount);

			// The result is denormalized.
			ulong sign = bits & 0x8000000000000000;
			bits &= 0x001FFFFFFFFFFFFF;
			// But was num denormalized already?
			if (exp > 1)
			{
				// not really, so let's get it ready for a denormalized right shift.
				amount -= ((int)exp - 1);
				Debug.Assert(amount >= 0);
				bits |= 0x0010000000000000;
			}
			if (amount > 53)
				return 0;

			return Int64BitsToDouble((long)(sign | (bits >> amount)));
		}
		public static float ShiftLeft(float num, int amount)
		{
			return (float)ShiftLeft((double)num, amount);
		}
		public static float ShiftRight(float num, int amount)
		{
			return (float)ShiftRight((double)num, amount);
		}
		#endregion

		public static T Min<T>(T a, T b) where T : IComparable<T>
		{
			return a.CompareTo(b) < 0 ? a : b;
		}
		public static T Max<T>(T a, T b) where T : IComparable<T>
		{
			return a.CompareTo(b) < 0 ? a : b;
		}

		public static void Swap<T>(ref T a, ref T b)
		{
			T c = a;
			a = b;
			b = c;
		}
	}
}
