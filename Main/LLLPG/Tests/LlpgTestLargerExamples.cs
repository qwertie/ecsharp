using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Ecs;
using Loyc.MiniTest;

namespace Loyc.LLParserGenerator
{
	class LlpgTestLargerExamples : LlpgGeneralTestsBase
	{
		#region Email example (tests inputSource inputClass)
		
		[Test]
		public void ParseEmails()
		{
			// Parses email address according to RFC 5322, excluding quoted usernames.
			// This example demonstrates a few things...
			// - It shows how to use the inputSource and inputClass options when
			//   you don't want to make a class derived from BaseLexer
			// - It shows how you optimize a parser to avoid memory allocations.
			//   Memory allocation is required only once, before the first parse.
			//   However, there is a risk of a memory leak in this example...
			// - It also demonstrates how to pass the lexer state between rules,
			//   although it is redundant to do so here since the state is already
			//   available through the static variable 'src'.
			string input = @"
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
					
						public static rule EmailAddress Parse(UString email) {
							if (src == null)
								src = new LexerSource<UString>(email, """", 0, false);
							else
								src.Reset(email, """", 0, false);
							@{ UsernameChars(src) ('.' UsernameChars(src))* };
							int at = src.InputPosition;
							UString userName = email.Substring(0, at);
							@{ '@' DomainCharSeq(src) ('.' DomainCharSeq(src))* EOF };
							UString domain = email.Substring(at + 1);
							return new EmailAddress(userName, domain);
						}
						static rule UsernameChars(LexerSource<UString> src) @{
							('a'..'z'|'A'..'Z'|'0'..'9'|'!'|'#'|'$'|'%'|'&'|'\''|
							'*'|'+'|'/'|'='|'?'|'^'|'_'|'`'|'{'|'|'|'}'|'~'|'-')+
						};
						static rule DomainCharSeq(LexerSource<UString> src) @{
								   ('a'..'z'|'A'..'Z'|'0'..'9')
							[ '-'? ('a'..'z'|'A'..'Z'|'0'..'9') ]*
						};
					}
				}";
			string expectedOutput = @"
				struct EmailAddress
				{
					public EmailAddress(string userName, string domain) 
						{ UserName = userName; Domain = domain; }
					public UString UserName;
					public UString Domain;
					public override string ToString() { return UserName + ""@"" + Domain; }

					[ThreadStatic] static LexerSource<UString> src;
					public static EmailAddress Parse(UString email)
					{
						int la0;
						if (src == null)
							src = new LexerSource<UString>(email, """", 0, false);
						else
							src.Reset(email, """", 0, false);
						UsernameChars(src);
						// Line 20: ([.] UsernameChars)*
						 for (;;) {
							la0 = src.LA0;
							if (la0 == '.') {
								src.Skip();
								UsernameChars(src);
							} else
								break;
						}
						int at = src.InputPosition;
						UString userName = email.Substring(0, at);
						src.Match('@');
						DomainCharSeq(src);
						// Line 23: ([.] DomainCharSeq)*
						 for (;;) {
							la0 = src.LA0;
							if (la0 == '.') {
								src.Skip();
								DomainCharSeq(src);
							} else
								break;
						}
						src.Match(-1);
						UString domain = email.Substring(at + 1);
						return new EmailAddress(userName, domain);
					}
					static readonly HashSet<int> UsernameChars_set0 = LexerSource.NewSetOfRanges('!', '!', '#', '\'', '*', '+', '-', '-', '/', '9', '=', '=', '?', '?', 'A', 'Z', '^', '~');
					static void UsernameChars(LexerSource<UString> src)
					{
						int la0;
						src.Match(UsernameChars_set0);
						// Line 28: ([!#-'*+\-/-9=?A-Z^-~])*
						 for (;;) {
							la0 = src.LA0;
							if (UsernameChars_set0.Contains(la0))
								src.Skip();
							else
								break;
						}
					}
					static readonly HashSet<int> DomainCharSeq_set0 = LexerSource.NewSetOfRanges('0', '9', 'A', 'Z', 'a', 'z');
					static readonly HashSet<int> DomainCharSeq_set1 = LexerSource.NewSetOfRanges('-', '-', '0', '9', 'A', 'Z', 'a', 'z');
					static void DomainCharSeq(LexerSource<UString> src)
					{
						int la0;
						src.Match(DomainCharSeq_set0);
						// Line 33: (([\-])? [0-9A-Za-z])*
						 for (;;) {
							la0 = src.LA0;
							if (DomainCharSeq_set1.Contains(la0)) {
								// Line 33: ([\-])?
								la0 = src.LA0;
								if (la0 == '-')
									src.Skip();
								src.Match(DomainCharSeq_set0);
							} else
								break;
						}
					}
				}";
			Test(input, expectedOutput, null, EcsLanguageService.Value);
		}

