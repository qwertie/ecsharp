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
	#if CSharp4
	public interface ICollectionSink<in T> : IAdd<T>
	#else
	public interface ICollectionSink<T> : IAdd<T>
	#endif
	{
		//inherited void Add(T item);
		void Clear();
		bool Remove(T item);
	}

	/// <summary>Represents a write-only array.</summary>
	#if CSharp4
	public interface IArraySink<in T>
	#else
	public interface IArraySink<T>
	#endif
	{
		T this[int index] { set; }
	}

	/// <summary>Represents a write-only indexable list class.</summary>
	#if CSharp4
	public interface IListSink<in T> : ICollectionSink<T>, IArraySink<T>
	#else
	public interface IListSink<T> : ICollectionSink<T>, IArraySink<T>
	#endif
	{
	}
}
