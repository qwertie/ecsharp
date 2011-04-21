

namespace Loyc.Math
{
	using System;
	using System.Collections.Generic;
	using T = System.SByte;

	public class MathI8 : Comparer<T>, IIntMath<sbyte>
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
		public T Zero       { get { return (T)0; } }
		public T One        { get { return (T)1; } }

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

		public bool IsLess(T a, T b)          { return a < b; }
		public bool IsLessOrEqual(T a, T b)   { return a <= b; }
		public T Abs(T a)                     { return (T)(a >= Zero ? a : -a); }
		public T Max(T a, T b)                { return a > b ? a : b; }
		public T Min(T a, T b)                { return a < b ? a : b; }
		public override int Compare(T x, T y) { return x.CompareTo(y); }
		public bool Equals(T x, T y)          { return x == y; }
		public int GetHashCode(T x)           { return x.GetHashCode(); }

		public T Incremented(T a) { return (T)(a + 1); }
		public T Decremented(T a) { return (T)(a - 1); }
		public T NextHigher(T a)  { return (T)(a + 1); }
		public T NextLower(T a)   { return (T)(a - 1); }

		public T Add(T a, T b)      { return (T)(a + b); }
		public T Subtract(T a, T b) { return (T)(a - b); }
		public T Multiply(T a, T b) { return (T)(a * b); }
		public T Divide(T a, T b)   { return (T)(a / b); }

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
	/*
    public class CheckedMathU8 : MathU8, IUIntMath<T>
	{
		#region IMath

		public new T From(uint t)   { return checked((T)t); }
		public new T From(int t)    { return checked((T)t); }
		public new T From(ulong t)  { return checked((T)t); }
		public new T From(long t)   { return checked((T)t); }
		public new T From(double t) { return checked((T)t); }

		public new T Incremented(T a) { return checked((T)(a + 1)); }
		public new T Decremented(T a) { return checked((T)(a - 1)); }
		public new T NextHigher(T a)  { return checked((T)(a + 1)); }
		public new T NextLower(T a)   { return checked((T)(a - 1)); }

		public new T Add(T a, T b)      { return checked((T)(a + b)); }
		public new T Subtract(T a, T b) { return checked((T)(a - b)); }
		public new T Multiply(T a, T b) { return checked((T)(a * b)); }
		public new T Divide(T a, T b)   { return checked((T)(a / b)); }

		public new T ShiftLeft(T a, int amount)  { return checked((T)(a << amount)); }
		public new T ShiftRight(T a, int amount) { return checked((T)(a >> amount)); }

		public new T Square(T a) { return checked((T)(a * a)); }

		#endregion
	}*/
}

namespace Loyc.Math
{
	using System;
	using System.Collections.Generic;
	using T = System.Byte;

	public class MathU8 : Comparer<T>, IUIntMath<byte>
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
		public T Zero       { get { return (T)0; } }
		public T One        { get { return (T)1; } }

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

		public bool IsLess(T a, T b)          { return a < b; }
		public bool IsLessOrEqual(T a, T b)   { return a <= b; }
		public T Abs(T a)                     { return a; }
		public T Max(T a, T b)                { return a > b ? a : b; }
		public T Min(T a, T b)                { return a < b ? a : b; }
		public override int Compare(T x, T y) { return x.CompareTo(y); }
		public bool Equals(T x, T y)          { return x == y; }
		public int GetHashCode(T x)           { return x.GetHashCode(); }

		public T Incremented(T a) { return (T)(a + 1); }
		public T Decremented(T a) { return (T)(a - 1); }
		public T NextHigher(T a)  { return (T)(a + 1); }
		public T NextLower(T a)   { return (T)(a - 1); }

		public T Add(T a, T b)      { return (T)(a + b); }
		public T Subtract(T a, T b) { return (T)(a - b); }
		public T Multiply(T a, T b) { return (T)(a * b); }
		public T Divide(T a, T b)   { return (T)(a / b); }

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
	/*
    public class CheckedMathU8 : MathU8, IUIntMath<T>
	{
		#region IMath

		public new T From(uint t)   { return checked((T)t); }
		public new T From(int t)    { return checked((T)t); }
		public new T From(ulong t)  { return checked((T)t); }
		public new T From(long t)   { return checked((T)t); }
		public new T From(double t) { return checked((T)t); }

		public new T Incremented(T a) { return checked((T)(a + 1)); }
		public new T Decremented(T a) { return checked((T)(a - 1)); }
		public new T NextHigher(T a)  { return checked((T)(a + 1)); }
		public new T NextLower(T a)   { return checked((T)(a - 1)); }

		public new T Add(T a, T b)      { return checked((T)(a + b)); }
		public new T Subtract(T a, T b) { return checked((T)(a - b)); }
		public new T Multiply(T a, T b) { return checked((T)(a * b)); }
		public new T Divide(T a, T b)   { return checked((T)(a / b)); }

		public new T ShiftLeft(T a, int amount)  { return checked((T)(a << amount)); }
		public new T ShiftRight(T a, int amount) { return checked((T)(a >> amount)); }

		public new T Square(T a) { return checked((T)(a * a)); }

		#endregion
	}*/
}

