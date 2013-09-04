//
// Visitors:
//   PredictionAnalysisVisitor
//   PrematchAnalysisVisitor
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using System.Diagnostics;
using Loyc.Collections.Impl;
using Loyc.Utilities;

namespace Loyc.LLParserGenerator
{
	partial class LLParserGenerator
	{
		/// <summary>
		/// Performs prediction analysis using the visitor pattern to visit the 
		/// predicates in a rule. The process starts with <see cref="Generate(Rule)"/>.
		/// </summary>
		/// <remarks>
		/// This class primarily does prediction analysis. It generates prediction
		/// trees, which <see cref="GenerateCodeVisitor"/> then uses to generate 
		/// code. It relies on the "Prediction analysis code" in 
		/// <see cref="LLParserGenerator"/> for the lowest-level analysis tasks.
		/// </remarks>
		protected class PredictionAnalysisVisitor : RecursivePredVisitor
		{
			public PredictionAnalysisVisitor(LLParserGenerator llpg) { LLPG = llpg; }

			LLParserGenerator LLPG;
			IPGCodeGenHelper CSG { get { return LLPG.SnippetGenerator; } }
			Rule _currentRule;
			Alts _currentAlts;
			int _k;

			public void Analyze(Rule rule)
			{
				_currentRule = rule;
				_k = rule.K > 0 ? rule.K : LLPG.DefaultK;
				rule.Pred.Call(this);
			}

			bool _codeBeforeAndWarning = false;
			public override void Visit(AndPred pred)
			{
				if (pred.PreAction != null && !_codeBeforeAndWarning) {
					LLPG.Output(pred.Basis, pred, Warning, 
						"It's poor style to put a code block {} before an and-predicate &{} because the and-predicate normally runs first.");
					_codeBeforeAndWarning = true;
				}
				VisitChildrenOf(pred);
			}

			public override void Visit(Alts alts)
			{
				_currentAlts = alts;
				KthSet[] firstSets = LLPG.ComputeFirstSets(alts);
				alts.PredictionTree = ComputePredictionTree(firstSets);

				if (LLPG.Verbosity > 0)
					for (int i = 0; i < firstSets.Length; i++) {
						if (firstSets[i].Alt == -1 || LLPG.Verbosity > 2)
							LLPG.Output(alts.Basis, alts, Verbose, 
								Localize.From("First set for {0}: {1}",
									firstSets[i].Alt == -1 ? "exit" : "alt #" + (firstSets[i].Alt + 1), firstSets[i]));
					}
							
				if ((LLPG.Verbosity & 2) != 0)
					LLPG.Output(alts.Basis, alts, Verbose, "(unsimplified) " + alts.PredictionTree.ToString());
				
				SimplifyPredictionTree(alts.PredictionTree);
				AddElseCases(alts, alts.PredictionTree);
				_currentAlts = null;

				if ((LLPG.Verbosity & 1) != 0)
					LLPG.Output(alts.Basis, alts, Verbose, alts.PredictionTree.ToString());

				VisitChildrenOf(alts);
			}

			#region ComputePredictionTree() and helpers

			protected PredictionTree ComputePredictionTree(KthSet[] kthSets)
			{
				var children = InternalList<PredictionBranch>.Empty;
				var thisBranch = new List<KthSet>();
				int lookahead = kthSets[0].LA;
				Debug.Assert(kthSets.All(p => p.LA == lookahead));

				IPGTerminalSet covered = CSG.EmptySet;
				for (;;)
				{
					thisBranch.Clear();
					// e.g. given an Alts value of ('0' '0'..'7'+ | '0'..'9'+), 
					// ComputeSetForNextBranch finds the set '0' in the first 
					// iteration (recording both alts in 'thisBranch'), '1'..'9' 
					// on the second iteration, and finally null.
					IPGTerminalSet set = ComputeSetForNextBranch(kthSets, thisBranch, covered);

					if (set == null)
						break;

					if (thisBranch.Count == 1) {
						var branch = thisBranch[0];
						children.Add(new PredictionBranch(set, branch.Alt, covered));
					} else {
						Debug.Assert(thisBranch.Count > 1);
						NarrowDownToSet(thisBranch, set);

						PredictionTreeOrAlt sub;
						if (thisBranch.Any(ks => ks.HasAnyAndPreds))
							sub = ComputeAssertionTree(thisBranch);
						else
							sub = ComputeNestedPredictionTree(thisBranch);
						children.Add(new PredictionBranch(set, sub, covered));
					}

					covered = covered.Union(set);
				}
				return new PredictionTree(lookahead, children, covered);
			}

