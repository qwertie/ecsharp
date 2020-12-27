using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc;
using Loyc.Syntax;
using Loyc.Collections.MutableListExtensionMethods;
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
		static LNodeFactory F = new LNodeFactory(EmptySourceFile.Synthetic);

		[LexicalMacro("noMacro(Code)", "Alias for `#noLexicalMacros`. Passes code through to the output language, without macro processing.",
			Mode = MacroMode.NoReprocessing)]
		public static LNode noMacro(LNode node, IMacroContext sink)
		{
			if (!node.IsCall)
				return null;
			return node.Args.AsLNode(S.Splice);
		}

		static readonly Symbol _hash_set = (Symbol)"#set";
		static readonly Symbol _hash_snippet = (Symbol)"#snippet";
		static readonly Symbol _hash_setScopedProperty = (Symbol)"#setScopedProperty";
		static readonly Symbol _hash_setScopedPropertyQuote = (Symbol)"#setScopedPropertyQuote";

		[LexicalMacro("#set Identifier = literal; #snippet Identifier = { statements; }; #snippet Identifier = expression;",
			"Sets an option, or saves a snippet of code for use later. The children are macro-expanded before the assignment occurs. See also: #get", 
			"#var", Mode = MacroMode.Passive)]
		public static LNode _set(LNode node, IMacroContext context)
		{
			var lhs = node.Args[0, LNode.Missing];
			var name = lhs.Name;
			bool isSnippet = name == _hash_snippet;
			if ((isSnippet || name == _hash_set) && node.ArgCount == 2 && lhs.IsId)
			{
				Symbol newTarget = isSnippet ? _hash_setScopedPropertyQuote : _hash_setScopedProperty;
				var stmts = node.Args.Slice(1).Select(key =>
					{
						LNode value = F.@true;
						if (key.Calls(S.Assign, 2))
						{
							value = key.Args[1];
							value = context.PreProcess(value);
							key = key.Args[0];
							if (isSnippet && value.Calls(S.Braces))
								value = value.Args.AsLNode(S.Splice);
						}

						if (!key.IsId)
							context.Write(Severity.Error, key, "Invalid key; expected an identifier.");
						return (LNode)node.With(newTarget, LNode.Literal(key.Name, key), value);
					});
				return F.Call(S.Splice, stmts);
			}
			return null;
		}

		[LexicalMacro("#get(key, defaultValueOpt)", 
			"Alias for #getScopedProperty. Gets a literal or code snippet that was previously set in this scope. "
			+"If the key is an identifier, it is treated as a symbol instead, e.g. `#get(Foo)` is equivalent to `#get(@@Foo)`.", 
			"#get")]
		public static LNode _get(LNode node, IMacroContext context)
		{
			if (node.ArgCount.IsInRange(1, 2))
				return MacroProcessorTask.getScopedProperty(node, context);
			return null;
		}

		[LexicalMacro("#statement { ... } // EC#/LES2 syntax",
			"This macro simply replaces itself with the contents of the braced block, " +
			"e.g. `#statements { x = 1; }` becomes `x = 1`. This allows statements to be " +
			"written in an expression context. If there are multiple statements, they " +
			"are enclosed in a #splice() node, e.g. `#statements { x = 1; y = 2; }` " +
			"becomes `#splice(x = 1, y = 2)`.", "#statement")]
		public static LNode _statement(LNode node, IMacroContext context)
		{
			LNode braces;
			if (node.ArgCount != 1 || !(braces = node[0]).Calls(S.Braces))
				return null;
			return braces.ArgCount == 1 ? braces[0] : braces.WithName(S.Splice);
		}

		static readonly Symbol _macros = GSymbol.Get("macros");
		static readonly Symbol _importMacros = GSymbol.Get("#importMacros");

		[LexicalMacro("import_macros(Namespace);",
			"Use macros from specified namespace. The 'macros' modifier imports macros only, deleting this statement from the output.")]
		public static LNode import_macros(LNode node, IMacroContext sink)
		{
			return node.With(_importMacros, node.Args);
		}

		[LexicalMacro("#printKnownMacros;", "Prints a table of all macros known to LeMP, as (invalid) C# code.",
			"printKnownMacros", "#printKnownMacros", Mode = MacroMode.NoReprocessing | MacroMode.MatchIdentifierOrCall)]
		public static LNode printKnownMacros(LNode node, IMacroContext context)
		{
			// namespace LeMP {
			//     /* documentation */
			//     macroName;
			//     ...
			// }
			return F.Call(S.Splice, context.AllKnownMacros.SelectMany(p => p.Value)
				.GroupBy(mi => mi.Namespace).OrderBy(g => g.Key).Select(group => {
					var descriptions = group.OrderBy(mi => mi.Macro.Method.Name).Select(GetDescriptionOfMacro);
					if ((group.Key ?? GSymbol.Empty).Name != "")
						return F.Attr(F.Trivia(S.TriviaSLComment, " printKnownMacros output:"),
							F.Call(S.Namespace, NamespaceSymbolToLNode(group.Key ?? GSymbol.Empty),
								LNode.Missing, F.Braces(descriptions)));
					else
						return F.Attr(F.Trivia(S.TriviaSLComment, " printKnownMacros output: global namespace"),
							F.Braces(descriptions));
					}));

			LNode GetDescriptionOfMacro(MacroInfo mi)
			{
				StringBuilder descr = new StringBuilder();
				descr.Append("\n### ").Append(mi.Names.FirstOrDefault("<no name>")).Append(" ###\n");
				if (!string.IsNullOrEmpty(mi.Syntax))
					descr.Append("\n\t").Append(mi.Syntax).Append("\n");
				if (!string.IsNullOrEmpty(mi.Description)) {
					var description = G.WordWrap(mi.Description, 80).Select(L => L.TrimEnd('\n')).Join("\n");
					descr.Append("\n").Append(description).Append("\n");
				}
				descr.Replace("\n", "\n\t\t");
				descr.Append("\t");
				LNode line = mi.Names.Length == 1
					? (LNode)LNode.Id(mi.Names[0])
					: LNode.Call(S.Tuple, LNode.List(mi.Names.Select(name => (LNode)LNode.Id(name))));

				string methodName = mi.Macro.Method.Name, @class = mi.Macro.Method.DeclaringType.Name;
				string postComment = " " + @class + "." + methodName;
				if (mi.Mode != MacroMode.Normal)
					postComment += string.Format(" (Mode = {0})", mi.Mode);
				return F.Attr(
					F.Trivia(S.TriviaMLComment, descr.ToString()),
					F.TriviaNewline,
					line).PlusTrailingTrivia(F.Trivia(S.TriviaSLComment, postComment));
			}
		}
		internal static LNode NamespaceSymbolToLNode(Symbol ns)
		{
			var parts = ns.Name.Split('.');
			return parts.Length == 1 ? F.Id(parts[0]) : F.Dot(parts);
		}
	}
}
