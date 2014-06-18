
// This is a generated file
using System.Collections.Generic;
using System.Diagnostics;
using Loyc.Math;
using Loyc.Collections;

namespace Loyc.Geometry
{
	using T = System.Int32;
	using Point = Point<int>;
	using Vector = Vector<int>;
	using Point3 = Point3<int>;
	using Vector3 = Vector3<int>;
	using LineSegment = LineSegment<int>;
	using System;

	public static partial class PointMath
	{
		public static Vector Add(this Vector a, Vector b) { return new Vector(a.X+b.X, a.Y+b.Y); }
		public static Point  Add(this Point a, Vector b)  { return new Point(a.X+b.X, a.Y+b.Y); }
		public static Point  Add(this Vector a, Point b)  { return new Point(a.X+b.X, a.Y+b.Y); }
		public static Vector Sub(this Vector a, Vector b) { return new Vector(a.X-b.X, a.Y-b.Y); }
		public static Vector Sub(this Point a, Point b)   { return new Vector(a.X-b.X, a.Y-b.Y); }
		public static Point  Sub(this Point a, Vector b)  { return new Point(a.X-b.X, a.Y-b.Y); }
		public static Vector Mul(this Vector p, T factor) { return new Vector(p.X*factor, p.Y*factor); }
		public static Point  Mul(this Point p,  T factor) { return new Point(p.X*factor, p.Y*factor); }
		public static Vector Div(this Vector p, T factor) { return new Vector(p.X/factor, p.Y/factor); }
		public static Point  Div(this Point p, T factor)  { return new Point(p.X/factor, p.Y/factor); }
		public static Vector Shl(this Vector p, int amt) { return new Vector((T)(p.X << amt), (T)(p.Y << amt)); }
		public static Vector Shr(this Vector p, int amt) { return new Vector((T)(p.X >> amt), (T)(p.Y >> amt)); }
		public static Point  Shl(this Point p,  int amt) { return new Point((T)(p.X << amt), (T)(p.Y << amt)); }
		public static Point  Shr(this Point p,  int amt) { return new Point((T)(p.X >> amt), (T)(p.Y >> amt)); }
		
		public static Vector3 Add(this Vector3 a, Vector3 b) { return new Vector3(a.X+b.X, a.Y+b.Y, a.Z+b.Z); }
		public static Point3  Add(this Point3 a, Vector3 b)  { return new Point3(a.X+b.X, a.Y+b.Y, a.Z+b.Z); }
		public static Point3  Add(this Vector3 a, Point3 b)  { return new Point3(a.X+b.X, a.Y+b.Y, a.Z+b.Z); }
		public static Vector3 Sub(this Vector3 a, Vector3 b) { return new Vector3(a.X-b.X, a.Y-b.Y, a.Z-b.Z); }
		public static Vector3 Sub(this Point3 a, Point3 b)   { return new Vector3(a.X-b.X, a.Y-b.Y, a.Z-b.Z); }
		public static Vector3 Mul(this Vector3 p, T factor) { return new Vector3(p.X*factor, p.Y*factor, p.Z*factor); }
		public static Point3  Mul(this Point3  p, T factor) { return new Point3(p.X*factor, p.Y*factor, p.Z*factor); }
		public static Vector3 Div(this Vector3 p, T factor) { return new Vector3(p.X/factor, p.Y/factor, p.Z*factor); }
		public static Point3  Div(this Point3  p, T factor) { return new Point3(p.X/factor, p.Y/factor, p.Z*factor); }
		public static Vector3 Shl(this Vector3 p, int amt) { return new Vector3((T)(p.X << amt), (T)(p.Y << amt), (T)(p.Z << amt)); }
		public static Vector3 Shr(this Vector3 p, int amt) { return new Vector3((T)(p.X >> amt), (T)(p.Y >> amt), (T)(p.Z >> amt)); }
		public static Point3  Shl(this Point3 p, int amt)  { return new Point3((T)(p.X << amt), (T)(p.Y << amt), (T)(p.Z << amt)); }
		public static Point3  Shr(this Point3 p, int amt)  { return new Point3((T)(p.X >> amt), (T)(p.Y >> amt), (T)(p.Z >> amt)); }

