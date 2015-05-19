using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.Utilities;
using Loyc.Syntax;
using Loyc.Math;

namespace Loyc.LLParserGenerator
{
	using S = CodeSymbols;

	/// <summary>Encapsulates LLLPG, the Loyc LL Parser Generator, which generates
	/// LL(k) recursive-descent parsers.</summary>
	/// <remarks>
	/// LLLPG is a new LL(k) parser generator under the umbrella of the Loyc 
	/// project (http://loyc.net).
	/// <para/>
	/// LLLPG generates recursive-descent parsers for LL(k) grammars. It is 
	/// designed for parsing computer languages, not natural languages. It also
	/// it supports "syntactic predicates" which are zero-width syntactic 
	/// assertions, and "semantic predicates" which are arbitrary expressions.
	/// <para/>
	/// The LLParserGenerator class is the core engine. It generates parsers in the 
	/// form of a Loyc tree, which can be printed out as C# code. Look at the 
	/// documentation of the Run() method for an overview of how the LLLPG engine 
	/// works.
	/// <para/>
	/// Note: the input to LLLPG is usually provided in the form of LES/EC# source code.
	/// In that case, there is no need to use this class directly. The source code of
	/// <see cref="LLLPG.Main"/> shows how to invoke LLLPG as a macro via the 
	/// <see cref="LeMP.Compiler"/>.
	/// <para/>
	/// For more information about how to use LLLPG, read these articles:
	/// http://www.codeproject.com/Articles/664785/A-New-Parser-Generator-for-Csharp
	/// http://www.codeproject.com/Articles/688152/The-Loyc-LL-k-Parser-Generator-Part-2
	/// http://loyc-etc.blogspot.ca/2013/11/lllpg-greedy-and-nongreedy.html
	/// http://loyc-etc.blogspot.ca/2013/12/bogus-ambiguity-warnings-in-lllpg.html
	/// </remarks>
	public partial class LLParserGenerator
	{
		public LLParserGenerator(IPGCodeGenHelper csg, IMessageSink sink = null)
		{ 
		 	_helper = csg; Sink = sink;
		}

		/// <summary>Specifies the default maximum lookahead for rules that do
		/// not specify a lookahead value.</summary>
		public int DefaultK = 2;
		
		/// <summary>Normally, the last arm in a list of alternatives is chosen
		/// as the default. For example, in ("Foo" | "Bar"), the second branch is
		/// taken unless the input begins with 'F'. However, if this flag is true,
		/// there is no default arm on <see cref="Alts"/> unless one is specified
		/// explicitly, so a special error branch is generated when none of the 
		/// alternatives apply. This increases code size and decreases speed, but 
		/// the generated parser may give better error messages.</summary>
		/// <remarks>When this flag is false, an error branch is still generated
		/// on a particular loop if requested with <see cref="Alts.ErrorBranch"/>.</remarks>
		public bool NoDefaultArm = false;
		
		/// <summary>Enables full LL(k) instead of "partly approximate" lookahead.</summary>
		/// <remarks>
		/// LLLPG's standard disambiguation mode is similar to the "linear 
		/// approximate" lookahead present in the ANTLR v2 parser generator.
		/// The original linear approximate lookahead fails to predict the 
		/// following case correctly:
		/// <code>
		///     Foo ==> @[ ('a' 'b' | 'c' 'd') ';' 
		///              | 'a' 'd'             ';' ];
		/// </code>
		/// LLLPG has no problem with this case. However, LLLPG's "somewhat
		/// approximate" lookahead still has problems with certain cases involving
		/// nested alternatives. Here's a case that it can't handle:
		/// <code>
		///     Foo ==> @[ ('a' 'b' | 'b' 'a') ';' 
		///              | ('a' 'a' | 'b' 'b') ';' ];
		/// </code>
		/// Basically here's what goes wrong: LLLPG detects that both alternatives
		/// can start with 'a' or 'b'. The way it normally builds a prediction tree
		/// is by creating a test for the common set between two alternatives:
		/// <code>
		///     la0 = LA(0);
		///     if (la0 == 'a' || la0 == 'b') { ... alt 1 or alt 2 ... }
		/// </code>
		/// Then, inside that "if" statement it adds a test for LA(1). Sadly,
		/// LLLPG discovers that if (la1 == 'a' || la1 == 'b'), both alternatives 
		/// still apply. Thus, it can't tell the difference between the two and
		/// gives up, picking the first alternative unconditionally and printing
		/// a warning that "Branch 2 is unreachable".
		/// <para/>
		/// To fix this, LLLPG must figure out that it should split the LA(0) test 
		/// into two separate "if" clauses. I've figured out how to do this, but
		/// the new code is experimental, it creates subtly different results than 
		/// standard prediction, which causes the test suite to fail, it sometimes 
		/// uses too many branches that are not merged properly, I suspect it
		/// might be substantially slower at code generation in some cases, and
		/// finally I am worried that it will make the generated code much larger
		/// sometimes (although I have not actually found or seen such a case).
		/// <para/>
		/// So, full LL(k) is disabled by default, but you can enable it if you
		/// encounter a problem like this.
		/// </remarks>
		public bool FullLLk = false;

		/// <summary>Gets or sets the verbosity level. Verbose output can help
		/// you debug grammars that don't produce the expected code.</summary>
		/// <remarks>
		/// Level 1 verbosity prints simplified prediction trees in each rule,
		/// and the follow sets of each rule.
		/// Level 2 verbosity prints prediction trees before they are simplified,
		/// and before they have been extended to handle unspecified cases (e.g. 
		/// if your rule says 'a' 'b' | 'c' 'd', the unspecified cases are all 
		/// other possible inputs.)
		/// Level 3 verbosity prints level 1 and 2 information.
		/// </remarks>
		public int Verbosity = 0;

		/// <summary>Whether to insert Specifies the default maximum lookahead for rules that do
		/// not specify a lookahead value.</summary>
		public bool AddComments = true;

		/// <summary>Called when an error or warning occurs while parsing a grammar
		/// or while generating code for a parser. Also called to print "verbose" 
		/// messages.</summary>
		/// <remarks>The parameters are (1) a Node that represents the location of 
		/// the error, or Node.Missing if the grammar was created programmatically 
		/// without any source code backing it; (2) a predicate related to the error, 
		/// or null if the error is a syntax error; (3) "Warning" for a warning,
		/// "Error" for an error, or "Verbose"; and (4) the text of the error 
		/// message.</remarks>
		public IMessageSink Sink { 
			get { return _sink; } 
			set {
				_sink = value ?? MessageSink.Null;
				#if DEBUG
				_sink = new MessageSplitter(_sink, MessageSink.Trace);
				#endif
			}
		}
		IMessageSink _sink;
		
