using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Syntax.Lexing;

namespace Loyc.Syntax.Les
{
	using TT = TokenType;

	public enum TokenType
	{
		EOF        = 0,
		//Spaces   = TokenKind.Spaces + 1, // Lexer simply skips spaces now
		Newline    = TokenKind.Spaces + 2,
		SLComment  = TokenKind.Comment,
		MLComment  = TokenKind.Comment + 1,
		Shebang    = TokenKind.Comment + 2,
		Id         = TokenKind.Id,
		BQId       = TokenKind.Id + 1, // LESv3 only
		Literal    = TokenKind.Literal, // true, false, null, @@sym, "string", 12345
		NegativeLiteral = TokenKind.Literal + 1, // -12345
		Dot        = TokenKind.Dot,
		Assignment = TokenKind.Assignment,
		NormalOp   = TokenKind.Operator,
		PreOrSufOp = TokenKind.Operator + 1,  // ++, --
		PrefixOp   = TokenKind.Operator + 2,  // $ (prefix only)
		At         = TokenKind.Operator + 5,
		Not        = TokenKind.Operator + 6, // !, special because it's used for #of: A!(B,C) => #of(A, B, C)
		BQOperator = TokenKind.Operator + 7, // No longer used in LESv3; `foo` is redefined as an identifier
		Colon      = TokenKind.Operator + 8, // LESv3 only, where : is a special line suffix
		SingleQuoteOp = TokenKind.Operator + 9, // LESv3 only
		Comma      = TokenKind.Separator,
		Semicolon  = TokenKind.Separator + 1,
		Keyword    = TokenKind.OtherKeyword,
		LParen     = TokenKind.LParen,
		SpaceLParen= TokenKind.LParen + 1,
		RParen     = TokenKind.RParen,
		LBrack     = TokenKind.LBrack,
		RBrack     = TokenKind.RBrack,
		LBrace     = TokenKind.LBrace,
		RBrace     = TokenKind.RBrace,
		Unknown    = TokenKind.Other,
	}

	/// <summary>Provides the <c>Type()</c> extension method required by 
	/// <see cref="Token"/> and the ToString(Token) method to express an LES token
	/// as a string, for tokens that contain sufficient information to do so.</summary>
	public static class TokenExt
	{
		/// <summary>Converts <c>t.TypeInt</c> to <see cref="TokenType"/>.</summary>
		[DebuggerStepThrough]
		public static TokenType Type(this Token t) { return (TokenType)t.TypeInt; }

		/// <summary>Expresses an LES token as a string.</summary>
		/// <remarks>Note that some Tokens do not contain enough information to
		/// reconstruct a useful token string, e.g. comment tokens do not store the 
		/// comment but merely contain the location of the comment in the source code.
		/// For performance reasons, a <see cref="Token"/> does not have a reference 
		/// to its source file, so this method cannot return the original string.
		/// <para/>
		/// The results are undefined if the token was not produced by <see cref="Les2Lexer"/>.
		/// </remarks>
		public static string ToString(Token t)
		{
			StringBuilder sb = new StringBuilder();
			switch (t.Type()) {
				case TT.Newline: return "\n";
				case TT.SLComment: return "//\n";
				case TT.MLComment: return "/**/";
				case TT.Literal:
					return Les2Printer.PrintLiteral(t.Value, t.Style);
				case TT.BQOperator: 
					return Les2Printer.PrintString((t.Value ?? "").ToString(), '`', false);
				case TT.Id: 
					return Les2Printer.PrintId(t.Value as Symbol ?? GSymbol.Empty);
				case TT.LParen: return "(";
				case TT.RParen: return ")";
				case TT.LBrack: return "[";
				case TT.RBrack: return "]";
				case TT.LBrace: return "{";
				case TT.RBrace: return "}";
				//case TT.OpenOf: return ".[";
				case TT.Shebang: return "#!" + t.Value + "\n";
				case TT.Dot:
				case TT.Assignment:
				case TT.NormalOp:
				case TT.PreOrSufOp:
				case TT.PrefixOp:
				//case TT.SuffixOp:
				case TT.At:
				case TT.Comma:
				case TT.Semicolon:
				case TT.Not:
					var name = (t.Value ?? "(punc missing value)").ToString();
					return name;
				default:
					return "@unknown_token";
			}
		}
	}
}
