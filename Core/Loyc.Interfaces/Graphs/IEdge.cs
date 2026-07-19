using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Graphs
{
	/// <summary>Something with a To property, usually a directed edge in a graph.</summary>
	public interface ITo<out Node>
	{
		/// <summary>The destination point of an edge in graph.</summary>
		Node To { get; }
	}

	/// <summary>A directed edge in a graph (without the From property of <see cref="IEdge{Node}"/>).</summary>
	public interface IEdgeTo<out Node, out TCost> : ITo<Node>
	{
		/// <summary>The weight of this edge.</summary>
		TCost Cost { get; }
	}

	/// <summary>A directed edge in a graph (without the From property of <see cref="IEdge{Node}"/>).</summary>
	public interface IEdgeTo<out Node> : IEdgeTo<Node, float> { }

	/// <summary>A directed edge in a graph.</summary>
	public interface IEdge<out Node, out TCost> : IEdgeTo<Node, TCost>
	{
		/// <summary>The starting point of an edge in a graph.</summary>
		Node From { get; }
	}

	/// <summary>A directed edge in a graph.</summary>
	public interface IEdge<out Node> : IEdge<Node, float>, IEdgeTo<Node> { }
}
