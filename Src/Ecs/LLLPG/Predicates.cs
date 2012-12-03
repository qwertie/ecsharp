using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using ecs;
using Loyc.Essentials;
using Loyc.Collections.Impl;
using Loyc.Utilities;

namespace Loyc.LLParserGenerator
{
	/// <summary>Represents part of a grammar for the <see cref="LLParserGenerator"/>.</summary>
	/// <remarks>
	/// This class is the root of a class hierarchy which contains
	/// <ul>
	/// <li><see cref="TerminalSet"/>: represents a terminal (which is a token or a 
	///     character) or a set of possible terminals (e.g. 'A'..'Z'). This class 
	///     has subclasses including <see cref="CharSet"/> and <see cref="AnyTerminal"/>.</li>
	/// <li><see cref="RuleRef"/>: represents a nonterminal, which is a reference to a rule.</li>
	/// <li>Other components of a rule:
	///     terminals and nonterminals (<see cref="TerminalSet"/> and <see cref="RuleRef"/>), 
	///     sequences (<see cref="Seq"/>),
	///     branches and loops (<see cref="Alts"/>),
	///     gates (<see cref="Gate"/>, a mechanism to separate prediction from matching), and
	///     and-predicates (<see cref="AndPred"/>, an assertion that consumes no input).</li>
	/// <li><see cref="EndOfRule"/>: a container for the follow set of a <see cref="Rule"/> 
	///     (this class is not a real predicate; it is derived from Pred so that it 
	///     can be a legal value for <see cref="Pred.Next"/>).</li>
	/// </remarks>
	public abstract class Pred
	{
		public abstract void Call(PredVisitor visitor); // visitor pattern
		public Pred(Node basis) { Basis = basis ?? Node.Missing; }
		public readonly Node Basis;
		public virtual Pred Predictor { get { return this; } }
		public virtual Pred Match { get { return this; } }
		public Node PreAction;
		public Node PostAction;
		public Pred Next; // The predicate that follows this one or EndOfRule

		public static Node AppendAction(Node action, Node action2)
		{
			if (action == null)
				return action2;
			else {
				action = action.Unfrozen();
				// TODO: implement ArgList.AddRange()
				int at = action.Args.Count;
				for (int j = action2.ArgCount-1; j >= 0; j--)
					action.Args.Insert(at, action2.Args.Detach(j));
				return action;
			}
		}
	}
	/// <summary>Represents a nonterminal, which is a reference to a rule.</summary>
	public class RuleRef : Pred
	{
		public override void Call(PredVisitor visitor) { visitor.Visit(this); }
		public RuleRef(Node basis, Rule rule) : base(basis) { Rule = rule; }
		public Rule Rule;
	}
	
	/// <summary>Represents a sequence of predicates (<see cref="Pred"/>s).</summary>
	public class Seq : Pred
	{
		public override void Call(PredVisitor visitor) { visitor.Visit(this); }
		public Seq(Node basis) : base(basis) {}
		public List<Pred> List = new List<Pred>();
	}
	
	/// <summary>Describes a series of alternatives (branches), a kleene star 
	/// (`*`), or an optional element (`?`).</summary>
	/// <remarks>
	/// Branches, stars and optional elements are represented by the same class 
	/// because they all require prediction, and prediction works the same way for 
	/// all three.
	/// <para/>
	/// The one-or-more operator `+` is represented simply by repeating the 
	/// contents once, i.e. (x`+`) is converted to (x, x`*`), which is a Seq of
	/// two elements: x and an Alts object that contains x.
	/// </remarks>
	public class Alts : Pred
	{
		public override void Call(PredVisitor visitor) { visitor.Visit(this); }

