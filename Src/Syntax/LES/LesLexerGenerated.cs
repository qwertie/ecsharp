using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.LLParserGenerator;
using Loyc.Syntax;
using Loyc.Syntax.Lexing;

namespace Loyc.Syntax.Les
{
	using TT = TokenType;

	public partial class LesLexer
	{
		public void Newline()
		{
			int la0;
			la0 = LA0;
			if (la0 == '\r') {
				Skip();
				la0 = LA0;
				if (la0 == '\n')
					Skip();
			} else
				Match('\n');
			_lineStartAt = InputPosition;
			_lineNumber++;
			_value = WhitespaceTag.Value;
		}
		public void Spaces()
		{
			int la0;
			Match('\t', ' ');
			for (;;) {
				la0 = LA0;
				if (la0 == '\t' || la0 == ' ')
					Skip();
				else
					break;
			}
			if (_lineStartAt == _startPosition) _indentLevel = MeasureIndent(_startPosition, InputPosition - _startPosition);
			_value = WhitespaceTag.Value;
		}
		public void SLComment()
		{
			int la0;
			Match('/');
			Match('/');
			for (;;) {
				la0 = LA0;
				if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
					Skip();
				else
					break;
			}
			_value = WhitespaceTag.Value;
		}
		public void MLComment()
		{
			int la0, la1;
			Match('/');
			Match('*');
			for (;;) {
				la0 = LA0;
				if (la0 == '*') {
					la1 = LA(1);
					if (la1 == -1 || la1 == '/')
						break;
					else
						MatchExcept();
				} else if (la0 == -1)
					break;
				else if (la0 == '/') {
					la1 = LA(1);
					if (la1 == '*')
						MLComment();
					else
						MatchExcept();
				} else
					MatchExcept();
			}
			Match('*');
			Match('/');
			_value = WhitespaceTag.Value;
		}
		public void SQString()
		{
			int la0;
			_parseNeeded = false;
			Match('\'');
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					Skip();
					MatchExcept();
					_parseNeeded = true;
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '\''))
					Skip();
				else
					break;
			}
			Match('\'');
			ParseCharValue();
		}
		public void DQString()
		{
			int la0, la1;
			_parseNeeded = false;
			la0 = LA0;
			if (la0 == '"') {
				Skip();
				for (;;) {
					la0 = LA0;
					if (la0 == '\\') {
						Skip();
						MatchExcept();
						_parseNeeded = true;
					} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '"'))
						Skip();
					else
						break;
				}
				Match('"');
			} else {
				_style = NodeStyle.Alternate;;
				Match('#');
				Match('"');
				for (;;) {
					la0 = LA0;
					if (la0 == '"') {
						la1 = LA(1);
						if (la1 == '"') {
							Skip();
							Skip();
							_parseNeeded = true;
						} else
							break;
					} else if (la0 != -1)
						Skip();
					else
						break;
				}
				Match('"');
			}
			ParseStringValue(false);
		}
		public void TQString()
		{
			int la0, la1, la2, la3;
			_parseNeeded = false; _style = NodeStyle.Alternate;
			Match('"');
			Match('"');
			Match('"');
			for (;;) {
				la0 = LA0;
				if (la0 == '"') {
					la1 = LA(1);
					if (la1 == '"') {
						la2 = LA(2);
						if (la2 == '"') {
							la3 = LA(3);
							if (la3 == '"') {
								Skip();
								Skip();
								Skip();
								Skip();
								_parseNeeded = true;
							} else
								Skip();
						} else if (la2 != -1)
							Skip();
						else
							break;
					} else if (la1 != -1)
						Skip();
					else
						break;
				} else if (la0 != -1)
					Skip();
				else
					break;
			}
			Match('"');
			Match('"');
			Match('"');
			ParseStringValue(true);
		}
		public void BQString()
		{
			BQString2();
			ParseStringValue(false);
		}
		private void BQString2()
		{
			int la0;
			_parseNeeded = false;
			Match('`');
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
		private void IdExtLetter()
		{
			Check(char.IsLetter((char) LA0), "char.IsLetter((char) LA0)");
			MatchRange('', '￼');
		}
		static readonly IntSet NormalId_set0 = IntSet.Parse("[#A-Z_a-z]");
		static readonly IntSet NormalId_set1 = IntSet.Parse("[#'0-9A-Z_a-z]");
		public void NormalId()
		{
			int la0;
			la0 = LA0;
			if (NormalId_set0.Contains(la0))
				Skip();
			else
				IdExtLetter();
			for (;;) {
				la0 = LA0;
				if (NormalId_set1.Contains(la0))
					Skip();
				else if (la0 >= '' && la0 <= '￼')
					IdExtLetter();
				else
					break;
			}
		}
		private void CommentStart()
		{
			Match('/');
			Match('*', '/');
		}
		private bool Is_CommentStart()
		{
			using (new SavedPosition(this)) {
				if (!TryMatch('/'))
					return false;
				if (!TryMatch('*', '/'))
					return false;
			}
			return true;
		}
		static readonly IntSet FancyId_set0 = IntSet.Parse("[!#-'*+\\--:<-?A-Z\\\\^_a-z|~]");
		public void FancyId()
		{
			int la0;
			la0 = LA0;
			if (la0 == '`')
				BQString2();
			else {
				la0 = LA0;
				if (FancyId_set0.Contains(la0)) {
					Check(!Is_CommentStart(), "CommentStart");
					Skip();
				} else
					IdExtLetter();
				for (;;) {
					la0 = LA0;
					if (FancyId_set0.Contains(la0)) {
						if (!Is_CommentStart())
							Skip();
						else
							break;
					} else if (la0 >= '' && la0 <= '￼') {
						if (char.IsLetter((char) LA0))
							IdExtLetter();
						else
							break;
					} else
						break;
				}
			}
		}
		public void Symbol()
		{
			_parseNeeded = false;
			Match('@');
			Match('@');
			FancyId();
			ParseSymbolValue();
		}
		static readonly IntSet Id_set0 = IntSet.Parse("(35, 65..90, 95, 97..122, 128..65532)");
		private void Id()
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
		private void Comma()
		{
			Skip();
			_type = TT.Comma;
			_value = _Comma;
		}
		private void Semicolon()
		{
			Skip();
			_type = TT.Semicolon;
			_value = _Semicolon;
		}
		private void At()
		{
			Skip();
			_type = TT.At;
			_value = _At;
		}
		private void Operator()
		{
			Check(!Is_CommentStart(), "CommentStart");
			Skip();
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
				case '^':
				case '|':
				case '~':
					{
						if (!Is_CommentStart())
							Skip();
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
		private void BackslashOp()
		{
			int la0;
			Skip();
			la0 = LA0;
			if (la0 == '`')
				FancyId();
			else if (FancyId_set0.Contains(la0)) {
				if (!Is_CommentStart())
					FancyId();
			} else if (la0 >= '' && la0 <= '￼') {
				if (char.IsLetter((char) LA0))
					FancyId();
			}
			ParseBackslashOp();
		}
		public void LParen()
		{
			Match('(');
		}
		public void RParen()
		{
			Match(')');
		}
		public void LBrack()
		{
			Match('[');
		}
		public void RBrack()
		{
			Match(']');
		}
		public void LBrace()
		{
			Match('{');
		}
		public void RBrace()
		{
			Match('}');
		}
		private void DecDigits()
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
		static readonly IntSet HexDigits_set0 = IntSet.Parse("[0-9A-Fa-f]");
		private void HexDigits()
		{
			int la0, la1;
			Skip();
			for (;;) {
				la0 = LA0;
				if (HexDigits_set0.Contains(la0))
					Skip();
				else
					break;
			}
			for (;;) {
				la0 = LA0;
				if (la0 == '_') {
					la1 = LA(1);
					if (HexDigits_set0.Contains(la1)) {
						Skip();
						Skip();
						for (;;) {
							la0 = LA0;
							if (HexDigits_set0.Contains(la0))
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
		private void BinDigits()
		{
			int la0, la1;
			Skip();
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
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '1') {
						Skip();
						Skip();
						for (;;) {
							la0 = LA0;
							if (la0 >= '0' && la0 <= '1')
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
		private void DecNumber()
		{
			int la0, la1;
			_numberBase = 10;
			la0 = LA0;
			if (la0 == '.') {
				_isFloat = true;
				Skip();
				DecDigits();
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
		private void HexNumber()
		{
			int la0, la1;
			_numberBase = 16; _style = NodeStyle.Alternate;
			Skip();
			Skip();
			la0 = LA0;
			if (HexDigits_set0.Contains(la0))
				HexDigits();
			la0 = LA0;
			if (la0 == '.') {
				la1 = LA(1);
				if (HexDigits_set0.Contains(la1)) {
					_isFloat = true;
					Skip();
					HexDigits();
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
		private void BinNumber()
		{
			int la0, la1;
			_numberBase = 2; _style = NodeStyle.Alternate2;
			Skip();
			Skip();
			la0 = LA0;
			if (la0 >= '0' && la0 <= '1')
				BinDigits();
			la0 = LA0;
			if (la0 == '.') {
				la1 = LA(1);
				if (la1 >= '0' && la1 <= '1') {
					_isFloat = true;
					Skip();
					BinDigits();
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
		public void Number()
		{
			int la0;
			_isFloat = false;
			_isNegative = false;
			la0 = LA0;
			if (la0 == '-') {
				Skip();
				_isNegative = true;
			}
			_typeSuffix = GSymbol.Get("");
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
					_typeSuffix=_F; _isFloat=true;
				}
				break;
			case 'D':
			case 'd':
				{
					Skip();
					_typeSuffix=_D; _isFloat=true;
				}
				break;
			case 'M':
			case 'm':
				{
					Skip();
					_typeSuffix=_M; _isFloat=true;
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
		public void Token()
		{
			int la1, la2;
			do {
				switch (LA0) {
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
								if (!Is_CommentStart())
									goto match2;
								else
									goto match21;
							} else if (la2 >= '' && la2 <= '￼') {
								if (char.IsLetter((char) LA0))
									goto match2;
								else
									goto match21;
							} else
								goto match21;
						} else if (la1 == '`')
							goto match3;
						else if (FancyId_set0.Contains(la1)) {
							if (!Is_CommentStart())
								goto match3;
							else
								goto match21;
						} else if (la1 >= '' && la1 <= '￼') {
							if (char.IsLetter((char) LA0))
								goto match3;
							else
								goto match21;
						} else
							goto match21;
					}
					break;
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
				case '/':
					{
						if (!Is_CommentStart()) {
							la1 = LA(1);
							if (la1 == '/')
								goto match6;
							else if (la1 == '*')
								goto match7;
							else
								Operator();
						} else {
							la1 = LA(1);
							if (la1 == '/')
								goto match6;
							else
								goto match7;
						}
					}
					break;
				case '-':
					{
						if (!Is_CommentStart()) {
							la1 = LA(1);
							if (la1 == '0')
								goto match8;
							else if (la1 == '.') {
								la2 = LA(2);
								if (la2 >= '0' && la2 <= '9')
									goto match8;
								else
									Operator();
							} else if (la1 >= '1' && la1 <= '9')
								goto match8;
							else
								Operator();
						} else
							goto match8;
					}
					break;
				case '0':
					goto match8;
				case '.':
					{
						if (!Is_CommentStart()) {
							la1 = LA(1);
							if (la1 >= '0' && la1 <= '9')
								goto match8;
							else
								Operator();
						} else
							goto match8;
					}
					break;
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
					goto match8;
				case '"':
					{
						la1 = LA(1);
						if (la1 == '"') {
							la2 = LA(2);
							if (la2 == '"') {
								_type = TT.String;
								TQString();
							} else
								goto match10;
						} else
							goto match10;
					}
					break;
				case '\'':
					{
						_type = TT.SQString;
						SQString();
					}
					break;
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
						LParen();
					}
					break;
				case '[':
					{
						_type = TT.LBrack;
						LBrack();
					}
					break;
				case '{':
					{
						_type = TT.LBrace;
						LBrace();
					}
					break;
				case ')':
					{
						_type = TT.RParen;
						RParen();
					}
					break;
				case ']':
					{
						_type = TT.RBrack;
						RBrack();
					}
					break;
				case '}':
					{
						_type = TT.RBrace;
						RBrace();
					}
					break;
				case '\\':
					BackslashOp();
					break;
				case '!':
				case '$':
				case '%':
				case '&':
				case '*':
				case '+':
				case ':':
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
					goto match3;
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
			match6:
				{
					_type = TT.SLComment;
					SLComment();
				}
				break;
			match7:
				{
					_type = TT.MLComment;
					MLComment();
				}
				break;
			match8:
				{
					_type = TT.Number;
					Number();
				}
				break;
			match10:
				{
					_type = TT.String;
					DQString();
				}
				break;
			match21:
				{
					_type = TT.At;
					At();
				}
			} while (false);
		}
		public void Shebang()
		{
			int la0;
			Match('#');
			Match('!');
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
		Symbol _Comma = GSymbol.Get("#,");
		Symbol _Semicolon = GSymbol.Get("#;");
		Symbol _At = GSymbol.Get("#@");
	}
}