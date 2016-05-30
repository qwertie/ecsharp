---
title: "3. Parsing terminology"
layout: article
date: 30 May 2016
toc: true
---

In this article series I will be teaching not just how to use my parser generator, but broader knowledge such as: what kinds of parser generators are out there? What's ambiguity and what do we do about it? What's a terminal? So this article is a general discussion of parsing terminology and parsing techniques.

## Parsing terminology

I'll start with a short glossary of standard parsing terminology.

First of all, grammars consist of _terminals_ and _nonterminals_.

A _terminal_ is an item from the input; when you are defining a lexer, a terminal is a single character, and when you are defining a parser, a terminal is a token from the lexer. More specifically, the grammar is concerned only with the "type" of the token, not its value. For example one of your token types might be `Number`, and a parser cannot treat a particular number specially; so if you ever need to treat the number "0" differently than other numbers, then your lexer would have to create a special token type for that number (e.g. `Zero`), because the grammar cannot make decisions based on a token's value. **Note**: in LLLPG you can circumvent this rule if you need to, but it's not pretty.

A _nonterminal_ is a rule in the grammar. So in this grammar:

    token Spaces @{ (' '|'t')+ };
    token Id @{
      ('a'..'z'|'A'..'Z'|'_')
      ('a'..'z'|'A'..'Z'|'_'|'0'..'9')*
    };
    token Int @{ '0'..'9'+ };
    token Token @{ Spaces | Id | Int };

The nonterminals are `Spaces`, `Id`, `Int` and `Token`, while the terminals are inputs like `'s'`, `'t'`, `'9'`, and so forth.

Traditional literature about parsing assumes that there is a single "Start Rule" that represents the entire language. For example, if you want to parse C#, then the start rule would say something like "zero or more using statements, followed by zero or more namespaces and/or classes":

    rule Start @{ UsingStmt* TopLevelDecl* };
    rule TopLevelDecl @{ NamespaceStmt | TypeDecl };
    ...

However, LLLPG is more flexible than that. It doesn't limit you to one start rule; instead, LLLPG assumes you might start with _any_ rule that isn't marked `private`. After all, you might want to parse just a subset of the language, such as a method declaration, a single statement, or a single expression.

