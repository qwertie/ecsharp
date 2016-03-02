// Generated from EcsLexerGrammar.les by LeMP custom tool. LeMP version: 1.5.0.0
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
using Loyc;
using Loyc.Syntax;
using Loyc.Syntax.Lexing;
namespace Loyc.Ecs.Parser
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
		static readonly Symbol _DotDotDot = GSymbol.Get("...");
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
		static readonly Symbol _when = GSymbol.Get("when");
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
			// Line 30: ([\t ])*
			for (;;) {
				la0 = LA0;
				if (la0 == '\t' || la0 == ' ')
					Skip();
				else
					break;
			}
			// Line 30: ([.] [\t ] ([\t ])*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 == '\t' || la1 == ' ') {
						Skip();
						Skip();
						// Line 30: ([\t ])*
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
			#line 32 "EcsLexerGrammar.les"
			_indentLevel = MeasureIndent(_indent = CharSource.Slice(_startPosition, InputPosition - _startPosition));
			_value = WhitespaceTag.Value;
			#line default
		}
		new void Spaces()
		{
			int la0;
			Skip();
			// Line 37: ([\t ])*
			for (;;) {
				la0 = LA0;
				if (la0 == '\t' || la0 == ' ')
					Skip();
				else
					break;
			}
			#line 39 "EcsLexerGrammar.les"
			if ((_allowPPAt == _startPosition)) {
				_allowPPAt = InputPosition;
			}
			#line 42 "EcsLexerGrammar.les"
			if ((_lineStartAt == _startPosition)) {
				_indentLevel = MeasureIndent(_indent = CharSource.Slice(_startPosition, InputPosition - _startPosition));
			}
			#line 46 "EcsLexerGrammar.les"
			_value = WhitespaceTag.Value;
			#line default
		}
		void UTF_BOM()
		{
			Skip();
			#line 51 "EcsLexerGrammar.les"
			if ((_lineStartAt == _startPosition)) {
				_lineStartAt = InputPosition;
			}
			#line 54 "EcsLexerGrammar.les"
			_value = WhitespaceTag.Value;
			#line default
		}
		void SLComment()
		{
			int la0;
			Skip();
			Skip();
			// Line 58: ([^\$\n\r])*
			for (;;) {
				la0 = LA0;
				if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
					Skip();
				else
					break;
			}
			#line 59 "EcsLexerGrammar.les"
			_value = WhitespaceTag.Value;
			#line default
		}
		void MLComment()
		{
			int la1;
			Skip();
			Skip();
			// Line 63: nongreedy( &{AllowNestedComments} MLComment / Newline / [^\$] )*
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
			#line 64 "EcsLexerGrammar.les"
			_value = WhitespaceTag.Value;
			#line default
		}
		void DecDigits()
		{
			int la0, la1;
			MatchRange('0', '9');
			// Line 69: ([0-9])*
			for (;;) {
				la0 = LA0;
				if (la0 >= '0' && la0 <= '9')
					Skip();
				else
					break;
			}
			// Line 69: ([_] [0-9] ([0-9])*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '_') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						Skip();
						Skip();
						// Line 69: ([0-9])*
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
			// Line 71: greedy(HexDigit)*
			for (;;) {
				la0 = LA0;
				if (HexDigit_set0.Contains(la0))
					HexDigit();
				else
					break;
			}
			// Line 71: greedy([_] HexDigit (HexDigit)*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '_') {
					la1 = LA(1);
					if (HexDigit_set0.Contains(la1)) {
						Skip();
						HexDigit();
						// Line 71: (HexDigit)*
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
			// Line 71: greedy(HexDigit)*
			for (;;) {
				la0 = LA0;
				if (HexDigit_set0.Contains(la0))
					{if (!Scan_HexDigit())
						return false;}
				else
					break;
			}
			// Line 71: greedy([_] HexDigit (HexDigit)*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '_') {
					la1 = LA(1);
					if (HexDigit_set0.Contains(la1)) {
						if (!TryMatch('_'))
							return false;
						if (!Scan_HexDigit())
							return false;
						// Line 71: (HexDigit)*
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
			// Line 72: ([01])*
			for (;;) {
				la0 = LA0;
				if (la0 >= '0' && la0 <= '1')
					Skip();
				else
					break;
			}
			// Line 72: ([_] [01] ([01])*)*
			for (;;) {
				la0 = LA0;
				if (la0 == '_') {
					Skip();
					Match('0', '1');
					// Line 72: ([01])*
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
			#line 74 "EcsLexerGrammar.les"
			_numberBase = 10;
			#line default
			// Line 75: ([.] DecDigits | DecDigits ([.] DecDigits)?)
			la0 = LA0;
			if (la0 == '.') {
				Skip();
				DecDigits();
				#line 75 "EcsLexerGrammar.les"
				_isFloat = true;
				#line default
			} else {
				DecDigits();
				// Line 76: ([.] DecDigits)?
				la0 = LA0;
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						#line 76 "EcsLexerGrammar.les"
						_isFloat = true;
						#line default
						Skip();
						DecDigits();
					}
				}
			}
			// Line 78: ([Ee] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'E' || la0 == 'e') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					#line 78 "EcsLexerGrammar.les"
					_isFloat = true;
					#line default
					Skip();
					// Line 78: ([+\-])?
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
			#line 81 "EcsLexerGrammar.les"
			_numberBase = 16;
			#line default
			// Line 82: (HexDigits)?
			la0 = LA0;
			if (HexDigit_set0.Contains(la0))
				HexDigits();
			// Line 84: ([.] &(([0-9] / HexDigits [Pp] [+\-0-9])) HexDigits)?
			la0 = LA0;
			if (la0 == '.') {
				la1 = LA(1);
				if (HexDigit_set0.Contains(la1)) {
					if (Try_HexNumber_Test0(1)) {
						Skip();
						#line 85 "EcsLexerGrammar.les"
						_isFloat = true;
						#line default
						HexDigits();
					}
				}
			}
			// Line 86: ([Pp] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'P' || la0 == 'p') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					#line 86 "EcsLexerGrammar.les"
					_isFloat = true;
					#line default
					Skip();
					// Line 86: ([+\-])?
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
			#line 89 "EcsLexerGrammar.les"
			_numberBase = 2;
			#line default
			// Line 90: ([.] DecDigits | DecDigits ([.] DecDigits)?)
			la0 = LA0;
			if (la0 == '.') {
				Skip();
				DecDigits();
				#line 90 "EcsLexerGrammar.les"
				_isFloat = true;
				#line default
			} else {
				DecDigits();
				// Line 91: ([.] DecDigits)?
				la0 = LA0;
				if (la0 == '.') {
					la1 = LA(1);
					if (la1 >= '0' && la1 <= '9') {
						#line 91 "EcsLexerGrammar.les"
						_isFloat = true;
						#line default
						Skip();
						DecDigits();
					}
				}
			}
			// Line 93: ([Pp] ([+\-])? DecDigits)?
			la0 = LA0;
			if (la0 == 'P' || la0 == 'p') {
				la1 = LA(1);
				if (la1 == '+' || la1 == '-' || la1 >= '0' && la1 <= '9') {
					#line 93 "EcsLexerGrammar.les"
					_isFloat = true;
					#line default
					Skip();
					// Line 93: ([+\-])?
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
			#line 96 "EcsLexerGrammar.les"
			_isFloat = false;
			#line 96 "EcsLexerGrammar.les"
			_typeSuffix = null;
			#line default
			// Line 97: ( HexNumber / BinNumber / DecNumber )
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
			// Line 98: ( [Ff] | [Dd] | [Mm] | [Ll] ([Uu])? | [Uu] ([Ll])? )?
			switch (LA0) {
			case 'F':
			case 'f':
				{
					Skip();
					#line 98 "EcsLexerGrammar.les"
					_typeSuffix = _F;
					#line 98 "EcsLexerGrammar.les"
					_isFloat = true;
					#line default
				}
				break;
			case 'D':
			case 'd':
				{
					Skip();
					#line 99 "EcsLexerGrammar.les"
					_typeSuffix = _D;
					#line 99 "EcsLexerGrammar.les"
					_isFloat = true;
					#line default
				}
				break;
			case 'M':
			case 'm':
				{
					Skip();
					#line 100 "EcsLexerGrammar.les"
					_typeSuffix = _M;
					#line 100 "EcsLexerGrammar.les"
					_isFloat = true;
					#line default
				}
				break;
			case 'L':
			case 'l':
				{
					Skip();
					#line 102 "EcsLexerGrammar.les"
					_typeSuffix = _L;
					#line default
					// Line 102: ([Uu])?
					la0 = LA0;
					if (la0 == 'U' || la0 == 'u') {
						Skip();
						#line 102 "EcsLexerGrammar.les"
						_typeSuffix = _UL;
						#line default
					}
				}
				break;
			case 'U':
			case 'u':
				{
					Skip();
					#line 103 "EcsLexerGrammar.les"
					_typeSuffix = _U;
					#line default
					// Line 103: ([Ll])?
					la0 = LA0;
					if (la0 == 'L' || la0 == 'l') {
						Skip();
						#line 103 "EcsLexerGrammar.les"
						_typeSuffix = _UL;
						#line default
					}
				}
				break;
			}
			#line 105 "EcsLexerGrammar.les"
			ParseNumberValue();
			#line default
		}
		void SQString()
		{
			int la0;
			_parseNeeded = false;
			_verbatim = false;
			Skip();
			// Line 113: ([\\] [^\$] | [^\$\n\r'\\])*
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					Skip();
					MatchExcept();
					#line 113 "EcsLexerGrammar.les"
					_parseNeeded = true;
					#line default
				} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '\''))
					Skip();
				else
					break;
			}
			Match('\'');
			#line 114 "EcsLexerGrammar.les"
			ParseSQStringValue();
			#line default
		}
		void DQString()
		{
			int la0, la1;
			_parseNeeded = false;
			_verbatim = false;
			// Line 119: (["] ([\\] [^\$] | [^\$\n\r"\\])* ["] | [@] ["] (["] ["] / [^\$"])* ["])
			la0 = LA0;
			if (la0 == '"') {
				Skip();
				// Line 119: ([\\] [^\$] | [^\$\n\r"\\])*
				for (;;) {
					la0 = LA0;
					if (la0 == '\\') {
						Skip();
						MatchExcept();
						#line 119 "EcsLexerGrammar.les"
						_parseNeeded = true;
						#line default
					} else if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == '"'))
						Skip();
					else
						break;
				}
				Match('"');
			} else {
				#line 120 "EcsLexerGrammar.les"
				_verbatim = true;
				#line 120 "EcsLexerGrammar.les"
				_style = NodeStyle.Alternate;
				#line default
				Match('@');
				Match('"');
				// Line 121: (["] ["] / [^\$"])*
				for (;;) {
					la0 = LA0;
					if (la0 == '"') {
						la1 = LA(1);
						if (la1 == '"') {
							Skip();
							Skip();
							#line 121 "EcsLexerGrammar.les"
							_parseNeeded = true;
							#line default
						} else
							break;
					} else if (la0 != -1)
						Skip();
					else
						break;
				}
				Match('"');
			}
			#line 122 "EcsLexerGrammar.les"
			ParseStringValue();
			#line default
		}
		void TQString()
		{
			int la0, la1, la2;
			_parseNeeded = true;
			_style = NodeStyle.Alternate2;
			// Line 128: (["] ["] ["] nongreedy([^\$])* ["] ["] ["] | ['] ['] ['] nongreedy([^\$])* ['] ['] ['])
			la0 = LA0;
			if (la0 == '"') {
				Skip();
				Match('"');
				Match('"');
				// Line 128: nongreedy([^\$])*
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
				// Line 129: nongreedy([^\$])*
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
				#line 129 "EcsLexerGrammar.les"
				_style = NodeStyle.Alternate | NodeStyle.Alternate2;
				#line default
			}
			#line 131 "EcsLexerGrammar.les"
			ParseStringValue();
			#line default
		}
		void BQStringN()
		{
			int la0;
			#line 136 "EcsLexerGrammar.les"
			_verbatim = false;
			#line default
			Skip();
			// Line 137: ([\\] [^\$] | [^\$\n\r\\`])*
			for (;;) {
				la0 = LA0;
				if (la0 == '\\') {
					Skip();
					#line 137 "EcsLexerGrammar.les"
					_parseNeeded = true;
					#line default
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
			#line 140 "EcsLexerGrammar.les"
			_parseNeeded = false;
			#line default
			BQStringN();
			#line 142 "EcsLexerGrammar.les"
			ParseBQStringValue();
			#line default
		}
		void IdStartChar()
		{
			Skip();
		}
		void IdUniLetter()
		{
			int la0, la1;
			// Line 150: ( &{@char.IsLetter(LA0->@char)} (128..65532) | [\\] [u] HexDigit HexDigit HexDigit HexDigit | [\\] [U] HexDigit HexDigit HexDigit HexDigit HexDigit HexDigit HexDigit HexDigit )
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
					#line 151 "EcsLexerGrammar.les"
					_parseNeeded = true;
					#line default
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
					#line 152 "EcsLexerGrammar.les"
					_parseNeeded = true;
					#line default
				}
			}
		}
		void IdContChars()
		{
			int la0, la1, la2, la3, la4, la5, la6, la7, la8;
			// Line 155: ( [#'0-9] | IdStartChar | IdUniLetter )*
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
			// Line 156: (IdStartChar | IdUniLetter)
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
			// Line 162: (BQStringN | (IdUniLetter / LettersOrPunc) (IdUniLetter / LettersOrPunc)*)
			la0 = LA0;
			if (la0 == '`') {
				BQStringN();
				#line 162 "EcsLexerGrammar.les"
				return true;
				#line default
			} else {
				// Line 163: (IdUniLetter / LettersOrPunc)
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
				// Line 163: (IdUniLetter / LettersOrPunc)*
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
				#line 163 "EcsLexerGrammar.les"
				return false;
				#line default
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
			// Line 169: (NormalId / FancyId)
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
			#line 170 "EcsLexerGrammar.les"
			ParseSymbolValue(isBQ);
			#line default
		}
		static readonly HashSet<int> Id_set0 = NewSetOfRanges('A', 'Z', '\\', '\\', '_', '_', 'a', 'z', 128, 65532);
		void Id()
		{
			int la0, la1, la2, la3, la4, la5, la6;
			_parseNeeded = _verbatim = false;
			bool isBQ = false;
			int skipAt = 0;
			// Line 177: ( default NormalId | HashId | [@] (NormalId / FancyId) )
			la0 = LA0;
			if (Id_set0.Contains(la0))
				NormalId();
			else if (la0 == '#')
				HashId();
			else if (la0 == '@') {
				Skip();
				// Line 179: (NormalId / FancyId)
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
				#line 179 "EcsLexerGrammar.les"
				skipAt = 1;
				#line default
			} else
				NormalId();
			#line 180 "EcsLexerGrammar.les"
			ParseIdValue(skipAt, isBQ);
			#line default
		}
		static readonly HashSet<int> LettersOrPunc_set0 = NewSetOfRanges('!', '!', '#', '\'', '*', '+', '-', ':', '<', '?', 'A', 'Z', '\\', '\\', '^', '_', 'a', 'z', '|', '|', '~', '~');
		void LettersOrPunc()
		{
			Match(LettersOrPunc_set0);
		}
		void Comma()
		{
			Skip();
			#line 193 "EcsLexerGrammar.les"
			_type = TT.Comma;
			#line 193 "EcsLexerGrammar.les"
			_value = _Comma;
			#line default
		}
		void Semicolon()
		{
			Skip();
			#line 194 "EcsLexerGrammar.les"
			_type = TT.Semicolon;
			#line 194 "EcsLexerGrammar.les"
			_value = _Semicolon;
			#line default
		}
		void At()
		{
			Skip();
			#line 195 "EcsLexerGrammar.les"
			_type = TT.At;
			#line 195 "EcsLexerGrammar.les"
			_value = _At;
			#line default
		}
		void Operator()
		{
			int la1, la2;
			// Line 198: ( (((((((((((([.] [.] [.] / [.] [.] [<] / [.] [.] / [.]) | ([>] [>] [=] / [>] [=] / [>] / [<] [<] [=] / [<] [=] / [<])) | ([&] [&] / [&] [=] / [&])) | ([|] [|] / [|] [=] / [|])) | ([\^] [\^] / [\^] [=] / [\^])) | ([:] [=] / [=] [:] / [:] [:] / [:] / [=] [=] [>] / [=] [=] / [=] [>] / [=])) | ([!] [=] / [!]) | [~]) | ([*] [*] [=] / [*] [*] / [*] [=] / [*])) | ([/] [=] / [/])) | ([%] [=] / [%])) | ([+] [=] / [+] [+] / [+])) | ([\-] [>] / [\-] [=] / [\-] [\-] / [\-])) | ([?] [?] [=] / [?] [?] / [?] [.] / [?]) | [$] | [\\] )
			do {
				switch (LA0) {
				case '.':
					{
						la1 = LA(1);
						if (la1 == '.') {
							la2 = LA(2);
							if (la2 == '.') {
								Skip();
								Skip();
								Skip();
								#line 198 "EcsLexerGrammar.les"
								_type = TT.DotDot;
								#line 198 "EcsLexerGrammar.les"
								_value = _DotDotDot;
								#line default
							} else if (la2 == '<') {
								Skip();
								Skip();
								Skip();
								#line 199 "EcsLexerGrammar.les"
								_type = TT.DotDot;
								#line 199 "EcsLexerGrammar.les"
								_value = _DotDot;
								#line default
							} else {
								Skip();
								Skip();
								#line 200 "EcsLexerGrammar.les"
								_type = TT.DotDot;
								#line 200 "EcsLexerGrammar.les"
								_value = _DotDot;
								#line default
							}
						} else {
							Skip();
							#line 201 "EcsLexerGrammar.les"
							_type = TT.Dot;
							#line 201 "EcsLexerGrammar.les"
							_value = _Dot;
							#line default
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
								#line 202 "EcsLexerGrammar.les"
								_type = TT.CompoundSet;
								#line 202 "EcsLexerGrammar.les"
								_value = _ShrSet;
								#line default
							} else
								goto match7;
						} else if (la1 == '=') {
							Skip();
							Skip();
							#line 203 "EcsLexerGrammar.les"
							_type = TT.LEGE;
							#line 203 "EcsLexerGrammar.les"
							_value = _GE;
							#line default
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
								#line 205 "EcsLexerGrammar.les"
								_type = TT.CompoundSet;
								#line 205 "EcsLexerGrammar.les"
								_value = _ShlSet;
								#line default
							} else
								goto match10;
						} else if (la1 == '=') {
							Skip();
							Skip();
							#line 206 "EcsLexerGrammar.les"
							_type = TT.LEGE;
							#line 206 "EcsLexerGrammar.les"
							_value = _LE;
							#line default
						} else
							goto match10;
					}
					break;
				case '&':
					{
						la1 = LA(1);
						if (la1 == '&') {
							Skip();
							Skip();
							#line 208 "EcsLexerGrammar.les"
							_type = TT.And;
							#line 208 "EcsLexerGrammar.les"
							_value = _And;
							#line default
						} else if (la1 == '=') {
							Skip();
							Skip();
							#line 209 "EcsLexerGrammar.les"
							_type = TT.CompoundSet;
							#line 209 "EcsLexerGrammar.les"
							_value = _AndBitsSet;
							#line default
						} else {
							Skip();
							#line 210 "EcsLexerGrammar.les"
							_type = TT.AndBits;
							#line 210 "EcsLexerGrammar.les"
							_value = _AndBits;
							#line default
						}
					}
					break;
				case '|':
					{
						la1 = LA(1);
						if (la1 == '|') {
							Skip();
							Skip();
							#line 211 "EcsLexerGrammar.les"
							_type = TT.OrXor;
							#line 211 "EcsLexerGrammar.les"
							_value = _Or;
							#line default
						} else if (la1 == '=') {
							Skip();
							Skip();
							#line 212 "EcsLexerGrammar.les"
							_type = TT.CompoundSet;
							#line 212 "EcsLexerGrammar.les"
							_value = _OrBitsSet;
							#line default
						} else {
							Skip();
							#line 213 "EcsLexerGrammar.les"
							_type = TT.OrBits;
							#line 213 "EcsLexerGrammar.les"
							_value = _OrBits;
							#line default
						}
					}
					break;
				case '^':
					{
						la1 = LA(1);
						if (la1 == '^') {
							Skip();
							Skip();
							#line 214 "EcsLexerGrammar.les"
							_type = TT.OrXor;
							#line 214 "EcsLexerGrammar.les"
							_value = _Xor;
							#line default
						} else if (la1 == '=') {
							Skip();
							Skip();
							#line 215 "EcsLexerGrammar.les"
							_type = TT.CompoundSet;
							#line 215 "EcsLexerGrammar.les"
							_value = _XorBitsSet;
							#line default
						} else {
							Skip();
							#line 216 "EcsLexerGrammar.les"
							_type = TT.XorBits;
							#line 216 "EcsLexerGrammar.les"
							_value = _XorBits;
							#line default
						}
					}
					break;
				case ':':
					{
						la1 = LA(1);
						if (la1 == '=') {
							Skip();
							Skip();
							#line 217 "EcsLexerGrammar.les"
							_type = TT.CompoundSet;
							#line 217 "EcsLexerGrammar.les"
							_value = _QuickBindSet;
							#line default
						} else if (la1 == ':') {
							Skip();
							Skip();
							#line 219 "EcsLexerGrammar.les"
							_type = TT.ColonColon;
							#line 219 "EcsLexerGrammar.les"
							_value = _ColonColon;
							#line default
						} else {
							Skip();
							#line 220 "EcsLexerGrammar.les"
							_type = TT.Colon;
							#line 220 "EcsLexerGrammar.les"
							_value = _Colon;
							#line default
						}
					}
					break;
				case '=':
					{
						la1 = LA(1);
						if (la1 == ':') {
							Skip();
							Skip();
							#line 218 "EcsLexerGrammar.les"
							_type = TT.QuickBind;
							#line 218 "EcsLexerGrammar.les"
							_value = _QuickBind;
							#line default
						} else if (la1 == '=') {
							la2 = LA(2);
							if (la2 == '>') {
								Skip();
								Skip();
								Skip();
								#line 221 "EcsLexerGrammar.les"
								_type = TT.Forward;
								#line 221 "EcsLexerGrammar.les"
								_value = _Forward;
								#line default
							} else {
								Skip();
								Skip();
								#line 222 "EcsLexerGrammar.les"
								_type = TT.EqNeq;
								#line 222 "EcsLexerGrammar.les"
								_value = _Eq;
								#line default
							}
						} else if (la1 == '>') {
							Skip();
							Skip();
							#line 223 "EcsLexerGrammar.les"
							_type = TT.LambdaArrow;
							#line 223 "EcsLexerGrammar.les"
							_value = _LambdaArrow;
							#line default
						} else {
							Skip();
							#line 224 "EcsLexerGrammar.les"
							_type = TT.Set;
							#line 224 "EcsLexerGrammar.les"
							_value = _Set;
							#line default
						}
					}
					break;
				case '!':
					{
						la1 = LA(1);
						if (la1 == '=') {
							Skip();
							Skip();
							#line 225 "EcsLexerGrammar.les"
							_type = TT.EqNeq;
							#line 225 "EcsLexerGrammar.les"
							_value = _Neq;
							#line default
						} else {
							Skip();
							#line 226 "EcsLexerGrammar.les"
							_type = TT.Not;
							#line 226 "EcsLexerGrammar.les"
							_value = _Not;
							#line default
						}
					}
					break;
				case '~':
					{
						Skip();
						#line 227 "EcsLexerGrammar.les"
						_type = TT.NotBits;
						#line 227 "EcsLexerGrammar.les"
						_value = _NotBits;
						#line default
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
								#line 228 "EcsLexerGrammar.les"
								_type = TT.CompoundSet;
								#line 228 "EcsLexerGrammar.les"
								_value = _ExpSet;
								#line default
							} else {
								Skip();
								Skip();
								#line 229 "EcsLexerGrammar.les"
								_type = TT.Power;
								#line 229 "EcsLexerGrammar.les"
								_value = _Exp;
								#line default
							}
						} else if (la1 == '=') {
							Skip();
							Skip();
							#line 230 "EcsLexerGrammar.les"
							_type = TT.CompoundSet;
							#line 230 "EcsLexerGrammar.les"
							_value = _MulSet;
							#line default
						} else {
							Skip();
							#line 231 "EcsLexerGrammar.les"
							_type = TT.Mul;
							#line 231 "EcsLexerGrammar.les"
							_value = _Mul;
							#line default
						}
					}
					break;
				case '/':
					{
						la1 = LA(1);
						if (la1 == '=') {
							Skip();
							Skip();
							#line 232 "EcsLexerGrammar.les"
							_type = TT.CompoundSet;
							#line 232 "EcsLexerGrammar.les"
							_value = _DivSet;
							#line default
						} else {
							Skip();
							#line 233 "EcsLexerGrammar.les"
							_type = TT.DivMod;
							#line 233 "EcsLexerGrammar.les"
							_value = _Div;
							#line default
						}
					}
					break;
				case '%':
					{
						la1 = LA(1);
						if (la1 == '=') {
							Skip();
							Skip();
							#line 234 "EcsLexerGrammar.les"
							_type = TT.CompoundSet;
							#line 234 "EcsLexerGrammar.les"
							_value = _ModSet;
							#line default
						} else {
							Skip();
							#line 235 "EcsLexerGrammar.les"
							_type = TT.DivMod;
							#line 235 "EcsLexerGrammar.les"
							_value = _Mod;
							#line default
						}
					}
					break;
				case '+':
					{
						la1 = LA(1);
						if (la1 == '=') {
							Skip();
							Skip();
							#line 236 "EcsLexerGrammar.les"
							_type = TT.CompoundSet;
							#line 236 "EcsLexerGrammar.les"
							_value = _AddSet;
							#line default
						} else if (la1 == '+') {
							Skip();
							Skip();
							#line 237 "EcsLexerGrammar.les"
							_type = TT.IncDec;
							#line 237 "EcsLexerGrammar.les"
							_value = _Inc;
							#line default
						} else {
							Skip();
							#line 238 "EcsLexerGrammar.les"
							_type = TT.Add;
							#line 238 "EcsLexerGrammar.les"
							_value = _Add;
							#line default
						}
					}
					break;
				case '-':
					{
						la1 = LA(1);
						if (la1 == '>') {
							Skip();
							Skip();
							#line 239 "EcsLexerGrammar.les"
							_type = TT.PtrArrow;
							#line 239 "EcsLexerGrammar.les"
							_value = _PtrArrow;
							#line default
						} else if (la1 == '=') {
							Skip();
							Skip();
							#line 240 "EcsLexerGrammar.les"
							_type = TT.CompoundSet;
							#line 240 "EcsLexerGrammar.les"
							_value = _SubSet;
							#line default
						} else if (la1 == '-') {
							Skip();
							Skip();
							#line 241 "EcsLexerGrammar.les"
							_type = TT.IncDec;
							#line 241 "EcsLexerGrammar.les"
							_value = _Dec;
							#line default
						} else {
							Skip();
							#line 242 "EcsLexerGrammar.les"
							_type = TT.Sub;
							#line 242 "EcsLexerGrammar.les"
							_value = _Sub;
							#line default
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
								#line 243 "EcsLexerGrammar.les"
								_type = TT.CompoundSet;
								#line 243 "EcsLexerGrammar.les"
								_value = _NullCoalesceSet;
								#line default
							} else {
								Skip();
								Skip();
								#line 244 "EcsLexerGrammar.les"
								_type = TT.NullCoalesce;
								#line 244 "EcsLexerGrammar.les"
								_value = _NullCoalesce;
								#line default
							}
						} else if (la1 == '.') {
							Skip();
							Skip();
							#line 245 "EcsLexerGrammar.les"
							_type = TT.NullDot;
							#line 245 "EcsLexerGrammar.les"
							_value = _NullDot;
							#line default
						} else {
							Skip();
							#line 246 "EcsLexerGrammar.les"
							_type = TT.QuestionMark;
							#line 246 "EcsLexerGrammar.les"
							_value = _QuestionMark;
							#line default
						}
					}
					break;
				case '$':
					{
						Skip();
						#line 247 "EcsLexerGrammar.les"
						_type = TT.Substitute;
						#line 247 "EcsLexerGrammar.les"
						_value = _Substitute;
						#line default
					}
					break;
				default:
					{
						Match('\\');
						#line 248 "EcsLexerGrammar.les"
						_type = TT.Backslash;
						#line 248 "EcsLexerGrammar.les"
						_value = _Backslash;
						#line default
					}
					break;
				}
				break;
			match7:
				{
					Skip();
					#line 204 "EcsLexerGrammar.les"
					_type = TT.GT;
					#line 204 "EcsLexerGrammar.les"
					_value = _GT;
					#line default
				}
				break;
			match10:
				{
					Skip();
					#line 207 "EcsLexerGrammar.les"
					_type = TT.LT;
					#line 207 "EcsLexerGrammar.les"
					_value = _LT;
					#line default
				}
			} while (false);
		}
		void Shebang()
		{
			int la0;
			Skip();
			Skip();
			// Line 307: ([^\$\n\r])*
			for (;;) {
				la0 = LA0;
				if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
					Skip();
				else
					break;
			}
			// Line 307: (Newline)?
			la0 = LA0;
			if (la0 == '\n' || la0 == '\r')
				Newline();
		}
		static readonly HashSet<int> IdOrKeyword_set0 = NewSetOfRanges('#', '#', '0', '9', 'A', 'Z', '_', '_', 'a', 'z');
		void IdOrKeyword()
		{
			int la1, la2, la3, la4, la5, la6, la7, la8, la9, la10;
			// Line 315: ( [a] [b] [s] [t] [r] [a] [c] [t] EndId =>  / [a] [s] EndId =>  / [b] [a] [s] [e] EndId =>  / [b] [o] [o] [l] EndId =>  / [b] [r] [e] [a] [k] EndId =>  / [b] [y] [t] [e] EndId =>  / [c] [a] [s] [e] EndId =>  / [c] [a] [t] [c] [h] EndId =>  / [c] [h] [a] [r] EndId =>  / [c] [h] [e] [c] [k] [e] [d] EndId =>  / [c] [l] [a] [s] [s] EndId =>  / [c] [o] [n] [s] [t] EndId =>  / [c] [o] [n] [t] [i] [n] [u] [e] EndId =>  / [d] [e] [c] [i] [m] [a] [l] EndId =>  / [d] [e] [f] [a] [u] [l] [t] EndId =>  / [d] [e] [l] [e] [g] [a] [t] [e] EndId =>  / [d] [o] [u] [b] [l] [e] EndId =>  / [d] [o] EndId =>  / [e] [l] [s] [e] EndId =>  / [e] [n] [u] [m] EndId =>  / [e] [v] [e] [n] [t] EndId =>  / [e] [x] [p] [l] [i] [c] [i] [t] EndId =>  / [e] [x] [t] [e] [r] [n] EndId =>  / [f] [a] [l] [s] [e] EndId =>  / [f] [i] [n] [a] [l] [l] [y] EndId =>  / [f] [i] [x] [e] [d] EndId =>  / [f] [l] [o] [a] [t] EndId =>  / [f] [o] [r] [e] [a] [c] [h] EndId =>  / [f] [o] [r] EndId =>  / [g] [o] [t] [o] EndId =>  / [i] [f] EndId =>  / [i] [m] [p] [l] [i] [c] [i] [t] EndId =>  / [i] [n] [t] [e] [r] [f] [a] [c] [e] EndId =>  / [i] [n] [t] [e] [r] [n] [a] [l] EndId =>  / [i] [n] [t] EndId =>  / [i] [n] EndId =>  / [i] [s] EndId =>  / [l] [o] [c] [k] EndId =>  / [l] [o] [n] [g] EndId =>  / [n] [a] [m] [e] [s] [p] [a] [c] [e] EndId =>  / [n] [e] [w] EndId =>  / [n] [u] [l] [l] EndId =>  / [o] [b] [j] [e] [c] [t] EndId =>  / [o] [p] [e] [r] [a] [t] [o] [r] EndId =>  / [o] [u] [t] EndId =>  / [o] [v] [e] [r] [r] [i] [d] [e] EndId =>  / [p] [a] [r] [a] [m] [s] EndId =>  / [p] [r] [i] [v] [a] [t] [e] EndId =>  / [p] [r] [o] [t] [e] [c] [t] [e] [d] EndId =>  / [p] [u] [b] [l] [i] [c] EndId =>  / [r] [e] [a] [d] [o] [n] [l] [y] EndId =>  / [r] [e] [f] EndId =>  / [r] [e] [t] [u] [r] [n] EndId =>  / [s] [b] [y] [t] [e] EndId =>  / [s] [e] [a] [l] [e] [d] EndId =>  / [s] [h] [o] [r] [t] EndId =>  / [s] [i] [z] [e] [o] [f] EndId =>  / [s] [t] [a] [c] [k] [a] [l] [l] [o] [c] EndId =>  / [s] [t] [a] [t] [i] [c] EndId =>  / [s] [t] [r] [i] [n] [g] EndId =>  / [s] [t] [r] [u] [c] [t] EndId =>  / [s] [w] [i] [t] [c] [h] EndId =>  / [t] [h] [i] [s] EndId =>  / [t] [h] [r] [o] [w] EndId =>  / [t] [r] [u] [e] EndId =>  / [t] [r] [y] EndId =>  / [t] [y] [p] [e] [o] [f] EndId =>  / [u] [i] [n] [t] EndId =>  / [u] [l] [o] [n] [g] EndId =>  / [u] [n] [c] [h] [e] [c] [k] [e] [d] EndId =>  / [u] [n] [s] [a] [f] [e] EndId =>  / [u] [s] [h] [o] [r] [t] EndId =>  / [u] [s] [i] [n] [g] EndId =>  / [v] [i] [r] [t] [u] [a] [l] EndId =>  / [v] [o] [l] [a] [t] [i] [l] [e] EndId =>  / [v] [o] [i] [d] EndId =>  / [w] [h] [i] [l] [e] EndId =>  / &{AllowPP} [#] [i] [f] EndId =>  / &{AllowPP} [#] [e] [l] [s] [e] EndId =>  / &{AllowPP} [#] [e] [l] [i] [f] EndId =>  / &{AllowPP} [#] [e] [n] [d] [i] [f] EndId =>  / &{AllowPP} [#] [d] [e] [f] [i] [n] [e] EndId =>  / &{AllowPP} [#] [u] [n] [d] [e] [f] EndId =>  / &{AllowPP} [#] [p] [r] [a] [g] [m] [a] EndId =>  / &{AllowPP} [#] [l] [i] [n] [e] EndId =>  / &{AllowPP} [#] [e] [r] [r] [o] [r] EndId => RestOfPPLine / &{AllowPP} [#] [w] [a] [r] [n] [i] [n] [g] EndId => RestOfPPLine / &{AllowPP} [#] [n] [o] [t] [e] EndId => RestOfPPLine / &{AllowPP} [#] [r] [e] [g] [i] [o] [n] EndId => RestOfPPLine / &{AllowPP} [#] [e] [n] [d] [r] [e] [g] [i] [o] [n] EndId =>  / [v] [a] [r] EndId =>  / [d] [y] [n] [a] [m] [i] [c] EndId =>  / [t] [r] [a] [i] [t] EndId =>  / [a] [l] [i] [a] [s] EndId =>  / [a] [s] [s] [e] [m] [b] [l] [y] EndId =>  / [m] [o] [d] [u] [l] [e] EndId =>  / [f] [r] [o] [m] EndId =>  / [w] [h] [e] [r] [e] EndId =>  / [w] [h] [e] [n] EndId =>  / [s] [e] [l] [e] [c] [t] EndId =>  / [j] [o] [i] [n] EndId =>  / [o] [n] EndId =>  / [e] [q] [u] [a] [l] [s] EndId =>  / [i] [n] [t] [o] EndId =>  / [l] [e] [t] EndId =>  / [o] [r] [d] [e] [r] [b] [y] EndId =>  / [a] [s] [c] [e] [n] [d] [i] [n] [g] EndId =>  / [d] [e] [s] [c] [e] [n] [d] [i] [n] [g] EndId =>  / [g] [r] [o] [u] [p] EndId =>  / [b] [y] EndId =>  / [a] [w] [a] [i] [t] EndId =>  / Id )
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
													#line 315 "EcsLexerGrammar.les"
													_type = TT.AttrKeyword;
													#line 315 "EcsLexerGrammar.les"
													_value = S.Abstract;
													#line default
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
							#line 316 "EcsLexerGrammar.les"
							_type = TT.@as;
							#line 316 "EcsLexerGrammar.les"
							_value = S.As;
							#line default
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
													#line 409 "EcsLexerGrammar.les"
													_type = TT.ContextualKeyword;
													#line 409 "EcsLexerGrammar.les"
													_value = _assembly;
													#line default
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
														#line 421 "EcsLexerGrammar.les"
														OtherContextualKeyword();
														#line default
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
										#line 408 "EcsLexerGrammar.les"
										_type = TT.ContextualKeyword;
										#line 408 "EcsLexerGrammar.les"
										_value = _alias;
										#line default
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
										#line 425 "EcsLexerGrammar.les"
										_type = TT.ContextualKeyword;
										#line 425 "EcsLexerGrammar.les"
										_value = _await;
										#line default
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
									#line 317 "EcsLexerGrammar.les"
									_type = TT.@base;
									#line 317 "EcsLexerGrammar.les"
									_value = S.Base;
									#line default
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
									#line 318 "EcsLexerGrammar.les"
									_type = TT.TypeKeyword;
									#line 318 "EcsLexerGrammar.les"
									_value = S.Bool;
									#line default
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
										#line 319 "EcsLexerGrammar.les"
										_type = TT.@break;
										#line 319 "EcsLexerGrammar.les"
										_value = S.Break;
										#line default
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
									#line 320 "EcsLexerGrammar.les"
									_type = TT.TypeKeyword;
									#line 320 "EcsLexerGrammar.les"
									_value = S.UInt8;
									#line default
								} else
									Id();
							} else
								Id();
						} else if (!IdOrKeyword_set0.Contains(la2)) {
							Skip();
							Skip();
							#line 424 "EcsLexerGrammar.les"
							OtherContextualKeyword();
							#line default
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
									#line 321 "EcsLexerGrammar.les"
									_type = TT.@case;
									#line 321 "EcsLexerGrammar.les"
									_value = S.Case;
									#line default
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
										#line 322 "EcsLexerGrammar.les"
										_type = TT.@catch;
										#line 322 "EcsLexerGrammar.les"
										_value = S.Catch;
										#line default
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
									#line 323 "EcsLexerGrammar.les"
									_type = TT.TypeKeyword;
									#line 323 "EcsLexerGrammar.les"
									_value = S.Char;
									#line default
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
												#line 324 "EcsLexerGrammar.les"
												_type = TT.@checked;
												#line 324 "EcsLexerGrammar.les"
												_value = S.Checked;
												#line default
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
										#line 325 "EcsLexerGrammar.les"
										_type = TT.@class;
										#line 325 "EcsLexerGrammar.les"
										_value = S.Class;
										#line default
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
										#line 326 "EcsLexerGrammar.les"
										_type = TT.AttrKeyword;
										#line 326 "EcsLexerGrammar.les"
										_value = S.Const;
										#line default
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
													#line 327 "EcsLexerGrammar.les"
													_type = TT.@continue;
													#line 327 "EcsLexerGrammar.les"
													_value = S.Continue;
													#line default
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
												#line 328 "EcsLexerGrammar.les"
												_type = TT.TypeKeyword;
												#line 328 "EcsLexerGrammar.les"
												_value = S.Decimal;
												#line default
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
												#line 329 "EcsLexerGrammar.les"
												_type = TT.@default;
												#line 329 "EcsLexerGrammar.les"
												_value = S.Default;
												#line default
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
													#line 330 "EcsLexerGrammar.les"
													_type = TT.@delegate;
													#line 330 "EcsLexerGrammar.les"
													_value = S.Delegate;
													#line default
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
															#line 422 "EcsLexerGrammar.les"
															OtherContextualKeyword();
															#line default
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
											#line 331 "EcsLexerGrammar.les"
											_type = TT.TypeKeyword;
											#line 331 "EcsLexerGrammar.les"
											_value = S.Double;
											#line default
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
							#line 332 "EcsLexerGrammar.les"
							_type = TT.@do;
							#line 332 "EcsLexerGrammar.les"
							_value = S.Do;
							#line default
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
												#line 406 "EcsLexerGrammar.les"
												_type = TT.ContextualKeyword;
												#line 406 "EcsLexerGrammar.les"
												_value = _dynamic;
												#line default
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
										#line 333 "EcsLexerGrammar.les"
										_type = TT.@else;
										#line 333 "EcsLexerGrammar.les"
										_value = S.Else;
										#line default
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
										#line 334 "EcsLexerGrammar.les"
										_type = TT.@enum;
										#line 334 "EcsLexerGrammar.les"
										_value = S.Enum;
										#line default
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
											#line 335 "EcsLexerGrammar.les"
											_type = TT.@event;
											#line 335 "EcsLexerGrammar.les"
											_value = S.Event;
											#line default
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
														#line 336 "EcsLexerGrammar.les"
														_type = TT.AttrKeyword;
														#line 336 "EcsLexerGrammar.les"
														_value = S.Explicit;
														#line default
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
												#line 337 "EcsLexerGrammar.les"
												_type = TT.AttrKeyword;
												#line 337 "EcsLexerGrammar.les"
												_value = S.Extern;
												#line default
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
												#line 417 "EcsLexerGrammar.les"
												OtherContextualKeyword();
												#line default
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
											#line 338 "EcsLexerGrammar.les"
											_type = TT.Literal;
											#line 338 "EcsLexerGrammar.les"
											_value = G.BoxedFalse;
											#line default
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
													#line 339 "EcsLexerGrammar.les"
													_type = TT.@finally;
													#line 339 "EcsLexerGrammar.les"
													_value = S.Finally;
													#line default
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
											#line 340 "EcsLexerGrammar.les"
											_type = TT.@fixed;
											#line 340 "EcsLexerGrammar.les"
											_value = S.Fixed;
											#line default
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
											#line 341 "EcsLexerGrammar.les"
											_type = TT.TypeKeyword;
											#line 341 "EcsLexerGrammar.les"
											_value = S.Single;
											#line default
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
													#line 342 "EcsLexerGrammar.les"
													_type = TT.@foreach;
													#line 342 "EcsLexerGrammar.les"
													_value = S.ForEach;
													#line default
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
									#line 343 "EcsLexerGrammar.les"
									_type = TT.@for;
									#line 343 "EcsLexerGrammar.les"
									_value = S.For;
									#line default
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
										#line 411 "EcsLexerGrammar.les"
										_type = TT.ContextualKeyword;
										#line 411 "EcsLexerGrammar.les"
										_value = _from;
										#line default
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
									#line 344 "EcsLexerGrammar.les"
									_type = TT.@goto;
									#line 344 "EcsLexerGrammar.les"
									_value = S.Goto;
									#line default
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
										#line 423 "EcsLexerGrammar.les"
										OtherContextualKeyword();
										#line default
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
							#line 345 "EcsLexerGrammar.les"
							_type = TT.@if;
							#line 345 "EcsLexerGrammar.les"
							_value = S.If;
							#line default
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
													#line 346 "EcsLexerGrammar.les"
													_type = TT.AttrKeyword;
													#line 346 "EcsLexerGrammar.les"
													_value = S.Implicit;
													#line default
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
														#line 347 "EcsLexerGrammar.les"
														_type = TT.@interface;
														#line 347 "EcsLexerGrammar.les"
														_value = S.Interface;
														#line default
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
													#line 348 "EcsLexerGrammar.les"
													_type = TT.AttrKeyword;
													#line 348 "EcsLexerGrammar.les"
													_value = S.Internal;
													#line default
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
								#line 349 "EcsLexerGrammar.les"
								_type = TT.TypeKeyword;
								#line 349 "EcsLexerGrammar.les"
								_value = S.Int32;
								#line default
							} else if (la3 == 'o') {
								la4 = LA(4);
								if (!IdOrKeyword_set0.Contains(la4)) {
									Skip();
									Skip();
									Skip();
									Skip();
									#line 418 "EcsLexerGrammar.les"
									OtherContextualKeyword();
									#line default
								} else
									Id();
							} else
								Id();
						} else if (!IdOrKeyword_set0.Contains(la2)) {
							Skip();
							Skip();
							#line 350 "EcsLexerGrammar.les"
							_type = TT.@in;
							#line 350 "EcsLexerGrammar.les"
							_value = S.In;
							#line default
						} else
							Id();
					} else if (la1 == 's') {
						la2 = LA(2);
						if (!IdOrKeyword_set0.Contains(la2)) {
							Skip();
							Skip();
							#line 351 "EcsLexerGrammar.les"
							_type = TT.@is;
							#line 351 "EcsLexerGrammar.les"
							_value = S.Is;
							#line default
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
									#line 352 "EcsLexerGrammar.les"
									_type = TT.@lock;
									#line 352 "EcsLexerGrammar.les"
									_value = S.Lock;
									#line default
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
									#line 353 "EcsLexerGrammar.les"
									_type = TT.TypeKeyword;
									#line 353 "EcsLexerGrammar.les"
									_value = S.Int64;
									#line default
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
								#line 419 "EcsLexerGrammar.les"
								OtherContextualKeyword();
								#line default
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
														#line 354 "EcsLexerGrammar.les"
														_type = TT.@namespace;
														#line 354 "EcsLexerGrammar.les"
														_value = S.Namespace;
														#line default
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
								#line 355 "EcsLexerGrammar.les"
								_type = TT.@new;
								#line 355 "EcsLexerGrammar.les"
								_value = S.New;
								#line default
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
									#line 356 "EcsLexerGrammar.les"
									_type = TT.Literal;
									#line 356 "EcsLexerGrammar.les"
									_value = null;
									#line default
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
												#line 357 "EcsLexerGrammar.les"
												_type = TT.TypeKeyword;
												#line 357 "EcsLexerGrammar.les"
												_value = S.Object;
												#line default
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
														#line 358 "EcsLexerGrammar.les"
														_type = TT.@operator;
														#line 358 "EcsLexerGrammar.les"
														_value = S.Operator;
														#line default
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
									#line 359 "EcsLexerGrammar.les"
									_type = TT.AttrKeyword;
									#line 359 "EcsLexerGrammar.les"
									_value = S.Out;
									#line default
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
														#line 360 "EcsLexerGrammar.les"
														_type = TT.AttrKeyword;
														#line 360 "EcsLexerGrammar.les"
														_value = S.Override;
														#line default
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
								#line 416 "EcsLexerGrammar.les"
								OtherContextualKeyword();
								#line default
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
													#line 420 "EcsLexerGrammar.les"
													OtherContextualKeyword();
													#line default
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
											#line 361 "EcsLexerGrammar.les"
											_type = TT.AttrKeyword;
											#line 361 "EcsLexerGrammar.les"
											_value = S.Params;
											#line default
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
												#line 362 "EcsLexerGrammar.les"
												_type = TT.AttrKeyword;
												#line 362 "EcsLexerGrammar.les"
												_value = S.Private;
												#line default
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
														#line 363 "EcsLexerGrammar.les"
														_type = TT.AttrKeyword;
														#line 363 "EcsLexerGrammar.les"
														_value = S.Protected;
														#line default
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
											#line 364 "EcsLexerGrammar.les"
											_type = TT.AttrKeyword;
											#line 364 "EcsLexerGrammar.les"
											_value = S.Public;
											#line default
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
													#line 365 "EcsLexerGrammar.les"
													_type = TT.AttrKeyword;
													#line 365 "EcsLexerGrammar.les"
													_value = S.Readonly;
													#line default
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
								#line 366 "EcsLexerGrammar.les"
								_type = TT.AttrKeyword;
								#line 366 "EcsLexerGrammar.les"
								_value = S.Ref;
								#line default
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
											#line 367 "EcsLexerGrammar.les"
											_type = TT.@return;
											#line 367 "EcsLexerGrammar.les"
											_value = S.Return;
											#line default
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
											#line 368 "EcsLexerGrammar.les"
											_type = TT.TypeKeyword;
											#line 368 "EcsLexerGrammar.les"
											_value = S.Int8;
											#line default
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
												#line 369 "EcsLexerGrammar.les"
												_type = TT.AttrKeyword;
												#line 369 "EcsLexerGrammar.les"
												_value = S.Sealed;
												#line default
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
												#line 414 "EcsLexerGrammar.les"
												_type = TT.ContextualKeyword;
												#line 414 "EcsLexerGrammar.les"
												_value = _select;
												#line default
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
											#line 370 "EcsLexerGrammar.les"
											_type = TT.TypeKeyword;
											#line 370 "EcsLexerGrammar.les"
											_value = S.Int16;
											#line default
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
												#line 371 "EcsLexerGrammar.les"
												_type = TT.@sizeof;
												#line 371 "EcsLexerGrammar.les"
												_value = S.Sizeof;
												#line default
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
																#line 372 "EcsLexerGrammar.les"
																_type = TT.@stackalloc;
																#line 372 "EcsLexerGrammar.les"
																_value = S.StackAlloc;
																#line default
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
												#line 373 "EcsLexerGrammar.les"
												_type = TT.AttrKeyword;
												#line 373 "EcsLexerGrammar.les"
												_value = S.Static;
												#line default
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
												#line 374 "EcsLexerGrammar.les"
												_type = TT.TypeKeyword;
												#line 374 "EcsLexerGrammar.les"
												_value = S.String;
												#line default
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
												#line 375 "EcsLexerGrammar.les"
												_type = TT.@struct;
												#line 375 "EcsLexerGrammar.les"
												_value = S.Struct;
												#line default
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
												#line 376 "EcsLexerGrammar.les"
												_type = TT.@switch;
												#line 376 "EcsLexerGrammar.les"
												_value = S.Switch;
												#line default
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
									#line 377 "EcsLexerGrammar.les"
									_type = TT.@this;
									#line 377 "EcsLexerGrammar.les"
									_value = S.This;
									#line default
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
										#line 378 "EcsLexerGrammar.les"
										_type = TT.@throw;
										#line 378 "EcsLexerGrammar.les"
										_value = S.Throw;
										#line default
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
									#line 379 "EcsLexerGrammar.les"
									_type = TT.Literal;
									#line 379 "EcsLexerGrammar.les"
									_value = G.BoxedTrue;
									#line default
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
								#line 380 "EcsLexerGrammar.les"
								_type = TT.@try;
								#line 380 "EcsLexerGrammar.les"
								_value = S.Try;
								#line default
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
										#line 407 "EcsLexerGrammar.les"
										_type = TT.ContextualKeyword;
										#line 407 "EcsLexerGrammar.les"
										_value = _trait;
										#line default
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
											#line 381 "EcsLexerGrammar.les"
											_type = TT.@typeof;
											#line 381 "EcsLexerGrammar.les"
											_value = S.Typeof;
											#line default
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
									#line 382 "EcsLexerGrammar.les"
									_type = TT.TypeKeyword;
									#line 382 "EcsLexerGrammar.les"
									_value = S.UInt32;
									#line default
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
										#line 383 "EcsLexerGrammar.les"
										_type = TT.TypeKeyword;
										#line 383 "EcsLexerGrammar.les"
										_value = S.UInt64;
										#line default
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
														#line 384 "EcsLexerGrammar.les"
														_type = TT.@unchecked;
														#line 384 "EcsLexerGrammar.les"
														_value = S.Unchecked;
														#line default
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
											#line 385 "EcsLexerGrammar.les"
											_type = TT.AttrKeyword;
											#line 385 "EcsLexerGrammar.les"
											_value = S.Unsafe;
											#line default
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
											#line 386 "EcsLexerGrammar.les"
											_type = TT.TypeKeyword;
											#line 386 "EcsLexerGrammar.les"
											_value = S.UInt16;
											#line default
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
										#line 387 "EcsLexerGrammar.les"
										_type = TT.@using;
										#line 387 "EcsLexerGrammar.les"
										_value = S.UsingStmt;
										#line default
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
												#line 388 "EcsLexerGrammar.les"
												_type = TT.AttrKeyword;
												#line 388 "EcsLexerGrammar.les"
												_value = S.Virtual;
												#line default
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
													#line 389 "EcsLexerGrammar.les"
													_type = TT.AttrKeyword;
													#line 389 "EcsLexerGrammar.les"
													_value = S.Volatile;
													#line default
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
									#line 390 "EcsLexerGrammar.les"
									_type = TT.TypeKeyword;
									#line 390 "EcsLexerGrammar.les"
									_value = S.Void;
									#line default
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
								#line 405 "EcsLexerGrammar.les"
								_type = TT.ContextualKeyword;
								#line 405 "EcsLexerGrammar.les"
								_value = _var;
								#line default
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
										#line 391 "EcsLexerGrammar.les"
										_type = TT.@while;
										#line 391 "EcsLexerGrammar.les"
										_value = S.While;
										#line default
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
										#line 412 "EcsLexerGrammar.les"
										_type = TT.ContextualKeyword;
										#line 412 "EcsLexerGrammar.les"
										_value = _where;
										#line default
									} else
										Id();
								} else
									Id();
							} else if (la3 == 'n') {
								la4 = LA(4);
								if (!IdOrKeyword_set0.Contains(la4)) {
									Skip();
									Skip();
									Skip();
									Skip();
									#line 413 "EcsLexerGrammar.les"
									_type = TT.ContextualKeyword;
									#line 413 "EcsLexerGrammar.les"
									_value = _when;
									#line default
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
										#line 392 "EcsLexerGrammar.les"
										_type = TT.PPif;
										#line 392 "EcsLexerGrammar.les"
										_value = S.PPIf;
										#line default
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
												#line 393 "EcsLexerGrammar.les"
												_type = TT.PPelse;
												#line 393 "EcsLexerGrammar.les"
												_value = S.PPElse;
												#line default
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
												#line 394 "EcsLexerGrammar.les"
												_type = TT.PPelif;
												#line 394 "EcsLexerGrammar.les"
												_value = S.PPElIf;
												#line default
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
													#line 395 "EcsLexerGrammar.les"
													_type = TT.PPendif;
													#line 395 "EcsLexerGrammar.les"
													_value = S.PPEndIf;
													#line default
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
																	#line 404 "EcsLexerGrammar.les"
																	_type = TT.PPendregion;
																	#line 404 "EcsLexerGrammar.les"
																	_value = S.PPEndRegion;
																	#line default
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
													#line 400 "EcsLexerGrammar.les"
													_type = TT.PPerror;
													#line default
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
														#line 396 "EcsLexerGrammar.les"
														_type = TT.PPdefine;
														#line 396 "EcsLexerGrammar.les"
														_value = S.PPDefine;
														#line default
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
													#line 397 "EcsLexerGrammar.les"
													_type = TT.PPundef;
													#line 397 "EcsLexerGrammar.les"
													_value = S.PPUndef;
													#line default
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
														#line 398 "EcsLexerGrammar.les"
														_type = TT.PPpragma;
														#line 398 "EcsLexerGrammar.les"
														_value = S.PPPragma;
														#line default
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
												#line 399 "EcsLexerGrammar.les"
												_type = TT.PPline;
												#line 399 "EcsLexerGrammar.les"
												_value = S.PPLine;
												#line default
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
															#line 401 "EcsLexerGrammar.les"
															_type = TT.PPwarning;
															#line default
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
												#line 402 "EcsLexerGrammar.les"
												_type = TT.PPnote;
												#line default
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
														#line 403 "EcsLexerGrammar.les"
														_type = TT.PPregion;
														#line default
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
											#line 410 "EcsLexerGrammar.les"
											_type = TT.ContextualKeyword;
											#line 410 "EcsLexerGrammar.les"
											_value = _module;
											#line default
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
									#line 415 "EcsLexerGrammar.les"
									OtherContextualKeyword();
									#line default
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
			#line 457 "EcsLexerGrammar.les"
			int start = InputPosition;
			#line default
			// Line 458: ([^\$\n\r])*
			for (;;) {
				la0 = LA0;
				if (!(la0 == -1 || la0 == '\n' || la0 == '\r'))
					Skip();
				else
					break;
			}
			#line 459 "EcsLexerGrammar.les"
			return CharSource.Slice(start, InputPosition - start).ToString();
			#line default
		}
		static readonly HashSet<int> Token_set0 = NewSetOfRanges('!', '!', '#', '\'', '*', '+', '-', ':', '<', '?', '\\', '\\', '^', '^', '`', '`', '|', '|', '~', '~');
		void Token()
		{
			int la0, la1, la2;
			// Line 471: ( Newline | (Spaces / DotIndent / Number / SLComment / MLComment / &{InputPosition == 0} Shebang / Id => IdOrKeyword / TQString / SQString / DQString / BQString / Symbol / At / Operator / UTF_BOM) | Comma | Semicolon | [(] | [)] | [[] | [\]] | [{] | [}] )
			do {
				la0 = LA0;
				switch (la0) {
				case '\n':
				case '\r':
					{
						#line 471 "EcsLexerGrammar.les"
						_type = TT.Newline;
						#line default
						Newline();
					}
					break;
				case '\t':
				case ' ':
					{
						#line 472 "EcsLexerGrammar.les"
						_type = TT.Spaces;
						#line default
						Spaces();
					}
					break;
				case '.':
					{
						if (_startPosition == _lineStartAt) {
							la1 = LA(1);
							if (la1 == '\t' || la1 == ' ') {
								#line 473 "EcsLexerGrammar.les"
								_type = TT.Spaces;
								#line default
								DotIndent();
							} else if (la1 >= '0' && la1 <= '9')
								goto matchNumber;
							else
								Operator();
						} else {
							la1 = LA(1);
							if (la1 >= '0' && la1 <= '9')
								goto matchNumber;
							else
								Operator();
						}
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
				case '/':
					{
						la1 = LA(1);
						if (la1 == '/') {
							#line 475 "EcsLexerGrammar.les"
							_type = TT.SLComment;
							#line default
							SLComment();
						} else if (la1 == '*') {
							la2 = LA(2);
							if (la2 != -1) {
								#line 476 "EcsLexerGrammar.les"
								_type = TT.MLComment;
								#line default
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
								#line 478 "EcsLexerGrammar.les"
								_type = TT.Shebang;
								#line default
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
							#line 487 "EcsLexerGrammar.les"
							_type = TT.Spaces;
							#line default
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
									goto matchAt;
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
									goto matchDQString;
								else
									goto matchAt;
							}
						case '@':
							{
								la2 = LA(2);
								if (la2 >= 'A' && la2 <= 'Z' || la2 == '_' || la2 >= 'a' && la2 <= 'z')
									goto matchSymbol;
								else if (la2 >= 128 && la2 <= 65532) {
									if (char.IsLetter((char) LA0))
										goto matchSymbol;
									else
										goto matchAt;
								} else if (Token_set0.Contains(la2))
									goto matchSymbol;
								else
									goto matchAt;
							}
						default:
							if (la1 >= 'A' && la1 <= 'Z' || la1 == '_' || la1 >= 'a' && la1 <= 'z')
								goto match8;
							else if (la1 >= 128 && la1 <= 65532) {
								if (char.IsLetter((char) LA0))
									goto match8;
								else
									goto matchAt;
							} else
								goto matchAt;
						}
					}
				case '"':
					{
						la1 = LA(1);
						if (la1 == '"') {
							la2 = LA(2);
							if (la2 == '"')
								goto matchTQString;
							else
								goto matchDQString;
						} else if (!(la1 == -1 || la1 == '\n' || la1 == '\r'))
							goto matchDQString;
						else
							goto error;
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
						} else if (!(la1 == -1 || la1 == '\n' || la1 == '\r'))
							goto matchSQString;
						else
							goto error;
					}
				case '`':
					{
						#line 483 "EcsLexerGrammar.les"
						_type = TT.BQString;
						#line default
						BQString();
					}
					break;
				case '!':
				case '$':
				case '%':
				case '&':
				case '*':
				case '+':
				case '-':
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
						#line 488 "EcsLexerGrammar.les"
						_type = TT.Comma;
						#line default
						Comma();
					}
					break;
				case ';':
					{
						#line 489 "EcsLexerGrammar.les"
						_type = TT.Semicolon;
						#line default
						Semicolon();
					}
					break;
				case '(':
					{
						#line 490 "EcsLexerGrammar.les"
						_type = TT.LParen;
						#line default
						Skip();
					}
					break;
				case ')':
					{
						#line 491 "EcsLexerGrammar.les"
						_type = TT.RParen;
						#line default
						Skip();
					}
					break;
				case '[':
					{
						#line 492 "EcsLexerGrammar.les"
						_type = TT.LBrack;
						#line default
						Skip();
					}
					break;
				case ']':
					{
						#line 493 "EcsLexerGrammar.les"
						_type = TT.RBrack;
						#line default
						Skip();
					}
					break;
				case '{':
					{
						#line 494 "EcsLexerGrammar.les"
						_type = TT.LBrace;
						#line default
						Skip();
					}
					break;
				case '}':
					{
						#line 495 "EcsLexerGrammar.les"
						_type = TT.RBrace;
						#line default
						Skip();
					}
					break;
				default:
					if (la0 >= 128 && la0 <= 65278 || la0 >= 65280 && la0 <= 65532)
						goto match8;
					else
						goto error;
				}
				break;
			matchNumber:
				{
					#line 474 "EcsLexerGrammar.les"
					_type = TT.Literal;
					#line default
					Number();
				}
				break;
			match8:
				{
					#line 479 "EcsLexerGrammar.les"
					_type = TT.Id;
					#line default
					IdOrKeyword();
				}
				break;
			matchTQString:
				{
					#line 480 "EcsLexerGrammar.les"
					_type = TT.Literal;
					#line default
					TQString();
				}
				break;
			matchSQString:
				{
					#line 481 "EcsLexerGrammar.les"
					_type = TT.Literal;
					#line default
					SQString();
				}
				break;
			matchDQString:
				{
					#line 482 "EcsLexerGrammar.les"
					_type = TT.Literal;
					#line default
					DQString();
				}
				break;
			matchSymbol:
				{
					#line 484 "EcsLexerGrammar.les"
					_type = TT.Literal;
					#line default
					Symbol();
				}
				break;
			matchAt:
				{
					#line 485 "EcsLexerGrammar.les"
					_type = TT.At;
					#line default
					At();
				}
				break;
			error:
				{
					#line 496 "EcsLexerGrammar.les"
					_type = TT.Unknown;
					#line 496 "EcsLexerGrammar.les"
					Error(0, "Unrecognized token");
					#line default
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
			// Line 84: ([0-9] / HexDigits [Pp] [+\-0-9])
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
