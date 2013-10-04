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

	public class Rule
	{
		/// <summary>A node that contains the original code of the rule, or, if the
		/// rule was created programmatically, the method prototype (e.g. 
		/// <c>#def(int, Rule, #(#var(int, arg)))</c>, which means 
		/// <c>int Rule(int arg)</c>). This can be null, in which case the
		/// default prototype is <c>void Rule();</c>, or if the rule is a 
		/// starting rule or token, <c>public void Rule();</c>.</summary>
		/// <remarks>The Basis is also used to provide an error location.</remarks>
		public LNode Basis;
		public readonly EndOfRule EndOfRule;

		public Rule(LNode basis, Symbol name, Pred pred, bool isStartingRule = true)
		{
			Basis = basis; Pred = pred; Name = name;
			IsStartingRule = isStartingRule;
			EndOfRule = new EndOfRule(this);
		}
		public Symbol Name;

		public Pred Pred;
		public bool IsToken, IsStartingRule;
		public bool IsPrivate, IsExternal;
		public bool? FullLLk;
		public bool IsRecognizer;
		public int K; // max lookahead; <= 0 to use default

		public Rule _recognizer;
		public Rule MakeRecognizerVersion(Symbol newName)
		{
			if (IsRecognizer) return this;
			_recognizer = (Rule)MemberwiseClone();
			_recognizer.IsRecognizer = true;
			_recognizer.Name = newName;
			return _recognizer;
		}
		public Rule MakeRecognizerVersion(LNode prototype)
		{
			if (IsRecognizer) return this;
			_recognizer = (Rule)MemberwiseClone();
			_recognizer.IsRecognizer = true;
			_recognizer.Basis = prototype;
			_recognizer.Name = prototype.Args[1].Name;
			return _recognizer;
		}
		public Rule MakeRecognizerVersion()
		{
			return _recognizer = _recognizer ?? MakeRecognizerVersion(GSymbol.Get("Is_" + Name.Name));
		}
		public bool HasRecognizerVersion { get { return _recognizer != null || IsRecognizer; } }
		
		// Types of rules...
		// "[#token]" - any follow set
		// "[#start]" - follow set is EOF plus information from calling rules;
		//    useful for top-level parser rules
		// "[#private]" - designed to be called by other rules in the same grammar;
		//    allows Consume() at the beginning of the rule based on predictions 
		//    in other rules. TODO: eliminate rule if it is not called.
		// "[#extern]" - Blocks codegen. Use for rules inherited from a base class or 
		//    implemented manually.
		// TODO: "[#inline]" - immediate rule contents are inlined into callers.
		//                   - alternately this could be a command at the call site.

		static readonly Symbol SavedPosition = GSymbol.Get("SavedPosition");

		/// <summary>Creates the default method definition to wrap around the body 
		/// of the rule, which has already been generated. Returns <see cref="Basis"/> 
		/// with the specified new method body. If Basis is null, a simple default 
		/// method signature is used, e.g. <c>public void R() {...}</c> where R is 
		/// the rule name.</summary>
		/// <param name="methodBody">The parsing code that was generated for this rule.</param>
		/// <returns>A method.</returns>
		public LNode CreateMethod(RVList<LNode> methodBody)
		{
			LNodeFactory F = new LNodeFactory(new EmptySourceFile("LLParserGenerator.cs"));
			Symbol name = Name;
			if (IsRecognizer) {
				var inner = methodBody;
				methodBody.Clear();
				methodBody.Add(F.Call(S.UsingStmt, F.Call(S.New, F.Call(SavedPosition, F.@this)), F.Braces(inner)));
				methodBody.Add(F.Call(S.Return, F.@true));
			}
			LNode methodBodyB = F.Braces(methodBody);

			if (Basis != null && !Basis.IsIdNamed(S.Missing)) {
				if (Basis.Calls(S.Def) && Basis.ArgCount.IsInRange(3, 4)) {
					var a = Basis.Args.ToRWList();
					a[1] = F.Id(name);
					if (a.Count == 3)
						a.Add(methodBodyB);
					else
						a[3] = methodBodyB;
					if (IsRecognizer)
						a[0] = F.Bool;
					return Basis.WithArgs(a.ToRVList());
				} else {
					// this is normal for recognizer fragments (e.g. RuleName_Test0)
					Trace.WriteLine(string.Format("CreateMethod(): Basis of Rule '{0}' is not a #def", Name));
				}
			}
			var rtype = IsRecognizer ? F.Bool : F.Void;
			var method = F.Def(rtype, F.Id(name), F.List(), methodBodyB);
			if (IsPrivate)
				method = F.Attr(F.Id(S.Private), method);
			else if (IsStartingRule | IsToken)
				method = F.Attr(F.Id(S.Public), method);
			return method;
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