		#endregion

		#region Calculator example (LES)
		// This is the oldest version of the calculator example, and uses
		// questionable pactices like const int tokens instead of enums. The
		// generated code would not compile anymore either (e.g. BaseLexer no 
		// longer has a list type parameter), but that's okay, it's still valid as 
		// a regression test.

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
					@[public] cons Lexer(source::UString)
					{
						base(source);
					};
					@[protected, override] fn Error(li::int, message::string)
					{
						Console.WriteLine(""At index {0}: {1}"", InputPosition+li, message);
					};

					_type::int;
					_value::double;
					_start::int;

					LLLPG lexer
					{
						@[public] token NextToken()::Token {
							_start = InputPosition;
							_value = null;
							@{ { _type = num; } Num
							 | { _type = id;  } Id
							 | { _type = mul; } '*'
							 | { _type = div; } '/'
							 | { _type = add; } '+'
							 | { _type = sub; } '-'
							 | { _type = set; } ':' '='
							 | { _type = num; } "".nan"" { _value = double.NaN; }
							 | { _type = num; } "".inf"" { _value = double.PositiveInfinity; }
							 | error
							   { _type = EOF; } (_ { _type = unknown; })? };
							return (new Token() { Type = _type; Value = _value; StartIndex = _start; });
						};
						@[private] token Id() @{
							('a'..'z'|'A'..'Z'|'_')
							('a'..'z'|'A'..'Z'|'_'|'0'..'9')*
							{ _value = CharSource.Substring(_startIndex, InputPosition - _startIndex); }
						};
						@[private] token Num() @{
							{dot::bool = @false;}
							('.' {dot = @true;})?
							'0'..'9'+
							(&!{dot} '.' '0'..'9'+)?
							{ _value = double.Parse(CharSource.Slice(_startIndex, InputPosition - _startIndex)); }
						};
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
										goto matchNum;
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
										goto error;
								}
								break;
							case '0': case '1': case '2': case '3':
							case '4': case '5': case '6': case '7':
							case '8': case '9':
								goto matchNum;
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
									goto error;
								break;
							}
							break;
						matchNum:
							{
								_type = num;
								Num();
							}
							break;
						error:
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
					private void Id()
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
					private void Num()
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
			@[public, partial] class Calculator(BaseParser!(Calculator.Token))
			{
				_vars::Dictionary!(string,double) = (new Dictionary!(string,double)());
				_tokens::List!Token = `new` List!Token();
				_input::string;
				
				@[public] fn Calculate(input::UString)::double
				{
					_input = input;
					_lexer = `new` Lexer(input);
					_tokens.Clear();
					t::Token;
					while ((t = lexer.NextToken()).Type != EOF) {
						_tokens.Add(t);
					};
					return Expr();
				};

				const EOF::int = -1;
				
				@[protected, override] fn EofInt()::int { return EOF; };
				@[protected, override] fn LA0Int()::int { return LT0.Type; };
				@[protected, override] fn LT(i::int)::Token
				{
					if i < _tokens.Count { 
						return _tokens[i]; 
					} else { 
						return (new Token { Type = EOF });
					};
				};
				@[protected, override] fn Error(li::int, message::string)
				{
					index::int = _input.Length;
					if InputPosition + li < _tokens.Count {
						index = _tokens[InputPosition + li].StartIndex;
					};
					Console.WriteLine(""Error at index {0}: {1}"", index, message);
				};
				@[protected, override] fn ToString(int `#var` tokenType)::string
				{
					switch tokenType {
						case id; return ""identifier"";
						case num; return ""number"";
						case set; return "":="";
						default; return (tokenType->char).ToString();
					};
				};

				LLLPG (parser(castLA(@false)))
				{
					rule Atom::double @{
						{ result::double; }
						( t:=id           { result = _vars[t.Value -> Symbol]; }
						| t:=num          { result = t.Value -> double; } 
						| '-' result=Atom { result = -result; }
						| '(' result=Expr ')')
						{ return result; }
					};
					rule MulExpr @{
						result:=Atom
						(op:=(mul|div) rhs:=Atom { result = Do(result, op, rhs); })*
						{ return result; }
					};
					rule AddExpr @{
						result:=MulExpr
						(op:=(add|sub) rhs:=MulExpr { result = Do(result, op, rhs); })*
						{ return result; }
					};
					rule Expr @{
						{ result::double; }
						( t:=id set result=Expr { _vars[t.Value.ToString()] = result; }
						| result=AddExpr )
						{ return result; }
					};
				};

				fn Do(left::double, op::Token, right::double)::double
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
					while (((t = lexer.NextToken()).Type != EOF)) {
						_tokens.Add(t);
					}
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
					if (InputPosition + li < _tokens.Count) {
						index = _tokens[InputPosition + li].StartIndex;
					}
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

		[Test]
		public void ScannerlessExpressionParser()
		{
			string input = @"
				#importMacros(Loyc.LLPG);
				using Loyc;
				using Loyc.Syntax;
				using Loyc.Syntax.Lexing;

				struct StringToken : ISimpleToken<string>
				{
					public string Type { get; set; }
					public object Value => Type;
					public int StartIndex { get; set; }
				}

				class ExprParser : BaseParserForList<StringToken, string>
				{
					public ExprParser(string input) 
						: this(input.Split(' ').Select(word => 
							new StringToken { Type=word }).ToList()) {}
					public ExprParser(IList<StringToken> tokens, ISourceFile file = null) 
						: base(tokens, default(StringToken), file ?? EmptySourceFile.Unknown) 
						{ F = new LNodeFactory(SourceFile); }
	
					protected override string ToString(string tokenType) => tokenType;

					LNodeFactory F = new LNodeFactory(EmptySourceFile.Unknown);

					LNode Op(LNode lhs, StringToken op, LNode rhs) {
						return F.Call((Symbol)op.Type, lhs, rhs, lhs.Range.StartIndex, rhs.Range.EndIndex);
					}

					LLLPG(parser(laType: string, terminalType: StringToken));

					public rule LNode Expr(int prec = 0) @{
						( '''-''' r:=Expr(50) 
						  { $result = F.Call((Symbol)'''-''', r, $'''-'''.StartIndex, r.Range.EndIndex); }
						/ result:Atom )
						greedy // to suppress ambiguity warning
						(   // Remember to add [Local] when your predicate uses a local variable
							&{[Local] prec <= 10}
							'''=''' r:=Expr(10)
							{ $result = Op($result, $'''=''', r); }
						|   &{[Local] prec < 20}
							op:=('''&&'''|'''||''') r:=Expr(20)
							{ $result = Op($result, op, r); }
						|   &{[Local] prec < 30}
							op:=('''>'''|'''<'''|'''>='''|'''<='''|'''=='''|'''!=''') r:=Expr(30)
							{ $result = Op($result, op, r); }
						|   &{[Local] prec < 40}
							op:=('''+'''|'''-''') r:=Expr(40)
							{ $result = Op($result, op, r); }
						|   &{[Local] prec < 50}
							op:=('''*'''|'''/'''|'''>>'''|'''<<''') r:=Expr(50)
							{ $result = Op($result, op, r); }
						|   '''(''' Expr ''')''' 
							{ $result = F.Call($result, $Expr, $result.Range.StartIndex); }
						|   '''.''' rhs:Atom 
							{ $result = F.Dot ($result, $rhs,  $result.Range.StartIndex); }
						)*
					};
					rule LNode PrefixExpr() @{
						( '''-''' r:=PrefixExpr
						  { $result = F.Call((Symbol)'''-''', r, $'''-'''.StartIndex, r.Range.EndIndex); }
						/ result:PrimaryExpr )
					};
					rule LNode PrimaryExpr() @{
						result:Atom
						(	'''(''' Expr ''')''' { $result = F.Call($result, $Expr, $result.Range.StartIndex); }
						|	'''.''' rhs:Atom { $result = F.Dot ($result, $rhs,  $result.Range.StartIndex); }
						)*
					};
					rule LNode Atom() @{
						'''(''' result:Expr ''')''' { $result = F.InParens($result); }
					/	_ { 
							double n; 
							$result = double.TryParse($_.Type, out n) ? F.Literal(n) : F.Id($_.Type);
						}
					};
				}";
			string expectedOutput = @"
				using Loyc;
				using Loyc.Syntax;
				using Loyc.Syntax.Lexing;

				struct StringToken : ISimpleToken<string>
				{
					public string Type { get; set; }
					public object Value => Type;
					public int StartIndex { get; set; }
				}

				class ExprParser : BaseParserForList<StringToken, string>
				{
					public ExprParser(string input)
						 : this(input.Split(' ').Select(word => 
						new StringToken { 
							Type = word
						}).ToList()) { }
					public ExprParser(IList<StringToken> tokens, ISourceFile file = null)
						 : base(tokens, default(StringToken), file ?? EmptySourceFile.Unknown)
					{ F = new LNodeFactory(SourceFile); }

					protected override string ToString(string tokenType) => tokenType;

					LNodeFactory F = new LNodeFactory(EmptySourceFile.Unknown);

					LNode Op(LNode lhs, StringToken op, LNode rhs) {
						return F.Call((Symbol) op.Type, lhs, rhs, lhs.Range.StartIndex, rhs.Range.EndIndex);
					}


					public LNode Expr(int prec = 0)
					{
						string la0, la1;
						LNode got_Expr = default(LNode);
						StringToken lit_dash = default(StringToken);
						StringToken litx3D = default(StringToken);
						LNode result = default(LNode);
						LNode rhs = default(LNode);
						// Line 34: (@""-"" Expr / Atom)
						la0 = (string) LA0;
						if (la0 == @""-"") {
							la1 = (string) LA(1);
							if (la1 != (string) EOF) {
								lit_dash = MatchAny();
								var r = Expr(50);
								// line 35
								result = F.Call((Symbol) @""-"", r, lit_dash.StartIndex, r.Range.EndIndex);
							} else
								result = Atom();
						} else
							result = Atom();
						// Line 39: greedy( &{prec <= 10} @""="" Expr | &{prec < 20} (@""&&""|@""||"") Expr | &{prec < 30} (@""!=""|@""<""|@""<=""|@""==""|@"">""|@"">="") Expr | &{prec < 40} (@""-""|@""+"") Expr | &{prec < 50} (@""*""|@""/""|@""<<""|@"">>"") Expr | @""("" Expr @"")"" | @""."" Atom )*
						for (;;) {
							switch ((string) LA0) {
							case @""="":
								{
									if (prec <= 10) {
										litx3D = MatchAny();
										var r = Expr(10);
										// line 41
										result = Op(result, litx3D, r);
									} else
										goto stop;
								}
								break;
							case @""&&"": case @""||"":
								{
									if (prec < 20) {
										var op = MatchAny();
										var r = Expr(20);
										// line 44
										result = Op(result, op, r);
									} else
										goto stop;
								}
								break;
							case @""!="": case @""<"": case @""<="": case @""=="":
							case @"">"": case @"">="":
								{
									if (prec < 30) {
										var op = MatchAny();
										var r = Expr(30);
										// line 47
										result = Op(result, op, r);
									} else
										goto stop;
								}
								break;
							case @""-"": case @""+"":
								{
									if (prec < 40) {
										var op = MatchAny();
										var r = Expr(40);
										// line 50
										result = Op(result, op, r);
									} else
										goto stop;
								}
								break;
							case @""*"": case @""/"": case @""<<"": case @"">>"":
								{
									if (prec < 50) {
										var op = MatchAny();
										var r = Expr(50);
										// line 53
										result = Op(result, op, r);
									} else
										goto stop;
								}
								break;
							case @""("":
								{
									Skip();
									got_Expr = Expr();
									Match(@"")"");
									// line 55
									result = F.Call(result, got_Expr, result.Range.StartIndex);
								}
								break;
							case @""."":
								{
									Skip();
									rhs = Atom();
									// line 57
									result = F.Dot(result, rhs, result.Range.StartIndex);
								}
								break;
							default:
								goto stop;
							}
						}
					stop:;
						return result;
					}

					LNode PrefixExpr()
					{
						string la0, la1;
						StringToken lit_dash = default(StringToken);
						LNode result = default(LNode);
						// Line 61: (@""-"" PrefixExpr / PrimaryExpr)
						la0 = (string) LA0;
						if (la0 == @""-"") {
							la1 = (string) LA(1);
							if (la1 != (string) EOF) {
								lit_dash = MatchAny();
								var r = PrefixExpr();
								// line 62
								result = F.Call((Symbol) @""-"", r, lit_dash.StartIndex, r.Range.EndIndex);
							} else
								result = PrimaryExpr();
						} else
							result = PrimaryExpr();
						return result;
					}

					LNode PrimaryExpr()
					{
						string la0;
						LNode got_Expr = default(LNode);
						LNode result = default(LNode);
						LNode rhs = default(LNode);
						result = Atom();
						// Line 67: (@""("" Expr @"")"" | @""."" Atom)*
						for (;;) {
							la0 = (string) LA0;
							if (la0 == @""("") {
								Skip();
								got_Expr = Expr();
								Match(@"")"");
								// line 67
								result = F.Call(result, got_Expr, result.Range.StartIndex);
							} else if (la0 == @""."") {
								Skip();
								rhs = Atom();
								// line 68
								result = F.Dot(result, rhs, result.Range.StartIndex);
							} else
								break;
						}
						return result;
					}

					LNode Atom()
					{
						string la0, la1;
						LNode result = default(LNode);
						StringToken tok__ = default(StringToken);
						// Line 72: (@""("" Expr @"")"" / ~(EOF))
						do {
							la0 = (string) LA0;
							if (la0 == @""("") {
								la1 = (string) LA(1);
								if (la1 != (string) EOF) {
									Skip();
									result = Expr();
									Match(@"")"");
									// line 72
									result = F.InParens(result);
								} else
									goto match2;
							} else
								goto match2;
							break;
						match2:
							{
								tok__ = MatchExcept();
								// line 74
								double n;
								result = double.TryParse(tok__.Type, out n) ? F.Literal(n) : F.Id(tok__.Type);
							}
						} while (false);
						return result;
					}
				}";
			Test(input, expectedOutput, null, EcsLanguageService.Value);
		}
	}
}
