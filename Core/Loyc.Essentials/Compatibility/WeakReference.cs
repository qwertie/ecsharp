using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Loyc
{
	/// <summary>The new <see cref="WeakReference{T}"/> type in .NET 4.5 removes
	/// the <c>Target</c> and <c>IsAlive</c> properties. These extension methods 
	/// restore that traditional functionality, making it easier to transition
	/// from the old <c>WeakReference</c> to the new one.</summary>
	public static class WeakReferenceExt
	{
		public static T Target<T>(this WeakReference<T> r) where T:class
		{
			T t;
			r.TryGetTarget(out t);
			return t;
		}
		public static bool IsAlive<T>(this WeakReference<T> r) where T : class
		{
			T _;
			return r.TryGetTarget(out _);
		}
	}
}
