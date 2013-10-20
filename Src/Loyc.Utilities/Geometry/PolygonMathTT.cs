
// This is a generated file
using Loyc.Math;
using System.Collections.Generic;

namespace Loyc.Geometry
{
	using T = System.Int32;
	using LineSegment = LineSegment<int>;
	using Point = Point<int>;
	using Vector = Vector<int>;
	using System;

	/// <summary>
	/// Contains useful basic polygon algorithms: hit testing, area calculation,
	/// orientation detection.
	/// </summary>
	public static partial class PolygonMath
	{
		/// <summary>Computes the area of a polygon.</summary>
		/// <returns>The area. The result is positive if the polygon is clockwise 
		/// (assuming a coordinate system in which increasing Y goes upward), or 
		/// negative if the polygon is counterclockwise.</returns>
		/// <remarks>http://www.codeproject.com/Tips/601272/Calculating-the-area-of-a-polygon</remarks>
		public static long PolygonArea(IEnumerable<Point> polygon) { return PolygonArea(polygon.GetEnumerator()); }
		public static long PolygonArea(IEnumerator<Point> e)
		{
		  if (!e.MoveNext()) return 0;
		  Point first = e.Current, last = first;

		  long area = 0;
		  while (e.MoveNext()) {
			Point next = e.Current;
			area += (long)next.X * (long)last.Y - (long)last.X * (long)next.Y;
			last = next;
		  }
		  area += (long)first.X * (long)last.Y - (long)last.X * (long)first.Y;
		  return area / (long)2;
		}

		/// <summary>Returns Math.Sign(PolygonArea(poly)): positive when clockwise 
		/// and increasing Y goes upward.</summary>
		/// <remarks>A common approach to this problem is to look at the topmost
		/// point and the two points on either side. However, if one is not careful,
		/// this technique may be unable to detect the orientation in case the 
		/// polygon has duplicate points, horizontal lines on top, or a degenerate
		/// top in which the top part of the polygon is zero-width (these problems
		/// can occur even if the polygon's lines do not cross one another.) That's
		/// why I chose to compute orientation based on area instead.</remarks>
		public static int Orientation(IEnumerable<Point> poly) { return MathEx.Sign(PolygonArea(poly)); }
		public static int Orientation(IEnumerator<Point> poly) { return MathEx.Sign(PolygonArea(poly)); }

		/// <summary>Finds out if a point is inside the polygon using a winding 
		/// test.</summary>
		public static bool IsPointInPolygon(IEnumerable<Point> poly, Point p) { return GetWindingNumber(poly.GetEnumerator(), p) != 0; }
		public static bool IsPointInPolygon(IEnumerator<Point> e, Point p)    { return GetWindingNumber(e, p) != 0; }

		/// <summary>Counts the number of times the polygon winds around a test 
		/// point, using a rightward raycasting test.</summary>
		/// <returns>Returns the winding number: the number of times that the
		/// polygon winds around the point. Positive means clockwise (assuming a 
		/// coordinate system in which increasing Y goes upward), negative means
		/// counterclockwise. Always returns -1, 0 or +1 when the polygon does
		/// not self-intersect. Returns 0 for a degenerate polygon.</returns>
		/// <remarks>
		/// The test point is considered to be within the polygon if it lies
		/// on a top or left edge, but not on a bottom or right edge (within
		/// the precision limits of 'double' arithmetic). The test point will 
		/// never be considered inside a degenerate (zero-width) area.
		/// </remarks>
		public static int GetWindingNumber(this IEnumerable<Point> poly, Point p) { return GetWindingNumber(poly.GetEnumerator(), p); }
		public static int GetWindingNumber(this IEnumerator<Point> e, Point p)
		{
			if (!e.MoveNext())
				return 0;

			Point first = e.Current, prev = first;
			int windingNo = 0;
			while(e.MoveNext())
				windingNo += GWN_NextLine(p, prev, prev = e.Current);
			windingNo += GWN_NextLine(p, prev, first);
			return windingNo;
		}
		static int GWN_NextLine(Point p, Point p1, Point p2)
		{
			if ((p.Y >= p1.Y) != (p.Y >= p2.Y)) {
				if (p1.X > p.X || p2.X > p.X) {
					if (p1.X > p.X && p2.X > p.X)
						return p2.Y > p1.Y ? 1 : -1;
					else {
						// If p2.Y > p1.Y, it's a crossing when
						//   p.Y - p1.Y     p.X - p1.X
						//  ------------ >  ------------
						//  p2.Y - p1.Y     p2.X - p1.X
						var lhs = (long)(p2.X - p1.X) * (long)(p.Y - p1.Y);
						var rhs = (long)(p2.Y - p1.Y) * (long)(p.X - p1.X);
						if (p2.Y > p1.Y) {
							if (lhs > rhs) return 1;
						} else {
							if (lhs < rhs) return -1;
						}
					}
				}
			}
			return 0;
		}
	}
}
namespace Loyc.Geometry
{
	using T = System.Single;
	using LineSegment = LineSegment<float>;
	using Point = Point<float>;
	using Vector = Vector<float>;
	using System;