		Dictionary<Symbol, Rule> _rules = new Dictionary<Symbol, Rule>();
		List<Rule> _rulesInOrder = new List<Rule>(); // in the order they were added

		protected static Severity Warning = Severity.Warning;
		protected static Severity Error = Severity.Error;
		protected static Severity Verbose = Severity.Verbose;
		private void Output(Severity type, Pred pred, string msg)
		{
			Sink.Write(type, pred, msg);
		}

		#region Step 1: AddRules (see also the Macros, StageOneParser & StageTwoParser classes)

		public void AddRules(params Rule[] rules) { AddRules((IEnumerable<Rule>)rules); }
		public void AddRules(IEnumerable<Rule> rules)
		{
			_rulesInOrder.AddRange(rules);
			AddRulesToDict(rules, _rules); 
		}
		public void AddRule(Rule rule) { 
			_rulesInOrder.Add(rule); 
			_rules.Add(rule.Name, rule);
		}
		public static Dictionary<Symbol, Rule> AddRulesToDict(IEnumerable<Rule> rules, Dictionary<Symbol, Rule> dict = null)
		{
			dict = dict ?? new Dictionary<Symbol, Rule>();
			foreach (var rule in rules)
				dict.Add(rule.Name, rule);
			return dict;
		}

		#endregion

		#region Step 2a: ApplyInlines (also, see AutoValueSaverVisitor in a separate file)

		// Finds rules for which the user has requested inlining. Inlined rules 
		// are pasted (unhygienically) into the rule that refers to them. Only
		// one level of inlining is performed, so I don't have to think about the 
		// horror of infinite recursion.
		// TODO: move the logic for merging nested Alts from StageTwoParser to Alts 
		//       itself so ApplyInlines can use it
		class ApplyInlines : RecursiveReplacementPredVisitor
		{
			Rule _curRule;
			IMessageSink _sink;
			public void Run(Rule rule, IMessageSink sink)
			{
				_curRule = rule;
				_sink = sink;
				Visit(rule.Pred);
			}
			public override void Visit(RuleRef rref)
			{
				if (rref.IsInline ?? rref.Rule.IsInline)
					if (rref.ResultSaver == null && rref.Params.IsEmpty) {
						Replacement = rref.Rule.Pred.Clone();
						Replacement.PreAction = Pred.MergeActions(rref.PreAction, Replacement.PreAction);
						Replacement.PostAction = Pred.MergeActions(Replacement.PostAction, rref.PreAction);
					} else {
						ParamError(rref);
					}
			}
			private void ParamError(object ctx)
			{
				_sink.Write(Severity.Error, ctx, "Inlining is not supported for rules that take parameters or return results.");
			}
		}

		#endregion

		#region Step 2b: Recognizer planning

		// "mini-recognizers" refer to the test methods produced in response to 
		// syntactic predicates that do not simply call another rule, e.g. 
		// &('.'|'0'..'9') or &('<' Id '>').
		MSet<Symbol> _miniRecognizerNames = new MSet<Symbol>();
		Dictionary<Pred, Rule> _miniRecognizerMap = new Dictionary<Pred,Rule>();
		
		// Produces sub-rules for &(...) syntactic predicates.
		// Must be done before everything else.
		class AddMiniRecognizers : RecursivePredVisitor
		{
			LLParserGenerator LLPG;
			public AddMiniRecognizers(LLParserGenerator llpg) { LLPG = llpg; }
			Rule _currentRule;

			internal void FindAndPreds(Rule rule)
			{
				_currentRule = rule;
				rule.Pred.Call(this);
			}
			public override void Visit(AndPred pred)
			{
				var synPred = pred.Pred as Pred;
				if (synPred != null) {
					var rref = pred.Pred as RuleRef;
					if (rref == null) {
						// Construct a rule from this predicate
						var synPred2 = synPred.Clone();
						var rule = new Rule(pred.Basis, null, synPred2, false);
						var recogName = Enumerable.Range(0, int.MaxValue)
							.Select(i => GSymbol.Get(string.Format("{0}_Test{1}", _currentRule.Name, i)))
							.First(n => !LLPG._miniRecognizerNames.Contains(n));
						rule.Name = recogName;
						rule.IsToken = true; // gives us a follow set of .*
						rule.IsPrivate = true;
						rule.IsRecognizer = true;
						rule.TryWrapperNeeded();
						LLPG._miniRecognizerNames.Add(recogName);
						LLPG._miniRecognizerMap[synPred] = rule;
						LLPG.AddRule(rule);
					} else {
						rref.Rule.MakeRecognizerVersion().TryWrapperNeeded();
					}
				}
			}
		}

		// Requests a recognizer (Scan_Xyz()) for each rule that is directly or 
		// indirectly referenced by another rule that will be turned into a recognizer.
		class AddRecognizersRecursively : RecursivePredVisitor
		{
			LLParserGenerator LLPG;
			public AddRecognizersRecursively(LLParserGenerator llpg) { LLPG = llpg; }
			public void Scan(Rule rule)
			{
				Debug.Assert(rule.HasRecognizerVersion);
				rule.Pred.Call(this);
			}
			public override void Visit(RuleRef rref)
			{
				if (!rref.Rule.HasRecognizerVersion) {
					rref.Rule.MakeRecognizerVersion();
					Scan(rref.Rule);
				}
			}
			public override void Visit(AndPred pred) { } // ignore &(...)
		}

		internal Rule GetRecognizerRule(Pred synPred)
		{
			var rref = synPred as RuleRef;
			if (rref != null)
				return rref.Rule.MakeRecognizerVersion();
			else
				return _miniRecognizerMap[synPred];
		}

		#endregion

		#region Step 2c: DetermineFollowSets() and related

		internal static TerminalPred EndOfToken;

