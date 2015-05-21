// Generated from LinqToCollections.ecs by LLLPG custom tool. LLLPG version: 1.3.0.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// --verbose             Allow verbose messages (shown as 'warnings')
// --no-out-header       Suppress this message
using System;
using System.Collections.Generic;
using System.Linq;
namespace Loyc.Collections
{
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
