---
title: LoycCore split into its own repository
layout: post
toc: false
---
People often have trouble understanding what Loyc is, and I often find it hard to explain it to people. That's not surprising: I know what I want--cross-language interoperability--but I don't know the best way to reach my goal, and that makes it hard to present a coherent vision. But out of the Loyc project I have grown a series of C# libraries that are much easier to understand and explain. That's why I am splitting out these libraries into their own repository, [LoycCore](https://github.com/qwertie/LoycCore).

LoycCore is a set of useful libraries for many kinds of .NET developers, starting with Loyc.Essentials, a library that "fills in the gaps" in the core of the .NET Base Class Library. LoycCore is especially focused on collections: classes, interfaces, adapters, and extension methods. Contributors are welcome: I'm looking for code reviews, unit tests, and new features. Anything that is relatively small (under about 4000 lines of code) that fits the theme "things that should have been built into the .NET framework, but aren't".

[Loyc.Essentials](https://github.com/qwertie/LoycCore/wiki/Loyc.Essentials).dll: a library that "fills in gaps" in the .NET Framework. It contains interfaces, extension methods, and small bits of functionality that are useful in virtually any software project. About half of Loyc.Essentials is devoted to collections: collection interfaces, collection adapters, collection extension methods and even a couple of collection implementations (most notably `DList<T>`). The other half includes a variety of things including math, geometry, `Symbol`s, localization, `Pair<A,B>`, "message sinks" and more.
[Loyc.Collections](https://github.com/qwertie/LoycCore/wiki/Loyc.Collections).dll: a library of sophisticated data structures including [VLists](http://www.codeproject.com/Articles/26171/VList-data-structures-in-C), persistent hashtables (Set/MSet/Map/MMap), [ALists](http://www.codeproject.com/Articles/568095/The-List-Trifecta-Part-1), and my favorite, the [hash tree types](https://github.com/qwertie/LoycCore/tree/master/Loyc.Collections/Sets) `Set<T>`, `MSet<T>`, `Map<T>` and `MMap<T>`.
[Loyc.Syntax](https://github.com/qwertie/LoycCore/wiki/Loyc.Syntax).dll: Contains a parser for Loyc Expression Syntax (LES), and various interfaces and base classes for Loyc Languages and for users of LLLPG. Many programs other than compilers need to parse "expressions" and LES provides a good balance between power and simplicity.
[Loyc.Utilities](https://github.com/qwertie/LoycCore/wiki/Loyc.Utilities).dll: Additional functionality that is either (A) not important enough to be placed in Loyc.Essentials.dll or (B) takes Loyc.Collections.dll as a dependency.

LoycCore will have its own home page at http://core.loyc.net and I have created a NuGet package for these libraries called LoycCore.