			private void CountAlt(int alt, Dictionary<int, int> timesUsed)
			{
				int counter;
				timesUsed.TryGetValue(alt, out counter);
				timesUsed[alt] = counter + 1;
			}

			private PredictionTreeOrAlt ComputeNestedPredictionTree(List<KthSet> prevSets)
			{
				Debug.Assert(prevSets.Count > 0);
				int lookahead = prevSets[0].LA;
				
				if (prevSets.Count == 1)
					return (PredictionTreeOrAlt) prevSets[0].Alt;
				else if (lookahead + 1 >= _k) {
					var @default = AmbiguityDetected(prevSets);
					return (PredictionTreeOrAlt) @default.Alt;
				}
				KthSet[] nextSets = LLPG.ComputeNextSets(prevSets);
				var subtree = ComputePredictionTree(nextSets);
				
				return subtree;
			}

			private void NarrowDownToSet(List<KthSet> thisBranch, IPGTerminalSet set)
			{
				// Scans the Transitions of thisBranch, removing cases that are
				// unreachable given the current set and intersecting the reachable 
				// sets with 'set'. This method is needed in rare cases involving 
				// nested Alts, but it is called unconditionally just in case 
				// futher lookahead steps might rely on the results. Here are two
				// examples where it is needed:
				//
				// ( ( &foo 'a' | 'b' 'b') | 'b' 'c' )
				//
				// In this case, a prediction subtree is generated for LA(0)=='b'.
				// Initially, thisBranch will contain a case for (&foo 'a') but it
				// is unreachable given that we know LA(0)=='b', so &foo should not 
				// be tested. This method will remove that case so it'll be ignored.
				//
				// (('a' | 'd' 'd') 't' | ('a'|'o') 'd' 'd') // test suite: NestedAlts()
				// 
				// Without this method, prediction would think that the sequence 
				// 'a' 'd' could match the first alt because it fails to discard the
				// second nested alt ('d' 'd') after matching 'a'.
				//
				// LL(k) prediction still doesn't work perfectly in all cases. For
				// example, this case is predicted incorrectly:
				// 
				// ( ('a' 'b' | 'b' 'a') 'c' | ('b' 'b' | 'a' 'a') 'c' )
				for (int i = 0; i < thisBranch.Count; i++)
					thisBranch[i] = NarrowDownToSet(thisBranch[i], set);
			}
			private KthSet NarrowDownToSet(KthSet kthSet, IPGTerminalSet set)
			{
				kthSet = kthSet.Clone(false);
				var cases = kthSet.Cases;
				for (int i = cases.Count-1; i >= 0; i--)
				{
					cases[i].Set = cases[i].Set.Intersection(set);
					if (cases[i].Set.IsEmptySet)
						cases.RemoveAt(i);
				}
				kthSet.UpdateSet(kthSet.Set.ContainsEOF);
				Debug.Assert(cases.Count > 0 || set.ContainsEOF);
				return kthSet;
			}

			private IPGTerminalSet ComputeSetForNextBranch(KthSet[] kthSets, List<KthSet> thisBranch, IPGTerminalSet covered)
			{
				int i;
				IPGTerminalSet set = null;
				for (i = 0; ; i++)
				{
					if (i == kthSets.Length)
						return null; // done!
					set = kthSets[i].Set.Subtract(covered);
					if (!set.IsEmptySet) {
						if (LLPG.FullLLk)
							set = NarrowDownToOneCase(set, kthSets[i].Cases);
						break;
					}
				}

				thisBranch.Add(kthSets[i]);
				for (i++; i < kthSets.Length; i++)
				{
					var next = set.Intersection(kthSets[i].Set);
					if (!next.IsEmptySet) {
						set = next;
						if (LLPG.FullLLk)
							set = NarrowDownToOneCase(set, kthSets[i].Cases);
						thisBranch.Add(kthSets[i]);
					}
				}

				return set;
			}

