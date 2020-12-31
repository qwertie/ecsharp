using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Collections
{
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
	/// a collection without risk of exceptions. Unlike <see cref="IReadOnlyDictionary{K, V}"/>,
	/// this interface supports variance (e.g. ITryGet{object, string} can be assigned to
	/// ITryGet{string, object}) and it does not throw when the key is null.</summary>
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
		/// This method must not throw if key == null, although it may use third-party 
		/// methods that throw (e.g. Object.Equals()).
		/// <para/>
		/// Ideally the return type would be <see cref="Maybe{T}"/>, but that design would
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

		public static Maybe<T> TryGet<T>(this ITryGet<int, T> self, int key)
		{
			T value = self.TryGet(key, out bool fail);
			return fail ? default(Maybe<T>) : new Maybe<T>(value);
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

		// Workarounds: even though interfaces such as IListSource include ITryGet<int, T>,
		// C# in 2020 is too stupid to figure out how to call the overload for ITryGet<K, V>.
		// But in the case of IListSource, it includes IReadOnlyList which has its own
		// TryGet(), so we'd need to disambiguate anyway.

		/// <inheritdoc cref="TryGet{K,V}(ITryGet{K,V}, K)"/>
		public static Maybe<T> TryGet<T>(this INegListSource<T> self, int key) => TryGet((ITryGet<int, T>)self, key);
		/// <inheritdoc cref="TryGet{K,V}(ITryGet{K,V}, K)"/>
		public static Maybe<T> TryGet<T>(this IListSource<T> self, int key) => TryGet((ITryGet<int, T>)self, key);
		/// <inheritdoc cref="TryGet{K,V}(ITryGet{K,V}, K)"/>
		public static Maybe<T> TryGet<T>(this IArray<T> self, int key) => TryGet((ITryGet<int, T>)self, key);
		/// <inheritdoc cref="TryGet{K,V}(ITryGet{K,V}, K)"/>
		public static Maybe<ILNode> TryGet(this ILNode self, int key) => TryGet((ITryGet<int, ILNode>)self, key);

		/// <inheritdoc cref="TryGet{K,V}(ITryGet{K,V}, K, V)"/>
		public static T TryGet<T>(this INegListSource<T> self,  int key, T defaultValue) => TryGet((ITryGet<int, T>)self, key, defaultValue);
		/// <inheritdoc cref="TryGet{K,V}(ITryGet{K,V}, K, V)"/>
		public static T TryGet<T>(this IListSource<T> self,     int key, T defaultValue) => TryGet((ITryGet<int, T>)self, key, defaultValue);
		/// <inheritdoc cref="TryGet{K,V}(ITryGet{K,V}, K, V)"/>
		public static T TryGet<T>(this IArray<T> self,          int key, T defaultValue) => TryGet((ITryGet<int, T>)self, key, defaultValue);
		/// <inheritdoc cref="TryGet{K,V}(ITryGet{K,V}, K, V)"/>
		public static ILNode TryGet(this ILNode self, int key, ILNode defaultValue) => TryGet((ITryGet<int, ILNode>)self, key, defaultValue);

	}

	/// <summary>Combines <see cref="IReadOnlyDictionary{K, V}"/> with related interfaces
	/// <see cref="IIndexed{K, V}"/>, <see cref="ITryGet{K, V}"/> and 
	/// <see cref="ISource{KeyValuePair{K,V}}"/>.</summary>
	/// <typeparam name="K">Used for lookups</typeparam>
	/// <typeparam name="V">Type of value associated with each key</typeparam>
	public interface IDictionarySource<K, V> : IReadOnlyDictionary<K, V>, IIndexed<K, V>, ITryGet<K, V>, ISource<KeyValuePair<K, V>>
	{
	}
}
