using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Syntax;
using Loyc.Collections;
using S = Loyc.Syntax.CodeSymbols;
using Loyc.Ecs;

namespace LeMP.CSharp6
{
	[ContainsMacros]
	public class CSharp6Macros
	{
		[LexicalMacro(@"nameof(id_or_expr)", @"Converts the 'key' name component of an expression to a string (e.g. nameof(A.B<C>(D)) == ""B"")")]
		public static LNode @nameof(LNode nameof, IMacroContext context)
		{
			if (nameof.ArgCount != 1)
				return null;
			Symbol expr = EcsValidators.KeyNameComponentOf(nameof.Args[0]);
			return F.Literal(expr.Name);
		}

		[LexicalMacro("a.b?.c.d", "a.b?.c.d means (a.b != null ? a.b.c.d : null)", "'?.", "'??.")]
		public static LNode NullDot(LNode node, IMacroContext context)
		{
			if (!node.Calls(S.NullDot, 2))
				return null;

			var a = node.Args;
			LNode leftSide = a[0], rightSide = a[1];
			// So our input will be something like a.b?.c().d<x>, which is parsed
			//     (a.b) ?. (c().d<x>)
			// in EC# we would transform this to 
			//     a.b::tmp != null ? tmp.c().d<x> : null
			// but there's no EC# compiler yet, so instead use code that plain C#
			// can support:
			//     a.b != null ? (a.b).c().d<x> : null
			LNode condition, thenExpr;
			if (StandardMacros.LooksLikeSimpleValue(leftSide))
			{
				condition = F.Call(S.Neq, leftSide, F.@null);
				thenExpr = ConvertToNormalDot(leftSide, rightSide);
			}
			else
			{
				LNode tempVar = F.Id(StandardMacros.NextTempName(context, leftSide));
				condition = F.Call(S.Neq, F.Var(F.Missing, tempVar, leftSide), F.@null);
				thenExpr = ConvertToNormalDot(tempVar, rightSide);
			}
			return F.InParens(F.Call(S.QuestionMark, condition, thenExpr, F.Null));
		}

		static readonly HashSet<Symbol> PrefixOps = new HashSet<Symbol>(new[] 
			{ S._UnaryPlus, S._Negate, S.NotBits, S.PreInc, S.PreDec, S.New });
		static readonly HashSet<Symbol> OtherOps = new HashSet<Symbol>(new[] 
			{ S.Dot, S.Of, S.IndexBracks, S.ColonColon, S.QuickBind, S.Cast, S.PostInc, S.PostDec, S.NullDot });

		static LNode ConvertToNormalDot(LNode prefix, LNode suffix)
		{
			// Essentially, we must increase the precedence of ?. to convert it to a normal dot.
			// This often requires multiple stages, as in:
			//     (a.b) ?. (c().d<x>)    ==> (a.b ?. c().d)<x>      ==> (a.b ?. c)().d<x>      ==> a.b.c().d<x>
			//     (a.b) ?. #of(c().d, x) ==> #of((a.b) ?. c().d, x) ==> #of((a.b ?. c)().d, x) ==> #of(a.b.c().d, x)
			// The cases to be handled are...
			//     x ?. y         <=>  x ?. y             ==>  x.y               (1)
			//     x ?. "foo"     <=>  x ?. "foo"         ==>  x."Foo"           (1)
			//     x ?. ++y       <=>  x ?. @`++`(y)      ==>  x.(++y)           (1)
			//     x ?. y<a, b>   <=>  x ?. #of(y, a, b)  ==>  #of(x.y, a, b)    (2)
			//     x ?. y[a, b]   <=>  x ?. @`[]`(y, a, b)==>  @`[]`(x.y, a, b)  (2)
			//     x ?. y.z       <=>  x ?. @.(y, z)      ==>  #.(x?.y, z)       (2)
			//     x ?. y::z      <=>  x ?. @`::`(y, z)   ==>  #::(x?.y, z)      (2)
			//     x ?. y:::z     <=>  x ?. @`:::`(y, z)  ==>  #:::(x?.y, z)     (2)
			//     x ?. y->z      <=>  x ?. @`->`(y, z)   ==>  #->(x?.y, z)      (2)
			//     x ?. y(->z)    <=>  x ?. #cast(y, z)   ==>  #cast(x?.y, z)    (2)
			//     x ?. y++       <=>  x ?. @`suf++`(y)   ==>  @`suf++`(x?.y)    (2)
			//     x ?. y ?. z    <=>  x ?. @`?.`(y, z)   ==>  @`?.`(x.y, z)       
			//     x ?. y(a, b)   <=>  x ?. y(a, b)       ==>  x.y(a, b)         (3: default case)
			// The following groups are handled essentially the same way:
			// 1. Ids, Literals and prefix operators (+ - ++ -- ! ~ new)
			// 2. #of, @`[]`, @`.`, @`::`, @`:::`, @`=:`, @`->`, #cast, @`suf++`, @`suf--`
			// 3. All other calls
			var c = suffix.ArgCount;
			var name = suffix.Name;
			if (suffix.IsCall) {
				if (c == 1 && PrefixOps.Contains(name)) // (1)
					return F.Dot(prefix, suffix);
				else if (c >= 1 && OtherOps.Contains(name)) { // (2)
					var inner = ConvertToNormalDot(prefix, suffix.Args[0]);
					return suffix.WithArgChanged(0, inner);
				} else { // (3)
					var inner = ConvertToNormalDot(prefix, suffix.Target);
					return suffix.WithTarget(inner);
				}
			} else // (1)
				return F.Dot(prefix, suffix);
		}

		static LNodeFactory F = new LNodeFactory(new EmptySourceFile("LeMP.CSharp6"));
	}
}
