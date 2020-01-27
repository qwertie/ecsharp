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

		[Test]
		public void Bug_2017_08_UnexpectedWarning()
		{
			// Spurious "It's poor style to put a code block {..} before an and-predicate"
			// warning was caused by a synthetic ActionPred inserted by AutoValueSaverVisitor
			Test(@"
				LLLPG parser(terminalType:Token) {
					@[private] rule Rule()::Token @{ &{cond} result:1234 };
				}",
				@"private Token Rule()
				{
					Token result = default(Token);
					Check(cond, ""Expected cond"");
					result = Match(1234);
					return result;
				}", new SeverityMessageFilter(ConsoleMessageSink.Value, Severity.Warning));
		}

		[Test(Fails = "TODO: figure out how to implement this properly")]
		public void Bug_2017_01_CannotSharePrematchData()
		{
			// This test checks that the recognizer version of a rule doesn't
			// use the same prematch information as the main rule (if it does,
			// Scan_XAbc() will incorrectly begin with Skip()).
			Test(@"
				LLLPG parser(LAType: char, AllowSwitch: @true, SetType: HashSet!int)
				{
					rule Start @{ XAbc / 'x' &XAbc ('a'|'x')* / 'y' };
					@[private] rule XAbc  @{ 'x' ('a'|'b'|'c')* };
				};", 
			@"
				void Start()
				{
					char la0;
					la0 = (char) LA0;
					if (la0 == 'x') {
						switch ((char) LA(1)) {
						case 'a': case 'b': case 'c': case EOF:
							XAbc();
							break;
						default:
							{
								Skip();
								Check(Try_Scan_XAbc(0), ""Expected XAbc"");
								for (;;) {
									la0 = (char) LA0;
									if (la0 == 'a' || la0 == 'x')
										Skip();
									else
										break;
								}
							}
							break;
						}
					} else
						Match('y');
				}

				private void XAbc()
				{
					char la0;
					Skip();
					for (;;) {
						la0 = (char) LA0;
						if (la0 == 'a' || la0 == 'b' || la0 == 'c')
							Skip();
						else
							break;
					}
				}

				private bool Try_Scan_XAbc(int lookaheadAmt) {
					using (new SavePosition(this, lookaheadAmt))
						return Scan_XAbc();
				}
				private bool Scan_XAbc()
				{
					char la0;
					if (!TryMatch('x'))
						return false;
					for (;;) {
						la0 = (char) LA0;
						if (la0 == 'a' || la0 == 'b' || la0 == 'c')
							Skip();
						else
							break;
					}
					return true;
				}");
		}

		[Test]
		public void Bug_2017_01_ErrorBranchCausesIncorrectRecognizer()
		{
			Test(@"
				LLLPG lexer {
					rule Digit(x::int) @{ &OddDigit '0'..'9' };
					rule OddDigit(x::int) @{ '1'|'3'|'5'|'7'|'9' | '-' '1'| error {Error();} };
				};",
			@"
				void Digit(int x)
				{
					Check(Try_Scan_OddDigit(0), ""Expected OddDigit"");
					MatchRange('0', '9');
				}

				void OddDigit(int x)
				{
					switch (LA0) {
					case '1': case '3': case '5': case '7':
					case '9':
						Skip();
						break;
					case '-':
						{Skip();
						Match('1');}
						break;
					default:
						{Error();}
						break;
					}
				}

				bool Try_Scan_OddDigit(int lookaheadAmt, int x) {
					using (new SavePosition(this, lookaheadAmt))
						return Scan_OddDigit(x);
				}
				bool Scan_OddDigit(int x)
				{
					switch (LA0) {
					case '1': case '3': case '5': case '7':
					case '9':
						Skip();
						break;
					case '-':
						{
							Skip();
							if (!TryMatch('1'))
								return false;
						}
						break;
					default:
						return false;
					}
					return true;
				}");

			Test(@"
				LLLPG lexer {
					rule Digit(x::int) @{ &OddDigit '0'..'9' };
					rule OddDigit(x::int) @{ '1'|'3'|'5'|'7'|'9'|error {Error();} };
				};",
			@"
				void Digit(int x)
				{
					Check(Try_Scan_OddDigit(0), ""Expected OddDigit"");
					MatchRange('0', '9');
				}

				void OddDigit(int x)
				{
					switch (LA0) {
					case '1': case '3': case '5': case '7':
					case '9':
						Skip();
						break;
					default:
						{Error();}
						break;
					}
				}

				bool Try_Scan_OddDigit(int lookaheadAmt, int x) {
					using (new SavePosition(this, lookaheadAmt))
						return Scan_OddDigit(x);
				}
				bool Scan_OddDigit(int x)
				{
					switch (LA0) {
					case '1': case '3': case '5': case '7':
					case '9':
						Skip();
						break;
					default:
						return false;
					}
					return true;
				}");
			// NOTE: This is the Scan_OddDigit that we would prefer to get from the 
			// previous test - this would be the result of eliminating the error branch 
			// when generating the recognizer. But that's not how LLLPG is implemented.
			/*
				static readonly HashSet<int> Scan_OddDigit_set0 = NewSet('1', '3', '5', '7', '9');

				bool Try_Scan_OddDigit(int lookaheadAmt, int x) {
					using (new SavePosition(this, lookaheadAmt))
						return Scan_OddDigit(x);
				}
				bool Scan_OddDigit(int x)
				{
					if (!TryMatch(Scan_OddDigit_set0))
						return false;
					return true;
				}
			*/
		}

		[Test(Fails = "TODO: investigate this bug")]
		public void Bug_2016_11()
		{
			// Originally SLComment would do `if (la1 == -1 || la1 == '\\') goto stop;`,
			// needlessly checking for EOF. Fixed by editing `ComputeNextSets()` to call
			// `ComputeNextSet(..., addEOF: false)` not `addEOF: previous[i].Alt == ExitAlt`.
			// This caused a few other EOF checks to disappear from the test suite and
			// elsewhere, but it seems like a reasonable change.
			Test(@"
				[FullLLk, AddCsLineDirectives(false)]
				LLLPG (lexer) @{
					private token SLComment returns[object result] :
						'/' '/' nongreedy(_)* ('\\' '\\' | ('\r'|'\n'|EOF) =>);
				};", @"
					private object SLComment()
					{
						int la0, la1;
						Match('/');
						Match('/');
						for (;;) {
							switch (LA0) {
							case '\\':
								{
									la1 = LA(1);
									if (la1 == '\\')
										goto stop;
									else
										Skip();
								}
								break;
							case -1: case '\n': case '\r':
								goto stop;
							default:
								Skip();
								break;
							}
						}
					stop:;
						la0 = LA0;
						if (la0 == '\\') {
							Skip();
							Match('\\');
						} else { }
					}", null, Ecs.EcsLanguageService.Value);
		}

		[Test]
		public void Regression_2016_10()
		{
			// Regression: while turning off hoisting by default for semantic &{...} 
			// predicates, hoisting was accidentally turned off for syntactic &(...)
			// predicates too (even though the latter doesn't currently expose any way 
			// to do that in code)
			Test(@"
				@[AddCsLineDirectives(false)] LLLPG(lexer);
				@[private] rule Letter @{ 'a'..'z' };
				@[private] rule Word @{ Letter+ };
				@[private] rule XCode @{ &('x' Letter Letter) Letter+ };
				@[LL(1)]
				rule Choice @{ XCode / Word };",
			@"
				private void Letter() {
					MatchRange('a', 'z');
				}
				private bool Scan_Letter() {
					if (!TryMatchRange('a', 'z'))
						return false;
					return true;
				}
				private void Word() {
					int la0;
					Letter();
					for (;;) {
						la0 = LA0;
						if (la0 >= 'a' && la0 <= 'z')
							Letter();
						else
							break;
					}
				}
				private void XCode() {
					int la0;
					Letter();
					for (;;) {
						la0 = LA0;
						if (la0 >= 'a' && la0 <= 'z')
							Letter();
						else
							break;
					}
				}
				void Choice() {
					if (Try_XCode_Test0(0))
						XCode();
					else
						Word();
				}
				private bool Try_XCode_Test0(int lookaheadAmt) {
					using (new SavePosition(this, lookaheadAmt))
						return XCode_Test0();
				}
				private bool XCode_Test0() {
					if (!TryMatch('x'))
						return false;
					if (!Scan_Letter())
						return false;
					if (!Scan_Letter())
						return false;
					return true;
				}");
		}
		[Test]
		public void Regression_2016_05()
		{
			// The bug here was thought to relate to `error`, but it turned out
			// that in EC#, 'token' was treated as 'rule' if there's a return value.
			Test(@"
				[AddCsLineDirectives(false)]
				LLLPG (lexer);

				public override token TT NextToken() @{
					( '=' '='     {$result = TT.Eq;}
					/ '='         {$result = TT.Assign;}
					| error _?    {$result = TT.Invalid;} 
					)
				}
				", @"
				public override TT NextToken()
				{
					int la0, la1;
					TT result = default(TT);
					la0 = LA0;
					if (la0 == '=') {
						la1 = LA(1);
						if (la1 == '=') {
							Skip();
							Skip();
							result = TT.Eq;
						} else {
							Skip();
							result = TT.Assign;
						}
					} else {
						la0 = LA0;
						if (la0 != -1)
							Skip();
						result = TT.Invalid;
					}
					return result;
				}", null, Ecs.EcsLanguageService.Value);
		}

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
					Check(!(LA(0) == 0), ""Did not expect LA($LI) == $LI"");
					Check(0() && Bar(((int)LA0)()), ""Expected $LI() && Bar($LA())"");
					Check($LI, ""Expected $LI"");
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
			@"	private void Atom()
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
			TraceMessageSink.Value); // Suppress warnings caused by this test
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
				rule WeirdDigit    @{ '0' | &{@[Hoist] a} '1' | &{@[Hoist] b} '2' | &{@[Hoist] c} '3' 
				      | &{@[Hoist] d} '4' | &{@[Hoist] e} '5' | &{@[Hoist] f} '6' | &{@[Hoist] g} '7'
				      | &{@[Hoist] h} '8' | &{@[Hoist] i} '9' };
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
					case '1': { Check(a, ""Expected a""); Skip(); } break;
					case '2': { Check(b, ""Expected b""); Skip(); } break;
					case '3': { Check(c, ""Expected c""); Skip(); } break;
					case '4': { Check(d, ""Expected d""); Skip(); } break;
					case '5': { Check(e, ""Expected e""); Skip(); } break;
					case '6': { Check(f, ""Expected f""); Skip(); } break;
					case '7': { Check(g, ""Expected g""); Skip(); } break;
					case '8': { Check(h, ""Expected h""); Skip(); } break;
					default:  { Check(i, ""Expected i""); Match('9'); } break;
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
				rule WeirdDigit    @{ '0' | &{@[Hoist] a} '1' | &{@[Hoist] b} '2' | &{@[Hoist] c} '3' 
				      | &{@[Hoist] d} '4' | &{@[Hoist] e} '5' | &{@[Hoist] f} '6' | &{@[Hoist] g} '7'
				      | &{@[Hoist] h} '8' | &{@[Hoist] i} '9' };
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
					case '1': { Check(a, ""Expected a""); Skip(); } break;
					case '2': { Check(b, ""Expected b""); Skip(); } break;
					case '3': { Check(c, ""Expected c""); Skip(); } break;
					case '4': { Check(d, ""Expected d""); Skip(); } break;
					case '5': { Check(e, ""Expected e""); Skip(); } break;
					case '6': { Check(f, ""Expected f""); Skip(); } break;
					case '7': { Check(g, ""Expected g""); Skip(); } break;
					case '8': { Check(h, ""Expected h""); Skip(); } break;
					default:  { Check(i, ""Expected i""); Match('9'); } break;
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
