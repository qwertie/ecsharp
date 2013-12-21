//
// Prediction analysis intermediate data structures defined here:
//   - KthSet
//   - Transition
//   - GrammarPos
//   see also PredictionTree.cs
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
		/// <summary>Represents the possible interpretations of a single input 
		/// character, in terms of transitions in the grammar.</summary>
		/// <remarks>
		/// For example, suppose the grammar is as follows (where "strings" are
		/// actually aliases for tokens):
		/// <code>
		///     For ==> @[ "for" (\id "in" $collection | \id '=' range) ]
		///     Range ==> @[ start ".." stop ]
		/// </code>
		/// If the starting position is right after "for", then <see cref="ComputeNextSet"/>
		/// will generate two <see cref="Cases"/>, one at <c>\id."in" $collection</c> 
		/// and another at <c>\id.'=' stop</c>. In both cases, the Set is $id, 
		/// so <see cref="KthSet.Set"/> will also be \id.
		/// </remarks>
		protected class KthSet
		{
			public KthSet(KthSet prev)
			{
				Prev = prev;
				LA = prev.LA + 1;
				Alt = prev.Alt;
				IsNongreedyExit = prev.IsNongreedyExit;
				Set = prev.Set.Empty;
			}
			public KthSet(Pred start, int alt, IPGTerminalSet emptySet, bool isNongreedyExit = false) {
				Cases.Add(new Transition(null, null, new GrammarPos(start, null)));
				Alt = alt;
				IsNongreedyExit = isNongreedyExit;
				Set = emptySet;
			}
			public int LA = -1;
			public List<Transition> Cases = new List<Transition>();
			public IPGTerminalSet Set;    // Union of tokens in all cases
			public Set<AndPred> AndReq;   // Intersection of AndPreds in all cases
			public KthSet Prev;           // Previous lookahead level
			public bool HasAnyAndPreds { get { return Cases.Any(t => !t.AndPreds.IsEmpty); } }
			public int Alt;               // -1 (ExitAlt) for exit, 0 for first alternative
			public bool IsNongreedyExit;    // indicates a nongreedy exit branch (which takes priority in case of ambiguity)
				
			public void UpdateSet(bool addEOF)
			{
				if (Cases.Count == 0) {
					Set = Set.Empty;
					if (addEOF)
						Set = Set.WithEOF();
					AndReq = new Set<AndPred>();
					return;
				}
				Set = Cases[0].Set;
				var andI = new MSet<AndPred>(Cases[0].AndPreds);
				for (int i = 1; i < Cases.Count; i++) {
					Set = Set.Union(Cases[i].Set);
					andI.IntersectWith(Cases[i].AndPreds);
				}
				AndReq = (Set<AndPred>)andI;

				if (addEOF)
					Set = Set.WithEOF();
			}
			public override string ToString() // for debugging
			{
				return string.Format("la{0} = {1} ({2})", LA, Set.ToString(), Cases.Select(c => c.Set).Join("|"));
			}
			public KthSet Clone(bool update)
			{
 				KthSet copy = new KthSet(Prev) { LA = LA, Set = Set, Alt = Alt };
				for (int i = 0; i < Cases.Count; i++)
					copy.Cases.Add(Cases[i].Clone());
				if (update)
					copy.UpdateSet(Set.ContainsEOF);
				return copy;
			}
		}
		
		/// <summary>Represents a position in a grammar (<see cref="GrammarPos"/>) 
		/// plus the set of characters that leads to that position from the previous 
		/// position. This is a single case in a <see cref="KthSet"/>.</summary>
		/// <remarks>
		/// For example, suppose the grammar is
		/// <code>
		///		rule X ==> @[ 'a' Y 'z' ];
		///		rule Y ==> @[ 'a'..'y' 'b'..'z' ];
		/// </code>
		/// If the previous position is represented by the dot in <c>'a'.Y 'z'</c>,
		/// i.e. before Y, then <see cref="ComputeNextSet"/> will compute a Transition
		/// with Set=[a-y] and Position pointing to <c>.'b'..'z'</c>, with a return 
		/// stack that points to <c>'a' Y.'z'</c>
		/// </remarks>
		protected class Transition : ICloneable<Transition>
		{
			public Transition(Pred prevPosition, IPGTerminalSet set, GrammarPos position) : this(prevPosition, set, RVList<AndPred>.Empty, position) { }
			public Transition(Pred prevPosition, IPGTerminalSet set, RVList<AndPred> andPreds, GrammarPos position)
			{
				PrevPosition = prevPosition;
				Debug.Assert(position != null);
				Set = set; 
				Position = position;
				AndPreds = andPreds;
			}
			public IPGTerminalSet Set;
			public RVList<AndPred> AndPreds;
			public GrammarPos Position;
			public Pred PrevPosition; // null if there were multiple starting positions

			public override string ToString() // for debugging
			{
				if (AndPreds.Count > 0)
					return string.Format("{2} {0} => {1}", Set, Position, StringExt.Join("", AndPreds));
				else
					return string.Format("{0} => {1}", Set, Position);
			}
			public Transition Clone()
			{
 				return new Transition(PrevPosition, Set, AndPreds, Position);
			}
		}

		/// <summary>Represents a location in a grammar: a predicate and a 
		/// "return stack" which is a so-called persistent singly-linked 
		/// list. This type is used within <see cref="Transition"/>.</summary>
		protected class GrammarPos : IEquatable<GrammarPos>
		{
			public GrammarPos(Pred pred, GrammarPos @return, bool inFollowSet = false)
			{
				Debug.Assert(pred != null); 
				Pred = pred; 
				Return = @return;
				InFollowSet = inFollowSet;
			}
			public GrammarPos(Pred pred, bool inFollowSet) : this(pred, null, inFollowSet) { }
			
			public readonly Pred Pred;
			public readonly GrammarPos Return;
			public readonly bool InFollowSet;  // if so, Return==null
			public bool InsideOtherRule { get { return Return != null || InFollowSet; } }

			public override string ToString() // for debugging
			{
				if (InFollowSet)
					return string.Format("{0} (in follow set)", Pred);
				if (Return != null)
					return string.Format("{0} (return to {1})", Pred, Return);
				return Pred.ToString();
			}
			public bool Equals(GrammarPos other) { return Equals(this, other); }
			public static bool Equals(GrammarPos a, GrammarPos b)
			{
				if (a != null && b != null)
					return a.Pred == b.Pred && Equals(a.Return, b.Return) && a.InFollowSet == b.InFollowSet;
				return a == b;
			}
			public override bool Equals(object obj)
			{
				return obj is GrammarPos && Equals(obj as GrammarPos);
			}
			public override int GetHashCode()
			{
				int hc = Pred.GetHashCode();
				if (Return != null)
					hc ^= Return.GetHashCode() * 13;
				if (InFollowSet) hc ^= 1;
				return hc;
			}
		}
	}
}
