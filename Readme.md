README
======

The ecsharp repository holds several tools for enhancing .NET and C# development:

- The Loyc .NET Core libraries, a set of libraries whose theme is "stuff that 
  should be built into the .NET framework, but isn't." These libraries have their
  [own repository](http://github.com/qwertie/LoycCore) and [home page](http://core.loyc.net),
  and the Loyc .NET Core repository is the `Core/` folder in this repository.
  One of these libraries (Loyc.Syntax) supports [universal syntax trees](http://loyc.net/loyc-trees),
  [LES2 and LES3](http://loyc.net/les).

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

Open Loyc.netfx.sln in Visual Studio (or Loyc.netstd.sln for the .NET Standard edition), set the build configuration to Debug, and build it!

If you need to change any .ecs or .les source files (Enhanced C# or [LES](http://loyc.net/les/)), you'll need to install the latest LeMP extension for Visual Studio, which can be found on the [Releases page](https://github.com/qwertie/ecsharp/releases). There is no _build step_ for these files, so the extension is not required for building. Unfortunately VS Code is not supported at this time - let me know if you need support.

If Visual Studio complains about OxyPlot, the easiest fix is to unload the LoycCore.Benchmarks project (nothing depends on it). To fix it properly, open Core\Loyc.netstd.sln, right-click the solution, choose "Restore NuGet packages", build the solution (just to make sure it worked), and then return to the original solution.

Visual Studio may complain, while building a .NET Framework 4.7.2 project, that 'Your project does not reference ".NETFramework,Version=v4.7.2" framework...." if you built the .NET Standard version of the same project earlier. To fix this, locate the folder named `obj` inside the project from which the error message originated, delete the entire `obj` folder, and rebuild (the `project.assets.json` file inside that folder seems to be causing the error).

How to publish new versions
---------------------------

This is more of a note-to-self than anything. Pull-requestors can ignore it.

1. Rebuild all (Release configuration in Loyc.all.sln) and run tests (Tests.exe)
2. Update version in Core/AssemblyVersion.cs
3. Update appveyor.yml at `version:` (first line)
4. Update appveyor.yml at `- set SEMVER=`
5. (Tentative) If the VS extension is to be released, update the version number in Visual Studio Integration\LoycForVS2017\source.extension.vsixmanifest
6. If a GitHub release is to be created, uninstall the LeMP VS extension and rebuild it with UpdateLibLeMPAndReinstall.bat. Manually check that it still works.
7. Commit changes
8. Push changes and check whether the build succeeded on AppVeyor.
9. On success, create an (unannotated) git tag like `v31.1.0` locally: `git tag v31.1.0`.
10. Push the tag to make Appveyor publish NuGet packages: `git push origin v31.1.0`.
11. Every so often, create a release on GitHub.com. Prepare a zip file from the built binaries and include Lib\LeMP\LeMP_VisualStudio.vsix separately as part of the release.
12. Update version-history.md on ecsharp.net and core.loyc.net, and update documentation by running doc/Doxygen.bat in the gh-pages branch.
13. If applicable, release a new version of the extension on the Visual Studio Marketplace