---
title: "2. Simple examples"
layout: article
date: 30 May 2016
toc: true
---

Here's a simple "scanner" in LLLPG - it's a scanner, in the sense that it scans the input and detects errors but doesn't produce any output:

~~~csharp
  LLLPG (lexer) {
      public rule Integer @{ Digit+ };
      public rule Digit @{ '0'..'9' };
  };
~~~

Here I used the original property-like notation for the rules. In LLLPG 1.7.5, an ANTLR-style notation was added, which looks like this:

~~~csharp
  LLLPG (lexer) @{
      public Digit : '0'..'9';
      public Integer : Digit+;
  };
~~~

Either way, the output is:

~~~csharp
  public void Digit()
  {
      MatchRange('0', '9');
  }
  public void Integer()
  {
      int la0;
      Digit();
      for (;;) {
        la0 = LA0;
        if (la0 >= '0' && la0 <= '9')
          Digit();
        else
          break;
      }
  }
~~~

That's it! So here's some relevant facts to learn at this point:

* First of all, to keep this example simple and brief I didn't bother with any "`using`" statements, and I didn't wrap this code in a `namespace` or a `class`. LLLPG (or more precisely, the LeMP preprocessor) doesn't care; the output reflects the input, so the output will likewise not have any "`using`" statements and won't be wrapped in a `class` or a `namespace` either. Garbage in, garbage out. If you want the output to be wrapped in a class declaration, you have to wrap the input in a class declaration.

* The grammar must be wrapped in an `LLLPG` block. Use "`LLLPG (lexer)`" for a lexer and "`LLLPG (parser)`" for a parser. The difference between the two is the treatment of _terminals_ (characters or tokens):
    * Lexer mode understands only integer and character input, but is optimized for this input. It does not accept named constants, only literal numbers and characters, because it can't tell which number a name might refer to (**Edit**: there is now an `alias` statement for naming constants). This mode assumes -1 means EOF (end-of-file). Note that lookahead variables have type `int`, not `char`, because `char` cannot hold -1, the representation of EOF. Luckily, C# doesn't really mind (`char` converts to `int` implicitly, although not the other way around).
    * Parser mode does not understand numbers, only symbols. Theoretically, the parser mode could be used for lexers too; the main problem is that it does not understand the range operator `..`, so you'd have to write monstrosities like `'0'|'1'|'2'|'3'|'4'|'5'|'6'|'7'|'8'|'9'` instead of `'0'..'9'`. Yuck. Parser mode expects you to define a C# symbol called `EOF` that represents end-of-file. In parser mode, a symbol called `Foo` is assumed to be a terminal if there is no rule called `Foo`.

* Each rule gets converted into a method with the same name. Attributes on the rule, such as `public` or `unsafe` (why are you using unsafe in a parser, smarty pants?) are transferred to the output. Rules support a few attributes, such as `[FullLLk]`, that are understood by LLLPG itself and stripped out of the output. The `private` attribute is slightly special; it enables an optimization called prematch analysis that non-private rules don't get.

* LLLPG expects a property called `LA0` to exist that returns the current input character; it also expects an `LA(int k)` method for other lookahead values.

* The `lexer` mode expects a method called `MatchRange()` to exist (and both modes expect a series of `Match()` methods for matching particular characters or tokens). This method's job is to test whether the input character matches the specified range, and to emit an error message if not. On mismatch, you can throw an exception, print a message, or whatever suits you. On success, `MatchRange()` should return the consumed character so that you can store it in a variable if you want. A runtime library is provided that includes all these methods.