		void DetermineFollowSets(IEnumerable<Rule> rules)
		{
			var anything = _helper.EmptySet.Inverted();
			EndOfToken = new TerminalPred(null, anything, true);
			EndOfToken.Next = EndOfToken;

			foreach (Rule rule in rules)
				new DetermineLocalFollowSets(this, EndOfToken).Run(rule);

			// Synthetic predicates to use as follow sets
			var eof = _helper.EmptySet.WithEOF();
			Pred eofAfterStartRule = new TerminalPred(null, eof, true);
			eofAfterStartRule.Next = eofAfterStartRule;

			// Add EOF as follow set for start rules and .* as follow set of "token" rules
			foreach (var rule in rules)
			{
				if (rule.IsToken) {
					rule.EndOfRule.FollowSet.Clear();
					rule.EndOfRule.FollowSet.Add(EndOfToken);
				} else if (rule.IsStartingRule)
					rule.EndOfRule.FollowSet.Add(eofAfterStartRule);
			}

			// Each rule's Next is always an EndOfRule object, which has a list 
			// of things that could follow the rule elsewhere in the grammar.
			// To determine the follow set of each rule, me must find all places 
			// where the rule is used...
			new DetermineRuleFollowSets().Run(rules);
		}

		/// <summary>Figures out the correct value of <see cref="Pred.Next"/> for 
		/// each sub-predicate in a rule.</summary>
		class DetermineLocalFollowSets : PredVisitor
		{
			LLParserGenerator LLPG;
			public DetermineLocalFollowSets(LLParserGenerator llpg, TerminalPred endOfToken) 
				{ LLPG = llpg; AnyFollowSet = endOfToken; }

			TerminalPred AnyFollowSet;

			public void Run(Rule rule)
			{
				Visit(rule.Pred, rule.EndOfRule);
			}
			void Visit(Pred pred, Pred next)
			{
				pred.Next = next;

				pred.Call(this);

				next.Prev = pred;

				// This is not related to follow sets, but we reset temporary state 
				// here in case code is generated from the same grammar repeatedly.
				pred.DiscardAnalysisResult();
			}

			public override void Visit(Seq seq)
			{
				var next = seq.Next;
				for (int i = seq.List.Count - 1; i >= 0; i--)
				{
					Visit(seq.List[i], next);
					next = seq.List[i];
				}
			}
			public override void Visit(Alts alts)
			{
				var next = (alts.Mode == LoopMode.Star ? alts : alts.Next);

				if (next == alts) {
					int badArm = alts.Arms.IndexWhere(arm => arm.IsNullable);
					if (badArm > -1) {
						LLPG.Output(Error, alts,
							alts.Arms.Count == 1 ? "The contents of this loop are nullable; the parser could loop forever without consuming any input."
							: string.Format("Arm #{0} of this loop is nullable; the parser could loop forever without consuming any input.", badArm + 1));
					}
				}

				foreach (var arm in alts.ArmsAndCustomErrorBranch)
					Visit(arm, next);
			}
			public override void Visit(Gate gate)
			{
				Visit(gate.Predictor,  gate.IsEquivalency ? gate.Next : AnyFollowSet);
				Visit(gate.Match, gate.Next);
			}
			public override void Visit(AndPred pred)
			{
				var child = pred.Pred as Pred;
				if (child != null)
					Visit(child, AnyFollowSet);
			}
			public override void Visit(RuleRef rref)
			{
				// *** NOTE *** If this assertion fails, it means that the grammar 
				// refers to a rule that is not in the grammar, i.e. the user forgot 
				// to call AddRule() for the referenced rule.
				Debug.Assert(LLPG._rules.TryGetValue(rref.Rule.Name, null) == rref.Rule);
			}
		}

		/// <summary>Populates each rule's <see cref="EndOfRule.FollowSet"/> 
		/// according to the predicates that follow each reference to the rule 
		/// in the entire grammar.</summary>
		/// <remarks>Ignores the <see cref="Rule.IsToken"/> flag.</remarks>
		class DetermineRuleFollowSets : RecursivePredVisitor
		{
			public DetermineRuleFollowSets() { }

			public void Run(IEnumerable<Rule> rules)
			{
				foreach (Rule rule in rules)
					rule.Pred.Call(this);
			}
			public override void Visit(RuleRef rref)
			{
				if (rref.Rule.IsToken) // bug fix: the Token flag is supposed to suppress warnings, 
					return;            // but warnings return if we add other stuff to the follow set.
				//if (rref.Next is EndOfRule)
				//	rref.Rule.EndOfRule.FollowSet.UnionWith((rref.Next as EndOfRule).FollowSet);
				//else
					rref.Rule.EndOfRule.FollowSet.Add(rref.Next);
			}
			public override void Visit(Alts pred)
			{
				// It's not immediately obvious whether to visit the error branch.
				// Do we want error branches to affect the follow sets of rules? Well,
				// I'll say yes (true), because if someone makes a complex error 
				// grammar I think they will probably want any called rules to 
				// consider the error branch.
				VisitChildrenOf(pred, true);
			}
		}

		#endregion

		#region Run()

		protected ISourceFile _sourceFile;
		protected RWList<LNode> _classBody;

