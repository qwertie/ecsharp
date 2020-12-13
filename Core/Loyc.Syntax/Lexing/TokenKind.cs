using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;

namespace Loyc.Syntax.Lexing
{
	/// <summary>A list of token categories that most programming languages have.</summary>
	/// <remarks>
	/// Some Loyc languages will support the concept of a "token literal" which
	/// is a <see cref="TokenTree"/>, and some DSLs will rely on these token 
	/// literals for input. However, tokens differ between different languages; 
	/// for instance the set of operators varies between languages. On the other 
	/// hand, most languages do have some concept of "an operator" and "an 
	/// identifier", and the TokenKind reflects this fact.
	/// <para/>
	/// When you are using <see cref="Token"/> to represent tokens in your language,
	/// it is recommended to define every value of your "TokenType" enumeration in 
	/// terms of TokenKind using integer offsets, like this:
	/// <pre>
	/// enum MyTokenType {
	///     EOF         = TokenKind.Spaces,
	///     Id          = TokenKind.Id,
	///     IfKeyword   = TokenKind.OtherKeyword,
	///     ForKeyword  = TokenKind.OtherKeyword + 1,
	///     LoopKeyword = TokenKind.OtherKeyword + 2,
	///     ...
	///     MulOp   = TokenKind.Operator,
	///     AddOp   = TokenKind.Operator + 1,
	///     DivOp   = TokenKind.Operator + 2,
	///     DotOp   = TokenKind.Dot,
	///     ...
	/// }
	/// </pre>
	/// Using TokenKind is only important if you intend to support DSLs via token
	/// literals (e.g. LLLPG) in your language.
	/// <para/>
	/// A DSL that just needs simple tokens like "strings", "identifiers" and "dots" 
	/// can write a parser based on values of <see cref="Token.Kind"/> alone; if 
	/// it needs certain specific operators or "keywords" that do not have a 
	/// dedicated TokenKind, such as + and %, it can further check the Value of the 
	/// token; meanwhile, the host language put a global <see cref="Symbol"/> 
	/// in the <see cref="Token.Value"/> to represent operators, keywords and 
	/// identifiers.
	/// </remarks>
	public enum TokenKind : ushort
	{
		/// <summary>For token types not covered by other token kinds.</summary>
		Other        = 0x0000,
		/// <summary>Single- and multi-line comments</summary>
		/// <remarks>Spaces and comments are typically filtered out before parsing and will not appear in token literals.</remarks>
		Comment      = 0x0100,
		/// <summary>Simple identifiers</summary>
		Id           = 0x0200,
		/// <summary>Literals, such as numbers and strings.</summary>
		Literal      = 0x0300,
		/// <summary>Scope operator (dot and dot-like ops such as :: in C++) </summary>
		Dot          = 0x0600,
		/// <summary>Simple or compound assignment</summary>
		Assignment   = 0x0700,
		/// <summary>All operators except assignment, dot, or separators</summary>
		Operator     = 0x0800,
		/// <summary>e.g. semicolon, comma (if not considered an operator)</summary>
		Separator    = 0x0900,
		/// <summary>e.g. public, private, static, virtual</summary>
		AttrKeyword  = 0x0A00,
		/// <summary>e.g. int, bool, double, void</summary>
		TypeKeyword  = 0x0B00,
		/// <summary>e.g. sizeof, struct. Does not include literal keywords (true, false, null)</summary>
		OtherKeyword = 0x0C00,
		/// <summary>Spaces, tabs, non-semantic newlines, and EOF</summary>
		/// <remarks>Spaces and comments are typically filtered out before parsing and will not appear in token literals.</remarks>
		Spaces       = 0x0F00, 
		LParen       = 0x1000,
		RParen       = 0x1100,
		LBrack       = 0x1200,
		RBrack       = 0x1300,
		LBrace       = 0x1400,
		RBrace       = 0x1500,
		Indent       = 0x1600,
		Dedent       = 0x1700,
		LOther       = 0x1800, // other opener(s)
		ROther       = 0x1900, // other closer(s)
		KindMask     = 0x1F00,
		/// <summary>Openers and closers all have this bit set.</summary>
		BracketFlag = 0x1000,
		/// <summary>Closers all have this bit set.</summary>
		CloserFlag = 0x0100,
	}
}
