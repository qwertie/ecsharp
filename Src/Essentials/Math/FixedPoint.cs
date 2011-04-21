
namespace Loyc.Math
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
	using System.Diagnostics;

	
    public partial struct FPI8 : IComparable< FPI8 >, IEquatable< FPI8 >, IConvertible
    {
        public const int Frac = 8;
		public static FPI8 Prescaled(Int32 n) { FPI8 r = new FPI8(); r.N = n; return r; }
        public static readonly FPI8 Zero = new FPI8();
		public static readonly FPI8 One = new FPI8(1);
		public static readonly FPI8 Epsilon = Prescaled(1);
        public static readonly FPI8 MaxValue = Prescaled(Int32.MaxValue);
        public static readonly FPI8 MinValue = Prescaled(Int32.MinValue);
        public const Int32 MaxInt = Int32.MaxValue >> Frac;
        public const Int32 MinInt = Int32.MinValue >> Frac;
        public const double MaxDouble = Int32.MaxValue / (double)(1 << Frac);
        public const double MinDouble = Int32.MinValue / (double)(1 << Frac);
		public const Int32 Mask = (1 << Frac) - 1;

		public static explicit operator FPI8(int value) { return new FPI8(value); }
		public static implicit operator FPI8(short value) { return new FPI8(value); }
		
		public static explicit operator FPI8(uint value) { return new FPI8(value); }
		public static implicit operator FPI8(ushort value) { return new FPI8(value); }

		public static explicit operator FPI8(long value) { return new FPI8(value); }
		public static explicit operator FPI8(ulong value) { return new FPI8(value); }
		public static explicit operator FPI8(float value) { return new FPI8(value); }
		public static explicit operator FPI8(double value) { return new FPI8(value); }
		public static explicit operator int(FPI8 value) { return (int)(value.N >> Frac); }
		public static explicit operator long(FPI8 value) { return value.N >> Frac; }
		public static explicit operator uint(FPI8 value) { return (uint)(value.N >> Frac); }
		public static explicit operator ulong(FPI8 value) { return (ulong)(value.N >> Frac); }
		public static explicit operator float(FPI8 value) { return (float)value.N * (1.0f / (1 << Frac)); }
		public static explicit operator double(FPI8 value) { return (double)value.N * (1.0 / (1 << Frac)); }

		public Int32 N;

		private static void Overflow()
		{
 			throw new OverflowException();
		}
		public static FPI8 CheckedCast(int num)
		{
			if (num < MinInt || num > MaxInt )
				Overflow();
			return Prescaled(num << Frac);
		}
		public static FPI8 CheckedCast(uint num)
		{
			if (num > MaxInt)
				Overflow();
			return Prescaled((int)num << Frac);
		}
		public static FPI8 CheckedCast(long num)
		{
			if (num < MinInt || num > MaxInt)
				Overflow();
			return Prescaled((int)num << Frac);
		}
		public static FPI8 CheckedCast(ulong num)
		{
			if (num > MaxInt)
				Overflow();
			return Prescaled((int)num << Frac);
		}
		public static FPI8 CheckedCast(double num)
		{
			if (!(num >= MinDouble && num <= MaxDouble))
				Overflow();
			return FastCast(num);
		}
		public static FPI8 FastCast(int num)
		{
			return Prescaled((Int32)num << Frac);
		}
		public static FPI8 FastCast(uint num)
		{
			return Prescaled((Int32)num << Frac);
		}
		public static FPI8 FastCast(long num)
		{
			return Prescaled((Int32)num << Frac);
		}
		public static FPI8 FastCast(double num)
		{
			return Prescaled((int)Math.Round(MathEx.ShiftLeft(num, Frac)));
		}

		public FPI8(int num)
		{
			N = num << Frac;
			if (num < MinInt)
				N = Int32.MinValue;
			if (num > MaxInt)
				N = Int32.MaxValue;
		}
		public FPI8(uint num)
		{
			N = (int)num << Frac;
			if (num > (uint)MaxInt)
				N = Int32.MaxValue;
		}
        public FPI8(long num)
		{
			N = (int)num << Frac;
			if (num < MinInt)
				N = Int32.MinValue;
			if (num > MaxInt)
				N = Int32.MaxValue;
		}
		public FPI8(ulong num)
		{
			N = (int)num << Frac;
			if (num > MaxInt)
				N = Int32.MaxValue;
		}
        public FPI8(double num)
		{
			N = (int)Math.Round(MathEx.ShiftLeft(num, Frac));
			if (num <= MinDouble)
				N = Int32.MinValue;
			if (num >= MaxDouble)
				N = Int32.MaxValue;
		}

		public static FPI8 operator +(FPI8 a, int b) { a.N += b << Frac; return a; }
        public static FPI8 operator -(FPI8 a, int b) { a.N -= b << Frac; return a; }
        public static FPI8 operator *(FPI8 a, int b) { a.N *= b; return a; }
        public static FPI8 operator /(FPI8 a, int b) { a.N /= b; return a; }
        public static FPI8 operator %(FPI8 a, int b) { a.N %= b << Frac; return a; }
		public static FPI8 operator +(FPI8 a, FPI8 b) { a.N += b.N; return a; }
        public static FPI8 operator -(FPI8 a, FPI8 b) { a.N -= b.N; return a; }
		public static FPI8 operator -(FPI8 a) { a.N = -a.N; return a; }

		public static FPI8 operator *(FPI8 a, FPI8 b) { return Prescaled((int)((long)a.N * (long)b.N >> Frac)); }
        public static FPI8 operator /(FPI8 a, FPI8 b) { return Prescaled((int)((long)(a.N << Frac) / b.N)); }
        public static FPI8 operator %(FPI8 a, FPI8 b) { a.N %= b.N; return a; }
		public static FPI8 operator <<(FPI8 a, int b) { a.N <<= b; return a; }
		public static FPI8 operator >>(FPI8 a, int b) { a.N >>= b; return a; }
		public static bool operator ==(FPI8 a, FPI8 b) { return a.N == b.N; }
		public static bool operator !=(FPI8 a, FPI8 b) { return a.N != b.N; }
		public static bool operator >=(FPI8 a, FPI8 b) { return a.N >= b.N; }
		public static bool operator <=(FPI8 a, FPI8 b) { return a.N <= b.N; }
		public static bool operator >(FPI8 a, FPI8 b) { return a.N > b.N; }
		public static bool operator <(FPI8 a, FPI8 b) { return a.N < b.N; }

		public static FPI8 operator &(FPI8 a, FPI8 b) { a.N &= b.N; return a; }
        public static FPI8 operator |(FPI8 a, FPI8 b) { a.N |= b.N; return a; }
        public static FPI8 operator ^(FPI8 a, FPI8 b) { a.N ^= b.N; return a; }
        public static FPI8 operator ~(FPI8 a) { a.N = ~a.N; return a; }
		
		public int CountOnes() { return MathEx.CountOnes(N); }
		public int Log2Floor()
		{
			int r = MathEx.Log2Floor(N);
			if (r >= 0) r -= Frac;
			return r;
		}
		public FPI8 Sqrt()
		{
			if ((uint)N <= (uint)MaxInt)
				return Prescaled((Int32)MathEx.Sqrt((uint)N << Frac));
			else
				// Compute lower-precision answer (this path is also taken if N is negative)
				return Prescaled(MathEx.Sqrt(N) << Frac/2);
		}

		public override bool Equals(object obj)
		{
			return obj is FPI8 && ((FPI8)obj).N == N;
		}
		public override int GetHashCode()
		{
			return N.GetHashCode();
		}
		public override string ToString()
		{
			return ((double)this).ToString();
		}

		public int CompareTo(FPI8 other)
		{
			return N.CompareTo(other.N);
		}
		public bool Equals(FPI8 other)
		{
			return N == other.N;
		}

		#region IConvertible

		TypeCode IConvertible.GetTypeCode()
		{
			return TypeCode.Object;
		}
		public bool ToBoolean(IFormatProvider provider)
		{
			return N != 0;
		}
		public sbyte ToSByte(IFormatProvider provider)
		{
			return checked((sbyte)(N >> Frac));
		}
		public short ToInt16(IFormatProvider provider)
		{
			return checked((short)(N >> Frac));
		}
		public int ToInt32(IFormatProvider provider)
		{
			return checked((int)(N >> Frac));
		}
		public long ToInt64(IFormatProvider provider)
		{
			return checked((long)(N >> Frac));
		}
		public byte ToByte(IFormatProvider provider)
		{
			return checked((byte)(N >> Frac));
		}
		public ushort ToUInt16(IFormatProvider provider)
		{
			return checked((ushort)(N >> Frac));
		}
		public uint ToUInt32(IFormatProvider provider)
		{
			return checked((uint)(N >> Frac));
		}
		public ulong ToUInt64(IFormatProvider provider)
		{
			return checked((ulong)(N >> Frac));
		}
		public char ToChar(IFormatProvider provider)
		{
			return checked((char)(N >> Frac));
		}
		public double ToDouble(IFormatProvider provider)
		{
			return (double)this;
		}
		DateTime IConvertible.ToDateTime(IFormatProvider provider)
		{
			throw new InvalidCastException();
		}
		public decimal ToDecimal(IFormatProvider provider)
		{
			return (decimal)(double)this;
		}
		public float ToSingle(IFormatProvider provider)
		{
			return (float)this;
		}
		string IConvertible.ToString(IFormatProvider provider)
		{
			return ToString();
		}
		object IConvertible.ToType(Type conversionType, IFormatProvider provider)
		{
			return Convert.ChangeType((double)this, conversionType);
		}

		#endregion
    }

	
    public partial struct FPI16 : IComparable< FPI16 >, IEquatable< FPI16 >, IConvertible
    {
        public const int Frac = 16;
		public static FPI16 Prescaled(Int32 n) { FPI16 r = new FPI16(); r.N = n; return r; }
        public static readonly FPI16 Zero = new FPI16();
		public static readonly FPI16 One = new FPI16(1);
		public static readonly FPI16 Epsilon = Prescaled(1);
        public static readonly FPI16 MaxValue = Prescaled(Int32.MaxValue);
        public static readonly FPI16 MinValue = Prescaled(Int32.MinValue);
        public const Int32 MaxInt = Int32.MaxValue >> Frac;
        public const Int32 MinInt = Int32.MinValue >> Frac;
        public const double MaxDouble = Int32.MaxValue / (double)(1 << Frac);
        public const double MinDouble = Int32.MinValue / (double)(1 << Frac);
		public const Int32 Mask = (1 << Frac) - 1;

		public static explicit operator FPI16(int value) { return new FPI16(value); }
		public static implicit operator FPI16(short value) { return new FPI16(value); }
		
		public static explicit operator FPI16(uint value) { return new FPI16(value); }
		public static implicit operator FPI16(ushort value) { return new FPI16(value); }

		public static explicit operator FPI16(long value) { return new FPI16(value); }
		public static explicit operator FPI16(ulong value) { return new FPI16(value); }
		public static explicit operator FPI16(float value) { return new FPI16(value); }
		public static explicit operator FPI16(double value) { return new FPI16(value); }
		public static explicit operator int(FPI16 value) { return (int)(value.N >> Frac); }
		public static explicit operator long(FPI16 value) { return value.N >> Frac; }
		public static explicit operator uint(FPI16 value) { return (uint)(value.N >> Frac); }
		public static explicit operator ulong(FPI16 value) { return (ulong)(value.N >> Frac); }
		public static explicit operator float(FPI16 value) { return (float)value.N * (1.0f / (1 << Frac)); }
		public static explicit operator double(FPI16 value) { return (double)value.N * (1.0 / (1 << Frac)); }

		public Int32 N;

		private static void Overflow()
		{
 			throw new OverflowException();
		}
		public static FPI16 CheckedCast(int num)
		{
			if (num < MinInt || num > MaxInt )
				Overflow();
			return Prescaled(num << Frac);
		}
		public static FPI16 CheckedCast(uint num)
		{
			if (num > MaxInt)
				Overflow();
			return Prescaled((int)num << Frac);
		}
		public static FPI16 CheckedCast(long num)
		{
			if (num < MinInt || num > MaxInt)
				Overflow();
			return Prescaled((int)num << Frac);
		}
		public static FPI16 CheckedCast(ulong num)
		{
			if (num > MaxInt)
				Overflow();
			return Prescaled((int)num << Frac);
		}
		public static FPI16 CheckedCast(double num)
		{
			if (!(num >= MinDouble && num <= MaxDouble))
				Overflow();
			return FastCast(num);
		}
		public static FPI16 FastCast(int num)
		{
			return Prescaled((Int32)num << Frac);
		}
		public static FPI16 FastCast(uint num)
		{
			return Prescaled((Int32)num << Frac);
		}
		public static FPI16 FastCast(long num)
		{
			return Prescaled((Int32)num << Frac);
		}
		public static FPI16 FastCast(double num)
		{
			return Prescaled((int)Math.Round(MathEx.ShiftLeft(num, Frac)));
		}

		public FPI16(int num)
		{
			N = num << Frac;
			if (num < MinInt)
				N = Int32.MinValue;
			if (num > MaxInt)
				N = Int32.MaxValue;
		}
		public FPI16(uint num)
		{
			N = (int)num << Frac;
			if (num > (uint)MaxInt)
				N = Int32.MaxValue;
		}
        public FPI16(long num)
		{
			N = (int)num << Frac;
			if (num < MinInt)
				N = Int32.MinValue;
			if (num > MaxInt)
				N = Int32.MaxValue;
		}
		public FPI16(ulong num)
		{
			N = (int)num << Frac;
			if (num > MaxInt)
				N = Int32.MaxValue;
		}
        public FPI16(double num)
		{
			N = (int)Math.Round(MathEx.ShiftLeft(num, Frac));
			if (num <= MinDouble)
				N = Int32.MinValue;
			if (num >= MaxDouble)
				N = Int32.MaxValue;
		}

		public static FPI16 operator +(FPI16 a, int b) { a.N += b << Frac; return a; }
        public static FPI16 operator -(FPI16 a, int b) { a.N -= b << Frac; return a; }
        public static FPI16 operator *(FPI16 a, int b) { a.N *= b; return a; }
        public static FPI16 operator /(FPI16 a, int b) { a.N /= b; return a; }
        public static FPI16 operator %(FPI16 a, int b) { a.N %= b << Frac; return a; }
		public static FPI16 operator +(FPI16 a, FPI16 b) { a.N += b.N; return a; }
        public static FPI16 operator -(FPI16 a, FPI16 b) { a.N -= b.N; return a; }
		public static FPI16 operator -(FPI16 a) { a.N = -a.N; return a; }

		public static FPI16 operator *(FPI16 a, FPI16 b) { return Prescaled((int)((long)a.N * (long)b.N >> Frac)); }
        public static FPI16 operator /(FPI16 a, FPI16 b) { return Prescaled((int)((long)(a.N << Frac) / b.N)); }
        public static FPI16 operator %(FPI16 a, FPI16 b) { a.N %= b.N; return a; }
		public static FPI16 operator <<(FPI16 a, int b) { a.N <<= b; return a; }
		public static FPI16 operator >>(FPI16 a, int b) { a.N >>= b; return a; }
		public static bool operator ==(FPI16 a, FPI16 b) { return a.N == b.N; }
		public static bool operator !=(FPI16 a, FPI16 b) { return a.N != b.N; }
		public static bool operator >=(FPI16 a, FPI16 b) { return a.N >= b.N; }
		public static bool operator <=(FPI16 a, FPI16 b) { return a.N <= b.N; }
		public static bool operator >(FPI16 a, FPI16 b) { return a.N > b.N; }
		public static bool operator <(FPI16 a, FPI16 b) { return a.N < b.N; }

		public static FPI16 operator &(FPI16 a, FPI16 b) { a.N &= b.N; return a; }
        public static FPI16 operator |(FPI16 a, FPI16 b) { a.N |= b.N; return a; }
        public static FPI16 operator ^(FPI16 a, FPI16 b) { a.N ^= b.N; return a; }
        public static FPI16 operator ~(FPI16 a) { a.N = ~a.N; return a; }
		
		public int CountOnes() { return MathEx.CountOnes(N); }
		public int Log2Floor()
		{
			int r = MathEx.Log2Floor(N);
			if (r >= 0) r -= Frac;
			return r;
		}
		public FPI16 Sqrt()
		{
			if ((uint)N <= (uint)MaxInt)
				return Prescaled((Int32)MathEx.Sqrt((uint)N << Frac));
			else
				// Compute lower-precision answer (this path is also taken if N is negative)
				return Prescaled(MathEx.Sqrt(N) << Frac/2);
		}

		public override bool Equals(object obj)
		{
			return obj is FPI16 && ((FPI16)obj).N == N;
		}
		public override int GetHashCode()
		{
			return N.GetHashCode();
		}
		public override string ToString()
		{
			return ((double)this).ToString();
		}

		public int CompareTo(FPI16 other)
		{
			return N.CompareTo(other.N);
		}
		public bool Equals(FPI16 other)
		{
			return N == other.N;
		}

		#region IConvertible

		TypeCode IConvertible.GetTypeCode()
		{
			return TypeCode.Object;
		}
		public bool ToBoolean(IFormatProvider provider)
		{
			return N != 0;
		}
		public sbyte ToSByte(IFormatProvider provider)
		{
			return checked((sbyte)(N >> Frac));
		}
		public short ToInt16(IFormatProvider provider)
		{
			return checked((short)(N >> Frac));
		}
		public int ToInt32(IFormatProvider provider)
		{
			return checked((int)(N >> Frac));
		}
		public long ToInt64(IFormatProvider provider)
		{
			return checked((long)(N >> Frac));
		}
		public byte ToByte(IFormatProvider provider)
		{
			return checked((byte)(N >> Frac));
		}
		public ushort ToUInt16(IFormatProvider provider)
		{
			return checked((ushort)(N >> Frac));
		}
		public uint ToUInt32(IFormatProvider provider)
		{
			return checked((uint)(N >> Frac));
		}
		public ulong ToUInt64(IFormatProvider provider)
		{
			return checked((ulong)(N >> Frac));
		}
		public char ToChar(IFormatProvider provider)
		{
			return checked((char)(N >> Frac));
		}
		public double ToDouble(IFormatProvider provider)
		{
			return (double)this;
		}
		DateTime IConvertible.ToDateTime(IFormatProvider provider)
		{
			throw new InvalidCastException();
		}
		public decimal ToDecimal(IFormatProvider provider)
		{
			return (decimal)(double)this;
		}
		public float ToSingle(IFormatProvider provider)
		{
			return (float)this;
		}
		string IConvertible.ToString(IFormatProvider provider)
		{
			return ToString();
		}
		object IConvertible.ToType(Type conversionType, IFormatProvider provider)
		{
			return Convert.ChangeType((double)this, conversionType);
		}

		#endregion
    }

	
    public partial struct FPI23 : IComparable< FPI23 >, IEquatable< FPI23 >, IConvertible
    {
        public const int Frac = 23;
		public static FPI23 Prescaled(Int32 n) { FPI23 r = new FPI23(); r.N = n; return r; }
        public static readonly FPI23 Zero = new FPI23();
		public static readonly FPI23 One = new FPI23(1);
		public static readonly FPI23 Epsilon = Prescaled(1);
        public static readonly FPI23 MaxValue = Prescaled(Int32.MaxValue);
        public static readonly FPI23 MinValue = Prescaled(Int32.MinValue);
        public const Int32 MaxInt = Int32.MaxValue >> Frac;
        public const Int32 MinInt = Int32.MinValue >> Frac;
        public const double MaxDouble = Int32.MaxValue / (double)(1 << Frac);
        public const double MinDouble = Int32.MinValue / (double)(1 << Frac);
		public const Int32 Mask = (1 << Frac) - 1;

		public static explicit operator FPI23(int value) { return new FPI23(value); }
		public static implicit operator FPI23(sbyte value) { return new FPI23(value); }
		
		public static explicit operator FPI23(uint value) { return new FPI23(value); }
		public static implicit operator FPI23(byte value) { return new FPI23(value); }

		public static explicit operator FPI23(long value) { return new FPI23(value); }
		public static explicit operator FPI23(ulong value) { return new FPI23(value); }
		public static explicit operator FPI23(float value) { return new FPI23(value); }
		public static explicit operator FPI23(double value) { return new FPI23(value); }
		public static explicit operator int(FPI23 value) { return (int)(value.N >> Frac); }
		public static explicit operator long(FPI23 value) { return value.N >> Frac; }
		public static explicit operator uint(FPI23 value) { return (uint)(value.N >> Frac); }
		public static explicit operator ulong(FPI23 value) { return (ulong)(value.N >> Frac); }
		public static explicit operator float(FPI23 value) { return (float)value.N * (1.0f / (1 << Frac)); }
		public static explicit operator double(FPI23 value) { return (double)value.N * (1.0 / (1 << Frac)); }

		public Int32 N;

		private static void Overflow()
		{
 			throw new OverflowException();
		}
		public static FPI23 CheckedCast(int num)
		{
			if (num < MinInt || num > MaxInt )
				Overflow();
			return Prescaled(num << Frac);
		}
		public static FPI23 CheckedCast(uint num)
		{
			if (num > MaxInt)
				Overflow();
			return Prescaled((int)num << Frac);
		}
		public static FPI23 CheckedCast(long num)
		{
			if (num < MinInt || num > MaxInt)
				Overflow();
			return Prescaled((int)num << Frac);
		}
		public static FPI23 CheckedCast(ulong num)
		{
			if (num > MaxInt)
				Overflow();
			return Prescaled((int)num << Frac);
		}
		public static FPI23 CheckedCast(double num)
		{
			if (!(num >= MinDouble && num <= MaxDouble))
				Overflow();
			return FastCast(num);
		}
		public static FPI23 FastCast(int num)
		{
			return Prescaled((Int32)num << Frac);
		}
		public static FPI23 FastCast(uint num)
		{
			return Prescaled((Int32)num << Frac);
		}
		public static FPI23 FastCast(long num)
		{
			return Prescaled((Int32)num << Frac);
		}
		public static FPI23 FastCast(double num)
		{
			return Prescaled((int)Math.Round(MathEx.ShiftLeft(num, Frac)));
		}

		public FPI23(int num)
		{
			N = num << Frac;
			if (num < MinInt)
				N = Int32.MinValue;
			if (num > MaxInt)
				N = Int32.MaxValue;
		}
		public FPI23(uint num)
		{
			N = (int)num << Frac;
			if (num > (uint)MaxInt)
				N = Int32.MaxValue;
		}
        public FPI23(long num)
		{
			N = (int)num << Frac;
			if (num < MinInt)
				N = Int32.MinValue;
			if (num > MaxInt)
				N = Int32.MaxValue;
		}
		public FPI23(ulong num)
		{
			N = (int)num << Frac;
			if (num > MaxInt)
				N = Int32.MaxValue;
		}
        public FPI23(double num)
		{
			N = (int)Math.Round(MathEx.ShiftLeft(num, Frac));
			if (num <= MinDouble)
				N = Int32.MinValue;
			if (num >= MaxDouble)
				N = Int32.MaxValue;
		}

		public static FPI23 operator +(FPI23 a, int b) { a.N += b << Frac; return a; }
        public static FPI23 operator -(FPI23 a, int b) { a.N -= b << Frac; return a; }
        public static FPI23 operator *(FPI23 a, int b) { a.N *= b; return a; }
        public static FPI23 operator /(FPI23 a, int b) { a.N /= b; return a; }
        public static FPI23 operator %(FPI23 a, int b) { a.N %= b << Frac; return a; }
		public static FPI23 operator +(FPI23 a, FPI23 b) { a.N += b.N; return a; }
        public static FPI23 operator -(FPI23 a, FPI23 b) { a.N -= b.N; return a; }
		public static FPI23 operator -(FPI23 a) { a.N = -a.N; return a; }

		public static FPI23 operator *(FPI23 a, FPI23 b) { return Prescaled((int)((long)a.N * (long)b.N >> Frac)); }
        public static FPI23 operator /(FPI23 a, FPI23 b) { return Prescaled((int)((long)(a.N << Frac) / b.N)); }
        public static FPI23 operator %(FPI23 a, FPI23 b) { a.N %= b.N; return a; }
		public static FPI23 operator <<(FPI23 a, int b) { a.N <<= b; return a; }
		public static FPI23 operator >>(FPI23 a, int b) { a.N >>= b; return a; }
		public static bool operator ==(FPI23 a, FPI23 b) { return a.N == b.N; }
		public static bool operator !=(FPI23 a, FPI23 b) { return a.N != b.N; }
		public static bool operator >=(FPI23 a, FPI23 b) { return a.N >= b.N; }
		public static bool operator <=(FPI23 a, FPI23 b) { return a.N <= b.N; }
		public static bool operator >(FPI23 a, FPI23 b) { return a.N > b.N; }
		public static bool operator <(FPI23 a, FPI23 b) { return a.N < b.N; }

		public static FPI23 operator &(FPI23 a, FPI23 b) { a.N &= b.N; return a; }
        public static FPI23 operator |(FPI23 a, FPI23 b) { a.N |= b.N; return a; }
        public static FPI23 operator ^(FPI23 a, FPI23 b) { a.N ^= b.N; return a; }
        public static FPI23 operator ~(FPI23 a) { a.N = ~a.N; return a; }
		
		public int CountOnes() { return MathEx.CountOnes(N); }
		public int Log2Floor()
		{
			int r = MathEx.Log2Floor(N);
			if (r >= 0) r -= Frac;
			return r;
		}
		public FPI23 Sqrt()
		{
			if ((uint)N <= (uint)MaxInt)
				return Prescaled((Int32)MathEx.Sqrt((uint)N << Frac));
			else
				// Compute lower-precision answer (this path is also taken if N is negative)
				return Prescaled(MathEx.Sqrt(N << 1) << Frac/2);
		}

		public override bool Equals(object obj)
		{
			return obj is FPI23 && ((FPI23)obj).N == N;
		}
		public override int GetHashCode()
		{
			return N.GetHashCode();
		}
		public override string ToString()
		{
			return ((double)this).ToString();
		}

		public int CompareTo(FPI23 other)
		{
			return N.CompareTo(other.N);
		}
		public bool Equals(FPI23 other)
		{
			return N == other.N;
		}

		#region IConvertible

		TypeCode IConvertible.GetTypeCode()
		{
			return TypeCode.Object;
		}
		public bool ToBoolean(IFormatProvider provider)
		{
			return N != 0;
		}
		public sbyte ToSByte(IFormatProvider provider)
		{
			return checked((sbyte)(N >> Frac));
		}
		public short ToInt16(IFormatProvider provider)
		{
			return checked((short)(N >> Frac));
		}
		public int ToInt32(IFormatProvider provider)
		{
			return checked((int)(N >> Frac));
		}
		public long ToInt64(IFormatProvider provider)
		{
			return checked((long)(N >> Frac));
		}
		public byte ToByte(IFormatProvider provider)
		{
			return checked((byte)(N >> Frac));
		}
		public ushort ToUInt16(IFormatProvider provider)
		{
			return checked((ushort)(N >> Frac));
		}
		public uint ToUInt32(IFormatProvider provider)
		{
			return checked((uint)(N >> Frac));
		}
		public ulong ToUInt64(IFormatProvider provider)
		{
			return checked((ulong)(N >> Frac));
		}
		public char ToChar(IFormatProvider provider)
		{
			return checked((char)(N >> Frac));
		}
		public double ToDouble(IFormatProvider provider)
		{
			return (double)this;
		}
		DateTime IConvertible.ToDateTime(IFormatProvider provider)
		{
			throw new InvalidCastException();
		}
		public decimal ToDecimal(IFormatProvider provider)
		{
			return (decimal)(double)this;
		}
		public float ToSingle(IFormatProvider provider)
		{
			return (float)this;
		}
		string IConvertible.ToString(IFormatProvider provider)
		{
			return ToString();
		}
		object IConvertible.ToType(Type conversionType, IFormatProvider provider)
		{
			return Convert.ChangeType((double)this, conversionType);
		}

		#endregion
    }

	
    public partial struct FPL16 : IComparable< FPL16 >, IEquatable< FPL16 >, IConvertible
    {
        public const int Frac = 16;
		public static FPL16 Prescaled(Int64 n) { FPL16 r = new FPL16(); r.N = n; return r; }
        public static readonly FPL16 Zero = new FPL16();
		public static readonly FPL16 One = new FPL16(1);
		public static readonly FPL16 Epsilon = Prescaled(1);
        public static readonly FPL16 MaxValue = Prescaled(Int64.MaxValue);
        public static readonly FPL16 MinValue = Prescaled(Int64.MinValue);
        public const Int64 MaxInt = Int64.MaxValue >> Frac;
        public const Int64 MinInt = Int64.MinValue >> Frac;
        public const double MaxDouble = Int64.MaxValue / (double)(1 << Frac);
        public const double MinDouble = Int64.MinValue / (double)(1 << Frac);
		public const Int64 Mask = (1 << Frac) - 1;

		public static implicit operator FPL16(int value) { return new FPL16(value); }
		
		public static implicit operator FPL16(uint value) { return new FPL16(value); }

		public static explicit operator FPL16(long value) { return new FPL16(value); }
		public static explicit operator FPL16(ulong value) { return new FPL16(value); }
		public static explicit operator FPL16(float value) { return new FPL16(value); }
		public static explicit operator FPL16(double value) { return new FPL16(value); }
		public static explicit operator int(FPL16 value) { return (int)(value.N >> Frac); }
		public static explicit operator long(FPL16 value) { return value.N >> Frac; }
		public static explicit operator uint(FPL16 value) { return (uint)(value.N >> Frac); }
		public static explicit operator ulong(FPL16 value) { return (ulong)(value.N >> Frac); }
		public static explicit operator float(FPL16 value) { return (float)value.N * (1.0f / (1 << Frac)); }
		public static explicit operator double(FPL16 value) { return (double)value.N * (1.0 / (1 << Frac)); }

		public Int64 N;

		private static void Overflow()
		{
 			throw new OverflowException();
		}
		public static FPL16 CheckedCast(long num)
		{
			if (num < MinInt || num > MaxInt)
				Overflow();
			return Prescaled((int)num << Frac);
		}
		public static FPL16 CheckedCast(ulong num)
		{
			if (num > MaxInt)
				Overflow();
			return Prescaled((int)num << Frac);
		}
		public static FPL16 CheckedCast(double num)
		{
			if (!(num >= MinDouble && num <= MaxDouble))
				Overflow();
			return FastCast(num);
		}
		public static FPL16 FastCast(int num)
		{
			return Prescaled((Int64)num << Frac);
		}
		public static FPL16 FastCast(uint num)
		{
			return Prescaled((Int64)num << Frac);
		}
		public static FPL16 FastCast(long num)
		{
			return Prescaled((Int64)num << Frac);
		}
		public static FPL16 FastCast(double num)
		{
			return Prescaled((int)Math.Round(MathEx.ShiftLeft(num, Frac)));
		}

		public FPL16(int num)
		{
			N = num << Frac;
		}
		public FPL16(uint num)
		{
			N = (int)num << Frac;
		}
        public FPL16(long num)
		{
			N = (int)num << Frac;
			if (num < MinInt)
				N = Int64.MinValue;
			if (num > MaxInt)
				N = Int64.MaxValue;
		}
		public FPL16(ulong num)
		{
			N = (int)num << Frac;
			if (num > MaxInt)
				N = Int64.MaxValue;
		}
        public FPL16(double num)
		{
			N = (int)Math.Round(MathEx.ShiftLeft(num, Frac));
			if (num <= MinDouble)
				N = Int64.MinValue;
			if (num >= MaxDouble)
				N = Int64.MaxValue;
		}

		public static FPL16 operator +(FPL16 a, int b) { a.N += b << Frac; return a; }
        public static FPL16 operator -(FPL16 a, int b) { a.N -= b << Frac; return a; }
        public static FPL16 operator *(FPL16 a, int b) { a.N *= b; return a; }
        public static FPL16 operator /(FPL16 a, int b) { a.N /= b; return a; }
        public static FPL16 operator %(FPL16 a, int b) { a.N %= b << Frac; return a; }
		public static FPL16 operator +(FPL16 a, FPL16 b) { a.N += b.N; return a; }
        public static FPL16 operator -(FPL16 a, FPL16 b) { a.N -= b.N; return a; }
		public static FPL16 operator -(FPL16 a) { a.N = -a.N; return a; }

		public static FPL16 operator *(FPL16 a, FPL16 b)
		{
			var afrac = a.N & Mask;
			var bfrac = b.N & Mask;
			var whole = (FPL16)((Int64)a * (Int64)b);
			whole.N += afrac * bfrac >> Frac;
			return whole;
		}
        public static FPL16 operator /(FPL16 a, FPL16 b)
		{
			long whole = a.N / b.N;
			long remainder = a.N % b.N;
			remainder = (remainder << Frac) / b.N;
			Debug.Assert(remainder < (1 << Frac));
			a.N = (whole << Frac) + remainder;
			return a;
			// TODO: test negative numbers: 7 / -2.5, -7 / 2.5, -7 / -2.5
		}
        public static FPL16 operator %(FPL16 a, FPL16 b) { a.N %= b.N; return a; }
		public static FPL16 operator <<(FPL16 a, int b) { a.N <<= b; return a; }
		public static FPL16 operator >>(FPL16 a, int b) { a.N >>= b; return a; }
		public static bool operator ==(FPL16 a, FPL16 b) { return a.N == b.N; }
		public static bool operator !=(FPL16 a, FPL16 b) { return a.N != b.N; }
		public static bool operator >=(FPL16 a, FPL16 b) { return a.N >= b.N; }
		public static bool operator <=(FPL16 a, FPL16 b) { return a.N <= b.N; }
		public static bool operator >(FPL16 a, FPL16 b) { return a.N > b.N; }
		public static bool operator <(FPL16 a, FPL16 b) { return a.N < b.N; }

		public static FPL16 operator &(FPL16 a, FPL16 b) { a.N &= b.N; return a; }
        public static FPL16 operator |(FPL16 a, FPL16 b) { a.N |= b.N; return a; }
        public static FPL16 operator ^(FPL16 a, FPL16 b) { a.N ^= b.N; return a; }
        public static FPL16 operator ~(FPL16 a) { a.N = ~a.N; return a; }
		
		public int CountOnes() { return MathEx.CountOnes(N); }
		public int Log2Floor()
		{
			int r = MathEx.Log2Floor(N);
			if (r >= 0) r -= Frac;
			return r;
		}
		public FPL16 Sqrt()
		{
			if ((ulong)N <= (ulong)MaxInt)
				return Prescaled((Int64)MathEx.Sqrt((ulong)N << Frac));
			else
				// Compute lower-precision answer (this path is also taken if N is negative)
				return Prescaled(MathEx.Sqrt(N) << Frac/2);
		}

		public override bool Equals(object obj)
		{
			return obj is FPL16 && ((FPL16)obj).N == N;
		}
		public override int GetHashCode()
		{
			return N.GetHashCode();
		}
		public override string ToString()
		{
			return ((double)this).ToString();
		}

		public int CompareTo(FPL16 other)
		{
			return N.CompareTo(other.N);
		}
		public bool Equals(FPL16 other)
		{
			return N == other.N;
		}

		#region IConvertible

		TypeCode IConvertible.GetTypeCode()
		{
			return TypeCode.Object;
		}
		public bool ToBoolean(IFormatProvider provider)
		{
			return N != 0;
		}
		public sbyte ToSByte(IFormatProvider provider)
		{
			return checked((sbyte)(N >> Frac));
		}
		public short ToInt16(IFormatProvider provider)
		{
			return checked((short)(N >> Frac));
		}
		public int ToInt32(IFormatProvider provider)
		{
			return checked((int)(N >> Frac));
		}
		public long ToInt64(IFormatProvider provider)
		{
			return checked((long)(N >> Frac));
		}
		public byte ToByte(IFormatProvider provider)
		{
			return checked((byte)(N >> Frac));
		}
		public ushort ToUInt16(IFormatProvider provider)
		{
			return checked((ushort)(N >> Frac));
		}
		public uint ToUInt32(IFormatProvider provider)
		{
			return checked((uint)(N >> Frac));
		}
		public ulong ToUInt64(IFormatProvider provider)
		{
			return checked((ulong)(N >> Frac));
		}
		public char ToChar(IFormatProvider provider)
		{
			return checked((char)(N >> Frac));
		}
		public double ToDouble(IFormatProvider provider)
		{
			return (double)this;
		}
		DateTime IConvertible.ToDateTime(IFormatProvider provider)
		{
			throw new InvalidCastException();
		}
		public decimal ToDecimal(IFormatProvider provider)
		{
			return (decimal)(double)this;
		}
		public float ToSingle(IFormatProvider provider)
		{
			return (float)this;
		}
		string IConvertible.ToString(IFormatProvider provider)
		{
			return ToString();
		}
		object IConvertible.ToType(Type conversionType, IFormatProvider provider)
		{
			return Convert.ChangeType((double)this, conversionType);
		}

		#endregion
    }

	
    public partial struct FPL32 : IComparable< FPL32 >, IEquatable< FPL32 >, IConvertible
    {
        public const int Frac = 32;
		public static FPL32 Prescaled(Int64 n) { FPL32 r = new FPL32(); r.N = n; return r; }
        public static readonly FPL32 Zero = new FPL32();
		public static readonly FPL32 One = new FPL32(1);
		public static readonly FPL32 Epsilon = Prescaled(1);
        public static readonly FPL32 MaxValue = Prescaled(Int64.MaxValue);
        public static readonly FPL32 MinValue = Prescaled(Int64.MinValue);
        public const Int64 MaxInt = Int64.MaxValue >> Frac;
        public const Int64 MinInt = Int64.MinValue >> Frac;
        public const double MaxDouble = Int64.MaxValue / (double)(1 << Frac);
        public const double MinDouble = Int64.MinValue / (double)(1 << Frac);
		public const Int64 Mask = (1 << Frac) - 1;

		public static implicit operator FPL32(int value) { return new FPL32(value); }
		
		public static explicit operator FPL32(uint value) { return new FPL32(value); }
		public static implicit operator FPL32(ushort value) { return new FPL32(value); }

		public static explicit operator FPL32(long value) { return new FPL32(value); }
		public static explicit operator FPL32(ulong value) { return new FPL32(value); }
		public static explicit operator FPL32(float value) { return new FPL32(value); }
		public static explicit operator FPL32(double value) { return new FPL32(value); }
		public static explicit operator int(FPL32 value) { return (int)(value.N >> Frac); }
		public static explicit operator long(FPL32 value) { return value.N >> Frac; }
		public static explicit operator uint(FPL32 value) { return (uint)(value.N >> Frac); }
		public static explicit operator ulong(FPL32 value) { return (ulong)(value.N >> Frac); }
		public static explicit operator float(FPL32 value) { return (float)value.N * (1.0f / (1 << Frac)); }
		public static explicit operator double(FPL32 value) { return (double)value.N * (1.0 / (1 << Frac)); }

		public Int64 N;

		private static void Overflow()
		{
 			throw new OverflowException();
		}
		public static FPL32 CheckedCast(long num)
		{
			if (num < MinInt || num > MaxInt)
				Overflow();
			return Prescaled((int)num << Frac);
		}
		public static FPL32 CheckedCast(ulong num)
		{
			if (num > MaxInt)
				Overflow();
			return Prescaled((int)num << Frac);
		}
		public static FPL32 CheckedCast(double num)
		{
			if (!(num >= MinDouble && num <= MaxDouble))
				Overflow();
			return FastCast(num);
		}
		public static FPL32 FastCast(int num)
		{
			return Prescaled((Int64)num << Frac);
		}
		public static FPL32 FastCast(uint num)
		{
			return Prescaled((Int64)num << Frac);
		}
		public static FPL32 FastCast(long num)
		{
			return Prescaled((Int64)num << Frac);
		}
		public static FPL32 FastCast(double num)
		{
			return Prescaled((int)Math.Round(MathEx.ShiftLeft(num, Frac)));
		}

		public FPL32(int num)
		{
			N = num << Frac;
		}
		public FPL32(uint num)
		{
			N = (int)num << Frac;
			if (num > (uint)MaxInt)
				N = Int64.MaxValue;
		}
        public FPL32(long num)
		{
			N = (int)num << Frac;
			if (num < MinInt)
				N = Int64.MinValue;
			if (num > MaxInt)
				N = Int64.MaxValue;
		}
		public FPL32(ulong num)
		{
			N = (int)num << Frac;
			if (num > MaxInt)
				N = Int64.MaxValue;
		}
        public FPL32(double num)
		{
			N = (int)Math.Round(MathEx.ShiftLeft(num, Frac));
			if (num <= MinDouble)
				N = Int64.MinValue;
			if (num >= MaxDouble)
				N = Int64.MaxValue;
		}

		public static FPL32 operator +(FPL32 a, int b) { a.N += b << Frac; return a; }
        public static FPL32 operator -(FPL32 a, int b) { a.N -= b << Frac; return a; }
        public static FPL32 operator *(FPL32 a, int b) { a.N *= b; return a; }
        public static FPL32 operator /(FPL32 a, int b) { a.N /= b; return a; }
        public static FPL32 operator %(FPL32 a, int b) { a.N %= b << Frac; return a; }
		public static FPL32 operator +(FPL32 a, FPL32 b) { a.N += b.N; return a; }
        public static FPL32 operator -(FPL32 a, FPL32 b) { a.N -= b.N; return a; }
		public static FPL32 operator -(FPL32 a) { a.N = -a.N; return a; }

		public static FPL32 operator *(FPL32 a, FPL32 b)
		{
			var afrac = a.N & Mask;
			var bfrac = b.N & Mask;
			var whole = (FPL32)((Int64)a * (Int64)b);
			whole.N += afrac * bfrac >> Frac;
			return whole;
		}
        public static FPL32 operator /(FPL32 a, FPL32 b)
		{
			long whole = a.N / b.N;
			long remainder = a.N % b.N;
			remainder = (remainder << Frac) / b.N;
			Debug.Assert(remainder < (1 << Frac));
			a.N = (whole << Frac) + remainder;
			return a;
			// TODO: test negative numbers: 7 / -2.5, -7 / 2.5, -7 / -2.5
		}
        public static FPL32 operator %(FPL32 a, FPL32 b) { a.N %= b.N; return a; }
		public static FPL32 operator <<(FPL32 a, int b) { a.N <<= b; return a; }
		public static FPL32 operator >>(FPL32 a, int b) { a.N >>= b; return a; }
		public static bool operator ==(FPL32 a, FPL32 b) { return a.N == b.N; }
		public static bool operator !=(FPL32 a, FPL32 b) { return a.N != b.N; }
		public static bool operator >=(FPL32 a, FPL32 b) { return a.N >= b.N; }
		public static bool operator <=(FPL32 a, FPL32 b) { return a.N <= b.N; }
		public static bool operator >(FPL32 a, FPL32 b) { return a.N > b.N; }
		public static bool operator <(FPL32 a, FPL32 b) { return a.N < b.N; }

		public static FPL32 operator &(FPL32 a, FPL32 b) { a.N &= b.N; return a; }
        public static FPL32 operator |(FPL32 a, FPL32 b) { a.N |= b.N; return a; }
        public static FPL32 operator ^(FPL32 a, FPL32 b) { a.N ^= b.N; return a; }
        public static FPL32 operator ~(FPL32 a) { a.N = ~a.N; return a; }
		
		public int CountOnes() { return MathEx.CountOnes(N); }
		public int Log2Floor()
		{
			int r = MathEx.Log2Floor(N);
			if (r >= 0) r -= Frac;
			return r;
		}
		public FPL32 Sqrt()
		{
			if ((ulong)N <= (ulong)MaxInt)
				return Prescaled((Int64)MathEx.Sqrt((ulong)N << Frac));
			else
				// Compute lower-precision answer (this path is also taken if N is negative)
				return Prescaled(MathEx.Sqrt(N) << Frac/2);
		}

		public override bool Equals(object obj)
		{
			return obj is FPL32 && ((FPL32)obj).N == N;
		}
		public override int GetHashCode()
		{
			return N.GetHashCode();
		}
		public override string ToString()
		{
			return ((double)this).ToString();
		}

		public int CompareTo(FPL32 other)
		{
			return N.CompareTo(other.N);
		}
		public bool Equals(FPL32 other)
		{
			return N == other.N;
		}

		#region IConvertible

		TypeCode IConvertible.GetTypeCode()
		{
			return TypeCode.Object;
		}
		public bool ToBoolean(IFormatProvider provider)
		{
			return N != 0;
		}
		public sbyte ToSByte(IFormatProvider provider)
		{
			return checked((sbyte)(N >> Frac));
		}
		public short ToInt16(IFormatProvider provider)
		{
			return checked((short)(N >> Frac));
		}
		public int ToInt32(IFormatProvider provider)
		{
			return checked((int)(N >> Frac));
		}
		public long ToInt64(IFormatProvider provider)
		{
			return checked((long)(N >> Frac));
		}
		public byte ToByte(IFormatProvider provider)
		{
			return checked((byte)(N >> Frac));
		}
		public ushort ToUInt16(IFormatProvider provider)
		{
			return checked((ushort)(N >> Frac));
		}
		public uint ToUInt32(IFormatProvider provider)
		{
			return checked((uint)(N >> Frac));
		}
		public ulong ToUInt64(IFormatProvider provider)
		{
			return checked((ulong)(N >> Frac));
		}
		public char ToChar(IFormatProvider provider)
		{
			return checked((char)(N >> Frac));
		}
		public double ToDouble(IFormatProvider provider)
		{
			return (double)this;
		}
		DateTime IConvertible.ToDateTime(IFormatProvider provider)
		{
			throw new InvalidCastException();
		}
		public decimal ToDecimal(IFormatProvider provider)
		{
			return (decimal)(double)this;
		}
		public float ToSingle(IFormatProvider provider)
		{
			return (float)this;
		}
		string IConvertible.ToString(IFormatProvider provider)
		{
			return ToString();
		}
		object IConvertible.ToType(Type conversionType, IFormatProvider provider)
		{
			return Convert.ChangeType((double)this, conversionType);
		}

		#endregion
    }

	}

