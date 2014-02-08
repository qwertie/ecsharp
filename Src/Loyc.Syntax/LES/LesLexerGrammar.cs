// Generated from LesLexerGrammar.les by LLLPG custom tool. LLLPG version: 1.0.0.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// --no-out-header       Suppress this message
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using Loyc.Syntax.Lexing;
namespace Loyc.Syntax.Les
{
	using TT = TokenType;
	public partial class LesLexer
	{
		new void Newline()
		{
			base.Newline();
			_value = WhitespaceTag.Value;
		}
		static readonly Symbol _Comma = GSymbol.Get("#,");
		static readonly Symbol _Semicolon = GSymbol.Get("#;");
		static readonly Symbol _Colon = GSymbol.Get("#:");
		void DotIndent()
		{
			int la0, la1;
			Skip();
			Skip();
			for (;;) {
				la0 = LA0;
				if (la0 == '\t' || la0 == ' ')
					Skip();
				else
					break;
			}
			for (;;) {
				la0 = LA0;
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 == '\t' || la1 == ' ') {
						Skip();
						Skip();
						for (;;) {
							la0 = LA0;
							if (la0 == '\t' || la0 == ' ')
								Skip();
							else
								break;
						}
					} else
						break;
				} else
					break;
			}
			_type = TT.Spaces;
			_indentLevel = MeasureIndent(_indent = CharSource.Slice(_startPosition, InputPosition - _startPosition));
			_value = WhitespaceTag.Value;
		}
		void Spaces()
		{
			int la0;
			Skip();
			for (;;) {
				la0 = LA0;
				if (la0 == '\t' || la0 == ' ')
					Skip();
				else
					break;
			}
			if ((_lineStartAt == _startPosition))
				_indentLevel = MeasureIndent(_indent = CharSource.Slice(_startPosition, InputPosition - _startPosition));
			_value = WhitespaceTag.Value;
		}
		void SLComment()
		{
			int la0;
			Skip();
			Skip();
			for (;;) {
				la0 = LA0;
				if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
					Skip();
				else
					break;
			}
			_value = WhitespaceTag.Value;
		}
		void MLComment()
		{
			int la1;
			Skip();
			Skip();
			for (;;) {
				switch (LA0) {
				case '*':
					{
						la1 = LA(1);
						if (la1 == -1 || la1 == '/')
							goto stop;
						else
							Skip();
					}
					break;
				case -1:
					goto stop;
				case '/':
					{
						la1 = LA(1);
						if (la1 == '*')
							MLComment();
						else
							Skip();
					}
					break;
				case '\n':
				case '\r':
					Newline();
					break;
				default:
					Skip();
					break;
				}
			}
		 stop:;
			Match('*');
			Match('/');
			_value = WhitespaceTag.Value;
		}
		void DecDigits()
		{
			int la0, la1;
			MatchRange('0', '9');
			for (;;) {
				la0 = LA0;
				if (la0 >= '0' && la0 <= '9')
					Skip();
				else
					break;
			}
			for (;;) {
				la0 = LA0;
				if (la0 == '_') {
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
					} else
						break;
				} else
					break;
			}
		}
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
				if (HexDigit_set0.Contains(la0))
					{if (!Scan_HexDigit())
						return false;}
				else
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
							if (HexDigit_set0.Contains(la0))
								{if (!Scan_HexDigit())
									return false;}
							else
								break;
						}
					} else
						break;
				} else
					break;
			}
			return true;
		}
		void BinDigits()
		{
			int la0;
			MatchRange('0', '1');
			for (;;) {
				la0 = LA0;
				if (la0 >= '0' && la0 <= '1')
					Skip();
				else
					break;
			}
			for (;;) {
				la0 = LA0;
				if (la0 == '_') {
					Skip();
					MatchRange('0', '1');
					for (;;) {
						la0 = LA0;
						if (la0 >= '0' && la0 <= '1')
							Skip();
						else
							break;
					}
				} else
					break;
			}
		}
		void DecNumber()
		{
			int la0, la1;
			_numberBase = 10;
			la0 = LA0;
			if (la0 == '.') {
				Skip();
				DecDigits();
				_isFloat = true;
			} else {
				DecDigits();
				la0 = LA0;
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						_isFloat = true;
						Skip();
						DecDigits();
					}
				}
			}
			la0 = LA0;
			if (la0 == 'E' || la0 == 'e') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					_isFloat = true;
					Skip();
					la0 = LA0;
					if (la0 == '+' || la0 == '-')
						Skip();
					DecDigits();
				}
			}
		}
		void HexNumber()
		{
			int la0, la1;
			Skip();
			Skip();
			_numberBase = 16;
			la0 = LA0;
			if (HexDigit_set0.Contains(la0))
				HexDigits();
			la0 = LA0;
			if (la0 == '.') {
				la1 = LA(1);
				if (HexDigit_set0.Contains(la1)) {
					if (Try_HexNumber_Test0(1)) {
						Skip();
						_isFloat = true;
						HexDigits();
					}
				}
			}
			la0 = LA0;
			if (la0 == 'P' || la0 == 'p') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					_isFloat = true;
					Skip();
					la0 = LA0;
					if (la0 == '+' || la0 == '-')
						Skip();
					DecDigits();
				}
			}
		}
		void BinNumber()
		{
			int la0, la1;
			Skip();
			Skip();
			_numberBase = 2;
			la0 = LA0;
			if (la0 == '.') {
				Skip();
				DecDigits();
				_isFloat = true;
			} else {
				DecDigits();
				la0 = LA0;
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						_isFloat = true;
						Skip();
						DecDigits();
					}
				}
			}
			la0 = LA0;
			if (la0 == 'P' || la0 == 'p') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					_isFloat = true;
					Skip();
					la0 = LA0;
					if (la0 == '+' || la0 == '-')
						Skip();
					DecDigits();
				}
			}
		}
		void Number()
		{
			int la0;
			_isFloat = _isNegative = false;
			_typeSuffix = null;
			la0 = LA0;
			if (la0 == '-') {
				Skip();
				_isNegative = true;
			}
			la0 = LA0;
			if (la0 == '0') {
				switch (LA(1)) {
				case 'X':
				case 'x':
					HexNumber();
					break;
				case 'B':
				case 'b':
					BinNumber();
					break;
				default:
					DecNumber();
					break;
				}
			} else
				DecNumber();
			switch (LA0) {
			case 'F':
			case 'f':
				{
					Skip();
					_typeSuffix = _F;
					_isFloat = true;
				}
				break;
			case 'D':
			case 'd':
				{
					Skip();
					_typeSuffix = _D;
					_isFloat = true;
				}
				break;
			case 'M':
			case 'm':
				{
					Skip();
					_typeSuffix = _M;
					_isFloat = true;
				}
				break;
			case 'L':
			case 'l':
				{
					Skip();
					_typeSuffix = _L;
					la0 = LA0;
					if (la0 == 'U' || la0 == 'u') {
						Skip();
						_typeSuffix = _UL;
					}
				}
				break;
			case 'U':
			case 'u':
				{
					Skip();
					_typeSuffix = _U;
					la0 = LA0;
					if (la0 == 'L' || la0 == 'l') {
						Skip();
						_typeSuffix = _UL;
					}
				}
				break;
			}
			ParseNumberValue();
		}
		void SQString()
		{
			int la0, la1;
			_parseNeeded = false;
			Skip();
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					la1 = LA(1);
					if (la1 != -1) {
						Skip();
						Skip();
						_parseNeeded = true;
					} else
						break;
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '\''))
					Skip();
				else
					break;
			}
			la0 = LA0;
			if (la0 == '\'')
				Skip();
			else
				_parseNeeded = true;
			ParseSQStringValue();
		}
		void DQString()
		{
			int la0, la1;
			_parseNeeded = false;
			Skip();
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					la1 = LA(1);
					if (la1 != -1) {
						Skip();
						Skip();
						_parseNeeded = true;
					} else
						break;
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '"'))
					Skip();
				else
					break;
			}
			la0 = LA0;
			if (la0 == '"')
				Skip();
			else
				_parseNeeded = true;
			ParseStringValue(false);
		}
		void TQString()
		{
			int la0, la1, la2;
			_parseNeeded = true;
			_style = NodeStyle.Alternate;
			la0 = LA0;
			if (la0 == '"') {
				Skip();
				Match('"');
				Match('"');
				for (;;) {
					switch (LA0) {
					case '"':
						{
							la1 = LA(1);
							if (la1 == '"') {
								la2 = LA(2);
								if (la2 == -1 || la2 == '"')
									goto stop;
								else
									Skip();
							} else if (la1 == -1)
								goto stop;
							else
								Skip();
						}
						break;
					case -1:
						goto stop;
					case '\n':
					case '\r':
						Newline();
						break;
					default:
						Skip();
						break;
					}
				}
			 stop:;
				Match('"');
				Match('"');
				Match('"');
			} else {
				_style |= NodeStyle.Alternate2;
				Match('\'');
				Match('\'');
				Match('\'');
				for (;;) {
					switch (LA0) {
					case '\'':
						{
							la1 = LA(1);
							if (la1 == '\'') {
								la2 = LA(2);
								if (la2 == -1 || la2 == '\'')
									goto stop2;
								else
									Skip();
							} else if (la1 == -1)
								goto stop2;
							else
								Skip();
						}
						break;
					case -1:
						goto stop2;
					case '\n':
					case '\r':
						Newline();
						break;
					default:
						Skip();
						break;
					}
				}
			 stop2:;
				Match('\'');
				Match('\'');
				Match('\'');
			}
			ParseStringValue(true);
		}
		void BQString2()
		{
			int la0;
			_parseNeeded = false;
			Skip();
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					Skip();
					MatchExcept();
					_parseNeeded = true;
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '`'))
					Skip();
				else
					break;
			}
			Match('`');
		}
		void BQString()
		{
			BQString2();
			ParseBQStringValue();
		}
		void IdStartChar()
		{
			Skip();
		}
		void IdExtLetter()
		{
			Check(char.IsLetter((char) LA0), "@char.IsLetter($LA->@char)");
			MatchRange(128, 65532);
		}
		static readonly HashSet<int> NormalId_set0 = NewSetOfRanges('#', '#', 'A', 'Z', '_', '_', 'a', 'z');
		void NormalId()
		{
			int la0;
			la0 = LA0;
			if (NormalId_set0.Contains(la0))
				IdStartChar();
			else
				IdExtLetter();
			for (;;) {
				la0 = LA0;
				if (NormalId_set0.Contains(la0))
					IdStartChar();
				else if (la0 >= '0' && la0 <= '9')
					Skip();
				else if (la0 == '\'')
					Skip();
				else if (la0 >= 128 && la0 <= 65532) {
					la0 = LA0;
					if (char.IsLetter((char) la0))
						IdExtLetter();
					else
						break;
				} else
					break;
			}
		}
		static readonly HashSet<int> FancyId_set0 = NewSetOfRanges('!', '!', '#', '\'', '*', '+', '-', ':', '<', '?', 'A', 'Z', '\\', '\\', '^', '_', 'a', 'z', '|', '|', '~', '~');
		void FancyId()
		{
			int la0;
			la0 = LA0;
			if (la0 == '`')
				BQString2();
			else {
				la0 = LA0;
				if (FancyId_set0.Contains(la0)) {
					Check(!Try_Scan_CommentStart(0), "!(CommentStart)");
					LettersOrPunc();
				} else
					IdExtLetter();
				for (;;) {
					la0 = LA0;
					if (FancyId_set0.Contains(la0)) {
						if (!Try_Scan_CommentStart(0))
							LettersOrPunc();
						else
							break;
					} else if (la0 >= 128 && la0 <= 65532) {
						la0 = LA0;
						if (char.IsLetter((char) la0))
							IdExtLetter();
						else
							break;
					} else
						break;
				}
			}
		}
		void Symbol()
		{
			_parseNeeded = false;
			Skip();
			Skip();
			FancyId();
			ParseSymbolValue();
		}
		static readonly HashSet<int> Id_set0 = NewSetOfRanges('#', '#', 'A', 'Z', '_', '_', 'a', 'z', 128, 65532);
		void Id()
		{
			int la0;
			_parseNeeded = false;
			la0 = LA0;
			if (Id_set0.Contains(la0))
				NormalId();
			else {
				Match('@');
				FancyId();
				_parseNeeded = true;
			}
			ParseIdValue();
		}
		void LettersOrPunc()
		{
			Skip();
		}
		void OpChars()
		{
			Skip();
		}
		void Comma()
		{
			Skip();
			_value = _Comma;
		}
		void Semicolon()
		{
			Skip();
			_value = _Semicolon;
		}
		void Colon()
		{
			Skip();
			_value = _Colon;
		}
		void At()
		{
			Skip();
			_value = GSymbol.Empty;
		}
		void CommentStart()
		{
			Match('/');
			Match('*', '/');
		}
		bool Try_Scan_CommentStart(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Scan_CommentStart();
		}
		bool Scan_CommentStart()
		{
			if (!TryMatch('/'))
				return false;
			if (!TryMatch('*', '/'))
				return false;
			return true;
		}
		void Operator()
		{
			Check(!Try_Scan_CommentStart(0), "!(CommentStart)");
			OpChars();
			for (;;) {
				switch (LA0) {
				case '!':
				case '$':
				case '%':
				case '&':
				case '*':
				case '+':
				case '-':
				case '.':
				case '/':
				case ':':
				case '<':
				case '=':
				case '>':
				case '?':
				case '\\':
				case '^':
				case '|':
				case '~':
					{
						if (!Try_Scan_CommentStart(0))
							OpChars();
						else
							goto stop;
					}
					break;
				default:
					goto stop;
				}
			}
		 stop:;
			ParseNormalOp();
		}
		void BackslashOp()
		{
			int la0, la1;
			Skip();
			la0 = LA0;
			if (la0 == '`') {
				la1 = LA(1);
				if (!(la1 == -1 || la1 == '\n' || la1 == '\r'))
					FancyId();
			} else if (FancyId_set0.Contains(la0)) {
				if (!Try_Scan_CommentStart(0))
					FancyId();
			} else if (la0 >= 128 && la0 <= 65532) {
				la0 = LA0;
				if (char.IsLetter((char) la0))
					FancyId();
			}
			ParseBackslashOp();
		}
		void Shebang()
		{
			int la0;
			Skip();
			Skip();
			for (;;) {
				la0 = LA0;
				if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
					Skip();
				else
					break;
			}
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r')
				Newline();
		}
		static readonly HashSet<int> Token_set0 = NewSetOfRanges('A', 'Z', '_', '_', 'a', 'z', 128, 65532);
		void Token()
		{
			int la0, la1, la2;
			do {
				la0 = LA0;
				switch (la0) {
				case '#':
					{
						if (InputPosition == 0) {
							la1 = LA(1);
							if (la1 == '!') {
								_type = TT.Shebang;
								Shebang();
							} else
								goto match3;
						} else
							goto match3;
					}
					break;
				case '@':
					{
						la1 = LA(1);
						if (la1 == '@') {
							la2 = LA(2);
							if (la2 == '`')
								goto match2;
							else if (FancyId_set0.Contains(la2)) {
								if (!Try_Scan_CommentStart(2))
									goto match2;
								else
									goto match22;
							} else if (la2 >= 128 && la2 <= 65532) {
								la2 = LA(2);
								if (char.IsLetter((char) la2))
									goto match2;
								else
									goto match22;
							} else
								goto match22;
						} else if (la1 == '`') {
							la2 = LA(2);
							if (!(la2 == -1 || la2 == '\n' || la2 == '\r'))
								goto match3;
							else
								goto match22;
						} else if (FancyId_set0.Contains(la1)) {
							if (!Try_Scan_CommentStart(1))
								goto match3;
							else
								goto match22;
						} else if (la1 >= 128 && la1 <= 65532) {
							la1 = LA(1);
							if (char.IsLetter((char) la1))
								goto match3;
							else
								goto match22;
						} else
							goto match22;
					}
				case '\t':
				case ' ':
					{
						_type = TT.Spaces;
						Spaces();
					}
					break;
				case '\n':
				case '\r':
					{
						_type = TT.Newline;
						Newline();
					}
					break;
				case '.':
					{
						if (_startPosition == _lineStartAt) {
							if (!Try_Scan_CommentStart(0)) {
								la1 = LA(1);
								if (la1 == '\t' || la1 == ' ')
									DotIndent();
								else if (la1 >= '0' && la1 <= '9')
									goto match9;
								else
									Operator();
							} else {
								la1 = LA(1);
								if (la1 == '\t' || la1 == ' ')
									DotIndent();
								else if (la1 >= '0' && la1 <= '9')
									goto match9;
								else
									goto match26;
							}
						} else if (!Try_Scan_CommentStart(0)) {
							la1 = LA(1);
							if (la1 >= '0' && la1 <= '9')
								goto match9;
							else
								Operator();
						} else
							goto match9;
					}
					break;
				case '/':
					{
						if (!Try_Scan_CommentStart(0)) {
							la1 = LA(1);
							if (la1 == '/')
								goto match7;
							else if (la1 == '*') {
								la2 = LA(2);
								if (la2 != -1)
									goto match8;
								else
									Operator();
							} else
								Operator();
						} else {
							la1 = LA(1);
							if (la1 == '/')
								goto match7;
							else if (la1 == '*')
								goto match8;
							else
								goto match26;
						}
					}
					break;
				case '-':
					{
						if (!Try_Scan_CommentStart(0)) {
							la1 = LA(1);
							if (la1 == '0')
								goto match9;
							else if (la1 == '.') {
								la2 = LA(2);
								if (la2 >= '0' && la2 <= '9')
									goto match9;
								else
									Operator();
							} else if (la1 >= '1' && la1 <= '9')
								goto match9;
							else
								Operator();
						} else
							goto match9;
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
					goto match9;
				case '"':
					{
						la1 = LA(1);
						if (la1 == '"') {
							la2 = LA(2);
							if (la2 == '"')
								goto match10;
							else
								goto match11;
						} else
							goto match11;
					}
				case '\'':
					{
						la1 = LA(1);
						if (la1 == '\'') {
							la2 = LA(2);
							if (la2 == '\'')
								goto match10;
							else
								goto match12;
						} else
							goto match12;
					}
				case '`':
					{
						_type = TT.BQString;
						BQString();
					}
					break;
				case ',':
					{
						_type = TT.Comma;
						Comma();
					}
					break;
				case ';':
					{
						_type = TT.Semicolon;
						Semicolon();
					}
					break;
				case '(':
					{
						_type = TT.LParen;
						Skip();
					}
					break;
				case ')':
					{
						_type = TT.RParen;
						Skip();
					}
					break;
				case '[':
					{
						_type = TT.LBrack;
						Skip();
					}
					break;
				case ']':
					{
						_type = TT.RBrack;
						Skip();
					}
					break;
				case '{':
					{
						_type = TT.LBrace;
						Skip();
					}
					break;
				case '}':
					{
						_type = TT.RBrace;
						Skip();
					}
					break;
				case '\\':
					BackslashOp();
					break;
				case ':':
					{
						if (!Try_Scan_CommentStart(0))
							Operator();
						else {
							_type = TT.Colon;
							Colon();
						}
					}
					break;
				case '!':
				case '$':
				case '%':
				case '&':
				case '*':
				case '+':
				case '<':
				case '=':
				case '>':
				case '?':
				case '^':
				case '|':
				case '~':
					Operator();
					break;
				default:
					if (Token_set0.Contains(la0))
						goto match3;
					else
						goto match26;
				}
				break;
			match2:
				{
					_type = TT.Symbol;
					Symbol();
				}
				break;
			match3:
				{
					_type = TT.Id;
					Id();
				}
				break;
			match7:
				{
					_type = TT.SLComment;
					SLComment();
				}
				break;
			match8:
				{
					_type = TT.MLComment;
					MLComment();
				}
				break;
			match9:
				{
					_type = TT.Number;
					Number();
				}
				break;
			match10:
				{
					_type = TT.String;
					TQString();
				}
				break;
			match11:
				{
					_type = TT.String;
					DQString();
				}
				break;
			match12:
				{
					_type = TT.SQString;
					SQString();
				}
				break;
			match22:
				{
					_type = TT.At;
					At();
				}
				break;
			match26:
				{
					_value = null;
					la0 = LA0;
					if (la0 == -1) {
						Skip();
						_type = TT.EOF;
					} else {
						Skip();
						_type = TT.Unknown;
					}
				}
			} while (false);
		}
		public bool TDQStringLine()
		{
			int la0, la1, la2;
			for (;;) {
				switch (LA0) {
				case '\n':
				case '\r':
					goto stop;
				case '"':
					{
						la1 = LA(1);
						if (la1 == '"') {
							la2 = LA(2);
							if (la2 == -1 || la2 == '"')
								goto stop;
							else
								Skip();
						} else if (la1 == -1)
							goto stop;
						else
							Skip();
					}
					break;
				case -1:
					goto stop;
				default:
					Skip();
					break;
				}
			}
		 stop:;
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline();
				return false;
			} else {
				Match('"');
				Match('"');
				Match('"');
				return true;
			}
		}
		public bool TSQStringLine()
		{
			int la0, la1, la2;
			for (;;) {
				switch (LA0) {
				case '\n':
				case '\r':
					goto stop;
				case '\'':
					{
						la1 = LA(1);
						if (la1 == '\'') {
							la2 = LA(2);
							if (la2 == -1 || la2 == '\'')
								goto stop;
							else
								Skip();
						} else if (la1 == -1)
							goto stop;
						else
							Skip();
					}
					break;
				case -1:
					goto stop;
				default:
					Skip();
					break;
				}
			}
		 stop:;
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline();
				return false;
			} else {
				Match('\'');
				Match('\'');
				Match('\'');
				return true;
			}
		}
		public bool MLCommentLine(ref int nested)
		{
			int la0, la1;
			for (;;) {
				la0 = LA0;
				if (la0 == '*') {
					if (nested > 0) {
						la1 = LA(1);
						if (la1 == '/') {
							Skip();
							Skip();
							nested--;
						} else if (la1 != -1)
							goto match4;
						else
							break;
					} else {
						la1 = LA(1);
						if (la1 == '*')
							goto match4;
						else if (la1 == '/') {
							if (!Try_MLCommentLine_Test0(1))
								goto match4;
							else
								break;
						} else if (la1 != -1)
							goto match4;
						else
							break;
					}
				} else if (la0 == '/') {
					la1 = LA(1);
					if (la1 == '*') {
						Skip();
						Skip();
						nested++;
					} else
						Skip();
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
					Skip();
				else
					break;
				continue;
			match4:
				{
					Skip();
					Check(!Try_MLCommentLine_Test0(0), "!([/])");
				}
			}
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline();
				return false;
			} else {
				Match('*');
				Match('/');
				return true;
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
		}
		private bool Try_MLCommentLine_Test0(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return MLCommentLine_Test0();
		}
		private bool MLCommentLine_Test0()
		{
			if (!TryMatch('/'))
				return false;
			return true;
		}
	}
}
