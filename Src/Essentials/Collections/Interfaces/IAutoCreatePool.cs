using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>Represents a pool of objects in which an object is
	/// automatically created when requested by its key.</summary>
	/// <typeparam name="TKey">Key type.</typeparam>
	/// <typeparam name="TValue">Value type.</typeparam>
	/// <remarks>This design assumes that the values in the pool know their own 
	/// key, so it implements IEnumerable{TValue} rather than 
	/// IEnumerable{KeyValuePair{TKey,TValue}}.</remarks>
	#if DotNet4
	public interface IAutoCreatePool<in TKey, out TValue> : ISource<TValue>
	#else
	public interface IAutoCreatePool<TKey, TValue> : ISource<TValue>
	#endif
	{
		/// <summary>Gets the item at the specified index.</summary>
		/// <exception cref="ArgumentOutOfRangeException">The index was not valid
		/// in this list.</exception>
		/// <param name="index">An index in the range 0 to Count-1.</param>
		/// <returns>The element at the specified index.</returns>
		TValue this[TKey key] { get; }

		/// <summary>Gets the item with the specified key, if it was created earlier.</summary>
		/// <returns>The value corresponding to the specified key, or default(T) if 
		/// the value has not been created.</returns>
		TValue GetIfExists(TKey key);
	}
}
