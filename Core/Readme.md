README
======

The Loyc Core project is a set of general-purpose .NET libraries. LoycCore is especially focused on collections - classes, interfaces, adapters, and extension methods - but also has code in other areas, most notably parsing and syntax trees.

Contributors are welcome: more unit tests, code reviews, and new features are desired, anything relatively small (under about 3000 lines of code) that fits the theme "things that should have been built into the .NET framework, but aren't".

Please visit http://core.loyc.net for documentation.

**NOTE**: Development occurs primarily in the [Enhanced C# repository](https://github.com/qwertie/ecsharp), which contains the LoycCore repo as a "git subtree". However, `git subtree push` mysteriously stopped working which means that synchronization with this repo has become a manual process. As a result I would ask you **not to use that repo anymore**. We'll just do everything in ecsharp.

Dependency tree
---------------

Low-level libraries on top:

         Loyc.Interfaces
                ^
                |
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

Links
-----

- [Home Page](http://core.loyc.net)
- [Reference Documentation](http://ecsharp.net/doc/code/)
- [Source code](http://github.com/qwertie/LoycCore)
- [Blog](http://loyc.net/blog)
