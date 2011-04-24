namespace Loyc.Math
{
	using System;

	public struct Point<T> : IPoint<T>, INewPoint<Point<T>,T> where T:IConvertible, IEquatable<T>
	{
		static IMath<T> m = Maths<T>.Math;

		public Point(T x, T y) { _x = x; _y = y; }

		T _x, _y;
		public T X { get { return _x; } set { _x = value; } }
		public T Y { get { return _y; } set { _y = value; } }
	
		public override bool Equals(object other) { return other is Point<T> && ((Point<T>)other) == this; }
		public override int GetHashCode() { return _x.GetHashCode() ^ _y.GetHashCode(); }
		public override string ToString() { return string.Format("({0},{1})", _x, _y); }
		
		public Point<T> New(T x, T y) { return new Point<T>(x, y); }
		IPoint<T> INewPoint<IPoint<T>, T>.New(T x, T y) { return new Point<T>(x, y); }

		public static explicit operator Vector<T>(Point<T> p) { return new Vector<T>(p.X, p.Y); }

		public static explicit operator Point<int>(Point<T> p) { return new Point<int>(p._x.ToInt32(null), p._y.ToInt32(null)); }
		public static explicit operator Point<long>(Point<T> p) { return new Point<long>(p._x.ToInt64(null), p._y.ToInt64(null)); }
		public static explicit operator Point<float>(Point<T> p) { return new Point<float>(p._x.ToSingle(null), p._y.ToSingle(null)); }
		public static explicit operator Point<double>(Point<T> p) { return new Point<double>(p._x.ToDouble(null), p._y.ToDouble(null)); }
		
		public static Point<T>  operator+(Point<T> a, Vector<T> b) { return new Point<T>(m.Add(a.X,b.X), m.Add(a.Y,b.Y)); }
		public static Point<T>  operator+(Vector<T> a, Point<T> b) { return new Point<T>(m.Add(a.X,b.X), m.Add(a.Y,b.Y)); }
		public static Point<T>  operator-(Point<T> a, Vector<T> b) { return new Point<T>(m.Subtract(a.X,b.X), m.Subtract(a.Y,b.Y)); }
		public static Vector<T> operator-(Point<T> a, Point<T> b)  { return new Vector<T>(m.Subtract(a.X,b.X), m.Subtract(a.Y,b.Y)); }
		public static Point<T>  operator*(Point<T> p, T factor) { return new Point<T>(m.Multiply(p.X,factor), m.Multiply(p.Y,factor)); }
		public static Point<T>  operator/(Point<T> p, T factor) { return new Point<T>(m.Divide(p.X,factor), m.Divide(p.Y,factor)); }
		public static Point<T>  operator<<(Point<T> p, int amt) { return new Point<T>(m.ShiftLeft(p.X,amt), m.ShiftLeft(p.Y,amt)); }
		public static Point<T>  operator>>(Point<T> p, int amt) { return new Point<T>(m.ShiftRight(p.X,amt), m.ShiftRight(p.Y,amt)); }

		public static bool operator== (Point<T> a, Point<T> b) { return a.X.Equals(b.X) && a.Y.Equals(b.Y); }
		public static bool operator!= (Point<T> a, Point<T> b) { return !a.X.Equals(b.X) || !a.Y.Equals(b.Y); }
		public bool Equals(Point<T> other) { return this == other; }
	}
}
