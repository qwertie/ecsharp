using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.CompilerCore;
using Loyc;

namespace Loyc.LLParserGenerator
{
	using S = ecs.CodeSymbols;
	using System.Diagnostics;
	using Loyc.Math;

	/// <summary>
	/// A class that implements this interface will generate small bits of code 
	/// that the parser generator will use. The default implementation is
	/// <see cref="PGCodeSnippetGenerator"/>. To install a new code generator,
	/// set the <see cref="LLParserGenerator.SnippetGenerator"/> property or
	/// supply the generator in LLParserGenerator's constructor.
	/// </summary>
	public interface IPGCodeSnippetGenerator
	{
		/// <summary>Before the parser generator generates code, it calls this
		/// method.</summary>
		/// <param name="classBody">the body (braced block) of the class where 
		/// the code will be generated, which allows the snippet generator to add 
		/// code at class level when needed.</param>
		/// <param name="sourceFile">the suggested <see cref="ISourceFile"/> to 
		/// assign to generated code snippets.</param>
		void Begin(Node classBody, ISourceFile sourceFile);

		/// <summary>Notifies the snippet generator that code generation is 
		/// starting for a new rule.</summary>
		void BeginRule(Rule rule);
		
		/// <summary><see cref="LLParserGenerator"/> calls this method to notify
		/// the snippet generator that code generation is complete.</summary>
		void Done();

		/// <summary>Generate code to match any token.</summary>
		/// <returns>Default implementation returns <c>@{ Match(); }</c>.</returns>
		Node GenerateConsume(); // match anything

		/// <summary>Generate code to check an and-predicate during or 
		/// after prediction, e.g. &!{foo} typically becomes !(foo) during 
		/// prediction and Check(!(foo)); afterward.</summary>
		/// <param name="andPred">Predicate for which to generate code</param>
		/// <param name="code">The code of the predicate, which is either <c>(andPred.Pred as Node)</c>
		/// or some other expression generated based on <c>andPred.Pred</c>.</param>
		/// <param name="predict">true to generate prediction code, false for checking post-prediction</param>
		Node GenerateAndPredCheck(AndPred andPred, Node code, bool predict);

		/// <summary>Generate code to match a set, e.g. 
		/// <c>@{ MatchRange('a', 'z');</c> or <c>@{ MatchExcept('\n', '\r'); }</c>.
		/// If the set is too complex, a declaration for it is created in classBody.</summary>
		Node GenerateMatch(IPGTerminalSet set_);

		/// <summary>Generates code to read LA(k).</summary>
		/// <returns>The default implementation returns @(LA(k)).</returns>
		GreenNode LA(int k);

		/// <summary>Returns the data type of LA(k)</summary>
		/// <returns>The default implementation returns @(int).</returns>
		GreenNode LAType();

		/// <summary>Generates code for the error branch of prediction.</summary>
		/// <param name="currentRule">Rule in which the code is generated.</param>
		/// <param name="covered">The permitted token set, which the input did not match. 
		/// NOTE: if the input matched but there were and-predicates that did not match,
		/// this parameter will be null (e.g. the input is 'b' in <c>(&{x} 'a' | &{y} 'b')</c>,
		/// but y is false.</param>
		Node ErrorBranch(IPGTerminalSet covered);

		/// <summary>Returns true if a "switch" statement is the preferable code 
		/// generation technique rather than the default if-else chain</summary>
		/// <param name="casesToInclude">This method should add the indexes of
		/// branches for which cases should be generated to this HashSet, e.g.
		/// adding index 2 means that switch cases should be generated for sets[2].
		/// The caller (<see cref="LLParserGenerator"/>) will create an if-else 
		/// chain for all branches that are not added to casesToInclude, and this 
		/// chain will be passed to <see cref="GenerateSwitch"/>.</param>
		bool ShouldGenerateSwitch(IPGTerminalSet[] sets, bool needErrorBranch, HashSet<int> casesToInclude);

