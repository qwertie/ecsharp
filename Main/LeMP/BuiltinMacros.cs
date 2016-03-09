using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc;
using Loyc.Syntax;
using Loyc.Collections;
using Loyc.Math;
using Loyc.Utilities;
using S = Loyc.Syntax.CodeSymbols;

/// <summary>Defines prelude macros, which are predefined macros that normally 
/// do not have to be explicitly imported before use (in LES or EC#).</summary>
namespace LeMP.Prelude
{
	/// <summary>Defines <c>noMacro(...)</c> for suppressing macro expansion and 
	/// <c>import macros your.namespace.name</c> as an alias for 
	/// <c>#importMacros(your.namespace.name)</c>.
	/// </summary>
	[ContainsMacros]
	public static partial class BuiltinMacros
	{
		static LNodeFactory F = new LNodeFactory(EmptySourceFile.Default);

		[LexicalMacro("noMacro(Code)", "Pass code through to the output language, without macro processing.",
			Mode = MacroMode.NoReprocessing)]
		public static LNode noMacro(LNode node, IMessageSink sink)
		{
			if (!node.IsCall)
				return null;
			return node.WithTarget(S.Splice);
		}

		static readonly Symbol _macros = GSymbol.Get("macros");
		static readonly Symbol _importMacros = GSymbol.Get("#importMacros");

		[LexicalMacro("import_macros Namespace",
			"Use macros from specified namespace. The 'macros' modifier imports macros only, deleting this statement from the output.")]
		public static LNode import_macros(LNode node, IMessageSink sink)
		{
			return node.With(_importMacros, node.Args);
		}

		[LexicalMacro("#printKnownMacros;", "Prints a table of all macros known to LeMP, as (invalid) C# code.",
			"#printKnownMacros", Mode = MacroMode.NoReprocessing)]
		public static LNode printKnownMacros(LNode node, IMacroContext context)
		{
			// namespace LeMP {
			//     /* documentation */
			//     #fn("Type Name(set Type name) {...}; Type Name(public Type name) {...}");
			//     ...
			// }
			return F.Call(S.Splice, context.AllKnownMacros.SelectMany(p => p.Value)
				.GroupBy(mi => mi.NamespaceSym).OrderBy(g => g.Key).Select(group =>
					F.Attr(F.Trivia(S.TriviaSLCommentBefore, "#printKnownMacros"),
					F.Call(S.Namespace, NamespaceSymbolToLNode(group.Key ?? GSymbol.Empty), LNode.Missing,
						F.Braces(group.Select(mi =>
						{
							LNode line = F.Call(mi.Name, F.Literal(mi.Info.Syntax).SetStyle(NodeStyle.Alternate)).SetBaseStyle(NodeStyle.PrefixNotation);
							if (string.IsNullOrEmpty(mi.Info.Description))
								return line;
							else
								return F.Attr(F.Trivia(S.TriviaMLCommentBefore, mi.Info.Description),
									F.Trivia(S.TriviaSpaceBefore, "\n"), line);
						}))))));
		}
		internal static LNode NamespaceSymbolToLNode(Symbol ns)
		{
			var parts = ns.Name.Split('.');
			return parts.Length == 1 ? F.Id(parts[0]) : F.Dot(parts);
		}
	}
}
