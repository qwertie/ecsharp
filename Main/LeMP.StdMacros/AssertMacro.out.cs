// Generated from AssertMacro.ecs by LeMP custom tool. LeMP version: 1.7.3.0
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
		static readonly Symbol sy__numassertMethod = (Symbol) "#assertMethod";
		static Symbol GetFnAndClassName(IMacroContext context, out LNode @class, out LNode fn)
		{
			@class = fn = null;
			var anc = context.Ancestors;
			for (int i = anc.Count - 1; i >= 0; i--) {
				var name = anc[i].Name;
				if (anc[i].ArgCount >= 2) {
					if (fn == null) {
						if (name == S.Fn || name == S.Property || name == S.Constructor || name == S.Event)
							fn = anc[i][1];
					}
					if (name == S.Struct || name == S.Class || name == S.Namespace || name == S.Interface || name == S.Trait || name == S.Alias) {
						@class = anc[i][0];
						return name;
					}
				}
			}
			return null;
		}
		static string GetFnAndClassNameString(IMacroContext context)
		{
			LNode @class, fn;
			GetFnAndClassName(context, out @class, out fn);
			var ps = ParsingService.Current;
			if (fn == null)
				return @class == null ? null : ps.Print(@class, null, ParsingMode.Exprs);
			else if (@class == null)
				return ps.Print(fn, null, ParsingMode.Exprs);
			else {
				while (fn.CallsMin(S.Dot, 2))
					fn = fn.Args.Last;
				return string.Format("{0}.{1}", ps.Print(@class, null, ParsingMode.Exprs), ps.Print(fn, null, ParsingMode.Exprs));
			}
		}
		static readonly LNode defaultAssertMethod = LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "System"), LNode.Id((Symbol) "Diagnostics"))), LNode.Id((Symbol) "Debug"))), LNode.Id((Symbol) "Assert")));
		internal static LNode GetAssertMethod(IMacroContext context)
		{
			return (context.ScopedProperties.TryGetValue(sy__numassertMethod, null)as LNode) ?? defaultAssertMethod;
		}
		[LexicalMacro("assert(condition);", "Translates assert(expr) to System.Diagnostics.Debug.Assert(expr, \"Assertion failed in Class.MethodName: expr\"). " + "You can change the assert method with `#snippet` as follows:\n\n" + "    #snippet #assertMethod = System.Diagnostics.Debug.Assert; // default", "assert")]
		public static LNode _assert(LNode node, IMacroContext context)
		{
			if (node.ArgCount > 0) {
				var results = LNode.List();
				foreach (var condition in node.Args) {
					string name = GetFnAndClassNameString(context) ?? "";
					var ps = ParsingService.Current;
					LNode condStr = F.Literal(string.Format("Assertion failed in `{0}`: {1}", name, ps.Print(condition, context.Sink, ParsingMode.Exprs)));
					var assertFn = GetAssertMethod(context);
					if (assertFn.IsIdNamed(node.Name))
						return null;
					results.Add(LNode.Call(assertFn, LNode.List(condition, condStr)));
				}
				return results.AsLNode(S.Splice);
			}
			return null;
		}
	}
}
