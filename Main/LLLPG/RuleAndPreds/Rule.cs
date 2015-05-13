using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Syntax;
using Loyc.Collections;
using Loyc.Math;
using System.Diagnostics;

namespace Loyc.LLParserGenerator
{
	using S = CodeSymbols;

	/// <summary>Represents an LLLPG rule, which is a <see cref="Pred"/>icate plus
	/// a <see cref="Name"/> and optional attributes (e.g. token, private, etc.).</summary>
	public class Rule
	{
		/// <summary>A node that contains the original code of the rule, or, if the
		/// rule was created programmatically, the method prototype (e.g. 
		/// <c>#fn(int, Rule, #(#var(int, arg)))</c>, which means 
		/// <c>int Rule(int arg)</c>). This can be null, in which case the
		/// default prototype is <c>void Rule();</c>, or if the rule is a 
		/// starting rule or token, <c>public void Rule();</c>.</summary>
		/// <remarks>The Basis is also used to provide an error location.</remarks>
		public LNode Basis;
		public LNode ReturnType; // extracted from Basis, used by AutoValueSaverVisitor
		public readonly EndOfRule EndOfRule;
		
		public Rule(LNode basis, Symbol name, Pred pred, bool isStartingRule = true)
		{
			Basis = basis; Pred = pred; Name = name;
			IsStartingRule = isStartingRule;
			EndOfRule = new EndOfRule(this);
			if (basis != null && basis.Calls(S.Fn) && basis.ArgCount >= 3)
				ReturnType = basis.Args[0];
		}
		public Symbol Name;

		public Pred Pred;
		public bool IsToken, IsStartingRule;
		public bool IsPrivate, IsExternal;
		public bool? FullLLk;
		public int K; // max lookahead; <= 0 to use default

		#region Recognizer-related crap

		public bool IsRecognizer;
		public LNode TryWrapperName; // e.g. Try_Scan_Foo. only used when IsRecognizer==true
		public Rule _recognizer;
		
		public bool HasRecognizerVersion { get { return _recognizer != null || IsRecognizer; } }
		public Rule MakeRecognizerVersion()
		{
			var scanName = GSymbol.Get("Scan_" + Name.Name);
			return _recognizer = _recognizer ?? MakeRecognizerVersion(scanName);
		}
		public Rule MakeRecognizerVersion(Symbol newName)
		{
			return MakeRecognizerVersion(Basis, newName);
		}
		public Rule MakeRecognizerVersion(LNode prototype)
		{
			return MakeRecognizerVersion(prototype, prototype.Args[1].Name);
		}
		Rule MakeRecognizerVersion(LNode prototype, Symbol newName)
		{
			if (IsRecognizer)
				return this;
			else {
				_recognizer = (Rule)MemberwiseClone();
				_recognizer.IsRecognizer = true;
				_recognizer.Basis = prototype;
				_recognizer.Name = newName;
				return _recognizer;
			}
		}
		public void TryWrapperNeeded()
		{
			Debug.Assert(IsRecognizer);
			if (TryWrapperName == null)
				TryWrapperName = LNode.Id(GSymbol.Get("Try_" + Name.Name));
		}

		/// <summary>See <see cref="IPGCodeGenHelper.CreateTryWrapperForRecognizer"/> for more information.</summary>
		public LNode CreateTryWrapperForRecognizer()
		{
			Debug.Assert(TryWrapperName != null);

			LNode method = GetMethodSignature();
			LNode retType = method.Args[0], name = method.Args[1], args = method.Args[2];
			RVList<LNode> forwardedArgs = ForwardedArgList(args);
			
			LNode lookahead = F.Id("lookaheadAmt");
			Debug.Assert(args.Calls(S.List));
			args = args.WithArgs(args.Args.Insert(0, F.Var(F.Int32, lookahead)));

			LNode body = F.Braces(
				F.Call(S.UsingStmt, F.Call(S.New, F.Call(SavePosition, F.@this, lookahead)), 
					F.Call(S.Return, F.Call(name, forwardedArgs)))
			);
			return method.WithArgs(retType, TryWrapperName, args, body);
		}

		static RVList<LNode> ForwardedArgList(LNode args)
		{
			// translates an argument list like (int x, string y) to { x, y }
			return args.Args.SmartSelect(arg => VarName(arg) ?? arg);
		}
		static LNode VarName(LNode varStmt)
		{
			if (varStmt.Calls(S.Var, 2)) {
				var nameAndInit = varStmt.Args[1];
				if (nameAndInit.Calls(S.Assign, 2))
					return nameAndInit.Args[0];
				else
					return nameAndInit;
			}
			return null;
		}

		#endregion

		public override string ToString() { return "Rule " + Name.Name; } // for debugging

		// Types of rules...
		// "[#token]" - any follow set
		// "[#start]" - follow set is EOF plus information from calling rules;
		//    useful for top-level parser rules
		// "[#private]" - designed to be called by other rules in the same grammar;
		//    allows Consume() at the beginning of the rule based on predictions 
		//    in other rules. TODO: eliminate rule if it is not called.
		// "[#extern]" - Blocks codegen. Use for rules inherited from a base class,
		//    implemented manually, or for rules that don't exist and are used only 
		//    for prediction in gates.
		// TODO: "[#inline]" - immediate rule contents are inlined into callers.
		//                   - alternately this could be a command at the call site.

		static readonly Symbol SavePosition = GSymbol.Get("SavePosition");
		static LNodeFactory F = new LNodeFactory(new EmptySourceFile("Rule.cs"));

