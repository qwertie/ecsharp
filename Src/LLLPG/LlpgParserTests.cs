using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using Loyc.Utilities;
using Loyc.Collections;

namespace Loyc.LLParserGenerator
{
	using S = CodeSymbols;
	using System.Reflection;

	/// <summary>Tests the stage 1 parser, the stage 2 parser and LLLPG installed 
	/// in the whole Micro-LEL MacroProcessor pipeline.</summary>
	[TestFixture]
	class LlpgParserTests : Assert
	{
		static LNodeFactory F = new LNodeFactory(EmptySourceFile.Default);
		static Symbol Seq = S.Tuple;
		static Symbol AndNot = GSymbol.Get("#&!");
		static Symbol Gate = S.Lambda, Plus = GSymbol.Get("#suf+"), Star = GSymbol.Get("#suf*"), Opt = GSymbol.Get("#suf?");
		static Symbol Greedy = GSymbol.Get("greedy"), Nongreedy = GSymbol.Get("nongreedy");
		static Symbol Default = GSymbol.Get("default"), Error = GSymbol.Get("error");
		static LNode a = F.Id("a"), b = F.Id("b"), c = F.Id("c");

		[SetUp]
		void SetUp()
		{
			ParsingService.Current = LesLanguageService.Value;
			MessageSink.Current = MessageSink.Console;
		}

		[Test]
		public void Stage1Les_SimpleTests()
		{
			TestStage1("a", a);
			TestStage1("'a'", F.Literal('a'));
			TestStage1("123", F.Literal(123));
			TestStage1("a..b", F.Call(S.DotDot, a, b));
			TestStage1("~a", F.Call(S.NotBits, a));
			TestStage1("a*", F.Call(Star, a));
			TestStage1("a+", F.Call(Plus, a));
			TestStage1("a | b", F.Call(S.OrBits, a, b));
			TestStage1("a / b", F.Call(S.Div, a, b));
			TestStage1("a(b | c)", F.Call(a, F.Call(S.OrBits, b, c)));
			TestStage1("a => b", F.Call(Gate, a, b));
			TestStage1("()", F.Call(Seq));
			TestStage1("a b", F.Call(Seq, a, b));
			TestStage1("(a) (b)", F.Call(Seq, a, b));
			TestStage1("(a b)?", F.Call(Opt, F.Call(Seq, a, b)));
			TestStage1("{ a() }", F.Braces(F.Call(a)));
			TestStage1("&{ a b | c; }", F.Call(S.AndBits, F.Braces(F.Call(a, F.Call(S.OrBits, b, c)))));
			TestStage1("&!{ a(); b(); }", F.Call(AndNot, F.Braces(F.Call(a), F.Call(b))));
			TestStage1("greedy a", F.Call(Greedy, a));
			TestStage1("nongreedy a", F.Call(Nongreedy, a));
			TestStage1("nongreedy(a)", F.Call(Nongreedy, a));
			TestStage1("default a", F.Call(Default, a));
			TestStage1("error a", F.Call(Error, a));
			TestStage1("a := b", F.Call(S.QuickBindSet, a, b));
			TestStage1("a = b..c", F.Call(S.Set, a, F.Call(S.DotDot, b, c)));
			TestStage1("a += _", F.Call(S.AddSet, a, F.Id("_")));
		}
		[Test]
		public void Stage1Les_MoreTests()
		{
			TestStage1("~a..b", F.Call(S.NotBits, F.Call(S.DotDot, a, b)));
			TestStage1("{ a(); } b c", F.Call(Seq, F.Braces(F.Call(a)), b, c));
			TestStage1("a (b c)", F.Call(Seq, a, F.Call(Seq, b, c)));
			TestStage1("a | (a b c)", F.Call(S.OrBits, a, F.Call(Seq, a, b, c)));
			TestStage1("a(b c)", F.Call(a, F.Call(b, c)));
			TestStage1("a | b / c", F.Call(S.Div, F.Call(S.OrBits, a, b), c));
			TestStage1("a / b | c", F.Call(S.OrBits, F.Call(S.Div, a, b), c));
			TestStage1("a* b | c", F.Call(S.OrBits, F.Call(Seq, F.Call(Star, a), b), c));
			TestStage1("a b? / c", F.Call(S.Div, F.Call(Seq, a, F.Call(Opt, b)), c));
			TestStage1("a / b => b+ / c", F.Call(S.Div, F.Call(S.Div, a, F.Call(Gate, b, F.Call(Plus, b))), c));
			TestStage1("~(a..b) | (-a)..b.c", F.Call(S.OrBits, F.Call(S.NotBits, F.Call(S.DotDot, a, b)), F.Call(S.DotDot, F.Call(S.Sub, a), F.Dot(b, c))));
			TestStage1("~ a..b  |  -a ..b.c", F.Call(S.OrBits, F.Call(S.NotBits, F.Call(S.DotDot, a, b)), F.Call(S.DotDot, F.Call(S.Sub, a), F.Dot(b, c))));
			TestStage1("a..b+", F.Call(Plus, F.Call(S.DotDot, a, b)));
			TestStage1("greedy(a | b)+", F.Call(Plus, F.Call(Greedy, F.Call(S.OrBits, a, b))));
			TestStage1("nongreedy a+",   F.Call(Plus, F.Call(Nongreedy, a)));
			TestStage1("default a b | c", F.Call(S.OrBits, F.Call(Default, F.Call(Seq, a, b)), c));
			TestStage1("error   a b | c", F.Call(S.OrBits, F.Call(Error,   F.Call(Seq, a, b)), c));
			TestStage1("(a | b? 'c')*", F.Call(Star, F.Call(S.OrBits, a, F.Call(Seq, F.Call(Opt, b), F.Literal('c')))));
			TestStage1("t:=id { } / '-' t:=num { } / '(' ')'", F.Call(S.Div, F.Call(S.Div, 
				F.Tuple(F.Call(S.QuickBindSet, F.Id("t"), F.Id("id")), F.Braces()),
				F.Tuple(F.Literal('-'), F.Call(S.QuickBindSet, F.Id("t"), F.Id("num")), F.Braces())),
				F.Tuple(F.Literal('('), F.Literal(')'))));
		}

