# README

August 2014

The Loyc Core project is a set of general-purpose .NET libraries:

- **[[Loyc.Essentials]].dll**: a library of interfaces, extension methods, and small bits of functionality that are useful in virtually any software project. About half of **Loyc.Essentials** is devoted to collections: collection interfaces, collection adapters, collection extension methods and even a couple of collection implementations (most notably `DList<T>`). The other half includes a variety of things including math, geometry, `Symbol`s, localization, `Pair<A,B>`, "message sinks" and more.
- **[[Loyc.Collections]].dll**: a library of sophisticated data structures including [ALists][1], [VLists][2], and my favorite, the hash tree types `Set<T>`, `MSet<T>`, `Map<T>` and `MMap<T>`.
- **[[Loyc.Syntax]].dll**: Contains a parser for [Loyc Expression Syntax (LES)][3], and various interfaces and base classes for Loyc Languages and for users of LLLPG.
- **[[Loyc.Utilities]].dll**: Additional functionality that is either (A) not important enough to be placed in **Loyc.Essentials.dll** or (B) takes **Loyc.Collections.dll** as a dependency.

  [1]: http://www.codeproject.com/Articles/568095/The-List-Trifecta-Part
  [2]: http://www.codeproject.com/Articles/26171/VList-data-structures-in-C
  [3]: https://github.com/qwertie/Loyc/wiki/Loyc-Expression-Syntax

You can find more detailed descriptions in the Wiki.

## Dependency tree

Low-level libraries on top:

         Loyc.Essentials
                |
         Loyc.Collections
                ^   ^      
                |   |      
                |   +----------------+
                |                    |     
                |                    |
          Loyc.Utilities        Loyc.Syntax

These projects use couple of tricks to support .NET 3.5, .NET 4 and .NET 4.5 in a single solution file. The tricks are documented here: http://stackoverflow.com/questions/5006397/targetting-multiple-net-framework-versions-by-using-different-project-configura/23705790#23705790

TODO: Add portable class library version. I may wait until PCLs are supported by Visual Studio Express Edition.

Note: the versions of these libraries for .NET 3.5 depend on the compatibility library [Theraot.Core.dll](https://github.com/theraot/Theraot).

## Links

- [Home Page](http://core.loyc.net)
- [Reference Documentation](http://loyc.net/doc/code/)
- [Source code](http://github.com/qwertie/LoycCore)
- [Wiki](https://github.com/qwertie/LoycCore/wiki)
- [Blog](http://loyc.net/blog)