		/// <summary>Generates a switch statement with the specified branches where
		/// branchCode[i] is the code to run if the input is in the set branchSets[i].</summary>
		/// <param name="errorBranch">The error code that should be placed in the 
		/// switch's default case. If an error branch is not required, the switch's
		/// default case should be the one that eliminates the largest number of 
		/// case labels.</param>
		/// <param name="laVar">The lookahead variable being switched on (e.g. la0)</param>
		/// <returns>The generated switch block.</returns>
		Node GenerateSwitch(IPGTerminalSet[] branchSets, Node[] branchCode, HashSet<int> casesToInclude, Node defaultBranch, GreenNode laVar);

		/// <summary>Generates code to test whether the terminal denoted 'laVar' is in the set.</summary>
		Node GenerateTest(IPGTerminalSet set, GreenNode laVar);

		IPGTerminalSet EmptySet { get; }
	}

	/// <summary>Suggested base class for custom code generators. Each derived 
	/// class is typically designed for a different kind of token.
	/// <remarks>
	/// LLPG comes with two derived classes, <see cref="PGCodeGenForIntStream"/> 
	/// for parsing input streams of characters or integers, and 
	/// <see cref="PGCodeGenForSymbolStream"/> for parsing streams of 
	/// <see cref="Symbol"/>s.
	/// </remarks>
	public abstract class PGCodeSnippetGeneratorBase : IPGCodeSnippetGenerator
	{
		protected static readonly Symbol _Consume = GSymbol.Get("Consume");
		protected static readonly Symbol _Match = GSymbol.Get("Match");
		protected static readonly Symbol _MatchExcept = GSymbol.Get("MatchExcept");
		protected static readonly Symbol _MatchRange = GSymbol.Get("MatchRange");
		protected static readonly Symbol _MatchExceptRange = GSymbol.Get("MatchExceptRange");
		protected static readonly Symbol _Check = GSymbol.Get("Check");

		protected int _setNameCounter = 0;
		protected GreenFactory F;
		protected NodeFactory NF;
		protected Node _classBody;
		protected Rule _currentRule;
		Dictionary<IPGTerminalSet, Symbol> _setDeclNames;

		public virtual void Begin(Node classBody, ISourceFile sourceFile)
		{
			_classBody = classBody;
			F = new GreenFactory(sourceFile);
			NF = new NodeFactory(sourceFile);
			_setDeclNames = new Dictionary<IPGTerminalSet, Symbol>();
		}
		public virtual void BeginRule(Rule rule)
		{
			_currentRule = rule;
			_setNameCounter = 0;
		}
		public virtual void Done()
		{
			_classBody = null;
			F = null;
			NF = null;
			_setDeclNames = null;
			_currentRule = null;
		}

		public virtual Node GenerateTest(IPGTerminalSet set, GreenNode laVar)
		{
			var laVar_ = Node.FromGreen(laVar);
			Node test = GenerateTest(set, laVar_, null);
			if (test == null) {
				var setName = GenerateSetDecl(set);
				test = GenerateTest(set, laVar_, setName);
			}
			return test;
		}

		/// <summary>Generates code to test whether a terminal is in the set.</summary>
		/// <param name="subject">Represents the variable to be tested.</param>
		/// <param name="setName">Names an external set variable to use for the test.</param>
		/// <returns>A test expression such as @(la0 >= '0' && '9' >= la0), or 
		/// null if an external setName is needed and was not provided.</returns>
		/// <remarks>
		/// At first, <see cref="LLParserGenerator"/> calls this method with 
		/// <c>setName == null</c>. If it returns null, it calls the method a
		/// second time, giving the name of an external variable in which the
		/// set is held (see <see cref="GenerateSetDecl"/>).
		/// <para/>
		/// For example, if the subject is @(la0), the test for a simple set
		/// like [a-z?] might be something like <c>@((la0 >= 'a' && 'z' >= la0)
		/// || la0 == '?')</c>. When the setName is @(foo), the test might be 
		/// <c>@(foo.Contains(la0))</c> instead.
		/// </remarks>
		protected abstract Node GenerateTest(IPGTerminalSet set, Node subject, Symbol setName);


