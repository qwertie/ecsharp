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

	/// <summary>Represents the essence of a dictionary, which returns a value given a key.</summary>
	/// <typeparam name="K">Input type.</typeparam>
	/// <typeparam name="V">Output type.</typeparam>
	/// <remarks>Consider implementing <see cref="ITryGet{K, V}"/> instead, or in addition 
	/// to this interface alone.</remarks>
	public interface IIndexed<in K, out V>
	{
		/// <summary>Gets the value associated with the specified key.</summary>
		/// <exception cref="KeyNotFoundException">The key was not found.</exception>
		/// <exception cref="ArgumentOutOfRangeException">The class implements <see cref="IReadOnlyList{V}"/>
		/// and the key is an integer index that is outside the valid range.</exception>
		/// <exception cref="IndexOutOfRangeException">The object is an array or other list,
		/// and the key is an integer index that is outside the valid range.</exception>
		V this[K key] { get; }
	}

	/// <summary>Enables access to TryGet extension methods for retrieving items from 
	/// a collection without risk of exceptions.</summary>
	public interface ITryGet<in K, out V>
	{
		/// <summary>Gets the item for the specified key or index, and does not throw an
		/// exception on failure.</summary>
		/// <param name="key">A lookup key that might be associated with a value in this 
		/// object. If K is an integer, this value could be an index into a list.</param>
		/// <param name="fail">TryGet sets this to true on failure or false on success.</param>
		/// <returns>The element at the specified index, or default(V) if the index
		/// is not valid.</returns>
		/// <remarks>
		/// This method should never intentionally throw (e.g. don't throw if key == null)
		/// although it may use third-party methods that throw (e.g. Object.Equals()).
		/// <para/>
		/// Ideally the return type would be <see cref="Maybe{T}"/> but that design would
		/// not allow variance on the output type (out V). Instead, an extension method
		/// <see cref="TryGetExt.TryGet{K, V}(ITryGet{K, V}, K)"/> is provided that returns
		/// <see cref="Maybe{V}"/>.
		/// </remarks>
		V TryGet(K key, out bool fail);
	}

	/// <summary>Standard extension methods for <see cref="ITryGet{K, V}"/>.</summary>
	public static class TryGetExt
	{
		/// <summary>Returns the value at the specified key or index, wrapped in 
		/// <see cref="Maybe{V}"/>.</summary>
		/// <param name="key">A lookup key that might be associated with a value in this 
		/// object. If K is an integer, this value could be an index into a list.</param>
		/// <returns>A value associated with the key, wrapped in <see cref="Maybe{V}"/>
		/// so that <see cref="Maybe{V}.HasValue"/> is false if lookup fails.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Maybe<V> TryGet<K, V>(this ITryGet<K, V> self, K key)
		{
			V value = self.TryGet(key, out bool fail);
			return fail ? default(Maybe<V>) : new Maybe<V>(value);
		}

		/// <summary>Returns the value at the specified key or index, or the specified
		/// default value if the key was not found.</summary>
		/// <param name="key">A lookup key that might be associated with a value in this 
		/// object. If K is an integer, this value could be an index into a list.</param>
		/// <param name="defaultValue">A value to return if lookup fails.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static V TryGet<K, V>(this ITryGet<K, V> self, K key, V defaultValue)
		{
			V value = self.TryGet(key, out bool fail);
			return fail ? defaultValue : value;
		}
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
