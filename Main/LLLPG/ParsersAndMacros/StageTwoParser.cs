using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Utilities;
using Loyc.Syntax;
using Loyc.Collections;

namespace Loyc.LLParserGenerator
{
	using S = CodeSymbols;

	/// <summary>
	/// Parses <see cref="LNode"/>s such as <c>(x | ("foo", y)) / z</c>
	/// into <see cref="Pred"/> objects.
	/// </summary>
	internal class StageTwoParser
	{
		IPGCodeGenHelper _helper;
		IMessageSink _sink;
		Dictionary<Symbol, Rule> _rules;
		public Dictionary<Symbol, Rule> Rules { get { return _rules; } }

		public StageTwoParser(IPGCodeGenHelper helper, IMessageSink sink)
		{
			_helper = helper;
			_sink = sink;
		}

		HashSet<Symbol> any_in_HashLabels = new HashSet<Symbol> { (Symbol)"#token" };

		/// <summary>Given Rules paired with LNodes produced by <see cref="StageOneParser"/>,
		/// this method translates each LNode into a <see cref="Pred"/> and updates
		/// <see cref="Rule.Pred"/> to point to the new Pred.</summary>
		public void Parse(IEnumerable<Pair<Rule,LNode>> rules)
		{
			_rules = LLParserGenerator.AddRulesToDict(rules.Select(p => p.A));
			foreach (var pair in rules) {
				Debug.Assert(pair.A.Pred == null);
				pair.A.Pred = NodeToPred(pair.B);
				if (pair.A.HasRecognizerVersion) // oops, need to keep recognizer in sync with main rule
					pair.A.MakeRecognizerVersion().Pred = pair.A.Pred;
			}
			Remove_any_in_Labels();
		}

		static LNodeFactory F = new LNodeFactory(new EmptySourceFile("LLLPG parser"));
		static readonly Symbol _Gate = S.Lambda;
		static readonly Symbol _EqGate = GSymbol.Get("<=>");
		static readonly Symbol _AddColon = GSymbol.Get("+:");
		static readonly Symbol _Star = GSymbol.Get("suf*");
		static readonly Symbol _Plus = GSymbol.Get("suf+");
		static readonly Symbol _Opt = GSymbol.Get("suf?");
		static readonly Symbol _And = S.AndBits;
		static readonly Symbol _AndNot = GSymbol.Get("&!");
		static readonly Symbol _Nongreedy = GSymbol.Get("nongreedy");
		static readonly Symbol _Greedy = GSymbol.Get("greedy");
		static readonly Symbol _Default = GSymbol.Get("default");
		static readonly Symbol _Default2 = GSymbol.Get("#default");
		static readonly Symbol _Inline = GSymbol.Get("inline");
		static readonly Symbol _Inline2 = GSymbol.Get("#inline");
		static readonly Symbol _NoInline = GSymbol.Get("noinline");
		static readonly Symbol _Error = GSymbol.Get("error");
		static readonly Symbol _DefaultError = GSymbol.Get("default_error");
		static readonly Symbol _Local = GSymbol.Get("Local");
		static readonly Symbol _Hoist = GSymbol.Get("Hoist");
		static readonly Symbol _Any = GSymbol.Get("any");
		
		enum Context { Rule, GateLeft, GateRight, And };

