using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;

namespace Loyc.LLParserGenerator
{
	class LlpgAutoValueSaverVisitorTests : LlpgGeneralTestsBase
	{
		[Test] public void TestExplicitLabels()
		{
			// Two explicit labels
			Test(@"LLLPG (parser(terminalType = Symbol)) {
					rule Foo() @[ a:A a2:A {Checkpoint($a); Checkpoint($a2);} ];
				};", @"
					void Foo()
					{
						Symbol a = default(Symbol);
						Symbol a2 = default(Symbol);
						a = Match(A);
						a2 = Match(A);
						Checkpoint(a);
						Checkpoint(a2);
					}");
			// Explicit label for list type
			Test(@"LLLPG (lexer) {
					rule CountHashes()::int @[ h+:'#'* {return $h.Count;} ];
				};", @"
					int CountHashes()
					{
						int la0;
						List<int> h = new List<int>();
						// Line 2: ([#])*
						 for (;;) {
							la0 = LA0;
							if (la0 == '#')
								h.Add(MatchAny());
							else
								break;
						}
						return h.Count;
					}");
		}
		[Test] public void TestImplicitRuleRefs()
		{
			// Implicit rule reference
			Test(@"LLLPG (lexer) {
					rule Start @[ Foo(7) {f($Foo);} ];
					rule Foo(x::int)::int @[ {return x;} ];
				};", @"
					void Start()
					{
						int got_Foo = 0;
						got_Foo = Foo(7);
						f(got_Foo);
					}
					int Foo(int x)
					{
						return x;
					}");
			// Implicit rule references
			Test(@"LLLPG (parser(terminalType = Token)) {
					rule Foo() @[ (A C | B C) {Blah($B, $C);} ];
				};", @"
					void Foo()
					{
						int la0;
						Token tok_B = default(Token);
						Token tok_C = default(Token);
						// Line 2: (A C | B C)
						la0 = (int)LA0;
						if (la0 == A) {
							Skip();
							tok_C = Match(C);
						} else {
							tok_B = Match(B);
							tok_C = Match(C);
						}
						Blah(tok_B, tok_C);
					}");
		}
		[Test] public void TestImplicitTerminalRefs()
		{
			// Implicit terminal reference in semantic predicate
			Test(@"LLLPG (parser(terminalType = char)) {
					token Foo() @[ Op (&{$Op == '$'} '$')? ];
				};", @"
					void Foo()
					{
						int la0;
						char tok_Op = default(char);
						tok_Op = Match(Op);
						// Line 2: (&{tok_Op == '$'} '$')?
						la0 = (int)LA0;
						if (la0 == '$') {
							if (tok_Op == '$')
								Skip();
						}
					}");
			// Implicit terminal reference
			Test(@"LLLPG (lexer) {
					rule Foo() @[ _ (&{$_ == '$'} '$')? ];
				};", @"
					void Foo()
					{
						int la0;
						int tok__ = 0;
						tok__ = MatchExcept();
						// Line 2: (&{tok__ == '$'} [$])?
						la0 = LA0;
						if (la0 == '$') {
							Check(tok__ == '$', ""tok__ == '$'"");
							Skip();
						}
					}");
			// Implicit rule reference and implicit terminal reference
			Test(@"LLLPG (lexer) {
					rule Main @[ Percent? {if ($Percent != null) Yay();} ];
					rule Percent()::opt!char @[ '%' {return $'%' -> char;} ];
				};", @"
					void Main()
					{
						int la0;
						char? got_Percent = default(char?);
						// Line 2: (Percent)?
						la0 = LA0;
						if (la0 == '%')
							got_Percent = Percent();
						if ((got_Percent != null))
							Yay();
					}
					char? Percent()
					{
						int chx25 = 0;
						chx25 = Match('%');
						return (char) chx25;
					}");
		}
		[Test] public void TestMisc()
		{
			// Implicit $B should be a separate variable from explicit $b.
			Test(@"LLLPG (parser(terminalType = object)) {
					rule Foo() @[ {x::int;} a:A (b+:B)* C B x=B {Blah(a, b, $B);} ];
				};", @"
					void Foo()
					{
						int la0;
						object a = default(object);
						List<object> b = new List<object>();
						object tok_B = default(object);
						int x;
						a = Match(A);
						for (;;) {
							la0 = (int)LA0;
							if (la0 == B)
								b.Add(MatchAny());
							else
								break;
						}
						Match(C);
						tok_B = Match(B);
						x = Match(B);
						Blah(a, b, tok_B);
					}");
			// c+:C* should be parsed as (c+:C)*
			Test(@"LLLPG (parser(terminalType = Token)) {
					rule Foo() @[ A C      {FunWith($C);}
					            | B c+:C*  {FunWith($c);} ];
				};", @"
					void Foo()
					{
						int la0;
						List<Token> c = new List<Token>();
						Token tok_C = default(Token);
						// Line 2: (A C | B (C)*)
						la0 = (int)LA0;
						if (la0 == A) {
							Skip();
							tok_C = Match(C);
							FunWith(tok_C);
						} else {
							Match(B);
							// Line 3: (C)*
							for (;;) {
								la0 = (int)LA0;
								if (la0 == C)
									c.Add(MatchAny());
								else
									break;
							}
							FunWith(c);
						}
					}");
			// Unused labels should be unsubstituted
			Test(@"LLLPG (parser(terminalType = object)) {
					rule Foo() @[ c:C {Dollar($B, $C);} ];
				};", @"
					void Foo()
					{
						object c = default(object);
						c = Match(C);
						Dollar($B, $C);
					}");
		}
	}
}
