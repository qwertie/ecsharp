using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Math;

namespace Loyc.Geometry
{
	/// <summary>Interface for reading the size of a 2D object.</summary>
	/// <typeparam name="T">Coordinate type.</typeparam>
	public interface ISizeReader<T>
	{
		/// <summary>Gets the width of a rectangle (the difference between X coordinates).</summary>
		T Width { get; }
		/// <summary>Gets the height of a rectangle (the difference between Y coordinates).</summary>
		T Height { get; }
	}

	/// <summary>Represents a read-only 2D rectangle.</summary>
	/// <remarks>
	/// The relationship between X1, X2 and Width, and the relationship between 
	/// Y1, Y2 and Height, depends on whether the object represents a normal 
	/// (traditional Windows) rectangle, a bounding rectangle, or a pair of 
	/// points. A traditional rectangle defines its Height as Y2 - Y1, and its
	/// Width as X2 - X1. A bounding rectangle is defined slightly differently:
	/// the Width is X2 - X1 + e, and the Height is Y2 - Y1 + e, where e is an
	/// infitessimal value of type T (e=1 if T is an integer type).
	/// <para/>
	/// Also, a traditional rectangle is stored as a starting point (X1, Y1)
	/// and a size (Width, Height), while a bounding rectangle stores X2 and Y2 
	/// instead, not the Width and Height.
	/// <para/>
	/// Finally, this interface could simply represent a pair of points. In 
	/// that case, Width and Height return the absolute value of X2-X1 and 
	/// Y2-Y1 respectively.
	/// <para/>
	/// A rectangle may or may not require X2 >= X1 and Y2 >= Y1. If X1>X2 or
	/// Y1>Y2, the rectangle is said to be "not normalized" and the 
	/// <see cref="RectangleExt.IsNormal"/> extension method returns false.
	/// </remarks>
	public interface IRectangleReader<T> : ISizeReader<T>
	{
		T X1 { get; }
		T Y1 { get; }
		T X2 { get; }
		T Y2 { get; }
	}

	/// <summary>Represents a mutable 2D rectangle.</summary>
	/// <typeparam name="T">Type of each coordinate.</typeparam>
	/// <remarks>
	/// This interface can represent one of three types of rectangles: either 
	/// "normal" rectangles with fundamental variables X, Y, Width, Height, 
	/// bounding rectangles with two coordinate ranges (X1-X2) and (Y1-Y2), or
	/// or pairs of points (X1, Y1) and (X2, Y2).
	/// <para/>
	/// Because of this fact, it is not clear when you modify X1 whether this
	/// should affect X2 or Width. Similarly, when you modify X2, Y1 or Y2, it
	/// is not clear whether the size property or the opposite boundary should 
	/// change. To resolve this question, this interface does not allow you to
	/// modify the coordinates individually; instead you must change them in 
	/// pairs: you either set X and Width together, or X1 and X2 together; and
	/// similarly Y and Height together, or Y1 and Y2 together.
	/// <para/>
	/// You can also call the extension methods SetRect() or SetRange() to set
	/// all four coordinates at once.
	/// </remarks>
	/// <seealso cref="IRectangle{T}"/>
	/// <seealso cref="BoundingBox{T}"/>
	public interface IRectangleBase<T> : IRectangleReader<T>
	{
		void SetXAndWidth(T x, T width);
		void SetYAndHeight(T y, T height);
		void SetXRange(T x1, T x2);
		void SetYRange(T x1, T x2);
	}
	
	/// <summary>This interface exists to work around a limitation of C#; see
	/// <see cref="IRectangle{T}"/>.</summary>
	public interface INewRectangle<Rect, T>
	{
		Rect NewRect(T x, T y, T width, T height);
		Rect NewRange(T x1, T y1, T x2, T y2);
	}
	
	/// <summary>Represents a mutable 2D rectangle.</summary>
	/// <remarks>
	/// This interface is separated into two bases, 
	/// <see cref="IRectangleBase{T}"/> and <see cref="INewRectangle{R,T}"/>, 
	/// for the same reason that <see cref="IPoint{T}"/>'s coordinates are 
	/// divided into <see cref="IPointBase{T}"/> and <see cref="INewPoint{P,T}"/>,
	/// as explained in the documentation of <see cref="IPoint{T}"/>. 
	/// </remarks>
	public interface IRectangle<T> : IRectangleBase<T>, INewRectangle<IRectangle<T>, T>
	{
	}

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
