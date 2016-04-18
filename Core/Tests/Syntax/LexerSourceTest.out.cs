// Generated from LexerSourceTest.ecs by LeMP custom tool. LeMP version: 1.7.3.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax.Lexing;
using Loyc.Collections.Impl;
using Loyc.MiniTest;
namespace Loyc.Syntax.Tests
{
	using TT = CalcTokenType;
	[TestFixture]
	public class LexerSourceTests_Calculator : TestHelpers
	{
		static Token T(TT type, object value = null)
		{
			return new Token((int) type, -1, 0, NodeStyle.Default, value);
		}
		static IListSource<Token> Lex(string str)
		{
			var b = new CalculatorLexer(str).Buffered();
			var _ = b.Count;
			return b;
		}
		[Test]
		public void SimpleTests()
		{
			ExpectList(Lex("2"), T(TT.Num, 2d));
			ExpectList(Lex("25"), T(TT.Num, 25d));
			ExpectList(Lex("2.5"), T(TT.Num, 2.5));
			ExpectList(Lex(".25"), T(TT.Num, 0.25));
			ExpectList(Lex("x"), T(TT.Id, "x"));
			ExpectList(Lex("Foo_7"), T(TT.Id, "Foo_7"));
		}
		[Test]
		public void MoreTests()
		{
			ExpectList(Lex("x *+ y"), T(TT.Id, "x"), T(TT.Mul), T(TT.Add), T(TT.Id, "y"));
			ExpectList(Lex(" 20 ; 40 - 5/0.25"), T(TT.Num, 20d), T(TT.Semicolon), T(TT.Num, 40d), T(TT.Sub), T(TT.Num, 5d), T(TT.Div), T(TT.Num, 0.25));
			ExpectList(Lex("  (the end)  "), T(TT.LParen), T(TT.Id, "the"), T(TT.Id, "end"), T(TT.RParen));
		}
	}
	public enum CalcTokenType
	{
		EOF = TokenKind.Spaces, Id = TokenKind.Id, Num = TokenKind.Literal, Shr = TokenKind.Operator + 1, Shl = TokenKind.Operator + 2, Assign = TokenKind.Assignment + 1, GT = TokenKind.Operator + 3, LT = TokenKind.Operator + 4, Exp = TokenKind.Operator + 5, Mul = TokenKind.Operator + 6, Div = TokenKind.Operator + 7, Add = TokenKind.Operator + 8, Sub = TokenKind.Operator + 9, Semicolon = TokenKind.Separator, LParen = TokenKind.LParen, RParen = TokenKind.RParen, Colon = TokenKind.Separator + 1, LBrace = TokenKind.LBrace, RBrace = TokenKind.RBrace, Unknown
	}
	partial class CalculatorLexer : BaseILexer<ICharSource,Token>
	{
		public CalculatorLexer(UString text, string fileName = "") : base(text, fileName)
		{
		}
		public CalculatorLexer(ICharSource text, string fileName = "") : base(text, fileName)
		{
		}
		TT _tokenType;
		int _startIndex;
		object _value;
		public override Maybe<Token> NextToken()
		{
			int la0, la1;
			// Line 100: ([\t ] | Newline)*
			for (;;) {
				switch (LA0) {
				case '\t':
				case ' ':
					Skip();
					break;
				case '\n':
				case '\r':
					Newline();
					break;
				default:
					goto stop;
				}
			}
		stop:;
			_startIndex = this.InputPosition;
			_value = null;
			// Line 103: ( (Num | Id | [.] [n] [a] [n] | [.] [i] [n] [f]) | ([>] [>] / [<] [<] / [=] / [>] / [<] / [\^] / [*] / [/] / [+] / [\-] / [;] / [(] / [)] / [:] / [{] / [}]) )
			do {
				la0 = LA0;
				switch (la0) {
				case '.':
					{
						la1 = LA(1);
						if (la1 >= '0' && la1 <= '9')
							goto matchNum;
						else if (la1 == 'n') {
							#line 105 "LexerSourceTest.ecs"
							_tokenType = TT.Num;
							#line default
							Skip();
							Skip();
							Match('a');
							Match('n');
							#line 105 "LexerSourceTest.ecs"
							_value = double.NaN;
							#line default
						} else if (la1 == 'i') {
							#line 106 "LexerSourceTest.ecs"
							_tokenType = TT.Num;
							#line default
							Skip();
							Skip();
							Match('n');
							Match('f');
							#line 106 "LexerSourceTest.ecs"
							_value = double.PositiveInfinity;
							#line default
						} else
							goto error;
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
					goto matchNum;
				case '>':
					{
						la1 = LA(1);
						if (la1 == '>') {
							Skip();
							Skip();
							#line 129 "LexerSourceTest.ecs"
							_tokenType = TT.Shr;
							#line default
						} else if (la1 == -1) {
							Skip();
							#line 129 "LexerSourceTest.ecs"
							_tokenType = TT.GT;
							#line default
						} else
							goto error;
					}
					break;
				case '<':
					{
						la1 = LA(1);
						if (la1 == '<') {
							Skip();
							Skip();
							#line 129 "LexerSourceTest.ecs"
							_tokenType = TT.Shl;
							#line default
						} else if (la1 == -1) {
							Skip();
							#line 129 "LexerSourceTest.ecs"
							_tokenType = TT.LT;
							#line default
						} else
							goto error;
					}
					break;
				case '=':
					{
						Skip();
						#line 129 "LexerSourceTest.ecs"
						_tokenType = TT.Assign;
						#line default
					}
					break;
				case '^':
					{
						Skip();
						#line 129 "LexerSourceTest.ecs"
						_tokenType = TT.Exp;
						#line default
					}
					break;
				case '*':
					{
						Skip();
						#line 129 "LexerSourceTest.ecs"
						_tokenType = TT.Mul;
						#line default
					}
					break;
				case '/':
					{
						Skip();
						#line 129 "LexerSourceTest.ecs"
						_tokenType = TT.Div;
						#line default
					}
					break;
				case '+':
					{
						Skip();
						#line 129 "LexerSourceTest.ecs"
						_tokenType = TT.Add;
						#line default
					}
					break;
				case '-':
					{
						Skip();
						#line 129 "LexerSourceTest.ecs"
						_tokenType = TT.Sub;
						#line default
					}
					break;
				case ';':
					{
						Skip();
						#line 129 "LexerSourceTest.ecs"
						_tokenType = TT.Semicolon;
						#line default
					}
					break;
				case '(':
					{
						Skip();
						#line 129 "LexerSourceTest.ecs"
						_tokenType = TT.LParen;
						#line default
					}
					break;
				case ')':
					{
						Skip();
						#line 129 "LexerSourceTest.ecs"
						_tokenType = TT.RParen;
						#line default
					}
					break;
				case ':':
					{
						Skip();
						#line 129 "LexerSourceTest.ecs"
						_tokenType = TT.Colon;
						#line default
					}
					break;
				case '{':
					{
						Skip();
						#line 129 "LexerSourceTest.ecs"
						_tokenType = TT.LBrace;
						#line default
					}
					break;
				case '}':
					{
						Skip();
						#line 129 "LexerSourceTest.ecs"
						_tokenType = TT.RBrace;
						#line default
					}
					break;
				default:
					if (la0 >= 'A' && la0 <= 'Z' || la0 == '_' || la0 >= 'a' && la0 <= 'z') {
						#line 104 "LexerSourceTest.ecs"
						_tokenType = TT.Id;
						#line default
						Id();
					} else
						goto error;
					break;
				}
				break;
			matchNum:
				{
					#line 103 "LexerSourceTest.ecs"
					_tokenType = TT.Num;
					#line default
					Num();
				}
				break;
			error:
				{
					// Line 109: ([^\$] | )
					la0 = LA0;
					if (la0 != -1) {
						Skip();
						#line 109 "LexerSourceTest.ecs"
						_tokenType = TT.Unknown;
						#line default
					} else {
						#line 109 "LexerSourceTest.ecs"
						return NoValue.Value;
						#line default
					}
				}
			} while (false);
			_current = new Token((int) _tokenType, _startIndex, this.InputPosition - _startIndex, _value);
			return _current;
		}
		static readonly HashSet<int> Id_set0 = NewSetOfRanges('0', '9', 'A', 'Z', '_', '_', 'a', 'z');
		void Id()
		{
			int la0;
			Skip();
			// Line 117: ([0-9A-Z_a-z])*
			for (;;) {
				la0 = LA0;
				if (Id_set0.Contains(la0))
					Skip();
				else
					break;
			}
			#line 118 "LexerSourceTest.ecs"
			_value = this.CharSource.Slice(_startIndex, this.InputPosition - _startIndex).ToString();
			#line default
		}
		void Num()
		{
			int la0, la1;
			int dot = 0;
			// Line 121: ([.])?
			la0 = LA0;
			if (la0 == '.')
				dot = MatchAny();
			MatchRange('0', '9');
			// Line 122: ([0-9])*
			for (;;) {
				la0 = LA0;
				if (la0 >= '0' && la0 <= '9')
					Skip();
				else
					break;
			}
			// Line 123: (&{dot == 0} [.] [0-9] ([0-9])*)?
			la0 = LA0;
			if (la0 == '.') {
				if (dot == 0) {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						Skip();
						Skip();
						// Line 123: ([0-9])*
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
			#line 124 "LexerSourceTest.ecs"
			_value = double.Parse(this.CharSource.Slice(_startIndex, this.InputPosition - _startIndex).ToString(), CultureInfo.InvariantCulture);
			#line default
		}
	}
}
