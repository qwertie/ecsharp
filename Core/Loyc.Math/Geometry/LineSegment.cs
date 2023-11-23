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
		public LineSegment(T ax, T ay, T bx, T by) { A = new Point<T>(ax, ay); B = new Point<T>(bx, by); }
		
		public Point<T> A, B;
		
		public static implicit operator Pair<Point<T>, Point<T>>(LineSegment<T> seg) { return new Pair<Point<T>, Point<T>>(seg.A, seg.B); }
		public static implicit operator LineSegment<T>(Pair<Point<T>, Point<T>> seg) { return new LineSegment<T>(seg.A, seg.B); }
		public static explicit operator LineSegment<int>(LineSegment<T> seg) { return new LineSegment<int>((Point<int>)seg.A, (Point<int>)seg.B); }
		public static explicit operator LineSegment<long>(LineSegment<T> seg) { return new LineSegment<long>((Point<long>)seg.A, (Point<long>)seg.B); }
		public static explicit operator LineSegment<float>(LineSegment<T> seg) { return new LineSegment<float>((Point<float>)seg.A, (Point<float>)seg.B); }
		public static explicit operator LineSegment<double>(LineSegment<T> seg) { return new LineSegment<double>((Point<double>)seg.A, (Point<double>)seg.B); }
		public LineSegment<T> Reversed { get { return new LineSegment<T>(B, A); } }

		public override string ToString()
		{
			return string.Format("({0},{1})-({2},{3})", A.X, A.Y, B.X, B.Y);
		}
	}

	/// <summary>Holds a 3D line segment.</summary>
	/// <typeparam name="T">Coordinate type</typeparam>
	public struct LineSegment3<T> where T : IConvertible, IEquatable<T>
	{
		public LineSegment3(Point3<T> a, Point3<T> b) { A = a; B = b; }
		public LineSegment3(T ax, T ay, T az, T bx, T by, T bz) { A = new Point3<T>(ax, ay, az); B = new Point3<T>(bx, by, bz); }
		public Point3<T> A, B;
		public static implicit operator Pair<Point3<T>, Point3<T>>(LineSegment3<T> seg) { return new Pair<Point3<T>, Point3<T>>(seg.A, seg.B); }
		public static implicit operator LineSegment3<T>(Pair<Point3<T>, Point3<T>> seg) { return new LineSegment3<T>(seg.A, seg.B); }
		public static explicit operator LineSegment3<int>(LineSegment3<T> seg) { return new LineSegment3<int>((Point3<int>)seg.A, (Point3<int>)seg.B); }
		public static explicit operator LineSegment3<long>(LineSegment3<T> seg) { return new LineSegment3<long>((Point3<long>)seg.A, (Point3<long>)seg.B); }
		public static explicit operator LineSegment3<float>(LineSegment3<T> seg) { return new LineSegment3<float>((Point3<float>)seg.A, (Point3<float>)seg.B); }
		public static explicit operator LineSegment3<double>(LineSegment3<T> seg) { return new LineSegment3<double>((Point3<double>)seg.A, (Point3<double>)seg.B); }
		public LineSegment3<T> Reversed { get { return new LineSegment3<T>(B, A); } }
		
		public override string ToString()
		{
			return string.Format("({0},{1},{2})-({3},{4},{5})", A.X, A.Y, A.Z, B.X, B.Y, B.Z);
		}
	}
}
