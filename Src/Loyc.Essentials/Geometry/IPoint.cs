using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Math;

namespace Loyc.Geometry
{
	public interface IPointReader<T>
	{
		T X { get; }
		T Y { get; }
	}

	/// <summary>This interface exists to work around a limitation of C#; use 
	/// <see cref="IPoint{T}"/> instead.</summary>
	/// <remarks>
	/// C# cannot combine a getter property and a setter property from two 
	/// interfaces, so this interface cannot inherit its getters from <see 
	/// cref="IPointReader{T}"/>. The workaround is to define another getter in 
	/// the read-write interface for each getter in the read-only interface. As far 
	/// as the CLR is concerned, the two getters are unrelated, but you won't 
	/// notice that unless you need to explicitly implement this interface.
	/// </remarks>
	public interface IPointBase<T> : IPointReader<T>
	{
		/// <summary>Horizontal coordinate of a point or vector.</summary>
		/// <remarks>In geographic points, X represents the longitude.</remarks>
		new T X { get; set; }
		/// <summary>Vertical coordinate of a point or vector.</summary>
		/// <remarks>
		/// In 3D spaces, Y is sometimes used as a depth coordinate instead.
		/// In geographic points, Y represents the latitude.
		/// </remarks>
		new T Y { get; set; }
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
	/// Normally this interface is not used directly, and the only operation
	/// provided is New(). It is provided in case you want it, but generally
	/// it's better to use <see cref="Point{T}"/>.
	/// <para/>
	/// In order for this interface to work more easily in generic code, there is 
	/// no corresponding IVector type for vectors because generic code must declare 
	/// every type it needs as a separate type parameter, which makes the code very 
	/// cumbersome to write already, even without a point/vector distinction.
	/// <para/>
	/// The New() method is not normally used in generic code because it returns 
	/// IPoint&lt;T>, not the original point type. It is provided mainly in case 
	/// somebody wants to use the raw interface to manipulate points.
	/// <para/>
	/// Due to a limitation of C#, the X and Y coordinates are separated into a 
	/// separate interface (<see cref="IPointBase{T}"/>) from the New() method in
	/// <see cref="INewPoint{Point,T}"/>. Without this separation, it's impossible 
	/// to write fast generic code that can operate on both IPoint itself and on 
	/// concrete types such as <see cref="Point{T}"/>. The reason for this is very 
	/// subtle. To understand it, consider the following generic method that adds 
	/// two points together:
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
	///	to INewPoint&lt;Point,T>.New()? Remember, IPoint is not the same as Point 
	///	from the compiler's perspective--IPoint is an interface, but Point is 
	///	typically a struct (it could be the same as IPoint, but in general, it is 
	///	not). The compiler doesn't know which version of New() to call, so it 
	///	refuses to compile the code. It will compile if we change the method body 
	///	to 
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
	///	the limitation I am trying to avoid!
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
	/// So, in summary, supporting fast generic code that can also operate on IPoint
	/// requires this odd arrangement of interfaces, and if you want to write such
	/// generic code then you will need three type parameters (Point, T and M) with
	/// the following constraints:
	///	<code>
	///     where Point : IPointBase&lt;T>, INewPoint&lt;Point, T>
	///     where M : IMath&lt;T> // or another math interface
	///	</code>
	///	It may help to place your methods in a generic class (of Point, T and M) so 
	///	that you only have to write the constraints once.
	/// </remarks>
	public interface IPoint<T> : IPointBase<T>, INewPoint<IPoint<T>,T>
	{
	}
	
	/// <summary>This interface exists to work around a limitation of C#; see
	/// <see cref="IPoint{T}"/>.</summary>
 	public interface INewPoint<Point,T>
	{
		Point New(T x, T y);
	}
}
