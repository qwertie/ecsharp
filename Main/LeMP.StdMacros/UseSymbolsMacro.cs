using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax;
using Loyc.Ecs;

namespace LeMP
{
	using S = CodeSymbols;

	partial class StandardMacros
	{
		[LexicalMacro("use_symbols; ... @@Foo ...", 
			"Replaces each @@symbol in the code that follows with a static readonly variable named sy_X for each symbol @@X. "
			+"The #useSymbols macro must be invoked inside a type definition where static variables are allowed.",
			"#useSymbols", "use_symbols" /*old name*/)]
		public static LNode useSymbols(LNode input, IMacroContext context)
		{
			var args_body = context.GetArgsAndBody(true);

			// Decode options (TODO: invent a simpler approach)
			string prefix = "sy_";
			var inherited = new HashSet<Symbol>();
			foreach (var pair in MacroContext.GetOptions(args_body.A))
			{
				if (pair.Key.Name == "prefix" && pair.Value.IsId)
					prefix = pair.Value.Name.Name;
				else if (pair.Key.Name == "inherit" && pair.Value.Value is Symbol)
					inherited.Add((Symbol)pair.Value.Value);
				else if (pair.Key.Name == "inherit" && (pair.Value.Calls(S.Braces) || pair.Value.Calls(S.Tuple)) && pair.Value.Args.All(n => n.Value is Symbol))
					foreach (var arg in pair.Value.Args)
						inherited.Add((Symbol)arg.Value);
				else
					context.Sink.Write(Severity.Warning, pair.Value, "Unrecognized parameter. Expected prefix:id or inherit:{@@A; @@B; ...})");
			}

			// Replace all symbols while collecting a list of them
			var symbols = new Dictionary<Symbol, LNode>();
			VList<LNode> output = args_body.B.SmartSelect(stmt => stmt.ReplaceRecursive(n => {
				var sym = n.Value as Symbol;
				if (n.IsLiteral && sym != null)
					return symbols[sym] = LNode.Id(prefix + sym.Name);
				return null;
			}));

			// Return updated code with variable declaration at the top for all non-inherit symbols used.
			var _Symbol = F.Id("Symbol");
			var vars = (from sym in symbols
			            where !inherited.Contains(sym.Key)
			            select F.Call(S.Assign, sym.Value, 
			                   F.Call(S.Cast, F.Literal(sym.Key.Name), _Symbol))).ToList();
			if (vars.Count > 0)
				output.Insert(0, F.Call(S.Var, ListExt.Single(_Symbol).Concat(vars))
					.WithAttrs(input.Attrs.Add(F.Id(S.Static)).Add(F.Id(S.Readonly))));
			return F.Call(S.Splice, output);
		}
	}
}
