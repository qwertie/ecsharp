

namespace Loyc.Math
{
}

namespace Loyc.Math
{
	using System;
	using T = System.Int32;
				
	public struct Point2I : IEquatable<Point2I>, IPoint<int>, INewPoint<Point2I,int>
	{
		public static readonly Point2I Origin = new Point2I();
		public static readonly Point2I Infinity = new Point2I(T.MaxValue, T.MaxValue);

		public Point2I(T x, T y) { _x = x; _y = y; }

		private int _x, _y;
		public int X { get { return _x; } set { _x = value; } }
		public int Y { get { return _y; } set { _y = value; } }

		public override bool Equals(object other) { return other is Point2I && ((Point2I)other) == this; }
		public override int GetHashCode() { return _x.GetHashCode() ^ _y.GetHashCode(); }
		public override string ToString() { return string.Format("({0},{1})", _x, _y); }
		
		public Point2I New(T x, T y) { return new Point2I(x, y); }
		IPoint<T> INewPoint<IPoint<T>, T>.New(T x, T y) { return new Point2I(x, y); }

		public static explicit operator Vector2I(Point2I p) { return new Vector2I(p.X, p.Y); }

		public static explicit operator Point2F(Point2I p) { return new Point2F((float)p.X, (float)p.Y); }
		public static explicit operator Point2D(Point2I p) { return new Point2D((double)p.X, (double)p.Y); }
		public static explicit operator Point2F8(Point2I p) { return new Point2F8((FPI8)p.X, (FPI8)p.Y); }

		public static Point2I  operator+(Point2I a, Vector2I b) { return new Point2I(a.X+b.X, a.Y+b.Y); }
		public static Point2I  operator+(Vector2I a, Point2I b) { return new Point2I(a.X+b.X, a.Y+b.Y); }
		public static Point2I  operator-(Point2I a, Vector2I b) { return new Point2I(a.X-b.X, a.Y-b.Y); }
		public static Vector2I operator-(Point2I a, Point2I b)  { return new Vector2I(a.X-b.X, a.Y-b.Y); }
		public static Point2I  operator*(Point2I p, T factor) { return new Point2I(p.X*factor, p.Y*factor); }
		public static Point2I  operator/(Point2I p, T factor) { return new Point2I(p.X/factor, p.Y/factor); }
		public static Point2I  operator<<(Point2I p, int amt) { return new Point2I((T)(p.X << amt), (T)(p.Y << amt)); }
		public static Point2I  operator>>(Point2I p, int amt) { return new Point2I((T)(p.X >> amt), (T)(p.Y >> amt)); }

		public static bool operator== (Point2I a, Point2I b) { return a.X == b.X && a.Y == b.Y; }
		public static bool operator!= (Point2I a, Point2I b) { return a.X != b.X || a.Y != b.Y; }
		public bool Equals(Point2I other) { return this == other; }
	}

	public struct Vector2I : IEquatable<Vector2I>, IPoint<int>, INewPoint<Vector2I,int>
	{
		public static readonly Vector2I Origin = new Vector2I();
		public static readonly Vector2I Infinity = new Vector2I(T.MaxValue, T.MaxValue);

		public Vector2I(T x, T y) { _x = x; _y = y; }

		private int _x, _y;
		public int X { get { return _x; } set { _x = value; } }
		public int Y { get { return _y; } set { _y = value; } }
		
		public override bool Equals(object other) { return other is Vector2I && ((Vector2I)other) == this; }
		public override int GetHashCode() { return _x.GetHashCode() ^ _y.GetHashCode(); }
		public override string ToString() { return string.Format("({0},{1})", _x, _y); }

		public Vector2I New(T x, T y) { return new Vector2I(x, y); }
		IPoint<T> INewPoint<IPoint<T>, T>.New(T x, T y) { return new Vector2I(x, y); }
		
		public static explicit operator Point2I(Vector2I p) { return new Point2I(p.X, p.Y); }

		public static explicit operator Vector2F(Vector2I p) { return new Vector2F((float)p.X, (float)p.Y); }
		public static explicit operator Vector2D(Vector2I p) { return new Vector2D((double)p.X, (double)p.Y); }
		public static explicit operator Vector2F8(Vector2I p) { return new Vector2F8((FPI8)p.X, (FPI8)p.Y); }
		
