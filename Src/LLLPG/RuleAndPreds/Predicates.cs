using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc;
using Loyc.Utilities;
using Loyc.Syntax;
using Loyc.Collections;
using Loyc.Math;
using Loyc.Collections.Impl;

namespace Loyc.LLParserGenerator
{
	using S = Loyc.Syntax.CodeSymbols;

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
	/// </ul>
	/// Each Pred object can be used only once in a grammar, because Preds contain context-
	/// sensitive state such as the <see cref="Next"/> field, which are used during grammar 
	/// analysis. A Pred must be Clone()d if one wants to use it multiple times.
	/// </remarks>
	public abstract class Pred : ICloneable<Pred>
	{
		public abstract void Call(PredVisitor visitor); // visitor pattern

		public Pred(LNode basis) { Basis = basis ?? LNode.Missing; }

		public LNode Basis { get; protected set; }
		public LNode PreAction;
		public LNode PostAction;

		protected internal Pred Prev; // For debugging only
		protected internal Pred Next; // The predicate that follows this one or EndOfRule

		/// <summary>A function that saves the result produced by the matching code 
		/// of this predicate (null if the result is not saved). For example, if 
		/// the parser generator is given the predicate <c>@[ x='a'..'z' ]</c>, the 
		/// default matching code will be @(Match('a', 'z')), and ResultSaver will 
		/// be set to a function that receives this matching code and returns 
		/// @(x = Match('a', 'z')) in response.</summary>
		public Func<LNode, LNode> ResultSaver;
		public LNode AutoSaveResult(LNode matchingCode)
		{
			return ResultSaver != null ? ResultSaver(matchingCode) : matchingCode;
		}

		/// <summary>Returns true if this predicate can match an empty input.</summary>
		public abstract bool IsNullable { get; }

		// Helper methods for creating a grammar without a source file (this is
		// used for testing and for bootstrapping the parser generator).
		public static Seq  operator + (char a, Pred b) { return Char(a) + b; }
		public static Seq  operator + (Pred a, char b) { return a + Char(b); }
		public static Seq  operator + (Pred a, Pred b) { return new Seq(a, b); }
		public static Pred operator | (char a, Pred b) { return Char(a) | b; }
		public static Pred operator | (Pred a, char b) { return a | Char(b); }
		public static Pred operator | (Pred a, Pred b) { return Or(a, b, false); }
		public static Pred operator / (Pred a, Pred b) { return Or(a, b, true); }
		public static Pred operator + (Pred a) { return a.Clone(); }
		public static Pred Or(Pred a, Pred b, bool slashJoined) { return Or(a, b, slashJoined, null); }
		public static Pred Or(Pred a, Pred b, bool slashJoined, LNode basis, BranchMode aMode = BranchMode.None, BranchMode bMode = BranchMode.None, IMessageSink sink = null)
		{
			TerminalPred a_ = a as TerminalPred, b_ = b as TerminalPred;
			if (a_ != null && b_ != null && a_.CanMerge(b_) && aMode == BranchMode.None && bMode == BranchMode.None) {
				return a_.Merge(b_);
			} else {
				return Alts.Merge(basis, a, b, slashJoined, aMode, bMode, sink);
			}
		}
		public static Alts Star (Pred contents, bool? greedy = null) { return new Alts(null, LoopMode.Star, contents, greedy); }
		public static Alts Opt (Pred contents, bool? greedy = null) { return new Alts(null, LoopMode.Opt, contents, greedy); }
		public static Seq Plus (Pred contents, bool? greedy = null) { return contents + new Alts(null, LoopMode.Star, contents.Clone(), greedy); }
		public static TerminalPred Range(char lo, char hi) { return new TerminalPred(null, lo, hi); }
		public static TerminalPred Set(IPGTerminalSet set) { return new TerminalPred(null, set); }
		public static TerminalPred Set(string set) { return Set(PGIntSet.Parse(set)); }
		public static TerminalPred Set(params LNode[] s) { return new TerminalPred(null, new PGNodeSet(s)); }
		public static TerminalPred Not(params LNode[] s) { return new TerminalPred(null, new PGNodeSet(s, true)); }
		public static TerminalPred Char(char c) { return new TerminalPred(null, c); }
		public static TerminalPred Chars(params char[] c)
		{
			var set = PGIntSet.WithChars(c.Select(ch => (int)ch).ToArray());
			return new TerminalPred(null, set);
		}
		public static Seq Seq(string s, LNode basis = null)
		{
			return new Seq(basis) { List = s.Select(ch => (Pred)Char(ch)).ToList() };
		}
		public static Rule Rule(string name, Pred pred, bool isStartingRule = false, bool isToken = false, int maximumK = -1)
		{
			return new Rule(null, GSymbol.Get(name), pred, isStartingRule) { IsToken = isToken, K = maximumK };
		}
		public static Pred operator + (LNode pre, Pred p)
		{
			if (p.PreAction == null)
				p.PreAction = pre;
			else
				p.PreAction = MergeActions(pre, p.PreAction);
			return p;
		}
		public static Pred operator + (Pred p, LNode post)
		{
			p.PostAction = MergeActions(p.PostAction, post);
			return p;
		}
		public static LNode MergeActions(LNode action, LNode action2)
		{
			return LNode.MergeLists(action, action2, S.Splice);
		}
		public static AndPred And(object test) { return new AndPred(null, test, false); }
		public static AndPred AndNot(object test) { return new AndPred(null, test, true); }

