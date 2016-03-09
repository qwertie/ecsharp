---
title:  "LeMP: Help Wanted"
layout: article
toc: true
---

Task: make EC# real
-------------------

Like Pinocchio, Enhanced C# wants to be real: not just a single-file generator, but its own proper project type with member completion and all that!

But before that can happen, I need someone to help make a Roslyn back-end for LeMP. In other words, I want to convert the output of LeMP into a Microsoft Roslyn syntax tree and compile it with the Roslyn C# 6 compiler. Once that exists, the next step will be to write a Visual Studio extension that introduces a new "Enhanced C# project type" that uses LeMP as the front-end and Roslyn C# as the backend. An EC# project type could allow `*.ecs` files to enjoy IntelliSense just like plain C#! It should also allow mixed C#-EC# projects, in which the *.cs files use Roslyn directly; both file types should be first-class citizens.

I do not have time to do all of this myself, my TO-DO list is full, so if nobody else volunteers, it won't happen. If you want to do this project, I will happily teach you whatever you need to now about LeMP; learning about Roslyn will be your responsibility, and I only know the basics of writing Visual Studio extensions (having written the syntax highlighter for `*.ecs`).

Task: write VB.NET printer
--------------------------

Write a class that prints Enhanced C# Loyc trees as VB.NET code. Since VB.NET and C# have the same type system (including generics), this will mostly be a straightforward (if time-consuming) task. There are a few differences where VB.NET allows something that Enhanced C# doesn't or vice versa; we should discuss these issues on GitHub as they come up.

Once a VB.NET printer exists, two cool things will be possible:

1. In a VB project, you can insert C# code in a .ecs file and it will almost seamlessly integrate with the VB code - the VB code can call the C# code and vice versa, with no need to create a separate assembly for your C# code.
2. You will be able to use all the features of LeMP in your VB project (albeit you'll have to use C# syntax, unless somebody writes a VB parser too... alternately, one could use the Roslyn VB parser and write a converter from Roslyn VB trees to Loyc trees).

The best example of an existing printer is `LesNodePrinter` (`EcsNodePrinter` is much too complex to base your printer on, and if I were to write it again, there would be some simplifications). Of course, now that `matchCode` exists, your printer can take advantage of it to deconstruct Loyc trees... or you could extend the existing validation code in `EcsValidators` to do deconstruction, too (e.g. extracting the class name and base classes from a class/struct/enum or other so-called "space declaration"). Note: I suspect that a good printer should use a data structure called a "rope" string so it can make smart decisions about line breaks and spacing, but I have not tried that approach before.

Task: write Javascript or C++ printer
-------------------------------------

Write a class that prints Loyc trees as C++ or Javascript code, so that in the future LLLPG and LeMP can produce code in those languages. Since no one has defined a mapping between Loyc trees and C++ or Javascript before, the first step is to plan out how each Javascript or C++ construct will be represented as a Loyc tree.

Sometimes you can just use the same mapping as Enhanced C#, and sometimes you'll have to extend it or modify it. For example, a C# class has a Loyc tree like this:

    class ClassName : BaseClass { Body; }       // EC# code
    #class(ClassName, #(BaseClass), { Body; }); // LES code

a very similar mapping might work well for C++:

    class ClassName : public BaseClass { Body; };      // C++ code
    #class(ClassName, #(public BaseClass), { Body; }); // LES code

but what to do about `ClassName {} Foo;`? One possible solution is to nest the class declaration inside a variable declaration:

    class ClassName : public BaseClass { Body; } Foo;
    #var(#class(ClassName, #(public BaseClass), { Body; }), Foo);

Designing a good and complete mapping could be pretty hard for C++, but Javascript by contrast should be quite straightforward.

Task: catalog bugs in EC# parser
--------------------------------

Could someone write a test program that looks for bugs in the Enhanced C# parser?

1. Recursively searches a directory for *.cs files and
2. Parses each one with code like this, printing out all errors to the console:

~~~csharp
var stream = File.Open(path, FileMode.Open, FileAccess.Read)
var chars = new StreamCharSource(stream);
IListSource<LNode> statements = EcsLanguageService.Parse(
   chars, path, MessageSink.Console, ParsingService.File);
~~~

3. Copy all files to a second folder, but with all *.cs files replaced by the output of `EcsLanguageService.Parse` (something like `File.WriteAllText(newPath, EcsLanguageService.Value.Print (chars, MessageSink.Console)`). This way you can try compiling the output, to verify that the printer works properly.

Finally, if you could [hire me](https://www.linkedin.com/in/qwertie) to do "consulting" work for you, it would be appreciated since I am currently unemployed. Even though my TO-DO list is full. Making FOSS is a full-time job™.

Task: talk to the lonely guy
----------------------------

What's missing from Enhanced C#? Can that feature be done with a macro? Tell me about your ideas or any macros you've made. My email address is on the home page.

P.S. a shout out to the [srclib](https://srclib.org/) project. I wish I had time to implement the Visual Studio version!
