---
title: "LLLPG Part 3: Beyond the basics"
layout: article
date: 23 Feb 2014
toc: true
redirectDomain: ecsharp.net
---

Welcome to part 3
-----------------

_New to LLLPG? Start at [part 1](http://www.codeproject.com/Articles/664785/A-New-Parser-Generator-for-Csharp)._

There are lots of things left to cover, so let's get started. LLLPG 1.1.0 was released at the same time as this article; it came with four demos:

- CalcExample-Standalone: Expression calculator with no dependencies
- CalcExample-UsingLoycLibs: Expression calculator that uses `BaseLexer` and `BaseParserForList` from Loyc.Syntax.dll
- CalcExample-UsingLoycTrees: Expression calculator whose parser produces Loyc trees instead of calculating a result directly.
- EnhancedC#Parser: I ripped the C# parser used by LLLPG out of Ecs.exe and dropped it into this demo program (so the parser is _not_ up-to-date, but good enough for a demo).

Many features have been added since this article was published, most of which are discussed in [Part 5](lllpg-part-5.html).

## A brief overview of the Loyc libraries

When writing a parser, you have to decide whether you'll use the Loyc runtime libraries or not; the main advantage of _not_ using them is that you won't have to distribute the 3 Loyc DLLs with your application. But they contain a lot of useful stuff, so have a look and see if you like them.

The important library for parsers based on LLLPG is _Loyc.Syntax.dll_, which depends on _Loyc.Essentials.dll_ and _Loyc.Collections.dll_. These DLLs have documentation for most of the classes they contain, automatically available to VS IntelliSense through _Loyc.Syntax.xml_, _Loyc.Essentials.xml_ and _Loyc.Collections.xml_.

In brief, let me just say very briefly what these libraries are for and what they contain.

**Loyc.Essentials.dll** is a library of general-purpose code that supplements the .NET BCL (standard libraries). It contains the following categories of stuff:

- Collection stuff: interfaces, adapters, helper classes, base classes, extension methods, and implementations for simple "core" collections such as [InternalList](http://core.loyc.net/collections/internal-list.html). You can [learn more in the docs](http://ecsharp.net/doc/code/namespaceLoyc_1_1Collections.html), but you'll be looking at Loyc.Essentials.dll and Loyc.Collections.dll combined.
- Geometry: simple generic geometric interfaces and classes, e.g. `Point<T>` and `Vector<T>`
- Math: generic math interfaces that allow arithmetic to be performed in generic code. Also includes fixed-point types, 128-bit integer math, and handy extra math functions in `MathEx`.
- Other utilities: message sinks ([`IMessageSink`](http://ecsharp.net/doc/code/interfaceLoyc_1_1IMessageSink.html)), [`Symbol`](http://ecsharp.net/doc/code/classLoyc_1_1Symbol.html), threading stuff, a miniture clone of NUnit ([`MiniTest`](https://github.com/qwertie/ecsharp/blob/master/Core/Loyc.Essentials/Utilities/MiniTest.cs), [`RunTests`](http://ecsharp.net/doc/code/classLoyc_1_1MiniTest_1_1RunTests.html)), and miscellaneous ["global" functions] and extension methods.
`Compatibility`: a very small amount of .NET 4.5 stuff, backported to .NET 4.0 when using the .NET 4 build.

Loyc.Essentials also defines [`ICharSource`](http://ecsharp.net/doc/code/interfaceLoyc_1_1Collections_1_1ICharSource.html) (defined in Loyc.Essentials.dll), a standard interface for a source of characters, which is used by lexers. `string` converts implicitly to [`UString`](http://ecsharp.net/doc/code/structLoyc_1_1UString.html) which is a string slice structure that implements `ICharSource`. The `Slice(start, count)` extension method can also get slices of strings.

**Note**: the Loyc.Essentials API is not 100% stable yet (feedback welcome). Also, since Loyc.Essentials already contains a lot of LINQy stuff, I intend to incorporate the core functionality of [Linq to Collections](http://twistedoakstudios.com/blog/Post1585_linq-to-collections-beyond-ienumerablet) eventually but have only written parts of it so far.

[`IMessageSink`](http://sourceforge.net/p/loyc/code/HEAD/tree/Src/Loyc.Essentials/Utilities/IMessageSink.cs) serves as a simple, generic logging interface. It is recommended that your parsers report warnings and errors to an `IMessageSink` object. You can use `MessageSink.Console` to print (colored) errors to the console, `MessageSink.Null` to suppress output, and `MessageSink.FromDelegate((type, context, message, args) => {...})` to customize error handling.

The [`G` class](http://sourceforge.net/p/loyc/code/HEAD/tree/Src/Loyc.Essentials/G.cs) has generic number parsers that are handy for lexers, such as `TryParseDouble`, which can parse numbers of any reasonable radix and is therefore useful for hex float literals such as `0xF.Fp+1` (a syntax that represents 31.875).

**Loyc.Collections.dll** is a library of data structures, mostly rather complex ones, currently all written by me:

- [VLists](http://www.codeproject.com/Articles/26171/VList-data-structures-in-C): this data structure is notable because Loyc nodes (`LNode`s) use `VList<LNode>` for their arguments and attributes. This is an implementation detail that ideally you wouldn't have to know about; but C# has no [`typedef`s](http://en.wikipedia.org/wiki/Typedef) that I could use to hide the type, and since VLists are `struct`s, if you treat them as `IList<T>` they will be boxed, and you don't really want that.
- [ALists](http://core.loyc.net/collections/alists-part1.html), including the B+tree-like data structures `BList<T>`, `BDictionary<K,V>`, and my favorite, `BMultiMap<K,V>`, plus the new [`SparseAList<T>`](http://core.loyc.net/collections/alists-part3.html) which I use in my syntax highlighter.
- [`Bijection<K1,K2>`](http://ecsharp.net/doc/code/classLoyc_1_1Collections_1_1Bijection_3_01K1_00_01K2_01_4.html): A dictionary that goes in both directions.
- [And more!](http://core.loyc.net/collections/)

**Loyc.Syntax.dll** provides the foundations for LLLPG and contains the reference implementation of [LES, the syntax tree interchange format](http://loyc.net/les):

- [`BaseLexer`](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1Lexing_1_1BaseLexer.html) is the recommended base class for lexers created with LLLPG. [`BaseParserForList<Token,MatchType>`](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1BaseParserForList_3_01Token_00_01MatchType_01_4.html) is the recommended base class for parsers.
- [`StreamCharSource`](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1StreamCharSource.html) is an implementation of `ICharSource` designed for parsing a file without storing the whole thing in memory.
-  [`ISourceFile`](http://ecsharp.net/doc/code/interfaceLoyc_1_1Syntax_1_1ISourceFile.html) encapsulates an `ICharSource`, a file name string, and a mapping to translate character indexes to (line, column) pairs and back. It is derived from [`IIndexPositionMapper`](http://ecsharp.net/doc/code/interfaceLoyc_1_1Syntax_1_1IIndexPositionMapper.html).
- [`SourceRange`](http://ecsharp.net/doc/code/structLoyc_1_1Syntax_1_1SourceRange.html) is a triple (`ISourceFile Source`, `int StartIndex`, `int Length`) that represents a range of characters in a source file.
- `SourcePos` is a (filename, line, column) triple. While `SourceRange` is a struct so it can be stored compactly, `SourcePos` is assumed to be used much less often, and it is a class so it can be derived from `LineAndCol` which is a (line, column) pair.
- `IndexPositionMapper` provides mapping from `SourceRange` to `SourcePos` and back, but you don't often _need_ this class because `BaseLexer` already keeps track of the current line number (and where it started). In your lexer, you **must** call `AfterNewline()` at each newline in order for index-position mapping to work correctly.
- [`LNode`](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1LNode.html) is a Loyc Node or (synonymously) a [Loyc Tree](https://sourceforge.net/apps/mediawiki/loyc/index.php?title=Loyc_trees). [`LNodeFactory`](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1LNodeFactory.html) is commonly used to help construct `LNode`s.
- `LesLanguageService.Value` provides an LES parser and printer, neither of which are fully complete yet. It implements [`IParsingService`](http://ecsharp.net/doc/code/interfaceLoyc_1_1Syntax_1_1IParsingService.html).
- [`SourceFileWithLineRemaps`](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1SourceFileWithLineRemaps.html) is a helper class for languages that have a `#line` directive.
- [`Precedence`](http://ecsharp.net/doc/code/structLoyc_1_1Syntax_1_1Precedence.html): a simple but flexible standard representation for the concept of operator precedence and "miscibility".
- [`CodeSymbols`](http://ecsharp.net/doc/code/classLoyc_1_1Syntax_1_1CodeSymbols.html) is a `static class` filled with standard `Symbol`s used in Loyc trees for operators (`Add` for +, `Sub` for -, `Mul` for *, `Set` for =, `Eq` for `==`, ...), statements (`Class` for `#class`, `Enum` for #enum, `ForEach` for #foreach, ...), modifiers (`Private` for #private, `Static` for #static, `Virtual` for #virtual, ...), types (`Void` for `#void`, `Int32` for `#int32`, Double for `#double`, ...), trivia (`TriviaInParens` for `#trivia_inParens`, ...), and so on.

The Loyc libraries contain only "safe", verifiable code.

**Note**: some of the links go to the SourceForge code browser which chops off the right side of the code. To scroll rightward in the code, click any line of code and then hold the right arrow key.

## Configuring LLLPG

LLLPG can be invoked either with the custom tool for Visual Studio, or on the command line (or in a pre-build step) by running **LLLPG.exe _filename_**.

The following command-line options are reported by LLLPG --help:

    --forcelang: Specifies that --inlang overrides the input file extension.
      Without this option, known file extensions override --inlang.
    --help: show this screen
    --inlang=name: Set input language: --inlang=ecs for Enhanced C#, --inlang=les for LES
    --macros=filename.dll: load macros from given assembly
      (by default, just LEL 'prelude' macros are available)
    --max-expand=N: stop expanding macros after N expansions.
    --noparallel: Process all files in sequence
    --outext=name: Set output extension and optional suffix:
        .ecs (Enhanced C#), .cs (C#), .les (LES)
      This can include a suffix before the extension, e.g. --outext=.output.cs
      If --outlang is not used, output language is chosen by file extension.
    --outlang=name: Set output language independently of file extension
    --parallel: Process all files in parallel (this is the default)
    --timeout: Aborts the processing thread(s) after this number of seconds
      (0=never, default=30)
    --verbose: Print extra status messages (e.g. discovered Types, list output files).

Any questions?

A couple of these options, such as `--verbose` and `--timeout=N`, are supported in the LLLPG Custom Tool; you can put command-line options in the "Custom Tool Namespace" field in Visual Studio. The `--outext` option is not supported because Visual Studio requires LLLPG to choose the output file extension before it provides the "Custom Tool Namespace" value; if you want LES output, you can use `LLLPG_Les` as the custom tool name instead of `LLLPG`.

**Note**: the `[Verbosity(N)]` grammar attribute doesn't work without the `--verbose` option.

In your *.ecs or *.les input file, the syntax for invoking LLLPG is to use one of these statements:

    LLLPG(lexer)                 { /* rules */ };
    LLLPG(lexer(...options...))  { /* rules */ };
    LLLPG(parser)                { /* rules */ };
    LLLPG(parser(...options...)) { /* rules */ };
    LLLPG   { /* parser mode is the default */ };

LES requires the semicolon while EC# does not. Also, LES files permit `LLLPG lexer {...}` and `LLLPG parser {...}` without parenthesis, which (due to the syntax rules of LES) is exactly equivalent to `LLLPG(lexer) {...}` or `LLLPG(parser) {...}`.

The following options are available for both `lexer` and `parser`:

- `inputSource: v` and `inputClass: T`: used by `static` lexers/parsers and parsers in `struct`s. See part 5 for more information.
- `terminalType: T`: data type of terminals. This is used by the colon operator, e.g. `x:Terminal`, which becomes `x = Match(Terminal)` in the output, declares a variable `x` of this type to store the terminal.
- `setType: T`: data type for large sets. When you write a set with more than four elements, such as `'a'|'e'|'i'|'o'|'u'|'y'`, LLLPG generates a set object and uses `set.Contains(la0)` for prediction and `Match(set)` for matching, e.g. instead of `Match('a', 'e', 'i', 'o', 'u', 'y')` it generates a set with a statement like `static HashSet<int> RuleName_set0 = NewSet('a', 'e', 'i', 'o', 'u', 'y');` and then calls `Match(RuleName_set0)`. The default is `HashSet<int>`.
- `listInitializer: e`: Sets the data type of lists declared automatically when you use the `+:` operator. An initializer like `Type x = expr` causes `Type` to be used as the list type and `expr` as the initialization expression. The `Type` can have a type parameter `T` that is replaced with the appropriate item type. The default is `listInitializer: List<T> = new List<T>()`.

The following options are available only for `parser`:

- `laType: T`: data type of `la0`, `la1`, etc. Typically this is the name of an `enum` that you are using to represent token types (default: `int`). For lexers, `laType` is always `int` (not `char`, because -1 is used for EOF).
- `matchCast: T`: causes a cast to be added to all token types passed to `Match`. For example, if you use `matchCast: int` option, it will change calls like `Match('+', '-')` into `Match((int) '+', (int) '-')`. `matchCast` is a synonym for `matchType`.
- `allowSwitch: bool`: whether to allow `switch` statements (default: `true`). In C#, switch cases must be constants, so certain `laType` data types like `Symbol` are incompatible with `switch`. Therefore, this option can be used to prevent `switch` statements from being generated. Requires a boolean literal `true` or `false` (`@true` or `@false` in LES).
- `castLa: bool`: whether to cast the result of `LA0` and `LA(i)` to `laType` (the default is `true`)

The above options apply to the `lexer` or `parser` helper object, which controls code generation and defines how terminals are interpreted:

- `lexer` mode requires numeric terminals, and allows numeric ranges like `1..31` or `'a'..'z'`
- `parser` mode permits any literal or complex identifier, but does not support numeric ranges.

In addition to the `lexer` and `parser` options, you can add one or more of the following attributes before the `LLLPG` statement:

- `[FullLLk(true)]`: enables deeper prediction analysis (as explained later)
- `[Verbosity(int)]`: prints extra messages to help debug a grammar. An integer literal is required and specifies how much detail to print: `1` for basic information, `2` for extra information, `3` for excessive information. Details printed include first sets, follow sets, and prediction trees. **Note**: This attribute does not work without the `--verbose` option. 
- `[NoDefaultArm(true)]`: adds a call to `Error(...)` at all branching points for which you did not provide a `default` or `error` arm (see §"Error handling mechanisms" below).
- `[LL(int)]` (synonyms: `[k(int)]` and `[DefaultK(int)]`): specifies the default maximum number of lookahead characters or tokens in this grammar.
- `[AddComments(false)]`: by default, a comment line is printed in the output file in front of the code generated for every Alts (branching point: `| / * ?`). `[AddComments(false)]` removes these comments.

## Boilerplate

The typical outline of an EC# grammar file looks like this. You'll start by defining token types and a lexer...

	using System(, .Text, .Linq, .Collections.Generic, .Diagnostics);
	using Loyc;               // optional (for IMessageSink, Symbol, etc.)
	using Loyc.Collections;   // optional (many handy interfaces & classes)
	using Loyc.Syntax.Lexing; // For BaseLexer
	using Loyc.Syntax;        // For BaseParser<Token> and LNode

	namespace MyLanguage; // Braces around the rest of the file are optional

    using TT = TokenType; // Abbreviate TokenType as TT

    public enum TokenType
    {
        EOF = -1,
        Unknown = 0,
        Spaces = 1, 
        Newline = 2,
        Number = 3,
        /* add more token names here */
    }

    // Optional: define a class/struct for Tokens, or use Loyc.Syntax.Lexing.Token.
    // In the latter case, define a "public static TokenType Type(this Token t)" 
    // extension method and define your TokenTypes based on TokenKind. Example:
    // https://github.com/qwertie/LoycCore/blob/master/Loyc.Syntax/LES/TokenType.cs
    public struct Token {
        public TokenType Type;
        public int StartIndex;
        /* add additional members here */
    }

    class MyLexer : BaseLexer
    {
        // If using the Loyc libraries: BaseLexer reads character data from an
        // ICharSource. The string wrapper UString implements ICharSource.
        public MyLexer(string text, string fileName = "") 
            : this((UString)text, fileName) { }
        public MyLexer(ICharSource text, string fileName = "") 
            : base(text, fileName) { }
        
        // Error handler that may be called by LLLPG or BaseLexer. LLLPG requires 
        // many other methods and properties provided by the base class.
        protected override void Error(int lookahead, string message)
        {
            Console.WriteLine("At index {0}: {1}", InputPosition + lookahead, message);
        }

        LLLPG (lexer)
        {
            private TokenType _type;
            private int _startIndex;
            
            public token Token NextToken()
            {
                _startIndex = InputPosition;
                @{ { _type = TT.Spaces; }  (' '|'\t')+
                 | { _type = TT.Newline; } Newline
                 | { _type = TT.Number; }  Number
                 | error _?
                   { _type = TT.Unknown; Error(0, "Unrecognized token"); }
                 };
                return new Token() { 
                    Type = _type, StartIndex = _startIndex, ...
                };
            }

            // 'extern' suppresses code generation, so the code of Newline is
            // inherited from BaseLexer but LLLPG still knows what's in it.
            extern token Newline @[ '\r' '\n'? | '\n' ];

            private token Number() @[
                '0'..'9'+ ('.' '0'..'9'+)?
            ];
        }
    }

`BaseLexer` only requires you to define an `Error()` method. `BaseParser<Token>` requires more work because: 

- it doesn't know about your `TokenType`: conversions to '`int`' are required because C# doesn't allow a generic class to _efficiently_ compare unknown enum values or convert them to int. So you must use the `matchType(int)` option, and `BaseParser` requires you to define `EOFInt` while LLLPG itself needs you to define `EOF`.
- it doesn't know how to get your `TokenType` out of a `Token`, so you must override `LA0Int`.
- unlike `BaseLexer`, which is based on `ICharSource` (Loyc version) or `IList<char>` (standalone version in the zip file), `BaseParser` isn't in charge of storing the input tokens, because I couldn't decide upon a single reasonable way to manage the input tokens. Therefore your derived class is in charge of storing the list of tokens and overriding `LT(i)` for supplying `Token`s to the base class (which are returned by the `Match(...)` methods.)

So here's a typical outline for a parser:

	namespace MyLanguage
	{
		public partial class MyParser : BaseParser<Token>
		{
			public MyParser(ICharSource text, string fileName)
			{
				// Grab all tokens from the lexer and ignore spaces
				var lexer = new MyLexer(text, fileName);
				_sourceFile = _lexer.SourceFile;
				_tokens = new List<Token>();
				Token t;
				while ((t = lexer.NextToken()).Type != TT.EOF) {
					if ((t.Type != TT.Spaces && t.Type != TT.Newline))
						_tokens.Add(t);
				}
			}

			#region Methods & properties required by BaseParser and LLLPG
			// Here are a couple of things required by LLLPG itself (EOF, LA0, 
			// LA(i)) followed by the helper methods required by BaseParser. 
			// The difference between "LA" and "LT" is that "LA" refers to the 
			// lookahead token type (e.g. TT.Num, TT.Add, etc.), while "LT" 
			// refers to the entire token (that's the Token structure, in this 
			// example.) LLLPG itself only requires LA, but BaseParser assumes 
			// that there is also a "Token" struct or class, which is the type 
			// returned by its Match() methods.

			const TokenType EOF = TT.EOF;
			TokenType LA0 { get { return LT0.Type; } }
			TokenType LA(int offset) { return LT(offset).Type; }

			protected override int EofInt() { return (int) EOF; }
			protected override int LA0Int { get { return (int) LT0.Type; } }
			protected override Token LT(int i)
			{
				i += InputPosition;
				if (i < _tokens.Count) {
					return _tokens[i];
				} else {
					return new Token { Type = EOF };
				}
			}
			protected override void Error(int lookahead, string message)
			{
				int tokenIndex = InputPosition + lookahead, charIndex;
				if (tokenIndex < _tokens.Count)
					charIndex = _tokens[tokenIndex].StartIndex;
				else
					charIndex = _sourceFile.Text.Count;
				SourcePos location = _sourceFile.IndexToLine(charIndex);
				Console.WriteLine("{0}: {1}", location.ToString(), message);
			}
			// BaseParser.Match() uses this for constructing error messages.
			protected override string ToString(int tokenType)
			{
				switch ((TT) tokenType) {
				case TT.Id:     return "identifier";
				case TT.Num:    return "number";
				case TT.Set:    return "':='";
				case TT.LParen: return "'('";
				case TT.RParen: return "')'";
				default:        return ((TokenType) tokenType).ToString();
				}
			}

			#endregion

			LLLPG(parser(laType(TokenType), matchType(int)))
			{
				public rule LNode Start() @[...];
			}
		}
	}
	
It's often more convenient to separate the grammar into a separate file from the other code, because you can't get code completion/IntelliSense in your LLLPG file. Hence, I always mark my parser as `partial` so I can write some of the code in a file that the IDE understands.

Normally, you'll use `{actions}` in the grammar to produce syntax tree objects. You can design the syntax tree yourself, or use Loyc trees (`LNode`s). `LNode` has `static` methods for constructing them, but it's more convenient to use `LNodeFactory` which keeps track of the current source file (`ISourceFile`) and provides a wider variety of methods for constructing trees. Here's a small expression parser that contructs `LNode`s (assuming the lexer produces Loyc Tokens):

	public partial class MyParser : BaseParser<Token>
 	{
		LNodeFactory F;

		public MyParser(...)
		{
			ISourceFile file = ...;
			F = new LNodeFactory(file);
		}

		LLLPG (parser(laType(TokenType), matchType(int)))
		{
			alias("(" = TT.LParen);
			alias(")" = TT.RParen);
			alias("*" = TT.Mul);
			alias("/" = TT.Div);
			alias("+" = TT.Add);
			alias("-" = TT.Sub);

			LNode BinOp(Symbol type, LNode lhs, LNode rhs)
			{
				return F.Call(type, lhs, rhs, lhs.Range.StartIndex, rhs.Range.EndIndex);
			}

			public rule LNode Start() @[ e:=AddExpr EOF {return e;} ];

			private rule LNode AddExpr() @[
				e:=MulExpr
				[ op:=("+"|"-") rhs:=MulExpr {e = BinOp((Symbol) op.Value, e, rhs);} ]*
				{ return e; }
			];

			private rule LNode MulExpr() @[ 
				e:=PrefixExpr
				[ op:=("*"|"/") rhs:=PrefixExpr 
				  {e = BinOp((Symbol) op.Value, e, rhs);}
				]*
				{return e;}
			];

			private rule LNode PrefixExpr() @
				[ minus:="-" e:=Atom {return F.Call(S.Sub, e, minus.StartIndex, e.Range.EndIndex);}
				| e:=Atom            {return e;}
				];

			private rule LNode Atom() @[
				{LNode r;}
				( t:=TT.Id          {r = F.Id(t);}
				| t:=TT.Num         {r = F.Literal(t);}
				| "(" r=Expr() ")"
				| error             {r = F._Missing;}
				  {Error(0, "Expected identifer, number, or (parens)");}
				)
				{return r;}
			];
		}
	}

A complete example is included in the zip file for LLLPG 1.1.0 (note that the '#' symbols printed by the demo are [planned to be removed](http://loyc-etc.blogspot.ca/2014/02/in-operator-names-to-be-removed.html)). By the way, if you have any ideas about how LLLPG could be changed to allow you to construct syntax trees in a more compact or elegant manner, I am open to suggestions.

## Rules with parameters and return values

You can add parameters and a return value to any rule, and use parameters when calling any rule:

	// Define a rule that takes an argument and returns a value.
	// Matches a pattern like TT.Num TT.Comma TT.Num TT.Comma TT.Num...
	// with a length that depends on the 'times' argument.
	token double Mul(int times) @[
		x:=TT.Num 
		nongreedy(
		   &{times>0} {times--;}
			TT.Comma y:=Num
			{x *= y;})*
		{return x;}
	];
	// To call a rule with a parameter, add a parameter list after 
	// the rule name.
	rule double Mul3 @[ x:=Mul(3) {return x;} ];

Here's the code generated for this parser:

	double Mul(int times)
	{
	  int la0, la1;
	  var x = Match(TT.Num);
	  for (;;) {
		 la0 = LA0;
		 if (la0 == TT.Comma) {
			if (times > 0) {
			  la1 = LA(1);
			  if (la1 == Num) {
				 times--;
				 Skip();
				 var y = MatchAny();
				 x *= y;
			  } else
				 break;
			} else
			  break;
		 } else
			break;
	  }
	  return x;
	}
	double Mul3()
	{
	  var x = Mul(3);
	  return x;
	}

There is a difference between `Foo(123)` and `Foo (123)` with a space. `Foo(123)` calls the `Foo` rule with a parameter of 123; `Foo (123)` is equivalent to `Foo 123` so the rule (or terminal) Foo is matched followed by the number 123 (which is "`{`" in ASCII).

## Parameters to recognizers

As you learned in the last article, each rule can have a _recognizer form_ which is called by syntactic predicates `&(...)`. The recognizer always has a return type of `bool`, regardless of the return type of the main rule, and any action blocks `{...}` are removed from the recognizer (currently there is no way to keep an action block, sorry.)

You can cause parameters to be kept or discarded from a recognizer using a `recognizer` attribute on a rule. Observe how code is generated for the following rule:

	LLLPG(parser) { 
		[recognizer { void FooRecognizer(int x); }]
		token double Foo(int x, int y) @[ match something ];
		
		token double FooCaller(int x, int y) @[
			Foo(1) Foo(1, 2) &Foo(1) &Foo(1, 2)
		];
	}

The recognizer version of Foo will accept only one argument because the `recognizer` attribute specifies only one argument. Although the `recognizer` attribute uses `void` as the return type of `FooRecognizer`, LLLPG ignores this and changes the return type to `bool`:

	double Foo(int x, int y)
	{
	  Match(match);
	  Match(something);
	}
	bool Try_FooRecognizer(int lookaheadAmt, int x)
	{
	  using (new SavePosition(this, lookaheadAmt))
		 return FooRecognizer(x);
	}
	bool FooRecognizer(int x)
	{
	  if (!TryMatch(match))
		 return false;
	  if (!TryMatch(something))
		 return false;
	  return true;
	}
	double FooCaller(int x, int y)
	{
	  Foo(1);
	  Foo(1, 2);
	  Check(Try_FooRecognizer(0, 1), "Foo");
	  Check(Try_FooRecognizer(0, 1, 2), "Foo");
	}

Notice that LLLPG does not verify that `FooCaller` passes the correct number of arguments to `Foo` or `FooRecognizer`, not in this case anyway. So LLLPG does not complain or alter the incorrect call `Foo(1)` or `Try_FooRecognizer(0, 1, 2)`. Usually LLLPG will simply repeat the argument argument list you provide, whether it makes sense or not. However, as a normal rule is "converted" into a recognizer, LLLPG can automatically reduce the number of arguments to other rules called by that rule, as demonstrated here:

	[recognizer {void BarRecognizer(int x);}]
	token double Bar(int x, int y) @[ match something ];
	
	rule void BarCaller @[
		Bar(1, 2)
	];
	
	rule double Start(int x, int y) @[ &BarCaller BarCaller ];

Generated code:

	double Bar(int x, int y)
	{
	  Match(match);
	  Match(something);
	}
	bool Try_BarRecognizer(int lookaheadAmt, int x)
	{
	  using (new SavePosition(this, lookaheadAmt))
		 return BarRecognizer(x);
	}
	bool BarRecognizer(int x)
	{
	  if (!TryMatch(match))
		 return false;
	  if (!TryMatch(something))
		 return false;
	  return true;
	}
	void BarCaller()
	{
	  Bar(1, 2);
	}
	bool Try_Scan_BarCaller(int lookaheadAmt)
	{
	  using (new SavePosition(this, lookaheadAmt))
		 return Scan_BarCaller();
	}
	bool Scan_BarCaller()
	{
	  if (!BarRecognizer(1))
		 return false;
	  return true;
	}
	double Start(int x, int y)
	{
	  Check(Try_Scan_BarCaller(0), "BarCaller");
	  BarCaller();
	}

Notice that `BarCaller()` calls `Bar(1, 2)`, with two arguments. However, `Scan_BarCaller`, which is the auto-generated name of the recognizer for `BarCaller`, calls `BarRecognizer(1)` with only a single parameter. Sometimes a parameter that is needed by the main rule (`Bar`) is not needed by the recognizer form of the rule (`BarRecognizer`) so LLLPG lets you remove parameters in the `recognizer` attribute; LLLPG will automatically delete call-site parameters when generating the recognizer version of a rule. You must only remove parameters from the end of the argument list; for example, if you write

	[recognizer { void XRecognizer(string second); }]
	rule double X(int first, string second) @[ match something ];

LLLPG will **not notice** that you removed the _first_ parameter rather than the _second_, it will only notice that the recognizer has a _shorter_ parameter list, so it will only remove the _second_ parameter. Also, LLLPG will only remove parameters from calls to the recognizer, not calls to the main rule, so the recognizer cannot accept more arguments than the main rule.

## Saving inputs

LLLPG recognizes three operators for "assigning" the result of reading a terminal or nonterminal to a variable: =, := and +=. This table how code is generated for these operators:

<table  border="1" width="640px">
<tr>
<th>Operator</th>
<th>Example</th>
<th>Generated code for terminal</th>
<th>Generated code for nonterminal</th>
</tr>
<tr>
<td><code>=</code></td>
<td><code>x=Foo</code></td>
<td><code>x = Match(Foo);</code></td>
<td><code>x = Foo();</code></td>
</tr>
<tr>
<td><code>:=</code></td>
<td><code>x:=Foo</code></td>
<td><code>var x = Match(Foo);</code></td>
<td><code>var x = Foo();</code></td>
</tr>
<tr>
<td><code>+=</code></td>
<td><code>x+=Foo</code></td>
<td><code>x.Add(Match(Foo));</code></td>
<td><code>x.Add(Foo());</code></td>
</tr>
</table>

You can match one of a set of terminals, for example `x:=('+'|'-'|'.')` generates code like `var x = Match('+', '-', '.')` (or `var x = Match(set)` for some `set` object, for large sets). However, currently LLLPG does not support matching a list of nonterminals, e.g. `x:=(A()|B())` is not supported.

I'm thinking about adding a feature where you would write simply `Foo` instead of `foo:=Foo` and then write `$Foo` in code later, which retroactively saves the value. For example, instead of writing code like this:

	private rule LNode IfStmt() @[
		{LNode @else = null;}
		t:=TT.If "(" cond:=Expr ")" then:=Stmt 
		greedy(TT.Else @else=Stmt)?
		{return IfNode(t, cond, then, @else);}
	];

It would be written like this instead:

	private rule LNode IfStmt() @[
		{LNode @else = null;}
		TT.If "(" Expr ")" Stmt 
		greedy(TT.Else @else=Stmt)?
		{return IfNode($TT.If, $Expr, $Stmt, @else);}
	];

Which would reduce the clutter inside the grammar. This idea (not yet implemented) comes from ANTLR which has something similar (more powerful, in fact). Suggestions?

## Error handling mechanisms in LLLPG

Okay, frankly, I haven't 100% figured out the right way to do error handling in a parser, but LLLPG does give you enough flexibility, I think.

First of all, when matching a single terminal, LLLPG puts your own code in charge of error handling. For example, if the rule says

	rule PlusMinus @[ '+'|'-' ];

the generated code is 

	void PlusMinus()
	{
	  Match('-', '+');
	}

So LLLPG is relying on the `Match()` method to decide how to handle errors. If the next input is neither `'-'` nor `'+'`, what should `Match()` do:

- Throw an exception?
- Print an error, consume one character and continue?
- Print an error, keep `InputPosition` unchanged and continue?

I'm not sure what the best approach is, but you can handle the situation however you choose. If you have advice about this type of default error handling, please leave a comment.

For cases that require if/else chains or switch statements, LLLPG's default behavior is optimistic: Quite simply, it assumes there are no erroroneous inputs. When you write

	rule Either @[ 'A' | B ];

the output is
  
	void Either()
	{
	  int la0;
	  la0 = LA0;
	  if (la0 == 'A')
	    Skip();
	  else
	    B();
	}

Under the assumption that there are no errors, if the input is not `'A'` then it must be `B`. Therefore, when you are writing a list of alternatives and one of them makes sense as a catch-all or default, you should put it last in the list of alternatives, which will make it the default. You can also specify which branch is the default by writing the word `default` at the beginning of one alternative:

	rule B @[ 'B' ];
	rule Either @[ default 'A' | B ];

	void B()
	{
	  Match('B');
	}
	void Either()
	{
	  int la0;
	  la0 = LA0;
	  if (la0 == 'A')
	    Match('A');
	  else if (la0 == 'B')
	    B();
	  else
	    Match('A');
	}

Remember that, if there is ambiguity between alternatives, the order of alternatives controls their priority. So you have at least two reasons to change the order of different alternatives:

1. To give one priority over another when the alteratives overlap
2. To select one as the default in case of invalid input

Occasionally these goals are in conflict: you may want a certain arm to have higher priority and _also_ be the default. That's where the `default` keyword comes in. In this example, the "Consonant" arm is the default:

	LLLPG(lexer)
	{
		rule ConsonantOrNot @[ 
			('A'|'E'|'I'|'O'|'U') {Vowel();} / 'A'..'Z' {Consonant();}
		];
	}
	
	void ConsonantOrNot()
	{
	  switch (LA0) {
	  // (Newlines between cases removed for brevity)
	  case 'A': case 'E': case 'I': case 'O': case 'U':
	    {
	      Skip();
	      Vowel();
	    }
	    break;
	  default:
	    {
	      MatchRange('A', 'Z');
	      Consonant();
	    }
	    break;
	  }
	}

You can use the `default` keyword to mark the "vowel" arm as the default instead, in which case perhaps we should call it "non-consonant" rather than "vowel":

	LLLPG(lexer) {
		rule ConsonantOrNot @[ 
			default ('A'|'E'|'I'|'O'|'U') {Other();} 
			       / 'A'..'Z' {Consonant();}
		];
	}
	
The generated code will be somewhat different:

	static readonly HashSet<int> ConsonantOrNot_set0 = NewSet('A', 'E', 'I', 'O', 'U');
	void ConsonantOrNot()
	{
		do {
			switch (LA0) {
			case 'A': case 'E': case 'I': case 'O': case 'U':
				goto match1;
			case 'B': case 'C': case 'D': case 'F': case 'G':
			case 'H': case 'J': case 'K': case 'L': case 'M':
			case 'N': case 'P': case 'Q': case 'R': case 'S':
			case 'T': case 'V': case 'W': case 'X': case 'Y':
			case 'Z':
				{
					Skip();
					Consonant();
				}
				break;
			default:
				goto match1;
			}
			break;
		match1:
			{
				Match(ConsonantOrNot_set0);
				Other();
			}
		} while (false);
	}");

This code ensures that the first branch matches any character that is not in one of the ranges 'B'..'D', 'F'..'H', 'J'..'N', 'P'..'T', or 'V'..'Z', i.e. the non-consonants (_Note_: this behavior was added in LLLPG 1.0.1; LLLPG 1.0.0 treats `default` as merely reordering the alternatives.)

Naming a default branch should never change the behavior of the generated parser _when the input is valid_. The default branch is invoked when the input is unexpected, which means it is specifically an error-handling mechanism.

**Note**: `(A | B | default C)` is usually, but not always, the same as `(A | B | C)`. Roughly speaking, in the latter case, LLLPG will sometimes let A or B handle invalid input if the code will be simpler that way.

Another error-handling feature is that LLLPG can insert error handlers automatically, in all cases more complicated than a call to `Match`. This is accomplished with the `[NoDefaultArm(true)]` grammar option, which causes an `Error(int, string)` method to be called whenever the input is not in the expected set. Here is an example:

	//[NoDefaultArm]
	LLLPG(parser)
	{
		rule B @[ 'B' ];
		rule Either @[ ('A' | B)* ];
	}


	void B()
	{
	  Match('B');
	}
	void Either()
	{
	  int la0;
	  for (;;) {
		 la0 = LA0;
		 if (la0 == 'A')
			Skip();
		 else if (la0 == 'B')
			B();
		 else
			break;
	  }
	}

When `[NoDefaultArm]` is added, the output changes to

	void Either()
	{
	  int la0;
	  for (;;) {
		 la0 = LA0;
		 if (la0 == 'A')
			Skip();
		 else if (la0 == 'B')
			B();
		 else if (la0 == EOF)
			break;
		 else
			Error(InputPosition + 0, "In rule 'Either', expected one of: (EOF|'A'|'B')");
	  }
	}

The error message is predefined, and `[NoDefaultArm]` is not currently supported on individual rules.

This mode probably isn't good enough for professional grammars so I'm taking suggestions for improvements. The other way to use this feature is to selectively enable it in individual loops using `default_error`. For example, this grammar produces the same output as the last one:

	LLLPG(parser)
	{
		rule B @[ 'B' ];
		rule Either @[ ('A' | B | default_error)* ];
	}

`default_error` must be used by itself; it does not support, for example, attaching custom actions.

You can customize the error handling for a particular loop using an `error` branch:

	void Error(string s) { ... }
	
	LLLPG
	{
		rule B @[ 'B' ];
		rule Either @[ ('A' | B | error _ {Error(""Anticipita 'A' aŭ B ĉi tie"");})* ];
	}

In this example I've written a custom error message in [Esperanto](http://en.wikipedia.org/wiki/Esperanto); here's the output:

    void B()
    {
      Match('B');
    }
    void Either()
    {
      int la0;
      for (;;) {
        la0 = LA0;
        if (la0 == 'A')
          Skip();
        else if (la0 == 'B')
          B();
        else if (la0 == EOF)
          break;
        else {
          MatchExcept();
          Error("Anticipita 'A' aŭ B ĉi tie");
        }
      }
    }

Notice that I used `_` inside the `error` branch to skip the invalid terminal. The `error` branch behaves very similarly to a `default` branch except that it does not participate in prediction decisions. A formal way to explain this would be to say that `(A | B | ... | error E)` is equivalent to `(A | B | ... | default ((~_) => E))`, although I didn't actually implement it that way, so maybe it's not perfectly equivalent.

One more thing that I think I should mention about error handling is the `Check()` function, which is used to check that an `&and` predicate matches. Previously you've seen an and-predicate that makes a prediction decision, as in:

	token Number @[
		{dot::bool=false;}
		('.' {dot=true;})?
		'0'..'9'+ (&{!dot} '.' '0'..'9'+)?
	];

In this case `'.' '0'..'9'+` will only be matched if `!dot`:

    ...
    la0 = LA0;
    if (la0 == '.') {
        if (!dot) {
            la1 = LA(1);
            if (la1 >= '0' && la1 <= '9') {
                Skip();
                Skip();
                for (;;) {
                    ...

The code only turns out this way because the follow set of Number is `_*`, as explained in the next article where I talk about the difference between `token` and `rule`. Due to the follow set, LLLPG assumes `Number` might be followed by `'.'` so `!dot` must be included in the prediction decision. But if `Number` is a normal `rule` (and the follow set of `Number` does not include `'.'`):

	rule Number @[
		{dot::bool=false;}
		('.' {dot=true;})?
		'0'..'9'+ (&{!dot} '.' '0'..'9'+)?
	];

Then the generated code is different:

    ...
    la0 = LA0;
    if (la0 == '.') {
      Check(!dot, "!dot");
      Skip();
      MatchRange('0', '9');
      for (;;) {
        ...

In this case, when LLLPG sees `'.'` it decides to enter the optional item `(&{!dot} '.' '0'..'9'+)?`  without checking `&{!dot}` first, because `'.'` is not considered a valid input for _skipping_ the optional item. Basically LLLPG thinks "if there's a dot here, matching the optional item is the only reasonable thing to do". So, it assumes there is a `Check(bool, string)` method, which it calls to check `&{!dot}` _after_ prediction.

Currently you can't (in general) force an and-predicate to be checked as part of prediction; prediction analysis checks and-predicates _only_ when needed to resolve ambiguity. Nor can you suppress `Check` statements or override the second parameter to `Check`. Let me know if this limitation is causing problems for you.

That's it for error handling in LLLPG!

## A random fact

_Did you know?_ Unlike ANTLR, LLLPG does not care much about parenthesis when interpreting loops and alternatives separated by `|` or `/`. For example, all of the following rules are interpreted the same way and produce the same code:

    rule Foo1 @[ ["AB" | "A" | "CD" | "C"]*     ];
    rule Foo2 @[ [("AB" | "A" | "CD" | "C")]*   ];
    rule Foo3 @[ [("AB" | "A") | ("CD" | "C")]* ];
    rule Foo4 @[ ["AB" | ("A" | "CD") | "C"]*   ];
    rule Foo5 @[ ["AB" | ("A" | ("CD" | "C"))]* ];

The loop (`*`) and all the arms are integrated into a single prediction tree, regardless of how you might fiddle with parenthesis. Knowing this may help you understand error messages and generated code better.

Another thing that is Good to Know™ is that `|` behaves differently when the left and right side are terminals. `('1'|'3'|'5'|'9' '9')` is treated not as _four_ alternatives, but only _two_: `('1'|'3'|'5') | '9' '9'`, as you can see from the generated code:

      la0 = LA0;
      if (la0 == '1' || la0 == '3' || la0 == '5')
        Skip();
      else {
        Match('9');
        Match('9');
      }

This happens because a "terminal set" is always a single unit in LLLPG, i.e. multiple terminals like `('A'|'B')` are combined and treated the same as a single terminal `'X'`, whenever the left and right sides of `|` are both terminal sets. If you insert an empty code block into the first alternative, it is no longer treated as a simple terminal, LLLPG cannot join the terminals into a single set anymore. In that case LLLPG sees four alternatives instead, causing different output:

	rule Foo@[ '1' {/*empty*/} | '3' | '5' | '9' '9' ];
	
	void Foo()
	{
	  int la0;
	  la0 = LA0;
	  if (la0 == '1')
	    Skip();
	  else if (la0 == '3')
	    Skip();
	  else if (la0 == '5')
	    Skip();
	  else {
	    Match('9');
	    Match('9');
	  }
	}

## End of part 3

The following topics still remain for future articles:

- `FullLLk` versus "approximate" LL(k)
- `token` versus `rule`. 
- Managing ambiguity.
- The API that LLLPG uses. You've seen it already, of course, I just need to write a complete reference.
- Advanced techniques: tree parsing, keyword parsing, collapsing many precedence levels into a single rule, and other tricks used by the EC# parser.
- All about Loyc and its libraries. Things you can do with LeMP: other source code manipulators besides LLLPG.

Are you using LLLPG to parse an interesting language? Please leave a comment!

## History

LLLPG v1.0.1:

- bug fix (lexers): now we call `MatchExcept(set)` when inverted set contains EOF
- bug fix (parsers): removed `EOF` from `MatchExcept(..., EOF)`
- bug fix: `default` can no longer change parser behavior except for bad input
- increased max params for `Match(...)` from 3 to 4
- errors/warnings include string version of an alt if it is short
- added "Line N: (...|...|...)" comment to output for every `Alts`, and `[AddComments(bool)]` option
- added more useful follow set info at `[Verbosity(2)]` and `[Verbosity(3)]`
- `Error(InputPosition + li, "...")` changed to `Error(li, "...")`

LLLPG v1.1.0:

- Implemented complex ambiguity suppression behavior for / operator (described in part 4)
- Loyc: Removed dependency on nunit.framework.dll, replaced with `Loyc.MiniTest`
- Loyc: Added enum `Severity`. Changed `IMessageSink.Write(Symbol,...)` to `IMessageSink.Write(Severity,...)`
- Rebuilt LesSyntaxForVs2010 to match DLLs used by LLLPG 1.1.0 (for some reason the LLLPG SFG breaks if LES syntax highlighter uses different DLL versions, even though LLLPG has its own copy of all DLLs.)
