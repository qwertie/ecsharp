using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>This interface models the capabilities of an array: getting and
	/// setting elements by index, but not adding or removing elements.</summary>
	/// <remarks>
	/// Member list:
	/// <code>
	/// public T this[int index] { get; set; }
	/// public T TryGet(int index, ref bool fail);
	/// public Iterator&lt;T> GetIterator();
	/// public int Count { get; }
	/// public IEnumerator&lt;T> GetEnumerator();
	/// System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator();
	/// </remarks>
	public interface IArray<T> : IListSource<T>, ISinkArray<T>
	{
		/// <summary>Gets or sets an element of the array-like collection.</summary>
		/// <returns>The value of the array at the specified index.</returns>
		/// <remarks>
		/// This redundant indexer is required by C# because the compiler imagines
		/// that the setter in <see cref="ISinkArray{T}"/> conflicts with the getter
		/// in <see cref="IListSource{T}"/>.
		/// </remarks>
		new T this[int index] { get; set; }

		bool TrySet(int index, T value);
	}

	/// <summary>
	/// This interface combines the original ICollection(T) interface with
	/// ISource(T) and ISinkCollection(T), a convenient way to implement all three.
	/// </summary>
	/// <remarks>
	/// ISource(T) and ISinkCollection(T) are largely subsets of the ICollection(T)
	/// interface. ICollectionEx has only one method that ICollection(T) does not
	/// (GetIterator()), and the easiest way to implement it if you already wrote 
	/// GetEnumerator() is as follows:
	/// <code>
	///     public Iterator&lt;T> GetIterator() { return GetEnumerator().ToIterator(); }
	/// </code>
	/// However, to gain the performance advantages of Iterable, it is better to
	/// implement GetIterator() instead and use the following implementations of
	/// GetEnumerator():
	/// <code>
	/// IEnumerator&lt;T> IEnumerable&lt;T>.GetEnumerator()
	/// {
	///   return GetEnumerator();
	/// }
	/// System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
	/// {
	///   return GetEnumerator();
	/// }
	/// // The C# compiler will use this method in for-loops to avoid late binding.
	/// public IteratorEnumerator&lt;T> GetEnumerator()
	/// {
	/// 	return GetIterator().ToEnumerator();
	/// }
	/// </code>
	/// </remarks>
	public interface ICollectionEx<T> : ICollection<T>, ISource<T>, ISinkCollection<T>
	{
	}

	/// <summary>
	/// This interface combines the original IList(T) interface with IArray(T)
	/// and ISinkList(T), to make implementing all three more convenient.
	/// </summary>
	/// <remarks>
	/// <see cref="IArray{T}"/> (a version of <see cref="IListSource{T}"/> that adds the writability of an
	/// array) and <see cref="ISinkList{T}"/> are largely subsets of the IList(T) interface. 
	/// IListEx has two methods that IList(T) does not (TryGet() and GetIterator()); for
	/// more information about GetIterator(), see the documentation of <see cref="IIterable{T}"/> and <see
	/// cref="ICollectionEx{T}"/>.
	/// <para/>
	/// Just as Iterator scans a collection faster than IEnumerator, TryGet() is intended to
	/// accelerate access to a list at a specific index; see <see
	/// cref="IListSource{T}"/> for more information. TryGet() may be called
	/// in different ways, through extension methods with the same name.
	/// <para/>
	/// Using <see cref="ListExBase{T}"/> as your base class can help you implement
	/// this interface faster.
	/// <para/>
	/// TODO: compiler complains of ambiguity calling methos such as Add(), this[]; find workaround
	/// </remarks>
	public interface IListEx<T> : IList<T>, ICollectionEx<T>, IArray<T>, ISinkList<T>
	{
	}

	/// <summary>An auto-sizing array is a list structure that allows you to modify
	/// the element at any index, including indices that don't yet exist; the
	/// collection automatically adds missing indices.</summary>
	/// <typeparam name="T">Data type of each element.</typeparam>
	/// <remarks>
	/// This interface begins counting elements at index zero. The <see
	/// cref="INegAutoSizeArray{T}"/> interface supports negative indexes.
	/// </remarks>
	public interface IAutoSizeArray<T> : IArray<T>
	{
		/// <summary>Optimizes the data structure to consume less memory or storage space.</summary>
		/// <remarks>
		/// A simple auto-sizing array can implement this method by examining the
		/// final elements and removing any that are equal to default(T).
		/// </remarks>
		void Optimize();
	}
}
