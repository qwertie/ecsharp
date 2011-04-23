//
// Math operation structures produced with the help of T4 (Maths.tt)
// NOTE: THIS CODE HAS NOT BEEN WELL-TESTED AND DOES NOT YET HAVE A TEST SUITE.
// 

using System.Collections.Generic;


namespace Loyc.Math
{
	using System;
	using T = System.SByte;

	public struct MathI8 : IIntMath<sbyte>
	{
		public static readonly MathI8 Value = new MathI8();

		#region INumTraits

		public T MinValue           { get { return T.MinValue; } }
		public T MaxValue           { get { return T.MaxValue; } }
		public T Epsilon            { get { return 1; } }
		public T PositiveInfinity   { get { return T.MaxValue; } }
		public T NegativeInfinity   { get { return T.MinValue; } }
		public bool IsInfinity(T value)   { return false; }
		public bool IsNaN(T value)        { return false; }
		public bool IsSigned        { get { return true; } }
		public bool IsFloatingPoint { get { return false; } }
		public bool IsInteger       { get { return true; } }
		public bool IsOrdered       { get { return true; } }
		public int SignificantBits  { get { return 7; } }
		public int MaxIntPowerOf2   { get { return 7; } }
		public ulong MaxInt { get { return (ulong)(ulong)System.SByte.MaxValue; } }
		public long MinInt  { get { return (long)(long)System.SByte.MinValue; } }
		public T Zero       { get { return (sbyte)0; } }
		public T One        { get { return (sbyte)1; } }

		#endregion

		#region ISignedMath

		public T From(uint t)   { return (T)t; }
		public T From(int t)    { return (T)t; }
		public T From(ulong t)  { return (T)t; }
		public T From(long t)   { return (T)t; }
		public T From(double t) { return (T)t; }

		public T Clip(uint t)   { return t > (uint)T.MaxValue ? T.MaxValue : (T)t; }
		public T Clip(ulong t)  { return t > (ulong)T.MaxValue ? T.MaxValue : (T)t; }
		public T Clip(int t)    { return t > (int)T.MaxValue ? T.MaxValue : 		                    t < (int)T.MinValue ? T.MinValue : (T)t; }
		public T Clip(long t)   { return t > (long)T.MaxValue ? T.MaxValue : 		                    t < (long)T.MinValue ? T.MinValue : (T)t; }
		public T Clip(double t) { return (T)MathEx.InRange(t, (double)0, (double)T.MaxValue); }

		public bool IsLess(T a, T b)        { return a < b; }
		public bool IsLessOrEqual(T a, T b) { return a <= b; }
		public T Abs(T a)                   { return (T)(a >= Zero ? a : -a); }
		public T Max(T a, T b)              { return a > b ? a : b; }
		public T Min(T a, T b)              { return a < b ? a : b; }
		public int Compare(T x, T y)        { return x.CompareTo(y); }
		public bool Equals(T x, T y)        { return x == y; }
		public int GetHashCode(T x)         { return x.GetHashCode(); }

		public T Incremented(T a)           { a++; return a; }
		public T Decremented(T a)           { a--; return a; }
		public T NextHigher(T a)            { a++; return a; }
		public T NextLower(T a)             { a--; return a; }

		public T Add(T a, T b)              { return (T)(a + b); }
		public T Subtract(T a, T b)         { return (T)(a - b); }
		public T Multiply(T a, T b)         { return (T)(a * b); }
		public T Divide(T a, T b)           { return (T)(a / b); }
		public T MulDiv(T a, T mul, T div)  { return (T)(a * mul / div); }

		public T Negate(T a) { return (T)(-a); }

		public T ShiftLeft(T a, int amount)  { return (T)(a << amount); }
		public T ShiftRight(T a, int amount) { return (T)(a >> amount); }

		public T Sqrt(T a)   { return (T)MathEx.Sqrt(a); }
		public T Square(T a) { return (T)(a * a); }

		#endregion

		#region BinaryMath

		public T And(T a, T b) { return (T)(a & b); }
		public T Or(T a, T b)  { return (T)(a | b); }
		public T Xor(T a, T b) { return (T)(a ^ b); }
		public T Not(T a)      { return (T)~a; }

		public int CountOnes(T a)     { return MathEx.CountOnes(a); }
		public int Log2Floor(T a)     { return MathEx.Log2Floor(a); }

		#endregion
	}
}

namespace Loyc.Math
{
	using System;
	using T = System.Byte;

	public struct MathU8 : IUIntMath<byte>
	{
		public static readonly MathU8 Value = new MathU8();

		#region INumTraits

		public T MinValue           { get { return T.MinValue; } }
		public T MaxValue           { get { return T.MaxValue; } }
		public T Epsilon            { get { return 1; } }
		public T PositiveInfinity   { get { return T.MaxValue; } }
		public T NegativeInfinity   { get { throw new NotSupportedException(); } }
		public bool IsInfinity(T value)   { return false; }
		public bool IsNaN(T value)        { return false; }
		public bool IsSigned        { get { return false; } }
		public bool IsFloatingPoint { get { return false; } }
		public bool IsInteger       { get { return true; } }
		public bool IsOrdered       { get { return true; } }
		public int SignificantBits  { get { return 8; } }
		public int MaxIntPowerOf2   { get { return 8; } }
		public ulong MaxInt { get { return (ulong)(ulong)System.Byte.MaxValue; } }
		public long MinInt  { get { return (long)(long)System.Byte.MinValue; } }
		public T Zero       { get { return (byte)0; } }
		public T One        { get { return (byte)1; } }

		#endregion

		#region IMath

		public T From(uint t)   { return (T)t; }
		public T From(int t)    { return (T)t; }
		public T From(ulong t)  { return (T)t; }
		public T From(long t)   { return (T)t; }
		public T From(double t) { return (T)t; }

		public T Clip(uint t)   { return t > (uint)T.MaxValue ? T.MaxValue : (T)t; }
		public T Clip(ulong t)  { return t > (ulong)T.MaxValue ? T.MaxValue : (T)t; }
		public T Clip(int t)    { return t > (int)T.MaxValue ? T.MaxValue : 		                    t < (int)T.MinValue ? T.MinValue : (T)t; }
		public T Clip(long t)   { return t > (long)T.MaxValue ? T.MaxValue : 		                    t < (long)T.MinValue ? T.MinValue : (T)t; }
		public T Clip(double t) { return (T)MathEx.InRange(t, (double)0, (double)T.MaxValue); }

		public bool IsLess(T a, T b)        { return a < b; }
		public bool IsLessOrEqual(T a, T b) { return a <= b; }
		public T Abs(T a)                   { return a; }
		public T Max(T a, T b)              { return a > b ? a : b; }
		public T Min(T a, T b)              { return a < b ? a : b; }
		public int Compare(T x, T y)        { return x.CompareTo(y); }
		public bool Equals(T x, T y)        { return x == y; }
		public int GetHashCode(T x)         { return x.GetHashCode(); }

		public T Incremented(T a)           { a++; return a; }
		public T Decremented(T a)           { a--; return a; }
		public T NextHigher(T a)            { a++; return a; }
		public T NextLower(T a)             { a--; return a; }

