using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Loyc.Math
{
	/// <summary>Holds a mutable 2D bounding rectangle.</summary>
	/// <typeparam name="T">Data type of each coordinate.</typeparam>
	/// <remarks>Several extension methods are provided in <see cref="BoundingBoxExt"/>.</remarks>
	public class BoundingBox<T> : IRectangle<T>, INewRectangle<BoundingBox<T>,T>
		where T : IConvertible, IComparable<T>, IEquatable<T>
	{
		static IAdditionGroup<T> m = Maths<T>.IAdditionGroup;
		static IInrementer<T> inc = Maths<T>.IInrementer;
		static INumTraits<T> traits = Maths<T>.Traits;

		public BoundingBox(T minX, T minY, T maxX, T maxY) 
			{ _minX = minX; _minY = minY; _maxX = maxX; _maxY = maxY; }
		public BoundingBox(Point<T> p) 
			{ _minX = p.X; _minY = p.Y; _maxX = p.X; _maxY = p.Y; }
		public BoundingBox(Point<T> p1, Point<T> p2)
			{ _minX = p1.X; _minY = p1.Y; _maxX = p2.X; _maxY = p2.Y; Normalize(); }
		public BoundingBox(IEnumerable<Point<T>> pts)
		{
			var e = pts.GetEnumerator();
			if (!e.MoveNext())
			{
				_minX = _minY = _maxX = _maxY = traits.NaN;
				return;
			}
			var p = e.Current;
			_minX = p.X; _minY = p.Y; _maxX = p.X; _maxY = p.Y;

			while (e.MoveNext())
				RectangleExt.ExpandToInclude<BoundingBox<T>,Point<T>,T>(this, e.Current);
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)] // reduce clutter
		private T _minX, _minY, _maxX, _maxY;

		public T X1 { get { return _minX; } set { _minX = value; } }
		public T Y1 { get { return _minY; } set { _minY = value; } }
		public T X2 { get { return _maxX; } set { _maxX = value; } }
		public T Y2 { get { return _maxY; } set { _maxY = value; } }

		public T Width
		{
			get { return inc.NextHigher(m.Subtract(_maxX, _minX)); }
			set { X2 = inc.NextLower(m.Add(X1, value)); }
		}
		public T Height 
		{
			get { return inc.NextHigher(m.Subtract(_maxY, _minY)); }
			set { Y2 = inc.NextLower(m.Add(Y1, value)); }
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
			return new BoundingBox<T>(x, y, inc.NextLower(m.Add(x, width)), inc.NextLower(m.Add(y, height)));
		}
		public BoundingBox<T> NewRange(T x1, T y1, T x2, T y2)
		{
			return new BoundingBox<T>(x1, y1, x2, y2);
		}
		IRectangle<T> INewRectangle<IRectangle<T>, T>.NewRect(T x, T y, T width, T height)
		{
			return new BoundingBox<T>(x, y, inc.NextLower(m.Add(x, width)), inc.NextLower(m.Add(y, height)));
		}
		IRectangle<T> INewRectangle<IRectangle<T>, T>.NewRange(T x1, T y1, T x2, T y2)
		{
			return new BoundingBox<T>(x1, y1, x2, y2);
		}

		public void SetXAndWidth(T x, T width)
		{
			SetXRange(x, inc.NextLower(m.Add(x, width)));
		}
		public void SetYAndHeight(T y, T height)
		{
			SetYRange(y, inc.NextLower(m.Add(y, height)));
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
		public void Normalize()
		{
			RectangleExt.Normalize<BoundingBox<T>, T>(this);
		}
		public BoundingBox<T> Union(BoundingBox<T> b)
		{
			return RectangleExt.Union<BoundingBox<T>, T>(this, b);
		}
		public BoundingBox<T> Intersect(BoundingBox<T> b)
		{
			return RectangleExt.Intersect<BoundingBox<T>, T>(this, b);
		}
		public bool ExpandToInclude(Point<T> point)
		{
			return RectangleExt.ExpandToInclude<BoundingBox<T>, Point<T>, T>(this, point);
		}
		public bool ExpandToInclude(IEnumerable<Point<T>> points)
		{
			return RectangleExt.ExpandToInclude<BoundingBox<T>, Point<T>, T>(this, points);
		}
		public bool Contains(Point<T> point)
		{
			return RectangleExt.Contains<BoundingBox<T>, Point<T>, T>(this, point);
		}
		public bool Contains(BoundingBox<T> other)
		{
			return RectangleExt.Contains<BoundingBox<T>, T>(this, other);
		}
		public bool Overlaps(BoundingBox<T> other)
		{
			return RectangleExt.Overlaps<BoundingBox<T>, T>(this, other);
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
	}
}
