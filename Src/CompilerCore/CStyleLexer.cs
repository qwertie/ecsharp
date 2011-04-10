using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Essentials;
using System.Diagnostics;
using Loyc.Utilities;

namespace Loyc.CompilerCore
{
	/// <summary>
	/// The common Loyc lexer for C# and LAIF.
	/// </summary>
	/// <remarks>
	/// For now this is just a copy of BooLexerCore
	/// </remarks>
	public class CStyleLexer : BaseLexer // IEnumerable<IToken>, IParseNext<IToken>
	{
		static public readonly Symbol _NEWLINE_CHAR = GSymbol.Get("NEWLINE_CHAR");
		static public readonly Symbol _WS_CHAR = GSymbol.Get("WS_CHAR");
		static public readonly Symbol _CONTROL_CHAR = GSymbol.Get("CONTROL_CHAR");
		static public readonly Symbol _DIGIT_CHAR = GSymbol.Get("DIGIT_CHAR");
		static public readonly Symbol _HEXDIGIT_CHAR = GSymbol.Get("HEXDIGIT_CHAR");
		static public readonly Symbol _LETTER_CHAR = GSymbol.Get("LETTER_CHAR");
		static public readonly Symbol _BASIC_PUNC_CHAR = GSymbol.Get("BASIC_PUNC_CHAR");
		static public readonly Symbol _EXT_PUNC_CHAR = GSymbol.Get("EXT_PUNC_CHAR");
		static public readonly Symbol _ID = GSymbol.Get("ID");
		static public readonly Symbol _ID_LETTER = GSymbol.Get("ID_LETTER");
		static public readonly Symbol _WS = GSymbol.Get("WS");
		static public readonly Symbol _LINE_CONTINUATION = GSymbol.Get("LINE_CONTINUATION");
		static public readonly Symbol _NEWLINE = GSymbol.Get("NEWLINE");
		static public readonly Symbol _ML_COMMENT = GSymbol.Get("ML_COMMENT");
		static public readonly Symbol _SL_COMMENT = GSymbol.Get("SL_COMMENT");
		static public readonly Symbol _INT = GSymbol.Get("INT");
		static public readonly Symbol _HEX_DIGIT_GROUP = GSymbol.Get("HEX_DIGIT_GROUP");
		static public readonly Symbol _DIGIT_GROUP = GSymbol.Get("DIGIT_GROUP");
		static public readonly Symbol _REVERSE_DIGIT_GROUP = GSymbol.Get("REVERSE_DIGIT_GROUP");
		static public readonly Symbol _EXPONENT_SUFFIX = GSymbol.Get("EXPONENT_SUFFIX");
		static public readonly Symbol _REAL_FRAC = GSymbol.Get("REAL_FRAC");
		static public readonly Symbol _LPAREN = GSymbol.Get("LPAREN");
		static public readonly Symbol _RPAREN = GSymbol.Get("RPAREN");
		static public readonly Symbol _LBRACE = GSymbol.Get("LBRACE");
		static public readonly Symbol _RBRACE = GSymbol.Get("RBRACE");
		static public readonly Symbol _LBRACK = GSymbol.Get("LBRACK");
		static public readonly Symbol _RBRACK = GSymbol.Get("RBRACK");
		static public readonly Symbol _SQ_STRING = GSymbol.Get("SQ_STRING");
		static public readonly Symbol _DQ_STRING = GSymbol.Get("DQ_STRING");
		static public readonly Symbol _TQ_STRING = GSymbol.Get("TQ_STRING");
		static public readonly Symbol _BQ_STRING = GSymbol.Get("BQ_STRING");
		static public readonly Symbol _RE_STRING = GSymbol.Get("RE_STRING");
		static public readonly Symbol _RE_CHAR = GSymbol.Get("RE_CHAR");
		static public readonly Symbol _RE_CHAR_EX = GSymbol.Get("RE_CHAR_EX");
		static public readonly Symbol _RE_ESC = GSymbol.Get("RE_ESC");
		static public readonly Symbol _PUNC = GSymbol.Get("PUNC");
		static public readonly Symbol _PUNC_CHAR = GSymbol.Get("PUNC_CHAR");
		static public readonly Symbol _EOS = GSymbol.Get("EOS");
		static public readonly Symbol _COLON = GSymbol.Get("COLON");

		/// <summary>
		/// Configures the lexer to read from the specified source and recognize the 
		/// specified set of keywords.
		/// </summary>
		/// <param name="keywords">Map from keywords to token types; can be null.</param>
		/// <remarks>
		/// BooLexerCore just stores the reference to the keyword dictionary, so if
		/// the keyword list changes after the constructor call, the lexer's behavior
		/// will change to match.
		/// </remarks>
		public CStyleLexer(ISourceFile source, IDictionary<string, Symbol> keywords)
			: base(source)
		{
			if (keywords == null)
				_keywords = new Dictionary<string, Symbol>();
			else
				_keywords = keywords;
		}

		protected IDictionary<string, Symbol> _keywords;