		public T Add(T a, T b)              { return (T)(a + b); }
		public T Subtract(T a, T b)         { return (T)(a - b); }
		public T Multiply(T a, T b)         { return (T)(a * b); }
		public T Divide(T a, T b)           { return (T)(a / b); }
		public T MulDiv(T a, T mul, T div)  { return (T)(a * mul / div); }

		public T ShiftLeft(T a, int amount)  { return (T)(a << amount); }
		public T ShiftRight(T a, int amount) { return (T)(a >> amount); }

		public T Sqrt(T a)   { return (T)MathEx.Sqrt(a); }
		public T Square(T a) { return (T)(a * a); }

		#endregion

		#region BinaryMath

		public T And(T a, T b) { return (T)(a & b); }
		public T Or(T a, T b)  { return (T)(a | b); }
		public T Xor(T a, T b) { return (T)(a ^ b); }
		public T Not(T a)      { return (T)~a; }

		public int CountOnes(T a)     { return MathEx.CountOnes(a); }
		public int Log2Floor(T a)     { return MathEx.Log2Floor(a); }

		#endregion
	}
}

namespace Loyc.Math
{
	using System;
	using T = System.Int16;

	public struct MathI16 : IIntMath<short>
	{
		public static readonly MathI16 Value = new MathI16();

		#region INumTraits

		public T MinValue           { get { return T.MinValue; } }
		public T MaxValue           { get { return T.MaxValue; } }
		public T Epsilon            { get { return 1; } }
		public T PositiveInfinity   { get { return T.MaxValue; } }
		public T NegativeInfinity   { get { return T.MinValue; } }
		public bool IsInfinity(T value)   { return false; }
		public bool IsNaN(T value)        { return false; }
		public bool IsSigned        { get { return true; } }
		public bool IsFloatingPoint { get { return false; } }
		public bool IsInteger       { get { return true; } }
		public bool IsOrdered       { get { return true; } }
		public int SignificantBits  { get { return 15; } }
		public int MaxIntPowerOf2   { get { return 15; } }
		public ulong MaxInt { get { return (ulong)(ulong)System.Int16.MaxValue; } }
		public long MinInt  { get { return (long)(long)System.Int16.MinValue; } }
		public T Zero       { get { return (short)0; } }
		public T One        { get { return (short)1; } }

		#endregion

		#region ISignedMath

		public T From(uint t)   { return (T)t; }
		public T From(int t)    { return (T)t; }
		public T From(ulong t)  { return (T)t; }
		public T From(long t)   { return (T)t; }
		public T From(double t) { return (T)t; }

		public T Clip(uint t)   { return t > (uint)T.MaxValue ? T.MaxValue : (T)t; }
		public T Clip(ulong t)  { return t > (ulong)T.MaxValue ? T.MaxValue : (T)t; }
		public T Clip(int t)    { return t > (int)T.MaxValue ? T.MaxValue : 		                    t < (int)T.MinValue ? T.MinValue : (T)t; }
		public T Clip(long t)   { return t > (long)T.MaxValue ? T.MaxValue : 		                    t < (long)T.MinValue ? T.MinValue : (T)t; }
		public T Clip(double t) { return (T)MathEx.InRange(t, (double)0, (double)T.MaxValue); }

		public bool IsLess(T a, T b)        { return a < b; }
		public bool IsLessOrEqual(T a, T b) { return a <= b; }
		public T Abs(T a)                   { return (T)(a >= Zero ? a : -a); }
		public T Max(T a, T b)              { return a > b ? a : b; }
		public T Min(T a, T b)              { return a < b ? a : b; }
		public int Compare(T x, T y)        { return x.CompareTo(y); }
		public bool Equals(T x, T y)        { return x == y; }
		public int GetHashCode(T x)         { return x.GetHashCode(); }

		public T Incremented(T a)           { a++; return a; }
		public T Decremented(T a)           { a--; return a; }
		public T NextHigher(T a)            { a++; return a; }
		public T NextLower(T a)             { a--; return a; }

		public T Add(T a, T b)              { return (T)(a + b); }
		public T Subtract(T a, T b)         { return (T)(a - b); }
		public T Multiply(T a, T b)         { return (T)(a * b); }
		public T Divide(T a, T b)           { return (T)(a / b); }
		public T MulDiv(T a, T mul, T div)  { return (T)(a * mul / div); }

		public T Negate(T a) { return (T)(-a); }

		public T ShiftLeft(T a, int amount)  { return (T)(a << amount); }
		public T ShiftRight(T a, int amount) { return (T)(a >> amount); }

		public T Sqrt(T a)   { return (T)MathEx.Sqrt(a); }
		public T Square(T a) { return (T)(a * a); }

		#endregion

		#region BinaryMath

		public T And(T a, T b) { return (T)(a & b); }
		public T Or(T a, T b)  { return (T)(a | b); }
		public T Xor(T a, T b) { return (T)(a ^ b); }
		public T Not(T a)      { return (T)~a; }

		public int CountOnes(T a)     { return MathEx.CountOnes(a); }
		public int Log2Floor(T a)     { return MathEx.Log2Floor(a); }

		#endregion
	}
}

namespace Loyc.Math
{
	using System;
	using T = System.UInt16;

	public struct MathU16 : IUIntMath<ushort>
	{
		public static readonly MathU16 Value = new MathU16();

		#region INumTraits

		public T MinValue           { get { return T.MinValue; } }
		public T MaxValue           { get { return T.MaxValue; } }
		public T Epsilon            { get { return 1; } }
		public T PositiveInfinity   { get { return T.MaxValue; } }
		public T NegativeInfinity   { get { throw new NotSupportedException(); } }
		public bool IsInfinity(T value)   { return false; }
		public bool IsNaN(T value)        { return false; }
		public bool IsSigned        { get { return false; } }
		public bool IsFloatingPoint { get { return false; } }
		public bool IsInteger       { get { return true; } }
		public bool IsOrdered       { get { return true; } }
		public int SignificantBits  { get { return 16; } }
		public int MaxIntPowerOf2   { get { return 16; } }
		public ulong MaxInt { get { return (ulong)(ulong)System.UInt16.MaxValue; } }
		public long MinInt  { get { return (long)(long)System.UInt16.MinValue; } }
		public T Zero       { get { return (ushort)0; } }
		public T One        { get { return (ushort)1; } }

		#endregion

		#region IMath

		public T From(uint t)   { return (T)t; }
		public T From(int t)    { return (T)t; }
		public T From(ulong t)  { return (T)t; }
		public T From(long t)   { return (T)t; }
		public T From(double t) { return (T)t; }

		public T Clip(uint t)   { return t > (uint)T.MaxValue ? T.MaxValue : (T)t; }
		public T Clip(ulong t)  { return t > (ulong)T.MaxValue ? T.MaxValue : (T)t; }
		public T Clip(int t)    { return t > (int)T.MaxValue ? T.MaxValue : 		                    t < (int)T.MinValue ? T.MinValue : (T)t; }
		public T Clip(long t)   { return t > (long)T.MaxValue ? T.MaxValue : 		                    t < (long)T.MinValue ? T.MinValue : (T)t; }
		public T Clip(double t) { return (T)MathEx.InRange(t, (double)0, (double)T.MaxValue); }

