using System;
using System.Collections.Generic;
using System.Text;

// Defines the "Ex" interfaces and subinterfaces
namespace Loyc.Collections
{
	/// <summary>An interface for the AddRange method, part of <see cref="IListEx{T}"/>
	/// and <see cref="ICollectionEx{T}"/>, for collection types that can add multiple 
	/// items in one method call.</summary>
	#if DotNet2 || DotNet3
	public interface IAddRange<T> : ICount
	#else
	public interface IAddRange<in T> : ICount
	#endif
	{
		void AddRange(IEnumerable<T> e);
		void AddRange(IReadOnlyCollection<T> s);
	}

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

	/// <summary>The batch-operation methods of <see cref="IListEx{T}"/>, mainly
	/// for collection types that can add or remove multiple items in one method 
	/// call.</summary>
	#if DotNet2 || DotNet3
	public interface IListRangeMethods<T> : IAddRange<T>
	#else
	public interface IListRangeMethods<in T> : IAddRange<T>
	#endif
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

	// See IDictionaryEx for extended IDictionary interface
}
