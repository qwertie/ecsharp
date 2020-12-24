using System;
using System.Collections.Generic;
using System.Linq;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax;

namespace LeMP
{
	/// <summary>This interface provides services offered by the lexical macro processor (LeMP).</summary>
	/// <remarks>Macros receive this as their second argument (see <see cref="LexicalMacro"/>)</remarks>
	public interface IMacroContext : IMessageSink
	{
		/// <summary>Returns the message sink, used for writing warnings and errors.</summary>
		/// <remarks>For backward compatibility, IMacroContext itself implements 
		/// IMessageSink also.</remarks>
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
		/// of all top-level statements in the file, and Ancestors.Last() is the
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
		/// If the currently-running macro fails, the result may be thrown away
		/// and the effort of processing the children will have been wasted. If
		/// the macro succeeds, and its <see cref="LexicalMacroAttribute"/> uses
		/// the default <c>MacroMode.Normal</c> processing mode, the children 
		/// will (normally) be processed again after the macro returns.
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

		/// <summary>Runs the macro processor on the specified node(s).</summary>
		/// <param name="input">The node or node list to process.</param>
		/// <param name="asRoot">If false, the nodes are treated as children of the 
		///   current node (using the current list of ancestors as a basis); if true,
		///   the list of parent nodes is cleared.</param>
		/// <param name="resetOpenNamespaces">If false, the set of open namespaces
		///   stays the same; if true it is cleared to the set of pre-opened 
		///   namespaces (<see cref="MacroProcessor.PreOpenedNamespaces"/>) and
		///   macros defined with <see cref="RegisterMacro"/> are forgotten.</param>
		/// <param name="resetProperties">If true, <see cref="ScopedProperties"/>
		///   is reset to contain only predefined properties.</param>
		/// <param name="areAttributes">This parameter controls the value of 
		///   <see cref="IsAttribute"/> while the items in the list are processed.</param>
		/// <remarks>The node(s)</remarks>
		LNodeList PreProcess(LNodeList input, bool asRoot = false, bool resetOpenNamespaces = false, bool resetProperties = false, bool areAttributes = false);
		/// <inheritdoc cref="PreProcess(LNodeList, bool, bool, bool, bool)"/>
		LNode PreProcess(LNode input, bool asRoot = false, bool resetOpenNamespaces = false, bool resetProperties = false, bool isTarget = false);

		/// <summary>Gets information about all macros registered with the macro 
		/// processor, including macros whose namespace has not been opened with
		/// <c>#importMacros</c>.</summary>
		IReadOnlyDictionary<Symbol, VList<MacroInfo>> AllKnownMacros { get; }

		/// <summary>Gets the next number to use as a suffix for temporary variables (without incrementing it).</summary>
		int NextTempCounter { get; }
		/// <summary>Gets the next number to use as a suffix for temporary variables, then increments it.</summary>
		int IncrementTempCounter();

		/// <summary>Registers a new macro in the current scope.</summary>
		/// <param name="macroInfo">Information about the macro</param>
		/// <remarks>
		/// The macro will be forgotten at the end of the current scope (regardless 
		/// of which namespace it is assigned to).
		/// <para/>
		/// Macros defined in child scopes do not shadow macros in outer scopes; 
		/// if there are macros with the same name in the outer scopes, conflicts are 
		/// handled in the same way as with groups of macros that are imported in the 
		/// same scope. For example, <c>Mode = <see cref="MacroMode.PriorityOverride"/></c>
		/// can be used to make macros that override normal-priority macros.</remarks>
		void RegisterMacro(MacroInfo macroInfo);

		/// <summary>Gets the list of namespaces that are being searched for macros in 
		/// the current scope. LeMP allows you to modify this collection.</summary>
		/// <remarks>The naming scheme of namespaces is not standardized in cases where
		/// the namespace name is not a "normal" identifier. It is recommended that 
		/// macros be placed in namespaces whose names are valid identifiers in most 
		/// languages (C++, C#, Java, Python, etc.). It's okay to put macros in nested
		/// namespaces; the Symbol.Name will have a format like "Namespace1.Namespace2".
		/// </remarks>
		ICollection<Symbol> OpenMacroNamespaces { get; }
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
		/// <c>list.AsLNode(CodeSymbols.Splice)</c> to convert a VList{LNode} to an 
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
		public static Pair<LNodeList, LNodeList> GetArgsAndBody(this IMacroContext ctx, bool orRemainingNodes)
		{
			var node = ctx.CurrentNode();
			var args = node.Args;
			LNode last = null;
			var body = LNode.List();
			if (node.ArgCount != 0 && (last = args.Last).Calls(CodeSymbols.Braces)) {
				body = last.Args;
				args = args.WithoutLast(1);
			} else if (orRemainingNodes && ctx.RemainingNodes != null) {
				body = LNode.List(ctx.RemainingNodes);
				ctx.DropRemainingNodes = true;
			} 
			return Pair.Create(args, body);
		}

		/// <summary>Transforms an option list in the format <c>option1(v1), option2(v2)</c> 
		/// or <c>option1: v1, option2: v2</c> into a sequence of (key, value) pairs.
		/// If the format of a given node is invalid, this function yields <c>(node, null)</c>.</summary>
		/// <remarks>
		/// <c>option1: v1, option2: v2</c> is parsed into <c>@`'::=`(option1, v1), 
		/// @`'::=`(option2, v2)</c> in EC# or <c>@`':`(option1, v1), @`':`(option2, v2)</c> in LES.
		/// </remarks>
		public static IEnumerable<KeyValuePair<LNode, LNode>> GetOptions(LNodeList optionList)
		{
			Symbol lesNamedArg = (Symbol)"'<:";
			foreach (var option in optionList) {
				if ((option.ArgCount == 2 && option.Name.IsOneOf(CodeSymbols.NamedArg, CodeSymbols.Colon, lesNamedArg)) && option.Args[0].IsId)
				{
					LNode key = option.Args[0];
					LNode value = option.Args.Last;
					yield return new KeyValuePair<LNode, LNode>(key, value);
				}
				else if (option.Args.Count == 1 && option.Target.IsId)
					yield return new KeyValuePair<LNode, LNode>(option.Target, option.Args[0]);
				else
					yield return new KeyValuePair<LNode, LNode>(option, null);
			}
		}
	}
}