		static LNodeFactory F = new LNodeFactory(LNode.SyntheticSource);
		public static Pred Set(string varName, Pred pred) {
			pred.ResultSaver = res => {
				return F.Set(F.Id(varName), res);
			};
			return pred;
		}
		public static Pred SetVar(string varName, Pred pred) {
			pred.ResultSaver = res => {
				// #var(#missing, $varName($res))
				return F.Var(F._Missing, varName, res);
			};
			return pred;
		}
		public static Pred AddSet(string varName, Pred pred)
		{
			pred.ResultSaver = res => {
				return F.Call(F.Dot(F.Id(varName), F.Id("Add")), res);
			};
			return pred;
		}
		public static Pred Op(string varName, Symbol @operator, Pred pred)
		{
			pred.ResultSaver = res => {
				// $@operator($varName, $res)
				return F.Call(@operator, F.Id(varName), res);
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

		internal virtual void DiscardAnalysisResult() {}
		
		public string ToStringWithPosition()
		{
			var pos = Basis.Range.Begin;
			if (pos.Line < 1) return ToString();
			return string.Format("({0},{1}) {2}", pos.Line, pos.PosInLine, ToString());
		}
	}

	/// <summary>Represents a nonterminal, which is a reference to a rule.</summary>
	public class RuleRef : Pred
	{
		public override void Call(PredVisitor visitor) { visitor.Visit(this); }
		public RuleRef(LNode basis, Rule rule) : base(basis) { Rule = rule; }
		public new Rule Rule;
		public RVList<LNode> Params = RVList<LNode>.Empty; // Params.Args is a list of parameters
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
		public Seq(LNode basis) : base(basis) {}
		public Seq(Pred one, Pred two, LNode basis = null) : base(basis)
		{
			if (one is Seq) {
				PreAction = one.PreAction;
				List.AddRange((one as Seq).List);
				if (List.Count > 0) {
					var last = List[List.Count - 1];
					last.PostAction = Pred.MergeActions(last.PostAction, one.PostAction);
				} else
					PreAction = Pred.MergeActions(PreAction, one.PostAction);
			} else
				List.Add(one);

			if (two is Seq) {
				if (List.Count > 0) {
					var last = List[List.Count - 1];
					last.PostAction = Pred.MergeActions(last.PostAction, two.PreAction);
				} else
					PreAction = Pred.MergeActions(PreAction, two.PreAction);
				List.AddRange((two as Seq).List);
				PostAction = Pred.MergeActions(PostAction, two.PostAction);
			} else
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
	/// (*), or an optional element (?).</summary>
	/// <remarks>
	/// Branches, stars and optional elements are represented by the same class 
	/// because they all require prediction, and prediction works the same way for 
	/// all three.
	/// <para/>
	/// The one-or-more operator '+' is represented simply by repeating the 
	/// contents once, i.e. (x+) is converted to (x x*), which is a Seq of
	/// two elements: x and an Alts object that contains a clone of x (a clone, 
	/// because a Pred object can only exist in one place in a grammar). Thus, 
	/// there is no predicate that represents x+ itself.
	/// <para/>
	/// Alts has a few options beyond the LoopMode:
	/// - A greedy flag (applies to loops only)
	/// - An optional default branch number (DefaultArm)
	/// - An optional error branch (ErrorBranch), which may be set to the 
	///   DefaultErrorBranch.Value, and a ExitOnError flag
	/// - NoAmbigWarningFlags represents use of / rather than |
	/// <para/>
	/// I have an uneasy feeling that the code in here is overly complicated,
	/// particularly the way I handled slashes and merging Alts. I'm just not sure 
	/// what simpler techniques I should have used instead.
	/// </remarks>
	public class Alts : Pred
	{
		public override void Call(PredVisitor visitor) { visitor.Visit(this); }

		public Alts(LNode basis, LoopMode mode, bool? greedy = null) : base(basis)
		{
			Mode = mode;
			Greedy = greedy;
		}

		public Alts(LNode basis, LoopMode mode, Pred contents, bool? greedy = null) : this(basis, mode, greedy)
		{
			Debug.Assert(mode == LoopMode.Star || mode == LoopMode.Opt);
			var contents2 = contents as Alts;
			if (contents2 != null && contents2.PreAction == null && contents2.PostAction == null) {
				if (contents2.Mode == LoopMode.Opt || contents2.Mode == LoopMode.Star)
					throw new ArgumentException(Localize.From("{0} predicate cannot directly contain {1} predicate", ToStr(mode), ToStr(contents2.Mode)));
				Arms = contents2.Arms;
				Greedy = greedy ?? contents2.Greedy;
				_divisions = contents2._divisions.Clone();
				DefaultArm = contents2.DefaultArm;
				ErrorBranch = contents2.ErrorBranch;
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

		#region Code for merging arbitrary Preds into Alts (including Pred-Alts, Alts-Pred, Alts-Alts mergers)

		public static Alts Merge(LNode basis, Pred a, Pred b, bool slashJoined, BranchMode aMode, BranchMode bMode, IMessageSink warnings)
		{
			Alts aAlts = a as Alts, bAlts = b as Alts;
			if (aAlts != null && aMode == BranchMode.None) {
				return aAlts.Insert(basis, slashJoined, true, b, bMode, warnings);
			} else if (bAlts != null && bMode == BranchMode.None) {
				return bAlts.Insert(basis, slashJoined, false, a, aMode, warnings);
			} else {
				return TwoArms(basis, a, b, slashJoined, aMode, bMode, warnings);
			}
		}
		static Alts TwoArms(LNode basis, Pred a, Pred b, bool slashJoined, BranchMode aMode, BranchMode bMode, IMessageSink warnings)
		{
			var alts = OneArm(a, aMode);
			return alts.Insert(basis, slashJoined, true, b, bMode, warnings);
		}
		static Alts OneArm(Pred a, BranchMode aMode)
		{
			var alts = new Alts(a.Basis, LoopMode.None);
			if (aMode == BranchMode.ErrorExit || aMode == BranchMode.ErrorContinue) {
				alts.ErrorBranch = a;
				alts.ExitOnError = aMode == BranchMode.ErrorExit;
			} else {
				alts.Arms.Add(a);
				if (aMode == BranchMode.Default)
					alts.DefaultArm = 0;
			}
			return alts;
		}
		Alts Insert(LNode newBasis, bool slashJoined, bool append, Pred b, BranchMode bMode, IMessageSink warnings)
		{
			if (SupportsMerge()) {
				this.Basis = newBasis ?? this.Basis;
				Alts bAlts = b as Alts;
				int insertAt = append ? this.Arms.Count : 0, boundary = insertAt;
				if (bAlts != null && bMode == BranchMode.None && bAlts.SupportsMerge()) {
					for (int i = 0; i < bAlts.Arms.Count; i++)
						this.InsertSingle(ref insertAt, bAlts.Arms[i], bAlts.DefaultArm == i ? BranchMode.Default : BranchMode.None, warnings);
					if (bAlts.ErrorBranch != null)
						this.InsertSingle(ref insertAt, bAlts.ErrorBranch, bAlts.ExitOnError ? BranchMode.ErrorExit : BranchMode.ErrorContinue, warnings);
					Debug.Assert(bAlts.DefaultArm != -1); // bAlts has no exit branch
				} else {
					bAlts = null;
					this.InsertSingle(ref insertAt, b, bMode, warnings);
				}
				if (!append)
					boundary = insertAt;
				UpdateSlashDivs(slashJoined, boundary, append, bAlts);
				if (slashJoined) {
					ulong three = 3ul;
					if (bMode == BranchMode.ErrorExit || bMode == BranchMode.ErrorContinue)
						three = append ? 1u : 2u;
				}
				return this;
			} else {
				var copy = OneArm(this, BranchMode.None);
				copy.Insert(newBasis, slashJoined, append, b, bMode, warnings);
				return copy;
			}
		}

		private void InsertSingle(ref int atIndex, Pred b, BranchMode bMode, IMessageSink warnings)
		{
			if (bMode == BranchMode.ErrorExit || bMode == BranchMode.ErrorContinue) {
				if (ErrorBranch != null)
					warnings.Write(MessageSink.Error, b.Basis, "There is already an error branch.");
				else {
					ErrorBranch = b;
					ExitOnError = bMode == BranchMode.ErrorExit;
					if (atIndex < Arms.Count)
						Warning_ErrorBranchNotLast(ErrorBranch, warnings);
				}
			} else {
				if (atIndex == Arms.Count && ErrorBranch != null)
					Warning_ErrorBranchNotLast(ErrorBranch, warnings);
				Arms.Insert(atIndex, b);
				if (bMode == BranchMode.Default) {
					if (DefaultArm != null) {
						int a = DefaultArm.Value;
						warnings.Write(MessageSink.Error, b.Basis, "There is already a default branch");
					} else
						DefaultArm = atIndex;
				}
				atIndex++;
			}
		}

		private static void Warning_ErrorBranchNotLast(Pred b, IMessageSink warnings)
		{
			warnings.Write(MessageSink.Warning, b.Basis, "The error branch should come last to avoid confusion. It is not numbered like the others, e.g. 'c' is considered the second arm in (a | error b | c).");
		}

		private bool SupportsMerge()
		{
			return Mode == LoopMode.None && PreAction == null && PostAction == null;
		}
	
		#endregion

		public LoopMode Mode = LoopMode.None;
		/// <summary>Specifies whether the loop is greedy or nongreedy (ignored for 
		/// non-loops). This flag is used in case of ambiguity between between the 
		/// arms and exit branch; if the loop is greedy, the arms win; if not, loop
		/// exits.</summary>
		/// <remarks>
		/// <c>Greedy == null</c> by default. This means that the loop is still 
		/// greedy, but a warning is printed if ambiguity is detected.
		/// </remarks>
		public bool? Greedy = null;
		public List<Pred> Arms = new List<Pred>();
		
		public IEnumerable<Pred> ArmsAndCustomErrorBranch
		{
			get { 
				if (ErrorBranch == null || ErrorBranch == DefaultErrorBranch.Value)
					return Arms;
				else
					return Arms.Concat(Enumerable.Repeat(ErrorBranch, 1));
			}
		}
		public bool HasErrorBranch(LLParserGenerator llpg)
		{
			return ErrorBranch != null || (DefaultArm == null && llpg.NoDefaultArm);
		}
		public bool HasDefaultErrorBranch(LLParserGenerator llpg)
		{
			return ErrorBranch == null ? llpg.NoDefaultArm : ErrorBranch == DefaultErrorBranch.Value;
		}
		public bool NonExitDefaultArmRequested()
		{
			return (uint)(DefaultArm ?? -1) < (uint)Arms.Count;
		}

		/// <summary>Specifies the action to take for error input. If an error 
		/// branch is specified, it serves as the default arm and DefaultArm has
		/// no significant effect. If ErrorBranch is null but DefaultArm is null 
		/// and the <see cref="LLParserGenerator.NoDefaultArm"/> flag is set, a 
		/// default error handler is generated.</summary>
		public Pred ErrorBranch = null;

		// TODO: Support not implemented
		public bool ExitOnError = true;

		/// <summary>Specifies the case that should be encoded as the default in the 
		/// prediction tree, i.e., the else clause in the if-else chain or the 
		/// "default:" label in the switch statement.</summary>
		/// <remarks>Use 0 for the first arm (only warning messages add 1 to arm 
		/// indexes). -1 means that the exit branch is the default (if there is
		/// no exit branch, the last branch is the default instead?)</remarks>
		public int? DefaultArm = null;

		//public int DefaultArmInt()
		//{
		//	if (DefaultArm.HasValue) return DefaultArm.Value;
		//	if (Mode != LoopMode.None) return -1;
		//	return Arms.Count - 1;
		//}

		public bool HasExit { get { return Mode != LoopMode.None; } }
		public int ArmCountPlusExit
		{
			get { return Arms.Count + (HasExit ? 1 : 0); }
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
		//public Alts Clone(LNode newBasis)
		//{
		//    Alts copy = new Alts(newBasis, Mode, Greedy);
		//    copy.Arms = new List<Pred>(Arms);
		//    copy.ErrorBranch = ErrorBranch;
		//    copy.ExitOnError = ExitOnError;
		//    copy.DefaultArm = DefaultArm;
		//    copy.NoAmbigWarningFlags = NoAmbigWarningFlags;
		//    Debug.Assert(PredictionTree == null && _ambiguitiesReported == null);
		//    return copy;
		//}

		#region Helper code used by LLParserGenerator

		const int ExitAlt = -1;

		/// <summary>Computed by <see cref="LLParserGenerator.PredictionAnalysisVisitor"/>.</summary>
		internal LLParserGenerator.PredictionTree PredictionTree;
		internal override void DiscardAnalysisResult() { PredictionTree = null; }

		/// <summary>After LLParserGenerator detects ambiguity, this method helps 
		/// decide whether to report it.</summary>
		internal bool ShouldReportAmbiguity(List<int> alts, ulong extraSuppression = 0, bool suppressExitWarning = false)
		{
			// The rules:
			// 1. Ambiguity with exit should be reported iff Greedy==null
			// 2. Ambiguity reporting between two branches is controlled by _slashDivs.
			// 3. Don't report ambiguities that are just subsets of previous warnings
			bool suppressExit = Greedy != null || suppressExitWarning;
			alts.RemoveAll(alt => {
				if (alt == ExitAlt)
					return suppressExit;
				else
					return alts.All(a => a == alt ||
						(a == ExitAlt ? suppressExit : ShouldSuppressWarning(alt, a, extraSuppression)));
			});
			return alts.Count > 0 && !AvoidRepeatedWarnings(alts);
		}

		// The same ambiguity may be detected in different parts of a prediction 
		// tree. This set is used to prevent the same ambiguity from being reported
		// repeatedly.
		MSet<int> _ambiguitiesReported;
		private bool AvoidRepeatedWarnings(IEnumerable<int> arms)
		{
			if (_ambiguitiesReported == null) {
				_ambiguitiesReported = new MSet<int>(arms);
				return false;
			} else {
				return _ambiguitiesReported.UnionWith(arms) == 0;
			}
		}

		#endregion

		#region Slash region tracking

		// _slashDivs keeps track of how '/' operators were used to construct the 
		// list of alternatives, which determines how ambiguity messages can be
		// suppressed. a|b expressions are of only transient importance: they 
		// do not suppress ambiguity messages but the left boundary of the next 
		// operator that comes along, if any, depends on the previous operator,
		// and ToString() pays attention to them too.
		//
		// This is a graphical representation of how the _slashDivs list is built
		// and what it will contain when derived from a complex tree of alts:
		//
		//     (a0 / a1 / a2) / (a3 | (a4 | a5) / a6) | a7 / ((a8 | a9) / (a10 | a11 / a12))
		// [0] 0   /1   2      ........................     ................................
		// [1] 0        /2    3:    :0    |1    2     :     .0    |1   2:      :0    /1   2.
		//                     :    :0          /2   3:     .           :      :..Merge+1..:
		//                     :    :.....Merge+1.....:     :           :      1     /2   3:
		//                     :    1     |2    3     :     :           :0     |1         3:
		//                     :    1           /3   4:     :           :.....Merge+2......:
		//                     :    .......Done.......:     :                  3     /4   5:
		//                     :0   |1               4:     :           2      |3         5:
		//                     :                      :     :           ........Done.......:
		//                     :........Merge+3.......:     :0          /2                5:
		// *                         4    |5    6           :                              :
		// [3]                       4          /6    7     :                              :
		// *                   3    |4                7     :                              :
		//                    ...........Done..........     :                              :
		// [4] 0              3/                      7     :............Merge+1...........:
		//                                                  1     |2    3                  :
		//                                            :                        4     /5   6:
		//                                            :                 3      |4         6:
		//                                            :     1           /3                6:
		//                                            :0   /1                             6: 
		//                                            :..............Merge+7...............: 
		// *                                                8     |9    10           
		// [5]                                                                 11    /12  13
		// *                                                            10     |11        13
		// [6]                                              8           /10               13
		// [7]                                        7    /8                             13
		//                                            ................Done..................
		// *   0                                      |7                                  13
		//                                           
		// Legend:
		// L  /M   R      Represents a Division with Left=L, Slash=true, Mid=M, Right=R
		// L  |M   R      Represents a transient value of _lastDivision for non|slash
		// *              This line never actually exists
		//
		// .............. Specifies the original set of Divisions that were created for
		// : Dotted box : a list of alternatives in parenthesis. Below the "Merge" line
		// :..Merge+N...: is the same list of Divisions with an offset added.
		//
		// N<==           Left bound moved left due to slash operator on line above
		// N<=()          Left bound moved left when left side is in (parens) on line above
		//
		// [N]            ignoring dotted boxes, this represents the index in the final
		//                _slashDivs list of the Division on this line.
		InternalList<Division> _divisions = InternalList<Division>.Empty;
		[DebuggerDisplay(@"{Left},{Mid}{Slash?""/"":""|""},{Right}")]
		struct Division
		{
			public short Left, Mid, Right;
			public bool Slash;
		}

		internal bool ShouldSuppressWarning(int armA, int armB, ulong extraSuppression = 0)
		{
			foreach (var div in _divisions)
			{
				if (div.Slash &&
					MathEx.IsInRange(armA, div.Left, div.Right - 1) &&
 					MathEx.IsInRange(armB, div.Left, div.Right - 1) &&
					(armA < div.Mid) != (armB < div.Mid))
					return true;
			}
			return (extraSuppression & (1uL << armA)) != 0
				&& (extraSuppression & (1uL << armB)) != 0;
		}

		private void UpdateSlashDivs(bool slashJoined, int boundary, bool append, Alts bAlts)
		{
			Debug.Assert(boundary > 0 && boundary < Arms.Count);
			Division prev = _divisions.LastOrDefault();
			var bDivs = bAlts != null ? bAlts._divisions : InternalList<Division>.Empty;
			if (append) {
				bDivs = Adjust(bDivs, boundary, true);
				_divisions.AddRange(bDivs);
			} else {
				prev = bAlts != null ? bAlts._divisions.LastOrDefault() : default(Division);
				Adjust(_divisions, boundary, false);
				_divisions.InsertRange(0, bDivs);
			}
			
			var newDiv = new Division {
				Left = 0, Mid = (short)boundary, Right = (short)Arms.Count, Slash = slashJoined
			};
			_divisions.Add(newDiv);
		}
		private static InternalList<Division> Adjust(InternalList<Division> bDivs, int offset, bool clone)
		{
			if (clone && bDivs.Count != 0)
				bDivs = bDivs.Clone();
			for (int i = 0; i < bDivs.Count; i++) {
				var div = bDivs[i];
				div.Left += (short)offset; div.Mid += (short)offset; div.Right += (short)offset;
				bDivs[i] = div;
			}
			return bDivs;
		}

		#endregion

		public override string ToString()
		{
			Pair<byte,byte>[] parens = new Pair<byte,byte>[Arms.Count + 1];
			ulong slashes = 0;
			Division prev = default(Division);
			for (int i = 0; i < _divisions.Count; i++)
			{
				var d = _divisions[i];
				if (!(prev.Right == d.Mid && prev.Left <= d.Left && (prev.Slash || !d.Slash)))
					MaybeAddParens(parens, d.Left, d.Mid);
				MaybeAddParens(parens, d.Mid, d.Right);
				if (d.Slash)
					slashes |= (1uL << (d.Mid - 1));
				prev = d;
			}

			string prefix = "", suffix = "";
			if (Arms.Count >= 3) {
				prefix = "( "; suffix = " )"; // make output easier to read
			} else {
				prefix = "("; suffix = ")";
			}
			var sb = new StringBuilder(prefix);
			if (Mode != LoopMode.None && Greedy.HasValue)
				sb.Insert(0, Greedy.Value ? "greedy" : "nongreedy");

			for (int i = 0; i < Arms.Count; i++)
			{
				sb.Append(')', parens[i].A);
				if (i > 0)
					sb.Append(((slashes >> (i - 1)) & 1) != 0 ? " / " : " | ");
				sb.Append('(', parens[i].B);
				if (DefaultArm == i)
					sb.Append("default ");
				sb.Append(((object)Arms[i] ?? "").ToString());
			}
			sb.Append(')', parens[Arms.Count].A);

			sb.Append(suffix);
			if (Mode == LoopMode.Opt)
				sb.Append('?');
			else if (Mode == LoopMode.Star)
				sb.Append('*');
			return sb.ToString();
		}
		private void MaybeAddParens(Pair<byte, byte>[] parens, int L, int R)
		{
			if (L + 1 < R) {
				parens[L].B++;
				parens[R].A++;
			}
		}
		
		// Returns a short name for an arm, for use in error/warning messages
		public string AltName(int altNum)
		{
			if (altNum == ExitAlt) return "exit";
			string descr;
			if ((uint)altNum < (uint)Arms.Count)
				if ((descr = Arms[altNum].ToString()).Length <= 9 + altNum)
					return (altNum + 1).ToString() + ":" + descr;
			return (altNum + 1).ToString();
		}
	}
	/// <summary>Types of <see cref="Alts"/> objects.</summary>
	/// <remarks>Although x? can be simulated with (x|), we keep them as separate modes for reporting purposes.</remarks>
	public enum LoopMode { None, Opt, Star };

	/// <summary>Types of branches in an <see cref="Alts"/> object (used during parsing only).</summary>
	public enum BranchMode { None, Default, ErrorExit, ErrorContinue };

	/// <summary>Represents a "gate" (p => m), which is a mechanism to separate 
	/// prediction from matching in the context of branching (<see cref="Alts"/>).</summary>
	/// <remarks>Gates are explained futher in this article:
	/// http://www.codeproject.com/Articles/688152/The-Loyc-LL-k-Parser-Generator-Part-2
	/// </remarks>
	public class Gate : Pred
	{
		public override void Call(PredVisitor visitor) { visitor.Visit(this); }
		public Gate(LNode basis, Pred predictor, Pred match) : base(basis) {
			G.Require(!(predictor is Gate) && !(match is Gate),
				"A gate '=>' cannot contain another gate");
			_predictor = predictor;
			_match = match;
		}
		Pred _predictor;
		Pred _match;

		/// <summary>Left-hand side of the gate, which is used for prediction decisions.</summary>
		public Pred Predictor { get { return _predictor; } }
		/// <summary>Right-hand side of the gate, which is used for matching decisions.</summary>
		public Pred Match { get { return _match; } }

		/// <summary>If true, this is an "&lt;=>" equivalency gate, otherwise it's 
		/// a "=>" normal gate.</summary>
		/// <remarks>This controls the follow set of the predictor. For a "=>" 
		/// normal gate, the predictor (left-hand side) has anything (_*) as its 
		/// follow set. For an equivalency, in which the predictor side and the
		/// match side are intended to represent the same language, the follow set
		/// of the predictor will be the same as that of the match; both are set
		/// to whatever follows the gate as a whole.
		/// <para/>
		/// The "=>" normal gate is often used to simplify or tweak prediction 
		/// decisions, e.g. in this example the gate is intended to ensure that the
		/// caller will not call Number unless LA0 is ('.'|'0'..'9'):
		/// <code>
		/// token Number  @[ '.'|'0'..'9' =>
		///                  '0'..'9'* ('.' '0'..'9'+)? ];
		/// </code>
		/// Notice that the predictor is, in general, shorter than the match side.
		/// It could potentially confuse the caller if the follow set of the 
		/// predictor and match sides were the same. An equivalency gate, on the 
		/// other hand, should be the same length as the match side, so it can have 
		/// the same follow set (I don't have a use case in mind for an equivalency 
		/// gate yet).
		/// </remarks>
		public bool IsEquivalency { get; set; }

		public override bool IsNullable
		{
			// FIXME
			//
			// There is no easy answer here. Whether a gate can actually match 
			// an empty input requires that 
			// (1) Match is nullable
			// (2) Either the prediction logic is bypassed, or the prediction
			//     logic actually invokes the match logic in a situation where
			//     Match is nullable.
			// Here's an example from the test suite:
			//
			//     token Number ==> @[ ('0'..'9' | '.' '0'..'9') =>
			//                         '0'..'9'* ('.' '0'..'9'+)? ];
			//     token Tokens ==> @[ (Number / _)* ];
			//
			// If Number is called directly, then yes, it is nullable. However,
			// if Number is private and only invoked by Tokens, then we *still* 
			// can't tell if it is really nullable. If FullLLk is enabled then 
			// it is not nullable, but without FullLLk mode, Tokens will call 
			// Number on an input of '.' that is NOT followed by a digit, which 
			// will cause nothing to be matched.
			// 
			// It seems reasonable to err on the side of caution and report
			// that the Gate is nullable. However, this causes LLLPG to emit an
			// error (although codegen still works):
			//
			// --- Error: Arm #1 of this loop is nullable; the parser could loop forever without consuming any input.
			//
			// If LLLPG is going to report this, first of all, it should only be 
			// a warning since LLLPG doesn't know how to do the necessary analysis 
			// on a gate to know for sure. Secondly, there should be a way to
			// suppress the warning. Since there is no way to suppress the 
			// warning/error, I will be generous and report nullable only when 
			// both sides of the gate are nullable.
			get { return Match.IsNullable && Predictor.IsNullable; }
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
		public AndPred(LNode basis, object pred, bool not, bool local = false)
			: base(basis) { Pred = pred; Not = not; Local = local; }

		static readonly LNodeFactory F = new LNodeFactory(EmptySourceFile.Default);
		internal static readonly LNode SubstituteLA = F.Call(S.Substitute, F.Id("LA"));
		internal static readonly LNode SubstituteLI = F.Call(S.Substitute, F.Id("LI"));

		/// <summary>Inverts the condition if Not==true, so that if the 
		/// <see cref="Pred"/> matches, the <see cref="AndPred"/> does not 
		/// match, and vice versa.</summary>
		public new bool Not = false;

		/// <summary>A local and-predicate cannot be hoisted into calling rules.</summary>
		public bool Local = false;

		bool? _usesLA;
		/// <summary>Returns true if <see cref="Pred"/> contains <c>$LA</c>.</summary>
		public bool PredUsesLA
		{
			get {
				if (_usesLA == null) {
					var node = Pred as LNode;
					if (node == null)
						_usesLA = false; // syntactic predicates use $LI, not $LA
					else
						_usesLA = node.Descendants().Any(n => n.Equals(SubstituteLA));
				}
				return _usesLA.Value;
			}
		}
		
		/// <summary>The predicate to match and backtrack. Must be of type 
		/// <see cref="LNode"/> or <see cref="Pred"/>.</summary>
		public object Pred;

		public bool? Prematched;
		internal override void DiscardAnalysisResult() { Prematched = null; }

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
			var node = Pred as LNode;
			string and = Not ? "&!" : "&";
			if (node != null)
				return string.Format("{0}{{{1}}}", and, node.Print(NodeStyle.Expression));
			else
				return string.Format("{0}({1})", and, Pred);
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
		
		public bool? Prematched;
		internal override void DiscardAnalysisResult() { Prematched = null; }

		public TerminalPred(LNode basis, char ch) : base(basis) { Set = new PGIntSet(new IntRange(ch), true); }
		public TerminalPred(LNode basis, int ch) : base(basis) { Set = new PGIntSet(new IntRange(ch), false); }
		public TerminalPred(LNode basis, char lo, char hi) : base(basis) { Set = new PGIntSet(new IntRange(lo, hi), true); }
		
		/// <summary>Initializes the object with the specified set.</summary>
		public TerminalPred(LNode basis, IPGTerminalSet set, bool allowEOF = false) : base(basis) 
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
			return new TerminalPred(Basis, Set.Union(r.Set), true) { PreAction = PreAction, PostAction = PostAction };
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
		public EndOfRule(Rule containingRule) : base(null) { ContainingRule = containingRule; }
		public override void Call(PredVisitor visitor) { visitor.Visit(this); }
		public HashSet<Pred> FollowSet = new HashSet<Pred>();
		public Rule ContainingRule; // to aid debugging
		public override string ToString()
		{
			return string.Format("End of rule '{0}'", ContainingRule.Name);
		}
		public override bool IsNullable
		{
			get { throw new NotImplementedException(); }
		}
	}

	/// <summary>A singleton to be used as the value of <see cref="Alts.ErrorBranch"/>, representing the default error branch.</summary>
	public class DefaultErrorBranch : Pred
	{
		public static readonly DefaultErrorBranch Value = new DefaultErrorBranch();
		DefaultErrorBranch() : base(null) { }

		public override void Call(PredVisitor visitor)
		{
			throw new NotImplementedException();
		}
		public override bool IsNullable
		{
			get { throw new NotImplementedException(); }
		}
	}
}
