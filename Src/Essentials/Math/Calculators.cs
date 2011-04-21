

namespace Loyc.Math
{
	using System;
	using T = System.SByte;

	public class MathI8 : IIntMath<sbyte>
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
		public ulong MaxInt { get { return (ulong)System.SByte.MaxValue; } }
		public long MinInt  { get { return System.SByte.MinValue; } }
		public T Zero       { get { return (T)0; } }
		public T One        { get { return (T)1; } }

		#endregion

		#region IMath

		public T From(uint t)   { return (T)t; }
		public T From(int t)    { return (T)t; }
		public T From(ulong t)  { return (T)t; }
		public T From(long t)   { return (T)t; }
		public T From(double t) { return (T)t; }

		public T Clip(uint t)   { return (T)(t <= T.MaxValue ? t : T.MaxValue); }
		public T Clip(ulong t)  { return (T)(t <= T.MaxValue ? t : T.MaxValue); }
		public T Clip(int t)    { return (T)this.InRange(t, (int)0, (int)T.MaxValue); }
		public T Clip(long t)   { return (T)this.InRange(t, (long)0, (long)T.MaxValue); }
		public T Clip(double t) { return (T)this.InRange(t, (double)0, (double)T.MaxValue); }

		public bool IsLess(T a, T b)        { return a < b; }
		public bool IsLessOrEqual(T a, T b) { return a <= b; }
		public T Abs(T a)                   { return a; }
		public T Max(T a, T b)           { return a > b ? a : b; }
		public T Min(T a, T b)           { return a < b ? a : b; }
		public int Compare(T x, T y)        { return x.CompareTo(y); }
		public bool Equals(T x, T y)        { return x == y; }

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
		public T Not(T a)         { return (T)~a; }

		public int CountOnes(T a)     { return MathEx.CountOnes(a); }
		public int Log2Floor(T a)     { return MathEx.Log2Floor(a); }

		public int FindFirstOne(T a)  { return MathEx.FindFirstOne(a); }
		public int FindFirstZero(T a) { return MathEx.FindFirstZero(a); }
		public int FindLastOne(T a)   { return MathEx.FindLastOne(a); }
		public int FindLastZero(T a)  { return MathEx.FindLastZero(a); }
		
		#endregion
	}
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
	}
}

namespace Loyc.Math
{
	using System;
	using T = System.Byte;

	public class MathU8 : IUIntMath<byte>
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
		public ulong MaxInt { get { return (ulong)System.Byte.MaxValue; } }
		public long MinInt  { get { return System.Byte.MinValue; } }
		public T Zero       { get { return (T)0; } }
		public T One        { get { return (T)1; } }

		#endregion

		#region IMath

		public T From(uint t)   { return (T)t; }
		public T From(int t)    { return (T)t; }
		public T From(ulong t)  { return (T)t; }
		public T From(long t)   { return (T)t; }
		public T From(double t) { return (T)t; }

		public T Clip(uint t)   { return (T)(t <= T.MaxValue ? t : T.MaxValue); }
		public T Clip(ulong t)  { return (T)(t <= T.MaxValue ? t : T.MaxValue); }
		public T Clip(int t)    { return (T)this.InRange(t, (int)0, (int)T.MaxValue); }
		public T Clip(long t)   { return (T)this.InRange(t, (long)0, (long)T.MaxValue); }
		public T Clip(double t) { return (T)this.InRange(t, (double)0, (double)T.MaxValue); }

		public bool IsLess(T a, T b)        { return a < b; }
		public bool IsLessOrEqual(T a, T b) { return a <= b; }
		public T Abs(T a)                   { return a; }
		public T Max(T a, T b)           { return a > b ? a : b; }
		public T Min(T a, T b)           { return a < b ? a : b; }
		public int Compare(T x, T y)        { return x.CompareTo(y); }
		public bool Equals(T x, T y)        { return x == y; }

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
		public T Not(T a)         { return (T)~a; }

		public int CountOnes(T a)     { return MathEx.CountOnes(a); }
		public int Log2Floor(T a)     { return MathEx.Log2Floor(a); }

		public int FindFirstOne(T a)  { return MathEx.FindFirstOne(a); }
		public int FindFirstZero(T a) { return MathEx.FindFirstZero(a); }
		public int FindLastOne(T a)   { return MathEx.FindLastOne(a); }
		public int FindLastZero(T a)  { return MathEx.FindLastZero(a); }
		
