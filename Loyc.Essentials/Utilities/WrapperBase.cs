using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc
{
	/// <summary>Abstract class that helps you implement wrappers by automatically
	/// forwarding calls to Equals(), GetHashCode() and ToString().</summary>
	[Serializable]
	public abstract class WrapperBase<T>
	{
		protected T _obj;
		protected WrapperBase(T wrappedObject)
		{
			_obj = wrappedObject; // possibly null
		}

		protected static readonly EqualityComparer<T> TComp = EqualityComparer<T>.Default;

		/// <summary>Returns true iff the parameter 'obj' is a wrapper around the same object that this object wraps.</summary>
		/// <param name="obj">The object to compare with the current object.</param>
		/// <remarks>If obj actually refers to the wrapped object, this method returns false to preserve commutativity of the "Equals" relation.</remarks>
		public override bool Equals(object obj)
		{
			var w = obj as WrapperBase<T>;
			return w != null && TComp.Equals(_obj, w._obj);
		}
		/// <summary>Returns the hashcode of the wrapped object.</summary>
		public override int GetHashCode()
		{
			return TComp.GetHashCode(_obj);
		}
		/// <summary>Returns ToString() of the wrapped object.</summary>
		public override string ToString()
		{
			return _obj.ToString();
		}
	}
}
