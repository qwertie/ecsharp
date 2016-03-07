// Generated from In.ecs by LeMP custom tool. LeMP version: 1.5.1.0
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
namespace Loyc
{
	public static class In
	{
		public static bool IsInRangeExcl(this int num, int lo, int hi)
		{
			return num >= lo && num < hi;
		}
		public static bool IsInRangeIncl(this int num, int lo, int hi)
		{
			return num >= lo && num <= hi;
		}
		public static bool IsInRangeExcl(this int num, int hi)
		{
			return num < hi;
		}
		public static bool IsInRangeIncl(this int num, int hi)
		{
			return num <= hi;
		}
		public static bool IsInRangeExcl(this uint num, uint lo, uint hi)
		{
			return num >= lo && num < hi;
		}
		public static bool IsInRangeIncl(this uint num, uint lo, uint hi)
		{
			return num >= lo && num <= hi;
		}
		public static bool IsInRangeExcl(this uint num, uint hi)
		{
			return num < hi;
		}
		public static bool IsInRangeIncl(this uint num, uint hi)
		{
			return num <= hi;
		}
		public static bool IsInRangeExcl(this long num, long lo, long hi)
		{
			return num >= lo && num < hi;
		}
		public static bool IsInRangeIncl(this long num, long lo, long hi)
		{
			return num >= lo && num <= hi;
		}
		public static bool IsInRangeExcl(this long num, long hi)
		{
			return num < hi;
		}
		public static bool IsInRangeIncl(this long num, long hi)
		{
			return num <= hi;
		}
		public static bool IsInRangeExcl(this ulong num, ulong lo, ulong hi)
		{
			return num >= lo && num < hi;
		}
		public static bool IsInRangeIncl(this ulong num, ulong lo, ulong hi)
		{
			return num >= lo && num <= hi;
		}
		public static bool IsInRangeExcl(this ulong num, ulong hi)
		{
			return num < hi;
		}
		public static bool IsInRangeIncl(this ulong num, ulong hi)
		{
			return num <= hi;
		}
		public static bool IsInRangeExcl(this float num, float lo, float hi)
		{
			return num >= lo && num < hi;
		}
		public static bool IsInRangeIncl(this float num, float lo, float hi)
		{
			return num >= lo && num <= hi;
		}
		public static bool IsInRangeExcl(this float num, float hi)
		{
			return num < hi;
		}
		public static bool IsInRangeIncl(this float num, float hi)
		{
			return num <= hi;
		}
		public static bool IsInRangeExcl(this double num, double lo, double hi)
		{
			return num >= lo && num < hi;
		}
		public static bool IsInRangeIncl(this double num, double lo, double hi)
		{
			return num >= lo && num <= hi;
		}
		public static bool IsInRangeExcl(this double num, double hi)
		{
			return num < hi;
		}
		public static bool IsInRangeIncl(this double num, double hi)
		{
			return num <= hi;
		}
		public static bool IsInRangeExcl< T>(this T num, T lo, T hi) where T: IComparable<T>
		{
			return num.CompareTo(lo) >= 0 && num.CompareTo(hi) < 0;
		}
		public static bool IsInRangeIncl< T>(this T num, T lo, T hi) where T: IComparable<T>
		{
			return num.CompareTo(lo) >= 0 && num.CompareTo(hi) <= 0;
		}
		public static bool IsInRangeExcl< T>(this T num, T hi) where T: IComparable<T>
		{
			return num.CompareTo(hi) < 0;
		}
		public static bool IsInRangeIncl< T>(this T num, T hi) where T: IComparable<T>
		{
			return num.CompareTo(hi) <= 0;
		}
	}
}
