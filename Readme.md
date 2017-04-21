README
======

[![Join the chat at https://gitter.im/qwertie/ecsharp](https://badges.gitter.im/qwertie/ecsharp.svg)](https://gitter.im/qwertie/ecsharp?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

This repository holds several tools for enhancing .NET and C# development:

- The Loyc .NET Core libraries, a set of libraries whose theme is "stuff that 
  should be built into the .NET framework, but isn't." These libraries have their
  [own repository](http://github.com/qwertie/LoycCore) and [home page](http://core.loyc.net),
  and the Loyc .NET Core repository is the `Core/` folder in this repository.

- [Enhanced C#](http://ecsharp.net) (or EC#) is a liberalization and regularization of the C# language.
  You can think of EC# as a C# _preprocessor_, since only the "front end" part of the project is done.
  The preprocessor consists of three mostly-independent parts,
    1. The Enhanced C# parser
    2. [LeMP](http://ecsharp.net/lemp), the Lexical Macro Processor
    3. LeMP Standard Macros

- [LLLPG](http://ecsharp.net/lllpg), the Loyc LL(k) Parser Generator, which is used 
  to generate code from the grammars of Enhanced C#, [LES](http://loyc.net/les), and 
  LLLPG itself.

These projects are the first products of the [Loyc](http://loyc.net) (Language of Your Choice) initiative.

For more information, please visit the [Enhanced C# web site](http://ecsharp.net).

What's the deal with the binaries?
----------------------------------

LeMP and LLLPG are self-hosting: they rely on themselves to help build themselves. Therefore, a binary copy of LeMP and LLLPG is kept in the `Lib\LeMP` subdirectory. However, to avoid bloating the git history, it is rarely updated. Consider checking [here](https://github.com/qwertie/ecsharp/releases) for a newer release. As of late 2016, releases still contain the .NET 4 Release build rather than .NET 4.5, because the Visual Studio syntax highlighters are still built with VS 2010 (and compatible with VS 2010, VS 2012, VS 2013 and VS 2015). As soon as someone asks me to switch the main release .NET 4.5, I will.

Of course, you can also just build Loyc.sln to get a .NET 4.5 or even .NET 3.5 build. Compatibility with .NET 3.5 is aided by the Theraot compatibility library.

Installing the Visual Studio extensions
---------------------------------------

- **To install the LeMP and LLLPG Custom Tools**, run **LoycFileGeneratorForVs.exe** (from one of the releases or from the Lib\LeMP folder), make sure your version of Visual Studio is listed, and click Register (install). **Note**: The custom tools run in-place; they are not copied anywhere else. Visual Studio versions 2008 through 2015 are supported.
- **To install syntax highlighting for *.ecs and *.les files**, run **LoycSyntaxForVs.vsix**. Visual Studio versions 2010 through 2015 are supported.
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

**Warning**: Before installing a new version of LeMP or LLLPG, you must uninstall the old syntax highlighter (Tools | Extensions and Updates | LoycSyntaxForVS | Uninstall). A version mismatch between the two will cause the LeMP or LLLPG Custom Tool to stop working (you might get a `MissingMethodException` or a failure to load an assembly.) This error occurs because when you install the the syntax highlighter, Visual Studio makes its own private copy of the DLLs, but when LeMP is loaded, for some reason the .NET framework tries to use the same version of the DLLs used by the syntax highlighter, even if they are the wrong version - despite the fact that the correct version is stored in the same folder as LeMP.exe.
