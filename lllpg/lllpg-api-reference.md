---
title: "Reference: APIs called by LLLPG"
layout: article
date: 30 May 2016
---

Here's the list of methods that LLLPG expects to exist. The `MatchRange`/`MatchExceptRange` methods are only used in lexers though, and `EOF` is only used in parsers (lexers refer to EOF as -1):

	// Note: the set type is expected to contain a Contains(MatchType) method.
	static HashSet<MatchType> NewSet(params MatchType[] items);
	static HashSet<MatchType> NewSetOfRanges(params MatchType[] ranges);

	LaType LA0 { get; }
	LaType LA(int i);
	static const LaType EOF;

	void Error(int lookaheadIndex, string message);

	// Normal matching methods
	void Skip();
	Token MatchAny();
	Token Match(MatchType a);
	Token Match(MatchType a, MatchType b);
	Token Match(MatchType a, MatchType b, MatchType c);
	Token Match(MatchType a, MatchType b, MatchType c, MatchType d);
	Token Match(HashSet<MatchType> set);
	Token MatchRange(int aLo, int aHi);
	Token MatchRange(int aLo, int aHi, int bLo, int bHi);
	Token MatchExcept();
	Token MatchExcept(MatchType a);
	Token MatchExcept(MatchType a, MatchType b);
	Token MatchExcept(MatchType a, MatchType b, MatchType c);
	Token MatchExcept(MatchType a, MatchType b, MatchType c, MatchType d);
	Token MatchExcept(HashSet<MatchType> set);
	Token MatchExceptRange(int aLo, int aHi);
	Token MatchExceptRange(int aLo, int aHi, int bLo, int bHi);

	// Used to verify and-predicates in the matching stage
	void Check(bool expectation, string expectedDescr);

	// For backtracking (used by generated Try_Xyz() methods)
	struct SavePosition : IDisposable
	{
		public SavePosition(Lexer lexer, int lookaheadAmt);
		public void Dispose();
	}
	
	// For recognizers (used by generated Scan_Xyz() methods)
	bool TryMatch(MatchType a);
	bool TryMatch(MatchType a, MatchType b);
	bool TryMatch(MatchType a, MatchType b, MatchType c);
	bool TryMatch(MatchType a, MatchType b, MatchType c, MatchType d);
	bool TryMatch(HashSet<MatchType> set);
	bool TryMatchRange(int aLo, int aHi);
	bool TryMatchRange(int aLo, int aHi, int bLo, int bHi);
	bool TryMatchExcept();
	bool TryMatchExcept(MatchType a);
	bool TryMatchExcept(MatchType a, MatchType b);
	bool TryMatchExcept(MatchType a, MatchType b, MatchType c);
	bool TryMatchExcept(MatchType a, MatchType b, MatchType c, MatchType d);
	bool TryMatchExcept(HashSet<MatchType> set);
	bool TryMatchExceptRange(int aLo, int aHi);
	bool TryMatchExceptRange(int aLo, int aHi, int bLo, int bHi);

The following data types are parameters that you can change:

- `LaType`: the data type of LA0 and LA(i). This is always `int` in lexers, but in parsers you can use the `laType(...)` option (documented in the previous article) to change this type.
- `MatchType`: the data type of arguments to `Match`, `MatchExcept`, `TryMatch` and `TryMatchExcept`. In lexers, `MatchType` is always `int`. In parsers, by default, LLLPG generates code as though `MatchType` is same as `LaType`, but `BaseParser` uses `int` instead for performance reasons. Consequently, when using `BaseParser` you need to use the `matchType(int)` option to change `MatchType` to `int`.
- `HashSet<MatchType>` is the declared data type of large sets. By default this is `HashSet<int>` but you can change it using the `setType(...)` option.
- `Token` is the return value of the `Match` methods. LLLPG does not care and does not need to know what this type is. In lexers, these methods should return the character that was matched, and in parsers they should return the token that was matched (if the match fails, BaseLexer and BaseParser still return the character or token, whatever it was.)

And now, here's a brief description of the APIs, with examples.

### NewSet, NewSetOfRanges ###
	
	static HashSet<MatchType> NewSet(params MatchType[] items);
	static HashSet<MatchType> NewSetOfRanges(params MatchType[] ranges);