		void TestStage1(string text, LNode expected)
		{
			var lexer = ParsingService.Current.Tokenize(text, MessageSink.Console);
			var tokens = lexer.Buffered();
			var parser = new StageOneParser(tokens, lexer.File, MessageSink.Console);
			LNode result = parser.Parse();
			AreEqual(expected, result);
		}

		[Test]
		public void Stage2_Tests()
		{
			// If we change the way Preds are printed, this will break, of course
			TestStage2(true, "az", "'a'..'z'", "[a-z]");
			TestStage2(true, "azAZ", "('a'..'z')|('A'..'Z')", "[A-Za-z]");
			TestStage2(true, "NotAZ", "~('A'..'Z')", @"[^\$A-Z]");
			TestStage2(true, "Seq", "('-', '0'..'9')", @"[\-] [0-9]");
			TestStage2(true, "Hi0-9", @"(""Hi"", '0'..'9')", "[H] [i] [0-9]");
			TestStage2(true, "Or1", @"""ETX"" | 3", "([E] [T] [X] | (3))");
			TestStage2(true, "Or2", @"(~10, {code;}) | '\n'", @"(~(-1, 10) | [\n])"); // code blocks not printed
			TestStage2(true, "Star", @"@`#suf*`('0'..'9')", "([0-9])*");
			TestStage2(true, "Plus", @"@`#suf+`('0'..'9')", "[0-9] ([0-9])*");
			TestStage2(true, "Opt", @"@`#suf?`(('a','b'))", "([a] [b])?");
			TestStage2(true, "Greedy", @"@`#suf*`(greedy(('a','b')))", "greedy([a] [b])*");
			TestStage2(true, "Nongreedy", @"@`#suf*`(nongreedy(('a','b')))", "nongreedy([a] [b])*");
			TestStage2(true, "Default1", @"('a'|""bee""|default('b'))", "([a] | [b] [e] [e] | default [b])");
			TestStage2(true, "Default2", @"@`#suf*`('a'|default('b')|'c')", "([a] | default [b] | [c])*");
			TestStage2(true, Tuple.Create("RuleRef", @"'.' | Digit", "([.] | Digit)"),
			                 Tuple.Create("Digit", "'0'..'9'", "[0-9]"));
			TestStage2(true, "ABorCD", @"'a'|'e'|'i'|'o'|'u'", "[aeiou]");
			TestStage2(false, "AB+orCD", @"@`#suf+`(A.B) | C.D", "(A.B (A.B)* | C.D)");
		}

		[Test]
		public void Stage2_Nondeterministic()
		{
			// can randomly switch between zero|one and one|zero due to hashtable representation in PGNodeSet
			// (I'm not sure why it can change every time the program runs, though!)
			TestStage2(false, Tuple.Create("RuleRef", @"""NaN"" | (Digit, _)", @"(""NaN"" | Digit ~(EOF))"),
			                  Tuple.Create("Digit", "zero|one", "(zero|one)"));
			// can randomly switch between a|b and b|a due to hashtable representation
			TestStage2(false, "AorB", @"a | b", "(a|b)");
		}

		void TestStage2(bool lexer, string ruleName, string inputExpr, string asString)
		{
			TestStage2(lexer, Tuple.Create(ruleName, inputExpr, asString));
		}
		void TestStage2(bool lexer, params Tuple<string,string,string>[] ruleTuples)
		{
			var helper = lexer ? (IPGCodeGenHelper)new IntStreamCodeGenHelper() : new GeneralCodeGenHelper();
			var rules = new List<Pair<Rule,LNode>>();
			foreach (var tuple in ruleTuples)
			{
				string ruleName = tuple.Item1, inputExpr = tuple.Item2;
				var node = LesLanguageService.Value.ParseSingle(inputExpr, MessageSink.Console, ParsingService.Exprs);
				var rule = new Rule(node, GSymbol.Get(ruleName), null);
				rules.Add(Pair.Create(rule, node));
			}
			
			var parser = new StageTwoParser(helper, MessageSink.Console);
			parser.Parse(rules);
			for (int i = 0; i < rules.Count; i++) {
				var rule = rules[i].A;
				var ruleAsString = rule.Pred.ToString();
				var expected = ruleTuples[i].Item3;
				if (expected == null)
					MessageSink.Console.Write(MessageSink.Warning, ruleTuples[i].Item1, ruleAsString);
				else
					AreEqual(expected, ruleAsString);
			}
		}