			private IPGTerminalSet NarrowDownToOneCase(IPGTerminalSet normalSet, List<Transition> cases)
			{
				if (cases.Count == 1)
					return normalSet; // a small optimization

				IPGTerminalSet narrowSet, next;
				int i;
				for (i = 0; ; i++) {
					if (i == cases.Count) {
						// this happens if normalSet is {EOF} and none of the cases have EOF.
						// (LLLPG puts EOF in all exit branches to prevent infinite loops)
						Debug.Assert(normalSet.ContainsEOF);
						return normalSet;
					}
					if (!(narrowSet = cases[i].Set.Intersection(normalSet)).IsEmptySet)
						break;
				}
				for (i++; i < cases.Count; i++)
					if (!(next = cases[i].Set.Intersection(narrowSet)).IsEmptySet)
						narrowSet = next;
				return narrowSet;
			}

			#endregion

			#region ComputeAssertionTree (used by ComputePredictionTree)

			private PredictionTreeOrAlt ComputeAssertionTree(List<KthSet> alts)
			{
				var children = InternalList<PredictionBranch>.Empty;

				// If any AndPreds show up in all cases, they are irrelevant for
				// prediction and should be ignored.
				var commonToAll = alts.Aggregate(null, (MSet<AndPred> set, KthSet alt) => {
					if (set == null) return (MSet<AndPred>)alt.AndReq;
					set.IntersectWith(alt.AndReq);
					return set;
				});
				return ComputeAssertionTree2(alts, new Set<AndPred>(commonToAll));
			}
			private PredictionTreeOrAlt ComputeAssertionTree2(List<KthSet> alts, Set<AndPred> matched)
			{
				int lookahead = alts[0].LA;
				var children = InternalList<PredictionBranch>.Empty;
				MSet<AndPred> falsified = new MSet<AndPred>();
				// Each KthSet represents a branch of the Alts for which we are 
				// generating a prediction tree; so if we find an and-predicate 
				// that, by failing, will exclude one or more KthSets, that's
				// probably the fastest way to get closer to completing the tree.
				// Any predicate in KthSet.AndReq (that isn't in matched) satisfies
				// this condition.
				var bestAndPreds = alts.SelectMany(alt => alt.AndReq).Where(ap => !matched.Contains(ap)).ToList();
				var altsLeft = alts.Select(alt => alt.Clone(true)).ToList();
				foreach (AndPred andPred in bestAndPreds)
				{
					AutoAddBranchForAndPred(ref children, andPred, altsLeft, matched, falsified);
					if (altsLeft.Count == 0)
						break;
				}
				// Testing any single AndPred will not exclude any KthSets, so
				// we'll proceed the slow way: pick any unmatched AndPred and test 
				// it. If it fails then the Transition(s) associated with it can be 
				// excluded.
				List<AndPred> predsLeft = 
					altsLeft.SelectMany(alt => alt.Cases)
					        .SelectMany(t => t.AndPreds)
					        .Where(ap => !matched.Contains(ap))
					        .Distinct().ToList();
				foreach (var andPred in predsLeft) {
					AutoAddBranchForAndPred(ref children, andPred, altsLeft, matched, falsified);
					if (altsLeft.Count == 0)
						break;
				}

				if (children.Count == 0)
				{
					// If no AndPreds were tested, proceed to the next level of prediction.
					Debug.Assert(falsified.Count == 0);
					return ComputeNestedPredictionTree(altsLeft);
				}
				
				// If there are any "unguarded" cases left after falsifying all 
				// the AndPreds, add a branch for them.
				Debug.Assert(falsified.Count > 0);
				if (altsLeft.Count > 0)
				{
					var final = new PredictionBranch(new Set<AndPred>(), ComputeNestedPredictionTree(altsLeft));
					children.Add(final);
				}
				return new PredictionTree(lookahead, children, null);
			}
			private void AutoAddBranchForAndPred(ref InternalList<PredictionBranch> children, AndPred andPred, List<KthSet> alts, Set<AndPred> matched, MSet<AndPred> falsified)
			{
				if (!falsified.Contains(andPred)) {
					var apSet = GetBuddies(alts, andPred, matched, falsified);
					if (!apSet.IsEmpty) {
						var innerMatched = matched | apSet;
						var result = new PredictionBranch(apSet, ComputeAssertionTree2(alts, innerMatched));
						falsified.UnionWith(apSet);
						RemoveFalsifiedCases(alts, falsified);
						children.Add(result);
					}
				}
			}
			private void RemoveFalsifiedCases(List<KthSet> alts, MSet<AndPred> falsified)
			{
				if (falsified.Count == 0)
					return;

				var results = new List<KthSet>(alts.Count);
				foreach (var alt in alts) {
					alt.Cases.RemoveAll(t => falsified.Overlaps(t.AndPreds));
					alt.UpdateSet(alt.Set.ContainsEOF);
				}
				alts.RemoveAll(alt => alt.Cases.Count == 0);
			}
			private Set<AndPred> GetBuddies(List<KthSet> alts, AndPred ap, Set<AndPred> matched, MSet<AndPred> falsified)
			{
				// Given an AndPred, find any other AndPreds that always appear 
				// together with ap; if any are found, we want to group them 
				// together because doing so will simplify the prediction tree.
				return new Set<AndPred>(
					alts.SelectMany(alt => alt.Cases)
						.Where(trans => trans.AndPreds.Contains(ap))
						.Aggregate(null, (MSet<AndPred> set, Transition trans) => {
							if (set == null) {
								set = new MSet<AndPred>(trans.AndPreds);
								set.ExceptWith(matched);
								set.ExceptWith(falsified);
							}
							set.IntersectWith(trans.AndPreds);
							return set;
						}));
			}
			