		/// <summary>Dot product. a*b equals lhs.Length*rhs.Length*Cos(theta) if theta 
		/// is the angle between two vectors.</summary>
		public static T Dot(this Vector a, Vector b) { return a.X*b.X + a.Y*b.Y; }
		public static T Dot(this Vector3 a, Vector3 b) { return a.X*b.X + a.Y*b.Y + a.Z*b.Z; }

		/// <summary>Computes the "cross product" of a pair of 2D vectors.</summary>
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
		public static T Cross(this Vector a, Vector b) { return a.X * b.Y - a.Y * b.X; }

		/// <summary>Computes the cross product of a pair of 3D vectors.</summary>
		public static Vector3 Cross(this Vector3 a, Vector3 b)
		{
			return new Vector3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
		}

		/// <summary>Rotates a vector 90 degrees.</summary>
		/// <remarks>
		/// Rotatation is clockwise if increasing Y goes downward, counter-
		/// clockwise if increasing Y goes upward. If the vector represents the 
		/// direction of a line, the result also represents the coefficients 
		/// (a,b) of the implicit line equation aX + bY + c = 0.
		/// </remarks>
		public static Vector Rot90(this Vector a) { return new Vector(-a.Y, a.X); }
		/// <summary>Reverses a vector.</summary>
		public static Vector Neg(this Vector a) { return new Vector(-a.X, -a.Y); }
		/// <summary>Reverses a vector.</summary>
		public static Vector3 Neg(this Vector3 a) { return new Vector3(-a.X, -a.Y, -a.Z); }

		/// <summary>Gets the square of the length of the vector.</summary>
		public static T Quadrance(this Vector v) { return v.X*v.X + v.Y*v.Y; }
		public static T Quadrance(this Vector3 v) { return v.X*v.X + v.Y*v.Y + v.Z*v.Z; }
		/// <summary>Gets the length of the vector.</summary>
		public static T Length(this Vector v) { return (T)MathEx.Sqrt(Quadrance(v)); }
		public static T Length(this Vector3 v) { return (T)MathEx.Sqrt(Quadrance(v)); }
		
		/// <summary>Gets the angle from 0 to 2*PI of the vector, where (1,0) has 
		/// angle 0 and (0,1) has angle PI/2.</summary>
		public static double Angle(this Vector v)
		{ 
			double angle = Math.Atan2((double)v.Y, (double)v.X);
			if (angle < 0)
				return angle + 2*Math.PI; 
			return angle;
		}
		/// <summary>Gets the angle from 0 to 360 of the vector, where (1,0) has 
		/// angle 0 and (0,1) has angle 90.</summary>
		public static double AngleDeg(this Vector v)
		{
			return Angle(v) * (180 / Math.PI);
		}


		public static Vector MulDiv(this Vector v, T mul, T div)
		{
			return new Vector(MathEx.MulDiv(v.X, mul, div), MathEx.MulDiv(v.Y, mul, div));
		}
		public static Vector3 MulDiv(this Vector3 v, T mul, T div)
		{
			return new Vector3(MathEx.MulDiv(v.X, mul, div), MathEx.MulDiv(v.Y, mul, div), MathEx.MulDiv(v.Z, mul, div));
		}

		/// <summary>Returns seg.B - seg.A.</summary>
		public static Vector Vector(this LineSegment seg)
		{
			return seg.B.Sub(seg.A);
		}

		/// <summary>Gets the absolute value of the vector's individual components.</summary>
		public static Vector Abs(this Vector v)
		{
			return new Vector(Math.Abs(v.X), Math.Abs(v.Y));
		}
		/// <summary>Gets the absolute value of the vector's individual components.</summary>
		public static Vector3 Abs(this Vector3 v)
		{
			return new Vector3(Math.Abs(v.X), Math.Abs(v.Y), Math.Abs(v.Z));
		}

