---
title: "9. Advanced techniques"
layout: article
date: 30 May 2016
toc: true
---

If you read this entire article, I guess you'll become an LLLPG master, ready to parse any language your boss throws at you.

`inline` and `extern` rules
---------------------------

LLLPG 1.3 supports "inline" rules, which are rules that are inserted verbatim at the location where they are used. Here is an example:

    LLLPG(lexer);
    
    inline extern rule IdStartChar @[ 'a'..'z'|'A'..'Z'|'_' ];
    inline extern rule IdContChar @[ IdStartChar|'0'..'9' ];
    rule Identifier @[ IdStartChar IdContChar* ];

This produces only a single method as output (`Identifier`), the contents of `IdStartChar` and `IdContChar` having been inlined. 

Meanwhile, the `extern` modifier suppresses code generation for a rule (in this case `IdStartChar` and `IdContChar`). Otherwise, those methods would exist but they wouldn't be called.

Currently, inlining is only allowed on rules that have no arguments and no return value. Inlining is "unsanitary", too; for example, the `inline rule` could contain code that refers to local variables that only exist in the location where inlining occurs. This is not recommended:

    /// Input
    rule Foo @{ digit:'0'..'9' Unsanitary };
    inline rule Unsanitary @{ {Console.WriteLine(digit);} };

    /// Output
    void Foo()
    {
        var digit = MatchRange('0', '9');
        Console.WriteLine(digit);
    }
    void Unsanitary()
    {
        Console.WriteLine(digit);
    }

How to avoid memory allocation in a lexer
-----------------------------------------

Now I will demonstrate how LLLPG can be used when you want to minimize memory allocations. Avoiding memory allocation in a full-blown parser is almost impossible, since you need to allocate memory to hold your syntax tree. But in simpler situations, you can optimize your scanner to avoid creating garbage objects.

The following example parses email addresses without allocating _any_ memory, beyond a single `LexerSource`, which is allocated only once per thread:

    struct EmailAddress
    {
      public EmailAddress(string userName, string domain) 
        { UserName = userName; Domain = domain; }
      public UString UserName;
      public UString Domain;
      public override string ToString() { return UserName + ""@"" + Domain; }

      LLLPG (lexer(inputSource(src), inputClass(LexerSource))) {
        // LexerSource provides the APIs expected by LLLPG. This is
        // static to avoid reallocating the helper object for each email.
        [ThreadStatic] static LexerSource<UString> src;
          
        /// <summary>Parses email addresses according to RFC 5322, not including 
        /// quoted usernames or non-ASCII addresses (TODO: support Unicode).</summary>
        /// <exception cref="FormatException">The input is not a legal email address.</exception>
        public static rule EmailAddress Parse(UString email)
        {
          if (src == null)
            src = new LexerSource<UString>(email, "", 0, false);
          else
            src.Reset(email, "", 0, false); // re-use old object
          
          @[ UsernameChars(src) ('.' UsernameChars(src))* ];
          int at = src.InputPosition;
          UString userName = email.Substring(0, at);
          
          @[ '@' DomainCharSeq(src) ('.' DomainCharSeq(src))* EOF ];
          UString domain = email.Substring(at + 1);
          return new EmailAddress(userName, domain);
        }
        static rule UsernameChars(LexerSource<UString> src) @[
          ('a'..'z'|'A'..'Z'|'0'..'9'|'!'|'#'|'$'|'%'|'&'|'\''|
          '*'|'+'|'/'|'='|'?'|'^'|'_'|'`'|'{'|'|'|'}'|'~'|'-')+
        ];
        static rule DomainCharSeq(LexerSource<UString> src) @[
                 ('a'..'z'|'A'..'Z'|'0'..'9')
          ( '-'? ('a'..'z'|'A'..'Z'|'0'..'9') )*
        ];
      }
    }

