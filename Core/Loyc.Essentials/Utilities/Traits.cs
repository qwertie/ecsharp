using Loyc.Collections;
using Loyc.Graphs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc
{
	/// <summary>Provides efficient access to non-numeric information about a type.
	/// For numeric traits, use <see cref="Loyc.Math.Maths{T}.Traits"/> in the Loyc.Math package.</summary>
	public class Traits<T>
	{
		public static readonly Type Type = typeof(T);

		/// <summary>True if T is a value type (struct or primitive).<summary>
		public static readonly bool IsValueType = Type.IsValueType;
		/// <summary>True if T is bool, char, byte, sbyte, short, ushort, int, uint, long, 
		/// ulong, nint, nuint, IntPtr, float or double. Note that decimal and string are not 
		/// considered primitive.</summary>
		public static readonly bool IsPrimitive = Type.IsPrimitive;
		/// <summary>True if T is an interface type (not a class or struct).</summary>
		public static readonly bool IsInterface = Type.IsInterface;
		/// <summary>True if T is a ref type (e.g. ref int).</summary>
		public static readonly bool IsByRef = Type.IsByRef;
		/// <summary>True if T is a numeric enum type.</summary>
		public static readonly bool IsEnum = Type.IsEnum;

		/// <summary>Gets a list of the interfaces that T implements, sorted topologically 
		/// so that the highest-level interfaces are listed first.</summary>
		public static IReadOnlyList<Type> Interfaces => _interfaces ??= Type.GetInterfacesSortedTopologically();
		static IReadOnlyList<Type>? _interfaces;
	}

}