namespace Loyc.Math
{
	using System;
	using System.Collections.Generic;
	using T = System.Int16;

	public class MathI16 : Comparer<T>, IIntMath<short>
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
		public T Zero       { get { return (T)0; } }
		public T One        { get { return (T)1; } }

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

		public bool IsLess(T a, T b)          { return a < b; }
		public bool IsLessOrEqual(T a, T b)   { return a <= b; }
		public T Abs(T a)                     { return (T)(a >= Zero ? a : -a); }
		public T Max(T a, T b)                { return a > b ? a : b; }
		public T Min(T a, T b)                { return a < b ? a : b; }
		public override int Compare(T x, T y) { return x.CompareTo(y); }
		public bool Equals(T x, T y)          { return x == y; }
		public int GetHashCode(T x)           { return x.GetHashCode(); }

		public T Incremented(T a) { return (T)(a + 1); }
		public T Decremented(T a) { return (T)(a - 1); }
		public T NextHigher(T a)  { return (T)(a + 1); }
		public T NextLower(T a)   { return (T)(a - 1); }

		public T Add(T a, T b)      { return (T)(a + b); }
		public T Subtract(T a, T b) { return (T)(a - b); }
		public T Multiply(T a, T b) { return (T)(a * b); }
		public T Divide(T a, T b)   { return (T)(a / b); }

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
	/*
    public class CheckedMathU8 : MathU8, IUIntMath<T>
	{
		#region IMath

		public new T From(uint t)   { return checked((T)t); }
		public new T From(int t)    { return checked((T)t); }
		public new T From(ulong t)  { return checked((T)t); }
		public new T From(long t)   { return checked((T)t); }
		public new T From(double t) { return checked((T)t); }

		public new T Incremented(T a) { return checked((T)(a + 1)); }
		public new T Decremented(T a) { return checked((T)(a - 1)); }
		public new T NextHigher(T a)  { return checked((T)(a + 1)); }
		public new T NextLower(T a)   { return checked((T)(a - 1)); }

		public new T Add(T a, T b)      { return checked((T)(a + b)); }
		public new T Subtract(T a, T b) { return checked((T)(a - b)); }
		public new T Multiply(T a, T b) { return checked((T)(a * b)); }
		public new T Divide(T a, T b)   { return checked((T)(a / b)); }

		public new T ShiftLeft(T a, int amount)  { return checked((T)(a << amount)); }
		public new T ShiftRight(T a, int amount) { return checked((T)(a >> amount)); }

		public new T Square(T a) { return checked((T)(a * a)); }

		#endregion
	}*/
}

namespace Loyc.Math
{
	using System;
	using System.Collections.Generic;
	using T = System.UInt16;

	public class MathU16 : Comparer<T>, IUIntMath<ushort>
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
		public T Zero       { get { return (T)0; } }
		public T One        { get { return (T)1; } }

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

		public bool IsLess(T a, T b)          { return a < b; }
		public bool IsLessOrEqual(T a, T b)   { return a <= b; }
		public T Abs(T a)                     { return a; }
		public T Max(T a, T b)                { return a > b ? a : b; }
		public T Min(T a, T b)                { return a < b ? a : b; }
		public override int Compare(T x, T y) { return x.CompareTo(y); }
		public bool Equals(T x, T y)          { return x == y; }
		public int GetHashCode(T x)           { return x.GetHashCode(); }

		public T Incremented(T a) { return (T)(a + 1); }
		public T Decremented(T a) { return (T)(a - 1); }
		public T NextHigher(T a)  { return (T)(a + 1); }
		public T NextLower(T a)   { return (T)(a - 1); }

		public T Add(T a, T b)      { return (T)(a + b); }
		public T Subtract(T a, T b) { return (T)(a - b); }
		public T Multiply(T a, T b) { return (T)(a * b); }
		public T Divide(T a, T b)   { return (T)(a / b); }

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
	/*
    public class CheckedMathU8 : MathU8, IUIntMath<T>
	{
		#region IMath

		public new T From(uint t)   { return checked((T)t); }
		public new T From(int t)    { return checked((T)t); }
		public new T From(ulong t)  { return checked((T)t); }
		public new T From(long t)   { return checked((T)t); }
		public new T From(double t) { return checked((T)t); }

		public new T Incremented(T a) { return checked((T)(a + 1)); }
		public new T Decremented(T a) { return checked((T)(a - 1)); }
		public new T NextHigher(T a)  { return checked((T)(a + 1)); }
		public new T NextLower(T a)   { return checked((T)(a - 1)); }

		public new T Add(T a, T b)      { return checked((T)(a + b)); }
		public new T Subtract(T a, T b) { return checked((T)(a - b)); }
		public new T Multiply(T a, T b) { return checked((T)(a * b)); }
		public new T Divide(T a, T b)   { return checked((T)(a / b)); }

		public new T ShiftLeft(T a, int amount)  { return checked((T)(a << amount)); }
		public new T ShiftRight(T a, int amount) { return checked((T)(a >> amount)); }

		public new T Square(T a) { return checked((T)(a * a)); }

		#endregion
	}*/
}