		#endregion
	}
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
	}
}

namespace Loyc.Math
{
	using System;
	using T = System.Int16;

	public class MathI16 : IIntMath<short>
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
		public ulong MaxInt { get { return (ulong)System.Int16.MaxValue; } }
		public long MinInt  { get { return System.Int16.MinValue; } }
		public T Zero       { get { return (T)0; } }
		public T One        { get { return (T)1; } }

		#endregion

		#region IMath

		public T From(uint t)   { return (T)t; }
		public T From(int t)    { return (T)t; }
		public T From(ulong t)  { return (T)t; }
		public T From(long t)   { return (T)t; }
		public T From(double t) { return (T)t; }

		public T Clip(uint t)   { return (T)(t <= T.MaxValue ? t : T.MaxValue); }
		public T Clip(ulong t)  { return (T)(t <= T.MaxValue ? t : T.MaxValue); }
		public T Clip(int t)    { return (T)this.InRange(t, (int)0, (int)T.MaxValue); }
		public T Clip(long t)   { return (T)this.InRange(t, (long)0, (long)T.MaxValue); }
		public T Clip(double t) { return (T)this.InRange(t, (double)0, (double)T.MaxValue); }

		public bool IsLess(T a, T b)        { return a < b; }
		public bool IsLessOrEqual(T a, T b) { return a <= b; }
		public T Abs(T a)                   { return a; }
		public T Max(T a, T b)           { return a > b ? a : b; }
		public T Min(T a, T b)           { return a < b ? a : b; }
		public int Compare(T x, T y)        { return x.CompareTo(y); }
		public bool Equals(T x, T y)        { return x == y; }

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
		public T Not(T a)         { return (T)~a; }

		public int CountOnes(T a)     { return MathEx.CountOnes(a); }
		public int Log2Floor(T a)     { return MathEx.Log2Floor(a); }

		public int FindFirstOne(T a)  { return MathEx.FindFirstOne(a); }
		public int FindFirstZero(T a) { return MathEx.FindFirstZero(a); }
		public int FindLastOne(T a)   { return MathEx.FindLastOne(a); }
		public int FindLastZero(T a)  { return MathEx.FindLastZero(a); }
		
		#endregion
	}
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
	}
}

namespace Loyc.Math
{
	using System;
	using T = System.UInt16;

	public class MathU16 : IUIntMath<ushort>
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
		public ulong MaxInt { get { return (ulong)System.UInt16.MaxValue; } }
		public long MinInt  { get { return System.UInt16.MinValue; } }
		public T Zero       { get { return (T)0; } }
		public T One        { get { return (T)1; } }

		#endregion

		#region IMath

		public T From(uint t)   { return (T)t; }
		public T From(int t)    { return (T)t; }
		public T From(ulong t)  { return (T)t; }
		public T From(long t)   { return (T)t; }
		public T From(double t) { return (T)t; }

		public T Clip(uint t)   { return (T)(t <= T.MaxValue ? t : T.MaxValue); }
		public T Clip(ulong t)  { return (T)(t <= T.MaxValue ? t : T.MaxValue); }
		public T Clip(int t)    { return (T)this.InRange(t, (int)0, (int)T.MaxValue); }
		public T Clip(long t)   { return (T)this.InRange(t, (long)0, (long)T.MaxValue); }
		public T Clip(double t) { return (T)this.InRange(t, (double)0, (double)T.MaxValue); }

		public bool IsLess(T a, T b)        { return a < b; }
		public bool IsLessOrEqual(T a, T b) { return a <= b; }
		public T Abs(T a)                   { return a; }
		public T Max(T a, T b)           { return a > b ? a : b; }
		public T Min(T a, T b)           { return a < b ? a : b; }
		public int Compare(T x, T y)        { return x.CompareTo(y); }
		public bool Equals(T x, T y)        { return x == y; }

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
		public T Not(T a)         { return (T)~a; }

		public int CountOnes(T a)     { return MathEx.CountOnes(a); }
		public int Log2Floor(T a)     { return MathEx.Log2Floor(a); }

		public int FindFirstOne(T a)  { return MathEx.FindFirstOne(a); }
		public int FindFirstZero(T a) { return MathEx.FindFirstZero(a); }
		public int FindLastOne(T a)   { return MathEx.FindLastOne(a); }
		public int FindLastZero(T a)  { return MathEx.FindLastZero(a); }
		