		public bool IsLess(T a, T b)        { return a < b; }
		public bool IsLessOrEqual(T a, T b) { return a <= b; }
		public T Abs(T a)                   { return a; }
		public T Max(T a, T b)              { return a > b ? a : b; }
		public T Min(T a, T b)              { return a < b ? a : b; }
		public int Compare(T x, T y)        { return x.CompareTo(y); }
		public bool Equals(T x, T y)        { return x == y; }
		public int GetHashCode(T x)         { return x.GetHashCode(); }

		public T Incremented(T a)           { a++; return a; }
		public T Decremented(T a)           { a--; return a; }
		public T NextHigher(T a)            { a++; return a; }
		public T NextLower(T a)             { a--; return a; }

		public T Add(T a, T b)              { return (T)(a + b); }
		public T Subtract(T a, T b)         { return (T)(a - b); }
		public T Multiply(T a, T b)         { return (T)(a * b); }
		public T Divide(T a, T b)           { return (T)(a / b); }
		public T MulDiv(T a, T mul, T div)  { return (T)(a * mul / div); }

		public T ShiftLeft(T a, int amount)  { return (T)(a << amount); }
		public T ShiftRight(T a, int amount) { return (T)(a >> amount); }

		public T Sqrt(T a)   { return (T)MathEx.Sqrt(a); }
		public T Square(T a) { return (T)(a * a); }

		#endregion

		#region BinaryMath

		public T And(T a, T b) { return (T)(a & b); }
		public T Or(T a, T b)  { return (T)(a | b); }
		public T Xor(T a, T b) { return (T)(a ^ b); }
		public T Not(T a)      { return (T)~a; }

		public int CountOnes(T a)     { return MathEx.CountOnes(a); }
		public int Log2Floor(T a)     { return MathEx.Log2Floor(a); }

		#endregion
	}
}

namespace Loyc.Math
{
	using System;
	using T = System.Int32;

	public struct MathI : IIntMath<int>
	{
		public static readonly MathI Value = new MathI();

		#region INumTraits

		public T MinValue           { get { return T.MinValue; } }
		public T MaxValue           { get { return T.MaxValue; } }
		public T Epsilon            { get { return 1; } }
		public T PositiveInfinity   { get { return T.MaxValue; } }
		public T NegativeInfinity   { get { return T.MinValue; } }
		public bool IsInfinity(T value)   { return false; }
		public bool IsNaN(T value)        { return false; }
		public bool IsSigned        { get { return true; } }
		public bool IsFloatingPoint { get { return false; } }
		public bool IsInteger       { get { return true; } }
		public bool IsOrdered       { get { return true; } }
		public int SignificantBits  { get { return 31; } }
		public int MaxIntPowerOf2   { get { return 31; } }
		public ulong MaxInt { get { return (ulong)(ulong)System.Int32.MaxValue; } }
		public long MinInt  { get { return (long)(long)System.Int32.MinValue; } }
		public T Zero       { get { return (int)0; } }
		public T One        { get { return (int)1; } }

		#endregion

		#region ISignedMath

		public T From(uint t)   { return (T)t; }
		public T From(int t)    { return (T)t; }
		public T From(ulong t)  { return (T)t; }
		public T From(long t)   { return (T)t; }
		public T From(double t) { return (T)t; }

		public T Clip(uint t)   { return t > (uint)T.MaxValue ? T.MaxValue : (T)t; }
		public T Clip(ulong t)  { return t > (ulong)T.MaxValue ? T.MaxValue : (T)t; }
		public T Clip(int t)    { return		                   (T)t; }
		public T Clip(long t)   { return t > (long)T.MaxValue ? T.MaxValue : 		                    t < (long)T.MinValue ? T.MinValue : (T)t; }
		public T Clip(double t) { return (T)MathEx.InRange(t, (double)0, (double)T.MaxValue); }

		public bool IsLess(T a, T b)        { return a < b; }
		public bool IsLessOrEqual(T a, T b) { return a <= b; }
		public T Abs(T a)                   { return (T)(a >= Zero ? a : -a); }
		public T Max(T a, T b)              { return a > b ? a : b; }
		public T Min(T a, T b)              { return a < b ? a : b; }
		public int Compare(T x, T y)        { return x.CompareTo(y); }
		public bool Equals(T x, T y)        { return x == y; }
		public int GetHashCode(T x)         { return x.GetHashCode(); }

		public T Incremented(T a)           { a++; return a; }
		public T Decremented(T a)           { a--; return a; }
		public T NextHigher(T a)            { a++; return a; }
		public T NextLower(T a)             { a--; return a; }

		public T Add(T a, T b)              { return (T)(a + b); }
		public T Subtract(T a, T b)         { return (T)(a - b); }
		public T Multiply(T a, T b)         { return (T)(a * b); }
		public T Divide(T a, T b)           { return (T)(a / b); }
		public T MulDiv(T a, T mul, T div)  { return (T)MathEx.MulDiv(a, mul, div); }

		public T Negate(T a) { return (T)(-a); }

		public T ShiftLeft(T a, int amount)  { return (T)(a << amount); }
		public T ShiftRight(T a, int amount) { return (T)(a >> amount); }

		public T Sqrt(T a)   { return (T)MathEx.Sqrt(a); }
		public T Square(T a) { return (T)(a * a); }

		#endregion

		#region BinaryMath

		public T And(T a, T b) { return (T)(a & b); }
		public T Or(T a, T b)  { return (T)(a | b); }
		public T Xor(T a, T b) { return (T)(a ^ b); }
		public T Not(T a)      { return (T)~a; }

		public int CountOnes(T a)     { return MathEx.CountOnes(a); }
		public int Log2Floor(T a)     { return MathEx.Log2Floor(a); }

		#endregion
	}
}

namespace Loyc.Math
{
	using System;
	using T = System.UInt32;

	public struct MathU : IUIntMath<uint>
	{
		public static readonly MathU Value = new MathU();

		#region INumTraits

		public T MinValue           { get { return T.MinValue; } }
		public T MaxValue           { get { return T.MaxValue; } }
		public T Epsilon            { get { return 1; } }
		public T PositiveInfinity   { get { return T.MaxValue; } }
		public T NegativeInfinity   { get { throw new NotSupportedException(); } }
		public bool IsInfinity(T value)   { return false; }
		public bool IsNaN(T value)        { return false; }
		public bool IsSigned        { get { return false; } }
		public bool IsFloatingPoint { get { return false; } }
		public bool IsInteger       { get { return true; } }
		public bool IsOrdered       { get { return true; } }
		public int SignificantBits  { get { return 32; } }
		public int MaxIntPowerOf2   { get { return 32; } }
		public ulong MaxInt { get { return (ulong)(ulong)System.UInt32.MaxValue; } }
		public long MinInt  { get { return (long)(long)System.UInt32.MinValue; } }
		public T Zero       { get { return (uint)0; } }
		public T One        { get { return (uint)1; } }

		#endregion

		#region IMath

		public T From(uint t)   { return (T)t; }
		public T From(int t)    { return (T)t; }
		public T From(ulong t)  { return (T)t; }
		public T From(long t)   { return (T)t; }
		public T From(double t) { return (T)t; }

		public T Clip(uint t)   { return(T)t; }
		public T Clip(ulong t)  { return t > (ulong)T.MaxValue ? T.MaxValue : (T)t; }
		public T Clip(int t)    { return		                    t < (int)T.MinValue ? T.MinValue : (T)t; }
		public T Clip(long t)   { return t > (long)T.MaxValue ? T.MaxValue : 		                    t < (long)T.MinValue ? T.MinValue : (T)t; }
		public T Clip(double t) { return (T)MathEx.InRange(t, (double)0, (double)T.MaxValue); }