		public static Vector<T> PolarToVector(T magnitude, double radians)
		{
			return new Vector<T>((T)Math.Cos(radians) * magnitude, (T)Math.Sin(radians) * magnitude);
		}
		public static Point<T> PolarToPoint(T magnitude, double radians)
		{
			return new Point<T>((T)Math.Cos(radians) * magnitude, (T)Math.Sin(radians) * magnitude);
		}

		/// <summary>Computes the convex hull of a polygon, in clockwise order in a Y-up 
		/// coordinate system (counterclockwise in a Y-down coordinate system).</summary>
		/// <remarks>Uses the Monotone Chain algorithm, a.k.a. Andrew's Algorithm.</remarks>
		public static IListSource<Point> ComputeConvexHull(IEnumerable<Point> points)
		{
			var list = new List<Point>(points);
			return ComputeConvexHull(list, true);
		}
		public static IListSource<Point> ComputeConvexHull(List<Point> points, bool sortInPlace)
		{
			if (!sortInPlace)
				points = new List<Point>(points);
			points.Sort((a, b) => 
				a.X == b.X ? a.Y.CompareTo(b.Y) : a.X > b.X ? 1 : -1);

			// Importantly, DList provides O(1) insertion at beginning and end
			DList<Point> hull = new DList<Point>();
			int L = 0, U = 0; // size of lower and upper hulls

			// Builds a hull such that the output polygon starts at the leftmost point.
			for (int i = points.Count - 1; i >= 0 ; i--)
			{
				// right turn (clockwise) => negative cross product (for Y-up coords)
				Point p = points[i], p1;

				// build lower hull (at end of output list)
				while (L >= 2 && (p1 = hull.Last).Sub(hull[hull.Count-2]).Cross(p.Sub(p1)) >= 0) {
					hull.RemoveAt(hull.Count-1);
					L--;
				}
				hull.PushLast(p);
				L++;

				// build upper hull (at beginning of output list)
				while (U >= 2 && (p1 = hull.First).Sub(hull[1]).Cross(p.Sub(p1)) <= 0) {
					hull.RemoveAt(0);
					U--;
				}
				if (U != 0) // when U == 0, share the point added above
					hull.PushFirst(p);
				U++;
				Debug.Assert(U + L == hull.Count + 1);
			}
			hull.RemoveAt(hull.Count - 1);
			return hull;
		}
	}
}
namespace Loyc.Geometry
{
	using T = System.Single;
	using Point = Point<float>;
	using Vector = Vector<float>;
	using Point3 = Point3<float>;
	using Vector3 = Vector3<float>;
	using LineSegment = LineSegment<float>;
	using System;

	public static partial class PointMath
	{
		public static Vector Add(this Vector a, Vector b) { return new Vector(a.X+b.X, a.Y+b.Y); }
		public static Point  Add(this Point a, Vector b)  { return new Point(a.X+b.X, a.Y+b.Y); }
		public static Point  Add(this Vector a, Point b)  { return new Point(a.X+b.X, a.Y+b.Y); }
		public static Vector Sub(this Vector a, Vector b) { return new Vector(a.X-b.X, a.Y-b.Y); }
		public static Vector Sub(this Point a, Point b)   { return new Vector(a.X-b.X, a.Y-b.Y); }
		public static Point  Sub(this Point a, Vector b)  { return new Point(a.X-b.X, a.Y-b.Y); }
		public static Vector Mul(this Vector p, T factor) { return new Vector(p.X*factor, p.Y*factor); }
		public static Point  Mul(this Point p,  T factor) { return new Point(p.X*factor, p.Y*factor); }
		public static Vector Div(this Vector p, T factor) { return new Vector(p.X/factor, p.Y/factor); }
		public static Point  Div(this Point p, T factor)  { return new Point(p.X/factor, p.Y/factor); }
		public static Vector Shl(this Vector p, int amt) { return new Vector(MathEx.ShiftLeft(p.X, amt), MathEx.ShiftLeft(p.Y, amt)); }
		public static Vector Shr(this Vector p, int amt) { return new Vector(MathEx.ShiftRight(p.X, amt), MathEx.ShiftRight(p.Y, amt)); }
		public static Point  Shl(this Point p,  int amt) { return new Point(MathEx.ShiftLeft(p.X, amt), MathEx.ShiftLeft(p.Y, amt)); }
		public static Point  Shr(this Point p,  int amt) { return new Point(MathEx.ShiftRight(p.X, amt), MathEx.ShiftRight(p.Y, amt)); }
		
