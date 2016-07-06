using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Syntax;
using Loyc.Collections;
using Loyc.Ecs;

namespace Loyc.LLParserGenerator
{
	using S = CodeSymbols;

	/// <summary>Helper class invoked just after <see cref="StageTwoParser"/>. Its 
	/// job is to create variables referenced by labels (label:item) and by code 
	/// blocks ($RuleName), to modify the code blocks to remove the $ operator, and 
	/// to update the ResultSaver of each labeled predicate. Also supports $result.
	/// </summary>
	/// <remarks>Labels and $substitutions can also be used with <see cref="Pred"/>s 
	/// constructed by hand, except that referencing tokens directly from code 
	/// blocks (e.g. $'#') won't work unless the corresponding grammar Pred has
	/// a Pred.Basis is an equal syntax tree.</remarks>
	class AutoValueSaverVisitor : RecursivePredVisitor
	{
		static readonly Symbol _result = GSymbol.Get("result");
		static readonly LNode _resultId = LNode.Id(_result);

		public static void Run(Rule rule, IMessageSink sink, IDictionary<Symbol, Rule> rules, IPGCodeGenHelper codeGen)
		{
			// 1. Scan for a list of code blocks that use $labels, and a list of rules referenced.
			var data = new DataGatheringVisitor(rules, rule);
			if (data.RulesReferenced.Count != 0 || data.OtherReferences.Count != 0 || data.ProperLabels.Count != 0)
			{
				var vsv = new AutoValueSaverVisitor(data, sink, rules, codeGen);
				// 2. Create $result variable if it was used
				// 3. Scan for predicates with labels, and RuleRefs referenced by 
				//    code blocks. For each such predicate, generate a variable at 
				//    the beginning of the rule and set the ResultSaver.
				vsv.Process(rule);
				// 4. Replace recognized $substitutions in code blocks
				vsv.ReplaceSubstitutionsInCodeBlocks();
			}
		}

		class DataGatheringVisitor : RecursivePredVisitor
		{
			// code blocks with $labels
			IDictionary<Symbol, Rule> _rules;
			public DataGatheringVisitor(IDictionary<Symbol, Rule> rules, Rule rule)
				{ _rules = rules; Visit(rule.Pred); }
			// List of predicates that are using {...$substitution...}
			public HashSet<Pred> PredsUsingSubstitution = new HashSet<Pred>();
			// Rules referenced by code blocks
			public HashSet<Rule> RulesReferenced = new HashSet<Rule>();
			// Labels, token sets, and unidentified things referenced by code blocks
			// The integer counts the number of times that an unlabeled thing, that 
			// was referenced by code block, appears in the grammar.
			public Dictionary<LNode, int> OtherReferences = new Dictionary<LNode, int>();
			// Labels encountered in predicates; the bool indicates whether ':' was used
			public Dictionary<Symbol, bool> ProperLabels = new Dictionary<Symbol, bool>();

			#region Step 1: data gathering

			public override void Visit(AndPred pred)
			{
				base.Visit(pred);
				VisitCode(pred, pred.Pred as LNode);
			}
			public override void VisitOther(Pred pred)
			{
				VisitCode(pred, pred.PreAction);
				VisitCode(pred, pred.PostAction);
				if (pred.VarLabel != null)
					ProperLabels[pred.VarLabel] = ProperLabels.TryGetValue(pred.VarLabel, false) | pred.ResultSaver == null;
			}
			void VisitCode(Pred pred, LNode code)
			{
				if (code == null) return;
				code.ReplaceRecursive(node => {
					if (node.Calls(S.Substitute, 1)) {
						var arg = node.Args[0];
						PredsUsingSubstitution.Add(pred);
						if (arg.IsId && _rules.ContainsKey(arg.Name))
							RulesReferenced.Add(_rules[arg.Name]);
						else
							OtherReferences[arg] = 0;
					}
					return null; // search only, no replace
				});
			}

			#endregion
		}

		static LNodeFactory F = new LNodeFactory(new EmptySourceFile("LLLPG $substitution analyzer"));

		IMessageSink _sink;
		IDictionary<Symbol, Rule> _rules;
		DataGatheringVisitor _data;
		IPGCodeGenHelper _codeGen;

		AutoValueSaverVisitor(DataGatheringVisitor data, IMessageSink sink, IDictionary<Symbol, Rule> rules, IPGCodeGenHelper codeGen)
			{ _data = data; _sink = sink; _rules = rules; _codeGen = codeGen; }

		// A map from variable names to a Pair<variable type, initializer statement>
		Dictionary<Symbol,Pair<LNode,LNode>> _newVarInitializers = new Dictionary<Symbol,Pair<LNode,LNode>>();

		#region Step 2: generate variables and set remaining ResultSavers

		public override void Visit(RuleRef pred)
		{
			var retType = pred.Rule.ReturnType;
			if (pred.VarLabel != null)
				MaybeCreateVariableFor(pred, pred.VarLabel, retType);
			else if (_data.RulesReferenced.Contains(pred.Rule))
				MaybeCreateVariableFor(pred, PickVarNameForRuleName(pred.Rule.Name), retType);
		}