		public bool IsLess(T a, T b)        { return a < b; }
		public bool IsLessOrEqual(T a, T b) { return a <= b; }
		public T Abs(T a)                   { return a; }
		public T Max(T a, T b)              { return a > b ? a : b; }
		public T Min(T a, T b)              { return a < b ? a : b; }
		public int Compare(T x, T y)        { return x.CompareTo(y); }
		public bool Equals(T x, T y)        { return x == y; }
		public int GetHashCode(T x)         { return x.GetHashCode(); }

		public T Incremented(T a)           { a++; return a; }
		public T Decremented(T a)           { a--; return a; }
		public T NextHigher(T a)            { a++; return a; }
		public T NextLower(T a)             { a--; return a; }

		public T Add(T a, T b)              { return (T)(a + b); }
		public T Subtract(T a, T b)         { return (T)(a - b); }
		public T Multiply(T a, T b)         { return (T)(a * b); }
		public T Divide(T a, T b)           { return (T)(a / b); }
		public T MulDiv(T a, T mul, T div)  { return (T)MathEx.MulDiv(a, mul, div); }

		public T ShiftLeft(T a, int amount)  { return (T)(a << amount); }
		public T ShiftRight(T a, int amount) { return (T)(a >> amount); }

		public T Sqrt(T a)   { return (T)MathEx.Sqrt(a); }
		public T Square(T a) { return (T)(a * a); }

		#endregion

		#region BinaryMath

		public T And(T a, T b) { return (T)(a & b); }
		public T Or(T a, T b)  { return (T)(a | b); }
		public T Xor(T a, T b) { return (T)(a ^ b); }
		public T Not(T a)      { return (T)~a; }

		public int CountOnes(T a)     { return MathEx.CountOnes(a); }
		public int Log2Floor(T a)     { return MathEx.Log2Floor(a); }

		#endregion
	}
}

namespace Loyc.Math
{
	using System;
	using T = System.Int64;

	public struct MathL : IIntMath<long>
	{
		public static readonly MathL Value = new MathL();

		#region INumTraits

		public T MinValue           { get { return T.MinValue; } }
		public T MaxValue           { get { return T.MaxValue; } }
		public T Epsilon            { get { return 1; } }
		public T PositiveInfinity   { get { return T.MaxValue; } }
		public T NegativeInfinity   { get { return T.MinValue; } }
		public bool IsInfinity(T value)   { return false; }
		public bool IsNaN(T value)        { return false; }
		public bool IsSigned        { get { return true; } }
		public bool IsFloatingPoint { get { return false; } }
		public bool IsInteger       { get { return true; } }
		public bool IsOrdered       { get { return true; } }
		public int SignificantBits  { get { return 63; } }
		public int MaxIntPowerOf2   { get { return 63; } }
		public ulong MaxInt { get { return (ulong)(ulong)System.Int64.MaxValue; } }
		public long MinInt  { get { return (long)(long)System.Int64.MinValue; } }
		public T Zero       { get { return (long)0; } }
		public T One        { get { return (long)1; } }

		#endregion

		#region ISignedMath

		public T From(uint t)   { return (T)t; }
		public T From(int t)    { return (T)t; }
		public T From(ulong t)  { return (T)t; }
		public T From(long t)   { return (T)t; }
		public T From(double t) { return (T)t; }

		public T Clip(uint t)   { return(T)t; }
		public T Clip(ulong t)  { return t > (ulong)T.MaxValue ? T.MaxValue : (T)t; }
		public T Clip(int t)    { return		                   (T)t; }
		public T Clip(long t)   { return		                   (T)t; }
		public T Clip(double t) { return (T)MathEx.InRange(t, (double)0, (double)T.MaxValue); }

		public bool IsLess(T a, T b)        { return a < b; }
		public bool IsLessOrEqual(T a, T b) { return a <= b; }
		public T Abs(T a)                   { return (T)(a >= Zero ? a : -a); }
		public T Max(T a, T b)              { return a > b ? a : b; }
		public T Min(T a, T b)              { return a < b ? a : b; }
		public int Compare(T x, T y)        { return x.CompareTo(y); }
		public bool Equals(T x, T y)        { return x == y; }
		public int GetHashCode(T x)         { return x.GetHashCode(); }

		public T Incremented(T a)           { a++; return a; }
		public T Decremented(T a)           { a--; return a; }
		public T NextHigher(T a)            { a++; return a; }
		public T NextLower(T a)             { a--; return a; }

		public T Add(T a, T b)              { return (T)(a + b); }
		public T Subtract(T a, T b)         { return (T)(a - b); }
		public T Multiply(T a, T b)         { return (T)(a * b); }
		public T Divide(T a, T b)           { return (T)(a / b); }
		public T MulDiv(T a, T mul, T div)  { return (T)MathEx.MulDiv(a, mul, div); }

		public T Negate(T a) { return (T)(-a); }

		public T ShiftLeft(T a, int amount)  { return (T)(a << amount); }
		public T ShiftRight(T a, int amount) { return (T)(a >> amount); }

		public T Sqrt(T a)   { return (T)MathEx.Sqrt(a); }
		public T Square(T a) { return (T)(a * a); }

		#endregion

		#region BinaryMath

		public T And(T a, T b) { return (T)(a & b); }
		public T Or(T a, T b)  { return (T)(a | b); }
		public T Xor(T a, T b) { return (T)(a ^ b); }
		public T Not(T a)      { return (T)~a; }

		public int CountOnes(T a)     { return MathEx.CountOnes(a); }
		public int Log2Floor(T a)     { return MathEx.Log2Floor(a); }

		#endregion
	}
}

namespace Loyc.Math
{
	using System;
	using T = System.UInt64;

	public struct MathUL : IUIntMath<ulong>
	{
		public static readonly MathUL Value = new MathUL();

		#region INumTraits

		public T MinValue           { get { return T.MinValue; } }
		public T MaxValue           { get { return T.MaxValue; } }
		public T Epsilon            { get { return 1; } }
		public T PositiveInfinity   { get { return T.MaxValue; } }
		public T NegativeInfinity   { get { throw new NotSupportedException(); } }
		public bool IsInfinity(T value)   { return false; }
		public bool IsNaN(T value)        { return false; }
		public bool IsSigned        { get { return false; } }
		public bool IsFloatingPoint { get { return false; } }
		public bool IsInteger       { get { return true; } }
		public bool IsOrdered       { get { return true; } }
		public int SignificantBits  { get { return 64; } }
		public int MaxIntPowerOf2   { get { return 64; } }
		public ulong MaxInt { get { return (ulong)(ulong)System.UInt64.MaxValue; } }
		public long MinInt  { get { return (long)(long)System.UInt64.MinValue; } }
		public T Zero       { get { return (ulong)0; } }
		public T One        { get { return (ulong)1; } }

		#endregion

		#region IMath

		public T From(uint t)   { return (T)t; }
		public T From(int t)    { return (T)t; }
		public T From(ulong t)  { return (T)t; }
		public T From(long t)   { return (T)t; }
		public T From(double t) { return (T)t; }

