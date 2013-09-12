README
------
September 8, 2013

The Language of Your Choice (Loyc) project is intended to become a rich set of tools for:

- Transforming source code between different languages
- Cross-language library programming, also known as acmeism
- IDEs (code completion lists, various kinds of code visualization, intellisense)
- Code analysis and transformation

Enhanced C#, LES (Loyc Expression Syntax) and LEL (Loyc expression langauge) are also under the Loyc umbrella.

It is in very early stages right now because I am working on it alone. I am focusing on the first point right now (transforming code between languages). Also, LES is working and I'm busy developing early stages of LEL. The project also currently includes a set of general-purpose libraries that will eventually be spun off into a separate project for a cross-language standard library.

Source code overview
--------------------

Loyc currently contains the following projects, listed in order from the lowest level to the highest level. A rough dependency tree is

          Loyc.Essentials
                 |
          Loyc.Collections
                 |
            Loyc.Syntax
                 |
           Loyc.Utilities
                 |
  +------+-------+-+------------+-------+----+
  |      |         |            |       |    |
Tests  LLLPG* MiniTestRunner  Baadia*  LEL  Ecs*

* I will eventually split out LLLPG, Baadia, Ecs (Enhanced C#), and the low-level libraries (Essentials, Collections) into separate projects on SourceForge or GitHub.

Also, Loyc.Syntax and Ecs depend on LLLPG at compile-time to generate their lexers and parsers.

Terminology:
- LLLPG is a parser generator (Loyc LL(k) Parser Generator) to help make fast recursive-descent parsers
- LEL is a LISP-inspired statically-typed programming language based on LES (Loyc Expression Syntax)
- EC# is an enhanced version of C# that does not exist yet but which will have tons of new features
- Baadia is a gesture-based program for drawing "Boxes and arrows" diagrams (yes, it totally doesn't belong here.)

I am currently keeping the unit tests in the same assemblies as the code being tested. I suspect this is why my libraries tend to be larger than many other "small" .NET libraries. Eventually I'll move the unit tests out into their own assemblies. 

For more information
--------------------

More overviews, architectural documentation, and specifications can be found in the project Wiki:

https://sourceforge.net/apps/mediawiki/loyc

You might also find interesting information on my development blog:

http://loyc-etc.blogspot.com