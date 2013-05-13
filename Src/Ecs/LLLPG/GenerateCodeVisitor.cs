using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.CompilerCore;
using Loyc.Collections;
using Loyc.Collections.Impl;
using GreenNode = Loyc.Syntax.LNode;
using Node = Loyc.Syntax.LNode;

namespace Loyc.LLParserGenerator
{
	using S = ecs.CodeSymbols;
	using System.Diagnostics;
	using Loyc.Utilities;
	using Loyc.Syntax;

	partial class LLParserGenerator
	{
		/// <summary>
		/// Directs code generation using the visitor pattern to visit the 
		/// predicates in a rule. The process starts with <see cref="Generate(Rule)"/>.
		/// </summary>
		/// <remarks>
		/// This class is in charge of both code generation and prediction analysis.
		/// It relies on <see cref="IPGCodeSnippetGenerator"/> for low-level code 
		/// generation tasks, and it relies on the "Prediction analysis code" in 
		/// <see cref="LLParserGenerator"/> for low-level analysis tasks.
		/// </remarks>
		protected class GenerateCodeVisitor : PredVisitor
		{
			public LLParserGenerator LLPG;
			public LNodeFactory F;
			Rule _currentRule;
			Pred _currentPred;
			RWList<LNode> _classBody; // Location where we generate terminal sets
			RWList<Node> _target; // List of statements method being generated
			ulong _laVarsNeeded;
			// # of alts using gotos -- a counter is used to make unique labels
			int _separatedMatchCounter = 0, _stopLabelCounter = 0;
			int _k;

			public GenerateCodeVisitor(LLParserGenerator llpg)
			{
				LLPG = llpg;
				F = new LNodeFactory(llpg._sourceFile);
				_classBody = llpg._classBody;
			}

			IPGCodeSnippetGenerator CSG { get { return LLPG.SnippetGenerator; } }

			public void Generate(Rule rule)
			{
				CSG.BeginRule(rule);
				_currentRule = rule;
				_k = rule.K > 0 ? rule.K : LLPG.DefaultK;
				//Node body = _target = F.Call(S.Braces);
				_target = new RWList<Node>();
				_laVarsNeeded = 0;
				_separatedMatchCounter = _stopLabelCounter = 0;
				
				Visit(rule.Pred);

				if (_laVarsNeeded != 0) {
					Node laVars = F.Call(S.Var, CSG.LAType());
					for (int i = 0; _laVarsNeeded != 0; i++, _laVarsNeeded >>= 1)
						if ((_laVarsNeeded & 1) != 0)
							laVars = laVars.PlusArg(F.Id("la" + i.ToString()));
					_target.Insert(0, laVars);
				}

				Node ruleMethod = rule.CreateMethod(F.Braces(_target.ToRVList()));
				_classBody.Add(ruleMethod);
			}

			new public void Visit(Pred pred)
			{
				if (pred.PreAction != null)
					_target.SpliceAdd(pred.PreAction, S.List);
				var old = _currentPred;
				_currentPred = pred;
				pred.Call(this);
				_currentPred = old;
				if (pred.PostAction != null)
					_target.SpliceAdd(pred.PostAction, S.List);
			}

			void VisitWithNewTarget(Pred toBeVisited, RWList<LNode> target)
			{
				var old = _target;
				_target = target;
				Visit(toBeVisited);
				_target = old;
			}

			/// <summary>
			/// Visit(Alts) is the most important method in this class. It generates 
			/// all prediction code, which is the majority of the code in a parser.
			/// </summary>
			public override void Visit(Alts alts)
			{
				var firstSets = LLPG.ComputeFirstSets(alts);
				var timesUsed = new Dictionary<int, int>();
				PredictionTree tree = ComputePredictionTree(firstSets, timesUsed);

				SimplifyPredictionTree(tree);

				GenerateCodeForAlts(alts, timesUsed, tree);
			}

			#region PredictionTree class and ComputePredictionTree()