		#endregion
	}
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
	}
}

namespace Loyc.Math
{
	using System;
	using T = System.Int32;

	public class MathI : IIntMath<int>
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
		public ulong MaxInt { get { return (ulong)System.Int32.MaxValue; } }
		public long MinInt  { get { return System.Int32.MinValue; } }
		public T Zero       { get { return (T)0; } }
		public T One        { get { return (T)1; } }

		#endregion

		#region IMath

		public T From(uint t)   { return (T)t; }
		public T From(int t)    { return (T)t; }
		public T From(ulong t)  { return (T)t; }
		public T From(long t)   { return (T)t; }
		public T From(double t) { return (T)t; }

		public T Clip(uint t)   { return (T)(t <= T.MaxValue ? t : T.MaxValue); }
		public T Clip(ulong t)  { return (T)(t <= T.MaxValue ? t : T.MaxValue); }
		public T Clip(int t)    { return (T)this.InRange(t, (int)0, (int)T.MaxValue); }
		public T Clip(long t)   { return (T)this.InRange(t, (long)0, (long)T.MaxValue); }
		public T Clip(double t) { return (T)this.InRange(t, (double)0, (double)T.MaxValue); }

		public bool IsLess(T a, T b)        { return a < b; }
		public bool IsLessOrEqual(T a, T b) { return a <= b; }
		public T Abs(T a)                   { return a; }
		public T Max(T a, T b)           { return a > b ? a : b; }
		public T Min(T a, T b)           { return a < b ? a : b; }
		public int Compare(T x, T y)        { return x.CompareTo(y); }
		public bool Equals(T x, T y)        { return x == y; }

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
		public T Not(T a)         { return (T)~a; }

		public int CountOnes(T a)     { return MathEx.CountOnes(a); }
		public int Log2Floor(T a)     { return MathEx.Log2Floor(a); }

		public int FindFirstOne(T a)  { return MathEx.FindFirstOne(a); }
		public int FindFirstZero(T a) { return MathEx.FindFirstZero(a); }
		public int FindLastOne(T a)   { return MathEx.FindLastOne(a); }
		public int FindLastZero(T a)  { return MathEx.FindLastZero(a); }
		
		#endregion
	}
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
	}
}

namespace Loyc.Math
{
	using System;
	using T = System.UInt32;

	public class MathU : IUIntMath<uint>
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
		public ulong MaxInt { get { return (ulong)System.UInt32.MaxValue; } }
		public long MinInt  { get { return System.UInt32.MinValue; } }
		public T Zero       { get { return (T)0; } }
		public T One        { get { return (T)1; } }

		#endregion

		#region IMath

		public T From(uint t)   { return (T)t; }
		public T From(int t)    { return (T)t; }
		public T From(ulong t)  { return (T)t; }
		public T From(long t)   { return (T)t; }
		public T From(double t) { return (T)t; }

		public T Clip(uint t)   { return (T)(t <= T.MaxValue ? t : T.MaxValue); }
		public T Clip(ulong t)  { return (T)(t <= T.MaxValue ? t : T.MaxValue); }
		public T Clip(int t)    { return (T)this.InRange(t, (int)0, (int)T.MaxValue); }
		public T Clip(long t)   { return (T)this.InRange(t, (long)0, (long)T.MaxValue); }
		public T Clip(double t) { return (T)this.InRange(t, (double)0, (double)T.MaxValue); }

		public bool IsLess(T a, T b)        { return a < b; }
		public bool IsLessOrEqual(T a, T b) { return a <= b; }
		public T Abs(T a)                   { return a; }
		public T Max(T a, T b)           { return a > b ? a : b; }
		public T Min(T a, T b)           { return a < b ? a : b; }
		public int Compare(T x, T y)        { return x.CompareTo(y); }
		public bool Equals(T x, T y)        { return x == y; }

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
		public T Not(T a)         { return (T)~a; }

		public int CountOnes(T a)     { return MathEx.CountOnes(a); }
		public int Log2Floor(T a)     { return MathEx.Log2Floor(a); }

		public int FindFirstOne(T a)  { return MathEx.FindFirstOne(a); }
		public int FindFirstZero(T a) { return MathEx.FindFirstZero(a); }
		public int FindLastOne(T a)   { return MathEx.FindLastOne(a); }
		public int FindLastZero(T a)  { return MathEx.FindLastZero(a); }
		
