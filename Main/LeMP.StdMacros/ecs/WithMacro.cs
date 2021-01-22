using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Syntax;
using static LeMP.StandardMacros;

namespace LeMP.ecs
{
	using S = CodeSymbols;
	using Loyc.Collections;

	partial class StandardMacros
	{
		static readonly Symbol _with = GSymbol.Get("with");

		[LexicalMacro("with (Some.Thing) { .Member = 0; .Method(); }", 
			@"Use members of a particular object with a shorthand ""prefix-dot"" notation. **Warning**: if used with a value type, a copy of the value is made.", 
			Mode = MacroMode.ProcessChildrenBefore)] // post-normal-macro-expansion
		public static LNode with(LNode fn, IMacroContext context)
		{
			LNode braces;
			if (fn.ArgCount != 2 || !(braces = fn.Args[1]).Calls(S.Braces))
				return null;

			LNode tmp = F.Id(NextTempName(context));
			WList<LNode> stmts = braces.Args.ToWList();
			stmts = stmts.SmartSelect(stmt => 
				stmt.ReplaceRecursive(expr => {
					if (expr.Calls(S.Dot, 1))
						return expr.WithArgs(LNode.List(tmp, expr.Args.Last));
					else if (expr.IsIdNamed("#"))
						return tmp;
					return null;
				}));

			stmts.Insert(0, F.Var(null, tmp.Name, fn.Args[0]));
			if (IsExpressionContext(context)) {
				stmts.Add(tmp);
				return F.Call("#runSequence", stmts.ToVList());
			} else {
				return F.Braces(stmts.ToVList());
			}
		}

		public static bool IsExpressionContext(LNode parent, int index)
		{
			if (parent.Name == S.Braces)
				return false;
			if (parent.HasSpecialName) {
				if (parent.Name == S.If && index > 0)
					return false;
				if (parent.Name.IsOneOf(S.While, S.UsingStmt, S.Lock, S.Fixed) && index == 1)
					return false;
				if (parent.Name.IsOneOf(S.DoWhile, S.Try) && index == 0)
					return false;
				if (parent.Name == S.For && index == 3)
					return false;
				if (parent.Name == S.ForEach && index == 2)
					return false;
			}
			return true;
		}
		public static bool IsExpressionContext(IMacroContext ctx)
		{
			if (ctx.Ancestors.Count <= 2) // unit test context
				return false;
			return IsExpressionContext(ctx.Parent, ctx.Parent.IndexOf(ctx.CurrentNode()) ?? -1);
		}
	}
}
