using Loyc.Collections;
using Loyc.Collections.Impl;
using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Graphs
{
	/// <summary>A set of static methods for graphs, like <see cref="GraphMethods{Node, EdgeList, Edge, Cost}"/>
	/// but for functions that only require the simpler Node and Edge interfaces
	/// <see cref="IOutbound{Node}"/> and <see cref="ITo{Node}"/>.</summary>
	/// <typeparam name="Node">Node type</typeparam>
	/// <typeparam name="EdgeList">Edge list type (often <see cref="IEnumerable{Edge}"/>)</typeparam>
	/// <typeparam name="Edge">Edge type</typeparam>
	internal class GraphMethodsBase<Node, Edge, EdgeList>
		where Node : IOutbound<EdgeList>
		where Edge : ITo<Node>
		where EdgeList : IEnumerable<Edge>
	{
		/// <inheritdoc cref="TopoVisit{NodeList}(Node, NodeList, Dictionary{Node, bool}, Action{Node}?)"/>
		public static DList<Node> SortedTopologically(IEnumerable<Node> nodes, Action<Node>? onCycle = null)
			=> TopologicalSort(nodes, new DList<Node>(), onCycle);

		/// <summary>Converts a (normally acyclic) graph to a topologically sorted flat list.
		///   If there is an edge from A to B, and A is in the list of nodes, A will appear 
		///   before B in the output list. On other words, "leaf" nodes will come first.</summary>
		/// <param name="nodes">List of nodes to scan from.</param>
		/// <param name="results">A list in which to place results (must not be null).</param>
		/// <param name="onCycle">A method to call (instead of throwing an exception) if a 
		///   cycle is discovered. If this method doesn't throw, the nodes in the cycle will 
		///   appear in the output in some arbitrary but deterministic order.</param>
		/// <exception cref="ArgumentException">A cycle was discovered in the graph, and 
		///   onCycle is null.</exception>
		/// <returns>A topologically sorted list of nodes, which includes all nodes
		///   reachable via the Outbound property of the nodes provided. Thus the
		///   returned list can be longer, but not shorter, than the input list.</returns>
		public static NodeList TopologicalSort<NodeList>(IEnumerable<Node> nodes, NodeList results, Action<Node>? onCycle = null)
			where NodeList : IAdd<Node>
		{
			var seen = new Dictionary<Node, bool>();

			foreach (var node in nodes)
				TopoVisit(node, results, seen, onCycle);

			return results;
		}

		internal protected static void TopoVisit<NodeList>(Node node, NodeList results, Dictionary<Node, bool> seen, Action<Node>? onCycle = null)
			where NodeList : IAdd<Node>
		{
			if (seen.TryGetValue(node, out bool cycle))
			{
				if (cycle)
					(onCycle ?? throw new ArgumentException("TopologicalSort: cycle detected."))(node);
			}
			else
			{
				seen.Add(node, true);

				foreach (var edge in node.Outbound)
					TopoVisit(edge.To, results, seen, onCycle);

				seen[node] = false;
				results.Add(node);
			}
		}

		/// <summary>Finds all edges that are reachable (directly or indirectly via EdgesOut)
		///   starting at the specified node.</summary>
		/// <param name="node">The starting point.</param>
		/// <param name="edges">A set of edges. If this is null, a set will be allocated.</param>
		/// <param name="seen">A set of nodes that have already been encountered, 
		///   used as a performance optimization to avoid scanning nodes more than
		///   once. If this is null, a set will not be allocated, so memory usage will
		///   be lower but performance will typically also be lower.</param>
		/// <returns>The set of reachable edges</returns>
		public static HashSet<Edge> GetReachableEdges(Node node, HashSet<Node>? seen, HashSet<Edge>? edges = null)
		{
			edges ??= new HashSet<Edge>();
			if (seen == null || !seen.Contains(node))
				GetReachableEdgesCore(node, seen, edges);
			return edges;
		}
		protected internal static void GetReachableEdgesCore(Node node, HashSet<Node>? seen, HashSet<Edge> edges)
		{
			seen?.Add(node);
			foreach (var edge in node.Outbound) {
				if (edges.Add(edge) && (seen == null || !seen.Contains(edge.To)))
					GetReachableEdgesCore(edge.To, seen, edges);
			}
		}
	}

	/// <summary>A set of static methods for graphs.</summary>
	/// <typeparam name="Node">Node type</typeparam>
	/// <typeparam name="EdgeList">Edge list type (often <see cref="IEnumerable{Edge}"/>)</typeparam>
	/// <typeparam name="Edge">Edge type</typeparam>
	internal class GraphMethodsBase<Node, Edge, EdgeList, Cost> : GraphMethodsBase<Node, Edge, EdgeList>
		where Node : INode<EdgeList>
		where Edge : IEdge<Node, Cost>
		where EdgeList : IEnumerable<Edge>
	{
		/// <summary>Gathers a list of edges in a connected component, i.e. all edges that are 
		///   reachable directly or indirectly via EdgesOut and EdgesIn, starting at the 
		///   specified node. If <see cref="INode{EdgeList}.HasInbound"/> is false, this
		///   method behaves like <see cref="GetReachableEdges{Node, Edge, EdgeList}"/>.</summary>
		/// <param name="node">The starting point.</param>
		/// <param name="seen">A set of nodes that have already been encountered, 
		///   used as a performance optimization to avoid scanning nodes more than
		///   once. If this is null, a set will not be allocated, so memory usage will
		///   be lower but performance will typically also be lower.</param>
		/// <param name="edges">A set to which all discovered edges are added. If this is 
		///   null, a set will be allocated.</param>
		/// <returns>The set of reachable edges</returns>
		public static HashSet<Edge> ScanComponent(Node node, HashSet<Node>? seen, HashSet<Edge>? edges = null)
		{
			edges ??= new HashSet<Edge>();
			if (seen == null || !seen.Contains(node))
				ScanComponentCore(node, seen, edges);
			return edges;
		}
		static void ScanComponentCore(Node node, HashSet<Node>? seen, HashSet<Edge> edges)
		{
			GetReachableEdgesCore(node, seen, edges);
			if (node.HasInbound) {
				foreach (var edge in node.Inbound) {
					if (edges.Add(edge) && (seen == null || !seen.Contains(edge.From)))
						ScanComponentCore(edge.To, seen, edges);
				}
			}
		}
	}
}
