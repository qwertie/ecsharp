// Generated from Les2LexerGrammar.les by LeMP custom tool. LeMP version: 2.9.0.1
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
		static readonly Symbol sy__ = (Symbol) "_";

		void Newline(bool ignoreIndent = false)
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
			// line 28
			AfterNewline(ignoreIndent, true);
		}

		private void SLComment()
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

		private void MLComment()
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
						if (la1 == '/')
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

		private void DecDigits()
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

		private void HexDigit()
		{
			Match(HexDigit_set0);
		}
		private bool Scan_HexDigit()
		{
			if (!TryMatch(HexDigit_set0))
				return false;
			return true;
		}

		private void HexDigits()
		{
			int la0, la1;
			HexDigit();
			// Line 43: (HexDigit)*
			for (;;) {
				la0 = LA0;
				if (HexDigit_set0.Contains(la0))
					HexDigit();
				else
					break;
			}
			// Line 43: ([_] HexDigit (HexDigit)*)*
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
		private bool Scan_HexDigits()
		{
			int la0, la1;
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
			// Line 43: ([_] HexDigit (HexDigit)*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '_') {
					la1 = LA(1);
					if (HexDigit_set0.Contains(la1)) {
						Skip();
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

		private void DecNumber()
		{
			int la0, la1;
			// Line 45: ([.] DecDigits | DecDigits ([.] DecDigits)?)
			la0 = LA0;
			if (la0 == '.') {
				Skip();
				DecDigits();
			} else {
				DecDigits();
				// Line 46: ([.] DecDigits)?
				la0 = LA0;
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						Skip();
						DecDigits();
					}
				}
			}
			// Line 48: greedy([Ee] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'E' || la0 == 'e') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					Skip();
					// Line 48: ([+\-])?
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
			Skip();
			Skip();
			// Line 52: greedy(HexDigits)?
			la0 = LA0;
			if (HexDigit_set0.Contains(la0))
				HexDigits();
			// Line 54: ([.] &(([0-9] / HexDigits [Pp] [+\-0-9])) HexDigits)?
			la0 = LA0;
			if (la0 == '.') {
				la1 = LA(1);
				if (HexDigit_set0.Contains(la1)) {
					if (Try_HexNumber_Test0(1)) {
						Skip();
						HexDigits();
					}
				}
			}
			// Line 56: greedy([Pp] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'P' || la0 == 'p') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					Skip();
					// Line 56: ([+\-])?
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
			Skip();
			Skip();
			// Line 60: ([.] DecDigits | DecDigits ([.] DecDigits)?)
			la0 = LA0;
			if (la0 == '.') {
				Skip();
				DecDigits();
			} else {
				DecDigits();
				// Line 61: ([.] DecDigits)?
				la0 = LA0;
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						Skip();
						DecDigits();
					}
				}
			}
			// Line 63: greedy([Pp] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'P' || la0 == 'p') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					Skip();
					// Line 63: ([+\-])?
					la0 = LA0;
					if (la0 == '+' || la0 == '-')
						Skip();
					DecDigits();
				}
			}
		}
		static readonly HashSet<int> Number_set0 = NewSetOfRanges('#', '#', 'A', 'Z', '_', '_', 'a', 'z', 128, 65532);

		private void Number()
		{
			int la0;
			// Line 66: ([−])?
			la0 = LA0;
			if (la0 == '−')
				Skip();
			// Line 67: ( HexNumber / BinNumber / DecNumber )
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
			// line 68
			_textValue = Text();
			// Line 69: (NormalId / {..})
			la0 = LA0;
			if (Number_set0.Contains(la0)) {
				// line 69
				int suffixStart = InputPosition;
				NormalId();
				// line 71
				_value = IdToSymbol("_" + CharSource.Slice(suffixStart, InputPosition - suffixStart));
			} else
				// line 72
				_value = sy__;
		}
		
		// Strings ---------------------------------------------------------------

		private void SQString()
		{
			int la0, la1;
			// line 79
			_hasEscapes = false;
			Skip();
			// Line 80: ([\\] [^\$] | [^\$\n\r'\\])*
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					la1 = LA(1);
					if (la1 != -1) {
						Skip();
						Skip();
						// line 80
						_hasEscapes = true;
					} else
						break;
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '\''))
					Skip();
				else
					break;
			}
			// Line 81: (['] / {..})
			la0 = LA0;
			if (la0 == '\'')
				Skip();
			else
				// line 81
				_hasEscapes = true;
			// line 82
			UnescapeSQStringValue();
		}

		private void DQString()
		{
			int la0, la1;
			// line 85
			_hasEscapes = false;
			Skip();
			// Line 86: ([\\] [^\$] | [^\$\n\r"\\])*
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					la1 = LA(1);
					if (la1 != -1) {
						Skip();
						Skip();
						// line 86
						_hasEscapes = true;
					} else
						break;
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '"'))
					Skip();
				else
					break;
			}
			// Line 87: (["] / {..})
			la0 = LA0;
			if (la0 == '"')
				Skip();
			else
				// line 87
				_hasEscapes = true;
			// line 88
			UnescapeString(false);
		}

		private void TQString()
		{
			int la0, la1, la2;
			// line 91
			_hasEscapes = true;
			// Line 92: (["] ["] ["] nongreedy(Newline / [^\$])* ["] ["] ["] | ['] ['] ['] nongreedy(Newline / [^\$])* ['] ['] ['])
			la0 = LA0;
			if (la0 == '"') {
				// line 92
				_style = NodeStyle.TDQStringLiteral;
				Skip();
				Match('"');
				Match('"');
				// Line 93: nongreedy(Newline / [^\$])*
				for (;;) {
					switch (LA0) {
					case '"':
						{
							la1 = LA(1);
							if (la1 == '"') {
								la2 = LA(2);
								if (la2 == '"')
									goto stop;
								else
									Skip();
							} else
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
				// line 94
				_style |= NodeStyle.TQStringLiteral;
				Match('\'');
				Match('\'');
				Match('\'');
				// Line 95: nongreedy(Newline / [^\$])*
				for (;;) {
					switch (LA0) {
					case '\'':
						{
							la1 = LA(1);
							if (la1 == '\'') {
								la2 = LA(2);
								if (la2 == '\'')
									goto stop2;
								else
									Skip();
							} else
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
			// line 96
			UnescapeString(true);
		}

		private void BQString()
		{
			int la0;
			// line 100
			_hasEscapes = false;
			Skip();
			// Line 101: ([\\] [^\$] | [^\$\n\r\\`])*
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					Skip();
					MatchExcept();
					// line 101
					_hasEscapes = true;
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '`'))
					Skip();
				else
					break;
			}
			Match('`');
		}

		private void BQOperator()
		{
			BQString();
			// line 103
			_value = ParseBQStringValue();
		}
		
		// Identifiers and Symbols -----------------------------------------------

		private void IdStartChar()
		{
			Skip();
		}

		// FIXME: 0x80..0xFFFC makes LLLPG make a HashSet<int> of unreasonable size.
		private void IdExtLetter()
		{
			Check(char.IsLetter((char) LA0), "Expected @char .IsLetter($LA->@char)");
			MatchRange(128, 65532);
		}
		static readonly HashSet<int> NormalId_set0 = NewSetOfRanges('#', '#', 'A', 'Z', '_', '_', 'a', 'z');

		private void NormalId()
		{
			int la0;
			// Line 111: (IdStartChar | IdExtLetter)
			la0 = LA0;
			if (NormalId_set0.Contains(la0))
				IdStartChar();
			else
				IdExtLetter();
			// Line 112: ( IdStartChar | [0-9] | IdExtLetter | ['] &!{LA($LI) == '\'' && LA($LI + 1) == '\''} )*
			for (;;) {
				la0 = LA0;
				if (NormalId_set0.Contains(la0))
					IdStartChar();
				else if (la0 >= '0' && la0 <= '9')
					Skip();
				else if (la0 >= 128 && la0 <= 65532)
					IdExtLetter();
				else if (la0 == '\'') {
					if (!(LA(1) == '\'' && LA(1 + 1) == '\''))
						Skip();
					else
						break;
				} else
					break;
			}
		}
		static readonly HashSet<int> FancyId_set0 = NewSetOfRanges('!', '!', '#', '\'', '*', '+', '-', ':', '<', '?', 'A', 'Z', '^', '_', 'a', 'z', '|', '|', '~', '~');

		private void FancyId()
		{
			int la0;
			// Line 114: (BQString | (LettersOrPunc | IdExtLetter) (LettersOrPunc | IdExtLetter)*)
			la0 = LA0;
			if (la0 == '`')
				BQString();
			else {
				// Line 114: (LettersOrPunc | IdExtLetter)
				la0 = LA0;
				if (FancyId_set0.Contains(la0))
					LettersOrPunc();
				else
					IdExtLetter();
				// Line 114: (LettersOrPunc | IdExtLetter)*
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

		private void Symbol()
		{
			// line 116
			_hasEscapes = false;
			Skip();
			Skip();
			FancyId();
			// line 118
			UnescapeSymbolValue();
		}

		private void Id()
		{
			int la0, la1;
			// Line 121: (NormalId | [@] FancyId)
			la0 = LA0;
			if (Number_set0.Contains(la0)) {
				NormalId();
				// line 121
				ParseIdValue(false);
			} else {
				Match('@');
				FancyId();
				// line 122
				ParseIdValue(true);
			}
			// Line 124: ((TQString / DQString))?
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
					// line 125
					var old_startPosition_10 = _startPosition;
					try {
						_startPosition = InputPosition;
						_type = TT.Literal;
						// Line 129: (TQString / DQString)
						la0 = LA0;
						if (la0 == '"') {
							la1 = LA(1);
							if (la1 == '"')
								TQString();
							else
								DQString();
						} else
							TQString();
					} finally {
						_startPosition = old_startPosition_10;
					}
				}
			} while (false);
		}

		private void LettersOrPunc()
		{
			Skip();
		}
		
		// Punctuation & operators -----------------------------------------------

		private void OpChar()
		{
			Skip();
		}

		private void Comma()
		{
			Skip();
			// line 141
			_value = S.Comma;
		}

		private void Semicolon()
		{
			Skip();
			// line 142
			_value = S.Semicolon;
		}

		private void At()
		{
			Skip();
			// line 143
			_value = GSymbol.Empty;
		}

		private void CommentStart()
		{
			Match('/');
			Match('*', '/');
		}

		private void Operator()
		{
			OpChar();
			// Line 145: (OpChar)*
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
			// line 145
			ParseNormalOp();
		}

		//[private] token BackslashOp @{ '\\' FancyId? {ParseBackslashOp();} };
		private void LParen()
		{
			// line 148
			var prev = LA(-1);
			// line 149
			_type = prev == ' ' || prev == '\t' ? TT.SpaceLParen : TT.LParen;
			Skip();
		}
		
		// Shebang ---------------------------------------------------------------

		private void Shebang()
		{
			int la0;
			Skip();
			Skip();
			// Line 155: ([^\$\n\r])*
			for (;;) {
				la0 = LA0;
				if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
					Skip();
				else
					break;
			}
			// Line 155: (Newline)?
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r')
				Newline();
		}
		
		// Token -----------------------------------------------------------------
		static readonly HashSet<int> NextToken_set0 = NewSetOfRanges('!', '!', '#', '\'', '*', '+', '-', ':', '<', '?', 'A', 'Z', '^', 'z', '|', '|', '~', '~', 128, 65532);
		static readonly HashSet<int> NextToken_set1 = NewSetOfRanges('!', '!', '#', '\'', '*', '+', '-', ':', '<', '?', 'A', 'Z', '^', '_', 'a', 'z', '|', '|', '~', '~', 128, 65532);
		static readonly HashSet<int> NextToken_set2 = NewSetOfRanges('A', 'Z', '_', '_', 'a', 'z', '', '∑', 8723, 65532);

		public override 
		Maybe<Token> NextToken()
		{
			int la0, la1, la2;
			// line 161
			Spaces();
			_value = null;
			_textValue = default(UString);
			_style = 0;
			if (LA0 == -1) {
				return NoValue.Value;
			}
			_startPosition = InputPosition;
			// Line 170: ( &{InputPosition == 0} Shebang / Symbol / Number / Id / Newline / SLComment / MLComment / TQString / DQString / SQString / BQOperator / Comma / Semicolon / LParen / [)] / [[] / [\]] / [{] / [}] / At / Operator )
			do {
				la0 = LA0;
				switch (la0) {
				case '#':
					{
						if (InputPosition == 0) {
							la1 = LA(1);
							if (la1 == '!') {
								// line 171
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
								// line 172
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
				case '−':
					{
						la1 = LA(1);
						if (la1 == '0')
							goto matchNumber;
						else if (la1 == '.') {
							la2 = LA(2);
							if (la2 >= '0' && la2 <= '9')
								goto matchNumber;
							else
								goto matchId;
						} else if (la1 >= '1' && la1 <= '9')
							goto matchNumber;
						else
							goto matchId;
					}
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
				case '\n': case '\r':
					{
						// line 175
						_type = TT.Newline;
						Newline();
						// line 175
						_value = WhitespaceTag.Value;
					}
					break;
				case '/':
					{
						la1 = LA(1);
						if (la1 == '/') {
							// line 176
							_type = TT.SLComment;
							SLComment();
						} else if (la1 == '*') {
							la2 = LA(2);
							if (la2 != -1) {
								// line 177
								_type = TT.MLComment;
								MLComment();
							} else
								Operator();
						} else
							Operator();
					}
					break;
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
						// line 181
						_type = TT.BQOperator;
						BQOperator();
					}
					break;
				case ',':
					{
						// line 182
						_type = TT.Comma;
						Comma();
					}
					break;
				case ';':
					{
						// line 183
						_type = TT.Semicolon;
						Semicolon();
					}
					break;
				case '(':
					{
						// line 184
						_type = TT.LParen;
						LParen();
					}
					break;
				case ')':
					{
						// line 185
						_type = TT.RParen;
						Skip();
					}
					break;
				case '[':
					{
						// line 186
						_type = TT.LBrack;
						Skip();
					}
					break;
				case ']':
					{
						// line 187
						_type = TT.RBrack;
						Skip();
					}
					break;
				case '{':
					{
						// line 188
						_type = TT.LBrace;
						Skip();
					}
					break;
				case '}':
					{
						// line 189
						_type = TT.RBrace;
						Skip();
					}
					break;
				case '!': case '$': case '%': case '&':
				case '*': case '+': case '-': case ':':
				case '<': case '=': case '>': case '?':
				case '^': case '|': case '~':
					Operator();
					break;
				default:
					if (NextToken_set2.Contains(la0))
						goto matchId;
					else {
						// line 192
						_value = null;
						// Line 193: ([\$] | [^\$])
						la0 = LA0;
						if (la0 == -1) {
							Skip();
							// line 193
							_type = TT.EOF;
						} else {
							Skip();
							// line 194
							_type = TT.Unknown;
						}
					}
					break;
				}
				break;
			matchNumber:
				{
					// line 173
					_type = TT.Literal;
					Number();
				}
				break;
			matchId:
				{
					// line 174
					_type = TT.Id;
					Id();
				}
				break;
			matchTQString:
				{
					// line 178
					_type = TT.Literal;
					TQString();
				}
				break;
			matchDQString:
				{
					// line 179
					_type = TT.Literal;
					DQString();
				}
				break;
			matchSQString:
				{
					// line 180
					_type = TT.Literal;
					SQString();
				}
				break;
			matchAt:
				{
					// line 190
					_type = TT.At;
					At();
				}
			} while (false);
			// line 196
			Debug.Assert(InputPosition > _startPosition);
			return _current = new Token((int) _type, _startPosition, Text(), _style, _value, _textValue);
		}
		
		// Partial tokens used for syntax highlighting. An LES syntax highlighter
		// can record the token continued in each line (''', """ or /*) call one
		// of these rules to proces that token until it ends or the line ends.

		public 
		bool TDQStringLine()
		{
			int la0, la1, la2;
			// Line 206: nongreedy([^\$])*
			for (;;) {
				switch (LA0) {
				case '\n': case '\r':
					goto stop;
				case '"':
					{
						la1 = LA(1);
						if (la1 == '"') {
							la2 = LA(2);
							if (la2 == '"')
								goto stop;
							else
								Skip();
						} else
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
			// Line 206: (Newline | ["] ["] ["])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				// line 206
				return false;
			} else {
				Match('"');
				Match('"');
				Match('"');
				// line 206
				return true;
			}
		}

		public 
		bool TSQStringLine()
		{
			int la0, la1, la2;
			// Line 208: nongreedy([^\$])*
			for (;;) {
				switch (LA0) {
				case '\n': case '\r':
					goto stop;
				case '\'':
					{
						la1 = LA(1);
						if (la1 == '\'') {
							la2 = LA(2);
							if (la2 == '\'')
								goto stop;
							else
								Skip();
						} else
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
			// Line 208: (Newline | ['] ['] ['])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				// line 208
				return false;
			} else {
				Match('\'');
				Match('\'');
				Match('\'');
				// line 208
				return true;
			}
		}

		public 
		bool MLCommentLine(ref int nested)
		{
			int la0, la1;
			// Line 212: greedy( &{nested > 0} [*] [/] / [/] [*] / [^\$\n\r*] / [*] &!([/]) )*
			for (;;) {
				la0 = LA0;
				if (la0 == '*') {
					if (nested > 0) {
						la1 = LA(1);
						if (la1 == '/') {
							Skip();
							Skip();
							// line 212
							nested--;
						} else
							goto match4;
					} else {
						la1 = LA(1);
						if (la1 == '*')
							goto match4;
						else if (la1 == '/') {
							if (!Try_MLCommentLine_Test0(1))
								goto match4;
							else
								break;
						} else
							goto match4;
					}
				} else if (la0 == '/') {
					la1 = LA(1);
					if (la1 == '*') {
						Skip();
						Skip();
						// line 213
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
					Check(!Try_MLCommentLine_Test0(0), "Did not expect [/]");
				}
			}
			// Line 217: (Newline | [*] [/])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				// line 217
				return false;
			} else {
				Match('*');
				Match('/');
				// line 217
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
			// Line 54: ([0-9] / HexDigits [Pp] [+\-0-9])
			la0 = LA0;
			if (la0 >= '0' && la0 <= '9')
				Skip();
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