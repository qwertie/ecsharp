using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Syntax.Lexing;

namespace Loyc.Syntax.Les
{
	using TT = TokenType;
	using System.Diagnostics;

	public static class TokenExt
	{
		[DebuggerStepThrough]
		public static TokenType Type(this Token t) { return (TokenType)t.TypeInt; }
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
					return LesNodePrinter.PrintString('`', false, (t.Value ?? "").ToString());
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
				case TT.Colon:
				case TT.At:
				case TT.Comma:
				case TT.Semicolon:
					var name = t.Value.ToString();
					Debug.Assert(name.StartsWith("#"));
					return name.Substring(1);
				case TT.Indent:
					return "'indent";
				case TT.Dedent:
					return "'dedent";
				default:
					return "'unknown_token";
			}
		}
	}

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
		String     = TokenKind.OtherLit,
		SQString   = TokenKind.OtherLit + 1,
		Symbol     = TokenKind.OtherLit + 3,
		OtherLit   = TokenKind.OtherLit + 4, // true, false, null
		Dot        = TokenKind.Dot,
		Assignment = TokenKind.Assignment,
		NormalOp   = TokenKind.Operator,
		PreSufOp   = TokenKind.Operator + 1,  // ++, --
		SuffixOp   = TokenKind.Operator + 2,  // \\... (suffix only)
		PrefixOp   = TokenKind.Operator + 3,  // $ (prefix only)
		Colon      = TokenKind.Operator + 4,
		At         = TokenKind.Operator + 5,
		Not        = TokenKind.Operator + 6, // !, special because it is used for #of: A!(B,C) => #of(A, B, C)
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
	}
}