		protected virtual Symbol GenerateSetName(Rule currentRule)
		{
			return GSymbol.Get(string.Format("{0}_set{1}", currentRule.Name.Name, _setNameCounter++));
		}

		protected virtual Symbol GenerateSetDecl(IPGTerminalSet set)
		{
			Symbol setName;
			if (_setDeclNames.TryGetValue(set, out setName))
				return setName;

			setName = GenerateSetName(_currentRule);
			_classBody.Args.Add(GenerateSetDecl(set, setName));

			return _setDeclNames[set] = setName;
		}

		/// <summary>Generates a declaration for a variable that holds the set.</summary>
		/// <remarks>
		/// For example, if setName is foo, a set such as [aeiouy] 
		/// might use an external declaration such as 
		/// <code>IntSet foo = IntSet.Parse("[aeiouy]");</code>
		/// This method will not be called if <see cref="GenerateTest(Node)"/>
		/// never returns null.
		/// </remarks>
		protected abstract Node GenerateSetDecl(IPGTerminalSet set, Symbol setName);

		/// <summary>Returns <c>@{ Consume(); }</summary>
		public virtual Node GenerateConsume() // match anything
		{
			return NF.Call(_Consume);
		}

		/// <summary>Generate code to check an and-predicate during or after prediction, 
		/// e.g. &!{foo} becomes !(foo) during prediction and Check(!(foo)); afterward.</summary>
		/// <param name="classBody">If the check requires a separate method, it will be created here.</param>
		/// <param name="currentRule">Rule in which the andPred is located</param>
		/// <param name="andPred">Predicate for which to generate code</param>
		/// <param name="predict">true to generate prediction code, false for checking post-prediction</param>
		public virtual Node GenerateAndPredCheck(AndPred andPred, Node code, bool predict)
		{
			code = code.Clone(); // in case it's used more than once
			if (andPred.Not)
				code = NF.Call(S.Not, code);
			if (predict)
				return code;
			else
				return NF.Call(_Check, code);
		}

		/// <summary>Generate code to match a set, e.g. 
		/// <c>@{ MatchRange('a', 'z');</c> or <c>@{ MatchExcept('\n', '\r'); }</c>.
		/// If the set is too complex, a declaration for it is created in classBody.</summary>
		public abstract Node GenerateMatch(IPGTerminalSet set_);

		/// <summary>Generates code to read LA(k).</summary>
		/// <returns>Default implementation returns @(LA(k)).</returns>
		public virtual GreenNode LA(int k)
		{
			return F.Call(GSymbol.Get("LA"), F.Literal(k));
		}

		/// <summary>Generates code for the error branch of prediction.</summary>
		/// <param name="currentRule">Rule in which the code is generated.</param>
		/// <param name="covered">The permitted token set, which the input did not match. 
		/// NOTE: if the input matched but there were and-predicates that did not match,
		/// this parameter will be null (e.g. the input is 'b' in <c>(&{x} 'a' | &{y} 'b')</c>,
		/// but y is false.</param>
		public virtual Node ErrorBranch(IPGTerminalSet covered)
		{
			return NF.Literal("TODO: Report error to user");
		}

		/// <summary>Returns the data type of LA(k)</summary>
		/// <returns>Default implementation returns @(int).</returns>
		public abstract GreenNode LAType();

		public abstract bool ShouldGenerateSwitch(IPGTerminalSet[] sets, bool needErrorBranch, HashSet<int> casesToInclude);
		public abstract Node GenerateSwitch(IPGTerminalSet[] branchSets, Node[] branchCode, HashSet<int> casesToInclude, Node defaultBranch, GreenNode laVar);

		public abstract IPGTerminalSet EmptySet { get; }
	}

