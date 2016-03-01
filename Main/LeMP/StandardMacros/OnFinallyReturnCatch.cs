using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Syntax;

namespace LeMP
{
	using Loyc.Collections;
	using S = CodeSymbols;

	public partial class StandardMacros
	{
		static readonly Symbol _on_finally = (Symbol)"on_finally";
		static readonly Symbol _on_return = (Symbol)"on_return";
		static readonly Symbol _on_catch = (Symbol)"on_catch";
		static readonly Symbol _exit = (Symbol)"exit";
		static readonly Symbol _success = (Symbol)"success";
		static readonly Symbol _failure = (Symbol)"failure";
		static readonly Symbol _Exception = (Symbol)"Exception";
		static readonly Symbol __exception__ = (Symbol)"__exception__";
		static readonly Symbol __result__ = (Symbol)"__result__";
		static readonly Symbol __retexpr__ = (Symbol)"<retexpr>";

		[LexicalMacro("on_finally { _foo = 0; }", 
			"Wraps the code that follows this macro in a try-finally statement, with the specified block as the 'finally' block.")]
		public static LNode on_finally(LNode node, IMacroContext context)
		{
			LNode firstArg, rest, on_handler = ValidateOnStmt(node, context, out rest, out firstArg);
			if (on_handler == null || firstArg != null)
				return null;
			return node.With(S.Try, rest, node.With(S.Finally, on_handler));
		}

		[LexicalMacro("on_throw(exc) { _foo = 0; }", 
			"Specifies an action to take in case the current block of code ends with an exception being thrown."+
			"It wraps the code that follows this macro in a try-catch statement, with the specified block as the 'catch' block, "+
			"followed by a 'throw;' statement to rethrow the exception. "+
			"The first argument to on_throw is optional and represents the desired name of the exception variable.")]
		public static LNode on_throw(LNode node, IMacroContext context)
		{
			LNode firstArg, rest, on_handler = ValidateOnStmt(node, context, out rest, out firstArg);
			if (on_handler == null)
				return null;
			on_handler = on_handler.PlusArg(F.Call(S.Throw));
			return TransformOnCatch(node, firstArg, rest, on_handler);
		}

		[LexicalMacro("on_catch(exc) { _foo = 0; }", 
			"Wraps the code that follows this macro in a try-catch statement, with the specified block as the 'catch' block. "+
			"The first argument to on_catch is optional and represents the desired name of the exception variable. "+
			"In contrast to on_throw(), the exception is not rethrown at the end of the generated catch block.")]
		public static LNode on_catch(LNode node, IMacroContext context)
		{
			LNode firstArg, rest, on_handler = ValidateOnStmt(node, context, out rest, out firstArg);
			return TransformOnCatch(node, firstArg, rest, on_handler);
		}

		private static LNode TransformOnCatch(LNode node, LNode firstArg, LNode rest, LNode on_handler)
		{
			if (on_handler == null)
				return null;
			if (firstArg == null)
				firstArg = LNode.Missing;
			else if (firstArg.IsId)
				firstArg = firstArg.With(S.Var, F.Id(_Exception), firstArg);
			return node.With(S.Try, rest, node.With(S.Catch, firstArg, F._Missing, on_handler));
		}

