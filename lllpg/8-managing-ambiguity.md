---
title: "8. Managing ambiguity"
layout: article
date: 30 May 2016
toc: true
---

Ambiguity: introduction
-----------------------

In the context of a parser generator, _Ambiguity_ refers to the situation in which, for a particular grammar, there can exist more than one potential parse tree for an input. Programming languages are designed to be less ambiguous than human languages, but their grammars generally do have ambiguities anyway. Some ambiguities are normal and expected and there is a standard "solution" to the problem, while others may be unique to the language you are parsing.

There are various kinds of ambiguities that I will illustrate by way of actual published newspaper headlines...

- **"Milk Drinkers are Turning to Powder."** 
- **"Red tape holds up new bridge."**
- **"Kids Make Nutritious Snacks."**
- **"Prostitutes Appeal to Pope."**
- **"Stolen painting Found by Tree."**

These sentences are ambiguous because of particular words or phrases that have multiple meanings: "turning", "holds up", "red tape", "make", "appeal" and "by". Ambiguities of this sort are easily avoided by giving words and symbols only one meaning. You can safely define multiple meanings, though, if you define a unique structure or context for each meaning, e.g. in C# there are two `using` statements, but in one case `using` is _always_ followed by `(` and in the other case it is _never_ followed by `(`. Plus, one only appears at the beginning of a file and the other never appears at the beginning of a file.

- **"Squad Helps Dog Bite Victim."**
- **"Complaints About NBA Referees Growing Ugly"**
- **"Quarter of a Million Chinese Live on Water"**

A lot of ambiguities in newspaper headlines are caused by the omission of "is" or "are" or a definite or indefinite article (the, a, an), or by the reader's _expectation_ of an omitted word. The problem exists only in certain languages such as English, and is easily avoided in programming languages by including sufficient redundancy to prevent ambiguity; for example, if the semicolons between the clauses of a "for" loop were not required, a loop like `for (i = j + 1; -i > k; ++i)` would become ambiguous because `for (i = j + 1 -i > k ++i)` could be parsed in several ways.

- **"Hospitals are Sued by 7 Foot Doctors."**
- **"Hershey Bars Protest."**

In these cases, the sentences permit different parts of speech for certain words, which allows different sentences structures to emerge, causing ambiguity. In programming languages, an example of a similar ambiguity arises if you define

1. An operator that can be prefix or infix, e.g. `-`: `-x` or `x-y`
2. A different operator that can be suffix or infix, e.g. `*`: `x*` or `x*y`

These operators are perfectly unambiguous by themselves; a problem arises only when you combine them in a certain way. Specifically, the expression `x * - y` is ambiguous, as it could be parsed `x * (- y)` or `(x *) - y`. For this reason, if a real-life programming language contains a suffix operator, that operator is usually not an infix operator also. C++, somewhat famously, violates this rule with the pointer/multiplication operator `*`: `X * Y;` could be interpreted in two ways: it may define a variable of type `X*` named `Y`, or it may invoke an overloaded `operator*` function (just as `cout >> Y;` is a valid statement that calls `operator>>`). C# doesn't have exactly the same problem because, for example, `cout >> Y;` is always illegal in C#; however, you can tell that the C# parser understands the expression because it reports the following error:

> error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement

In order to report this error and _only_ this error, the C# parser still detects that `cout >> Y` is a valid expression. You can see this because the error messages _change_ for other invalid input like `x */ Y;` or `<< hello there >>;`. Thus, in C# the same ambiguity exists at the parser level and is solved in a similar manner as in C++, by giving higher priority to the "variable declaration" interpretation than the "expression" interpretation.

- **"Include your Children When Baking Cookies."**

Here, the sentence is _unambiguous_ from a parsing perspective, but has incomplete information: there are two different ways the children could be "included" in the process and I'm not sure I would trust a computer to choose an interpretation! This is not a parsing issue; the sentence can be parsed unambiguously, yet its meaning is ambiguous. I suppose an analogy in computer languages would be this C# code:

	class A { public virtual void F(Foo x) {...} }
	class B : A {
		public override void F(Foo x) {...} // first
		public          void F(Bar x) {...} // second
	}

If I write `new B().F(new Foo())`, will the compiler call the first or second method? In fact, under certain circumstances, a C# compiler will call the second method (brownie points go to the first person to explain why).

- **"Enraged Cow Injures Farmer With an Axe."**

Here, the issue is that the "With" clause can attach to either "Farmer" or "Injures" (or is it "Cow"?) In computer languages, this type of problem is generally solved _precedence rules_ and _parenthesis_. In LL(k) parsers, precedence rules are typically expressed by creating a rule for every precedence level, plus an innermost rule for parenthesis, identifiers and literals, which I like to call `Atom`. So if you want these four precedence levels:

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
		public rule List<Tok> Start @{
			{var ts = new List<Tok>();} ts+=Token* EOF {return ts;}
		};
		token Tok Token() {
			// BTW: Instead of writing a @{...} block with {...} actions inside,
			// LLLPG lets you write a {...} block with @{...} blocks inside. But 
			// currently the @{...} blocks must be at the top level of the method,
			// not nested inside anything else such as a try {...} region.
			Token t;
			@{ t=Spaces | t=Id | t=Int | t=Op };
			return t;
		}
		token Tok Spaces @{ (' '|'\t')+      {return new Token(...);} };
		token Tok Id     @{ ('a'..'z'|'A'..'Z') ('a'..'z'|'A'..'Z'|'0'..'9')+
		                                     {return new Token(...);} };
		token Tok Int    @{ ('0'..'9')+      {return new Token(...);} };
		token Tok Op     @{ op:=('+'|'-'|'*'|'/'|'=')+  
		                                     {return new Token(...);} };
	}