		Pred NodeToPred(LNode expr, Context ctx = Context.Rule)
		{
			try {
				return NodeToPredCore(expr, ctx);
			} catch (Exception ex) {
				_sink.Write(Severity.Error, expr, ex.ExceptionMessageAndType());
				return new TerminalPred(expr, _helper.EmptySet);
			}
		}
		Pred NodeToPredCore(LNode expr, Context ctx = Context.Rule)
		{
			if (expr.IsCall)
			{
				bool slash = false, not;
				if (expr.Calls(S.Tuple))
				{
					// sequence: (a, b, c)
					if (expr.Calls(S.Tuple, 1))
						return NodeToPred(expr.Args[0], ctx);
					return ArgsToSeq(expr, ctx);
				}
				else if (expr.Calls(S.Braces))
				{
					// Just code: use an empty sequence
					var seq = new Seq(expr);
					seq.PostAction = RemoveBraces(expr);
					return seq;
				}
				else if (expr.Calls(S.OrBits, 2) || (slash = expr.Calls(S.Div, 2)))
				{
					// alternatives: a | b, a || b, a / b
					LNode lhs = expr.Args[0], rhs = expr.Args[1];
					BranchMode lhsMode, rhsMode;
					Pred left = BranchToPred(lhs, out lhsMode, ctx);
					Pred right = BranchToPred(rhs, out rhsMode, ctx);
					return Pred.Or(left, right, slash, expr, lhsMode, rhsMode, _sink);
				}
				else if (expr.Calls(_Star, 1) || expr.Calls(_Plus, 1) || expr.Calls(_Opt, 1))
				{
					// loop (a+, a*) or optional (a?)
					return TranslateLoopExpr(expr, ctx);
				}
				else if (expr.Calls(_Gate, 1) || expr.Calls(_EqGate, 1))
				{
					// => foo (LES-based parser artifact)
					return new Gate(expr, new Seq(F.Missing),
					                      NodeToPred(expr.Args[0], Context.GateRight))
					                      { IsEquivalency = expr.Calls(_EqGate) };
				}
				else if (expr.Calls(_Gate, 2) || expr.Calls(_EqGate, 2))
				{
					if (ctx == Context.GateLeft)
						_sink.Write(Severity.Error, expr, "Cannot use a gate in the left-hand side of another gate");

					return new Gate(expr, NodeToPred(expr.Args[0], Context.GateLeft),
					                      NodeToPred(expr.Args[1], Context.GateRight)) 
					                      { IsEquivalency = expr.Calls(_EqGate) };
				}
				else if ((not = expr.Calls(_AndNot, 1)) || expr.Calls(_And, 1))
				{
					expr = expr.Args[0];
					var subpred = AutoNodeToPred(expr, Context.And);
					LNode subexpr = subpred as LNode, subexpr0 = subexpr;
					bool local = false;
					if (subexpr != null) {
						if ((subexpr = subexpr.WithoutAttrNamed(_Local)) != subexpr0)
							local = true;
						// we should default to [Local] eventually, so recognize [Hoist] too
						subexpr = subexpr.WithoutAttrNamed(_Hoist);
					}
					return new AndPred(expr, subexpr ?? subpred, not, local);
				}
				else if (expr.Calls(S.NotBits, 1))
				{
					var subpred = NodeToPred(expr.Args[0], ctx);
					if (subpred is TerminalPred) {
						var term = (TerminalPred)subpred;
						term.Set = term.Set.Inverted().WithoutEOF();
						return term;
					} else {
						_sink.Write(Severity.Error, expr, 
							"The set-inversion operator ~ can only be applied to a single terminal, not a '{0}'", subpred.GetType().Name);
						return subpred;
					}
				}
				else if (expr.Calls(_Any, 2) && expr.Args[0].IsId) 
				{
					return Translate_any_in_Expr(expr, ctx);
				}
				else if ((expr.Name.Name.EndsWith(":") || expr.Name.Name.EndsWith("=")) && expr.ArgCount == 2)
				{
					return TranslateLabeledExpr(expr, ctx);
				}
			}
			
			// expr is an Id, literal, or non-special call
			Rule rule = TryGetRule(expr);
			if (rule != null)
			{
				//int ruleArgc = rule.Basis.Args[2].ArgCount;
				//if (expr.ArgCount > ruleArgc) // don't complain about too few args, in case there are default args (I'm too lazy to check)
				//    _sink.Write(MessageSink.Error, expr, "Rule '{0}' takes {1} arguments ({2} given)", rule.Name, ruleArgc, expr.ArgCount);
				return new RuleRef(expr, rule) { Params = expr.Args };
			}

			string errorMsg = null;
			Pred terminal = _helper.CodeToTerminalPred(expr, ref errorMsg);
			if (terminal == null) {
				errorMsg = errorMsg ?? "LLLPG: unrecognized expression";
				terminal = new TerminalPred(expr, _helper.EmptySet);
				_sink.Write(Severity.Error, expr, errorMsg);
			} else if (errorMsg != null)
				_sink.Write(Severity.Warning, expr, errorMsg);
			return terminal;
		}

