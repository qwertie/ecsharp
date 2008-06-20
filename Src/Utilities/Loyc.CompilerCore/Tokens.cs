using System;
using System.Collections.Generic;
using System.Text;
//using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using Loyc.Runtime;

namespace Loyc.CompilerCore
{
	/// <summary>
	/// A list of standard (cross-language) token types.
	/// </summary>
	public static class Tokens {
		static public readonly Symbol WS = Symbol.Get("WS"); // whitespace
		static public readonly Symbol NEWLINE = Symbol.Get("NEWLINE");
		static public readonly Symbol LINE_CONTINUATION = Symbol.Get("LINE_CONTINUATION");
		static public readonly Symbol ID = Symbol.Get("ID");
		static public readonly Symbol PUNC = Symbol.Get("PUNC");
		static public readonly Symbol EOS = Symbol.Get("EOS");
		static public readonly Symbol ML_COMMENT = Symbol.Get("ML_COMMENT");
		static public readonly Symbol SL_COMMENT = Symbol.Get("SL_COMMENT");
		static public readonly Symbol EXTRA_COMMENT_1 = Symbol.Get("EXTRA_COMMENT_1");
		static public readonly Symbol EXTRA_COMMENT_2 = Symbol.Get("EXTRA_COMMENT_2");
		static public readonly Symbol LPAREN = Symbol.Get("LPAREN");
		static public readonly Symbol RPAREN = Symbol.Get("RPAREN");
		static public readonly Symbol LBRACK = Symbol.Get("LBRACK");
		static public readonly Symbol RBRACK = Symbol.Get("RBRACK");
		static public readonly Symbol LBRACE = Symbol.Get("LBRACE");
		static public readonly Symbol RBRACE = Symbol.Get("RBRACE");
		static public readonly Symbol RANGLE = Symbol.Get("RANGLE");
		static public readonly Symbol LANGLE = Symbol.Get("LANGLE");
		static public readonly Symbol INDENT = Symbol.Get("INDENT");
		static public readonly Symbol DEDENT = Symbol.Get("DEDENT");
		static public readonly Symbol EXTRA_LPAREN = Symbol.Get("EXTRA_LPAREN");
		static public readonly Symbol EXTRA_RPAREN = Symbol.Get("EXTRA_RPAREN");
		static public readonly Symbol EXTRA_LBRACE = Symbol.Get("EXTRA_LBRACE");
		static public readonly Symbol EXTRA_RBRACE = Symbol.Get("EXTRA_RBRACE");
		static public readonly Symbol INT = Symbol.Get("INT");
		static public readonly Symbol REAL = Symbol.Get("REAL");
		static public readonly Symbol SYMBOL = Symbol.Get("SYMBOL");
		static public readonly Symbol EXTRA_LITERAL_1 = Symbol.Get("EXTRA_LITERAL_1");
		static public readonly Symbol EXTRA_LITERAL_2 = Symbol.Get("EXTRA_LITERAL_2");
		static public readonly Symbol EXTRA_LITERAL_3 = Symbol.Get("EXTRA_LITERAL_3");
		static public readonly Symbol EXTRA_LITERAL_4 = Symbol.Get("EXTRA_LITERAL_4");
		static public readonly Symbol FILE = Symbol.Get("FILE");
		static public readonly Symbol SQ_STRING = Symbol.Get("SQ_STRING");
		static public readonly Symbol DQ_STRING = Symbol.Get("DQ_STRING");
		static public readonly Symbol BQ_STRING = Symbol.Get("BQ_STRING");
		static public readonly Symbol TQ_STRING = Symbol.Get("TQ_STRING");
		static public readonly Symbol RE_STRING = Symbol.Get("RE_STRING");
		static public readonly Symbol EXTRA_STRING_1 = Symbol.Get("EXTRA_STRING_1");
		static public readonly Symbol EXTRA_STRING_2 = Symbol.Get("EXTRA_STRING_2");
		static public readonly Symbol EXTRA_STRING_3 = Symbol.Get("EXTRA_STRING_3");
		static public readonly Symbol EXTRA_STRING_4 = Symbol.Get("EXTRA_STRING_4");
		static public readonly Symbol PARTIAL_DEDENT = Symbol.Get("PARTIAL_DEDENT");
		static public readonly Symbol Parens = Symbol.Get("( )");
		static public readonly Symbol Bracks = Symbol.Get("[ ]");
		static public readonly Symbol Braces = Symbol.Get("{ }");
		static public readonly Symbol Angles = Symbol.Get("< >");
		static public readonly Symbol LParenRBrack = Symbol.Get("( ]");
		static public readonly Symbol LBrackRParen = Symbol.Get("[ )");
		static public readonly Symbol Block = Symbol.Get("Block");

