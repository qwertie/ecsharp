using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Graphs
{
	/// <summary>A node in a graph that only provides access to outbound edges.</summary>
	/// <remarks>When implementing this interface, consider also implementing 
	/// <see cref="IEquatable{Node}"/> for your type and overriding Equals and GetHashCode.</remarks>
	public interface IOutbound<out EdgeList>
	{
		/// <summary>Gets edges leaving this node.</summary>
		EdgeList Outbound { get; }
	}

	/// <summary>A node in a graph.</summary>
	public interface INode<out EdgeList> : IOutbound<EdgeList>
	{
		/// <summary>Gets edges whose value of <see cref="ITo{Node}.To"/> is this node,
		/// or default(EdgeList) if HasEdgesIn is false.</summary>
		EdgeList Inbound { get; }
		
		/// <summary>Returns true iff this node has access to its list of incoming 
		/// edges. Normally, this property is the same for all nodes of a given type.</summary>
		bool HasInbound { get; }
	}
}