		////////////////////////////////////////////////////////////////////////
		// Planned parser generator code ///////////////////////////////////////

		// Note how AnyToken() sets NodeType=alt between the prediction and 
		// matching stages.
		public override void AnyToken()
		{
			// | ID | WS | LINE_CONTINUATION | NEWLINE | ML_COMMENT | SL_COMMENT
			// | INT | LPAREN | RPAREN | LBRACK | RBRACK | LBRACE | RBRACE
			// | SQ_STRING | BQ_STRING | DQ_STRING | TQ_STRING | RE_STRING
			// | PUNC
			// Prediction tree:
			// '_' => ID
			// IsLETTER_CHAR => ID
			// '\\' =>
			//   '"' => ID
			//   ~(NEWLINE_CHAR || WS_CHAR) => ID
			// IsWS_CHAR => WS
			// '\n'|'\r' => NEWLINE
			// '/' =>
			//   '*' => ML_COMMENT
			//   '/' => SL_COMMENT (prealt == 1)
			//   '\\' | ~(NEWLINE_CHAR | WS_CHAR) => ('/' RE_CHAR+ '/') => RE_STRING
			//   . => PUNC
			// '#' => SL_COMMENT (prealt == 2)
			// IsDIGIT_CHAR => INT
			// '.' =>
			//   this could be a complex case to handle due to the complexity of INT.
			//   but for a handmade parser, assume it's INT if there's a digit.
			//   IsDigit_CHAR => INT (prealt == 3)
			//   . => PUNC
			// '(' => LPAREN
			// ')' => RPAREN
			// '[' => LBRACK
			// ']' => RBRACK
			// '{' => LBRACE
			// '}' => RBRACE
			// '\'' => SQ_STRING
			// '"' =>
			//   '"' =>
			//     '"' => TQ_STRING
			//     . => DQ_STRING
			//   . => DQ_STRING
			// '`' => BQ_STRING
			// ':' =>
			//   WS_CHAR | NEWLINE_CHAR | ';' | '#' => COLON
			//   '/' =>
			//     '*' | '/' => COLON
			//     . => PUNC
			//   . => PUNC
			// ';' => EOS
			// . => PUNC
			//
			// Note: all negative sets (~(...)) must check explicitly for EOF; otherwise
			// there is occasionaly the risk of an infinite loop of accepting EOFs.
			//
			// The parser generator will probably number alternatives with integers but
			// since all alternatives are rules, a symbol is definitely better.
			Symbol alt;
			char LA0 = LA(0);
			if (LA0 == '_' || IsLETTER_CHAR(LA0))
				alt = _ID;
			else if (LA0 == '\\')
			{
				char LA1 = LA(1);
				if (LA1 == '"' || !(IsNEWLINE_CHAR(LA1) || IsWS_CHAR(LA1) || LA1 == EOF))
					alt = _ID;
				else
					alt = _PUNC;
			}
			else if (IsWS_CHAR(LA0))
				alt = _WS;
			else if (LA0 == '\n' || LA0 == '\r')
				alt = _NEWLINE;
			else if (LA0 == '/')
			{
				char LA1 = LA(1);
				if (LA1 == '*')
					alt = _ML_COMMENT;
				else if (LA1 == '/')
				{
					alt = _SL_COMMENT;
					SetPrematch(_SL_COMMENT, 1);
				}
				else if (!(IsNEWLINE_CHAR(LA1) || IsWS_CHAR(LA1) || LA1 == EOF) && Try_RE_STRING_Pred1())
				{
					// Something to think about: should we test further with 
					// conventional lookahead or just do the predicate? Why 
					// should we stop at LL(2), or for that matter, go beyond 
					// LL(1)?
					alt = _RE_STRING;
					SetPrematch(_RE_STRING, 1);
				}
				else
					alt = _PUNC;
			}
			else if (LA0 == '#')
			{
				alt = _SL_COMMENT;
				SetPrematch(_SL_COMMENT, 2);
			}
			else if (IsDIGIT_CHAR(LA0))
				alt = _INT;
			else if (LA0 == '.')
			{
				char LA1 = LA(1);
				if (IsDIGIT_CHAR(LA1))
				{
					alt = _INT;
					SetPrematch(_INT, 3);
				}
				else
					alt = _PUNC;
			}
			else if (LA0 == '(')
				alt = _LPAREN;
			else if (LA0 == ')')
				alt = _RPAREN;
			else if (LA0 == '[')
				alt = _LBRACK;
			else if (LA0 == ']')
				alt = _RBRACK;
			else if (LA0 == '{')
				alt = _LBRACE;
			else if (LA0 == '}')
				alt = _RBRACE;
			else if (LA0 == '\'')
				alt = _SQ_STRING;
			else if (LA0 == '"')
			{
				char LA1 = LA(1);
				if (LA1 == '"')
				{
					char LA2 = LA(1);
					if (LA2 == '"')
						alt = _TQ_STRING;
					else
						alt = _DQ_STRING;
				}
				else
					alt = _DQ_STRING;
			}
			else if (LA0 == '`')
				alt = _BQ_STRING;
			else if (LA0 == '@')
			{
				char LA1 = LA(1);
				if (LA1 == '/')
				{
					alt = _RE_STRING;
					SetPrematch(_RE_STRING, 2);
				}
				else
					alt = _PUNC;
			}
			else if (LA0 == ';')
				alt = _EOS;
			else if (LA0 == ':')
			{
				char LA1 = LA(1);
				if (IsWS_CHAR(LA1) || IsNEWLINE_CHAR(LA1) || LA1 == ';' || LA1 == '#')
				{
					alt = _COLON;
				}
				else if (LA1 == '/')
				{
					char LA2 = LA(1);
					if (LA2 == '/' || LA2 == '*')
						alt = _COLON;
					else
						alt = _PUNC;
				}
				else
					alt = _PUNC;
			}
			else
				alt = _PUNC;

			NodeType = alt;

			if (alt == _ID)
				ID();
			else if (alt == _WS)
				WS();
			else if (alt == _LINE_CONTINUATION)
				LINE_CONTINUATION();
			else if (alt == _NEWLINE)
				NEWLINE();
			else if (alt == _ML_COMMENT)
				ML_COMMENT();
			else if (alt == _SL_COMMENT)
				SL_COMMENT();
			else if (alt == _INT)
				INT();
			else if (alt == _LPAREN)
				LPAREN();
			else if (alt == _RPAREN)
				RPAREN();
			else if (alt == _LBRACE)
				LBRACE();
			else if (alt == _RBRACE)
				RBRACE();
			else if (alt == _LBRACK)
				LBRACK();
			else if (alt == _RBRACK)
				RBRACK();
			else if (alt == _SQ_STRING)
				SQ_STRING();
			else if (alt == _DQ_STRING)
				DQ_STRING();
			else if (alt == _TQ_STRING)
				TQ_STRING();
			else if (alt == _BQ_STRING)
				BQ_STRING();
			else if (alt == _RE_STRING)
				RE_STRING();
			else if (alt == _EOS)
				EOS();
			else if (alt == _COLON)
				COLON();
			else
				PUNC();

			if (NodeType == _ID)
			{
				string text = _source2.Substring(_startingPosition, _inputPosition - _startingPosition);
				Symbol type;
				if (_keywords.TryGetValue(text, out type))
					NodeType = type;
			}
		}
		static protected bool IsEOF(char LA) { return LA == '\uFFFF'; }
		static protected bool IsNEWLINE_CHAR(char LA) { return LA == '\n' || LA == '\r'; }
		static protected bool IsWS_CHAR(char LA) { return LA == ' ' || LA == '\t'; }
		static protected bool IsCONTROL_CHAR(char LA) { return LA >= 0 && LA <= '\u001F'; }
		static protected bool IsDIGIT_CHAR(char LA) { return LA >= '0' && LA <= '9'; }
		static protected bool IsHEXDIGIT_CHAR(char LA) { return IsDIGIT_CHAR(LA) || (LA >= 'a' && LA <= 'f') || (LA >= 'A' && LA <= 'F'); }
		static protected bool IsLETTER_CHAR(char LA) { return (LA >= 'A' && LA <= 'Z') || (LA >= 'a' && LA <= 'z') || (LA >= '\u0080' && Char.IsLetter((char)LA)); }
		static protected bool IsBASIC_PUNC_CHAR(char LA)
		{
			return LA == ':' || LA == ';' || LA == '.' || LA == '~' || LA == '!'
				|| LA == '@' || LA == '$' || LA == '%' || LA == '^' || LA == '&'
				|| LA == '*' || LA == '-' || LA == '+' || LA == '=' || LA == '|'
				|| LA == ',' || LA == '<' || LA == '>' || LA == '?';
		}
		static protected bool IsEXT_PUNC_CHAR(char LA)
		{
			return IsBASIC_PUNC_CHAR(LA) || LA == '\\' || LA == '/';
		}

