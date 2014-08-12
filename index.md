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

- [Enhanced C#](https://sourceforge.net/p/loyc/wiki/Ecs/): a starting point for the Loyc framework, EC# will add new operators and many other new features to C#, starting with a LISP-inspired macro system.
- [Loyc trees](http://sourceforge.net/p/loyc/wiki/Loyc%20trees/): a generic in-memory representation of syntax trees of any language
- [LES](http://sourceforge.net/p/loyc/wiki/LES/) (Loyc Expression Syntax): a compact textual interchange format for Loyc trees
- [LeMP](https://sourceforge.net/p/loyc/wiki/LEL/#lemp) (Lexical Macro Processor): a LISP-style macro preprocessor that operates on Loyc trees
- [LLLPG](http://www.codeproject.com/Articles/664785/A-New-Parser-Generator-for-Csharp) (Loyc LL(k) Parser Generator): The parser generator being used to parse Enhanced C# and LES
- MLSL (Multi-Language Standard Library): not yet started
- [SIL](https://sourceforge.net/p/loyc/wiki/Standard%20Imperative%20Language/) (Standard Imperative Language): not yet started

At the moment, Loyc is limited to the .NET platform. The Loyc core libraries are

- [Loyc.Essentials.dll](https://sourceforge.net/p/loyc/wiki/Loyc.Essentials/): A .NET library to "fill in the gaps" in the .NET framework by adding numerous simple "core" classes and interfaces
- [Loyc.Collections.dll](https://sourceforge.net/p/loyc/wiki/Loyc.Collections/): A library of interesting data structures (references `Loyc.Essentials`)
- [Loyc.Syntax.dll](https://sourceforge.net/p/loyc/wiki/Loyc.Syntax/): Contains the [`LNode`](https://github.com/qwertie/Loyc/blob/master/Src/Loyc.Syntax/Nodes/LNode.cs) class that represents a Loyc tree, the LES parser and printer, and recommended base classes for use with LLLPG.
- [Loyc.Utilities](https://sourceforge.net/p/loyc/wiki/Loyc.Utilities/): Additional general-purpose classes that either have a dependency on `Loyc.Collections` or were not important enough to go in `Loyc.Essentials`

This is a huge project with many parts and I am looking feverishly for volunteers to help create these parts. You can reach me at `gmail.com`, with account name `qwertie256`.
