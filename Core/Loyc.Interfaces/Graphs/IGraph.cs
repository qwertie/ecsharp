using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Graphs
{
	/// <summary>A directed graph, consisting of nodes and edges.</summary>
	/// <typeparam name="Node">Node type, e.g. types or modules in a program, intersections, or locations</typeparam>
	/// <typeparam name="Edge">Edge type, e.g. dependencies in a program, roadways, pipes or lines</typeparam>
	/// <typeparam name="Cost">Edge weight type, used to represent how expensive or 
	///   time-consuming it is to use or travel along an edge</typeparam>
	public interface IGraph<out Node, out Edge, out EdgeList, out Cost>
		where Node : INode<EdgeList>
		where Edge : IEdgeTo<Node, Cost>
		where EdgeList : IEnumerable<Edge>
	{
		IEnumerable<Node> Nodes { get; }
		
		#if NETSTANDARD2_0 || NET45 || NET451 || NET452 || NET46 || NET461 || NET462 || NET47 || NET471 || NET472
		IEnumerable<Edge> Edges { get; }
		#else
		IEnumerable<Edge> Edges => LGInterfaces.GetOutboundDirectEdges<Node, Edge, EdgeList>(Nodes);
		#endif
	}

	/// <summary>A directed graph, consisting of nodes and edges (using the default cost type, float).</summary>
	public interface IGraph<out Node, out Edge, out EdgeList> : IGraph<Node, Edge, EdgeList, float>
		where Node : INode<EdgeList>
		where Edge : IEdgeTo<Node, float>
		where EdgeList : IEnumerable<Edge> { }

	/// <summary>A directed graph, consisting of nodes and edges (using the default edge list type <see cref="IEnumerable{Edge}"/>).</summary>
	public interface IGraph<out Node, out Edge> : IGraph<Node, Edge, IEnumerable<Edge>, float>
		where Node : INode<IEnumerable<Edge>>
		where Edge : IEdgeTo<Node, float> { }

	/// <summary>A directed graph, consisting of nodes and edges (using the default edge type <see cref="IEdgeTo{Node}"/>).</summary>
	public interface IGraph<out Node> : IGraph<Node, IEdgeTo<Node>, IEnumerable<IEdgeTo<Node>>, float>
		where Node : INode<IEnumerable<IEdgeTo<Node>>> { }

	/// <summary>Core functionaliity for graphs</summary>
	public static class LGInterfaces
	{
		public static IEnumerable<Edge> GetOutboundDirectEdges<Node, Edge, EdgeList>(this IEnumerable<Node> nodes)
			where Node : IOutbound<EdgeList>
			where EdgeList : IEnumerable<Edge>
		{
			var edges = new HashSet<Edge>();

			foreach (var node in nodes) {
				foreach (var edge in node.Outbound)
					edges.Add(edge);
			}

			return edges;
		}
	}
}