	/// <summary>
	/// Contains useful basic polygon algorithms: hit testing, area calculation,
	/// orientation detection.
	/// </summary>
	public static partial class PolygonMath
	{
		/// <summary>Computes the area of a polygon.</summary>
		/// <returns>The area. The result is positive if the polygon is clockwise 
		/// (assuming a coordinate system in which increasing Y goes upward), or 
		/// negative if the polygon is counterclockwise.</returns>
		/// <remarks>http://www.codeproject.com/Tips/601272/Calculating-the-area-of-a-polygon</remarks>
		public static float PolygonArea(IEnumerable<Point> polygon) { return PolygonArea(polygon.GetEnumerator()); }
		public static float PolygonArea(IEnumerator<Point> e)
		{
		  if (!e.MoveNext()) return 0;
		  Point first = e.Current, last = first;

		  float area = 0;
		  while (e.MoveNext()) {
			Point next = e.Current;
			area += (float)next.X * (float)last.Y - (float)last.X * (float)next.Y;
			last = next;
		  }
		  area += (float)first.X * (float)last.Y - (float)last.X * (float)first.Y;
		  return area / (float)2;
		}

		/// <summary>Returns Math.Sign(PolygonArea(poly)): positive when clockwise 
		/// and increasing Y goes upward.</summary>
		/// <remarks>A common approach to this problem is to look at the topmost
		/// point and the two points on either side. However, if one is not careful,
		/// this technique may be unable to detect the orientation in case the 
		/// polygon has duplicate points, horizontal lines on top, or a degenerate
		/// top in which the top part of the polygon is zero-width (these problems
		/// can occur even if the polygon's lines do not cross one another.) That's
		/// why I chose to compute orientation based on area instead.</remarks>
		public static int Orientation(IEnumerable<Point> poly) { return MathEx.Sign(PolygonArea(poly)); }
		public static int Orientation(IEnumerator<Point> poly) { return MathEx.Sign(PolygonArea(poly)); }

		/// <summary>Finds out if a point is inside the polygon using a winding 
		/// test.</summary>
		public static bool IsPointInPolygon(IEnumerable<Point> poly, Point p) { return GetWindingNumber(poly.GetEnumerator(), p) != 0; }
		public static bool IsPointInPolygon(IEnumerator<Point> e, Point p)    { return GetWindingNumber(e, p) != 0; }

