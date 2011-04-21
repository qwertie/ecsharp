
namespace Loyc.Math
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
	using System.Diagnostics;

	
    public partial struct FPI8
    {
        public static FPI8 Prescaled(Int32 n) { FPI8 r = new FPI8(); r.N = n; return r; }
        public static readonly FPI8 Zero = new FPI8();
		public static readonly FPI8 One = new FPI8(1);
		public static readonly FPI8 Epsilon = Prescaled(1);
        public static readonly FPI8 MaxValue = Prescaled(Int32.MaxValue);
        public static readonly FPI8 MinValue = Prescaled(Int32.MinValue);
        public const Int32 MaxInt = Int32.MaxValue >> 8;
        public const Int32 MinInt = Int32.MinValue >> 8;
        public const double MaxDouble = Int32.MaxValue / (double)(1 << 8);
        public const double MinDouble = Int32.MinValue / (double)(1 << 8);
		public const Int32 Mask = (1 << 8) - 1;

		public static explicit operator FPI8(int value) { return new FPI8(value); }
		public static implicit operator FPI8(short value) { return new FPI8(value); }
		
		public static explicit operator FPI8(uint value) { return new FPI8(value); }
		public static implicit operator FPI8(ushort value) { return new FPI8(value); }

		public static explicit operator FPI8(long value) { return new FPI8(value); }
		public static explicit operator FPI8(ulong value) { return new FPI8(value); }
		public static explicit operator FPI8(float value) { return new FPI8(value); }
		public static explicit operator FPI8(double value) { return new FPI8(value); }
		public static explicit operator int(FPI8 value) { return (int)(value.N >> 8); }
		public static explicit operator long(FPI8 value) { return value.N >> 8; }
		public static explicit operator uint(FPI8 value) { return (uint)(value.N >> 8); }
		public static explicit operator ulong(FPI8 value) { return (ulong)(value.N >> 8); }
		public static explicit operator float(FPI8 value) { return (float)value.N * (1.0f / (1 << 8)); }
		public static explicit operator double(FPI8 value) { return (double)value.N * (1.0 / (1 << 8)); }

		public Int32 N;

		private void Overflow()
		{
 			throw new OverflowException();
		}
		public FPI8 CheckedCast(int num)
		{
			if (num < MinInt || num > MaxInt )
				Overflow();
			return Prescaled(num << 8);
		}
		public FPI8 CheckedCast(uint num)
		{
			if (num > MaxInt)
				Overflow();
			return Prescaled((int)num << 8);
		}
		public FPI8 CheckedCast(long num)
		{
			if (num < MinInt || num > MaxInt)
				Overflow();
			return Prescaled((int)num << 8);
		}
		public FPI8 CheckedCast(ulong num)
		{
			if (num > MaxInt)
				Overflow();
			return Prescaled((int)num << 8);
		}
		public FPI8 CheckedCast(double num)
		{
			if (!(num >= MinDouble && num <= MaxDouble))
				Overflow();
			return FastCast(num);
		}
		public FPI8 FastCast(int num)
		{
			return Prescaled(num << 8);
		}
		public FPI8 FastCast(uint num)
		{
			return Prescaled((int)num << 8);
		}
		public FPI8 FastCast(double num)
		{
			return Prescaled((int)Math.Round(MathEx.ShiftLeft(num, 8)));
		}

		public FPI8(int num)
		{
			N = num << 8;
			if (num < MinInt)
				N = Int32.MinValue;
			if (num > MaxInt)
				N = Int32.MaxValue;
		}
		public FPI8(uint num)
		{
			N = (int)num << 8;
			if (num > (uint)MaxInt)
				N = Int32.MaxValue;
		}
        public FPI8(long num)
		{
			N = (int)num << 8;
			if (num < MinInt)
				N = Int32.MinValue;
			if (num > MaxInt)
				N = Int32.MaxValue;
		}
		public FPI8(ulong num)
		{
			N = (int)num << 8;
			if (num > MaxInt)
				N = Int32.MaxValue;
		}
        public FPI8(double num)
		{
			N = (int)Math.Round(MathEx.ShiftLeft(num, 8));
			if (num <= MinDouble)
				N = Int32.MinValue;
			if (num >= MaxDouble)
				N = Int32.MaxValue;
		}

		public static FPI8 operator +(FPI8 a, int b) { a.N += b << 8; return a; }
        public static FPI8 operator -(FPI8 a, int b) { a.N -= b << 8; return a; }
        public static FPI8 operator *(FPI8 a, int b) { a.N *= b; return a; }
        public static FPI8 operator /(FPI8 a, int b) { a.N /= b; return a; }
        public static FPI8 operator %(FPI8 a, int b) { a.N %= b << 8; return a; }
		public static FPI8 operator +(FPI8 a, FPI8 b) { a.N += b.N; return a; }
        public static FPI8 operator -(FPI8 a, FPI8 b) { a.N -= b.N; return a; }
        
		public static FPI8 operator *(FPI8 a, FPI8 b) { return Prescaled((int)((long)a.N * (long)b.N >> 8)); }
        public static FPI8 operator /(FPI8 a, FPI8 b) { return Prescaled((int)((long)(a.N << 8) / b.N)); }
        public static FPI8 operator %(FPI8 a, FPI8 b) { a.N %= b.N; return a; }
		public static FPI8 operator <<(FPI8 a, int b) { a.N <<= b; return a; }
		public static FPI8 operator >>(FPI8 a, int b) { a.N >>= b; return a; }
		public static bool operator ==(FPI8 a, FPI8 b) { return a.N == b.N; }
		public static bool operator !=(FPI8 a, FPI8 b) { return a.N != b.N; }
		public static bool operator >=(FPI8 a, FPI8 b) { return a.N >= b.N; }
		public static bool operator <=(FPI8 a, FPI8 b) { return a.N <= b.N; }
		public static bool operator >(FPI8 a, FPI8 b) { return a.N > b.N; }
		public static bool operator <(FPI8 a, FPI8 b) { return a.N < b.N; }
		
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
    }

	
    public partial struct FPI16
    {
        public static FPI16 Prescaled(Int32 n) { FPI16 r = new FPI16(); r.N = n; return r; }
        public static readonly FPI16 Zero = new FPI16();
		public static readonly FPI16 One = new FPI16(1);
		public static readonly FPI16 Epsilon = Prescaled(1);
        public static readonly FPI16 MaxValue = Prescaled(Int32.MaxValue);
        public static readonly FPI16 MinValue = Prescaled(Int32.MinValue);
        public const Int32 MaxInt = Int32.MaxValue >> 16;
        public const Int32 MinInt = Int32.MinValue >> 16;
        public const double MaxDouble = Int32.MaxValue / (double)(1 << 16);
        public const double MinDouble = Int32.MinValue / (double)(1 << 16);
		public const Int32 Mask = (1 << 16) - 1;

		public static explicit operator FPI16(int value) { return new FPI16(value); }
		public static implicit operator FPI16(short value) { return new FPI16(value); }
		
		public static explicit operator FPI16(uint value) { return new FPI16(value); }
		public static implicit operator FPI16(ushort value) { return new FPI16(value); }

		public static explicit operator FPI16(long value) { return new FPI16(value); }
		public static explicit operator FPI16(ulong value) { return new FPI16(value); }
		public static explicit operator FPI16(float value) { return new FPI16(value); }
		public static explicit operator FPI16(double value) { return new FPI16(value); }
		public static explicit operator int(FPI16 value) { return (int)(value.N >> 16); }
		public static explicit operator long(FPI16 value) { return value.N >> 16; }
		public static explicit operator uint(FPI16 value) { return (uint)(value.N >> 16); }
		public static explicit operator ulong(FPI16 value) { return (ulong)(value.N >> 16); }
		public static explicit operator float(FPI16 value) { return (float)value.N * (1.0f / (1 << 16)); }
		public static explicit operator double(FPI16 value) { return (double)value.N * (1.0 / (1 << 16)); }

		public Int32 N;

		private void Overflow()
		{
 			throw new OverflowException();
		}
		public FPI16 CheckedCast(int num)
		{
			if (num < MinInt || num > MaxInt )
				Overflow();
			return Prescaled(num << 16);
		}
		public FPI16 CheckedCast(uint num)
		{
			if (num > MaxInt)
				Overflow();
			return Prescaled((int)num << 16);
		}
		public FPI16 CheckedCast(long num)
		{
			if (num < MinInt || num > MaxInt)
				Overflow();
			return Prescaled((int)num << 16);
		}
		public FPI16 CheckedCast(ulong num)
		{
			if (num > MaxInt)
				Overflow();
			return Prescaled((int)num << 16);
		}
		public FPI16 CheckedCast(double num)
		{
			if (!(num >= MinDouble && num <= MaxDouble))
				Overflow();
			return FastCast(num);
		}
		public FPI16 FastCast(int num)
		{
			return Prescaled(num << 16);
		}
		public FPI16 FastCast(uint num)
		{
			return Prescaled((int)num << 16);
		}
		public FPI16 FastCast(double num)
		{
			return Prescaled((int)Math.Round(MathEx.ShiftLeft(num, 16)));
		}

		public FPI16(int num)
		{
			N = num << 16;
			if (num < MinInt)
				N = Int32.MinValue;
			if (num > MaxInt)
				N = Int32.MaxValue;
		}
		public FPI16(uint num)
		{
			N = (int)num << 16;
			if (num > (uint)MaxInt)
				N = Int32.MaxValue;
		}
        public FPI16(long num)
		{
			N = (int)num << 16;
			if (num < MinInt)
				N = Int32.MinValue;
			if (num > MaxInt)
				N = Int32.MaxValue;
		}
		public FPI16(ulong num)
		{
			N = (int)num << 16;
			if (num > MaxInt)
				N = Int32.MaxValue;
		}
        public FPI16(double num)
		{
			N = (int)Math.Round(MathEx.ShiftLeft(num, 16));
			if (num <= MinDouble)
				N = Int32.MinValue;
			if (num >= MaxDouble)
				N = Int32.MaxValue;
		}

		public static FPI16 operator +(FPI16 a, int b) { a.N += b << 16; return a; }
        public static FPI16 operator -(FPI16 a, int b) { a.N -= b << 16; return a; }
        public static FPI16 operator *(FPI16 a, int b) { a.N *= b; return a; }
        public static FPI16 operator /(FPI16 a, int b) { a.N /= b; return a; }
        public static FPI16 operator %(FPI16 a, int b) { a.N %= b << 16; return a; }
		public static FPI16 operator +(FPI16 a, FPI16 b) { a.N += b.N; return a; }
        public static FPI16 operator -(FPI16 a, FPI16 b) { a.N -= b.N; return a; }
        
		public static FPI16 operator *(FPI16 a, FPI16 b) { return Prescaled((int)((long)a.N * (long)b.N >> 16)); }
        public static FPI16 operator /(FPI16 a, FPI16 b) { return Prescaled((int)((long)(a.N << 16) / b.N)); }
        public static FPI16 operator %(FPI16 a, FPI16 b) { a.N %= b.N; return a; }
		public static FPI16 operator <<(FPI16 a, int b) { a.N <<= b; return a; }
		public static FPI16 operator >>(FPI16 a, int b) { a.N >>= b; return a; }
		public static bool operator ==(FPI16 a, FPI16 b) { return a.N == b.N; }
		public static bool operator !=(FPI16 a, FPI16 b) { return a.N != b.N; }
		public static bool operator >=(FPI16 a, FPI16 b) { return a.N >= b.N; }
		public static bool operator <=(FPI16 a, FPI16 b) { return a.N <= b.N; }
		public static bool operator >(FPI16 a, FPI16 b) { return a.N > b.N; }
		public static bool operator <(FPI16 a, FPI16 b) { return a.N < b.N; }
		
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
    }

	
    public partial struct FPI24
    {
        public static FPI24 Prescaled(Int32 n) { FPI24 r = new FPI24(); r.N = n; return r; }
        public static readonly FPI24 Zero = new FPI24();
		public static readonly FPI24 One = new FPI24(1);
		public static readonly FPI24 Epsilon = Prescaled(1);
        public static readonly FPI24 MaxValue = Prescaled(Int32.MaxValue);
        public static readonly FPI24 MinValue = Prescaled(Int32.MinValue);
        public const Int32 MaxInt = Int32.MaxValue >> 24;
        public const Int32 MinInt = Int32.MinValue >> 24;
        public const double MaxDouble = Int32.MaxValue / (double)(1 << 24);
        public const double MinDouble = Int32.MinValue / (double)(1 << 24);
		public const Int32 Mask = (1 << 24) - 1;

		public static explicit operator FPI24(int value) { return new FPI24(value); }
		public static implicit operator FPI24(sbyte value) { return new FPI24(value); }
		
		public static explicit operator FPI24(uint value) { return new FPI24(value); }
		public static implicit operator FPI24(byte value) { return new FPI24(value); }

		public static explicit operator FPI24(long value) { return new FPI24(value); }
		public static explicit operator FPI24(ulong value) { return new FPI24(value); }
		public static explicit operator FPI24(float value) { return new FPI24(value); }
		public static explicit operator FPI24(double value) { return new FPI24(value); }
		public static explicit operator int(FPI24 value) { return (int)(value.N >> 24); }
		public static explicit operator long(FPI24 value) { return value.N >> 24; }
		public static explicit operator uint(FPI24 value) { return (uint)(value.N >> 24); }
		public static explicit operator ulong(FPI24 value) { return (ulong)(value.N >> 24); }
		public static explicit operator float(FPI24 value) { return (float)value.N * (1.0f / (1 << 24)); }
		public static explicit operator double(FPI24 value) { return (double)value.N * (1.0 / (1 << 24)); }

		public Int32 N;

		private void Overflow()
		{
 			throw new OverflowException();
		}
		public FPI24 CheckedCast(int num)
		{
			if (num < MinInt || num > MaxInt )
				Overflow();
			return Prescaled(num << 24);
		}
		public FPI24 CheckedCast(uint num)
		{
			if (num > MaxInt)
				Overflow();
			return Prescaled((int)num << 24);
		}
		public FPI24 CheckedCast(long num)
		{
			if (num < MinInt || num > MaxInt)
				Overflow();
			return Prescaled((int)num << 24);
		}
		public FPI24 CheckedCast(ulong num)
		{
			if (num > MaxInt)
				Overflow();
			return Prescaled((int)num << 24);
		}
		public FPI24 CheckedCast(double num)
		{
			if (!(num >= MinDouble && num <= MaxDouble))
				Overflow();
			return FastCast(num);
		}
		public FPI24 FastCast(int num)
		{
			return Prescaled(num << 24);
		}
		public FPI24 FastCast(uint num)
		{
			return Prescaled((int)num << 24);
		}
		public FPI24 FastCast(double num)
		{
			return Prescaled((int)Math.Round(MathEx.ShiftLeft(num, 24)));
		}

		public FPI24(int num)
		{
			N = num << 24;
			if (num < MinInt)
				N = Int32.MinValue;
			if (num > MaxInt)
				N = Int32.MaxValue;
		}
		public FPI24(uint num)
		{
			N = (int)num << 24;
			if (num > (uint)MaxInt)
				N = Int32.MaxValue;
		}
        public FPI24(long num)
		{
			N = (int)num << 24;
			if (num < MinInt)
				N = Int32.MinValue;
			if (num > MaxInt)
				N = Int32.MaxValue;
		}
		public FPI24(ulong num)
		{
			N = (int)num << 24;
			if (num > MaxInt)
				N = Int32.MaxValue;
		}
        public FPI24(double num)
		{
			N = (int)Math.Round(MathEx.ShiftLeft(num, 24));
			if (num <= MinDouble)
				N = Int32.MinValue;
			if (num >= MaxDouble)
				N = Int32.MaxValue;
		}

		public static FPI24 operator +(FPI24 a, int b) { a.N += b << 24; return a; }
        public static FPI24 operator -(FPI24 a, int b) { a.N -= b << 24; return a; }
        public static FPI24 operator *(FPI24 a, int b) { a.N *= b; return a; }
        public static FPI24 operator /(FPI24 a, int b) { a.N /= b; return a; }
        public static FPI24 operator %(FPI24 a, int b) { a.N %= b << 24; return a; }
		public static FPI24 operator +(FPI24 a, FPI24 b) { a.N += b.N; return a; }
        public static FPI24 operator -(FPI24 a, FPI24 b) { a.N -= b.N; return a; }
        
		public static FPI24 operator *(FPI24 a, FPI24 b) { return Prescaled((int)((long)a.N * (long)b.N >> 24)); }
        public static FPI24 operator /(FPI24 a, FPI24 b) { return Prescaled((int)((long)(a.N << 24) / b.N)); }
        public static FPI24 operator %(FPI24 a, FPI24 b) { a.N %= b.N; return a; }
		public static FPI24 operator <<(FPI24 a, int b) { a.N <<= b; return a; }
		public static FPI24 operator >>(FPI24 a, int b) { a.N >>= b; return a; }
		public static bool operator ==(FPI24 a, FPI24 b) { return a.N == b.N; }
		public static bool operator !=(FPI24 a, FPI24 b) { return a.N != b.N; }
		public static bool operator >=(FPI24 a, FPI24 b) { return a.N >= b.N; }
		public static bool operator <=(FPI24 a, FPI24 b) { return a.N <= b.N; }
		public static bool operator >(FPI24 a, FPI24 b) { return a.N > b.N; }
		public static bool operator <(FPI24 a, FPI24 b) { return a.N < b.N; }
		
		public override bool Equals(object obj)
		{
			return obj is FPI24 && ((FPI24)obj).N == N;
		}
		public override int GetHashCode()
		{
			return N.GetHashCode();
		}
		public override string ToString()
		{
			return ((double)this).ToString();
		}
    }

	
    public partial struct FPL16
    {
        public static FPL16 Prescaled(Int64 n) { FPL16 r = new FPL16(); r.N = n; return r; }
        public static readonly FPL16 Zero = new FPL16();
		public static readonly FPL16 One = new FPL16(1);
		public static readonly FPL16 Epsilon = Prescaled(1);
        public static readonly FPL16 MaxValue = Prescaled(Int64.MaxValue);
        public static readonly FPL16 MinValue = Prescaled(Int64.MinValue);
        public const Int64 MaxInt = Int64.MaxValue >> 16;
        public const Int64 MinInt = Int64.MinValue >> 16;
        public const double MaxDouble = Int64.MaxValue / (double)(1 << 16);
        public const double MinDouble = Int64.MinValue / (double)(1 << 16);
		public const Int64 Mask = (1 << 16) - 1;

		public static implicit operator FPL16(int value) { return new FPL16(value); }
		
		public static implicit operator FPL16(uint value) { return new FPL16(value); }

		public static explicit operator FPL16(long value) { return new FPL16(value); }
		public static explicit operator FPL16(ulong value) { return new FPL16(value); }
		public static explicit operator FPL16(float value) { return new FPL16(value); }
		public static explicit operator FPL16(double value) { return new FPL16(value); }
		public static explicit operator int(FPL16 value) { return (int)(value.N >> 16); }
		public static explicit operator long(FPL16 value) { return value.N >> 16; }
		public static explicit operator uint(FPL16 value) { return (uint)(value.N >> 16); }
		public static explicit operator ulong(FPL16 value) { return (ulong)(value.N >> 16); }
		public static explicit operator float(FPL16 value) { return (float)value.N * (1.0f / (1 << 16)); }
		public static explicit operator double(FPL16 value) { return (double)value.N * (1.0 / (1 << 16)); }

		public Int64 N;

		private void Overflow()
		{
 			throw new OverflowException();
		}
		public FPL16 CheckedCast(long num)
		{
			if (num < MinInt || num > MaxInt)
				Overflow();
			return Prescaled((int)num << 16);
		}
		public FPL16 CheckedCast(ulong num)
		{
			if (num > MaxInt)
				Overflow();
			return Prescaled((int)num << 16);
		}
		public FPL16 CheckedCast(double num)
		{
			if (!(num >= MinDouble && num <= MaxDouble))
				Overflow();
			return FastCast(num);
		}
		public FPL16 FastCast(int num)
		{
			return Prescaled(num << 16);
		}
		public FPL16 FastCast(uint num)
		{
			return Prescaled((int)num << 16);
		}
		public FPL16 FastCast(double num)
		{
			return Prescaled((int)Math.Round(MathEx.ShiftLeft(num, 16)));
		}

		public FPL16(int num)
		{
			N = num << 16;
		}
		public FPL16(uint num)
		{
			N = (int)num << 16;
		}
        public FPL16(long num)
		{
			N = (int)num << 16;
			if (num < MinInt)
				N = Int64.MinValue;
			if (num > MaxInt)
				N = Int64.MaxValue;
		}
		public FPL16(ulong num)
		{
			N = (int)num << 16;
			if (num > MaxInt)
				N = Int64.MaxValue;
		}
        public FPL16(double num)
		{
			N = (int)Math.Round(MathEx.ShiftLeft(num, 16));
			if (num <= MinDouble)
				N = Int64.MinValue;
			if (num >= MaxDouble)
				N = Int64.MaxValue;
		}

		public static FPL16 operator +(FPL16 a, int b) { a.N += b << 16; return a; }
        public static FPL16 operator -(FPL16 a, int b) { a.N -= b << 16; return a; }
        public static FPL16 operator *(FPL16 a, int b) { a.N *= b; return a; }
        public static FPL16 operator /(FPL16 a, int b) { a.N /= b; return a; }
        public static FPL16 operator %(FPL16 a, int b) { a.N %= b << 16; return a; }
		public static FPL16 operator +(FPL16 a, FPL16 b) { a.N += b.N; return a; }
        public static FPL16 operator -(FPL16 a, FPL16 b) { a.N -= b.N; return a; }
        
		public static FPL16 operator *(FPL16 a, FPL16 b)
		{
			var afrac = a.N & Mask;
			var bfrac = b.N & Mask;
			var whole = (FPL16)((Int64)a * (Int64)b);
			whole.N += afrac * bfrac >> 16;
			return whole;
		}
        public static FPL16 operator /(FPL16 a, FPL16 b)
		{
			long whole = a.N / b.N;
			long remainder = a.N % b.N;
			remainder = (remainder << 16) / b.N;
			Debug.Assert(remainder < (1 << 16));
			a.N = (whole << 16) + remainder;
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
    }

	
    public partial struct FPL32
    {
        public static FPL32 Prescaled(Int64 n) { FPL32 r = new FPL32(); r.N = n; return r; }
        public static readonly FPL32 Zero = new FPL32();
		public static readonly FPL32 One = new FPL32(1);
		public static readonly FPL32 Epsilon = Prescaled(1);
        public static readonly FPL32 MaxValue = Prescaled(Int64.MaxValue);
        public static readonly FPL32 MinValue = Prescaled(Int64.MinValue);
        public const Int64 MaxInt = Int64.MaxValue >> 32;
        public const Int64 MinInt = Int64.MinValue >> 32;
        public const double MaxDouble = Int64.MaxValue / (double)(1 << 32);
        public const double MinDouble = Int64.MinValue / (double)(1 << 32);
		public const Int64 Mask = (1 << 32) - 1;

		public static implicit operator FPL32(int value) { return new FPL32(value); }
		
		public static explicit operator FPL32(uint value) { return new FPL32(value); }
		public static implicit operator FPL32(ushort value) { return new FPL32(value); }

		public static explicit operator FPL32(long value) { return new FPL32(value); }
		public static explicit operator FPL32(ulong value) { return new FPL32(value); }
		public static explicit operator FPL32(float value) { return new FPL32(value); }
		public static explicit operator FPL32(double value) { return new FPL32(value); }
		public static explicit operator int(FPL32 value) { return (int)(value.N >> 32); }
		public static explicit operator long(FPL32 value) { return value.N >> 32; }
		public static explicit operator uint(FPL32 value) { return (uint)(value.N >> 32); }
		public static explicit operator ulong(FPL32 value) { return (ulong)(value.N >> 32); }
		public static explicit operator float(FPL32 value) { return (float)value.N * (1.0f / (1 << 32)); }
		public static explicit operator double(FPL32 value) { return (double)value.N * (1.0 / (1 << 32)); }

		public Int64 N;

		private void Overflow()
		{
 			throw new OverflowException();
		}
		public FPL32 CheckedCast(long num)
		{
			if (num < MinInt || num > MaxInt)
				Overflow();
			return Prescaled((int)num << 32);
		}
		public FPL32 CheckedCast(ulong num)
		{
			if (num > MaxInt)
				Overflow();
			return Prescaled((int)num << 32);
		}
		public FPL32 CheckedCast(double num)
		{
			if (!(num >= MinDouble && num <= MaxDouble))
				Overflow();
			return FastCast(num);
		}
		public FPL32 FastCast(int num)
		{
			return Prescaled(num << 32);
		}
		public FPL32 FastCast(uint num)
		{
			return Prescaled((int)num << 32);
		}
		public FPL32 FastCast(double num)
		{
			return Prescaled((int)Math.Round(MathEx.ShiftLeft(num, 32)));
		}

		public FPL32(int num)
		{
			N = num << 32;
		}
		public FPL32(uint num)
		{
			N = (int)num << 32;
			if (num > (uint)MaxInt)
				N = Int64.MaxValue;
		}
        public FPL32(long num)
		{
			N = (int)num << 32;
			if (num < MinInt)
				N = Int64.MinValue;
			if (num > MaxInt)
				N = Int64.MaxValue;
		}
		public FPL32(ulong num)
		{
			N = (int)num << 32;
			if (num > MaxInt)
				N = Int64.MaxValue;
		}
        public FPL32(double num)
		{
			N = (int)Math.Round(MathEx.ShiftLeft(num, 32));
			if (num <= MinDouble)
				N = Int64.MinValue;
			if (num >= MaxDouble)
				N = Int64.MaxValue;
		}

		public static FPL32 operator +(FPL32 a, int b) { a.N += b << 32; return a; }
        public static FPL32 operator -(FPL32 a, int b) { a.N -= b << 32; return a; }
        public static FPL32 operator *(FPL32 a, int b) { a.N *= b; return a; }
        public static FPL32 operator /(FPL32 a, int b) { a.N /= b; return a; }
        public static FPL32 operator %(FPL32 a, int b) { a.N %= b << 32; return a; }
		public static FPL32 operator +(FPL32 a, FPL32 b) { a.N += b.N; return a; }
        public static FPL32 operator -(FPL32 a, FPL32 b) { a.N -= b.N; return a; }
        
		public static FPL32 operator *(FPL32 a, FPL32 b)
		{
			var afrac = a.N & Mask;
			var bfrac = b.N & Mask;
			var whole = (FPL32)((Int64)a * (Int64)b);
			whole.N += afrac * bfrac >> 32;
			return whole;
		}
        public static FPL32 operator /(FPL32 a, FPL32 b)
		{
			long whole = a.N / b.N;
			long remainder = a.N % b.N;
			remainder = (remainder << 32) / b.N;
			Debug.Assert(remainder < (1 << 32));
			a.N = (whole << 32) + remainder;
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
    }

	
    public partial struct FPL48
    {
        public static FPL48 Prescaled(Int64 n) { FPL48 r = new FPL48(); r.N = n; return r; }
        public static readonly FPL48 Zero = new FPL48();
		public static readonly FPL48 One = new FPL48(1);
		public static readonly FPL48 Epsilon = Prescaled(1);
        public static readonly FPL48 MaxValue = Prescaled(Int64.MaxValue);
        public static readonly FPL48 MinValue = Prescaled(Int64.MinValue);
        public const Int64 MaxInt = Int64.MaxValue >> 48;
        public const Int64 MinInt = Int64.MinValue >> 48;
        public const double MaxDouble = Int64.MaxValue / (double)(1 << 48);
        public const double MinDouble = Int64.MinValue / (double)(1 << 48);
		public const Int64 Mask = (1 << 48) - 1;

		public static explicit operator FPL48(int value) { return new FPL48(value); }
		public static implicit operator FPL48(short value) { return new FPL48(value); }
		
		public static explicit operator FPL48(uint value) { return new FPL48(value); }
		public static implicit operator FPL48(ushort value) { return new FPL48(value); }

		public static explicit operator FPL48(long value) { return new FPL48(value); }
		public static explicit operator FPL48(ulong value) { return new FPL48(value); }
		public static explicit operator FPL48(float value) { return new FPL48(value); }
		public static explicit operator FPL48(double value) { return new FPL48(value); }
		public static explicit operator int(FPL48 value) { return (int)(value.N >> 48); }
		public static explicit operator long(FPL48 value) { return value.N >> 48; }
		public static explicit operator uint(FPL48 value) { return (uint)(value.N >> 48); }
		public static explicit operator ulong(FPL48 value) { return (ulong)(value.N >> 48); }
		public static explicit operator float(FPL48 value) { return (float)value.N * (1.0f / (1 << 48)); }
		public static explicit operator double(FPL48 value) { return (double)value.N * (1.0 / (1 << 48)); }

		public Int64 N;

		private void Overflow()
		{
 			throw new OverflowException();
		}
		public FPL48 CheckedCast(long num)
		{
			if (num < MinInt || num > MaxInt)
				Overflow();
			return Prescaled((int)num << 48);
		}
		public FPL48 CheckedCast(ulong num)
		{
			if (num > MaxInt)
				Overflow();
			return Prescaled((int)num << 48);
		}
		public FPL48 CheckedCast(double num)
		{
			if (!(num >= MinDouble && num <= MaxDouble))
				Overflow();
			return FastCast(num);
		}
		public FPL48 FastCast(int num)
		{
			return Prescaled(num << 48);
		}
		public FPL48 FastCast(uint num)
		{
			return Prescaled((int)num << 48);
		}
		public FPL48 FastCast(double num)
		{
			return Prescaled((int)Math.Round(MathEx.ShiftLeft(num, 48)));
		}

		public FPL48(int num)
		{
			N = num << 48;
			if (num < MinInt)
				N = Int64.MinValue;
			if (num > MaxInt)
				N = Int64.MaxValue;
		}
		public FPL48(uint num)
		{
			N = (int)num << 48;
			if (num > (uint)MaxInt)
				N = Int64.MaxValue;
		}
        public FPL48(long num)
		{
			N = (int)num << 48;
			if (num < MinInt)
				N = Int64.MinValue;
			if (num > MaxInt)
				N = Int64.MaxValue;
		}
		public FPL48(ulong num)
		{
			N = (int)num << 48;
			if (num > MaxInt)
				N = Int64.MaxValue;
		}
        public FPL48(double num)
		{
			N = (int)Math.Round(MathEx.ShiftLeft(num, 48));
			if (num <= MinDouble)
				N = Int64.MinValue;
			if (num >= MaxDouble)
				N = Int64.MaxValue;
		}

		public static FPL48 operator +(FPL48 a, int b) { a.N += b << 48; return a; }
        public static FPL48 operator -(FPL48 a, int b) { a.N -= b << 48; return a; }
        public static FPL48 operator *(FPL48 a, int b) { a.N *= b; return a; }
        public static FPL48 operator /(FPL48 a, int b) { a.N /= b; return a; }
        public static FPL48 operator %(FPL48 a, int b) { a.N %= b << 48; return a; }
		public static FPL48 operator +(FPL48 a, FPL48 b) { a.N += b.N; return a; }
        public static FPL48 operator -(FPL48 a, FPL48 b) { a.N -= b.N; return a; }
        
		public static FPL48 operator *(FPL48 a, FPL48 b)
		{
			var afrac = a.N & Mask;
			var bfrac = b.N & Mask;
			var whole = (FPL48)((Int64)a * (Int64)b);
			whole.N += afrac * bfrac >> 48;
			return whole;
		}
        public static FPL48 operator /(FPL48 a, FPL48 b)
		{
			long whole = a.N / b.N;
			long remainder = a.N % b.N;
			remainder = (remainder << 48) / b.N;
			Debug.Assert(remainder < (1 << 48));
			a.N = (whole << 48) + remainder;
			return a;
			// TODO: test negative numbers: 7 / -2.5, -7 / 2.5, -7 / -2.5
		}
        public static FPL48 operator %(FPL48 a, FPL48 b) { a.N %= b.N; return a; }
		public static FPL48 operator <<(FPL48 a, int b) { a.N <<= b; return a; }
		public static FPL48 operator >>(FPL48 a, int b) { a.N >>= b; return a; }
		public static bool operator ==(FPL48 a, FPL48 b) { return a.N == b.N; }
		public static bool operator !=(FPL48 a, FPL48 b) { return a.N != b.N; }
		public static bool operator >=(FPL48 a, FPL48 b) { return a.N >= b.N; }
		public static bool operator <=(FPL48 a, FPL48 b) { return a.N <= b.N; }
		public static bool operator >(FPL48 a, FPL48 b) { return a.N > b.N; }
		public static bool operator <(FPL48 a, FPL48 b) { return a.N < b.N; }
		
		public override bool Equals(object obj)
		{
			return obj is FPL48 && ((FPL48)obj).N == N;
		}
		public override int GetHashCode()
		{
			return N.GetHashCode();
		}
		public override string ToString()
		{
			return ((double)this).ToString();
		}
    }

	}

