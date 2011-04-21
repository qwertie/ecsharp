using System;

namespace Loyc.Math
{
	// Copyright (c) 2004, Rüdiger Klaehn
	// All rights reserved.
	// 
	// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
	// 
	//    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
	//    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
	//    * Neither the name of lambda computing nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
	//
	// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
	//
	#region Unsigned Wrapper
	public struct Unsigned<T, O>
		where O : IUIntMath<T>, new()
	{
		private static O o = new O();
		private readonly T value;
		public Unsigned(T a)
		{
			value = a;
		}
		public static explicit operator Unsigned<T, O>(ulong a)
		{
			return o.From(a);
		}
		public static implicit operator Unsigned<T, O>(T a)
		{
			return new Unsigned<T, O>(a);
		}
		public static implicit operator T(Unsigned<T, O> a)
		{
			return a.value;
		}
		public static Unsigned<T, O> operator +(Unsigned<T, O> a, Unsigned<T, O> b)
		{
			return o.Add(a, b);
		}
		public static Unsigned<T, O> operator -(Unsigned<T, O> a, Unsigned<T, O> b)
		{
			return o.Subtract(a, b);
		}
		public static Unsigned<T, O> operator *(Unsigned<T, O> a, Unsigned<T, O> b)
		{
			return o.Multiply(a, b);
		}
		public static Unsigned<T, O> operator /(Unsigned<T, O> a, Unsigned<T, O> b)
		{
			return o.Divide(a, b);
		}
		public static Unsigned<T, O> Zero
		{
			get { return o.Zero; }
		}
		public static Unsigned<T, O> One
		{
			get { return o.One; }
		}
		public static bool operator ==(Unsigned<T, O> a, Unsigned<T, O> b)
		{
			return o.Equals(a, b);
		}
		public static bool operator !=(Unsigned<T, O> a, Unsigned<T, O> b)
		{
			return !o.Equals(a, b);
		}
		public static bool operator <=(Unsigned<T, O> a, Unsigned<T, O> b)
		{
			return o.Compare(a, b) <= 0;
		}
		public static bool operator >=(Unsigned<T, O> a, Unsigned<T, O> b)
		{
			return o.Compare(a, b) >= 0;
		}
		public static bool operator <(Unsigned<T, O> a, Unsigned<T, O> b)
		{
			return o.Compare(a, b) < 0;
		}
		public static bool operator >(Unsigned<T, O> a, Unsigned<T, O> b)
		{
			return o.Compare(a, b) > 0;
		}
		public override bool Equals(object a)
		{
			if (a is T)
				return o.Equals(value, (T)a);
			else
				return false;
		}
		public override int GetHashCode()
		{
			return o.GetHashCode(value);
		}
	}
	#endregion

