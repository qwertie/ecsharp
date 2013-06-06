using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;

namespace Loyc.Syntax
{
	public enum TokenKind
	{
		Spacer       = 0x000,
		Id           = 0x100,
		Number       = 0x200,
		OtherLiteral = 0x300,
		Dot          = 0x400,
		Assignment   = 0x500,
		Operator     = 0x600,
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

	public interface IToken : ICloneable<IToken>
	{
		SourceRange Range { get; }
		ISourceFile Source { get; }
		
		Symbol Type { get; }
		LNode WithType(Symbol name);

		TokenKind Kind { get; }

		object Value { get; }
		LiteralNode WithValue(object value);

		IListSource<IToken> Children { get; }
	}
}