		#endregion
	}
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
	}
}

namespace Loyc.Math
{
	using System;
	using T = System.Int64;

	public class MathL : IIntMath<long>
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
		public ulong MaxInt { get { return (ulong)System.Int64.MaxValue; } }
		public long MinInt  { get { return System.Int64.MinValue; } }
		public T Zero       { get { return (T)0; } }
		public T One        { get { return (T)1; } }

		#endregion

		#region IMath

		public T From(uint t)   { return (T)t; }
		public T From(int t)    { return (T)t; }
		public T From(ulong t)  { return (T)t; }
		public T From(long t)   { return (T)t; }
		public T From(double t) { return (T)t; }

		public T Clip(uint t)   { return (T)(t <= T.MaxValue ? t : T.MaxValue); }
		public T Clip(ulong t)  { return (T)(t <= T.MaxValue ? t : T.MaxValue); }
		public T Clip(int t)    { return (T)this.InRange(t, (int)0, (int)T.MaxValue); }
		public T Clip(long t)   { return (T)this.InRange(t, (long)0, (long)T.MaxValue); }
		public T Clip(double t) { return (T)this.InRange(t, (double)0, (double)T.MaxValue); }

		public bool IsLess(T a, T b)        { return a < b; }
		public bool IsLessOrEqual(T a, T b) { return a <= b; }
		public T Abs(T a)                   { return a; }
		public T Max(T a, T b)           { return a > b ? a : b; }
		public T Min(T a, T b)           { return a < b ? a : b; }
		public int Compare(T x, T y)        { return x.CompareTo(y); }
		public bool Equals(T x, T y)        { return x == y; }

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
		public T Not(T a)         { return (T)~a; }

		public int CountOnes(T a)     { return MathEx.CountOnes(a); }
		public int Log2Floor(T a)     { return MathEx.Log2Floor(a); }

		public int FindFirstOne(T a)  { return MathEx.FindFirstOne(a); }
		public int FindFirstZero(T a) { return MathEx.FindFirstZero(a); }
		public int FindLastOne(T a)   { return MathEx.FindLastOne(a); }
		public int FindLastZero(T a)  { return MathEx.FindLastZero(a); }
		
		#endregion
	}
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
	}
}

namespace Loyc.Math
{
	using System;
	using T = System.UInt64;

	public class MathUL : IUIntMath<ulong>
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
		public ulong MaxInt { get { return (ulong)System.UInt64.MaxValue; } }
		public long MinInt  { get { return System.UInt64.MinValue; } }
		public T Zero       { get { return (T)0; } }
		public T One        { get { return (T)1; } }

		#endregion

		#region IMath

		public T From(uint t)   { return (T)t; }
		public T From(int t)    { return (T)t; }
		public T From(ulong t)  { return (T)t; }
		public T From(long t)   { return (T)t; }
		public T From(double t) { return (T)t; }

		public T Clip(uint t)   { return (T)(t <= T.MaxValue ? t : T.MaxValue); }
		public T Clip(ulong t)  { return (T)(t <= T.MaxValue ? t : T.MaxValue); }
		public T Clip(int t)    { return (T)this.InRange(t, (int)0, (int)T.MaxValue); }
		public T Clip(long t)   { return (T)this.InRange(t, (long)0, (long)T.MaxValue); }
		public T Clip(double t) { return (T)this.InRange(t, (double)0, (double)T.MaxValue); }

		public bool IsLess(T a, T b)        { return a < b; }
		public bool IsLessOrEqual(T a, T b) { return a <= b; }
		public T Abs(T a)                   { return a; }
		public T Max(T a, T b)           { return a > b ? a : b; }
		public T Min(T a, T b)           { return a < b ? a : b; }
		public int Compare(T x, T y)        { return x.CompareTo(y); }
		public bool Equals(T x, T y)        { return x == y; }

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
		public T Not(T a)         { return (T)~a; }

		public int CountOnes(T a)     { return MathEx.CountOnes(a); }
		public int Log2Floor(T a)     { return MathEx.Log2Floor(a); }

		public int FindFirstOne(T a)  { return MathEx.FindFirstOne(a); }
		public int FindFirstZero(T a) { return MathEx.FindFirstZero(a); }
		public int FindLastOne(T a)   { return MathEx.FindLastOne(a); }
		public int FindLastZero(T a)  { return MathEx.FindLastZero(a); }
		
