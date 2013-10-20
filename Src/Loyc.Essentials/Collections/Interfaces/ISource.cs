// This file is part of the Loyc project. Licence: LGPL
using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Essentials;

namespace Loyc.Collections
{
	/// <summary>
	/// Encapsulates GetIterator() and a Count property.
	/// </summary>
	/// <remarks>
	/// Member list:
	/// <code>
	/// public Iterator&lt;T> GetIterator();
	/// public int Count { get; }
	/// public IEnumerator&lt;T> GetEnumerator();
	/// System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator();
	/// </code>
	/// The term "source" means a read-only collection, as opposed to a "sink"
	/// which would be a write-only collection. The purpose of ISource is to make
	/// it easier to implement a read-only collection, by lifting the requirement
	/// to write implementations for Add(), Remove(), etc.
	/// <para/>
	/// Originally this interface had a Contains() method, just in case the source 
	/// had some kind of fast lookup logic (e.g. hashtable). However, this is 
	/// not allowed in C# 4 when T is marked as "out" (covariant), so Contains() 
	/// must be an extension method.
	/// </remarks>
	#if DotNet4
	public interface ISource<out T> : IEnumerable<T>, ICount
	#else
	public interface ISource<T> : IEnumerable<T>, ICount
	#endif
	{
	}

	public static partial class LCInterfaces
	{
		public static void CopyTo<T>(this IReadOnlyCollection<T> c, T[] array, int arrayIndex)
		{
			int space = array.Length - arrayIndex;
			if (c.Count > space)
				throw new ArgumentException(Localize.From("CopyTo: array is too small ({0} < {1})", space, c.Count));
			foreach (var item in c)
				array[arrayIndex++] = item;
		}
	}
}
