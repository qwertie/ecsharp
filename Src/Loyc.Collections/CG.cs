using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>Contains global functions of Loyc.Collections that don't belong in any specific class.</summary>
	public static class CG
	{
		[ThreadStatic] public static SimpleCache<object> _objectCache;
		[ThreadStatic] public static SimpleCache<string> _stringCache;
		
		public static string Cache(string s)
		{
			if (_stringCache == null)
				_stringCache = new SimpleCache<string>();
			return _stringCache.Cache(s);
		}
		public static object Cache(object o)
		{
			string s = o as string;
			if (s != null)
				return Cache(s);
			
			if (_objectCache == null)
				_objectCache = new SimpleCache<object>();
			return _objectCache.Cache(o);
		}

	}
}
