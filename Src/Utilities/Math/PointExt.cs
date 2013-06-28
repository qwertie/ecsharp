namespace Loyc.Geometry
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using Loyc.Essentials;
	using Loyc.Math;

	/// <summary>Contains methods for manipulating points in generic code.</summary>
	/// <remarks>
	/// These methods typically modify the first point, so for your own safety,
	/// they are constrained to struct types. Some constraint is required: either 
	/// struct, or new(). For consistency all 2D points are structs, so the 
	/// constraint is struct in order to maximize performance. In any case,
	/// this class cannot support <see cref="IPoint{T}"/> itself, since an 
	/// interface can't support either of those constraints. Instead, there is a 
	/// separate set of extension methods for IPoints, in <see cref="IPointExt"/>.
	/// </remarks>
	public static class PointExt
	{
		/// <summary>Returns the sum of two vectors.</summary>
		public static Point Add<Point,T,M>(this M m, Point a, Point b)
			where Point : IPointBase<T>, INewPoint<Point, T>
			where M : IAdditionGroup<T>
		{
			return a.New(m.Add(a.X, b.X), m.Add(a.Y, b.Y));
		}
		/// <summary>Returns the difference between two points or vectors.</summary>
		public static IPoint<T> Subtract<T,M>(this M m, IPoint<T> a, IPoint<T> b) where M:IAdditionGroup<T>
		{
			return a.New(m.Sub(a.X, b.X), m.Sub(a.Y, b.Y));
		}
		/// <summary>Returns a point or vector multiplied by a scaling factor.</summary>
		public static IPoint<T> Multiply<T,M>(this M m, IPoint<T> a, T factor) where M:IMultiplicationGroup<T>
		{
			return a.New(m.Mul(a.X, factor), m.Mul(a.Y, factor));
		}
		/// <summary>Returns a point or vector divided by a scaling factor.</summary>
		public static IPoint<T> Divide<T,M>(this M m, IPoint<T> a, T factor) where M:IField<T>
		{
			return a.New(m.Div(a.X, factor), m.Div(a.Y, factor));
		}
		/// <summary>Returns a point or vector scaled up by a power of two.</summary>
		public static IPoint<T> ShiftLeft<T,M>(this M m, IPoint<T> a, int amount) where M:IField<T>
		{
			return a.New(m.Shl(a.X, amount), m.Shl(a.Y, amount));
		}
		/// <summary>Returns a point or vector scaled down by a power of two.</summary>
		public static IPoint<T> ShiftRight<T,M>(this M m, IPoint<T> a, int amount) where M:IField<T>
		{
			return a.New(m.Shr(a.X, amount), m.Shr(a.Y, amount));
		}
		/// <summary>Returns a point or vector by a factor, then divides by another factor.</summary>
		public static IPoint<T> MulDiv<T,M>(this M m, IPoint<T> a, T mulBy, T divBy) where M:IField<T>
		{
			return a.New(m.MulDiv(a.X, mulBy, divBy), m.MulDiv(a.Y, mulBy, divBy));
		}
		/// <summary>Gets the dot product of two vectors.</summary>
		public static T Dot<T,M>(this M m, IPoint<T> a, IPoint<T> b) where M:IField<T>
		{
			return m.Add(m.Mul(a.X, b.X), m.Mul(a.Y, b.Y));
		}
		/// <summary>Gets the cross product of two vectors.</summary>
		public static T Cross<T,M>(this M m, IPoint<T> a, IPoint<T> b) where M:IRing<T>
		{
			return m.Sub(m.Mul(a.X, b.Y), m.Mul(a.Y, b.X));
		}
		/// <summary>Returns a vector rotated 90 degrees.</summary>
		/// <remarks>
		/// Rotatation is clockwise if increasing Y goes downward, counter-
		/// clockwise if increasing Y goes upward.
		/// </remarks>
		public static IPoint<T> Rot90<T,M>(this M m, IPoint<T> p) where M:ISignedMath<T>
		{
			return p.New(m.Negate(p.Y), p.X);
		}
		/// <summary>Returns a vector with its direction reversed.</summary>
		public static IPoint<T> Negate<T,M>(this M m, IPoint<T> p) where M:ISignedMath<T>
		{
			return p.New(m.Negate(p.X), m.Negate(p.Y));
		}
		/// <summary>Gets the square of the length of a vector.</summary>
		public static T Quadrance<T,M>(this M m, IPoint<T> p) where M:IMath<T>
		{
			return m.Add(m.Square(p.X), m.Square(p.Y));
		}
		/// <summary>Gets the length of a vector.</summary>
		public static T Length<T,M>(this M m, IPoint<T> p) where M:IMath<T>
		{
			return m.Sqrt(Quadrance(m, p));
		}
		/// <summary>Gets the angle from 0 to 2*PI of the vector, where (1,0) has 
		/// angle 0 and (0,1) has angle PI/2.</summary>
		public static T Angle<T,M>(this M m, IPoint<T> p) where M:IFloatMath<T>
		{ 
			T angle = m.Atan2(p.Y, p.X);
			if (m.IsLess(angle, m.Zero))
				return m.Add(angle, m.From(2*Math.PI)); 
			return angle;
		}
	}
}
