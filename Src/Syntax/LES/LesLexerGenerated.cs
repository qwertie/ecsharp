using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Syntax;
using Loyc;

namespace Loyc.Syntax.Les
{
	using TT = TokenType;
	using Loyc.LLParserGenerator;

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
			_lineNumber++;
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
			ParseStringValue();
		}
		public void TQString()
		{
			int la0, la1, la2, la3;
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
			ParseStringValue();
		}
		public void BQString()
		{
			BQStringP();
			ParseStringValue();
		}
		private void BQStringP()
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
		static readonly IntSet OpChars_set0 = IntSet.Parse("[!%-&*-+.-/:<-?^|~]");
		private void OpChars()
		{
			Match(OpChars_set0);
			for (;;) {
				switch (LA0) {
				case '!':
				case '$':
				case '%':
				case '&':
				case '*':
				case '+':
				case '.':
				case '/':
				case ':':
				case '<':
				case '=':
				case '>':
				case '?':
				case '@':
				case '^':
				case '|':
				case '~':
					Skip();
					break;
				default:
					goto stop;
				}
			}
		stop:;
		}
		public void Operator()
		{
			OpChars();
			ParseOp();
		}
		private void IdExtLetter()
		{
			Check(char.IsLetter((char) LA(0)), "char.IsLetter((char) LA(0))");
			MatchRange('', '￼');
		}
		private void IdStart()
		{
			int la0;
			la0 = LA0;
			if (la0 >= 'A' && la0 <= 'Z' || la0 == '_' || la0 >= 'a' && la0 <= 'z')
				Skip();
			else
				IdExtLetter();
		}
		static readonly IntSet IdCont_set0 = IntSet.Parse("['0-9A-Z_a-z]");
		private void IdCont()
		{
			int la0;
			la0 = LA0;
			if (IdCont_set0.Contains(la0))
				Skip();
			else
				IdExtLetter();
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
				else if (la0 >= '' && la0 <= '￼') {
					if (char.IsLetter((char) LA(0)))
						IdExtLetter();
					else
						break;
				} else
					break;
			}
		}
		public void Symbol()
		{
			int la0;
			Match('\\');
			la0 = LA0;
			if (la0 == '`')
				BQString();
			else
				NormalId();
			ParseSymbolValue();
		}
		private void Id()
		{
			int la0;
			la0 = LA0;
			if (la0 == '\\') {
				Skip();
				la0 = LA0;
				if (la0 == '`')
					BQString();
				else
					NormalId();
				ParseIdValue();
			} else
				NormalId();
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
		public void OpenOf()
		{
			Match('.');
			Match('[');
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
			_numberBase = 2; _style = NodeStyle.UserFlag;
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
				case '\\':
					{
						_type = TT.Symbol;
						Symbol();
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
						la1 = LA(1);
						if (la1 == '/') {
							_type = TT.SLComment;
							SLComment();
						} else if (la1 == '*') {
							_type = TT.MLComment;
							MLComment();
						} else
							Operator();
					}
					break;
				case '-':
				case '0':
					goto match8;
				case '.':
					{
						la1 = LA(1);
						if (la1 >= '0' && la1 <= '9')
							goto match8;
						else if (la1 == '[') {
							_type = TT.OpenOf;
							OpenOf();
						} else
							Operator();
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
								goto match11;
						} else
							goto match11;
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
				case '!':
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
			match3:
				{
					_type = TT.Id;
					Id();
				}
				break;
			match8:
				{
					_type = TT.Number;
					Number();
				}
				break;
			match11:
				{
					_type = TT.String;
					DQString();
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
	}
}