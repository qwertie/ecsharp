using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using Loyc.Runtime;

namespace Loyc.Runtime
{
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
		ValueT GetExtra(Symbol key);     // Returns null if key is null
		ValueT GetExtra(string key);     // Returns null if key is null
		void SetExtra(Symbol key, ValueT val); // Exception if key is null
		void SetExtra(string key, ValueT val); // Exception if key is null
		bool RemoveExtra(Symbol key); // Returns true if attribute existed
		bool RemoveExtra(string key); // Returns true if attribute existed
		bool HasExtra(Symbol key);    // Returns false if key is null
		bool HasExtra(string key);    // Returns false if key is null
		IEnumerator<KeyValuePair<Symbol, ValueT>> ExtraEnumerator();
	}
}
