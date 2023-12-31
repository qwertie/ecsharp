// Generated from MatchCode.ecs by LeMP custom tool. LeMP version: 30.1.91.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		[LexicalMacro("matches(expr, pattern, ...)", 
		"Attempts to match and deconstruct a Loyc tree against one or more code patterns at " + 
		"runtime. For example, `matches(quote(Sqrt(9) + 3*4), $a + $b)` will return true " + 
		"and create variables called `a` and `b` that contain the Loyc trees for `Sqrt(9)` and " + 
		"`3*4`. You can use `$(...x)` in the pattern to gather zero or more parameters into a " + 
		"list `x`. To re-use a pre-existing variable x in a pattern, use $(ref x) instead of " + 
		"$x. To match a variable conditionally in EC# or LES3, use `$(x when condition)`. For " + 
		"example, `$(x when x.IsId && x.AttrCount == 0)` will match only if x is a simple " + 
		"identifier with no attributes or trivia attached to it.\n\n" + 
		"You can specify multiple patterns. In that case, the input is compared with each " + 
		"pattern in order, the matching process stops at the first successful match, and " + 
		"the result is true if any of the patterns match.")] 
		public static LNode matches(LNode node, IMacroContext context)
		{
			if (node.ArgCount < 2)
				return null;

			var input = node[0];

			// If we're matching against something nontrivial like `matches(foo(x), ...)`,
			// create a temporary variable to hold the result of `foo(x)`.
			LNode tempVarForInput = null;
			if (!LooksLikeSimpleValue(input)) {
				var tempVarId = LNode.Id(NextTempName(context, input), input);
				tempVarForInput = LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "LNode"), LNode.Id((Symbol) "Var"))).SetStyle(NodeStyle.Operator), LNode.List(LNode.Call(LNode.List(LNode.Id(CodeSymbols.Out)), CodeSymbols.Var, LNode.List(LNode.Missing, tempVarId)), input));
				input = tempVarId;
			}

			var cmc = new CodeMatchContext { 
				Context = context, IsMultiCase = node.ArgCount > 2
			};

			var patterns = node.Args.Skip(1).Select(UnwrapBraces);
			var tests = patterns.Aggregate((LNode) null, (tests_, pattern) => {
				var test = cmc.MakeTopTestExpr(pattern, input);
				return LNode.MergeBinary(tests_, test, S.Or);
			});

			return LNode.MergeBinary(tempVarForInput, tests, S.And);
		}

		[LexicalMacro("matchCode (var) { case ...: ... }; // In LES, use a => b instead of case a: b", 
		"Attempts to match and deconstruct a Loyc tree against a series of cases with patterns, e.g. " + 
		"`case $a + $b:` expects a tree that calls `+` with two parameters, placed in new variables called a and b. " + 
		"`break` is not required or recognized at the end of each case's handler (code block). " + 
		"Use `$(...x)` to gather zero or more parameters into a list `x`. " + 
		"Use `case pattern1, pattern2:` in EC# to handle multiple cases with the same handler.")] 
		public static LNode matchCode(LNode node, IMacroContext context)
		{
			if (node.AttrNamed(S.Static) != null)
				return null;	// this case is handled by static_matchCode macro
			// Expect a single arg (the variable being matched) and a body (braced block with cases)
			var args_body = context.GetArgsAndBody(false);
			LNodeList args = args_body.Item1, body = args_body.Item2;
			if (args.Count != 1 || body.Count < 1)
				return null;

			// Get a list of case blocks in the body. Each block has a list of patterns, and a 
			// list of statements to run if the pattern is matched.
			VList<(LNodeList Cases, LNodeList Handler)> blocks = GetCases(body, context.Sink);
			if (blocks.IsEmpty)
				return null;

			// If we're matching against something nontrivial like `matchCode(foo(x))`,
			// create a temporary variable to hold the result of `foo(x)`.
			var output = new WList<LNode>();
			var var = MaybeAddTempVarDecl(context, args[0], output);

			// `ifClauses` will hold the output code, with one pair per case block. The first 
			// item in each pair is the expression used to decide whether the input matches 
			// the pattern; the second is the user-defined code to run if the pattern matches.
			var ifClauses = new List<(LNode TestExpr, LNode Handler)>();

			// This is the workhorse of code generation, and it holds intermediate state.
			var cmc = new CodeMatchContext { 
				Context = context, DeclareVarsSeparately = true
			};

			foreach (var block in blocks)
			{
				// e.g. case [$(..._)] Foo($x + 1, $y) => 
				//      LNode x, y, tmp9; 
				//      if (var.Calls((Symbol) "Foo", 2) && (tmp9 = var.Args[0]).Calls(CodeSymbols.Plus, 2)
				//          && (x = tmp9.Args[0]) != null // this will never be null, but we want to put it the assignment in the 'if' statement
				//          && 1.Equals(tmp9.Args[1].Value) && (y = var.Args[1]) != null) { ... }
				LNode testExpr = null;
				if (block.Cases.Count > 0) {
					if (cmc.IsMultiCase = block.Cases.Count > 1) {
						cmc.UsageCounters.Clear();
						testExpr = block.Cases.Aggregate((LNode) null, (tests_, pattern) => {
							var test = cmc.MakeTopTestExpr(pattern, var);
							return LNode.MergeBinary(tests_, test, S.Or);
						});
						foreach (var pair in cmc.UsageCounters.Where(p => p.Value < block.Cases.Count)) {
							if (cmc.NodeVars.ContainsKey(pair.Key))
								cmc.NodeVars[pair.Key] = true;
							if (cmc.ListVars.ContainsKey(pair.Key))
								cmc.ListVars[pair.Key] = true;
						}
					} else
						testExpr = cmc.MakeTopTestExpr(block.Cases[0], var);
				}
				var handler = F.Braces(block.Handler);
				ifClauses.Add((testExpr, handler));
			}

			// Combine all the conditions and code blocks into a single if-else chain.
			LNode ifStmt = null;
			for (int i = ifClauses.Count - 1; i >= 0; i--)
			{
				if (ifClauses[i].TestExpr == null) {
					if (ifStmt == null)
						ifStmt = ifClauses[i].Handler;
					else
						context.Sink.Error(node, "The default case must appear last, and there can be only one.");
				} else {
					if (ifStmt == null)
						ifStmt = F.Call(S.If, ifClauses[i].TestExpr, ifClauses[i].Handler);
					else
						ifStmt = F.Call(S.If, ifClauses[i].TestExpr, ifClauses[i].Handler, ifStmt);
				}
			}

			if (cmc.NodeVars.Count > 0)
				output.Add(F.Call(S.Var, ListExt.Single(F.Id("LNode")).Concat(
				cmc.NodeVars.OrderBy(v => v.Key.Name).Select(kvp => kvp.Value ? F.Call(S.Assign, F.Id(kvp.Key), F.Null) : F.Id(kvp.Key)))));
			if (cmc.ListVars.Count > 0) {
				LNode type = LNode.Id((Symbol) "LNodeList");
				output.Add(F.Call(S.Var, ListExt.Single(type).Concat(
				cmc.ListVars.OrderBy(v => v.Key.Name).Select(kvp => kvp.Value ? LNode.Call(CodeSymbols.Assign, LNode.List(F.Id(kvp.Key), LNode.Call(CodeSymbols.Default, LNode.List(type)))).SetStyle(NodeStyle.Operator) : F.Id(kvp.Key)))));
			}
			if (output.Count == 0)
				return ifStmt.IncludingTriviaFrom(node);
			else {
				output.Add(ifStmt);
				return F.Braces(output.ToVList()).IncludingTriviaFrom(node);
			}
		}

		class CodeMatchContext
		{
			// For detecting when the same $var is used more than once
			HashSet<Symbol> DuplicateDetector = new HashSet<Symbol>();

			// UsageCounters will count how many of the patterns in a single 'case' (or the 
			// patterns given to 'matches') refer to each pattern variable, e.g. for 
			// `matches(x, $x, $x + $y)`, $y is used once and $x twice.
			public Dictionary<Symbol, int> UsageCounters = new Dictionary<Symbol, int>();
			// These dictionaries will have an entry for each variable (from a pattern) that needs 
			// to be declared. The boolean value in each of the two dictionaries will be set to 
			// false if the variable will be definitely initialized on all paths, and true if the 
			// variable needs to be initialized to a default value. In matchCode statements,
			// these dictionaries are for all the code blocks (not reset between blocks).
			public Dictionary<Symbol, bool> NodeVars = new Dictionary<Symbol, bool>();	// LNode vars to declare
			public Dictionary<Symbol, bool> ListVars = new Dictionary<Symbol, bool>();	// LNodeList vars to declare
			public IMacroContext Context;
			public bool IsMultiCase;	// true if building a case statement with multiple patterns
			public WList<LNode> Tests = new WList<LNode>();	// list of tests involved in matching the current pattern
			// The old style (used by matchCode) is to declare variables on separate lines, while 
			// the new style (used by the `matches`, which is newer) relies on `LNode.Var(out var...)`.
			// Someday we can remove `DeclareVarsSeparately` mode along with the separate var decls.
			public bool DeclareVarsSeparately = false;

			internal LNode MakeTopTestExpr(LNode pattern, LNode input)
			{
				DuplicateDetector.Clear();
				Tests.Clear();
				MakeTestExpr(pattern, input);

				LNode result = null;
				foreach (var test in Tests)
					result = LNode.MergeBinary(result, test, S.And);
				return result;
			}

			private void MakeTestExpr(LNode pattern, LNode candidate)	// Matches a top-level expr or a Target
			{
				MakeTestExpr(pattern, (Symbol varArgSym, bool hasCondition) => {
					if (varArgSym != null) {
						Context.Sink.Error(pattern, "A list cannot be matched in this context. Remove '...' or 'params'.");
						return null;
					}
					return candidate;
				});
			}
			private void MakeTestExpr(LNode pattern, Func<Symbol, bool, LNode> getCandidateSubtree)
			{
				// is this a $substitutionVar? (if yes, nodeVar != null)
				LNode condition;
				bool isParams, refExistingVar;
				var nodeVar = DecodeSubstitutionExpr(pattern, out condition, out isParams, out refExistingVar);

				var candidate = getCandidateSubtree(isParams ? nodeVar : null, condition != null);

				// Unless the candidate is a simple variable name, avoid repeating 
				// it by creating a temporary variable to hold its value
				int predictedTests = pattern.Attrs.Count + 
				(nodeVar != null ? 0 : pattern.Args.Count) + 
				(!pattern.HasSimpleHeadWithoutPAttrs() ? 1 : 0);
				if (predictedTests > 1)
					candidate = MaybePutCandidateInTempVar(candidate.IsCall, candidate);

				// Look for an attribute list of the form [$(...var)] in the pattern
				MatchAttributes(pattern, candidate);

				if (nodeVar != null) {
					// Handle $_, $normalName, $(name when ...), and $(...varArgName)
					if (nodeVar != __ || condition != null) {
						bool isNew = TryAddVar(nodeVar, isParams, refExistingVar);
						AddVarAssignmentOrComparisonTest(isNew, nodeVar, candidate, condition, isParams);
					}
				} else if (pattern.IsId) {
					Tests.Add(LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "IsIdNamed"))).SetStyle(NodeStyle.Operator), LNode.List(LNode.Call(CodeSymbols.Cast, LNode.List(F.Literal(pattern.Name.Name), LNode.Id((Symbol) "Symbol"))).SetStyle(NodeStyle.Operator))));
				} else if (pattern.IsLiteral) {
					if (pattern.Value == null)
						Tests.Add(LNode.Call(CodeSymbols.Eq, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Value"))).SetStyle(NodeStyle.Operator), LNode.Literal(null))).SetStyle(NodeStyle.Operator));
					else
						Tests.Add(LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(pattern, LNode.Id((Symbol) "Equals"))).SetStyle(NodeStyle.Operator), LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Value"))).SetStyle(NodeStyle.Operator))));
				} else {	// call(...)
					int? varArgAt;
					int fixedArgC = GetFixedArgCount(pattern.Args, out varArgAt);

					// Test if the call target matches
					var pTarget = pattern.Target;
					if (pTarget.IsId && !pTarget.HasPAttrs()) {
						var quoteTarget = QuoteSymbol(pTarget.Name);
						LNode targetTest;
						if (varArgAt.HasValue && fixedArgC == 0)
							targetTest = LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Calls"))).SetStyle(NodeStyle.Operator), LNode.List(quoteTarget));
						else if (varArgAt.HasValue)
							targetTest = LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "CallsMin"))).SetStyle(NodeStyle.Operator), LNode.List(quoteTarget, F.Literal(fixedArgC)));
						else
							targetTest = LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Calls"))).SetStyle(NodeStyle.Operator), LNode.List(quoteTarget, F.Literal(fixedArgC)));
						Tests.Add(targetTest);
					} else {
						if (fixedArgC == 0) {
							Tests.Add(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "IsCall"))).SetStyle(NodeStyle.Operator));
							if (!varArgAt.HasValue)
								Tests.Add(LNode.Call(CodeSymbols.Eq, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Args"))).SetStyle(NodeStyle.Operator), LNode.Id((Symbol) "Count"))).SetStyle(NodeStyle.Operator), LNode.Literal(0))).SetStyle(NodeStyle.Operator));
						} else {
							var op = varArgAt.HasValue ? S.GE : S.Eq;
							Tests.Add(LNode.Call(op, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Args"))).SetStyle(NodeStyle.Operator), LNode.Id((Symbol) "Count"))).SetStyle(NodeStyle.Operator), F.Literal(fixedArgC))));
						}
						int i = Tests.Count;
						MakeTestExpr(pTarget, LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Target"))).SetStyle(NodeStyle.Operator));
					}

					MakeArgListTests(pattern.Args, candidate);
				}
			}

			// Used for optimization, to avoid writing complicated expressions in the output:
			// e.g. instead of  code.Args[0].Target.Args.Count == 1 && code.Args[0].Target.Args[0].IsIdNamed((Symbol) "_")
			//   we might write (tmp_5 = code.Args[0].Target) != null && tmp_5.Args.Count == 1 && tmp_5.Args[0].IsIdNamed((Symbol) "_")
			LNode MaybePutCandidateInTempVar(bool condition, LNode candidate)
			{
				if (condition) {
					var targetTmp = NextTempName(Context);
					var targetTmpId = F.Id(targetTmp);
					G.Verify(TryAddVar(targetTmp, false));
					if (DeclareVarsSeparately)
						Tests.Add(LNode.Call(CodeSymbols.NotEq, LNode.List(LNode.Call(LNode.List(LNode.InParensTrivia), CodeSymbols.Assign, LNode.List(targetTmpId, candidate)).SetStyle(NodeStyle.Operator), LNode.Literal(null))).SetStyle(NodeStyle.Operator));
					else
						Tests.Add(LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "LNode"), LNode.Id((Symbol) "Var"))).SetStyle(NodeStyle.Operator), LNode.List(LNode.Call(LNode.List(LNode.Id(CodeSymbols.Out)), CodeSymbols.Var, LNode.List(LNode.Missing, targetTmpId)), candidate)));
					return targetTmpId;
				} else {
					return candidate;
				}
			}

			private bool TryAddVar(Symbol varName, bool isList, bool doNotAddBecauseRef = false)
			{
				if (!DuplicateDetector.Add(varName))
					return false;
				if (!doNotAddBecauseRef) {
					var vars = isList ? ListVars : NodeVars;
					if (!vars.ContainsKey(varName))
						vars[varName] = false;
					UsageCounters[varName] = UsageCounters.TryGetValue(varName, 0) + 1;
				}
				return true;
			}

			private void AddVarAssignmentOrComparisonTest(bool isNew, Symbol varName, LNode candidateSubtree, LNode condition = null, bool isLNodeList = false)
			{
				var varId = F.Id(varName);
				if (isNew) {
					if (!DeclareVarsSeparately)
						Tests.Add(LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "LNode"), LNode.Id((Symbol) "Var"))).SetStyle(NodeStyle.Operator), LNode.List(LNode.Call(LNode.List(LNode.Id(CodeSymbols.Out)), CodeSymbols.Var, LNode.List(LNode.Missing, varId)), candidateSubtree)));
					else if (isLNodeList)
						Tests.Add(LNode.Call(CodeSymbols.OrBits, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(LNode.List(LNode.InParensTrivia), CodeSymbols.Assign, LNode.List(varId, candidateSubtree)).SetStyle(NodeStyle.Operator), LNode.Id((Symbol) "IsEmpty"))).SetStyle(NodeStyle.Operator), LNode.Literal(true))).SetStyle(NodeStyle.Operator));
					else
						Tests.Add(LNode.Call(CodeSymbols.NotEq, LNode.List(LNode.Call(LNode.List(LNode.InParensTrivia), CodeSymbols.Assign, LNode.List(varId, candidateSubtree)).SetStyle(NodeStyle.Operator), LNode.Literal(null))).SetStyle(NodeStyle.Operator));
				} else {
					if (varName != __)
						Tests.Add(LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "LNode"), LNode.Id((Symbol) "Equals"))).SetStyle(NodeStyle.Operator), LNode.List(varId, candidateSubtree, LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "LNode"), LNode.Id((Symbol) "CompareMode"))).SetStyle(NodeStyle.Operator), LNode.Id((Symbol) "IgnoreTrivia"))).SetStyle(NodeStyle.Operator))));
				}
				if (condition != null)
					Tests.Add(condition);
			}

			private void MatchAttributes(LNode pattern, LNode candidate)
			{
				LNode condition;
				bool isParams, refExistingVar;
				Symbol listVar;
				var pAttrs = pattern.PAttrs();
				if (pAttrs.Count == 1 && (listVar = DecodeSubstitutionExpr(pAttrs[0], out condition, out isParams, out refExistingVar)) != null && isParams) {
					if (listVar != __ || condition != null) {
						bool isNew = TryAddVar(listVar, true, refExistingVar);
						AddVarAssignmentOrComparisonTest(isNew, listVar, LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Attrs"))).SetStyle(NodeStyle.Operator), condition, true);
					}
				} else if (pAttrs.Count != 0)
					Context.Sink.Error(pAttrs[0], "Currently, Attribute matching is very limited; you can only use `[$(...varName)]`");
			}

			private int GetFixedArgCount(LNodeList patternArgs, out int? varArgAt)
			{
				varArgAt = null;
				int argc = 0;
				for (int i = 0; i < patternArgs.Count; i++) {
					LNode condition;
					bool isParams, _;
					var nodeVar = DecodeSubstitutionExpr(patternArgs[i], out condition, out isParams, out _);
					if (isParams)
						varArgAt = i;
					else
						argc++;
				}
				return argc;
			}

			private void MakeArgListTests(LNodeList patternArgs, LNode candidate)
			{
				// Note: at this point we can assume that a check on the number of 
				// args in the candidate has already been emitted.
				bool seenVarArg = false;
				int i;
				for (i = 0; i < patternArgs.Count; i++)
				{
					MakeTestExpr(patternArgs[i], (Symbol varArgSym, bool hasCondition) => {
						if (varArgSym != null && !seenVarArg) {
							seenVarArg = true;

							LNode argList;
							if (i == 0 && patternArgs.Count == 1) {
								argList = LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Args"))).SetStyle(NodeStyle.Operator);
							} else {
								var numFixedArgs = F.Literal(patternArgs.Count - 1);
								if (i == 0 && patternArgs.Count > 1)
									argList = LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Args"))).SetStyle(NodeStyle.Operator), LNode.Id((Symbol) "WithoutLast"))).SetStyle(NodeStyle.Operator), LNode.List(numFixedArgs));
								else {
									var varArgStartLit = F.Literal(i);
									if (i + 1 == patternArgs.Count)
										argList = LNode.Call(CodeSymbols.New, LNode.List(LNode.Call((Symbol) "LNodeList", LNode.List(LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Args"))).SetStyle(NodeStyle.Operator), LNode.Id((Symbol) "Slice"))).SetStyle(NodeStyle.Operator), LNode.List(varArgStartLit))))));
									else
										argList = LNode.Call(CodeSymbols.New, LNode.List(LNode.Call((Symbol) "LNodeList", LNode.List(LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Args"))).SetStyle(NodeStyle.Operator), LNode.Id((Symbol) "Slice"))).SetStyle(NodeStyle.Operator), LNode.List(varArgStartLit, LNode.Call(CodeSymbols.Sub, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Args"))).SetStyle(NodeStyle.Operator), LNode.Id((Symbol) "Count"))).SetStyle(NodeStyle.Operator), numFixedArgs)).SetStyle(NodeStyle.Operator)))))));
								}
							}
							return argList;
						} else {
							if (varArgSym != null && seenVarArg)
								Context.Sink.Error(patternArgs[i], "More than a single $(...varargs) variable is not supported in a single argument list.");

							if (seenVarArg) {
								int left = patternArgs.Count - i;
								return LNode.Call(CodeSymbols.IndexBracks, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Args"))).SetStyle(NodeStyle.Operator), LNode.Call(CodeSymbols.Sub, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Args"))).SetStyle(NodeStyle.Operator), LNode.Id((Symbol) "Count"))).SetStyle(NodeStyle.Operator), F.Literal(left))).SetStyle(NodeStyle.Operator))).SetStyle(NodeStyle.Operator);
							} else
								return LNode.Call(CodeSymbols.IndexBracks, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Args"))).SetStyle(NodeStyle.Operator), F.Literal(i))).SetStyle(NodeStyle.Operator);
						}
					});
				}
			}
		}
	}
}