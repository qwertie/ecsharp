using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Collections
{
	/// <summary>Extension methods for <see cref="ISet{K}"/> and <see cref="HashSet{K}"/>.</summary>
	public static class CollectionExt
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
	}
}