The thing is, any lexer grammar that follows this pattern is ambiguous! Because of the loop in `Start`, an identifier like "`ab3`" could theoretically be parsed in four different ways: as a single Id "`ab3`", as two Ids "`a`" "`b3`", as two tokens "`ab`" "`3`", or as three tokens "`a`" "`b`" "`3`".

So if all the rules are written using `rule` rather than `token`, LLLPG will report three ambiguities, one each for `Id` or `Num` and `Spaces`:

- Warning: (12,23): Loyc.LLParserGenerator.Macros.run_LLLPG: Alternatives (1, exit) are ambiguous for input such as « *» ([\t ], [\$\t *+\-/-9=A-Za-z])
- Warning: (13,43): Loyc.LLParserGenerator.Macros.run_LLLPG: Alternatives (1, exit) are ambiguous for input such as «0*» ([0-9A-Za-z], [\$\t *+\-/-9=A-Za-z])
- Warning: (15,23): Loyc.LLParserGenerator.Macros.run_LLLPG: Alternatives (1, exit) are ambiguous for input such as «0*» ([0-9], [\$\t *+\-/-9=A-Za-z])

LLLPG detects the ambiguity while looking at the _follow set_ of each rule, which it does while analyzing the loops. Since `Token` appears in a loop, `Id` can theoretically be followed by another `Id`, or by `Num`, so the location where an `Id` or `Num` or `Spaces` loop should _stop_ is ambiguous.

