The Loyc LL(k) Parser Generator: Part 5
=======================================

TODO: then again we should save some stuff -- e.g. regex dos, details on simplified workflow -- for a "relaunch" article.
Frustrated that regular expressions are unintelligible, repetitive, hard to get right and don't recurse? Concerned about regex DOS attacks? Like Lightweight Language Parsers? Then LLLPG is for you!

Welcome to part 5
-----------------

I've finally decided to finish this article series and to add a few new features to make LLLPG more flexible and appealing, especially as a alternative to regexes.

To recap, LLLPG is a parser generator integrated into an "Enhanced" C# language. The tool accepts normal C# code interspersed with LLLPG grammars or grammar fragments, and it outputs plain C#. Advantages of LLLPG over other tools:

- LLLPG generates simple, relatively concise, fast code.
- As a Visual Studio Custom Tool, it is ideal for medium-size parsing tasks that are a bit too big for a regex. LLLPG is also sophisticated enough to parse complex languages like "enhanced C#", LLLPG's own input language.
- You can add a parser to an existing class--ideal for writing `static Parse` methods.
- You can avoid memory allocation during parsing (ideal for parsing short strings!)
- No runtime library is required (although I suggest using LoycCore as your runtime library for maximum flexibility.) (TODO: update NuGet package AND standalone BaseLexer, rename it to LexerSource)
- Short learning curve: LLLPG is intuitive to use because it augments an existing programming language and _doesn't_ attempt to do everything on your behalf. Also, the generated code follows the structure of the input code so you can easily see how the tool behaves.
- Just one parsing model to learn: some other systems use one model (regex) for lexers and something else for parsers. Often lexers have a completely different syntax than parsers, and the lexer can't handle things like nested comments (lex and yacc are even separate programs!). LLLPG uses just a single model, LL(k); its lexers and parsers have nearly identical syntax and behavior.
- For tricky situations, LLLPG offers zero-width asertions (a.k.a. semantic & syntactic predicates) and "gates".
- Compared to regexes, LLLPG allows recursive grammars, often reduces repetitions of grammar fragments, and because LLLPG only supports LL(k), it mitigates the risk of [regex denial-of-service attacks](http://en.wikipedia.org/wiki/ReDoS). On the other hand, LLLPG is less convenient in that grammars tend to be longer than regexes, changing the grammar requires the LLLPG tool to be installed, and writing an LL(k) grammar correctly may require more thought than writing a regex.
- Compared to ANTLR, LLLPG is designed for C# rather than Java, so naturally there's a Visual Studio plugin, and I don't [sell half of the documentation as a book](http://www.amazon.ca/The-Definitive-ANTLR-4-Reference/dp/1934356999). Syntax is comparable to ANTLR, but superficially different because unlike ANTLR rules, LLLPG rules resemble function declarations. Also, I recently tried ANTLR 4 and I was shocked at how inefficient the output code appears to be.
- Bonus features from LeMP (more on that later)

And Introducing LeMP
--------------------

Today I'll give you a recipe for writing parsers for programming languages with LLLPG. But first I'd like to introduce a couple of other macros that come with LeMP, the macro processing engine that LLLPG runs on top of. 

1. `replace` is a macro that replaces all instances of some pattern with some other pattern. For example,

		/// Input
		replace (MB => MessageBox.Show, 
			     FMT($fmt, $arg) => string.Format($fmt, $arg))
		{
			MB(FMT("Hi, I'm {0}...", name));
			MB(FMT("I am {0} years old!", name.Length));
		}

		/// Output
		MessageBox.Show(string.Format("Hi, I'm {0}...", name));
		MessageBox.Show(string.Format("I am {0} years old!", name.Length));

	The braces are optional. If the braces are present, replacement occurs only inside the braces; if you end with a semicolon instead of braces, replacement occurs on all remaining statements in the same block.
	
	This example requires `FMT` to take exactly two arguments called `$fmt` and `$arg`, but we could also capture any number of arguments or statements like this:
	
		FMT($fmt, $(params args)) => string.Format($fmt, $args) // 1 or more args
		FMT($(params args)) => string.Format($args)             // 0 or more args

2. `unroll..in` is a kind of compile-time `foreach` loop. It generates several copies of a piece of code, replacing one or more identifiers each time. Unlike `replace`, `unroll` can only match simple identifiers on the left side of `in`.
	
		/// Input
		void SetInfo(string firstName, string lastName, object data, string phoneNumber)
		{
			unroll ((VAR) in (firstName, lastName, data, phoneNumber)) {
				if (VAR != null) throw new ArgumentNullException(nameof(VAR));
			}
			...
		}
		/// Output
		void SetInfo(string firstName, string lastName, object data, string phoneNumber)
		{
			if (firstName != null) 
				throw new ArgumentNullException(nameof("firstName"));
			if (lastName != null)
				throw new ArgumentNullException(nameof("lastName"));
			if (data != null)
				throw new ArgumentNullException(nameof("data"));
			if (phoneNumber != null)
				throw new ArgumentNullException(nameof("phoneNumber"));
			...
		}
	
	This example also uses the `nameof()` macro to convert each variable name to a string.

LeMP includes a number of other macros and you can also write your own, but these two macros can help shorten a large parser.


..

	
And now for the recipe for writing a lexer and parser in EC#.

	// A list of simple tokens to be represented literally (note: a slightly more
	// sophisticated approach is needed for keywords, see LLLPG Part 5 article.)
	replace (OPERATOR_TOKEN_LIST => (
		(">>", Shr),    // Note: as a general rule, in your lexer you should list 
		("<<", Shl),    // longer operators first. We will use this token list 
		("=", Assign),  // in the lexer, so longer operators are listed first here.
		(">",  GT),
		("<",  LT),
		("^",  Exp),
		("*",  Mul),
		("/",  Div),
		("+",  Add),
		("-",  Sub),
		(";",  Semicolon),
		("(",  LParen),
		(")",  RParen)));


New features of LLLPG 1.3
-------------------------

- "External API": in LLLPG 1.1 you had to write a class derived from `BaseLexer` or `BaseParser` which contained the LLLPG APIs such as `Match`, `LA0`, `Error`, etc. Now you can encapsulate that API in a field or a local variable. This means you can have a different base class, or you can put a lexer/parser inside a value type (`struct`) or a `static class`.
- "Automatic Value Saver: in LLLPG 1.1, if you wanted to save the return value of a rule or token, you sometimes had to manually create an associated variable. In the new version, you can attach a "label" to any terminal or nonterminal, which will make LLLPG create a variable automatically. Even better, you can often get away with not attaching a label.
- Automatic return value: when you use `$result` or the `result:` label in a rule, LLLPG automatically creates a variable called `result` to hold the return value of the current rule, and it returns that value at the end of the method.
- `any` command, `inline` rules, and implicit LLLPG blocks: Details below.

Using LLLPG with an external API
--------------------------------

You can use the `inputSource` and `inputClass` options to designate an object to which LLLPG should send all its API calls. `inputClass` should be the data type of the object that `inputSource` refers to. For example, if you specify `inputSource(src)`, LLLPG will translate a grammar fragment like `'+'|'-'` into code like `src.Match('+','-')`. Without the `inputSource` option, this would simply be `Match('+','-')`.

Loyc.Syntax.dll (part of the LoycCore NuGet package) have `LexerSource` and `LexerSource<C>` types, which are derived from `BaseLexer` and provide the LLLPG Lexer API. When using these options, a lexer will look something like this:

    using Loyc;
    using Loyc.Syntax.Lexing;
    
    public class MyLexer {
      public MyLexer(string input, string fileName = "") { 
        src = new LexerSource((UString)input, fileName);
      }
      LexerSource src;
      LLLPG (lexer(inputSource(src), inputClass(LexerSource))) {
        public rule MyToken Token() @[ Id | Spaces | Newline ];
        private rule Id             @[ IdStartChar (IdStartChar|'0'..'9'|'\'')* ];
        private rule IdStartChar    @[ 'a'..'z'|'A'..'Z'|'_' ];
        private rule Spaces         @[ (' '|'\t')+ ];
        private rule Newline        @[ ('\n' | '\r' '\n'?)
          {src.AfterNewline();} // increments LineNumber
        ];
      }
    }

`LexerSource` accepts any implementation of `ICharSource`; `ICharSource` represents a source of characters with a `Slice(...)` method, which is used to speed up access to individual characters. If your input is simply a string, convert the string to `LexerSource` using `new LexerSource((UString)S)`. `UString` is a wrapper around `string` that implements the `ICharSource` interface (the U in `UString` means "unicode"; see the (documentation of UString)[http://loyc.net/doc/code/structLoyc_1_1UString.html] for details.)

Automatic Value Saver
---------------------

Often you need to store stuff in variables, and in LLLPG 1.1 this was inconvenient because you had to manually create variables to hold stuff. Now LLLPG can create the variables for you. For example, this lexer parses an integer:

    // Usage: int num = new IntParser("1234").Parse();
    public class IntParser : BaseLexer {
      // Note: a string converts implicitly to UString
      public ParseInt(UString s) : base(s) {}
      LLLPG (lexer(terminalType: int)) {
        public rule int Parse() @[ 
          {int i = 0;}
          ' '+ (digit:'0'..'9' {i = 10*i + ($digit - '0');})+
          {return i;}
        ];
      }
    }

The label `digit:` causes LLLPG to create a variable at the beginning of the method (`int digit = default(int)`), and to assign the result of matching to it (`digit = MatchRange('0', '9')`). As in previous versions, you can also write `digit='0'..'9'`, but this only assigns the digit to a variable, it doesn't _create_ the variable. The type of the variable is controlled by the `terminalType(...)` option, but the default for a lexer is `int` so it wasn't needed in this example. Inside code blocks you can write either `$digit` or just `digit`.

Frankly, this feature isn't very useful with `BaseLexer` because `BaseLexer` only provides the token's value. It generally makes more sense in parsers:

You can also use the (admittedly weird-looking) `+:` operator to add stuff to a list. For example:

    // Usage: int num = new IntParser("1234").Parse();
    public class IntParser : BaseLexer {
      public ParseInt(UString s) : base(s) {}
      LLLPG (lexer) {
        public rule int Parse() @[
          // LLLPG inserts {List<int> digits = new List<int>();} at the beginning of the method
          ' '+ (digits+:'0'..'9')+
          // Use LINQ to convert the list of digits to an integer
          {return digits.Aggregate(0, (n, d) => n * 10 + (d - '0'));}
        ];
      }
    }

In ANTLR you use `+=` to accomplish the same thing. Obviously, `+:` is uglier; unfortunately I had already defined `+=` as "add something to a _user-defined_ list", whereas `+:` means "automatically create a list at the beginning of the method, and add something to it now".

You can only use `:`, `+:`, `=` and `+=` on "primitive" predicates: terminal sets and rule references. For example, `digits:(('0'..'9')+)` and `digits+:(('0'..'9')+)` are illegal; but `(digits+:('0'..'9'))+` is legal.

How to avoid memory allocation in a lexer
-----------------------------------------

I mentioned that LLLPG lets you avoid memory allocation, and now I will demonstrate. Avoiding memory allocation in a full-blown parser is almost impossible, since you need to allocate memory to hold your syntax tree. But in simpler situations, you can optimize your scanner to avoid creating garbage objects.

The following example parses email addresses without allocating _any_ memory, beyond a single `LexerSource`, which is allocated only once per thread:

    struct EmailAddress
    {
      public EmailAddress(string userName, string domain) 
        { UserName = userName; Domain = domain; }
      public UString UserName;
      public UString Domain;
      public override string ToString() { return UserName + ""@"" + Domain; }

      LLLPG (lexer(inputSource(src), inputClass(LexerSource))) {
        // LexerSource provides the APIs expected by LLLPG. This is
        // static to avoid reallocating the helper object for each email.
        [ThreadStatic] static LexerSource<UString> src;
          
			  // <summary>Parses email addresses according to RFC 5322, not including 
			  // quoted usernames or non-ASCII addresses (TODO: support Unicode).</summary>
        public static rule EmailAddress Parse(UString email)
        {
          if (src == null)
            src = new LexerSource<UString>(email, "", 0, false);
          else
            src.Reset(email, "", 0, false); // re-use old object
          
          @[ UsernameChars(src) ('.' UsernameChars(src))* ];
          int at = src.InputPosition;
          UString userName = email.Substring(0, at);
          
          @[ '@' DomainCharSeq(src) ('.' DomainCharSeq(src))* EOF ];
          UString domain = email.Substring(at + 1);
          return new EmailAddress(userName, domain);
        }
        static rule UsernameChars(LexerSource<UString> src) @[
          ('a'..'z'|'A'..'Z'|'0'..'9'|'!'|'#'|'$'|'%'|'&'|'\''|
          '*'|'+'|'/'|'='|'?'|'^'|'_'|'`'|'{'|'|'|'}'|'~'|'-')+
        ];
        static rule DomainCharSeq(LexerSource<UString> src) @[
                 ('a'..'z'|'A'..'Z'|'0'..'9')
          ( '-'? ('a'..'z'|'A'..'Z'|'0'..'9') )*
        ];
      }
    }

This example demonstrates that you can pass the `LexerSource` between rules as a parameter, although it's actually redundant here, and the `src` parameters could be safely removed.

Here's how this example avoids memory allocation:

1. `LexerSource` is allocated only once in a thread-local variable, then re-used by calling `Reset(...)` on subsequent calls. `Reset(...)` takes the same parameters as the contructor.
2. It uses `UString` instead of `string`. `UString` is a `struct` and it represents a slice of a string. When this example calls `email.Substring()` it's not creating a new string, it's simply creating a `UString` that refers to part of the `email` string.
3. It uses `LexerSource<UString>` instead of `LexerSource`. Remember that `LexerSource` accepts a reference to `ICharSource`, so if you write `new LexerSource((UString)"string")` you are boxing `UString` on the heap. In contrast, `new LexerSource<UString>((UString)"string")` does not box the `UString`.
4. It uses the four-argument constructor `new LexerSource<UString>(email, "", 0, false)`. The last argument is the important one; by default `LexerSource` allocates a `LexerSourceFile` object (the `LexerSource.SourceFile` property) which keeps track of where the line breaks are located in the file so that you can convert between integer indexes and (Line, Column) pairs. By setting this parameter to `false` you are turning off this feature to avoid memory allocations.

Keyword parsing
---------------

Collapsing precedence levels into a single rule
-----------------------------------------------

Tree parsing
------------



Bonus features with LeMP
------------------------




TODO: Update BaseParser, make it more user-friendly.
