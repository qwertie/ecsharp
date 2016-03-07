// Generated from InRange.ecs by LeMP custom tool. LeMP version: 1.5.1.0
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
using S = Loyc.Syntax.CodeSymbols;
namespace LeMP
{
	partial class StandardMacros
	{
		[LexicalMacro("x in lo..hi; x in lo...hi; x in ..hi; x in lo..._; x in range", "Converts an 'in' expression to a normal C# expression using the following rules " + "(keeping in mind that the EC# parser treats `..<` as an alias for `..`):\n" + "1. `x in _..hi` and `x in ..hi` become `x.IsInRangeExcl(hi)`\n" + "2. `x in _...hi` and `x in ...hi` become `x.IsInRangeIncl(hi)`\n" + "3. `x in lo.._` and `x in lo..._` become simply `x >= lo`\n" + "4. `x in lo..hi` becomes `x.IsInRangeExcl(lo, hi)`\n" + "5. `x in lo...hi` becomes `x.IsInRangeIncl(lo, hi)`\n" + "6. `x in range` becomes `range.Contains(x)`\n" + "The first applicable rule is used.", "#in")] public static LNode In(LNode node, IMacroContext context)
		{
			{
				LNode range, x;
				if (node.Calls(CodeSymbols.In, 2) && (x = node.Args[0]) != null && (range = node.Args[1]) != null) {
					LNode parens;
					range = range.WithoutAttrNamed(S.TriviaInParens, out parens);
					if (parens == null) {
						{
							LNode hi, lo;
							if (range.Calls(CodeSymbols.DotDot, 2) && (lo = range.Args[0]) != null && (hi = range.Args[1]) != null)
								if (lo.IsIdNamed(__))
									return LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(x, LNode.Id((Symbol) "IsInRangeExcl"))), LNode.List(hi));
								else if (hi.IsIdNamed(__))
									return LNode.Call(CodeSymbols.GE, LNode.List(x, lo)).SetStyle(NodeStyle.Operator);
								else
									return LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(x, LNode.Id((Symbol) "IsInRangeExcl"))), LNode.List(lo, hi));
							else if (range.Calls(CodeSymbols.DotDot, 1) && (hi = range.Args[0]) != null)
								return LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(x, LNode.Id((Symbol) "IsInRangeExcl"))), LNode.List(hi));
							else if (range.Calls(CodeSymbols.DotDotDot, 2) && (lo = range.Args[0]) != null && (hi = range.Args[1]) != null)
								if (lo.IsIdNamed(__))
									return LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(x, LNode.Id((Symbol) "IsInRangeIncl"))), LNode.List(hi));
								else if (hi.IsIdNamed(__))
									return LNode.Call(CodeSymbols.GE, LNode.List(x, lo)).SetStyle(NodeStyle.Operator);
								else
									return LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(x, LNode.Id((Symbol) "IsInRangeIncl"))), LNode.List(lo, hi));
							else if (range.Calls(CodeSymbols.DotDotDot, 1) && (hi = range.Args[0]) != null)
								return LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(x, LNode.Id((Symbol) "IsInRangeIncl"))), LNode.List(hi));
						}
					}
					return LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(range, LNode.Id((Symbol) "Contains"))), LNode.List(x));
				}
			}
			return null;
		}
		static LNode Range_Excl = LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "Range"), LNode.Id((Symbol) "Excl")));
		static LNode Range_Incl = LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "Range"), LNode.Id((Symbol) "Incl")));
		static LNode Range_Low = LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "Range"), LNode.Id((Symbol) "Low")));
		[LexicalMacro("lo..hi; ..hi; lo.._", "Given `lo..hi, produces `Range.Excl(lo, hi)", "..")] public static LNode RangeExcl(LNode node, IMacroContext context)
		{
			{
				LNode hi, lo;
				if (node.Args.Count == 2 && (lo = node.Args[0]) != null && (hi = node.Args[1]) != null)
					if (lo.IsIdNamed(__))
						return LNode.Call(Range_Excl, LNode.List(hi));
					else if (hi.IsIdNamed(__))
						return LNode.Call(Range_Low, LNode.List(lo));
					else
						return LNode.Call(Range_Excl, LNode.List(lo, hi));
				else if (node.Args.Count == 1 && (hi = node.Args[0]) != null)
					return LNode.Call(Range_Excl, LNode.List(hi));
			}
			return null;
		}
		[LexicalMacro("lo..hi; ..hi; lo.._", "Given `lo..hi, produces `Range.Excl(lo, hi)", "...")] public static LNode RangeIncl(LNode node, IMacroContext context)
		{
			{
				LNode hi, lo;
				if (node.Args.Count == 2 && (lo = node.Args[0]) != null && (hi = node.Args[1]) != null)
					if (lo.IsIdNamed(__))
						return LNode.Call(Range_Incl, LNode.List(hi));
					else if (hi.IsIdNamed(__))
						return LNode.Call(Range_Low, LNode.List(lo));
					else
						return LNode.Call(Range_Incl, LNode.List(lo, hi));
				else if (node.Args.Count == 1 && (hi = node.Args[0]) != null)
					return LNode.Call(Range_Incl, LNode.List(hi));
			}
			return null;
		}
	}
}
