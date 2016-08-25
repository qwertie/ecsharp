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
		static readonly Symbol sy_true = (Symbol) "true", sy_false = (Symbol) "false", sy_null = (Symbol) "null", sy_s = (Symbol) "s";
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
			int la0;
			// line 40
			_isFloat = _isNegative = false;
			_typeSuffix = null;
			// Line 41: ([\-])?
			la0 = LA0;
			if (la0 == '-') {
				Skip();
				// line 41
				_isNegative = true;
			}
			// Line 42: ( HexNumber / BinNumber / DecNumber )
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
			// line 43
			int numberEndPosition = InputPosition;
			// Line 44: (NumberSuffix)?
			la0 = LA0;
			if (la0 >= 'A' && la0 <= 'Z' || la0 == '_' || la0 >= 'a' && la0 <= 'z')
				_typeSuffix = NumberSuffix(ref _isFloat);
			else if (la0 >= 128 && la0 <= 65532) {
				la0 = LA0;
				if (char.IsLetter((char) la0))
					_typeSuffix = NumberSuffix(ref _isFloat);
			}
			// line 46
			_type = _isNegative ? TT.NegativeLiteral : TT.Literal;
			return ParseNumberValue(numberEndPosition);
		}
		void DecDigits()
		{
			int la0, la1;
			MatchRange('0', '9');
			// Line 50: ([0-9])*
			for (;;) {
				la0 = LA0;
				if (la0 >= '0' && la0 <= '9')
					Skip();
				else
					break;
			}
			// Line 50: greedy([_] [0-9] ([0-9])*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '_') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						Skip();
						Skip();
						// Line 50: ([0-9])*
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
			// Line 50: greedy([_])?
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
			// Line 52: greedy([0-9A-Fa-f])*
			for (;;) {
				la0 = LA0;
				if (HexDigit_set0.Contains(la0))
					Skip();
				else
					break;
			}
			// Line 52: greedy([_] [0-9A-Fa-f] greedy([0-9A-Fa-f])*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '_') {
					la1 = LA(1);
					if (HexDigit_set0.Contains(la1)) {
						Skip();
						Skip();
						// Line 52: greedy([0-9A-Fa-f])*
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
			// Line 52: greedy([_])?
			la0 = LA0;
			if (la0 == '_')
				Skip();
		}
		bool Scan_HexDigits()
		{
			int la0, la1;
			if (!TryMatch(HexDigit_set0))
				return false;
			// Line 52: greedy([0-9A-Fa-f])*
			for (;;) {
				la0 = LA0;
				if (HexDigit_set0.Contains(la0))
					{if (!TryMatch(HexDigit_set0))
						return false;}
				else
					break;
			}
			// Line 52: greedy([_] [0-9A-Fa-f] greedy([0-9A-Fa-f])*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '_') {
					la1 = LA(1);
					if (HexDigit_set0.Contains(la1)) {
						if (!TryMatch('_'))
							return false;
						if (!TryMatch(HexDigit_set0))
							return false;
						// Line 52: greedy([0-9A-Fa-f])*
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
			// Line 52: greedy([_])?
			la0 = LA0;
			if (la0 == '_')
				if (!TryMatch('_'))
					return false;
			return true;
		}
		void DecNumber()
		{
			int la0, la1;
			// line 54
			_numberBase = 10;
			// Line 55: ([.] DecDigits | DecDigits ([.] DecDigits)?)
			la0 = LA0;
			if (la0 == '.') {
				Skip();
				DecDigits();
				// line 55
				_isFloat = true;
			} else {
				DecDigits();
				// Line 56: ([.] DecDigits)?
				la0 = LA0;
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						// line 56
						_isFloat = true;
						Skip();
						DecDigits();
					}
				}
			}
			// Line 58: greedy([Ee] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'E' || la0 == 'e') {
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
		void HexNumber()
		{
			int la0, la1;
			Skip();
			Skip();
			// line 61
			_numberBase = 16;
			// Line 62: greedy(HexDigits)?
			la0 = LA0;
			if (HexDigit_set0.Contains(la0))
				HexDigits();
			// Line 64: ([.] ([0-9] =>  / &(HexDigits [Pp] [+\-0-9])) HexDigits)?
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
					// Line 64: ([0-9] =>  / &(HexDigits [Pp] [+\-0-9]))
					la0 = LA0;
					if (la0 >= '0' && la0 <= '9') {
					} else
						Check(Try_HexNumber_Test0(0), "HexDigits [Pp] [+\\-0-9]");
					// line 65
					_isFloat = true;
					HexDigits();
				}
			} while (false);
			// Line 66: greedy([Pp] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'P' || la0 == 'p') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					// line 66
					_isFloat = true;
					Skip();
					// Line 66: ([+\-])?
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
			// line 69
			_numberBase = 2;
			// Line 70: ([.] DecDigits | DecDigits ([.] DecDigits)?)
			la0 = LA0;
			if (la0 == '.') {
				Skip();
				DecDigits();
				// line 70
				_isFloat = true;
			} else {
				DecDigits();
				// Line 71: ([.] DecDigits)?
				la0 = LA0;
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						// line 71
						_isFloat = true;
						Skip();
						DecDigits();
					}
				}
			}
			// Line 73: greedy([Pp] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'P' || la0 == 'p') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					// line 73
					_isFloat = true;
					Skip();
					// Line 73: ([+\-])?
					la0 = LA0;
					if (la0 == '+' || la0 == '-')
						Skip();
					DecDigits();
				}
			}
		}
		static readonly HashSet<int> NumberSuffix_set0 = NewSetOfRanges('#', '#', '0', '9', 'A', 'Z', '_', '_', 'a', 'z');
		static readonly HashSet<int> NumberSuffix_set1 = NewSetOfRanges('#', '#', '0', '9', 'A', 'K', 'M', 'Z', '_', '_', 'a', 'k', 'm', 'z');
		Symbol NumberSuffix(ref bool isFloat)
		{
			int la0, la1, la2, la3;
			Symbol result = default(Symbol);
			var here = InputPosition;
			// Line 79: (( [Ff] | [Dd] | [Mm] | [Ll] ([Uu])? | [Uu] ([Ll])? | [u] [3] [2] | [u] [6] [4] | [i] [3] [2] | [i] [6] [4] | [f] [3] [2] | [f] [6] [4] ) [^#0-9A-Z_a-z] =>  / NormalId)
			do {
				switch (LA0) {
				case 'f':
					{
						la1 = LA(1);
						if (!NumberSuffix_set0.Contains(la1))
							// Line 79: ( [Ff] | [Dd] | [Mm] | [Ll] ([Uu])? | [Uu] ([Ll])? | [u] [3] [2] | [u] [6] [4] | [i] [3] [2] | [i] [6] [4] | [f] [3] [2] | [f] [6] [4] )
							do {
								switch (LA0) {
								case 'f':
									{
										la1 = LA(1);
										if (!NumberSuffix_set0.Contains(la1))
											goto match1;
										else if (la1 == '3') {
											Skip();
											Skip();
											Match('2');
											// line 88
											result = _F;
										} else {
											Skip();
											Match('6');
											Match('4');
											// line 89
											result = _D;
										}
									}
									break;
								case 'F':
									goto match1;
								case 'D':
								case 'd':
									{
										Skip();
										// line 80
										result = _D;
										isFloat = true;
									}
									break;
								case 'M':
								case 'm':
									{
										Skip();
										// line 81
										result = _M;
										isFloat = true;
									}
									break;
								case 'L':
								case 'l':
									{
										Skip();
										// line 82
										result = _L;
										// Line 82: ([Uu])?
										la0 = LA0;
										if (la0 == 'U' || la0 == 'u') {
											Skip();
											// line 82
											result = _UL;
										}
									}
									break;
								case 'u':
									{
										la1 = LA(1);
										if (!NumberSuffix_set1.Contains(la1))
											goto match5;
										else if (la1 == '3') {
											Skip();
											Skip();
											Match('2');
											// line 84
											result = _U;
										} else {
											Skip();
											Match('6');
											Match('4');
											// line 85
											result = _UL;
										}
									}
									break;
								case 'U':
									goto match5;
								default:
									{
										la1 = LA(1);
										if (la1 == '3') {
											Match('i');
											Skip();
											Match('2');
											// line 86
											result = null;
										} else {
											Match('i');
											Match('6');
											Match('4');
											// line 87
											result = _L;
										}
									}
									break;
								}
								break;
							match1:
								{
									Skip();
									// line 79
									result = _F;
									isFloat = true;
								}
								break;
							match5:
								{
									Skip();
									// line 83
									result = _U;
									// Line 83: ([Ll])?
									la0 = LA0;
									if (la0 == 'L' || la0 == 'l') {
										Skip();
										// line 83
										result = _UL;
									}
								}
							} while (false);
						else if (la1 == '3') {
							la2 = LA(2);
							if (la2 == '2') {
								la3 = LA(3);
								if (!NumberSuffix_set0.Contains(la3))
									// Line 79: ( [Ff] | [Dd] | [Mm] | [Ll] ([Uu])? | [Uu] ([Ll])? | [u] [3] [2] | [u] [6] [4] | [i] [3] [2] | [i] [6] [4] | [f] [3] [2] | [f] [6] [4] )
									do {
										switch (LA0) {
										case 'f':
											{
												la1 = LA(1);
												if (!NumberSuffix_set0.Contains(la1))
													goto match1;
												else if (la1 == '3') {
													Skip();
													Skip();
													Match('2');
													// line 88
													result = _F;
												} else {
													Skip();
													Match('6');
													Match('4');
													// line 89
													result = _D;
												}
											}
											break;
										case 'F':
											goto match1;
										case 'D':
										case 'd':
											{
												Skip();
												// line 80
												result = _D;
												isFloat = true;
											}
											break;
										case 'M':
										case 'm':
											{
												Skip();
												// line 81
												result = _M;
												isFloat = true;
											}
											break;
										case 'L':
										case 'l':
											{
												Skip();
												// line 82
												result = _L;
												// Line 82: ([Uu])?
												la0 = LA0;
												if (la0 == 'U' || la0 == 'u') {
													Skip();
													// line 82
													result = _UL;
												}
											}
											break;
										case 'u':
											{
												la1 = LA(1);
												if (!NumberSuffix_set1.Contains(la1))
													goto match5;
												else if (la1 == '3') {
													Skip();
													Skip();
													Match('2');
													// line 84
													result = _U;
												} else {
													Skip();
													Match('6');
													Match('4');
													// line 85
													result = _UL;
												}
											}
											break;
										case 'U':
											goto match5;
										default:
											{
												la1 = LA(1);
												if (la1 == '3') {
													Match('i');
													Skip();
													Match('2');
													// line 86
													result = null;
												} else {
													Match('i');
													Match('6');
													Match('4');
													// line 87
													result = _L;
												}
											}
											break;
										}
										break;
									match1:
										{
											Skip();
											// line 79
											result = _F;
											isFloat = true;
										}
										break;
									match5:
										{
											Skip();
											// line 83
											result = _U;
											// Line 83: ([Ll])?
											la0 = LA0;
											if (la0 == 'L' || la0 == 'l') {
												Skip();
												// line 83
												result = _UL;
											}
										}
									} while (false);
								else
									goto matchNormalId;
							} else
								goto matchNormalId;
						} else if (la1 == '6') {
							la2 = LA(2);
							if (la2 == '4') {
								la3 = LA(3);
								if (!NumberSuffix_set0.Contains(la3))
									// Line 79: ( [Ff] | [Dd] | [Mm] | [Ll] ([Uu])? | [Uu] ([Ll])? | [u] [3] [2] | [u] [6] [4] | [i] [3] [2] | [i] [6] [4] | [f] [3] [2] | [f] [6] [4] )
									do {
										switch (LA0) {
										case 'f':
											{
												la1 = LA(1);
												if (!NumberSuffix_set0.Contains(la1))
													goto match1;
												else if (la1 == '3') {
													Skip();
													Skip();
													Match('2');
													// line 88
													result = _F;
												} else {
													Skip();
													Match('6');
													Match('4');
													// line 89
													result = _D;
												}
											}
											break;
										case 'F':
											goto match1;
										case 'D':
										case 'd':
											{
												Skip();
												// line 80
												result = _D;
												isFloat = true;
											}
											break;
										case 'M':
										case 'm':
											{
												Skip();
												// line 81
												result = _M;
												isFloat = true;
											}
											break;
										case 'L':
										case 'l':
											{
												Skip();
												// line 82
												result = _L;
												// Line 82: ([Uu])?
												la0 = LA0;
												if (la0 == 'U' || la0 == 'u') {
													Skip();
													// line 82
													result = _UL;
												}
											}
											break;
										case 'u':
											{
												la1 = LA(1);
												if (!NumberSuffix_set1.Contains(la1))
													goto match5;
												else if (la1 == '3') {
													Skip();
													Skip();
													Match('2');
													// line 84
													result = _U;
												} else {
													Skip();
													Match('6');
													Match('4');
													// line 85
													result = _UL;
												}
											}
											break;
										case 'U':
											goto match5;
										default:
											{
												la1 = LA(1);
												if (la1 == '3') {
													Match('i');
													Skip();
													Match('2');
													// line 86
													result = null;
												} else {
													Match('i');
													Match('6');
													Match('4');
													// line 87
													result = _L;
												}
											}
											break;
										}
										break;
									match1:
										{
											Skip();
											// line 79
											result = _F;
											isFloat = true;
										}
										break;
									match5:
										{
											Skip();
											// line 83
											result = _U;
											// Line 83: ([Ll])?
											la0 = LA0;
											if (la0 == 'L' || la0 == 'l') {
												Skip();
												// line 83
												result = _UL;
											}
										}
									} while (false);
								else
									goto matchNormalId;
							} else
								goto matchNormalId;
						} else
							goto matchNormalId;
					}
					break;
				case 'D':
				case 'F':
				case 'M':
				case 'd':
				case 'm':
					{
						la1 = LA(1);
						if (!NumberSuffix_set0.Contains(la1))
							// Line 79: ( [Ff] | [Dd] | [Mm] | [Ll] ([Uu])? | [Uu] ([Ll])? | [u] [3] [2] | [u] [6] [4] | [i] [3] [2] | [i] [6] [4] | [f] [3] [2] | [f] [6] [4] )
							do {
								switch (LA0) {
								case 'f':
									{
										la1 = LA(1);
										if (!NumberSuffix_set0.Contains(la1))
											goto match1;
										else if (la1 == '3') {
											Skip();
											Skip();
											Match('2');
											// line 88
											result = _F;
										} else {
											Skip();
											Match('6');
											Match('4');
											// line 89
											result = _D;
										}
									}
									break;
								case 'F':
									goto match1;
								case 'D':
								case 'd':
									{
										Skip();
										// line 80
										result = _D;
										isFloat = true;
									}
									break;
								case 'M':
								case 'm':
									{
										Skip();
										// line 81
										result = _M;
										isFloat = true;
									}
									break;
								case 'L':
								case 'l':
									{
										Skip();
										// line 82
										result = _L;
										// Line 82: ([Uu])?
										la0 = LA0;
										if (la0 == 'U' || la0 == 'u') {
											Skip();
											// line 82
											result = _UL;
										}
									}
									break;
								case 'u':
									{
										la1 = LA(1);
										if (!NumberSuffix_set1.Contains(la1))
											goto match5;
										else if (la1 == '3') {
											Skip();
											Skip();
											Match('2');
											// line 84
											result = _U;
										} else {
											Skip();
											Match('6');
											Match('4');
											// line 85
											result = _UL;
										}
									}
									break;
								case 'U':
									goto match5;
								default:
									{
										la1 = LA(1);
										if (la1 == '3') {
											Match('i');
											Skip();
											Match('2');
											// line 86
											result = null;
										} else {
											Match('i');
											Match('6');
											Match('4');
											// line 87
											result = _L;
										}
									}
									break;
								}
								break;
							match1:
								{
									Skip();
									// line 79
									result = _F;
									isFloat = true;
								}
								break;
							match5:
								{
									Skip();
									// line 83
									result = _U;
									// Line 83: ([Ll])?
									la0 = LA0;
									if (la0 == 'L' || la0 == 'l') {
										Skip();
										// line 83
										result = _UL;
									}
								}
							} while (false);
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
								// Line 79: ( [Ff] | [Dd] | [Mm] | [Ll] ([Uu])? | [Uu] ([Ll])? | [u] [3] [2] | [u] [6] [4] | [i] [3] [2] | [i] [6] [4] | [f] [3] [2] | [f] [6] [4] )
								do {
									switch (LA0) {
									case 'f':
										{
											la1 = LA(1);
											if (!NumberSuffix_set0.Contains(la1))
												goto match1;
											else if (la1 == '3') {
												Skip();
												Skip();
												Match('2');
												// line 88
												result = _F;
											} else {
												Skip();
												Match('6');
												Match('4');
												// line 89
												result = _D;
											}
										}
										break;
									case 'F':
										goto match1;
									case 'D':
									case 'd':
										{
											Skip();
											// line 80
											result = _D;
											isFloat = true;
										}
										break;
									case 'M':
									case 'm':
										{
											Skip();
											// line 81
											result = _M;
											isFloat = true;
										}
										break;
									case 'L':
									case 'l':
										{
											Skip();
											// line 82
											result = _L;
											// Line 82: ([Uu])?
											la0 = LA0;
											if (la0 == 'U' || la0 == 'u') {
												Skip();
												// line 82
												result = _UL;
											}
										}
										break;
									case 'u':
										{
											la1 = LA(1);
											if (!NumberSuffix_set1.Contains(la1))
												goto match5;
											else if (la1 == '3') {
												Skip();
												Skip();
												Match('2');
												// line 84
												result = _U;
											} else {
												Skip();
												Match('6');
												Match('4');
												// line 85
												result = _UL;
											}
										}
										break;
									case 'U':
										goto match5;
									default:
										{
											la1 = LA(1);
											if (la1 == '3') {
												Match('i');
												Skip();
												Match('2');
												// line 86
												result = null;
											} else {
												Match('i');
												Match('6');
												Match('4');
												// line 87
												result = _L;
											}
										}
										break;
									}
									break;
								match1:
									{
										Skip();
										// line 79
										result = _F;
										isFloat = true;
									}
									break;
								match5:
									{
										Skip();
										// line 83
										result = _U;
										// Line 83: ([Ll])?
										la0 = LA0;
										if (la0 == 'L' || la0 == 'l') {
											Skip();
											// line 83
											result = _UL;
										}
									}
								} while (false);
							else
								goto matchNormalId;
						} else if (!NumberSuffix_set0.Contains(la1))
							// Line 79: ( [Ff] | [Dd] | [Mm] | [Ll] ([Uu])? | [Uu] ([Ll])? | [u] [3] [2] | [u] [6] [4] | [i] [3] [2] | [i] [6] [4] | [f] [3] [2] | [f] [6] [4] )
							do {
								switch (LA0) {
								case 'f':
									{
										la1 = LA(1);
										if (!NumberSuffix_set0.Contains(la1))
											goto match1;
										else if (la1 == '3') {
											Skip();
											Skip();
											Match('2');
											// line 88
											result = _F;
										} else {
											Skip();
											Match('6');
											Match('4');
											// line 89
											result = _D;
										}
									}
									break;
								case 'F':
									goto match1;
								case 'D':
								case 'd':
									{
										Skip();
										// line 80
										result = _D;
										isFloat = true;
									}
									break;
								case 'M':
								case 'm':
									{
										Skip();
										// line 81
										result = _M;
										isFloat = true;
									}
									break;
								case 'L':
								case 'l':
									{
										Skip();
										// line 82
										result = _L;
										// Line 82: ([Uu])?
										la0 = LA0;
										if (la0 == 'U' || la0 == 'u') {
											Skip();
											// line 82
											result = _UL;
										}
									}
									break;
								case 'u':
									{
										la1 = LA(1);
										if (!NumberSuffix_set1.Contains(la1))
											goto match5;
										else if (la1 == '3') {
											Skip();
											Skip();
											Match('2');
											// line 84
											result = _U;
										} else {
											Skip();
											Match('6');
											Match('4');
											// line 85
											result = _UL;
										}
									}
									break;
								case 'U':
									goto match5;
								default:
									{
										la1 = LA(1);
										if (la1 == '3') {
											Match('i');
											Skip();
											Match('2');
											// line 86
											result = null;
										} else {
											Match('i');
											Match('6');
											Match('4');
											// line 87
											result = _L;
										}
									}
									break;
								}
								break;
							match1:
								{
									Skip();
									// line 79
									result = _F;
									isFloat = true;
								}
								break;
							match5:
								{
									Skip();
									// line 83
									result = _U;
									// Line 83: ([Ll])?
									la0 = LA0;
									if (la0 == 'L' || la0 == 'l') {
										Skip();
										// line 83
										result = _UL;
									}
								}
							} while (false);
						else
							goto matchNormalId;
					}
					break;
				case 'u':
					{
						la1 = LA(1);
						switch (la1) {
						case 'L':
						case 'l':
							{
								la2 = LA(2);
								if (!NumberSuffix_set0.Contains(la2))
									// Line 79: ( [Ff] | [Dd] | [Mm] | [Ll] ([Uu])? | [Uu] ([Ll])? | [u] [3] [2] | [u] [6] [4] | [i] [3] [2] | [i] [6] [4] | [f] [3] [2] | [f] [6] [4] )
									do {
										switch (LA0) {
										case 'f':
											{
												la1 = LA(1);
												if (!NumberSuffix_set0.Contains(la1))
													goto match1;
												else if (la1 == '3') {
													Skip();
													Skip();
													Match('2');
													// line 88
													result = _F;
												} else {
													Skip();
													Match('6');
													Match('4');
													// line 89
													result = _D;
												}
											}
											break;
										case 'F':
											goto match1;
										case 'D':
										case 'd':
											{
												Skip();
												// line 80
												result = _D;
												isFloat = true;
											}
											break;
										case 'M':
										case 'm':
											{
												Skip();
												// line 81
												result = _M;
												isFloat = true;
											}
											break;
										case 'L':
										case 'l':
											{
												Skip();
												// line 82
												result = _L;
												// Line 82: ([Uu])?
												la0 = LA0;
												if (la0 == 'U' || la0 == 'u') {
													Skip();
													// line 82
													result = _UL;
												}
											}
											break;
										case 'u':
											{
												la1 = LA(1);
												if (!NumberSuffix_set1.Contains(la1))
													goto match5;
												else if (la1 == '3') {
													Skip();
													Skip();
													Match('2');
													// line 84
													result = _U;
												} else {
													Skip();
													Match('6');
													Match('4');
													// line 85
													result = _UL;
												}
											}
											break;
										case 'U':
											goto match5;
										default:
											{
												la1 = LA(1);
												if (la1 == '3') {
													Match('i');
													Skip();
													Match('2');
													// line 86
													result = null;
												} else {
													Match('i');
													Match('6');
													Match('4');
													// line 87
													result = _L;
												}
											}
											break;
										}
										break;
									match1:
										{
											Skip();
											// line 79
											result = _F;
											isFloat = true;
										}
										break;
									match5:
										{
											Skip();
											// line 83
											result = _U;
											// Line 83: ([Ll])?
											la0 = LA0;
											if (la0 == 'L' || la0 == 'l') {
												Skip();
												// line 83
												result = _UL;
											}
										}
									} while (false);
								else
									goto matchNormalId;
							}
							break;
						case '3':
							{
								la2 = LA(2);
								if (la2 == '2') {
									la3 = LA(3);
									if (!NumberSuffix_set0.Contains(la3))
										// Line 79: ( [Ff] | [Dd] | [Mm] | [Ll] ([Uu])? | [Uu] ([Ll])? | [u] [3] [2] | [u] [6] [4] | [i] [3] [2] | [i] [6] [4] | [f] [3] [2] | [f] [6] [4] )
										do {
											switch (LA0) {
											case 'f':
												{
													la1 = LA(1);
													if (!NumberSuffix_set0.Contains(la1))
														goto match1;
													else if (la1 == '3') {
														Skip();
														Skip();
														Match('2');
														// line 88
														result = _F;
													} else {
														Skip();
														Match('6');
														Match('4');
														// line 89
														result = _D;
													}
												}
												break;
											case 'F':
												goto match1;
											case 'D':
											case 'd':
												{
													Skip();
													// line 80
													result = _D;
													isFloat = true;
												}
												break;
											case 'M':
											case 'm':
												{
													Skip();
													// line 81
													result = _M;
													isFloat = true;
												}
												break;
											case 'L':
											case 'l':
												{
													Skip();
													// line 82
													result = _L;
													// Line 82: ([Uu])?
													la0 = LA0;
													if (la0 == 'U' || la0 == 'u') {
														Skip();
														// line 82
														result = _UL;
													}
												}
												break;
											case 'u':
												{
													la1 = LA(1);
													if (!NumberSuffix_set1.Contains(la1))
														goto match5;
													else if (la1 == '3') {
														Skip();
														Skip();
														Match('2');
														// line 84
														result = _U;
													} else {
														Skip();
														Match('6');
														Match('4');
														// line 85
														result = _UL;
													}
												}
												break;
											case 'U':
												goto match5;
											default:
												{
													la1 = LA(1);
													if (la1 == '3') {
														Match('i');
														Skip();
														Match('2');
														// line 86
														result = null;
													} else {
														Match('i');
														Match('6');
														Match('4');
														// line 87
														result = _L;
													}
												}
												break;
											}
											break;
										match1:
											{
												Skip();
												// line 79
												result = _F;
												isFloat = true;
											}
											break;
										match5:
											{
												Skip();
												// line 83
												result = _U;
												// Line 83: ([Ll])?
												la0 = LA0;
												if (la0 == 'L' || la0 == 'l') {
													Skip();
													// line 83
													result = _UL;
												}
											}
										} while (false);
									else
										goto matchNormalId;
								} else
									goto matchNormalId;
							}
							break;
						case '6':
							{
								la2 = LA(2);
								if (la2 == '4') {
									la3 = LA(3);
									if (!NumberSuffix_set0.Contains(la3))
										// Line 79: ( [Ff] | [Dd] | [Mm] | [Ll] ([Uu])? | [Uu] ([Ll])? | [u] [3] [2] | [u] [6] [4] | [i] [3] [2] | [i] [6] [4] | [f] [3] [2] | [f] [6] [4] )
										do {
											switch (LA0) {
											case 'f':
												{
													la1 = LA(1);
													if (!NumberSuffix_set0.Contains(la1))
														goto match1;
													else if (la1 == '3') {
														Skip();
														Skip();
														Match('2');
														// line 88
														result = _F;
													} else {
														Skip();
														Match('6');
														Match('4');
														// line 89
														result = _D;
													}
												}
												break;
											case 'F':
												goto match1;
											case 'D':
											case 'd':
												{
													Skip();
													// line 80
													result = _D;
													isFloat = true;
												}
												break;
											case 'M':
											case 'm':
												{
													Skip();
													// line 81
													result = _M;
													isFloat = true;
												}
												break;
											case 'L':
											case 'l':
												{
													Skip();
													// line 82
													result = _L;
													// Line 82: ([Uu])?
													la0 = LA0;
													if (la0 == 'U' || la0 == 'u') {
														Skip();
														// line 82
														result = _UL;
													}
												}
												break;
											case 'u':
												{
													la1 = LA(1);
													if (!NumberSuffix_set1.Contains(la1))
														goto match5;
													else if (la1 == '3') {
														Skip();
														Skip();
														Match('2');
														// line 84
														result = _U;
													} else {
														Skip();
														Match('6');
														Match('4');
														// line 85
														result = _UL;
													}
												}
												break;
											case 'U':
												goto match5;
											default:
												{
													la1 = LA(1);
													if (la1 == '3') {
														Match('i');
														Skip();
														Match('2');
														// line 86
														result = null;
													} else {
														Match('i');
														Match('6');
														Match('4');
														// line 87
														result = _L;
													}
												}
												break;
											}
											break;
										match1:
											{
												Skip();
												// line 79
												result = _F;
												isFloat = true;
											}
											break;
										match5:
											{
												Skip();
												// line 83
												result = _U;
												// Line 83: ([Ll])?
												la0 = LA0;
												if (la0 == 'L' || la0 == 'l') {
													Skip();
													// line 83
													result = _UL;
												}
											}
										} while (false);
									else
										goto matchNormalId;
								} else
									goto matchNormalId;
							}
							break;
						default:
							if (!NumberSuffix_set0.Contains(la1))
								// Line 79: ( [Ff] | [Dd] | [Mm] | [Ll] ([Uu])? | [Uu] ([Ll])? | [u] [3] [2] | [u] [6] [4] | [i] [3] [2] | [i] [6] [4] | [f] [3] [2] | [f] [6] [4] )
								do {
									switch (LA0) {
									case 'f':
										{
											la1 = LA(1);
											if (!NumberSuffix_set0.Contains(la1))
												goto match1;
											else if (la1 == '3') {
												Skip();
												Skip();
												Match('2');
												// line 88
												result = _F;
											} else {
												Skip();
												Match('6');
												Match('4');
												// line 89
												result = _D;
											}
										}
										break;
									case 'F':
										goto match1;
									case 'D':
									case 'd':
										{
											Skip();
											// line 80
											result = _D;
											isFloat = true;
										}
										break;
									case 'M':
									case 'm':
										{
											Skip();
											// line 81
											result = _M;
											isFloat = true;
										}
										break;
									case 'L':
									case 'l':
										{
											Skip();
											// line 82
											result = _L;
											// Line 82: ([Uu])?
											la0 = LA0;
											if (la0 == 'U' || la0 == 'u') {
												Skip();
												// line 82
												result = _UL;
											}
										}
										break;
									case 'u':
										{
											la1 = LA(1);
											if (!NumberSuffix_set1.Contains(la1))
												goto match5;
											else if (la1 == '3') {
												Skip();
												Skip();
												Match('2');
												// line 84
												result = _U;
											} else {
												Skip();
												Match('6');
												Match('4');
												// line 85
												result = _UL;
											}
										}
										break;
									case 'U':
										goto match5;
									default:
										{
											la1 = LA(1);
											if (la1 == '3') {
												Match('i');
												Skip();
												Match('2');
												// line 86
												result = null;
											} else {
												Match('i');
												Match('6');
												Match('4');
												// line 87
												result = _L;
											}
										}
										break;
									}
									break;
								match1:
									{
										Skip();
										// line 79
										result = _F;
										isFloat = true;
									}
									break;
								match5:
									{
										Skip();
										// line 83
										result = _U;
										// Line 83: ([Ll])?
										la0 = LA0;
										if (la0 == 'L' || la0 == 'l') {
											Skip();
											// line 83
											result = _UL;
										}
									}
								} while (false);
							else
								goto matchNormalId;
							break;
						}
					}
					break;
				case 'U':
					{
						la1 = LA(1);
						if (la1 == 'L' || la1 == 'l') {
							la2 = LA(2);
							if (!NumberSuffix_set0.Contains(la2))
								// Line 79: ( [Ff] | [Dd] | [Mm] | [Ll] ([Uu])? | [Uu] ([Ll])? | [u] [3] [2] | [u] [6] [4] | [i] [3] [2] | [i] [6] [4] | [f] [3] [2] | [f] [6] [4] )
								do {
									switch (LA0) {
									case 'f':
										{
											la1 = LA(1);
											if (!NumberSuffix_set0.Contains(la1))
												goto match1;
											else if (la1 == '3') {
												Skip();
												Skip();
												Match('2');
												// line 88
												result = _F;
											} else {
												Skip();
												Match('6');
												Match('4');
												// line 89
												result = _D;
											}
										}
										break;
									case 'F':
										goto match1;
									case 'D':
									case 'd':
										{
											Skip();
											// line 80
											result = _D;
											isFloat = true;
										}
										break;
									case 'M':
									case 'm':
										{
											Skip();
											// line 81
											result = _M;
											isFloat = true;
										}
										break;
									case 'L':
									case 'l':
										{
											Skip();
											// line 82
											result = _L;
											// Line 82: ([Uu])?
											la0 = LA0;
											if (la0 == 'U' || la0 == 'u') {
												Skip();
												// line 82
												result = _UL;
											}
										}
										break;
									case 'u':
										{
											la1 = LA(1);
											if (!NumberSuffix_set1.Contains(la1))
												goto match5;
											else if (la1 == '3') {
												Skip();
												Skip();
												Match('2');
												// line 84
												result = _U;
											} else {
												Skip();
												Match('6');
												Match('4');
												// line 85
												result = _UL;
											}
										}
										break;
									case 'U':
										goto match5;
									default:
										{
											la1 = LA(1);
											if (la1 == '3') {
												Match('i');
												Skip();
												Match('2');
												// line 86
												result = null;
											} else {
												Match('i');
												Match('6');
												Match('4');
												// line 87
												result = _L;
											}
										}
										break;
									}
									break;
								match1:
									{
										Skip();
										// line 79
										result = _F;
										isFloat = true;
									}
									break;
								match5:
									{
										Skip();
										// line 83
										result = _U;
										// Line 83: ([Ll])?
										la0 = LA0;
										if (la0 == 'L' || la0 == 'l') {
											Skip();
											// line 83
											result = _UL;
										}
									}
								} while (false);
							else
								goto matchNormalId;
						} else if (!NumberSuffix_set0.Contains(la1))
							// Line 79: ( [Ff] | [Dd] | [Mm] | [Ll] ([Uu])? | [Uu] ([Ll])? | [u] [3] [2] | [u] [6] [4] | [i] [3] [2] | [i] [6] [4] | [f] [3] [2] | [f] [6] [4] )
							do {
								switch (LA0) {
								case 'f':
									{
										la1 = LA(1);
										if (!NumberSuffix_set0.Contains(la1))
											goto match1;
										else if (la1 == '3') {
											Skip();
											Skip();
											Match('2');
											// line 88
											result = _F;
										} else {
											Skip();
											Match('6');
											Match('4');
											// line 89
											result = _D;
										}
									}
									break;
								case 'F':
									goto match1;
								case 'D':
								case 'd':
									{
										Skip();
										// line 80
										result = _D;
										isFloat = true;
									}
									break;
								case 'M':
								case 'm':
									{
										Skip();
										// line 81
										result = _M;
										isFloat = true;
									}
									break;
								case 'L':
								case 'l':
									{
										Skip();
										// line 82
										result = _L;
										// Line 82: ([Uu])?
										la0 = LA0;
										if (la0 == 'U' || la0 == 'u') {
											Skip();
											// line 82
											result = _UL;
										}
									}
									break;
								case 'u':
									{
										la1 = LA(1);
										if (!NumberSuffix_set1.Contains(la1))
											goto match5;
										else if (la1 == '3') {
											Skip();
											Skip();
											Match('2');
											// line 84
											result = _U;
										} else {
											Skip();
											Match('6');
											Match('4');
											// line 85
											result = _UL;
										}
									}
									break;
								case 'U':
									goto match5;
								default:
									{
										la1 = LA(1);
										if (la1 == '3') {
											Match('i');
											Skip();
											Match('2');
											// line 86
											result = null;
										} else {
											Match('i');
											Match('6');
											Match('4');
											// line 87
											result = _L;
										}
									}
									break;
								}
								break;
							match1:
								{
									Skip();
									// line 79
									result = _F;
									isFloat = true;
								}
								break;
							match5:
								{
									Skip();
									// line 83
									result = _U;
									// Line 83: ([Ll])?
									la0 = LA0;
									if (la0 == 'L' || la0 == 'l') {
										Skip();
										// line 83
										result = _UL;
									}
								}
							} while (false);
						else
							goto matchNormalId;
					}
					break;
				case 'i':
					{
						la1 = LA(1);
						if (la1 == '3') {
							la2 = LA(2);
							if (la2 == '2') {
								la3 = LA(3);
								if (!NumberSuffix_set0.Contains(la3))
									// Line 79: ( [Ff] | [Dd] | [Mm] | [Ll] ([Uu])? | [Uu] ([Ll])? | [u] [3] [2] | [u] [6] [4] | [i] [3] [2] | [i] [6] [4] | [f] [3] [2] | [f] [6] [4] )
									do {
										switch (LA0) {
										case 'f':
											{
												la1 = LA(1);
												if (!NumberSuffix_set0.Contains(la1))
													goto match1;
												else if (la1 == '3') {
													Skip();
													Skip();
													Match('2');
													// line 88
													result = _F;
												} else {
													Skip();
													Match('6');
													Match('4');
													// line 89
													result = _D;
												}
											}
											break;
										case 'F':
											goto match1;
										case 'D':
										case 'd':
											{
												Skip();
												// line 80
												result = _D;
												isFloat = true;
											}
											break;
										case 'M':
										case 'm':
											{
												Skip();
												// line 81
												result = _M;
												isFloat = true;
											}
											break;
										case 'L':
										case 'l':
											{
												Skip();
												// line 82
												result = _L;
												// Line 82: ([Uu])?
												la0 = LA0;
												if (la0 == 'U' || la0 == 'u') {
													Skip();
													// line 82
													result = _UL;
												}
											}
											break;
										case 'u':
											{
												la1 = LA(1);
												if (!NumberSuffix_set1.Contains(la1))
													goto match5;
												else if (la1 == '3') {
													Skip();
													Skip();
													Match('2');
													// line 84
													result = _U;
												} else {
													Skip();
													Match('6');
													Match('4');
													// line 85
													result = _UL;
												}
											}
											break;
										case 'U':
											goto match5;
										default:
											{
												la1 = LA(1);
												if (la1 == '3') {
													Match('i');
													Skip();
													Match('2');
													// line 86
													result = null;
												} else {
													Match('i');
													Match('6');
													Match('4');
													// line 87
													result = _L;
												}
											}
											break;
										}
										break;
									match1:
										{
											Skip();
											// line 79
											result = _F;
											isFloat = true;
										}
										break;
									match5:
										{
											Skip();
											// line 83
											result = _U;
											// Line 83: ([Ll])?
											la0 = LA0;
											if (la0 == 'L' || la0 == 'l') {
												Skip();
												// line 83
												result = _UL;
											}
										}
									} while (false);
								else
									goto matchNormalId;
							} else
								goto matchNormalId;
						} else if (la1 == '6') {
							la2 = LA(2);
							if (la2 == '4') {
								la3 = LA(3);
								if (!NumberSuffix_set0.Contains(la3))
									// Line 79: ( [Ff] | [Dd] | [Mm] | [Ll] ([Uu])? | [Uu] ([Ll])? | [u] [3] [2] | [u] [6] [4] | [i] [3] [2] | [i] [6] [4] | [f] [3] [2] | [f] [6] [4] )
									do {
										switch (LA0) {
										case 'f':
											{
												la1 = LA(1);
												if (!NumberSuffix_set0.Contains(la1))
													goto match1;
												else if (la1 == '3') {
													Skip();
													Skip();
													Match('2');
													// line 88
													result = _F;
												} else {
													Skip();
													Match('6');
													Match('4');
													// line 89
													result = _D;
												}
											}
											break;
										case 'F':
											goto match1;
										case 'D':
										case 'd':
											{
												Skip();
												// line 80
												result = _D;
												isFloat = true;
											}
											break;
										case 'M':
										case 'm':
											{
												Skip();
												// line 81
												result = _M;
												isFloat = true;
											}
											break;
										case 'L':
										case 'l':
											{
												Skip();
												// line 82
												result = _L;
												// Line 82: ([Uu])?
												la0 = LA0;
												if (la0 == 'U' || la0 == 'u') {
													Skip();
													// line 82
													result = _UL;
												}
											}
											break;
										case 'u':
											{
												la1 = LA(1);
												if (!NumberSuffix_set1.Contains(la1))
													goto match5;
												else if (la1 == '3') {
													Skip();
													Skip();
													Match('2');
													// line 84
													result = _U;
												} else {
													Skip();
													Match('6');
													Match('4');
													// line 85
													result = _UL;
												}
											}
											break;
										case 'U':
											goto match5;
										default:
											{
												la1 = LA(1);
												if (la1 == '3') {
													Match('i');
													Skip();
													Match('2');
													// line 86
													result = null;
												} else {
													Match('i');
													Match('6');
													Match('4');
													// line 87
													result = _L;
												}
											}
											break;
										}
										break;
									match1:
										{
											Skip();
											// line 79
											result = _F;
											isFloat = true;
										}
										break;
									match5:
										{
											Skip();
											// line 83
											result = _U;
											// Line 83: ([Ll])?
											la0 = LA0;
											if (la0 == 'L' || la0 == 'l') {
												Skip();
												// line 83
												result = _UL;
											}
										}
									} while (false);
								else
									goto matchNormalId;
							} else
								goto matchNormalId;
						} else
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
					// line 92
					result = IdToSymbol(CharSource.Slice(here, InputPosition - here));
				}
			} while (false);
			return result;
		}
		object SQString()
		{
			int la0;
			// line 100
			_parseNeeded = false;
			Skip();
			// Line 101: ([\\] [^\$] | [^\$\n\r'\\])
			la0 = LA0;
			if (la0 == '\\') {
				Skip();
				MatchExcept();
				// line 101
				_parseNeeded = true;
			} else
				MatchExcept('\n', '\r', '\'', '\\');
			Match('\'');
			// line 102
			return ParseSQStringValue();
		}
		object DQString()
		{
			int la0, la1;
			// line 105
			_parseNeeded = false;
			Skip();
			// Line 106: ([\\] [^\$] | [^\$\n\r"\\])*
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					la1 = LA(1);
					if (la1 != -1) {
						Skip();
						Skip();
						// line 106
						_parseNeeded = true;
					} else
						break;
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '"'))
					Skip();
				else
					break;
			}
			// Line 107: (["] / )
			la0 = LA0;
			if (la0 == '"')
				Skip();
			else
				// line 107
				_parseNeeded = true;
			// line 108
			return ParseStringValue(false);
		}
		object TQString()
		{
			int la0, la1, la2;
			_parseNeeded = true;
			_style = NodeStyle.Alternate;
			// Line 113: (["] ["] ["] nongreedy(Newline / [^\$])* ["] ["] ["] | ['] ['] ['] nongreedy(Newline / [^\$])* ['] ['] ['])
			la0 = LA0;
			if (la0 == '"') {
				Skip();
				Match('"');
				Match('"');
				// Line 113: nongreedy(Newline / [^\$])*
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
				// line 114
				_style |= NodeStyle.Alternate2;
				Match('\'');
				Match('\'');
				Match('\'');
				// Line 115: nongreedy(Newline / [^\$])*
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
			// line 116
			return ParseStringValue(true, true);
		}
		void BQString()
		{
			int la0;
			// line 119
			_parseNeeded = false;
			Skip();
			// Line 120: ([\\] [^\$] | [^\$\n\r\\`])*
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					Skip();
					MatchExcept();
					// line 120
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
			OpChar();
			// Line 127: (OpChar)*
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
			// line 127
			return ParseNormalOp();
		}
		static readonly HashSet<int> SQOperator_set0 = NewSetOfRanges('!', '!', '#', '&', '*', '+', '-', ':', '<', '?', 'A', 'Z', '^', '_', 'a', 'z', '|', '|', '~', '~');
		object SQOperator()
		{
			int la0;
			Skip();
			LettersOrPunc();
			// Line 129: (LettersOrPunc)*
			for (;;) {
				la0 = LA0;
				if (SQOperator_set0.Contains(la0))
					LettersOrPunc();
				else
					break;
			}
			// line 131
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
			// Line 141: ([A-Z_a-z] | IdExtLetter)
			la0 = LA0;
			if (la0 >= 'A' && la0 <= 'Z' || la0 == '_' || la0 >= 'a' && la0 <= 'z')
				Skip();
			else
				IdExtLetter();
			// Line 142: greedy( [A-Z_a-z] | [#] | [0-9] | ['] &!(['] [']) | IdExtLetter )*
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
				} else if (la0 >= 128 && la0 <= 65532) {
					la0 = LA0;
					if (char.IsLetter((char) la0))
						IdExtLetter();
					else
						break;
				} else
					break;
			}
		}
		object Id()
		{
			int la0, la1;
			object result = default(object);
			object value = default(object);
			// Line 145: (BQString | NormalId)
			la0 = LA0;
			if (la0 == '`') {
				BQString();
				// line 145
				result = ParseBQStringValue();
			} else {
				NormalId();
				// line 147
				result = IdToSymbol(Text());
				if (result == sy_true) {
					_type = TT.Literal;
					return G.BoxedTrue;
				}
				if (result == sy_false) {
					_type = TT.Literal;
					return G.BoxedFalse;
				}
				if (result == sy_null) {
					_type = TT.Literal;
					return null;
				}
			}
			// Line 153: ((TQString / DQString))?
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
						// Line 154: (TQString / DQString)
						la0 = LA0;
						if (la0 == '"') {
							la1 = LA(1);
							if (la1 == '"')
								value = TQString();
							else
								value = DQString();
						} else
							value = TQString();
						// line 156
						_type = TT.Literal;
						if (result == sy_s)
							return (Symbol) value.ToString();
						else
							return new SpecialLiteral(value, (Symbol) result);
					} finally {
						_startPosition = old_startPosition_0;
					}
				}
			} while (false);
			return result;
		}
		void LettersOrPunc()
		{
			Match(SQOperator_set0);
		}
		object SpecialLiteral()
		{
			int la0;
			object result = default(object);
			var old_startPosition_1 = _startPosition;
			try {
				Skip();
				Skip();
				// line 169
				int here = InputPosition;
				LettersOrPunc();
				// Line 170: (LettersOrPunc)*
				for (;;) {
					la0 = LA0;
					if (SQOperator_set0.Contains(la0))
						LettersOrPunc();
					else
						break;
				}
				// line 171
				var sym = CharSource.Slice(here, InputPosition - here);
				if (!NamedLiterals.TryGetValue(sym, out result))
					result = IdToSymbol(sym);
				return result;
			} finally {
				_startPosition = old_startPosition_1;
			}
		}
		object Keyword()
		{
			int la0;
			Skip();
			LettersOrPunc();
			// Line 177: (LettersOrPunc)*
			for (;;) {
				la0 = LA0;
				if (SQOperator_set0.Contains(la0))
					LettersOrPunc();
				else
					break;
			}
			// line 177
			return IdToSymbol(Text());
		}
		object Shebang()
		{
			int la0;
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
		static readonly HashSet<int> NextToken_set0 = NewSetOfRanges('#', '&', '*', '+', '-', ':', '<', '?', 'A', 'Z', '^', '_', 'a', 'z', '|', '|', '~', '~');
		static readonly HashSet<int> NextToken_set1 = NewSetOfRanges('A', 'Z', '_', 'z', 128, 65532);
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
			if (LA0 == -1) {
				return NoValue.Value;
			}
			// Line 196: ( Shebang / SpecialLiteral / Id / Keyword / Newline / SLComment / MLComment / Number / TQString / DQString / SQString / SQOperator / ['] ['] / [,] / [;] / [(] / [)] / [[] / [\]] / [{] / [}] / ['] [{] / [@] [@] [{] / [@] / Operator )
			do {
				la0 = LA0;
				switch (la0) {
				case '#':
					{
						if (InputPosition == 0) {
							la1 = LA(1);
							if (la1 == '!') {
								// line 196
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
								// line 197
								_type = TT.Literal;
								value = SpecialLiteral();
							} else if (la2 == '{') {
								// line 218
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
				case '\n':
				case '\r':
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
							la2 = LA(2);
							if (la2 != -1) {
								la3 = LA(3);
								if (la3 != -1) {
									// line 202
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
								// line 207
								_type = TT.NormalOp;
								value = SQOperator();
							}
						} else if (la1 == '{') {
							la2 = LA(2);
							if (la2 == '\'')
								goto matchSQString;
							else {
								// line 217
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
						// line 209
						_type = TT.Comma;
						Skip();
						// line 209
						value = GSymbol.Empty;
					}
					break;
				case ';':
					{
						// line 210
						_type = TT.Semicolon;
						Skip();
						// line 210
						value = GSymbol.Empty;
					}
					break;
				case '(':
					{
						// line 211
						_type = TT.LParen;
						Skip();
					}
					break;
				case ')':
					{
						// line 212
						_type = TT.RParen;
						Skip();
					}
					break;
				case '[':
					{
						// line 213
						_type = TT.LBrack;
						Skip();
					}
					break;
				case ']':
					{
						// line 214
						_type = TT.RBrack;
						Skip();
					}
					break;
				case '{':
					{
						// line 215
						_type = TT.LBrace;
						Skip();
					}
					break;
				case '}':
					{
						// line 216
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
					if (NextToken_set1.Contains(la0)) {
						// line 198
						_type = TT.Id;
						value = Id();
					} else
						goto error;
					break;
				}
				break;
			matchKeyword:
				{
					// line 199
					_type = TT.Keyword;
					value = Keyword();
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
					value = TQString();
				}
				break;
			matchDQString:
				{
					// line 205
					_type = TT.Literal;
					value = DQString();
				}
				break;
			matchSQString:
				{
					// line 206
					_type = TT.Literal;
					value = SQString();
				}
				break;
			match13:
				{
					// line 208
					_type = TT.Unknown;
					Skip();
					Skip();
				}
				break;
			match24:
				{
					// line 219
					_type = TT.At;
					Skip();
					// line 219
					value = GSymbol.Empty;
				}
				break;
			error:
				{
					Skip();
					// line 221
					_type = TT.Unknown;
				}
			} while (false);
			// line 223
			Debug.Assert(InputPosition > _startPosition);
			return new Token((int) _type, _startPosition, InputPosition - _startPosition, _style, value);
		}
		new public bool TDQStringLine()
		{
			int la0, la1, la2;
			// Line 233: nongreedy([^\$])*
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
			// Line 233: (Newline | ["] ["] ["])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				// line 233
				return false;
			} else {
				Match('"');
				Match('"');
				Match('"');
				// line 233
				return true;
			}
		}
		new public bool TSQStringLine()
		{
			int la0, la1, la2;
			// Line 236: nongreedy([^\$])*
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
			// Line 236: (Newline | ['] ['] ['])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				// line 236
				return false;
			} else {
				Match('\'');
				Match('\'');
				Match('\'');
				// line 236
				return true;
			}
		}
		new public bool MLCommentLine(ref int nested)
		{
			int la0, la1;
			// Line 239: greedy( &{nested > 0} [*] [/] / [/] [*] / [^\$\n\r*] / [*] &!([/]) )*
			for (;;) {
				la0 = LA0;
				if (la0 == '*') {
					if (nested > 0) {
						la1 = LA(1);
						if (la1 == '/') {
							Skip();
							Skip();
							// line 239
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
						// line 240
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
			// Line 244: (Newline | [*] [/])
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r') {
				Newline(true);
				// line 244
				return false;
			} else {
				Match('*');
				Match('/');
				// line 244
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