		public void ID()
		{
			// ID_LETTER (DIGIT_CHAR || ID_LETTER)*
			// Internal prediction prefix: none

			// The _prematch value doesn't help here because there is only one 
			// alternative and ID_LETTER will have to re-predict no matter what.
			// Perhaps the caller should prepredict _ID_LETTER because there is only 
			// one alternative and it immediately enters another rule. Note that the 
			// following test found in other rules is absent here:
			//     if (_prematch != ID_LETTER) ClearPreAlt();
			// because this method doesn't use _prealt.
			ID_LETTER();

			for (;;)
			{
				// This is a little bit tricky because we have to consider the
				// follow set of the loop--and hence ID--in order to make a proper 
				// prediction. The point of concern is the backslash, because 
				// matching it is conditional upon it being followed by 
				//     ('"' => DQ_STRING) || ~(NEWLINE_CHAR || WS_CHAR).
				// We could use backslash alone for prediction IF it were the only
				// interpretation for the backslash. But it is not. Because when ID
				// is considered as part of a stream of tokens, you should notice 
				// that '\\' can begin tokens (such as ID and PUNC); therefore we 
				// need to resolve the ambiguity using more lookahead.
				char LA0 = LA(0);
				int alt;
				if (IsDIGIT_CHAR(LA0))
					alt = 1;
				else if (LA0 == '_' || IsLETTER_CHAR(LA0))
				{
					alt = 2;
					SetPrematch(_ID_LETTER, 1);
				}
				else if (LA0 == '\\')
				{
					char LA1 = LA(1);
					if (LA1 == '"' || (!IsNEWLINE_CHAR(LA1) && !IsWS_CHAR(LA1)))
					{
						alt = 2;
						SetPrematch(_ID_LETTER, 2);
					}
					else
						alt = -1;
				}
				else
					alt = -1;

				if (alt == -1)
					break;
				else if (alt == 1)
					Consume();
				else
					ID_LETTER();
			}
		}
		protected void ID_LETTER()
		{
			//	('_' || LETTER_CHAR) ||
			//	'\\' (
			//		('"' => DQ_STRING) || ~(NEWLINE_CHAR || WS_CHAR)
			//	)
			// Internal prediction prefix: ('_' | LETTER_CHAR) | .*
			// After '\\':                 '"' | .*
			if (_prematch != _ID_LETTER)
				ClearPreAlt();

			char LA0 = LA(0);
			int alt = PreAlt;
			if (alt == 0)
			{
				if (LA0 == '_' || IsLETTER_CHAR(LA0))
					alt = 1;
				else
					// Note that there is no need to test anything to predict 
					// the second alternative - it can be assumed, for now.
					alt = 2;
			}

			if (alt == 1)
				Consume();
			else if (alt == 2)
			{
				// We have not yet tested that the input is a backslash. Do so now.
				Match('\\');

				LA0 = LA(0);
				int altB;
				if (LA0 == '"')
					// There is no need to test the predicate separately in this case
					// because it has already been tested fully by the initial 
					// prediction test. However, if we were to test it, we would 
					// do so as part of prediction, not part of matching; even if the
					// predicate is the last alternative, it is sometimes better to do 
					// it during the prediction phase because sometimes the caller will
					// have done the predicate already and the prematch mechanism can
					// avoid doing it again.
					altB = 1;
				else
					// We can always assume (at first) that the last alternative 
					// will work.
					altB = 2;

				if (altB == 1)
					DQ_STRING();
				else
				{
					// A parser generator should no doubt represent a set by some
					// other means than a (slow) delegate predicate, especially
					// since a delegate cannot be printed out to help the user in
					// case of error.
					Match(delegate(char la)
					{ return !(IsNEWLINE_CHAR(la) || IsWS_CHAR(la) || la == EOF); });
				}
			}
		}

