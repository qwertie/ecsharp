// Generated from Range.ecs by LeMP custom tool. LeMP version: 1.5.1.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Math;
using Loyc.Collections;
namespace Loyc
{
	public static class Range
	{
		public static bool IsInRangeExcludeHi(this int num, int lo, int hi)
		{
			return num >= lo && num < hi;
		}
		public static bool IsInRange(this int num, int lo, int hi)
		{
			return num >= lo && num <= hi;
		}
		public static int PutInRange(this int n, int min, int max)
		{
			if (n < min)
				return min;
			if (n > max)
				return max;
			return n;
		}
		public static bool IsInRangeExcludeHi(this uint num, uint lo, uint hi)
		{
			return num >= lo && num < hi;
		}
		public static bool IsInRange(this uint num, uint lo, uint hi)
		{
			return num >= lo && num <= hi;
		}
		public static uint PutInRange(this uint n, uint min, uint max)
		{
			if (n < min)
				return min;
			if (n > max)
				return max;
			return n;
		}
		public static bool IsInRangeExcludeHi(this long num, long lo, long hi)
		{
			return num >= lo && num < hi;
		}
		public static bool IsInRange(this long num, long lo, long hi)
		{
			return num >= lo && num <= hi;
		}
		public static long PutInRange(this long n, long min, long max)
		{
			if (n < min)
				return min;
			if (n > max)
				return max;
			return n;
		}
		public static bool IsInRangeExcludeHi(this ulong num, ulong lo, ulong hi)
		{
			return num >= lo && num < hi;
		}
		public static bool IsInRange(this ulong num, ulong lo, ulong hi)
		{
			return num >= lo && num <= hi;
		}
		public static ulong PutInRange(this ulong n, ulong min, ulong max)
		{
			if (n < min)
				return min;
			if (n > max)
				return max;
			return n;
		}
		public static bool IsInRangeExcludeHi(this float num, float lo, float hi)
		{
			return num >= lo && num < hi;
		}
		public static bool IsInRange(this float num, float lo, float hi)
		{
			return num >= lo && num <= hi;
		}
		public static float PutInRange(this float n, float min, float max)
		{
			if (n < min)
				return min;
			if (n > max)
				return max;
			return n;
		}
		public static bool IsInRangeExcludeHi(this double num, double lo, double hi)
		{
			return num >= lo && num < hi;
		}
		public static bool IsInRange(this double num, double lo, double hi)
		{
			return num >= lo && num <= hi;
		}
		public static double PutInRange(this double n, double min, double max)
		{
			if (n < min)
				return min;
			if (n > max)
				return max;
			return n;
		}
		public static bool IsInRangeExcludeHi<T>(this T num, T lo, T hi) where T: IComparable<T>
		{
			return num.CompareTo(lo) >= 0 && num.CompareTo(hi) < 0;
		}
		public static bool IsInRange<T>(this T num, T lo, T hi) where T: IComparable<T>
		{
			return num.CompareTo(lo) >= 0 && num.CompareTo(hi) <= 0;
		}
		public static T PutInRange<T>(this T n, T min, T max) where T: IComparable<T>
		{
			if (n.CompareTo(min) <= 0)
				return min;
			if (n.CompareTo(max) >= 0)
				return max;
			return n;
		}
		public static NumRange<int,MathI> Inclusive(int lo, int hi)
		{
			return new NumRange<int,MathI>(lo, hi);
		}
		public static NumRange<int,MathI> ExcludeHi(int lo, int hi)
		{
			return new NumRange<int,MathI>(lo, hi - 1);
		}
		public static NumRange<int,MathI> StartingAt(int lo)
		{
			return new NumRange<int,MathI>(lo, int.MaxValue);
		}
		public static NumRange<int,MathI> Only(int num)
		{
			return new NumRange<int,MathI>(num, num);
		}
		public static NumRange<uint,MathU> Inclusive(uint lo, uint hi)
		{
			return new NumRange<uint,MathU>(lo, hi);
		}
		public static NumRange<uint,MathU> ExcludeHi(uint lo, uint hi)
		{
			return new NumRange<uint,MathU>(lo, hi - 1);
		}
		public static NumRange<uint,MathU> StartingAt(uint lo)
		{
			return new NumRange<uint,MathU>(lo, uint.MaxValue);
		}
		public static NumRange<uint,MathU> Only(uint num)
		{
			return new NumRange<uint,MathU>(num, num);
		}
		public static NumRange<long,MathL> Inclusive(long lo, long hi)
		{
			return new NumRange<long,MathL>(lo, hi);
		}
		public static NumRange<long,MathL> ExcludeHi(long lo, long hi)
		{
			return new NumRange<long,MathL>(lo, hi - 1);
		}
		public static NumRange<long,MathL> StartingAt(long lo)
		{
			return new NumRange<long,MathL>(lo, long.MaxValue);
		}
		public static NumRange<long,MathL> Only(long num)
		{
			return new NumRange<long,MathL>(num, num);
		}
		public static NumRange<ulong,MathUL> Inclusive(ulong lo, ulong hi)
		{
			return new NumRange<ulong,MathUL>(lo, hi);
		}
		public static NumRange<ulong,MathUL> ExcludeHi(ulong lo, ulong hi)
		{
			return new NumRange<ulong,MathUL>(lo, hi - 1);
		}
		public static NumRange<ulong,MathUL> StartingAt(ulong lo)
		{
			return new NumRange<ulong,MathUL>(lo, ulong.MaxValue);
		}
		public static NumRange<ulong,MathUL> Only(ulong num)
		{
			return new NumRange<ulong,MathUL>(num, num);
		}
		public static NumRange<float,MathF> Inclusive(float lo, float hi)
		{
			return new NumRange<float,MathF>(lo, hi);
		}
		public static NumRange<float,MathF> ExcludeHi(float lo, float hi)
		{
			return new NumRange<float,MathF>(lo, hi - 1);
		}
		public static NumRange<float,MathF> StartingAt(float lo)
		{
			return new NumRange<float,MathF>(lo, float.MaxValue);
		}
		public static NumRange<float,MathF> Only(float num)
		{
			return new NumRange<float,MathF>(num, num);
		}
		public static NumRange<double,MathD> Inclusive(double lo, double hi)
		{
			return new NumRange<double,MathD>(lo, hi);
		}
		public static NumRange<double,MathD> ExcludeHi(double lo, double hi)
		{
			return new NumRange<double,MathD>(lo, hi - 1);
		}
		public static NumRange<double,MathD> StartingAt(double lo)
		{
			return new NumRange<double,MathD>(lo, double.MaxValue);
		}
		public static NumRange<double,MathD> Only(double num)
		{
			return new NumRange<double,MathD>(num, num);
		}
		public static NumRange<FPI8,MathF8> Inclusive(FPI8 lo, FPI8 hi)
		{
			return new NumRange<FPI8,MathF8>(lo, hi);
		}
		public static NumRange<FPI8,MathF8> ExcludeHi(FPI8 lo, FPI8 hi)
		{
			return new NumRange<FPI8,MathF8>(lo, hi - 1);
		}
		public static NumRange<FPI8,MathF8> StartingAt(FPI8 lo)
		{
			return new NumRange<FPI8,MathF8>(lo, FPI8.MaxValue);
		}
		public static NumRange<FPI8,MathF8> Only(FPI8 num)
		{
			return new NumRange<FPI8,MathF8>(num, num);
		}
		public static NumRange<FPI16,MathF16> Inclusive(FPI16 lo, FPI16 hi)
		{
			return new NumRange<FPI16,MathF16>(lo, hi);
		}
		public static NumRange<FPI16,MathF16> ExcludeHi(FPI16 lo, FPI16 hi)
		{
			return new NumRange<FPI16,MathF16>(lo, hi - 1);
		}
		public static NumRange<FPI16,MathF16> StartingAt(FPI16 lo)
		{
			return new NumRange<FPI16,MathF16>(lo, FPI16.MaxValue);
		}
		public static NumRange<FPI16,MathF16> Only(FPI16 num)
		{
			return new NumRange<FPI16,MathF16>(num, num);
		}
		public static NumRange<uint,MathU> UntilInclusive(uint hi)
		{
			return new NumRange<uint,MathU>(0, hi);
		}
		public static NumRange<uint,MathU> UntilExclusive(uint hi)
		{
			return new NumRange<uint,MathU>(0, hi - 1);
		}
		public static NumRange<ulong,MathUL> UntilInclusive(ulong hi)
		{
			return new NumRange<ulong,MathUL>(0, hi);
		}
		public static NumRange<ulong,MathUL> UntilExclusive(ulong hi)
		{
			return new NumRange<ulong,MathUL>(0, hi - 1);
		}
	}
}
