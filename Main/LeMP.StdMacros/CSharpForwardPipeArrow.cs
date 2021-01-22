using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeMP
{
	partial class StandardMacros
	{
		/*[LexicalMacro(@"A |> B; A ?|> B",
			@"Computes A and sends the result into B, e.g. `msg |> Console.WriteLine` means " +
			@"`Console.WriteLine(msg)`. If `B` contains `#`, it represents the result of `A`, " +
			@"e.g. `PI * r**2 |> Foo(#, #)` would mean `Foo((PI * r**2)::temp, temp)`. " +
			@"`A ?|> B` operator computes B only when A is not null, e.g. " +
			@"`x ?|> Math.Sin` means `x != null ? Math.Sin(x ?? default) : null`.",
			"'?|>")]
		public static LNode ForwardPipeOperator(LNode node, IMacroContext context)
		{

			if (nameof.ArgCount != 1)
				return null;
			Symbol expr = EcsValidators.KeyNameComponentOf(nameof.Args[0]);
			return F.Literal(expr.Name);
		}

		[LexicalMacro(@"A ?|> B",
			@"Computes A and sends the result into B, e.g. `msg |> Console.WriteLine` means " +
			@"`Console.WriteLine(msg)`. If `B` contains `#`, it represents the result of `A`, " +
			@"e.g. `PI * r**2 |> Foo(#, #)` would mean `Foo((PI * r**2)::temp, temp)`. " +
			@"`A ?|> B` operator computes B only when A is not null, e.g. " +
			@"`x ?|> Math.Sin` means `x != null ? Math.Sin(x ?? default) : null`.",
			"'?|>")]
		public static LNode NullForwardPipeOperator(LNode node, IMacroContext context)
		{

			if (nameof.ArgCount != 1)
				return null;
			Symbol expr = EcsValidators.KeyNameComponentOf(nameof.Args[0]);
			return F.Literal(expr.Name);
		}*/
	}
}