I created `token` mode to avoid warnings like this and the potentially complex analysis that produces them. `token` replaces the follow set of a rule with `_*` (which means "anything"), and then it suppresses the inevitable ambiguity warning (because a decision between `_*` and anything else always ambiguous.) The mode is called `token` because it should be used on rules that describe tokens, but occasionally it is useful in other contexts (so you can use it in a parser if it turns out that you want LLLPG to ignore the rule's follow set).

When I write a lexer, I mark the top-level token rules with `token`, and I use `rule` for the sub-rules that are called by `token`s.

Managing ambiguity, part 2: LLLPG's missing feature
---------------------------------------------------

In certain ambiguous cases, notably those in which some alternatives are prefixes of others, some parser generators (including ANTLR) have the ability to select the _longest match_ automatically. LLLPG does not have this ability, and you'll notice this problem when writing rules for operators:

~~~csharp
token CompareOp() @{ '>' | '<' | '=' | ">=" | "<=" };
~~~

The output is:

- Warning: (4,23): Loyc.LLParserGenerator.Macros.run_LLLPG: Alternatives (1, 2) are ambiguous for input such as «>=» ([>], [=])
- Warning: (4,23): Loyc.LLParserGenerator.Macros.run_LLLPG: Alternatives (1, 3) are ambiguous for input such as «<=» ([<], [=])
- Warning: (4,23): Loyc.LLParserGenerator.Macros.run_LLLPG: Branches 2, 3 are unreachable.

~~~csharp
    void CompareOp()
    {
        MatchRange('<', '>');
    }
~~~

Please excuse the strange numbering scheme in the error message: LLLPG actually interprets `'>' | '<' | '=' | ">=" | "<="` as `('>' | '<' | '=') | ">=" | "<="`, unifying the three single characters into a set as an optimization, and it turns out that `('>' | '<' | '=')` is equivalent to `'<'..'>'`, which explains where `MatchRange('<', '>')` comes from.

What's going on here? Well, if the input is '`<=`' then '`<`' matches just as well as '`<=`', as far as LLLPG is concerned. And because it is listed first, LLLPG gives it higher priority. So '`<=`' is unreachable because '`<`' takes priority, and '`>=`' is unreachable because '`>`' takes priority.

You can easily fix this by always listing longer operators first:

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

	token CompareOp() @{ ">=" | "<=" | '>' | '<' | '=' };

can be suppressed by switching '`|`'s to '`/`'s:

	token CompareOp() @{ ">=" / "<=" / '>' / '<' | '=' };

'`/`' is transitive, so in this example, the ambiguity between `">="` and `'>'` is suppressed and likewise between `"<="` and `'<'`. Note that the following will _not_ suppress the warnings:

	token CompareOp() @{ ">=" / "<=" | '>' / '<' | '=' };

(Footnote: in fact this would have suppressed the warnings in earlier versions of LLLPG, but the logic has changed. I will describe only the new rules.)

Earlier I said that LLLPG "doesn't care much about parenthesis", so that, for instance,

	rule Foo @{ ["AB" | "A" | "CD" | "C"]*     };

is equivalent to 

	rule Foo @{ [("AB" | "A") | ("CD" | "C")]* };

That's true, but as of version 1.1.0 it will now pay attention to the relationship between the slash operator and parenthesis. In

	token CompareOp() @{ ">=" / "<=" | '>' / '<' | '=' };

LLLPG is instructed to suppress warnings between `">=" / "<="` and `'>' / '<'`, but that's all. '`/`' has higher precedence than '`|`', so this is equivalent to 

	token CompareOp() @{ (">=" / "<=") | ('>' / '<') | '=' };

And the '`|`' operator causes warnings between the two groups (`">=" / "<="` and `'>' / '<'`) to be permitted. If you write 

	token CompareOp() @{ ">=" | "<=" / '>' | '<' | '=' };

no warnings are suppressed either, but if you now add parenthesis:

	token CompareOp() @{ (">=" | "<=") / ('>' | '<') | '=' };

then the warnings are suppressed, because there is now a slash separating `">="` / `'>'` and `"<="` / `'<'`.

You may remember from [Section 3](3-parsing-terminology.md) that slash is the alt-separator in PEGs. In LLLPG, the slash operator works similarly, but not quite the same. In fact, `/` always yields the same code as `|`. Here's an example where a PEG would parse differently from LLLPG:

    rule abc @{ ('a' / 'a' 'b') 'c' };

In a PEG (correct me if I'm wrong), the first branch always takes priority and the second branch is unreachable. If the input is `abc`, a PEG will not backtrack and try the `'a' 'b'` branch because the first branch was matched successfully. An LL(k) parser generator, however, performs prediction on the first `k` characters, even if those characters come after the set of alternatives under consideration, which means the `'c'` influences code generation, producing the following code (by default):

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

      private rule IfStmt @{ 
        "if" "(" Expr ")" Stmt greedy("else" Stmt)?
      };
      private rule Stmt @{ 
        IfStmt | OtherStmt | Expr ";" | ...
      };
  
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
  
By the way, the formal way of describing this limitation of LLLPG's behavior is 
to say that LLLPG supports only ["strong" LL(k) grammars](http://slkpg.byethost7.com/llkparse.html), 
not "general" LL(k) grammars (this is true even when you use `FullLLk(true)`).  
  
So at the end of a rule, LLLPG makes decisions based on all possible contexts
of that rule, rather than the actual context. Consequently, `nongreedy` is not
as useful as it could be. However, `nongreedy` still has its uses. Good
examples include strings and comments:

      token TQString @{ "'''" (nongreedy(_))* "'''" };
      token MLComment @{ "/*" (nongreedy(MLComment / _))* "*/" };

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
this, simply add an `[LL(3)]` attribute to the rule. No warning was printed
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

      public token MLCommentLine(ref nested::int)::bool @{
        (greedy
          ( &{nested>0} "*/" {nested--;}
          / "/*" {nested++;}
          / ~('\r'|'\n'|'*')
          / '*' (&!'/')
          ))*
        (Newline {return false;} | "*/" {return true;})
       };

Here, I switched back to a greedy loop and added `'*'` as its own branch with an
extra check to make sure `'*'` is not followed by `'/'`. If the test `&!'/'`
succeeds, the new fourth branch matches the `'*'` character (but not the
character afterward); otherwise the loop exits. I could have also written it
like this, with only three branches:

      public token MLCommentLine(ref nested::int)::bool @{ 
        (greedy
          ( &{nested>0} "*/" {nested--;}
          / "/*" {nested++;}
          / (&!"*/") ~('\r'|'\n')
          ))*
        (Newline {return false;} | "*/" {return true;})
      };

However, this version is slower, because LLLPG will actually run the `&!"*/"`
test on every character within the comment.  

Here's one more example using nongreedy:

    // Parsing a comma-separated value file (.csv)
    public rule CSVFile @{ Line* };
    rule Line           @{ Field greedy(',' Field)* (Newline | EOF) };
    rule Newline        @{ ('\r' '\n'?) | '\n' };
    rule Field          @{ nongreedy(_)*
                         | '"' ('"' '"' | nongreedy(~('\n'|'\r'))* '"' };

This grammar describes a file filled with fields separated by commas (plus I
introduced the `EOF` symbol, so that no `Newline` is required at the end of the
last line). Notice that `Field` has the loop `nongreedy(_)*`. How does LLLPG
know to when to break out of the loop? Because it computes the "follow set" or
"return address" of each rule. In this case, `Field` can be followed by
`','|'\n'|'\r'|EOF`, so the loop will break as soon as one of these characters
is encountered. This is different than the `IfStmt` example above in one
important way: `Field` always has the same follow set. Even though `Field`
is called from two different places, the follow set is the same in both
locations: `','|'\n'|'\r'|EOF`. So `nongreedy` works reliably in this example
because it makes no difference which context `Field` was called from.

Next up
-------

Next article in the series: [Advanced Techniques](9-advanced-techniques.html).
