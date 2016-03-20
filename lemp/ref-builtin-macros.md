---
title: "LeMP Reference: Built-in macros"
layout: article
date: 17 Mar 2016
toc: true
---

### \#importMacros ###

	#importMacros(namespace);
	import_macros(namespace); // alias for #importMacros
	import_macros namespace;  // LES only
	
LeMP will look for macros in the specified namespace. Only applies within the current braced block. **Note**: normal C# `using` statements also import macros.

### \#unimportMacros ###

	#unimportMacros(namespace1, namespace2)

Tells LeMP to stop looking for macros in the specified namespace(s). Only applies within the current braced block.

### \#noLexicalMacros ###

	#noLexicalMacros(expr);
	noMacros(expr);

Suppresses macro invocations inside the specified expression. The word `#noLexicalMacros` is removed from the output. Note: `noMacro` (in LeMP.Prelude) is a shortened synonym for this macro.

### \#printKnownMacros ###

	#printKnownMacros;
	
Prints a table of all macros known to LeMP, as (invalid) C# code.
