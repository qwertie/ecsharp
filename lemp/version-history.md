---
title: "LeMP & EC# Release Notes"
tagline: "Gathered from commit messages. Trivial changes omitted."
layout: article
---

See also: version history of [LoycCore](http://core.loyc.net/version-history.html) and [LLLPG](/lllpg/version-history.html).

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

