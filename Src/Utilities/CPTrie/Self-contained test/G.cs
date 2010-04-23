using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Loyc.Utilities
{
	/// <summary>Misc. functions taken from Loyc.Utilities.dll</summary>
	class G
	{
		/// <summary>Something Microsoft forgot.</summary>
		public static void Swap<T>(ref T a, ref T b)
		{
			T tmp = a;
			a = b;
			b = tmp;
		}

		/// <summary>Returns the number of '1' bits in x.</summary>
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
			if (i >> 16 == 0) {
				i <<= 16;
				result -= 16;
			}
			if (i >> 24 == 0) {
				i <<= 8;
				result -= 8;
			}
			if (i >> 28 == 0) {
				i <<= 4;
				result -= 4;
			}
			if (i >> 30 == 0) {
				i <<= 2;
				result -= 2;
			}
			if (i >> 31 == 0) {
				result -= 1;
				if (i == 0) {
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

		/// <summary>Like Debug.Assert, except that the argument is still evaluated
		/// in a release build.</summary>
		public static void Verify(bool cond)
		{
			Debug.Assert(cond);
		}
	}
}
