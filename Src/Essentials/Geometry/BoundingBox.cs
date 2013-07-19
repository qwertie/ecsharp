using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Math;

namespace Loyc.Geometry
{
	/// <summary>Holds a mutable 2D bounding rectangle.</summary>
	/// <typeparam name="T">Data type of each coordinate.</typeparam>
	/// <remarks>
	/// Many extension methods are provided in <see cref="BoundingBoxExt"/>.
	/// They are separate from the class itself so that specialized versions are
	/// available for particular types (notably int, double and float).
	/// <para/>
	/// X1 and Y1 should contains the minimum coordinates, while X2 and Y2 
	/// should contain the maximum coordinates. BoundingBox generally does not 
	/// guarantee that this is true unless you call <see cref="Normalize()"/>,
	/// or if you use a constructor that accepts two points (rather than 
	/// explicit minimum and maximum coordinates.) Some methods will not work
	/// correctly if the "maximum" coordinate is less than the "minimum" 
	/// coordinate; they may simply assume that the maximum exceeds the 
	/// minumum.
	/// <para/>
	/// A BoundingBox is considered to include both the minimum and maximum 
	/// coordinates. A point on the border is considered to be within the 
	/// bounding box, and the Width and Height are incremented to account
	/// for this. For example, if T is int and (X1,X2)=(0,10), the Width is
	/// 11; if T is float and (X1,X2)=(0,10), the Width is infitessimally
	/// more than 10 (the result is incremented with MathEx.NextHigher()).
	/// </remarks>
	public class BoundingBox<T> : IRectangle<T>, INewRectangle<BoundingBox<T>, T>, ICloneable<BoundingBox<T>>
		where T : IConvertible, IComparable<T>, IEquatable<T>
	{
		static IAdditionGroup<T> ag = Maths<T>.AdditionGroup;
		static IInrementer<T> inc = Maths<T>.Inrementer;
		static INumTraits<T> traits = Maths<T>.Traits;
		static IMath<T> m = Maths<T>.Math;

		public BoundingBox(T minX, T minY, T maxX, T maxY)
			{ _minX = minX; _minY = minY; _maxX = maxX; _maxY = maxY; }
		public BoundingBox(Point<T> p)
			{ _minX = p.X; _minY = p.Y; _maxX = p.X; _maxY = p.Y; }
		public BoundingBox(Point<T> p1, Point<T> p2)
			{ _minX = p1.X; _minY = p1.Y; _maxX = p2.X; _maxY = p2.Y; this.Normalize(); }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)] // reduce clutter
		private T _minX, _minY, _maxX, _maxY;

		public T X1 { get { return _minX; } set { _minX = value; } }
		public T Y1 { get { return _minY; } set { _minY = value; } }
		public T X2 { get { return _maxX; } set { _maxX = value; } }
		public T Y2 { get { return _maxY; } set { _maxY = value; } }

		public T Width
		{
			get { return inc.NextHigher(ag.Sub(_maxX, _minX)); }
			set { X2 = inc.NextLower(ag.Add(X1, value)); }
		}
		public T Height
		{
			get { return inc.NextHigher(ag.Sub(_maxY, _minY)); }
			set { Y2 = inc.NextLower(ag.Add(Y1, value)); }
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)] // reduce clutter
		public Point<T> MinPoint
		{
			get { return new Point<T>(_minX, _minY); }
			set { _minX = value.X; _minY = value.Y; }
		}
		[DebuggerBrowsable(DebuggerBrowsableState.Never)] // reduce clutter
		public Point<T> MaxPoint
		{
			get { return new Point<T>(_maxX, _maxY); }
			set { _maxX = value.X; _maxY = value.Y; }
		}

		public BoundingBox<T> NewRect(T x, T y, T width, T height)
		{
			return new BoundingBox<T>(x, y, inc.NextLower(ag.Add(x, width)), inc.NextLower(ag.Add(y, height)));
		}
		public BoundingBox<T> NewRange(T x1, T y1, T x2, T y2)
		{
			return new BoundingBox<T>(x1, y1, x2, y2);
		}
		IRectangle<T> INewRectangle<IRectangle<T>, T>.NewRect(T x, T y, T width, T height)
		{
			return new BoundingBox<T>(x, y, inc.NextLower(ag.Add(x, width)), inc.NextLower(ag.Add(y, height)));
		}
		IRectangle<T> INewRectangle<IRectangle<T>, T>.NewRange(T x1, T y1, T x2, T y2)
		{
			return new BoundingBox<T>(x1, y1, x2, y2);
		}

		public void SetXAndWidth(T x, T width)
		{
			SetXRange(x, inc.NextLower(ag.Add(x, width)));
		}
		public void SetYAndHeight(T y, T height)
		{
			SetYRange(y, inc.NextLower(ag.Add(y, height)));
		}
		public void SetXRange(T x1, T x2)
		{
			_minX = x1;
			_maxX = x2;
		}
		public void SetYRange(T y1, T y2)
		{
			_minY = y1;
			_maxY = y2;
		}
		public bool IsNormal()
		{
			return _maxX.CompareTo(_minX) >= 0 && _maxY.CompareTo(_minY) >= 0;
		}

		public void SetRect(T x, T y, T width, T height)
		{
			SetXAndWidth(x, width);
			SetYAndHeight(y, height);
		}
		public void SetRange(T x1, T y1, T x2, T y2)
		{
			SetXRange(x1, x2);
			SetYRange(y1, y2);
		}

		public BoundingBox<T> Clone()
		{
			return new BoundingBox<T>(X1, Y1, X2, Y2);
		}
	}

	public static class BoundingBoxExt
	{
		public static void Normalize<T>(this BoundingBox<T> self) where T : IConvertible, IComparable<T>, IEquatable<T>
		{
			RectangleExt.Normalize<BoundingBox<T>, T>(self);
		}
		public static BoundingBox<T> Union<T>(this BoundingBox<T> self, BoundingBox<T> b) where T : IConvertible, IComparable<T>, IEquatable<T>
		{
			return RectangleExt.Union<BoundingBox<T>, T>(self, b);
		}
		public static BoundingBox<T> Intersect<T>(this BoundingBox<T> self, BoundingBox<T> b) where T : IConvertible, IComparable<T>, IEquatable<T>
		{
			return RectangleExt.Intersect<BoundingBox<T>, T>(self, b);
		}
		public static bool ExpandToInclude<T>(this BoundingBox<T> self, Point<T> point) where T : IConvertible, IComparable<T>, IEquatable<T>
		{
			return RectangleExt.ExpandToInclude<BoundingBox<T>, Point<T>, T>(self, point);
		}
		public static bool ExpandToInclude<T>(this BoundingBox<T> self, IEnumerable<Point<T>> points) where T : IConvertible, IComparable<T>, IEquatable<T>
		{
			return RectangleExt.ExpandToInclude<BoundingBox<T>, Point<T>, T>(self, points);
		}
		public static bool Contains<T>(this BoundingBox<T> self, Point<T> point) where T : IConvertible, IComparable<T>, IEquatable<T>
		{
			return RectangleExt.Contains<BoundingBox<T>, Point<T>, T>(self, point);
		}
		public static bool Contains<T>(this BoundingBox<T> self, BoundingBox<T> other) where T : IConvertible, IComparable<T>, IEquatable<T>
		{
			return RectangleExt.Contains<BoundingBox<T>, T>(self, other);
		}
		public static bool Overlaps<T>(this BoundingBox<T> self, BoundingBox<T> other) where T : IConvertible, IComparable<T>, IEquatable<T>
		{
			return RectangleExt.Overlaps<BoundingBox<T>, T>(self, other);
		}
		public static void Inflate<T>(this BoundingBox<T> self, T amount) where T : IConvertible, IComparable<T>, IEquatable<T>
		{
			Inflate(self, amount, amount);
		}
		public static void Deflate<T>(this BoundingBox<T> self, T amount) where T : IConvertible, IComparable<T>, IEquatable<T>
		{
			amount = Maths<T>.SignedMath.Negate(amount);
			Inflate(self, amount, amount);
		}
		public static void Deflate<T>(this BoundingBox<T> self, T amountX, T amountY) where T : IConvertible, IComparable<T>, IEquatable<T>
		{
			var m = Maths<T>.SignedMath;
			Inflate(self, m.Negate(amountX), m.Negate(amountY));
		}
		public static void Inflate<T>(this BoundingBox<T> self, T amountX, T amountY) where T : IConvertible, IComparable<T>, IEquatable<T>
		{
			var m = Maths<T>.SignedMath;
			if (amountX.CompareTo(m.Zero) < 0 && m.Shl(m.Negate(amountX), 1).CompareTo(self.Width) >= 0)
				self.SetXAndWidth(MathEx.Average(self.X1, self.X2), m.Zero);
			else
				self.SetXAndWidth(m.Sub(self.X1, amountX), m.Sub(self.X2, amountX));
			if (amountY.CompareTo(m.Zero) < 0 && m.Shl(m.Negate(amountY), 1).CompareTo(self.Width) >= 0)
				self.SetYAndHeight(MathEx.Average(self.Y1, self.Y2), m.Zero);
			else 
				self.SetYAndHeight(m.Sub(self.Y1, amountY), m.Sub(self.Y2, amountY));
		}
		public static BoundingBox<T> Inflated<T>(this BoundingBox<T> self, T amountX, T amountY) where T : IConvertible, IComparable<T>, IEquatable<T>
		{
			var copy = self.Clone();
			Inflate(copy, amountX, amountY);
			return copy;
		}
		public static BoundingBox<T> Deflated<T>(this BoundingBox<T> self, T amountX, T amountY) where T : IConvertible, IComparable<T>, IEquatable<T>
		{
			var copy = self.Clone();
			Deflate(copy, amountX, amountY);
			return copy;
		}
		
		public static Point<T> Center<T>(this BoundingBox<T> self) where T : IConvertible, IComparable<T>, IEquatable<T>
		{
			return new Point<T>(MathEx.Average(self.X1, self.X2), MathEx.Average(self.Y1, self.Y2));
		}

		public static BoundingBox<T> ToBoundingBox<T>(this IEnumerable<Point<T>> pts) where T : IConvertible, IComparable<T>, IEquatable<T>
		{
			return ToBoundingBox(pts.GetEnumerator());
		}
		public static BoundingBox<T> ToBoundingBox<T>(this IEnumerator<Point<T>> e) where T : IConvertible, IComparable<T>, IEquatable<T>
		{
			if (!e.MoveNext())
				return null;
			var bb = new BoundingBox<T>(e.Current);
			while (e.MoveNext())
				RectangleExt.ExpandToInclude<BoundingBox<T>, Point<T>, T>(bb, e.Current);
			return bb;
		}
		public static BoundingBox<T> ToBoundingBox<T>(this LineSegment<T> seg) where T : IConvertible, IComparable<T>, IEquatable<T>
		{
			return new BoundingBox<T>(seg.A, seg.B);
		}
		public static System.Drawing.Rectangle ToBCL(this BoundingBox<int> bbox)
		{
			return new System.Drawing.Rectangle(bbox.X1, bbox.Y1, bbox.X2 - bbox.X1, bbox.Y2 - bbox.Y1);
		}
		public static System.Drawing.RectangleF ToBCL(this BoundingBox<float> bbox)
		{
			return new System.Drawing.RectangleF(bbox.X1, bbox.Y1, bbox.X2 - bbox.X1, bbox.Y2 - bbox.Y1);
		}
	}
}
