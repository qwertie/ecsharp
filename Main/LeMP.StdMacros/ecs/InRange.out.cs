// Generated from InRange.ecs by LeMP custom tool. LeMP version: 2.9.0.3
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Syntax;
using Loyc.Collections;
using static LeMP.StandardMacros;
using S = Loyc.Syntax.CodeSymbols;

namespace LeMP.ecs
{
	partial class StandardMacros
	{
		[LexicalMacro("x in lo..hi; x in lo...hi; x in ..hi; x in lo..._; x in range", 
		"Converts an 'in' expression to a normal C# expression using the following rules " + 
		"(keeping in mind that the EC# parser treats `..<` as an alias for `..`):\n" + 
		"1. `x in _..hi` and `x in ..hi` become `x.IsInRangeExcl(hi)`\n" + 
		"2. `x in _...hi` and `x in ...hi` become `x.IsInRangeIncl(hi)`\n" + 
		"3. `x in lo.._` and `x in lo..._` become simply `x >= lo`\n" + 
		"4. `x in lo..hi` becomes `x.IsInRangeExcludeHi(lo, hi)`\n" + 
		"5. `x in lo...hi` becomes `x.IsInRange(lo, hi)`\n" + 
		"6. `x in range` becomes `range.Contains(x)`\n" + 
		"The first applicable rule is used.", "'in")] 
		public static LNode In(LNode node, IMacroContext context)
		{
			if (context.Parent.Calls(S.From) || context.Parent.Calls(S.Join))
				return null;	// ignore 'in' inside LINQ expression
			{
				LNode range, x;
				if (node.Calls(CodeSymbols.In, 2) && (x = node.Args[0]) != null && (range = node.Args[1]) != null) {
					LNode parens;
					range = range.WithoutAttrNamed(S.TriviaInParens, out parens);
					if (parens == null) {
						{
							LNode hi, lo;
							if (range.Calls(CodeSymbols.DotDot, 2) && (lo = range.Args[0]) != null && (hi = range.Args[1]) != null) {
								if (lo.IsIdNamed(__))
									return LNode.Call(CodeSymbols.LT, LNode.List(x, hi)).SetStyle(NodeStyle.Operator);
								else if (hi.IsIdNamed(__))
									return LNode.Call(CodeSymbols.GE, LNode.List(x, lo)).SetStyle(NodeStyle.Operator);
								else
									return LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(x, LNode.Id((Symbol) "IsInRangeExcludeHi"))).SetStyle(NodeStyle.Operator), LNode.List(lo, hi));
							} else if (range.Calls(CodeSymbols.DotDot, 1) && (hi = range.Args[0]) != null) {
								return LNode.Call(CodeSymbols.LT, LNode.List(x, hi)).SetStyle(NodeStyle.Operator);
							} else if (range.Calls(CodeSymbols.DotDotDot, 2) && (lo = range.Args[0]) != null && (hi = range.Args[1]) != null) {
								if (lo.IsIdNamed(__))
									return LNode.Call(CodeSymbols.LE, LNode.List(x, hi)).SetStyle(NodeStyle.Operator);
								else if (hi.IsIdNamed(__))
									return LNode.Call(CodeSymbols.GE, LNode.List(x, lo)).SetStyle(NodeStyle.Operator);
								else
									return LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(x, LNode.Id((Symbol) "IsInRange"))).SetStyle(NodeStyle.Operator), LNode.List(lo, hi));
							} else if (range.Calls(CodeSymbols.DotDotDot, 1) && (hi = range.Args[0]) != null) {
								return LNode.Call(CodeSymbols.LE, LNode.List(x, hi)).SetStyle(NodeStyle.Operator);
							}
						}
					}
					return LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(range, LNode.Id((Symbol) "Contains"))).SetStyle(NodeStyle.Operator), LNode.List(x));
				}
			}
			return null;
		}
	}
}