		public Alts(Node basis, LoopMode mode, bool ignoreAmbig) : base(basis)
		{
			Mode = mode;
			IgnoreAmbiguous = ignoreAmbig;
		}
		public Alts(Node basis, Pred a, Pred b, bool ignoreAmbig) : this(basis, LoopMode.None, ignoreAmbig)
		{
			Add(a);
			Add(b);
		}
		public Alts(Node basis, LoopMode mode, Pred contents) : this(basis, mode, false)
		{
			Debug.Assert(mode == LoopMode.Star || mode == LoopMode.Opt);
			var contents2 = contents as Alts;
			if (contents2 != null) {
				if (contents2.Mode == LoopMode.Opt || contents2.Mode == LoopMode.Star)
					throw new ArgumentException(Localize.From("{0} predicate cannot directly contain {1} predicate", ToStr(mode), ToStr(contents2.Mode)));
				IgnoreAmbiguous = contents2.IgnoreAmbiguous;
				Greedy = contents2.Greedy;
				Arms = contents2.Arms;
			} else {
				Arms.Add(contents);
			}
		}
		static string ToStr(LoopMode m) 
		{
			switch(m) {
				case LoopMode.Opt: return "an optional (?)";
				case LoopMode.Star: return "a loop (*, +)";
				default: return "an alternative list";
			}
		}
		
		public LoopMode Mode = LoopMode.None;
		public bool IgnoreAmbiguous = false;
		public bool Greedy = false;
		public List<Pred> Arms = new List<Pred>();

		public void Add(Pred p)
		{
			var a = p as Alts;
			if (a != null && a.Mode == LoopMode.None && a.IgnoreAmbiguous == IgnoreAmbiguous)
				Arms.AddRange(a.Arms);
			else
				Arms.Add(p);
		}
	}
	/// <summary>Types of <see cref="Alts"/> objects.</summary>
	/// <remarks>Although x? can be simulated with (x|), we keep them as separate modes for reporting purposes.</remarks>
	public enum LoopMode { None, Opt, Star };

	/// <summary>Represents a "gate" (p => m), which is a mechanism to separate 
	/// prediction from matching in the context of branching (<see cref="Alts"/>).</summary>
	/// <remarks>
	/// </remarks>
	public class Gate : Pred
	{
		public override void Call(PredVisitor visitor) { visitor.Visit(this); }
		public Gate(Node basis, Pred predictor, Pred match) : base(basis) {
			G.Require(predictor.Predictor == predictor && match.Match == match,
				"A gate '=>' cannot contain another gate");
			_predictor = predictor;
			_match = match;
		}
		public Pred _predictor;
		public Pred _match;
		public override Pred Predictor { get { return _predictor; } }
		public override Pred Match { get { return _match; } }
	}

	/// <summary>Represents a zero-width assertion: either user-defined code to
	/// check a condition, or a predicate that scans ahead in the input and then
	/// backtracks to the starting point.</summary>
	public class AndPred : Pred
	{
		public override void Call(PredVisitor visitor) { visitor.Visit(this); }
		public AndPred(Node basis, object pred, bool not) : base(basis) { Pred = pred; Not = not; }
		
		/// <summary>Inverts the condition if Not==true, so that if the 
		/// <see cref="Pred"/> matches, the <see cref="AndPred"/> does not 
		/// match, and vice versa.</summary>
		public bool Not = false;
		
		/// <summary>The predicate to match and backtrack. Must be of type 
		/// <see cref="Node"/> or <see cref="Pred"/>.</summary>
		public object Pred;
	}

	/// <summary>Represents a terminal (which is a token or a character) or a set 
	/// of possible terminals (e.g. 'A'..'Z').</summary>
	public abstract class TerminalSet : Pred
	{
		public override void Call(PredVisitor visitor) { visitor.Visit(this); }
		public TerminalSet(Node basis) : base(basis) {}
		public static TerminalSet New(Node basis, char c) { return new CharSet(basis, new CharRange(c)); }
		public static TerminalSet New(Node basis, char lo, char hi) { return new CharSet(basis, new CharRange(lo, hi)); }

		// For combining with | operator; cannot merge if PreAction/PostAction differs between arms
		public abstract bool CanMerge(TerminalSet r);
		public abstract TerminalSet Merge(TerminalSet r);
	}
	
	/// <summary>Represents a set of single characters (e.g. 'A' | 'a')</summary>
	public class CharSet : TerminalSet
	{
		public InternalList<CharRange> Ranges = InternalList<CharRange>.Empty;

		public CharSet(Node basis) : base(basis) {}
		public CharSet(Node basis, CharRange r) : base(basis)
		{
			Ranges.Add(r);
		}
		public override bool CanMerge(TerminalSet r)
		{
			return r is CharSet && r.PreAction == PreAction && r.PostAction == PostAction;
		}
		public override TerminalSet Merge(TerminalSet r)
		{
			if (!CanMerge(r)) return null;
			return Merged((CharSet)r, false);
		}

