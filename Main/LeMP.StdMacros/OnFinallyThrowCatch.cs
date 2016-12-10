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
		static readonly Symbol _on_throw_catch = (Symbol)"on_throw_catch";
		static readonly Symbol _exit = (Symbol)"exit";
		static readonly Symbol _success = (Symbol)"success";
		static readonly Symbol _failure = (Symbol)"failure";
		static readonly Symbol _Exception = (Symbol)"Exception";

		[LexicalMacro("on_finally { _foo = 0; }", 
			"Wraps the code that follows this macro in a try-finally statement, with the specified block as the 'finally' block.")]
		public static LNode on_finally(LNode node, IMacroContext context)
		{
			VList<LNode> rest;
			LNode firstArg, on_handler = ValidateOnStmt(node, context, out rest, out firstArg);
			if (on_handler == null || firstArg != null)
				return null;

			node.Style &= ~NodeStyle.OneLiner; // avoid collapsing output to one line
			return node.With(S.Try, F.Braces(rest), node.With(S.Finally, on_handler));
		}

		[LexicalMacro("on_throw(exc) { OnThrowAction(exc); }", 
			"Specifies an action to take in case the current block of code ends with an exception being thrown. "+
			"It wraps the code that follows this macro in a try-catch statement, with the braced block you provide as the 'catch' block, "+
			"followed by a 'throw;' statement to rethrow the exception. "+
			"The first argument to on_throw is optional and represents the desired name of the exception variable.")]
		public static LNode on_throw(LNode node, IMacroContext context)
		{
			VList<LNode> rest;
			LNode firstArg, on_handler = ValidateOnStmt(node, context, out rest, out firstArg);
			if (on_handler == null)
				return null;
			on_handler = on_handler.PlusArg(F.Call(S.Throw));
			return TransformOnCatch(node, firstArg, F.Braces(rest), on_handler);
		}

		[LexicalMacro("on_throw_catch(exc) { _foo = 0; }", 
			"Wraps the code that follows this macro in a try-catch statement, with the given braced block as the 'catch' block. "+
			"The first argument to on_error_catch is optional and represents the desired name of the exception variable. "+
			"In contrast to on_throw(), the exception is not rethrown at the end of the generated catch block.",
			"on_throw_catch", "on_error_catch")]
		public static LNode on_throw_catch(LNode node, IMacroContext context)
		{
			VList<LNode> rest;
			LNode firstArg, on_handler = ValidateOnStmt(node, context, out rest, out firstArg);
			return TransformOnCatch(node, firstArg, F.Braces(rest), on_handler);
		}

		private static LNode TransformOnCatch(LNode node, LNode firstArg, LNode rest, LNode on_handler)
		{
			if (on_handler == null)
				return null;
			if (firstArg == null)
				firstArg = LNode.Missing;
			else if (firstArg.IsId)
				firstArg = firstArg.With(S.Var, F.Id(_Exception), firstArg);
			return node.With(S.Try, rest, node.With(S.Catch, firstArg, F.Missing, on_handler));
		}

		private static LNode ValidateOnStmt(LNode node, IMacroContext context, out VList<LNode> restOfStmts, out LNode firstArg)
		{
			var a = node.Args;
			LNode on_handler;
			restOfStmts = LNode.List();
			firstArg = null;
			if (a.Count == 2) {
				firstArg = a[0];
			} else if (a.Count != 1)
				return null;
			if (!(on_handler = a.Last).Calls(S.Braces))
				return null;
			if (context.RemainingNodes.Count == 0)
				context.Write(Severity.Warning, node, "{0} should not be the final statement of a block.", node.Name);
			restOfStmts = new VList<LNode>(context.RemainingNodes);
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
					return F.Call(_on_throw_catch, a[1]);
				else
					return Reject(context, a[0], "Expected 'exit', 'success', or 'failure'");
			}
			return null;
		}
	}
}
