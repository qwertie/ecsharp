using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.LLParserGenerator;
using Loyc;

namespace Ecs.Parser
{
	using LS = EcsLexerSymbols;

	public partial class EcsLexer
	{
		public void Newline()
		{
			int la0;
			la0 = LA(0);
			if (la0 == '\r') {
				Match('\r');
				la0 = LA(0);
				if (la0 == '\n')
					Match('\n');
			} else
				Match('\n');
			_allowPPAt = _inputPosition;
		}
		public void Spaces()
		{
			int la0;
			Match('\t', ' ');
			for (; ; ) {
				la0 = LA(0);
				if (la0 == '\t' || la0 == ' ')
					Match('\t', ' ');
				else
					break;
			}
			if (_allowPPAt == _startPosition) _allowPPAt = _inputPosition;
		}
		public void SLComment()
		{
			int la0;
			Match('/');
			Match('/');
			for (; ; ) {
				la0 = LA(0);
				if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
					MatchExcept('\n', '\r');
				else
					break;
			}
		}
		public void MLComment()
		{
			int la0, la1;
			Match('/');
			Match('*');
			for (; ; ) {
				la0 = LA(0);
				if (la0 == '*') {
					la1 = LA(1);
					if (la1 == -1 || la1 == '/')
						break;
					else
						MatchExcept();
				} else if (la0 == -1)
					break;
				else if (la0 == '/') {
					if (AllowNestedComments) {
						la1 = LA(1);
						if (la1 == '*')
							goto match1;
						else
							MatchExcept();
					} else
						MatchExcept();
				} else
					MatchExcept();
				continue;
			match1: {
					Check(AllowNestedComments);
					MLComment();
				}
			}
			Match('*');
			Match('/');
		}
		static readonly IntSet SQString_set0 = IntSet.Parse("[^\\$\\n\\r'\\\\]");
		public void SQString()
		{
			int la0;
			_parseNeeded = false;
			_verbatims = 0;
			Match('\'');
			for (; ; ) {
				la0 = LA(0);
				if (la0 == '\\') {
					Match('\\');
					MatchExcept();
					_parseNeeded = true;
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '\''))
					Match(SQString_set0);
				else
					break;
			}
			Match('\'');
			ParseCharValue();
		}
		static readonly IntSet DQString_set0 = IntSet.Parse("[^\\$\\n\\r\"\\\\]");
		public void DQString()
		{
			int la0, la1;
			_parseNeeded = false;
			la0 = LA(0);
			if (la0 == '"') {
				_verbatims = 0;
				Match('"');
				for (; ; ) {
					la0 = LA(0);
					if (la0 == '\\') {
						Match('\\');
						MatchExcept();
						_parseNeeded = true;
					} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '"'))
						Match(DQString_set0);
					else
						break;
				}
				Match('"');
			} else {
				_verbatims = 1;
				Match('@');
				la0 = LA(0);
				if (la0 == '@') {
					Match('@');
					_verbatims = 2;
				}
				Match('"');
				for (; ; ) {
					la0 = LA(0);
					if (la0 == '"') {
						la1 = LA(1);
						if (la1 == '"') {
							Match('"');
							Match('"');
							_parseNeeded = true;
						} else
							break;
					} else if (la0 == '\\') {
						la1 = LA(1);
						if (la1 == '(' || la1 == '{') {
							Match('\\');
							Match('(', '{');
							_parseNeeded = true;
						} else
							MatchExcept('"');
					} else if (la0 != -1)
						MatchExcept('"');
					else
						break;
				}
				Match('"');
			}
			ParseStringValue();
		}
		public void BQString()
		{
			_parseNeeded = false;
			BQStringN();
			ParseBQStringValue();
		}
		static readonly IntSet BQStringN_set0 = IntSet.Parse("[^\\$\\n\\r\\\\`]");
		public void BQStringN()
		{
			int la0;
			_verbatims = 0;
			Match('`');
			for (; ; ) {
				la0 = LA(0);
				if (la0 == '\\') {
					Match('\\');
					_parseNeeded = true;
					MatchExcept();
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '`'))
					Match(BQStringN_set0);
				else
					break;
			}
			Match('`');
		}
		public void BQStringV()
		{
			int la0, la1;
			_verbatims = 1;
			Match('`');
			for (; ; ) {
				la0 = LA(0);
				if (la0 == '`') {
					la1 = LA(1);
					if (la1 == '`')
						goto match1;
					else
						break;
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
					MatchExcept('\n', '\r', '`');
				else
					break;
				continue;
			match1: {
					Match('`');
					Match('`');
					_parseNeeded = true;
				}
			}
			Match('`');
		}
		public void Comma()
		{
			OnOneCharOperator(Match(','));
		}
		public void Colon()
		{
			OnOneCharOperator(Match(':'));
		}
		public void Semicolon()
		{
			OnOneCharOperator(Match(';'));
		}
		static readonly IntSet Operator_set0 = IntSet.Parse("[!%-&*-+<->^|]");
		static readonly IntSet Operator_set1 = IntSet.Parse("[!%-&*-+\\--/<-?\\\\^|~]");
		public void Operator()
		{
			int la0, la1, la2;
			do {
				la0 = LA(0);
				switch (la0) {
					case '>': {
							la1 = LA(1);
							if (la1 == '>') {
								la2 = LA(2);
								if (la2 == '=') {
									Match('>');
									Match('>');
									Match('=');
									_value = GSymbol.Get("#>>=");
								} else
									OnOneCharOperator(Match(Operator_set1));
							} else if (la1 == '=')
								goto match12;
							else
								OnOneCharOperator(Match(Operator_set1));
						}
						break;
					case '<': {
							la1 = LA(1);
							if (la1 == '<') {
								la2 = LA(2);
								if (la2 == '=') {
									Match('<');
									Match('<');
									Match('=');
									_value = GSymbol.Get("#<<=");
								} else
									OnOneCharOperator(Match(Operator_set1));
							} else if (la1 == '=')
								goto match12;
							else
								OnOneCharOperator(Match(Operator_set1));
						}
						break;
					case '&': {
							la1 = LA(1);
							if (la1 == '&') {
								Match('&');
								Match('&');
								_value = GSymbol.Get("#&&");
							} else if (la1 == '=')
								goto match12;
							else
								OnOneCharOperator(Match(Operator_set1));
						}
						break;
					case '+': {
							la1 = LA(1);
							if (la1 == '+') {
								Match('+');
								Match('+');
								_value = GSymbol.Get("#++");
							} else if (la1 == '=')
								goto match12;
							else
								OnOneCharOperator(Match(Operator_set1));
						}
						break;
					case '-': {
							la1 = LA(1);
							if (la1 == '-') {
								Match('-');
								Match('-');
								_value = GSymbol.Get("#--");
							} else if (la1 == '>') {
								Match('-');
								Match('>');
								_value = GSymbol.Get("#->");
							} else
								OnOneCharOperator(Match(Operator_set1));
						}
						break;
					case '|': {
							la1 = LA(1);
							if (la1 == '|') {
								Match('|');
								Match('|');
								_value = GSymbol.Get("#||");
							} else if (la1 == '=')
								goto match12;
							else
								OnOneCharOperator(Match(Operator_set1));
						}
						break;
					case '.': {
							la1 = LA(1);
							if (la1 == '.') {
								Match('.');
								Match('.');
								_value = GSymbol.Get("#..");
							} else
								OnOneCharOperator(Match(Operator_set1));
						}
						break;
					case '?': {
							la1 = LA(1);
							if (la1 == '?') {
								Match('?');
								Match('?');
								la0 = LA(0);
								if (la0 == '.') {
									Match('.');
									_value = GSymbol.Get("#??.");
								} else if (la0 == '=') {
									Match('=');
									_value = GSymbol.Get("#??=");
								}
							} else
								OnOneCharOperator(Match(Operator_set1));
						}
						break;
					case '=': {
							la1 = LA(1);
							if (la1 == '>') {
								Match('=');
								Match('>');
								_value = GSymbol.Get("#=>");
							} else if (la1 == '=') {
								la2 = LA(2);
								if (la2 == '>') {
									Match('=');
									Match('=');
									Match('>');
									_value = GSymbol.Get("#==>");
								} else
									goto match12;
							} else
								OnOneCharOperator(Match(Operator_set1));
						}
						break;
					case '!':
					case '%':
					case '*':
					case '^': {
							la1 = LA(1);
							if (la1 == '=')
								goto match12;
							else
								OnOneCharOperator(Match(Operator_set1));
						}
						break;
					default:
						OnOneCharOperator(Match(Operator_set1));
						break;
				}
				break;
			match12: {
					OnOperatorEquals(Match(Operator_set0));
					Match('=');
				}
			} while (false);
		}
		static readonly IntSet Id_set0 = IntSet.Parse("[0-9A-Fa-f]");
		static readonly IntSet Id_set1 = IntSet.Parse("(39, 48..57, 65..90, 95..122, 128..65532)");
		static readonly IntSet Id_set2 = IntSet.Parse("(39, 48..57, 65..90, 92, 95, 97..122, 128..65532)");
		static readonly IntSet Id_set3 = IntSet.Parse("(39, 48..57, 65..90, 92, 95..122, 128..65532)");
		static readonly IntSet Id_set4 = IntSet.Parse("(65..90, 92, 95, 97..122, 128..65532)");
		public void Id()
		{
			int la0, la1, la2;
			_parseNeeded = true;
			do {
				la0 = LA(0);
				if (la0 == '@') {
					la1 = LA(1);
					if (Id_set3.Contains(la1)) {
						Match('@');
						SpecialIdV();
					} else
						goto match3b;
				} else if (la0 == '#') {
					la1 = LA(1);
					if (la1 == '@') {
						la2 = LA(2);
						if (Id_set3.Contains(la2))
							goto match2b;
						else
							goto match3b;
					} else
						goto match3b;
				} else if (Id_set4.Contains(la0)) {
					IdStart();
					for (; ; ) {
						la0 = LA(0);
						if (Id_set2.Contains(la0))
							IdCont();
						else
							break;
					}
					_parseNeeded = false;
				} else
					Match('$');
				break;
			match2b: {
					Match('#');
					Match('@');
					SpecialIdV();
				}
				break;
			match3b: {
					la0 = LA(0);
					if (la0 == '@')
						Match('@');
					Match('#');
					do {
						la0 = LA(0);
						switch (la0) {
							case '\\': {
									la1 = LA(1);
									if (la1 == 'u') {
										la2 = LA(2);
										if (Id_set0.Contains(la2))
											SpecialId();
										else
											Operator();
									} else
										Operator();
								}
								break;
							case '<': {
									la1 = LA(1);
									if (la1 == '<') {
										la2 = LA(2);
										if (la2 == '=')
											goto match2;
										else
											goto match3;
									} else
										Operator();
								}
								break;
							case '>': {
									la1 = LA(1);
									if (la1 == '>') {
										la2 = LA(2);
										if (la2 == '=')
											goto match4;
										else
											goto match5;
									} else
										Operator();
								}
								break;
							case '*': {
									la1 = LA(1);
									if (la1 == '*')
										goto match6;
									else
										Operator();
								}
								break;
							case '!':
							case '%':
							case '&':
							case '+':
							case '-':
							case '.':
							case '/':
							case '=':
							case '?':
							case '^':
							case '|':
							case '~':
								Operator();
								break;
							case ',':
								Comma();
								break;
							case ':':
								Colon();
								break;
							case ';':
								Semicolon();
								break;
							case '$':
								Match('$');
								break;
							default:
								if (Id_set1.Contains(la0))
									SpecialId();
								break;
						}
						break;
					match2: {
							Match('<');
							Match('<');
							Match('=');
						}
						break;
					match3: {
							Match('<');
							Match('<');
						}
						break;
					match4: {
							Match('>');
							Match('>');
							Match('=');
						}
						break;
					match5: {
							Match('>');
							Match('>');
						}
						break;
					match6: {
							Match('*');
							Match('*');
						}
					} while (false);
				}
			} while (false);
			bool isPPLine = ParseIdValue();
			if (isPPLine) {
				Check(isPPLine);
				int ppTextStart = _inputPosition;
				for (; ; ) {
					la0 = LA(0);
					if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
						MatchExcept('\n', '\r');
					else
						break;
				}
				_value = _source.Substring(ppTextStart, _inputPosition - ppTextStart);
			}
		}
		public void IdSpecial()
		{
			int la0;
			la0 = LA(0);
			if (la0 == '\\') {
				Match('\\');
				Match('u');
				Match(Id_set0);
				Match(Id_set0);
				Match(Id_set0);
				Match(Id_set0);
				_parseNeeded = true;
			} else {
				Check(char.IsLetter((char)LA(0)));
				MatchRange('', '￼');
			}
		}
		static readonly IntSet IdStart_set0 = IntSet.Parse("[A-Z_a-z]");
		public void IdStart()
		{
			int la0;
			la0 = LA(0);
			if (la0 >= 'A' && la0 <= 'Z' || la0 == '_' || la0 >= 'a' && la0 <= 'z')
				Match(IdStart_set0);
			else
				IdSpecial();
		}
		static readonly IntSet IdCont_set0 = IntSet.Parse("['0-9A-Z_a-z]");
		public void IdCont()
		{
			int la0;
			la0 = LA(0);
			if (IdCont_set0.Contains(la0))
				Match(IdCont_set0);
			else
				IdSpecial();
		}
		public void SpecialId()
		{
			int la0;
			la0 = LA(0);
			if (la0 == '`')
				BQStringN();
			else {
				IdCont();
				for (; ; ) {
					la0 = LA(0);
					if (Id_set2.Contains(la0))
						IdCont();
					else
						break;
				}
			}
		}
		public void SpecialIdV()
		{
			int la0;
			la0 = LA(0);
			if (la0 == '`')
				BQStringV();
			else {
				IdCont();
				for (; ; ) {
					la0 = LA(0);
					if (Id_set2.Contains(la0))
						IdCont();
					else
						break;
				}
			}
		}
		public void Symbol()
		{
			Match('$');
			_verbatims = -1;
			SpecialId();
			ParseSymbolValue();
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
		public void LCodeQuote()
		{
			Match('@');
			Match('(', '[', '{');
		}
		public void LCodeQuoteS()
		{
			Match('@');
			Match('@');
			Match('(', '[', '{');
		}
		public void DecDigits()
		{
			int la0, la1;
			MatchRange('0', '9');
			for (; ; ) {
				la0 = LA(0);
				if (la0 >= '0' && la0 <= '9')
					MatchRange('0', '9');
				else
					break;
			}
			for (; ; ) {
				la0 = LA(0);
				if (la0 == '_') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						Match('_');
						MatchRange('0', '9');
						for (; ; ) {
							la0 = LA(0);
							if (la0 >= '0' && la0 <= '9')
								MatchRange('0', '9');
							else
								break;
						}
					} else
						break;
				} else
					break;
			}
		}
		public void HexDigits()
		{
			int la0, la1;
			Match(Id_set0);
			for (; ; ) {
				la0 = LA(0);
				if (Id_set0.Contains(la0))
					Match(Id_set0);
				else
					break;
			}
			for (; ; ) {
				la0 = LA(0);
				if (la0 == '_') {
					la1 = LA(1);
					if (Id_set0.Contains(la1)) {
						Match('_');
						Match(Id_set0);
						for (; ; ) {
							la0 = LA(0);
							if (Id_set0.Contains(la0))
								Match(Id_set0);
							else
								break;
						}
					} else
						break;
				} else
					break;
			}
		}
		public void BinDigits()
		{
			int la0, la1;
			MatchRange('0', '1');
			for (; ; ) {
				la0 = LA(0);
				if (la0 >= '0' && la0 <= '1')
					MatchRange('0', '1');
				else
					break;
			}
			for (; ; ) {
				la0 = LA(0);
				if (la0 == '_') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '1') {
						Match('_');
						MatchRange('0', '1');
						for (; ; ) {
							la0 = LA(0);
							if (la0 >= '0' && la0 <= '1')
								MatchRange('0', '1');
							else
								break;
						}
					} else
						break;
				} else
					break;
			}
		}
		public void DecNumber()
		{
			int la0, la1;
			_numberBase = 10;
			la0 = LA(0);
			if (la0 == '.') {
				_isFloat = true;
				Match('.');
				DecDigits();
			} else {
				DecDigits();
				la0 = LA(0);
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						_isFloat = true;
						Match('.');
						DecDigits();
					}
				}
			}
			la0 = LA(0);
			if (la0 == 'E' || la0 == 'e') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					_isFloat = true;
					Match('E', 'e');
					la0 = LA(0);
					if (la0 == '+' || la0 == '-')
						Match('+', '-');
					DecDigits();
				}
			}
		}
		public void HexNumber()
		{
			int la0, la1;
			_numberBase = 16;
			Match('0');
			Match('X', 'x');
			la0 = LA(0);
			if (Id_set0.Contains(la0))
				HexDigits();
			la0 = LA(0);
			if (la0 == '.') {
				la1 = LA(1);
				if (Id_set0.Contains(la1)) {
					_isFloat = true;
					Match('.');
					HexDigits();
				}
			}
			la0 = LA(0);
			if (la0 == 'P' || la0 == 'p') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					_isFloat = true;
					Match('P', 'p');
					la0 = LA(0);
					if (la0 == '+' || la0 == '-')
						Match('+', '-');
					DecDigits();
				}
			}
		}
		public void BinNumber()
		{
			int la0, la1;
			_numberBase = 2;
			Match('0');
			Match('B', 'b');
			la0 = LA(0);
			if (la0 >= '0' && la0 <= '1')
				BinDigits();
			la0 = LA(0);
			if (la0 == '.') {
				la1 = LA(1);
				if (la1 >= '0' && la1 <= '1') {
					_isFloat = true;
					Match('.');
					BinDigits();
				}
			}
			la0 = LA(0);
			if (la0 == 'P' || la0 == 'p') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					_isFloat = true;
					Match('P', 'p');
					la0 = LA(0);
					if (la0 == '+' || la0 == '-')
						Match('+', '-');
					DecDigits();
				}
			}
		}
		public void Number()
		{
			int la0, la1;
			_isFloat = false;
			_isNegative = false;
			la0 = LA(0);
			if (la0 == '-') {
				Match('-');
				_isNegative = true;
			}
			_typeSuffix = GSymbol.Get("");
			la0 = LA(0);
			if (la0 == '0') {
				la1 = LA(1);
				switch (la1) {
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
			la0 = LA(0);
			switch (la0) {
				case 'F':
				case 'f': {
						Match('F', 'f');
						_typeSuffix = _F; _isFloat = true;
					}
					break;
				case 'D':
				case 'd': {
						Match('D', 'd');
						_typeSuffix = _D; _isFloat = true;
					}
					break;
				case 'M':
				case 'm': {
						Match('M', 'm');
						_typeSuffix = _M; _isFloat = true;
					}
					break;
				case 'L':
				case 'l': {
						Match('L', 'l');
						_typeSuffix = _L;
						la0 = LA(0);
						if (la0 == 'U' || la0 == 'u') {
							Match('U', 'u');
							_typeSuffix = _UL;
						}
					}
					break;
				case 'U':
				case 'u': {
						Match('U', 'u');
						_typeSuffix = _U;
						la0 = LA(0);
						if (la0 == 'L' || la0 == 'l') {
							Match('L', 'l');
							_typeSuffix = _UL;
						}
					}
					break;
			}
			ParseNumberValue();
		}
		public void Token()
		{
			int la0, la1, la2;
			do {
				la0 = LA(0);
				switch (la0) {
					case '\t':
					case ' ': {
							_type = LS.Spaces;
							Spaces();
						}
						break;
					case '\n':
					case '\r': {
							_type = LS.Newline;
							Newline();
						}
						break;
					case '/': {
							la1 = LA(1);
							if (la1 == '/') {
								_type = LS.SLComment;
								SLComment();
							} else if (la1 == '*')
								goto match5;
							else
								goto match23;
						}
						break;
					case '#': {
							if (_inputPosition == 0) {
								la1 = LA(1);
								if (la1 == '!')
									goto match6;
								else
									goto match1;
							} else
								goto match1;
						}
						break;
					case '$': {
							la1 = LA(1);
							if (Id_set3.Contains(la1))
								goto match7;
							else
								goto match1;
						}
						break;
					case '-':
					case '.': {
							la1 = LA(1);
							if (la1 == '.' || la1 >= '0' && la1 <= '9')
								goto match8;
							else
								goto match23;
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
						goto match8;
					case '\'': {
							_type = LS.SQString;
							SQString();
						}
						break;
					case '@': {
							la1 = LA(1);
							switch (la1) {
								case '@': {
										la2 = LA(2);
										if (la2 == '"')
											goto match10;
										else {
											_type = LS.LCodeQuoteS;
											LCodeQuoteS();
										}
									}
									break;
								case '"':
									goto match10;
								case '(':
								case '[':
								case '{': {
										_type = LS.LCodeQuote;
										LCodeQuote();
									}
									break;
								default:
									goto match1;
							}
						}
						break;
					case '"':
						goto match10;
					case '`': {
							_type = LS.BQString;
							BQString();
						}
						break;
					case ',': {
							_type = LS.Comma;
							Comma();
						}
						break;
					case ':': {
							_type = LS.Colon;
							Colon();
						}
						break;
					case ';': {
							_type = LS.Semicolon;
							Semicolon();
						}
						break;
					case '(': {
							_type = LS.LParen;
							LParen();
						}
						break;
					case '[': {
							_type = LS.LBrack;
							LBrack();
						}
						break;
					case '{': {
							_type = LS.LBrace;
							LBrace();
						}
						break;
					case ')': {
							_type = LS.RParen;
							RParen();
						}
						break;
					case ']': {
							_type = LS.RBrack;
							RBrack();
						}
						break;
					case '}': {
							_type = LS.RBrace;
							RBrace();
						}
						break;
					case '!':
					case '%':
					case '&':
					case '*':
					case '+':
					case '<':
					case '=':
					case '>':
					case '?':
					case '\\':
					case '^':
					case '|':
					case '~':
						goto match23;
					default:
						goto match1;
				}
				break;
			match1: {
					_type = LS.Id;
					Id();
				}
				break;
			match5: {
					_type = LS.MLComment;
					MLComment();
				}
				break;
			match6: {
					Check(_inputPosition == 0);
					_type = LS.Shebang;
					Shebang();
				}
				break;
			match7: {
					_type = LS.Symbol;
					Symbol();
				}
				break;
			match8: {
					_type = LS.Number;
					Number();
				}
				break;
			match10: {
					_type = LS.DQString;
					DQString();
				}
				break;
			match23: {
					_type = LS.Operator;
					Operator();
				}
			} while (false);
		}
		public void Shebang()
		{
			int la0;
			Match('#');
			Match('!');
			for (; ; ) {
				la0 = LA(0);
				if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
					MatchExcept('\n', '\r');
				else
					break;
			}
			la0 = LA(0);
			if (la0 == '\n' || la0 == '\r')
				Newline();
		}
	}
}