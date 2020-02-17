using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Geometry
{
	/// <summary>Interface for reading the size of a 3D object.</summary>
	/// <typeparam name="T">Coordinate type.</typeparam>
	public interface ISize3Reader<T> : ISizeReader<T>
	{
		/// <summary>Gets the depth of a rectangle (the difference between Z coordinates).</summary>
		T Depth { get; }
	}

	/// <summary>Represents a read-only 3D rectangular prism.</summary>
	/// <remarks>
	/// The relationship between Z1, Z2 and Depth, as well as between the 
	/// other coodinates and Width/Height, depends on whether the object represents 
	/// a starting point plus a size, or a bounding rectangle or a pair of 
	/// points. Either the object stores a starting point (X1, Y1, Z1) and
	/// a size (Width, Height, Depth), or it stores a starting point and an
	/// ending point (X2, Y2, Z2).
	/// <para/>
	/// Conventionally, when using the size representation, the Width is 
	/// defined as X2 - X1, the Height as Y2 - Y1, and the Depth as Z2 - Z1.
	/// A bounding rectangle is defined slightly differently: the Width is 
	/// X2 - X1 + e, the Height is Y2 - Y1 + e, and the Depth is Z2 - Z1 + e,
	/// where e is an infitessimal value of type T (e=1 if T is an integer 
	/// type).
	/// <para/>
	/// Finally, this interface could simply represent a pair of points. In 
	/// that case, Width and Height return the absolute value of X2-X1 and 
	/// Y2-Y1 respectively.
	/// <para/>
	/// The object may or may not require X2 >= X1 and Y2 >= Y1 and Z2 >= Z1.
	/// If X1>X2 or Y1>Y2 or Z1>Z2, the rectangle is said to be "not 
	/// normalized" and the <see cref="Rectangle3Ext.IsNormal{Rect,T}(Rect)"/> extension 
	/// method returns false.
	/// </remarks>
	public interface IRectangle3Reader<T> : IRectangleReader<T>, ISize3Reader<T>
	{
		T Z1 { get; }
		T Z2 { get; }
	}

	/// <summary>Represents a mutable 3D rectangular prism.</summary>
	/// <typeparam name="T">Type of each coordinate.</typeparam>
	/// <remarks>
	/// This interface can represent one of three types of rectangles: either 
	/// "normal" rectangles with fundamental variables X, Y, Z, Width, Height, 
	/// Depth, bounding rectangles with two coordinate ranges (X1-X2), (Y1-Y2),
	/// and (Z1-Z2), or pairs of points (X1, Y1, Z1) and (X2, Y2, Z2).
	/// <para/>
	/// Because of this fact, it is not clear when if modify X1 whether this
	/// should affect X2 or Width. Similarly for Y1, Z1, X2, Y2 and Z2:
	/// should the size property or the opposite boundary change?. To resolve 
	/// this question, this interface does not allow you to modify the 
	/// coordinates individually; instead you must change them in pairs: you 
	/// either set X and Width together, or X1 and X2 together; and similarly
	/// for Y1/Y2/Height and Z1/Z2/Depth.
	/// <para/>
	/// You can also call the extension methods SetRect() or SetRange() to set
	/// all six coordinates at once.
	/// </remarks>
	/// <seealso cref="IRectangle3{T}"/>
	public interface IRectangle3Base<T> : IRectangleBase<T>, IRectangle3Reader<T>
	{
		void SetZAndDepth(T z, T depth);
		void SetZRange(T z1, T z2);
	}

	/// <summary>This interface exists to work around a limitation of C#; see
	/// <see cref="IRectangle3{T}"/>.</summary>
	public interface INewRectangle3<Rect, T>
	{
		Rect NewRect(T x, T y, T z, T width, T height, T depth);
		Rect NewRange(T x1, T y1, T z1, T x2, T y2, T z2);
	}

	/// <summary>Represents a mutable 3D rectangle.</summary>
	/// <remarks>
	/// This interface is separated into two bases, 
	/// <see cref="IRectangle3Base{T}"/> and <see cref="INewRectangle3{R,T}"/>, 
	/// for the same reason that <see cref="IPoint{T}"/>'s coordinates are 
	/// divided into <see cref="IPointBase{T}"/> and <see cref="INewPoint{P,T}"/>,
	/// as explained in the documentation of <see cref="IPoint{T}"/>. 
	/// </remarks>
	public interface IRectangle3<T> : IRectangle3Base<T>, INewRectangle3<IRectangle<T>, T>
	{
	}
}
