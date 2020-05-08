---
layout: default
title: Home
---
Welcome
=======

Enhanced C# is a modified syntax for the C# programming language that is backward-compatible with C#. Currently, EC# is just a parser that is compatible with a macro preprocessor called [_LeMP_](/lemp). LeMP is, in fact, language-agnostic and can support _any_ programming language for which a parser and printer has been written based on [Loyc trees](http://loyc.net/loyc-trees), but so far no volunteers have stepped forward to write parsers or printers for other languages.

Currently there is a Single-File Generator (a.k.a. Custom Tool) in Visual Studio that converts a single \*.ecs file to plain C# whenever you save it. Typically, then, you'll write projects that are mostly C#, with EC# used only when its benefits are big enough to outweigh the drawbacks of using a C# preprocessor (e.g. no IntelliSense).

It's possible to use a modified build process, but more convenient not to; instead, just place both the \*.ecs and \*.out.cs files in source control. This has the advantage that you don't have to modify your build server, and those who have not installed LeMP can still compile your projects.

EC# and LeMP are part of the [Loyc initiative](http://loyc.net).

Why a new language?
-------------------

I see new general-purpose language projects popping up all the time, and I don't like it. In my opinion there's no point in just making a language that is "better than C++" or "better than Java" or "better than C#", because _several languages fitting these descriptions already exist_, and many of them are even stable. Enhanced C# is really a gimmick — it's not the best language I could create, and it's not intended to be. I'm developing it because I think that backward compatibility is a potential fruitful way to put more power in the hands of developers. Switching languages is a risky business maneuver, and a potentially difficult one if you already have a C# code base. The cost and perceived risk of switching largely explains why there is slow uptake of nice languages like D, Rust, Nemerle and even Ceylon. Languages perceived to be obscure, like Nim, Plaid, or Dyvil, don't stand a chance.

That's why Enhanced C# tries very hard to be backward compatible with C#. I don't think 100% compatibility is necessary, but it's important to make sure that the number of changes required to make the EC# compiler happy is very close to zero. If you find any existing C# isn't handled properly by EC#, it's probably just a bug - please file a bug report.

Plus, the fact that EC# converts to plain C# means that if you decide to stop using EC#, you can always discard your EC# code and just use the C# code - especially now that EC# can preserve comments and blank lines.

Learn More
----------

To learn more about Enhanced C#, please read ["Enhanced C# for ordinary coders"](/ecs/for-normal-coders.html).

Most of the features currently available in Enhanced C# are implemented as LeMP macros. To learn about all these features, please visit the [LeMP home page](/lemp).

Dependency tree
---------------

The dependence tree of Enhanced C#, LeMP, and other .NET Loyc libraries is

         Loyc.Interfaces (Almost all interfaces + types used by interfaces)
                ^
                |
         Loyc.Essentials (collection adaptors, extension methods, message
                ^   ^     sinks, important utility classes, and more)
                |   |
                |   +----------------------------------------------+
                |                                                  |
         Loyc.Collections (Handy mutable and immutable         Loyc.Math
                ^          collections: ALists, VList,             ^
                |          hash trees...)                          |
                |                                                  |
        Loyc.Syntax.dll (Loyc trees, LES, LLLPG helper types)      |
         ^    ^    ^                                               |
         |    |    |                                               |
         |    |    +---------+-------------------------------------+
         |    |              |
         |    |         Loyc.Utilities
         |    |      (more utility classes)
         |    |            ^       ^   ^
         |    |            |       |   |
         |    |            |       |   |
         |  LoycCore.Tests and     |   |
         |  LoycCore.Benchmarks    |   |
         |                         |   |
    Loyc.Ecs.dll (Enhanced C#      |   |
         |       parser & printer) |   |
         |                         |   |
         +----------------------+  |   |
                                |  | LeMP.StdMacros.dll (standard LeMP macros)
                                |  |   |
                                |  |   |
                               LeMP.exe (Lexical Macro Processor + macros)
                                  |
                                  |
                               LLLPG.exe (Loyc LL(k) Parser Generator)

    External libraries:
    - Theraot.Core is a compatibility library used only in .NET 3.5 builds
    - ICSharpCode.TextEditor is a text editor widget for LeMP's built-in editor
    - OxyPlot is used by LoycCore.Benchmarks to results of the newest benchmarks
    Note: Baadia is a prototype boxes-and-arrows diagram maker. It doesn't belong 
      in this repo, but I haven't got around to separating it into its own repo.

To talk to me, you can either leave an [issue on GitHub](https://github.com/qwertie/ecsharp/issues) or email me at `gmail.com`, with account name `qwertie256`.