		/// <summary>Generates a braced block of code {...} for the grammar 
		/// described by the rules that were previously added to this object 
		/// with <see cref="AddRule"/> or <see cref="AddRules(Rule[])"/>.</summary>
		/// <param name="sourceFile"></param>
		/// <returns>The generated parser class.</returns>
		/// <remarks>
		/// [This may be outdated, TODO: review it]
		/// <para/>
		/// By far the greatest difficulty in this process is generating prediction 
		/// code when the grammar branches: (<c>x | y | z</c>). Since this class 
		/// creates LL(k) parsers without memoization or implicit backtracking, it 
		/// relies on prediction trees to correctly decide <i>in advance</i> which 
		/// branch to follow.
		/// <para/>
		/// The following kinds of grammar elements require prediction:
		/// <para/>
		/// <ul>
		/// <li><c>a | b</c> (which is equivalent to <c>a / b</c>): prediction chooses between a and b</li>
		/// <li><c>a?</c>: prediction chooses between a and whatever follows a?</li>
		/// <li><c>a*</c>: prediction chooses between a and whatever follows a*</li>
		/// <li><c>(a | b)*: </c>prediction chooses between three alternatives (a, b, and exiting the loop).</li>
		/// <li><c>(a | b)?: </c>prediction chooses between three alternatives (a, b, and skipping both).</li>
		/// <li><c>a+</c>: exactly equivalent to <c>a a*</c></li>
		/// </ul>
		/// All of these are based on an <see cref="Alts"/> object.
		/// <para/>
		/// Let's look at a simple example of the prediction code generated for a rule 
		/// called "Foo":
		/// <code>
		/// // rule a @[ 'a' | 'A' ];
		/// // rule b @[ 'b' | 'B' ];
		/// // public rule Foo @[ a | b ];
		/// public void Foo()
		/// {
		///   var la0 = LA0;
		///   if (la0 == 'a' || la0 == 'A')
		///     a();
		///   else
		///     b();
		/// }
		/// </code>
		/// By default, to make prediction more efficient, the last alternative is 
		/// assumed to match if the others don't. So when <c>a</c> doesn't match, <c>b</c>
		/// is called even though it has not been verified to match yet. This behavior
		/// can be changed by setting <see cref="NoDefaultArm"/>=true.
		/// <para/>
		/// Alternatively, you can select the default using the 'default' keyword,
		/// which controls the <see cref="Alts.DefaultArm"/> property, e.g.
		/// <code>
		/// // public rule Foo ==> @[ default a | b ];
		/// public void Foo()
		/// {
		///   int la0;
		///   la0 = LA(0);
		///   if (la0 == 'b' || la0 == 'B')
		///     b();
		///   else
		///     a();
		/// }
		/// </code>
		/// In simple cases like this one that only require LL(1) prediction, 
		/// prediction and matching are merged into a single if-else chain. In more
		/// complicated cases, goto statements may be used to avoid code duplication
		/// (ANTLR uses pairs of if-else or switch statements instead, but I chose
		/// to use gotos because the generated code will be faster.) The if-else 
		/// statements are the "prediction" part of the code, while the calls to a() 
		/// and b() are the "matching" part.
		/// <para/>
		/// Here's another example:
		/// <code>
		/// // public rule Foo ==> @[ (a | b? 'c')* ];
		/// public void Foo()
		/// {
		///   int la0;
		///   for (;;) {
		///     la0 = LA(0);
		///     if (la0 == 'a' || la0 == 'A')
		///       a();
		///     else if (la0 == 'b' || la0 == 'B' || la0 == 'c') {
		///       la0 = LA(0);
		///       if (la0 == 'b' || la0 == 'B')
		///         b();
		///       Match('c');
		///     } else
		///       break;
		///   }
		/// }
		/// </code>
		/// A kleene star (*) always produces a "for(;;)" loop, while an optional item
		/// may produce a "do ... while(false)" pseudo-loop in some circumstances (but
		/// this case is too simple to require it). Here there are two separate 
		/// prediction phases: one for the outer loop <c>(a | b? 'c')*</c>,
		/// and one for <c>b?</c>.
		/// <para/>
		/// In this example, the loop appears at the end of the rule. In some such 
		/// cases, the "follow set" of the rule becomes relevant. In order for the 
		/// parser to decide whether to exit the loop or not, it may need to know what 
		/// can follow the loop. For instance, if <c>('a' 'b')*</c> is followed by 
		/// 'a'..'z' 'c', it is not possible to tell whether to stay in the loop or 
		/// exit just by looking at the first input character. If LA(0) is 'a', it is 
		/// necessary to look at the second character LA(1); only if the second 
		/// character is 'b' is it possible to conclude that 'a' 'b' should be matched.
		/// <para/>
		/// Therefore, before generating a parser one of the steps is to build the 
		/// follow set of each rule, by looking for places where a rule appears inside
		/// other rules. A rule is not aware of its current caller, so it gathers 
		/// information from all call sites and merges it together. When a rule is 
		/// marked "public", it is considered to be a starting rule, which causes 
		/// the follow set to include $ (which means "end of input").
		/// <para/>
		/// The fact that LLLPG is aware of follow sets and the differences between
		/// alternatives, and the fact that its generated parsers do not normally 
		/// backtrack, makes LLLPG's LL(k) parsing tecnique fundamentally different 
		/// from another popular parsing technique, PEG. The documentation of 
		/// <see cref="LLParserGenerator"/> explains further.
		/// <para/>
		/// Here's an example that needs more than one character of lookahead:
		/// <code>
		/// // public rule Foo ==> @[ 'a'..'z'+ | 'x' '0'..'9' '0'..'9' ];
		/// public void Foo()
		/// {
		///   int la0, la1;
		///   do {
		///     la0 = LA(0);
		///     if (la0 == 'x') {
		///       la1 = LA(1);
		///       if (la1 >= '0' &amp;&amp; '9' >= la1) {
		///         Match();
		///         Match();
		///         MatchRange('0', '9');
		///       } else
		///         goto match1;
		///     } else
		///       goto match1;
		///     break;
		///     match1:
		///     {
		///       Match();
		///       for (;;) {
		///         la0 = LA(0);
		///         if (la0 >= 'a' &amp;&amp; 'z' >= la0)
		///           Match();
		///         else
		///           break;
		///       }
		///     }
		///   } while (false);
		/// }
		/// </code>
		/// Here, the prediction and matching phases are merged for the second 
		/// alternative, but separate for the first alternative (because it is chosen 
		/// in two different places in the prediction logic). Notice that the matching 
		/// for alt 2 starts with <c>Match()</c> twice, with no arguments, but is 
		/// followed by <c>MatchRange('a', 'z')</c>. This demonstrates communication 
		/// from prediction to matching: the matching phase can tell that LA(0) is 
		/// confirmed to be 'x', and LA(1) is confirmed to be '0'..'9', so an 
		/// unconditional match suffices. However, nothing is known about LA(2) so its 
		/// value must be checked, which is what MatchRange() is supposed to do.
		/// <para/>
		/// In some cases, LA(0) is irrelevant. Consider this example:
		/// <code>
		/// // public rule Foo ==> @[ '(' 'a'..'z'* ')' | '(' '0'..'9'+ ')' ];
		/// public void Foo()
		/// {
		///   int la0, la1;
		///   la1 = LA(1);
		///   if (la1 >= 'a' &amp;&amp; 'z' >= la1) {
		///     Match('(');
		///     for (;;) {
		///       la0 = LA(0);
		///       if (la0 >= 'a' &amp;&amp; 'z' >= la0)
		///         Match();
		///       else
		///         break;
		///     }
		///     Match(')');
		///   } else {
		///     Match('(');
		///     MatchRange('0', '9');
		///     for (;;) {
		///       la0 = LA(0);
		///       if (la0 >= '0' &amp;&amp; '9' >= la0)
		///         Match();
		///       else
		///         break;
		///     }
		///     Match(')');
		///   }
		/// }
		/// </code>
		/// Here, the first character of both alternatives is always '(', so looking at
		/// LA(0) doesn't help choose which branch to take, and prediction skips ahead
		/// to LA(1).
		///
		/// <h3>And-predicates</h3>
		/// 
		/// An and-predicate specifies an extra condition on the input that must be 
		/// checked. Here is a simple example:
		/// <code>
		/// (&amp;{flag} '0'..'9' | 'a'..'z')
		/// </code>
		/// This example says that '0'..'9' is only allowed if the expression <c>flag</c>
		/// evaluates to true, otherwise 'a'..'z' is required. LLPG, however, gives
		/// and-predicates lower priority, and always inverts the order of the 
		/// testing: it checks for '0'..'9' first, then checks <c>flag</c> 
		/// afterward. I chose to make LLPG work this way because in general, and-
		/// predicates can be much more expensive to check than character sets; if 
		/// one of the alternatives rarely runs, it would be wasteful to check an 
		/// expensive and-predicate before checking if the input character could 
		/// possibly match. Therefore, the generated code looks like this:
		/// <code>
		/// la0 = LA(0);
		/// if (la0 >= '0' &amp;&amp; la0 &lt;= '9') {
		///    Check(flag);
		///    Match();
		/// } else
		///    MatchRange('a', 'z');
		/// </code>
		/// If you really need to make the and-predicate run first for some reason,
		/// I dunno. I got nothin'. Complain to me every month until I implement 
		/// something, maybe.
		/// <para/>
		/// A generated parser performs prediction in two interleaved parts: 
		/// character-set tests, and and-predicate tests. In this example,
		/// <code>
		/// ('0'..'9'+ | &amp;{hexAllowed} '0' 'x' ('0'..'9'|'a'..'f')+)
		/// </code>
		/// The code will look like this:
		/// <code>
		/// do {
		///   la0 = LA(0);
		///   if (la0 == '0') {
		///     if (hexAllowed) {
		///       la1 = LA(1);
		///       if (la1 == 'x') {
		///         Match();
		///         Match();
		///         MatchRange('0', '9', 'a', 'f');
		///         ...
		///       } else
		///         goto match1;
		///     } else
		///       goto match1;
		///   } else
		///     goto match1;
		///   break;
		///   match1:;
		///   {
		///     MatchRange('0', '9');
		///     ...
		///   }
		/// } while (false);
		/// </code>
		/// Here you can see the interleaving: first the parser checks LA(0), then 
		/// it checks the and-predicate, then it checks LA(1).
		/// <para/>
		/// LLPG (let's call it 1.0) does not support any analysis of the 
		/// <i>contents</i> of an and-predicate. Thus, without loss of generality,
		/// these examples use semantic predicates &amp;{...} instead of syntactic 
		/// predicates &amp;(...); LLPG can't "see inside them" either way.
		/// <para/>
		/// Even without analyzing the contents of an and-predicate, they can still
		/// make prediction extremely complicated. Consider this example:
		/// <code>
		/// (.&amp;{a} (&amp;{b} {B();} | &amp;{c})
		///   &amp;{d} [&amp;{e} ('e'|'E')]?
		///   (&amp;{f} ('f'|'t') | 'F')
		/// | &amp;{c} (&amp;{f} ('e'|'t') | 'f') 'g'
		/// | '!' )
		/// </code>
		/// In this example, the first branch requires 'a' and 'd' to be true, 
		/// there's a pair of zero-width alternatives that require 'b' or 'c' 
		/// to be true, {B()} must be executed if 'b' is true, 'e' must be true 
		/// if LA(0) is ('e'|'E'), 'f' must be true if LA(0) is 'f' and no 
		/// condition is required for 'F'. The second branch also allows 'e' or
		/// 'f', provided that 'c' is true, but requires 'f' if LA(0) is 'e'. 
		/// <para/>
		/// LLLPG appears to handle this case correctly.
		/// </remarks>
		public LNode Run(ISourceFile sourceFile)
		{
			var rules = _rulesInOrder.Where(r => !r.IsExternal);
			var rulesAndExterns = _rulesInOrder;
			
			// Generate variables for labeled Preds (e.g x:Foo y+:Bar Baz {$Baz;})
			foreach (var rule in rulesAndExterns)
				AutoValueSaverVisitor.Run(rule, _sink, _rules, _helper.TerminalType);

			// Expand rules or rule references marked inline, if any
			var inl = new ApplyInlines();
			foreach (var rule in rulesAndExterns)
				inl.Run(rule, _sink);

			// Add special recognizer rules for &(syntactic predicates)
			var pmr = new AddMiniRecognizers(this);
			foreach (var rule in rulesAndExterns.ToList())
				pmr.FindAndPreds(rule);

			// Figure out which rules need recognizer forms, starting from the ones that already do
			var prr = new AddRecognizersRecursively(this);
			foreach (var rule in rulesAndExterns.Where(r => r.HasRecognizerVersion))
				prr.Scan(rule);

			// Record follow sets of rules, and build a flow graph by setting 
			// the Next field of every Pred.
			DetermineFollowSets(rulesAndExterns);

			// Print some stats if grammar has [Verbosity(n)] option.
			if (Verbosity > 0)
				PrintVerboseStats(rules);

			// ***** PREDICTION ANALYSIS *****: everyone's favorite part
			var pav = new PredictionAnalysisVisitor(this);
			foreach (var rule in rules) {
				if (Verbosity > 0) Output(Verbose, null, 
					Localize.From("Doing prediction analysis for rule '{0}'", rule.Name));
				pav.Analyze(rule);
			}

			// Prematch optimization: replaces Match(...) with Skip() where possible
			var pmav = new PrematchAnalysisVisitor(this);
			foreach (var rule in rules)
				pmav.Analyze(rule);

			// Generate output code
			_sourceFile = sourceFile;
			var F = new LNodeFactory(_sourceFile);
			_classBody = new RWList<LNode>();
			_helper.Begin(_classBody, _sourceFile);

			var generator = new GenerateCodeVisitor(this);
			foreach (var rule in rules) {
				generator.Generate(rule);
				if (!rule.IsRecognizer && rule.HasRecognizerVersion)
					generator.Generate(rule.MakeRecognizerVersion());
			}
			
			_helper.Done();

			return F.Braces(_classBody.ToRVList());
		}

