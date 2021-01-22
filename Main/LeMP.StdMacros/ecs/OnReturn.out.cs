// Generated from OnReturn.ecs by LeMP custom tool. LeMP version: 2.9.0.3
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
using Loyc.Collections;
using static LeMP.StandardMacros;

namespace LeMP.ecs
{
	using S = CodeSymbols;

	partial class StandardMacros
	{
		static readonly Symbol _on_return = (Symbol) "on_return";
		static readonly Symbol __result__ = (Symbol) "__result__";
		static readonly Symbol __retexpr__ = (Symbol) "<returned expr>";
		static readonly LNode Id__result__ = LNode.Id((Symbol) "__result__");

		[LexicalMacro("on_return (result) { Console.WriteLine(result); }", 
		"In the code that follows this macro, all return statements are replaced by a block that runs a copy of this code and then returns. " + 
		"For example, the code `{ on_return(r) { r++; } Foo(); return Math.Abs(x); }` is replaced by " + 
		"`{ Foo(); { var r = Math.Abs(x); r++; return r; } }`. Because this is a lexical macro, it " + 
		"lets you do things that you shouldn't be allowed to do. For example, `{ on_return { x++; } int x=0; return; }` " + 
		"will compile although the `on_return` block shouldn't be allowed to access `x`. Please don't do that, because if this were a built-in language feature, it wouldn't be allowed.")] 
		public static LNode on_return(LNode node, IMacroContext context)
		{
			// Originally this could replace a plain 'return;' statement with one 
			// that returns a value, i.e. { on_return(var r = expr) { G(); } F(); return; } 
			//                         => { F(); { var r = expr; G(); return r; } }"
			// This feature was removed because if the return statement is missing at the
			// end, a macro cannot decide 100% reliably whether or not it is necessary to 
			// add one (mainly because we don't have proper reachability analysis, just the 
			// heuristic NextStatementMayBeReachable())
			LNodeList rest;
			LNode varDecl, bracedHandler = ValidateOnStmt(node, context, out rest, out varDecl);
			if (bracedHandler == null)
				return null;

			rest = context.PreProcess(rest);
			bracedHandler = context.PreProcess(bracedHandler);

			// Get/construct the declaration of the var to return, and get its name
			LNode varName;
			if (varDecl == null) {
				varName = Id__result__;
				varDecl = F.Var(F.Missing, varName);
			} else {
				{
					LNode tmp_10;
					if (varDecl.Calls(CodeSymbols.Var, 2) && (tmp_10 = varDecl.Args[1]) != null && tmp_10.Calls(CodeSymbols.Assign, 2) && (varName = tmp_10.Args[0]) != null) {
						context.Sink.Error(varName, "The return value cannot be assigned here. The value of this variable must be placed on the return statement(s).");
					} else if (varDecl.Calls(CodeSymbols.Var, 2) && (varName = varDecl.Args[1]) != null) { } else {
						if ((varName = varDecl).IsId)
							varDecl = varName.With(S.Var, F.Missing, varName);
						else
							return Reject(context, varDecl, "The first parameter to on_return must be a simple identifier (the name of a variable to return) or a variable declaration (for a variable to be returned).");
					}
				}
			}

			bool foundReturn = false;
			rest = rest.SmartSelect(arg => arg.ReplaceRecursive(rnode => {
				{
					LNode retVal;
					if (rnode.Calls(CodeSymbols.Lambda, 2)) {
						return rnode;	// ignore children of lambda expression or delegate(){}
					} else if (rnode.Calls(CodeSymbols.Return, 0)) {
						foundReturn = true;
						return LNode.Call(CodeSymbols.Braces, LNode.List().AddRange(bracedHandler.Args).Add(rnode)).SetStyle(NodeStyle.StatementBlock);
					} else if (rnode.Calls(CodeSymbols.Return, 1) && (retVal = rnode.Args[0]) != null) {
						foundReturn = true;
						var retValDecl = varDecl.WithArgChanged(1, LNode.Call(CodeSymbols.Assign, LNode.List(varName, retVal)).SetStyle(NodeStyle.Operator));
						rnode = rnode.WithArgs(varName);
						return LNode.Call(CodeSymbols.Braces, LNode.List().Add(retValDecl).AddRange(bracedHandler.Args).Add(rnode)).SetStyle(NodeStyle.StatementBlock);
					} else {
						return null;
					}
				}
			}));

			if (DetectMissingVoidReturn(context, rest[rest.Count - 1, LNode.Missing]))
				rest.Add(bracedHandler.Args.AsLNode(S.Braces));
			else if (!foundReturn)
				context.Sink.Warning(node, "'on_return': no 'return' statements were found in this context, so this macro had no effect.");

			// Block reprocessing of `rest` because we already preprocessed it
			return LNode.Call((Symbol) "#noLexicalMacros", LNode.List(rest));
		}

		static bool DetectMissingVoidReturn(IMacroContext context, LNode lastStmt)
		{
			if (!NextStatementMayBeReachable(lastStmt))
				return false;

			var anc = context.Ancestors;
			var parent = anc.TryGet(anc.Count - 2, LNode.Missing);
			var grandparent = anc.TryGet(anc.Count - 3, LNode.Missing);
			do {
				if (parent.Calls(S.Braces)) {
					if (grandparent.CallsMin(S.Fn, 4) && grandparent.Args[0].IsIdNamed(S.Void))
						return true;
					if (grandparent.Calls(S.Constructor))
						return true;
					if (grandparent.Calls(S.set, 1) || grandparent.Calls(S.add, 1) || grandparent.Calls(S.remove, 1))
						return true;
					if (grandparent.Calls(S.Lambda, 2))
						return true;
				}
				return false;
			} while (false);
		}
	}
}