These solutions build Visual Studio extensions.

- with the exception of single-file generators (see http://www.codeproject.com/Articles/686405/Writing-a-Single-File-Generator), Visual Studio extensions can only be built by the exact version of Visual Studio they were designed for, e.g. a VS2010 extension can only be built by VS2010. If you know a workaround, I'd like to hear it!
- They require a professional (non-Express) version of Visual Studio
- They require the matching Visual Studio SDK. Note that VS2010 SP1 requires the VS2010 SP1 SDK; the VS2010 SDK will not work in VS 2010 SP1.

There are two separate parts, the Single-File Generator (a.k.a. LLLPG Custom Tool) and the Syntax Highlighting extension (LoycExtensionForVs). I'd like them to be a single unit, but MS decided that vsix files (i.e. LoycExtensionForVs) would not be allowed to put things in the registry as required by a Single-File Generator. To make matters worse, any difference between the DLLs used by LoycExtensionForVs and those used by LllpgForVisualStudio will cause the latter to spit out some kind of weird error and refuse to work. Workaround: disable the syntax highlighter extension.
