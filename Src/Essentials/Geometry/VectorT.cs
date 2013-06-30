using System.Diagnostics;
using Loyc.Math;

namespace Loyc.Geometry
{
	using System;

	/// <summary>Represents a two-dimensional vector, i.e. a magnitude and 
	/// direction or the difference between two points, stored as X and Y
	/// components.</summary>
	/// <remarks>A vector is the same as a point except for the operations it
	/// supports. For example, it usually does not make sense to add two points,
	/// but you can add two vectors (to get another vector) or you can add a 
	/// vector to a point (to get a point).
	/// <para/>
	/// If you really do need to add two points together or something like 
	/// that, you can typecast from <see cref="Point{T}"/> to 
	/// <see cref="Vector{T}"/>.
	/// </remarks>
	/// <seealso cref="PointMath"/>
	public struct Vector<T> : IPoint<T>, INewPoint<Vector<T>, T> where T:IConvertible, IEquatable<T>
	{
		static ISignedMath<T> m = Maths<T>.SignedMath;

		public static readonly Vector<T> Zero = new Vector<T>();
		public static readonly Vector<T> Inf = new Vector<T>(m.PositiveInfinity, m.PositiveInfinity);

		public Vector(T x, T y) { _x = x; _y = y; }

		internal T _x, _y;
		public T X { get { return _x; } set { _x = value; } }
		public T Y { get { return _y; } set { _y = value; } }
	
		public override bool Equals(object other) { return other is Vector<T> && ((Vector<T>)other) == this; }
		public override int GetHashCode() { return _x.GetHashCode() ^ (_y.GetHashCode() << 1); }
		public override string ToString() { return string.Format("({0},{1})", _x, _y); }

		public Vector<T> New(T x, T y) { return new Vector<T>(x, y); }
		IPoint<T> INewPoint<IPoint<T>,T>.New(T x, T y) { return new Vector<T>(x, y); }

		public static explicit operator Point<T>(Vector<T> p) { return new Point<T>(p.X, p.Y); }

		public static explicit operator Vector<int>(Vector<T> p) { return new Vector<int>(p._x.ToInt32(null), p._y.ToInt32(null)); }
		public static explicit operator Vector<long>(Vector<T> p) { return new Vector<long>(p._x.ToInt64(null), p._y.ToInt64(null)); }
		public static explicit operator Vector<float>(Vector<T> p) { return new Vector<float>(p._x.ToSingle(null), p._y.ToSingle(null)); }
		public static explicit operator Vector<double>(Vector<T> p) { return new Vector<double>(p._x.ToDouble(null), p._y.ToDouble(null)); }

		public static Vector<T> operator+(Vector<T> a, Vector<T> b) { return new Vector<T>(m.Add(a.X,b.X), m.Add(a.Y,b.Y)); }
		public static Vector<T> operator-(Vector<T> a, Vector<T> b) { return new Vector<T>(m.Sub(a.X,b.X), m.Sub(a.Y,b.Y)); }
		public static Vector<T> operator*(Vector<T> p, T factor) { return new Vector<T>(m.Mul(p.X,factor), m.Mul(p.Y,factor)); }
		public static Vector<T> operator/(Vector<T> p, T factor) { return new Vector<T>(m.Div(p.X,factor), m.Div(p.Y,factor)); }
		public static Vector<T> operator<<(Vector<T> p, int amt) { return new Vector<T>(m.Shl(p.X, amt), m.Shl(p.Y, amt)); }
		public static Vector<T> operator>>(Vector<T> p, int amt) { return new Vector<T>(m.Shr(p.X, amt), m.Shr(p.Y, amt)); }

		/// <summary>Dot product. a*b equals lhs.Length*rhs.Length*Cos(theta) if theta 
		/// is the angle between two vectors.</summary>
		public static T operator*(Vector<T> a, Vector<T> b) { return m.Add(m.Mul(a.X,b.X), m.Mul(a.Y,b.Y)); }
		/// <summary>Reverses a vector.</summary>
		public static Vector<T> operator-(Vector<T> a) { return new Vector<T>(m.Negate(a.X), m.Negate(a.Y)); }

		public static bool operator== (Vector<T> a, Vector<T> b) { return a.X.Equals(b.X) && a.Y.Equals(b.Y); }
		public static bool operator!= (Vector<T> a, Vector<T> b) { return !a.X.Equals(b.X) || !a.Y.Equals(b.Y); }
		public bool Equals(Vector<T> other) { return this == other; }
		
	}

	public struct VectorMath<T> : IAdditionGroup<Vector<T>> where T:IConvertible, IEquatable<T>
	{
		static IMath<T> m = Maths<T>.Math;

		public Vector<T> Add(Vector<T> a, Vector<T> b) { return new Vector<T>(m.Add(a.X, b.X), m.Add(a.Y, b.Y)); }
		public Vector<T> Add(Vector<T> a, Vector<T> b, Vector<T> c) { return new Vector<T>(m.Add(a.X, b.X, c.X), m.Add(a.Y, b.Y, c.Y)); }
		public Vector<T> Sub(Vector<T> a, Vector<T> b) { return new Vector<T>(m.Sub(a.X, b.X), m.Sub(a.Y, b.Y)); }
		public Vector<T> Zero { get { return new Vector<T>(); } }
	}
}
