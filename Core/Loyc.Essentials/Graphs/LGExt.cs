using Loyc.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Loyc.Graphs
{
	public static class LGExt
	{
		// Special overload (C# cannot infer the type arguments of the standard overload)
		/// <inheritdoc cref="GraphMethodsBase{Node,Edge,EdgeList,Cost}.ScanComponent(Node, HashSet{Node}?, HashSet{Edge}?)"/>
		public static HashSet<Edge> ScanComponent<Node, Edge>(this Node node, HashSet<Edge>? edges = null)
			where Node : INode<IEnumerable<Edge>>
			where Edge : IEdge<Node>
			=> GraphMethodsBase<Node, Edge, IEnumerable<Edge>, float>.ScanComponent(node, new HashSet<Node>(), edges);

		/// <inheritdoc cref="GraphMethodsBase{Node,Edge,EdgeList,Cost}.ScanComponent(Node, HashSet{Node}?, HashSet{Edge}?)"/>
		public static HashSet<Edge> ScanComponent<Node, Edge, EdgeList, Cost>(this Node node, HashSet<Node>? seen, HashSet<Edge>? edges = null)
			where Node : INode<EdgeList>
			where Edge : IEdge<Node, Cost>
			where EdgeList : IEnumerable<Edge>
			=> GraphMethodsBase<Node, Edge, EdgeList, Cost>.ScanComponent(node, seen, edges);

		// Special overload (C# cannot infer the type arguments of the standard overload)
		/// <inheritdoc cref="GraphMethodsBase{Node,Edge,EdgeList}.GetReachableEdges(Node, HashSet{Node}?, HashSet{Edge}?)"/>
		public static HashSet<Edge> GetReachableEdges<Node, Edge>(this Node node, HashSet<Edge>? edges = null)
			where Node : IOutbound<IEnumerable<Edge>>
			where Edge : ITo<Node>
			=> GraphMethodsBase<Node, Edge, IEnumerable<Edge>>.GetReachableEdges(node, new HashSet<Node>(), edges);

		/// <inheritdoc cref="GraphMethodsBase{Node,Edge,EdgeList}.GetReachableEdges(Node, HashSet{Node}?, HashSet{Edge}?)"/>
		public static HashSet<Edge> GetReachableEdges<Node, Edge, EdgeList>(this Node node, HashSet<Node>? seen, HashSet<Edge>? edges = null)
			where Node : IOutbound<EdgeList>
			where Edge : ITo<Node>
			where EdgeList : IEnumerable<Edge>
			=> GraphMethodsBase<Node, Edge, EdgeList>.GetReachableEdges(node, seen, edges);

		// Special overload (C# cannot infer the type arguments of the standard overloads)
		/// <inheritdoc cref="GraphMethodsBase{Node,Edge,EdgeList}.SortedTopologically(IEnumerable{Node}, Action{Node}?)"/>
		public static DList<Node> SortedTopologically<Node, Edge, EdgeList, Cost>(this IGraph<Node, Edge, EdgeList, Cost> graph, Action<Node>? onCycle = null)
			where Node : INode<EdgeList>
			where Edge : IEdgeTo<Node, Cost>
			where EdgeList : IEnumerable<Edge>
			=> GraphMethodsBase<Node, Edge, EdgeList>.SortedTopologically(graph.Nodes, onCycle);

		/// <inheritdoc cref="GraphMethodsBase{Node,Edge,EdgeList}.SortedTopologically(IEnumerable{Node}, Action{Node}?)"/>
		public static DList<Node> SortedTopologically<Node, Edge, EdgeList>(this IEnumerable<Node> nodes, Action<Node>? onCycle = null)
			where Node : IOutbound<EdgeList>
			where Edge : ITo<Node>
			where EdgeList : IEnumerable<Edge>
			=> GraphMethodsBase<Node, Edge, EdgeList>.SortedTopologically(nodes, onCycle);
	}
}
