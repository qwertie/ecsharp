---
title: "LeMP & EC# Release Notes"
tagline: "Gathered from commit messages. Trivial changes omitted."
layout: article
---

See also: version history of [LoycCore](http://core.loyc.net/version-history.html) and [LLLPG](/lllpg/version-history.html).

### v2.9.0.3 (a.k.a. v29): January 13, 2021 ###

Potentially breaking changes:
- Renamed `EcsCodeSymbols.InitializerAssignment` to `DictionaryInitAssign`
- Renamed `MacroMode.MatchIdentifier` to `MatchIdentifierOrCall` to clarify its behavior; the old identifier still exists and is marked Obsolete.

#### Enhanced C#: ####

- Added support for `switch` expressions and C# 8/9 patterns in the right-hand side of `is` (here's a [video about my work](https://youtu.be/ExJzi3MMiws) in that regard)
- Added infix operators `with {...}`, `when` and `where`
- Multiple empty type parameters (such as `Dictionary<,>`) can now be parsed and printed ([#125](https://github.com/qwertie/ecsharp/issues/125))
- EC# now has special-case code that allows it to print `ref var X = ref Y` instead than `ref var X = (ref Y)`, as the C# compiler rejects the latter.
- Added `|=>` and `?|=>` operators, with precedence between `=` and `??`. These operators don't currently do anything, as no macros are attached to them, but the intention is that `|=>` could be used as an assignment operator with reversed argument order (i.e. `A |=> B` means `B = A`). These operators are meant to resemble the pipe operators proposed in [issue #113](https://github.com/qwertie/ecsharp/issues/113). By analogy to the `?|>` operator (and its synonym `?>`), `A ?|=> B` should probably mean `A::tmp == null ? null : B = tmp`. 
- `?>` as synonym for `?|>` and allow `?=>` as synonym for `?|=>`
- When using `ParsingMode.Expressions`, the EC# parser no longer expects a newline between items and will not insert `%appendStatement` trivia when the expected newline isn't there
- Bug fix: Line numbers reported for errors located after the end of a triple-quoted string were wrong (too low).

#### LeMP macros: ####

- `compileTime` (and related macros such as `compileTimeAndRuntime` and `macro`) now map C# compiler errors back to the original code (as demonstrated in [this video](https://www.youtube.com/watch?v=Ue3W52iVH8c)
- Added the `macro` macro, which builds on `compileTime` to allow users to define macros at compile time that contain arbitrary logic. I made a [short video](https://youtu.be/S7uJ793H59w) about this, but it doesn't explain in much detail and I should probably made a more detailed video or article about it.
    - Known inconsistency: `macro` recognizes `using` directives from _before_ the `macro` block, but `compileTime` does not. `compileTime` was implemented first, and ignoring `using` directives made more sense for it, because `using` statements might refer to namespaces that don't exist at compile time, and because LeMP did not provide plumbing necessary to scan above anyway. I am considering whether to recognize `using` statements above `compileTime` in the next release, which is potentially a breaking change. Also, it has been proposed in C# itself to use a new ambiguous syntax for `using` that could cause problems for EC#, so `macro` might drop support for `using` directives inside its body.
- Enhanced `define` with the ability to generate unique variable names. Inside the body of a `define` block, identifiers are searched for the substring `unique#`, which is replaced with a unique number each time the macro is invoked; also, variables whose name starts with `temp#` are uniquely numbered.
- `compileTime` and `compileTimeAndRuntime` will now detect when a method has a `[LexicalMacro(...)]` attribute attached to it, and will add hidden code to register the macro so that it can be used immediately after the end of the `compileTime` block.
- `compileTime` and `compileTimeAndRuntime` now provide a predefined variable called `#macro_context` of type `LeMP.IMacroContext`, which points to the current macro context object.
- Added macros `rawCompileTime` and `rawCompileTimeAndRuntime`, which do not preprocess the code block and do not warn if they are nested somewhere they ought not to be
- When `#useSequenceExpressions;` is added to a file or block, `#runSequence` will work outside methods, which notably means that it will now work directly inside `compileTime`. The new behavior of `#useSequenceExpressions` is to assume it is in a method context until a type definition such as a class is encountered, which proves it is not in a method context.
- Fixed about 4 bugs in `#useSequenceExpressions`
- `#runSequence` now recognizes the contents of a single braced argument as a sequence (EC# syntax: `#runSequence { statement1; statement2; }`)
- When using `compileTime` or `precompute`, a `__macro_context` variable now exists
- Added `concat` macro
- Enhanced `concatId` macro to use `LNode.TextValue` when available.
- Added `#statement { ... }` macro, whose only job is to replace itself with its own argument(s). This is sometimes useful in order to switch to statement syntax in an expression context.
- Added `#preprocessArgsOf` and `#preprocessChild` macros
- `matchCode` supports the new `when` operator, e.g. `case $(id when id.IsId):`
- Bug fix ([#59](https://github.com/qwertie/ecsharp/issues/59)): `#useSymbols` no longer deletes attributes from the `Target` of braced blocks.
- Bug fix: now that C# supports `Foo(out int x)`, `#useSequenceExpressions` no longer needs to transform it (and won't). It was causing errors by changing code like `Method(out var x)` to `var x; Method(out x)`.
- Removed old name `use_symbols;`. Use `#useSymbols;` instead.
- Removed old name of binary `code==` operator macro, which was `tree==`

#### LeMP engine: ####

- Added `ICollection<Symbol> OpenMacroNamespaces` property to `IMacroContext`. This allows macros to change the set of open namespaces for the purpose of macro resolution. Changes apply only to the current braced block.
- Added `PreviousSiblings` and `AncestorsAndPreviousSiblings` properties to `IMacroContext`, with support in LeMP.
- LeMP now allows you to define macros that are called on every literal node, every identifier node, and/or every call node. For this purpose, new mode flags were added: `MacroMode.MatchEveryLiteral`,   `MacroMode.MatchEveryCall`, and `MacroMode.MatchEveryIdentifier`.
- A new macro flag `MacroMode.UseLogicalNameInErrorMessages` is used to show a clearer source location when a user-defined macro doesn't match. Previously, LeMP would show the true namespace and method, which was the same for every macro created with `define`.
- Added a new mode, `MacroMode.MatchIdentifierOnly`, which won't match calls. Introducing `MacroMode.MatchIdentifierOnly` will slightly reduce LeMP performance, which I didn't realize until the feature was nearly done.
- Added `#inputPath` scoped property, which has the input folder and input filename together
- When run without arguments, LeMP now reports its own location and full assembly name
- Bug fix in Visual Studio extension: `--outlang` and `--outext` options failed to change output language
- Bug fix: `#inputFolder` could be missing, or was a relative path, when LeMP was invoked on the command line. Now it is an absolute path.
- LeMP is now more willing to print notes from macros that reject their input. LeMP chooses a threshold for the minimum `Severity` a message must have in order to be printed after a macro call returns:
    - The default threshold is Warning
    - If a macro produces output, all messages are shown
    - If none of the matching macro(s) produce output for a particular node, the threshold drops to `NoteDetail` normally, or `InfoDetail` (which is between `Note` and `Debug`) if the node's base style is `NodeStyle.Special`.
- LeMPDemo:
    - `--macros:LLLPG.exe` is now included in the options list by default
    - Fixed a couple of visual bugs in the process of saving files
- Added `--eval` command-line option. Here are examples:

        # Bash on Windows and PowerShell
        LeMP.exe '--eval:define S($x) { You Said=$x; } S(2+1); S("Hello");' --inlang=.cs
        # Example (Windows cmd):
        LeMP "--eval:define S($x) { You Said=$x; } S(1+2); S(""Hello"");" --inlang=.cs

    Note: In single quotes, PowerShell treats two single quotes (`''`) as one, while in Bash you need `'"'"'` to get the same effect.

### v2.8.3: November 16, 2020 ###

EC# Parser/Printer:
- Add new binary operators `<=>`, `|>`, `?|>` (#113)
- `EcsValidators.SanitizeIdentifier` now allows long entity names such as _percnt and _period, which improves the appearance of plain C# output when an identifier with punctuation, such as ``@`%example` ``, is converted from Enhanced C# to plain C#.
- In support of a change to LeMP in this version, the printer now handles `#rawText()` and `#splice()`, with no arguments, as no-ops that produce no output, with the side effect that attached trivia can be printed. Note: `#splice()` is only recognized in a statement context, and currently arguments are not recognized (e.g. `#splice(Foo())` is printed as though it were a normal nested function call).

LeMP:
- In `compileTime` blocks, you can now use `MessageSink.Default` to report errors and warnings. Example: `compileTime { MessageSink.Default.Error("error location", "{0} is an error", "This"); }`
- LeMP has been changed to support attributes, and in particular, trivia, attached to `#splice()` calls (#122). If a `#splice()` node takes at least one argument, LeMP moves the attributes into the children while expanding the splice; most attributes are simply moved to the beginning of the attribute list of the first child, but trailing trivia are placed on the last child instead. If the `#splice()` call takes no arguments, attached attributes are ignored except for trivia. LeMP attempts to delete the `#splice()` node and add its trivia to the next or previous sibling. If there are no siblings, it attempts to create trailing trivia in the parent node. If this is not possible because there is no parent node, the empty `#splice()` becomes LeMP's output. Therefore, ideally, language printers will recognize and ignore `#splice()` except for the trivia attached to it. LeMP will behave this way regardless of whether the `#splice` was produced by a macro or was present in the original source tree.
- Bug fix: preserve trivia on `matchCode`, `precompute`, `saveAndRestore`, `unroll` macros (#122)
- Bug fix: a statement of the form `var x = #runSequence(...)` was being ignored in the context of `#useSequenceExpressions`
- The output of `#printKnownMacros;` now has word wrapping
- A NuGet-based LeMP command-line tool is now available in the LeMP-Tool package.

### v2.8.2: July 24, 2020 ###

LeMP:
- Add `*.exe.config` files in LeMP2.8.2.zip to avoid `FileLoadException` from Roslyn when attempting to use `compileTime` and `precompute` with LeMP (note: the VS extension never had this problem)
- Extension methods are no longer blocked in `compileTime`: I thought extension methods weren't allowed by C# Interactive engine. In fact they are only disallowed in classes. You'll get an odd-looking error if you attempt to use an extension method in a class inside `compileTime`; the C# interactive engine will complain that the class is "a nested class".

Enhanced C# printer/parser:
- Interpret `\u` differently to match C#/JS. In C#/JS/JSON, `\u` must be followed by 4 digits, and any additional digits will be ignored; previously `\u` could be followed by a code between 4 and 6 digits long. A new prefix `\U` has been introdued for codes between 4 and 6 digits, and `\u` is reserved for codes that are exactly 4 digits.

### v2.8.1: July 6, 2020 ###

EC# Parser/Printer:
- Support `#nullable` directive and the C# interactive directives `#r`, `#load`, `#cls`, `#clear`, `#help`, and `#reset` in the parser. The printer currently supports only `#nullable`, `#r` and `#load`.

Loyc.Ecs package:
- Add `Loyc.Ecs.EcsCodeSymbols` class with a handful of symbols moved from `CodeSymbols` (the versions in `CodeSymbols` are marked `[Obsolete]`), plus a handful of new symbols

LeMP:
- Make it easy to run arbitrary C# code at compile time ([#112](https://github.com/qwertie/ecsharp/issues/112)) with the new macros `compileTime {...}`, `compileTimeAndRuntime {...}`, `precompute()` and `rawPrecompute(...)`.
- Bug fix: `LeMP --help` didn't show help

Note: including Roslyn has ballooned the Visual Studio extension to six times its earlier size. Also, the .NET 4.5 version of LeMP and LLLPG changed to use .NET 4.7.2, but the core Loyc.\* libraries still use .NET 4.5.

### v2.8.0: July 3, 2020 ###

.NET 3.5 and .NET 4 versions have been dropped, leaving only .NET 4.5 and .NET Standard versions.

LeMP:
- The macro for the binary `:` operator now means "variable declaration" instead of "named argument". Use `arg <: value` for named arguments.

EC# Printer:
- Bug fix: the syntax tree for `({ block; })` now prints correctly ([#90](https://github.com/qwertie/ecsharp/issues/90))
- If an operator like `'+` is a method name, print it as `operator+` even if `%useOperatorKeyword` is missing

### v2.7.1.2: March 29, 2020 ###

- Fix regressions in EC# parser:
  - A variable declaration inside `case` (`case T x:`) could not be parsed
  - The optimized code path was broken such that a variable declaration
    like `T $x` could not be parsed
- Fix formatting in EC# printer:
  - Case statements did not always render nicely
  - Brace-expression could start with an unwanted space

### v2.7.0: February 17, 2020 ###

#### EC# (parser and printer):

- Mostly finish C# 7.0 support
  - Support tuple type names
  - Support `var (a, b, c) = ...`
  - Add basic support for "arrow" constructors and destructors. These are currently parsed as expressions instead of constructors, but they successfully round-trip.
  - Other C# 7 features such as variable declarations in `out` parameters, `ref` returns and binary literals were already understood
- Support C# 7.1's "default literal expression" (bare `default` keyword)
- Make `**` operator right-associative to match Python and JavaScript (despite the rarity of actually wanting it to be right-associative)
- The at-sign in `@verbatim` identifiers should now be preserved from parser to printer
- Lexer: remove the distinction between `HashId` and `NormalId`. The consequence is that `#foo*` now parses as two tokens `#foo *` instead of as a single token. Verbatim operators that begin with `@'`, such as `@'+=` and `@'hello!`, are still parsed as a single token.
- Fixed a spacing bug in `EcsNodePrinter.PrintId` where its output occasionally started with an unwanted space
- Fixed bugs involving scope resolution `::` operator:
  - `EcsValidators.IsComplexIdentifier` did not understand `::` before, which sometimes caused problems printing it
  - Max precedence when printing a type should be NullDot (not Primary)

#### LeMP:

- Added macros for converting LES3 code to C# (LeMP.Les3.To.CSharp namespace)
- `define` macro now works in LES
- Rename `LeMP.CSharp6` to `LeMP.CSharp6.To.OlderVersions` namespace to better indicate the purpose of its macros (down-conversion)
- Moved tuple macros to new `LeMP.CSharp7.To.OlderVersions` namespace
- `MacroProcessor.AddMacros` now returns the number of macros added
- `GetMacros` can now find macros by calling static methods that take no parameters and return a list (IEnumerable) of `MacroInfo`. This can be used to add the same macro to multiple namespaces. Example: [AliasedMacros()](https://github.com/qwertie/ecsharp/blob/master/Main/LeMP.StdMacros/Prelude.Les3.ecs#L17)
- Moved `GetMacros` from `LeMP.MacroProcessor` to `Loyc.Syntax.MacroInfo`.
- Bug fix: macros duplicated across namespaces are now recognized as being the same macro. This fix required substantial changes including the creation of the `SelectDictionaryFromKeys` adapter added recently to Loyc.Essentials.

### v2.6.8: May 12, 2019 ###

- Introduced .NET Standard 2.0 versions. NuGet package now contains four builds: .NET 3.5, .NET 4.0, .NET 4.5 and .NET Standard 2.
- LeMP demo window (WinForms) has been split into its own project (LeMPDemo.exe)
- v2.6.8.1 (May 13): Visual Studio extension (LeMP_VisualStudio.vsix) can now install in Visual Studio 2019.

### v2.6.5: February 17, 2019 ###

- Removed VS2010 support

### v2.6.3: July 23, 2018 ###

- Added new "is" syntax from C# 7. Removed special EC# syntax for `as` (e.g. `x as Y(a,b)` or `x as Y[a,b]`)  but added tuple notation for `is` (e.g. `x is Y(a, b)`).
- EC#: Add support for preserving `#region/#endregion` in output.
- EC#: Add support for methods named `operator true` and `operator false`
- LeMP: Reconcile `match` macro with new `is` syntax of C# 7
- Bug fix (EC#): .ecs file extension did not use EC# output mode.
- Bug fix (EC#): trivia between an [attribute] and a variable declaration wasn't preserved correctly because the #var's Range failed to include its attributes.

### v2.6.0: August 30, 2017 ###

- **Enhanced C#**: Bug fix: double pointer types like `Foo** x` were incorrectly parsed as exponentiation expressions.

### v2.5.3: March 26, 2017 ###

- **LeMP**: Reprogrammed `Assembly.Load()` to find assemblies that are already loaded, so that `--macros:CustomMacro.dll` works inside Visual Studio.

### v2.5.2: February 17, 2017 ###

#### LeMP

- `IMacroContext.PreProcess`: added boolean `resetProperties` parameter
- `SetOrCreateMemberMacro` (e.g. `void SetX(public T x) {}`): now transfers comments associated with the parameter to the newly created field
- Added `macro_scope {...}` and `reset_macros {..}` macros

#### Enhanced C#

- Added LINQ support in parser and printer (finally!)
- Fixed parse error on `operator>>` and `operator<<`.
- Fixed regression: lexer no longer recognized UTF BOM.
- Fixed reported error message locations in lexer.
- Fixed EC# to recognize `goto default` instead of `goto case default` which was never a thing.

### v2.4.2: January 8, 2017 ###

#### LeMP

- Added `includeFileBinary("file")` and `includeFileText("file")`
- Added `#lines` macro for adding C# `#line` directives to output
- LeMP bug fix: right side of LeMP demo now fully refreshed when number of lines decreases

#### Enhanced C# syntax

- Printer now supports `#trivia_C#PPRawText` for adding preprocessor lines, used by `#lines` macro.
- Printer now supports `IEnumerable<byte>` literals generated by `includeFileBinary`

### v2.4.0.1: December 26, 2016 ###

#### LeMP

- Added new macros: `#deconstruct` aka `static deconstruct` (I'm having trouble choosing a naming convention), `#tryDeconstruct`, `$varName`, `static matchCode` aka `#matchCode`, and the binary `staticMatches` operator.
- `unroll()` now runs macros on the right-hand side of `in` if the right-hand side is not already some kind of list (tuple, splice or braced block).
- Renamed operator `tree==` to `code==`. The old name still works, for now.
- `replace` and `define` now allow you to use `$(..args)` rather than `$args` in the replacement expression.
- `nameof()` macro moved to `LeMP.CSharp6` namespace so it doesn't run by default.
- `?.` macro now creates temporary variable if needed. Output of `?.` macro is now surrounded by parentheses.
- Bug fix in `quote {...}` macro: `.SetStyle()` was called on `$substituted_variables`.
- Fix regression so `#useDefaultTupleTypes` works without an arg list. 

#### Enhanced C# syntax

- Changed the meaning of `A.B<C>` from `#of(A.B, C)` to `A.(#of(B,C))` to synchronize it with LES.
- Synchronized parser to printer so that an attribute list like `([] foo)` suppresses `#trivia_inParens`.
- Printer refactored.

### v2.3.3: December 21, 2016 ###

- EC#: Support `throw`, `return`, `break`, `continue`, `goto`, and `goto case` as expressions as well as statements

### v2.3.2: December 12, 2016 ###

- Method-style `replace` is now known as `define` (but `replace` still works)
- Original `replace` now simply removes the braces in case of `replace (... => {...})` instead of changing to `=> #splice(...)`, and `unroll` now treats `... in #splice(...)` like a tuple.
- Ecs project file renamed to Loyc.Ecs to match output assembly

### v2.3.1: December 11, 2016 ###

- Note: [Loyc.Essentials was split](http://core.loyc.net/version-history.html) into Loyc.Essentials and Loyc.Math.
- Bug fix: `replace` now avoids a fatal `StackOverflowException` in case you write `replace($x => ...)` (which matches everything)

### v2.1.2: December 6, 2016 ###

- Bug fix in handling LeMP command-line args that caused `LeMP --editor` to throw on startup.


### v2.1.1: December 4, 2016 ###

- Macro processor now pays attention to `MacroMode.MatchIdentifier`.
- EC# & LES printers now ignore obsolete trivia like `#trivia_SLCommentBefore` (use `#trivia_SLComment` instead).

### v2.0.0: November 23, 2016 ###

- `IParsingService` and other APIs were changed in v2.0 and v2.1; for details see the [LoycCore release notes](http://core.loyc.net/version-history.html).
- EC# parser no longer uses `SourcePos` for the context of `IMessageSink.Write()`; `SourceRange` is used instead.
- EC# bug fix: `delegate() {...}` did not copy properly to output file
- EC# aesthetic fix: Comments/newlines after attributes now appear _after_ ']'

### v1.9.6: November 23, 2016 ###

- Added new method-style `replace` macro.
- Old `replace` macro: if either side of `=>` is in braces, the braces are now ignored even when the other side is not in braces.
- LeMP: Added `IMacroContext.RegisterMacro()` and `#registerMacro()` macro. Macros registered by other macros are scoped to the current braced block.
- EC# bug fix: properties tended to grab trivia that they didn't own.
- EC# bug fix: property `where` clause, if any, comes _after_ argument list, if any.

### v1.9.5: November 14, 2016 ###

- EC# printer: fixed regression where `(#return(x))` could be printed `(return x` with a single parenthesis
- LeMP.exe: fixed regression: output was not written sometimes (workaround: `--noparallel`)
- EC#: add `#C#PPRawText` node for guaranteeing that the text appears on a line by itself despite `NodeStyle.OneLiner` on a parent (used by LLLPG for `#line` directives).

### v1.9.4: October 25, 2016 ###

- Enhanced C# can now preserve comments and newlines. Note: by default, the printer now writes fewer newlines when no trivia is present, since newlines can be added with `#trivia_newline`.
- EC# printer: unknown trivia is no longer dropped by default in EC# mode; it will be printed out.
- EC# lexer no longer produces `Spaces` tokens.
- LeMP.Compiler: add PreserveComments and ParsingMode options (--preserve-comments:false available on command line)
- LeMP: SetOrCreateMember macro now keeps attributes on the argument unless you mark the attribute with `field:` or `property:`.
- Added a few minor extension methods
- Fixed a few bugs related to source ranges and trivia handling
- EC# bug fix: attributes like `[A] [foo: B]` produced an attribute list like `[foo: A] [foo: B]`

### v1.9.2: September 3, 2016 ###

- Only [core libraries](http://core.loyc.net/version-history) changed

### v1.9.0: July 26, 2016 ###

- Names of operators in LES and EC# now start with an apostrophe (`'`)
- Bux fix in LES->EC# conversion of `for` loop with empty clauses.
- Bug fix: `#useSymbols` crashed if it encountered `using X = Y`, and didn't work with LLLPG

### v1.8.1: June 13, 2016 ###

- Introduced `#ecs` as shorthand for `#useSymbols; #useSequenceExpressions;`
- `with(...) {...}` can now be used as an expression in conjunction with `#useSequenceExpressions` (or `#ecs`).
- Moved `NextTempCounter` from `StandardMacros` to `MacroProcessor` and `IMacroContext`.
- `MacroProcessorTask` now expands `#splice` earlier, so that when a macro is used inside a splice, `RemainingNodes` shows later items from outside the `#splice`.
- `#useSymbols` can now be used outside any class; it defers `Symbol` declarations until a type declaration starts
- Bug fix in `#useSequenceExpressions`: it didn't work in a `class` or `struct` when `#useSequenceExpressions` was located outside the class.

EC# parser & printer:

- Parser now handles `([] L<T> x)` as a var declaration, and printer prefers this output format over `#var(...)`.
- EC# printer: removed annoying newlines in autoproperties
- Bug fix: parser ignored the `new` attribute in most cases.
- Bug fix: `/*suffix comments*/` were printed incorrectly (with no content).
- Bug fix: `for` loops now support multiple expressions in the "init" and "increment" parts.
- Bug fix: fixed miscellaneous printing problems so that test suite passes.

### v1.7.6: May 17, 2016 ###

- Completed `#useSequenceExpressions`, which enables 
  - the quick-bind operator `::`
  - in-situ declaration of `out` and `ref` variables as in `int.TryParse(s, out int x)` (**note:** data type is mandatory due to limitations of LeMP)
  - `#runSequence`, which runs a sequence of statements inside an expression, and is designed to be used by other macros in the future.
- `SetOrCreateMember` macro: refactored; bug fix: added logic to support constructors that call other constructors.
- Bug fix: `EcsLanguageService.Print` wouldn't use plain C# mode

### v1.7.5: April 29, 2016 ###

- EC# printer now sanitizes identifiers in plain C# mode (e.g. `foo'` => `foo_apos`)
- Renamed `ParsingMode.Exprs` => `Expressions`, `ParsingMode.Stmts` => `Statements`

### v1.7.4: April 18, 2016 ###

- `with` macro now recognizes `#` as the "current object"
- Added `saveAndRestore()` macro
- Changed `on_return` to preprocess the code that follows it
- Reclassified `unless` as a standard macro (LeMP namespace)
- EC# parser: enabled support for C# 5 await
- EC# parser: block-call expressions now allow a token literal @{}, not just braces (for LLLPG ANTLR mode)
- EC# parser: Refactored, and completely changed the strategy of how to distinguish which expressions are variable declarations. Changed Loyc tree for event declarations. 
- EC# parsing fix: extension methods were broken because test suite didn't test them
- Bug fix: `(Foo) * x` is now parsed as a multiplication (TODO: change `(int) * x` to be parsed as a cast)
- Bug fix: printer now supports `goto case default;`

### v1.7.2: April 1, 2016 ###

- Changed EC# triple-quoted strings to allow up to three extra spaces or one extra tab after the initial indent.
- Changed public interface of `LeMP.Compiler` to make it a bit more flexible and easier to use
- `NullDotMacro` moved into `LeMP.CSharp6` namespace, which disables it by default.

### v1.7.1: Mar 22, 2016 ###

- Unveiled the [Macro Reference Manual](reference.html)
- Added macros: `includeFile` (aka `#include`), `#set` (aka `#setScopedProperty`), `#snippet` (aka `#setScopedPropertyQuote`), `#get` (aka `#getScopedProperty`), and `replacePP` (`replace` plus preprocessing of initial parameters).
- `MacroProcessorTask` introduces two global-scoped Symbols, `#inputFolder` and `#inputFile`.
- Eliminated `#haveContractRewriter` and `#setAssertMethod` macros, since you can now just use `#set #haveContractRewriter` and `#snippet #assertMethod = AssertMethod;`
- Renamed `on_error_catch` to `on_throw_catch` so that it's made of keywords.
- Code contracts:
    - Introduced `[ensuresFinally]`, which checks a postcondition in `finally`
    - Changed Contract Attribute error messages to match MS Code Contracts
    - Code contract attributes now support lambda functions
- Changed public interface of `LeMP.Compiler` to make it a bit more flexible and easier to use
- `ParsingService`: added global language "registration" feature.
- Loyc.Essentials: added `TryGetValue` extension method for `IReadOnlyDictionary`.
- Misc., e.g. renamed some members of `Loyc.Ecs.Parser.TokenType`
- Bug fix to `IMacroContext`: made macro dictionary _fully_ immutable

### v1.7.0: Mar 18, 2016 ###

- Renamed `Localize.From` => `Localize.Localized` and made it an extension method
- Forwarding macro `==>` now recognizes `_` as name of current method/property (synonym of `#`).
- Refactored EC# parser.

### v1.6.0: Mar 9, 2016 ###

- Factored standard macros into their own assembly
- LeMP: Changed the syntax of multi-`using`
- Bug fix in handling of `in` in `match` pattern matching
- Bug fix: Despite appearances, `StreamCharSource` did not actually use UTF8 encoding by default

### v1.5.1: Mar 5, 2016 ###

- Renames: `EcsNodePrinter.AllowChangeParenthesis` => `AllowChangeParentheses`, `CodeSymbols.Cons` => `Constructor`.
- LeMP: Added macros for `in`, `..<`, `...`;
- `LogException` now puts "Context" & "Severity" in `Exception.Data`
- LeMP: Renamed `on_catch` to `on_error_catch` for clarity.
- LeMP: Introduced macro for constructors named `this` .
- LeMP: Enhanced `alt class` usability by preserving `where` clauses in derived types. 
- LeMP: Refactored `MacroProcessorTask` to use call stacks that are less deep.
- EC# parser: Added property-as-expression syntax. Mainly this is to support constructor+property syntax like `this(int Foo { get; } = 0) {}`, but in fact properties are now allowed wherever variable declarations allowed.
- EC#: Introduced `EcsValidators` class (factored out of `EcsNodePrinter`)

### v1.5.0: Mar 2, 2016 ###

- Renamed Ecs.exe to Loyc.Ecs.dll, and added new Main/Test project.
- Renamed `RVList` to `VList` throughout the codebase, and renamed `RWList` to `WList`.
- Renamed namespace `Ecs` to `Loyc.Ecs` and moved `EcsLanguageService` from `Ecs.Parser` to `Loyc.Ecs`.
- Renamed `ParsingService.PrintMultiple` to `Print` (easier to remember).
- LeMP: Added UsingMulti macro for `using` multiple namespaces
- LeMP: `ForwardingMacro` no longer generates a setter for `T Prop ==> expr`. Rationale: the macro has no idea whether or not a setter exists, and the visual similarity to `=> expr` subtly suggests that no setter is generated.
- EC#: Added support for C# 6 syntax; in `#catch(eVar, when, {...})`, inserted new `Args[1]`  for `when` condition
- EC# parser: allow enums to have a name that is a `ComplexNameDecl` rather than just a simple `Id` (EDIT: broken?)
- EC# parser: the argument of a `using` directive is now parsed as an expression.
- EC# bug fix: Added support for properties with arguments, e.g. `T this[int x, int y] {...}`, by adding a third parameter in #property for all properties (e.g. `#property(T, this, #(int x, int y), {...})`)

### v1.4.1: Feb 28, 2016 ###

- Updated the `replace` macro and the `LNodeExt.MatchesPattern` method to support `$(..p)`, in addition to the existing `$(params p)` syntax, as another way to match multiple parameters or statements.
- `matchCode`: Eliminated the old `$(x(condition))` syntax in favor of `$(x && condition)`, for consistency with the `match` macro. The other old syntax `$(x[condition])` still works.
- LeMP: Added pattern-matching `match(...) {...}` macro!
- EC# parser: Added `...` operator (and `..<` as synonym of `..`). Fixed a bug where some error messages showed an incorrect location.
- LES: Edited `ParseIdentifier()` not to support legacy ``#`...` `` identifiers.
- EC# parser: In order to support a [pattern-matching macro](pattern-matching.html), added a unary `is` operator, and tried to add support for parsing `X is Foo(...)` as `(X is Foo)(...)`, while increasing the precedence of `is` and `as` on the right side of the operator. Sadly, parsing of `X as Foo < Y` is broken due to a bug in LLLPG that I can't figure out how to fix. Increased the number of places where uninitialized variable declarations are allowed.
- LLLPG issue #13 (`GeneralCodeGenHelper`): `EOF` is now casted before comparing with laN, e.g. `la0 == (LaType)EOF`
- `RunTests` Now prints reason of `Fails` option on-screen

### v1.4.0: Aug 25, 2015 ###

- Wrote and tested `matchCode` macro.
- Wrote and tested `alt class`, a macro for building algebraic data types
- `use_symbols` (later renamed to `#useSymbols`) now gives valid identifier names to symbols that contain punctuation.
- Changed EC# parser so pattern matching `class X:$(..bases) {}` works even if there are no base classes.
- Changed code `quote` macro to avoid needing an `LNodeFactory F`.
- Bug fix: certain macros did not enable reprocessing of children
- Bug fixes in `quote`/`rawQuote`, `matchCode`, and `SetOrCreateMember` macros; and in printer and parser.

### July updates: forgot to raise version number ###

- Added `use_symbols` macro (later renamed to `#useSymbols`) to generate field declarations for `@@symbols` in LES/EC#
- LeMP: tuples: added support for deconstruction + declaration: `(var x, var y) = tuple`
- LeMP: `nameof()`: split into `nameof()` and `stringify()`, with `nameof()` matching C# 6 behavior
- Oops, `LNodeExt.FindAndReplace()` and `LNode.ReplaceRecursive()` are duplicates. Deleted `FindAndReplace()`
- Updated LoycFileGeneratorForVs.exe to be able to register itself in VS 2015
- All LES-based parsers, lexers and unit tests throughout the codebase have been updated to use LESv2 syntax.
- VS extension: Fixed LES syntax highlighting of superexpressions & function calls.
- Split the symbol for indexing and array types into two separate symbols `_[]` (IndexBracks) and `[]` (Array), where previously `[]` (Bracks) was used for both.
- Bug fixes in EC# printer.

### v1.3.2: Jun 21, 2015 ###

- LoycSyntaxForVs.vsix now installs (and happens to work) in VS 2015 RC
- Bug fix: culture-sensitive number parsing (tests would break on machines not using "." as number separator)

### v1.3.1: Jun 14, 2015 ###

- LeMP: Added `#printKnownMacros` macro, `IMacroContext.AllKnownMacros` property
- Tuples: `set_tuple_type(TupleSize, BareName, Factory.Method)` and `use_default_tuple_types()` are now scoped to the current braced block. `Pair.Create` is no longer used by default (only `Tuple.Create`).
- Adjusted EC# parser to accept `Type $name;` as a variable decl
- Added Demo Window to LeMP.
- Renamed LllpgForVisualStudio => LoycFileGeneratorForVS.

### v1.3.0: May 27, 2015 ###

I'm not attempting to make release notes this far back.

The [LLLPG vers	ion history](/lllpg/version-history.html) does go back further.

