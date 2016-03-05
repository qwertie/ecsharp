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

P.S. a shout out to the [srclib](https://srclib.org/) project. I wish I had time to implement the Visual Studio version!

Task: talk to the lonely guy
----------------------------

What's missing from Enhanced C#? Can that feature be done with a macro? Tell me about your ideas or any macros you've made. My email address is on the home page.