	/// <summary>Standard code generator for character/integer input streams
	/// and is the default code generator for <see cref="LLParserGenerator"/>.</summary>
	class PGCodeGenForIntStream : PGCodeSnippetGeneratorBase
	{
		public const int EOF_int = PGIntSet.EOF_int;

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
			var set = set_ as PGIntSet;
			if (set != null) {
				if (set.Complexity(2, 3, !set.IsInverted) <= 6) {
					Node call;
					if (set.Complexity(1, 2, true) > set.Count) {
						// Use MatchRange or MatchExceptRange
						call = NF.Call(set.IsInverted ? _MatchExceptRange : _MatchRange);
						for (int i = 0; i < set.Count; i++) {
							if (!set.IsInverted || set[i].Lo != EOF_int || set[i].Hi != EOF_int) {
								call.Args.Add((Node)set.MakeLiteral(set[i].Lo));
								call.Args.Add((Node)set.MakeLiteral(set[i].Hi));
							}
						}
					} else {
						// Use Match or MatchExcept
						call = NF.Call(set.IsInverted ? _MatchExcept : _Match);
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
			}

			var setName = GenerateSetDecl(set_);
			return NF.Call(_Match, NF.Symbol(setName));
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

			Node braces = NF.Braces(), @switch = NF.Call(S.Switch, (Node)laVar, braces);
			var stmts = braces.Args;
			for (int i = 0; i < branchSets.Length; i++) {
				if (!casesToInclude.Contains(i))
					continue;

				// Generate all the needed cases
				var intset = (PGIntSet)branchSets[i];
				foreach (IntRange range in intset) {
					for (int ch = range.Lo; ch <= range.Hi; ch++) {
						bool isChar = intset.IsCharSet && (char)ch == ch;
						stmts.Add(NF.Call(S.Case, NF.Literal(isChar ? (object)(char)ch : (object)ch)));
						if (stmts.Count > 65535) // sanity check
							throw new InvalidOperationException("switch is too large to generate");
					}
				}

				AddSwitchHandler(branchCode[i], stmts);
			}
			if (!defaultBranch.IsSimpleSymbol(S.Missing)) {
				stmts.Add(NF.Call(S.Label, NF.Symbol(S.Default)));
				AddSwitchHandler(defaultBranch, stmts);
			}

			return @switch;
		}
		private void AddSwitchHandler(Node branch, ArgList stmts)
		{
			stmts.AddSpliceClone(branch);
			if (!branch.Calls(S.Goto, 1))
				stmts.Add(NF.Call(S.Break));
		}

		public override IPGTerminalSet EmptySet
		{
			get { return PGIntSet.Empty; }
		}
	}

	// Refactoring plan:
	//  DONE 1. Support switch() for chars and ints, not symbols
	//  DONE 2. Change unit tests to use switch() where needed
	//  DONE 3. Change IPGTerminalSet to be fully immutable
	// 1test 4. Write unit tests for Symbol stream parsing
	//       5. Write PGSymbolSet
	//       6. Eliminate Symbol support from PGIntSet
	//       7. Write PGCodeGenForSymbolStream
	//       8. Replace unnecessary Match() calls with Consume(); eliminate unnecessary Check()s

	/// <summary>Standard code generator for streams of <see cref="Symbol"/>s.</summary>
	class PGCodeGenForSymbolStream : PGCodeSnippetGeneratorBase
	{
		protected static readonly Symbol _Symbol = GSymbol.Get("Symbol");

		protected override Node GenerateTest(IPGTerminalSet set, Node subject, Symbol setName)
		{
			return ((PGSymbolSet)set).GenerateTest(subject, setName);
		}
		protected override Node GenerateSetDecl(IPGTerminalSet set, Symbol setName)
		{
			return ((PGSymbolSet)set).GenerateSetDecl(setName);
		}

		public override Node GenerateMatch(IPGTerminalSet set_)
		{
			throw new NotImplementedException();
		}
		public override GreenNode LAType()
		{
			return F.Symbol(_Symbol);
		}
		public override bool ShouldGenerateSwitch(IPGTerminalSet[] sets, bool needErrorBranch, HashSet<int> casesToInclude)
		{
			throw new NotImplementedException();
		}
		public override Node GenerateSwitch(IPGTerminalSet[] branchSets, Node[] branchCode, HashSet<int> casesToInclude, Node defaultBranch, GreenNode laVar)
		{
			throw new NotImplementedException();
		}

		public override IPGTerminalSet EmptySet
		{
			get { return PGSymbolSet.Empty; }
		}
	}
}
