// Generated from LESv3.ecs by LeMP custom tool. LeMP version: 1.8.1.0
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
using Loyc.Syntax;
namespace Loyc.Syntax.Les
{
	using TT = TokenType;
	using P = LesPrecedence;
	using S = CodeSymbols;
	public partial class Les3Lexer
	{
		static readonly Symbol sy_true = (Symbol) "true", sy_false = (Symbol) "false", sy_null = (Symbol) "null", sy__semi = (Symbol) ";", sy__comma = (Symbol) ",", sy_ = (Symbol) "";
		protected new object ParseIdValue(bool isFancy)
		{
			object result = base.ParseIdValue(isFancy);
			if (!isFancy) {
				if (result == sy_true)
					return G.BoxedTrue;
				if (result == sy_false)
					return G.BoxedFalse;
				if (result == sy_null)
					return null;
			}
			return result;
		}
		object Newline(bool ignoreIndent = false)
		{
			int la0;
			// Line 41: ([\r] ([\n])? | [\n])
			la0 = LA0;
			if (la0 == '\r') {
				Skip();
				// Line 41: ([\n])?
				la0 = LA0;
				if (la0 == '\n')
					Skip();
			} else
				Match('\n');
			// line 42
			return WhitespaceTag.Value;
		}
		object SLComment()
		{
			int la0;
			Skip();
			Skip();
			// Line 44: ([^\$\n\r])*
			for (;;) {
				la0 = LA0;
				if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
					Skip();
				else
					break;
			}
			// line 45
			return WhitespaceTag.Value;
		}
		object MLComment()
		{
			int la1;
			Skip();
			Skip();
			// Line 47: nongreedy( MLComment / Newline / [^\$] )*
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
			// line 48
			return WhitespaceTag.Value;
		}
		static readonly HashSet<int> Number_set0 = NewSetOfRanges('#', '#', 'A', 'Z', '_', '_', 'a', 'z');
		object Number()
		{
			int la0;
			// line 53
			_isFloat = _isNegative = false;
			_typeSuffix = null;
			// Line 54: ([\-])?
			la0 = LA0;
			if (la0 == '-') {
				Skip();
				// line 54
				_isNegative = true;
			}
			// Line 55: ( HexNumber / BinNumber / DecNumber )
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
			// Line 56: (NumberSuffix)?
			la0 = LA0;
			if (Number_set0.Contains(la0))
				_typeSuffix = NumberSuffix(ref _isFloat);
			else if (la0 >= 128 && la0 <= 65532) {
				la0 = LA0;
				if (char.IsLetter((char) la0))
					_typeSuffix = NumberSuffix(ref _isFloat);
			}
			// line 57
			return ParseNumberValue();
		}
		void DecDigits()
		{
			int la0, la1;
			MatchRange('0', '9');
			// Line 59: ([0-9])*
			for (;;) {
				la0 = LA0;
				if (la0 >= '0' && la0 <= '9')
					Skip();
				else
					break;
			}
			// Line 59: greedy([_] [0-9] ([0-9])*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '_') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						Skip();
						Skip();
						// Line 59: ([0-9])*
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
			// Line 59: greedy([_])?
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
			// Line 61: greedy([0-9A-Fa-f])*
			for (;;) {
				la0 = LA0;
				if (HexDigit_set0.Contains(la0))
					Skip();
				else
					break;
			}
			// Line 61: greedy([_] [0-9A-Fa-f] greedy([0-9A-Fa-f])*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '_') {
					la1 = LA(1);
					if (HexDigit_set0.Contains(la1)) {
						Skip();
						Skip();
						// Line 61: greedy([0-9A-Fa-f])*
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
			// Line 61: greedy([_])?
			la0 = LA0;
			if (la0 == '_')
				Skip();
		}
		bool Scan_HexDigits()
		{
			int la0, la1;
			if (!TryMatch(HexDigit_set0))
				return false;
			// Line 61: greedy([0-9A-Fa-f])*
			for (;;) {
				la0 = LA0;
				if (HexDigit_set0.Contains(la0))
					{if (!TryMatch(HexDigit_set0))
						return false;}
				else
					break;
			}
			// Line 61: greedy([_] [0-9A-Fa-f] greedy([0-9A-Fa-f])*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '_') {
					la1 = LA(1);
					if (HexDigit_set0.Contains(la1)) {
						if (!TryMatch('_'))
							return false;
						if (!TryMatch(HexDigit_set0))
							return false;
						// Line 61: greedy([0-9A-Fa-f])*
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
			// Line 61: greedy([_])?
			la0 = LA0;
			if (la0 == '_')
				if (!TryMatch('_'))
					return false;
			return true;
		}
		void DecNumber()
		{
			int la0, la1;
			// line 63
			_numberBase = 10;
			// Line 64: ([.] DecDigits | DecDigits ([.] DecDigits)?)
			la0 = LA0;
			if (la0 == '.') {
				Skip();
				DecDigits();
				// line 64
				_isFloat = true;
			} else {
				DecDigits();
				// Line 65: ([.] DecDigits)?
				la0 = LA0;
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						// line 65
						_isFloat = true;
						Skip();
						DecDigits();
					}
				}
			}
			// Line 67: greedy([Ee] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'E' || la0 == 'e') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					// line 67
					_isFloat = true;
					Skip();
					// Line 67: ([+\-])?
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
			// line 70
			_numberBase = 16;
			// Line 71: greedy(HexDigits)?
			la0 = LA0;
			if (HexDigit_set0.Contains(la0))
				HexDigits();
			// Line 73: ([.] ([0-9] =>  / &(HexDigits [Pp] [+\-0-9])) HexDigits)?
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
					// Line 73: ([0-9] =>  / &(HexDigits [Pp] [+\-0-9]))
					la0 = LA0;
					if (la0 >= '0' && la0 <= '9') {
					} else
						Check(Try_HexNumber_Test0(0), "HexDigits [Pp] [+\\-0-9]");
					// line 74
					_isFloat = true;
					HexDigits();
				}
			} while (false);
			// Line 75: greedy([Pp] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'P' || la0 == 'p') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					// line 75
					_isFloat = true;
					Skip();
					// Line 75: ([+\-])?
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
			// line 78
			_numberBase = 2;
			// Line 79: ([.] DecDigits | DecDigits ([.] DecDigits)?)
			la0 = LA0;
			if (la0 == '.') {
				Skip();
				DecDigits();
				// line 79
				_isFloat = true;
			} else {
				DecDigits();
				// Line 80: ([.] DecDigits)?
				la0 = LA0;
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						// line 80
						_isFloat = true;
						Skip();
						DecDigits();
					}
				}
			}
			// Line 82: greedy([Pp] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'P' || la0 == 'p') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					// line 82
					_isFloat = true;
					Skip();
					// Line 82: ([+\-])?
					la0 = LA0;
					if (la0 == '+' || la0 == '-')
						Skip();
					DecDigits();
				}
			}
		}
		static readonly HashSet<int> NumberSuffix_set0 = NewSetOfRanges('#', '#', '0', '9', 'A', 'Z', '_', '_', 'a', 'z');
		Symbol NumberSuffix(ref bool isFloat)
		{
			int la0, la1, la2;
			Symbol result = default(Symbol);
			var here = InputPosition;
			// Line 88: (( [Ff] | [Dd] | [Mm] | [Ll] ([Uu])? | [Uu] ([Ll])? ) EndId =>  / NormalId)
			do {
				switch (LA0) {
				case 'D':
				case 'F':
				case 'M':
				case 'd':
				case 'f':
				case 'm':
					{
						la1 = LA(1);
						if (!NumberSuffix_set0.Contains(la1))
							// Line 88: ( [Ff] | [Dd] | [Mm] | [Ll] ([Uu])? | [Uu] ([Ll])? )
							switch (LA0) {
							case 'F':
							case 'f':
								{
									Skip();
									// line 88
									result = _F;
									isFloat = true;
								}
								break;
							case 'D':
							case 'd':
								{
									Skip();
									// line 89
									result = _D;
									isFloat = true;
								}
								break;
							case 'M':
							case 'm':
								{
									Skip();
									// line 90
									result = _M;
									isFloat = true;
								}
								break;
							case 'L':
							case 'l':
								{
									Skip();
									// line 91
									result = _L;
									// Line 91: ([Uu])?
									la0 = LA0;
									if (la0 == 'U' || la0 == 'u') {
										Skip();
										// line 91
										result = _UL;
									}
								}
								break;
							default:
								{
									Match('U', 'u');
									// line 92
									result = _U;
									// Line 92: ([Ll])?
									la0 = LA0;
									if (la0 == 'L' || la0 == 'l') {
										Skip();
										// line 92
										result = _UL;
									}
								}
								break;
							}
						else
							goto matchNormalId;
					}
					break;
				case 'L':
				case 'l':
					{
						la1 = LA(1);
						if (la1 == 'U' || la1 == 'u') {
							la2 = LA(2);
							if (!NumberSuffix_set0.Contains(la2))
								// Line 88: ( [Ff] | [Dd] | [Mm] | [Ll] ([Uu])? | [Uu] ([Ll])? )
								switch (LA0) {
								case 'F':
								case 'f':
									{
										Skip();
										// line 88
										result = _F;
										isFloat = true;
									}
									break;
								case 'D':
								case 'd':
									{
										Skip();
										// line 89
										result = _D;
										isFloat = true;
									}
									break;
								case 'M':
								case 'm':
									{
										Skip();
										// line 90
										result = _M;
										isFloat = true;
									}
									break;
								case 'L':
								case 'l':
									{
										Skip();
										// line 91
										result = _L;
										// Line 91: ([Uu])?
										la0 = LA0;
										if (la0 == 'U' || la0 == 'u') {
											Skip();
											// line 91
											result = _UL;
										}
									}
									break;
								default:
									{
										Match('U', 'u');
										// line 92
										result = _U;
										// Line 92: ([Ll])?
										la0 = LA0;
										if (la0 == 'L' || la0 == 'l') {
											Skip();
											// line 92
											result = _UL;
										}
									}
									break;
								}
							else
								goto matchNormalId;
						} else if (!NumberSuffix_set0.Contains(la1))
							// Line 88: ( [Ff] | [Dd] | [Mm] | [Ll] ([Uu])? | [Uu] ([Ll])? )
							switch (LA0) {
							case 'F':
							case 'f':
								{
									Skip();
									// line 88
									result = _F;
									isFloat = true;
								}
								break;
							case 'D':
							case 'd':
								{
									Skip();
									// line 89
									result = _D;
									isFloat = true;
								}
								break;
							case 'M':
							case 'm':
								{
									Skip();
									// line 90
									result = _M;
									isFloat = true;
								}
								break;
							case 'L':
							case 'l':
								{
									Skip();
									// line 91
									result = _L;
									// Line 91: ([Uu])?
									la0 = LA0;
									if (la0 == 'U' || la0 == 'u') {
										Skip();
										// line 91
										result = _UL;
									}
								}
								break;
							default:
								{
									Match('U', 'u');
									// line 92
									result = _U;
									// Line 92: ([Ll])?
									la0 = LA0;
									if (la0 == 'L' || la0 == 'l') {
										Skip();
										// line 92
										result = _UL;
									}
								}
								break;
							}
						else
							goto matchNormalId;
					}
					break;
				case 'U':
				case 'u':
					{
						la1 = LA(1);
						if (la1 == 'L' || la1 == 'l') {
							la2 = LA(2);
							if (!NumberSuffix_set0.Contains(la2))
								// Line 88: ( [Ff] | [Dd] | [Mm] | [Ll] ([Uu])? | [Uu] ([Ll])? )
								switch (LA0) {
								case 'F':
								case 'f':
									{
										Skip();
										// line 88
										result = _F;
										isFloat = true;
									}
									break;
								case 'D':
								case 'd':
									{
										Skip();
										// line 89
										result = _D;
										isFloat = true;
									}
									break;
								case 'M':
								case 'm':
									{
										Skip();
										// line 90
										result = _M;
										isFloat = true;
									}
									break;
								case 'L':
								case 'l':
									{
										Skip();
										// line 91
										result = _L;
										// Line 91: ([Uu])?
										la0 = LA0;
										if (la0 == 'U' || la0 == 'u') {
											Skip();
											// line 91
											result = _UL;
										}
									}
									break;
								default:
									{
										Match('U', 'u');
										// line 92
										result = _U;
										// Line 92: ([Ll])?
										la0 = LA0;
										if (la0 == 'L' || la0 == 'l') {
											Skip();
											// line 92
											result = _UL;
										}
									}
									break;
								}
							else
								goto matchNormalId;
						} else if (!NumberSuffix_set0.Contains(la1))
							// Line 88: ( [Ff] | [Dd] | [Mm] | [Ll] ([Uu])? | [Uu] ([Ll])? )
							switch (LA0) {
							case 'F':
							case 'f':
								{
									Skip();
									// line 88
									result = _F;
									isFloat = true;
								}
								break;
							case 'D':
							case 'd':
								{
									Skip();
									// line 89
									result = _D;
									isFloat = true;
								}
								break;
							case 'M':
							case 'm':
								{
									Skip();
									// line 90
									result = _M;
									isFloat = true;
								}
								break;
							case 'L':
							case 'l':
								{
									Skip();
									// line 91
									result = _L;
									// Line 91: ([Uu])?
									la0 = LA0;
									if (la0 == 'U' || la0 == 'u') {
										Skip();
										// line 91
										result = _UL;
									}
								}
								break;
							default:
								{
									Match('U', 'u');
									// line 92
									result = _U;
									// Line 92: ([Ll])?
									la0 = LA0;
									if (la0 == 'L' || la0 == 'l') {
										Skip();
										// line 92
										result = _UL;
									}
								}
								break;
							}
						else
							goto matchNormalId;
					}
					break;
				default:
					goto matchNormalId;
				}
				break;
			matchNormalId:
				{
					NormalId();
					// line 95
					result = IdToSymbol(CharSource.Slice(here, InputPosition - here));
				}
			} while (false);
			return result;
		}
		object SQString()
		{
			int la0;
			// line 103
			_parseNeeded = false;
			Skip();
			// Line 104: ([\\] [^\$] | [^\$\n\r'\\])
			la0 = LA0;
			if (la0 == '\\') {
				Skip();
				MatchExcept();
				// line 104
				_parseNeeded = true;
			} else
				MatchExcept('\n', '\r', '\'', '\\');
			// Line 105: (['] / )
			la0 = LA0;
			if (la0 == '\'')
				Skip();
			else
				// line 105
				_parseNeeded = true;
			// line 106
			return ParseSQStringValue();
		}
		object DQString()
		{
			int la0, la1;
			// line 109
			_parseNeeded = false;
			Skip();
			// Line 110: ([\\] [^\$] | [^\$\n\r"\\])*
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					la1 = LA(1);
					if (la1 != -1) {
						Skip();
						Skip();
						// line 110
						_parseNeeded = true;
					} else
						break;
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '"'))
					Skip();
				else
					break;
			}
			// Line 111: (["] / )
			la0 = LA0;
			if (la0 == '"')
				Skip();
			else
				// line 111
				_parseNeeded = true;
			// line 112
			return ParseStringValue(false);
		}
		object TQString()
		{
			int la0, la1, la2;
			_parseNeeded = true;
			_style = NodeStyle.Alternate;
			// Line 117: (["] ["] ["] nongreedy(Newline / [^\$])* ["] ["] ["] | ['] ['] ['] nongreedy(Newline / [^\$])* ['] ['] ['])
			la0 = LA0;
			if (la0 == '"') {
				Skip();
				Match('"');
				Match('"');
				// Line 117: nongreedy(Newline / [^\$])*
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
				// line 118
				_style |= NodeStyle.Alternate2;
				Match('\'');
				Match('\'');
				Match('\'');
				// Line 119: nongreedy(Newline / [^\$])*
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
			// line 120
			return ParseStringValue(true);
		}
		void BQString2()
		{
			int la0;
			// line 123
			_parseNeeded = false;
			Skip();
			// Line 124: ([\\] [^\$] | [^\$\n\r\\`])*
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					Skip();
					MatchExcept();
					// line 124
					_parseNeeded = true;
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '`'))
					Skip();
				else
					break;
			}
			Match('`');
		}
		object BQString()
		{
			BQString2();
			// line 126
			return ParseBQStringValue();
		}
		void OpChar()
		{
			Skip();
		}
		object Operator()
		{
			OpChar();
			// Line 133: (OpChar)*
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
					OpChar();
					break;
				default:
					goto stop;
				}
			}
		stop:;
			// line 133
			return ParseNormalOp();
		}
		static readonly HashSet<int> SQOperator_set0 = NewSetOfRanges('!', '!', '#', '\'', '*', '+', '-', ':', '<', '?', 'A', 'Z', '^', '_', 'a', 'z', '|', '|', '~', '~');
		object SQOperator()
		{
			int la0;
			Skip();
			LettersOrPunc();
			// Line 135: (LettersOrPunc)*
			for (;;) {
				la0 = LA0;
				if (SQOperator_set0.Contains(la0))
					LettersOrPunc();
				else
					break;
			}
			// line 135
			return ParseNormalOp();
		}
		void IdExtLetter()
		{
			Check(char.IsLetter((char) LA0), "char.IsLetter((char) $LA)");
			MatchRange(128, 65532);
		}
		void NormalId()
		{
			int la0;
			// Line 143: ([#A-Z_a-z] | IdExtLetter)
			la0 = LA0;
			if (Number_set0.Contains(la0))
				Skip();
			else
				IdExtLetter();
			// Line 143: ( [#A-Z_a-z] | [0-9] | ['] | IdExtLetter )*
			for (;;) {
				la0 = LA0;
				if (Number_set0.Contains(la0))
					Skip();
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
		void FancyId()
		{
			int la0;
			// Line 145: (BQString2 | (LettersOrPunc | IdExtLetter) (LettersOrPunc | IdExtLetter)*)
			la0 = LA0;
			if (la0 == '`')
				BQString2();
			else {
				// Line 145: (LettersOrPunc | IdExtLetter)
				la0 = LA0;
				if (SQOperator_set0.Contains(la0))
					LettersOrPunc();
				else
					IdExtLetter();
				// Line 145: (LettersOrPunc | IdExtLetter)*
				for (;;) {
					la0 = LA0;
					if (SQOperator_set0.Contains(la0))
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
		object Symbol()
		{
			// line 147
			_parseNeeded = false;
			Skip();
			Skip();
			FancyId();
			// line 149
			return ParseSymbolValue();
		}
		static readonly HashSet<int> Id_set0 = NewSetOfRanges('#', '#', 'A', 'Z', '_', '_', 'a', 'z', 128, 65532);
		object Id()
		{
			int la0;
			// line 151
			_parseNeeded = false;
			// Line 152: (NormalId | [@] FancyId)
			la0 = LA0;
			if (Id_set0.Contains(la0)) {
				NormalId();
				// line 152
				return ParseIdValue(false);
			} else {
				Match('@');
				FancyId();
				// line 153
				return ParseIdValue(true);
			}
		}
		void LettersOrPunc()
		{
			Skip();
		}
		object Shebang()
		{
			int la0;
			Skip();
			Skip();
			// Line 160: ([^\$\n\r])*
			for (;;) {
				la0 = LA0;
				if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
					Skip();
				else
					break;
			}
			// Line 160: (Newline)?
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r')
				Newline();
			// line 161
			return WhitespaceTag.Value;
		}
		static readonly HashSet<int> NextToken_set0 = NewSetOfRanges('A', 'Z', '_', '_', 'a', 'z', 128, 65532);
		public override Maybe<Token> NextToken()
		{
			int la0, la1, la2, la3;
			object value = default(object);
			Spaces();
			_startPosition = InputPosition;
			_style = 0;
			if (LA0 == -1) {
				return NoValue.Value;
			}
			// Line 174: ( Shebang / Symbol / Id / Newline / SLComment / MLComment / Number / TQString / DQString / SQString / BQString / SQOperator / [,] / [;] / [(] / [)] / [[] / [\]] / [{] / [}] / [@] / Operator )
			do {
				la0 = LA0;
				switch (la0) {
				case '#':
					{
						if (InputPosition == 0) {
							la1 = LA(1);
							if (la1 == '!') {
								// line 174
								_type = TT.Shebang;
								value = Shebang();
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
							if (la2 == '`') {
								la3 = LA(3);
								if (!(la3 == -1 || la3 == '\n' || la3 == '\r'))
									goto matchSymbol;
								else
									goto match21;
							} else if (SQOperator_set0.Contains(la2))
								goto matchSymbol;
							else if (la2 >= 128 && la2 <= 65532) {
								la2 = LA(2);
								if (char.IsLetter((char) la2))
									goto matchSymbol;
								else
									goto match21;
							} else
								goto match21;
						} else if (la1 == '`') {
							la2 = LA(2);
							if (la2 == '\\') {
								la3 = LA(3);
								if (la3 != -1)
									goto matchId;
								else
									goto match21;
							} else if (!(la2 == -1 || la2 == '\n' || la2 == '\r' || la2 == '`')) {
								la3 = LA(3);
								if (!(la3 == -1 || la3 == '\n' || la3 == '\r'))
									goto matchId;
								else
									goto match21;
							} else if (la2 == '`')
								goto matchId;
							else
								goto match21;
						} else if (SQOperator_set0.Contains(la1))
							goto matchId;
						else if (la1 >= 128 && la1 <= 65532) {
							la1 = LA(1);
							if (char.IsLetter((char) la1))
								goto matchId;
							else
								goto match21;
						} else
							goto match21;
					}
				case '\n':
				case '\r':
					{
						// line 177
						_type = TT.Newline;
						value = Newline();
					}
					break;
				case '/':
					{
						la1 = LA(1);
						if (la1 == '/') {
							// line 178
							_type = TT.SLComment;
							value = SLComment();
						} else if (la1 == '*') {
							la2 = LA(2);
							if (la2 != -1) {
								la3 = LA(3);
								if (la3 != -1) {
									// line 179
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
						if (la1 == '0')
							goto matchNumber;
						else if (la1 == '.') {
							la2 = LA(2);
							if (la2 >= '0' && la2 <= '9')
								goto matchNumber;
							else
								value = Operator();
						} else if (la1 >= '1' && la1 <= '9')
							goto matchNumber;
						else
							value = Operator();
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
							value = Operator();
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
									value = SQOperator();
							} else
								value = SQOperator();
						} else if (!(la1 == -1 || la1 == '\n' || la1 == '\r')) {
							// line 183
							_type = TT.Literal;
							value = SQString();
						} else
							goto error;
					}
					break;
				case '`':
					{
						// line 184
						_type = TT.BQOperator;
						value = BQString();
					}
					break;
				case ',':
					{
						// line 186
						_type = TT.Comma;
						Skip();
						// line 186
						_value = sy__semi;
					}
					break;
				case ';':
					{
						// line 187
						_type = TT.Semicolon;
						Skip();
						// line 187
						_value = sy__comma;
					}
					break;
				case '(':
					{
						// line 188
						_type = TT.LParen;
						Skip();
					}
					break;
				case ')':
					{
						// line 189
						_type = TT.RParen;
						Skip();
					}
					break;
				case '[':
					{
						// line 190
						_type = TT.LBrack;
						Skip();
					}
					break;
				case ']':
					{
						// line 191
						_type = TT.RBrack;
						Skip();
					}
					break;
				case '{':
					{
						// line 192
						_type = TT.LBrace;
						Skip();
					}
					break;
				case '}':
					{
						// line 193
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
					if (NextToken_set0.Contains(la0))
						goto matchId;
					else
						goto error;
				}
				break;
			matchSymbol:
				{
					// line 175
					_type = TT.Literal;
					value = Symbol();
				}
				break;
			matchId:
				{
					// line 176
					_type = TT.Id;
					value = Id();
				}
				break;
			matchNumber:
				{
					// line 180
					_type = TT.Literal;
					value = Number();
				}
				break;
			matchTQString:
				{
					// line 181
					_type = TT.Literal;
					value = TQString();
				}
				break;
			matchDQString:
				{
					// line 182
					_type = TT.Literal;
					value = DQString();
				}
				break;
			match21:
				{
					// line 194
					_type = TT.At;
					Skip();
					// line 194
					_value = sy_;
				}
				break;
			error:
				{
					// Line 196: ([\$] | [^\$])
					la0 = LA0;
					if (la0 == -1) {
						Skip();
						// line 196
						_type = TT.EOF;
					} else {
						Skip();
						// line 197
						_type = TT.Unknown;
					}
				}
			} while (false);
			// line 199
			Debug.Assert(InputPosition > _startPosition);
			return new Token((int) _type, _startPosition, InputPosition - _startPosition, _style, _value);
		}
		bool TDQStringLine()
		{
			int la0, la1, la2;
			// Line 209: nongreedy([^\$])*
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
			// Line 209: (Newline | ["] ["] ["])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				// line 209
				return false;
			} else {
				Match('"');
				Match('"');
				Match('"');
				// line 209
				return true;
			}
		}
		bool TSQStringLine()
		{
			int la0, la1, la2;
			// Line 212: nongreedy([^\$])*
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
			// Line 212: (Newline | ['] ['] ['])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				// line 212
				return false;
			} else {
				Match('\'');
				Match('\'');
				Match('\'');
				// line 212
				return true;
			}
		}
		public bool MLCommentLine(ref int nested)
		{
			int la0, la1;
			// Line 215: greedy( &{nested > 0} [*] [/] / [/] [*] / [^\$\n\r*] / [*] &!([/]) )*
			for (;;) {
				la0 = LA0;
				if (la0 == '*') {
					if (nested > 0) {
						la1 = LA(1);
						if (la1 == '/') {
							Skip();
							Skip();
							// line 215
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
						// line 216
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
			// Line 220: (Newline | [*] [/])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				// line 220
				return false;
			} else {
				Match('*');
				Match('/');
				// line 220
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
	partial class Les3Parser
	{
		#pragma warning disable 162, 642
		protected new const TT EOF = TT.EOF;
		void CheckEndMarker(ref TokenType endMarker, ref Token end)
		{
			if (endMarker != end.Type()) {
				if (endMarker == default(TT)) {
					endMarker = end.Type();
				} else {
					Error(-1, "Unexpected separator: {0} should be {1}", ToString(end.TypeInt), ToString((int) endMarker));
				}
			}
		}
		public VList<LNode> ExprList(VList<LNode> list = default(VList<LNode>))
		{
			var endMarker = default(TT);
			return ExprList(ref endMarker, list);
		}
		public VList<LNode> StmtList()
		{
			VList<LNode> result = default(VList<LNode>);
			var endMarker = TT.Semicolon;
			result = ExprList(ref endMarker);
			return result;
		}
		public VList<LNode> ExprList(ref TokenType endMarker, VList<LNode> list = default(VList<LNode>))
		{
			TT la0;
			LNode e = default(LNode);
			Token end = default(Token);
			// line 274
			if (LT0.Value is string) {
				endMarker = TT.EOF;
			}
			;
			// Line 1: ( / TopExpr)
			switch ((TT) LA0) {
			case EOF:
			case TT.Comma:
			case TT.RBrace:
			case TT.RBrack:
			case TT.RParen:
			case TT.Semicolon:
				{
				}
				break;
			default:
				e = TopExpr();
				break;
			}
			// Line 276: ((TT.Comma|TT.Semicolon) ( / TopExpr))*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Comma || la0 == TT.Semicolon) {
					end = MatchAny();
					list.Add(e ?? MissingExpr());
					CheckEndMarker(ref endMarker, ref end);
					// Line 279: ( / TopExpr)
					switch ((TT) LA0) {
					case EOF:
					case TT.Comma:
					case TT.RBrace:
					case TT.RBrack:
					case TT.RParen:
					case TT.Semicolon:
						// line 279
						e = null;
						break;
					default:
						e = TopExpr();
						break;
					}
				} else
					break;
			}
			// line 281
			if (e != null || end.Type() == TT.Comma) {
				list.Add(e ?? MissingExpr());
			}
			;
			return list;
		}
		public IEnumerable<LNode> ExprListLazy(Holder<TokenType> endMarker)
		{
			TT la0;
			LNode e = default(LNode);
			Token end = default(Token);
			// line 285
			if (LT0.Value is string) {
				endMarker = TT.EOF;
			}
			;
			// Line 1: ( / TopExpr)
			la0 = (TT) LA0;
			if (la0 == (TT) EOF || la0 == TT.Comma || la0 == TT.Semicolon) {
			} else
				e = TopExpr();
			// Line 287: ((TT.Comma|TT.Semicolon) ( / TopExpr))*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Comma || la0 == TT.Semicolon) {
					end = MatchAny();
					yield return e ?? MissingExpr();
					CheckEndMarker(ref endMarker.Value, ref end);
					// Line 290: ( / TopExpr)
					la0 = (TT) LA0;
					if (la0 == (TT) EOF || la0 == TT.Comma || la0 == TT.Semicolon)
						// line 290
						e = null;
					else
						e = TopExpr();
				} else
					break;
			}
			// line 292
			if (e != null || end.Type() == TT.Comma) {
				yield return e ?? MissingExpr();
			}
		}
		protected LNode TopExpr()
		{
			TT la0;
			VList<LNode> attrs = default(VList<LNode>);
			LNode e = default(LNode);
			Token litx40 = default(Token);
			// Line 297: (TT.At TT.LBrack ExprList TT.RBrack)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.At) {
					litx40 = MatchAny();
					Match((int) TT.LBrack);
					attrs = ExprList(attrs);
					Match((int) TT.RBrack);
				} else
					break;
			}
			// Line 299: (Expr / TT.Id Expr (Particle)*)
			e = Expr(StartStmt);
			// line 314
			if (litx40.TypeInt != 0) {
				e = e.WithRange(litx40.StartIndex, e.Range.EndIndex);
			}
			;
			return e.PlusAttrs(attrs);
		}
		LNode Expr(Precedence context)
		{
			LNode e = default(LNode);
			Token t = default(Token);
			// line 326
			Precedence prec;
			e = PrefixExpr(context);
			// Line 330: greedy( &{context.CanParse(prec = InfixPrecedenceOf(LT($LI)))} (TT.Assignment|TT.BQString|TT.Dot|TT.NormalOp) Expr | &{context.CanParse(P.Primary)} FinishPrimaryExpr | &{context.CanParse(SuffixPrecedenceOf(LT($LI)))} TT.PreOrSufOp )*
			for (;;) {
				switch ((TT) LA0) {
				case TT.Assignment:
				case TT.BQOperator:
				case TT.Dot:
				case TT.NormalOp:
					{
						if (context.CanParse(prec = InfixPrecedenceOf(LT(0)))) {
							// line 331
							if (!prec.CanMixWith(context)) {
								Error(0, "Operator '{0}' is not allowed in this context. Add parentheses to clarify the code's meaning.", LT0.Value);
							}
							;
							t = MatchAny();
							var rhs = Expr(prec);
							// line 336
							e = F.Call((Symbol) t.Value, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex).SetStyle(NodeStyle.Operator);
						} else
							goto stop;
					}
					break;
				case TT.LBrack:
				case TT.LParen:
				case TT.Not:
					{
						if (context.CanParse(P.Primary))
							e = FinishPrimaryExpr(e);
						else
							goto stop;
					}
					break;
				case TT.PreOrSufOp:
					{
						if (context.CanParse(SuffixPrecedenceOf(LT(0)))) {
							t = MatchAny();
							// line 343
							e = F.Call(ToSuffixOpName((Symbol) t.Value), e, e.Range.StartIndex, t.EndIndex).SetStyle(NodeStyle.Operator);
						} else
							goto stop;
					}
					break;
				default:
					goto stop;
				}
			}
		stop:;
			// line 345
			return e;
		}
		LNode FinishPrimaryExpr(LNode e)
		{
			TT la0;
			VList<LNode> list = default(VList<LNode>);
			// Line 350: ( TT.LParen ExprList TT.RParen | TT.Not (TT.LParen ExprList TT.RParen / Expr) | TT.LBrack ExprList TT.RBrack )
			la0 = (TT) LA0;
			if (la0 == TT.LParen) {
				// line 350
				var endMarker = default(TokenType);
				Skip();
				list = ExprList(ref endMarker);
				var c = Match((int) TT.RParen);
				// line 353
				e = MarkCall(F.Call(e, list, e.Range.StartIndex, c.EndIndex));
				if (endMarker == TT.Semicolon) {
					e.Style = NodeStyle.Statement | NodeStyle.Alternate;
				}
				;
			} else if (la0 == TT.Not) {
				Skip();
				// line 358
				var args = new VList<LNode> { 
					e
				};
				int endIndex;
				// Line 359: (TT.LParen ExprList TT.RParen / Expr)
				la0 = (TT) LA0;
				if (la0 == TT.LParen) {
					Skip();
					args = ExprList(args);
					var c = Match((int) TT.RParen);
					// line 359
					endIndex = c.EndIndex;
				} else {
					var T = Expr(P.Primary);
					// line 360
					args.Add(T);
					endIndex = T.Range.EndIndex;
				}
				// line 362
				e = F.Call(S.Of, args, e.Range.StartIndex, endIndex).SetStyle(NodeStyle.Operator);
			} else {
				// line 364
				var args = new VList<LNode> { 
					e
				};
				Match((int) TT.LBrack);
				args = ExprList(args);
				var c = Match((int) TT.RBrack);
				// line 366
				e = F.Call(S.IndexBracks, args, e.Range.StartIndex, c.EndIndex).SetStyle(NodeStyle.Operator);
			}
			// line 368
			return e;
		}
		LNode PrefixExpr(Precedence context)
		{
			LNode e = default(LNode);
			LNode result = default(LNode);
			Token t = default(Token);
			// Line 372: ((TT.Assignment|TT.BQString|TT.Dot|TT.NormalOp|TT.Not|TT.PrefixOp|TT.PreOrSufOp) Expr | Particle)
			switch ((TT) LA0) {
			case TT.Assignment:
			case TT.BQOperator:
			case TT.Dot:
			case TT.NormalOp:
			case TT.Not:
			case TT.PrefixOp:
			case TT.PreOrSufOp:
				{
					t = MatchAny();
					e = Expr(PrefixPrecedenceOf(t));
					// line 374
					result = F.Call((Symbol) t.Value, e, t.StartIndex, e.Range.EndIndex).SetStyle(NodeStyle.Operator);
				}
				break;
			default:
				result = Particle();
				break;
			}
			return result;
		}
		LNode Particle()
		{
			TT la0;
			Token c = default(Token);
			Token lit_lcub = default(Token);
			Token lit_lpar = default(Token);
			Token lit_lsqb = default(Token);
			Token lit_rcub = default(Token);
			Token lit_rpar = default(Token);
			Token lit_rsqb = default(Token);
			Token o = default(Token);
			LNode result = default(LNode);
			TokenTree tree = default(TokenTree);
			// Line 387: ( TT.Id | TT.Literal | TT.At (TT.LBrack TokenTree TT.RBrack | TT.LBrace TokenTree TT.RBrace) | TT.LBrace StmtList TT.RBrace | TT.LBrack ExprList TT.RBrack | TT.LParen ExprList TT.RParen )
			switch ((TT) LA0) {
			case TT.Id:
				{
					var id = MatchAny();
					// line 388
					result = F.Id(id).SetStyle(id.Style);
				}
				break;
			case TT.Literal:
				{
					var lit = MatchAny();
					// line 390
					result = F.Literal(lit).SetStyle(lit.Style);
				}
				break;
			case TT.At:
				{
					o = MatchAny();
					// Line 393: (TT.LBrack TokenTree TT.RBrack | TT.LBrace TokenTree TT.RBrace)
					la0 = (TT) LA0;
					if (la0 == TT.LBrack) {
						lit_lsqb = MatchAny();
						tree = TokenTree();
						c = Match((int) TT.RBrack);
					} else {
						lit_lcub = Match((int) TT.LBrace);
						tree = TokenTree();
						c = Match((int) TT.RBrace);
					}
					// line 395
					result = F.Literal(tree, o.StartIndex, c.EndIndex);
				}
				break;
			case TT.LBrace:
				{
					lit_lcub = MatchAny();
					var list = StmtList();
					lit_rcub = Match((int) TT.RBrace);
					// line 398
					result = F.Braces(list, lit_lcub.StartIndex, lit_rcub.EndIndex).SetStyle(NodeStyle.Statement);
				}
				break;
			case TT.LBrack:
				{
					lit_lsqb = MatchAny();
					var list = ExprList();
					lit_rsqb = Match((int) TT.RBrack);
					// line 401
					result = F.Call(S.Array, list, lit_lsqb.StartIndex, lit_rsqb.EndIndex).SetStyle(NodeStyle.Expression);
				}
				break;
			case TT.LParen:
				{
					// line 403
					var endMarker = default(TT);
					lit_lpar = MatchAny();
					// line 404
					var hasAttrList = (TT) LA0 == TT.LBrack || (TT) LA0 == TT.At;
					var list = ExprList(ref endMarker);
					lit_rpar = Match((int) TT.RParen);
					// line 407
					if (endMarker == TT.Semicolon || list.Count != 1) {
						result = F.Call(S.Tuple, list, lit_lpar.StartIndex, lit_rpar.EndIndex);
						if (endMarker == TT.Comma) {
							var msg = "Tuples require ';' as a separator.";
							ErrorSink.Write(Severity.Error, list[0].Range.End, msg);
						}
						;
					} else {
						result = hasAttrList ? list[0] : F.InParens(list[0], lit_lpar.StartIndex, lit_rpar.EndIndex);
					}
					;
				}
				break;
			default:
				{
					// line 418
					Error(0, "Expected a particle (id, literal, {braces} or (parens)).");
					result = MissingExpr();
				}
				break;
			}
			return result;
		}
		TokenTree TokenTree()
		{
			TokenTree got_TokenTree = default(TokenTree);
			TokenTree result = default(TokenTree);
			result = new TokenTree(SourceFile);
			// Line 425: nongreedy((TT.LBrace|TT.LBrack|TT.LParen) TokenTree (TT.RBrace|TT.RBrack|TT.RParen) / ~(EOF))*
			for (;;) {
				switch ((TT) LA0) {
				case EOF:
				case TT.RBrace:
				case TT.RBrack:
				case TT.RParen:
					goto stop;
				case TT.LBrace:
				case TT.LBrack:
				case TT.LParen:
					{
						var open = MatchAny();
						got_TokenTree = TokenTree();
						// line 427
						result.Add(open.WithValue(got_TokenTree));
						result.Add(Match((int) TT.RBrace, (int) TT.RBrack, (int) TT.RParen));
					}
					break;
				default:
					result.Add(MatchAny());
					break;
				}
			}
		stop:;
			return result;
		}
	}
}