			/// <summary>A <see cref="PredictionTree"/> or a single alternative to assume.</summary>
			protected struct PredictionTreeOrAlt : IEquatable<PredictionTreeOrAlt>
			{
				public static implicit operator PredictionTreeOrAlt(PredictionTree t) { return new PredictionTreeOrAlt { Tree = t }; }
				public static implicit operator PredictionTreeOrAlt(int alt) { return new PredictionTreeOrAlt { Alt = alt }; }
				public PredictionTree Tree;
				public int Alt; // used if Tree==null

				public override string ToString()
				{
					return Tree != null ? Tree.ToString() : string.Format("alt #{0}", Alt);
				}
				public bool Equals(PredictionTreeOrAlt other)
				{
					if (Tree == null)
						return other.Tree == null && Alt == other.Alt;
					else
						return Tree.Equals(other.Tree);
				}
			}

			/// <summary>An abstract representation of a prediction tree, which 
			/// will be transformed into prediction code. PredictionTree has a list
			/// of <see cref="PredictionBranch"/>es at a particular level of lookahead.
			/// </summary><remarks>
			/// This represents the final result of lookahead analysis, in contrast 
			/// to the <see cref="KthSet"/> class which is lower-level and 
			/// represents specific transitions in the grammar. A single 
			/// branch in a prediction tree could be derived from a single case 
			/// in a KthSet, or it could represent several different cases from
			/// one or more different KthSets.
			/// </remarks>
			protected class PredictionTree : IEquatable<PredictionTree>
			{
				public PredictionTree(int la, InternalList<PredictionBranch> children, IPGTerminalSet coverage)
				{
					Lookahead = la;
					Children = children;
					TotalCoverage = coverage;
				}
				public InternalList<PredictionBranch> Children = InternalList<PredictionBranch>.Empty;
				// only used if Children is empty. Alt=0 for first alternative, -1 for exit
				public IPGTerminalSet TotalCoverage; // null for an assertion level
				public int Lookahead; // starts at 0 for first terminal of lookahead

				public bool IsAssertionLevel { get { return TotalCoverage == null; } }

				public override string ToString()
				{
					var s = new StringBuilder(
						string.Format(IsAssertionLevel ? "test and-predicates at LA({0}):" : "test LA({0}):", Lookahead));
					for (int i = 0; i < Children.Count; i++) {
						s.Append("\n  ");
						s.Append(Children[i].ToString().Replace("\n", "\n  "));
					}
					return s.ToString();
				}
				public bool Equals(PredictionTree other)
				{
					if (other == null || Lookahead != other.Lookahead || Children.Count != other.Children.Count)
						return false;
					for (int i = 0; i < Children.Count; i++)
						if (!Children[i].Equals(other.Children[i]))
							return false;
					return true;
				}

				internal bool NeedsLaVar()
				{
					return !IsAssertionLevel || Children.Any(branch => branch.AndPreds.Any(ap => ap.PredUsesLA));
				}
			}

			/// <summary>Represents one branch (if statement or case) in a prediction tree.</summary>
			/// <remarks>
			/// For example, code like 
			/// <code>if (la0 == 'a' || la0 == 'A') { code for first alternative }</code>
			/// is represented by a PredictionBranch with <c>Set = [aA]</c> and 
			/// <c>Sub.Alt = 0.</c> A single prediction branch may (or may not)
			/// represent multiple alternatives, and contain nested subtrees.
			/// </remarks>
			protected class PredictionBranch : IEquatable<PredictionBranch>
			{
				public PredictionBranch(Set<AndPred> andPreds, PredictionTreeOrAlt sub)
				{
					AndPreds = andPreds;
					Sub = sub;
				}
				public PredictionBranch(IPGTerminalSet set, PredictionTreeOrAlt sub, IPGTerminalSet covered)
				{
					Set = set;
					Sub = sub;
					Covered = covered;
				}

				public IPGTerminalSet Set;    // used in standard prediction levels
				public Set<AndPred> AndPreds; // used in assertion levels

				public PredictionTreeOrAlt Sub;
				
				public IPGTerminalSet Covered;

				public override string ToString() // for debugging
				{
					string andPreds = StringExt.Join("", AndPreds);
					string set = Set == null ? "" : Set.ToString();
					if (andPreds == "" && (set == "" || set == "[^]"))
						return string.Format("otherwise, {0}", Sub.ToString());
					else
						return string.Format("when {0} {1}, {2}", andPreds, set, Sub.ToString());
				}
				public bool Equals(PredictionBranch other)
				{
					return other != null 
						&& Set == null ? other.Set == null : Set.Equals(other.Set) 
						&& other.AndPreds.SetEquals(AndPreds) 
						&& other.Sub.Equals(Sub);
				}
			}
			
