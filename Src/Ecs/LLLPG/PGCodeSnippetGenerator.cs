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

	/// <summary>
	/// A class that implements this interface will generate small bits of code 
	/// that the parser generator will use. The default implementation is
	/// <see cref="PGCodeSnippetGenerator"/>. To install a new code generator,
	/// set the <see cref="LLParserGenerator.SnippetGenerator"/> property.
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

		Symbol GenerateSetName(Rule currentRule);

		Symbol GenerateSetDecl(IPGTerminalSet set);

		/// <summary>Generate code to match any token.</summary>
		/// <returns>Default implementation returns <c>@{ Match(); }</c>.</returns>
		Node GenerateConsume(); // match anything

		/// <summary>Generate code to check an and-predicate during or after prediction, 
		/// e.g. &!{foo} becomes !(foo) during prediction and Check(!(foo)); afterward.</summary>
		/// <param name="classBody">If the check requires a separate method, it will be created here.</param>
		/// <param name="currentRule">Rule in which the andPred is located</param>
		/// <param name="andPred">Predicate for which to generate code</param>
		/// <param name="predict">true to generate prediction code, false for checking post-prediction</param>
		Node GenerateAndPredCheck(AndPred andPred, bool predict);

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
		Node ErrorBranch(Rule currentRule, IPGTerminalSet covered);

		/*/// <summary>Gets a number that represents the relative cost of testing
		/// membership in this set using switch cases compared to expressions in
		/// an "if" statement.</summary>
		/// <remarks>In case of a tie score, if-else wins, as the code is shorter.</remarks>
		int SwitchCost { get; }
		/// <summary>Gets a number that represents the relative cost of testing
		/// membership in this set using an expression compared to cases in a
		/// "switch" statement.</summary>
		int IfExprCost { get; }

		/// <summary>Generates an empty switch body, e.g. <c>switch(\subject) { }</c>.</summary>
		/// <remarks>If a static dictionary is needed to assist with the switch,
		/// this method also creates code to build the dictionary, e.g.
		/// <code>
		/// static Dictionary&lt;Symbol, int> \dictName = \(GSymbol.Get(dictName.Name+"_"))();
		/// static Dictionary&lt;Symbol, int> \(GSymbol.Get(dictName.Name+"_"))()
		/// {
		///    var tbl = new Dictionary&lt;Symbol, int>();
		///    tbl.Add(GSymbol.Get("foo"), 1);
		///    tbl.Add(GSymbol.Get("bar"), 1);
		///    tbl.Add(GSymbol.Get("baz"), 2);
		/// }
		/// </code>
		/// </remarks>
		Node GenerateSwitchBody(Node subject, Symbol dictName, out Node dictDecl);*/
	}

	/// <summary>Default code generator for <see cref="LLParserGenerator"/> and
	/// suggested base class for custom code generators.</summary>
	class PGCodeSnippetGenerator : IPGCodeSnippetGenerator
	{
		public const int EOF = PGIntSet.EOF;
		protected static readonly Symbol _Consume = GSymbol.Get("Consume");
		protected static readonly Symbol _Match = GSymbol.Get("Match");
		protected static readonly Symbol _MatchExcept = GSymbol.Get("MatchExcept");
		protected static readonly Symbol _MatchRange = GSymbol.Get("MatchRange");
		protected static readonly Symbol _MatchExceptRange = GSymbol.Get("MatchExceptRange");

		protected int _setNameCounter = 0;
		protected GreenFactory F;
		protected Node _classBody;
		protected Rule _currentRule;
		Dictionary<IPGTerminalSet, Symbol> _setDeclNames;

		public void Begin(Node classBody, ISourceFile sourceFile)
		{
			_classBody = classBody;
			F = new GreenFactory(sourceFile);
			_setDeclNames = new Dictionary<IPGTerminalSet, Symbol>();
		}
		public void BeginRule(Rule rule)
		{
			_currentRule = rule;
			_setNameCounter = 0;
		}
		public void Done()
		{
			_classBody = null;
			F = null;
			_setDeclNames = null;
			_currentRule = null;
		}

		public virtual Symbol GenerateSetName(Rule currentRule)
		{
			return GSymbol.Get(string.Format("{0}_set{1}", currentRule.Name.Name, _setNameCounter++));
		}

		public virtual Symbol GenerateSetDecl(IPGTerminalSet set)
		{
			Symbol setName;
			if (_setDeclNames.TryGetValue(set, out setName))
				return setName;

			setName = GenerateSetName(_currentRule);
			_classBody.Args.Add(set.GenerateSetDecl(setName));

			return _setDeclNames[set] = setName;
		}


		/// <summary>Returns <c>@{ Consume(); }</summary>
		public virtual Node GenerateConsume() // match anything
		{
			return Node.FromGreen(F.Call(_Consume));
		}

		/// <summary>Generate code to check an and-predicate during or after prediction, 
		/// e.g. &!{foo} becomes !(foo) during prediction and Check(!(foo)); afterward.</summary>
		/// <param name="classBody">If the check requires a separate method, it will be created here.</param>
		/// <param name="currentRule">Rule in which the andPred is located</param>
		/// <param name="andPred">Predicate for which to generate code</param>
		/// <param name="predict">true to generate prediction code, false for checking post-prediction</param>
		public virtual Node GenerateAndPredCheck(AndPred andPred, bool predict)
		{
			var predTest = andPred.Pred as Node;
			if (predTest != null)
				predTest = predTest.Clone(); // in case it's used more than once
			else
				predTest = Node.FromGreen(F.Literal("TODO"));
			if (andPred.Not)
				predTest = Node.FromGreen(F.Call(S.Not, predTest.FrozenGreen));
			if (predict)
				return predTest;
			else
				return Node.FromGreen(F.Call(GSymbol.Get("Check"), predTest.FrozenGreen));
		}

		/// <summary>Generate code to match a set, e.g. 
		/// <c>@{ MatchRange('a', 'z');</c> or <c>@{ MatchExcept('\n', '\r'); }</c>.
		/// If the set is too complex, a declaration for it is created in classBody.</summary>
		public virtual Node GenerateMatch(IPGTerminalSet set_)
		{
			var set = set_ as PGIntSet;
			if (set != null) {
				if (set.Complexity(2, 3, !set.Inverted) <= 6) {
					Node call;
					Symbol matchMethod = set.Inverted ? _MatchExcept : _Match;
					if (set.Complexity(1, 2, true) > set.Count) {
						Debug.Assert(!set.IsSymbolSet);
						matchMethod = set.Inverted ? _MatchExceptRange : _MatchRange;
						call = Node.FromGreen(F.Call(matchMethod));
						for (int i = 0; i < set.Count; i++) {
							if (!set.Inverted || set[i].Lo != EOF || set[i].Hi != EOF) {
								call.Args.Add(Node.FromGreen(set.MakeLiteral(set[i].Lo)));
								call.Args.Add(Node.FromGreen(set.MakeLiteral(set[i].Hi)));
							}
						}
					} else {
						call = Node.FromGreen(F.Call(matchMethod));
						for (int i = 0; i < set.Count; i++) {
							var r = set[i];
							for (int c = r.Lo; c <= r.Hi; c++) {
								if (!set.Inverted || c != EOF)
									call.Args.Add(Node.FromGreen(set.MakeLiteral(c)));
							}
						}
					}
					return call;
				}
			}

			var tset = set_ as TrivialTerminalSet;
			if (tset != null)
				return GenerateMatch(new PGIntSet(false, tset.Inverted) 
				                     { ContainsEOF = tset.ContainsEOF });

			var setName = GenerateSetDecl(set_);
			return Node.FromGreen(F.Call(_Match, F.Symbol(setName)));
		}

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
		public virtual Node ErrorBranch(Rule currentRule, IPGTerminalSet covered)
		{
			return Node.FromGreen(F.Literal("TODO: Report error to user"));
		}

		/// <summary>Returns the data type of LA(k)</summary>
		/// <returns>Default implementation returns @(int).</returns>
		public virtual GreenNode LAType()
		{
			return F.Int32;
		}


	}
}
