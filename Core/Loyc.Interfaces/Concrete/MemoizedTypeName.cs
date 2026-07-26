using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Loyc
{
	/// <summary>.NET Framework reflection doesn't offer complete type names for 
	/// generic types such as "List&lt;int>" (the <c>Type.Name</c> value of that class is 
	/// "List`1"). <see cref="Get"/> fills in the gap, and also saves the 
	/// computed name for fast repeated lookups.</summary>
	public static class MemoizedTypeName
	{
		static readonly ConcurrentDictionary<Type, string> _shortNames = new ConcurrentDictionary<Type, string>();

		/// <summary>Computes a short language-agnostic name for a type, including 
		/// generic parameters, e.g. GenericName(typeof(int)) is "Int32"; 
		/// GenericName(typeof(Dictionary&lt;int, string>)) is 
		/// "Dictionary&lt;Int32, String>".</summary>
		/// <param name="type">Type whose name you want</param>
		/// <returns>Name with generic parameters, as explained in the summary.</returns>
		/// <remarks>The result is memoized, so that the name is computed only once.</remarks>
		[return: NotNullIfNotNull("type")]
		public static string? Get(Type? type)
		{
			if (type == null)
				return null;
			if (_shortNames.TryGetValue(type, out string? name))
				return name;
			// ConcurrentDictionary does not hold a lock while running the value factory,
			// so the recursive Get() calls inside ComputeGenericName cannot deadlock
			// (they may duplicate work under contention, but the result is the same
			// string). Non-generic names are memoized too, so that the TryGetValue above
			// is a complete fast path for every type.
			return _shortNames.GetOrAdd(type, ComputeGenericName);
		}

		/// <summary>Computes a type's name without memoization.</summary>
		internal static string ComputeGenericName(Type type)
		{
			string result = type.Name;
			
			if (type.IsGenericType)
			{
				// remove generic indication (e.g. `1). Careful: a NON-generic type nested
				// inside a generic type (e.g. Outer<int>.Inner) also reports IsGenericType,
				// but its Name has no arity suffix, so LastIndexOf returns -1 and the old
				// unguarded Substring(0, -1) threw ArgumentOutOfRangeException.
				int tick = result.LastIndexOf('`');
				if (tick >= 0)
					result = result.Substring(0, tick);

				result = string.Format(
					"{0}<{1}>",
					result,
					string.Join(", ", type.GetGenericArguments()
									  .Select(t => Get(t)).ToArray()));
			}
			return result;
		}

		[Obsolete("I accidentally made two of these methods; this one is less popular. Use NameWithGenericArgs() or "+nameof(MemoizedTypeName)+".Get instead")]
		public static string NameWithGenericParams(this Type t)
		{
			return Get(t);
		}
	}

	/// <summary><c>MemoizedTypeName&lt;T>.Get()</c> is an alternative to
	/// <see cref="MemoizedTypeName.Get"/>(typeof(T)).</summary>
	/// <remarks>This class is faster for getting the same name repeatedly, but 
	/// demands more memory and initialization overhead from the CLR.</remarks>
	public static class MemoizedTypeName<T>
	{
		static string? _name;
		public static string Get()
		{
			if (_name == null)
				_name = MemoizedTypeName.Get(typeof(T));
			return _name;
		}
	}
}
