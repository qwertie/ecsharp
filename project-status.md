---
layout: page
title: "Project Status & Task List"
commentIssueId: 3
---
Help wanted!

## Loyc .NET core libraries, Loyc trees, and LES

See [status at core.loyc.net](http://core.loyc.net/project-status.html).

## Enhanced C#: the project to add numerous features to C#  

- Design: Main points complete, taking suggestions.
- Parser:
    - Supports most of C# and most planned syntax for EC#
    - TODO: LINQ parsing with tests
    - TODO: async/await parsing with tests
- Printer:
    - Supports most of C# and most planned syntax for EC#
    - Includes plain C# output mode that avoids using EC# syntax (only works when the syntax tree does not contain EC#-only stuff)
- Semantic analysis:
    - TODO. Does not exist!
- Backend:
    - TODO. Does not exist! (for now, using LeMP with C# output)
- Syntax highlighting:
    - Not implemented.

## LeMP: Lexical Macro Processor

LeMP takes the idea of LISP macros (arbitrarily complex user-defined source code transformations) and applies it to statically-typed languages (currently LES and EC# are supported). LeMP could be compared to the C/C++ preprocessor, but is much more powerful.

I'm trying to get as much mileage as possible out of LeMP before writing a proper EC# compiler. For example, there is a "safe navigation operator", `?.`, which is implemented as a macro by translating code like `x = A?.B` into `x = A != null ? A.B : null` (note that the `?.` operator itself is built into the EC# parser, but the macro is needed to give meaning to it). This is not necessarily the correct behavior; if `A` is a property then it should not be called twice. So if `A` is a property, the translation should be something like

    x = (var tmp_A = A) != null ? tmp_A.B : null

so that `A` is evaluated only once. However, this would require me to create a compiler that lets me declare a variable inside an expression, and I don't have time to write that compiler yet, so for now I settle for the inferior translation `x = A != null ? A.B : null`.

- Lexical Macro Processor (LeMP.MacroProcessor class, plus helper class LeMP.Compiler):
    - "Simple" version is done. Supports only [SimpleMacro] macros, which are designed to be stateless and to operate without context information. Currently macros do not support "hygiene", they do not support typing, and they cannot store state that is limited to a lexical block (macros that need state currently store it in ThreadStatic variabes). Macro names can be overloaded; if two macros have the same name, that's fine if exactly one of the macros returns a non-null result.
    - TODO: design a more powerful macro system.
- Standard Macros (designed to convert simple EC# code to plain C# code. this is not an exhaustive list):
    - Done: LES to C# macros (LeMP.Prelude.Les.Macros class): Contains numerous macros to convert LES code that resembles C# into syntax trees understood by the C# printer, e.g. the LES expression "class X { two::int; };" is converted to the syntax tree "#class(X, #(), { #var(#int, two); });" which is recognized by the C# printer as a class declaration.
    - Done: Null-dot: e.g. `a = b.c?.d.e` is translated to `a = b.c != null ? b.c.d.e : null`
    - TODO: Null coalesce set: e.g. `a ??= b` means `a = a ?? b` (`a ?? (a = b)` is potentially more efficient but not always allowed by standard C#).
    - Done: Tuple literals e.g. `(3, "three")`: done. By default `(X, Y)` is mapped to `Pair.Create(X, Y)` while other numbers of arguments are mapped to `Tuple.Create`. Use `set_tuple_maker(Tuple.Create)` if you prefer to use `System.Tuple` for all sizes. Unpacking, e.g. `(x, var y) = Foo();` is supported at the statement level (not inside a more complex statement)
    - TODO: String interpolation e.g. `$"Did you know that $x + $y = $(x+y)? It's true!"`
    - Done: `unroll(...) {...}`. This EC# example creates three fields and three properties: 
        unroll ((X,_x) in ((X, _x), (Y, _y), (Z, _z))) {
            private double _x;
            public double X { 
                get { return _x; }
                set { _x = value; }
            }
        }
    - Done: `replace(...) {...}`, e.g. `replace(C => Console, W => WriteLine) {C.WL("Hello");}`
    - TODO: `in`: `x in (1, 2, 3)` => `x == 1 || x == 2 || x == 3`
    - TODO: code contracts: `[requires]`, `[required]`, `[assert]` `[ensures]`
    - TODO: set-field modifiers: `public Foo(public int X, private int Y, set int Z) {}`
    - TODO: `[field]` attribute: `[field _x] public int X { get; set; }`
    - TODO: Forwarded methods: `static int InRange(int x, int lo, int hi) ==> MathEx.InRange;`
    - TODO: D's `scope(exit), scope(success), scope(failure)`, probably renamed `on_finally {}`, `on_return {}`, `on_exception {}`
    - TODO: `static int Square(int x) => x*x;` becomes `static int Square(int x) { return x*x; }`
    - TODO: `[unroll<T>(int, long, float, double)] public static T Add<$T>(T a, T b) { return a+b; }`
    - TODO: Code quotes.
    - TODO: A macro that runs C# code at compile-time.
    - Done: `LLLPG`
- IDE features:
    - LeMP single-file generator is done. It is the engine behind [LLLPG](http://www.codeproject.com/Articles/664785/A-New-Parser-Generator-for-Csharp). The copy distributed with LLLPG as of June 2014 doesn't include new macros like `unroll` and `replace`.

## Multi-Language Standard Library (MLSL)

Not started.

This project will be to create a "standard library" that facilitates writing libraries in multiple languages simultaneously. The MLSL

- will be ported to several different programming languages (how port a large library to several languages? I don't know yet! No doubt there will be substantial manual labour for each target language, but also some automated code conversion and code generation.)
- Has the same overall structure in all supported languages, but accepts minor style points from each language (e.g. might use firstWordLowerCase in Java but FirstWordUpperCase in C#)
- Contains language-specific interoperability features, e.g. the C++ version should have containers that include STL interfaces as well as MLSL interfaces, and be able to interoperate with C++ STL algorithms.
- Eventually the MLSL could form the basis of run-time cross-language interoperability, as an alternative to mechanisms like the CLR, JVM, Microsoft COM and (shudder) plain C interop, but its initial focus will probably be on making it possible to write sophisticated code in one language and port it to several languages automatically.
- Documentation for each API could have a list of related APIs in existing standard libraries.

## Standard Imperative Language (SIL)

Not started.

This project will be to create a family of similar "abstract" languages with

- a "universal" syntax tree 
- a "universal" semantics that encompass most of the capabilities of most statically-typed languages (I plan to ignore dynamic languages initially; except Julia, they do not interest me).
- a set of transformations for removing features by expressing a removed feature in terms of other features.

SIL will have three purposes:

- To serve as an intermediate format for translation between two programming languages
- To serve as an intermediate format for translation between a source language and an object language (e.g. between Swift and LLVM bitcode)
- To advance the field of computer science by refining the language (i.e. English terminology and mental models) we use to discuss features of programming language and how they differ. By converting code between languages, we will learn in a precise way how different features of different languages are related to each other, and hopefully develop a useful vocabulary for categorizing the _kinds_ of differences as well as for describing those differences.

SIL will be a superset of most existing languages, in that its broadest incarnation needs to support the features of all source languages.

## Loyc LL(k) Parser Generator (LLLPG)

- [Documentation](http://www.codeproject.com/Articles/664785/A-New-Parser-Generator-for-Csharp)
- LLLPG works almost exactly as desired. I can think of two current issues and one feature I'd like to add:
  - `&(and predicates)` are used in prediction only to resolve ambiguity. When they are not strictly needed for prediction, they are converted to calls to `Check()` instead, which is less than ideal because (1) you might want to force it to be used for prediction, but you can't and (2) LLLPG provides no way to customize the error message given to `Check()`; the end-user is shown the code of the predicate, which is ugly.
  - LLLPG is slow for some grammars, especially highly ambiguous ones; in fact I have found a case that has O(N!) performance for a specially-crafted input grammar of size N. The EC# parser and lexer require 3 seconds each to compile, and that's using a couple of strategically-placed gates to avoid extremely poor performance. I do not yet understand what makes the performance so bad.
  - Rather than write `@[ x:=X (ys+=Y)* { /* do something with x and ys */ } ]` I want to be able to write code like `@[ X Y* { /* do something with $X and $Y */ } ]`.

## Baadia: Boxes and arrows diagram maker

- TODO: split this into a separate GitHub repo.
- Gestures are working nicely
- Work left before v1.0 "minimum viable product"
  - TODO: scrolling and zooming
  - TODO: UI for selecting and editing box and line styles
  - TODO: in saved files: persist attachments (anchors) between shapes

## MiniTestRunner: Unit test and benchmark runner

- Work stopped, may not restart (I burned out trying to figure out how to do sandboxing and appdomains and how to seamlessly update the result tree when reloading a DLL and rerunning tests).