		//static public readonly Symbol NEWLINE_CHAR = Symbol.Get("NEWLINE_CHAR");
		//static public readonly Symbol DIGIT_CHAR = Symbol.Get("DIGIT_CHAR");
		//static public readonly Symbol HEXDIGIT_CHAR = Symbol.Get("HEXDIGIT_CHAR");
		//static public readonly Symbol LETTER_CHAR = Symbol.Get("LETTER_CHAR");

		static public bool IsWsOrNewline(Symbol s)
		{
			return s == WS || s == NEWLINE || s == LINE_CONTINUATION;
		}
		static public bool IsComment(Symbol s)
		{
			return s == SL_COMMENT || s == ML_COMMENT || s == EXTRA_COMMENT_1 || s == EXTRA_COMMENT_2;
		}
		static public bool IsStringOrFile(Symbol s)
		{
			return s == FILE || IsString(s);
		}
		static public bool IsString(Symbol s)
		{
			return
				s == SQ_STRING || s == DQ_STRING || s == TQ_STRING || 
				s == BQ_STRING || s == RE_STRING ||
				s == EXTRA_STRING_1 || s == EXTRA_STRING_2 ||
				s == EXTRA_STRING_3 || s == EXTRA_STRING_4;
		}
		static public bool IsLiteral(Symbol s)
		{
			return s == INT || s == REAL|| s == SYMBOL ||
				s == EXTRA_LITERAL_1 || s == EXTRA_LITERAL_2 ||
				s == EXTRA_LITERAL_3 || s == EXTRA_LITERAL_4;
		}
		static public bool IsOpenParen(Symbol s)
		{
			return s == LPAREN || s == LBRACK || s == EXTRA_LPAREN;
		}
		static public bool IsOpenBrace(Symbol s)
		{
			return s == LBRACE || s == EXTRA_LBRACE || s == INDENT;
		}
		static public bool IsOpener(Symbol s)
		{
			return s == LPAREN || s == LBRACK || s == LBRACE || 
				s == LANGLE || s == INDENT ||
				s == EXTRA_LPAREN || s == EXTRA_LBRACE;
		}
		static public bool IsCloser(Symbol s)
		{
			return s == RPAREN || s == RBRACK || s == RBRACE || 
				s == RANGLE || s == DEDENT ||
				s == EXTRA_RPAREN || s == EXTRA_RBRACE;
		}
		static public bool IsCharSet(Symbol s)
		{
			return s.Name.EndsWith("_CHAR");
		}
	}
	/// <summary>
	/// A list of standard (cross-language) token types. The use of an enumeration
	/// rather than symbols is useful for some parser generators that like to 
	/// represent sets using bitsets. Symbol ID numbers are unpredictable and may be
	/// large, so a bitset cannot represent a set of Symbols. Since most of Loyc
	/// uses symbols, however, it is necessary to convert back and forth between 
	/// symbols and enum values, but means to do so are not provided by Loyc itself.
	/// </summary><remarks>
	/// The first four numbers are reserved as a concession to ANTLR, which uses
	/// token IDs 0 through 3 for special purposes. Mind you I'm not using ANTLR
	/// right now, but it's nice to have compatibility when someone needs it.
	/// </remarks>
	enum TokenE {
		RESERVED1,
		RESERVED2,
		RESERVED3,
		RESERVED4,
		WS,
		NEWLINE,
		LINE_CONTINUATION,
		ID,
		PUNC,
		EOS,
		ML_COMMENT,
		SL_COMMENT,
		EXTRA_COMMENT_1,
		EXTRA_COMMENT_2,
		LPAREN,
		RPAREN,
		LBRACK,
		RBRACK,
		LBRACE,
		RBRACE,
		RANGLE,
		LANGLE,
		INDENT,
		DEDENT,
		EXTRA_LPAREN_1,
		EXTRA_RPAREN_1,
		EXTRA_LPAREN_2,
		EXTRA_RPAREN_2,
		INT,
		REAL,
		SYMBOL,
		EXTRA_LITERAL_1,
		EXTRA_LITERAL_2,
		EXTRA_LITERAL_3,
		EXTRA_LITERAL_4,
		FILE,
		SQ_STRING,
		DQ_STRING,
		BQ_STRING,
		RE_STRING,
		EXTRA_STRING_1,
		EXTRA_STRING_2,
		EXTRA_STRING_3,
		EXTRA_STRING_4,
	}
}
