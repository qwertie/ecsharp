// Generated from Range.ecs by LeMP custom tool. LeMP version: 2.3.1.0
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
	/// <summary>
	/// Contains the functions used by the Enhanced C# <c>in</c>, <c>..</c> and 
	/// <c>...</c> operators... plus the handy <c>PutInRange()</c> methods.
	/// </summary>
	/// <remarks>
	/// Note: the following <c>InRange</c> extension methods have been moved to 
	/// class <see cref="G"/> in Loyc.Essentials so that Loyc.Syntax can use them:
	/// <ul>
	/// <li><c>n.IsInRange(lo, hi)</c> returns true if <c>n >= lo</c> and <c>hi >= n</c>, 
	///     which corresponds to <c>n in lo...hi</c> in EC#.</li>
	/// <li><c>n.IsInRangeExcludeHi(lo, hi)</c> returns true if <c>n >= lo</c> and <c>hi > n</c>,
	///     which corresponds to <c>n in lo..hi</c> in EC#.</li>
	/// </ul>
	/// If `in` and a range operator are not used together, something 
	/// slightly different happens:
	/// <ul>
	/// <li><c>var r = lo..hi</c> becomes <c>Range.ExcludeHi(lo, hi)</c> 
	///     (<c>Range.Inclusive</c> for <c>...</c>).</li>
	/// <li><c>x in r</c> becomes <c>r.Contains(x)</c>.</li>
	/// </ul>
	/// </remarks>
	public static class Range
	{
		/// <summary>Returns a range from lo to hi that includes both lo and hi.</summary>
		public static NumRange<int,MathI> Inclusive(int lo, int hi)
		{
			return new NumRange<int,MathI>(lo, hi);
		}
		/// <summary>Returns a range from lo to hi that excludes hi by decreasing it by 1.</summary>
		public static NumRange<int,MathI> ExcludeHi(int lo, int hi)
		{
			return new NumRange<int,MathI>(lo, hi - 1);
		}
		/// <summary>Returns a range from lo to the MaxValue of the number type.</summary>
		public static NumRange<int,MathI> StartingAt(int lo)
		{
			return new NumRange<int,MathI>(lo, int.MaxValue);
		}
		/// <summary>Returns the same range as Incl(num, num).</summary>
		public static NumRange<int,MathI> Only(int num)
		{
			return new NumRange<int,MathI>(num, num);
		}
		/// <summary>Returns a range from lo to hi that includes both lo and hi.</summary>
		public static NumRange<uint,MathU> Inclusive(uint lo, uint hi)
		{
			return new NumRange<uint,MathU>(lo, hi);
		}
		/// <summary>Returns a range from lo to hi that excludes hi by decreasing it by 1.</summary>
		public static NumRange<uint,MathU> ExcludeHi(uint lo, uint hi)
		{
			return new NumRange<uint,MathU>(lo, hi - 1);
		}
		/// <summary>Returns a range from lo to the MaxValue of the number type.</summary>
		public static NumRange<uint,MathU> StartingAt(uint lo)
		{
			return new NumRange<uint,MathU>(lo, uint.MaxValue);
		}
		/// <summary>Returns the same range as Incl(num, num).</summary>
		public static NumRange<uint,MathU> Only(uint num)
		{
			return new NumRange<uint,MathU>(num, num);
		}
		/// <summary>Returns a range from lo to hi that includes both lo and hi.</summary>
		public static NumRange<long,MathL> Inclusive(long lo, long hi)
		{
			return new NumRange<long,MathL>(lo, hi);
		}
		/// <summary>Returns a range from lo to hi that excludes hi by decreasing it by 1.</summary>
		public static NumRange<long,MathL> ExcludeHi(long lo, long hi)
		{
			return new NumRange<long,MathL>(lo, hi - 1);
		}
		/// <summary>Returns a range from lo to the MaxValue of the number type.</summary>
		public static NumRange<long,MathL> StartingAt(long lo)
		{
			return new NumRange<long,MathL>(lo, long.MaxValue);
		}
		/// <summary>Returns the same range as Incl(num, num).</summary>
		public static NumRange<long,MathL> Only(long num)
		{
			return new NumRange<long,MathL>(num, num);
		}
		/// <summary>Returns a range from lo to hi that includes both lo and hi.</summary>
		public static NumRange<ulong,MathUL> Inclusive(ulong lo, ulong hi)
		{
			return new NumRange<ulong,MathUL>(lo, hi);
		}
		/// <summary>Returns a range from lo to hi that excludes hi by decreasing it by 1.</summary>
		public static NumRange<ulong,MathUL> ExcludeHi(ulong lo, ulong hi)
		{
			return new NumRange<ulong,MathUL>(lo, hi - 1);
		}
		/// <summary>Returns a range from lo to the MaxValue of the number type.</summary>
		public static NumRange<ulong,MathUL> StartingAt(ulong lo)
		{
			return new NumRange<ulong,MathUL>(lo, ulong.MaxValue);
		}
		/// <summary>Returns the same range as Incl(num, num).</summary>
		public static NumRange<ulong,MathUL> Only(ulong num)
		{
			return new NumRange<ulong,MathUL>(num, num);
		}
		/// <summary>Returns a range from lo to hi that includes both lo and hi.</summary>
		public static NumRange<float,MathF> Inclusive(float lo, float hi)
		{
			return new NumRange<float,MathF>(lo, hi);
		}
		/// <summary>Returns a range from lo to hi that excludes hi by decreasing it by 1.</summary>
		public static NumRange<float,MathF> ExcludeHi(float lo, float hi)
		{
			return new NumRange<float,MathF>(lo, hi - 1);
		}
		/// <summary>Returns a range from lo to the MaxValue of the number type.</summary>
		public static NumRange<float,MathF> StartingAt(float lo)
		{
			return new NumRange<float,MathF>(lo, float.MaxValue);
		}
		/// <summary>Returns the same range as Incl(num, num).</summary>
		public static NumRange<float,MathF> Only(float num)
		{
			return new NumRange<float,MathF>(num, num);
		}
		/// <summary>Returns a range from lo to hi that includes both lo and hi.</summary>
		public static NumRange<double,MathD> Inclusive(double lo, double hi)
		{
			return new NumRange<double,MathD>(lo, hi);
		}
		/// <summary>Returns a range from lo to hi that excludes hi by decreasing it by 1.</summary>
		public static NumRange<double,MathD> ExcludeHi(double lo, double hi)
		{
			return new NumRange<double,MathD>(lo, hi - 1);
		}
		/// <summary>Returns a range from lo to the MaxValue of the number type.</summary>
		public static NumRange<double,MathD> StartingAt(double lo)
		{
			return new NumRange<double,MathD>(lo, double.MaxValue);
		}
		/// <summary>Returns the same range as Incl(num, num).</summary>
		public static NumRange<double,MathD> Only(double num)
		{
			return new NumRange<double,MathD>(num, num);
		}
		/// <summary>Returns a range from lo to hi that includes both lo and hi.</summary>
		public static NumRange<FPI8,MathFPI8> Inclusive(FPI8 lo, FPI8 hi)
		{
			return new NumRange<FPI8,MathFPI8>(lo, hi);
		}
		/// <summary>Returns a range from lo to hi that excludes hi by decreasing it by 1.</summary>
		public static NumRange<FPI8,MathFPI8> ExcludeHi(FPI8 lo, FPI8 hi)
		{
			return new NumRange<FPI8,MathFPI8>(lo, hi - 1);
		}
		/// <summary>Returns a range from lo to the MaxValue of the number type.</summary>
		public static NumRange<FPI8,MathFPI8> StartingAt(FPI8 lo)
		{
			return new NumRange<FPI8,MathFPI8>(lo, FPI8.MaxValue);
		}
		/// <summary>Returns the same range as Incl(num, num).</summary>
		public static NumRange<FPI8,MathFPI8> Only(FPI8 num)
		{
			return new NumRange<FPI8,MathFPI8>(num, num);
		}
		/// <summary>Returns a range from lo to hi that includes both lo and hi.</summary>
		public static NumRange<FPI16,MathFPI16> Inclusive(FPI16 lo, FPI16 hi)
		{
			return new NumRange<FPI16,MathFPI16>(lo, hi);
		}
		/// <summary>Returns a range from lo to hi that excludes hi by decreasing it by 1.</summary>
		public static NumRange<FPI16,MathFPI16> ExcludeHi(FPI16 lo, FPI16 hi)
		{
			return new NumRange<FPI16,MathFPI16>(lo, hi - 1);
		}
		/// <summary>Returns a range from lo to the MaxValue of the number type.</summary>
		public static NumRange<FPI16,MathFPI16> StartingAt(FPI16 lo)
		{
			return new NumRange<FPI16,MathFPI16>(lo, FPI16.MaxValue);
		}
		/// <summary>Returns the same range as Incl(num, num).</summary>
		public static NumRange<FPI16,MathFPI16> Only(FPI16 num)
		{
			return new NumRange<FPI16,MathFPI16>(num, num);
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