namespace Loyc.Math
{
	using System;
	using System.Collections.Generic;
	using T = System.Int32;

	public class MathI : Comparer<T>, IIntMath<int>
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
		public T Zero       { get { return (T)0; } }
		public T One        { get { return (T)1; } }

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

		public bool IsLess(T a, T b)          { return a < b; }
		public bool IsLessOrEqual(T a, T b)   { return a <= b; }
		public T Abs(T a)                     { return (T)(a >= Zero ? a : -a); }
		public T Max(T a, T b)                { return a > b ? a : b; }
		public T Min(T a, T b)                { return a < b ? a : b; }
		public override int Compare(T x, T y) { return x.CompareTo(y); }
		public bool Equals(T x, T y)          { return x == y; }
		public int GetHashCode(T x)           { return x.GetHashCode(); }

		public T Incremented(T a) { return (T)(a + 1); }
		public T Decremented(T a) { return (T)(a - 1); }
		public T NextHigher(T a)  { return (T)(a + 1); }
		public T NextLower(T a)   { return (T)(a - 1); }

		public T Add(T a, T b)      { return (T)(a + b); }
		public T Subtract(T a, T b) { return (T)(a - b); }
		public T Multiply(T a, T b) { return (T)(a * b); }
		public T Divide(T a, T b)   { return (T)(a / b); }

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
	/*
    public class CheckedMathU8 : MathU8, IUIntMath<T>
	{
		#region IMath

		public new T From(uint t)   { return checked((T)t); }
		public new T From(int t)    { return checked((T)t); }
		public new T From(ulong t)  { return checked((T)t); }
		public new T From(long t)   { return checked((T)t); }
		public new T From(double t) { return checked((T)t); }

		public new T Incremented(T a) { return checked((T)(a + 1)); }
		public new T Decremented(T a) { return checked((T)(a - 1)); }
		public new T NextHigher(T a)  { return checked((T)(a + 1)); }
		public new T NextLower(T a)   { return checked((T)(a - 1)); }

		public new T Add(T a, T b)      { return checked((T)(a + b)); }
		public new T Subtract(T a, T b) { return checked((T)(a - b)); }
		public new T Multiply(T a, T b) { return checked((T)(a * b)); }
		public new T Divide(T a, T b)   { return checked((T)(a / b)); }

		public new T ShiftLeft(T a, int amount)  { return checked((T)(a << amount)); }
		public new T ShiftRight(T a, int amount) { return checked((T)(a >> amount)); }

		public new T Square(T a) { return checked((T)(a * a)); }

		#endregion
	}*/
}

namespace Loyc.Math
{
	using System;
	using System.Collections.Generic;
	using T = System.UInt32;

	public class MathU : Comparer<T>, IUIntMath<uint>
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
		public T Zero       { get { return (T)0; } }
		public T One        { get { return (T)1; } }

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

		public bool IsLess(T a, T b)          { return a < b; }
		public bool IsLessOrEqual(T a, T b)   { return a <= b; }
		public T Abs(T a)                     { return a; }
		public T Max(T a, T b)                { return a > b ? a : b; }
		public T Min(T a, T b)                { return a < b ? a : b; }
		public override int Compare(T x, T y) { return x.CompareTo(y); }
		public bool Equals(T x, T y)          { return x == y; }
		public int GetHashCode(T x)           { return x.GetHashCode(); }

		public T Incremented(T a) { return (T)(a + 1); }
		public T Decremented(T a) { return (T)(a - 1); }
		public T NextHigher(T a)  { return (T)(a + 1); }
		public T NextLower(T a)   { return (T)(a - 1); }

		public T Add(T a, T b)      { return (T)(a + b); }
		public T Subtract(T a, T b) { return (T)(a - b); }
		public T Multiply(T a, T b) { return (T)(a * b); }
		public T Divide(T a, T b)   { return (T)(a / b); }

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
	/*
    public class CheckedMathU8 : MathU8, IUIntMath<T>
	{
		#region IMath

		public new T From(uint t)   { return checked((T)t); }
		public new T From(int t)    { return checked((T)t); }
		public new T From(ulong t)  { return checked((T)t); }
		public new T From(long t)   { return checked((T)t); }
		public new T From(double t) { return checked((T)t); }

		public new T Incremented(T a) { return checked((T)(a + 1)); }
		public new T Decremented(T a) { return checked((T)(a - 1)); }
		public new T NextHigher(T a)  { return checked((T)(a + 1)); }
		public new T NextLower(T a)   { return checked((T)(a - 1)); }

		public new T Add(T a, T b)      { return checked((T)(a + b)); }
		public new T Subtract(T a, T b) { return checked((T)(a - b)); }
		public new T Multiply(T a, T b) { return checked((T)(a * b)); }
		public new T Divide(T a, T b)   { return checked((T)(a / b)); }

		public new T ShiftLeft(T a, int amount)  { return checked((T)(a << amount)); }
		public new T ShiftRight(T a, int amount) { return checked((T)(a >> amount)); }

		public new T Square(T a) { return checked((T)(a * a)); }

		#endregion
	}*/
}

