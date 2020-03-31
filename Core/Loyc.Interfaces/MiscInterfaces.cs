using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc
{
	/// <summary>Interface for types that can duplicate themselves.</summary>
	/// <typeparam name="T">Normally T is the type that implements this interface.</typeparam>
	public interface ICloneable<out T>
	{
		T Clone();
	}

	/// <summary>Interface for an object that can have "tags" attached, which are 
	/// arbitrary objects reached through a key <see cref="Symbol"/>.</summary>
	public interface ITags<T>
	{
		/// <summary>Returns a dictionary that can be used to store additional state
		/// beyond the standard content of the object.
		/// </summary><remarks>
		/// Is is possible that Tags==this to reduce overhead.
		/// </remarks>
		IDictionary<Symbol, T> Tags { get; }
	}

	/// <summary>This is a tag which indicates that objects of this type are 
	/// unique; specifically, any two different objects that implement this 
	/// interface are always unequal, and one object is equal only to itself.</summary>
	/// <remarks>
	/// This interface is recognized by <see cref="Loyc.Collections.MSet{T}"/>, <see cref="Loyc.Collections.Set{T}"/>
	/// and <see cref="Loyc.Collections.InternalSet{T}"/>. It causes normal comparison (via
	/// <see cref="IEqualityComparer{T}"/> to be skipped in favor of reference 
	/// comparison. <see cref="Symbol"/> implements this interface.
	/// </remarks>
	public interface IReferenceEquatable { }

	/// <summary>Interface for things that have a Value property.</summary>
	public interface IHasValue<out T>
	{
		T Value { get; }
	}
	
	/// <summary>Interface for things that have a mutable Value property.</summary>
	public interface IHasMutableValue<T> : IHasValue<T>
	{
		new T Value { get; set; }
	}

	public interface IMaybe<out T> : IHasValue<T>
	{
		bool HasValue { get; }
	}

	/// <summary>Represents a type that holds a single value of one of two types (L or R).</summary>
	public interface IEither<out L, out R>
	{
		IMaybe<L> Left { get; }
		IMaybe<R> Right { get; }
	}
}
