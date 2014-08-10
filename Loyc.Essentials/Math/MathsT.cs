using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Math
{
	/// <summary>
	/// This class helps generic code to perform calculations on numbers of 
	/// unknown type, by providing access to various math interfaces.
	/// </summary>
	/// <typeparam name="T">A numeric type.</typeparam>
	/// <remarks>
	/// If a certain math interface is not available for a certain type, 
	/// then the corresponding field will be null. For example, 
	/// Maths&lt;int>.FloatMath is null because int is not floating-point.
	/// The <see cref="Traits"/>, <see cref="Math"/>, and related properties 
	/// will be null for non-numeric types.
	/// <para/>
	/// TODO: support non-builtin types!
	/// <para/>
	/// Generic code that uses Maths&lt;T> is slower than code that uses a
	/// generic parameter with a math constraint. That's because generic 
	/// struct parameters are early-bound and can be inlined, while calls 
	/// through an interface such as IMath&lt;T> are normal late-bound 
	/// interface calls and cannot be inlined. Compare the two examples 
	/// below.
	/// </remarks>
	/// <example>
	/// // Calculates the length of a vector with magnitude (x, y):
	/// // Slower version based on Maths&lt;T>. Example: Length(3.0,4.0)
	/// public T Length&lt;T>(T x, T y)
	/// {
	///     var m = Maths&lt;T>.Math;
	///     return m.Sqrt(m.Add(m.Square(x), m.Square(y)));
	/// }
	/// 
	/// // Calculates the length of a vector with magnitude (x, y):
	/// // Faster version based on Maths&lt;T>. Unfortunately, this version is 
	/// // inconvenient to call because the caller must specify which math 
	/// // provider to use. Example: Length&lt;double,MathD>(3.0,4.0)
	/// public T Length&lt;T,M>(T x, T y) where M:struct,IMath&lt;T>
	/// {
	///     var m = default(M);
	///     return m.Sqrt(m.Add(m.Square(x), m.Square(y)));
	/// }
	/// </example>
	public static class Maths<T>
	{
		public static readonly INumTraits<T> Traits = Get() as INumTraits<T>;
		public static readonly IMath<T> Math = Get() as IMath<T>;
		public static readonly ISignedMath<T> SignedMath = Get() as ISignedMath<T>;
		public static readonly IUIntMath<T> UIntMath = Get() as IUIntMath<T>;
		public static readonly IIntMath<T> IntMath = Get() as IIntMath<T>;
		public static readonly IRationalMath<T> RationalMath = Get() as IRationalMath<T>;
		public static readonly IFloatMath<T> FloatMath = Get() as IFloatMath<T>;
		public static readonly IComplexMath<T> ComplexMath = Get() as IComplexMath<T>;
		public static readonly INumConverter<T> NumConverter = Get() as INumConverter<T>;
		public static readonly IOrdered<T> Ordered = Get() as IOrdered<T>;
		public static readonly IIncrementer<T> Inrementer = Get() as IIncrementer<T>;
		public static readonly IBitwise<T> Bitwise = Get() as IBitwise<T>;
		public static readonly IBinaryMath<T> BinaryMath = Get() as IBinaryMath<T>;
		public static readonly IAdditionGroup<T> AdditionGroup = Get() as IAdditionGroup<T>;
		public static readonly ITrigonometry<T> Trigonometry = Get() as ITrigonometry<T>;
		public static readonly IHasRoot<T> HasRoot = Get() as IHasRoot<T>;
		public static readonly IExp<T> Exp = Get() as IExp<T>;
		public static readonly IMultiply<T> Multiply = Get() as IMultiply<T>;
		public static readonly IMultiplicationGroup<T> MultiplicationGroup = Get() as IMultiplicationGroup<T>;
		public static readonly IRing<T> Ring = Get() as IRing<T>;
		public static readonly IField<T> Field = Get() as IField<T>;

		private static object _math;
		private static object Get()
		{
			if (_math != null)
				return _math;
			
			Type t = typeof(T);
			object m;
			
			if (t == typeof(sbyte)) m = new MathI8();
			else if (t == typeof(byte)) m = new MathU8();
			else if (t == typeof(short)) m = new MathI16();
			else if (t == typeof(ushort)) m = new MathU16();
			else if (t == typeof(int)) m = new MathI();
			else if (t == typeof(uint)) m = new MathU();
			else if (t == typeof(long)) m = new MathL();
			else if (t == typeof(ulong)) m = new MathUL();
			else if (t == typeof(float)) m = new MathF();
			else if (t == typeof(double)) m = new MathD();
			else m = null; // TODO: search open assemblies for INumTraits<T> via reflection?

			return _math = m;
		}
	}
}
