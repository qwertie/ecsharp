using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Math;
using System.Diagnostics;

namespace Loyc.Geometry
{
	public struct Vector3<T> : IPoint3<T>, INewPoint3<Vector3<T>, T> where T:IConvertible, IEquatable<T>
	{
		static ISignedMath<T> m = Maths<T>.SignedMath;

		public static readonly Vector3<T> Zero = new Vector3<T>();
		public static readonly Vector3<T> Inf = new Vector3<T>(m.PositiveInfinity, m.PositiveInfinity, m.PositiveInfinity);

		public Vector3(T x, T y) { _x = x; _y = y; _z = default(T); }
		public Vector3(T x, T y, T z) { _x = x; _y = y; _z = z; }

		internal T _x, _y, _z;
		public T X { get { return _x; } set { _x = value; } }
		public T Y { get { return _y; } set { _y = value; } }
		public T Z { get { return _z; } set { _z = value; } }
	
		public override bool Equals(object other) { return other is Vector3<T> && ((Vector3<T>)other) == this; }
		public override int GetHashCode() { return (_x.GetHashCode() ^ _z.GetHashCode()) ^ (_y.GetHashCode() << 1); }
		public override string ToString() { return string.Format("({0},{1},{2})", _x, _y, _z); }

		public Vector3<T> New(T x, T y, T z) { return new Vector3<T>(x, y, z); }
		IPoint3<T> INewPoint3<IPoint3<T>,T>.New(T x, T y, T z) { return new Vector3<T>(x, y, z); }

		public static explicit operator Point3<T>(Vector3<T> p) { return new Point3<T>(p.X, p.Y, p.Z); }

		public static explicit operator Vector3<int>(Vector3<T> p) { return new Vector3<int>(p._x.ToInt32(null), p._y.ToInt32(null), p._z.ToInt32(null)); }
		public static explicit operator Vector3<long>(Vector3<T> p) { return new Vector3<long>(p._x.ToInt64(null), p._y.ToInt64(null), p._z.ToInt64(null)); }
		public static explicit operator Vector3<float>(Vector3<T> p) { return new Vector3<float>(p._x.ToSingle(null), p._y.ToSingle(null), p._z.ToSingle(null)); }
		public static explicit operator Vector3<double>(Vector3<T> p) { return new Vector3<double>(p._x.ToDouble(null), p._y.ToDouble(null), p._z.ToDouble(null)); }

		public static Vector3<T> operator+(Vector3<T> a, Vector3<T> b) { return new Vector3<T>(m.Add(a.X,b.X), m.Add(a.Y,b.Y), m.Add(a.Z,b.Z)); }
		public static Vector3<T> operator-(Vector3<T> a, Vector3<T> b) { return new Vector3<T>(m.Sub(a.X,b.X), m.Sub(a.Y,b.Y), m.Sub(a.Z,b.Z)); }
		public static Vector3<T> operator*(Vector3<T> p, T factor)     { return new Vector3<T>(m.Mul(p.X,factor), m.Mul(p.Y,factor), m.Mul(p.Z,factor)); }
		public static Vector3<T> operator/(Vector3<T> p, T factor)     { return new Vector3<T>(m.Div(p.X,factor), m.Div(p.Y,factor), m.Div(p.Z,factor)); }
		public static Vector3<T> operator<<(Vector3<T> p, int amt)     { return new Vector3<T>(m.Shl(p.X,amt), m.Shl(p.Y,amt), m.Shl(p.Z,amt)); }
		public static Vector3<T> operator>>(Vector3<T> p, int amt)     { return new Vector3<T>(m.Shr(p.X,amt), m.Shr(p.Y,amt), m.Shr(p.Z,amt)); }

		/// <summary>Dot product. a*b equals lhs.Length*rhs.Length*Cos(theta) if theta 
		/// is the angle between two vectors.</summary>
		public static T operator*(Vector3<T> a, Vector3<T> b) { return m.Add(m.Add(m.Mul(a.X,b.X), m.Mul(a.Y,b.Y)), m.Mul(a.Z,b.Z)); }
		/// <summary>Reverses a vector.</summary>
		public static Vector3<T> operator-(Vector3<T> a) { return new Vector3<T>(m.Negate(a.X), m.Negate(a.Y), m.Negate(a.Z)); }