		/// <summary>Counts the number of times the polygon winds around a test 
		/// point, using a rightward raycasting test.</summary>
		/// <returns>Returns the winding number: the number of times that the
		/// polygon winds around the point. Positive means clockwise (assuming a 
		/// coordinate system in which increasing Y goes upward), negative means
		/// counterclockwise. Always returns -1, 0 or +1 when the polygon does
		/// not self-intersect. Returns 0 for a degenerate polygon.</returns>
		/// <remarks>
		/// The test point is considered to be within the polygon if it lies
		/// on a top or left edge, but not on a bottom or right edge (within
		/// the precision limits of 'double' arithmetic). The test point will 
		/// never be considered inside a degenerate (zero-width) area.
		/// </remarks>
		public static int GetWindingNumber(this IEnumerable<Point> poly, Point p) { return GetWindingNumber(poly.GetEnumerator(), p); }
		public static int GetWindingNumber(this IEnumerator<Point> e, Point p)
		{
			if (!e.MoveNext())
				return 0;

			Point first = e.Current, prev = first;
			int windingNo = 0;
			while(e.MoveNext())
				windingNo += GWN_NextLine(p, prev, prev = e.Current);
			windingNo += GWN_NextLine(p, prev, first);
			return windingNo;
		}
		static int GWN_NextLine(Point p, Point p1, Point p2)
		{
			if ((p.Y >= p1.Y) != (p.Y >= p2.Y)) {
				if (p1.X > p.X || p2.X > p.X) {
					if (p1.X > p.X && p2.X > p.X)
						return p2.Y > p1.Y ? 1 : -1;
					else {
						// If p2.Y > p1.Y, it's a crossing when
						//   p.Y - p1.Y     p.X - p1.X
						//  ------------ >  ------------
						//  p2.Y - p1.Y     p2.X - p1.X
						var lhs = (float)(p2.X - p1.X) * (float)(p.Y - p1.Y);
						var rhs = (float)(p2.Y - p1.Y) * (float)(p.X - p1.X);
						if (p2.Y > p1.Y) {
							if (lhs > rhs) return 1;
						} else {
							if (lhs < rhs) return -1;
						}
					}
				}
			}
			return 0;
		}
	}
}
namespace Loyc.Geometry
{
	using T = System.Double;
	using LineSegment = LineSegment<double>;
	using Point = Point<double>;
	using Vector = Vector<double>;
	using System;

	/// <summary>
	/// Contains useful basic polygon algorithms: hit testing, area calculation,
	/// orientation detection.
	/// </summary>
	public static partial class PolygonMath
	{
		/// <summary>Computes the area of a polygon.</summary>
		/// <returns>The area. The result is positive if the polygon is clockwise 
		/// (assuming a coordinate system in which increasing Y goes upward), or 
		/// negative if the polygon is counterclockwise.</returns>
		/// <remarks>http://www.codeproject.com/Tips/601272/Calculating-the-area-of-a-polygon</remarks>
		public static double PolygonArea(IEnumerable<Point> polygon) { return PolygonArea(polygon.GetEnumerator()); }
		public static double PolygonArea(IEnumerator<Point> e)
		{
		  if (!e.MoveNext()) return 0;
		  Point first = e.Current, last = first;

		  double area = 0;
		  while (e.MoveNext()) {
			Point next = e.Current;
			area += (double)next.X * (double)last.Y - (double)last.X * (double)next.Y;
			last = next;
		  }
		  area += (double)first.X * (double)last.Y - (double)last.X * (double)first.Y;
		  return area / (double)2;
		}

		/// <summary>Returns Math.Sign(PolygonArea(poly)): positive when clockwise 
		/// and increasing Y goes upward.</summary>
		/// <remarks>A common approach to this problem is to look at the topmost
		/// point and the two points on either side. However, if one is not careful,
		/// this technique may be unable to detect the orientation in case the 
		/// polygon has duplicate points, horizontal lines on top, or a degenerate
		/// top in which the top part of the polygon is zero-width (these problems
		/// can occur even if the polygon's lines do not cross one another.) That's
		/// why I chose to compute orientation based on area instead.</remarks>
		public static int Orientation(IEnumerable<Point> poly) { return MathEx.Sign(PolygonArea(poly)); }
		public static int Orientation(IEnumerator<Point> poly) { return MathEx.Sign(PolygonArea(poly)); }

		/// <summary>Finds out if a point is inside the polygon using a winding 
		/// test.</summary>
		public static bool IsPointInPolygon(IEnumerable<Point> poly, Point p) { return GetWindingNumber(poly.GetEnumerator(), p) != 0; }
		public static bool IsPointInPolygon(IEnumerator<Point> e, Point p)    { return GetWindingNumber(e, p) != 0; }

