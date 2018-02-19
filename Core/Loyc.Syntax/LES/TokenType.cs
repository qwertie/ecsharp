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
		Shebang    = TokenKind.Comment + 255,
		Id         = TokenKind.Id,
		BQId       = TokenKind.Id + 1, // LESv3 only
		Literal    = TokenKind.Literal, // true, false, null, @@sym, "string", 12345
		Dot        = TokenKind.Dot,
		Assignment = TokenKind.Assignment,
		NormalOp   = TokenKind.Operator,
		PreOrSufOp = TokenKind.Operator + 1,  // ++, --
		PrefixOp   = TokenKind.Operator + 2,  // $ (prefix only)
		At         = TokenKind.Operator + 5,
		Not        = TokenKind.Operator + 6, // !, special because it's used for #of: A!(B,C) => #of(A, B, C)
		Colon      = TokenKind.Operator + 8, // LESv3 only, where : is a special line suffix
		SingleQuoteOp = TokenKind.Operator + 9, // LESv3 only
		BQOperator = TokenKind.Operator + 255, // No longer used in LESv3; `foo` is redefined as an identifier
		Comma = TokenKind.Separator,
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

		/// <summary>Expresses a token as a string, using LES printers for identifiers and literals.</summary>
		/// <remarks>Note that some Tokens do not contain enough information to
		/// reconstruct a useful token string, e.g. comment tokens do not store the 
		/// comment but merely contain the location of the comment in the source code.
		/// For performance reasons, a <see cref="Token"/> does not have a reference 
		/// to its source file, so this method cannot return the original string.
		/// </remarks>
		public static string ToString(Token t)
		{
			StringBuilder sb = new StringBuilder();
			switch (t.Kind)
			{
				case TokenKind.Spaces: return (t.Value ?? " ").ToString();
				case TokenKind.Comment:
					if (t.Type() == TokenType.SLComment)
						return "// (comment)";
					else
						return "/* (comment) */";
				case TokenKind.Id:
					return Les2Printer.PrintId(t.Value as Symbol ?? GSymbol.Empty);
				case TokenKind.Literal:
					return Les2Printer.PrintLiteral(t.Value, t.Style);
			}
			if (t.Value != null)
			{
				if (t.Type() == TokenType.BQOperator)
					return Les2Printer.PrintString((t.Value ?? "").ToString(), '`', false);
				else if (t.Type() == TokenType.Shebang)
					return "#!" + t.Value.ToString() + "\n";
				return t.Value.ToString();
			}
			switch (t.Kind)
			{
				case TokenKind.LParen: return "(";
				case TokenKind.RParen: return ")";
				case TokenKind.LBrack: return "[";
				case TokenKind.RBrack: return "]";
				case TokenKind.LBrace: return "{";
				case TokenKind.RBrace: return "}";
				case TokenKind.Indent:       return "(Indent)";
				case TokenKind.Dedent:       return "(Dedent)";
				case TokenKind.Dot:          return "(Dot)";
				case TokenKind.Assignment:   return "(Assignment)";
				case TokenKind.Operator:     return "(Operator)";
				case TokenKind.Separator:    return "(Separator)";
				case TokenKind.AttrKeyword:  return "(AttrKeyword)";
				case TokenKind.TypeKeyword:  return "(TypeKeyword)";
				case TokenKind.OtherKeyword: return "(OtherKeyword)";
			}
			return "(Type " + t.TypeInt + ")";
		}
	}
}
