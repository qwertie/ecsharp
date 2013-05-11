using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc;
using Loyc.Math;
using Loyc.CompilerCore;
using GreenNode = Loyc.Syntax.LNode;
using Node = Loyc.Syntax.LNode;

namespace Loyc.LLParserGenerator
{
	using S = ecs.CodeSymbols;
	using Loyc.Collections;
	using Loyc.Syntax;

	/// <summary>Standard code generator for character/integer input streams
	/// and is the default code generator for <see cref="LLParserGenerator"/>.</summary>
	class PGCodeGenForIntStream : PGCodeSnippetGeneratorBase
	{
		public const int EOF_int = PGIntSet.EOF_int;

		public override IPGTerminalSet EmptySet
		{
			get { return PGIntSet.Empty; }
		}

		protected override Node GenerateTest(IPGTerminalSet set, Node subject, Symbol setName)
		{
			return ((PGIntSet)set).GenerateTest(subject, setName);
		}
		protected override Node GenerateSetDecl(IPGTerminalSet set, Symbol setName)
		{
			return ((PGIntSet)set).GenerateSetDecl(setName);
		}

		public override Node GenerateMatch(IPGTerminalSet set_)
		{
			var set = (PGIntSet)set_;
			if (set.Complexity(2, 3, !set.IsInverted) <= 6) {
				Node call;
				if (set.Complexity(1, 2, true) > set.Count) {
					// Use MatchRange or MatchExceptRange
					call = F.Call(set.IsInverted ? _MatchExceptRange : _MatchRange);
					for (int i = 0; i < set.Count; i++) {
						if (!set.IsInverted || set[i].Lo != EOF_int || set[i].Hi != EOF_int) {
							call.Args.Add((Node)set.MakeLiteral(set[i].Lo));
							call.Args.Add((Node)set.MakeLiteral(set[i].Hi));
						}
					}
				} else {
					// Use Match or MatchExcept
					call = F.Call(set.IsInverted ? _MatchExcept : _Match);
					for (int i = 0; i < set.Count; i++) {
						var r = set[i];
						for (int c = r.Lo; c <= r.Hi; c++) {
							if (!set.IsInverted || c != EOF_int)
								call.Args.Add((Node)set.MakeLiteral(c));
						}
					}
				}
				return call;
			}

			var setName = GenerateSetDecl(set_);
			return F.Call(_Match, F.Id(setName));
		}

		public override GreenNode LAType()
		{
			return F.Int32;
		}

		/// <summary>Used to help decide whether a "switch" or an if-else chain 
		/// will be used. This property specifies the cost of the simplest "if" 
		/// test such as "if (la0 == 'x')", where "case 'x':" has a cost of 1.</summary>
		protected virtual int IfToSwitchCostRatio { get { return 5; } }
		/// <summary>Used to help decide whether a "switch" or an if-else chain 
		/// will be used for prediction. This is the starting cost of a switch 
		/// (the starting cost of an if-else chain is set to zero).</summary>
		protected virtual int BaseCostForSwitch { get { return 10; } }
		/// <summary>Maximum cost assigned to a single "if" test in an if-else chain.</summary>
		protected virtual int MaxCostPerIf { get { return 40; } }

		public override bool ShouldGenerateSwitch(IPGTerminalSet[] sets, bool needErrorBranch, HashSet<int> casesToInclude)
		{
			int Ratio = IfToSwitchCostRatio, MaxCostPerIf = this.MaxCostPerIf;

			// Compute scores
			PGIntSet covered = PGIntSet.Empty;
			int[] score = new int[sets.Length - (needErrorBranch ? 0 : 1)]; // positive when switch is preferred
			for (int i = 0; i < score.Length; i++) {
				Debug.Assert(sets[i].Subtract(covered).Equals(sets[i]));
				var intset = (PGIntSet)sets[i];
				if (intset != null) {
					covered = covered.Union(intset);

					int switchCost = (int)System.Math.Min(1 + intset.Size, 1000000);
					int ifCost = System.Math.Min(intset.Complexity(Ratio, Ratio * 2, true), MaxCostPerIf);
					score[i] = ifCost - switchCost;
				} else {
					// Any other type of set is not supported in the switch()
					score[i] = -1000000;
				}
			}

			// Consider highest scores first to figure out whether switch is 
			// justified, and which branches should be expressed with "case"s.
			bool should = false;
			int switchScore = -BaseCostForSwitch;
			for (; ; ) {
				int maxIndex = score.IndexOfMax(), maxScore = score[maxIndex];
				switchScore += maxScore;
				if (switchScore > 0)
					should = true;
				else if (maxScore < 0)
					break;
				casesToInclude.Add(maxIndex);
				score[maxIndex] = -1000000;
			}
			return should;
		}

		public override Node GenerateSwitch(IPGTerminalSet[] branchSets, Node[] branchCode, HashSet<int> casesToInclude, Node defaultBranch, GreenNode laVar)
		{
			Debug.Assert(branchSets.Length == branchCode.Length);

			Node @switch = F.Call(S.Switch, (Node)laVar);
			RWList<LNode> stmts = new RWList<Node>();
			for (int i = 0; i < branchSets.Length; i++) {
				if (!casesToInclude.Contains(i))
					continue;

				// Generate all the needed cases
				var intset = (PGIntSet)branchSets[i];
				foreach (IntRange range in intset) {
					for (int ch = range.Lo; ch <= range.Hi; ch++) {
						bool isChar = intset.IsCharSet && (char)ch == ch;
						stmts.Add(F.Call(S.Case, F.Literal(isChar ? (object)(char)ch : (object)ch)));
						if (stmts.Count > 65535) // sanity check
							throw new InvalidOperationException("switch is too large to generate");
					}
				}

				AddSwitchHandler(branchCode[i], stmts);
			}
			if (!defaultBranch.IsSymbolNamed(S.Missing)) {
				stmts.Add(F.Call(S.Label, F.Id(S.Default)));
				AddSwitchHandler(defaultBranch, stmts);
			}

			@switch = @switch.PlusArg(F.Braces(stmts.ToRVList()));
			return @switch;
		}
		private void AddSwitchHandler(Node branch, RWList<LNode> stmts)
		{
			stmts.SpliceAdd(branch, S.List);
			if (!branch.Calls(S.Goto, 1))
				stmts.Add(F.Call(S.Break));
		}
	}
}