		[LexicalMacro("on_return(result) { result++; }", 
			"In the code that follows this macro, all return statements are replaced by a block that runs a copy of this code and then returns. "+
			"For example, the code { on_return(r) { r++; } Foo(); return x > 0 ? x : -x; } is replaced by " +
			"{ Foo(); { var r = x > 0 ? x : -x; r++; return r; } }. Because this is a lexical macro, it " +
			"lets you do things that you shouldn't be allowed to do. For example, { on_return { x++; } int x=0; return; } "+
			"will compile although the on_return block shouldn't be allowed to access x." +
			"This macro can also replace a plain 'return;' statement with one that returns a value, if the argument to on_return is a variable declaration: " +
			"{ on_return(var tc = Environment.TickCount) { } Foo(); return; } => { Foo(); { var tc = Environment.TickCount; return tc; } }")]
		public static LNode on_return(LNode node, IMacroContext context)
		{
			LNode firstArg, rest, on_handler = ValidateOnStmt(node, context, out rest, out firstArg);
			if (on_handler == null)
				return null;
			
			// Get/construct the declaration of the var to return, and get its name
			LNode varDecl = firstArg, varName = firstArg;
			bool varAssigned = false;
			if (firstArg == null) {
				varName = F.Id(__result__);
				varDecl = F.Var(F._Missing, varName);
			} else {
				if (varDecl.Calls(S.Var, 2)) {
					if (varAssigned = (varName = varDecl.Args[1]).Calls(S.Assign, 2))
						varName = varName.Args[0];
				} else if (varName.IsId) {
					varDecl = node.With(S.Var, F._Missing, varName);
				} else
					return Reject(context, firstArg, "The first parameter to on_return must be a simple identifier (the name of a variable to return) or a variable declaration (for a variable to be returned).");
			}
			var retExpr = F.Call(S.Substitute, F.Id(__retexpr__));
			Pair<LNode, LNode>[] patterns = new Pair<LNode, LNode>[2] {
				// return; => { <on_handler> return; }
				new Pair<LNode,LNode>(F.Call(S.Return), varAssigned
				                ? on_handler.WithArgs(new RVList<LNode>(varDecl)
				                      .AddRange(on_handler.Args)
				                      .Add(F.Call(S.Return, varName)))
				                : on_handler.PlusArg(F.Call(S.Return))),
				// return exp; => { <varDecl = $exp> <on_handler> return <varName>; }
				new Pair<LNode,LNode>(F.Call(S.Return, retExpr),
				                  on_handler.WithArgs(new RVList<LNode>(
				                      varDecl.WithArgChanged(1, F.Call(S.Assign, varName, retExpr)))
				                      .AddRange(on_handler.Args)
				                      .Add(F.Call(S.Return, varName))))
			};
			int replacementCount = 0;
			RVList<LNode> output = StandardMacros.Replace(rest.Args, patterns, out replacementCount);
			if (replacementCount == 0)
				context.Write(Severity.Warning, node, "'on_return': no 'return' statements were found below this line, so this macro had no effect.");
			return output.AsLNode(S.Splice);
		}

		private static LNode ValidateOnStmt(LNode node, IMacroContext context, out LNode restInBraces, out LNode firstArg)
		{
			var a = node.Args;
			LNode on_handler;
			restInBraces = firstArg = null;
			if (a.Count == 2) {
				firstArg = a[0];
			} else if (a.Count != 1)
				return null;
			if (!(on_handler = a.Last).Calls(S.Braces))
				return null;
			if (context.RemainingNodes.Count == 0)
				context.Write(Severity.Warning, node, "{0} should not be the final statement of a block.", node.Name);
			restInBraces = F.Braces(context.RemainingNodes);
			context.DropRemainingNodes = true;
			return on_handler;
		}

		[LexicalMacro("scope(exit) { ... }; scope(success) {..}; scope(failure) {...}", "Translates the three 'scope' statements from the D programming language to the LeMP equivalents on_finally, on_return and on_catch.")]
		public static LNode scope(LNode node, IMacroContext context)
		{
			var a = node.Args;
			if (a.Count == 2 && a[1].Calls(S.Braces) && a[0].IsId) {
				Symbol name = a[0].Name;
				if (name == _exit || name == S.Finally)
					return F.Call(_on_finally, a[1]);
				else if (name == _success || name == S.Return)
					return F.Call(_on_return, a[1]);
				else if (name == _failure || name == S.Catch)
					return F.Call(_on_catch, a[1]);
				else
					return Reject(context, a[0], "Expected 'exit', 'success', or 'failure'");
			}
			return null;
		}
	}
}