		public T Clip(uint t)   { return(T)t; }
		public T Clip(ulong t)  { return(T)t; }
		public T Clip(int t)    { return		                    t < (int)T.MinValue ? T.MinValue : (T)t; }
		public T Clip(long t)   { return		                    t < (long)T.MinValue ? T.MinValue : (T)t; }
		public T Clip(double t) { return (T)MathEx.InRange(t, (double)0, (double)T.MaxValue); }

		public bool IsLess(T a, T b)        { return a < b; }
		public bool IsLessOrEqual(T a, T b) { return a <= b; }
		public T Abs(T a)                   { return a; }
		public T Max(T a, T b)              { return a > b ? a : b; }
		public T Min(T a, T b)              { return a < b ? a : b; }
		public int Compare(T x, T y)        { return x.CompareTo(y); }
		public bool Equals(T x, T y)        { return x == y; }
		public int GetHashCode(T x)         { return x.GetHashCode(); }

		public T Incremented(T a)           { a++; return a; }
		public T Decremented(T a)           { a--; return a; }
		public T NextHigher(T a)            { a++; return a; }
		public T NextLower(T a)             { a--; return a; }

		public T Add(T a, T b)              { return (T)(a + b); }
		public T Subtract(T a, T b)         { return (T)(a - b); }
		public T Multiply(T a, T b)         { return (T)(a * b); }
		public T Divide(T a, T b)           { return (T)(a / b); }
		public T MulDiv(T a, T mul, T div)  { return (T)MathEx.MulDiv(a, mul, div); }

		public T ShiftLeft(T a, int amount)  { return (T)(a << amount); }
		public T ShiftRight(T a, int amount) { return (T)(a >> amount); }

		public T Sqrt(T a)   { return (T)MathEx.Sqrt(a); }
		public T Square(T a) { return (T)(a * a); }

		#endregion

		#region BinaryMath

		public T And(T a, T b) { return (T)(a & b); }
		public T Or(T a, T b)  { return (T)(a | b); }
		public T Xor(T a, T b) { return (T)(a ^ b); }
		public T Not(T a)      { return (T)~a; }

		public int CountOnes(T a)     { return MathEx.CountOnes(a); }
		public int Log2Floor(T a)     { return MathEx.Log2Floor(a); }

		#endregion
	}
}

namespace Loyc.Math
{
	using System;
	using T = System.Single;

	public struct MathF : IFloatMath<float>
	{
		public static readonly MathF Value = new MathF();

		#region INumTraits

		public T MinValue           { get { return T.MinValue; } }
		public T MaxValue           { get { return T.MaxValue; } }
		public T Epsilon            { get { return T.Epsilon; } }
		public T PositiveInfinity   { get { return T.PositiveInfinity; } }
		public T NegativeInfinity   { get { return T.NegativeInfinity; } }
		public bool IsInfinity(T value)   { return T.IsInfinity(value); }
		public bool IsNaN(T value)        { return T.IsNaN(value); }
		public bool IsSigned        { get { return true; } }
		public bool IsFloatingPoint { get { return true; } }
		public bool IsInteger       { get { return false; } }
		public bool IsOrdered       { get { return true; } }
		public int SignificantBits  { get { return 24; } }
		public int MaxIntPowerOf2   { get { return 128; } }
		public ulong MaxInt { get { return (ulong)ulong.MaxValue; } }
		public long MinInt  { get { return (long)long.MinValue; } }
		public T Zero       { get { return (float)0; } }
		public T One        { get { return (float)1; } }

		#endregion

		#region ISignedMath

		public T From(uint t)   { return (T)t; }
		public T From(int t)    { return (T)t; }
		public T From(ulong t)  { return (T)t; }
		public T From(long t)   { return (T)t; }
		public T From(double t) { return (T)t; }

		
		public T Clip(uint t)   { return (T)t; }
		public T Clip(int t)    { return (T)t; }
		public T Clip(ulong t)  { return (T)t; }
		public T Clip(long t)   { return (T)t; }
		public T Clip(double t) { return (T)t; }

		public bool IsLess(T a, T b)        { return a < b; }
		public bool IsLessOrEqual(T a, T b) { return a <= b; }
		public T Abs(T a)                   { return (T)(a >= Zero ? a : -a); }
		public T Max(T a, T b)              { return a > b ? a : b; }
		public T Min(T a, T b)              { return a < b ? a : b; }
		public int Compare(T x, T y)        { return x.CompareTo(y); }
		public bool Equals(T x, T y)        { return x == y; }
		public int GetHashCode(T x)         { return x.GetHashCode(); }

		public T Incremented(T a)           { a++; return a; }
		public T Decremented(T a)           { a--; return a; }
		public T NextHigher(T a)            { return MathEx.NextHigher(a); }
		public T NextLower(T a)             { return MathEx.NextLower(a); }

		public T Add(T a, T b)              { return (T)(a + b); }
		public T Subtract(T a, T b)         { return (T)(a - b); }
		public T Multiply(T a, T b)         { return (T)(a * b); }
		public T Divide(T a, T b)           { return (T)(a / b); }
		public T MulDiv(T a, T mul, T div)  { return (T)(a * mul / div); }

		public T Reciprocal(T a) { return One / a; }
		public T Negate(T a) { return (T)(-a); }

		public T ShiftLeft(T a, int amount)  { return MathEx.ShiftLeft(a, amount); }
		public T ShiftRight(T a, int amount) { return MathEx.ShiftRight(a, amount); }

		public T Sqrt(T a)   { return (T)Math.Sqrt(a); }
		public T Square(T a) { return (T)(a * a); }

		#endregion

		#region ITrigonometry & IExp Members

		public T Asin(T a) { return (T)Math.Asin(a); }
		public T Acos(T a) { return (T)Math.Acos(a); }
		public T Atan(T a) { return (T)Math.Atan(a); }
		public T Atan2(T y, T x) { return (T)Math.Atan2(y, x); }

		public T Sin(T a) { return (T)Math.Sin(a); }
		public T Cos(T a) { return (T)Math.Cos(a); }
		public T Tan(T a) { return (T)Math.Tan(a); }

		public T Exp(T a)                 { return (T)Math.Exp(a); }
		public T Pow(T @base, T exponent) { return (T)Math.Pow(@base, exponent); }
		public T Ln(T a)                  { return (T)Math.Log(a); }
		public T Log(T a, T @base)        { return (T)Math.Log(a, @base); }

		#endregion
	}
}

namespace Loyc.Math
{
	using System;
	using T = System.Double;

	public struct MathD : IFloatMath<double>
	{
		public static readonly MathD Value = new MathD();

		#region INumTraits

		public T MinValue           { get { return T.MinValue; } }
		public T MaxValue           { get { return T.MaxValue; } }
		public T Epsilon            { get { return T.Epsilon; } }
		public T PositiveInfinity   { get { return T.PositiveInfinity; } }
		public T NegativeInfinity   { get { return T.NegativeInfinity; } }
		public bool IsInfinity(T value)   { return T.IsInfinity(value); }
		public bool IsNaN(T value)        { return T.IsNaN(value); }
		public bool IsSigned        { get { return true; } }
		public bool IsFloatingPoint { get { return true; } }
		public bool IsInteger       { get { return false; } }
		public bool IsOrdered       { get { return true; } }
		public int SignificantBits  { get { return 53; } }
		public int MaxIntPowerOf2   { get { return 1024; } }
		public ulong MaxInt { get { return (ulong)ulong.MaxValue; } }
		public long MinInt  { get { return (long)long.MinValue; } }
		public T Zero       { get { return (double)0; } }
		public T One        { get { return (double)1; } }