		public static Vector3 Add(this Vector3 a, Vector3 b) { return new Vector3(a.X+b.X, a.Y+b.Y, a.Z+b.Z); }
		public static Point3  Add(this Point3 a, Vector3 b)  { return new Point3(a.X+b.X, a.Y+b.Y, a.Z+b.Z); }
		public static Point3  Add(this Vector3 a, Point3 b)  { return new Point3(a.X+b.X, a.Y+b.Y, a.Z+b.Z); }
		public static Vector3 Sub(this Vector3 a, Vector3 b) { return new Vector3(a.X-b.X, a.Y-b.Y, a.Z-b.Z); }
		public static Vector3 Sub(this Point3 a, Point3 b)   { return new Vector3(a.X-b.X, a.Y-b.Y, a.Z-b.Z); }
		public static Vector3 Mul(this Vector3 p, T factor) { return new Vector3(p.X*factor, p.Y*factor, p.Z*factor); }
		public static Point3  Mul(this Point3  p, T factor) { return new Point3(p.X*factor, p.Y*factor, p.Z*factor); }
		public static Vector3 Div(this Vector3 p, T factor) { return new Vector3(p.X/factor, p.Y/factor, p.Z*factor); }
		public static Point3  Div(this Point3  p, T factor) { return new Point3(p.X/factor, p.Y/factor, p.Z*factor); }
		public static Vector3 Shl(this Vector3 p, int amt) { return new Vector3(MathEx.ShiftLeft(p.X, amt), MathEx.ShiftLeft(p.Y, amt), MathEx.ShiftLeft(p.Z, amt)); }
		public static Vector3 Shr(this Vector3 p, int amt) { return new Vector3(MathEx.ShiftRight(p.X, amt), MathEx.ShiftRight(p.Y, amt), MathEx.ShiftRight(p.Z, amt)); }
		public static Point3  Shl(this Point3 p, int amt)  { return new Point3(MathEx.ShiftLeft(p.X, amt), MathEx.ShiftLeft(p.Y, amt), MathEx.ShiftLeft(p.Z, amt)); }
		public static Point3  Shr(this Point3 p, int amt)  { return new Point3(MathEx.ShiftRight(p.X, amt), MathEx.ShiftRight(p.Y, amt), MathEx.ShiftRight(p.Z, amt)); }

		/// <summary>Dot product. a*b equals lhs.Length*rhs.Length*Cos(theta) if theta 
		/// is the angle between two vectors.</summary>
		public static T Dot(this Vector a, Vector b) { return a.X*b.X + a.Y*b.Y; }
		public static T Dot(this Vector3 a, Vector3 b) { return a.X*b.X + a.Y*b.Y + a.Z*b.Z; }

		/// <summary>Computes the "cross product" of a pair of 2D vectors.</summary>
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
		public static T Cross(this Vector a, Vector b) { return a.X * b.Y - a.Y * b.X; }

		/// <summary>Computes the cross product of a pair of 3D vectors.</summary>
		public static Vector3 Cross(this Vector3 a, Vector3 b)
		{
			return new Vector3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
		}

		/// <summary>Rotates a vector 90 degrees.</summary>
		/// <remarks>
		/// Rotatation is clockwise if increasing Y goes downward, counter-
		/// clockwise if increasing Y goes upward. If the vector represents the 
		/// direction of a line, the result also represents the coefficients 
		/// (a,b) of the implicit line equation aX + bY + c = 0.
		/// </remarks>
		public static Vector Rot90(this Vector a) { return new Vector(-a.Y, a.X); }
		/// <summary>Reverses a vector.</summary>
		public static Vector Neg(this Vector a) { return new Vector(-a.X, -a.Y); }
		/// <summary>Reverses a vector.</summary>
		public static Vector3 Neg(this Vector3 a) { return new Vector3(-a.X, -a.Y, -a.Z); }

