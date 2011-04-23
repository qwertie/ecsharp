using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Math;
using Loyc.Essentials;

namespace Loyc.Math
{
	/// <summary>This interface exists to work around a limitation of C#; use 
	/// <see cref="IPoint{T}"/> instead.</summary>
	public interface IPointBase<T>
	{
		/// <summary>Horizontal coordinate of a point or vector.</summary>
		/// <remarks>In geographic points, X represents the longitude.</remarks>
		T X { get; set; }
		/// <summary>Vertical coordinate of a point or vector.</summary>
		/// <remarks>
		/// In 3D spaces, Y is sometimes used as a depth coordinate instead.
		/// In geographic points, Y represents the latitude.
		/// </remarks>
		T Y { get; set; }
	}

	/// <summary>A mutable 2D point with X and Y coordinates.</summary>
	/// <remarks>
	/// WARNING: When casting a point (or vector) structure to this interface, 
	/// it is boxed, making a copy. Changes made through a reference to IPoint do 
	/// not affect the original point!
	/// <para/>
	/// It is important to distinguish between generic code that operates on points
	/// (e.g. Foo&lt;Point,T>(Point x) where Point:IPoint&lt;T>) and code that uses 
	/// the IPoint interface directly (e.g. Foo(IPoint&lt;int>) or even 
	/// Foo&lt;T>(IPoint&lt;T> x)). The latter uses slow, late-bound interface 
	/// calls and the boxing-copy issue mentioned before must be kept in mind.
	/// Generic code that uses IPoint as a <i>constraint</i>, not as a <i>parameter 
	/// type,</i> is faster and does not have the same problem because it does not 
	/// actually box the point, nor does it use late-bound invocation.
	/// <para/>
	/// Normally this interface is not used directly, and the only operations 
	/// provided are New() and some extension methods. Instead, this interface is 
	/// intended mainly for use in generic code, where it is used together with a 
	/// calculator struct implementing an interface such as <see cref="IMath{T}"/>.
	/// <para/>
	/// There is no corresponding IVector type for vectors because generic code 
	/// must declare every type it needs as a separate type parameter, which makes 
	/// the code very cumbersome to write already, even without a point/vector 
	/// distinction.
	/// <para/>
	/// The New() method is not normally used in generic code because it returns 
	/// IPoint&lt;T>, not the original point type. It is provided mainly in case 
	/// it is needed in code that relies on the interface for some reason.
	/// <para/>
	/// Due to a limitation of C#, the X and Y coordinates are separated into a 
	/// separate interface (<see cref="IPointBase{T}"/>) from the New() method in
	/// <see cref="INewPoint{Point,T}"/>. The reason for this is very subtle. 
	/// To understand it, consider the following generic method that adds two 
	/// points together:
	/// <para/>
	/// <code>
	/// public static Point Add&lt;Point,T,M>(this M m, Point a, Point b)
	///		where Point : IPoint&lt;T>
	///		where M : IAdditionGroup&lt;T>
	///	{
	///		return a.New(m.Add(a.X, b.X), m.Add(a.Y, b.Y));
	///	}
	///	</code>
	/// As written, this code does not compile. The reason is that a.New() does not 
	/// return a Point; instead, it returns <see cref="IPoint{T}"/>, which is a more 
	/// general (interface) type than Point (which is probably a struct). Therefore, 
	/// in order for the code above to work, a cast from IPoint to Point would be 
	/// necessary. However, the boxing performed by new() and the unboxing performed 
	/// by the cast will slow down the method. My goal, however, is to allow generic
	/// code to run fast; otherwise it's hard to justify the extra effort required 
	/// to make the code generic. After all, the code to add non-generic points is
	/// trivial in comparison:
	/// <code>
	/// public static PointD Add(PointD a, PointD b) { return a + b; }
	/// </code>
	/// You might think to yourself, "okay, why don't we just add a new() 
	/// constraint on Point?" In that case the Add() method can be written as 
	/// follows:
	/// <code>
	/// public static Point Add&lt;Point,T,M>(this M m, Point a, Point b)
	///		where Point : IPoint&lt;T>, new()
	///		where M : IAdditionGroup&lt;T>
	///	{
	///		Point p = new Point();
	///		p.X = m.Add(a.X, b.X);
	///		p.Y = m.Add(a.Y, b.Y);
	///		return p;
	///	}
	///	</code>
	///	This works if the input is a concrete point type, but this version of the 
	///	method cannot be used if Point happens to be <see cref="IPoint{T}"/> itself;
	///	you cannot do "new IPoint" because it is an interface. To solve this I 
	///	considered splitting out New() into a separate interface and using it as a 
	///	constraint of the generic method:
	/// <code>
	/// public static Point Add&lt;Point,T,M>(this M m, Point a, Point b)
	///		where Point : IPoint&lt;T>, INewPoint&lt;Point, T>
	///		where M : IAdditionGroup&lt;T>
	///	{
	///		return a.New(m.Add(a.X, b.X), m.Add(a.Y, b.Y));
	///	}
	///	</code>
	///	This code compiles under one condition: IPoint must <i>not</i> be derived 
	///	from INewPoint&lt;IPoint&lt;T>,T>. Because if it is, then the call to New()
	///	is ambiguous: does a.New() refer to INewPoint&lt;IPoint&lt;T>,T>.New() or 
	///	to INewPoint&lt;Point,T>.New()? Remember, IPoint is not the same as Point--
	///	IPoint is an interface, but Point is usually a struct (it could be the same 
	///	as IPoint, but in general, it is not). The compiler doesn't know which 
	///	version of New() to call, so it refuses to compile the code. It will
	///	compile if we change the method body to 
	/// <code>
	/// 	return ((INewPoint&lt;Point,T>)a).New(m.Add(a.X, b.X), m.Add(a.Y, b.Y));
	///	</code>
	///	The cast resolves the ambiguity, but as a side-effect, 'a' is boxed and 
	///	the call to New() becomes a virtual call that cannot be inlined. So the 
	///	same performance penalty is back!
	///	<para/>
	///	But as I was saying, the original code does compile if IPoint is <i>not</i>
	///	derived from INewPoint. Unfortunately, if IPoint is not derived from 
	///	INewPoint then it is impossible to pass a reference to IPoint to this 
	///	method (because it no longer meets the constraints). Remember, that is 
	///	exactly the problem I was trying to avoid!
	///	<para/>
	///	One more "solution" is not to create any new points:
	/// <code>
	/// public static Point Add&lt;Point,T,M>(this M m, Point a, Point b)
	///		where Point : IPoint&lt;T>
	///		where M : IAdditionGroup&lt;T>
	///	{
	///		a.X = m.Add(a.X, b.X);
	///		a.Y = m.Add(a.Y, b.Y);
	///		return a;
	///	}
	///	</code>
	///	Alas, this version of the code modifies the point 'a' if Point is IPoint, 
	///	but it does not modify 'a' if Point is a struct, because structs are passed 
	///	by value. This inconsistency is not acceptable, and besides, there are
	///	(of course) situations where creating new points is required.
	///	<para/>
	///	The problem is that if there is only one New() method defined (in the 
	///	point structures such as PointI and PointD) then it's impossible to pass
	///	references to IPoint to Add(); however, if there are two New() methods (one 
	///	in the point struct and one in IPoint), it is impossible to tell the C# 
	///	compiler which method we want to call without slowing down the code as a 
	///	side-effect. My solution to this very peculiar problem is to split IPoint
	///	into two independent interfaces, IPointBase and INewPoint. This separation
	///	allows us to tell the C# compiler that Point implements only one of the 
	///	New() methods, not both:
	///	<code>
	/// public static Point Add&lt;Point,T,M>(this M m, Point a, Point b)
	///     where Point : IPointBase&lt;T>, INewPoint&lt;Point, T>
	///     where M : IAdditionGroup&lt;T>
	/// {
	///     return a.New(m.Add(a.X, b.X), m.Add(a.Y, b.Y));
	/// }
	///	</code>
	///	IPointBase, unlike IPoint, does not have a New() method, so only the 
	///	New() method in INewPoint&lt;Point, T> is available to be called, and the 
	///	C# compiler stops complaining. Also, since IPoint&lt;T> implements both 
	///	IPointBase&lt;T> and INewPoint&lt;IPoint&lt;T>, T>, it meets the generic
	///	constraints of this method and can be passed to it.
	///	<para/>
	///	Note that you don't have to write methods like Add() yourself (they are 
	///	provided as extension methods on IPoint.) Still, if you've read this far,
	///	you're probably now afraid of the effort required to write generic code! 
	/// Your fear may be justified. But there is another, easier way that you can 
	/// write generic code, based on <see cref="Point{T}"/> instead of IPoint:
	///	<code>
	/// public static Point&lt;T> Add&lt;T>(Point&lt;T> a, Point&lt;T> b)
	/// {
	///		return a+b;
	/// }
	///	</code>
	///	A lot easier without all those constraints, yes? The main disadvantage of 
	///	this version is that it doesn't have great performance, because the 
	///	additions are done through interface calls. A second disadvantage is that 
	///	you can't pass an IPoint to it. That's okay because as stated before, 
	///	normally the IPoint interface is not used directly!
	///	<para/>
	///	Code that uses Point&lt;T,M> can run faster:
	///	<code>
	/// public static Point&lt;T,M> Add&lt;T,M>(Point&lt;T,M> a, Point&lt;T,M> b)
	/// {
	///		return a+b;
	/// }
	///	</code>
	/// 
	/// For the sake of completeness, this library supports several operations 
	/// (Add, Subtract, Multiply, etc.) on all Point types including IPoint itself,
	/// but as you have seen, this makes the generic constraints (as well as the 
	/// definition of IPoint itself) more complicated. Realistically you will 
	/// need to use the same two constraints if you want to write fast generic 
	/// code that operates on points or vectors:
	///	<code>
	///     where Point : IPointBase&lt;T>, INewPoint&lt;Point, T>
	///	</code>
	/// </remarks>
	public interface IPoint<T> : IPointBase<T>, INewPoint<IPoint<T>,T>
	{
	}
	public interface INewPoint<Point,T>
	{
		Point New(T x, T y);
	}

