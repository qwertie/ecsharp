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

namespace LeMP.ecs.deprecated
{
	[ContainsMacros] 
	public partial class Macros
	{
		static LNode Range_ExcludeHi = LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "Range"), LNode.Id((Symbol) "ExcludeHi"))).SetStyle(NodeStyle.Operator);
		static LNode Range_Inclusive = LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "Range"), LNode.Id((Symbol) "Inclusive"))).SetStyle(NodeStyle.Operator);
		static LNode Range_StartingAt = LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "Range"), LNode.Id((Symbol) "StartingAt"))).SetStyle(NodeStyle.Operator);
		static LNode Range_UntilInclusive = LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "Range"), LNode.Id((Symbol) "UntilInclusive"))).SetStyle(NodeStyle.Operator);
		static LNode Range_UntilExclusive = LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "Range"), LNode.Id((Symbol) "UntilExclusive"))).SetStyle(NodeStyle.Operator);
		static LNode Range_Everything = LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "Range"), LNode.Id((Symbol) "Everything"))).SetStyle(NodeStyle.Operator));

		[LexicalMacro("lo..hi; ..hi; lo.._", "Given `lo..hi, produces `Range.Excl(lo, hi)", "'..")] 
		public static LNode RangeExcl(LNode node, IMacroContext context)
		{
			LNode lo = null;
			{
				LNode hi;
				if (node.Args.Count == 2 && (lo = node.Args[0]) != null && (hi = node.Args[1]) != null || node.Args.Count == 1 && (hi = node.Args[0]) != null) {
					if (lo == null || lo.IsIdNamed(__))
						if (hi.IsIdNamed(__))
							return Range_Everything;
						else
							return LNode.Call(Range_UntilExclusive, LNode.List(hi));
					else if (hi.IsIdNamed(__))
						return LNode.Call(Range_StartingAt, LNode.List(lo));
					else
						return LNode.Call(Range_ExcludeHi, LNode.List(lo, hi));
				}
			}
			return null;
		}

		[LexicalMacro("lo..hi; ..hi; lo.._", "Given `lo..hi, produces `Range.Excl(lo, hi)", "'...")] 
		public static LNode RangeIncl(LNode node, IMacroContext context)
		{
			LNode lo = null;
			{
				LNode hi;
				if (node.Args.Count == 2 && (lo = node.Args[0]) != null && (hi = node.Args[1]) != null || node.Args.Count == 1 && (hi = node.Args[0]) != null) {
					if (lo == null || lo.IsIdNamed(__))
						if (hi.IsIdNamed(__))
							return Range_Everything;
						else
							return LNode.Call(Range_UntilInclusive, LNode.List(hi));
					else if (hi.IsIdNamed(__))
						return LNode.Call(Range_StartingAt, LNode.List(lo));
					else
						return LNode.Call(Range_Inclusive, LNode.List(lo, hi));
				}
			}
			return null;
		}
	}
}