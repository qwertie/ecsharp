using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Syntax;
using Loyc.Collections;
using System.Diagnostics;
using Loyc.Syntax.Lexing;
using Loyc.Collections.Impl;
using Loyc.Ecs;
using S = Loyc.Syntax.CodeSymbols;

namespace LeMP
{
	public partial class StandardMacros
	{
		[LexicalMacro(@"replace (input($capture) => output($capture), ...) {...}",
			"Finds one or more patterns in a block of code and replaces each matching expression with another expression. "+
			"The braces are omitted from the output (and are not matchable). "+
			"This macro can be used without braces, in which case it affects all the statements/arguments that follow it in the current statement or argument list. "+
			"The alternate name `replacePP` additionally preprocesses the input and output arguments, and is useful to get around problems with macro execution order. "+
			"This behavior is not the default, since the final output will be macro-processed a second time.",
			"replace", "#replace", "replacePP")]
		public static LNode replace(LNode node, IMacroContext context)
		{
			var args_body = context.GetArgsAndBody(true);
			var args = args_body.A;
			var body = args_body.B;
			if (args.Count == 1 && args[0].Calls(S.Tuple)) args = args[0].Args; // LESv2
			if (args.Count >= 1)
			{
				bool preprocess = node.Calls("replacePP");

				var patterns = new Pair<LNode, LNode>[args.Count];
				for (int i = 0; i < patterns.Length; i++)
				{
					var pair = args[i];
					if (pair.Calls(S.Lambda, 2)) {
						LNode pattern = pair[0], repl = pair[1];
						if (preprocess)
						{
							pattern = context.PreProcess(pattern);
							repl = context.PreProcess(repl);
						}
						if (pattern.Calls(S.Braces)) {
							if (pattern.ArgCount == 1)
								pattern = pattern.Args[0];
							else
								context.Write(Severity.Error, pattern, "The braces must contain only a single statement. To search for braces literally, use `{{ ... }}`");
						}
						if (repl.Calls(S.Braces))
							repl = repl.Args.AsLNode(S.Splice);
						
						// Avoid StackOverflowException when pattern is $Id (sadly, it
						// is uncatchable so it can crash LeMP.exe and even Visual Studio)
						if (LNodeExt.GetCaptureIdentifier(pattern) != null)
 							return Reject(context, pattern, "The left side of `=>` cannot be a capture. Remove the `$`.");

						patterns[i] = Pair.Create(pattern, repl);
					} else {
						string msg = "Expected 'pattern => replacement'.";
						if (pair.Descendants().Any(n => n.Calls(S.Lambda, 2)))
							msg += " " + "(Using '=>' already? Put the pattern on the left-hand side in parentheses.)";
						return Reject(context, pair, msg);
					}
				}

				int replacementCount;
				var output = Replace(body, patterns, out replacementCount);
				if (replacementCount == 0)
					context.Write(Severity.Warning, node, "No patterns recognized; no replacements were made.");
				return output.AsLNode(S.Splice);
			}
			return null;
		}

		[ThreadStatic]
		static InternalList<Triplet<Symbol, LNode, int>> _tokenTreeRepls;

		/// <summary>Searches a list of expressions/statements for one or more 
		/// patterns, and performs replacements.</summary>
		/// <param name="stmts">A list of expressions/statements in which to search.</param>
		/// <param name="patterns">Each pair consists of (A) something to search 
		/// for and (B) a replacement expression. Part A can use the substitution
		/// operator with an identifier inside (e.g. $Foo) to "capture" any 
		/// subexpression, and part B can use the same substitution (e.g. $Foo)
		/// to insert the captured subexpression(s) into the output.</param>
		/// <param name="replacementCount">Number of replacements that occurred.</param>
		/// <returns>The result of applying the replacements.</returns>
		/// <remarks><see cref="LNodeExt.MatchesPattern"/> is used for matching.</remarks>
		public static VList<LNode> Replace(VList<LNode> stmts, Pair<LNode, LNode>[] patterns, out int replacementCount)
		{
			// This list is used to support simple token replacement in TokenTrees
			_tokenTreeRepls = InternalList<Triplet<Symbol, LNode, int>>.Empty;
			foreach (var pair in patterns) // Look for Id => Id or Id => Literal
				if (pair.A.IsId && (pair.B.IsId || pair.B.IsLiteral))
					_tokenTreeRepls.Add(new Triplet<Symbol,LNode,int>(pair.A.Name, pair.B, 0));

			// Scan the syntax tree for things to replace...
			int count = 0;
			var temp = new MMap<Symbol, LNode>();
			var output = stmts.SmartSelect(stmt => stmt.ReplaceRecursive(n => {
				LNode r = TryReplaceHere(n, patterns, temp);
				if (r != null) count++;
				return r;
			}));
			replacementCount = count;
			return output;
		}
		public static LNode Replace(LNode stmt, Pair<LNode, LNode>[] patterns, out int replacementCount)
		{
			CheckParam.IsNotNull("stmt", stmt);
			return Replace(new VList<LNode>(stmt), patterns, out replacementCount)[0];
		}