		/// <summary>Gets the square of the length of the vector.</summary>
		public static T Quadrance(this Vector v) { return v.X*v.X + v.Y*v.Y; }
		public static T Quadrance(this Vector3 v) { return v.X*v.X + v.Y*v.Y + v.Z*v.Z; }
		/// <summary>Gets the length of the vector.</summary>
		public static T Length(this Vector v) { return (T)Math.Sqrt(Quadrance(v)); }
		public static T Length(this Vector3 v) { return (T)Math.Sqrt(Quadrance(v)); }
		
		/// <summary>Gets the angle from 0 to 2*PI of the vector, where (1,0) has 
		/// angle 0 and (0,1) has angle PI/2.</summary>
		public static double Angle(this Vector v)
		{ 
			double angle = Math.Atan2((double)v.Y, (double)v.X);
			if (angle < 0)
				return angle + 2*Math.PI; 
			return angle;
		}
		/// <summary>Gets the angle from 0 to 360 of the vector, where (1,0) has 
		/// angle 0 and (0,1) has angle 90.</summary>
		public static double AngleDeg(this Vector v)
		{
			return Angle(v) * (180 / Math.PI);
		}

		public static Vector Normalized(this Vector v)
		{
			T len = Length(v);
			if (len == (float)0)
				return v;
			if (len != (float)1) {
				var r = 1 / len;
				v.X *= r;
				v.Y *= r;
			}
			return v;
		}
		public static Vector3 Normalized(this Vector3 v)
		{
			T len = Length(v);
			if (len == (float)0)
				return v;
			if (len != (float)1) {
				var r = 1 / len;
				v.X *= r;
				v.Y *= r;
				v.Z *= r;
			}
			return v;
		}

		public static Vector MulDiv(this Vector v, T mul, T div)
		{
			return new Vector((v.X * mul / div), (v.Y * mul / div));
		}
		public static Vector3 MulDiv(this Vector3 v, T mul, T div)
		{
			return new Vector3((v.X * mul / div), (v.Y * mul / div), (v.Z * mul / div));
		}

		/// <summary>Returns seg.B - seg.A.</summary>
		public static Vector Vector(this LineSegment seg)
		{
			return seg.B.Sub(seg.A);
		}

		/// <summary>Gets the absolute value of the vector's individual components.</summary>
		public static Vector Abs(this Vector v)
		{
			return new Vector(Math.Abs(v.X), Math.Abs(v.Y));
		}
		/// <summary>Gets the absolute value of the vector's individual components.</summary>
		public static Vector3 Abs(this Vector3 v)
		{
			return new Vector3(Math.Abs(v.X), Math.Abs(v.Y), Math.Abs(v.Z));
		}

		public static Vector<T> PolarToVector(T magnitude, double radians)
		{
			return new Vector<T>((T)Math.Cos(radians) * magnitude, (T)Math.Sin(radians) * magnitude);
		}
		public static Point<T> PolarToPoint(T magnitude, double radians)
		{
			return new Point<T>((T)Math.Cos(radians) * magnitude, (T)Math.Sin(radians) * magnitude);
		}

