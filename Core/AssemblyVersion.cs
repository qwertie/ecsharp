using System.Reflection;

[assembly: AssemblyCopyright("Copyright ©2016. Licence: LGPL")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using '.*' - but you shouldn't, because it will cause a simple "Rebuild All" 
// command to change the version number which, I guess, produces an incompatible 
// assembly in the presence of strong names (strong naming prevents two assemblies 
// from linking together without an exact match - and now that I'm no longer using 
// '.*' I am still having occasional problems with 'MissingMethodException' in the 
// Visual Studio SFG, but I don't know why, maybe it's not about version numbers..)
[assembly: AssemblyVersion("2.4.0.1")]
[assembly: AssemblyFileVersion("2.4.0.1")]
