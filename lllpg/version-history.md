---
title: LLLPG Version History
layout: article
---

### LLLPG v1.5.1 (March 5, 2015) ### 

- LLLPG home page officially opens!
- LLLPG: Added `...` as synonym for `..`; `...` is preferred since character ranges are inclusive
- Changes to LLLPG itself are minimal, but [LeMP](http://loyc.net/lemp) has been updated to v1.5.1, which includes a richer set of macros (not really related to parsing), such as macros for pattern matching and algebraic data types.

### LLLPG v1.4.0 (Aug 25, 2015) ### 

- LLLPG: Fixed to support for aliases in code blocks e.g. rule Foo @{ "alias" { Process($"alias"); } }; now works when "alias" is an alias
- LLLPG: eliminated `lexer(option = value)` and `parser(option = value)` syntax; added support for `parser(option: value)` syntax in LES to match EC#. See `MacroContext.GetOptions()`
- LLLPG custom tool: reduced default timeout to 10 seconds

### LLLPG v1.3.2 (June 19, 2015) ### 

- Published LLLPG Part 5 article, which describes the new features in detail
- Standalone version: Replaced `BaseParser/BaseLexer` with dual-purpose `LexerSource/ParserSource` which can be used either as base classes, or as objects with LLLPG's `inputSource` and `inputClass` options.
- LoycSyntaxForVs.vsix now installs (and happens to work) in VS 2015 RC
- LES "Python mode" (ISM) installed and tested
- Implemented and tested `IndentTokenGenerator`
- Added `IIndexToLine` as part of `ILexer<Token>` because certain lexers don't have a `SourceFile`, and `BaseLexer` implements `IndexToLine` anyway
- Implemented VS syntax highlighter for Enhanced C# in Visual Studio 2010 to 2015
- LLLPG: Added `listInitializer: var _ = new List<T>()` option
- Added Demo Window to LeMP. Renamed `LllpgForVisualStudio.exe` => `LoycFileGeneratorForVS.exe`
- `ILexer<Token>` now has a type parameter; `NextToken()` now returns [`Maybe<Token>`](http://loyc.net/doc/code/structLoyc_1_1Maybe_3_01T_01_4.html) instead of `Token?` so that `Token` is not required to be a struct.
- Added [`BaseILexer<CharSrc,Token>`](http://loyc.net/doc/code/classLoyc_1_1Syntax_1_1Lexing_1_1BaseILexer_3_01CharSrc_00_01Token_01_4.html), and installed it as the new base class of LesLexer
- Added `BaseLexer.ErrorSink` property; default behavior is still to throw `FormatException`
- LLLPG Bug fix: `SavePosition` now prefixed by value of `inputClass` option.
- LLLPG Bug fix: `result:Terminal` did not automatically `return result`.
- LLLPG: Added `parser(CastLA(bool))` option; it defaults to `true`, which is usually needed when using `ParserSource<Token>`.
- Added `ParserSource<...>`; added optional `MatchType` parameter to `BaseParser` and `BaseParserForList`.
- Added TT type parameter to `ISimpleToken` and `IToken`. Normally `TT=int`.
- LLLPG: now produces C# `#line` directives (grammar actions only). Tweaked output header msg.
- LLLPG: braces no longer required after `LLLPG(...)` statement
- LLLPG: Added support for `$result` variable.
- LLLPG: Added basic support for inlining rules (`inline` keyword)
- LLLPG: Added support for `any foo in (foo ...)` expression, where `foo` refers to an attribute or word attribute on one or more other rules.
- LLLPG `StageOneParser`: started adding "!" suffix operator to resemble ANTLR's old feature; incomplete.
- LLLPG: Added `AutoValueSaverVisitor` to recognize labels & substitutions like `a:Foo b+:Bar Baz {$Baz}`, plus integration tests, and new `terminalType` option.
- Loyc.Syntax: Added `BaseParserForList` & `BaseParserNoBacktracking` (untested). Added `ISimpleToken` interface.
- Loyc.Syntax: added `CharSrc` type parameter to `BaseLexer<CharSrc>` as a way to avoid boxing during lexing. Added `LexerSource` and `LexerSource<CharSrc>` for use with LLLPG's new `inputClass` and `inputSource` options.
- LLLPG: added the `inputSource` and `inputClass` options to add flexibility, so that lexers and parsers no longer have to use `BaseLexer` / `BaseParser` as their base class.
- LLLPG: goto-labels improved in certain cases, e.g. in `(Foo | Bar)`, the labels will be `matchFoo` and `matchBar` instead of `match1` and `match2`
- LLLPG: Partly fixed nondeterministic bug in `PredictionAnalysisVisitor` that caused EC# parser to parse `alias(...)` calls as type alias constructs.
- LeMP: substantial enhancements, see my [LeMP article](http://www.codeproject.com/Articles/995264/Avoid-tedious-coding-with-LeMP-Part)

### LLLPG v1.1.0 (Feb 23, 2014) ### 

- Implemented complex ambiguity suppression behavior for `/` operator (described in part 4)
- Loyc: Removed dependency on nunit.framework.dll, replaced with Loyc.MiniTest
- Loyc: Added enum `Severity`. Changed `IMessageSink.Write(Symbol,...)` to `IMessageSink.Write(Severity,...)` 
Rebuilt LesSyntaxForVs2010 to match DLLs used by LLLPG 1.1.0 (for some reason the LLLPG SFG breaks if LES syntax highlighter uses different DLL versions, even though LLLPG has its own copy of all DLLs.)

### LLLPG v1.0.1 ###

- Bug fix (lexers): now calls `MatchExcept(set)` when inverted set contains `EOF`
- Bug fix (parsers): removed EOF from `MatchExcept(..., EOF)` 
- Bug fix: default can no longer change parser behavior except for bad input
- Increased max params for `Match(...)` from 3 to 4 
- Errors/warnings include string version of an alt if it is short 
- Added "`Line N: (...|...|...)`" comment to output for every Alts, and `[AddComments(bool)]` option
- Added more useful follow set info at `[Verbosity(2)]` and `[Verbosity(3)]`
- `Error(InputPosition + li, "...")` changed to `Error(li, "...")`

### LLLPG v1.0 (Feb 8, 2014) ###

- EC# support
- Demo and article updated
- Demo now uses EC# by default (LES version still included) and supports "mathy" expressions such as 2(2 + 5) => 14.

### LLLPG v0.9.1 (Nov 19, 2013) ###

- Updated demo to be a bit cleaner and to eliminate dependencies on Loyc libraries.
- Some bug fixes, a new alias(X = Y) command, and eliminated dependency on IntSet.

### LLLPG v0.9 (Oct 7, 2013) ### 

- Initial release with Part 1 article

