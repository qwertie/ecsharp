using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;

namespace Loyc.LLParserGenerator
{
	/// <summary>Tests for known slugs (slowness bugs) and fixed bugs (regressions)</summary>
	class LlpgBugsAndSlugs : LlpgGeneralTestsBase
	{
		[Test(Fails = "Haven't decided how to fix this yet")]
		public void Regression_2016_03_InappropriateSkip()
		{
			Test(@"LLLPG (parser(terminalType: Token, laType: TT, matchType: int, castLa: @false)) {
					rule X()::LNode @{
						D | TT.Tilde TT.Literal
					};
					@[private] rule D()::LNode @{
						tilde:=TT.Tilde
						(	n=(TT.Id|TT.ContextualKeyword)
						|	n=TT.This
						)
					};
				};", @"
				LNode X()
				{
					TT la1;
					la1 = LA(1);
					if (la1 == TT.ContextualKeyword || la1 == TT.Id || la1 == TT.This)
						D();
					else {
						Match((int) TT.Tilde);
						Match((int) TT.Literal);
					}
				}
				LNode D()
				{
					var tilde = Match((int) TT.Tilde);
					TODO; // bug: Skip() here, should be... if-else? MatchAny? not Skip anyway
				}
			");
		}

		[Test(Fails = "Unsure how to fix. Considering a major rewrite to make core engine easier to reason about.")]
		public void Regression_2016_01_AndPredBug()
		{
			Test(@"LLLPG (parser(terminalType: Token, laType: TT)) {
					token AndPredBug() @{
						[  t:TT.QuestionMark (&{flagA} | &{flagB})  ]?
					};
				}", @"
					void AndPredBug()
					{
						TT la0;
						Token t = default(Token);
						la0 = (TT) LA0;
						if (la0 == TT.QuestionMark) {
							if (flagA || flagB)
								t = MatchAny();
						}
					}");
			Test(@"LLLPG (parser) {
					token AndPredBug()
					@{
						[	t:=TT.QuestionMark (&{flagA} | &{flagB})
							{Action();}
						]*
					};
				}", @"
					void AndPredBug()
					{
						TokenType la0, la1;
						for (;;) {
							la0 = LA0;
							if (la0 == TT.QuestionMark) {
								la1 = LA(1);
								if (la1 == TT.QuestionMark) {
									if (flagA || flagB)
										goto match1;
									else
										break;
								} else {
									if (flagA || flagB)
										goto match1;
									else
										break;
								}
							} else
								break;
						match1:
							{
								var t = MatchAny();
								Action();
							}
						}
					}");
		}

		[Test] public void Regression_2012_02()
		{
			// 2014-2-05: Weird threading bug while adding --timeout option
			// The bug was that StageOneParser.ReclassifyTokens was called twice on 
			// the same tokens. Mysteriously, this caused parsing errors only when
			// StageOneParser ran on an a worker thread (i.e. for --timeout=i, i != 0)
			Test(@"LLLPG lexer { rule Foo @{ _ greedy('g')* _ }; }",
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
		}
		
		[Test] public void Regression_2013_12_LI_LA()
		{
			// 2013-12-01: Regression test: $LI and $LA were not replaced inside call targets or attributes
			Test(@"LLLPG parser { 
				rule Foo() @{ &!{LA($LI) == $LI} &{$LI() && Bar($LA())} &{@[Foo($LA)] $LI} _ };
			}", @"void Foo()
				{
					Check(!(LA(0) == 0), ""!(LA($LI) == $LI)"");
					Check(0() && Bar(((int)LA0)()), ""$LI() && Bar($LA())"");
					Check($LI, ""$LI"");
					MatchExcept();
				}");
		}

		[Test]
		public void Regression_2013_12_Misc()
		{
			// 2013-12-22: I really thought by now that I had found most of the
			// bugs, but this example exposed two separate bugs:
			// 1. {Money();} was dropped by Alts constructor, as the inner and
			//    outer Alts were merged and should not have been.
			// 2. GenerateExtraMatchingCode() didn't add "break;" before "match1:"
			Test(@"
			LLLPG lexer {
				rule Test @{
					({Money();} ('$' {Dollar();} | '#' {Pound();}))?
					'$'
				};
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
				rule Test @{
					(	'a'..'b' 'c'
					|	'b'..'c' 'a'
					)?	'$'
				};
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
		}

		[Test] public void Regression_2013_12_NullReferenceException()
		{
			// This grammar used to crash LLLPG with a NullReferenceException.
			// The output doesn't seem quite right; probably because of the left recursion.
			Test(@"@[FullLLk] LLLPG parser(laType(TT), matchType(int), allowSwitch(@true), castLA(@false)) {
				private rule Atom @{
					TT.Id (TT.LParen TT.RParen)?
				};
				token Expr @{
					greedy(
						Atom
					|	&{foo}
						greedy(Expr)+
					)*
				};
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

		[Test] public void SlugTest1()
		{
			// [2013-12-25]
			// It became very clear while writing the EC# grammar that large, 
			// complex, ambiguous grammars could make LLLPG run very slowly, but 
			// this was the first SMALL grammar I could find that would make LLLPG 
			// run very slowly (25 seconds to analyze 'Start' with k=2!).
			Test(@"@[DefaultK(2), FullLLk(false)]
			LLLPG lexer {
				rule PositiveDigit @{ '1'..'9' {""Think positive!""} };
				rule WeirdDigit @{ '0' | &{a} '1' | &{b} '2' | &{c} '3' 
				       | &{d} '4' | &{e} '5' | &{f} '6' | &{g} '7'
				       | &{h} '8' | &{i} '9' };
				rule Start @{ (WeirdDigit / PositiveDigit)* };
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
		}

		[Test] public void SlugTest2()
		{
			// This example is just a slight variation on the first, but it is
			// more difficult to fix.
			//
			// This takes over 15 seconds to analyze 'Start' with k=2, and
			// 8 minutes and 20 seconds (and almost 2 GB memory) for k=3!).
			//
			// Each branch of WeirdDigit that overlaps PositiveDigit and has an
			// &and-predicate increases CPU time and memory by a factor of four.
			// FullLLk(false) is required to cause the slug; with FullLLk(true),
			// processing time drops dramatically but the output is nearly seven
			// times larger.
			Test(@"@[DefaultK(2), FullLLk(false)] //@[Verbosity(3)]
			LLLPG lexer {
				rule PositiveDigit @{ '1'..'9' {""Think positive!""} };
				rule WeirdDigit @{ '0' | &{a} '1' | &{b} '2' | &{c} '3' 
				       | &{d} '4' | &{e} '5' | &{f} '6' | &{g} '7'
				       | &{h} '8' | &{i} '9' };
				rule Start @{ WeirdDigit+ / PositiveDigit+ };
			};",
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
								goto matchWeirdDigit;
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
							goto matchWeirdDigit;
						break;
					matchWeirdDigit:
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
	}
}