			#endregion

			#region Ambiguity handling (not including detection)

			private KthSet AmbiguityDetected(List<KthSet> prevSets)
			{
				if (ShouldReportAmbiguity(prevSets)) {
					IEnumerable<int> arms = prevSets.Select(ks => ks.Alt);
					_currentAlts.AmbiguityReported(arms);

					string format = "Alternatives ({0}) are ambiguous for input such as {1}";
					if (_currentAlts.Mode == LoopMode.Opt && _currentAlts.Arms.Count == 1)
						format = "Optional branch is ambiguous for input such as {1}";
					LLPG.Output(_currentAlts.Basis, _currentAlts, Warning,
						string.Format(format,
							StringExt.Join(", ", prevSets.Select(
								ks => ks.Alt == ExitAlt ? "exit" : (ks.Alt + 1).ToString())),
							GetAmbiguousCase(prevSets)));
				}
				// Return the KthSet representing the branch to use by default.
				// The nongreedy exit branch takes priority; if there isn't one,
				// the lexically first applicable Alt takes priority (bug fix: 
				// prevSets[0] may not be lexically first if the user specified
				// a "default" arm.)
				Debug.Assert (!prevSets.Slice(1).Any(s => s.IsNongreedyExit));
				if (prevSets[0].IsNongreedyExit)
					return prevSets[0];
				return prevSets[prevSets.IndexOfMin(s => (uint)s.Alt)];
			}

			private bool ShouldReportAmbiguity(List<KthSet> prevSets)
			{
				// Look for any and-predicates that are unique to particular 
				// branches. Such predicates can suppress warnings.
				var andPreds = new List<Set<AndPred>>();
				var common = new Set<AndPred>();
				bool first = true;
				foreach (var ks in prevSets) {
					var andSet = new Set<AndPred>();
					for (var ks2 = ks; ks2 != null; ks2 = ks2.Prev)
						andSet = andSet | ks2.AndReq;
					andPreds.Add(andSet);
					common = first ? andSet : andSet & common;
					first = false;
				}
				ulong suppressWarnings = 0;
				for (int i = 0; i < andPreds.Count; i++) {
					if (!(andPreds[i] - common).IsEmpty)
						suppressWarnings |= 1ul << i;
				}

				// Suppress ambiguity with exit if the ambiguity is caused by 
				// reaching the end of a rule that is marked as a "token".
				bool suppressExitWarning = false;
				{
					var ks = prevSets.Where(ks0 => ks0.Alt == ExitAlt).SingleOrDefault();
					if (ks != null && ks.Cases.All(transition => transition.Position.Pred == EndOfToken && transition.PrevPosition == EndOfToken))
						suppressExitWarning = true;
				}

				return _currentAlts.ShouldReportAmbiguity(prevSets.Select(ks => ks.Alt), suppressWarnings, suppressExitWarning);
			}

