// Generated from MatchCode.ecs by LeMP custom tool. LLLPG version: 1.4.0.0
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
		[LexicalMacro("matchCode (var) { case ...: ... }; // In LES, use a => b instead of case a: b", "Attempts to match and deconstruct a Loyc tree against a series of cases with patterns, e.g. " + "`case $a + $b:` expects a tree that calls `+` with two parameters, placed in new variables called a and b. " + "`break` is not required or recognized at the end of each case's handler (code block). " + "Use `$(..x)` to gather zero or more parameters into a list `x`. " + "Use `case pattern1, pattern2:` in EC# to handle multiple cases with the same handler.")] public static LNode matchCode(LNode node, IMacroContext context)
		{
			var args_body = context.GetArgsAndBody(true);
			RVList<LNode> args = args_body.Item1, body = args_body.Item2;
			if (args.Count != 1 || body.Count < 1)
				return null;
			var cases = GetCases(body, context.Sink);
			if (cases.IsEmpty)
				return null;
			var output = new RWList<LNode>();
			var @var = MaybeAddTempVarDecl(args[0], output);
			var ifClauses = new List<Pair<LNode,LNode>>();
			var cmc = new CodeMatchContext { 
				Context = context
			};
			foreach (var @case in cases) {
				cmc.ThenClause.Clear();
				LNode testExpr = null;
				if (@case.Key.Count > 0) {
					if (cmc.IsMultiCase = @case.Key.Count > 1) {
						cmc.UsageCounters.Clear();
						testExpr = @case.Key.Aggregate((LNode) null, (test, pattern) => {
							test = LNode.MergeBinary(test, cmc.MakeTopTestExpr(pattern, @var), S.Or);
							return test;
						});
						foreach (var pair in cmc.UsageCounters.Where(p => p.Value < @case.Key.Count)) {
							if (cmc.NodeVars.ContainsKey(pair.Key))
								cmc.NodeVars[pair.Key] = true;
							if (cmc.ListVars.ContainsKey(pair.Key))
								cmc.ListVars[pair.Key] = true;
						}
					} else
						testExpr = cmc.MakeTopTestExpr(@case.Key[0], @var);
				}
				var handler = @case.Value;
				if (cmc.ThenClause.Count > 0)
					handler = LNode.MergeLists(F.Braces(cmc.ThenClause), handler, S.Braces);
				ifClauses.Add(Pair.Create(testExpr, handler));
			}
			LNode ifStmt = null;
			for (int i = ifClauses.Count - 1; i >= 0; i--) {
				if (ifClauses[i].Item1 == null) {
					if (ifStmt == null)
						ifStmt = ifClauses[i].Item2;
					else
						context.Sink.Write(Severity.Error, node, "The default case must appear last, and there can be only one.");
				} else {
					if (ifStmt == null)
						ifStmt = F.Call(S.If, ifClauses[i].Item1, ifClauses[i].Item2);
					else
						ifStmt = F.Call(S.If, ifClauses[i].Item1, ifClauses[i].Item2, ifStmt);
				}
			}
			if (cmc.NodeVars.Count > 0)
				output.Add(F.Call(S.Var, Range.Single(F.Id("LNode")).Concat(cmc.NodeVars.OrderBy(v => v.Key.Name).Select(kvp => kvp.Value ? F.Call(S.Assign, F.Id(kvp.Key), F.Null) : F.Id(kvp.Key)))));
			if (cmc.ListVars.Count > 0) {
				LNode type = LNode.Call(CodeSymbols.Of, LNode.List(LNode.Id((Symbol) "RVList"), LNode.Id((Symbol) "LNode")));
				output.Add(F.Call(S.Var, Range.Single(type).Concat(cmc.ListVars.OrderBy(v => v.Key.Name).Select(kvp => kvp.Value ? LNode.Call(CodeSymbols.Assign, LNode.List(F.Id(kvp.Key), LNode.Call(CodeSymbols.Default, LNode.List(type)))) : F.Id(kvp.Key)))));
			}
			if (output.Count == 0)
				return ifStmt;
			else {
				output.Add(ifStmt);
				return F.Braces(output.ToRVList());
			}
		}
		static readonly Symbol __ = (Symbol) "_";
		static RVList<Pair<RVList<LNode>,LNode>> GetCases(RVList<LNode> body, IMessageSink sink)
		{
			var pairs = RVList<Pair<RVList<LNode>,LNode>>.Empty;
			for (int i = 0; i < body.Count; i++) {
				bool isDefault;
				if (body[i].Calls(S.Lambda, 2)) {
					var key = new RVList<LNode>(body[i][0].WithoutOuterParens());
					pairs.Add(Pair.Create(key, AutoStripBraces(body[i][1])));
				} else if ((isDefault = IsDefaultLabel(body[i])) || body[i].CallsMin(S.Case, 1)) {
					var alts = isDefault ? RVList<LNode>.Empty : body[i].Args.SmartSelect(pat => AutoStripBraces(pat));
					int bodyStart = ++i;
					for (; i < body.Count && !IsDefaultLabel(body[i]) && !body[i].CallsMin(S.Case, 1); i++) {
					}
					LNode handler;
					if (i == bodyStart + 1)
						handler = body[bodyStart];
					else
						handler = F.Braces(body.Slice(bodyStart, i - bodyStart));
					pairs.Add(Pair.Create(alts, handler));
					i--;
				} else {
					Reject(sink, body[i], "expected 'case _:' or '_ => _'");
					break;
				}
			}
			return pairs;
		}
		static LNode AutoStripBraces(LNode node)
		{
			if (node.Calls(S.Braces, 1) && !node.HasPAttrs())
				return node.Args[0];
			return node;
		}
		static bool IsDefaultLabel(LNode stmt)
		{
			return stmt.Calls(S.Label, 1) && stmt[0].IsIdNamed(S.Default);
		}
		class CodeMatchContext
		{
			HashSet<Symbol> DuplicateDetector = new HashSet<Symbol>();
			public Dictionary<Symbol,int> UsageCounters = new Dictionary<Symbol,int>();
			public Dictionary<Symbol,bool> NodeVars = new Dictionary<Symbol,bool>();
			public Dictionary<Symbol,bool> ListVars = new Dictionary<Symbol,bool>();
			public RVList<LNode> ThenClause = new RVList<LNode>();
			public IMacroContext Context;
			public bool IsMultiCase;
			public RWList<LNode> Tests = new RWList<LNode>();
			internal LNode MakeTopTestExpr(LNode pattern, LNode @var)
			{
				DuplicateDetector.Clear();
				Tests.Clear();
				MakeTestExpr(pattern, @var);
				LNode result = null;
				foreach (var test in Tests)
					result = LNode.MergeBinary(result, test, S.And);
				return result;
			}
			private void MakeTestExpr(LNode pattern, LNode candidate)
			{
				Symbol varArgSym;
				LNode varArgCond;
				MakeTestExpr(pattern, candidate, out varArgSym, out varArgCond);
				if (varArgSym != null)
					Context.Sink.Write(Severity.Error, pattern, "A list cannot be matched in this context. Remove '..' or 'params'.");
			}
			private void MakeTestExpr(LNode pattern, LNode candidate, out Symbol varArgSym, out LNode varArgCond)
			{
				varArgSym = null;
				varArgCond = null;
				LNode condition;
				bool isParams;
				var nodeVar = GetSubstitutionVar(pattern, out condition, out isParams);
				int predictedTests = pattern.Attrs.Count + (nodeVar != null ? 0 : pattern.Args.Count) + (!pattern.HasSimpleHeadWithoutPAttrs() ? 1 : 0);
				if (predictedTests > 1)
					candidate = MaybePutCandidateInTempVar(candidate.IsCall, candidate);
				MatchAttributes(pattern, candidate);
				if (nodeVar != null) {
					if (nodeVar != __ || condition != null) {
						AddVar(nodeVar, isParams, errAt: pattern);
						if (!isParams) {
							var assignment = LNode.Call(CodeSymbols.Assign, LNode.List(F.Id(nodeVar), candidate));
							Tests.Add(LNode.Call(CodeSymbols.Neq, LNode.List(assignment.WithAttrs(LNode.InParensTrivia), LNode.Literal(null))));
							Tests.Add(condition);
						}
					}
					if (isParams) {
						varArgSym = nodeVar;
						varArgCond = condition;
						return;
					}
				} else if (pattern.IsId) {
					Tests.Add(LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "IsIdNamed"))), LNode.List(LNode.Call(CodeSymbols.Cast, LNode.List(F.Literal(pattern.Name.Name), LNode.Id((Symbol) "Symbol"))))));
				} else if (pattern.IsLiteral) {
					if (pattern.Value == null)
						Tests.Add(LNode.Call(CodeSymbols.Eq, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Value"))), LNode.Literal(null))));
					else
						Tests.Add(LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(pattern, LNode.Id((Symbol) "Equals"))), LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Value"))))));
				} else {
					int? varArgAt;
					int fixedArgC = GetFixedArgCount(pattern.Args, out varArgAt);
					var pTarget = pattern.Target;
					if (pTarget.IsId && !pTarget.HasPAttrs()) {
						var quoteTarget = QuoteSymbol(pTarget.Name);
						LNode targetTest;
						if (varArgAt.HasValue && fixedArgC == 0)
							targetTest = LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Calls"))), LNode.List(quoteTarget));
						else if (varArgAt.HasValue)
							targetTest = LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "CallsMin"))), LNode.List(quoteTarget, F.Literal(fixedArgC)));
						else
							targetTest = LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Calls"))), LNode.List(quoteTarget, F.Literal(fixedArgC)));
						Tests.Add(targetTest);
					} else {
						if (fixedArgC == 0) {
							Tests.Add(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "IsCall"))));
							if (!varArgAt.HasValue)
								Tests.Add(LNode.Call(CodeSymbols.Eq, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Args"))), LNode.Id((Symbol) "Count"))), LNode.Literal(0))));
						} else {
							var op = varArgAt.HasValue ? S.GE : S.Eq;
							Tests.Add(LNode.Call(op, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Args"))), LNode.Id((Symbol) "Count"))), F.Literal(fixedArgC))));
						}
						int i = Tests.Count;
						MakeTestExpr(pTarget, LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Target"))));
					}
					MakeArgListTests(pattern.Args, ref candidate);
				}
			}
			LNode MaybePutCandidateInTempVar(bool condition, LNode candidate)
			{
				if (condition) {
					var targetTmp = NextTempName();
					var targetTmpId = F.Id(targetTmp);
					AddVar(targetTmp, false, errAt: candidate);
					Tests.Add(LNode.Call(CodeSymbols.Neq, LNode.List(LNode.Call(LNode.List(LNode.InParensTrivia), CodeSymbols.Assign, LNode.List(targetTmpId, candidate)), LNode.Literal(null))));
					return targetTmpId;
				} else {
					return candidate;
				}
			}
			private void AddVar(Symbol varName, bool isList, LNode errAt)
			{
				if (!DuplicateDetector.Add(varName))
					Context.Sink.Write(Severity.Error, errAt, "'{0}': Each matched $variable must have a unique name.", varName);
				var vars = isList ? ListVars : NodeVars;
				if (!vars.ContainsKey(varName))
					vars[varName] = false;
				UsageCounters[varName] = UsageCounters.TryGetValue(varName, 0) + 1;
			}
			private void MatchAttributes(LNode pattern, LNode candidate)
			{
				LNode condition;
				bool isParams;
				Symbol listVar;
				var pAttrs = pattern.PAttrs();
				if (pAttrs.Count == 1 && (listVar = GetSubstitutionVar(pAttrs[0], out condition, out isParams)) != null && isParams) {
					if (listVar != __ || condition != null) {
						AddVar(listVar, true, errAt: pattern);
						Tests.Add(LNode.Call(CodeSymbols.OrBits, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(LNode.List(LNode.InParensTrivia), CodeSymbols.Assign, LNode.List(F.Id(listVar), LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Attrs"))))), LNode.Id((Symbol) "IsEmpty"))), LNode.Literal(true))));
						if (condition != null)
							Tests.Add(condition);
					}
				} else if (pAttrs.Count != 0)
					Context.Sink.Write(Severity.Error, pAttrs[0], "Currently, Attribute matching is very limited; you can only use `[$(..varName)]`");
			}
			private int GetFixedArgCount(RVList<LNode> patternArgs, out int? varArgAt)
			{
				varArgAt = null;
				int argc = 0;
				for (int i = 0; i < patternArgs.Count; i++) {
					LNode condition;
					bool isParams;
					var nodeVar = GetSubstitutionVar(patternArgs[i], out condition, out isParams);
					if (isParams)
						varArgAt = i;
					else
						argc++;
				}
				return argc;
			}
			private void MakeArgListTests(RVList<LNode> patternArgs, ref LNode candidate)
			{
				Symbol varArgSym = null;
				LNode varArgCond = null;
				int i;
				for (i = 0; i < patternArgs.Count; i++) {
					MakeTestExpr(patternArgs[i], LNode.Call(CodeSymbols.IndexBracks, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Args"))), F.Literal(i))), out varArgSym, out varArgCond);
					if (varArgSym != null)
						break;
				}
				int i2 = i + 1;
				for (int left = patternArgs.Count - i2; i2 < patternArgs.Count; i2++) {
					Symbol varArgSym2 = null;
					LNode varArgCond2 = null;
					MakeTestExpr(patternArgs[i2], LNode.Call(CodeSymbols.IndexBracks, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Args"))), LNode.Call(CodeSymbols.Sub, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Args"))), LNode.Id((Symbol) "Count"))), F.Literal(left))))), out varArgSym2, out varArgCond2);
					if (varArgSym2 != null) {
						Context.Sink.Write(Severity.Error, patternArgs[i2], "More than a single $(..varargs) variable is not supported in a single argument list.");
						break;
					}
					left--;
				}
				if (varArgSym != null) {
					LNode varArgSymId = F.Id(varArgSym);
					LNode grabVarArgs;
					if (i == 0 && patternArgs.Count == 1) {
						grabVarArgs = LNode.Call(CodeSymbols.Assign, LNode.List(varArgSymId, LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Args")))));
					} else if (i == 0 && patternArgs.Count > 1) {
						var fixedArgsLit = F.Literal(patternArgs.Count - 1);
						grabVarArgs = LNode.Call(CodeSymbols.Assign, LNode.List(varArgSymId, LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Args"))), LNode.Id((Symbol) "WithoutLast"))), LNode.List(fixedArgsLit))));
					} else {
						var varArgStartLit = F.Literal(i);
						var fixedArgsLit = F.Literal(patternArgs.Count - 1);
						if (i + 1 == patternArgs.Count)
							grabVarArgs = LNode.Call(CodeSymbols.Assign, LNode.List(varArgSymId, LNode.Call(CodeSymbols.New, LNode.List(LNode.Call(LNode.Call(CodeSymbols.Of, LNode.List(LNode.Id((Symbol) "RVList"), LNode.Id((Symbol) "LNode"))), LNode.List(LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Args"))), LNode.Id((Symbol) "Slice"))), LNode.List(varArgStartLit))))))));
						else
							grabVarArgs = LNode.Call(CodeSymbols.Assign, LNode.List(varArgSymId, LNode.Call(CodeSymbols.New, LNode.List(LNode.Call(LNode.Call(CodeSymbols.Of, LNode.List(LNode.Id((Symbol) "RVList"), LNode.Id((Symbol) "LNode"))), LNode.List(LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Args"))), LNode.Id((Symbol) "Slice"))), LNode.List(varArgStartLit, LNode.Call(CodeSymbols.Sub, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(candidate, LNode.Id((Symbol) "Args"))), LNode.Id((Symbol) "Count"))), fixedArgsLit))))))))));
					}
					if (varArgCond != null || IsMultiCase) {
						Tests.Add(LNode.Call(CodeSymbols.OrBits, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(grabVarArgs.WithAttrs(LNode.InParensTrivia), LNode.Id((Symbol) "IsEmpty"))), LNode.Literal(true))));
						Tests.Add(varArgCond);
					} else
						ThenClause.Add(grabVarArgs);
				}
			}
			internal static Symbol GetSubstitutionVar(LNode expr, out LNode condition, out bool isParams)
			{
				condition = null;
				isParams = false;
				if (expr.Calls(S.Substitute, 1)) {
					LNode id = expr.Args[0];
					if (id.AttrNamed(S.Params) != null)
						isParams = true;
					else if (id.Calls(S.DotDot, 1)) {
						isParams = true;
						id = id.Args[0];
					}
					if (id.Calls(S.IndexBracks, 2)) {
						condition = id.Args[1];
						id = id.Args[0];
					} else if (id.ArgCount == 1) {
						condition = id.Args[0];
						id = id.Target;
					}
					if (condition != null)
						condition = condition.ReplaceRecursive(n => n.IsIdNamed(S._HashMark) ? id : null);
					if (!id.IsId)
						return null;
					return id.Name;
				}
				return null;
			}
		}
	}
}
