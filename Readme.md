README
======

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

Installation
------------

If you just want the [core libraries](http://core.loyc.net/), you can find them in NuGet. Otherwise, see

- How to set up [LeMP or LLLPG in Visual Studio](http://ecsharp.net/lemp/install.html)
- How to set up [LeMP or LLLPG on other platforms](http://ecsharp.net/lemp/install.html#on-other-platforms)
- [Download page](https://github.com/qwertie/ecsharp/releases)

How to build
------------

Open Loyc.sln in Visual Studio, set the build configuration to Debug.NET45, and build it!

What's the deal with the binaries?
----------------------------------

LeMP and LLLPG are self-hosting: they rely on themselves to help build themselves. Therefore, a binary copy of LeMP and LLLPG is kept in the `Lib\LeMP` subdirectory. However, to avoid bloating the git history, it is rarely updated. Consider checking [here](https://github.com/qwertie/ecsharp/releases) for a newer release. As of late 2016, releases still contain the .NET 4 Release build rather than .NET 4.5, because the Visual Studio syntax highlighters are still built with VS 2010 (and compatible with VS 2010, VS 2012, VS 2013 and VS 2015). As soon as someone asks me to switch the main release .NET 4.5, I will.

Of course, you can also just build Loyc.sln to get a .NET 4.5 or even .NET 3.5 build. Compatibility with .NET 3.5 is aided by the Theraot compatibility library.

How to increment and publish new versions
-----------------------------------------

1. Update version in Core/AssemblyVersion.cs
2. Update appveyor.yml at `version:` (first line)
3. Update appveyor.yml at `- set SEMVER=` (semantic version combines w.x.y into wx.y, e.g. 2.7.1 => 27.1, because semantic versioning demands a new major version number for each breaking change, while the internal version number increments the minor version for a minor breaking change.)
4. Commit changes
5. Create an (unannotated) git tag like `v2.7.1` locally. Push changes. Appveyor should publish the NuGet packages.
6. Separately, create a release on GitHub.com, at least if the Visual Studio extension changed. Run UpdateLibLeMPAndReinstall.bat and if it builds successfully, prepare a zip file from the built files and include Lib\LeMP\LeMP_VisualStudio.vsix separately as part of the release.
