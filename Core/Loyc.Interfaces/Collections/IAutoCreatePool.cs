using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
	public interface IAutoCreatePool<in TKey, out TValue> : IReadOnlyCollection<TValue>, IIndexed<TKey, TValue>
	{
		/// <summary>Gets the item with the specified key, if it was created earlier.</summary>
		/// <returns>The value corresponding to the specified key, or 
		/// <c>default(TValue)</c> if the value has not been created.</returns>
		[return: MaybeNull]
		TValue GetIfExists(TKey key);
	}
}
