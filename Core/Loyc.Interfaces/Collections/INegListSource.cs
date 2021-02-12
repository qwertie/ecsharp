using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>This interface is the counterpart to <see cref="IListSource{T}"/> 
	/// for lists whose minimum index is not (necessarily) zero.</summary>
	/// <remarks>
	/// Be careful not to write a loop that relies on <see cref="ICount.Count"/> or starts at
	/// zero! You must always loop from Min to Max, like so:
	/// <code>
	/// for (int i = list.Min; i &lt;= list.Max; i++) { ... }
	/// </code>
	/// </remarks>
	public interface INegListSource<out T> : IReadOnlyCollection<T>, ITryGet<int, T>, IIndexed<int, T>
	{
		/// <summary>Returns the minimum valid index in the collection.</summary>
		int Min { get; }

		/// <summary>Returns the maximum valid index in the collection.</summary>
		/// <remarks>Count must equal Max-Min+1. If Count is 0, Max = Min-1</remarks>
		int Max { get; }

		/// <summary>Returns a sub-range of this list.</summary>
		IListSource<T> Slice(int start, int count = int.MaxValue);
	}

	public static partial class LCInterfaces
	{
		/// <summary>Tries to get a value from the list at the specified index.</summary>
		/// <param name="index">The index to access. Valid indexes are between Min and Max.</param>
		/// <param name="value">A variable that will be changed to the retrieved value. If the index is not valid, this variable is left unmodified.</param>
		/// <returns>True on success, or false if the index was not valid.</returns>
		[Obsolete("Please use another overload (TryGetExt.TryGet()); this one will be deleted eventually")]
		public static bool TryGet<T>(this INegListSource<T> list, int index, ref T value)
		{
			bool fail;
			T result = list.TryGet(index, out fail);
			if (fail)
				return false;
			value = result;
			return true;
		}

		/// <summary>Determines the index of a specific value.</summary>
		/// <returns>The index of the value, if found, or null if it was not found.</returns>
		/// <remarks>
		/// At first, this method was a member of IListSource itself, just in 
		/// case the source might have some kind of fast lookup logic (e.g. binary 
		/// search) or custom comparer. However, since the item to find is an "in" 
		/// argument, it would prevent IListSource from being marked covariant when
		/// I upgrade to C# 4.
		/// </remarks>
		public static int? IndexOf<T>(this INegListSource<T> list, T item)
		{
			int max = list.Max;
			EqualityComparer<T> comparer = EqualityComparer<T>.Default;
			for (int i = list.Min; i <= max; i++)
				if (comparer.Equals(item, list[i]))
					return i;
			return null;
		}
	}

	/// <summary>This interface models the capabilities of an array: getting and
	/// setting elements by index, but not adding or removing elements. Implementing 
	/// <see cref="INegListSource{T}"/> makes it slightly different from  
	/// <see cref="IArray{T}"/>, in that indexes can be negative, so there are
	/// Min and Max properties.</summary>
	public interface INegArray<T> : INegListSource<T>, IArraySink<T>, IIndexed<int, T>
	{
		/// <summary>Gets or sets an element of the array-like collection.</summary>
		/// <returns>The value of the array at the specified index.</returns>
		/// <remarks>
		/// A redundant getter is required by C# because C# code is unable to use it
		/// (from a reference to <see cref="INegArray{T}"/>) otherwise.
		/// </remarks>
		new T this[int index] { set; get; }
		
		bool TrySet(int index, T value);
	}

	/// <summary>This is a tag interface indicating that the boundaries of the array
	/// can be expanded implcitly by writing to an index outside the range. However,
	/// the indexer may still throw when reading outside the current boundaries.
	/// To avoid exceptions, please use a TryGet extension method.</summary>
	public interface IAutoNegArray<T> : INegArray<T> { }

	/// <summary>An auto-sizing array is a list structure that allows you to modify
	/// the element at any index, including indexes that don't yet exist; the
	/// collection automatically adds missing indexes.</summary>
	/// <typeparam name="T">Data type of each element.</typeparam>
	/// <remarks>
	/// This interface allows negative indexes, unlike <see cref="IAutoSizeArray{T}"/>.
	/// </remarks>
	public interface INegAutoSizeArray<T> : INegArray<T>, IOptimize
	{
	}

	/// <summary>Represents a Deque that supports negative indexes. In this kind of
	/// Deque, pushing and popping elements does not affect the indexes of the other
	/// elements in the collection.</summary>
	/// <typeparam name="T"></typeparam>
	public interface INegDeque<T> : INegArray<T>, IDeque<T> 
	{
	}
}
