using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
	public interface IArray<T> : IListSource<T>, IArraySink<T>, IIndexed<int, T>
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

	/// <summary>Represents the essence of a mutable dictionary: the ability to get or set 
	/// a value for a key.</summary>
	public interface IMIndexed<in K, V> : IIndexed<K, V>, IIndexedSink<K, V>
	{
		/// <summary>Gets the value associated with the specified key.</summary>
		/// <remarks>This property should not exist. It exists only to resolve the 
		/// supposed "ambiguity" between the getter and setter in the base interfaces.</remarks>
		new V this[K key] { get; set; }
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
