using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Syntax;

namespace Ecs.Macros
{
	public static partial class BuiltinMacro
	{
		[LexicalMacro("#tuple")]
		public static LNode Tuple(LNode node)
		{
			// TODO: support .[a, b] (and !(a, b)) as syntax for @``<a, b> which will refer to a tuple type
			// Random thought: "@" could mean "#at" and refer to the subject of a #with() statement
			if (node.ArgCount == 0)
				return null;
			if (node.ArgCount == 2)
				return node.WithTarget(F.Dot("Pair", "Create"));
			else
				return node.WithTarget(F.Dot("Tuple", "Create"));
		}
	}
}
