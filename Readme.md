# README

**Note:** Please visit the home page at http://loyc.net

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

Installing the Visual Studio extensions
---------------------------------------

LeMP and LLLPG are self-hosting: they rely on themselves to help build themselves. Therefore, there is always a binary copy of LeMP and LLLPG in the `Lib\LLLPG` subdirectory.

- **To install the LeMP and LLLPG Custom Tools**, run **Lib\LeMP\LoycFileGeneratorForVs.exe**, make sure your version of Visual Studio is listed, and click Register (install). **Note**: The custom tools run in-place; they are not copied anywhere else. Visual Studio versions 2008 through 2015 are supported.
- **To install syntax highlighting for *.ecs and *.les files**, run **Lib\LeMP\LoycSyntaxForVs.vsix**. Visual Studio versions 2010 through 2015 are supported.
- **To use the custom tool in a C# project**, add a text file to your project with a `*.ecs` extension, e.g. example.ecs. Open the Properties pane and change the "Custom Tool" option to **LeMP** (or **LLLPG** if you will be writing parsers.) To make sure it works, save some sample code and check the output file, like this:

		using System;
		using System.Collections.Generic;
		using System.Linq;
		using System.Windows
		namespace Loyc.Ecs {
			class Person {
				public Person(public readonly string Name, public int WeightLb, public int Age) {}
			}
		}

**Warning**: Before installing a new version of LeMP or LLLPG, you must uninstall the old syntax highlighter (Tools | Extensions and Updates | LoycSyntaxForVS | Uninstall). A version mismatch between the two will cause the LeMP or LLLPG Custom Tool to stop working with the dreaded error **"Cannot find custom tool 'LeMP' on this system."**) This error occurs because when you install the the syntax highlighter, Visual Studio makes its own private copy of the DLLs, but when LeMP is loaded, for some reason the .NET framework tries to use the same version of the DLLs used by the syntax highlighter, even if they are the wrong version, despite the fact that the correct version is stored in the same folder as LeMP.exe.
