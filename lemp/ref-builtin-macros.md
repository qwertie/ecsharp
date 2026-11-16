---
title: "LeMP Reference: Built-in macros"
layout: article
date: 20 Mar 2016
toc: true
---

**Note**: The `#` character in macro names has two meanings. Usually it means "this macro produces no direct output and will disappear from the output file." The other meaning is "this is one of the global macros that is always available and cannot be un-imported."

Scoped Properties
-----------------

Some of these macros refer to "scoped properties" which is actually the [`IMacroContext.ScopedProperties`]() collection that all macros have access to. When one macro changes a scoped property, the changes are visible to other macros that are processed later (i.e. later statements, or later parts of an expression). However, they are scoped, meaning that any changes you make to them are not visible outside the current braced block. Conceptually, a closing brace "reverts" any changes that were made inside the braces.

Scoped properties whose key is a [`Symbol`](http://localhost:4000/doc/code/classLoyc_1_1Symbol.html) that starts with `#` are reserved for use by built-in macros and standard macros.

By default, there are two globally-scoped properties, `#inputFileName` and `#inputFolder`. It is possible to create additional globally-scoped properties with the `--set` and `--snippet` command-line options (e.g. `--set:#haveContractRewriter=true` or `--snippet:#assertMethodForRequires=Contract.Requires`).

Global macros (always available)
--------------------------------

### \#importMacros ###

	#importMacros(namespace);
	import_macros(namespace); // alias for #importMacros
	import_macros namespace;  // LES only
	
LeMP will look for macros in the specified namespace. Only applies within the current braced block. **Note**: normal C# `using` statements can also cause macros to be imported.

### \#unimportMacros ###

	#unimportMacros(namespace1, namespace2)

Tells LeMP to stop looking for macros in the specified namespace(s). Only applies within the current braced block.

### \#noLexicalMacros ###

	#noLexicalMacros(expr);

Suppresses macro invocations inside the specified expression. The word `#noLexicalMacros` is removed from the output. 

### \#setScopedProperty ###

	#setScopedProperty(keyLiteral, valueLiteral);

Sets the value of a scoped property, using the first parameter (usually a `@@symbol`) as a key in the property dictionary. The key and value must both be literals; expressions are not supported.

**Note:** Usually `#set` is used instead.

### \#setScopedPropertyQuote ###

	#setScopedPropertyQuote(keyLiteral, valueCode);
	
Sets the value of a scoped property to an `LNode`, using the first parameter (usually a `@@symbol`) as a key in the property dictionary. The key must be a literal, while the value can be _any_ expression.

**Note:** Usually `#snippet` is used instead.

### \#getScopedProperty ###

	#getScopedProperty(keyLiteral, defaultCode);

Replaces the current node with the value of a scoped property. The key must be a literal or an identifier. If the key is an identifier, it is treated as a symbol instead, e.g. `KEY` is equivalent to `@@KEY`. If the scoped property is an LNode, the code it represents is expanded in-place. If the scoped property is anything else, its value is inserted as a literal. If the property does not exist, the second parameter is used instead. The second parameter is optional; if there is no second parameter and the requested property does not exist, an error is printed.

**Note:** Usually `#get` is used instead.

Other built-in macros (LeMP.Prelude namespace)
----------------------------------------------

### noMacro ###

	noMacro(Code)

Alias for `#noLexicalMacros`. Passes code through to the output language, without macro processing.

### #get ###

	#get(key, defaultValueOpt)

Alias for `#getScopedProperty`. Gets a literal or code snippet that was previously set in this scope. If the key is an identifier, it is treated as a symbol instead, e.g. `#get(Foo)` is equivalent to `#get(@@Foo)`.

### \#preprocessArgsOf ###

	#preprocessArgsOf(macroCall(...))

Preprocesses all arguments of `macroCall` before `macroCall` itself runs. Normally LeMP runs a macro first and then processes its output, so the arguments of a macro are usually not preprocessed when the macro sees them. This macro works around that limitation when a macro needs to see the expanded form of its arguments.

**Caution**: if an argument contains a macro that queries its Parent node, the parent will be `#preprocessArgsOf` rather than `macroCall`. Also, the arguments may be preprocessed twice: once by `#preprocessArgsOf`, and again afterward if `macroCall` includes its arguments in its output (unless `macroCall` blocks further processing).

### \#preprocessChild ###

	#preprocessChild(paths, syntaxTree(...));
	#preprocessChild(paths in syntaxTree(...)); // EC# syntax

Like `#preprocessArgsOf`, but preprocesses only specific parts of `syntaxTree`. `paths` can be

1. an integer indicating which child to preprocess, where 0 is the first argument and -1 is the Target;
2. the Name of a child to preprocess (only the first child with that name is processed);
3. a tuple containing a list of children to preprocess; or
4. a path to a deeper child, consisting of a series of these items separated by the `/` operator.

For example, in `#preprocessChild(0 / (hi, -1), { greetings(hi(Fred), hello); })`, the path `0 / (hi, -1)` means "inside child 0, preprocess the child whose name is `hi` and the child whose index is -1". Child 0 is the first statement inside the braced block, `greetings(hi(Fred), hello)`. Inside that, `hi` refers to `hi(Fred)` and -1 refers to the Target, `greetings`.

The cautions that apply to `#preprocessArgsOf` also apply to `#preprocessChild`.

### \#statement ###

	#statement { statements; }

Replaces itself with the contents of the braced block, e.g. `#statement { x = 1; }` becomes `x = 1`. This allows statements to be written in an expression context. If there are multiple statements, they are enclosed in a `#splice()` node, e.g. `#statement { x = 1; y = 2; }` becomes `#splice(x = 1, y = 2)`.

### \#set and #snippet ###

	#set Identifier = literalValue; 
	#set Identifier;  // the default literal value is true
	#snippet Identifier = expression;
	#snippet Identifier = { statement(s); }; 

Sets an option, or saves a snippet of code for use later. #set is a synonym for #setScopedProperty, while `#snippet` is a synonym for #setScopedPropertyQuote. `#get` can be used to get the saved value or code snippet, but `#set` and `#snippet` are most often used to set options recognized by other macros. For example, 

### import_macros ###

	import_macros Namespace; // LES syntax
	import_macros(Namespace); // EC# syntax

Use macros from specified namespace. 

### printKnownMacros ###

	#printKnownMacros;

Prints a table of all macros known to LeMP, including macros that have _not_ been imported, along with the documentation of each macro. The output is not valid C# code. `printKnownMacros` and `#help` are synonyms.
