// Generated from Les3Lexer.ecs by LeMP custom tool. LeMP version: 2.7.0.0
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
using Loyc;	// optional (for IMessageSink, Symbol, etc.)
using Loyc.Collections;	// optional (many handy interfaces & classes)
using Loyc.Syntax.Lexing;	// For BaseLexer
namespace Loyc.Syntax.Les
{
	using TT = TokenType;	// Abbreviate TokenType as TT
	using P = LesPrecedence;
	using S = CodeSymbols;

	public partial class Les3Lexer {
		static readonly Symbol sy__apos_comma = (Symbol) "',", sy__apos_semi = (Symbol) "';", sy__aposx40 = (Symbol) "'@";
	
		void DotIndent()
		{
			int la0, la1;
			// Line 34: ([.] ([\t] | [ ] ([ ])*))*
			for (;;) {
				la0 = LA0;
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 == '\t' || la1 == ' ') {
						Skip();
						// Line 34: ([\t] | [ ] ([ ])*)
						la0 = LA0;
						if (la0 == '\t')
							Skip();
						else {
							Match(' ');
							// Line 34: ([ ])*
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
			// Line 37: ([\r] ([\n])? | [\n])
			la0 = LA0;
			if (la0 == '\r') {
				Skip();
				// Line 37: ([\n])?
				la0 = LA0;
				if (la0 == '\n')
					Skip();
			} else
				Match('\n');
			// line 38
			AfterNewline(ignoreIndent, skipIndent: false);
			// line 41
			return _brackStack.Last == TokenType.LBrace ? null : WhitespaceTag.Value;
		}
	
		object SLComment()
		{
			int la0, la1;
			Skip();
			Skip();
			// Line 43: nongreedy([^\$])*
			for (;;) {
				switch (LA0) {
				case '\\':
					{
						la1 = LA(1);
						if (la1 == '\\')
							goto stop;
						else
							Skip();
					}
					break;
				case -1: case '\n': case '\r':
					goto stop;
				default:
					Skip();
					break;
				}
			}
		stop:;
			// Line 43: ([\\] [\\] | [\$\n\r] => )
			la0 = LA0;
			if (la0 == '\\') {
				Skip();
				Match('\\');
			} else { }
			// line 44
			return WhitespaceTag.Value;
		}
	
		object MLComment()
		{
			int la1;
			Skip();
			Skip();
			// Line 46: nongreedy( MLComment / Newline / [^\$] )*
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
			// line 47
			return WhitespaceTag.Value;
		}
		static readonly HashSet<int> Number_set0 = NewSetOfRanges('#', '#', 'A', 'Z', '_', '_', 'a', 'z');
	
		object Number()
		{
			int la0, la1;
			UString suffix = default(UString);
			// Line 52: ([−])?
			la0 = LA0;
			if (la0 == '−')
				Skip();
			// Line 53: ( HexNumber / BinNumber / DecNumber )
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
			// line 54
			UString numberText = Text();
			// Line 55: (IdCore)?
			do {
				la0 = LA0;
				if (la0 == '`') {
					la1 = LA(1);
					if (!(la1 == -1 || la1 == '\n' || la1 == '\r'))
						goto matchIdCore;
				} else if (Number_set0.Contains(la0))
					goto matchIdCore;
				break;
			matchIdCore:
				{
					// line 55
					_startPosition = InputPosition;
					// line 56
					object boolOrNull = NoValue.Value;
					suffix = IdCore(ref boolOrNull);
					// line 58
					PrintErrorIfTypeMarkerIsKeywordLiteral(boolOrNull);
				}
			} while (false);
			// line 60
			return ParseLiteral2(suffix, numberText, true);
		}
	
		void DecDigits()
		{
			int la0, la1;
			MatchRange('0', '9');
			// Line 62: ([0-9])*
			for (;;) {
				la0 = LA0;
				if (la0 >= '0' && la0 <= '9')
					Skip();
				else
					break;
			}
			// Line 62: greedy(['_] [0-9] ([0-9])*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '\'' || la0 == '_') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						Skip();
						Skip();
						// Line 62: ([0-9])*
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
			// Line 62: greedy([_])?
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
			// Line 64: greedy([0-9A-Fa-f])*
			for (;;) {
				la0 = LA0;
				if (HexDigit_set0.Contains(la0))
					Skip();
				else
					break;
			}
			// Line 64: greedy(['_] [0-9A-Fa-f] greedy([0-9A-Fa-f])*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '\'' || la0 == '_') {
					la1 = LA(1);
					if (HexDigit_set0.Contains(la1)) {
						Skip();
						Skip();
						// Line 64: greedy([0-9A-Fa-f])*
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
			// Line 64: greedy([_])?
			la0 = LA0;
			if (la0 == '_')
				Skip();
		}
	
		void DecNumber()
		{
			int la0, la1;
			// Line 67: (DecDigits | [.] DecDigits => )
			la0 = LA0;
			if (la0 >= '0' && la0 <= '9')
				DecDigits();
			else { }
			// Line 68: ([.] DecDigits)?
			la0 = LA0;
			if (la0 == '.') {
				la1 = LA(1);
				if (la1 >= '0' && la1 <= '9') {
					Skip();
					DecDigits();
				}
			}
			// Line 69: greedy([Ee] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'E' || la0 == 'e') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					Skip();
					// Line 69: ([+\-])?
					la0 = LA0;
					if (la0 == '+' || la0 == '-')
						Skip();
					DecDigits();
				}
			}
		}
		static readonly HashSet<int> HexNumber_set0 = NewSetOfRanges('0', '9', 'A', 'F', 'P', 'P', 'a', 'f', 'p', 'p');
	
		void HexNumber()
		{
			int la0, la1;
			Skip();
			Skip();
			// Line 73: (HexDigits | [.] HexDigits => )
			la0 = LA0;
			if (HexDigit_set0.Contains(la0))
				HexDigits();
			else { }
			// Line 74: ([.] ([Pp] | [0-9A-Fa-f]) => [.] greedy(HexDigits)? greedy([Pp] ([+\-])? DecDigits)?)?
			la0 = LA0;
			if (la0 == '.') {
				la1 = LA(1);
				if (HexNumber_set0.Contains(la1)) {
					Skip();
					// Line 75: greedy(HexDigits)?
					la0 = LA0;
					if (HexDigit_set0.Contains(la0))
						HexDigits();
					// Line 76: greedy([Pp] ([+\-])? DecDigits)?
					la0 = LA0;
					if (la0 == 'P' || la0 == 'p') {
						la1 = LA(1);
						if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
							Skip();
							// Line 76: ([+\-])?
							la0 = LA0;
							if (la0 == '+' || la0 == '-')
								Skip();
							DecDigits();
						}
					}
				}
			}
		}
	