* The `+` operator means "one or more of these". `Digit+` is exactly equivalent to `Digit Digit*`; the `*` means "zero or more of these", and the `*` operator is translated into a for-loop, as you can see in the output (as you probably know, `for (;;)` means "loop indefinitely"; it's equivalent to `while (true)`.)

* This example also demonstrates the main characteristic of LL(k) parsers: prediction. The `if (la0 >= '0' && la0 <= '9')` statement is performing a task called "prediction", which means, it is deciding which branch to take (`Digit`? or exit the loop?). It must reach across rules to do this: each rule requires an analysis of every other rule it calls, in addition to analysis inside the rule itself. In this case, `Integer` must be intimately familiar with the contents of `Digit`. Which is kind of romantic, when you think about it. Or not.

* The body of the rule is enclosed in `@{...};` - or the entire grammar, when using ANTLR-style syntax. Why not just plain braces or something else? Because LLLPG is embedded inside another programming language, and it cannot change the syntax of the host language. The construct "`public rule Foo @{...};`" is actually parsed by EC# as a property declaration, except with `@{...}` instead of the usual `{...}`. The `@{...}` is something called a **token literal**, which is a list of tokens (actually a _tree_, which matches pairs of `( ) { } [ ]`). The EC# parser gathers up all the tokens and stores them for later. After the entire source file is parsed, the macro processor gives LLLPG a chance to receive the token tree and transform it into something else. LLLPG it runs its _own independent parser_ to process the token tree. Finally, it replaces the `LLLPG` block with normal C# code that it generated. I'll explain this process in more detail later.

A simple lexer
--------------

My next example is almost _useful_. Now, somehow you need to provide the helper methods like `MatchRange` to LLLPG, and you can do that either with a special base class (`BaseLexer` in the Loyc.Syntax.dll runtime library) or a helper object (`LexerSource`, which is available as a standalone version, or in Loyc.Syntax.dll). The following example uses the latter:

~~~csharp
using System;
using Loyc.Syntax.Lexing;

enum TT
{ 
  Integer, Identifier, Operator
}
class MyLexer
{
  LexerSource _src;
  public MyLexer(string input) { _src = (LexerSource)input; }
  
  public Token NextToken()
  {
    int startPosition = InputPosition;
    TT tokenType = Token();
    int length = InputPosition - _startIndex;
    string text = _src.CharSource.Substring(_startIndex, length);
    return new Token((int)tokenType, startPosition, length, text);
  }

  LLLPG (lexer(inputSource: _src, inputClass: LexerSource)) @{
    Token returns [TT result]
      :  Op  {$result = TT.Operator;}
      |  Id  {$result = TT.Identifier;}
      |  Int {$result = TT.Integer;}
      ;
      
    private token Letter : ('a'..'z'|'A'..'Z');
    private token Id :     (Letter|'_')
                           (Letter|'_'|'0'..'9')*;
    private token Int:     '0'..'9'+;
    private token Op:      '+'|'-'|'*'|'/';
  };
}
~~~

This example uses `inputSource: _src, inputClass: LexerSource` to tell LLLPG where the helper methods are (in the `_src` object and the `LexerSource` class).

Unlike ANTLR, which generates text output and treats actions in braces `{...}` like plain text, LeMP (which comes with LLLPG) fully parses its input, and generates output in the form  of a Loyc tree, not text. A separate library is in charge of formatting that Loyc tree as C# code (I welcome volunteers to write output libraries for  other languages such as C++ and Java. You won't just be helping LLLPG itself, but the entire Loyc project! Let me know if you're interested.)

The output code includes the plain C# code such as the `MyLexer` constructor and the `NextToken` method, as well as the output of LLLPG. LLLPG's output, which becomes part of te `MyLexer` class, looks like this:

~~~csharp
TT Token()
{
    int la0;
    TT result = default(TT);
    // Line 25: ( Op | Id | Int )
    la0 = _src.LA0;
    switch (la0) {
    case '*':
    case '+':
    case '-':
    case '/':
        {
            Op();
            #line 25 "Untitled.ecs"
            result = TT.Operator;
            #line default
        }
        break;
    default:
        if (la0 >= 'A' && la0 <= 'Z' || la0 == '_' || la0 >= 'a' && la0 <= 'z') {
            Id();
            #line 26 "Untitled.ecs"
            result = TT.Identifier;
            #line default
        } else {
            Int();
            #line 27 "Untitled.ecs"
            result = TT.Integer;
            #line default
        }
        break;
    }
    return result;
}
void Letter()
{
    _src.Skip();
}
void Id()
{
    int la0;
    // Line 31: (Letter | [_])
    la0 = _src.LA0;
    if (la0 >= 'A' && la0 <= 'Z' || la0 >= 'a' && la0 <= 'z')
        Letter();
    else
        _src.Match('_');
    // Line 32: ( Letter | [_] | [0-9] )*
    for (;;) {
        la0 = _src.LA0;
        if (la0 >= 'A' && la0 <= 'Z' || la0 >= 'a' && la0 <= 'z')
            Letter();
        else if (la0 == '_')
            _src.Skip();
        else if (la0 >= '0' && la0 <= '9')
            _src.Skip();
        else
            break;
    }
}
void Int()
{
    int la0;
    _src.MatchRange('0', '9');
    // Line 33: ([0-9])*
    for (;;) {
        la0 = _src.LA0;
        if (la0 >= '0' && la0 <= '9')
            _src.Skip();
        else
            break;
    }
}
void Op()
{
    _src.Skip();
}
~~~

This example demonstrates some new things:

* The `$result = ...` statements in braces are called _actions_. Although actions are parsed as C# code (not plain text), it's important to understand that LLLPG _does not understand them_. For example, you could write a `return` statement in an action, but LLLPG would not understand that this causes the rule to exit, and would not take that into account during its analysis of the grammar. In this case we _could_ use a `return` statement safely, but in more complicated situations you might introduce a bug in your parser by doing unexpected control flow. So, be smart about it.

* Unlike most parser generators, LLLPG doesn't create output for you. That's why a `NextToken()` method exists _outside_ the grammar to create a `Token` structure from the text of the token. `Token` is included in the runtime library, but you could easily write your own. Now, this example is overly simplistic. In most real-life parsers you'll want to parse the token, e.g. given `"1234"` you'll want to construct the integer `1234`, not merely a string. Typicaly this would be done using custom code inside the `Int` rule itself.

* In the output, notice that the `Letter()` and `Op()` methods simply call  `Skip()`, which means "advance to the next input character". That's because LLLPG has analyzed the grammar and detected that in all the places where `Letter()` or `Op()` is called, the caller has already verified the input! So  `Letter()` and `Op()` don't have to _check_ the input, it's already guaranteed to be correct. This is an optimization called _prematch analysis_. **Note**: For this optimization to work, the rule must be explicitly marked private; otherwise, LLLPG assumes that the rule could be called by code _outside_ the grammar.

* Similarly, there are statements like `if (la0 == '_') Skip();` rather than `if (la0 == '_') Match('_');`. The user-supplied `Match(x)` method must check whether `LA0` matches `x`, but `Skip()` is faster since it skips the check.

* All the tokens are marked with the word `token`. The meaning of `token` will be explained later; it is recommended to put it on all tokens in a lexer, although it often has no effect (in a sense, `token` is the opposite of the `fragment` marker in ANTLR... sort of).

* LLLPG uses a `switch` statement if it suspects the code could be more efficient that way. Here it used  `switch()` to match `Op()`. However, it tries to balance code size with speed. It does not use switch cases for `Id()` because it would need 53 "`case`" labels to match it (26 uppercase + 26 lowercase + `'_'`), which seems excessive.

By the way, this example uses the [ANTLR-style syntax mode](lllpg-in-antlr-style.html) again. However, most of this manual uses the original syntax because most of the text was written before the ANTLR mode existed.

### Think twice: Do you really need a parser generator?

One of the most common introductory examples for any parser generator is an expression parser or calculator, maybe something along these lines:

~~~csharp
const int id = 0, num = 1; // other token types: '(', ')', '-', '+', etc.

LLLPG (parser(TerminalType: Token))
@{
   Atom returns [double result]
        : id              { $result = Vars[(string) $id.Value]; }
        | num             { $result = (double) $num.Value; }
        | '-' result=Atom { $result = -$result; }
        | '(' result=Expr ')'
        | error           { $result = 0;
          Error(0, "Expected identifer, number, or (stuff)"); }
        ;
    MulExpr returns [double result] :
        result:Atom
        (op:=('*'|'/') rhs:=Atom { $result = Do(result, op, rhs); })*
        ;
    AddExpr returns [double result] :
        result:MulExpr
        (op:=('+'|'-') rhs:=MulExpr { $result = Do(result, op, rhs); })*
        { return result; }
        ;
    Expr returns [double result]
        : t:=id '=' result:Expr { Vars[t.Value.ToString()] = $result; }
        | result:AddExpr
        ;
};

double Do(double left, Token op, double right)
{
    switch (op.Type) {
        case '+': return left + right;
        case '-': return left - right;
        case '*': return left * right;
        case '/': return left / right;
    }
    return double.NaN;
}
~~~

But if expression parsing is all you need, you really don't need a parser generator. For example, you can use the LES parser in [LoycCore](http://core.loyc.net), which is great for parsing simple expressions. Or you could use a [Pratt Parser like this one](http://higherlogics.blogspot.ca/2009/11/extensible-statically-typed-pratt.html). If you only need to parse simple text fields like phone numbers, you can use [regular expressions](http://www.regular-expressions.info/). And even if you need an entire programming language, you don't necessarily need to create your own; again, in many cases the [LES](http://loyc.net/les) parser is perfectly sufficient.

So before you go writing a parser, especially if it's for something important rather than "for fun", seriously consider whether an existing parser would be good enough.

Next up
-------

This concludes the introductory material. From here, you can choose where to go next:

- [Download & install instructions](http://ecsharp.net/lemp/install.html) (for LeMP, which includes LLLPG)
- [View samples in the LLLPG-Samples repository](http://github.com/qwertie/LLLPG-Samples)
- [The making-of](lemp-processing-model.html): Learn the history of LLLPG and how it is related to EC#, LeMP and LES
- [Parsing theory](3-parsing-terminology.html): learn about parsing terminology, especially if you've never written a parser. This contains _some_ LLLPG-specific information, so you might want to skim it even if you already know what you're doing.
- [Grammar features](4-lllpg-grammar-features.html): for example, operators like `&`, `=>`, `~` and `/` that you can put in a rule.
- [About the Loyc Libraries](5-loyc-libraries.html)
- [How to write a parser](6-how-to-write-a-parser.html): everything from boilerplate to syntax trees

For a complete list of articles, please visit the [home page](http://ecsharp.net/lllpg).