This example demonstrates that you can pass the `LexerSource` between rules as a parameter, although it's actually redundant here, and the `src` parameters could be safely removed.

Here's how this example avoids memory allocation:

1. `LexerSource` is allocated only once in a thread-local variable, then re-used by calling `Reset(...)` on subsequent calls. `Reset(...)` takes the same parameters as the contructor.
2. It uses `UString` instead of `string`. `UString` is a `struct` defined in Loyc.Essentials.dll that represents a slice of a string. When this example calls `email.Substring()` it's not creating a new string, it's simply creating a `UString` that refers to part of the `email` string.
3. It uses `LexerSource<UString>` instead of `LexerSource`. Remember that `LexerSource` accepts a reference to `ICharSource`, so if you write `new LexerSource((UString)"string")` you are boxing `UString` on the heap. In contrast, `new LexerSource<UString>((UString)"string")` does not box the `UString`.
4. It uses the four-argument constructor `new LexerSource<UString>(email, "", 0, false)`. The last argument is the important one; by default `LexerSource` allocates a `LexerSourceFile` object (the `LexerSource.SourceFile` property) which keeps track of where the line breaks are located in the file so that you can convert between integer indexes and (Line, Column) pairs. By setting this parameter to `false` you are turning off this feature to avoid memory allocations.

Keyword parsing
---------------

