---
title: "LeMP Macro Reference Manual"
layout: article
date: 20 Mar 2016
redirectDomain: ecsharp.net
---

Look up by category
-------------------

- Bulk code generation
    - [`unroll`](ref-other.html#unroll) ([see article](avoid-tedium-with-LeMP.html#unroll))
    - [`replace`](ref-other.html#replace) ([see article](avoid-tedium-with-LeMP.html#replace))
    - [Algebraic Data Types](ref-other.html#alt-class-algebraic-data-type) ([see article](pattern-matching.html#algebraic-data-types))
- Pattern matching
    - [`matchCode`](ref-other.html#matchcode): Syntax tree matching ([see article](lemp-code-gen-and-analysis.html#pattern-matching-using-matchcode))
    - [`match`](ref-other.html#match): Object pattern matching ([see
 article](pattern-matching.html#pattern-matching))
- [Code Contracts](ref-code-contracts.html)
    - [notnull](ref-code-contracts.html#notnull--notnull)
    - [[requires] & [assert]](ref-code-contracts.html#requires--assert)
    - [[ensures] & [ensuresAssert]](ref-code-contracts.html#ensures--ensuresassert)
    - [[ensuresOnThrow]](ref-code-contracts.html#ensuresonthrow)
    - [[ensuresFinally]](ref-code-contracts.html#ensuresfinally)
- Syntax tree generation
    - [`quote`](ref-other.html#quote) ([see article](lemp-code-gen-and-analysis.html#introducing-lemp))
    - [`rawQuote`](ref-other.html#rawquote)
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
    - `static if` and `static_if`
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
- [Other macros](ref-other.html)
