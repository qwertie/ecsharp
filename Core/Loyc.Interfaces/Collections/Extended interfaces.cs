using System;
using System.Collections.Generic;
using System.Text;

// Defines the "Ex" interfaces and subinterfaces
namespace Loyc.Collections
{
	/// <summary>An interface for the AddRange method, part of <see cref="IListEx{T}"/>
	/// and <see cref="ICollectionEx{T}"/>, for collection types that can add multiple 
	/// items in one method call.</summary>
	public interface IAddRange<in T> : ICount
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
	public interface ICollectionEx<T> : ICollectionImpl<T>, IAddRange<T>, IIsEmpty //, IScannable<T>
	{
		//void AddRange(IScannable<T> e);
	}

	/// <summary>Combines <see cref="ICollectionEx{T}"/> with INotifyListChanging{T, ICollection{T}}.
	/// and INotifyListChanged{T, ICollection{T}}. This exists for completeness; as of 2020/12 there 
	/// are no implementations.</summary>
	public interface ICollectionExWithChangeEvents<T> : ICollectionEx<T>, INotifyListChanging<T, ICollection<T>>, INotifyListChanged<T, ICollection<T>>
	{
	}

	/// <summary>Combines <see cref="ICollection{T}"/> with INotifyListChanging{T, ICollection{T}}. 
	/// and INotifyListChanged{T, ICollection{T}}.</summary>
	/// <seealso cref="Loyc.Collections.CollectionWithChangeEvents{T}"/>
	public interface ICollectionWithChangeEvents<T> : ICollection<T>, INotifyListChanging<T, ICollection<T>>, INotifyListChanged<T, ICollection<T>>
	{
	}

	/// <summary>The batch-operation methods of <see cref="IListEx{T}"/>, mainly
	/// for collection types that can add or remove multiple items in one method 
	/// call.</summary>
	public interface IListRangeMethods<in T> : IAddRange<T>
	{
		void InsertRange(int index, IEnumerable<T> s);
		void InsertRange(int index, IReadOnlyCollection<T> s);
		void RemoveRange(int index, int amount);
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
		new int Count { get; } // resolve "ambiguity" between IList.Count and ICount.Count
	}

	public interface IListWithChangeEvents<T> : IListAndListSource<T>, INotifyListChanging<T>, INotifyListChanged<T>
	{
	}

	public interface IListExWithChangeEvents<T> : IListEx<T>, IListWithChangeEvents<T>
	{
	}

	/// <summary>Core functionality for Loyc collection interfaces</summary>
	public static partial class LCInterfaces
	{
		public static void Resize<T>(this IListRangeMethods<T> list, int newSize)
		{
			int count = list.Count;
			if (newSize < count)
				list.RemoveRange(newSize, count - newSize);
			else if (newSize > count)
				list.InsertRange(count, new T[newSize - count]); // ListExt.Repeat(default(T), newSize - count)
		}
		public static void Resize<T>(this IListEx<T> list, int newSize) 
			=> Resize(list as IListRangeMethods<T>, newSize);
	}

	// See IDictionaryEx for extended IDictionary interface
}