namespace Loyc.Math
{
	using System;
	using System.Collections.Generic;
	using T = System.Int64;

	public class MathL : Comparer<T>, IIntMath<long>
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
		public T Zero       { get { return (T)0; } }
		public T One        { get { return (T)1; } }

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

		public bool IsLess(T a, T b)          { return a < b; }
		public bool IsLessOrEqual(T a, T b)   { return a <= b; }
		public T Abs(T a)                     { return (T)(a >= Zero ? a : -a); }
		public T Max(T a, T b)                { return a > b ? a : b; }
		public T Min(T a, T b)                { return a < b ? a : b; }
		public override int Compare(T x, T y) { return x.CompareTo(y); }
		public bool Equals(T x, T y)          { return x == y; }
		public int GetHashCode(T x)           { return x.GetHashCode(); }

		public T Incremented(T a) { return (T)(a + 1); }
		public T Decremented(T a) { return (T)(a - 1); }
		public T NextHigher(T a)  { return (T)(a + 1); }
		public T NextLower(T a)   { return (T)(a - 1); }

		public T Add(T a, T b)      { return (T)(a + b); }
		public T Subtract(T a, T b) { return (T)(a - b); }
		public T Multiply(T a, T b) { return (T)(a * b); }
		public T Divide(T a, T b)   { return (T)(a / b); }

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
	/*
    public class CheckedMathU8 : MathU8, IUIntMath<T>
	{
		#region IMath

		public new T From(uint t)   { return checked((T)t); }
		public new T From(int t)    { return checked((T)t); }
		public new T From(ulong t)  { return checked((T)t); }
		public new T From(long t)   { return checked((T)t); }
		public new T From(double t) { return checked((T)t); }

		public new T Incremented(T a) { return checked((T)(a + 1)); }
		public new T Decremented(T a) { return checked((T)(a - 1)); }
		public new T NextHigher(T a)  { return checked((T)(a + 1)); }
		public new T NextLower(T a)   { return checked((T)(a - 1)); }

		public new T Add(T a, T b)      { return checked((T)(a + b)); }
		public new T Subtract(T a, T b) { return checked((T)(a - b)); }
		public new T Multiply(T a, T b) { return checked((T)(a * b)); }
		public new T Divide(T a, T b)   { return checked((T)(a / b)); }

		public new T ShiftLeft(T a, int amount)  { return checked((T)(a << amount)); }
		public new T ShiftRight(T a, int amount) { return checked((T)(a >> amount)); }

		public new T Square(T a) { return checked((T)(a * a)); }

		#endregion
	}*/
}

namespace Loyc.Math
{
	using System;
	using System.Collections.Generic;
	using T = System.UInt64;

	public class MathUL : Comparer<T>, IUIntMath<ulong>
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
		public T Zero       { get { return (T)0; } }
		public T One        { get { return (T)1; } }

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

		public bool IsLess(T a, T b)          { return a < b; }
		public bool IsLessOrEqual(T a, T b)   { return a <= b; }
		public T Abs(T a)                     { return a; }
		public T Max(T a, T b)                { return a > b ? a : b; }
		public T Min(T a, T b)                { return a < b ? a : b; }
		public override int Compare(T x, T y) { return x.CompareTo(y); }
		public bool Equals(T x, T y)          { return x == y; }
		public int GetHashCode(T x)           { return x.GetHashCode(); }

		public T Incremented(T a) { return (T)(a + 1); }
		public T Decremented(T a) { return (T)(a - 1); }
		public T NextHigher(T a)  { return (T)(a + 1); }
		public T NextLower(T a)   { return (T)(a - 1); }

		public T Add(T a, T b)      { return (T)(a + b); }
		public T Subtract(T a, T b) { return (T)(a - b); }
		public T Multiply(T a, T b) { return (T)(a * b); }
		public T Divide(T a, T b)   { return (T)(a / b); }

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
	/*
    public class CheckedMathU8 : MathU8, IUIntMath<T>
	{
		#region IMath

		public new T From(uint t)   { return checked((T)t); }
		public new T From(int t)    { return checked((T)t); }
		public new T From(ulong t)  { return checked((T)t); }
		public new T From(long t)   { return checked((T)t); }
		public new T From(double t) { return checked((T)t); }

		public new T Incremented(T a) { return checked((T)(a + 1)); }
		public new T Decremented(T a) { return checked((T)(a - 1)); }
		public new T NextHigher(T a)  { return checked((T)(a + 1)); }
		public new T NextLower(T a)   { return checked((T)(a - 1)); }

		public new T Add(T a, T b)      { return checked((T)(a + b)); }
		public new T Subtract(T a, T b) { return checked((T)(a - b)); }
		public new T Multiply(T a, T b) { return checked((T)(a * b)); }
		public new T Divide(T a, T b)   { return checked((T)(a / b)); }

		public new T ShiftLeft(T a, int amount)  { return checked((T)(a << amount)); }
		public new T ShiftRight(T a, int amount) { return checked((T)(a >> amount)); }

		public new T Square(T a) { return checked((T)(a * a)); }

		#endregion
	}*/
}

