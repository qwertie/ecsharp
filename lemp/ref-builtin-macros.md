---
title: "LeMP Reference: Built-in macros"
layout: article
date: 20 Mar 2016
toc: true
redirectDomain: ecsharp.net
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

Pass code through to the output language, without macro processing.

### #get ###

	#get(key, defaultValueOpt)

Alias for `#getScopedProperty`. Gets a literal or code snippet that was previously set in this scope. If the key is an identifier, it is treated as a symbol instead, e.g. `#get(Foo)` is equivalent to `#get(@@Foo)`.

### import_macros ###

	import_macros Namespace

Use macros from specified namespace. The 'macros' modifier imports macros only, deleting this statement from the output.

### printKnownMacros ###

	printKnownMacros;

Prints a table of all macros known to LeMP, as (invalid) C# code.

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

	printKnownMacros;

Prints a table of all macros known to LeMP, including macros that have _not_ been imported. The output is not valid C# code.
