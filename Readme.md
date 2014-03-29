README
------
Ovtober 31, 2013

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
             |   |      
             |   +---------------+
             |                   |
             |               Loyc.Syntax
             |                  | |
       Loyc.Utilities-----------+ |
             ^                    |
             |                    |
   +---------+-------------+----+ |
   |         |             |    | |
 Baadia* MiniTestRunner  Tests  Ecs*
                                 |
                                LEL
                                 |
                               LLLPG*

&#42; I will eventually split out LLLPG, Baadia, Ecs (Enhanced C#), and the low-level libraries (Essentials, Collections) into separate projects on SourceForge or GitHub.

In this graph, a dependency line from C to A is hidden when there is already a line from C to B and B to A. For example, Ecs depends on Loyc.Syntax; Ecs also depends directly on Loyc.Collections and Loyc.Essentials, but the direct dependency lines are hidden so that the graph does not turn into spaghetti. Also, the graph does not show that Loyc.Syntax and Ecs depend on LLLPG at compile-time to generate their lexers and parsers.

Terminology:
- LLLPG is a parser generator (Loyc LL(k) Parser Generator) to help make fast recursive-descent parsers
- LEL is a name for a programming language that doesn't really exist yet. Right now it really implements something that I'm calling LeMP (Lexical Macro Processor), which was earlier named micro-LEL. LeMP is a source-code preprocessor that LLLPG is based on.
- EC# is an enhanced version of C# that does not exist yet but which will have tons of new features
- Baadia is a gesture-based program for drawing "Boxes and arrows" diagrams (yes, it totally doesn't belong here.)
- MiniTestRunner is a unit test runner that I never completed, for a small unit test framework that resembles earlier versions of NUnit (Loyc.MiniTest).

I am currently keeping the unit tests in the same assemblies as the code being tested. I suspect this is why my libraries tend to be larger than many other "small" .NET libraries. Eventually I'll move the unit tests out into their own assemblies.

For more information
--------------------

More overviews, architectural documentation, and specifications can be found in the project Wiki:
http://sourceforge.net/apps/mediawiki/loyc

You might also find interesting information on my development blog:
http://loyc-etc.blogspot.com

Home page:
http://loyc.net