		public void WS()
		{
			//	WS_CHAR+
			// Internal prediction prefix: none
			// After WS_CHAR:              WS_CHAR | .*
			Match(IsWS_CHAR);

			for (; ; )
			{
				char LA0 = LA(0);
				int alt;
				if (IsWS_CHAR(LA0))
					alt = 1;
				else
					alt = -1;

				if (alt == -1)
					break;
				else if (alt == 1)
					Match(IsWS_CHAR);
			}
			//_visibleToParser = false;
		}

		public void LINE_CONTINUATION()
		{
			//	'\\' NEWLINE_CHAR
			// Internal prediction prefix: none
			Match('\\');
			Match(IsNEWLINE_CHAR);
			//_visibleToParser = false;
		}

		public void NEWLINE()
		{
			//	'\n' || '\r' ('\n')?
			// Internal prediction prefix: '\n' | .*
			if (_prematch != _NEWLINE)
				ClearPreAlt();

			char LA0 = LA(0);
			int alt = PreAlt;
			if (alt == 0)
			{
				if (LA0 == '\n')
					alt = 1;
				else
					alt = 2;
			}

			if (alt == 1)
				Consume();
			else
			{
				// '\r' ('\n')?
				Match('\r');

				LA0 = LA(0);
				int altB;
				if (LA0 == '\n')
					altB = 1;
				else
					altB = -1;

				if (altB == 1)
					Consume();
			}
			//_visibleToParser = false;
		}
		public void ML_COMMENT()
		{
			//	"/*" => ("/*" nongreedy(ML_COMMENT || .)* "*/")
			// Internal prediction prefix: none
			// After "/*":                 "*/" ????? | /* .* | .*
			// Okay, there is a major difficulty here because of the way
			// "nongreedy" should work in this context. In order to disambiguate 
			// .* from the exit specification, the parser generator will have to
			// look as far ahead as it is allowed to, using the follow set of
			// ML_COMMENT which will (in general) be very complex indeed. If
			// there are any characters after "*/" that cannot be matched outside 
			// the comment, then the parser generator will refuse to allow "*/" to 
			// close the comment because the third alternative seems more 
			// attractive. Perhaps there should be an option to disregard the
			// "all tokens" follow set in situations like this; and there should 
			// also be an option to reduce the maximum k value when opening a list
			// of alternatives. But even without these features, the problem could 
			// be eliminated by rewriting the rule like so:
			//	"/*" => ("/*" (ML_COMMENT || !"*/" => .)* "*/")
			// Since I'm doing this by hand, I'll assume that the follow set of
			// ML_COMMENT is .*.
			//_visibleToParser = false;
			Match('/');
			Match('*');

			for (; ; )
			{
				char LA0 = LA(0);
				int alt;
				if (LA0 == '*')
				{
					char LA1 = LA(1);
					if (LA1 == '/')
						alt = -1;
					else
						alt = 2;
				}
				else if (LA0 == '/')
				{
					char LA1 = LA(1);
					if (LA1 == '*')
						// Don't set prematch here because ML_COMMENT doesn't use it
						alt = 1;
					else
						alt = 2;
				}
				else
					alt = 2;

				if (alt == -1)
					break;
				else if (alt == 1)
					ML_COMMENT();
				else
					Consume();
			}

			Match('*');
			Match('/');
		}
		public void SL_COMMENT()
		{
			//_visibleToParser = false;
			//    "//" => ("//" (&!"\\\\" ~NEWLINE_CHAR)* "\\\\"?)
			// || '#' ~NEWLINE_CHAR*
			// Internal prediction prefix: '/' | .*
			// After "//":                 &!"\\\\" ~NEWLINE_CHAR | .*
			if (_prematch != _SL_COMMENT)
				ClearPreAlt();

			char LA0 = LA(0);
			int alt = PreAlt;
			if (alt == 0)
			{
				if (LA0 == '/')
					// We don't need to match the entire predicate because
					// it's not needed to disambiguate with the other alternative.
					// However, note that the predicate is a prefix of what is 
					// actually matched; if this were not so, then we ought to 
					// test it completely during prediction in preference to 
					// testing it during matching (because it has to be tested 
					// sometime, which may as well be during prediction.)
					alt = 1;
				else
					alt = 2;
			}

			if (alt == 1)
			{
				// "//" (&!"\\\\" ~NEWLINE_CHAR)* "\\\\"?
				Consume();
				Match('/');
				for (; ; )
				{
					LA0 = LA(0);
					int altB;
					// TODO: think carefully about how to combine the
					// predicate with the ordinary condition. Consider more
					// complicated situations.
					if (!(IsNEWLINE_CHAR(LA0) || LA0 == EOF))
					{
						if (LA0 == '\\')
						{
							char LA1 = LA(1);
							if (LA1 == '\\')
								altB = -1;
							else
								altB = 1;
						}
						else
							altB = 1;
					}
					else
						altB = -1;

					if (altB == -1)
						break;
					else
						Consume();
				}

				LA0 = LA(0);
				int altC;
				if (LA0 == '\\')
				{
					char LA1 = LA(1);
					if (LA1 == '\\')
						altC = 1;
					else
						altC = -1;
				}
				else
					altC = -1;

				if (altC == 1)
					Consume(2);
			}
			else
			{
				// '#' ~NEWLINE_CHAR*
				Match('#');

				for (; ; )
				{
					LA0 = LA(0);
					int altD;
					if (!(IsNEWLINE_CHAR(LA0) || LA0 == EOF))
						altD = 1;
					else
						altD = -1;

					if (altD == -1)
						break;
					else
						Consume();
				}
			}
		}

