// This file is part of the Loyc project. Licence: LGPL
using System;
using System.Collections.Generic;

namespace Loyc.Collections
{
	/// <summary>A high-performance alternative to IEnumerable(of T).</summary>
	/// <remarks>
	/// <see cref="IIterable{T}"/> is to <see cref="Iterator{T}"/> what 
	/// <see cref="IEnumerable{T}"/> is to <see cref="IEnumerator{T}"/>.
	/// <para/>
	/// If you already implement IEnumerable and want to add support for <see
	/// cref="IIterable{T}"/>, you can easily do so using the ToIterator extension 
	/// method:
	/// <code>
	/// public Iterator&lt;T> GetIterator()
	/// {
	///     return GetEnumerator().ToIterator();
	/// }
	/// </code>
	/// Otherwise, consider deriving your class from <see cref="IterableBase{T}"/>
	/// so that you don't have to implement the GetEnumerator() methods yourself.
	/// See also: <see cref="SourceBase{T}"/>, <see cref="ListSourceBase{T}"/>, 
	/// <see cref="ListExBase{T}"/> 
	/// </remarks>
	#if DotNet4
	public interface IIterable<out T> : IEnumerable<T>
	#else
	public interface IIterable<T> : IEnumerable<T>
	// Only .NET Framework 4.0 permits the first (covariant) definition, because
	// IEnumerable<T> is invariant in .NET 3.5 and earlier.
	#endif
	{
		Iterator<T> GetIterator();
	}

	/*public struct IterableEnumerable<T> : IEnumerable<T>
	{
		IIterable<T> _source;
		public IterableEnumerable(IIterable<T> source)
		{
			_source = source;
		}
		public IteratorEnumerator<T> GetEnumerator()
		{
			return new IteratorEnumerator<T>(_source.GetIterator());
		}
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetEnumerator();
		}
		System.Collections.IEnumerator  System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}*/

	public static partial class LCInterfaces
	{
		/// <summary>Determines whether the source contains a specific value.</summary>
		/// <returns>true if an element that equals 'item' was found, false otherwise.</returns>
		/// <remarks>
		/// Contains() was originally a member of the ISource(T) interface, just in 
		/// case the source had some kind of fast lookup logic. However, this is
		/// not allowed in C# 4 when T is marked as "out" (covariant), so Contains()
		/// must be an extension method.
		/// </remarks>
		public static bool Contains<T>(this IIterable<T> list, T item)
		{
			EqualityComparer<T> comparer = EqualityComparer<T>.Default;
			bool ended = false;
			var it = list.GetIterator();
			for (T current = it(ref ended); !ended; current = it(ref ended))
				if (comparer.Equals(item, current))
					return true;
			return false;
		}

		public static int CopyTo<T>(this IIterable<T> c, T[] array, int arrayIndex)
		{
			bool ended = false;
			var it = c.GetIterator();
			for (T current = it(ref ended); !ended; current = it(ref ended))
				array[arrayIndex++] = current;
			return arrayIndex;
		}
	}
}
