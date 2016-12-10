// Generated from Les2LexerGrammar.les by LeMP custom tool. LeMP version: 2.3.0.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc;
using Loyc.Syntax;
using Loyc.Syntax.Lexing;

namespace Loyc.Syntax.Les
{
	using TT = TokenType;
	using S = CodeSymbols;

	public partial class Les2Lexer
	{
	
		void Newline(bool ignoreIndent = false)
		{
			int la0;
			// Line 26: ([\r] ([\n])? | [\n])
			la0 = LA0;
			if (la0 == '\r') {
				Skip();
				// Line 26: ([\n])?
				la0 = LA0;
				if (la0 == '\n')
					Skip();
			} else
				Match('\n');
			AfterNewline(ignoreIndent, true);
			_value = WhitespaceTag.Value;
		}
	
		void SLComment()
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
			_value = WhitespaceTag.Value;
		}
	
		void MLComment()
		{
			int la1;
			Skip();
			Skip();
			// Line 35: nongreedy( MLComment / Newline / [^\$] )*
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
				case '\n': case '\r':
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
			// line 36
			_value = WhitespaceTag.Value;
		}
	
	
		// Numbers ---------------------------------------------------------------
		void DecDigits()
		{
			int la0, la1;
			MatchRange('0', '9');
			// Line 41: ([0-9])*
			for (;;) {
				la0 = LA0;
				if (la0 >= '0' && la0 <= '9')
					Skip();
				else
					break;
			}
			// Line 41: ([_] [0-9] ([0-9])*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '_') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						Skip();
						Skip();
						// Line 41: ([0-9])*
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
			// Line 43: greedy(HexDigit)*
			for (;;) {
				la0 = LA0;
				if (HexDigit_set0.Contains(la0))
					HexDigit();
				else
					break;
			}
			// Line 43: greedy([_] HexDigit (HexDigit)*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '_') {
					la1 = LA(1);
					if (HexDigit_set0.Contains(la1)) {
						Skip();
						HexDigit();
						// Line 43: (HexDigit)*
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
			// Line 43: greedy(HexDigit)*
			for (;;) {
				la0 = LA0;
				if (HexDigit_set0.Contains(la0)){
					if (!Scan_HexDigit())
						return false;}
				else
					break;
			}
			// Line 43: greedy([_] HexDigit (HexDigit)*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '_') {
					la1 = LA(1);
					if (HexDigit_set0.Contains(la1)) {
						if (!TryMatch('_'))
							return false;
						if (!Scan_HexDigit())
							return false;
						// Line 43: (HexDigit)*
						for (;;) {
							la0 = LA0;
							if (HexDigit_set0.Contains(la0)){
								if (!Scan_HexDigit())
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
			Match('0', '1');
			// Line 44: ([01])*
			for (;;) {
				la0 = LA0;
				if (la0 >= '0' && la0 <= '1')
					Skip();
				else
					break;
			}
			// Line 44: ([_] [01] ([01])*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '_') {
					Skip();
					Match('0', '1');
					// Line 44: ([01])*
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
			// line 46
			_numberBase = 10;
			// Line 47: ([.] DecDigits | DecDigits ([.] DecDigits)?)
			la0 = LA0;
			if (la0 == '.') {
				Skip();
				DecDigits();
				// line 47
				_isFloat = true;
			} else {
				DecDigits();
				// Line 48: ([.] DecDigits)?
				la0 = LA0;
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						// line 48
						_isFloat = true;
						Skip();
						DecDigits();
					}
				}
			}
			// Line 50: ([Ee] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'E' || la0 == 'e') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					// line 50
					_isFloat = true;
					Skip();
					// Line 50: ([+\-])?
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
			// line 53
			_numberBase = 16;
			// Line 54: (HexDigits)?
			la0 = LA0;
			if (HexDigit_set0.Contains(la0))
				HexDigits();
			// Line 56: ([.] &(([0-9] / HexDigits [Pp] [+\-0-9])) HexDigits)?
			la0 = LA0;
			if (la0 == '.') {
				la1 = LA(1);
				if (HexDigit_set0.Contains(la1)) {
					if (Try_HexNumber_Test0(1)) {
						Skip();
						// line 57
						_isFloat = true;
						HexDigits();
					}
				}
			}
			// Line 58: ([Pp] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'P' || la0 == 'p') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					// line 58
					_isFloat = true;
					Skip();
					// Line 58: ([+\-])?
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
			// line 61
			_numberBase = 2;
			// Line 62: ([.] DecDigits | DecDigits ([.] DecDigits)?)
			la0 = LA0;
			if (la0 == '.') {
				Skip();
				DecDigits();
				// line 62
				_isFloat = true;
			} else {
				DecDigits();
				// Line 63: ([.] DecDigits)?
				la0 = LA0;
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						// line 63
						_isFloat = true;
						Skip();
						DecDigits();
					}
				}
			}
			// Line 65: ([Pp] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'P' || la0 == 'p') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					// line 65
					_isFloat = true;
					Skip();
					// Line 65: ([+\-])?
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
			// line 68
			_isFloat = _isNegative = false;
			_typeSuffix = null;
			// Line 69: ([\-])?
			la0 = LA0;
			if (la0 == '-') {
				Skip();
				// line 69
				_isNegative = true;
			}
			// Line 70: ( HexNumber / BinNumber / DecNumber )
			la0 = LA0;
			if (la0 == '0') {
				switch (LA(1)) {
				case 'X': case 'x':
					HexNumber();
					break;
				case 'B': case 'b':
					BinNumber();
					break;
				default:
					DecNumber();
					break;
				}
			} else
				DecNumber();
			// line 71
			var numberEndPosition = InputPosition;
			// Line 72: ( [Ff] | [Dd] | [Mm] | [Zz] | [Ll] ([Uu])? | [Uu] ([Ll])? )?
			switch (LA0) {
			case 'F': case 'f':
				{
					Skip();
					// line 72
					_typeSuffix = _F;
					_isFloat = true;
				}
				break;
			case 'D': case 'd':
				{
					Skip();
					// line 73
					_typeSuffix = _D;
					_isFloat = true;
				}
				break;
			case 'M': case 'm':
				{
					Skip();
					// line 74
					_typeSuffix = _M;
					_isFloat = true;
				}
				break;
			case 'Z': case 'z':
				{
					Skip();
					// line 75
					_typeSuffix = _Z;
				}
				break;
			case 'L': case 'l':
				{
					Skip();
					// line 77
					_typeSuffix = _L;
					// Line 77: ([Uu])?
					la0 = LA0;
					if (la0 == 'U' || la0 == 'u') {
						Skip();
						// line 77
						_typeSuffix = _UL;
					}
				}
				break;
			case 'U': case 'u':
				{
					Skip();
					// line 78
					_typeSuffix = _U;
					// Line 78: ([Ll])?
					la0 = LA0;
					if (la0 == 'L' || la0 == 'l') {
						Skip();
						// line 78
						_typeSuffix = _UL;
					}
				}
				break;
			}
			// line 80
			ParseNumberValue(numberEndPosition);
		}
	
	
		// Strings ---------------------------------------------------------------
		void SQString()
		{
			int la0, la1;
			// line 86
			_parseNeeded = false;
			Skip();
			// Line 87: ([\\] [^\$] | [^\$\n\r'\\])*
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					la1 = LA(1);
					if (la1 != -1) {
						Skip();
						Skip();
						// line 87
						_parseNeeded = true;
					} else
						break;
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '\''))
					Skip();
				else
					break;
			}
			// Line 88: (['] / )
			la0 = LA0;
			if (la0 == '\'')
				Skip();
			else
				// line 88
				_parseNeeded = true;
			// line 89
			ParseSQStringValue();
		}
	
		void DQString()
		{
			int la0, la1;
			// line 92
			_parseNeeded = false;
			Skip();
			// Line 93: ([\\] [^\$] | [^\$\n\r"\\])*
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					la1 = LA(1);
					if (la1 != -1) {
						Skip();
						Skip();
						// line 93
						_parseNeeded = true;
					} else
						break;
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '"'))
					Skip();
				else
					break;
			}
			// Line 94: (["] / )
			la0 = LA0;
			if (la0 == '"')
				Skip();
			else
				// line 94
				_parseNeeded = true;
			// line 95
			ParseStringValue(false);
		}
	
		void TQString()
		{
			int la0, la1, la2;
			_parseNeeded = true;
			_style = NodeStyle.TDQStringLiteral;
			// Line 100: (["] ["] ["] nongreedy(Newline / [^\$])* ["] ["] ["] | ['] ['] ['] nongreedy(Newline / [^\$])* ['] ['] ['])
			la0 = LA0;
			if (la0 == '"') {
				Skip();
				Match('"');
				Match('"');
				// Line 100: nongreedy(Newline / [^\$])*
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
					case '\n': case '\r':
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
				// line 101
				_style |= NodeStyle.TQStringLiteral;
				Match('\'');
				Match('\'');
				Match('\'');
				// Line 102: nongreedy(Newline / [^\$])*
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
					case '\n': case '\r':
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
			// line 103
			ParseStringValue(true);
		}
	
	
		void BQString()
		{
			int la0;
			// line 107
			_parseNeeded = false;
			Skip();
			// Line 108: ([\\] [^\$] | [^\$\n\r\\`])*
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					Skip();
					MatchExcept();
					// line 108
					_parseNeeded = true;
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '`'))
					Skip();
				else
					break;
			}
			Match('`');
		}
	
		void BQOperator()
		{
			BQString();
			// line 110
			_value = ParseBQStringValue();
		}
	
	
		// Identifiers and Symbols -----------------------------------------------
		void IdStartChar()
		{
			Skip();
		}
	
		// FIXME: 0x80..0xFFFC makes LLLPG make a HashSet<int> of unreasonable size.
		void IdExtLetter()
		{
			Check(char.IsLetter((char) LA0), "@char.IsLetter($LA->@char)");
			MatchRange(128, 65532);
		}
		static readonly HashSet<int> NormalId_set0 = NewSetOfRanges('#', '#', 'A', 'Z', '_', '_', 'a', 'z');
	
		void NormalId()
		{
			int la0;
			// Line 118: (IdStartChar | IdExtLetter)
			la0 = LA0;
			if (NormalId_set0.Contains(la0))
				IdStartChar();
			else
				IdExtLetter();
			// Line 118: ( IdStartChar | [0-9] | ['] | IdExtLetter )*
			for (;;) {
				la0 = LA0;
				if (NormalId_set0.Contains(la0))
					IdStartChar();
				else if (la0 >= '0' && la0 <= '9')
					Skip();
				else if (la0 == '\'')
					Skip();
				else if (la0 >= 128 && la0 <= 65532)
					IdExtLetter();
				else
					break;
			}
		}
		static readonly HashSet<int> FancyId_set0 = NewSetOfRanges('!', '!', '#', '\'', '*', '+', '-', ':', '<', '?', 'A', 'Z', '^', '_', 'a', 'z', '|', '|', '~', '~');
	
		void FancyId()
		{
			int la0;
			// Line 120: (BQString | (LettersOrPunc | IdExtLetter) (LettersOrPunc | IdExtLetter)*)
			la0 = LA0;
			if (la0 == '`')
				BQString();
			else {
				// Line 120: (LettersOrPunc | IdExtLetter)
				la0 = LA0;
				if (FancyId_set0.Contains(la0))
					LettersOrPunc();
				else
					IdExtLetter();
				// Line 120: (LettersOrPunc | IdExtLetter)*
				for (;;) {
					la0 = LA0;
					if (FancyId_set0.Contains(la0))
						LettersOrPunc();
					else if (la0 >= 128 && la0 <= 65532)
						IdExtLetter();
					else
						break;
				}
			}
		}
	
		void Symbol()
		{
			// line 122
			_parseNeeded = false;
			Skip();
			Skip();
			FancyId();
			// line 124
			ParseSymbolValue();
		}
		static readonly HashSet<int> Id_set0 = NewSetOfRanges('#', '#', 'A', 'Z', '_', '_', 'a', 'z', 128, 65532);
	
		void Id()
		{
			int la0;
			// Line 127: (NormalId | [@] FancyId)
			la0 = LA0;
			if (Id_set0.Contains(la0)) {
				NormalId();
				// line 127
				ParseIdValue(false);
			} else {
				Match('@');
				FancyId();
				// line 128
				ParseIdValue(true);
			}
		}
	
		void LettersOrPunc()
		{
			Skip();
		}
	
	
		// Punctuation & operators -----------------------------------------------
		void OpChar()
		{
			Skip();
		}
	
		void Comma()
		{
			Skip();
			// line 139
			_value = S.Comma;
		}
	
		void Semicolon()
		{
			Skip();
			// line 140
			_value = S.Semicolon;
		}
	
		void At()
		{
			Skip();
			// line 141
			_value = GSymbol.Empty;
		}
	
		void CommentStart()
		{
			Match('/');
			Match('*', '/');
		}
	
		void Operator()
		{
			OpChar();
			// Line 143: (OpChar)*
			for (;;) {
				switch (LA0) {
				case '!': case '$': case '%': case '&':
				case '*': case '+': case '-': case '.':
				case '/': case ':': case '<': case '=':
				case '>': case '?': case '^': case '|':
				case '~':
					OpChar();
					break;
				default:
					goto stop;
				}
			}
		stop:;
			// line 143
			ParseNormalOp();
		}
	
		//[private] token BackslashOp @{ '\\' FancyId? {ParseBackslashOp();} };
		void LParen()
		{
			var prev = LA(-1);
			_type = prev == ' ' || prev == '\t' ? TT.SpaceLParen : TT.LParen;
			Skip();
		}
	
	
		// Shebang ---------------------------------------------------------------
		void Shebang()
		{
			int la0;
			Skip();
			Skip();
			// Line 153: ([^\$\n\r])*
			for (;;) {
				la0 = LA0;
				if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
					Skip();
				else
					break;
			}
			// Line 153: (Newline)?
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r')
				Newline();
		}
	
		static readonly HashSet<int> NextToken_set0 = NewSetOfRanges('!', '!', '#', '\'', '*', '+', '-', ':', '<', '?', 'A', 'Z', '^', 'z', '|', '|', '~', '~', 128, 65532);
		static readonly HashSet<int> NextToken_set1 = NewSetOfRanges('!', '!', '#', '\'', '*', '+', '-', ':', '<', '?', 'A', 'Z', '^', '_', 'a', 'z', '|', '|', '~', '~', 128, 65532);
		static readonly HashSet<int> NextToken_set2 = NewSetOfRanges('A', 'Z', '_', '_', 'a', 'z', 128, 65532);
	
		// Token -----------------------------------------------------------------
		public override 
		Maybe<Token> NextToken()
		{
			int la0, la1, la2;
			// line 159
			Spaces();
			_value = null;
			_style = 0;
			if (LA0 == -1) {
				return NoValue.Value;
			}
			_startPosition = InputPosition;
			// Line 167: ( &{InputPosition == 0} Shebang / Symbol / Id / Newline / SLComment / MLComment / Number / TQString / DQString / SQString / BQOperator / Comma / Semicolon / LParen / [)] / [[] / [\]] / [{] / [}] / At / Operator )
			do {
				la0 = LA0;
				switch (la0) {
				case '#':
					{
						if (InputPosition == 0) {
							la1 = LA(1);
							if (la1 == '!') {
								// line 168
								_type = TT.Shebang;
								Shebang();
							} else
								goto matchId;
						} else
							goto matchId;
					}
					break;
				case '@':
					{
						la1 = LA(1);
						if (la1 == '@') {
							la2 = LA(2);
							if (NextToken_set0.Contains(la2)) {
								// line 169
								_type = TT.Literal;
								Symbol();
							} else
								goto matchAt;
						} else if (la1 == '`') {
							la2 = LA(2);
							if (!(la2 == -1 || la2 == '\n' || la2 == '\r'))
								goto matchId;
							else
								goto matchAt;
						} else if (NextToken_set1.Contains(la1))
							goto matchId;
						else
							goto matchAt;
					}
					break;
				case '\n': case '\r':
					{
						// line 171
						_type = TT.Newline;
						Newline();
					}
					break;
				case '/':
					{
						la1 = LA(1);
						if (la1 == '/') {
							// line 172
							_type = TT.SLComment;
							SLComment();
						} else if (la1 == '*') {
							la2 = LA(2);
							if (la2 != -1) {
								// line 173
								_type = TT.MLComment;
								MLComment();
							} else
								Operator();
						} else
							Operator();
					}
					break;
				case '-':
					{
						la1 = LA(1);
						if (la1 == '0')
							goto matchNumber;
						else if (la1 == '.') {
							la2 = LA(2);
							if (la2 >= '0' && la2 <= '9')
								goto matchNumber;
							else
								Operator();
						} else if (la1 >= '1' && la1 <= '9')
							goto matchNumber;
						else
							Operator();
					}
					break;
				case '0':
					goto matchNumber;
				case '.':
					{
						la1 = LA(1);
						if (la1 >= '0' && la1 <= '9')
							goto matchNumber;
						else
							Operator();
					}
					break;
				case '1': case '2': case '3': case '4':
				case '5': case '6': case '7': case '8':
				case '9':
					goto matchNumber;
				case '"':
					{
						la1 = LA(1);
						if (la1 == '"') {
							la2 = LA(2);
							if (la2 == '"')
								goto matchTQString;
							else
								goto matchDQString;
						} else
							goto matchDQString;
					}
				case '\'':
					{
						la1 = LA(1);
						if (la1 == '\'') {
							la2 = LA(2);
							if (la2 == '\'')
								goto matchTQString;
							else
								goto matchSQString;
						} else
							goto matchSQString;
					}
				case '`':
					{
						// line 178
						_type = TT.BQOperator;
						BQOperator();
					}
					break;
				case ',':
					{
						// line 179
						_type = TT.Comma;
						Comma();
					}
					break;
				case ';':
					{
						// line 180
						_type = TT.Semicolon;
						Semicolon();
					}
					break;
				case '(':
					{
						// line 181
						_type = TT.LParen;
						LParen();
					}
					break;
				case ')':
					{
						// line 182
						_type = TT.RParen;
						Skip();
					}
					break;
				case '[':
					{
						// line 183
						_type = TT.LBrack;
						Skip();
					}
					break;
				case ']':
					{
						// line 184
						_type = TT.RBrack;
						Skip();
					}
					break;
				case '{':
					{
						// line 185
						_type = TT.LBrace;
						Skip();
					}
					break;
				case '}':
					{
						// line 186
						_type = TT.RBrace;
						Skip();
					}
					break;
				case '!': case '$': case '%': case '&':
				case '*': case '+': case ':': case '<':
				case '=': case '>': case '?': case '^':
				case '|': case '~':
					Operator();
					break;
				default:
					if (NextToken_set2.Contains(la0))
						goto matchId;
					else {
						// line 189
						_value = null;
						// Line 190: ([\$] | [^\$])
						la0 = LA0;
						if (la0 == -1) {
							Skip();
							// line 190
							_type = TT.EOF;
						} else {
							Skip();
							// line 191
							_type = TT.Unknown;
						}
					}
					break;
				}
				break;
			matchId:
				{
					// line 170
					_type = TT.Id;
					Id();
				}
				break;
			matchNumber:
				{
					// line 174
					_type = TT.Literal;
					Number();
				}
				break;
			matchTQString:
				{
					// line 175
					_type = TT.Literal;
					TQString();
				}
				break;
			matchDQString:
				{
					// line 176
					_type = TT.Literal;
					DQString();
				}
				break;
			matchSQString:
				{
					// line 177
					_type = TT.Literal;
					SQString();
				}
				break;
			matchAt:
				{
					// line 187
					_type = TT.At;
					At();
				}
			} while (false);
			// line 193
			Debug.Assert(InputPosition > _startPosition);
			return _current = new Token((int) _type, _startPosition, InputPosition - _startPosition, _style, _value);
		}
	
	
		// Partial tokens used for syntax highlighting. An LES syntax highlighter
		// can record the token continued in each line (''', """ or /*) call one
		// of these rules to proces that token until it ends or the line ends.
		public 
		bool TDQStringLine()
		{
			int la0, la1, la2;
			// Line 203: nongreedy([^\$])*
			for (;;) {
				switch (LA0) {
				case '\n': case '\r':
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
			// Line 203: (Newline | ["] ["] ["])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				// line 203
				return false;
			} else {
				Match('"');
				Match('"');
				Match('"');
				// line 203
				return true;
			}
		}
	
		public 
		bool TSQStringLine()
		{
			int la0, la1, la2;
			// Line 205: nongreedy([^\$])*
			for (;;) {
				switch (LA0) {
				case '\n': case '\r':
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
			// Line 205: (Newline | ['] ['] ['])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				// line 205
				return false;
			} else {
				Match('\'');
				Match('\'');
				Match('\'');
				// line 205
				return true;
			}
		}
	
		public 
		bool MLCommentLine(ref int nested)
		{
			int la0, la1;
			// Line 209: greedy( &{nested > 0} [*] [/] / [/] [*] / [^\$\n\r*] / [*] &!([/]) )*
			for (;;) {
				la0 = LA0;
				if (la0 == '*') {
					if (nested > 0) {
						la1 = LA(1);
						if (la1 == '/') {
							Skip();
							Skip();
							// line 209
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
						// line 210
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
			// Line 214: (Newline | [*] [/])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				// line 214
				return false;
			} else {
				Match('*');
				Match('/');
				// line 214
				return true;
			}
		}
		static readonly HashSet<int> HexNumber_Test0_set0 = NewSetOfRanges('+', '+', '-', '-', '0', '9');
	
		private bool Try_HexNumber_Test0(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return HexNumber_Test0();
		}
		private bool HexNumber_Test0()
		{
			int la0;
			// Line 56: ([0-9] / HexDigits [Pp] [+\-0-9])
			la0 = LA0;
			if (la0 >= '0' && la0 <= '9'){
				if (!TryMatchRange('0', '9'))
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
	
		private bool Try_MLCommentLine_Test0(int lookaheadAmt) {
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