---
title: "Enhanced C# for PL Nerds, Part 4: Future Features, Help Wanted"
layout: article
toc: true
---

## Aliases

Aliases are the minor tweak to the type system that I mentioned before.


## The future

- Resource imports & diagrammatic programming
- Slices & ranges
- Extensible syntax


## Help wanted

Editor features:

Tooltips and F1 help to explain the meaning of operators and other punctuation syntax such as ??, [[, and @@{...}.

Smart syntax coloring based on the syntax tree. It may be expensive to run the macros of an EC# program, so syntax highlighting should be based on the syntax tree as much as possible. Existing C# syntax coloring is based mostly on token types or very superficial syntax features; I'd like to see highlighting like this:

<demo> we must write a colorizer anyway for these articles
	struct Point<$T>
	{
		static readonly Point Empty = new Point<T>();
		[[set]] public new(
	}
	
	- Type names are italicized or have a different background color
	- Different levels of parenthesis should be highlighted differently.
	- Definitions are highlighted differently from usages, e.g. the x in "int x" 
	  should be highlighted differently from the x in "x++", and the "Foo" in
	  "void Foo();" should be highlighted differently from a call to Foo().
	- Modifier words (public, partial), type keywords (int) and other keywords 
	  can all use different colors.
	- In strings, escape sequences and substitutions can be highlighted

Editor wishlist:
	- Different font sizes: smaller font for large method bodies, larger font for class-level declarations
	- region("comment") { ... }, details("comment") { ... } macros detected by editor. "(expr_in_parens);" for one-line minutia
	- Lines that contain only a single { or } should be slightly reduced in height
	- Context gathered at the top of the screen
	- Multiple keyboard interface styles: Visual Studio, vi, emacs
	- Tabs for indentation, spaces for alignment by default
	- Elastic tab stops
	- Rectangular selections exactly as in VS2010

## The end

- "protected override void Finalize()" finalizer syntax

I would like to thank John McCarthy, the D folks and bearophile's random links on the D forums for many of the interesting ideas that are planned for EC#.


## Overview of the EC# compilation process

The phases of compilation are

1. Parsing (source code is lexed and parsed into an abstract syntax tree).
2. Building the program tree.
3. Semantic analysis and building the executable code.
4. Converting EC# code to plain C# or a .NET assembly (the back-end).

Phases 1 to 3 are collectively known as the front-end. The parser is quite separate from the other phases, but phases 2 through 4 may overlap, because in order to build the program tree, phase 2 requires some of the executable code to be built in advance, and it may (theoretically) use the back-end to help run code at compile-time.

Thus, the compiler's most difficult responsibility is to convert the syntax tree to a "program tree", which is a tree of "spaces" and "members".

1. Spaces are addressable code containers, such as namespaces, classes, and aliases, that can contain members and other spaces. "Addressable" means that every space has a unique name in the tree, and any part of a program can refer to a particular space. All data types are spaces, but some spaces are not data types. Aliases are a special kind of space that refers to another space, optionally creating a new perspective on that space.

2. Members are other named code elements defined in spaces, such as methods, fields, and properties. Members cannot contain spaces (although a macro could be used to simulate creating a local data type inside a method.) The most important kind of member is the method, which contains executable code.

The program tree is like a file system in which spaces are folders, members are files, and aliases are symbolic links. Some spaces and members can be composed from multiple syntax trees; for example, two "partial class" definitions with the same name are combined into a single space. Thus, the program tree is a separate entity derived from the syntax tree, but it refers back to some of the code that the syntax tree contains.

So far, this is all very similar to plain C#, but the program tree can also, temporarily, contain executable code outside of any member; in particular it can have macro calls. A macro is executed at compile-time to produce new code that replaces the macro call. The new code, in turn, can also contain macros, which are called at compile-time.

A macro is just a method with the [Macro] attribute. When the compiler determines that a method call refers to a macro, it "quotes" the macro's arguments instead of evaluating them; these arguments, which have a data type of "Node", are passed to the macro, then the macro is executed, and then the compiler replaces the macro call with whatever code the macro returned.

"Node" is a real data type that can be used at runtime as well as compile-time if necessary, and can represent any syntax tree whatsoever, including an infinite variety of syntax trees that would be meaningless to the EC# compiler.

EC# also calls non-macro methods at compile-time inside any "const" context, e.g. when computing the value of a "const" variable.

Once the program tree is complete, EC# builds any executable code that wasn't built in advance (phase 3), and then it converts the program tree to an output language (phase 4). 






CTCE will be limited to a subset of EC#, and this subset will slowly expand as EC# is developed. Eventually the goal is to allow you to run any safe code (i.e. code that is not marked unsafe) that does not access global (static) variables, subject to restrictions on the use of external assemblies (by default, only certain whitelisted BCL classes will be accessible, e.g. you will not be able to access the file system at compile-time).
