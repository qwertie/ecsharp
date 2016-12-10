using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Math;

namespace Loyc.Geometry
{
	/// <summary>Contains methods to manipulate rectangles.</summary>
	/// <remarks>Unfortunately, C# often can't infer the type parameters. Many of 
	/// these methods must be called with explicit type parameters.</remarks>
	public static class RectangleExt
	{
		public static bool IsNormal<Rect, T>(this Rect r)
			where Rect : IRectangleReader<T>
			where T : IComparable<T>
		{
			// Hey Microsoft, this would probably be faster if the built-in types 
			// implemented simple boolean methods: IsLess(), IsLessOrEqual().
			return r.X2.CompareTo(r.X1) >= 0 && r.Y2.CompareTo(r.Y1) >= 0;
		}
		public static void Normalize<Rect, T>(this Rect r)
			where Rect : IRectangleBase<T>
			where T : IComparable<T>
		{
			T x1 = r.X1, x2 = r.X2;
			if (r.X2.CompareTo(r.X1) < 0)
				r.SetXRange(x2, x1);
			T y1 = r.Y1, y2 = r.Y2;
			if (r.Y2.CompareTo(r.Y1) < 0)
				r.SetYRange(y2, y1);
		}

		/// <summary>Computes the union of two <i>normalized</i> rectangles,
		/// i.e. a rectangle large enough to cover both original rectangles.</summary>
		/// <remarks>The results may be incorrect if one or both rectangles 
		/// are not normalized.</remarks>
		public static Rect Union<Rect,T>(this Rect a, Rect b)
			where Rect : IRectangleBase<T>, INewRectangle<Rect, T>
			where T : IComparable<T>
		{
			return a.NewRange(MathEx.Min(a.X1, b.X1), MathEx.Min(a.Y1, b.Y1),
			                  MathEx.Max(a.X2, b.X2), MathEx.Max(a.Y2, b.Y2));
		}

		/// <summary>Computes the intersection of two <i>normalized</i> rectangles,
		/// i.e. the region covered by both original rectangles.</summary>
		/// <remarks>The results may be incorrect if one or both rectangles are
		/// not normalized. If the rectangles do not overlap, a non-normalized 
		/// rectangle is returned.</remarks>
		public static Rect Intersect<Rect,T>(this Rect a, Rect b)
			where Rect : IRectangleBase<T>, INewRectangle<Rect, T>
			where T : IComparable<T>
		{
			return a.NewRange(MathEx.Max(a.X1, b.X1), MathEx.Max(a.Y1, b.Y1),
			                  MathEx.Min(a.X2, b.X2), MathEx.Min(a.Y2, b.Y2));
		}

		/// <summary>Expands a rectangle's boundaries to include a given point.</summary>
		/// <returns>Returns true if the rectangle changed.</returns>
		public static bool ExpandToInclude<Rect,Point,T>(this Rect a, Point b)
			where Point : IPointReader<T>
			where Rect : IRectangleBase<T>, INewRectangle<Rect, T>
			where T : IComparable<T>
		{
			T x1 = a.X1, y1 = a.Y1;
			T x2 = a.X2, y2 = a.Y2;
			bool changed = false;
			if (x1.CompareTo(b.X) > 0) {
				x1 = b.X;
				changed = true;
			}
			if (x2.CompareTo(b.X) < 0) {
				x2 = b.X;
				changed = true;
			}
			if (y1.CompareTo(b.Y) > 0) {
				y1 = b.Y;
				changed = true;
			}
			if (y2.CompareTo(b.Y) < 0) {
				y2 = b.Y;
				changed = true;
			}
			if (changed)
				a.SetRange(x1, y1, x2, y2);
			return changed;
		}
		
		/// <summary>Expands a rectangle's boundaries to include a series of points.</summary>
		/// <returns>Returns true if the rectangle changed.</returns>
		public static bool ExpandToInclude<Rect, Point, T>(this Rect a, IEnumerable<Point> bs)
			where Point : IPointReader<T>
			where Rect : IRectangleBase<T>, INewRectangle<Rect, T>
			where T : IComparable<T>
		{
			bool changed = false;
			foreach (Point b in bs)
				changed |= ExpandToInclude<Rect, Point, T>(a, b);
			return changed;
		}

		/// <summary>Determines whether a rectangle contains a given point.</summary>
		/// <remarks>Returns false if the rectangle is not normalized.</remarks>
		public static bool Contains<Rect, Point, T>(this Rect a, Point b)
			where Point : IPointReader<T>
			where Rect : IRectangleBase<T>, INewRectangle<Rect, T>
			where T : IComparable<T>
		{
			return a.X1.CompareTo(b.X) <= 0 && a.X2.CompareTo(b.X) >= 0 &&
				   a.Y1.CompareTo(b.Y) <= 0 && a.Y2.CompareTo(b.Y) >= 0;
		}
		
		/// <summary>Determines whether a rectangle fully contains another rectangle.</summary>
		/// <remarks>May not work correctly if either of the rectangles is not normalized.</remarks>
		public static bool Contains<Rect, T>(this Rect a, Rect b)
			where Rect : IRectangleBase<T>, INewRectangle<Rect, T>
			where T : IComparable<T>
		{
			return a.X1.CompareTo(b.X1) <= 0 && a.X2.CompareTo(b.X2) >= 0 &&
				   a.Y1.CompareTo(b.Y1) <= 0 && a.Y2.CompareTo(b.Y2) >= 0;
		}
		
		/// <summary>Determines whether a rectangle overlaps another rectangle.</summary>
		/// <remarks>May not work correctly if either of the rectangles is not normalized.</remarks>
		public static bool Overlaps<Rect, T>(this Rect a, Rect b)
			where Rect : IRectangleBase<T>, INewRectangle<Rect, T>
			where T : IComparable<T>
		{
			return IsNormal<Rect, T>(Intersect<Rect, T>(a, b));
		}

		public static void SetRect<Rect, T>(this Rect r, T x, T y, T width, T height)
			where Rect : IRectangleBase<T>
		{
			r.SetXAndWidth(x, width);
			r.SetYAndHeight(y, height);
		}
		public static void SetRange<Rect, T>(this Rect r, T x1, T y1, T x2, T y2)
			where Rect : IRectangleBase<T>
		{
			r.SetXRange(x1, x2);
			r.SetYRange(y1, y2);
		}
	}
}
