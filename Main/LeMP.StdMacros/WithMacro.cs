using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Syntax;

namespace LeMP
{
	using S = CodeSymbols;
	using Loyc.Collections;

	partial class StandardMacros
	{
		static readonly Symbol _with = GSymbol.Get("with");

		[LexicalMacro("with (Some.Thing) { .Member = 0; .Method(); }", 
			@"Use members of a particular object with a shorthand ""prefix-dot"" notation. **Warning**: if used with a value type, a copy of the value is made.", 
			Mode = MacroMode.ProcessChildrenBefore)] // post-normal-macro-expansion
		public static LNode with(LNode fn, IMessageSink sink)
		{
			LNode braces;
			if (fn.ArgCount != 2 || !(braces = fn.Args[1]).Calls(S.Braces))
				return null;

			LNode tmp = F.Id(NextTempName());
			WList<LNode> stmts = braces.Args.ToWList();
			stmts = stmts.SmartSelect(stmt => 
				stmt.ReplaceRecursive(expr => {
					if (expr.Calls(S.Dot, 1))
						return expr.WithArgs(new VList<LNode>(tmp, expr.Args.Last));
					else if (expr.IsIdNamed("#"))
						return tmp;
					return null;
				}));
			stmts.Insert(0, F.Var(null, tmp.Name, fn.Args[0]));
			return F.Braces(stmts.ToVList());
		}
	}
}
