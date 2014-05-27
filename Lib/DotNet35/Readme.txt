Theraot.Core is a .NET compatibility library that brings .NET 4.5 features to .NET 4, 3.5, and 2. See

https://github.com/theraot/Theraot

There is a separate version of the DLL for each .NET framework version. Originally I renamed Theraot.Core.dll to Theraot.Core.NET35.dll to distinguish it from versions of the library that target other .NET versions. This seemed to work fine in the Loyc projects themselves (VS2010), but for some reason it broke in my VS2008 work projects. In VS2008 the projects build fine, but at runtime there is an FileNotFoundException saying "Could not load file or assembly 'Theraot.Core, ...'". Googling around, I found no explanation for the problem or even anyone else reporting it. MS docs only say that "If you put a strong-named assembly in the global assembly cache, the assembly's file name must match the assembly name (not including the file name extension, such as .exe or .dll)"; this implies there is no such restriction for non-GAC assemblies.

To fix that, I switched back to the original filename Theraot.Core.dll, and put it in its own "DotNet35" directory in case we want to build a "DotNet2" version later.