		#endregion

		#region ISignedMath

		public T From(uint t)   { return (T)t; }
		public T From(int t)    { return (T)t; }
		public T From(ulong t)  { return (T)t; }
		public T From(long t)   { return (T)t; }
		public T From(double t) { return (T)t; }

		
		public T Clip(uint t)   { return (T)t; }
		public T Clip(int t)    { return (T)t; }
		public T Clip(ulong t)  { return (T)t; }
		public T Clip(long t)   { return (T)t; }
		public T Clip(double t) { return (T)t; }

		public bool IsLess(T a, T b)        { return a < b; }
		public bool IsLessOrEqual(T a, T b) { return a <= b; }
		public T Abs(T a)                   { return (T)(a >= Zero ? a : -a); }
		public T Max(T a, T b)              { return a > b ? a : b; }
		public T Min(T a, T b)              { return a < b ? a : b; }
		public int Compare(T x, T y)        { return x.CompareTo(y); }
		public bool Equals(T x, T y)        { return x == y; }
		public int GetHashCode(T x)         { return x.GetHashCode(); }

		public T Incremented(T a)           { a++; return a; }
		public T Decremented(T a)           { a--; return a; }
		public T NextHigher(T a)            { return MathEx.NextHigher(a); }
		public T NextLower(T a)             { return MathEx.NextLower(a); }

		public T Add(T a, T b)              { return (T)(a + b); }
		public T Subtract(T a, T b)         { return (T)(a - b); }
		public T Multiply(T a, T b)         { return (T)(a * b); }
		public T Divide(T a, T b)           { return (T)(a / b); }
		public T MulDiv(T a, T mul, T div)  { return (T)(a * mul / div); }

		public T Reciprocal(T a) { return One / a; }
		public T Negate(T a) { return (T)(-a); }

		public T ShiftLeft(T a, int amount)  { return MathEx.ShiftLeft(a, amount); }
		public T ShiftRight(T a, int amount) { return MathEx.ShiftRight(a, amount); }

		public T Sqrt(T a)   { return (T)Math.Sqrt(a); }
		public T Square(T a) { return (T)(a * a); }

		#endregion

		#region ITrigonometry & IExp Members

		public T Asin(T a) { return (T)Math.Asin(a); }
		public T Acos(T a) { return (T)Math.Acos(a); }
		public T Atan(T a) { return (T)Math.Atan(a); }
		public T Atan2(T y, T x) { return (T)Math.Atan2(y, x); }

		public T Sin(T a) { return (T)Math.Sin(a); }
		public T Cos(T a) { return (T)Math.Cos(a); }
		public T Tan(T a) { return (T)Math.Tan(a); }

		public T Exp(T a)                 { return (T)Math.Exp(a); }
		public T Pow(T @base, T exponent) { return (T)Math.Pow(@base, exponent); }
		public T Ln(T a)                  { return (T)Math.Log(a); }
		public T Log(T a, T @base)        { return (T)Math.Log(a, @base); }

		#endregion
	}
}

namespace Loyc.Math
{
	using System;
	using T = FPI8;

	public struct MathF8 : IRationalMath<T>, IBinaryMath<T>
	{
		public static readonly MathF8 Value = new MathF8();

		#region INumTraits

		public T MinValue           { get { return T.MinValue; } }
		public T MaxValue           { get { return T.MaxValue; } }
		public T Epsilon            { get { return T.Epsilon; } }
		public T PositiveInfinity   { get { return T.MaxValue; } }
		public T NegativeInfinity   { get { return T.MinValue; } }
		public bool IsInfinity(T value)   { return false; }
		public bool IsNaN(T value)        { return false; }
		public bool IsSigned        { get { return true; } }
		public bool IsFloatingPoint { get { return false; } }
		public bool IsInteger       { get { return false; } }
		public bool IsOrdered       { get { return true; } }
		public int SignificantBits  { get { return 31; } }
		public int MaxIntPowerOf2   { get { return 23; } }
		public ulong MaxInt { get { return (ulong)(ulong)FPI8.MaxValue; } }
		public long MinInt  { get { return (long)(long)FPI8.MinValue; } }
		public T Zero       { get { return FPI8.Zero; } }
		public T One        { get { return FPI8.One; } }

		#endregion

		#region ISignedMath

		public T From(uint t)   { return T.FastCast(t); }
		public T From(int t)    { return T.FastCast(t); }
		public T From(ulong t)  { return T.FastCast((long)t); }
		public T From(long t)   { return T.FastCast(t); }
		public T From(double t) { return T.FastCast(t); }

		public T Clip(uint t)   { return new T(t); }
		public T Clip(int t)    { return new T(t); }
		public T Clip(ulong t)  { return new T(t); }
		public T Clip(long t)   { return new T(t); }
		public T Clip(double t) { return new T(t); }

		public bool IsLess(T a, T b)        { return a < b; }
		public bool IsLessOrEqual(T a, T b) { return a <= b; }
		public T Abs(T a)                   { return (T)(a >= Zero ? a : -a); }
		public T Max(T a, T b)              { return a > b ? a : b; }
		public T Min(T a, T b)              { return a < b ? a : b; }
		public int Compare(T x, T y)        { return x.CompareTo(y); }
		public bool Equals(T x, T y)        { return x == y; }
		public int GetHashCode(T x)         { return x.GetHashCode(); }

		public T Incremented(T a)           { a++; return a; }
		public T Decremented(T a)           { a--; return a; }
		public T NextHigher(T a)            { a++; return a; }
		public T NextLower(T a)             { a--; return a; }

		public T Add(T a, T b)              { return (T)(a + b); }
		public T Subtract(T a, T b)         { return (T)(a - b); }
		public T Multiply(T a, T b)         { return (T)(a * b); }
		public T Divide(T a, T b)           { return (T)(a / b); }
		public T MulDiv(T a, T mul, T div)  { return (T)a.MulDiv(mul, div); }

		public T Reciprocal(T a) { return One / a; }
		public T Negate(T a) { return (T)(-a); }

		public T ShiftLeft(T a, int amount)  { return (T)(a << amount); }
		public T ShiftRight(T a, int amount) { return (T)(a >> amount); }

		public T Sqrt(T a)   { return a.Sqrt(); }
		public T Square(T a) { return (T)(a * a); }

		#endregion

		#region BinaryMath

		public T And(T a, T b) { return (T)(a & b); }
		public T Or(T a, T b)  { return (T)(a | b); }
		public T Xor(T a, T b) { return (T)(a ^ b); }
		public T Not(T a)      { return (T)~a; }

		public int CountOnes(T a)     { return a.CountOnes(); }
		public int Log2Floor(T a)     { return a.Log2Floor(); }

		#endregion
	}
}

namespace Loyc.Math
{
	using System;
	using T = FPI16;

	public struct MathF16 : IRationalMath<T>, IBinaryMath<T>
	{
		public static readonly MathF16 Value = new MathF16();

		#region INumTraits

