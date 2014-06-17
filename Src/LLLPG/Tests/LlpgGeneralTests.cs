using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Loyc.MiniTest;
using Loyc;
using Loyc.Syntax;
using Loyc.Utilities;

namespace Loyc.LLParserGenerator
{
	using S = CodeSymbols;
	using Loyc.Collections;
	using Ecs.Parser;
	using System.Diagnostics;
	using Loyc.Syntax.Les;

	/// <summary>Tests LLLPG with the whole <see cref="LeMP.MacroProcessor"/> pipeline.</summary>
	/// <remarks>All input examples are written in LES.</remarks>
	[TestFixture]
	class LlpgGeneralTests
	{
		[Test]
		public void SimpleMatching()
		{
			Test(@"
			LLLPG_stage1 @[ 'x' '0'..'9'* ];
			LLLPG lexer {
				[pub] rule Foo @[ 'x' '0'..'9' '0'..'9' ];
			}", @"
				('x', @`suf*`('0'..'9'));
				public void Foo()
				{
					Match('x');
					MatchRange('0', '9');
					MatchRange('0', '9');
				}");
		}
		
		[Test]
		public void SimpleAltsOptStar()
		{
			DualLanguageTest(@"
			LLLPG lexer {
				[pub] rule a @[ 'A'|'a' ];
				[pub] rule b @[ 'B'|'b' ];
				[pub] rule Foo @[ (a | b? 'c')* ];
			}", @"
			LLLPG(lexer) {
				// Verify that three different rule syntaxes all work
				public rule a @[ 'A'|'a' ];
				public rule b() @[ 'B'|'b' ];
				public rule void Foo() @[ (a | b? 'c')* ];
			}", @"
				public void a()
				{
					Match('A', 'a');
				}
				public void b()
				{
					Match('B', 'b');
				}
				public void Foo()
				{
					int la0;
					for (;;) {
						switch (LA0) {
						case 'A':
						case 'a':
							a();
							break;
						case 'B':
						case 'b':
						case 'c':
							{
								la0 = LA0;
								if (la0 == 'B' || la0 == 'b')
									b();
								Match('c');
							}
							break;
						default:
							goto stop;
						}
					}
				stop:;
				}");
		}

		[Test]
		public void SquareBracketTest()
		{
			// New feature: LLLPG stage one accepts square brackets followed by 
			// ? or * for optional items.
			DualLanguageTest(@"
			LLLPG parser {
				[pub] rule Foo @[ [A]? [B]* ];
			}", @"
			LLLPG(parser) {
				public rule void Foo @[ [A]? [B]* ];
			}", @"
				public void Foo()
				{
					int la0;
					la0 = LA0;
					if (la0 == A)
						Skip();
					for (;;) {
						la0 = LA0;
						if (la0 == B)
							Skip();
						else
							break;
					}
				}");
		}

		[Test]
		public void MatchInvertedSet()
		{
			Test(@"LLLPG lexer {
				[pub] rule Except @[ ~'a' ~('a'..'z') ];
				[pub] rule String @[ '""' ~('""'|'\n')* '""' ];
			}", @"
				public void Except()
				{
					MatchExcept('a');
					MatchExceptRange('a', 'z');
				}
				public void String()
				{
					int la0;
					Match('""');
					for (;;) {
						la0 = LA0;
						if (!(la0 == -1 || la0 == '\n' || la0 == '""'))
							Skip();
						else
							break;
					}
					Match('""');
				}
			");
		}

		[Test]
		public void BigInvertedSets()
		{
			DualLanguageTest(@"LLLPG lexer {
				[pub] rule DisOrDat @[ ~('a'..'z'|'A'..'Z'|'0'..'9'|'_') | {Lowercase();} 'a'..'z' ];
				[pub] rule Dis      @[ ~('a'..'z'|'A'..'Z'|'0'..'9'|'_') ];
			}", @"LLLPG (lexer) {
				public rule DisOrDat @[ ~('a'..'z'|'A'..'Z'|'0'..'9'|'_') | {Lowercase();} 'a'..'z' ];
				public rule Dis      @[ ~('a'..'z'|'A'..'Z'|'0'..'9'|'_') ];
			}", @"
				static readonly HashSet<int> DisOrDat_set0 = NewSetOfRanges(-1, -1, '0', '9', 'A', 'Z', '_', '_', 'a', 'z');
				public void DisOrDat()
				{
					int la0;
					la0 = LA0;
					if (!DisOrDat_set0.Contains(la0))
						Skip();
					else {
						Lowercase();
						MatchRange('a', 'z');
					}
				}
				public void Dis()
				{
					MatchExcept(DisOrDat_set0);
				}");
			Test(@"LLLPG parser {
				[pub] rule DisOrDat @[ ~('a'|'b'|'c'|'d'|1|2|3|4) | 'a' 1 ];
				[pub] rule Dis      @[ ~('a'|'b'|'c'|'d'|1|2|3|4) ];
			}", @"
				static readonly HashSet<int> DisOrDat_set0 = NewSet(1, 2, 3, 4, 'a', 'b', 'c', 'd', EOF);
				public void DisOrDat()
				{
					int la0;
					la0 = LA0;
					if (!DisOrDat_set0.Contains(la0))
						Skip();
					else {
						Match('a');
						Match(1);
					}
				}
				public void Dis()
				{
					MatchExcept(DisOrDat_set0);
				}");
		}

		[Test]
		public void FullLL2()
		{
			string lesAndEcs = @"
			[FullLLk] LLLPG (lexer) {
				public rule FullLL2 @[ ('a' 'b' | 'b' 'a') 'c' | ('a' 'a' | 'b' 'b') 'c' ];
			}";
			DualLanguageTest(lesAndEcs, lesAndEcs, @"
				public void FullLL2()
				{
					int la0, la1;
					do {
						la0 = LA0;
						if (la0 == 'a') {
							la1 = LA(1);
							if (la1 == 'b')
								goto match1;
							else
								goto match2;
						} else {
							la1 = LA(1);
							if (la1 == 'a')
								goto match1;
							else
								goto match2;
						}
					match1:
						{
							la0 = LA0;
							if (la0 == 'a') {
								Skip();
								Match('b');
							} else {
								Match('b');
								Match('a');
							}
							Match('c');
						}
						break;
					match2:
						{
							la0 = LA0;
							if (la0 == 'a') {
								Skip();
								Match('a');
							} else {
								Match('b');
								Match('b');
							}
							Match('c');
						}
					} while (false);
				}
			");
		}

		[Test]
		public void SemPredUsingLI()
		{
			// You can write a semantic predicate using the replacement "$LI" which
			// will insert the index of the current lookahead, or "$LA" which 
			// inserts a variable that holds the actual lookahead symbol. Test this 
			// feature with two different lookahead amounts for the same predicate.
			Test(@"LLLPG lexer {
				[pub] rule Id() @[ &{char.IsLetter($LA)} _ (&{char.IsLetter($LA) || char.IsDigit($LA)} _)* ];
				[pub] rule Twin() @[ 'T' &{$LA == LA($LI+1)} '0'..'9' '0'..'9' ];
				[pub] token Token() @[ Twin / Id ];
			}", @"
				public void Id()
				{
					int la0;
					Check(char.IsLetter(LA0), ""@char.IsLetter($LA)"");
					MatchExcept();
					for (;;) {
						la0 = LA0;
						if (la0 != -1) {
							la0 = LA0;
							if (char.IsLetter(la0) || char.IsDigit(la0))
								Skip();
							else
								break;
						} else
							break;
					}
				}
				public void Twin()
				{
					Match('T');
					Check(LA0 == LA(0 + 1), ""$LA == LA($LI + 1)"");
					MatchRange('0', '9');
					MatchRange('0', '9');
				}
				public void Token()
				{
					int la0, la1;
					la0 = LA0;
					if (la0 == 'T') {
						la0 = LA0;
						if (char.IsLetter(la0)) {
							la1 = LA(1);
							if (la1 >= '0' && la1 <= '9') {
								la1 = LA(1);
								if (la1 == LA(1 + 1))
									Twin();
								else
									Id();
							} else
								Id();
						} else
							Twin();
					} else
						Id();
				}
			");
		}

		[Test]
		public void DifferentDefault3()
		{
			string input = @"LLLPG(lexer)
				{
					rule VowelOrNot() @[ 
						('A'|'E'|'I'|'O'|'U') {Vowel();} / 'A'..'Z' {Consonant();}
					];
					rule ConsonantOrNot @[ 
						default ('A'|'E'|'I'|'O'|'U') {Other();} / 'A'..'Z' {Consonant();}
					];
				}";
			DualLanguageTest(input, input, @"
				void VowelOrNot()
				{
					switch (LA0) {
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
		}

		[Test]
		public void SymbolTest()
		{
			DualLanguageTest(@"
			LLLPG parser(laType(Symbol), allowSwitch(@false)) {
				public rule Stmt @[ @@Number (@@print @@DQString | @@goto @@Number) @@Newline ];
				public rule Stmts @[ Stmt* ];
			}", @"
			LLLPG(parser(laType(Symbol), allowSwitch(false))) {
				public rule Stmt @[ @@Number (@@print @@DQString | @@goto @@Number) @@Newline ];
				public rule Stmts @[ Stmt* ];
			}", @"
				public void Stmt()
				{
					Symbol la0;
					Match(@@Number);
					la0 = LA0;
					if (la0 == @@print) {
						Skip();
						Match(@@DQString);
					} else {
						Match(@@goto);
						Match(@@Number);
					}
					Match(@@Newline);
				}
				public void Stmts()
				{
					Symbol la0;
					for (;;) {
						la0 = LA0;
						if (la0 == @@Number)
							Stmt();
						else
							break;
					}
				}
			");
		}

		[Test]
		public void Actions()
		{
			Test(@"LLLPG {
				rule Foo {
					Blah0; Blah1;
					@[ 'x' ];
					@[ { Blah2; Blah3; } 'y' ];
					Blah5; Blah6;
				};
			}", @"
				void Foo()
				{
					Blah0;
					Blah1;
					Match('x');
					Blah2;
					Blah3;
					Match('y');
					Blah5;
					Blah6;
				}");
		}

		[Test]
		public void RuleRefWithArgs()
		{
			Test(@"LLLPG lexer {
				rule NTokens(max::int) @[ 
					{x:=0;} (&{x < max}               ~('\n'|'\r'|' ')+  {x++;})?
					greedy  (&{x < max} greedy(' ')+ (~('\n'|'\r'|' '))* {x++;})*
				];
				rule Line @[ c:='0'..'9' NTokens(c - '0') ('\n'|'\r')? ];
			}", 
			@"
				void NTokens(int max)
				{
					int la0;
					var x = 0;
					la0 = LA0;
					if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == ' ')) {
						Check(x < max, ""x < max"");
						Skip();
						for (;;) {
							la0 = LA0;
							if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == ' '))
								Skip();
							else
								break;
						}
						x++;
					}
					for (;;) {
						la0 = LA0;
						if (la0 == ' ') {
							Check(x < max, ""x < max"");
							Skip();
							for (;;) {
								la0 = LA0;
								if (la0 == ' ')
									Skip();
								else
									break;
							}
							for (;;) {
								la0 = LA0;
								if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == ' '))
									Skip();
								else
									break;
							}
							x++;
						} else
							break;
					}
				}
				void Line()
				{
					int la0;
					var c = MatchRange('0', '9');
					NTokens(c - '0');
					la0 = LA0;
					if (la0 == '\n' || la0 == '\r')
						Skip();
				}
				");
		}

		[Test]
		public void SynPred0()
		{
			Test(@"
			LLLPG lexer {
				rule Digit(x::int) @[ '0'..'9' ];
				[recognizer { protected def IsOddDigit(y::float); }]
				rule OddDigit(x::int) @[ '1'|'3'|'5'|'7'|'9' ];
				rule NonDigit @[ &!Digit(7) _ ];
				rule EvenDigit @[ &!OddDigit _ ];
			};", @"
				void Digit(int x)
				{
					MatchRange('0', '9');
				}
				bool Try_Scan_Digit(int lookaheadAmt, int x)
				{
					using (new SavePosition(this, lookaheadAmt))
						return Scan_Digit(x);
				}
				bool Scan_Digit(int x)
				{
					if (!TryMatchRange('0', '9'))
						return false;
					return true;
				}
				static readonly HashSet<int> OddDigit_set0 = NewSet('1', '3', '5', '7', '9');
				void OddDigit(int x)
				{
					Match(OddDigit_set0);
				}
				protected bool Try_IsOddDigit(int lookaheadAmt, float y)
				{
					using (new SavePosition(this, lookaheadAmt))
						return IsOddDigit(y);
				}
				protected bool IsOddDigit(float y)
				{
					if (!TryMatch(OddDigit_set0))
						return false;
					return true;
				}
				void NonDigit()
				{
					Check(!Try_Scan_Digit(0, 7), ""!(Digit)"");
					MatchExcept();
				}
				void EvenDigit()
				{
					Check(!Try_IsOddDigit(0), ""!(OddDigit)"");
					MatchExcept();
				}");
		}

		[Test]
		public void SynPred1()
		{
			Test(@"
			LLLPG lexer {
				token Number @[
					&('0'..'9'|'.')
					'0'..'9'* ('.' '0'..'9'+)?
				];
			};", @"
				void Number()
				{
					int la0, la1;
					Check(Try_Number_Test0(0), ""[.0-9]"");
					for (;;) {
						la0 = LA0;
						if (la0 >= '0' && la0 <= '9')
							Skip();
						else
							break;
					}
					la0 = LA0;
					if (la0 == '.') {
						la1 = LA(1);
						if (la1 >= '0' && la1 <= '9') {
							Skip();
							Skip();
							for (;;) {
								la0 = LA0;
								if (la0 >= '0' && la0 <= '9')
									Skip();
								else
									break;
							}
						}
					}
				}
				private bool Try_Number_Test0(int lookaheadAmt)
				{
					using (new SavePosition(this, lookaheadAmt))
						return Number_Test0();
				}
				private bool Number_Test0()
				{
					if (!TryMatchRange('.', '.', '0', '9'))
						return false;
					return true;
				}
			");
		}

		[Test]
		public void HexFloatsWithSynPred() // regression
		{
			string input = @"
			LLLPG lexer {
				extern rule DecDigits() @[ '0'..'9'+ ];
				rule HexDigit()  @[ '0'..'9' | 'a'..'f' | 'A'..'F' ];
				rule HexDigits() @[ { Hex(); } HexDigit+ ('_' HexDigit+)* ];
				token HexNumber() @[
					'0' ('x'|'X')
					HexDigits?
					// Avoid ambiguity with 0x5.Equals(): a dot is not enough
					(	'.' &(HexDigits ('p'|'P') ('+'|'-'|'0'..'9')) HexDigits )?
					( ('p'|'P') ('+'|'-')? DecDigits )?
				];
			}";
			string expect = @"
				static readonly HashSet<int> HexDigit_set0 = NewSetOfRanges('0', '9', 'A', 'F', 'a', 'f');
				void HexDigit()
				{
					Match(HexDigit_set0);
				}
				bool Scan_HexDigit()
				{
					if (!TryMatch(HexDigit_set0))
						return false;
					return true;
				}
				void HexDigits()
				{
					int la0, la1;
					Hex();
					HexDigit();
					for (;;) {
						la0 = LA0;
						if (HexDigit_set0.Contains(la0))
							HexDigit();
						else
							break;
					}
					for (;;) {
						la0 = LA0;
						if (la0 == '_') {
							la1 = LA(1);
							if (HexDigit_set0.Contains(la1)) {
								Skip();
								HexDigit();
								for (;;) {
									la0 = LA0;
									if (HexDigit_set0.Contains(la0))
										HexDigit();
									else
										break;
								}
							} else
								break;
						} else
							break;
					}
				}
				bool Scan_HexDigits()
				{
					int la0, la1;
					if (!Scan_HexDigit())
						return false;
					for (;;) {
						la0 = LA0;
						if (HexDigit_set0.Contains(la0)) {
							if (!Scan_HexDigit())
								return false;
						} else
							break;
					}
					for (;;) {
						la0 = LA0;
						if (la0 == '_') {
							la1 = LA(1);
							if (HexDigit_set0.Contains(la1)) {
								if (!TryMatch('_'))
									return false;
								if (!Scan_HexDigit())
									return false;
								for (;;) {
									la0 = LA0;
									if (HexDigit_set0.Contains(la0)) {
										if (!Scan_HexDigit())
											return false;
									} else
										break;
								}
							} else
								break;
						} else
							break;
					}
					return true;
				}
				void HexNumber()
				{
					int la0, la1;
					Match('0');
					Match('X', 'x');
					la0 = LA0;
					if (HexDigit_set0.Contains(la0))
						HexDigits();
					la0 = LA0;
					if (la0 == '.') {
						la1 = LA(1);
						if (HexDigit_set0.Contains(la1)) {
							if (Try_HexNumber_Test0(1)) {
								Skip();
								HexDigits();
							}
						}
					}
					la0 = LA0;
					if (la0 == 'P' || la0 == 'p') {
						la1 = LA(1);
						if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
							Skip();
							la0 = LA0;
							if (la0 == '+' || la0 == '-')
								Skip();
							DecDigits();
						}
					}
				}
				static readonly HashSet<int> HexNumber_Test0_set0 = NewSetOfRanges('+', '+', '-', '-', '0', '9');
				private bool Try_HexNumber_Test0(int lookaheadAmt)
				{
					using (new SavePosition(this, lookaheadAmt))
						return HexNumber_Test0();
				}
				private bool HexNumber_Test0()
				{
					if (!Scan_HexDigits())
						return false;
					if (!TryMatch('P', 'p'))
						return false;
					if (!TryMatch(HexNumber_Test0_set0))
						return false;
					return true;
				}";
			Test(input, expect);

			// Hex floats that support "0x0.0" (without p0) suffered from a different
			// bug, where the first branch of the syn pred ('0'..'9') was unreachable.
			// The root cause was that the follow set of all syn preds was empty. In
			// this case the two branches are ambiguous, and since the input "'0'..'9' 
			// not followed by anything" (not even EOF) is impossible, LLLPG resolved
			// the ambiguity by always choosing the second branch. Fixed by setting
			// IsToken=true on all "mini-recognizers" (HexNumber_Test0 in this case).
			Test(@"
			LLLPG lexer {
				private rule HexDigit()  @[ '0'..'9' | 'a'..'f' | 'A'..'F' ];
				private rule HexDigits() @[ HexDigit+ ];
				private rule HexNumber() @[
					'0' ('x'|'X')
					HexDigits?
					// Avoid ambiguity with 0x5.Equals(): a dot is not enough
					(	'.' &( '0'..'9' / HexDigits ('p'|'P') ('+'|'-'|'0'..'9') ) 
						HexDigits )?
					//( ('p'|'P') ('+'|'-')? '0'..'9'+ )?
				];
			}",
			@"static readonly HashSet<int> HexDigit_set0 = NewSetOfRanges('0', '9', 'A', 'F', 'a', 'f');
			void HexDigit()
			{
				Match(HexDigit_set0);
			}
			bool Scan_HexDigit()
			{
				if (!TryMatch(HexDigit_set0))
					return false;
				return true;
			}
			void HexDigits()
			{
				int la0;
				HexDigit();
				for (;;) {
					la0 = LA0;
					if (HexDigit_set0.Contains(la0))
						HexDigit();
					else
						break;
				}
			}
			bool Scan_HexDigits()
			{
				int la0;
				if (!Scan_HexDigit())
					return false;
				for (;;) {
					la0 = LA0;
					if (HexDigit_set0.Contains(la0))
						{if (!Scan_HexDigit())
							return false;}
					else
						break;
				}
				return true;
			}
			void HexNumber()
			{
				int la0;
				Match('0');
				Match('X', 'x');
				la0 = LA0;
				if (HexDigit_set0.Contains(la0))
					HexDigits();
				la0 = LA0;
				if (la0 == '.') {
					Skip();
					Check(Try_HexNumber_Test0(0), ""([0-9] / HexDigits [Pp] [+\\-0-9])"");
					HexDigits();
				}
			}
			static readonly HashSet<int> HexNumber_Test0_set0 = NewSetOfRanges('+', '+', '-', '-', '0', '9');
			private bool Try_HexNumber_Test0(int lookaheadAmt)
			{
				using (new SavePosition(this, lookaheadAmt))
					return HexNumber_Test0();
			}
			private bool HexNumber_Test0()
			{
				int la0;
				la0 = LA0;
				if (la0 >= '0' && la0 <= '9')
					{if (!TryMatchRange('0', '9'))
						return false;}
				else {
					if (!Scan_HexDigits())
						return false;
					if (!TryMatch('P', 'p'))
						return false;
					if (!TryMatch(HexNumber_Test0_set0))
						return false;
				}
				return true;
			}");
		}

		[Test]
		public void ErrorBranchTest()
		{
			// This example contrasts a rule that does not have an error branch
			// against a rule that does. There is also a third rule (Token2) in
			// which SQString was left out. Notice that in Token1, LLLPG uses the
			// error branch in case of invalid input like \"\n, but in Token2,
			// the same input (\"\n) will invoke SQString. That's because, for
			// better or for worse, the error branch is only used above LL(1) 
			// when LLLPG has to decide between two or more rules, or in other
			// words, when it is resolving ambguity. Perhaps this design should
			// be changed, I just haven't decided how it should work instead.
			//
			// To shorten output, SQString & TQString are suppressed with "extern".
			Test(@"[DefaultK(3)] 
			LLLPG lexer {
				extern token SQString @[
					'\'' ('\\' _ {_parseNeeded = true;} | ~('\''|'\\'|'\r'|'\n'))* '\''
				];
				[k(4)]
				extern token TQString @[
					""'''"" nongreedy(_)* ""'''""
				];
				token Token0 @[ // No error branch
					( TQString
					/ SQString 
					| ' '
					)];
				token Token1 @[
					( TQString 
					/ SQString
					| ' '
					| error { Error(); }
					  ( EOF | _ )
					)];
				token Token2 @[
					( SQString
					| error { Error(); }
					  ( EOF | _ )
					)];
			};",
			@"
				void Token0()
				{
					int la0, la1, la2;
					la0 = LA0;
					if (la0 == '\'') {
						la1 = LA(1);
						if (la1 == '\'') {
							la2 = LA(2);
							if (la2 == '\'')
								TQString();
							else
								SQString();
						} else
							SQString();
					} else
						Match(' ');
				}
				void Token1()
				{
					int la0, la1, la2;
					do {
						la0 = LA0;
						if (la0 == '\'') {
							la1 = LA(1);
							if (la1 == '\'') {
								la2 = LA(2);
								if (la2 == '\'')
									TQString();
								else
									SQString();
							} else if (!(la1 == -1 || la1 == '\n' || la1 == '\r'))
								SQString();
							else
								goto match4;
						} else if (la0 == ' ')
							Skip();
						else
							goto match4;
						break;
					match4:
						{
							Error();
							Skip();
						}
					} while (false);
				}
				void Token2()
				{
					int la0;
					la0 = LA0;
					if (la0 == '\'')
						SQString();
					else {
						Error();
						Skip();
					}
				}");
		}

		[Test]
		public void KeywordTrieTest()
		{
			// By the way, it's more efficient to use a gate for this: 
			// (Not_IdContChar=>) instead of &Not_IdContChar. But LLLPG
			// had trouble with this version, so that's what I'm testing.
			Test(@"
			LLLPG lexer {
				[extern] token Id @[
					('a'..'z'|'A'..'Z'|'_') ('a'..'z'|'A'..'Z'|'0'..'9'|'_')*
				];
				token Not_IdContChar @[
					~('a'..'z'|'A'..'Z'|'0'..'9'|'_'|'#') | EOF
				];
				[k(12)]
				token IdOrKeyword @
					[ ""case""  &Not_IdContChar
					/ ""catch"" &Not_IdContChar
					/ ""char""  &Not_IdContChar
					/ Id ];
			}", @"
				static readonly HashSet<int> Not_IdContChar_set0 = NewSetOfRanges('#', '#', '0', '9', 'A', 'Z', '_', '_', 'a', 'z');
				void Not_IdContChar()
				{
					MatchExcept(Not_IdContChar_set0);
				}
				bool Try_Scan_Not_IdContChar(int lookaheadAmt)
				{
					using (new SavePosition(this, lookaheadAmt))
						return Scan_Not_IdContChar();
				}
				bool Scan_Not_IdContChar()
				{
					if (!TryMatchExcept(Not_IdContChar_set0))
						return false;
					return true;
				}
				void IdOrKeyword()
				{
					int la0, la1, la2, la3, la4;
					la0 = LA0;
					if (la0 == 'c') {
						la1 = LA(1);
						if (la1 == 'a') {
							la2 = LA(2);
							if (la2 == 's') {
								la3 = LA(3);
								if (la3 == 'e') {
									if (Try_Scan_Not_IdContChar(4)) {
										Skip();
										Skip();
										Skip();
										Skip();
									} else
										Id();
								} else
									Id();
							} else if (la2 == 't') {
								la3 = LA(3);
								if (la3 == 'c') {
									la4 = LA(4);
									if (la4 == 'h') {
										if (Try_Scan_Not_IdContChar(5)) {
											Skip();
											Skip();
											Skip();
											Skip();
											Skip();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else if (la1 == 'h') {
							la2 = LA(2);
							if (la2 == 'a') {
								la3 = LA(3);
								if (la3 == 'r') {
									if (Try_Scan_Not_IdContChar(4)) {
										Skip();
										Skip();
										Skip();
										Skip();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else
						Id();
				}");
		}

		[Test]
		public void FewerArgsInRecognizer()
		{
			// LLLPG can truncate arguments when generating a recognizer. In this
			// example, the call Foo(2, -2) becomes RecognizeFoo(2)
			Test(@"LLLPG parser {
				[recognizer(def RecognizeFoo(z::int))]
				rule Foo(x::int, y::int) @[ 'F''u' ];
				rule Foo2 @[ Foo(2, -2) '2' ];
				rule Main @[ Foo2 (&Foo2 _)? ];
			}", @"
				void Foo(int x, int y)
				{
					Match('F');
					Match('u');
				}
				bool Try_RecognizeFoo(int lookaheadAmt, int z)
				{
					using (new SavePosition(this, lookaheadAmt))
						return RecognizeFoo(z);
				}
				bool RecognizeFoo(int z)
				{
					if (!TryMatch('F'))
						return false;
					if (!TryMatch('u'))
						return false;
					return true;
				}
				void Foo2()
				{
					Foo(2, -2);
					Match('2');
				}
				bool Try_Scan_Foo2(int lookaheadAmt)
				{
					using (new SavePosition(this, lookaheadAmt))
						return Scan_Foo2();
				}
				bool Scan_Foo2()
				{
					if (!RecognizeFoo(2))
						return false;
					if (!TryMatch('2'))
						return false;
					return true;
				}
				void Main()
				{
					int la0;
					Foo2();
					la0 = LA0;
					if (la0 != EOF) {
						Check(Try_Scan_Foo2(0), ""Foo2"");
						Skip();
					}
				}");
		}

		[Test]
		public void AliasInLexer()
		{
			Test(@"LLLPG lexer {
				alias Quote = '\'';
				alias Bang = '!';
				rule QuoteBang @[ Bang | Quote QuoteBang Quote ];
			}", @"
				void QuoteBang()
				{
					int la0;
					la0 = LA0;
					if (la0 == '!')
						Skip();
					else {
						Match('\'');
						QuoteBang();
						Match('\'');
					}
				}");
		}

		[Test]
		public void AliasInParser()
		{
			Test(@"LLLPG parser {
				alias('\'' = TokenType.Quote);
				alias(Bang = TokenType.ExclamationMark);
				rule QuoteBang @[ Bang | '\'' QuoteBang '\'' ];
			}", @"
				void QuoteBang()
				{
					int la0;
					la0 = LA0;
					if (la0 == TokenType.ExclamationMark)
						Skip();
					else {
						Match(TokenType.Quote);
						QuoteBang();
						Match(TokenType.Quote);
					}
				}");
		}

		[Test]
		public void ExplicitEof()
		{
			Test(@"LLLPG parser {
				extern rule Atom() @[ Id | Number | '(' Expr ')' ];
				rule Expr() @[ Atom (('+'|'-'|'*'|'/') Atom | error { Error(); })* ];
				rule Start() @[ Expr EOF ];
			}", @"
				void Expr()
				{
					Atom();
					for (;;) {
						switch (LA0) {
						case '*':
						case '/':
						case '-':
						case '+':
							{
								Skip();
								Atom();
							}
							break;
						case ')':
						case EOF:
							goto stop;
						default:
							{
								Error();
							}
							break;
						}
					}
				 stop:;
				}
				void Start()
				{
					Expr();
					Match(EOF);
				}");
		}

		[Test]
		public void SetTypeCast()
		{
			Test(@"LLLPG parser(laType(TokenType), matchType(int), allowSwitch(@false)) {
				extern rule Atom() @[ Id | Number | '(' Expr ')' ];
				rule Expr() @[ Atom (('+'|'-'|'*'|'/'|'%'|'^') Atom)* ];
			}", @"
				static readonly HashSet<int> Expr_set0 = NewSet((int) '%', (int) '*', (int) '/', (int) '-', (int) '^', (int) '+');
				void Expr()
				{
					TokenType la0;
					Atom();
					for (;;) {
						la0 = LA0;
						if (Expr_set0.Contains((int) la0)) {
							Skip();
							Atom();
						} else
							break;
					}
				}");
		}

		[Test]
		public void ExceptSetWithOrWithoutEOF()
		{
			// Here, the generated code can't be "MatchExcept(Id)" because by 
			// convention that means "not Id and not EOF". MatchExcept(Set), in 
			// contrast, doesn't prohibit EOF unless the set contains EOF.
			Test(@"LLLPG parser {
				// Note: ~Id alone means 'not Id and not EOF'; we add '|EOF' to allow EOF
				rule NoId @[ ~Id|EOF ];
				rule NonId @[ ~Id ];
			}", @"
				static readonly HashSet<int> NoId_set0 = NewSet(Id);
				void NoId()
				{
					MatchExcept(NoId_set0);
				}
				void NonId()
				{
					MatchExcept(Id);
				}");
			
			Test(@"LLLPG lexer {
				// Note: ~Id alone means 'not Id and not EOF'; we add '|EOF' to allow EOF
				rule NoId @[ ~'X'|EOF ];
				rule NonId @[ ~'X' ];
			}", @"
				static readonly HashSet<int> NoId_set0 = NewSet('X');
				void NoId()
				{
					MatchExcept(NoId_set0);
				}
				void NonId()
				{
					MatchExcept('X');
				}");

			// NonDigit_set0 must include EOF
			Test(@"LLLPG parser {
				rule NonDigit @[ ~('0'|'1'|'2'|'3'|'4'|'5'|'6'|'7'|'8'|'9') ];
				rule NoDigit  @[ ~('0'|'1'|'2'|'3'|'4'|'5'|'6'|'7'|'8'|'9')|EOF ];
			}", @"
				static readonly HashSet<int> NonDigit_set0 = NewSet('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', EOF);
				void NonDigit()
				{
					MatchExcept(NonDigit_set0);
				}
				static readonly HashSet<int> NoDigit_set0 = NewSet('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
				void NoDigit()
				{
					MatchExcept(NoDigit_set0);
				}");
			Test(@"LLLPG lexer {
				rule NonDigit @[ ~('A'|'E'|'I'|'O'|'U'|'a'|'e'|'i'|'o'|'u') ];
				rule NoDigit  @[ ~('A'|'E'|'I'|'O'|'U'|'a'|'e'|'i'|'o'|'u')|EOF ];
			}", @"
				static readonly HashSet<int> NonDigit_set0 = NewSet(-1, 'A', 'E', 'I', 'O', 'U', 'a', 'e', 'i', 'o', 'u');
				void NonDigit()
				{
					MatchExcept(NonDigit_set0);
				}
				static readonly HashSet<int> NoDigit_set0 = NewSet('A', 'E', 'I', 'O', 'U', 'a', 'e', 'i', 'o', 'u');
				void NoDigit()
				{
					MatchExcept(NoDigit_set0);
				}");
		}

		[Test]
		public void RightRecursive()
		{
			Test(@"LLLPG lexer {
				rule A @[ 'a' B ];
				rule B @[ 'b' C ];
				rule C @[ 'c' A | '$' ];
			}", @"
				void A() {
					Match('a');
					B();
				}
				void B() {
					Match('b');
					C();
				}
				void C() {
					int la0;
					la0 = LA0;
					if (la0 == 'c') {
						Skip();
						A();
					} else
						Match('$');
				}");
		}

		[Test]
		public void LocalAndPred()
		{
			// Local flag blocks hoisting to caller
			Test(@"LLLPG lexer {
				rule A @[ BC? 'a' ];
				rule BC @[ &{[Local] $LA + 1 == LA($LI+1)} 'a'..'y' 'b'..'z' ];
			}", @"
				void A()
				{
					int la0, la1;
					la0 = LA0;
					if (la0 == 'a') {
						la1 = LA(1);
						if (la1 >= 'b' && la1 <= 'z')
							BC();
					} else if (la0 >= 'b' && la0 <= 'y')
						BC();
					Match('a');
				}
				void BC()
				{
					Check(LA0 + 1 == LA(0 + 1), ""$LA + 1 == LA($LI + 1)"");
					MatchRange('a', 'y');
					MatchRange('b', 'z');
				}");

			// The [Local] flag also prevents hoisting to the SAME rule, which is important
			// for the technique (used by LES and EC# parsers) of collapsing many precedence
			// levels into a single rule.
			Test(@"LLLPG parser(laType(string)) {
				rule Expr(context::Precedence)::object @[
					{prec::Precedence;}
					result:=(Id|Number)
					greedy(
						// Infix operator with order-of-operations detection
						&{[Local] context.CanParse(prec = GetInfixPrecedence(LA($LI)))}
						op:=(""+""|""-""|""*""|""/""|"">""|""<""|""==""|""="")
						rhs:=Expr(prec)
						{result = NewOperatorNode(result, op, rhs);}
					)*
					{return result;}
				];
			}", @"
				object Expr(Precedence context)
				{
					string la1;
					Precedence prec;
					var result = Match(Id, Number);
					for (;;) {
						switch (LA0) {
						case ""-"":
						case ""*"":
						case ""/"":
						case ""+"":
						case ""<"":
						case ""="":
						case ""=="":
						case "">"":
							{
								if (context.CanParse(prec = GetInfixPrecedence(LA(0)))) {
									la1 = LA(1);
									if (la1 == Id || la1 == Number) {
										var op = MatchAny();
										var rhs = Expr(prec);
										result = NewOperatorNode(result, op, rhs);
									} else
										goto stop;
								} else
									goto stop;
							}
							break;
						default:
							goto stop;
						}
					}
					stop:;
					return result;
				}");
		}

		[Test]
		public void TypeParameterBug()
		{
			// An obscure bug encountered while writing the EC# parser. It's hard 
			// to describe this bug; let it suffice to say that follow sets were 
			// not always complete. In this case PrimaryExpr had only EOF as its
			// follow set, so the test &(TParams (~TT.Id | EOF)) had no effect.
			Test(@"LLLPG parser(laType(string), allowSwitch(@false), setType(HashSet!string))
				{
					extern rule IdAtom @[
						(TT.Id|TT.TypeKeyword)
					];
					extern rule TParams() @[ // type parameters
						( ""<"" (IdAtom ("","" IdAtom)*)? "">"" )
					];			
		
					private rule PrimaryExpr @[
						TT.Id
						greedy(
							&(TParams (~TT.Id | EOF)) ""<"" greedy(_)* => TParams()
						)*
					];

					// An intermediate rule was needed to trigger the bug
					private rule PrefixExpr @[ PrimaryExpr ];

					private rule Expr() @[
						e:=PrefixExpr
						greedy
						(	// Infix operator
							( ""*""|""/""|""+""|""-""|""<""|"">""|""<=,>=""|""==,!="" )
							Expr()
						)*
					];
				}",
				@"void PrimaryExpr()
				{
					string la0;
					Skip();
					for (;;) {
						la0 = LA0;
						if (la0 == ""<"") {
							if (Try_PrimaryExpr_Test0(0))
								TParams();
							else
								break;
						} else
							break;
					}
				}
				void PrefixExpr()
				{
					PrimaryExpr();
				}
				static readonly HashSet<string> Expr_set0 = NewSet(""-"", ""*"", ""/"", ""+"", ""<"", ""<=,>="", ""==,!="", "">"");
				void Expr()
				{
					string la0, la1;
					var e = PrefixExpr();
					for (;;) {
						la0 = LA0;
						if (Expr_set0.Contains(la0)) {
							la1 = LA(1);
							if (la1 == TT.Id) {
								Skip();
								Expr();
							} else
								break;
						} else
							break;
					}
				}
				static readonly HashSet<string> PrimaryExpr_Test0_set0 = NewSet(TT.Id);
				private bool Try_PrimaryExpr_Test0(int lookaheadAmt)
				{
					using (new SavePosition(this, lookaheadAmt))
						return PrimaryExpr_Test0();
				}
				private bool PrimaryExpr_Test0()
				{
					if (!Scan_TParams())
						return false;
					if (!TryMatchExcept(PrimaryExpr_Test0_set0))
						return false;
					return true;
				}");
		}

		[Test]
		public void SlugTest()
		{
			// [2013-12-25]
			// It became very clear while writing the EC# grammar that large, 
			// complex, ambiguous grammars could make LLLPG run very slowly, but 
			// this was the first SMALL grammar I could find that would make LLLPG 
			// run very slowly (25 seconds to analyze 'Start' with k=2!).
			Test(@"[DefaultK(2)] [FullLLk(false)]
			LLLPG lexer {
				rule PositiveDigit @[ '1'..'9' {""Think positive!""} ];
				rule WeirdDigit @[ '0' | &{a} '1' | &{b} '2' | &{c} '3' 
				       | &{d} '4' | &{e} '5' | &{f} '6' | &{g} '7'
				       | &{h} '8' | &{i} '9' ];
				rule Start @[ (WeirdDigit / PositiveDigit)* ];
			}",
				@"void PositiveDigit()
				{
					MatchRange('1', '9');
					""Think positive!"";
				}
				void WeirdDigit()
				{
					switch (LA0) {
					case '0': Skip(); break;
					case '1': { Check(a, ""a""); Skip(); } break;
					case '2': { Check(b, ""b""); Skip(); } break;
					case '3': { Check(c, ""c""); Skip(); } break;
					case '4': { Check(d, ""d""); Skip(); } break;
					case '5': { Check(e, ""e""); Skip(); } break;
					case '6': { Check(f, ""f""); Skip(); } break;
					case '7': { Check(g, ""g""); Skip(); } break;
					case '8': { Check(h, ""h""); Skip(); } break;
					default:  { Check(i, ""i""); Match('9'); } break;
					}
				}
				void Start()
				{
					int la0;
					for (;;) {
						la0 = LA0;
						if (la0 >= '1' && la0 <= '9') {
							if (a || b || c || d || e || f || g || h || i)
								WeirdDigit();
							else
								PositiveDigit();
						} else if (la0 == '0')
							WeirdDigit();
						else
							break;
					}
				}");

			Console.WriteLine("-----------------");

			// This example is just a slight variation on the first, but it is
			// more difficult to fix.
			//
			// This takes over 15 seconds to analyze 'Start' with k=2, and
			// 8 minutes and 20 seconds (and almost 2 GB memory) for k=3!).
			Test(@"[DefaultK(2)] [FullLLk(false)] //[Verbosity(3)]
			LLLPG lexer {
				rule PositiveDigit @[ '1'..'9' {""Think positive!""} ];
				rule WeirdDigit @[ '0' | &{a} '1' | &{b} '2' | &{c} '3' 
				       | &{d} '4' | &{e} '5' | &{f} '6' | &{g} '7'
				       | &{h} '8' | &{i} '9' ];
				rule Start @[ WeirdDigit+ / PositiveDigit+ ];
			}",
				@"void PositiveDigit()
				{
					MatchRange('1', '9');
					""Think positive!"";
				}
				void WeirdDigit()
				{
					switch (LA0) {
					case '0': Skip(); break;
					case '1': { Check(a, ""a""); Skip(); } break;
					case '2': { Check(b, ""b""); Skip(); } break;
					case '3': { Check(c, ""c""); Skip(); } break;
					case '4': { Check(d, ""d""); Skip(); } break;
					case '5': { Check(e, ""e""); Skip(); } break;
					case '6': { Check(f, ""f""); Skip(); } break;
					case '7': { Check(g, ""g""); Skip(); } break;
					case '8': { Check(h, ""h""); Skip(); } break;
					default:  { Check(i, ""i""); Match('9'); } break;
					}
				}
				void Start()
				{
					int la0;
					do {
						la0 = LA0;
						if (la0 >= '1' && la0 <= '9') {
							if (a || b || c || d || e || f || g || h || i)
								goto match1;
							else {
								PositiveDigit();
								for (;;) {
									la0 = LA0;
									if (la0 >= '1' && la0 <= '9')
										PositiveDigit();
									else
										break;
								}
							}
						} else
							goto match1;
						break;
					match1:
						{
							WeirdDigit();
							for (;;) {
								la0 = LA0;
								if (la0 >= '0' && la0 <= '9')
									WeirdDigit();
								else
									break;
							}
						}
					} while (false);
				}");
		}

		[Test]
		public void Regressions()
		{
			// 2014-2-05: Weird threading bug while adding --timeout option
			// The bug was that StageOneParser.ReclassifyTokens was called twice on 
			// the same tokens. Mysteriously, this caused parsing errors only when
			// StageOneParser ran on an a worker thread (i.e. for --timeout=i, i != 0)
			Test(@"LLLPG lexer { rule Foo @[ _ greedy('g')* _ ]; }",
				@"void Foo() {
					int la0, la1;
					MatchExcept();
					for(;;) {
						la0 = LA0;
						if (la0=='g') {
							la1 = LA(1);
							if (la1 != -1)
								Skip();
							else
								break;
						} else
							break;
					}
					MatchExcept();
				}");


			// 2013-12-01: Regression test: $LI and $LA were not replaced inside call targets or attributes
			Test(@"LLLPG parser { 
				rule Foo() @[ &!{LA($LI) == $LI} &{$LI() && Bar($LA())} &{[Foo($LA)] $LI} _ ];
			}", @"void Foo()
				{
					Check(!(LA(0) == 0), ""!(LA($LI) == $LI)"");
					Check(0() && Bar(LA0()), ""$LI() && Bar($LA())"");
					Check($LI, ""$LI"");
					MatchExcept();
				}");

			// 2013-12-22: I really thought by now that I had found most of the
			// bugs, but this example exposed two separate bugs:
			// 1. {Money();} was dropped by Alts constructor, as the inner and
			//    outer Alts were merged and should not have been.
			// 2. GenerateExtraMatchingCode() didn't add "break;" before "match1:"
			Test(@"
			LLLPG lexer {
				rule Test @[
					({Money();} ('$' {Dollar();} | '#' {Pound();}))?
					'$'
				];
			}", @"
				void Test()
				{
					int la0, la1;
					do {
						la0 = LA0;
						if (la0 == '$') {
							la1 = LA(1);
							if (la1 == '$')
								goto match1;
						} else if (la0 == '#')
							goto match1;
						break;
					match1: {
							Money();
							la0 = LA0;
							if (la0 == '$') {
								Skip();
								Dollar();
							} else {
								Match('#');
								Pound();
							}
						}
					} while(false);
					Match('$');
				}");
			
			// 2013-12-22: A variation on the same bug in GenerateExtraMatchingCode
			Test(@"
			LLLPG lexer {
				rule Test @[
					(	'a'..'b' 'c'
					|	'b'..'c' 'a'
					)?	'$'
				];
			}",
			@"	void Test()
				{
					int la0, la1;
					do {
						la0 = LA0;
						if (la0 == 'b') {
							la1 = LA(1);
							if (la1 == 'c')
								goto match1;
							else
								goto match2;
						} else if (la0 == 'a')
							goto match1;
						else if (la0 == 'c')
							goto match2;
						break;
					match1:
						{
							Skip();
							Match('c');
						}
						break;
					match2:
						{
							Skip();
							Match('a');
						}
					} while (false);
					Match('$');
				}");

			// This grammar used to crash LLLPG with a NullReferenceException.
			// The output doesn't seem quite right; probably because of the left recursion.
			Test(@"[FullLLk] LLLPG parser(laType(TT), matchType(int), allowSwitch(@true)) {
				private rule Atom @[
					TT.Id (TT.LParen TT.RParen)?
				];
				token Expr @[
					greedy(
						Atom
					|	&{foo}
						greedy(Expr)+
					)*
				];
			}", // Output changed 2013-12-21; doesn't matter because grammar is invalid.
			@"	void Atom()
				{
					TT la0;
					Skip();
					la0 = LA0;
					if (la0 == TT.LParen) {
						Skip();
						Match((int) TT.RParen);
					}
				}
				void Expr()
				{
					TT la0, la1;
					for (;;) {
						la0 = LA0;
						if (la0 == TT.Id) {
							la1 = LA(1);
							if (la1 == TT.Id || la1 == TT.LParen)
								Atom();
							else
								break;
						} else
							break;
					}
				}
			",
			MessageSink.Trace); // Suppress warnings caused by this test
		}

		class TestCompiler : LeMP.TestCompiler
		{
			public TestCompiler(IMessageSink sink, ICharSource text, string fileName = "")
				: base(sink, text, fileName)
			{
				MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude.Les"));
				MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("Loyc.LLParserGenerator"));
				MacroProcessor.AbortTimeout = TimeSpan.Zero;
				AddMacros(Assembly.GetExecutingAssembly());
			}
		}

		IMessageSink _sink = new SeverityMessageFilter(MessageSink.Console, Severity.Debug);

		// This method can be used when the LES and EC# versions of a grammar 
		// produce identical output: two inputs, one output.
		void DualLanguageTest(string inputLES, string inputECS, string expected, IMessageSink sink = null)
		{
			Test(inputLES, expected, sink); // default is LES
			Test(inputECS, expected, sink, EcsLanguageService.Value);
		}
		void Test(string input, string expected, IMessageSink sink = null, IParsingService parser = null)
		{
			using (ParsingService.PushCurrent(parser ?? LesLanguageService.Value))
			using (LNode.PushPrinter(Ecs.EcsNodePrinter.PrintPlainCSharp)) {
				var c = new TestCompiler(sink ?? _sink, new StringSlice(input));
				c.Run();
				Assert.AreEqual(StripExtraWhitespace(expected), StripExtraWhitespace(c.Output.ToString()));
			}
		}
		static string StripExtraWhitespace(string a) { return LeMP.TestCompiler.StripExtraWhitespace(a); }

		#region Calculator example (LES)
		// This is the oldest version of the calculator example, and uses
		// questionable pactices like const int tokens instead of enums. The
		// generated code would not compile anymore either (e.g. BaseLexer no 
		// longer has a type parameter), but that's okay, it's still valid as a 
		// regression test.

		[Test]
		public void CalculatorLexerLes()
		{
			string input = @"import Loyc.LLParserGenerator;
			public partial class Calculator
			{
				const(id::int = 1, num::int = 2, set::int = ':');
				const(mul::int = '*', div::int = '/', add::int = '+', sub::int = '-');
				const(lparen::int = '(', rparen::int = ')', unknown::int = '?');
				const(EOF::int = -1);

				struct Token {
					public Type::int;
					public Value::object;
					public StartIndex::int;
				};

				class Lexer(BaseLexer!UString)
				{
					public cons Lexer(source::UString)
					{
						base(source);
					};
					protected override def Error(li::int, message::string)
					{
						Console.WriteLine(""At index {0}: {1}"", InputPosition+li, message);
					};

					_type::int;
					_value::double;
					_start::int;

					LLLPG lexer
					{
						[pub] token NextToken()::Token {
							_start = InputPosition;
							_value = null;
							@[ { _type = num; } Num
							 | { _type = id;  } Id
							 | { _type = mul; } '*'
							 | { _type = div; } '/'
							 | { _type = add; } '+'
							 | { _type = sub; } '-'
							 | { _type = set; } ':' '='
							 | { _type = num; } "".nan"" { _value = double.NaN; }
							 | { _type = num; } "".inf"" { _value = double.PositiveInfinity; }
							 | error
							   { _type = EOF; } (_ { _type = unknown; })? ];
							return (new Token() { Type = _type; Value = _value; StartIndex = _start; });
						};
						[priv] token Id() @[
							('a'..'z'|'A'..'Z'|'_')
							('a'..'z'|'A'..'Z'|'_'|'0'..'9')*
							{ _value = CharSource.Substring(_startIndex, InputPosition - _startIndex); }
						];
						[priv] token Num() @[
							{dot::bool = @false;}
							('.' {dot = @true;})?
							'0'..'9'+
							(&!{dot} '.' '0'..'9'+)?
							{ _value = double.Parse(CharSource.Slice(_startIndex, InputPosition - _startIndex)); }
						];
					};
				};
			};";
			string expectedOutput = @"
			using Loyc.LLParserGenerator;
			public partial class Calculator
			{
				const int id = 1, num = 2, set = ':';
				const int mul = '*', div = '/', add = '+', sub = '-';
				const int lparen = '(', rparen = ')', unknown = '?';
				const int EOF = -1;
				struct Token
				{
					public int Type;
					public object Value;
					public int StartIndex;
				}
				class Lexer : BaseLexer<UString>
				{
					public Lexer(UString source) : base(source)
					{
					}
					protected override void Error(int li, string message)
					{
						Console.WriteLine(""At index {0}: {1}"", InputPosition + li, message);
					}
					int _type;
					double _value;
					int _start;
					public Token NextToken()
					{
						int la0, la1;
						_start = InputPosition;
						_value = null;
						do {
							la0 = LA0;
							switch (la0) {
							case '.':
								{
									la1 = LA(1);
									if (la1 >= '0' && la1 <= '9')
										goto match1;
									else if (la1 == 'n') {
										_type = num;
										Skip();
										Skip();
										Match('a');
										Match('n');
										_value = double.NaN;
									} else if (la1 == 'i') {
										_type = num;
										Skip();
										Skip();
										Match('n');
										Match('f');
										_value = double.PositiveInfinity;
									} else
										goto match10;
								}
								break;
							case '0':
							case '1':
							case '2':
							case '3':
							case '4':
							case '5':
							case '6':
							case '7':
							case '8':
							case '9':
								goto match1;
							case '*':
								{
									_type = mul;
									Skip();
								}
								break;
							case '/':
								{
									_type = div;
									Skip();
								}
								break;
							case '+':
								{
									_type = add;
									Skip();
								}
								break;
							case '-':
								{
									_type = sub;
									Skip();
								}
								break;
							case ':':
								{
									_type = set;
									Skip();
									Match('=');
								}
								break;
							default:
								if (la0 >= 'A' && la0 <= 'Z' || la0 == '_' || la0 >= 'a' && la0 <= 'z') {
									_type = id;
									Id();
								} else
									goto match10;
								break;
							}
							break;
						match1:
							{
								_type = num;
								Num();
							}
							break;
						match10:
							{
								_type = EOF;
								la0 = LA0;
								if (la0 != -1) {
									Skip();
									_type = unknown;
								}
							}
						} while (false);
						return new Token { 
							Type = _type, Value = _value, StartIndex = _start
						};
					}
					static readonly HashSet<int> Id_set0 = NewSetOfRanges('0', '9', 'A', 'Z', '_', '_', 'a', 'z');
					void Id()
					{
						int la0;
						Skip();
						for (;;) {
							la0 = LA0;
							if (Id_set0.Contains(la0))
								Skip();
							else
								break;
						}
						_value = CharSource.Substring(_startIndex, InputPosition - _startIndex);
					}
					void Num()
					{
						int la0, la1;
						bool dot = false;
						la0 = LA0;
						if (la0 == '.') {
							Skip();
							dot = true;
						}
						MatchRange('0', '9');
						for (;;) {
							la0 = LA0;
							if (la0 >= '0' && la0 <= '9')
								Skip();
							else
								break;
						}
						la0 = LA0;
						if (la0 == '.') {
							if (!dot) {
								la1 = LA(1);
								if (la1 >= '0' && la1 <= '9') {
									Skip();
									Skip();
									for (;;) {
										la0 = LA0;
										if (la0 >= '0' && la0 <= '9')
											Skip();
										else
											break;
									}
								}
							}
						}
						_value = double.Parse(CharSource.Slice(_startIndex, InputPosition - _startIndex));
					}
				}
			}";
			Test(input, expectedOutput);
		}

		[Test]
		public void CalculatorRunnerLes()
		{
			string input = @"
			import Loyc.LLParserGenerator;
			public partial class Calculator(BaseParser!(Calculator.Token))
			{
				_vars::Dictionary!(string,double) = new(Dictionary!(string,double)());
				_tokens::List!Token = new List!Token();
				_input::string;
				
				public def Calculate(input::UString)::double
				{
					_input = input;
					_lexer = new Lexer(input);
					_tokens.Clear();
					t::Token;
					while ((t = lexer.NextToken()).Type != EOF)
						_tokens.Add(t);
					return Expr();
				};

				const EOF::int = -1;
				
				protected override def EofInt()::int { return EOF; };
				protected override def LA0Int()::int { return LT0.Type; };
				protected override def LT(i::int)::Token
				{
					if i < _tokens.Count { 
						return _tokens[i]; 
					} else { 
						return (new Token { Type = EOF });
					};
				};
				protected override def Error(li::int, message::string)
				{
					index::int = _input.Length;
					if InputPosition + li < _tokens.Count
						index = _tokens[InputPosition + li].StartIndex;
					Console.WriteLine(""Error at index {0}: {1}"", index, message);
				};
				protected override def ToString(int `#var` tokenType)::string
				{
					switch tokenType {
						case id; return ""identifier"";
						case num; return ""number"";
						case set; return "":="";
						default; return (tokenType->char).ToString();
					};
				};

				LLLPG
				{
					rule Atom::double @[
						{ result::double; }
						( t:=id           { result = _vars[t.Value -> Symbol]; }
						| t:=num          { result = t.Value -> double; } 
						| '-' result=Atom { result = -result; }
						| '(' result=Expr ')')
						{ return result; }
					];
					rule MulExpr @[ 
						result:=Atom
						(op:=(mul|div) rhs:=Atom { result = Do(result, op, rhs); })*
						{ return result; }
					];
					rule AddExpr @[
						result:=MulExpr
						(op:=(add|sub) rhs:=MulExpr { result = Do(result, op, rhs); })*
						{ return result; }
					];
					rule Expr @[
						{ result::double; }
						( t:=id set result=Expr { _vars[t.Value.ToString()] = result; }
						| result=AddExpr )
						{ return result; }
					];
				};

				def Do(left::double, op::Token, right::double)::double
				{
					switch op.Type {
						case add; return left + right;
						case sub; return left - right;
						case mul; return left * right;
						case div; return left / right;
					};
					return double.NaN; // unreachable
				};
			};";
			string expectedOutput = @"
			using Loyc.LLParserGenerator;
			public partial class Calculator : BaseParser<Calculator.Token>
			{
				Dictionary<string,double> _vars = new Dictionary<string,double>();
				List<Token> _tokens = new List<Token>();
				string _input;
				public double Calculate(UString input)
				{
					_input = input;
					_lexer = new Lexer(input);
					_tokens.Clear();
					Token t;
					while (((t = lexer.NextToken()).Type != EOF))
						_tokens.Add(t);
					return Expr();
				}
				const int EOF = -1;
				protected override int EofInt()
				{
					return EOF;
				}
				protected override int LA0Int()
				{
					return LT0.Type;
				}
				protected override Token LT(int i)
				{
					if (i < _tokens.Count) {
						return _tokens[i];
					} else {
						return new Token { 
							Type = EOF
						};
					}
				}
				protected override void Error(int li, string message)
				{
					int index = _input.Length;
					if (InputPosition + li < _tokens.Count)
						index = _tokens[InputPosition + li].StartIndex;
					Console.WriteLine(""Error at index {0}: {1}"", index, message);
				}
				protected override string ToString(int tokenType)
				{
					switch (tokenType) {
					case id: return ""identifier"";
					case num: return ""number"";
					case set: return "":="";
					default: return ((char)tokenType).ToString();
					}
				}
				double Atom()
				{
					int la0;
					double result;
					la0 = LA0;
					if (la0 == id) {
						var t = MatchAny();
						result = _vars[(Symbol) t.Value];
					} else if (la0 == num) {
						var t = MatchAny();
						result = (double) t.Value;
					} else if (la0 == '-') {
						Skip();
						result = Atom();
						result = -result;
					} else {
						Match('(');
						result = Expr();
						Match(')');
					}
					return result;
				}
				void MulExpr()
				{
					int la0;
					var result = Atom();
					for (;;) {
						la0 = LA0;
						if (la0 == div || la0 == mul) {
							var op = MatchAny();
							var rhs = Atom();
							result = Do(result, op, rhs);
						} else
							break;
					}
					return result;
				}
				void AddExpr()
				{
					int la0;
					var result = MulExpr();
					for (;;) {
						la0 = LA0;
						if (la0 == add || la0 == sub) {
							var op = MatchAny();
							var rhs = MulExpr();
							result = Do(result, op, rhs);
						} else
							break;
					}
					return result;
				}
				void Expr()
				{
					int la0, la1;
					double result;
					la0 = LA0;
					if (la0 == id) {
						la1 = LA(1);
						if (la1 == set) {
							var t = MatchAny();
							Skip();
							result = Expr();
							_vars[t.Value.ToString()] = result;
						} else
							result = AddExpr();
					} else
						result = AddExpr();
					return result;
				}
				double Do(double left, Token op, double right)
				{
					switch (op.Type) {
					case add: return left + right;
					case sub: return left - right;
					case mul: return left * right;
					case div: return left / right;
					}
					return double.NaN;
				}
			}";
			Test(input, expectedOutput);
		}

		#endregion
		
		#region Calculator example (EC#)
		
		//TODO

		#endregion
	}
}