		/// <summary>Returns Basis if it's a method signature; otherwise constructs a default signature.</summary>
		public LNode GetMethodSignature()
		{
			if (Basis != null && Basis.Calls(S.Fn) && Basis.ArgCount.IsInRange(3, 4)) {
				var parts = Basis.Args;
				if (parts.Count == 4)
					parts.RemoveAt(3);
				if (IsRecognizer)
					parts[0] = F.Bool;
				parts[1] = F.Id(Name);
				return Basis.WithArgs(parts);
			} else {
				var method = F.Fn(IsRecognizer ? F.Bool : F.Void, F.Id(Name), F.List());
				if (IsPrivate)
					method = F.Attr(F.Id(S.Private), method);
				else if (IsStartingRule | IsToken)
					method = F.Attr(F.Id(S.Public), method);
				return method;
			}
		}

		/// <summary>Creates the default method definition to wrap around the body 
		/// of the rule, which has already been generated. Returns <see cref="Basis"/> 
		/// with the specified new method body. If Basis is null, a simple default 
		/// method signature is used, e.g. <c>public void R() {...}</c> where R is 
		/// the rule name.</summary>
		/// <param name="methodBody">The parsing code that was generated for this rule.</param>
		/// <returns>A method.</returns>
		public LNode CreateMethod(RVList<LNode> methodBody)
		{
			LNode method = GetMethodSignature();
			var parts = method.Args.ToRWList();
			if (parts[0].IsIdNamed(S.Missing))
				parts[0] = F.Id(Name);
			Debug.Assert(parts.Count == 3);
			if (IsRecognizer)
				methodBody.Add(F.Call(S.Return, F.True));
			parts.Add(F.Braces(methodBody));
			return method.WithArgs(parts.ToRVList());
		}

		public static Alts operator |(Rule a, Pred b) { return (Alts)((RuleRef)a | b); }
		public static Alts operator |(Pred a, Rule b) { return (Alts)(a | (RuleRef)b); }
		public static Alts operator |(Rule a, Rule b) { return (Alts)((RuleRef)a | (RuleRef)b); }
		public static Alts operator /(Rule a, Pred b) { return (Alts)((RuleRef)a / b); }
		public static Alts operator /(Pred a, Rule b) { return (Alts)(a / (RuleRef)b); }
		public static Alts operator /(Rule a, Rule b) { return (Alts)((RuleRef)a / (RuleRef)b); }
		public static Pred operator +(Rule a, char b) { return (RuleRef)a + b; }
		public static Pred operator +(char a, Rule b) { return a + (RuleRef)b; }
		public static Pred operator +(Rule a, LNode b) { return (RuleRef)a + b; }
		public static Pred operator +(LNode a, Rule b) { return a + (RuleRef)b; }
		public static implicit operator Rule(RuleRef rref) { return rref.Rule; }
		public static implicit operator RuleRef(Rule rule) { return new RuleRef(null, rule); }
	}

	/*
	 * token Id() {
	 *   (('@', {_verbatim=1;})`?`, NormalIdStart, NormalIdCont`*`) |
	 *   ('@', {_verbatim=1;}, SqString);
	 * }
	 * rule IdStart() { Letter | '_'; }
	 * rule IdCont()  { IdStart | '0'..'9'; }
	 * rule Letter()  { ('a'..'z') | ('A'..'Z') | (&{Char.IsLetter(LA0)}, _); }
	 * 
	 * 
	 * 
	 * rule goo() => { foo 'z' }
	 * rule foo() => { nongreedy(('a' | 'b') num | 'c')* ('c' | 'd') | 'a'+; }
	 * rule num() => { ('0'..'9')+; }
	 * 
	 * static readonly InputSet num_set0 = Range('0','9');
	 * void foo()
	 * {
	 *   int alt;
	 *   char LA0, LA1;
	 *   LA0 = LA(0);
	 *   if (LA0 == 'a') {
	 *     LA1 = LA(1);
	 *     if (LA1 >= '0' && LA1 <= '9')
	 *       alt = 0;
	 *     else
	 *       alt = 1;
	 *   } else
	 *     alt = 1;
	 *   
	 *   if (alt == 0) {
	 *     for (;;) { // for `?` use do...while(false)
	 *       LA0 = LA(0);
	 *       if (LA0 >= 'a' && LA0 <= 'b')
	 *         alt = 0;
	 *       else if (LA0 == 'c') {
	 *         if (LA0 == 'z')
	 *           break;
	 *         else
	 *           alt = 1;
	 *       } else
	 *         break;
	 *       
	 *       if (alt == 0) {
	 *         Consume(); // alt == 0 already implies 'a'..'b'; no check needed
	 *         num();
	 *       }
	 *     }
	 *   } else {
	 *     Consume();
	 *   }
	 * }
	 * void num()
	 * {
	 *   int alt;
	 *   char LA0;
	 *   LA0 = LA(0);
	 *   Match(LA0 >= '0' && LA0 <= '9', "0..9");
	 *   for (;;) {
	 *     LA0 = LA(0);
	 *     if (LA0 >= '0' && LA0 <= '9')
	 *       alt = 0;
	 *     else
	 *       alt = -1;
	 *     if (alt == 0)
	 *       Consume();
	 *     else
	 *       break;
	 *   }
	 * }
	 * 
	 * 
	 * Distinguishing argument lists from expressions: arg list when
	 * 1. beginning of statement, multiple words before parens, and/or
	 * 2. parens followed by =>
	 * a b c d(e < f, g > h, j < k, l < m >> m) => { ... }
	 * Note: constructors and destructors may look like exprs; assume fn call
	 *       and disambiguate in postprocessing step.
	 * Note: var decl in expression requires '=' to disambiguate, either right
	 *       after the type and name (int x = ...) or outside the parenthesis 
	 *       (x, int y, a[i]) = ...
	 */
}