			/// <summary>Gets an example of an ambiguous input, based on a list of 
			/// two or more ambiguous paths through the grammar.</summary>
			private string GetAmbiguousCase(List<KthSet> lastSets)
			{
				var seq = new List<IPGTerminalSet>();
				IEnumerable<KthSet> kthSets = lastSets;
				for(;;) {
					IPGTerminalSet tokSet = null;
					foreach(KthSet ks in kthSets)
						tokSet = tokSet == null ? ks.Set : tokSet.Intersection(ks.Set);
					if (tokSet == null || tokSet.IsEmptySet)
						break;
					seq.Add(tokSet);
					Debug.Assert(!kthSets.Any(ks => ks.Prev == null));
					kthSets = kthSets.Select(ks => ks.Prev);
				}
				seq.Reverse();
				
				var result = new StringBuilder("«");
				if (seq.All(set => CSG.ExampleChar(set) != null)) {
					StringBuilder temp = new StringBuilder();
					foreach(var set in seq)
						temp.Append(CSG.ExampleChar(set));
					result.Append(G.EscapeCStyle(temp.ToString(), EscapeC.Control, '»'));
				} else {
					result.Append(seq.Select(set => CSG.Example(set)).Join(" "));
				}
				result.Append("» (");
				result.Append(seq.Join(", "));
				result.Append(')');
				return result.ToString();
			}

			#endregion

			/// <summary>Recursively merges adjacent duplicate cases in prediction trees.
			/// The tree is modified in-place, but in case a tree collapses to a single 
			/// alternative, the return value indicates which single alternative.</summary>
			private PredictionTreeOrAlt SimplifyPredictionTree(PredictionTree tree)
			{
				for (int i = 0; i < tree.Children.Count; i++) {
					PredictionBranch pb = tree.Children[i];
					if (pb.Sub.Tree != null)
						pb.Sub = SimplifyPredictionTree(pb.Sub.Tree);
				}
				for (int i = tree.Children.Count-1; i > 0; i--) {
					PredictionBranch a = tree.Children[i-1], b = tree.Children[i];
					if (a.Sub.Equals(b.Sub))
					{
						// Merge a and b
						if (a.Set != null)
							a.Set = a.Set.Union(b.Set);
						a.CombineAndPredsWith(b.AndPreds);
						tree.Children.RemoveAt(i);
					}
				}
				if (tree.Children.Count == 1)
					return tree.Children[0].Sub;
				return tree;
			}

			/// <summary>Extends each level of the prediction tree so that it has 
			/// total coverage. For example, a typicaly prediction tree might have 
			/// branches for 'a'..'z' and '0..'9'; this method will add coverage for 
			/// all other possible inputs. It does this either by adding an error 
			/// branch, or by extending the set handled by the final branch of each 
			/// level.</summary>
			private void AddElseCases(Alts alts, PredictionTree tree)
			{
				foreach (var branch in tree.Children)
					if (branch.Sub.Tree != null)
						AddElseCases(alts, branch.Sub.Tree);

				if (tree.IsAssertionLevel) {
					tree.Children.Last.AndPreds.Clear();
					tree.Children.Last.AndPreds.Add(Set<AndPred>.Empty);
				} else if (!tree.TotalCoverage.ContainsEverything) {
					var rest = tree.TotalCoverage.Inverted();
					bool noDefault = alts.DefaultArm == null ? LLPG.NoDefaultArm : alts.DefaultArm.Value == -1;
					if (noDefault)
						tree.Children.Add(new PredictionBranch(rest, new PredictionTreeOrAlt { Alt = ErrorAlt }, tree.TotalCoverage));
					else
						tree.Children.Last.Set = tree.Children.Last.Set.Union(rest);
				}
			}
		}



		/// <summary>Figures out which terminals and and-predicates are "prematched".
		/// A prematched "Match()" call can be replaced with "Skip()" or "MatchAny()"
		/// in the generated code, a prematched Check() can be eliminated, to improve 
		/// performance of the generated code.</summary>
		class PrematchAnalysisVisitor : RecursivePredVisitor
		{
			static readonly DList<Prematched> Empty = new DList<Prematched>();
			
			LLParserGenerator LLPG;
			public PrematchAnalysisVisitor(LLParserGenerator llpg) { LLPG = llpg; }

			public void Analyze(Rule rule)
			{
				rule.Pred.Call(this);

				// For rules that are not marked "private", we must apply "empty" 
				// prematch information in case the rule is called directly or from 
				// outside the known ruleset, to force use of Match() at rule start.
				if (!rule.IsPrivate)
					_apply.ApplyPrematchData(rule.Pred, Empty);
			}

			public override void Visit(Alts alts)
			{
				// Traverse each prediction tree find branches taken and save prematch info
				ScanTree(alts.PredictionTree, alts, new DList<Prematched>());
				VisitChildrenOf(alts);
			}