		public T MinValue           { get { return T.MinValue; } }
		public T MaxValue           { get { return T.MaxValue; } }
		public T Epsilon            { get { return T.Epsilon; } }
		public T PositiveInfinity   { get { return T.MaxValue; } }
		public T NegativeInfinity   { get { return T.MinValue; } }
		public bool IsInfinity(T value)   { return false; }
		public bool IsNaN(T value)        { return false; }
		public bool IsSigned        { get { return true; } }
		public bool IsFloatingPoint { get { return false; } }
		public bool IsInteger       { get { return false; } }
		public bool IsOrdered       { get { return true; } }
		public int SignificantBits  { get { return 31; } }
		public int MaxIntPowerOf2   { get { return 23; } }
		public ulong MaxInt { get { return (ulong)(ulong)FPI16.MaxValue; } }
		public long MinInt  { get { return (long)(long)FPI16.MinValue; } }
		public T Zero       { get { return FPI16.Zero; } }
		public T One        { get { return FPI16.One; } }

		#endregion

		#region ISignedMath

		public T From(uint t)   { return T.FastCast(t); }
		public T From(int t)    { return T.FastCast(t); }
		public T From(ulong t)  { return T.FastCast((long)t); }
		public T From(long t)   { return T.FastCast(t); }
		public T From(double t) { return T.FastCast(t); }

		public T Clip(uint t)   { return new T(t); }
		public T Clip(int t)    { return new T(t); }
		public T Clip(ulong t)  { return new T(t); }
		public T Clip(long t)   { return new T(t); }
		public T Clip(double t) { return new T(t); }

		public bool IsLess(T a, T b)        { return a < b; }
		public bool IsLessOrEqual(T a, T b) { return a <= b; }
		public T Abs(T a)                   { return (T)(a >= Zero ? a : -a); }
		public T Max(T a, T b)              { return a > b ? a : b; }
		public T Min(T a, T b)              { return a < b ? a : b; }
		public int Compare(T x, T y)        { return x.CompareTo(y); }
		public bool Equals(T x, T y)        { return x == y; }
		public int GetHashCode(T x)         { return x.GetHashCode(); }

		public T Incremented(T a)           { a++; return a; }
		public T Decremented(T a)           { a--; return a; }
		public T NextHigher(T a)            { a++; return a; }
		public T NextLower(T a)             { a--; return a; }

		public T Add(T a, T b)              { return (T)(a + b); }
		public T Subtract(T a, T b)         { return (T)(a - b); }
		public T Multiply(T a, T b)         { return (T)(a * b); }
		public T Divide(T a, T b)           { return (T)(a / b); }
		public T MulDiv(T a, T mul, T div)  { return (T)a.MulDiv(mul, div); }

		public T Reciprocal(T a) { return One / a; }
		public T Negate(T a) { return (T)(-a); }

		public T ShiftLeft(T a, int amount)  { return (T)(a << amount); }
		public T ShiftRight(T a, int amount) { return (T)(a >> amount); }

		public T Sqrt(T a)   { return a.Sqrt(); }
		public T Square(T a) { return (T)(a * a); }

		#endregion

		#region BinaryMath

		public T And(T a, T b) { return (T)(a & b); }
		public T Or(T a, T b)  { return (T)(a | b); }
		public T Xor(T a, T b) { return (T)(a ^ b); }
		public T Not(T a)      { return (T)~a; }

		public int CountOnes(T a)     { return a.CountOnes(); }
		public int Log2Floor(T a)     { return a.Log2Floor(); }

		#endregion
	}
}

namespace Loyc.Math
{
	using System;
	using T = FPI23;

	public struct MathF23 : IRationalMath<T>, IBinaryMath<T>
	{
		public static readonly MathF23 Value = new MathF23();

		#region INumTraits

		public T MinValue           { get { return T.MinValue; } }
		public T MaxValue           { get { return T.MaxValue; } }
		public T Epsilon            { get { return T.Epsilon; } }
		public T PositiveInfinity   { get { return T.MaxValue; } }
		public T NegativeInfinity   { get { return T.MinValue; } }
		public bool IsInfinity(T value)   { return false; }
		public bool IsNaN(T value)        { return false; }
		public bool IsSigned        { get { return true; } }
		public bool IsFloatingPoint { get { return false; } }
		public bool IsInteger       { get { return false; } }
		public bool IsOrdered       { get { return true; } }
		public int SignificantBits  { get { return 31; } }
		public int MaxIntPowerOf2   { get { return 23; } }
		public ulong MaxInt { get { return (ulong)(ulong)FPI23.MaxValue; } }
		public long MinInt  { get { return (long)(long)FPI23.MinValue; } }
		public T Zero       { get { return FPI23.Zero; } }
		public T One        { get { return FPI23.One; } }

		#endregion

		#region ISignedMath

		public T From(uint t)   { return T.FastCast(t); }
		public T From(int t)    { return T.FastCast(t); }
		public T From(ulong t)  { return T.FastCast((long)t); }
		public T From(long t)   { return T.FastCast(t); }
		public T From(double t) { return T.FastCast(t); }

		public T Clip(uint t)   { return new T(t); }
		public T Clip(int t)    { return new T(t); }
		public T Clip(ulong t)  { return new T(t); }
		public T Clip(long t)   { return new T(t); }
		public T Clip(double t) { return new T(t); }

		public bool IsLess(T a, T b)        { return a < b; }
		public bool IsLessOrEqual(T a, T b) { return a <= b; }
		public T Abs(T a)                   { return (T)(a >= Zero ? a : -a); }
		public T Max(T a, T b)              { return a > b ? a : b; }
		public T Min(T a, T b)              { return a < b ? a : b; }
		public int Compare(T x, T y)        { return x.CompareTo(y); }
		public bool Equals(T x, T y)        { return x == y; }
		public int GetHashCode(T x)         { return x.GetHashCode(); }

		public T Incremented(T a)           { a++; return a; }
		public T Decremented(T a)           { a--; return a; }
		public T NextHigher(T a)            { a++; return a; }
		public T NextLower(T a)             { a--; return a; }

		public T Add(T a, T b)              { return (T)(a + b); }
		public T Subtract(T a, T b)         { return (T)(a - b); }
		public T Multiply(T a, T b)         { return (T)(a * b); }
		public T Divide(T a, T b)           { return (T)(a / b); }
		public T MulDiv(T a, T mul, T div)  { return (T)a.MulDiv(mul, div); }

		public T Reciprocal(T a) { return One / a; }
		public T Negate(T a) { return (T)(-a); }

		public T ShiftLeft(T a, int amount)  { return (T)(a << amount); }
		public T ShiftRight(T a, int amount) { return (T)(a >> amount); }

		public T Sqrt(T a)   { return a.Sqrt(); }
		public T Square(T a) { return (T)(a * a); }

		#endregion

		#region BinaryMath

		public T And(T a, T b) { return (T)(a & b); }
		public T Or(T a, T b)  { return (T)(a | b); }
		public T Xor(T a, T b) { return (T)(a ^ b); }
		public T Not(T a)      { return (T)~a; }

		public int CountOnes(T a)     { return a.CountOnes(); }
		public int Log2Floor(T a)     { return a.Log2Floor(); }

		#endregion
	}
}

namespace Loyc.Math
{
	using System;
	using T = FPL16;

	public struct MathFL16 : IRationalMath<T>, IBinaryMath<T>
	{
		public static readonly MathFL16 Value = new MathFL16();

