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
		
		public Rule(LNode basis, Symbol name, Pred pred, bool isStartingRule = true, bool isRecognizer = false)
		{
			Basis = basis; Pred = pred; Name = name;
			IsStartingRule = isStartingRule;
			IsRecognizer = isRecognizer;
			EndOfRule = new EndOfRule(this);
			if (basis != null && basis.Calls(S.Fn) && basis.ArgCount >= 3)
				ReturnType = basis.Args[0];
		}
		public Symbol Name;

		public Pred Pred;           // Contents of the rule
		public bool IsToken;        // Indicates that the follow set shall be treated as _*
		public bool IsStartingRule; // Whether the rule may be called directly by users
		public bool IsExternal;     // Suppresses code generation
		public bool IsInline;       // Causes naive inlining (e.g. inline rule can use vars in its host)
		public bool? IsPrivate;     // Enables prematch analysis benefits. Implies !IsStartingRule.
		public bool? FullLLk;       // Changes from mostly-correct approximate lookahead to exhaustive
		public int K;               // Max lookahead for disambiguation; <= 0 to use default for grammar

		#region Recognizer-related crap

		public bool IsRecognizer { get; private set; }
		public LNode TryWrapperName; // e.g. Try_Scan_Foo. only used when IsRecognizer==true
		private Rule _recognizer;
		
		public bool HasRecognizerVersion => _recognizer != null || IsRecognizer;
		public Rule Recognizer => _recognizer;
		public Rule GetOrMakeRecognizerVersion()
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
				if (IsPrivate == true)
					method = F.Attr(F.Id(S.Private), method);
				else if (IsStartingRule | IsToken)
					method = F.Attr(F.Id(S.Public), method);
				return method;
			}
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
