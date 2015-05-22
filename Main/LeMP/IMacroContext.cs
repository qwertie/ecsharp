using System;
using System.Collections.Generic;
using System.Linq;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax;

namespace LeMP
{
	public interface IMacroContext : IMessageSink
	{
		/// <summary>Returns the message sink, used for writing warnings and errors.</summary>
		/// <remarks>For backward compatibility, IMacroContext itself implements 
		/// IMessageSink, but if I were starting from scratch you'd have to write 
		/// output through this property.</remarks>
		IMessageSink Sink { get; }

		/// <summary>Returns a table of "properties" (arbitrary key-value pairs)
		/// that exist in the current scope. This dictionary is "persistent" in the
		/// computer science sense; any changes to these properties affect only the
		/// current scope and child scopes. When the current scope ends, the set of 
		/// properties that existed in the parent scope are restored.</summary>
		/// <remarks>
		/// Scopes are bounded by curly brace nodes (Call nodes named "{}").
		/// </remarks>
		IDictionary<object, object> ScopedProperties { get; }
		
		/// <summary>Returns a list of ancestors of the current node being 
		/// processed. Normally Ancestors[0] is a #splice node that contains a list 
		/// of all top-level statements in the file, and Parents.Last() is the
		/// current node.</summary>
		/// <remarks>You would expect that Ancestors[N] would contain Ancestors[N+1]
		/// as part of the attributes, target or arguments, but this is not always
		/// true. The ancestor list contains original versions of each node; when a
		/// child node is changed by a macro, the parent is not updated in this 
		/// list, but macro processing continues for the descendants of that child,
		/// so the ancestor list may occasionally seem incoherent.</remarks>
		IReadOnlyList<LNode> Ancestors { get; }

		/// <summary>Gets the logical parent of the current node, which is 
		/// <c>Ancestors[Ancestors.Count - 2]</c>, or null if the current node
		/// is the root node.</summary>
		/// <remarks>Please note that the current node may not actually exist in 
		/// the parent node due to changes made earlier to the current node by 
		/// other macros (or even the same macro); the Parent property still 
		/// returns the old version of the parent node.</remarks>
		LNode Parent { get; }

		/// <summary>Applies all available macros to the current node's children 
		/// and returns the result.</summary>
		/// <remarks>
		/// This method only processes children once. If this method is called 
		/// again for the same node, it returns a cached result.
		/// <para/>
		/// If the currently-running macro fails the result may be thrown away
		/// and the effort of processing the children will have been wasted. If
		/// the macro succeeds a its <see cref="SimpleMacro"/> attribute does not 
		/// include <c>Mode = MacroMode.NoReprocessing</c>, the children will 
		/// (normally) be processed again after the macro returns.
		/// </remarks>
		LNode PreProcessChildren();

		/// <summary>Gets a list of the remaining nodes (arguments/statements or 
		/// attributes) after this point in the code stream.</summary>
		/// <remarks>
		/// The list is null when processing a target node.
		/// <para/>
		/// For example, if your macro is called "macro" and it appears in the
		/// following context:
		/// <code>{
		///   a();
		///   macro(b());
		///   c();
		///   d();
		/// }</code>
		/// Then this list will contain two items, c() and d(). Similarly, if
		/// the context is <c>[a, b, macro(c), d, e]</c> then the list will contain
		/// the items d and e.
		/// </remarks>
		IListSource<LNode> RemainingNodes { get; }

		/// <summary>Returns true if the current node is in the attribute list of 
		/// its immediate parent.</summary>
		bool IsAttribute { get; }

		/// <summary>Returns true if the current node is the target of its parent
		/// call node.</summary>
		bool IsTarget { get; }

		/// <summary>Gets or sets a value that indicates whether to drop all 
		/// remaining node after the current one when the current macro returns.
		/// This property has no effect if the macro rejects the input by returning 
		/// null.</summary>
		/// <remarks>See remarks at <see cref="MacroContext.GetArgsAndBody"/>.</remarks>
		bool DropRemainingNodes { get; set; }
		// properties
		// fields
		// functions
		// events 
		// types
		//   namespaces
		//   classes
		//   structs
		//   delegates
		//   enums

