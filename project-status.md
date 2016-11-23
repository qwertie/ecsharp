---
layout: page
title: "Project Status & Task List"
commentIssueId: 3
date: updated 10 Nov 2016
---
Help wanted!

## Loyc .NET core libraries, Loyc trees, and LES

See [status at core.loyc.net](http://core.loyc.net/project-status.html).

## Enhanced C#: the project to add numerous features to C#  

- Design: Main points complete, taking suggestions.
- Parser:
    - Supports most of C# and most planned syntax for EC#
    - TODO: LINQ parsing with tests
- Printer:
    - Supports most of C# and most planned syntax for EC#
    - Includes plain C# output mode that avoids using EC# syntax (only works when the syntax tree does not contain EC#-only stuff)
- Semantic analysis & Backend:
    - Does not exist! (for now, using LeMP with C# output)
    - Jonathan VDC is writing Flame-based EC# compiler
    - TODO: Roslyn backend
- Syntax highlighting:
    - Available as Visual Studio extension
- Code completion:
    - TODO

## LeMP: Lexical Macro Processor

- Lexical Macro Processor (LeMP.MacroProcessor class, plus helper class LeMP.Compiler):
    - Mostly done, working well.
    - TODO: A macro that runs C# code at compile-time.
    - TODO: `[unroll<T>(int, long, float, double)] public static T Add<$T>(T a, T b) { return a+b; }`
- IDE features:
    - LeMP single-file generator is done. It is the engine behind [LLLPG](http://www.codeproject.com/Articles/664785/A-New-Parser-Generator-for-Csharp).

## Multi-Language Standard Library (MLSL)

Not started.

This project will be to create a "standard library" that facilitates writing libraries in multiple languages simultaneously. [Discussed here](http://loyc.net/2014/design-elements-of-mlsl.html).

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

- Done. Stable. See known bugs & slugs by searching [this file](https://github.com/qwertie/ecsharp/blob/master/Main/LLLPG/Tests/LlpgBugsAndSlugs.cs) for "`Fails =`"
- See [home page](http://ecsharp.net/lllpg)

## Baadia: Boxes and arrows diagram maker

- TODO: split this into a separate GitHub repo.
- TODO: rewrite in Javascript so it works on the web!
- Gestures are working nicely
- Work left before v1.0 "minimum viable product"
  - TODO: proper mouse-based scrolling and zooming
  - TODO: UI for selecting and editing box and line styles
  - TODO: in saved files: persist attachments (anchors) between shapes

## MiniTestRunner: Unit test and benchmark runner

- Work stopped, may not restart (I burned out trying to figure out how to do sandboxing and appdomains and how to seamlessly update the result tree when reloading a DLL and rerunning tests).
