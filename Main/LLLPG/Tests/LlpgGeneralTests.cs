using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using Loyc.MiniTest;
using Loyc;
using Loyc.Syntax;
using Loyc.Utilities;
using Loyc.Collections;
using Loyc.Syntax.Les;
using Loyc.Ecs;

namespace Loyc.LLParserGenerator
{
	using S = CodeSymbols;

	/// <summary>Tests LLLPG with the whole <see cref="LeMP.MacroProcessor"/> pipeline.</summary>
	/// <remarks>All input examples are written in LES.</remarks>
	[TestFixture]
	class LlpgGeneralTests : LlpgGeneralTestsBase
	{
		[Test]
		public void SimpleMatching()
		{
			Test(@"
			LLLPG lexer {
				@[pub] rule Foo @{ 'x' '0'..'9' '0'..'9' };
			}", @"
				public void Foo()
				{
					Match('x');
					MatchRange('0', '9');
					MatchRange('0', '9');
				}");
		}

		[Test]
		public void SimpleMatchingAntlrStyle()
		{
			Test(@"
			LLLPG lexer @{
				[public] Foo : 'x' '0'..'9' '0'..'9';
			};", @"
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
				@[pub] rule a @{ 'A'|'a' };
				@[pub] rule b @{ 'B'|'b' };
				@[pub] rule Foo @{ (a | b? 'c')* };
			}", @"
			LLLPG(lexer) {
				// Verify that three different rule syntaxes all work
				public rule a @{ 'A'|'a' };
				public rule b() @{ 'B'|'b' };
				public rule void Foo() @{ (a | b? 'c')* };
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
				@[pub] rule Foo @{ [A]? [B]* };
			}", @"
			LLLPG(parser) {
				public rule void Foo @{ [A]? [B]* };
			}", @"
				public void Foo()
				{
					int la0;
					la0 = (int)LA0;
					if (la0 == A)
						Skip();
					for (;;) {
						la0 = (int)LA0;
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
				@[pub] rule Except @{ ~'a' ~('a'..'z') };
				@[pub] rule String @{ '""' ~('""'|'\n')* '""' };
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
				@[pub] rule DisOrDat @{ ~('a'..'z'|'A'..'Z'|'0'..'9'|'_') | {Lowercase();} 'a'..'z' };
				@[pub] rule Dis      @{ ~('a'..'z'|'A'..'Z'|'0'..'9'|'_') };
			}", @"LLLPG (lexer) {
				public rule DisOrDat @{ ~('a'..'z'|'A'..'Z'|'0'..'9'|'_') | {Lowercase();} 'a'..'z' };
				public rule Dis      @{ ~('a'..'z'|'A'..'Z'|'0'..'9'|'_') };
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
				@[pub] rule DisOrDat @{ ~('a'|'b'|'c'|'d'|1|2|3|4) | 'a' 1 };
				@[pub] rule Dis      @{ ~('a'|'b'|'c'|'d'|1|2|3|4) };
			}", @"
				static readonly HashSet<int> DisOrDat_set0 = NewSet(1, 2, 3, 4, 'a', 'b', 'c', 'd', EOF);
				public void DisOrDat()
				{
					int la0;
					la0 = (int)LA0;
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
			DualLanguageTest(@"
			@[FullLLk] LLLPG (lexer) {
				@[public] rule FullLL2 @{ ('a' 'b' | 'b' 'a') 'c' | ('a' 'a' | 'b' 'b') 'c' };
			};", @"
			[FullLLk] LLLPG (lexer) {
				public rule FullLL2 @{ ('a' 'b' | 'b' 'a') 'c' | ('a' 'a' | 'b' 'b') 'c' };
			}", @"
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
		public void SimplePrematchAnalysisTest()
		{
			DualLanguageTest(@"
			@[PrematchByDefault] LLLPG lexer {
				@[protected] rule a @{ 'A'|'a' };
				@[internal] rule b  @{ 'B'|'b' };
				@[public] rule c    @{ 'C'|'c' };
				rule d              @{ 'd' };
				private rule D      @{ 'D' };
				@[public] rule Foo  @{ a / b / c / d / D / _ };
			}", @"
			[PrematchByDefault] LLLPG(lexer) {
				protected rule a @{ 'A'|'a' };
				internal rule b  @{ 'B'|'b' };
				public rule c    @{ 'C'|'c' };
				rule d           @{ 'd' };
				private rule D   @{ 'D' };
				public rule Foo() @{ a / b / c / d / D / _ };
			}", @"
				protected void a()
				{
					Match('A', 'a');
				}
				internal void b()
				{
					Match('B', 'b');
				}
				public void c()
				{
					Match('C', 'c');
				}
				void d()
				{
					Skip();
				}
				private void D()
				{
					Skip();
				}
				public void Foo()
				{
					switch (LA0) {
					case 'A': case 'a':
						a();
						break;
					case 'B': case 'b':
						b();
						break;
					case 'C': case 'c':
						c();
						break;
					case 'd':
						d();
						break;
					case 'D':
						D();
						break;
					default:
						MatchExcept();
						break;
					}
				}");
		}

		[Test]
		public void SemPredUsingLI()
		{
			// You can write a semantic predicate using the replacement "$LI" which
			// will insert the index of the current lookahead, or "$LA" which 
			// inserts a variable that holds the actual lookahead symbol. Test this 
			// feature with two different lookahead amounts for the same predicate.
			Test(@"LLLPG lexer {
				@[pub] rule Id() @{ &{@[Hoist] char.IsLetter($LA)} _ (&{@[Hoist] char.IsLetter($LA) || char.IsDigit($LA)} _)* };
				@[pub] rule Twin() @{ 'T' &{@[Hoist] $LA == LA($LI+1)} '0'..'9' '0'..'9' };
				@[pub] token Token() @{ Twin / Id };
			}", @"
				public void Id()
				{
					int la0;
					Check(char.IsLetter(LA0), ""Expected @char.IsLetter($LA)"");
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
					Check(LA0 == LA(0 + 1), ""Expected $LA == LA($LI + 1)"");
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
		public void SemPredCustomCheckMessageOrNoCheck()
		{
			Test(@"LLLPG lexer {
				@[pub] rule Int()  @{ &{@[Hoist, NoCheck] NumbersAllowed} '0'..'9'+ };
				@[pub] rule Twin() @{ &{@[Hoist, ""Must be saturday""] IsSaturday} '0'..'9' '0'..'9' };
				@[pub] token Token() @{ Twin / Int };
			}", @"
				public void Int()
				{
					int la0;
					MatchRange('0', '9');
					for (;;) {
						la0 = LA0;
						if (la0 >= '0' && la0 <= '9')
							Skip();
						else
							break;
					}
				}
				public void Twin()
				{
					Check(IsSaturday, ""Must be saturday"");
					MatchRange('0', '9');
					MatchRange('0', '9');
				}
				public void Token()
				{
					int la1;
					if (IsSaturday) {
						if (NumbersAllowed) {
							la1 = LA(1);
							if (la1 >= '0' && la1 <= '9')
								Twin();
							else
								Int();
						} else
							Twin();
					} else
						Int();
				}");
		}

		[Test]
		public void DifferentDefault3()
		{
			string input = @"LLLPG (lexer)
				{
					rule VowelOrNot() @{ 
						('A'|'E'|'I'|'O'|'U') {Vowel();} / 'A'..'Z' {Consonant();}
					};
					rule ConsonantOrNot @{
						default ('A'|'E'|'I'|'O'|'U') {Other();} / 'A'..'Z' {Consonant();}
					};
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
				public rule Stmt @{ @@Number (@@print @@DQString | @@goto @@Number) @@Newline };
				public rule Stmts @{ Stmt* };
			}", @"
			LLLPG(parser(laType(Symbol), allowSwitch(false))) {
				public rule Stmt @{ @@Number (@@print @@DQString | @@goto @@Number) @@Newline };
				public rule Stmts @{ Stmt* };
			}", @"
				public void Stmt()
				{
					Symbol la0;
					Match(@@Number);
					la0 = (Symbol)LA0;
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
						la0 = (Symbol)LA0;
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
					@{ 'x' };
					@{ { Blah2; Blah3; } 'y' };
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
					{var x=0;} (&{x < max}               ~('\n'|'\r'|' ')+  {x++;})?
					greedy     (&{x < max} greedy(' ')+ (~('\n'|'\r'|' '))* {x++;})*
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
						Check(x < max, ""Expected x < max"");
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
							Check(x < max, ""Expected x < max"");
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
				rule Digit(x::int) @{ '0'..'9' };
				@[recognizer { @[prot] def IsOddDigit(y::float); }]
				rule OddDigit(x::int) @{ '1'|'3'|'5'|'7'|'9' };
				rule NonDigit @{ &!Digit(7) _ };
				rule EvenDigit @{ &!OddDigit _ };
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
					Check(!Try_Scan_Digit(0, 7), ""Did not expect Digit"");
					MatchExcept();
				}
				void EvenDigit()
				{
					Check(!Try_IsOddDigit(0), ""Did not expect OddDigit"");
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
					Check(Try_Number_Test0(0), ""Expected [.0-9]"");
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
				@[extern] rule DecDigits() @{ '0'..'9'+ };
				rule HexDigit()  @{ '0'..'9' | 'a'..'f' | 'A'..'F' };
				rule HexDigits() @{ { Hex(); } HexDigit+ ('_' HexDigit+)* };
				token HexNumber() @{
					'0' ('x'|'X')
					HexDigits?
					// Avoid ambiguity with 0x5.Equals(): a dot is not enough
					(	'.' &(HexDigits ('p'|'P') ('+'|'-'|'0'..'9')) HexDigits )?
					( ('p'|'P') ('+'|'-')? DecDigits )?
				};
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
				@[private] rule HexDigit()  @{ '0'..'9' | 'a'..'f' | 'A'..'F' };
				@[private] rule HexDigits() @{ HexDigit+ };
				@[private] rule HexNumber() @{
					'0' ('x'|'X')
					HexDigits?
					// Avoid ambiguity with 0x5.Equals(): a dot is not enough
					(	'.' &( '0'..'9' / HexDigits ('p'|'P') ('+'|'-'|'0'..'9') ) 
						HexDigits )?
					//( ('p'|'P') ('+'|'-')? '0'..'9'+ )?
				};
			}",
			@"static readonly HashSet<int> HexDigit_set0 = NewSetOfRanges('0', '9', 'A', 'F', 'a', 'f');
			private void HexDigit()
			{
				Match(HexDigit_set0);
			}
			private bool Scan_HexDigit()
			{
				if (!TryMatch(HexDigit_set0))
					return false;
				return true;
			}
			private void HexDigits()
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
			private bool Scan_HexDigits()
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
			private void HexNumber()
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
					Check(Try_HexNumber_Test0(0), ""Expected ([0-9] / HexDigits [Pp] [+\\-0-9])"");
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
			Test(@"@[DefaultK(3)] 
			LLLPG lexer {
				@[extern] token SQString @{
					'\'' ('\\' _ {_parseNeeded = true;} | ~('\''|'\\'|'\r'|'\n'))* '\''
				};
				@[k(4), extern] token TQString @{
					""'''"" nongreedy(_)* ""'''""
				};
				token Token0 @{ // No error branch
					( TQString
					/ SQString 
					| ' '
					)};
				token Token1 @{
					( TQString 
					/ SQString
					| ' '
					| error { Error(); }
					  ( EOF | _ )
					)};
				token Token2 @{
					( SQString
					| error { Error(); }
					  ( EOF | _ )
					)};
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
								goto error;
						} else if (la0 == ' ')
							Skip();
						else
							goto error;
						break;
					error:
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

		/* I haven't figured out how to modify LLLPG to make this work the way I want it to.
		[Test]
		public void AndPredOrError()
		{
			Test(@"
			LLLPG parser {
				public rule ConditionalDot @[ 
					( &{cond} 
					| error {Error(""Unexpected Dot.""); return;} )
					'.'
				];
			}", @"
				public void ConditionalDot()
				{
					int la0;
					do {
						la0 = LA0;
						if (la0 == '.')
							if (cond)
								break;
							else
								goto match2;
						else
							goto match2;
					match2: {
							Error(""Unexpected Dot."");
							return;
						}
					} while (false);
					Match('.');
				}");
		}*/

		[Test]
		public void KeywordTrieTest()
		{
			// By the way, it's more efficient to use a gate for this: 
			// (Not_IdContChar=>) instead of &Not_IdContChar. But LLLPG
			// had trouble with this version, so that's what I'm testing.
			Test(@"
			LLLPG lexer {
				@[extern] token Id @{
					('a'..'z'|'A'..'Z'|'_') ('a'..'z'|'A'..'Z'|'0'..'9'|'_')*
				};
				token Not_IdContChar @{
					~('a'..'z'|'A'..'Z'|'0'..'9'|'_'|'#') | EOF
				};
				@[k(12)]
				token IdOrKeyword @
					{ ""case""  &Not_IdContChar
					/ ""catch"" &Not_IdContChar
					/ ""char""  &Not_IdContChar
					/ Id };
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
			Test(@"LLLPG parser(CastLA(@false)) {
				@[recognizer(def RecognizeFoo(z::int))]
				rule Foo(x::int, y::int) @{ 'F''u' };
				rule Foo2 @{ Foo(2, -2) '2' };
				rule Main @{ Foo2 (&Foo2 _)? };
			};", @"
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
					if (la0 != (int) EOF) {
						Check(Try_Scan_Foo2(0), ""Expected Foo2"");
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
				rule QuoteBang @{ Bang | Quote QuoteBang Quote };
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
			Test(@"LLLPG parser(CastLA(@false)) {
				alias('\'' = TokenType.Quote);
				alias(Bang = TokenType.ExclamationMark);
				rule QuoteBang @{ Bang | '\'' QuoteBang '\'' };
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
			Test(@"LLLPG parser(CastLA(@false)) {
				@[extern] rule Atom() @{ Id | Number | '(' Expr ')' };
				rule Expr() @{ Atom (('+'|'-'|'*'|'/') Atom | error { Error(); })* };
				rule Start() @{ Expr EOF };
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
			Test(@"LLLPG parser(laType(TokenType), castLA(@false), matchType(int), allowSwitch(@false)) {
				@[extern] rule Atom() @{ Id | Number | '(' Expr ')' };
				rule Expr() @{ Atom (('+'|'-'|'*'|'/'|'%'|'^') Atom)* };
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
				rule BC @[ &{@[Local] $LA + 1 == LA($LI+1)} 'a'..'y' 'b'..'z' ];
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
					Check(LA0 + 1 == LA(0 + 1), ""Expected $LA + 1 == LA($LI + 1)"");
					MatchRange('a', 'y');
					MatchRange('b', 'z');
				}");

			// The [Local] flag also prevents hoisting to the SAME rule, which is important
			// for the technique (used by LES and EC# parsers) of collapsing many precedence
			// levels into a single rule.
			Test(@"LLLPG parser(laType(string), castLA(@false)) {
				rule Expr(context::Precedence)::object @[
					{prec::Precedence;}
					result:=(Id|Number)
					greedy(
						// Infix operator with order-of-operations detection
						&{@[Local] context.CanParse(prec = GetInfixPrecedence(LA($LI)))}
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
			Test(@"LLLPG parser(laType(string), castLA(@false), allowSwitch(@false), setType(HashSet!string))
				{
					@[extern] rule IdAtom @{
						(TT.Id|TT.TypeKeyword)
					};
					@[extern] rule TParams() @{ // type parameters
						( ""<"" (IdAtom ("","" IdAtom)*)? "">"" )
					};
		
					@[private] rule PrimaryExpr @{
						TT.Id
						greedy(
							&(TParams (~TT.Id | EOF)) ""<"" greedy(_)* => TParams()
						)*
					};

					// An intermediate rule was needed to trigger the bug
					@[private] rule PrefixExpr @{ PrimaryExpr };

					@[private] rule Expr() @{
						e:=PrefixExpr
						greedy
						(	// Infix operator
							( ""*""|""/""|""+""|""-""|""<""|"">""|""<=,>=""|""==,!="" )
							Expr()
						)*
					};
				}",
				@"private void PrimaryExpr()
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
				private void PrefixExpr()
				{
					PrimaryExpr();
				}
				static readonly HashSet<string> Expr_set0 = NewSet(""-"", ""*"", ""/"", ""+"", ""<"", ""<=,>="", ""==,!="", "">"");
				private void Expr()
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
		public void ChangedInputSource()
		{
			Test(@"
			LLLPG lexer(inputSource(b)) {
				@[pub] rule Foo(b::Bar) @{ 'x' '0'..'9' '0'..'9' };
			}", @"
				public void Foo(Bar b)
				{
					b.Match('x');
					b.MatchRange('0', '9');
					b.MatchRange('0', '9');
				}");
		}

		[Test]
		public void ChangedEofClass()
		{
			Test(@"@[NoDefaultArm] 
			LLLPG (parser(inputSource(inp), inputClass(ParserClass))) {
				rule AllBs @[ 'B'* ];
			};", @"
				void AllBs()
				{
					int la0;
					for (;;) {
						la0 = (int)inp.LA0;
						if (la0 == 'B')
							inp.Skip();
						else if (la0 == (int) ParserClass.EOF)
							break;
						else
							inp.Error(0, ""In rule 'AllBs', expected one of: ('B'|EOF)"");
					}
				}");
		}

		[Test]
		public void TestInlining()
		{
			// This inlining example demonstrates:
			// - Inlining isn't fully operational yet - The Alts in Letter are
			//   not merged with the Alts in IdStartChar or in IdContChar.
			// - 'extern' suppresses generation of IdStartChar and IdContChar
			// - order matters: inlining is applied only once, from top to bottom.
			//   so Letter is inlined into IdStartChar BEFORE IdStartChar is inlined
			//   into Id. Thus Id has an inlined copy of Letter as referenced by
			//   IdStartChar, but later makes normal method call to Letter() when
			//   it inlines IdContChar.
			// - (side node) and-preds still aren't handled the way I'd like
			Test(@"LLLPG (lexer) {
					@[inline] rule Letter         @{ 'a'..'z' | 'A'..'Z' | _x:128..65534 &{_x > 128 && char.IsLetter(_x)} };
					@[extern] rule IdStartChar    @{ Letter | '_' };
					@[public] token Id            @{ inline:IdStartChar inline:IdContChar+ };
					@[inline, extern] rule IdContChar @{ '0'..'9' | '_' | Letter };
				};", @"
					void Letter()
					{
						int la0;
						int _x = 0;
						// Line 2: ([A-Za-z] | (128..65534) &{_x > 128 && @char.IsLetter(_x)})
						la0 = LA0;
						if (la0 >= 'A' && la0 <= 'Z' || la0 >= 'a' && la0 <= 'z')
							Skip();
						else {
							_x = MatchRange(128, 65534);
							Check(_x > 128 && char.IsLetter(_x), ""Expected _x > 128 && @char.IsLetter(_x)"");
						}
					}
					static readonly HashSet<int> Id_set0 = NewSetOfRanges('A', 'Z', 'a', 'z', 128, 65534);
					static readonly HashSet<int> Id_set1 = NewSetOfRanges('0', '9', 'A', 'Z', '_', '_', 'a', 'z', 128, 65534);
					public void Id()
					{
						int la0;
						// Line 3: (([A-Za-z] | (128..65534) &{_x > 128 && @char.IsLetter(_x)}) | [_])
						la0 = LA0;
						if (Id_set0.Contains(la0)) {
							int _x = 0;
							// Line 2: ([A-Za-z] | (128..65534) &{_x > 128 && @char.IsLetter(_x)})
							la0 = LA0;
							if (la0 >= 'A' && la0 <= 'Z' || la0 >= 'a' && la0 <= 'z')
								Skip();
							else {
								_x = MatchRange(128, 65534);
								Check(_x > 128 && char.IsLetter(_x), ""Expected _x > 128 && @char.IsLetter(_x)"");
							}
						} else
							Match('_');
						// Line 5: ([0-9_] | Letter)
						la0 = LA0;
						if (la0 >= '0' && la0 <= '9' || la0 == '_')
							Skip();
						else
							Letter();
						// Line 4: (([0-9_] | Letter))*
						for (;;) {
							la0 = LA0;
							if (Id_set1.Contains(la0)) {
								// Line 5: ([0-9_] | Letter)
								la0 = LA0;
								if (la0 >= '0' && la0 <= '9' || la0 == '_')
									Skip();
								else
									Letter();
							} else
								break;
						}
					}");
		}

		[Test]
		public void TestAnyIn()
		{
			Test(@"LLLPG (lexer()) {
					rule Words @{ (any fruit ' ')* };
					@[#fruit] rule A @{ ""apple"" };
					@[#fruit] rule G @{ ""grape"" };
					@[#fruit] rule L @{ ""lemon"" };
				}", @"
					void Words()
					{
						int la0;
						// Line 2: (( A | G | L ) [ ])*
						 for (;;) {
							la0 = LA0;
							if (la0 == 'a' || la0 == 'g' || la0 == 'l') {
								// Line 0: ( A | G | L )
								la0 = LA0;
								if (la0 == 'a')
									A();
								else if (la0 == 'g')
									G();
								else
									L();
								Match(' ');
							} else
								break;
						}
					}
					void A()
					{
						Match('a'); Match('p'); Match('p'); Match('l'); Match('e');
					}
					void G()
					{
						Match('g'); Match('r'); Match('a'); Match('p'); Match('e');
					}
					void L()
					{
						Match('l'); Match('e'); Match('m'); Match('o'); Match('n');
					}
				");
			Test(@"LLLPG (lexer()) {
					rule SumWords::int @[ {var x=0;} (any Literal in (x+=Literal) ' ')* {return x;} ];
					@[Literal] rule One::int @[ ""one"" {return 1;}  ];
					@[Literal] rule Two::int @[ ""two"" {return 2;}  ];
					@[Literal] rule Ten::int @[ ""ten"" {return 10;} ];
				}", @"
					int SumWords()
					{
						int la0, la1;
						var x = 0;
						for (;;) {
							la0 = LA0;
							if (la0 == 'o') {
								x.Add(One());
								Match(' ');
							} else if (la0 == 't') {
								la1 = LA(1);
								if (la1 == 'w') {
									x.Add(Two());
									Match(' ');
								} else {
									x.Add(Ten());
									Match(' ');
								}
							} else
								break;
						}
						return x;
					}
					[Literal] int One()
					{
						Match('o');
						Match('n');
						Match('e');
						return 1;
					}
					[Literal] int Two()
					{
						Match('t');
						Match('w');
						Match('o');
						return 2;
					}
					[Literal] int Ten()
					{
						Match('t');
						Match('e');
						Match('n');
						return 10;
					}
				");
		}

		[Test]
		public void TestAnyToken()
		{
			Test(@"
				LLLPG(lexer);
	
				rule NextToken @{
					any token
				};
				token L() @{'a'..'z'};
				token void N() @{'0'..'9'};
			", @"
				void NextToken()
				{
					int la0;
					la0 = LA0;
					if (la0 >= 'a' && la0 <= 'z')
						L();
					else
						N();
				}
				void L()
				{
					MatchRange('a', 'z');
				}
				void N()
				{
					MatchRange('0', '9');
				}
			", null, EcsLanguageService.Value);

			Test(@"
				LLLPG(lexer);
	
				rule int NextToken @{
					any token in result:token
				};
				token int L() @{result:'a'..'z'};
				token int N() @{result:'0'..'9'};
			", @"
				int NextToken()
				{
					int la0;
					int result = 0;
					la0 = LA0;
					if (la0 >= 'a' && la0 <= 'z')
						result = L();
					else
						result = N();
					return result;
				}
				int L()
				{
					int result = 0;
					result = MatchRange('a', 'z');
					return result;
				}
				int N()
				{
					int result = 0;
					result = MatchRange('0', '9');
					return result;
				}
			", null, EcsLanguageService.Value);
		}

		[Test]
		public void TestResultVariable()
		{
			Test(@"LLLPG (lexer()) {
					rule DigitList::List!int @[ {$result = `new` List!int();} result+:'0'..'9' ];
				}", @"
					List<int> DigitList()
					{
						List<int> result = default(List<int>);
						result = new List<int>();
						result.Add(MatchRange('0', '9'));
						return result;
					}");

			Test(@"
				LLLPG(lexer(inputSource(src), inputClass(LexerSource))) {
					static rule int ParseInt(string input) {
						var src = (LexerSource)input;
						@[ (d:='0'..'9' {$result = $result * 10 + (d - '0');})+ ];
					}
				}", @"
				static int ParseInt(string input)
				{
					int la0;
					int result = 0;
					var src = (LexerSource)input;
					var d = src.MatchRange('0', '9');
					result = result * 10 + (d - '0');
					for (;;) {
						la0 = src.LA0;
						if (la0 >= '0' && la0 <= '9') {
							var d = src.MatchAny();
							result = result * 10 + (d - '0');
						} else
							break;
					}
					return result;
				}", null, EcsLanguageService.Value);

			Test(@"LLLPG (lexer()) {
					rule Digit::int @{ result:'0'..'9' };
				}", @"
					int Digit()
					{
						int result = 0;
						result = MatchRange('0', '9');
						return result;
					}");
		}

		[Test]
		public void TestImplicitLllpgBlock()
		{
			Test(@" {
					Before::int;
					LLLPG lexer;
					public rule Foo @[ 'x' ];
				}", @"{
					int Before;
					public void Foo()
					{
						Match('x');
					}
				}");
			DualLanguageTest(@"
				LLLPG parser(laType: Symbol, allowSwitch: @false);
				public rule Number @[ @@Number ];
				public rule Numbers @[ Number* ];
			", @"
				LLLPG (parser(laType: Symbol, allowSwitch: false));
				public rule Number @[ @@Number ];
				public rule Numbers @[ Number* ];
			", @"
				public void Number()
				{
					Match(@@Number);
				}
				public void Numbers()
				{
					Symbol la0;
					for (;;) {
						la0 = (Symbol)LA0;
						if (la0 == @@Number)
							Number();
						else
							break;
					}
				}
			");
		}

		[Test]
		public void SimpleParserSourceExample()
		{
			// How to use ParserSource
			Test(@"{
					using TT = MyTokenType;
					enum MyTokenType { EOF = 0, Id = 1, Etc }
					ParserSource<Token> _ps;
					
					LLLPG (parser(laType(TT), matchCast(int), inputSource(_ps), inputClass(ParserSource<Token>)));
					public rule IdList @[ list+=TT.Id (TT.Comma list+=TT.Id)* ];
				}", @"{
					using TT = MyTokenType;
					enum MyTokenType { EOF = 0, Id = 1, Etc }
					ParserSource<Token> _ps;
					
					public void IdList() {
						TT la0;
						list.Add(_ps.Match((int)TT.Id));
						for(;;) {
							la0 = (TT)_ps.LA0;
							if (la0 == TT.Comma) {
								_ps.Skip();
								list.Add(_ps.Match((int)TT.Id));
							} else
								break;
						}
					}
				}", null, EcsLanguageService.Value);
		}

		[Test]
		public void AntlrStyle()
		{
			Test(@"
				LLLPG (lexer(inputSource: src, inputClass: LexerSource)) @{
					public static ParseInt[string input] returns [int result] :
						{var src = (LexerSource)input;}
						( d:='0'..'9' {$result = $result * 10 + (d - '0');} )+;
					// You can also use BNF-style ::=
					[LL(3), AnotherAttribute()]
					public static ParseVoid(string input) returns (void) ::=
						{var src = (LexerSource)input;}
						""void"";
					public static ParseVoid2[string input] @init { BeforeParse(); } ::= ParseVoid[input];
				};", @"
				public static int ParseInt(string input)
				{
					int la0;
					int result = 0;
					var src = (LexerSource)input;
					var d = src.MatchRange('0', '9');
					result = result * 10 + (d - '0');
					for (;;) {
						la0 = src.LA0;
						if (la0 >= '0' && la0 <= '9') {
							var d = src.MatchAny();
							result = result * 10 + (d - '0');
						} else
							break;
					}
					return result;
				}
				[AnotherAttribute()]
				public static void ParseVoid(string input)
				{
					var src = (LexerSource)input;
					src.Match('v');
					src.Match('o');
					src.Match('i');
					src.Match('d');
				}
				public static void ParseVoid2(string input)
				{
					BeforeParse();
					ParseVoid(input);
				}", null, EcsLanguageService.Value);

		}

		[Test]
		public void AntlrStyleWithHostCode()
		{
			Test(@"
			LLLPG (parser(laType: TT)) @{
				alias('(' = TT.LParen);
				alias(')' = TT.RParen);
				@members { Statement1(); Statement2(); }
				
				{int _depth = 0;}
				public ScanParens returns [int] : 
				[ '(' {_depth++;} 
				| ')' {_depth--;} 
				| ~('('|')')
				]*
				{return _depth;};
			};", @"
				Statement1();
				Statement2();
				int _depth = 0;
				public int ScanParens()
				{
					TT la0;
					for (;;) {
						la0 = (TT) LA0;
						if (la0 == TT.LParen) {
							Skip();
							_depth++;
						} else if (la0 == TT.RParen) {
							Skip();
							_depth--;
						// Strange, I thought we got rid of such redundant checks?
						} else if (!(la0 == (TT) EOF || la0 == TT.LParen || la0 == TT.RParen))
							Skip();
						else
							break;
					}
					return _depth;
				}", 
				null, EcsLanguageService.Value);
		}

		[Test]
		public void TestListInitializer()
		{
			Test(@"
				LLLPG (lexer(terminalType: int, listInitializer: var _ = new VList<T>()));
				public rule int ParseInt() @[
					' '* (digits+:'0'..'9')+
					{return digits.Aggregate(0, (n, d) => n * 10 + (d - '0'));}
				];", @"
					public int ParseInt()
					{
						int la0;
						var digits = new VList<int>();
						for (;;) {
							la0 = LA0;
							if (la0 == ' ')
								Skip();
							else
								break;
						}
						digits.Add(MatchRange('0', '9'));
						for (;;) {
							la0 = LA0;
							if (la0 >= '0' && la0 <= '9')
								digits.Add(MatchAny());
							else
								break;
						}
						return digits.Aggregate(0, (n, d) => n * 10 + (d - '0'));
					}", null, EcsLanguageService.Value);
		}

		[Test]
		public void TestDefaultOnOnlyArmOfLoop()
		{
			Test(@"
				LLLPG (parser(laType: TT, terminalType: Token));
				public rule LNode Expr() @{ Id { return F.Id($Id); } };
				public rule void ExprList() @{
					[default e:Expr]?
					[	default 
						(	end:(Comma|Semicolon)
						/	error {MissingEndMarker(e);}
						)
						({$e = null;} / e:Expr)
					]*
				};
				public rule void Tuple() @{
					LParen ExprList RParen
				}",
			@"
				public LNode Expr()
				{
					Token tok_Id = default(Token);
					tok_Id = Match(Id);
					return F.Id(tok_Id);
				}
				public void ExprList()
				{
					TT la0;
					LNode e = default(LNode);
					Token end = default(Token);
					// FIXME: default causes some redundant code to be generated (case Id).
					do {
						switch ((TT) LA0) {
						case Id:
							e = Expr();
							break;
						case Comma: case EOF: case RParen: case Semicolon:
							; break;
						default:
							e = Expr();
							break;
						}
					} while (false);
					// FIXME: default causes some redundant code to be generated (case Comma/Semicolon).
					for (;;) {
						switch ((TT) LA0) {
						case Comma: case Semicolon:
							goto match1;
						case EOF: case RParen:
							goto stop;
						default:
							goto match1;
						}
					match1:
						{
							// Line 7: ((Comma|Semicolon))
							la0 = (TT) LA0;
							if (la0 == Comma || la0 == Semicolon)
								end = MatchAny();
							else {
								MissingEndMarker(e);
							}
							switch ((TT) LA0) {
							case Comma: case EOF: case RParen: case Semicolon:
								e = null;
								break;
							default:
								e = Expr();
								break;
							}
						}
					}
				stop:;
				}
				public void Tuple()
				{
					Match(LParen);
					ExprList();
					Match(RParen);
				}
				", null, EcsLanguageService.Value);
		}
	}
}
