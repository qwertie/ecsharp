// Generated from LexerSourceTest.ecs by LeMP custom tool. LeMP version: 30.1.0.0
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
using Loyc.Syntax.Lexing;	// for LexerSource, ISimpleToken<int>
using Loyc.Collections.Impl;
using Loyc.MiniTest;

namespace Loyc.Syntax.Tests
{
	using TT = CalcTokenType;

	[TestFixture] 
	public class LexerSourceTests_Calculator : TestHelpers
	{
		static Token T(TT type, object? value = null) {
			return new Token((int) type, -1, 0, NodeStyle.Default, value);
		}
		static IListSource<Token> Lex(string str) {
			var b = new CalculatorLexer(str).Buffered();
			var _ = b.Count;	// force immediate lexing
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
			ExpectList(Lex(" 20 ; 40 - 5/0.25"), T(TT.Num, 20d), T(TT.Semicolon), 
			T(TT.Num, 40d), T(TT.Sub), T(TT.Num, 5d), T(TT.Div), T(TT.Num, 0.25));
			ExpectList(Lex("  (the end)  "), T(TT.LParen), T(TT.Id, "the"), T(TT.Id, "end"), T(TT.RParen));
		}
	}
	public enum CalcTokenType
	{
		EOF = TokenKind.Spaces	// If you use EOF = 0, default(Token) represents EOF
		,
		Id = TokenKind.Id,
		Num = TokenKind.Literal,
		
		Shr = TokenKind.Operator + 1	// inside 'unroll', must use ';' instead of ',' as separator
		,
		Shl = TokenKind.Operator + 2	// inside 'unroll', must use ';' instead of ',' as separator
		,
		Assign = TokenKind.Assignment + 1	// inside 'unroll', must use ';' instead of ',' as separator
		,
		GT = TokenKind.Operator + 3	// inside 'unroll', must use ';' instead of ',' as separator
		,
		LT = TokenKind.Operator + 4	// inside 'unroll', must use ';' instead of ',' as separator
		,
		Exp = TokenKind.Operator + 5	// inside 'unroll', must use ';' instead of ',' as separator
		,
		Mul = TokenKind.Operator + 6	// inside 'unroll', must use ';' instead of ',' as separator
		,
		Div = TokenKind.Operator + 7	// inside 'unroll', must use ';' instead of ',' as separator
		,
		Add = TokenKind.Operator + 8	// inside 'unroll', must use ';' instead of ',' as separator
		,
		Sub = TokenKind.Operator + 9	// inside 'unroll', must use ';' instead of ',' as separator
		,
		Semicolon = TokenKind.Separator	// inside 'unroll', must use ';' instead of ',' as separator
		,
		LParen = TokenKind.LParen	// inside 'unroll', must use ';' instead of ',' as separator
		,
		RParen = TokenKind.RParen	// inside 'unroll', must use ';' instead of ',' as separator
		,
		Colon = TokenKind.Separator + 1	// inside 'unroll', must use ';' instead of ',' as separator
		,
		LBrace = TokenKind.LBrace	// inside 'unroll', must use ';' instead of ',' as separator
		,
		RBrace = TokenKind.RBrace	// inside 'unroll', must use ';' instead of ',' as separator
		,
		Unknown
	}
	
	//--------------------------------------------------------------------------
	//-- LEXER -----------------------------------------------------------------
	//--------------------------------------------------------------------------
	partial class CalculatorLexer : BaseILexer<ICharSource, Token>
	{

		public CalculatorLexer(UString text, string fileName = "")
			 : base(text, fileName) { }
		public CalculatorLexer(ICharSource text, string fileName = "")
			 : base(text, fileName) { }

		TT _tokenType;
		int _startIndex;
		object? _value;