		private Pred TranslateLoopExpr(LNode expr, Context ctx)
		{
			Symbol type = expr.Name;
			bool? greedy = null;
			bool g;
			expr = expr.Args[0];
			if ((g = expr.Calls(_Greedy, 1)) || expr.Calls(_Nongreedy, 1)) {
				greedy = g;
				expr = expr.Args[0];
			}
			BranchMode branchMode;
			Pred subpred = BranchToPred(expr, out branchMode, ctx);

			if (branchMode != BranchMode.None)
				_sink.Write(Severity.Warning, expr, "'default' and 'error' only apply when there are multiple arms (a|b, a/b)");

			if (type == _Star)
				return new Alts(expr, LoopMode.Star, subpred, greedy);
			else if (type == _Plus) {
				return new Seq(subpred, new Alts(expr, LoopMode.Star, subpred.Clone(), greedy), expr);
			} else // type == _Opt
				return new Alts(expr, LoopMode.Opt, subpred, greedy);
		}

		private Pred TranslateLabeledExpr(LNode expr, Context ctx)
		{
			var pred = NodeToPred(expr.Args[1], ctx);
			// Note: it's required that we don't change pred.Basis here--
			// AutoValueSaverVisitor expects the Basis of x:'#' to be '#' so 
			// that in code blocks $'#' is recognized as being "the same".
			var label = expr.Args[0];
			var labelName = label.Name;
			if (!label.IsId) {
				_sink.Write(Severity.Error, label, "A label must be an identifier");
			} else if (labelName == _Inline2 || labelName == _Inline || labelName == _NoInline) {
				if (pred is RuleRef)
					((RuleRef)pred).IsInline = labelName != _NoInline;
				else
					_sink.Write(Severity.Error, label, "'{0}:' can only be attached to a rule reference, which '{1}' is not", labelName, pred.ToString());
			} else {
				var oper = expr.Name;
				pred.VarLabel = labelName;
				pred.VarIsList = oper == S.AddSet || oper == _AddColon;
				pred.ResultSaver = Pred.GetStandardResultSaver(label, expr.Name);
			}
			return pred;
		}

		private Pred Translate_any_in_Expr(LNode expr, Context ctx)
		{
			// The user typed something of the form "any idNode in subExpr"
			LNode idNode = expr.Args[0], subExpr = expr.Args[1];
			Symbol id = idNode.Name, id2 = (Symbol)("#" + id.Name);
			any_in_HashLabels.Add(id2);
			if (id.Name.StartsWith("#"))
				any_in_HashLabels.Add(id);
			bool isToken = id.Name == "#token" || id2.Name == "#token";
			// Scan the rules looking for ones marked with the specified ID.
			LNode newExpr = null;
			foreach (var rul in _rules.Values) {
				if (isToken ? rul.IsToken : rul.Basis.Attrs.Any(attr => attr.Name == id || attr.Name == id2)) {
					// Given "any foo in (A foo B)", suppose we find rules Foo1, 
					// Foo2 & Foo3 having the attribute 'foo'. We're constructing
					// the grammar fragment (A Foo1 B | A Foo2 B | A Foo3 B)
					// i.e. ((A, Foo1, B) | (A, Foo2, B)) | (A, Foo3, B) in LES.
					bool found = false;
					LNode subExpr2 = subExpr.ReplaceRecursive(node => {
						if (node.Equals(idNode)) {
							found = true;
							return node.WithName(rul.Name);
						}
						return null;
					});
					if (!found) {
						_sink.Write(Severity.Error, subExpr,
							"'any': expected '{0}' somewhere in the expression following 'in'", id);
						break;
					}
					if (newExpr == null)
						newExpr = subExpr2;
					else
						newExpr = F.Call(S.Div, newExpr, subExpr2);
				}
			}
			if (newExpr == null) {
				_sink.Write(Severity.Warning, expr,
					"'any': there are no rules marked with the attribute '{0}', so this item has no effect", id);
				newExpr = F.Tuple();
			}
			return NodeToPred(newExpr, ctx);
		}
		private void Remove_any_in_Labels()
		{
			// Remove #attr in rules referred to by an "any attr in ..." expression.
			// (If we don't delete them, the final output will cause C# compiler errors.)
			if (any_in_HashLabels.Count == 0)
				return;
			foreach (var rule in _rules.Values) {
				rule.Basis = rule.Basis.WithAttrs(a =>
					any_in_HashLabels.Contains(a.Name) ? Maybe<LNode>.NoValue : a);
			}
		}

