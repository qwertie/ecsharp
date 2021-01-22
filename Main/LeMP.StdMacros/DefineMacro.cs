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
		static readonly Symbol _replace = (Symbol)"replace";
		static readonly Symbol _define = (Symbol)"define";

		[LexicalMacro(@"define (_pattern_) { body; }",
			"Defines a local macro that matches the specified pattern and is replaced with another pattern from within the braces. " +
			"This works differently than the replace(...) macro: it doesn't perform a find-and-replace operation; instead it creates a macro that the macro processor can use later. " +
			"Example: `define ($x = $y + $z) { $x = Add($y, $z); }` would replace an assignment, involving adding two things, with an assignment that involves calling a method called `Add()`. " +
			"MacroMode enum values can be attached as attributes. For example, the [PriorityOverride] attribute will cause this macro to supercede other macros that use default priority." +
			"Inside the body, identifiers are searched for the substring `unique#`, which is replaced with a fresh number each time the macro is invoked." +
			"Similarly, variables whose name starts with `temp#` are uniquely numbered.",
			"define", "#define")]
		public static LNode define(LNode node, IMacroContext context)
		{
			if (node.Args.Count != 2)
				return Reject(context, node, "Expected one macro pattern and one body.");
			LNode pattern = node.Args[0].UnwrapBraces(), body = node.Args[1];
			if (!body.Calls(S.Braces))
				return Reject(context, body, "Expected a braced block containing a replacement pattern to emit when the pattern on the left is matched.");

			return RegisterSimpleMacro(node.Attrs, pattern, body, context);
		}

		[LexicalMacro(@"define Name($arg1, $arg2, ...) { body; }; define Name($arg1, $arg2, ...) => body; // EC# syntax",
			"Defines a local macro with the specified name that matches the specified patterns and is replaced with the output code within the braces. " +
			"This works differently than the replace(...) macro: it doesn't perform a find-and-replace operation; instead it creates a macro that the macro processor can match later. " +
			"The macro's arguments can be patterns; for example `define Foo($x = $y) {...}` would match `Foo(Bar = Math.Abs(-123))`. " +
			"MacroMode enum values can be attached as attributes. For example, the [PriorityOverride] attribute will cause this macro to supercede other macros that use default priority." +
			"Inside the body, identifiers are searched for the substring `unique#`, which is replaced with a fresh number each time the macro is invoked." +
			"Similarly, variables whose name starts with `temp#` are uniquely numbered.",
			"#fn", Mode = MacroMode.Passive)]
		public static LNode methodStyleDefine(LNode node, IMacroContext context)
		{
			if (EcsValidators.MethodDefinitionKind(node, out var defineKw, out var macroName, out var args, out var body) == S.Fn && body != null && defineKw.IsIdNamed("define"))
			{
				var pattern = args.WithTarget(macroName);
				return RegisterSimpleMacro(node.Attrs, pattern, body, context);
			}
			return null;
		}

		[LexicalMacro(@"define Name {...} // EC# syntax",
			"Defines a local macro with the specified name that causes the specified identifier to be replaced with the specified syntax tree." +
			"This works differently than the replace(...) macro: it doesn't perform a find-and-replace operation; instead it creates a macro that the macro processor can match later. ",
			"#property", Mode = MacroMode.Passive)]
		public static LNode defineId(LNode node, IMacroContext context)
		{
			if (node.Args[0, LNode.Missing].Name != _define)
				return null;
			LNode defineKw, macroId, args, body, initialValue;
			if (!EcsValidators.IsPropertyDefinition(node, out defineKw, out macroId, out args, out body, out initialValue) 
				|| body == null || args.ArgCount != 0 || initialValue != null)
				return null;

			return RegisterSimpleMacro(node.Attrs, macroId, body, context);
		}

		internal static MacroMode GetMacroMode(ref LNodeList attrs, LNode pattern)
		{
			MacroMode modes = 0;	
			attrs = attrs.SmartWhere(attr =>
			{
				if (attr.IsId && Enum.TryParse(attr.Name.Name, out MacroMode mode))
				{
					modes |= mode;
					return false;
				}
				return true;
			});

			if (pattern.IsLiteral)
				modes |= MacroMode.MatchEveryLiteral;
			else if (pattern.IsId)
				modes |= MacroMode.MatchIdentifierOnly;
			else if (DecodeSubstitutionExpr(pattern, out _, out _, out _) != null)
				modes |= MacroMode.MatchEveryCall | MacroMode.MatchEveryIdentifier | MacroMode.MatchEveryLiteral;
			else if (!pattern.Target.IsId)
				modes |= MacroMode.MatchEveryCall; // custom matching code needed
			return modes;
		}

		private static LNode RegisterSimpleMacro(LNodeList attrs, LNode pattern, LNode body, IMacroContext context)
		{
			if (DecodeSubstitutionExpr(pattern, out _, out _, out _) != null)
				return Reject(context, pattern, "Defining a macro that could match everything is not allowed.");

			MacroMode modes = GetMacroMode(ref attrs, pattern);

			LNode macroName = pattern.Target ?? pattern;
			LNode replacement = body.AsList(S.Braces).AsLNode(S.Splice);

			if (pattern.IsCall)
				WarnAboutMissingDollarSigns(pattern.Args, context, pattern, replacement);

			// Note: we could fill out the macro's Syntax and Description with the 
			// pattern and replacement converted to strings, but it's generally a 
			// waste of CPU time as those strings are usually not requested. 
			// Compromise: provide syntax pattern only
			var syntax = pattern.ToString();
			var lma = new LexicalMacroAttribute(syntax, "User-defined macro at {0}".Localized(pattern.Range.Start), macroName.Name.Name) { Mode = modes };
			if ((modes & (MacroMode.MatchEveryLiteral | MacroMode.MatchEveryCall | MacroMode.MatchEveryIdentifier)) != 0)
				lma = new LexicalMacroAttribute(syntax, lma.Description) { Mode = modes };

			var macroInfo = new MacroInfo(null, lma, UserDefinedMacro);
			macroInfo.Mode |= MacroMode.UseLogicalNameInErrorMessages;

			context.RegisterMacro(macroInfo);
			
			return F.Splice(); // delete the `define` node from the output

			LNode UserDefinedMacro(LNode candidate, IMacroContext context2)
			{
				MMap<Symbol, LNode> captures = new MMap<Symbol, LNode>();
				if (candidate.MatchesPattern(pattern, ref captures, out LNodeList unmatchedAttrs))
				{
					LNode replacement2 = WithUniqueIdentifiers(replacement, context.IncrementTempCounter, out _);
					return ReplaceCaptures(replacement2, captures).PlusAttrsBefore(unmatchedAttrs);
				}
				return null;
			}
		}

		private static void WarnAboutMissingDollarSigns(LNodeList argList, IMacroContext context, LNode pattern, LNode replacement)
		{
			// Warn if a name appears in both pattern and replacement but uses $ in only one of the two.
			Dictionary<Symbol, LNode> pVars = ScanForVariables(pattern), rVars = ScanForVariables(replacement);
			// Also warn if it looks like all `$`s were forgotten.
			bool allIds = argList.Count > 0 && argList.All(n => !n.IsCall);
			foreach (var pair in pVars)
			{
				LNode rVar = rVars.TryGetValue(pair.Key, null);
				if (pair.Value.IsId) // id without `$` in pattern list
				{
					if (rVar != null && (allIds || !rVar.IsId))
						context.Sink.Warning(pair.Value, "`{0}` is written without `$`, so it may not match as intended.", pair.Value.Name);
				}
				else // $id in pattern list
				{
					if (rVar != null && rVar.IsId)
						context.Sink.Warning(rVar, "`{0}` appears in the output without `$` so replacement will not occur.", pair.Key);
				}
			}
		}

		private static Dictionary<Symbol, LNode> ScanForVariables(LNode code)
		{
			var nameTable = new Dictionary<Symbol, LNode>();
			code.ReplaceRecursive(n =>
			{
				if (n.IsId)
					nameTable[n.Name] = n;
				else
				{
					LNode id = LNodeExt.GetCaptureIdentifier(n);
					if (id != null)
					{
						if (!nameTable.ContainsKey(n.Name))
							nameTable[id.Name] = n;
						return n;
					}
				}
				return null;
			});
			return nameTable;
		}

		/// <summary>Finds identifiers containing the string "unique#" and replaces that 
		/// string with a unique integer from nextTempCounter(). Also finds names that 
		/// start with "temp#" and replaces # with a unique integer.</summary>
		/// <param name="mappings">The identifier mappings produced, or null if there were none</param>
		public static LNode WithUniqueIdentifiers(LNode code, Func<int> nextTempCounter, out Dictionary<Symbol, Symbol> mappings)
		{
			Dictionary<Symbol, Symbol> mappings_ = null;
			var result = code.ReplaceRecursive(node => {
				var name = node.Name.Name;
				if (node.IsId) {
					if (name.StartsWith("temp#"))
						name = "tempunique#" + name.Substring("temp#".Length);
					int i = name.IndexOf("unique#");
					if (i > -1) {
						mappings_ = mappings_ ?? new Dictionary<Symbol, Symbol>();
						if (!mappings_.TryGetValue(node.Name, out Symbol newName))
							mappings_[node.Name] = newName = (Symbol)(name.Left(i) + nextTempCounter() + name.Substring(i + "unique#".Length));
						return LNode.Id(newName, node);
					}
				}
				return null;
			});
			mappings = mappings_;
			return result;
		}
	}
}
