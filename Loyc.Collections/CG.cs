using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>Contains global functions of Loyc.Collections that don't belong in any specific class.</summary>
	public static class CG
	{
		#region Caching facility

		[ThreadStatic] public static SimpleCache<object> _objectCache;
		
		/// <summary>Passes the object through a thread-static instance of <see cref="SimpleCache{o}"/>.</summary>
		/// <remarks>If o is a string, an alternative to Caching is interning 
		/// (String.Intern("...")). The latter tends to be more dangerous 
		/// because an interned string can never be garbage-collected.
		/// <para/>
		/// Note that <see cref="SimpleCache"/> contains strong references to 
		/// cached items, and the maximum cache size is 1024 items. The references 
		/// are released when the current thread terminates or when you call 
		/// <c>ObjectCache.Clear()</c>.</remarks>
		public static object Cache(object o)
		{
			return ObjectCache.Cache(o);
		}

		/// <summary>Gets the cache used by <see cref="Cache(object)"/>.</summary>
		public static SimpleCache<object> ObjectCache
		{
			get { return _objectCache ?? (_objectCache = new SimpleCache<object>()); }
		}
		
		static readonly object[] OneDigitInts = new object[13] { -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
		/// <summary>If the specified number is in the range -3 to 9 inclusive, an 
		/// equivalent preallocated boxed integer is returned, otherwise the other
		/// overload, <c>Cache(object)</c>, is invoked to handle the request.</summary>
		/// <param name="num">An integer you want to box.</param>
		public static object Cache(int num)
		{
			if (num >= -3 && num <= 9)
				return OneDigitInts[num + 3];
			return Cache((object)num);
		}

		/// <summary>Special overload to avoid treating argument as int32 in C#.</summary>
		public static object Cache(char o) { return Cache((object)o); }
		/// <summary>Special overload to avoid treating argument as int32 in C#.</summary>
		public static object Cache(byte o) { return Cache((object)o); }
		/// <summary>Special overload to avoid treating argument as int32 in C#.</summary>
		public static object Cache(sbyte o) { return Cache((object)o); }
		/// <summary>Special overload to avoid treating argument as int32 in C#.</summary>
		public static object Cache(short o) { return Cache((object)o); }
		/// <summary>Special overload to avoid treating argument as int32 in C#.</summary>
		public static object Cache(ushort o) { return Cache((object)o); }

		/// <summary>Returns <see cref="G.BoxedTrue"/> or <see cref="G.BoxedFalse"/> depending on the parameter.</summary>
		public static object Cache(bool value)
		{
			return value ? G.BoxedTrue : G.BoxedFalse;
		}

		#endregion
	}
}
