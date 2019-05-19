using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

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
	/// </code>
	/// </remarks>
	public interface IArray<T> : IListSource<T>, IArraySink<T>
	{
		/// <summary>Gets or sets an element of the array-like collection.</summary>
		/// <returns>The value of the array at the specified index.</returns>
		/// <remarks>
		/// This redundant indexer is required by C# because the compiler imagines
		/// that the setter in <see cref="IArraySink{T}"/> conflicts with the getter
		/// in <see cref="IListSource{T}"/>.
		/// </remarks>
		new T this[int index] { get; set; }

		bool TrySet(int index, T value);
	}

	/// <summary>An interface for the AddRange method, part of <see cref="IListEx{T}"/>
	/// and <see cref="ICollectionEx{T}"/>, for collection types that can add multiple 
	/// items in one method call.</summary>
	public interface IAddRange<T> : ICount
	{
		void AddRange(IEnumerable<T> e);
		void AddRange(IReadOnlyCollection<T> s);
	}

	/// <summary>The batch-operation methods of <see cref="IListEx{T}"/>, mainly
	/// for collection types that can add or remove multiple items in one method 
	/// call.</summary>
	public interface IListRangeMethods<T> : IAddRange<T>
	{
		void InsertRange(int index, IEnumerable<T> s);
		void InsertRange(int index, IReadOnlyCollection<T> s);
		void RemoveRange(int index, int amount);
	}

	/// <summary>Extension methods for Loyc collection interfaces</summary>
	public static partial class LCInterfaces
	{
		public static void Resize<T>(this IListRangeMethods<T> list, int newSize)
		{
			int count = list.Count;
			if (newSize < count)
				list.RemoveRange(newSize, count - newSize);
			else if (newSize > count)
				list.InsertRange(count, (IListSource<T>)ListExt.Repeat(default(T), newSize - count));
		}
	}

	/// <summary>This interface is meant to be implemented by read-only sequence types
	/// that originally implemented <see cref="ICollection{T}"/> and want to now implement 
	/// <see cref="IReadOnlyCollection{T}"/>. It is recommended to implement 
	/// <see cref="ICollectionAndSource{T}"/> instead, but the latter requires you to 
	/// implement a couple of additional methods.</summary>
	public interface ICollectionAndReadOnly<T> : IReadOnlyCollection<T>, ICollection<T> { }

	/// <summary>This interface is to be implemented by read-only sequence types that 
	/// still want to be compatible with APIs that accept <see cref="ICollection{T}"/>.
	/// (writable collections should implement <see cref="ICollectionImpl{T}"/> instead.)
	/// </summary>
	/// <seealso cref="ICollectionAndReadOnly{T}"/>
	public interface ICollectionAndSource<T> : ICollectionSource<T>, ICollectionAndReadOnly<T> { }

	/// <summary>This interface is intended to be implemented by editable collection 
	/// classes that are not indexable lists nor dictionaries.</summary>
	/// <remarks>
	/// IReadOnlyCollection(T) and ISinkCollection(T) are subsets of the ICollection(T)
	/// interface. ICollectionEx adds the following methods that ICollection(T) lacks:
	/// AddRange() and RemoveAll().
	/// </remarks>
	public interface ICollectionEx<T> : ICollectionImpl<T>, IAddRange<T>, IIsEmpty
	{
	}

	/// <summary>This interface is intended to be implemented by editable collection 
	/// classes that are not indexable lists nor dictionaries. It is recommended to
	/// implement <see cref="ICollectionEx{T}"/> instead, but the latter requires more
	/// effort.</summary>
	/// <remarks>
	/// This interface is used in C# for disambiguation (as explained in the description
	/// of <see cref="IListImpl{T}"/>.) Variables should not have this type (except in
	/// disambiguation methods, which immediately cast the variable to another type).
	/// </remarks>
	public interface ICollectionImpl<T> : ICollection<T>, ICollectionSource<T>, ICollectionSink<T>, ICollectionAndReadOnly<T> { }

	/// <summary>This interface is intended to be implemented by all Loyc collections 
	/// that implement <see cref="IList{T}"/>. It combines the original 
	/// <see cref="IList{T}"/> interface with its component interfaces 
	/// <see cref="IReadOnlyList{T}"/> and <see cref="IListSink{T}"/>, plus 
	/// a little bit of additional functionality in <see cref="IListSource{T}"/>.</summary>
	/// <remarks>
	/// Unfortunately, as far as the C# compiler is concerned, <see cref="IList{T}"/>
	/// and <see cref="IReadOnlyList{T}"/> are unrelated, which causes problems.
	/// <para/>
	/// This interface is not meant to be used as a variable type. It exists mainly 
	/// to avoid ambiguity errors when invoking overloaded methods in plain C#. For 
	/// example, there is a TryGet() extension method for IList{T} and an identical 
	/// TryGet() method for <see cref="IListSource{T}"/>. To prevent the C# compiler 
	/// from giving an ambiguity error when you try to call TryGet(), 
	/// <ul>
	/// <li>The list class must implement this interface (or <see cref="IListEx{T}"/> or
	///    <see cref="IListAndReadOnly{T}"/> or <see cref="IListAndListSource{T}"/>, and</li>
	/// <li>There must be a third version of TryGet() that accepts the interface that
	///     combines <see cref="IList{T}"/> with <see cref="IListSource{T}"/>, namely
	///     <see cref="IListAndListSource{T}"/> (if there is an overload that accepts
	///     IListImpl, it will of course eliminate the ambiguity error when called with
	///     a class that implements IListImpl, but not when called with a class that
	///     only implements IListAndListSource.)</li>
	/// </ul>
	/// Ironically, however, if you actually try to use the list through this 
	/// interface you'll tend to get errors. For instance, both <see cref="IList{T}"/>
	/// and <see cref="IReadOnlyList{T}"/> have an indexer, so using the indexer in
	/// this interface is ambiguous. Therefore, variables should not have this type 
	/// (except parameters to disambiguation methods, in which case the parameter is 
	/// immediately casted to another type).
	/// </remarks>
	public interface IListImpl<T> : IList<T>, IListSource<T>, IListSink<T>, IListAndReadOnly<T> { }

	/// <summary>This interface is to be used by read-only sequences that 
	/// nevertheless wish to be compatible with APIs that accept <see cref="IList{T}"/>.
	/// (writable collections should implement <see cref="IListImpl{T}"/> instead.)
	/// </summary>
	public interface IListAndListSource<T> : IListSource<T>, IList<T>, ICollectionAndSource<T> { }

	/// <summary>This interface is meant to be implemented by read-only sequence 
	/// classes that originally implemented <see cref="IList{T}"/> and want to now 
	/// implement <see cref="IReadOnlyList{T}"/> and <see cref="IReadOnlyCollection{T}"/>.
	/// It is recommended to implement <see cref="IListAndListSource{T}"/> instead, 
	/// but the latter requires you to implement more methods.
	/// </summary><remarks>
	/// This interface is useful in C# for disambiguation (as explained in the description
	/// of <see cref="IListImpl{T}"/>.) Variables should not have this type (except in
	/// disambiguation methods, which immediately cast the variable to another type).
	/// </remarks>
	public interface IListAndReadOnly<T> : ICollectionAndReadOnly<T>, IList<T>, IReadOnlyList<T> { }

	/// <summary>This interface is meant to be implemented by read-only dictionary
	/// classes that originally implemented <see cref="IDictionary{K, V}"/> and now want
	/// to add its read-only version, <see cref="IReadOnlyDictionary{K, V}"/>.</summary>
	/// <remarks>
	/// This interface is used in C# for disambiguation (as explained in the description
	/// of <see cref="IListImpl{T}"/>.) Variables should not have this type (except in
	/// disambiguation methods, which immediately cast the variable to another type).
	/// </remarks>
	public interface IDictionaryAndReadOnly<K, V> : IDictionary<K, V>, IReadOnlyDictionary<K, V> { }

	/// <summary>This interface is intended to be implemented by all Loyc collections 
	/// that implement <see cref="IDictionary{K,V}"/>. It combines the original 
	/// <see cref="IDictionary{K,V}"/> interface with its component interfaces 
	/// <see cref="IReadOnlyDictionary{K,V}"/> and <see cref="IDictionarySink{K,V}"/>.</summary>
	/// <remarks>
	/// This interface is used in C# for disambiguation (as explained in the description
	/// of <see cref="IListImpl{T}"/>.) Variables should not have this type (except in
	/// disambiguation methods, which immediately cast the variable to another type).
	/// </remarks>
	public interface IDictionaryImpl<K, V> : IDictionary<K, V>, IDictionaryAndReadOnly<K, V>, IDictionarySink<K, V> { }

	/// <summary>
	/// This interface combines the original IList(T) interface with others -
	/// IListSource(T), ISinkList(T), IArray(T) - and some additional methods
	/// (e.g. RemoveAll, InsertRange).
	/// </summary>
	/// <remarks>
	/// <see cref="IArray{T}"/> (a version of <see cref="IListSource{T}"/> that adds the 
	/// writability of an array) and <see cref="IListSink{T}"/> are largely subsets of the 
	/// IList(T) interface. IListSource has a couple of methods that IList(T) does not, 
	/// while <see cref="ICollectionEx{T}"/> adds RemoveAll and AddRange. Finally,
	/// <see cref="IListRangeMethods{T}"/> adds InsertRange and RemoveRange.
	/// <para/>
	/// Using <see cref="Impl.ListExBase{T}"/> as your base class can help you implement
	/// this interface more easily.
	/// </remarks>
	public interface IListEx<T> : IListAndListSource<T>, ICollectionEx<T>, IArray<T>, IListRangeMethods<T>
	{
	}

	/// <summary>Interface for an Optimize() method.</summary>
	public interface IOptimize
	{
		/// <summary>Optimizes the data structure to consume less memory or storage space.</summary>
		/// <remarks>
		/// Typically this method will take O(N) or O(N log N) time.
		/// <para/>
		/// For example, a simple <see cref="IAutoSizeArray{T}"/> implementation
		/// could implement this method by examining the final elements and removing 
		/// any that are equal to default(T).
		/// </remarks>
		void Optimize();
	}

	/// <summary>An auto-sizing array is a list structure that allows you to modify
	/// the element at any index, including indices that don't yet exist; the
	/// collection automatically adds missing indices.</summary>
	/// <typeparam name="T">Data type of each element.</typeparam>
	/// <remarks>
	/// This interface begins counting elements at index zero. The <see
	/// cref="INegAutoSizeArray{T}"/> interface supports negative indexes.
	/// <para/>
	/// Although it is legal to set <c>this[i]</c> for any <c>i >= 0</c> (as long
	/// as there is enough memory available for required array), <c>this[i]</c>
	/// may still throw <see cref="ArgumentOutOfRangeException"/> when the 
	/// index is not yet valid. However, implementations can choose not to throw
	/// an exception and return <c>default(T)</c> instead.
	/// </remarks>
	public interface IAutoSizeArray<T> : IArray<T>, IOptimize
	{
	}
}
