using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

namespace Loyc.Math
{
	/// <summary>
	/// Provides methods for converting common numeric types to another numeric type "T".
	/// </summary>
	/// <typeparam name="T">A numeric type</typeparam>
	/// <remarks>Methods for converting type T to standard numeric types would be 
	/// redundant, because standard numeric types already implement IConvertible for 
	/// this purpose. To use IConvertible in generic code, add IConvertible as a 
	/// type constraint on the numeric type.</remarks>
	public interface INumConverter<T>
	{
		T From(uint t);
		T From(int t);
		T From(ulong t);
		T From(long t);
		T From(double t);

		T Clip(uint t);
		T Clip(int t);
		T Clip(ulong t);
		T Clip(long t);
		T Clip(double t);
	}

	public interface IOrdered<T> : IComparer<T>, IEqualityComparer<T>
	{
		bool IsLess(T a, T b);
		bool IsLessOrEqual(T a, T b);
		T Abs(T a);
		T Max(T a, T b);
		T Min(T a, T b);
	}
	public static partial class MathExtensions
	{
		public static bool IsGreater<T,C>(this C c, T a, T b) where C : IOrdered<T>, new()
		{
			return c.IsLess(b, a);
		}
		public static bool IsGreaterOrEqual<T, C>(this C c, T a, T b) where C : IOrdered<T>, new()
		{
			return c.IsLessOrEqual(b, a);
		}
		public static T InRange<T,C>(this C c, T value, T min, T max) where C : IOrdered<T>, new()
		{
			if (c.IsLess(value, min))
				return min;
			if (c.IsLess(max, value))
				return max;
			return value;
		}
		public static bool IsInRange<T, C>(this C c, T value, T min, T max) where C : IOrdered<T>, new()
		{
			return c.IsLessOrEqual(min, value) && c.IsLessOrEqual(value, max);
		}
	}

	public interface IZeroProvider<T>
	{
		/// <summary>Returns the "zero" or additive identity of this type.</summary>
		T Zero { get; }
	}
	public interface IOneProvider<T>
	{
		/// <summary>Returns the "one" or identity value of this type.</summary>
		T One { get; }
	}

	/// <summary>This interface provides information about a numeric type T.</summary>
	/// <typeparam name="T">A numeric type</typeparam>
	public interface INumTraits<T> : IZeroProvider<T>, IOneProvider<T>
	{
		/// <summary>Minimum value of this type above negative infinity.</summary>
		T MinValue { get; }
		/// <summary>Maximum value of this type below infinity.</summary>
		T MaxValue { get; }
		/// <summary>Smallest representable positive value of T (1 for integer types).</summary>
		T Epsilon { get; }
		/// <summary>Returns positive infinity, or MaxValue for types that cannot represent infinity.</summary>
		T PositiveInfinity { get; }
		/// <summary>Returns negative infinity, or throws NotSupportedException if T is unsigned.</summary>
		/// <exception cref="NotSupportedException">T is unsigned.</exception>
		T NegativeInfinity { get; }
		/// <summary>Not-a-number or null representation for this type.</summary>
		/// <exception cref="NotSupportedException">There is no null or NaN value for type T.</exception>
		T NaN { get; }
		/// <summary>Returns true if the given value is infinite.</summary>
		/// <remarks>Types that do not have an infinity value always return false 
		/// from this method.</remarks>
		bool IsInfinity(T value);
		/// <summary>Returns true if the given value is not a number (can only be true for floats).</summary>
		bool IsNaN(T value);
		/// <summary>Returns true if T can represent negative values.</summary>
		bool IsSigned { get; }
		/// <summary>Returns true if T is floating-point, meaning that it can 
		/// represent very large and very small numbers, despite possibly limited 
		/// precision. Returns false for fixed-point and integer-rational types.</summary>
		bool IsFloatingPoint { get; }
		/// <summary>Returns true if the type represents only whole numbers.</summary>
		bool IsInteger { get; }
		/// <summary>Returns true for "normal" numbers, false for ones that aren't 
		/// necessarily comparable (notably complex numbers).</summary>
		bool IsOrdered { get; }
		/// <summary>Returns the normal maximum number of significant (mantissa) 
		/// bits for this type (not counting the sign bit), or int.MaxValue for 
		/// unlimited-size types.</summary>
		int SignificantBits { get; }
		/// <summary>Returns the maximum power-of-two-minus-one that can be 
		/// represented by this type, e.g. for Int32 it's 31, and for UInt32 it's 
		/// 32.</summary>
		int MaxIntPowerOf2 { get; }
		/// <summary>Returns the maximum integer that this type can represent.</summary>
		/// <remarks>If the maximum integer exceeds ulong.MaxValue, this returns 
		/// ulong.MaxValue.</remarks>
		ulong MaxInt { get; }
		/// <summary>Returns the minimum integer that this type can represent.</summary>
		/// <remarks>If the minimum is less than long.MinValue, this returns 
		/// long.MinValue.</remarks>
		long MinInt { get; }
	}

