namespace Loyc.Geometry
{
	using System;
	using Loyc.Math;

	public struct Point<T> : IPoint<T>, INewPoint<Point<T>,T> where T:IConvertible, IEquatable<T>
	{
		static IMath<T> m = Maths<T>.Math;

		public static readonly Point<T> Zero = new Point<T>();
		public static readonly Point<T> Inf = new Point<T>(m.PositiveInfinity, m.PositiveInfinity);

		public Point(T x, T y) { _x = x; _y = y; }

		T _x, _y;
		public T X { get { return _x; } set { _x = value; } }
		public T Y { get { return _y; } set { _y = value; } }
	
		public override bool Equals(object other) { return other is Point<T> && ((Point<T>)other) == this; }
		public override int GetHashCode() { return _x.GetHashCode() ^ (_y.GetHashCode() << 1); }
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
		public static Point<T>  operator-(Point<T> a, Vector<T> b) { return new Point<T>(m.Sub(a.X,b.X), m.Sub(a.Y,b.Y)); }
		public static Vector<T> operator-(Point<T> a, Point<T> b)  { return new Vector<T>(m.Sub(a.X,b.X), m.Sub(a.Y,b.Y)); }
		public static Point<T>  operator*(Point<T> p, T factor) { return new Point<T>(m.Mul(p.X,factor), m.Mul(p.Y,factor)); }
		public static Point<T>  operator/(Point<T> p, T factor) { return new Point<T>(m.Div(p.X,factor), m.Div(p.Y,factor)); }
		public static Point<T>  operator<<(Point<T> p, int amt) { return new Point<T>(m.Shl(p.X,amt), m.Shl(p.Y,amt)); }
		public static Point<T>  operator>>(Point<T> p, int amt) { return new Point<T>(m.Shr(p.X,amt), m.Shr(p.Y,amt)); }

		public static bool operator== (Point<T> a, Point<T> b) { return a.X.Equals(b.X) && a.Y.Equals(b.Y); }
		public static bool operator!= (Point<T> a, Point<T> b) { return !a.X.Equals(b.X) || !a.Y.Equals(b.Y); }
		public bool Equals(Point<T> other) { return this == other; }
	}

	public struct Point3<T> : IPoint3<T>, INewPoint3<Point3<T>,T> where T:IConvertible, IEquatable<T>
	{
		static IMath<T> m = Maths<T>.Math;

		public static readonly Point3<T> Zero = new Point3<T>();
		public static readonly Point3<T> Inf = new Point3<T>(m.PositiveInfinity, m.PositiveInfinity, m.PositiveInfinity);

		public Point3(T x, T y) { _x = x; _y = y; _z = default(T); }
		public Point3(T x, T y, T z) { _x = x; _y = y; _z = z; }

		T _x, _y, _z;
		public T X { get { return _x; } set { _x = value; } }
		public T Y { get { return _y; } set { _y = value; } }
		public T Z { get { return _z; } set { _z = value; } }
	
		public override bool Equals(object other) { return other is Point3<T> && ((Point3<T>)other) == this; }
		public override int GetHashCode() { return (_x.GetHashCode() ^ _z.GetHashCode()) ^ (_y.GetHashCode() << 1); }
		public override string ToString() { return string.Format("({0},{1},{2})", _x, _y, _z); }
		
		public Point3<T> New(T x, T y, T z) { return new Point3<T>(x, y, z); }
		IPoint3<T> INewPoint3<IPoint3<T>, T>.New(T x, T y, T z) { return new Point3<T>(x, y, z); }

		public static explicit operator Vector3<T>(Point3<T> p) { return new Vector3<T>(p.X, p.Y, p.Z); }

		public static explicit operator Point3<int>(Point3<T> p) { return new Point3<int>(p._x.ToInt32(null), p._y.ToInt32(null), p._z.ToInt32(null)); }
		public static explicit operator Point3<long>(Point3<T> p) { return new Point3<long>(p._x.ToInt64(null), p._y.ToInt64(null), p._z.ToInt64(null)); }
		public static explicit operator Point3<float>(Point3<T> p) { return new Point3<float>(p._x.ToSingle(null), p._y.ToSingle(null), p._z.ToSingle(null)); }
		public static explicit operator Point3<double>(Point3<T> p) { return new Point3<double>(p._x.ToDouble(null), p._y.ToDouble(null), p._z.ToDouble(null)); }
		
		public static Point3<T>  operator+(Point3<T> a, Vector3<T> b) { return new Point3<T>(m.Add(a.X,b.X), m.Add(a.Y,b.Y), m.Add(a.Z,b.Z)); }
		public static Point3<T>  operator+(Vector3<T> a, Point3<T> b) { return new Point3<T>(m.Add(a.X,b.X), m.Add(a.Y,b.Y), m.Add(a.Z,b.Z)); }
		public static Point3<T>  operator-(Point3<T> a, Vector3<T> b) { return new Point3<T>(m.Sub(a.X,b.X), m.Sub(a.Y,b.Y), m.Sub(a.Z,b.Z)); }
		public static Vector3<T> operator-(Point3<T> a, Point3<T> b)  { return new Vector3<T>(m.Sub(a.X,b.X), m.Sub(a.Y,b.Y), m.Sub(a.Z,b.Z)); }
		public static Point3<T>  operator*(Point3<T> p, T factor) { return new Point3<T>(m.Mul(p.X,factor), m.Mul(p.Y,factor), m.Mul(p.Z,factor)); }
		public static Point3<T>  operator/(Point3<T> p, T factor) { return new Point3<T>(m.Div(p.X,factor), m.Div(p.Y,factor), m.Div(p.Z,factor)); }
		public static Point3<T>  operator<<(Point3<T> p, int amt) { return new Point3<T>(m.Shl(p.X,amt), m.Shl(p.Y,amt), m.Shl(p.Z,amt)); }
		public static Point3<T>  operator>>(Point3<T> p, int amt) { return new Point3<T>(m.Shr(p.X,amt), m.Shr(p.Y,amt), m.Shr(p.Z,amt)); }

		public static bool operator== (Point3<T> a, Point3<T> b) { return a.X.Equals(b.X) && a.Y.Equals(b.Y) && a.Z.Equals(b.Z); }
		public static bool operator!= (Point3<T> a, Point3<T> b) { return !a.X.Equals(b.X) || !a.Y.Equals(b.Y) || !a.Z.Equals(b.Z); }
		public bool Equals(Point3<T> other) { return this == other; }
	}

}
