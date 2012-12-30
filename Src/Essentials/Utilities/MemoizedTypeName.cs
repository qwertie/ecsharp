using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc
{
	/// <summary>.NET Framework reflection doesn't offer complete type names for 
	/// generic types such as "List&lt;int>" (the Type.Name value of that class is 
	/// "List`1"). <see cref="GetGenericName"/> fills in the gap, and also saves the 
	/// computed name for fast repeated lookups.</summary>
	public static class MemoizedTypeName
	{
		static Dictionary<Type, string> _shortNames = new Dictionary<Type, string>();

		/// <summary>Computes a short language-agnostic name for a type, including 
		/// generic parameters, e.g. GenericName(typeof(int)) is "Int32"; 
		/// GenericName(typeof(Dictionary&lt;int, string>)) is 
		/// "Dictionary&lt;Int32, String>".</summary>
		/// <param name="type">Type whose name you want</param>
		/// <returns>Name with generic parameters, as explained in the summary.</returns>
		/// <remarks>The result is memoized for generic types, so that the name is
		/// computed only once.</remarks>
		public static string GetGenericName(Type type)
		{
			if (type == null)
				return null;
			string name;
			lock (_shortNames)
			{
				if (!_shortNames.TryGetValue(type, out name))
				{
					if (type.IsGenericType)
						_shortNames[type] = name = ComputeGenericName(type);
					else
						name = type.Name;
				}
			}
			return name;
		}

		/// <summary>Computes a type's name without memoization.</summary>
		internal static string ComputeGenericName(Type type)
		{
			string result = type.Name;
			if (type.IsGenericType)
			{
				// remove genric indication (e.g. `1)
				result = result.Substring(0, result.LastIndexOf('`'));

				result = string.Format(
					"{0}<{1}>",
					result,
					string.Join(", ", type.GetGenericArguments()
									  .Select(t => GetGenericName(t)).ToArray()));
			}
			return result;
		}

		/// <summary>Extension method on Type that is an alias for the <see cref="ShortName"/> method.</summary>
		public static string NameWithGenericParams(this Type t)
		{
			return GetGenericName(t);
		}
	}

	public static class MemoizedTypeName<T>
	{
		static string _name;
		public static string GenericName()
		{
			if (_name == null)
				_name = MemoizedTypeName.GetGenericName(typeof(T));
			return _name;
		}
	}
}