			protected PredictionTree ComputePredictionTree(KthSet[] kthSets, Dictionary<int, int> timesUsed)
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
						children.Add(new PredictionBranch(branch.Set, branch.Alt, covered));
						CountAlt(branch.Alt, timesUsed);
					} else {
						Debug.Assert(thisBranch.Count > 1);
						NarrowDownToSet(thisBranch, set);

						PredictionTreeOrAlt sub;
						if (thisBranch.Any(ks => ks.HasAnyAndPreds))
							sub = ComputeAssertionTree(thisBranch, timesUsed);
						else
							sub = ComputeNestedPredictionTree(thisBranch, timesUsed);
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

			private PredictionTreeOrAlt ComputeNestedPredictionTree(List<KthSet> prevSets, Dictionary<int, int> timesUsed)
			{
				Debug.Assert(prevSets.Count > 0);
				int lookahead = prevSets[0].LA;
				if (prevSets.Count == 1 || lookahead + 1 >= _k)
				{
					if (prevSets.Count > 1 && ShouldReportAmbiguity(prevSets))
					{
						Debug.Assert(_currentPred is Alts);
						IEnumerable<int> arms = prevSets.Select(ks => ks.Alt);
						((Alts)_currentPred).AmbiguityReported(arms);

						string format = "Alternatives ({0}) are ambiguous for input such as {1}";
						if (((Alts)_currentPred).Mode == LoopMode.Opt && ((Alts)_currentPred).Arms.Count == 1)
							format = "Optional branch is ambiguous for input such as {1}";
						LLPG.Output(_currentPred.Basis, _currentPred, Warning,
							string.Format(format,
								StringExt.Join(", ", prevSets.Select(
									ks => ks.Alt == -1 ? "exit" : (ks.Alt + 1).ToString())),
								GetAmbiguousCase(prevSets)));
					}
					var @default = prevSets[0];
					CountAlt(@default.Alt, timesUsed);
					return (PredictionTreeOrAlt) @default.Alt;
				}
				KthSet[] nextSets = LLPG.ComputeNextSets(prevSets);
				var subtree = ComputePredictionTree(nextSets, timesUsed);
				
				return subtree;
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
					var ks = prevSets.Where(ks0 => ks0.Alt == -1).SingleOrDefault();
					if (ks != null && ks.Cases.All(transition => transition.Position.Pred == EndOfToken && transition.PrevPosition == EndOfToken))
						suppressExitWarning = true;
				}

				return ((Alts)_currentPred).ShouldReportAmbiguity(prevSets.Select(ks => ks.Alt), suppressWarnings, suppressExitWarning);
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
					if (tokSet == null)
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
				kthSet = kthSet.Clone();
				var cases = kthSet.Cases;
				for (int i = cases.Count-1; i >= 0; i--)
				{
					cases[i].Set = cases[i].Set.Intersection(set);
					if (cases[i].Set.IsEmptySet)
						cases.RemoveAt(i);
				}
				kthSet.UpdateSet(kthSet.Set.ContainsEOF);
				Debug.Assert(cases.Count > 0);
				return kthSet;
			}

			private static IPGTerminalSet ComputeSetForNextBranch(KthSet[] kthSets, List<KthSet> thisBranch, IPGTerminalSet covered)
			{
				int i;
				IPGTerminalSet set = null;
				for (i = 0; ; i++)
				{
					if (i == kthSets.Length)
						return null; // done!
					set = kthSets[i].Set.Subtract(covered);
					if (!set.IsEmptySet)
						break;
				}

				thisBranch.Add(kthSets[i]);
				for (i++; i < kthSets.Length; i++)
				{
					var next = set.Intersection(kthSets[i].Set);
					if (!next.IsEmptySet)
					{
						set = next;
						thisBranch.Add(kthSets[i]);
					}
				}

				return set;
			}

