using System;
using System.Collections.Generic;
using System.Text;
using Loyc.CompilerCore;
using Loyc.Runtime;

namespace TempParserGenerator
{
	class Program
	{
		public static void Main(string[] args)
		{
		}

		void stuff()
		{
			
		}
	}

	class ParserGenerator
	{
		protected static readonly Symbol _Alt           = Symbol.Get("Alt");
		protected static readonly Symbol _Seq           = Symbol.Get("Seq");
		protected static readonly Symbol _Parens        = Symbol.Get("Parens");
		protected static readonly Symbol _Constraint    = Symbol.Get("Constraint");
		protected static readonly Symbol _Action        = Symbol.Get("Action");
		protected static readonly Symbol _Not           = Symbol.Get("Not");
		protected static readonly Symbol _Optional      = Symbol.Get("Optional");
		protected static readonly Symbol _Star          = Symbol.Get("Star");
		protected static readonly Symbol _Plus          = Symbol.Get("Plus");
		protected static readonly Symbol _AnyTerminal   = Symbol.Get("AnyTerminal");
		protected static readonly Symbol _Terminal      = Symbol.Get("Terminal");
		protected static readonly Symbol _Nonterminal   = Symbol.Get("Nonterminal");
		protected static readonly Symbol _Rule          = Symbol.Get("Rule");

		AstNode Node(Symbol type, string name, params AstNode[] children)
		{
			AstNode n = new AstNode(type, SourceRange.Empty, name);
			if (children != null)
				n.Block.AddRange(children);
			return n;
		}
		AstNode NT(string text) { return Node(_Nonterminal, text); }
		AstNode T(string text) { return Node(_Terminal, text); }
		AstNode Alt(params AstNode[] children) { return Node(_Alt, "", children); }
		AstNode Seq(params AstNode[] children) { return Node(_Seq, "", children); }
		AstNode Star(params AstNode[] children) { return Node(_Star, "*", children); }
		AstNode Optional(params AstNode[] children) { return Node(_Optional, "?", children); }
		AstNode SampleGrammar()
		{
			// Expr -> Term ((PLUS | MINUS) Term)*
			// Term -> Atom ((STAR | SLASH) Atom)*
			// Atom -> MINUS? (LPAREN Expr RPAREN | INT | ID)

			AstNode r1 = Node(_Rule, "Expr", NT("Term"), Star(Alt(T("PLUS"), T("MINUS")), NT("Term")));
			AstNode r2 = Node(_Rule, "Term", NT("Atom"), Star(Alt(T("STAR"), T("SLASH")), NT("Atom")));
			AstNode r3 = Node(_Rule, "Atom", Optional(NT("MINUS")), 
			                          Alt(Seq(T("LPAREN"),NT("Expr"),T("RPAREN")), T("INT"), T("ID")));
			return Node(Symbol.Get("_class"), "ExprParser", r1, r2, r3);
		}

		AstNode Generate(AstNode ruleClass)
		{
			throw new NotImplementedException();
		}
	}

	// Okay, we need a representation for the parser generator:
	// - AST representation
	// - Parsing algorithm

	// Operators:       Symbol         Comment
	// A | B ...        :Alt           Match one of N alternatives; items listed first have precedence
	// A B ...          :Seq           Match all of N items, in order
	// (E)              :Parens
	// A => B           :Shortcut      Shortcut: for matching purposes, test A but match B. The follow
	//                                 sets of both A and B are ignored. Turns off ambiguity warnings 
	//                                 between A and later alternatives. Symbol A may be a predicate.
	// {A}?             :Constraint    Semantic predicate: requires that code segment A evaluates to 
	//                                 true in order to continue past the predicate. A should not have 
	//                                 side-effects.
	// &{A}             :Constraint    Synonym for {A}?
	// &A               :Constraint    Syntactic constraint: must match A to continue
	// &!A              :ConstraintNot Syntactic constraint: must not match A to continue
	// {A}              :Action 
	// ~A               :Not           Matches an input symbol that does not match A
	// E?               :Optional      equivalent: (E |)
	// E*               :Star          equivalent: X, where X -> E X |
	// E+               :Plus          equivalent: E X, where X -> E X |
	// A=B              :Assign        match B and assign the return value of B to variable A
	// A+=B             :Append        match B and append the return value of B to list A
	//
	// Atoms:
	// .                :AnyTerminal   Matches any input symbol
	// <identifier>     :Terminal      Matches the specified terminal
	// <identifier>     :Nonterminal   Matches the specified nonterminal

	// nongreedy(E)
	//    greedy(E)

	// Algorithm:
	// - First, we should convert the AST into a simplified form with fewer operators. Let's see...
	//   
	// Expr -> Term ((PLUS | MINUS) Term)*
	// Term -> Atom ((STAR | SLASH) Atom)*
	// Atom -> MINUS? (LPAREN Expr RPAREN | INT | ID)
	// 
	// Desired representation:
	// - A rule (or subrule) is a list of alternatives. Each alternative is a sequence.
	// - A sequence consists of a lookahead spec and a match spec, which are often
	//   identical to each other. The lookahead spec specifies the pattern which will
	//   cause the parser to choose to match the sequence, while the match spec 
	//   specifies what to match and any additional actions to take. Constraints are
	//   only present in the lookahead spec, actions are only present in the match spec,
	//   and when a shortcut A => B is encountered, A becomes the lookahead spec and B 
	//   becomes the match spec.
	// - A spec can contain Terminal sets, Nonterminals, Constraints and Actions (but
	//   only lookahead specs have Constraints and only match specs have Actions).
	// - A Terminal set is either a single Terminal, a set of terminals (A | B | C), a 
	//   negative set of terminals (denoted by the ~ operator), or "any" terminal (.).
	//   LoycPG will allow a terminal to be any arbitrary object, so a terminal set 
	//   could be an arbitrarily complex object. The important thing is that terminal
	//   sets must support set operations (union, intersection and negation) and 
	//   LoycPG needs to be able to tell whether a given terminal set is empty.
	//   Typically, terminals are either characters or tokens.

	// Terminology:
	// rule: a named nonterminal and an expression that describes what it matches and what actions to take when a match is encountered.
	// subrule: an expression of the form E?, E*, or E+ or (F) where E is any expression and F is a production (list of alternatives)
	// production: a list of alternatives
	// literal phrase: an ordered list of terminals
	// first set
	// follow set

	
}