		public static Vector2I operator+(Vector2I a, Vector2I b) { return new Vector2I(a.X+b.X, a.Y+b.Y); }
		public static Vector2I operator-(Vector2I a, Vector2I b) { return new Vector2I(a.X-b.X, a.Y-b.Y); }
		public static Vector2I operator*(Vector2I p, T factor) { return new Vector2I(p.X*factor, p.Y*factor); }
		public static Vector2I operator/(Vector2I p, T factor) { return new Vector2I(p.X/factor, p.Y/factor); }
		public static Vector2I operator<<(Vector2I p, int amt) { return new Vector2I((T)(p.X << amt), (T)(p.Y << amt)); }
		public static Vector2I operator>>(Vector2I p, int amt) { return new Vector2I((T)(p.X >> amt), (T)(p.Y >> amt)); }

		/// <summary>Dot product. a*b equals lhs.Length*rhs.Length*Cos(theta) if theta 
		/// is the angle between two vectors.</summary>
		public static T operator*(Vector2I a, Vector2I b) { return a.X*b.X + a.Y*b.Y; }
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
		public T Cross(Vector2I b) { return X * b.Y - Y * b.X; }
		/// <summary>Rotates a vector 90 degrees.</summary>
		/// <remarks>
		/// Rotatation is clockwise if increasing Y goes downward, counter-
		/// clockwise if increasing Y goes upward. If the vector represents the 
		/// direction of a line, the result also represents the coefficients 
		/// (a,b) of the implicit line equation aX + bY + c = 0.
		/// </remarks>
		public Vector2I Rot90() { return new Vector2I(-Y, X); }
		/// <summary>Reverses a vector.</summary>
		public static Vector2I operator-(Vector2I a) { return new Vector2I(-a.X, -a.Y); }

		public static bool operator== (Vector2I a, Vector2I b) { return a.X == b.X && a.Y == b.Y; }
		public static bool operator!= (Vector2I a, Vector2I b) { return a.X != b.X || a.Y != b.Y; }
		public bool Equals(Vector2I other) { return this == other; }
		
		/// <summary>Gets the square of the length of the vector.</summary>
		public T Quadrance { get { return X*X + Y*Y; } }
		/// <summary>Gets the length of the vector.</summary>
		public T Length { get { return (T)MathEx.Sqrt(Quadrance); } }
		
		/// <summary>Gets the angle from 0 to 2*PI of the vector, where (1,0) has 
		/// angle 0 and (0,1) has angle PI/2.</summary>
		public double Angle
		{ 
			get {
				double angle = Math.Atan2((double)Y, (double)X);
				if (angle < 0)
					return angle + 2*Math.PI; 
				return angle;
			}
		}

		Vector2I MulDiv(T mul, T div)
		{
			return new Vector2I(MathEx.MulDiv(X, mul, div), MathEx.MulDiv(Y, mul, div));
		}
	}
}
namespace Loyc.Math
{
	using System;
	using T = System.Single;
				
	public struct Point2F : IEquatable<Point2F>, IPoint<float>, INewPoint<Point2F,float>
	{
		public static readonly Point2F Origin = new Point2F();
		public static readonly Point2F Infinity = new Point2F(T.PositiveInfinity, T.PositiveInfinity);

		public Point2F(T x, T y) { _x = x; _y = y; }

		private float _x, _y;
		public float X { get { return _x; } set { _x = value; } }
		public float Y { get { return _y; } set { _y = value; } }

		public override bool Equals(object other) { return other is Point2F && ((Point2F)other) == this; }
		public override int GetHashCode() { return _x.GetHashCode() ^ _y.GetHashCode(); }
		public override string ToString() { return string.Format("({0},{1})", _x, _y); }
		
		public Point2F New(T x, T y) { return new Point2F(x, y); }
		IPoint<T> INewPoint<IPoint<T>, T>.New(T x, T y) { return new Point2F(x, y); }

		public static explicit operator Vector2F(Point2F p) { return new Vector2F(p.X, p.Y); }

		public static explicit operator Point2I(Point2F p) { return new Point2I((int)p.X, (int)p.Y); }
		public static explicit operator Point2D(Point2F p) { return new Point2D((double)p.X, (double)p.Y); }
		public static explicit operator Point2F8(Point2F p) { return new Point2F8((FPI8)p.X, (FPI8)p.Y); }