			private PredictionTreeOrAlt ComputeAssertionTree(List<KthSet> alts, Dictionary<int, int> timesUsed)
			{
				var children = InternalList<PredictionBranch>.Empty;

				// If any AndPreds show up in all cases, they are irrelevant for
				// prediction and should be ignored.
				var commonToAll = alts.Aggregate(null, (HashSet<AndPred> set, KthSet alt) => {
					if (set == null) return alt.AndReq.ClonedHashSet();
					set.IntersectWith(alt.AndReq.InternalSet);
					return set;
				});
				return ComputeAssertionTree2(alts, new Set<AndPred>(commonToAll), timesUsed);
			}
			private PredictionTreeOrAlt ComputeAssertionTree2(List<KthSet> alts, Set<AndPred> matched, Dictionary<int, int> timesUsed)
			{
				int lookahead = alts[0].LA;
				var children = InternalList<PredictionBranch>.Empty;
				HashSet<AndPred> falsified = new HashSet<AndPred>();
				// Each KthSet represents a branch of the Alts for which we are 
				// generating a prediction tree; so if we find an and-predicate 
				// that, by failing, will exclude one or more KthSets, that's
				// probably the fastest way to get closer to completing the tree.
				// Any predicate in KthSet.AndReq (that isn't in matched) satisfies
				// this condition.
				var bestAndPreds = alts.SelectMany(alt => alt.AndReq).Where(ap => !matched.Contains(ap)).ToList();
				foreach (AndPred andPred in bestAndPreds)
				{
					if (!falsified.Contains(andPred))
						children.Add(MakeBranchForAndPred(andPred, alts, matched, timesUsed, falsified));
				}
				// Testing any single AndPred will not exclude any KthSets, so
				// we'll proceed the slow way: pick any unmatched AndPred and test 
				// it. If it fails then the Transition(s) associated with it can be 
				// excluded.
				foreach (Transition trans in
					alts.SelectMany(alt => alt.Cases)
						.Where(trans => !matched.Overlaps(trans.AndPreds) && !falsified.Overlaps(trans.AndPreds)))
					foreach(var andPred in trans.AndPreds)
						children.Add(MakeBranchForAndPred(andPred, alts, matched, timesUsed, falsified));

				if (children.Count == 0)
				{
					// If no AndPreds were tested, proceed to the next level of prediction.
					Debug.Assert(falsified.Count == 0);
					return ComputeNestedPredictionTree(alts, timesUsed);
				}
				
				// If there are any "unguarded" cases left after falsifying all 
				// the AndPreds, add a branch for them.
				Debug.Assert(falsified.Count > 0);
				alts = RemoveFalsifiedCases(alts, falsified);
				if (alts.Count > 0)
				{
					var final = new PredictionBranch(new Set<AndPred>(), ComputeNestedPredictionTree(alts, timesUsed));
					children.Add(final);
				}
				return new PredictionTree(lookahead, children, null);
			}
			private PredictionBranch MakeBranchForAndPred(AndPred andPred, List<KthSet> alts, Set<AndPred> matched, Dictionary<int, int> timesUsed, HashSet<AndPred> falsified)
			{
				if (falsified.Count > 0)
					alts = RemoveFalsifiedCases(alts, falsified);

				var apSet = GetBuddies(alts, andPred);
				Debug.Assert(!apSet.IsEmpty);
				var innerMatched = matched | apSet;
				var result = new PredictionBranch(apSet, ComputeAssertionTree2(alts, innerMatched, timesUsed));
				falsified.UnionWith(apSet);
				return result;
			}
			private List<KthSet> RemoveFalsifiedCases(List<KthSet> alts, HashSet<AndPred> falsified)
			{
				var results = new List<KthSet>(alts.Count);
				for (int i = 0; i < alts.Count; i++) {
					KthSet alt = alts[i].Clone();
					for (int c = alt.Cases.Count - 1; c >= 0; c--)
						if (falsified.Overlaps(alt.Cases[c].AndPreds))
							alt.Cases.RemoveAt(c);
					if (alt.Cases.Count > 0)
						results.Add(alt);
				}
				return results;
			}
			private Set<AndPred> GetBuddies(List<KthSet> alts, AndPred ap)
			{
				// Given an AndPred, find any other AndPreds that always appear 
				// together with ap; if any are found, we want to group them 
				// together because doing so will simplify the prediction tree.
				return new Set<AndPred>(
					alts.SelectMany(alt => alt.Cases)
						.Where(trans => trans.AndPreds.Contains(ap))
						.Aggregate(null, (HashSet<AndPred> set, Transition trans) => {
							if (set == null) return new HashSet<AndPred>(trans.AndPreds);
							set.IntersectWith(trans.AndPreds);
							return set;
						}));
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
						a.AndPreds = a.AndPreds & b.AndPreds;
						tree.Children.RemoveAt(i);
					}
				}
				if (tree.Children.Count == 1)
					return tree.Children[0].Sub;
				return tree;
			}

			#region GenerateCodeForAlts() and related: generates code based on a prediction tree

			// GENERATED CODE EXAMPLE: The methods in this region generate
			// the for(;;) loop in this example and everything inside it, except
			// the calls to Match() which are generated by Visit(TerminalPred).
			// The generated code uses "goto" and "match" blocks in some cases
			// to avoid code duplication. This occurs when the matching code 
			// requires multiple statements AND appears more than once in the 
			// prediction tree. Otherwise, matching is done "inline" during 
			// prediction. We generate a for(;;) loop for (...)*, and in certain 
			// cases, we generates a do...while(false) loop for (...)?.
			//
			// rule Foo ==> #[ (('a'|'A') 'A')* 'a'..'z' 'a'..'z' ];
			// public void Foo()
			// {
			//     int la0, la1;
			//     for (;;) {
			//         la0 = LA(0);
			//         if (la0 == 'a') {
			//             la1 = LA(1);
			//             if (la1 == 'A')
			//                 goto match1;
			//             else
			//                 break;
			//         } else if (la0 == 'A')
			//             goto match1;
			//         else
			//             break;
			//         match1:
			//         {
			//             Match('A', 'a');
			//             Match('A');
			//         }
			//     }
			//     MatchRange('a', 'z');
			//     MatchRange('a', 'z');
			// }

			private void GenerateCodeForAlts(Alts alts, Dictionary<int, int> timesUsed, PredictionTree tree)
			{
				// Generate matching code for each arm. The "bool" in each pair 
				// indicates whether the matching code needs to be split out 
				// (separated) from the prediction tree.
				Pair<Node, bool>[] matchingCode = new Pair<Node, bool>[alts.Arms.Count];
				HashSet<int> unreachable = new HashSet<int>();
				int separateCount = 0;
				for (int i = 0; i < alts.Arms.Count; i++)
				{
					if (!timesUsed.ContainsKey(i)) {
						unreachable.Add(i+1);
						continue;
					}

					var codeForThisArm = new RWList<LNode>();
					VisitWithNewTarget(alts.Arms[i], codeForThisArm);

					matchingCode[i].A = F.Braces(codeForThisArm.ToRVList());
					if (matchingCode[i].B = timesUsed[i] > 1 && !SimpleEnoughToRepeat(matchingCode[i].A))
						separateCount++;
				}

				if (unreachable.Count == 1)
					LLPG.Output(alts.Basis, alts, Warning, string.Format("Branch {0} is unreachable.", unreachable.First()));
				else if (unreachable.Count > 1)
					LLPG.Output(alts.Basis, alts, Warning, string.Format("Branches {0} are unreachable.", unreachable.Join(", ")));
				if (!timesUsed.ContainsKey(-1) && alts.Mode != LoopMode.None)
					LLPG.Output(alts.Basis, alts, Warning, "Infinite loop. The exit branch is unreachable.");

				Symbol loopType = null;

				// Generate a loop body for (...)* or (...)?:
				if (alts.Mode == LoopMode.Star)
					loopType = S.For;
				else if (alts.Mode == LoopMode.Opt && (uint)(alts.DefaultArm ?? -1) < (uint)alts.Arms.Count)
					loopType = S.Do;

				// If the code for an arm is nontrivial and appears multiple times 
				// in the prediction table, it will have to be split out into a 
				// labeled block and reached via "goto". I'd rather just do a goto
				// from inside one "if" statement to inside another, but in C# 
				// (unlike in C and unlike in CIL) that is prohibited :(
				var extraMatching = GenerateExtraMatchingCode(matchingCode, separateCount, ref loopType);

				Symbol breakMode = loopType; // used to request a "goto" label in addition to the loop
				Node code = GeneratePredictionTreeCode(tree, matchingCode, ref breakMode);

				if (!extraMatching.IsEmpty)
					code = LNode.MergeLists(code, F.Braces(extraMatching), S.Braces);

				if (loopType == S.For) {
					// (...)* => for (;;) {}
					code = F.Call(S.For, F._Missing, F._Missing, F._Missing, code);
				} else if (loopType == S.Do) {
					// (...)? becomes "do {...} while(false);" IF the exit branch is NOT the default.
					// If the exit branch is the default, then no loop and no "break" is needed.
					code = F.Call(S.Do, code, F.@false);
				}
				if (breakMode != loopType) {
					// Add "stop:" label (plus extra ";" for C# compatibility, in 
					// case the label ends the block in which it is located.)
					var stopLabel = F.Call(S.Label, F.Id(breakMode))
					                 .PlusAttr(F.Trivia(S.TriviaRawTextAfter, ";"));
					code = LNode.MergeLists(code, stopLabel, S.Braces);
				}
				
				_target.SpliceAdd(code, S.Braces);
			}

			private bool SimpleEnoughToRepeat(Node code)
			{
				Debug.Assert(code.Calls(S.Braces));
				if (code.ArgCount > 1)
					return false;
				return code.ArgCount == 1 && !code.Args[0].Calls(S.If) && code.FindArgNamed(S.Braces) == null;
			}

			private RWList<LNode> GenerateExtraMatchingCode(Pair<Node, bool>[] matchingCode, int separateCount, ref Symbol loopType)
			{
				var extraMatching = new RWList<LNode>();
				if (separateCount != 0)
				{
					//int labelCounter = 0;
					int skipCount = 0;
					int firstSkip = -1;
					string suffix = NextGotoSuffix();

					for (int i = 0; i < matchingCode.Length; i++)
					{
						if (matchingCode[i].B) // split out this case
						{
							var label = F.Id("match" + (i+1) /*(++labelCounter)*/ + suffix);

							// break/continue; matchN: matchingCode[i].A;
							var skip = F.Call(loopType == S.For ? S.Continue : S.Break);
							if (firstSkip == -1)
								firstSkip = extraMatching.Count;
							extraMatching.Add(skip);
							extraMatching.Add(F.Call(S.Label, label));
							extraMatching.Add(matchingCode[i].A);
							skipCount++;

							// put @@{ goto matchN; } in prediction tree
							matchingCode[i].A = F.Call(S.Goto, label);
						}
					}
					Debug.Assert(firstSkip != -1);
					if (separateCount == matchingCode.Length)
					{
						// All of the matching code was split out, so the first 
						// break/continue statement is not needed.
						extraMatching.RemoveAt(firstSkip);
						skipCount--;
					}
					if (skipCount > 0 && loopType == null)
						// add do...while(false) loop so that the break statements make sense
						loopType = S.Do; 
				}
				return extraMatching;
			}

			private string NextStopLabel()
			{
				if (++_stopLabelCounter == 1)
					return "stop";
				else
					return string.Format("stop{0}", _stopLabelCounter);
			}
			private string NextGotoSuffix()
			{
				if (++_separatedMatchCounter == 1)
					return "";
				if (_separatedMatchCounter > 26)
					return string.Format("_{0}", _separatedMatchCounter - 1);
				else
					return ((char)('a' + _separatedMatchCounter - 1)).ToString();
			}

			protected Node GetPredictionSubtree(PredictionBranch branch, Pair<Node, bool>[] matchingCode, ref Symbol haveLoop)
			{
				if (branch.Sub.Tree != null)
					return GeneratePredictionTreeCode(branch.Sub.Tree, matchingCode, ref haveLoop);
				else {
					if (branch.Sub.Alt == -1) {
						return GetExitStmt(haveLoop);
					} else {
						var code = matchingCode[branch.Sub.Alt].A;
						if (code.Calls(S.Braces, 1))
							return code.Args[0].Clone();
						else
							return code.Clone();
					}
				}
			}

			private Node GetExitStmt(Symbol haveLoop)
			{
				if (haveLoop == null || haveLoop == S.Do)
					return (Node)F._Missing;
				if (haveLoop == S.For)
					return (Node)F.Call(S.Break);
				return (Node)F.Call(S.Goto, F.Id(haveLoop));
			}

			protected Node GeneratePredictionTreeCode(PredictionTree tree, Pair<Node,bool>[] matchingCode, ref Symbol haveLoop)
			{
				var braces = F.Braces();

				Debug.Assert(tree.Children.Count >= 1);
				var alts = (Alts)_currentPred;
				bool noDefault = alts.DefaultArm == null ? LLPG.NoDefaultArm : alts.DefaultArm.Value == -1;
				bool needErrorBranch = noDefault && (tree.IsAssertionLevel
					? !tree.Children.Last.AndPreds.IsEmpty
					: !tree.TotalCoverage.ContainsEverything);

				if (!needErrorBranch && tree.Children.Count == 1)
					return GetPredictionSubtree(tree.Children[0], matchingCode, ref haveLoop);

				// From the prediction table, we can generate either an if-else chain:
				//
				//   if (la0 >= '0' && la0 <= '7') sub_tree_1();
				//   else if (la0 == '-') sub_tree_2();
				//   else break;
				//
				// or a switch statement:
				//
				//   switch(la0) {
				//   case '0': case '1': case '2': case '3': case '4': case '5': case '6': case '7':
				//     sub_tree_1();
				//     break;
				//   case '-':
				//     sub_tree_2();
				//     break;
				//   default:
				//     goto breakfor;
				//   }
				//
				// Assertion levels always need an if-else chain; lookahead levels 
				// consider the complexity of switch vs if and decide which is most
				// appropriate. Generally "if" is slower, but a switch may require
				// too many labels since it doesn't support ranges like "la0 >= 'a'
				// && la0 <= 'z'".
				//
				// This class makes if-else chains directly (using IPGTerminalSet.
				// GenerateTest() to generate the test expressions), but the code 
				// snippet generator (CSG) is used to generate switch statements 
				// because the required code may be more complex and depends on the 
				// type of terminals--for example, if the terminals are Symbols, 
				// we'll need a static Dictionary in order to use a switch:
				//
				// static Dictionary<Symbol, int> Foo_JmpTbl = Foo_MakeJmpTbl();
				// static Dictionary<Symbol, int> Foo_MakeJmpTbl()
				// {
				//    var tbl = new Dictionary<Symbol, int>();
				//    tbl.Add(GSymbol.Get("0"), 1);
				//    ...
				//    tbl.Add(GSymbol.Get("7"), 1);
				//    tbl.Add(GSymbol.Get("-"), 2);
				// }
				// void Foo()
				// {
				//   Symbol la0;
				//   for (;;) {
				//     la0 = LA(0);
				//     int label;
				//     Foo_JmpTbl.TryGetValue(la0, out label);
				//     switch(label) {
				//     case 0:
				//       goto breakfor;
				//     case 1:
				//       sub_tree_1();
				//       break;
				//     case 2:
				//       sub_tree_2();
				//       break;
				//     }
				//   }
				//   breakfor:;
				// }
				//
				// We may or may not be generating code inside a for(;;) loop. If we 
				// decide to generate a switch() statement, one of the branches will 
				// usually need to break out of the for loop, but "break" can only
				// break out of the switch(). Therefore, if any of the matching code
				// is a break statement, .... hmm... I guess we could put a "breakfor"
				// label outside the for-loop and goto it.
				
				Node block;
				GreenNode laVar = null;
				HashSet<int> switchCases = new HashSet<int>();
				IPGTerminalSet[] branchSets = null;
				bool should = false;

				if (!tree.NeedsLaVar()) {
					block = F.Braces();
				} else {
					_laVarsNeeded |= 1ul << tree.Lookahead;
					laVar = F.Id("la" + tree.Lookahead.ToString());
					// block = @@{{ \laVar = \(LA(context.Count)); }}
					block = F.Braces(F.Call(S.Set, laVar, CSG.LA(tree.Lookahead)));

					if (!tree.IsAssertionLevel) {
						IPGTerminalSet covered = CSG.EmptySet;
						branchSets = tree.Children.Select(branch => {
							var set = branch.Set.Subtract(covered);
							covered = covered.Union(branch.Set);
							return set;
						}).ToArray();

						should = CSG.ShouldGenerateSwitch(branchSets, switchCases, needErrorBranch);
						if (!should)
							switchCases.Clear();
						else if (should && haveLoop == S.For)
							haveLoop = GSymbol.Get(NextStopLabel());
					}
				}

				Node[] branchCode = new Node[tree.Children.Count];
				for (int i = 0; i < tree.Children.Count; i++)
					branchCode[i] = GetPredictionSubtree(tree.Children[i], matchingCode, ref haveLoop);

				var code = GenerateIfElseChain(tree, branchCode, needErrorBranch, laVar, switchCases);
				if (should) {
					Debug.Assert(switchCases.Count != 0);
					code = CSG.GenerateSwitch(branchSets, switchCases, branchCode, code, laVar);
				}

				return block.PlusArg(code);
			}

			private Node GenerateIfElseChain(PredictionTree tree, Node[] branchCode, bool needErrorBranch, GreenNode laVar, HashSet<int> switchCases)
			{
				// From the prediction table, generate a chain of if-else 
				// statements in reverse, starting with the final "else" clause.
				// Skip any branches that have been claimed for use in a switch()
				Node ifChain = null;
				if (needErrorBranch)
					ifChain = CSG.ErrorBranch(tree.TotalCoverage, laVar);

				for (int i = tree.Children.Count-1; i >= 0; i--) {
					if (switchCases.Contains(i))
						continue;

					if (ifChain == null)
						ifChain = branchCode[i];
					else {
						var branch = tree.Children[i];
						Node test;
						if (tree.IsAssertionLevel)
							test = GenerateTest(branch.AndPreds, tree.Lookahead, laVar);
						else {
							var set = CSG.Optimize(branch.Set, branch.Covered);
							test = CSG.GenerateTest(set, laVar);
						}

						Node @if = F.Call(S.If, test, branchCode[i]);
						if (!ifChain.IsSymbolWithoutPAttrs(S.Missing))
							@if = @if.PlusArg(ifChain);
						ifChain = @if;
					}
				}
				return ifChain;
			}

			private Node GenerateTest(Set<AndPred> andPreds, int lookaheadAmt, GreenNode laVar)
			{
				Node test;
				test = null;
				foreach (AndPred ap in andPreds)
				{
					Node code = GetAndPredCode(ap, lookaheadAmt, laVar);
					Node next = CSG.GenerateAndPredCheck(ap, code, true);
					if (test == null)
						test = next;
					else
						test = F.Call(S.And, test, next);
				}
				return test;
			}

			#endregion

			public override void Visit(Seq pred)
			{
				foreach (var p in pred.List)
					Visit(p);
			}
			public override void Visit(Gate pred)
			{
				Visit(pred.Match);
			}
			public override void Visit(AndPred pred)
			{
				_target.Add(CSG.GenerateAndPredCheck(pred, GetAndPredCode(pred, 0, CSG.LA(0)), false));
			}
			public override void Visit(RuleRef rref)
			{
				_target.Add(rref.AutoSaveResult(F.Call(rref.Rule.Name)));
			}
			public override void Visit(TerminalPred term)
			{
				if (term.Set.ContainsEverything)
					_target.Add(term.AutoSaveResult(CSG.GenerateConsume()));
				else
					_target.Add(term.AutoSaveResult(CSG.GenerateMatch(term.Set)));
			}
			
			Node GetAndPredCode(AndPred pred, int lookaheadAmt, GreenNode laVar)
			{
				Node code = (Node)pred.Pred; // this is all we support right now

				Func<Node, Node> selector = null; selector = arg => {
					if (arg.Equals(AndPred.SubstituteLA))
						return (Node)laVar;
					if (arg.Equals(AndPred.SubstituteLI))
						return (Node)F.Literal(lookaheadAmt);
					return arg.WithArgs(selector);
				};
				return code.WithArgs(selector);
			}
		}
	}
}
