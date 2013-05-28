//
// Prediction analysis intermediate data structures defined here:
//   PredictionTreeOrAlt
//   PredictionTree
//   PredictionBranch
//   see also KthSet.cs
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Collections;
using Loyc.Collections.Impl;

namespace Loyc.LLParserGenerator
{
	partial class LLParserGenerator
	{
		protected internal const int ErrorAlt = -2, ExitAlt = -1;

		/// <summary>A <see cref="PredictionTree"/> or a single alternative to assume.</summary>
		protected internal struct PredictionTreeOrAlt : IEquatable<PredictionTreeOrAlt>
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

			internal void CountTimesUsed(Dictionary<int, int> timesUsed)
			{
				if (Tree == null)
					CountAlt(Alt, timesUsed);
				else
					Tree.CountTimesUsed(timesUsed);
			}
			static void CountAlt(int alt, Dictionary<int, int> timesUsed)
			{
				int counter;
				timesUsed.TryGetValue(alt, out counter);
				timesUsed[alt] = counter + 1;
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
		protected internal class PredictionTree : IEquatable<PredictionTree>
		{
			public PredictionTree(int la, InternalList<PredictionBranch> children, IPGTerminalSet coverage)
			{
				Lookahead = la;
				Children = children;
				TotalCoverage = coverage;
			}
			public InternalList<PredictionBranch> Children = InternalList<PredictionBranch>.Empty;
			public int Lookahead; // starts at 0 for first terminal of lookahead
			public bool IsAssertionLevel { get { return TotalCoverage == null; } }
			// The set of possible non-error inputs; null for an assertion level
			public IPGTerminalSet TotalCoverage;

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
				return !IsAssertionLevel || Children.Any(branch => branch.AndPreds.SelectMany(s => s).Any(ap => ap.PredUsesLA));
			}
			internal void CountTimesUsed(Dictionary<int, int> timesUsed)
			{
				foreach (var branch in Children)
					branch.Sub.CountTimesUsed(timesUsed);
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
		protected internal class PredictionBranch : IEquatable<PredictionBranch>
		{
			public PredictionBranch(Set<AndPred> andPreds, PredictionTreeOrAlt sub)
			{
				AndPreds = new List<Set<AndPred>>() { andPreds };
				Sub = sub;
			}
			public PredictionBranch(IPGTerminalSet set, PredictionTreeOrAlt sub, IPGTerminalSet covered)
			{
				Set = set;
				Sub = sub;
				Covered = covered;
			}

			// Used in standard prediction levels.
			public IPGTerminalSet Set;
			// Used in assertion levels. Each set is a set of predicates that 
			// must all be true; the outer list represents alternatives, of 
			// which only one set must be true ("or"). We always start with one 
			// set, but SimplifyPredictionTree may join multiple sets.
			public List<Set<AndPred>> AndPreds; 

			public PredictionTreeOrAlt Sub;
			
			public IPGTerminalSet Covered; // the set of terminals handled by previous branches

			public bool IsErrorBranch { get { return Sub.Alt == ErrorAlt; } }

			public override string ToString() // for debugging
			{
				string andPreds = StringExt.Join(" || ", AndPreds.Select(s => StringExt.Join("", s)));
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
					&& other.AndPredsEqual(AndPreds) 
					&& other.Sub.Equals(Sub);
			}
			private bool AndPredsEqual(List<Set<AndPred>> otherPreds)
			{
				if (otherPreds == null)
					return AndPreds == null;
				if (otherPreds.Count != AndPreds.Count)
					return false;
				for (int i = 0; i < AndPreds.Count; i++)
					if (!AndPreds[i].SetEquals(otherPreds[i]))
						return false;
				return true;
			}
			internal void CombineAndPredsWith(List<Set<AndPred>> list)
			{
				if (AndPreds != null) {
					foreach (var set in list) {
						if (set.IsEmpty) {
							AndPreds.Clear();
							AndPreds.Add(set);
							return;
						}
						bool handled = false;
						AndPreds.RemoveAll(s => {
							bool sp = s.IsSupersetOf(set);
							bool sb = s.IsSubsetOf(set);
							handled |= sp || sb;
							return sp;
						});
						if (!handled)
							AndPreds.Add(set);
					}
				} else
					Debug.Assert(list == null);
			}
		}
	}
}
