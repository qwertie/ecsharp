
// This is a generated file
using Loyc.Math;
using System.Diagnostics;
using System;

namespace Loyc.Geometry
{
	using T = System.Single;
	using LineSegment = LineSegment<float>;
	using Point = Point<float>;
	using Vector = Vector<float>;
	using System;

	public static partial class LineMath
	{
		/// <summary>Performs projection, which finds the nearest point to a 
		/// specified point that is on a line segment or infinite line.</summary>
		/// <param name="seg">The line segment</param>
		/// <param name="p">The test point to be projected</param>
		/// <param name="infiniteLine">Whether to extend the line infinitely.</param>
		/// <param name="end">Set to 0 if the point is on the line segment, -1 if the 
		/// point is before seg.A, 1 if the point is after seg.B, and null if the line 
		/// segment is degenerate (seg.A==seg.B)</param>
		/// <returns>The projected point.</returns>
		/// <remarks>
		/// This algorithm is fast and accurate, and can be easily adapted to 3D.
		/// A special advantage of this approach is that it runs fastest when the 
		/// point is projected onto one of the endpoints (when infiniteLine is 
		/// false).
		/// <para/>
		/// Algorithm comes from: http://geomalgorithms.com/a02-_lines.html
		/// See section "Distance of a Point to a Ray or Segment"
		/// </remarks>
		public static Point ProjectOnto(this Point p, LineSegment seg, bool infiniteLine = false)
		{
			int? _;
			return ProjectOnto(p, seg, infiniteLine, out _);
		}
		public static Point ProjectOnto(this Point p, LineSegment seg, bool infiniteLine, out int? end)
		{
			Vector v = seg.Vector();
			Vector w = p.Sub(seg.A);
			T c1 = w.Dot(v); // c1 == |w|*|v|*cos(angle between them)
			if (c1 <= 0) { // angle between line segment and (p-seg.A) is negative (-180..0)?
				end = -1;
				if (!infiniteLine)
					return seg.A;
			}
			T c2 = v.Quadrance(); // == |v|*|v|
			if (c1 >= c2) { // quadrance from seg.A to projected point >= quadrance of seg
				end = 1;
				if (!infiniteLine)
					return seg.B;
			} else
				end = 0;

			if (c2 == 0) {
				Debug.Assert(seg.A == seg.B);
				end = null;
				return seg.A;
			}
			T frac = c1 / c2;                    // == |w|/|v|*cos(angle)
			Point projected = seg.A.Add(v.Mul(frac)); // == p0 + v/|v|*|w|*cos(angle)
			return projected;
		}
		public static Point ProjectOntoInfiniteLine(this Point p, LineSegment seg)
		{
			return ProjectOnto(p, seg, true);
		}
		public static Point ProjectOnto(this Point p, LineSegment seg)
		{
			return ProjectOnto(p, seg, false);
		}

		/// <summary>Gets the projection of a point onto a line, expressed as a 
		/// fraction where 0 represents the start of the line and 1 represents the 
		/// end of the line.</summary>
		/// <param name="infiniteLine">Whether to return numbers outside the range
		/// (0, 1) if the projection is outside the line segment. If this is false,
		/// the result is clamped to (0, 1)</param>
		/// <param name="end">Same as for <see cref="ProjectOnto"/>.</param>
		/// <returns>The fraction of p along seg, as explained already. If seg is
		/// zero-length, the result is always 0.</returns>
		/// <remarks>This method uses the same technique as <see cref="ProjectOnto"/>.</remarks>
		public static T GetFractionAlong(this Point p, LineSegment seg, bool infiniteLine = false)
		{
			int? _;
			return GetFractionAlong(p, seg, infiniteLine, out _);
		}
		public static T GetFractionAlong(this Point p, LineSegment seg, bool infiniteLine, out int? end)
		{
			Vector v = seg.Vector();
			Vector w = p.Sub(seg.A);
			T c1 = w.Dot(v); // c1 == |w|*|v|*cos(angle between them)
			if (c1 <= 0) { // angle between line segment and (p-seg.A) is negative (-180..0)?
				end = -1;
				if (!infiniteLine)
					return 0;
			}
			T c2 = v.Quadrance(); // == |v|*|v|
			if (c1 >= c2) { // quadrance from seg.A to projected point >= quadrance of seg
				end = 1;
				if (!infiniteLine)
					return 1;
			} else
				end = 0;

			if (c2 == 0) {
				Debug.Assert(seg.A == seg.B);
				end = null;
				return 0;
			}
			return c1 / c2; // == |w|/|v|*cos(angle)
		}