	public struct Point<T> : IPoint<T>, INewPoint<Point<T>,T>
	{
		public Point(T x, T y) { _x = x; _y = y; }

		T _x, _y;
		public T X { get { return _x; } set { _x = value; } }
		public T Y { get { return _y; } set { _y = value; } }
	
		Point<T> INewPoint<Point<T>,T>.New(T x, T y) { return new Point<T>(x, y); }
		IPoint<T> INewPoint<IPoint<T>,T>.New(T x, T y) { return new Point<T>(x, y); }
	}

	/// <summary>
	/// A set of extension methods for manipulating points that implement 
	/// <see cref="IPoint{T}"/>. For better performance, it is preferable 
	/// to use the methods of <see cref="PointExt"/> instead.
	/// </summary>
	/// <remarks>
	/// Compared to PointExt, certain operations such as ShiftLeft have been left 
	/// out of this (less important) class because they are easily performed in 
	/// other ways.
	/// </remarks>
	public static class IPointExt
	{
		/// <summary>Returns the sum of two vectors.</summary>
		public static IPoint<T> Add<T,M>(this M m, IPoint<T> a, IPoint<T> b) where M:IAdditionGroup<T>
		{
			return a.New(m.Add(a.X, b.X), m.Add(a.Y, b.Y));
		}
		/// <summary>Returns the difference between two points or vectors.</summary>
		public static IPoint<T> Subtract<T,M>(this M m, IPoint<T> a, IPoint<T> b) where M:IAdditionGroup<T>
		{
			return a.New(m.Subtract(a.X, b.X), m.Subtract(a.Y, b.Y));
		}
		/// <summary>Returns a point or vector multiplied by a scaling factor.</summary>
		public static IPoint<T> Multiply<T,M>(this M m, IPoint<T> a, T factor) where M:IMultiplicationGroup<T>
		{
			return a.New(m.Multiply(a.X, factor), m.Multiply(a.Y, factor));
		}
		/// <summary>Returns a point or vector divided by a scaling factor.</summary>
		public static IPoint<T> Divide<T,M>(this M m, IPoint<T> a, T factor) where M:IField<T>
		{
			return a.New(m.Divide(a.X, factor), m.Divide(a.Y, factor));
		}
		/// <summary>Returns a point or vector by a factor, then divides by another factor.</summary>
		public static IPoint<T> MulDiv<T,M>(this M m, IPoint<T> a, T mulBy, T divBy) where M:IField<T>
		{
			return a.New(m.MulDiv(a.X, mulBy, divBy), m.MulDiv(a.Y, mulBy, divBy));
		}
		/// <summary>Gets the dot product of two vectors.</summary>
		public static T Dot<T,M>(this M m, IPoint<T> a, IPoint<T> b) where M:IField<T>
		{
			return m.Add(m.Multiply(a.X, b.X), m.Multiply(a.Y, b.Y));
		}
		/// <summary>Gets the cross product of two vectors.</summary>
		public static T Cross<T,M>(this M m, IPoint<T> a, IPoint<T> b) where M:IRing<T>
		{
			return m.Subtract(m.Multiply(a.X, b.Y), m.Multiply(a.Y, b.X));
		}
		/// <summary>Returns a vector rotated 90 degrees.</summary>
		/// <remarks>
		/// Rotatation is clockwise if increasing Y goes downward, counter-
		/// clockwise if increasing Y goes upward.
		/// </remarks>
		public static IPoint<T> Rot90<T,M>(this M m, IPoint<T> p) where M:ISignedMath<T>
		{
			return p.New(m.Negate(p.Y), p.X);
		}
		/// <summary>Returns a vector with its direction reversed.</summary>
		public static IPoint<T> Negate<T,M>(this M m, IPoint<T> p) where M:ISignedMath<T>
		{
			return p.New(m.Negate(p.X), m.Negate(p.Y));
		}
		/// <summary>Gets the square of the length of a vector.</summary>
		public static T Quadrance<T,M>(this M m, IPoint<T> p) where M:IMath<T>
		{
			return m.Add(m.Square(p.X), m.Square(p.Y));
		}
		/// <summary>Gets the length of a vector.</summary>
		public static T Length<T,M>(this M m, IPoint<T> p) where M:IMath<T>
		{
			return m.Sqrt(Quadrance(m, p));
		}
	}
	

	/// <summary>A mutable 3D point with X, Y, Z coordinates.</summary>
	/// <remarks>See <see cref="IPoint{T}"/> for more information.</remarks>
}