		void INT()
		{
			// Incomplete!
			DIGIT_GROUP();

			// Hack in temporary support for simple floating point literals like 
			// 2.5, to avoid the difficulty of implementing the full rule.
			if (LA(0) == '.' && IsDIGIT_CHAR(LA(1)))
			{
				NodeType = Tokens.REAL;
				Consume();
				DIGIT_GROUP();
			}
		}
		void DIGIT_GROUP()
		{
			// DIGIT (('_' DIGIT DIGIT DIGIT) | DIGIT)*
			// Internal prediction prefix: none
			// After DIGIT:                '_' DIGIT DIGIT DIGIT | DIGIT | .*

			Match(delegate(char la) { return IsDIGIT_CHAR(la); });

			for (; ; )
			{
				char LA0 = LA(0);
				int alt;
				if (LA0 == '_')
				{
					char LA1 = LA(1);
					if (IsDIGIT_CHAR(LA1))
					{
						char LA2 = LA(2);
						if (IsDIGIT_CHAR(LA2))
						{
							char LA3 = LA(3);
							if (IsDIGIT_CHAR(LA3))
								alt = 1;
							else
								alt = -1;
						}
						else
							alt = -1;
					}
					else
						alt = -1;
				}
				else if (IsDIGIT_CHAR(LA0))
					alt = 2;
				else
					alt = -1;

				if (alt == -1)
					break;
				else if (alt == 1)
					Consume(4);
				else
					Consume();
			}
		}


