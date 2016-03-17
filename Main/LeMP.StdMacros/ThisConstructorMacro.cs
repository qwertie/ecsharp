using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax;
using Loyc.Ecs;
using S = Loyc.Syntax.CodeSymbols;

namespace LeMP
{
	partial class StandardMacros
	{
		// TODO: support "attribute" macros.
		[LexicalMacro("class Foo { this() {} }", 
			"Supports the EC# 'this()' syntax for constructors by replacing 'this' with the name of the enclosing class.", 
			"#cons", Mode = MacroMode.Passive | MacroMode.Normal | MacroMode.PriorityOverride)]
		public static LNode Constructor(LNode cons, IMacroContext context)
		{
			if (cons.ArgCount >= 3 && cons.Args[1].IsIdNamed(S.This))
			{
				var anc = context.Ancestors;
				LNode space = anc.TryGet(anc.Count - 3, LNode.Missing), typeName;
				Symbol type = EcsValidators.SpaceStatementKind(space);
				if (type != null && anc[anc.Count - 2] == space.Args[2]) {
					typeName = space.Args[0];
					return cons.WithArgChanged(1, F.Id(KeyNameComponentOf(typeName)));
				}
			}
			return null;
		}
	}
}
