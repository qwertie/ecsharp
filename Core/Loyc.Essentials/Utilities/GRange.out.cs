// Generated from GRange.ecs by LeMP custom tool. LeMP version: 2.8.3.0
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
	partial class G
	{
		// These methods were supposed to be in Loyc.Range, but Loyc.Range contains methods
		// that return NumRange<Num,Math>, which uses Maths and therefore must be in 
		// Loyc.Math, not Loyc.Essentials. The problem is that some of these methods are 
		// used by Loyc.Syntax, which doesn't reference Loyc.Math and so can't use Range.
		// Since these are extension methods, hopefully few will notice that they moved.
		/// <summary>Returns true if `num` is between `lo` and `hi`, excluding `hi` but not `lo`.</summary>
		public static bool IsInRangeExcludeHi(this int num, int lo, int hi) {
			return num >= lo && num < hi;
		}
		/// <summary>Returns true if `num` is between `lo` and `hi`.</summary>
		public static bool IsInRange(this int num, int lo, int hi) {
			return num >= lo && num <= hi;
		}
		/// <summary>Returns `num` clamped to the range `min` and `max`.</summary>
		public static int PutInRange(this int n, int min, int max)
		{
			if (n < min)
				return min;
			if (n > max)
				return max;
			return n;
		}
		/// <summary>Returns true if `num` is between `lo` and `hi`, excluding `hi` but not `lo`.</summary>
		public static bool IsInRangeExcludeHi(this uint num, uint lo, uint hi) {
			return num >= lo && num < hi;
		}
		/// <summary>Returns true if `num` is between `lo` and `hi`.</summary>
		public static bool IsInRange(this uint num, uint lo, uint hi) {
			return num >= lo && num <= hi;
		}
		/// <summary>Returns `num` clamped to the range `min` and `max`.</summary>
		public static uint PutInRange(this uint n, uint min, uint max)
		{
			if (n < min)
				return min;
			if (n > max)
				return max;
			return n;
		}
		/// <summary>Returns true if `num` is between `lo` and `hi`, excluding `hi` but not `lo`.</summary>
		public static bool IsInRangeExcludeHi(this long num, long lo, long hi) {
			return num >= lo && num < hi;
		}
		/// <summary>Returns true if `num` is between `lo` and `hi`.</summary>
		public static bool IsInRange(this long num, long lo, long hi) {
			return num >= lo && num <= hi;
		}
		/// <summary>Returns `num` clamped to the range `min` and `max`.</summary>
		public static long PutInRange(this long n, long min, long max)
		{
			if (n < min)
				return min;
			if (n > max)
				return max;
			return n;
		}
		/// <summary>Returns true if `num` is between `lo` and `hi`, excluding `hi` but not `lo`.</summary>
		public static bool IsInRangeExcludeHi(this ulong num, ulong lo, ulong hi) {
			return num >= lo && num < hi;
		}
		/// <summary>Returns true if `num` is between `lo` and `hi`.</summary>
		public static bool IsInRange(this ulong num, ulong lo, ulong hi) {
			return num >= lo && num <= hi;
		}
		/// <summary>Returns `num` clamped to the range `min` and `max`.</summary>
		public static ulong PutInRange(this ulong n, ulong min, ulong max)
		{
			if (n < min)
				return min;
			if (n > max)
				return max;
			return n;
		}
		/// <summary>Returns true if `num` is between `lo` and `hi`, excluding `hi` but not `lo`.</summary>
		public static bool IsInRangeExcludeHi(this float num, float lo, float hi) {
			return num >= lo && num < hi;
		}
		/// <summary>Returns true if `num` is between `lo` and `hi`.</summary>
		public static bool IsInRange(this float num, float lo, float hi) {
			return num >= lo && num <= hi;
		}
		/// <summary>Returns `num` clamped to the range `min` and `max`.</summary>
		public static float PutInRange(this float n, float min, float max)
		{
			if (n < min)
				return min;
			if (n > max)
				return max;
			return n;
		}
		/// <summary>Returns true if `num` is between `lo` and `hi`, excluding `hi` but not `lo`.</summary>
		public static bool IsInRangeExcludeHi(this double num, double lo, double hi) {
			return num >= lo && num < hi;
		}
		/// <summary>Returns true if `num` is between `lo` and `hi`.</summary>
		public static bool IsInRange(this double num, double lo, double hi) {
			return num >= lo && num <= hi;
		}
		/// <summary>Returns `num` clamped to the range `min` and `max`.</summary>
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
	}
}