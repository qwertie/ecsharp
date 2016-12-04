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
		[LexicalMacro("#useSymbols; ... @@Foo ...", 
			"Enables @@symbols to be used in the code that follows. A static readonly variable named sy_X will be created for each symbol @@X. "
			+"The #useSymbols macro can be invoked at global scope, or inside a type definition where static variables are allowed. Cannot be used inside a method.",
			"#useSymbols", "use_symbols" /*old name*/, Mode = MacroMode.NoReprocessing | MacroMode.MatchIdentifier)]
		public static LNode useSymbols(LNode input, IMacroContext context)
		{
			bool inType = context.Ancestors.Any(parent => {
				var kind = EcsValidators.SpaceDefinitionKind(parent);
				return kind != null && kind != S.Namespace;
			});
			var args_body = context.GetArgsAndBody(true);
			args_body.B = context.PreProcess(args_body.B);
			return UseSymbolsCore(input.Attrs, args_body.A, args_body.B, context, inType);
		}

		public static LNode UseSymbolsCore(VList<LNode> symbolAttrs, VList<LNode> options, VList<LNode> body, IMacroContext context, bool inType)
		{
			// Decode options (TODO: invent a simpler approach)
			string prefix = "sy_";
			var inherited = new HashSet<Symbol>();
			foreach (var pair in MacroContext.GetOptions(options))
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
			VList<LNode> output = body.SmartSelect(stmt => stmt.ReplaceRecursive(n => {
				if (!inType && n.ArgCount == 3) {
					// Since we're outside any type, we must avoid creating symbol 
					// fields. When we cross into a type then we can start making
					// Symbols by calling ourself recursively with inType=true
					var kind = EcsValidators.SpaceDefinitionKind(n);
					if (kind == S.Class || kind == S.Struct || kind == S.Interface || kind == S.Alias || kind == S.Trait) {
						var body2 = n.Args[2];
						return n.WithArgChanged(2, UseSymbolsCore(symbolAttrs, options, body2.Args, context, true).WithName(body2.Name));
					}
				}
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
					.WithAttrs(symbolAttrs.Add(F.Id(S.Static)).Add(F.Id(S.Readonly))));
			return F.Call(S.Splice, output);
		}
	}
}
