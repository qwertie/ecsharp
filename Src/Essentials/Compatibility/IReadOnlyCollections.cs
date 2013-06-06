using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
	#if !DotNet45
	/// <summary>Read-only interface defined in .NET 4.5, compare with Loyc's 
	/// <see cref="IReadCollection<T>"/>.</summary>
	public interface IReadOnlyCollection<out T> : IEnumerable<T>
	{
		int Count { get; }
	}

	/// <summary>Read-only interface defined in .NET 4.5, compare with Loyc's 
	/// <see cref="IReadList<T>"/>.</summary>
	public interface IReadOnlyList<out T> : IReadOnlyCollection<T>, IEnumerable<T>
	{
		T this[int index] { get; }
	}

	/// <summary>Read-only dictionary interface defined in .NET 4.5</summary>
	public interface IReadOnlyDictionary<TKey, TValue> : IReadOnlyCollection<KeyValuePair<TKey, TValue>>
	{
		TValue this[TKey key] { get; }
		IEnumerable<TKey> Keys { get; }
		IEnumerable<TValue> Values { get; }
		bool ContainsKey(TKey key);
		bool TryGetValue(TKey key, out TValue value);
	}

	#endif
}
