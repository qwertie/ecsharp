// Generated from LexerSourceTest.ecs by LeMP custom tool. LLLPG version: 1.3.0.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax.Lexing;
using Loyc.Collections.Impl;
using Loyc.MiniTest;
namespace Loyc.Syntax.Tests
{
	using TT = CalcTokenType;
	[TestFixture] public class LexerSourceTests_Calculator : TestHelpers
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
		[Test] public void SimpleTests()
		{
			ExpectList(Lex("2"), T(TT.Num, 2d));
			ExpectList(Lex("25"), T(TT.Num, 25d));
			ExpectList(Lex("2.5"), T(TT.Num, 2.5));
			ExpectList(Lex(".25"), T(TT.Num, 0.25));
			ExpectList(Lex("x"), T(TT.Id, "x"));
			ExpectList(Lex("Foo_7"), T(TT.Id, "Foo_7"));
		}
		[Test] public void MoreTests()
		{
			ExpectList(Lex("x *+ y"), T(TT.Id, "x"), T(TT.Mul), T(TT.Add), T(TT.Id, "y"));
			ExpectList(Lex(" 20 ; 40 - 5/0.25"), T(TT.Num, 20d), T(TT.Semicolon), T(TT.Num, 40d), T(TT.Sub), T(TT.Num, 5d), T(TT.Div), T(TT.Num, 0.25));
			ExpectList(Lex("  (the end)  "), T(TT.LParen), T(TT.Id, "the"), T(TT.Id, "end"), T(TT.RParen));
		}
	}
	public enum CalcTokenType
	{
		EOF = TokenKind.Spaces, Id = TokenKind.Id, Num = TokenKind.Number, Shr = TokenKind.Operator + 1, Shl = TokenKind.Operator + 2, Assign = TokenKind.Assignment + 1, GT = TokenKind.Operator + 3, LT = TokenKind.Operator + 4, Exp = TokenKind.Operator + 5, Mul = TokenKind.Operator + 6, Div = TokenKind.Operator + 7, Add = TokenKind.Operator + 8, Sub = TokenKind.Operator + 9, Semicolon = TokenKind.Operator + 10, LParen = TokenKind.Operator + 11, RParen = TokenKind.Operator + 12, LBrace, RBrace, Unknown
	}
	partial class CalculatorLexer : EnumeratorBase<Token>
	{
		public LexerSource Src
		{
			get;
			set;
		}
		public CalculatorLexer(string text, string fileName = "")
		{
			Src = (LexerSource) text;
		}
		public CalculatorLexer(ICharSource text, string fileName = "")
		{
			Src = new LexerSource(text);
		}
		TT _tokenType;
		int _startIndex;
		object _value;
		public override bool MoveNext()
		{
			int la0, la1;
			// Line 97: ([\t ])*
			for (;;) {
				la0 = Src.LA0;
				if (la0 == '\t' || la0 == ' ')
					Src.Skip();
				else
					break;
			}
			_startIndex = Src.InputPosition;
			_value = null;
			// Line 100: ( (Num | Id | [.] [n] [a] [n] | [.] [i] [n] [f]) | ([>] [>] / [<] [<] / [=] / [>] / [<] / [\^] / [*] / [/] / [+] / [\-] / [;] / [(] / [)]) )
			do {
				la0 = Src.LA0;
				switch (la0) {
				case '.':
					{
						la1 = Src.LA(1);
						if (la1 >= '0' && la1 <= '9')
							goto matchNum;
						else if (la1 == 'n') {
							#line 102 "LexerSourceTest.ecs"
							_tokenType = TT.Num;
							#line default
							Src.Skip();
							Src.Skip();
							Src.Match('a');
							Src.Match('n');
							#line 102 "LexerSourceTest.ecs"
							_value = double.NaN;
							#line default
						} else if (la1 == 'i') {
							#line 103 "LexerSourceTest.ecs"
							_tokenType = TT.Num;
							#line default
							Src.Skip();
							Src.Skip();
							Src.Match('n');
							Src.Match('f');
							#line 103 "LexerSourceTest.ecs"
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
						la1 = Src.LA(1);
						if (la1 == '>') {
							Src.Skip();
							Src.Skip();
							#line 126 "LexerSourceTest.ecs"
							_tokenType = TT.Shr;
							#line default
						} else {
							Src.Skip();
							#line 126 "LexerSourceTest.ecs"
							_tokenType = TT.GT;
							#line default
						}
					}
					break;
				case '<':
					{
						la1 = Src.LA(1);
						if (la1 == '<') {
							Src.Skip();
							Src.Skip();
							#line 126 "LexerSourceTest.ecs"
							_tokenType = TT.Shl;
							#line default
						} else {
							Src.Skip();
							#line 126 "LexerSourceTest.ecs"
							_tokenType = TT.LT;
							#line default
						}
					}
					break;
				case '=':
					{
						Src.Skip();
						#line 126 "LexerSourceTest.ecs"
						_tokenType = TT.Assign;
						#line default
					}
					break;
				case '^':
					{
						Src.Skip();
						#line 126 "LexerSourceTest.ecs"
						_tokenType = TT.Exp;
						#line default
					}
					break;
				case '*':
					{
						Src.Skip();
						#line 126 "LexerSourceTest.ecs"
						_tokenType = TT.Mul;
						#line default
					}
					break;
				case '/':
					{
						Src.Skip();
						#line 126 "LexerSourceTest.ecs"
						_tokenType = TT.Div;
						#line default
					}
					break;
				case '+':
					{
						Src.Skip();
						#line 126 "LexerSourceTest.ecs"
						_tokenType = TT.Add;
						#line default
					}
					break;
				case '-':
					{
						Src.Skip();
						#line 126 "LexerSourceTest.ecs"
						_tokenType = TT.Sub;
						#line default
					}
					break;
				case ';':
					{
						Src.Skip();
						#line 126 "LexerSourceTest.ecs"
						_tokenType = TT.Semicolon;
						#line default
					}
					break;
				case '(':
					{
						Src.Skip();
						#line 126 "LexerSourceTest.ecs"
						_tokenType = TT.LParen;
						#line default
					}
					break;
				case ')':
					{
						Src.Skip();
						#line 126 "LexerSourceTest.ecs"
						_tokenType = TT.RParen;
						#line default
					}
					break;
				default:
					if (la0 >= 'A' && la0 <= 'Z' || la0 == '_' || la0 >= 'a' && la0 <= 'z') {
						#line 101 "LexerSourceTest.ecs"
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
					#line 100 "LexerSourceTest.ecs"
					_tokenType = TT.Num;
					#line default
					Num();
				}
				break;
			error:
				{
					#line 106 "LexerSourceTest.ecs"
					_tokenType = TT.EOF;
					#line default
					// Line 106: ([^\$])?
					la0 = Src.LA0;
					if (la0 != -1) {
						Src.Skip();
						#line 106 "LexerSourceTest.ecs"
						_tokenType = TT.Unknown;
						#line default
					}
				}
			} while (false);
			Current = new Token((int) _tokenType, _startIndex, Src.InputPosition - _startIndex, _value);
			return _tokenType != TT.EOF;
		}
		static readonly HashSet<int> Id_set0 = LexerSource.NewSetOfRanges('0', '9', 'A', 'Z', '_', '_', 'a', 'z');
		void Id()
		{
			int la0;
			Src.Skip();
			// Line 114: ([0-9A-Z_a-z])*
			for (;;) {
				la0 = Src.LA0;
				if (Id_set0.Contains(la0))
					Src.Skip();
				else
					break;
			}
			#line 115 "LexerSourceTest.ecs"
			_value = Src.CharSource.Slice(_startIndex, Src.InputPosition - _startIndex).ToString();
			#line default
		}
		void Num()
		{
			int la0, la1;
			int dot = 0;
			// Line 118: ([.])?
			la0 = Src.LA0;
			if (la0 == '.')
				dot = Src.MatchAny();
			Src.MatchRange('0', '9');
			// Line 119: ([0-9])*
			for (;;) {
				la0 = Src.LA0;
				if (la0 >= '0' && la0 <= '9')
					Src.Skip();
				else
					break;
			}
			// Line 120: (&{dot == 0} [.] [0-9] ([0-9])*)?
			la0 = Src.LA0;
			if (la0 == '.') {
				if (dot == 0) {
					la1 = Src.LA(1);
					if (la1 >= '0' && la1 <= '9') {
						Src.Skip();
						Src.Skip();
						// Line 120: ([0-9])*
						for (;;) {
							la0 = Src.LA0;
							if (la0 >= '0' && la0 <= '9')
								Src.Skip();
							else
								break;
						}
					}
				}
			}
			#line 121 "LexerSourceTest.ecs"
			_value = double.Parse(Src.CharSource.Slice(_startIndex, Src.InputPosition - _startIndex).ToString());
			#line default
		}
	}
}