		/// <summary>Counts the number of times the polygon winds around a test 
		/// point, using a rightward raycasting test.</summary>
		/// <returns>Returns the winding number: the number of times that the
		/// polygon winds around the point. Positive means clockwise (assuming a 
		/// coordinate system in which increasing Y goes upward), negative means
		/// counterclockwise. Always returns -1, 0 or +1 when the polygon does
		/// not self-intersect. Returns 0 for a degenerate polygon.</returns>
		/// <remarks>
		/// The test point is considered to be within the polygon if it lies
		/// on a top or left edge, but not on a bottom or right edge (within
		/// the precision limits of 'double' arithmetic). The test point will 
		/// never be considered inside a degenerate (zero-width) area.
		/// </remarks>
		public static int GetWindingNumber(this IEnumerable<Point> poly, Point p) { return GetWindingNumber(poly.GetEnumerator(), p); }
		public static int GetWindingNumber(this IEnumerator<Point> e, Point p)
		{
			if (!e.MoveNext())
				return 0;

			Point first = e.Current, prev = first;
			int windingNo = 0;
			while(e.MoveNext())
				windingNo += GWN_NextLine(p, prev, prev = e.Current);
			windingNo += GWN_NextLine(p, prev, first);
			return windingNo;
		}
		static int GWN_NextLine(Point p, Point p1, Point p2)
		{
			if ((p.Y >= p1.Y) != (p.Y >= p2.Y)) {
				if (p1.X > p.X || p2.X > p.X) {
					if (p1.X > p.X && p2.X > p.X)
						return p2.Y > p1.Y ? 1 : -1;
					else {
						// If p2.Y > p1.Y, it's a crossing when
						//   p.Y - p1.Y     p.X - p1.X
						//  ------------ >  ------------
						//  p2.Y - p1.Y     p2.X - p1.X
						var lhs = (double)(p2.X - p1.X) * (double)(p.Y - p1.Y);
						var rhs = (double)(p2.Y - p1.Y) * (double)(p.X - p1.X);
						if (p2.Y > p1.Y) {
							if (lhs > rhs) return 1;
						} else {
							if (lhs < rhs) return -1;
						}
					}
				}
			}
			return 0;
		}
	}
}
namespace Loyc.Geometry
{
	using T = FPI8;
	using LineSegment = LineSegment<FPI8>;
	using Point = Point<FPI8>;
	using Vector = Vector<FPI8>;
	using System;

	/// <summary>
	/// Contains useful basic polygon algorithms: hit testing, area calculation,
	/// orientation detection.
	/// </summary>
	public static partial class PolygonMath
	{
		/// <summary>Computes the area of a polygon.</summary>
		/// <returns>The area. The result is positive if the polygon is clockwise 
		/// (assuming a coordinate system in which increasing Y goes upward), or 
		/// negative if the polygon is counterclockwise.</returns>
		/// <remarks>http://www.codeproject.com/Tips/601272/Calculating-the-area-of-a-polygon</remarks>
		public static FPL16 PolygonArea(IEnumerable<Point> polygon) { return PolygonArea(polygon.GetEnumerator()); }
		public static FPL16 PolygonArea(IEnumerator<Point> e)
		{
		  if (!e.MoveNext()) return 0;
		  Point first = e.Current, last = first;

		  FPL16 area = 0;
		  while (e.MoveNext()) {
			Point next = e.Current;
			area += (FPL16)next.X * (FPL16)last.Y - (FPL16)last.X * (FPL16)next.Y;
			last = next;
		  }
		  area += (FPL16)first.X * (FPL16)last.Y - (FPL16)last.X * (FPL16)first.Y;
		  return area / (FPL16)2;
		}