namespace Loyc.Math
{
	using System;
	using System.Collections.Generic;
	using T = System.Single;

	public class MathF : Comparer<T>, IFloatMath<float>
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
		public T Zero       { get { return (T)0; } }
		public T One        { get { return (T)1; } }

		#endregion

		#region ISignedMath

		public T From(uint t)   { return (T)t; }
		public T From(int t)    { return (T)t; }
		public T From(ulong t)  { return (T)t; }
		public T From(long t)   { return (T)t; }
		public T From(double t) { return (T)t; }
		
		public T Clip(uint t)   { return(T)t; }
		public T Clip(ulong t)  { return(T)t; }
		public T Clip(int t)    { return		                   (T)t; }
		public T Clip(long t)   { return		                   (T)t; }
		public T Clip(double t) { return (T)MathEx.InRange(t, (double)0, (double)T.MaxValue); }

		public bool IsLess(T a, T b)          { return a < b; }
		public bool IsLessOrEqual(T a, T b)   { return a <= b; }
		public T Abs(T a)                     { return (T)(a >= Zero ? a : -a); }
		public T Max(T a, T b)                { return a > b ? a : b; }
		public T Min(T a, T b)                { return a < b ? a : b; }
		public override int Compare(T x, T y) { return x.CompareTo(y); }
		public bool Equals(T x, T y)          { return x == y; }
		public int GetHashCode(T x)           { return x.GetHashCode(); }

		public T Incremented(T a) { return (T)(a + 1); }
		public T Decremented(T a) { return (T)(a - 1); }
		public T NextHigher(T a)  { return (T)(a + 1); }
		public T NextLower(T a)   { return (T)(a - 1); }

		public T Add(T a, T b)      { return (T)(a + b); }
		public T Subtract(T a, T b) { return (T)(a - b); }
		public T Multiply(T a, T b) { return (T)(a * b); }
		public T Divide(T a, T b)   { return (T)(a / b); }

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
	/*
    public class CheckedMathU8 : MathU8, IUIntMath<T>
	{
		#region IMath

		public new T From(uint t)   { return checked((T)t); }
		public new T From(int t)    { return checked((T)t); }
		public new T From(ulong t)  { return checked((T)t); }
		public new T From(long t)   { return checked((T)t); }
		public new T From(double t) { return checked((T)t); }

		public new T Incremented(T a) { return checked((T)(a + 1)); }
		public new T Decremented(T a) { return checked((T)(a - 1)); }
		public new T NextHigher(T a)  { return checked((T)(a + 1)); }
		public new T NextLower(T a)   { return checked((T)(a - 1)); }

		public new T Add(T a, T b)      { return checked((T)(a + b)); }
		public new T Subtract(T a, T b) { return checked((T)(a - b)); }
		public new T Multiply(T a, T b) { return checked((T)(a * b)); }
		public new T Divide(T a, T b)   { return checked((T)(a / b)); }

		public new T ShiftLeft(T a, int amount)  { return checked((T)(a << amount)); }
		public new T ShiftRight(T a, int amount) { return checked((T)(a >> amount)); }

		public new T Square(T a) { return checked((T)(a * a)); }

		#endregion
	}*/
}

namespace Loyc.Math
{
	using System;
	using System.Collections.Generic;
	using T = System.Double;

	public class MathD : Comparer<T>, IFloatMath<double>
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
		public T Zero       { get { return (T)0; } }
		public T One        { get { return (T)1; } }

		#endregion

		#region ISignedMath

		public T From(uint t)   { return (T)t; }
		public T From(int t)    { return (T)t; }
		public T From(ulong t)  { return (T)t; }
		public T From(long t)   { return (T)t; }
		public T From(double t) { return (T)t; }
		
		public T Clip(uint t)   { return(T)t; }
		public T Clip(ulong t)  { return(T)t; }
		public T Clip(int t)    { return		                   (T)t; }
		public T Clip(long t)   { return		                   (T)t; }
		public T Clip(double t) { return (T)MathEx.InRange(t, (double)0, (double)T.MaxValue); }

		public bool IsLess(T a, T b)          { return a < b; }
		public bool IsLessOrEqual(T a, T b)   { return a <= b; }
		public T Abs(T a)                     { return (T)(a >= Zero ? a : -a); }
		public T Max(T a, T b)                { return a > b ? a : b; }
		public T Min(T a, T b)                { return a < b ? a : b; }
		public override int Compare(T x, T y) { return x.CompareTo(y); }
		public bool Equals(T x, T y)          { return x == y; }
		public int GetHashCode(T x)           { return x.GetHashCode(); }

