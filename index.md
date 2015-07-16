---
layout: default
title: Home
---
# <center>The Power of Choice</choice>

Coders are constrained in the way they express themselves by the language they are using. Different languages have different strengths and weaknesses, but expert coders are often unable to choose a programming language or library with the combination of strengths they desire. There are many scenarios that produce this result...

- You're working on a large project based on Language X. You probably have to keep using Language X, no matter how weak it is for the task at hand.
- The best library for doing T is written in Language X, but you also need to do Y and X doesn't support Y very well.
- You've chosen language X and realize later that you need a good library for doing T. Sadly, none of the libraries for doing T in X are any good.
- Your code _might_, _just might_ need to run in a web browser. Now you can only use Javascript, or one of a handful of languages that transpile to Javascript.
- You're not writing code for the browser, but you want to re-use code that _was_ designed for the browser. There's a strong pull toward Javascript, but it might not be the right fit for the new code you want to write.
- Your code needs high performance, but needs to interoperate with a slower language like Ruby, Python, etc. Now your design is highly constrained because it's hard to trade data between the two languages, due to fundamental mismatches between data types and memory management schemes. And if you don't choose C/C++, interoperability may be very hard in a language that doesn't understand C header files.
- You want a language with strong support for A, B and C, but no language exists that is strong in all three areas at the same time.
- You find a language that is excellent for A, B, and C, and start using it for a new project, only to discover that its IDE/Intellisense/Debugger/third party libraries are crap.
- You want to use two libraries related to the same topic (whether it's graphics, GIS, math, persistence, GUIs...), written in the same language, but it's painful because the two libraries use completely different interfaces and conventions.

Loyc is about finding ways to bring the world's programming languages closer together, looking for ways to solve problems like these with as little code as possible, and making developers more productive by giving them options they've never had before.

# About Loyc

The Language of Your Choice (Loyc) project is a group of projects related to cross-language interoperability:

- Transforming source code between different languages
- Writing libraries (or entire programs) in multiple programming languages
- Code analysis and transformation
- IDEs (syntax highlighting, intellisense)

Loyc is in its infancy, and probably will remain so until I attract either (A) volunteers to work on its components, or (B) a major corporate sponsor. Current and potential Loyc projects include:

- [Enhanced C#](https://github.com/qwertie/Loyc/wiki/Enhanced-C%23): a starting point for the Loyc framework, EC# will add new operators and many other new features to C#, starting with a LISP-inspired macro system (LeMP).
- [LeMP](https://github.com/qwertie/Loyc/wiki/Loyc-Expression-Language/#lemp) (Lexical Macro Processor): a LISP-style macro preprocessor that operates on Loyc trees
- [Loyc trees](https://github.com/qwertie/LoycCore/wiki/Loyc-trees): a generic in-memory representation for syntax trees of any language.
- [LES](https://github.com/qwertie/LoycCore/wiki/Loyc-Expression-Syntax) (Loyc Expression Syntax): a superset of JSON, LES is an C-like interchange format for Loyc trees, suitable for representing normal programming languages, DSLs, configuration files, and intermediate representations.
- [LLLPG](http://www.codeproject.com/Articles/664785/A-New-Parser-Generator-for-Csharp) (Loyc LL(k) Parser Generator): The parser generator being used to parse Enhanced C# and LES
- [MLSL](http://loyc.net/2014/design-elements-of-mlsl.html) (Multi-Language Standard Library): not yet started
- [SIL](https://github.com/qwertie/Loyc/wiki/Standard-Imperative-Language) (Standard Imperative Language): not yet started
- Visual studio integration: When you write a lexer & parser, you can get syntax highlighting almost for free.

At the moment, Loyc is limited to the .NET platform. Loyc has several general-purpose "core" libraries that you can read about at [core.loyc.net](http://core.loyc.net).

This is a huge project with many parts and I am looking feverishly for volunteers to help create these parts. You can reach me at `gmail.com`, with account name `qwertie256`.