These are used for large sets, when it would be inappropriate to generate an expression or `Match` call.

	// Example:
	LLLPG(lexer) {
	  rule Vowel @[ 'a'|'e'|'i'|'o'|'u'|'A'|'E'|'I'|'O'|'U' ];
	  rule MaybeHexDigit @[ ['0'..'9'|'a'..'f'|'A'..'F']? ];
	};
	
	// Generated code:
	static readonly HashSet<int> Vowel_set0 = NewSet(
		'A', 'E', 'I', 'O', 'U', 'a', 'e', 'i', 'o', 'u');
	void Vowel()
	{
	  Match(Vowel_set0);
	}
	static readonly HashSet<int> MaybeHexDigit_set0 = 
		NewSetOfRanges('0', '9', 'A', 'F', 'a', 'f');
	void MaybeHexDigit()
	{
	  int la0;
	  la0 = LA0;
	  if (MaybeHexDigit_set0.Contains(la0))
		 Skip();
	}

### LA0, LA(i) ###

	LaType LA0 { get; }
	LaType LA(int i);

LLLPG assumes that there is a state variable somewhere that tracks the "current input position"; the current position is usually called `InputPosition` but LLLPG never refers to it directly. `LA0` returns the character or token at the current position, and `LA(i)` returns the character or token at `InputPosition + i`.

Obviously, a single function `LA(i)` would have been enough, but `LA(0)` is used much more often than `LA(i)` so I decided to define an extra API which gives implementations an opportunity to optimize access to `LA0`. But in case `LA0` and `LA(i)` are nontrivial, LLLPG also caches the value of `LA0` or `LA(i)` in a local variable.

	// Example:
	LLLPG(parser) {
	  token OptionalIndefiniteArticle @[ ('a' 'n' / 'a')? ];
	};
	
	// Generated code:
	void OptionalIndefiniteArticle()
	{
	  int la0, la1;
	  la0 = LA0;
	  if (la0 == 'a') {
		 la1 = LA(1);
		 if (la1 == 'n') {
			Skip();
			Skip();
		 } else
			Skip();
	  }
	}

### EOF (parsers only) ###

	static const LaType EOF;

Occasionally LLLPG needs to check for EOF. For example, the default follow set of a rule is EOF, and when using `NoDefaultArm`, LLLPG may check whether LA0==EOF to see if an error occurred.

	[NoDefaultArm] LLLPG(parser) {
	  rule AllBs @[ 'B'* ];
	};

	void AllBs()
	{
	  int la0;
	  for (;;) {
		 la0 = LA0;
		 if (la0 == 'B')
			Skip();
		 else if (la0 == EOF)
			break;
		 else
			Error(0, "In rule 'MaybeB', expected one of: ('B'|EOF)");
	  }
	}

In lexers, LLLPG uses `-1` instead of `EOF`.

### Error(i, msg) ###

	void Error(int lookaheadIndex, string message);

This method is called by the default error branch with an auto-generated message, as shown in the example above. `lookaheadIndex` is the offset (`LA(lookaheadIndex)`) where the unexpected character or token was encountered (usually 0). Currently, the error message cannot be customized.

### Skip(), MatchAny() ###

	void Skip();
	Token MatchAny();

Both of these methods advance the current position by one character or token. `Skip()` is called when the return value will not be used, while `MatchAny()` is called if the return value is saved.

	// Example
	LLLPG(lexer) {
	  rule WhateverB @[ (_|EOF) [b='B']? ];
	}

	// Generated code
	void WhateverB()
	{
	  int la0;
	  Skip();
	  la0 = LA0;
	  if (la0 == 'B')
		 b = MatchAny();
	}

### Match ###

	Token Match(MatchType a);
	Token Match(MatchType a, MatchType b);
	Token Match(MatchType a, MatchType b, MatchType c);
	Token Match(MatchType a, MatchType b, MatchType c, MatchType d);
	Token Match(HashSet<MatchType> set);

Ensures that `LA0` matches the argument(s) given to `Match`, taking any appropriate action (printing an error message or throwing an exception) if `LA0` does not match the argument(s). Then `LA0` is "consumed", meaning that the input position is increased by one. LLLPG does not care about the return type, but the return value is used in expressions like `zero:='0'` (see example).

	// Example
	LLLPG(lexer) {
	  rule FiveEvenDigits @[ 
	    zero:='0' ('0'|'2') ('0'|'2'|'4') ('0'|'2'|'4'|'6') ('0'|'2'|'4'|'6'|'8')
	  ];
	}

	// Generated code
	static readonly HashSet<int> FiveEvenDigits_set0 = NewSet('0', '2', '4', '6', '8');
	void FiveEvenDigits()
	{
	  var zero = Match('0');
	  Match('0', '2');
	  Match('0', '2', '4');
	  Match('0', '2', '4', '6');
	  Match(FiveEvenDigits_set0);
	}

### MatchRange (lexers only) ###

	Token MatchRange(int aLo, int aHi);
	Token MatchRange(int aLo, int aHi, int bLo, int bHi);
	
