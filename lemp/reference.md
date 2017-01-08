---
title: "LeMP Macro Reference Manual"
layout: article
date: 20 Mar 2016
---

Look up by category
-------------------

- Code generation & macro replacement
    - [`define`](ref-codegen.html#define)
    - [`replace`](ref-codegen.html#replace) ([see article](avoid-tedium-with-LeMP.html#replace))
    - [`unroll`](ref-codegen.html#unroll) ([see article](avoid-tedium-with-LeMP.html#unroll))
    - [Algebraic Data Types](ref-codegen.html#alt-class-algebraic-data-type) ([see article](pattern-matching.html#algebraic-data-types))
- Compile-time decision-making
    - [`static matchCode`](ref-codegen.html#static-matchcode)
    - [`static if` and `static_if`](ref-codegen.html#static-if)
    - [`` `staticMatches` `` operator](ref-codegen.html#staticmatches-operator)
    - [`static deconstruct`](ref-codegen.html#static-deconstruct-aka-deconstruct) and `static tryDeconstruct`
- Syntax tree generation (at runtime)
    - [`quote`](ref-other.html#quote) ([see article](lemp-code-gen-and-analysis.html#introducing-lemp))
    - [`rawQuote`](ref-other.html#rawquote)
- Pattern matching (at runtime)
    - [`matchCode`](ref-other.html#matchcode): Syntax tree matching ([see article](lemp-code-gen-and-analysis.html#pattern-matching-using-matchcode))
    - [`match`](ref-other.html#match): Object pattern matching ([see
 article](pattern-matching.html#pattern-matching))
- [Code Contracts](ref-code-contracts.html)
    - [notnull](ref-code-contracts.html#notnull--notnull)
    - [[requires] & [assert]](ref-code-contracts.html#requires--assert)
    - [[ensures] & [ensuresAssert]](ref-code-contracts.html#ensures--ensuresassert)
    - [[ensuresOnThrow]](ref-code-contracts.html#ensuresonthrow)
    - [[ensuresFinally]](ref-code-contracts.html#ensuresfinally)
- [Fine-grained features](ref-other.html)
    - `assert`
    - Backing fields
    - Get-or-create-member ([article](avoid-tedium-with-LeMP.html#automagic-field-generation))
    - Tuples ([see article](pattern-matching.html#tuples))
    - `concatId` and `$(out ...)`
    - `this` constructors
    - [Easy method forwarding](ref-other.html#method-forwarding)
    - `in` and in-range operator combinations
    - `includeFile`
    - `namespace` without braces
    - `??=`
    - `stringify`
    - `@@symbols`
    - multi-using
- [LLLPG](/lllpg)

How to write your own macros? [TODO](lemp-code-gen-and-analysis.html#writing-macros) - ask me how

Pages
-----

- [Built-in macros](ref-builtin-macros.html)
- [Code Contracts](ref-code-contracts.html)
- [on_return, on_throw, on_throw_catch, on_finally](ref-on_star.html)
- [Code generation & compile-time decision-making](ref-codegen.html)
- [Other macros](ref-other.html)