		public static Point2F  operator+(Point2F a, Vector2F b) { return new Point2F(a.X+b.X, a.Y+b.Y); }
		public static Point2F  operator+(Vector2F a, Point2F b) { return new Point2F(a.X+b.X, a.Y+b.Y); }
		public static Point2F  operator-(Point2F a, Vector2F b) { return new Point2F(a.X-b.X, a.Y-b.Y); }
		public static Vector2F operator-(Point2F a, Point2F b)  { return new Vector2F(a.X-b.X, a.Y-b.Y); }
		public static Point2F  operator*(Point2F p, T factor) { return new Point2F(p.X*factor, p.Y*factor); }
		public static Point2F  operator/(Point2F p, T factor) { return new Point2F(p.X/factor, p.Y/factor); }
		public static Point2F  operator<<(Point2F p, int amt) { return new Point2F(MathEx.ShiftLeft(p.X, amt), MathEx.ShiftLeft(p.Y, amt)); }
		public static Point2F  operator>>(Point2F p, int amt) { return new Point2F(MathEx.ShiftRight(p.X, amt), MathEx.ShiftRight(p.Y, amt)); }

		public static bool operator== (Point2F a, Point2F b) { return a.X == b.X && a.Y == b.Y; }
		public static bool operator!= (Point2F a, Point2F b) { return a.X != b.X || a.Y != b.Y; }
		public bool Equals(Point2F other) { return this == other; }
	}

	public struct Vector2F : IEquatable<Vector2F>, IPoint<float>, INewPoint<Vector2F,float>
	{
		public static readonly Vector2F Origin = new Vector2F();
		public static readonly Vector2F Infinity = new Vector2F(T.PositiveInfinity, T.PositiveInfinity);

		public Vector2F(T x, T y) { _x = x; _y = y; }

		private float _x, _y;
		public float X { get { return _x; } set { _x = value; } }
		public float Y { get { return _y; } set { _y = value; } }
		
		public override bool Equals(object other) { return other is Vector2F && ((Vector2F)other) == this; }
		public override int GetHashCode() { return _x.GetHashCode() ^ _y.GetHashCode(); }
		public override string ToString() { return string.Format("({0},{1})", _x, _y); }

		public Vector2F New(T x, T y) { return new Vector2F(x, y); }
		IPoint<T> INewPoint<IPoint<T>, T>.New(T x, T y) { return new Vector2F(x, y); }
		
		public static explicit operator Point2F(Vector2F p) { return new Point2F(p.X, p.Y); }

		public static explicit operator Vector2I(Vector2F p) { return new Vector2I((int)p.X, (int)p.Y); }
		public static explicit operator Vector2D(Vector2F p) { return new Vector2D((double)p.X, (double)p.Y); }
		public static explicit operator Vector2F8(Vector2F p) { return new Vector2F8((FPI8)p.X, (FPI8)p.Y); }
		
		public static Vector2F operator+(Vector2F a, Vector2F b) { return new Vector2F(a.X+b.X, a.Y+b.Y); }
		public static Vector2F operator-(Vector2F a, Vector2F b) { return new Vector2F(a.X-b.X, a.Y-b.Y); }
		public static Vector2F operator*(Vector2F p, T factor) { return new Vector2F(p.X*factor, p.Y*factor); }
		public static Vector2F operator/(Vector2F p, T factor) { return new Vector2F(p.X/factor, p.Y/factor); }
		public static Vector2F operator<<(Vector2F p, int amt) { return new Vector2F(MathEx.ShiftLeft(p.X, amt), MathEx.ShiftLeft(p.Y, amt)); }
		public static Vector2F operator>>(Vector2F p, int amt) { return new Vector2F(MathEx.ShiftRight(p.X, amt), MathEx.ShiftRight(p.Y, amt)); }

		/// <summary>Dot product. a*b equals lhs.Length*rhs.Length*Cos(theta) if theta 
		/// is the angle between two vectors.</summary>
		public static T operator*(Vector2F a, Vector2F b) { return a.X*b.X + a.Y*b.Y; }
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
		public T Cross(Vector2F b) { return X * b.Y - Y * b.X; }
		/// <summary>Rotates a vector 90 degrees.</summary>
		/// <remarks>
		/// Rotatation is clockwise if increasing Y goes downward, counter-
		/// clockwise if increasing Y goes upward. If the vector represents the 
		/// direction of a line, the result also represents the coefficients 
		/// (a,b) of the implicit line equation aX + bY + c = 0.
		/// </remarks>
		public Vector2F Rot90() { return new Vector2F(-Y, X); }
		/// <summary>Reverses a vector.</summary>
		public static Vector2F operator-(Vector2F a) { return new Vector2F(-a.X, -a.Y); }