LLLPG rules express grammars in a somewhat non-standard way. Firstly, LLLPG notation is based on ANTLR notation, which itself is mildly different from EBNF (Extended Backus–Naur Form) notation which, in some circles, is considered more standard. Also, because LLLPG is embedded inside another programming language (LES or EC#), it uses the `@{...}` notation which means "token literal". A token literal is a sequence of tokens that is not interpreted by the host language; the host language merely figures out the boundaries of the "token literal" and saves the tokens so that LLLPG can use them later. So while a rule in EBNF might look like this:

    Atom = ['-'], (num | id);

the same rule in LLLPG looks like this:

    rule Atom @{ ['-']? (num | id) };

LLLPG also has a ANTLR-flavored input mode, and if you're using that then you'd write

    Atom : ('-')? (num | id);

In EBNF, `[Foo]` means Foo is optional, whereas LLLPG expects `Foo?`, to express the same idea. Some versions of EBNF use `{Foo}` to represent a list of zero or more Foos, while LLLPG uses `Foo*` for the same thing. In LLLPG, like ANTLR, `{...}` represents a block of normal code (C#), which is why braces can't represent a list.

In LLLPG 1.0 I decided to support a compromise between the ANTLR and EBNF styles: you are allowed to write `[...]?` or `[...]*` instead of `(...)?` or `(...)*`; the `?` or `*` suffix is still required. Thus, square brackets indicate _nullability_--the idea that the region in square brackets may not consume any input.

Alternatives and grouping work the same way in LLLPG and EBNF (e.g. `(a | b)`), although LLLPG also has the notation `(a / b)` which I'll explain later.

In addition, some grammar representations do not allow loops, or even optional items. For example, formal "four-tuple" grammars are defined this way. I discuss this further in my blog post, [Grammars: theory vs practice](http://loyc.net/2013/grammars-theory-vs-practice.html).

A "language" is a different concept than a "grammar". A _grammar_ represents some kind of language, but generally there are many possible grammars that could represent the same language. The word "language" refers to the set of sentences that are considered valid; two different grammars represent the same _language_ if they accept and reject the same inputs (or, looking at the matter in reverse, if you can generate the same set of "sentences" from both grammars). For example, the following four rules all represent a list of digits:

    rule Digits1 @[ '0'..'9'+ ];
    rule Digits2 @[ '0'..'9'* '0'..'9' ];
    rule Digits3 @[ '0'..'9' | Digits3 '0'..'9' ];
    rule Digits4 @[ ('0'..'9'+)* ];

If we consider each rule to be a separate grammar, the four grammars all represent the same language (a list of digits). But the grammars are of different types: `Digits1` is an LL(1) grammar, `Digits2` is LL(2), `Digits3` is LALR(1), and I don't know what the heck to call `Digits4` (it's highly ambiguous, and weird). Since LLLPG is an LL(k) parser generator, it supports the first two grammars, but can't handle the other two; it will print warnings about "ambiguity" for `Digits3` and `Digits4`, then generate code that doesn't work properly. Actually, while `Digits4` is truly ambiguous, `Digits3` is actually unambiguous. However, `Digits3` **is** "ambiguous in LL(k)", meaning that it is ambiguous from the top-down LL(k) perspective (which is LLLPG's perspective).

The word _nullable_ means "can match nothing". For example, `@[ '0'..'9'* ]` is nullable because it successfully "matches" an input like "hello, world" by doing nothing; but `@[ '0'..'9'+ ]` is not nullable, and will only match something that starts with at least one digit.

## LL(k) versus the competition

LLLPG is in the LL(k) family of parser generators. It is suitable for writing both lexers (also known as tokenizers or scanners) and parsers, but not for writing one-stage parsers that combine lexing and parsing into one step. It is more powerful than LL(1) parser generators such as Coco/R.

LL(k) parsers, both generated and hand-written, are very popular. Personally, I like the LL(k) class of parsers because I find them intuitive, and they are intuitive because when you write a parser by hand, it is natural to end up with a mostly-LL(k) parser. But there are two other popular families of parser generators for computer languages, based on LALR(1) and PEGs:

* LALR(1) parsers are simplified LR(1) parsers which--well, it would take a lot of space to explain what LALR(1) is. Suffice it to say that  LALR(1) parsers support left-recursion and use table-based lookups that are impractical to write by hand, i.e. a parser generator or similar infrastructure is always required, unlike LL(k) grammars which  are straightforward to write by hand and therefore straightforward to understand. LALR(1) supports neither a superset nor a subset of LL(k) grammars; While I'm familiar with LALR(1), I admittedly don't know anything about its more general cousin LR(k), and I am not aware of any parser generators for LR(k) or projects using LR(k). Regardless  of merit, LR(k) for k>1 just isn't very popular. I believe the main advantage of the LR family of parsers is that LR parsers support left-recursion while LL parsers do not (more on that later).

* [PEGs](http://pdos.csail.mit.edu/papers/parsing%3Apopl04.pdf) are recursive-descent parsers with syntactic predicates, so PEG grammars are potentially very similar to grammars that you would use with LLLPG. However, PEG parsers traditionally do not use prediction (although prediction could probably be added as an optimization),  which means that they don't try to figure out in advance what the input means; for example, if the input is "42", a PEG parser does not  look at the '4' and decide "oh, that looks like a number, so I'll call the number sub-parser". Instead, a PEG parser simply "tries out" each option starting with the first (is it spaces? is it an identifier?), until one of them successfully parses the input. Without a prediction step, PEG parsers _may_ require memoization for efficiency (that is, a memory of failed and successful matches at each input character, to avoid repeating the same work over and over in different contexts). PEGs typically combine lexing (tokenization) and parsing into a single grammar (although it is not required), while other kinds of parsers separate lexing and parsing into independent stages.

Of course, I should also mention regular expressions, which probably the most popular parsing tool of all. However, while you can use regular expressions for simple parsing tasks, they are worthless for "full-scale" parsing, such as parsing an entire source file. The reason for this is that regular expressions do not support recursion; for example, the following rule is impossible to represent with a regular expression:

    rule PairsOfParens @[ '(' PairsOfParens? ')' ];

Because of this limitation, I don't think of regexes as a "serious" parsing tool.

Other kinds of parser generators also exist, but are less popular. Note that I'm only talking about parsers for computer languages; Natural Language Processing (e.g. to parse English) typically relies on different kinds of parsers that can handle ambiguity in more flexible ways. LLLPG is not really suitable for NLP.

As I was saying, the main difference between LL(k) and its closest cousin, the PEG, is that LL(k) parsers use prediction and LL(k) grammars usually suffer from ambiguity, while PEGs do not use prediction and the definition of PEGs pretends that ambiguity does not exist because it has a clearly-defined system of prioritization.

"Prediction" means figuring out which branch to take before it is taken. In a "plain" LL(k) parser (without and-predicates), the parser makes a decision and "never looks back". For example, when parsing the following LL(1) grammar:

    public rule Tokens @[ Token* ];
    public rule Token  @[ Float | Id | ' ' ];
    token Float        @[ '0'..'9'* '.' '0'..'9'+ ];
    token Id           @[ IdStart IdCont* ];
    rule  IdStart      @[ 'a'..'z' | 'A'..'Z' | '_' ];
    rule  IdCont       @[ IdStart | '0'..'9' ];

The `Token` method will get the next input character (known as `LA0` or lookahead zero), check if it is a digit or '.', and call `Float` if so or `Id` (or consume a space) otherwise. If the input is something like "42", which does not match the definition of `Float`, the problem will be detected by the `Float` method, not by `Token`, and the parser cannot back up and try something else. If you add a new `Int` rule:

    ...
    public rule Token @[ Float | Int | Id ];
    token Float       @[ '0'..'9'* '.' '0'..'9'+ ];
    token Int         @[ '0'..'9'+ ];
    token Id          @[ IdStart IdCont* ];
    ...

Now you have a problem, because the parser potentially requires infinite  lookahead to distinguish between `Float` and `Int`. By default, LLLPG uses LL(2), meaning it allows at most two characters of lookahead.  With two characters of lookahead, it is possible to tell that input like  "1.5" is `Float`, but it is not possible to tell whether "42" is a `Float` or  an `Int` without looking at the third character. Thus, this grammar is  ambiguous in LL(2), even though it is unambiguous when you have infinite  lookahead. The parser will handle single-digit integers fine, but given a two-digit integer it will call `Float` and then produce an error because the expected '.' was missing.

A PEG parser does not have this problem; it will "try out" `Float` first and if that fails, the parser backs up and tries `Int` next. There's a performance tradeoff, though, as the input will be scanned twice for the two rules.

Although LLLPG is designed to parse LL(k) grammars, it handles ambiguity similarly to a PEG: if `A|B` is ambiguous, the parser will choose A by default because it came first, but it will also warn you about the ambiguity.

Since the number of leading digits is unlimited, LLLPG will consider this grammar ambiguous no matter how high your maximum lookahead `k` (as  in LL(k)) is. You can resolve the conflict by combining F`loat` and `Int` into a single rule:

    public rule Tokens @[ Token* ];
    public rule Token  @[ Number | Id ];
    token Number       @[ '.' '0'..'9'+
                        | '0'..'9'+ ('.' '0'..'9'+)? ];
    token Id           @[ IdStart IdCont* ];
    ...

Unfortunately, it's a little tricky sometimes to merge rules correctly. In this case, the problem is that `Int` always starts with a digit but `Float` does not. My solution here was to separate out the case of "no leading digits" into a separate "alternative" from the "has leading digits" case. There are a few other solutions you could use, which I'll discuss later in this article.

I mentioned that PEGs can combine lexing and parsing in a single grammar  because they effectively support unlimited lookahead. To demonstrate why  LL(k) parsers usually can't combine lexing and parsing, imagine that you  want to parse a program that supports variable assignments like `x = 0` and function calls like `x(0)`, something like this:

    rule Expr    @[ Assign | Call | ... ];
    rule Assign  @[ Id Equals Expr ];
    rule Call    @[ Id LParen ArgList ];
    rule ArgList ...
    ...

If the input is received in the form of tokens, then this grammar only  requires LL(2): the `Expr` parser just has to look at the second token to  find out whether it is `Equals` ('=') or `LParen` ('(') to decide whether to call `Assign` or `Call`. However, if the input is received in the form of characters, no amount of lookahead is enough! The input could be something like:

    this_name_is_31_characters_long = 42;

To parse this directly from characters, 33 characters of lookahead would be required (LL(33)), and of course, in principle, there is no limit to  the amount of lookahead. Besides, LLLPG is designed for small amounts of  lookahead like LL(2) or maybe LL(4); a double-digit value is almost always a mistake. LL(33) could produce a ridiculously large and inefficient  parser (I'm too afraid to even try it.)

In summary, LL(k) parsers are not as flexible as PEG parsers, because they  are normally limited to k characters or tokens of lookahead, and k is  usually small. PEGs, in contrast, can always "back up" and try another  alternative when parsing fails. LLLPG makes up for this problem with  "syntactic predicates", which allow unlimited lookahead, but you must insert  them yourself, so there is slightly more work involved and you have to pay  some attention to the lookahead issue. In exchange for this extra effort,  though, your parsers are likely to have better performance, because you are explicitly aware of when you are doing something expensive (large lookahead). I say "likely" because I haven't been able to find any benchmarks comparing  LL(k) parsers to PEG parsers, but I've heard rumors that PEGs are slower,  and intuitively it seems to me that the memoization and retrying required  by PEGs must have some cost, it can't be free. Prediction is not free either, but since lookahead has a strict limit, the costs usually don't get very  high. Please note, however, that using syntactic predicates excessively  could certainly cause an LLLPG parser to be slower than a PEG parser,  especially given that LLLPG does not use memoization.

Let's talk briefly about LL(k) versus LALR(1). Sadly, my memory of LALR(1) has faded since university, and while Googling around, I didn't find any explanations of LALR(1) that I found really satisfying. Basically, LALR(1) is neither "better" nor "worse" than LL(k), it's just different. As wikipedia [says](http://en.wikipedia.org/wiki/LALR_parser):

> The LALR(k) parsers are incomparable with LL(k) parsers – for any j and k both greater than 0, there are LALR(j) grammars that are not LL(k) grammars and conversely. In fact, it is undecidable whether a given LL(1) grammar is LALR(k) for any k >= 0.

But I have a sense that most LALR parser generators in widespread use support only LALR(1), not LALR(k); thus, roughly speaking, LLLPG is more powerful than an LALR(1) parser generator because it can use multiple lookahead tokens. However, since LALR(k) and LL(k) are incompatible, you would have to alter an LALR(1) grammar to work in an LL(k) parser generator and vice versa.

Here are some key points about the three classes of parser generators.

### LL(k) ###

* Straightforward to learn: just look at the generated code, you can see how it works directly.
* Straightforward to add custom behavior with action code (`{...}`).
* Straightforward default error handling. When unexpected input arrives, an LL(k) parser can always report what input it was expecting at the point of failure.
* Predictably good performance. Performance depends on how many lookahead characters are _actually_ needed, not by the maximum k specified in the grammar definition. Performance of LL(k) parsers can be harmed by nesting rules too deeply, since each rule requires a method call (and often a prediction step); specifically, expression grammars often have this problem. But in LLLPG you can use a trick to handle large expression grammars without deep nesting (this is explained in [Part 5](lllpg-part-5.html#collapsing-precedence-levels-into-a-single-rule).)
* LL(k) parser generators, including LLLPG, may support valuable extra features outside the realm of LL(k) grammars, such as zero-width assertions (a.k.a. and-predicates), and multiple techniques for dealing with ambiguity.
* No left recursion allowed (direct or indirect)
* Strictly limited lookahead. It's easy to write grammars that require unlimited lookahead, so such grammars will have to be modified to work in LL(k). By the way, [ANTLR](http://antlr.org/) overcomes this problem with techniques such as LL(*).
* Low memory requirements.
* Usually requires a separate parser and lexer.

### LALR(1) ###

* Challenging to learn if you can't find a good tutorial; the generated code may be impossible to follow
* Supports left recursion (direct and indirect)
* Some people feel that LALR(1) grammars are more elegant. For example, consider a rule "List" that represents a comma-separated list of numbers. The LALR(1) form of this rule would be `num | List ',' num`, while the LL(1) form of the rule is `num (',' num)*`. Which is better? You decide.
* Low memory requirements.
* Usually requires a separate parser and lexer.
* LALR parser generators often have explicit mechanisms for specifying operator precedence.

### PEGs ###

* New! (formalized in 2004)
* Unlimited lookahead
* "No ambiguity" (wink, wink) - or rather, any ambiguity is simply ignored, "first rule wins"
* Supports zero-width assertions as a standard feature
* Grammars are _composable_. It is easier to merge different PEG parsers than different LL/LR parsers.
* Performance characteristics are not well-documented (as far as I've seen), but my intuition tells me to expect a naive memoization-based PEG parser generator to produce generally slow parsers. That said, I think all three parser types offer roughly O(N) performance to parse a file of size N.
* High memory requirements due to memoization (or, when not using memoization, the risk of exponential time complexity). Note: recently I saw a paper about a new feature to deal with this issue, called the "[cut operator](http://www.ialab.cs.tsukuba.ac.jp/~mizusima/publications/paste513-mizushima.pdf)", although it is not currently supported by most PEG parser generators.
* It's non-obvious how to support "custom actions". Although a rule may parse successfully, its caller can fail (and often does), so I guess any work that a rule performs must be transactional, i.e. it must be possible to undo the action.
* Supports unified grammars: a parser and lexer in a single grammar. However, I question the assumption that just because you _can_ combine the lexer and parser into a single grammar, you _should_. In typical PEG grammars the parser is "polluted" with lexical concerns, especially skipping whitespace. It's not bad once you get used to it, but I actually think a multi-stage PEG (with separate PEG lexer and PEG grammar) would be the best approach sometimes, except that most PEG parser generators do not support it.

### Regular expressions ###

* Very short, but often very cryptic, syntax.
* Matches characters directly; not usable for token-based parsing.
* Incapable of parsing languages of any significant size, because they do not support recursion or multiple "rules".
* Most regex libraries have special shorthand directives like `\b` that usually require more code to express when using a parser generator.
* Regexes are traditionally interpreted, but may be compiled. Although regexes do a kind of parsing, regex engines are not called "parser generators" even if they generate code.
* Regexes are closely related to [DFA](http://en.wikipedia.org/wiki/Deterministic_finite_automaton)s and [NFA](http://en.wikipedia.org/wiki/Nondeterministic_finite_automaton)s.

Next up
-------

In the next article you'll learn about the interesting [features you can use in your LLLPG grammars](4-lllpg-grammar-features.html).
