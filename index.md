---
layout: default
title: Home
---
# <center>The Power of Choice</choice>

Coders are constrained in the way they express themselves by the language they are using. Different languages have different strengths and weaknesses, but expert coders are often unable to choose a programming language with the combination of strengths they desire. There are many scenarios that produce this result...

- You're working on a large project based on Language X. You probably have to keep using Language X, no matter how weak it is for the task at hand.
- The best library for doing T is written in Language X, but you also need to do Y and X doesn't support Y very well.
- Your code _might_, _just might_ need to run in a web browser. Now you can only use Javascript, or one of a handful of languages that transpile to Javascript.
- You're not writing code for the browser, but you want to re-use code that _was_ designed for the browser. There's a strong pull toward Javascript, but it's not always the right fit for the new code you want to write.
- Your code needs high performance, but needs to interoperate with a slower language like Ruby, Python, etc. Now your design is highly constrained because it's hard to trade data with the slower language, due to mismatches between fundamental types and memory management schemes. And if you don't choose C/C++, the amount of work needed to interoperate may be prohibitive because your language doesn't understand C header files.
- You want a language with strong support for A, B and C? For some values of A, B and C, no language exists that is strong in all three areas at the same time.

# About Loyc

The Language of Your Choice (Loyc) project is a group of projects related to cross-language interoperability:

- Transforming source code between different languages
- Writing libraries (or entire programs) in multiple programming languages
- Code analysis and transformation
- IDEs (code completion lists, various kinds of code visualization, intellisense)

Loyc is in its infancy, and probably will remain so until I attract other people to the project. Current and potential Loyc projects include:

- [Enhanced C#](https://github.com/qwertie/Loyc/wiki/Enhanced-C%23): a starting point for the Loyc framework, EC# will add new operators and many other new features to C#, starting with a LISP-inspired macro system.
- [Loyc trees](https://github.com/qwertie/LoycCore/wiki/Loyc-trees): a generic in-memory representation of syntax trees of any language
- [LES](https://github.com/qwertie/LoycCore/wiki/Loyc-Expression-Syntax) (Loyc Expression Syntax): a compact textual interchange format for Loyc trees
- [LeMP](https://github.com/qwertie/Loyc/wiki/Loyc-Expression-Language/#lemp) (Lexical Macro Processor): a LISP-style macro preprocessor that operates on Loyc trees
- [LLLPG](http://www.codeproject.com/Articles/664785/A-New-Parser-Generator-for-Csharp) (Loyc LL(k) Parser Generator): The parser generator being used to parse Enhanced C# and LES
- [MLSL](http://loyc.net/2014/design-elements-of-mlsl.html) (Multi-Language Standard Library): not yet started
- [SIL](https://github.com/qwertie/Loyc/wiki/Standard-Imperative-Language) (Standard Imperative Language): not yet started

At the moment, Loyc is limited to the .NET platform. Loyc has several general-purpose "core" libraries that you can read about at [core.loyc.net](http://core.loyc.net).

This is a huge project with many parts and I am looking feverishly for volunteers to help create these parts. You can reach me at `gmail.com`, with account name `qwertie256`.