		[Test]
		public void SimpleMatching()
		{
			Test(@"
			LLLPG_stage1 @[ 'x' '0'..'9'* ];
			LLLPG lexer {
				[pub] rule Foo @[ 'x' '0'..'9' '0'..'9' ];
			}", @"
				('x', @`#suf*`('0'..'9'));
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
			Test(@"
			LLLPG lexer {
				[pub] rule a @[ 'A'|'a' ];
				[pub] rule b @[ 'B'|'b' ];
				[pub] rule Foo @[ (a | b? 'c')* ];
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
		public void FullLL2()
		{
			Test(@"
			[FullLLk] LLLPG lexer {
				[pub] rule FullLL2 @[ ('a' 'b' | 'b' 'a') 'c' | ('a' 'a' | 'b' 'b') 'c' ];
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
		public void SymbolTest()
		{
			Test(@"LLLPG parser(laType(Symbol), allowSwitch(@false)) {
				[pub] rule Stmt @[ @@Number (@@print @@DQString | @@goto @@Number) @@Newline ];
				[pub] rule Stmts @[ Stmt* ];
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
				[recognizer { [protected] def IsOddDigit(y::float); }]
				rule OddDigit(x::int) @[ '1'|'3'|'5'|'7'|'9' ];
				rule NonDigit @[ &!Digit(0) _ ];
				rule EvenDigit @[ &!OddDigit _ ];
			};", @"
				void Digit(int x)
				{
					MatchRange('0', '9');
				}
				bool Is_Digit(int x)
				{
					using (new SavedPosition(this)) {
						if (!TryMatchRange('0', '9'))
							return false;
					}
					return true;
				}
				static readonly IntSet OddDigit_set0 = IntSet.Parse(""[13579]"");
				void OddDigit(int x)
				{
					Match(OddDigit_set0);
				}
				protected bool IsOddDigit(float y)
				{
					using (new SavedPosition(this)) {
						if (!TryMatch(OddDigit_set0))
							return false;
					}
					return true;
				}
				void NonDigit()
				{
					Check(!Is_Digit(), ""Digit"");
					MatchExcept();
				}
				void EvenDigit()
				{
					Check(!IsOddDigit(), ""OddDigit"");
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
					Check(Number_Test0(), ""[.0-9]"");
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
				bool Number_Test0()
				{
					using (new SavedPosition(this)) {
						if (!TryMatchRange('.', '.', '0', '9'))
							return false;
					}
					return true;
				}
			");
		}

		[Test]
		public void CalculatorLexer()
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
					protected override def Error(inputPosition::int, message::string)
					{
						Console.WriteLine(""At index {0}: {1}"", inputPosition, message);
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
					protected override void Error(int inputPosition, string message)
					{
						Console.WriteLine(""At index {0}: {1}"", inputPosition, message);
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
					static readonly IntSet Id_set0 = IntSet.Parse(""[0-9A-Z_a-z]"");
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
		public void CalculatorRunner()
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
				protected override def Error(inputPosition::int, message::string)
				{
					index::int = _input.Length;
					if inputPosition < _tokens.Count
						index = _tokens[inputPosition].StartIndex;
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
				protected override void Error(int inputPosition, string message)
				{
					int index = _input.Length;
					if (inputPosition < _tokens.Count)
						index = _tokens[inputPosition].StartIndex;
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

//        [Test]
//        public void SyntaxError()
//        {
//            Test(@"rule Foo @[
//				@ @ wtf ;?! """"];", @"");
//        }

		class TestCompiler : LEL.TestCompiler
		{
			public TestCompiler(IMessageSink sink, ISourceFile sourceFile)
				: base(sink, sourceFile)
			{
				MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("Loyc.LLParserGenerator"));
				AddMacros(Assembly.GetExecutingAssembly());
			}
		}

		SeverityMessageFilter _sink = new SeverityMessageFilter(MessageSink.Console, MessageSink.Debug);

		void Test(string input, string expected)
		{
			using (LNode.PushPrinter(Ecs.EcsNodePrinter.Printer)) {
				var c = new TestCompiler(_sink, new StringCharSourceFile(input, ""));
				c.Run();
				Assert.AreEqual(StripExtraWhitespace(expected), StripExtraWhitespace(c.Output.ToString()));
			}
		}
		static string StripExtraWhitespace(string a) { return LEL.MacroProcessorTests.StripExtraWhitespace(a); }
	}
}