		/// <summary>Runs the macro processor on the specified node(s).</summary>
		/// <param name="input">The node or node list to process.</param>
		/// <param name="asRoot">If false, the nodes are treated as children of the 
		/// current node (using the current list of ancestors as a basis), otherwise
		/// the nodes are processed alone as if they were a separate file.</param>
		/// <param name="clearOpenNamespaces">If false, the set of open namespaces
		/// stays the same; if true it is cleared to the set of pre-opened 
		/// namespaces (<see cref="MacroProcessor.PreOpenedNamespaces"/>).</param>
		/// <remarks>The node(s)</remarks>
		RVList<LNode> PreProcess(RVList<LNode> input, bool asRoot = false, bool resetOpenNamespaces = false, bool areAttributes = false);
		/// <inheritdoc cref="PreProcess(RVList{LNode}, bool, bool)"/>
		LNode PreProcess(LNode input, bool asRoot = false, bool resetOpenNamespaces = false, bool isTarget = false);
	}

	/// <summary>Standard extension methods for <see cref="IMacroContext"/>.</summary>
	public static class MacroContext
	{
		public static LNode CurrentNode(this IMacroContext ctx) { return ctx.Ancestors[ctx.Ancestors.Count-1]; }
		
		/// <summary>Splits the current node into a pair of "argument" and "body" 
		/// lists, potentially treating ctx.RemainingNodes as the "body" list.</summary>
		/// <param name="ctx">Context of the current macro.</param>
		/// <param name="orRemainingNodes">Whether to use ctx.RemainingNodes as
		/// the second list if there is no {braces node} at the end of 
		/// ctx.CurrentNode().Args.</param>
		/// <returns>A pair where the first item is "arguments" and the second is 
		/// the "body". If no body was detected then the second list is empty and
		/// the first list is simply ctx.CurrentNode().Args.</returns>
		/// <remarks>
		/// EC# supports a syntax specially designed for macro calls: 
		/// <code>macroName(args) { stmts; }</code>
		/// This is stored as a call node with a body, in braces, as its final parameter,
		/// i.e. it is equivalent to
		/// <code>macroName(args, { stmts; });</code>
		/// A similar, but more general feature called "superexpressions" exists in LES.
		/// <para/>
		/// Some macros would additionally like to apply themselves to all remaining
		/// nodes in the current list of statements or expressions, i.e.
		/// <code>macroName(args); stmts;</code>
		/// LeMP supports this through the <see cref="IMacroContext.DropRemainingNodes"/>
		/// and <see cref="IMacroContext.RemainingNodes"/> APIs. If your macro wants
		/// to apply itself to all remaining statements or expressions in the 
		/// current sequence of nodes, it can set the DropRemainingNodes property 
		/// to true and then simply incorporate RemainingNodes into its own output
		/// (if you need to return multiple statements from your macro, use 
		/// <c>list.AsLNode(CodeSymbols.Splice)</c> to convert a RVList{LNode} to an 
		/// LNode.)
		/// <para/>
		/// This extension method helps you by detecting whether the current node
		/// has a body in braces or not. If the braces are present, the returned
		/// pair consists of the args shortened by one (i.e.
		/// <c>ctx.CurrentNode().Args.WithoutLast(1)</c>) and the Args of the "{}"
		/// braces node. Otherwise, <c>ctx.CurrentNode().Args</c> is the first item
		/// in the pair.
		/// <para/>
		/// In the latter case, if <c>orRemainingNodes</c> then this method sets
		/// <c>ctx.DropRemainingNodes</c> to true and uses <c>ctx.RemainingNodes</c>
		/// as the second list. Otherwise the second list is left blank.
		/// </remarks>
		public static Pair<RVList<LNode>, RVList<LNode>> GetArgsAndBody(this IMacroContext ctx, bool orRemainingNodes)
		{
			var node = ctx.CurrentNode();
			var args = node.Args;
			LNode last = null;
			RVList<LNode> body = new RVList<LNode>();
			if (node.ArgCount != 0 && (last = args.Last).Calls(CodeSymbols.Braces)) {
				body = last.Args;
				args = args.WithoutLast(1);
			} else if (orRemainingNodes) {
				body = new RVList<LNode>(ctx.RemainingNodes);
				ctx.DropRemainingNodes = true;
			}
			return Pair.Create(args, body);
		}
	}
}
