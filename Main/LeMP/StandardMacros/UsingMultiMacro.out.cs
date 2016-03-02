// Generated from UsingMultiMacro.ecs by LeMP custom tool. LeMP version: 1.4.1.0
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
using Loyc.Collections;
using Loyc.Syntax;
using Loyc.Ecs;
namespace LeMP
{
	partial class StandardMacros
	{
		[LexicalMacro("using (System, System.(Collections.Generic, Linq, Text));", "Generates multiple using-statements from a single one.", "#import", Mode = MacroMode.Passive | MacroMode.Normal)] public static LNode UsingMulti(LNode stmt, IMacroContext context)
		{
			{
				LNode multiNamespace;
				if (stmt.Calls(CodeSymbols.Import, 1) && (multiNamespace = stmt.Args[0]) != null)
					try {
						var list = GetNamespaces(stmt[0]);
						if (list == null)
							return null;
						return LNode.Call(CodeSymbols.Splice, new VList<LNode>(list.Select(namespc => (LNode)LNode.Call(CodeSymbols.Import, LNode.List(namespc)))));
					} catch (NotSupportedException) {
						context.Write(Severity.Note, stmt, "Multi-using statement seems malformed. Correct example: `using (System, System.(Text, Linq));`");
					}
			}
			return null;
		}
		static IEnumerable<LNode> GetNamespaces(LNode multiName)
		{
			{
				LNode outerNamespace;
				VList<LNode> tupleArgs;
				if (multiName.Calls(CodeSymbols.Tuple)) {
					tupleArgs = multiName.Args;
					return tupleArgs.SelectMany(expr => GetNamespaces(expr) ?? Range.Single(expr));
				} else if (multiName.Calls(CodeSymbols.Dot, 2) && (outerNamespace = multiName.Args[0]) != null && multiName.Args[1].Calls(CodeSymbols.Tuple)) {
					tupleArgs = multiName.Args[1].Args;
					return tupleArgs.SelectMany(arg => (GetNamespaces(arg) ?? Range.Single(arg)).Select(subNS => MergeIdentifiers(outerNamespace, subNS)));
				} else
					return null;
			}
		}
		static LNode MergeIdentifiers(LNode left, LNode right)
		{
			if (right.IsId) {
				if (right.Name.Name == "" || right.Name.Name == "#")
					return left;
				else
					return LNode.Call(CodeSymbols.Dot, LNode.List(left, right));
			} else {
				{
					LNode right1, right2;
					if (right.Calls(CodeSymbols.Dot, 2) && (right1 = right.Args[0]) != null && (right2 = right.Args[1]) != null)
						return MergeIdentifiers(LNode.Call(CodeSymbols.Dot, LNode.List(left, right1)), right2);
					else
						throw new NotSupportedException();
				}
			}
		}
	}
}
