using System;
using System.Collections.Generic;
using System.Text;

// See documentation of IListImpl to understand why these interfaces exist
namespace Loyc.Collections
{
	#region ICollection variants: ICollectionAndReadOnly, ICollectionAndSource, ICollectionImpl

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
	/// classes that are not indexable lists nor dictionaries. It is recommended to
	/// implement <see cref="ICollectionEx{T}"/> instead, but the latter requires more
	/// effort.</summary>
	/// <remarks>
	/// This interface is used in C# for disambiguation (as explained in the description
	/// of <see cref="IListImpl{T}"/>.) Variables should not have this type (except in
	/// disambiguation methods, which immediately cast the variable to another type).
	/// </remarks>
	public interface ICollectionImpl<T> : ICollection<T>, ICollectionSource<T>, ICollectionSink<T>, ICollectionAndReadOnly<T> { }

	#endregion

	#region IList variants: IListAndReadOnly, IListAndListSource, IListImpl

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

	/// <summary>This interface is to be used by read-only sequences that 
	/// nevertheless wish to be compatible with APIs that accept <see cref="IList{T}"/>.
	/// (writable collections should implement <see cref="IListImpl{T}"/> instead.)
	/// </summary>
	public interface IListAndListSource<T> : IListAndReadOnly<T>, IListSource<T>, IList<T>, ICollectionAndSource<T> { }

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

	#endregion

	#region IDictionary variants: IDictionaryAndReadOnly, IDictionaryImpl

	/// <summary>This interface is meant to be implemented by read-only dictionary
	/// classes that originally implemented <see cref="IDictionary{K, V}"/> and now want
	/// to add its read-only version, <see cref="IReadOnlyDictionary{K, V}"/>.</summary>
	/// <remarks>
	/// This interface is used in C# for disambiguation (as explained in the description
	/// of <see cref="IListImpl{T}"/>.) Variables should not have this type (except in
	/// disambiguation methods, which immediately cast the variable to another type).
	/// </remarks>
	public interface IDictionaryAndReadOnly<K, V> : IDictionary<K, V>, IReadOnlyDictionary<K, V>, IIndexed<K, V> { }

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

	#endregion
}
