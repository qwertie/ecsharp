---
title: "Configuring & invoking LLLPG"
layout: article
date: 30 May 2016
toc: true
---

Command-line options
--------------------

LLLPG can be invoked either with the custom tool for Visual Studio, or on the command line (or in a pre-build step) by running **LLLPG.exe _filename_**. When using the custom tool, command-line options can be written in the "Default Namespace" field in Visual Studio, although not all of them are supported.

The following command-line options are reported by LLLPG --help, but command-line options are rarely necessary.

    --forcelang: Specifies that --inlang overrides the input file extension.
      Without this option, known file extensions override --inlang.
    --help: show this screen
    --inlang=name: Set input language: --inlang=ecs for Enhanced C#, --inlang=les for LES
    --macros=filename.dll: load macros from given assembly
    --max-expand=N: stop expanding macros after N nested or iterated expansions.
    --noparallel: Process all files in sequence
    --nostdmacros: Don't scan LeMP.StdMacros.dll or pre-import LeMP and LeMP.Prelude
    --outext=name: Set output extension and optional suffix:
        .ecs (Enhanced C#), .cs (C#), .les (LES)
      This can include a suffix before the extension, e.g. --outext=.output.cs
      If --outlang is not used, output language is chosen by file extension.
    --outlang=name: Set output language independently of file extension
    --parallel: Process all files in parallel (this is the default)
    --set:key=literal: Associate a value with a key (use #get(key) to read it back)
    --snippet:key=code: Associate code with a key (use #get(key) to read it back)
    --timeout=N: Aborts the processing thread(s) after this many seconds (0=never)
    --verbose: Print extra status messages (e.g. discovered Types, list output files).

**Note**: in VS, the `[Verbosity(N)]` grammar attribute doesn't work without the `--verbose` option.

Invoking LLLPG in source code
-----------------------------

In your *.ecs or *.les input file, the syntax for invoking LLLPG is to use one of these statements:

~~~csharp
    [general options]
    LLLPG(lexer(code generation options))  { /* grammar */ };
    
    LLLPG(lexer)                           { /* grammar */ };
    
    [general options]
    LLLPG(parser(code generation options)) { /* grammar */ };
    
    LLLPG               { /* parser mode is the default */ };
~~~

Note: LES currently requires the semicolon while EC# does not, and LES files permit `LLLPG lexer {...}` and `LLLPG parser {...}` without parenthesis, which (due to the syntax rules of LES) is exactly equivalent to `LLLPG(lexer) {...}` or `LLLPG(parser) {...}`).

The braces can be omitted, leaving only a semicolon. In that case the remainder of the current block is treated as the grammar.

The rules of your grammar go inside the braces, but normally you are also allowed to put normal code inside the braces too, such as fields, methods, and child classes.

To use the [ANTLR-style syntax mode](lllpg-in-antlr-style.html), put an `@` before the opening brace, e.g.

~~~csharp
    LLLPG(lexer) @{ /* grammar */ };
~~~

In this case the braces are required, and normal code (fields, methods, etc.) are not allowed except inside an additional set of braces, e.g. 

~~~csharp
    LLLPG(lexer) @{ 
        /* grammar */ 
        {/* normal code */}
        /* grammmar */
    };
~~~

Code generation options
-----------------------

The following options are available for both `lexer` and `parser`:

- `inputSource: v` and `inputClass: T`: needed by "static" lexers/parsers and parsers in `struct`s. See section 'Using LLLPG with an "external" API' below
- `terminalType: T`: data type of terminals. This is used by the colon operator, e.g. `x:Terminal`, which becomes `x = Match(Terminal)` in the output, declares a variable `x` of this type to store the terminal.
- `setType: T`: data type for large sets. When you write a set with more than four elements, such as `'a'|'e'|'i'|'o'|'u'|'y'`, LLLPG generates a set object and uses `set.Contains(la0)` for prediction and `Match(set)` for matching, e.g. instead of `Match('a', 'e', 'i', 'o', 'u', 'y')` it generates a set with a statement like `static HashSet<int> RuleName_set0 = NewSet('a', 'e', 'i', 'o', 'u', 'y');` and then calls `Match(RuleName_set0)`. The default is `HashSet<int>`.
- `listInitializer: e`: Sets the data type of lists declared automatically when you use the `+:` operator. An initializer like `Type x = expr` causes `Type` to be used as the list type and `expr` as the initialization expression. The `Type` can have a type parameter `T` that is replaced with the appropriate item type. The default is `listInitializer: List<T> = new List<T>()`.
- `noCheckByDefault: bool`: If this option is true, calls to `Check()` are eliminated when using semantic or syntactic predicates.

The following options are available only for `parser`:

- `laType: T`: data type of `la0`, `la1`, etc. Typically this is the name of an `enum` that you are using to represent token types (default: `int`). For lexers, `laType` is always `int` (not `char`, because -1 is used for EOF).
- `matchCast: T`: causes a cast to be added to all token types passed to `Match`. For example, if you use `matchCast: int` option, it will change calls like `Match('+', '-')` into `Match((int) '+', (int) '-')`. `matchCast` is a synonym for `matchType`.
- `allowSwitch: bool`: whether to allow `switch` statements (default: `true`). In C#, switch cases must be constants, so certain `laType` data types like `Symbol` are incompatible with `switch`. Therefore, this option can be used to prevent `switch` statements from being generated. Requires a boolean literal `true` or `false` (`@true` or `@false` in LES).
- `castLa: bool`: whether to cast the result of `LA0` and `LA(i)` to `laType` (the default is `true`)

The above options apply to the `lexer` or `parser` helper object, which controls code generation and defines how terminals are interpreted:

- `lexer` mode requires numeric terminals, and allows numeric ranges like `1..31` or `'a'..'z'`
- `parser` mode permits any literal or complex identifier, but does not support numeric ranges.

General options
---------------

In addition to the `lexer` and `parser` options above, you can add one or more of the following attributes before the `LLLPG` statement:

- `[FullLLk(true)]` or `[FullLLk(false)]`: enables or disables complete prediction analysis; for more information, see the appendix [FullLLk versus "approximate" LL(k)](full-llk-vs-approximate.html).
- `[Verbosity(int)]`: prints extra messages to help debug a grammar. An integer literal is required and specifies how much detail to print: `1` for basic information, `2` for extra information, `3` for excessive information. Details printed include first sets, follow sets, and prediction trees. **Note**: This attribute does not work without the `--verbose` option. 
- `[NoDefaultArm(true)]`: adds a call to `Error(...)` at all branching points for which you did not provide a `default` or `error` arm (see §"Error handling mechanisms" below).
- `[LL(int)]` (synonyms: `[k(int)]` and `[DefaultK(int)]`): specifies the default maximum number of lookahead characters or tokens in this grammar.
- `[AddComments(false)]`: by default, a comment line is printed in the output file in front of the code generated for every Alts (branching point: `| / * ?`). `[AddComments(false)]` removes these comments.
- `[AddCsLineDirectives(true)]`: adds `#line` directives to the output, in an effort to let errors in actions in the C# file point back to the EC# file. This feature doesn't work so well, since only line numbers are translated (not column numbers), and it only works inside rules (not inside other code in your .ecs or .les file). This option is largely superceded by the `#lines;` macro, which can be added to the top of any .ecs file to add `#line` directives throughout it.
- `[PrematchByDefault]`: if a rule is only called by other rules (not called from the outside) then "prematch analysis" can sometimes replace `Match()` calls with `Skip()` calls to improve performance. By default, this is only done for rules that are marked `private`, but `PrematchByDefault` extends this optimization to rules that have no access modifier (not `public`, nor `private`, nor `protected` nor `internal`.)

Setting lookahead
-----------------

Pure LL(k) parsers look up to `k` terminals ahead to make a branching decision, and once a decision is make they stick to it, they don't "backtrack" or try something else. So if `k` is too low, LLLPG will generate code that makes incorrect decisions.

LLLPG's default `k` value is `2`, which is enough in the majority of situations, as long as your grammar is designed to be LL(k). To increase `k` to `X`, simply add a `[DefaultK(X)]` attribute to the grammar (i.e. the LLLPG statement), or add a `[k(X)]` attribute to a single rule (`[LL(X)]`is a synonym). Here's an example that represents `"double-quoted"` and `"""triple-quoted"""` strings, where k=2 is not enough:

    private token DQString @{
        '"' ('\' _  | ~('"'|'\'|'r'|'n'))* '"'? ];
    };
    [k(4)]
    private token TQString @{
        '"' '"' '"' nongreedy(Newline / _)* '"' '"' '"'
        "'''"       nongreedy(Newline / _)* "'''"
    };
    [k(4)]
    private token Token @{
        ( {_type = TT.Spaces;}    Spaces
        ...
        | {_type = TT.String;}    TQString
        | {_type = TT.String;}    DQString
        ...
        )
    };

Here I've used "`_`" inside both kinds of strings, meaning "match any character", but this implies that the string can go on and on forever. To fix that, I add nongreedy meaning "exit the loop when it makes sense to do so" ([greedy and nongreedy are explained more in my blog][16].)

With only two characters of lookahead, LLLPG cannot tell whether `"""this"""` is an empty `DQString` (`""`) or a triple-quoted `TQString`. Since `TQString` is listed first, LLLPG will always choose `TQString` when a `Token` starts with `""`, but of course this may be the wrong decision. You'll also get a warning like this one:

    warning : Loyc.LLParserGenerator.Macros.run_LLLPG:
    Alternatives (4, 5) are ambiguous for input such as «""» (["], ["])

`[k(3)]` is sufficient in this case, but it's okay if you use a number that is a little higher than necessary, so I've used `[k(4)]` here.

Using LLLPG with an "external" API
----------------------------------

You can use the `inputSource` and `inputClass` options to designate an object to which LLLPG should send all its API calls. `inputClass` should be the data type of the object that `inputSource` refers to. For example, if you specify `inputSource(src)`, LLLPG will translate a grammar fragment like `'+'|'-'` into code like `src.Match('+','-')`. Without the `inputSource` option, this would have just been `Match('+','-')`.

Loyc.Syntax.dll (included with LLLPG 1.3) has external API classes called [`LexerSource`](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1Lexing_1_1LexerSource.html) and [`LexerSource<C>`](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1Lexing_1_1LexerSource_3_01CharSrc_01_4.html) types, which are derived from [`BaseLexer`](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1Lexing_1_1BaseLexer_3_01CharSrc_01_4.html) and provide the LLLPG Lexer API. 

When using these options, a lexer will look something like this:

    using Loyc;
    using Loyc.Syntax.Lexing;
    
    public class MyLexer {
      public MyLexer(string input, string fileName = "") { 
        src = new LexerSource((UString)input, fileName);
      }
      LexerSource src;
      
      LLLPG (lexer(inputSource: src, inputClass: LexerSource)) {
        public rule Token()         @{ Id  | Spaces | Newline };
        private rule Id             @{ IdStartChar (IdStartChar|'0'..'9'|'\'')* };
        private rule IdStartChar    @{ 'a'..'z'|'A'..'Z'|'_' };
        private rule Spaces         @{ (' '|'\t')+ };
        private rule Newline        @{ ('\n' | '\r' '\n'?)
          {src.AfterNewline();} // increments LineNumber
        };
      }
    }

`LexerSource` accepts any implementation of (`ICharSource`](http://ecsharp.net/doc/code/interfaceLoyc_1_1Collections_1_1ICharSource.html); `ICharSource` represents a source of characters with a `Slice(...)` method, which is used to speed up access to individual characters. If your input is simply a string `S`, convert the string to `LexerSource` using `new LexerSource((UString)S)`; the shortcut `(LexerSource)S` is also provided. [`UString`](http://ecsharp.net/doc/code/structLoyc_1_1UString.html#a19b13b6171235bfa8b3d8ca12902eb89) is a wrapper around `string` that implements the `ICharSource` interface (the U in `UString` means "unicode"; see the (documentation of UString)[http://ecsharp.net/doc/code/structLoyc_1_1UString.html] for details.)

See also
--------

### Grammar features ###

To learn about semantic and syntactic predicates (also known as zero-width assertions), `=>` gates, the set inversion operator `~`, or the underscore `_` which matches any character or token, please see [LLLPG Grammar Features](4-lllpg-grammar-features.md).

### Error handling ###

To learn about LLLPG's error handling mechanisms, please see the article about [Error Handling](7-error-handling.html).