		public static bool operator== (Vector2F a, Vector2F b) { return a.X == b.X && a.Y == b.Y; }
		public static bool operator!= (Vector2F a, Vector2F b) { return a.X != b.X || a.Y != b.Y; }
		public bool Equals(Vector2F other) { return this == other; }
		
		/// <summary>Gets the square of the length of the vector.</summary>
		public T Quadrance { get { return X*X + Y*Y; } }
		/// <summary>Gets the length of the vector.</summary>
		public T Length { get { return (T)Math.Sqrt(Quadrance); } }
		
		/// <summary>Gets the angle from 0 to 2*PI of the vector, where (1,0) has 
		/// angle 0 and (0,1) has angle PI/2.</summary>
		public double Angle
		{ 
			get {
				double angle = Math.Atan2((double)Y, (double)X);
				if (angle < 0)
					return angle + 2*Math.PI; 
				return angle;
			}
		}

		public bool Normalize()
		{
			T len = Length;
			if (len == (float)0)
				return false;
			if (len != (float)1) {
				X /= len;
				Y /= len;
			}
			return true;
		}
		
		public Vector2F Normalized()
		{
			Vector2F v = this;
			v.Normalize();
			return v;
		}

		Vector2F MulDiv(T mul, T div)
		{
			return new Vector2F((X * mul / div), (Y * mul / div));
		}
	}
}
namespace Loyc.Math
{
	using System;
	using T = System.Double;
				
	public struct Point2D : IEquatable<Point2D>, IPoint<double>, INewPoint<Point2D,double>
	{
		public static readonly Point2D Origin = new Point2D();
		public static readonly Point2D Infinity = new Point2D(T.PositiveInfinity, T.PositiveInfinity);

		public Point2D(T x, T y) { _x = x; _y = y; }

		private double _x, _y;
		public double X { get { return _x; } set { _x = value; } }
		public double Y { get { return _y; } set { _y = value; } }

		public override bool Equals(object other) { return other is Point2D && ((Point2D)other) == this; }
		public override int GetHashCode() { return _x.GetHashCode() ^ _y.GetHashCode(); }
		public override string ToString() { return string.Format("({0},{1})", _x, _y); }
		
		public Point2D New(T x, T y) { return new Point2D(x, y); }
		IPoint<T> INewPoint<IPoint<T>, T>.New(T x, T y) { return new Point2D(x, y); }

		public static explicit operator Vector2D(Point2D p) { return new Vector2D(p.X, p.Y); }

		public static explicit operator Point2I(Point2D p) { return new Point2I((int)p.X, (int)p.Y); }
		public static explicit operator Point2F(Point2D p) { return new Point2F((float)p.X, (float)p.Y); }
		public static explicit operator Point2F8(Point2D p) { return new Point2F8((FPI8)p.X, (FPI8)p.Y); }

		public static Point2D  operator+(Point2D a, Vector2D b) { return new Point2D(a.X+b.X, a.Y+b.Y); }
		public static Point2D  operator+(Vector2D a, Point2D b) { return new Point2D(a.X+b.X, a.Y+b.Y); }
		public static Point2D  operator-(Point2D a, Vector2D b) { return new Point2D(a.X-b.X, a.Y-b.Y); }
		public static Vector2D operator-(Point2D a, Point2D b)  { return new Vector2D(a.X-b.X, a.Y-b.Y); }
		public static Point2D  operator*(Point2D p, T factor) { return new Point2D(p.X*factor, p.Y*factor); }
		public static Point2D  operator/(Point2D p, T factor) { return new Point2D(p.X/factor, p.Y/factor); }
		public static Point2D  operator<<(Point2D p, int amt) { return new Point2D(MathEx.ShiftLeft(p.X, amt), MathEx.ShiftLeft(p.Y, amt)); }
		public static Point2D  operator>>(Point2D p, int amt) { return new Point2D(MathEx.ShiftRight(p.X, amt), MathEx.ShiftRight(p.Y, amt)); }