		/* TODO
		////////////////////////////////////////////////////////////////////////////
		// Numbers
		rule INT():
			is64bit = false
			isFloat = false
			match {
				   '0' ('x' || 'X') HEX_DIGIT_GROUP
				   ('.' HEX_DIGIT_GROUP { Type = :REAL })?
				|| DIGIT_GROUP REAL_FRAC? EXPONENT_SUFFIX?
				|| REAL_FRAC EXPONENT_SUFFIX?
			}
			match { // Suffixes:
				   ('f' | 'F') { isFloat = true; Type = :REAL }
				|| &{ Type == :INT } ('l' | 'L') { is64bit = true }
			)?;
		fragment rule HEX_DIGIT_GROUP() = HEXDIGIT+ ('_' HEXDIGIT+)*
		fragment rule DIGIT_GROUP() = DIGIT (('_' DIGIT DIGIT DIGIT) | DIGIT)*
		fragment rule REVERSE_DIGIT_GROUP() = DIGIT DIGIT DIGIT '_' REVERSE_DIGIT_GROUP | DIGIT+
		fragment rule EXPONENT_SUFFIX() = ('e'|'E') ('+'|'-')? DIGIT_GROUP
		fragment rule REAL_FRAC() = '.' REVERSE_DIGIT_GROUP { Type = :REAL }
		*/

		public void LPAREN()
		{
			// The generator should produce something this simple
			Match('(');
		}
		public void RPAREN()
		{
			Match(')');
		}
		public void LBRACK()
		{
			Match('[');
		}
		public void RBRACK()
		{
			Match(']');
		}
		public void LBRACE()
		{
			Match('{');
		}
		public void RBRACE()
		{
			Match('}');
		}

