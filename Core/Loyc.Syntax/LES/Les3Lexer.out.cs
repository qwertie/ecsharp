// Generated from Les3Lexer.ecs by LeMP custom tool. LeMP version: 1.9.0.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax.Lexing;
namespace Loyc.Syntax.Les
{
	using TT = TokenType;
	using P = LesPrecedence;
	using S = CodeSymbols;
	public partial class Les3Lexer
	{
		void DotIndent()
		{
			int la0, la1;
			// Line 24: ([.] ([\t] | [ ] ([ ])*))*
			for (;;) {
				la0 = LA0;
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 == '\t' || la1 == ' ') {
						Skip();
						// Line 24: ([\t] | [ ] ([ ])*)
						la0 = LA0;
						if (la0 == '\t')
							Skip();
						else {
							Match(' ');
							// Line 24: ([ ])*
							for (;;) {
								la0 = LA0;
								if (la0 == ' ')
									Skip();
								else
									break;
							}
						}
					} else
						break;
				} else
					break;
			}
		}
		object Newline(bool ignoreIndent = false)
		{
			int la0;
			// Line 27: ([\r] ([\n])? | [\n])
			la0 = LA0;
			if (la0 == '\r') {
				Skip();
				// Line 27: ([\n])?
				la0 = LA0;
				if (la0 == '\n')
					Skip();
			} else
				Match('\n');
			AfterNewline(ignoreIndent, skipIndent: false);
			return WhitespaceTag.Value;
		}
		object SLComment()
		{
			int la0;
			Skip();
			Skip();
			// Line 31: ([^\$\n\r])*
			for (;;) {
				la0 = LA0;
				if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
					Skip();
				else
					break;
			}
			// line 32
			return WhitespaceTag.Value;
		}
		object MLComment()
		{
			int la1;
			Skip();
			Skip();
			// Line 34: nongreedy( MLComment / Newline / [^\$] )*
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
					Newline(true);
					break;
				default:
					Skip();
					break;
				}
			}
		stop:;
			Match('*');
			Match('/');
			// line 35
			return WhitespaceTag.Value;
		}
		object Number()
		{
			int la0, la1;
			UString suffix = default(UString);
			// Line 40: ([\-] / )
			la0 = LA0;
			if (la0 == '-') {
				Skip();
				// line 40
				_isNegative = true;
			} else
				// line 40
				_isNegative = false;
			// Line 41: ( HexNumber / BinNumber / DecNumber )
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
			// line 42
			UString numberText = Text();
			// Line 43: (IdCore)?
			do {
				la0 = LA0;
				if (la0 == '`') {
					la1 = LA(1);
					if (!(la1 == -1 || la1 == '\n' || la1 == '\r'))
						goto matchIdCore;
				} else if (la0 >= 'A' && la0 <= 'Z' || la0 == '_' || la0 >= 'a' && la0 <= 'z')
					goto matchIdCore;
				break;
			matchIdCore:
				{
					_startPosition = InputPosition;
					object boolOrNull = NoValue.Value;
					suffix = IdCore(ref boolOrNull);
					// line 46
					PrintErrorIfTypeMarkerIsKeywordLiteral(boolOrNull);
				}
			} while (false);
			// line 49
			_type = _isNegative ? TT.NegativeLiteral : TT.Literal;
			return ParseLiteral2(suffix, numberText, true);
		}
		void DecDigits()
		{
			int la0, la1;
			MatchRange('0', '9');
			// Line 53: ([0-9])*
			for (;;) {
				la0 = LA0;
				if (la0 >= '0' && la0 <= '9')
					Skip();
				else
					break;
			}
			// Line 53: greedy(['_] [0-9] ([0-9])*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '\'' || la0 == '_') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						Skip();
						Skip();
						// Line 53: ([0-9])*
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
			// Line 53: greedy([_])?
			la0 = LA0;
			if (la0 == '_')
				Skip();
		}
		static readonly HashSet<int> HexDigit_set0 = NewSetOfRanges('0', '9', 'A', 'F', 'a', 'f');
		void HexDigit()
		{
			Match(HexDigit_set0);
		}
		void HexDigits()
		{
			int la0, la1;
			Skip();
			// Line 55: greedy([0-9A-Fa-f])*
			for (;;) {
				la0 = LA0;
				if (HexDigit_set0.Contains(la0))
					Skip();
				else
					break;
			}
			// Line 55: greedy(['_] [0-9A-Fa-f] greedy([0-9A-Fa-f])*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '\'' || la0 == '_') {
					la1 = LA(1);
					if (HexDigit_set0.Contains(la1)) {
						Skip();
						Skip();
						// Line 55: greedy([0-9A-Fa-f])*
						for (;;) {
							la0 = LA0;
							if (HexDigit_set0.Contains(la0))
								Skip();
							else
								break;
						}
					} else
						break;
				} else
					break;
			}
			// Line 55: greedy([_])?
			la0 = LA0;
			if (la0 == '_')
				Skip();
		}
		bool Scan_HexDigits()
		{
			int la0, la1;
			if (!TryMatch(HexDigit_set0))
				return false;
			// Line 55: greedy([0-9A-Fa-f])*
			for (;;) {
				la0 = LA0;
				if (HexDigit_set0.Contains(la0))
					{if (!TryMatch(HexDigit_set0))
						return false;}
				else
					break;
			}
			// Line 55: greedy(['_] [0-9A-Fa-f] greedy([0-9A-Fa-f])*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '\'' || la0 == '_') {
					la1 = LA(1);
					if (HexDigit_set0.Contains(la1)) {
						if (!TryMatch('\'', '_'))
							return false;
						if (!TryMatch(HexDigit_set0))
							return false;
						// Line 55: greedy([0-9A-Fa-f])*
						for (;;) {
							la0 = LA0;
							if (HexDigit_set0.Contains(la0))
								{if (!TryMatch(HexDigit_set0))
									return false;}
							else
								break;
						}
					} else
						break;
				} else
					break;
			}
			// Line 55: greedy([_])?
			la0 = LA0;
			if (la0 == '_')
				if (!TryMatch('_'))
					return false;
			return true;
		}
		void DecNumber()
		{
			int la0, la1;
			// line 59
			_numberBase = 10;
			// Line 60: (DecDigits | [.] DecDigits => )
			la0 = LA0;
			if (la0 >= '0' && la0 <= '9')
				DecDigits();
			else {
			}
			// Line 61: ([.] DecDigits)?
			la0 = LA0;
			if (la0 == '.') {
				la1 = LA(1);
				if (la1 >= '0' && la1 <= '9') {
					// line 61
					_isFloat = true;
					Skip();
					DecDigits();
				}
			}
			// Line 62: greedy([Ee] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'E' || la0 == 'e') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					// line 62
					_isFloat = true;
					Skip();
					// Line 62: ([+\-])?
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
			// line 65
			_numberBase = 16;
			// Line 66: (HexDigits | [.] HexDigits => )
			la0 = LA0;
			if (HexDigit_set0.Contains(la0))
				HexDigits();
			else {
			}
			// Line 68: ([.] ([0-9] =>  / &(HexDigits [Pp] [+\-0-9])) HexDigits)?
			do {
				la0 = LA0;
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9')
						goto match1;
					else if (la1 >= 'A' && la1 <= 'F' || la1 >= 'a' && la1 <= 'f') {
						if (Try_HexNumber_Test0(1))
							goto match1;
					}
				}
				break;
			match1:
				{
					Skip();
					// Line 68: ([0-9] =>  / &(HexDigits [Pp] [+\-0-9]))
					la0 = LA0;
					if (la0 >= '0' && la0 <= '9') {
					} else
						Check(Try_HexNumber_Test0(0), "HexDigits [Pp] [+\\-0-9]");
					// line 69
					_isFloat = true;
					HexDigits();
				}
			} while (false);
			// Line 70: greedy([Pp] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'P' || la0 == 'p') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					// line 70
					_isFloat = true;
					Skip();
					// Line 70: ([+\-])?
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
			// line 73
			_numberBase = 2;
			// Line 74: (DecDigits | [.] DecDigits => )
			la0 = LA0;
			if (la0 >= '0' && la0 <= '9')
				DecDigits();
			else {
			}
			// Line 75: ([.] DecDigits)?
			la0 = LA0;
			if (la0 == '.') {
				la1 = LA(1);
				if (la1 >= '0' && la1 <= '9') {
					// line 75
					_isFloat = true;
					Skip();
					DecDigits();
				}
			}
			// Line 76: greedy([Pp] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'P' || la0 == 'p') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					// line 76
					_isFloat = true;
					Skip();
					// Line 76: ([+\-])?
					la0 = LA0;
					if (la0 == '+' || la0 == '-')
						Skip();
					DecDigits();
				}
			}
		}
		object SQString()
		{
			int la0;
			// line 85
			_parseNeeded = false;
			Skip();
			// Line 86: ([\\] [^\$] | [^\$\n\r'\\])
			la0 = LA0;
			if (la0 == '\\') {
				Skip();
				MatchExcept();
				// line 86
				_parseNeeded = true;
			} else
				MatchExcept('\n', '\r', '\'', '\\');
			Match('\'');
			// line 87
			return ParseSQStringValue();
		}
		object DQString()
		{
			int la0, la1;
			// line 90
			_parseNeeded = false;
			Skip();
			// Line 91: ([\\] [^\$] | [^\$\n\r"\\])*
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					la1 = LA(1);
					if (la1 != -1) {
						Skip();
						Skip();
						// line 91
						_parseNeeded = true;
					} else
						break;
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '"'))
					Skip();
				else
					break;
			}
			// Line 92: (["] / )
			la0 = LA0;
			if (la0 == '"')
				Skip();
			else
				// line 92
				_parseNeeded = true;
			// line 93
			return ParseStringValue(isTripleQuoted: false);
		}
		object TQString()
		{
			int la0, la1, la2;
			_parseNeeded = true;
			_style = NodeStyle.Alternate;
			// Line 98: (["] ["] ["] nongreedy(Newline / [^\$])* ["] ["] ["] | ['] ['] ['] nongreedy(Newline / [^\$])* ['] ['] ['])
			la0 = LA0;
			if (la0 == '"') {
				Skip();
				Match('"');
				Match('"');
				// Line 98: nongreedy(Newline / [^\$])*
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
						Newline(true);
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
				// line 99
				_style |= NodeStyle.Alternate2;
				Match('\'');
				Match('\'');
				Match('\'');
				// Line 100: nongreedy(Newline / [^\$])*
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
						Newline(true);
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
			// line 101
			return ParseStringValue(isTripleQuoted: true, les3TQIndents: true);
		}
		void BQString()
		{
			int la0;
			// line 104
			_parseNeeded = false;
			Skip();
			// Line 105: ([\\] [^\$] | [^\$\n\r\\`])*
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					Skip();
					MatchExcept();
					// line 105
					_parseNeeded = true;
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '`'))
					Skip();
				else
					break;
			}
			Match('`');
		}
		void OpChar()
		{
			Skip();
		}
		object Operator()
		{
			// Line 112: (OpChar | [$])
			switch (LA0) {
			case '!':
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
				OpChar();
				break;
			default:
				Match('$');
				break;
			}
			// Line 112: (OpChar)*
			for (;;) {
				switch (LA0) {
				case '!':
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
					OpChar();
					break;
				default:
					goto stop;
				}
			}
		stop:;
			// line 112
			return ParseNormalOp();
		}
		static readonly HashSet<int> SQOperator_set0 = NewSetOfRanges('!', '!', '#', '&', '*', '+', '-', ':', '<', '?', 'A', 'Z', '^', '_', 'a', 'z', '|', '|', '~', '~');
		object SQOperator()
		{
			int la0;
			Skip();
			LettersOrPunc();
			// Line 114: (LettersOrPunc)*
			for (;;) {
				la0 = LA0;
				if (SQOperator_set0.Contains(la0))
					LettersOrPunc();
				else
					break;
			}
			// line 116
			return ParseNormalOp();
		}
		static readonly HashSet<int> NormalId_set0 = NewSetOfRanges('A', 'Z', '_', '_', 'a', 'z');
		void NormalId()
		{
			int la0;
			Match(NormalId_set0);
			// Line 124: greedy( [A-Z_a-z] | [#] | [0-9] | ['] &!(['] [']) )*
			for (;;) {
				la0 = LA0;
				if (la0 >= 'A' && la0 <= 'Z' || la0 == '_' || la0 >= 'a' && la0 <= 'z')
					Skip();
				else if (la0 == '#')
					Skip();
				else if (la0 >= '0' && la0 <= '9')
					Skip();
				else if (la0 == '\'') {
					if (!Try_NormalId_Test0(1))
						Skip();
					else
						break;
				} else
					break;
			}
		}
		object Id()
		{
			int la0, la1;
			UString idtext = default(UString);
			object value = default(object);
			// line 127
			object boolOrNull = NoValue.Value;
			idtext = IdCore(ref boolOrNull);
			// Line 129: ((TQString / DQString))?
			do {
				la0 = LA0;
				if (la0 == '"')
					goto match1;
				else if (la0 == '\'') {
					la1 = LA(1);
					if (la1 == '\'')
						goto match1;
				}
				break;
			match1:
				{
					var old_startPosition_0 = _startPosition;
					try {
						_startPosition = InputPosition;
						// Line 130: (TQString / DQString)
						la0 = LA0;
						if (la0 == '"') {
							la1 = LA(1);
							if (la1 == '"')
								value = TQString();
							else
								value = DQString();
						} else
							value = TQString();
						// line 132
						_type = TT.Literal;
						PrintErrorIfTypeMarkerIsKeywordLiteral(boolOrNull);
						return _value = ParseLiteral2(idtext, value.ToString(), false);
					} finally {
						_startPosition = old_startPosition_0;
					}
				}
			} while (false);
			// line 137
			return boolOrNull != NoValue.Value ? boolOrNull : IdToSymbol(idtext);
		}
		UString IdCore(ref object boolOrNull)
		{
			int la0;
			UString result = default(UString);
			// Line 140: (BQString | NormalId)
			la0 = LA0;
			if (la0 == '`') {
				BQString();
				// line 140
				result = ParseStringCore(false);
			} else {
				NormalId();
				// line 142
				result = Text();
				if (result == "true") {
					_type = TT.Literal;
					boolOrNull = G.BoxedTrue;
				}
				if (result == "false") {
					_type = TT.Literal;
					boolOrNull = G.BoxedFalse;
				}
				if (result == "null") {
					_type = TT.Literal;
					boolOrNull = null;
				}
			}
			return result;
		}
		void LettersOrPunc()
		{
			Match(SQOperator_set0);
		}
		object SpecialLiteral()
		{
			int la0;
			Skip();
			Skip();
			LettersOrPunc();
			// Line 153: (LettersOrPunc)*
			for (;;) {
				la0 = LA0;
				if (SQOperator_set0.Contains(la0))
					LettersOrPunc();
				else
					break;
			}
			// line 153
			return ParseAtAtLiteral(Text());
		}
		object Keyword()
		{
			int la0;
			Skip();
			LettersOrPunc();
			// Line 156: (LettersOrPunc)*
			for (;;) {
				la0 = LA0;
				if (SQOperator_set0.Contains(la0))
					LettersOrPunc();
				else
					break;
			}
			// line 156
			return IdToSymbol(Text());
		}
		object Shebang()
		{
			int la0;
			Skip();
			Skip();
			// Line 161: ([^\$\n\r])*
			for (;;) {
				la0 = LA0;
				if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
					Skip();
				else
					break;
			}
			// Line 161: (Newline)?
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r')
				Newline();
			// line 162
			return WhitespaceTag.Value;
		}
		static readonly HashSet<int> NextToken_set0 = NewSetOfRanges('#', '&', '*', '+', '-', ':', '<', '?', 'A', 'Z', '^', '_', 'a', 'z', '|', '|', '~', '~');
		public override Maybe<Token> NextToken()
		{
			int la0, la1, la2, la3;
			object value = default(object);
			// Line 167: (Spaces / &{InputPosition == _lineStartAt} [.] [\t ] => DotIndent)?
			la0 = LA0;
			if (la0 == '\t' || la0 == ' ')
				Spaces();
			else if (la0 == '.') {
				if (InputPosition == _lineStartAt) {
					la1 = LA(1);
					if (la1 == '\t' || la1 == ' ')
						DotIndent();
				}
			}
			// line 169
			_startPosition = InputPosition;
			_style = 0;
			if (LA0 == -1) {
				return NoValue.Value;
			}
			// Line 175: ( Shebang / SpecialLiteral / Id / Keyword / Newline / SLComment / MLComment / Number / TQString / DQString / SQString / SQOperator / ['] ['] / [,] / [;] / [(] / [)] / [[] / [\]] / [{] / [}] / ['] [{] / [@] [@] [{] / [@] / Operator )
			do {
				switch (LA0) {
				case '#':
					{
						if (InputPosition == 0) {
							la1 = LA(1);
							if (la1 == '!') {
								// line 175
								_type = TT.Shebang;
								value = Shebang();
							} else if (NextToken_set0.Contains(la1))
								goto matchKeyword;
							else
								goto error;
						} else
							goto matchKeyword;
					}
					break;
				case '@':
					{
						la1 = LA(1);
						if (la1 == '@') {
							la2 = LA(2);
							if (SQOperator_set0.Contains(la2)) {
								// line 176
								_type = TT.Literal;
								value = SpecialLiteral();
							} else if (la2 == '{') {
								// line 197
								_type = TT.LTokenLiteral;
								Skip();
								Skip();
								Skip();
							} else
								goto match24;
						} else
							goto match24;
					}
					break;
				case 'A':
				case 'B':
				case 'C':
				case 'D':
				case 'E':
				case 'F':
				case 'G':
				case 'H':
				case 'I':
				case 'J':
				case 'K':
				case 'L':
				case 'M':
				case 'N':
				case 'O':
				case 'P':
				case 'Q':
				case 'R':
				case 'S':
				case 'T':
				case 'U':
				case 'V':
				case 'W':
				case 'X':
				case 'Y':
				case 'Z':
				case '_':
				case '`':
				case 'a':
				case 'b':
				case 'c':
				case 'd':
				case 'e':
				case 'f':
				case 'g':
				case 'h':
				case 'i':
				case 'j':
				case 'k':
				case 'l':
				case 'm':
				case 'n':
				case 'o':
				case 'p':
				case 'q':
				case 'r':
				case 's':
				case 't':
				case 'u':
				case 'v':
				case 'w':
				case 'x':
				case 'y':
				case 'z':
					{
						// line 177
						_type = TT.Id;
						value = Id();
					}
					break;
				case '\n':
				case '\r':
					{
						// line 179
						_type = TT.Newline;
						value = Newline();
					}
					break;
				case '/':
					{
						la1 = LA(1);
						if (la1 == '/') {
							// line 180
							_type = TT.SLComment;
							value = SLComment();
						} else if (la1 == '*') {
							la2 = LA(2);
							if (la2 != -1) {
								la3 = LA(3);
								if (la3 != -1) {
									// line 181
									_type = TT.MLComment;
									value = MLComment();
								} else
									value = Operator();
							} else
								value = Operator();
						} else
							value = Operator();
					}
					break;
				case '-':
					{
						la1 = LA(1);
						if (la1 >= '0' && la1 <= '9')
							goto matchNumber;
						else if (la1 == '.') {
							la2 = LA(2);
							if (la2 >= '0' && la2 <= '9')
								goto matchNumber;
							else
								value = Operator();
						} else
							value = Operator();
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
					goto matchNumber;
				case '.':
					{
						la1 = LA(1);
						if (la1 >= '0' && la1 <= '9')
							goto matchNumber;
						else
							value = Operator();
					}
					break;
				case '"':
					{
						la1 = LA(1);
						if (la1 == '"') {
							la2 = LA(2);
							if (la2 == '"') {
								la3 = LA(3);
								if (la3 != -1)
									goto matchTQString;
								else
									goto matchDQString;
							} else
								goto matchDQString;
						} else
							goto matchDQString;
					}
				case '\'':
					{
						la1 = LA(1);
						if (la1 == '\'') {
							la2 = LA(2);
							if (la2 == '\'') {
								la3 = LA(3);
								if (la3 != -1)
									goto matchTQString;
								else
									goto match13;
							} else
								goto match13;
						} else if (la1 == '\\')
							goto matchSQString;
						else if (SQOperator_set0.Contains(la1)) {
							la2 = LA(2);
							if (la2 == '\'')
								goto matchSQString;
							else {
								// line 186
								_type = TT.NormalOp;
								value = SQOperator();
							}
						} else if (la1 == '{') {
							la2 = LA(2);
							if (la2 == '\'')
								goto matchSQString;
							else {
								// line 196
								_type = TT.LTokenLiteral;
								Skip();
								Skip();
							}
						} else if (!(la1 == -1 || la1 == '\n' || la1 == '\r'))
							goto matchSQString;
						else
							goto error;
					}
					break;
				case ',':
					{
						// line 188
						_type = TT.Comma;
						Skip();
						// line 188
						value = GSymbol.Empty;
					}
					break;
				case ';':
					{
						// line 189
						_type = TT.Semicolon;
						Skip();
						// line 189
						value = GSymbol.Empty;
					}
					break;
				case '(':
					{
						// line 190
						_type = TT.LParen;
						Skip();
					}
					break;
				case ')':
					{
						// line 191
						_type = TT.RParen;
						Skip();
					}
					break;
				case '[':
					{
						// line 192
						_type = TT.LBrack;
						Skip();
					}
					break;
				case ']':
					{
						// line 193
						_type = TT.RBrack;
						Skip();
					}
					break;
				case '{':
					{
						// line 194
						_type = TT.LBrace;
						Skip();
					}
					break;
				case '}':
					{
						// line 195
						_type = TT.RBrace;
						Skip();
					}
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
					value = Operator();
					break;
				default:
					goto error;
				}
				break;
			matchKeyword:
				{
					// line 178
					_type = TT.Keyword;
					value = Keyword();
				}
				break;
			matchNumber:
				{
					// line 182
					_type = TT.Literal;
					value = Number();
				}
				break;
			matchTQString:
				{
					// line 183
					_type = TT.Literal;
					value = TQString();
				}
				break;
			matchDQString:
				{
					// line 184
					_type = TT.Literal;
					value = DQString();
				}
				break;
			matchSQString:
				{
					// line 185
					_type = TT.Literal;
					value = SQString();
				}
				break;
			match13:
				{
					// line 187
					_type = TT.Unknown;
					Skip();
					Skip();
				}
				break;
			match24:
				{
					// line 198
					_type = TT.At;
					Skip();
					// line 198
					value = GSymbol.Empty;
				}
				break;
			error:
				{
					Skip();
					// line 200
					_type = TT.Unknown;
				}
			} while (false);
			// line 202
			Debug.Assert(InputPosition > _startPosition);
			return new Token((int) _type, _startPosition, InputPosition - _startPosition, _style, value);
		}
		new public bool TDQStringLine()
		{
			int la0, la1, la2;
			// Line 212: nongreedy([^\$])*
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
			// Line 212: (Newline | ["] ["] ["])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				// line 212
				return false;
			} else {
				Match('"');
				Match('"');
				Match('"');
				// line 212
				return true;
			}
		}
		new public bool TSQStringLine()
		{
			int la0, la1, la2;
			// Line 215: nongreedy([^\$])*
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
			// Line 215: (Newline | ['] ['] ['])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				// line 215
				return false;
			} else {
				Match('\'');
				Match('\'');
				Match('\'');
				// line 215
				return true;
			}
		}
		new public bool MLCommentLine(ref int nested)
		{
			int la0, la1;
			// Line 218: greedy( &{nested > 0} [*] [/] / [/] [*] / [^\$\n\r*] / [*] &!([/]) )*
			for (;;) {
				la0 = LA0;
				if (la0 == '*') {
					if (nested > 0) {
						la1 = LA(1);
						if (la1 == '/') {
							Skip();
							Skip();
							// line 218
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
						// line 219
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
			// Line 223: (Newline | [*] [/])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				// line 223
				return false;
			} else {
				Match('*');
				Match('/');
				// line 223
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
			if (!Scan_HexDigits())
				return false;
			if (!TryMatch('P', 'p'))
				return false;
			if (!TryMatch(HexNumber_Test0_set0))
				return false;
			return true;
		}
		private bool Try_NormalId_Test0(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return NormalId_Test0();
		}
		private bool NormalId_Test0()
		{
			if (!TryMatch('\''))
				return false;
			if (!TryMatch('\''))
				return false;
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
	;
}
