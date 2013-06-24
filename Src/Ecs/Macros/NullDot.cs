using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Syntax;
using System.Diagnostics;
using Loyc;
using Loyc.Utilities;
using S = Loyc.Syntax.CodeSymbols;

namespace Ecs.Macros
{
	[ContainsMacro]
	public static partial class NullDotMacro
	{
		static LNodeFactory F = new LNodeFactory(new EmptySourceFile("NullDot.cs"));

		[LexicalMacro("#?.")]
		public static LNode NullDot(LNode node)
		{
			if (!node.Calls(S.NullDot, 2))
				return null;

			var a = node.Args;
			LNode prefix = a[0], suffix = a[1];
			// So our input will be something like a.b??.c().d<x>, which is parsed
			//     (a.b) ??. (c().d<x>)
			// in EC# we would transform this to 
			//     a.b::tmp != null ? tmp.c().d<x> : null
			// but there's no EC# compiler yet, so instead use code that plain C#
			// can support:
			//     a.b != null ? (a.b).c().d<x> : null
			return F.Call(S.QuestionMark, F.Call(S.Neq, prefix, F.@null), 
				ConvertToNormalDot(prefix, suffix), null);
		}

		static readonly HashSet<Symbol> PrefixOps = new HashSet<Symbol>(new[] 
			{ S._UnaryPlus, S._Negate, S.NotBits, S.PreInc, S.PreDec, S.New });
		static readonly HashSet<Symbol> OtherOps = new HashSet<Symbol>(new[] 
			{ S.Dot, S.Of, S.Bracks, S.ColonColon, S.QuickBind, S.Cast, S.PostInc, S.PostDec });

		static LNode ConvertToNormalDot(LNode prefix, LNode suffix)
		{
			// Essentially, we must increase the precedence of ??. to convert it to a normal dot.
			// This often requires multiple stages, as in:
			//     (a.b) ??. (c().d<x>)    ==> (a.b ??. c().d)<x>      ==> (a.b ??. c)().d<x>      ==> a.b.c().d<x>
			//     (a.b) ??. #of(c().d, x) ==> #of((a.b) ??. c().d, x) ==> #of((a.b ??. c)().d, x) ==> #of(a.b.c().d, x)
			// The cases to be handled are...
			//     x ??. y         <=>  x ??. y             ==>  x.y
			//     x ??. "foo"     <=>  x ??. "foo"         ==>  x."Foo"
			//     x ??. ++y       <=>  x ??. #++(y)        ==>  x.(++y)
			//     x ??. y<a, b>   <=>  x ??. #of(y, a, b)  ==>  #of(x??.y, a, b)
			//     x ??. y[a, b]   <=>  x ??. #[](y, a, b)  ==>  #[](x??.y, a, b)
			//     x ??. y.z       <=>  x ??. #.(y, z)      ==>  #::(x??.y, z)
			//     x ??. y::z      <=>  x ??. #::(y, z)     ==>  #::(x??.y, z)
			//     x ??. y:::z     <=>  x ??. #:::(y, z)    ==>  #:::(x??.y, z)
			//     x ??. y->z      <=>  x ??. #->(y, z)     ==>  #->(x??.y, z)
			//     x ??. y(->z)    <=>  x ??. #cast(y, z)   ==>  #cast(x??.y, z)
			//     x ??. y++       <=>  x ??. #`suf++`(y)   ==>  #`suf++`(x??.y)
			//     x ??. typeof(y) <=>  x ??. #typeof(y)    ==>  Not handled. Default case used.
			//     x ??. y(a, b)   <=>  x ??. y(a, b)       ==>  x??.y(a, b)     (default case!)
			// The following groups are handled essentially the same way:
			// 1. Ids, Literals and prefix operators (+ - ++ -- ! ~ new)
			// 2. #of, #[] #., #::, #:::, #->, #cast, #suf++, #suf--
			// 3. All other calls
			var c = suffix.ArgCount;
			var name = suffix.Name;
			if (suffix.IsCall) {
				if (c == 1 && PrefixOps.Contains(name))
					return F.Dot(prefix, suffix);
				else if (c >= 1 && OtherOps.Contains(name)) {
					var inner = ConvertToNormalDot(prefix, suffix.Args[0]);
					return suffix.WithArgChanged(0, inner);
				} else {
					var inner = ConvertToNormalDot(prefix, suffix.Target);
					return suffix.WithTarget(inner);
				}
			} else
				return F.Dot(prefix, suffix);
		}
	}
}
