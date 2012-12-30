using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc
{
	/// <summary>You'd think the .NET framework would have a built-in method--even
	/// a CIL opcode--to bitwise-compare two values. Not supporting bitwise compare
	/// is, in my opinion, one of several mind-bogglingly stupid decisions in the
	/// CLR. Instead, all you can do is call ValueComparer.Default.Equals(a, b).
	/// </summary>
	/// <remarks>
	/// The Default.Equals method is a virtual function call, but as far as I know,
	/// in generic code there is no way to avoid this while supporting any type T.
	/// <para/>
	/// If T is a reference type, it compares the two references using
	/// ReferenceComparer. If T is a struct then this class does not currently
	/// perform a bitwise comparison, as it just uses EqualityComparer(T).Default; 
	/// however, the comparison ends up being bitwise for most value types. In the
	/// future somebody should write a fast "unsafe" bitwise comparer for value
	/// types that do not implement IEquatable, because the default implementation
	/// of Equals is documented to use reflection, so we can expect that it is 
	/// extremely slow.
	/// </remarks>
	public static class ValueComparer<T>
	{
		public static readonly EqualityComparer<T> Default = GetComparer();

		public static bool Equals(T a, T b)
		{
			return Default.Equals(a, b);
		}

		private static EqualityComparer<T> GetComparer()
		{
			if (typeof(T).IsValueType)
				return EqualityComparer<T>.Default;
			else {
				// return new ReferenceComparer<T>()
				Type type = typeof(ReferenceComparer<>).MakeGenericType(new Type[] { typeof(T) });
				return (EqualityComparer<T>)Activator.CreateInstance(type);
			}
		}
	}
	public class ReferenceComparer<T> : EqualityComparer<T> where T:class
	{
		public ReferenceComparer() {}

		public override bool Equals(T x, T y)
		{
			return x == y;
		}
		public override int GetHashCode(T obj)
		{
			if (obj == null)
				return 0;
			return obj.GetHashCode();
		}
	}
}
