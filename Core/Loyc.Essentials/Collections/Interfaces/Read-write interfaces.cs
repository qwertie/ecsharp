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

	/// <summary>An interface typically implemented alongside <see cref="ICollection{T}"/>,
	/// for collection types that can add multiple items in one method call.</summary>
	public interface IAddRange<T> : ICount
	{
		void AddRange(IEnumerable<T> e);
		void AddRange(IReadOnlyCollection<T> s);
	}
	
	/// <summary>An interface typically implemented alongside <see cref="IList{T}"/> 
	/// for collection types that can add or remove multiple items in one method 
	/// call.</summary>
	public interface IListRangeMethods<T> : IAddRange<T>
	{
		void InsertRange(int index, IEnumerable<T> e);
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

		//public static void Sort<T>(this IListRangeMethods<T> list)
		//{
		//    list.Sort(0, list.Count, Comparer<T>.Default.Compare);
		//}
		//public static void Sort<T>(this IListRangeMethods<T> list, Comparison<T> comp)
		//{
		//    list.Sort(0, list.Count, comp);
		//}
		//public static void Sort<T>(this IListRangeMethods<T> list, IComparer<T> comp)
		//{
		//    list.Sort(0, list.Count, comp.Compare);
		//}
		//public static void Sort<T>(this IListRangeMethods<T> list, int index, int count, IComparer<T> comp)
		//{
		//    list.Sort(index, count, comp.Compare);
		//}
	}

	/// <summary>This interface combines the original <see cref="ICollection{T}"/> 
	/// with <see cref="IReadOnlyCollection{T}"/>. It exists for the same reason
	/// as <see cref="IListAndListSource{T}"/>, to fix ambiguity errors.</summary>
	public interface ICollectionAndReadOnly<T> : ICollection<T>, IReadOnlyCollection<T> { }

	/// <summary>
	/// This interface combines the original ICollection(T) interface with
	/// IReadOnlyCollection(T), ISinkCollection(T), and IAddRange(T), a convenient 
	/// way to implement all three.
	/// </summary>
	/// <remarks>
	/// IReadOnlyCollection(T) and ISinkCollection(T) are subsets of the ICollection(T)
	/// interface. ICollectionEx the following methods that ICollection(T) does not:
	/// AddRange() and RemoveAll().
	/// </remarks>
	public interface ICollectionEx<T> : ICollectionAndReadOnly<T>, ICollectionSink<T>, IAddRange<T>, IIsEmpty
	{
		/// <summary>Removes the all the elements that match the conditions defined 
		/// by the specified predicate.</summary>
		/// <param name="match">A delegate that defines the conditions of the elements to remove</param>
		/// <returns>The number of elements removed.</returns>
		int RemoveAll(Predicate<T> match);
		// A reasonable default implementation for lists:
		// int RemoveAll(Predicate<T> match) { return LCExt.RemoveAll(this, match); }
	}

	/// <summary>This interface combines the original <see cref="IList{T}"/> 
	/// interface with its "source" (read-only) component interfaces, including 
	/// <see cref="IReadOnlyList{T}"/>, plus <see cref="IListSource{T}"/>.</summary>
	/// <remarks>
	/// This interface is not meant to be used by callers. It exists mainly to 
	/// avoiding ambiguity errors when invoking extension methods in plain C#. For 
	/// example, there is a TryGet() extension method for IList(T) and an identical 
	/// TryGet() method for IListSource(T). To prevent the C# from giving an ambiguity 
	/// error when you try to use TryGet(), 
	/// <ul>
	/// <li>The list class must implement this interface (or <see cref="IListEx{T}"/>), and</li>
	/// <li>There must be a third version of TryGet() that accepts this interface.</li>
	/// </ul>
	/// Ironically, however, if you actually try to use the list through this 
	/// interface you'll tend to get errors. For instance, both <see cref="IList{T}"/>
	/// and <see cref="IReadOnlyList{T}"/> have an indexer, so using the indexer in
	/// this interface is ambiguous.
	/// <para/>
	/// In Enhanced C# I plan to add some kind of prioritization feature that will 
	/// eliminate the need for interfaces like this one.
	/// <para/>
	/// Does not include <see cref="IListSink{T}"/> because this interface may be 
	/// implemented by list classes that are read-only.
	/// </remarks>
	public interface IListAndListSource<T> : IList<T>, IListSource<T>, ICollectionAndReadOnly<T> { }

	/// <summary>
	/// This interface combines the original IList(T) interface with several
	/// IListSource(T), ISinkList(T), IArray(T) and several additional methods
	/// (e.g. RemoveAll, InsertRange).
	/// </summary>
	/// <remarks>
	/// <see cref="IArray{T}"/> (a version of <see cref="IListSource{T}"/> that adds the writability of an
	/// array) and <see cref="IListSink{T}"/> are largely subsets of the IList(T) interface. 
	/// IListSource has two methods that IList(T) does not (TryGet() and Slice()), while
	/// <see cref="ICollectionEx{T}"/> adds RemoveAll and AddRange.
	/// <para/>
	/// Just as Iterator scans a collection faster than IEnumerator, TryGet() is intended to
	/// accelerate access to a list at a specific index; see <see
	/// cref="IListSource{T}"/> for more information. TryGet() may be called
	/// in different ways, through extension methods with the same name.
	/// <para/>
	/// Using <see cref="Impl.ListExBase{T}"/> as your base class can help you implement
	/// this interface more easily.
	/// </remarks>
	public interface IListEx<T> : IListAndListSource<T>, ICollectionEx<T>, IArray<T>, IListRangeMethods<T>
	{
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