			class Prematched
			{
				public IPGTerminalSet Terminals; // the current LA(i) is verified to be in this set
				public MSet<AndPred> AndPreds = new MSet<AndPred>(); // these and-preds are verified true at this location
			}

			void ScanTree(PredictionTree tree, Alts alts, DList<Prematched> path)
			{
				if (tree.IsAssertionLevel) {
					Debug.Assert(path.Count == tree.Lookahead+1);
					var pm = path.Last;
					foreach (PredictionBranch b in tree.Children)
					{
						var old = pm.AndPreds.Clone();
						var verified = Enumerable.Aggregate(b.AndPreds, (set1, set2) => (set1.Union(set2))); // usually empty if more than one
						pm.AndPreds.UnionWith(verified);

						if (b.Sub.Tree != null) {
							ScanTree(b.Sub.Tree, alts, path);
						} else {
							Debug.Assert(b.Sub.Alt != ErrorAlt);
							if (b.Sub.Alt == ExitAlt)
								_apply.ApplyPrematchData(alts.Next, path);
							else
								_apply.ApplyPrematchData(alts.Arms[b.Sub.Alt], path);
						}
						pm.AndPreds = old;
					}
				} else {
					var pm = new Prematched();
					path.Add(pm);
					bool haveErrorBranch = LLPG.NeedsErrorBranch(tree, alts);

					for (int i = 0; i < tree.Children.Count; i++) {
						PredictionBranch b = tree.Children[i];
						IPGTerminalSet set = b.Set;
						if (!haveErrorBranch && i + 1 == tree.Children.Count)
							// Add all the default cases
							set = set.Union(tree.TotalCoverage.Inverted());
						pm.Terminals = set;
						if (b.Sub.Tree != null) {
							ScanTree(b.Sub.Tree, alts, path);
						} else {
							if (b.Sub.Alt == ExitAlt)
								_apply.ApplyPrematchData(alts.Next, path);
							else if (b.Sub.Alt != ErrorAlt)
								_apply.ApplyPrematchData(alts.Arms[b.Sub.Alt], path);
						}
					}
					path.PopLast();
				}
			}

			ApplyPrematchVisitor _apply = new ApplyPrematchVisitor();
			class ApplyPrematchVisitor : PredVisitor
			{
				bool _reachedInnerAlts;
				DList<Prematched> _path;
				int _index;


				public void ApplyPrematchData(Pred pred, DList<Prematched> path)
				{
					_path = path;
					_index = 0;
					_reachedInnerAlts = false;
					pred.Call(this);
				}

				public override void Visit(Gate gate)
				{
					gate.Match.Call(this);
				}
				public override void Visit(Alts alts)
				{
					bool stop = true;
					_reachedInnerAlts = true;
					if (alts.Mode == LoopMode.None) {
						stop = false;
						int startIndex = _index;
						int length = -1;
						
						foreach (var p in alts.Arms) {
							_index = startIndex;
							p.Call(this);
							
							int newLen = _index - startIndex;
							if (length == -1)
								length = newLen;
							else if (length != newLen)
								stop = true;
						}
					}
					// stop prematching after a variable-length Alts (including any loop)
					if (stop)
						_index = int.MaxValue;
				}
				public override void Visit(Seq seq)
				{
					foreach (var pred in seq.List) {
						pred.Call(this);
						if (_index >= _path.Count && (_index == int.MaxValue || _reachedInnerAlts))
							break;
					}
				}
				public override void Visit(TerminalPred term)
				{
					if (_index < _path.Count) {
						if (term.Prematched != false) {
							bool pm = _path[_index].Terminals.IsSubsetOf(term.Set);
							if (pm || !_reachedInnerAlts)
								term.Prematched = pm;
						}
						_index++;
					} else if (!_reachedInnerAlts) {
						term.Prematched = false;
					}
				}
				public override void Visit(AndPred and)
				{
					if (_index < _path.Count) {
						if (and.Prematched != false) {
							bool pm = _path[_index].AndPreds.Contains(and);
							if (pm || !_reachedInnerAlts)
								and.Prematched = pm;
						}
					} else if (!_reachedInnerAlts) {
						and.Prematched = false;
					}
				}
				public override void Visit(RuleRef rref)
				{
					var rule = rref.Rule;
					if (rule.IsPrivate)
						rule.Pred.Call(this);
				}
			}
		}
	}
}
