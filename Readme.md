README
======

The Language of Your Choice (Loyc) project is intended to become a rich set of tools for:

- Code analysis and transformation
- (_future_) Transforming source code between different languages
- (_future_) Writing libraries (or entire programs) in multiple programming languages
- (_future_) IDEs (code completion lists, various kinds of code visualization, intellisense)

Its components include [LeMP / Enhanced C#](http://loyc.net/lemp/), [LES](https://github.com/qwertie/LoycCore/wiki/Loyc-Expression-Syntax), [Loyc trees](https://github.com/qwertie/LoycCore/wiki/Loyc-trees), and the [Loyc Core libraries](http://core.loyc.net/). At the moment, Loyc tools are limited to the .NET platform.

For more information, please visit the [Loyc web site](http://loyc.net).

Installing the Visual Studio extensions
---------------------------------------

LeMP and LLLPG are self-hosting: they rely on themselves to help build themselves. Therefore, there is always a binary copy of LeMP and LLLPG in the `Lib\LeMP` subdirectory.

- **To install the LeMP and LLLPG Custom Tools**, run **Lib\LeMP\LoycFileGeneratorForVs.exe**, make sure your version of Visual Studio is listed, and click Register (install). **Note**: The custom tools run in-place; they are not copied anywhere else. Visual Studio versions 2008 through 2015 are supported.
- **To install syntax highlighting for *.ecs and *.les files**, run **Lib\LeMP\LoycSyntaxForVs.vsix**. Visual Studio versions 2010 through 2015 are supported.
- **To use the custom tool in a C# project**, add a text file to your project with a `*.ecs` extension, e.g. example.ecs. Open the Properties pane and change the "Custom Tool" option to **LeMP** (or **LLLPG** if you will be writing parsers.) To make sure it works, save some sample code and check the output file, like this:

		using System;
		using System.Collections.Generic;
		using System.Linq;
		using System.Windows
		namespace Loyc.Ecs {
			class Person {
				public this(public readonly string Name, public int WeightLb, public int Age) {}
			}
		}

**Warning**: Before installing a new version of LeMP or LLLPG, you must uninstall the old syntax highlighter (Tools | Extensions and Updates | LoycSyntaxForVS | Uninstall). A version mismatch between the two will cause the LeMP or LLLPG Custom Tool to stop working (you might get a `MissingMethodException` or a failure to load an assembly.) This error occurs because when you install the the syntax highlighter, Visual Studio makes its own private copy of the DLLs, but when LeMP is loaded, for some reason the .NET framework tries to use the same version of the DLLs used by the syntax highlighter, even if they are the wrong version, despite the fact that the correct version is stored in the same folder as LeMP.exe.
