---
title: LeMP FAQ
layout: article
tagline: "not a real FAQ: nobody's asking questions yet"
---

### Q. How do I invoke LeMP programmatically? ###

A. There are two ways. Either you can use  [`MacroProcessor`](https://github.com/qwertie/Loyc/blob/master/Main/LeMP/MacroProcessor.cs) directly, or you can use [`Compiler`](http://ecsharp.net/doc/code/classLeMP_1_1Compiler.html) (see [Compiler.cs](https://github.com/qwertie/Loyc/blob/master/Main/LeMP/Compiler.cs)), which is a wrapper around it. `Compiler` is designed to read and write files on disk based mainly on command-line arguments, while `MacroProcessor` is designed mainly to process Loyc trees.

**How to use `Compiler`:**

	var c = new LeMP.Compiler(MessageSink.Console);
	var argList = new List<string>() { @"D:\Dev\Loyc\Core\Tests\Program.cs", "--outext=out.cs" };
	var options = c.ProcessArguments(argList, true, true);
	// Look for --help in options list
	if (!LeMP.Compiler.MaybeShowHelp(options, LeMP.Compiler.KnownOptions))
		c.Run();

**How to use `MacroProcessor`:** (two ways)

	var MP = new LeMP.MacroProcessor(typeof(LeMP.Prelude.BuiltinMacros), MessageSink.Console);
	MP.AddMacros(typeof(LeMP.StandardMacros).Assembly);
	MP.PreOpenedNamespaces.Add((Symbol) "LeMP");
	MP.PreOpenedNamespaces.Add((Symbol) "LeMP.Prelude");
	
	// Approach #1: directly feed it Loyc trees
	var code = EcsLanguageService.Value.Parse("double Sqrt(notnull double x) ==> Math.Sqrt;");
	var output = MP.ProcessSynchronously(LNode.List(code));
	Console.WriteLine(EcsLanguageService.Value.Print(output));

	// Approach #2: use an InputOutput object (produces an output file by default)
	// (this will set the #inputFolder and #inputFileName properties, unlike Approach 1)
	UString code = "double Sqrt(notnull double x) ==> Math.Sqrt;";
	var io = new LeMP.InputOutput(code, "/Folder/FileName.ecs", EcsLanguageService.Value, 
		EcsLanguageService.Value.Printer, "OutputFile.cs");
	// Note: you could call ProcessParallel in case there are multiple input files
	MP.ProcessSynchronously(ListExt.Single(io), io2 => {
		Console.WriteLine(EcsLanguageService.Value.Print(io2.Output));
	});

In either case you'll probably need references to

- Loyc.Essentials.dll (for `Symbol`, `UString` and many other useful things)
- Loyc.Collections.dll (for `VList<LNode>` and `BMultiMap<TKey, TValue>`)
- Loyc.Syntax.dll (for `LNode` and `LesLanguageService`)
- Loyc.Ecs.dll (for `EcsLanguageService`)
- LeMP.exe (for `MacroProcessor` and `Compiler`)
- LeMP.StdMacros.dll (for standard macros)

LeMP itself references all these plus `Loyc.Utilities.dll` (for `UG.ProcessCommandLineArguments`)

### Q. My question isn't here!

Please leave your question on StackOverflow with the `lemp` tag and I'll receive a notification. You can also reach me by email at `gmail.com`, with account name `qwertie256`, or make an [issue on GitHub](https://github.com/qwertie/Loyc/issues).
