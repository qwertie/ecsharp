// Generated from SaveAndRestoreMacro.ecs by LeMP custom tool. LeMP version: 1.7.6.0
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
using S = Loyc.Syntax.CodeSymbols;
namespace LeMP
{
	partial class StandardMacros
	{
		[LexicalMacro("saveAndRestore(VarOrProperty); saveAndRestore(VarOrProperty = NewValue);", "Saves the value of a variable or property, and uses `on_finally` to restore it at the end of the current scope. " + "If the argument is an assignment `P = V`, then the property `P` is set to `V` after its value is saved. " + "Warning: any references through which the variable or property is accessed is evaluated multiple times. " + "For example, `saveAndRestore(A.B)` evaluates `A` twice, once to save the value and again to restore it. " + "If `A` changes between the save and restore points, the value will be \"restored\" into the wrong `B`. ", Mode = MacroMode.Normal)]
		public static LNode saveAndRestore(LNode node, IMacroContext context)
		{
			var tmp_0 = context.GetArgsAndBody(true);
			var args = tmp_0.Item1;
			var body = tmp_0.Item2;
			if (args.Count == 1) {
				LNode newValue = null;
				{
					var tmp_1 = args[0];
					LNode property;
					if (tmp_1.Calls(CodeSymbols.Assign, 2) && (property = tmp_1.Args[0]) != null && (newValue = tmp_1.Args[1]) != null || (property = tmp_1) != null) {
						string mainProp = KeyNameComponentOf(property).Name;
						string varPrefix = "old" + mainProp + "_";
						LNode varName, varDecl = TempVarDecl(context, property, out varName, varPrefix);
						LNode tryFinally = LNode.Call(CodeSymbols.Try, LNode.List(LNode.Call(CodeSymbols.Braces, LNode.List(body)).SetStyle(NodeStyle.Statement), LNode.Call(CodeSymbols.Finally, LNode.List(LNode.Call(CodeSymbols.Braces, LNode.List(LNode.Call(CodeSymbols.Assign, LNode.List(property, varName)).SetStyle(NodeStyle.Operator))).SetStyle(NodeStyle.Statement)))));
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
