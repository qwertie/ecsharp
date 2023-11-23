using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections.Impl;
using Loyc.MiniTest;

namespace Loyc.Geometry
{
	[TestFixture]
	public class LineMathTests : TestHelpers
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
		static BoundingBox<float> BBox(float x1, float y1, float x2, float y2) { return new BoundingBox<float>(x1, y1, x2, y2); }

		[Test]
		public void ClipToBBoxTests()
		{
			// No overlap between bounding boxes
			TestClip(Seg(-2, 3, 0, 4),     BBox(1, 1, 10, 10), null);  // Left
			TestClip(Seg(-2, 11, 0, 15),   BBox(1, 1, 10, 10), null);  // Up-left
			TestClip(Seg(5, 41, 3, 11),    BBox(1, 1, 10, 10), null);  // Up
			TestClip(Seg(12, 13, 14, 15),  BBox(1, 1, 10, 10), null);  // Up-right
			TestClip(Seg(19, 2, 11, 10),   BBox(1, 1, 10, 10), null);  // Right
			TestClip(Seg(13, 0, 11, -1),   BBox(1, 1, 10, 10), null);  // Down-right
			TestClip(Seg(1, -3, 9, -3e8f), BBox(1, 1, 10, 10), null);  // Down
			TestClip(Seg(-99,-99,-99, -1), BBox(1, 1, 10, 10), null);  // Down-left
			TestClip(Seg(-99,-99,9, -3),   BBox(1, 1, 10, 10), null);  // Down-ish
			TestClip(Seg(99, 99, -9, 11),  BBox(1, 1, 10, 10), null);  // Up-ish
			TestClip(Seg(-99, 99,0, -1),   BBox(1, 1, 10, 10), null);  // Left-ish
			TestClip(Seg(12, 5, 19, -3),   BBox(1, 1, 10, 10), null);  // Right-ish

			// Trivial non-clipped inputs
			TestClip(Seg(5, 5, 5, 5), BBox(1, 1, 10, 10), Seg(5, 5, 5, 5));
			TestClip(Seg(0, 0, 10, 10), BBox(0, 0, 10, 10), Seg(0, 0, 10, 10));
			TestClip(Seg(1, 5, 10, 5), BBox(1, 1, 10, 10), Seg(1, 5, 10, 5));
			TestClip(Seg(5, 1, 5, 10), BBox(1, 1, 10, 10), Seg(5, 1, 5, 10));

			// Tricky null output (bounding boxes overlap)
			TestClip(Seg(-9, 2, 5, -9), BBox(1, 1, 10, 10), null);
			TestClip(Seg(5, 55, 15, 0), BBox(1, 1, 10, 10), null);

			// X-clipping
			TestClip(Seg(-2, 4, 2, 5), BBox(0, 4, 5, 8), Seg(0, 4.5f, 2, 5));
			TestClip(Seg(2, 5, -2, 4), BBox(0, 4, 5, 8), Seg(2, 5, 0, 4.5f));
			TestClip(Seg(3, 4, 7, 5),  BBox(0, 4, 5, 8), Seg(3, 4, 5, 4.5f));
			TestClip(Seg(7, 5, 3, 4),  BBox(0, 4, 5, 8), Seg(5, 4.5f, 3, 4));
			TestClip(Seg(-5, 4, 15, 8), BBox(0, 4, 5, 8), Seg(0, 5, 5, 6));
			TestClip(Seg(15, 8, -5, 4), BBox(0, 4, 5, 8), Seg(5, 6, 0, 5));

			// Y-clipping
			TestClip(Seg(4, -2, 6, 2 ), BBox(4, 0, 8, 5), Seg(5, 0, 6, 2));
			TestClip(Seg(6, 2 , 4, -2), BBox(4, 0, 8, 5), Seg(6, 2, 5, 0));
			TestClip(Seg(4, 3 , 6, 7 ), BBox(4, 0, 8, 5), Seg(4, 3, 5, 5));
			TestClip(Seg(6, 7 , 4, 3 ), BBox(4, 0, 8, 5), Seg(5, 5, 4, 3));
			TestClip(Seg(4, -5, 8, 15), BBox(4, 0, 8, 5), Seg(5, 0, 6, 5));
			TestClip(Seg(8, 15, 4, -5), BBox(4, 0, 8, 5), Seg(6, 5, 5, 0));
			
			// X- and Y-clipping
			TestClip(Seg(-1, 2, 3, -2), BBox(0, 0, 8, 8), Seg(0, 1, 1, 0));
			
			// Corner case
			TestClip(Seg(-2, 2, 2, -2), BBox(0, 0, 8, 8), Seg(0, 0, 0, 0));
		}