		public bool Overlaps(CharRange r)
		{
			for (int i = 0; i < Ranges.Count; i++)
				if (Ranges[i].Overlaps(r))
					return true;
			return false;
		}
		public CharSet Merged(CharSet r, bool ignoreActions)
		{
			if (!ignoreActions && (PreAction != r.PreAction || PostAction != r.PostAction))
				throw new InvalidOperationException("Internal error: cannot merge CharSets that have actions");

			var e0 = Ranges.GetEnumerator();
			if (!e0.MoveNext())
				return new CharSet(Basis) { Ranges = r.Ranges.Clone() };
			var e1 = r.Ranges.GetEnumerator();
			if (!e1.MoveNext())
				return new CharSet(Basis) { Ranges = Ranges.Clone() };
			
			var result = new InternalList<CharRange>(Ranges.Count + r.Ranges.Count);
			while (e0 != null && e1 != null)
			{
				var r0 = e0.Current;
				var r1 = e1.Current;
				if (r0.CanMerge(r1)) {
					result.Add(r0.Merged(r1));
					if (!e0.MoveNext()) e0 = null;
					if (!e1.MoveNext()) e1 = null;
				} else if (r0 < r1) {
					result.Add(r0);
					if (!e0.MoveNext()) e0 = null;
				} else {
					result.Add(r1);
					if (!e1.MoveNext()) e1 = null;
				}
			}

			if (e0 != null) do
				result.Add(e0.Current);
			while(e0.MoveNext());
			
			if (e1 != null) do
				result.Add(e1.Current);
			while(e1.MoveNext());
			
			return new CharSet(Basis) { Ranges = result };
		}
	}

	/// <summary>Represents a range of single characters (e.g. 'A'..'Z').</summary>
	public struct CharRange : IComparable<CharRange>
	{
		public CharRange(char c) { Lo = Hi = c; }
		public CharRange(char lo, char hi) {
			Lo = lo; Hi = hi;
			if (lo > hi)
				throw new ArgumentException(Localize.From("Character range Lo > Hi: '{0}' > '{1}'", lo, hi));
		}

		public char Lo, Hi;

		public bool Overlaps(CharRange r)
		{
			Debug.Assert(Lo <= Hi && r.Lo <= r.Hi);
			if (Lo <= r.Lo)
				return Hi >= r.Lo;
			else // r.Lo < Lo
				return r.Hi >= Lo;
		}
		public bool CanMerge(CharRange r)
		{
			Debug.Assert(Lo <= Hi && r.Lo <= r.Hi);
			if (Lo <= r.Lo)
				return Hi >= r.Lo - 1;
			else // r.Lo < Lo
				return r.Hi >= Lo - 1;
		}
		public CharRange Merged(CharRange r)
		{
			if (Lo < r.Lo)
				return new CharRange { Lo = Lo, Hi = (Hi > r.Hi ? Hi : r.Hi) };
			else
				return r.Merged(this);
		}
		public int CompareTo(CharRange other)
		{
			return ((int)Lo).CompareTo(((int)other.Lo));
		}
		public static bool operator >(CharRange a, CharRange b) { return a.Lo > b.Lo; }
		public static bool operator <(CharRange a, CharRange b) { return a.Lo < b.Lo; }
	}

	/// <summary>A container for the follow set of a <see cref="Rule"/>.</summary>
	public class EndOfRule : Pred
	{
		public EndOfRule() : base(null) { }
		public override void Call(PredVisitor visitor) { throw new NotSupportedException(); }
		public HashSet<Pred> FollowSet = new HashSet<Pred>();
	}

	/// <summary>Represents any single terminal.</summary>
	public class AnyTerminal : TerminalSet
	{
		public bool AllowEOF = false;
		public static AnyTerminal AnyFollowSet()
		{
			var a = new AnyTerminal() { AllowEOF = true };
			a.Next = a;
			return a;
		}

		public AnyTerminal() : base(Node.Missing) { }
		public override bool CanMerge(TerminalSet r)
		{
			return r.PreAction == PreAction && r.PostAction == PostAction;
		}
		public override TerminalSet Merge(TerminalSet r)
		{
			if (!CanMerge(r)) return null;
			if ((r is AnyTerminal) && (r as AnyTerminal).AllowEOF) return r;
			return this;
		}
	}
}