		private void PrintVerboseStats(IEnumerable<Rule> rules)
		{
			int tokens = 0, privates = 0;
			foreach (var rule in rules) {
				if (rule.IsPrivate)
					privates++;
				if (rule.IsToken)
					tokens++;
				else {
					Output(Verbose, rule.Pred, Localize.From("Follow set of '{0}': {1}",
						rule.Name, rule.EndOfRule.FollowSet.Select(p => p.ToStringWithPosition()).Join(", ")));
					if (Verbosity >= 2) {
						var end = new KthSet(rule.EndOfRule, ExitAlt, _helper.EmptySet);
						var followSet = ComputeNextSet(end, false);

						string casesStr;
						IList<Transition> cases = followSet.Cases;
						string message = "Follow set of '{0}': {1} from {2} cases: {4}";
						if (Verbosity <= 2) {
							message = "Follow set of '{0}': {1} from {2} cases ({3} abridged): {4}";
							var coverage = CodeGenHelper.EmptySet;
							cases = cases.Where(c => {
								bool subset = c.Set.IsSubsetOf(coverage);
								if (!subset) coverage = coverage.Union(c.Set);
								return !subset;
							}).ToList();
						}
						casesStr = string.Concat(cases.Select(c => "\n  " + c.ToString()));

						Output(Verbose, rule.Pred, Localize.From(message,
							rule.Name, followSet.Set, followSet.Cases.Count, cases.Count, casesStr));
					}
				}
			}
			Output(Verbose, null, Localize.From("{0} rule(s) are using Token mode. This mode assumes the follow set could be anything.", tokens));
			Output(Verbose, null, Localize.From("{0} rule(s) are private. Private rules should only be called from other rules.", privates));
		}