		public T Incremented(T a) { return (T)(a + 1); }
		public T Decremented(T a) { return (T)(a - 1); }
		public T NextHigher(T a)  { return (T)(a + 1); }
		public T NextLower(T a)   { return (T)(a - 1); }

		public T Add(T a, T b)      { return (T)(a + b); }
		public T Subtract(T a, T b) { return (T)(a - b); }
		public T Multiply(T a, T b) { return (T)(a * b); }
		public T Divide(T a, T b)   { return (T)(a / b); }

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
	/*
    public class CheckedMathU8 : MathU8, IUIntMath<T>
	{
		#region IMath

		public new T From(uint t)   { return checked((T)t); }
		public new T From(int t)    { return checked((T)t); }
		public new T From(ulong t)  { return checked((T)t); }
		public new T From(long t)   { return checked((T)t); }
		public new T From(double t) { return checked((T)t); }

		public new T Incremented(T a) { return checked((T)(a + 1)); }
		public new T Decremented(T a) { return checked((T)(a - 1)); }
		public new T NextHigher(T a)  { return checked((T)(a + 1)); }
		public new T NextLower(T a)   { return checked((T)(a - 1)); }

		public new T Add(T a, T b)      { return checked((T)(a + b)); }
		public new T Subtract(T a, T b) { return checked((T)(a - b)); }
		public new T Multiply(T a, T b) { return checked((T)(a * b)); }
		public new T Divide(T a, T b)   { return checked((T)(a / b)); }

		public new T ShiftLeft(T a, int amount)  { return checked((T)(a << amount)); }
		public new T ShiftRight(T a, int amount) { return checked((T)(a >> amount)); }

		public new T Square(T a) { return checked((T)(a * a)); }

		#endregion
	}*/
}

namespace Loyc.Math
{
	using System;
	using System.Collections.Generic;
	using T = FPI8;

	public class MathF8 : Comparer<T>, IRationalMath<T>, IBinaryMath<T>
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
		public T Zero       { get { return T.Zero; } }
		public T One        { get { return T.One; } }

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

		public bool IsLess(T a, T b)          { return a < b; }
		public bool IsLessOrEqual(T a, T b)   { return a <= b; }
		public T Abs(T a)                     { return (T)(a >= Zero ? a : -a); }
		public T Max(T a, T b)                { return a > b ? a : b; }
		public T Min(T a, T b)                { return a < b ? a : b; }
		public override int Compare(T x, T y) { return x.CompareTo(y); }
		public bool Equals(T x, T y)          { return x == y; }
		public int GetHashCode(T x)           { return x.GetHashCode(); }

		public T Incremented(T a) { return (T)(a + 1); }
		public T Decremented(T a) { return (T)(a - 1); }
		public T NextHigher(T a)  { return (T)(a + 1); }
		public T NextLower(T a)   { return (T)(a - 1); }

		public T Add(T a, T b)      { return (T)(a + b); }
		public T Subtract(T a, T b) { return (T)(a - b); }
		public T Multiply(T a, T b) { return (T)(a * b); }
		public T Divide(T a, T b)   { return (T)(a / b); }

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
	/*
    public class CheckedMathU8 : MathU8, IUIntMath<T>
	{
		#region IMath

		public new T From(uint t)   { return checked((T)t); }
		public new T From(int t)    { return checked((T)t); }
		public new T From(ulong t)  { return checked((T)t); }
		public new T From(long t)   { return checked((T)t); }
		public new T From(double t) { return checked((T)t); }

		public new T Incremented(T a) { return checked((T)(a + 1)); }
		public new T Decremented(T a) { return checked((T)(a - 1)); }
		public new T NextHigher(T a)  { return checked((T)(a + 1)); }
		public new T NextLower(T a)   { return checked((T)(a - 1)); }

		public new T Add(T a, T b)      { return checked((T)(a + b)); }
		public new T Subtract(T a, T b) { return checked((T)(a - b)); }
		public new T Multiply(T a, T b) { return checked((T)(a * b)); }
		public new T Divide(T a, T b)   { return checked((T)(a / b)); }

		public new T ShiftLeft(T a, int amount)  { return checked((T)(a << amount)); }
		public new T ShiftRight(T a, int amount) { return checked((T)(a >> amount)); }

		public new T Square(T a) { return checked((T)(a * a)); }

		#endregion
	}*/
}

namespace Loyc.Math
{
	using System;
	using System.Collections.Generic;
	using T = FPI16;

	public class MathF16 : Comparer<T>, IRationalMath<T>, IBinaryMath<T>
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
		public T Zero       { get { return T.Zero; } }
		public T One        { get { return T.One; } }

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

