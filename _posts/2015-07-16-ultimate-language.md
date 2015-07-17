---
title: TODO - The ultimate programming language
layout: post
toc: false
commentIssueId: 10
---

I'd like to make a "universal" programming language that takes the best features of all the new languages, and either (1) directly supports those features, somehow, or (2) provides lower-level primitives out of which those features can be built, with features provided by the standard library.

![](../blog/GiantRobot.jpg) <br/>_If I have seen further, it is by having built a giant robot to stand on._

Goals
-----

- To support programming styles that people find familiar (object-oriented as well as functional, mutable as well as immutable state), as well as metaprogramming and more. In other words,
- To be all things to all people: to support a fantastically wide variety of features, and minimize the number of built-in features--implement as many features as possible in libraries.
- To be friendly to automatic refactoring tools
- To be friendly to autocompletion
- To be friendly to analysis tools (e.g. dependency graph generator)
- To be amenible to next-gen text editors (e.g. embedding diagrams in source code)
- To be useful as a "master language" for automatic conversions to other programming languages. To achieve this while still being a powerful language, the standard library designs should rely a lot on features such as macros that don't require support in target language. However, this goal is not exclusionary: the language can certainly _support_ features that cannot translate to some (or any) other languages. Often, features can be converted to most target languages with some loss of performance (e.g. union and intersection types) and/or loss of code clarity (e.g. alias types).
- To use the [MLSL](http://loyc.net/2014/design-elements-of-mlsl.html) as the "basic" standard library.
- To _eventually_ support C-like performance for D-like code. In other words, the generated code should be fast even when the code was convenient to write, in contrast to languages like C# where, for example, LINQ-to-objects has a performance penalty over plain-old `for` loops. However, in the interest of Rapid Application Development, performance cannot be a priority in the beginning, especially since I expect to start from my existing C# tools.
- To be as simple as possible under the above constraints.

Prototyping can be done in [LES](https://github.com/qwertie/LoycCore/wiki/Loyc-Expression-Syntax).

The wishlist
------------

Step 1 is to gather a list of the features we'd like to support. This post is step 1.

**Functional languages**

- Garbage collection
- Closures ("delegate" = function pointer with environment)
- Generics (preferably proven correct for all possible instances, unlike C++/D templates)
- Algebraic Data Types (and GADTs)
- Pattern matching / deconstruction
- (Tuples, [and, lists])
- "Everything is an expression"; `()` as a first-class type
- Equivalence between "functions" and "operators"
- Other features that make functional programming convenient (immutability by default, higher-order functions, etc.)

**Object-oriented languages**

- Dot notation for convenient invocation
- Encapsulation & data hiding
- Exceptions (throw/try/catch/finally; nearly zero runtime cost when exceptions don't occur)
- Inheritance
- Interfaces (= type classes + existential quantification in Haskell)

**C# (among others)**:

- Coroutines/generators (async/await & iterators)
- Properties
- Run-time code generation

**LISP languages**:

- Lexical macros

**[sweet.js](http://sweetjs.org) (among others)**

- Lexical macros with custom syntax

**[Nemerle](http://www.nemerle.org/About)**:

- Semantic macros (type system available)

**[Ceylon](http://ceylon-lang.org)**:

- Union and intersection types
- Solves the null reference problem that other OO languages have

**[D2](http://dlang.org/)**:

- Slices & ranges
- `scope(exit)` and friends (already supported in LeMP as [on_finally, etc.](https://github.com/qwertie/Loyc/blob/master/Main/LeMP/StandardMacros/OnFinallyReturnCatch.cs))
- compile-time code execution ("CTFE" in D circles)
- compile-time reflection (program introspection)
- GC or manual memory management on a case-by-case basis; "agnostic" pointers that can point _either_ to GC or non-GC memory
- Note: D's "mixins" are better achieved with a macro system, and UFCS is probably better achieved with an opt-in system like C# uses
- `with` statement

**[Go](https://golang.org/)**:

- Ad-hoc/implicit interfaces (fat pointer technique), known as [Dynamic Interfaces](http://www.codeproject.com/Articles/87991/Dynamic-interfaces-in-any-NET-language) in Visual Basic
- goroutines (making threading convenient)

**[Rust](http://www.rust-lang.org/)**:

- Zero-cost abstractions: unique boxes, borrowing, moving
- [Associated types](https://doc.rust-lang.org/book/associated-types.html)

**[Julia](http://julialang.org)**:

- Effective use of multiple dispatch (e.g. very interesting library-based type promotion system)
- High-performance dynamic typing (albeit with high memory usage)

**[Fortress](https://en.wikipedia.org/wiki/Fortress_(programming_language)) (dead language)**:

- Traits: Composable Units of Behavior
- Unit inference

**[Plaid](http://www.cs.cmu.edu/~aldrich/plaid/)**:

- Typestate
- "Concurrency-by-default"
- Integrated nominal and structural subtyping

**Other ideas**:

- Alias types (see below)
- Quick-bind operator `expr::var`, e.g. `if (File.ReadAllText(filename)::s.Length > 0 && !s.StartsWith("//")) ...`
- Custom literals and token trees (DSLs, metaprogramming)
- "Argument lists are tuples"
- Coroutines via stack switching (this possible in D, but has relatively poor performance for reasons that are unclear.)
- Concurrent session/protocol types for type-safe communication
- "Higher-kinded types"

Note: many of the above features implicitly enable other useful features that some languages implement directly. For example, C#'s `event`s and the `safe?.navigation?.operator` would not require any special compiler support in a language with macros and custom operators. I still put a couple of features of this nature in the above list, e.g. an expression-based language with macros wouldn't require the quick-bind `::` operator to be a built-in feature, but I put it on the list anyway because it is very handy but not well-known.

### TODO: Learn more about ###
- [Plaid](http://www.cs.cmu.edu/~aldrich/plaid/)
- [Bidirectional transformations](https://en.wikipedia.org/wiki/Bidirectional_transformation) / [Lenses](http://www.cis.upenn.edu/~bcpierce/papers/lenses-etapsslides.pdf)
- [Same-language GPU programming](http://www.ncbray.com/pystream)
- [Functional reactive programming](https://en.wikipedia.org/wiki/Functional_reactive_programming): Find out whether techniques to efficiently and automatically synchronize UI state with an underlying model require, or benefit from, explicit support in the programming language. Also, figure out what FRP is good for other than GUIs.
- [Dependent types](https://en.wikipedia.org/wiki/Dependent_type)

**Notes**:
- Markdown is probably best for doc comments
- The compiler should be a toolkit, not a black box, with the syntax tree and semantic analysis stuff usable by the outside world.

Type system ideas
-----------------

I'm thinking of having several levels of typing:

- Physical (structural) types: a minimum level of typing needed for memory safety (ignoring, for instance, the distinction between a signed and unsigned 32-bit integer). It's unclear what the exact definition should be, but the general idea is that physical types should allow casting between two unrelated nominal types if the have the same physical structure. A physical type system could potentially include a subtyping relation; for example, an integer could be considered a subtype of a pointer, since a conversion from pointer to integer is memory-safe, but the reverse conversion is dangerous.
- Nominal types: corresponds to the usual concept of types in most programming languages: a type name with an associated list of components (or a primitive type), arranged in memory in a particular way, with an associated set of operations.
- Alias type: associates a new name, and optionally a new interface (set of associated operations) to an existing type, while keeping the same physical structure and some degree of compatibility with the original type. An alias type could perhaps also be a represent a proof that a particular value fulfills some kind of constraint (e.g. a string is UTF-8, a tree has a particular structure.) Alias types are important for interoperability and programming language conversion.
- Typestate: a kind of type information that changes implicitly as an object is used, e.g. an object `file` may have typestate `Closed`, which changes to `Open` after calling `file.Open("filename.txt")`. Typestates would notably help with type-safe concurrency: channel types that implement "protocols".
- Proof types: associates some additional information with a value at compile-time, without changing the interface or behavior of the type. For example, unit inference could be done in this manner. If a type scheme does not affect program behavior, semantic analysis can be done in parallel with code generation (and execution, for that matter).

Details are yet to be worked out, but the most important goal should be to find some reasonable and easy-to-use form of type-system extensibility, so that interesting type-system features can be added as "libraries", in such a way that multiple orthogonal typing concepts can apply to a value at the same time.
