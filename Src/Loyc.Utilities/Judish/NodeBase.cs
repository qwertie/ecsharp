using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Utilities.Judish.Internal;

namespace Loyc.Utilities.Judish
{
	/// <summary>Base class of all node types.</summary>
	/// <remarks>A Judish collection cannot contain an empty node.</remarks>
	internal abstract class NodeBase
	{
		public static object NotFound = new object();

		/// <summary>Retrieves a value from a Judish collection.</summary>
		/// <param name="q">Query state (includes all or part of the key)</param>
		/// <param name="value">Value that was associated with the key</param>
		/// <returns>Returns the value associated with the key, or NotFound 
		/// if the key does not exist.</returns>
		/// <remarks>The Get() method should not use and need not update q.Node</remarks>
		public abstract object Get(ref QueryState q);

		/// <summary>Creates or replaces a key-value pair in a Judish collection.</summary>
		/// <param name="q">Query state (includes all or part of the key)</param>
		/// <param name="value">Value to assign to the key.</param>
		/// <param name="thisNode">Sometimes when setting a value, a node must be 
		/// converted to a different type. Before returning, Set() must always set 
		/// thisNode to point to the current node representation. thisNode should 
		/// really have type NodeBase, but it is sometimes more convenient to use
		/// an output variable of type object, and C#, stupidly, doesn't support 
		/// contravariant output parameters.</param>
		/// <returns>Returns the previous value of the key, or NotFound if it
		/// did not exist before the call.</returns>
		/// <remarks>
		/// The caller must set q.Parent to the parent node before calling 
		/// Set().
		/// </remarks>
		public abstract object Set(ref QueryState q, object value, out object thisNode);

		/// <summary>Removes a key from a Judish collection.</summary>
		/// <param name="q">Query state (includes all or part of the key)</param>
		/// <param name="thisNode">Sometimes when removing a value, a node must be 
		/// converted to a different type. Before returning, Set() sets thisNode to
		/// point to the current node representation. If the node is entirely empty,
		/// it should set thisNode to NotFound, and the caller MUST check for 
		/// this special case. If the node is empty but for a zero-length key, 
		/// thisNode must be set to the value of the key.</param>
		/// <returns>Returns the value formerly associated with the key, or 
		/// NotFound if the key did not exist.</returns>
		/// <remarks>
		/// The caller must set q.Parent to the parent node before calling 
		/// Remove().
		/// </remarks>
		public abstract object Remove(ref QueryState q, out object thisNode);

		public abstract object MoveFirst(out JKeyPart path);
		public abstract object MoveLast(out JKeyPart path);
		public abstract object MoveNext(ref JKeyPart path);
		public abstract object MovePrev(ref JKeyPart path);

		public abstract int LocalCount { get; }

		/// <summary>Makes a deep copy of the node and its children.</summary>
		public abstract NodeBase Clone();

		public abstract void CheckValidity(bool recursive);
	}
}
