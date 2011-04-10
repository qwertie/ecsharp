using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Essentials
{
	/// <summary>Abstract class that helps you implement wrappers by automatically
	/// forwarding calls to Equals(), GetHashCode() and ToString().</summary>
	public abstract class WrapperBase<T>
	{
		protected T _obj;
		protected WrapperBase(T wrappedObject)
		{
			if (wrappedObject == null)
				throw new ArgumentNullException("wrappedObject");
			_obj = wrappedObject;
		}

		public override bool Equals(object obj)
		{
			if (obj is WrapperBase<T>)
				return _obj.Equals((obj as WrapperBase<T>)._obj);
			else
				return _obj.Equals(obj);
		}
		public override int GetHashCode()
		{
			return _obj.GetHashCode();
		}
		public override string ToString()
		{
			return _obj.ToString();
		}
	}
}
