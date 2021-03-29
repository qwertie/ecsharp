using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Collections.MutableListExtensionMethods
{
	/// <summary>Extension methods for <see cref="ICollection{T}"/>.</summary>
	public static class ICollectionExt
	{
		/// <summary>Adds data to a set (<c>set.Add(value)</c> for all values in a sequence.)</summary>
		public static void AddRange<K>(this ICollection<K> set, IEnumerable<K> list)
		{
			foreach (var item in list)
				set.Add(item);
		}

		/// <summary>Removes data from a set (<c>set.Remove(value)</c> for all values in a sequence.)</summary>
		/// <returns>The number of items removed (that had been present in the set).</returns>
		public static int RemoveRange<K>(this ICollection<K> set, IEnumerable<K> list)
		{
			int removed = 0;
			foreach (var item in list)
				if (set.Remove(item))
					removed++;
			return removed;
		}

		/// <summary>Maps a list to an array of the same length.</summary>
		public static R[] SelectArray<T, R>(this ICollection<T> input, Func<T, R> selector)
		{
			// There's no attribute like [return: MaybeNullIfNull("input")], but `input` is
			// not nullable so it won't return null if the contract is followed.
			if (input == null)
				return null!;
			R[] result = new R[input.Count];
			var e = input.GetEnumerator();
			for (int i = 0; i < result.Length; i++)
			{
				e.MoveNext();
				result[i] = selector(e.Current);
			}
			return result;
		}
	}
}
