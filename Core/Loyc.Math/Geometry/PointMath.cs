using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Math;

namespace Loyc.Geometry
{
	using System;
	using System.Diagnostics;
	using System.Drawing;

	/// <summary>Math and extension methods for <see cref="Point{T}"/>.</summary>
	/// <remarks>This contains many methods that should be considered "core" 
	/// functionality, including many methods such as Add(), Sub() and Mul() that 
	/// would ordinarily be implemented as operators (+ - *) if it weren't for 
	/// limitations of C#. These operations are implemented as extension methods for 
	/// performance reasons, because as a generic class, <see cref="Point{T}"/> 
	/// cannot do arithmetic quickly. The extension methods provide operations that 
	/// are optimized for specific data types (currently int, float and double).</remarks>
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
		public static System.Drawing.Point AsBCL(this Point<int> p)
		{
			return new System.Drawing.Point(p.X, p.Y);
		}
		/// <summary>Converts a Loyc vector to BCL type.</summary>
		public static System.Drawing.Point AsBCL(this Vector<int> p)
		{
			return new System.Drawing.Point(p.X, p.Y);
		}
		/// <summary>Converts a Loyc point to BCL type.</summary>
		public static System.Drawing.PointF AsBCL(this Point<float> p)
		{
			return new System.Drawing.PointF(p.X, p.Y);
		}
		/// <summary>Converts a Loyc vector to BCL type.</summary>
		public static System.Drawing.PointF AsBCL(this Vector<float> p)
		{
			return new System.Drawing.PointF(p.X, p.Y);
		}
		#if DotNet3 || DotNet4
		/// <summary>Converts a Loyc point to BCL type.</summary>
		public static System.Windows.Point AsBCL(this Point<double> p)
		{
			return new System.Windows.Point(p.X, p.Y);
		}
		/// <summary>Converts a Loyc vector to BCL type.</summary>
		public static System.Windows.Vector AsBCL(this Vector<double> p)
		{
			return new System.Windows.Vector(p.X, p.Y);
		}
		#endif
		/// <summary>Converts a BCL point to a Loyc point.</summary>
		public static Point<int> AsLoyc(this System.Drawing.Point p)
		{
			return new Point<int>(p.X, p.Y);
		}
		/// <summary>Converts a BCL point to a Loyc point.</summary>
		public static Point<float> AsLoyc(this System.Drawing.PointF p)
		{
			return new Point<float>(p.X, p.Y);
		}
		#if DotNet3 || DotNet4
		/// <summary>Converts a BCL point to a Loyc point.</summary>
		public static Point<double> AsLoyc(this System.Windows.Point p)
		{
			return new Point<double>(p.X, p.Y);
		}
		/// <summary>Converts a BCL Vector to a Loyc point.</summary>
		public static Vector<double> AsLoyc(this System.Windows.Vector p)
		{
			return new Vector<double>(p.X, p.Y);
		}
		#endif
		/// <summary>Converts a BCL point to a Loyc vector.</summary>
		public static Vector<int> AsLoycVector(this System.Drawing.Point p)
		{
			return new Vector<int>(p.X, p.Y);
		}
		/// <summary>Converts a BCL point to a Loyc vector.</summary>
		public static Vector<float> AsLoycVector(this System.Drawing.PointF p)
		{
			return new Vector<float>(p.X, p.Y);
		}
		#if DotNet3 || DotNet4
		/// <summary>Converts a BCL point to a Loyc vector.</summary>
		public static Vector<double> AsLoycVector(this System.Windows.Point p)
		{
			return new Vector<double>(p.X, p.Y);
		}
		#endif
		/// <summary>Constructs a <see cref="LineSegment{T}"/> from two points.</summary>
		public static LineSegment<T> To<T>(this Point<T> a, Point<T> b) where T : IConvertible, IEquatable<T>
		{
			return new LineSegment<T>(a, b);
		}
		/// <summary>Gets the absolute value of vector's individual components.</summary>
		public static Vector<T> Abs<T>(this Vector<T> v) where T : IComparable<T>, IConvertible, IEquatable<T>
		{
			var m = Maths<T>.SignedMath;
			T x = v.X, y = v.Y, zero = m.Zero;
			if (x.CompareTo(zero) < 0)
				x = m.Negate(x);
			if (y.CompareTo(zero) < 0)
				y = m.Negate(y);
			return new Vector<T>(x, y);
		}
		/// <summary>Gets the absolute value of vector's individual components.</summary>
		public static Vector3<T> Abs<T>(this Vector3<T> v) where T : IComparable<T>, IConvertible, IEquatable<T>
		{
			var m = Maths<T>.SignedMath;
			T x = v.X, y = v.Y, z = v.Z, zero = m.Zero;
			if (x.CompareTo(zero) < 0)
				x = m.Negate(x);
			if (y.CompareTo(zero) < 0)
				y = m.Negate(y);
			if (z.CompareTo(zero) < 0)
				z = m.Negate(z);
			return new Vector3<T>(x, y, z);
		}

