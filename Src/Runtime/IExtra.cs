using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using Loyc.Runtime;

namespace Loyc.Runtime
{
	public interface IExtra<T>
	{
		/// <summary>Returns a dictionary that can be used to store additional state
		/// beyond the standard content of the object.
		/// </summary><remarks>
		/// Extra is never null or read-only.
		/// 
		/// Extra should normally hold transient information, not information that 
		/// is part of the node's syntax. However, the node can also use this 
		/// dictionary to hold the normal properties of the node, such as Block, 
		/// Attrs, BriefText, etc. In this case, the symbol name should begin with 
		/// an underscore to differentiate it from transient state, e.g. :_Block, 
		/// :_Attrs, :_BriefText. This is the recommended way to store properties
		/// that are normally null. For example, a node designed to store expressions
		/// normally has a list of Params but not Attrs or Block. So rather than 
		/// having two references that are usually null, They should be placed in
		/// the Extra dictionary only on request.
		/// 
		/// By using ExtraAttributes(of object) as a node's base class, so that
		/// Extra==this, overhead is reduced because a separate dictionary object is
		/// not needed for every node.
		/// </remarks>
		IDictionary<Symbol, T> Extra { get; }
	}

#if false
	/// <summary>
	/// Interface for classes that allow "state variables" to be attached to them.
	/// </summary><remarks>
	/// This interface allows arbitrary data to be attached to an object, using Symbols
	/// as unique keys. Duplicate keys are not allowed, and the data must inherit from 
	/// ValueT. It's a good idea for attribute classes to implement 
	/// IDictionary<Symbol, ValueT> also.
	/// </remarks>
	public interface IExtraAttributes<ValueT>
	{
		ValueT GetTag(Symbol key);     // Returns null if key is null
		ValueT GetTag(string key);     // Returns null if key is null
		void SetTag(Symbol key, ValueT val); // Exception if key is null
		void SetTag(string key, ValueT val); // Exception if key is null
		bool RemoveTag(Symbol key); // Returns true if attribute existed
		bool RemoveTag(string key); // Returns true if attribute existed
		bool HasTag(Symbol key);    // Returns false if key is null
		bool HasTag(string key);    // Returns false if key is null
		IEnumerator<KeyValuePair<Symbol, ValueT>> TagEnumerator();
	}
#endif
}
