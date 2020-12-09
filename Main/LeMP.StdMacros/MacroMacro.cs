using Loyc;
using Loyc.Collections;
using Loyc.Ecs;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using S = Loyc.Syntax.CodeSymbols;

namespace LeMP
{
	partial class StandardMacros
	{
		[LexicalMacro("macro (pattern) { /* C# macro method body */ }",
			"Compiles and registers a macro that recognizes the specified syntactical pattern and " +
			"optionally produces a replacement pattern. LeMP will preprocess the method body before " +
			"giving it to the C# scripting engine, so LeMP macros can be used inside the method body. " +
			"The method body has access to two parameter variables (LNode node, IMacroContext context) " +
			"and it can change the matched code by returning new code. " +
			"MacroMode enum values can be attached as attributes. For example, the [PriorityOverride] attribute will cause this macro to supercede other macros that use default priority. " +
			"Example: macro ($lhs += 1) { return quote($lhs++); }"
			)]
		public static LNode macro(LNode node, IMacroContext context)
		{
			if (node.Args.Count != 2)
				return Reject(context, node, "Expected one macro pattern and one body.");
			LNode pattern = node.Args[0], body = node.Args[1];
			if (!body.Calls(S.Braces))
				return Reject(context, body, "Expected a braced block containing C# code to run when the pattern on the left is matched.");

			return CompileMacro(pattern, body, context, node.Attrs);
		}

		[LexicalMacro(@"macro Name($arg1, $arg2, ...) { /* C# macro method body */ }; macro Name($arg1, $arg2, ...) => /* C# expression */;",
			"Compiles and registers a macro that recognizes the specified syntactical pattern and " +
			"optionally produces a replacement pattern. LeMP will preprocess the method body before " +
			"giving it to the C# scripting engine, so LeMP macros can be used inside the method body. " +
			"The method body has access to two parameter variables (LNode node, IMacroContext context) " +
			"and it can change the matched code by returning new code. " +
			"MacroMode enum values can be attached as attributes. For example, the [PriorityOverride] attribute will cause this macro to supercede other macros that use default priority. " +
			"Example: macro ExprToString($expr) { return LNode.Literal(expr.ToString()); }",
			"#fn", Mode = MacroMode.Passive)]
		public static LNode methodStyleMacro(LNode node, IMacroContext context)
		{
			if (EcsValidators.MethodDefinitionKind(node, out var defineKw, out var macroName, out var args, out var body) == S.Fn && body != null && defineKw.IsIdNamed("macro"))
			{
				var pattern = args.WithTarget(macroName);
				return CompileMacro(pattern, body, context, node.Attrs);
			}
			return null;
		}

		public static LNode CompileMacro(LNode pattern, LNode body, IMacroContext context, LNodeList attrs)
		{
			MacroMode modes = GetMacroMode(ref attrs, pattern);

			LNode macroName = (pattern.Target ?? pattern).PlusAttrs(attrs);
			//var macroName = 
			return null;
		}
	}
}