		/// <summary>Computes the convex hull of a polygon, in clockwise order in a Y-up 
		/// coordinate system (counterclockwise in a Y-down coordinate system).</summary>
		/// <remarks>Uses the Monotone Chain algorithm, a.k.a. Andrew's Algorithm.</remarks>
		public static IListSource<Point> ComputeConvexHull(IEnumerable<Point> points)
		{
			var list = new List<Point>(points);
			return ComputeConvexHull(list, true);
		}
		public static IListSource<Point> ComputeConvexHull(List<Point> points, bool sortInPlace)
		{
			if (!sortInPlace)
				points = new List<Point>(points);
			points.Sort((a, b) => 
				a.X == b.X ? a.Y.CompareTo(b.Y) : a.X > b.X ? 1 : -1);

			// Importantly, DList provides O(1) insertion at beginning and end
			DList<Point> hull = new DList<Point>();
			int L = 0, U = 0; // size of lower and upper hulls

			// Builds a hull such that the output polygon starts at the leftmost point.
			for (int i = points.Count - 1; i >= 0 ; i--)
			{
				// right turn (clockwise) => negative cross product (for Y-up coords)
				Point p = points[i], p1;

				// build lower hull (at end of output list)
				while (L >= 2 && (p1 = hull.Last).Sub(hull[hull.Count-2]).Cross(p.Sub(p1)) >= 0) {
					hull.RemoveAt(hull.Count-1);
					L--;
				}
				hull.PushLast(p);
				L++;

				// build upper hull (at beginning of output list)
				while (U >= 2 && (p1 = hull.First).Sub(hull[1]).Cross(p.Sub(p1)) <= 0) {
					hull.RemoveAt(0);
					U--;
				}
				if (U != 0) // when U == 0, share the point added above
					hull.PushFirst(p);
				U++;
				Debug.Assert(U + L == hull.Count + 1);
			}
			hull.RemoveAt(hull.Count - 1);
			return hull;
		}
	}
}
namespace Loyc.Geometry
{
	using T = System.Double;
	using Point = Point<double>;
	using Vector = Vector<double>;
	using Point3 = Point3<double>;
	using Vector3 = Vector3<double>;
	using LineSegment = LineSegment<double>;
	using System;

	public static partial class PointMath
	{
		public static Vector Add(this Vector a, Vector b) { return new Vector(a.X+b.X, a.Y+b.Y); }
		public static Point  Add(this Point a, Vector b)  { return new Point(a.X+b.X, a.Y+b.Y); }
		public static Point  Add(this Vector a, Point b)  { return new Point(a.X+b.X, a.Y+b.Y); }
		public static Vector Sub(this Vector a, Vector b) { return new Vector(a.X-b.X, a.Y-b.Y); }
		public static Vector Sub(this Point a, Point b)   { return new Vector(a.X-b.X, a.Y-b.Y); }
		public static Point  Sub(this Point a, Vector b)  { return new Point(a.X-b.X, a.Y-b.Y); }
		public static Vector Mul(this Vector p, T factor) { return new Vector(p.X*factor, p.Y*factor); }
		public static Point  Mul(this Point p,  T factor) { return new Point(p.X*factor, p.Y*factor); }
		public static Vector Div(this Vector p, T factor) { return new Vector(p.X/factor, p.Y/factor); }
		public static Point  Div(this Point p, T factor)  { return new Point(p.X/factor, p.Y/factor); }
		public static Vector Shl(this Vector p, int amt) { return new Vector(MathEx.ShiftLeft(p.X, amt), MathEx.ShiftLeft(p.Y, amt)); }
		public static Vector Shr(this Vector p, int amt) { return new Vector(MathEx.ShiftRight(p.X, amt), MathEx.ShiftRight(p.Y, amt)); }
		public static Point  Shl(this Point p,  int amt) { return new Point(MathEx.ShiftLeft(p.X, amt), MathEx.ShiftLeft(p.Y, amt)); }
		public static Point  Shr(this Point p,  int amt) { return new Point(MathEx.ShiftRight(p.X, amt), MathEx.ShiftRight(p.Y, amt)); }
		