		public void SQ_STRING()
		{
			// nongreedy must be used together with ?, * or +.
			// I just realized that the original formulation...
			//	   '\'' nongreedy(ESC_SEQ / ~NEWLINE_CHAR)* '\'' 
			//	|| "'''" nongreedy(.)* "'''"
			// ...produces a second alternative that's unreachable because the 
			// prefix "'''" can be considered an empty string followed by 
			// another token. ANTLR has some way of prioritizing so that the
			// longer token takes precedent, but I can't replicate that becuase 
			// I don't know how it works. So we use this formulation instead:
			//	   "'''" nongreedy(.)* "'''"
			//	|| '\'' nongreedy(ESC_SEQ / ~NEWLINE_CHAR)* '\'' 
			// Internal prediction prefix: . '\'' '\'' | .*
			// After the single open quote: '\'' | '\\' | .
			//   Note that the algorithm will consider input like "\\\n", which 
			// cannot match alt 1, but it will also see that it cannot match alt
			// -1 or 2 either, so it is not necessary to extend the lookahead 
			// beyond one character.
			int alt;
			char LA0;
			char LA1 = LA(1);
			if (LA1 == '\'')
			{
				char LA2 = LA(2);
				if (LA2 == '\'')
					alt = 1;
				else
					alt = 2;
			}
			else
				alt = 2;

			if (alt == 1)
			{
				Match('\'');
				Consume(2);

				for (; ; )
				{
					LA0 = LA(0);
					if (LA0 == '\'')
					{
						LA1 = LA(1);
						if (LA1 == '\'')
						{
							char LA2 = LA(2);
							if (LA2 == '\'')
								alt = -1;
							else
								alt = 1;
						}
						else
							alt = 1;
					}
					else
						alt = 1;

					if (alt == -1)
						break;
					else
						Consume();
				}

				Match('\'');
				Match('\'');
				Match('\'');
			}
			else
			{
				Match('\'');

				for (; ; )
				{
					LA0 = LA(0);
					if (LA0 == '\'')
						alt = -1;
					else if (LA0 == '\\')
						alt = 1;
					else
						alt = 2;

					if (alt == -1)
						break;
					else if (alt == 1)
						ESC_SEQ();
					else
						Match(delegate(char la)
						{ return !(IsNEWLINE_CHAR(la) || la == EOF); });
				}

				Match('\'');
			}
		}
		public void BQ_STRING()
		{
			// This is just a tweaked version of SQ_STRING.
			//	   "```" nongreedy(.)* "```"
			//	|| '`' nongreedy(ESC_SEQ / ~NEWLINE_CHAR)* '`' 
			// Internal prediction prefix: . '`' '`' | .*
			// After the single open quote: '`' | '\\' | .
			int alt;
			char LA0;
			char LA1 = LA(1);
			if (LA1 == '`')
			{
				char LA2 = LA(2);
				if (LA2 == '`')
					alt = 1;
				else
					alt = 2;
			}
			else
				alt = 2;

			if (alt == 1)
			{
				Match('`');
				Consume(2);

				for (; ; )
				{
					LA0 = LA(0);
					if (LA0 == '`')
					{
						LA1 = LA(1);
						if (LA1 == '`')
						{
							char LA2 = LA(2);
							if (LA2 == '`')
								alt = -1;
							else
								alt = 1;
						}
						else
							alt = 1;
					}
					else
						alt = 1;

					if (alt == -1)
						break;
					else
						Consume();
				}

				Match('`');
				Match('`');
				Match('`');
			}
			else
			{
				Match('`');

				for (; ; )
				{
					LA0 = LA(0);
					if (LA0 == '`')
						alt = -1;
					else if (LA0 == '\\')
						alt = 1;
					else
						alt = 2;

					if (alt == -1)
						break;
					else if (alt == 1)
						ESC_SEQ();
					else
						Match(delegate(char la) { return !(IsNEWLINE_CHAR(la) || la == EOF); });
				}

				Match('`');
			}
		}
		public void TQ_STRING()
		{
			Match('"');
			Match('"');
			Match('"');

			for (; ; )
			{
				int alt;
				char LA0 = LA(0);
				if (LA0 == '"')
				{
					char LA1 = LA(1);
					if (LA1 == '"')
					{
						char LA2 = LA(2);
						if (LA2 == '"')
							alt = -1;
						else
							alt = 1;
					}
					else
						alt = 1;
				}
				else
					alt = 1;

				if (alt == -1)
					break;
				else
					Consume();
			}

			Match('"');
			Match('"');
			Match('"');
		}
		public void DQ_STRING()
		{
			Match('"');

			for (; ; )
			{
				int alt;
				char LA0 = LA(0);
				if (LA0 == '"')
					alt = -1;
				else if (LA0 == '\\')
					alt = 1;
				else
					alt = 2;

				if (alt == -1)
					break;
				else if (alt == 1)
					ESC_SEQ();
				else
					Match(delegate(char la) { return !(IsNEWLINE_CHAR(la) || la == EOF); });
			}

			Match('"');
		}
		protected void ESC_SEQ()
		{
			//	'\\' ~NEWLINE_CHAR
			Match('\\');
			Match(delegate(char la)
			{ return !(IsNEWLINE_CHAR(la) || la == EOF); });
		}
		private bool Try_RE_STRING_Pred1()
		{
			// ('/' nongreedy(RE_CHAR)+ '/') => ...
			try
			{
				BeginGuess();
				Match('/');
				if (GuessFailed) return false;
				// Any rule called from a "Try" method must be able to fail without 
				// throwing an exception (either by returning bool, or, in case the 
				// user specifies his own return type, by setting a failure flag). 
				// Furthermore, any rules called by the rule must also be converted in 
				// the same way. We avoid exceptions because (1) they are expensive, 
				// so one would want very few exceptions in code that succeeds (remember 
				// that a failed predicate leads to parsing failure only sometimes) and
				// (2) debuggers are often configured to stop on all exceptions, which 
				// is inconvenient when no problem has actually occurred.
				RE_CHAR();
				if (GuessFailed) return false;

				for (; ; )
				{
					char LA0 = LA(0);
					int alt;
					if (LA0 == '/')
						alt = -1;
					else
						alt = 1;

					if (alt == -1)
						break;
					else
					{
						RE_CHAR();
						if (GuessFailed) return false;
					}
				}
				Consume();
			}
			finally
			{
				EndGuess();
			}
			return true;
		}
		public void RE_STRING()
		{
			//    ('/' nongreedy(RE_CHAR)+ '/') =>
			//    '/' nongreedy(RE_CHAR)+ '/'
			// || "@/" =>
			//    "@/" nongreedy(RE_CHAR_EX)+ '/'
			// As a rule, should we also match against one or both predicates here? 
			// I'm undecided but for simplicity I'll not match the predicates this time:
			// Internal prediction prefix: '/' | .*
			if (_prematch != _RE_STRING)
				ClearPreAlt();

			char LA0;
			int alt = PreAlt;
			if (alt == 0)
			{
				LA0 = LA(0);
				if (LA0 == '/')
					alt = 1;
				else
					alt = 2;
			}

			if (alt == 1)
			{
				Consume();
				RE_CHAR();

				for (; ; )
				{
					LA0 = LA(0);
					if (LA0 == '/')
						alt = -1;
					else
						alt = 1;

					if (alt == -1)
						break;
					else
						RE_CHAR();
				}
				Consume();
			}
			else
			{
				Match('@');
				Match('/');
				RE_CHAR_EX();

				for (; ; )
				{
					LA0 = LA(0);
					if (LA0 == '/')
						alt = -1;
					else
						alt = 1;

					if (alt == -1)
						break;
					else
						RE_CHAR_EX();
				}
				Consume();
			}
		}
		protected void RE_CHAR()
		{
			// RE_ESC / ~(NEWLINE_CHAR | WS_CHAR)
			char LA0 = LA(0);
			int alt;
			if (LA0 == '\\')
			{
				char LA1 = LA(1);
				if (!IsNEWLINE_CHAR(LA1))
					alt = 1;
				else
					alt = 2;
			}
			else
				alt = 2;

			if (alt == 1)
			{
				RE_ESC();
				if (GuessFailed) return;
			}
			else
			{
				Match(delegate(char la)
				{ return !(IsNEWLINE_CHAR(la) || IsWS_CHAR(la)); });
				if (GuessFailed) return;
			}
		}
		protected void RE_CHAR_EX()
		{
			// RE_ESC / ~NEWLINE_CHAR
			char LA0 = LA(0);
			int alt;
			if (LA0 == '\\')
			{
				char LA1 = LA(1);
				if (!IsNEWLINE_CHAR(LA1))
					alt = 1;
				else
					alt = 2;
			}
			else
				alt = 2;

			if (alt == 1)
				RE_ESC();
			else
				Match(delegate(char la)
				{ return !IsNEWLINE_CHAR(la); });
		}
		protected void RE_ESC()
		{
			//	'\\' ~NEWLINE_CHAR
			Match('\\');
			if (GuessFailed) return;
			Match(delegate(char la)
			{ return !(IsNEWLINE_CHAR(la) || la == EOF); });
			if (GuessFailed) return; // could optimize this away
		}
		/*
		rule RE_STRING():
			match {
				   ('/' RE_CHAR+ '/') =>
				   '/' RE_CHAR+ '/'
				|| "@/" =>
				   "@/" RE_CHAR_EX+ '/'
			}
		fragment RE_CHAR:    RE_ESC / ~(NEWLINE_CHAR | WS_CHAR);
		fragment RE_CHAR_EX: RE_ESC / ~NEWLINE_CHAR;
		fragment RE_ESC: '\\' ~NEWLINE_CHAR;
		*/

