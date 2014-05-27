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
	public interface INegListSource<T> : IReadOnlyCollection<T>
	{
		/// <summary>Returns the minimum valid index in the collection.</summary>
		int Min { get; }

		/// <summary>Returns the maximum valid index in the collection.</summary>
		/// <remarks>Count must equal Max-Min+1. If Count is 0, Max = Min-1</remarks>
		int Max { get; }

		/// <summary>Gets the item at the specified index.</summary>
		/// <param name="index">An index in the range Min to Max.</param>
		/// <exception cref="ArgumentOutOfRangeException">The index provided is not 
		/// valid in this list.</exception>
		/// <returns>The element at the specified index.</returns>
		T this[int index] { get; }

		/// <summary>Gets the item at the specified index, and does not throw an
		/// exception on failure.</summary>
		/// <param name="index">An index in the range Min to Max.</param>
		/// <param name="fail">A flag that is set on failure. To improve
		/// performance slightly, this flag is not cleared on success.</param>
		/// <returns>The element at the specified index, or default(T) if the index
		/// is not valid.</returns>
		/// <remarks>In my original design, the caller could provide a value to 
		/// return on failure, but this would not allow T to be marked as "out" in 
		/// C# 4. For the same reason, we cannot have a ref/out T parameter.
		/// Instead, the following extension methods are provided:
		/// <code>
		///     bool TryGet(int index, ref T value);
		///     T TryGet(int, T defaultValue);
		/// </code>
		/// </remarks>
		T TryGet(int index, out bool fail);

		/// <summary>Returns a sub-range of this list.</summary>
		IRange<T> Slice(int start, int count = int.MaxValue);
	}

	public static partial class LCInterfaces
	{
		/// <summary>Tries to get a value from the list at the specified index.</summary>
		/// <param name="index">The index to access. Valid indexes are between Min and Max.</param>
		/// <param name="value">A variable that will be changed to the retrieved value. If the index is not valid, this variable is left unmodified.</param>
		/// <returns>True on success, or false if the index was not valid.</returns>
		public static bool TryGet<T>(this INegListSource<T> list, int index, ref T value)
		{
			bool fail;
			T result = list.TryGet(index, out fail);
			if (fail)
				return false;
			value = result;
			return true;
		}
		
		/// <summary>Tries to get a value from the list at the specified index.</summary>
		/// <param name="index">The index to access. Valid indexes are between Min and Max.</param>
		/// <param name="defaultValue">A value to return if the index is not valid.</param>
		/// <returns>The retrieved value, or defaultValue if the index provided was not valid.</returns>
		public static T TryGet<T>(this INegListSource<T> list, int index, T defaultValue)
		{
			bool fail;
			T result = list.TryGet(index, out fail);
			if (fail)
				return defaultValue;
			else
				return result;
		}
		
		/// <summary>Determines the index of a specific value.</summary>
		/// <returns>The index of the value, if found, or -1 if it was not found.</returns>
		/// <remarks>
		/// At first, this method was a member of IListSource itself, just in 
		/// case the source might have some kind of fast lookup logic (e.g. binary 
		/// search) or custom comparer. However, since the item to find is an "in" 
		/// argument, it would prevent IListSource from being marked covariant when
		/// I upgrade to C# 4.
		/// </remarks>
		public static int IndexOf<T>(this INegListSource<T> list, T item)
		{
			int max = list.Max;
			EqualityComparer<T> comparer = EqualityComparer<T>.Default;
			for (int i = list.Min; i <= max; i++)
				if (comparer.Equals(item, list[i]))
					return i;
			return -1;
		}
	}
	
	/// <summary>This interface models the capabilities of an array: getting and
	/// setting elements by index, but not adding or removing elements. This 
	/// interface is the counterpart to <see cref="IListSource{T}"/> 
	/// for lists whose minimum index is not (necessarily) zero.
	/// </summary>
	public interface INegArray<T> : INegListSource<T>
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

	/// <summary>An auto-sizing array is a list structure that allows you to modify
	/// the element at any index, including indexes that don't yet exist; the
	/// collection automatically adds missing indexes.</summary>
	/// <typeparam name="T">Data type of each element.</typeparam>
	/// <remarks>
	/// This interface allows negative indexes, unlike <see cref="IAutoSizeArray{T}"/>.
	/// </remarks>
	public interface INegAutoSizeArray<T> : INegArray<T>
	{
		/// <summary>Optimizes the data structure to consume less memory or storage space.</summary>
		/// <remarks>
		/// A simple auto-sizing array can implement this method by examining the
		/// elements with the most positive and most negative indexes, and removing
		/// any that are equal to default(T).
		/// </remarks>
		void Optimize();
	}

	/// <summary>Represents a Deque that supports negative indexes. In this kind of
	/// Deque, pushing and popping elements does not affect the indexes of the other
	/// elements in the collection.</summary>
	/// <typeparam name="T"></typeparam>
	public interface INegDeque<T> : INegArray<T>, IDeque<T> 
	{
	}
}