		public static Vector3 Add(this Vector3 a, Vector3 b) { return new Vector3(a.X+b.X, a.Y+b.Y, a.Z+b.Z); }
		public static Point3  Add(this Point3 a, Vector3 b)  { return new Point3(a.X+b.X, a.Y+b.Y, a.Z+b.Z); }
		public static Point3  Add(this Vector3 a, Point3 b)  { return new Point3(a.X+b.X, a.Y+b.Y, a.Z+b.Z); }
		public static Vector3 Sub(this Vector3 a, Vector3 b) { return new Vector3(a.X-b.X, a.Y-b.Y, a.Z-b.Z); }
		public static Vector3 Sub(this Point3 a, Point3 b)   { return new Vector3(a.X-b.X, a.Y-b.Y, a.Z-b.Z); }
		public static Vector3 Mul(this Vector3 p, T factor) { return new Vector3(p.X*factor, p.Y*factor, p.Z*factor); }
		public static Point3  Mul(this Point3  p, T factor) { return new Point3(p.X*factor, p.Y*factor, p.Z*factor); }
		public static Vector3 Div(this Vector3 p, T factor) { return new Vector3(p.X/factor, p.Y/factor, p.Z*factor); }
		public static Point3  Div(this Point3  p, T factor) { return new Point3(p.X/factor, p.Y/factor, p.Z*factor); }
		public static Vector3 Shl(this Vector3 p, int amt) { return new Vector3(MathEx.ShiftLeft(p.X, amt), MathEx.ShiftLeft(p.Y, amt), MathEx.ShiftLeft(p.Z, amt)); }
		public static Vector3 Shr(this Vector3 p, int amt) { return new Vector3(MathEx.ShiftRight(p.X, amt), MathEx.ShiftRight(p.Y, amt), MathEx.ShiftRight(p.Z, amt)); }
		public static Point3  Shl(this Point3 p, int amt)  { return new Point3(MathEx.ShiftLeft(p.X, amt), MathEx.ShiftLeft(p.Y, amt), MathEx.ShiftLeft(p.Z, amt)); }
		public static Point3  Shr(this Point3 p, int amt)  { return new Point3(MathEx.ShiftRight(p.X, amt), MathEx.ShiftRight(p.Y, amt), MathEx.ShiftRight(p.Z, amt)); }

		/// <summary>Dot product. a*b equals lhs.Length*rhs.Length*Cos(theta) if theta 
		/// is the angle between two vectors.</summary>
		public static T Dot(this Vector a, Vector b) { return a.X*b.X + a.Y*b.Y; }
		public static T Dot(this Vector3 a, Vector3 b) { return a.X*b.X + a.Y*b.Y + a.Z*b.Z; }

		/// <summary>Computes the "cross product" of a pair of 2D vectors.</summary>
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
		public static T Cross(this Vector a, Vector b) { return a.X * b.Y - a.Y * b.X; }

		/// <summary>Computes the cross product of a pair of 3D vectors.</summary>
		public static Vector3 Cross(this Vector3 a, Vector3 b)
		{
			return new Vector3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
		}

		/// <summary>Rotates a vector 90 degrees.</summary>
		/// <remarks>
		/// Rotatation is clockwise if increasing Y goes downward, counter-
		/// clockwise if increasing Y goes upward. If the vector represents the 
		/// direction of a line, the result also represents the coefficients 
		/// (a,b) of the implicit line equation aX + bY + c = 0.
		/// </remarks>
		public static Vector Rot90(this Vector a) { return new Vector(-a.Y, a.X); }
		/// <summary>Reverses a vector.</summary>
		public static Vector Neg(this Vector a) { return new Vector(-a.X, -a.Y); }
		/// <summary>Reverses a vector.</summary>
		public static Vector3 Neg(this Vector3 a) { return new Vector3(-a.X, -a.Y, -a.Z); }

		/// <summary>Gets the square of the length of the vector.</summary>
		public static T Quadrance(this Vector v) { return v.X*v.X + v.Y*v.Y; }
		public static T Quadrance(this Vector3 v) { return v.X*v.X + v.Y*v.Y + v.Z*v.Z; }
		/// <summary>Gets the length of the vector.</summary>
		public static T Length(this Vector v) { return (T)Math.Sqrt(Quadrance(v)); }
		public static T Length(this Vector3 v) { return (T)Math.Sqrt(Quadrance(v)); }
		
		/// <summary>Gets the angle from 0 to 2*PI of the vector, where (1,0) has 
		/// angle 0 and (0,1) has angle PI/2.</summary>
		public static double Angle(this Vector v)
		{ 
			double angle = Math.Atan2((double)v.Y, (double)v.X);
			if (angle < 0)
				return angle + 2*Math.PI; 
			return angle;
		}
		/// <summary>Gets the angle from 0 to 360 of the vector, where (1,0) has 
		/// angle 0 and (0,1) has angle 90.</summary>
		public static double AngleDeg(this Vector v)
		{
			return Angle(v) * (180 / Math.PI);
		}

