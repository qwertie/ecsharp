// Generated from Les3Lexer.ecs by LeMP custom tool. LeMP version: 2.8.4.0
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
	#pragma warning disable 162, 642
	using TT = TokenType;	// Abbreviate TokenType as TT
	using P = LesPrecedence;
	using S = CodeSymbols;

	public partial class Les3Lexer {
		static readonly Symbol sy_c = (Symbol) "c", sy__apos_commat = (Symbol) "'@";
	
		void DotIndent()
		{
			int la0, la1;
			// Line 35: ([.] ([\t] | [ ] ([ ])*))*
			for (;;) {
				la0 = LA0;
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 == '\t' || la1 == ' ') {
						Skip();
						// Line 35: ([\t] | [ ] ([ ])*)
						la0 = LA0;
						if (la0 == '\t')
							Skip();
						else {
							Match(' ');
							// Line 35: ([ ])*
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
			int ch_vert = 0;
			// Line 38: ([\r] ([\n])? | [\n])
			la0 = LA0;
			if (la0 == '\r') {
				Skip();
				// Line 38: ([\n])?
				la0 = LA0;
				if (la0 == '\n')
					Skip();
			} else
				Match('\n');
			// line 39
			AfterNewline(ignoreIndent, skipIndent: true);
			// Line 40: greedy([|])?
			la0 = LA0;
			if (la0 == '|')
				ch_vert = MatchAny();
			// line 44
			return _brackStack.Last == TokenType.LBrace && ch_vert != '|' ? null : WhitespaceTag.Value;
		}
	
		object SLComment()
		{
			int la0, la1;
			Skip();
			Skip();
			// Line 46: nongreedy([^\$])*
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
			// Line 46: ([\\] [\\] | [\$\n\r] => )
			la0 = LA0;
			if (la0 == '\\') {
				Skip();
				Match('\\');
			} else { }
			// line 47
			return WhitespaceTag.Value;
		}
	
		object MLComment()
		{
			int la1;
			Skip();
			Skip();
			// Line 50: nongreedy( MLComment / Newline / [^\$] )*
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
			// line 51
			return WhitespaceTag.Value;
		}
		static readonly HashSet<int> Number_set0 = NewSetOfRanges('#', '#', 'A', 'Z', '_', '_', 'a', 'z');
	
		object Number()
		{
			int la0;
			// Line 56: ([−])?
			la0 = LA0;
			if (la0 == '−')
				Skip();
			// Line 57: ( HexNumber / BinNumber / DecNumber )
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
			// line 58
			_textValue = Text();
			// line 59
			UString suffix = UString.Empty;
			// Line 60: (NormalId)?
			la0 = LA0;
			if (Number_set0.Contains(la0)) {
				// line 60
				int suffixStart = InputPosition;
				NormalId();
				// line 62
				suffix = Text(suffixStart);
			}
			// line 64
			return GetTypeMarkerSymbol(suffix, true);
		}
	
		void DecDigits()
		{
			int la0, la1;
			MatchRange('0', '9');
			// Line 66: ([0-9])*
			for (;;) {
				la0 = LA0;
				if (la0 >= '0' && la0 <= '9')
					Skip();
				else
					break;
			}
			// Line 66: greedy(['_] [0-9] ([0-9])*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '\'' || la0 == '_') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						Skip();
						Skip();
						// Line 66: ([0-9])*
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
	
		void HexDigits()
		{
			int la0, la1;
			Skip();
			// Line 68: greedy([0-9A-Fa-f])*
			for (;;) {
				la0 = LA0;
				if (HexDigit_set0.Contains(la0))
					Skip();
				else
					break;
			}
			// Line 68: greedy(['_] [0-9A-Fa-f] greedy([0-9A-Fa-f])*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '\'' || la0 == '_') {
					la1 = LA(1);
					if (HexDigit_set0.Contains(la1)) {
						Skip();
						Skip();
						// Line 68: greedy([0-9A-Fa-f])*
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
		}
	
		void DecNumber()
		{
			int la0, la1;
			// Line 71: (DecDigits | [.] DecDigits => )
			la0 = LA0;
			if (la0 >= '0' && la0 <= '9')
				DecDigits();
			else { }
			// Line 72: ([.] DecDigits)?
			la0 = LA0;
			if (la0 == '.') {
				la1 = LA(1);
				if (la1 >= '0' && la1 <= '9') {
					Skip();
					DecDigits();
				}
			}
			// Line 73: greedy([Ee] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'E' || la0 == 'e') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					Skip();
					// Line 73: ([+\-])?
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
			// Line 77: (HexDigits | [.] HexDigits => )
			la0 = LA0;
			if (HexDigit_set0.Contains(la0))
				HexDigits();
			else { }
			// Line 78: ([.] ([Pp] | [0-9A-Fa-f]) => [.] greedy(HexDigits)? greedy([Pp] ([+\-])? DecDigits)?)?
			la0 = LA0;
			if (la0 == '.') {
				la1 = LA(1);
				if (HexNumber_set0.Contains(la1)) {
					Skip();
					// Line 79: greedy(HexDigits)?
					la0 = LA0;
					if (HexDigit_set0.Contains(la0))
						HexDigits();
					// Line 80: greedy([Pp] ([+\-])? DecDigits)?
					la0 = LA0;
					if (la0 == 'P' || la0 == 'p') {
						la1 = LA(1);
						if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
							Skip();
							// Line 80: ([+\-])?
							la0 = LA0;
							if (la0 == '+' || la0 == '-')
								Skip();
							DecDigits();
						}
					}
				}
			}
			// line 82
			_style = NodeStyle.HexLiteral;
		}
	
		void BinNumber()
		{
			int la0, la1;
			Skip();
			Skip();
			// Line 85: (DecDigits | [.] DecDigits => )
			la0 = LA0;
			if (la0 >= '0' && la0 <= '9')
				DecDigits();
			else { }
			// Line 86: ([.] DecDigits)?
			la0 = LA0;
			if (la0 == '.') {
				la1 = LA(1);
				if (la1 >= '0' && la1 <= '9') {
					Skip();
					DecDigits();
				}
			}
			// Line 87: greedy([Pp] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'P' || la0 == 'p') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					Skip();
					// Line 87: ([+\-])?
					la0 = LA0;
					if (la0 == '+' || la0 == '-')
						Skip();
					DecDigits();
				}
			}
			// line 88
			_style = NodeStyle.BinaryLiteral;
		}
	
		void SQChar()
		{
			int la0;
			// line 98
			bool parseNeeded = false;
			Skip();
			// Line 99: ([\\] [^\$\n\r] ([^\$\n\r'])* | ((55296..56319) ((56320..57343))? / [^\$\n\r'\\]))
			la0 = LA0;
			if (la0 == '\\') {
				Skip();
				MatchExcept('\n', '\r');
				// Line 99: ([^\$\n\r'])*
				for (;;) {
					la0 = LA0;
					if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '\''))
						Skip();
					else
						break;
				}
				// line 99
				parseNeeded = true;
			} else {
				// Line 95: ((55296..56319) ((56320..57343))? / [^\$\n\r'\\])
				la0 = LA0;
				if (la0 >= 55296 && la0 <= 56319) {
					Skip();
					// Line 95: ((56320..57343))?
					la0 = LA0;
					if (la0 >= 56320 && la0 <= 57343)
						Skip();
				} else
					MatchExcept('\n', '\r', '\'', '\\');
			}
			Match('\'');
			// line 100
			_textValue = UnescapeSQStringValue(parseNeeded);
		}
	
		void DQString()
		{
			int la0, la1;
			// line 103
			bool parseNeeded = false;
			Skip();
			// Line 104: ([\\] [^\$\n\r] | [^\$\n\r"\\])*
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					la1 = LA(1);
					if (!(la1 == -1 || la1 == '\n' || la1 == '\r')) {
						Skip();
						Skip();
						// line 104
						parseNeeded = true;
					} else
						break;
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '"'))
					Skip();
				else
					break;
			}
			// Line 105: (["] / {..})
			la0 = LA0;
			if (la0 == '"')
				Skip();
			else
				// line 105
				parseNeeded = true;
			// line 106
			_textValue = ParseStringValue(parseNeeded, isTripleQuoted: false);
		}
	
		void TQString()
		{
			int la0, la1, la2;
			// line 109
			bool parseNeeded = true;
			// line 110
			_style = NodeStyle.TDQStringLiteral;
			// Line 111: (["] ["] ["] nongreedy(Newline / [^\$])* ["] ["] ["] | ['] ['] ['] nongreedy(Newline / [^\$])* ['] ['] ['])
			la0 = LA0;
			if (la0 == '"') {
				Skip();
				Match('"');
				Match('"');
				// Line 111: nongreedy(Newline / [^\$])*
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
				// line 112
				_style = NodeStyle.TQStringLiteral;
				Match('\'');
				Match('\'');
				Match('\'');
				// Line 113: nongreedy(Newline / [^\$])*
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
			// line 114
			_textValue = ParseStringValue(parseNeeded, isTripleQuoted: true);
		}
	
		void BQString(out bool parseNeeded)
		{
			int la0;
			// line 117
			parseNeeded = false;
			Skip();
			// Line 118: ([\\] [^\$\n\r] | [^\$\n\r\\`])*
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					Skip();
					MatchExcept('\n', '\r');
					// line 118
					parseNeeded = true;
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '`'))
					Skip();
				else
					break;
			}
			// Line 119: ([`])
			la0 = LA0;
			if (la0 == '`')
				Skip();
			else {
				// line 119
				parseNeeded = true;
				Error(0, "Expected closing backquote");
			}
		}
		static readonly HashSet<int> LettersOrPunc_set0 = NewSetOfRanges('!', '!', '#', '&', '*', '+', '-', ':', '<', '?', 'A', 'Z', '^', '_', 'a', 'z', '|', '|', '~', '~');
	
		void LettersOrPunc()
		{
			Match(LettersOrPunc_set0);
		}
	
		object Operator()
		{
			int la0, la1;
			object result = default(object);
			// Line 132: (([$])? ([!%&*+\-/:<-?^|~] | [.] [^0-9] =>  greedy([.])*) ([!%&*+\-/:<-?^|~] | [.] [^0-9] =>  greedy([.])*)* / [$])
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
					// Line 132: ([$])?
					la0 = LA0;
					if (la0 == '$')
						Skip();
					// Line 132: ([!%&*+\-/:<-?^|~] | [.] [^0-9] =>  greedy([.])*)
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
							// Line 132: greedy([.])*
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
					// Line 132: ([!%&*+\-/:<-?^|~] | [.] [^0-9] =>  greedy([.])*)*
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
									// Line 132: greedy([.])*
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
			// line 133
			result = ParseOp(out _type);
			return result;
		}
	
		object SQOperator()
		{
			int la0;
			Skip();
			// Line 137: ([#A-Z_a-z] ([#A-Z_a-z])*)?
			la0 = LA0;
			if (Number_set0.Contains(la0)) {
				Skip();
				// Line 137: ([#A-Z_a-z])*
				for (;;) {
					la0 = LA0;
					if (Number_set0.Contains(la0))
						Skip();
					else
						break;
				}
				// line 137
				_type = TT.PreOrSufOp;
			}
			// line 138
			return (Symbol) Text();
		}
		static readonly HashSet<int> TreeDefOrBackRef_set0 = NewSetOfRanges('#', '#', '0', '9', 'A', 'Z', '_', '_', 'a', 'z');
	
		object TreeDefOrBackRef()
		{
			int la0;
			Skip();
			Skip();
			// line 142
			int idStart = InputPosition;
			IdContChar();
			// Line 143: greedy(IdContChar)*
			for (;;) {
				la0 = LA0;
				if (TreeDefOrBackRef_set0.Contains(la0))
					IdContChar();
				else if (la0 == '\'') {
					if (!(LA(1) == '\'' && LA(1 + 1) == '\''))
						IdContChar();
					else
						break;
				} else
					break;
			}
			// line 144
			return null;
		}
	
		object Id()
		{
			int la0, la1, la2;
			// line 150
			UString idText;
			// Line 151: (BQString | NormalId)
			la0 = LA0;
			if (la0 == '`') {
				bool parseNeeded;
				BQString(out parseNeeded);
				// line 151
				idText = ParseStringValue(parseNeeded, false);
			} else {
				NormalId();
				// line 152
				idText = Text();
			}
			// Line 155: ((TQString / DQString) / {..})
			do {
				la0 = LA0;
				if (la0 == '"')
					goto match1;
				else if (la0 == '\'') {
					la1 = LA(1);
					if (la1 == '\'') {
						la2 = LA(2);
						if (la2 == '\'')
							goto match1;
						else
							goto match2;
					} else
						goto match2;
				} else
					goto match2;
			match1:
				{
					// line 156
					var old_startPosition_10 = _startPosition;
					try {
						_startPosition = InputPosition;
						_type = TT.Literal;
						// Line 160: (TQString / DQString)
						la0 = LA0;
						if (la0 == '"') {
							la1 = LA(1);
							if (la1 == '"') {
								la2 = LA(2);
								if (la2 == '"')
									TQString();
								else
									DQString();
							} else
								DQString();
						} else
							TQString();
						// line 161
						return GetTypeMarkerSymbol(idText, false);
					} finally {
						_startPosition = old_startPosition_10;
					}
				}
				break;
			match2:
				{
					// line 163
					if (_type != TT.BQId) {
						if (idText == "true") {
							_type = TT.Literal;
							return G.BoxedTrue;
						}
						if (idText == "false") {
							_type = TT.Literal;
							return G.BoxedFalse;
						}
						if (idText == "null") {
							_type = TT.Literal;
							return null;
						}
					}
					return (Symbol) idText;
				}
			} while (false);
		}
	
		void IdContChar()
		{
			int la0;
			// Line 175: ( [#A-Z_a-z] | [0-9] | ['] &!{LA($LI) == '\'' && LA($LI + 1) == '\''} )
			la0 = LA0;
			if (Number_set0.Contains(la0))
				Skip();
			else if (la0 >= '0' && la0 <= '9')
				Skip();
			else {
				Match('\'');
				Check(!(LA(0) == '\'' && LA(0 + 1) == '\''), "Did not expect LA($LI) == '\\'' && LA($LI + 1) == '\\''");
			}
		}
	
		bool Try_ScanIdContChar(int lookaheadAmt) {
			using (new SavePosition(this, lookaheadAmt))
				return ScanIdContChar();
		}
		bool ScanIdContChar()
		{
			int la0;
			// Line 175: ( [#A-Z_a-z] | [0-9] | ['] &!{LA($LI) == '\'' && LA($LI + 1) == '\''} )
			la0 = LA0;
			if (Number_set0.Contains(la0))
				Skip();
			else if (la0 >= '0' && la0 <= '9')
				Skip();
			else {
				if (!TryMatch('\''))
					return false;
				if (LA(0) == '\'' && LA(0 + 1) == '\'')
					return false;
			}
			return true;
		}
	
		private void NormalId()
		{
			int la0;
			Match(Number_set0);
			// Line 177: greedy(IdContChar)*
			for (;;) {
				la0 = LA0;
				if (TreeDefOrBackRef_set0.Contains(la0))
					IdContChar();
				else if (la0 == '\'') {
					if (!(LA(1) == '\'' && LA(1 + 1) == '\''))
						IdContChar();
					else
						break;
				} else
					break;
			}
		}
	
		object Shebang()
		{
			int la0;
			Check(InputPosition == 0, "Expected InputPosition == 0");
			Skip();
			Skip();
			// Line 182: ([^\$\n\r])*
			for (;;) {
				la0 = LA0;
				if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
					Skip();
				else
					break;
			}
			// Line 182: (Newline)?
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r')
				Newline();
			// line 183
			return WhitespaceTag.Value;
		}
	
		public override Maybe<Token> NextToken()
		{
			int la0, la1, la2, la3;
			object value = default(object);
			// Line 188: (Spaces / &{InputPosition == _lineStartAt} [.] [\t ] => DotIndent)?
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
			// line 190
			_startPosition = InputPosition;
			_style = 0;
			_textValue = default(UString);
			if (LA0 == -1) {
				return NoValue.Value;
			}
			// Line 197: ( Shebang / [`] => Id / Id / Newline / SLComment / MLComment / Number / TQString / DQString / SQChar / SQOperator / [,] / [;] / [(] / [)] / [[] / [\]] / [{] / [}] / [@] [.] [#A-Z_a-z] => TreeDefOrBackRef / TreeDefOrBackRef / [@] / Operator )
			do {
				switch (LA0) {
				case '#':
					{
						la1 = LA(1);
						if (la1 == '!') {
							// line 197
							_type = TT.Shebang;
							value = Shebang();
						} else
							goto matchId;
					}
					break;
				case '`':
					{
						// line 198
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
						// line 200
						_type = TT.Newline;
						value = Newline();
					}
					break;
				case '/':
					{
						la1 = LA(1);
						if (la1 == '/') {
							// line 201
							_type = TT.SLComment;
							value = SLComment();
						} else if (la1 == '*') {
							// line 202
							_type = TT.MLComment;
							value = MLComment();
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
									goto matchSQOperator;
							} else
								goto matchSQOperator;
						} else if (la1 == '\\') {
							la2 = LA(2);
							if (!(la2 == -1 || la2 == '\n' || la2 == '\r')) {
								la3 = LA(3);
								if (!(la3 == -1 || la3 == '\n' || la3 == '\r'))
									goto matchSQChar;
								else
									goto matchSQOperator;
							} else
								goto matchSQOperator;
						} else if (la1 >= 55296 && la1 <= 56319) {
							la2 = LA(2);
							if (la2 >= 56320 && la2 <= 57343) {
								la3 = LA(3);
								if (la3 == '\'')
									goto matchSQChar;
								else
									goto matchSQOperator;
							} else if (la2 == '\'')
								goto matchSQChar;
							else
								goto matchSQOperator;
						} else if (!(la1 == -1 || la1 == '\n' || la1 == '\r')) {
							la2 = LA(2);
							if (la2 == '\'')
								goto matchSQChar;
							else
								goto matchSQOperator;
						} else
							goto matchSQOperator;
					}
				case ',':
					{
						// line 208
						_type = TT.Comma;
						Skip();
						// line 208
						value = S.Comma;
					}
					break;
				case ';':
					{
						// line 209
						_type = TT.Semicolon;
						Skip();
						// line 209
						value = S.Semicolon;
					}
					break;
				case '(':
					{
						// line 210
						_type = TT.LParen;
						Skip();
						// line 210
						_brackStack.Add(_type);
					}
					break;
				case ')':
					{
						// line 211
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
						// line 212
						_type = TT.LBrack;
						Skip();
						// line 212
						_brackStack.Add(_type);
					}
					break;
				case ']':
					{
						// line 213
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
						// line 214
						_type = TT.LBrace;
						Skip();
						// line 214
						_brackStack.Add(_type);
					}
					break;
				case '}':
					{
						// line 215
						_type = TT.RBrace;
						Skip();
						while (_brackStack[_brackStack.Count - 1, default(TT)] != TT.LBrace)
							_brackStack.Pop();
						if (_brackStack.Count > 1 && true)
							_brackStack.Pop();
					}
					break;
				case '@':
					{
						la1 = LA(1);
						if (la1 == '.') {
							la2 = LA(2);
							if (Number_set0.Contains(la2)) {
								// line 216
								_type = TT.TreeDef;
								value = TreeDefOrBackRef();
							} else if (la2 >= '0' && la2 <= '9')
								goto matchTreeDefOrBackRef;
							else if (la2 == '\'') {
								if (!(LA(3) == '\'' && LA(3 + 1) == '\''))
									goto matchTreeDefOrBackRef;
								else
									goto match22;
							} else
								goto match22;
						} else if (la1 == '@') {
							la2 = LA(2);
							if (TreeDefOrBackRef_set0.Contains(la2))
								goto matchTreeDefOrBackRef;
							else if (la2 == '\'') {
								if (!(LA(3) == '\'' && LA(3 + 1) == '\''))
									goto matchTreeDefOrBackRef;
								else
									goto match22;
							} else
								goto match22;
						} else
							goto match22;
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
						// line 220
						_type = TT.Unknown;
					}
					break;
				}
				break;
			matchId:
				{
					// line 199
					_type = TT.Id;
					value = Id();
				}
				break;
			matchNumber:
				{
					// line 203
					_type = TT.Literal;
					value = Number();
				}
				break;
			matchTQString:
				{
					// line 204
					_type = TT.Literal;
					TQString();
					// line 204
					value = GSymbol.Empty;
				}
				break;
			matchDQString:
				{
					// line 205
					_type = TT.Literal;
					DQString();
					// line 205
					value = GSymbol.Empty;
				}
				break;
			matchSQChar:
				{
					// line 206
					_type = TT.Literal;
					SQChar();
					// line 206
					value = sy_c;
				}
				break;
			matchSQOperator:
				{
					// line 207
					_type = TT.SingleQuote;
					value = SQOperator();
				}
				break;
			matchTreeDefOrBackRef:
				{
					// line 217
					_type = TT.BackRef;
					value = TreeDefOrBackRef();
				}
				break;
			match22:
				{
					// line 218
					_type = TT.At;
					Skip();
					// line 218
					value = sy__apos_commat;
				}
			} while (false);
			// line 222
			Debug.Assert(InputPosition > _startPosition);
			return new Token((int) _type, _startPosition, Text(), _style, value, _textValue);
		}
	
		public bool TDQStringLine()
		{
			int la0, la1, la2;
			// Line 232: nongreedy([^\$])*
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
			// Line 232: (Newline | ["] ["] ["])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				// line 232
				return false;
			} else {
				Match('"');
				Match('"');
				Match('"');
				// line 232
				return true;
			}
		}
	
		public bool TSQStringLine()
		{
			int la0, la1, la2;
			// Line 235: nongreedy([^\$])*
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
			// Line 235: (Newline | ['] ['] ['])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				// line 235
				return false;
			} else {
				Match('\'');
				Match('\'');
				Match('\'');
				// line 235
				return true;
			}
		}
	
		public bool MLCommentLine(ref int nested)
		{
			int la0, la1;
			// Line 238: greedy( &{nested > 0} [*] [/] / [/] [*] / [^\$\n\r*] / [*] &!([/]) )*
			for (;;) {
				la0 = LA0;
				if (la0 == '*') {
					if (nested > 0) {
						la1 = LA(1);
						if (la1 == '/') {
							Skip();
							Skip();
							// line 238
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
						// line 239
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
			// Line 243: (Newline | [*] [/])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				// line 243
				return false;
			} else {
				Match('*');
				Match('/');
				// line 243
				return true;
			}
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