using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
	}
}