		#endregion

		protected IPGCodeGenHelper _helper = new IntStreamCodeGenHelper();
		public IPGCodeGenHelper CodeGenHelper
		{
			get { return _helper; }
			set { _helper = value ?? new IntStreamCodeGenHelper(); }
		}

		internal bool NeedsErrorBranch(PredictionTree tree, Alts alts)
		{
			bool hasError = alts.HasErrorBranch(this);
			bool needErrorBranch = hasError && (tree.IsAssertionLevel
				? tree.Children.Last.AndPreds.Count != 0
				: !tree.TotalCoverage.ContainsEverything);
			return needErrorBranch;
		}

		#region Prediction analysis: low-level helper code
		// Helper code and visitors for PredictionAnalysisVisitor.ComputePredictionTree()

		// The first sets are returned in priority order (in case of ambiguity, 
		// the branch with the lower index wins) 
		protected KthSet[] ComputeFirstSets(Alts alts)
		{
			bool hasExit = alts.Mode != LoopMode.None;
			var firstSets = new KthSet[alts.ArmCountPlusExit];

			int i;
			for (i = 0; i < alts.Arms.Count; i++)
				firstSets[i] = ComputeNextSet(new KthSet(alts.Arms[i], i, _helper.EmptySet), false);
			var exit = i;
			if (hasExit)
				firstSets[exit] = ComputeNextSet(new KthSet(alts.Next, ExitAlt, _helper.EmptySet, alts.Greedy == false), true);
			//if (alts.NonExitDefaultArmRequested()) {
			//	InternalList.Move(firstSets, alts.DefaultArm.Value, firstSets.Length - 1);
			//	exit--;
			//}
			if (!(alts.Greedy ?? true))
				InternalList.Move(firstSets, exit, 0);
			return firstSets;
		}
		protected KthSet[] ComputeNextSets(List<KthSet> previous, Alts currentAlts)
		{
			// [2013-12-24] Slug fix (e.g. LlpgGeneralTests.SlugTest()):
			// Detect duplicate positions between different arms. If two arms 
			// contain the same position, they are ambiguous, and further 
			// analysis potentially wastes exponential time so we must avoid it.
			EliminateDuplicatePositions(previous, currentAlts);

			var result = new KthSet[previous.Count];
			for (int i = 0; i < previous.Count; i++)
				result[i] = ComputeNextSet(previous[i], previous[i].Alt == ExitAlt);
			return result;
		}

		bool _printedRecursionError; // to avoid overwhelming user with messages

		protected KthSet ComputeNextSet(KthSet previous, bool addEOF)
		{
			var next = new KthSet(previous);
			for (int i = 0; i < previous.Cases.Count; i++) {
				Transition @case = previous.Cases[i];
				try {
					_computeNext.Do(next, @case.Position);
				} catch (StackOverflowException ex) {
					if (_printedRecursionError == false) {
						_printedRecursionError = true;
						Output(Error, @case.Position.Pred, ex.Message);
					}
				}
			}
			MakeCanonical(next);
			ConsolidateDuplicatePositions(next);
			next.UpdateSet(addEOF);
			return next;
		}

		protected void MakeCanonical(KthSet next)
		{
			var cases = next.Cases;
			for (int i = 0; i < cases.Count; i++)
				cases[i].Position = _getCanonical.Do(cases[i].Position);
		}

		protected ComputeNext _computeNext = new ComputeNext();
		protected GetCanonical _getCanonical = new GetCanonical();
		
		/// <summary>Computes the "canonical" interpretation of a position for
		/// prediction purposes, so that <see cref="ConsolidateDuplicatePositions"/> 
		/// can detect duplicates reliably. Call <see cref="Do"/>() to use.</summary>
		protected class GetCanonical : PredVisitor
		{
			/// <summary>Computes the "canonical" interpretation of a position.</summary>
			/// <remarks>
			/// For example, given
			/// <code>
			///		rule X @[ 'a' Y 'z' ];
			///		rule Y @[ 'a'..'y' 'b'..'z' ];
			/// </code>
			/// The position before the sequence <c>'a' Y 'z'</c> is equivalent to 
			/// the position before 'a', so the result points to 'a' rather than to
			/// the sequence itself.
			/// <para/>
			/// The position after 'b'..'z' is equivalent to the position before 'z',
			/// if Y was called from X. Therefore, given the position after 'b'..'z'
			/// (a pointer to <see cref="EndOfRule"/>), and return address before 'z',
			/// this method returns the position before 'z'.
			/// </remarks>
			public GrammarPos Do(GrammarPos input)
			{
				_result = input;
				Visit(input.Pred);
				return _result ?? input;
			}
			GrammarPos _result;

			public override void Visit(Seq seq)
			{
				if (seq.List.Count > 0)
					Visit(seq.List[0]);
				else
					Visit(seq.Next);
			}
			public override void Visit(Gate gate)
			{
				Visit(gate.Predictor);
			}
			public override void Visit(EndOfRule end)
			{
				if (_result.Return != null) {
					_result = _result.Return; // Return!
					Visit(_result.Pred);
				}
			}
			public override void VisitOther(Pred pred)
			{
				_result = new GrammarPos(pred, _result.Return, _result.InFollowSet);
			}
		}

