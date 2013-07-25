using Loyc.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Loyc.Geometry
{
	public static partial class LineMath
	{
		public static T Length<T>(this LineSegment<T> seg) where T : IConvertible, IEquatable<T>
		{
			return seg.Vector().Length();
		}

		static readonly Func<Point<float>,  Point<float>,  Point<float>,  float>  _quadranceToLineF = (p, p0, p1) => (p - ProjectOnto(p, p0.To(p1), LineType.Segment)).Quadrance();
		static readonly Func<Point<double>, Point<double>, Point<double>, double> _quadranceToLineD = (p, p0, p1) => (p - ProjectOnto(p, p0.To(p1), LineType.Segment)).Quadrance();

		/// <summary>Simplifies a polyline using the Douglas-Peucker line 
		/// simplification algorithm. This algorithm removes points that are 
		/// deemed unimportant; the output is a subset of the input.</summary>
		/// <typeparam name="List">Original unsimplified polyline</typeparam>
		/// <param name="output">The output polyline is added in order to this collection</param>
		/// <param name="tolerance">The distance between the input polyline and the 
		/// output polyline will never exceed this distance. Increase this value to 
		/// simplify more aggressively.</param>
		/// <returns>The number of output points.</returns>
		/// <remarks>
		/// The average time complexity of this algorithm is O(N log N). 
		/// The worst-case time complexity is O(N^2).
		/// </remarks>
		public static int SimplifyPolyline<List>(List points, ICollection<Point<float>> output, float tolerance) where List : IListSource<Point<float>>
		{
			return SimplifyPolyline(points, output, tolerance * tolerance, _quadranceToLineF);
		}
		public static List<Point<float>> SimplifyPolyline<List>(List points, float tolerance) where List : IListSource<Point<float>>
		{
			var output = new List<Point<float>>();
			SimplifyPolyline(points, output, tolerance * tolerance, _quadranceToLineF);
			return output;
		}
		/// <inheritdoc cref="SimplifyPolyline{List}(List points, ICollection<Point<float>> output, float tolerance)"/>
		public static int SimplifyPolyline<List>(List points, ICollection<Point<double>> output, double tolerance) where List : IListSource<Point<double>>
		{
			return SimplifyPolyline(points, output, tolerance * tolerance, _quadranceToLineD);
		}
		public static List<Point<double>> SimplifyPolyline<List>(List points, double tolerance) where List : IListSource<Point<double>>
		{
			var output = new List<Point<double>>();
			SimplifyPolyline(points, output, tolerance * tolerance, _quadranceToLineD);
			return output;
		}
		public static int SimplifyPolyline<List, Point, T>(List points, ICollection<Point> output, T tolerance, Func<Point, Point, Point, T> distanceToLine, bool inRecursion = false)
			where List : IListSource<Point> where T : IComparable<T>
		{
			int c = points.Count;
			if (c <= 2) {
				if (c > 0) {
					output.Add(points[0]);
					if (c > 1)
						output.Add(points[1]);
				}
				return c;
			}

			int iFarthest = -1;
			T maxDist = tolerance;
			Point from = points[0], to = points[c-1];
			for (int i = 1; i < c - 1; i++)
			{
				var dist = distanceToLine(points[i], from, to);
				if (maxDist.CompareTo(dist) < 0) {
					maxDist = dist;
					iFarthest = i;
				}
			}
			if (iFarthest == -1) {
				output.Add(from);
				if (inRecursion)
					return 1;
				output.Add(to);
				return 2;
			} else {
				int count = SimplifyPolyline(points.Slice(0, iFarthest + 1), output, tolerance, distanceToLine, true);
				Debug.Assert(true);//TEMP
				count    += SimplifyPolyline(points.Slice(iFarthest), output, tolerance, distanceToLine, inRecursion);
				return count;
			}
		}
	}
}
