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
	using Ecs;
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
		public bool _ruleChangesInRecognizer;
		public bool _parsingRecognizerVersion;
		public bool _insideRecognizerSequence = false;

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
			foreach (var pair in rules)
			{
				Debug.Assert(pair.A.Pred == null);

				_parsingRecognizerVersion = _ruleChangesInRecognizer = false;
				pair.A.Pred = NodeToPred(pair.B);

				if (pair.A.HasRecognizerVersion) {
					if (_ruleChangesInRecognizer) {
						_parsingRecognizerVersion = true;
						pair.A.Recognizer.Pred = NodeToPred(pair.B);
					} else {
						pair.A.Recognizer.Pred = pair.A.Pred;
					}
				}
			}

			Remove_any_in_Labels();
		}

		static LNodeFactory F = new LNodeFactory(new EmptySourceFile("LLLPG parser"));
		static readonly Symbol _Gate = S.Lambda;
		static readonly Symbol _EqGate = GSymbol.Get("'<=>");
		static readonly Symbol _AddColon = GSymbol.Get("'+:");
		static readonly Symbol _Star = GSymbol.Get("'suf*");
		static readonly Symbol _Plus = GSymbol.Get("'suf+");
		static readonly Symbol _Opt = GSymbol.Get("'suf?");
		static readonly Symbol _And = S.AndBits;
		static readonly Symbol _AndNot = GSymbol.Get("'&!");
		static readonly Symbol _nongreedy = GSymbol.Get("nongreedy");
		static readonly Symbol _greedy = GSymbol.Get("greedy");
		static readonly Symbol _default = GSymbol.Get("default");
		static readonly Symbol _default2 = GSymbol.Get("'default");
		static readonly Symbol _inline = GSymbol.Get("inline");
		static readonly Symbol _inline2 = GSymbol.Get("#inline");
		static readonly Symbol _noinline = GSymbol.Get("noinline");
		static readonly Symbol _error = GSymbol.Get("error");
		static readonly Symbol _DefaultError = GSymbol.Get("default_error");
		static readonly Symbol _Local = GSymbol.Get("Local");
		static readonly Symbol _Hoist = GSymbol.Get("Hoist");
		static readonly Symbol _any = GSymbol.Get("any");
		static readonly Symbol _recognizer = GSymbol.Get("recognizer");
		static readonly Symbol _nonrecognizer = GSymbol.Get("nonrecognizer");

		enum Context { Rule, GateLeft, GateRight, And };

		Pred NodeToPred(LNode expr, Context ctx = Context.Rule)
		{
			try {
				return NodeToPredCore(expr, ctx);
			} catch (Exception ex) {
				_sink.Error(expr, ex.ExceptionMessageAndType());
				return new TerminalPred(expr, _helper.EmptySet);
			}
		}
		Pred NodeToPredCore(LNode expr, Context ctx = Context.Rule)
		{
			if (expr.IsCall)
			{
				bool slash = false, not;
				var name = expr.Name;
				if (name == S.Tuple)
				{
					// sequence: (a, b, c)
					if (expr.Calls(S.Tuple, 1))
						return NodeToPred(expr.Args[0], ctx);
					return TranslateToSeq(expr, ctx);
				}
				else if (name == S.Braces)
				{
					// User action {block}
					if (ctx == Context.And || ctx == Context.GateLeft) {
						_sink.Error(expr, ctx == Context.And ?
							"Cannot use an action block inside an '&' or '&!' predicate; these predicates are for prediction only." :
							"Cannot use an action block on the left side of a '=>' gate; the left side is for prediction only.");
					}
					return new ActionPred(expr, expr.Args) { AllowInRecognizer = _insideRecognizerSequence };
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
						_sink.Error(expr, "Cannot use a gate in the left-hand side of another gate");

					return new Gate(expr, NodeToPred(expr.Args[0], Context.GateLeft),
					                      NodeToPred(expr.Args[1], Context.GateRight)) 
					                      { IsEquivalency = expr.Calls(_EqGate) };
				}
				else if ((not = expr.Calls(_AndNot, 1)) || expr.Calls(_And, 1))
				{
					return TranslateAndPred(expr, not);
				}
				else if (expr.Calls(S.NotBits, 1))
				{
					var subpred = NodeToPred(expr.Args[0], ctx);
					if (subpred is TerminalPred) {
						var term = (TerminalPred)subpred;
						term.Set = term.Set.Inverted().WithoutEOF();
						return term;
					} else {
						_sink.Error(expr, 
							"The set-inversion operator ~ can only be applied to a single terminal, not a '{0}'", subpred.GetType().Name);
						return subpred;
					}
				}
				else if ((name.Name.EndsWith(":") || name.Name.EndsWith("=")) && expr.ArgCount == 2)
				{
					return TranslateLabeledExpr(expr, ctx);
				}
				else if (expr.Calls(_any, 2) && expr.Args[0].IsId) 
				{
					return Translate_any_in_Expr(expr, ctx);
				}
				else if (expr.Calls(_recognizer, 1) || expr.Calls(_nonrecognizer, 1))
				{
					_ruleChangesInRecognizer = true;
					var saved = _insideRecognizerSequence;
					try {
						_insideRecognizerSequence = expr.Calls(_recognizer, 1);
						if (_insideRecognizerSequence == _parsingRecognizerVersion)
							return NodeToPred(expr[0]);
						else
							return new Seq(expr.Target); // empty sequence
					} finally {
						_insideRecognizerSequence = saved;
					}
				}
			}
			
			// expr is an Id, literal, or non-special call
			Rule rule = TryGetRule(expr);
			if (rule != null)
			{
				LNode _, args;
				if (EcsValidators.MethodDefinitionKind(rule.Basis, out _, out _, out args, out _, false) == S.Fn) {
					if (expr.ArgCount > args.ArgCount) // don't complain about too few args, in case there are default args (I'm too lazy to check)
						_sink.Error(expr, "Rule '{0}' takes {1} arguments ({2} given)", rule.Name, args.ArgCount, expr.ArgCount);
				}

				return new RuleRef(expr, rule) { Params = expr.Args };
			}

			string errorMsg = null;
			Pred terminal = _helper.CodeToTerminalPred(expr, ref errorMsg);
			if (terminal == null) {
				errorMsg = errorMsg ?? "LLLPG: unrecognized expression";
				terminal = new TerminalPred(expr, _helper.EmptySet);
				_sink.Error(expr, errorMsg);
			} else if (errorMsg != null)
				_sink.Warning(expr, errorMsg);
			return terminal;
		}

		private Pred TranslateLoopExpr(LNode expr, Context ctx)
		{
			Symbol type = expr.Name;
			bool? greedy = null;
			bool g;
			expr = expr.Args[0];
			if ((g = expr.Calls(_greedy, 1)) || expr.Calls(_nongreedy, 1)) {
				greedy = g;
				expr = expr.Args[0];
			}
			BranchMode branchMode;
			Pred subpred = BranchToPred(expr, out branchMode, ctx);

			if (branchMode == BranchMode.ErrorContinue || branchMode == BranchMode.ErrorExit)
				_sink.Error(expr, "'error' only applies when there are multiple arms (a|b, a/b)");

			Pred clone = type == _Plus ? subpred.Clone() : null;
			Alts alts = new Alts(expr, type == _Opt ? LoopMode.Opt : LoopMode.Star, clone ?? subpred, greedy);
			if (branchMode == BranchMode.Default)
				alts.DefaultArm = 0;
			if (clone != null)
				return new Seq(subpred, alts, expr);
			else
				return alts;
		}

		private Pred TranslateAndPred(LNode andExpr, bool not)
		{
			var expr = andExpr.Args[0];

			// Distinguish between semantic and syntactic predicates
			LNode subexpr = null;
			Pred subpred = null;
			if (expr.Calls(S.Braces))
				subexpr = expr.ArgCount == 1 ? expr[0] : expr;
			else
				subpred = NodeToPred(expr, Context.And);
			LNode subexpr0 = subexpr;

			// Extract [Local] or [Hoist] attribute
			bool local = false;
			if (subexpr != null) {
				local = true;
				if ((subexpr = subexpr.WithoutAttrNamed(_Hoist)) != subexpr0)
					local = false;
				// also recognize [Local], which was not the default until v1.9.1
				subexpr = subexpr.WithoutAttrNamed(_Local);
			}

			// Extract error message for Check(), if provided.
			string errorString = null;
			if (subexpr != null)
				subexpr = subexpr.WithAttrs(n => {
					if (n.Value is string) {
						errorString = (string)n.Value;
						return NoValue.Value; // drop attribute from output
					} else if (n.IsIdNamed("NoCheck")) {
						errorString = "";
						return NoValue.Value;
					}
					return n;
				});

			return new AndPred(expr, (object)subexpr ?? subpred, not, local) { CheckErrorMessage = errorString };
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
				_sink.Error(label, "A label must be an identifier");
			} else if (labelName == _inline2 || labelName == _inline || labelName == _noinline) {
				if (pred is RuleRef)
					((RuleRef)pred).IsInline = labelName != _noinline;
				else
					_sink.Error(label, "'{0}:' can only be attached to a rule reference, which '{1}' is not", labelName, pred.ToString());
			} else {
				var oper = expr.Name;
				pred.VarLabel = labelName;
				pred.VarIsList = oper == S.AddAssign || oper == _AddColon;
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
						_sink.Error(subExpr,
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
				_sink.Warning(expr,
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
			if (expr.Calls(_default, 1) || expr.Calls(_default2, 1)) {
				expr = expr.Args[0];
				mode = BranchMode.Default;
			} else if (expr.Calls(_error, 1) || expr.IsIdNamed(_DefaultError)) {
				mode = (expr.AttrNamed(S.Continue) != null || expr.AttrNamed(GSymbol.Get("continue")) != null) 
				       ? BranchMode.ErrorContinue : BranchMode.ErrorExit;
				if (expr.Calls(_error, 1))
					expr = expr.Args[0];
				else
					return DefaultErrorBranch.Value;
			} else
				mode = BranchMode.None;

			return NodeToPred(expr, ctx);
		}

		// Convert a `expr`, which is a tuple, to a Seq predicate
		Pred TranslateToSeq(LNode expr, Context ctx)
		{
			List<Pred> list = expr.Args.Select(node => NodeToPred(node, ctx)).ToList();
			if (list.Count == 1)
				return list[0];
			return new Seq(expr) { List = list };
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
