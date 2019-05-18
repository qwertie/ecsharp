using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>An interface for depositing items. Includes only an Add(T) method.</summary>
	public interface IAdd<in T>
	{
		void Add(T item);
	}

	/// <summary>Represents a write-only collection: you can modify it, but you
	/// cannot learn what it contains.</summary>
	public interface ICollectionSink<in T> : IAdd<T>
	{
		//inherited void Add(T item);
		void Clear();
		bool Remove(T item);
	}

	/// <summary>Represents a write-only array.</summary>
	public interface IArraySink<in T>
	{
		T this[int index] { set; }
	}

	/// <summary>Represents a write-only indexable list class.</summary>
	public interface IListSink<in T> : ICollectionSink<T>, IArraySink<T>
	{
	}

	/// <summary>Represents a write-only dictionary class.</summary>
	/// <remarks>
	/// The methods here are a subset of the methods of IDictionary, so that a class 
	/// that already implements IDictionary can support this interface also just by
	/// adding it to the interface list. However, one of the reasons you might want
	/// to implement this interface is to provide an asynchronous dictionary writer
	/// (in which operations are completed at a later time). In this case, the sink
	/// (dictionary writer) doesn't know whether a given key exists in the dictionary
	/// at the time <see cref="Add(K,V)"/> or <see cref="Remove(K)"/> is called, so
	/// it cannot know whether to throw <see cref="KeyAlreadyExistsException"/> or 
	/// return <c>true</c> or <c>false</c>. Such implementations should not throw 
	/// that exception, and they should return <c>true</c> from <c>Remove</c>. The 
	/// implementation might also be unable to quickly count the values in the 
	/// collection, so there is no <c>Count</c> property.
	/// <para/>
	/// Due to the above considerations, when <see cref="Remove"/> returns true, users 
	/// of this interface should not assume that the collection actually did contain 
	/// the specified key. However if it returns false, the collection definitely did 
	/// not contain it.
	/// <para/>
	/// This interface does not implement IAdd{KeyValuePair{K, V}} 
	/// because it would defeat contravariance, because structs do not support variance
	/// (KeyValuePair is a struct).
	/// </remarks>
	public interface IDictionarySink<in K, in V>
	{
		V this[K key] { set; }
		void Add(K key, V value);
		bool Remove(K key);
		void Clear();
	}
}
