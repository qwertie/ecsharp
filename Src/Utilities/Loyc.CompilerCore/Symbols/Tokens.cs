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
		static public readonly Symbol WS = GSymbol.Get("WS"); // whitespace
		static public readonly Symbol NEWLINE = GSymbol.Get("NEWLINE");
		static public readonly Symbol LINE_CONTINUATION = GSymbol.Get("LINE_CONTINUATION");
		static public readonly Symbol ID = GSymbol.Get("ID");
		static public readonly Symbol PUNC = GSymbol.Get("PUNC");
		static public readonly Symbol EOS = GSymbol.Get("EOS");
		static public readonly Symbol ML_COMMENT = GSymbol.Get("ML_COMMENT");
		static public readonly Symbol SL_COMMENT = GSymbol.Get("SL_COMMENT");
		static public readonly Symbol EXTRA_COMMENT_1 = GSymbol.Get("EXTRA_COMMENT_1");
		static public readonly Symbol EXTRA_COMMENT_2 = GSymbol.Get("EXTRA_COMMENT_2");
		static public readonly Symbol LPAREN = GSymbol.Get("LPAREN");
		static public readonly Symbol RPAREN = GSymbol.Get("RPAREN");
		static public readonly Symbol LBRACK = GSymbol.Get("LBRACK");
		static public readonly Symbol RBRACK = GSymbol.Get("RBRACK");
		static public readonly Symbol LBRACE = GSymbol.Get("LBRACE");
		static public readonly Symbol RBRACE = GSymbol.Get("RBRACE");
		static public readonly Symbol RANGLE = GSymbol.Get("RANGLE");
		static public readonly Symbol LANGLE = GSymbol.Get("LANGLE");
		static public readonly Symbol INDENT = GSymbol.Get("INDENT");
		static public readonly Symbol DEDENT = GSymbol.Get("DEDENT");
		static public readonly Symbol EXTRA_LPAREN = GSymbol.Get("EXTRA_LPAREN");
		static public readonly Symbol EXTRA_RPAREN = GSymbol.Get("EXTRA_RPAREN");
		static public readonly Symbol EXTRA_LBRACE = GSymbol.Get("EXTRA_LBRACE");
		static public readonly Symbol EXTRA_RBRACE = GSymbol.Get("EXTRA_RBRACE");
		static public readonly Symbol INT = GSymbol.Get("INT");
		static public readonly Symbol REAL = GSymbol.Get("REAL");
		static public readonly Symbol SYMBOL = GSymbol.Get("SYMBOL");
		static public readonly Symbol EXTRA_LITERAL_1 = GSymbol.Get("EXTRA_LITERAL_1");
		static public readonly Symbol EXTRA_LITERAL_2 = GSymbol.Get("EXTRA_LITERAL_2");
		static public readonly Symbol EXTRA_LITERAL_3 = GSymbol.Get("EXTRA_LITERAL_3");
		static public readonly Symbol EXTRA_LITERAL_4 = GSymbol.Get("EXTRA_LITERAL_4");
		static public readonly Symbol FILE = GSymbol.Get("FILE");
		static public readonly Symbol SQ_STRING = GSymbol.Get("SQ_STRING");
		static public readonly Symbol DQ_STRING = GSymbol.Get("DQ_STRING");
		static public readonly Symbol BQ_STRING = GSymbol.Get("BQ_STRING");
		static public readonly Symbol TQ_STRING = GSymbol.Get("TQ_STRING");
		static public readonly Symbol RE_STRING = GSymbol.Get("RE_STRING");
		static public readonly Symbol EXTRA_STRING_1 = GSymbol.Get("EXTRA_STRING_1");
		static public readonly Symbol EXTRA_STRING_2 = GSymbol.Get("EXTRA_STRING_2");
		static public readonly Symbol EXTRA_STRING_3 = GSymbol.Get("EXTRA_STRING_3");
		static public readonly Symbol EXTRA_STRING_4 = GSymbol.Get("EXTRA_STRING_4");
		static public readonly Symbol PARTIAL_DEDENT = GSymbol.Get("PARTIAL_DEDENT");

		//static public readonly Symbol NEWLINE_CHAR = GSymbol.Get("NEWLINE_CHAR");
		//static public readonly Symbol DIGIT_CHAR = GSymbol.Get("DIGIT_CHAR");
		//static public readonly Symbol HEXDIGIT_CHAR = GSymbol.Get("HEXDIGIT_CHAR");
		//static public readonly Symbol LETTER_CHAR = GSymbol.Get("LETTER_CHAR");

		public static readonly SymbolSet SetOfWsEtc = new SymbolSet(WS, NEWLINE, LINE_CONTINUATION);
		public static readonly SymbolSet SetOfComments = 
			new SymbolSet(SL_COMMENT, ML_COMMENT, EXTRA_COMMENT_1, EXTRA_COMMENT_2);
		public static readonly SymbolSet SetOfStrings = 
			new SymbolSet(SQ_STRING, DQ_STRING, TQ_STRING, BQ_STRING, RE_STRING, 
				EXTRA_STRING_1, EXTRA_STRING_2, EXTRA_STRING_3, EXTRA_STRING_4);
		public static readonly SymbolSet SetOfLiterals = 
			new SymbolSet(INT, REAL, SYMBOL, EXTRA_LITERAL_1, EXTRA_LITERAL_2, EXTRA_LITERAL_3, EXTRA_LITERAL_4);
		public static readonly SymbolSet SetOfOpenParens = new SymbolSet(LPAREN, LBRACK, EXTRA_LPAREN);
		public static readonly SymbolSet SetOfCloseParens = new SymbolSet(RPAREN, RBRACK, EXTRA_RPAREN);
		public static readonly SymbolSet SetOfOpenBraces = new SymbolSet(LBRACE, EXTRA_LBRACE);
		public static readonly SymbolSet SetOfCloseBraces = new SymbolSet(RBRACE, EXTRA_RBRACE);
		public static readonly SymbolSet SetOfIndent = new SymbolSet(INDENT);
		public static readonly SymbolSet SetOfDedent = new SymbolSet(DEDENT);
		public static readonly SymbolSet SetOfIndentDedent = new SymbolSet(INDENT, DEDENT);
		public static readonly SymbolSet SetOfParens = new SymbolSet(SetOfOpenParens, SetOfCloseParens);
		public static readonly SymbolSet SetOfBraces = new SymbolSet(SetOfOpenBraces, SetOfCloseBraces);
		public static readonly SymbolSet SetOfOpeners = new SymbolSet(SetOfOpenBraces, SetOfOpenParens, SetOfIndent);
		public static readonly SymbolSet SetOfClosers = new SymbolSet(SetOfCloseBraces, SetOfCloseParens, SetOfDedent);

		static public bool IsWsOrNewline(Symbol s) { return SetOfWsEtc.Contains(s); }
		static public bool IsComment(Symbol s)  { return SetOfComments.Contains(s); }
		static public bool IsStringOrFile(Symbol s) { return s == FILE || SetOfStrings.Contains(s); }
		static public bool IsString(Symbol s) { return SetOfStrings.Contains(s); }
		static public bool IsLiteral(Symbol s) { return SetOfLiterals.Contains(s); }
		static public bool IsOpenParen(Symbol s) { return SetOfOpenParens.Contains(s); }
		static public bool IsOpenBrace(Symbol s) { return SetOfOpenBraces.Contains(s); }
		static public bool IsCloseParen(Symbol s) { return SetOfCloseParens.Contains(s); }
		static public bool IsCloseBrace(Symbol s) { return SetOfCloseBraces.Contains(s); }
		static public bool IsOpener(Symbol s) { return SetOfOpeners.Contains(s); }
		static public bool IsCloser(Symbol s) { return SetOfClosers.Contains(s); }
		static public bool IsBracket(Symbol s) { return IsOpener(s) || IsCloser(s); }
		static public bool IsCharSet(Symbol s)
		{
			return s.Name.EndsWith("_CHAR");
		}
		static public bool IsOob(Symbol nodeType)
		{
			return nodeType == Tokens.WS
				|| nodeType == Tokens.SL_COMMENT
				|| nodeType == Tokens.ML_COMMENT
				|| nodeType == Tokens.NEWLINE
				|| nodeType == Tokens.LINE_CONTINUATION
				|| nodeType == Tokens.EXTRA_COMMENT_1
				|| nodeType == Tokens.EXTRA_COMMENT_2;
		}

		public static Symbol MatchingBracket(Symbol type)
		{
			if (type == LPAREN) return RPAREN;
			if (type == LBRACE) return RBRACE;
			if (type == LBRACK) return RBRACK;
			if (type == LANGLE) return RANGLE;
			if (type == EXTRA_LPAREN) return EXTRA_RPAREN;
			if (type == EXTRA_LBRACE) return EXTRA_RBRACE;
			if (type == RPAREN) return LPAREN;
			if (type == RBRACE) return LBRACE;
			if (type == RBRACK) return LBRACK;
			if (type == RANGLE) return LANGLE;
			if (type == EXTRA_RPAREN) return EXTRA_LPAREN;
			if (type == EXTRA_RBRACE) return EXTRA_LBRACE;
			throw new ArgumentException(Localize.From("MatchingBracket: type '{0}' is not a bracket", type));
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
