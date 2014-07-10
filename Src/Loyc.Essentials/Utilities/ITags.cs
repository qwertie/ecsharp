using System;
using System.Collections.Generic;
using System.Diagnostics;
using Loyc.MiniTest;

namespace Loyc
{
	/// <summary>Interface for an object that can have "tags" attached, which are 
	/// arbitrary objects reached through a key <see cref="Symbol"/>.</summary>
	public interface ITags<T>
	{
		/// <summary>Returns a dictionary that can be used to store additional state
		/// beyond the standard content of the object.
		/// </summary><remarks>
		/// Is is possible that Tags==this to reduce overhead.
		/// </remarks>
		IDictionary<Symbol, T> Tags { get; }
	}
}