		private void TestClip(LineSegment<float> seg, BoundingBox<float> bbox, LineSegment<float>? expected)
		{
			var result = seg.ClipTo(bbox);
			Assert.AreEqual(expected, result);
		}

		[Test]
		public void TestSimplifyPolyline_Example1()
		{
			TestSimplifyPolyline(
				new Point<float>[] {
					P(0, 0),
					P(20, 8),
					P(30, 11),
					P(50, 0),
					P(57, 100),
					P(50, 100),
				},
				10,
				P(0, 0), P(30, 11), P(50, 0), P(50, 100));
		}

		[Test]
		public void TestSimplifyPolyline_Example2()
		{
			TestSimplifyPolyline(
				new Point<float>[] {
					P(10, 0),
					P(15, 2),
					P(45, -2),
					P(50, 0),
					P(53, 40),
					P(54, 50),
					P(50, 100),
				},
				2,
				P(10, 0), P(50, 0), P(54, 50), P(50, 100));
		}

		[Test]
		public void TestSimplifyPolyline_Example3()
		{
			// This test verifies that the quadrance/distance calculation uses
			// "distance to line segment" and not "distance to infinite line".
			TestSimplifyPolyline(
				new Point<float>[] {
					P(10, 20),
					P(11, 40),
					P(11, 60),
					P(10, 50),
				},
				5,
				P(10, 20), P(11, 60), P(10, 50));
		}

		[Test]
		public void TestSimplifyPolyline_ColinearMiddlePointsAreAlwaysDeleted()
		{
			// Colinear middle points are always deleted
			foreach (float tolerance in new[] { 1f, 0f }) {
				TestSimplifyPolyline(
					new[] { P(50, 20), P(55, 10), P(51, 18), P(60,  0) },
					tolerance,
					P(50, 20), P(60, 0));
			}
		}

		[Test]
		public void TestSimplifyPolyline_DuplicatePointsArePartiallyDeduplicated()
		{
			TestSimplifyPolyline(
				new Point<float>[] {
					P(10, -1),
					P(10, -1),
					P(10, -1),
				},
				2,
				P(10, -1), P(10, -1));

			TestSimplifyPolyline(
				new Point<float>[] {
					P(10, 1),
					P(10, 1),
					P(10, 1),
					P(10, 2),
					P(10, 2),
					P(10, 2),
				},
				2,
				P(10, 1), P(10, 2));
		}

		void TestSimplifyPolyline(Point<float>[] polyline, float tolerance, params Point<float>[] expected)
		{
			// Act
			var output = LineMath.SimplifyPolyline(polyline, tolerance);

			// Assert
			ExpectList(output, expected);

			// Act 2: the double version behaves the same way as the float version
			var polylineD = polyline.Select(p => new Point<double>(p.X, p.Y)).ToArray();
			var expectedD = expected.Select(p => new Point<double>(p.X, p.Y)).ToArray();
			var outputD = LineMath.SimplifyPolyline(polylineD, tolerance);

			ExpectList(outputD, expectedD);

			// Act 3: After swapping coordinates, the simplification should be the same but swapped
			polyline = polyline.Select(p => new Point<float>(p.Y, p.X)).ToArray();
			expected = expected.Select(p => new Point<float>(p.Y, p.X)).ToArray();

			output = LineMath.SimplifyPolyline(polyline, tolerance);

			ExpectList(output, expected);

			// Act 4: After reversing the polyline, the simplification should be the same but reversed
			Array.Reverse(polyline);
			Array.Reverse(expected);

			output = LineMath.SimplifyPolyline(polyline, tolerance);

			ExpectList(output, expected);
		}

		[Test]
		public void TestSimplifyPolyline_VerySimpleInputsArePassedThrough()
		{
			// Two points or fewer are passed through unchanged
			var output = LineMath.SimplifyPolyline(new[] { P(1, 2), P(-1, -2) }, 10);

			ExpectList(output, P(1, 2), P(-1, -2));

			output = LineMath.SimplifyPolyline(new[] { P(1, 2) }, 10);

			ExpectList(output, P(1, 2));

			output = LineMath.SimplifyPolyline(new Point<float>[] { }, 10);

			ExpectList(output);
		}
	}
}