		/// <summary>Given a fraction between zero and one, calculates a point 
		/// between two points (0=point A, 1=point B, 0.5=midpoint).</summary>
		/// <remarks>If you just want the midpoint, call Midpoint() which 
		/// is faster. If the fraction is outside the range [0,1], the result
		/// will be along the infinite extension of the line. If the two points
		/// are the same, this method always returns the same point as long as
		/// the math doesn't overflow, possibly with slight deviations caused 
		/// by floating-point rounding.</remarks>
		public static Point PointAlong(this LineSegment seg, T frac)
		{
			T frac1 = 1 - frac;
			return new Point(seg.A.X * frac1 + seg.B.X * frac, seg.A.Y * frac1 + seg.B.Y * frac);
		}

		/// <summary>Returns the midpoint, (A + B) >> 1.</summary>
		public static Point Midpoint(this LineSegment seg)
		{
			return new Point(MathEx.ShiftRight(seg.A.X + seg.B.X, 1), MathEx.ShiftRight(seg.A.Y + seg.B.Y, 1));
		}

		public static T QuadranceTo(this Point p, LineSegment seg, bool infiniteLine = false)
		{
			return (p - ProjectOnto(p, seg, infiniteLine)).Quadrance();
		}
		public static T DistanceTo(this Point p, LineSegment seg, bool infiniteLine = false)
		{
			return (p - ProjectOnto(p, seg, infiniteLine)).Length();
		}
		public static T Length(this LineSegment seg)
		{
			return seg.Vector().Length();
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

	public static partial class LineMath
	{
		/// <summary>Performs projection, which finds the nearest point to a 
		/// specified point that is on a line segment or infinite line.</summary>
		/// <param name="seg">The line segment</param>
		/// <param name="p">The test point to be projected</param>
		/// <param name="infiniteLine">Whether to extend the line infinitely.</param>
		/// <param name="end">Set to 0 if the point is on the line segment, -1 if the 
		/// point is before seg.A, 1 if the point is after seg.B, and null if the line 
		/// segment is degenerate (seg.A==seg.B)</param>
		/// <returns>The projected point.</returns>
		/// <remarks>
		/// This algorithm is fast and accurate, and can be easily adapted to 3D.
		/// A special advantage of this approach is that it runs fastest when the 
		/// point is projected onto one of the endpoints (when infiniteLine is 
		/// false).
		/// <para/>
		/// Algorithm comes from: http://geomalgorithms.com/a02-_lines.html
		/// See section "Distance of a Point to a Ray or Segment"
		/// </remarks>
		public static Point ProjectOnto(this Point p, LineSegment seg, bool infiniteLine = false)
		{
			int? _;
			return ProjectOnto(p, seg, infiniteLine, out _);
		}
		public static Point ProjectOnto(this Point p, LineSegment seg, bool infiniteLine, out int? end)
		{
			Vector v = seg.Vector();
			Vector w = p.Sub(seg.A);
			T c1 = w.Dot(v); // c1 == |w|*|v|*cos(angle between them)
			if (c1 <= 0) { // angle between line segment and (p-seg.A) is negative (-180..0)?
				end = -1;
				if (!infiniteLine)
					return seg.A;
			}
			T c2 = v.Quadrance(); // == |v|*|v|
			if (c1 >= c2) { // quadrance from seg.A to projected point >= quadrance of seg
				end = 1;
				if (!infiniteLine)
					return seg.B;
			} else
				end = 0;

			if (c2 == 0) {
				Debug.Assert(seg.A == seg.B);
				end = null;
				return seg.A;
			}
			T frac = c1 / c2;                    // == |w|/|v|*cos(angle)
			Point projected = seg.A.Add(v.Mul(frac)); // == p0 + v/|v|*|w|*cos(angle)
			return projected;
		}
		public static Point ProjectOntoInfiniteLine(this Point p, LineSegment seg)
		{
			return ProjectOnto(p, seg, true);
		}
		public static Point ProjectOnto(this Point p, LineSegment seg)
		{
			return ProjectOnto(p, seg, false);
		}

		/// <summary>Gets the projection of a point onto a line, expressed as a 
		/// fraction where 0 represents the start of the line and 1 represents the 
		/// end of the line.</summary>
		/// <param name="infiniteLine">Whether to return numbers outside the range
		/// (0, 1) if the projection is outside the line segment. If this is false,
		/// the result is clamped to (0, 1)</param>
		/// <param name="end">Same as for <see cref="ProjectOnto"/>.</param>
		/// <returns>The fraction of p along seg, as explained already. If seg is
		/// zero-length, the result is always 0.</returns>
		/// <remarks>This method uses the same technique as <see cref="ProjectOnto"/>.</remarks>
		public static T GetFractionAlong(this Point p, LineSegment seg, bool infiniteLine = false)
		{
			int? _;
			return GetFractionAlong(p, seg, infiniteLine, out _);
		}
		public static T GetFractionAlong(this Point p, LineSegment seg, bool infiniteLine, out int? end)
		{
			Vector v = seg.Vector();
			Vector w = p.Sub(seg.A);
			T c1 = w.Dot(v); // c1 == |w|*|v|*cos(angle between them)
			if (c1 <= 0) { // angle between line segment and (p-seg.A) is negative (-180..0)?
				end = -1;
				if (!infiniteLine)
					return 0;
			}
			T c2 = v.Quadrance(); // == |v|*|v|
			if (c1 >= c2) { // quadrance from seg.A to projected point >= quadrance of seg
				end = 1;
				if (!infiniteLine)
					return 1;
			} else
				end = 0;

			if (c2 == 0) {
				Debug.Assert(seg.A == seg.B);
				end = null;
				return 0;
			}
			return c1 / c2; // == |w|/|v|*cos(angle)
		}

		/// <summary>Given a fraction between zero and one, calculates a point 
		/// between two points (0=point A, 1=point B, 0.5=midpoint).</summary>
		/// <remarks>If you just want the midpoint, call Midpoint() which 
		/// is faster. If the fraction is outside the range [0,1], the result
		/// will be along the infinite extension of the line. If the two points
		/// are the same, this method always returns the same point as long as
		/// the math doesn't overflow, possibly with slight deviations caused 
		/// by floating-point rounding.</remarks>
		public static Point PointAlong(this LineSegment seg, T frac)
		{
			T frac1 = 1 - frac;
			return new Point(seg.A.X * frac1 + seg.B.X * frac, seg.A.Y * frac1 + seg.B.Y * frac);
		}

		/// <summary>Returns the midpoint, (A + B) >> 1.</summary>
		public static Point Midpoint(this LineSegment seg)
		{
			return new Point(MathEx.ShiftRight(seg.A.X + seg.B.X, 1), MathEx.ShiftRight(seg.A.Y + seg.B.Y, 1));
		}

		public static T QuadranceTo(this Point p, LineSegment seg, bool infiniteLine = false)
		{
			return (p - ProjectOnto(p, seg, infiniteLine)).Quadrance();
		}
		public static T DistanceTo(this Point p, LineSegment seg, bool infiniteLine = false)
		{
			return (p - ProjectOnto(p, seg, infiniteLine)).Length();
		}
		public static T Length(this LineSegment seg)
		{
			return seg.Vector().Length();
		}
	}
}

namespace Loyc.Geometry
{
	public static partial class LineMath
	{
		public static T Length<T>(this LineSegment<T> seg) where T : IConvertible, IEquatable<T>
		{
			return seg.Vector().Length();
		}
	}
}