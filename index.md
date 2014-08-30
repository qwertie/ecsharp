---
layout: default
title: Home
---
# About Loyc

The Language of Your Choice (Loyc) project is intended to become a rich set of tools for cross-language interoperability:

- Transforming source code between different languages
- Code analysis and transformation
- Writing libraries (or entire programs) in multiple programming languages
- IDEs (code completion lists, various kinds of code visualization, intellisense)

Its subprojects include:

- [Enhanced C#](https://github.com/qwertie/Loyc/wiki/Enhanced-C%23): a starting point for the Loyc framework, EC# will add new operators and many other new features to C#, starting with a LISP-inspired macro system.
- [Loyc trees](https://github.com/qwertie/LoycCore/wiki/Loyc-trees): a generic in-memory representation of syntax trees of any language
- [LES](https://github.com/qwertie/LoycCore/wiki/Loyc-Expression-Syntax) (Loyc Expression Syntax): a compact textual interchange format for Loyc trees
- [LeMP](https://github.com/qwertie/Loyc/wiki/Loyc-Expression-Language/#lemp) (Lexical Macro Processor): a LISP-style macro preprocessor that operates on Loyc trees
- [LLLPG](http://www.codeproject.com/Articles/664785/A-New-Parser-Generator-for-Csharp) (Loyc LL(k) Parser Generator): The parser generator being used to parse Enhanced C# and LES
- [MLSL](http://loyc.net/2014/design-elements-of-mlsl.html) (Multi-Language Standard Library): not yet started
- [SIL](https://github.com/qwertie/Loyc/wiki/Standard-Imperative-Language) (Standard Imperative Language): not yet started

At the moment, Loyc is limited to the .NET platform. Loyc has several general-purpose "core" libraries that you can read about at [core.loyc.net](http://core.loyc.net).

This is a huge project with many parts and I am looking feverishly for volunteers to help create these parts. You can reach me at `gmail.com`, with account name `qwertie256`.