		/// <summary>Returns Math.Sign(PolygonArea(poly)): positive when clockwise 
		/// and increasing Y goes upward.</summary>
		/// <remarks>A common approach to this problem is to look at the topmost
		/// point and the two points on either side. However, if one is not careful,
		/// this technique may be unable to detect the orientation in case the 
		/// polygon has duplicate points, horizontal lines on top, or a degenerate
		/// top in which the top part of the polygon is zero-width (these problems
		/// can occur even if the polygon's lines do not cross one another.) That's
		/// why I chose to compute orientation based on area instead.</remarks>
		public static int Orientation(IEnumerable<Point> poly) { return MathEx.Sign(PolygonArea(poly)); }
		public static int Orientation(IEnumerator<Point> poly) { return MathEx.Sign(PolygonArea(poly)); }

		/// <summary>Finds out if a point is inside the polygon using a winding 
		/// test.</summary>
		public static bool IsPointInPolygon(IEnumerable<Point> poly, Point p) { return GetWindingNumber(poly.GetEnumerator(), p) != 0; }
		public static bool IsPointInPolygon(IEnumerator<Point> e, Point p)    { return GetWindingNumber(e, p) != 0; }

		/// <summary>Counts the number of times the polygon winds around a test 
		/// point, using a rightward raycasting test.</summary>
		/// <returns>Returns the winding number: the number of times that the
		/// polygon winds around the point. Positive means clockwise (assuming a 
		/// coordinate system in which increasing Y goes upward), negative means
		/// counterclockwise. Always returns -1, 0 or +1 when the polygon does
		/// not self-intersect. Returns 0 for a degenerate polygon.</returns>
		/// <remarks>
		/// The test point is considered to be within the polygon if it lies
		/// on a top or left edge, but not on a bottom or right edge (within
		/// the precision limits of 'double' arithmetic). The test point will 
		/// never be considered inside a degenerate (zero-width) area.
		/// </remarks>
		public static int GetWindingNumber(this IEnumerable<Point> poly, Point p) { return GetWindingNumber(poly.GetEnumerator(), p); }
		public static int GetWindingNumber(this IEnumerator<Point> e, Point p)
		{
			if (!e.MoveNext())
				return 0;

			Point first = e.Current, prev = first;
			int windingNo = 0;
			while(e.MoveNext())
				windingNo += GWN_NextLine(p, prev, prev = e.Current);
			windingNo += GWN_NextLine(p, prev, first);
			return windingNo;
		}
		static int GWN_NextLine(Point p, Point p1, Point p2)
		{
			if ((p.Y >= p1.Y) != (p.Y >= p2.Y)) {
				if (p1.X > p.X || p2.X > p.X) {
					if (p1.X > p.X && p2.X > p.X)
						return p2.Y > p1.Y ? 1 : -1;
					else {
						// If p2.Y > p1.Y, it's a crossing when
						//   p.Y - p1.Y     p.X - p1.X
						//  ------------ >  ------------
						//  p2.Y - p1.Y     p2.X - p1.X
						var lhs = (FPL16)(p2.X - p1.X) * (FPL16)(p.Y - p1.Y);
						var rhs = (FPL16)(p2.Y - p1.Y) * (FPL16)(p.X - p1.X);
						if (p2.Y > p1.Y) {
							if (lhs > rhs) return 1;
						} else {
							if (lhs < rhs) return -1;
						}
					}
				}
			}
			return 0;
		}
	}
}
namespace Loyc.Geometry
{
	using T = FPI16;
	using LineSegment = LineSegment<FPI16>;
	using Point = Point<FPI16>;
	using Vector = Vector<FPI16>;
	using System;

	/// <summary>
	/// Contains useful basic polygon algorithms: hit testing, area calculation,
	/// orientation detection.
	/// </summary>
	public static partial class PolygonMath
	{
		/// <summary>Computes the area of a polygon.</summary>
		/// <returns>The area. The result is positive if the polygon is clockwise 
		/// (assuming a coordinate system in which increasing Y goes upward), or 
		/// negative if the polygon is counterclockwise.</returns>
		/// <remarks>http://www.codeproject.com/Tips/601272/Calculating-the-area-of-a-polygon</remarks>
		public static FPL32 PolygonArea(IEnumerable<Point> polygon) { return PolygonArea(polygon.GetEnumerator()); }
		public static FPL32 PolygonArea(IEnumerator<Point> e)
		{
		  if (!e.MoveNext()) return 0;
		  Point first = e.Current, last = first;

		  FPL32 area = 0;
		  while (e.MoveNext()) {
			Point next = e.Current;
			area += (FPL32)next.X * (FPL32)last.Y - (FPL32)last.X * (FPL32)next.Y;
			last = next;
		  }
		  area += (FPL32)first.X * (FPL32)last.Y - (FPL32)last.X * (FPL32)first.Y;
		  return area / (FPL32)2;
		}

