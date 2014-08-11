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
	/// is a tree of tokens, and some DSLs will rely on these token literals for
	/// input. However, tokens differ between different languages; for instance
	/// the set of operators varies between languages. On the other hand, most
	/// languages do have some concept of "an operator", and the TokenKind reflects
	/// this fact.
	/// <para/>
	/// The "TokenKind" concept may assist some simple DSLs to support multiple 
	/// host languages, by breaking tokens down into common categories. Thus, for
	/// instance, all the various strings in a language should be based on 
	/// TokenKind.String, the dot operator should be TokenKind.Dot, etc. A DSL
	/// that just needs simple tokens like "strings", "identifiers" and "dots" 
	/// can write a parser based on values of <see cref="Token.Kind"/> alone; if 
	/// it needs certain specific operators or "keywords" that do not have a 
	/// dedicated TokenKind, such as + and %, it can further check the 
	/// <see cref="Token.Value"/>.
	/// </remarks>
	public enum TokenKind
	{
		Spaces       = 0x0000, // Spaces, tabs, and non-semantic newlines*
		Comment      = 0x0100, // Single- and multi-line comments*
		// * Spaces and comments are typically filtered out before parsing and may 
		//   not appear in token literals.
		Id           = 0x0200, // Simple identifiers 
		Number       = 0x0300, // Integers, floats
		String       = 0x0400, // e.g. single-quoted, double-quoted, triple-quoted
		OtherLit     = 0x0500, // Literals other than numbers and strings
		Dot          = 0x0600, // dot and dot-like ops such as C++'s ::
		Assignment   = 0x0700, // Simple or compound assignment
		Operator     = 0x0800, // Other than assignment, dot, or separators
		Separator    = 0x0900, // e.g. comma, semicolon
		AttrKeyword  = 0x0A00, // e.g. public, private, static, virtual
		TypeKeyword  = 0x0B00, // e.g. int, bool, double, void
		OtherKeyword = 0x0C00, // e.g. sizeof, struct
		Other        = 0x0F00,
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
	}
}