Matches `LA0` against a range of characters, then increases the input position by one.

	// Example
	LLLPG(lexer) {
	  rule LetterDigit @[ ('a'..'z'|'A'..'Z') '0'..'9' ]; 
	}

	// Generated code
	void LetterDigit()
	{
	  MatchRange('A', 'Z', 'a', 'z');
	  MatchRange('0', '9');
	}

### MatchExcept ###

	Token MatchExcept();
	Token MatchExcept(MatchType a);
	Token MatchExcept(MatchType a, MatchType b);
	Token MatchExcept(MatchType a, MatchType b, MatchType c);
	Token MatchExcept(MatchType a, MatchType b, MatchType c, MatchType d);
	Token MatchExcept(HashSet<MatchType> set);

Ensures that `LA0` does **not** match the argument(s) given to `MatchExcept`, taking any appropriate action (printing an error message or throwing an exception) if `LA0` matches the argument(s). Then `LA0` is "consumed", meaning that the input position is increased by one.

In addition, all overloads except the last one must test that `LA0` is not `EOF`. This rule makes `MatchExcept()` (with no arguments) different from `MatchAny()` which does allow `EOF`.

When a set is passed to `MatchExcept`, that set will explicitly contain EOF when EOF is _not_ allowed.

	// Example (remember that _ does NOT match EOF)
	LLLPG(parser) {
	  rule MatchExcept @[ _ ~A ~(A|B) ~(A|B|C) ~(A|B|C|D)
	                      ~(A|B|C|D|E) (~E | EOF) ]; 
	}

	// Generated code
	static readonly HashSet<int> NotA_set0 = NewSet(A, B, C, D, E, EOF);
	static readonly HashSet<int> NotA_set1 = NewSet(E);
	void MatchExcept()
	{
	  MatchExcept();
	  MatchExcept(A);
	  MatchExcept(A, B);
	  MatchExcept(A, B, C);
	  MatchExcept(A, B, C, D);
	  MatchExcept(NotA_set0);
	  MatchExcept(NotA_set1);
	}

### MatchExceptRange (lexers only) ###

	Token MatchExceptRange(int aLo, int aHi);
	Token MatchExceptRange(int aLo, int aHi, int bLo, int bHi);

Verifies that `LA0` is not within the specified range(s) of characters, then increases the input position by one.

	// Example
	LLLPG(lexer) {
	  rule NotInRanges @[ ~('0'..'9') ~('a'..'z'|'A'..'Z') ]; 
	}

	// Generated code
	void NotInRanges()
	{
	  MatchExceptRange('0', '9');
	  MatchExceptRange('A', 'Z', 'a', 'z');
	}

### Check ###

	void Check(bool expectation, string expectedDescr);

As explained in the section §"Error handling mechanisms in LLLPG" (part 3), this is called to check and-predicate conditions during matching if they were not verified during prediction.

	// Example
	LLLPG(lexer) {
		token DosEquis @[ &!{condition} 'X' 'X' ]; 
	}
	
	// Generated code	
	void DosEquis()
	{
	  Check(!condition, "!(condition)");
	  Match('X');
	  Match('X');
	}

### SavePosition ###

	struct SavePosition : IDisposable
	{
		public SavePosition(Lexer lexer, int lookaheadAmt);
		public void Dispose();
	}

This is used for backtracking. `SavePosition` must save the current input position in its constructor, then restore it in `Dispose()`.

	// Example
	LLLPG(lexer) {
		token JustOneCapital @[ 'A'..'Z' &!('A'..'Z') ]; 
	}

	// Generated code
	void JustOneCapital()
	{
	  MatchRange('A', 'Z');
	  Check(!Try_JustOneCapital_Test0(0), "!([A-Z])");
	}
	private bool Try_JustOneCapital_Test0(int lookaheadAmt)
	{
	  using (new SavePosition(this, lookaheadAmt))
		 return JustOneCapital_Test0();
	}
	private bool JustOneCapital_Test0()
	{
	  if (!TryMatchRange('A', 'Z'))
		 return false;
	  return true;
	}

### TryMatch ###

	bool TryMatch(MatchType a);
	bool TryMatch(MatchType a, MatchType b);
	bool TryMatch(MatchType a, MatchType b, MatchType c);
	bool TryMatch(MatchType a, MatchType b, MatchType c, MatchType d);
	bool TryMatch(HashSet<MatchType> set);

