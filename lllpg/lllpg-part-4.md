---
title: "LLLPG Part 4: Managing Ambiguity + API reference"
layout: article
date: 25 Feb 2014 (updated 22 May 2016)
tagline: "The ambivalent world of ambiguity, the slash, greedy and nongreedy. At the end, in lieu of refreshments, there will be an API reference."
toc: true
redirectDomain: ecsharp.net
---

# This page is obsolete # 

The [LLLPG manual](/lllpg) has been reorganized. These old articles may be deleted in the future.

Welcome to part 4
-----------------

_New to LLLPG? Start at [part 1](http://www.codeproject.com/Articles/664785/A-New-Parser-Generator-for-Csharp)_.

Part 4 is all about nitty-gritty details: how prediction works, a discussion of ambiguity (what it is, common ambiguous situations, and how to deal with them), and a list of APIs that LLLPG calls in generated code.

FullLLk versus "approximate" LL(k)
----------------------------------

First, the short version: try adding `[FullLLk(true)]` to your grammar if you suspect prediction isn't working perfectly.

Now, it's a bit difficult to explain how LLLPG generates a prediction tree without invoking all sorts of math-speak that, if you are like me, would make your head hurt. It is easier to explain with examples. Let's start simple:

	rule Comparison @[ '>' '=' | '<' '=' | '=' '=' | '>' | '<' ];

	void Comparison()
	{
	  int la0, la1;
	  la0 = LA0;
	  if (la0 == '>') {
		 la1 = LA(1);
		 if (la1 == '=') {
			Skip();
			Skip();
		 } else
			Skip();
	  } else if (la0 == '<') {
		 la1 = LA(1);
		 if (la1 == '=') {
			Skip();
			Skip();
		 } else
			Skip();
	  } else {
		 Match('=');
		 Match('=');
	  }
	}

Roughly what happens here is that LLLPG

1. Finds the first set for each arm: {'>'} for 1 and 4, {'<'} for 2 and 5, {'='} for 3.
2. Finds a common subset between arm 1 and the others. In this case it finds {'>'}, common between 1 and 4.
3. Generates the `if (la0 == '>') {...}` statement and then generates an inner prediction tree _based on the knowledge_ that la0 is in the set {'>'}, which excludes arms 2, 3 and 5.
4. Knowing that `la0 != '>'`, it excludes arms 1 and 4, then looks for another common subset and finds {'<'}, common between 2 and 5.
5. Generates the `if (la0 == '<') {...}` statement and then generates an inner prediction tree _based on the knowledge_ that la0 is in the set {'<'}, which additionally excludes arm 3.
6. Only one arm is left, arm 3, and this becomes the else branch.

Note: the generated code is correct, although this example is unusual because arm 3 ends up acting as if it were the `default` branch. The code will change if you explicitly mark the last arm as the `default`, or if you add an `error` branch.

Here's another example:

    rule ABCD @[ (A B | C D) {} | A D ];

    void ABCD()
    {
      int la0, la1;
      do {
        la0 = LA0;
        if (la0 == A) {
          la1 = LA(1);
          if (la1 == B)
            goto match1;
          else {
            Skip();
            Match(D);
          }
        } else
          goto match1;
        break;
      match1: { ... } // omitted for brevity
      } while (false);
    }

**Note**: `{}` forces LLLPG to create two prediction trees instead of one, see "A random fact" from the previous article.

I'm using this example because it was mentioned by Terrance Parr as something that ANTLR 2 couldn't handle. LLLPG has no problem; to generate the outer prediction tree, LLLPG

1. Finds the first set for each arm: {`A`,`B`} for 1, {`A`} for 2.
2. Finds a common subset between arm 1 and the others. In this case it finds {`A`}.
3. Generates the `if (la0 == A) {...}` statement and then generates an inner prediction tree (for `LA(1)`) _based on the knowledge_ that la0 is in the set {`A`}, which excludes the _inner_ arm `C D` of `(A B | C D)`.
4. Knowing that `la0 != A`, it excludes arms 2, leaving only arm 1, and this becomes the `else` branch.

**Note**: I've been speaking as though LLLPG generates code during prediction, but it doesn't. Instead there is an abstract intermediate representation for prediction trees, and the C# code is only generated after analysis and prediction is complete.

I didn't realize it at first, but LLLPG's technique doesn't support all LL(k) grammars. It is more powerful than the [Linear Approximate Lookahead](http://www.antlr2.org/doc/glossary.html#Linear_approximate_lookahead) of ANTLR 2, but some cases still don't work, like this one:

	LLLPG (lexer)
	{
		[LL(3)]
		token Token    @[ Number | Operator | ' ' ];
		token Operator @[ '+'|'-'|'*'|'/'|'.' ];
		token Number   @[ '-'? '.'? '0'..'9'+ ];
	}

After (correctly) warning that `Alternatives (1, 2) are ambiguous for input such as «'-' '.' 0» ([\-.], [.0-9], ~())`, LLLPG generates this slightly incorrect code for `Token`:

    void Token()
    {
      int la1;
      switch (LA0) {
      case '-': case '.':
        {
          la1 = LA(1);
          if (la1 == '.' || la1 >= '0' && la1 <= '9')
            Number();
          else
            Operator();
        }
        break;
      case '0': case '1': case '2': case '3': case '4':
      case '5': case '6': case '7': case '8': case '9':
        Number();
        break;
      case '*': case '+': case '/':
        Operator();
        break;
      default:
        Match(' ');
        break;
      }
    }

To choose this code, LLLPG

1. Finds the first set for each arm: {`'+','-','*','/','.'`} for 1, {`'-','.','0'..'9'`} for 2 and {`' '`} for 3.
2. Finds a common subset between arm 1 and the others. In this case it finds {`'-','.'`}.
3. Generates the `case '-': case '.': {...}` block and then generates an inner prediction tree (for `LA(1)`) _based on the knowledge_ that `LA0` is in the set {`'-','.'`}, which excludes only the possibility of arm 3 (`' '`).
  - LLLPG computes the _second sets_ which (keeping in mind that `LA0` is `'-'` or `'.'`) are {`'.','0'..'9'`} for arm 1 and `_` (any character) for arm 2.
  - It finds the common subset, {`'.','0'..'9'`}
  - It generates the `if (la1 == '.' || la1 >= '0' && la1 <= '9')` and generates an inner prediction tree for `LA(2)`.
    - `LA(2)` can be anything `_` for both rules, so LLLPG reports an ambiguity between 1 and 2 and chooses 1 (`Number()`) as it has higher priority because it is listed first.
  - Knowing that LA(1) is not in the set {`'.','0'..'9'`}, it excludes arms 1, leaving only arm 2, and this becomes the `else` branch.
4. It generates the other cases, which are easy to understand so I'll skip them.

Now, why is the generated code wrong? It's wrong in the case of the input string "`-. `", which should match `Operator` but instead matches `Number`. To fix this, I added a finer-grained analysis that is enabled by the `[FullLLk]` option.

	[LL(3)] [FullLLk]
	token Token    @[ Number | Operator | ' ' ];

This analysis realizes that, due to the relatively complex substructure of `Number`, it should split `'-'` and `'.'` into two separate cases. 

When analyzing the case `la0 == '-'`, the set for `LA(1)` for arm 1 is still {`'.','0'..'9'`}, but LLLPG further figures out that it should split the analysis of `LA(1)` into separate subtrees for `'.'` and `'0'..'9'`. In the subtree where `la0 == '-'` and `la1 == '.'`, LLLPG is able to figure out that `Number` should only be invoked if `LA(2)` is `'0'..'9'`. It is able to figure this out now because the information `la0 == '-' && la1 == '.'` is more _specific_ than the information it had without `[FullLLk]` (i.e. `(la0 == '-' || la0 == '.') && (la1 == '.' || la1 >= '0' && la1 <= '9')`).

So after the more detailed analysis of `[FullLLk]`, the output code becomes

    void Token()
    {
      int la1, la2;
      switch (LA0) {
      case '-':
        {
          la1 = LA(1);
          if (la1 == '.') {
            la2 = LA(2);
            if (la2 >= '0' && la2 <= '9')
              Number();
            else
              Operator();
          } else if (la1 >= '0' && la1 <= '9')
            Number();
          else
            Operator();
        }
        break;
      case '.':
        {
          la1 = LA(1);
          if (la1 >= '0' && la1 <= '9')
            Number();
          else
            Operator();
        }
        break;
      case '0': case '1': case '2': case '3': case '4':
      case '5': case '6': case '7': case '8': case '9':
        Number();
        break;
      case '*': case '+': case '/':
        Operator();
        break;
      default:
        Match(' ');
        break;
      }
    }

You still get the ambiguity warning, though. Use a slash to suppress the warning: `Number / Operator | ' '`.

Full LL(k) mode doesn't always work perfectly, and may make LLLPG run slower, which is why it is not enabled by default. But usually, it works fine and you can safely apply it to your entire grammar.

In certain cases, LLLPG reports an ambiguity that doesn't actually exist in a grammar without the `[FullLLk]` option. One example is given by [this blog post](http://loyc-etc.blogspot.ca/2013/12/bogus-ambiguity-warnings-in-lllpg.html) that I wrote while writing the EC# grammar. So if you can't figure out where an ambiguity comes from, try `[FullLLk]`. If you still get the same ambiguity warning after enabling Full LL(k), check over your grammar carefully, because it is probably genuinely ambiguous.

Ambiguity: introduction
-----------------------

In the context of a parser generator, _Ambiguity_ refers to the situation in which, for a particular grammar, there can exist more than one potential parse tree for an input. Programming languages are designed to be less ambiguous than human languages, but their grammars generally do have ambiguities anyway. Some ambiguities are normal and expected and there is a standard "solution" to the problem, while others may be unique to the language you are parsing.

There are various kinds of ambiguities that I will illustrate by way of actual published newspaper headlines...

**"Milk Drinkers are Turning to Powder." "Red tape holds up new bridge." "Kids Make Nutritious Snacks." "Prostitutes Appeal to Pope." "Stolen painting Found by Tree."** These sentences are ambiguous because of particular words or phrases that have multiple meanings: "turning", "holds up", "red tape", "make", "appeal" and "by". Ambiguities of this sort are easily avoided by giving words and symbols only one meaning. You can safely define multiple meanings, though, if you define a unique structure or context for each meaning, e.g. in C# there are two `using` statements, but in one case `using` is _always_ followed by `(` and in the other case it is _never_ followed by `(`. Plus, one only appears at the beginning of a file and the other never appears at the beginning of a file.

**"Squad Helps Dog Bite Victim." "Complaints About NBA Referees Growing Ugly" "Quarter of a Million Chinese Live on Water"** A lot of ambiguities in newspaper headlines are caused by the omission of "is" or "are" or a definite or indefinite article (the, a, an), or by the reader's _expectation_ of an omitted word. The problem exists only in certain languages such as English, and is easily avoided in programming languages by including sufficient redundancy to prevent ambiguity; for example, if the semicolons between the clauses of a "for" loop were not required, a loop like `for (i = j + 1; -i > k; ++i)` would become ambiguous because `for (i = j + 1 -i > k ++i)` could be parsed in several ways.

**"Hospitals are Sued by 7 Foot Doctors." "Hershey Bars Protest."** In these cases, the sentences permit different parts of speech for certain words, which allows different sentences structures to emerge, causing ambiguity. In programming languages, an example of a similar ambiguity arises if you define

1. An operator that can be prefix or infix, e.g. `-`: `-x` or `x-y`
2. A different operator that can be suffix or infix, e.g. `*`: `x*` or `x*y`

These operators are perfectly unambiguous by themselves; a problem arises only when you combine them in a certain way. Specifically, the expression `x * - y` is ambiguous, as it could be parsed `x * (- y)` or `(x *) - y`. For this reason, if a real-life programming language contains a suffix operator, that operator is usually not an infix operator also. C++, somewhat famously, violates this rule with the pointer/multiplication operator `*`: `X * Y;` could be interpreted in two ways: it may define a variable of type `X*` named `Y`, or it may invoke an overloaded `operator*` function (just as `cout >> Y;` is a valid statement that calls `operator>>`). C# doesn't have exactly the same problem because, for example, `cout >> Y;` is always illegal in C#; however, you can tell that the C# parser understands the expression because it reports the following error:

> error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement

In order to report this error and _only_ this error, the C# parser still detects that `cout >> Y` is a valid expression. You can see this because the error messages _change_ for other invalid input like `x */ Y;` or `<< hello there >>;`. Thus, in C# the same ambiguity exists at the parser level and is solved in a similar manner as in C++, by giving higher priority to the "variable declaration" interpretation than the "expression" interpretation.

**"Include your Children When Baking Cookies."** Here, the sentence is _unambiguous_ from a parsing perspective, but has incomplete information: there are two different ways the children could be "included" in the process and I'm not sure I would trust a computer to choose an interpretation! This is not a parsing issue; the sentence can be parsed unambiguously, yet its meaning is ambiguous. I suppose an analogy in computer languages would be this C# code:

	class A { public virtual void F(Foo x) {...} }
	class B : A {
		public override void F(Foo x) {...} // first
		public          void F(Bar x) {...} // second
	}

If I write `new B().F(new Foo())`, will the compiler call the first or second method? In fact, under certain circumstances, a C# compiler will call the second method (brownie points go to the first person to explain why).

**"Enraged Cow Injures Farmer With an Axe."** Here, the issue is that the "With" clause can attach to either "Farmer" or "Injures" (or is it "Cow"?) In computer languages, this type of problem is generally solved _precedence rules_ and _parenthesis_. In LL(k) parsers, precedence rules are typically expressed by creating a rule for every precedence level, plus an innermost rule for parenthesis, identifiers and literals, which I like to call `Atom`. So if you want these four precedence levels:

1. Primary: `a.b`, `f(x)`
2. Prefix: `-a`
3. multiply/divide: `a*b`, `a/b`
4. add/subtract: `a+b`, `a-b`

Then you will need five rules that typically have the following form (plus a bunch of `{actions}` that I left out):

	// (before this you'll need to define some aliases for '.', '+', '-', etc.)
	rule Atom()        @[ TT.Id | TT.Num | '(' Expr ')'        ];
	rule PrimaryExpr() @[ Atom [ '.' Atom | '(' Expr ')' ]*    ];
	rule PrefixExpr()  @[ '-' Atom | Atom                      ];
	rule MulExpr()     @[ PrefixExpr [ ('*'|'/') PrefixExpr ]* ];
	rule Expr()        @[ MulExpr    [ ('+'|'-') MulExpr ]*    ];

If you write the grammar in a single rule like this:

	rule Expr() @
		[ Expr ('*'|'/') Expr
		| Expr ('+'|'-') Expr
		| '-' Expr
		| TT.Id | TT.Num | '(' Expr ')'
		];

The grammar is not only ambiguous, but also left-recursive (an `Expr` can start with an `Expr`), which LLLPG is completely unable to handle. If you use two rules, like this:

	rule Atom() @[ TT.Id | TT.Num | '(' Expr ')' ];
	rule Expr() @[
		( Atom | '-' Atom ) 
		[ ('*'|'/') Atom
		| ('+'|'-') Atom
		]*
	];

The grammar is **not** ambiguous, but will parse expressions like a cheap calculator (so that `2+3*4 = 20`) instead of a scientific calculator (`2+3*4 = 14`). Also, `- - x` cannot be parsed since `-` is to be followed by `Atom` (if you write `'-' Expr`, the grammar is ambiguous _and_ the parser will have strange behavior, parsing `2 * -3 + 4` like `2 * -(3 + 4)` because LLLPG parses greedily by default).

In traditional LL(k) parsers, you must define a separate rule for every precedence level, and in EC# there are 22 levels. It is possible to collapse many levels into a single rule, though, and in the next article I will describe how.

**Tip**: when writing your first parser, write it without any important `{actions}`, i.e. don't create a syntax tree at first. Just focus on making a _recognizer_, like the examples above, that simply scans through the input without interpreting it.

Managing ambiguity, part 1: `token` rules
-----------------------------------------

By declaring a token using `token` instead of `rule`, you're asking LLLPG to simplify its analysis while avoiding warnings about a certain type of ambiguity.

A lexer separates a text document into a sequence of tokens, so it could be written like this:

	public struct Tok { ... }

	LLLPG(lexer)
	{
		public rule List<Tok> Start @[
			{List<Token> ts;} ts+=Token* EOF {return ts;}
		];
		token Tok Token() {
			// BTW: Instead of writing a @[...] block with {...} actions inside,
			// LLLPG lets you write a {...} block with @[...] blocks inside. But 
			// currently the @[...] blocks must be at the top level of the method,
			// not nested inside anything else such as a try {...} region.
			Token t;
			@[ t=Spaces | t=Id | t=Int | t=Op ];
			return t;
		}
		token Tok Spaces @[ (' '|'\t')+      {return new Token(...);} ];
		token Tok Id     @[ ('a'..'z'|'A'..'Z') ('a'..'z'|'A'..'Z'|'0'..'9')+
		                                     {return new Token(...);} ];
		token Tok Int    @[ ('0'..'9')+      {return new Token(...);} ];
		token Tok Op     @[ op:=('+'|'-'|'*'|'/'|'=')+  
		                                     {return new Token(...);} ];
	}

The thing is, any lexer grammar that follows this pattern is ambiguous! Because of the loop in `Start`, an identifier like "`ab3`" could theoretically be parsed in four different ways: as a single Id "`ab3`", as two Ids "`a`" "`b3`", as two tokens "`ab`" "`3`", or as three tokens "`a`" "`b`" "`3`".

So if all the rules are written using `rule` rather than `token`, LLLPG will report three ambiguities, one each for `Id` or `Num` and `Spaces`:

- Warning: (12,23): Loyc.LLParserGenerator.Macros.run_LLLPG: Alternatives (1, exit) are ambiguous for input such as « *» ([\t ], [\$\t *+\-/-9=A-Za-z])
- Warning: (13,43): Loyc.LLParserGenerator.Macros.run_LLLPG: Alternatives (1, exit) are ambiguous for input such as «0*» ([0-9A-Za-z], [\$\t *+\-/-9=A-Za-z])
- Warning: (15,23): Loyc.LLParserGenerator.Macros.run_LLLPG: Alternatives (1, exit) are ambiguous for input such as «0*» ([0-9], [\$\t *+\-/-9=A-Za-z])

LLLPG detects the ambiguity while looking at the _follow set_ of each rule, which it does while analyzing the loops. Since `Token` appears in a loop, `Id` can theoretically be followed by another `Id`, or by `Num`, so the location where an `Id` or `Num` or `Spaces` loop should _stop_ is ambiguous.

I created `token` mode to avoid warnings like this and the potentially complex analysis that produces them. `token` replaces the follow set of a rule with `_*` (i.e. anything), and then suppresses the inevitable ambiguity warning (because a decision between _* and anything else always ambiguous.) The mode is called `token` since it is useful in the context of a lexer, but occasionally it is useful in parsers too.

By convention, when I write a lexer, I mark the top-level token rules with `token`, and I use `rule` for the sub-rules that are called by `token`s.

Managing ambiguity, part 2: LLLPG's missing feature
---------------------------------------------------

In certain ambiguous cases, notably those in which some alternatives are prefixes of others, some parser generators (including ANTLR) have the ability to select the _longest match_ automatically. LLLPG does not have this ability, and you'll notice this problem when writing rules for operators:

	token CompareOp() @[ '>' | '<' | '=' | ">=" | "<=" ];
	
The output is:

- Warning: (4,23): Loyc.LLParserGenerator.Macros.run_LLLPG: Alternatives (1, 2) are ambiguous for input such as «>=» ([>], [=])
- Warning: (4,23): Loyc.LLParserGenerator.Macros.run_LLLPG: Alternatives (1, 3) are ambiguous for input such as «<=» ([<], [=])
- Warning: (4,23): Loyc.LLParserGenerator.Macros.run_LLLPG: Branches 2, 3 are unreachable.

        void CompareOp()
        {
          MatchRange('<', '>');
        }

Please excuse the strange numbering scheme in the error message: LLLPG actually interprets `'>' | '<' | '=' | ">=" | "<="` as `('>' | '<' | '=') | ">=" | "<="`, with the three single characters unified into a set, and it turns out that `('>' | '<' | '=')` is equivalent to `'<'..'>'`, which explains where `MatchRange('<', '>')` comes from.

What's going on here? Well, if the input is '`<=`', '`<`' matches just as well as '`<=`', and because it is listed first, LLLPG gives it higher priority. So '`<=`' is unreachable because '`<`' takes priority, and '`>=`' is unreachable because '`>`' takes priority. This is easily fixed by always listing longer operators first:

	token CompareOp() @[ ">=" | "<=" | '>' | '<' | '=' ];

Keywords are even more tricky. Let's say you have the keywords `fn`, `for`, `if` and `while`:

	rule IdStartChar  @[ 'a'..'z'|'A'..'Z'|'_' ];
	rule Id           @[ IdStartChar (IdStartChar|'0'..'9')* ];
	
	[LL(6)] // Longest keyword plus one
	token IdOrKeyword @[ "fn" | "for" | "if" | "while" | Id ];

Here I've given `Id` lower priority than the keywords, which will usually work correctly. However, it won't work correctly for an `Id` prefixed by a keyword, such as `form`, which of course will parse as `for` followed by '`m`' as a separate identifier. There is a solution, which I'll show you in the next article, but LLLPG does not solve the problem automatically.

Managing ambiguity, part 3: the slash operator
----------------------------------------------

The slash operator suppresses the ambiguity warning between two or more alternatives. The warnings you saw for

	token CompareOp() @[ ">=" | "<=" | '>' | '<' | '=' ];

can be suppressed by switching '`|`'s to '`/`'s:

	token CompareOp() @[ ">=" / "<=" / '>' / '<' | '=' ];

'`/`' is transitive, so in this example, the ambiguity between `">="` and `'>'` is suppressed and likewise between `"<="` and `'<'`. Note that the following will _not_ suppress the warnings:

	token CompareOp() @[ ">=" / "<=" | '>' / '<' | '=' ];

(Footnote: in fact this would have suppressed the warnings in earlier versions of LLLPG, but the logic has changed. I will describe only the new rules.)

Earlier I said that LLLPG "doesn't care much about parenthesis", so that, for instance,

	rule Foo @[ ["AB" | "A" | "CD" | "C"]*     ];

is equivalent to 

	rule Foo @[ [("AB" | "A") | ("CD" | "C")]* ];

That's true, but as of version 1.1.0 it will now pay attention to the relationship between the slash operator and parenthesis. In

	token CompareOp() @[ ">=" / "<=" | '>' / '<' | '=' ];

LLLPG is instructed to suppress warnings between `">=" / "<="` and `'>' / '<'`, but that's all. '`/`' has higher precedence than '`|`', so this is equivalent to 

	token CompareOp() @[ (">=" / "<=") | ('>' / '<') | '=' ];

And the '`|`' operator causes warnings between the two groups (`">=" / "<="` and `'>' / '<'`) to be permitted. If you write 

	token CompareOp() @[ ">=" | "<=" / '>' | '<' | '=' ];

no warnings are suppressed either, but if you now add parenthesis:

	token CompareOp() @[ (">=" | "<=") / ('>' | '<') | '=' ];

then the warnings are suppressed, because there is now a slash separating `">="` / `'>'` and `"<="` / `'<'`.

You may remember from <a href="http://www.codeproject.com/Articles/688152/The-Loyc-LL-k-Parser-Generator-Part-2">Part 2</a> that slash is the alt-separator in PEGs. In LLLPG, the slash operator works similarly, but quite the same (in fact, `/` always yields the same code as `|`.) Here's an example where a PEG would parse differently from LLLPG:

    rule abc @[ ('a' / 'a' 'b') 'c' ];

In a PEG (correct me if I'm wrong), the first branch always takes priority and the second branch is unreachable. If the input is `ac`, a PEG will not backtrack and try the `'a' 'b'` branch because the first branch was matched successfully. An LL(k) parser generator, however, performs prediction on the first `k` characters, even if those characters are beyond the list of alternatives under consideration, and that means the `'c'` influences code generation, producing the following code (by default):

	void abc()
	{
	  int la1;
	  la1 = LA(1);
	  if (la1 == 'c')
	    Match('a');
	  else {
	    Match('a');
	    Match('b');
	  }
	  Match('c');
	}

Managing ambiguity, part 4: greedy and nongreedy
------------------------------------------------

LLLPG supports '`greedy`' and '`nongreedy`' loops and optional items. The '`greedy`' 
and '`nongreedy`' modes refer to the action you prefer to take in case of
ambiguity between an exit branch and another branch. Greedy is the default: it
means that if the input matches both a non-exit branch and an exit branch, the
non-exit branch should be taken. A typical greedy example is this rule for an
"if" statement:

      private rule IfStmt @[ 
        "if" "(" Expr ")" Stmt greedy("else" Stmt)?
      ];
      private rule Stmt @[ 
        IfStmt | OtherStmt | Expr ";" | ...
      ];
  
In this case, it is possible that the "if" statement is nested inside another
"if" statement. Given that the input could be something like

    if (expr) if (expr)
      Stmt();
    else
      Stmt();

It is, _in general_, ambiguous whether to consume `TT.Else Stmt` or to exit,
because the else clause could be paired with the first "if" or the second one.
The "greedy" modifier, which must always be paired with a loop or option
operator (`* + ?`) means "in case of ambiguity with the exit branch, do not exit
and do not print a warning." Since greedy behavior is the default, the greedy
modifier's only purpose is to suppress the warning.  

Now, you might logically think that changing '`greedy`' to '`nongreedy`' would
cause the '`else`' to match with the outer '`if`' statement rather than the inner
one. Unfortunately, that's not what happens! It does not work because the code
generated for `IfStmt` is not aware of the run-time call stack leading up to it:
it does not know whether it is nested inside another `IfStmt` or not. LLLPG only
knows that it _could_ be nested inside another '`if`' statement; the technical
jargon for this is that the _follow set_ of the `IfStmt` rule includes
`TT.Else Stmt`.
  
What actually happens is that `nongreedy(TT.Else Stmt)?` will _never_ match
`TT.Else`, and LLLPG will give you a warning that "branch 1 is unreachable". Not
knowing the actual context in which `IfStmt` was called, LLLPG is programmed to
assume that all possible follow sets of `IfStmt` apply simultaneously, even
though in reality `IfStmt` is called in one specific context. The statically
computed follow set of `IfStmt`, which is based on all possible contexts where
`IfStmt` might appear, includes `TT.Else Stmt`, and `nongreedy` uses this
information to decide, unconditionally, to let the exit branch win. To put it
another way, LLLPG behaves as if `IfStmt` is _always_ called from inside another
`IfStmt`, when in reality it merely _might_ be. It would be fairly difficult for
LLLPG to behave any other way; how is the `IfStmt()` method supposed to know
call stack of other rules that called it?  
  
By the way, I have the impression that the formal way of describing this
limitation of LLLPG's behavior is to say that LLLPG supports only ["strong"
LL(k) grammars](http://slkpg.byethost7.com/llkparse.html), not "general" LL(k)
grammars (this is true even when you use `FullLLk(true)`).  
  
So at the end of a rule, LLLPG makes decisions based on all possible contexts
of that rule, rather than the actual context. Consequently, `nongreedy` is not
as useful as it could be. However, `nongreedy` still has its uses. Good
examples include strings and comments:

      token TQString @[ "'''" (nongreedy(_))* "'''" ];
      token MLComment @[ "/*" (nongreedy(MLComment / _))* "*/" ];

This introduces the single underscore `_`, which matches any single terminal
(not including EOF).  
  
The first example defines the syntax of triple-quoted strings `'''like this
one'''`. The contents of the string are any sequence of characters _except_
`"'''"`, which ends the string. The `nongreedy` modifier is important; without
it, the loop `(_)*` will simply consume all characters until end of file, and
then produce errors because the expected `"'''"` was not found at EOF.  

The second example for `/* multi-line comments */` is similar, except that
(just for fun) I decided to support nested multi-line comments by calling the
`MLComment` rule recursively.  

There's actually a bug in `TQString`, assuming that LLLPG is left in its
default configuration. Moreover, LLLPG will not print a warning about it. Got
any idea what the bug is? I'm about to spoil the answer, so if you want to
give it some thought, do so now before you start glancing at the lower half of
this paragraph. Well, if you actually tested this code you might notice that a
string like `'''one''two'''` will be parsed incorrectly, because two quotes,
not three, will cause the loop to exit. The reason is that the default maximum
lookahead is 2, so two quotes are enough to make LLLPG decide to exit the loop
(and then the third `Match('\'')` in the generated code will fail). To fix
this, simply add a `[k(3)]` attribute to the rule. No warning was printed
because half the purpose of `nongreedy` is to suppress warnings; after all,
mixing `(_)*` with anything else is inherently ambiguous and will frequently
cause a warning that you must suppress.

Earlier I ran into an unfortunate situation in which neither `greedy` nor
`nongreedy` was appropriate. I was writing a Visual Studio "classifier" for
syntax-highlighting of LES, and I decided to use a line-based design where
lexing would always start at the beginning of a line. Therefore, I needed to
keep track of which lines started inside multi-line comments and triple-quoted
strings. Now, if a line starts inside a comment or string, I invoke a special
rule that is designed to parse the rest of the comment or string, or stop at
the end of the line. Since LES supports nested multi-line comments, I wrote
the following rule:

		// (LES code, so "nested::int" instead of "int nested")
      public token MLCommentLine(ref nested::int)::bool @[ 
        (nongreedy
          ( &{nested>0} "*/" {nested--;}
          / "/*" {nested++;}
          / ~('\r'|'\n')
          ))*
        (Newline {return false;} | "*/" {return true;})
      ];

This rule takes the current comment nesting level as an argument (0 = comment
is not nested) and updates the nesting level if it changes during the current
line of code. The loop has three arms:

  1. For input of "*/" when comments are nested, reduce the nesting level
  2. For input of "/*", increase the nesting level
  3. For input of anything else (not including a newline), consume one character.

I chose '`nongreedy`' because otherwise the third branch `~('\r'|'\n')` will
match the first character of "*/", so the loop would never exit. But this
didn't work; LLLPG gave the warning "branch 1 is unreachable". Why is it
unreachable? I have to admit, I couldn't figure it out at first. If you feel
like you're stumped by LLLPG warnings sometimes, you're not alone, they
sometimes confuse me too. In this case I was confused because I thought the
predicate `&{nested>0}` would choose whether to stay in the loop or exit. But
in fact `nongreedy` gives the exit branch a higher priority than the first
branch, so regardless of whether `&{nested>0}`, LLLPG would always choose the
exit branch when the input is '`*/`'.

At that point I realized that what I wanted was a loop that was neither greedy
nor nongreedy, in which the priority of the exit branch is somewhere in the
middle. I wanted to be able to write something like this, where '`exit`' is
higher priority than `~('\r'|'\n')` but lower priority than `&{nested>0}
"*/"`:

      public token MLCommentLine(ref nested::int)::bool @[ 
        ( &{nested>0} "*/" {nested--;}
        / "/*" {nested++;}
        / exit
        / ~('\r'|'\n')
        )*
        (Newline {return false;} | "*/" {return true;})
      ];

Unfortunately, LLLPG does not support this notation. Maybe in a future
version. Here's what I did instead:

      public token MLCommentLine(ref nested::int)::bool @[ 
        (greedy
          ( &{nested>0} "*/" {nested--;}
          / "/*" {nested++;}
          / ~('\r'|'\n'|'*')
          / '*' (&!'/')
          ))*
        (Newline {return false;} | "*/" {return true;})
       ];

Here, I switched back to a greedy loop and added `'*'` as its own branch with an
extra check to make sure `'*'` is not followed by `'/'`. If the test `&!'/'`
succeeds, the new fourth branch matches the `'*'` character (but not the
character afterward); otherwise the loop exits. I could have also written it
like this, with only three branches:

      public token MLCommentLine(ref nested::int)::bool @[ 
        (greedy
          ( &{nested>0} "*/" {nested--;}
          / "/*" {nested++;}
          / (&!"*/") ~('\r'|'\n')
          ))*
        (Newline {return false;} | "*/" {return true;})
      ];

However, this version is slower, because LLLPG will actually run the `&!"*/"`
test on every character within the comment.  

Here's one more example using nongreedy:

    // Parsing a comma-separated value file (.csv)
    public rule CSVFile @[ Line* ];
    rule Line           @[ Field greedy(',' Field)* (Newline | EOF) ];
    rule Newline        @[ ('\r' '\n'?) | '\n' ];
    rule Field          @[ nongreedy(_)*
                         | '"' ('"' '"' | nongreedy(~('\n'|'\r'))* '"' ];

This grammar describes a file filled with fields separated by commas (plus I
introduced the EOF symbol, so that no Newline is required at the end of the
last line). Notice that `Field` has the loop `nongreedy(_)*`. How does LLLPG
know to when to break out of the loop? Because it computes the "follow set" or
"return address" of each rule. In this case, 'Field' can be followed by
`','|'\n'|'\r'|EOF`, so the loop will break as soon as one of these characters
is encountered. This is different than the `IfStmt` example above in an
important respect: `Field` always has the same follow set. Even though `Field`
is called from two different places, the follow set is the same in both
locations: `','|'\n'|'\r'|EOF`. So `nongreedy` works reliably in this example
because it makes no difference which context `Field` was called from.

Reference: APIs called by LLLPG
-------------------------------

Here's the list of methods that LLLPG expects to exist. The `MatchRange`/`MatchExceptRange` methods are only used in lexers though, and `EOF` is only used in parsers (lexers refer to EOF as -1):

	// Note: the set type is expected to contain a Contains(MatchType) method.
	static HashSet<MatchType> NewSet(params MatchType[] items);
	static HashSet<MatchType> NewSetOfRanges(params MatchType[] ranges);

	LaType LA0 { get; }
	LaType LA(int i);
	static const LaType EOF;

	void Error(int lookaheadIndex, string message);

	// Normal matching methods
	void Skip();
	Token MatchAny();
	Token Match(MatchType a);
	Token Match(MatchType a, MatchType b);
	Token Match(MatchType a, MatchType b, MatchType c);
	Token Match(MatchType a, MatchType b, MatchType c, MatchType d);
	Token Match(HashSet<MatchType> set);
	Token MatchRange(int aLo, int aHi);
	Token MatchRange(int aLo, int aHi, int bLo, int bHi);
	Token MatchExcept();
	Token MatchExcept(MatchType a);
	Token MatchExcept(MatchType a, MatchType b);
	Token MatchExcept(MatchType a, MatchType b, MatchType c);
	Token MatchExcept(MatchType a, MatchType b, MatchType c, MatchType d);
	Token MatchExcept(HashSet<MatchType> set);
	Token MatchExceptRange(int aLo, int aHi);
	Token MatchExceptRange(int aLo, int aHi, int bLo, int bHi);

	// Used to verify and-predicates in the matching stage
	void Check(bool expectation, string expectedDescr);

	// For backtracking (used by generated Try_Xyz() methods)
	struct SavePosition : IDisposable
	{
		public SavePosition(Lexer lexer, int lookaheadAmt);
		public void Dispose();
	}
	
	// For recognizers (used by generated Scan_Xyz() methods)
	bool TryMatch(MatchType a);
	bool TryMatch(MatchType a, MatchType b);
	bool TryMatch(MatchType a, MatchType b, MatchType c);
	bool TryMatch(MatchType a, MatchType b, MatchType c, MatchType d);
	bool TryMatch(HashSet<MatchType> set);
	bool TryMatchRange(int aLo, int aHi);
	bool TryMatchRange(int aLo, int aHi, int bLo, int bHi);
	bool TryMatchExcept();
	bool TryMatchExcept(MatchType a);
	bool TryMatchExcept(MatchType a, MatchType b);
	bool TryMatchExcept(MatchType a, MatchType b, MatchType c);
	bool TryMatchExcept(MatchType a, MatchType b, MatchType c, MatchType d);
	bool TryMatchExcept(HashSet<MatchType> set);
	bool TryMatchExceptRange(int aLo, int aHi);
	bool TryMatchExceptRange(int aLo, int aHi, int bLo, int bHi);

The following data types are parameters that you can change:

- `LaType`: the data type of LA0 and LA(i). This is always `int` in lexers, but in parsers you can use the `laType(...)` option (documented in the previous article) to change this type.
- `MatchType`: the data type of arguments to `Match`, `MatchExcept`, `TryMatch` and `TryMatchExcept`. In lexers, `MatchType` is always `int`. In parsers, by default, LLLPG generates code as though `MatchType` is same as `LaType`, but `BaseParser` uses `int` instead for performance reasons. Consequently, when using `BaseParser` you need to use the `matchType(int)` option to change `MatchType` to `int`.
- `HashSet<MatchType>` is the declared data type of large sets. By default this is `HashSet<int>` but you can change it using the `setType(...)` option.
- `Token` is the return value of the `Match` methods. LLLPG does not care and does not need to know what this type is. In lexers, these methods should return the character that was matched, and in parsers they should return the token that was matched (if the match fails, BaseLexer and BaseParser still return the character or token, whatever it was.)

And now, here's a brief description of the APIs, with examples.

### NewSet, NewSetOfRanges ###
	
	static HashSet<MatchType> NewSet(params MatchType[] items);
	static HashSet<MatchType> NewSetOfRanges(params MatchType[] ranges);

These are used for large sets, when it would be inappropriate to generate an expression or `Match` call.

	// Example:
	LLLPG(lexer) {
	  rule Vowel @[ 'a'|'e'|'i'|'o'|'u'|'A'|'E'|'I'|'O'|'U' ];
	  rule MaybeHexDigit @[ ['0'..'9'|'a'..'f'|'A'..'F']? ];
	};
	
	// Generated code:
	static readonly HashSet<int> Vowel_set0 = NewSet(
		'A', 'E', 'I', 'O', 'U', 'a', 'e', 'i', 'o', 'u');
	void Vowel()
	{
	  Match(Vowel_set0);
	}
	static readonly HashSet<int> MaybeHexDigit_set0 = 
		NewSetOfRanges('0', '9', 'A', 'F', 'a', 'f');
	void MaybeHexDigit()
	{
	  int la0;
	  la0 = LA0;
	  if (MaybeHexDigit_set0.Contains(la0))
		 Skip();
	}

### LA0, LA(i) ###

	LaType LA0 { get; }
	LaType LA(int i);

LLLPG assumes that there is a state variable somewhere that tracks the "current input position"; the current position is usually called `InputPosition` but LLLPG never refers to it directly. `LA0` returns the character or token at the current position, and `LA(i)` returns the character or token at `InputPosition + i`.

Obviously, a single function `LA(i)` would have been enough, but `LA(0)` is used much more often than `LA(i)` so I decided to define an extra API which gives implementations an opportunity to optimize access to `LA0`. But in case `LA0` and `LA(i)` are nontrivial, LLLPG also caches the value of `LA0` or `LA(i)` in a local variable.

	// Example:
	LLLPG(parser) {
	  token OptionalIndefiniteArticle @[ ('a' 'n' / 'a')? ];
	};
	
	// Generated code:
	void OptionalIndefiniteArticle()
	{
	  int la0, la1;
	  la0 = LA0;
	  if (la0 == 'a') {
		 la1 = LA(1);
		 if (la1 == 'n') {
			Skip();
			Skip();
		 } else
			Skip();
	  }
	}

### EOF (parsers only) ###

	static const LaType EOF;

Occasionally LLLPG needs to check for EOF. For example, the default follow set of a rule is EOF, and when using `NoDefaultArm`, LLLPG may check whether LA0==EOF to see if an error occurred.

	[NoDefaultArm] LLLPG(parser) {
	  rule AllBs @[ 'B'* ];
	};

	void AllBs()
	{
	  int la0;
	  for (;;) {
		 la0 = LA0;
		 if (la0 == 'B')
			Skip();
		 else if (la0 == EOF)
			break;
		 else
			Error(0, "In rule 'MaybeB', expected one of: ('B'|EOF)");
	  }
	}

In lexers, LLLPG uses `-1` instead of `EOF`.

### Error(i, msg) ###

	void Error(int lookaheadIndex, string message);

This method is called by the default error branch with an auto-generated message, as shown in the example above. `lookaheadIndex` is the offset (`LA(lookaheadIndex)`) where the unexpected character or token was encountered (usually 0). Currently, the error message cannot be customized.

### Skip(), MatchAny() ###

	void Skip();
	Token MatchAny();

Both of these methods advance the current position by one character or token. `Skip()` is called when the return value will not be used, while `MatchAny()` is called if the return value is saved.

	// Example
	LLLPG(lexer) {
	  rule WhateverB @[ (_|EOF) [b='B']? ];
	}

	// Generated code
	void WhateverB()
	{
	  int la0;
	  Skip();
	  la0 = LA0;
	  if (la0 == 'B')
		 b = MatchAny();
	}

### Match ###

	Token Match(MatchType a);
	Token Match(MatchType a, MatchType b);
	Token Match(MatchType a, MatchType b, MatchType c);
	Token Match(MatchType a, MatchType b, MatchType c, MatchType d);
	Token Match(HashSet<MatchType> set);

Ensures that `LA0` matches the argument(s) given to `Match`, taking any appropriate action (printing an error message or throwing an exception) if `LA0` does not match the argument(s). Then `LA0` is "consumed", meaning that the input position is increased by one. LLLPG does not care about the return type, but the return value is used in expressions like `zero:='0'` (see example).

	// Example
	LLLPG(lexer) {
	  rule FiveEvenDigits @[ 
	    zero:='0' ('0'|'2') ('0'|'2'|'4') ('0'|'2'|'4'|'6') ('0'|'2'|'4'|'6'|'8')
	  ];
	}

	// Generated code
	static readonly HashSet<int> FiveEvenDigits_set0 = NewSet('0', '2', '4', '6', '8');
	void FiveEvenDigits()
	{
	  var zero = Match('0');
	  Match('0', '2');
	  Match('0', '2', '4');
	  Match('0', '2', '4', '6');
	  Match(FiveEvenDigits_set0);
	}

### MatchRange (lexers only) ###

	Token MatchRange(int aLo, int aHi);
	Token MatchRange(int aLo, int aHi, int bLo, int bHi);
	
Matches `LA0` against a range of characters, then increases the input position by one.

	// Example
	LLLPG(lexer) {
	  rule LetterDigit @[ ('a'..'z'|'A'..'Z') '0'..'9' ]; 
	}

	// Generated code
	void LetterDigit()
	{
	  MatchRange('A', 'Z', 'a', 'z');
	  MatchRange('0', '9');
	}

### MatchExcept ###

	Token MatchExcept();
	Token MatchExcept(MatchType a);
	Token MatchExcept(MatchType a, MatchType b);
	Token MatchExcept(MatchType a, MatchType b, MatchType c);
	Token MatchExcept(MatchType a, MatchType b, MatchType c, MatchType d);
	Token MatchExcept(HashSet<MatchType> set);

Ensures that `LA0` does **not** match the argument(s) given to `MatchExcept`, taking any appropriate action (printing an error message or throwing an exception) if `LA0` matches the argument(s). Then `LA0` is "consumed", meaning that the input position is increased by one.

In addition, all overloads except the last one must test that `LA0` is not `EOF`. This rule makes `MatchExcept()` (with no arguments) different from `MatchAny()` which does allow `EOF`.

When a set is passed to `MatchExcept`, that set will explicitly contain EOF when EOF is _not_ allowed.

	// Example (remember that _ does NOT match EOF)
	LLLPG(parser) {
	  rule MatchExcept @[ _ ~A ~(A|B) ~(A|B|C) ~(A|B|C|D)
	                      ~(A|B|C|D|E) (~E | EOF) ]; 
	}

	// Generated code
	static readonly HashSet<int> NotA_set0 = NewSet(A, B, C, D, E, EOF);
	static readonly HashSet<int> NotA_set1 = NewSet(E);
	void MatchExcept()
	{
	  MatchExcept();
	  MatchExcept(A);
	  MatchExcept(A, B);
	  MatchExcept(A, B, C);
	  MatchExcept(A, B, C, D);
	  MatchExcept(NotA_set0);
	  MatchExcept(NotA_set1);
	}

### MatchExceptRange (lexers only) ###

	Token MatchExceptRange(int aLo, int aHi);
	Token MatchExceptRange(int aLo, int aHi, int bLo, int bHi);

Verifies that `LA0` is not within the specified range(s) of characters, then increases the input position by one.

	// Example
	LLLPG(lexer) {
	  rule NotInRanges @[ ~('0'..'9') ~('a'..'z'|'A'..'Z') ]; 
	}

	// Generated code
	void NotInRanges()
	{
	  MatchExceptRange('0', '9');
	  MatchExceptRange('A', 'Z', 'a', 'z');
	}

### Check ###

	void Check(bool expectation, string expectedDescr);

As explained in the section §"Error handling mechanisms in LLLPG" (part 3), this is called to check and-predicate conditions during matching if they were not verified during prediction.

	// Example
	LLLPG(lexer) {
		token DosEquis @[ &!{condition} 'X' 'X' ]; 
	}
	
	// Generated code	
	void DosEquis()
	{
	  Check(!condition, "!(condition)");
	  Match('X');
	  Match('X');
	}

### SavePosition ###

	struct SavePosition : IDisposable
	{
		public SavePosition(Lexer lexer, int lookaheadAmt);
		public void Dispose();
	}

This is used for backtracking. `SavePosition` must save the current input position in its constructor, then restore it in `Dispose()`.

	// Example
	LLLPG(lexer) {
		token JustOneCapital @[ 'A'..'Z' &!('A'..'Z') ]; 
	}

	// Generated code
	void JustOneCapital()
	{
	  MatchRange('A', 'Z');
	  Check(!Try_JustOneCapital_Test0(0), "!([A-Z])");
	}
	private bool Try_JustOneCapital_Test0(int lookaheadAmt)
	{
	  using (new SavePosition(this, lookaheadAmt))
		 return JustOneCapital_Test0();
	}
	private bool JustOneCapital_Test0()
	{
	  if (!TryMatchRange('A', 'Z'))
		 return false;
	  return true;
	}

### TryMatch ###

	bool TryMatch(MatchType a);
	bool TryMatch(MatchType a, MatchType b);
	bool TryMatch(MatchType a, MatchType b, MatchType c);
	bool TryMatch(MatchType a, MatchType b, MatchType c, MatchType d);
	bool TryMatch(HashSet<MatchType> set);

Tests whether `LA0` matches the argument(s) given to `TryMatch`. Returns true if `LA0` is a match and false if not. The input position is increased by one.

	// Example
	LLLPG(lexer) {
		[recognizer { bool ScanFiveEvenDigits(); }]
		rule FiveEvenDigits @[ 
			zero:='0' ('0'|'2') ('0'|'2'|'4') ('0'|'2'|'4'|'6') ('0'|'2'|'4'|'6'|'8')
		];
	}

	// Generated code
	static readonly HashSet<int> FiveEvenDigits_set0 = NewSet('0', '2', '4', '6', '8');
	void FiveEvenDigits()
	{
	  var zero = Match('0');
	  Match('0', '2');
	  Match('0', '2', '4');
	  Match('0', '2', '4', '6');
	  Match(FiveEvenDigits_set0);
	}
	bool Try_ScanFiveEvenDigits(int lookaheadAmt)
	{
	  using (new SavePosition(this, lookaheadAmt))
		 return ScanFiveEvenDigits();
	}
	bool ScanFiveEvenDigits()
	{
	  if (!TryMatch('0'))
		 return false;
	  if (!TryMatch('0', '2'))
		 return false;
	  if (!TryMatch('0', '2', '4'))
		 return false;
	  if (!TryMatch('0', '2', '4', '6'))
		 return false;
	  if (!TryMatch(FiveEvenDigits_set0))
		 return false;
	  return true;
	}

### TryMatchRange (lexers only) ###

	bool TryMatchRange(int aLo, int aHi);
	bool TryMatchRange(int aLo, int aHi, int bLo, int bHi);
	
Tests whether `LA0` matches one or two ranges of characters. Returns true if `LA0` is a match and false if not. The input position is increased by one.

	// Example
	LLLPG(lexer) {
		[recognizer { bool ScanLetterDigit(); }]
		rule LetterDigit @[ ('a'..'z'|'A'..'Z') '0'..'9' ]; 
	}
	
	// Generated code
	void LetterDigit()
	{
	  MatchRange('A', 'Z', 'a', 'z');
	  MatchRange('0', '9');
	}
	bool Try_ScanLetterDigit(int lookaheadAmt)
	{
	  using (new SavePosition(this, lookaheadAmt))
		 return ScanLetterDigit();
	}
	bool ScanLetterDigit()
	{
	  if (!TryMatchRange('A', 'Z', 'a', 'z'))
		 return false;
	  if (!TryMatchRange('0', '9'))
		 return false;
	  return true;
	}

### TryMatchExcept ###

	bool TryMatchExcept();
	bool TryMatchExcept(MatchType a);
	bool TryMatchExcept(MatchType a, MatchType b);
	bool TryMatchExcept(MatchType a, MatchType b, MatchType c);
	bool TryMatchExcept(MatchType a, MatchType b, MatchType c, MatchType d);
	bool TryMatchExcept(HashSet<MatchType> set);

Tests whether `LA0` matches the argument(s) given to `TryMatch`. Returns **false** if `LA0` is a match and true if not. The input position is increased by one.

In addition, all overloads except the last one must test that `LA0` is not `EOF`. This rule makes `TryMatchExcept()` (with no arguments) different from `Skip()` which does allow `EOF`.

**Note**: as I write this, it occurs to me that these APIs are redundant. LLLPG could have called `TryMatch(...)` instead and inverted the return value. Should this API be removed in a future version?

	// Example (remember that _ does NOT match EOF)
	LLLPG(parser) {
	  [recognizer { bool TryMatchExcept(); }]
	  rule MatchExcept @[ _ ~A ~(A|B) ~(A|B|C) ~(A|B|C|D)
	                      ~(A|B|C|D|E) (~E | EOF) ]; 
	}

	// Generated code
	static readonly HashSet<int> MatchExcept_set0 = NewSet(A, B, C, D, E, EOF);
	static readonly HashSet<int> MatchExcept_set1 = NewSet(E);
	void MatchExcept()
		{ /* Omitted for brevity */ }
	bool Try_TryMatchExcept(int lookaheadAmt)
	{
	  using (new SavePosition(this, lookaheadAmt))
		 return TryMatchExcept();
	}
	bool TryMatchExcept()
	{
	  if (!TryMatchExcept())
		 return false;
	  if (!TryMatchExcept(A))
		 return false;
	  if (!TryMatchExcept(A, B))
		 return false;
	  if (!TryMatchExcept(A, B, C))
		 return false;
	  if (!TryMatchExcept(A, B, C, D))
		 return false;
	  if (!TryMatchExcept(MatchExcept_set0))
		 return false;
	  if (!TryMatchExcept(MatchExcept_set1))
		 return false;
	  return true;
	}

### TryMatchExceptRange (lexers only) ###

	bool TryMatchExceptRange(int aLo, int aHi);
	bool TryMatchExceptRange(int aLo, int aHi, int bLo, int bHi);

Tests whether `LA0` matches one or two ranges of characters. Returns **false** if `LA0` is a match and **true** if not. The input position is increased by one.

I'll skip the example this time: I think by now you get the idea.

Reference: things you must do when overriding `BaseLexer` and `BaseParser`
--------------------------------------------------------------------------

### For lexing ###

The typical base class for lexing is `BaseLexer`, but you can specialize it as `BaseLexer<UString>`, which should (in theory) give higher perfomance if your input is always a string.

In either case, you must call `AfterNewline()` whenever you encounter a newline `('\n' | '\r' '\n'?)` so that the `LineNumber` property is increased by one. `BaseLexer` also contains its own `Newline` rule, which you can incorporate into your lexer with

	// 'extern' suppresses code generation, so the code is inherited 
	// from BaseLexer, and `'\r' '\n'? | '\n'` tells LLLPG what it does.
	extern token Newline @{ '\r' '\n'? | '\n' };

`BaseLexer` additionally records the locations of all line breaks in its `SourceFile` property (which is `protected`) so you can call `SourceFile.IndexToLine(i).Line` to get the line number of any character that has been tokenized so far; _this only works properly if you call `AfterNewline` consistently_.

When LLLPG was first released, you had to override the `abstract` error handler:

~~~csharp
protected abstract void Error(int lookaheadIndex, string message);
~~~

But now a default error handler is provided that throws `LogException`, and the normal way to change how errors are reported is _not_ to override `Error()`, but instead to set the `ErrorSink` property, e.g. this causes errors to be printed to the terminal:

~~~csharp
base.ErrorSink = Loyc.MessageSink.Console;
~~~

### For parsing ###

When LLLPG was first released, you were expected to use `BaseParser` as your base class, which was tedious because you had to override all these methods:

	protected abstract Int32 EofInt();
	protected abstract Int32 LA0Int { get; }
	protected abstract Token LT(int i);
	protected abstract void Error(int li, string message);
	protected abstract string ToString(Int32 tokenType);

Only one of the above APIs are required by LLLPG itself; the others help `BaseParser` implement the other APIs. In addition to the above, you had to implement the following APIs that are required by LLLPG and not provided by `BaseParser`:

	// (typical implementation shown)
	const TokenType EOF = TokenType.EOF;
	TokenType LA0 { get { return LT0.Type(); } }
	TokenType LA(int offset) { return LT(offset).Type(); }

Because using `BaseParser` was cumbersome, [`BaseParserForList<Token,MatchType>`](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1BaseParserForList_3_01Token_00_01MatchType_01_4.html) was introduced (and its specialized form `BaseParserForList<Token,MatchType,List>`). `BaseParserForList<Token,MatchType>` manages the list of tokens itself - any list that implements `IList<Token>` is acceptable, and the derived class constructor must pass a list of tokens to the base class, along with a token that represents EOF, and an [`ISourceFile`](http://ecsharp.net/doc/code/interfaceLoyc_1_1Syntax_1_1ISourceFile.html) (which you can get from the `SourceFile` property of `BaseLexer`):

    protected BaseParserForList(IList<Token> list, Token eofToken, ISourceFile file, int startIndex=0);

`BaseParserForList` only requires you to implement a single `abstract` method, to convert `MatchType` to a string. `MatchType` is usually `int` in practise, so your implementation might look like this (if `TokenType` is the name of your token type enum):

	protected override string ToString(int tokenType)
	{
	    return ((TokenType)tokenType).ToString();
	}

All the base classes have an `InputPosition` property. `BaseLexer` caches the current character in `LA0` when `InputPosition` changes, while `BaseParser` caches the current token in `LT0` when `InputPosition` changes.

End of Part 4
-------------

With part four, I've almost finished writing the documentation of LLLPG. So that just leaves...

- Advanced techniques: tree parsing, keyword parsing, collapsing many precedence levels into a single rule, and other tricks used by the EC# parser.
- Things you can do with LeMP: other source code manipulators besides LLLPG.
- A call for volunteers to help me build Enhanced C#.

Stay tuned.