		/// <summary>Tries to interpret expr as a reference to an existing rule.</summary>
		Rule TryGetRule(LNode expr)
		{
			Rule rule;
			if (!expr.IsLiteral && _rules.TryGetValue(expr.Name, out rule))
				return rule;
			return null;
		}

		Pred BranchToPred(LNode expr, out BranchMode mode, Context ctx)
		{
			if (expr.Calls(_Default, 1) || expr.Calls(_Default2, 1)) {
				expr = expr.Args[0];
				mode = BranchMode.Default;
			} else if (expr.Calls(_Error, 1) || expr.IsIdNamed(_DefaultError)) {
				mode = (expr.AttrNamed(S.Continue) != null || expr.AttrNamed(GSymbol.Get("continue")) != null) 
				       ? BranchMode.ErrorContinue : BranchMode.ErrorExit;
				if (expr.Calls(_Error, 1))
					expr = expr.Args[0];
				else
					return DefaultErrorBranch.Value;
			} else
				mode = BranchMode.None;

			return NodeToPred(expr, ctx);
		}

		Pred ArgsToSeq(LNode expr, Context ctx)
		{
			List<object> objs = expr.Args.Select(node => AutoNodeToPred(node, ctx)).ToList();
			Seq seq = new Seq(expr);
			LNode action = null;
			bool error = false;
			for (int i = 0; i < objs.Count; i++)
			{
				if (objs[i] is LNode)
				{
					var code = (LNode)objs[i];
					if ((ctx == Context.And || ctx == Context.GateLeft) && !error) {
						error = true;
						_sink.Write(Severity.Error, objs[i], ctx == Context.And ?
							"Cannot use an action block inside an '&' or '!' predicate; these predicates are for prediction only." :
							"Cannot use an action block on the left side of a '=>' gate; the left side is for prediction only.");
					}
					action = Pred.MergeActions(action, code);
				}
				else // Pred
				{
					Pred pred = (Pred)objs[i];
					pred.PreAction = Pred.MergeActions(action, pred.PreAction);
					action = null;
					seq.List.Add(pred);
				}
			}
			if (action != null)
				seq.PostAction = action;

			if (seq.List.Count == 1) {
				var contents = seq.List[0];
				contents.PreAction = Pred.MergeActions(seq.PreAction, contents.PreAction);
				contents.PostAction = Pred.MergeActions(seq.PostAction, contents.PostAction);
				return contents;
			}

			return seq;
		}
		object AutoNodeToPred(LNode expr, Context ctx)
		{
			if (expr.CallsMin(S.Braces, 0))
				return RemoveBraces(expr);
			return NodeToPred(expr, ctx);
		}
		static LNode RemoveBraces(LNode expr)
		{
			Debug.Assert(expr.Calls(S.Braces));
			if (expr.ArgCount == 1)
				return expr.Args[0];
			else
				return expr.WithTarget(S.Splice);
		}
		//static TerminalPred AsTerminalSet(Pred pred)
		//{
		//    if (pred is RuleRef)
		//        return AsTerminalSet(((RuleRef)pred).Rule.Pred);
		//    if (pred is TerminalPred)
		//        return (TerminalPred)pred;
		//    return null;
		//}
	
	}
}
