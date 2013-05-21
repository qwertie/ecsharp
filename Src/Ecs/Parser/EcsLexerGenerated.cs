using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.LLParserGenerator;
using Loyc;

namespace Ecs.Parser
{
	using TT = TokenType;

	public partial class EcsLexer
	{
		public void Newline()
		{
			int la0;
			la0 = LA(0);
			if (la0 == '\r') {
				Skip();
				la0 = LA(0);
				if (la0 == '\n')
					Skip();
			} else
				Match('\n');
			_allowPPAt = _lineStartAt = _inputPosition;
			_lineNumber++;
		}
		public void Spaces()
		{
			int la0;
			Match('\t', ' ');
			for (;;) {
				la0 = LA(0);
				if (la0 == '\t' || la0 == ' ')
					Skip();
				else
					break;
			}
			if (_allowPPAt == _startPosition) _allowPPAt = _inputPosition;
			if (_lineStartAt == _startPosition) _indentLevel = MeasureIndent(_startPosition, _inputPosition - _startPosition);
		}
		public void SLComment()
		{
			int la0;
			Match('/');
			Match('/');
			for (;;) {
				la0 = LA(0);
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
							MLComment();
						else
							MatchExcept();
					} else
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
			_verbatims = 0;
			Match('\'');
			for (;;) {
				la0 = LA(0);
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
			la0 = LA(0);
			if (la0 == '"') {
				_verbatims = 0;
				Skip();
				for (;;) {
					la0 = LA(0);
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
				_verbatims = 1;
				Match('@');
				la0 = LA(0);
				if (la0 == '@') {
					Skip();
					_verbatims = 2;
				}
				Match('"');
				for (;;) {
					la0 = LA(0);
					if (la0 == '"') {
						la1 = LA(1);
						if (la1 == '"') {
							Skip();
							Skip();
							_parseNeeded = true;
						} else
							break;
					} else if (la0 == '\\') {
						la1 = LA(1);
						if (la1 == '(' || la1 == '{') {
							Skip();
							Skip();
							_parseNeeded = true;
						} else
							Skip();
					} else if (la0 != -1)
						Skip();
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
		private void BQStringN()
		{
			int la0;
			_verbatims = 0;
			Match('`');
			for (;;) {
				la0 = LA(0);
				if (la0 == '\\') {
					Skip();
					_parseNeeded = true;
					MatchExcept();
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '`'))
					Skip();
				else
					break;
			}
			Match('`');
		}
		private void BQStringV()
		{
			int la0, la1;
			_verbatims = 1;
			Skip();
			for (;;) {
				la0 = LA(0);
				if (la0 == '`') {
					la1 = LA(1);
					if (la1 == '`') {
						Skip();
						Skip();
						_parseNeeded = true;
					} else
						break;
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
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
		private void Colon()
		{
			Skip();
			_type = TT.Colon;
			_value = _Colon;
		}
		private void Semicolon()
		{
			Skip();
			_type = TT.Semicolon;
			_value = _Semicolon;
		}
		public void Operator()
		{
			int la0, la1, la2;
			do {
				la0 = LA(0);
				switch (la0) {
				case '-':
					{
						la1 = LA(1);
						if (la1 == '>') {
							Skip();
							Skip();
							_type = TT.PtrArrow;
							_value = _PtrArrow;
						} else if (la1 == '=') {
							Skip();
							Skip();
							_type = TT.SubSet;
							_value = _SubSet;
						} else if (la1 == '-') {
							Skip();
							Skip();
							_type = TT.Dec;
							_value = _Dec;
						} else {
							Skip();
							_type = TT.Sub;
							_value = _Sub;
						}
					}
					break;
				case '.':
					{
						la1 = LA(1);
						if (la1 == '.') {
							Skip();
							Skip();
							_type = TT.DotDot;
							_value = _DotDot;
						} else {
							Skip();
							_type = TT.Dot;
							_value = _Dot;
						}
					}
					break;
				case '>':
					{
						la1 = LA(1);
						if (la1 == '>') {
							la2 = LA(2);
							if (la2 == '=') {
								Skip();
								Skip();
								Skip();
								_type = TT.ShrSet;
								_value = _ShrSet;
							} else
								goto match6;
						} else if (la1 == '=') {
							Skip();
							Skip();
							_type = TT.GE;
							_value = _GE;
						} else
							goto match6;
					}
					break;
				case '<':
					{
						la1 = LA(1);
						if (la1 == '<') {
							la2 = LA(2);
							if (la2 == '=') {
								Skip();
								Skip();
								Skip();
								_type = TT.ShlSet;
								_value = _ShlSet;
							} else
								goto match9;
						} else if (la1 == '=') {
							Skip();
							Skip();
							_type = TT.LE;
							_value = _LE;
						} else
							goto match9;
					}
					break;
				case '&':
					{
						la1 = LA(1);
						if (la1 == '&') {
							Skip();
							Skip();
							_type = TT.And;
							_value = _And;
						} else if (la1 == '=') {
							Skip();
							Skip();
							_type = TT.AndBitsSet;
							_value = _AndBitsSet;
						} else {
							Skip();
							_type = TT.AndBits;
							_value = _AndBits;
						}
					}
					break;
				case '|':
					{
						la1 = LA(1);
						if (la1 == '|') {
							Skip();
							Skip();
							_type = TT.Or;
							_value = _Or;
						} else if (la1 == '=') {
							Skip();
							Skip();
							_type = TT.OrBitsSet;
							_value = _OrBitsSet;
						} else {
							Skip();
							_type = TT.OrBits;
							_value = _OrBits;
						}
					}
					break;
				case '^':
					{
						la1 = LA(1);
						if (la1 == '^') {
							Skip();
							Skip();
							_type = TT.Xor;
							_value = _Xor;
						} else if (la1 == '=') {
							Skip();
							Skip();
							_type = TT.XorBitsSet;
							_value = _XorBitsSet;
						} else {
							Skip();
							_type = TT.XorBits;
							_value = _XorBits;
						}
					}
					break;
				case '=':
					{
						la1 = LA(1);
						if (la1 == '=') {
							la2 = LA(2);
							if (la2 == '>') {
								Skip();
								Skip();
								Skip();
								_type = TT.Forward;
								_value = _Forward;
							} else {
								Skip();
								Skip();
								_type = TT.Eq;
								_value = _Eq;
							}
						} else if (la1 == '>') {
							Skip();
							Skip();
							_type = TT.LambdaArrow;
							_value = _LambdaArrow;
						} else {
							Skip();
							_type = TT.Set;
							_value = _Set;
						}
					}
					break;
				case '!':
					{
						la1 = LA(1);
						if (la1 == '=') {
							Skip();
							Skip();
							_type = TT.Neq;
							_value = _Neq;
						} else {
							Skip();
							_type = TT.Not;
							_value = _Not;
						}
					}
					break;
				case '~':
					{
						Skip();
						_type = TT.NotBits;
						_value = _NotBits;
					}
					break;
				case '*':
					{
						la1 = LA(1);
						if (la1 == '*') {
							la2 = LA(2);
							if (la2 == '=') {
								Skip();
								Skip();
								Skip();
								_type = TT.ExpSet;
								_value = _ExpSet;
							} else
								goto match28;
						} else if (la1 == '=') {
							Skip();
							Skip();
							_type = TT.MulSet;
							_value = _MulSet;
						} else
							goto match28;
					}
					break;
				case '/':
					{
						la1 = LA(1);
						if (la1 == '=') {
							Skip();
							Skip();
							_type = TT.DivSet;
							_value = _DivSet;
						} else {
							Skip();
							_type = TT.Div;
							_value = _Div;
						}
					}
					break;
				case '%':
					{
						la1 = LA(1);
						if (la1 == '=') {
							Skip();
							Skip();
							_type = TT.ModSet;
							_value = _ModSet;
						} else {
							Skip();
							_type = TT.Mod;
							_value = _Mod;
						}
					}
					break;
				case '+':
					{
						la1 = LA(1);
						if (la1 == '=') {
							Skip();
							Skip();
							_type = TT.AddSet;
							_value = _AddSet;
						} else if (la1 == '+') {
							Skip();
							Skip();
							_type = TT.Inc;
							_value = _Inc;
						} else {
							Skip();
							_type = TT.Add;
							_value = _Add;
						}
					}
					break;
				case ':':
					{
						la1 = LA(1);
						if (la1 == '=') {
							Skip();
							Skip();
							_type = TT.QuickBindVar;
							_value = _QuickBindVar;
						} else {
							la2 = LA(2);
							if (la2 == ':') {
								Skip();
								Match(':');
								Skip();
								_type = TT.QuickBind;
								_value = _QuickBind;
							} else {
								Skip();
								Match(':');
								_type = TT.ColonColon;
								_value = _ColonColon;
							}
						}
					}
					break;
				case '?':
					{
						la1 = LA(1);
						if (la1 == '?') {
							la2 = LA(2);
							if (la2 == '=') {
								Skip();
								Skip();
								Skip();
								_type = TT.NullCoalesceSet;
								_value = _NullCoalesceSet;
							} else if (la2 == '.') {
								Skip();
								Skip();
								Skip();
								_type = TT.NullDot;
								_value = _NullDot;
							} else {
								Skip();
								Skip();
								_type = TT.NullCoalesce;
								_value = _NullCoalesce;
							}
						} else if (la1 == '.') {
							Skip();
							Skip();
							_type = TT.NullDot;
							_value = _NullDot;
						} else {
							Skip();
							_type = TT.QuestionMark;
							_value = _QuestionMark;
						}
					}
					break;
				default:
					{
						Match('\\');
						_type = TT.Substitute;
						_value = _Substitute;
					}
					break;
				}
				break;
			match6:
				{
					Skip();
					_type = TT.GT;
					_value = _GT;
				}
				break;
			match9:
				{
					Skip();
					_type = TT.LT;
					_value = _LT;
				}
				break;
			match28:
				{
					Skip();
					_type = TT.Mul;
					_value = _Mul;
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
						Skip();
						SpecialIdV();
					} else
						goto match3b;
				} else if (la0 == '#') {
					la1 = LA(1);
					if (la1 == '@') {
						la2 = LA(2);
						if (Id_set3.Contains(la2)) {
							Skip();
							Skip();
							SpecialIdV();
						} else
							goto match3b;
					} else
						goto match3b;
				} else if (Id_set4.Contains(la0)) {
					IdStart();
					for (;;) {
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
			match3b:
				{
					la0 = LA(0);
					if (la0 == '@')
						Skip();
					Match('#');
					do {
						la0 = LA(0);
						switch (la0) {
						case '\\':
							{
								la1 = LA(1);
								if (la1 == 'u') {
									la2 = LA(2);
									if (Id_set0.Contains(la2))
										SpecialId();
									else
										goto match7;
								} else
									goto match7;
							}
							break;
						case '<':
							{
								la1 = LA(1);
								if (la1 == '<') {
									la2 = LA(2);
									if (la2 == '=') {
										Skip();
										Skip();
										Skip();
									} else {
										Skip();
										Skip();
									}
								} else
									goto match7;
							}
							break;
						case '>':
							{
								la1 = LA(1);
								if (la1 == '>') {
									la2 = LA(2);
									if (la2 == '=') {
										Skip();
										Skip();
										Skip();
									} else {
										Skip();
										Skip();
									}
								} else
									goto match7;
							}
							break;
						case '*':
							{
								la1 = LA(1);
								if (la1 == '*') {
									Skip();
									Skip();
								} else
									goto match7;
							}
							break;
						case ':':
							{
								la1 = LA(1);
								if (la1 == ':' || la1 == '=')
									goto match7;
								else
									Colon();
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
							goto match7;
						case ',':
							Comma();
							break;
						case ';':
							Semicolon();
							break;
						case '$':
							Skip();
							break;
						default:
							if (Id_set1.Contains(la0))
								SpecialId();
							break;
						}
						break;
					match7:
						{
							Operator();
							_type = TT.Id;
						}
					} while (false);
				}
			} while (false);
			bool isPPLine = ParseIdValue();
			if (isPPLine) {
				int ppTextStart = _inputPosition;
				for (;;) {
					la0 = LA(0);
					if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
						Skip();
					else
						break;
				}
				_value = _source.Substring(ppTextStart, _inputPosition - ppTextStart);
			}
		}
		private void IdSpecial()
		{
			int la0;
			la0 = LA(0);
			if (la0 == '\\') {
				Skip();
				Match('u');
				Match(Id_set0);
				Match(Id_set0);
				Match(Id_set0);
				Match(Id_set0);
				_parseNeeded = true;
			} else {
				Check(char.IsLetter((char) LA(0)));
				MatchRange('', '￼');
			}
		}
		private void IdStart()
		{
			int la0;
			la0 = LA(0);
			if (la0 >= 'A' && la0 <= 'Z' || la0 == '_' || la0 >= 'a' && la0 <= 'z')
				Skip();
			else
				IdSpecial();
		}
		static readonly IntSet IdCont_set0 = IntSet.Parse("['0-9A-Z_a-z]");
		private void IdCont()
		{
			int la0;
			la0 = LA(0);
			if (IdCont_set0.Contains(la0))
				Skip();
			else
				IdSpecial();
		}
		private void SpecialId()
		{
			int la0;
			la0 = LA(0);
			if (la0 == '`')
				BQStringN();
			else {
				IdCont();
				for (;;) {
					la0 = LA(0);
					if (Id_set2.Contains(la0))
						IdCont();
					else
						break;
				}
			}
		}
		private void SpecialIdV()
		{
			int la0;
			la0 = LA(0);
			if (la0 == '`')
				BQStringV();
			else {
				IdCont();
				for (;;) {
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
		public void At()
		{
			Match('@');
		}
		private void DecDigits()
		{
			int la0, la1;
			MatchRange('0', '9');
			for (;;) {
				la0 = LA(0);
				if (la0 >= '0' && la0 <= '9')
					Skip();
				else
					break;
			}
			for (;;) {
				la0 = LA(0);
				if (la0 == '_') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						Skip();
						Skip();
						for (;;) {
							la0 = LA(0);
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
		private void HexDigits()
		{
			int la0, la1;
			Skip();
			for (;;) {
				la0 = LA(0);
				if (Id_set0.Contains(la0))
					Skip();
				else
					break;
			}
			for (;;) {
				la0 = LA(0);
				if (la0 == '_') {
					la1 = LA(1);
					if (Id_set0.Contains(la1)) {
						Skip();
						Skip();
						for (;;) {
							la0 = LA(0);
							if (Id_set0.Contains(la0))
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
				la0 = LA(0);
				if (la0 >= '0' && la0 <= '1')
					Skip();
				else
					break;
			}
			for (;;) {
				la0 = LA(0);
				if (la0 == '_') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '1') {
						Skip();
						Skip();
						for (;;) {
							la0 = LA(0);
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
			la0 = LA(0);
			if (la0 == '.') {
				_isFloat = true;
				Skip();
				DecDigits();
			} else {
				DecDigits();
				la0 = LA(0);
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						_isFloat = true;
						Skip();
						DecDigits();
					}
				}
			}
			la0 = LA(0);
			if (la0 == 'E' || la0 == 'e') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					_isFloat = true;
					Skip();
					la0 = LA(0);
					if (la0 == '+' || la0 == '-')
						Skip();
					DecDigits();
				}
			}
		}
		private void HexNumber()
		{
			int la0, la1;
			_numberBase = 16;
			Skip();
			Skip();
			la0 = LA(0);
			if (Id_set0.Contains(la0))
				HexDigits();
			la0 = LA(0);
			if (la0 == '.') {
				la1 = LA(1);
				if (Id_set0.Contains(la1)) {
					_isFloat = true;
					Skip();
					HexDigits();
				}
			}
			la0 = LA(0);
			if (la0 == 'P' || la0 == 'p') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					_isFloat = true;
					Skip();
					la0 = LA(0);
					if (la0 == '+' || la0 == '-')
						Skip();
					DecDigits();
				}
			}
		}
		private void BinNumber()
		{
			int la0, la1;
			_numberBase = 2;
			Skip();
			Skip();
			la0 = LA(0);
			if (la0 >= '0' && la0 <= '1')
				BinDigits();
			la0 = LA(0);
			if (la0 == '.') {
				la1 = LA(1);
				if (la1 >= '0' && la1 <= '1') {
					_isFloat = true;
					Skip();
					BinDigits();
				}
			}
			la0 = LA(0);
			if (la0 == 'P' || la0 == 'p') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					_isFloat = true;
					Skip();
					la0 = LA(0);
					if (la0 == '+' || la0 == '-')
						Skip();
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
				Skip();
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
					la0 = LA(0);
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
					la0 = LA(0);
					if (la0 == 'L' || la0 == 'l') {
						Skip();
						_typeSuffix = _UL;
					}
				}
				break;
			}
			ParseNumberValue();
		}
		static readonly IntSet Token_set0 = IntSet.Parse("(35, 39, 48..57, 65..90, 92, 95..122, 128..65532)");
		public void Token()
		{
			int la0, la1, la2;
			do {
				la0 = LA(0);
				switch (la0) {
				case '#':
					{
						if (_inputPosition == 0) {
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
				case '$':
					{
						la1 = LA(1);
						if (Id_set3.Contains(la1)) {
							_type = TT.Symbol;
							Symbol();
						} else
							goto match3;
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
				case '.':
					{
						la1 = LA(1);
						if (la1 == '.' || la1 >= '0' && la1 <= '9')
							goto match8;
						else
							Operator();
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
				case '@':
					{
						la1 = LA(1);
						if (la1 == '"' || la1 == '@')
							goto match9;
						else if (Token_set0.Contains(la1))
							goto match3;
						else
							goto match9;
					}
					break;
				case '\'':
					{
						_type = TT.SQString;
						SQString();
					}
					break;
				case '"':
					{
						_type = TT.DQString;
						DQString();
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
				case ':':
					{
						_type = TT.Colon;
						Colon();
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
					{
						la1 = LA(1);
						if (la1 == 'u') {
							la2 = LA(2);
							if (Id_set0.Contains(la2))
								goto match3;
							else
								Operator();
						} else
							Operator();
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
			match9:
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
				la0 = LA(0);
				if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
					Skip();
				else
					break;
			}
			la0 = LA(0);
			if (la0 == '\n' || la0 == '\r')
				Newline();
		}
		Symbol _Comma = GSymbol.Get("#,");
		Symbol _Colon = GSymbol.Get("#:");
		Symbol _Semicolon = GSymbol.Get("#;");
		Symbol _PtrArrow = GSymbol.Get("#->");
		Symbol _DotDot = GSymbol.Get("#..");
		Symbol _Dot = GSymbol.Get("#.");
		Symbol _ShrSet = GSymbol.Get("#>>=");
		Symbol _GE = GSymbol.Get("#>=");
		Symbol _GT = GSymbol.Get("#>");
		Symbol _ShlSet = GSymbol.Get("#<<=");
		Symbol _LE = GSymbol.Get("#<=");
		Symbol _LT = GSymbol.Get("#<");
		Symbol _And = GSymbol.Get("#&&");
		Symbol _AndBitsSet = GSymbol.Get("#&=");
		Symbol _AndBits = GSymbol.Get("#&");
		Symbol _Or = GSymbol.Get("#||");
		Symbol _OrBitsSet = GSymbol.Get("#|=");
		Symbol _OrBits = GSymbol.Get("#|");
		Symbol _Xor = GSymbol.Get("#^^");
		Symbol _XorBitsSet = GSymbol.Get("#^=");
		Symbol _XorBits = GSymbol.Get("#^");
		Symbol _Forward = GSymbol.Get("#==>");
		Symbol _Eq = GSymbol.Get("#==");
		Symbol _LambdaArrow = GSymbol.Get("#=>");
		Symbol _Set = GSymbol.Get("#=");
		Symbol _Neq = GSymbol.Get("#!=");
		Symbol _Not = GSymbol.Get("#!");
		Symbol _NotBits = GSymbol.Get("#~");
		Symbol _ExpSet = GSymbol.Get("#**=");
		Symbol _MulSet = GSymbol.Get("#*=");
		Symbol _Mul = GSymbol.Get("#*");
		Symbol _DivSet = GSymbol.Get("#/=");
		Symbol _Div = GSymbol.Get("#/");
		Symbol _ModSet = GSymbol.Get("#%=");
		Symbol _Mod = GSymbol.Get("#%");
		Symbol _AddSet = GSymbol.Get("#+=");
		Symbol _Inc = GSymbol.Get("#++");
		Symbol _Add = GSymbol.Get("#+");
		Symbol _SubSet = GSymbol.Get("#-=");
		Symbol _Dec = GSymbol.Get("#--");
		Symbol _Sub = GSymbol.Get("#-");
		Symbol _QuickBindVar = GSymbol.Get("#:=");
		Symbol _QuickBind = GSymbol.Get("#:::");
		Symbol _ColonColon = GSymbol.Get("#::");
		Symbol _NullCoalesceSet = GSymbol.Get("#??=");
		Symbol _NullDot = GSymbol.Get("#?.");
		Symbol _NullCoalesce = GSymbol.Get("#??");
		Symbol _QuestionMark = GSymbol.Get("#?");
		Symbol _Substitute = GSymbol.Get("#\\");
	}
}