		public bool IsLess(T a, T b)          { return a < b; }
		public bool IsLessOrEqual(T a, T b)   { return a <= b; }
		public T Abs(T a)                     { return (T)(a >= Zero ? a : -a); }
		public T Max(T a, T b)                { return a > b ? a : b; }
		public T Min(T a, T b)                { return a < b ? a : b; }
		public override int Compare(T x, T y) { return x.CompareTo(y); }
		public bool Equals(T x, T y)          { return x == y; }
		public int GetHashCode(T x)           { return x.GetHashCode(); }

		public T Incremented(T a) { return (T)(a + 1); }
		public T Decremented(T a) { return (T)(a - 1); }
		public T NextHigher(T a)  { return (T)(a + 1); }
		public T NextLower(T a)   { return (T)(a - 1); }

		public T Add(T a, T b)      { return (T)(a + b); }
		public T Subtract(T a, T b) { return (T)(a - b); }
		public T Multiply(T a, T b) { return (T)(a * b); }
		public T Divide(T a, T b)   { return (T)(a / b); }

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
	/*
    public class CheckedMathU8 : MathU8, IUIntMath<T>
	{
		#region IMath

		public new T From(uint t)   { return checked((T)t); }
		public new T From(int t)    { return checked((T)t); }
		public new T From(ulong t)  { return checked((T)t); }
		public new T From(long t)   { return checked((T)t); }
		public new T From(double t) { return checked((T)t); }

		public new T Incremented(T a) { return checked((T)(a + 1)); }
		public new T Decremented(T a) { return checked((T)(a - 1)); }
		public new T NextHigher(T a)  { return checked((T)(a + 1)); }
		public new T NextLower(T a)   { return checked((T)(a - 1)); }

		public new T Add(T a, T b)      { return checked((T)(a + b)); }
		public new T Subtract(T a, T b) { return checked((T)(a - b)); }
		public new T Multiply(T a, T b) { return checked((T)(a * b)); }
		public new T Divide(T a, T b)   { return checked((T)(a / b)); }

		public new T ShiftLeft(T a, int amount)  { return checked((T)(a << amount)); }
		public new T ShiftRight(T a, int amount) { return checked((T)(a >> amount)); }

		public new T Square(T a) { return checked((T)(a * a)); }

		#endregion
	}*/
}

namespace Loyc.Math
{
	using System;
	using System.Collections.Generic;
	using T = FPI23;

	public class MathF23 : Comparer<T>, IRationalMath<T>, IBinaryMath<T>
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
		public T Zero       { get { return T.Zero; } }
		public T One        { get { return T.One; } }

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

		public bool IsLess(T a, T b)          { return a < b; }
		public bool IsLessOrEqual(T a, T b)   { return a <= b; }
		public T Abs(T a)                     { return (T)(a >= Zero ? a : -a); }
		public T Max(T a, T b)                { return a > b ? a : b; }
		public T Min(T a, T b)                { return a < b ? a : b; }
		public override int Compare(T x, T y) { return x.CompareTo(y); }
		public bool Equals(T x, T y)          { return x == y; }
		public int GetHashCode(T x)           { return x.GetHashCode(); }

		public T Incremented(T a) { return (T)(a + 1); }
		public T Decremented(T a) { return (T)(a - 1); }
		public T NextHigher(T a)  { return (T)(a + 1); }
		public T NextLower(T a)   { return (T)(a - 1); }

		public T Add(T a, T b)      { return (T)(a + b); }
		public T Subtract(T a, T b) { return (T)(a - b); }
		public T Multiply(T a, T b) { return (T)(a * b); }
		public T Divide(T a, T b)   { return (T)(a / b); }

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
	/*
    public class CheckedMathU8 : MathU8, IUIntMath<T>
	{
		#region IMath

		public new T From(uint t)   { return checked((T)t); }
		public new T From(int t)    { return checked((T)t); }
		public new T From(ulong t)  { return checked((T)t); }
		public new T From(long t)   { return checked((T)t); }
		public new T From(double t) { return checked((T)t); }

		public new T Incremented(T a) { return checked((T)(a + 1)); }
		public new T Decremented(T a) { return checked((T)(a - 1)); }
		public new T NextHigher(T a)  { return checked((T)(a + 1)); }
		public new T NextLower(T a)   { return checked((T)(a - 1)); }

		public new T Add(T a, T b)      { return checked((T)(a + b)); }
		public new T Subtract(T a, T b) { return checked((T)(a - b)); }
		public new T Multiply(T a, T b) { return checked((T)(a * b)); }
		public new T Divide(T a, T b)   { return checked((T)(a / b)); }

		public new T ShiftLeft(T a, int amount)  { return checked((T)(a << amount)); }
		public new T ShiftRight(T a, int amount) { return checked((T)(a >> amount)); }

		public new T Square(T a) { return checked((T)(a * a)); }

		#endregion
	}*/
}

namespace Loyc.Math
{
	using System;
	using System.Collections.Generic;
	using T = FPL16;

	public class MathFL16 : Comparer<T>, IRationalMath<T>, IBinaryMath<T>
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
		public T Zero       { get { return T.Zero; } }
		public T One        { get { return T.One; } }

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