		public static bool operator== (Vector3<T> a, Vector3<T> b) { return a.X.Equals(b.X) && a.Y.Equals(b.Y) && a.Z.Equals(b.Z); }
		public static bool operator!= (Vector3<T> a, Vector3<T> b) { return !a.X.Equals(b.X) || !a.Y.Equals(b.Y) || !a.Z.Equals(b.Z); }
		public bool Equals(Vector3<T> other) { return this == other; }
	}

	public struct Vector3Math<T> : IAdditionGroup<Vector3<T>> where T:IConvertible, IEquatable<T>
	{
		static IMath<T> m = Maths<T>.Math;

		public Vector3<T> Add(Vector3<T> a, Vector3<T> b) { return a + b; }
		public Vector3<T> Add(Vector3<T> a, Vector3<T> b, Vector3<T> c) { return a + b + c; }
		public Vector3<T> Sub(Vector3<T> a, Vector3<T> b) { return a - b; }
		public Vector3<T> Zero { get { return Vector3<T>.Zero; } }
	}

	public static partial class VectorExt
	{
		/// <summary>Computes the "cross product" of a pair of vectors.</summary>
		/// <remarks>
		/// This is not a general cross product, as cross product is only a 3D concept,
		/// but this operator acts as though the two points were in the Z=0 plane and
		/// returns the Z coordinate of the cross product: b.X * a.Y - b.Y * a.X.
		/// This value is zero if the vectors are parallel; it is a.Length * b.Length 
		/// or -a.Length * b.Length if the vectors are perpendicular. One use of 
		/// cross product is to determine whether the angle between two lines is greater 
		/// or less than 180 degrees, corresponding to return values less or greater than 
		/// zero.
		/// </remarks>
		public static Vector3<T> Cross<T>(this Vector3<T> a, Vector3<T> b) where T:IConvertible, IEquatable<T>
		{
			var m = Maths<T>.SignedMath;
			return new Vector3<T>(
				m.Sub(m.Mul(a.Y, b.Z), m.Mul(a.Z, b.Y)), 
				m.Sub(m.Mul(a.Z, b.X), m.Mul(a.X, b.Z)), 
				m.Sub(m.Mul(a.X, b.Y), m.Mul(a.Y, b.X)));
		}

		/// <summary>Gets the square of the length of the vector.</summary>
		public static T Quadrance<T>(this Vector3<T> self) where T:IConvertible, IEquatable<T> 
		{
			var m = Maths<T>.SignedMath;
			return m.Add(m.Add(m.Square(self.X), m.Square(self.Y)), m.Square(self.Z));
		}
		/// <summary>Gets the length of the vector.</summary>
		public static T Length<T>(this Vector3<T> self) where T:IConvertible, IEquatable<T>
		{
			var m = Maths<T>.SignedMath;
			return m.Sqrt(Quadrance(self));
		}
		
		public static bool Normalize<T>(this Vector3<T> self) where T:IConvertible, IEquatable<T>
		{
			var m = Maths<T>.SignedMath;
			Debug.Assert(!m.IsInteger);
			T q = Quadrance(self);
			if (q.Equals(m.Zero))
				return false;
			if (!q.Equals(m.One)) {
				T len = m.Sqrt(q);
				self._x = m.Div(self._x, len);
				self._y = m.Div(self._y, len);
				self._z = m.Div(self._z, len);
			}
			return true;
		}
		
		public static Vector3<T> Normalized<T>(this Vector3<T> self) where T:IConvertible, IEquatable<T>
		{
			Normalize(self);
			return self;
		}

		public static Vector3<T> MulDiv<T>(this Vector3<T> self, T mul, T div) where T:IConvertible, IEquatable<T>
		{
			var m = Maths<T>.SignedMath;
			return new Vector3<T>(m.MulDiv(self._x, mul, div), m.MulDiv(self._y, mul, div), m.MulDiv(self._z, mul, div));
		}
	}
}