		public static bool operator== (Point2D a, Point2D b) { return a.X == b.X && a.Y == b.Y; }
		public static bool operator!= (Point2D a, Point2D b) { return a.X != b.X || a.Y != b.Y; }
		public bool Equals(Point2D other) { return this == other; }
	}

	public struct Vector2D : IEquatable<Vector2D>, IPoint<double>, INewPoint<Vector2D,double>
	{
		public static readonly Vector2D Origin = new Vector2D();
		public static readonly Vector2D Infinity = new Vector2D(T.PositiveInfinity, T.PositiveInfinity);

		public Vector2D(T x, T y) { _x = x; _y = y; }

		private double _x, _y;
		public double X { get { return _x; } set { _x = value; } }
		public double Y { get { return _y; } set { _y = value; } }
		
		public override bool Equals(object other) { return other is Vector2D && ((Vector2D)other) == this; }
		public override int GetHashCode() { return _x.GetHashCode() ^ _y.GetHashCode(); }
		public override string ToString() { return string.Format("({0},{1})", _x, _y); }

		public Vector2D New(T x, T y) { return new Vector2D(x, y); }
		IPoint<T> INewPoint<IPoint<T>, T>.New(T x, T y) { return new Vector2D(x, y); }
		
		public static explicit operator Point2D(Vector2D p) { return new Point2D(p.X, p.Y); }

		public static explicit operator Vector2I(Vector2D p) { return new Vector2I((int)p.X, (int)p.Y); }
		public static explicit operator Vector2F(Vector2D p) { return new Vector2F((float)p.X, (float)p.Y); }
		public static explicit operator Vector2F8(Vector2D p) { return new Vector2F8((FPI8)p.X, (FPI8)p.Y); }
		
		public static Vector2D operator+(Vector2D a, Vector2D b) { return new Vector2D(a.X+b.X, a.Y+b.Y); }
		public static Vector2D operator-(Vector2D a, Vector2D b) { return new Vector2D(a.X-b.X, a.Y-b.Y); }
		public static Vector2D operator*(Vector2D p, T factor) { return new Vector2D(p.X*factor, p.Y*factor); }
		public static Vector2D operator/(Vector2D p, T factor) { return new Vector2D(p.X/factor, p.Y/factor); }
		public static Vector2D operator<<(Vector2D p, int amt) { return new Vector2D(MathEx.ShiftLeft(p.X, amt), MathEx.ShiftLeft(p.Y, amt)); }
		public static Vector2D operator>>(Vector2D p, int amt) { return new Vector2D(MathEx.ShiftRight(p.X, amt), MathEx.ShiftRight(p.Y, amt)); }

		/// <summary>Dot product. a*b equals lhs.Length*rhs.Length*Cos(theta) if theta 
		/// is the angle between two vectors.</summary>
		public static T operator*(Vector2D a, Vector2D b) { return a.X*b.X + a.Y*b.Y; }
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
		public T Cross(Vector2D b) { return X * b.Y - Y * b.X; }
		/// <summary>Rotates a vector 90 degrees.</summary>
		/// <remarks>
		/// Rotatation is clockwise if increasing Y goes downward, counter-
		/// clockwise if increasing Y goes upward. If the vector represents the 
		/// direction of a line, the result also represents the coefficients 
		/// (a,b) of the implicit line equation aX + bY + c = 0.
		/// </remarks>
		public Vector2D Rot90() { return new Vector2D(-Y, X); }
		/// <summary>Reverses a vector.</summary>
		public static Vector2D operator-(Vector2D a) { return new Vector2D(-a.X, -a.Y); }

		public static bool operator== (Vector2D a, Vector2D b) { return a.X == b.X && a.Y == b.Y; }
		public static bool operator!= (Vector2D a, Vector2D b) { return a.X != b.X || a.Y != b.Y; }
		public bool Equals(Vector2D other) { return this == other; }
		
		/// <summary>Gets the square of the length of the vector.</summary>
		public T Quadrance { get { return X*X + Y*Y; } }
		/// <summary>Gets the length of the vector.</summary>
		public T Length { get { return (T)Math.Sqrt(Quadrance); } }
		
		/// <summary>Gets the angle from 0 to 2*PI of the vector, where (1,0) has 
		/// angle 0 and (0,1) has angle PI/2.</summary>
		public double Angle
		{ 
			get {
				double angle = Math.Atan2((double)Y, (double)X);
				if (angle < 0)
					return angle + 2*Math.PI; 
				return angle;
			}
			set {
				this = FromPolar(Length, value);
			}
		}