		/// <summary>Returns Math.Sign(PolygonArea(poly)): positive when clockwise 
		/// and increasing Y goes upward.</summary>
		/// <remarks>A common approach to this problem is to look at the topmost
		/// point and the two points on either side. However, if one is not careful,
		/// this technique may be unable to detect the orientation in case the 
		/// polygon has duplicate points, horizontal lines on top, or a degenerate
		/// top in which the top part of the polygon is zero-width (these problems
		/// can occur even if the polygon's lines do not cross one another.) That's
		/// why I chose to compute orientation based on area instead.</remarks>
		public static int Orientation(IEnumerable<Point> poly) { return MathEx.Sign(PolygonArea(poly)); }
		public static int Orientation(IEnumerator<Point> poly) { return MathEx.Sign(PolygonArea(poly)); }

		/// <summary>Finds out if a point is inside the polygon using a winding 
		/// test.</summary>
		public static bool IsPointInPolygon(IEnumerable<Point> poly, Point p) { return GetWindingNumber(poly.GetEnumerator(), p) != 0; }
		public static bool IsPointInPolygon(IEnumerator<Point> e, Point p)    { return GetWindingNumber(e, p) != 0; }

		/// <summary>Counts the number of times the polygon winds around a test 
		/// point, using a rightward raycasting test.</summary>
		/// <returns>Returns the winding number: the number of times that the
		/// polygon winds around the point. Positive means clockwise (assuming a 
		/// coordinate system in which increasing Y goes upward), negative means
		/// counterclockwise. Always returns -1, 0 or +1 when the polygon does
		/// not self-intersect. Returns 0 for a degenerate polygon.</returns>
		/// <remarks>
		/// The test point is considered to be within the polygon if it lies
		/// on a top or left edge, but not on a bottom or right edge (within
		/// the precision limits of 'double' arithmetic). The test point will 
		/// never be considered inside a degenerate (zero-width) area.
		/// </remarks>
		public static int GetWindingNumber(this IEnumerable<Point> poly, Point p) { return GetWindingNumber(poly.GetEnumerator(), p); }
		public static int GetWindingNumber(this IEnumerator<Point> e, Point p)
		{
			if (!e.MoveNext())
				return 0;

			Point first = e.Current, prev = first;
			int windingNo = 0;
			while(e.MoveNext())
				windingNo += GWN_NextLine(p, prev, prev = e.Current);
			windingNo += GWN_NextLine(p, prev, first);
			return windingNo;
		}
		static int GWN_NextLine(Point p, Point p1, Point p2)
		{
			if ((p.Y >= p1.Y) != (p.Y >= p2.Y)) {
				if (p1.X > p.X || p2.X > p.X) {
					if (p1.X > p.X && p2.X > p.X)
						return p2.Y > p1.Y ? 1 : -1;
					else {
						// If p2.Y > p1.Y, it's a crossing when
						//   p.Y - p1.Y     p.X - p1.X
						//  ------------ >  ------------
						//  p2.Y - p1.Y     p2.X - p1.X
						var lhs = (FPL32)(p2.X - p1.X) * (FPL32)(p.Y - p1.Y);
						var rhs = (FPL32)(p2.Y - p1.Y) * (FPL32)(p.X - p1.X);
						if (p2.Y > p1.Y) {
							if (lhs > rhs) return 1;
						} else {
							if (lhs < rhs) return -1;
						}
					}
				}
			}
			return 0;
		}
	}
}
