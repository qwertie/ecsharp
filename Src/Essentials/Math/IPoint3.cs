using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Math
{
	public interface IPoint3Reader<T> : IPointReader<T>
	{
		T Z { get; }
	}

	/// <summary>This interface exists to work around a limitation of C#; use 
	/// <see cref="IPoint{T}"/> instead.</summary>
	/// <remarks>
	/// C# cannot combine a getter property and a setter property from two 
	/// interfaces, so this interface cannot inherit its getters from <see 
	/// cref="IPoint3Reader{T}"/>. The workaround is to define another getter in 
	/// the read-write interface for each getter in the read-only interface. As far 
	/// as the CLR is concerned, the two getters are unrelated, but you won't 
	/// notice that unless you need to explicitly implement this interface.
	/// </remarks>
	public interface IPoint3Base<T> : IPointBase<T>, IPoint3Reader<T>
	{
		/// <summary>Z coordinate of a point or vector.</summary>
		/// <remarks>
		/// Z typically represents either the vertical or depth component of a 
		/// point or vector.
		/// </remarks>
		new T Z { get; set; }
	}

	/// <summary>A mutable 3D point with X, Y, and Z coordinates.</summary>
	/// <remarks>
	/// WARNING: When casting a point (or vector) structure to this interface, 
	/// it is boxed, making a copy. Changes made through a reference to IPoint3 do 
	/// not affect the original point!
	/// <para/>
	/// The coordinates of an IPoint3 are separated into a base interface, 
	/// <see cref="IPoint3Base{T}"/>, for the same reason that <see cref="IPoint{T}"/>'s
	/// coordinates are separated into <see cref="IPointBase{T}"/>, as explained in 
	/// the documentation of <see cref="IPoint{T}"/>. 
	/// <para/>
	/// For better or for worse, this interface design does not include the <see 
	/// cref="IPoint{T}.New"/> method, so you cannot (via this interface alone) 
	/// convert a 3D point to a 2D point (although <see cref="IPointBase{T}"/>
	/// provides a 2D view.)
	/// </remarks>
	public interface IPoint3<T> : IPoint3Base<T>, INewPoint3<IPoint<T>,T>
	{
	}
	
	/// <summary>This interface exists to work around a limitation of C#; see
	/// <see cref="IPoint{T}"/> and <see cref="IPoint3{T}"/>.</summary>
	public interface INewPoint3<Point,T>
	{
		Point New(T x, T y, T z);
	}
}
