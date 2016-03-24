---
title: "LeMP & EC# Release Notes"
tagline: "Gathered from commit messages. Trivial changes omitted."
layout: article
redirectDomain: ecsharp.net
---

See also: version history of [LoycCore](http://core.loyc.net/version-history.html) and [LLLPG](/lllpg/version-history.html).

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