		public override void VisitOther(Pred pred)
		{
			LNode basis = pred.Basis;
			if (pred.VarLabel != null) {
				basis = null;
				MaybeCreateVariableFor(pred, pred.VarLabel, _codeGen.TerminalType);
			}
			// If code blocks refer to this predicate's label or basis node, tally
			// the reference and create a variable decl for it if we haven't yet.
			// TODO: bug here: LNode equality depends on trivia. 
			//       Should we change default definition of LNode equality?
			int predCounter;
			if (_data.OtherReferences.TryGetValueSafe(basis, out predCounter)) {
				_data.OtherReferences[basis] = predCounter + 1;
				MaybeCreateVariableFor(pred, PickVarNameForLNode(basis), _codeGen.TerminalType);
			}
		}
		private void MaybeCreateVariableFor(Pred pred, Symbol varName, LNode primType)
		{
			if (pred.ResultSaver != null)
				return;
			if (primType == null) {
				primType = F.Object;
				_sink.Write(Severity.Error, pred, Localize.Localized("The type of this expression is unknown (did you set LLLPG's 'terminalType'  option?)"));
			}
			LNode type = primType, oldType;
			if (pred.VarIsList)
				type = _codeGen.GetListType(primType);
			if (!_newVarInitializers.ContainsKey(varName))
				_newVarInitializers[varName] = Pair.Create(type, _codeGen.MakeInitializedVarDecl(primType, pred.VarIsList, varName));
			else if (!(oldType = _newVarInitializers[varName].A).Equals(type))
				_sink.Write(Severity.Error, pred, Localize.Localized(
					"Type mismatch: Variable '{0}' was generated earlier with type {1}, but this predicate expects {2}.",
					varName, oldType, type));
			pred.ResultSaver = Pred.GetStandardResultSaver(F.Id(varName),
				pred.VarIsList ? S.AddAssign : S.Assign);
		}

		#endregion
		
		static Symbol PickVarNameForRuleName(Symbol name)
			{ return GSymbol.Get("got_" + name); }

		// Converts the subject of a substitution expr like $'*' to a valid ident-
		// ifier, under the assumption that it doesn't refer to a rule or label.
		static Symbol PickVarNameForLNode(LNode label)
		{
			if (label.IsId) {
				// Ignore the predefined special substitutions $LA and $LI
				//if (label.Name.Name == "LA" || label.Name.Name == "LI")
				//	return null;
				return GSymbol.Get("tok_" + label.Name);
			} else if (label.IsLiteral) {
				return LiteralToVarName(label.Value);
			} else if (label.Calls(S.Dot, 2))
				return GSymbol.Get("tok__" + label.Args[1].Name);
			else if (label.Calls(S.DotDot, 2) || label.Calls(S.DotDotDot, 2))
				return GSymbol.Get(PickVarNameForLNode(label[0]).Name + "_" + PickVarNameForLNode(label[1]).Name);
			else // can't return null
				return GSymbol.Get(label.GetHashCode().ToString());
		}
		static Symbol LiteralToVarName(object literal)
		{
			string prefix = literal is char ? "ch" : "lit";
			return GSymbol.Get(prefix + LiteralToIdent(literal));
		}
		static string LiteralToIdent(object literal)
		{
			return EcsValidators.SanitizeIdentifier((literal ?? "null").ToString());
		}

		// Step 3
		void Process(Rule rule)
		{
			// Create $result variable if it was used
			bool usingResult = _data.OtherReferences.ContainsKey(_resultId) || _data.ProperLabels.TryGetValue(_result, false);
			if (usingResult && rule.ReturnType != null)
			{
				_data.ProperLabels[_result] = true;
				var type = rule.ReturnType;
				_newVarInitializers[_result] = Pair.Create(type, _codeGen.MakeInitializedVarDecl(type, false, _result));
			}

			Visit(rule.Pred);

			if (_newVarInitializers.Count != 0)
			{
				var decls = _newVarInitializers.OrderBy(p => p.Key.Name).Select(p => p.Value.B);
				LNode decls2 = F.Call(S.Splice, decls);
				rule.Pred.PreAction = Pred.MergeActions(decls2, rule.Pred.PreAction);
				if (usingResult)
					rule.Pred.PostAction = Pred.MergeActions(rule.Pred.PostAction, F.Call(S.Return, _resultId));
			}
		}

		#region Step 4: perform code substitutions

		internal void ReplaceSubstitutionsInCodeBlocks()
		{
			foreach (var pred in _data.PredsUsingSubstitution)
			{
				pred.PreAction = ReplaceSubstitutionsIn(pred.PreAction);
				pred.PostAction = ReplaceSubstitutionsIn(pred.PostAction);
				var and = pred as AndPred;
				if (and != null && and.Pred is LNode)
					and.Pred = ReplaceSubstitutionsIn((LNode)and.Pred);
			}
		}
		LNode ReplaceSubstitutionsIn(LNode code)
		{
			if (code == null) return null;
			return code.ReplaceRecursive(node =>
			{
				if (node.Calls(S.Substitute, 1))
				{ // found $subst_expr
					var label = node.Args[0];
					if (label.IsId)
					{
						if (_data.ProperLabels.ContainsKey(label.Name))
							return label;
						else if (_rules.ContainsKey(label.Name))
							return F.Id(PickVarNameForRuleName(label.Name));
					}
					if (_data.OtherReferences.TryGetValue(label, -1) > 0)
					{
						return F.Id(PickVarNameForLNode(label));
					}
					// Do not change the code in other cases (e.g. the code 
					// block might contain $LI/$LA, handled in a later stage)
				}
				return null;
			});
		}

		#endregion
	}
}
