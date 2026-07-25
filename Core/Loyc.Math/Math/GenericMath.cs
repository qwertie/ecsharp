using System;
using System.Collections.Generic;
#if NET7_0_OR_GREATER
using System.Numerics;
#endif

namespace Loyc.Math
{
	/// <summary>Locates a fallback math provider for a numeric type that
	/// <see cref="Maths{T}"/> does not have a hand-written provider for.</summary>
	/// <remarks>
	/// On .NET 7 and above this uses the "generic math" static abstract interfaces
	/// (<c>System.Numerics.INumber&lt;T></c>), which lets <see cref="Maths{T}"/>
	/// support <c>Int128</c>, <c>UInt128</c>, <c>Half</c>, <c>decimal</c>,
	/// <c>BigInteger</c>, <c>nint</c> and user-defined numeric types. On earlier
	/// targets there is no such mechanism and this always returns null, which
	/// preserves the historical behaviour exactly.
	/// </remarks>
	internal static class GenericMath
	{
		/// <summary>Returns a math provider for <paramref name="type"/>, or null if
		/// none is available.</summary>
		public static object? TryCreateFor(Type type)
		{
			#if NET7_0_OR_GREATER
				// Prefer the binary-integer provider: it can implement Shl/Shr with real
				// shift operators instead of emulating them with repeated multiplication.
				if (SelfImplements(type, typeof(IBinaryInteger<>)))
					return Activator.CreateInstance(typeof(BinaryIntegerMath<>).MakeGenericType(type));
				if (SelfImplements(type, typeof(INumber<>)))
					return Activator.CreateInstance(typeof(NumberMath<>).MakeGenericType(type));
			#endif
			return null;
		}

		#if NET7_0_OR_GREATER
		/// <summary>Returns true if <paramref name="type"/> implements
		/// <c>openInterface&lt;type></c>, i.e. the curiously-recurring "TSelf" form.</summary>
		/// <remarks>
		/// This deliberately scans <see cref="Type.GetInterfaces"/> rather than calling
		/// <c>typeof(IBinaryInteger&lt;>).MakeGenericType(type).IsAssignableFrom(type)</c>.
		/// The latter THROWS <see cref="ArgumentException"/> for any type that does not
		/// satisfy the interface's self-referential TSelf constraint -- for example
		/// <c>MakeGenericType(typeof(Half))</c> on <c>IBinaryInteger&lt;></c> throws,
		/// because Half does not implement IShiftOperators. Constructing the generic type
		/// is only safe once we already know the constraint holds.
		/// </remarks>
		static bool SelfImplements(Type type, Type openInterface)
		{
			foreach (var i in type.GetInterfaces())
				if (i.IsGenericType && i.GetGenericTypeDefinition() == openInterface
					&& i.GetGenericArguments()[0] == type)
					return true;
			return false;
		}
		#endif
	}

	#if NET7_0_OR_GREATER

	/// <summary>Implements the arithmetic subset of the Loyc math interfaces for any
	/// type that implements <see cref="System.Numerics.INumber{T}"/> (.NET 7+).</summary>
	/// <remarks>
	/// This does not implement the full <see cref="IMath{T}"/>: members of
	/// <see cref="INumTraits{T}"/> such as SignificantBits, MaxInt and MinInt are not
	/// derivable from <c>INumber&lt;T></c> alone. It covers <see cref="IField{T}"/>
	/// (and therefore IRing, IAdditionGroup, IMultiply, IMultiplicationGroup) plus
	/// <see cref="IOrdered{T}"/>, which is what most generic algorithms need.
	/// </remarks>
	public struct NumberMath<T> : IField<T>, IOrdered<T> where T : INumber<T>
	{
		public static readonly NumberMath<T> Value = new NumberMath<T>();

		public T Zero => T.Zero;
		public T One => T.One;