		#endregion
	}
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
	}
}

namespace Loyc.Math
{
	using System;
	using T = System.Single;

	public class MathF : IFloatMath<float>
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
		public ulong MaxInt { get { return (ulong)System.Single.MaxValue; } }
		public long MinInt  { get { return System.Single.MinValue; } }
		public T Zero       { get { return (T)0; } }
		public T One        { get { return (T)1; } }

		#endregion

		#region IMath

		public T From(uint t)   { return (T)t; }
		public T From(int t)    { return (T)t; }
		public T From(ulong t)  { return (T)t; }
		public T From(long t)   { return (T)t; }
		public T From(double t) { return (T)t; }

		public T Clip(uint t)   { return (T)(t <= T.MaxValue ? t : T.MaxValue); }
		public T Clip(ulong t)  { return (T)(t <= T.MaxValue ? t : T.MaxValue); }
		public T Clip(int t)    { return (T)this.InRange(t, (int)0, (int)T.MaxValue); }
		public T Clip(long t)   { return (T)this.InRange(t, (long)0, (long)T.MaxValue); }
		public T Clip(double t) { return (T)this.InRange(t, (double)0, (double)T.MaxValue); }

		public bool IsLess(T a, T b)        { return a < b; }
		public bool IsLessOrEqual(T a, T b) { return a <= b; }
		public T Abs(T a)                   { return a; }
		public T Max(T a, T b)           { return a > b ? a : b; }
		public T Min(T a, T b)           { return a < b ? a : b; }
		public int Compare(T x, T y)        { return x.CompareTo(y); }
		public bool Equals(T x, T y)        { return x == y; }

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
		public T Not(T a)         { return (T)~a; }

		public int CountOnes(T a)     { return MathEx.CountOnes(a); }
		public int Log2Floor(T a)     { return MathEx.Log2Floor(a); }

		public int FindFirstOne(T a)  { return MathEx.FindFirstOne(a); }
		public int FindFirstZero(T a) { return MathEx.FindFirstZero(a); }
		public int FindLastOne(T a)   { return MathEx.FindLastOne(a); }
		public int FindLastZero(T a)  { return MathEx.FindLastZero(a); }
		
		#endregion
	}
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
	}
}

namespace Loyc.Math
{
	using System;
	using T = System.Double;

	public class MathD : IFloatMath<double>
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
		public ulong MaxInt { get { return (ulong)System.Double.MaxValue; } }
		public long MinInt  { get { return System.Double.MinValue; } }
		public T Zero       { get { return (T)0; } }
		public T One        { get { return (T)1; } }

		#endregion

		#region IMath

		public T From(uint t)   { return (T)t; }
		public T From(int t)    { return (T)t; }
		public T From(ulong t)  { return (T)t; }
		public T From(long t)   { return (T)t; }
		public T From(double t) { return (T)t; }

		public T Clip(uint t)   { return (T)(t <= T.MaxValue ? t : T.MaxValue); }
		public T Clip(ulong t)  { return (T)(t <= T.MaxValue ? t : T.MaxValue); }
		public T Clip(int t)    { return (T)this.InRange(t, (int)0, (int)T.MaxValue); }
		public T Clip(long t)   { return (T)this.InRange(t, (long)0, (long)T.MaxValue); }
		public T Clip(double t) { return (T)this.InRange(t, (double)0, (double)T.MaxValue); }

		public bool IsLess(T a, T b)        { return a < b; }
		public bool IsLessOrEqual(T a, T b) { return a <= b; }
		public T Abs(T a)                   { return a; }
		public T Max(T a, T b)           { return a > b ? a : b; }
		public T Min(T a, T b)           { return a < b ? a : b; }
		public int Compare(T x, T y)        { return x.CompareTo(y); }
		public bool Equals(T x, T y)        { return x == y; }

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
		public T Not(T a)         { return (T)~a; }

		public int CountOnes(T a)     { return MathEx.CountOnes(a); }
		public int Log2Floor(T a)     { return MathEx.Log2Floor(a); }

		public int FindFirstOne(T a)  { return MathEx.FindFirstOne(a); }
		public int FindFirstZero(T a) { return MathEx.FindFirstZero(a); }
		public int FindLastOne(T a)   { return MathEx.FindLastOne(a); }
		public int FindLastZero(T a)  { return MathEx.FindLastZero(a); }
		
		#endregion
	}
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
	}
}

