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
	/// <li><see cref="TerminalPred"/>: represents a terminal (which is a token or a 
	///     character) or a set of possible terminals (e.g. 'A'..'Z').</li>
	/// <li><see cref="RuleRef"/>: represents a nonterminal, which is a reference to a rule.</li>
	/// <li>Other components of a rule:
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
		public Node PreAction;
		public Node PostAction;
		public Pred Next; // The predicate that follows this one or EndOfRule
		
		/// <summary>A function that saves the result produced by the matching code 
		/// of this predicate. For example, if the parser generator is given the
		/// predicate #[ x = 'a'..'z' ], the default matching code will be 
		/// @(Match('a', 'z')), and ResultSaver will be set to a function that 
		/// receives this matching code and returns @(x = Match('a', 'z')) in 
		/// response.</summary>
		public Func<Node, Node> ResultSaver;
		public Node AutoSaveResult(Node matchingCode)
		{
			return ResultSaver != null ? ResultSaver(matchingCode) : matchingCode;
		}

		public abstract bool IsNullable { get; }

		// Helper methods for creating a grammar without a source file (this is
		// used for testing and for bootstrapping the parser generator).
		public static Seq  operator + (char a, Pred b) { return Char(a) + b; }
		public static Seq  operator + (Pred a, char b) { return a + Char(b); }
		public static Seq  operator + (Symbol a, Pred b) { return Sym(a) + b; }
		public static Seq  operator + (Pred a, Symbol b) { return a + Sym(b); }
		public static Seq  operator + (Pred a, Pred b) { return new Seq(a, b); }
		public static Pred operator | (char a, Pred b) { return Char(a) | b; }
		public static Pred operator | (Pred a, char b) { return a | Char(b); }
		public static Pred operator | (Symbol a, Pred b) { return Sym(a) | b; }
		public static Pred operator | (Pred a, Symbol b) { return a | Sym(b); }
		public static Pred operator | (Pred a, Pred b) { return Or(a, b, false); }
		public static Pred operator / (Pred a, Pred b) { return Or(a, b, true); }
		public static Pred Or(Pred a, Pred b, bool ignoreAmbig)
		{
			TerminalPred a_ = a as TerminalPred, b_ = b as TerminalPred;
			if (a_ != null && b_ != null && a_.CanMerge(b_))
				return a_.Merge(b_);
			else
				return new Alts(null, a, b, ignoreAmbig);
		}
		public static Alts Star (Pred contents, bool? greedy = null) { return new Alts(null, LoopMode.Star, contents, greedy); }
		public static Alts Opt (Pred contents, bool? greedy = null) { return new Alts(null, LoopMode.Opt, contents, greedy); }
		public static Seq Plus (Pred contents, bool? greedy = null) { return contents + new Alts(null, LoopMode.Star, contents.Clone(), greedy); }
		public static TerminalPred Range(char lo, char hi) { return new TerminalPred(null, lo, hi); }
		public static TerminalPred Set(IPGTerminalSet set) { return new TerminalPred(null, set); }
		public static TerminalPred Set(string set) { return Set(PGIntSet.Parse(set)); }
		public static TerminalPred Sym(params Symbol[] s) { return new TerminalPred(null, PGSymbolSet.With(s)); }
		public static TerminalPred NotSym(params Symbol[] s) { return new TerminalPred(null, PGSymbolSet.Without(s)); }
		public static TerminalPred Char(char c) { return new TerminalPred(null, c); }
		public static TerminalPred Chars(params char[] c)
		{
			var set = PGIntSet.WithChars(c.Select(ch => (int)ch).ToArray());
			return new TerminalPred(null, set);
		}
		public static Seq Seq(string s)
		{
			return new Seq(null) { List = s.Select(ch => (Pred)Char(ch)).ToList() };
		}
		public static Rule Rule(string name, Pred pred, bool isStartingRule = false, bool isToken = false, int maximumK = -1)
		{
			return new Rule(null, GSymbol.Get(name), pred, isStartingRule) { IsToken = isToken, K = maximumK };
		}
		public static Pred operator + (Node pre, Pred p)
		{
			if (p.PreAction == null)
				p.PreAction = pre;
			else
				p.PreAction = AppendAction(pre, p.PreAction);
			return p;
		}
		public static Pred operator + (Pred p, Node post)
		{
			p.PostAction = AppendAction(p.PostAction, post);
			return p;
		}
		public static Node AppendAction(Node action, Node action2)
		{
			if (action == null)
				return action2;
			else {
				if (action.Calls(ecs.CodeSymbols.List))
					action = action.Unfrozen();
				else {
					var list = Node.NewSynthetic(ecs.CodeSymbols.List, action.SourceFile);
					list.Args.Add(action);
					action = list;
				}
				action.Args.AddSpliceClone(action2);
				return action;
			}
		}
		public static AndPred And(object test) { return new AndPred(null, test, false); }
		public static AndPred AndNot(object test) { return new AndPred(null, test, true); }
		
		public static Pred Set(string varName, Pred pred) {
			pred.ResultSaver = res => {
				var node = Node.NewSynthetic(CodeSymbols.Set, res.SourceFile);
				node.Args.Add(Node.NewSynthetic(GSymbol.Get(varName), res.SourceFile));
				node.Args.Add(res);
				return node;
			};
			return pred;
		}
		public static Pred SetVar(string varName, Pred pred) {
			pred.ResultSaver = res => {
				// #var(#missing, \varName(\res))
				var outer = Node.NewSynthetic(CodeSymbols.Var, res.SourceFile);
				outer.Args.Add(Node.Missing);
				var inner = Node.NewSynthetic(GSymbol.Get(varName), res.SourceFile);
				outer.Args.Add(inner);
				inner.Args.Add(res);
				return outer;
			};
			return pred;
		}
		public static Pred Op(string varName, Symbol @operator, Pred pred)
		{
			pred.ResultSaver = res => {
				var node = Node.NewSynthetic(@operator, res.SourceFile);
				node.Args.Add(Node.NewSynthetic(GSymbol.Get(varName), res.SourceFile));
				node.Args.Add(res);
				return node;
			};
			return pred;
		}

		/// <summary>Deep-clones a predicate tree. Terminal sets and Nodes 
		/// referenced by the tree are not cloned; the clone's value of
		/// <see cref="Next"/> will be null. The same <see cref="Pred"/> cannot 
		/// appear in two places in a tree, so you must clone before re-use.</summary>
		public virtual Pred Clone()
		{
			var clone = (Pred)MemberwiseClone();
			clone.Next = null;
			return clone;
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
		public override string ToString()
		{
			return Rule.Name.Name;
		}
	}
	
	/// <summary>Represents a sequence of predicates (<see cref="Pred"/>s).</summary>
	public class Seq : Pred
	{
		public override void Call(PredVisitor visitor) { visitor.Visit(this); }
		public Seq(Node basis) : base(basis) {}
		public Seq(Pred one, Pred two) : base(null)
		{
			if (one is Seq)
				List.AddRange((one as Seq).List);
			else
				List.Add(one);
			if (two is Seq)
				List.AddRange((two as Seq).List);
			else
				List.Add(two);
		}
		public List<Pred> List = new List<Pred>();

		public override bool IsNullable
		{
			get { return List.TrueForAll(p => p.IsNullable); }
		}
		public override Pred Clone()
		{
			Seq clone = (Seq)base.Clone();
			clone.List = new List<Pred>(List.Select(pred => pred.Clone()));
			return clone;
		}
		public override string ToString()
		{
			return StringExt.Join(" ", List);
		}
	}
	
	/// <summary>Describes a series of alternatives (branches), a kleene star 
	/// (`*`), or an optional element (`?`).</summary>
	/// <remarks>
	/// Branches, stars and optional elements are represented by the same class 
	/// because they all require prediction, and prediction works the same way for 
	/// all three.
	/// <para/>
	/// The one-or-more operator '+' is represented simply by repeating the 
	/// contents once, i.e. (x+) is converted to (x x*), which is a Seq of
	/// two elements: x and an Alts object that contains x.
	/// </remarks>
	public class Alts : Pred
	{
		public override void Call(PredVisitor visitor) { visitor.Visit(this); }

		public Alts(Node basis, LoopMode mode, bool? greedy = null) : base(basis)
		{
			Mode = mode;
			Greedy = greedy;
		}
		public Alts(Node basis, Pred a, Pred b, bool ignoreAmbig = false) : this(basis, LoopMode.None)
		{
			Add(a);
			int boundary = Arms.Count;
			Add(b);
			if (ignoreAmbig)
				NoAmbigWarningFlags |= 3ul << (boundary - 1);
		}
		public Alts(Node basis, LoopMode mode, Pred contents, bool? greedy = null) : this(basis, mode, greedy)
		{
			Debug.Assert(mode == LoopMode.Star || mode == LoopMode.Opt);
			var contents2 = contents as Alts;
			if (contents2 != null) {
				if (contents2.Mode == LoopMode.Opt || contents2.Mode == LoopMode.Star)
					throw new ArgumentException(Localize.From("{0} predicate cannot directly contain {1} predicate", ToStr(mode), ToStr(contents2.Mode)));
				Arms = contents2.Arms;
				Greedy = greedy ?? contents2.Greedy;
				NoAmbigWarningFlags = contents2.NoAmbigWarningFlags;
			} else {
				Arms.Add(contents);
				Greedy = greedy;
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
		// default is greedy (meaning that in case of ambiguity between arms and exit, the arms win) but Nongreedy==null means a warning is printed while Nongreedy==false means a warning is not printed.
		public bool? Greedy = null;
		public List<Pred> Arms = new List<Pred>();
		/// <summary>Specifies the case that should be encoded as the default in the 
		/// prediction tree, i.e., the else clause in the if-else chain or the 
		/// "default:" label in the switch statement. Use 0 for the first arm (only 
		/// warning messages add 1 to arm indexes).</summary>
		public int DefaultArm = -1;
		public ulong NoAmbigWarningFlags = 0; // alts for which to suppress ambig warnings
		public bool HasExit { get { return Mode != LoopMode.None; } }
		public int ArmCountPlusExit
		{
			get { return Arms.Count + (HasExit ? 1 : 0); }
		}

		public void Add(Pred p)
		{
			var a = p as Alts;
			if (a != null && a.Mode == LoopMode.None) {
				NoAmbigWarningFlags |= a.NoAmbigWarningFlags << Arms.Count;
				Arms.AddRange(a.Arms);
			} else
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
		
		public override Pred Clone()
		{
			Alts clone = (Alts)base.Clone();
			clone.Arms = new List<Pred>(Arms.Select(arm => arm.Clone()));
			return clone;
		}

		/// <summary>After LLParserGenerator detects ambiguity, this method helps 
		/// decide whether to report it.</summary>
		internal bool ShouldReportAmbiguity(IEnumerable<int> alts, ulong suppressWarnings = 0, bool suppressExitWarning = false)
		{
			if (_ambiguitiesReported != null && _ambiguitiesReported.IsSupersetOf(alts))
				return false;

			// The rules:
			// 1. Ambiguity with exit should be reported iff Greedy==null
			// 2. Ambiguity involving branches should be reported if it 
			//    involves any branch without a NoAmbigWarningFlags bit set.
			int should = 0;
			foreach (int alt in alts) {
				Debug.Assert(alt < Arms.Count);
				if (alt == -1) {
					if (Greedy == null && !suppressExitWarning)
						return true;
					should--;
				} else {
					if (((NoAmbigWarningFlags | suppressWarnings) & (1ul << alt)) == 0)
						should++;
				}
			}
			return should > 0;
		}

		// The same ambiguity may be detected in different parts of a prediction 
		// tree. This set is used to prevent the same ambiguity from being reported
		// repeatedly.
		HashSet<int> _ambiguitiesReported;
		internal void AmbiguityReported(IEnumerable<int> arms)
		{
			if (_ambiguitiesReported == null)
				_ambiguitiesReported = new HashSet<int>(arms);
			else
				_ambiguitiesReported.UnionWith(arms);
		}

		public override string ToString()
		{
			string prefix = "(";
			if (Mode != LoopMode.None && Greedy.HasValue)
				prefix = Greedy.Value ? "greedy(" : "nongreedy(";
			
			var sb = new StringBuilder(prefix);
			for (int i = 0; i < Arms.Count; i++)
			{
				if (i > 0)
					sb.Append(((NoAmbigWarningFlags >> (i - 1)) & 3) == 3 ? " / " : " | ");
				sb.Append(((object)Arms[i] ?? "").ToString());
			}

			if (Mode == LoopMode.Opt)
				sb.Append(")?");
			else if (Mode == LoopMode.Star)
				sb.Append(")*");
			else
				sb.Append(")");
			return sb.ToString();
		}
	}
	/// <summary>Types of <see cref="Alts"/> objects.</summary>
	/// <remarks>Although x? can be simulated with (x|), we keep them as separate modes for reporting purposes.</remarks>
	public enum LoopMode { None, Opt, Star };

	/// <summary>Represents a "gate" (p => m), which is a mechanism to separate 
	/// prediction from matching in the context of branching (<see cref="Alts"/>).</summary>
	public class Gate : Pred
	{
		public override void Call(PredVisitor visitor) { visitor.Visit(this); }
		public Gate(Node basis, Pred predictor, Pred match) : base(basis) {
			G.Require(!(predictor is Gate) && !(match is Gate),
				"A gate '=>' cannot contain another gate");
			_predictor = predictor;
			_match = match;
		}
		Pred _predictor;
		Pred _match;
		public Pred Predictor { get { return _predictor; } }
		public Pred Match { get { return _match; } }

		public override bool IsNullable
		{
			get { return Predictor.IsNullable; }
		}
		public override Pred Clone()
		{
			Gate clone = (Gate)base.Clone();
			clone._predictor = _predictor.Clone();
			clone._match = _match.Clone();
			return clone;
		}
		public override string ToString()
		{
			return string.Format("{0} => {1}", Predictor, Match);
		}
	}

	/// <summary>Represents a zero-width assertion: either user-defined code to
	/// check a condition, or a predicate that scans ahead in the input and then
	/// backtracks to the starting point.</summary>
	public class AndPred : Pred, IEquatable<AndPred>
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
		public override Pred Clone()
		{
			return base.Clone();
		}
		public override string ToString()
		{
			var node = Pred as Node;
			if (node != null)
				return string.Format("&{{{0}}}", node.Print(NodeStyle.Expression));
			else
				return string.Format("&({0})", Pred);
		}
		public bool Equals(AndPred other)
		{
			return object.Equals(Pred, other.Pred) && Not == other.Not;
		}
		public override bool Equals(object obj)
		{
			return obj is AndPred && Equals(obj as AndPred);
		}
		public override int GetHashCode()
		{
			int hc = (Pred ?? "").GetHashCode();
			if (Not) hc = ~hc;
			return hc;
		}
	}

	/// <summary>Represents a terminal (which is a token or a character) or a set 
	/// of possible terminals (e.g. 'A'..'Z').</summary>
	public class TerminalPred : Pred
	{
		public override void Call(PredVisitor visitor) { visitor.Visit(this); }
		
		new public IPGTerminalSet Set;

		public TerminalPred(Node basis, char ch) : base(basis) { Set = new PGIntSet(new IntRange(ch), true); }
		public TerminalPred(Node basis, int ch) : base(basis) { Set = new PGIntSet(new IntRange(ch), false); }
		public TerminalPred(Node basis, char lo, char hi) : base(basis) { Set = new PGIntSet(new IntRange(lo, hi), true); }
		
		/// <summary>Initializes the object with the specified set.</summary>
		public TerminalPred(Node basis, IPGTerminalSet set, bool allowEOF = false) : base(basis) 
		{
			Set = allowEOF ? set : set.WithoutEOF();
		}

		// For combining with | operator; cannot merge if PreAction/PostAction differs between arms
		public virtual bool CanMerge(TerminalPred r)
		{
			return r.PreAction == PreAction && r.PostAction == PostAction;
		}
		public TerminalPred Merge(TerminalPred r, bool ignoreActions = false)
		{
			if (!ignoreActions && (PreAction != r.PreAction || PostAction != r.PostAction))
				throw new InvalidOperationException("Internal error: cannot merge TerminalPreds that have actions");
			return new TerminalPred(Basis, Set.Union(r.Set)) { PreAction = PreAction, PostAction = PostAction };
		}

		public override bool IsNullable
		{
			get { return false; }
		}
		public override string ToString() // for debugging
		{
			return Set.ToString();
		}
	}

	/// <summary>A container for the follow set of a <see cref="Rule"/>.</summary>
	public class EndOfRule : Pred
	{
		public EndOfRule() : base(null) { }
		public override void Call(PredVisitor visitor) { visitor.Visit(this); }
		public HashSet<Pred> FollowSet = new HashSet<Pred>();

		public override bool IsNullable
		{
			get { throw new NotImplementedException(); }
		}
	}
}