	#region Signed Wrapper
	public struct Signed<T, O>
		where O : ISignedMath<T>, new()
	{
		private static O o = new O();
		private readonly T value;
		public Signed(T a)
		{
			value = a;
		}
		public static explicit operator Signed<T, O>(ulong a)
		{
			return o.From(a);
		}
		public static explicit operator Signed<T, O>(long a)
		{
			return o.From(a);
		}
		public static implicit operator Signed<T, O>(T a)
		{
			return new Signed<T, O>(a);
		}
		public static implicit operator T(Signed<T, O> a)
		{
			return a.value;
		}
		public static Signed<T, O> operator +(Signed<T, O> a, Signed<T, O> b)
		{
			return o.Add(a, b);
		}
		public static Signed<T, O> operator -(Signed<T, O> a, Signed<T, O> b)
		{
			return o.Subtract(a, b);
		}
		public static Signed<T, O> operator -(Signed<T, O> a)
		{
			return o.Negate(a);
		}
		public static Signed<T, O> operator *(Signed<T, O> a, Signed<T, O> b)
		{
			return o.Multiply(a, b);
		}
		public static Signed<T, O> operator /(Signed<T, O> a, Signed<T, O> b)
		{
			return o.Divide(a, b);
		}
		public static Signed<T, O> Zero
		{
			get { return o.Zero; }
		}
		public static Signed<T, O> One
		{
			get { return o.One; }
		}
		public static bool operator ==(Signed<T, O> a, Signed<T, O> b)
		{
			return o.Equals(a, b);
		}
		public static bool operator !=(Signed<T, O> a, Signed<T, O> b)
		{
			return !o.Equals(a, b);
		}
		public static bool operator <=(Signed<T, O> a, Signed<T, O> b)
		{
			return o.Compare(a, b) <= 0;
		}
		public static bool operator >=(Signed<T, O> a, Signed<T, O> b)
		{
			return o.Compare(a, b) >= 0;
		}
		public static bool operator <(Signed<T, O> a, Signed<T, O> b)
		{
			return o.Compare(a, b) < 0;
		}
		public static bool operator >(Signed<T, O> a, Signed<T, O> b)
		{
			return o.Compare(a, b) > 0;
		}
		public override bool Equals(object a)
		{
			if (a is T)
				return o.Equals(value, (T)a);
			else
				return false;
		}
		public override int GetHashCode()
		{
			return o.GetHashCode(value);
		}
	}
	#endregion

	#region Rational Wrapper
	public struct Rational<T, O>
		where O : IRationalMath<T>, new()
	{
		private static O o = new O();
		private readonly T value;
		public Rational(T a)
		{
			value = a;
		}
		public static implicit operator Rational<T, O>(T a)
		{
			return new Rational<T, O>(a);
		}
		public static implicit operator T(Rational<T, O> a)
		{
			return a.value;
		}
		public static explicit operator Rational<T, O>(ulong a)
		{
			return o.From(a);
		}
		public static explicit operator Rational<T, O>(long a)
		{
			return o.From(a);
		}
		public static explicit operator Rational<T, O>(double a)
		{
			return o.From(a);
		}
		public static Rational<T, O> operator +(Rational<T, O> a, Rational<T, O> b)
		{
			return o.Add(a, b);
		}
		public static Rational<T, O> operator -(Rational<T, O> a, Rational<T, O> b)
		{
			return o.Subtract(a, b);
		}
		public static Rational<T, O> operator -(Rational<T, O> a)
		{
			return o.Negate(a);
		}
		public static Rational<T, O> operator *(Rational<T, O> a, Rational<T, O> b)
		{
			return o.Multiply(a, b);
		}
		public static Rational<T, O> operator /(Rational<T, O> a, Rational<T, O> b)
		{
			return o.Divide(a, b);
		}
		public Rational<T, O> Reciprocal
		{
			get { return o.Reciprocal(value); }
		}
		public static Rational<T, O> Zero
		{
			get { return o.Zero; }
		}
		public static Rational<T, O> One
		{
			get { return o.One; }
		}
		public static bool operator ==(Rational<T, O> a, Rational<T, O> b)
		{
			return o.Equals(a, b);
		}
		public static bool operator !=(Rational<T, O> a, Rational<T, O> b)
		{
			return !o.Equals(a, b);
		}
		public static bool operator <=(Rational<T, O> a, Rational<T, O> b)
		{
			return o.Compare(a, b) <= 0;
		}
		public static bool operator >=(Rational<T, O> a, Rational<T, O> b)
		{
			return o.Compare(a, b) >= 0;
		}
		public static bool operator <(Rational<T, O> a, Rational<T, O> b)
		{
			return o.Compare(a, b) < 0;
		}
		public static bool operator >(Rational<T, O> a, Rational<T, O> b)
		{
			return o.Compare(a, b) > 0;
		}
		public override bool Equals(object a)
		{
			if (a is T)
				return o.Equals(value, (T)a);
			else
				return false;
		}
		public override int GetHashCode()
		{
			return o.GetHashCode(value);
		}
	}
	#endregion
}
