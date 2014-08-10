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
		Spaces     = TokenKind.Spaces + 1,
		Newline    = TokenKind.Spaces + 2,
		SLComment  = TokenKind.Comment,
		MLComment  = TokenKind.Comment + 1,
		Shebang    = TokenKind.Comment + 2,
		Id         = TokenKind.Id,
		Number     = TokenKind.Number,
		String     = TokenKind.String,
		SQString   = TokenKind.String + 1,
		Symbol     = TokenKind.OtherLit,
		OtherLit   = TokenKind.OtherLit + 1, // true, false, null
		Dot        = TokenKind.Dot,
		Assignment = TokenKind.Assignment,
		NormalOp   = TokenKind.Operator,
		PreSufOp   = TokenKind.Operator + 1,  // ++, --
		SuffixOp   = TokenKind.Operator + 2,  // \\... (suffix only)
		PrefixOp   = TokenKind.Operator + 3,  // $ (prefix only)
		Colon      = TokenKind.Operator + 4,
		At         = TokenKind.Operator + 5,
		Not        = TokenKind.Operator + 6, // !, special because it's used for #of: A!(B,C) => #of(A, B, C)
		BQString   = TokenKind.Operator + 7,
		Comma      = TokenKind.Separator,
		Semicolon  = TokenKind.Separator + 1,
		LParen     = TokenKind.LParen,
		RParen     = TokenKind.RParen,
		LBrack     = TokenKind.LBrack,
		RBrack     = TokenKind.RBrack,
		LBrace     = TokenKind.LBrace,
		RBrace     = TokenKind.RBrace,
		//OpenOf     = TokenKind.OtherGroup + 1,
		Indent     = TokenKind.LBrace + 1,
		Dedent     = TokenKind.RBrace + 1,
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
		/// The results are undefined if the token was not produced by <see cref="LesLexer"/>.
		/// </remarks>
		public static string ToString(Token t)
		{
			StringBuilder sb = new StringBuilder();
			switch (t.Type()) {
				case TT.Spaces: return " ";
				case TT.Newline: return "\n";
				case TT.SLComment: return "//\n";
				case TT.MLComment: return "/**/";
				case TT.Number: 
				case TT.String:
				case TT.SQString:
				case TT.Symbol:
				case TT.OtherLit: 
					return LesNodePrinter.PrintLiteral(t.Value, t.Style);
				case TT.BQString: 
					return LesNodePrinter.PrintString((t.Value ?? "").ToString(), '`', false);
				case TT.Id: 
					return LesNodePrinter.PrintId(t.Value as Symbol ?? GSymbol.Empty);
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
				case TT.PreSufOp:
				case TT.PrefixOp:
				case TT.SuffixOp:
				case TT.Colon:
				case TT.At:
				case TT.Comma:
				case TT.Semicolon:
				case TT.Not:
					var name = t.Value.ToString();
					return name;
				case TT.Indent:
					return "'indent";
				case TT.Dedent:
					return "'dedent";
				default:
					return "@unknown_token";
			}
		}
	}
}
