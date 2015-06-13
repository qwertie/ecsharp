// Generated from LinqToCollections.ecs by LeMP custom tool. LLLPG version: 1.3.0.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Collections.Generic;
using System.Linq;
namespace Loyc.Collections
{
	/// <summary>
	/// Work in progress. This class will enhance LINQ-to-Objects with 
	/// type-preserving and/or higher-performance extension methods.
	/// </summary><remarks>
	/// For example, the <see cref="Enumerable.Last(IEnumerable{T})"/> extension 
	/// method scans the entire list before returning the last item, while 
	/// <see cref="Last(IReadOnlyList{T})"/> and <see cref="Last(IList{T})"/> simply
	/// return the last item directly.
	/// </remarks>
	public static class LinqToCollections
	{
		public static int Count<T>(this IList<T> list)
		{
			return list.Count;
		}
		public static T Last<T>(this IList<T> list)
		{
			int last = list.Count - 1;
			if (last < 0)
				throw new EmptySequenceException();
			return list[last];
		}
		public static T LastOrDefault<T>(this IList<T> list, T defaultValue = default(T))
		{
			int last = list.Count - 1;
			return last < 0 ? defaultValue : list[last];
		}
		public static int Count<T>(this IReadOnlyList<T> list)
		{
			return list.Count;
		}
		public static T Last<T>(this IReadOnlyList<T> list)
		{
			int last = list.Count - 1;
			if (last < 0)
				throw new EmptySequenceException();
			return list[last];
		}
		public static T LastOrDefault<T>(this IReadOnlyList<T> list, T defaultValue = default(T))
		{
			int last = list.Count - 1;
			return last < 0 ? defaultValue : list[last];
		}
	}
}
