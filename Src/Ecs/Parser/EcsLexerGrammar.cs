// Generated from EcsLexerGrammar.les by LLLPG custom tool. LLLPG version: 1.1.0.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// --verbose             Allow verbose messages (shown as 'warnings')
// --no-out-header       Suppress this message
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.LLParserGenerator;
using Loyc.Syntax;
using Loyc.Syntax.Lexing;
namespace Ecs.Parser
{
	using TT = TokenType;
	using S = CodeSymbols;
	public partial class EcsLexer
	{
		new void Newline()
		{
			base.Newline();
			_allowPPAt = InputPosition;
			_value = WhitespaceTag.Value;
		}
		void OtherContextualKeyword()
		{
			_parseNeeded = _verbatim = false;
			_type = TT.ContextualKeyword;
			ParseIdValue(0, false);
		}
		static readonly Symbol _Comma = GSymbol.Get(",");
		static readonly Symbol _Semicolon = GSymbol.Get(";");
		static readonly Symbol _At = GSymbol.Get("@");
		static readonly Symbol _DotDot = GSymbol.Get("..");
		static readonly Symbol _Dot = GSymbol.Get(".");
		static readonly Symbol _ShrSet = GSymbol.Get(">>=");
		static readonly Symbol _GE = GSymbol.Get(">=");
		static readonly Symbol _GT = GSymbol.Get(">");
		static readonly Symbol _ShlSet = GSymbol.Get("<<=");
		static readonly Symbol _LE = GSymbol.Get("<=");
		static readonly Symbol _LT = GSymbol.Get("<");
		static readonly Symbol _And = GSymbol.Get("&&");
		static readonly Symbol _AndBitsSet = GSymbol.Get("&=");
		static readonly Symbol _AndBits = GSymbol.Get("&");
		static readonly Symbol _Or = GSymbol.Get("||");
		static readonly Symbol _OrBitsSet = GSymbol.Get("|=");
		static readonly Symbol _OrBits = GSymbol.Get("|");
		static readonly Symbol _Xor = GSymbol.Get("^^");
		static readonly Symbol _XorBitsSet = GSymbol.Get("^=");
		static readonly Symbol _XorBits = GSymbol.Get("^");
		static readonly Symbol _QuickBindSet = GSymbol.Get(":=");
		static readonly Symbol _QuickBind = GSymbol.Get("=:");
		static readonly Symbol _ColonColon = GSymbol.Get("::");
		static readonly Symbol _Forward = GSymbol.Get("==>");
		static readonly Symbol _Eq = GSymbol.Get("==");
		static readonly Symbol _LambdaArrow = GSymbol.Get("=>");
		static readonly Symbol _Set = GSymbol.Get("=");
		static readonly Symbol _Neq = GSymbol.Get("!=");
		static readonly Symbol _Not = GSymbol.Get("!");
		static readonly Symbol _NotBits = GSymbol.Get("~");
		static readonly Symbol _ExpSet = GSymbol.Get("**=");
		static readonly Symbol _Exp = GSymbol.Get("**");
		static readonly Symbol _MulSet = GSymbol.Get("*=");
		static readonly Symbol _Mul = GSymbol.Get("*");
		static readonly Symbol _DivSet = GSymbol.Get("/=");
		static readonly Symbol _Div = GSymbol.Get("/");
		static readonly Symbol _ModSet = GSymbol.Get("%=");
		static readonly Symbol _Mod = GSymbol.Get("%");
		static readonly Symbol _AddSet = GSymbol.Get("+=");
		static readonly Symbol _Inc = GSymbol.Get("++");
		static readonly Symbol _Add = GSymbol.Get("+");
		static readonly Symbol _SubSet = GSymbol.Get("-=");
		static readonly Symbol _Dec = GSymbol.Get("--");
		static readonly Symbol _Sub = GSymbol.Get("-");
		static readonly Symbol _NullCoalesceSet = GSymbol.Get("??=");
		static readonly Symbol _NullCoalesce = GSymbol.Get("??");
		static readonly Symbol _NullDot = GSymbol.Get("?.");
		static readonly Symbol _QuestionMark = GSymbol.Get("?");
		static readonly Symbol _Substitute = GSymbol.Get("$");
		static readonly Symbol _Backslash = GSymbol.Get("\\");
		static readonly Symbol _Colon = GSymbol.Get(":");
		static readonly Symbol _PtrArrow = GSymbol.Get("->");
		bool AllowPP
		{
			get {
				return _startPosition == _allowPPAt;
			}
		}
		static readonly Symbol _var = GSymbol.Get("var");
		static readonly Symbol _dynamic = GSymbol.Get("dynamic");
		static readonly Symbol _trait = GSymbol.Get("trait");
		static readonly Symbol _alias = GSymbol.Get("alias");
		static readonly Symbol _assembly = GSymbol.Get("assembly");
		static readonly Symbol _module = GSymbol.Get("module");
		static readonly Symbol _await = GSymbol.Get("await");
		static readonly Symbol _where = GSymbol.Get("where");
		static readonly Symbol _select = GSymbol.Get("select");
		static readonly Symbol _from = GSymbol.Get("from");
		static readonly Symbol _join = GSymbol.Get("join");
		static readonly Symbol _on = GSymbol.Get("on");
		static readonly Symbol _equals = GSymbol.Get("equals");
		static readonly Symbol _into = GSymbol.Get("into");
		static readonly Symbol _let = GSymbol.Get("let");
		static readonly Symbol _orderby = GSymbol.Get("orderby");
		static readonly Symbol _ascending = GSymbol.Get("ascending");
		static readonly Symbol _descending = GSymbol.Get("descending");
		static readonly Symbol _group = GSymbol.Get("group");
		static readonly Symbol _by = GSymbol.Get("by");
		internal static readonly HashSet<object> LinqKeywords = new HashSet<object> { 
			_where, _select, _from, _join, _on, _equals, _into, _let, _orderby, _ascending, _descending, _group, _by
		};
		void DotIndent()
		{
			int la0, la1;
			Skip();
			Skip();
			// Line 29: ([\t ])*
			 for (;;) {
				la0 = LA0;
				if (la0 == '\t' || la0 == ' ')
					Skip();
				else
					break;
			}
			// Line 29: ([.] [\t ] ([\t ])*)*
			 for (;;) {
				la0 = LA0;
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 == '\t' || la1 == ' ') {
						Skip();
						Skip();
						// Line 29: ([\t ])*
						 for (;;) {
							la0 = LA0;
							if (la0 == '\t' || la0 == ' ')
								Skip();
							else
								break;
						}
					} else
						break;
				} else
					break;
			}
			_indentLevel = MeasureIndent(_indent = CharSource.Slice(_startPosition, InputPosition - _startPosition));
			_value = WhitespaceTag.Value;
		}
		void Spaces()
		{
			int la0;
			Skip();
			// Line 35: ([\t ])*
			 for (;;) {
				la0 = LA0;
				if (la0 == '\t' || la0 == ' ')
					Skip();
				else
					break;
			}
			if ((_allowPPAt == _startPosition))
				_allowPPAt = InputPosition;
			if ((_lineStartAt == _startPosition))
				_indentLevel = MeasureIndent(_indent = CharSource.Slice(_startPosition, InputPosition - _startPosition));
			_value = WhitespaceTag.Value;
		}
		void UTF_BOM()
		{
			Skip();
			if ((_lineStartAt == _startPosition))
				_lineStartAt = InputPosition;
			_value = WhitespaceTag.Value;
		}
		void SLComment()
		{
			int la0;
			Skip();
			Skip();
			// Line 50: ([^\$\n\r])*
			 for (;;) {
				la0 = LA0;
				if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
					Skip();
				else
					break;
			}
			_value = WhitespaceTag.Value;
		}
		void MLComment()
		{
			int la1;
			Skip();
			Skip();
			// Line 55: nongreedy( &{AllowNestedComments} MLComment / Newline / [^\$] )*
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
						if (AllowNestedComments) {
							la1 = LA(1);
							if (la1 == '*')
								MLComment();
							else
								Skip();
						} else
							Skip();
					}
					break;
				case '\n':
				case '\r':
					Newline();
					break;
				default:
					Skip();
					break;
				}
			}
		 stop:;
			Match('*');
			Match('/');
			_value = WhitespaceTag.Value;
		}
		void DecDigits()
		{
			int la0, la1;
			MatchRange('0', '9');
			// Line 61: ([0-9])*
			 for (;;) {
				la0 = LA0;
				if (la0 >= '0' && la0 <= '9')
					Skip();
				else
					break;
			}
			// Line 61: ([_] [0-9] ([0-9])*)*
			 for (;;) {
				la0 = LA0;
				if (la0 == '_') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						Skip();
						Skip();
						// Line 61: ([0-9])*
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
			// Line 63: greedy(HexDigit)*
			 for (;;) {
				la0 = LA0;
				if (HexDigit_set0.Contains(la0))
					HexDigit();
				else
					break;
			}
			// Line 63: greedy([_] HexDigit (HexDigit)*)*
			 for (;;) {
				la0 = LA0;
				if (la0 == '_') {
					la1 = LA(1);
					if (HexDigit_set0.Contains(la1)) {
						Skip();
						HexDigit();
						// Line 63: (HexDigit)*
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
			// Line 63: greedy(HexDigit)*
			 for (;;) {
				la0 = LA0;
				if (HexDigit_set0.Contains(la0))
					{if (!Scan_HexDigit())
						return false;}
				else
					break;
			}
			// Line 63: greedy([_] HexDigit (HexDigit)*)*
			 for (;;) {
				la0 = LA0;
				if (la0 == '_') {
					la1 = LA(1);
					if (HexDigit_set0.Contains(la1)) {
						if (!TryMatch('_'))
							return false;
						if (!Scan_HexDigit())
							return false;
						// Line 63: (HexDigit)*
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
			// Line 64: ([01])*
			 for (;;) {
				la0 = LA0;
				if (la0 >= '0' && la0 <= '1')
					Skip();
				else
					break;
			}
			// Line 64: ([_] [01] ([01])*)*
			 for (;;) {
				la0 = LA0;
				if (la0 == '_') {
					Skip();
					Match('0', '1');
					// Line 64: ([01])*
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
			_numberBase = 10;
			// Line 67: ([.] DecDigits | DecDigits ([.] DecDigits)?)
			la0 = LA0;
			if (la0 == '.') {
				Skip();
				DecDigits();
				_isFloat = true;
			} else {
				DecDigits();
				// Line 68: ([.] DecDigits)?
				la0 = LA0;
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						_isFloat = true;
						Skip();
						DecDigits();
					}
				}
			}
			// Line 70: ([Ee] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'E' || la0 == 'e') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
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
		void HexNumber()
		{
			int la0, la1;
			Skip();
			Skip();
			_numberBase = 16;
			// Line 74: (HexDigits)?
			la0 = LA0;
			if (HexDigit_set0.Contains(la0))
				HexDigits();
			// Line 76: ([.] &(([0-9] / HexDigits [Pp] [+\-0-9])) HexDigits)?
			la0 = LA0;
			if (la0 == '.') {
				la1 = LA(1);
				if (HexDigit_set0.Contains(la1)) {
					if (Try_HexNumber_Test0(1)) {
						Skip();
						_isFloat = true;
						HexDigits();
					}
				}
			}
			// Line 78: ([Pp] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'P' || la0 == 'p') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					_isFloat = true;
					Skip();
					// Line 78: ([+\-])?
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
			_numberBase = 2;
			// Line 82: ([.] DecDigits | DecDigits ([.] DecDigits)?)
			la0 = LA0;
			if (la0 == '.') {
				Skip();
				DecDigits();
				_isFloat = true;
			} else {
				DecDigits();
				// Line 83: ([.] DecDigits)?
				la0 = LA0;
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						_isFloat = true;
						Skip();
						DecDigits();
					}
				}
			}
			// Line 85: ([Pp] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'P' || la0 == 'p') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					_isFloat = true;
					Skip();
					// Line 85: ([+\-])?
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
			_isFloat = _isNegative = false;
			_typeSuffix = null;
			// Line 89: ([\-])?
			la0 = LA0;
			if (la0 == '-') {
				Skip();
				_isNegative = true;
			}
			// Line 90: ( HexNumber / BinNumber / DecNumber )
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
			// Line 91: ( [Ff] | [Dd] | [Mm] | [Ll] ([Uu])? | [Uu] ([Ll])? )?
			 switch (LA0) {
			case 'F':
			case 'f':
				{
					Skip();
					_typeSuffix = _F;
					_isFloat = true;
				}
				break;
			case 'D':
			case 'd':
				{
					Skip();
					_typeSuffix = _D;
					_isFloat = true;
				}
				break;
			case 'M':
			case 'm':
				{
					Skip();
					_typeSuffix = _M;
					_isFloat = true;
				}
				break;
			case 'L':
			case 'l':
				{
					Skip();
					_typeSuffix = _L;
					// Line 95: ([Uu])?
					la0 = LA0;
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
					// Line 96: ([Ll])?
					la0 = LA0;
					if (la0 == 'L' || la0 == 'l') {
						Skip();
						_typeSuffix = _UL;
					}
				}
				break;
			}
			ParseNumberValue();
		}
		void SQString()
		{
			int la0;
			_parseNeeded = false;
			_verbatim = false;
			Skip();
			// Line 106: ([\\] [^\$] | [^\$\n\r'\\])*
			 for (;;) {
				la0 = LA0;
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
			ParseSQStringValue();
		}
		void DQString()
		{
			int la0, la1;
			_parseNeeded = false;
			// Line 111: (["] ([\\] [^\$] | [^\$\n\r"\\])* ["] | [@] ["] (["] ["] / [^\$"])* ["])
			la0 = LA0;
			if (la0 == '"') {
				_verbatim = false;
				Skip();
				// Line 112: ([\\] [^\$] | [^\$\n\r"\\])*
				 for (;;) {
					la0 = LA0;
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
				_verbatim = true;
				_style = NodeStyle.Alternate;
				Match('@');
				Match('"');
				// Line 114: (["] ["] / [^\$"])*
				 for (;;) {
					la0 = LA0;
					if (la0 == '"') {
						la1 = LA(1);
						if (la1 == '"') {
							Skip();
							Skip();
							_parseNeeded = true;
						} else
							break;
					} else if (la0 != -1)
						Skip();
					else
						break;
				}
				Match('"');
			}
			ParseStringValue();
		}
		void TQString()
		{
			int la0, la1, la2;
			_parseNeeded = true;
			_style = NodeStyle.Alternate2;
			// Line 121: (["] ["] ["] nongreedy([^\$])* ["] ["] ["] | ['] ['] ['] nongreedy([^\$])* ['] ['] ['])
			la0 = LA0;
			if (la0 == '"') {
				Skip();
				Match('"');
				Match('"');
				// Line 121: nongreedy([^\$])*
				 for (;;) {
					la0 = LA0;
					if (la0 == '"') {
						la1 = LA(1);
						if (la1 == '"') {
							la2 = LA(2);
							if (la2 == -1 || la2 == '"')
								break;
							else
								Skip();
						} else if (la1 == -1)
							break;
						else
							Skip();
					} else if (la0 == -1)
						break;
					else
						Skip();
				}
				Match('"');
				Match('"');
				Match('"');
			} else {
				Match('\'');
				Match('\'');
				Match('\'');
				// Line 122: nongreedy([^\$])*
				 for (;;) {
					la0 = LA0;
					if (la0 == '\'') {
						la1 = LA(1);
						if (la1 == '\'') {
							la2 = LA(2);
							if (la2 == -1 || la2 == '\'')
								break;
							else
								Skip();
						} else if (la1 == -1)
							break;
						else
							Skip();
					} else if (la0 == -1)
						break;
					else
						Skip();
				}
				Match('\'');
				Match('\'');
				Match('\'');
				_style = NodeStyle.Alternate | NodeStyle.Alternate2;
			}
			ParseStringValue();
		}
		void BQStringN()
		{
			int la0;
			_verbatim = false;
			Skip();
			// Line 130: ([\\] [^\$] | [^\$\n\r\\`])*
			 for (;;) {
				la0 = LA0;
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
		void BQString()
		{
			_parseNeeded = false;
			BQStringN();
			ParseBQStringValue();
		}
		void IdStartChar()
		{
			Skip();
		}
		void IdUniLetter()
		{
			int la0, la1;
			// Line 143: ( &{@char.IsLetter(LA0->@char)} (128..65532) | [\\] [u] HexDigit HexDigit HexDigit HexDigit | [\\] [U] HexDigit HexDigit HexDigit HexDigit HexDigit HexDigit HexDigit HexDigit )
			la0 = LA0;
			if (la0 >= 128 && la0 <= 65532) {
				Check(char.IsLetter((char) LA0), "@char.IsLetter(LA0->@char)");
				Skip();
			} else {
				la1 = LA(1);
				if (la1 == 'u') {
					Match('\\');
					Skip();
					HexDigit();
					HexDigit();
					HexDigit();
					HexDigit();
					_parseNeeded = true;
				} else {
					Match('\\');
					Match('U');
					HexDigit();
					HexDigit();
					HexDigit();
					HexDigit();
					HexDigit();
					HexDigit();
					HexDigit();
					HexDigit();
					_parseNeeded = true;
				}
			}
		}
		void IdContChars()
		{
			int la0, la1, la2, la3, la4, la5, la6, la7, la8;
			// Line 148: ( [#'0-9] | IdStartChar | IdUniLetter )*
			 for (;;) {
				la0 = LA0;
				if (la0 == '#' || la0 == '\'' || la0 >= '0' && la0 <= '9')
					Skip();
				else if (la0 >= 'A' && la0 <= 'Z' || la0 == '_' || la0 >= 'a' && la0 <= 'z')
					IdStartChar();
				else if (la0 >= 128 && la0 <= 65532) {
					if (char.IsLetter((char) LA0))
						IdUniLetter();
					else
						break;
				} else if (la0 == '\\') {
					la1 = LA(1);
					if (la1 == 'u') {
						la2 = LA(2);
						if (HexDigit_set0.Contains(la2)) {
							la3 = LA(3);
							if (HexDigit_set0.Contains(la3)) {
								la4 = LA(4);
								if (HexDigit_set0.Contains(la4)) {
									la5 = LA(5);
									if (HexDigit_set0.Contains(la5))
										IdUniLetter();
									else
										break;
								} else
									break;
							} else
								break;
						} else
							break;
					} else if (la1 == 'U') {
						la2 = LA(2);
						if (HexDigit_set0.Contains(la2)) {
							la3 = LA(3);
							if (HexDigit_set0.Contains(la3)) {
								la4 = LA(4);
								if (HexDigit_set0.Contains(la4)) {
									la5 = LA(5);
									if (HexDigit_set0.Contains(la5)) {
										la6 = LA(6);
										if (HexDigit_set0.Contains(la6)) {
											la7 = LA(7);
											if (HexDigit_set0.Contains(la7)) {
												la8 = LA(8);
												if (HexDigit_set0.Contains(la8))
													IdUniLetter();
												else
													break;
											} else
												break;
										} else
											break;
									} else
										break;
								} else
									break;
							} else
								break;
						} else
							break;
					} else
						break;
				} else
					break;
			}
		}
		void NormalId()
		{
			int la0;
			// Line 149: (IdStartChar | IdUniLetter)
			la0 = LA0;
			if (la0 >= 'A' && la0 <= 'Z' || la0 == '_' || la0 >= 'a' && la0 <= 'z')
				IdStartChar();
			else
				IdUniLetter();
			IdContChars();
		}
		void HashId()
		{
			Skip();
			IdContChars();
		}
		void CommentStart()
		{
			Match('/');
			Match('*', '/');
		}
		static readonly HashSet<int> FancyId_set0 = NewSetOfRanges('!', '!', '#', '\'', '*', '+', '-', ':', '<', '?', 'A', 'Z', '^', '_', 'a', 'z', '|', '|', '~', '~');
		bool FancyId()
		{
			int la0, la1, la2, la3, la4, la5, la6, la7, la8;
			// Line 155: (BQStringN | (IdUniLetter / LettersOrPunc) (IdUniLetter / LettersOrPunc)*)
			la0 = LA0;
			if (la0 == '`') {
				BQStringN();
				return true;
			} else {
				// Line 156: (IdUniLetter / LettersOrPunc)
				la0 = LA0;
				if (la0 >= 128 && la0 <= 65532)
					IdUniLetter();
				else if (la0 == '\\') {
					la1 = LA(1);
					if (la1 == 'u') {
						la2 = LA(2);
						if (HexDigit_set0.Contains(la2)) {
							la3 = LA(3);
							if (HexDigit_set0.Contains(la3)) {
								la4 = LA(4);
								if (HexDigit_set0.Contains(la4)) {
									la5 = LA(5);
									if (HexDigit_set0.Contains(la5))
										IdUniLetter();
									else
										LettersOrPunc();
								} else
									LettersOrPunc();
							} else
								LettersOrPunc();
						} else
							LettersOrPunc();
					} else if (la1 == 'U') {
						la2 = LA(2);
						if (HexDigit_set0.Contains(la2)) {
							la3 = LA(3);
							if (HexDigit_set0.Contains(la3)) {
								la4 = LA(4);
								if (HexDigit_set0.Contains(la4)) {
									la5 = LA(5);
									if (HexDigit_set0.Contains(la5)) {
										la6 = LA(6);
										if (HexDigit_set0.Contains(la6)) {
											la7 = LA(7);
											if (HexDigit_set0.Contains(la7)) {
												la8 = LA(8);
												if (HexDigit_set0.Contains(la8))
													IdUniLetter();
												else
													LettersOrPunc();
											} else
												LettersOrPunc();
										} else
											LettersOrPunc();
									} else
										LettersOrPunc();
								} else
									LettersOrPunc();
							} else
								LettersOrPunc();
						} else
							LettersOrPunc();
					} else
						LettersOrPunc();
				} else
					LettersOrPunc();
				// Line 156: (IdUniLetter / LettersOrPunc)*
				 for (;;) {
					la0 = LA0;
					if (la0 >= 128 && la0 <= 65532) {
						if (char.IsLetter((char) LA0))
							IdUniLetter();
						else
							break;
					} else if (la0 == '\\') {
						la1 = LA(1);
						if (la1 == 'u') {
							la2 = LA(2);
							if (HexDigit_set0.Contains(la2)) {
								la3 = LA(3);
								if (HexDigit_set0.Contains(la3)) {
									la4 = LA(4);
									if (HexDigit_set0.Contains(la4)) {
										la5 = LA(5);
										if (HexDigit_set0.Contains(la5))
											IdUniLetter();
										else
											LettersOrPunc();
									} else
										LettersOrPunc();
								} else
									LettersOrPunc();
							} else
								LettersOrPunc();
						} else if (la1 == 'U') {
							la2 = LA(2);
							if (HexDigit_set0.Contains(la2)) {
								la3 = LA(3);
								if (HexDigit_set0.Contains(la3)) {
									la4 = LA(4);
									if (HexDigit_set0.Contains(la4)) {
										la5 = LA(5);
										if (HexDigit_set0.Contains(la5)) {
											la6 = LA(6);
											if (HexDigit_set0.Contains(la6)) {
												la7 = LA(7);
												if (HexDigit_set0.Contains(la7)) {
													la8 = LA(8);
													if (HexDigit_set0.Contains(la8))
														IdUniLetter();
													else
														LettersOrPunc();
												} else
													LettersOrPunc();
											} else
												LettersOrPunc();
										} else
											LettersOrPunc();
									} else
										LettersOrPunc();
								} else
									LettersOrPunc();
							} else
								LettersOrPunc();
						} else
							LettersOrPunc();
					} else if (FancyId_set0.Contains(la0))
						LettersOrPunc();
					else
						break;
				}
				return false;
			}
		}
		static readonly HashSet<int> Symbol_set0 = NewSetOfRanges('A', 'Z', '_', '_', 'a', 'z', 128, 65532);
		void Symbol()
		{
			int la0, la1;
			_parseNeeded = _verbatim = false;
			bool isBQ = false;
			Skip();
			Skip();
			// Line 162: (NormalId / FancyId)
			la0 = LA0;
			if (Symbol_set0.Contains(la0))
				NormalId();
			else if (la0 == '\\') {
				la1 = LA(1);
				if (la1 == 'U' || la1 == 'u')
					NormalId();
				else
					isBQ = FancyId();
			} else
				isBQ = FancyId();
			ParseSymbolValue(isBQ);
		}
		static readonly HashSet<int> Id_set0 = NewSetOfRanges('A', 'Z', '\\', '\\', '_', '_', 'a', 'z', 128, 65532);
		void Id()
		{
			int la0, la1, la2, la3, la4, la5, la6;
			_parseNeeded = _verbatim = false;
			bool isBQ = false;
			int skipAt = 0;
			// Line 170: ( default NormalId | HashId | [@] (NormalId / FancyId) )
			la0 = LA0;
			if (Id_set0.Contains(la0))
				NormalId();
			else if (la0 == '#')
				HashId();
			else if (la0 == '@') {
				Skip();
				// Line 171: (NormalId / FancyId)
				la0 = LA0;
				if (Symbol_set0.Contains(la0))
					NormalId();
				else if (la0 == '\\') {
					la1 = LA(1);
					if (la1 == 'u') {
						la2 = LA(2);
						if (HexDigit_set0.Contains(la2)) {
							la3 = LA(3);
							if (HexDigit_set0.Contains(la3)) {
								la4 = LA(4);
								if (HexDigit_set0.Contains(la4)) {
									la5 = LA(5);
									if (HexDigit_set0.Contains(la5))
										NormalId();
									else
										isBQ = FancyId();
								} else
									isBQ = FancyId();
							} else
								isBQ = FancyId();
						} else
							isBQ = FancyId();
					} else if (la1 == 'U') {
						la2 = LA(2);
						if (HexDigit_set0.Contains(la2)) {
							la3 = LA(3);
							if (HexDigit_set0.Contains(la3)) {
								la4 = LA(4);
								if (HexDigit_set0.Contains(la4)) {
									la5 = LA(5);
									if (HexDigit_set0.Contains(la5)) {
										la6 = LA(6);
										if (HexDigit_set0.Contains(la6))
											NormalId();
										else
											isBQ = FancyId();
									} else
										isBQ = FancyId();
								} else
									isBQ = FancyId();
							} else
								isBQ = FancyId();
						} else
							isBQ = FancyId();
					} else
						isBQ = FancyId();
				} else
					isBQ = FancyId();
				skipAt = 1;
			} else
				NormalId();
			ParseIdValue(skipAt, isBQ);
		}
		static readonly HashSet<int> LettersOrPunc_set0 = NewSetOfRanges('!', '!', '#', '\'', '*', '+', '-', ':', '<', '?', 'A', 'Z', '\\', '\\', '^', '_', 'a', 'z', '|', '|', '~', '~');
		void LettersOrPunc()
		{
			Match(LettersOrPunc_set0);
		}
		void Comma()
		{
			Skip();
			_type = TT.Comma;
			_value = _Comma;
		}
		void Semicolon()
		{
			Skip();
			_type = TT.Semicolon;
			_value = _Semicolon;
		}
		void At()
		{
			Skip();
			_type = TT.At;
			_value = _At;
		}
		void Operator()
		{
			int la1, la2;
			// Line 191: ( (((((((((((([.] [.] / [.]) | ([>] [>] [=] / [>] [=] / [>] / [<] [<] [=] / [<] [=] / [<])) | ([&] [&] / [&] [=] / [&])) | ([|] [|] / [|] [=] / [|])) | ([\^] [\^] / [\^] [=] / [\^])) | ([:] [=] / [=] [:] / [:] [:] / [:] / [=] [=] [>] / [=] [=] / [=] [>] / [=])) | ([!] [=] / [!]) | [~]) | ([*] [*] [=] / [*] [*] / [*] [=] / [*])) | ([/] [=] / [/])) | ([%] [=] / [%])) | ([+] [=] / [+] [+] / [+])) | ([\-] [>] / [\-] [=] / [\-] [\-] / [\-])) | ([?] [?] [=] / [?] [?] / [?] [.] / [?]) | [$] | [\\] )
			 do {
				switch (LA0) {
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
								_type = TT.CompoundSet;
								_value = _ShrSet;
							} else
								goto match5;
						} else if (la1 == '=') {
							Skip();
							Skip();
							_type = TT.LEGE;
							_value = _GE;
						} else
							goto match5;
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
								_type = TT.CompoundSet;
								_value = _ShlSet;
							} else
								goto match8;
						} else if (la1 == '=') {
							Skip();
							Skip();
							_type = TT.LEGE;
							_value = _LE;
						} else
							goto match8;
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
							_type = TT.CompoundSet;
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
							_type = TT.OrXor;
							_value = _Or;
						} else if (la1 == '=') {
							Skip();
							Skip();
							_type = TT.CompoundSet;
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
							_type = TT.OrXor;
							_value = _Xor;
						} else if (la1 == '=') {
							Skip();
							Skip();
							_type = TT.CompoundSet;
							_value = _XorBitsSet;
						} else {
							Skip();
							_type = TT.XorBits;
							_value = _XorBits;
						}
					}
					break;
				case ':':
					{
						la1 = LA(1);
						if (la1 == '=') {
							Skip();
							Skip();
							_type = TT.CompoundSet;
							_value = _QuickBindSet;
						} else if (la1 == ':') {
							Skip();
							Skip();
							_type = TT.ColonColon;
							_value = _ColonColon;
						} else {
							Skip();
							_type = TT.Colon;
							_value = _Colon;
						}
					}
					break;
				case '=':
					{
						la1 = LA(1);
						if (la1 == ':') {
							Skip();
							Skip();
							_type = TT.QuickBind;
							_value = _QuickBind;
						} else if (la1 == '=') {
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
								_type = TT.EqNeq;
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
							_type = TT.EqNeq;
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
								_type = TT.CompoundSet;
								_value = _ExpSet;
							} else {
								Skip();
								Skip();
								_type = TT.Power;
								_value = _Exp;
							}
						} else if (la1 == '=') {
							Skip();
							Skip();
							_type = TT.CompoundSet;
							_value = _MulSet;
						} else {
							Skip();
							_type = TT.Mul;
							_value = _Mul;
						}
					}
					break;
				case '/':
					{
						la1 = LA(1);
						if (la1 == '=') {
							Skip();
							Skip();
							_type = TT.CompoundSet;
							_value = _DivSet;
						} else {
							Skip();
							_type = TT.DivMod;
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
							_type = TT.CompoundSet;
							_value = _ModSet;
						} else {
							Skip();
							_type = TT.DivMod;
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
							_type = TT.CompoundSet;
							_value = _AddSet;
						} else if (la1 == '+') {
							Skip();
							Skip();
							_type = TT.IncDec;
							_value = _Inc;
						} else {
							Skip();
							_type = TT.Add;
							_value = _Add;
						}
					}
					break;
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
							_type = TT.CompoundSet;
							_value = _SubSet;
						} else if (la1 == '-') {
							Skip();
							Skip();
							_type = TT.IncDec;
							_value = _Dec;
						} else {
							Skip();
							_type = TT.Sub;
							_value = _Sub;
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
								_type = TT.CompoundSet;
								_value = _NullCoalesceSet;
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
				case '$':
					{
						Skip();
						_type = TT.Substitute;
						_value = _Substitute;
					}
					break;
				default:
					{
						Match('\\');
						_type = TT.Backslash;
						_value = _Backslash;
					}
					break;
				}
				break;
			match5:
				{
					Skip();
					_type = TT.GT;
					_value = _GT;
				}
				break;
			match8:
				{
					Skip();
					_type = TT.LT;
					_value = _LT;
				}
			} while (false);
		}
		void Shebang()
		{
			int la0;
			Skip();
			Skip();
			// Line 297: ([^\$\n\r])*
			 for (;;) {
				la0 = LA0;
				if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
					Skip();
				else
					break;
			}
			// Line 297: (Newline)?
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r')
				Newline();
		}
		static readonly HashSet<int> IdOrKeyword_set0 = NewSetOfRanges('#', '#', '0', '9', 'A', 'Z', '_', '_', 'a', 'z');
		void IdOrKeyword()
		{
			int la1, la2, la3, la4, la5, la6, la7, la8, la9, la10;
			// Line 305: ( [a] [b] [s] [t] [r] [a] [c] [t] EndId =>  / [a] [s] EndId =>  / [b] [a] [s] [e] EndId =>  / [b] [o] [o] [l] EndId =>  / [b] [r] [e] [a] [k] EndId =>  / [b] [y] [t] [e] EndId =>  / [c] [a] [s] [e] EndId =>  / [c] [a] [t] [c] [h] EndId =>  / [c] [h] [a] [r] EndId =>  / [c] [h] [e] [c] [k] [e] [d] EndId =>  / [c] [l] [a] [s] [s] EndId =>  / [c] [o] [n] [s] [t] EndId =>  / [c] [o] [n] [t] [i] [n] [u] [e] EndId =>  / [d] [e] [c] [i] [m] [a] [l] EndId =>  / [d] [e] [f] [a] [u] [l] [t] EndId =>  / [d] [e] [l] [e] [g] [a] [t] [e] EndId =>  / [d] [o] [u] [b] [l] [e] EndId =>  / [d] [o] EndId =>  / [e] [l] [s] [e] EndId =>  / [e] [n] [u] [m] EndId =>  / [e] [v] [e] [n] [t] EndId =>  / [e] [x] [p] [l] [i] [c] [i] [t] EndId =>  / [e] [x] [t] [e] [r] [n] EndId =>  / [f] [a] [l] [s] [e] EndId =>  / [f] [i] [n] [a] [l] [l] [y] EndId =>  / [f] [i] [x] [e] [d] EndId =>  / [f] [l] [o] [a] [t] EndId =>  / [f] [o] [r] [e] [a] [c] [h] EndId =>  / [f] [o] [r] EndId =>  / [g] [o] [t] [o] EndId =>  / [i] [f] EndId =>  / [i] [m] [p] [l] [i] [c] [i] [t] EndId =>  / [i] [n] [t] [e] [r] [f] [a] [c] [e] EndId =>  / [i] [n] [t] [e] [r] [n] [a] [l] EndId =>  / [i] [n] [t] EndId =>  / [i] [n] EndId =>  / [i] [s] EndId =>  / [l] [o] [c] [k] EndId =>  / [l] [o] [n] [g] EndId =>  / [n] [a] [m] [e] [s] [p] [a] [c] [e] EndId =>  / [n] [e] [w] EndId =>  / [n] [u] [l] [l] EndId =>  / [o] [b] [j] [e] [c] [t] EndId =>  / [o] [p] [e] [r] [a] [t] [o] [r] EndId =>  / [o] [u] [t] EndId =>  / [o] [v] [e] [r] [r] [i] [d] [e] EndId =>  / [p] [a] [r] [a] [m] [s] EndId =>  / [p] [r] [i] [v] [a] [t] [e] EndId =>  / [p] [r] [o] [t] [e] [c] [t] [e] [d] EndId =>  / [p] [u] [b] [l] [i] [c] EndId =>  / [r] [e] [a] [d] [o] [n] [l] [y] EndId =>  / [r] [e] [f] EndId =>  / [r] [e] [t] [u] [r] [n] EndId =>  / [s] [b] [y] [t] [e] EndId =>  / [s] [e] [a] [l] [e] [d] EndId =>  / [s] [h] [o] [r] [t] EndId =>  / [s] [i] [z] [e] [o] [f] EndId =>  / [s] [t] [a] [c] [k] [a] [l] [l] [o] [c] EndId =>  / [s] [t] [a] [t] [i] [c] EndId =>  / [s] [t] [r] [i] [n] [g] EndId =>  / [s] [t] [r] [u] [c] [t] EndId =>  / [s] [w] [i] [t] [c] [h] EndId =>  / [t] [h] [i] [s] EndId =>  / [t] [h] [r] [o] [w] EndId =>  / [t] [r] [u] [e] EndId =>  / [t] [r] [y] EndId =>  / [t] [y] [p] [e] [o] [f] EndId =>  / [u] [i] [n] [t] EndId =>  / [u] [l] [o] [n] [g] EndId =>  / [u] [n] [c] [h] [e] [c] [k] [e] [d] EndId =>  / [u] [n] [s] [a] [f] [e] EndId =>  / [u] [s] [h] [o] [r] [t] EndId =>  / [u] [s] [i] [n] [g] EndId =>  / [v] [i] [r] [t] [u] [a] [l] EndId =>  / [v] [o] [l] [a] [t] [i] [l] [e] EndId =>  / [v] [o] [i] [d] EndId =>  / [w] [h] [i] [l] [e] EndId =>  / &{AllowPP} [#] [i] [f] EndId =>  / &{AllowPP} [#] [e] [l] [s] [e] EndId =>  / &{AllowPP} [#] [e] [l] [i] [f] EndId =>  / &{AllowPP} [#] [e] [n] [d] [i] [f] EndId =>  / &{AllowPP} [#] [d] [e] [f] [i] [n] [e] EndId =>  / &{AllowPP} [#] [u] [n] [d] [e] [f] EndId =>  / &{AllowPP} [#] [p] [r] [a] [g] [m] [a] EndId =>  / &{AllowPP} [#] [l] [i] [n] [e] EndId =>  / &{AllowPP} [#] [e] [r] [r] [o] [r] EndId => RestOfPPLine / &{AllowPP} [#] [w] [a] [r] [n] [i] [n] [g] EndId => RestOfPPLine / &{AllowPP} [#] [n] [o] [t] [e] EndId => RestOfPPLine / &{AllowPP} [#] [r] [e] [g] [i] [o] [n] EndId => RestOfPPLine / &{AllowPP} [#] [e] [n] [d] [r] [e] [g] [i] [o] [n] EndId =>  / [v] [a] [r] EndId =>  / [d] [y] [n] [a] [m] [i] [c] EndId =>  / [t] [r] [a] [i] [t] EndId =>  / [a] [l] [i] [a] [s] EndId =>  / [a] [s] [s] [e] [m] [b] [l] [y] EndId =>  / [m] [o] [d] [u] [l] [e] EndId =>  / [f] [r] [o] [m] EndId =>  / [w] [h] [e] [r] [e] EndId =>  / [s] [e] [l] [e] [c] [t] EndId =>  / [j] [o] [i] [n] EndId =>  / [o] [n] EndId =>  / [e] [q] [u] [a] [l] [s] EndId =>  / [i] [n] [t] [o] EndId =>  / [l] [e] [t] EndId =>  / [o] [r] [d] [e] [r] [b] [y] EndId =>  / [a] [s] [c] [e] [n] [d] [i] [n] [g] EndId =>  / [d] [e] [s] [c] [e] [n] [d] [i] [n] [g] EndId =>  / [g] [r] [o] [u] [p] EndId =>  / [b] [y] EndId =>  / [a] [w] [a] [i] [t] EndId =>  / Id )
			 switch (LA0) {
			case 'a':
				{
					la1 = LA(1);
					if (la1 == 'b') {
						la2 = LA(2);
						if (la2 == 's') {
							la3 = LA(3);
							if (la3 == 't') {
								la4 = LA(4);
								if (la4 == 'r') {
									la5 = LA(5);
									if (la5 == 'a') {
										la6 = LA(6);
										if (la6 == 'c') {
											la7 = LA(7);
											if (la7 == 't') {
												la8 = LA(8);
												if (!IdOrKeyword_set0.Contains(la8)) {
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													_type = TT.AttrKeyword;
													_value = S.Abstract;
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else if (la1 == 's') {
						la2 = LA(2);
						if (!IdOrKeyword_set0.Contains(la2)) {
							Skip();
							Skip();
							_type = TT.@as;
							_value = S.As;
						} else if (la2 == 's') {
							la3 = LA(3);
							if (la3 == 'e') {
								la4 = LA(4);
								if (la4 == 'm') {
									la5 = LA(5);
									if (la5 == 'b') {
										la6 = LA(6);
										if (la6 == 'l') {
											la7 = LA(7);
											if (la7 == 'y') {
												la8 = LA(8);
												if (!IdOrKeyword_set0.Contains(la8)) {
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													_type = TT.ContextualKeyword;
													_value = _assembly;
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else if (la2 == 'c') {
							la3 = LA(3);
							if (la3 == 'e') {
								la4 = LA(4);
								if (la4 == 'n') {
									la5 = LA(5);
									if (la5 == 'd') {
										la6 = LA(6);
										if (la6 == 'i') {
											la7 = LA(7);
											if (la7 == 'n') {
												la8 = LA(8);
												if (la8 == 'g') {
													la9 = LA(9);
													if (!IdOrKeyword_set0.Contains(la9)) {
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														OtherContextualKeyword();
													} else
														Id();
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else if (la1 == 'l') {
						la2 = LA(2);
						if (la2 == 'i') {
							la3 = LA(3);
							if (la3 == 'a') {
								la4 = LA(4);
								if (la4 == 's') {
									la5 = LA(5);
									if (!IdOrKeyword_set0.Contains(la5)) {
										Skip();
										Skip();
										Skip();
										Skip();
										Skip();
										_type = TT.ContextualKeyword;
										_value = _alias;
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else if (la1 == 'w') {
						la2 = LA(2);
						if (la2 == 'a') {
							la3 = LA(3);
							if (la3 == 'i') {
								la4 = LA(4);
								if (la4 == 't') {
									la5 = LA(5);
									if (!IdOrKeyword_set0.Contains(la5)) {
										Skip();
										Skip();
										Skip();
										Skip();
										Skip();
										_type = TT.ContextualKeyword;
										_value = _await;
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else
						Id();
				}
				break;
			case 'b':
				{
					la1 = LA(1);
					if (la1 == 'a') {
						la2 = LA(2);
						if (la2 == 's') {
							la3 = LA(3);
							if (la3 == 'e') {
								la4 = LA(4);
								if (!IdOrKeyword_set0.Contains(la4)) {
									Skip();
									Skip();
									Skip();
									Skip();
									_type = TT.@base;
									_value = S.Base;
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else if (la1 == 'o') {
						la2 = LA(2);
						if (la2 == 'o') {
							la3 = LA(3);
							if (la3 == 'l') {
								la4 = LA(4);
								if (!IdOrKeyword_set0.Contains(la4)) {
									Skip();
									Skip();
									Skip();
									Skip();
									_type = TT.TypeKeyword;
									_value = S.Bool;
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else if (la1 == 'r') {
						la2 = LA(2);
						if (la2 == 'e') {
							la3 = LA(3);
							if (la3 == 'a') {
								la4 = LA(4);
								if (la4 == 'k') {
									la5 = LA(5);
									if (!IdOrKeyword_set0.Contains(la5)) {
										Skip();
										Skip();
										Skip();
										Skip();
										Skip();
										_type = TT.@break;
										_value = S.Break;
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else if (la1 == 'y') {
						la2 = LA(2);
						if (la2 == 't') {
							la3 = LA(3);
							if (la3 == 'e') {
								la4 = LA(4);
								if (!IdOrKeyword_set0.Contains(la4)) {
									Skip();
									Skip();
									Skip();
									Skip();
									_type = TT.TypeKeyword;
									_value = S.UInt8;
								} else
									Id();
							} else
								Id();
						} else if (!IdOrKeyword_set0.Contains(la2)) {
							Skip();
							Skip();
							OtherContextualKeyword();
						} else
							Id();
					} else
						Id();
				}
				break;
			case 'c':
				{
					la1 = LA(1);
					if (la1 == 'a') {
						la2 = LA(2);
						if (la2 == 's') {
							la3 = LA(3);
							if (la3 == 'e') {
								la4 = LA(4);
								if (!IdOrKeyword_set0.Contains(la4)) {
									Skip();
									Skip();
									Skip();
									Skip();
									_type = TT.@case;
									_value = S.Case;
								} else
									Id();
							} else
								Id();
						} else if (la2 == 't') {
							la3 = LA(3);
							if (la3 == 'c') {
								la4 = LA(4);
								if (la4 == 'h') {
									la5 = LA(5);
									if (!IdOrKeyword_set0.Contains(la5)) {
										Skip();
										Skip();
										Skip();
										Skip();
										Skip();
										_type = TT.@catch;
										_value = S.Catch;
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else if (la1 == 'h') {
						la2 = LA(2);
						if (la2 == 'a') {
							la3 = LA(3);
							if (la3 == 'r') {
								la4 = LA(4);
								if (!IdOrKeyword_set0.Contains(la4)) {
									Skip();
									Skip();
									Skip();
									Skip();
									_type = TT.TypeKeyword;
									_value = S.Char;
								} else
									Id();
							} else
								Id();
						} else if (la2 == 'e') {
							la3 = LA(3);
							if (la3 == 'c') {
								la4 = LA(4);
								if (la4 == 'k') {
									la5 = LA(5);
									if (la5 == 'e') {
										la6 = LA(6);
										if (la6 == 'd') {
											la7 = LA(7);
											if (!IdOrKeyword_set0.Contains(la7)) {
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												_type = TT.@checked;
												_value = S.Checked;
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else if (la1 == 'l') {
						la2 = LA(2);
						if (la2 == 'a') {
							la3 = LA(3);
							if (la3 == 's') {
								la4 = LA(4);
								if (la4 == 's') {
									la5 = LA(5);
									if (!IdOrKeyword_set0.Contains(la5)) {
										Skip();
										Skip();
										Skip();
										Skip();
										Skip();
										_type = TT.@class;
										_value = S.Class;
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else if (la1 == 'o') {
						la2 = LA(2);
						if (la2 == 'n') {
							la3 = LA(3);
							if (la3 == 's') {
								la4 = LA(4);
								if (la4 == 't') {
									la5 = LA(5);
									if (!IdOrKeyword_set0.Contains(la5)) {
										Skip();
										Skip();
										Skip();
										Skip();
										Skip();
										_type = TT.AttrKeyword;
										_value = S.Const;
									} else
										Id();
								} else
									Id();
							} else if (la3 == 't') {
								la4 = LA(4);
								if (la4 == 'i') {
									la5 = LA(5);
									if (la5 == 'n') {
										la6 = LA(6);
										if (la6 == 'u') {
											la7 = LA(7);
											if (la7 == 'e') {
												la8 = LA(8);
												if (!IdOrKeyword_set0.Contains(la8)) {
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													_type = TT.@continue;
													_value = S.Continue;
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else
						Id();
				}
				break;
			case 'd':
				{
					la1 = LA(1);
					if (la1 == 'e') {
						la2 = LA(2);
						if (la2 == 'c') {
							la3 = LA(3);
							if (la3 == 'i') {
								la4 = LA(4);
								if (la4 == 'm') {
									la5 = LA(5);
									if (la5 == 'a') {
										la6 = LA(6);
										if (la6 == 'l') {
											la7 = LA(7);
											if (!IdOrKeyword_set0.Contains(la7)) {
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												_type = TT.TypeKeyword;
												_value = S.Decimal;
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else if (la2 == 'f') {
							la3 = LA(3);
							if (la3 == 'a') {
								la4 = LA(4);
								if (la4 == 'u') {
									la5 = LA(5);
									if (la5 == 'l') {
										la6 = LA(6);
										if (la6 == 't') {
											la7 = LA(7);
											if (!IdOrKeyword_set0.Contains(la7)) {
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												_type = TT.@default;
												_value = S.Default;
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else if (la2 == 'l') {
							la3 = LA(3);
							if (la3 == 'e') {
								la4 = LA(4);
								if (la4 == 'g') {
									la5 = LA(5);
									if (la5 == 'a') {
										la6 = LA(6);
										if (la6 == 't') {
											la7 = LA(7);
											if (la7 == 'e') {
												la8 = LA(8);
												if (!IdOrKeyword_set0.Contains(la8)) {
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													_type = TT.@delegate;
													_value = S.Delegate;
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else if (la2 == 's') {
							la3 = LA(3);
							if (la3 == 'c') {
								la4 = LA(4);
								if (la4 == 'e') {
									la5 = LA(5);
									if (la5 == 'n') {
										la6 = LA(6);
										if (la6 == 'd') {
											la7 = LA(7);
											if (la7 == 'i') {
												la8 = LA(8);
												if (la8 == 'n') {
													la9 = LA(9);
													if (la9 == 'g') {
														la10 = LA(10);
														if (!IdOrKeyword_set0.Contains(la10)) {
															Skip();
															Skip();
															Skip();
															Skip();
															Skip();
															Skip();
															Skip();
															Skip();
															Skip();
															Skip();
															OtherContextualKeyword();
														} else
															Id();
													} else
														Id();
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else if (la1 == 'o') {
						la2 = LA(2);
						if (la2 == 'u') {
							la3 = LA(3);
							if (la3 == 'b') {
								la4 = LA(4);
								if (la4 == 'l') {
									la5 = LA(5);
									if (la5 == 'e') {
										la6 = LA(6);
										if (!IdOrKeyword_set0.Contains(la6)) {
											Skip();
											Skip();
											Skip();
											Skip();
											Skip();
											Skip();
											_type = TT.TypeKeyword;
											_value = S.Double;
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else if (!IdOrKeyword_set0.Contains(la2)) {
							Skip();
							Skip();
							_type = TT.@do;
							_value = S.Do;
						} else
							Id();
					} else if (la1 == 'y') {
						la2 = LA(2);
						if (la2 == 'n') {
							la3 = LA(3);
							if (la3 == 'a') {
								la4 = LA(4);
								if (la4 == 'm') {
									la5 = LA(5);
									if (la5 == 'i') {
										la6 = LA(6);
										if (la6 == 'c') {
											la7 = LA(7);
											if (!IdOrKeyword_set0.Contains(la7)) {
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												_type = TT.ContextualKeyword;
												_value = _dynamic;
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else
						Id();
				}
				break;
			case 'e':
				{
					switch (LA(1)) {
					case 'l':
						{
							la2 = LA(2);
							if (la2 == 's') {
								la3 = LA(3);
								if (la3 == 'e') {
									la4 = LA(4);
									if (!IdOrKeyword_set0.Contains(la4)) {
										Skip();
										Skip();
										Skip();
										Skip();
										_type = TT.@else;
										_value = S.Else;
									} else
										Id();
								} else
									Id();
							} else
								Id();
						}
						break;
					case 'n':
						{
							la2 = LA(2);
							if (la2 == 'u') {
								la3 = LA(3);
								if (la3 == 'm') {
									la4 = LA(4);
									if (!IdOrKeyword_set0.Contains(la4)) {
										Skip();
										Skip();
										Skip();
										Skip();
										_type = TT.@enum;
										_value = S.Enum;
									} else
										Id();
								} else
									Id();
							} else
								Id();
						}
						break;
					case 'v':
						{
							la2 = LA(2);
							if (la2 == 'e') {
								la3 = LA(3);
								if (la3 == 'n') {
									la4 = LA(4);
									if (la4 == 't') {
										la5 = LA(5);
										if (!IdOrKeyword_set0.Contains(la5)) {
											Skip();
											Skip();
											Skip();
											Skip();
											Skip();
											_type = TT.@event;
											_value = S.Event;
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						}
						break;
					case 'x':
						{
							la2 = LA(2);
							if (la2 == 'p') {
								la3 = LA(3);
								if (la3 == 'l') {
									la4 = LA(4);
									if (la4 == 'i') {
										la5 = LA(5);
										if (la5 == 'c') {
											la6 = LA(6);
											if (la6 == 'i') {
												la7 = LA(7);
												if (la7 == 't') {
													la8 = LA(8);
													if (!IdOrKeyword_set0.Contains(la8)) {
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														_type = TT.AttrKeyword;
														_value = S.Explicit;
													} else
														Id();
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else if (la2 == 't') {
								la3 = LA(3);
								if (la3 == 'e') {
									la4 = LA(4);
									if (la4 == 'r') {
										la5 = LA(5);
										if (la5 == 'n') {
											la6 = LA(6);
											if (!IdOrKeyword_set0.Contains(la6)) {
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												_type = TT.AttrKeyword;
												_value = S.Extern;
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						}
						break;
					case 'q':
						{
							la2 = LA(2);
							if (la2 == 'u') {
								la3 = LA(3);
								if (la3 == 'a') {
									la4 = LA(4);
									if (la4 == 'l') {
										la5 = LA(5);
										if (la5 == 's') {
											la6 = LA(6);
											if (!IdOrKeyword_set0.Contains(la6)) {
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												OtherContextualKeyword();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						}
						break;
					default:
						Id();
						break;
					}
				}
				break;
			case 'f':
				{
					switch (LA(1)) {
					case 'a':
						{
							la2 = LA(2);
							if (la2 == 'l') {
								la3 = LA(3);
								if (la3 == 's') {
									la4 = LA(4);
									if (la4 == 'e') {
										la5 = LA(5);
										if (!IdOrKeyword_set0.Contains(la5)) {
											Skip();
											Skip();
											Skip();
											Skip();
											Skip();
											_type = TT.OtherLit;
											_value = G.BoxedFalse;
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						}
						break;
					case 'i':
						{
							la2 = LA(2);
							if (la2 == 'n') {
								la3 = LA(3);
								if (la3 == 'a') {
									la4 = LA(4);
									if (la4 == 'l') {
										la5 = LA(5);
										if (la5 == 'l') {
											la6 = LA(6);
											if (la6 == 'y') {
												la7 = LA(7);
												if (!IdOrKeyword_set0.Contains(la7)) {
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													_type = TT.@finally;
													_value = S.Finally;
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else if (la2 == 'x') {
								la3 = LA(3);
								if (la3 == 'e') {
									la4 = LA(4);
									if (la4 == 'd') {
										la5 = LA(5);
										if (!IdOrKeyword_set0.Contains(la5)) {
											Skip();
											Skip();
											Skip();
											Skip();
											Skip();
											_type = TT.@fixed;
											_value = S.Fixed;
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						}
						break;
					case 'l':
						{
							la2 = LA(2);
							if (la2 == 'o') {
								la3 = LA(3);
								if (la3 == 'a') {
									la4 = LA(4);
									if (la4 == 't') {
										la5 = LA(5);
										if (!IdOrKeyword_set0.Contains(la5)) {
											Skip();
											Skip();
											Skip();
											Skip();
											Skip();
											_type = TT.TypeKeyword;
											_value = S.Single;
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						}
						break;
					case 'o':
						{
							la2 = LA(2);
							if (la2 == 'r') {
								la3 = LA(3);
								if (la3 == 'e') {
									la4 = LA(4);
									if (la4 == 'a') {
										la5 = LA(5);
										if (la5 == 'c') {
											la6 = LA(6);
											if (la6 == 'h') {
												la7 = LA(7);
												if (!IdOrKeyword_set0.Contains(la7)) {
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													_type = TT.@foreach;
													_value = S.ForEach;
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else if (!IdOrKeyword_set0.Contains(la3)) {
									Skip();
									Skip();
									Skip();
									_type = TT.@for;
									_value = S.For;
								} else
									Id();
							} else
								Id();
						}
						break;
					case 'r':
						{
							la2 = LA(2);
							if (la2 == 'o') {
								la3 = LA(3);
								if (la3 == 'm') {
									la4 = LA(4);
									if (!IdOrKeyword_set0.Contains(la4)) {
										Skip();
										Skip();
										Skip();
										Skip();
										_type = TT.ContextualKeyword;
										_value = _from;
									} else
										Id();
								} else
									Id();
							} else
								Id();
						}
						break;
					default:
						Id();
						break;
					}
				}
				break;
			case 'g':
				{
					la1 = LA(1);
					if (la1 == 'o') {
						la2 = LA(2);
						if (la2 == 't') {
							la3 = LA(3);
							if (la3 == 'o') {
								la4 = LA(4);
								if (!IdOrKeyword_set0.Contains(la4)) {
									Skip();
									Skip();
									Skip();
									Skip();
									_type = TT.@goto;
									_value = S.Goto;
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else if (la1 == 'r') {
						la2 = LA(2);
						if (la2 == 'o') {
							la3 = LA(3);
							if (la3 == 'u') {
								la4 = LA(4);
								if (la4 == 'p') {
									la5 = LA(5);
									if (!IdOrKeyword_set0.Contains(la5)) {
										Skip();
										Skip();
										Skip();
										Skip();
										Skip();
										OtherContextualKeyword();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else
						Id();
				}
				break;
			case 'i':
				{
					la1 = LA(1);
					if (la1 == 'f') {
						la2 = LA(2);
						if (!IdOrKeyword_set0.Contains(la2)) {
							Skip();
							Skip();
							_type = TT.@if;
							_value = S.If;
						} else
							Id();
					} else if (la1 == 'm') {
						la2 = LA(2);
						if (la2 == 'p') {
							la3 = LA(3);
							if (la3 == 'l') {
								la4 = LA(4);
								if (la4 == 'i') {
									la5 = LA(5);
									if (la5 == 'c') {
										la6 = LA(6);
										if (la6 == 'i') {
											la7 = LA(7);
											if (la7 == 't') {
												la8 = LA(8);
												if (!IdOrKeyword_set0.Contains(la8)) {
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													_type = TT.AttrKeyword;
													_value = S.Implicit;
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else if (la1 == 'n') {
						la2 = LA(2);
						if (la2 == 't') {
							la3 = LA(3);
							if (la3 == 'e') {
								la4 = LA(4);
								if (la4 == 'r') {
									la5 = LA(5);
									if (la5 == 'f') {
										la6 = LA(6);
										if (la6 == 'a') {
											la7 = LA(7);
											if (la7 == 'c') {
												la8 = LA(8);
												if (la8 == 'e') {
													la9 = LA(9);
													if (!IdOrKeyword_set0.Contains(la9)) {
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														_type = TT.@interface;
														_value = S.Interface;
													} else
														Id();
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else if (la5 == 'n') {
										la6 = LA(6);
										if (la6 == 'a') {
											la7 = LA(7);
											if (la7 == 'l') {
												la8 = LA(8);
												if (!IdOrKeyword_set0.Contains(la8)) {
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													_type = TT.AttrKeyword;
													_value = S.Internal;
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else if (!IdOrKeyword_set0.Contains(la3)) {
								Skip();
								Skip();
								Skip();
								_type = TT.TypeKeyword;
								_value = S.Int32;
							} else if (la3 == 'o') {
								la4 = LA(4);
								if (!IdOrKeyword_set0.Contains(la4)) {
									Skip();
									Skip();
									Skip();
									Skip();
									OtherContextualKeyword();
								} else
									Id();
							} else
								Id();
						} else if (!IdOrKeyword_set0.Contains(la2)) {
							Skip();
							Skip();
							_type = TT.@in;
							_value = S.In;
						} else
							Id();
					} else if (la1 == 's') {
						la2 = LA(2);
						if (!IdOrKeyword_set0.Contains(la2)) {
							Skip();
							Skip();
							_type = TT.@is;
							_value = S.Is;
						} else
							Id();
					} else
						Id();
				}
				break;
			case 'l':
				{
					la1 = LA(1);
					if (la1 == 'o') {
						la2 = LA(2);
						if (la2 == 'c') {
							la3 = LA(3);
							if (la3 == 'k') {
								la4 = LA(4);
								if (!IdOrKeyword_set0.Contains(la4)) {
									Skip();
									Skip();
									Skip();
									Skip();
									_type = TT.@lock;
									_value = S.Lock;
								} else
									Id();
							} else
								Id();
						} else if (la2 == 'n') {
							la3 = LA(3);
							if (la3 == 'g') {
								la4 = LA(4);
								if (!IdOrKeyword_set0.Contains(la4)) {
									Skip();
									Skip();
									Skip();
									Skip();
									_type = TT.TypeKeyword;
									_value = S.Int64;
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else if (la1 == 'e') {
						la2 = LA(2);
						if (la2 == 't') {
							la3 = LA(3);
							if (!IdOrKeyword_set0.Contains(la3)) {
								Skip();
								Skip();
								Skip();
								OtherContextualKeyword();
							} else
								Id();
						} else
							Id();
					} else
						Id();
				}
				break;
			case 'n':
				{
					la1 = LA(1);
					if (la1 == 'a') {
						la2 = LA(2);
						if (la2 == 'm') {
							la3 = LA(3);
							if (la3 == 'e') {
								la4 = LA(4);
								if (la4 == 's') {
									la5 = LA(5);
									if (la5 == 'p') {
										la6 = LA(6);
										if (la6 == 'a') {
											la7 = LA(7);
											if (la7 == 'c') {
												la8 = LA(8);
												if (la8 == 'e') {
													la9 = LA(9);
													if (!IdOrKeyword_set0.Contains(la9)) {
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														_type = TT.@namespace;
														_value = S.Namespace;
													} else
														Id();
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else if (la1 == 'e') {
						la2 = LA(2);
						if (la2 == 'w') {
							la3 = LA(3);
							if (!IdOrKeyword_set0.Contains(la3)) {
								Skip();
								Skip();
								Skip();
								_type = TT.@new;
								_value = S.New;
							} else
								Id();
						} else
							Id();
					} else if (la1 == 'u') {
						la2 = LA(2);
						if (la2 == 'l') {
							la3 = LA(3);
							if (la3 == 'l') {
								la4 = LA(4);
								if (!IdOrKeyword_set0.Contains(la4)) {
									Skip();
									Skip();
									Skip();
									Skip();
									_type = TT.OtherLit;
									_value = null;
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else
						Id();
				}
				break;
			case 'o':
				{
					switch (LA(1)) {
					case 'b':
						{
							la2 = LA(2);
							if (la2 == 'j') {
								la3 = LA(3);
								if (la3 == 'e') {
									la4 = LA(4);
									if (la4 == 'c') {
										la5 = LA(5);
										if (la5 == 't') {
											la6 = LA(6);
											if (!IdOrKeyword_set0.Contains(la6)) {
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												_type = TT.TypeKeyword;
												_value = S.Object;
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						}
						break;
					case 'p':
						{
							la2 = LA(2);
							if (la2 == 'e') {
								la3 = LA(3);
								if (la3 == 'r') {
									la4 = LA(4);
									if (la4 == 'a') {
										la5 = LA(5);
										if (la5 == 't') {
											la6 = LA(6);
											if (la6 == 'o') {
												la7 = LA(7);
												if (la7 == 'r') {
													la8 = LA(8);
													if (!IdOrKeyword_set0.Contains(la8)) {
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														_type = TT.@operator;
														_value = S.Operator;
													} else
														Id();
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						}
						break;
					case 'u':
						{
							la2 = LA(2);
							if (la2 == 't') {
								la3 = LA(3);
								if (!IdOrKeyword_set0.Contains(la3)) {
									Skip();
									Skip();
									Skip();
									_type = TT.AttrKeyword;
									_value = S.Out;
								} else
									Id();
							} else
								Id();
						}
						break;
					case 'v':
						{
							la2 = LA(2);
							if (la2 == 'e') {
								la3 = LA(3);
								if (la3 == 'r') {
									la4 = LA(4);
									if (la4 == 'r') {
										la5 = LA(5);
										if (la5 == 'i') {
											la6 = LA(6);
											if (la6 == 'd') {
												la7 = LA(7);
												if (la7 == 'e') {
													la8 = LA(8);
													if (!IdOrKeyword_set0.Contains(la8)) {
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														_type = TT.AttrKeyword;
														_value = S.Override;
													} else
														Id();
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						}
						break;
					case 'n':
						{
							la2 = LA(2);
							if (!IdOrKeyword_set0.Contains(la2)) {
								Skip();
								Skip();
								OtherContextualKeyword();
							} else
								Id();
						}
						break;
					case 'r':
						{
							la2 = LA(2);
							if (la2 == 'd') {
								la3 = LA(3);
								if (la3 == 'e') {
									la4 = LA(4);
									if (la4 == 'r') {
										la5 = LA(5);
										if (la5 == 'b') {
											la6 = LA(6);
											if (la6 == 'y') {
												la7 = LA(7);
												if (!IdOrKeyword_set0.Contains(la7)) {
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													OtherContextualKeyword();
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						}
						break;
					default:
						Id();
						break;
					}
				}
				break;
			case 'p':
				{
					la1 = LA(1);
					if (la1 == 'a') {
						la2 = LA(2);
						if (la2 == 'r') {
							la3 = LA(3);
							if (la3 == 'a') {
								la4 = LA(4);
								if (la4 == 'm') {
									la5 = LA(5);
									if (la5 == 's') {
										la6 = LA(6);
										if (!IdOrKeyword_set0.Contains(la6)) {
											Skip();
											Skip();
											Skip();
											Skip();
											Skip();
											Skip();
											_type = TT.AttrKeyword;
											_value = S.Params;
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else if (la1 == 'r') {
						la2 = LA(2);
						if (la2 == 'i') {
							la3 = LA(3);
							if (la3 == 'v') {
								la4 = LA(4);
								if (la4 == 'a') {
									la5 = LA(5);
									if (la5 == 't') {
										la6 = LA(6);
										if (la6 == 'e') {
											la7 = LA(7);
											if (!IdOrKeyword_set0.Contains(la7)) {
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												_type = TT.AttrKeyword;
												_value = S.Private;
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else if (la2 == 'o') {
							la3 = LA(3);
							if (la3 == 't') {
								la4 = LA(4);
								if (la4 == 'e') {
									la5 = LA(5);
									if (la5 == 'c') {
										la6 = LA(6);
										if (la6 == 't') {
											la7 = LA(7);
											if (la7 == 'e') {
												la8 = LA(8);
												if (la8 == 'd') {
													la9 = LA(9);
													if (!IdOrKeyword_set0.Contains(la9)) {
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														_type = TT.AttrKeyword;
														_value = S.Protected;
													} else
														Id();
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else if (la1 == 'u') {
						la2 = LA(2);
						if (la2 == 'b') {
							la3 = LA(3);
							if (la3 == 'l') {
								la4 = LA(4);
								if (la4 == 'i') {
									la5 = LA(5);
									if (la5 == 'c') {
										la6 = LA(6);
										if (!IdOrKeyword_set0.Contains(la6)) {
											Skip();
											Skip();
											Skip();
											Skip();
											Skip();
											Skip();
											_type = TT.AttrKeyword;
											_value = S.Public;
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else
						Id();
				}
				break;
			case 'r':
				{
					la1 = LA(1);
					if (la1 == 'e') {
						la2 = LA(2);
						if (la2 == 'a') {
							la3 = LA(3);
							if (la3 == 'd') {
								la4 = LA(4);
								if (la4 == 'o') {
									la5 = LA(5);
									if (la5 == 'n') {
										la6 = LA(6);
										if (la6 == 'l') {
											la7 = LA(7);
											if (la7 == 'y') {
												la8 = LA(8);
												if (!IdOrKeyword_set0.Contains(la8)) {
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													_type = TT.AttrKeyword;
													_value = S.Readonly;
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else if (la2 == 'f') {
							la3 = LA(3);
							if (!IdOrKeyword_set0.Contains(la3)) {
								Skip();
								Skip();
								Skip();
								_type = TT.AttrKeyword;
								_value = S.Ref;
							} else
								Id();
						} else if (la2 == 't') {
							la3 = LA(3);
							if (la3 == 'u') {
								la4 = LA(4);
								if (la4 == 'r') {
									la5 = LA(5);
									if (la5 == 'n') {
										la6 = LA(6);
										if (!IdOrKeyword_set0.Contains(la6)) {
											Skip();
											Skip();
											Skip();
											Skip();
											Skip();
											Skip();
											_type = TT.@return;
											_value = S.Return;
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else
						Id();
				}
				break;
			case 's':
				{
					switch (LA(1)) {
					case 'b':
						{
							la2 = LA(2);
							if (la2 == 'y') {
								la3 = LA(3);
								if (la3 == 't') {
									la4 = LA(4);
									if (la4 == 'e') {
										la5 = LA(5);
										if (!IdOrKeyword_set0.Contains(la5)) {
											Skip();
											Skip();
											Skip();
											Skip();
											Skip();
											_type = TT.TypeKeyword;
											_value = S.Int8;
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						}
						break;
					case 'e':
						{
							la2 = LA(2);
							if (la2 == 'a') {
								la3 = LA(3);
								if (la3 == 'l') {
									la4 = LA(4);
									if (la4 == 'e') {
										la5 = LA(5);
										if (la5 == 'd') {
											la6 = LA(6);
											if (!IdOrKeyword_set0.Contains(la6)) {
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												_type = TT.AttrKeyword;
												_value = S.Sealed;
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else if (la2 == 'l') {
								la3 = LA(3);
								if (la3 == 'e') {
									la4 = LA(4);
									if (la4 == 'c') {
										la5 = LA(5);
										if (la5 == 't') {
											la6 = LA(6);
											if (!IdOrKeyword_set0.Contains(la6)) {
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												_type = TT.ContextualKeyword;
												_value = _select;
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						}
						break;
					case 'h':
						{
							la2 = LA(2);
							if (la2 == 'o') {
								la3 = LA(3);
								if (la3 == 'r') {
									la4 = LA(4);
									if (la4 == 't') {
										la5 = LA(5);
										if (!IdOrKeyword_set0.Contains(la5)) {
											Skip();
											Skip();
											Skip();
											Skip();
											Skip();
											_type = TT.TypeKeyword;
											_value = S.Int16;
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						}
						break;
					case 'i':
						{
							la2 = LA(2);
							if (la2 == 'z') {
								la3 = LA(3);
								if (la3 == 'e') {
									la4 = LA(4);
									if (la4 == 'o') {
										la5 = LA(5);
										if (la5 == 'f') {
											la6 = LA(6);
											if (!IdOrKeyword_set0.Contains(la6)) {
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												_type = TT.@sizeof;
												_value = S.Sizeof;
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						}
						break;
					case 't':
						{
							la2 = LA(2);
							if (la2 == 'a') {
								la3 = LA(3);
								if (la3 == 'c') {
									la4 = LA(4);
									if (la4 == 'k') {
										la5 = LA(5);
										if (la5 == 'a') {
											la6 = LA(6);
											if (la6 == 'l') {
												la7 = LA(7);
												if (la7 == 'l') {
													la8 = LA(8);
													if (la8 == 'o') {
														la9 = LA(9);
														if (la9 == 'c') {
															la10 = LA(10);
															if (!IdOrKeyword_set0.Contains(la10)) {
																Skip();
																Skip();
																Skip();
																Skip();
																Skip();
																Skip();
																Skip();
																Skip();
																Skip();
																Skip();
																_type = TT.@stackalloc;
																_value = S.StackAlloc;
															} else
																Id();
														} else
															Id();
													} else
														Id();
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else if (la3 == 't') {
									la4 = LA(4);
									if (la4 == 'i') {
										la5 = LA(5);
										if (la5 == 'c') {
											la6 = LA(6);
											if (!IdOrKeyword_set0.Contains(la6)) {
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												_type = TT.AttrKeyword;
												_value = S.Static;
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else if (la2 == 'r') {
								la3 = LA(3);
								if (la3 == 'i') {
									la4 = LA(4);
									if (la4 == 'n') {
										la5 = LA(5);
										if (la5 == 'g') {
											la6 = LA(6);
											if (!IdOrKeyword_set0.Contains(la6)) {
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												_type = TT.TypeKeyword;
												_value = S.String;
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else if (la3 == 'u') {
									la4 = LA(4);
									if (la4 == 'c') {
										la5 = LA(5);
										if (la5 == 't') {
											la6 = LA(6);
											if (!IdOrKeyword_set0.Contains(la6)) {
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												_type = TT.@struct;
												_value = S.Struct;
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						}
						break;
					case 'w':
						{
							la2 = LA(2);
							if (la2 == 'i') {
								la3 = LA(3);
								if (la3 == 't') {
									la4 = LA(4);
									if (la4 == 'c') {
										la5 = LA(5);
										if (la5 == 'h') {
											la6 = LA(6);
											if (!IdOrKeyword_set0.Contains(la6)) {
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												_type = TT.@switch;
												_value = S.Switch;
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						}
						break;
					default:
						Id();
						break;
					}
				}
				break;
			case 't':
				{
					la1 = LA(1);
					if (la1 == 'h') {
						la2 = LA(2);
						if (la2 == 'i') {
							la3 = LA(3);
							if (la3 == 's') {
								la4 = LA(4);
								if (!IdOrKeyword_set0.Contains(la4)) {
									Skip();
									Skip();
									Skip();
									Skip();
									_type = TT.@this;
									_value = S.This;
								} else
									Id();
							} else
								Id();
						} else if (la2 == 'r') {
							la3 = LA(3);
							if (la3 == 'o') {
								la4 = LA(4);
								if (la4 == 'w') {
									la5 = LA(5);
									if (!IdOrKeyword_set0.Contains(la5)) {
										Skip();
										Skip();
										Skip();
										Skip();
										Skip();
										_type = TT.@throw;
										_value = S.Throw;
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else if (la1 == 'r') {
						la2 = LA(2);
						if (la2 == 'u') {
							la3 = LA(3);
							if (la3 == 'e') {
								la4 = LA(4);
								if (!IdOrKeyword_set0.Contains(la4)) {
									Skip();
									Skip();
									Skip();
									Skip();
									_type = TT.OtherLit;
									_value = G.BoxedTrue;
								} else
									Id();
							} else
								Id();
						} else if (la2 == 'y') {
							la3 = LA(3);
							if (!IdOrKeyword_set0.Contains(la3)) {
								Skip();
								Skip();
								Skip();
								_type = TT.@try;
								_value = S.Try;
							} else
								Id();
						} else if (la2 == 'a') {
							la3 = LA(3);
							if (la3 == 'i') {
								la4 = LA(4);
								if (la4 == 't') {
									la5 = LA(5);
									if (!IdOrKeyword_set0.Contains(la5)) {
										Skip();
										Skip();
										Skip();
										Skip();
										Skip();
										_type = TT.ContextualKeyword;
										_value = _trait;
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else if (la1 == 'y') {
						la2 = LA(2);
						if (la2 == 'p') {
							la3 = LA(3);
							if (la3 == 'e') {
								la4 = LA(4);
								if (la4 == 'o') {
									la5 = LA(5);
									if (la5 == 'f') {
										la6 = LA(6);
										if (!IdOrKeyword_set0.Contains(la6)) {
											Skip();
											Skip();
											Skip();
											Skip();
											Skip();
											Skip();
											_type = TT.@typeof;
											_value = S.Typeof;
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else
						Id();
				}
				break;
			case 'u':
				{
					la1 = LA(1);
					if (la1 == 'i') {
						la2 = LA(2);
						if (la2 == 'n') {
							la3 = LA(3);
							if (la3 == 't') {
								la4 = LA(4);
								if (!IdOrKeyword_set0.Contains(la4)) {
									Skip();
									Skip();
									Skip();
									Skip();
									_type = TT.TypeKeyword;
									_value = S.UInt32;
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else if (la1 == 'l') {
						la2 = LA(2);
						if (la2 == 'o') {
							la3 = LA(3);
							if (la3 == 'n') {
								la4 = LA(4);
								if (la4 == 'g') {
									la5 = LA(5);
									if (!IdOrKeyword_set0.Contains(la5)) {
										Skip();
										Skip();
										Skip();
										Skip();
										Skip();
										_type = TT.TypeKeyword;
										_value = S.UInt64;
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else if (la1 == 'n') {
						la2 = LA(2);
						if (la2 == 'c') {
							la3 = LA(3);
							if (la3 == 'h') {
								la4 = LA(4);
								if (la4 == 'e') {
									la5 = LA(5);
									if (la5 == 'c') {
										la6 = LA(6);
										if (la6 == 'k') {
											la7 = LA(7);
											if (la7 == 'e') {
												la8 = LA(8);
												if (la8 == 'd') {
													la9 = LA(9);
													if (!IdOrKeyword_set0.Contains(la9)) {
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														_type = TT.@unchecked;
														_value = S.Unchecked;
													} else
														Id();
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else if (la2 == 's') {
							la3 = LA(3);
							if (la3 == 'a') {
								la4 = LA(4);
								if (la4 == 'f') {
									la5 = LA(5);
									if (la5 == 'e') {
										la6 = LA(6);
										if (!IdOrKeyword_set0.Contains(la6)) {
											Skip();
											Skip();
											Skip();
											Skip();
											Skip();
											Skip();
											_type = TT.AttrKeyword;
											_value = S.Unsafe;
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else if (la1 == 's') {
						la2 = LA(2);
						if (la2 == 'h') {
							la3 = LA(3);
							if (la3 == 'o') {
								la4 = LA(4);
								if (la4 == 'r') {
									la5 = LA(5);
									if (la5 == 't') {
										la6 = LA(6);
										if (!IdOrKeyword_set0.Contains(la6)) {
											Skip();
											Skip();
											Skip();
											Skip();
											Skip();
											Skip();
											_type = TT.TypeKeyword;
											_value = S.UInt16;
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else if (la2 == 'i') {
							la3 = LA(3);
							if (la3 == 'n') {
								la4 = LA(4);
								if (la4 == 'g') {
									la5 = LA(5);
									if (!IdOrKeyword_set0.Contains(la5)) {
										Skip();
										Skip();
										Skip();
										Skip();
										Skip();
										_type = TT.@using;
										_value = S.UsingStmt;
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else
						Id();
				}
				break;
			case 'v':
				{
					la1 = LA(1);
					if (la1 == 'i') {
						la2 = LA(2);
						if (la2 == 'r') {
							la3 = LA(3);
							if (la3 == 't') {
								la4 = LA(4);
								if (la4 == 'u') {
									la5 = LA(5);
									if (la5 == 'a') {
										la6 = LA(6);
										if (la6 == 'l') {
											la7 = LA(7);
											if (!IdOrKeyword_set0.Contains(la7)) {
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												_type = TT.AttrKeyword;
												_value = S.Virtual;
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else if (la1 == 'o') {
						la2 = LA(2);
						if (la2 == 'l') {
							la3 = LA(3);
							if (la3 == 'a') {
								la4 = LA(4);
								if (la4 == 't') {
									la5 = LA(5);
									if (la5 == 'i') {
										la6 = LA(6);
										if (la6 == 'l') {
											la7 = LA(7);
											if (la7 == 'e') {
												la8 = LA(8);
												if (!IdOrKeyword_set0.Contains(la8)) {
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													_type = TT.AttrKeyword;
													_value = S.Volatile;
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else if (la2 == 'i') {
							la3 = LA(3);
							if (la3 == 'd') {
								la4 = LA(4);
								if (!IdOrKeyword_set0.Contains(la4)) {
									Skip();
									Skip();
									Skip();
									Skip();
									_type = TT.TypeKeyword;
									_value = S.Void;
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else if (la1 == 'a') {
						la2 = LA(2);
						if (la2 == 'r') {
							la3 = LA(3);
							if (!IdOrKeyword_set0.Contains(la3)) {
								Skip();
								Skip();
								Skip();
								_type = TT.ContextualKeyword;
								_value = _var;
							} else
								Id();
						} else
							Id();
					} else
						Id();
				}
				break;
			case 'w':
				{
					la1 = LA(1);
					if (la1 == 'h') {
						la2 = LA(2);
						if (la2 == 'i') {
							la3 = LA(3);
							if (la3 == 'l') {
								la4 = LA(4);
								if (la4 == 'e') {
									la5 = LA(5);
									if (!IdOrKeyword_set0.Contains(la5)) {
										Skip();
										Skip();
										Skip();
										Skip();
										Skip();
										_type = TT.@while;
										_value = S.While;
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else if (la2 == 'e') {
							la3 = LA(3);
							if (la3 == 'r') {
								la4 = LA(4);
								if (la4 == 'e') {
									la5 = LA(5);
									if (!IdOrKeyword_set0.Contains(la5)) {
										Skip();
										Skip();
										Skip();
										Skip();
										Skip();
										_type = TT.ContextualKeyword;
										_value = _where;
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else
						Id();
				}
				break;
			case '#':
				{
					if (AllowPP) {
						switch (LA(1)) {
						case 'i':
							{
								la2 = LA(2);
								if (la2 == 'f') {
									la3 = LA(3);
									if (!IdOrKeyword_set0.Contains(la3)) {
										Skip();
										Skip();
										Skip();
										_type = TT.PPif;
										_value = S.PPIf;
									} else
										Id();
								} else
									Id();
							}
							break;
						case 'e':
							{
								la2 = LA(2);
								if (la2 == 'l') {
									la3 = LA(3);
									if (la3 == 's') {
										la4 = LA(4);
										if (la4 == 'e') {
											la5 = LA(5);
											if (!IdOrKeyword_set0.Contains(la5)) {
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												_type = TT.PPelse;
												_value = S.PPElse;
											} else
												Id();
										} else
											Id();
									} else if (la3 == 'i') {
										la4 = LA(4);
										if (la4 == 'f') {
											la5 = LA(5);
											if (!IdOrKeyword_set0.Contains(la5)) {
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												_type = TT.PPelif;
												_value = S.PPElIf;
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else if (la2 == 'n') {
									la3 = LA(3);
									if (la3 == 'd') {
										la4 = LA(4);
										if (la4 == 'i') {
											la5 = LA(5);
											if (la5 == 'f') {
												la6 = LA(6);
												if (!IdOrKeyword_set0.Contains(la6)) {
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													_type = TT.PPendif;
													_value = S.PPEndIf;
												} else
													Id();
											} else
												Id();
										} else if (la4 == 'r') {
											la5 = LA(5);
											if (la5 == 'e') {
												la6 = LA(6);
												if (la6 == 'g') {
													la7 = LA(7);
													if (la7 == 'i') {
														la8 = LA(8);
														if (la8 == 'o') {
															la9 = LA(9);
															if (la9 == 'n') {
																la10 = LA(10);
																if (!IdOrKeyword_set0.Contains(la10)) {
																	Skip();
																	Skip();
																	Skip();
																	Skip();
																	Skip();
																	Skip();
																	Skip();
																	Skip();
																	Skip();
																	Skip();
																	_type = TT.PPendregion;
																	_value = S.PPEndRegion;
																} else
																	Id();
															} else
																Id();
														} else
															Id();
													} else
														Id();
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else if (la2 == 'r') {
									la3 = LA(3);
									if (la3 == 'r') {
										la4 = LA(4);
										if (la4 == 'o') {
											la5 = LA(5);
											if (la5 == 'r') {
												la6 = LA(6);
												if (!IdOrKeyword_set0.Contains(la6)) {
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													_type = TT.PPerror;
													_value = RestOfPPLine();
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							}
							break;
						case 'd':
							{
								la2 = LA(2);
								if (la2 == 'e') {
									la3 = LA(3);
									if (la3 == 'f') {
										la4 = LA(4);
										if (la4 == 'i') {
											la5 = LA(5);
											if (la5 == 'n') {
												la6 = LA(6);
												if (la6 == 'e') {
													la7 = LA(7);
													if (!IdOrKeyword_set0.Contains(la7)) {
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														_type = TT.PPdefine;
														_value = S.PPDefine;
													} else
														Id();
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							}
							break;
						case 'u':
							{
								la2 = LA(2);
								if (la2 == 'n') {
									la3 = LA(3);
									if (la3 == 'd') {
										la4 = LA(4);
										if (la4 == 'e') {
											la5 = LA(5);
											if (la5 == 'f') {
												la6 = LA(6);
												if (!IdOrKeyword_set0.Contains(la6)) {
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													Skip();
													_type = TT.PPundef;
													_value = S.PPUndef;
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							}
							break;
						case 'p':
							{
								la2 = LA(2);
								if (la2 == 'r') {
									la3 = LA(3);
									if (la3 == 'a') {
										la4 = LA(4);
										if (la4 == 'g') {
											la5 = LA(5);
											if (la5 == 'm') {
												la6 = LA(6);
												if (la6 == 'a') {
													la7 = LA(7);
													if (!IdOrKeyword_set0.Contains(la7)) {
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														_type = TT.PPpragma;
														_value = S.PPPragma;
													} else
														Id();
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							}
							break;
						case 'l':
							{
								la2 = LA(2);
								if (la2 == 'i') {
									la3 = LA(3);
									if (la3 == 'n') {
										la4 = LA(4);
										if (la4 == 'e') {
											la5 = LA(5);
											if (!IdOrKeyword_set0.Contains(la5)) {
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												_type = TT.PPline;
												_value = S.PPLine;
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							}
							break;
						case 'w':
							{
								la2 = LA(2);
								if (la2 == 'a') {
									la3 = LA(3);
									if (la3 == 'r') {
										la4 = LA(4);
										if (la4 == 'n') {
											la5 = LA(5);
											if (la5 == 'i') {
												la6 = LA(6);
												if (la6 == 'n') {
													la7 = LA(7);
													if (la7 == 'g') {
														la8 = LA(8);
														if (!IdOrKeyword_set0.Contains(la8)) {
															Skip();
															Skip();
															Skip();
															Skip();
															Skip();
															Skip();
															Skip();
															Skip();
															_type = TT.PPwarning;
															_value = RestOfPPLine();
														} else
															Id();
													} else
														Id();
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							}
							break;
						case 'n':
							{
								la2 = LA(2);
								if (la2 == 'o') {
									la3 = LA(3);
									if (la3 == 't') {
										la4 = LA(4);
										if (la4 == 'e') {
											la5 = LA(5);
											if (!IdOrKeyword_set0.Contains(la5)) {
												Skip();
												Skip();
												Skip();
												Skip();
												Skip();
												_type = TT.PPnote;
												_value = RestOfPPLine();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							}
							break;
						case 'r':
							{
								la2 = LA(2);
								if (la2 == 'e') {
									la3 = LA(3);
									if (la3 == 'g') {
										la4 = LA(4);
										if (la4 == 'i') {
											la5 = LA(5);
											if (la5 == 'o') {
												la6 = LA(6);
												if (la6 == 'n') {
													la7 = LA(7);
													if (!IdOrKeyword_set0.Contains(la7)) {
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														Skip();
														_type = TT.PPregion;
														_value = RestOfPPLine();
													} else
														Id();
												} else
													Id();
											} else
												Id();
										} else
											Id();
									} else
										Id();
								} else
									Id();
							}
							break;
						default:
							Id();
							break;
						}
					} else
						Id();
				}
				break;
			case 'm':
				{
					la1 = LA(1);
					if (la1 == 'o') {
						la2 = LA(2);
						if (la2 == 'd') {
							la3 = LA(3);
							if (la3 == 'u') {
								la4 = LA(4);
								if (la4 == 'l') {
									la5 = LA(5);
									if (la5 == 'e') {
										la6 = LA(6);
										if (!IdOrKeyword_set0.Contains(la6)) {
											Skip();
											Skip();
											Skip();
											Skip();
											Skip();
											Skip();
											_type = TT.ContextualKeyword;
											_value = _module;
										} else
											Id();
									} else
										Id();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else
						Id();
				}
				break;
			case 'j':
				{
					la1 = LA(1);
					if (la1 == 'o') {
						la2 = LA(2);
						if (la2 == 'i') {
							la3 = LA(3);
							if (la3 == 'n') {
								la4 = LA(4);
								if (!IdOrKeyword_set0.Contains(la4)) {
									Skip();
									Skip();
									Skip();
									Skip();
									OtherContextualKeyword();
								} else
									Id();
							} else
								Id();
						} else
							Id();
					} else
						Id();
				}
				break;
			default:
				Id();
				break;
			}
		}
		string RestOfPPLine()
		{
			int la0;
			int start = InputPosition;
			// Line 445: ([^\$\n\r])*
			 for (;;) {
				la0 = LA0;
				if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
					Skip();
				else
					break;
			}
			return CharSource.Slice(start, InputPosition - start).ToString();
		}
		static readonly HashSet<int> Token_set0 = NewSetOfRanges('!', '!', '#', '\'', '*', '+', '-', ':', '<', '?', '\\', '\\', '^', '^', '`', '`', '|', '|', '~', '~');
		void Token()
		{
			int la0, la1, la2;
			// Line 458: ( Newline | (Spaces / DotIndent / Number / SLComment / MLComment / &{InputPosition == 0} Shebang / Id => IdOrKeyword / TQString / SQString / DQString / BQString / Symbol / At / Operator / UTF_BOM) | Comma | Semicolon | [(] | [)] | [[] | [\]] | [{] | [}] )
			 do {
				la0 = LA0;
				switch (la0) {
				case '\n':
				case '\r':
					{
						_type = TT.Newline;
						Newline();
					}
					break;
				case '\t':
				case ' ':
					{
						_type = TT.Spaces;
						Spaces();
					}
					break;
				case '.':
					{
						if (_startPosition == _lineStartAt) {
							la1 = LA(1);
							if (la1 == '\t' || la1 == ' ') {
								_type = TT.Spaces;
								DotIndent();
							} else if (la1 >= '0' && la1 <= '9')
								goto match4;
							else
								Operator();
						} else {
							la1 = LA(1);
							if (la1 >= '0' && la1 <= '9')
								goto match4;
							else
								Operator();
						}
					}
					break;
				case '-':
					{
						la1 = LA(1);
						if (la1 == '0')
							goto match4;
						else if (la1 == '.') {
							la2 = LA(2);
							if (la2 >= '0' && la2 <= '9')
								goto match4;
							else
								Operator();
						} else if (la1 >= '1' && la1 <= '9')
							goto match4;
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
					goto match4;
				case '/':
					{
						la1 = LA(1);
						if (la1 == '/') {
							_type = TT.SLComment;
							SLComment();
						} else if (la1 == '*') {
							la2 = LA(2);
							if (la2 != -1) {
								_type = TT.MLComment;
								MLComment();
							} else
								Operator();
						} else
							Operator();
					}
					break;
				case '#':
					{
						if (InputPosition == 0) {
							la1 = LA(1);
							if (la1 == '!') {
								_type = TT.Shebang;
								Shebang();
							} else
								goto match8;
						} else
							goto match8;
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
					goto match8;
				case 65279:
					{
						if (char.IsLetter((char) LA0))
							goto match8;
						else {
							_type = TT.Spaces;
							UTF_BOM();
						}
					}
					break;
				case '\\':
					{
						la1 = LA(1);
						if (la1 == 'U' || la1 == 'u') {
							la2 = LA(2);
							if (HexDigit_set0.Contains(la2))
								goto match8;
							else
								Operator();
						} else
							Operator();
					}
					break;
				case '@':
					{
						la1 = LA(1);
						switch (la1) {
						case '\\':
							goto match8;
						case '`':
							{
								la2 = LA(2);
								if (!(la2 == -1 || la2 == '\n' || la2 == '\r'))
									goto match8;
								else
									goto match14;
							}
						case '!':
						case '#':
						case '$':
						case '%':
						case '&':
						case '\'':
						case '*':
						case '+':
						case '-':
						case '.':
						case '/':
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
						case ':':
						case '<':
						case '=':
						case '>':
						case '?':
						case '^':
						case '|':
						case '~':
							goto match8;
						case '"':
							{
								la2 = LA(2);
								if (la2 != -1)
									goto match11;
								else
									goto match14;
							}
						case '@':
							{
								la2 = LA(2);
								if (la2 >= 'A' && la2 <= 'Z' || la2 == '_' || la2 >= 'a' && la2 <= 'z')
									goto match13;
								else if (la2 >= 128 && la2 <= 65532) {
									if (char.IsLetter((char) LA0))
										goto match13;
									else
										goto match14;
								} else if (Token_set0.Contains(la2))
									goto match13;
								else
									goto match14;
							}
						default:
							if (la1 >= 'A' && la1 <= 'Z' || la1 == '_' || la1 >= 'a' && la1 <= 'z')
								goto match8;
							else if (la1 >= 128 && la1 <= 65532) {
								if (char.IsLetter((char) LA0))
									goto match8;
								else
									goto match14;
							} else
								goto match14;
						}
					}
				case '"':
					{
						la1 = LA(1);
						if (la1 == '"') {
							la2 = LA(2);
							if (la2 == '"')
								goto match9;
							else
								goto match11;
						} else if (!(la1 == -1 || la1 == '\n' || la1 == '\r'))
							goto match11;
						else
							goto match25;
					}
				case '\'':
					{
						la1 = LA(1);
						if (la1 == '\'') {
							la2 = LA(2);
							if (la2 == '\'')
								goto match9;
							else
								goto match10;
						} else if (!(la1 == -1 || la1 == '\n' || la1 == '\r'))
							goto match10;
						else
							goto match25;
					}
				case '`':
					{
						_type = TT.BQString;
						BQString();
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
					Operator();
					break;
				case ',':
					{
						_type = TT.Comma;
						Comma();
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
						Skip();
					}
					break;
				case ')':
					{
						_type = TT.RParen;
						Skip();
					}
					break;
				case '[':
					{
						_type = TT.LBrack;
						Skip();
					}
					break;
				case ']':
					{
						_type = TT.RBrack;
						Skip();
					}
					break;
				case '{':
					{
						_type = TT.LBrace;
						Skip();
					}
					break;
				case '}':
					{
						_type = TT.RBrace;
						Skip();
					}
					break;
				default:
					if (la0 >= 128 && la0 <= 65278 || la0 >= 65280 && la0 <= 65532)
						goto match8;
					else
						goto match25;
				}
				break;
			match4:
				{
					_type = TT.Number;
					Number();
				}
				break;
			match8:
				{
					_type = TT.Id;
					IdOrKeyword();
				}
				break;
			match9:
				{
					_type = TT.String;
					TQString();
				}
				break;
			match10:
				{
					_type = TT.SQString;
					SQString();
				}
				break;
			match11:
				{
					_type = TT.String;
					DQString();
				}
				break;
			match13:
				{
					_type = TT.Symbol;
					Symbol();
				}
				break;
			match14:
				{
					_type = TT.At;
					At();
				}
				break;
			match25:
				{
					_type = TT.Unknown;
					Error(0, "Unrecognized token");
					MatchExcept();
				}
			} while (false);
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
			// Line 76: ([0-9] / HexDigits [Pp] [+\-0-9])
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
	}
}