Suppose that we have a language with keywords like `for`, `foreach`, `while`, `if`,  `do`, and `function`. We could write code like this (assuming you've defined an `enum TT` filled with token types):
    
    [k(9)]
    private token TT IdOrKeyword @[
          "do"        {return TT.Do;      }
        / "if"        {return TT.If;      }
        / "for"       {return TT.For;     }
        / "foreach"   {return TT.Foreach; }
        / "while"     {return TT.While;   }
        / "function"  {return TT.Function;}
        / Identifier  {return TT.Id;      } 
    ];
    
    public token ScanNextToken() @[
          Spaces        { return TT.Spaces; } 
        / t:IdOrKeyword
        / t:Operator
        / t:Literal
        / ...
        { return t; }
    ];

This example uses `[k(9)]` to increase the lookahead to 9 (longer than any of the keywords) only inside this rule. Unfortunately, this won't quite work the way you want it to. There are two problems with this example:

1. The `foreach` branch is unreachable, since it will be detected as the keyword `for` followed by `each`.
2. Words like "form", "ifone", and "functionality" will be parsed as a keyword followed by an `Identifier`.

You can solve the first problem by moving the `foreach` branch above the `for` branch, to give it higher priority.

You can solve the second problem by using a gate (`=>`) or zero-width predicate (`&(...)`) to ensure that the keyword is _not_ followed by some other character, like a letter or digit, that would imply it is not a keyword. The generated code will be more efficient if you use a gate instead of a predicate, so my standard solution looks like this:

    [k(/*k must be larger than the longest keyword*/)]
    private token IdOrKeyword @
        [ "first_keyword"  EndId {/* custom action for this keyword */}
        / "second_keyword" EndId {/* custom action for this keyword */}
        / "third_keyword"  EndId {/* custom action for this keyword */}
        / Identifier             {/* custom action for normal identifier */}
        ];
     
    // If a keyword is followed by a letter or number then it is NOT a keyword.
    // So this rule is used to cause LLLPG to verify that there is no letter or
    // number after the keyword. 'inline' ensures that the effect of this rule 
    // internalized to IdOrKeyword, and `extern` suppresses generating the empty 
    // method that would be created for this rule.
    extern inline token EndId @{ (~('a'..'z'|'A'..'Z'|'0'..'9'|'_') | EOF) => };

Actually there is a third problem. Due to limitations of LLLPG, if you have a large number of keywords, LLLPG may take a long time to analyze your grammar. Part of the problem is that `IdOrKeyword` is analyzed more than once: it is analyzed in isolation, and then it is "comparatively analyzed" when generating the code for `ScanNextToken`, as LLLPG must figure out when to call `IdOrKeyword` and when to call some other rule. So you can get a speedup by using a gate to "hide" the `IdOrKeyword` during the anaylsis of `ScanNextToken`, like this:

    public token ScanNextToken() @[
          Spaces        { return TT.Spaces; } 
        / (Id => t:IdOrKeyword)
        / t:Operator
        / t:Literal
        / ...
        { return t; }
    ];

The gate `Id => t:IdOrKeyword` simplifies analysis by saying "if it looks like an identifier, call `IdOrKeyword()` - ignore all the differences between the various branches inside `IdOrKeyword()`".

Collapsing precedence levels into a single rule
-----------------------------------------------

One of the traditional disadvantages of LL(k) parsing is the need for a separate rule for each precedence level when parsing expressions. Consider this **fully operational** example which parses an expression into a Loyc tree:

    class ExprParser : BaseParserForList<StringToken, string>
    {
        public ExprParser(string input) 
            : this(input.Split(' ').Select(word => 
                   new StringToken { Type=word }).ToList()) {}
        public ExprParser(IList<StringToken> tokens, ISourceFile file = null) 
            : base(tokens, default(StringToken), file ?? EmptySourceFile.Unknown) 
            { F = new LNodeFactory(SourceFile); }
        
        protected override string ToString(string tokType) { return tokType; }
        
        LNodeFactory F;
        LNode Op(LNode lhs, StringToken op, LNode rhs) { 
            return F.Call((Symbol)op.Type, lhs, rhs, lhs.Range.StartIndex, rhs.Range.EndIndex);
        }

        LLLPG(parser(laType: string, terminalType: StringToken));

        public rule LNode Expr() @[
            result:Expr1 [ "=" r:=Expr
                           { $result = Op($result, $"=", r); } ]?
        ];
        rule LNode Expr1() @[
            result:Expr2 ( op:=("&&"|"||") r:=Expr2
                           { $result = Op($result, op, r); } )*
        ];
        rule LNode Expr2() @[
            result:Expr3 ( op:=(">"|"<"|">="|"<="|"=="|"!=") r:=Expr3
                           { $result = Op($result, op, r); } )*
        ];
        rule LNode Expr3() @[
            result:Expr4 ( op:=("+"|"-") r:=Expr4 
                           { $result = Op($result, op, r); } )*
        ];
        rule LNode Expr4() @[
            result:PrefixExpr ( op:=("*"|"/"|">>"|"<<") r:=PrefixExpr
                           { $result = Op($result, op, r); } )*
        ];
        rule LNode PrefixExpr() @[
            ( "-" r:=PrefixExpr { $result = F.Call((Symbol)"-", r, 
                                            $"-".StartIndex, r.Range.EndIndex); }
            / result:PrimaryExpr )
        ];
        rule LNode PrimaryExpr() @[
            result:Atom
            (	"(" Expr ")" { $result = F.Call($result, $Expr, $result.Range.StartIndex); }
            |	"." rhs:Atom { $result = F.Dot ($result, $rhs,  $result.Range.StartIndex); }
            )*
        ];
        rule LNode Atom() @[
            "(" result:Expr ")" { $result = F.InParens($result); }
        /	_ { 
                double n; 
                $result = double.TryParse($_.Type, out n) 
                        ? F.Literal(n) : F.Id($_.Type);
            }
        ];
    }

I designed this example to work without a lexer (I really don't recommend this approach, but it keeps the example short). It will accept tokens separated by spaces, so you can test it with code like this:

    Console.WriteLine(new ExprParser("x . Foo ( 0 ) * ( 7.5 + 2.5 ) > 100").Expr());

To make it compile, it just needs a few `using`s and a definition for `StringToken` (see below).

Notice that in the middle of the parser there's a series of `Expr` rules: `Expr1`, `Expr2`, `Expr3` and `Expr4`. In a parser for a "real" language there might be several more. And notice that even when parsing a simple expression like "`42`", the same call stack will alway occur: `Expr`, `Expr1`, `Expr2`, `Expr3`, `Expr4`, `PrefixExpr`, `PrimaryExpr`, `Atom`. That's inefficient. It is straightforward, though, to collapse all the "infix" operators (`ExprN`) into a single rule. This involves an integer that represents the current "precedence floor", and a semantic predicate `&{...}`:

    public rule LNode Expr(int prec = 0) @[
        result:PrefixExpr
        greedy // to suppress ambiguity warning
        (   // Remember to add [Local] when your predicate uses a local variable
            // (Someday I'll make [Local] the default; use [Hoist] for non-local)
            &{[Local] prec <= 10}
            "=" r:=Expr(10)
            { $result = Op($result, $"=", r); }
        |   &{[Local] prec < 20}
            op:=("&&"|"||") r:=Expr(20)
            { $result = Op($result, op, r); }
        |   &{[Local] prec < 30}
            op:=(">"|"<"|">="|"<="|"=="|"!=") r:=Expr(30)
            { $result = Op($result, op, r); }
        |   &{[Local] prec < 40}
            op:=("+"|"-") r:=Expr(40)
            { $result = Op($result, op, r); }
        |   &{[Local] prec < 50}
            op:=("*"|"/"|">>"|"<<") r:=Expr(50)
            { $result = Op($result, op, r); }
        )*
    ];

Here I've multiplied my precedence levels by 10, to make it easy to add more precedence levels in the future (in between the existing ones).

How does it work? Lower values of `prec` represent lower precedence levels, with 0 representing the outermost expression. After matching an operator with a certain precedence level, `Expr` calls itself with a raised "precedence floor", in which low-precedence operators will no longer match, but high-precedence operators still match.

Let's work through the expression "`- 6 * 5 > 4 - 3 - 2`". At first, `Expr(0)` is called, and `PrefixExpr` matches `-6`. At this point, any infix operator can be matched. After matching `*`, `Expr(50)` is called, which matches `5` and then returns (as it cannot match `>`), and `Expr(0)` calls `Op` to create an [`LNode`](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1LNode.html) that represents the subexpression `-6 * 5`. Next, `>` is matched, so `Expr(30)` is called.

`Expr(30)` matches `4`, and then it sees `-` so it checks whether `prec < 40`. This is true, so it calls `Expr(40)`. `Expr(40)` matches `3` and then it sees the second `-`. This time `prec < 40` is _false_ so it returns. `Expr(30)` calls `Op` to create the subexpression `4.0 - 3.0`.

Next, `Expr(30)` sees the second `-` and checks if `prec < 40`, which is _true_ so it matches the second `-` and calls `Expr(40)` which matches `2` and returns. Then `Expr(30)` calls `Op` to create the subexpression `(4.0 - 3.0) - 2.0`. Finally, `Expr(30)` returns, and `Expr(0)` creates the expression tree `(-6 * 5) > ((4.0 - 3.0) - 2.0)`.

Notice the difference between left-associative and right-associative operators:

- For Left-associative (e.g. `4 - 3 - 2` is parsed like `(4 - 3) - 2`), you should call `Expr(N)` and your predicate should check if `prec < N`.
- For right-associative (e.g. `a = b = c` is parsed like `a = (b = c)`), you should call `Expr(N)` and your predicate should check if `prec <= N`.

With some extra effort, you could, for maximum efficiency, merge the `PrefixExpr` and `PrimaryExpr` rules into `Expr` also:

    public rule LNode Expr(int prec = 0) @[
        ( "-" r:=Expr(50) { $result = F.Call((Symbol)"-", r, 
                                      $"-".StartIndex, r.Range.EndIndex); }
        / result:Atom )
        greedy // to suppress ambiguity warning
        (   // Remember to add [Local] when your predicate uses a local variable
            &{[Local] prec <= 10}
            "=" r:=Expr(10)
            { $result = Op($result, $"=", r); }
        |   &{[Local] prec < 20}
            op:=("&&"|"||") r:=Expr(20)
            { $result = Op($result, op, r); }
        |   &{[Local] prec < 30}
            op:=(">"|"<"|">="|"<="|"=="|"!=") r:=Expr(30)
            { $result = Op($result, op, r); }
        |   &{[Local] prec < 40}
            op:=("+"|"-") r:=Expr(40)
            { $result = Op($result, op, r); }
        |   &{[Local] prec < 50}
            op:=("*"|"/"|">>"|"<<") r:=Expr(50)
            { $result = Op($result, op, r); }
        |   "(" Expr ")" // PrimaryExpr
            { $result = F.Call($result, $Expr, $result.Range.StartIndex); }
        |   "." rhs:Atom // PrimaryExpr
            { $result = F.Dot ($result, $rhs,  $result.Range.StartIndex); }
        )*
    ];

Here you can think of `PrimaryExpr` as having a precedence level of 60, but since `prec` never goes that high, there's no need to include a predicate like `&{[Local] prec < 60}` on the last two branches.

It's even possible to merge the last rule, `Atom`, into this rule, but let's not get carried away.

To make this example compile, add the following code above `ExprParser` and ensure your project has references to `Loyc.Syntax.dll`, `Loyc.Collections.dll` & `Loyc.Essentials.dll`:

    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Loyc;
    using Loyc.Syntax;
    using Loyc.Syntax.Lexing;

    struct StringToken : ISimpleToken<string>
    {
        public string Type { get; set; }
        public object Value { get { return Type; } }
        public int StartIndex { get; set; }
    }

Tree parsing
------------

TODO: review. I suspect some of this section is out of date.

In virtually all programming languages, it is possible to insert an intermediate stage between the lexer and parser that groups parentheses, square brackets and curly braces together, to produce a "token tree". The way I've been doing it is to write a normal lexer that translates code like `{ w = (x + y) * z >> (-1); }` into a sequence of token objects

    {  w  =  (  x  +  y  )  *  z  >>  (  -  1  )  ;  }

and then I use a "lexer wrapper" called [`TokensToTree`](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1Lexing_1_1TokensToTree.html) which converts this to a tree with children under the opening brackets, like this:

    {  }
    |
    |
    +--- w  =  (  )  *  z  >>  (  )  ;
               |               |
               |               |
               +--- x  +  y    +---  -  1

A token's children are stored in the Value property as type [TokenTree](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1Lexing_1_1TokenTree.html), which is derived from [`DList<Token>`](http://core.loyc.net/collections/dlist.html) and returned by the [`Children`](http://ecsharp.net/doc/code/structLoyc_1_1Syntax_1_1Lexing_1_1Token.html#a2ddfce45f749139cbd86874638db04f6) property.

Why would you want to do this? There are a couple of reasons:

1. It allows the parser to "instantly" skip past the contents of an expression in parenthesis, to see what comes afterward. Consider the C# expression `(List<T> L) => L.Count`: this is parsed in a completely different way than `(List < T > L) + L.Count`! To avoid the need for unlimited lookahead, I felt that preprocessing into an expression tree was worthwhile in my [EC# parser](https://github.com/qwertie/Loyc/blob/master/Main/Ecs/Parser/EcsParserGrammar.les).
2. Some have found it useful for implementing a [macro system that allows syntax extensions](http://disnetdev.com/papers/sweetjs.pdf).

The preprocessing step itself is simple; you can either use the existing [`TokensToTree`](https://github.com/qwertie/Loyc/blob/master/Core/Loyc.Syntax/Lexing/TokensToTree.cs) class (if your lexer implements [ILexer](http://ecsharp.net/doc/code/interfaceLoyc_1_1Syntax_1_1Lexing_1_1ILexer.html) and produces [Token](http://ecsharp.net/doc/code/structLoyc_1_1Syntax_1_1Lexing_1_1Token.html) structures), or copy and modify the existing code. (In hindsight I think it would have been better to make the closing bracket a child of the opening bracket, because currently LLLPG tends to give error messages about "EOF" when it's not really EOF, it's just the end of a stream of child tokens.)

So how do you use LLLPG with a token tree? Well, LLLPG doesn't directly support token trees, so it will see only the sequence of tokens at the current "level" of the tree, e.g. `w  =  (  )  *  z  >>  (  )  ;`. For example, consider the [LES](https://github.com/qwertie/LoycCore/wiki/Loyc-Expression-Syntax) parser. Normally you invoke it with code like `LesLanguageService.Value.Parse("code")`, but you could construct the full parsing pipeline manually, like this:

    var input = (UString)"{ w = (x + y) * z >> (-1); };";
    var errOut = new ConsoleMessageSink();
    var lexer = new LesLexer(input, "", errOut);
    var tree = new TokensToTree(lexer, true); // <= Convert tokens to tree!
    var parser = new LesParser(tree.Buffered(), lexer.SourceFile, errOut);
    var results = parser.ParseStmtsLazy().Buffered();

Initially the `LesParser` starts at the "top level" of the token tree, and in this example, it sees just two tokens, two braces. In my parser I use two helper functions to navigate into (`Down`) and out of (`Up`) the child trees:

    Stack<Pair<IList<Token>, int>> _parents;

    protected bool Down(IList<Token> children)
    {
        if (children != null) {
            if (_parents == null)
                _parents = new Stack<Pair<IList<Token>, int>>();
            _parents.Push(Pair.Create(TokenList, InputPosition));
            _tokenList = children;
            InputPosition = 0;
            return true;
        }
        return false;
    }
    protected void Up()
    {
        Debug.Assert(_parents.Count > 0);
        var pair = _parents.Pop();
        _tokenList = pair.A;
        InputPosition = pair.B;
    }

(After writing this, I decided to add these methods to [`BaseParserForList`](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1BaseParserForList_3_01Token_00_01MatchType_00_01List_01_4.html) so that you call them from your own parsers if you want.)

In the grammar, parenthesis and braces are handled like this:

    |	// (parens)
        t:=TT.LParen rp:=TT.RParen {e = ParseParens(t, rp.EndIndex);}
    |	// {braces}
        t:=TT.LBrace rb:=TT.RBrace {e = ParseBraces(t, rb.EndIndex);}

For example, `ParseBraces` looks like this - it calls `Down`, invokes `StmtList` which is one of the grammar rules, and finally calls `Up` to return to the previous level of the token tree.

    protected LNode ParseBraces(Token t, int endIndex)
    {
        RWList<LNode> list = new RWList<LNode>();
        if (Down(t.Children)) {
            StmtList(ref list);
            Up();
        }
        return F.Braces(list.ToRVList(), t.StartIndex, endIndex);
    }

The LES parser, of course, produces [Loyc trees](https://github.com/qwertie/LoycCore/wiki/Loyc-trees), which in turn use `RVList`s, which are described in [their own separate article](http://www.codeproject.com/Articles/26171/VList-data-structures-in-C); this function uses `RWList`, a mutable version of `RVList`.

How to parse indentation-sensitive languages
--------------------------------------------

Python uses indentation and newlines to indicate program structure:

    if foo:
      while bar < 100:
        bar *= 2;
    else:
      print("unfoo! UNFOO!")

Newlines generally represent the end of a statement, while colons indicate the beginning of a "child" block. Inside parenthesis, square brackets, or braces, newlines are ignored:

    s = ("this is a pretty long string that I'd like "
      + " to continue writing on the next line")

If you don't use brackets, Python 3 doesn't try to figure out if you "really" meant to continue a statement on the next line:

    # SyntaxError after '+': invalid syntax
    s = "this is a pretty long string that I'd like " + 
        " to continue writing on the next line"

And inside brackets, indentation is ignored, so this is allowed:

    if foo:
        s = ("this is a pretty long string that I'd like "
    + " to continue writing on the next line")
        print(s)

By far the easiest way to handle this kind of language is to insert a preprocessor (postprocessor?) step, after the lexer and before the parser. Loyc.Syntax.dll includes a preprocessor for this purpose, called `IndentTokenGenerator`. Here's how to use it:

1. Use `BaseILexer<CharSrc, Token>` as the base class of your lexer instead of `BaseLexer<CharSrc>` or `BaseLexer`. This will implement the `ILexer<Token>` interface for you, which is required by `IndentTokenGenerator`. As with `BaseLexer`, you're required to call `AfterNewline()` after reading each newline from the file (see [BaseILexer's documentation](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1Lexing_1_1BaseLexer_3_01CharSrc_01_4.html) for details)
2. If you use the standard `Token` type (`Loyc.Syntax.Lexing.Token`), you can wrap your lexer in an `IndentTokenGenerator`, like this:

        /// given class YourLexerClass : BaseILexer<ICharSource,Token> { ... }
        var lexer = new YourLexerClass(input);
        /// IndentTokenGenerator needs a list of tokens that trigger indent tokens 
        /// to be generated, e.g. Colon in Python-like languages.
        var triggers = new[] { (int)YourTokenType.Colon };
        var wrapr = new IndentTokenGenerator(lexer, triggers, 
            new Token((int)YourTokenType.Semicolon, 0, 0, null))
        {
            /// This property specifies triggers that only have an effect when
            /// they appear at the end of a line (they are ignored elsewhere)
            EolIndentTriggers = triggers, 
            /// Tokens that represent indentation and unindent
            IndentToken = new Token((int)YourTokenType.Indent, 0, 0, null),
            DedentToken = new Token((int)YourTokenType.Dedent, 0, 0, null),
        };
        /// LCExt.Buffered() is an extension method that lazily converts an 
        /// IEnumerator<T> or IEnumerator<T> to a list (I've used it because
        /// BaseILexer is an enumerator, so ToList() can't be used directly)
        List<Token> tokens = wrapr.Buffered().ToList();
        var parser = new YourParserClass(tokens);
    
    See the [documentation of `IndentTokenGenerator`](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1Lexing_1_1IndentTokenGenerator.html) for more information; it documents specifically how I'd handle Python, for example.

    If you're not using the standard `Token` type, you can use [`IndentTokenGenerator<Tok>`](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1Lexing_1_1IndentTokenGenerator_3_01Token_01_4.html) instead, you just have to implement its abstract methods. If you need to customize the generator's behavior, you can derive from either of these classes and override their virtual methods.

Shortening your code with LeMP
------------------------------

In LLLPG 1.3 I've finally completed a bunch of basic macro functionality so you can do a bunch of stuff that has nothing to do with parsing. See my new article "[Avoid Tedious Coding With LeMP](http://ecsharp.net/lemp/avoid-tedium-with-LeMP.html)" to learn more.

The new `unroll` and `replace` macros, in particular, are useful for eliminating some of the boilerplate from an LLLPG parser. You'll see these macros in action in some of the [LLLPG samples](http://github.com/qwertie/LLLPG-Samples).

The End
-------

I hope you enjoyed this article series and that you'll use LLLPG for your parsing needs. I haven't earned a penny working on this; all I want is your feedback, and a job on the C# compiler team (well, it's been 30 months... I'm still waiting for a call!)

For the complete list of LLLPG articles & pages, visit the [home page](http://ecsharp.net/lllpg). To give feedback, post a comment [here](https://github.com/qwertie/ecsharp/issues/35) or an issue [here](https://github.com/qwertie/ecsharp/issues).