Tests whether `LA0` matches the argument(s) given to `TryMatch`. Returns true if `LA0` is a match and false if not. The input position is increased by one.

	// Example
	LLLPG(lexer) {
		[recognizer { bool ScanFiveEvenDigits(); }]
		rule FiveEvenDigits @[ 
			zero:='0' ('0'|'2') ('0'|'2'|'4') ('0'|'2'|'4'|'6') ('0'|'2'|'4'|'6'|'8')
		];
	}

	// Generated code
	static readonly HashSet<int> FiveEvenDigits_set0 = NewSet('0', '2', '4', '6', '8');
	void FiveEvenDigits()
	{
	  var zero = Match('0');
	  Match('0', '2');
	  Match('0', '2', '4');
	  Match('0', '2', '4', '6');
	  Match(FiveEvenDigits_set0);
	}
	bool Try_ScanFiveEvenDigits(int lookaheadAmt)
	{
	  using (new SavePosition(this, lookaheadAmt))
		 return ScanFiveEvenDigits();
	}
	bool ScanFiveEvenDigits()
	{
	  if (!TryMatch('0'))
		 return false;
	  if (!TryMatch('0', '2'))
		 return false;
	  if (!TryMatch('0', '2', '4'))
		 return false;
	  if (!TryMatch('0', '2', '4', '6'))
		 return false;
	  if (!TryMatch(FiveEvenDigits_set0))
		 return false;
	  return true;
	}

### TryMatchRange (lexers only) ###

	bool TryMatchRange(int aLo, int aHi);
	bool TryMatchRange(int aLo, int aHi, int bLo, int bHi);
	
Tests whether `LA0` matches one or two ranges of characters. Returns true if `LA0` is a match and false if not. The input position is increased by one.

	// Example
	LLLPG(lexer) {
		[recognizer { bool ScanLetterDigit(); }]
		rule LetterDigit @[ ('a'..'z'|'A'..'Z') '0'..'9' ]; 
	}
	
	// Generated code
	void LetterDigit()
	{
	  MatchRange('A', 'Z', 'a', 'z');
	  MatchRange('0', '9');
	}
	bool Try_ScanLetterDigit(int lookaheadAmt)
	{
	  using (new SavePosition(this, lookaheadAmt))
		 return ScanLetterDigit();
	}
	bool ScanLetterDigit()
	{
	  if (!TryMatchRange('A', 'Z', 'a', 'z'))
		 return false;
	  if (!TryMatchRange('0', '9'))
		 return false;
	  return true;
	}

### TryMatchExcept ###

	bool TryMatchExcept();
	bool TryMatchExcept(MatchType a);
	bool TryMatchExcept(MatchType a, MatchType b);
	bool TryMatchExcept(MatchType a, MatchType b, MatchType c);
	bool TryMatchExcept(MatchType a, MatchType b, MatchType c, MatchType d);
	bool TryMatchExcept(HashSet<MatchType> set);

Tests whether `LA0` matches the argument(s) given to `TryMatch`. Returns **false** if `LA0` is a match and true if not. The input position is increased by one.

In addition, all overloads except the last one must test that `LA0` is not `EOF`. This rule makes `TryMatchExcept()` (with no arguments) different from `Skip()` which does allow `EOF`.

**Note**: as I write this, it occurs to me that these APIs are redundant. LLLPG could have called `TryMatch(...)` instead and inverted the return value. Should this API be removed in a future version?

	// Example (remember that _ does NOT match EOF)
	LLLPG(parser) {
	  [recognizer { bool TryMatchExcept(); }]
	  rule MatchExcept @[ _ ~A ~(A|B) ~(A|B|C) ~(A|B|C|D)
	                      ~(A|B|C|D|E) (~E | EOF) ]; 
	}

	// Generated code
	static readonly HashSet<int> MatchExcept_set0 = NewSet(A, B, C, D, E, EOF);
	static readonly HashSet<int> MatchExcept_set1 = NewSet(E);
	void MatchExcept()
		{ /* Omitted for brevity */ }
	bool Try_TryMatchExcept(int lookaheadAmt)
	{
	  using (new SavePosition(this, lookaheadAmt))
		 return TryMatchExcept();
	}
	bool TryMatchExcept()
	{
	  if (!TryMatchExcept())
		 return false;
	  if (!TryMatchExcept(A))
		 return false;
	  if (!TryMatchExcept(A, B))
		 return false;
	  if (!TryMatchExcept(A, B, C))
		 return false;
	  if (!TryMatchExcept(A, B, C, D))
		 return false;
	  if (!TryMatchExcept(MatchExcept_set0))
		 return false;
	  if (!TryMatchExcept(MatchExcept_set1))
		 return false;
	  return true;
	}

### TryMatchExceptRange (lexers only) ###

	bool TryMatchExceptRange(int aLo, int aHi);
	bool TryMatchExceptRange(int aLo, int aHi, int bLo, int bHi);

Tests whether `LA0` matches one or two ranges of characters. Returns **false** if `LA0` is a match and **true** if not. The input position is increased by one.

I'll skip the example this time: I think by now you get the idea.
