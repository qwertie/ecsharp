using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Geometry
{
	/// <summary>Holds a 2D line segment.</summary>
	/// <typeparam name="T">Coordinate type</typeparam>
	public struct LineSegment<T> where T : IConvertible, IEquatable<T>
	{
		public LineSegment(Point<T> a, Point<T> b) { A = a; B = b; }
		public Point<T> A, B;
		public static implicit operator Pair<Point<T>, Point<T>>(LineSegment<T> seg) { return new Pair<Point<T>, Point<T>>(seg.A, seg.B); }
		public static implicit operator LineSegment<T>(Pair<Point<T>, Point<T>> seg) { return new LineSegment<T>(seg.A, seg.B); }
	}

	/// <summary>Holds a 3D line segment.</summary>
	/// <typeparam name="T">Coordinate type</typeparam>
	public struct LineSegment3<T> where T : IConvertible, IEquatable<T>
	{
		public LineSegment3(Point3<T> a, Point3<T> b) { A = a; B = b; }
		public Point3<T> A, B;
		public static implicit operator Pair<Point3<T>, Point3<T>>(LineSegment3<T> seg) { return new Pair<Point3<T>, Point3<T>>(seg.A, seg.B); }
		public static implicit operator LineSegment3<T>(Pair<Point3<T>, Point3<T>> seg) { return new LineSegment3<T>(seg.A, seg.B); }
	}
}
