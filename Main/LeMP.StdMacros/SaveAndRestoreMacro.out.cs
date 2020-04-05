// Generated from SaveAndRestoreMacro.ecs by LeMP custom tool. LeMP version: 2.8.0.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Syntax;
using LeMP.CSharp7.To.OlderVersions;
using S = Loyc.Syntax.CodeSymbols;

namespace LeMP
{
	partial class StandardMacros
	{
		[LexicalMacro("saveAndRestore(VarOrProperty); saveAndRestore(VarOrProperty = NewValue);", 
		"Saves the value of a variable or property, and uses `on_finally` to restore it at the end of the current scope. " 
		+ "If the argument is an assignment `P = V`, then the property `P` is set to `V` after its value is saved. " 
		+ "Warning: any references through which the variable or property is accessed is evaluated multiple times. " 
		+ "For example, `saveAndRestore(A.B)` evaluates `A` twice, once to save the value and again to restore it. " 
		+ "If `A` changes between the save and restore points, the value will be \"restored\" into the wrong `B`. ", 
		Mode = MacroMode.Normal)] 
		public static LNode saveAndRestore(LNode node, IMacroContext context)
		{
			var tmp_10 = context.GetArgsAndBody(true);
			var args = tmp_10.Item1;
			var body = tmp_10.Item2;
			if (args.Count == 1) {
				LNode newValue = null;
				{
					var tmp_11 = args[0];
					LNode property;
					if (tmp_11.Calls(CodeSymbols.Assign, 2) && (property = tmp_11.Args[0]) != null && (newValue = tmp_11.Args[1]) != null || (property = tmp_11) != null) {
						string mainProp = KeyNameComponentOf(property).Name;
						string varPrefix = "old" + mainProp + "_";
						LNode varName, varDecl = TempVarDecl(context, property, out varName, varPrefix);
						LNode tryFinally = LNode.Call(CodeSymbols.Try, LNode.List(LNode.Call(CodeSymbols.Braces, LNode.List(body)).SetStyle(NodeStyle.StatementBlock), LNode.Call(CodeSymbols.Finally, LNode.List(LNode.Call(CodeSymbols.Braces, LNode.List(LNode.Call(CodeSymbols.Assign, LNode.List(property, varName)).SetStyle(NodeStyle.Operator))).SetStyle(NodeStyle.StatementBlock)))));
						if (newValue != null) {
							return LNode.Call(CodeSymbols.Splice, LNode.List(varDecl, LNode.Call(CodeSymbols.Assign, LNode.List(property, newValue)).SetStyle(NodeStyle.Operator), tryFinally));
						} else {
							return LNode.Call(CodeSymbols.Splice, LNode.List(varDecl, tryFinally));
						}
					}
				}
			}
			return null;
		}
	}
}