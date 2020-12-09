using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Syntax;
using Loyc.Collections;
using S = Loyc.Ecs.EcsCodeSymbols;
using Loyc.Ecs;

namespace LeMP
{
	public partial class StandardMacros
	{
		// BTW: THERE ARE NO UNIT TESTS FOR THIS because our unit testing setup 
		// is not designed to handle preprocessor directives or raw text. 

		[LexicalMacro("#lines;",
			"C# specific: adds a #line directive in front of many statements so error messages are mapped back to the source file. "
			+"This macro doesn't know exactly how the output will be printed, so it tends to emit more #line directives than necessary "
			+"to increase the chances of a correct mapping.",
			"#lines", Mode = MacroMode.MatchIdentifierOrCall)]
		public static LNode AddCsLineDirectives(LNode node, IMacroContext context)
		{
			if (node.ArgCount != 0)
				return null;
			int sourceLine = -1;
			var list0 = new LNodeList(context.RemainingNodes);
			var list1 = context.PreProcess(list0);
			var list2 = AddLineDirectives(list1, true, ref sourceLine);
			context.DropRemainingNodes = true;
			return F.Call(S.Splice, list2);
		}

		static LNodeList AddLineDirectives(LNodeList nodes, bool stmtContext, ref int sourceLine_)
		{
			int sourceLine = sourceLine_;
			nodes = nodes.SmartSelect(node => {
				if (stmtContext && sourceLine > 0 && node.AttrNamed(S.TriviaAppendStatement) == null)
					sourceLine++; // printer will print a newline by default

				int explicitNewlines = node.Attrs.Count(n => n.IsIdNamed(S.TriviaNewline));
				sourceLine += explicitNewlines;

				// Generate line directive if necessary; to avoid excess 
				// clutter, don't consider emit #line directives within an expression.
				string lineDirective = null;
				if (stmtContext || explicitNewlines != 0) {
					if (node.Range.Source is EmptySourceFile || string.IsNullOrEmpty(node.Range.Source.FileName)) {
						// synthetic code: no source location
						if (sourceLine != -1) {
							sourceLine = -1;
							lineDirective = "#line default";
						}
					} else {
						var start = node.Range.Start;
						if (sourceLine != start.Line) {
							sourceLine = start.Line;
							lineDirective = "#line "+start.Line+" "+EcsNodePrinter.PrintString(start.FileName, '"');
						}
					}
				}

				int sourceLineWas = sourceLine;

				if (node.Name.Name.StartsWith("#") && node.ArgCount > 1) {
					// For some special calls like #if, #while, and #doWhile, 
					// printer might print newlines in places we don't know about,
					// so erase our knowledge of what the current line is.
					if (sourceLine > 0)
						sourceLine = int.MinValue;
				}

				// Process children
				node = node.WithAttrs(AddLineDirectives(node.Attrs, false, ref sourceLine));
				if (node.IsCall)
					node = node.WithArgs(AddLineDirectives(node.Args, node.Calls(S.Braces), ref sourceLine));

				if (sourceLine > 0)
					sourceLine += node.GetTrailingTrivia().Count(n => n.IsIdNamed(S.TriviaNewline));

				// Finally, add a line directive if requested.
				if (lineDirective != null) {
					var trivia = F.Trivia(S.TriviaCsPPRawText, lineDirective);
					if (!node.Attrs.Contains(trivia)) {
						// Trivia tends not to be included in the source range so adding #line
						// before trivia is generally wrong, while adding #line after attributes
						// tends to be wrong too. Sigh. Search for a good location to insert...
						// unless inserting #line default which we can just put at the beginning
						int insertIndex = 0;
						if (sourceLineWas > 0) {
							insertIndex = node.Attrs.FirstIndexWhere(n => n.Range.Start.Line == sourceLineWas && n.Range.Source == node.Range.Source) 
								?? node.Attrs.Count;
						}
						node = node.WithAttrs(node.Attrs.Insert(insertIndex, trivia));
					}
				}

				return node;
			});
			sourceLine_ = sourceLine;
			return nodes;
		}
	}
}
