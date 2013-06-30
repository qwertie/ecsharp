using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Math;

namespace Loyc.Geometry
{
	using System;
	using System.Diagnostics;

	public static partial class PointMath
	{
		/// <summary>Computes the "cross product" of a pair of vectors.</summary>
		/// <remarks>
		/// This is not a general cross product, as cross product is only a 3D concept,
		/// but this operator acts as though the two points were in the Z=0 plane and
		/// returns the Z coordinate of the cross product: b.X * a.Y - b.Y * a.X.
		/// This value is zero if the vectors are parallel; it is a.Length * b.Length 
		/// or -a.Length * b.Length if the vectors are perpendicular. One use of 
		/// cross product is to determine whether the angle between two lines is greater 
		/// or less than 180 degrees, corresponding to return values less or greater than 
		/// zero.
		/// </remarks>
		public static T Cross<T>(this Vector<T> a, Vector<T> b) where T:IConvertible, IEquatable<T>
		{
			var m = Maths<T>.SignedMath;
			return m.Sub(m.Mul(a.X, b.Y), m.Mul(a.Y, b.X));
		}

		/// <summary>Rotates a vector 90 degrees.</summary>
		/// <remarks>
		/// Rotatation is clockwise if increasing Y goes downward, counter-
		/// clockwise if increasing Y goes upward. If the vector represents the 
		/// direction of a line, the result also represents the coefficients 
		/// (a,b) of the implicit line equation aX + bY + c = 0.
		/// </remarks>
		public static Vector<T> Rot90<T>(this Vector<T> self) where T:IConvertible, IEquatable<T>
		{
			var m = Maths<T>.SignedMath;
			return new Vector<T>(m.Negate(self.Y), self.X);
		}

		/// <summary>Gets the square of the length of the vector.</summary>
		public static T Quadrance<T>(this Vector<T> self) where T:IConvertible, IEquatable<T> 
		{
			var m = Maths<T>.SignedMath;
			return m.Add(m.Square(self.X), m.Square(self.Y));
		}
		/// <summary>Gets the length of the vector.</summary>
		public static T Length<T>(this Vector<T> self) where T:IConvertible, IEquatable<T>
		{
			var m = Maths<T>.SignedMath;
			return m.Sqrt(Quadrance(self));
		}
		
		/// <summary>Gets the angle from 0 to 2*PI of the vector, where (1,0) has 
		/// angle 0 and (0,1) has angle PI/2.</summary>
		public static double Angle<T>(this Vector<T> self) where T:IConvertible, IEquatable<T>
		{ 
			double angle = Math.Atan2(self.Y.ToDouble(null), self.X.ToDouble(null));
			if (angle < 0)
				return angle + 2*Math.PI; 
			return angle;
		}

		public static bool Normalize<T>(this Vector<T> self) where T:IConvertible, IEquatable<T>
		{
			var m = Maths<T>.SignedMath;
			Debug.Assert(!m.IsInteger);
			T q = Quadrance(self);
			if (q.Equals(m.Zero))
				return false;
			if (!q.Equals(m.One)) {
				T len = m.Sqrt(q);
				self.X = m.Div(self.X, len);
				self.Y = m.Div(self.Y, len);
			}
			return true;
		}
		
		public static Vector<T> Normalized<T>(this Vector<T> self) where T:IConvertible, IEquatable<T>
		{
			Normalize(self);
			return self;
		}

		public static Vector<T> MulDiv<T>(this Vector<T> self, T mul, T div) where T:IConvertible, IEquatable<T>
		{
			var m = Maths<T>.SignedMath;
			return new Vector<T>(m.MulDiv(self.X, mul, div), m.MulDiv(self.Y, mul, div));
		}

		/// <summary>Returns the vector from A to B (seg.B - seg.A)</summary>
		public static Vector<T> Vector<T>(this LineSegment<T> seg) where T:IConvertible, IEquatable<T>
		{
			return seg.B - seg.A;
		}

		/// <summary>Returns the midpoint between A and B ((a + b) / 2)</summary>
		public static Point<T> HalfwayTo<T>(this Point<T> a, Point<T> b) where T:IConvertible, IEquatable<T>
		{
			return (a + (Vector<T>)b) >> 1;
		}
		
		/// <summary>Converts a Loyc point to BCL type.</summary>
		public static System.Drawing.Point ToBCL(this Point<int> p)
		{
			return new System.Drawing.Point(p.X, p.Y);
		}
		/// <summary>Converts a Loyc vector to BCL type.</summary>
		public static System.Drawing.Point ToBCL(this Vector<int> p)
		{
			return new System.Drawing.Point(p.X, p.Y);
		}
		/// <summary>Converts a Loyc point to BCL type.</summary>
		public static System.Drawing.PointF ToBCL(this Point<float> p)
		{
			return new System.Drawing.PointF(p.X, p.Y);
		}
		/// <summary>Converts a Loyc vector to BCL type.</summary>
		public static System.Drawing.PointF ToBCL(this Vector<float> p)
		{
			return new System.Drawing.PointF(p.X, p.Y);
		}
		/// <summary>Converts a Loyc point to BCL type.</summary>
		public static System.Windows.Point ToBCL(this Point<double> p)
		{
			return new System.Windows.Point(p.X, p.Y);
		}
		/// <summary>Converts a Loyc vector to BCL type.</summary>
		public static System.Windows.Vector ToBCL(this Vector<double> p)
		{
			return new System.Windows.Vector(p.X, p.Y);
		}
		/// <summary>Converts a BCL point to a Loyc point.</summary>
		public static Point<int> ToLoyc(this System.Drawing.Point p)
		{
			return new Point<int>(p.X, p.Y);
		}
		/// <summary>Converts a BCL point to a Loyc point.</summary>
		public static Point<float> ToLoyc(this System.Drawing.PointF p)
		{
			return new Point<float>(p.X, p.Y);
		}
		/// <summary>Converts a BCL point to a Loyc point.</summary>
		public static Point<double> ToLoyc(this System.Windows.Point p)
		{
			return new Point<double>(p.X, p.Y);
		}
		/// <summary>Converts a BCL Vector to a Loyc point.</summary>
		public static Vector<double> ToLoyc(this System.Windows.Vector p)
		{
			return new Vector<double>(p.X, p.Y);
		}
		/// <summary>Converts a BCL point to a Loyc vector.</summary>
		public static Vector<int> ToLoycVector(this System.Drawing.Point p)
		{
			return new Vector<int>(p.X, p.Y);
		}
		/// <summary>Converts a BCL point to a Loyc vector.</summary>
		public static Vector<float> ToLoycVector(this System.Drawing.PointF p)
		{
			return new Vector<float>(p.X, p.Y);
		}
		/// <summary>Converts a BCL point to a Loyc vector.</summary>
		public static Vector<double> ToLoycVector(this System.Windows.Point p)
		{
			return new Vector<double>(p.X, p.Y);
		}
		/// <summary>Constructs a <see cref="LineSegment{T}"/> from two points.</summary>
		public static LineSegment<T> To<T>(this Point<T> a, Point<T> b) where T : IConvertible, IEquatable<T>
		{
			return new LineSegment<T>(a, b);
		}
	}
}
