using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;

namespace Loyc.Syntax
{
	/// <summary>A list of token categories that most programming languages have.</summary>
	/// <remarks>
	/// Some Loyc languages will support the concept of a "token literal" which
	/// is a tree of tokens, and some DSLs will rely on these token literals for
	/// input. However, tokens differ between different languages; for instance
	/// the set of operators varies between languages.
	/// <para/>
	/// The "TokenKind" concept can allow simple DSLs to support multiple host 
	/// languages, 
	/// 
	/// a limited degree of interoperability between different 
	/// languages,
	/// </remarks>
	public enum TokenKind
	{
		Spacer       = 0x000,
		Id           = 0x100,
		Number       = 0x200,
		Literal      = 0x300, // literals other than numbers
		Dot          = 0x400,
		Assignment   = 0x500,
		Operator     = 0x600, // other than assignment, dot, or separators
		Separator    = 0x700,
		AttrKeyword  = 0x800,
		TypeKeyword  = 0x900,
		OtherKeyword = 0xA00,
		Parens       = 0xB00,
		Bracks       = 0xC00,
		Braces       = 0xD00,
		OtherGroup   = 0xE00,
		Other        = 0xF00,
		KindMask     = 0xF00,
	}
}