		#region INumTraits

		public T MinValue           { get { return T.MinValue; } }
		public T MaxValue           { get { return T.MaxValue; } }
		public T Epsilon            { get { return T.Epsilon; } }
		public T PositiveInfinity   { get { return T.MaxValue; } }
		public T NegativeInfinity   { get { return T.MinValue; } }
		public bool IsInfinity(T value)   { return false; }
		public bool IsNaN(T value)        { return false; }
		public bool IsSigned        { get { return true; } }
		public bool IsFloatingPoint { get { return false; } }
		public bool IsInteger       { get { return false; } }
		public bool IsOrdered       { get { return true; } }
		public int SignificantBits  { get { return 63; } }
		public int MaxIntPowerOf2   { get { return 47; } }
		public ulong MaxInt { get { return (ulong)(ulong)FPL16.MaxValue; } }
		public long MinInt  { get { return (long)(long)FPL16.MinValue; } }
		public T Zero       { get { return FPL16.Zero; } }
		public T One        { get { return FPL16.One; } }

		#endregion

		#region ISignedMath

		public T From(uint t)   { return T.FastCast(t); }
		public T From(int t)    { return T.FastCast(t); }
		public T From(ulong t)  { return T.FastCast((long)t); }
		public T From(long t)   { return T.FastCast(t); }
		public T From(double t) { return T.FastCast(t); }

		public T Clip(uint t)   { return new T(t); }
		public T Clip(int t)    { return new T(t); }
		public T Clip(ulong t)  { return new T(t); }
		public T Clip(long t)   { return new T(t); }
		public T Clip(double t) { return new T(t); }

		public bool IsLess(T a, T b)        { return a < b; }
		public bool IsLessOrEqual(T a, T b) { return a <= b; }
		public T Abs(T a)                   { return (T)(a >= Zero ? a : -a); }
		public T Max(T a, T b)              { return a > b ? a : b; }
		public T Min(T a, T b)              { return a < b ? a : b; }
		public int Compare(T x, T y)        { return x.CompareTo(y); }
		public bool Equals(T x, T y)        { return x == y; }
		public int GetHashCode(T x)         { return x.GetHashCode(); }

		public T Incremented(T a)           { a++; return a; }
		public T Decremented(T a)           { a--; return a; }
		public T NextHigher(T a)            { a++; return a; }
		public T NextLower(T a)             { a--; return a; }

		public T Add(T a, T b)              { return (T)(a + b); }
		public T Subtract(T a, T b)         { return (T)(a - b); }
		public T Multiply(T a, T b)         { return (T)(a * b); }
		public T Divide(T a, T b)           { return (T)(a / b); }
		public T MulDiv(T a, T mul, T div)  { return (T)a.MulDiv(mul, div); }

		public T Reciprocal(T a) { return One / a; }
		public T Negate(T a) { return (T)(-a); }

		public T ShiftLeft(T a, int amount)  { return (T)(a << amount); }
		public T ShiftRight(T a, int amount) { return (T)(a >> amount); }

		public T Sqrt(T a)   { return a.Sqrt(); }
		public T Square(T a) { return (T)(a * a); }

		#endregion

		#region BinaryMath

		public T And(T a, T b) { return (T)(a & b); }
		public T Or(T a, T b)  { return (T)(a | b); }
		public T Xor(T a, T b) { return (T)(a ^ b); }
		public T Not(T a)      { return (T)~a; }

		public int CountOnes(T a)     { return a.CountOnes(); }
		public int Log2Floor(T a)     { return a.Log2Floor(); }

		#endregion
	}
}

namespace Loyc.Math
{
	using System;
	using T = FPL32;

	public struct MathFL32 : IRationalMath<T>, IBinaryMath<T>
	{
		public static readonly MathFL32 Value = new MathFL32();

		#region INumTraits

		public T MinValue           { get { return T.MinValue; } }
		public T MaxValue           { get { return T.MaxValue; } }
		public T Epsilon            { get { return T.Epsilon; } }
		public T PositiveInfinity   { get { return T.MaxValue; } }
		public T NegativeInfinity   { get { return T.MinValue; } }
		public bool IsInfinity(T value)   { return false; }
		public bool IsNaN(T value)        { return false; }
		public bool IsSigned        { get { return true; } }
		public bool IsFloatingPoint { get { return false; } }
		public bool IsInteger       { get { return false; } }
		public bool IsOrdered       { get { return true; } }
		public int SignificantBits  { get { return 63; } }
		public int MaxIntPowerOf2   { get { return 31; } }
		public ulong MaxInt { get { return (ulong)(ulong)FPL32.MaxValue; } }
		public long MinInt  { get { return (long)(long)FPL32.MinValue; } }
		public T Zero       { get { return FPL32.Zero; } }
		public T One        { get { return FPL32.One; } }

		#endregion

		#region ISignedMath

		public T From(uint t)   { return T.FastCast(t); }
		public T From(int t)    { return T.FastCast(t); }
		public T From(ulong t)  { return T.FastCast((long)t); }
		public T From(long t)   { return T.FastCast(t); }
		public T From(double t) { return T.FastCast(t); }

		public T Clip(uint t)   { return new T(t); }
		public T Clip(int t)    { return new T(t); }
		public T Clip(ulong t)  { return new T(t); }
		public T Clip(long t)   { return new T(t); }
		public T Clip(double t) { return new T(t); }

		public bool IsLess(T a, T b)        { return a < b; }
		public bool IsLessOrEqual(T a, T b) { return a <= b; }
		public T Abs(T a)                   { return (T)(a >= Zero ? a : -a); }
		public T Max(T a, T b)              { return a > b ? a : b; }
		public T Min(T a, T b)              { return a < b ? a : b; }
		public int Compare(T x, T y)        { return x.CompareTo(y); }
		public bool Equals(T x, T y)        { return x == y; }
		public int GetHashCode(T x)         { return x.GetHashCode(); }

		public T Incremented(T a)           { a++; return a; }
		public T Decremented(T a)           { a--; return a; }
		public T NextHigher(T a)            { a++; return a; }
		public T NextLower(T a)             { a--; return a; }

		public T Add(T a, T b)              { return (T)(a + b); }
		public T Subtract(T a, T b)         { return (T)(a - b); }
		public T Multiply(T a, T b)         { return (T)(a * b); }
		public T Divide(T a, T b)           { return (T)(a / b); }
		public T MulDiv(T a, T mul, T div)  { return (T)a.MulDiv(mul, div); }

		public T Reciprocal(T a) { return One / a; }
		public T Negate(T a) { return (T)(-a); }

		public T ShiftLeft(T a, int amount)  { return (T)(a << amount); }
		public T ShiftRight(T a, int amount) { return (T)(a >> amount); }

		public T Sqrt(T a)   { return a.Sqrt(); }
		public T Square(T a) { return (T)(a * a); }

		#endregion

		#region BinaryMath

		public T And(T a, T b) { return (T)(a & b); }
		public T Or(T a, T b)  { return (T)(a | b); }
		public T Xor(T a, T b) { return (T)(a ^ b); }
		public T Not(T a)      { return (T)~a; }

		public int CountOnes(T a)     { return a.CountOnes(); }
		public int Log2Floor(T a)     { return a.Log2Floor(); }

		#endregion
	}
}
