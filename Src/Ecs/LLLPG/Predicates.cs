using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using ecs;
using Loyc.Essentials;
using Loyc.Utilities;
using Loyc.CompilerCore;

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

		public abstract bool IsNullable { get; }

		// Helper methods for creating a grammar without a source file (this is
		// used for testing and for bootstrapping the parser generator).
		public static Alts operator | (Pred a, Pred b) { return new Alts(null, a, b, false); }
		public static Alts operator / (Pred a, Pred b) { return new Alts(null, a, b, true); }
		public static Alts operator | (char a, Pred b) { return new Alts(null, Char(a), b, false); }
		public static Alts operator | (Pred a, char b) { return new Alts(null, a, Char(b), false); }
		public static Seq  operator + (Pred a, Pred b) { return new Seq(a, b); }
		public static Seq  operator + (char a, Pred b) { return new Seq(Char(a), b); }
		public static Seq  operator + (Pred a, char b) { return new Seq(a, Char(b)); }
		public static Alts Star (Pred contents) { return new Alts(null, LoopMode.Star, contents); }
		public static Alts Opt (Pred contents) { return new Alts(null, LoopMode.Opt, contents); }
		public static Seq Plus (Pred contents) { return new Seq(contents, new Alts(null, LoopMode.Star, contents)); }
		public static TerminalSet Range(char lo, char hi) { return TerminalSet.New(null, lo, hi); }
		public static TerminalSet Char(char c) { return TerminalSet.New(null, c); }
		public static TerminalSet Chars(params char[] c)
		{
			var set = new IntSet(new IntRange(c[0]));
			for (int i = 1; i < c.Length; i++)
			{
				var seti = new IntSet(new IntRange(c[i]));
				set = set.Union(seti);
			}
			return new CharSetTerminal(null, set);
		}
		public static Rule Rule(string name, Pred pred, bool isStartingRule = false, bool isToken = false)
		{
			return new Rule(null, GSymbol.Get(name), pred, isStartingRule) { IsToken = isToken };
		}
	}
	/// <summary>Represents a nonterminal, which is a reference to a rule.</summary>
	public class RuleRef : Pred
	{
		public override void Call(PredVisitor visitor) { visitor.Visit(this); }
		public RuleRef(Node basis, Rule rule) : base(basis) { Rule = rule; }
		public new Rule Rule;
		public override bool IsNullable
		{
			get { return Rule.Pred.IsNullable; }
		}
	}
	
	/// <summary>Represents a sequence of predicates (<see cref="Pred"/>s).</summary>
	public class Seq : Pred
	{
		public override void Call(PredVisitor visitor) { visitor.Visit(this); }
		public Seq(Node basis) : base(basis) {}
		public Seq(Pred seq, Pred add) : base(null)
		{
			if (seq is Seq)
				List.AddRange((seq as Seq).List);
			Debug.Assert(!(add is Seq));
			List.Add(add);
		}
		public List<Pred> List = new List<Pred>();

		public override bool IsNullable
		{
			get { return List.TrueForAll(p => p.IsNullable); }
		}
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
		public Alts(Node basis, Pred a, Pred b, bool ignoreAmbig = false) : this(basis, LoopMode.None, ignoreAmbig)
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
		public int DefaultArm = -1;

		public void Add(Pred p)
		{
			var a = p as Alts;
			if (a != null && a.Mode == LoopMode.None && a.IgnoreAmbiguous == IgnoreAmbiguous)
				Arms.AddRange(a.Arms);
			else
				Arms.Add(p);
		}

		public override bool IsNullable
		{
			get {
				if (Mode != LoopMode.None)
					return true;
				return Arms.Any(arm => arm.IsNullable);
			}
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

		public override bool IsNullable
		{
			get { return Predictor.IsNullable; }
		}
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

		public override bool IsNullable
		{
			get { return true; }
		}
	}

	//public class ITerminalSet
	//{
	//    public abstract TerminalSetBase Merged(TerminalSetBase r);
	//    public abstract TerminalSetBase 
	//}

	/// <summary>Represents a terminal (which is a token or a character) or a set 
	/// of possible terminals (e.g. 'A'..'Z').</summary>
	public abstract class TerminalSet : Pred
	{
		public override void Call(PredVisitor visitor) { visitor.Visit(this); }
		public TerminalSet(Node basis) : base(basis) {}
		public static TerminalSet New(Node basis, char c) { return new CharSetTerminal(null, c); }
		public static TerminalSet New(Node basis, char lo, char hi) { return new CharSetTerminal(null, new IntRange(lo, hi)); }
		public static readonly EmptyTerminalSet Empty = new EmptyTerminalSet(null);

		// For combining with | operator; cannot merge if PreAction/PostAction differs between arms
		public virtual bool CanMerge(TerminalSet r)
		{
			return r.PreAction == PreAction && r.PostAction == PostAction;
		}
		public abstract TerminalSet Union(TerminalSet r, bool ignoreActions = false);

		public abstract bool ContainsEOF { get; }
		public abstract bool IsEmptySet { get; }
		public override bool IsNullable
		{
			get { return false; }
		}

		public virtual TerminalSet Intersection(TerminalSet other)
		{
			throw new NotImplementedException();
		}
		internal virtual TerminalSet Subtract(TerminalSet coverage)
		{
			throw new NotImplementedException();
		}
	}

	public class CharSetTerminal : TerminalSet
	{
		protected IntSet _set;

		public CharSetTerminal(Node basis) : base(basis) {}
		public CharSetTerminal(Node basis, IntRange r, bool inverted = false) : base(basis)
		{
			_set = new IntSet(r, true, inverted);
		}
		public CharSetTerminal(Node basis, IntSet set) : base(basis)
		{
			_set = set;
		}

		public static implicit operator CharSetTerminal(char c) { return new CharSetTerminal(null, c); }
		public static implicit operator CharSetTerminal(IntRange r) { return new CharSetTerminal(null, r); }
		public static implicit operator CharSetTerminal(IntSet set) { return new CharSetTerminal(null, set); }
		
		public override bool CanMerge(TerminalSet r)
		{
			return r is CharSetTerminal && r.PreAction == PreAction && r.PostAction == PostAction;
		}
		public override TerminalSet Union(TerminalSet r, bool ignoreActions)
		{
			var r2 = r as CharSetTerminal;
			if (r2 == null) return null;
			return Merged(r2, ignoreActions);
		}
		public CharSetTerminal Merged(CharSetTerminal r, bool ignoreActions)
		{
			if (!ignoreActions && (PreAction != r.PreAction || PostAction != r.PostAction))
				throw new InvalidOperationException("Internal error: cannot merge CharSets that have actions");
			return new CharSetTerminal(Basis, _set.Union(r._set)) { PreAction = PreAction, PostAction = PostAction };
		}

		public override bool ContainsEOF { get { return _set.Contains(-1); } }
		public override bool IsEmptySet { get { return _set.IsEmptySet; } }
	}

	/// <summary>A container for the follow set of a <see cref="Rule"/>.</summary>
	public class EndOfRule : Pred
	{
		public EndOfRule() : base(null) { }
		public override void Call(PredVisitor visitor) { throw new NotSupportedException(); }
		public HashSet<Pred> FollowSet = new HashSet<Pred>();

		public override bool IsNullable
		{
			get { throw new NotImplementedException(); }
		}
	}
}
