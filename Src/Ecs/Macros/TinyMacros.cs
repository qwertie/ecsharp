using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Syntax;
using Loyc.Utilities;
using S = ecs.CodeSymbols;

namespace Ecs.Macros
{
	[ContainsMacro]
	public static partial class TinyMacros
	{
		static LNodeFactory F = new LNodeFactory(new EmptySourceFile("TinyMacros.cs"));

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

		[LexicalMacro("#??=")]
		public static LNode NullCoalesceSet(LNode node)
		{
			var a = node.Args;
			if (a.Count != 2)
				return null;
			LNode x = a[0], y = a[1];
			// This is INCOMPLETE! But it'll suffice temporarily.
			// #??=(x, y) => x = x ?? y => #=(x, #??(x, y))
			return F.Call(S.Set, x, F.Call(S.NullCoalesce, x, y));
		}

		[LexicalMacro("#:::")]
		public static LNode QuickBind(LNode node)
		{
			var a = node.Args;
			if (a.Count != 2)
				return null;
			LNode x = a[0], y = a[1];
			return F.Var(F._Missing, F.Call(x, y)); // TODO: F.Call(S.Set, x, y)
		}
	}
}