		public static Vector2D FromPolar(T magnitude, T angle)
		{
			return new Vector2D((T)Math.Cos((double)angle)*(double)magnitude,
			                        (T)Math.Sin((double)angle)*(double)magnitude);
		}

		public bool Normalize()
		{
			T len = Length;
			if (len == (double)0)
				return false;
			if (len != (double)1) {
				X /= len;
				Y /= len;
			}
			return true;
		}
		
		public Vector2D Normalized()
		{
			Vector2D v = this;
			v.Normalize();
			return v;
		}

		Vector2D MulDiv(T mul, T div)
		{
			return new Vector2D((X * mul / div), (Y * mul / div));
		}
	}
}
namespace Loyc.Math
{
	using System;
	using T = FPI8;
				
	public struct Point2F8 : IEquatable<Point2F8>, IPoint<FPI8>, INewPoint<Point2F8,FPI8>
	{
		public static readonly Point2F8 Origin = new Point2F8();
		public static readonly Point2F8 Infinity = new Point2F8(T.MaxValue, T.MaxValue);

		public Point2F8(T x, T y) { _x = x; _y = y; }

		private FPI8 _x, _y;
		public FPI8 X { get { return _x; } set { _x = value; } }
		public FPI8 Y { get { return _y; } set { _y = value; } }

		public override bool Equals(object other) { return other is Point2F8 && ((Point2F8)other) == this; }
		public override int GetHashCode() { return _x.GetHashCode() ^ _y.GetHashCode(); }
		public override string ToString() { return string.Format("({0},{1})", _x, _y); }
		
		public Point2F8 New(T x, T y) { return new Point2F8(x, y); }
		IPoint<T> INewPoint<IPoint<T>, T>.New(T x, T y) { return new Point2F8(x, y); }

		public static explicit operator Vector2F8(Point2F8 p) { return new Vector2F8(p.X, p.Y); }

		public static explicit operator Point2I(Point2F8 p) { return new Point2I((int)p.X, (int)p.Y); }
		public static explicit operator Point2F(Point2F8 p) { return new Point2F((float)p.X, (float)p.Y); }
		public static explicit operator Point2D(Point2F8 p) { return new Point2D((double)p.X, (double)p.Y); }

		public static Point2F8  operator+(Point2F8 a, Vector2F8 b) { return new Point2F8(a.X+b.X, a.Y+b.Y); }
		public static Point2F8  operator+(Vector2F8 a, Point2F8 b) { return new Point2F8(a.X+b.X, a.Y+b.Y); }
		public static Point2F8  operator-(Point2F8 a, Vector2F8 b) { return new Point2F8(a.X-b.X, a.Y-b.Y); }
		public static Vector2F8 operator-(Point2F8 a, Point2F8 b)  { return new Vector2F8(a.X-b.X, a.Y-b.Y); }
		public static Point2F8  operator*(Point2F8 p, T factor) { return new Point2F8(p.X*factor, p.Y*factor); }
		public static Point2F8  operator/(Point2F8 p, T factor) { return new Point2F8(p.X/factor, p.Y/factor); }
		public static Point2F8  operator<<(Point2F8 p, int amt) { return new Point2F8((T)(p.X << amt), (T)(p.Y << amt)); }
		public static Point2F8  operator>>(Point2F8 p, int amt) { return new Point2F8((T)(p.X >> amt), (T)(p.Y >> amt)); }

		public static bool operator== (Point2F8 a, Point2F8 b) { return a.X == b.X && a.Y == b.Y; }
		public static bool operator!= (Point2F8 a, Point2F8 b) { return a.X != b.X || a.Y != b.Y; }
		public bool Equals(Point2F8 other) { return this == other; }
	}

	public struct Vector2F8 : IEquatable<Vector2F8>, IPoint<FPI8>, INewPoint<Vector2F8,FPI8>
	{
		public static readonly Vector2F8 Origin = new Vector2F8();
		public static readonly Vector2F8 Infinity = new Vector2F8(T.MaxValue, T.MaxValue);

		public Vector2F8(T x, T y) { _x = x; _y = y; }

		private FPI8 _x, _y;
		public FPI8 X { get { return _x; } set { _x = value; } }
		public FPI8 Y { get { return _y; } set { _y = value; } }
		
