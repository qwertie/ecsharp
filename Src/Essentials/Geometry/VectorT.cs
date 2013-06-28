using System.Diagnostics;
using Loyc.Math;

namespace Loyc.Geometry
{
	using System;

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
		public static T Cross<T>(this Vector<T> a, Vector<T> b) where T:IConvertible, IEquatable<T>
		{
			var m = Maths<T>.SignedMath;
			return m.Sub(m.Mul(a.X, b.Y), m.Mul(a.Y, b.X));
		}


		/// <summary>Rotates a vector 90 degrees.</summary>
		/// <remarks>
		/// Rotatation is clockwise if increasing Y goes downward, counter-
		/// clockwise if increasing Y goes upward. If the vector represents the 
		/// direction of a line, the result also represents the coefficients 
		/// (a,b) of the implicit line equation aX + bY + c = 0.
		/// </remarks>
		public static Vector<T> Rot90<T>(this Vector<T> self) where T:IConvertible, IEquatable<T>
		{
			var m = Maths<T>.SignedMath;
			return new Vector<T>(m.Negate(self.Y), self.X);
		}

		/// <summary>Gets the square of the length of the vector.</summary>
		public static T Quadrance<T>(this Vector<T> self) where T:IConvertible, IEquatable<T> 
		{
			var m = Maths<T>.SignedMath;
			return m.Add(m.Square(self.X), m.Square(self.Y));
		}
		/// <summary>Gets the length of the vector.</summary>
		public static T Length<T>(this Vector<T> self) where T:IConvertible, IEquatable<T>
		{
			var m = Maths<T>.SignedMath;
			return m.Sqrt(Quadrance(self));
		}
		
		/// <summary>Gets the angle from 0 to 2*PI of the vector, where (1,0) has 
		/// angle 0 and (0,1) has angle PI/2.</summary>
		public static double Angle<T>(this Vector<T> self) where T:IConvertible, IEquatable<T>
		{ 
			double angle = Math.Atan2(self.Y.ToDouble(null), self.X.ToDouble(null));
			if (angle < 0)
				return angle + 2*Math.PI; 
			return angle;
		}

		public static bool Normalize<T>(this Vector<T> self) where T:IConvertible, IEquatable<T>
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
			}
			return true;
		}
		
		public static Vector<T> Normalized<T>(this Vector<T> self) where T:IConvertible, IEquatable<T>
		{
			Normalize(self);
			return self;
		}

		public static Vector<T> MulDiv<T>(this Vector<T> self, T mul, T div) where T:IConvertible, IEquatable<T>
		{
			var m = Maths<T>.SignedMath;
			return new Vector<T>(m.MulDiv(self._x, mul, div), m.MulDiv(self._y, mul, div));
		}
	}
}
