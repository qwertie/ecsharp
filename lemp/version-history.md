---
title: "LeMP & EC# Release Notes"
tagline: "Gathered from commit messages. Trivial changes omitted."
layout: article
---

See also: version history of [LoycCore](http://core.loyc.net/version-history.html) and [LLLPG](/lllpg/version-history.html).

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
- `use_symbols` now gives valid identifier names to symbols that contain punctuation.
- Changed EC# parser so pattern matching `class X:$(..bases) {}` works even if there are no base classes.
- Changed code `quote` macro to avoid needing an `LNodeFactory F`.
- Bug fix: certain macros did not enable reprocessing of children
- Bug fixes in `quote`/`rawQuote`, `matchCode`, and `SetOrCreateMember` macros; and in printer and parser.

### July updates: forgot to raise version number ###

- Added `use_symbols` macro to generate field declarations for `@@symbols` in LES/EC#.
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

