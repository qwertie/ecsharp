// ISetImm and its component interfaces, ISetTests and ISetOperations
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>Set testing operations.</summary>
	/// <typeparam name="SetT">Type of set that this set can be tested against.</typeparam>
	public interface ISetTests<SetT>
	{
		bool IsProperSubsetOf(SetT other);
		bool IsProperSupersetOf(SetT other);
		bool IsSubsetOf(SetT other);
		bool IsSupersetOf(SetT other);
		bool Overlaps(SetT other);
		bool SetEquals(SetT other);
	}

	/// <summary>Set-combining operations: With, Without, Union, Intersect, Except, Xor.</summary>
	/// <typeparam name="T">Type of items in the set.</typeparam>
	/// <typeparam name="SetT">Type of the set itself.</typeparam>
	#if !CSharp4
	public interface ISetOperations<in T, SetT> : ISetOperations<T, SetT, SetT> { }
	public interface ISetOperations<in T, in InSetT, out OutSetT>
	#else
	public interface ISetOperations<T, SetT> : ISetOperations<T, SetT, SetT> { }
	/// <summary>Set-combining operations: With, Without, Union, Intersect, Except, Xor.</summary>
	/// <typeparam name="T">Type of items in the set.</typeparam>
	/// <typeparam name="InSetT">Data type of the input sets that you pass to methods of this interface.</typeparam>
	/// <typeparam name="OutSetT">Data type of the output sets returned from this interface.</typeparam>
	public interface ISetOperations<T, InSetT, OutSetT>
	#endif
	{
		OutSetT With(T item);
		OutSetT Without(T item);
		OutSetT Union(InSetT other);
		OutSetT Intersect(InSetT other);
		OutSetT Except(InSetT other);
		OutSetT Xor(InSetT other);
	}

	/// <summary>An immutable set of type SetT with elements of type T.</summary>
	/// <typeparam name="SetT">Type of this set.</typeparam>
	/// <typeparam name="T">Type of items in the set</typeparam>
	public interface ISetImm<T, SetT> : ISetOperations<T, SetT>, ISetTests<SetT>, ICollectionSource<T> //ICount
	{
		/// <summary>Returns true if the set is inverted, which means that the
		/// enumerator returns all the items that are <i>not</i> in the set, 
		/// and the <see cref="IReadOnlyCollection{T}.Count"/> returns the number 
		/// of items that are not in the set.</summary>
		/// <remarks><see cref="Loyc.Collections.InvertibleSet{T}"/> is an example 
		/// of a set that can be inverted. In most set implementations, this 
		/// property always returns false.</remarks>
		bool IsInverted { get; }
	}

	/// <summary>An immutable set that supports numerous set operations.</summary>
	/// <typeparam name="T">Type of items in the set</typeparam>
	public interface ISetImm<T> : ISetOperations<T, IEnumerable<T>, ISetImm<T>>, ISetTests<IEnumerable<T>>, IReadOnlyCollection<T>
	{
		//bool IsProperSubsetOf(IEnumerable<T> other);
		//bool IsProperSupersetOf(IEnumerable<T> other);
		//bool IsSubsetOf(IEnumerable<T> other);
		//bool IsSupersetOf(IEnumerable<T> other);
		//bool Overlaps(IEnumerable<T> other);
		//bool SetEquals(IEnumerable<T> other);
		//ISetImm<T> With(T item);
		//ISetImm<T> Without(T item);
		//ISetImm<T> Union(IEnumerable<T> other);
		//ISetImm<T> Intersect(IEnumerable<T> other);
		//ISetImm<T> Except(IEnumerable<T> other);
		//ISetImm<T> Xor(IEnumerable<T> other);
		//bool IsInverted { get; }
	}
}