		void BinNumber()
		{
			int la0, la1;
			Skip();
			Skip();
			// Line 81: (DecDigits | [.] DecDigits => )
			la0 = LA0;
			if (la0 >= '0' && la0 <= '9')
				DecDigits();
			else { }
			// Line 82: ([.] DecDigits)?
			la0 = LA0;
			if (la0 == '.') {
				la1 = LA(1);
				if (la1 >= '0' && la1 <= '9') {
					Skip();
					DecDigits();
				}
			}
			// Line 83: greedy([Pp] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'P' || la0 == 'p') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					Skip();
					// Line 83: ([+\-])?
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
			// line 89
			bool parseNeeded = false;
			Skip();
			// Line 90: ([\\] [^\$] | [^\$\n\r'\\])
			la0 = LA0;
			if (la0 == '\\') {
				Skip();
				MatchExcept();
				// line 90
				parseNeeded = true;
			} else
				MatchExcept('\n', '\r', '\'', '\\');
			Match('\'');
			// line 91
			return ParseSQStringValue(parseNeeded);
		}
	
		object DQString()
		{
			int la0, la1;
			// line 94
			bool parseNeeded = false;
			Skip();
			// Line 95: ([\\] [^\$] | [^\$\n\r"\\])*
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					la1 = LA(1);
					if (la1 != -1) {
						Skip();
						Skip();
						// line 95
						parseNeeded = true;
					} else
						break;
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '"'))
					Skip();
				else
					break;
			}
			// Line 96: (["] / {..})
			la0 = LA0;
			if (la0 == '"')
				Skip();
			else
				// line 96
				parseNeeded = true;
			// line 97
			return ParseStringValue(parseNeeded, isTripleQuoted: false);
		}
	
		object TQString()
		{
			int la0, la1, la2;
			// line 100
			bool parseNeeded = true;
			// line 101
			_style = NodeStyle.TDQStringLiteral;
			// Line 102: (["] ["] ["] nongreedy(Newline / [^\$])* ["] ["] ["] | ['] ['] ['] nongreedy(Newline / [^\$])* ['] ['] ['])
			la0 = LA0;
			if (la0 == '"') {
				Skip();
				Match('"');
				Match('"');
				// Line 102: nongreedy(Newline / [^\$])*
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
				// line 103
				_style = NodeStyle.TQStringLiteral;
				Match('\'');
				Match('\'');
				Match('\'');
				// Line 104: nongreedy(Newline / [^\$])*
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
			// line 105
			return ParseStringValue(parseNeeded, isTripleQuoted: true);
		}
	
		void BQString(out bool parseNeeded)
		{
			int la0;
			// line 108
			parseNeeded = false;
			Skip();
			// Line 109: ([\\] [^\$] | [^\$\n\r\\`])*
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					Skip();
					MatchExcept();
					// line 109
					parseNeeded = true;
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '`'))
					Skip();
				else
					break;
			}
			// Line 110: ([`])
			la0 = LA0;
			if (la0 == '`')
				Skip();
			else {
				// line 110
				parseNeeded = true;
				Error(0, "Expected closing backquote");
			}
		}
	
		void LettersOrPunc()
		{
			Skip();
		}
	
		object Operator()
		{
			int la0, la1;
			object result = default(object);
			// Line 123: (([$])? ([!%&*+\-/:<-?^|~] | [.] [^0-9] =>  greedy([.])*) ([!%&*+\-/:<-?^|~] | [.] [^0-9] =>  greedy([.])*)* / [$])
			do {
				la0 = LA0;
				if (la0 == '$') {
					switch (LA(1)) {
					case '!': case '%': case '&': case '*':
					case '+': case '-': case '.': case '/':
					case ':': case '<': case '=': case '>':
					case '?': case '^': case '|': case '~':
						goto match1;
					default:
						Skip();
						break;
					}
				} else
					goto match1;
				break;
			match1:
				{
					// Line 123: ([$])?
					la0 = LA0;
					if (la0 == '$')
						Skip();
					// Line 123: ([!%&*+\-/:<-?^|~] | [.] [^0-9] =>  greedy([.])*)
					switch (LA0) {
					case '!': case '%': case '&': case '*':
					case '+': case '-': case '/': case ':':
					case '<': case '=': case '>': case '?':
					case '^': case '|': case '~':
						Skip();
						break;
					default:
						{
							Match('.');
							// Line 123: greedy([.])*
							for (;;) {
								la0 = LA0;
								if (la0 == '.')
									Skip();
								else
									break;
							}
						}
						break;
					}
					// Line 123: ([!%&*+\-/:<-?^|~] | [.] [^0-9] =>  greedy([.])*)*
					for (;;) {
						switch (LA0) {
						case '!': case '%': case '&': case '*':
						case '+': case '-': case '/': case ':':
						case '<': case '=': case '>': case '?':
						case '^': case '|': case '~':
							Skip();
							break;
						case '.':
							{
								la1 = LA(1);
								if (!(la1 >= '0' && la1 <= '9')) {
									Skip();
									// Line 123: greedy([.])*
									for (;;) {
										la0 = LA0;
										if (la0 == '.')
											Skip();
										else
											break;
									}
								} else
									goto stop;
							}
							break;
						default:
							goto stop;
						}
					}
				stop:;
				}
			} while (false);
			// line 124
			result = ParseOp(out _type);
			return result;
		}
		static readonly HashSet<int> SQOperator_set0 = NewSetOfRanges('!', '!', '#', '&', '*', '+', '-', ':', '<', '?', 'A', 'Z', '^', '_', 'a', 'z', '|', '|', '~', '~');
	
		object SQOperator()
		{
			int la0;
			Skip();
			// Line 127: (LettersOrPunc)*
			for (;;) {
				la0 = LA0;
				if (SQOperator_set0.Contains(la0))
					LettersOrPunc();
				else
					break;
			}
			// Line 129: (['])?
			la0 = LA0;
			if (la0 == '\'') {
				Skip();
				// line 129
				_type = TT.Literal;
				return ParseSQStringValue(true);
			}
			// line 130
			return (Symbol) Text();
		}
	
		object Keyword()
		{
			Skip();
			NormalId();
			int sp = _startPosition + 1;
			// line 140
			return (Symbol) ("#" + CharSource.Slice(sp, InputPosition - sp));
		}
	
		object Id()
		{
			int la0, la1;
			UString idtext = default(UString);
			object value = default(object);
			// line 143
			object boolOrNull = NoValue.Value;
			idtext = IdCore(ref boolOrNull);
			// Line 145: ((TQString / DQString))?
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
					var old_startPosition_10 = _startPosition;
					try {
						_startPosition = InputPosition;
						// Line 146: (TQString / DQString)
						la0 = LA0;
						if (la0 == '"') {
							la1 = LA(1);
							if (la1 == '"')
								value = TQString();
							else
								value = DQString();
						} else
							value = TQString();
						// line 148
						_type = TT.Literal;
						PrintErrorIfTypeMarkerIsKeywordLiteral(boolOrNull);
						return ParseLiteral2(idtext, value.ToString(), false);
					} finally {
						_startPosition = old_startPosition_10;
					}
				}
			} while (false);
			// line 153
			return boolOrNull != NoValue.Value ? boolOrNull : (Symbol) idtext;
		}
	
		void IdContChar()
		{
			int la0;
			// Line 157: ( [#A-Z_a-z] | [0-9] | ['] &!(['] [']) )
			la0 = LA0;
			if (Number_set0.Contains(la0))
				Skip();
			else if (la0 >= '0' && la0 <= '9')
				Skip();
			else {
				Match('\'');
				Check(!Try_IdContChar_Test0(0), "Did not expect ['] [']");
			}
		}
	
		bool Try_ScanIdContChar(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return ScanIdContChar();
		}
		bool ScanIdContChar()
		{
			int la0;
			// Line 157: ( [#A-Z_a-z] | [0-9] | ['] &!(['] [']) )
			la0 = LA0;
			if (Number_set0.Contains(la0))
				Skip();
			else if (la0 >= '0' && la0 <= '9')
				Skip();
			else {
				if (!TryMatch('\''))
					return false;
				if (Try_IdContChar_Test0(0))
					return false;
			}
			return true;
		}
		static readonly HashSet<int> NormalId_set0 = NewSetOfRanges('#', '#', '0', '9', 'A', 'Z', '_', '_', 'a', 'z');
	
		void NormalId()
		{
			int la0;
			Match(Number_set0);
			// Line 159: greedy(IdContChar)*
			for (;;) {
				la0 = LA0;
				if (NormalId_set0.Contains(la0))
					IdContChar();
				else if (la0 == '\'') {
					if (!Try_IdContChar_Test0(1))
						IdContChar();
					else
						break;
				} else
					break;
			}
		}
	
		UString IdCore(ref object boolOrNull)
		{
			int la0;
			UString result = default(UString);
			// Line 162: (BQString | NormalId)
			la0 = LA0;
			if (la0 == '`') {
				bool parseNeeded;
				BQString(out parseNeeded);
				// line 162
				result = ParseStringValue(parseNeeded, false);
			} else {
				NormalId();
				// line 164
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
	
		object SpecialLiteral()
		{
			int la0;
			Skip();
			Skip();
			LettersOrPunc();
			// Line 173: (LettersOrPunc)*
			for (;;) {
				la0 = LA0;
				if (SQOperator_set0.Contains(la0))
					LettersOrPunc();
				else
					break;
			}
			// line 173
			return ParseAtAtLiteral(Text());
		}
	
		object Shebang()
		{
			int la0;
			Check(InputPosition == 0, "Expected InputPosition == 0");
			Skip();
			Skip();
			// Line 178: ([^\$\n\r])*
			for (;;) {
				la0 = LA0;
				if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
					Skip();
				else
					break;
			}
			// Line 178: (Newline)?
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r')
				Newline();
			// line 179
			return WhitespaceTag.Value;
		}
	
		public override Maybe<Token> NextToken()
		{
			int la0, la1, la2, la3;
			object value = default(object);
			// Line 184: (Spaces / &{InputPosition == _lineStartAt} [.] [\t ] => DotIndent)?
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
			// line 186
			_startPosition = InputPosition;
			_style = 0;
			if (LA0 == -1) {
				return NoValue.Value;
			}
			// Line 192: ( Shebang / SpecialLiteral / [`] => Id / Id / Newline / SLComment / MLComment / Number / TQString / DQString / SQString / SQOperator / [,] / [;] / [(] / [)] / [[] / [\]] / [{] / [}] / [@] / Keyword / Operator )
			do {
				switch (LA0) {
				case '#':
					{
						la1 = LA(1);
						if (la1 == '!') {
							// line 192
							_type = TT.Shebang;
							value = Shebang();
						} else
							goto matchId;
					}
					break;
				case '@':
					{
						la1 = LA(1);
						if (la1 == '@') {
							la2 = LA(2);
							if (SQOperator_set0.Contains(la2)) {
								// line 193
								_type = TT.Literal;
								value = SpecialLiteral();
							} else
								goto match21;
						} else
							goto match21;
					}
					break;
				case '`':
					{
						// line 194
						_type = TT.BQId;
						value = Id();
					}
					break;
				case 'A': case 'B': case 'C': case 'D':
				case 'E': case 'F': case 'G': case 'H':
				case 'I': case 'J': case 'K': case 'L':
				case 'M': case 'N': case 'O': case 'P':
				case 'Q': case 'R': case 'S': case 'T':
				case 'U': case 'V': case 'W': case 'X':
				case 'Y': case 'Z': case '_': case 'a':
				case 'b': case 'c': case 'd': case 'e':
				case 'f': case 'g': case 'h': case 'i':
				case 'j': case 'k': case 'l': case 'm':
				case 'n': case 'o': case 'p': case 'q':
				case 'r': case 's': case 't': case 'u':
				case 'v': case 'w': case 'x': case 'y':
				case 'z':
					goto matchId;
				case '\n': case '\r':
					{
						// line 196
						_type = TT.Newline;
						value = Newline();
					}
					break;
				case '/':
					{
						la1 = LA(1);
						if (la1 == '/') {
							// line 197
							_type = TT.SLComment;
							value = SLComment();
						} else if (la1 == '*') {
							la2 = LA(2);
							if (la2 != -1) {
								la3 = LA(3);
								if (la3 != -1) {
									// line 198
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
				case '0': case '1': case '2': case '3':
				case '4': case '5': case '6': case '7':
				case '8': case '9': case '−':
					goto matchNumber;
				case '.':
					{
						la1 = LA(1);
						if (la1 >= '0' && la1 <= '9')
							goto matchNumber;
						else if (Number_set0.Contains(la1)) {
							if (InputPosition < 2 - 1 || !Try_ScanIdContChar(1 - 2)) {
								// line 213
								_type = TT.Keyword;
								value = Keyword();
							} else
								value = Operator();
						} else
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
									goto matchSQOperator;
							} else
								goto matchSQOperator;
						} else if (la1 == '\\') {
							la2 = LA(2);
							if (la2 != -1) {
								la3 = LA(3);
								if (la3 == '\'')
									goto matchSQString;
								else
									goto matchSQOperator;
							} else
								goto matchSQOperator;
						} else if (!(la1 == -1 || la1 == '\n' || la1 == '\r')) {
							la2 = LA(2);
							if (la2 == '\'')
								goto matchSQString;
							else
								goto matchSQOperator;
						} else
							goto matchSQOperator;
					}
				case ',':
					{
						// line 204
						_type = TT.Comma;
						Skip();
						// line 204
						value = sy__apos_comma;
					}
					break;
				case ';':
					{
						// line 205
						_type = TT.Semicolon;
						Skip();
						// line 205
						value = sy__apos_semi;
					}
					break;
				case '(':
					{
						// line 206
						_type = TT.LParen;
						Skip();
						// line 206
						_brackStack.Add(_type);
					}
					break;
				case ')':
					{
						// line 207
						_type = TT.RParen;
						Skip();
						while (_brackStack[_brackStack.Count - 1, default(TT)] == TT.LBrack)
							_brackStack.Pop();
						if (_brackStack.Count > 1 && _brackStack.Last == TT.LParen)
							_brackStack.Pop();
					}
					break;
				case '[':
					{
						// line 208
						_type = TT.LBrack;
						Skip();
						// line 208
						_brackStack.Add(_type);
					}
					break;
				case ']':
					{
						// line 209
						_type = TT.RBrack;
						Skip();
						while (_brackStack[_brackStack.Count - 1, default(TT)] == TT.LParen)
							_brackStack.Pop();
						if (_brackStack.Count > 1 && _brackStack.Last == TT.LBrack)
							_brackStack.Pop();
					}
					break;
				case '{':
					{
						// line 210
						_type = TT.LBrace;
						Skip();
						// line 210
						_brackStack.Add(_type);
					}
					break;
				case '}':
					{
						// line 211
						_type = TT.RBrace;
						Skip();
						while (_brackStack[_brackStack.Count - 1, default(TT)] != TT.LBrace)
							_brackStack.Pop();
						if (_brackStack.Count > 1 && true)
							_brackStack.Pop();
					}
					break;
				case '!': case '$': case '%': case '&':
				case '*': case '+': case '-': case ':':
				case '<': case '=': case '>': case '?':
				case '^': case '|': case '~':
					value = Operator();
					break;
				default:
					{
						MatchExcept();
						// line 215
						_type = TT.Unknown;
					}
					break;
				}
				break;
			matchId:
				{
					// line 195
					_type = TT.Id;
					value = Id();
				}
				break;
			matchNumber:
				{
					// line 199
					_type = TT.Literal;
					value = Number();
				}
				break;
			matchTQString:
				{
					// line 200
					_type = TT.Literal;
					value = TQString();
				}
				break;
			matchDQString:
				{
					// line 201
					_type = TT.Literal;
					value = DQString();
				}
				break;
			matchSQString:
				{
					// line 202
					_type = TT.Literal;
					value = SQString();
				}
				break;
			matchSQOperator:
				{
					// line 203
					_type = TT.SingleQuoteOp;
					value = SQOperator();
				}
				break;
			match21:
				{
					// line 212
					_type = TT.At;
					Skip();
					// line 212
					value = sy__aposx40;
				}
			} while (false);
			// line 217
			Debug.Assert(InputPosition > _startPosition);
			return new Token((int) _type, _startPosition, InputPosition - _startPosition, _style, value);
		}
	
		public bool TDQStringLine()
		{
			int la0, la1, la2;
			// Line 227: nongreedy([^\$])*
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
			// Line 227: (Newline | ["] ["] ["])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				// line 227
				return false;
			} else {
				Match('"');
				Match('"');
				Match('"');
				// line 227
				return true;
			}
		}
	
		public bool TSQStringLine()
		{
			int la0, la1, la2;
			// Line 230: nongreedy([^\$])*
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
			// Line 230: (Newline | ['] ['] ['])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				// line 230
				return false;
			} else {
				Match('\'');
				Match('\'');
				Match('\'');
				// line 230
				return true;
			}
		}
	
		public bool MLCommentLine(ref int nested)
		{
			int la0, la1;
			// Line 233: greedy( &{nested > 0} [*] [/] / [/] [*] / [^\$\n\r*] / [*] &!([/]) )*
			for (;;) {
				la0 = LA0;
				if (la0 == '*') {
					if (nested > 0) {
						la1 = LA(1);
						if (la1 == '/') {
							Skip();
							Skip();
							// line 233
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
						// line 234
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
			// Line 238: (Newline | [*] [/])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				// line 238
				return false;
			} else {
				Match('*');
				Match('/');
				// line 238
				return true;
			}
		}
	
		private bool Try_IdContChar_Test0(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return IdContChar_Test0();
		}
		private bool IdContChar_Test0()
		{
			if (!TryMatch('\''))
				return false;
			if (!TryMatch('\''))
				return false;
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
	} ;
}	// braces around the rest of the file are optional