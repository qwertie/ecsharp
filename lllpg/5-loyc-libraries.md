---
title: "5. A brief overview of the Loyc libraries"
layout: article
date: 30 May 2016
---

When writing a parser, you have to decide whether you'll use the Loyc runtime libraries or not; the main advantage of _not_ using them is that you won't have to distribute the 3 Loyc DLLs with your application. But they contain a lot of useful stuff, so have a look and see if you like them.

The alternative is to use the standalone `LexerSource` and `ParserSource` which can be found in the "standalone" example in the [LLLPG Samples repository](http://github.com/qwertie/LLLPG-Samples).

The important library for parsers based on LLLPG is _Loyc.Syntax.dll_, which depends on _Loyc.Essentials.dll_ and _Loyc.Collections.dll_. These DLLs have documentation for most of the classes they contain, automatically available to VS IntelliSense through _Loyc.Syntax.xml_, _Loyc.Essentials.xml_ and _Loyc.Collections.xml_; you can also view the reference documentation [online](http://ecsharp.net/doc/code). The Loyc libraries contain only "safe", verifiable code.

In brief, let me just say very briefly what these libraries are for and what they contain. 

### Loyc.Essentials.dll ###

A library of general-purpose code that supplements the .NET BCL (standard libraries). It contains the following categories of stuff:

- Collection stuff: interfaces, adapters, helper classes, base classes, extension methods, and implementations for simple "core" collections such as [InternalList](http://core.loyc.net/collections/internal-list.html). You can [learn more in the docs](http://ecsharp.net/doc/code/namespaceLoyc_1_1Collections.html), but note that the documentation also shows the collections from Loyc.Collections.dll since it's all in the same namespace, `Loyc.Collections`.
- Geometry: simple generic geometric interfaces and classes, e.g. `Point<T>` and `Vector<T>`
- Math: generic math interfaces that allow arithmetic to be performed in generic code. Also includes fixed-point types, 128-bit integer math, and handy extra math functions in `MathEx`.
- Other utilities: message sinks ([`IMessageSink`](http://ecsharp.net/doc/code/interfaceLoyc_1_1IMessageSink.html)), [`Symbol`](http://ecsharp.net/doc/code/classLoyc_1_1Symbol.html), threading stuff, a miniture clone of NUnit ([`MiniTest`](https://github.com/qwertie/ecsharp/blob/master/Core/Loyc.Essentials/Utilities/MiniTest.cs), [`RunTests`](http://ecsharp.net/doc/code/classLoyc_1_1MiniTest_1_1RunTests.html)), and miscellaneous ["global" functions] and extension methods.
`Compatibility`: a very small amount of .NET 4.5 stuff, backported to .NET 4.0 when using the .NET 4 build.

Loyc.Essentials also defines [`ICharSource`](http://ecsharp.net/doc/code/interfaceLoyc_1_1Collections_1_1ICharSource.html) (defined in Loyc.Essentials.dll), a standard interface for a source of characters, which is used by lexers. `string` converts implicitly to [`UString`](http://ecsharp.net/doc/code/structLoyc_1_1UString.html) which is a string slice structure that implements `ICharSource`. The `Slice(start, count)` extension method can also get slices of strings.

[`IMessageSink`](http://sourceforge.net/p/loyc/code/HEAD/tree/Src/Loyc.Essentials/Utilities/IMessageSink.cs) serves as a simple, generic logging interface. It is recommended that your parsers report warnings and errors to an `IMessageSink` object. You can use `MessageSink.Console` to print (colored) errors to the console, `MessageSink.Null` to suppress output, and `MessageSink.FromDelegate((type, context, message, args) => {...})` to customize error handling.

The [`ParseHelpers` class](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1ParseHelpers.html) has generic number parsers that are handy for lexers, such as `TryParseDouble`, which can parse numbers of any reasonable radix and is therefore useful for hex float literals such as `0xF.Fp+1` (a syntax that represents 31.875).

### Loyc.Collections.dll ###

A library of data structures, mostly rather complex ones, currently all written by me:

- [VLists](http://www.codeproject.com/Articles/26171/VList-data-structures-in-C): this data structure is notable because Loyc nodes (`LNode`s) use `VList<LNode>` for their arguments and attributes. This is an implementation detail that ideally you wouldn't have to know about; but C# has no [`typedef`s](http://en.wikipedia.org/wiki/Typedef) that I could use to hide the type, and since VLists are `struct`s, if you treat them as `IList<T>` they will be boxed, and you don't really want that.
- [ALists](http://core.loyc.net/collections/alists-part1.html), including the B+tree-like data structures `BList<T>`, `BDictionary<K,V>`, and my favorite, `BMultiMap<K,V>`, plus the new [`SparseAList<T>`](http://core.loyc.net/collections/alists-part3.html) which I use in my syntax highlighter.
- [`Bijection<K1,K2>`](http://ecsharp.net/doc/code/classLoyc_1_1Collections_1_1Bijection_3_01K1_00_01K2_01_4.html): A dictionary that goes in both directions.
- [And more!](http://core.loyc.net/collections/)

### Loyc.Syntax.dll ###

Provides the foundations for LLLPG and contains the reference implementation of [LES, the syntax tree interchange format](http://loyc.net/les):

- [`BaseLexer`](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1Lexing_1_1BaseLexer.html) is the recommended base class for lexers created with LLLPG. [`BaseParserForList<Token,MatchType>`](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1BaseParserForList_3_01Token_00_01MatchType_01_4.html) is the recommended base class for parsers.
- [`StreamCharSource`](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1StreamCharSource.html) is an implementation of `ICharSource` designed for parsing a file without storing the whole thing in memory.
-  [`ISourceFile`](http://ecsharp.net/doc/code/interfaceLoyc_1_1Syntax_1_1ISourceFile.html) encapsulates an `ICharSource`, a file name string, and a mapping to translate character indexes to (line, column) pairs and back. It is derived from [`IIndexPositionMapper`](http://ecsharp.net/doc/code/interfaceLoyc_1_1Syntax_1_1IIndexPositionMapper.html).
- [`SourceRange`](http://ecsharp.net/doc/code/structLoyc_1_1Syntax_1_1SourceRange.html) is a triple (`ISourceFile Source`, `int StartIndex`, `int Length`) that represents a range of characters in a source file.
- `SourcePos` is a (filename, line, column) triple. While `SourceRange` is a struct so it can be stored compactly, `SourcePos` is assumed to be used much less often, and it is a class so it can be derived from `LineAndCol` which is a (line, column) pair.
- `IndexPositionMapper` provides mapping from `SourceRange` to `SourcePos` and back, but you don't necessarily _need_ this class because `BaseLexer` already keeps track of the current line number (and where it started). In your lexer, you **must** call `AfterNewline()` at each newline in order for index-position mapping to work correctly.
- [`LNode`](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1LNode.html) is a [Loyc Tree](http://loyc.net/loyc-trees). Parsers commonly use [`LNodeFactory`](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1LNodeFactory.html) to help construct `LNode`s.
- `LesLanguageService.Value` provides an LES parser and printer. It implements [`IParsingService`](http://ecsharp.net/doc/code/interfaceLoyc_1_1Syntax_1_1IParsingService.html).
- [`SourceFileWithLineRemaps`](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1SourceFileWithLineRemaps.html) is a helper class for languages that have a `#line` directive.
- [`Precedence`](http://ecsharp.net/doc/code/structLoyc_1_1Syntax_1_1Precedence.html): a simple but flexible standard representation for the concept of operator precedence and "miscibility".
- [`CodeSymbols`](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1CodeSymbols.html) is a `static class` filled with standard `Symbol`s used in Loyc trees for operators (`Add` for +, `Sub` for -, `Mul` for *, `Assign` for =, `Eq` for `==`, ...), statements (`Class` for `#class`, `Enum` for #enum, `ForEach` for #foreach, ...), modifiers (`Private` for #private, `Static` for #static, `Virtual` for #virtual, ...), types (`Void` for `#void`, `Int32` for `#int32`, Double for `#double`, ...), and so on.

Next up
-------

The next article in this series is ["how to write a parser"](6-how-to-write-a-parser.html).