		public T Add(T a, T b) => a + b;
		public T Add(T a, T b, T c) => a + b + c;
		public T Sub(T a, T b) => a - b;
		public T Mul(T a, T b) => a * b;
		public T Div(T a, T b) => a / b;
		public T MulDiv(T a, T mulBy, T divBy) => a * mulBy / divBy;

		/// <summary>Multiplies by 2 to the power of <paramref name="amount"/>.</summary>
		/// <remarks>INumber&lt;T> has no shift operators, so this is emulated with
		/// repeated multiplication/division and costs O(|amount|). Types that implement
		/// IBinaryInteger&lt;T> get <see cref="BinaryIntegerMath{T}"/> instead, which
		/// uses real shifts.</remarks>
		public T Shl(T a, int amount)
		{
			T two = T.One + T.One;
			for (; amount > 0; amount--) a *= two;
			for (; amount < 0; amount++) a /= two;
			return a;
		}
		public T Shr(T a, int amount) => Shl(a, -amount);

		public int Compare(T? a, T? b) => Comparer<T>.Default.Compare(a, b);
		public new bool Equals(T? a, T? b) => EqualityComparer<T>.Default.Equals(a, b);
		public int GetHashCode(T a) => a!.GetHashCode();
		public bool IsLess(T a, T b) => a < b;
		public bool IsLessOrEqual(T a, T b) => a <= b;
		public T Abs(T a) => T.Abs(a);
		public T Max(T a, T b) => T.Max(a, b);
		public T Min(T a, T b) => T.Min(a, b);
	}

	/// <summary>Implements the arithmetic subset of the Loyc math interfaces for any
	/// type that implements <see cref="System.Numerics.IBinaryInteger{T}"/> (.NET 7+),
	/// using real shift operators and hardware bit-counting.</summary>
	public struct BinaryIntegerMath<T> : IField<T>, IOrdered<T>, IBinaryMath<T>
		where T : IBinaryInteger<T>, IMinMaxValue<T>
	{
		public static readonly BinaryIntegerMath<T> Value = new BinaryIntegerMath<T>();

		public T Zero => T.Zero;
		public T One => T.One;
		public T MinValue => T.MinValue;
		public T MaxValue => T.MaxValue;

		public T Add(T a, T b) => a + b;
		public T Add(T a, T b, T c) => a + b + c;
		public T Sub(T a, T b) => a - b;
		public T Mul(T a, T b) => a * b;
		public T Div(T a, T b) => a / b;
		public T MulDiv(T a, T mulBy, T divBy) => a * mulBy / divBy;

		public T Shl(T a, int amount) => amount >= 0 ? a << amount : a >> -amount;
		public T Shr(T a, int amount) => amount >= 0 ? a >> amount : a << -amount;

		public int Compare(T? a, T? b) => Comparer<T>.Default.Compare(a, b);
		public new bool Equals(T? a, T? b) => EqualityComparer<T>.Default.Equals(a, b);
		public int GetHashCode(T a) => a!.GetHashCode();
		public bool IsLess(T a, T b) => a < b;
		public bool IsLessOrEqual(T a, T b) => a <= b;
		public T Abs(T a) => T.Abs(a);
		public T Max(T a, T b) => T.Max(a, b);
		public T Min(T a, T b) => T.Min(a, b);

		// IBitwise<T>
		public T And(T a, T b) => a & b;
		public T Or(T a, T b) => a | b;
		public T Xor(T a, T b) => a ^ b;
		public T Not(T a) => ~a;

		// IBinaryMath<T>
		public int CountOnes(T a) => int.CreateTruncating(T.PopCount(a));
		/// <summary>Returns the floor of the base-2 logarithm, or -1 if a is zero
		/// or negative. This matches what the hand-written providers in Maths.cs
		/// actually do (they delegate to <see cref="MathEx"/>), rather than the
		/// int.MinValue mentioned in the IBinaryMath documentation.</summary>
		public int Log2Floor(T a) => T.IsNegative(a) ? -1 : (int)a.GetShortestBitLength() - 1;
	}

	#endif
}
