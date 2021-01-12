// Generated from AssertMacro.ecs by LeMP custom tool. LeMP version: 2.9.0.1
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
		// Finds the method/property/constructor/event in which a macro was called,
		// and also the class/struct/interface/alias or, if not found, namespace.
		static Symbol GetFnAndClassName(IMacroContext context, out LNode @class, out LNode fn)
		{
			@class = fn = null;
			var anc = context.Ancestors;	// scan these to find function/property and class/struct/etc.
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
			var ps = LNode.Printer;
			if (fn == null)
				return @class == null ? null : ps.Print(@class, MessageSink.Null, ParsingMode.Expressions);
			else if (@class == null)
				return ps.Print(fn, MessageSink.Null, ParsingMode.Expressions);
			else {
				while (fn.CallsMin(S.Dot, 2))
					fn = fn.Args.Last;
				return string.Format("{0}.{1}", ps.Print(@class, MessageSink.Null, ParsingMode.Expressions), 
				ps.Print(fn, MessageSink.Null, ParsingMode.Expressions));
			}
		}

		static readonly LNode defaultAssertMethod = LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "System"), LNode.Id((Symbol) "Diagnostics"))).SetStyle(NodeStyle.Operator), LNode.Id((Symbol) "Debug"))).SetStyle(NodeStyle.Operator), LNode.Id((Symbol) "Assert"))).SetStyle(NodeStyle.Operator);

		internal static LNode GetAssertMethod(IMacroContext context)
		{
			return (context.ScopedProperties.TryGetValue(sy__numassertMethod, null) as LNode) ?? defaultAssertMethod;
		}

		[LexicalMacro(@"assert(condition);", 
		"Translates assert(expr) to System.Diagnostics.Debug.Assert(expr, \"Assertion failed in Class.MethodName: expr\"). " 
		+ "You can change the assert method with `#snippet` as follows:\n\n" 
		+ "    #snippet #assertMethod = System.Diagnostics.Debug.Assert; // default", 
		"assert")] 
		public static LNode _assert(LNode node, IMacroContext context)
		{
			if (node.ArgCount > 0) {
				var results = LNode.List();
				foreach (var condition in node.Args) {
					string name = GetFnAndClassNameString(context) ?? "";
					var ps = LNode.Printer;
					LNode condStr = F.Literal(string.Format("Assertion failed in `{0}`: {1}", 
					name, ps.Print(condition, context.Sink, ParsingMode.Expressions)));

					var assertFn = GetAssertMethod(context);
					if (assertFn.IsIdNamed(node.Name))
						return null;	// disabled!
					results.Add(LNode.Call(assertFn, LNode.List(condition, condStr)));
				}
				return results.AsLNode(S.Splice);
			}
			return null;
		}
	}
}