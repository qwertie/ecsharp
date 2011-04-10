using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using Loyc.Essentials;

namespace Loyc.Essentials
{
	public interface ITags<T>
	{
		/// <summary>Returns a dictionary that can be used to store additional state
		/// beyond the standard content of the object.
		/// </summary><remarks>
		/// Tags is never null or read-only.
		/// <para/>
		/// Tags of AstNodes should normally hold transient or derived information,
		/// not information that is part of the node's syntax.
		/// <para/>
		/// Is is possible that Tags==this to reduce overhead.
		/// </remarks>
		IDictionary<Symbol, T> Tags { get; }
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