		public static Vector Normalized(this Vector v)
		{
			T len = Length(v);
			if (len == (double)0)
				return v;
			if (len != (double)1) {
				var r = 1 / len;
				v.X *= r;
				v.Y *= r;
			}
			return v;
		}
		public static Vector3 Normalized(this Vector3 v)
		{
			T len = Length(v);
			if (len == (double)0)
				return v;
			if (len != (double)1) {
				var r = 1 / len;
				v.X *= r;
				v.Y *= r;
				v.Z *= r;
			}
			return v;
		}

		public static Vector MulDiv(this Vector v, T mul, T div)
		{
			return new Vector((v.X * mul / div), (v.Y * mul / div));
		}
		public static Vector3 MulDiv(this Vector3 v, T mul, T div)
		{
			return new Vector3((v.X * mul / div), (v.Y * mul / div), (v.Z * mul / div));
		}

		/// <summary>Returns seg.B - seg.A.</summary>
		public static Vector Vector(this LineSegment seg)
		{
			return seg.B.Sub(seg.A);
		}

		/// <summary>Gets the absolute value of the vector's individual components.</summary>
		public static Vector Abs(this Vector v)
		{
			return new Vector(Math.Abs(v.X), Math.Abs(v.Y));
		}
		/// <summary>Gets the absolute value of the vector's individual components.</summary>
		public static Vector3 Abs(this Vector3 v)
		{
			return new Vector3(Math.Abs(v.X), Math.Abs(v.Y), Math.Abs(v.Z));
		}

		public static Vector<T> PolarToVector(T magnitude, double radians)
		{
			return new Vector<T>((T)Math.Cos(radians) * magnitude, (T)Math.Sin(radians) * magnitude);
		}
		public static Point<T> PolarToPoint(T magnitude, double radians)
		{
			return new Point<T>((T)Math.Cos(radians) * magnitude, (T)Math.Sin(radians) * magnitude);
		}

		/// <summary>Computes the convex hull of a polygon, in clockwise order in a Y-up 
		/// coordinate system (counterclockwise in a Y-down coordinate system).</summary>
		/// <remarks>Uses the Monotone Chain algorithm, a.k.a. Andrew's Algorithm.</remarks>
		public static IListSource<Point> ComputeConvexHull(IEnumerable<Point> points)
		{
			var list = new List<Point>(points);
			return ComputeConvexHull(list, true);
		}
		public static IListSource<Point> ComputeConvexHull(List<Point> points, bool sortInPlace)
		{
			if (!sortInPlace)
				points = new List<Point>(points);
			points.Sort((a, b) => 
				a.X == b.X ? a.Y.CompareTo(b.Y) : a.X > b.X ? 1 : -1);

			// Importantly, DList provides O(1) insertion at beginning and end
			DList<Point> hull = new DList<Point>();
			int L = 0, U = 0; // size of lower and upper hulls

			// Builds a hull such that the output polygon starts at the leftmost point.
			for (int i = points.Count - 1; i >= 0 ; i--)
			{
				// right turn (clockwise) => negative cross product (for Y-up coords)
				Point p = points[i], p1;

				// build lower hull (at end of output list)
				while (L >= 2 && (p1 = hull.Last).Sub(hull[hull.Count-2]).Cross(p.Sub(p1)) >= 0) {
					hull.RemoveAt(hull.Count-1);
					L--;
				}
				hull.PushLast(p);
				L++;

				// build upper hull (at beginning of output list)
				while (U >= 2 && (p1 = hull.First).Sub(hull[1]).Cross(p.Sub(p1)) <= 0) {
					hull.RemoveAt(0);
					U--;
				}
				if (U != 0) // when U == 0, share the point added above
					hull.PushFirst(p);
				U++;
				Debug.Assert(U + L == hull.Count + 1);
			}
			hull.RemoveAt(hull.Count - 1);
			return hull;
		}
	}
}