		public override bool Equals(object other) { return other is Vector2F8 && ((Vector2F8)other) == this; }
		public override int GetHashCode() { return _x.GetHashCode() ^ _y.GetHashCode(); }
		public override string ToString() { return string.Format("({0},{1})", _x, _y); }

		public Vector2F8 New(T x, T y) { return new Vector2F8(x, y); }
		IPoint<T> INewPoint<IPoint<T>, T>.New(T x, T y) { return new Vector2F8(x, y); }
		
		public static explicit operator Point2F8(Vector2F8 p) { return new Point2F8(p.X, p.Y); }

		public static explicit operator Vector2I(Vector2F8 p) { return new Vector2I((int)p.X, (int)p.Y); }
		public static explicit operator Vector2F(Vector2F8 p) { return new Vector2F((float)p.X, (float)p.Y); }
		public static explicit operator Vector2D(Vector2F8 p) { return new Vector2D((double)p.X, (double)p.Y); }
		
		public static Vector2F8 operator+(Vector2F8 a, Vector2F8 b) { return new Vector2F8(a.X+b.X, a.Y+b.Y); }
		public static Vector2F8 operator-(Vector2F8 a, Vector2F8 b) { return new Vector2F8(a.X-b.X, a.Y-b.Y); }
		public static Vector2F8 operator*(Vector2F8 p, T factor) { return new Vector2F8(p.X*factor, p.Y*factor); }
		public static Vector2F8 operator/(Vector2F8 p, T factor) { return new Vector2F8(p.X/factor, p.Y/factor); }
		public static Vector2F8 operator<<(Vector2F8 p, int amt) { return new Vector2F8((T)(p.X << amt), (T)(p.Y << amt)); }
		public static Vector2F8 operator>>(Vector2F8 p, int amt) { return new Vector2F8((T)(p.X >> amt), (T)(p.Y >> amt)); }

		/// <summary>Dot product. a*b equals lhs.Length*rhs.Length*Cos(theta) if theta 
		/// is the angle between two vectors.</summary>
		public static T operator*(Vector2F8 a, Vector2F8 b) { return a.X*b.X + a.Y*b.Y; }
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
		public T Cross(Vector2F8 b) { return X * b.Y - Y * b.X; }
		/// <summary>Rotates a vector 90 degrees.</summary>
		/// <remarks>
		/// Rotatation is clockwise if increasing Y goes downward, counter-
		/// clockwise if increasing Y goes upward. If the vector represents the 
		/// direction of a line, the result also represents the coefficients 
		/// (a,b) of the implicit line equation aX + bY + c = 0.
		/// </remarks>
		public Vector2F8 Rot90() { return new Vector2F8(-Y, X); }
		/// <summary>Reverses a vector.</summary>
		public static Vector2F8 operator-(Vector2F8 a) { return new Vector2F8(-a.X, -a.Y); }

		public static bool operator== (Vector2F8 a, Vector2F8 b) { return a.X == b.X && a.Y == b.Y; }
		public static bool operator!= (Vector2F8 a, Vector2F8 b) { return a.X != b.X || a.Y != b.Y; }
		public bool Equals(Vector2F8 other) { return this == other; }
		
		/// <summary>Gets the square of the length of the vector.</summary>
		public T Quadrance { get { return X*X + Y*Y; } }
		/// <summary>Gets the length of the vector.</summary>
		public T Length { get { return Quadrance.Sqrt(); } }
		
		/// <summary>Gets the angle from 0 to 2*PI of the vector, where (1,0) has 
		/// angle 0 and (0,1) has angle PI/2.</summary>
		public double Angle
		{ 
			get {
				double angle = Math.Atan2((double)Y, (double)X);
				if (angle < 0)
					return angle + 2*Math.PI; 
				return angle;
			}
		}

		public bool Normalize()
		{
			T len = Length;
			if (len == FPI8.Zero)
				return false;
			if (len != FPI8.One) {
				X /= len;
				Y /= len;
			}
			return true;
		}
		
		public Vector2F8 Normalized()
		{
			Vector2F8 v = this;
			v.Normalize();
			return v;
		}

		Vector2F8 MulDiv(T mul, T div)
		{
			return new Vector2F8(X.MulDiv(mul, div), Y.MulDiv(mul, div));
		}
	}
}
