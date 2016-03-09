### Installing LeMP

First, clone the [Loyc respository](https://github.com/qwertie/Loyc) from GitHub, or download and extract the [repository's zip file](https://github.com/qwertie/Loyc/archive/master.zip), because I haven't yet figured out how to do that magic auto-installation via NuGet.

![](lemp-install-1.png)

**Note**: the instructions below are for Visual Studio. To use LeMP on Linux or Mac, use LeMP.exe instead (it works under mono). For example, use `mono LeMP.exe --editor` to run its built-in editor.

For very esoteric reasons (long story short: blame Microsoft), LeMP/LLLPG are distributed in two separate parts: the Single-File Generator (Custom Tool), and the syntax highlighter.

So, browse to the `Lib\LeMP` folder and run `Lib\LeMP\LoycFileGeneratorForVs.exe` to install the LeMP & LLLPG Custom Tools (a.k.a. Single-File Generators). Make sure your version of Visual Studio is listed, and click Register (install).

![](lemp-install-2.png)

**Note**: The custom tools run in-place; they are not copied anywhere else. Visual Studio versions 2008 through 2015 are supported.

To install syntax highlighting for `.ecs` and `.les` files, run `Lib\LeMP\LoycSyntaxForVs.vsix`. Visual Studio versions 2010 through 2015 are supported.

![](lemp-install-3.png)

Finally, create a new C# project in Visual Studio (or open an existing one), and then create a new text file named `example.ecs`:

![](lemp-add-file-1.png)
![](lemp-add-file-2.png)

Finally, open the *Properties* panel and change the *Custom Tool* option to **_LeMP_**. An output file called `example.out.cs` should appear under `example.ecs`. To make sure it's all working fine, paste a little code in the new file, e.g.

~~~csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows
namespace Loyc.Ecs {
   class Person {
      public Person(public readonly string Name, public int WeightLb, public int Age) {}
   }
}
~~~

![](lemp-add-file-3.png)

**Warning**: Before installing a new version of LeMP or LLLPG, you must uninstall the old syntax highlighter _(Tools \| Extensions and Updates \| LoycSyntaxForVS \| Uninstall)_. A version mismatch between the two will cause the LeMP or LLLPG Custom Tool to stop working (typically with a `MissingMethodException` or a failure to load an assembly.) Fun fact: for extra lameness, such errors occurs even in the absense of changes; whenever I "Rebuild All", it always produces incompatible assemblies.

Certain features of LeMP (or [LLLPG](/lllpg)) require one or more Loyc libraries to be available at runtime. To use such features you'll also have to add references to the necessary DLLs (see the dependency tree on the [home page](/)).
