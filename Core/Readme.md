README
======

The Loyc Core project is a set of general-purpose .NET libraries. LoycCore is especially focused on collections - classes, interfaces, adapters, and extension methods - but also has code in other areas, most notably parsing and syntax trees.

Contributors are welcome: more unit tests, code reviews, and new features are desired, anything relatively small (under about 3000 lines of code) that fits the theme "things that should have been built into the .NET framework, but aren't".

Please visit http://core.loyc.net for documentation.

**NOTE**: Development occurs primarily in the [Enhanced C# repository](https://github.com/loycnet/ecsharp), which contains the LoycCore repo as a "git subtree". However, `git subtree push` mysteriously stopped working which means that synchronization with this repo has become a manual process. As a result I would ask you **not to use that repo anymore**. We'll just do everything in ecsharp.

Dependency tree
---------------

Low-level libraries on top:

         Loyc.Essentials
                ^   ^
                |   |
                |   +----------------+
                |                    |
         Loyc.Collections        Loyc.Math
                ^                    ^
                |                    |
           Loyc.Syntax               |
                ^                    |
                |                    |
                +---------+----------+
                          |
                     Loyc.Utilities

These projects use couple of tricks to support .NET 3.5, .NET 4 and .NET 4.5 in a single solution file. The tricks are documented here: http://stackoverflow.com/questions/5006397/targetting-multiple-net-framework-versions-by-using-different-project-configura/23705790#23705790

Note: the versions of these libraries for .NET 3.5 depend on the compatibility library [Theraot.Core.dll](https://github.com/theraot/Theraot).

Links
-----

- [Home Page](http://core.loyc.net)
- [Reference Documentation](http://ecsharp.net/doc/code/)
- [Source code](http://github.com/qwertie/LoycCore)
- [Blog](http://loyc.net/blog)
