using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Loyc.Geometry
{
	[TestFixture]
	public class LineMathTests
	{
		[Test]
		public void LineIntersectionTests()
		{
			// Intersecting line segments, non-perpendicular
			TestItsc(Seg(2, 2, 10, 10), Seg(3, 0, 5, 8), P(4, 4), 0.25f, LineType.Segment, LineType.Segment);
			TestItsc(Seg(2, 2, 10, 10), Seg(5, 8, 3, 0), P(4, 4), 0.25f, LineType.Ray, LineType.Ray);
			TestItsc(Seg(10, 10, 2, 2), Seg(3, 0, 5, 8), P(4, 4), 0.75f, LineType.Infinite, LineType.Infinite);
			
			// Perpendicular non-intersecting line segments
			TestItsc(Seg(2, 0, 10, 0), Seg(0, 10, 0, 2), null, 0, LineType.Segment, LineType.Segment);
			TestItsc(Seg(2, 0, 10, 0), Seg(0, 10, 0, 2), null, 0, LineType.Ray,     LineType.Segment);
			TestItsc(Seg(2, 0, 10, 0), Seg(0, 10, 0, 2), null, 0, LineType.Segment, LineType.Ray);
			TestItsc(Seg(2, 0, 10, 0), Seg(0, 10, 0, 2), null, 0, LineType.Ray,     LineType.Ray);
			TestItsc(Seg(2, 0, 10, 0), Seg(0, 10, 0, 2), null,    -0.25f, LineType.Infinite, LineType.Segment);
			TestItsc(Seg(2, 0, 10, 0), Seg(0, 10, 0, 2), P(0, 0), -0.25f, LineType.Infinite, LineType.Ray);
			TestItsc(Seg(2, 0, 10, 0), Seg(0, 10, 0, 2), P(0, 0), -0.25f, LineType.Infinite, LineType.Infinite);
			
			// Line segments don't intersect but rays might
			TestItsc(Seg(0, 0, 8, 8), Seg(8, 2, 10, 2), null,    0.25f, LineType.Infinite, LineType.Ray);
			TestItsc(Seg(0, 0, 8, 8), Seg(10, 2, 8, 2), P(2, 2), 0.25f, LineType.Ray, LineType.Ray);

			// Endpoint of one line touches other line
			TestItsc(Seg(1, 1, 0, 5), Seg(1, 1, 5, 17), P(1, 1), 0, LineType.Segment, LineType.Segment);
			TestItsc(Seg(1, 1, 0, 5), Seg(0, 0, 4, 4),  P(1, 1), 0, LineType.Segment, LineType.Segment);
			TestItsc(Seg(0, 0, 4, 4), Seg(1, 1, 0, 5),  P(1, 1), 0.25f, LineType.Segment, LineType.Segment);

			// Regression test: rightward line + upward line (which is above and to the right)
			TestItsc(Seg(0, 0, 1, 0), Seg(10, 10, 10, 11), P(10, 0), 10, LineType.Infinite, LineType.Infinite);
		}
		[Test]
		public void ParallelAndDegenerateIntersectionTests()
		{
			// Lines and points
			TestItsc(Seg(0, 0, 0, 0), Seg(1, 1, 1, 1), null, float.NaN, LineType.Infinite, LineType.Infinite);
			TestItsc(Seg(1, 2, 3, 4), Seg(1, 1, 1, 1), null, float.NaN, LineType.Infinite, LineType.Infinite);
			TestItsc(Seg(0, 0, 0, 0), Seg(1, 2, 3, 4), null, float.NaN, LineType.Infinite, LineType.Infinite);
			TestItsc(Seg(1, 1, 1, 1), Seg(1, 1, 1, 1), P(1,1), 0.5f,    LineType.Infinite, LineType.Infinite);
			TestItsc(Seg(1, 1, 1, 1), Seg(0, 0, 3, 3), P(1,1), 0.5f);
			TestItsc(Seg(0, 0, 4, 4), Seg(1, 1, 1, 1), P(1,1), 0.25f);
			
			// Two identical lines
			TestItsc(Seg(1, 2, 4, 3), Seg(1, 2, 4, 3), P(2.5f,2.5f), 0.5f, LineType.Segment, LineType.Infinite);
			
			// Parallel lines, close together
			TestItsc(Seg(0, 0, 10, 10), Seg(4, 5, 6, 7), null, float.NaN, LineType.Segment, LineType.Segment);
			
			// Colinear lines meeting at a point
			TestItsc(Seg(1, 1, 5, 5), Seg(0, 0, 1, 1), P(1,1), 0,    LineType.Segment, LineType.Segment);
			TestItsc(Seg(1, 1, 5, 5), Seg(0, 0, 1, 1), P(1,1), 0,    LineType.Ray,     LineType.Segment);
			TestItsc(Seg(1, 1, 5, 5), Seg(0, 0, 1, 1), P(.5f,.5f), -0.125f, LineType.Infinite, LineType.Segment);
			TestItsc(Seg(1, 1, 5, 5), Seg(0, 0, 1, 1), P(3,3), 0.5f, LineType.Segment, LineType.Infinite);
			TestItsc(Seg(1, 1, 5, 5), Seg(0, 0, 1, 1), P(3,3), 0.5f, LineType.Segment, LineType.Ray);
			TestItsc(Seg(1, 1, 5, 5), Seg(0, 0, 1, 1), P(2.5f,2.5f), 0.375f, LineType.Infinite, LineType.Infinite);
			// Colinear lines meeting at a point, opposite slope
			TestItsc(Seg(1, 7, 7, 1), Seg(7, 1, 8, 0), P(7,1), 1,    LineType.Segment, LineType.Segment);
			TestItsc(Seg(5, 5, 1, 1), Seg(0, 0, 1, 1), P(1,1), 1,    LineType.Segment, LineType.Segment);
			// Colinear infinite lines but non-overlapping line segments
			TestItsc(Seg(5, 5, 2, 2), Seg(0, 0, 1, 1), null, float.NaN, LineType.Segment, LineType.Segment);
			TestItsc(Seg(4, 4, 2, 2), Seg(0, 0, 1, 1), P(2,2),       1f,    LineType.Ray, LineType.Ray);
			TestItsc(Seg(4, 4, 2, 2), Seg(0, 0, 1, 1), P(0.5f,0.5f), 1.75f, LineType.Ray, LineType.Segment);
			// Colinear, one line segment is fully inside the other
			TestItsc(Seg(9, 9, 1, 1), Seg(2, 2, 4, 4), P(3,3), 0.75f,  LineType.Segment, LineType.Segment);
			TestItsc(Seg(9, 9, 1, 1), Seg(3, 3, 4, 4), P(6,6), 0.375f, LineType.Ray, LineType.Ray);
		}
		private void TestItsc(LineSegment<float> p, LineSegment<float> q, Point<float>? expected, float expect_pFrac, LineType pt = LineType.Segment, LineType qt = LineType.Segment)
		{
			float pFrac, qFrac;
			bool intersected = p.ComputeIntersection(pt, out pFrac, q, qt, out qFrac);
			Assert.AreEqual(expected.HasValue, intersected);
			Point<float>? result = p.ComputeIntersection(pt, q, qt);
			Assert.AreEqual(expected, result);
			Assert.AreEqual(expect_pFrac, pFrac);
		}

		static Point<float> P(float x, float y) { return new Point<float>(x, y); }
		static LineSegment<float> Seg(float x1, float y1, float x2, float y2) { return new LineSegment<float>(x1, y1, x2, y2); }
	}
}