		[ThreadStatic]
		static PointF[] _onePointF;
		static PointF[] AsArray(PointF pt) { 
			if (_onePointF == null) _onePointF = new PointF[1];
			_onePointF[0] = pt;
			return _onePointF;
		}

		#if DotNet3 || DotNet4
		public static PointF Transform(this System.Drawing.Drawing2D.Matrix matrix, PointF point)
		{
			var a = AsArray(point);
			matrix.TransformPoints(a);
			return a[0];
		}
		public static Point<float> Transform(this System.Drawing.Drawing2D.Matrix matrix, Point<float> point)
		{
			var a = AsArray(point.AsBCL());
			matrix.TransformPoints(a);
			return a[0].AsLoyc();
		}
		public static Vector<float> Transform(this System.Drawing.Drawing2D.Matrix matrix, Vector<float> vec)
		{
			var a = AsArray(vec.AsBCL());
			matrix.TransformVectors(a);
			return a[0].AsLoycVector();
		}
		#endif

		// In cases where we don't have optimized extension methods, do not require 
		// users to manually fall back on overloaded operators.

		public static Point<T>  Add<T>(this Point<T> a, Vector<T> b)  where T:IEquatable<T>, IConvertible { return a + b; }
		public static Point<T>  Add<T>(this Vector<T> a, Point<T> b)  where T:IEquatable<T>, IConvertible { return a + b; }
		public static Vector<T> Add<T>(this Vector<T> a, Vector<T> b) where T:IEquatable<T>, IConvertible { return a + b; }
		public static Vector<T> Sub<T>(this Point<T> a, Point<T> b)   where T:IEquatable<T>, IConvertible { return a - b; }
		public static Vector<T> Sub<T>(this Vector<T> a, Vector<T> b) where T:IEquatable<T>, IConvertible { return a - b; }
		public static Point<T>  Sub<T>(this Point<T> a, Vector<T> b)  where T:IEquatable<T>, IConvertible { return a - b; }
		public static Vector<T> Mul<T>(this Vector<T> p, T factor)    where T:IEquatable<T>, IConvertible { return p * factor; }
		public static Point<T>  Mul<T>(this Point<T> p, T factor)     where T:IEquatable<T>, IConvertible { return p * factor; }
		public static Vector<T> Div<T>(this Vector<T> p, T factor)    where T:IEquatable<T>, IConvertible { return p / factor; }
		public static Point<T>  Div<T>(this Point<T> p, T factor)     where T:IEquatable<T>, IConvertible { return p / factor; }
		public static Vector<T> Shl<T>(this Vector<T> p, int amt)     where T:IEquatable<T>, IConvertible { return p << amt; }
		public static Vector<T> Shr<T>(this Vector<T> p, int amt)     where T:IEquatable<T>, IConvertible { return p >> amt; }
		public static Point<T>  Shl<T>(this Point<T> p, int amt)      where T:IEquatable<T>, IConvertible { return p << amt; }
		public static Point<T>  Shr<T>(this Point<T> p, int amt)      where T:IEquatable<T>, IConvertible { return p >> amt; }

		public static Vector3<T> Add<T>(this Vector3<T> a, Vector3<T> b) where T:IEquatable<T>, IConvertible { return a + b; }
		public static Point3<T>  Add<T>(this Point3<T> a, Vector3<T> b)  where T:IEquatable<T>, IConvertible { return a + b; }
		public static Point3<T>  Add<T>(this Vector3<T> a, Point3<T> b)  where T:IEquatable<T>, IConvertible { return a + b; }
		public static Vector3<T> Sub<T>(this Vector3<T> a, Vector3<T> b) where T:IEquatable<T>, IConvertible { return a - b; }
		public static Vector3<T> Sub<T>(this Point3<T> a, Point3<T> b)   where T:IEquatable<T>, IConvertible { return a - b; }
		public static Point3<T>  Sub<T>(this Point3<T> a, Vector3<T> b)  where T:IEquatable<T>, IConvertible { return a - b; }
		public static Vector3<T> Mul<T>(this Vector3<T> p, T factor)     where T:IEquatable<T>, IConvertible { return p * factor; }
		public static Point3<T>  Mul<T>(this Point3<T> p, T factor)      where T:IEquatable<T>, IConvertible { return p * factor; }
		public static Vector3<T> Div<T>(this Vector3<T> p, T factor)     where T:IEquatable<T>, IConvertible { return p / factor; }
		public static Point3<T>  Div<T>(this Point3<T> p, T factor)      where T:IEquatable<T>, IConvertible { return p / factor; }
		public static Vector3<T> Shl<T>(this Vector3<T> p, int amt)      where T:IEquatable<T>, IConvertible { return p << amt; }
		public static Vector3<T> Shr<T>(this Vector3<T> p, int amt)      where T:IEquatable<T>, IConvertible { return p >> amt; }
		public static Point3<T>  Shl<T>(this Point3<T> p, int amt)       where T:IEquatable<T>, IConvertible { return p << amt; }
		public static Point3<T>  Shr<T>(this Point3<T> p, int amt)       where T:IEquatable<T>, IConvertible { return p >> amt; }
	}
}