		/// <summary>Gathers a list of all one-token transitions starting from a 
		/// single position. Also gathers any and-predicates that must be traversed
		/// before completing a transition.</summary>
		/// <remarks>
		/// For example, given
		/// <code>
		///		rule X @[ 'x' Y '0'..'9' 'x' ];
		///		rule Y @[.('y' | Z)? ];
		///		rule Z @[ ('z' | '0'..'9' '0'..'9'*) ];
		/// </code>
		/// If the dot (.) represents the current position, then this class 
		/// computes the possible <see cref="Transition"/>s, which are as follows:
		/// <code>
		///     Transition.Set   Transition.Position
		///     'y'              rule Y @[ ('y' | Z)?.];                 (EndOfRule)
		///     '0'..'9'         rule X @[ 'x' Y '0'..'9'.'x' ];         (TerminalPred)
		///     'z'              rule Z @[ ('z' | '0'..'9' '0'..'9'*).]; (EndOfRule)
		///     '0'..'9'         rule Z @[ ('z' | '0'..'9'.'0'..'9'*) ]; (Alts)
		/// </code>
		/// Notice that there can be duplicate sets--different destinations for the
		/// same input character. This means that there is an LL(1) ambiguity. The
		/// ambiguity may (or may not, depending on the situation) be resolved by 
		/// looking ahead further (it is the responsibility of 
		/// <see cref="PredictionAnalysisVisitor.ComputePredictionTree"/> to do so).
		/// <para/>
		/// This class is derived from GetCanonical just to inherit some code from it.
		/// <para/>
		/// What to do with and-predicates? It's a tricky question. And-predicates 
		/// are not used nearly as often as normal terminals and nonterminals, yet 
		/// they can produce the most complicated prediction code. Consider Alts
		/// such as:
		/// <code>
		/// ( ( &amp;{a} {f();} | &amp;{b} {g();} ) &amp;{c}
		///   ( &amp;{a} 'a' | &amp;{x} 'b' | &amp;{x} 'c')
		/// | &amp;{x} ( 'a' | &amp;{y} 'b' 'c' )
		/// )
		/// </code>
		/// It's enough to make your head explode. IIRC, PredictionAnalysisVisitor
		/// deals with such complications--all ComputeNext does is gather a list of 
		/// AndPreds.
		/// </remarks>
		protected class ComputeNext : PredVisitor
		{
			public void Do(KthSet result, GrammarPos position)
			{
				_stackDepth = 0;
				Debug.Assert(_followSetVisited.Count == 0);

				try {
					_result = result;
					_currentPos = position;
					_andPreds = RVList<AndPred>.Empty;
					Visit(position.Pred);
					Debug.Assert(_stackDepth == 0);
				} finally {
					_followSetVisited.Clear();
				}
			}
			KthSet _result;
			RVList<AndPred> _andPreds;
			GrammarPos _currentPos;
			HashSet<Pred> _followSetVisited = new HashSet<Pred>();
			int _stackDepth;

			public void Visit(Pred pred, GrammarPos newPos = null)
			{
				++_stackDepth;
				if (_stackDepth > 250) {
					// Detect infinite loops and escape by throwing. One known 
					// cause of an infinite loop is a nullable item inside a loop, 
					// e.g. ('a'? 'b'?)*, so note that infinite recursion in 
					// ComputeNext can occur without ever calling another rule.
					throw new StackOverflowException("The grammar is left-recursive, contains an infinite loop, or is too recursive.");
				}
				var oldCur = _currentPos;
				
				_currentPos = newPos ?? new GrammarPos(pred, _currentPos.Return, _currentPos.InFollowSet);
				pred.Call(this);
				
				_currentPos = oldCur;
				--_stackDepth;
			}

			public override void Visit(Seq seq)
			{
				if (seq.List.Count > 0)
					Visit(seq.List[0]);
				else
					Visit(seq.Next);
			}
			public override void Visit(Gate gate)
			{
				Visit(gate.Predictor);
			}

			public override void Visit(TerminalPred term)
			{
				_result.Cases.Add(new Transition(term, term.Set, _andPreds, 
					new GrammarPos(term.Next, _currentPos.Return, _currentPos.InFollowSet)));
			}
			public override void Visit(RuleRef rref)
			{
				var returnTo = new GrammarPos(rref.Next, _currentPos.Return, _currentPos.InFollowSet);
				Visit(rref.Rule.Pred, new GrammarPos(rref.Rule.Pred, returnTo, _currentPos.InFollowSet));
			}
			public override void Visit(Alts alts)
			{
				var saved = _andPreds;
				foreach (var pred in alts.Arms) {
					Visit(pred);
					_andPreds = saved;
				}
				if (alts.HasExit)
					Visit(alts.Next);
			}
			public override void Visit(AndPred and)
			{
				if (!(_currentPos.InsideOtherRule && and.Local))
					_andPreds.Add(and);
				Visit(and.Next); // skip
			}
			public override void Visit(EndOfRule end)
			{
				if (_currentPos.Return != null) {
					// "Return" to calling rule
					Visit(_currentPos.Return.Pred, _currentPos.Return);
				} else {
					// Nowhere to return to? Visit the follow set of the rule.
					foreach (var pred in end.FollowSet) {
						// Avoid visiting same follow set multiple times (e.g. right-recursive rule)
						if (_followSetVisited.Add(pred))
							Visit(pred, new GrammarPos(pred, null, true));
					}
				}
			}
		}

		/// <summary>Used by <see cref="ConsolidateDuplicatePositions"/>.</summary>
		class ConsolidationComparer : IEqualityComparer<Transition>
		{
			public static readonly ConsolidationComparer Value = new ConsolidationComparer();
			public bool Equals(Transition x, Transition y)
			{
				return x.Position.Equals(y.Position) &&
					x.AndPreds.SequenceEqual(y.AndPreds);
			}
			public int GetHashCode(Transition obj)
			{
				return obj.Position.GetHashCode();
			}
		}
		