	/// <summary>Provides increment, decrement, and next/previous-representable-
	/// value operations.</summary>
	/// <typeparam name="T">A numeric type.</typeparam>
	/// <remarks>Implementations may or may not detect overflow.</remarks>
	public interface IInrementer<T>
	{
		/// <summary>Returns a + 1.</summary>
		T AddOne(T a);
		/// <summary>Returns a - 1.</summary>
		T SubOne(T a);
		/// <summary>Returns the next representable number higher than a.</summary>
		T NextHigher(T a);
		/// <summary>Returns the next representable number lower than a.</summary>
		T NextLower(T a);
	}

	/// <summary>Provides the standard set of bitwise operators.</summary>
	/// <typeparam name="T">An integer or bit array type.</typeparam>
	public interface IBitwise<T> : IZeroProvider<T>, IOneProvider<T>
	{
		T And(T a, T b);
		T Or(T a, T b);
		T Xor(T a, T b);
		T Not(T a);
	}

	/// <summary>Provides additional bit-oriented integer operations.</summary>
	/// <typeparam name="T">An integer or integer-based types.</typeparam>
	public interface IBinaryMath<T> : IBitwise<T>
	{
		/// <summary>Shifts 'a' left by the specified number of bits.</summary>
		/// <remarks>A shift amount A negative shift amount produces undefined results</remarks>
		T Shl(T a, int amount);
		/// <summary>Shifts 'a' right by the specified number of bits.</summary>
		T Shr(T a, int amount);
		/// <summary>Returns the number of '1' bits in 'a'.</summary>
		int CountOnes(T a);
		/// <summary>
		/// Returns the floor of the base-2 logarithm of x. e.g. 1024 -> 10, 1000 -> 9
		/// </summary><remarks>
		/// The return value is int.MinValue for an input of zero (for which the 
		/// logarithm is technically undefined.)
		/// </remarks>
		int Log2Floor(T a);
	}

	/// <summary>
	/// This defines a Group with the operation +, the neutral element Zero,
	/// and an operation - that is defined in terms of the inverse. A Negate 
	/// operation is not provided so that this interface makes more sense for 
	/// use with unsigned types.
	/// 
	/// Axioms that have to be satisified by the operations:
	/// Commutativity of addition: Add(a,b)=Add(b,a) for all a,b in T
	/// Associativity of addition: Add(Add(a,b),c)=Add(a,Add(b,c))
	/// Inverse of addition: Add(a,Negate(a))==Zero
	/// Subtraction: Subtract(a,b)==Add(a,Negate(b))
	/// Neutral element: Add(Zero,a)==a for all a in T
	/// </summary>
	public interface IAdditionGroup<T> : IZeroProvider<T>
	{
		T Add(T a, T b);
		T Add(T a, T b, T c);
		T Sub(T a, T b);
		// T Zero { get; }
	}

	/// <summary>Provides trigonometry operations.</summary>
	public interface ITrigonometry<T>
	{
		T Asin(T a);
		T Acos(T a);
		T Atan(T a);
		T Atan2(T a, T b);
		T Sin(T a);
		T Cos(T a);
		T Tan(T a);
	}

	/// <summary>Provides the Sqrt operation and its inverse, Square.</summary>
	public interface IHasRoot<T>
	{
		T Sqrt(T a);
		T Square(T a);
	}

	/// <summary>Provides power, logarithm, raise-e-to-exponent (Exp) and logarithm-of-e (Log) operations.</summary>
	public interface IExp<T>
	{
		T Exp(T a);
		T Pow(T @base, T exponent);
		T Ln(T a);
		T Log(T a, T @base);
	}

	/// <summary>Provides the multiplication operation and the multiplicative identity, one.</summary>
	public interface IMultiply<T> : IOneProvider<T>
	{
		T Mul(T a, T b);
		// T One { get; }
	}

	/// <summary>
	/// This defines a Group with the operation *, the neutral element One,
	/// the inverse Inverse and an operation / that is defined in terms of the inverse.
	/// </summary>
	/// <remarks>
	/// Axioms that have to be satisified by the operations:
	/// Commutativity of multiplication: Multiply(a,b)=Multiply(b,a) for all a,b in T
	/// Associativity of multiplication: Multiply(Multiply(a,b),c)=Multiply(a,Multiply(b,c))
	/// Inverse of multiplication: Multiply(a,Inverse(a))==One for all a in T
	/// Divison: Divide(a,b)==Multiply(a,Inverse(b)) for all a in T
	/// Neutral element: Multiply(One,a)==a for all a in T
	/// <br/><br/>
	/// ShiftLeft and ShiftRight operations are commonly thought of as binary 
	/// operations, but some algorithms need to multiply numbers by powers of two 
	/// and want to do so efficiently, while still supporting floating-point types. 
	/// Therefore it makes sense to offer ShiftLeft ("multiply by a power of two")
	/// and ShiftRight ("divide by a power of two") operators as part of the 
	/// multiple/divide interface, not just <see cref="IBinaryMath{T}"/>. Even 
	/// floating-point types can support these two operations efficiently by 
	/// directly modifying the exponent part of the floating-point representation.
	/// </remarks>
	public interface IMultiplicationGroup<T> : IMultiply<T>
	{
		T Div(T a, T b);
		T Shl(T a, int amount);
		T Shr(T a, int amount);
		T MulDiv(T a, T mulBy, T divBy);
	}