		public override Maybe<Token> NextToken()
		{
			int la0, la1;
			// Line 100: ([\t ] | Newline)*
			for (;;) {
				switch (LA0) {
				case '\t': case ' ':
					Skip();
					break;
				case '\n': case '\r':
					Newline();
					break;
				default:
					goto stop;
				}
			}
		stop:;
			// line 101
			_startIndex = this.InputPosition;
			// line 102
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
							// line 105
							_tokenType = TT.Num;
							Skip();
							Skip();
							Match('a');
							Match('n');
							// line 105
							_value = double.NaN;
						} else if (la1 == 'i') {
							// line 106
							_tokenType = TT.Num;
							Skip();
							Skip();
							Match('n');
							Match('f');
							// line 106
							_value = double.PositiveInfinity;
						} else
							goto error;
					}
					break;
				case '0': case '1': case '2': case '3':
				case '4': case '5': case '6': case '7':
				case '8': case '9':
					goto matchNum;
				case '>':
					{
						la1 = LA(1);
						if (la1 == '>') {
							Skip();
							Skip(); // line 129
							_tokenType = TT.Shr;
						} else {
							Skip(); // line 129
							_tokenType = TT.GT;
						}
					}
					break;
				case '<':
					{
						la1 = LA(1);
						if (la1 == '<') {
							Skip();
							Skip(); // line 129
							_tokenType = TT.Shl;
						} else {
							Skip(); // line 129
							_tokenType = TT.LT;
						}
					}
					break;
				case '=':
					{
						Skip(); // line 129
						_tokenType = TT.Assign;
					}
					break;
				case '^':
					{
						Skip(); // line 129
						_tokenType = TT.Exp;
					}
					break;
				case '*':
					{
						Skip(); // line 129
						_tokenType = TT.Mul;
					}
					break;
				case '/':
					{
						Skip(); // line 129
						_tokenType = TT.Div;
					}
					break;
				case '+':
					{
						Skip(); // line 129
						_tokenType = TT.Add;
					}
					break;
				case '-':
					{
						Skip(); // line 129
						_tokenType = TT.Sub;
					}
					break;
				case ';':
					{
						Skip(); // line 129
						_tokenType = TT.Semicolon;
					}
					break;
				case '(':
					{
						Skip(); // line 129
						_tokenType = TT.LParen;
					}
					break;
				case ')':
					{
						Skip(); // line 129
						_tokenType = TT.RParen;
					}
					break;
				case ':':
					{
						Skip(); // line 129
						_tokenType = TT.Colon;
					}
					break;
				case '{':
					{
						Skip(); // line 129
						_tokenType = TT.LBrace;
					}
					break;
				case '}':
					{
						Skip(); // line 129
						_tokenType = TT.RBrace;
					}
					break;
				default:
					if (la0 >= 'A' && la0 <= 'Z' || la0 == '_' || la0 >= 'a' && la0 <= 'z') {
						// line 104
						_tokenType = TT.Id;
						Id();
					} else
						goto error;
					break;
				}
				break;
			matchNum:
				{
					// line 103
					_tokenType = TT.Num;
					Num();
				}
				break;
			error:
				{
					// Line 109: ([^\$] | {..})
					la0 = LA0;
					if (la0 != -1) {
						Skip();
						// line 109
						_tokenType = TT.Unknown;
					} else
						// line 109
						return NoValue.Value;
				}
			} while (false);
			// line 111
			_current = new Token((int) _tokenType, _startIndex, this.InputPosition - _startIndex, _value);
			// line 112
			return _current;
		}
		static readonly HashSet<int> Id_set0 = NewSetOfRanges('0', '9', 'A', 'Z', '_', '_', 'a', 'z');

		private void Id()
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
			// line 118
			_value = this.CharSource.Slice(_startIndex, this.InputPosition - _startIndex).ToString();
		}

		private void Num()
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
			// line 124
			_value = double.Parse(this.CharSource.Slice(_startIndex, this.InputPosition - _startIndex).ToString(), CultureInfo.InvariantCulture);
		}
	}
}