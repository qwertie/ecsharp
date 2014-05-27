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
	#if DotNet2 || DotNet3
	public interface IAutoCreatePool<TKey, TValue> : IReadOnlyCollection<TValue>
	#else
	public interface IAutoCreatePool<in TKey, out TValue> : IReadOnlyCollection<TValue>
	#endif
	{
		/// <summary>Gets or creates the value associated with the specified key.</summary>
		/// <exception cref="ArgumentOutOfRangeException">The key was not valid
		/// for this list.</exception>
		/// <param name="key">A key object.</param>
		/// <returns>The associated value object, which is created automatically 
		/// if it does not already exist.</returns>
		TValue this[TKey key] { get; }

		/// <summary>Gets the item with the specified key, if it was created earlier.</summary>
		/// <returns>The value corresponding to the specified key, or 
		/// <c>default(TValue)</c> if the value has not been created.</returns>
		TValue GetIfExists(TKey key);
	}
}