	/// <summary>
	/// This defines a Ring with the operations +,*        
	/// 
	/// Axioms that have to be satisified by the operations:
	/// The group axioms for +
	/// Associativity of *: a * (b*c) = (a*b) * c
	/// Neutral element of *: Multiply(One,a)==a for all a in T
	/// Distributivity: 
	///     a * (b+c) = (a*b) + (a*c) 
	///     (a+b) * c = (a*c) + (b*c) 
	/// </summary>
	public interface IRing<T> :
		IAdditionGroup<T>,
		IMultiply<T>
	{ }

	/// <summary>This defines a Field with the operations +,-,*,/</summary>
	/// <remarks>
	/// Axioms that have to be satisified by the operations:
	/// The group axioms for +
	/// The group axioms for *
	/// Associativity: a * (b*c) = (a*b) * c
	/// Distributivity: 
	///     a * (b+c) = (a*b) + (a*c) 
	///     (a+b) * c = (a*c) + (b*c) 
	/// </remarks>
	public interface IField<T> :
		IRing<T>,
		IMultiplicationGroup<T>
	{ }

	/// <summary>
	/// Provides operations available on all system numeric types (int, uint, double,
	/// etc.); see also <see cref="ISignedMath{T}"/>, <see cref="IUIntMath{T}"/>, 
	/// <see cref="IIntMath{T}"/> and <see cref="IFloatMath{T}"/>.
	/// </summary>
	/// <typeparam name="T">An integer, fixed-point, rational or floating-point numeric type</typeparam>
	/// <remarks>
	/// List of operations and properties: From, CompareTo, Equals, IsLess, 
	/// IsLessOrEqual, Abs, Min, Max, MinValue, MaxValue, Epsilon, PositiveInfinity,
	/// NegativeInfinity, IsSigned, Increment, Decrement, NextHigher, NextLower, 
	/// Add, Subtract, Zero, One, Multiply, Divide, ShiftLeft, ShiftRight, Sqrt, 
	/// Square.
	/// <para/>
	/// Also available as extension methods: IsGreater, IsGreaterOrEqual
	/// <para/>
	/// It is commonly thought that integer types do not support square root. 
	/// Obviously, the accuracy of Sqrt on integers is limited, but Sqrt(uint) and 
	/// Sqrt(ulong) are still provided in Loyc.Essentials.
	/// </remarks>
	public interface IMath<T> :
		INumTraits<T>,          // MinValue MaxValue Epsilon PositiveInfinity NegativeInfinity IsSigned...
		INumConverter<T>,       // From
		IOrdered<T>,            // CompareTo, Equals, IsLess, IsLessOrEqual, Abs, Min, Max
		IInrementer<T>,         // ++ -- NextHigher NextLower
		IField<T>,              // + - * / << >> Zero One
		IHasRoot<T>             // Sqrt Square
	{
	}

	/// <summary>
	/// Provides operations available on all signed numeric types (int, double,
	/// etc.); see also <see cref="IUIntMath{T}"/>, <see cref="IIntMath{T}"/> and 
	/// <see cref="IFloatMath{T}"/>.
	public interface ISignedMath<T> : IMath<T>
	{
		T Negate(T a);
	}

	/// <summary>
	/// Provides operations available on all unsigned integer types (byte, uint,
	/// etc.); see also <see cref="IMath{T}"/>, <see cref="IIntMath{T}"/>, and
	/// <see cref="IFloatMath{T}"/>.
	public interface IUIntMath<T> : IMath<T>, IBinaryMath<T>
	{
	}

	/// <summary>
	/// Provides operations available on all unsigned integer types (byte, uint,
	/// etc.); see also <see cref="IMath{T}"/>, <see cref="IIntMath{T}"/>, and
	/// <see cref="IFloatMath{T}"/>.
	public interface IIntMath<T> : ISignedMath<T>, IBinaryMath<T>
	{
	}

	/// <summary>
	/// Use this interface for floating-point, fixed-point, and rational types.
	/// Rational types support reciprocal and negation.
	/// </summary>
	public interface IRationalMath<T> : ISignedMath<T>
	{
		T Reciprocal(T a);
	}

	/// <summary>Provides operations available on floating-point types 
	/// (float and double), including trigonometry and exponentiation.
	/// </summary>
	/// <typeparam name="T">A floating-point type</typeparam>
	/// <remarks>Algorithms that support both floating and fixed-point should 
	/// require <see cref="IRationalMath{T}"/> instead.</remarks>
	public interface IFloatMath<T> : IRationalMath<T>,
		ITrigonometry<T>,
		IExp<T>
	{
	}

	/// <summary>
	/// Use this interface for types such as complex numbers that satisfy 
	/// the field axioms but do not have a natural order.
	/// complex numbers of course do support IHasRoot.
	/// </summary>
	public interface IComplexMath<T> :
		INumTraits<T>,
		INumConverter<T>,
		IField<T>,
		IHasRoot<T>,
		IExp<T>
	{
		T ConvertFrom(double x);
	}
}
