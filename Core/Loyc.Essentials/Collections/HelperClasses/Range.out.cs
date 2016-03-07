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
namespace Loyc.Collections
{
	public partial class Range
	{
		public static NumRange<int,MathI> Incl(int lo, int hi)
		{
			return new NumRange<int,MathI>(lo, hi);
		}
		public static NumRange<int,MathI> Excl(int lo, int hi)
		{
			return new NumRange<int,MathI>(lo, hi - 1);
		}
		public static NumRange<int,MathI> Low(int lo)
		{
			return new NumRange<int,MathI>(lo, int.MaxValue);
		}
		public static NumRange<int,MathI> Only(int num)
		{
			return new NumRange<int,MathI>(num, num);
		}
		public static NumRange<uint,MathU> Incl(uint lo, uint hi)
		{
			return new NumRange<uint,MathU>(lo, hi);
		}
		public static NumRange<uint,MathU> Excl(uint lo, uint hi)
		{
			return new NumRange<uint,MathU>(lo, hi - 1);
		}
		public static NumRange<uint,MathU> Low(uint lo)
		{
			return new NumRange<uint,MathU>(lo, uint.MaxValue);
		}
		public static NumRange<uint,MathU> Only(uint num)
		{
			return new NumRange<uint,MathU>(num, num);
		}
		public static NumRange<long,MathL> Incl(long lo, long hi)
		{
			return new NumRange<long,MathL>(lo, hi);
		}
		public static NumRange<long,MathL> Excl(long lo, long hi)
		{
			return new NumRange<long,MathL>(lo, hi - 1);
		}
		public static NumRange<long,MathL> Low(long lo)
		{
			return new NumRange<long,MathL>(lo, long.MaxValue);
		}
		public static NumRange<long,MathL> Only(long num)
		{
			return new NumRange<long,MathL>(num, num);
		}
		public static NumRange<ulong,MathUL> Incl(ulong lo, ulong hi)
		{
			return new NumRange<ulong,MathUL>(lo, hi);
		}
		public static NumRange<ulong,MathUL> Excl(ulong lo, ulong hi)
		{
			return new NumRange<ulong,MathUL>(lo, hi - 1);
		}
		public static NumRange<ulong,MathUL> Low(ulong lo)
		{
			return new NumRange<ulong,MathUL>(lo, ulong.MaxValue);
		}
		public static NumRange<ulong,MathUL> Only(ulong num)
		{
			return new NumRange<ulong,MathUL>(num, num);
		}
		public static NumRange<float,MathF> Incl(float lo, float hi)
		{
			return new NumRange<float,MathF>(lo, hi);
		}
		public static NumRange<float,MathF> Excl(float lo, float hi)
		{
			return new NumRange<float,MathF>(lo, hi - 1);
		}
		public static NumRange<float,MathF> Low(float lo)
		{
			return new NumRange<float,MathF>(lo, float.MaxValue);
		}
		public static NumRange<float,MathF> Only(float num)
		{
			return new NumRange<float,MathF>(num, num);
		}
		public static NumRange<double,MathD> Incl(double lo, double hi)
		{
			return new NumRange<double,MathD>(lo, hi);
		}
		public static NumRange<double,MathD> Excl(double lo, double hi)
		{
			return new NumRange<double,MathD>(lo, hi - 1);
		}
		public static NumRange<double,MathD> Low(double lo)
		{
			return new NumRange<double,MathD>(lo, double.MaxValue);
		}
		public static NumRange<double,MathD> Only(double num)
		{
			return new NumRange<double,MathD>(num, num);
		}
		public static NumRange<FPI8,MathF8> Incl(FPI8 lo, FPI8 hi)
		{
			return new NumRange<FPI8,MathF8>(lo, hi);
		}
		public static NumRange<FPI8,MathF8> Excl(FPI8 lo, FPI8 hi)
		{
			return new NumRange<FPI8,MathF8>(lo, hi - 1);
		}
		public static NumRange<FPI8,MathF8> Low(FPI8 lo)
		{
			return new NumRange<FPI8,MathF8>(lo, FPI8.MaxValue);
		}
		public static NumRange<FPI8,MathF8> Only(FPI8 num)
		{
			return new NumRange<FPI8,MathF8>(num, num);
		}
		public static NumRange<FPI16,MathF16> Incl(FPI16 lo, FPI16 hi)
		{
			return new NumRange<FPI16,MathF16>(lo, hi);
		}
		public static NumRange<FPI16,MathF16> Excl(FPI16 lo, FPI16 hi)
		{
			return new NumRange<FPI16,MathF16>(lo, hi - 1);
		}
		public static NumRange<FPI16,MathF16> Low(FPI16 lo)
		{
			return new NumRange<FPI16,MathF16>(lo, FPI16.MaxValue);
		}
		public static NumRange<FPI16,MathF16> Only(FPI16 num)
		{
			return new NumRange<FPI16,MathF16>(num, num);
		}
	}
}
