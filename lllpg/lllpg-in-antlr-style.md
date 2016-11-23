---
title: "Reference: the ANTLR-like syntax mode"
layout: article
date: 30 May 2016
---

As of LLLPG 1.7.4, an ANTLR-like syntax mode has been added that makes it easier to port grammars between ANTLR and LLLPG. This mode is used by writing code like

    class MyParser : BaseParserForList {
        [/* general options (as usual) */]
        LLLPG (/* code generation options (as usual) */) @{
            /* ANTLR-style rules (that's the new part) */
            myRule [ArgType argument] returns [ReturnType result] : grammar ;
        };
    }

In this mode, the LLLPG block can _only_ contain rules (not ordinary C# methods and fields).

Although the _outline_ of each rule uses an ANTLR-like syntax, there are still substantial differences:

- The grammar _inside_ each rule still follows LLLPG syntax and semantic rules, which are slightly different from ANTLR. For example, semantic predicates (written `{...}?` in ANTLR, `&{...}` in LLLPG) and gates (`=>`) work differently in the two systems. In LLLPG, semantic predicates are zero-width assertions that are handled somewhat like ordinary tokens, whereas in ANTLR... it's complicated, [I'll let Bart explain](http://stackoverflow.com/questions/3056441/what-is-a-semantic-predicate-in-antlr#answer-3056517). Also, LLLPG does not support regex character classes like `[a-zA-z]` because it shares the lexer with the host language (C#), which does not support character classes. Luckily, both ANTLR and LLLPG support `('a'..'z'|'A'..'Z')`.

- ANTLR supports LL(\*); LLLPG does not.

- LLLPG allows attributes in front of each rule; ANTLR does not.

- LLLPG does not support multiple return values; instead, use C# `out` parameters, or use `Tuple<A,B>` instead, as LeMP supports tuple syntax like `return (1, "two");`.

- The [`options`](https://theantlrguy.atlassian.net/wiki/display/ANTLR3/Rule+and+subrule+options) clause is not supported, but you can use `[k(constant)]` to set the lookahead for a rule. LLLPG does not support automatic backtracking or memoization, and the `greedy` option is a property of loops, not rules.

- `scope` clauses are not supported; you can manage scopes with ordinary C# code. `throws` and `after` are not supported either.

- The `catch` and `finally` clauses are not currently supported, but I believe the grammar actions `{on_finally {...}}` or `{on_throw_catch (Exception e) {...}}` have the same effect.

- A rule's return value must be named `result`. No other name is currently supported.

Also note:

- Generally speaking, Enhanced C# copies comments to your output file; however, this doesn't work inside `@{...}` blocks due to a limitation of how the whole system was designed. In ANTLR mode, the _entire grammar_ is inside a `@{...}` block so _none_ of the comments can be copied.

- The `@init` clause is supported but it is not needed; the following rules are equivalent:

        rule1 @init { Action(); }: X | Y;
        rule2 : { Action(); } (X | Y);

- Conventionally LLLPG uses round brackets for argument lists, e.g. you would write `R(777)` to pass `777` as the first argument of rule `R`. ANTLR requires `R[777]` instead. Because of this difference, ANTLR rule signatures use square brackets too (`R[int num] : ...`). In order to accommodate the ANTLR style mode, LLLPG now allows argument lists to use square brackets, but if you really prefer round brackets, LLLPG allows them on the formal argument list in ANTLR mode too (e.g. you can write `R(int num) returns (int result) : ...`).

- In any case, the return type always needs to be in brackets (square or round).

- The rest of these articles will use traditional LLLPG notation, which is designed to resemble method declarations.