		private void EOS()
		{
			Match(';');
		}
		private void COLON()
		{
			Match(':');
		}
		public void PUNC()
		{
			// ( BASIC_PUNC_CHAR
			// | '/' &!('/'|'*')
			// | '\\' &(WS_CHAR)
			// | ':' &{true}
			//	)+
			// Internal prediction prefix: none
			// After first iteration: BASIC_PUNC_CHAR | '/' &!('/'|'*') | '\\' &WS_CHAR | ':' &{true} | .*
			PUNC__1();

			for (; ; )
			{
				char LA0 = LA(0);
				int alt;
				if (IsBASIC_PUNC_CHAR(LA0))
				{
					alt = 1;
					SetPrematch(_PUNC_CHAR, 1);
				}
				else if (LA0 == '/')
				{
					char LA1 = LA(1);
					if (!(LA1 == '/' || LA1 == '*'))
					{
						alt = 1;
						SetPrematch(_PUNC_CHAR, 2);
					}
					else
						alt = -1;
				}
				else if (LA0 == '\\')
				{
					char LA1 = LA(1);
					if (IsWS_CHAR(LA1))
					{
						alt = 1;
						SetPrematch(_PUNC_CHAR, 3);
					}
					else
						alt = -1;
				}
				else if (LA0 == ':' && (true))
				{
					alt = 1;
					SetPrematch(_PUNC_CHAR, 4);
				}
				else
					alt = -1;

				if (alt == -1)
					break;
				else
					PUNC__1();
			}
		}
		protected void PUNC__1()
		{
			//	   BASIC_PUNC_CHAR
			//	|| '/' &!('/'|'*')
			//	|| '\\' &WS_CHAR
			// || ':' &{true}
			// Internal prediction prefix: BASIC_PUNC_CHAR | '/' &!('/'|'*') | '\\' &WS_CHAR | . &{true}
			// IMPORTANT: the current prealt system is flawed. consider what happens 
			// for the rules
			//   FOO(): BAR? 'bar' BAR
			//   BAR(): 'ba' | 'r'
			// FOO will prematch the first time BAR is called (because it has to do a
			// prediction) but it won't clear or set the prematch after BAR returns, so 
			// it will still be set to _BAR with a possibly incorrect PreAlt when BAR 
			// is called the second time (because no prediction is required the second
			// time). TODO: Design a fix. Maybe caller should do ClearPreAlt() 
			// after calling a rule, just in case it might have done a prematch.
			// If this fix is implemented, the condition
			//   if (Prematch != _BAR) ClearPreAlt();
			// should be replaced with
			//   System.Diagnostics.Debug.Assert(PreAlt == 0 || Prematch == _BAR);
			if (Prematch != _PUNC_CHAR)
				ClearPreAlt();

			// Note that we have to check the predicates! There must be an exception
			// if the input doesn't match them. TODO: think about how the exception
			// should properly be raised.
			char LA0 = LA(0);
			int alt = PreAlt;
			if (alt == 0)
			{
				if (IsBASIC_PUNC_CHAR(LA0))
				{
					alt = 1;
				}
				else if (LA0 == '/')
				{
					char LA1 = LA(1);
					if (!(LA1 == '/' || LA1 == '*'))
						alt = 2;
					else
						Throw("something else", LA1);
				}
				else if (LA0 == '\\')
				{
					char LA1 = LA(1);
					if (IsWS_CHAR(LA1))
						alt = 3;
					else
						Throw("something else", LA1);
				}
				else
				{
					char LA1 = LA(1);
					if ((true))
						alt = 4;
					else
						Throw("something else", LA1);
				}
			}
			if (alt == 1)
				Consume();
			else if (alt == 2)
				Consume();
			else if (alt == 3)
				Consume();
			else
				Match(':');
		}
	}
}