		public bool IsLess(T a, T b)          { return a < b; }
		public bool IsLessOrEqual(T a, T b)   { return a <= b; }
		public T Abs(T a)                     { return (T)(a >= Zero ? a : -a); }
		public T Max(T a, T b)                { return a > b ? a : b; }
		public T Min(T a, T b)                { return a < b ? a : b; }
		public override int Compare(T x, T y) { return x.CompareTo(y); }
		public bool Equals(T x, T y)          { return x == y; }
		public int GetHashCode(T x)           { return x.GetHashCode(); }

		public T Incremented(T a) { return (T)(a + 1); }
		public T Decremented(T a) { return (T)(a - 1); }
		public T NextHigher(T a)  { return (T)(a + 1); }
		public T NextLower(T a)   { return (T)(a - 1); }

		public T Add(T a, T b)      { return (T)(a + b); }
		public T Subtract(T a, T b) { return (T)(a - b); }
		public T Multiply(T a, T b) { return (T)(a * b); }
		public T Divide(T a, T b)   { return (T)(a / b); }

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
	/*
    public class CheckedMathU8 : MathU8, IUIntMath<T>
	{
		#region IMath

		public new T From(uint t)   { return checked((T)t); }
		public new T From(int t)    { return checked((T)t); }
		public new T From(ulong t)  { return checked((T)t); }
		public new T From(long t)   { return checked((T)t); }
		public new T From(double t) { return checked((T)t); }

		public new T Incremented(T a) { return checked((T)(a + 1)); }
		public new T Decremented(T a) { return checked((T)(a - 1)); }
		public new T NextHigher(T a)  { return checked((T)(a + 1)); }
		public new T NextLower(T a)   { return checked((T)(a - 1)); }

		public new T Add(T a, T b)      { return checked((T)(a + b)); }
		public new T Subtract(T a, T b) { return checked((T)(a - b)); }
		public new T Multiply(T a, T b) { return checked((T)(a * b)); }
		public new T Divide(T a, T b)   { return checked((T)(a / b)); }

		public new T ShiftLeft(T a, int amount)  { return checked((T)(a << amount)); }
		public new T ShiftRight(T a, int amount) { return checked((T)(a >> amount)); }

		public new T Square(T a) { return checked((T)(a * a)); }

		#endregion
	}*/
}

namespace Loyc.Math
{
	using System;
	using System.Collections.Generic;
	using T = FPL32;

	public class MathFL32 : Comparer<T>, IRationalMath<T>, IBinaryMath<T>
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
		public T Zero       { get { return T.Zero; } }
		public T One        { get { return T.One; } }

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

		public bool IsLess(T a, T b)          { return a < b; }
		public bool IsLessOrEqual(T a, T b)   { return a <= b; }
		public T Abs(T a)                     { return (T)(a >= Zero ? a : -a); }
		public T Max(T a, T b)                { return a > b ? a : b; }
		public T Min(T a, T b)                { return a < b ? a : b; }
		public override int Compare(T x, T y) { return x.CompareTo(y); }
		public bool Equals(T x, T y)          { return x == y; }
		public int GetHashCode(T x)           { return x.GetHashCode(); }

		public T Incremented(T a) { return (T)(a + 1); }
		public T Decremented(T a) { return (T)(a - 1); }
		public T NextHigher(T a)  { return (T)(a + 1); }
		public T NextLower(T a)   { return (T)(a - 1); }

		public T Add(T a, T b)      { return (T)(a + b); }
		public T Subtract(T a, T b) { return (T)(a - b); }
		public T Multiply(T a, T b) { return (T)(a * b); }
		public T Divide(T a, T b)   { return (T)(a / b); }

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
	/*
    public class CheckedMathU8 : MathU8, IUIntMath<T>
	{
		#region IMath

		public new T From(uint t)   { return checked((T)t); }
		public new T From(int t)    { return checked((T)t); }
		public new T From(ulong t)  { return checked((T)t); }
		public new T From(long t)   { return checked((T)t); }
		public new T From(double t) { return checked((T)t); }

		public new T Incremented(T a) { return checked((T)(a + 1)); }
		public new T Decremented(T a) { return checked((T)(a - 1)); }
		public new T NextHigher(T a)  { return checked((T)(a + 1)); }
		public new T NextLower(T a)   { return checked((T)(a - 1)); }

		public new T Add(T a, T b)      { return checked((T)(a + b)); }
		public new T Subtract(T a, T b) { return checked((T)(a - b)); }
		public new T Multiply(T a, T b) { return checked((T)(a * b)); }
		public new T Divide(T a, T b)   { return checked((T)(a / b)); }

		public new T ShiftLeft(T a, int amount)  { return checked((T)(a << amount)); }
		public new T ShiftRight(T a, int amount) { return checked((T)(a >> amount)); }

		public new T Square(T a) { return checked((T)(a * a)); }

		#endregion
	}*/
}

