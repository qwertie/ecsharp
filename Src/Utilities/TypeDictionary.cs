using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Utilities
{
	public class TypeDictionary<TValue> : Dictionary<Type, TValue>
	{
		public new TValue this[Type key]
		{
			get {
				TValue value;
				if (TryGetValue(key, out value))
					return value;
				throw new KeyNotFoundException("Not found: " + key.Name);
			}
		}
		public new bool TryGetValue(Type key, out TValue value)
		{
			if (base.TryGetValue(key, out value))
				return true;

			Type[] interfaces = key.GetInterfaces();
			for (int i = 0; i < interfaces.Length; i++)
				if (base.TryGetValue(interfaces[i], out value))
					return true;

			Type @base = key.BaseType;
			if (@base != null && TryGetValue(@base, out value))
				return true;

			return false;
		}
	}
}
