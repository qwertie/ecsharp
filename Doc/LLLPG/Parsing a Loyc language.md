Parsing a Loyc language with LLLPG, part 1
==========================================

Introducing LeMP 1.3
--------------------

Today I'll start by giving you a recipe for parsing programming languages with LLLPG. But first I'd like to introduce a couple of other macros that come with LeMP (the macro processing engine that LLLPG runs on top of). 

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

These macros can be used to avoid code duplication in a lexer-parser combo. To learn more, read [Avoid Tedious Coding with LeMP]().

Programming language parsing template
-------------------------------------

So here's the formula. First, define a list of punctuation tokens:

	// A list of simple tokens to be represented literally
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

Keywords need to be handled separately, as explained later.

Next, define an enum for all the token types:

	using TT = TokenType; // Abbreviate TokenType as TT

	// Usually you'll need an enum containing the kinds of tokens you'll recognize.
	public enum TokenType
	{
		EOF = 0, // If you use EOF = 0, default(Token) represents EOF
		Id,
		Num,
		unroll ((TEXT, TOKEN_NAME) in OPERATOR_TOKEN_LIST)
		{
			TOKEN_NAME; // inside 'unroll', must use ';' instead of ',' as separator
		},
		Unknown
	}
