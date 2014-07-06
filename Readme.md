# README

July 2014

The Language of Your Choice (Loyc) project is intended to become a rich set of tools for:

- Transforming source code between different languages
- Writing libraries (or entire programs) in multiple programming languages
- IDEs (code completion lists, various kinds of code visualization, intellisense)
- Code analysis and transformation

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
                ^                    |
                |                    |
      +---------+-------------+----+ |
      |         |             |    | |
    Baadia* MiniTestRunner  Tests  Ecs*
                                    |
                                   LeMP
                                    |
                                  LLLPG*

\* I will eventually split out LLLPG, Baadia, Ecs (Enhanced C#), and the low-level libraries (Essentials, Collections) into separate projects on SourceForge or GitHub.

In this graph, a dependency line from C to A is hidden when there is already a line 
from C to B and B to A. For example, Ecs depends on Loyc.Syntax; Ecs also depends 
directly on Loyc.Collections and Loyc.Essentials, but the direct dependency lines are 
hidden so that the graph does not turn into spaghetti. Also, the graph does not show 
that Loyc.Syntax and Ecs depend on LLLPG at compile-time to generate their lexers and 
parsers.

I am currently keeping the unit tests in the same assemblies as the code being tested. I suspect this is why my libraries tend to be larger than many other "small" .NET libraries. Eventually I'll move the unit tests out into their own assemblies.

These projects use couple of tricks to support both .NET 3.5 and .NET 4 using a single solution file. The tricks are documented here: http://stackoverflow.com/questions/5006397/targetting-multiple-net-framework-versions-by-using-different-project-configura/23705790#23705790

## For more information

Home page and blog:
http://loyc.net

More overviews, architectural documentation, and specifications can be found in the project Wiki:
http://sourceforge.net/apps/mediawiki/loyc