		/// <summary>Different paths through a grammar can lead to the same place.
		/// This method is an optimization that merges duplicate cases. If we don't 
		/// do this, the number of cases can sometimes get very large, very quickly.</summary>
		static void ConsolidateDuplicatePositions(KthSet set)
		{
			if (set.Cases.Count <= 1)
				return;
			
			var unique = new MSet<Transition>(ConsolidationComparer.Value);
			for (int i = set.Cases.Count-1; i >= 0; i--) {
				Transition c = set.Cases[i], c0 = c;
				if (!unique.AddOrFind(ref c0, false)) {
					c0.Set = c.Set.Union(c0.Set);
					set.Cases.RemoveAt(i);
				}
			}
			Debug.Assert(unique.Count == set.Cases.Count);
		}

		private void EliminateDuplicatePositions(List<KthSet> arms, Alts currentAlts)
		{
			// MAYBE BUG HERE: two positions are not necessarily equivalent if different
			// branches have different knowledge about which and-predicates match (the 
			// related method ConsolidateDuplicatePositions takes this into account). 
			// However, in the interest of speed I'm ignoring this potential problem, 
			// for now.

			var positionMap = new MMap<GrammarPos, KthSet>();
			for (int i = 0; i < arms.Count; i++)
			{
				ulong bit = (1ul << i);
				var cases = arms[i].Cases;
				for (int c = 0; c < cases.Count; c++)
				{
					var pair = new KeyValuePair<GrammarPos,KthSet>(cases[c].Position, arms[i]);
					if (!positionMap.AddOrFind(ref pair, false) && pair.Value != arms[i]) {
						// Key was already present for a different arm--ambiguity detected!
						// Record this ambiguity by setting value to null.
						positionMap[pair.Key] = null;
					}
				}
			}

			// Deal with duplicate positions by eliminating lower-priority copies
			foreach (var pair in positionMap.Where(p => p.Value == null))
			{
				List<KthSet> applicableArms = arms.Where(arm => arm.Cases.Any(t => t.Position.Equals(pair.Key))).ToList();
				Debug.Assert(applicableArms.Count > 1);
				AmbiguityDetected(applicableArms, currentAlts);

				for (int i = 1; i < applicableArms.Count; i++)
					applicableArms[i].Cases.RemoveAll(t => t.Position.Equals(pair.Key));
			}
		}

		#endregion

		#region Ambiguity handling (not including detection)

		private KthSet AmbiguityDetected(IList<KthSet> prevSets, Alts currentAlts)
		{
			List<int> list = ShouldReportAmbiguity(prevSets, currentAlts);
			if (list != null)
			{
				IEnumerable<int> arms = prevSets.Select(ks => ks.Alt);

				string format = "Alternatives {{{0}}} are ambiguous for input such as {1}";
				if (currentAlts.Mode == LoopMode.Opt && currentAlts.Arms.Count == 1)
					format = "Optional branch is ambiguous for input such as {1}";
				Output(Warning, currentAlts,
					string.Format(format,
						StringExt.Join(", ", prevSets.Select(
							ks => currentAlts.AltName(ks.Alt))),
						GetAmbiguousCase(prevSets)));
			}
			// Return the KthSet representing the branch to use by default.
			// The nongreedy exit branch takes priority; if there isn't one,
			// the lexically first applicable Alt takes priority (bug fix: 
			// prevSets[0] may not be lexically first if the user specified
			// a "default" arm.)
			Debug.Assert(!prevSets.Slice(1).Any(s => s.IsNongreedyExit));
			if (prevSets[0].IsNongreedyExit)
				return prevSets[0];
			return prevSets[prevSets.IndexOfMin(s => (uint)s.Alt)];
		}

		private List<int> ShouldReportAmbiguity(IList<KthSet> prevSets, Alts currentAlts)
		{
			// Look for any and-predicates that are unique to particular 
			// branches. Such predicates can suppress warnings.
			var andPreds = new List<Set<AndPred>>();
			var common = new Set<AndPred>();
			bool first = true;
			foreach (var ks in prevSets)
			{
				var andSet = new Set<AndPred>();
				for (var ks2 = ks; ks2 != null; ks2 = ks2.Prev)
					andSet = andSet | ks2.AndReq;
				andPreds.Add(andSet);
				common = first ? andSet : andSet & common;
				first = false;
			}
			ulong suppressWarnings = 0;
			for (int i = 0; i < andPreds.Count; i++)
			{
				if (!(andPreds[i] - common).IsEmpty && prevSets[i].Alt != ExitAlt)
					suppressWarnings |= 1ul << prevSets[i].Alt;
			}

			// Suppress ambiguity with exit if the ambiguity is caused by 
			// reaching the end of a rule that is marked as a "token", or by 
			// reaching the end of prediction in a gate.
			bool suppressExitWarning = false;
			{
				var ks = prevSets.Where(ks0 => ks0.Alt == ExitAlt).SingleOrDefault();
				if (ks != null && ks.Cases.All(transition => transition.Position.Pred == EndOfToken && transition.PrevPosition == EndOfToken))
					suppressExitWarning = true;
			}

			var list = prevSets.Select(ks => ks.Alt).ToList();
			if (currentAlts.ShouldReportAmbiguity(list, suppressWarnings, suppressExitWarning))
				return list;
			else
				return null;
		}

		/// <summary>Gets an example of an ambiguous input, based on a list of 
		/// two or more ambiguous paths through the grammar.</summary>
		private string GetAmbiguousCase(IList<KthSet> lastSets)
		{
			var seq = new List<IPGTerminalSet>();
			IEnumerable<KthSet> kthSets = lastSets;
			for (;;)
			{
				IPGTerminalSet tokSet = null;
				foreach (KthSet ks in kthSets)
					tokSet = tokSet == null ? ks.Set : tokSet.Intersection(ks.Set);
				if (tokSet == null || tokSet.IsEmptySet)
					break;
				seq.Add(tokSet);
				Debug.Assert(!kthSets.Any(ks => ks.Prev == null));
				kthSets = kthSets.Select(ks => ks.Prev);
			}
			seq.Reverse();

			var result = new StringBuilder("«");
			if (seq.All(set => _helper.ExampleChar(set) != null)) {
				StringBuilder temp = new StringBuilder();
				foreach (var set in seq)
					temp.Append(_helper.ExampleChar(set));
				result.Append(G.EscapeCStyle(temp.ToString(), EscapeC.Control, '»'));
			} else {
				result.Append(seq.Select(set => _helper.Example(set)).Join(" "));
			}
			result.Append("» (");
			result.Append(seq.Join(", "));
			result.Append(')');
			return result.ToString();
		}

		#endregion
	}
}
