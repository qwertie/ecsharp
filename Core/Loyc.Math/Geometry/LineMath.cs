using Loyc.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Loyc.Geometry
{
	/// <summary>Contains algorithms that operate on lines.</summary>
	public static partial class LineMath
	{
		public static T Length<T>(this LineSegment<T> seg) where T : IConvertible, IEquatable<T>
		{
			return seg.Vector().Length();
		}

		/// <summary>Simplifies a polyline using the Douglas-Peucker line 
		///   simplification algorithm. This algorithm removes points that are 
		///   deemed unimportant, so the output is a subset of the input.</summary>
		/// <typeparam name="List">Original unsimplified polyline</typeparam>
		/// <param name="output">The output polyline is added (in order) to this collection</param>
		/// <param name="tolerance">The distance between the input polyline and the 
		///   output polyline will never exceed this distance. Increase this value to 
		///   simplify more aggressively.</param>
		/// <returns>The number of output points.</returns>
		/// <remarks>
		///   The average time complexity of this algorithm is O(N log N). 
		///   The worst-case time complexity is O(N^2).
		/// </remarks>
		public static int SimplifyPolyline<List>(List points, ICollection<Point<float>> output, float tolerance) where List : IReadOnlyList<Point<float>>
		{
			return SimplifyPolyline(points, 0, points.Count, output, tolerance * tolerance, _quadranceToLineF);
		}
		public static List<Point<float>> SimplifyPolyline<List>(List points, float tolerance) where List : IReadOnlyList<Point<float>>
		{
			var output = new List<Point<float>>();
			SimplifyPolyline(points, 0, points.Count, output, tolerance * tolerance, _quadranceToLineF);
			return output;
		}

		/// <inheritdoc cref="SimplifyPolyline{List}(List, ICollection{Point{float}}, float)"/>
		public static int SimplifyPolyline<List>(List points, ICollection<Point<double>> output, double tolerance) where List : IReadOnlyList<Point<double>>
		{
			return SimplifyPolyline(points, 0, points.Count, output, tolerance * tolerance, _quadranceToLineD);
		}
		public static List<Point<double>> SimplifyPolyline<List>(List points, double tolerance) where List : IReadOnlyList<Point<double>>
		{
			var output = new List<Point<double>>();
			SimplifyPolyline(points, 0, points.Count, output, tolerance * tolerance, _quadranceToLineD);
			return output;
		}

		static readonly Func<Point<float>, Point<float>, Point<float>, float> _quadranceToLineF
			= (p, p0, p1) => p.Sub(ProjectOnto(p, new LineSegment<float>(p0, p1), LineType.Segment)).Quadrance();
		static readonly Func<Point<double>, Point<double>, Point<double>, double> _quadranceToLineD
			= (p, p0, p1) => p.Sub(ProjectOnto(p, new LineSegment<double>(p0, p1), LineType.Segment)).Quadrance();

		public static int SimplifyPolyline<List, Point, T>(
			List points, int iStart, int iStop,
			ICollection<Point> output, T tolerance,
			Func<Point, Point, Point, T> distanceToLine)
			where List : IReadOnlyList<Point> where T : IComparable<T>
		{
			int iLast = iStop - 1;
			if (iStart >= iLast) {
				if (iStart == iLast)
					output.Add(points[iStart]);

				return iStop - iStart;
			}

			// Run Douglas-Peucker algorithm
			int count = SimplifyRecursively(points, iStart, iLast, output, tolerance, distanceToLine);

			// The last point is not included by `SimplifyRecursively`, so add it afterward.
			output.Add(points[iLast]);
			return count + 1;
			
			static int SimplifyRecursively(List points, int iFirst, int iLast, ICollection<Point> output, T tolerance, Func<Point, Point, Point, T> distanceToLine)
			{
				Debug.Assert(iFirst < iLast);

				Point first = points[iFirst];
				int i = iFirst + 1;
				if (i < iLast) {
					Point last = points[iLast];
					T maxDist = tolerance;
					int iFarthest = -1;
					do {
						var dist = distanceToLine(points[i], first, last);
						if (maxDist.CompareTo(dist) < 0) {
							maxDist = dist;
							iFarthest = i;
						}
					} while (++i < iLast);

					if (iFarthest != -1) {
						int count = SimplifyRecursively(points, iFirst, iFarthest, output, tolerance, distanceToLine);
						count += SimplifyRecursively(points, iFarthest, iLast, output, tolerance, distanceToLine);
						return count;
					}
				}
				
				output.Add(first);
				return 1;
			}
		}
	}
}
