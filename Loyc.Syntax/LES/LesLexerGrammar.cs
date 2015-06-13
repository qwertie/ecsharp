// Generated from LesLexerGrammar.les by LeMP custom tool. LLLPG version: 1.3.0.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
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
	public partial class LesLexer
	{
		static readonly Symbol _Comma = GSymbol.Get(",");
		static readonly Symbol _Semicolon = GSymbol.Get(";");
		static readonly Symbol _Colon = GSymbol.Get(":");
		new void Newline(bool ignoreIndent = false)
		{
			int la0;
			// Line 23: ([\r] ([\n])? | [\n])
			la0 = LA0;
			if (la0 == '\r') {
				Skip();
				// Line 23: ([\n])?
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
			// Line 28: ([^\$\n\r])*
			for (;;) {
				la0 = LA0;
				if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
					Skip();
				else
					break;
			}
			#line 29 "LesLexerGrammar.les"
			_value = WhitespaceTag.Value;
			#line default
		}
		void MLComment()
		{
			int la1;
			Skip();
			Skip();
			// Line 33: nongreedy( MLComment / Newline / [^\$] )*
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
			#line 34 "LesLexerGrammar.les"
			_value = WhitespaceTag.Value;
			#line default
		}
		void DecDigits()
		{
			int la0, la1;
			MatchRange('0', '9');
			// Line 39: ([0-9])*
			for (;;) {
				la0 = LA0;
				if (la0 >= '0' && la0 <= '9')
					Skip();
				else
					break;
			}
			// Line 39: ([_] [0-9] ([0-9])*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '_') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						Skip();
						Skip();
						// Line 39: ([0-9])*
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
			// Line 41: greedy(HexDigit)*
			for (;;) {
				la0 = LA0;
				if (HexDigit_set0.Contains(la0))
					HexDigit();
				else
					break;
			}
			// Line 41: greedy([_] HexDigit (HexDigit)*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '_') {
					la1 = LA(1);
					if (HexDigit_set0.Contains(la1)) {
						Skip();
						HexDigit();
						// Line 41: (HexDigit)*
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
			// Line 41: greedy(HexDigit)*
			for (;;) {
				la0 = LA0;
				if (HexDigit_set0.Contains(la0))
					{if (!Scan_HexDigit())
						return false;}
				else
					break;
			}
			// Line 41: greedy([_] HexDigit (HexDigit)*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '_') {
					la1 = LA(1);
					if (HexDigit_set0.Contains(la1)) {
						if (!TryMatch('_'))
							return false;
						if (!Scan_HexDigit())
							return false;
						// Line 41: (HexDigit)*
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
			Match('0', '1');
			// Line 42: ([01])*
			for (;;) {
				la0 = LA0;
				if (la0 >= '0' && la0 <= '1')
					Skip();
				else
					break;
			}
			// Line 42: ([_] [01] ([01])*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '_') {
					Skip();
					Match('0', '1');
					// Line 42: ([01])*
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
			#line 44 "LesLexerGrammar.les"
			_numberBase = 10;
			#line default
			// Line 45: ([.] DecDigits | DecDigits ([.] DecDigits)?)
			la0 = LA0;
			if (la0 == '.') {
				Skip();
				DecDigits();
				#line 45 "LesLexerGrammar.les"
				_isFloat = true;
				#line default
			} else {
				DecDigits();
				// Line 46: ([.] DecDigits)?
				la0 = LA0;
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						#line 46 "LesLexerGrammar.les"
						_isFloat = true;
						#line default
						Skip();
						DecDigits();
					}
				}
			}
			// Line 48: ([Ee] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'E' || la0 == 'e') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					#line 48 "LesLexerGrammar.les"
					_isFloat = true;
					#line default
					Skip();
					// Line 48: ([+\-])?
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
			#line 51 "LesLexerGrammar.les"
			_numberBase = 16;
			#line default
			// Line 52: (HexDigits)?
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
						#line 55 "LesLexerGrammar.les"
						_isFloat = true;
						#line default
						HexDigits();
					}
				}
			}
			// Line 56: ([Pp] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'P' || la0 == 'p') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					#line 56 "LesLexerGrammar.les"
					_isFloat = true;
					#line default
					Skip();
					// Line 56: ([+\-])?
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
			#line 59 "LesLexerGrammar.les"
			_numberBase = 2;
			#line default
			// Line 60: ([.] DecDigits | DecDigits ([.] DecDigits)?)
			la0 = LA0;
			if (la0 == '.') {
				Skip();
				DecDigits();
				#line 60 "LesLexerGrammar.les"
				_isFloat = true;
				#line default
			} else {
				DecDigits();
				// Line 61: ([.] DecDigits)?
				la0 = LA0;
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						#line 61 "LesLexerGrammar.les"
						_isFloat = true;
						#line default
						Skip();
						DecDigits();
					}
				}
			}
			// Line 63: ([Pp] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'P' || la0 == 'p') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					#line 63 "LesLexerGrammar.les"
					_isFloat = true;
					#line default
					Skip();
					// Line 63: ([+\-])?
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
			#line 66 "LesLexerGrammar.les"
			_isFloat = _isNegative = false;
			#line 66 "LesLexerGrammar.les"
			_typeSuffix = null;
			#line default
			// Line 67: ([\-])?
			la0 = LA0;
			if (la0 == '-') {
				Skip();
				#line 67 "LesLexerGrammar.les"
				_isNegative = true;
				#line default
			}
			// Line 68: ( HexNumber / BinNumber / DecNumber )
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
			// Line 69: ( [Ff] | [Dd] | [Mm] | [Ll] ([Uu])? | [Uu] ([Ll])? )?
			switch (LA0) {
			case 'F':
			case 'f':
				{
					Skip();
					#line 69 "LesLexerGrammar.les"
					_typeSuffix = _F;
					#line 69 "LesLexerGrammar.les"
					_isFloat = true;
					#line default
				}
				break;
			case 'D':
			case 'd':
				{
					Skip();
					#line 70 "LesLexerGrammar.les"
					_typeSuffix = _D;
					#line 70 "LesLexerGrammar.les"
					_isFloat = true;
					#line default
				}
				break;
			case 'M':
			case 'm':
				{
					Skip();
					#line 71 "LesLexerGrammar.les"
					_typeSuffix = _M;
					#line 71 "LesLexerGrammar.les"
					_isFloat = true;
					#line default
				}
				break;
			case 'L':
			case 'l':
				{
					Skip();
					#line 73 "LesLexerGrammar.les"
					_typeSuffix = _L;
					#line default
					// Line 73: ([Uu])?
					la0 = LA0;
					if (la0 == 'U' || la0 == 'u') {
						Skip();
						#line 73 "LesLexerGrammar.les"
						_typeSuffix = _UL;
						#line default
					}
				}
				break;
			case 'U':
			case 'u':
				{
					Skip();
					#line 74 "LesLexerGrammar.les"
					_typeSuffix = _U;
					#line default
					// Line 74: ([Ll])?
					la0 = LA0;
					if (la0 == 'L' || la0 == 'l') {
						Skip();
						#line 74 "LesLexerGrammar.les"
						_typeSuffix = _UL;
						#line default
					}
				}
				break;
			}
			#line 76 "LesLexerGrammar.les"
			ParseNumberValue();
			#line default
		}
		void SQString()
		{
			int la0, la1;
			#line 82 "LesLexerGrammar.les"
			_parseNeeded = false;
			#line default
			Skip();
			// Line 83: ([\\] [^\$] | [^\$\n\r'\\])*
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					la1 = LA(1);
					if (la1 != -1) {
						Skip();
						Skip();
						#line 83 "LesLexerGrammar.les"
						_parseNeeded = true;
						#line default
					} else
						break;
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '\''))
					Skip();
				else
					break;
			}
			// Line 84: (['] / )
			la0 = LA0;
			if (la0 == '\'')
				Skip();
			else {
				#line 84 "LesLexerGrammar.les"
				_parseNeeded = true;
				#line default
			}
			#line 85 "LesLexerGrammar.les"
			ParseSQStringValue();
			#line default
		}
		void DQString()
		{
			int la0, la1;
			#line 88 "LesLexerGrammar.les"
			_parseNeeded = false;
			#line default
			Skip();
			// Line 89: ([\\] [^\$] | [^\$\n\r"\\])*
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					la1 = LA(1);
					if (la1 != -1) {
						Skip();
						Skip();
						#line 89 "LesLexerGrammar.les"
						_parseNeeded = true;
						#line default
					} else
						break;
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '"'))
					Skip();
				else
					break;
			}
			// Line 90: (["] / )
			la0 = LA0;
			if (la0 == '"')
				Skip();
			else {
				#line 90 "LesLexerGrammar.les"
				_parseNeeded = true;
				#line default
			}
			#line 91 "LesLexerGrammar.les"
			ParseStringValue(false);
			#line default
		}
		void TQString()
		{
			int la0, la1, la2;
			_parseNeeded = true;
			_style = NodeStyle.Alternate;
			// Line 97: (["] ["] ["] nongreedy(Newline / [^\$])* ["] ["] ["] | ['] ['] ['] nongreedy(Newline / [^\$])* ['] ['] ['])
			la0 = LA0;
			if (la0 == '"') {
				Skip();
				Match('"');
				Match('"');
				// Line 97: nongreedy(Newline / [^\$])*
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
				#line 98 "LesLexerGrammar.les"
				_style |= NodeStyle.Alternate2;
				#line default
				Match('\'');
				Match('\'');
				Match('\'');
				// Line 99: nongreedy(Newline / [^\$])*
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
			#line 100 "LesLexerGrammar.les"
			ParseStringValue(true);
			#line default
		}
		void BQString2()
		{
			int la0;
			#line 104 "LesLexerGrammar.les"
			_parseNeeded = false;
			#line default
			Skip();
			// Line 105: ([\\] [^\$] | [^\$\n\r\\`])*
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					Skip();
					MatchExcept();
					#line 105 "LesLexerGrammar.les"
					_parseNeeded = true;
					#line default
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
			#line 107 "LesLexerGrammar.les"
			ParseBQStringValue();
			#line default
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
			// Line 114: (IdStartChar | IdExtLetter)
			la0 = LA0;
			if (NormalId_set0.Contains(la0))
				IdStartChar();
			else
				IdExtLetter();
			// Line 114: ( IdStartChar | [0-9] | ['] | IdExtLetter )*
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
			// Line 116: (BQString2 | (LettersOrPunc | IdExtLetter) (LettersOrPunc | IdExtLetter)*)
			la0 = LA0;
			if (la0 == '`')
				BQString2();
			else {
				// Line 116: (LettersOrPunc | IdExtLetter)
				la0 = LA0;
				if (FancyId_set0.Contains(la0))
					LettersOrPunc();
				else
					IdExtLetter();
				// Line 116: (LettersOrPunc | IdExtLetter)*
				for (;;) {
					la0 = LA0;
					if (FancyId_set0.Contains(la0))
						LettersOrPunc();
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
		}
		void Symbol()
		{
			#line 118 "LesLexerGrammar.les"
			_parseNeeded = false;
			#line default
			Skip();
			Skip();
			FancyId();
			#line 120 "LesLexerGrammar.les"
			ParseSymbolValue();
			#line default
		}
		static readonly HashSet<int> Id_set0 = NewSetOfRanges('#', '#', 'A', 'Z', '_', '_', 'a', 'z', 128, 65532);
		void Id()
		{
			int la0;
			#line 123 "LesLexerGrammar.les"
			_parseNeeded = false;
			#line default
			// Line 124: (NormalId | [@] FancyId)
			la0 = LA0;
			if (Id_set0.Contains(la0))
				NormalId();
			else {
				Match('@');
				FancyId();
				#line 124 "LesLexerGrammar.les"
				_parseNeeded = true;
				#line default
			}
			#line 125 "LesLexerGrammar.les"
			ParseIdValue();
			#line default
		}
		void LettersOrPunc()
		{
			Skip();
		}
		void OpChar()
		{
			Skip();
		}
		void Comma()
		{
			Skip();
			#line 134 "LesLexerGrammar.les"
			_value = _Comma;
			#line default
		}
		void Semicolon()
		{
			Skip();
			#line 135 "LesLexerGrammar.les"
			_value = _Semicolon;
			#line default
		}
		void Colon()
		{
			Match(':');
			#line 136 "LesLexerGrammar.les"
			_value = _Colon;
			#line default
		}
		void At()
		{
			Skip();
			#line 137 "LesLexerGrammar.les"
			_value = GSymbol.Empty;
			#line default
		}
		void CommentStart()
		{
			Match('/');
			Match('*', '/');
		}
		void Operator()
		{
			OpChar();
			// Line 139: (OpChar)*
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
					OpChar();
					break;
				default:
					goto stop;
				}
			}
		stop:;
			#line 139 "LesLexerGrammar.les"
			ParseNormalOp();
			#line default
		}
		void BackslashOp()
		{
			int la0, la1;
			Skip();
			// Line 140: (FancyId)?
			la0 = LA0;
			if (la0 == '`') {
				la1 = LA(1);
				if (!(la1 == -1 || la1 == '\n' || la1 == '\r'))
					FancyId();
			} else if (FancyId_set0.Contains(la0))
				FancyId();
			else if (la0 >= 128 && la0 <= 65532) {
				la0 = LA0;
				if (char.IsLetter((char) la0))
					FancyId();
			}
			#line 140 "LesLexerGrammar.les"
			ParseBackslashOp();
			#line default
		}
		void Shebang()
		{
			int la0;
			Skip();
			Skip();
			// Line 144: ([^\$\n\r])*
			for (;;) {
				la0 = LA0;
				if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
					Skip();
				else
					break;
			}
			// Line 144: (Newline)?
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r')
				Newline();
		}
		static readonly HashSet<int> NextToken_set0 = NewSetOfRanges('!', '!', '#', '\'', '*', '+', '-', ':', '<', '?', 'A', 'Z', '\\', '\\', '^', 'z', '|', '|', '~', '~');
		static readonly HashSet<int> NextToken_set1 = NewSetOfRanges('A', 'Z', '_', '_', 'a', 'z', 128, 65532);
		public override Maybe<Token> NextToken()
		{
			int la0, la1, la2;
			Spaces();
			_value = null;
			_style = 0;
			if (LA0 == -1) {
				return NoValue.Value;
			}
			_startPosition = InputPosition;
			// Line 158: ( &{InputPosition == 0} Shebang / Symbol / Id / Newline / SLComment / MLComment / Number / TQString / DQString / SQString / BQString / Comma / Semicolon / [(] / [)] / [[] / [\]] / [{] / [}] / At / BackslashOp / Operator / Colon )
			do {
				la0 = LA0;
				switch (la0) {
				case '#':
					{
						if (InputPosition == 0) {
							la1 = LA(1);
							if (la1 == '!') {
								#line 159 "LesLexerGrammar.les"
								_type = TT.Shebang;
								#line default
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
							if (NextToken_set0.Contains(la2))
								goto matchSymbol;
							else if (la2 >= 128 && la2 <= 65532) {
								la2 = LA(2);
								if (char.IsLetter((char) la2))
									goto matchSymbol;
								else
									goto matchAt;
							} else
								goto matchAt;
						} else if (la1 == '`') {
							la2 = LA(2);
							if (!(la2 == -1 || la2 == '\n' || la2 == '\r'))
								goto matchId;
							else
								goto matchAt;
						} else if (FancyId_set0.Contains(la1))
							goto matchId;
						else if (la1 >= 128 && la1 <= 65532) {
							la1 = LA(1);
							if (char.IsLetter((char) la1))
								goto matchId;
							else
								goto matchAt;
						} else
							goto matchAt;
					}
				case '\n':
				case '\r':
					{
						#line 162 "LesLexerGrammar.les"
						_type = TT.Newline;
						#line default
						Newline();
					}
					break;
				case '/':
					{
						la1 = LA(1);
						if (la1 == '/') {
							#line 163 "LesLexerGrammar.les"
							_type = TT.SLComment;
							#line default
							SLComment();
						} else if (la1 == '*') {
							la2 = LA(2);
							if (la2 != -1) {
								#line 164 "LesLexerGrammar.les"
								_type = TT.MLComment;
								#line default
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
						#line 169 "LesLexerGrammar.les"
						_type = TT.BQString;
						#line default
						BQString();
					}
					break;
				case ',':
					{
						#line 170 "LesLexerGrammar.les"
						_type = TT.Comma;
						#line default
						Comma();
					}
					break;
				case ';':
					{
						#line 171 "LesLexerGrammar.les"
						_type = TT.Semicolon;
						#line default
						Semicolon();
					}
					break;
				case '(':
					{
						#line 172 "LesLexerGrammar.les"
						_type = TT.LParen;
						#line default
						Skip();
					}
					break;
				case ')':
					{
						#line 173 "LesLexerGrammar.les"
						_type = TT.RParen;
						#line default
						Skip();
					}
					break;
				case '[':
					{
						#line 174 "LesLexerGrammar.les"
						_type = TT.LBrack;
						#line default
						Skip();
					}
					break;
				case ']':
					{
						#line 175 "LesLexerGrammar.les"
						_type = TT.RBrack;
						#line default
						Skip();
					}
					break;
				case '{':
					{
						#line 176 "LesLexerGrammar.les"
						_type = TT.LBrace;
						#line default
						Skip();
					}
					break;
				case '}':
					{
						#line 177 "LesLexerGrammar.les"
						_type = TT.RBrace;
						#line default
						Skip();
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
					if (NextToken_set1.Contains(la0))
						goto matchId;
					else {
						#line 182 "LesLexerGrammar.les"
						_value = null;
						#line default
						// Line 183: ([\$] | [^\$])
						la0 = LA0;
						if (la0 == -1) {
							Skip();
							#line 183 "LesLexerGrammar.les"
							_type = TT.EOF;
							#line default
						} else {
							Skip();
							#line 184 "LesLexerGrammar.les"
							_type = TT.Unknown;
							#line default
						}
					}
					break;
				}
				break;
			matchSymbol:
				{
					#line 160 "LesLexerGrammar.les"
					_type = TT.Symbol;
					#line default
					Symbol();
				}
				break;
			matchId:
				{
					#line 161 "LesLexerGrammar.les"
					_type = TT.Id;
					#line default
					Id();
				}
				break;
			matchNumber:
				{
					#line 165 "LesLexerGrammar.les"
					_type = TT.Number;
					#line default
					Number();
				}
				break;
			matchTQString:
				{
					#line 166 "LesLexerGrammar.les"
					_type = TT.String;
					#line default
					TQString();
				}
				break;
			matchDQString:
				{
					#line 167 "LesLexerGrammar.les"
					_type = TT.String;
					#line default
					DQString();
				}
				break;
			matchSQString:
				{
					#line 168 "LesLexerGrammar.les"
					_type = TT.SQString;
					#line default
					SQString();
				}
				break;
			matchAt:
				{
					#line 178 "LesLexerGrammar.les"
					_type = TT.At;
					#line default
					At();
				}
			} while (false);
			Debug.Assert(InputPosition > _startPosition);
			return (_current = new Token((int) _type, _startPosition, InputPosition - _startPosition, _style, _value));
		}
		public bool TDQStringLine()
		{
			int la0, la1, la2;
			// Line 199: nongreedy([^\$])*
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
			// Line 199: (Newline | ["] ["] ["])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				#line 199 "LesLexerGrammar.les"
				return false;
				#line default
			} else {
				Match('"');
				Match('"');
				Match('"');
				#line 199 "LesLexerGrammar.les"
				return true;
				#line default
			}
		}
		public bool TSQStringLine()
		{
			int la0, la1, la2;
			// Line 201: nongreedy([^\$])*
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
			// Line 201: (Newline | ['] ['] ['])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				#line 201 "LesLexerGrammar.les"
				return false;
				#line default
			} else {
				Match('\'');
				Match('\'');
				Match('\'');
				#line 201 "LesLexerGrammar.les"
				return true;
				#line default
			}
		}
		public bool MLCommentLine(ref int nested)
		{
			int la0, la1;
			// Line 204: greedy( &{nested > 0} [*] [/] / [/] [*] / [^\$\n\r*] / [*] &!([/]) )*
			for (;;) {
				la0 = LA0;
				if (la0 == '*') {
					if (nested > 0) {
						la1 = LA(1);
						if (la1 == '/') {
							Skip();
							Skip();
							#line 204 "LesLexerGrammar.les"
							nested--;
							#line default
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
						#line 205 "LesLexerGrammar.les"
						nested++;
						#line default
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
			// Line 209: (Newline | [*] [/])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				#line 209 "LesLexerGrammar.les"
				return false;
				#line default
			} else {
				Match('*');
				Match('/');
				#line 209 "LesLexerGrammar.les"
				return true;
				#line default
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
			// Line 54: ([0-9] / HexDigits [Pp] [+\-0-9])
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
