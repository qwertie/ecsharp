---
title: LLLPG Version History
layout: article
---

_Note_: some version numbers are skipped because the LLLPG version number is synchronized with LeMP/EC#, which iterate more frequently.

### LLLPG v2.3.1: December 11, 2016 ###

No functional changes. Updated to match LoycCore and LeMP.

### LLLPG v1.9.5: November 14, 2016 ###

- LLLPG now bundles four `case`s per line when generating a `switch` statement.
- LLLPG.exe: fixed regression: output was not written sometimes (workaround: `--noparallel`)

### LLLPG v1.9.4: October 25, 2016 ###

- Generally, EC# can now preserve newlines and comments. However, this doesn't work inside `LLLPG @{}` blocks or individual rules because token trees were not designed to deal with trivia.
- Added a newline between rule methods.
- Bug fix (regression): while turning off hoisting by default for semantic `&{...}` predicates, hoisting was accidentally forced off for syntactic `&(...)` predicates too (even though the latter doesn't even expose any way for users to do that).
- Bug fix: `#line 0` was sometimes written before `return result`, which is a C# compiler error.

### LLLPG v1.9.2: September 3, 2016 ###

- `&{[Local]}` is now the default, rather than `&{[Hoist]}`. Semantic (`&{...}`) predicates are no longer hoisted into other rules by default, but syntactic predicates (`&(...)`) still are.
- Bug fix in `BaseLexer` : error messages were broken when the expected set was a `HashSet`.
- Bug fix: `out` is no longer stripped out of `out $grammarLabel`

### LLLPG v1.8.1: June 13, 2016 ###

- To avoid weird problems, added a check to ensure LLLPG keywords are not used as rule names
- Bug fix: When processing bad grammars, one source of stack overflow has been fixed. This is important since stack overflows crash Visual Studio as a whole.

### LLLPG v1.8.0: May 21, 2016 ###

- LLLPG: make `--help` work again, and fix a bug where `token` could be treated like `rule`
- LLLPG: added support for `any token` (`token` is now treated like an attribute for this purpose)
- LLLPG: reduce precedence of RHS of `any-in` operator so that `any token in result:token` parses OK.

### LLLPG v1.7.5: April, 2016 ### 

- Added optional ANTLR-style rule syntax (usage: `LLLPG (/*options*/) @{ /*ANTLR-style rules*/ };`) (introduced in v1.7.3, completed in 1.7.5). LLLPG-style rules (`rule R(args) @{...}`) are supported in a limited way when using ANTLR-style syntax mode
- `BaseParser`/`BaseLexer` now throw `LogException` instead of `FormatException` by default
- Shift `FileName` property from `ISourceFile` to base interface `IIndexToLine`

### LLLPG v1.5.1: March 5, 2016 ### 

- LLLPG home page officially opens!
- LLLPG: Added `...` as synonym for `..`; `...` is preferred since character ranges are inclusive
- Changes to LLLPG itself are minimal, but [LeMP](http://ecsharp.net/lemp) has been updated to v1.5.1, which includes a richer set of macros (not really related to parsing), such as macros for pattern matching and algebraic data types.

### LLLPG v1.4.0: Aug 25, 2015 ### 

- LLLPG: Fixed to support for aliases in code blocks e.g. rule Foo @{ "alias" { Process($"alias"); } }; now works when "alias" is an alias
- LLLPG: eliminated `lexer(option = value)` and `parser(option = value)` syntax; added support for `parser(option: value)` syntax in LES to match EC#. See `MacroContext.GetOptions()`
- LLLPG custom tool: reduced default timeout to 10 seconds

### LLLPG v1.3.2: June 19, 2015 ### 

Main new features:

- "External API": in LLLPG 1.1 you had to write a class derived from `BaseLexer` or `BaseParser` which contained the LLLPG APIs such as `Match`, `LA0`, `Error`, etc. Now you can encapsulate that API in a field or a local variable. This means you can have a different base class, or you can put a lexer/parser inside a value type (`struct`) or a `static class`.
- "Automatic Value Saver: in LLLPG 1.1, if you wanted to save the return value of a rule or token, you (sometimes) had to manually create an associated variable. In the new version, you can attach a "label" to any terminal or nonterminal, which will make LLLPG create a variable automatically at the beginning of the method. Even better, you can often get away with not attaching a label.
- Automatic return value: when you use `$result` or the `result:` label in a rule, LLLPG automatically creates a variable called `result` to hold the return value of the current rule, and it adds a `return result` statement at the end of the method.
- implicit LLLPG blocks: instead of writing `LLLPG(lexer) { /* rules */ }`, with braces around the rules, you are now allowed to write `LLLPG(lexer); /* rules */`, so  you won't be pressured to indent the rules so much.
- `any` command and `inline` rules
- The new base class [`BaseParserForList<Token,int>`](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1BaseParserForList_3_01Token_00_01MatchType_01_4.html) is easier to use than the old base class `BaseParser<Token>`.
- LLLPG will now insert `#line` directives in the code for grammar actions. While useful for compiler errors, this feature turned out to be disorienting when debugging; to convert the `#line` directives into comments, attach the following attribute before the `LLLPG` command: `[AddCsLineDirectives(false)]`.

Complete list:

- Published LLLPG Part 5 article
- Standalone version: Replaced `BaseParser/BaseLexer` with dual-purpose `LexerSource/ParserSource` which can be used either as base classes, or as objects with LLLPG's `inputSource` and `inputClass` options.
- LoycSyntaxForVs.vsix now installs (and happens to work) in VS 2015 RC
- Implemented and tested `IndentTokenGenerator`
- Added `IIndexToLine` as part of `ILexer<Token>` because certain lexers don't have a `SourceFile`, and `BaseLexer` implements `IndexToLine` anyway
- Implemented VS syntax highlighter for Enhanced C# in Visual Studio 2010 to 2015
- LLLPG: Added `listInitializer: var _ = new List<T>()` option
- Renamed `LllpgForVisualStudio.exe` => `LoycFileGeneratorForVS.exe`
- `ILexer<Token>` now has a type parameter; `NextToken()` now returns [`Maybe<Token>`](http://ecsharp.net/doc/code/structLoyc_1_1Maybe_3_01T_01_4.html) instead of `Token?` so that `Token` is not required to be a struct.
- Added [`BaseILexer<CharSrc,Token>`](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1Lexing_1_1BaseILexer_3_01CharSrc_00_01Token_01_4.html), and installed it as the new base class of LesLexer
- Added `BaseLexer.ErrorSink` property; default behavior is still to throw `FormatException`
- LLLPG: Added `parser(CastLA(bool))` option; it defaults to `true`, which is usually needed when using `ParserSource<Token>`.
- Added `ParserSource<...>`; added optional `MatchType` parameter to `BaseParser` and `BaseParserForList`.
- Added TT type parameter to `ISimpleToken` and `IToken`. Normally `TT=int`.
- LLLPG: now produces C# `#line` directives (grammar actions only). Tweaked output header msg.
- LLLPG: braces no longer required after `LLLPG(...)` statement
- LLLPG: Added support for `$result` variable.
- LLLPG: Added basic support for inlining rules (`inline` keyword)
- LLLPG: Added support for `any foo in (foo ...)` expression, where `foo` refers to an attribute or word attribute on one or more other rules.
- LLLPG `StageOneParser`: started adding "!" suffix operator to resemble ANTLR's old feature; **incomplete**.
- LLLPG: Added `AutoValueSaverVisitor` to recognize labels & substitutions like `a:Foo b+:Bar Baz {$Baz}`, plus integration tests, and new `terminalType` option.
- Loyc.Syntax: Added `BaseParserForList` & `BaseParserNoBacktracking` (untested). Added `ISimpleToken` interface.
- Loyc.Syntax: added `CharSrc` type parameter to `BaseLexer<CharSrc>` as a way to avoid boxing during lexing. Added `LexerSource` and `LexerSource<CharSrc>` for use with LLLPG's new `inputClass` and `inputSource` options.
- LLLPG: added the `inputSource` and `inputClass` options to add flexibility, so that lexers and parsers no longer have to use `BaseLexer` / `BaseParser` as their base class.
- LLLPG: goto-labels improved in certain cases, e.g. in `(Foo | Bar)`, the labels will be `matchFoo` and `matchBar` instead of `match1` and `match2`
- LLLPG bug fix: Partly fixed nondeterministic bug in `PredictionAnalysisVisitor` that caused EC# parser to parse `alias(...)` calls as type alias constructs.
- LLLPG bug fix: `SavePosition` now prefixed by value of `inputClass` option.
- LLLPG bug fix: `result:Terminal` did not automatically `return result`.

### LLLPG v1.1.0: Feb 23, 2014 ### 

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

### LLLPG v1.0: Feb 8, 2014 ###

- EC# support
- Demo and article updated
- Demo now uses EC# by default (LES version still included) and supports "mathy" expressions such as 2(2 + 5) => 14.

### LLLPG v0.9.1: Nov 19, 2013 ###

- Updated demo to be a bit cleaner and to eliminate dependencies on Loyc libraries.
- Some bug fixes, a new alias(X = Y) command, and eliminated dependency on IntSet.

### LLLPG v0.9: Oct 7, 2013 ### 

- Initial release with Part 1 article