		static LNode TryReplaceHere(LNode node, Pair<LNode, LNode>[] patterns, MMap<Symbol, LNode> temp)
		{
			for (int i = 0; i < patterns.Length; i++)
			{
				temp.Clear();
				LNode r = TryReplaceHere(node, patterns[i].A, patterns[i].B, temp, patterns);
				if (r != null) return r;
			}

			// Support simple token replacement in TokenTrees
			TokenTree tt;
			if (node.IsLiteral && (tt = node.Value as TokenTree) != null) {
				bool modified = ReplaceInTokenTree(ref tt, _tokenTreeRepls);
				if (modified)
					return node.WithValue(tt);
			}
	
			return null;
		}
		static LNode TryReplaceHere(LNode node, LNode pattern, LNode replacement, MMap<Symbol, LNode> captures, Pair<LNode, LNode>[] allPatterns)
		{
			VList<LNode> attrs;
			if (LNodeExt.MatchesPattern(node, pattern, ref captures, out attrs)) {
				foreach (var pair in captures) {
					var input = pair.Value.AsList(S.Splice);
					int c;
					var output = Replace(input, allPatterns, out c);
					if (output != input)
						captures[pair.Key] = output.AsLNode(S.Splice);
				}
				return ReplaceCaptures(replacement, captures).PlusAttrs(attrs);
			}

			return null;
		}
		public static LNode ReplaceCaptures(LNode replacement, MMap<Symbol, LNode> captures)
		{
			if (captures.Count != 0)
			{
				// TODO: EXPAND SPLICES! Generally it works anyway though because 
				// the macro processor has built-in support for #splice.
				return replacement.ReplaceRecursive(n => {
					LNode sub, cap;
					if (n.Calls(S.Substitute, 1) && (sub = n.Args.Last).IsId && captures.TryGetValue(sub.Name, out cap))
						return cap;
					return null;
				});
			}
			return replacement;
		}

		static readonly Symbol _replace = (Symbol)"replace";
		static readonly Symbol _define = (Symbol)"define";

		[LexicalMacro(@"define Name($arg1, $arg2, ...) {...}; define Name($arg1, $arg2, ...) => ...",
			"Defines a local macro with the specified name that matches the specified patterns and is replaced with the output code within the braces. " +
			"This works differently than the replace(...) macro: it doesn't perform a find-and-replace operation; instead it creates a macro that the macro processor can match later. " +
			"In some cases this macro is more efficient than replace(...). " +
			"The macro's arguments can be patterns; for example `replace Foo($x = $y) {...}` would match `Foo(Bar = Math.Abs(-123))`.", 
			"#fn", Mode = MacroMode.Passive)]
		public static LNode replaceFn(LNode node, IMacroContext context1)
		{
			var retType = node.Args[0, LNode.Missing].Name;
			if (retType != _replace && retType != _define)
				return null;
			LNode replaceKw, macroName, args, body;
			if (EcsValidators.MethodDefinitionKind(node, out replaceKw, out macroName, out args, out body, allowDelegate: false) != S.Fn || body == null)
				return null;

			MacroMode mode, modes = 0;
			var leftoverAttrs = node.Attrs.SmartWhere(attr =>
			{
				if (attr.IsId && Loyc.Compatibility.EnumStatic.TryParse(attr.Name.Name, out mode))
				{
					modes |= mode;
					return false;
				}
				return true;
			});

			LNode pattern = F.Call(macroName, args.Args).PlusAttrs(leftoverAttrs);
			LNode replacement = body.AsList(S.Braces).AsLNode(S.Splice).PlusAttrs(replaceKw.Attrs);
			replacement.Style &= ~NodeStyle.OneLiner;

			WarnAboutMissingDollarSigns(args, context1, pattern, replacement);

			// Note: we could fill out the macro's Syntax and Description with the 
			// pattern and replacement converted to strings, but it's generally a 
			// waste of CPU time as those strings are usually not requested.
			var lma = new LexicalMacroAttribute(
				string.Concat(macroName.Name, "(", args.Args.Count.ToString(), " args)"), "", macroName.Name.Name);
			var macroInfo = new MacroInfo(null, lma, (candidate, context2) =>
			{
				MMap<Symbol, LNode> captures = new MMap<Symbol, LNode>();
				VList<LNode> unmatchedAttrs;
				if (candidate.MatchesPattern(pattern, ref captures, out unmatchedAttrs))
				{
					return ReplaceCaptures(replacement, captures).PlusAttrsBefore(unmatchedAttrs);
				}
				return null;
			}) {
				Mode = modes
			};
			context1.RegisterMacro(macroInfo);
			return F.Splice();
		}

		private static void WarnAboutMissingDollarSigns(LNode argList, IMacroContext context, LNode pattern, LNode replacement)
		{
			// Warn if a name appears in both pattern and replacement but uses $ in only one of the two.
			Dictionary<Symbol, LNode> pVars = ScanForVariables(pattern), rVars = ScanForVariables(replacement);
			// Also warn if it looks like all `$`s were forgotten.
			bool allIds = argList.Args.Count > 0 && argList.Args.All(n => !n.IsCall);
			foreach (var pair in pVars)
			{
				LNode rVar = rVars.TryGetValue(pair.Key, null);
				if (pair.Value.IsId) // id without `$` in pattern list
				{
					if (rVar != null && (allIds || !rVar.IsId))
						context.Write(Severity.Warning, pair.Value, "`{0}` is written without `$`, so it may not match as intended.", pair.Value.Name);
				}
				else // $id in pattern list
				{
					if (rVar != null && rVar.IsId)
						context.Write(Severity.Warning, rVar, "`{0}` appears in the output without `$` so replacement will not occur.", pair.Key);
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
					else {
						LNode id = LNodeExt.GetCaptureIdentifier(n);
						if (id != null) {
							if (!nameTable.ContainsKey(n.Name))
								nameTable[id.Name] = n;
							return n;
						}
					}
					return null;
				});
			return nameTable;
		}
	}
}
