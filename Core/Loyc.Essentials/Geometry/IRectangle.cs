using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Math;

namespace Loyc.Geometry
{
	/// <summary>Interface for reading the size of a 2D object.</summary>
	/// <typeparam name="T">Coordinate type.</typeparam>
	public interface ISizeReader<T>
	{
		/// <summary>Gets the width of a rectangle (the difference between X coordinates).</summary>
		T Width { get; }
		/// <summary>Gets the height of a rectangle (the difference between Y coordinates).</summary>
		T Height { get; }
	}

	/// <summary>Represents a read-only 2D rectangle.</summary>
	/// <remarks>
	/// The relationship between X1, X2 and Width, and the relationship between 
	/// Y1, Y2 and Height, depends on whether the object represents a normal 
	/// (traditional Windows) rectangle, a bounding rectangle, or a pair of 
	/// points. A traditional rectangle defines its Height as Y2 - Y1, and its
	/// Width as X2 - X1. A bounding rectangle is defined slightly differently:
	/// the Width is X2 - X1 + e, and the Height is Y2 - Y1 + e, where e is an
	/// infitessimal value of type T (e=1 if T is an integer type).
	/// <para/>
	/// Also, a traditional rectangle is stored as a starting point (X1, Y1)
	/// and a size (Width, Height), while a bounding rectangle stores X2 and Y2 
	/// instead, not the Width and Height.
	/// <para/>
	/// Finally, this interface could simply represent a pair of points. In 
	/// that case, Width and Height return the absolute value of X2-X1 and 
	/// Y2-Y1 respectively.
	/// <para/>
	/// A rectangle may or may not require X2 >= X1 and Y2 >= Y1. If X1>X2 or
	/// Y1>Y2, the rectangle is said to be "not normalized" and the 
	/// <see cref="RectangleExt.IsNormal"/> extension method returns false.
	/// </remarks>
	public interface IRectangleReader<T> : ISizeReader<T>
	{
		T X1 { get; }
		T Y1 { get; }
		T X2 { get; }
		T Y2 { get; }
	}

	/// <summary>Represents a mutable 2D rectangle.</summary>
	/// <typeparam name="T">Type of each coordinate.</typeparam>
	/// <remarks>
	/// This interface can represent one of three types of rectangles: either 
	/// "normal" rectangles with fundamental variables X, Y, Width, Height, 
	/// bounding rectangles with two coordinate ranges (X1-X2) and (Y1-Y2), or
	/// or pairs of points (X1, Y1) and (X2, Y2).
	/// <para/>
	/// Because of this fact, it is not clear when you modify X1 whether this
	/// should affect X2 or Width. Similarly, when you modify X2, Y1 or Y2, it
	/// is not clear whether the size property or the opposite boundary should 
	/// change. To resolve this question, this interface does not allow you to
	/// modify the coordinates individually; instead you must change them in 
	/// pairs: you either set X and Width together, or X1 and X2 together; and
	/// similarly Y and Height together, or Y1 and Y2 together.
	/// <para/>
	/// You can also call the extension methods SetRect() or SetRange() to set
	/// all four coordinates at once.
	/// </remarks>
	/// <seealso cref="IRectangle{T}"/>
	/// <seealso cref="BoundingBox{T}"/>
	public interface IRectangleBase<T> : IRectangleReader<T>
	{
		void SetXAndWidth(T x, T width);
		void SetYAndHeight(T y, T height);
		void SetXRange(T x1, T x2);
		void SetYRange(T x1, T x2);
	}
	
	/// <summary>This interface exists to work around a limitation of C#; see
	/// <see cref="IRectangle{T}"/>.</summary>
	public interface INewRectangle<Rect, T>
	{
		Rect NewRect(T x, T y, T width, T height);
		Rect NewRange(T x1, T y1, T x2, T y2);
	}
	
	/// <summary>Represents a mutable 2D rectangle.</summary>
	/// <remarks>
	/// This interface is separated into two bases, 
	/// <see cref="IRectangleBase{T}"/> and <see cref="INewRectangle{R,T}"/>, 
	/// for the same reason that <see cref="IPoint{T}"/>'s coordinates are 
	/// divided into <see cref="IPointBase{T}"/> and <see cref="INewPoint{P,T}"/>,
	/// as explained in the documentation of <see cref="IPoint{T}"/>. 
	/// </remarks>
	public interface IRectangle<T> : IRectangleBase<T>, INewRectangle<IRectangle<T>, T>
	{
	}
}
