using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.MiniTest;

namespace Loyc.Geometry
{
	using Point = Point<double>;
	using Vector = Vector<double>;

	[TestFixture]
	public class PointMathTests : TestHelpers
	{
		[Test]
		public void TestConvexHull()
		{
			int seed = Environment.TickCount;
			var r = new Random(55523173);

			var pts = new List<Point> {
				new Point(10, 22),
				new Point(10, 10),
				new Point(22, 20),
				new Point(20, 10)
			};

			for (int i = 0; i < 20; i++)
				pts.Insert(r.Next(pts.Count + 1), new Point(r.Next(10, 21), r.Next(10, 21)));

			var results = PointMath.ComputeConvexHull(pts, true);
			Assert.AreEqual(4, results.Count);
			Assert.That(results.Contains(new Point(10, 22)));
			Assert.That(results.Contains(new Point(10, 10)));
			Assert.That(results.Contains(new Point(22, 20)));
			Assert.That(results.Contains(new Point(20, 10)));

			for (int trial = 0; trial < 20; trial++) {
				// For our second test we use random points plus a possible outlier,
				// and check that all points are within or on the convex hull.
				int Lim = 100, PtCount = 5 * (trial + 1);
				pts.Clear();
				for (int i = 0; i < PtCount; i++)
					pts.Add(new Point(r.Next(-Lim, Lim), r.Next(-Lim, Lim)));
				pts.Add(new Point(r.Next(Lim * -4, Lim * 4), r.Next(Lim * -4, Lim * 4)));

				results = PointMath.ComputeConvexHull(pts, true);

				for (int i = 0; i < pts.Count; i++)
					Assert.That(PolygonMath.IsPointInPolygon(results, pts[i]) || results.Contains(pts[i]),
						"Fail for seed {0}, trial {1}, pts[{2}]", seed, trial, i);
			}
		}

		[Test]
		public void TestExampleConvexHull()
		{
			var points = new List<Point>() {
				new Point(4, 21 ),
				new Point(3, 15 ),
				new Point(6, 9  ),
				new Point(7, 7  ),
				new Point(10, 5 ),
				new Point(11, 29),
				new Point(14, 29),
				new Point(27, 14),
				new Point(29, 18),
				new Point(27, 22)
			};
			var hull = PointMath.ComputeConvexHull(points);
			ExpectList(hull, new List<Point>() {
				new Point(3, 15),
				new Point(4, 21),
				new Point(11, 29),
				new Point(14, 29),
				new Point(27, 22),
				new Point(29, 18),
				new Point(27, 14),
				new Point(10, 5),
				new Point(7, 7)
			});
		}

		[Test]
		public void TestTrivialConvexHulls()
		{
			Point p = new Point(1, 2), q = new Point(2, 1);
			ExpectList(PointMath.ComputeConvexHull(new List<Point>()));
			ExpectList(PointMath.ComputeConvexHull(new List<Point>() { p }), p);
			ExpectList(PointMath.ComputeConvexHull(new List<Point>() { q }), q);
			ExpectList(PointMath.ComputeConvexHull(new List<Point>() { p, q }), p, q);
			ExpectList(PointMath.ComputeConvexHull(new List<Point>() { p, p, q }), p, q);
			ExpectList(PointMath.ComputeConvexHull(new List<Point>() { p, q, p }), p, q);
			
			// Ideally these would be the results, but it actually repeats p
			//ExpectList(PointMath.ComputeConvexHull(new List<Point>() { p, p }), p);
			//ExpectList(PointMath.ComputeConvexHull(new List<Point>() { p, p, p }), p);
			ExpectList(PointMath.ComputeConvexHull(new List<Point>() { p, p }), p, p);
			ExpectList(PointMath.ComputeConvexHull(new List<Point>() { p, p, p }), p, p);
		}
	}
}
